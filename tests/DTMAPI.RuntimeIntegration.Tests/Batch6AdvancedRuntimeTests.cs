using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using DTMAPI.Abstractions;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;

namespace DTMAPI.UnitTests
{
    // The historical entrypoint now tests the unified CodeMod ownership boundary.
    internal static class Batch6AdvancedRuntimeTests
    {
        public static void RunAll() => RunHarmonySupervision();
        public static void RunHarmonySupervision()
        {
            const string id = "Test.OptionalHooks", owner = "Test.OptionalHooks.Native";
            var manifest = new ManifestModel { ManifestVersion = 2, UniqueID = id, Type = "CodeMod", HarmonyOwner = owner };
            var classification = ManagedModClassifier.ClassifyDeclaredIdentity(manifest);
            MethodInfo ownedPatch = MemoryPatch("Owner");
            MethodInfo otherPatch = MemoryPatch("Sibling");
            var assembly = ownedPatch.Module.Assembly;
            string mvid = assembly.ManifestModule.ModuleVersionId.ToString("D");
            AdvancedHarmonyPatchRecord Patch(string who, string target, MethodInfo? method = null) =>
                new AdvancedHarmonyPatchRecord(who, target, "prefix", "patch:" + target, mvid, typeof(object).GetMethod("ToString"), method ?? ownedPatch);
            var inspector = new Inspector(); var supervisor = new AdvancedHarmonySupervisor(inspector);
            supervisor.BeginEntry(id, classification, assembly); supervisor.CompleteEntry(id);
            inspector.Patches.Add(Patch(owner, "Late.Target"));
            Assert(supervisor.AuditActiveOwners().Count == 0, "A valid hook installed after Entry remains valid.");
            inspector.Available = false;
            Assert(supervisor.AuditActiveOwners().Single().Code == "harmony-not-checked", "Unavailable inspection reports not checked without accusing an owner.");
            inspector.Available = true;
            inspector.Patches.Add(Patch(owner, "Other.Target", otherPatch));
            var cleanup = supervisor.RemoveOwner(id, ModOwnerCleanupReason.Unload);
            Assert(cleanup.RemainingResources == 0 && inspector.Patches.Single().Patch == otherPatch, "Same Harmony ID does not grant ownership of another assembly's patch.");
            var unavailable = new Inspector { Available = false }; var noInspector = new AdvancedHarmonySupervisor(unavailable);
            noInspector.BeginEntry(id, classification, assembly); noInspector.CompleteEntry(id);
            Assert(noInspector.RemoveOwner(id, ModOwnerCleanupReason.Unload).FailureCount != 0, "Unverifiable cleanup stays explicit; it does not prevent entry.");
            foreach (bool duplicate in new[] { false, true })
            {
                var broken = new Inspector(); var bounded = new AdvancedHarmonySupervisor(broken);
                bounded.BeginEntry(id, classification, assembly);
                broken.Patches.Add(Patch(duplicate ? owner : "Wrong.Owner", "Target"));
                if (duplicate) broken.Patches.Add(Patch(owner, "Target"));
                bounded.CompleteEntry(id);
                Assert(broken.Patches.Count == (duplicate ? 2 : 1), "Harmony grouping names and repeated patch records do not create Entry permission checks.");
                var removed = bounded.RemoveOwner(id, ModOwnerCleanupReason.EntryFailed);
                Assert(removed.RemainingResources == 0 && broken.Patches.Count == 0, "Patch methods attributed to the entry are cleaned regardless of their Harmony grouping name.");
            }
            Console.WriteLine("Unified CodeMod ownership: PASS (late hooks, unavailable inspector, wrong owner, duplicates, scoped cleanup).");
        }
        internal static MethodInfo MemoryPatch(string name)
        {
            var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(name + Guid.NewGuid().ToString("N")), AssemblyBuilderAccess.Run);
            var type = assembly.DefineDynamicModule(name).DefineType(name, TypeAttributes.Public);
            var method = type.DefineMethod("Patch", MethodAttributes.Public | MethodAttributes.Static, typeof(void), Type.EmptyTypes);
            method.GetILGenerator().Emit(OpCodes.Ret);
            return type.CreateType()!.GetMethod("Patch")!;
        }
        private static void Assert(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        private sealed class Inspector : IAdvancedHarmonyInspector
        {
            internal bool Available = true;
            internal readonly List<AdvancedHarmonyPatchRecord> Patches = new List<AdvancedHarmonyPatchRecord>();
            public AdvancedHarmonySnapshot Capture() => Available ? new AdvancedHarmonySnapshot(true, Patches.ToArray(), "") : AdvancedHarmonySnapshot.Unavailable("test unavailable");
            public AdvancedHarmonyUnpatchResult UnpatchPatches(IReadOnlyList<AdvancedHarmonyPatchRecord> patches)
            { int count = Available ? Patches.RemoveAll(patch => patches.Contains(patch)) : 0; return new AdvancedHarmonyUnpatchResult(Available, count, Available ? 0 : patches.Count, "test method cleanup"); }
        }
    }
}
