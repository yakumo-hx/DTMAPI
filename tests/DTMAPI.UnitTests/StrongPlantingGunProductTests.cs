using System;
using System.Diagnostics;
using System.IO;
using DTMAPI.StrongPlantingGun;

namespace DTMAPI.UnitTests
{
    internal static class StrongPlantingGunProductTests
    {
        internal static void RunAll()
        {
            ProductContractIsFixed();
            FiveTargetOwnerDecisionsFailClosed();
            ExactOwnerObservationIsTruthful();
            LogGateIsBounded();
            ReflectionMetadataIsCachedAndCleared();
            RepeatedCachedLookupHasAShortAllocationGate();
            CapacityAndCleanupContractsPreserveNativeState();
            LifecycleSummaryReachesExactZero();
            PhysicalProductionHookLifecycleFixturePasses();
        }

        private static void ProductContractIsFixed()
        {
            Assert(
                StrongPlantingGunProductContract.FixedSlotCount == 3,
                "ProductNative must remain fixed at three slots.");
            Assert(
                StrongPlantingGunProductContract.ExpectedHookCount == 5,
                "Two constructors plus tool/place/swap must remain five concrete patches.");
            Assert(
                StrongPlantingGunProductContract.HarmonyOwner ==
                    "dtmapi.mod.dtmapi.strongplantinggunmod",
                "Canonical Harmony owner drifted.");
        }

        private static void FiveTargetOwnerDecisionsFailClosed()
        {
            bool[] empty = new bool[5];
            Assert(
                StrongPlantingGunHookOwnership.DecideInstall(
                    empty,
                    empty) ==
                StrongPlantingGunInstallDecision.Install,
                "An empty five-target set should install.");

            for (int index = 0; index < 5; index++)
            {
                var compatibility = new bool[5];
                compatibility[index] = true;
                Assert(
                    StrongPlantingGunHookOwnership.DecideInstall(
                        compatibility,
                        empty) ==
                    StrongPlantingGunInstallDecision
                        .RejectCompatibilityOwner,
                    "Either load order must reject any legacy owner target.");

                var partialProduct = new bool[5];
                partialProduct[index] = true;
                Assert(
                    StrongPlantingGunHookOwnership.DecideInstall(
                        empty,
                        partialProduct) ==
                    StrongPlantingGunInstallDecision
                        .RejectPartialOwnerSet,
                    "A partial ProductNative owner must fail closed.");
            }

            Assert(
                StrongPlantingGunHookOwnership.DecideInstall(
                    empty,
                    new[]
                    {
                        true,
                        true,
                        true,
                        true,
                        true
                    }) ==
                StrongPlantingGunInstallDecision
                    .RejectDuplicateProductOwner,
                "A complete existing ProductNative set must reject duplicate install.");
        }

        private static void ExactOwnerObservationIsTruthful()
        {
            StrongPlantingGunObservedHookState empty =
                StrongPlantingGunHookOwnership.Observe(
                    new bool[5]);
            Assert(
                !empty.IsInstalled &&
                empty.InstalledPatchCount == 0 &&
                !empty.HasPartialOwnerSet,
                "An empty owner set must report zero.");

            StrongPlantingGunObservedHookState partial =
                StrongPlantingGunHookOwnership.Observe(
                    new[]
                    {
                        true,
                        true,
                        false,
                        false,
                        false
                    });
            Assert(
                !partial.IsInstalled &&
                partial.InstalledPatchCount == 2 &&
                partial.HasPartialOwnerSet,
                "Partial observation must not report installed.");

            StrongPlantingGunObservedHookState complete =
                StrongPlantingGunHookOwnership.Observe(
                    new[]
                    {
                        true,
                        true,
                        true,
                        true,
                        true
                    });
            Assert(
                complete.IsInstalled &&
                complete.InstalledPatchCount == 5 &&
                !complete.HasPartialOwnerSet,
                "Only all five owners may report installed.");
        }

        private static void LogGateIsBounded()
        {
            var gate = new StrongPlantingGunLogGate();
            int logged = 0;
            for (int index = 0; index < 512; index++)
            {
                if (gate.ShouldLog(
                    verbose: true,
                    kind: "repeat",
                    materialChange: false))
                {
                    logged++;
                }
            }
            Assert(
                logged <= 5,
                "Repeated diagnostics must remain bounded.");
            Assert(
                gate.ShouldLog(
                    verbose: false,
                    kind: "changed",
                    materialChange: true),
                "A material transition must still be observable.");
        }

        private static void ReflectionMetadataIsCachedAndCleared()
        {
            var cache = new StrongPlantingGunReflectionCache();
            for (int index = 0; index < 100; index++)
            {
                Assert(
                    cache.GetMethod(
                        typeof(FakeInventory),
                        nameof(FakeInventory.Read),
                        1) != null,
                    "Expected fake method was not resolved.");
            }
            Assert(
                cache.ResolutionCount == 1 &&
                cache.Count == 1,
                "Repeated metadata access must resolve once.");
            cache.Clear();
            Assert(
                cache.ResolutionCount == 0 &&
                cache.Count == 0,
                "Owner cleanup must clear cached reflection metadata.");
        }

        private static void RepeatedCachedLookupHasAShortAllocationGate()
        {
            var cache = new StrongPlantingGunReflectionCache();
            cache.GetMethod(
                typeof(FakeInventory),
                nameof(FakeInventory.Read),
                1);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            long before =
                GC.GetAllocatedBytesForCurrentThread();
            for (int index = 0; index < 10_000; index++)
            {
                cache.GetMethod(
                    typeof(FakeInventory),
                    nameof(FakeInventory.Read),
                    1);
            }
            long allocated =
                GC.GetAllocatedBytesForCurrentThread() -
                before;
            Assert(
                allocated <= 4096,
                "Cached metadata lookup allocated " +
                allocated +
                " bytes across 10,000 calls.");
        }

        private static void CapacityAndCleanupContractsPreserveNativeState()
        {
            Assert(
                StrongPlantingGunProductContract
                    .GetTargetInventoryCapacity(1) == 3,
                "An ordinary one-slot gun must expand to three.");
            Assert(
                StrongPlantingGunProductContract
                    .GetTargetInventoryCapacity(7) == 7,
                "A JSON inventory already larger than three must never shrink.");

            bool unpatchCalled = false;
            var failures =
                StrongPlantingGunCleanupTransaction.Run(
                    () => throw new InvalidOperationException(
                        "restore"),
                    () => unpatchCalled = true);
            Assert(
                unpatchCalled && failures.Count == 1,
                "Capacity restore failure must retain evidence and still execute exact-owner unpatch.");
        }

        private static void LifecycleSummaryReachesExactZero()
        {
            Assert(
                StrongPlantingGunLifecycleSummary.Format(
                    callbacks: 0,
                    hooks: 0,
                    cachedMembers: 0,
                    capacitySnapshots: 0) ==
                "listeners=0;callbacks=0;hooks=0;cachedObjects=0;cachedMembers=0;capacitySnapshots=0;roots=0",
                "Normal disable/title/Dispose cleanup must expose an exact zero summary.");
            Assert(
                StrongPlantingGunLifecycleSummary.Format(
                    callbacks: 0,
                    hooks: 1,
                    cachedMembers: 0,
                    capacitySnapshots: 0).EndsWith(
                        "roots=1",
                        StringComparison.Ordinal),
                "A failed exact-owner unpatch must retain a non-zero root even after callback detach.");
        }

        private static void
            PhysicalProductionHookLifecycleFixturePasses()
        {
            string executable =
                Path.Combine(
                    FindRepositoryRoot(),
                    "tests",
                    "DTMAPI.UnitTests",
                    "Fixtures",
                    "StrongPlantingGunHarmonyOwnerFixture",
                    "bin",
                    "Release",
                    "net48",
                    "StrongPlantingGunHarmonyOwnerFixture.exe");
            if (!File.Exists(executable))
            {
                throw new FileNotFoundException(
                    "The focused Unit build did not produce the physical StrongPlantingGun production Hook fixture.",
                    executable);
            }

            var start =
                new ProcessStartInfo
                {
                    FileName = executable,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
            using Process process =
                Process.Start(start)
                ?? throw new InvalidOperationException(
                    "Could not start the StrongPlantingGun production Hook fixture.");
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
                    "The StrongPlantingGun production Hook fixture did not exit within 30 seconds.");
            }
            Assert(
                process.ExitCode == 0 &&
                output.Contains(
                    "StrongPlantingGunHarmonyOwnerFixture: OK",
                    StringComparison.Ordinal),
                "The StrongPlantingGun production Hook fixture failed. exit=" +
                process.ExitCode +
                "; stdout=" +
                output +
                "; stderr=" +
                error);
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

        private static void Assert(
            bool condition,
            string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private sealed class FakeInventory
        {
            public object? Read(int index) => null;
        }
    }
}
