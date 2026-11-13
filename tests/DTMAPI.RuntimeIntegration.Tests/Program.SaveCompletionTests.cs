using System;
using System.Collections.Generic;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;
using DTMAPI.GameBridge.DolocTown;
using DTMAPI.ModConfigMenu;

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {
        private static void NativeSaveResultControlsSavedEvent()
        {
            string? previousRoot = UseTempPersistentRoot();
            DtmApiRuntime? previousRuntime = DolocTownHookCallbacks.Runtime;
            DolocTownGameBridge? previousBridge = DolocTownHookCallbacks.Bridge;
            DtmApiRuntime? runtime = null;
            DolocTownGameBridge? bridge = null;
            try
            {
                runtime = new DtmApiRuntime(new FakeHost(NewTempGameDir()), new ConfigMenuRegistry());
                bridge = new DolocTownGameBridge(runtime);
                IEventsHelper events = CreateEventsProxy(runtime, "DTMAPI.Tests.SaveCompletion");
                var savedSlots = new List<int?>();
                events.Save.SaveSaved += (_, args) => savedSlots.Add(args.SaveSlot);
                runtime.Start();
                DolocTownHookCallbacks.Runtime = runtime;
                DolocTownHookCallbacks.Bridge = bridge;

                DolocTownHookCallbacks.SaveGamePostfix(11, false);
                Assert(savedSlots.Count == 0, "A failed native save must not publish SaveSaved.");

                DolocTownHookCallbacks.SaveGamePostfix(11, true);
                Assert(savedSlots.Count == 1 && savedSlots[0] == 11,
                    "A successful native save must publish its slot exactly once without a QA host.");

                DolocTownHookCallbacks.SaveGamePostfix(11, false);
                Assert(savedSlots.Count == 1, "A later failed save must not repeat the previous save notification.");
            }
            finally
            {
                try
                {
                    bridge?.Shutdown("save completion test finished");
                    runtime?.NotifyRuntimeShutdown("save completion test finished");
                }
                finally
                {
                    DolocTownHookCallbacks.Runtime = previousRuntime;
                    DolocTownHookCallbacks.Bridge = previousBridge;
                    RestorePersistentRoot(previousRoot);
                }
            }
        }
    }
}
