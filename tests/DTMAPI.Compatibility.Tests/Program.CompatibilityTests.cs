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
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                const string ownerId = "DTMAPI.Tests.CleanupRetry";
                var participant = new RetryingOwnerCleanupParticipant("DTMAPI.Tests.CleanupRetry.Participant", failFirstCleanup: true);
                runtime.RegisterModOwnerCleanupParticipant(participant);
                runtime.Start();

                string first = InvokeFailedOwnerCleanup(runtime, ownerId);
                Assert(participant.CallCount == 1 && participant.RemainingResources == 1, "The first owner cleanup attempt should expose the participant's retained root.");
                Assert(first.Contains("participantCleanupFailures=1", StringComparison.Ordinal) && first.Contains("remaining=1", StringComparison.Ordinal), "The first owner cleanup summary should fail closed while a participant root remains.");
                Assert(runtime.OwnerRequiresRestart(ownerId), "A failed deactivation attempt must leave the owner inactive and restart-required.");

                string second = InvokeOwnerDeactivation(runtime, ownerId, ModOwnerCleanupReason.EntryFailed, shutdown: false);
                Assert(participant.CallCount == 2 && participant.RemainingResources == 0, "An inactive owner must permit a cleanup retry that removes the retained root.");
                Assert(!second.Contains("alreadyInactive=true", StringComparison.Ordinal), "A terminal owner state must not short-circuit a required cleanup retry.");
                Assert(second.Contains("participantCleanupFailures=0", StringComparison.Ordinal) && second.Contains("remaining=0", StringComparison.Ordinal), "The retry summary should prove zero remaining roots without another Entry.");
                Assert(!runtime.OwnerRequiresRestart(ownerId), "An owner that never loaded an assembly should become eligible for same-process Entry once a retry proves every platform root is zero.");

                string modDir = WriteAtomicProbePackage(dir, ownerId, string.Empty);
                AtomicOwnerProbeMod.EntryCount = 0;
                runtime.NotifyWorkshopModListChanged();
                Assert(runtime.ModRegistry.IsLoaded(ownerId) && AtomicOwnerProbeMod.EntryCount == 1 && !runtime.OwnerRequiresRestart(ownerId),
                    "Successful cleanup lets the official Mod list load the repaired source once.");
                runtime.NotifyRuntimeShutdown("cleanup retry discovery");
                var restarted = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                restarted.Start();
                Assert(restarted.LoadedMods.Count(mod => mod.Manifest.UniqueID == ownerId) == 1 && AtomicOwnerProbeMod.EntryCount == 2,
                    "The next runtime admits the repaired source without a stale cleanup gate.");
                InvokeOwnerDeactivation(restarted, ownerId, ModOwnerCleanupReason.EntryFailed, shutdown: false);
                Assert(CountCoreOwnerRootsForTest(restarted, ownerId) == 0 && restarted.OwnerRequiresRestart(ownerId),
                    "Explicit deactivation releases roots but cannot unload a resident code assembly.");
                Assert(AtomicOwnerProbeMod.EntryCount == 2, "Explicit cleanup must not re-run Entry.");
                restarted.NotifyRuntimeShutdown("cleanup retry complete");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

    }
}
