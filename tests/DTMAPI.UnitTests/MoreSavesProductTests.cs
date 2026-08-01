using System;
using System.Collections.Generic;
using DTMAPI.Abstractions;
using DTMAPI.MoreSaves;

namespace DTMAPI.UnitTests
{
    internal static class MoreSavesProductTests
    {
        internal static void RunAll()
        {
            EnabledProductOwnsOneWriterAndInstallsZeroHooks();
            MissingManagerRetriesAtBoundedCadence();
            DisableRestoresSixBeforeReleasingOwner();
            FailedFinalRestoreRetainsExactOwnerUntilLaterCleanup();
            CompatibilityFirstProductActivationFailsClosed();
        }

        private static void EnabledProductOwnsOneWriterAndInstallsZeroHooks()
        {
            MoreSavesNativeOwnerCoordinator.ResetForTests();
            try
            {
                var manager = new ManagerFixture();
                var retryDemand = new List<bool>();
                var runtime = new MoreSavesNativeRuntime(
                    NullMonitor.Instance,
                    () => manager,
                    value => ((ManagerFixture)value).archiveFileCount,
                    (value, count) => { ((ManagerFixture)value).archiveFileCount = count; return true; },
                    retryDemandChanged: retryDemand.Add);
                runtime.Configure(new MoreSavesConfig { Enabled = true }, "unit enabled");

                Assert(manager.archiveFileCount == 12, "Enabled MoreSaves must write the fixed twelve-slot contract.");
                Assert(runtime.LeaseHeld && MoreSavesNativeOwnerCoordinator.IsOwnedBy(MoreSavesNativeOwnerCoordinator.ProductOwner),
                    "Enabled MoreSaves must hold the one ProductNative archive-count lease.");
                Assert(runtime.InstalledPatchCount == 0, "MoreSaves must install zero Harmony patches.");
                Assert(!runtime.RetrySchedulingRequested && retryDemand.Count == 0,
                    "Healthy MoreSaves must publish no frame-retry subscription demand.");
                Assert(runtime.StatusSummary.Contains("pendingWork=false") &&
                       runtime.StatusSummary.Contains("retryScheduled=false"),
                    "Healthy status must distinguish absent pending work from absent retry scheduling.");

                runtime.ResetBoundary("unit title");
                Assert(manager.archiveFileCount == 12 && !runtime.RetryPending &&
                       !runtime.RetrySchedulingRequested && retryDemand.Count == 0,
                    "Title/save lifecycle reconciliation must preserve twelve without creating frame-retry demand.");
                runtime.DeactivateOwner("unit cleanup");
            }
            finally
            {
                MoreSavesNativeOwnerCoordinator.ResetForTests();
            }
        }

        private static void MissingManagerRetriesAtBoundedCadence()
        {
            MoreSavesNativeOwnerCoordinator.ResetForTests();
            try
            {
                ManagerFixture? manager = null;
                DateTimeOffset now = new DateTimeOffset(2026, 7, 23, 0, 0, 0, TimeSpan.Zero);
                var retryDemand = new List<bool>();
                var runtime = new MoreSavesNativeRuntime(
                    NullMonitor.Instance,
                    () => manager,
                    value => ((ManagerFixture)value).archiveFileCount,
                    (value, count) => { ((ManagerFixture)value).archiveFileCount = count; return true; },
                    () => now,
                    retryDemand.Add);

                runtime.Configure(new MoreSavesConfig { Enabled = true }, "unit missing");
                Assert(runtime.RetryPending && runtime.LeaseHeld && runtime.RetrySchedulingRequested &&
                       retryDemand.Count == 1 && retryDemand[0],
                    "Missing manager must retain one lease and demand-activate one bounded retry subscription.");
                Assert(runtime.StatusSummary.Contains("pendingWork=true") &&
                       runtime.StatusSummary.Contains("retryScheduled=true"),
                    "Active missing-manager status must report both pending work and its live retry scheduler.");
                manager = new ManagerFixture();
                runtime.Update();
                Assert(manager.archiveFileCount == 6, "Retry must not run before the bounded 750ms cadence.");
                now = now.AddMilliseconds(749);
                runtime.Update();
                Assert(manager.archiveFileCount == 6, "Retry must remain dormant before 750ms.");
                now = now.AddMilliseconds(1);
                runtime.Update();
                Assert(manager.archiveFileCount == 12 && !runtime.RetryPending && !runtime.RetrySchedulingRequested &&
                       retryDemand.Count == 2 && !retryDemand[1],
                    "The first due retry must apply twelve, clear pending work and remove the frame subscription.");
                runtime.DeactivateOwner("unit cleanup");
            }
            finally
            {
                MoreSavesNativeOwnerCoordinator.ResetForTests();
            }
        }

        private static void DisableRestoresSixBeforeReleasingOwner()
        {
            MoreSavesNativeOwnerCoordinator.ResetForTests();
            try
            {
                var manager = new ManagerFixture();
                var runtime = Runtime(manager);
                runtime.Configure(new MoreSavesConfig { Enabled = true }, "unit enabled");
                runtime.Configure(new MoreSavesConfig { Enabled = false }, "unit disabled");

                Assert(manager.archiveFileCount == 6, "Disabling MoreSaves must restore the native six-slot count.");
                Assert(!runtime.LeaseHeld && MoreSavesNativeOwnerCoordinator.ActiveOwner.Length == 0,
                    "The product lease must release only after native six is observed.");
            }
            finally
            {
                MoreSavesNativeOwnerCoordinator.ResetForTests();
            }
        }

        private static void FailedFinalRestoreRetainsExactOwnerUntilLaterCleanup()
        {
            MoreSavesNativeOwnerCoordinator.ResetForTests();
            try
            {
                var manager = new ManagerFixture();
                DateTimeOffset now = new DateTimeOffset(2026, 7, 23, 0, 0, 0, TimeSpan.Zero);
                bool allowRestore = false;
                var retryDemand = new List<bool>();
                var runtime = new MoreSavesNativeRuntime(
                    NullMonitor.Instance,
                    () => manager,
                    value => ((ManagerFixture)value).archiveFileCount,
                    (value, count) =>
                    {
                        if (count == 6 && !allowRestore)
                            return false;
                        ((ManagerFixture)value).archiveFileCount = count;
                        return true;
                    },
                    () => now,
                    retryDemand.Add);
                runtime.Configure(new MoreSavesConfig { Enabled = true }, "unit enabled");

                bool failed = false;
                try { runtime.DeactivateOwner("unit failing restore"); }
                catch (InvalidOperationException) { failed = true; }
                Assert(failed && manager.archiveFileCount == 12 && runtime.LeaseHeld && runtime.RetryPending,
                    "A failed final restore must fail closed while retaining the exact product lease.");
                Assert(MoreSavesNativeOwnerCoordinator.IsOwnedBy(MoreSavesNativeOwnerCoordinator.ProductOwner),
                    "A failed restore must not expose the native writer to a second owner.");
                Assert(!runtime.RetrySchedulingRequested && retryDemand.Count == 0,
                    "Final Loader deactivation must not claim an automatic retry after owner event delivery is removed.");
                Assert(runtime.StatusSummary.Contains("pendingWork=true") &&
                       runtime.StatusSummary.Contains("retryScheduled=false"),
                    "Failed final cleanup status must retain pending-work evidence without claiming an automatic scheduler.");

                allowRestore = true;
                now = now.AddMilliseconds(750);
                runtime.DeactivateOwner("unit later explicit cleanup");
                Assert(manager.archiveFileCount == 6 && !runtime.LeaseHeld && !runtime.RetryPending &&
                    MoreSavesNativeOwnerCoordinator.ActiveOwner.Length == 0,
                    "A later explicit cleanup pass must observe six before releasing the retained product lease.");
            }
            finally
            {
                MoreSavesNativeOwnerCoordinator.ResetForTests();
            }
        }

        private static void CompatibilityFirstProductActivationFailsClosed()
        {
            MoreSavesNativeOwnerCoordinator.ResetForTests();
            try
            {
                Assert(MoreSavesNativeOwnerCoordinator.TryAcquire(MoreSavesNativeOwnerCoordinator.CompatibilityOwner, out _),
                    "Compatibility fixture should acquire the one native writer.");
                var manager = new ManagerFixture();
                var runtime = Runtime(manager);
                bool rejected = false;
                try { runtime.Configure(new MoreSavesConfig { Enabled = true }, "unit compatibility-first"); }
                catch (InvalidOperationException) { rejected = true; }

                Assert(rejected && manager.archiveFileCount == 6 && !runtime.LeaseHeld,
                    "Compatibility-first ProductNative activation must reject before any native write or product retention.");
                Assert(MoreSavesNativeOwnerCoordinator.ReleaseAfterNativeRestore(MoreSavesNativeOwnerCoordinator.CompatibilityOwner, true),
                    "Compatibility fixture should release after its native-six proof.");
            }
            finally
            {
                MoreSavesNativeOwnerCoordinator.ResetForTests();
            }
        }

        private static MoreSavesNativeRuntime Runtime(ManagerFixture manager) =>
            new MoreSavesNativeRuntime(
                NullMonitor.Instance,
                () => manager,
                value => ((ManagerFixture)value).archiveFileCount,
                (value, count) => { ((ManagerFixture)value).archiveFileCount = count; return true; });

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private sealed class ManagerFixture
        {
            internal int archiveFileCount = 6;
        }
    }
}
