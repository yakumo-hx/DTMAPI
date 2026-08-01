using System;
using System.Linq;
using System.Reflection;
using DTMAPI.Abstractions;
using DTMAPI.GameBridge.DolocTown;
using DTMAPI.Zoom;
using HarmonyLib;

public static class DolocAPI
{
    public static object mainCamera = new FixtureCamera();
    public static object cameraController = new FixtureCameraController();

    public static void SetEnvCamera(
        UnityEngine.Vector2 position,
        UnityEngine.Vector2 size,
        bool immediate,
        bool refresh,
        bool preserve)
    {
    }
}

public sealed class FixtureCamera
{
    public float orthographicSize = 10f;
}

public sealed class FixtureCameraController
{
    public float ObservedOrthographicSize { get; private set; }

    public int RefreshCalls { get; private set; }

    public Exception? Failure { get; set; }

    public void RefreshResolution()
    {
        RefreshCalls++;
        ObservedOrthographicSize =
            ((FixtureCamera)DolocAPI.mainCamera)
                .orthographicSize;
        if (Failure != null)
            throw Failure;
    }
}

namespace UnityEngine
{
    public struct Vector2
    {
        public float x;
        public float y;
    }
}

namespace DTMAPI.Zoom
{
    internal static class ZoomNativeAccess
    {
        internal static bool TryReadOrthographicSize(
            out double value)
        {
            value =
                ((FixtureCamera)DolocAPI.mainCamera)
                    .orthographicSize;
            return value > 0d;
        }

        internal static bool TryWriteOrthographicSize(
            double value)
        {
            ((FixtureCamera)DolocAPI.mainCamera)
                .orthographicSize = (float)value;
            return true;
        }

        internal static bool IsMainCameraDefinitelyAbsent() =>
            DolocAPI.mainCamera == null;
    }
}

namespace DTMAPI.ZoomHarmonyOwnerFixture
{
    internal static class Program
    {
        private const string ObserverOwner =
            "dtmapi.tests.zoom.observer";
        private static readonly MethodInfo Target =
            ZoomHookInstaller.ResolveTarget();
        private static readonly MethodInfo RefreshTarget =
            ZoomHookInstaller.ResolveRefreshResolutionTarget();
        private static readonly HarmonyMethod NoOpPostfix =
            new HarmonyMethod(
                typeof(Program).GetMethod(
                    nameof(Postfix),
                    BindingFlags.NonPublic |
                    BindingFlags.Static) ??
                throw new MissingMethodException(
                    typeof(Program).FullName,
                    nameof(Postfix)));

        private static int Main()
        {
            try
            {
                ProductFirstRefusesCompatibility();
                CompatibilityFirstRefusesProduct();
                FailedAtomicInstallRollsBackRealOwner();
                ExactProductUnpatchPreservesUnrelatedOwner();
                NativeRefreshUsesBaselineAndPreservesException();
                Console.WriteLine(
                    "ZoomHarmonyOwnerFixture: OK");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                Cleanup(
                    ZoomProductContract.HarmonyOwner);
                Cleanup(
                    ZoomProductContract.CompatibilityOwner);
                Cleanup(ObserverOwner);
                return 1;
            }
        }

        private static void ProductFirstRefusesCompatibility()
        {
            CleanupAll();
            ZoomHookInstaller installer =
                CreateInstaller(out ZoomNativeRuntime runtime);
            installer.InstallAtomically(runtime);
            Assert(
                OwnerCount(
                    ZoomProductContract.HarmonyOwner) == 1,
                "Product-first install did not publish exactly one SetEnvCamera owner.");
            Assert(
                installer.InstalledPatchCount == 3 &&
                TotalOwnerPatchCount(
                    ZoomProductContract.HarmonyOwner) == 3,
                "Product-first install did not publish all three real camera patches.");
            CameraCompatibilityInstallDecision decision =
                CameraCompatibilityHookOwnership.DecideInstall(
                    managedProductOwnerPresent:
                        OwnerCount(
                            ZoomProductContract
                                .HarmonyOwner) > 0,
                    compatibilityDemandPresent: true);
            Assert(
                decision ==
                    CameraCompatibilityInstallDecision
                        .RejectManagedProductOwner &&
                OwnerCount(
                    ZoomProductContract
                        .CompatibilityOwner) == 0,
                "Product-first physical owner did not fail the compatibility install closed.");
            installer.UnpatchOwnedHooks(runtime);
            Assert(
                TotalOwnerPatchCount(
                    ZoomProductContract.HarmonyOwner) == 0,
                "Product exact-owner cleanup left a real patch.");
        }

        private static void CompatibilityFirstRefusesProduct()
        {
            CleanupAll();
            Install(
                ZoomProductContract.CompatibilityOwner);
            ZoomHookInstaller installer =
                CreateInstaller(out ZoomNativeRuntime runtime);
            AssertThrows<InvalidOperationException>(
                () => installer.InstallAtomically(runtime),
                "Compatibility-first physical owner did not reject ProductNative Zoom.");
            Assert(
                OwnerCount(
                    ZoomProductContract.CompatibilityOwner) ==
                    1 &&
                OwnerCount(
                    ZoomProductContract.HarmonyOwner) == 0,
                "Rejected product install changed the compatibility owner.");
            Cleanup(
                ZoomProductContract.CompatibilityOwner);
        }

        private static void FailedAtomicInstallRollsBackRealOwner()
        {
            CleanupAll();
            bool attached = false;
            bool detached = false;
            AssertThrows<InvalidOperationException>(
                () => ZoomHookTransaction.Install(
                    () => attached = true,
                    () => Install(
                        ZoomProductContract.HarmonyOwner),
                    () => false,
                    () => Cleanup(
                        ZoomProductContract.HarmonyOwner),
                    () => detached = true),
                "A failed exact-owner proof did not surface.");
            Assert(
                attached &&
                detached &&
                OwnerCount(
                    ZoomProductContract.HarmonyOwner) == 0,
                "Atomic failure did not detach and roll back the real Harmony owner.");
        }

        private static void ExactProductUnpatchPreservesUnrelatedOwner()
        {
            CleanupAll();
            ZoomHookInstaller installer =
                CreateInstaller(out ZoomNativeRuntime runtime);
            installer.InstallAtomically(runtime);
            Install(ObserverOwner);
            installer.UnpatchOwnedHooks(runtime);
            Assert(
                TotalOwnerPatchCount(
                    ZoomProductContract.HarmonyOwner) == 0 &&
                OwnerCount(ObserverOwner) == 1,
                "Exact ProductNative unpatch removed an unrelated physical Harmony owner.");
            Cleanup(ObserverOwner);
        }

        private static void NativeRefreshUsesBaselineAndPreservesException()
        {
            CleanupAll();
            var camera = new FixtureCamera();
            var controller = new FixtureCameraController();
            DolocAPI.mainCamera = camera;
            DolocAPI.cameraController = controller;

            ZoomHookInstaller installer =
                CreateInstaller(out ZoomNativeRuntime runtime);
            runtime.Configure(
                new ZoomConfig
                {
                    Enabled = true,
                    MaxViewScale = 4d,
                    Step = 1d
                },
                "fixture");
            Assert(
                runtime.SetViewScale(4d, "fixture-4x") &&
                Approximately(camera.orthographicSize, 40f),
                "4x did not write the orthographic presentation size.");

            int refreshCalls = controller.RefreshCalls;
            Assert(
                runtime.SetViewScale(2d, "fixture-2x") &&
                controller.RefreshCalls == refreshCalls,
                "A Zoom scale transition invoked native RefreshResolution.");
            Assert(
                runtime.SetViewScale(4d, "fixture-back-to-4x"),
                "The fixture could not restore 4x before native refresh.");

            controller.RefreshResolution();
            Assert(
                controller.RefreshCalls == refreshCalls + 1 &&
                Approximately(controller.ObservedOrthographicSize, 10f) &&
                Approximately(camera.orthographicSize, 40f),
                "Native RefreshResolution did not observe the 1x baseline and return to 4x.");

            Exception expected =
                new InvalidOperationException(
                    "fixture-native-refresh-failure");
            controller.Failure = expected;
            Exception? observed = null;
            try
            {
                controller.RefreshResolution();
            }
            catch (Exception ex)
            {
                observed = ex;
            }
            Assert(
                ReferenceEquals(observed, expected) &&
                Approximately(controller.ObservedOrthographicSize, 10f) &&
                Approximately(camera.orthographicSize, 40f),
                "Refresh finalization did not preserve the native exception and active scale.");

            controller.Failure = null;
            runtime.DeactivateOwner("fixture-cleanup");
            Assert(
                Approximately(camera.orthographicSize, 10f) &&
                TotalOwnerPatchCount(
                    ZoomProductContract.HarmonyOwner) == 0,
                "Owner cleanup did not restore the baseline and remove all Zoom patches.");
        }

        private static ZoomHookInstaller CreateInstaller(
            out ZoomNativeRuntime runtime)
        {
            var installer =
                new ZoomHookInstaller(
                    NullMonitor.Instance);
            runtime =
                new ZoomNativeRuntime(
                    NullMonitor.Instance,
                    installer);
            return installer;
        }

        private static void Install(string owner)
        {
            new Harmony(owner).Patch(
                Target,
                postfix: NoOpPostfix);
        }

        private static void Cleanup(string owner) =>
            Harmony.UnpatchID(owner);

        private static void CleanupAll()
        {
            Cleanup(
                ZoomProductContract.HarmonyOwner);
            Cleanup(
                ZoomProductContract.CompatibilityOwner);
            Cleanup(ObserverOwner);
        }

        private static int OwnerCount(string owner)
        {
            Patches? info =
                Harmony.GetPatchInfo(Target);
            return info == null
                ? 0
                : info.Owners.Count(candidate =>
                    candidate.Equals(
                        owner,
                        StringComparison.Ordinal));
        }

        private static int TotalOwnerPatchCount(string owner) =>
            PatchCount(Target, owner) +
            PatchCount(RefreshTarget, owner);

        private static int PatchCount(
            MethodBase method,
            string owner)
        {
            Patches? info = Harmony.GetPatchInfo(method);
            if (info == null)
                return 0;
            return info.Prefixes.Count(patch =>
                       patch.owner.Equals(owner, StringComparison.Ordinal)) +
                   info.Postfixes.Count(patch =>
                       patch.owner.Equals(owner, StringComparison.Ordinal)) +
                   info.Transpilers.Count(patch =>
                       patch.owner.Equals(owner, StringComparison.Ordinal)) +
                   info.Finalizers.Count(patch =>
                       patch.owner.Equals(owner, StringComparison.Ordinal));
        }

        private static bool Approximately(float left, float right) =>
            Math.Abs(left - right) < 0.001f;

        private static void Postfix()
        {
        }

        private static void Assert(
            bool condition,
            string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private static void AssertThrows<TException>(
            Action action,
            string message)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }
            throw new InvalidOperationException(message);
        }
    }
}
