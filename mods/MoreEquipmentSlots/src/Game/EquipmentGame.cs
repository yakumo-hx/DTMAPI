using System;
using System.Collections.Generic;
using DTMAPI.Abstractions;
using DolocTown;
using DolocTown.GameData;
using DolocTown.UI;
using HarmonyLib;

namespace DTMAPI.MoreEquipmentSlots
{
    internal sealed class EquipmentGame : IAgentEquipmentShieldItem, IDisposable
    {
        private readonly IMonitor monitor;
        private readonly Func<bool> canEdit;
        private readonly Action<string> notify;
        private readonly Action requestImport;
        private readonly EquipmentEffects effects = new EquipmentEffects();
        private readonly EquipmentView view;
        private readonly EquipmentHooks hooks;
        private EquipmentItem?[] slots = new EquipmentItem?[3];
        private bool loaded;

        internal EquipmentGame(IMonitor monitor, Func<bool> canEdit, Action<string> notify, Action requestImport)
        {
            this.monitor = monitor;
            this.canEdit = canEdit;
            this.notify = notify;
            this.requestImport = requestImport;
            view = new EquipmentView(this);
            hooks = new EquipmentHooks(this);
        }

        internal string AccessPrompt { get; set; } = "导入旧装备";
        internal bool CanEdit => canEdit();
        internal EquipmentItem? ItemAt(int index) => slots[index];
        internal void Install() => hooks.Install();
        internal void RefreshView() => view.Refresh();

        internal void Load(IReadOnlyList<EquipmentSlotData> saved)
        {
            var restored = new EquipmentItem?[3];
            for (int i = 0; i < restored.Length; i++)
                if (saved[i].ItemId.Length > 0) restored[i] = EquipmentItem.Restore(saved[i]);
            loaded = false;
            slots = new EquipmentItem?[3];
            effects.Clear();
            slots = restored;
            loaded = true;
            effects.Refresh(slots);
            view.Refresh();
        }

        internal List<EquipmentSlotData> Capture()
        {
            var saved = new List<EquipmentSlotData>(3);
            for (int i = 0; i < slots.Length; i++)
                saved.Add(slots[i]?.Capture(i) ?? new EquipmentSlotData { Index = i });
            return saved;
        }

        internal void Clear()
        {
            loaded = false;
            slots = new EquipmentItem?[3];
            view.Clear();
            effects.Clear();
        }

        internal void Click(int index) => HandleGameCall(() =>
        {
            if (!CanEdit) { requestImport(); return; }
            var buffer = DolocAPI.archiveHandle.InventorySystem.buffer;
            Item? held = buffer.CurrentItem;
            EquipmentItem? incoming = held == null ? null : new EquipmentItem(held);
            // 在移动任何物品前先找到归还手持物品的成员。
            Traverse? heldField = incoming == null ? null : Traverse.Create(buffer).Field("item");
            if (heldField != null && !heldField.FieldExists())
                throw new MissingFieldException(buffer.GetType().FullName, "item");
            EquipmentItem? outgoing = slots[index];
            if (outgoing != null && !ReturnItem(outgoing)) return;
            if (incoming != null)
            {
                try
                {
                    Item? taken = buffer.Take();
                    if (!ReferenceEquals(taken, held))
                        throw new InvalidOperationException("游戏没有取出手持的装备。");
                }
                catch
                {
                    // Take 的游戏回调可能在清空手持物品后抛出；槽仍保持原样。
                    heldField!.SetValue(held);
                    throw;
                }
            }
            slots[index] = incoming;
            try
            {
                effects.Refresh(slots);
                view.Refresh();
            }
            catch
            {
                // 先归还新物品，再恢复原槽；后续刷新失败也不会丢掉任何一件。
                if (incoming != null) heldField!.SetValue(held);
                slots[index] = outgoing;
                effects.Refresh(slots);
                view.Refresh();
                throw;
            }
        });

        private bool ReturnItem(EquipmentItem item)
        {
            // 用另一件原生实例调用放入；原调用出错也不会改掉槽中那一件。
            Item returned = EquipmentItem.Restore(item.Capture(0)).Item;
            if (DolocAPI.TryPlaceInBackpack(returned, false)) return true;
            if (!item.CanMail)
            {
                notify("背包已满，请留出一个空位；装备仍在额外槽里。");
                return false;
            }
            if (DolocAPI.SendItemAsEmail(item.Item.name, 1, null, null, null, "send_item_template")) return true;
            notify("装备没有退回成功，仍在额外槽里。");
            return false;
        }

        internal void RequestAccess() => HandleGameCall(requestImport);
        internal void Render(AccessoriesBar bar, int count)
        {
            try { view.Render(bar, count); }
            catch { view.Clear(); throw; }
        }
        internal UnityEngine.UI.Selectable[] Selectables(AccessoriesBar bar, UnityEngine.UI.Selectable[] native) =>
            view.Selectables(bar, native);
        internal void Hide(AccessoriesBar bar) => view.Hide(bar);
        internal void ApplyDefense(AgentEquipmentManager manager)
        {
            if (loaded) effects.ApplyDefense(manager, slots);
        }
        internal IAgentEquipmentShieldItem? ExtraShield => loaded && ShieldIndex >= 0 ? this : null;

        private int ShieldIndex
        {
            get
            {
                for (int i = slots.Length - 1; i >= 0; i--)
                    if (slots[i]?.Item is ItemHatShield && slots[i]!.ShieldValue > 0) return i;
                return -1;
            }
        }

        public float ShieldValue => ShieldIndex < 0 ? 0 : slots[ShieldIndex]!.ShieldValue;
        public float ShieldPercent => ShieldIndex < 0 ? 0 : ShieldValue / slots[ShieldIndex]!.ShieldMax;

        public bool TryBlockAttack(int damage, out int blockedDamage)
        {
            int blocked = 0;
            bool result = false;
            HandleGameCall(() => result = BlockAttack(damage, out blocked));
            blockedDamage = blocked;
            return result;
        }

        private bool BlockAttack(int damage, out int blockedDamage)
        {
            blockedDamage = 0;
            int index = ShieldIndex;
            if (index < 0) return false;
            EquipmentItem item = slots[index]!;
            int reduced = Math.Max(0, damage - item.ShieldDefense);
            if (reduced == 0) return true;
            int available = item.ShieldValue;
            if (available > reduced)
            {
                ((ItemHatShield)item.Item).SetShieldValue(available - reduced);
                blockedDamage = reduced;
            }
            else
            {
                blockedDamage = available;
                slots[index] = null;
            }
            HandleGameCall(() =>
            {
                if (slots[index] == null) effects.Refresh(slots);
                DolocAPI.uiSystem.agentStatusBar.UpdateHealth();
                view.Refresh();
            });
            return available > reduced;
        }

        internal void HandleGameCall(Action action)
        {
            try { action(); }
            catch (Exception ex)
            {
                string message = "额外装备操作失败：" + ex.GetBaseException().Message;
                monitor.Log(message + Environment.NewLine + ex, LogLevel.Error);
                notify(message);
            }
        }

        public void Dispose()
        {
            hooks.Dispose();
            Clear();
        }
    }
}
