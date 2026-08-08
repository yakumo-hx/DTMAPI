using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using DTMAPI.Abstractions;
using DTMAPI.MoreEquipmentSlots;
using static DTMAPI.GameBridge.DolocTown.DolocTownExperimentalBridgeApi;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed partial class EquipmentSlotsCompatibilityService
    {
        internal void NotifyEquipmentSlotsSaveSaving(int? slot)
        {
            ReconcileEquipmentSlotWorkingPlacementGuards(slot);
            ReconcileEquipmentSlotUnknownJournals(slot);
            NotifyProductColdRecoverySaveSaving(slot);
            PrepareEquipmentSlotGameplayCandidatesBeforeNativeSave(slot);
            foreach (KeyValuePair<string, EquipmentSlotTransactionJournal> pair in equipmentSlotJournals.ToArray())
            {
                EquipmentSlotTransactionJournal journal = pair.Value;
                if (journal.State != EquipmentSlotTransactionState.Prepared)
                    continue;
                if (journal.Origin != EquipmentSlotTransactionOrigin.OwnerRecovery &&
                    journal.Origin != EquipmentSlotTransactionOrigin.OrphanRecovery)
                {
                    throw new InvalidOperationException(
                        "EquipmentSlots refused an untyped or gameplay journal before native SaveGame for " +
                        pair.Key +
                        ".");
                }

                journal.AttemptStarted = true;
                if (string.IsNullOrWhiteSpace(journal.PreSaveFingerprint))
                    journal.PreSaveFingerprint = GetNativeSaveFingerprint(slot ?? journal.ArchiveIndex);
                if (!PersistEquipmentSlotStorage(pair.Key, "SaveSaving journal preimage"))
                {
                    throw new InvalidOperationException(
                        "EquipmentSlots could not persist prepared journal before native SaveGame for " + pair.Key + ".");
                }
            }
        }

        private void CommitEquipmentSlotJournalsAfterNativeSave(int? slot)
        {
            foreach (KeyValuePair<string, EquipmentSlotTransactionJournal> pair in equipmentSlotJournals.ToArray())
            {
                string ownerId = pair.Key;
                EquipmentSlotTransactionJournal journal = pair.Value;
                if (journal.Origin != EquipmentSlotTransactionOrigin.OwnerRecovery &&
                    journal.Origin != EquipmentSlotTransactionOrigin.OrphanRecovery)
                {
                    throw new InvalidOperationException(
                        "EquipmentSlots refused to promote an untyped or gameplay recovery journal for " +
                        ownerId +
                        ".");
                }
                if (journal.State == EquipmentSlotTransactionState.Prepared &&
                    (!journal.AttemptStarted ||
                        journal.Placement == NativeEquipmentPlacementKind.Failure))
                {
                    continue;
                }
                if (journal.State == EquipmentSlotTransactionState.Prepared)
                {
                    journal.State = EquipmentSlotTransactionState.NativeCommitted;
                    journal.PostSaveFingerprint = GetNativeSaveFingerprint(slot ?? journal.ArchiveIndex);
                    if (!PersistEquipmentSlotStorage(ownerId, "SaveSaved native-committed journal"))
                    {
                        throw new InvalidOperationException(
                            "EquipmentSlots native save completed, but its NativeCommitted journal could not be persisted for " + ownerId + ".");
                    }
                }

                if (journal.State == EquipmentSlotTransactionState.NativeCommitted)
                {
                    journal.State = EquipmentSlotTransactionState.Committed;
                    if (!PersistEquipmentSlotStorage(ownerId, "SaveSaved committed journal"))
                    {
                        journal.State = EquipmentSlotTransactionState.NativeCommitted;
                        throw new InvalidOperationException(
                            "EquipmentSlots could not persist the committed journal boundary for " + ownerId + ".");
                    }
                }

                equipmentSlotJournals.Remove(ownerId);
                if (!PersistEquipmentSlotStorage(ownerId, "SaveSaved journal cleanup"))
                {
                    equipmentSlotJournals[ownerId] = journal;
                    throw new InvalidOperationException(
                        "EquipmentSlots could not clean the committed journal for " + ownerId + ".");
                }
                dirtyEquipmentSlotStorageOwners.Remove(ownerId);
                if (equipmentSlotEntries.TryGetValue(
                    ownerId,
                    out List<EquipmentSlotRuntimeEntry>? committedEntries))
                {
                    foreach (EquipmentSlotRuntimeEntry entry in
                        committedEntries)
                    {
                        entry.CommittedItemReleasedToNative =
                            false;
                    }
                    committedEquipmentSlotEntries[ownerId] =
                        CloneEquipmentSlotRuntimeEntries(
                            committedEntries);
                }
            }
        }

        private void PrepareEquipmentSlotGameplayCandidatesBeforeNativeSave(
            int? slot)
        {
            if (dirtyEquipmentSlotStorageOwners.Count == 0)
                return;

            EquipmentSlotSaveScope scope =
                GetEquipmentSlotSaveScope();
            if (!scope.HasArchive)
            {
                throw new InvalidOperationException(
                    "EquipmentSlots cannot prepare gameplay state without an exact loaded archive.");
            }
            string fingerprint =
                GetNativeSaveFingerprint(
                    slot ?? scope.ArchiveIndex);
            if (string.IsNullOrWhiteSpace(fingerprint))
            {
                throw new InvalidOperationException(
                    "EquipmentSlots cannot prepare gameplay state without the native save preimage fingerprint.");
            }

            foreach (string ownerId in
                dirtyEquipmentSlotStorageOwners.ToArray())
            {
                if (equipmentSlotJournals.ContainsKey(ownerId))
                    continue;
                if (!equipmentSlotEntries.TryGetValue(
                    ownerId,
                    out List<EquipmentSlotRuntimeEntry>? working))
                {
                    continue;
                }
                if (!committedEquipmentSlotEntries.TryGetValue(
                    ownerId,
                    out List<EquipmentSlotRuntimeEntry>? committed))
                {
                    committed =
                        CloneEquipmentSlotRuntimeEntries(working);
                    committedEquipmentSlotEntries[ownerId] =
                        committed;
                }

                int totalSlots = Math.Max(
                    working.Count,
                    working.Count == 0
                        ? 0
                        : working.Max(entry => entry.Index) + 1);
                var candidate =
                    new EquipmentSlotCompatibilityGameplayCandidate
                    {
                        TransactionId =
                            Guid.NewGuid().ToString("N"),
                        State =
                            EquipmentSlotGameplayCandidateState
                                .Prepared,
                        Origin =
                            EquipmentSlotTransactionOrigin
                                .GameplayMutation,
                        Scope =
                            EquipmentSlotCompatibilityCandidateScope
                                .From(scope),
                        BaseGeneration =
                            equipmentSlotStorageGenerations.TryGetValue(
                                ownerId,
                                out long generation)
                                ? generation
                                : 0,
                        PreSaveFingerprint = fingerprint,
                        WorkingSlots =
                            ToEquipmentSlotStorageEntries(
                                working,
                                totalSlots)
                    };
                Type? dolocApi =
                    ResolveType("DolocAPI, Assembly-CSharp");
                IEnumerable<string> itemIds =
                    committed.Concat(working)
                        .Select(entry => entry.ItemId)
                        .Where(itemId =>
                            !string.IsNullOrWhiteSpace(itemId))
                        .Distinct(StringComparer.Ordinal);
                foreach (string itemId in itemIds)
                {
                    candidate.NativeExpectations.Add(
                        new EquipmentSlotNativeExpectation
                        {
                            ItemId = itemId,
                            BackpackCount =
                                CountNativeBackpackItem(
                                    dolocApi,
                                    itemId),
                            MailCount =
                                dolocApi == null
                                    ? 0
                                    : CountPendingUnacceptedItemMail(
                                        dolocApi,
                                        itemId)
                        });
                }

                equipmentSlotGameplayCandidates[ownerId] =
                    candidate;
                if (!PersistEquipmentSlotStorage(
                    ownerId,
                    "SaveSaving gameplay candidate"))
                {
                    equipmentSlotGameplayCandidates.Remove(ownerId);
                    throw new InvalidOperationException(
                        "EquipmentSlots could not persist the gameplay candidate before native SaveGame for " +
                        ownerId +
                        ".");
                }
            }
        }

        private void CommitEquipmentSlotGameplayCandidatesAfterNativeSave(
            int? slot)
        {
            foreach (KeyValuePair<string, EquipmentSlotCompatibilityGameplayCandidate> pair in
                equipmentSlotGameplayCandidates.ToArray())
            {
                string ownerId = pair.Key;
                EquipmentSlotCompatibilityGameplayCandidate candidate =
                    pair.Value;
                if (candidate.State ==
                    EquipmentSlotGameplayCandidateState
                        .Prepared)
                {
                    string fingerprint =
                        GetNativeSaveFingerprint(
                            slot ?? candidate.Scope.ArchiveIndex);
                    if (string.IsNullOrWhiteSpace(fingerprint))
                    {
                        throw new InvalidOperationException(
                            "EquipmentSlots native SaveGame completed, but its post-save fingerprint was unavailable for " +
                            ownerId +
                            ".");
                    }

                    candidate.State =
                        EquipmentSlotGameplayCandidateState
                            .CommittedTombstone;
                    candidate.PostSaveFingerprint =
                        fingerprint;
                    List<EquipmentSlotRuntimeEntry>? previousCommitted =
                        committedEquipmentSlotEntries.TryGetValue(
                            ownerId,
                            out List<EquipmentSlotRuntimeEntry>? existingCommitted)
                            ? CloneEquipmentSlotRuntimeEntries(
                                existingCommitted)
                            : null;
                    if (equipmentSlotEntries.TryGetValue(
                        ownerId,
                        out List<EquipmentSlotRuntimeEntry>? working))
                    {
                        foreach (EquipmentSlotRuntimeEntry entry in
                            working)
                        {
                            entry.CommittedItemReleasedToNative =
                                false;
                        }
                        committedEquipmentSlotEntries[ownerId] =
                            CloneEquipmentSlotRuntimeEntries(
                                working);
                    }
                    if (!PersistEquipmentSlotStorage(
                        ownerId,
                        "SaveSaved committed gameplay tombstone"))
                    {
                        candidate.State =
                            EquipmentSlotGameplayCandidateState
                                .Prepared;
                        candidate.PostSaveFingerprint =
                            string.Empty;
                        if (previousCommitted == null)
                            committedEquipmentSlotEntries.Remove(ownerId);
                        else
                            committedEquipmentSlotEntries[ownerId] =
                                previousCommitted;
                        throw new InvalidOperationException(
                            "EquipmentSlots could not persist the committed gameplay tombstone for " +
                            ownerId +
                            ".");
                    }
                }

                equipmentSlotGameplayCandidates.Remove(ownerId);
                if (!PersistEquipmentSlotStorage(
                    ownerId,
                    "SaveSaved gameplay candidate cleanup"))
                {
                    equipmentSlotGameplayCandidates[ownerId] =
                        candidate;
                    throw new InvalidOperationException(
                        "EquipmentSlots could not clean the committed gameplay candidate for " +
                        ownerId +
                        ".");
                }
                dirtyEquipmentSlotStorageOwners.Remove(ownerId);
            }
        }

        private bool PrepareEquipmentSlotOwnerRecoveryProjection(
            string ownerId,
            out string message)
        {
            message = string.Empty;
            ownerId ??= string.Empty;
            bool hasWorking =
                dirtyEquipmentSlotStorageOwners.Contains(ownerId) ||
                equipmentSlotGameplayCandidates.ContainsKey(ownerId) ||
                equipmentSlotWorkingPlacementGuards.ContainsKey(
                    ownerId);
            if (!hasWorking)
                return true;
            message =
                "OwnerRecovery was deferred because uncommitted gameplay state must first reach SaveSaved or be discarded at title/restart.";
            return false;
        }

        private bool RecoverEquipmentSlotEntryTransactional(
            string ownerId,
            EquipmentSlotRuntimeEntry entry,
            string reason,
            EquipmentSlotTransactionOrigin origin,
            bool saveAfterRecovery,
            out string message,
            out int recoveredCount)
        {
            recoveredCount = 0;
            ownerId ??= string.Empty;
            if (entry == null || string.IsNullOrWhiteSpace(entry.ItemId))
            {
                message = "Slot is already empty.";
                return true;
            }
            if (equipmentSlotJournals.ContainsKey(ownerId))
            {
                message = "A protected EquipmentSlots transaction is already pending for " + ownerId + "; save or restart before another transfer.";
                return false;
            }
            if (origin != EquipmentSlotTransactionOrigin.OwnerRecovery &&
                origin != EquipmentSlotTransactionOrigin.OrphanRecovery)
            {
                message =
                    "Only explicit owner/orphan recovery may create a durable EquipmentSlots placement journal.";
                return false;
            }
            if (dirtyEquipmentSlotStorageOwners.Contains(ownerId) ||
                equipmentSlotGameplayCandidates.ContainsKey(ownerId))
            {
                message =
                    "Uncommitted gameplay state must be discarded at the native no-save boundary before owner/orphan recovery can begin.";
                return false;
            }

            string itemId = entry.ItemId;
            string display = FirstText(entry.DisplayName, itemId);
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            if (dolocApi == null)
            {
                message = "DolocAPI is unavailable.";
                return false;
            }

            int beforeBackpack;
            int beforeMail;
            string preSaveFingerprint;
            try
            {
                beforeBackpack =
                    CountNativeBackpackItemStrict(
                        dolocApi,
                        itemId);
                beforeMail =
                    CountPendingUnacceptedItemMail(
                        dolocApi,
                        itemId);
                preSaveFingerprint =
                    GetNativeSaveFingerprint(
                        GetEquipmentSlotSaveScope()
                            .ArchiveIndex);
            }
            catch (Exception ex)
            {
                message =
                    "Recovery stopped before native mutation because its backpack/mail preflight was unreadable: " +
                    ex.Message;
                return false;
            }

            var journal = EquipmentSlotTransactionJournal.FromEntry(
                GetEquipmentSlotSaveScope().ArchiveIndex,
                entry,
                beforeBackpack,
                beforeMail,
                preSaveFingerprint,
                origin);
            journal.AttemptStarted = true;
            equipmentSlotJournals[ownerId] = journal;

            object? manager = GetNativeAgentEquipmentManager();
            if (manager != null)
                RemoveEquipmentSlotFunction(manager, entry);
            ClearEquipmentSlotStoredItem(entry);
            if (!PersistEquipmentSlotStorage(ownerId, "prepared sidecar-to-native journal"))
            {
                journal.Restore(entry);
                equipmentSlotJournals.Remove(ownerId);
                message = "Recovery stopped before native mutation because the prepared journal could not be persisted.";
                return false;
            }

            NativeEquipmentPlacement placement;
            try
            {
                placement =
                    PlaceNativeItemWithImmediateEvidence(
                        itemId,
                        1);
            }
            catch (
                EquipmentSlotNativeMutationOutcomeUnknownException
                    ex)
            {
                message =
                    "Native placement outcome is unknown; durable escrow remains prepared and SaveGame/retry is blocked until exact evidence reconciles it. " +
                    ex.Message;
                return false;
            }
            if (!placement.Success)
            {
                journal.Restore(entry);
                equipmentSlotJournals.Remove(ownerId);
                if (!PersistEquipmentSlotStorage(ownerId, "rollback failed native placement"))
                {
                    equipmentSlotJournals[ownerId] = journal;
                    ClearEquipmentSlotStoredItem(entry);
                    message = "Native placement failed and sidecar rollback could not be persisted; journal escrow remains fail-closed. " + placement.Message;
                    return false;
                }
                message = "Recovery failed for " + display + ": " + placement.Message;
                return false;
            }

            journal.Placement = placement.Kind;
            journal.ExpectedBackpackCount =
                placement.AfterBackpackCount;
            journal.ExpectedMailCount =
                placement.AfterMailCount;
            entry.LastMessage = "Recovered " + display + " from DTMAPI extra slot reason=" +
                (reason ?? string.Empty) + ". " + placement.Message;
            if (!PersistEquipmentSlotStorage(ownerId, "native placement pending SaveGame"))
            {
                message = "Native placement succeeded, but the durable pending-save journal update failed; retained prepared escrow requires restart reconciliation.";
                return false;
            }

            dirtyEquipmentSlotStorageOwners.Add(ownerId);
            recoveredCount = 1;
            message = entry.LastMessage;
            if (manager != null)
                InvokeNativeReloadParams(manager);
            return true;
        }

        private bool RecoverEquipmentSlotEntryWorking(
            string ownerId,
            EquipmentSlotRuntimeEntry entry,
            string reason,
            out string message,
            out int recoveredCount)
        {
            recoveredCount = 0;
            ownerId ??= string.Empty;
            if (entry == null ||
                string.IsNullOrWhiteSpace(entry.ItemId))
            {
                message = "Slot is already empty.";
                return true;
            }
            if (equipmentSlotJournals.ContainsKey(ownerId) ||
                equipmentSlotGameplayCandidates.ContainsKey(ownerId) ||
                equipmentSlotWorkingPlacementGuards.ContainsKey(
                    ownerId))
            {
                message =
                    "An EquipmentSlots transaction or outcome-unknown native placement is already pending; reconcile, save, or reload before another gameplay mutation.";
                return false;
            }

            string itemId = entry.ItemId;
            string display =
                FirstText(entry.DisplayName, itemId);
            EquipmentSlotSaveScope scope =
                GetEquipmentSlotSaveScope();
            if (!scope.HasArchive)
            {
                message =
                    "Recovery stopped before native mutation because no exact save scope is loaded.";
                return false;
            }
            string preSaveFingerprint =
                GetNativeSaveFingerprint(scope.ArchiveIndex);
            NativeEquipmentPlacement placement;
            try
            {
                placement =
                    PlaceNativeItemWithImmediateEvidence(
                        itemId,
                        1);
            }
            catch (
                EquipmentSlotNativeMutationOutcomeUnknownException
                    ex)
            {
                equipmentSlotWorkingPlacementGuards[ownerId] =
                    new EquipmentSlotWorkingPlacementGuard(
                        entry.Index,
                        itemId,
                        preSaveFingerprint,
                        reason,
                        ex);
                message =
                    "Recovery retained its Working sidecar item and blocked retry/SaveGame because the native placement outcome is unknown. " +
                    ex.Message;
                return false;
            }
            catch (InvalidDataException ex)
            {
                message =
                    "Recovery stopped before native mutation because its backpack/mail preflight was unreadable: " +
                    ex.Message;
                return false;
            }
            if (!placement.Success)
            {
                message =
                    "Recovery failed for " +
                    display +
                    ": " +
                    placement.Message;
                return false;
            }

            CommitEquipmentSlotWorkingPlacement(
                ownerId,
                entry,
                display,
                reason,
                placement);
            recoveredCount = 1;
            message = entry.LastMessage;
            return true;
        }

        private void CommitEquipmentSlotWorkingPlacement(
            string ownerId,
            EquipmentSlotRuntimeEntry entry,
            string display,
            string reason,
            NativeEquipmentPlacement placement)
        {
            object? manager =
                GetNativeAgentEquipmentManager();
            if (manager != null)
                RemoveEquipmentSlotFunction(manager, entry);
            if (committedEquipmentSlotEntries.TryGetValue(
                    ownerId,
                    out List<EquipmentSlotRuntimeEntry>? committed) &&
                committed.FirstOrDefault(candidate =>
                    candidate.Index == entry.Index) is
                    EquipmentSlotRuntimeEntry committedEntry &&
                !string.IsNullOrWhiteSpace(
                    committedEntry.ItemId) &&
                string.Equals(
                    committedEntry.ItemId,
                    entry.ItemId,
                    StringComparison.Ordinal))
            {
                entry.CommittedItemReleasedToNative = true;
            }
            ClearEquipmentSlotStoredItem(entry);
            entry.LastMessage =
                "Moved " +
                display +
                " to native storage as an uncommitted gameplay mutation reason=" +
                (reason ?? string.Empty) +
                ". " +
                placement.Message;
            SaveEquipmentSlotStorage(ownerId);
            if (manager != null)
                InvokeNativeReloadParams(manager);
        }

        private void ReconcileEquipmentSlotWorkingPlacementGuards(
            int? slot)
        {
            if (equipmentSlotWorkingPlacementGuards.Count > 0)
            {
                throw new InvalidOperationException(
                    "EquipmentSlots refused native SaveGame because an outcome-unknown Working placement was not resolved by the one immediate same-call observation; reload without native save to restore the committed projection.");
            }
        }

        private void ReconcileEquipmentSlotUnknownJournals(
            int? slot)
        {
            foreach (KeyValuePair<
                string,
                EquipmentSlotTransactionJournal> pair in
                equipmentSlotJournals.ToArray())
            {
                string ownerId = pair.Key;
                EquipmentSlotTransactionJournal journal =
                    pair.Value;
                if (journal.State !=
                        EquipmentSlotTransactionState.Prepared ||
                    !journal.AttemptStarted ||
                    journal.Placement !=
                        NativeEquipmentPlacementKind.Failure)
                {
                    continue;
                }
                throw new InvalidOperationException(
                    "EquipmentSlots refused native SaveGame because durable placement for " +
                    ownerId +
                    " was not resolved by the one immediate same-call observation; reload without native save before another attempt.");
            }
        }

        private static NativeEquipmentPlacement
            ResolveUnknownNativePlacement(
                EquipmentSlotNativeMutationOutcomeUnknownException
                    pending)
        {
            Type? dolocApi =
                ResolveType("DolocAPI, Assembly-CSharp");
            if (dolocApi == null)
            {
                throw new InvalidOperationException(
                    "DolocAPI is unavailable while native placement evidence remains outcome-unknown.");
            }
            EquipmentSlotNativeMutationResolution resolution =
                ObserveNativePlacementResolution(
                    dolocApi,
                    pending.ItemId,
                    pending.ExpectedCount,
                    pending.BeforeBackpackCount,
                    pending.BeforeMailCount,
                    "immediate outcome-unknown re-observation",
                    out int afterBackpack,
                    out int afterMail);
            if (resolution ==
                EquipmentSlotNativeMutationResolution.Backpack)
            {
                return NativeEquipmentPlacement.Backpack(
                    "Immediate exact re-observation found one native backpack destination.",
                    afterBackpack,
                    afterMail);
            }
            if (resolution ==
                EquipmentSlotNativeMutationResolution.Mail)
            {
                return NativeEquipmentPlacement.Mail(
                    "Immediate exact re-observation found one unaccepted native mail destination.",
                    afterBackpack,
                    afterMail);
            }
            if (resolution ==
                EquipmentSlotNativeMutationResolution.None)
            {
                return NativeEquipmentPlacement.Failed(
                    "Exact reconciliation found an unchanged native preimage.");
            }
            throw EquipmentSlotNativeMutationEvidence
                .Unknown(
                    pending.ItemId,
                    pending.ExpectedCount,
                    pending.BeforeBackpackCount,
                    pending.BeforeMailCount,
                    "Reconciliation still has ambiguous native count evidence.",
                    pending);
        }

        private bool ReconcileEquipmentSlotJournal(
            string ownerId,
            EquipmentSlotSaveScope scope,
            out string message)
        {
            message = string.Empty;
            if (!equipmentSlotJournals.TryGetValue(ownerId, out EquipmentSlotTransactionJournal? journal))
                return true;

            if (journal.Origin !=
                    EquipmentSlotTransactionOrigin.OwnerRecovery &&
                journal.Origin !=
                    EquipmentSlotTransactionOrigin.OrphanRecovery)
            {
                message =
                    journal.Origin ==
                    EquipmentSlotTransactionOrigin.Unknown
                        ? "Legacy journal origin is unknown; escrow retained fail-closed without native replay."
                        : "Gameplay mutation appeared in the durable recovery journal; escrow retained fail-closed without native replay.";
                return false;
            }

            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            if (dolocApi == null || !scope.HasArchive || journal.ArchiveIndex != scope.ArchiveIndex)
            {
                message = "Journal archive identity cannot be proven; escrow retained fail-closed.";
                return false;
            }

            string fingerprint = GetNativeSaveFingerprint(scope.ArchiveIndex);
            int backpack = CountNativeBackpackItem(dolocApi, journal.ItemId);
            int mail = CountPendingUnacceptedItemMail(dolocApi, journal.ItemId);
            EquipmentSlotJournalRecoveryDecision decision =
                EquipmentSlotJournalRecoveryPolicy.Decide(
                    new EquipmentSlotJournalRecoverySnapshot(
                        (EquipmentSlotJournalCheckpoint)journal.State,
                        journal.AttemptStarted,
                        journal.Placement !=
                            NativeEquipmentPlacementKind.Failure,
                        journal.PreSaveFingerprint,
                        journal.PostSaveFingerprint,
                        fingerprint,
                        journal.BeforeBackpackCount,
                        journal.ExpectedBackpackCount,
                        backpack,
                        journal.BeforeMailCount,
                        journal.ExpectedMailCount,
                        mail));

            if (decision == EquipmentSlotJournalRecoveryDecision.FinalizeCommitted)
            {
                if (journal.State ==
                    EquipmentSlotTransactionState.Prepared)
                {
                    journal.PostSaveFingerprint = fingerprint;
                }
                journal.State = EquipmentSlotTransactionState.Committed;
                if (!PersistEquipmentSlotStorage(ownerId, "restart committed journal proof"))
                {
                    message = "Committed journal proof could not be persisted; escrow retained.";
                    return false;
                }
                equipmentSlotJournals.Remove(ownerId);
                if (!PersistEquipmentSlotStorage(ownerId, "restart committed journal cleanup"))
                {
                    equipmentSlotJournals[ownerId] = journal;
                    message = "Committed journal cleanup failed; escrow retained.";
                    return false;
                }
                message = "Committed journal finalized from native save evidence.";
                return true;
            }

            if (decision == EquipmentSlotJournalRecoveryDecision.FailClosed)
            {
                message = "Prepared journal and native storage disagree; escrow retained fail-closed.";
                return false;
            }

            journal.AttemptStarted = true;
            journal.PreSaveFingerprint = fingerprint;
            if (!PersistEquipmentSlotStorage(
                ownerId,
                "restart prepared journal attempt preimage"))
            {
                message =
                    "Prepared journal retry stopped before native mutation because its attempt preimage could not be persisted.";
                return false;
            }
            NativeEquipmentPlacement placement;
            try
            {
                placement =
                    PlaceNativeItemWithImmediateEvidence(
                        journal.ItemId,
                        1);
            }
            catch (
                EquipmentSlotNativeMutationOutcomeUnknownException
                    ex)
            {
                message =
                    "Prepared journal retry outcome is unknown; escrow remains fail-closed without another native retry. " +
                    ex.Message;
                return false;
            }
            if (!placement.Success)
            {
                message = "Prepared journal retry failed: " + placement.Message;
                return false;
            }
            journal.Placement = placement.Kind;
            journal.ExpectedBackpackCount =
                placement.AfterBackpackCount;
            journal.ExpectedMailCount =
                placement.AfterMailCount;
            dirtyEquipmentSlotStorageOwners.Add(ownerId);
            if (!PersistEquipmentSlotStorage(ownerId, "restart prepared journal retry"))
            {
                message = "Prepared journal retry succeeded natively, but its durable update failed.";
                return false;
            }
            message = "Prepared journal retried through " + placement.Kind + "; waiting for native SaveGame.";
            return true;
        }

        private static NativeEquipmentPlacement
            PlaceNativeItemWithImmediateEvidence(
                string itemId,
                int count)
        {
            try
            {
                return PlaceNativeItem(itemId, count);
            }
            catch (
                EquipmentSlotNativeMutationOutcomeUnknownException
                    pending)
            {
                // This is the only permitted reconciliation observation. It
                // runs synchronously in the same caller before control can
                // return to gameplay or SaveGame.
                return ResolveUnknownNativePlacement(pending);
            }
        }

        private static NativeEquipmentPlacement PlaceNativeItem(
            string itemId,
            int count)
        {
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            if (dolocApi == null)
                return NativeEquipmentPlacement.Failed("DolocAPI is unavailable.");

            int normalizedCount = Math.Max(1, count);
            int backpackBefore =
                CountNativeBackpackItemStrict(
                    dolocApi,
                    itemId);
            int mailBefore =
                CountPendingUnacceptedItemMail(
                    dolocApi,
                    itemId);
            MethodInfo? canPlace = dolocApi.GetMethod(
                "CanPlaceItem",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(string), typeof(int) },
                null);
            MethodInfo? place = dolocApi.GetMethod(
                "TryPlaceInBackpack",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(string), typeof(int), typeof(bool) },
                null);
            bool backpackAvailable = canPlace?.Invoke(null, new object[] { itemId, normalizedCount }) is bool can && can;
            if (backpackAvailable)
            {
                if (place == null)
                {
                    return NativeEquipmentPlacement.Failed(
                        "DolocAPI.TryPlaceInBackpack was not found.");
                }
                bool backpackReported;
                try
                {
                    object? placed =
                        place.Invoke(
                            null,
                            new object[]
                            {
                                itemId,
                                normalizedCount,
                                false
                            });
                    backpackReported =
                        placed is bool ok && ok;
                }
                catch (Exception ex)
                {
                    throw EquipmentSlotNativeMutationEvidence
                        .Unknown(
                            itemId,
                            normalizedCount,
                            backpackBefore,
                            mailBefore,
                            "Backpack invocation failed after native mutation may have begun.",
                            ex);
                }
                EquipmentSlotNativeMutationResolution
                    backpackResolution =
                        ObserveNativePlacementResolution(
                            dolocApi,
                            itemId,
                            normalizedCount,
                            backpackBefore,
                            mailBefore,
                            "backpack",
                            out int backpackAfter,
                            out int backpackMailAfter);
                if (backpackResolution ==
                    EquipmentSlotNativeMutationResolution
                        .Backpack)
                {
                    return NativeEquipmentPlacement.Backpack(
                        "Returned " + itemId + " x" + normalizedCount + " to the native backpack.",
                        backpackAfter,
                        backpackMailAfter);
                }
                if (backpackResolution !=
                        EquipmentSlotNativeMutationResolution.None ||
                    backpackReported)
                {
                    throw EquipmentSlotNativeMutationEvidence
                        .Unknown(
                            itemId,
                            normalizedCount,
                            backpackBefore,
                            mailBefore,
                            "Backpack invocation returned " +
                            backpackReported +
                            " with " +
                            backpackResolution +
                            " count evidence.");
                }
            }

            MethodInfo? sendMail = dolocApi.GetMethod(
                "SendItemAsEmail",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(string), typeof(int), typeof(string), typeof(string), typeof(string), typeof(string) },
                null);
            if (sendMail == null)
                return NativeEquipmentPlacement.Failed("DolocAPI.SendItemAsEmail was not found.");
            bool mailReported;
            try
            {
                object? sent = sendMail.Invoke(
                    null,
                    new object?[] { itemId, normalizedCount, null, null, null, "send_item_template" });
                mailReported = sent is bool mailOk && mailOk;
            }
            catch (Exception ex)
            {
                throw EquipmentSlotNativeMutationEvidence
                    .Unknown(
                        itemId,
                        normalizedCount,
                        backpackBefore,
                        mailBefore,
                        "Mail invocation failed after native mutation may have begun.",
                        ex);
            }
            EquipmentSlotNativeMutationResolution mailResolution =
                ObserveNativePlacementResolution(
                    dolocApi,
                    itemId,
                    normalizedCount,
                    backpackBefore,
                    mailBefore,
                    "mail",
                    out int finalBackpack,
                    out int finalMail);
            if (mailResolution ==
                EquipmentSlotNativeMutationResolution.Mail)
            {
                return NativeEquipmentPlacement.Mail(
                    "Returned " + itemId + " x" + normalizedCount + " through native item mail.",
                    finalBackpack,
                    finalMail);
            }
            if (mailResolution ==
                    EquipmentSlotNativeMutationResolution.None &&
                !mailReported)
            {
                return NativeEquipmentPlacement.Failed(
                    "Native backpack and mail both reported failure and exact evidence remained unchanged.");
            }
            throw EquipmentSlotNativeMutationEvidence
                .Unknown(
                    itemId,
                    normalizedCount,
                    backpackBefore,
                    mailBefore,
                    "Mail invocation returned " +
                    mailReported +
                    " with " +
                    mailResolution +
                    " count evidence.");
        }

        private static EquipmentSlotNativeMutationResolution
            ObserveNativePlacementResolution(
                Type dolocApi,
                string itemId,
                int expectedCount,
                int beforeBackpack,
                int beforeMail,
                string phase,
                out int afterBackpack,
                out int afterMail)
        {
            afterBackpack = -1;
            afterMail = -1;
            try
            {
                afterBackpack =
                    CountNativeBackpackItemStrict(
                        dolocApi,
                        itemId);
                afterMail =
                    CountPendingUnacceptedItemMail(
                        dolocApi,
                        itemId);
                return EquipmentSlotNativeMutationEvidence
                    .Resolve(
                        beforeBackpack,
                        beforeMail,
                        afterBackpack,
                        afterMail,
                        expectedCount);
            }
            catch (Exception ex)
            {
                throw EquipmentSlotNativeMutationEvidence
                    .Unknown(
                        itemId,
                        expectedCount,
                        beforeBackpack,
                        beforeMail,
                        "Native evidence became unreadable after " +
                        phase +
                        " invocation.",
                        ex);
            }
        }

        private static int CountNativeBackpackItemStrict(
            Type dolocApi,
            string itemId)
        {
            MethodInfo? method = dolocApi.GetMethod(
                "CountItem",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(string), typeof(bool) },
                null);
            if (method == null)
            {
                throw new InvalidDataException(
                    "DolocAPI.CountItem(string,bool) was not found.");
            }
            object? value = method.Invoke(
                null,
                new object?[] { itemId, false });
            if (value is int count && count >= 0)
                return count;
            throw new InvalidDataException(
                "DolocAPI.CountItem returned an unreadable or negative backpack count for " +
                itemId +
                ".");
        }

        private static int CountPendingUnacceptedItemMail(Type dolocApi, string itemId)
            => EquipmentSlotNativeMailEvidence
                .CountUnacceptedDtmapiItemMail(
                    dolocApi,
                    itemId);

        private static string GetNativeSaveFingerprint(int archiveIndex)
        {
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            object? manager = ReadStaticMember(
                dolocApi,
                "dataPersistenceManager");
            object? handler = manager == null
                ? null
                : ReadMember(manager, "fileDataHandler");
            if (handler == null)
            {
                throw new InvalidDataException(
                    "Native save fingerprint could not resolve LocalSave.");
            }
            MethodInfo? getPath = handler.GetType().GetMethod(
                "GetDataFullPath",
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance,
                null,
                new[] { typeof(int) },
                null);
            string? path = getPath?.Invoke(
                handler,
                new object[] { archiveIndex }) as string;
            object? backupValue = ReadMember(
                handler,
                "backupCount");
            if (string.IsNullOrWhiteSpace(path) ||
                !(backupValue is int backupCount) ||
                backupCount <= 0)
            {
                throw new InvalidDataException(
                    "Native save fingerprint could not resolve the exact current path and LocalSave.backupCount.");
            }
            return EquipmentSlotNativeCommitFingerprint
                .Compute(path, backupCount);
        }

        private long GetNextEquipmentSlotStorageGeneration(
            string ownerId)
        {
            ownerId ??= string.Empty;
            long current =
                equipmentSlotStorageGenerations.TryGetValue(
                    ownerId,
                    out long generation)
                    ? generation
                    : 0;
            long next =
                current == long.MaxValue
                    ? long.MaxValue
                    : current + 1;
            equipmentSlotStorageGenerations[ownerId] =
                next;
            return next;
        }

        private bool ReconcileEquipmentSlotGameplayCandidateDocument(
            string ownerId,
            string path,
            EquipmentSlotSaveScope scope,
            EquipmentSlotStorageDocument document,
            out string message)
        {
            message = string.Empty;
            EquipmentSlotCompatibilityGameplayCandidate? candidate =
                document.GameplayCandidate;
            if (candidate == null)
                return true;
            candidate.Normalize();
            if (document.Journal != null)
            {
                message =
                    "Gameplay candidate and recovery journal coexist; storage retained fail-closed.";
                return false;
            }
            if (!candidate.IsValidFor(
                scope,
                document.Generation))
            {
                message =
                    "Gameplay candidate identity, generation or slot projection is invalid; storage retained fail-closed.";
                return false;
            }

            if (candidate.State ==
                EquipmentSlotGameplayCandidateState
                    .CommittedTombstone)
            {
                string currentFingerprint =
                    GetNativeSaveFingerprint(
                        scope.ArchiveIndex);
                if (string.IsNullOrWhiteSpace(
                        candidate.PostSaveFingerprint) ||
                    !EquipmentSlotNativeCommitFingerprint
                        .Equivalent(
                        candidate.PostSaveFingerprint,
                        currentFingerprint) ||
                    !CandidateNativeExpectationsMatch(
                        candidate))
                {
                    message =
                        "Committed gameplay tombstone no longer matches its exact native-save fingerprint and inventory evidence; storage retained fail-closed.";
                    return false;
                }
                document.GameplayCandidate = null;
                document.Generation =
                    document.Generation == long.MaxValue
                        ? long.MaxValue
                        : document.Generation + 1;
                WriteJson(path, document);
                message =
                    "Finalized committed gameplay tombstone without replaying native placement.";
                return true;
            }

            string preparedCurrentFingerprint =
                GetNativeSaveFingerprint(scope.ArchiveIndex);
            if (!string.IsNullOrWhiteSpace(
                    candidate.PreSaveFingerprint) &&
                EquipmentSlotNativeCommitFingerprint
                    .Equivalent(
                    candidate.PreSaveFingerprint,
                    preparedCurrentFingerprint))
            {
                document.GameplayCandidate = null;
                document.Generation =
                    document.Generation == long.MaxValue
                        ? long.MaxValue
                        : document.Generation + 1;
                WriteJson(path, document);
                message =
                    "Discarded uncommitted gameplay candidate because the native save preimage is unchanged.";
                return true;
            }

            if (string.IsNullOrWhiteSpace(
                    candidate.PreSaveFingerprint) ||
                string.IsNullOrWhiteSpace(
                    preparedCurrentFingerprint) ||
                !CandidateNativeExpectationsMatch(
                    candidate))
            {
                message =
                    "Gameplay candidate lacks exact native-commit fingerprint and inventory evidence; storage retained fail-closed without native replay.";
                return false;
            }

            document.Slots =
                candidate.WorkingSlots
                    .Select(CloneEquipmentSlotStorageEntry)
                    .ToList();
            candidate.State =
                EquipmentSlotGameplayCandidateState
                    .CommittedTombstone;
            candidate.PostSaveFingerprint =
                preparedCurrentFingerprint;
            document.Generation =
                document.Generation == long.MaxValue
                    ? long.MaxValue
                    : document.Generation + 1;
            WriteJson(path, document);

            document.GameplayCandidate = null;
            document.Generation =
                document.Generation == long.MaxValue
                    ? long.MaxValue
                    : document.Generation + 1;
            WriteJson(path, document);
            message =
                "Promoted gameplay candidate from exact native-save fingerprint and inventory evidence, then cleaned its tombstone.";
            return true;
        }

        private static bool CandidateNativeExpectationsMatch(
            EquipmentSlotCompatibilityGameplayCandidate candidate)
        {
            Type? dolocApi =
                ResolveType("DolocAPI, Assembly-CSharp");
            if (dolocApi == null)
                return false;
            foreach (EquipmentSlotNativeExpectation expectation in
                candidate.NativeExpectations)
            {
                if (CountNativeBackpackItem(
                        dolocApi,
                        expectation.ItemId) !=
                        expectation.BackpackCount ||
                    CountPendingUnacceptedItemMail(
                        dolocApi,
                        expectation.ItemId) !=
                        expectation.MailCount)
                {
                    return false;
                }
            }
            return true;
        }

        private static EquipmentSlotStorageEntry
            CloneEquipmentSlotStorageEntry(
                EquipmentSlotStorageEntry entry) =>
            new EquipmentSlotStorageEntry
            {
                Index = entry.Index,
                TailIndexFromEnd = entry.TailIndexFromEnd,
                SlotId = entry.SlotId,
                ItemId = entry.ItemId,
                DisplayName = entry.DisplayName,
                SkillId = entry.SkillId,
                DefenseBonus = entry.DefenseBonus,
                IsShieldHat = entry.IsShieldHat,
                ShieldMaxValue = entry.ShieldMaxValue,
                ShieldValue = entry.ShieldValue,
                ShieldDefend = entry.ShieldDefend,
                LastMessage = entry.LastMessage
            };

        private enum EquipmentSlotTransactionState
        {
            Prepared = 0,
            NativeCommitted = 1,
            Committed = 2
        }

        private enum NativeEquipmentPlacementKind
        {
            Failure = 0,
            Backpack = 1,
            Mail = 2
        }

        private sealed class EquipmentSlotWorkingPlacementGuard
        {
            internal EquipmentSlotWorkingPlacementGuard(
                int slotIndex,
                string itemId,
                string preSaveFingerprint,
                string reason,
                EquipmentSlotNativeMutationOutcomeUnknownException
                    outcome)
            {
                SlotIndex = slotIndex;
                ItemId = itemId ?? string.Empty;
                PreSaveFingerprint =
                    preSaveFingerprint ?? string.Empty;
                Reason = reason ?? string.Empty;
                Outcome = outcome ??
                    throw new ArgumentNullException(
                        nameof(outcome));
            }

            internal int SlotIndex { get; }

            internal string ItemId { get; }

            internal string PreSaveFingerprint { get; }

            internal string Reason { get; }

            internal
                EquipmentSlotNativeMutationOutcomeUnknownException
                Outcome { get; }
        }

        private readonly struct NativeEquipmentPlacement
        {
            private NativeEquipmentPlacement(
                NativeEquipmentPlacementKind kind,
                string message,
                int afterBackpackCount,
                int afterMailCount)
            {
                Kind = kind;
                Message = message ?? string.Empty;
                AfterBackpackCount = afterBackpackCount;
                AfterMailCount = afterMailCount;
            }

            public NativeEquipmentPlacementKind Kind { get; }
            public string Message { get; }
            public bool Success => Kind != NativeEquipmentPlacementKind.Failure;
            public int AfterBackpackCount { get; }
            public int AfterMailCount { get; }

            public static NativeEquipmentPlacement Failed(string message) =>
                new NativeEquipmentPlacement(
                    NativeEquipmentPlacementKind.Failure,
                    message,
                    -1,
                    -1);

            public static NativeEquipmentPlacement Backpack(
                string message,
                int afterBackpackCount,
                int afterMailCount) =>
                new NativeEquipmentPlacement(
                    NativeEquipmentPlacementKind.Backpack,
                    message,
                    afterBackpackCount,
                    afterMailCount);

            public static NativeEquipmentPlacement Mail(
                string message,
                int afterBackpackCount,
                int afterMailCount) =>
                new NativeEquipmentPlacement(
                    NativeEquipmentPlacementKind.Mail,
                    message,
                    afterBackpackCount,
                    afterMailCount);
        }

        [DataContract]
        private sealed class EquipmentSlotTransactionJournal
        {
            [DataMember(Name = "transactionId", Order = 1)]
            public string TransactionId { get; set; } = string.Empty;

            [DataMember(Name = "state", Order = 2)]
            public EquipmentSlotTransactionState State { get; set; }

            [DataMember(Name = "archiveIndex", Order = 3)]
            public int ArchiveIndex { get; set; } = -1;

            [DataMember(Name = "slotIndex", Order = 4)]
            public int SlotIndex { get; set; } = -1;

            [DataMember(Name = "slotId", Order = 5)]
            public string SlotId { get; set; } = string.Empty;

            [DataMember(Name = "itemId", Order = 6)]
            public string ItemId { get; set; } = string.Empty;

            [DataMember(Name = "displayName", Order = 7)]
            public string DisplayName { get; set; } = string.Empty;

            [DataMember(Name = "skillId", Order = 8)]
            public string SkillId { get; set; } = string.Empty;

            [DataMember(Name = "defenseBonus", Order = 9)]
            public int DefenseBonus { get; set; }

            [DataMember(Name = "isShieldHat", Order = 10)]
            public bool IsShieldHat { get; set; }

            [DataMember(Name = "shieldMaxValue", Order = 11)]
            public int ShieldMaxValue { get; set; }

            [DataMember(Name = "shieldValue", Order = 12)]
            public int ShieldValue { get; set; }

            [DataMember(Name = "shieldDefend", Order = 13)]
            public int ShieldDefend { get; set; }

            [DataMember(Name = "attemptStarted", Order = 14)]
            public bool AttemptStarted { get; set; }

            [DataMember(Name = "preSaveFingerprint", Order = 15)]
            public string PreSaveFingerprint { get; set; } = string.Empty;

            [DataMember(Name = "postSaveFingerprint", Order = 16)]
            public string PostSaveFingerprint { get; set; } = string.Empty;

            [DataMember(Name = "beforeBackpackCount", Order = 17)]
            public int BeforeBackpackCount { get; set; }

            [DataMember(Name = "beforeMailCount", Order = 18)]
            public int BeforeMailCount { get; set; }

            [DataMember(Name = "placement", Order = 19)]
            public NativeEquipmentPlacementKind Placement { get; set; }

            [DataMember(Name = "origin", Order = 20)]
            public EquipmentSlotTransactionOrigin Origin { get; set; }

            [DataMember(Name = "expectedBackpackCount", Order = 21)]
            public int ExpectedBackpackCount { get; set; } = -1;

            [DataMember(Name = "expectedMailCount", Order = 22)]
            public int ExpectedMailCount { get; set; } = -1;

            public static EquipmentSlotTransactionJournal FromEntry(
                int archiveIndex,
                EquipmentSlotRuntimeEntry entry,
                int beforeBackpack,
                int beforeMail,
                string preSaveFingerprint,
                EquipmentSlotTransactionOrigin origin) =>
                new EquipmentSlotTransactionJournal
                {
                    TransactionId = Guid.NewGuid().ToString("N"),
                    State = EquipmentSlotTransactionState.Prepared,
                    ArchiveIndex = archiveIndex,
                    SlotIndex = entry.Index,
                    SlotId = entry.SlotId,
                    ItemId = entry.ItemId,
                    DisplayName = entry.DisplayName,
                    SkillId = entry.SkillId,
                    DefenseBonus = entry.DefenseBonus,
                    IsShieldHat = entry.IsShieldHat,
                    ShieldMaxValue = entry.ShieldMaxValue,
                    ShieldValue = entry.ShieldValue,
                    ShieldDefend = entry.ShieldDefend,
                    PreSaveFingerprint = preSaveFingerprint,
                    BeforeBackpackCount = beforeBackpack,
                    BeforeMailCount = beforeMail,
                    ExpectedBackpackCount = -1,
                    ExpectedMailCount = -1,
                    Origin = origin
                };

            public void Normalize()
            {
                TransactionId = TransactionId?.Trim() ?? string.Empty;
                SlotId = SlotId?.Trim() ?? string.Empty;
                ItemId = ItemId?.Trim() ?? string.Empty;
                DisplayName = DisplayName?.Trim() ?? string.Empty;
                SkillId = SkillId?.Trim() ?? string.Empty;
                PreSaveFingerprint = PreSaveFingerprint?.Trim() ?? string.Empty;
                PostSaveFingerprint = PostSaveFingerprint?.Trim() ?? string.Empty;
                BeforeBackpackCount = Math.Max(0, BeforeBackpackCount);
                BeforeMailCount = Math.Max(0, BeforeMailCount);
                if (Placement ==
                    NativeEquipmentPlacementKind.Backpack)
                {
                    ExpectedBackpackCount =
                        checked(BeforeBackpackCount + 1);
                    ExpectedMailCount = BeforeMailCount;
                }
                else if (Placement ==
                    NativeEquipmentPlacementKind.Mail)
                {
                    ExpectedBackpackCount =
                        BeforeBackpackCount;
                    ExpectedMailCount =
                        checked(BeforeMailCount + 1);
                }
                else
                {
                    ExpectedBackpackCount = -1;
                    ExpectedMailCount = -1;
                }
            }

            public void Restore(EquipmentSlotRuntimeEntry entry)
            {
                entry.Index = SlotIndex;
                entry.SlotId = SlotId;
                entry.ItemId = ItemId;
                entry.DisplayName = DisplayName;
                entry.SkillId = SkillId;
                entry.DefenseBonus = DefenseBonus;
                entry.IsShieldHat = IsShieldHat;
                entry.ShieldMaxValue = ShieldMaxValue;
                entry.ShieldValue = ShieldValue;
                entry.ShieldDefend = ShieldDefend;
                entry.Applied = false;
                entry.NativeItem = null;
                entry.NativeFunction = null;
            }
        }

        [DataContract]
        private sealed class EquipmentSlotCompatibilityGameplayCandidate
        {
            [DataMember(Name = "transactionId", Order = 1)]
            public string TransactionId { get; set; } =
                string.Empty;

            [DataMember(Name = "phase", Order = 2)]
            public EquipmentSlotGameplayCandidateState State
            {
                get;
                set;
            }

            [DataMember(Name = "origin", Order = 3)]
            public EquipmentSlotTransactionOrigin Origin
            {
                get;
                set;
            } = EquipmentSlotTransactionOrigin.GameplayMutation;

            [DataMember(Name = "scope", Order = 4)]
            public EquipmentSlotCompatibilityCandidateScope Scope
            {
                get;
                set;
            } = new EquipmentSlotCompatibilityCandidateScope();

            [DataMember(Name = "baseGeneration", Order = 5)]
            public long BaseGeneration { get; set; }

            [DataMember(Name = "preSaveFingerprint", Order = 6)]
            public string PreSaveFingerprint { get; set; } =
                string.Empty;

            [DataMember(Name = "postSaveFingerprint", Order = 7)]
            public string PostSaveFingerprint { get; set; } =
                string.Empty;

            [DataMember(Name = "workingSlots", Order = 8)]
            public List<EquipmentSlotStorageEntry> WorkingSlots
            {
                get;
                set;
            } = new List<EquipmentSlotStorageEntry>();

            [DataMember(Name = "nativeExpectations", Order = 9)]
            public List<EquipmentSlotNativeExpectation>
                NativeExpectations { get; set; } =
                new List<EquipmentSlotNativeExpectation>();

            public void Normalize()
            {
                TransactionId =
                    TransactionId?.Trim() ?? string.Empty;
                Scope ??=
                    new EquipmentSlotCompatibilityCandidateScope();
                Scope.Normalize();
                BaseGeneration = Math.Max(0, BaseGeneration);
                PreSaveFingerprint =
                    PreSaveFingerprint?.Trim() ?? string.Empty;
                PostSaveFingerprint =
                    PostSaveFingerprint?.Trim() ?? string.Empty;
                WorkingSlots ??=
                    new List<EquipmentSlotStorageEntry>();
                foreach (EquipmentSlotStorageEntry slot in
                    WorkingSlots)
                {
                    if (slot == null)
                        continue;
                    slot.SlotId =
                        slot.SlotId?.Trim() ?? string.Empty;
                    slot.ItemId =
                        slot.ItemId?.Trim() ?? string.Empty;
                    slot.DisplayName =
                        slot.DisplayName?.Trim() ?? string.Empty;
                    slot.SkillId =
                        slot.SkillId?.Trim() ?? string.Empty;
                }
                NativeExpectations ??=
                    new List<EquipmentSlotNativeExpectation>();
                foreach (EquipmentSlotNativeExpectation expectation in
                    NativeExpectations)
                {
                    expectation?.Normalize();
                }
            }

            public bool IsValidFor(
                EquipmentSlotSaveScope scope,
                long documentGeneration)
            {
                if (TransactionId.Length != 32 ||
                    !TransactionId.All(Uri.IsHexDigit) ||
                    Origin !=
                        EquipmentSlotTransactionOrigin
                            .GameplayMutation ||
                    !Scope.Matches(scope) ||
                    BaseGeneration > documentGeneration ||
                    WorkingSlots.Any(slot =>
                        slot == null ||
                        slot.Index < 0) ||
                    WorkingSlots.Select(slot => slot.Index)
                        .Distinct()
                        .Count() != WorkingSlots.Count ||
                    NativeExpectations.Any(expectation =>
                        expectation == null ||
                        string.IsNullOrWhiteSpace(
                            expectation.ItemId)) ||
                    NativeExpectations
                        .Select(expectation =>
                            expectation.ItemId)
                        .Distinct(StringComparer.Ordinal)
                        .Count() !=
                        NativeExpectations.Count)
                {
                    return false;
                }
                return State ==
                        EquipmentSlotGameplayCandidateState
                            .Prepared ||
                    State ==
                        EquipmentSlotGameplayCandidateState
                            .CommittedTombstone;
            }
        }

        [DataContract]
        private sealed class EquipmentSlotCompatibilityCandidateScope
        {
            [DataMember(Name = "archiveIndex", Order = 1)]
            public int ArchiveIndex { get; set; } = -1;

            [DataMember(Name = "playerName", Order = 2)]
            public string PlayerName { get; set; } =
                string.Empty;

            [DataMember(Name = "customPlayerName", Order = 3)]
            public string CustomPlayerName { get; set; } =
                string.Empty;

            public static EquipmentSlotCompatibilityCandidateScope
                From(EquipmentSlotSaveScope scope) =>
                new EquipmentSlotCompatibilityCandidateScope
                {
                    ArchiveIndex = scope.ArchiveIndex,
                    PlayerName = scope.PlayerName,
                    CustomPlayerName =
                        scope.CustomPlayerName
                };

            public void Normalize()
            {
                PlayerName =
                    PlayerName?.Trim() ?? string.Empty;
                CustomPlayerName =
                    CustomPlayerName?.Trim() ??
                    string.Empty;
            }

            public bool Matches(
                EquipmentSlotSaveScope scope) =>
                scope != null &&
                ArchiveIndex == scope.ArchiveIndex &&
                string.Equals(
                    PlayerName,
                    scope.PlayerName,
                    StringComparison.Ordinal) &&
                string.Equals(
                    CustomPlayerName,
                    scope.CustomPlayerName,
                    StringComparison.Ordinal);
        }

    }
}
