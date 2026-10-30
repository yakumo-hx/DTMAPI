using System;
using System.Runtime.Serialization;

namespace DTMAPI.MoreEquipmentSlots
{
    // A value snapshot, never the live native object. The five supported native
    // item classes own exactly these mutable equipment fields in build 25163613.
    [DataContract]
    internal sealed class EquipmentSlotItemState
    {
        [DataMember(Name = "instanceId", Order = 1)]
        public string InstanceId { get; set; } = string.Empty;

        [DataMember(Name = "nativeType", Order = 2)]
        public string NativeType { get; set; } = string.Empty;

        [DataMember(Name = "filled", Order = 3, EmitDefaultValue = false)]
        public bool? Filled { get; set; }

        [DataMember(Name = "cooldown", Order = 4, EmitDefaultValue = false)]
        public int? Cooldown { get; set; }

        internal EquipmentSlotItemState Clone() => (EquipmentSlotItemState)MemberwiseClone();

        internal bool IsValid => Guid.TryParseExact(InstanceId, "N", out _) &&
            (NativeType == "DolocTown.ItemHerbPackage"
                ? Filled.HasValue && !Cooldown.HasValue
                : NativeType == "DolocTown.ItemCooldown"
                    ? Cooldown >= 0 && !Filled.HasValue
                    : (NativeType == "DolocTown.ItemHat" ||
                       NativeType == "DolocTown.ItemHatShield" ||
                       NativeType == "DolocTown.ItemPassive") &&
                      !Filled.HasValue && !Cooldown.HasValue);

        internal bool CanUseIdOnlyMail =>
            NativeType == "DolocTown.ItemHat" || NativeType == "DolocTown.ItemPassive";

        internal static bool Same(EquipmentSlotItemState? a, EquipmentSlotItemState? b) =>
            a == null || b == null ? a == null && b == null :
            a.InstanceId == b.InstanceId && a.NativeType == b.NativeType &&
            a.Filled == b.Filled && a.Cooldown == b.Cooldown;
    }
}
