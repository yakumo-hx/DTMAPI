using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using DTMAPI.Core.Runtime;
using DTMAPI.GameBridge.DolocTown;
using HarmonyLib;

namespace DTMAPI.DebugConsoleHarmonyOwnerFixture
{
    internal static partial class Program
    {
        private static int platformFinalizerCalls;
        private static Action<MethodBase>? cleanupRebuildAction;

        private static object CreatePatchCleanup() => Activator.CreateInstance(
            typeof(DolocTownGameBridge).Assembly.GetType("DTMAPI.GameBridge.DolocTown.HarmonyModPatchCleanup", true)!, true)!;

        private static void PlatformPatcherCallsHarmonyDirectly()
        {
            const string owner = "Test.PlatformPatcher", foreign = "Test.PlatformPatcher.Foreign";
            MethodInfo target = typeof(Program).GetMethod(nameof(PlatformPatchTarget), BindingFlags.Static | BindingFlags.NonPublic)!;
            MethodInfo prefix = typeof(Program).GetMethod(nameof(PlatformPrefix), BindingFlags.Static | BindingFlags.NonPublic)!;
            MethodInfo postfix = typeof(Program).GetMethod(nameof(PlatformPostfix), BindingFlags.Static | BindingFlags.NonPublic)!;
            MethodInfo finalizer = typeof(Program).GetMethod(nameof(PlatformFinalizer), BindingFlags.Static | BindingFlags.NonPublic)!;
            Assembly bridge = typeof(DolocTownGameBridge).Assembly;
            Type patcherType = bridge.GetType("DTMAPI.GameBridge.DolocTown.HarmonyPatcher", true)!;
            Type signatureType = bridge.GetType("DTMAPI.GameBridge.DolocTown.HarmonyTargetSignature", true)!;
            object signature = signatureType.GetMethod("Exact", new[] { typeof(Type), typeof(Type), typeof(Type[]) })!
                .Invoke(null, new object[] { typeof(Program), typeof(int), new[] { typeof(int) } })!;
            ConstructorInfo constructor = patcherType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null, new[] { typeof(DtmApiRuntime), typeof(string) }, modifiers: null)!;
            var runtime = CreateRuntime();
            try
            {
                new Harmony(foreign).Patch(target, prefix: new HarmonyMethod(prefix));
                for (int attempt = 0; attempt < 2; attempt++)
                {
                    object patcher = constructor.Invoke(new object[] { runtime, owner });
                    foreach (var patch in new[] { ("TryPatchPrefix", prefix), ("TryPatchPostfix", postfix), ("TryPatchFinalizer", finalizer) })
                    {
                        MethodInfo install = patcherType.GetMethod(patch.Item1,
                            new[] { typeof(string), typeof(string), typeof(MethodInfo), signatureType })!;
                        bool installed = (bool)install.Invoke(patcher, new[] { (object)typeof(Program).AssemblyQualifiedName!, target.Name, patch.Item2, signature })!;
                        Assert(installed, "GameBridge failed to install " + patch.Item1 + ".");
                    }
                }
                Patches info = Harmony.GetPatchInfo(target)!;
                Assert(info.Prefixes.Count(p => p.owner == owner && p.PatchMethod == prefix) == 1 &&
                    info.Postfixes.Count(p => p.owner == owner && p.PatchMethod == postfix) == 1 &&
                    info.Finalizers.Count(p => p.owner == owner && p.PatchMethod == finalizer) == 1 &&
                    info.Prefixes.Count(p => p.owner == foreign && p.PatchMethod == prefix) == 1,
                    "Repeated platform installs duplicated a patch or removed another owner's contribution.");
                platformFinalizerCalls = 0;
                Assert(PlatformPatchTarget(3) == 10 && PlatformPatchTarget(-5) == 17 && platformFinalizerCalls == 2,
                    "The actual target did not run both owners' prefixes, the postfix and the exception finalizer.");
            }
            finally
            {
                Harmony.UnpatchID(owner);
                Harmony.UnpatchID(foreign);
            }
            Assert(PlatformPatchTarget(3) == 3, "Removing the test patches did not restore the target.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int PlatformPatchTarget(int value)
        {
            if (value < 0) throw new InvalidOperationException("target failed");
            return value;
        }

        private static void PlatformPrefix(ref int value) { value++; }
        private static void PlatformPostfix(ref int __result) { __result *= 2; }
        private static Exception? PlatformFinalizer(Exception? __exception, ref int __result)
        {
            platformFinalizerCalls++;
            if (__exception != null) __result = 17;
            return null;
        }

        private static void PlatformCleanupUsesPatchAssemblies()
        {
            const string owner = "Test.Cleanup", wrongOwner = "Test.Cleanup.OtherName", sharedOwner = "Test.Cleanup.Shared", platformOwner = "Test.Cleanup.Platform";
            MethodInfo target = typeof(Program).GetMethod(nameof(CleanupPatchTarget), BindingFlags.Static | BindingFlags.NonPublic)!;
            MethodInfo owned = CreateMemoryPatch("Owned");
            MethodInfo sibling = CreateMemoryPatch("Sibling");
            MethodInfo shared = CreateMemoryPatch("Shared");
            MethodInfo platform = CreateMemoryPatch("Platform");
            object cleanup = CreatePatchCleanup();
            try
            {
                var harmony = new Harmony(owner);
                harmony.Patch(target, prefix: new HarmonyMethod(owned), postfix: new HarmonyMethod(owned), finalizer: new HarmonyMethod(owned));
                harmony.Patch(target, prefix: new HarmonyMethod(owned));
                harmony.Patch(target, prefix: new HarmonyMethod(sibling));
                new Harmony(wrongOwner).Patch(target, prefix: new HarmonyMethod(owned));
                new Harmony(sharedOwner).Patch(target, prefix: new HarmonyMethod(shared));
                new Harmony(platformOwner).Patch(target, prefix: new HarmonyMethod(platform));
                int ownedCount = ReadFixturePatches(target).Count(p => p.PatchMethod == owned);
                object result = Invoke(cleanup, "RemovePatches", new[] { owned.Module.Assembly }, new[] { shared.Module.Assembly }, owner)!;
                Assert(CleanupResultCount(result, "RemovedResources") == ownedCount &&
                    CleanupResultCount(result, "RemainingResources") == 1 && CleanupResultCount(result, "FailureCount") == 1,
                    "Exclusive patches were not removed or a shared patch was not reported as retained.");
                var retained = ReadFixturePatches(target).ToArray();
                Assert(retained.Length == 3 && retained.All(p => p.PatchMethod != owned) &&
                    retained.Any(p => p.PatchMethod == sibling) && retained.Any(p => p.PatchMethod == shared) &&
                    retained.Any(p => p.PatchMethod == platform) && CleanupPatchTarget() == 9,
                    "Cleanup removed an unrelated assembly's patch or left an attributed method installed.");
                result = Invoke(cleanup, "RemovePatches", new[] { shared.Module.Assembly }, Array.Empty<Assembly>(), platformOwner)!;
                Assert(CleanupResultCount(result, "RemovedResources") == 1 && CleanupResultCount(result, "RemainingResources") == 0 &&
                    CleanupResultCount(result, "FailureCount") == 0 && ReadFixturePatches(target).Count() == 2,
                    "Cleanup followed the owner hint instead of the exclusive assembly binding.");
            }
            finally
            {
                foreach (string id in new[] { owner, wrongOwner, sharedOwner, platformOwner }) Harmony.UnpatchID(id);
            }
        }

        private static int CleanupResultCount(object result, string name) => (int)result.GetType().GetProperty(name)!.GetValue(result)!;

        private static void PlatformCleanupPreservesReentrantContributions()
        {
            const string owner = "Test.ReentrantCleanup", observer = "Test.ReentrantCleanup.Observer", late = "Test.ReentrantCleanup.Late";
            MethodInfo firstTarget = typeof(Program).GetMethod(nameof(ReentrantCleanupFirstTarget), BindingFlags.Static | BindingFlags.NonPublic)!;
            MethodInfo secondTarget = typeof(Program).GetMethod(nameof(ReentrantCleanupSecondTarget), BindingFlags.Static | BindingFlags.NonPublic)!;
            MethodInfo transpiler = typeof(Program).GetMethod(nameof(CleanupRebuildTranspiler), BindingFlags.Static | BindingFlags.NonPublic)!;
            foreach (bool reinstallEarlier in new[] { false, true })
            {
                MethodInfo owned = CreateMemoryPatch("Reentrant");
                try
                {
                    foreach (MethodInfo target in new[] { firstTarget, secondTarget })
                    {
                        new Harmony(owner).Patch(target, prefix: new HarmonyMethod(owned));
                        new Harmony(observer).Patch(target, transpiler: new HarmonyMethod(transpiler));
                    }
                    MethodBase[] order = Harmony.GetAllPatchedMethods()
                        .Where(target => Equals(target, firstTarget) || Equals(target, secondTarget)).ToArray();
                    Assert(order.Length == 2, "Both cleanup targets must be installed.");
                    MethodBase trigger = reinstallEarlier ? order[1] : order[0];
                    MethodBase changed = reinstallEarlier ? order[0] : order[1];
                    int callbacks = 0;
                    cleanupRebuildAction = original =>
                    {
                        if (!Equals(original, trigger)) return;
                        cleanupRebuildAction = null;
                        callbacks++;
                        if (reinstallEarlier)
                            new Harmony(owner).Patch(changed, prefix: new HarmonyMethod(owned));
                        else
                            new Harmony(late).Patch(changed, postfix: new HarmonyMethod(owned));
                    };
                    object result = Invoke(CreatePatchCleanup(), "RemovePatches", new[] { owned.Module.Assembly }, Array.Empty<Assembly>(), owner)!;
                    Assert(callbacks == 1 && CleanupResultCount(result, "RemovedResources") == 1 &&
                        CleanupResultCount(result, "RemainingResources") == 1 && CleanupResultCount(result, "FailureCount") == 1,
                        "Cleanup failed to report an initial contribution retained or reinstalled during another target's rebuild.");
                    Patches retained = Harmony.GetPatchInfo(changed)!;
                    Assert(retained.Prefixes.Any(p => p.owner == owner && p.PatchMethod == owned),
                        "The original contribution should remain installed after the reentrant callback.");
                    if (!reinstallEarlier)
                        Assert(retained.Postfixes.Any(p => p.owner == late && p.PatchMethod == owned),
                            "Method-specific cleanup deleted a contribution created after cleanup began.");
                }
                finally
                {
                    cleanupRebuildAction = null;
                    foreach (string id in new[] { owner, observer, late }) Harmony.UnpatchID(id);
                }
            }
        }

        private static IEnumerable<CodeInstruction> CleanupRebuildTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            cleanupRebuildAction?.Invoke(original);
            return instructions;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int ReentrantCleanupFirstTarget() => 1;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int ReentrantCleanupSecondTarget() => 2;

        private static IEnumerable<Patch> ReadFixturePatches(MethodBase target)
        {
            Patches? info = Harmony.GetPatchInfo(target);
            return info == null ? Enumerable.Empty<Patch>() : info.Prefixes.Concat(info.Postfixes).Concat(info.Transpilers).Concat(info.Finalizers).Concat(info.ILManipulators);
        }

        private static MethodInfo CreateMemoryPatch(string name)
        {
            var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(name + Guid.NewGuid().ToString("N")), AssemblyBuilderAccess.Run);
            TypeBuilder type = assembly.DefineDynamicModule(name).DefineType(name, TypeAttributes.Public);
            MethodBuilder method = type.DefineMethod("Patch", MethodAttributes.Public | MethodAttributes.Static, typeof(void), Type.EmptyTypes);
            method.GetILGenerator().Emit(OpCodes.Ret);
            return type.CreateType()!.GetMethod("Patch")!;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int CleanupPatchTarget() => 9;
    }
}
