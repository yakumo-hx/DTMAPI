using System;
using System.Diagnostics;
using System.IO;
using DTMAPI.Abstractions;
using DTMAPI.GameBridge.DolocTown;
using DTMAPI.Zoom;

namespace DTMAPI.UnitTests
{
    internal static class ZoomProductTests
    {
        internal static void RunAll()
        {
            ProductAndCompatibilityOwnersRejectBothLoadOrders();
            DisabledCompatibilityDemandRemainsDormant();
            ProductConfigClampsTheFrozenPlayableRange();
            NativeScaleReappliesAndEveryLifecycleRestores();
            NativeRefreshUsesBaselineAndFinalizerRestoresScale();
            MaximumClampFailuresRemainVisibleAndDoNotRebaseline();
            CleanupUnpatchesEvenWhenNativeRestoreFails();
            LiveCameraReadFailureDoesNotMasqueradeAsDestroyed();
            AbsentCameraAllowsSafeCleanupAndExactUnpatch();
            FailedNativeWriteRollsBackTheRequestedScale();
            AtomicInstallFailureRollsBackAndReportsResidue();
            PhysicalHarmonyFixtureCoversBothOrdersRollbackAndExactUnpatch();
        }

        private static void PhysicalHarmonyFixtureCoversBothOrdersRollbackAndExactUnpatch()
        {
            string executable =
                Path.Combine(
                    FindRepositoryRoot(),
                    "tests",
                    "DTMAPI.UnitTests",
                    "Fixtures",
                    "ZoomHarmonyOwnerFixture",
                    "bin",
                    "Release",
                    "net48",
                    "ZoomHarmonyOwnerFixture.exe");
            if (!File.Exists(executable))
            {
                throw new FileNotFoundException(
                    "The focused Unit build did not produce the physical Zoom Harmony owner fixture.",
                    executable);
            }

            var start = new ProcessStartInfo
            {
                FileName = executable,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using Process process =
                Process.Start(start) ??
                throw new InvalidOperationException(
                    "Could not start the physical Zoom Harmony owner fixture.");
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            if (!process.WaitForExit(30000))
            {
                try
                {
                    process.Kill();
                }
                catch
                {
                }
                throw new TimeoutException(
                    "The physical Zoom Harmony owner fixture did not exit within 30 seconds.");
            }
            Assert(
                process.ExitCode == 0 &&
                output.Contains(
                    "ZoomHarmonyOwnerFixture: OK",
                    StringComparison.Ordinal),
                "The physical Zoom Harmony owner fixture failed. exit=" +
                process.ExitCode +
                ", output=" +
                output +
                ", error=" +
                error);
        }

        private static void ProductAndCompatibilityOwnersRejectBothLoadOrders()
        {
            Assert(
                ZoomHookOwnership.DecideInstall(
                    compatibilityOwnerPresent: false,
                    productOwnerPresent: false) ==
                ZoomInstallDecision.Install,
                "A clean exact target must admit the Zoom ProductNative owner.");
            Assert(
                ZoomHookOwnership.DecideInstall(
                    compatibilityOwnerPresent: true,
                    productOwnerPresent: false) ==
                ZoomInstallDecision.RejectCompatibilityOwner,
                "Compatibility-first ordering must make the Zoom product fail closed.");
            Assert(
                CameraCompatibilityHookOwnership.DecideInstall(
                    managedProductOwnerPresent: true,
                    compatibilityDemandPresent: true) ==
                CameraCompatibilityInstallDecision
                    .RejectManagedProductOwner,
                "Product-first ordering must remove/refuse frozen Camera compatibility demand before its Hook installs.");
            Assert(
                ZoomProductContract.CompatibilityOwner ==
                CameraCompatibilityHookBridge.HarmonyOwner,
                "Product and compatibility shells must coordinate on the same exact compatibility owner identity.");
        }

        private static void DisabledCompatibilityDemandRemainsDormant()
        {
            Assert(
                CameraCompatibilityHookOwnership.DecideInstall(
                    managedProductOwnerPresent: false,
                    compatibilityDemandPresent: false) ==
                CameraCompatibilityInstallDecision.Dormant,
                "A disabled or cleaned-up frozen Camera owner must not leave a Hook demand.");
        }

        private static void ProductConfigClampsTheFrozenPlayableRange()
        {
            var config = new ZoomConfig
            {
                Enabled = true,
                MaxViewScale = 99d,
                Step = 0d,
                IncreaseKey = null!,
                DecreaseKey = null!
            };
            ZoomConfig normalized = config.Copy();
            Assert(
                Math.Abs(normalized.MaxViewScale - 4d) < 0.001d,
                "Zoom must preserve the frozen 4x playable maximum.");
            Assert(
                Math.Abs(normalized.Step - 0.05d) < 0.001d,
                "Zoom must clamp the configured step to its supported minimum.");
            Assert(
                normalized.IncreaseKey == string.Empty &&
                normalized.DecreaseKey == string.Empty,
                "Zoom config normalization must not retain null key strings.");
        }

        private static void NativeScaleReappliesAndEveryLifecycleRestores()
        {
            var hooks = new FakeHookOwner();
            var runtime = new ZoomNativeRuntime(
                NullMonitor.Instance,
                hooks);
            double nativeSize = 10d;
            runtime.ConfigureNativeAccessForTests(
                () => nativeSize,
                value =>
                {
                    nativeSize = value;
                    return true;
                });
            runtime.Configure(
                new ZoomConfig
                {
                    Enabled = true,
                    MaxViewScale = 4d,
                    Step = 1d
                },
                "focused unit");
            Assert(
                hooks.IsInstalled &&
                runtime.InstalledPatchCount == 3,
                "Enabled Zoom must install exactly the SetEnv postfix plus RefreshResolution Prefix/Finalizer.");
            Assert(
                runtime.SetViewScale(2d, "focused apply") &&
                Math.Abs(nativeSize - 20d) < 0.001d,
                "A 2x playable view must write the captured native orthographic size exactly once.");

            runtime.OnEnvironmentReset();
            runtime.OnEnvironmentReset();
            Assert(
                Math.Abs(nativeSize - 20d) < 0.001d &&
                Math.Abs(runtime.VanillaOrthographicSize - 10d) <
                0.001d,
                "Repeated real SetEnvCamera callbacks that leave orthographic size unchanged must preserve the original native baseline.");

            Assert(
                runtime.Step(-1, "minus-key to vanilla") &&
                Math.Abs(nativeSize - 10d) < 0.001d &&
                Math.Abs(runtime.CurrentViewScale - 1d) <
                0.001d,
                "The minus-key Step path must write the captured native 1x size instead of only changing the internal multiplier.");

            Assert(
                runtime.SetViewScale(4d, "before max clamp") &&
                Math.Abs(nativeSize - 40d) < 0.001d,
                "The max-clamp fixture must first own a 4x camera write.");
            runtime.Configure(
                new ZoomConfig
                {
                    Enabled = true,
                    MaxViewScale = 1d,
                    Step = 1d
                },
                "maximum reduced to 1x");
            Assert(
                Math.Abs(nativeSize - 10d) < 0.001d &&
                Math.Abs(runtime.CurrentViewScale - 1d) <
                0.001d,
                "Reducing MaxViewScale to 1 must immediately restore the captured native camera size.");

            runtime.Configure(
                new ZoomConfig
                {
                    Enabled = true,
                    MaxViewScale = 4d,
                    Step = 1d
                },
                "maximum restored");
            Assert(
                runtime.SetViewScale(
                    2d,
                    "before genuine native reset"),
                "The genuine native-reset fixture must re-enter 2x.");
            nativeSize = 12d;
            runtime.OnEnvironmentReset();
            Assert(
                Math.Abs(nativeSize - 24d) < 0.001d &&
                Math.Abs(runtime.VanillaOrthographicSize - 12d) <
                0.001d,
                "SetEnvCamera Postfix must capture the new native baseline and reapply the current scale.");
            runtime.OnEnvironmentReset();
            Assert(
                Math.Abs(nativeSize - 24d) < 0.001d &&
                Math.Abs(runtime.VanillaOrthographicSize - 12d) <
                0.001d,
                "A repeated no-size-change callback after genuine native rebaselining must not compound the current scale.");

            runtime.ResetToVanilla("ReturnedToTitle");
            Assert(
                Math.Abs(nativeSize - 12d) < 0.001d &&
                Math.Abs(runtime.CurrentViewScale - 1d) < 0.001d,
                "ReturnedToTitle must restore the latest native baseline.");

            runtime.SetViewScale(2d, "before config disable");
            runtime.Configure(
                new ZoomConfig { Enabled = false },
                "config disabled");
            Assert(
                Math.Abs(nativeSize - 12d) < 0.001d &&
                !hooks.IsInstalled &&
                runtime.InstalledPatchCount == 0,
                "Config disable must restore native state and remove the exact product owner.");

            runtime.Configure(
                new ZoomConfig { Enabled = true },
                "config enabled");
            runtime.SetViewScale(2d, "before owner cleanup");
            runtime.DeactivateOwner("focused unit");
            Assert(
                Math.Abs(nativeSize - 12d) < 0.001d &&
                !hooks.IsInstalled,
                "Loader owner deactivation must restore native state and leave zero ProductNative Hooks.");
        }

        private static void CleanupUnpatchesEvenWhenNativeRestoreFails()
        {
            var hooks = new FakeHookOwner();
            var runtime = new ZoomNativeRuntime(
                NullMonitor.Instance,
                hooks);
            double nativeSize = 10d;
            bool failWrites = false;
            runtime.ConfigureNativeAccessForTests(
                () => nativeSize,
                value =>
                {
                    if (failWrites)
                        return false;
                    nativeSize = value;
                    return true;
                });
            runtime.Configure(
                new ZoomConfig { Enabled = true },
                "focused unit");
            Assert(
                runtime.SetViewScale(2d, "capture baseline"),
                "The cleanup failure fixture must first own native state.");
            failWrites = true;
            bool threw = false;
            try
            {
                runtime.DeactivateOwner(
                    "injected restore failure");
            }
            catch (InvalidOperationException)
            {
                threw = true;
            }
            Assert(
                threw &&
                !hooks.IsInstalled &&
                hooks.UnpatchCalls == 1,
                "A restore failure must stay explicit while exact-owner unpatch still runs.");
        }

        private static void NativeRefreshUsesBaselineAndFinalizerRestoresScale()
        {
            var hooks = new FakeHookOwner();
            var runtime = new ZoomNativeRuntime(
                NullMonitor.Instance,
                hooks);
            double nativeSize = 10d;
            double derivedCamHeight = 20d;
            runtime.ConfigureNativeAccessForTests(
                () => nativeSize,
                value =>
                {
                    nativeSize = value;
                    return true;
                });
            runtime.Configure(
                new ZoomConfig
                {
                    Enabled = true,
                    MaxViewScale = 4d,
                    Step = 1d
                },
                "derived-state fixture");

            Assert(
                runtime.SetViewScale(4d, "active-scale refresh setup") &&
                Math.Abs(nativeSize - 40d) < 0.001d &&
                Math.Abs(derivedCamHeight - 20d) < 0.001d,
                "Changing to 4x must alter only orthographicSize and must not proactively refresh native controller state.");

            ZoomNativeRefreshState state =
                runtime.BeforeNativeRefreshResolution();
            Assert(
                Math.Abs(nativeSize - 10d) < 0.001d,
                "RefreshResolution Prefix must expose the exact 1x native baseline to the original method.");
            derivedCamHeight = nativeSize * 2d;
            Exception? result = runtime.AfterNativeRefreshResolution(
                state,
                nativeException: null);
            Assert(
                result == null &&
                Math.Abs(nativeSize - 40d) < 0.001d &&
                Math.Abs(derivedCamHeight - 20d) < 0.001d,
                "RefreshResolution Finalizer must restore 4x presentation while leaving native derived state at its own 1x baseline.");

            Assert(
                runtime.SetViewScale(
                    2d,
                    "4x native refresh to 2x") &&
                Math.Abs(runtime.CurrentViewScale - 2d) < 0.001d &&
                Math.Abs(nativeSize - 20d) < 0.001d &&
                Math.Abs(derivedCamHeight - 20d) < 0.001d,
                "4x -> native refresh -> 2x must change only final orthographicSize; native controller fields remain at their native baseline.");

            runtime.SetViewScale(4d, "native rebaseline setup");
            state = runtime.BeforeNativeRefreshResolution();
            nativeSize = 12d;
            derivedCamHeight = nativeSize * 2d;
            runtime.AfterNativeRefreshResolution(state, nativeException: null);
            Assert(
                Math.Abs(runtime.VanillaOrthographicSize - 12d) < 0.001d &&
                Math.Abs(nativeSize - 48d) < 0.001d &&
                Math.Abs(derivedCamHeight - 24d) < 0.001d,
                "A genuine native refresh may establish a new 1x baseline before the active multiplier is reapplied.");

            var nativeFailure = new InvalidOperationException(
                "native refresh failure");
            state = runtime.BeforeNativeRefreshResolution();
            Exception? propagated = runtime.AfterNativeRefreshResolution(
                state,
                nativeFailure);
            Assert(
                ReferenceEquals(propagated, nativeFailure) &&
                Math.Abs(nativeSize - 48d) < 0.001d,
                "Finalizer must restore active presentation after an original exception and return that exact native exception unchanged.");

            runtime.ResetToVanilla("ReturnedToTitle");
            Assert(
                Math.Abs(nativeSize - 12d) < 0.001d &&
                Math.Abs(derivedCamHeight - 24d) < 0.001d,
                "ReturnedToTitle must restore only the captured orthographic baseline and leave native controller state untouched.");
            runtime.DeactivateOwner("Loader owner deactivation");
            Assert(
                Math.Abs(nativeSize - 12d) < 0.001d &&
                !hooks.IsInstalled,
                "Loader owner deactivation must leave the native baseline and zero exact-owner patches.");
        }

        private static void MaximumClampFailuresRemainVisibleAndDoNotRebaseline()
        {
            AssertMaximumClampFailure(
                failRead: true,
                failWrite: false,
                expectedStatus: "pending-camera");
            AssertMaximumClampFailure(
                failRead: false,
                failWrite: true,
                expectedStatus: "failed-closed");
        }

        private static void AssertMaximumClampFailure(
            bool failRead,
            bool failWrite,
            string expectedStatus)
        {
            var hooks = new FakeHookOwner();
            var runtime = new ZoomNativeRuntime(
                NullMonitor.Instance,
                hooks);
            double nativeSize = 10d;
            bool injectReadFailure = false;
            bool injectWriteFailure = false;
            runtime.ConfigureNativeAccessForTests(
                () =>
                {
                    if (injectReadFailure)
                        return null;
                    return nativeSize;
                },
                value =>
                {
                    if (injectWriteFailure)
                        return false;
                    nativeSize = value;
                    return true;
                });
            runtime.Configure(
                new ZoomConfig
                {
                    Enabled = true,
                    MaxViewScale = 4d
                },
                "failure fixture");
            Assert(
                runtime.SetViewScale(4d, "failure setup") &&
                Math.Abs(nativeSize - 40d) < 0.001d,
                "The maximum-clamp failure fixture must begin from a coherent 4x state.");

            injectReadFailure = failRead;
            injectWriteFailure = failWrite;
            runtime.Configure(
                new ZoomConfig
                {
                    Enabled = true,
                    MaxViewScale = 1d
                },
                "injected maximum clamp failure");
            Assert(
                runtime.Status == expectedStatus &&
                Math.Abs(runtime.CurrentViewScale - 4d) < 0.001d &&
                Math.Abs(runtime.VanillaOrthographicSize - 10d) < 0.001d &&
                Math.Abs(nativeSize - 40d) < 0.001d,
                "A MaxViewScale-to-1 read/write failure must remain visible, preserve the coherent prior 4x scale, and retain the true 1x baseline.");

            injectReadFailure = false;
            injectWriteFailure = false;
            runtime.OnEnvironmentReset();
            Assert(
                runtime.Status == "vanilla" &&
                Math.Abs(runtime.CurrentViewScale - 1d) < 0.001d &&
                Math.Abs(nativeSize - 10d) < 0.001d &&
                Math.Abs(runtime.VanillaOrthographicSize - 10d) < 0.001d,
                "A later native environment callback must retry the configured maximum clamp and restore 1x without registering the failed live value as a new baseline.");
            runtime.DeactivateOwner("failure fixture cleanup");
        }

        private static void LiveCameraReadFailureDoesNotMasqueradeAsDestroyed()
        {
            var hooks = new FakeHookOwner();
            var runtime = new ZoomNativeRuntime(
                NullMonitor.Instance,
                hooks);
            double nativeSize = 10d;
            bool failRead = false;
            runtime.ConfigureNativeAccessForTests(
                () =>
                {
                    if (failRead)
                    {
                        throw new InvalidOperationException(
                            "injected live camera read failure");
                    }
                    return nativeSize;
                },
                value =>
                {
                    nativeSize = value;
                    return true;
                },
                mainCameraAbsent: () => false);
            runtime.Configure(
                new ZoomConfig { Enabled = true },
                "live camera read fixture");
            Assert(
                runtime.SetViewScale(2d, "capture live baseline"),
                "The live-read failure fixture must first capture and apply a native baseline.");
            failRead = true;
            bool threw = false;
            try
            {
                runtime.DeactivateOwner(
                    "live camera read failure");
            }
            catch (InvalidOperationException ex)
            {
                threw = ex.Message.Contains(
                    "live main camera",
                    StringComparison.OrdinalIgnoreCase);
            }
            Assert(
                threw &&
                !hooks.IsInstalled &&
                hooks.UnpatchCalls == 1 &&
                Math.Abs(runtime.VanillaOrthographicSize - 10d) <
                0.001d &&
                Math.Abs(runtime.AppliedOrthographicSize - 20d) <
                0.001d &&
                runtime.Status ==
                    "restore-failed-owner-unpatched",
                "A read failure on a camera that is still live must remain explicit, retain its retryable snapshot, and still unpatch the exact owner.");
            failRead = false;
            runtime.DeactivateOwner(
                "retry live camera restore");
            Assert(
                Math.Abs(nativeSize - 10d) < 0.001d &&
                Math.Abs(runtime.VanillaOrthographicSize) <
                0.001d &&
                !hooks.IsInstalled &&
                hooks.UnpatchCalls == 2,
                "A later cleanup round must use the retained snapshot to restore the live camera even though the exact owner was already removed.");
        }

        private static void AbsentCameraAllowsSafeCleanupAndExactUnpatch()
        {
            var hooks = new FakeHookOwner();
            var runtime = new ZoomNativeRuntime(
                NullMonitor.Instance,
                hooks);
            double? nativeSize = 10d;
            runtime.ConfigureNativeAccessForTests(
                () => nativeSize,
                value =>
                {
                    nativeSize = value;
                    return true;
                },
                mainCameraAbsent: () =>
                    !nativeSize.HasValue);
            runtime.Configure(
                new ZoomConfig { Enabled = true },
                "absent camera fixture");
            Assert(
                runtime.SetViewScale(2d, "capture absent baseline"),
                "The absent-camera fixture must first capture and apply a native baseline.");
            nativeSize = null;
            runtime.DeactivateOwner(
                "camera destroyed at title");
            Assert(
                !hooks.IsInstalled &&
                hooks.UnpatchCalls == 1 &&
                Math.Abs(runtime.VanillaOrthographicSize) <
                0.001d,
                "A definitely absent camera may drop its unreachable snapshot and must still remove the exact owner.");
        }

        private static void FailedNativeWriteRollsBackTheRequestedScale()
        {
            var hooks = new FakeHookOwner();
            var runtime = new ZoomNativeRuntime(
                NullMonitor.Instance,
                hooks);
            double nativeSize = 10d;
            bool failWrites = false;
            runtime.ConfigureNativeAccessForTests(
                () => nativeSize,
                value =>
                {
                    if (failWrites)
                        return false;
                    nativeSize = value;
                    return true;
                });
            runtime.Configure(
                new ZoomConfig { Enabled = true },
                "focused unit");
            failWrites = true;
            Assert(
                !runtime.SetViewScale(
                    2d,
                    "injected write failure") &&
                Math.Abs(runtime.CurrentViewScale - 1d) < 0.001d &&
                Math.Abs(nativeSize - 10d) < 0.001d,
                "A failed native write must fail closed and restore the in-memory requested scale.");
            failWrites = false;
            runtime.DeactivateOwner("focused cleanup");
        }

        private static void AtomicInstallFailureRollsBackAndReportsResidue()
        {
            bool attached = false;
            bool ownerPresent = false;
            int unpatchCalls = 0;
            bool threw = false;
            try
            {
                ZoomHookTransaction.Install(
                    () => attached = true,
                    () =>
                    {
                        ownerPresent = true;
                        throw new InvalidOperationException(
                            "injected patch failure");
                    },
                    () => ownerPresent,
                    () =>
                    {
                        unpatchCalls++;
                        ownerPresent = false;
                    },
                    () => attached = false);
            }
            catch (InvalidOperationException)
            {
                threw = true;
            }
            Assert(
                threw &&
                !attached &&
                !ownerPresent &&
                unpatchCalls == 1,
                "An injected installation failure must detach callbacks and roll back the exact owner.");

            bool aggregate = false;
            attached = false;
            ownerPresent = false;
            try
            {
                ZoomHookTransaction.Install(
                    () => attached = true,
                    () =>
                    {
                        ownerPresent = true;
                        throw new InvalidOperationException(
                            "injected patch failure");
                    },
                    () => ownerPresent,
                    () => throw new InvalidOperationException(
                        "injected unpatch failure"),
                    () => attached = false);
            }
            catch (AggregateException ex)
            {
                aggregate = ex.InnerExceptions.Count >= 3;
            }
            Assert(
                aggregate &&
                !attached &&
                ownerPresent,
                "An unpatch failure plus exact-owner residue must be reported together and never hidden.");
        }

        private static void Assert(
            bool condition,
            string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private static string FindRepositoryRoot()
        {
            DirectoryInfo? current =
                new DirectoryInfo(AppContext.BaseDirectory);
            while (current != null)
            {
                if (File.Exists(
                    Path.Combine(
                        current.FullName,
                        "PROJECT.md")))
                {
                    return current.FullName;
                }
                current = current.Parent;
            }
            throw new DirectoryNotFoundException(
                "Could not locate the DTMAPI repository root.");
        }

        private sealed class FakeHookOwner : IZoomHookOwner
        {
            public bool IsInstalled { get; private set; }

            public int InstalledPatchCount =>
                IsInstalled ? 3 : 0;

            internal int UnpatchCalls { get; private set; }

            public void InstallAtomically(
                ZoomNativeRuntime runtime)
            {
                if (IsInstalled)
                return;
                IsInstalled = true;
                ZoomCallbacks.Attach(runtime);
            }

            public void UnpatchOwnedHooks(
                ZoomNativeRuntime runtime)
            {
                UnpatchCalls++;
                IsInstalled = false;
                ZoomCallbacks.Detach(runtime);
            }
        }
    }
}
