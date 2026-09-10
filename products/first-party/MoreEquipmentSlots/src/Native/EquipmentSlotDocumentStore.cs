using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;

namespace DTMAPI.MoreEquipmentSlots
{
    internal sealed partial class EquipmentSlotDocumentStore
    {
        internal EquipmentSlotStorageDocument Load(
            string path,
            EquipmentSlotSaveScope expectedScope)
        {
            if (expectedScope == null)
                throw new ArgumentNullException(nameof(expectedScope));
            expectedScope.Normalize();
            if (string.IsNullOrWhiteSpace(path))
                return CreateEmpty(expectedScope);

            if (TryLoadValidated(
                path,
                expectedScope,
                out EquipmentSlotStorageDocument? loaded,
                out _,
                out string failure))
            {
                return loaded!;
            }

            if (File.Exists(path) ||
                File.Exists(path + ".previous"))
            {
                throw new InvalidDataException(
                    "Neither the live nor eligible previous equipment-slot sidecar was valid: " +
                    failure);
            }

            return CreateEmpty(expectedScope);
        }

        internal bool DeleteSlotDirectoryForNewGame(
            string sidecarPath,
            int archiveIndex)
        {
            if (archiveIndex < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(archiveIndex));
            if (string.IsNullOrWhiteSpace(sidecarPath))
            {
                throw new ArgumentException(
                    "A NewGame sidecar path is required.",
                    nameof(sidecarPath));
            }

            string fullSidecarPath =
                Path.GetFullPath(sidecarPath);
            string expectedFileName =
                "equipment-slots-" +
                MoreEquipmentSlotsProductContract.UniqueId +
                ".json";
            string? slotDirectory =
                Path.GetDirectoryName(fullSidecarPath);
            string? equipmentSlotsDirectory =
                slotDirectory == null
                    ? null
                    : Path.GetDirectoryName(slotDirectory);
            string? protectedItemsDirectory =
                equipmentSlotsDirectory == null
                    ? null
                    : Path.GetDirectoryName(
                        equipmentSlotsDirectory);
            string expectedSlotName =
                "slot-" + archiveIndex.ToString(
                    System.Globalization.CultureInfo
                        .InvariantCulture);
            if (!string.Equals(
                    Path.GetFileName(fullSidecarPath),
                    expectedFileName,
                    StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(slotDirectory) ||
                !string.Equals(
                    Path.GetFileName(slotDirectory),
                    expectedSlotName,
                    StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(
                    equipmentSlotsDirectory) ||
                !string.Equals(
                    Path.GetFileName(
                        equipmentSlotsDirectory),
                    "equipment-slots",
                    StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(
                    protectedItemsDirectory) ||
                !string.Equals(
                    Path.GetFileName(
                        protectedItemsDirectory),
                    "protected-items",
                    StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    "MoreEquipmentSlots refused a NewGame reset outside the exact Product slot-directory shape.");
            }

            string exactSlotDirectory = slotDirectory!;
            if (!Directory.Exists(exactSlotDirectory))
                return false;
            ThrowIfDirectoryContainsReparsePoint(
                exactSlotDirectory);
            Directory.Delete(
                exactSlotDirectory,
                recursive: true);
            if (Directory.Exists(exactSlotDirectory))
            {
                throw new IOException(
                    "The stale MoreEquipmentSlots Product slot directory remained after NewGame reset: " +
                    exactSlotDirectory);
            }
            return true;
        }

        internal void WriteAtomic(
            string path,
            EquipmentSlotStorageDocument document)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException(
                    "A sidecar path is required.",
                    nameof(path));
            }
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            if (!TryValidateRawV3(
                document,
                document.Scope,
                out string documentFailure))
            {
                throw new InvalidDataException(
                    "The equipment-slot sidecar cannot be normalized or written because its raw v3 structure is invalid: " +
                    documentFailure);
            }
            document.Normalize();
            if (document.Scope.ArchiveIndex < 0)
            {
                throw new InvalidOperationException(
                    "A loaded archive scope is required for a protected sidecar write.");
            }

            bool liveAuthorityRecoveredFromPrevious = false;
            if (File.Exists(path))
            {
                if (!TryReadValidated(
                    path,
                    document.Scope,
                    out EquipmentSlotStorageDocument? live,
                    out string liveFailure))
                {
                    string previousFailure = "not-checked";
                    if (!IsPreviousFallbackEligible(liveFailure) ||
                        !TryReadValidated(
                            path + ".previous",
                            document.Scope,
                            out live,
                            out previousFailure))
                    {
                        throw new InvalidDataException(
                            "The live equipment-slot sidecar cannot be replaced safely: " +
                            liveFailure +
                            "; previous=" +
                            previousFailure);
                    }
                    liveAuthorityRecoveredFromPrevious = true;
                }
                if (document.Generation <= live!.Generation)
                {
                    throw new InvalidOperationException(
                        "Equipment-slot sidecar generation must increase monotonically.");
                }
            }

            string? directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            string temporaryPath =
                path + ".tmp-" + Guid.NewGuid().ToString("N");
            string previousPath = path + ".previous";
            string rejectedLivePath =
                path + ".invalid-" + Guid.NewGuid().ToString("N");
            try
            {
                using (var stream = new FileStream(
                    temporaryPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None))
                {
                    CreateSerializer().WriteObject(stream, document);
                    stream.Flush(flushToDisk: true);
                }

                if (!TryReadValidated(
                    temporaryPath,
                    document.Scope,
                    out EquipmentSlotStorageDocument? roundTrip,
                    out string roundTripFailure) ||
                    roundTrip!.Generation != document.Generation ||
                    !JournalIdentityMatches(document, roundTrip))
                {
                    throw new InvalidDataException(
                        "Equipment-slot temporary sidecar failed durable identity verification: " +
                        roundTripFailure);
                }

                if (File.Exists(path))
                {
                    File.Replace(
                        temporaryPath,
                        path,
                        liveAuthorityRecoveredFromPrevious
                            ? rejectedLivePath
                            : previousPath,
                        ignoreMetadataErrors: true);
                }
                else
                {
                    File.Move(temporaryPath, path);
                }
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
                if (File.Exists(rejectedLivePath))
                {
                    try
                    {
                        File.Delete(rejectedLivePath);
                    }
                    catch
                    {
                        // The authoritative live and previous documents are
                        // already intact. A rejected corrupt backup is not
                        // part of the recovery authority.
                    }
                }
            }
        }

        internal static bool TryReadValidated(
            string path,
            EquipmentSlotSaveScope expectedScope,
            out EquipmentSlotStorageDocument? document,
            out string failure)
        {
            document = null;
            failure = string.Empty;
            if (string.IsNullOrWhiteSpace(path) ||
                !File.Exists(path))
            {
                failure = "file-missing";
                return false;
            }

            try
            {
                using (FileStream stream = File.OpenRead(path))
                {
                    document =
                        CreateSerializer().ReadObject(stream) as
                        EquipmentSlotStorageDocument;
                }
                if (document == null)
                {
                    failure = "document-null";
                    return false;
                }

                if (!TryValidateRawV3(
                    document,
                    expectedScope,
                    out failure))
                {
                    document = null;
                    return false;
                }

                document.Normalize();
                return true;
            }
            catch (Exception ex)
            {
                failure =
                    ex.GetType().Name + ": " + ex.Message;
                document = null;
                return false;
            }
        }

        internal static bool TryLoadValidated(
            string path,
            EquipmentSlotSaveScope expectedScope,
            out EquipmentSlotStorageDocument? document,
            out string sourcePath,
            out string failure)
        {
            sourcePath = string.Empty;
            if (TryReadValidated(
                path,
                expectedScope,
                out document,
                out failure))
            {
                sourcePath = path;
                return true;
            }

            string liveFailure = failure;
            if (!IsPreviousFallbackEligible(liveFailure))
            {
                document = null;
                return false;
            }

            string previousPath = path + ".previous";
            if (TryReadValidated(
                previousPath,
                expectedScope,
                out document,
                out string previousFailure))
            {
                sourcePath = previousPath;
                failure =
                    "live=" + liveFailure +
                    "; recovered=previous";
                return true;
            }

            failure =
                "live=" + liveFailure +
                "; previous=" + previousFailure;
            document = null;
            return false;
        }

        private static bool TryValidateRawV3(
            EquipmentSlotStorageDocument document,
            EquipmentSlotSaveScope expectedScope,
            out string failure)
        {
            failure = string.Empty;
            if (document.SchemaVersion !=
                MoreEquipmentSlotsProductContract
                    .StorageSchemaVersion)
            {
                failure = "schema-mismatch";
                return false;
            }
            if (expectedScope == null ||
                document.Scope == null ||
                document.Scope.ArchiveIndex < 0 ||
                !IsNormalizedText(
                    document.Scope.PlayerName) ||
                !IsNormalizedText(
                    document.Scope.CustomPlayerName) ||
                !RawScopeIdentityMatches(
                    document.Scope,
                    expectedScope))
            {
                failure = "scope-mismatch";
                return false;
            }
            if (!document.Scope.TotalGameSeconds.HasValue ||
                document.Scope.TotalGameSeconds.Value < 0)
            {
                failure = "scope-revision-missing";
                return false;
            }
            if (!expectedScope.TotalGameSeconds.HasValue ||
                expectedScope.TotalGameSeconds.Value < 0)
            {
                failure = "current-scope-revision-missing";
                return false;
            }
            if (!RawScopeRevisionIsCompatible(
                document.Scope,
                expectedScope))
            {
                failure = "scope-revision-regressed";
                return false;
            }
            if (document.Generation < 0)
            {
                failure = "generation-invalid";
                return false;
            }
            if (document.Slots == null ||
                document.Slots.Count !=
                    MoreEquipmentSlotsProductContract
                        .FixedSlotCount)
            {
                failure = "slot-count-invalid";
                return false;
            }

            var slotIndexes = new HashSet<int>();
            for (int position = 0;
                 position < document.Slots.Count;
                 position++)
            {
                EquipmentSlotStorageEntry entry =
                    document.Slots[position];
                if (entry == null ||
                    entry.Index != position)
                {
                    failure = "slot-order-invalid";
                    return false;
                }
                if (!TryValidateRawSlot(
                    entry,
                    requireOccupied: false,
                    out failure) ||
                    !slotIndexes.Add(entry.Index))
                {
                    if (string.IsNullOrWhiteSpace(failure))
                        failure = "slot-index-duplicate";
                    return false;
                }
            }
            for (int index = 0;
                 index <
                    MoreEquipmentSlotsProductContract
                        .FixedSlotCount;
                 index++)
            {
                if (!slotIndexes.Contains(index))
                {
                    failure = "slot-index-missing";
                    return false;
                }
            }

            if (document.Journal != null &&
                document.GameplayCandidate != null)
            {
                failure = "transaction-shape-conflict";
                return false;
            }
            if (document.GameplayCandidate != null &&
                !TryValidateGameplayCandidate(
                    document,
                    document.GameplayCandidate,
                    out failure))
            {
                return false;
            }
            if (document.LegacyMigration != null &&
                !TryValidateLegacyMigrationStamp(
                    document.LegacyMigration,
                    out failure))
            {
                return false;
            }

            EquipmentSlotTransactionJournal? journal =
                document.Journal;
            if (journal == null)
                return true;
            if (!Enum.IsDefined(
                    typeof(EquipmentSlotJournalState),
                    journal.State) ||
                !Enum.IsDefined(
                    typeof(EquipmentSlotTransactionOrigin),
                    journal.Origin) ||
                journal.Origin ==
                    EquipmentSlotTransactionOrigin
                        .GameplayMutation ||
                !Guid.TryParseExact(
                    journal.TransactionId,
                    "N",
                    out _) ||
                journal.Scope == null ||
                !IsNormalizedText(
                    journal.Scope.PlayerName) ||
                !IsNormalizedText(
                    journal.Scope.CustomPlayerName) ||
                !RawScopeMatches(
                    journal.Scope,
                    document.Scope) ||
                journal.BaseGeneration < 0 ||
                journal.BaseGeneration >
                    document.Generation)
            {
                failure = "journal-identity-mismatch";
                return false;
            }
            if (journal.Escrow == null ||
                journal.Replacements == null ||
                (journal.Escrow.Count == 0 &&
                 journal.Replacements.Count == 0) ||
                journal.Escrow.Count >
                    MoreEquipmentSlotsProductContract
                        .FixedSlotCount ||
                journal.Replacements.Count > 1)
            {
                failure = "journal-shape-invalid";
                return false;
            }
            if (!IsNormalizedText(
                    journal.PreSaveFingerprint) ||
                !IsNormalizedText(
                    journal.PostSaveFingerprint) ||
                journal.IncomingBeforeBackpackCount < 0 ||
                journal.IncomingExpectedBackpackCount < 0)
            {
                failure = "journal-observation-invalid";
                return false;
            }
            if (!journal.AttemptStarted &&
                (!string.IsNullOrEmpty(
                    journal.PreSaveFingerprint) ||
                 !string.IsNullOrEmpty(
                    journal.PostSaveFingerprint) ||
                 journal.IncomingAttemptCompleted ||
                 journal.IncomingSucceeded ||
                 journal.IncomingFromNativeBuffer))
            {
                failure =
                    "journal-attempt-state-invalid";
                return false;
            }
            if (journal.AttemptStarted &&
                string.IsNullOrWhiteSpace(
                    journal.PreSaveFingerprint))
            {
                failure =
                    "journal-preimage-missing";
                return false;
            }
            if (journal.State ==
                    EquipmentSlotJournalState.Prepared &&
                !string.IsNullOrEmpty(
                    journal.PostSaveFingerprint))
            {
                failure =
                    "journal-prepared-postimage-invalid";
                return false;
            }

            var escrowIndexes = new HashSet<int>();
            foreach (EquipmentSlotEscrowEntry escrow in
                journal.Escrow)
            {
                if (escrow == null ||
                    escrow.SlotIndex < 0 ||
                    escrow.SlotIndex >=
                        MoreEquipmentSlotsProductContract
                            .FixedSlotCount ||
                    !escrowIndexes.Add(escrow.SlotIndex) ||
                    string.IsNullOrWhiteSpace(
                        escrow.ItemId) ||
                    !IsNormalizedText(escrow.ItemId) ||
                    !IsNormalizedText(
                        escrow.DisplayName) ||
                    !IsNormalizedText(escrow.SkillId) ||
                    escrow.DefenseBonus < 0 ||
                    escrow.ShieldValue < 0 ||
                    escrow.ShieldMaxValue < 0 ||
                    escrow.ShieldDefend < 0 ||
                    escrow.ShieldValue >
                        escrow.ShieldMaxValue ||
                    (!escrow.IsShield &&
                     (escrow.ShieldValue != 0 ||
                      escrow.ShieldMaxValue != 0 ||
                      escrow.ShieldDefend != 0)) ||
                    (escrow.IsShield &&
                     escrow.ShieldMaxValue <= 0) ||
                    escrow.BeforeBackpackCount < 0 ||
                    escrow.BeforeMailCount < 0 ||
                    escrow.ExpectedBackpackCount < 0 ||
                    escrow.ExpectedMailCount < 0 ||
                    !Enum.IsDefined(
                        typeof(NativePlacementKind),
                        escrow.Placement))
                {
                    failure = "journal-escrow-invalid";
                    return false;
                }
                if (!journal.AttemptStarted &&
                    (escrow.AttemptCompleted ||
                     escrow.BeforeBackpackCount != 0 ||
                     escrow.BeforeMailCount != 0 ||
                     escrow.ExpectedBackpackCount != 0 ||
                     escrow.ExpectedMailCount != 0 ||
                     escrow.Placement !=
                        NativePlacementKind.Failure))
                {
                    failure =
                        "journal-attempt-state-invalid";
                    return false;
                }
                EquipmentSlotStorageEntry projected =
                    document.Slots[escrow.SlotIndex];
                if (journal.State ==
                        EquipmentSlotJournalState.Prepared &&
                    projected.IsOccupied)
                {
                    failure =
                        "journal-prepared-slot-not-empty";
                    return false;
                }
            }
            if (journal.AttemptStarted &&
                !TryValidateRawAttemptEvidence(
                    journal,
                    out failure))
            {
                return false;
            }

            if (journal.Replacements.Count == 1)
            {
                EquipmentSlotStorageEntry replacement =
                    journal.Replacements[0];
                if (!TryValidateRawSlot(
                        replacement,
                        requireOccupied: true,
                        out failure) ||
                    (journal.Escrow.Count == 1 &&
                     journal.Escrow[0].SlotIndex !=
                        replacement.Index) ||
                    journal.Escrow.Count > 1)
                {
                    if (string.IsNullOrWhiteSpace(failure))
                        failure =
                            "journal-replacement-shape-invalid";
                    return false;
                }
                if (journal.IncomingSucceeded &&
                    !journal.IncomingAttemptCompleted)
                {
                    failure =
                        "journal-incoming-state-invalid";
                    return false;
                }
                foreach (EquipmentSlotEscrowEntry escrow in
                    journal.Escrow)
                {
                    if (string.Equals(
                            escrow.ItemId,
                            replacement.ItemId,
                            StringComparison.Ordinal) &&
                        escrow.BeforeBackpackCount !=
                            journal
                                .IncomingBeforeBackpackCount)
                    {
                        failure =
                            "journal-same-item-baseline-mismatch";
                        return false;
                    }
                }
                EquipmentSlotStorageEntry projected =
                    document.Slots[replacement.Index];
                bool outgoingFailed =
                    journal.Escrow.Count == 1 &&
                    journal.Escrow[0].Placement ==
                        NativePlacementKind.Failure;
                if (outgoingFailed &&
                    journal.IncomingSucceeded)
                {
                    failure =
                        "journal-incoming-after-outgoing-failure";
                    return false;
                }
                bool replacementProjected =
                    journal.State ==
                        EquipmentSlotJournalState
                            .CommittedTombstone &&
                    journal.IncomingSucceeded &&
                    !outgoingFailed;
                if (replacementProjected
                        ? !RawSlotIdentityMatches(
                            projected,
                            replacement) ||
                            !string.Equals(
                                projected.DisplayName,
                                replacement.DisplayName,
                                StringComparison.Ordinal)
                        : projected.IsOccupied)
                {
                    failure =
                        "journal-replacement-projection-invalid";
                    return false;
                }
            }
            else if (journal.IncomingAttemptCompleted ||
                journal.IncomingSucceeded ||
                journal.IncomingFromNativeBuffer ||
                journal.IncomingBeforeBackpackCount != 0 ||
                journal.IncomingExpectedBackpackCount != 0)
            {
                failure =
                    "journal-incoming-without-replacement";
                return false;
            }

            if (journal.State ==
                EquipmentSlotJournalState
                    .CommittedTombstone &&
                (!journal.AttemptStarted ||
                 string.IsNullOrWhiteSpace(
                    journal.PostSaveFingerprint) ||
                 HasIncompleteEscrow(journal) ||
                 (journal.Replacements.Count == 1 &&
                  !journal.IncomingAttemptCompleted)))
            {
                failure =
                    "journal-committed-state-invalid";
                return false;
            }
            if (journal.State ==
                EquipmentSlotJournalState
                    .CommittedTombstone)
            {
                foreach (EquipmentSlotEscrowEntry escrow in
                    journal.Escrow)
                {
                    EquipmentSlotStorageEntry projected =
                        document.Slots[escrow.SlotIndex];
                    bool allowedReplacement =
                        journal.Replacements.Count == 1 &&
                        journal.IncomingSucceeded &&
                        escrow.Placement !=
                            NativePlacementKind.Failure &&
                        RawSlotIdentityMatches(
                            projected,
                            journal.Replacements[0]);
                    if (projected.IsOccupied &&
                        !allowedReplacement)
                    {
                        failure =
                            "journal-committed-escrow-projection-invalid";
                        return false;
                    }
                }
            }
            return true;
        }

        private static bool TryValidateLegacyMigrationStamp(
            EquipmentSlotLegacyMigrationStamp stamp,
            out string failure)
        {
            failure = string.Empty;
            if (stamp == null ||
                (!string.Equals(
                    stamp.SourceKind,
                    EquipmentSlotLegacyMigrationSourceKinds
                        .ScopedFlat,
                    StringComparison.Ordinal) &&
                 !string.Equals(
                    stamp.SourceKind,
                    EquipmentSlotLegacyMigrationSourceKinds
                        .GlobalFlat,
                    StringComparison.Ordinal) &&
                 !string.Equals(
                    stamp.SourceKind,
                    EquipmentSlotLegacyMigrationSourceKinds
                        .PreSchemaGlobal,
                    StringComparison.Ordinal)) ||
                (stamp.SourceSchema == 0
                    ? !string.Equals(
                        stamp.SourceKind,
                        EquipmentSlotLegacyMigrationSourceKinds
                            .PreSchemaGlobal,
                        StringComparison.Ordinal)
                    : stamp.SourceSchema < 1 ||
                        stamp.SourceSchema > 3) ||
                stamp.SourceSha256 == null ||
                stamp.SourceSha256.Length != 64)
            {
                failure = "legacy-migration-stamp-invalid";
                return false;
            }
            for (int index = 0;
                 index < stamp.SourceSha256.Length;
                 index++)
            {
                char value = stamp.SourceSha256[index];
                if (!((value >= '0' && value <= '9') ||
                      (value >= 'A' && value <= 'F')))
                {
                    failure =
                        "legacy-migration-hash-invalid";
                    return false;
                }
            }
            return true;
        }

        private static bool TryValidateGameplayCandidate(
            EquipmentSlotStorageDocument document,
            EquipmentSlotGameplayCandidate candidate,
            out string failure)
        {
            failure = string.Empty;
            if (!Enum.IsDefined(
                    typeof(EquipmentSlotGameplayCandidateState),
                    candidate.State) ||
                candidate.Origin !=
                    EquipmentSlotTransactionOrigin.GameplayMutation ||
                !Guid.TryParseExact(
                    candidate.TransactionId,
                    "N",
                    out _) ||
                candidate.Scope == null ||
                !IsNormalizedText(
                    candidate.Scope.PlayerName) ||
                !IsNormalizedText(
                    candidate.Scope.CustomPlayerName) ||
                !RawScopeMatches(
                    candidate.Scope,
                    document.Scope) ||
                candidate.BaseGeneration < 0 ||
                candidate.BaseGeneration >
                    document.Generation ||
                string.IsNullOrWhiteSpace(
                    candidate.PreSaveFingerprint) ||
                !IsNormalizedText(
                    candidate.PreSaveFingerprint) ||
                !IsNormalizedText(
                    candidate.PostSaveFingerprint))
            {
                failure =
                    "gameplay-candidate-identity-invalid";
                return false;
            }
            if (candidate.State ==
                    EquipmentSlotGameplayCandidateState.Prepared
                    ? !string.IsNullOrEmpty(
                        candidate.PostSaveFingerprint)
                    : string.IsNullOrWhiteSpace(
                        candidate.PostSaveFingerprint))
            {
                failure =
                    "gameplay-candidate-phase-invalid";
                return false;
            }
            if (candidate.WorkingSlots == null ||
                candidate.WorkingSlots.Count !=
                    MoreEquipmentSlotsProductContract
                        .FixedSlotCount)
            {
                failure =
                    "gameplay-candidate-slot-count-invalid";
                return false;
            }
            for (int index = 0;
                 index < candidate.WorkingSlots.Count;
                 index++)
            {
                EquipmentSlotStorageEntry slot =
                    candidate.WorkingSlots[index];
                if (slot == null ||
                    slot.Index != index ||
                    !TryValidateRawSlot(
                        slot,
                        requireOccupied: false,
                        out failure))
                {
                    if (string.IsNullOrWhiteSpace(failure))
                    {
                        failure =
                            "gameplay-candidate-slot-invalid";
                    }
                    return false;
                }
                if (candidate.State ==
                        EquipmentSlotGameplayCandidateState
                            .CommittedTombstone &&
                    (!RawSlotIdentityMatches(
                        document.Slots[index],
                        slot) ||
                     !string.Equals(
                        document.Slots[index].DisplayName,
                        slot.DisplayName,
                        StringComparison.Ordinal)))
                {
                    failure =
                        "gameplay-candidate-projection-invalid";
                    return false;
                }
            }

            if (candidate.NativeExpectations == null)
            {
                failure =
                    "gameplay-candidate-expectations-null";
                return false;
            }
            var itemIds = new HashSet<string>(
                StringComparer.Ordinal);
            foreach (EquipmentSlotNativeExpectation expectation in
                candidate.NativeExpectations)
            {
                if (expectation == null ||
                    string.IsNullOrWhiteSpace(
                        expectation.ItemId) ||
                    !IsNormalizedText(expectation.ItemId) ||
                    expectation.BackpackCount < 0 ||
                    expectation.MailCount < 0 ||
                    !itemIds.Add(expectation.ItemId))
                {
                    failure =
                        "gameplay-candidate-expectation-invalid";
                    return false;
                }
            }
            return true;
        }

        private static bool TryValidateRawAttemptEvidence(
            EquipmentSlotTransactionJournal journal,
            out string failure)
        {
            failure = string.Empty;
            var baselines =
                new Dictionary<string, NativeRecoveryObservation>(
                    StringComparer.Ordinal);
            var backpackDeltas =
                new Dictionary<string, int>(
                    StringComparer.Ordinal);
            var mailDeltas =
                new Dictionary<string, int>(
                    StringComparer.Ordinal);
            foreach (EquipmentSlotEscrowEntry escrow in
                journal.Escrow)
            {
                if (baselines.TryGetValue(
                        escrow.ItemId,
                        out NativeRecoveryObservation baseline))
                {
                    if (baseline.BackpackCount !=
                            escrow.BeforeBackpackCount ||
                        baseline.MailCount !=
                            escrow.BeforeMailCount)
                    {
                        failure =
                            "journal-item-baseline-mismatch";
                        return false;
                    }
                }
                else
                {
                    baselines.Add(
                        escrow.ItemId,
                        new NativeRecoveryObservation(
                            string.Empty,
                            escrow.BeforeBackpackCount,
                            escrow.BeforeMailCount));
                    backpackDeltas.Add(escrow.ItemId, 0);
                    mailDeltas.Add(escrow.ItemId, 0);
                }

                if (!escrow.AttemptCompleted)
                {
                    if (escrow.Placement !=
                            NativePlacementKind.Failure ||
                        escrow.ExpectedBackpackCount !=
                            escrow.BeforeBackpackCount ||
                        escrow.ExpectedMailCount !=
                            escrow.BeforeMailCount)
                    {
                        failure =
                            "journal-partial-attempt-invalid";
                        return false;
                    }
                    continue;
                }

                if (escrow.Placement ==
                    NativePlacementKind.Backpack)
                {
                    backpackDeltas[escrow.ItemId]++;
                }
                else if (escrow.Placement ==
                    NativePlacementKind.Mail)
                {
                    mailDeltas[escrow.ItemId]++;
                }
                if (escrow.ExpectedBackpackCount !=
                        escrow.BeforeBackpackCount +
                        backpackDeltas[escrow.ItemId] ||
                    escrow.ExpectedMailCount !=
                        escrow.BeforeMailCount +
                        mailDeltas[escrow.ItemId])
                {
                    failure =
                        "journal-placement-observation-invalid";
                    return false;
                }
            }

            if (journal.Replacements.Count == 1)
            {
                string incomingItemId =
                    journal.Replacements[0].ItemId;
                int outgoingBackpackDelta =
                    backpackDeltas.TryGetValue(
                        incomingItemId,
                        out int observedDelta)
                        ? observedDelta
                        : 0;
                if (baselines.TryGetValue(
                        incomingItemId,
                        out NativeRecoveryObservation baseline) &&
                    journal.IncomingBeforeBackpackCount !=
                        baseline.BackpackCount)
                {
                    failure =
                        "journal-same-item-baseline-mismatch";
                    return false;
                }
                int expectedIncomingCount =
                    journal.IncomingBeforeBackpackCount +
                    outgoingBackpackDelta +
                    (journal.IncomingSucceeded &&
                     !journal.IncomingFromNativeBuffer
                        ? -1
                        : 0);
                if (journal.IncomingAttemptCompleted &&
                    journal.IncomingExpectedBackpackCount !=
                        Math.Max(0, expectedIncomingCount))
                {
                    failure =
                        "journal-incoming-observation-invalid";
                    return false;
                }
                if (!journal.IncomingAttemptCompleted &&
                    (journal.IncomingSucceeded ||
                     journal.IncomingFromNativeBuffer ||
                     journal.IncomingExpectedBackpackCount !=
                        journal.IncomingBeforeBackpackCount))
                {
                    failure =
                        "journal-incoming-state-invalid";
                    return false;
                }
            }
            return true;
        }

        private static bool RawSlotIdentityMatches(
            EquipmentSlotStorageEntry left,
            EquipmentSlotStorageEntry right) =>
            left != null &&
            right != null &&
            left.Index == right.Index &&
            string.Equals(
                left.ItemId,
                right.ItemId,
                StringComparison.Ordinal) &&
            string.Equals(
                left.SkillId,
                right.SkillId,
                StringComparison.Ordinal) &&
            left.DefenseBonus == right.DefenseBonus &&
            left.IsShield == right.IsShield &&
            left.ShieldValue == right.ShieldValue &&
            left.ShieldMaxValue ==
                right.ShieldMaxValue &&
            left.ShieldDefend == right.ShieldDefend;

        private static bool TryValidateRawSlot(
            EquipmentSlotStorageEntry entry,
            bool requireOccupied,
            out string failure)
        {
            failure = string.Empty;
            if (entry == null ||
                entry.Index < 0 ||
                entry.Index >=
                    MoreEquipmentSlotsProductContract
                        .FixedSlotCount ||
                !IsNormalizedText(entry.ItemId) ||
                !IsNormalizedText(entry.DisplayName) ||
                !IsNormalizedText(entry.SkillId) ||
                entry.DefenseBonus < 0 ||
                entry.ShieldValue < 0 ||
                entry.ShieldMaxValue < 0 ||
                entry.ShieldDefend < 0 ||
                entry.ShieldValue >
                    entry.ShieldMaxValue)
            {
                failure = "slot-value-invalid";
                return false;
            }

            bool occupied =
                !string.IsNullOrWhiteSpace(entry.ItemId);
            if (requireOccupied && !occupied)
            {
                failure = "slot-item-missing";
                return false;
            }
            if (!occupied &&
                (!string.IsNullOrEmpty(
                    entry.DisplayName) ||
                 !string.IsNullOrEmpty(entry.SkillId) ||
                 entry.DefenseBonus != 0 ||
                 entry.IsShield ||
                 entry.ShieldValue != 0 ||
                 entry.ShieldMaxValue != 0 ||
                 entry.ShieldDefend != 0))
            {
                failure =
                    "empty-slot-metadata-invalid";
                return false;
            }
            if (occupied &&
                entry.IsShield &&
                entry.ShieldMaxValue <= 0)
            {
                failure = "shield-capacity-invalid";
                return false;
            }
            if (!entry.IsShield &&
                (entry.ShieldValue != 0 ||
                 entry.ShieldMaxValue != 0 ||
                 entry.ShieldDefend != 0))
            {
                failure =
                    "non-shield-metadata-invalid";
                return false;
            }
            return true;
        }

        private static bool RawScopeMatches(
            EquipmentSlotSaveScope left,
            EquipmentSlotSaveScope right) =>
            RawScopeIdentityMatches(left, right) &&
            left.TotalGameSeconds == right.TotalGameSeconds;

        private static bool RawScopeIdentityMatches(
            EquipmentSlotSaveScope left,
            EquipmentSlotSaveScope right) =>
            left.ArchiveIndex == right.ArchiveIndex &&
            string.Equals(
                NormalizeText(left.PlayerName),
                NormalizeText(right.PlayerName),
                StringComparison.Ordinal) &&
            string.Equals(
                NormalizeText(left.CustomPlayerName),
                NormalizeText(right.CustomPlayerName),
                StringComparison.Ordinal);

        private static bool RawScopeRevisionIsCompatible(
            EquipmentSlotSaveScope document,
            EquipmentSlotSaveScope current)
        {
            return current.TotalGameSeconds.GetValueOrDefault() +
                    EquipmentSlotSaveScope
                        .SaveClockRegressionToleranceSeconds >=
                document.TotalGameSeconds.GetValueOrDefault();
        }

        private static bool HasIncompleteEscrow(
            EquipmentSlotTransactionJournal journal)
        {
            foreach (EquipmentSlotEscrowEntry escrow in
                journal.Escrow)
            {
                if (!escrow.AttemptCompleted)
                    return true;
            }
            return false;
        }

        private static bool IsNormalizedText(string value) =>
            value != null &&
            string.Equals(
                value,
                value.Trim(),
                StringComparison.Ordinal);

        private static bool IsPreviousFallbackEligible(
            string failure) =>
            !string.Equals(
                failure,
                "schema-mismatch",
                StringComparison.Ordinal) &&
            !string.Equals(
                failure,
                "scope-mismatch",
                StringComparison.Ordinal) &&
            !string.Equals(
                failure,
                "scope-revision-regressed",
                StringComparison.Ordinal) &&
            !string.Equals(
                failure,
                "scope-revision-missing",
                StringComparison.Ordinal) &&
            !string.Equals(
                failure,
                "current-scope-revision-missing",
                StringComparison.Ordinal);

        private static string NormalizeText(string value) =>
            value?.Trim() ?? string.Empty;

        private static bool JournalIdentityMatches(
            EquipmentSlotStorageDocument expected,
            EquipmentSlotStorageDocument actual)
        {
            if (expected.SchemaVersion != actual.SchemaVersion ||
                expected.Generation != actual.Generation ||
                !RawScopeMatches(expected.Scope, actual.Scope) ||
                expected.Slots.Count != actual.Slots.Count ||
                !LegacyMigrationIdentityMatches(
                    expected.LegacyMigration,
                    actual.LegacyMigration))
            {
                return false;
            }
            for (int index = 0;
                 index < expected.Slots.Count;
                 index++)
            {
                EquipmentSlotStorageEntry left =
                    expected.Slots[index];
                EquipmentSlotStorageEntry right =
                    actual.Slots[index];
                if (!RawSlotIdentityMatches(left, right) ||
                    !string.Equals(
                        left.DisplayName,
                        right.DisplayName,
                        StringComparison.Ordinal))
                {
                    return false;
                }
            }

            EquipmentSlotTransactionJournal? leftJournal =
                expected.Journal;
            EquipmentSlotTransactionJournal? rightJournal =
                actual.Journal;
            if (leftJournal == null ||
                rightJournal == null)
            {
                return leftJournal == null &&
                    rightJournal == null &&
                    GameplayCandidateIdentityMatches(
                        expected.GameplayCandidate,
                        actual.GameplayCandidate);
            }

            if (!string.Equals(
                    leftJournal.TransactionId,
                    rightJournal.TransactionId,
                    StringComparison.Ordinal) ||
                leftJournal.State != rightJournal.State ||
                leftJournal.Origin != rightJournal.Origin ||
                !RawScopeMatches(
                    leftJournal.Scope,
                    rightJournal.Scope) ||
                leftJournal.BaseGeneration !=
                    rightJournal.BaseGeneration ||
                leftJournal.AttemptStarted !=
                    rightJournal.AttemptStarted ||
                !string.Equals(
                    leftJournal.PreSaveFingerprint,
                    rightJournal.PreSaveFingerprint,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    leftJournal.PostSaveFingerprint,
                    rightJournal.PostSaveFingerprint,
                    StringComparison.Ordinal) ||
                leftJournal.IncomingAttemptCompleted !=
                    rightJournal.IncomingAttemptCompleted ||
                leftJournal.IncomingSucceeded !=
                    rightJournal.IncomingSucceeded ||
                leftJournal.IncomingBeforeBackpackCount !=
                    rightJournal.IncomingBeforeBackpackCount ||
                leftJournal.IncomingExpectedBackpackCount !=
                    rightJournal.IncomingExpectedBackpackCount ||
                leftJournal.IncomingFromNativeBuffer !=
                    rightJournal.IncomingFromNativeBuffer ||
                leftJournal.Escrow.Count !=
                    rightJournal.Escrow.Count ||
                leftJournal.Replacements.Count !=
                    rightJournal.Replacements.Count)
            {
                return false;
            }
            for (int index = 0;
                 index < leftJournal.Escrow.Count;
                 index++)
            {
                EquipmentSlotEscrowEntry left =
                    leftJournal.Escrow[index];
                EquipmentSlotEscrowEntry right =
                    rightJournal.Escrow[index];
                if (left.SlotIndex != right.SlotIndex ||
                    !string.Equals(
                        left.ItemId,
                        right.ItemId,
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        left.DisplayName,
                        right.DisplayName,
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        left.SkillId,
                        right.SkillId,
                        StringComparison.Ordinal) ||
                    left.DefenseBonus != right.DefenseBonus ||
                    left.IsShield != right.IsShield ||
                    left.ShieldValue != right.ShieldValue ||
                    left.ShieldMaxValue !=
                        right.ShieldMaxValue ||
                    left.ShieldDefend != right.ShieldDefend ||
                    left.BeforeBackpackCount !=
                        right.BeforeBackpackCount ||
                    left.BeforeMailCount !=
                        right.BeforeMailCount ||
                    left.ExpectedBackpackCount !=
                        right.ExpectedBackpackCount ||
                    left.ExpectedMailCount !=
                        right.ExpectedMailCount ||
                    left.Placement != right.Placement ||
                    left.AttemptCompleted !=
                        right.AttemptCompleted)
                {
                    return false;
                }
            }
            for (int index = 0;
                 index < leftJournal.Replacements.Count;
                 index++)
            {
                EquipmentSlotStorageEntry left =
                    leftJournal.Replacements[index];
                EquipmentSlotStorageEntry right =
                    rightJournal.Replacements[index];
                if (!RawSlotIdentityMatches(left, right) ||
                    !string.Equals(
                        left.DisplayName,
                        right.DisplayName,
                        StringComparison.Ordinal))
                {
                    return false;
                }
            }
            return GameplayCandidateIdentityMatches(
                expected.GameplayCandidate,
                actual.GameplayCandidate);
        }

        private static bool GameplayCandidateIdentityMatches(
            EquipmentSlotGameplayCandidate? left,
            EquipmentSlotGameplayCandidate? right)
        {
            if (left == null || right == null)
                return left == null && right == null;
            if (!string.Equals(
                    left.TransactionId,
                    right.TransactionId,
                    StringComparison.Ordinal) ||
                left.State != right.State ||
                left.Origin != right.Origin ||
                !RawScopeMatches(left.Scope, right.Scope) ||
                left.BaseGeneration != right.BaseGeneration ||
                !string.Equals(
                    left.PreSaveFingerprint,
                    right.PreSaveFingerprint,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    left.PostSaveFingerprint,
                    right.PostSaveFingerprint,
                    StringComparison.Ordinal) ||
                left.WorkingSlots.Count != right.WorkingSlots.Count ||
                left.NativeExpectations.Count !=
                    right.NativeExpectations.Count)
            {
                return false;
            }
            for (int index = 0;
                 index < left.WorkingSlots.Count;
                 index++)
            {
                EquipmentSlotStorageEntry leftSlot =
                    left.WorkingSlots[index];
                EquipmentSlotStorageEntry rightSlot =
                    right.WorkingSlots[index];
                if (!RawSlotIdentityMatches(leftSlot, rightSlot) ||
                    !string.Equals(
                        leftSlot.DisplayName,
                        rightSlot.DisplayName,
                        StringComparison.Ordinal))
                {
                    return false;
                }
            }
            for (int index = 0;
                 index < left.NativeExpectations.Count;
                 index++)
            {
                EquipmentSlotNativeExpectation leftExpectation =
                    left.NativeExpectations[index];
                EquipmentSlotNativeExpectation rightExpectation =
                    right.NativeExpectations[index];
                if (!string.Equals(
                        leftExpectation.ItemId,
                        rightExpectation.ItemId,
                        StringComparison.Ordinal) ||
                    leftExpectation.BackpackCount !=
                        rightExpectation.BackpackCount ||
                    leftExpectation.MailCount !=
                        rightExpectation.MailCount)
                {
                    return false;
                }
            }
            return true;
        }

        private static bool LegacyMigrationIdentityMatches(
            EquipmentSlotLegacyMigrationStamp? left,
            EquipmentSlotLegacyMigrationStamp? right)
        {
            if (left == null || right == null)
                return left == null && right == null;
            return string.Equals(
                    left.SourceKind,
                    right.SourceKind,
                    StringComparison.Ordinal) &&
                left.SourceSchema == right.SourceSchema &&
                string.Equals(
                    left.SourceSha256,
                    right.SourceSha256,
                    StringComparison.Ordinal);
        }

        private static DataContractJsonSerializer CreateSerializer() =>
            new DataContractJsonSerializer(
                typeof(EquipmentSlotStorageDocument));

        internal static EquipmentSlotStorageDocument CreateEmpty(
            EquipmentSlotSaveScope scope) =>
            new EquipmentSlotStorageDocument
            {
                Scope = scope.Clone()
            };

        private static void ThrowIfDirectoryContainsReparsePoint(
            string root)
        {
            var pending = new Stack<DirectoryInfo>();
            var rootInfo = new DirectoryInfo(root);
            if ((rootInfo.Attributes &
                 FileAttributes.ReparsePoint) != 0)
            {
                throw new InvalidDataException(
                    "MoreEquipmentSlots refused to delete a reparse-point Product slot directory during NewGame reset.");
            }
            pending.Push(rootInfo);
            while (pending.Count > 0)
            {
                DirectoryInfo directory = pending.Pop();
                foreach (FileSystemInfo entry in
                    directory.EnumerateFileSystemInfos())
                {
                    if ((entry.Attributes &
                         FileAttributes.ReparsePoint) != 0)
                    {
                        throw new InvalidDataException(
                            "MoreEquipmentSlots refused to traverse reparse-point residue during NewGame reset: " +
                            entry.FullName);
                    }
                    if (entry is DirectoryInfo child)
                        pending.Push(child);
                }
            }
        }
    }
}
