using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DTMAPI.StrongPlantingGun;

namespace DTMAPI.UnitTests
{
    internal static class StrongPlantingGunProductTests
    {
        internal static void RunAll() => RunAll(null);

        internal static void RunAll(string? fixtureExecutable)
        {
            ProductContractIsFixed();
            RangeInputsAreBoundedIntegers();
            RangeConfigRoundTripsThroughContractSerializer();
            FiveTargetOwnerDecisionsFailClosed();
            ExactOwnerObservationIsTruthful();
            LogGateIsBounded();
            ReflectionMetadataIsCachedAndCleared();
            RepeatedCachedLookupHasAShortAllocationGate();
            CapacityAndCleanupContractsPreserveNativeState();
            LifecycleSummaryReachesExactZero();
            PhysicalProductionHookLifecycleFixturePasses(fixtureExecutable);
        }

        private static void RangeInputsAreBoundedIntegers()
        {
            Assert(new StrongPlantingGunConfig().Range == 7, "Range default must match current native 7.");
            for (int n = 7; n <= 15; n++) Assert(StrongPlantingGunConfig.Normalize(n) == n, "Valid integer range changed.");
            foreach (double n in new[] { double.NaN, double.NegativeInfinity, double.PositiveInfinity, 7.5, -1d, 0d, 6d })
                Assert(StrongPlantingGunConfig.Normalize(n) == 7, "Invalid/fractional/below-native value did not use 7.");
            foreach (double n in new[] { 16d, (double)int.MaxValue, 1e100 })
                Assert(StrongPlantingGunConfig.Normalize(n) == 15, "Above-maximum integer did not clamp to 15.");
        }

        private static void RangeConfigRoundTripsThroughContractSerializer()
        {
            var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(StrongPlantingGunConfig));
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, new StrongPlantingGunConfig { Range = 15 });
                stream.Position = 0;
                var restored = (StrongPlantingGunConfig)serializer.ReadObject(stream)!;
                Assert(restored.Range == 15, "The actual config serializer must read and write the public range contract.");
            }
            using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("{\"Range\":6.5,\"Enabled\":false}")))
            {
                var restored = (StrongPlantingGunConfig)serializer.ReadObject(stream)!;
                Assert(StrongPlantingGunConfig.Normalize(restored.Range) == 7, "Legacy flags must not prevent fractional range fallback.");
            }
        }

        private static void ProductContractIsFixed()
        {
            Assert(
                StrongPlantingGunProductContract.FixedSlotCount == 4,
                "ProductNative must remain fixed at four slots.");
            Assert(
                StrongPlantingGunProductContract.ExpectedHookCount == 15,
                "The exact constructors, tool, preview and nine container adapters must remain fifteen patches.");
            Assert(
                StrongPlantingGunProductContract.HarmonyOwner ==
                    "DTMAPI.StrongPlantingGunMod.Native" &&
                StrongPlantingGunProductContract.LegacyProductOwner ==
                    "dtmapi.mod.dtmapi.strongplantinggunmod",
                "V2 product owner or its conflicting legacy owner drifted.");
        }

        private static void FiveTargetOwnerDecisionsFailClosed()
        {
            bool[] empty = new bool[15];
            Assert(
                StrongPlantingGunHookOwnership.DecideInstall(
                    empty,
                    empty) ==
                StrongPlantingGunInstallDecision.Install,
                "An empty fifteen-target set should install.");

            for (int index = 0; index < 15; index++)
            {
                var compatibility = new bool[15];
                compatibility[index] = true;
                Assert(
                    StrongPlantingGunHookOwnership.DecideInstall(
                        compatibility,
                        empty) ==
                    StrongPlantingGunInstallDecision
                        .RejectCompatibilityOwner,
                    "Either load order must reject any legacy owner target.");

                var partialProduct = new bool[15];
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
                    Enumerable.Repeat(true, 15).ToArray()) ==
                StrongPlantingGunInstallDecision
                    .RejectDuplicateProductOwner,
                "A complete existing ProductNative set must reject duplicate install.");
        }

        private static void ExactOwnerObservationIsTruthful()
        {
            StrongPlantingGunObservedHookState empty =
                StrongPlantingGunHookOwnership.Observe(
                    new bool[15]);
            Assert(
                !empty.IsInstalled &&
                empty.InstalledPatchCount == 0 &&
                !empty.HasPartialOwnerSet,
                "An empty owner set must report zero.");

            StrongPlantingGunObservedHookState partial =
                StrongPlantingGunHookOwnership.Observe(
                    Enumerable.Range(0, 15).Select(i => i < 2).ToArray());
            Assert(
                !partial.IsInstalled &&
                partial.InstalledPatchCount == 2 &&
                partial.HasPartialOwnerSet,
                "Partial observation must not report installed.");

            StrongPlantingGunObservedHookState complete =
                StrongPlantingGunHookOwnership.Observe(
                    Enumerable.Repeat(true, 15).ToArray());
            Assert(
                complete.IsInstalled &&
                complete.InstalledPatchCount == 15 &&
                !complete.HasPartialOwnerSet,
                "Only all fifteen owners may report installed.");
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
                    .GetTargetInventoryCapacity(1) == 4,
                "An ordinary one-slot gun must expand to four.");
            Assert(
                StrongPlantingGunProductContract
                    .GetTargetInventoryCapacity(7) == 7,
                "A JSON inventory already larger than four must never shrink.");

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
            PhysicalProductionHookLifecycleFixturePasses(string? fixtureExecutable)
        {
            string executable =
                fixtureExecutable ?? Path.Combine(
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
                if ((File.Exists(Path.Combine(current.FullName, "DTMAPI.sln")) && File.Exists(Path.Combine(current.FullName, "global.json"))))
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
