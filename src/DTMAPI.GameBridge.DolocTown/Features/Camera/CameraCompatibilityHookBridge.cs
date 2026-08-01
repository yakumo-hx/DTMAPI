using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    /// <summary>
    /// Exact-owner Hook shell for the dormant frozen camera ABI. The managed
    /// Zoom product owns the same native target under a different owner and
    /// both sides fail closed when the other owner arrives first.
    /// </summary>
    internal sealed class CameraCompatibilityHookBridge
    {
        internal const string HarmonyOwner =
            "dtmapi.gamebridge.doloctown.camera.compatibility";
        internal const string ManagedProductHarmonyOwner =
            "dtmapi.mod.dtmapi.zoommod";

        private readonly DtmApiRuntime runtime;
        private readonly CameraViewService service;
        private readonly HarmonyReflectionPatcher patcher;
        private Func<bool>? compatibilityOwnerClaimOverrideForTests;

        internal CameraCompatibilityHookBridge(
            DtmApiRuntime runtime,
            CameraViewService service)
        {
            this.runtime = runtime ??
                throw new ArgumentNullException(nameof(runtime));
            this.service = service ??
                throw new ArgumentNullException(nameof(service));
            patcher = new HarmonyReflectionPatcher(
                runtime,
                HarmonyOwner);
        }

        internal bool SetEnvCameraPatched { get; private set; }

        internal void SetCompatibilityOwnerClaimOverrideForTests(
            Func<bool>? ownerClaim) =>
            compatibilityOwnerClaimOverrideForTests = ownerClaim;

        internal bool EnsureCompatibilityOwnerClaim()
        {
            if (IsManagedProductOwnerPresent())
                return false;
            if (compatibilityOwnerClaimOverrideForTests != null)
            {
                SetEnvCameraPatched =
                    compatibilityOwnerClaimOverrideForTests();
                return SetEnvCameraPatched;
            }
            if (!SetEnvCameraPatched)
            {
                if (HasOwner(HarmonyOwner))
                {
                    SetEnvCameraPatched = true;
                }
                else
                {
                    SetEnvCameraPatched =
                        patcher.TryPatchPostfix(
                            "DolocAPI, Assembly-CSharp",
                            "SetEnvCamera",
                            typeof(DolocTownHookCallbacks)
                                .GetMethod(
                                    nameof(DolocTownHookCallbacks
                                        .CameraCompatibilitySetEnvCameraPostfix),
                                    BindingFlags.Public |
                                    BindingFlags.Static),
                            5);
                }
            }
            SetEnvCameraPatched =
                SetEnvCameraPatched &&
                HasOwner(HarmonyOwner);
            return SetEnvCameraPatched;
        }

        internal void PublishHookStatuses()
        {
            service.PublishHookStatuses();
            PublishOwnershipStatus("dormant");
        }

        internal void InstallHooks()
        {
            int removed =
                service.ReconcileManagedProductOwnerBeforeHookInstall();
            bool productOwnerPresent =
                IsManagedProductOwnerPresent();
            bool demanded =
                service.HasEnvironmentResetDemand;
            CameraCompatibilityInstallDecision decision =
                CameraCompatibilityHookOwnership.DecideInstall(
                    productOwnerPresent,
                    demanded);
            if (removed > 0 ||
                decision !=
                CameraCompatibilityInstallDecision.Install)
            {
                RemoveOwnedHook();
                runtime.SetHookStatus(
                    "Camera.ViewEnvironmentLifecycle",
                    productOwnerPresent || removed > 0
                        ? "refused-managed-product-owner"
                        : "dormant",
                    "DolocAPI.SetEnvCamera exact-target ownership",
                    productOwnerPresent || removed > 0
                        ? "Frozen camera compatibility owns no Hook because the managed Zoom product owns the exact target."
                        : "No frozen ICameraViewApi or ICameraZoomApi lease demands the compatibility Postfix.");
                return;
            }

            if (!EnsureCompatibilityOwnerClaim())
                SetEnvCameraPatched = false;

            service.SetEnvironmentLifecyclePatched(
                SetEnvCameraPatched);
            PublishOwnershipStatus(
                SetEnvCameraPatched
                    ? "obsolete-compatibility"
                    : "pending");
        }

        internal void RemoveOwnedHook()
        {
            if (compatibilityOwnerClaimOverrideForTests != null)
            {
                SetEnvCameraPatched = false;
                service.SetEnvironmentLifecyclePatched(false);
                return;
            }
            if (SetEnvCameraPatched &&
                !patcher.TryUnpatchAllOwnedPatches())
            {
                service.SetEnvironmentLifecyclePatched(true);
                throw new InvalidOperationException(
                    "Frozen camera compatibility could not remove its exact Harmony owner.");
            }

            SetEnvCameraPatched = false;
            service.SetEnvironmentLifecyclePatched(false);
        }

        internal static bool IsManagedProductOwnerPresent() =>
            HasOwner(ManagedProductHarmonyOwner);

        private void PublishOwnershipStatus(string status)
        {
            runtime.SetHookStatus(
                "Camera.ViewEnvironmentLifecycle",
                status,
                "Harmony Postfix: DolocAPI.SetEnvCamera",
                "Frozen camera ABI compatibility owner=" +
                HarmonyOwner +
                "; installed=" +
                SetEnvCameraPatched +
                "; managed product owner=" +
                ManagedProductHarmonyOwner +
                ".");
        }

        private static bool HasOwner(string owner)
        {
            Type? targetType =
                ResolveType("DolocAPI, Assembly-CSharp");
            MethodInfo? target = targetType?
                .GetMethods(
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Static)
                .SingleOrDefault(method =>
                    method.Name.Equals(
                        "SetEnvCamera",
                        StringComparison.Ordinal) &&
                    HasExactSetEnvCameraSignature(method));
            Type? harmonyType =
                ResolveType("HarmonyLib.Harmony, 0Harmony");
            MethodInfo? getPatchInfo = harmonyType?
                .GetMethod(
                    "GetPatchInfo",
                    BindingFlags.Public |
                    BindingFlags.Static,
                    null,
                    new[] { typeof(MethodBase) },
                    null);
            object? info =
                target == null || getPatchInfo == null
                    ? null
                    : getPatchInfo.Invoke(
                        null,
                        new object[] { target });
            object? ownerList = info?
                .GetType()
                .GetProperty(
                    "Owners",
                    BindingFlags.Public |
                    BindingFlags.Instance)?
                .GetValue(info);
            if (!(ownerList is IEnumerable owners))
                return false;
            foreach (object? value in owners)
            {
                if ((value as string ?? string.Empty)
                    .Equals(
                        owner,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool HasExactSetEnvCameraSignature(
            MethodInfo method)
        {
            ParameterInfo[] parameters =
                method.GetParameters();
            return parameters.Length == 5 &&
                parameters[0].ParameterType.FullName ==
                    "UnityEngine.Vector2" &&
                parameters[1].ParameterType.FullName ==
                    "UnityEngine.Vector2" &&
                parameters[2].ParameterType == typeof(bool) &&
                parameters[3].ParameterType == typeof(bool) &&
                parameters[4].ParameterType == typeof(bool);
        }

        private static Type? ResolveType(
            string assemblyQualifiedName)
        {
            Type? type = Type.GetType(
                assemblyQualifiedName,
                throwOnError: false);
            if (type != null)
                return type;
            int comma = assemblyQualifiedName.IndexOf(',');
            string name = comma < 0
                ? assemblyQualifiedName
                : assemblyQualifiedName
                    .Substring(0, comma)
                    .Trim();
            foreach (Assembly assembly in
                     AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(
                    name,
                    throwOnError: false);
                if (type != null)
                    return type;
            }
            return null;
        }
    }
}
