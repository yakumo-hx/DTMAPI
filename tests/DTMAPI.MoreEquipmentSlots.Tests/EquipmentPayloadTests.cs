using System;
using System.Collections.Generic;
using DTMAPI.MoreEquipmentSlots;

namespace DTMAPI.UnitTests
{
    internal static class EquipmentPayloadTests
    {
        internal static void RunAll()
        {
            EmptySlotsRoundTrip();
            ShieldAndMutableItemsRoundTrip();
            MissingValueAllowsNewSlots();
            DamagedValueHasNoWritableSlots();
            NewerVersionHasNoWritableSlots();
        }

        private static void EmptySlotsRoundTrip()
        {
            var save = new EquipmentSave(false, null);
            save.Enable();
            string raw = string.Empty;
            Check(save.Write(EquipmentPayload.EmptySlots(), value => raw = value), "空的新存档可以随原生保存写入装备数据。");
            Check(EquipmentPayload.Read(true, raw, out var slots, out _) == EquipmentPayloadReadKind.Ready,
                "三个空槽应该能读写往返。");
            for (int index = 0; index < 3; index++)
                Check(slots![index].Index == index && !slots[index].IsOccupied, "空槽的顺序和空状态要保留。");
        }

        private static void ShieldAndMutableItemsRoundTrip()
        {
            List<EquipmentSlotData> source = EquipmentPayload.EmptySlots();
            source[0] = new EquipmentSlotData
            {
                Index = 0, ItemId = "shield", DisplayName = "测试护盾", SkillId = "shield-skill",
                DefenseBonus = 7, IsShield = true, ShieldValue = 21, ShieldMaxValue = 50, ShieldDefend = 5,
                ItemState = State("DolocTown.ItemHatShield")
            };
            source[1] = new EquipmentSlotData
            {
                Index = 1, ItemId = "herb", ItemState = State("DolocTown.ItemHerbPackage", filled: false)
            };
            source[2] = new EquipmentSlotData
            {
                Index = 2, ItemId = "cooldown", ItemState = State("DolocTown.ItemCooldown", cooldown: 37)
            };
            string raw = EquipmentPayload.Encode(source);
            Check(EquipmentPayload.Read(true, raw, out var slots, out _) == EquipmentPayloadReadKind.Ready,
                "带状态的装备应该能读写往返。");
            Check(slots![0].ItemId == "shield" && slots[0].ShieldValue == 21 && slots[0].ShieldMaxValue == 50 &&
                slots[0].ShieldDefend == 5 && slots[0].DefenseBonus == 7 && slots[0].DisplayName == "测试护盾" &&
                slots[0].SkillId == "shield-skill" && slots[0].ItemState!.InstanceId == source[0].ItemState!.InstanceId,
                "护盾数值、显示资料和物品实例要保留。");
            Check(slots[1].ItemState!.Filled == false && slots[1].ItemState!.Cooldown == null,
                "空药包不能变成装满的药包。");
            Check(slots[2].ItemState!.Cooldown == 37 && slots[2].ItemState!.Filled == null,
                "冷却时间不能重置。");
        }

        private static void MissingValueAllowsNewSlots()
        {
            Check(EquipmentPayload.Read(false, null, out var slots, out var error) == EquipmentPayloadReadKind.Missing &&
                slots == null && error.Length == 0, "没有数据和损坏的数据必须能区分。");
        }

        private static void DamagedValueHasNoWritableSlots()
        {
            RejectWithoutChangingOriginal("{ broken");
            RejectWithoutChangingOriginal(123);
            RejectWithoutChangingOriginal("{\"schema\":1,\"slots\":[]}");
            string complete = EquipmentPayload.Encode(EquipmentPayload.EmptySlots());
            RejectWithoutChangingOriginal(complete.Replace("\"shieldValue\":0,", ""));

            List<EquipmentSlotData> slots = EquipmentPayload.EmptySlots();
            slots[0].ItemId = "hat"; // 没有实例数据，不能把它当成完整装备保存。
            RejectWithoutChangingOriginal(EquipmentPayload.Encode(slots));
            slots[0].ItemState = State("DolocTown.ItemHat");
            slots[1].ItemId = "another-hat";
            slots[1].ItemState = slots[0].ItemState;
            RejectWithoutChangingOriginal(EquipmentPayload.Encode(slots));
        }

        private static void NewerVersionHasNoWritableSlots()
        {
            string complete = EquipmentPayload.Encode(EquipmentPayload.EmptySlots());
            RejectWithoutChangingOriginal(complete.Replace("\"schema\":1", "\"schema\":2"));
        }

        private static void RejectWithoutChangingOriginal(object raw)
        {
            var variables = new Dictionary<string, object> { [EquipmentPayload.Key] = raw };
            var save = new EquipmentSave(true, variables[EquipmentPayload.Key]);
            Check(save.Kind == EquipmentPayloadReadKind.Unavailable && save.Slots == null && save.Error.Length != 0,
                "无法读取时不能给调用方一组可写回的空槽。");
            bool wrote = false;
            bool accepted = save.Write(EquipmentPayload.EmptySlots(), value =>
            {
                wrote = true;
                variables[EquipmentPayload.Key] = value;
            });
            Check(!accepted && !wrote, "读不了的存档在保存时不能调用原生变量写入。");
            Check(ReferenceEquals(variables[EquipmentPayload.Key], raw), "损坏或更新版本的原值必须保持不动。");
        }

        private static EquipmentItemState State(string type, bool? filled = null, int? cooldown = null) =>
            new EquipmentItemState
            {
                InstanceId = Guid.NewGuid().ToString("N"), NativeType = type, Filled = filled, Cooldown = cooldown
            };

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
