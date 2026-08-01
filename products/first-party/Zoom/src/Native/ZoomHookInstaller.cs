using System;
using System.Linq;
using System.Reflection;
using global::DTMAPI.Abstractions;
using HarmonyLib;

namespace DTMAPI.Zoom
{
    internal sealed class ZoomHookInstaller : IZoomHookOwner
    {
        private readonly IMonitor monitor;
        private readonly Func<bool>? installGate;
        private readonly Action? unpatchOverride;
        private Harmony? harmony;
        private MethodInfo? setEnvironmentCameraTarget;
        private MethodInfo? refreshResolutionTarget;

        internal ZoomHookInstaller(
            IMonitor monitor,
            Func<bool>? installGate = null,
            Action? unpatchOverride = null)
        {
            this.monitor = monitor ??
                throw new ArgumentNullException(nameof(monitor));
            this.installGate = installGate;
            this.unpatchOverride = unpatchOverride;
        }

        public bool IsInstalled { get; private set; }

        public int InstalledPatchCount =>
            CountOwnedPatches();

        public void InstallAtomically(
            ZoomNativeRuntime runtime)
        {
            if (runtime == null)
                throw new ArgumentNullException(nameof(runtime));
            if (IsInstalled)
                return;

            setEnvironmentCameraTarget = ResolveTarget();
            refreshResolutionTarget =
                ResolveRefreshResolutionTarget();
            ThrowIfOwnerCannotInstall(
                setEnvironmentCameraTarget,
                refreshResolutionTarget);
            if (installGate != null && !installGate())
            {
                throw new InvalidOperationException(
                    "Injected Zoom Hook installation failure.");
            }

            MethodInfo setEnvironmentPostfix =
                typeof(ZoomCallbacks).GetMethod(
                    nameof(ZoomCallbacks
                        .DolocApiSetEnvCameraPostfix),
                    BindingFlags.Public |
                    BindingFlags.Static) ??
                throw new MissingMethodException(
                    typeof(ZoomCallbacks).FullName,
                    nameof(ZoomCallbacks
                        .DolocApiSetEnvCameraPostfix));
            MethodInfo refreshPrefix =
                typeof(ZoomCallbacks).GetMethod(
                    nameof(ZoomCallbacks
                        .CameraControllerRefreshResolutionPrefix),
                    BindingFlags.Public |
                    BindingFlags.Static) ??
                throw new MissingMethodException(
                    typeof(ZoomCallbacks).FullName,
                    nameof(ZoomCallbacks
                        .CameraControllerRefreshResolutionPrefix));
            MethodInfo refreshFinalizer =
                typeof(ZoomCallbacks).GetMethod(
                    nameof(ZoomCallbacks
                        .CameraControllerRefreshResolutionFinalizer),
                    BindingFlags.Public |
                    BindingFlags.Static) ??
                throw new MissingMethodException(
                    typeof(ZoomCallbacks).FullName,
                    nameof(ZoomCallbacks
                        .CameraControllerRefreshResolutionFinalizer));
            harmony = new Harmony(
                ZoomProductContract.HarmonyOwner);
            ZoomHookTransaction.Install(
                () => ZoomCallbacks.Attach(runtime),
                () =>
                {
                    harmony.Patch(
                        setEnvironmentCameraTarget,
                        postfix: new HarmonyMethod(
                            setEnvironmentPostfix));
                    harmony.Patch(
                        refreshResolutionTarget,
                        prefix: new HarmonyMethod(
                            refreshPrefix),
                        finalizer: new HarmonyMethod(
                            refreshFinalizer));
                },
                () =>
                {
                    PublishObservedState();
                    return IsInstalled;
                },
                UnpatchExactOwner,
                () =>
                {
                    PublishObservedState();
                    ZoomCallbacks.Detach(runtime);
                });
            PublishObservedState();
            monitor.Log(
                "Zoom ProductNative Hook installed owner=" +
                ZoomProductContract.HarmonyOwner +
                " count=3.");
        }

        public void UnpatchOwnedHooks(
            ZoomNativeRuntime runtime)
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
                if (setEnvironmentCameraTarget == null)
                {
                    try
                    {
                        setEnvironmentCameraTarget =
                            ResolveTarget();
                    }
                    catch
                    {
                    }
                }
                if (refreshResolutionTarget == null)
                {
                    try
                    {
                        refreshResolutionTarget =
                            ResolveRefreshResolutionTarget();
                    }
                    catch
                    {
                    }
                }
                PublishObservedState();
                ZoomCallbacks.Detach(runtime);
            }

            if (IsInstalled)
            {
                throw new InvalidOperationException(
                    "Zoom exact-owner cleanup retained one or more camera patches.");
            }
            if (unpatchFailure != null)
                throw unpatchFailure;
            harmony = null;
        }

        internal static MethodInfo ResolveTarget()
        {
            MethodInfo[] matches = typeof(DolocAPI)
                .GetMethods(
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Static)
                .Where(method =>
                    method.Name.Equals(
                        "SetEnvCamera",
                        StringComparison.Ordinal) &&
                    HasExactSetEnvCameraSignature(method))
                .ToArray();
            if (matches.Length != 1)
            {
                throw new MissingMethodException(
                    typeof(DolocAPI).FullName,
                    "SetEnvCamera(Vector2,Vector2,bool,bool,bool)");
            }
            return matches[0];
        }

        internal static MethodInfo ResolveRefreshResolutionTarget()
        {
            Type controllerType =
                ResolveCameraControllerType();
            MethodInfo[] matches = controllerType
                .GetMethods(
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance)
                .Where(method =>
                    method.Name.Equals(
                        "RefreshResolution",
                        StringComparison.Ordinal) &&
                    !method.IsStatic &&
                    method.ReturnType == typeof(void) &&
                    method.GetParameters().Length == 0)
                .ToArray();
            if (matches.Length != 1)
            {
                throw new MissingMethodException(
                    controllerType.FullName,
                    "RefreshResolution()");
            }
            return matches[0];
        }

        private static Type ResolveCameraControllerType()
        {
            FieldInfo? field = typeof(DolocAPI).GetField(
                "cameraController",
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Static);
            if (field != null && field.FieldType != typeof(object))
                return field.FieldType;

            PropertyInfo? property = typeof(DolocAPI).GetProperty(
                "cameraController",
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Static);
            if (property != null && property.PropertyType != typeof(object))
                return property.PropertyType;

            object? instance = field?.GetValue(null) ??
                property?.GetValue(null);
            return instance?.GetType() ??
                throw new TypeLoadException(
                    "DolocAPI.cameraController type was unavailable.");
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

        internal static bool HasExactOwner(
            MethodBase method,
            string owner)
        {
            Patches? info = Harmony.GetPatchInfo(method);
            return info != null &&
                info.Owners.Any(candidate =>
                    candidate.Equals(
                        owner,
                        StringComparison.Ordinal));
        }

        private static void ThrowIfOwnerCannotInstall(
            MethodBase setEnvironmentCamera,
            MethodBase refreshResolution)
        {
            switch (ZoomHookOwnership.DecideInstall(
                HasExactOwner(
                    setEnvironmentCamera,
                    ZoomProductContract
                        .CompatibilityOwner),
                HasExactOwner(
                    setEnvironmentCamera,
                    ZoomProductContract.HarmonyOwner) ||
                HasExactOwner(
                    refreshResolution,
                    ZoomProductContract.HarmonyOwner)))
            {
                case ZoomInstallDecision.Install:
                    return;
                case ZoomInstallDecision
                    .RejectCompatibilityOwner:
                    throw new InvalidOperationException(
                        "Zoom refused to install because frozen camera compatibility already owns DolocAPI.SetEnvCamera.");
                default:
                    throw new InvalidOperationException(
                        "Zoom refused a duplicate exact ProductNative owner.");
            }
        }

        private void PublishObservedState()
        {
            IsInstalled = CountOwnedPatches() == 3;
        }

        private int CountOwnedPatches()
        {
            int count = 0;
            if (setEnvironmentCameraTarget != null)
            {
                Patches? setInfo = Harmony.GetPatchInfo(
                    setEnvironmentCameraTarget);
                if (setInfo != null)
                {
                    count += setInfo.Postfixes.Count(patch =>
                        patch.owner.Equals(
                            ZoomProductContract.HarmonyOwner,
                            StringComparison.Ordinal));
                }
            }
            if (refreshResolutionTarget != null)
            {
                Patches? refreshInfo = Harmony.GetPatchInfo(
                    refreshResolutionTarget);
                if (refreshInfo != null)
                {
                    count += refreshInfo.Prefixes.Count(patch =>
                        patch.owner.Equals(
                            ZoomProductContract.HarmonyOwner,
                            StringComparison.Ordinal));
                    count += refreshInfo.Finalizers.Count(patch =>
                        patch.owner.Equals(
                            ZoomProductContract.HarmonyOwner,
                            StringComparison.Ordinal));
                }
            }
            return count;
        }

        private void UnpatchExactOwner()
        {
            if (unpatchOverride != null)
            {
                unpatchOverride();
                return;
            }
            Harmony owner = harmony ??
                new Harmony(
                    ZoomProductContract.HarmonyOwner);
            MethodInfo? method = typeof(Harmony)
                .GetMethods(
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Static |
                    BindingFlags.Instance)
                .SingleOrDefault(candidate =>
                    candidate.Name == "UnpatchAll" &&
                    candidate.GetParameters().Length == 1 &&
                    candidate.GetParameters()[0]
                        .ParameterType == typeof(string));
            if (method == null)
            {
                throw new MissingMethodException(
                    typeof(Harmony).FullName,
                    "UnpatchAll(string)");
            }
            method.Invoke(
                method.IsStatic ? null : owner,
                new object[]
                {
                    ZoomProductContract.HarmonyOwner
                });
        }
    }
}
