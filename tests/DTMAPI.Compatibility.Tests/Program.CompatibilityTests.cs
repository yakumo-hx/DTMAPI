#pragma warning disable CS0618 // Frozen compatibility contracts are intentionally exercised.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using DTMAPI.Abstractions;
using DTMAPI.Core.Diagnostics;
using DTMAPI.Core.Logging;
using DTMAPI.Core.Manager;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;
using DTMAPI.Core.Services;
using DTMAPI.GameBridge.DolocTown;
using DTMAPI.ModConfigMenu;

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {

        private static void InactiveOwnerCleanupCanBeRetriedUntilZero()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                StageCompatibilityHostFixture(dir);
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                const string ownerId = "DTMAPI.Tests.CleanupRetry";
                var participant = new RetryingOwnerCleanupParticipant("DTMAPI.Tests.CleanupRetry.Participant", failFirstCleanup: true);
                runtime.RegisterModOwnerCleanupParticipant(participant);
                runtime.Start();

                string first = InvokeFailedOwnerCleanup(runtime, ownerId);
                Assert(participant.CallCount == 1 && participant.RemainingResources == 1, "The first owner cleanup attempt should expose the participant's retained root.");
                Assert(first.Contains("participantCleanupFailures=1", StringComparison.Ordinal) && first.Contains("remaining=1", StringComparison.Ordinal), "The first owner cleanup summary should fail closed while a participant root remains.");
                Assert(runtime.OwnerRequiresRestart(ownerId), "A failed deactivation attempt must leave the owner inactive and restart-required.");
                int restartDiagnostics = runtime.ModOwnerLedgerSnapshot.NeedsRestart;

                string second = InvokeOwnerDeactivation(runtime, ownerId, ModOwnerCleanupReason.EntryFailed, shutdown: false);
                Assert(participant.CallCount == 2 && participant.RemainingResources == 0, "An inactive owner must permit a cleanup retry that removes the retained root.");
                Assert(!second.Contains("alreadyInactive=true", StringComparison.Ordinal), "A terminal owner state must not short-circuit a required cleanup retry.");
                Assert(second.Contains("participantCleanupFailures=0", StringComparison.Ordinal) && second.Contains("remaining=0", StringComparison.Ordinal), "The retry summary should prove zero remaining roots without another Entry.");
                Assert(!runtime.OwnerRequiresRestart(ownerId), "An owner that never loaded an assembly should become eligible for same-process Entry once a retry proves every platform root is zero.");
                Assert(runtime.ModOwnerLedgerSnapshot.NeedsRestart == restartDiagnostics, "A cleanup retry must not duplicate the owner's restart-required diagnostic.");

                string modDir = WriteAtomicProbePackage(dir, ownerId, string.Empty);
                AtomicOwnerProbeMod.EntryCount = 0;
                runtime.NotifyWorkshopModListChanged();
                Assert(runtime.LoadedMods.Count(mod => mod.Manifest.UniqueID == ownerId) == 1 && AtomicOwnerProbeMod.EntryCount == 1, "The fully cleaned no-assembly owner should be able to enter and load normally in the same process.");
                Assert(!runtime.OwnerRequiresRestart(ownerId) && CountCoreOwnerRootsForTest(runtime, ownerId) > 0, "Successful same-process re-entry should publish ordinary owner roots without a stale restart gate.");

                File.WriteAllText(Path.Combine(modDir, "dtmapi.disabled"), "disable after successful Assembly.LoadFrom");
                runtime.NotifyWorkshopModListChanged();
                Assert(!runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == ownerId) && CountCoreOwnerRootsForTest(runtime, ownerId) == 0, "Disabling the successfully loaded retry owner should still clean all platform roots.");
                Assert(runtime.OwnerRequiresRestart(ownerId), "Once Assembly.LoadFrom succeeds, the code owner must remain restart-required after deactivation even when cleanup proves zero roots.");
                Assert(AtomicOwnerProbeMod.EntryCount == 1, "A loaded-assembly deactivation must not re-run Entry.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void EquipmentSlotProtectedStoragePolicyUsesPerSaveTailRecovery()
        {
            Assert(EquipmentSlotProtectedStoragePolicy.BuildSaveScopeKey(3) == "slot-3", "Equipment-slot protected storage should key sidecars by archive slot.");
            Assert(EquipmentSlotProtectedStoragePolicy.MakeSafePathSegment("A:B/C") == "A_B_C", "Equipment-slot protected storage should sanitize path segments.");
            Assert(EquipmentSlotProtectedStoragePolicy.GetTailIndexFromEnd(0, 4) == 3, "Equipment-slot storage should record head slots as furthest from the tail.");
            Assert(EquipmentSlotProtectedStoragePolicy.GetTailIndexFromEnd(3, 4) == 0, "Equipment-slot storage should record the final slot as the tail.");

            int[] ordered = EquipmentSlotProtectedStoragePolicy.OrderTailFirst(new[] { 0, 1, 2, 3 }, value => value).ToArray();
            Assert(ordered.SequenceEqual(new[] { 3, 2, 1, 0 }), "Equipment-slot recovery should process tail slots before head slots.");

            Assert(EquipmentSlotProtectedStoragePolicy.IsStorageCompatible(3, "Yuuka", string.Empty, 100, 3, "Yuuka", string.Empty, 120, out _), "Matching save identity should allow protected storage.");
            Assert(!EquipmentSlotProtectedStoragePolicy.IsStorageCompatible(2, "Yuuka", string.Empty, 100, 3, "Yuuka", string.Empty, 120, out string archiveReason) && archiveReason.Contains("archive-index-mismatch"), "Protected storage should reject mismatched archive slots.");
            Assert(!EquipmentSlotProtectedStoragePolicy.IsStorageCompatible(3, "Other", string.Empty, 100, 3, "Yuuka", string.Empty, 120, out string playerReason) && playerReason.Contains("player-name-mismatch"), "Protected storage should reject clearly mismatched player identities.");
            Assert(!EquipmentSlotProtectedStoragePolicy.IsStorageCompatible(3, "Yuuka", string.Empty, 1000, 3, "Yuuka", string.Empty, 120, out string clockReason) && clockReason.Contains("total-game-seconds-regressed"), "Protected storage should reject strong save-clock regressions.");
        }
    }
}
