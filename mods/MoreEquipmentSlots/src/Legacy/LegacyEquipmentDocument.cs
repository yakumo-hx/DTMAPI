using System.Collections.Generic;
using System.Runtime.Serialization;

namespace DTMAPI.MoreEquipmentSlots
{
    // 这些类型只描述 1.0.1 的文件。2.0 不创建或更新旧事务记录。
    internal enum LegacyEquipmentPhase { Prepared, CommittedTombstone }
    internal enum LegacyEquipmentPlacement { Failure, Backpack, Mail }

    [DataContract]
    internal sealed class LegacyEquipmentDocument
    {
        [DataMember(Name = "schemaVersion")] public int SchemaVersion { get; set; } = 3;
        [DataMember(Name = "scope")] public LegacyEquipmentScope Scope { get; set; } = new LegacyEquipmentScope();
        [DataMember(Name = "slots")] public List<LegacyEquipmentSlot> Slots { get; set; } = CreateEmptySlots();
        [DataMember(Name = "journal", EmitDefaultValue = false)] public LegacyEquipmentJournal? Journal { get; set; }
        [DataMember(Name = "gameplayCandidate", EmitDefaultValue = false)] public LegacyEquipmentCandidate? GameplayCandidate { get; set; }

        internal static List<LegacyEquipmentSlot> CreateEmptySlots() => new List<LegacyEquipmentSlot>
        {
            new LegacyEquipmentSlot { Index = 0 }, new LegacyEquipmentSlot { Index = 1 }, new LegacyEquipmentSlot { Index = 2 }
        };

        internal void Normalize()
        {
            Scope ??= new LegacyEquipmentScope();
            Scope.PlayerName = Scope.PlayerName?.Trim() ?? string.Empty;
            Slots ??= CreateEmptySlots();
            foreach (LegacyEquipmentSlot? slot in Slots) slot?.Normalize();
            if (Journal != null)
            {
                Journal.Escrow ??= new List<LegacyEquipmentEscrow>();
                Journal.Replacements ??= new List<LegacyEquipmentSlot>();
                foreach (LegacyEquipmentEscrow? entry in Journal.Escrow) entry?.Normalize();
                foreach (LegacyEquipmentSlot? slot in Journal.Replacements) slot?.Normalize();
            }
            if (GameplayCandidate != null)
            {
                GameplayCandidate.WorkingSlots ??= CreateEmptySlots();
                foreach (LegacyEquipmentSlot? slot in GameplayCandidate.WorkingSlots) slot?.Normalize();
            }
        }
    }

    [DataContract]
    internal sealed class LegacyEquipmentScope
    {
        [DataMember(Name = "playerName")] public string PlayerName { get; set; } = string.Empty;
    }

    [DataContract]
    internal sealed class LegacyEquipmentSlot
    {
        [DataMember(Name = "index")] public int Index { get; set; }
        [DataMember(Name = "itemId")] public string ItemId { get; set; } = string.Empty;
        [DataMember(Name = "displayName")] public string DisplayName { get; set; } = string.Empty;
        [DataMember(Name = "skillId")] public string SkillId { get; set; } = string.Empty;
        [DataMember(Name = "defenseBonus")] public int DefenseBonus { get; set; }
        [DataMember(Name = "isShield")] public bool IsShield { get; set; }
        [DataMember(Name = "shieldValue")] public int ShieldValue { get; set; }
        [DataMember(Name = "shieldMaxValue")] public int ShieldMaxValue { get; set; }
        [DataMember(Name = "shieldDefend")] public int ShieldDefend { get; set; }
        internal bool IsOccupied => !string.IsNullOrWhiteSpace(ItemId);
        internal LegacyEquipmentSlot Clone() => (LegacyEquipmentSlot)MemberwiseClone();
        internal void Normalize()
        {
            ItemId = ItemId?.Trim() ?? string.Empty;
            DisplayName = DisplayName?.Trim() ?? string.Empty;
            SkillId = SkillId?.Trim() ?? string.Empty;
        }
    }

    [DataContract]
    internal sealed class LegacyEquipmentJournal
    {
        [DataMember(Name = "phase")] public LegacyEquipmentPhase State { get; set; }
        [DataMember(Name = "escrow")] public List<LegacyEquipmentEscrow> Escrow { get; set; } = new List<LegacyEquipmentEscrow>();
        [DataMember(Name = "replacements")] public List<LegacyEquipmentSlot> Replacements { get; set; } = new List<LegacyEquipmentSlot>();
        [DataMember(Name = "incomingSucceeded")] public bool IncomingSucceeded { get; set; }
    }

    [DataContract]
    internal sealed class LegacyEquipmentCandidate
    {
        [DataMember(Name = "phase")] public LegacyEquipmentPhase State { get; set; }
        [DataMember(Name = "workingSlots")] public List<LegacyEquipmentSlot> WorkingSlots { get; set; } = LegacyEquipmentDocument.CreateEmptySlots();
    }

    [DataContract]
    internal sealed class LegacyEquipmentEscrow
    {
        [DataMember(Name = "slotIndex")] public int SlotIndex { get; set; }
        [DataMember(Name = "itemId")] public string ItemId { get; set; } = string.Empty;
        [DataMember(Name = "displayName")] public string DisplayName { get; set; } = string.Empty;
        [DataMember(Name = "placement")] public LegacyEquipmentPlacement Placement { get; set; }
        [DataMember(Name = "skillId")] public string SkillId { get; set; } = string.Empty;
        [DataMember(Name = "defenseBonus")] public int DefenseBonus { get; set; }
        [DataMember(Name = "isShield")] public bool IsShield { get; set; }
        [DataMember(Name = "shieldValue")] public int ShieldValue { get; set; }
        [DataMember(Name = "shieldMaxValue")] public int ShieldMaxValue { get; set; }
        [DataMember(Name = "shieldDefend")] public int ShieldDefend { get; set; }
        internal LegacyEquipmentSlot ToSlot() => new LegacyEquipmentSlot
        {
            Index = SlotIndex, ItemId = ItemId, DisplayName = DisplayName, SkillId = SkillId,
            DefenseBonus = DefenseBonus, IsShield = IsShield, ShieldValue = ShieldValue,
            ShieldMaxValue = ShieldMaxValue, ShieldDefend = ShieldDefend
        };
        internal void Normalize()
        {
            ItemId = ItemId?.Trim() ?? string.Empty;
            DisplayName = DisplayName?.Trim() ?? string.Empty;
            SkillId = SkillId?.Trim() ?? string.Empty;
        }
    }
}
