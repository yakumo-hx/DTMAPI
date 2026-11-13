using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DTMAPI.Abstractions.Internal;
using DTMAPI.Core.Runtime;

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {
        private static void RuntimeStartsModsBelowLinkedParents()
        {
            string? previous = UseTempPersistentRoot();
            string linkedParent = Path.Combine(NewTempGameDir(), "mapped-drive");
            Directory.CreateSymbolicLink(linkedParent, Environment.GetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT")!);
            DtmApiRuntime? runtime = null;
            var launched = new List<string>();
            try
            {
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", linkedParent);
                string game = Path.Combine(linkedParent, "game");
                Directory.CreateDirectory(game);
                WriteAtomicProbePackage(game, "Paths.First", "");
                WriteAtomicProbePackage(game, "Paths.Second", ",\"Dependencies\":[{\"UniqueID\":\"Paths.First\",\"Required\":true}]");
                AtomicOwnerProbeMod.EntryObserved = helper => helper.Events.GameLoop.GameLaunched += (_, _) => launched.Add(helper.ModManifest.UniqueID);
                runtime = CreateContentPathRuntime(game, out _);
                runtime.Start();
                Assert(runtime.ModRegistry.IsLoaded("Paths.First") && runtime.ModRegistry.IsLoaded("Paths.Second"), "A linked installation parent must not interrupt loading the remaining Mods.");
                Assert(launched.SequenceEqual(new[] { "Paths.First", "Paths.Second" }), "Runtime startup reaches GameLaunched for every Mod below the linked parent.");
                Assert(!runtime.Diagnostics.GetErrors().Any(error => error.Owner == "Paths.First" || error.Owner == "Paths.Second"), "A linked installation parent must not produce content path errors.");
            }
            finally
            {
                AtomicOwnerProbeMod.EntryObserved = null;
                runtime?.NotifyRuntimeShutdown("linked installation parent test");
                RestorePersistentRoot(previous);
                Directory.Delete(linkedParent);
            }
        }

        private static void RuntimeContentPathFailuresAreIsolated()
        {
            string? previous = UseTempPersistentRoot();
            DtmApiRuntime? runtime = null;
            string? linkedContent = null;
            var launched = new List<string>();
            try
            {
                string game = NewTempGameDir();
                string first = WriteAtomicProbePackage(game, "Paths.LinkedContent", "");
                WriteAtomicProbePackage(game, "Paths.Follower", ",\"Dependencies\":[{\"UniqueID\":\"Paths.LinkedContent\",\"Required\":true}]");
                string outside = NewTempGameDir();
                File.WriteAllText(Path.Combine(outside, "content.json"), "{\"schemaVersion\":1}");
                linkedContent = Path.Combine(first, "PackData");
                Directory.CreateSymbolicLink(linkedContent, outside);
                AtomicOwnerProbeMod.EntryObserved = helper => helper.Events.GameLoop.GameLaunched += (_, _) => launched.Add(helper.ModManifest.UniqueID);
                runtime = CreateContentPathRuntime(game, out var catalog);
                runtime.Start();
                Assert(runtime.ModRegistry.IsLoaded("Paths.LinkedContent") && runtime.ModRegistry.IsLoaded("Paths.Follower"), "A rejected content path must preserve the running CodeMod and allow subsequent Mods to load.");
                Assert(launched.SequenceEqual(new[] { "Paths.LinkedContent", "Paths.Follower" }), "A rejected content path must not prevent startup completion.");
                Assert(!catalog.Prepared, "A linked content path is rejected before any content module reads it.");
                Assert(runtime.Diagnostics.GetErrors().Any(error => error.Owner == "Paths.LinkedContent") &&
                    !runtime.Diagnostics.GetErrors().Any(error => error.Owner == "Paths.Follower"), "The content path failure is recorded against the contributing Mod.");
            }
            finally
            {
                AtomicOwnerProbeMod.EntryObserved = null;
                runtime?.NotifyRuntimeShutdown("content path isolation test");
                RestorePersistentRoot(previous);
                if (linkedContent != null) Directory.Delete(linkedContent);
            }
        }

        private static DtmApiRuntime CreateContentPathRuntime(string game, out ContentPathProbeCatalog catalog)
        {
            var runtime = new DtmApiRuntime(new FakeHost(game));
            catalog = new ContentPathProbeCatalog();
            SetPrivateField(runtime, "moduleCatalog", catalog);
            return runtime;
        }

        private sealed class ContentPathProbeCatalog : IRuntimeModuleCatalog
        {
            internal bool Prepared { get; private set; }
            public IRuntimeModule Create(string provider) => throw new InvalidOperationException("This fixture only checks content file paths.");
            public IRuntimeContentSource Prepare(IRuntimeModuleOwnerContext owner)
            {
                Prepared = true;
                throw new InvalidOperationException("Absent or linked content must not reach module preparation.");
            }
        }
    }
}
