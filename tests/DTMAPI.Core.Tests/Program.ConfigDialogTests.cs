using System;
using System.Threading;
using DTMAPI.Abstractions;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;
using DTMAPI.Core.Services;
using DTMAPI.ModConfigMenu;

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {
        private static void ConfigRestartRemindersAggregateSuccessfulSaves()
        {
            var registry = new ConfigMenuRegistry();
            var runtime = (IConfigMenuRuntime)registry;
            var first = new ManifestModel { UniqueID = "Tests.RestartA", Name = "First", Version = "1.0.0" };
            var second = new ManifestModel { UniqueID = "Tests.RestartB", Name = "Second", Version = "1.0.0" };
            bool a = true, b = true, failSave = false;
            var firstApi = (IDtmConfigMenuApi)((IOwnerBoundApiFactory)registry).CreateOwnerBoundApi(typeof(IDtmConfigMenuApi), first, () => { });
            firstApi.Register(first, () => { }, () => { });
            firstApi.AddBoolOption(first, () => "A", () => "", () => a, value => a = value);
            ((IDtmConfigMenuRestartApi)firstApi).RequireRestartForLastOption(first);
            AssertThrows(() => ((IDtmConfigMenuRestartApi)firstApi).RequireRestartForLastOption(second), "Restart declarations cannot cross owners.");
            registry.Register(second, () => { }, () => { if (failSave) throw new Exception("disk failure"); });
            registry.AddBoolOption(second, () => "B", () => "", () => b, value => b = value);
            registry.RequireRestartForLastOption(second);
            runtime.BeginEditing(first.UniqueID); runtime.BeginEditing(second.UniqueID);
            var itemA = runtime.GetPage(first.UniqueID)!.Items[0];
            var itemB = runtime.GetPage(second.UniqueID)!.Items[0];
            itemA.TrySetPendingValue("false", out _); runtime.Cancel(first.UniqueID);
            Assert(!runtime.HasUnnotifiedRestartChanges(), "Draft cancellation is absent from restart state.");
            itemA.TrySetPendingValue("false", out _); runtime.Save(first.UniqueID);
            itemB.TrySetPendingValue("false", out _); failSave = true;
            AssertThrows(() => runtime.Save(second.UniqueID), "Failed persistence rejects restart state.");
            Assert(runtime.GetPendingRestartChanges().Count == 1 && b, "Failed save rolls back values and leaves only the first owner pending.");
            failSave = false; runtime.Save(second.UniqueID);
            Assert(runtime.GetPendingRestartChanges().Count == 2 && runtime.HasUnnotifiedRestartChanges(), "Successful saves combine owner groups before any popup.");
            runtime.AcknowledgeRestartChanges(); runtime.Save(first.UniqueID);
            Assert(!runtime.HasUnnotifiedRestartChanges() && runtime.GetPendingRestartChanges().Count == 2, "Acknowledgement suppresses duplicate reminders without forgetting pending settings.");
            itemA.TrySetPendingValue("true", out _); runtime.Save(first.UniqueID);
            Assert(runtime.GetPendingRestartChanges().Count == 1, "Restoring the startup value removes that pending setting.");
            itemA.TrySetPendingValue("false", out _); runtime.Save(first.UniqueID);
            Assert(runtime.HasUnnotifiedRestartChanges(), "A fresh startup-sensitive change can be reported again.");
            runtime.RemoveOwner(first.UniqueID); runtime.RemoveOwner(second.UniqueID);
            Assert(runtime.GetPendingRestartChanges().Count == 0 && !runtime.HasUnnotifiedRestartChanges(), "Owner removal drops its restart state.");
        }

        private static void ConfigDialogsPreserveParentsAndReleaseOwnerCallbacks()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                var registry = new ConfigMenuRegistry();
                var runtime = new DtmApiRuntime(new FakeHost(NewTempGameDir()), registry);
                runtime.Start();
                var owner = new ManifestModel { UniqueID = "Tests.Dialog", Version = "1.0.0", Name = "Dialog" };
                runtime.BeginOwnerEntry(owner.UniqueID); runtime.ActivateOwnerEntry(owner);
                var api = (IDtmConfigMenuDialogsApi)((IOwnerBoundApiFactory)registry).CreateOwnerBoundApi(typeof(IDtmConfigMenuApi), owner,
                    () => runtime.EnsureModOwnerRegistrationAllowed(owner.UniqueID));
                var request = new DtmConfigDialog("Title", new string('中', 1800), new[] {
                    new DtmConfigDialogButton("disabled", "Unavailable", false),
                    new DtmConfigDialogButton("yes", "Accept"), new DtmConfigDialogButton("cancel", "Cancel") }, "cancel");
                int selections = 0, hidden = 0;
                ConfigMenuDialogService.Session? displayed = null;
                runtime.ConfigDialogs.Show = view => { displayed = view; return true; };
                runtime.ConfigDialogs.Hide = _ => hidden++;
                runtime.UI.OpenConfigPage(owner.UniqueID);
                long parent = runtime.UI.OverlaySessionSequence;
                Assert(api.TryShowDialog(owner, request, _ => selections++, out var first), "Config page can host a child dialog.");
                Assert(!api.TryShowDialog(owner, request, _ => selections++, out _), "Busy dialog must not be replaced or queued.");
                displayed!.Select("disabled");
                Assert(first!.IsOpen && selections == 0, "Disabled actions cannot be invoked through stale/native callbacks.");
                var oldView = displayed;
                displayed.Select("yes"); oldView.Select("yes");
                Assert(selections == 1 && hidden == 1 && !first.IsOpen, "A choice executes exactly once and clears its view.");
                Assert(runtime.UI.IsOpen && runtime.UI.OverlaySessionSequence == parent && runtime.UI.RequestedConfigUniqueId == owner.UniqueID,
                    "Closing a child preserves the original config page/session.");
                Assert(api.TryShowDialog(owner, request, _ => selections++, out var second), "A dismissed dialog can be reopened.");
                first.Dispose(); oldView.Select("yes");
                Assert(second!.IsOpen && selections == 1, "Old handles and buttons cannot affect a successor.");
                Exception? wrongThread = null;
                var thread = new Thread(() => { try { second.Close(); } catch (Exception ex) { wrongThread = ex; } });
                thread.Start(); thread.Join();
                Assert(wrongThread is InvalidOperationException && second.IsOpen, "Off-thread close is rejected before mutation.");
                runtime.DeactivateOwner(owner.UniqueID, ModOwnerCleanupReason.Unload, false, "");
                Assert(!second.IsOpen && runtime.ConfigDialogs.CountOwnerResources(owner.UniqueID) == 0 && selections == 1,
                    "Owner removal clears dialogs even when the owner has no registered page, without selecting an action.");
                Assert(runtime.UI.IsOpen, "Removing a child owner does not close the platform parent.");
                AssertThrows(() => api.TryShowDialog(owner, request, _ => { }, out _), "Inactive facade cannot open another dialog.");
                runtime.NotifyRuntimeShutdown("dialog test complete");
            }
            finally { RestorePersistentRoot(previousRoot); }
        }

        private static void ConfigDialogsHandleReentryFailureAndBoundaries()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                foreach (string scenario in new[] { "success", "render-false", "render-throw", "reenter", "deactivate", "replace-parent", "title" })
                {
                    var registry = new ConfigMenuRegistry();
                    var runtime = new DtmApiRuntime(new FakeHost(NewTempGameDir()), registry);
                    runtime.Start();
                    var owner = new ManifestModel { UniqueID = "Tests.Dialog", Version = "1.0.0" };
                    runtime.BeginOwnerEntry(owner.UniqueID); runtime.ActivateOwnerEntry(owner);
                    var dialog = new DtmConfigDialog("Title", "Body", new[] { new DtmConfigDialogButton("ok", "OK") }, "ok");
                    runtime.ConfigDialogs.Show = _ => scenario == "render-throw" ? throw new Exception("render failure") : scenario != "render-false";
                    bool eventFired = false;
                    runtime.Events.CreateProxy("Tests.Observer").UI.MenuOpened += (_, _) =>
                    {
                        if (eventFired) return;
                        eventFired = true;
                        if (scenario == "reenter") Assert(!registry.TryShowDialog(owner, dialog, _ => { }, out _), "Opening reservation rejects synchronous reentry.");
                        if (scenario == "deactivate") runtime.DeactivateOwner(owner.UniqueID, ModOwnerCleanupReason.Unload, false, "");
                    };
                    int selected = 0;
                    bool opened = registry.TryShowDialog(owner, dialog, _ => selected++, out var session);
                    if (scenario == "render-false" || scenario == "render-throw" || scenario == "deactivate")
                        Assert(!opened && session == null && runtime.ConfigDialogs.Current == null && !runtime.UI.IsOpen, "Failed/interrupted opens release both view and occupancy: " + scenario);
                    else
                    {
                        Assert(opened && session!.IsOpen, "Standalone dialog acquires existing Core occupancy: " + scenario);
                        if (scenario == "replace-parent")
                        {
                            runtime.UI.OpenConfigPage(); runtime.ConfigDialogs.Validate();
                            Assert(!session!.IsOpen && runtime.UI.IsOpen && runtime.UI.ActiveMenuOwnerId.Length == 0, "Platform replacement closes only the stale dialog.");
                        }
                        else if (scenario == "title")
                        {
                            runtime.NotifyReturnedToTitle();
                            Assert(!session!.IsOpen && runtime.ConfigDialogs.Current == null && selected == 0, "Title boundary closes without invoking gameplay actions.");
                        }
                        else
                        {
                            runtime.ConfigDialogs.Current!.Select("ok");
                            Assert(selected == 1 && !runtime.UI.IsOpen && !session!.Close(), "Standalone selection releases exact occupancy once.");
                        }
                    }
                    runtime.NotifyRuntimeShutdown("dialog test complete");
                }
            }
            finally { RestorePersistentRoot(previousRoot); }
        }
    }
}
