using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using DTMAPI.Abstractions;
using DTMAPI.Core.Diagnostics;

namespace DTMAPI.Core.Services
{
    internal sealed class EventManager
    {
        private const int HighFrequencyFailureThreshold = 3;
        private const int MaxWarningKeys = 128;
        private const int MaxQueueKeyChars = 256;
        private readonly DiagnosticsService diagnostics;
        private readonly Func<bool> mainThreadBoundaryEnabled;
        private readonly Func<bool> eventHandlerQuarantineEnabled;
        private readonly Func<int> runtimeThreadIdProvider;
        private readonly Func<string> phaseProvider;
        private readonly Action<string, string, string, string>? recordOwnerRegistration;
        private readonly Action<string, string, int, string>? recordOwnerCleanup;
        private readonly Action<EventListenerTransition>? listenerTransition;
        private readonly EventKernelOptions kernelOptions;
        private readonly Func<bool> eventHandlerTimingEnabled;
        private readonly object dispatchGate = new object();
        private readonly LinkedList<QueuedEventDispatch> queuedDispatches = new LinkedList<QueuedEventDispatch>();
        private readonly Dictionary<string, LinkedListNode<QueuedEventDispatch>> coalescedDispatches = new Dictionary<string, LinkedListNode<QueuedEventDispatch>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<EventDispatchPolicy, EventQueuePolicyCounters> policyCounters = new Dictionary<EventDispatchPolicy, EventQueuePolicyCounters>();
        private readonly HashSet<string> warningKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private long totalQueued;
        private long totalDispatched;
        private long totalRejected;
        private long totalFlushed;
        private long totalCoalesced;
        private long totalDropped;
        private long totalOverflow;
        private long totalQueueDispatchFailures;
        private long trimmedWarningKeyCount;
        private long boundaryDiagnosticRevision;
        private int maxQueueDepth;
        private int queuedDispatchCount;
        private int flushActive;
        private string lastQueuedEvent = string.Empty;
        private string lastRejectedEvent = string.Empty;
        private string lastDroppedEvent = string.Empty;
        private string lastPhase = string.Empty;
        private int lastThreadId;
        private Action<string>? publicationPreparedForTests;

        private readonly EventSlot<GameLaunchedEventArgs> gameLaunched;
        private readonly EventSlot<UpdateTickedEventArgs> updateTicked;
        private readonly EventSlot<OneSecondUpdateTickedEventArgs> oneSecondUpdateTicked;
        private readonly EventSlot<ReturnedToTitleEventArgs> returnedToTitle;
        private readonly EventSlot<ButtonPressedEventArgs> buttonPressed;
        private readonly EventSlot<ButtonReleasedEventArgs> buttonReleased;
        private readonly EventSlot<KeybindPressedEventArgs> keybindPressed;
        private readonly EventSlot<KeybindReleasedEventArgs> keybindReleased;
        private readonly EventSlot<SaveLoadedEventArgs> saveLoaded;
        private readonly EventSlot<SaveSavingEventArgs> saveSaving;
        private readonly EventSlot<SaveSavedEventArgs> saveSaved;
        private readonly EventSlot<MenuOpenedEventArgs> menuOpened;
        private readonly EventSlot<MenuClosedEventArgs> menuClosed;
        private readonly EventSlot<WorkshopModListChangedEventArgs> workshopModListChanged;
        private readonly EventSlot<LogExportedEventArgs> logExported;
        private readonly EventSlot<HookStatusChangedEventArgs> hookStatusChanged;

        public EventManager(
            DiagnosticsService diagnostics,
            Func<bool>? mainThreadBoundaryEnabled = null,
            Func<bool>? eventHandlerQuarantineEnabled = null,
            Func<int>? runtimeThreadIdProvider = null,
            Func<string>? phaseProvider = null,
            Action<string, string, string, string>? recordOwnerRegistration = null,
            Action<string, string, int, string>? recordOwnerCleanup = null,
            EventKernelOptions? kernelOptions = null,
            Action<EventListenerTransition>? listenerTransition = null,
            Func<bool>? eventHandlerTimingEnabled = null)
        {
            this.diagnostics = diagnostics;
            this.mainThreadBoundaryEnabled = mainThreadBoundaryEnabled ?? (() => false);
            this.eventHandlerQuarantineEnabled = eventHandlerQuarantineEnabled ?? (() => false);
            this.runtimeThreadIdProvider = runtimeThreadIdProvider ?? (() => Thread.CurrentThread.ManagedThreadId);
            this.phaseProvider = phaseProvider ?? (() => string.Empty);
            this.recordOwnerRegistration = recordOwnerRegistration;
            this.recordOwnerCleanup = recordOwnerCleanup;
            this.kernelOptions = kernelOptions ?? EventKernelOptions.Default;
            this.listenerTransition = listenerTransition;
            this.eventHandlerTimingEnabled = eventHandlerTimingEnabled ?? (() => this.kernelOptions.MeasureHandlerTiming);
            policyCounters.Add(EventDispatchPolicy.Direct, new EventQueuePolicyCounters());
            policyCounters.Add(EventDispatchPolicy.BoundedFifo, new EventQueuePolicyCounters());
            policyCounters.Add(EventDispatchPolicy.CoalesceLatestByKey, new EventQueuePolicyCounters());
            policyCounters.Add(EventDispatchPolicy.Reject, new EventQueuePolicyCounters());
            gameLaunched = CreateSlot<GameLaunchedEventArgs>("GameLoop.GameLaunched");
            updateTicked = CreateSlot<UpdateTickedEventArgs>("GameLoop.UpdateTicked", HighFrequencyFailureThreshold);
            oneSecondUpdateTicked = CreateSlot<OneSecondUpdateTickedEventArgs>("GameLoop.OneSecondUpdateTicked", HighFrequencyFailureThreshold);
            returnedToTitle = CreateSlot<ReturnedToTitleEventArgs>("GameLoop.ReturnedToTitle");
            buttonPressed = CreateSlot<ButtonPressedEventArgs>("Input.ButtonPressed");
            buttonReleased = CreateSlot<ButtonReleasedEventArgs>("Input.ButtonReleased");
            keybindPressed = CreateSlot<KeybindPressedEventArgs>("Input.KeybindPressed");
            keybindReleased = CreateSlot<KeybindReleasedEventArgs>("Input.KeybindReleased");
            saveLoaded = CreateSlot<SaveLoadedEventArgs>("Save.SaveLoaded");
            saveSaving = CreateSlot<SaveSavingEventArgs>("Save.SaveSaving");
            saveSaved = CreateSlot<SaveSavedEventArgs>("Save.SaveSaved");
            menuOpened = CreateSlot<MenuOpenedEventArgs>("UI.MenuOpened");
            menuClosed = CreateSlot<MenuClosedEventArgs>("UI.MenuClosed");
            workshopModListChanged = CreateSlot<WorkshopModListChangedEventArgs>("Workshop.ModListChanged");
            logExported = CreateSlot<LogExportedEventArgs>("Diagnostics.LogExported");
            hookStatusChanged = CreateSlot<HookStatusChangedEventArgs>("Diagnostics.HookStatusChanged");
        }

        public IEventsHelper CreateProxy(string owner) => new EventsProxy(this, owner, () => { });

        public IEventsHelper CreateOwnerBoundProxy(string owner, Action ensureOwnerActive) => new EventsProxy(this, owner, ensureOwnerActive ?? throw new ArgumentNullException(nameof(ensureOwnerActive)));

        /// <summary>
        /// Cheap queue hint for the Runtime safe points. A concurrent enqueue after a false
        /// result is intentionally handled by the next safe point.
        /// </summary>
        public bool HasQueuedDispatches => Volatile.Read(ref queuedDispatchCount) > 0;

        /// <summary>
        /// Changes for queue, rejection, overflow, failure, or bounded-warning state only.
        /// Ordinary direct dispatch counters deliberately don't advance this revision, since
        /// doing so would rebuild detailed diagnostics on every player frame.
        /// </summary>
        public long BoundaryDiagnosticRevision => Interlocked.Read(ref boundaryDiagnosticRevision);

        internal void ConfigurePublicationPreparedCheckpointForTests(Action<string>? checkpoint)
        {
            publicationPreparedForTests = checkpoint;
        }

        private EventSlot<TArgs> CreateSlot<TArgs>(string eventName, int failureThreshold = 0) where TArgs : EventArgs
        {
            return new EventSlot<TArgs>(
                diagnostics,
                eventName,
                failureThreshold,
                eventHandlerQuarantineEnabled,
                recordOwnerRegistration,
                PublishListenerTransition,
                eventHandlerTimingEnabled);
        }

        private void PublishListenerTransition(EventListenerTransition transition)
        {
            // Listener membership/quarantine changes are rare diagnostic mutations. Mark
            // them dirty so Runtime publishes one fresh detailed snapshot at the next safe
            // point, while ordinary direct event delivery remains revision-neutral.
            Interlocked.Increment(ref boundaryDiagnosticRevision);
            if (listenerTransition == null)
                return;

            try
            {
                listenerTransition(transition);
            }
            catch (Exception ex)
            {
                diagnostics.RecordError(
                    "DTMAPI.Core.EventManager",
                    "Event listener transition callback failed for " + transition.EventName + ".",
                    ex.ToString());
            }
        }

        public int RemoveOwner(string owner)
        {
            if (string.IsNullOrWhiteSpace(owner))
                return 0;

            int removed = 0;
            removed += gameLaunched.RemoveOwner(owner);
            removed += updateTicked.RemoveOwner(owner);
            removed += oneSecondUpdateTicked.RemoveOwner(owner);
            removed += returnedToTitle.RemoveOwner(owner);
            removed += buttonPressed.RemoveOwner(owner);
            removed += buttonReleased.RemoveOwner(owner);
            removed += keybindPressed.RemoveOwner(owner);
            removed += keybindReleased.RemoveOwner(owner);
            removed += saveLoaded.RemoveOwner(owner);
            removed += saveSaving.RemoveOwner(owner);
            removed += saveSaved.RemoveOwner(owner);
            removed += menuOpened.RemoveOwner(owner);
            removed += menuClosed.RemoveOwner(owner);
            removed += workshopModListChanged.RemoveOwner(owner);
            removed += logExported.RemoveOwner(owner);
            removed += hookStatusChanged.RemoveOwner(owner);
            if (removed > 0)
                recordOwnerCleanup?.Invoke(owner, "EventHandler", removed, "Owner cleanup removed active/quarantined event handlers.");
            return removed;
        }

        public int CountOwnerResources(string owner)
        {
            if (string.IsNullOrWhiteSpace(owner))
                return 0;

            return gameLaunched.CountOwnerResources(owner) +
                updateTicked.CountOwnerResources(owner) +
                oneSecondUpdateTicked.CountOwnerResources(owner) +
                returnedToTitle.CountOwnerResources(owner) +
                buttonPressed.CountOwnerResources(owner) +
                buttonReleased.CountOwnerResources(owner) +
                keybindPressed.CountOwnerResources(owner) +
                keybindReleased.CountOwnerResources(owner) +
                saveLoaded.CountOwnerResources(owner) +
                saveSaving.CountOwnerResources(owner) +
                saveSaved.CountOwnerResources(owner) +
                menuOpened.CountOwnerResources(owner) +
                menuClosed.CountOwnerResources(owner) +
                workshopModListChanged.CountOwnerResources(owner) +
                logExported.CountOwnerResources(owner) +
                hookStatusChanged.CountOwnerResources(owner);
        }

        public EventHandlerCleanupSnapshot GetHandlerCleanupSnapshot()
        {
            EventSlotSnapshot[] slots =
            {
                gameLaunched.GetSnapshot(),
                updateTicked.GetSnapshot(),
                oneSecondUpdateTicked.GetSnapshot(),
                returnedToTitle.GetSnapshot(),
                buttonPressed.GetSnapshot(),
                buttonReleased.GetSnapshot(),
                keybindPressed.GetSnapshot(),
                keybindReleased.GetSnapshot(),
                saveLoaded.GetSnapshot(),
                saveSaving.GetSnapshot(),
                saveSaved.GetSnapshot(),
                menuOpened.GetSnapshot(),
                menuClosed.GetSnapshot(),
                workshopModListChanged.GetSnapshot(),
                logExported.GetSnapshot(),
                hookStatusChanged.GetSnapshot()
            };

            return new EventHandlerCleanupSnapshot(eventHandlerQuarantineEnabled(), slots);
        }

        public void DispatchGameLaunched()
        {
            if (TryPrepareDispatch(gameLaunched, EventDispatchPolicy.Reject, "DTMAPI.Core", out var preparation))
                DispatchPrepared(gameLaunched, new GameLaunchedEventArgs(), preparation, coalesceKey: null);
        }

        public void DispatchUpdateTicked(ulong tick)
        {
            if (TryPrepareDispatch(updateTicked, EventDispatchPolicy.Direct, "DTMAPI.Core", out var preparation))
                DispatchPrepared(updateTicked, new UpdateTickedEventArgs(tick), preparation, coalesceKey: null);
        }

        public void DispatchOneSecondUpdateTicked(uint second)
        {
            if (TryPrepareDispatch(oneSecondUpdateTicked, EventDispatchPolicy.Direct, "DTMAPI.Core", out var preparation))
                DispatchPrepared(oneSecondUpdateTicked, new OneSecondUpdateTickedEventArgs(second), preparation, coalesceKey: null);
        }

        public void DispatchReturnedToTitle()
        {
            if (TryPrepareDispatch(returnedToTitle, EventDispatchPolicy.BoundedFifo, "DTMAPI.Core", out var preparation))
                DispatchPrepared(returnedToTitle, new ReturnedToTitleEventArgs(), preparation, coalesceKey: null);
        }

        public void DispatchButtonPressed(string button)
        {
            if (TryPrepareDispatch(buttonPressed, EventDispatchPolicy.Direct, "DTMAPI.Core.Input", out var preparation))
                DispatchPrepared(buttonPressed, new ButtonPressedEventArgs(button), preparation, coalesceKey: null);
        }

        internal EventDeliveryReceipt DispatchButtonPressedWithReceipt(string button)
        {
            if (!TryPrepareDispatch(buttonPressed, EventDispatchPolicy.Direct, "DTMAPI.Core.Input", out var preparation))
                return EventDeliveryReceipt.Empty;
            return DispatchPreparedWithReceipt(buttonPressed, new ButtonPressedEventArgs(button), preparation);
        }

        internal int DispatchButtonPressedToOwner(string ownerId, string button)
        {
            if (!TryPrepareOwnerDispatch(buttonPressed, ownerId, "DTMAPI.Core.Input.LegacyModal", out var preparation))
                return 0;
            return DispatchPreparedToOwner(buttonPressed, new ButtonPressedEventArgs(button), preparation, ownerId);
        }

        internal EventDeliveryReceipt DispatchButtonPressedToOwnerWithReceipt(string ownerId, string button)
        {
            if (!TryPrepareOwnerDispatch(buttonPressed, ownerId, "DTMAPI.Core.Input.OwnerModal", out var preparation))
                return EventDeliveryReceipt.Empty;
            return DispatchPreparedToOwnerWithReceipt(buttonPressed, new ButtonPressedEventArgs(button), preparation, ownerId);
        }

        public void DispatchButtonReleased(string button)
        {
            if (TryPrepareDispatch(buttonReleased, EventDispatchPolicy.Direct, "DTMAPI.Core.Input", out var preparation))
                DispatchPrepared(buttonReleased, new ButtonReleasedEventArgs(button), preparation, coalesceKey: null);
        }

        internal void DispatchButtonReleasedToOwners(IReadOnlyList<string> ownerIds, string button)
        {
            if (ownerIds == null)
                return;
            for (int index = 0; index < ownerIds.Count; index++)
            {
                string ownerId = ownerIds[index];
                if (!TryPrepareOwnerDispatch(buttonReleased, ownerId, "DTMAPI.Core.Input.ReleaseSettlement", out var preparation))
                    continue;
                DispatchPreparedToOwner(buttonReleased, new ButtonReleasedEventArgs(button), preparation, ownerId);
            }
        }

        public void DispatchKeybindPressed(string ownerId, string keybindId, DtmKeybindList keybinds, string triggerButton)
        {
            if (TryPrepareDispatch(keybindPressed, EventDispatchPolicy.Direct, "DTMAPI.Core.Input", out var preparation))
                DispatchPrepared(keybindPressed, new KeybindPressedEventArgs(ownerId, keybindId, keybinds, triggerButton), preparation, coalesceKey: null);
        }

        internal EventDeliveryReceipt DispatchKeybindPressedWithReceipt(string ownerId, string keybindId, DtmKeybindList keybinds, string triggerButton)
        {
            if (!TryPrepareDispatch(keybindPressed, EventDispatchPolicy.Direct, "DTMAPI.Core.Input", out var preparation))
                return EventDeliveryReceipt.Empty;
            return DispatchPreparedWithReceipt(keybindPressed, new KeybindPressedEventArgs(ownerId, keybindId, keybinds, triggerButton), preparation);
        }

        internal EventDeliveryReceipt DispatchKeybindPressedToOwnerWithReceipt(string targetOwnerId, string ownerId, string keybindId, DtmKeybindList keybinds, string triggerButton)
        {
            if (!TryPrepareOwnerDispatch(keybindPressed, targetOwnerId, "DTMAPI.Core.Input.OwnerModal", out var preparation))
                return EventDeliveryReceipt.Empty;
            return DispatchPreparedToOwnerWithReceipt(keybindPressed, new KeybindPressedEventArgs(ownerId, keybindId, keybinds, triggerButton), preparation, targetOwnerId);
        }

        public void DispatchKeybindReleased(string ownerId, string keybindId, DtmKeybindList keybinds, string triggerButton)
        {
            if (TryPrepareDispatch(keybindReleased, EventDispatchPolicy.Direct, "DTMAPI.Core.Input", out var preparation))
                DispatchPrepared(keybindReleased, new KeybindReleasedEventArgs(ownerId, keybindId, keybinds, triggerButton), preparation, coalesceKey: null);
        }

        internal void DispatchKeybindReleasedToOwners(IReadOnlyList<string> ownerIds, string ownerId, string keybindId, DtmKeybindList keybinds, string triggerButton)
        {
            if (ownerIds == null)
                return;
            for (int index = 0; index < ownerIds.Count; index++)
            {
                string targetOwnerId = ownerIds[index];
                if (!TryPrepareOwnerDispatch(keybindReleased, targetOwnerId, "DTMAPI.Core.Input.ReleaseSettlement", out var preparation))
                    continue;
                DispatchPreparedToOwner(
                    keybindReleased,
                    new KeybindReleasedEventArgs(ownerId, keybindId, keybinds, triggerButton),
                    preparation,
                    targetOwnerId);
            }
        }

        public void DispatchSaveLoaded(int? saveSlot, bool isNewGame)
        {
            if (TryPrepareDispatch(saveLoaded, EventDispatchPolicy.BoundedFifo, "DTMAPI.Core", out var preparation))
                DispatchPrepared(saveLoaded, new SaveLoadedEventArgs(saveSlot, isNewGame), preparation, coalesceKey: null);
        }

        public void DispatchSaveSaving(int? saveSlot) =>
            _ = TryDispatchSaveSaving(saveSlot);

        public bool TryDispatchSaveSaving(int? saveSlot)
        {
            long failuresBefore =
                saveSaving.HandlerFailureCount;
            if (TryPrepareDispatch(saveSaving, EventDispatchPolicy.BoundedFifo, "DTMAPI.Core", out var preparation))
            {
                DispatchPrepared(saveSaving, new SaveSavingEventArgs(saveSlot), preparation, coalesceKey: null);
            }
            return saveSaving.HandlerFailureCount ==
                failuresBefore;
        }

        public void DispatchSaveSaved(int? saveSlot)
        {
            if (TryPrepareDispatch(saveSaved, EventDispatchPolicy.BoundedFifo, "DTMAPI.Core", out var preparation))
                DispatchPrepared(saveSaved, new SaveSavedEventArgs(saveSlot), preparation, coalesceKey: null);
        }

        public void DispatchMenuOpened(string menuId)
        {
            if (TryPrepareDispatch(menuOpened, EventDispatchPolicy.BoundedFifo, "DTMAPI.Core.UI", out var preparation))
                DispatchPrepared(menuOpened, new MenuOpenedEventArgs(menuId), preparation, coalesceKey: null);
        }

        public void DispatchMenuClosed(string menuId)
        {
            if (TryPrepareDispatch(menuClosed, EventDispatchPolicy.BoundedFifo, "DTMAPI.Core.UI", out var preparation))
                DispatchPrepared(menuClosed, new MenuClosedEventArgs(menuId), preparation, coalesceKey: null);
        }

        public void DispatchWorkshopModListChanged(int count)
        {
            if (TryPrepareDispatch(workshopModListChanged, EventDispatchPolicy.CoalesceLatestByKey, "DTMAPI.Core.Workshop", out var preparation))
                DispatchPrepared(workshopModListChanged, new WorkshopModListChangedEventArgs(count), preparation, coalesceKey: "latest");
        }

        public void DispatchLogExported(string path)
        {
            if (TryPrepareDispatch(logExported, EventDispatchPolicy.BoundedFifo, "DTMAPI.Core.Diagnostics", out var preparation))
                DispatchPrepared(logExported, new LogExportedEventArgs(path), preparation, coalesceKey: null);
        }

        public void DispatchHookStatusChanged(string hookId, string status)
        {
            if (TryPrepareDispatch(hookStatusChanged, EventDispatchPolicy.CoalesceLatestByKey, "DTMAPI.Core.Diagnostics", out var preparation))
                DispatchPrepared(hookStatusChanged, new HookStatusChangedEventArgs(hookId, status), preparation, coalesceKey: hookId);
        }

        public void RecordRejectedExternalEvent(string eventName, string producerOwner, string reason)
        {
            if (!mainThreadBoundaryEnabled())
                return;
            RecordRejected(eventName, producerOwner, reason, EventDispatchPolicy.Reject);
        }

        public int FlushQueuedDispatches()
        {
            if (Interlocked.CompareExchange(ref flushActive, 1, 0) != 0)
                return 0;

            int flushed = 0;
            try
            {
                int available;
                lock (dispatchGate)
                    available = Math.Min(queuedDispatches.Count, kernelOptions.FlushBudget);

                while (flushed < available)
                {
                    QueuedEventDispatch? dispatch;
                    lock (dispatchGate)
                    {
                        LinkedListNode<QueuedEventDispatch>? node = queuedDispatches.First;
                        if (node == null)
                            break;
                        dispatch = node.Value;
                        RemoveQueuedNodeLocked(node, dropped: false);
                    }

                    bool failed = false;
                    Exception? failure = null;
                    try
                    {
                        dispatch.Execute(this);
                    }
                    catch (Exception ex)
                    {
                        failed = true;
                        failure = ex;
                    }

                    lock (dispatchGate)
                    {
                        totalFlushed++;
                        policyCounters[dispatch.Policy].TotalFlushed++;
                        if (failed)
                            totalQueueDispatchFailures++;
                        Interlocked.Increment(ref boundaryDiagnosticRevision);
                    }

                    if (failure != null)
                    {
                        diagnostics.RecordError(
                            string.IsNullOrWhiteSpace(dispatch.ProducerOwner) ? "DTMAPI.Core.EventManager" : dispatch.ProducerOwner,
                            "Queued event dispatch failed for " + dispatch.EventName + "; later queued items remain eligible.",
                            failure.ToString());
                    }
                    flushed++;
                }
            }
            finally
            {
                Volatile.Write(ref flushActive, 0);
            }

            return flushed;
        }

        public EventDispatchBoundarySnapshot GetBoundarySnapshot()
        {
            lock (dispatchGate)
                return BuildBoundarySnapshotNoLock();
        }

        private EventDispatchBoundarySnapshot BuildBoundarySnapshotNoLock()
        {
            var byPolicy = new Dictionary<EventDispatchPolicy, EventQueuePolicySnapshot>();
            foreach (KeyValuePair<EventDispatchPolicy, EventQueuePolicyCounters> pair in policyCounters)
                byPolicy[pair.Key] = pair.Value.CreateSnapshot();
            return new EventDispatchBoundarySnapshot(
                boundaryDiagnosticRevision,
                queuedDispatches.Count,
                maxQueueDepth,
                kernelOptions.QueueCapacity,
                kernelOptions.FlushBudget,
                totalQueued,
                Interlocked.Read(ref totalDispatched),
                totalRejected,
                totalFlushed,
                totalCoalesced,
                totalDropped,
                totalOverflow,
                totalQueueDispatchFailures,
                trimmedWarningKeyCount,
                lastQueuedEvent,
                lastRejectedEvent,
                lastDroppedEvent,
                lastPhase,
                lastThreadId,
                byPolicy);
        }

        internal bool EnqueueTestDispatch(string eventName, EventDispatchPolicy policy, string? coalesceKey, Action dispatch)
        {
            if (dispatch == null)
                throw new ArgumentNullException(nameof(dispatch));
            if (policy != EventDispatchPolicy.BoundedFifo && policy != EventDispatchPolicy.CoalesceLatestByKey)
                throw new ArgumentOutOfRangeException(nameof(policy), "Only queue-backed policies can be injected into the test queue.");

            string phase = phaseProvider() ?? string.Empty;
            return TryEnqueue(new TestQueuedEventDispatch(
                eventName ?? string.Empty,
                phase,
                Thread.CurrentThread.ManagedThreadId,
                "DTMAPI.Core.EventManager.Test",
                policy,
                NormalizeCoalesceKey(eventName, coalesceKey, policy),
                dispatch));
        }

        private bool TryPrepareDispatch<TArgs>(
            EventSlot<TArgs> slot,
            EventDispatchPolicy defaultPolicy,
            string producerOwner,
            out EventDispatchPreparation<TArgs> preparation) where TArgs : EventArgs
        {
            if (!slot.TryBeginPublication(out EventMembershipSnapshot<TArgs> membership))
            {
                preparation = default;
                return false;
            }

            return TryPrepareBoundary(slot.EventName, membership, defaultPolicy, producerOwner, out preparation);
        }

        private bool TryPrepareOwnerDispatch<TArgs>(
            EventSlot<TArgs> slot,
            string ownerId,
            string producerOwner,
            out EventDispatchPreparation<TArgs> preparation) where TArgs : EventArgs
        {
            if (!slot.TryBeginPublication(ownerId, out EventMembershipSnapshot<TArgs> membership))
            {
                preparation = default;
                return false;
            }

            return TryPrepareBoundary(slot.EventName, membership, EventDispatchPolicy.Direct, producerOwner, out preparation);
        }

        private bool TryPrepareBoundary<TArgs>(
            string eventName,
            EventMembershipSnapshot<TArgs> membership,
            EventDispatchPolicy defaultPolicy,
            string producerOwner,
            out EventDispatchPreparation<TArgs> preparation) where TArgs : EventArgs
        {
            EventDispatchPolicy policy = kernelOptions.ResolvePolicy(eventName, defaultPolicy);
            int threadId = Thread.CurrentThread.ManagedThreadId;
            string phase = phaseProvider() ?? string.Empty;
            if (!mainThreadBoundaryEnabled() || threadId == runtimeThreadIdProvider())
            {
                preparation = new EventDispatchPreparation<TArgs>(policy, membership, phase, threadId, producerOwner, queue: false);
                return true;
            }

            if (policy == EventDispatchPolicy.BoundedFifo || policy == EventDispatchPolicy.CoalesceLatestByKey)
            {
                preparation = new EventDispatchPreparation<TArgs>(policy, membership, phase, threadId, producerOwner, queue: true);
                return true;
            }

            string reason = policy == EventDispatchPolicy.Direct
                ? "direct-policy-non-runtime-thread"
                : "reject-policy-non-runtime-thread";
            RecordRejected(eventName, producerOwner, reason, policy);
            preparation = default;
            return false;
        }

        private void DispatchPrepared<TArgs>(
            EventSlot<TArgs> slot,
            TArgs args,
            EventDispatchPreparation<TArgs> preparation,
            string? coalesceKey) where TArgs : EventArgs
        {
            publicationPreparedForTests?.Invoke(slot.EventName);
            slot.RecordEventArgsCreated();
            if (!preparation.Queue)
            {
                slot.Dispatch(this, args, preparation.Membership);
                RecordDispatched(slot.EventName, preparation.Phase, preparation.ProducerThreadId);
                return;
            }

            TryEnqueue(new QueuedEventDispatch<TArgs>(
                slot,
                args,
                preparation.Membership,
                preparation.Phase,
                preparation.ProducerThreadId,
                preparation.ProducerOwner,
                preparation.Policy,
                NormalizeCoalesceKey(slot.EventName, coalesceKey, preparation.Policy)));
        }

        private int DispatchPreparedToOwner<TArgs>(
            EventSlot<TArgs> slot,
            TArgs args,
            EventDispatchPreparation<TArgs> preparation,
            string ownerId) where TArgs : EventArgs
        {
            publicationPreparedForTests?.Invoke(slot.EventName);
            slot.RecordEventArgsCreated();
            int dispatchedHandlers = slot.DispatchOwner(this, args, ownerId, preparation.Membership);
            RecordDispatched(slot.EventName, preparation.Phase, preparation.ProducerThreadId);
            return dispatchedHandlers;
        }

        private EventDeliveryReceipt DispatchPreparedWithReceipt<TArgs>(
            EventSlot<TArgs> slot,
            TArgs args,
            EventDispatchPreparation<TArgs> preparation) where TArgs : EventArgs
        {
            publicationPreparedForTests?.Invoke(slot.EventName);
            slot.RecordEventArgsCreated();
            string[] deliveredOwners = slot.DispatchWithReceipt(this, args, preparation.Membership);
            RecordDispatched(slot.EventName, preparation.Phase, preparation.ProducerThreadId);
            return deliveredOwners.Length == 0
                ? EventDeliveryReceipt.Empty
                : new EventDeliveryReceipt(deliveredOwners);
        }

        private EventDeliveryReceipt DispatchPreparedToOwnerWithReceipt<TArgs>(
            EventSlot<TArgs> slot,
            TArgs args,
            EventDispatchPreparation<TArgs> preparation,
            string ownerId) where TArgs : EventArgs
        {
            publicationPreparedForTests?.Invoke(slot.EventName);
            slot.RecordEventArgsCreated();
            string[] deliveredOwners = slot.DispatchOwnerWithReceipt(this, args, ownerId, preparation.Membership);
            RecordDispatched(slot.EventName, preparation.Phase, preparation.ProducerThreadId);
            return deliveredOwners.Length == 0
                ? EventDeliveryReceipt.Empty
                : new EventDeliveryReceipt(deliveredOwners);
        }

        private bool TryEnqueue(QueuedEventDispatch dispatch)
        {
            bool overflowed = false;
            bool accepted = false;
            bool canEnqueue = true;
            lock (dispatchGate)
            {
                EventQueuePolicyCounters counters = policyCounters[dispatch.Policy];
                if (dispatch.Policy == EventDispatchPolicy.CoalesceLatestByKey &&
                    coalescedDispatches.TryGetValue(dispatch.CoalesceKey, out LinkedListNode<QueuedEventDispatch> existing))
                {
                    existing.Value = dispatch;
                    totalCoalesced++;
                    counters.TotalCoalesced++;
                    lastQueuedEvent = dispatch.EventName;
                    lastPhase = dispatch.Phase;
                    lastThreadId = dispatch.ProducerThreadId;
                    Interlocked.Increment(ref boundaryDiagnosticRevision);
                    return true;
                }

                if (queuedDispatches.Count >= kernelOptions.QueueCapacity)
                {
                    overflowed = true;
                    totalOverflow++;
                    counters.TotalOverflow++;
                    if (dispatch.Policy == EventDispatchPolicy.CoalesceLatestByKey)
                    {
                        LinkedListNode<QueuedEventDispatch>? oldestCoalescable = FindOldestCoalescableLocked();
                        if (oldestCoalescable != null)
                        {
                            RemoveQueuedNodeLocked(oldestCoalescable, dropped: true);
                        }
                        else
                        {
                            totalDropped++;
                            counters.TotalDropped++;
                            lastDroppedEvent = dispatch.EventName;
                            canEnqueue = false;
                        }
                    }
                    else
                    {
                        totalDropped++;
                        counters.TotalDropped++;
                        lastDroppedEvent = dispatch.EventName;
                        canEnqueue = false;
                    }
                }

                if (canEnqueue)
                {
                    LinkedListNode<QueuedEventDispatch> node = queuedDispatches.AddLast(dispatch);
                    if (dispatch.Policy == EventDispatchPolicy.CoalesceLatestByKey)
                        coalescedDispatches[dispatch.CoalesceKey] = node;
                    totalQueued++;
                    counters.TotalQueued++;
                    counters.Pending++;
                    lastQueuedEvent = dispatch.EventName;
                    lastPhase = dispatch.Phase;
                    lastThreadId = dispatch.ProducerThreadId;
                    if (queuedDispatches.Count > maxQueueDepth)
                        maxQueueDepth = queuedDispatches.Count;
                    accepted = true;
                }
                Volatile.Write(ref queuedDispatchCount, queuedDispatches.Count);
                Interlocked.Increment(ref boundaryDiagnosticRevision);
            }

            if (overflowed)
                RecordWarningOnce(
                    dispatch.EventName + "::queue-overflow",
                    dispatch.ProducerOwner,
                    "Event queue capacity was reached.",
                    "event=" + dispatch.EventName + "; policy=" + dispatch.Policy + "; capacity=" + kernelOptions.QueueCapacity + "; phase=" + dispatch.Phase);
            return accepted;
        }

        private LinkedListNode<QueuedEventDispatch>? FindOldestCoalescableLocked()
        {
            LinkedListNode<QueuedEventDispatch>? node = queuedDispatches.First;
            while (node != null)
            {
                if (node.Value.Policy == EventDispatchPolicy.CoalesceLatestByKey)
                    return node;
                node = node.Next;
            }
            return null;
        }

        private void RemoveQueuedNodeLocked(LinkedListNode<QueuedEventDispatch> node, bool dropped)
        {
            QueuedEventDispatch dispatch = node.Value;
            queuedDispatches.Remove(node);
            Volatile.Write(ref queuedDispatchCount, queuedDispatches.Count);
            if (dispatch.Policy == EventDispatchPolicy.CoalesceLatestByKey &&
                coalescedDispatches.TryGetValue(dispatch.CoalesceKey, out LinkedListNode<QueuedEventDispatch> current) &&
                ReferenceEquals(current, node))
            {
                coalescedDispatches.Remove(dispatch.CoalesceKey);
            }

            EventQueuePolicyCounters counters = policyCounters[dispatch.Policy];
            if (counters.Pending > 0)
                counters.Pending--;
            if (dropped)
            {
                totalDropped++;
                counters.TotalDropped++;
                lastDroppedEvent = dispatch.EventName;
            }
        }

        private static string NormalizeCoalesceKey(string? eventName, string? key, EventDispatchPolicy policy)
        {
            if (policy != EventDispatchPolicy.CoalesceLatestByKey)
                return string.Empty;
            return BoundedKey(eventName) + "::" + BoundedKey(string.IsNullOrWhiteSpace(key) ? "latest" : key);
        }

        private void RecordDispatched(string eventName, string phase, int threadId)
        {
            Interlocked.Increment(ref totalDispatched);
            Volatile.Write(ref lastPhase, phase ?? string.Empty);
            Volatile.Write(ref lastThreadId, threadId);
        }

        private void RecordRejected(string eventName, string producerOwner, string reason, EventDispatchPolicy policy)
        {
            lock (dispatchGate)
            {
                totalRejected++;
                policyCounters[policy].TotalRejected++;
                lastRejectedEvent = eventName ?? string.Empty;
                lastPhase = phaseProvider() ?? string.Empty;
                lastThreadId = Thread.CurrentThread.ManagedThreadId;
                Interlocked.Increment(ref boundaryDiagnosticRevision);
            }

            RecordWarningOnce(
                (eventName ?? string.Empty) + "::" + (reason ?? string.Empty),
                producerOwner,
                "Event dispatch rejected outside runtime thread.",
                "event=" + (eventName ?? string.Empty) + "; reason=" + (reason ?? string.Empty) + "; phase=" + (phaseProvider() ?? string.Empty) + "; thread=" + Thread.CurrentThread.ManagedThreadId);
        }

        private void RecordWarningOnce(string warningKey, string owner, string message, string details)
        {
            bool publishWarning;
            lock (dispatchGate)
            {
                string key = BoundedKey(warningKey);
                if (warningKeys.Contains(key))
                {
                    publishWarning = false;
                }
                else if (warningKeys.Count < MaxWarningKeys)
                {
                    warningKeys.Add(key);
                    publishWarning = true;
                }
                else
                {
                    trimmedWarningKeyCount++;
                    Interlocked.Increment(ref boundaryDiagnosticRevision);
                    publishWarning = false;
                }
            }
            if (publishWarning)
                diagnostics.RecordWarning(string.IsNullOrWhiteSpace(owner) ? "DTMAPI.Core.EventManager" : owner, message, details);
        }

        private static string BoundedKey(string? value)
        {
            string text = (value ?? string.Empty).Trim();
            long trimmedBytes = 0;
            return BoundedDiagnosticScalar.Sanitize(text, MaxQueueKeyChars, ref trimmedBytes);
        }

        private sealed class EventSlot<TArgs> where TArgs : EventArgs
        {
            private const int MaxNamedQuarantineOwners = 128;
            private const int MaxNamedDiagnosticOwners = 128;
            private readonly DiagnosticsService diagnostics;
            private readonly int failureThreshold;
            private readonly Func<bool> quarantineEnabled;
            private readonly Action<string, string, string, string>? recordOwnerRegistration;
            private readonly Action<EventListenerTransition>? publishTransition;
            private readonly List<OwnedHandler<TArgs>> handlers = new List<OwnedHandler<TArgs>>();
            private readonly Dictionary<string, int> activeByOwner = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            private readonly Dictionary<string, EventOwnerDispatchState> dispatchStateByOwner = new Dictionary<string, EventOwnerDispatchState>(StringComparer.OrdinalIgnoreCase);
            private readonly Dictionary<string, EventOwnerSlotCounts> quarantinedByOwner = new Dictionary<string, EventOwnerSlotCounts>(StringComparer.OrdinalIgnoreCase);
            private readonly object gate = new object();
            private readonly object transitionOrderGate = new object();
            private EventMembershipSnapshot<TArgs> dispatchSnapshot = EventMembershipSnapshot<TArgs>.Empty;
            private long membershipVersion;
            private long publicationCalls;
            private long eventArgsCreated;
            private long snapshotRebuilds;
            private long handlerCalls;
            private long handlerFailures;
            private long handlerQuarantines;
            private long handlerTimingSamples;
            private long handlerTimingTotalTicks;
            private long handlerTimingMaxTicks;
            private long zeroListenerBypasses;
            private long trimmedQuarantineCount;
            private long trimmedQuarantineHandlerCalls;
            private long trimmedQuarantineHandlerFailures;
            private long nextTransitionTicket;
            private long nextTransitionTicketToPublish = 1;
            private int transitionPublisherThreadId;
            private readonly Func<bool> measureHandlerTiming;

            public EventSlot(
                DiagnosticsService diagnostics,
                string eventName,
                int failureThreshold = 0,
                Func<bool>? quarantineEnabled = null,
                Action<string, string, string, string>? recordOwnerRegistration = null,
                Action<EventListenerTransition>? publishTransition = null,
                Func<bool>? measureHandlerTiming = null)
            {
                this.diagnostics = diagnostics;
                EventName = eventName;
                this.failureThreshold = failureThreshold;
                this.quarantineEnabled = quarantineEnabled ?? (() => false);
                this.recordOwnerRegistration = recordOwnerRegistration;
                this.publishTransition = publishTransition;
                this.measureHandlerTiming = measureHandlerTiming ?? (() => false);
            }

            public string EventName { get; }

            public long HandlerFailureCount =>
                Interlocked.Read(ref handlerFailures);

            public bool TryBeginPublication(out EventMembershipSnapshot<TArgs> membership)
            {
                Interlocked.Increment(ref publicationCalls);
                membership = Volatile.Read(ref dispatchSnapshot);
                if (membership.Handlers.Length == 0)
                {
                    Interlocked.Increment(ref zeroListenerBypasses);
                    return false;
                }

                return true;
            }

            public bool TryBeginPublication(string ownerId, out EventMembershipSnapshot<TArgs> membership)
            {
                Interlocked.Increment(ref publicationCalls);
                membership = Volatile.Read(ref dispatchSnapshot);
                if (string.IsNullOrWhiteSpace(ownerId))
                {
                    Interlocked.Increment(ref zeroListenerBypasses);
                    return false;
                }

                string owner = ownerId.Trim();
                foreach (OwnedHandler<TArgs> entry in membership.Handlers)
                {
                    if (!entry.IsTerminallyCancelled &&
                        entry.Owner.Equals(owner, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                Interlocked.Increment(ref zeroListenerBypasses);
                return false;
            }

            public void Add(string owner, EventHandler<TArgs>? handler, Action ensureOwnerActive)
            {
                if (handler == null)
                    return;

                EnsureTransitionMutationAllowed();
                ensureOwnerActive();
                TransitionPublication transition;
                lock (gate)
                {
                    int previousCount = handlers.Count;
                    int previousOwnerCount = CountOwnerLocked(owner);
                    long version = Interlocked.Increment(ref membershipVersion);
                    if (!dispatchStateByOwner.TryGetValue(owner, out EventOwnerDispatchState? ownerDispatchState))
                    {
                        ownerDispatchState = new EventOwnerDispatchState();
                        dispatchStateByOwner.Add(owner, ownerDispatchState);
                    }
                    handlers.Add(new OwnedHandler<TArgs>(owner, handler, version, ownerDispatchState));
                    activeByOwner[owner] = previousOwnerCount + 1;
                    PublishSnapshotLocked();
                    transition = PrepareTransitionLocked(new EventListenerTransition(
                        EventName,
                        owner,
                        EventListenerTransitionReason.Added,
                        previousCount,
                        handlers.Count,
                        affectedRegistrations: 1,
                        previousOwnerCount: previousOwnerCount,
                        currentOwnerCount: previousOwnerCount + 1));
                }

                PublishTransitionInOrder(transition);
                recordOwnerRegistration?.Invoke(owner, "EventHandler", EventName, "Event handler registered.");
            }

            public void Remove(string owner, EventHandler<TArgs>? handler, Action ensureOwnerActive)
            {
                if (handler == null)
                    return;

                EnsureTransitionMutationAllowed();
                ensureOwnerActive();
                TransitionPublication transition = default;
                lock (gate)
                {
                    for (int index = handlers.Count - 1; index >= 0; index--)
                    {
                        OwnedHandler<TArgs> entry = handlers[index];
                        if (!entry.Owner.Equals(owner, StringComparison.OrdinalIgnoreCase) || entry.Handler != handler)
                            continue;

                        int previousCount = handlers.Count;
                        int previousOwnerCount = CountOwnerLocked(owner);
                        Interlocked.Increment(ref membershipVersion);
                        handlers.RemoveAt(index);
                        SetActiveOwnerCountLocked(owner, previousOwnerCount - 1);
                        PublishSnapshotLocked();
                        transition = PrepareTransitionLocked(new EventListenerTransition(
                            EventName,
                            owner,
                            EventListenerTransitionReason.Removed,
                            previousCount,
                            handlers.Count,
                            affectedRegistrations: 1,
                            previousOwnerCount: previousOwnerCount,
                            currentOwnerCount: previousOwnerCount - 1));
                        break;
                    }
                }

                PublishTransitionInOrder(transition);
            }

            public int RemoveOwner(string owner)
            {
                EnsureTransitionMutationAllowed();
                TransitionPublication transition = default;
                int removed;
                lock (gate)
                {
                    int previousCount = handlers.Count;
                    int previousOwnerCount = CountOwnerLocked(owner);
                    if (dispatchStateByOwner.TryGetValue(owner, out EventOwnerDispatchState? ownerDispatchState))
                    {
                        ownerDispatchState.Cancel();
                        dispatchStateByOwner.Remove(owner);
                    }
                    int activeRemoved = 0;
                    long removalVersion = 0;
                    for (int index = handlers.Count - 1; index >= 0; index--)
                    {
                        OwnedHandler<TArgs> entry = handlers[index];
                        if (!entry.Owner.Equals(owner, StringComparison.OrdinalIgnoreCase))
                            continue;

                        if (removalVersion == 0)
                            removalVersion = Interlocked.Increment(ref membershipVersion);
                        handlers.RemoveAt(index);
                        activeRemoved++;
                    }

                    if (activeRemoved > 0)
                    {
                        activeByOwner.Remove(owner);
                        PublishSnapshotLocked();
                    }

                    int quarantined = 0;
                    if (quarantinedByOwner.TryGetValue(owner, out EventOwnerSlotCounts? quarantinedCounts))
                    {
                        quarantined = quarantinedCounts.QuarantinedHandlers;
                        quarantinedByOwner.Remove(owner);
                    }

                    removed = activeRemoved + quarantined;
                    if (removed > 0)
                    {
                        transition = PrepareTransitionLocked(new EventListenerTransition(
                            EventName,
                            owner,
                            EventListenerTransitionReason.OwnerRemoved,
                            previousCount,
                            handlers.Count,
                            affectedRegistrations: removed,
                            previousOwnerCount: previousOwnerCount,
                            currentOwnerCount: 0));
                    }
                }

                PublishTransitionInOrder(transition);
                return removed;
            }

            public int CountOwnerResources(string owner)
            {
                string normalized = (owner ?? string.Empty).Trim();
                if (normalized.Length == 0)
                    return 0;

                lock (gate)
                {
                    int active = activeByOwner.TryGetValue(normalized, out int activeCount) ? activeCount : 0;
                    int quarantined = quarantinedByOwner.TryGetValue(normalized, out EventOwnerSlotCounts? counts)
                        ? counts.QuarantinedHandlers
                        : 0;
                    return active + quarantined;
                }
            }

            public EventSlotSnapshot GetSnapshot()
            {
                lock (gate)
                {
                    Dictionary<string, EventOwnerSlotCounts> byOwner = new Dictionary<string, EventOwnerSlotCounts>(StringComparer.OrdinalIgnoreCase);
                    long trimmedActiveHandlers = 0;
                    long trimmedActiveHandlerCalls = 0;
                    long trimmedActiveHandlerFailures = 0;
                    foreach (OwnedHandler<TArgs> entry in handlers)
                    {
                        if (!byOwner.TryGetValue(entry.Owner, out EventOwnerSlotCounts? existing))
                        {
                            if (byOwner.Count >= MaxNamedDiagnosticOwners)
                            {
                                trimmedActiveHandlers++;
                                trimmedActiveHandlerCalls += Interlocked.Read(ref entry.HandlerCalls);
                                trimmedActiveHandlerFailures += Interlocked.Read(ref entry.HandlerFailures);
                                continue;
                            }
                            existing = new EventOwnerSlotCounts();
                            byOwner.Add(entry.Owner, existing);
                        }
                        existing.ActiveHandlers++;
                        existing.DispatchableHandlers++;
                        existing.HandlerCalls += Interlocked.Read(ref entry.HandlerCalls);
                        existing.HandlerFailures += Interlocked.Read(ref entry.HandlerFailures);
                    }

                    long trimmedQuarantinedHandlers = 0;
                    long trimmedQuarantinedHandlerCalls = 0;
                    long trimmedQuarantinedHandlerFailures = 0;
                    long trimmedQuarantinedHandlerQuarantines = 0;
                    foreach (KeyValuePair<string, EventOwnerSlotCounts> entry in quarantinedByOwner)
                    {
                        if (!byOwner.TryGetValue(entry.Key, out EventOwnerSlotCounts? existing))
                        {
                            if (byOwner.Count >= MaxNamedDiagnosticOwners)
                            {
                                trimmedQuarantinedHandlers += entry.Value.QuarantinedHandlers;
                                trimmedQuarantinedHandlerCalls += entry.Value.HandlerCalls;
                                trimmedQuarantinedHandlerFailures += entry.Value.HandlerFailures;
                                trimmedQuarantinedHandlerQuarantines += entry.Value.HandlerQuarantines;
                                continue;
                            }
                            existing = new EventOwnerSlotCounts();
                            byOwner.Add(entry.Key, existing);
                        }
                        existing.QuarantinedHandlers += entry.Value.QuarantinedHandlers;
                        existing.HandlerCalls += entry.Value.HandlerCalls;
                        existing.HandlerFailures += entry.Value.HandlerFailures;
                        existing.HandlerQuarantines += entry.Value.HandlerQuarantines;
                    }

                    int quarantined = quarantinedByOwner.Values.Sum(value => value.QuarantinedHandlers);
                    int totalOwnerCount = activeByOwner.Count + quarantinedByOwner.Keys.Count(owner => !activeByOwner.ContainsKey(owner));
                    return new EventSlotSnapshot(
                        EventName,
                        handlers.Count,
                        quarantined,
                        handlers.Count,
                        byOwner,
                        trimmedQuarantineCount,
                        Interlocked.Read(ref publicationCalls),
                        Interlocked.Read(ref eventArgsCreated),
                        Interlocked.Read(ref snapshotRebuilds),
                        Interlocked.Read(ref handlerCalls),
                        Interlocked.Read(ref handlerFailures),
                        Interlocked.Read(ref handlerQuarantines),
                        Interlocked.Read(ref zeroListenerBypasses),
                        namedOwnerCapacity: MaxNamedDiagnosticOwners,
                        totalOwnerCount: totalOwnerCount,
                        trimmedOwnerCount: Math.Max(0, totalOwnerCount - byOwner.Count),
                        trimmedActiveHandlers: trimmedActiveHandlers,
                        trimmedQuarantinedHandlers: trimmedQuarantinedHandlers,
                        trimmedHandlerCalls: trimmedActiveHandlerCalls + trimmedQuarantinedHandlerCalls + trimmedQuarantineHandlerCalls,
                        trimmedHandlerFailures: trimmedActiveHandlerFailures + trimmedQuarantinedHandlerFailures + trimmedQuarantineHandlerFailures,
                        trimmedHandlerQuarantines: trimmedQuarantinedHandlerQuarantines + trimmedQuarantineCount,
                        handlerTimingEnabled: measureHandlerTiming(),
                        handlerTimingSamples: Interlocked.Read(ref handlerTimingSamples),
                        handlerTimingTotalTicks: Interlocked.Read(ref handlerTimingTotalTicks),
                        handlerTimingMaxTicks: Interlocked.Read(ref handlerTimingMaxTicks));
                }
            }

            public void RecordEventArgsCreated()
            {
                Interlocked.Increment(ref eventArgsCreated);
            }

            public void Dispatch(object sender, TArgs args, EventMembershipSnapshot<TArgs> membership)
            {
                DispatchCore(sender, args, ownerId: null, membership, deliveredOwners: null);
            }

            public int DispatchOwner(object sender, TArgs args, string ownerId, EventMembershipSnapshot<TArgs> membership)
            {
                if (string.IsNullOrWhiteSpace(ownerId))
                    return 0;
                return DispatchCore(sender, args, ownerId.Trim(), membership, deliveredOwners: null);
            }

            public string[] DispatchWithReceipt(object sender, TArgs args, EventMembershipSnapshot<TArgs> membership)
            {
                var deliveredOwners = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                DispatchCore(sender, args, ownerId: null, membership, deliveredOwners);
                return deliveredOwners.Count == 0 ? Array.Empty<string>() : deliveredOwners.ToArray();
            }

            public string[] DispatchOwnerWithReceipt(object sender, TArgs args, string ownerId, EventMembershipSnapshot<TArgs> membership)
            {
                if (string.IsNullOrWhiteSpace(ownerId))
                    return Array.Empty<string>();
                var deliveredOwners = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                DispatchCore(sender, args, ownerId.Trim(), membership, deliveredOwners);
                return deliveredOwners.Count == 0 ? Array.Empty<string>() : deliveredOwners.ToArray();
            }

            private int DispatchCore(object sender, TArgs args, string? ownerId, EventMembershipSnapshot<TArgs> membership, ISet<string>? deliveredOwners)
            {
                bool measureTiming = measureHandlerTiming();

                int dispatchedHandlers = 0;
                foreach (OwnedHandler<TArgs> entry in membership.Handlers)
                {
                    if (entry.IsTerminallyCancelled ||
                        (ownerId != null && !entry.Owner.Equals(ownerId, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    dispatchedHandlers++;
                    deliveredOwners?.Add(entry.Owner);
                    Interlocked.Increment(ref handlerCalls);
                    Interlocked.Increment(ref entry.HandlerCalls);
                    long timingStarted = measureTiming ? Stopwatch.GetTimestamp() : 0;
                    try
                    {
                        entry.Handler(sender, args);
                        Interlocked.Exchange(ref entry.ConsecutiveFailures, 0);
                    }
                    catch (Exception ex)
                    {
                        Interlocked.Increment(ref handlerFailures);
                        Interlocked.Increment(ref entry.HandlerFailures);
                        int consecutiveFailures = Interlocked.Increment(ref entry.ConsecutiveFailures);
                        diagnostics.RecordError(entry.Owner, "Unhandled exception in " + EventName + " event handler.", ex.ToString());
                        if (failureThreshold > 0 && consecutiveFailures >= failureThreshold && quarantineEnabled() && Quarantine(entry))
                        {
                            diagnostics.RecordError(
                                entry.Owner,
                                EventName + " event handler quarantined after " + consecutiveFailures + " consecutive failures.",
                                "DTMAPI opened a process-lifetime circuit breaker for this registration. An explicit later subscription creates a new registration.");
                        }
                    }
                    finally
                    {
                        if (measureTiming)
                            RecordHandlerTiming(Stopwatch.GetTimestamp() - timingStarted);
                    }
                }

                return dispatchedHandlers;
            }

            private void RecordHandlerTiming(long elapsedTicks)
            {
                elapsedTicks = Math.Max(0, elapsedTicks);
                Interlocked.Increment(ref handlerTimingSamples);
                Interlocked.Add(ref handlerTimingTotalTicks, elapsedTicks);
                long observed = Interlocked.Read(ref handlerTimingMaxTicks);
                while (elapsedTicks > observed)
                {
                    long previous = Interlocked.CompareExchange(ref handlerTimingMaxTicks, elapsedTicks, observed);
                    if (previous == observed)
                        break;
                    observed = previous;
                }
            }

            private bool Quarantine(OwnedHandler<TArgs> entry)
            {
                EnsureTransitionMutationAllowed();
                TransitionPublication transition;
                lock (gate)
                {
                    int index = handlers.IndexOf(entry);
                    if (index < 0)
                        return false;

                    int previousCount = handlers.Count;
                    int previousOwnerCount = CountOwnerLocked(entry.Owner);
                    Interlocked.Increment(ref membershipVersion);
                    entry.CancelPreparedPublications();
                    handlers.RemoveAt(index);
                    SetActiveOwnerCountLocked(entry.Owner, previousOwnerCount - 1);
                    PublishSnapshotLocked();
                    if (quarantinedByOwner.TryGetValue(entry.Owner, out EventOwnerSlotCounts? counts))
                    {
                        counts.QuarantinedHandlers++;
                        counts.HandlerCalls += Interlocked.Read(ref entry.HandlerCalls);
                        counts.HandlerFailures += Interlocked.Read(ref entry.HandlerFailures);
                        counts.HandlerQuarantines++;
                    }
                    else if (quarantinedByOwner.Count < MaxNamedQuarantineOwners)
                    {
                        quarantinedByOwner.Add(entry.Owner, new EventOwnerSlotCounts
                        {
                            QuarantinedHandlers = 1,
                            HandlerCalls = Interlocked.Read(ref entry.HandlerCalls),
                            HandlerFailures = Interlocked.Read(ref entry.HandlerFailures),
                            HandlerQuarantines = 1
                        });
                    }
                    else
                    {
                        // The delegate has already been released. Once the bounded named-owner
                        // table is full, retain only a historical scalar; don't misreport it as
                        // a current quarantined handler which owner cleanup could never remove.
                        trimmedQuarantineCount++;
                        trimmedQuarantineHandlerCalls += Interlocked.Read(ref entry.HandlerCalls);
                        trimmedQuarantineHandlerFailures += Interlocked.Read(ref entry.HandlerFailures);
                    }

                    Interlocked.Increment(ref handlerQuarantines);
                    transition = PrepareTransitionLocked(new EventListenerTransition(
                        EventName,
                        entry.Owner,
                        EventListenerTransitionReason.Quarantined,
                        previousCount,
                        handlers.Count,
                        affectedRegistrations: 1,
                        previousOwnerCount: previousOwnerCount,
                        currentOwnerCount: previousOwnerCount - 1));
                }

                PublishTransitionInOrder(transition);
                return true;
            }

            private TransitionPublication PrepareTransitionLocked(EventListenerTransition transition)
            {
                return new TransitionPublication(++nextTransitionTicket, transition);
            }

            private void EnsureTransitionMutationAllowed()
            {
                int threadId = Thread.CurrentThread.ManagedThreadId;
                lock (transitionOrderGate)
                {
                    if (transitionPublisherThreadId == threadId)
                        throw new InvalidOperationException("Event listener transition callbacks cannot mutate the same event slot reentrantly.");
                }
            }

            private void PublishTransitionInOrder(TransitionPublication publication)
            {
                if (publication.Ticket == 0 || publication.Transition == null)
                    return;

                int threadId = Thread.CurrentThread.ManagedThreadId;
                lock (transitionOrderGate)
                {
                    while (publication.Ticket != nextTransitionTicketToPublish)
                        Monitor.Wait(transitionOrderGate);
                    transitionPublisherThreadId = threadId;
                }

                try
                {
                    publishTransition?.Invoke(publication.Transition);
                }
                catch (Exception ex)
                {
                    // EventManager's normal transition adapter already isolates callbacks.
                    // Keep injected/future adapters from stranding the ticket sequence.
                    diagnostics.RecordError(
                        "DTMAPI.Core.EventManager",
                        "Event listener transition publication failed for " + EventName + ".",
                        ex.ToString());
                }
                finally
                {
                    lock (transitionOrderGate)
                    {
                        transitionPublisherThreadId = 0;
                        nextTransitionTicketToPublish++;
                        Monitor.PulseAll(transitionOrderGate);
                    }
                }
            }

            private readonly struct TransitionPublication
            {
                public TransitionPublication(long ticket, EventListenerTransition transition)
                {
                    Ticket = ticket;
                    Transition = transition;
                }

                public long Ticket { get; }
                public EventListenerTransition? Transition { get; }
            }

            private void PublishSnapshotLocked()
            {
                OwnedHandler<TArgs>[] handlersSnapshot = handlers.Count == 0
                    ? Array.Empty<OwnedHandler<TArgs>>()
                    : handlers.ToArray();
                var next = new EventMembershipSnapshot<TArgs>(
                    Interlocked.Read(ref membershipVersion),
                    handlersSnapshot);
                Volatile.Write(ref dispatchSnapshot, next);
                Interlocked.Increment(ref snapshotRebuilds);
            }

            private int CountOwnerLocked(string owner)
            {
                return activeByOwner.TryGetValue(owner, out int count) ? count : 0;
            }

            private void SetActiveOwnerCountLocked(string owner, int count)
            {
                if (count <= 0)
                    activeByOwner.Remove(owner);
                else
                    activeByOwner[owner] = count;
            }
        }

        private readonly struct EventDispatchPreparation<TArgs> where TArgs : EventArgs
        {
            public EventDispatchPreparation(
                EventDispatchPolicy policy,
                EventMembershipSnapshot<TArgs> membership,
                string phase,
                int producerThreadId,
                string producerOwner,
                bool queue)
            {
                Policy = policy;
                Membership = membership;
                Phase = phase;
                ProducerThreadId = producerThreadId;
                ProducerOwner = producerOwner;
                Queue = queue;
            }

            public EventDispatchPolicy Policy { get; }
            public EventMembershipSnapshot<TArgs> Membership { get; }
            public string Phase { get; }
            public int ProducerThreadId { get; }
            public string ProducerOwner { get; }
            public bool Queue { get; }
        }

        private abstract class QueuedEventDispatch
        {
            protected QueuedEventDispatch(
                string eventName,
                string phase,
                int producerThreadId,
                string producerOwner,
                EventDispatchPolicy policy,
                string coalesceKey)
            {
                EventName = eventName;
                Phase = phase;
                ProducerThreadId = producerThreadId;
                ProducerOwner = producerOwner;
                Policy = policy;
                CoalesceKey = coalesceKey;
            }

            public string EventName { get; }
            public string Phase { get; }
            public int ProducerThreadId { get; }
            public string ProducerOwner { get; }
            public EventDispatchPolicy Policy { get; }
            public string CoalesceKey { get; }

            public abstract void Execute(EventManager manager);
        }

        private sealed class QueuedEventDispatch<TArgs> : QueuedEventDispatch where TArgs : EventArgs
        {
            private readonly EventSlot<TArgs> slot;
            private readonly TArgs args;
            private readonly EventMembershipSnapshot<TArgs> membership;

            public QueuedEventDispatch(
                EventSlot<TArgs> slot,
                TArgs args,
                EventMembershipSnapshot<TArgs> membership,
                string phase,
                int producerThreadId,
                string producerOwner,
                EventDispatchPolicy policy,
                string coalesceKey)
                : base(slot.EventName, phase, producerThreadId, producerOwner, policy, coalesceKey)
            {
                this.slot = slot;
                this.args = args;
                this.membership = membership;
            }

            public override void Execute(EventManager manager)
            {
                slot.Dispatch(manager, args, membership);
                manager.RecordDispatched(EventName, Phase, Thread.CurrentThread.ManagedThreadId);
            }
        }

        private sealed class TestQueuedEventDispatch : QueuedEventDispatch
        {
            private readonly Action dispatch;

            public TestQueuedEventDispatch(
                string eventName,
                string phase,
                int producerThreadId,
                string producerOwner,
                EventDispatchPolicy policy,
                string coalesceKey,
                Action dispatch)
                : base(eventName, phase, producerThreadId, producerOwner, policy, coalesceKey)
            {
                this.dispatch = dispatch;
            }

            public override void Execute(EventManager manager)
            {
                dispatch();
                manager.RecordDispatched(EventName, Phase, Thread.CurrentThread.ManagedThreadId);
            }
        }

        private sealed class EventQueuePolicyCounters
        {
            public int Pending;
            public long TotalQueued;
            public long TotalCoalesced;
            public long TotalDropped;
            public long TotalOverflow;
            public long TotalRejected;
            public long TotalFlushed;

            public EventQueuePolicySnapshot CreateSnapshot()
            {
                return new EventQueuePolicySnapshot(
                    Pending,
                    TotalQueued,
                    TotalCoalesced,
                    TotalDropped,
                    TotalOverflow,
                    TotalRejected,
                    TotalFlushed);
            }
        }

        private sealed class EventMembershipSnapshot<TArgs> where TArgs : EventArgs
        {
            public static EventMembershipSnapshot<TArgs> Empty { get; } = new EventMembershipSnapshot<TArgs>(0, Array.Empty<OwnedHandler<TArgs>>());

            public EventMembershipSnapshot(long version, OwnedHandler<TArgs>[] handlers)
            {
                Version = Math.Max(0, version);
                Handlers = handlers ?? Array.Empty<OwnedHandler<TArgs>>();
            }

            public long Version { get; }
            public OwnedHandler<TArgs>[] Handlers { get; }
        }

        private sealed class EventOwnerDispatchState
        {
            private int cancelled;

            public bool IsCancelled => Volatile.Read(ref cancelled) != 0;

            public void Cancel()
            {
                Volatile.Write(ref cancelled, 1);
            }
        }

        private sealed class OwnedHandler<TArgs> where TArgs : EventArgs
        {
            private readonly EventOwnerDispatchState ownerDispatchState;
            private int preparedPublicationsCancelled;

            public OwnedHandler(string owner, EventHandler<TArgs> handler, long registrationVersion, EventOwnerDispatchState ownerDispatchState)
            {
                Owner = owner;
                Handler = handler;
                RegistrationVersion = registrationVersion;
                this.ownerDispatchState = ownerDispatchState;
            }

            public string Owner { get; }
            public EventHandler<TArgs> Handler { get; }
            public long RegistrationVersion { get; }
            public int ConsecutiveFailures;
            public long HandlerCalls;
            public long HandlerFailures;

            public bool IsTerminallyCancelled =>
                ownerDispatchState.IsCancelled || Volatile.Read(ref preparedPublicationsCancelled) != 0;

            public void CancelPreparedPublications()
            {
                Volatile.Write(ref preparedPublicationsCancelled, 1);
            }
        }

        private sealed class EventsProxy : IEventsHelper
        {
            public EventsProxy(EventManager events, string owner, Action ensureOwnerActive)
            {
                GameLoop = new GameLoopProxy(events, owner, ensureOwnerActive);
                Input = new InputProxy(events, owner, ensureOwnerActive);
                Save = new SaveProxy(events, owner, ensureOwnerActive);
                UI = new UiProxy(events, owner, ensureOwnerActive);
                Workshop = new WorkshopProxy(events, owner, ensureOwnerActive);
                Diagnostics = new DiagnosticsProxy(events, owner, ensureOwnerActive);
            }

            public IGameLoopEvents GameLoop { get; }
            public IInputEvents Input { get; }
            public ISaveEvents Save { get; }
            public IUiEvents UI { get; }
            public IWorkshopEvents Workshop { get; }
            public IDiagnosticsEvents Diagnostics { get; }
        }

        private sealed class GameLoopProxy : IGameLoopEvents
        {
            private readonly EventManager events;
            private readonly string owner;
            private readonly Action ensure;
            public GameLoopProxy(EventManager events, string owner, Action ensure) { this.events = events; this.owner = owner; this.ensure = ensure; }
            public event EventHandler<GameLaunchedEventArgs>? GameLaunched { add => events.gameLaunched.Add(owner, value, ensure); remove => events.gameLaunched.Remove(owner, value, ensure); }
            public event EventHandler<UpdateTickedEventArgs>? UpdateTicked { add => events.updateTicked.Add(owner, value, ensure); remove => events.updateTicked.Remove(owner, value, ensure); }
            public event EventHandler<OneSecondUpdateTickedEventArgs>? OneSecondUpdateTicked { add => events.oneSecondUpdateTicked.Add(owner, value, ensure); remove => events.oneSecondUpdateTicked.Remove(owner, value, ensure); }
            public event EventHandler<ReturnedToTitleEventArgs>? ReturnedToTitle { add => events.returnedToTitle.Add(owner, value, ensure); remove => events.returnedToTitle.Remove(owner, value, ensure); }
        }

        private sealed class InputProxy : IInputEvents
        {
            private readonly EventManager events;
            private readonly string owner;
            private readonly Action ensure;
            public InputProxy(EventManager events, string owner, Action ensure) { this.events = events; this.owner = owner; this.ensure = ensure; }
            public event EventHandler<ButtonPressedEventArgs>? ButtonPressed { add => events.buttonPressed.Add(owner, value, ensure); remove => events.buttonPressed.Remove(owner, value, ensure); }
            public event EventHandler<ButtonReleasedEventArgs>? ButtonReleased { add => events.buttonReleased.Add(owner, value, ensure); remove => events.buttonReleased.Remove(owner, value, ensure); }
            public event EventHandler<KeybindPressedEventArgs>? KeybindPressed { add => events.keybindPressed.Add(owner, value, ensure); remove => events.keybindPressed.Remove(owner, value, ensure); }
            public event EventHandler<KeybindReleasedEventArgs>? KeybindReleased { add => events.keybindReleased.Add(owner, value, ensure); remove => events.keybindReleased.Remove(owner, value, ensure); }
        }

        private sealed class SaveProxy : ISaveEvents
        {
            private readonly EventManager events;
            private readonly string owner;
            private readonly Action ensure;
            public SaveProxy(EventManager events, string owner, Action ensure) { this.events = events; this.owner = owner; this.ensure = ensure; }
            public event EventHandler<SaveLoadedEventArgs>? SaveLoaded { add => events.saveLoaded.Add(owner, value, ensure); remove => events.saveLoaded.Remove(owner, value, ensure); }
            public event EventHandler<SaveSavingEventArgs>? SaveSaving { add => events.saveSaving.Add(owner, value, ensure); remove => events.saveSaving.Remove(owner, value, ensure); }
            public event EventHandler<SaveSavedEventArgs>? SaveSaved { add => events.saveSaved.Add(owner, value, ensure); remove => events.saveSaved.Remove(owner, value, ensure); }
        }

        private sealed class UiProxy : IUiEvents
        {
            private readonly EventManager events;
            private readonly string owner;
            private readonly Action ensure;
            public UiProxy(EventManager events, string owner, Action ensure) { this.events = events; this.owner = owner; this.ensure = ensure; }
            public event EventHandler<MenuOpenedEventArgs>? MenuOpened { add => events.menuOpened.Add(owner, value, ensure); remove => events.menuOpened.Remove(owner, value, ensure); }
            public event EventHandler<MenuClosedEventArgs>? MenuClosed { add => events.menuClosed.Add(owner, value, ensure); remove => events.menuClosed.Remove(owner, value, ensure); }
        }

        private sealed class WorkshopProxy : IWorkshopEvents
        {
            private readonly EventManager events;
            private readonly string owner;
            private readonly Action ensure;
            public WorkshopProxy(EventManager events, string owner, Action ensure) { this.events = events; this.owner = owner; this.ensure = ensure; }
            public event EventHandler<WorkshopModListChangedEventArgs>? ModListChanged { add => events.workshopModListChanged.Add(owner, value, ensure); remove => events.workshopModListChanged.Remove(owner, value, ensure); }
        }

        private sealed class DiagnosticsProxy : IDiagnosticsEvents
        {
            private readonly EventManager events;
            private readonly string owner;
            private readonly Action ensure;
            public DiagnosticsProxy(EventManager events, string owner, Action ensure) { this.events = events; this.owner = owner; this.ensure = ensure; }
            public event EventHandler<LogExportedEventArgs>? LogExported { add => events.logExported.Add(owner, value, ensure); remove => events.logExported.Remove(owner, value, ensure); }
            public event EventHandler<HookStatusChangedEventArgs>? HookStatusChanged { add => events.hookStatusChanged.Add(owner, value, ensure); remove => events.hookStatusChanged.Remove(owner, value, ensure); }
        }
    }

    internal readonly struct EventDeliveryReceipt
    {
        public static EventDeliveryReceipt Empty { get; } = new EventDeliveryReceipt(Array.Empty<string>());

        public EventDeliveryReceipt(IReadOnlyList<string> ownerIds)
        {
            OwnerIds = ownerIds ?? Array.Empty<string>();
        }

        public IReadOnlyList<string> OwnerIds { get; }

        public bool HasRecipients => OwnerIds.Count > 0;
    }

    internal sealed class EventDispatchBoundarySnapshot
    {
        public EventDispatchBoundarySnapshot(
            long revision,
            int queued,
            int maxQueueDepth,
            int capacity,
            int flushBudget,
            long totalQueued,
            long totalDispatched,
            long totalRejected,
            long totalFlushed,
            long totalCoalesced,
            long totalDropped,
            long totalOverflow,
            long totalQueueDispatchFailures,
            long trimmedWarningKeyCount,
            string lastQueuedEvent,
            string lastRejectedEvent,
            string lastDroppedEvent,
            string lastPhase,
            int lastThreadId,
            IReadOnlyDictionary<EventDispatchPolicy, EventQueuePolicySnapshot>? byPolicy = null)
        {
            Revision = Math.Max(0, revision);
            Queued = queued;
            MaxQueueDepth = maxQueueDepth;
            Capacity = capacity;
            FlushBudget = flushBudget;
            TotalQueued = totalQueued;
            TotalDispatched = totalDispatched;
            TotalRejected = totalRejected;
            TotalFlushed = totalFlushed;
            TotalCoalesced = totalCoalesced;
            TotalDropped = totalDropped;
            TotalOverflow = totalOverflow;
            TotalQueueDispatchFailures = totalQueueDispatchFailures;
            TrimmedWarningKeyCount = trimmedWarningKeyCount;
            LastQueuedEvent = lastQueuedEvent;
            LastRejectedEvent = lastRejectedEvent;
            LastDroppedEvent = lastDroppedEvent;
            LastPhase = lastPhase;
            LastThreadId = lastThreadId;
            ByPolicy = byPolicy ?? new Dictionary<EventDispatchPolicy, EventQueuePolicySnapshot>();
        }

        public long Revision { get; }

        public int Queued { get; }

        public int MaxQueueDepth { get; }

        public int Capacity { get; }

        public int FlushBudget { get; }

        public long TotalQueued { get; }

        public long TotalDispatched { get; }

        public long TotalRejected { get; }

        public long TotalFlushed { get; }

        public long TotalCoalesced { get; }

        public long TotalDropped { get; }

        public long TotalOverflow { get; }

        public long TotalQueueDispatchFailures { get; }

        public long TrimmedWarningKeyCount { get; }

        public string LastQueuedEvent { get; }

        public string LastRejectedEvent { get; }

        public string LastDroppedEvent { get; }

        public string LastPhase { get; }

        public int LastThreadId { get; }

        public IReadOnlyDictionary<EventDispatchPolicy, EventQueuePolicySnapshot> ByPolicy { get; }

        public bool Success => TotalRejected == 0 &&
            TotalDropped == 0 &&
            TotalOverflow == 0 &&
            TotalQueueDispatchFailures == 0 &&
            TrimmedWarningKeyCount == 0;

        public string FormatSummary()
        {
            return "status=" + (Success ? "ok" : "warning") +
                "; queued=" + Queued +
                "; capacity=" + Capacity +
                "; maxDepth=" + MaxQueueDepth +
                "; flushBudget=" + FlushBudget +
                "; totalQueued=" + TotalQueued +
                "; totalFlushed=" + TotalFlushed +
                "; coalesced=" + TotalCoalesced +
                "; dropped=" + TotalDropped +
                "; overflow=" + TotalOverflow +
                "; queueFailures=" + TotalQueueDispatchFailures +
                "; trimmedWarningKeys=" + TrimmedWarningKeyCount +
                "; totalDispatched=" + TotalDispatched +
                "; rejected=" + TotalRejected +
                "; lastQueued=" + SingleLine(LastQueuedEvent) +
                "; lastRejected=" + SingleLine(LastRejectedEvent) +
                "; lastDropped=" + SingleLine(LastDroppedEvent) +
                "; phase=" + SingleLine(LastPhase) +
                "; thread=" + LastThreadId;
        }

        private static string SingleLine(string? value)
        {
            string text = value ?? string.Empty;
            return string.IsNullOrWhiteSpace(text)
                ? "-"
                : text.Replace('\r', ' ').Replace('\n', ' ').Trim();
        }
    }

    internal enum EventDispatchPolicy
    {
        Direct,
        BoundedFifo,
        CoalesceLatestByKey,
        Reject
    }

    internal sealed class EventKernelOptions
    {
        private readonly IReadOnlyDictionary<string, EventDispatchPolicy> policyOverrides;

        public EventKernelOptions(
            int queueCapacity = 512,
            int flushBudget = 128,
            IReadOnlyDictionary<string, EventDispatchPolicy>? policyOverrides = null,
            bool measureHandlerTiming = false)
        {
            if (queueCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(queueCapacity));
            if (flushBudget <= 0)
                throw new ArgumentOutOfRangeException(nameof(flushBudget));

            QueueCapacity = queueCapacity;
            FlushBudget = flushBudget;
            MeasureHandlerTiming = measureHandlerTiming;
            var overrides = new Dictionary<string, EventDispatchPolicy>(StringComparer.OrdinalIgnoreCase);
            if (policyOverrides != null)
            {
                foreach (KeyValuePair<string, EventDispatchPolicy> pair in policyOverrides)
                    overrides[pair.Key] = pair.Value;
            }
            this.policyOverrides = overrides;
        }

        public static EventKernelOptions Default { get; } = new EventKernelOptions();

        public int QueueCapacity { get; }

        public int FlushBudget { get; }

        /// <summary>
        /// Enables two timestamp reads per invoked handler for explicit diagnostics only.
        /// The player-runtime default is false so the normal event hot path pays no clock cost.
        /// </summary>
        public bool MeasureHandlerTiming { get; }

        internal EventDispatchPolicy ResolvePolicy(string eventName, EventDispatchPolicy defaultPolicy)
        {
            return policyOverrides.TryGetValue(eventName ?? string.Empty, out EventDispatchPolicy policy)
                ? policy
                : defaultPolicy;
        }
    }

    internal sealed class EventQueuePolicySnapshot
    {
        public EventQueuePolicySnapshot(
            int pending,
            long totalQueued,
            long totalCoalesced,
            long totalDropped,
            long totalOverflow,
            long totalRejected,
            long totalFlushed)
        {
            Pending = pending;
            TotalQueued = totalQueued;
            TotalCoalesced = totalCoalesced;
            TotalDropped = totalDropped;
            TotalOverflow = totalOverflow;
            TotalRejected = totalRejected;
            TotalFlushed = totalFlushed;
        }

        public int Pending { get; }
        public long TotalQueued { get; }
        public long TotalCoalesced { get; }
        public long TotalDropped { get; }
        public long TotalOverflow { get; }
        public long TotalRejected { get; }
        public long TotalFlushed { get; }
    }

    internal enum EventListenerTransitionReason
    {
        Added,
        Removed,
        OwnerRemoved,
        Quarantined
    }

    internal sealed class EventListenerTransition
    {
        public EventListenerTransition(
            string eventName,
            string owner,
            EventListenerTransitionReason reason,
            int previousCount,
            int currentCount,
            int affectedRegistrations,
            int previousOwnerCount,
            int currentOwnerCount)
        {
            EventName = eventName ?? string.Empty;
            Owner = owner ?? string.Empty;
            Reason = reason;
            PreviousCount = Math.Max(0, previousCount);
            CurrentCount = Math.Max(0, currentCount);
            AffectedRegistrations = Math.Max(0, affectedRegistrations);
            PreviousOwnerCount = Math.Max(0, previousOwnerCount);
            CurrentOwnerCount = Math.Max(0, currentOwnerCount);
            ThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        public string EventName { get; }
        public string Owner { get; }
        public EventListenerTransitionReason Reason { get; }
        public int PreviousCount { get; }
        public int CurrentCount { get; }
        public int AffectedRegistrations { get; }
        public int PreviousOwnerCount { get; }
        public int CurrentOwnerCount { get; }
        public int ThreadId { get; }
        public bool BecameActive => PreviousCount == 0 && CurrentCount > 0;
        public bool BecameInactive => PreviousCount > 0 && CurrentCount == 0;
        public bool OwnerBecameActive => PreviousOwnerCount == 0 && CurrentOwnerCount > 0;
        public bool OwnerBecameInactive => PreviousOwnerCount > 0 && CurrentOwnerCount == 0;
    }

    internal sealed class EventSlotSnapshot
    {
        public EventSlotSnapshot(
            string eventName,
            int activeHandlers,
            int quarantinedHandlers,
            int dispatchableHandlers,
            IReadOnlyDictionary<string, EventOwnerSlotCounts>? byOwner = null,
            long trimmedQuarantineCount = 0,
            long publicationCalls = 0,
            long eventArgsCreated = 0,
            long snapshotRebuilds = 0,
            long handlerCalls = 0,
            long handlerFailures = 0,
            long handlerQuarantines = 0,
            long zeroListenerBypasses = 0,
            int namedOwnerCapacity = 128,
            int totalOwnerCount = 0,
            int trimmedOwnerCount = 0,
            long trimmedActiveHandlers = 0,
            long trimmedQuarantinedHandlers = 0,
            long trimmedHandlerCalls = 0,
            long trimmedHandlerFailures = 0,
            long trimmedHandlerQuarantines = 0,
            bool handlerTimingEnabled = false,
            long handlerTimingSamples = 0,
            long handlerTimingTotalTicks = 0,
            long handlerTimingMaxTicks = 0)
        {
            EventName = eventName;
            ActiveHandlers = activeHandlers;
            QuarantinedHandlers = quarantinedHandlers;
            DispatchableHandlers = dispatchableHandlers;
            ByOwner = byOwner ?? new Dictionary<string, EventOwnerSlotCounts>(StringComparer.OrdinalIgnoreCase);
            TrimmedQuarantineCount = Math.Max(0, trimmedQuarantineCount);
            PublicationCalls = Math.Max(0, publicationCalls);
            EventArgsCreated = Math.Max(0, eventArgsCreated);
            SnapshotRebuilds = Math.Max(0, snapshotRebuilds);
            HandlerCalls = Math.Max(0, handlerCalls);
            HandlerFailures = Math.Max(0, handlerFailures);
            HandlerQuarantines = Math.Max(0, handlerQuarantines);
            ZeroListenerBypasses = Math.Max(0, zeroListenerBypasses);
            NamedOwnerCapacity = Math.Max(1, namedOwnerCapacity);
            TotalOwnerCount = Math.Max(ByOwner.Count, totalOwnerCount);
            TrimmedOwnerCount = Math.Max(0, trimmedOwnerCount);
            TrimmedActiveHandlers = Math.Max(0, trimmedActiveHandlers);
            TrimmedQuarantinedHandlers = Math.Max(0, trimmedQuarantinedHandlers);
            TrimmedHandlerCalls = Math.Max(0, trimmedHandlerCalls);
            TrimmedHandlerFailures = Math.Max(0, trimmedHandlerFailures);
            TrimmedHandlerQuarantines = Math.Max(0, trimmedHandlerQuarantines);
            HandlerTimingEnabled = handlerTimingEnabled;
            HandlerTimingSamples = Math.Max(0, handlerTimingSamples);
            HandlerTimingTotalTicks = Math.Max(0, handlerTimingTotalTicks);
            HandlerTimingMaxTicks = Math.Max(0, handlerTimingMaxTicks);
        }

        public string EventName { get; }
        public int ActiveHandlers { get; }
        public int QuarantinedHandlers { get; }
        public int DispatchableHandlers { get; }
        public IReadOnlyDictionary<string, EventOwnerSlotCounts> ByOwner { get; }
        public long TrimmedQuarantineCount { get; }
        public long PublicationCalls { get; }
        public long EventArgsCreated { get; }
        public long SnapshotRebuilds { get; }
        public long HandlerCalls { get; }
        public long HandlerFailures { get; }
        public long HandlerQuarantines { get; }
        public long ZeroListenerBypasses { get; }
        public int NamedOwnerCapacity { get; }
        public int TotalOwnerCount { get; }
        public int TrimmedOwnerCount { get; }
        public long TrimmedActiveHandlers { get; }
        public long TrimmedQuarantinedHandlers { get; }
        public long TrimmedHandlerCalls { get; }
        public long TrimmedHandlerFailures { get; }
        public long TrimmedHandlerQuarantines { get; }
        public bool HandlerTimingEnabled { get; }
        public long HandlerTimingSamples { get; }
        public long HandlerTimingTotalTicks { get; }
        public long HandlerTimingMaxTicks { get; }
    }

    internal sealed class EventOwnerSlotCounts
    {
        public int ActiveHandlers { get; set; }
        public int QuarantinedHandlers { get; set; }
        public int DispatchableHandlers { get; set; }
        public long HandlerCalls { get; set; }
        public long HandlerFailures { get; set; }
        public long HandlerQuarantines { get; set; }
    }

    internal sealed class EventHandlerCleanupSnapshot
    {
        public EventHandlerCleanupSnapshot(bool quarantineEnabled, IReadOnlyList<EventSlotSnapshot> slots)
        {
            QuarantineEnabled = quarantineEnabled;
            Slots = slots;
        }

        public bool QuarantineEnabled { get; }
        public IReadOnlyList<EventSlotSnapshot> Slots { get; }
        public int ActiveHandlers => Slots.Sum(s => s.ActiveHandlers);
        public int DispatchableHandlers => Slots.Sum(s => s.DispatchableHandlers);
        public int QuarantinedHandlers => Slots.Sum(s => s.QuarantinedHandlers);
        public long TrimmedQuarantineCount => Slots.Sum(s => s.TrimmedQuarantineCount);
        public long PublicationCalls => Slots.Sum(s => s.PublicationCalls);
        public long EventArgsCreated => Slots.Sum(s => s.EventArgsCreated);
        public long SnapshotRebuilds => Slots.Sum(s => s.SnapshotRebuilds);
        public long HandlerCalls => Slots.Sum(s => s.HandlerCalls);
        public long HandlerFailures => Slots.Sum(s => s.HandlerFailures);
        public long HandlerQuarantines => Slots.Sum(s => s.HandlerQuarantines);
        public long ZeroListenerBypasses => Slots.Sum(s => s.ZeroListenerBypasses);
        public int TrimmedOwnerCount => Slots.Sum(s => s.TrimmedOwnerCount);
        public long TrimmedActiveHandlers => Slots.Sum(s => s.TrimmedActiveHandlers);
        public long TrimmedQuarantinedHandlers => Slots.Sum(s => s.TrimmedQuarantinedHandlers);
        public long TrimmedHandlerCalls => Slots.Sum(s => s.TrimmedHandlerCalls);
        public long TrimmedHandlerFailures => Slots.Sum(s => s.TrimmedHandlerFailures);
        public long TrimmedHandlerQuarantines => Slots.Sum(s => s.TrimmedHandlerQuarantines);
        public bool HandlerTimingEnabled => Slots.Any(s => s.HandlerTimingEnabled);
        public long HandlerTimingSamples => Slots.Sum(s => s.HandlerTimingSamples);
        public long HandlerTimingTotalTicks => Slots.Sum(s => s.HandlerTimingTotalTicks);
        public long HandlerTimingMaxTicks => Slots.Count == 0 ? 0 : Slots.Max(s => s.HandlerTimingMaxTicks);
        public bool Success => QuarantinedHandlers >= 0;

        public string FormatSummary()
        {
            return (QuarantineEnabled ? "status=ok" : "status=disabled") +
                "; activeHandlers=" + ActiveHandlers +
                "; dispatchableHandlers=" + DispatchableHandlers +
                "; quarantinedHandlers=" + QuarantinedHandlers +
                "; trimmedQuarantines=" + TrimmedQuarantineCount +
                "; publicationCalls=" + PublicationCalls +
                "; eventArgsCreated=" + EventArgsCreated +
                "; snapshotRebuilds=" + SnapshotRebuilds +
                "; handlerCalls=" + HandlerCalls +
                "; handlerFailures=" + HandlerFailures +
                "; handlerQuarantines=" + HandlerQuarantines +
                "; zeroListenerBypasses=" + ZeroListenerBypasses +
                "; diagnosticOwnersPerSlot=" + (Slots.Count == 0 ? 0 : Slots.Max(slot => slot.NamedOwnerCapacity)) +
                "; trimmedOwners=" + TrimmedOwnerCount +
                "; trimmedActiveHandlers=" + TrimmedActiveHandlers +
                "; trimmedQuarantinedHandlers=" + TrimmedQuarantinedHandlers +
                "; trimmedHandlerCalls=" + TrimmedHandlerCalls +
                "; trimmedHandlerFailures=" + TrimmedHandlerFailures +
                "; trimmedHandlerQuarantines=" + TrimmedHandlerQuarantines +
                "; handlerTimingPolicy=" + (HandlerTimingEnabled ? "explicit-diagnostic" : "disabled-hot-path") +
                "; handlerTimingSamples=" + HandlerTimingSamples +
                "; handlerTimingTotalTicks=" + HandlerTimingTotalTicks +
                "; handlerTimingMaxTicks=" + HandlerTimingMaxTicks +
                "; hotPathQuarantine=" + (QuarantineEnabled ? "true" : "false") +
                "; byOwner={" + FormatByOwner() + "}";
        }

        private string FormatByOwner()
        {
            var byOwner = new Dictionary<string, EventOwnerSlotCounts>(StringComparer.OrdinalIgnoreCase);
            foreach (EventSlotSnapshot slot in Slots)
            {
                foreach (KeyValuePair<string, EventOwnerSlotCounts> pair in slot.ByOwner)
                {
                    EventOwnerSlotCounts existing = byOwner.TryGetValue(pair.Key, out EventOwnerSlotCounts counts)
                        ? counts
                        : new EventOwnerSlotCounts();
                    existing.ActiveHandlers += pair.Value.ActiveHandlers;
                    existing.DispatchableHandlers += pair.Value.DispatchableHandlers;
                    existing.QuarantinedHandlers += pair.Value.QuarantinedHandlers;
                    existing.HandlerCalls += pair.Value.HandlerCalls;
                    existing.HandlerFailures += pair.Value.HandlerFailures;
                    existing.HandlerQuarantines += pair.Value.HandlerQuarantines;
                    byOwner[pair.Key] = existing;
                }
            }

            string value = string.Join("; ", byOwner
                .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .Take(32)
                .Select(pair => SanitizeMetricKey(pair.Key) +
                    "={activeHandlers=" + pair.Value.ActiveHandlers.ToString(CultureInfo.InvariantCulture) +
                    "; dispatchableHandlers=" + pair.Value.DispatchableHandlers.ToString(CultureInfo.InvariantCulture) +
                    "; quarantinedHandlers=" + pair.Value.QuarantinedHandlers.ToString(CultureInfo.InvariantCulture) +
                    "; handlerCalls=" + pair.Value.HandlerCalls.ToString(CultureInfo.InvariantCulture) +
                    "; handlerFailures=" + pair.Value.HandlerFailures.ToString(CultureInfo.InvariantCulture) +
                    "; handlerQuarantines=" + pair.Value.HandlerQuarantines.ToString(CultureInfo.InvariantCulture) + "}"));
            return string.IsNullOrWhiteSpace(value) ? "none" : value;
        }

        private static string SanitizeMetricKey(string value)
        {
            string text = string.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim();
            var builder = new System.Text.StringBuilder(text.Length);
            bool previousUnderscore = false;
            foreach (char ch in text)
            {
                if (char.IsLetterOrDigit(ch) || ch == '_' || ch == '-' || ch == '.')
                {
                    builder.Append(ch);
                    previousUnderscore = false;
                    continue;
                }

                if (!previousUnderscore)
                {
                    builder.Append('_');
                    previousUnderscore = true;
                }
            }

            return builder.ToString().Trim('_');
        }
    }
}
