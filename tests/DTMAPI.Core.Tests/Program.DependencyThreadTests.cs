using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using DTMAPI.Core.Runtime;

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {
        private static void RuntimeDependencyThreadsAndFailureCleanup()
        {
            string? previous = UseTempPersistentRoot();
            DtmApiRuntime? runtime = null;
            try
            {
                string game = NewTempGameDir();
                const string entryName = "DTMAPI.DemandEntryFixture";
                Assert(!AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "DTMAPI.DemandPayloadFixture" || a.GetName().Name == "DTMAPI.DemandCallerFixture"),
                    "Thread/disposal dependencies must really be first loads in this process.");
                string Create(string owner, string type)
                {
                    string root = CreateEnabledFixtureModDirectory(game, owner);
                    File.Copy(Path.Combine(AppContext.BaseDirectory, "demand-fixtures", entryName + ".dll"), Path.Combine(root, entryName + ".dll"));
                    File.WriteAllText(Path.Combine(root, "manifest.json"), "{\"UniqueID\":\"" + owner + "\",\"Type\":\"CodeMod\",\"Version\":\"1.0.0\",\"EntryDll\":\"" + entryName + ".dll\",\"EntryType\":\"DTMAPI.DemandEntryFixture." + type + "\"}");
                    return root;
                }
                string failed = Create("Binding.BackgroundFailed", "ThreadedFailureMod");
                Create("Binding.Shared", "SharedMod");
                Create("Binding.SharedFailed", "SharedMod");
                Directory.CreateDirectory(Path.Combine(failed, "lib/private"));
                foreach (string dependency in new[] { "DTMAPI.DemandPayloadFixture", "DTMAPI.DemandCallerFixture" })
                    File.Copy(Path.Combine(AppContext.BaseDirectory, "demand-fixtures", dependency + ".dll"), Path.Combine(failed, "lib/private", dependency + ".dll"));
                var code = Assembly.LoadFrom(Path.Combine(failed, entryName + ".dll"));
                var fixture = code.GetType("DTMAPI.DemandEntryFixture.ThreadedFailureMod", true)!;
                var cleanup = new BindingPatchCleanup(Thread.CurrentThread.ManagedThreadId);
                MethodInfo platformPatch = typeof(BindingPatchCleanup).GetMethod(nameof(BindingPatchCleanup.PlatformPatch))!;
                cleanup.Patches.Add(platformPatch);
                fixture.GetField("OnPatch")!.SetValue(null, new Action<MethodInfo>(method => cleanup.Patches.Add(method)));
                runtime = new DtmApiRuntime(new FakeHost(game), configMenuApi: null, cleanup);
                runtime.Start();
                Assert((int)fixture.GetField("EntryThread")!.GetValue(null)! == Thread.CurrentThread.ManagedThreadId &&
                    (int)fixture.GetField("LoadThread")!.GetValue(null)! != Thread.CurrentThread.ManagedThreadId, "Entry waited for an actual worker load without a main-thread rendezvous.");
                Assert((int)fixture.GetField("LoadCallbackThread")!.GetValue(null)! == (int)fixture.GetField("LoadThread")!.GetValue(null)!, "The actual CLR callback ran on the worker thread.");
                Assert((bool)fixture.GetField("Disposed")!.GetValue(null)! && (int)fixture.GetField("DisposeThread")!.GetValue(null)! == Thread.CurrentThread.ManagedThreadId,
                    "Failed Entry disposal loads its dependency on the main thread.");
                var loader = (DependencyAssemblyLoader)typeof(DtmApiRuntime).GetField("activeDependencyLoader", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(runtime)!;
                var bindings = loader.GetBindings();
                foreach (string dependency in new[] { "DTMAPI.DemandPayloadFixture", "DTMAPI.DemandCallerFixture" })
                    Assert(bindings.Single(p => p.Key.GetName().Name == dependency).Value.SequenceEqual(new[] { "Binding.BackgroundFailed" }), "Actual dependency belongs to the failed package: " + dependency);
                Assert(bindings[code].Contains("Binding.Shared") && bindings[code].Contains("Binding.SharedFailed"), "Two packages bound the same resident Assembly; both owners are recorded.");
                Assert(cleanup.Removed.Select(p => p.Module.Assembly.GetName().Name).OrderBy(n => n).SequenceEqual(new[] { "DTMAPI.DemandCallerFixture", "DTMAPI.DemandPayloadFixture" }),
                    "Cleanup includes both the worker dependency and the dependency first loaded during disposal.");
                Assert(cleanup.Patches.Count == 2 && cleanup.Patches.Contains(platformPatch) && cleanup.Patches.Any(p => p.Module.Assembly == code),
                    "Shared and platform patches are preserved.");
                Assert(runtime.ModRegistry.IsLoaded("Binding.Shared") && !runtime.ModRegistry.IsLoaded("Binding.SharedFailed") && runtime.OwnerRequiresRestart("Binding.SharedFailed"),
                    "The survivor stays loaded; failed shared cleanup requires restart.");
                var sharedCleanup = runtime.CleanupModOwnerParticipants("Binding.SharedFailed", ModOwnerCleanupReason.EntryFailed);
                Assert(sharedCleanup.RemainingResources == 1 && sharedCleanup.FailureCount == 1, "Shared patches are reported as incomplete cleanup.");
                cleanup.Fail = true;
                Assert(runtime.CleanupModOwnerParticipants("Binding.SharedFailed", ModOwnerCleanupReason.EntryFailed).FailureCount == 1,
                    "A patch cleanup callback failure is reported at the Mod cleanup boundary.");
                cleanup.Fail = false;
                int calls = cleanup.Calls;
                runtime.NotifyRuntimeShutdown("binding-test"); runtime = null;
                Assert(cleanup.Calls == calls && cleanup.Patches.Count == 2, "Process shutdown does not inspect or unpatch Mod Harmony patches.");
                fixture.GetField("OnPatch")!.SetValue(null, null);
            }
            finally { runtime?.NotifyRuntimeShutdown("binding-test-finally"); RestorePersistentRoot(previous); }
        }
        private sealed class BindingPatchCleanup : IModPatchCleanup
        {
            private readonly int mainThread;
            internal BindingPatchCleanup(int mainThread) { this.mainThread = mainThread; }
            internal int Calls;
            internal bool Fail;
            internal readonly List<MethodInfo> Patches = new List<MethodInfo>();
            internal readonly List<MethodInfo> Removed = new List<MethodInfo>();
            public static void PlatformPatch() { }
            public ModOwnerCleanupParticipantResult RemovePatches(IReadOnlyCollection<Assembly> exclusive, IReadOnlyCollection<Assembly> shared, string ownerHint)
            {
                Assert(Thread.CurrentThread.ManagedThreadId == mainThread, "Patch cleanup is main-thread only.");
                Calls++;
                if (Fail) throw new InvalidOperationException("Patch callback failed.");
                var owned = Patches.Where(p => exclusive.Contains(p.Module.Assembly)).ToArray();
                Removed.AddRange(owned);
                int count = Patches.RemoveAll(p => owned.Contains(p));
                int remaining = Patches.Count(p => shared.Contains(p.Module.Assembly));
                return new ModOwnerCleanupParticipantResult(count, remaining, remaining == 0 ? 0 : 1, "test cleanup");
            }
        }
    }
}
