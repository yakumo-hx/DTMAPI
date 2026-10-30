using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DTMAPI.Abstractions;
using HarmonyLib;

namespace DTMAPI.Mine
{
    internal sealed class MineHookInstaller
    {
        private readonly IMonitor monitor;
        private readonly Func<int, bool>? installGate;
        private readonly Action? unpatchOverride;
        private readonly List<MethodBase> targets =
            new List<MethodBase>(
                MineProductContract.ExpectedHookCount);
        private Harmony? harmony;

        internal MineHookInstaller(
            IMonitor monitor,
            Func<int, bool>? installGate = null,
            Action? unpatchOverride = null)
        {
            this.monitor = monitor ??
                throw new ArgumentNullException(nameof(monitor));
            this.installGate = installGate;
            this.unpatchOverride = unpatchOverride;
        }

        internal int InstalledPatchCount { get; private set; }

        internal void InstallAtomically(
            MineNativeRuntime runtime)
        {
            if (InstalledPatchCount ==
                MineProductContract.ExpectedHookCount)
            {
                return;
            }

            ResolveAllTargets();
            ThrowIfOwnerCannotInstall();
            harmony = new Harmony(
                MineProductContract.HarmonyOwner);
            MineCallbacks.Attach(runtime);
            try
            {
                PatchWithGate(
                    1,
                    targets[0],
                    nameof(MineCallbacks
                        .EquipmentRendererOnReusePostfix));
                PatchWithGate(
                    2,
                    targets[1],
                    nameof(MineCallbacks
                        .EquipmentBuilderCreateIndicatorPostfix));
                PatchWithGate(
                    3,
                    targets[2],
                    nameof(MineCallbacks
                        .EquipmentBuilderTurnIndicatorPostfix));
                PublishObservedState();
                if (InstalledPatchCount !=
                    MineProductContract.ExpectedHookCount)
                {
                    throw new InvalidOperationException(
                        "Mine could not prove all three exact-owner visual hooks.");
                }
                monitor.Log(
                    "Mine ProductNative visual hooks installed owner=" +
                    MineProductContract.HarmonyOwner +
                    " count=" + InstalledPatchCount + ".");
            }
            catch (Exception installFailure)
            {
                var failures =
                    new List<Exception> { installFailure };
                try
                {
                    UnpatchExactOwner();
                }
                catch (Exception cleanupFailure)
                {
                    failures.Add(cleanupFailure);
                }
                PublishObservedState();
                MineCallbacks.Detach(runtime);
                if (InstalledPatchCount != 0)
                {
                    failures.Add(
                        new InvalidOperationException(
                            "Mine hook rollback left exact-owner residue."));
                }
                if (failures.Count == 1)
                    throw;
                throw new AggregateException(
                    "Mine atomic hook install failed and rollback was incomplete.",
                    failures);
            }
        }

        internal void UnpatchOwnedHooks(
            MineNativeRuntime runtime)
        {
            Exception? failure = null;
            try
            {
                UnpatchExactOwner();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
            finally
            {
                PublishObservedState();
                MineCallbacks.Detach(runtime);
            }

            if (InstalledPatchCount != 0)
            {
                var residue =
                    new InvalidOperationException(
                        "Mine exact-owner unpatch left visual hook residue.");
                if (failure != null)
                    throw new AggregateException(failure, residue);
                throw residue;
            }
            if (failure != null)
                throw failure;
            harmony = null;
        }

        private void ResolveAllTargets()
        {
            targets.Clear();
            targets.Add(
                ResolveInstanceMethod(
                    "DolocTown.EquipmentRenderer",
                    "OnReuse"));
            targets.Add(
                ResolveInstanceMethod(
                    "DolocTown.EquipmentBuilder",
                    "CreateIndicator"));
            targets.Add(
                ResolveInstanceMethod(
                    "DolocTown.EquipmentBuilder",
                    "TurnIndicator"));
        }

        private static MethodInfo ResolveInstanceMethod(
            string typeName,
            string methodName)
        {
            Type type =
                MineNativeRuntime.ResolveType(
                    typeName + ", Assembly-CSharp") ??
                throw new TypeLoadException(typeName);
            return type.GetMethods(
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance)
                .SingleOrDefault(method =>
                    method.Name == methodName &&
                    method.GetParameters().Length == 0) ??
                throw new MissingMethodException(
                    typeName,
                    methodName + "()");
        }

        private void ThrowIfOwnerCannotInstall()
        {
            foreach (MethodBase target in targets)
            {
                Patches? info =
                    Harmony.GetPatchInfo(target);
                if (info == null)
                    continue;
                int ownCount =
                    CountOwnerPatches(info);
                if (ownCount > 0)
                {
                    throw new InvalidOperationException(
                        "Mine exact owner already exists on " +
                        Describe(target) + ".");
                }
            }
        }

        private void PatchWithGate(
            int index,
            MethodBase target,
            string callbackName)
        {
            if (installGate != null &&
                !installGate(index))
            {
                throw new InvalidOperationException(
                    "Injected Mine hook installation failure at step " +
                    index + ".");
            }
            MethodInfo callback =
                typeof(MineCallbacks).GetMethod(
                    callbackName,
                    BindingFlags.Public |
                    BindingFlags.Static) ??
                throw new MissingMethodException(
                    typeof(MineCallbacks).FullName,
                    callbackName);
            harmony!.Patch(
                target,
                postfix: new HarmonyMethod(callback));
        }

        private void UnpatchExactOwner()
        {
            if (unpatchOverride != null)
            {
                unpatchOverride();
                return;
            }
            var owner =
                new Harmony(MineProductContract.HarmonyOwner);
            foreach (MethodBase target in targets)
            {
                owner.Unpatch(
                    target,
                    HarmonyPatchType.All,
                    MineProductContract.HarmonyOwner);
            }
        }

        private void PublishObservedState()
        {
            int count = 0;
            foreach (MethodBase target in targets)
            {
                Patches? info =
                    Harmony.GetPatchInfo(target);
                if (info != null)
                    count += CountOwnerPatches(info);
            }
            InstalledPatchCount = count;
        }

        private static int CountOwnerPatches(
            Patches patches) =>
            patches.Prefixes.Count(p =>
                p.owner ==
                MineProductContract.HarmonyOwner) +
            patches.Postfixes.Count(p =>
                p.owner ==
                MineProductContract.HarmonyOwner) +
            patches.Transpilers.Count(p =>
                p.owner ==
                MineProductContract.HarmonyOwner) +
            patches.Finalizers.Count(p =>
                p.owner ==
                MineProductContract.HarmonyOwner);

        private static string Describe(
            MethodBase method) =>
            (method.DeclaringType?.FullName ?? "?") +
            "." + method.Name;
    }
}
