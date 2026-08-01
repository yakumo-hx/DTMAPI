using System;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed partial class QaScenarioController
    {
        private const string ZoomProductOwnerId =
            "DTMAPI.ZoomMod";
        private const string ZoomProductAssemblyName =
            "DTMAPI.Zoom";
        private const string ZoomProductHarmonyOwner =
            "dtmapi.mod.dtmapi.zoommod";
        private const string ZoomCompatibilityHarmonyOwner =
            "dtmapi.gamebridge.doloctown.camera.compatibility";
        private const string ZoomProductCallbackTypeName =
            "DTMAPI.Zoom.ZoomCallbacks";
        private static readonly Batch6HarmonyPatchTarget[]
            ZoomProductHarmonyTargets =
            {
                new Batch6HarmonyPatchTarget(
                    "DolocAPI",
                    "SetEnvCamera",
                    5),
                new Batch6HarmonyPatchTarget(
                    "DolocTown.CameraController",
                    "RefreshResolution",
                    0,
                    expectedOwnerPatchCount: 2)
            };

        private void TryExerciseZoomProductNativeForFixture()
        {
            object? productRuntime = null;
            object? enabledConfig = null;
            bool needsReenable = false;
            try
            {
                Batch6HarmonyOwnerInventory before =
                    ObserveZoomProductOwnerForFixture();
                if (!before.IsComplete ||
                    before.ExactOwnerPatchCount != 3)
                {
                    throw new InvalidOperationException(
                        "Zoom ProductNative did not own exactly three " +
                        "SetEnvCamera/RefreshResolution patches before behavior exercise. " +
                        before.Details);
                }
                int compatibilityPatches =
                    Batch6AdvancedHarmonyOwnerObserver
                        .CountAllOwnerPatches(
                            ZoomCompatibilityHarmonyOwner);
                if (compatibilityPatches != 0)
                {
                    throw new InvalidOperationException(
                        "Zoom ProductNative behavior refuses a live " +
                        "Camera compatibility Harmony owner; patches=" +
                        compatibilityPatches.ToString(
                            CultureInfo.InvariantCulture) +
                        ".");
                }

                productRuntime = ReadStaticCallbackRuntime(
                    ZoomProductAssemblyName,
                    ZoomProductCallbackTypeName);
                if (productRuntime == null)
                {
                    throw new InvalidOperationException(
                        "Zoom ProductNative callback runtime was absent.");
                }

                Type runtimeType = productRuntime.GetType();
                MethodInfo setViewScale = RequireInstanceMethod(
                    runtimeType,
                    "SetViewScale",
                    2);
                MethodInfo stepViewScale = RequireInstanceMethod(
                    runtimeType,
                    "Step",
                    2);
                MethodInfo configure = RequireInstanceMethod(
                    runtimeType,
                    "Configure",
                    2);
                Type configType =
                    runtimeType.Assembly.GetType(
                        "DTMAPI.Zoom.ZoomConfig",
                        throwOnError: true,
                        ignoreCase: false)
                    ?? throw new TypeLoadException(
                        "DTMAPI.Zoom.ZoomConfig was unavailable.");
                enabledConfig =
                    Activator.CreateInstance(configType) ??
                    throw new InvalidOperationException(
                        "Zoom enabled config could not be created.");
                SetPublicProperty(
                    enabledConfig,
                    "Enabled",
                    true);
                SetPublicProperty(
                    enabledConfig,
                    "MaxViewScale",
                    4d);
                SetPublicProperty(
                    enabledConfig,
                    "Step",
                    1d);

                patcher ??= new HarmonyReflectionPatcher(runtime);
                Type dolocApi =
                    patcher.ResolveType("DolocAPI, Assembly-CSharp")
                    ?? throw new TypeLoadException(
                        "DolocAPI was unavailable.");
                object mainCamera =
                    ReadStaticMember(dolocApi, "mainCamera")
                    ?? throw new InvalidOperationException(
                        "DolocAPI.mainCamera was unavailable.");
                object cameraController =
                    ReadStaticMember(
                        dolocApi,
                        "cameraController")
                    ?? throw new InvalidOperationException(
                        "DolocAPI.cameraController was unavailable.");
                object? initialFollow =
                    ReadMember(cameraController, "follow");
                MethodInfo refreshResolution =
                    RequireInstanceMethod(
                        cameraController.GetType(),
                        "RefreshResolution",
                        0);
                double initialSize =
                    ReadPositiveDouble(
                        mainCamera,
                        "orthographicSize");
                double initialCamHeight =
                    ReadNestedFiniteDouble(
                        cameraController,
                        "CamSize",
                        "y");
                double initialXRangeMin =
                    ReadNestedFiniteDouble(
                        cameraController,
                        "xRange",
                        "x");
                double initialXRangeMax =
                    ReadNestedFiniteDouble(
                        cameraController,
                        "xRange",
                        "y");
                double initialYRangeMin =
                    ReadNestedFiniteDouble(
                        cameraController,
                        "yRange",
                        "x");
                double initialYRangeMax =
                    ReadNestedFiniteDouble(
                        cameraController,
                        "yRange",
                        "y");
                object? room =
                    ReadStaticMember(dolocApi, "CurrentRoom");
                if (room == null)
                {
                    runtime.SetHookStatus(
                        "Smoke.ZoomProductNative",
                        "pending",
                        "Zoom ProductNative exact-owner behavior",
                        "Waiting for CurrentRoom after SaveLoaded.");
                    return;
                }

                configure.Invoke(
                    productRuntime,
                    new[]
                    {
                        enabledConfig,
                        "G5 ProductNative fixture baseline"
                    });
                if (!InvokeBoolean(
                        setViewScale,
                        productRuntime,
                        2d,
                        "G5 ProductNative 2x"))
                {
                    throw new InvalidOperationException(
                        "Zoom ProductNative rejected the reviewed 2x scale.");
                }
                double sizeAt2x =
                    ReadPositiveDouble(
                        mainCamera,
                        "orthographicSize");
                AssertNear(
                    initialSize * 2d,
                    sizeAt2x,
                    "2x native camera size");
                refreshResolution.Invoke(
                    cameraController,
                    Array.Empty<object>());
                double derivedHeightAt2x =
                    ReadNestedFiniteDouble(
                        cameraController,
                        "CamSize",
                        "y");
                AssertNear(
                    initialCamHeight,
                    derivedHeightAt2x,
                    "native-baseline CameraController.camSize during active 2x refresh");

                if (!InvokeBoolean(
                        stepViewScale,
                        productRuntime,
                        -1,
                        "G5 ProductNative minus key 2x to 1x"))
                {
                    throw new InvalidOperationException(
                        "Zoom ProductNative rejected the reviewed 2x to 1x restoration.");
                }
                double sizeAfterDirectOne =
                    ReadPositiveDouble(
                        mainCamera,
                        "orthographicSize");
                AssertNear(
                    initialSize,
                    sizeAfterDirectOne,
                    "2x to 1x native camera restoration");
                AssertCameraControllerSnapshot(
                    cameraController,
                    initialCamHeight,
                    initialXRangeMin,
                    initialXRangeMax,
                    initialYRangeMin,
                    initialYRangeMax,
                    "2x to 1x native derived-state restoration");

                if (!InvokeBoolean(
                        setViewScale,
                        productRuntime,
                        4d,
                        "G5 ProductNative 4x"))
                {
                    throw new InvalidOperationException(
                        "Zoom ProductNative rejected the reviewed 4x scale.");
                }
                double sizeAt4x =
                    ReadPositiveDouble(
                        mainCamera,
                        "orthographicSize");
                AssertNear(
                    initialSize * 4d,
                    sizeAt4x,
                    "4x native camera size");
                refreshResolution.Invoke(
                    cameraController,
                    Array.Empty<object>());
                double derivedHeightAt4x =
                    ReadNestedFiniteDouble(
                        cameraController,
                        "CamSize",
                        "y");
                AssertNear(
                    initialCamHeight,
                    derivedHeightAt4x,
                    "native-baseline CameraController.camSize during active 4x refresh");
                if (!InvokeBoolean(
                        setViewScale,
                        productRuntime,
                        2d,
                        "G5 4x native refresh to 2x"))
                {
                    throw new InvalidOperationException(
                        "Zoom ProductNative rejected the reviewed 4x -> native resolution/fullscreen refresh -> 2x transition.");
                }
                double sizeAfterActiveRefreshTo2x =
                    ReadPositiveDouble(
                        mainCamera,
                        "orthographicSize");
                AssertNear(
                    initialSize * 2d,
                    sizeAfterActiveRefreshTo2x,
                    "post-4x-refresh 2x native camera size");
                AssertCameraControllerSnapshot(
                    cameraController,
                    initialCamHeight,
                    initialXRangeMin,
                    initialXRangeMax,
                    initialYRangeMin,
                    initialYRangeMax,
                    "4x native refresh to 2x preserved native controller baseline");
                if (!ReferenceEquals(
                        initialFollow,
                        ReadMember(cameraController, "follow")))
                {
                    throw new InvalidOperationException(
                        "Zoom changed CameraController.follow while switching 4x -> native refresh -> 2x.");
                }
                if (!InvokeBoolean(
                        setViewScale,
                        productRuntime,
                        4d,
                        "G5 restore 4x after active-scale coverage"))
                {
                    throw new InvalidOperationException(
                        "Zoom ProductNative could not restore 4x after the active-scale coverage boundary.");
                }

                MethodInfo setEnvCamera = dolocApi
                    .GetMethods(
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.Static)
                    .Single(
                        method =>
                            method.Name == "SetEnvCamera" &&
                            method.GetParameters().Length == 5);
                setEnvCamera.Invoke(
                    null,
                    new[]
                    {
                        ReadMember(room, "CameraPosition"),
                        ReadMember(room, "CameraSize"),
                        ReadMember(room, "ShouldShowBackground"),
                        ReadMember(room, "ShouldMaskBackground"),
                        (object)false
                    });
                setEnvCamera.Invoke(
                    null,
                    new[]
                    {
                        ReadMember(room, "CameraPosition"),
                        ReadMember(room, "CameraSize"),
                        ReadMember(room, "ShouldShowBackground"),
                        ReadMember(room, "ShouldMaskBackground"),
                        (object)false
                    });
                double resetVanillaSize =
                    ReadPositiveDouble(
                        productRuntime,
                        "VanillaOrthographicSize");
                double sizeAfterRealReset =
                    ReadPositiveDouble(
                        mainCamera,
                        "orthographicSize");
                AssertNear(
                    initialSize,
                    resetVanillaSize,
                    "real SetEnvCamera preserved native baseline");
                AssertNear(
                    initialSize * 4d,
                    sizeAfterRealReset,
                    "repeated real SetEnvCamera Postfix reapply without compounding");

                object maxOneConfig =
                    Activator.CreateInstance(configType)
                    ?? throw new InvalidOperationException(
                        "Zoom max-one config could not be created.");
                SetPublicProperty(
                    maxOneConfig,
                    "Enabled",
                    true);
                SetPublicProperty(
                    maxOneConfig,
                    "MaxViewScale",
                    1d);
                configure.Invoke(
                    productRuntime,
                    new[]
                    {
                        maxOneConfig,
                        (object)"G5 maximum reduced to 1x"
                    });
                double sizeAfterMaxOne =
                    ReadPositiveDouble(
                        mainCamera,
                        "orthographicSize");
                AssertNear(
                    initialSize,
                    sizeAfterMaxOne,
                    "MaxViewScale to 1 native restoration");
                AssertCameraControllerSnapshot(
                    cameraController,
                    initialCamHeight,
                    initialXRangeMin,
                    initialXRangeMax,
                    initialYRangeMin,
                    initialYRangeMax,
                    "MaxViewScale to 1 native derived-state restoration");

                configure.Invoke(
                    productRuntime,
                    new[]
                    {
                        enabledConfig,
                        (object)"G5 maximum restored"
                    });
                if (!InvokeBoolean(
                        setViewScale,
                        productRuntime,
                        4d,
                        "G5 before config disable"))
                {
                    throw new InvalidOperationException(
                        "Zoom ProductNative could not restore the reviewed 4x state before config disable.");
                }
                refreshResolution.Invoke(
                    cameraController,
                    Array.Empty<object>());

                object disabledConfig =
                    Activator.CreateInstance(configType)
                    ?? throw new InvalidOperationException(
                        "Zoom disabled config could not be created.");
                SetPublicProperty(
                    disabledConfig,
                    "Enabled",
                    false);
                needsReenable = true;
                configure.Invoke(
                    productRuntime,
                    new[]
                    {
                        disabledConfig,
                        (object)"G5 config disable"
                    });
                double sizeAfterDisable =
                    ReadPositiveDouble(
                        mainCamera,
                        "orthographicSize");
                AssertNear(
                    initialSize,
                    sizeAfterDisable,
                    "config-disable vanilla restoration");
                AssertCameraControllerSnapshot(
                    cameraController,
                    initialCamHeight,
                    initialXRangeMin,
                    initialXRangeMax,
                    initialYRangeMin,
                    initialYRangeMax,
                    "config-disable native derived-state restoration");
                Batch6HarmonyOwnerInventory disabled =
                    ObserveZoomProductOwnerForFixture();
                if (disabled.ExactOwnerPatchCount != 0 ||
                    disabled.ExactOwnerTargetCount != 0 ||
                    ReadStaticCallbackRuntimePresent(
                        ZoomProductAssemblyName,
                        ZoomProductCallbackTypeName))
                {
                    throw new InvalidOperationException(
                        "Zoom config disable did not clear the exact owner " +
                        "and callback runtime. " +
                        disabled.Details);
                }

                configure.Invoke(
                    productRuntime,
                    new[]
                    {
                        enabledConfig,
                        (object)"G5 config re-enable"
                    });
                needsReenable = false;
                Batch6HarmonyOwnerInventory reenabled =
                    ObserveZoomProductOwnerForFixture();
                if (!reenabled.IsComplete ||
                    reenabled.ExactOwnerPatchCount != 3 ||
                    !ReadStaticCallbackRuntimePresent(
                        ZoomProductAssemblyName,
                        ZoomProductCallbackTypeName))
                {
                    throw new InvalidOperationException(
                        "Zoom config re-enable did not restore exactly three " +
                        "ProductNative patches and callback runtime. " +
                        reenabled.Details);
                }
                if (!InvokeBoolean(
                        setViewScale,
                        productRuntime,
                        4d,
                        "G5 title-boundary setup"))
                {
                    throw new InvalidOperationException(
                        "Zoom could not prepare the reviewed 4x " +
                        "ReturnedToTitle restoration boundary.");
                }
                double titlePendingSize =
                    ReadPositiveDouble(
                        mainCamera,
                        "orthographicSize");
                AssertNear(
                    initialSize * 4d,
                    titlePendingSize,
                    "pre-title 4x setup");
                refreshResolution.Invoke(
                    cameraController,
                    Array.Empty<object>());
                double titlePendingCamHeight =
                    ReadNestedFiniteDouble(
                        cameraController,
                        "CamSize",
                        "y");
                AssertNear(
                    initialCamHeight,
                    titlePendingCamHeight,
                    "pre-title native controller baseline");
                int roots =
                    runtime.CountCoreOwnerRoots(
                        ZoomProductOwnerId);
                string details =
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "route=ProductNative; scale2={0:0.###}; " +
                        "direct1={1:0.###}; scale4={2:0.###}; " +
                        "refresh4to2={3:0.###}; " +
                        "resetVanilla={4:0.###}; " +
                        "afterRealSetEnvCameraTwice={5:0.###}; " +
                        "maxTo1Restore={6:0.###}; " +
                        "configDisableRestore={7:0.###}; " +
                        "exactOwnerPatches=3; compatibilityOwner=0; " +
                        "callback=1; titlePendingScale=4; " +
                        "titlePendingSize={8:0.###}; " +
                        "camHeight1={9:0.###}; " +
                        "camHeight2={10:0.###}; " +
                        "camHeight4={11:0.###}; " +
                        "titlePendingCamHeight={12:0.###}; " +
                        "roots={13}; target={14}",
                        sizeAt2x,
                        sizeAfterDirectOne,
                        sizeAt4x,
                        sizeAfterActiveRefreshTo2x,
                        resetVanillaSize,
                        sizeAfterRealReset,
                        sizeAfterMaxOne,
                        sizeAfterDisable,
                        titlePendingSize,
                        initialCamHeight,
                        derivedHeightAt2x,
                        derivedHeightAt4x,
                        titlePendingCamHeight,
                        roots,
                        reenabled.Details);
                runtime.RuntimeMonitor.Log(
                    "Smoke exercise ZoomProductNative OK " +
                    details +
                    ".");
                runtime.SetHookStatus(
                    "Smoke.ZoomProductNative",
                    "verified",
                    "Zoom ProductNative 4x -> native refresh -> 2x; " +
                    "real DolocAPI.SetEnvCamera; config disable/re-enable",
                    details);
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError(
                    "DTMAPI.GameBridge",
                    "Smoke Zoom ProductNative exercise failed.",
                    ex.ToString());
                runtime.SetHookStatus(
                    "Smoke.ZoomProductNative",
                    "failed",
                    "Zoom ProductNative exact-owner behavior",
                    ex.GetType().Name +
                    ": " +
                    ex.Message);
            }
            finally
            {
                if (needsReenable &&
                    productRuntime != null &&
                    enabledConfig != null)
                {
                    try
                    {
                        RequireInstanceMethod(
                                productRuntime.GetType(),
                                "Configure",
                                2)
                            .Invoke(
                                productRuntime,
                                new[]
                                {
                                    enabledConfig,
                                    (object)"G5 failure cleanup re-enable"
                                });
                    }
                    catch (Exception cleanupFailure)
                    {
                        runtime.Diagnostics.RecordError(
                            "DTMAPI.GameBridge",
                            "Zoom G5 failure cleanup could not re-enable " +
                            "the reviewed product.",
                            cleanupFailure.ToString());
                    }
                }
            }
        }

        private static Batch6HarmonyOwnerInventory
            ObserveZoomProductOwnerForFixture() =>
            Batch6AdvancedHarmonyOwnerObserver.Observe(
                ZoomProductAssemblyName,
                ZoomProductHarmonyOwner,
                ZoomProductHarmonyTargets);

        private static MethodInfo RequireInstanceMethod(
            Type type,
            string name,
            int parameterCount) =>
            type.GetMethods(
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance)
                .Single(
                    method =>
                        method.Name == name &&
                        method.GetParameters().Length ==
                            parameterCount);

        private static void SetPublicProperty(
            object target,
            string name,
            object value)
        {
            PropertyInfo property =
                target.GetType().GetProperty(
                    name,
                    BindingFlags.Public |
                    BindingFlags.Instance)
                ?? throw new MissingMemberException(
                    target.GetType().FullName,
                    name);
            property.SetValue(
                target,
                value,
                null);
        }

        private static bool InvokeBoolean(
            MethodInfo method,
            object target,
            params object[] arguments) =>
            method.Invoke(
                target,
                arguments) is bool success &&
            success;

        private static double ReadPositiveDouble(
            object target,
            string member)
        {
            double value =
                Convert.ToDouble(
                    ReadMember(target, member),
                    CultureInfo.InvariantCulture);
            if (value <= 0d ||
                double.IsNaN(value) ||
                double.IsInfinity(value))
            {
                throw new InvalidOperationException(
                    target.GetType().FullName +
                    "." +
                    member +
                    " was not a positive finite camera size.");
            }
            return value;
        }

        private static double ReadNestedFiniteDouble(
            object target,
            string member,
            string nestedMember)
        {
            object value =
                ReadMember(target, member)
                ?? throw new MissingMemberException(
                    target.GetType().FullName,
                    member);
            double result =
                Convert.ToDouble(
                    ReadMember(value, nestedMember),
                    CultureInfo.InvariantCulture);
            if (double.IsNaN(result) ||
                double.IsInfinity(result))
            {
                throw new InvalidOperationException(
                    target.GetType().FullName +
                    "." +
                    member +
                    "." +
                    nestedMember +
                    " was not finite.");
            }
            return result;
        }

        private static void AssertCameraControllerSnapshot(
            object cameraController,
            double camHeight,
            double xRangeMin,
            double xRangeMax,
            double yRangeMin,
            double yRangeMax,
            string label)
        {
            AssertNear(
                camHeight,
                ReadNestedFiniteDouble(
                    cameraController,
                    "CamSize",
                    "y"),
                label + " camSize.y");
            AssertNear(
                xRangeMin,
                ReadNestedFiniteDouble(
                    cameraController,
                    "xRange",
                    "x"),
                label + " xRange.min");
            AssertNear(
                xRangeMax,
                ReadNestedFiniteDouble(
                    cameraController,
                    "xRange",
                    "y"),
                label + " xRange.max");
            AssertNear(
                yRangeMin,
                ReadNestedFiniteDouble(
                    cameraController,
                    "yRange",
                    "x"),
                label + " yRange.min");
            AssertNear(
                yRangeMax,
                ReadNestedFiniteDouble(
                    cameraController,
                    "yRange",
                    "y"),
                label + " yRange.max");
        }

        private static void AssertNear(
            double expected,
            double actual,
            string label)
        {
            double tolerance =
                Math.Max(0.001d, expected * 0.001d);
            if (Math.Abs(expected - actual) > tolerance)
            {
                throw new InvalidOperationException(
                    label +
                    " mismatch expected=" +
                    expected.ToString(
                        "0.###",
                        CultureInfo.InvariantCulture) +
                    ", actual=" +
                    actual.ToString(
                        "0.###",
                        CultureInfo.InvariantCulture) +
                    ".");
            }
        }
    }
}
