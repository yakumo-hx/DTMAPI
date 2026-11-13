using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace DTMAPI.MoreEquipmentSlots
{
    internal enum EquipmentPayloadReadKind { Missing, Ready, Unavailable }

    // 原生变量里的格式沿用 schema 1。这里不访问游戏，也不改原始值。
    internal static class EquipmentPayload
    {
        internal const string Key = "$DTMAPI__MoreEquipmentSlots_v1";

        internal static List<EquipmentSlotData> EmptySlots() => new List<EquipmentSlotData>
        {
            new EquipmentSlotData { Index = 0 },
            new EquipmentSlotData { Index = 1 },
            new EquipmentSlotData { Index = 2 }
        };

        internal static string Encode(IReadOnlyList<EquipmentSlotData> slots)
        {
            using (var stream = new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(Payload)).WriteObject(stream,
                    new Payload { Schema = 1, Slots = new List<EquipmentSlotData>(slots) });
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        internal static EquipmentPayloadReadKind Read(bool exists, object? raw,
            out List<EquipmentSlotData>? slots, out string error)
        {
            slots = null;
            error = string.Empty;
            if (!exists) return EquipmentPayloadReadKind.Missing;
            if (!(raw is string text))
            {
                error = "存档里的装备数据不是文本，额外槽暂时不能使用。原数据已保留。";
                return EquipmentPayloadReadKind.Unavailable;
            }

            try
            {
                Payload? payload;
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(text)))
                    payload = new DataContractJsonSerializer(typeof(Payload)).ReadObject(stream) as Payload;
                if (payload == null) throw new SerializationException("装备数据为空。");
                if (payload.Schema != 1)
                {
                    error = "不认识存档里的装备数据版本 " + payload.Schema + "，额外槽暂时不能使用。原数据已保留。";
                    return EquipmentPayloadReadKind.Unavailable;
                }
                Validate(payload.Slots);
                slots = payload.Slots;
                return EquipmentPayloadReadKind.Ready;
            }
            catch (Exception exception)
            {
                // 读存档是输入边界。返回不可用，调用方不能用空槽覆盖这个值。
                error = "存档里的装备数据读不了，额外槽暂时不能使用。原数据已保留：" + exception.Message;
                return EquipmentPayloadReadKind.Unavailable;
            }
        }

        private static void Validate(List<EquipmentSlotData>? slots)
        {
            if (slots == null || slots.Count != 3)
                throw new SerializationException("装备数据需要包含三个槽位。");
            var instances = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < slots.Count; index++)
            {
                EquipmentSlotData? slot = slots[index];
                if (slot == null || slot.Index != index || slot.ItemId == null ||
                    slot.DisplayName == null || slot.SkillId == null || slot.ItemId != slot.ItemId.Trim())
                    throw new SerializationException("装备槽位或物品编号无效。");
                if (!slot.IsOccupied)
                {
                    if (slot.ItemState != null || slot.DisplayName.Length != 0 || slot.SkillId.Length != 0 ||
                        slot.DefenseBonus != 0 || slot.IsShield || slot.ShieldValue != 0 ||
                        slot.ShieldMaxValue != 0 || slot.ShieldDefend != 0)
                        throw new SerializationException("空槽里仍有装备数据。");
                    continue;
                }
                EquipmentItemState? state = slot.ItemState;
                if (state == null || !state.IsValid || !instances.Add(state.InstanceId) ||
                    slot.IsShield != (state.NativeType == "DolocTown.ItemHatShield"))
                    throw new SerializationException("装备的实例数据无效。");
                if (slot.ShieldValue < 0 || slot.ShieldMaxValue < 0 || slot.ShieldDefend < 0 ||
                    slot.ShieldValue > slot.ShieldMaxValue ||
                    (slot.IsShield && slot.ShieldMaxValue == 0) ||
                    (!slot.IsShield && (slot.ShieldValue != 0 || slot.ShieldMaxValue != 0 || slot.ShieldDefend != 0)))
                    throw new SerializationException("装备的护盾数据无效。");
            }
        }

        [DataContract]
        private sealed class Payload
        {
            [DataMember(Name = "schema", IsRequired = true, Order = 1)] public int Schema { get; set; }
            [DataMember(Name = "slots", IsRequired = true, Order = 2)] public List<EquipmentSlotData> Slots { get; set; } = null!;
        }
    }

    [DataContract]
    internal sealed class EquipmentSlotData
    {
        [DataMember(Name = "index", IsRequired = true, Order = 1)] public int Index { get; set; }
        [DataMember(Name = "itemId", IsRequired = true, Order = 2)] public string ItemId { get; set; } = string.Empty;
        [DataMember(Name = "displayName", IsRequired = true, Order = 3)] public string DisplayName { get; set; } = string.Empty;
        [DataMember(Name = "skillId", IsRequired = true, Order = 4)] public string SkillId { get; set; } = string.Empty;
        [DataMember(Name = "defenseBonus", IsRequired = true, Order = 5)] public int DefenseBonus { get; set; }
        [DataMember(Name = "isShield", IsRequired = true, Order = 6)] public bool IsShield { get; set; }
        [DataMember(Name = "shieldValue", IsRequired = true, Order = 7)] public int ShieldValue { get; set; }
        [DataMember(Name = "shieldMaxValue", IsRequired = true, Order = 8)] public int ShieldMaxValue { get; set; }
        [DataMember(Name = "shieldDefend", IsRequired = true, Order = 9)] public int ShieldDefend { get; set; }
        [DataMember(Name = "itemState", EmitDefaultValue = false, Order = 10)] public EquipmentItemState? ItemState { get; set; }
        internal bool IsOccupied => ItemId.Length != 0;
    }

    [DataContract]
    internal sealed class EquipmentItemState
    {
        [DataMember(Name = "instanceId", IsRequired = true, Order = 1)] public string InstanceId { get; set; } = string.Empty;
        [DataMember(Name = "nativeType", IsRequired = true, Order = 2)] public string NativeType { get; set; } = string.Empty;
        [DataMember(Name = "filled", EmitDefaultValue = false, Order = 3)] public bool? Filled { get; set; }
        [DataMember(Name = "cooldown", EmitDefaultValue = false, Order = 4)] public int? Cooldown { get; set; }

        internal bool IsValid => Guid.TryParseExact(InstanceId, "N", out _) &&
            (NativeType == "DolocTown.ItemHerbPackage" ? Filled.HasValue && !Cooldown.HasValue :
             NativeType == "DolocTown.ItemCooldown" ? Cooldown >= 0 && !Filled.HasValue :
             (NativeType == "DolocTown.ItemHat" || NativeType == "DolocTown.ItemHatShield" ||
              NativeType == "DolocTown.ItemPassive") && !Filled.HasValue && !Cooldown.HasValue);
    }
}
