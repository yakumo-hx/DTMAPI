using System;
using System.Collections;
using System.Collections.Generic;
using DolocTown;
using DolocTown.GameData;
using HarmonyLib;

namespace DTMAPI.MoreEquipmentSlots
{
    internal sealed class EquipmentEffects
    {
        private readonly Dictionary<Item, AgentEquipmentFunction> added =
            new Dictionary<Item, AgentEquipmentFunction>();
        private AgentEquipmentManager? manager;

        internal void Refresh(EquipmentItem?[] slots)
        {
            AgentEquipmentManager current = DolocAPI.archiveHandle.farmData.agentData.agentEquipment;
            if (!ReferenceEquals(current, manager))
            {
                Clear();
                manager = current;
            }

            var wanted = new Dictionary<Item, EquipmentItem>();
            foreach (EquipmentItem? slot in slots)
                if (slot != null && !(slot.Item is ItemHatShield) && slot.Skill.Length > 0)
                    wanted.Add(slot.Item, slot);
            IDictionary functions = Traverse.Create(manager).Field("functions").GetValue<IDictionary>();
            foreach (Item item in new List<Item>(added.Keys))
            {
                if (wanted.ContainsKey(item)) continue;
                AgentEquipmentFunction function = added[item];
                if (ReferenceEquals(functions[item], function)) functions.Remove(item);
                added.Remove(item);
                function.OnRemove();
            }
            foreach (EquipmentItem slot in wanted.Values)
            {
                if (added.ContainsKey(slot.Item)) continue;
                if (!AgentEquipmentFunction.CreateAgentEquipmentFunction(slot.Item, manager, slot.Skill,
                        out AgentEquipmentFunction function))
                    throw new InvalidOperationException("游戏无法创建装备效果：" + slot.Item.name);
                functions.Add(slot.Item, function);
                added.Add(slot.Item, function);
                function.OnCreate();
            }
            manager.ReloadParams();
        }

        internal void ApplyDefense(AgentEquipmentManager target, EquipmentItem?[] slots)
        {
            if (!ReferenceEquals(manager, target)) return;
            int defense = 0;
            foreach (EquipmentItem? item in slots) defense += item?.Defense ?? 0;
            if (defense == 0) return;
            var ability = target.EquipmentAbility;
            // 原生能力值不公开写入；只改这次重算得到的防御值，其余字段留在原生对象上。
            object combined = ability;
            Traverse.Create(combined).Field("Defence").SetValue(ability.Defence + defense);
            Traverse.Create(target).Property("EquipmentAbility").SetValue(combined);
        }

        internal void Clear()
        {
            if (manager == null) return;
            IDictionary functions = Traverse.Create(manager).Field("functions").GetValue<IDictionary>();
            foreach (KeyValuePair<Item, AgentEquipmentFunction> pair in new Dictionary<Item, AgentEquipmentFunction>(added))
            {
                if (ReferenceEquals(functions[pair.Key], pair.Value)) functions.Remove(pair.Key);
                added.Remove(pair.Key);
                pair.Value.OnRemove();
            }
            AgentEquipmentManager previous = manager;
            manager = null;
            // 回标题时原生存档已卸载，不再刷新角色状态栏。
            if (DolocAPI.IsDataLoaded &&
                ReferenceEquals(previous, DolocAPI.archiveHandle.farmData.agentData.agentEquipment))
                previous.ReloadParams();
        }
    }
}
