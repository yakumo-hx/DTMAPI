using System;
using System.Linq;
using System.Reflection;
using DTMAPI.Abstractions;
using DolocTown;
using DolocTown.GameData;
using HarmonyLib;
using UnityEngine;

namespace DTMAPI.Zoom
{
    internal sealed class ZoomHookInstaller : IZoomHookOwner
    {
        private readonly IMonitor monitor;
        private readonly Func<bool>? installGate;
        private readonly Action? unpatchOverride;
        private Harmony? harmony;
        private ZoomPatch[]? required;

        internal ZoomHookInstaller(IMonitor monitor, Func<bool>? installGate = null, Action? unpatchOverride = null)
        { this.monitor = monitor; this.installGate = installGate; this.unpatchOverride = unpatchOverride; }
        public bool IsInstalled => required != null && required.All(p => Patches(p.Target, p.Kind).Any(x => IsOurs(x) && x.PatchMethod == p.Callback));
        public int InstalledPatchCount => Harmony.GetAllPatchedMethods().Sum(m => AllPatches(m).Count(IsOurs));
        internal bool HasOwnedPatches => Harmony.GetAllPatchedMethods().Any(m => AllPatches(m).Any(IsOurs));

        public void InstallAtomically(ZoomNativeRuntime runtime)
        {
            if (IsInstalled) return;
            required = new[]
            {
                Patch(ResolveTarget(), HarmonyPatchType.Postfix, nameof(ZoomCallbacks.DolocApiSetEnvCameraPostfix)),
                Patch(ResolveRefreshResolutionTarget(), HarmonyPatchType.Prefix, nameof(ZoomCallbacks.CameraControllerRefreshResolutionPrefix)),
                Patch(ResolveRefreshResolutionTarget(), HarmonyPatchType.Finalizer, nameof(ZoomCallbacks.CameraControllerRefreshResolutionFinalizer)),
                Patch(Target(typeof(BackgroundRenderer), "LoadPreset", typeof(EnvBackgroundSO)), HarmonyPatchType.Prefix, nameof(ZoomCallbacks.BackgroundLoadPrefix)),
                Patch(Target(typeof(BackgroundRenderer), "LoadPreset", typeof(EnvBackgroundSO)), HarmonyPatchType.Finalizer, nameof(ZoomCallbacks.BackgroundLoadFinalizer)),
                Patch(Target(typeof(DepthFogController), "__Init"), HarmonyPatchType.Postfix, nameof(ZoomCallbacks.ContentChangedPostfix)),
                Patch(Target(typeof(DepthFogControllerBuilding), "__Init"), HarmonyPatchType.Postfix, nameof(ZoomCallbacks.ContentChangedPostfix))
            };
            if (HasExactOwner(ResolveTarget(), ZoomProductContract.CompatibilityOwner))
                throw new InvalidOperationException("Zoom refused the active frozen camera compatibility owner.");
            if (HasOwnedPatches) throw new InvalidOperationException("Zoom refused a duplicate or residual exact ProductNative owner.");
            if (installGate != null && !installGate()) throw new InvalidOperationException("Injected Zoom Hook installation failure.");
            harmony = new Harmony(ZoomProductContract.HarmonyOwner);
            ZoomHookTransaction.Install(
                () => ZoomCallbacks.Attach(runtime),
                () =>
                {
                    foreach (var p in required)
                    {
                        var hook = new HarmonyMethod(p.Callback);
                        harmony.Patch(p.Target, prefix: p.Kind == HarmonyPatchType.Prefix ? hook : null,
                            postfix: p.Kind == HarmonyPatchType.Postfix ? hook : null,
                            finalizer: p.Kind == HarmonyPatchType.Finalizer ? hook : null);
                    }
                },
                () => IsInstalled, () => HasOwnedPatches, UnpatchExactOwner, () => ZoomCallbacks.Detach(runtime));
            monitor.Log("Zoom ProductNative Hook installed owner=" + ZoomProductContract.HarmonyOwner + " count=" + InstalledPatchCount);
        }
        public void UnpatchOwnedHooks(ZoomNativeRuntime runtime)
        {
            Exception? failure = null;
            try { UnpatchExactOwner(); } catch (Exception ex) { failure = ex; }
            finally { ZoomCallbacks.Detach(runtime); }
            if (HasOwnedPatches) throw new InvalidOperationException("Zoom exact-owner cleanup retained patches.", failure);
            if (failure != null) throw failure;
            harmony = null;
        }
        internal static MethodInfo ResolveTarget() => Target(typeof(DolocAPI), "SetEnvCamera", typeof(Vector2), typeof(Vector2), typeof(bool), typeof(bool), typeof(bool));
        internal static MethodInfo ResolveRefreshResolutionTarget() => Target(typeof(CameraController), "RefreshResolution");
        internal static bool HasExactOwner(MethodBase method, string owner) => Harmony.GetPatchInfo(method)?.Owners.Any(x => string.Equals(x, owner, StringComparison.Ordinal)) == true;
        private static MethodInfo Target(Type type, string name, params Type[] args) =>
            type.GetMethod(name, BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, args, null) ?? throw new MissingMethodException(type.FullName, name);
        private static ZoomPatch Patch(MethodInfo target, HarmonyPatchType kind, string callback) =>
            new ZoomPatch(target, kind, typeof(ZoomCallbacks).GetMethod(callback)!);
        private static bool IsOurs(HarmonyLib.Patch patch) => string.Equals(patch.owner, ZoomProductContract.HarmonyOwner, StringComparison.Ordinal);
        private static System.Collections.Generic.IEnumerable<HarmonyLib.Patch> AllPatches(MethodBase target)
        {
            var info = Harmony.GetPatchInfo(target);
            return info == null ? Array.Empty<HarmonyLib.Patch>() : info.Prefixes.Concat(info.Postfixes).Concat(info.Finalizers).Concat(info.Transpilers);
        }
        private static System.Collections.Generic.IEnumerable<HarmonyLib.Patch> Patches(MethodBase target, HarmonyPatchType kind)
        {
            var info = Harmony.GetPatchInfo(target);
            if (info == null) return Array.Empty<HarmonyLib.Patch>();
            return kind == HarmonyPatchType.Prefix ? info.Prefixes : kind == HarmonyPatchType.Postfix ? info.Postfixes : info.Finalizers;
        }
        private void UnpatchExactOwner()
        {
            if (unpatchOverride != null) { unpatchOverride(); return; }
            (harmony ?? new Harmony(ZoomProductContract.HarmonyOwner)).UnpatchSelf();
        }
        private readonly struct ZoomPatch
        {
            internal ZoomPatch(MethodInfo target, HarmonyPatchType kind, MethodInfo callback) { Target = target; Kind = kind; Callback = callback; }
            internal MethodInfo Target { get; }
            internal HarmonyPatchType Kind { get; }
            internal MethodInfo Callback { get; }
        }
    }
}
