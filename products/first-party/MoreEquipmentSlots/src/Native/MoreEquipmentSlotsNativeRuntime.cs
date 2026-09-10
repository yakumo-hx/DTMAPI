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

        public bool UiVisible { get; internal set; }

        public bool UiLayoutBlocked { get; internal set; }

        public string UiLayoutMessage { get; internal set; } =
            string.Empty;

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
        private readonly MoreEquipmentSlotsNativeShieldAdapter
            nativeShieldAdapter;
        private readonly EquipmentSlotDocumentStore store =
            new EquipmentSlotDocumentStore();
        private readonly EquipmentSlotNativePlacement placement =
            new EquipmentSlotNativePlacement(
                new DolocTownItemPlacementGateway());
        private readonly List<NativeFunctionLease> functionLeases =
            new List<NativeFunctionLease>();
        private readonly List<UiLease> uiLeases =
            new List<UiLease>();
        private UiSession? uiSession;
        private string slotUiText =
            "Extra equipment slot {0}";
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
        private bool newGamePending;
        private bool journalRecoveryBlocked;
        private bool workingDirty;
        private PendingGameplayPlacement?
            pendingGameplayPlacement;
        private EquipmentSlotNativeMutationOutcomeUnknownException?
            pendingGameplayIncomingWithdrawal;
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
            nativeShieldAdapter =
                new MoreEquipmentSlotsNativeShieldAdapter(this);
        }

        internal int InstalledPatchCount =>
            hooks.InstalledPatchCount;

        internal void ConfigureUiText(string slot)
        {
            slotUiText = FirstText(
                slot,
                slotUiText);
        }

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

        internal void OnSaveLoaded(int? slot) =>
            OnSaveLoaded(slot, isNewGame: false);

        internal void OnSaveLoaded(
            int? slot,
            bool isNewGame)
        {
            ResetTransientRoots("SaveLoaded");
            ClearLoadedSaveState();
            if (isNewGame)
            {
                InitializeNewGame(slot);
                return;
            }
            if (!slot.HasValue || slot.Value < 0)
            {
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

        private void InitializeNewGame(int? eventSlot)
        {
            int currentArchiveIndex =
                DolocTownItemPlacementGateway
                    .ReadCurrentArchiveIndex();
            if (eventSlot.HasValue &&
                eventSlot.Value != currentArchiveIndex)
            {
                throw new InvalidDataException(
                    "MoreEquipmentSlots refused NewGame initialization because the event slot did not match the initialized native archive index.");
            }

            DolocTownItemPlacementGateway
                .RequireNativeCurrentSaveMissing(
                    currentArchiveIndex);
            EquipmentSlotSaveScope currentScope =
                DolocTownItemPlacementGateway.ReadCurrentScope(
                    currentArchiveIndex);
            EquipmentSlotStorageDocument emptyDocument =
                EquipmentSlotDocumentStore.CreateEmpty(
                    currentScope);
            string currentSidecarPath =
                BuildSidecarPath(currentArchiveIndex);
            bool deleted =
                store.DeleteSlotDirectoryForNewGame(
                    currentSidecarPath,
                    currentArchiveIndex);

            archiveIndex = currentArchiveIndex;
            scope = currentScope;
            sidecarPath = currentSidecarPath;
            document = emptyDocument;
            workingSlots =
                EquipmentSlotGameplayCandidateCoordinator
                    .CloneSlots(document.Slots);
            workingDirty = false;
            newGamePending = true;
            RehydrateWorkingTraits("NewGame");
            if (config.Enabled)
                ApplyStoredFunctions("NewGame");
            lastMessage =
                "NewGame initialized archive=" +
                archiveIndex.ToString(
                    CultureInfo.InvariantCulture) +
                "; previousProductDirectoryDeleted=" +
                deleted.ToString() +
                ".";
            monitor.Log(
                "MoreEquipmentSlots NewGame reset established " +
                "archive=" +
                archiveIndex.ToString(
                    CultureInfo.InvariantCulture) +
                "; previousProductDirectoryDeleted=" +
                deleted.ToString() +
                "; sidecar=" +
                sidecarPath +
                ".");
            PublishLifecycle();
        }

        private void RefreshNewGameScopeBeforeFirstSave()
        {
            if (document == null || !newGamePending)
            {
                throw new InvalidOperationException(
                    "A pending NewGame document is required before first-save scope binding.");
            }
            int currentArchiveIndex =
                DolocTownItemPlacementGateway
                    .ReadCurrentArchiveIndex();
            if (currentArchiveIndex != archiveIndex)
            {
                throw new InvalidDataException(
                    "MoreEquipmentSlots refused first NewGame save because the initialized native archive index changed.");
            }
            DolocTownItemPlacementGateway
                .RequireNativeCurrentSaveMissing(archiveIndex);
            EquipmentSlotSaveScope currentScope =
                DolocTownItemPlacementGateway.ReadCurrentScope(
                    archiveIndex);
            document.Scope = currentScope.Clone();
            scope = currentScope;
        }

        private string CapturePreSaveFingerprint() =>
            DolocTownItemPlacementGateway
                .GetNativeSaveFingerprint(
                    archiveIndex,
                    allowMissingCurrent: newGamePending);

        private void ClearLoadedSaveState()
        {
            archiveIndex = -1;
            document = null;
            workingSlots = null;
            scope = null;
            sidecarPath = string.Empty;
            pendingNativeSave = false;
            newGamePending = false;
            journalRecoveryBlocked = false;
            workingDirty = false;
            pendingGameplayPlacement = null;
            pendingGameplayIncomingWithdrawal = null;
            pendingJournalPlacements.Clear();
            invalidTraitSlots.Clear();
        }

        internal void OnSaveSaving(int? slot)
        {
            if (!MatchesLoadedSlot(slot) ||
                document == null)
            {
                return;
            }
            bool recoveringNewGameCandidate =
                newGamePending &&
                document.GameplayCandidate != null;
            ReconcilePendingGameplayPlacement(
                "SaveSaving");
            ReconcilePendingGameplayIncomingWithdrawal(
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
            if (recoveringNewGameCandidate &&
                document.GameplayCandidate == null &&
                DolocTownItemPlacementGateway
                    .NativeCurrentSaveExists(archiveIndex))
            {
                newGamePending = false;
                lastMessage =
                    "Recovered the first native NewGame commit before a repeated SaveSaving boundary.";
            }
            if (newGamePending)
                RefreshNewGameScopeBeforeFirstSave();
            if ((workingDirty || newGamePending) &&
                document.GameplayCandidate == null &&
                document.Journal == null)
            {
                string gameplayPreFingerprint =
                    CapturePreSaveFingerprint();
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
                CapturePreSaveFingerprint();
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
                        placement.PlaceOneWithImmediateEvidence(
                            escrow.ItemId);
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
                    try
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
                    catch (
                        EquipmentSlotNativeMutationOutcomeUnknownException
                            ex)
                    {
                        pendingJournalPlacements[-1] = ex;
                        journalRecoveryBlocked = true;
                        pendingNativeSave = false;
                        monitor.Log(
                            "MoreEquipmentSlots retained the durable replacement journal after incoming withdrawal became outcome-unknown; this process will not replay it item=" +
                            incomingItemId +
                            " transaction=" +
                            journal.TransactionId +
                            ".",
                            LogLevel.Error);
                        PublishLifecycle();
                        throw;
                    }
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
                newGamePending = false;
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
            newGamePending = false;
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
                pendingGameplayIncomingWithdrawal != null ||
                string.IsNullOrWhiteSpace(itemId))
            {
                return false;
            }
            if (pendingGameplayPlacement != null)
            {
                lastMessage =
                    "Replacement remains blocked because its one immediate native placement observation was ambiguous; reload without native save to restore the committed projection.";
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
                    CapturePreSaveFingerprint();
                NativePlacementResult outgoing;
                try
                {
                    outgoing =
                        placement.PlaceOneWithImmediateEvidence(
                            slot.ItemId);
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

            bool withdrawn;
            try
            {
                bool bufferAttempted =
                    TryTakeMatchingNativeBuffer(
                        replacement.ItemId,
                        out bool bufferSucceeded);
                withdrawn =
                    bufferAttempted
                        ? bufferSucceeded
                        : placement.TryWithdrawOne(
                            replacement.ItemId);
            }
            catch (
                EquipmentSlotNativeMutationOutcomeUnknownException
                    ex)
            {
                pendingGameplayIncomingWithdrawal = ex;
                if (outgoingReleased)
                {
                    MarkWorkingDirty(
                        "replacement incoming withdrawal became outcome-unknown");
                    ClearFunctions(
                        "EquipFromBackpack incoming outcome unknown");
                    ApplyStoredFunctions(
                        "EquipFromBackpack incoming outcome unknown");
                }
                lastMessage =
                    "Incoming withdrawal became outcome-unknown; all Product slot operations and SaveSaving are blocked until title/restart discards the unsaved native transaction.";
                monitor.Log(
                    "MoreEquipmentSlots gameplay incoming withdrawal became outcome-unknown item=" +
                    replacement.ItemId +
                    " slot=" +
                    slotIndex +
                    ".",
                    LogLevel.Error);
                return false;
            }
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
                pendingJournalPlacements.Count > 0 ||
                pendingGameplayIncomingWithdrawal != null)
            {
                return false;
            }
            if (pendingGameplayPlacement != null)
            {
                lastMessage =
                    "Unequip remains blocked because its one immediate native placement observation was ambiguous; reload without native save to restore the committed projection.";
                return false;
            }
            EquipmentSlotStorageEntry slot =
                RequireSlot(slotIndex);
            if (!slot.IsOccupied)
                return true;

            string fingerprint =
                CapturePreSaveFingerprint();
            NativePlacementResult result;
            try
            {
                result =
                    placement.PlaceOneWithImmediateEvidence(
                        slot.ItemId);
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
                    RootCount = uiSession != null ? 1 : 0,
                    UiVisible =
                        uiSession?.Visible == true &&
                        uiSession?.LayoutBlocked != true,
                    UiLayoutBlocked =
                        uiSession?.LayoutBlocked == true,
                    UiLayoutMessage =
                        uiSession?.LayoutMessage ?? string.Empty,
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
                newGamePending = false;
                journalRecoveryBlocked = false;
                workingDirty = false;
                pendingGameplayPlacement = null;
                pendingGameplayIncomingWithdrawal = null;
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
            newGamePending = false;
            journalRecoveryBlocked = false;
            workingDirty = false;
            pendingGameplayPlacement = null;
            pendingGameplayIncomingWithdrawal = null;
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

        internal DolocTown.IAgentEquipmentShieldItem?
            GetNativeShieldAdapter() =>
            config.Enabled && FindTailShield() != null
                ? nativeShieldAdapter
                : null;

        internal float GetNativeShieldPercent()
        {
            EquipmentSlotStorageEntry? shield =
                FindTailShield();
            return shield == null ||
                shield.ShieldMaxValue <= 0
                ? 0f
                : Math.Max(
                    0f,
                    Math.Min(
                        1f,
                        (float)shield.ShieldValue /
                        shield.ShieldMaxValue));
        }

        internal float GetNativeShieldValue() =>
            Math.Max(
                0,
                FindTailShield()?.ShieldValue ?? 0);

        internal bool TryBlockNativeAttack(
            int damage,
            out int blockedDamage)
        {
            blockedDamage = 0;
            EquipmentSlotStorageEntry? shield =
                FindTailShield();
            if (!config.Enabled || shield == null)
                return false;

            int defendedDamage = Math.Max(
                0,
                damage - Math.Max(0, shield.ShieldDefend));
            if (defendedDamage <= 0)
                return true;

            int available = Math.Max(0, shield.ShieldValue);
            if (available > defendedDamage)
            {
                shield.ShieldValue =
                    available - defendedDamage;
                blockedDamage = defendedDamage;
                MarkWorkingDirty(
                    "native shield interface absorbed attack");
                RefreshNativeShieldStatus();
                RenderCurrentUi(
                    "native shield interface absorbed attack");
                return true;
            }

            blockedDamage = available;
            shield.Clear();
            MarkWorkingDirty(
                "native shield interface broke");
            RefreshNativeParametersAfterShieldBreak();
            RefreshNativeShieldStatus();
            RenderCurrentUi(
                "native shield interface broke");
            return false;
        }

        internal void RenderAccessoriesBar(
            object accessoriesBar,
            int officialPassiveCount,
            string reason)
        {
            if (!config.Enabled ||
                document == null ||
                accessoriesBar == null)
            {
                return;
            }
            try
            {
                if (uiSession == null ||
                    !ReferenceEquals(
                        uiSession.AccessoriesBar,
                        accessoriesBar))
                {
                    ClearUi("native AccessoriesBar generation changed");
                    uiSession = new UiSession(accessoriesBar);
                    InitializeUiSession(
                        uiSession,
                        officialPassiveCount);
                }
                else
                {
                    uiSession.OfficialPassiveCount =
                        officialPassiveCount;
                    RefreshUiLayout(uiSession);
                }
                uiSession.Visible = !uiSession.LayoutBlocked;
                RenderCurrentUi(reason);
                ApplyUiVisibility(uiSession);
                monitor.Log(
                    "MoreEquipmentSlots UI rendered reason=" +
                    reason +
                    " clones=" +
                    uiLeases.Count +
                    " visible=" +
                    uiSession.Visible +
                    " layoutBlocked=" +
                    uiSession.LayoutBlocked +
                    " listeners=" +
                    CountUiListeners() +
                    ".");
            }
            catch (Exception ex)
            {
                BlockUiLayout(
                    uiSession,
                    "Product UI construction failed closed: " +
                    ex.GetBaseException().Message);
            }
            PublishLifecycle();
        }

        internal Array ComposeAccessoriesSelectables(
            object accessoriesBar,
            Array nativeSelectables)
        {
            if (nativeSelectables == null)
                throw new ArgumentNullException(
                    nameof(nativeSelectables));
            UiSession? session = uiSession;
            if (session == null ||
                session.LayoutBlocked ||
                !session.Visible ||
                !ReferenceEquals(
                    session.AccessoriesBar,
                    accessoriesBar))
            {
                return nativeSelectables;
            }

            var productButtons = new List<object>(
                MoreEquipmentSlotsProductContract.FixedSlotCount);
            foreach (UiLease lease in uiLeases)
                productButtons.Add(RequireUiButton(lease.Slot));
            Type elementType =
                nativeSelectables.GetType().GetElementType() ??
                throw new InvalidOperationException(
                    "Native AccessoriesBar selectable array has no element type.");
            Array result = Array.CreateInstance(
                elementType,
                nativeSelectables.Length +
                    productButtons.Count);
            Array.Copy(
                nativeSelectables,
                result,
                nativeSelectables.Length);
            for (int index = 0;
                 index < productButtons.Count;
                 index++)
            {
                result.SetValue(
                    productButtons[index],
                    nativeSelectables.Length + index);
            }
            return result;
        }

        internal void OnAccessoriesBarClear(
            object accessoriesBar)
        {
            UiSession? session = uiSession;
            if (session == null ||
                !ReferenceEquals(
                    session.AccessoriesBar,
                    accessoriesBar))
            {
                return;
            }
            session.Visible = false;
            HideHover();
            ApplyUiVisibility(session);
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
            (uiSession != null ? 1 : 0);

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
                pendingGameplayIncomingWithdrawal != null ||
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
                CapturePreSaveFingerprint();
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
            throw new InvalidOperationException(
                reason +
                ": pending gameplay placement was not resolved by the one immediate same-call observation; reload without native save to restore the committed projection.");
        }

        private void ReconcilePendingGameplayIncomingWithdrawal(
            string reason)
        {
            if (pendingGameplayIncomingWithdrawal == null)
                return;
            throw new InvalidOperationException(
                reason +
                ": an incoming native withdrawal has an unknown outcome; this process forbids every Product slot operation and native SaveGame. Return to title or restart without saving to restore the committed projection.",
                pendingGameplayIncomingWithdrawal);
        }

        private void ReconcilePendingJournalPlacements(
            string reason)
        {
            if (pendingJournalPlacements.Count == 0)
                return;
            throw new InvalidOperationException(
                reason +
                ": pending durable placement was not resolved by the one immediate same-call observation; reload without native save before another attempt.");
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
                CapturePreSaveFingerprint();
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
            MoreEquipmentSlotsItemAdmission admission =
                MoreEquipmentSlotsItemAdmissionPolicy.Decide(
                    isPassive,
                    isHat,
                    itemFunctionType,
                    skillId,
                    shield,
                    shieldMax);
            if (!admission.Accepted)
            {
                throw new InvalidOperationException(
                    admission.Reason);
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

        private void InitializeUiSession(
            UiSession session,
            int officialPassiveCount)
        {
            session.OfficialPassiveCount = officialPassiveCount;
            object accessoriesTransform = RequireMember(
                session.AccessoriesBar,
                "transform",
                "AccessoriesBar.transform");
            session.CharacterPanel = RequireMember(
                accessoriesTransform,
                "parent",
                "AccessoriesBar character-panel parent");
            session.WidgetRoot = RequireMember(
                session.CharacterPanel,
                "parent",
                "EquipmentBarWidget root");
            session.NativeDronePanel = FindChildByName(
                session.WidgetRoot,
                "drone_panel") ??
                throw new InvalidOperationException(
                    "Native EquipmentBarWidget has no drone_panel authority.");

            object sourceSlot = ResolveSourceSlot(
                session.AccessoriesBar,
                officialPassiveCount);
            session.ProductRowRoot = CreateProductRowRoot(
                session.CharacterPanel);
            SetIgnoreLayout(session.ProductRowRoot);
            object rowTransform = RequireMember(
                session.ProductRowRoot,
                "transform",
                "Product row transform");
            for (int index = 0;
                 index < MoreEquipmentSlotsProductContract
                    .FixedSlotCount;
                 index++)
            {
                object? clone = CloneUnityObject(
                    sourceSlot,
                    rowTransform);
                if (clone == null)
                {
                    throw new InvalidOperationException(
                        "Product slot " +
                        index +
                        " could not clone the native AccessorySlot.");
                }
                SetMember(
                    Read(clone, "gameObject"),
                    "name",
                    "DTMAPI.MoreEquipmentSlots.Slot." +
                    index);
                var lease = new UiLease(clone)
                {
                    Index = index
                };
                InitializeClonedSlotRuntimeMembers(clone);
                SanitizeClonedSlot(lease);
                int capturedIndex = index;
                BindProductButton(
                    lease,
                    () => OnUiClick(capturedIndex),
                    () => OnUiHover(capturedIndex),
                    HideHover);
                uiLeases.Add(lease);
            }

            RefreshUiLayout(session);
        }

        private MoreEquipmentSlotsUiLayout CalculateUiLayout(
            UiSession session,
            out UiBounds lastOfficialBounds)
        {
            lastOfficialBounds = default;
            object characterPanel =
                session.CharacterPanel ??
                throw new InvalidOperationException(
                    "Character equipment panel is unavailable.");
            var officialSlots = new List<object>();
            foreach (object slot in GetOfficialSlots(
                session.AccessoriesBar,
                session.OfficialPassiveCount))
            {
                officialSlots.Add(slot);
            }
            if (officialSlots.Count !=
                2 + session.OfficialPassiveCount)
            {
                return MoreEquipmentSlotsUiLayoutPolicy.Calculate(
                    0f,
                    0f,
                    0f,
                    session.OfficialPassiveCount);
            }

            UiBounds previousBounds = ReadRelativeBounds(
                officialSlots[officialSlots.Count - 2],
                characterPanel);
            lastOfficialBounds = ReadRelativeBounds(
                officialSlots[officialSlots.Count - 1],
                characterPanel);
            if (lastOfficialBounds.MinX <= previousBounds.MinX)
            {
                return MoreEquipmentSlotsUiLayoutPolicy.Calculate(
                    0f,
                    0f,
                    0f,
                    session.OfficialPassiveCount);
            }
            return MoreEquipmentSlotsUiLayoutPolicy.Calculate(
                lastOfficialBounds.MaxX -
                    lastOfficialBounds.MinX,
                lastOfficialBounds.MaxY -
                    lastOfficialBounds.MinY,
                lastOfficialBounds.MinX -
                    previousBounds.MaxX,
                session.OfficialPassiveCount);
        }

        private void RefreshUiLayout(UiSession session)
        {
            ForceCanvasLayout();
            MoreEquipmentSlotsUiLayout layout =
                CalculateUiLayout(
                    session,
                    out UiBounds lastOfficialBounds);
            if (!layout.Valid)
            {
                BlockUiLayout(session, layout.Reason);
                return;
            }
            if (session.ProductRowRoot == null)
            {
                BlockUiLayout(
                    session,
                    "The Product equipment-row root is incomplete.");
                return;
            }
            if (uiLeases.Count !=
                MoreEquipmentSlotsProductContract.FixedSlotCount)
            {
                BlockUiLayout(
                    session,
                    "The Product equipment row does not own exactly three slot clones.");
                return;
            }

            object characterPanel =
                session.CharacterPanel ??
                throw new InvalidOperationException(
                    "Character equipment panel is unavailable.");
            object characterRect =
                RequireRectTransform(characterPanel);
            GetRectSize(
                characterRect,
                out float characterWidth,
                out float characterHeight);
            object pivot = RequireMember(
                characterRect,
                "pivot",
                "Character equipment panel pivot");
            float parentMinX =
                -ReadFloat(pivot, "x") * characterWidth;
            float parentMinY =
                -ReadFloat(pivot, "y") * characterHeight;
            SetRectTransform(
                RequireRectTransform(session.ProductRowRoot),
                anchorX: 0f,
                anchorY: 0f,
                pivotX: 0f,
                pivotY: 0f,
                width: layout.ContentWidth,
                height: layout.ContentHeight,
                positionX:
                    lastOfficialBounds.MaxX +
                    MoreEquipmentSlotsUiLayoutPolicy
                        .SlotSpacing -
                    parentMinX,
                positionY:
                    lastOfficialBounds.MinY -
                    parentMinY);
            for (int index = 0;
                 index < uiLeases.Count;
                 index++)
            {
                SetRectTransform(
                    RequireRectTransform(
                        uiLeases[index].Slot),
                    anchorX: 0f,
                    anchorY: 0f,
                    pivotX: 0.5f,
                    pivotY: 0.5f,
                    width:
                        MoreEquipmentSlotsUiLayoutPolicy
                            .SlotSize,
                    height:
                        MoreEquipmentSlotsUiLayoutPolicy
                            .SlotSize,
                    positionX:
                        MoreEquipmentSlotsUiLayoutPolicy
                            .ProductSlotX(layout, index),
                    positionY: layout.SlotY);
            }
            ForceCanvasLayout();
            string? invalid = ValidateProductRow(session);
            if (invalid != null)
            {
                BlockUiLayout(session, invalid);
                return;
            }

            session.LayoutBlocked = false;
            session.LayoutMessage = layout.Reason;
            session.Visible = true;
            ApplyUiVisibility(session);
            RebuildEquipmentNavigation(session);
        }

        private string? ValidateProductRow(UiSession session)
        {
            object nativeDrone =
                session.NativeDronePanel ??
                throw new InvalidOperationException(
                    "The native drone panel is unavailable.");
            var officialSlots = new List<object>();
            foreach (object slot in GetOfficialSlots(
                session.AccessoriesBar,
                session.OfficialPassiveCount))
            {
                officialSlots.Add(slot);
            }

            foreach (UiLease lease in uiLeases)
            {
                object productSlot = lease.Slot;
                if (WorldRectsIntersect(
                    productSlot,
                    nativeDrone))
                {
                    return
                        "A Product equipment slot intersects the native drone panel; Product UI is hidden and release acceptance must stop.";
                }
                foreach (object officialSlot in officialSlots)
                {
                    if (WorldRectsIntersect(
                        productSlot,
                        officialSlot))
                    {
                        return
                            "A Product equipment slot intersects a native equipment slot; Product UI is hidden and release acceptance must stop.";
                    }
                }
            }
            return null;
        }

        private void ApplyUiVisibility(UiSession session)
        {
            SetUnityActive(
                session.ProductRowRoot,
                !session.LayoutBlocked && session.Visible);
        }

        private void BlockUiLayout(
            UiSession? session,
            string reason)
        {
            if (session != null)
            {
                bool changed =
                    !session.LayoutBlocked ||
                    !string.Equals(
                        session.LayoutMessage,
                        reason,
                        StringComparison.Ordinal);
                session.LayoutBlocked = true;
                session.Visible = false;
                session.LayoutMessage = reason ?? string.Empty;
                ApplyUiVisibility(session);
                if (!changed)
                    return;
            }
            lastMessage =
                "Product UI layout blocked: " +
                (reason ?? string.Empty);
            monitor.Log(
                "MoreEquipmentSlots Product UI failed closed: " +
                reason,
                LogLevel.Error);
        }

        private void ClearUi(string reason)
        {
            UiSession? session = uiSession;
            foreach (UiLease lease in uiLeases)
                lease.ClearListeners();
            uiLeases.Clear();
            DestroyUnityObject(session?.ProductRowRoot);
            uiSession = null;
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

        private static void SanitizeClonedSlot(UiLease lease)
        {
            string[] eventNames =
            {
                "onClick",
                "onSelect",
                "onDeselect",
                "onPointerEnter",
                "onPointerExit",
                "onPointerDown",
                "onPointerUp",
                "onLeftClick",
                "onLeftLongClick",
                "onRightClick",
                "onRightLongClick",
                "onAssistLeftClick",
                "onAssistRightClick",
                "onMove"
            };
            InvokeNoArg(lease.Slot, "ClearAllClickCallbacks");
            foreach (string eventName in eventNames)
                RemoveAllListeners(Read(lease.Slot, eventName));
            object? button = Read(lease.Slot, "button");
            InvokeNoArg(button, "ClearAllClickCallbacks");
            foreach (string eventName in eventNames)
                RemoveAllListeners(Read(button, eventName));
            SetMember(button, "onLeftContinuesClick", null);
            SetMember(button, "onRightContinuesClick", null);
        }

        private static void InitializeClonedSlotRuntimeMembers(
            object slot)
        {
            object? ownedTransform = Read(slot, "transform") ??
                Read(Read(slot, "gameObject"), "transform");
            if (ownedTransform == null)
            {
                throw new InvalidOperationException(
                    "The cloned Product AccessorySlot has no owned transform.");
            }
            object rectTransform = RequireRectTransform(
                ownedTransform);
            _ = SetMember(
                slot,
                "rectTransform",
                rectTransform);
            object? confirmed = Read(slot, "rectTransform");
            if (confirmed == null ||
                !ReferenceEquals(confirmed, rectTransform))
            {
                throw new InvalidOperationException(
                    "The cloned Product AccessorySlot could not bind its runtime RectTransform.");
            }

            object? parent = Read(rectTransform, "parent");
            if (parent != null)
                SetMember(slot, "parentRect", parent);
        }

        private static void BindProductButton(
            UiLease lease,
            Action click,
            Action hover,
            Action exit)
        {
            object button = RequireUiButton(lease.Slot);
            BindVoidEvent(
                Read(button, "onLeftClick"),
                click,
                lease);
            BindVoidEvent(
                Read(button, "onSelect"),
                hover,
                lease);
            BindVoidEvent(
                Read(button, "onPointerEnter"),
                hover,
                lease);
            BindVoidEvent(
                Read(button, "onDeselect"),
                exit,
                lease);
            BindVoidEvent(
                Read(button, "onPointerExit"),
                exit,
                lease);
        }

        private static void BindVoidEvent(
            object? unityEvent,
            Action action,
            UiLease lease)
        {
            if (unityEvent == null)
                return;
            MethodInfo? add = FindMethod(
                unityEvent.GetType(),
                "AddListener",
                1);
            if (add == null)
                return;
            var binder = new VoidCallbackBinder(action);
            Type delegateType =
                add.GetParameters()[0].ParameterType;
            Delegate callback = Delegate.CreateDelegate(
                delegateType,
                binder,
                typeof(VoidCallbackBinder).GetMethod(
                    nameof(VoidCallbackBinder.Invoke))!);
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

            object? taken;
            try
            {
                MethodInfo? take = FindMethod(
                    buffer.GetType(),
                    "Take",
                    0);
                if (take == null)
                {
                    throw new MissingMethodException(
                        buffer.GetType().FullName,
                        "Take()");
                }
                taken = take.Invoke(buffer, null);
            }
            catch (Exception ex)
            {
                throw EquipmentSlotNativeMutationEvidence.Unknown(
                    itemId,
                    1,
                    0,
                    0,
                    "Native held-item buffer Take threw after mutation may have begun.",
                    ex);
            }

            object? after;
            try
            {
                after = Read(buffer, "CurrentItem");
            }
            catch (Exception ex)
            {
                throw EquipmentSlotNativeMutationEvidence.Unknown(
                    itemId,
                    1,
                    0,
                    0,
                    "Native held-item buffer post-state is unreadable.",
                    ex);
            }
            succeeded =
                taken != null &&
                string.Equals(
                    ReadString(taken, "name"),
                    itemId,
                    StringComparison.Ordinal) &&
                after == null;
            if (!succeeded)
            {
                throw EquipmentSlotNativeMutationEvidence.Unknown(
                    itemId,
                    1,
                    0,
                    0,
                    "Native held-item buffer returned contradictory withdrawal evidence; the matching item was not both returned and removed from the buffer.");
            }
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
            if (lease != null)
            {
                ShowSlotHint(
                    lease.Slot,
                    nativeItem,
                    string.Format(
                        CultureInfo.CurrentCulture,
                        slotUiText,
                        slotIndex + 1));
            }
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
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: Type.EmptyTypes,
                modifiers: null);
            method?.Invoke(null, null);
        }

        private static void ShowSlotHint(
            object slot,
            object? nativeItem,
            string text)
        {
            FindMethod(
                slot.GetType(),
                "ShowEquipmentItemViewer",
                2)?.Invoke(
                    slot,
                    new object?[]
                    {
                        nativeItem,
                        text ?? string.Empty
                    });
        }

        private static object RequireMember(
            object target,
            string name,
            string authority) =>
            Read(target, name) ??
            throw new InvalidOperationException(
                authority + " is unavailable.");

        private static object ResolveSourceSlot(
            object accessoriesBar,
            int officialPassiveCount)
        {
            if (officialPassiveCount > 0)
            {
                MethodInfo? getPassive = FindMethod(
                    accessoriesBar.GetType(),
                    "GetPassiveSlotByIndex",
                    1);
                object? passive = getPassive?.Invoke(
                    accessoriesBar,
                    new object[] { 0 });
                if (passive != null)
                    return passive;
            }
            return Read(accessoriesBar, "positiveItem") ??
                throw new InvalidOperationException(
                    "Native AccessoriesBar exposes no cloneable slot.");
        }

        private static IEnumerable<object> GetOfficialSlots(
            object accessoriesBar,
            int officialPassiveCount)
        {
            object? hat = Read(accessoriesBar, "hatItem");
            if (hat != null)
                yield return hat;
            object? positive =
                Read(accessoriesBar, "positiveItem");
            if (positive != null)
                yield return positive;
            MethodInfo? getPassive = FindMethod(
                accessoriesBar.GetType(),
                "GetPassiveSlotByIndex",
                1);
            if (getPassive == null)
                yield break;
            for (int index = 0;
                 index < officialPassiveCount;
                 index++)
            {
                object? passive = getPassive.Invoke(
                    accessoriesBar,
                    new object[] { index });
                if (passive != null)
                    yield return passive;
            }
        }

        private static object? FindChildByName(
            object parentTransform,
            string name)
        {
            int childCount = ReadInt(
                parentTransform,
                "childCount");
            MethodInfo? getChild = FindMethod(
                parentTransform.GetType(),
                "GetChild",
                1);
            if (getChild == null)
                return null;
            for (int index = 0; index < childCount; index++)
            {
                object? child = getChild.Invoke(
                    parentTransform,
                    new object[] { index });
                if (child == null)
                    continue;
                string childName =
                    ReadString(
                        Read(child, "gameObject"),
                        "name");
                if (string.Equals(
                    childName,
                    name,
                    StringComparison.Ordinal))
                {
                    return child;
                }
            }
            for (int index = 0; index < childCount; index++)
            {
                object? child = getChild.Invoke(
                    parentTransform,
                    new object[] { index });
                if (child == null)
                    continue;
                object? found = FindChildByName(
                    child,
                    name);
                if (found != null)
                    return found;
            }
            return null;
        }

        private static object CreateProductRowRoot(
            object characterPanel)
        {
            Type gameObjectType = RequireRuntimeType(
                "UnityEngine.GameObject");
            Type rectTransformType = RequireRuntimeType(
                "UnityEngine.RectTransform");
            object root = Activator.CreateInstance(
                gameObjectType,
                new object[]
                {
                    "DTMAPI.MoreEquipmentSlots.Row",
                    new[]
                    {
                        rectTransformType
                    }
                }) ??
                throw new InvalidOperationException(
                    "Unity could not create the Product equipment-row root.");
            object transform = RequireMember(
                root,
                "transform",
                "Product equipment-row transform");
            SetTransformParent(
                transform,
                characterPanel);
            InvokeNoArg(transform, "SetAsLastSibling");
            return root;
        }

        private static void SetIgnoreLayout(
            object target,
            float? preferredWidth = null,
            float? preferredHeight = null)
        {
            object gameObject =
                Read(target, "gameObject") ?? target;
            Type layoutElementType = RequireRuntimeType(
                "UnityEngine.UI.LayoutElement");
            object layout = GetComponent(
                gameObject,
                layoutElementType) ??
                AddComponent(
                    gameObject,
                    layoutElementType) ??
                throw new InvalidOperationException(
                    "Product UI root could not acquire LayoutElement.");
            SetMember(layout, "ignoreLayout", true);
            if (preferredWidth.HasValue)
            {
                SetMember(
                    layout,
                    "preferredWidth",
                    preferredWidth.Value);
            }
            if (preferredHeight.HasValue)
            {
                SetMember(
                    layout,
                    "preferredHeight",
                    preferredHeight.Value);
            }
        }

        private static object RequireRectTransform(object target)
        {
            if (string.Equals(
                target.GetType().FullName,
                "UnityEngine.RectTransform",
                StringComparison.Ordinal))
            {
                return target;
            }
            object? rect = Read(target, "rectTransform");
            if (rect != null)
                return rect;
            object? transform = Read(target, "transform");
            if (transform != null &&
                string.Equals(
                    transform.GetType().FullName,
                    "UnityEngine.RectTransform",
                    StringComparison.Ordinal))
            {
                return transform;
            }
            object? gameObject = Read(target, "gameObject");
            transform = Read(gameObject, "transform");
            if (transform != null)
                return transform;
            throw new InvalidOperationException(
                "Product UI target has no RectTransform.");
        }

        private static void GetRectSize(
            object target,
            out float width,
            out float height)
        {
            object rectTransform = RequireRectTransform(target);
            object rect = RequireMember(
                rectTransform,
                "rect",
                "RectTransform.rect");
            width = ReadFloat(rect, "width");
            height = ReadFloat(rect, "height");
            if (width <= 0f || height <= 0f)
            {
                throw new InvalidOperationException(
                    "Product UI target has an unreadable or empty Rect.");
            }
        }

        private static void SetRectTransform(
            object rectTransform,
            float anchorX,
            float anchorY,
            float pivotX,
            float pivotY,
            float width,
            float height,
            float positionX,
            float positionY)
        {
            object anchor = CreateUnityValue(
                "UnityEngine.Vector2",
                anchorX,
                anchorY);
            SetMember(rectTransform, "anchorMin", anchor);
            SetMember(rectTransform, "anchorMax", anchor);
            SetMember(
                rectTransform,
                "pivot",
                CreateUnityValue(
                    "UnityEngine.Vector2",
                    pivotX,
                    pivotY));
            SetMember(
                rectTransform,
                "sizeDelta",
                CreateUnityValue(
                    "UnityEngine.Vector2",
                    width,
                    height));
            SetMember(
                rectTransform,
                "anchoredPosition",
                CreateUnityValue(
                    "UnityEngine.Vector2",
                    positionX,
                    positionY));
        }

        private static void ForceCanvasLayout()
        {
            Type? canvas = ResolveRuntimeType(
                "UnityEngine.Canvas");
            canvas?.GetMethod(
                "ForceUpdateCanvases",
                BindingFlags.Public |
                BindingFlags.Static,
                binder: null,
                types: Type.EmptyTypes,
                modifiers: null)?.Invoke(null, null);
        }

        private static bool WorldRectsIntersect(
            object left,
            object right)
        {
            UiBounds a = ReadWorldBounds(left);
            UiBounds b = ReadWorldBounds(right);
            const float epsilon = 0.5f;
            return a.MinX < b.MaxX - epsilon &&
                a.MaxX > b.MinX + epsilon &&
                a.MinY < b.MaxY - epsilon &&
                a.MaxY > b.MinY + epsilon;
        }

        private static UiBounds ReadRelativeBounds(
            object target,
            object relativeTo)
        {
            object targetRect = RequireRectTransform(target);
            object relativeRect = RequireRectTransform(relativeTo);
            Type vector3Type = RequireRuntimeType(
                "UnityEngine.Vector3");
            Array corners = Array.CreateInstance(vector3Type, 4);
            MethodInfo? getWorldCorners = FindMethod(
                targetRect.GetType(),
                "GetWorldCorners",
                1);
            MethodInfo? inverseTransformPoint = FindMethod(
                relativeRect.GetType(),
                "InverseTransformPoint",
                1);
            if (getWorldCorners == null ||
                inverseTransformPoint == null)
            {
                throw new MissingMethodException(
                    "RectTransform.GetWorldCorners/Transform.InverseTransformPoint");
            }
            getWorldCorners.Invoke(
                targetRect,
                new object[] { corners });
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;
            foreach (object corner in corners)
            {
                object local = inverseTransformPoint.Invoke(
                    relativeRect,
                    new[] { corner }) ??
                    throw new InvalidOperationException(
                        "RectTransform.InverseTransformPoint returned no coordinate.");
                float x = ReadFloat(local, "x");
                float y = ReadFloat(local, "y");
                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }
            return new UiBounds(minX, minY, maxX, maxY);
        }

        private static UiBounds ReadWorldBounds(object target)
        {
            object rectTransform = RequireRectTransform(target);
            Type vector3Type = RequireRuntimeType(
                "UnityEngine.Vector3");
            Array corners = Array.CreateInstance(
                vector3Type,
                4);
            MethodInfo? getWorldCorners = FindMethod(
                rectTransform.GetType(),
                "GetWorldCorners",
                1);
            if (getWorldCorners == null)
            {
                throw new MissingMethodException(
                    rectTransform.GetType().FullName,
                    "GetWorldCorners(Vector3[])");
            }
            getWorldCorners.Invoke(
                rectTransform,
                new object[] { corners });
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;
            foreach (object corner in corners)
            {
                float x = ReadFloat(corner, "x");
                float y = ReadFloat(corner, "y");
                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }
            return new UiBounds(minX, minY, maxX, maxY);
        }

        private static void RebuildEquipmentNavigation(
            UiSession session)
        {
            Type? panelType = typeof(DolocAPI).Assembly.GetType(
                "DolocTown.UI.EquipmentBarPanel",
                throwOnError: false);
            if (panelType == null)
                return;
            object? current =
                Read(session.AccessoriesBar, "transform");
            while (current != null)
            {
                object? gameObject = Read(
                    current,
                    "gameObject");
                object? panel = gameObject == null
                    ? null
                    : GetComponent(
                        gameObject,
                        panelType);
                if (panel != null)
                {
                    InvokeNoArg(panel, "RebuildNavigation");
                    return;
                }
                current = Read(current, "parent");
            }
        }

        private static object RequireUiButton(object slot) =>
            Read(slot, "button") ??
            throw new InvalidOperationException(
                "Product AccessorySlot exposes no native Selectable button.");

        private static void SetUnityActive(
            object? gameObject,
            bool active)
        {
            if (gameObject == null)
                return;
            MethodInfo? setActive = FindMethod(
                gameObject.GetType(),
                "SetActive",
                1);
            setActive?.Invoke(
                gameObject,
                new object[] { active });
        }

        private static void RemoveAllListeners(
            object? unityEvent)
        {
            if (unityEvent != null)
                InvokeNoArg(unityEvent, "RemoveAllListeners");
        }

        private static Type RequireRuntimeType(string fullName) =>
            ResolveRuntimeType(fullName) ??
            throw new TypeLoadException(fullName);

        private static Type? ResolveRuntimeType(string fullName)
        {
            string[] assemblies =
            {
                "UnityEngine.CoreModule",
                "UnityEngine.UI",
                "UnityEngine"
            };
            foreach (string assembly in assemblies)
            {
                Type? type = Type.GetType(
                    fullName + ", " + assembly,
                    throwOnError: false);
                if (type != null)
                    return type;
            }
            foreach (Assembly assembly in
                AppDomain.CurrentDomain.GetAssemblies())
            {
                Type? type = assembly.GetType(
                    fullName,
                    throwOnError: false);
                if (type != null)
                    return type;
            }
            return null;
        }

        private static object CreateUnityValue(
            string typeName,
            params object[] values) =>
            Activator.CreateInstance(
                RequireRuntimeType(typeName),
                values) ??
            throw new InvalidOperationException(
                "Could not construct " + typeName + ".");

        private static object? GetComponent(
            object gameObject,
            Type componentType) =>
            InvokeTypeComponentMethod(
                gameObject,
                "GetComponent",
                componentType);

        private static object? AddComponent(
            object gameObject,
            Type componentType) =>
            InvokeTypeComponentMethod(
                gameObject,
                "AddComponent",
                componentType);

        private static object? InvokeTypeComponentMethod(
            object gameObject,
            string methodName,
            Type componentType)
        {
            foreach (MethodInfo method in gameObject.GetType()
                .GetMethods(AllMembers))
            {
                ParameterInfo[] parameters = method.GetParameters();
                if (method.Name == methodName &&
                    !method.IsGenericMethod &&
                    parameters.Length == 1 &&
                    parameters[0].ParameterType == typeof(Type))
                {
                    return method.Invoke(
                        gameObject,
                        new object[] { componentType });
                }
            }
            return null;
        }

        private static void SetTransformParent(
            object transform,
            object parent)
        {
            MethodInfo? setParent = FindMethod(
                transform.GetType(),
                "SetParent",
                2);
            if (setParent != null)
            {
                setParent.Invoke(
                    transform,
                    new object[] { parent, false });
                return;
            }
            setParent = FindMethod(
                transform.GetType(),
                "SetParent",
                1);
            if (setParent == null)
            {
                throw new MissingMethodException(
                    transform.GetType().FullName,
                    "SetParent(Transform,bool)");
            }
            setParent.Invoke(
                transform,
                new[] { parent });
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

        private void RefreshNativeShieldStatus()
        {
            try
            {
                InvokeNoArg(
                    Read(
                        ReadStatic(
                            typeof(DolocAPI),
                            "uiSystem"),
                        "agentStatusBar"),
                    "UpdateHealth");
            }
            catch (Exception ex)
            {
                monitor.Log(
                    "MoreEquipmentSlots could not refresh the native health/shield status UI: " +
                    ex.Message,
                    LogLevel.Warn);
            }
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
            Type? unityObject = ResolveRuntimeType(
                "UnityEngine.Object");
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
            Type? unityObject = ResolveRuntimeType(
                "UnityEngine.Object");
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

        private sealed class UiSession
        {
            internal UiSession(object accessoriesBar) =>
                AccessoriesBar = accessoriesBar ??
                    throw new ArgumentNullException(
                        nameof(accessoriesBar));

            internal object AccessoriesBar { get; }

            internal object? CharacterPanel { get; set; }

            internal object? WidgetRoot { get; set; }

            internal object? NativeDronePanel { get; set; }

            internal object? ProductRowRoot { get; set; }

            internal int OfficialPassiveCount { get; set; }

            internal bool Visible { get; set; }

            internal bool LayoutBlocked { get; set; }

            internal string LayoutMessage { get; set; } =
                string.Empty;

        }

        private readonly struct UiBounds
        {
            internal UiBounds(
                float minX,
                float minY,
                float maxX,
                float maxY)
            {
                MinX = minX;
                MinY = minY;
                MaxX = maxX;
                MaxY = maxY;
            }

            internal float MinX { get; }

            internal float MinY { get; }

            internal float MaxX { get; }

            internal float MaxY { get; }
        }

        private sealed class VoidCallbackBinder
        {
            private readonly Action callback;

            internal VoidCallbackBinder(
                Action callback) =>
                this.callback = callback;

            public void Invoke() => callback();
        }
    }
}
