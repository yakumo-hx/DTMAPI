using System;
using System.Collections.Generic;
using System.Linq;
using DTMAPI.Core.Runtime;

namespace DTMAPI.UnitTests
{
    internal static class AudioE21DeferredHooksTests
    {
        public static void Run()
        {
            var inspector = new Inspector(); var supervisor = new AdvancedHarmonySupervisor(inspector);
            foreach (string owner in new[] { "DTMAPI.Audio", "DTMAPI.Animals" })
            {
                supervisor.RegisterRuntimeModule(owner);
                var method = Batch6AdvancedRuntimeTests.MemoryPatch(owner);
                var patch = new AdvancedHarmonyPatchRecord(owner + ".Native", owner + ".Target", "prefix", "Run", method.Module.ModuleVersionId.ToString("D"), typeof(object).GetMethod("ToString"), method);
                for (int i = 0; i < 3; i++)
                {
                    supervisor.ActivateInternalModule(owner, method.Module.Assembly, () => { inspector.Patches.Add(patch); return new object(); });
                    if (supervisor.AuditActiveOwners().Count != 0) throw new Exception("Deferred module hook lost Provider attribution.");
                    inspector.Patches.Remove(patch);
                }
                try
                {
                    supervisor.ActivateInternalModule(owner, method.Module.Assembly, () => { inspector.Patches.Add(patch); throw new InvalidOperationException("partial install"); });
                }
                catch (InvalidOperationException) { }
                if (inspector.Patches.Count != 0) throw new Exception("Failed module activation did not compensate its new native patches.");
            }
            Console.WriteLine("Typed module supervision passed: independent Providers, deferred reinstall and failed-install compensation.");
        }
        private sealed class Inspector : IAdvancedHarmonyInspector
        {
            internal readonly List<AdvancedHarmonyPatchRecord> Patches = new List<AdvancedHarmonyPatchRecord>();
            public AdvancedHarmonySnapshot Capture() => new AdvancedHarmonySnapshot(true, Patches.ToArray(), "");
            public AdvancedHarmonyUnpatchResult UnpatchPatches(IReadOnlyList<AdvancedHarmonyPatchRecord> patches)
            { int removed = Patches.RemoveAll(p => patches.Contains(p)); return new AdvancedHarmonyUnpatchResult(true, removed, 0, "offline"); }
        }
    }
}
