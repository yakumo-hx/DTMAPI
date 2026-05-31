using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using DTMAPI.Abstractions;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;
using DTMAPI.ModConfigMenu;

namespace DTMAPI.UnitTests
{
    internal static class Program
    {
        private static int Main()
        {
            try
            {
                RuntimeStartsWithEmptyMods();
                RuntimeApiCanRegisterBeforeStart();
                BrokenManifestDoesNotCrashDiscovery();
                ConfigMenuEditsSaveCancelAndDetectConflicts();
                DisabledDiscoveredModLocksConfigPage();
                OfficialLocalModPackagesRespectOfficialEnablement();
                WorkshopReloadHotLoadsNewlyEnabledCodeModOnceAndLocksDisabledLoadedMod();
                RuntimeUiBoundariesBlockGameplayHotkeysAndModUpdates();
                Console.WriteLine("DTMAPI.UnitTests: OK");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("DTMAPI.UnitTests: FAILED");
                Console.Error.WriteLine(ex);
                return 1;
            }
        }

        private static void RuntimeApiCanRegisterBeforeStart()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
            string dir = NewTempGameDir();
            var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
            IManifest bridgeManifest = new ManifestModel
            {
                Name = "Bridge",
                Author = "DTMAPI",
                Version = DtmApiRuntime.ApiVersion,
                UniqueID = "DTMAPI.GameBridge.DolocTown",
                Type = "RuntimeApi"
            };
            runtime.RegisterRuntimeApi<IActionCompletionApi>(bridgeManifest, new FakeActionCompletionApi());
            runtime.Start();
            RuntimeSnapshot snapshot = runtime.CreateSnapshot();
            Assert(snapshot.Registry.Any(m => m.UniqueID == "DTMAPI.GameBridge.DolocTown"), "Runtime API manifest should stay registered after Start.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void RuntimeStartsWithEmptyMods()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
            string dir = NewTempGameDir();
            var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
            runtime.Start();
            RuntimeSnapshot snapshot = runtime.CreateSnapshot();
            Assert(snapshot.Registry.Count >= 1, "Runtime manifest should be registered.");
            Assert(File.Exists(runtime.Diagnostics.GetLatestLogPath()), "Latest DTMAPI log should exist.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void BrokenManifestDoesNotCrashDiscovery()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
            string dir = NewTempGameDir();
            string modDir = Path.Combine(dir, "Mods", "Broken");
            Directory.CreateDirectory(modDir);
            File.WriteAllText(Path.Combine(modDir, "manifest.json"), "{ \"Name\": \"Broken\" }");
            var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
            runtime.Start();
            RuntimeSnapshot snapshot = runtime.CreateSnapshot();
            Assert(snapshot.Errors.Count >= 1, "Broken manifest should be reported as an error.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void ConfigMenuEditsSaveCancelAndDetectConflicts()
        {
            var menu = new ConfigMenuRegistry();
            IManifest manifest = new ManifestModel
            {
                Name = "Menu Test",
                Author = "DTMAPI",
                Version = "1.0.0",
                UniqueID = "DTMAPI.Tests.Menu"
            };
            bool enabled = false;
            double multiplier = 1.0;
            string label = "before";
            string choice = "Safe";
            string keybind = "F10";
            string alternateKeybind = "F9";
            int saved = 0;
            int reset = 0;

            menu.Register(manifest, () =>
            {
                reset++;
                enabled = false;
                multiplier = 1.0;
                label = "default";
                choice = "Safe";
                keybind = "F10";
            }, () => saved++);
            menu.AddBoolOption(manifest, () => "Enabled", () => "", () => enabled, value => enabled = value);
            menu.AddNumberOption(manifest, () => "Multiplier", () => "", () => multiplier, value => multiplier = value, 1, 4, 0.5);
            menu.AddTextOption(manifest, () => "Label", () => "", () => label, value => label = value);
            menu.AddChoiceOption(manifest, () => "Choice", () => "", () => choice, value => choice = value, new[] { "Safe", "Fast" });
            menu.AddKeybindOption(manifest, () => "Toggle", () => "", () => keybind, value => keybind = value);
            menu.AddKeybindOption(manifest, () => "Alternate", () => "", () => alternateKeybind, value => alternateKeybind = value);

            IConfigMenuPage page = menu.GetPage(manifest.UniqueID) ?? throw new InvalidOperationException("Page should exist.");
            page.BeginEditing();
            Assert(page.Items[0].TrySetPendingValue("true", out _), "Bool option should accept true.");
            Assert(page.Items[1].TrySetPendingValue("3.25", out _), "Number option should accept numeric input.");
            Assert(page.Items[2].TrySetPendingValue("after", out _), "Text option should accept text input.");
            Assert(page.Items[3].TrySetPendingValue("Fast", out _), "Choice option should accept known choices.");
            Assert(page.HasPendingChanges, "Pending edits should be tracked.");

            page.Cancel();
            Assert(!enabled && multiplier == 1.0 && label == "before" && choice == "Safe", "Cancel should discard pending edits.");

            page.BeginEditing();
            page.Items[0].TrySetPendingValue("true", out _);
            page.Items[1].TrySetPendingValue("3.25", out _);
            page.Items[2].TrySetPendingValue("after", out _);
            page.Items[3].TrySetPendingValue("Fast", out _);
            page.Save();
            Assert(saved == 1, "Save callback should run.");
            Assert(enabled && Math.Abs(multiplier - 3.5) < 0.001 && label == "after" && choice == "Fast", "Save should apply pending values.");

            page.Reset();
            Assert(reset == 1, "Reset callback should run.");
            Assert(page.HasPendingChanges, "Reset should remain pending until save.");
            page.Cancel();
            Assert(enabled && Math.Abs(multiplier - 3.5) < 0.001 && label == "after" && choice == "Fast", "Cancel after reset should restore committed values.");

            page.BeginEditing();
            Assert(page.Items[4].TrySetPendingValue("F9", out _), "Keybind option should accept captured keys.");
            string conflict = menu.GetKeybindConflicts(manifest.UniqueID).Single();
            Assert(conflict.StartsWith("按键冲突：F9", StringComparison.Ordinal), "Duplicate keybinds should be reported with Chinese-first conflict text.");
            bool conflictBlocked = false;
            try
            {
                page.Save();
            }
            catch (InvalidOperationException)
            {
                conflictBlocked = true;
            }
            Assert(conflictBlocked, "Save should block keybind conflicts.");
        }

        private static void DisabledDiscoveredModLocksConfigPage()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
            string dir = NewTempGameDir();
            string modDir = Path.Combine(dir, "Mods", "Disabled");
            Directory.CreateDirectory(modDir);
            File.WriteAllText(Path.Combine(modDir, "manifest.json"), "{ \"Name\": \"Disabled\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.Disabled\", \"EntryDll\": \"Disabled.dll\" }");
            File.WriteAllText(Path.Combine(modDir, "dtmapi.disabled"), "disabled by official path");

            var menu = new ConfigMenuRegistry();
            IManifest manifest = new ManifestModel
            {
                Name = "Disabled",
                Author = "DTMAPI",
                Version = "1.0.0",
                UniqueID = "DTMAPI.Tests.Disabled"
            };
            bool enabled = false;
            int saved = 0;
            int reset = 0;
            menu.Register(manifest, () => reset++, () => saved++);
            menu.AddBoolOption(manifest, () => "Enabled", () => "", () => enabled, value => enabled = value);

            var runtime = new DtmApiRuntime(new FakeHost(dir), menu);
            runtime.Start();

            IConfigMenuPage page = menu.GetPage(manifest.UniqueID) ?? throw new InvalidOperationException("Disabled page should exist.");
            Assert(page.IsLocked, "Disabled discovered mod should lock its config page.");
            Assert(page.LockReason.IndexOf("本地 DTMAPI 禁用标记", StringComparison.OrdinalIgnoreCase) >= 0, "Disabled lock should explain the local marker with Chinese-first text.");

            page.BeginEditing();
            Assert(page.Items[0].TrySetPendingValue("true", out _), "Locked page may stage text but must not apply it.");
            AssertThrows(() => page.Save(), "Locked page save should be rejected.");
            AssertThrows(() => page.Reset(), "Locked page reset should be rejected.");
            Assert(!enabled && saved == 0 && reset == 0, "Locked page actions must not mutate config.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void OfficialLocalModPackagesRespectOfficialEnablement()
        {
            string gameDir = NewTempGameDir();
            string persistentRoot = Path.Combine(Path.GetTempPath(), "DTMAPI-tests-persistent", Guid.NewGuid().ToString("N"));
            string packageRoot = Path.Combine(persistentRoot, "MODS", "Yuuka_DTMAPI_Test");
            string contentRoot = Path.Combine(packageRoot, "Content", "DTMAPI");
            Directory.CreateDirectory(contentRoot);
            File.WriteAllText(
                Path.Combine(contentRoot, "manifest.json"),
                "{ \"Name\": \"Official Test\", \"Author\": \"Yuuka\", \"Version\": \"1.0.0\", \"UniqueID\": \"Yuuka.DTMAPI.Test\", \"Type\": \"ContentPack\" }",
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

            string localDuplicate = Path.Combine(gameDir, "Mods", "Yuuka.DTMAPI.Test");
            Directory.CreateDirectory(localDuplicate);
            File.WriteAllText(
                Path.Combine(localDuplicate, "manifest.json"),
                "{ \"Name\": \"Local Duplicate\", \"Author\": \"Yuuka\", \"Version\": \"1.0.0\", \"UniqueID\": \"Yuuka.DTMAPI.Test\", \"Type\": \"ContentPack\" }");

            string? previousRoot = Environment.GetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT");
            try
            {
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", persistentRoot);
                WriteOfficialModInfos(persistentRoot, "Local.Yuuka_DTMAPI_Test", false);
                var disabledRuntime = new DtmApiRuntime(new FakeHost(gameDir), new ConfigMenuRegistry());
                disabledRuntime.Start();
                RuntimeSnapshot disabledSnapshot = disabledRuntime.CreateSnapshot();
                DiscoveredMod disabled = disabledSnapshot.DiscoveredMods.Single(m => m.Manifest.UniqueID == "Yuuka.DTMAPI.Test");
                Assert(disabled.Source == "OfficialLocal", "Official local package should take precedence over duplicate game Mods entries.");
                Assert(!disabled.OfficialEnabled, "Official disabled state should prevent loading.");
                Assert(!disabledSnapshot.LoadedMods.Any(m => m.Manifest.UniqueID == "Yuuka.DTMAPI.Test"), "Official-disabled package must not load.");

                WriteOfficialModInfos(persistentRoot, "Local.Yuuka_DTMAPI_Test", true);
                var enabledRuntime = new DtmApiRuntime(new FakeHost(gameDir), new ConfigMenuRegistry());
                enabledRuntime.Start();
                RuntimeSnapshot enabledSnapshot = enabledRuntime.CreateSnapshot();
                Assert(enabledSnapshot.DiscoveredMods.Single(m => m.Manifest.UniqueID == "Yuuka.DTMAPI.Test").OfficialEnabled, "Official enabled state should be honored.");
                Assert(enabledSnapshot.LoadedMods.Any(m => m.Manifest.UniqueID == "Yuuka.DTMAPI.Test"), "Official-enabled content package should load/index.");

                File.Delete(Path.Combine(persistentRoot, "SAVE", "mod_infos.json"));
                var unknownRuntime = new DtmApiRuntime(new FakeHost(gameDir), new ConfigMenuRegistry());
                unknownRuntime.Start();
                DiscoveredMod unknown = unknownRuntime.CreateSnapshot().DiscoveredMods.Single(m => m.Manifest.UniqueID == "Yuuka.DTMAPI.Test");
                Assert(!unknown.OfficialEnabled, "Missing official enablement data should not default to enabled.");
                Assert(unknown.EnablementReason.IndexOf("官方启用状态文件", StringComparison.OrdinalIgnoreCase) >= 0, "Missing official state should have a clear Chinese-first reason.");
            }
            finally
            {
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", previousRoot);
            }
        }

        private static void WorkshopReloadHotLoadsNewlyEnabledCodeModOnceAndLocksDisabledLoadedMod()
        {
            string gameDir = NewTempGameDir();
            string persistentRoot = Path.Combine(Path.GetTempPath(), "DTMAPI-tests-persistent", Guid.NewGuid().ToString("N"));
            string packageRoot = Path.Combine(persistentRoot, "MODS", "Yuuka_DTMAPI_HotLoad");
            string contentRoot = Path.Combine(packageRoot, "Content", "DTMAPI");
            Directory.CreateDirectory(contentRoot);

            string assemblyPath = typeof(HotLoadProbeMod).Assembly.Location;
            string assemblyName = Path.GetFileName(assemblyPath);
            File.Copy(assemblyPath, Path.Combine(contentRoot, assemblyName), overwrite: true);
            File.WriteAllText(
                Path.Combine(contentRoot, "manifest.json"),
                "{ \"Name\": \"Hot Load Test\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.HotLoad\", \"EntryDll\": \"Content/DTMAPI/" + assemblyName + "\", \"Type\": \"CodeMod\" }");

            string? previousRoot = Environment.GetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT");
            try
            {
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", persistentRoot);
                WriteOfficialModInfos(persistentRoot, "Local.Yuuka_DTMAPI_HotLoad", false);

                var menu = new ConfigMenuRegistry();
                var runtime = new DtmApiRuntime(new FakeHost(gameDir), menu);
                runtime.Start();
                Assert(!runtime.CreateSnapshot().LoadedMods.Any(m => m.Manifest.UniqueID == "DTMAPI.Tests.HotLoad"), "Disabled official package should not load at startup.");

                WriteOfficialModInfos(persistentRoot, "Local.Yuuka_DTMAPI_HotLoad", true);
                runtime.NotifyWorkshopModListChanged();
                RuntimeSnapshot loadedSnapshot = runtime.CreateSnapshot();
                Assert(loadedSnapshot.LoadedMods.Count(m => m.Manifest.UniqueID == "DTMAPI.Tests.HotLoad") == 1, "Official reload should hot-load a newly enabled code mod once.");
                Assert(ReadHotLoadEntryCount(gameDir) == 1, "Hot-loaded code mod Entry should run exactly once.");
                IConfigMenuPage loadedPage = menu.GetPage("DTMAPI.Tests.HotLoad") ?? throw new InvalidOperationException("Hot-loaded mod should register a config page.");
                Assert(!loadedPage.IsLocked, "Hot-loaded enabled page should be editable.");

                runtime.NotifyWorkshopModListChanged();
                Assert(runtime.CreateSnapshot().LoadedMods.Count(m => m.Manifest.UniqueID == "DTMAPI.Tests.HotLoad") == 1, "Repeated official reload must not duplicate loaded mods.");
                Assert(ReadHotLoadEntryCount(gameDir) == 1, "Repeated official reload must not re-run Entry for an already loaded mod.");

                WriteOfficialModInfos(persistentRoot, "Local.Yuuka_DTMAPI_HotLoad", false);
                runtime.NotifyWorkshopModListChanged();
                RuntimeSnapshot disabledAfterLoad = runtime.CreateSnapshot();
                Assert(disabledAfterLoad.LoadedMods.Any(m => m.Manifest.UniqueID == "DTMAPI.Tests.HotLoad"), "Runtime should not attempt to unload a DLL after official disable.");
                IConfigMenuPage lockedPage = menu.GetPage("DTMAPI.Tests.HotLoad") ?? throw new InvalidOperationException("Loaded disabled page should still exist.");
                Assert(lockedPage.IsLocked, "Loaded disabled page should be locked until restart.");
                Assert(lockedPage.LockReason.IndexOf("重启", StringComparison.OrdinalIgnoreCase) >= 0, "Loaded disabled page should explain restart is required with Chinese-first text.");
            }
            finally
            {
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", previousRoot);
            }
        }

        private static void RuntimeUiBoundariesBlockGameplayHotkeysAndModUpdates()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
            string dir = NewTempGameDir();
            var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
            IEventsHelper events = CreateEventsProxy(runtime, "DTMAPI.Tests.Events");
            int pressed = 0;
            int released = 0;
            int updates = 0;
            int menuOpened = 0;
            int menuClosed = 0;
            events.Input.ButtonPressed += (_, _) => pressed++;
            events.Input.ButtonReleased += (_, _) => released++;
            events.GameLoop.UpdateTicked += (_, _) => updates++;
            events.UI.MenuOpened += (_, _) => menuOpened++;
            events.UI.MenuClosed += (_, _) => menuClosed++;

            runtime.Start();
            runtime.UI.SetUiContext("Gameplay", canDrawOverlay: true, gameplayHotkeysAllowed: true, reason: "unit test");
            runtime.RecordInputPressed("F10");
            runtime.RecordInputReleased("F10");
            runtime.Update();
            Assert(pressed == 1 && released == 1 && updates == 1, $"Gameplay context should deliver input and update events. pressed={pressed} released={released} updates={updates} open={runtime.UI.IsOpen} context={runtime.UI.InputContext}");

            runtime.UI.OpenConfigPage("DTMAPI.Tests.Events");
            Assert(menuOpened == 1 && runtime.UI.BlocksGameplayHotkeys && runtime.UI.BlocksModUpdates, "Open DTMAPI menu should block gameplay input and mod updates.");
            runtime.RecordInputPressed("F10");
            runtime.RecordInputReleased("F10");
            runtime.Update();
            Assert(pressed == 1 && released == 1 && updates == 1, "Open DTMAPI menu must not deliver gameplay input or UpdateTicked.");
            Assert(!runtime.IsInputDown("F10"), "Blocked input should not leave a stuck down-state.");

            runtime.UI.Close();
            Assert(menuClosed == 1, "Close should dispatch the menu closed event.");
            runtime.UI.SetUiContext("ModUiState", canDrawOverlay: true, gameplayHotkeysAllowed: false, reason: "official mod menu");
            runtime.RecordInputPressed("F10");
            runtime.RecordInputReleased("F10");
            Assert(pressed == 1 && released == 1, "Official/title UI contexts should not deliver gameplay hotkeys.");
            Assert(!runtime.IsInputDown("F10"), "Blocked official-menu input should not leave a stuck down-state.");

            runtime.UI.SetUiContext("Gameplay", canDrawOverlay: true, gameplayHotkeysAllowed: true, reason: "unit test");
            runtime.RecordInputPressed("F10");
            runtime.RecordInputReleased("F10");
            runtime.Update();
            Assert(pressed == 2 && released == 2 && updates == 2, "Gameplay input and updates should resume after closing the menu.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static string NewTempGameDir()
        {
            string dir = Path.Combine(Path.GetTempPath(), "DTMAPI-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(dir, "Mods"));
            return dir;
        }

        private static string? UseTempPersistentRoot()
        {
            string? previousRoot = Environment.GetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT");
            string root = Path.Combine(Path.GetTempPath(), "DTMAPI-tests-persistent", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(root, "SAVE"));
            Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", root);
            return previousRoot;
        }

        private static void RestorePersistentRoot(string? previousRoot)
        {
            Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", previousRoot);
        }

        private static void WriteOfficialModInfos(string persistentRoot, string officialId, bool enabled)
        {
            string saveDir = Path.Combine(persistentRoot, "SAVE");
            Directory.CreateDirectory(saveDir);
            File.WriteAllText(
                Path.Combine(saveDir, "mod_infos.json"),
                "{ \"modInfos\": { \"" + officialId + "\": { \"id\": \"" + officialId + "\", \"enabled\": " + (enabled ? "true" : "false") + ", \"priority\": -1, \"source\": \"Local\", \"title\": \"DTMAPI Test\" } } }");
        }

        private static int ReadHotLoadEntryCount(string gameDir)
        {
            string path = Path.Combine(gameDir, "DTMAPI", "config", "DTMAPI.Tests.HotLoad.hotload.txt");
            return File.Exists(path) && int.TryParse(File.ReadAllText(path), out int value) ? value : 0;
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private static void AssertThrows(Action action, string message)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException)
            {
                return;
            }
            throw new InvalidOperationException(message);
        }

        private static IEventsHelper CreateEventsProxy(DtmApiRuntime runtime, string owner)
        {
            PropertyInfo? eventsProperty = typeof(DtmApiRuntime).GetProperty("Events", BindingFlags.Instance | BindingFlags.NonPublic);
            object events = eventsProperty?.GetValue(runtime) ?? throw new InvalidOperationException("Runtime events should exist.");
            MethodInfo createProxy = events.GetType().GetMethod("CreateProxy", BindingFlags.Instance | BindingFlags.Public) ?? throw new InvalidOperationException("CreateProxy should exist.");
            return (IEventsHelper)(createProxy.Invoke(events, new object[] { owner }) ?? throw new InvalidOperationException("CreateProxy returned null."));
        }

        private sealed class FakeHost : IRuntimeHost
        {
            public FakeHost(string gamePath)
            {
                GamePath = gamePath;
                PluginPath = Path.Combine(gamePath, "BepInEx", "plugins");
            }

            public string GamePath { get; }
            public string PluginPath { get; }
            public string HostName => "UnitTest";
            public void Log(string message) { }
            public void LogWarning(string message) { }
            public void LogError(string message, Exception? exception = null) { }
        }

        private sealed class FakeActionCompletionApi : IActionCompletionApi
        {
            public void Configure(IManifest owner, ActionCompletionOptions options) { }
            public BridgeFeatureStatus GetStatus(string uniqueId) => new BridgeFeatureStatus("test", uniqueId);
        }
    }

    public sealed class HotLoadProbeMod : DtmMod
    {
        public override void Entry(IDtmHelper helper)
        {
            string configPath = helper.Config.GetConfigPath(helper.ModManifest);
            string dir = Path.GetDirectoryName(configPath) ?? string.Empty;
            Directory.CreateDirectory(dir);
            string marker = Path.Combine(dir, helper.ModManifest.UniqueID + ".hotload.txt");
            int count = File.Exists(marker) && int.TryParse(File.ReadAllText(marker), out int existing) ? existing : 0;
            File.WriteAllText(marker, (count + 1).ToString());

            IDtmConfigMenuApi? menu = helper.ModRegistry.GetApi<IDtmConfigMenuApi>("DTMAPI.ModConfigMenu");
            if (menu != null)
            {
                menu.Register(helper.ModManifest, () => { }, () => { });
                menu.AddParagraph(helper.ModManifest, () => "Hot-load probe config page.");
            }
        }
    }
}
