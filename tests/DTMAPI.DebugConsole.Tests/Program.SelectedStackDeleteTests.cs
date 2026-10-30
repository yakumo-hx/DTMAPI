using System;
using System.Linq;
using System.Reflection;
using DTMAPI.Abstractions;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;
using DTMAPI.DebugConsole;
using DTMAPI.ModConfigMenu;

public static partial class DolocAPI
{
    public static int SelectedItemIndex;
    public static object? SelectedItem;
    public static Action<int>? DeleteStackAction;
    public static void DestroyItem(int index) => DeleteStackAction!(index);
}
namespace DolocTown
{
    public interface IContainer { DTMAPI.UnitTests.DeleteTestInventory inventory { get; } }
}
namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {
        private static void DebugConsoleSelectedStackDeleteIdentityAndContents()
        {
            WithDeleteActions((actions, owner, context, inventory) =>
            {
                var item = new DeleteTestItem { name = "CannotDiscard", count = 999 };
                SelectDeleteItem(inventory, item);
                SelectedStackDeleteToken token = actions.PrepareSelectedStackDelete(owner, 4).Token!;
                Assert(token.Quantity == 999 && !token.HasContents && token.DisplayName == "CannotDiscard title", "Confirmation uses exact name/whole count without disposal flag requirements.");
                actions.CancelSelectedStackDelete(token);
                Assert(actions.ConfirmSelectedStackDelete(owner, token, 4).Outcome == "invalidated" && inventory.Calls == 0, "Cancel cannot delete.");
                token = actions.PrepareSelectedStackDelete(owner, 5).Token!;
                Assert(actions.ConfirmSelectedStackDelete(owner, token, 5).Removed && inventory.Calls == 1 && inventory.Read(0) == null, "One confirmation removes one complete stack.");
                Assert(!actions.ConfirmSelectedStackDelete(owner, token, 5).Removed && inventory.Calls == 1, "Repeated confirmation never repeats native removal.");
                foreach (int index in new[] { -1, -2 }) { DolocAPI.SelectedItemIndex = index; DolocAPI.SelectedItem = item;
                    Assert(actions.PrepareSelectedStackDelete(owner, 6).Token == null, "Equipment/drone selected indices are excluded."); }
                SelectDeleteItem(inventory, item); DolocAPI.SelectedItem = new DeleteTestItem { name = item.name, count = item.count };
                Assert(actions.PrepareSelectedStackDelete(owner, 7).Token == null, "SelectedItem must equal the actual slot by reference.");
            });
            foreach (string change in new[] { "index", "replace", "count", "id", "sort", "backpack", "archive", "epoch", "owner", "window" })
                WithDeleteActions((actions, owner, context, inventory) =>
                {
                    var item = new DeleteTestItem(); SelectDeleteItem(inventory, item);
                    SelectedStackDeleteToken token = actions.PrepareSelectedStackDelete(owner, 8).Token!;
                    if (change == "index") DolocAPI.SelectedItemIndex = 1;
                    if (change == "replace") SelectDeleteItem(inventory, new DeleteTestItem());
                    if (change == "count") item.count++;
                    if (change == "id") item.name = "WOOD";
                    if (change == "sort") { inventory.Slots[0] = null; inventory.Slots[1] = item; }
                    if (change == "backpack") ((DeleteTestArchive)DolocAPI.archiveHandle!).InventorySystem.inventory = new DeleteTestInventory();
                    if (change == "archive") DolocAPI.archiveHandle = new DeleteTestArchive();
                    if (change == "epoch") context.Epoch++;
                    if (change == "owner") actions.EndSaveSession("owner-deactivation");
                    Assert(actions.ConfirmSelectedStackDelete(owner, token, change == "window" ? 9 : 8).Outcome == "invalidated" && inventory.Calls == 0,
                        "Changed " + change + " invalidates the old confirmation.");
                });
            WithDeleteActions((actions, owner, context, inventory) =>
            {
                var box = new DeleteTestContainer();
                var gun = new DeleteTestContainer { name = "gun" };
                gun.inventory.Slots[0] = new DeleteTestItem { name = "seed", count = 999 };
                box.inventory.Slots = Enumerable.Range(0, 16).Select(i => (DeleteTestItem?)new DeleteTestItem { name = "wood" + i, count = 999 }).ToArray();
                box.inventory.Slots[15] = gun;
                SelectDeleteItem(inventory, box);
                SelectedStackDeleteToken token = actions.PrepareSelectedStackDelete(owner, 10).Token!;
                Assert(token.HasContents && token.Contents.Count == 18, "Full box plus nested loaded gun is captured once per existing object, not999 times.");
                gun.inventory.Slots[0]!.count--;
                Assert(actions.ConfirmSelectedStackDelete(owner, token, 10).Outcome == "invalidated" && inventory.Calls == 0, "Nested quantity change requires reconfirmation.");
                token = actions.PrepareSelectedStackDelete(owner, 11).Token!;
                box.inventory.Slots[0] = new DeleteTestItem { name = "wood0", count = 999 };
                Assert(!actions.ConfirmSelectedStackDelete(owner, token, 11).Removed && inventory.Calls == 0, "Same-shell/same-id/same-count content replacement invalidates.");
                token = actions.PrepareSelectedStackDelete(owner, 12).Token!;
                object originalContents = box.inventory;
                Assert(actions.ConfirmSelectedStackDelete(owner, token, 12).Removed && inventory.Calls == 1 &&
                    ReferenceEquals(originalContents, box.inventory) && box.inventory.Slots.Count(i => i != null) == 16 && gun.inventory.Slots[0]!.count == 998,
                    "Delete only the selected root via one native call; never clear interior arrays or recursively delete contents.");
            });
        }

        private static void DebugConsoleSelectedStackDeleteNotificationAndUiLifetime()
        {
            foreach (string failure in new[] { "after", "before", "replacement", "unreadable" })
                WithDeleteActions((actions, owner, context, inventory) =>
                {
                    var original = new DeleteTestItem(); SelectDeleteItem(inventory, original);
                    SelectedStackDeleteToken token = actions.PrepareSelectedStackDelete(owner, 1).Token!;
                    if (failure == "before") inventory.Before = () => throw new Exception("before-remove");
                    else inventory.After = () => { if (failure == "replacement") inventory.Slots[0] = new DeleteTestItem();
                        if (failure == "unreadable") inventory.Unreadable = true;
                        throw new Exception("notification"); };
                    SelectedStackDeleteResult result = actions.ConfirmSelectedStackDelete(owner, token, 1);
                    string expected = failure == "after" ? "removed-notification-failed" : failure == "before" ? "not-removed" :
                        failure == "replacement" ? "slot-replaced-unknown" : "unreadable-unknown";
                    Assert(result.Outcome == expected && inventory.Calls == 1 && !actions.ConfirmSelectedStackDelete(owner, token, 1).Removed && inventory.Calls == 1,
                        "Native " + failure + " is reported from the original slot and never retried.");
                });
            WithDeleteActions((actions, owner, context, inventory) =>
            {
                SelectDeleteItem(inventory, new DeleteTestItem());
                var first = actions.PrepareSelectedStackDelete(owner, 1).Token!;
                context.Epoch++;
                var next = actions.PrepareSelectedStackDelete(owner, 2).Token!;
                Assert(!actions.ConfirmSelectedStackDelete(owner, first, 1).Removed && actions.IsSelectedStackDeleteCurrent(owner, next, 2),
                    "Old-session callback cannot cancel or delete a successor token.");
                var runtime = new DtmApiRuntime(new FakeHost(NewTempGameDir()), new ConfigMenuRegistry());
                var ui = new DebugConsoleUi(new DebugConsoleTestRuntime(runtime));
                ui.BindAdvanced(owner, actions);
                SetDeleteField(ui, "<IsOpen>k__BackingField", true);
                SetDeleteField(ui, "deleteConfirmation", next); SetDeleteField(ui, "deleteConfirmationGeneration", 2L);
                SetDeleteField(actions, "godEnabled", true);
                MethodInfo cancel = typeof(DebugConsoleUi).GetMethod("CancelSelectedStackDelete", BindingFlags.NonPublic | BindingFlags.Instance)!;
                cancel.Invoke(ui, new object[] { first, 1L });
                Assert(actions.IsSelectedStackDeleteCurrent(owner, next, 2), "Old-window cancel cannot close the new confirmation.");
                cancel.Invoke(ui, new object[] { next, 2L });
                Assert(ui.IsOpen && actions.GodModeEnabled && inventory.Calls == 0 && !actions.IsSelectedStackDeleteCurrent(owner, next, 2),
                    "Cancelling only the confirmation preserves the Y window and active session mode.");
                next = actions.PrepareSelectedStackDelete(owner, 4).Token!;
                SetDeleteField(ui, "deleteConfirmation", next); SetDeleteField(ui, "deleteConfirmationGeneration", 4L);
                typeof(DebugConsoleUi).GetMethod("ResetSaveConfirmation", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(ui, null);
                Assert(!actions.ConfirmSelectedStackDelete(owner, next, 4).Removed && inventory.Calls == 0, "Existing close/title/dispose reset boundary invalidates deletion too.");
            });
        }
        private static void SetDeleteField(object value, string name, object field) => value.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(value, field);
        private static void SelectDeleteItem(DeleteTestInventory inventory, DeleteTestItem item)
        { inventory.Slots[0] = item; DolocAPI.SelectedItemIndex = 0; DolocAPI.SelectedItem = item; }
        private static void WithDeleteActions(Action<DebugConsoleNativeActions, ManifestModel, CropTestContext, DeleteTestInventory> test)
        {
            object? oldArchive = DolocAPI.archiveHandle, oldSelected = DolocAPI.SelectedItem;
            int oldIndex = DolocAPI.SelectedItemIndex; Action<int>? oldAction = DolocAPI.DeleteStackAction;
            var runtime = new DtmApiRuntime(new FakeHost(NewTempGameDir()), new ConfigMenuRegistry());
            var actions = new DebugConsoleNativeActions(new DebugConsoleTestRuntime(runtime));
            var archive = new DeleteTestArchive(); var context = new CropTestContext();
            try { DolocAPI.archiveHandle = archive; actions.SetDeletionContext(context); actions.BeginSaveSession();
                DolocAPI.DeleteStackAction = index => archive.InventorySystem.inventory.Take(index);
                test(actions, new ManifestModel { UniqueID = "DTMAPI.DebugConsoleMod" }, context, archive.InventorySystem.inventory); }
            finally { actions.EndSaveSession("test"); DolocAPI.archiveHandle = oldArchive; DolocAPI.SelectedItem = oldSelected;
                DolocAPI.SelectedItemIndex = oldIndex; DolocAPI.DeleteStackAction = oldAction; }
        }
        private sealed class DeleteTestArchive { public ModeTestFarm farmData = new ModeTestFarm(); public DeleteTestInventorySystem InventorySystem = new DeleteTestInventorySystem(); }
    }
    public class DeleteTestItem { public string name = "wood"; public int count = 10; public string title => name + " title"; }
    public sealed class DeleteTestContainer : DeleteTestItem, DolocTown.IContainer { public DeleteTestInventory inventory { get; set; } = new DeleteTestInventory(); }
    public sealed class DeleteTestInventorySystem { public DeleteTestInventory inventory = new DeleteTestInventory(); }
    public sealed class DeleteTestInventory
    {
        public DeleteTestItem?[] Slots = new DeleteTestItem?[4];
        public int Calls; public bool Unreadable; public Action? Before, After;
        public DeleteTestItem? Read(int index) { if (Unreadable) throw new Exception("read-back"); return index >= 0 && index < Slots.Length ? Slots[index] : null; }
        public DeleteTestItem?[] ReadAllWithNull() => Slots.ToArray();
        public void Take(int index) { Calls++; Before?.Invoke(); Slots[index] = null; After?.Invoke(); }
    }
}
