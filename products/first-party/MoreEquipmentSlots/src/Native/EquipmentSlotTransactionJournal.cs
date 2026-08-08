using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace DTMAPI.MoreEquipmentSlots
{
    internal static class EquipmentSlotNativeCommitFingerprint
    {
        private const int MaximumBackupCount = 32;

        internal static string Compute(
            string? currentPath,
            int backupCount)
        {
            if (string.IsNullOrWhiteSpace(currentPath))
            {
                throw new InvalidDataException(
                    "Native save fingerprint requires an exact current archive path.");
            }
            if (backupCount <= 0 ||
                backupCount > MaximumBackupCount)
            {
                throw new InvalidDataException(
                    "Native save fingerprint requires backupCount in the supported range 1.." +
                    MaximumBackupCount +
                    ".");
            }

            string path = Path.GetFullPath(currentPath);
            string current = Capture(path, required: true);
            var previous = new string[backupCount];
            for (int index = 0; index < backupCount; index++)
            {
                previous[index] = Capture(
                    path + ".prev" + index,
                    required: false);
            }
            string backup = Capture(path + ".bak", required: false);

            var legacy = new StringBuilder("native-v2");
            Append(legacy, "current", current);
            Append(legacy, "prev", previous[0]);
            Append(legacy, "bak", backup);

            var result = new StringBuilder("native-v3");
            result.Append("|backupCount=").Append(backupCount);
            Append(result, "current", current);
            for (int index = 0; index < previous.Length; index++)
            {
                Append(
                    result,
                    "prev" + index,
                    previous[index]);
            }
            Append(result, "bak", backup);
            result
                .Append("|legacyV2=")
                .Append(HashText(legacy.ToString()));
            return result.ToString();
        }

        internal static bool Equivalent(
            string? recorded,
            string? current)
        {
            string oldValue = recorded?.Trim() ?? string.Empty;
            string currentValue = current?.Trim() ?? string.Empty;
            if (oldValue.Length == 0 || currentValue.Length == 0)
                return false;
            if (string.Equals(
                oldValue,
                currentValue,
                StringComparison.Ordinal))
            {
                return true;
            }
            if (!oldValue.StartsWith(
                    "native-v2|",
                    StringComparison.Ordinal) ||
                !currentValue.StartsWith(
                    "native-v3|",
                    StringComparison.Ordinal))
            {
                return false;
            }

            const string marker = "|legacyV2=";
            int markerIndex = currentValue.LastIndexOf(
                marker,
                StringComparison.Ordinal);
            return markerIndex >= 0 &&
                string.Equals(
                    currentValue.Substring(
                        markerIndex + marker.Length),
                    HashText(oldValue),
                    StringComparison.Ordinal);
        }

        private static void Append(
            StringBuilder result,
            string label,
            string value)
        {
            result
                .Append('|')
                .Append(label)
                .Append('=')
                .Append(value);
        }

        private static string Capture(
            string path,
            bool required)
        {
            if (!File.Exists(path))
            {
                if (!required)
                    return "missing";
                throw new FileNotFoundException(
                    "The native current save archive is missing.",
                    path);
            }

            var info = new FileInfo(path);
            long length = info.Length;
            long writeTicks =
                info.LastWriteTimeUtc.Ticks;
            using (FileStream stream = File.Open(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite |
                FileShare.Delete))
            using (SHA256 hash = SHA256.Create())
            {
                byte[] digest = hash.ComputeHash(stream);
                info.Refresh();
                if (!info.Exists ||
                    info.Length != length ||
                    info.LastWriteTimeUtc.Ticks != writeTicks)
                {
                    throw new IOException(
                        "Native save archive changed while its commit fingerprint was being captured.");
                }
                return new StringBuilder()
                    .Append("exists:")
                    .Append(length)
                    .Append(':')
                    .Append(writeTicks)
                    .Append(':')
                    .Append(ToHex(digest))
                    .ToString();
            }
        }

        private static string HashText(string value)
        {
            using (SHA256 hash = SHA256.Create())
            {
                return ToHex(
                    hash.ComputeHash(
                        Encoding.UTF8.GetBytes(value)));
            }
        }

        private static string ToHex(byte[] bytes)
        {
            var text =
                new StringBuilder(bytes.Length * 2);
            foreach (byte value in bytes)
                text.Append(value.ToString("x2"));
            return text.ToString();
        }
    }

    internal static class EquipmentSlotTransactionCoordinator
    {
        internal static EquipmentSlotTransactionJournal PrepareRecovery(
            EquipmentSlotStorageDocument document,
            IReadOnlyList<int> slotIndexes) =>
            PrepareRecovery(
                document,
                slotIndexes,
                EquipmentSlotTransactionOrigin.OwnerRecovery);

        internal static EquipmentSlotTransactionJournal PrepareRecovery(
            EquipmentSlotStorageDocument document,
            IReadOnlyList<int> slotIndexes,
            EquipmentSlotTransactionOrigin origin)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            if (slotIndexes == null)
                throw new ArgumentNullException(nameof(slotIndexes));
            document.Normalize();
            ThrowIfJournalActive(document);

            var journal = CreateJournal(document, origin);
            var seen = new HashSet<int>();
            foreach (int slotIndex in slotIndexes)
            {
                if (!seen.Add(slotIndex))
                    continue;
                EquipmentSlotStorageEntry slot =
                    RequireSlot(document, slotIndex);
                if (!slot.IsOccupied)
                    continue;
                journal.Escrow.Add(ToEscrow(slot));
                slot.Clear();
            }

            if (journal.Escrow.Count == 0)
            {
                throw new InvalidOperationException(
                    "No occupied product slots were selected for protected recovery.");
            }

            document.Journal = journal;
            return journal;
        }

        internal static EquipmentSlotTransactionJournal PrepareReplacement(
            EquipmentSlotStorageDocument document,
            int slotIndex,
            EquipmentSlotStorageEntry replacement) =>
            PrepareReplacement(
                document,
                slotIndex,
                replacement,
                EquipmentSlotTransactionOrigin.OwnerRecovery);

        internal static EquipmentSlotTransactionJournal PrepareReplacement(
            EquipmentSlotStorageDocument document,
            int slotIndex,
            EquipmentSlotStorageEntry replacement,
            EquipmentSlotTransactionOrigin origin)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            if (replacement == null)
                throw new ArgumentNullException(nameof(replacement));
            document.Normalize();
            ThrowIfJournalActive(document);
            if (!replacement.IsOccupied)
            {
                throw new InvalidOperationException(
                    "A replacement transaction requires a new item.");
            }

            EquipmentSlotStorageEntry slot =
                RequireSlot(document, slotIndex);
            var journal = CreateJournal(document, origin);
            if (slot.IsOccupied)
                journal.Escrow.Add(ToEscrow(slot));
            slot.Clear();
            EquipmentSlotStorageEntry normalized =
                CloneSlot(replacement);
            normalized.Index = slotIndex;
            journal.Replacements.Add(normalized);
            document.Journal = journal;
            return journal;
        }

        internal static void StartAttempt(
            EquipmentSlotStorageDocument document,
            string saveFingerprint,
            Func<string, NativeRecoveryObservation> observeItem)
        {
            if (observeItem == null)
                throw new ArgumentNullException(nameof(observeItem));
            EquipmentSlotTransactionJournal journal =
                RequireJournal(document);
            if (!IsPersistentRecoveryOrigin(journal.Origin))
            {
                throw new InvalidOperationException(
                    "Durable placement attempts are reserved for explicit OwnerRecovery or OrphanRecovery.");
            }
            if (journal.State !=
                EquipmentSlotJournalState.Prepared)
            {
                throw new InvalidOperationException(
                    "Only a prepared journal can start native placement.");
            }
            if (!journal.Scope.Matches(document.Scope) ||
                journal.BaseGeneration > document.Generation)
            {
                throw new InvalidOperationException(
                    "Prepared journal scope or generation does not match its sidecar.");
            }

            journal.AttemptStarted = true;
            journal.PreSaveFingerprint =
                saveFingerprint?.Trim() ?? string.Empty;
            journal.PostSaveFingerprint = string.Empty;
            if (journal.Replacements.Count > 0)
            {
                NativeRecoveryObservation incoming =
                    observeItem(
                        journal.Replacements[0].ItemId);
                journal.IncomingBeforeBackpackCount =
                    incoming.BackpackCount;
                journal.IncomingExpectedBackpackCount =
                    incoming.BackpackCount;
                journal.IncomingAttemptCompleted = false;
                journal.IncomingSucceeded = false;
            }
            foreach (EquipmentSlotEscrowEntry item in journal.Escrow)
            {
                NativeRecoveryObservation before =
                    observeItem(item.ItemId);
                item.BeforeBackpackCount = before.BackpackCount;
                item.BeforeMailCount = before.MailCount;
                item.ExpectedBackpackCount = before.BackpackCount;
                item.ExpectedMailCount = before.MailCount;
                item.Placement = NativePlacementKind.Failure;
                item.AttemptCompleted = false;
            }
        }

        internal static void RecordIncomingWithdrawal(
            EquipmentSlotTransactionJournal journal,
            bool succeeded,
            int afterBackpackCount,
            bool fromNativeBuffer = false)
        {
            if (journal == null)
                throw new ArgumentNullException(nameof(journal));
            if (journal.Replacements.Count == 0)
            {
                throw new InvalidOperationException(
                    "No replacement item is staged.");
            }

            journal.IncomingAttemptCompleted = true;
            journal.IncomingSucceeded = succeeded;
            journal.IncomingFromNativeBuffer =
                fromNativeBuffer;
            journal.IncomingExpectedBackpackCount =
                Math.Max(0, afterBackpackCount);
        }

        internal static void RecordPlacement(
            EquipmentSlotTransactionJournal journal,
            int escrowIndex,
            NativePlacementResult placement,
            NativeRecoveryObservation after)
        {
            if (journal == null)
                throw new ArgumentNullException(nameof(journal));
            if (escrowIndex < 0 ||
                escrowIndex >= journal.Escrow.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(escrowIndex));
            }
            EquipmentSlotEscrowEntry item =
                journal.Escrow[escrowIndex];
            item.Placement = placement.Kind;
            item.AttemptCompleted = true;
            item.ExpectedBackpackCount = after.BackpackCount;
            item.ExpectedMailCount = after.MailCount;
        }

        internal static bool CanWithdrawIncoming(
            EquipmentSlotTransactionJournal journal)
        {
            if (journal == null)
                throw new ArgumentNullException(nameof(journal));
            if (journal.Replacements.Count == 0)
            {
                throw new InvalidOperationException(
                    "No replacement item is staged.");
            }

            foreach (EquipmentSlotEscrowEntry item in journal.Escrow)
            {
                if (!item.AttemptCompleted ||
                    item.Placement ==
                        NativePlacementKind.Failure)
                {
                    return false;
                }
            }

            return true;
        }

        internal static void PromoteCommittedTombstone(
            EquipmentSlotStorageDocument document,
            string postSaveFingerprint)
        {
            EquipmentSlotTransactionJournal journal =
                RequireJournal(document);
            if (!journal.AttemptStarted ||
                !AllEscrowAttemptsCompleted(journal) ||
                (journal.Replacements.Count > 0 &&
                 !journal.IncomingAttemptCompleted))
            {
                throw new InvalidOperationException(
                    "Every escrowed item must finish its native placement attempt before promotion.");
            }
            if (!journal.Scope.Matches(document.Scope))
            {
                throw new InvalidOperationException(
                    "Journal scope changed before native commit promotion.");
            }

            ApplyReplacements(document, journal);
            journal.State =
                EquipmentSlotJournalState.CommittedTombstone;
            journal.PostSaveFingerprint =
                postSaveFingerprint?.Trim() ?? string.Empty;
        }

        internal static void ResetFailedAttempt(
            EquipmentSlotStorageDocument document)
        {
            EquipmentSlotTransactionJournal journal =
                RequireJournal(document);
            if (journal.State !=
                EquipmentSlotJournalState.Prepared)
            {
                throw new InvalidOperationException(
                    "A committed tombstone cannot be reset for placement.");
            }

            journal.AttemptStarted = false;
            journal.PreSaveFingerprint = string.Empty;
            journal.PostSaveFingerprint = string.Empty;
            journal.IncomingAttemptCompleted = false;
            journal.IncomingSucceeded = false;
            journal.IncomingBeforeBackpackCount = 0;
            journal.IncomingExpectedBackpackCount = 0;
            journal.IncomingFromNativeBuffer = false;
            foreach (EquipmentSlotEscrowEntry item in journal.Escrow)
            {
                item.BeforeBackpackCount = 0;
                item.BeforeMailCount = 0;
                item.ExpectedBackpackCount = 0;
                item.ExpectedMailCount = 0;
                item.Placement = NativePlacementKind.Failure;
                item.AttemptCompleted = false;
            }
        }

        internal static void DiscardUncommittedReplacementForColdRecovery(
            EquipmentSlotStorageDocument document)
        {
            EquipmentSlotTransactionJournal journal =
                RequireJournal(document);
            if (journal.State !=
                    EquipmentSlotJournalState.Prepared ||
                journal.Replacements.Count != 1)
            {
                throw new InvalidOperationException(
                    "Only one uncommitted prepared replacement can be discarded by cold recovery.");
            }

            journal.Replacements.Clear();
            journal.IncomingAttemptCompleted = false;
            journal.IncomingSucceeded = false;
            journal.IncomingBeforeBackpackCount = 0;
            journal.IncomingExpectedBackpackCount = 0;
            journal.IncomingFromNativeBuffer = false;
            if (journal.Escrow.Count == 0)
            {
                document.Journal = null;
                return;
            }

            ResetFailedAttempt(document);
        }

        internal static EquipmentSlotRecoveryDecision DecideRecovery(
            EquipmentSlotStorageDocument document,
            string currentSaveFingerprint,
            Func<string, NativeRecoveryObservation> observeItem)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            if (observeItem == null)
                throw new ArgumentNullException(nameof(observeItem));
            document.Normalize();
            EquipmentSlotTransactionJournal journal =
                RequireJournal(document);
            if (!IsPersistentRecoveryOrigin(journal.Origin))
            {
                return FailClosed(
                    "Durable journal origin is unknown or is not a persistent owner/orphan recovery transaction.");
            }
            if (!journal.Scope.Matches(document.Scope))
            {
                return FailClosed(
                    "Journal scope does not match the loaded archive identity.");
            }
            if (journal.BaseGeneration > document.Generation)
            {
                return FailClosed(
                    "Journal base generation is newer than the containing sidecar.");
            }
            if (journal.Escrow.Count == 0 &&
                journal.Replacements.Count == 0)
            {
                return FailClosed(
                    "Journal contains no escrow or replacement mutation.");
            }

            if (journal.State ==
                EquipmentSlotJournalState.CommittedTombstone)
            {
                if (!EquipmentSlotNativeCommitFingerprint
                    .Equivalent(
                    journal.PostSaveFingerprint,
                    currentSaveFingerprint))
                {
                    return FailClosed(
                        "Committed tombstone does not match the current native save fingerprint.");
                }

                return HasExactCommittedEvidence(
                    journal,
                    observeItem,
                    out string committedReason)
                    ? Finalize(committedReason)
                    : FailClosed(committedReason);
            }

            if (!journal.AttemptStarted)
            {
                return Retry(
                    "Prepared escrow has not started native placement.");
            }
            if (string.IsNullOrWhiteSpace(
                journal.PreSaveFingerprint))
            {
                return FailClosed(
                    "Started journal has no exact native save preimage fingerprint.");
            }

            bool fingerprintChanged =
                !EquipmentSlotNativeCommitFingerprint
                    .Equivalent(
                        journal.PreSaveFingerprint,
                        currentSaveFingerprint);
            if (!fingerprintChanged)
            {
                return HasNoNativeDestinationDelta(
                    journal,
                    observeItem,
                    out string unchangedReason)
                    ? Retry(
                        "Native save still matches the prepared preimage.")
                    : FailClosed(unchangedReason);
            }

            return HasExactPreparedEvidence(
                journal,
                observeItem,
                out string preparedReason)
                ? Finalize(preparedReason)
                : FailClosed(preparedReason);
        }

        internal static void FinalizeCommitted(
            EquipmentSlotStorageDocument document)
        {
            EquipmentSlotTransactionJournal journal =
                RequireJournal(document);
            RestoreFailedEscrow(document, journal);
            ApplyReplacements(document, journal);
            document.Journal = null;
        }

        private static bool HasNoNativeDestinationDelta(
            EquipmentSlotTransactionJournal journal,
            Func<string, NativeRecoveryObservation> observeItem,
            out string reason)
        {
            foreach (ItemGroup group in GroupEscrow(journal))
            {
                if (!group.BaselineConsistent ||
                    !group.StoredEvidenceConsistent)
                {
                    reason =
                        "Prepared journal contains inconsistent native observation evidence.";
                    return false;
                }
                NativeRecoveryObservation current =
                    observeItem(group.ItemId);
                int backpackDelta =
                    current.BackpackCount -
                    group.BeforeBackpackCount;
                int mailDelta =
                    current.MailCount -
                    group.BeforeMailCount;
                if (backpackDelta != 0 || mailDelta != 0)
                {
                    reason =
                        "Prepared preimage is unchanged but native destination counts moved.";
                    return false;
                }
            }

            if (journal.Replacements.Count > 0)
            {
                NativeRecoveryObservation current =
                    observeItem(journal.Replacements[0].ItemId);
                if (current.BackpackCount !=
                    journal.IncomingBeforeBackpackCount)
                {
                    reason =
                        "Prepared preimage is unchanged but replacement backpack count moved.";
                    return false;
                }
            }

            reason = "No native destination delta is observable.";
            return true;
        }

        private static bool HasExactPreparedEvidence(
            EquipmentSlotTransactionJournal journal,
            Func<string, NativeRecoveryObservation> observeItem,
            out string reason)
        {
            foreach (ItemGroup group in GroupEscrow(journal))
            {
                NativeRecoveryObservation current =
                    observeItem(group.ItemId);
                int backpackDelta =
                    current.BackpackCount -
                    group.BeforeBackpackCount;
                int mailDelta =
                    current.MailCount -
                    group.BeforeMailCount;
                int expectedBackpackDelta =
                    group.ExpectedBackpackDelta +
                    GetIncomingBackpackDelta(
                        journal,
                        group.ItemId);
                if (!HasConsistentIncomingEvidence(
                        journal,
                        group,
                        expectedBackpackDelta))
                {
                    reason =
                        "Prepared replacement journal contains inconsistent same-item withdrawal evidence.";
                    return false;
                }
                if (group.HasUnrecordedPlacement ||
                    backpackDelta !=
                        expectedBackpackDelta ||
                    mailDelta != group.ExpectedMailDelta)
                {
                    reason =
                        "Prepared native destination is ambiguous: item=" +
                        group.ItemId +
                        ", expectedBackpackDelta=" +
                        expectedBackpackDelta +
                        ", expectedMailDelta=" +
                        group.ExpectedMailDelta +
                        ", backpackDelta=" +
                        backpackDelta +
                        ", mailDelta=" +
                        mailDelta +
                        ".";
                    return false;
                }
            }

            if (journal.Replacements.Count > 0)
            {
                if (!journal.IncomingAttemptCompleted)
                {
                    reason =
                        "Replacement withdrawal outcome was not durably recorded.";
                    return false;
                }
                string incomingItemId =
                    journal.Replacements[0].ItemId;
                if (!ContainsEscrowItem(
                    journal,
                    incomingItemId))
                {
                    NativeRecoveryObservation current =
                        observeItem(incomingItemId);
                    int incomingDelta =
                        current.BackpackCount -
                        journal.IncomingBeforeBackpackCount;
                    int expectedIncomingDelta =
                        GetIncomingBackpackDelta(
                            journal,
                            incomingItemId);
                    if (incomingDelta !=
                        expectedIncomingDelta)
                    {
                        reason =
                            "Replacement withdrawal is ambiguous: expectedBackpackDelta=" +
                            expectedIncomingDelta +
                            ", actual=" +
                            incomingDelta +
                            ".";
                        return false;
                    }
                }
            }

            reason =
                "Native save changed and every escrow item has one exact destination.";
            return true;
        }

        private static bool HasExactCommittedEvidence(
            EquipmentSlotTransactionJournal journal,
            Func<string, NativeRecoveryObservation> observeItem,
            out string reason)
        {
            foreach (ItemGroup group in GroupEscrow(journal))
            {
                if (!group.BaselineConsistent ||
                    !group.StoredEvidenceConsistent)
                {
                    reason =
                        "Committed journal contains inconsistent native observation evidence.";
                    return false;
                }
                NativeRecoveryObservation current =
                    observeItem(group.ItemId);
                int expectedBackpack =
                    group.BeforeBackpackCount +
                    group.ExpectedBackpackDelta +
                    GetIncomingBackpackDelta(
                        journal,
                        group.ItemId);
                if (!HasConsistentIncomingEvidence(
                        journal,
                        group,
                        expectedBackpack -
                            group.BeforeBackpackCount))
                {
                    reason =
                        "Committed replacement journal contains inconsistent same-item withdrawal evidence.";
                    return false;
                }
                int expectedMail =
                    group.BeforeMailCount +
                    group.ExpectedMailDelta;
                if (current.BackpackCount != expectedBackpack ||
                    current.MailCount != expectedMail)
                {
                    reason =
                        "Committed native destination count drifted: item=" +
                        group.ItemId +
                        ", backpack=" +
                        current.BackpackCount +
                        "/" +
                        expectedBackpack +
                        ", mail=" +
                        current.MailCount +
                        "/" +
                        expectedMail +
                        ".";
                    return false;
                }
            }

            if (journal.Replacements.Count > 0)
            {
                string incomingItemId =
                    journal.Replacements[0].ItemId;
                if (!ContainsEscrowItem(
                    journal,
                    incomingItemId))
                {
                    int expectedIncomingCount =
                        journal.IncomingBeforeBackpackCount +
                        GetIncomingBackpackDelta(
                            journal,
                            incomingItemId);
                    if (journal
                            .IncomingExpectedBackpackCount !=
                        expectedIncomingCount)
                    {
                        reason =
                            "Committed replacement journal contains inconsistent withdrawal evidence.";
                        return false;
                    }
                    NativeRecoveryObservation current =
                        observeItem(incomingItemId);
                    if (current.BackpackCount !=
                        expectedIncomingCount)
                    {
                        reason =
                            "Committed replacement withdrawal count drifted.";
                        return false;
                    }
                }
            }

            reason =
                "Committed tombstone and exact native destinations are observable.";
            return true;
        }

        private static List<ItemGroup> GroupEscrow(
            EquipmentSlotTransactionJournal journal)
        {
            var result = new List<ItemGroup>();
            foreach (EquipmentSlotEscrowEntry item in journal.Escrow)
            {
                ItemGroup? existing = null;
                foreach (ItemGroup group in result)
                {
                    if (group.ItemId.Equals(
                        item.ItemId,
                        StringComparison.Ordinal))
                    {
                        existing = group;
                        break;
                    }
                }

                if (existing == null)
                {
                    existing = new ItemGroup(item);
                    result.Add(existing);
                }
                else
                {
                    existing.Add(item);
                }
            }

            return result;
        }

        private static void ApplyReplacements(
            EquipmentSlotStorageDocument document,
            EquipmentSlotTransactionJournal journal)
        {
            foreach (EquipmentSlotStorageEntry replacement in
                journal.Replacements)
            {
                if (!journal.IncomingSucceeded)
                    continue;
                bool failedOutgoing = false;
                foreach (EquipmentSlotEscrowEntry escrow in
                    journal.Escrow)
                {
                    if (escrow.SlotIndex == replacement.Index &&
                        escrow.Placement ==
                            NativePlacementKind.Failure)
                    {
                        failedOutgoing = true;
                        break;
                    }
                }
                if (failedOutgoing)
                    continue;

                EquipmentSlotStorageEntry target =
                    RequireSlot(document, replacement.Index);
                document.Slots[replacement.Index] =
                    CloneSlot(replacement);
                target.Clear();
            }
        }

        private static bool AllEscrowAttemptsCompleted(
            EquipmentSlotTransactionJournal journal)
        {
            foreach (EquipmentSlotEscrowEntry item in journal.Escrow)
            {
                if (!item.AttemptCompleted)
                {
                    return false;
                }
            }

            return true;
        }

        private static void RestoreFailedEscrow(
            EquipmentSlotStorageDocument document,
            EquipmentSlotTransactionJournal journal)
        {
            foreach (EquipmentSlotEscrowEntry item in journal.Escrow)
            {
                if (item.Placement !=
                    NativePlacementKind.Failure)
                {
                    continue;
                }

                EquipmentSlotStorageEntry slot =
                    RequireSlot(document, item.SlotIndex);
                if (slot.IsOccupied)
                {
                    throw new InvalidOperationException(
                        "Failed recovery escrow cannot be restored because its product slot is occupied.");
                }
                slot.ItemId = item.ItemId;
                slot.DisplayName = item.DisplayName;
                slot.SkillId = item.SkillId;
                slot.DefenseBonus = item.DefenseBonus;
                slot.IsShield = item.IsShield;
                slot.ShieldValue = item.ShieldValue;
                slot.ShieldMaxValue =
                    item.ShieldMaxValue;
                slot.ShieldDefend = item.ShieldDefend;
            }
        }

        private static EquipmentSlotTransactionJournal CreateJournal(
            EquipmentSlotStorageDocument document,
            EquipmentSlotTransactionOrigin origin)
        {
            if (!IsPersistentRecoveryOrigin(origin))
            {
                throw new InvalidOperationException(
                    "A durable placement journal is reserved for explicit OwnerRecovery or OrphanRecovery.");
            }
            return new EquipmentSlotTransactionJournal
            {
                TransactionId = Guid.NewGuid().ToString("N"),
                State = EquipmentSlotJournalState.Prepared,
                Origin = origin,
                Scope = document.Scope.Clone(),
                BaseGeneration = document.Generation
            };
        }

        private static bool IsPersistentRecoveryOrigin(
            EquipmentSlotTransactionOrigin origin) =>
            origin == EquipmentSlotTransactionOrigin.OwnerRecovery ||
            origin == EquipmentSlotTransactionOrigin.OrphanRecovery;

        private static EquipmentSlotEscrowEntry ToEscrow(
            EquipmentSlotStorageEntry slot) =>
            new EquipmentSlotEscrowEntry
            {
                SlotIndex = slot.Index,
                ItemId = slot.ItemId,
                DisplayName = slot.DisplayName,
                SkillId = slot.SkillId,
                DefenseBonus = slot.DefenseBonus,
                IsShield = slot.IsShield,
                ShieldValue = slot.ShieldValue,
                ShieldMaxValue = slot.ShieldMaxValue,
                ShieldDefend = slot.ShieldDefend
            };

        private static int GetIncomingBackpackDelta(
            EquipmentSlotTransactionJournal journal,
            string itemId) =>
            journal.Replacements.Count > 0 &&
            string.Equals(
                journal.Replacements[0].ItemId,
                itemId,
                StringComparison.Ordinal) &&
            journal.IncomingSucceeded &&
            !journal.IncomingFromNativeBuffer
                ? -1
                : 0;

        private static bool ContainsEscrowItem(
            EquipmentSlotTransactionJournal journal,
            string itemId)
        {
            foreach (EquipmentSlotEscrowEntry escrow in
                journal.Escrow)
            {
                if (string.Equals(
                    escrow.ItemId,
                    itemId,
                    StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool HasConsistentIncomingEvidence(
            EquipmentSlotTransactionJournal journal,
            ItemGroup group,
            int expectedBackpackDelta)
        {
            if (journal.Replacements.Count == 0 ||
                !string.Equals(
                    journal.Replacements[0].ItemId,
                    group.ItemId,
                    StringComparison.Ordinal))
            {
                return true;
            }

            return journal.IncomingBeforeBackpackCount ==
                    group.BeforeBackpackCount &&
                (!journal.IncomingAttemptCompleted ||
                 journal.IncomingExpectedBackpackCount ==
                    group.BeforeBackpackCount +
                    expectedBackpackDelta);
        }

        private static EquipmentSlotStorageEntry CloneSlot(
            EquipmentSlotStorageEntry slot) =>
            new EquipmentSlotStorageEntry
            {
                Index = slot.Index,
                ItemId = slot.ItemId,
                DisplayName = slot.DisplayName,
                SkillId = slot.SkillId,
                DefenseBonus = slot.DefenseBonus,
                IsShield = slot.IsShield,
                ShieldValue = slot.ShieldValue,
                ShieldMaxValue = slot.ShieldMaxValue,
                ShieldDefend = slot.ShieldDefend
            };

        private static EquipmentSlotStorageEntry RequireSlot(
            EquipmentSlotStorageDocument document,
            int slotIndex)
        {
            if (slotIndex < 0 ||
                slotIndex >=
                MoreEquipmentSlotsProductContract.FixedSlotCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(slotIndex));
            }
            return document.Slots[slotIndex];
        }

        private static EquipmentSlotTransactionJournal RequireJournal(
            EquipmentSlotStorageDocument document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            return document.Journal ??
                throw new InvalidOperationException(
                    "No protected equipment-slot transaction is active.");
        }

        private static void ThrowIfJournalActive(
            EquipmentSlotStorageDocument document)
        {
            if (document.Journal != null ||
                document.GameplayCandidate != null)
            {
                throw new InvalidOperationException(
                    "A protected equipment-slot transaction is already active.");
            }
        }

        private static EquipmentSlotRecoveryDecision Retry(
            string reason) =>
            new EquipmentSlotRecoveryDecision(
                EquipmentSlotRecoveryAction.RetryPlacement,
                reason);

        private static EquipmentSlotRecoveryDecision Finalize(
            string reason) =>
            new EquipmentSlotRecoveryDecision(
                EquipmentSlotRecoveryAction.FinalizeCommitted,
                reason);

        private static EquipmentSlotRecoveryDecision FailClosed(
            string reason) =>
            new EquipmentSlotRecoveryDecision(
                EquipmentSlotRecoveryAction.FailClosed,
                reason);

        private sealed class ItemGroup
        {
            internal ItemGroup(EquipmentSlotEscrowEntry first)
            {
                ItemId = first.ItemId;
                BeforeBackpackCount =
                    first.BeforeBackpackCount;
                BeforeMailCount = first.BeforeMailCount;
                Count = 1;
                ExpectedBackpackDelta =
                    first.Placement ==
                    NativePlacementKind.Backpack
                        ? 1
                        : 0;
                ExpectedMailDelta =
                    first.Placement ==
                    NativePlacementKind.Mail
                        ? 1
                        : 0;
                HasUnrecordedPlacement =
                    !first.AttemptCompleted;
                StoredEvidenceConsistent =
                    HasValidStoredObservation(
                        first,
                        ExpectedBackpackDelta,
                        ExpectedMailDelta);
            }

            internal string ItemId { get; }

            internal int Count { get; private set; }

            internal int BeforeBackpackCount { get; }

            internal int BeforeMailCount { get; }

            internal int ExpectedBackpackDelta { get; private set; }

            internal int ExpectedMailDelta { get; private set; }

            internal bool HasUnrecordedPlacement { get; private set; }

            internal bool BaselineConsistent { get; private set; } =
                true;

            internal bool StoredEvidenceConsistent { get; private set; }

            internal void Add(EquipmentSlotEscrowEntry item)
            {
                Count++;
                if (item.BeforeBackpackCount !=
                        BeforeBackpackCount ||
                    item.BeforeMailCount != BeforeMailCount)
                {
                    BaselineConsistent = false;
                }
                if (item.Placement ==
                    NativePlacementKind.Backpack)
                {
                    ExpectedBackpackDelta++;
                }
                else if (item.Placement ==
                    NativePlacementKind.Mail)
                {
                    ExpectedMailDelta++;
                }
                else if (!item.AttemptCompleted)
                {
                    HasUnrecordedPlacement = true;
                }
                StoredEvidenceConsistent &=
                    HasValidStoredObservation(
                        item,
                        ExpectedBackpackDelta,
                        ExpectedMailDelta);
            }

            private bool HasValidStoredObservation(
                EquipmentSlotEscrowEntry item,
                int backpackDelta,
                int mailDelta)
            {
                if (!item.AttemptCompleted)
                {
                    return item.Placement ==
                            NativePlacementKind.Failure &&
                        item.ExpectedBackpackCount ==
                            item.BeforeBackpackCount &&
                        item.ExpectedMailCount ==
                            item.BeforeMailCount;
                }

                return item.ExpectedBackpackCount ==
                        BeforeBackpackCount +
                        backpackDelta &&
                    item.ExpectedMailCount ==
                        BeforeMailCount +
                        mailDelta;
            }
        }
    }

    internal static class EquipmentSlotGameplayCandidateCoordinator
    {
        internal static EquipmentSlotGameplayCandidate Prepare(
            EquipmentSlotStorageDocument document,
            IReadOnlyList<EquipmentSlotStorageEntry> workingSlots,
            string preSaveFingerprint,
            Func<string, NativeRecoveryObservation> observeItem)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            if (workingSlots == null)
                throw new ArgumentNullException(nameof(workingSlots));
            if (observeItem == null)
                throw new ArgumentNullException(nameof(observeItem));
            document.Normalize();
            if (document.Journal != null ||
                document.GameplayCandidate != null)
            {
                throw new InvalidOperationException(
                    "A protected equipment transaction is already active.");
            }
            if (workingSlots.Count !=
                MoreEquipmentSlotsProductContract.FixedSlotCount)
            {
                throw new InvalidOperationException(
                    "A gameplay candidate must project exactly three product slots.");
            }

            var candidate = new EquipmentSlotGameplayCandidate
            {
                TransactionId = Guid.NewGuid().ToString("N"),
                State =
                    EquipmentSlotGameplayCandidateState.Prepared,
                Origin =
                    EquipmentSlotTransactionOrigin.GameplayMutation,
                Scope = document.Scope.Clone(),
                BaseGeneration = document.Generation,
                PreSaveFingerprint =
                    preSaveFingerprint?.Trim() ?? string.Empty,
                WorkingSlots = CloneSlots(workingSlots)
            };
            if (string.IsNullOrWhiteSpace(
                    candidate.PreSaveFingerprint))
            {
                throw new InvalidOperationException(
                    "A gameplay candidate requires the exact native save preimage.");
            }

            var itemIds = new HashSet<string>(
                StringComparer.Ordinal);
            AddOccupiedItemIds(document.Slots, itemIds);
            AddOccupiedItemIds(candidate.WorkingSlots, itemIds);
            foreach (string itemId in itemIds)
            {
                NativeRecoveryObservation observation =
                    observeItem(itemId);
                candidate.NativeExpectations.Add(
                    new EquipmentSlotNativeExpectation
                    {
                        ItemId = itemId,
                        BackpackCount =
                            observation.BackpackCount,
                        MailCount = observation.MailCount
                    });
            }

            document.GameplayCandidate = candidate;
            return candidate;
        }

        internal static EquipmentSlotGameplayRecoveryDecision
            DecideRecovery(
                EquipmentSlotStorageDocument document,
                string currentSaveFingerprint,
                Func<string, NativeRecoveryObservation> observeItem)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            if (observeItem == null)
                throw new ArgumentNullException(nameof(observeItem));
            document.Normalize();
            EquipmentSlotGameplayCandidate candidate =
                RequireCandidate(document);
            if (candidate.Origin !=
                    EquipmentSlotTransactionOrigin
                        .GameplayMutation ||
                !candidate.Scope.Matches(document.Scope) ||
                candidate.BaseGeneration > document.Generation)
            {
                return FailClosed(
                    "Gameplay candidate identity, origin, or generation is invalid.");
            }

            string fingerprint =
                currentSaveFingerprint?.Trim() ?? string.Empty;
            if (candidate.State ==
                EquipmentSlotGameplayCandidateState.Prepared)
            {
                if (EquipmentSlotNativeCommitFingerprint
                    .Equivalent(
                        candidate.PreSaveFingerprint,
                        fingerprint))
                {
                    return new EquipmentSlotGameplayRecoveryDecision(
                        EquipmentSlotGameplayRecoveryAction
                            .DiscardUncommitted,
                        "Native save still matches the gameplay candidate preimage.");
                }
                return ExpectationsMatch(
                    candidate,
                    fingerprint,
                    observeItem,
                    out string reason)
                    ? new EquipmentSlotGameplayRecoveryDecision(
                        EquipmentSlotGameplayRecoveryAction
                            .PromoteCommitted,
                        reason)
                    : FailClosed(reason);
            }

            if (!EquipmentSlotNativeCommitFingerprint
                .Equivalent(
                    candidate.PostSaveFingerprint,
                    fingerprint))
            {
                return FailClosed(
                    "Committed gameplay tombstone does not match the current native save fingerprint.");
            }
            return ExpectationsMatch(
                candidate,
                fingerprint,
                observeItem,
                out string committedReason)
                ? new EquipmentSlotGameplayRecoveryDecision(
                    EquipmentSlotGameplayRecoveryAction
                        .FinalizeCommitted,
                    committedReason)
                : FailClosed(committedReason);
        }

        internal static void PromoteCommitted(
            EquipmentSlotStorageDocument document,
            string postSaveFingerprint,
            bool nativeSaveConfirmed = false)
        {
            EquipmentSlotGameplayCandidate candidate =
                RequireCandidate(document);
            if (candidate.State !=
                    EquipmentSlotGameplayCandidateState.Prepared ||
                candidate.Origin !=
                    EquipmentSlotTransactionOrigin
                        .GameplayMutation ||
                !candidate.Scope.Matches(document.Scope))
            {
                throw new InvalidOperationException(
                    "Only a matching prepared gameplay candidate can be promoted.");
            }
            string fingerprint =
                postSaveFingerprint?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(fingerprint) ||
                (!nativeSaveConfirmed &&
                 EquipmentSlotNativeCommitFingerprint
                    .Equivalent(
                        candidate.PreSaveFingerprint,
                        fingerprint)))
            {
                throw new InvalidOperationException(
                    "Gameplay promotion requires a changed native save fingerprint.");
            }

            document.Slots = CloneSlots(candidate.WorkingSlots);
            candidate.State =
                EquipmentSlotGameplayCandidateState
                    .CommittedTombstone;
            candidate.PostSaveFingerprint = fingerprint;
        }

        internal static void FinalizeCommitted(
            EquipmentSlotStorageDocument document)
        {
            EquipmentSlotGameplayCandidate candidate =
                RequireCandidate(document);
            if (candidate.State !=
                EquipmentSlotGameplayCandidateState
                    .CommittedTombstone)
            {
                throw new InvalidOperationException(
                    "Only a committed gameplay tombstone can be finalized.");
            }
            document.GameplayCandidate = null;
        }

        internal static void DiscardUncommitted(
            EquipmentSlotStorageDocument document)
        {
            EquipmentSlotGameplayCandidate candidate =
                RequireCandidate(document);
            if (candidate.State !=
                EquipmentSlotGameplayCandidateState.Prepared)
            {
                throw new InvalidOperationException(
                    "A committed gameplay tombstone cannot be discarded.");
            }
            document.GameplayCandidate = null;
        }

        internal static List<EquipmentSlotStorageEntry>
            CloneSlots(
                IReadOnlyList<EquipmentSlotStorageEntry> slots)
        {
            if (slots == null)
                throw new ArgumentNullException(nameof(slots));
            var clone = new List<EquipmentSlotStorageEntry>(
                MoreEquipmentSlotsProductContract.FixedSlotCount);
            for (int index = 0;
                 index < slots.Count;
                 index++)
            {
                EquipmentSlotStorageEntry source =
                    slots[index] ??
                    throw new InvalidOperationException(
                        "A gameplay slot projection contains a null slot.");
                clone.Add(CloneSlot(source, index));
            }
            return clone;
        }

        private static bool ExpectationsMatch(
            EquipmentSlotGameplayCandidate candidate,
            string fingerprint,
            Func<string, NativeRecoveryObservation> observeItem,
            out string reason)
        {
            foreach (EquipmentSlotNativeExpectation expectation in
                candidate.NativeExpectations)
            {
                NativeRecoveryObservation current =
                    observeItem(expectation.ItemId);
                if (current.BackpackCount !=
                        expectation.BackpackCount ||
                    current.MailCount != expectation.MailCount)
                {
                    reason =
                        "Gameplay native expectation drifted: item=" +
                        expectation.ItemId +
                        ", backpack=" +
                        current.BackpackCount +
                        "/" +
                        expectation.BackpackCount +
                        ", mail=" +
                        current.MailCount +
                        "/" +
                        expectation.MailCount +
                        ".";
                    return false;
                }
            }

            reason =
                "Native save " +
                fingerprint +
                " matches every gameplay candidate native expectation.";
            return true;
        }

        private static void AddOccupiedItemIds(
            IEnumerable<EquipmentSlotStorageEntry> slots,
            ISet<string> itemIds)
        {
            foreach (EquipmentSlotStorageEntry slot in slots)
            {
                if (slot != null && slot.IsOccupied)
                    itemIds.Add(slot.ItemId);
            }
        }

        private static EquipmentSlotStorageEntry CloneSlot(
            EquipmentSlotStorageEntry source,
            int index) =>
            new EquipmentSlotStorageEntry
            {
                Index = index,
                ItemId = source.ItemId,
                DisplayName = source.DisplayName,
                SkillId = source.SkillId,
                DefenseBonus = source.DefenseBonus,
                IsShield = source.IsShield,
                ShieldValue = source.ShieldValue,
                ShieldMaxValue = source.ShieldMaxValue,
                ShieldDefend = source.ShieldDefend
            };

        private static EquipmentSlotGameplayCandidate RequireCandidate(
            EquipmentSlotStorageDocument document) =>
            document.GameplayCandidate ??
            throw new InvalidOperationException(
                "No gameplay candidate is active.");

        private static EquipmentSlotGameplayRecoveryDecision
            FailClosed(string reason) =>
            new EquipmentSlotGameplayRecoveryDecision(
                EquipmentSlotGameplayRecoveryAction.FailClosed,
                reason);
    }
}
