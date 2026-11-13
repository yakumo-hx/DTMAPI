using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;
using DTMAPI.ModConfigMenu;

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {
        private static void OfficialPageShowsEachChangeOnce()
        {
            string? previous = UseTempPersistentRoot();
            DtmApiRuntime? runtime = null;
            try
            {
                string game = NewTempGameDir();
                runtime = new DtmApiRuntime(new FakeHost(game));
                runtime.Start();
                var dialogs = new List<DtmConfigDialog>();
                runtime.ConfigDialogs.Show = session => { dialogs.Add(session.Dialog); return true; };
                void Pack(string folder, bool restart)
                {
                    string path = CreateEnabledFixtureModDirectory(game, folder);
                    Directory.CreateDirectory(path);
                    File.WriteAllText(Path.Combine(path, "manifest.json"), "{\"UniqueID\":\"Notice." + folder + "\",\"Name\":\"" + folder + "\",\"Version\":\"1.2.3\",\"Type\":\"ContentPack\",\"RequiresRestart\":" + (restart ? "true" : "false") + "}");
                }
                Pack("Running", false);
                runtime.NotifyWorkshopModListChanged();
                Assert(dialogs.Count == 0 && runtime.ModRegistry.IsLoaded("Notice.Running"), "Successful loading does not open a result dialog.");
                Pack("Startup", true);
                runtime.NotifyWorkshopModListChanged();
                Assert(dialogs.Count == 1 && dialogs[0].Message == "重启后生效：Startup", "The restart dialog only lists pending Mod names.");
                runtime.ConfigDialogs.Current!.Close();
                string persistent = Environment.GetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT")!;
                SetFixtureModEnabled(Path.Combine(persistent, "MODS", "Startup"), false);
                string bad = WriteAtomicProbePackage(game, "Notice.Failed", "");
                string manifest = Path.Combine(bad, "manifest.json");
                File.WriteAllText(manifest, File.ReadAllText(manifest).Replace(typeof(AtomicOwnerProbeMod).FullName!, typeof(ThrowingEntryProbeMod).FullName!));
                runtime.NotifyWorkshopModListChanged();
                Assert(dialogs.Count == 2 && dialogs[1].Message.Contains("throwing-entry-probe", StringComparison.Ordinal) &&
                    runtime.CreateDiagnosticsSnapshot().Mods.Single(mod => mod.UniqueID == "Notice.Failed").StatusCode == "code-load-error",
                    "A failed package shows its cause in a dialog and remains visible on the manager.");
                runtime.ConfigDialogs.Current!.Close();
                string missing = CreateEnabledFixtureModDirectory(game, "Missing");
                Directory.CreateDirectory(missing);
                File.WriteAllText(Path.Combine(missing, "manifest.json"), "{\"UniqueID\":\"Notice.Missing\",\"Name\":\"Missing\",\"Version\":\"1.0.0\",\"Type\":\"CodeMod\",\"EntryDll\":\"missing.dll\"}");
                runtime.NotifyWorkshopModListChanged();
                Assert(dialogs.Count == 3 && dialogs[2].Message.Contains("Missing：EntryDll 缺失。", StringComparison.Ordinal) &&
                    !dialogs[2].Message.Contains(missing, StringComparison.Ordinal),
                    "A missing entry popup explains the failure instead of displaying only its disk path.");
                Assert(!dialogs[2].Message.Contains("throwing-entry-probe", StringComparison.Ordinal), "A failure already shown is not repeated.");
                runtime.ConfigDialogs.Current!.Close();
                SetFixtureModEnabled(Path.Combine(persistent, "MODS", "Running"), false);
                runtime.NotifyWorkshopModListChanged();
                Assert(dialogs.Count == 4 && dialogs[3].Message == "重启后停用：Running", "Unchecking a running Mod says it stops after restart.");
                runtime.ConfigDialogs.Current!.Close();
                runtime.NotifyWorkshopModListChanged();
                Assert(dialogs.Count == 4, "Closing the page again without changes shows nothing.");
                SetFixtureModEnabled(Path.Combine(persistent, "MODS", "Running"), true);
                runtime.NotifyWorkshopModListChanged();
                Assert(dialogs.Count == 4, "Rechecking the running Mod clears the pending stop without a dialog.");
                SetFixtureModEnabled(Path.Combine(persistent, "MODS", "Running"), false);
                runtime.NotifyWorkshopModListChanged();
                Assert(dialogs.Count == 5 && dialogs[4].Message == "重启后停用：Running",
                    "A pending stop is shown again after the previous change disappeared.");
                runtime.ConfigDialogs.Current!.Close();
            }
            finally { runtime?.NotifyRuntimeShutdown("restart notice test"); RestorePersistentRoot(previous); }
        }

        private static void OfficialPageLoadsNewPackagesAndSettlesAgainstRunningInstances()
        {
            string? previous = UseTempPersistentRoot();
            DtmApiRuntime? runtime = null;
            var entries = new List<string>();
            var launched = new List<string>();
            try
            {
                string game = NewTempGameDir();
                string consumer = WriteAtomicProbePackage(game, "Page.Consumer", ",\"Dependencies\":[{\"UniqueID\":\"Page.Provider\",\"Required\":true}]");
                string provider = WriteAtomicProbePackage(game, "Page.Provider", "");
                string early = WriteAtomicProbePackage(game, "Page.StartupOnly", ",\"RequiresRestart\":true");
                string follower = WriteAtomicProbePackage(game, "Page.Follower", ",\"Dependencies\":[{\"UniqueID\":\"Page.StartupOnly\",\"Required\":true}]");
                string bad = WriteAtomicProbePackage(game, "Page.Failed", "");
                foreach (string path in new[] { provider, early, follower, bad }) SetFixtureModEnabled(path, false);
                AtomicOwnerProbeMod.EntryObserved = helper =>
                {
                    string id = helper.ModManifest.UniqueID;
                    entries.Add(id);
                    helper.Events.GameLoop.GameLaunched += (_, _) => launched.Add(id);
                    if (id == "Page.Failed") throw new InvalidOperationException("package initialization failed");
                };
                var menu = new ConfigMenuRegistry();
                runtime = new DtmApiRuntime(new FakeHost(game), menu);
                runtime.Start();
                int observerLaunches = 0;
                runtime.Events.CreateProxy("Observer").GameLoop.GameLaunched += (_, _) => observerLaunches++;
                Assert(!runtime.ModRegistry.IsLoaded("Page.Consumer"), "Missing dependency leaves the consumer uninitialized.");
                foreach (string path in new[] { provider, early, follower, bad }) SetFixtureModEnabled(path, true);
                runtime.NotifyWorkshopModListChanged();
                Assert(entries.SequenceEqual(new[] { "Page.Failed", "Page.Provider", "Page.Consumer" }), "One failed Entry must not block a ready provider and its previously blocked consumer.");
                Assert(launched.SequenceEqual(new[] { "Page.Provider", "Page.Consumer" }) && observerLaunches == 0, "Only successfully loaded new owners receive GameLaunched once.");
                Assert(((IConfigMenuRuntime)menu).GetPage("Page.Consumer")?.IsLocked == false, "Late-loaded configuration becomes editable.");
                var statuses = runtime.CreateDiagnosticsSnapshot().Mods;
                Assert(statuses.Single(mod => mod.UniqueID == "Page.Failed").Status == "error" &&
                    statuses.Single(mod => mod.UniqueID == "Page.Failed").Reason.Contains("package initialization failed", StringComparison.Ordinal), "Entry failure reports its actual cause.");
                Assert(statuses.Where(mod => mod.UniqueID == "Page.StartupOnly" || mod.UniqueID == "Page.Follower").All(mod => !mod.Loaded && mod.StatusCode == "restart-required"), "Author restart requirements propagate through required dependencies.");
                Assert(statuses.Single(mod => mod.UniqueID == "Page.Consumer").StatusCode == "loaded", "An earlier missing dependency error must not hide the now-running consumer.");
                DtmMod? instance = runtime.GetOwnerInstance("Page.Consumer");
                SetFixtureModEnabled(consumer, false);
                runtime.NotifyWorkshopModListChanged();
                Assert(runtime.CreateDiagnosticsSnapshot().Mods.Single(mod => mod.UniqueID == "Page.Consumer").StatusCode == "restart-required" &&
                    ReferenceEquals(instance, runtime.GetOwnerInstance("Page.Consumer")), "Unchecking retains the current instance until restart.");
                SetFixtureModEnabled(consumer, true);
                runtime.NotifyWorkshopModListChanged();
                Assert(runtime.CreateDiagnosticsSnapshot().Mods.Single(mod => mod.UniqueID == "Page.Consumer").StatusCode == "loaded" &&
                    ReferenceEquals(instance, runtime.GetOwnerInstance("Page.Consumer")), "Checking the original source again cancels the stop request.");
                Assert(entries.Count == 3 && launched.Count == 2, "Repeated submissions neither repeat Entry nor replay GameLaunched.");
            }
            finally
            {
                AtomicOwnerProbeMod.EntryObserved = null;
                runtime?.NotifyRuntimeShutdown("official-page package loading test");
                RestorePersistentRoot(previous);
            }
        }

        private static void LateContentPacksAppendOrderAndRefreshResourcesOnce()
        {
            string? previous = UseTempPersistentRoot();
            DtmApiRuntime? runtime = null;
            try
            {
                string game = NewTempGameDir();
                string first = CreateEnabledFixtureModDirectory(game, "First");
                Directory.CreateDirectory(first);
                File.WriteAllText(Path.Combine(first, "manifest.json"), "{\"UniqueID\":\"Page.First\",\"Name\":\"First\",\"Version\":\"1.0.0\",\"Type\":\"ContentPack\"}");
                File.WriteAllText(Path.Combine(first, "first.txt"), "first");
                using var files = new RuntimeComponentFiles(game);
                foreach (string component in new[] { "DTMAPI.RuntimeModules", "DTMAPI.AudioContent", "DTMAPI.AnimalContent", "DTMAPI.MonsterContent" })
                {
                    Directory.CreateDirectory(files.DirectoryFor(component));
                    File.Copy(Path.Combine(FindRepositoryRoot(), "src", component, "bin", "Release", "netstandard2.0", component + ".dll"),
                        Path.Combine(files.DirectoryFor(component), component + ".dll"));
                }
                runtime = new DtmApiRuntime(new FakeHost(game));
                runtime.RegisterRuntimeModule("DTMAPI.Audio");
                runtime.Start();
                IManifest original = runtime.ModRegistry.Get("Page.First")!;
                int? originalOrder = runtime.ResolveOfficialOwnerOrder(original);
                Assert(runtime.Content.FindAssets("txt").Count == 1, "The initial resource list includes the running pack.");
                string added = CreateEnabledFixtureModDirectory(game, "Added");
                Directory.CreateDirectory(added);
                File.WriteAllText(Path.Combine(added, "manifest.json"), "{\"UniqueID\":\"Page.Added\",\"Name\":\"Added\",\"Version\":\"1.0.0\",\"Type\":\"ContentPack\"}");
                File.WriteAllText(Path.Combine(added, "added.txt"), "added");
                Directory.CreateDirectory(Path.Combine(added, "PackData"));
                File.WriteAllText(Path.Combine(added, "PackData", "content.json"), "{\"schemaVersion\":1}");
                runtime.NotifyWorkshopModListChanged();
                IManifest newcomer = runtime.ModRegistry.Get("Page.Added")!;
                Assert(runtime.ResolveOfficialOwnerOrder(original) == originalOrder && runtime.ResolveOfficialOwnerOrder(newcomer) > originalOrder,
                    "The new pack appends order without changing a running pack's position.");
                Assert(runtime.Content.FindAssets("txt").Count == 2 && runtime.Content.TryReadTextAsset("added.txt", out string text) && text == "added",
                    "Closing the official page refreshes package resource owners.");
                var dependencies = (OwnerDependencyStore)typeof(DtmApiRuntime).GetField("ownerDependencies", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(runtime)!;
                Assert(dependencies.CountContributions("Page.Added") == 1, "PackData creates one live content contribution. " + string.Join(" | ", runtime.Diagnostics.GetErrors().Select(error => error.Details)));
                runtime.NotifyWorkshopModListChanged();
                Assert(dependencies.CountContributions("Page.Added") == 1, "Unchanged page submissions do not activate PackData again.");
                Assert(runtime.LoadedMods.Count == 2 && ReferenceEquals(newcomer, runtime.ModRegistry.Get("Page.Added")), "The package is not activated twice.");
                WriteOfficialModInfos(Environment.GetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT")!,
                    new OfficialModInfoTestEntry("Local.First", true, "Local", 10),
                    new OfficialModInfoTestEntry("Local.Added", true, "Local", 20));
                runtime.NotifyWorkshopModListChanged();
                Assert(runtime.CreateDiagnosticsSnapshot().Mods.All(mod => mod.StatusCode == "loaded"),
                    "Native priority renumbering does not create a source or version replacement.");
            }
            finally { runtime?.NotifyRuntimeShutdown("late content test"); RestorePersistentRoot(previous); }
        }
    }
}
