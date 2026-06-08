using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
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
                ManifestDependencyIsRequiredAliasSupportsOptionalDependencies();
                DependencyVersionApiVersionAndCircularDependencyDiagnostics();
                EntryDllMustRemainInsideModRoot();
                EntryDllMustBeDllFile();
                EntryTypeSelectsEntryAndMissingEntryTypeRejectsAmbiguousDll();
                MinimumGameVersionWithoutDetectedGameVersionLogsWarning();
                HelperModRegistryBindsApiRegistrationToOwner();
                HighFrequencyEventsDisableHandlersAfterConsecutiveFailures();
                EventRemoveIsOwnerBound();
                BadConfigJsonIsBackedUpAndDefaultedWithTempFileWrites();
                OffThreadTimerFallbackUpdateDoesNotDispatchOrdinaryModUpdates();
                ConfigMenuEditsSaveCancelAndDetectConflicts();
                ConfigMenuPendingPreviewDrivesConditionalVisibility();
                DisabledDiscoveredModLocksConfigPage();
                OfficialLocalModPackagesRespectOfficialEnablement();
                WorkshopReloadHotLoadsNewlyEnabledCodeModOnceAndLocksDisabledLoadedMod();
                RuntimeUiBoundariesBlockGameplayHotkeysAndModUpdates();
                Suppress_OneFrame_ClearsAfterUpdate();
                CustomEntityRegistriesValidateRegistrationDuplicateCleanupAndSnapshots();
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

        private static void ManifestDependencyIsRequiredAliasSupportsOptionalDependencies()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                string modDir = Path.Combine(dir, "Mods", "OptionalAlias");
                Directory.CreateDirectory(modDir);
                File.WriteAllText(
                    Path.Combine(modDir, "manifest.json"),
                    "{ \"Name\": \"Optional Alias\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.OptionalAlias\", \"Type\": \"ContentPack\", \"Dependencies\": [ { \"UniqueID\": \"DTMAPI.Tests.Missing\", \"IsRequired\": false } ] }");

                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                runtime.Start();
                RuntimeSnapshot snapshot = runtime.CreateSnapshot();
                Assert(snapshot.LoadedMods.Any(m => m.Manifest.UniqueID == "DTMAPI.Tests.OptionalAlias"), "IsRequired=false alias should make a missing dependency optional.");
                Assert(!snapshot.Errors.Any(e => e.Owner == "DTMAPI.Tests.OptionalAlias" && e.Message.Contains("缺少必需依赖")), "Optional alias dependency must not be reported as required missing.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void DependencyVersionApiVersionAndCircularDependencyDiagnostics()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                WriteManifest(dir, "Base", "{ \"Name\": \"Base\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.Base\", \"Type\": \"ContentPack\" }");
                WriteManifest(dir, "NeedsBase2", "{ \"Name\": \"Needs Base 2\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.NeedsBase2\", \"Type\": \"ContentPack\", \"Dependencies\": [ { \"UniqueID\": \"DTMAPI.Tests.Base\", \"MinimumVersion\": \"2.0.0\", \"Required\": true } ] }");
                WriteManifest(dir, "NeedsFutureApi", "{ \"Name\": \"Needs Future API\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.FutureApi\", \"Type\": \"ContentPack\", \"MinimumDTMApiVersion\": \"99.0.0\" }");
                WriteManifest(dir, "CycleA", "{ \"Name\": \"Cycle A\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.CycleA\", \"Type\": \"ContentPack\", \"Dependencies\": [ { \"UniqueID\": \"DTMAPI.Tests.CycleB\", \"Required\": true } ] }");
                WriteManifest(dir, "CycleB", "{ \"Name\": \"Cycle B\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.CycleB\", \"Type\": \"ContentPack\", \"Dependencies\": [ { \"UniqueID\": \"DTMAPI.Tests.CycleA\", \"Required\": true } ] }");

                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                runtime.Start();
                RuntimeSnapshot snapshot = runtime.CreateSnapshot();
                Assert(snapshot.LoadedMods.Any(m => m.Manifest.UniqueID == "DTMAPI.Tests.Base"), "Base dependency should load.");
                Assert(!snapshot.LoadedMods.Any(m => m.Manifest.UniqueID == "DTMAPI.Tests.NeedsBase2"), "Required dependency version mismatch should block loading.");
                Assert(!snapshot.LoadedMods.Any(m => m.Manifest.UniqueID == "DTMAPI.Tests.FutureApi"), "Future MinimumDTMApiVersion should block loading.");
                Assert(!snapshot.LoadedMods.Any(m => m.Manifest.UniqueID == "DTMAPI.Tests.CycleA"), "CycleA should be blocked and must not load.");
                Assert(!snapshot.LoadedMods.Any(m => m.Manifest.UniqueID == "DTMAPI.Tests.CycleB"), "CycleB should be blocked and must not load.");
                Assert(snapshot.Errors.Any(e => e.Owner == "DTMAPI.Tests.NeedsBase2" && e.Message.Contains("依赖版本")), "Dependency version mismatch should be diagnosed.");
                Assert(snapshot.Errors.Any(e => e.Owner == "DTMAPI.Tests.FutureApi" && e.Message.Contains("API 版本")), "MinimumDTMApiVersion mismatch should be diagnosed.");
                Assert(snapshot.Errors.Any(e => e.Message.Contains("依赖循环") && e.Details.Contains("DTMAPI.Tests.CycleA") && e.Details.Contains("DTMAPI.Tests.CycleB")), "Circular dependencies should be diagnosed with the cycle path.");
                Assert(snapshot.Errors.Any(e => e.Owner == "DTMAPI.Tests.CycleA" && e.Message.Contains("依赖循环阻止加载")), "CycleA should have an owner-specific blocked diagnostic.");
                Assert(snapshot.Errors.Any(e => e.Owner == "DTMAPI.Tests.CycleB" && e.Message.Contains("依赖循环阻止加载")), "CycleB should have an owner-specific blocked diagnostic.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void EntryDllMustRemainInsideModRoot()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                WriteManifest(dir, "EscapingDll", "{ \"Name\": \"Escaping DLL\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.EscapingDll\", \"EntryDll\": \"../Escaping.dll\", \"Type\": \"CodeMod\" }");

                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                runtime.Start();
                RuntimeSnapshot snapshot = runtime.CreateSnapshot();
                Assert(!snapshot.LoadedMods.Any(m => m.Manifest.UniqueID == "DTMAPI.Tests.EscapingDll"), "EntryDll escaping the mod root must not load.");
                Assert(snapshot.Errors.Any(e => e.Owner == "DTMAPI.Tests.EscapingDll" && e.Message.Contains("不能逃出")), "Escaping EntryDll should have a path-safety diagnostic.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void EntryDllMustBeDllFile()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                WriteManifest(dir, "NotDll", "{ \"Name\": \"Not DLL\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.NotDll\", \"EntryDll\": \"NotDll.txt\", \"Type\": \"CodeMod\" }");

                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                runtime.Start();
                RuntimeSnapshot snapshot = runtime.CreateSnapshot();
                Assert(!snapshot.LoadedMods.Any(m => m.Manifest.UniqueID == "DTMAPI.Tests.NotDll"), "EntryDll with a non-.dll extension must not load.");
                Assert(snapshot.Errors.Any(e => e.Owner == "DTMAPI.Tests.NotDll" && e.Message.Contains(".dll")), "Non-.dll EntryDll should have a clear diagnostic.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void EntryTypeSelectsEntryAndMissingEntryTypeRejectsAmbiguousDll()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                string assemblyPath = typeof(ApiOwnerProbeMod).Assembly.Location;
                string assemblyName = Path.GetFileName(assemblyPath);
                string selectedEntryType = typeof(ApiOwnerProbeMod).FullName ?? nameof(ApiOwnerProbeMod);

                string ambiguousDir = Path.Combine(dir, "Mods", "AmbiguousEntry");
                Directory.CreateDirectory(ambiguousDir);
                File.Copy(assemblyPath, Path.Combine(ambiguousDir, assemblyName), overwrite: true);
                File.WriteAllText(
                    Path.Combine(ambiguousDir, "manifest.json"),
                    "{ \"Name\": \"Ambiguous Entry\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.AmbiguousEntry\", \"EntryDll\": \"" + assemblyName + "\", \"Type\": \"CodeMod\" }");

                string selectedDir = Path.Combine(dir, "Mods", "SelectedEntry");
                Directory.CreateDirectory(selectedDir);
                File.Copy(assemblyPath, Path.Combine(selectedDir, assemblyName), overwrite: true);
                File.WriteAllText(
                    Path.Combine(selectedDir, "manifest.json"),
                    "{ \"Name\": \"Selected Entry\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.SelectedEntry\", \"EntryDll\": \"" + assemblyName + "\", \"EntryType\": \"" + selectedEntryType + "\", \"Type\": \"CodeMod\" }");

                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                runtime.Start();
                RuntimeSnapshot snapshot = runtime.CreateSnapshot();
                Assert(snapshot.DiscoveredMods.Single(m => m.Manifest.UniqueID == "DTMAPI.Tests.SelectedEntry").Manifest.EntryType == selectedEntryType, "Manifest should parse and preserve EntryType.");
                Assert(!snapshot.LoadedMods.Any(m => m.Manifest.UniqueID == "DTMAPI.Tests.AmbiguousEntry"), "DLLs with multiple DtmMod subclasses require EntryType.");
                Assert(snapshot.Errors.Any(e => e.Owner == "DTMAPI.Tests.AmbiguousEntry" && e.Message.Contains("EntryType 缺失")), "Missing EntryType in an ambiguous DLL should be diagnosed.");
                Assert(snapshot.LoadedMods.Any(m => m.Manifest.UniqueID == "DTMAPI.Tests.SelectedEntry"), "EntryType should select the requested DtmMod subclass.");
                IModRegistry registry = GetModRegistry(runtime);
                IUnitProbeApi? selectedApi = registry.GetApi<IUnitProbeApi>("DTMAPI.Tests.SelectedEntry");
                Assert(selectedApi != null && selectedApi.Owner == "DTMAPI.Tests.SelectedEntry", "Selected EntryType should execute the chosen DtmMod Entry method.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void MinimumGameVersionWithoutDetectedGameVersionLogsWarning()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                WriteManifest(dir, "NeedsGameVersion", "{ \"Name\": \"Needs Game Version\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.NeedsGameVersion\", \"Type\": \"ContentPack\", \"MinimumGameVersion\": \"99.0.0\" }");

                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                runtime.Start();
                RuntimeSnapshot snapshot = runtime.CreateSnapshot();
                Assert(snapshot.LoadedMods.Any(m => m.Manifest.UniqueID == "DTMAPI.Tests.NeedsGameVersion"), "Missing game-version detection should warn but not block loading.");
                string log = File.ReadAllText(runtime.Diagnostics.GetLatestLogPath());
                Assert(log.Contains("[Warn]") && log.Contains("MinimumGameVersion") && log.Contains("cannot detect the game version"), "MinimumGameVersion should emit an explicit warning when game version cannot be detected.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void HelperModRegistryBindsApiRegistrationToOwner()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                string modDir = Path.Combine(dir, "Mods", "ApiOwner");
                Directory.CreateDirectory(modDir);
                string assemblyPath = typeof(ApiOwnerProbeMod).Assembly.Location;
                string assemblyName = Path.GetFileName(assemblyPath);
                File.Copy(assemblyPath, Path.Combine(modDir, assemblyName), overwrite: true);
                File.WriteAllText(
                    Path.Combine(modDir, "manifest.json"),
                    "{ \"Name\": \"API Owner\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.ApiOwner\", \"EntryDll\": \"" + assemblyName + "\", \"EntryType\": \"" + (typeof(ApiOwnerProbeMod).FullName ?? nameof(ApiOwnerProbeMod)) + "\", \"Type\": \"CodeMod\" }");

                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                runtime.Start();
                IModRegistry registry = GetModRegistry(runtime);
                IUnitProbeApi? ownedApi = registry.GetApi<IUnitProbeApi>("DTMAPI.Tests.ApiOwner");
                Assert(ownedApi != null && ownedApi.Owner == "DTMAPI.Tests.ApiOwner", "Helper registry should register APIs under the helper's mod owner.");
                Assert(registry.GetApi<IUnitProbeApi>("DTMAPI.Tests.SpoofedOwner") == null, "Helper registry should not let mods spoof another API owner.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void HighFrequencyEventsDisableHandlersAfterConsecutiveFailures()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                IEventsHelper events = CreateEventsProxy(runtime, "DTMAPI.Tests.ThrowingEvents");
                int updateCalls = 0;
                int secondCalls = 0;
                events.GameLoop.UpdateTicked += (_, _) =>
                {
                    updateCalls++;
                    throw new InvalidOperationException("update failure");
                };
                events.GameLoop.OneSecondUpdateTicked += (_, _) =>
                {
                    secondCalls++;
                    throw new InvalidOperationException("second failure");
                };

                runtime.Start();
                for (int i = 0; i < 5; i++)
                    runtime.Update();
                for (uint i = 0; i < 5; i++)
                    DispatchOneSecondUpdateTicked(runtime, i);

                RuntimeSnapshot snapshot = runtime.CreateSnapshot();
                Assert(updateCalls == 3, "UpdateTicked handler should be disabled after three consecutive failures.");
                Assert(secondCalls == 3, "OneSecondUpdateTicked handler should be disabled after three consecutive failures.");
                Assert(snapshot.Errors.Any(e => e.Owner == "DTMAPI.Tests.ThrowingEvents" && e.Message.Contains("GameLoop.UpdateTicked") && e.Message.Contains("disabled")), "UpdateTicked circuit breaker should record a diagnostic.");
                Assert(snapshot.Errors.Any(e => e.Owner == "DTMAPI.Tests.ThrowingEvents" && e.Message.Contains("GameLoop.OneSecondUpdateTicked") && e.Message.Contains("disabled")), "OneSecondUpdateTicked circuit breaker should record a diagnostic.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void EventRemoveIsOwnerBound()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                IEventsHelper ownerA = CreateEventsProxy(runtime, "DTMAPI.Tests.OwnerA");
                IEventsHelper ownerB = CreateEventsProxy(runtime, "DTMAPI.Tests.OwnerB");
                int updateCalls = 0;
                EventHandler<UpdateTickedEventArgs> sharedHandler = (_, _) => updateCalls++;

                ownerA.GameLoop.UpdateTicked += sharedHandler;
                ownerB.GameLoop.UpdateTicked += sharedHandler;
                ownerA.GameLoop.UpdateTicked -= sharedHandler;

                runtime.Start();
                runtime.Update();
                Assert(updateCalls == 1, "Owner A removing a shared delegate must not remove Owner B's event subscription.");

                ownerB.GameLoop.UpdateTicked -= sharedHandler;
                runtime.Update();
                Assert(updateCalls == 1, "Owner B removing its own delegate should remove the remaining subscription.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void BadConfigJsonIsBackedUpAndDefaultedWithTempFileWrites()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                runtime.Start();
                IConfigHelper config = GetConfig(runtime);
                IManifest manifest = new ManifestModel
                {
                    Name = "Bad Config",
                    Author = "DTMAPI",
                    Version = "1.0.0",
                    UniqueID = "DTMAPI.Tests.BadConfig"
                };
                string path = config.GetConfigPath(manifest);
                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
                File.WriteAllText(path, "{ this is not valid json");

                SampleConfig restored = config.ReadConfig<SampleConfig>(manifest);
                Assert(restored.Enabled && restored.Count == 7, "Bad config JSON should restore default config values.");
                Assert(Directory.GetFiles(Path.GetDirectoryName(path) ?? ".", Path.GetFileName(path) + ".invalid-*.bak").Length == 1, "Bad config JSON should be backed up.");
                Assert(runtime.CreateSnapshot().Errors.Any(e => e.Owner == manifest.UniqueID && e.Message.Contains("配置 JSON 损坏")), "Bad config JSON should be diagnosed.");

                restored.Count = 11;
                config.WriteConfig(manifest, restored);
                Assert(config.ReadConfig<SampleConfig>(manifest).Count == 11, "Config writes should round-trip after recovery.");
                Assert(Directory.GetFiles(Path.GetDirectoryName(path) ?? ".", "*.tmp").Length == 0, "Config writes should not leave temp files behind.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void OffThreadTimerFallbackUpdateDoesNotDispatchOrdinaryModUpdates()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                IEventsHelper events = CreateEventsProxy(runtime, "DTMAPI.Tests.TimerFallback");
                int updates = 0;
                events.GameLoop.UpdateTicked += (_, _) => updates++;
                runtime.Start();

                Exception? threadError = null;
                var thread = new Thread(() =>
                {
                    try
                    {
                        runtime.Update();
                    }
                    catch (Exception ex)
                    {
                        threadError = ex;
                    }
                });
                thread.Start();
                thread.Join();
                if (threadError != null)
                    throw threadError;
                Assert(updates == 0, "Off-thread TimerFallback update must not dispatch ordinary UpdateTicked callbacks.");

                runtime.Update();
                Assert(updates == 1, "Runtime-thread update should still dispatch ordinary UpdateTicked callbacks.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void ConfigMenuEditsSaveCancelAndDetectConflicts()
        {
            var menu = new ConfigMenuRegistry();
            IConfigMenuRuntime menuRuntime = menu;
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

            IConfigMenuPage page = menuRuntime.GetPage(manifest.UniqueID) ?? throw new InvalidOperationException("Page should exist.");
            menuRuntime.BeginEditing(manifest.UniqueID);
            Assert(page.Items[0].TrySetPendingValue("true", out _), "Bool option should accept true.");
            Assert(page.Items[1].TrySetPendingValue("3.25", out _), "Number option should accept numeric input.");
            Assert(page.Items[2].TrySetPendingValue("after", out _), "Text option should accept text input.");
            Assert(page.Items[3].TrySetPendingValue("Fast", out _), "Choice option should accept known choices.");
            Assert(page.HasPendingChanges, "Pending edits should be tracked.");

            menuRuntime.Cancel(manifest.UniqueID);
            Assert(!enabled && multiplier == 1.0 && label == "before" && choice == "Safe", "Cancel should discard pending edits.");

            menuRuntime.BeginEditing(manifest.UniqueID);
            page.Items[0].TrySetPendingValue("true", out _);
            page.Items[1].TrySetPendingValue("3.25", out _);
            page.Items[2].TrySetPendingValue("after", out _);
            page.Items[3].TrySetPendingValue("Fast", out _);
            menuRuntime.Save(manifest.UniqueID);
            Assert(saved == 1, "Save callback should run.");
            Assert(enabled && Math.Abs(multiplier - 3.5) < 0.001 && label == "after" && choice == "Fast", "Save should apply pending values.");

            menuRuntime.Reset(manifest.UniqueID);
            Assert(reset == 1, "Reset callback should run.");
            Assert(page.HasPendingChanges, "Reset should remain pending until save.");
            menuRuntime.Cancel(manifest.UniqueID);
            Assert(enabled && Math.Abs(multiplier - 3.5) < 0.001 && label == "after" && choice == "Fast", "Cancel after reset should restore committed values.");

            menuRuntime.BeginEditing(manifest.UniqueID);
            Assert(page.Items[4].TrySetPendingValue("F9", out _), "Keybind option should accept captured keys.");
            string conflict = menu.GetKeybindConflicts(manifest.UniqueID).Single();
            Assert(conflict.StartsWith("按键冲突：F9", StringComparison.Ordinal), "Duplicate keybinds should be reported with Chinese-first conflict text.");
            bool conflictBlocked = false;
            try
            {
                menuRuntime.Save(manifest.UniqueID);
            }
            catch (InvalidOperationException)
            {
                conflictBlocked = true;
            }
            Assert(conflictBlocked, "Save should block keybind conflicts.");
        }

        private static void ConfigMenuPendingPreviewDrivesConditionalVisibility()
        {
            var menu = new ConfigMenuRegistry();
            IConfigMenuRuntime menuRuntime = menu;
            IManifest manifest = new ManifestModel
            {
                Name = "Conditional Menu Test",
                Author = "DTMAPI",
                Version = "1.0.0",
                UniqueID = "DTMAPI.Tests.ConditionalMenu"
            };
            string colorPreset = "Orange";
            string hex = "FF942E";

            menu.Register(manifest, () =>
            {
                colorPreset = "Orange";
                hex = "FF942E";
            }, () => { });
            menu.AddColorPresetOption(
                manifest,
                () => "Color",
                () => "",
                () => colorPreset,
                value => colorPreset = value,
                new[]
                {
                    new DtmColorPreset("Orange", "Orange", "FF942E"),
                    new DtmColorPreset("Custom", "Custom", hex)
                });
            Func<bool> customSelected = () => colorPreset.Equals("Custom", StringComparison.OrdinalIgnoreCase);
            menu.AddTextOption(manifest, () => "Hex", () => "", () => hex, value => hex = value, customSelected, customSelected);

            IConfigMenuPage page = menuRuntime.GetPage(manifest.UniqueID) ?? throw new InvalidOperationException("Page should exist.");
            menuRuntime.BeginEditing(manifest.UniqueID);
            Assert(page.Items.Count(item => item.Kind == "Text") == 0, "Non-custom color should hide the custom text input.");
            IConfigMenuItem colorItem = page.Items.Single(item => item.Kind == "ColorPreset");
            Assert(colorItem.TrySetPendingValue("Custom", out _), "Custom color preset should be selectable.");
            Assert(colorPreset == "Orange", "Pending edits should not permanently apply before save.");

            using (menuRuntime.PreviewPendingValues(page) ?? throw new InvalidOperationException("Pending preview should be available."))
            {
                Assert(colorPreset == "Custom", "Pending preview should temporarily expose the selected custom preset.");
                IConfigMenuItem textItem = page.Items.Single(item => item.Kind == "Text");
                Assert(textItem.CanEdit, "Custom color text input should be editable inside pending preview.");
            }

            Assert(colorPreset == "Orange", "Pending preview should restore the committed color after rendering.");
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
            IConfigMenuRuntime menuRuntime = menu;
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

            IConfigMenuPage page = menuRuntime.GetPage(manifest.UniqueID) ?? throw new InvalidOperationException("Disabled page should exist.");
            Assert(page.IsLocked, "Disabled discovered mod should lock its config page.");
            Assert(page.LockReason.IndexOf("本地 DTMAPI 禁用标记", StringComparison.OrdinalIgnoreCase) >= 0, "Disabled lock should explain the local marker with Chinese-first text.");

            menuRuntime.BeginEditing(manifest.UniqueID);
            Assert(page.Items[0].TrySetPendingValue("true", out _), "Locked page may stage text but must not apply it.");
            AssertThrows(() => menuRuntime.Save(manifest.UniqueID), "Locked page save should be rejected.");
            AssertThrows(() => menuRuntime.Reset(manifest.UniqueID), "Locked page reset should be rejected.");
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
                "{ \"Name\": \"Hot Load Test\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.HotLoad\", \"EntryDll\": \"Content/DTMAPI/" + assemblyName + "\", \"EntryType\": \"" + (typeof(HotLoadProbeMod).FullName ?? nameof(HotLoadProbeMod)) + "\", \"Type\": \"CodeMod\" }");

            string? previousRoot = Environment.GetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT");
            try
            {
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", persistentRoot);
                WriteOfficialModInfos(persistentRoot, "Local.Yuuka_DTMAPI_HotLoad", false);

                var menu = new ConfigMenuRegistry();
                IConfigMenuRuntime menuRuntime = menu;
                var runtime = new DtmApiRuntime(new FakeHost(gameDir), menu);
                runtime.Start();
                Assert(!runtime.CreateSnapshot().LoadedMods.Any(m => m.Manifest.UniqueID == "DTMAPI.Tests.HotLoad"), "Disabled official package should not load at startup.");

                WriteOfficialModInfos(persistentRoot, "Local.Yuuka_DTMAPI_HotLoad", true);
                runtime.NotifyWorkshopModListChanged();
                RuntimeSnapshot loadedSnapshot = runtime.CreateSnapshot();
                Assert(loadedSnapshot.LoadedMods.Count(m => m.Manifest.UniqueID == "DTMAPI.Tests.HotLoad") == 1, "Official reload should hot-load a newly enabled code mod once.");
                Assert(ReadHotLoadEntryCount(gameDir) == 1, "Hot-loaded code mod Entry should run exactly once.");
                IConfigMenuPage loadedPage = menuRuntime.GetPage("DTMAPI.Tests.HotLoad") ?? throw new InvalidOperationException("Hot-loaded mod should register a config page.");
                Assert(!loadedPage.IsLocked, "Hot-loaded enabled page should be editable.");

                runtime.NotifyWorkshopModListChanged();
                Assert(runtime.CreateSnapshot().LoadedMods.Count(m => m.Manifest.UniqueID == "DTMAPI.Tests.HotLoad") == 1, "Repeated official reload must not duplicate loaded mods.");
                Assert(ReadHotLoadEntryCount(gameDir) == 1, "Repeated official reload must not re-run Entry for an already loaded mod.");

                WriteOfficialModInfos(persistentRoot, "Local.Yuuka_DTMAPI_HotLoad", false);
                runtime.NotifyWorkshopModListChanged();
                RuntimeSnapshot disabledAfterLoad = runtime.CreateSnapshot();
                Assert(disabledAfterLoad.LoadedMods.Any(m => m.Manifest.UniqueID == "DTMAPI.Tests.HotLoad"), "Runtime should not attempt to unload a DLL after official disable.");
                IConfigMenuPage lockedPage = menuRuntime.GetPage("DTMAPI.Tests.HotLoad") ?? throw new InvalidOperationException("Loaded disabled page should still exist.");
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

        private static void Suppress_OneFrame_ClearsAfterUpdate()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                IInputHelper input = GetInput(runtime);

                runtime.Start();
                runtime.UI.SetUiContext("Gameplay", canDrawOverlay: true, gameplayHotkeysAllowed: true, reason: "unit test");
                input.RegisterButton("F10");
                runtime.RecordInputPressed("F10");
                input.Suppress("F10");

                Assert(input.WasPressed("F10"), "Pressed input should be visible during the frame it is recorded.");
                Assert(input.IsDown("F10"), "Pressed input should remain down until release.");
                Assert(input.GetSuppressedButtons().Contains("F10", StringComparer.OrdinalIgnoreCase), "Suppressed input should be visible during the current frame.");

                runtime.Update();
                Assert(!input.WasPressed("F10"), "Runtime update should clear one-frame pressed input state.");
                Assert(!input.GetSuppressedButtons().Contains("F10", StringComparer.OrdinalIgnoreCase), "Runtime update should clear one-frame suppressed input state.");
                Assert(input.IsDown("F10"), "Clearing frame state should not release the input down-state.");

                runtime.RecordInputReleased("F10");
                Assert(!input.IsDown("F10"), "Released input should clear the down-state.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void CustomEntityRegistriesValidateRegistrationDuplicateCleanupAndSnapshots()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
            string dir = NewTempGameDir();
            var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
            runtime.Start();

            IModRegistry registry = GetModRegistry(runtime);
            Assert(registry.GetApi<ICustomAnimalApi>("DTMAPI") != null, "Custom animal registry API should remain registered by the runtime manifest.");
            Assert(registry.GetApi<ICustomMonsterApi>("DTMAPI") != null, "Custom monster registry API should remain registered by the runtime manifest.");
            Assert(registry.GetApi<ICustomAttackApi>("DTMAPI") != null, "Custom attack registry API should remain registered by the runtime manifest.");
            Assert(registry.GetApi<ICustomDroneApi>("DTMAPI") != null, "Custom drone registry API should remain registered by the runtime manifest.");

            IManifest owner = new ManifestModel
            {
                Name = "Custom Entity Test",
                Author = "DTMAPI",
                Version = "1.0.0",
                UniqueID = "DTMAPI.Tests.CustomEntity"
            };

            runtime.CustomEntities.AnimalLifecycleChanged += (_, __) => throw new InvalidOperationException("listener isolation probe");

            CustomAnimalRegistrationResult invalidAnimal = runtime.CustomEntities.RegisterSpecies(owner, new CustomAnimalSpeciesDefinition { SpeciesId = "NotNamespaced" });
            Assert(!invalidAnimal.Succeeded && invalidAnimal.Messages.Any(m => m.Code == "definition-id-not-namespaced"), "Invalid non-namespaced animal IDs should be rejected.");

            string animalId = owner.UniqueID + ".Animal";
            string monsterId = owner.UniqueID + ".Monster";
            string attackId = owner.UniqueID + ".Attack";
            string droneId = owner.UniqueID + ".Drone";
            CustomAnimalRegistrationResult animal = runtime.CustomEntities.RegisterSpecies(owner, new CustomAnimalSpeciesDefinition
            {
                SpeciesId = animalId,
                DisplayName = TestText("Animal"),
                Diet = new CustomAnimalDietPolicy { AcceptedItemIds = new[] { "hay" }, UnitsPerFeeding = 1 },
                Excrement = new CustomAnimalExcrementPolicy { Enabled = true, Outputs = new[] { new CustomAnimalItemOutput { ItemId = "poop", MinStack = 1, MaxStack = 1 } } },
                Breeding = new CustomAnimalBreedingPolicy { Enabled = true, CompatibleSpeciesIds = new[] { animalId }, OffspringCount = 1 },
                HiddenProducts = new[] { new CustomAnimalProductRule { ProductId = owner.UniqueID + ".HiddenProduct", RequiredProgress = 100, Outputs = new[] { new CustomAnimalItemOutput { ItemId = "hidden", MinStack = 1, MaxStack = 1 } } } },
                Persistence = TestPersistence()
            });
            Assert(animal.Succeeded, "Valid animal definition should register.");
            Assert(runtime.CreateSnapshot().Errors.Any(e => e.Owner == owner.UniqueID && e.Message.Contains("lifecycle listener")), "Throwing lifecycle listeners should be captured with owner attribution.");

            CustomAnimalRegistrationResult duplicateAnimal = runtime.CustomEntities.RegisterSpecies(owner, new CustomAnimalSpeciesDefinition { SpeciesId = animalId });
            Assert(!duplicateAnimal.Succeeded && duplicateAnimal.FailureReason == "duplicate-definition-id", "Duplicate animal definition IDs should be rejected.");

            CustomAttackRegistrationResult attack = runtime.CustomEntities.RegisterAttack(owner, new CustomAttackDefinition
            {
                AttackId = attackId,
                Damage = new CustomDamagePayload { Amount = 2, DamageType = "test" },
                Pattern = new CustomBarragePatternDefinition { Kind = CustomAttackPatternKind.Barrage, ProjectileCount = 3, DeterministicRandomSeed = true }
            });
            Assert(attack.Succeeded, "Valid attack definition should register.");

            CustomMonsterRegistrationResult monster = runtime.CustomEntities.RegisterMonster(owner, new CustomMonsterDefinition
            {
                MonsterId = monsterId,
                SpawnRules = new[] { new CustomMonsterSpawnRule { RuleId = owner.UniqueID + ".Spawn", RoomTags = new[] { "test" }, Probability = 1 } },
                AttackSlots = new[] { new CustomMonsterAttackSlot { SlotId = "primary", AttackId = attackId, CooldownSeconds = 1, Range = 5 } },
                Loot = new[] { new CustomMonsterLootRule { ItemId = "test-loot", MinStack = 1, MaxStack = 1, Chance = 1 } },
                Persistence = TestPersistence()
            });
            Assert(monster.Succeeded, "Valid monster definition should register.");
            CustomMonsterRegistrationResult spawnTable = runtime.CustomEntities.RegisterSpawnTable(owner, new CustomMonsterSpawnTableDefinition { SpawnTableId = owner.UniqueID + ".SpawnTable", MonsterIds = new[] { monsterId } });
            Assert(spawnTable.Succeeded, "Valid monster spawn table should register.");

            CustomDroneRegistrationResult drone = runtime.CustomEntities.RegisterDrone(owner, new CustomDroneDefinition
            {
                DroneId = droneId,
                SupportedModes = new[] { CustomDroneBehaviorMode.Follow, CustomDroneBehaviorMode.Guard, CustomDroneBehaviorMode.Attack },
                EquipmentSlots = new[] { new CustomDroneEquipmentSlotDefinition { SlotId = "weapon", AllowedItemTags = new[] { "weapon" } } },
                AttackIds = new[] { attackId },
                Persistence = TestPersistence()
            });
            Assert(drone.Succeeded, "Valid drone definition should register.");

            Assert(runtime.CustomEntities.GetAnimalSnapshot(owner.UniqueID).RegisteredDefinitionCount == 1, "Animal snapshot should count registered definitions.");
            Assert(runtime.CustomEntities.GetMonsterSnapshot(owner.UniqueID).RuntimeStatus == CustomEntityRuntimeStatus.ConfiguredNoRuntimeInstance, "Monster snapshot should report configured with no runtime instance.");
            Assert(runtime.CustomEntities.GetAttackStatus(owner.UniqueID).Status == "configured-no-runtime-instance", "Attack status should report configured-no-runtime-instance.");
            Assert(runtime.CustomEntities.GetDroneStatus(owner.UniqueID).FailureReason == "runtime-creation-blocked", "Drone status should expose runtime creation blocker.");

            Assert(IsBlocked(runtime.CustomEntities.RequestSpawn(owner, new CustomAnimalSpawnRequest { SpeciesId = animalId })), "Animal spawn should return runtime-creation-blocked before native adapters are verified.");
            Assert(IsBlocked(runtime.CustomEntities.RequestSpawn(owner, new CustomMonsterSpawnRequest { MonsterId = monsterId })), "Monster spawn should return runtime-creation-blocked before native adapters are verified.");
            Assert(IsBlocked(runtime.CustomEntities.ExecuteAttack(owner, new CustomAttackSpawnRequest { AttackId = attackId })), "Attack execution should return runtime-creation-blocked before native adapters are verified.");
            Assert(IsBlocked(runtime.CustomEntities.RequestSummon(owner, new CustomDroneSummonRequest { DroneId = droneId })), "Drone summon should return runtime-creation-blocked before native adapters are verified.");

            runtime.NotifyLoadGameRequested(2);
            runtime.NotifySaveLoaded(isNewGame: false);
            Assert(runtime.CustomEntities.GetAnimalSnapshot(owner.UniqueID).ActiveRuntimeInstanceCount == 0, "Save-load boundary should not keep stale custom animal runtime instances.");

            int removed = runtime.CustomEntities.RemoveOwner(owner.UniqueID, "unit test cleanup");
            Assert(removed >= 5, "Owner cleanup should remove four definitions plus the monster spawn table.");
            Assert(runtime.CustomEntities.GetAnimalSnapshot(owner.UniqueID).RegisteredDefinitionCount == 0, "Animal definitions should be removed by owner cleanup.");
            Assert(runtime.CustomEntities.GetMonsterSnapshot(owner.UniqueID).RegisteredDefinitionCount == 0, "Monster definitions should be removed by owner cleanup.");
            Assert(runtime.CustomEntities.GetAttackSnapshot(owner.UniqueID).RegisteredDefinitionCount == 0, "Attack definitions should be removed by owner cleanup.");
            Assert(runtime.CustomEntities.GetDroneSnapshot(owner.UniqueID).RegisteredDefinitionCount == 0, "Drone definitions should be removed by owner cleanup.");
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

        private static void WriteManifest(string gameDir, string folderName, string json)
        {
            string modDir = Path.Combine(gameDir, "Mods", folderName);
            Directory.CreateDirectory(modDir);
            File.WriteAllText(Path.Combine(modDir, "manifest.json"), json);
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

        private static bool IsBlocked(CustomEntityRequestResult result)
        {
            return result != null && !result.Succeeded && result.FailureReason == "runtime-creation-blocked" && result.RuntimeStatus == CustomEntityRuntimeStatus.RuntimeCreationBlocked;
        }

        private static CustomEntityLocalizedText TestText(string text)
        {
            return new CustomEntityLocalizedText
            {
                Default = text,
                English = text,
                SimplifiedChinese = text
            };
        }

        private static CustomEntityPersistencePolicy TestPersistence()
        {
            return new CustomEntityPersistencePolicy
            {
                Kind = CustomEntityPersistenceKind.SaveScoped,
                SchemaVersion = 1,
                RemoveInstancesWhenOwnerMissing = true,
                SaveKeys = new[] { new CustomEntitySaveDataKey { Key = "state", Version = 1 } }
            };
        }

        private static IModRegistry GetModRegistry(DtmApiRuntime runtime)
        {
            PropertyInfo? registryProperty = typeof(DtmApiRuntime).GetProperty("ModRegistry", BindingFlags.Instance | BindingFlags.NonPublic);
            return (IModRegistry)(registryProperty?.GetValue(runtime) ?? throw new InvalidOperationException("Runtime mod registry should exist."));
        }

        private static IConfigHelper GetConfig(DtmApiRuntime runtime)
        {
            PropertyInfo? configProperty = typeof(DtmApiRuntime).GetProperty("Config", BindingFlags.Instance | BindingFlags.NonPublic);
            return (IConfigHelper)(configProperty?.GetValue(runtime) ?? throw new InvalidOperationException("Runtime config helper should exist."));
        }

        private static IInputHelper GetInput(DtmApiRuntime runtime)
        {
            PropertyInfo? inputProperty = typeof(DtmApiRuntime).GetProperty("Input", BindingFlags.Instance | BindingFlags.NonPublic);
            return (IInputHelper)(inputProperty?.GetValue(runtime) ?? throw new InvalidOperationException("Runtime input helper should exist."));
        }

        private static IEventsHelper CreateEventsProxy(DtmApiRuntime runtime, string owner)
        {
            PropertyInfo? eventsProperty = typeof(DtmApiRuntime).GetProperty("Events", BindingFlags.Instance | BindingFlags.NonPublic);
            object events = eventsProperty?.GetValue(runtime) ?? throw new InvalidOperationException("Runtime events should exist.");
            MethodInfo createProxy = events.GetType().GetMethod("CreateProxy", BindingFlags.Instance | BindingFlags.Public) ?? throw new InvalidOperationException("CreateProxy should exist.");
            return (IEventsHelper)(createProxy.Invoke(events, new object[] { owner }) ?? throw new InvalidOperationException("CreateProxy returned null."));
        }

        private static void DispatchOneSecondUpdateTicked(DtmApiRuntime runtime, uint second)
        {
            PropertyInfo? eventsProperty = typeof(DtmApiRuntime).GetProperty("Events", BindingFlags.Instance | BindingFlags.NonPublic);
            object events = eventsProperty?.GetValue(runtime) ?? throw new InvalidOperationException("Runtime events should exist.");
            MethodInfo dispatch = events.GetType().GetMethod("DispatchOneSecondUpdateTicked", BindingFlags.Instance | BindingFlags.Public) ?? throw new InvalidOperationException("DispatchOneSecondUpdateTicked should exist.");
            dispatch.Invoke(events, new object[] { second });
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

        [DataContract]
        private sealed class SampleConfig
        {
            [DataMember] public bool Enabled { get; set; } = true;
            [DataMember] public int Count { get; set; } = 7;
        }

        public interface IUnitProbeApi
        {
            string Owner { get; }
        }

        public sealed class UnitProbeApi : IUnitProbeApi
        {
            public UnitProbeApi(string owner) => Owner = owner;
            public string Owner { get; }
        }
    }

    public sealed class HotLoadProbeMod : DtmMod
    {
        public override void Entry(IDtmHelper helper)
        {
            helper.ModRegistry.RegisterApi<Program.IUnitProbeApi>(new Program.UnitProbeApi(helper.ModManifest.UniqueID));

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

    public sealed class ApiOwnerProbeMod : DtmMod
    {
        public override void Entry(IDtmHelper helper)
        {
            helper.ModRegistry.RegisterApi<Program.IUnitProbeApi>(new Program.UnitProbeApi(helper.ModManifest.UniqueID));
        }
    }
}
