using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace DTMAPI.MoreEquipmentSlots
{
    internal static class MoreEquipmentSlotsProductContract
    {
        internal const int FixedSlotCount = 3;
        internal const int ExpectedHookCount = 4;
        internal const int StorageSchemaVersion = 3;
        internal const string UniqueId = "DTMAPI.MoreEquipmentSlotsMod";
        internal const string HarmonyOwner =
            "dtmapi.mod.dtmapi.moreequipmentslotsmod";
        internal const string CompatibilityOwner =
            "dtmapi.gamebridge.doloctown.equipmentslots.compatibility";
    }

    internal enum NativePlacementKind
    {
        Failure = 0,
        Backpack = 1,
        Mail = 2
    }

    internal enum EquipmentSlotJournalState
    {
        Prepared = 0,
        CommittedTombstone = 1
    }

    internal enum EquipmentSlotTransactionOrigin
    {
        Unknown = 0,
        GameplayMutation = 1,
        OwnerRecovery = 2,
        OrphanRecovery = 3
    }

    internal enum EquipmentSlotGameplayCandidateState
    {
        Prepared = 0,
        CommittedTombstone = 1
    }

    internal enum EquipmentSlotRecoveryAction
    {
        RetryPlacement = 0,
        FinalizeCommitted = 1,
        FailClosed = 2
    }

    internal enum EquipmentSlotGameplayRecoveryAction
    {
        DiscardUncommitted = 0,
        PromoteCommitted = 1,
        FinalizeCommitted = 2,
        FailClosed = 3
    }

    internal readonly struct NativePlacementResult
    {
        internal NativePlacementResult(
            NativePlacementKind kind,
            int backpackDelta,
            int mailDelta,
            string message,
            int afterBackpackCount = -1,
            int afterMailCount = -1)
        {
            Kind = kind;
            BackpackDelta = backpackDelta;
            MailDelta = mailDelta;
            Message = message ?? string.Empty;
            AfterBackpackCount = afterBackpackCount;
            AfterMailCount = afterMailCount;
        }

        internal NativePlacementKind Kind { get; }

        internal int BackpackDelta { get; }

        internal int MailDelta { get; }

        internal string Message { get; }

        internal int AfterBackpackCount { get; }

        internal int AfterMailCount { get; }

        internal bool HasExactAfterCounts =>
            AfterBackpackCount >= 0 &&
            AfterMailCount >= 0;

        internal bool Success => Kind != NativePlacementKind.Failure;
    }

    internal readonly struct NativeRecoveryObservation
    {
        internal NativeRecoveryObservation(
            string saveFingerprint,
            int backpackCount,
            int mailCount)
        {
            SaveFingerprint = saveFingerprint ?? string.Empty;
            BackpackCount = Math.Max(0, backpackCount);
            MailCount = Math.Max(0, mailCount);
        }

        internal string SaveFingerprint { get; }

        internal int BackpackCount { get; }

        internal int MailCount { get; }
    }

    [DataContract]
    internal sealed class EquipmentSlotSaveScope
    {
        internal const long SaveClockRegressionToleranceSeconds =
            300;

        [DataMember(Name = "archiveIndex", Order = 1)]
        public int ArchiveIndex { get; set; } = -1;

        [DataMember(Name = "playerName", Order = 2)]
        public string PlayerName { get; set; } = string.Empty;

        [DataMember(Name = "customPlayerName", Order = 3)]
        public string CustomPlayerName { get; set; } = string.Empty;

        [DataMember(
            Name = "totalGameSeconds",
            Order = 4,
            EmitDefaultValue = false)]
        public long? TotalGameSeconds { get; set; }

        internal void Normalize()
        {
            PlayerName = PlayerName?.Trim() ?? string.Empty;
            CustomPlayerName =
                CustomPlayerName?.Trim() ?? string.Empty;
        }

        internal bool Matches(EquipmentSlotSaveScope? other)
        {
            if (other == null)
                return false;
            Normalize();
            other.Normalize();
            return ArchiveIndex == other.ArchiveIndex &&
                string.Equals(
                    PlayerName,
                    other.PlayerName,
                    StringComparison.Ordinal) &&
                string.Equals(
                    CustomPlayerName,
                    other.CustomPlayerName,
                    StringComparison.Ordinal) &&
                TotalGameSeconds == other.TotalGameSeconds;
        }

        internal EquipmentSlotSaveScope Clone() =>
            new EquipmentSlotSaveScope
            {
                ArchiveIndex = ArchiveIndex,
                PlayerName = PlayerName,
                CustomPlayerName = CustomPlayerName,
                TotalGameSeconds = TotalGameSeconds
            };
    }

    internal readonly struct EquipmentSlotRecoveryDecision
    {
        internal EquipmentSlotRecoveryDecision(
            EquipmentSlotRecoveryAction action,
            string reason)
        {
            Action = action;
            Reason = reason ?? string.Empty;
        }

        internal EquipmentSlotRecoveryAction Action { get; }

        internal string Reason { get; }
    }

    internal readonly struct EquipmentSlotGameplayRecoveryDecision
    {
        internal EquipmentSlotGameplayRecoveryDecision(
            EquipmentSlotGameplayRecoveryAction action,
            string reason)
        {
            Action = action;
            Reason = reason ?? string.Empty;
        }

        internal EquipmentSlotGameplayRecoveryAction Action
        {
            get;
        }

        internal string Reason { get; }
    }

    [DataContract]
    internal sealed class EquipmentSlotStorageDocument
    {
        internal EquipmentSlotStorageDocument()
        {
            Slots = CreateEmptySlots();
        }

        [DataMember(Name = "schemaVersion", Order = 1)]
        public int SchemaVersion { get; set; } =
            MoreEquipmentSlotsProductContract.StorageSchemaVersion;

        [DataMember(Name = "scope", Order = 2)]
        public EquipmentSlotSaveScope Scope { get; set; } =
            new EquipmentSlotSaveScope();

        [DataMember(Name = "generation", Order = 3)]
        public long Generation { get; set; }

        [DataMember(Name = "slots", Order = 4)]
        public List<EquipmentSlotStorageEntry> Slots { get; set; }

        [DataMember(
            Name = "journal",
            Order = 5,
            EmitDefaultValue = false)]
        public EquipmentSlotTransactionJournal? Journal { get; set; }

        [DataMember(
            Name = "gameplayCandidate",
            Order = 6,
            EmitDefaultValue = false)]
        public EquipmentSlotGameplayCandidate? GameplayCandidate
        {
            get;
            set;
        }

        [DataMember(
            Name = "legacyMigration",
            Order = 7,
            EmitDefaultValue = false)]
        public EquipmentSlotLegacyMigrationStamp? LegacyMigration
        {
            get;
            set;
        }

        internal static List<EquipmentSlotStorageEntry> CreateEmptySlots()
        {
            var slots = new List<EquipmentSlotStorageEntry>(
                MoreEquipmentSlotsProductContract.FixedSlotCount);
            for (int index = 0;
                 index < MoreEquipmentSlotsProductContract.FixedSlotCount;
                 index++)
            {
                slots.Add(
                    new EquipmentSlotStorageEntry
                    {
                        Index = index
                    });
            }

            return slots;
        }

        internal void Normalize()
        {
            SchemaVersion =
                MoreEquipmentSlotsProductContract.StorageSchemaVersion;
            Scope ??= new EquipmentSlotSaveScope();
            Scope.Normalize();
            Generation = Math.Max(0, Generation);
            var normalized = CreateEmptySlots();
            if (Slots != null)
            {
                foreach (EquipmentSlotStorageEntry entry in Slots)
                {
                    if (entry == null ||
                        entry.Index < 0 ||
                        entry.Index >= normalized.Count)
                    {
                        continue;
                    }

                    normalized[entry.Index] = entry;
                    entry.ItemId = entry.ItemId?.Trim() ?? string.Empty;
                    entry.DisplayName =
                        entry.DisplayName?.Trim() ?? string.Empty;
                    entry.SkillId = entry.SkillId?.Trim() ?? string.Empty;
                }
            }

            Slots = normalized;
            Journal?.Normalize();
            GameplayCandidate?.Normalize();
            LegacyMigration?.Normalize();
        }
    }

    [DataContract]
    internal sealed class EquipmentSlotLegacyMigrationStamp
    {
        [DataMember(Name = "sourceKind", Order = 1)]
        public string SourceKind { get; set; } = string.Empty;

        [DataMember(Name = "sourceSchema", Order = 2)]
        public int SourceSchema { get; set; }

        [DataMember(Name = "sourceSha256", Order = 3)]
        public string SourceSha256 { get; set; } = string.Empty;

        internal void Normalize()
        {
            SourceKind = SourceKind?.Trim() ?? string.Empty;
            SourceSha256 =
                SourceSha256?.Trim().ToUpperInvariant() ??
                string.Empty;
        }
    }

    [DataContract]
    internal sealed class EquipmentSlotStorageEntry
    {
        [DataMember(Name = "index", Order = 1)]
        public int Index { get; set; }

        [DataMember(Name = "itemId", Order = 2)]
        public string ItemId { get; set; } = string.Empty;

        [DataMember(Name = "displayName", Order = 3)]
        public string DisplayName { get; set; } = string.Empty;

        [DataMember(Name = "skillId", Order = 4)]
        public string SkillId { get; set; } = string.Empty;

        [DataMember(Name = "defenseBonus", Order = 5)]
        public int DefenseBonus { get; set; }

        [DataMember(Name = "isShield", Order = 6)]
        public bool IsShield { get; set; }

        [DataMember(Name = "shieldValue", Order = 7)]
        public int ShieldValue { get; set; }

        [DataMember(Name = "shieldMaxValue", Order = 8)]
        public int ShieldMaxValue { get; set; }

        [DataMember(Name = "shieldDefend", Order = 9)]
        public int ShieldDefend { get; set; }

        internal bool IsOccupied =>
            !string.IsNullOrWhiteSpace(ItemId);

        internal void Clear()
        {
            ItemId = string.Empty;
            DisplayName = string.Empty;
            SkillId = string.Empty;
            DefenseBonus = 0;
            IsShield = false;
            ShieldValue = 0;
            ShieldMaxValue = 0;
            ShieldDefend = 0;
        }
    }

    [DataContract]
    internal sealed class EquipmentSlotTransactionJournal
    {
        [DataMember(Name = "transactionId", Order = 1)]
        public string TransactionId { get; set; } = string.Empty;

        [DataMember(Name = "phase", Order = 2)]
        public EquipmentSlotJournalState State { get; set; }

        [DataMember(Name = "scope", Order = 3)]
        public EquipmentSlotSaveScope Scope { get; set; } =
            new EquipmentSlotSaveScope();

        [DataMember(Name = "baseGeneration", Order = 4)]
        public long BaseGeneration { get; set; }

        [DataMember(Name = "escrow", Order = 5)]
        public List<EquipmentSlotEscrowEntry> Escrow { get; set; } =
            new List<EquipmentSlotEscrowEntry>();

        [DataMember(Name = "replacements", Order = 6)]
        public List<EquipmentSlotStorageEntry> Replacements { get; set; } =
            new List<EquipmentSlotStorageEntry>();

        [DataMember(Name = "attemptStarted", Order = 7)]
        public bool AttemptStarted { get; set; }

        [DataMember(Name = "preSaveFingerprint", Order = 8)]
        public string PreSaveFingerprint { get; set; } = string.Empty;

        [DataMember(Name = "postSaveFingerprint", Order = 9)]
        public string PostSaveFingerprint { get; set; } = string.Empty;

        [DataMember(Name = "incomingAttemptCompleted", Order = 10)]
        public bool IncomingAttemptCompleted { get; set; }

        [DataMember(Name = "incomingSucceeded", Order = 11)]
        public bool IncomingSucceeded { get; set; }

        [DataMember(Name = "incomingBeforeBackpackCount", Order = 12)]
        public int IncomingBeforeBackpackCount { get; set; }

        [DataMember(Name = "incomingExpectedBackpackCount", Order = 13)]
        public int IncomingExpectedBackpackCount { get; set; }

        [DataMember(Name = "incomingFromNativeBuffer", Order = 14)]
        public bool IncomingFromNativeBuffer { get; set; }

        [DataMember(Name = "origin", Order = 15)]
        public EquipmentSlotTransactionOrigin Origin { get; set; }

        internal void Normalize()
        {
            TransactionId = TransactionId?.Trim() ?? string.Empty;
            Scope ??= new EquipmentSlotSaveScope();
            Scope.Normalize();
            BaseGeneration = Math.Max(0, BaseGeneration);
            Escrow ??= new List<EquipmentSlotEscrowEntry>();
            foreach (EquipmentSlotEscrowEntry item in Escrow)
                item?.Normalize();
            Replacements ??= new List<EquipmentSlotStorageEntry>();
            foreach (EquipmentSlotStorageEntry item in Replacements)
            {
                if (item == null)
                    continue;
                item.ItemId = item.ItemId?.Trim() ?? string.Empty;
                item.DisplayName =
                    item.DisplayName?.Trim() ?? string.Empty;
                item.SkillId = item.SkillId?.Trim() ?? string.Empty;
            }
            PreSaveFingerprint =
                PreSaveFingerprint?.Trim() ?? string.Empty;
            PostSaveFingerprint =
                PostSaveFingerprint?.Trim() ?? string.Empty;
            IncomingBeforeBackpackCount =
                Math.Max(0, IncomingBeforeBackpackCount);
            IncomingExpectedBackpackCount =
                Math.Max(0, IncomingExpectedBackpackCount);
        }
    }

    [DataContract]
    internal sealed class EquipmentSlotGameplayCandidate
    {
        [DataMember(Name = "transactionId", Order = 1)]
        public string TransactionId { get; set; } = string.Empty;

        [DataMember(Name = "phase", Order = 2)]
        public EquipmentSlotGameplayCandidateState State { get; set; }

        [DataMember(Name = "origin", Order = 3)]
        public EquipmentSlotTransactionOrigin Origin { get; set; } =
            EquipmentSlotTransactionOrigin.GameplayMutation;

        [DataMember(Name = "scope", Order = 4)]
        public EquipmentSlotSaveScope Scope { get; set; } =
            new EquipmentSlotSaveScope();

        [DataMember(Name = "baseGeneration", Order = 5)]
        public long BaseGeneration { get; set; }

        [DataMember(Name = "preSaveFingerprint", Order = 6)]
        public string PreSaveFingerprint { get; set; } = string.Empty;

        [DataMember(Name = "postSaveFingerprint", Order = 7)]
        public string PostSaveFingerprint { get; set; } = string.Empty;

        [DataMember(Name = "workingSlots", Order = 8)]
        public List<EquipmentSlotStorageEntry> WorkingSlots { get; set; } =
            EquipmentSlotStorageDocument.CreateEmptySlots();

        [DataMember(Name = "nativeExpectations", Order = 9)]
        public List<EquipmentSlotNativeExpectation> NativeExpectations
        {
            get;
            set;
        } = new List<EquipmentSlotNativeExpectation>();

        internal void Normalize()
        {
            TransactionId = TransactionId?.Trim() ?? string.Empty;
            Scope ??= new EquipmentSlotSaveScope();
            Scope.Normalize();
            BaseGeneration = Math.Max(0, BaseGeneration);
            PreSaveFingerprint =
                PreSaveFingerprint?.Trim() ?? string.Empty;
            PostSaveFingerprint =
                PostSaveFingerprint?.Trim() ?? string.Empty;
            WorkingSlots ??=
                EquipmentSlotStorageDocument.CreateEmptySlots();
            NativeExpectations ??=
                new List<EquipmentSlotNativeExpectation>();
            foreach (EquipmentSlotNativeExpectation expectation in
                NativeExpectations)
            {
                expectation?.Normalize();
            }
        }
    }

    [DataContract]
    internal sealed class EquipmentSlotNativeExpectation
    {
        [DataMember(Name = "itemId", Order = 1)]
        public string ItemId { get; set; } = string.Empty;

        [DataMember(Name = "backpackCount", Order = 2)]
        public int BackpackCount { get; set; }

        [DataMember(Name = "mailCount", Order = 3)]
        public int MailCount { get; set; }

        internal void Normalize()
        {
            ItemId = ItemId?.Trim() ?? string.Empty;
            BackpackCount = Math.Max(0, BackpackCount);
            MailCount = Math.Max(0, MailCount);
        }
    }

    [DataContract]
    internal sealed class EquipmentSlotEscrowEntry
    {
        [DataMember(Name = "slotIndex", Order = 1)]
        public int SlotIndex { get; set; } = -1;

        [DataMember(Name = "itemId", Order = 2)]
        public string ItemId { get; set; } = string.Empty;

        [DataMember(Name = "displayName", Order = 3)]
        public string DisplayName { get; set; } = string.Empty;

        [DataMember(Name = "beforeBackpackCount", Order = 4)]
        public int BeforeBackpackCount { get; set; }

        [DataMember(Name = "beforeMailCount", Order = 5)]
        public int BeforeMailCount { get; set; }

        [DataMember(Name = "expectedBackpackCount", Order = 6)]
        public int ExpectedBackpackCount { get; set; }

        [DataMember(Name = "expectedMailCount", Order = 7)]
        public int ExpectedMailCount { get; set; }

        [DataMember(Name = "placement", Order = 8)]
        public NativePlacementKind Placement { get; set; }

        [DataMember(Name = "attemptCompleted", Order = 9)]
        public bool AttemptCompleted { get; set; }

        [DataMember(Name = "skillId", Order = 10)]
        public string SkillId { get; set; } = string.Empty;

        [DataMember(Name = "defenseBonus", Order = 11)]
        public int DefenseBonus { get; set; }

        [DataMember(Name = "isShield", Order = 12)]
        public bool IsShield { get; set; }

        [DataMember(Name = "shieldValue", Order = 13)]
        public int ShieldValue { get; set; }

        [DataMember(Name = "shieldMaxValue", Order = 14)]
        public int ShieldMaxValue { get; set; }

        [DataMember(Name = "shieldDefend", Order = 15)]
        public int ShieldDefend { get; set; }

        internal void Normalize()
        {
            ItemId = ItemId?.Trim() ?? string.Empty;
            DisplayName = DisplayName?.Trim() ?? string.Empty;
            SkillId = SkillId?.Trim() ?? string.Empty;
            BeforeBackpackCount = Math.Max(0, BeforeBackpackCount);
            BeforeMailCount = Math.Max(0, BeforeMailCount);
            ExpectedBackpackCount =
                Math.Max(0, ExpectedBackpackCount);
            ExpectedMailCount = Math.Max(0, ExpectedMailCount);
        }
    }
}
