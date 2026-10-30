using System;
using System.Collections.Generic;
using System.Reflection;
using global::DTMAPI.Abstractions;
using HarmonyLib;

namespace DTMAPI.ChestLocatorEnhancer
{
    internal sealed class ChestLocatorEnhancerHookInstaller
    {
        internal const string HarmonyOwner =
            ChestLocatorHookOwnership.HarmonyOwner;
        internal const string CompatibilityOwner =
            "dtmapi.gamebridge.doloctown";
        private readonly IMonitor monitor;
        private Harmony? harmony;
        private MethodInfo? target;
        private readonly List<MethodBase> ownedTargets = new List<MethodBase>();

        internal ChestLocatorEnhancerHookInstaller(IMonitor monitor) =>
            this.monitor =
                monitor ??
                throw new ArgumentNullException(nameof(monitor));

        internal bool IsInstalled { get; private set; }

        internal int InstalledPatchCount { get; private set; }

        internal MethodInfo Target => target ?? ResolveTarget();

        internal void InstallAtomically(
            ChestLocatorEnhancerNativeRuntime runtime)
        {
            if (runtime == null)
                throw new ArgumentNullException(nameof(runtime));
            if (IsInstalled)
                return;

            target = ResolveTarget();
            ThrowIfConflictingOwnerIsPresent(target);
            ChestLocatorUiRefresh.Patch[] uiPatches = ChestLocatorUiRefresh.ResolvePatches();
            ownedTargets.Clear();
            ownedTargets.Add(target);
            foreach (ChestLocatorUiRefresh.Patch patch in uiPatches)
            {
                if (HasExactOwner(patch.Target, HarmonyOwner))
                    throw new InvalidOperationException("ChestLocatorEnhancer refuses a residual product UI patch: " + patch.Target);
                if (!ownedTargets.Contains(patch.Target)) ownedTargets.Add(patch.Target);
            }
            MethodInfo genericCallback =
                typeof(ChestLocatorEnhancerCallbacks).GetMethod(
                    nameof(ChestLocatorEnhancerCallbacks
                        .ArchiveDataHandleGetAvailableInventoriesPostfix),
                    BindingFlags.Public | BindingFlags.Static)
                ?? throw new MissingMethodException(
                    typeof(ChestLocatorEnhancerCallbacks).FullName,
                    nameof(ChestLocatorEnhancerCallbacks
                        .ArchiveDataHandleGetAvailableInventoriesPostfix));
            ParameterInfo[] targetParameters = target.GetParameters();
            Type? inventoryType = target.ReturnType.GetElementType();
            if (targetParameters.Length != 3 ||
                targetParameters[2].ParameterType != typeof(bool) ||
                inventoryType == null)
            {
                throw new MissingMethodException(
                    target.DeclaringType?.FullName,
                    "GetAvailableInventories(Vector2Int, Vector2Int, bool) -> LinearInventory[]");
            }

            MethodInfo callback =
                genericCallback.MakeGenericMethod(
                    inventoryType);
            harmony = new Harmony(HarmonyOwner);
            ChestLocatorEnhancerCallbacks.Attach(runtime);
            try
            {
                harmony.Patch(
                    target,
                    postfix: new HarmonyMethod(callback));
                foreach (ChestLocatorUiRefresh.Patch patch in uiPatches)
                {
                    harmony.Patch(patch.Target,
                        prefix: patch.Prefix ? new HarmonyMethod(patch.Callback) : null,
                        postfix: !patch.Prefix && !patch.Finalizer ? new HarmonyMethod(patch.Callback) : null,
                        finalizer: patch.Finalizer ? new HarmonyMethod(patch.Callback) : null);
                }
                if (!HasExactOwner(target, HarmonyOwner))
                {
                    throw new InvalidOperationException(
                        "ChestLocatorEnhancer could not prove its exact Harmony owner on the native inventory target.");
                }

                PublishObservedOwnerState(target);
                if (InstalledPatchCount != 1 + uiPatches.Length)
                    throw new InvalidOperationException("ChestLocatorEnhancer could not prove every selected UI patch.");
                monitor.Log(
                    "ChestLocatorEnhancer product-owned patch installed owner=" +
                    HarmonyOwner +
                    " count=" +
                    InstalledPatchCount +
                    ".");
            }
            catch (Exception installFailure)
            {
                ChestLocatorEnhancerCallbacks.Detach(runtime);
                try
                {
                    UnpatchOwnedHooks();
                }
                catch (Exception cleanupFailure)
                {
                    throw new AggregateException(
                        "ChestLocatorEnhancer atomic Hook installation failed and exact-owner rollback also failed.",
                        installFailure,
                        cleanupFailure);
                }
                throw;
            }
        }

        internal void UnpatchOwnedHooks()
        {
            if (harmony == null || target == null)
            {
                InstalledPatchCount = 0;
                IsInstalled = false;
                return;
            }

            try
            {
                UnpatchExactOwner(harmony);
            }
            catch
            {
                PublishObservedOwnerState(target);
                throw;
            }

            PublishObservedOwnerState(target);
            if (IsInstalled)
            {
                throw new InvalidOperationException(
                    "ChestLocatorEnhancer exact-owner unpatch returned while its Harmony owner still remained on a selected native target.");
            }

            harmony = null;
        }

        internal static MethodInfo ResolveTarget()
        {
            Type archiveType =
                typeof(DolocAPI).Assembly.GetType(
                    "DolocTown.GameData.ArchiveDataHandle",
                    throwOnError: false)
                ?? throw new TypeLoadException(
                    "ChestLocatorEnhancer could not resolve DolocTown.GameData.ArchiveDataHandle.");
            foreach (MethodInfo method in archiveType.GetMethods(
                BindingFlags.Public | BindingFlags.Instance))
            {
                if (method.Name != "GetAvailableInventories" ||
                    !method.ReturnType.IsArray)
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length == 3 &&
                    parameters[0].ParameterType.FullName ==
                        "UnityEngine.Vector2Int" &&
                    parameters[1].ParameterType.FullName ==
                        "UnityEngine.Vector2Int" &&
                    parameters[2].ParameterType == typeof(bool))
                {
                    return method;
                }
            }

            throw new MissingMethodException(
                archiveType.FullName,
                "GetAvailableInventories(Vector2Int, Vector2Int, bool)");
        }

        internal static bool HasExactOwner(
            MethodBase method,
            string owner)
        {
            Patches? info = Harmony.GetPatchInfo(method);
            if (info == null)
                return false;
            foreach (string installedOwner in info.Owners)
            {
                if (installedOwner.Equals(
                    owner,
                    StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static void ThrowIfConflictingOwnerIsPresent(
            MethodBase method)
        {
            Patches? info = Harmony.GetPatchInfo(method);
            bool compatibilityOwnerPresent = false;
            bool productOwnerPresent = false;
            if (info != null)
            {
                foreach (string owner in info.Owners)
                {
                    compatibilityOwnerPresent |= owner.Equals(
                        CompatibilityOwner,
                        StringComparison.Ordinal);
                    productOwnerPresent |= owner.Equals(
                        HarmonyOwner,
                        StringComparison.Ordinal);
                }
            }

            ChestLocatorInstallDecision decision =
                ChestLocatorHookOwnership.DecideInstall(
                    compatibilityOwnerPresent,
                    productOwnerPresent);
            if (decision == ChestLocatorInstallDecision.RejectCompatibilityOwner)
                throw new InvalidOperationException(
                    "ChestLocatorEnhancer refused to install because frozen compatibility already owns ArchiveDataHandle.GetAvailableInventories. Restart after disabling the old Strict consumer.");
            if (decision == ChestLocatorInstallDecision.RejectDuplicateProductOwner)
                throw new InvalidOperationException(
                    "ChestLocatorEnhancer refused to install because its exact ProductNative Harmony owner is already present.");
        }

        private void PublishObservedOwnerState(
            MethodBase method)
        {
            int count = 0;
            foreach (MethodBase owned in ownedTargets)
            {
                Patches? info = Harmony.GetPatchInfo(owned);
                if (info == null) continue;
                foreach (Patch patch in info.Prefixes) if (patch.owner == HarmonyOwner) count++;
                foreach (Patch patch in info.Postfixes) if (patch.owner == HarmonyOwner) count++;
                foreach (Patch patch in info.Transpilers) if (patch.owner == HarmonyOwner) count++;
                foreach (Patch patch in info.Finalizers) if (patch.owner == HarmonyOwner) count++;
            }
            InstalledPatchCount = count;
            IsInstalled = count != 0;
        }

        private static void UnpatchExactOwner(
            Harmony ownerHarmony)
        {
            foreach (MethodInfo method in typeof(Harmony).GetMethods(
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Static |
                BindingFlags.Instance))
            {
                ParameterInfo[] parameters = method.GetParameters();
                if (method.Name != "UnpatchAll" ||
                    parameters.Length != 1 ||
                    parameters[0].ParameterType != typeof(string))
                {
                    continue;
                }

                method.Invoke(
                    method.IsStatic ? null : ownerHarmony,
                    new object[] { HarmonyOwner });
                return;
            }

            throw new MissingMethodException(
                typeof(Harmony).FullName,
                "UnpatchAll(string)");
        }
    }
}
