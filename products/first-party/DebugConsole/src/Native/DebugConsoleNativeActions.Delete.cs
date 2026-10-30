using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DTMAPI.Abstractions;
using Native = DTMAPI.DebugConsole.DebugConsoleNativeAccess;

namespace DTMAPI.DebugConsole
{
    internal sealed class SelectedStackDeleteToken
    {
        internal string Owner = string.Empty;
        internal long Window;
        internal DtmRuntimeContextSnapshot Epoch = null!;
        internal object Archive = null!, InventorySystem = null!, Backpack = null!, Item = null!;
        internal List<DeleteItemSnapshot> Contents = new List<DeleteItemSnapshot>();
        public int Slot { get; internal set; }
        public string ItemId { get; internal set; } = string.Empty;
        public string DisplayName { get; internal set; } = string.Empty;
        public int Quantity { get; internal set; }
        public bool HasContents => Contents.Any(s => s.Slots != null && s.Slots.Any(i => i != null));
    }
    internal sealed class DeleteItemSnapshot
    {
        internal object Item = null!;
        internal string Id = string.Empty;
        internal int Count;
        internal object? Inventory;
        internal object?[]? Slots;
    }
    internal sealed class SelectedStackDeletePreparation
    {
        public SelectedStackDeleteToken? Token { get; internal set; }
        public string Reason { get; internal set; } = string.Empty;
    }
    internal sealed class SelectedStackDeleteResult
    {
        public string Outcome { get; internal set; } = "invalidated";
        public string Detail { get; internal set; } = string.Empty;
        public bool Removed => Outcome == "removed" || Outcome == "removed-notification-failed";
    }

    internal sealed partial class DebugConsoleNativeActions
    {
        private IDtmRuntimeContext? deletionContext;
        private SelectedStackDeleteToken? pendingDelete;
        internal void SetDeletionContext(IDtmRuntimeContext context) => deletionContext = context;
        private static object DeleteApi => Native.Resolve("DolocAPI, Assembly-CSharp") ?? throw new InvalidOperationException("native-api-unavailable");
        private static object DeleteRequired(object value, string name) => Native.Read(value, name) ?? throw new MissingMemberException(name);
        private static object? DeleteReadSlot(object inventory, int index) => DeleteMethod(inventory, "Read", typeof(int)).Invoke(inventory, new object[] { index });
        private static MethodInfo DeleteMethod(object target, string name, params Type[] args) => target.GetType().GetMethod(name,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, args, null) ?? throw new MissingMethodException(name);
        private static string DeleteId(object item) => (string)DeleteRequired(item, "name");
        private static int DeleteCount(object item) => (int)DeleteRequired(item, "count");
        private static object? DeleteInventory(object item)
        {
            Type contract = Native.Resolve("DolocTown.IContainer, Assembly-CSharp") ?? throw new InvalidOperationException("container-contract-unavailable");
            return contract.IsInstanceOfType(item) ? contract.GetProperty("inventory")!.GetValue(item, null) : null;
        }
        private static object?[] DeleteSlots(object inventory) => ((IEnumerable)(DeleteMethod(inventory, "ReadAllWithNull")
            .Invoke(inventory, null) ?? throw new InvalidOperationException("inventory-unreadable"))).Cast<object?>().ToArray();

        public SelectedStackDeletePreparation PrepareSelectedStackDelete(IManifest owner, long window)
        {
            pendingDelete = null;
            var result = new SelectedStackDeletePreparation();
            try
            {
                IDtmRuntimeContext context = deletionContext ?? throw new InvalidOperationException("session-unavailable");
                object api = DeleteApi;
                int slot = (int)DeleteRequired(api, "SelectedItemIndex");
                if (slot < 0) { result.Reason = "select-backpack-slot"; return result; }
                object archive = Native.Archive ?? throw new InvalidOperationException("session-unavailable");
                object system = DeleteRequired(archive, "InventorySystem"), backpack = DeleteRequired(system, "inventory");
                object? item = DeleteReadSlot(backpack, slot);
                if (item == null || !ReferenceEquals(item, Native.Read(api, "SelectedItem")))
                { result.Reason = "select-backpack-slot"; return result; }
                var token = new SelectedStackDeleteToken { Owner = owner.UniqueID, Window = window, Epoch = context.Snapshot,
                    Archive = archive, InventorySystem = system, Backpack = backpack, Item = item, Slot = slot,
                    ItemId = DeleteId(item), Quantity = DeleteCount(item), DisplayName = (string)DeleteRequired(item, "title") };
                if (token.Quantity <= 0) throw new InvalidOperationException("invalid-item-count");
                // Snapshot occupied objects and slot positions, never expand stack counts or clone native items.
                var queue = new Queue<object>(); queue.Enqueue(item);
                var seen = new HashSet<object>(CropReferenceComparer.Instance);
                while (queue.Count > 0)
                {
                    object current = queue.Dequeue();
                    if (!seen.Add(current)) continue;
                    object? inventory = DeleteInventory(current);
                    var observation = new DeleteItemSnapshot { Item = current, Id = DeleteId(current), Count = DeleteCount(current),
                        Inventory = inventory, Slots = inventory == null ? null : DeleteSlots(inventory) };
                    token.Contents.Add(observation);
                    if (observation.Slots != null) foreach (object? child in observation.Slots) if (child != null) queue.Enqueue(child);
                }
                pendingDelete = token;
                if (!IsSelectedStackDeleteCurrent(owner, token, window)) { pendingDelete = null; result.Reason = "target-changed"; return result; }
                result.Token = token;
            }
            catch (Exception error) { pendingDelete = null; result.Reason = "target-unavailable"; runtime.Error("prepare-delete-stack", error.GetBaseException()); }
            return result;
        }

        public bool IsSelectedStackDeleteCurrent(IManifest owner, SelectedStackDeleteToken token, long window)
        {
            try
            {
                if (!ReferenceEquals(pendingDelete, token) || !DeleteSessionCurrent(owner, token, window)) return false;
                if ((int)DeleteRequired(DeleteApi, "SelectedItemIndex") != token.Slot ||
                    !ReferenceEquals(Native.Read(DeleteApi, "SelectedItem"), token.Item) ||
                    !ReferenceEquals(DeleteReadSlot(token.Backpack, token.Slot), token.Item)) return false;
                foreach (DeleteItemSnapshot node in token.Contents)
                {
                    if (!string.Equals(DeleteId(node.Item), node.Id, StringComparison.Ordinal) || DeleteCount(node.Item) != node.Count ||
                        !ReferenceEquals(DeleteInventory(node.Item), node.Inventory)) return false;
                    if (node.Inventory == null) continue;
                    object?[] current = DeleteSlots(node.Inventory);
                    if (current.Length != node.Slots!.Length) return false;
                    for (int i = 0; i < current.Length; i++) if (!ReferenceEquals(current[i], node.Slots[i])) return false;
                }
                return true;
            }
            catch { return false; }
        }
        private bool DeleteSessionCurrent(IManifest owner, SelectedStackDeleteToken token, long window)
        {
            IDtmRuntimeContext context = deletionContext ?? throw new InvalidOperationException("session-unavailable");
            DtmRuntimeContextSnapshot now = context.Snapshot;
            return context.IsMainThread && IsCurrentSaveSession && now.IsWorldReady && token.Epoch.IsWorldReady &&
                token.Owner == owner.UniqueID && token.Window == window &&
                now.RuntimeInstanceId == token.Epoch.RuntimeInstanceId && now.SaveSessionEpoch == token.Epoch.SaveSessionEpoch &&
                now.WorldEpoch == token.Epoch.WorldEpoch && ReferenceEquals(Native.Archive, token.Archive) &&
                ReferenceEquals(Native.Read(token.Archive, "InventorySystem"), token.InventorySystem) &&
                ReferenceEquals(Native.Read(token.InventorySystem, "inventory"), token.Backpack);
        }
        public void CancelSelectedStackDelete(SelectedStackDeleteToken token)
        { if (ReferenceEquals(pendingDelete, token)) pendingDelete = null; }

        public SelectedStackDeleteResult ConfirmSelectedStackDelete(IManifest owner, SelectedStackDeleteToken token, long window)
        {
            bool valid = IsSelectedStackDeleteCurrent(owner, token, window);
            CancelSelectedStackDelete(token); // Consume before any native call or notification reentry.
            var result = new SelectedStackDeleteResult();
            if (!valid) return result;
            Exception? failure = null;
            try
            {
                Type api = (Type)DeleteApi;
                MethodInfo method = api.GetMethod("DestroyItem", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(int) }, null)
                    ?? throw new MissingMethodException("DestroyItem(int)");
                method.Invoke(null, new object[] { token.Slot });
            }
            catch (Exception error) { failure = error.GetBaseException(); runtime.Error("delete-stack", failure); }
            // Read only the original slot, even if a notification changed the current archive/selection.
            try
            {
                object? after = DeleteReadSlot(token.Backpack, token.Slot);
                result.Outcome = after == null ? (failure == null ? "removed" : "removed-notification-failed") :
                    ReferenceEquals(after, token.Item) ? "not-removed" : "slot-replaced-unknown";
            }
            catch (Exception error) { result.Outcome = "unreadable-unknown"; result.Detail = error.GetBaseException().Message; }
            if (failure != null) result.Detail = failure.Message;
            LogMutation("delete-selected-stack", result.Outcome == "removed", "slot=" + token.Slot + " id=" + token.ItemId +
                " quantity=" + token.Quantity + " outcome=" + result.Outcome + " detail=" + result.Detail);
            return result;
        }
    }
}
