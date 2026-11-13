using System;
using System.IO;
using System.Linq;
using DTMAPI.Core.Runtime;

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {
        private static void RuntimeAudioRegistersInternalCapabilityWithoutPlayback()
        {
            string? previous = UseTempPersistentRoot(); DtmApiRuntime? runtime = null;
            try
            {
                string game = NewTempGameDir(); string repo = FindRepositoryRoot();
                using var files = new RuntimeComponentFiles(game);
                AssertThrows<IOException>(() => files.Load("DTMAPI.RuntimeModules"), "An actual missing component request reports its fixed path.");
                foreach (string name in new[] { "DTMAPI.RuntimeModules", "DTMAPI.AudioContent", "DTMAPI.AnimalContent" })
                {
                    Directory.CreateDirectory(files.DirectoryFor(name));
                    File.Copy(Path.Combine(repo, "src", name, "bin", "Release", "netstandard2.0", name + ".dll"), Path.Combine(files.DirectoryFor(name), name + ".dll"));
                }
                var host = new FakeHost(game); runtime = new DtmApiRuntime(host);
                runtime.RegisterRuntimeModule("DTMAPI.Audio"); runtime.RegisterRuntimeModule("DTMAPI.Animals");
                runtime.Start();
                foreach (string provider in new[] { "DTMAPI.Audio", "DTMAPI.Animals" })
                {
                    Assert(runtime.ModRegistry.IsLoaded(provider), "Built-in typed API is available without native backend files: " + provider + " " + string.Join("; ", runtime.Diagnostics.GetErrors().Select(e => e.Details)));
                    Assert(!runtime.LoadedMods.Any(m => m.Manifest.UniqueID == provider), "A Runtime capability does not create a player Mod row.");
                }
                Assert(host.Logs.Any(l => l.Contains("NotRequested")), "No accepted contribution creates no playback demand.");
                Assert(!File.Exists(Path.Combine(files.DirectoryFor("DTMAPI.Modules.Audio"), "DTMAPI.Modules.Audio.dll")), "Cold registration requires neither playback implementation nor decoder.");
                runtime.NotifyRuntimeShutdown("runtime-modules-test");
                Assert(!runtime.ModRegistry.IsLoaded("DTMAPI.Audio") && !runtime.ModRegistry.IsLoaded("DTMAPI.Animals"), "Shutdown withdraws both capabilities.");
            }
            finally { runtime?.NotifyRuntimeShutdown("runtime-modules-test"); RestorePersistentRoot(previous); }
        }
    }
}
