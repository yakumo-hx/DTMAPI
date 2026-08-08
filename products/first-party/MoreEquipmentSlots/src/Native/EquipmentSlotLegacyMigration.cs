using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;

namespace DTMAPI.MoreEquipmentSlots
{
    internal static class EquipmentSlotLegacyMigration
    {
        private const int OldestSupportedSchema = 1;
        private const int NewestSupportedSchema = 3;

        internal static bool TryConvert(
            string sourcePath,
            EquipmentSlotSaveScope expectedScope,
            bool globalSource,
            out EquipmentSlotStorageDocument? document,
            out string sourceSha256,
            out string failure)
        {
            document = null;
            sourceSha256 = string.Empty;
            failure = string.Empty;
            if (expectedScope == null)
                throw new ArgumentNullException(nameof(expectedScope));
            expectedScope.Normalize();

            EquipmentSlotStorageFormatProbe probe =
                EquipmentSlotStorageFormatClassifier.Probe(
                    sourcePath);
            bool preSchema =
                probe.Format ==
                    EquipmentSlotStorageFormat.PreSchemaGlobal;
            if (probe.Format !=
                    EquipmentSlotStorageFormat.LegacyFlat &&
                !preSchema)
            {
                failure =
                    "legacy-format-required:" +
                    probe.Format +
                    ":" +
                    probe.Failure;
                return false;
            }
            if (preSchema && !globalSource)
            {
                failure =
                    "pre-schema-storage-is-global-only";
                return false;
            }

            LegacyDocument? legacy;
            try
            {
                byte[] bytes = File.ReadAllBytes(sourcePath);
                sourceSha256 = ComputeSha256(bytes);
                using (var stream = new MemoryStream(bytes, writable: false))
                {
                    if (preSchema)
                    {
                        PreSchemaDocument? old =
                            CreateSerializer(
                                typeof(PreSchemaDocument))
                                .ReadObject(stream) as
                                PreSchemaDocument;
                        legacy =
                            ConvertPreSchemaDocument(old);
                    }
                    else
                    {
                        legacy =
                            CreateSerializer(typeof(LegacyDocument))
                                .ReadObject(stream) as
                                LegacyDocument;
                    }
                }
            }
            catch (Exception ex)
            {
                failure =
                    ex.GetType().Name + ": " + ex.Message;
                return false;
            }

            if (legacy == null ||
                (!preSchema &&
                 (legacy.SchemaVersion <
                    OldestSupportedSchema ||
                  legacy.SchemaVersion >
                    NewestSupportedSchema)))
            {
                failure = "legacy-schema-invalid";
                return false;
            }
            string ownerId =
                legacy.OwnerId?.Trim() ?? string.Empty;
            if (ownerId.Length == 0 ||
                !string.Equals(
                    ownerId,
                    MoreEquipmentSlotsProductContract.UniqueId,
                    StringComparison.OrdinalIgnoreCase))
            {
                failure = "legacy-owner-mismatch";
                return false;
            }
            if (legacy.Generation < 0 ||
                legacy.Generation == long.MaxValue)
            {
                failure = "legacy-generation-invalid";
                return false;
            }
            if (!preSchema &&
                !LegacyScopeMatches(
                    legacy,
                    expectedScope,
                    out string scopeFailure))
            {
                failure =
                    "legacy-scope-mismatch:" +
                    scopeFailure;
                return false;
            }
            if (!TryConvertSlots(
                legacy.Slots,
                out List<EquipmentSlotStorageEntry>? slots,
                out failure))
            {
                return false;
            }

            var converted =
                new EquipmentSlotStorageDocument
                {
                    Scope = expectedScope.Clone(),
                    Generation = legacy.Generation + 1,
                    Slots = slots!,
                    LegacyMigration =
                        new EquipmentSlotLegacyMigrationStamp
                        {
                            SourceKind =
                                preSchema
                                    ? EquipmentSlotLegacyMigrationSourceKinds
                                        .PreSchemaGlobal
                                    : globalSource
                                        ? EquipmentSlotLegacyMigrationSourceKinds
                                            .GlobalFlat
                                        : EquipmentSlotLegacyMigrationSourceKinds
                                            .ScopedFlat,
                            SourceSchema =
                                legacy.SchemaVersion,
                            SourceSha256 = sourceSha256
                        }
                };
            if (!preSchema)
            {
                converted.Scope.TotalGameSeconds =
                    legacy.SavedTotalGameSeconds;
            }
            if (legacy.Journal != null &&
                legacy.GameplayCandidate != null)
            {
                failure = "legacy-transaction-shape-conflict";
                return false;
            }
            EquipmentSlotTransactionJournal? journal = null;
            if (legacy.Journal != null &&
                !TryConvertJournal(
                    legacy.Journal,
                    converted,
                    out journal,
                    out failure))
            {
                return false;
            }
            else
            {
                converted.Journal = journal;
            }
            EquipmentSlotGameplayCandidate? candidate = null;
            if (legacy.GameplayCandidate != null &&
                !TryConvertCandidate(
                    legacy.GameplayCandidate,
                    converted,
                    out candidate,
                    out failure))
            {
                return false;
            }
            else
            {
                converted.GameplayCandidate = candidate;
            }

            document = converted;
            return true;
        }

        internal static string ComputeFileSha256(string path) =>
            ComputeSha256(File.ReadAllBytes(path));

        internal static string GetLegacyBackupPath(
            string targetPath,
            string sourceSha256)
        {
            string directory =
                Path.GetDirectoryName(targetPath) ??
                throw new InvalidOperationException(
                    "The Product sidecar path has no directory.");
            return Path.Combine(
                directory,
                ".legacy-migrations",
                sourceSha256 +
                ".flat.json");
        }

        internal static string GetGlobalArchivePath(
            string globalPath,
            string sourceSha256) =>
            globalPath +
            ".migrated-product-v3-" +
            sourceSha256;

        internal static string GetPreSchemaGlobalClaimPath(
            string globalPath,
            string sourceSha256)
        {
            return Path.Combine(
                GetPreSchemaGlobalClaimDirectory(globalPath),
                sourceSha256 + ".json");
        }

        internal static string GetPreSchemaGlobalWinnerPath(
            string globalPath) =>
            Path.Combine(
                GetPreSchemaGlobalClaimDirectory(globalPath),
                "winner.json");

        internal static string GetPreSchemaGlobalOperationLockPath(
            string globalPath) =>
            Path.Combine(
                GetPreSchemaGlobalClaimDirectory(globalPath),
                "operation.lock");

        internal static string GetPreSchemaGlobalClaimDirectory(
            string globalPath)
        {
            string directory =
                Path.GetDirectoryName(globalPath)
                ?? throw new InvalidOperationException(
                    "The pre-schema global path has no directory.");
            return Path.Combine(
                directory,
                ".equipment-slot-migration-claims",
                MoreEquipmentSlotsProductContract.UniqueId);
        }

        private static bool LegacyScopeMatches(
            LegacyDocument legacy,
            EquipmentSlotSaveScope expected,
            out string failure)
        {
            failure = string.Empty;
            string expectedStorageScope =
                "slot-" +
                expected.ArchiveIndex.ToString(
                    System.Globalization.CultureInfo.InvariantCulture);
            if (!string.Equals(
                    legacy.StorageScope?.Trim(),
                    expectedStorageScope,
                    StringComparison.OrdinalIgnoreCase))
            {
                failure = "storage-scope-mismatch";
                return false;
            }
            if (legacy.ArchiveIndex < 0 ||
                legacy.ArchiveIndex != expected.ArchiveIndex)
            {
                failure = "archive-index-mismatch";
                return false;
            }
            string documentName =
                FirstText(
                    legacy.CustomPlayerName,
                    legacy.PlayerName);
            string currentName =
                FirstText(
                    expected.CustomPlayerName,
                    expected.PlayerName);
            if (documentName.Length == 0 ||
                currentName.Length == 0 ||
                !string.Equals(
                    documentName,
                    currentName,
                    StringComparison.OrdinalIgnoreCase))
            {
                failure = "player-name-mismatch";
                return false;
            }
            if (!legacy.SavedTotalGameSeconds.HasValue ||
                legacy.SavedTotalGameSeconds.Value < 0 ||
                !expected.TotalGameSeconds.HasValue)
            {
                failure = "save-clock-missing";
                return false;
            }
            if (expected.TotalGameSeconds.Value +
                    EquipmentSlotSaveScope
                        .SaveClockRegressionToleranceSeconds <
                legacy.SavedTotalGameSeconds.Value)
            {
                failure = "save-clock-regressed";
                return false;
            }
            return true;
        }

        private static LegacyDocument? ConvertPreSchemaDocument(
            PreSchemaDocument? old)
        {
            if (old == null)
                return null;
            var slots = new List<LegacySlot>();
            if (old.Slots != null)
            {
                foreach (PreSchemaSlot slot in old.Slots)
                {
                    if (slot == null)
                        return null;
                    slots.Add(
                        new LegacySlot
                        {
                            Index = slot.Index,
                            ItemId = slot.ItemId,
                            DisplayName = slot.DisplayName,
                            SkillId = slot.SkillId
                        });
                }
            }
            return new LegacyDocument
            {
                SchemaVersion = 0,
                OwnerId = old.OwnerId,
                Generation = 0,
                Slots = slots
            };
        }

        private static string FirstText(
            string? preferred,
            string? fallback)
        {
            string first = preferred?.Trim() ?? string.Empty;
            return first.Length > 0
                ? first
                : fallback?.Trim() ?? string.Empty;
        }

        private static bool TryConvertSlots(
            List<LegacySlot>? legacySlots,
            out List<EquipmentSlotStorageEntry>? slots,
            out string failure)
        {
            slots =
                EquipmentSlotStorageDocument.CreateEmptySlots();
            failure = string.Empty;
            if (legacySlots == null)
            {
                failure = "legacy-slots-null";
                slots = null;
                return false;
            }

            var indexes = new HashSet<int>();
            foreach (LegacySlot legacy in legacySlots)
            {
                if (legacy == null ||
                    legacy.Index < 0 ||
                    !indexes.Add(legacy.Index))
                {
                    failure =
                        "legacy-slot-index-invalid-or-duplicate";
                    slots = null;
                    return false;
                }
                bool occupied =
                    !string.IsNullOrWhiteSpace(legacy.ItemId);
                if (legacy.Index >=
                    MoreEquipmentSlotsProductContract
                        .FixedSlotCount)
                {
                    if (occupied)
                    {
                        failure =
                            "legacy-occupied-slot-outside-product-range";
                        slots = null;
                        return false;
                    }
                    continue;
                }
                if (!occupied)
                    continue;
                if (legacy.DefenseBonus < 0 ||
                    legacy.ShieldValue < 0 ||
                    legacy.ShieldMaxValue < 0 ||
                    legacy.ShieldDefend < 0 ||
                    legacy.ShieldValue >
                        legacy.ShieldMaxValue ||
                    (legacy.IsShieldHat &&
                     legacy.ShieldMaxValue <= 0))
                {
                    failure = "legacy-slot-value-invalid";
                    slots = null;
                    return false;
                }

                slots[legacy.Index] =
                    new EquipmentSlotStorageEntry
                    {
                        Index = legacy.Index,
                        ItemId =
                            legacy.ItemId?.Trim() ??
                            string.Empty,
                        DisplayName =
                            legacy.DisplayName?.Trim() ??
                            string.Empty,
                        SkillId =
                            legacy.SkillId?.Trim() ??
                            string.Empty,
                        DefenseBonus = legacy.DefenseBonus,
                        IsShield = legacy.IsShieldHat,
                        ShieldValue =
                            legacy.IsShieldHat
                                ? legacy.ShieldValue
                                : 0,
                        ShieldMaxValue =
                            legacy.IsShieldHat
                                ? legacy.ShieldMaxValue
                                : 0,
                        ShieldDefend =
                            legacy.IsShieldHat
                                ? legacy.ShieldDefend
                                : 0
                    };
            }
            return true;
        }

        private static bool TryConvertJournal(
            LegacyJournal legacy,
            EquipmentSlotStorageDocument document,
            out EquipmentSlotTransactionJournal? journal,
            out string failure)
        {
            journal = null;
            failure = string.Empty;
            string transactionId =
                legacy.TransactionId?.Trim() ?? string.Empty;
            if (legacy.State < 0 ||
                legacy.State > 2 ||
                legacy.ArchiveIndex !=
                    document.Scope.ArchiveIndex ||
                legacy.SlotIndex < 0 ||
                legacy.SlotIndex >=
                    MoreEquipmentSlotsProductContract
                        .FixedSlotCount ||
                string.IsNullOrWhiteSpace(legacy.ItemId) ||
                !Guid.TryParseExact(
                    transactionId,
                    "N",
                    out _) ||
                (legacy.Origin !=
                    EquipmentSlotTransactionOrigin
                        .OwnerRecovery &&
                 legacy.Origin !=
                    EquipmentSlotTransactionOrigin
                        .OrphanRecovery))
            {
                failure = "legacy-journal-identity-invalid";
                return false;
            }
            bool attemptCompleted =
                legacy.AttemptStarted &&
                legacy.Placement !=
                    (int)NativePlacementKind.Failure;
            bool committed = legacy.State != 0;
            if (committed &&
                (!attemptCompleted ||
                 string.IsNullOrWhiteSpace(
                    legacy.PostSaveFingerprint)))
            {
                failure = "legacy-journal-commit-invalid";
                return false;
            }

            var escrow =
                new EquipmentSlotEscrowEntry
                {
                    SlotIndex = legacy.SlotIndex,
                    ItemId =
                        legacy.ItemId?.Trim() ?? string.Empty,
                    DisplayName =
                        legacy.DisplayName?.Trim() ??
                        string.Empty,
                    SkillId =
                        legacy.SkillId?.Trim() ??
                        string.Empty,
                    DefenseBonus = legacy.DefenseBonus,
                    IsShield = legacy.IsShieldHat,
                    ShieldValue =
                        legacy.IsShieldHat
                            ? legacy.ShieldValue
                            : 0,
                    ShieldMaxValue =
                        legacy.IsShieldHat
                            ? legacy.ShieldMaxValue
                            : 0,
                    ShieldDefend =
                        legacy.IsShieldHat
                            ? legacy.ShieldDefend
                            : 0,
                    AttemptCompleted = attemptCompleted,
                    Placement =
                        attemptCompleted
                            ? (NativePlacementKind)
                                legacy.Placement
                            : NativePlacementKind.Failure
                };
            if (legacy.AttemptStarted)
            {
                if (string.IsNullOrWhiteSpace(
                    legacy.PreSaveFingerprint) ||
                    legacy.BeforeBackpackCount < 0 ||
                    legacy.BeforeMailCount < 0)
                {
                    failure =
                        "legacy-journal-observation-invalid";
                    return false;
                }
                escrow.BeforeBackpackCount =
                    legacy.BeforeBackpackCount;
                escrow.BeforeMailCount =
                    legacy.BeforeMailCount;
                escrow.ExpectedBackpackCount =
                    legacy.BeforeBackpackCount +
                    (escrow.Placement ==
                        NativePlacementKind.Backpack
                        ? 1
                        : 0);
                escrow.ExpectedMailCount =
                    legacy.BeforeMailCount +
                    (escrow.Placement ==
                        NativePlacementKind.Mail
                        ? 1
                        : 0);
            }

            journal =
                new EquipmentSlotTransactionJournal
                {
                    TransactionId = transactionId,
                    State =
                        committed
                            ? EquipmentSlotJournalState
                                .CommittedTombstone
                            : EquipmentSlotJournalState.Prepared,
                    Scope = document.Scope.Clone(),
                    BaseGeneration =
                        Math.Min(
                            document.Generation,
                            Math.Max(0, document.Generation - 1)),
                    Escrow =
                        new List<EquipmentSlotEscrowEntry>
                        {
                            escrow
                        },
                    AttemptStarted = legacy.AttemptStarted,
                    PreSaveFingerprint =
                        legacy.AttemptStarted
                            ? legacy.PreSaveFingerprint
                                ?.Trim() ?? string.Empty
                            : string.Empty,
                    PostSaveFingerprint =
                        committed
                            ? legacy.PostSaveFingerprint
                                ?.Trim() ?? string.Empty
                            : string.Empty,
                    Origin = legacy.Origin
                };
            return true;
        }

        private static bool TryConvertCandidate(
            LegacyCandidate legacy,
            EquipmentSlotStorageDocument document,
            out EquipmentSlotGameplayCandidate? candidate,
            out string failure)
        {
            candidate = null;
            failure = string.Empty;
            string transactionId =
                legacy.TransactionId?.Trim() ?? string.Empty;
            if (legacy.State < 0 ||
                legacy.State > 1 ||
                legacy.Origin !=
                    EquipmentSlotTransactionOrigin
                        .GameplayMutation ||
                legacy.Scope == null ||
                legacy.Scope.ArchiveIndex !=
                    document.Scope.ArchiveIndex ||
                !TextMatchesIfPresent(
                    legacy.Scope.PlayerName,
                    document.Scope.PlayerName) ||
                !TextMatchesIfPresent(
                    legacy.Scope.CustomPlayerName,
                    document.Scope.CustomPlayerName) ||
                !Guid.TryParseExact(
                    transactionId,
                    "N",
                    out _) ||
                legacy.BaseGeneration < 0 ||
                legacy.BaseGeneration >
                    document.Generation ||
                string.IsNullOrWhiteSpace(
                    legacy.PreSaveFingerprint))
            {
                failure =
                    "legacy-gameplay-candidate-identity-invalid";
                return false;
            }
            if (!TryConvertSlots(
                legacy.WorkingSlots,
                out List<EquipmentSlotStorageEntry>? working,
                out failure))
            {
                failure =
                    "legacy-gameplay-candidate-" + failure;
                return false;
            }

            var expectations =
                new List<EquipmentSlotNativeExpectation>();
            var ids = new HashSet<string>(
                StringComparer.Ordinal);
            if (legacy.NativeExpectations == null)
            {
                failure =
                    "legacy-gameplay-candidate-expectations-null";
                return false;
            }
            foreach (LegacyExpectation item in
                legacy.NativeExpectations)
            {
                string id = item?.ItemId?.Trim() ?? string.Empty;
                if (id.Length == 0 ||
                    item!.BackpackCount < 0 ||
                    item.MailCount < 0 ||
                    !ids.Add(id))
                {
                    failure =
                        "legacy-gameplay-candidate-expectation-invalid";
                    return false;
                }
                expectations.Add(
                    new EquipmentSlotNativeExpectation
                    {
                        ItemId = id,
                        BackpackCount = item.BackpackCount,
                        MailCount = item.MailCount
                    });
            }

            candidate =
                new EquipmentSlotGameplayCandidate
                {
                    TransactionId = transactionId,
                    State =
                        (EquipmentSlotGameplayCandidateState)
                            legacy.State,
                    Origin =
                        EquipmentSlotTransactionOrigin
                            .GameplayMutation,
                    Scope = document.Scope.Clone(),
                    BaseGeneration =
                        legacy.BaseGeneration,
                    PreSaveFingerprint =
                        legacy.PreSaveFingerprint
                            ?.Trim() ?? string.Empty,
                    PostSaveFingerprint =
                        legacy.PostSaveFingerprint
                            ?.Trim() ?? string.Empty,
                    WorkingSlots = working!,
                    NativeExpectations = expectations
                };
            return true;
        }

        private static bool TextMatchesIfPresent(
            string? legacy,
            string expected)
        {
            string value = legacy?.Trim() ?? string.Empty;
            return value.Length == 0 ||
                string.Equals(
                    value,
                    expected,
                    StringComparison.Ordinal);
        }

        private static string ComputeSha256(byte[] bytes)
        {
            using (SHA256 sha = SHA256.Create())
                return ToHex(sha.ComputeHash(bytes));
        }

        private static string ToHex(byte[] bytes)
        {
            var chars = new char[bytes.Length * 2];
            const string alphabet = "0123456789ABCDEF";
            for (int index = 0; index < bytes.Length; index++)
            {
                chars[index * 2] =
                    alphabet[bytes[index] >> 4];
                chars[(index * 2) + 1] =
                    alphabet[bytes[index] & 0x0F];
            }
            return new string(chars);
        }

        private static DataContractJsonSerializer CreateSerializer(
            Type type) =>
            new DataContractJsonSerializer(
                type,
                new DataContractJsonSerializerSettings
                {
                    UseSimpleDictionaryFormat = true
                });

        [DataContract]
        private sealed class LegacyDocument
        {
            [DataMember(Name = "schemaVersion")]
            public int SchemaVersion { get; set; }

            [DataMember(Name = "ownerId")]
            public string OwnerId { get; set; } = string.Empty;

            [DataMember(Name = "storageScope")]
            public string StorageScope { get; set; } = string.Empty;

            [DataMember(Name = "archiveIndex")]
            public int ArchiveIndex { get; set; } = -1;

            [DataMember(Name = "playerName")]
            public string PlayerName { get; set; } = string.Empty;

            [DataMember(Name = "customPlayerName")]
            public string CustomPlayerName { get; set; } =
                string.Empty;

            [DataMember(Name = "generation")]
            public long Generation { get; set; }

            [DataMember(Name = "savedTotalGameSeconds")]
            public long? SavedTotalGameSeconds { get; set; }

            [DataMember(Name = "slots")]
            public List<LegacySlot> Slots { get; set; } =
                new List<LegacySlot>();

            [DataMember(
                Name = "journal",
                EmitDefaultValue = false)]
            public LegacyJournal? Journal { get; set; }

            [DataMember(
                Name = "gameplayCandidate",
                EmitDefaultValue = false)]
            public LegacyCandidate? GameplayCandidate { get; set; }
        }

        [DataContract]
        private sealed class PreSchemaDocument
        {
            [DataMember(Name = "ownerId")]
            public string OwnerId { get; set; } = string.Empty;

            [DataMember(Name = "savedAt")]
            public string SavedAt { get; set; } = string.Empty;

            [DataMember(Name = "slots")]
            public List<PreSchemaSlot> Slots { get; set; } =
                new List<PreSchemaSlot>();
        }

        [DataContract]
        private sealed class PreSchemaSlot
        {
            [DataMember(Name = "index")]
            public int Index { get; set; }

            [DataMember(Name = "slotId")]
            public string SlotId { get; set; } = string.Empty;

            [DataMember(Name = "itemId")]
            public string ItemId { get; set; } = string.Empty;

            [DataMember(Name = "displayName")]
            public string DisplayName { get; set; } = string.Empty;

            [DataMember(Name = "skillId")]
            public string SkillId { get; set; } = string.Empty;

            [DataMember(Name = "lastMessage")]
            public string LastMessage { get; set; } = string.Empty;
        }

        [DataContract]
        private sealed class LegacySlot
        {
            [DataMember(Name = "index")]
            public int Index { get; set; }

            [DataMember(Name = "itemId")]
            public string ItemId { get; set; } = string.Empty;

            [DataMember(Name = "displayName")]
            public string DisplayName { get; set; } = string.Empty;

            [DataMember(Name = "skillId")]
            public string SkillId { get; set; } = string.Empty;

            [DataMember(Name = "defenseBonus")]
            public int DefenseBonus { get; set; }

            [DataMember(Name = "isShieldHat")]
            public bool IsShieldHat { get; set; }

            [DataMember(Name = "shieldMaxValue")]
            public int ShieldMaxValue { get; set; }

            [DataMember(Name = "shieldValue")]
            public int ShieldValue { get; set; }

            [DataMember(Name = "shieldDefend")]
            public int ShieldDefend { get; set; }
        }

        [DataContract]
        private sealed class LegacyJournal
        {
            [DataMember(Name = "transactionId")]
            public string TransactionId { get; set; } = string.Empty;

            [DataMember(Name = "state")]
            public int State { get; set; }

            [DataMember(Name = "archiveIndex")]
            public int ArchiveIndex { get; set; } = -1;

            [DataMember(Name = "slotIndex")]
            public int SlotIndex { get; set; } = -1;

            [DataMember(Name = "itemId")]
            public string ItemId { get; set; } = string.Empty;

            [DataMember(Name = "displayName")]
            public string DisplayName { get; set; } = string.Empty;

            [DataMember(Name = "skillId")]
            public string SkillId { get; set; } = string.Empty;

            [DataMember(Name = "defenseBonus")]
            public int DefenseBonus { get; set; }

            [DataMember(Name = "isShieldHat")]
            public bool IsShieldHat { get; set; }

            [DataMember(Name = "shieldMaxValue")]
            public int ShieldMaxValue { get; set; }

            [DataMember(Name = "shieldValue")]
            public int ShieldValue { get; set; }

            [DataMember(Name = "shieldDefend")]
            public int ShieldDefend { get; set; }

            [DataMember(Name = "attemptStarted")]
            public bool AttemptStarted { get; set; }

            [DataMember(Name = "preSaveFingerprint")]
            public string PreSaveFingerprint { get; set; } =
                string.Empty;

            [DataMember(Name = "postSaveFingerprint")]
            public string PostSaveFingerprint { get; set; } =
                string.Empty;

            [DataMember(Name = "beforeBackpackCount")]
            public int BeforeBackpackCount { get; set; }

            [DataMember(Name = "beforeMailCount")]
            public int BeforeMailCount { get; set; }

            [DataMember(Name = "placement")]
            public int Placement { get; set; }

            [DataMember(Name = "origin")]
            public EquipmentSlotTransactionOrigin Origin { get; set; }
        }

        [DataContract]
        private sealed class LegacyCandidate
        {
            [DataMember(Name = "transactionId")]
            public string TransactionId { get; set; } = string.Empty;

            [DataMember(Name = "phase")]
            public int State { get; set; }

            [DataMember(Name = "origin")]
            public EquipmentSlotTransactionOrigin Origin { get; set; }

            [DataMember(Name = "scope")]
            public LegacyScope Scope { get; set; } =
                new LegacyScope();

            [DataMember(Name = "baseGeneration")]
            public long BaseGeneration { get; set; }

            [DataMember(Name = "preSaveFingerprint")]
            public string PreSaveFingerprint { get; set; } =
                string.Empty;

            [DataMember(Name = "postSaveFingerprint")]
            public string PostSaveFingerprint { get; set; } =
                string.Empty;

            [DataMember(Name = "workingSlots")]
            public List<LegacySlot> WorkingSlots { get; set; } =
                new List<LegacySlot>();

            [DataMember(Name = "nativeExpectations")]
            public List<LegacyExpectation> NativeExpectations
            {
                get;
                set;
            } = new List<LegacyExpectation>();
        }

        [DataContract]
        private sealed class LegacyScope
        {
            [DataMember(Name = "archiveIndex")]
            public int ArchiveIndex { get; set; } = -1;

            [DataMember(Name = "playerName")]
            public string PlayerName { get; set; } = string.Empty;

            [DataMember(Name = "customPlayerName")]
            public string CustomPlayerName { get; set; } =
                string.Empty;
        }

        [DataContract]
        private sealed class LegacyExpectation
        {
            [DataMember(Name = "itemId")]
            public string ItemId { get; set; } = string.Empty;

            [DataMember(Name = "backpackCount")]
            public int BackpackCount { get; set; }

            [DataMember(Name = "mailCount")]
            public int MailCount { get; set; }
        }
    }
}
