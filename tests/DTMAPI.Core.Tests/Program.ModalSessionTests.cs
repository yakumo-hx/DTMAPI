using System;
using System.Threading;
using DTMAPI.Abstractions;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;
using DTMAPI.ModConfigMenu;

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {
        private static void ModalSessionsUseRealDispatchAndRejectStaleClosure()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                foreach (string scenario in new[] { "close", "same-id", "foreign", "platform", "deactivate", "throw", "close-throw" })
                {
                    var runtime = new DtmApiRuntime(new FakeHost(NewTempGameDir()), new ConfigMenuRegistry());
                    runtime.Start();
                    var owner = new ManifestModel { UniqueID = "DTMAPI.Tests.ModalA", Name = "Modal A", Version = "1.0.0" };
                    var other = new ManifestModel { UniqueID = "DTMAPI.Tests.ModalB", Name = "Modal B", Version = "1.0.0" };
                    runtime.BeginOwnerEntry(owner.UniqueID);
                    runtime.ActivateOwnerEntry(owner);
                    runtime.BeginOwnerEntry(other.UniqueID);
                    runtime.ActivateOwnerEntry(other);
                    IUiModalSessions Modal(string id) => (IUiModalSessions)runtime.UI.CreateOwnerBound(id,
                        () => runtime.EnsureModHelperRuntimeThread(id), () => runtime.EnsureModOwnerRegistrationAllowed(id));
                    IUiModalSessions a = Modal(owner.UniqueID);
                    IUiModalSessions b = Modal(other.UniqueID);
                    IEventsHelper events = runtime.Events.CreateOwnerBoundProxy(owner.UniqueID,
                        () => runtime.EnsureModOwnerRegistrationAllowed(owner.UniqueID));
                    bool first = true;
                    int inactiveCloseCalls = 0;
                    IUiModalSession? successor = null;
                    events.UI.MenuOpened += (_, _) =>
                    {
                        if (!first) return;
                        first = false;
                        if (scenario == "close") runtime.UI.Close();
                        if (scenario == "same-id")
                        {
                            runtime.UI.Close();
                            successor = a.OpenModal("modal").Session;
                        }
                        if (scenario == "foreign")
                        {
                            Assert(b.OpenModal("modal").Status == DtmModalOpenStatus.Busy,
                                "Another owner cannot acquire by reusing the menu ID.");
                            runtime.UI.Close();
                            successor = b.OpenModal("modal").Session;
                        }
                        if (scenario == "platform") runtime.UI.OpenConfigPage();
                        if (scenario == "deactivate") runtime.DeactivateOwner(owner.UniqueID, ModOwnerCleanupReason.EntryFailed, false);
                        if (scenario == "throw") throw new InvalidOperationException("modal-open-subscriber-failure");
                    };
                    events.UI.MenuClosed += (_, _) =>
                    {
                        inactiveCloseCalls++;
                        if (scenario == "close-throw") throw new InvalidOperationException("modal-close-subscriber-failure");
                    };
                    DtmModalOpenResult result = a.OpenModal("modal");
                    if (scenario == "throw" || scenario == "close-throw")
                    {
                        Assert(result.Status == DtmModalOpenStatus.Opened && result.Session?.IsOpen == true,
                            "Subscriber exceptions are isolated by the real EventManager; the result follows occupancy.");
                        IUiModalSession old = result.Session!;
                        DtmModalOpenResult repeated = a.OpenModal("MODAL");
                        Assert(repeated.Status == DtmModalOpenStatus.AlreadyOpen && repeated.Session!.Sequence == old.Sequence,
                            "Repeated same-owner/menu open aliases the current generation without dispatching another open.");
                        Assert(old.Close() && !old.IsOpen, "Close must report that the exact session was released even if a subscriber throws.");
                        IUiModalSession newer = a.OpenModal("modal").Session!;
                        Assert(newer.Sequence > old.Sequence && !old.Close(), "Old Close cannot release a same-ID successor.");
                        repeated.Session!.Dispose();
                        old.Dispose();
                        old.Dispose();
                        Assert(newer.IsOpen, "Repeated Dispose and aliases of an old session cannot release its successor.");
                        Exception? offThread = null;
                        var thread = new Thread(() => { try { newer.Close(); } catch (Exception ex) { offThread = ex; } });
                        thread.Start(); thread.Join();
                        Assert(offThread is InvalidOperationException && newer.IsOpen, "Off-thread release must fail before mutation.");
                        runtime.DeactivateOwner(owner.UniqueID, ModOwnerCleanupReason.EntryFailed, false);
                        int callbacks = inactiveCloseCalls;
                        newer.Dispose();
                        Assert(!newer.IsOpen && !runtime.UI.IsOpen && inactiveCloseCalls == callbacks && runtime.UI.CountOwnerResources(owner.UniqueID) == 0,
                            "Owner teardown releases occupancy; stale cleanup stays safe and invokes no inactive owner.");
                        AssertThrows(() => a.OpenModal("modal"), "A deactivated helper cannot acquire again.");
                        Assert(runtime.Diagnostics.GetErrors().Count > 0, "The thrown subscriber must remain visible in real diagnostics.");
                    }
                    else
                    {
                        Assert(result.Status == DtmModalOpenStatus.Interrupted && result.Session == null,
                            "Open must return Interrupted without a handle after callback mutation: " + scenario);
                        if (successor != null) Assert(successor.IsOpen, "The reentrant successor must remain owned.");
                        if (scenario == "platform") Assert(runtime.UI.IsOpen && runtime.UI.ActiveMenuOwnerId.Length == 0, "Platform replacement remains the current occupancy.");
                        if (scenario == "deactivate") Assert(!runtime.UI.IsOpen && inactiveCloseCalls == 0, "Teardown must skip the deactivated owner's close subscriber.");
                    }
                    runtime.NotifyRuntimeShutdown("modal test complete");
                }

                // A close callback can create the successor before Close returns.
                var closeRuntime = new DtmApiRuntime(new FakeHost(NewTempGameDir()), new ConfigMenuRegistry());
                closeRuntime.Start();
                var modal = (IUiModalSessions)closeRuntime.UI.CreateOwnerBound("DTMAPI.Tests.Reenter", () => { }, () => { });
                IUiModalSession oldSession = modal.OpenModal("same").Session!;
                IUiModalSession? reopened = null;
                CreateEventsProxy(closeRuntime, "DTMAPI.Tests.Observer").UI.MenuClosed += (_, _) => reopened = modal.OpenModal("same").Session;
                Assert(oldSession.Close() && !oldSession.IsOpen && reopened?.IsOpen == true,
                    "Close returns the released generation, not the final state of a reentrant successor.");
                oldSession.Dispose();
                Assert(reopened!.IsOpen, "Late cleanup cannot revoke the reentrant successor.");
            }
            finally { RestorePersistentRoot(previousRoot); }
        }
    }
}
