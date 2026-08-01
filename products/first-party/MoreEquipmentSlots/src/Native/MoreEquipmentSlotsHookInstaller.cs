using System;
using System.Collections.Generic;
using System.Reflection;
using global::DTMAPI.Abstractions;
using HarmonyLib;

namespace DTMAPI.MoreEquipmentSlots
{
    internal sealed class MoreEquipmentSlotsHookInstaller
    {
        internal const int ExpectedPatchCount =
            MoreEquipmentSlotsProductContract.ExpectedHookCount;
        private readonly IMonitor monitor;
        private readonly Func<int, bool>? installGate;
        private readonly List<MethodInfo> targets =
            new List<MethodInfo>(ExpectedPatchCount);
        private Harmony? harmony;

        internal MoreEquipmentSlotsHookInstaller(
            IMonitor monitor,
            Func<int, bool>? installGate = null)
        {
            this.monitor = monitor ??
                throw new ArgumentNullException(nameof(monitor));
            this.installGate = installGate;
        }

        internal bool IsInstalled { get; private set; }

        internal int InstalledPatchCount { get; private set; }

        internal void InstallAtomically(
            MoreEquipmentSlotsNativeRuntime runtime)
        {
            if (runtime == null)
                throw new ArgumentNullException(nameof(runtime));
            if (IsInstalled)
                return;

            targets.Clear();
            targets.Add(ResolveReloadParams());
            targets.Add(ResolveOnAttacked());
            targets.Add(ResolveAccessoriesBarMethod("__Init"));
            targets.Add(ResolveAccessoriesBarMethod("OnStartShow"));
            ThrowIfOwnerCannotInstall(targets);

            MethodInfo reloadCallback = RequireCallback(
                nameof(MoreEquipmentSlotsCallbacks
                    .AgentEquipmentManagerReloadParamsPostfix));
            MethodInfo attackGeneric = RequireCallback(
                nameof(MoreEquipmentSlotsCallbacks
                    .BodyControllerOnAttackedPrefix));
            Type positionType =
                targets[1].GetParameters()[2].ParameterType;
            MethodInfo attackCallback =
                attackGeneric.MakeGenericMethod(positionType);
            MethodInfo initCallback = RequireCallback(
                nameof(MoreEquipmentSlotsCallbacks
                    .AccessoriesBarInitPostfix));
            MethodInfo showCallback = RequireCallback(
                nameof(MoreEquipmentSlotsCallbacks
                    .AccessoriesBarStartShowPostfix));

            harmony = new Harmony(
                MoreEquipmentSlotsProductContract.HarmonyOwner);
            MoreEquipmentSlotsCallbacks.Attach(runtime);
            try
            {
                PatchWithGate(
                    1,
                    targets[0],
                    postfix: new HarmonyMethod(reloadCallback));
                PatchWithGate(
                    2,
                    targets[1],
                    prefix: new HarmonyMethod(attackCallback));
                PatchWithGate(
                    3,
                    targets[2],
                    postfix: new HarmonyMethod(initCallback));
                PatchWithGate(
                    4,
                    targets[3],
                    postfix: new HarmonyMethod(showCallback));

                PublishObservedState();
                if (!IsInstalled ||
                    InstalledPatchCount != ExpectedPatchCount)
                {
                    throw new InvalidOperationException(
                        "MoreEquipmentSlots could not prove all four exact-owner hooks after installation.");
                }

                monitor.Log(
                    "MoreEquipmentSlots product-owned Hook set installed owner=" +
                    MoreEquipmentSlotsProductContract.HarmonyOwner +
                    " count=" +
                    InstalledPatchCount +
                    ".");
            }
            catch (Exception installFailure)
            {
                MoreEquipmentSlotsCallbacks.Detach(
                    runtime,
                    runtime.BuildLifecycleSummary(
                        callbacksOverride: 0,
                        hooksOverride: 0));
                try
                {
                    UnpatchExactOwner();
                }
                catch (Exception cleanupFailure)
                {
                    throw new AggregateException(
                        "MoreEquipmentSlots atomic Hook installation failed and exact-owner rollback also failed.",
                        installFailure,
                        cleanupFailure);
                }
                throw;
            }
        }

        internal void UnpatchOwnedHooks(
            MoreEquipmentSlotsNativeRuntime runtime)
        {
            Exception? unpatchFailure = null;
            try
            {
                UnpatchExactOwner();
            }
            catch (Exception ex)
            {
                unpatchFailure = ex;
            }
            finally
            {
                MoreEquipmentSlotsCallbacks.Detach(
                    runtime,
                    runtime.BuildLifecycleSummary(
                        callbacksOverride: 0,
                        hooksOverride: 0));
                PublishObservedState();
            }

            if (IsInstalled || InstalledPatchCount != 0)
            {
                var residue = new InvalidOperationException(
                    "MoreEquipmentSlots exact-owner unpatch left one or more product Hook owners installed.");
                if (unpatchFailure != null)
                {
                    throw new AggregateException(
                        unpatchFailure,
                        residue);
                }
                throw residue;
            }
            if (unpatchFailure != null)
                throw unpatchFailure;

            harmony = null;
        }

        private static MethodInfo ResolveReloadParams()
        {
            Type type = ResolveGameType(
                "DolocTown.GameData.AgentEquipmentManager");
            return type.GetMethod(
                "ReloadParams",
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance,
                binder: null,
                types: Type.EmptyTypes,
                modifiers: null) ??
                throw new MissingMethodException(
                    type.FullName,
                    "ReloadParams()");
        }

        private static MethodInfo ResolveOnAttacked()
        {
            Type type = ResolveGameType(
                "DolocTown.BodyController");
            foreach (MethodInfo method in type.GetMethods(
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance))
            {
                if (method.Name != "OnAttacked" ||
                    method.ReturnType != typeof(bool))
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length == 4 &&
                    parameters[0].ParameterType == typeof(float) &&
                    parameters[1].ParameterType == typeof(bool) &&
                    parameters[2].ParameterType.FullName ==
                        "UnityEngine.Vector2" &&
                    parameters[3].ParameterType ==
                        typeof(bool).MakeByRefType())
                {
                    return method;
                }
            }

            throw new MissingMethodException(
                type.FullName,
                "OnAttacked(float,bool,Vector2,out bool)");
        }

        private static MethodInfo ResolveAccessoriesBarMethod(
            string name)
        {
            Type type = ResolveGameType(
                "DolocTown.UI.AccessoriesBar");
            return type.GetMethod(
                name,
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance,
                binder: null,
                types: Type.EmptyTypes,
                modifiers: null) ??
                throw new MissingMethodException(
                    type.FullName,
                    name + "()");
        }

        private static Type ResolveGameType(string fullName) =>
            typeof(DolocAPI).Assembly.GetType(
                fullName,
                throwOnError: false) ??
            throw new TypeLoadException(
                "MoreEquipmentSlots could not resolve " +
                fullName +
                ".");

        private static MethodInfo RequireCallback(string name) =>
            typeof(MoreEquipmentSlotsCallbacks).GetMethod(
                name,
                BindingFlags.Public | BindingFlags.Static) ??
            throw new MissingMethodException(
                typeof(MoreEquipmentSlotsCallbacks).FullName,
                name);

        private void PatchWithGate(
            int ordinal,
            MethodInfo target,
            HarmonyMethod? prefix = null,
            HarmonyMethod? postfix = null)
        {
            if (installGate != null &&
                !installGate(ordinal))
            {
                throw new InvalidOperationException(
                    "Injected MoreEquipmentSlots product Hook installation failure at ordinal " +
                    ordinal +
                    ".");
            }
            harmony!.Patch(
                target,
                prefix: prefix,
                postfix: postfix);
        }

        private static void ThrowIfOwnerCannotInstall(
            IReadOnlyList<MethodInfo> methods)
        {
            bool[] compatibility = ObserveOwners(
                methods,
                MoreEquipmentSlotsProductContract
                    .CompatibilityOwner);
            bool[] product = ObserveOwners(
                methods,
                MoreEquipmentSlotsProductContract.HarmonyOwner);
            EquipmentSlotsInstallDecision decision =
                MoreEquipmentSlotsHookOwnership.DecideInstall(
                    compatibility,
                    product);
            switch (decision)
            {
                case EquipmentSlotsInstallDecision.Install:
                    return;
                case EquipmentSlotsInstallDecision
                    .RejectCompatibilityOwner:
                    throw new InvalidOperationException(
                        "MoreEquipmentSlots refused to install because frozen Compatibility owns at least one of its four native targets.");
                case EquipmentSlotsInstallDecision
                    .RejectDuplicateProductOwner:
                    throw new InvalidOperationException(
                        "MoreEquipmentSlots refused a duplicate exact ProductNative owner.");
                default:
                    throw new InvalidOperationException(
                        "MoreEquipmentSlots found a partial exact-owner Hook set and failed closed.");
            }
        }

        private void PublishObservedState()
        {
            if (targets.Count != ExpectedPatchCount)
            {
                IsInstalled = false;
                InstalledPatchCount = 0;
                return;
            }

            EquipmentSlotsObservedHookState observed =
                MoreEquipmentSlotsHookOwnership.Observe(
                    ObserveOwners(
                        targets,
                        MoreEquipmentSlotsProductContract
                            .HarmonyOwner));
            IsInstalled = observed.IsInstalled;
            InstalledPatchCount =
                observed.InstalledPatchCount;
        }

        private static bool[] ObserveOwners(
            IReadOnlyList<MethodInfo> methods,
            string owner)
        {
            var result = new bool[methods.Count];
            for (int index = 0; index < methods.Count; index++)
            {
                Patches? info =
                    Harmony.GetPatchInfo(methods[index]);
                if (info == null)
                    continue;
                foreach (string installedOwner in info.Owners)
                {
                    if (installedOwner.Equals(
                        owner,
                        StringComparison.Ordinal))
                    {
                        result[index] = true;
                        break;
                    }
                }
            }

            return result;
        }

        private void UnpatchExactOwner()
        {
            if (harmony == null)
                return;
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
                    method.IsStatic ? null : harmony,
                    new object[]
                    {
                        MoreEquipmentSlotsProductContract
                            .HarmonyOwner
                    });
                return;
            }

            throw new MissingMethodException(
                typeof(Harmony).FullName,
                "UnpatchAll(string)");
        }
    }
}
