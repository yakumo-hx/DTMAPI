using System;
using System.IO;
using DolocTown;
using DolocTown.Config.Item;
using HarmonyLib;

namespace DTMAPI.MoreEquipmentSlots
{
    internal sealed class EquipmentItem
    {
        internal EquipmentItem(Item item, string? instanceId = null)
        {
            Item = item;
            InstanceId = instanceId ?? Guid.NewGuid().ToString("N");
            if (item.count != 1 || !(item is ItemHat || item is ItemPassive))
                throw new InvalidDataException("额外槽只能放一件帽子或被动装备。");
            if (item.GetType() != typeof(ItemHat) && item.GetType() != typeof(ItemHatShield) &&
                item.GetType() != typeof(ItemPassive) && item.GetType() != typeof(ItemHerbPackage) &&
                item.GetType() != typeof(ItemCooldown))
                throw new InvalidDataException("无法保存这种装备的状态：" + item.GetType().FullName);

            var proto = item.proto;
            DisplayName = proto.Title ?? item.name;
            if (proto.Function is ItemFunctionHatBase function)
            {
                var hat = function.HatId_Ref;
                Skill = hat.Skill ?? "";
                Defense = Math.Max(0, hat.Defense);
                if (item is ItemHatShield)
                {
                    ShieldMax = ((ItemFunctionHatShield)function).MaxShieldValue;
                    if (ShieldMax <= 0) throw new InvalidDataException("游戏的护盾最大值无效：" + item.name);
                    var defend = Traverse.Create(hat.Skill_Ref.Function).Property("Defend");
                    if (!defend.PropertyExists())
                        throw new MissingMemberException(hat.Skill_Ref.Function.GetType().FullName, "Defend");
                    ShieldDefense = Math.Max(0, defend.GetValue<int>());
                }
            }
            else
            {
                switch (proto.Function)
                {
                    case ItemFunctionHerbPackage herb: Skill = herb.Skill; break;
                    case ItemFunctionCooldown cooldown: Skill = cooldown.Skill; break;
                    case ItemFunctionPassive passive: Skill = passive.Skill; break;
                    default: throw new InvalidDataException("游戏无法识别装备效果：" + item.name);
                }
            }
        }

        internal Item Item { get; }
        internal string InstanceId { get; }
        internal string DisplayName { get; }
        internal string Skill { get; }
        internal int Defense { get; }
        internal int ShieldMax { get; }
        internal int ShieldDefense { get; }
        internal int ShieldValue => Item is ItemHatShield shield ? shield.ShieldValue : 0;
        internal bool CanMail => Item.GetType() == typeof(ItemHat) || Item.GetType() == typeof(ItemPassive);

        internal EquipmentSlotData Capture(int index)
        {
            var state = new EquipmentItemState
            {
                InstanceId = InstanceId,
                NativeType = Item.GetType().FullName!
            };
            if (Item is ItemHerbPackage herb) state.Filled = herb.isFilled;
            if (Item is ItemCooldown cooldown) state.Cooldown = cooldown.cdDuration;
            if (!state.IsValid)
                throw new InvalidDataException("游戏里的装备状态无法保存：" + Item.name);
            return new EquipmentSlotData
            {
                Index = index,
                ItemId = Item.name,
                DisplayName = DisplayName,
                SkillId = Skill,
                DefenseBonus = Defense,
                IsShield = Item is ItemHatShield,
                ShieldValue = ShieldValue,
                ShieldMaxValue = ShieldMax,
                ShieldDefend = ShieldDefense,
                ItemState = state
            };
        }

        internal static EquipmentItem Restore(EquipmentSlotData saved)
        {
            if (!ItemFactory.GenerateItem(saved.ItemId, 1, out Item item) || item == null)
                throw new InvalidDataException("游戏无法生成装备：" + saved.ItemId);
            EquipmentItemState? state = saved.ItemState;
            if (state != null && item.GetType().FullName != state.NativeType)
                throw new InvalidDataException("装备的游戏类型已改变：" + saved.ItemId);
            if (saved.IsShield != (item is ItemHatShield))
                throw new InvalidDataException("装备的护盾类型已改变：" + saved.ItemId);
            if (item is ItemHatShield shield) shield.SetShieldValue(saved.ShieldValue);
            if (item is ItemHerbPackage herb && state?.Filled is bool filled) herb.SetFilled(filled);
            if (item is ItemCooldown cooldown && state?.Cooldown is int remaining) cooldown.cdDuration = remaining;
            return new EquipmentItem(item, state?.InstanceId);
        }
    }
}
