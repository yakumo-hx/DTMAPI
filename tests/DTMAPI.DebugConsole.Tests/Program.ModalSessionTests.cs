using System;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;
using DTMAPI.DebugConsole;
using DTMAPI.ModConfigMenu;

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {
        private static void DebugConsoleModalIdentityKeepsReentryAndRestoreRetry()
        {
            var runtime = new DtmApiRuntime(new FakeHost(NewTempGameDir()), new ConfigMenuRegistry());
            var adapter = new DebugConsoleTestRuntime(runtime);
            var owner = DebugConsoleUnitManifest();
            var ui = new DebugConsoleUi(adapter);
            ui.Bind(owner, null, null, null, null, null);
            try
            {
                ui.Open(owner, "button");
                IUiModalSession old = GetPrivateField<IUiModalSession>(ui, "modalSession");
                bool reopen = true;
                runtime.Events.CreateProxy("DTMAPI.Tests.ModalObserver").UI.MenuClosed += (_, _) =>
                {
                    if (!reopen) return;
                    reopen = false;
                    ui.Open(owner, "reentrant close callback");
                };
                ui.Close(owner, "button");
                IUiModalSession newer = GetPrivateField<IUiModalSession>(ui, "modalSession");
                Assert(ui.IsOpen && newer.IsOpen && newer.Sequence != old.Sequence,
                    "Close must release the old presentation before a callback opens its successor.");
                old.Dispose(); old.Dispose();
                Assert(newer.IsOpen && ui.IsOpen && DebugConsoleInputGate.ModalOpen,
                    "Late platform handle disposal cannot clear the new product's gates or restore its effects.");
                runtime.UI.OpenConfigPage();
                ui.Update();
                Assert(!ui.IsOpen && !newer.IsOpen && runtime.UI.IsOpen && runtime.UI.ActiveMenuId == "DTMAPI.Config",
                    "Platform replacement cleans the old presentation without closing the platform page.");
                runtime.UI.Close();
                ui.Open(owner, "button");
                IUiModalSession last = GetPrivateField<IUiModalSession>(ui, "modalSession");
                ui.ResetForTitleBoundary();
                Assert(!ui.IsOpen && !last.IsOpen && !DebugConsoleInputGate.ModalOpen && !DebugConsoleInputGate.NativeInputDrainActive,
                    "Title cleanup must release the current claim and both native gates.");
            }
            finally { ui.Shutdown("test cleanup"); DebugConsoleInputGate.Reset(); }
        }
    }
}
