using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using global::DTMAPI.Abstractions;

namespace DTMAPI.MoreEquipmentSlots
{
    public sealed class MoreEquipmentSlotsDiagnosticsSnapshot
    {
        public int SlotCount { get; internal set; }

        public int PatchCount { get; internal set; }

        public int CloneCount { get; internal set; }

        public int ListenerCount { get; internal set; }

        public int FunctionCount { get; internal set; }

        public int CallbackCount { get; internal set; }

        public int RootCount { get; internal set; }

        public string JournalPhase { get; internal set; } =
            "None";

        public string LogicalOwner { get; internal set; } =
            MoreEquipmentSlotsProductContract.UniqueId;

        public string LastPlacement { get; internal set; } =
            NativePlacementKind.Failure.ToString();

        public string LifecycleSummary { get; internal set; } =
            string.Empty;
    }

    internal sealed class MoreEquipmentSlotsNativeRuntime
    {
        private const BindingFlags AllMembers =
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.Instance |
            BindingFlags.Static;
        private readonly IMonitor monitor;
        private readonly string configRoot;
        private readonly MoreEquipmentSlotsHookInstaller hooks;
        private readonly EquipmentSlotDocumentStore store =
            new EquipmentSlotDocumentStore();
        private readonly EquipmentSlotNativePlacement placement =
            new EquipmentSlotNativePlacement(
                new DolocTownItemPlacementGateway());
        private readonly List<NativeFunctionLease> functionLeases =
            new List<NativeFunctionLease>();
        private readonly List<UiLease> uiLeases =
            new List<UiLease>();
        private readonly HashSet<int> invalidTraitSlots =
            new HashSet<int>();
        private readonly Dictionary<
            int,
            EquipmentSlotNativeMutationOutcomeUnknownException>
            pendingJournalPlacements =
                new Dictionary<
                    int,
                    EquipmentSlotNativeMutationOutcomeUnknownException>();
        private MoreEquipmentSlotsConfig config =
            new MoreEquipmentSlotsConfig();
        private EquipmentSlotStorageDocument? document;
        private List<EquipmentSlotStorageEntry>? workingSlots;
        private EquipmentSlotSaveScope? scope;
        private string sidecarPath = string.Empty;
        private int archiveIndex = -1;
        private bool pendingNativeSave;
        private bool journalRecoveryBlocked;
        private bool workingDirty;
        private PendingGameplayPlacement?
            pendingGameplayPlacement;
        private bool deactivated;
        private bool suppressNativeReloadReapply;
        private NativePlacementKind lastPlacement =
            NativePlacementKind.Failure;
        private string lastMessage = "not-started";

        internal MoreEquipmentSlotsNativeRuntime(
            IMonitor monitor,
            string configPath,
            Func<int, bool>? hookInstallGate = null)
        {
            this.monitor = monitor ??
                throw new ArgumentNullException(nameof(monitor));
            configRoot =
                Path.GetDirectoryName(configPath) ??
                throw new ArgumentException(
                    "The product config path has no directory.",
                    nameof(configPath));
            hooks = new MoreEquipmentSlotsHookInstaller(
                monitor,
                hookInstallGate);
        }

        internal int InstalledPatchCount =>
            hooks.InstalledPatchCount;

        internal string StatusSummary =>
            "enabled=" +
            config.Enabled +
            ";slotCount=" +
            MoreEquipmentSlotsProductContract.FixedSlotCount +
            ";patches=" +
            InstalledPatchCount +
            ";journal=" +
            GetJournalPhase() +
            ";lastPlacement=" +
            lastPlacement +
            ";message=" +
            lastMessage;

        internal void Configure(
            MoreEquipmentSlotsConfig next,
            string reason)
        {
            if (deactivated)
            {
                throw new ObjectDisposedException(
                    nameof(MoreEquipmentSlotsNativeRuntime));
            }
            MoreEquipmentSlotsConfig requested =
                CloneConfig(next);
            bool wasEnabled = config.Enabled;
            if (requested.Enabled)
            {
                config = requested;
                hooks.InstallAtomically(this);
                ApplyStoredFunctions("configure " + reason);
            }
            else
            {
                PrepareConfiguration(
                    requested,
                    reason);
                var failures = new List<Exception>();
                try
                {
                    ClearNativeAndUi(
                        "configuration disabled " + reason);
                }
                catch (Exception ex)
                {
                    failures.Add(ex);
                }
                try
                {
                    hooks.UnpatchOwnedHooks(this);
                }
                catch (Exception ex)
                {
                    failures.Add(ex);
                }
                if (failures.Count > 0)
                {
                    TryRestoreEnabledRuntimeAfterConfigurationFailure(
                        wasEnabled,
                        failures);
                    throw new AggregateException(
                        "MoreEquipmentSlots configuration disable was incomplete.",
                        failures);
                }
                try
                {
                    StageAllOccupiedForRecovery(
                        "configuration disabled " + reason);
                }
                catch (Exception stagingFailure)
                {
                    failures.Add(stagingFailure);
                    TryRestoreEnabledRuntimeAfterConfigurationFailure(
                        wasEnabled,
                        failures);
                    throw new AggregateException(
                        "MoreEquipmentSlots configuration disable could not stage committed owner recovery.",
                        failures);
                }
                config = requested;
            }
            PublishLifecycle();
        }

        internal void PrepareConfiguration(
            MoreEquipmentSlotsConfig next,
            string reason)
        {
            MoreEquipmentSlotsConfig requested =
                CloneConfig(next);
            if (!requested.Enabled)
            {
                ValidateOwnerDeactivation(
                    "configuration disabled " + reason);
            }
        }

        internal void OnSaveLoaded(int? slot)
        {
            ResetTransientRoots("SaveLoaded");
            if (!slot.HasValue || slot.Value < 0)
            {
                archiveIndex = -1;
                document = null;
                scope = null;
                sidecarPath = string.Empty;
                lastMessage = "SaveLoaded without archive.";
                return;
            }

            archiveIndex = slot.Value;
            scope =
                DolocTownItemPlacementGateway.ReadCurrentScope(
                    archiveIndex);
            sidecarPath = BuildSidecarPath(archiveIndex);
            string migrationMessage;
            try
            {
                document = store.LoadOrMigrate(
                    sidecarPath,
                    BuildLegacyGlobalSidecarPath(),
                    scope,
                    out migrationMessage);
            }
            catch (Exception ex)
            {
                monitor.Log(
                    "MoreEquipmentSlots storage load failed closed " +
                    "archive=" +
                    archiveIndex.ToString(
                        CultureInfo.InvariantCulture) +
                    "; playerIdentityPresent=" +
                    (!string.IsNullOrWhiteSpace(
                        scope.CustomPlayerName) ||
                     !string.IsNullOrWhiteSpace(
                        scope.PlayerName)) +
                    "; saveClock=" +
                    (scope.TotalGameSeconds.HasValue
                        ? scope.TotalGameSeconds.Value.ToString(
                            CultureInfo.InvariantCulture)
                        : "missing") +
                    "; sidecar=" +
                    sidecarPath +
                    "; error=" +
                    ex,
                    LogLevel.Error);
                throw;
            }
            if (!string.IsNullOrWhiteSpace(migrationMessage))
            {
                monitor.Log(
                    "MoreEquipmentSlots storage migration: " +
                    migrationMessage);
            }
            invalidTraitSlots.Clear();
            pendingGameplayPlacement = null;
            pendingJournalPlacements.Clear();
            journalRecoveryBlocked = false;
            RecoverDurableJournal("SaveLoaded");
            RecoverGameplayCandidate("SaveLoaded");
            workingSlots =
                EquipmentSlotGameplayCandidateCoordinator
                    .CloneSlots(document.Slots);
            workingDirty = false;
            RehydrateWorkingTraits("SaveLoaded");
            if (config.Enabled)
                ApplyStoredFunctions("SaveLoaded");
            lastMessage =
                "SaveLoaded archive=" + archiveIndex + ".";
            PublishLifecycle();
        }

        internal void OnSaveSaving(int? slot)
        {
            if (!MatchesLoadedSlot(slot) ||
                document == null)
            {
                return;
            }
            ReconcilePendingGameplayPlacement(
                "SaveSaving");
            ReconcilePendingJournalPlacements(
                "SaveSaving");
            if (document.GameplayCandidate != null)
            {
                RecoverGameplayCandidate("SaveSaving reentry");
                if (document.GameplayCandidate != null ||
                    journalRecoveryBlocked)
                {
                    throw new InvalidOperationException(
                        "MoreEquipmentSlots refused native SaveGame because an ambiguous gameplay candidate remains fail-closed.");
                }
            }
            if (workingDirty &&
                document.GameplayCandidate == null &&
                document.Journal == null)
            {
                string gameplayPreFingerprint =
                    DolocTownItemPlacementGateway
                        .GetNativeSaveFingerprint(archiveIndex);
                EquipmentSlotGameplayCandidateCoordinator
                    .Prepare(
                        document,
                        RequireWorkingSlots(),
                        gameplayPreFingerprint,
                        itemId => Observe(
                            itemId,
                            gameplayPreFingerprint));
                PersistDocument(
                    "SaveSaving gameplay candidate");
                pendingNativeSave = true;
                PublishLifecycle();
                return;
            }
            if (document.Journal == null)
                return;
            if (journalRecoveryBlocked)
            {
                pendingNativeSave = false;
                monitor.Log(
                    "MoreEquipmentSlots retained an ambiguous durable journal fail-closed; SaveSaving will not replay or promote it in this session.",
                    LogLevel.Error);
                throw new InvalidOperationException(
                    "MoreEquipmentSlots refused native SaveGame because an ambiguous durable journal remains fail-closed.");
            }

            EquipmentSlotTransactionJournal journal =
                document.Journal;
            string preFingerprint =
                DolocTownItemPlacementGateway
                    .GetNativeSaveFingerprint(archiveIndex);
            if (!journal.AttemptStarted)
            {
                EquipmentSlotTransactionCoordinator.StartAttempt(
                    document,
                    preFingerprint,
                    itemId => Observe(itemId, preFingerprint));
                PersistDocument("SaveSaving prepared");
            }

            for (int index = 0;
                 index < journal.Escrow.Count;
                 index++)
            {
                EquipmentSlotEscrowEntry escrow =
                    journal.Escrow[index];
                if (escrow.AttemptCompleted)
                    continue;
                NativePlacementResult result;
                try
                {
                    result =
                        placement.PlaceOne(escrow.ItemId);
                }
                catch (
                    EquipmentSlotNativeMutationOutcomeUnknownException
                        ex)
                {
                    pendingJournalPlacements[index] = ex;
                    journalRecoveryBlocked = true;
                    pendingNativeSave = false;
                    monitor.Log(
                        "MoreEquipmentSlots retained durable escrow after a native placement outcome became unknown; SaveGame is blocked until exact evidence reconciles item=" +
                        escrow.ItemId +
                        " transaction=" +
                        journal.TransactionId +
                        ".",
                        LogLevel.Error);
                    PublishLifecycle();
                    throw;
                }
                NativeRecoveryObservation after =
                    ObservationAfterPlacement(
                        preFingerprint,
                        escrow,
                        result);
                EquipmentSlotTransactionCoordinator
                    .RecordPlacement(
                        journal,
                        index,
                        result,
                        after);
                lastPlacement = result.Kind;
                monitor.Log(
                    "MoreEquipmentSlots SaveSaving placement outcome=" +
                    result.Kind +
                    " item=" +
                    escrow.ItemId +
                    " transaction=" +
                    journal.TransactionId +
                    " " +
                    result.Message);
            }

            if (journal.Replacements.Count > 0 &&
                !journal.IncomingAttemptCompleted)
            {
                string incomingItemId =
                    journal.Replacements[0].ItemId;
                bool fromNativeBuffer = false;
                bool withdrawn = false;
                if (EquipmentSlotTransactionCoordinator
                    .CanWithdrawIncoming(journal))
                {
                    bool bufferAttempted =
                        TryTakeMatchingNativeBuffer(
                            incomingItemId,
                            out bool bufferSucceeded);
                    fromNativeBuffer = bufferAttempted;
                    withdrawn =
                        bufferAttempted
                            ? bufferSucceeded
                            : placement.TryWithdrawOne(
                                incomingItemId);
                }
                NativeRecoveryObservation afterIncoming =
                    Observe(incomingItemId, preFingerprint);
                int sameItemOutgoingBackpackCount = 0;
                foreach (EquipmentSlotEscrowEntry escrow in
                    journal.Escrow)
                {
                    if (string.Equals(
                            escrow.ItemId,
                            incomingItemId,
                            StringComparison.Ordinal) &&
                        escrow.AttemptCompleted &&
                        escrow.Placement ==
                            NativePlacementKind.Backpack)
                    {
                        sameItemOutgoingBackpackCount++;
                    }
                }
                int expectedBackpackCount =
                    journal.IncomingBeforeBackpackCount +
                    sameItemOutgoingBackpackCount +
                    (fromNativeBuffer ? 0 : -1);
                bool exactWithdrawal =
                    withdrawn &&
                    afterIncoming.BackpackCount ==
                        Math.Max(0, expectedBackpackCount);
                EquipmentSlotTransactionCoordinator
                    .RecordIncomingWithdrawal(
                        journal,
                        exactWithdrawal,
                        afterIncoming.BackpackCount,
                        fromNativeBuffer);
            }

            // This second durable prepared write records exact per-item
            // destinations before the synchronous native SaveGame continues.
            PersistDocument("SaveSaving placement outcomes");
            pendingNativeSave = true;
            PublishLifecycle();
        }

        internal void OnSaveSaved(int? slot)
        {
            if (!pendingNativeSave ||
                !MatchesLoadedSlot(slot) ||
                document == null)
            {
                return;
            }

            AdvanceCommittedScopeAfterNativeSave();
            string postFingerprint =
                DolocTownItemPlacementGateway
                    .GetNativeSaveFingerprint(archiveIndex);
            if (document.GameplayCandidate != null)
            {
                EquipmentSlotGameplayCandidateCoordinator
                    .PromoteCommitted(
                        document,
                        postFingerprint,
                        nativeSaveConfirmed: true);
                PersistDocument(
                    "SaveSaved gameplay committed tombstone");
                EquipmentSlotGameplayCandidateCoordinator
                    .FinalizeCommitted(document);
                PersistDocument(
                    "SaveSaved gameplay cleanup");
                workingSlots =
                    EquipmentSlotGameplayCandidateCoordinator
                        .CloneSlots(document.Slots);
                workingDirty = false;
                pendingNativeSave = false;
                journalRecoveryBlocked = false;
                if (config.Enabled)
                    ApplyStoredFunctions("SaveSaved");
                PublishLifecycle();
                return;
            }
            if (document.Journal == null)
                return;
            EquipmentSlotTransactionJournal journal =
                document.Journal;
            EquipmentSlotTransactionCoordinator
                .PromoteCommittedTombstone(
                    document,
                    postFingerprint);
            PersistDocument("SaveSaved committed tombstone");
            monitor.Log(
                "MoreEquipmentSlots SaveSaved journal promotion transaction=" +
                journal.TransactionId +
                " generation=" +
                document.Generation +
                ".");
            LogCommittedDestinations(journal);
            EquipmentSlotTransactionCoordinator
                .FinalizeCommitted(document);
            PersistDocument("SaveSaved tombstone finalized");
            workingSlots =
                EquipmentSlotGameplayCandidateCoordinator
                    .CloneSlots(document.Slots);
            workingDirty = false;
            pendingNativeSave = false;
            journalRecoveryBlocked = false;
            if (config.Enabled)
                ApplyStoredFunctions("SaveSaved");
            PublishLifecycle();
        }

        private void AdvanceCommittedScopeAfterNativeSave()
        {
            if (document == null)
                throw new InvalidOperationException(
                    "A Product document is required after native save.");
            EquipmentSlotSaveScope current =
                DolocTownItemPlacementGateway.ReadCurrentScope(
                    archiveIndex);
            if (current.ArchiveIndex !=
                    document.Scope.ArchiveIndex ||
                !string.Equals(
                    current.PlayerName,
                    document.Scope.PlayerName,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    current.CustomPlayerName,
                    document.Scope.CustomPlayerName,
                    StringComparison.Ordinal) ||
                !current.TotalGameSeconds.HasValue)
            {
                throw new InvalidOperationException(
                    "MoreEquipmentSlots refused to advance its committed save revision because the post-save native identity was incomplete or changed.");
            }

            document.Scope.TotalGameSeconds =
                current.TotalGameSeconds;
            if (document.Journal != null)
            {
                document.Journal.Scope =
                    document.Scope.Clone();
            }
            if (document.GameplayCandidate != null)
            {
                document.GameplayCandidate.Scope =
                    document.Scope.Clone();
            }
            scope = current;
        }

        internal bool EquipFromBackpack(
            string itemId,
            int slotIndex)
        {
            if (!config.Enabled ||
                document == null ||
                document.Journal != null ||
                document.GameplayCandidate != null ||
                pendingJournalPlacements.Count > 0 ||
                string.IsNullOrWhiteSpace(itemId))
            {
                return false;
            }
            if (pendingGameplayPlacement != null)
            {
                try
                {
                    ReconcilePendingGameplayPlacement(
                        "EquipFromBackpack retry");
                }
                catch (Exception ex)
                {
                    lastMessage =
                        "Replacement remains blocked because native placement evidence is unreadable or ambiguous: " +
                        ex.Message;
                }
                return false;
            }

            EquipmentSlotStorageEntry replacement =
                BuildStorageEntry(itemId.Trim(), slotIndex);
            EquipmentSlotStorageEntry slot =
                RequireSlot(slotIndex);
            bool outgoingReleased = false;
            if (slot.IsOccupied)
            {
                string fingerprint =
                    DolocTownItemPlacementGateway
                        .GetNativeSaveFingerprint(archiveIndex);
                NativePlacementResult outgoing;
                try
                {
                    outgoing =
                        placement.PlaceOne(slot.ItemId);
                }
                catch (
                    EquipmentSlotNativeMutationOutcomeUnknownException
                        ex)
                {
                    pendingGameplayPlacement =
                        new PendingGameplayPlacement(
                            slotIndex,
                            slot.ItemId,
                            fingerprint,
                            ex);
                    lastMessage =
                        "Replacement paused because the outgoing native placement outcome is unknown; retry is blocked until exact evidence reconciles it.";
                    monitor.Log(
                        "MoreEquipmentSlots gameplay placement outcome unknown item=" +
                        slot.ItemId +
                        " slot=" +
                        slotIndex +
                        ".",
                        LogLevel.Error);
                    return false;
                }
                catch (InvalidDataException ex)
                {
                    lastMessage =
                        "Replacement stopped before native mutation because backpack/mail preflight evidence was unreadable: " +
                        ex.Message;
                    monitor.Log(
                        "MoreEquipmentSlots gameplay placement preflight failed item=" +
                        slot.ItemId +
                        " slot=" +
                        slotIndex +
                        " error=" +
                        ex.Message,
                        LogLevel.Error);
                    return false;
                }
                lastPlacement = outgoing.Kind;
                if (!outgoing.Success)
                {
                    lastMessage =
                        "Replacement refused because the outgoing item has no exact native destination.";
                    return false;
                }
                outgoingReleased = true;
                slot.Clear();
            }

            bool bufferAttempted =
                TryTakeMatchingNativeBuffer(
                    replacement.ItemId,
                    out bool bufferSucceeded);
            bool withdrawn =
                bufferAttempted
                    ? bufferSucceeded
                    : placement.TryWithdrawOne(
                        replacement.ItemId);
            if (!withdrawn)
            {
                if (outgoingReleased)
                {
                    MarkWorkingDirty(
                        "replacement incoming item remained in native storage");
                    ClearFunctions(
                        "EquipFromBackpack incoming unavailable");
                    ApplyStoredFunctions(
                        "EquipFromBackpack incoming unavailable");
                }
                lastMessage =
                    "Incoming item could not be withdrawn; any outgoing item remains in its exact native destination.";
                return false;
            }

            RequireWorkingSlots()[slotIndex] =
                CloneStorageEntry(replacement);
            MarkWorkingDirty("EquipFromBackpack");
            ClearFunctions("EquipFromBackpack");
            ApplyStoredFunctions("EquipFromBackpack");
            lastMessage =
                "Working equipment changed; committed sidecar waits for SaveSaved.";
            return true;
        }

        internal bool RequestUnequip(int slotIndex)
        {
            if (document == null ||
                document.Journal != null ||
                document.GameplayCandidate != null ||
                pendingJournalPlacements.Count > 0)
            {
                return false;
            }
            if (pendingGameplayPlacement != null)
            {
                try
                {
                    ReconcilePendingGameplayPlacement(
                        "RequestUnequip retry");
                }
                catch (Exception ex)
                {
                    lastMessage =
                        "Unequip remains blocked because native placement evidence is unreadable or ambiguous: " +
                        ex.Message;
                }
                return false;
            }
            EquipmentSlotStorageEntry slot =
                RequireSlot(slotIndex);
            if (!slot.IsOccupied)
                return true;

            string fingerprint =
                DolocTownItemPlacementGateway
                    .GetNativeSaveFingerprint(archiveIndex);
            NativePlacementResult result;
            try
            {
                result =
                    placement.PlaceOne(slot.ItemId);
            }
            catch (
                EquipmentSlotNativeMutationOutcomeUnknownException
                    ex)
            {
                pendingGameplayPlacement =
                    new PendingGameplayPlacement(
                        slotIndex,
                        slot.ItemId,
                        fingerprint,
                        ex);
                lastMessage =
                    "Unequip retained its Working slot and blocked retry because the native placement outcome is unknown.";
                monitor.Log(
                    "MoreEquipmentSlots gameplay placement outcome unknown item=" +
                    slot.ItemId +
                    " slot=" +
                    slotIndex +
                    ".",
                    LogLevel.Error);
                return false;
            }
            catch (InvalidDataException ex)
            {
                lastMessage =
                    "Unequip stopped before native mutation because backpack/mail preflight evidence was unreadable: " +
                    ex.Message;
                monitor.Log(
                    "MoreEquipmentSlots gameplay placement preflight failed item=" +
                    slot.ItemId +
                    " slot=" +
                    slotIndex +
                    " error=" +
                    ex.Message,
                    LogLevel.Error);
                return false;
            }
            lastPlacement = result.Kind;
            if (!result.Success)
            {
                lastMessage =
                    "Unequip retained its Working slot because no exact native destination accepted the item.";
                return false;
            }
            slot.Clear();
            MarkWorkingDirty("RequestUnequip");
            ClearFunctions("RequestUnequip");
            ApplyStoredFunctions("RequestUnequip");
            lastMessage =
                "Working equipment changed; committed sidecar waits for SaveSaved.";
            return true;
        }

        internal string GetSlotItemId(int slotIndex) =>
            document == null
                ? string.Empty
                : RequireSlot(slotIndex).ItemId;

        internal string GetJournalPhase() =>
            document?.GameplayCandidate != null
                ? "Gameplay." +
                    document.GameplayCandidate.State
                : document?.Journal == null
                    ? "None"
                    : document.Journal.Origin +
                        "." +
                        document.Journal.State;

        internal MoreEquipmentSlotsDiagnosticsSnapshot
            GetDiagnosticsSnapshot()
        {
            var snapshot =
                new MoreEquipmentSlotsDiagnosticsSnapshot
                {
                    SlotCount =
                        MoreEquipmentSlotsProductContract
                            .FixedSlotCount,
                    PatchCount = InstalledPatchCount,
                    CloneCount = uiLeases.Count,
                    ListenerCount =
                        CountUiListeners(),
                    FunctionCount = functionLeases.Count,
                    CallbackCount =
                        hooks.IsInstalled ? 1 : 0,
                    RootCount = uiLeases.Count > 0 ? 1 : 0,
                    JournalPhase = GetJournalPhase(),
                    LastPlacement = lastPlacement.ToString()
                };
            snapshot.LifecycleSummary =
                BuildLifecycleSummary();
            return snapshot;
        }

        internal void ReturnedToTitle()
        {
            Exception? cleanupFailure = null;
            try
            {
                RecoverGameplayCandidate(
                    "ReturnedToTitle");
                ResetTransientRoots("ReturnedToTitle");
            }
            catch (Exception ex)
            {
                cleanupFailure = ex;
            }
            finally
            {
                archiveIndex = -1;
                document = null;
                workingSlots = null;
                scope = null;
                sidecarPath = string.Empty;
                pendingNativeSave = false;
                journalRecoveryBlocked = false;
                workingDirty = false;
                pendingGameplayPlacement = null;
                pendingJournalPlacements.Clear();
                invalidTraitSlots.Clear();
            }
            lastMessage = "ReturnedToTitle";
            monitor.Log(
                "MoreEquipmentSlots title cleanup " +
                BuildLifecycleSummary());
            if (cleanupFailure != null)
                throw cleanupFailure;
        }

        internal void DeactivateOwner(string reason)
        {
            if (deactivated)
                return;
            PrepareOwnerDeactivation(reason);
            var failures = new List<Exception>();
            try
            {
                ClearNativeAndUi(
                    "owner deactivation " + reason);
            }
            catch (Exception ex)
            {
                failures.Add(ex);
            }
            try
            {
                hooks.UnpatchOwnedHooks(this);
            }
            catch (Exception ex)
            {
                failures.Add(ex);
            }
            if (failures.Count > 0)
            {
                MoreEquipmentSlotsCallbacks
                    .PublishLifecycleSummary(
                        BuildLifecycleSummary());
                throw new AggregateException(
                    "MoreEquipmentSlots owner deactivation was incomplete.",
                    failures);
            }

            archiveIndex = -1;
            document = null;
            workingSlots = null;
            scope = null;
            sidecarPath = string.Empty;
            pendingNativeSave = false;
            journalRecoveryBlocked = false;
            workingDirty = false;
            pendingGameplayPlacement = null;
            pendingJournalPlacements.Clear();
            invalidTraitSlots.Clear();
            deactivated = true;
            string summary = BuildLifecycleSummary(
                callbacksOverride: 0,
                hooksOverride: 0);
            MoreEquipmentSlotsCallbacks
                .PublishLifecycleSummary(summary);
            monitor.Log(
                "MoreEquipmentSlots owner deactivation " +
                summary +
                " reason=" +
                reason +
                ".");
        }

        internal void PrepareOwnerDeactivation(
            string reason)
        {
            if (deactivated ||
                string.Equals(
                    reason,
                    "RuntimeShutdown",
                    StringComparison.Ordinal))
            {
                return;
            }

            RecoverGameplayCandidate(
                "owner deactivation " + reason);
            ValidateOwnerDeactivation(reason);
            StageAllOccupiedForRecovery(
                "owner deactivation " + reason);
        }

        internal void ApplyAfterNativeReload(object manager)
        {
            if (!config.Enabled ||
                suppressNativeReloadReapply ||
                manager == null)
                return;
            int defense = 0;
            if (workingSlots != null)
            {
                foreach (EquipmentSlotStorageEntry slot in
                    workingSlots)
                {
                    if (!invalidTraitSlots.Contains(slot.Index))
                    {
                        defense += Math.Max(
                            0,
                            slot.DefenseBonus);
                    }
                }
            }
            if (defense > 0)
                TryApplyDefense(manager, defense);
        }

        internal bool HandleAttackPrefix(
            object body,
            float attack,
            bool criticalRate,
            object position,
            ref bool isDead,
            ref bool result)
        {
            if (!config.Enabled ||
                body == null ||
                ReadBool(body, "IsFaint"))
            {
                return true;
            }
            EquipmentSlotStorageEntry? shield =
                FindTailShield();
            if (!MoreEquipmentSlotsEffectPolicy
                .ProductShieldMayHandleAttack(
                    NativeManagerHasShield(),
                    shield != null))
                return true;
            EquipmentSlotStorageEntry activeShield = shield!;

            int damage =
                CalculateNativeDamage(
                    body,
                    attack,
                    criticalRate);
            int damageAfterShieldDefend =
                Math.Max(
                    0,
                    damage - activeShield.ShieldDefend);
            int blocked = 0;
            bool fullyBlocked =
                damageAfterShieldDefend <= 0;
            bool shieldBroken = false;
            if (!fullyBlocked)
            {
                blocked =
                    Math.Min(
                        Math.Max(
                            0,
                            activeShield.ShieldValue),
                        damageAfterShieldDefend);
                activeShield.ShieldValue =
                    Math.Max(
                        0,
                        activeShield.ShieldValue -
                            blocked);
                fullyBlocked =
                    activeShield.ShieldValue > 0;
                shieldBroken = !fullyBlocked;
            }
            int residual =
                fullyBlocked
                    ? 0
                    : Math.Max(0, damage - blocked);
            if (shieldBroken)
                activeShield.Clear();
            bool shieldStateChanged = blocked > 0;
            if (shieldStateChanged)
            {
                MarkWorkingDirty("shield state changed");
            }
            if (shieldBroken)
                RefreshNativeParametersAfterShieldBreak();

            ApplyNativeAttackTail(
                body,
                residual,
                position,
                ref isDead,
                fullyBlocked);
            result = true;
            if (shieldStateChanged)
                RenderCurrentUi("shield state changed");
            return false;
        }

        internal void RenderAccessoriesBar(
            object accessoriesBar,
            string reason)
        {
            if (!config.Enabled ||
                document == null ||
                accessoriesBar == null)
            {
                return;
            }
            ClearUi("rebuild " + reason);
            object? sourceSlot =
                Read(accessoriesBar, "passiveItem2") ??
                Read(accessoriesBar, "passiveItem1") ??
                Read(accessoriesBar, "positiveItem");
            object? sourceTransform =
                Read(Read(sourceSlot, "gameObject"), "transform");
            object? parent = Read(sourceTransform, "parent");
            if (sourceSlot == null || parent == null)
                return;

            for (int index = 0;
                 index <
                    MoreEquipmentSlotsProductContract
                        .FixedSlotCount;
                 index++)
            {
                object? clone = CloneUnityObject(
                    sourceSlot,
                    parent);
                if (clone == null)
                    break;
                SetMember(
                    Read(clone, "gameObject"),
                    "name",
                    "DTMAPI.MoreEquipmentSlots." + index);
                var lease = new UiLease(clone);
                BindUiLease(lease, index);
                RenderSlot(clone, RequireSlot(index));
                uiLeases.Add(lease);
            }

            monitor.Log(
                "MoreEquipmentSlots UI rendered reason=" +
                reason +
                " clones=" +
                uiLeases.Count +
                " listeners=" +
                CountUiListeners() +
                ".");
            PublishLifecycle();
        }

        internal string BuildLifecycleSummary(
            int? callbacksOverride = null,
            int? hooksOverride = null) =>
            "clones=" +
            uiLeases.Count +
            ";listeners=" +
            CountUiListeners() +
            ";functions=" +
            functionLeases.Count +
            ";callbacks=" +
            (callbacksOverride ??
             (hooks.IsInstalled ? 1 : 0)) +
            ";hooks=" +
            (hooksOverride ?? InstalledPatchCount) +
            ";roots=" +
            (uiLeases.Count > 0 ? 1 : 0);

        private void StageAllOccupiedForRecovery(string reason)
        {
            if (document == null ||
                document.Journal != null ||
                document.GameplayCandidate != null)
            {
                return;
            }
            if (workingDirty)
            {
                throw new InvalidOperationException(
                    "OwnerRecovery cannot consume uncommitted gameplay state; save it normally or discard it at title/restart first.");
            }
            var occupied = new List<int>();
            foreach (EquipmentSlotStorageEntry slot in document.Slots)
            {
                if (slot.IsOccupied)
                    occupied.Add(slot.Index);
            }
            if (occupied.Count == 0)
                return;
            EquipmentSlotStorageDocument staged =
                new EquipmentSlotStorageDocument
                {
                    SchemaVersion = document.SchemaVersion,
                    Scope = document.Scope.Clone(),
                    Generation = document.Generation,
                    Slots =
                        EquipmentSlotGameplayCandidateCoordinator
                            .CloneSlots(document.Slots)
                };
            EquipmentSlotTransactionCoordinator.PrepareRecovery(
                staged,
                occupied,
                EquipmentSlotTransactionOrigin.OwnerRecovery);
            long previousGeneration = staged.Generation;
            staged.Generation++;
            try
            {
                store.WriteAtomic(sidecarPath, staged);
            }
            catch
            {
                staged.Generation = previousGeneration;
                throw;
            }
            document = staged;
            workingSlots =
                EquipmentSlotGameplayCandidateCoordinator
                    .CloneSlots(staged.Slots);
            invalidTraitSlots.Clear();
            journalRecoveryBlocked = false;
            lastMessage =
                reason +
                " generation=" +
                document.Generation;
            workingDirty = false;
        }

        private void ValidateOwnerDeactivation(string reason)
        {
            if (workingDirty ||
                document?.GameplayCandidate != null ||
                journalRecoveryBlocked ||
                pendingGameplayPlacement != null ||
                pendingJournalPlacements.Count > 0)
            {
                throw new InvalidOperationException(
                    "MoreEquipmentSlots owner deactivation was deferred because uncommitted or ambiguous gameplay state must first reach SaveSaved or be discarded at title/restart. reason=" +
                    reason +
                    ".");
            }
        }

        private void TryRestoreEnabledRuntimeAfterConfigurationFailure(
            bool wasEnabled,
            ICollection<Exception> failures)
        {
            if (!wasEnabled)
                return;
            try
            {
                hooks.InstallAtomically(this);
            }
            catch (Exception ex)
            {
                failures.Add(ex);
            }
            try
            {
                ApplyStoredFunctions(
                    "configuration disable rollback");
            }
            catch (Exception ex)
            {
                failures.Add(ex);
            }
        }

        private void RecoverDurableJournal(string reason)
        {
            if (document?.Journal == null)
                return;
            string fingerprint =
                DolocTownItemPlacementGateway
                    .GetNativeSaveFingerprint(archiveIndex);
            EquipmentSlotRecoveryDecision decision =
                EquipmentSlotTransactionCoordinator
                    .DecideRecovery(
                        document,
                        fingerprint,
                        itemId => Observe(
                            itemId,
                            fingerprint));
            lastMessage =
                reason + ": " + decision.Reason;
            if (decision.Action ==
                EquipmentSlotRecoveryAction
                    .RetryPlacement)
            {
                journalRecoveryBlocked = false;
                if (document.Journal.AttemptStarted)
                {
                    EquipmentSlotTransactionCoordinator
                        .ResetFailedAttempt(document);
                    PersistDocument(
                        "reset unsaved prepared attempt");
                }
                return;
            }
            if (decision.Action ==
                EquipmentSlotRecoveryAction.FailClosed)
            {
                journalRecoveryBlocked = true;
                pendingNativeSave = false;
                monitor.Log(
                    "MoreEquipmentSlots journal recovery failed closed: " +
                    decision.Reason,
                    LogLevel.Error);
                return;
            }

            journalRecoveryBlocked = false;
            EquipmentSlotTransactionJournal journal =
                document.Journal;
            if (journal.State ==
                EquipmentSlotJournalState.Prepared)
            {
                EquipmentSlotTransactionCoordinator
                    .PromoteCommittedTombstone(
                        document,
                        fingerprint);
                PersistDocument(
                    "recovered prepared native commit");
                LogCommittedDestinations(journal);
            }
            EquipmentSlotTransactionCoordinator
                .FinalizeCommitted(document);
            PersistDocument("recovered committed tombstone");
        }

        private void ReconcilePendingGameplayPlacement(
            string reason)
        {
            PendingGameplayPlacement? pending =
                pendingGameplayPlacement;
            if (pending == null)
                return;
            if (document == null ||
                workingSlots == null ||
                archiveIndex < 0)
            {
                throw new InvalidOperationException(
                    "Pending gameplay placement has no loaded save authority.");
            }
            string fingerprint =
                DolocTownItemPlacementGateway
                    .GetNativeSaveFingerprint(archiveIndex);
            if (!string.Equals(
                pending.PreSaveFingerprint,
                fingerprint,
                StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Pending gameplay placement cannot overwrite a changed native save preimage.");
            }

            EquipmentSlotStorageEntry slot =
                RequireSlot(pending.SlotIndex);
            if (!slot.IsOccupied ||
                !string.Equals(
                    slot.ItemId,
                    pending.ItemId,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Pending gameplay placement no longer matches its exact Working slot authority.");
            }

            NativePlacementResult resolved =
                placement.ResolveUnknown(pending.Outcome);
            pendingGameplayPlacement = null;
            lastPlacement = resolved.Kind;
            if (!resolved.Success)
            {
                lastMessage =
                    reason +
                    ": native placement reconciled to no mutation; the Working slot remains authoritative.";
                return;
            }

            slot.Clear();
            MarkWorkingDirty(
                reason +
                " reconciled native placement");
            ClearFunctions(
                reason +
                " reconciled native placement");
            ApplyStoredFunctions(
                reason +
                " reconciled native placement");
            lastMessage =
                reason +
                ": native placement reconciled exactly to " +
                resolved.Kind +
                "; the Working slot was released without retry.";
        }

        private void ReconcilePendingJournalPlacements(
            string reason)
        {
            if (pendingJournalPlacements.Count == 0)
                return;
            if (document?.Journal == null)
            {
                throw new InvalidOperationException(
                    "Pending durable placement evidence has no journal authority.");
            }
            EquipmentSlotTransactionJournal journal =
                document.Journal;
            string fingerprint =
                DolocTownItemPlacementGateway
                    .GetNativeSaveFingerprint(archiveIndex);
            if (!string.Equals(
                journal.PreSaveFingerprint,
                fingerprint,
                StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Pending durable placement cannot reconcile across a changed native save preimage.");
            }

            foreach (KeyValuePair<
                int,
                EquipmentSlotNativeMutationOutcomeUnknownException>
                pair in pendingJournalPlacements)
            {
                if (pair.Key < 0 ||
                    pair.Key >= journal.Escrow.Count)
                {
                    throw new InvalidOperationException(
                        "Pending durable placement references an invalid escrow index.");
                }
                EquipmentSlotEscrowEntry escrow =
                    journal.Escrow[pair.Key];
                if (escrow.AttemptCompleted ||
                    !string.Equals(
                        escrow.ItemId,
                        pair.Value.ItemId,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Pending durable placement no longer matches its exact escrow authority.");
                }
                NativePlacementResult resolved =
                    placement.ResolveUnknown(pair.Value);
                var after =
                    new NativeRecoveryObservation(
                        fingerprint,
                        resolved.HasExactAfterCounts
                            ? resolved.AfterBackpackCount
                            : pair.Value.BeforeBackpackCount +
                                resolved.BackpackDelta,
                        resolved.HasExactAfterCounts
                            ? resolved.AfterMailCount
                            : pair.Value.BeforeMailCount +
                                resolved.MailDelta);
                EquipmentSlotTransactionCoordinator
                    .RecordPlacement(
                        journal,
                        pair.Key,
                        resolved,
                        after);
                lastPlacement = resolved.Kind;
            }

            PersistDocument(
                reason +
                " reconciled outcome-unknown durable placements");
            pendingJournalPlacements.Clear();
            journalRecoveryBlocked = false;
            monitor.Log(
                "MoreEquipmentSlots reconciled outcome-unknown durable placements without replay transaction=" +
                journal.TransactionId +
                ".");
        }

        private static NativeRecoveryObservation
            ObservationAfterPlacement(
                string fingerprint,
                EquipmentSlotEscrowEntry escrow,
                NativePlacementResult result) =>
            new NativeRecoveryObservation(
                fingerprint,
                result.HasExactAfterCounts
                    ? result.AfterBackpackCount
                    : escrow.BeforeBackpackCount +
                        result.BackpackDelta,
                result.HasExactAfterCounts
                    ? result.AfterMailCount
                    : escrow.BeforeMailCount +
                        result.MailDelta);

        private void RecoverGameplayCandidate(string reason)
        {
            if (document?.GameplayCandidate == null)
                return;
            string fingerprint =
                DolocTownItemPlacementGateway
                    .GetNativeSaveFingerprint(archiveIndex);
            EquipmentSlotGameplayRecoveryDecision decision =
                EquipmentSlotGameplayCandidateCoordinator
                    .DecideRecovery(
                        document,
                        fingerprint,
                        itemId => Observe(
                            itemId,
                            fingerprint));
            lastMessage =
                reason + ": " + decision.Reason;
            if (decision.Action ==
                EquipmentSlotGameplayRecoveryAction
                    .FailClosed)
            {
                journalRecoveryBlocked = true;
                pendingNativeSave = false;
                monitor.Log(
                    "MoreEquipmentSlots gameplay candidate recovery failed closed: " +
                    decision.Reason,
                    LogLevel.Error);
                return;
            }
            if (decision.Action ==
                EquipmentSlotGameplayRecoveryAction
                    .DiscardUncommitted)
            {
                EquipmentSlotGameplayCandidateCoordinator
                    .DiscardUncommitted(document);
                PersistDocument(
                    "discarded uncommitted gameplay candidate");
                pendingNativeSave = false;
                return;
            }
            if (decision.Action ==
                EquipmentSlotGameplayRecoveryAction
                    .PromoteCommitted)
            {
                EquipmentSlotGameplayCandidateCoordinator
                    .PromoteCommitted(
                        document,
                        fingerprint);
                PersistDocument(
                    "recovered native-committed gameplay candidate");
            }
            EquipmentSlotGameplayCandidateCoordinator
                .FinalizeCommitted(document);
            PersistDocument(
                "recovered gameplay tombstone cleanup");
            pendingNativeSave = false;
            workingDirty = false;
            workingSlots =
                EquipmentSlotGameplayCandidateCoordinator
                    .CloneSlots(document.Slots);
        }

        private void LogCommittedDestinations(
            EquipmentSlotTransactionJournal journal)
        {
            foreach (EquipmentSlotEscrowEntry item in
                journal.Escrow)
            {
                if (item.Placement ==
                    NativePlacementKind.Failure)
                {
                    continue;
                }
                monitor.Log(
                    "MoreEquipmentSlots protected transaction committed destination=" +
                    item.Placement +
                    " item=" +
                    item.ItemId +
                    " transaction=" +
                    journal.TransactionId +
                    ".");
                monitor.Log(
                    "MoreEquipmentSlots recovery committed destination=" +
                    item.Placement +
                    " item=" +
                    item.ItemId +
                    ".");
            }
        }

        private NativeRecoveryObservation Observe(
            string itemId,
            string fingerprint) =>
            placement.Observe(itemId, fingerprint);

        private void PersistDocument(string reason)
        {
            if (document == null ||
                string.IsNullOrWhiteSpace(sidecarPath))
            {
                throw new InvalidOperationException(
                    "A loaded archive is required before protected equipment-slot storage can change.");
            }
            long previousGeneration =
                document.Generation;
            document.Generation++;
            try
            {
                store.WriteAtomic(sidecarPath, document);
                lastMessage =
                    reason +
                    " generation=" +
                    document.Generation;
            }
            catch
            {
                document.Generation =
                    previousGeneration;
                throw;
            }
        }

        private string BuildSidecarPath(int slot) =>
            Path.Combine(
                configRoot,
                "protected-items",
                "equipment-slots",
                "slot-" +
                slot.ToString(CultureInfo.InvariantCulture),
                "equipment-slots-" +
                MoreEquipmentSlotsProductContract.UniqueId +
                ".json");

        private string BuildLegacyGlobalSidecarPath() =>
            Path.Combine(
                configRoot,
                "equipment-slots-" +
                MoreEquipmentSlotsProductContract.UniqueId +
                ".json");

        private bool MatchesLoadedSlot(int? slot) =>
            slot.HasValue &&
            slot.Value == archiveIndex &&
            document != null;

        private EquipmentSlotStorageEntry RequireSlot(int index)
        {
            if (document == null)
            {
                throw new InvalidOperationException(
                    "No equipment-slot sidecar is loaded.");
            }
            if (index < 0 ||
                index >=
                    MoreEquipmentSlotsProductContract
                        .FixedSlotCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            return RequireWorkingSlots()[index];
        }

        private List<EquipmentSlotStorageEntry>
            RequireWorkingSlots()
        {
            if (workingSlots == null)
            {
                if (document == null)
                {
                    throw new InvalidOperationException(
                        "No equipment-slot sidecar is loaded.");
                }
                workingSlots =
                    EquipmentSlotGameplayCandidateCoordinator
                        .CloneSlots(document.Slots);
            }
            return workingSlots;
        }

        private void MarkWorkingDirty(string reason)
        {
            workingDirty = true;
            lastMessage =
                reason +
                "; Working differs from the last committed sidecar.";
        }

        private static EquipmentSlotStorageEntry CloneStorageEntry(
            EquipmentSlotStorageEntry source) =>
            new EquipmentSlotStorageEntry
            {
                Index = source.Index,
                ItemId = source.ItemId,
                DisplayName = source.DisplayName,
                SkillId = source.SkillId,
                DefenseBonus = source.DefenseBonus,
                IsShield = source.IsShield,
                ShieldValue = source.ShieldValue,
                ShieldMaxValue = source.ShieldMaxValue,
                ShieldDefend = source.ShieldDefend
            };

        private static MoreEquipmentSlotsConfig CloneConfig(
            MoreEquipmentSlotsConfig? source) =>
            new MoreEquipmentSlotsConfig
            {
                Enabled = source?.Enabled ?? true,
                VerboseLogging =
                    source?.VerboseLogging ?? false
            };

        private EquipmentSlotStorageEntry BuildStorageEntry(
            string itemId,
            int slotIndex)
        {
            object item = GenerateNativeItem(itemId);
            Type itemType = item.GetType();
            bool isPassive =
                IsTypeOrBase(itemType, "DolocTown.ItemPassive");
            bool isHat =
                IsTypeOrBase(itemType, "DolocTown.ItemHat");
            if (!isPassive && !isHat)
            {
                throw new InvalidOperationException(
                    "Only native passive equipment or hats may enter product slots.");
            }

            object? proto = Read(item, "proto");
            object? itemFunction = Read(proto, "Function");
            string itemFunctionType =
                itemFunction?.GetType().FullName ?? string.Empty;
            object? hatInfo =
                isHat ? Read(itemFunction, "HatId_Ref") : null;
            string skillId =
                isHat
                    ? ReadString(hatInfo, "Skill")
                    : ReadString(itemFunction, "Skill");
            int defense =
                isHat
                    ? Math.Max(0, ReadInt(hatInfo, "Defense"))
                    : 0;
            object? skillRef = Read(hatInfo, "Skill_Ref");
            object? skillFunction =
                Read(skillRef, "Function");
            string skillFunctionType =
                skillFunction?.GetType().FullName ??
                string.Empty;
            bool shield =
                string.Equals(
                    skillId,
                    "shield",
                    StringComparison.OrdinalIgnoreCase) ||
                itemFunctionType.IndexOf(
                    "ItemFunctionHatShield",
                    StringComparison.OrdinalIgnoreCase) >= 0 ||
                skillFunctionType.IndexOf(
                    "AgentEquipmentFuncProtoShield",
                    StringComparison.OrdinalIgnoreCase) >= 0;
            int shieldMax =
                shield
                    ? Math.Max(
                        0,
                        ReadInt(
                            itemFunction,
                            "MaxShieldValue"))
                    : 0;
            int shieldDefend =
                shield
                    ? Math.Max(
                        0,
                        ReadInt(
                            skillFunction,
                            "Defend"))
                    : 0;
            if (shield && shieldMax <= 0)
            {
                throw new InvalidOperationException(
                    "A native shield item must expose a positive maximum shield value before it can enter a ProductNative slot.");
            }
            return new EquipmentSlotStorageEntry
            {
                Index = slotIndex,
                ItemId = itemId,
                DisplayName =
                    FirstText(
                        ReadString(proto, "Title"),
                        itemId),
                SkillId = skillId,
                DefenseBonus = defense,
                IsShield = shield,
                ShieldMaxValue = shieldMax,
                ShieldValue = shieldMax,
                ShieldDefend = shieldDefend
            };
        }

        private void RehydrateWorkingTraits(string reason)
        {
            if (workingSlots == null)
                return;
            bool changed = false;
            foreach (EquipmentSlotStorageEntry slot in
                workingSlots)
            {
                if (!slot.IsOccupied)
                    continue;
                try
                {
                    EquipmentSlotStorageEntry native =
                        BuildStorageEntry(
                            slot.ItemId,
                            slot.Index);
                    int shieldValue =
                        slot.IsShield &&
                        native.IsShield &&
                        slot.ShieldMaxValue > 0
                            ? Math.Min(
                                Math.Max(
                                    0,
                                    slot.ShieldValue),
                                native.ShieldMaxValue)
                            : native.ShieldValue;
                    if (string.Equals(
                            slot.DisplayName,
                            native.DisplayName,
                            StringComparison.Ordinal) &&
                        string.Equals(
                            slot.SkillId,
                            native.SkillId,
                            StringComparison.Ordinal) &&
                        slot.DefenseBonus ==
                            native.DefenseBonus &&
                        slot.IsShield == native.IsShield &&
                        slot.ShieldValue == shieldValue &&
                        slot.ShieldMaxValue ==
                            native.ShieldMaxValue &&
                        slot.ShieldDefend ==
                            native.ShieldDefend)
                    {
                        invalidTraitSlots.Remove(slot.Index);
                        continue;
                    }

                    slot.DisplayName =
                        native.DisplayName;
                    slot.SkillId = native.SkillId;
                    slot.DefenseBonus =
                        native.DefenseBonus;
                    slot.IsShield = native.IsShield;
                    slot.ShieldValue = shieldValue;
                    slot.ShieldMaxValue =
                        native.ShieldMaxValue;
                    slot.ShieldDefend =
                        native.ShieldDefend;
                    invalidTraitSlots.Remove(slot.Index);
                    changed = true;
                }
                catch (Exception ex)
                {
                    invalidTraitSlots.Add(slot.Index);
                    monitor.Log(
                        "MoreEquipmentSlots retained protected item '" +
                        slot.ItemId +
                        "' and its durable traits unchanged, but disabled its product effect for this session because native traits could not be refreshed during " +
                        reason +
                        ": " +
                        ex.Message,
                        LogLevel.Error);
                }
            }
            if (changed)
                MarkWorkingDirty(
                    "native traits refreshed " + reason);
        }

        private void ApplyStoredFunctions(string reason)
        {
            if (!config.Enabled || document == null)
                return;
            object? manager = GetEquipmentManager();
            if (manager == null)
                return;
            ClearFunctions(
                "reapply " + reason,
                deferNativeReload: true);
            IDictionary? functions =
                Read(manager, "functions") as IDictionary;
            if (functions == null)
            {
                suppressNativeReloadReapply = true;
                try
                {
                    InvokeNoArg(manager, "ReloadParams");
                }
                finally
                {
                    suppressNativeReloadReapply = false;
                }
                return;
            }

            foreach (EquipmentSlotStorageEntry slot in
                RequireWorkingSlots())
            {
                if (!slot.IsOccupied ||
                    invalidTraitSlots.Contains(slot.Index) ||
                    !MoreEquipmentSlotsEffectPolicy
                        .ShouldCreateNativeFunction(
                            slot.IsShield,
                            slot.SkillId))
                {
                    continue;
                }
                try
                {
                    object item =
                        GenerateNativeItem(slot.ItemId);
                    Type functionType =
                        typeof(DolocAPI).Assembly.GetType(
                            "DolocTown.AgentEquipmentFunction",
                            throwOnError: false) ??
                        throw new TypeLoadException(
                            "AgentEquipmentFunction");
                    MethodInfo create =
                        functionType.GetMethod(
                            "CreateAgentEquipmentFunction",
                            BindingFlags.Public |
                            BindingFlags.Static) ??
                        throw new MissingMethodException(
                            functionType.FullName,
                            "CreateAgentEquipmentFunction");
                    object?[] args =
                    {
                        item,
                        manager,
                        slot.SkillId,
                        null
                    };
                    object? created =
                        create.Invoke(null, args);
                    if (created is bool ok &&
                        ok &&
                        args[3] != null)
                    {
                        functions[item] = args[3];
                        functionLeases.Add(
                            new NativeFunctionLease(
                                functions,
                                item,
                                args[3]!));
                    }
                }
                catch (Exception ex)
                {
                    monitor.Log(
                        "MoreEquipmentSlots function apply failed item=" +
                        slot.ItemId +
                        " " +
                        ex.GetType().Name +
                        ": " +
                        ex.Message,
                        LogLevel.Error);
                }
            }
            suppressNativeReloadReapply = true;
            try
            {
                InvokeNoArg(manager, "ReloadParams");
            }
            finally
            {
                suppressNativeReloadReapply = false;
            }
            ApplyAfterNativeReload(manager);
            PublishLifecycle();
        }

        private void ClearFunctions(
            string reason,
            bool deferNativeReload = false)
        {
            var failures = new List<Exception>();
            object? manager = GetEquipmentManager();
            foreach (NativeFunctionLease lease in
                functionLeases)
            {
                try
                {
                    lease.RemoveAndDispose();
                }
                catch (Exception ex)
                {
                    failures.Add(ex);
                }
            }
            functionLeases.Clear();
            if (manager != null &&
                (!deferNativeReload || failures.Count > 0))
            {
                suppressNativeReloadReapply = true;
                try
                {
                    InvokeNoArg(manager, "ReloadParams");
                }
                catch (Exception ex)
                {
                    failures.Add(ex);
                }
                finally
                {
                    suppressNativeReloadReapply = false;
                }
            }
            if (failures.Count > 0)
            {
                throw new AggregateException(
                    "MoreEquipmentSlots could not clear exact product function roots for " +
                    reason +
                    ".",
                    failures);
            }
        }

        private void ClearNativeAndUi(string reason)
        {
            Exception? functionFailure = null;
            try
            {
                ClearFunctions(reason);
            }
            catch (Exception ex)
            {
                functionFailure = ex;
            }
            ClearUi(reason);
            if (functionFailure != null)
                throw functionFailure;
        }

        private void ResetTransientRoots(string reason)
        {
            ClearNativeAndUi(reason);
            pendingNativeSave = false;
            monitor.Log(
                "MoreEquipmentSlots UI lifecycle cleared reason=" +
                reason +
                " " +
                BuildLifecycleSummary() +
                ".");
        }

        private sealed class PendingGameplayPlacement
        {
            internal PendingGameplayPlacement(
                int slotIndex,
                string itemId,
                string preSaveFingerprint,
                EquipmentSlotNativeMutationOutcomeUnknownException
                    outcome)
            {
                SlotIndex = slotIndex;
                ItemId = itemId ?? string.Empty;
                PreSaveFingerprint =
                    preSaveFingerprint ?? string.Empty;
                Outcome = outcome ??
                    throw new ArgumentNullException(
                        nameof(outcome));
            }

            internal int SlotIndex { get; }

            internal string ItemId { get; }

            internal string PreSaveFingerprint { get; }

            internal
                EquipmentSlotNativeMutationOutcomeUnknownException
                Outcome { get; }
        }

        private void ClearUi(string reason)
        {
            foreach (UiLease lease in uiLeases)
            {
                lease.ClearListeners();
                DestroyUnityObject(
                    Read(lease.Slot, "gameObject") ??
                    lease.Slot);
            }
            uiLeases.Clear();
            monitor.Log(
                "MoreEquipmentSlots UI lifecycle cleared reason=" +
                reason +
                " clones=0;listeners=0.");
        }

        private void RenderCurrentUi(string reason)
        {
            if (uiLeases.Count == 0)
                return;
            foreach (UiLease lease in uiLeases)
                RenderSlot(lease.Slot, RequireSlot(lease.Index));
            monitor.Log(
                "MoreEquipmentSlots UI rendered reason=" +
                reason +
                " clones=" +
                uiLeases.Count +
                ".");
        }

        private void BindUiLease(UiLease lease, int slotIndex)
        {
            lease.Index = slotIndex;
            MethodInfo? setClick = FindMethod(
                lease.Slot.GetType(),
                "SetClickCallbacks",
                8);
            if (setClick != null)
            {
                ParameterInfo[] parameters =
                    setClick.GetParameters();
                var clickBinder =
                    new IntCallbackBinder(
                        _ => OnUiClick(slotIndex));
                Delegate click = Delegate.CreateDelegate(
                    parameters[0].ParameterType,
                    clickBinder,
                    typeof(IntCallbackBinder).GetMethod(
                        nameof(IntCallbackBinder.Invoke))!);
                object?[] args =
                    new object?[parameters.Length];
                args[0] = click;
                if (args.Length > 4)
                    args[4] = click;
                setClick.Invoke(lease.Slot, args);
                lease.ListenerRoots.Add(clickBinder);
                lease.ListenerRoots.Add(click);
            }

            BindIntEvent(
                Read(lease.Slot, "onPointerEnter"),
                new IntCallbackBinder(
                    _ => OnUiHover(slotIndex)),
                lease);
            BindIntEvent(
                Read(lease.Slot, "onPointerExit"),
                new IntCallbackBinder(
                    _ => HideHover()),
                lease);
        }

        private void BindIntEvent(
            object? unityEvent,
            IntCallbackBinder binder,
            UiLease lease)
        {
            if (unityEvent == null)
                return;
            InvokeNoArg(unityEvent, "RemoveAllListeners");
            MethodInfo? add = FindMethod(
                unityEvent.GetType(),
                "AddListener",
                1);
            if (add == null)
                return;
            Type delegateType =
                add.GetParameters()[0].ParameterType;
            Delegate callback = Delegate.CreateDelegate(
                delegateType,
                binder,
                typeof(IntCallbackBinder).GetMethod(
                    nameof(IntCallbackBinder.Invoke))!);
            add.Invoke(
                unityEvent,
                new object[] { callback });
            lease.ListenerRoots.Add(binder);
            lease.ListenerRoots.Add(callback);
            lease.Events.Add(unityEvent);
        }

        private void OnUiClick(int slotIndex)
        {
            EquipmentSlotStorageEntry slot =
                RequireSlot(slotIndex);
            if (slot.IsOccupied)
                RequestUnequip(slotIndex);
            else if (!TryEquipFromNativeBuffer(slotIndex))
                lastMessage =
                    "Empty slot click requires an item held by the native inventory UI.";
            RenderCurrentUi("click slot=" + slotIndex);
        }

        private bool TryEquipFromNativeBuffer(int slotIndex)
        {
            if (!TryGetNativeInventoryBuffer(
                out _,
                out object? currentItem) ||
                currentItem == null)
            {
                return false;
            }
            string itemId = ReadString(currentItem, "name");
            return !string.IsNullOrWhiteSpace(itemId) &&
                EquipFromBackpack(itemId, slotIndex);
        }

        private static bool TryTakeMatchingNativeBuffer(
            string itemId,
            out bool succeeded)
        {
            succeeded = false;
            if (!TryGetNativeInventoryBuffer(
                    out object? buffer,
                    out object? currentItem) ||
                buffer == null ||
                currentItem == null ||
                !string.Equals(
                    ReadString(currentItem, "name"),
                    itemId,
                    StringComparison.Ordinal))
            {
                return false;
            }

            object? taken =
                FindMethod(
                    buffer.GetType(),
                    "Take",
                    0)?.Invoke(buffer, null);
            succeeded =
                taken != null &&
                string.Equals(
                    ReadString(taken, "name"),
                    itemId,
                    StringComparison.Ordinal);
            return true;
        }

        private static bool TryGetNativeInventoryBuffer(
            out object? buffer,
            out object? currentItem)
        {
            object? archive = ReadStatic(
                typeof(DolocAPI),
                "archiveHandle");
            object? inventorySystem =
                Read(archive, "InventorySystem");
            buffer = Read(inventorySystem, "buffer");
            currentItem = Read(buffer, "CurrentItem");
            return buffer != null;
        }

        private void OnUiHover(int slotIndex)
        {
            EquipmentSlotStorageEntry slot =
                RequireSlot(slotIndex);
            object? nativeItem = slot.IsOccupied
                ? GenerateNativeItem(slot.ItemId)
                : null;
            UiLease? lease = FindUiLease(slotIndex);
            MethodInfo? show = lease == null
                ? null
                : FindMethod(
                    lease.Slot.GetType(),
                    "ShowEquipmentItemViewer",
                    2);
            show?.Invoke(
                lease!.Slot,
                new object?[]
                {
                    nativeItem,
                    "DTMAPI extra attribute slot"
                });
            monitor.Log(
                "MoreEquipmentSlots UI hover slot=" +
                slotIndex +
                " item=" +
                (slot.IsOccupied
                    ? slot.ItemId
                    : "empty") +
                ".");
        }

        private UiLease? FindUiLease(int index)
        {
            foreach (UiLease lease in uiLeases)
            {
                if (lease.Index == index)
                    return lease;
            }
            return null;
        }

        private static void HideHover()
        {
            MethodInfo? method = typeof(DolocAPI).GetMethod(
                "HideHoverBox",
                BindingFlags.Public | BindingFlags.Static);
            method?.Invoke(null, null);
        }

        private static void RenderSlot(
            object slot,
            EquipmentSlotStorageEntry entry)
        {
            object? sprite = null;
            if (entry.IsOccupied)
            {
                MethodInfo? getSprite =
                    typeof(DolocAPI).GetMethod(
                        "GetItemSprite",
                        BindingFlags.Public |
                        BindingFlags.Static,
                        binder: null,
                        types: new[] { typeof(string) },
                        modifiers: null);
                sprite = getSprite?.Invoke(
                    null,
                    new object[] { entry.ItemId });
            }
            MethodInfo? render =
                FindMethod(slot.GetType(), "Render", 1);
            render?.Invoke(slot, new[] { sprite });
        }

        private int CountUiListeners()
        {
            int count = 0;
            foreach (UiLease lease in uiLeases)
                count += lease.ListenerRoots.Count;
            return count;
        }

        private void PublishLifecycle()
        {
            MoreEquipmentSlotsCallbacks.PublishLifecycleSummary(
                BuildLifecycleSummary());
        }

        private static object GenerateNativeItem(string itemId)
        {
            Type type =
                typeof(DolocAPI).Assembly.GetType(
                    "DolocTown.ItemFactory",
                    throwOnError: false) ??
                throw new TypeLoadException(
                    "DolocTown.ItemFactory");
            MethodInfo? generate = null;
            foreach (MethodInfo method in type.GetMethods(
                BindingFlags.Public | BindingFlags.Static))
            {
                ParameterInfo[] parameters = method.GetParameters();
                if (method.Name ==
                    "GenerateItem" &&
                    parameters.Length == 3 &&
                    parameters[0].ParameterType ==
                        typeof(string) &&
                    parameters[1].ParameterType ==
                        typeof(int) &&
                    parameters[2].IsOut)
                {
                    generate = method;
                    break;
                }
            }
            if (generate == null)
            {
                throw new MissingMethodException(
                    type.FullName,
                    "GenerateItem(string,int,out Item)");
            }
            object?[] args = { itemId, 1, null };
            generate.Invoke(null, args);
            return args[2] ??
                throw new InvalidOperationException(
                    "Native item generation returned null.");
        }

        private static object? GetEquipmentManager()
        {
            object? archive = ReadStatic(
                typeof(DolocAPI),
                "archiveHandle");
            return Read(
                Read(
                    Read(archive, "farmData"),
                    "agentData"),
                "agentEquipment");
        }

        private static bool NativeManagerHasShield()
        {
            try
            {
                object? manager = GetEquipmentManager();
                MethodInfo? method = manager == null
                    ? null
                    : FindMethod(
                        manager.GetType(),
                        "TryGetShieldItem",
                        1);
                if (method == null)
                    return true;
                object?[] args = { null };
                object? result = method.Invoke(manager, args);
                return result is bool ok && ok && args[0] != null;
            }
            catch
            {
                return true;
            }
        }

        private EquipmentSlotStorageEntry? FindTailShield()
        {
            if (document == null)
                return null;
            List<EquipmentSlotStorageEntry> slots =
                RequireWorkingSlots();
            for (int index = slots.Count - 1;
                 index >= 0;
                 index--)
            {
                EquipmentSlotStorageEntry slot =
                    slots[index];
                if (!invalidTraitSlots.Contains(slot.Index) &&
                    slot.IsOccupied &&
                    slot.IsShield &&
                    slot.ShieldValue > 0)
                {
                    return slot;
                }
            }
            return null;
        }

        private static int CalculateNativeDamage(
            object body,
            float attack,
            bool critical)
        {
            float defend = Convert.ToSingle(
                Read(body, "CurrentDefend") ?? 0f,
                CultureInfo.InvariantCulture);
            Type? battle = typeof(DolocAPI).Assembly.GetType(
                "DolocTown.BattleUtils",
                throwOnError: false);
            MethodInfo? calc = battle?.GetMethod(
                "CalcDamage",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: new[]
                {
                    typeof(float),
                    typeof(float),
                    typeof(bool)
                },
                modifiers: null);
            object? result = calc?.Invoke(
                null,
                new object[] { attack, defend, critical });
            return result is int damage
                ? Math.Max(0, damage)
                : Math.Max(
                    0,
                    Convert.ToInt32(
                        Math.Round(attack - defend)));
        }

        private static void ApplyNativeAttackTail(
            object body,
            int damage,
            object position,
            ref bool isDead,
            bool fullBlocked)
        {
            RaiseDamageTip(damage, position);
            if (fullBlocked)
            {
                InvokeNoArg(Read(body, "HatRenderer"), "Shine");
                InvokeHitBack(body, position);
                isDead = false;
                return;
            }

            BroadcastHurt();
            isDead = InvokeMonsterAttackCostHealth(damage);
            if (!isDead)
            {
                ResetFishingStateOnNativeHit(body);
                OverwriteAgentStateHit(body);
            }
            InvokeHitBack(body, position);
        }

        private void
            RefreshNativeParametersAfterShieldBreak()
        {
            try
            {
                ClearFunctions(
                    "shield break",
                    deferNativeReload: true);
                ApplyStoredFunctions("shield break");
            }
            catch (Exception ex)
            {
                monitor.Log(
                    "MoreEquipmentSlots shield was removed, but native equipment parameters could not be refreshed immediately: " +
                    ex.Message,
                    LogLevel.Error);
            }
        }

        private static bool InvokeMonsterAttackCostHealth(
            int damage)
        {
            Type hurtReason =
                typeof(DolocAPI).Assembly.GetType(
                    "DolocTown.HurtReason",
                    throwOnError: false) ??
                typeof(DolocAPI).Assembly.GetType(
                    "HurtReason",
                    throwOnError: false) ??
                throw new TypeLoadException(
                    "DolocTown.HurtReason");
            object monsterAttack =
                Enum.Parse(
                    hurtReason,
                    "MonsterAttack",
                    ignoreCase: false);
            MethodInfo cost =
                typeof(DolocAPI).GetMethod(
                    "CostHealth",
                    BindingFlags.Public |
                    BindingFlags.Static,
                    binder: null,
                    types: new[]
                    {
                        typeof(int),
                        hurtReason
                    },
                    modifiers: null) ??
                throw new MissingMethodException(
                    typeof(DolocAPI).FullName,
                    "CostHealth(int,HurtReason)");
            object? result = cost.Invoke(
                null,
                new[]
                {
                    (object)Math.Max(0, damage),
                    monsterAttack
                });
            return result is bool dead && dead;
        }

        private static void ResetFishingStateOnNativeHit(
            object body)
        {
            object? stateManager = Read(body, "StateManager");
            object? current = Read(stateManager, "current");
            if (current == null ||
                !IsTypeOrBase(
                    current.GetType(),
                    "DolocTown.AgentStateFishing"))
            {
                return;
            }

            Type fishingState =
                typeof(DolocAPI).Assembly.GetType(
                    "DolocTown.AgentStateFishing",
                    throwOnError: false) ??
                throw new TypeLoadException(
                    "DolocTown.AgentStateFishing");
            MethodInfo unset =
                FindMethod(
                    fishingState,
                    "UnsetUiControl",
                    0) ??
                throw new MissingMethodException(
                    fishingState.FullName,
                    "UnsetUiControl()");
            unset.Invoke(null, null);

            object renderer = Read(body, "fishRodRenderer") ??
                throw new MissingMemberException(
                    body.GetType().FullName,
                    "fishRodRenderer");
            MethodInfo setVisible =
                FindMethod(
                    renderer.GetType(),
                    "SetVisible",
                    1) ??
                throw new MissingMethodException(
                    renderer.GetType().FullName,
                    "SetVisible(bool)");
            setVisible.Invoke(
                renderer,
                new object[] { false });
        }

        private static void OverwriteAgentStateHit(object body)
        {
            object stateManager =
                Read(body, "StateManager") ??
                throw new MissingMemberException(
                    body.GetType().FullName,
                    "StateManager");
            Type stateHit =
                typeof(DolocAPI).Assembly.GetType(
                    "DolocTown.AgentStateHit",
                    throwOnError: false) ??
                throw new TypeLoadException(
                    "DolocTown.AgentStateHit");
            MethodInfo? overwrite = null;
            for (Type? current = stateManager.GetType();
                 current != null && overwrite == null;
                 current = current.BaseType)
            {
                foreach (MethodInfo method in current.GetMethods(
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance |
                    BindingFlags.DeclaredOnly))
                {
                    ParameterInfo[] parameters =
                        method.GetParameters();
                    if (method.Name == "Overwrite" &&
                        method.IsGenericMethodDefinition &&
                        parameters.Length == 1 &&
                        parameters[0].ParameterType ==
                            typeof(bool))
                    {
                        overwrite = method;
                        break;
                    }
                }
            }
            if (overwrite == null)
            {
                throw new MissingMethodException(
                    stateManager.GetType().FullName,
                    "Overwrite<T>(bool)");
            }
            overwrite.MakeGenericMethod(stateHit).Invoke(
                stateManager,
                new object[] { true });
        }

        private static void RaiseDamageTip(
            int damage,
            object position)
        {
            foreach (MethodInfo method in typeof(DolocAPI).GetMethods(
                BindingFlags.Public | BindingFlags.Static))
            {
                if (method.Name != "RaiseDamageTip" ||
                    method.GetParameters().Length != 6)
                {
                    continue;
                }
                method.Invoke(
                    null,
                    new object[]
                    {
                        damage,
                        position,
                        false,
                        0.7f,
                        50f,
                        1.5f
                    });
                return;
            }
        }

        private static void BroadcastHurt()
        {
            Type? eventType = typeof(DolocAPI).Assembly.GetType(
                "DolocTown.GameEventType",
                throwOnError: false);
            if (eventType == null)
                return;
            object value = Enum.Parse(
                eventType,
                "HURT_BY_MONSTER");
            MethodInfo? method = typeof(DolocAPI).GetMethod(
                "Broadcast",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: new[] { eventType },
                modifiers: null);
            method?.Invoke(null, new[] { value });
        }

        private static void InvokeHitBack(
            object body,
            object position)
        {
            MethodInfo? method = FindMethod(
                body.GetType(),
                "HitBack",
                1);
            method?.Invoke(body, new[] { position });
        }

        private static bool TryApplyDefense(
            object manager,
            int defense)
        {
            object? ability = Read(
                manager,
                "EquipmentAbility");
            if (ability == null)
                return false;
            Type type = ability.GetType();
            ConstructorInfo? constructor =
                type.GetConstructor(
                    new[]
                    {
                        typeof(int),
                        typeof(float),
                        typeof(bool),
                        typeof(float),
                        typeof(float),
                        typeof(int),
                        typeof(string[]),
                        typeof(float)
                    });
            if (constructor == null)
                return false;
            object updated = constructor.Invoke(
                new object[]
                {
                    ReadInt(ability, "defence") + defense,
                    ReadFloat(ability, "moveSpeedAddition"),
                    ReadBool(ability, "immuneAcidRain"),
                    ReadFloat(ability, "dashCdDecrease"),
                    ReadFloat(
                        ability,
                        "recoveryAdditionPercent"),
                    ReadInt(
                        ability,
                        "fellCoundAdditionOre"),
                    Read(ability, "ShieldSightOfMonsterNames")
                        as string[] ??
                    Array.Empty<string>(),
                    ReadFloat(
                        ability,
                        "CriticalRateChanged")
                });
            return SetMember(
                manager,
                "EquipmentAbility",
                updated);
        }

        private static object? CloneUnityObject(
            object source,
            object parent)
        {
            Type? unityObject =
                Type.GetType(
                    "UnityEngine.Object, UnityEngine.CoreModule",
                    throwOnError: false) ??
                Type.GetType(
                    "UnityEngine.Object, UnityEngine",
                    throwOnError: false);
            if (unityObject == null)
                return null;
            foreach (MethodInfo method in unityObject.GetMethods(
                BindingFlags.Public | BindingFlags.Static))
            {
                ParameterInfo[] parameters = method.GetParameters();
                if (method.Name != "Instantiate" ||
                    method.IsGenericMethod ||
                    parameters.Length != 2 ||
                    !parameters[0].ParameterType.IsAssignableFrom(
                        source.GetType()) ||
                    !parameters[1].ParameterType.IsAssignableFrom(
                        parent.GetType()))
                {
                    continue;
                }
                return method.Invoke(
                    null,
                    new[] { source, parent });
            }
            return null;
        }

        private static void DestroyUnityObject(object? value)
        {
            if (value == null)
                return;
            Type? unityObject =
                Type.GetType(
                    "UnityEngine.Object, UnityEngine.CoreModule",
                    throwOnError: false) ??
                Type.GetType(
                    "UnityEngine.Object, UnityEngine",
                    throwOnError: false);
            MethodInfo? destroy = unityObject?.GetMethod(
                "Destroy",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: new[] { unityObject },
                modifiers: null);
            destroy?.Invoke(null, new[] { value });
        }

        private static object? Read(
            object? instance,
            string name) =>
            MoreEquipmentSlotsReflectionAccess.Read(
                instance,
                name);

        private static object? ReadStatic(
            Type type,
            string name) =>
            MoreEquipmentSlotsReflectionAccess.ReadStatic(
                type,
                name);

        private static bool SetMember(
            object? instance,
            string name,
            object? value) =>
            MoreEquipmentSlotsReflectionAccess.Set(
                instance,
                name,
                value);

        private static MethodInfo? FindMethod(
            Type type,
            string name,
            int parameterCount)
        {
            for (Type? current = type;
                 current != null;
                 current = current.BaseType)
            {
                foreach (MethodInfo method in
                    current.GetMethods(
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.Instance |
                        BindingFlags.Static |
                        BindingFlags.DeclaredOnly))
                {
                    if (method.Name == name &&
                        method.GetParameters().Length ==
                            parameterCount)
                    {
                        return method;
                    }
                }
            }
            return null;
        }

        private static void InvokeNoArg(
            object? instance,
            string name)
        {
            if (instance == null)
                return;
            FindMethod(
                instance.GetType(),
                name,
                0)?.Invoke(instance, null);
        }

        private static bool IsTypeOrBase(
            Type type,
            string fullName)
        {
            for (Type? current = type;
                 current != null;
                 current = current.BaseType)
            {
                if (string.Equals(
                    current.FullName,
                    fullName,
                    StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        private static string ReadString(
            object? instance,
            string name) =>
            Read(instance, name) as string ??
            string.Empty;

        private static int ReadInt(
            object? instance,
            string name)
        {
            object? value = Read(instance, name);
            return value == null
                ? 0
                : Convert.ToInt32(
                    value,
                    CultureInfo.InvariantCulture);
        }

        private static float ReadFloat(
            object? instance,
            string name)
        {
            object? value = Read(instance, name);
            return value == null
                ? 0f
                : Convert.ToSingle(
                    value,
                    CultureInfo.InvariantCulture);
        }

        private static bool ReadBool(
            object? instance,
            string name)
        {
            object? value = Read(instance, name);
            return value is bool result && result;
        }

        private static string FirstText(
            string first,
            string second) =>
            !string.IsNullOrWhiteSpace(first)
                ? first
                : second ?? string.Empty;

        private sealed class NativeFunctionLease
        {
            private readonly IDictionary dictionary;
            private readonly object item;
            private readonly object function;

            internal NativeFunctionLease(
                IDictionary dictionary,
                object item,
                object function)
            {
                this.dictionary = dictionary;
                this.item = item;
                this.function = function;
            }

            internal void RemoveAndDispose()
            {
                if (dictionary.Contains(item))
                    dictionary.Remove(item);
                InvokeNoArg(function, "Dispose");
            }
        }

        private sealed class UiLease
        {
            internal UiLease(object slot) =>
                Slot = slot;

            internal object Slot { get; }

            internal int Index { get; set; }

            internal List<object> ListenerRoots { get; } =
                new List<object>();

            internal List<object> Events { get; } =
                new List<object>();

            internal void ClearListeners()
            {
                foreach (object unityEvent in Events)
                    InvokeNoArg(unityEvent, "RemoveAllListeners");
                InvokeNoArg(Slot, "ClearAllClickCallbacks");
                ListenerRoots.Clear();
                Events.Clear();
            }
        }

        private sealed class IntCallbackBinder
        {
            private readonly Action<int> callback;

            internal IntCallbackBinder(
                Action<int> callback) =>
                this.callback = callback;

            public void Invoke(int value) =>
                callback(value);
        }
    }
}
