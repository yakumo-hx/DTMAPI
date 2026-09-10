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
using DTMAPI.ModConfigMenu;

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {

        private static void RegistryAndConfigFacadesEnforceOwnerLifetime()
        {
            var registry = new ModRegistryService();
            var provider = new ManifestModel { Name = "Provider", Author = "DTMAPI", Version = "1.0.0", UniqueID = "DTMAPI.Tests.Provider" };
            var consumer = new ManifestModel { Name = "Consumer", Author = "DTMAPI", Version = "1.0.0", UniqueID = "DTMAPI.Tests.Consumer" };
            var spoof = new ManifestModel { Name = "Spoof", Author = "DTMAPI", Version = "1.0.0", UniqueID = "DTMAPI.Tests.Spoof" };
            var dual = new DualProbeApi(provider.UniqueID);
            registry.RegisterApiForOwner<IUnitProbeApi>(provider, dual);
            AssertThrows(() => registry.RegisterApiForOwner<IUnitProbeApi>(provider, new UnitProbeApi("replacement")), "Duplicate provider/contract registration must throw instead of replacing the original API.");
            Assert(ReferenceEquals(registry.GetApi<IUnitProbeApi>(provider.UniqueID), dual), "Duplicate registration failure must preserve the original provider object.");
            registry.RegisterApiForOwner<ISecondProbeApi>(provider, dual);
            Assert(ReferenceEquals(registry.GetApi<ISecondProbeApi>(provider.UniqueID), dual), "One object may register distinct API contracts for the same owner.");

            var menu = new ConfigMenuRegistry();
            var menuProvider = new ManifestModel { Name = "Menu", Author = "DTMAPI", Version = "1.0.0", UniqueID = "DTMAPI.ModConfigMenu" };
            registry.RegisterApiForOwner<IDtmConfigMenuApi>(menuProvider, menu);
            bool consumerActive = true;
            IModRegistry consumerRegistry = registry.CreateOwnerBoundRegistry(consumer, () =>
            {
                if (!consumerActive)
                    throw new InvalidOperationException("consumer inactive");
            });
            IDtmConfigMenuApi facade = consumerRegistry.GetApi<IDtmConfigMenuApi>(menuProvider.UniqueID) ?? throw new InvalidOperationException("Owner-bound config facade missing.");
            Assert(ReferenceEquals(facade, consumerRegistry.GetApi<IDtmConfigMenuApi>(menuProvider.UniqueID)), "Owner-bound API facade should be stable for a consumer/provider/contract tuple.");
            Assert(facade is IDtmConfigMenuKeybindDefaultsApi, "Owner-bound primary config facade should retain optional keybind-default capability.");
            Assert(facade is IOwnerBoundApiFacade, "Owner-bound config facade should support explicit cache deactivation so stale third-party references can't retain its provider and owner graph.");
            AssertThrows(() => facade.Register(spoof, () => { }, () => { }), "Config facade must reject a forged owner manifest.");
            facade.Register(consumer, () => { }, () => { });
            AssertThrows(() => facade.Register(consumer, () => { }, () => { }), "Config pages must reject duplicate registration.");
            registry.RemoveOwner(menuProvider.UniqueID);
            AssertThrows(() => facade.AddParagraph(consumer, () => "stale"), "A stale facade must reject mutations after its provider is inactive.");
            consumerActive = false;
            registry.RemoveOwner(consumer.UniqueID);
            AssertThrows(() => consumerRegistry.IsLoaded(provider.UniqueID), "A stale Registry helper must reject IsLoaded queries after consumer deactivation.");
            AssertThrows(() => consumerRegistry.Get(provider.UniqueID), "A stale Registry helper must reject manifest queries after consumer deactivation.");
            AssertThrows(() => consumerRegistry.GetAll(), "A stale Registry helper must reject registry snapshots after consumer deactivation.");

            string? previousRoot = UseTempPersistentRoot();
            try
            {
                var config = new ConfigService(new RuntimePaths(NewTempGameDir(), NewTempGameDir()), new DiagnosticsService(new RuntimePaths(NewTempGameDir(), NewTempGameDir())));
                bool active = true;
                IConfigHelper bound = config.CreateOwnerBound(consumer, () => { if (!active) throw new InvalidOperationException("inactive"); });
                AssertThrows(() => bound.GetConfigPath(spoof), "Owner-bound config helper must reject another manifest.");
                bound.RegisterMigration<HotLoadProbeConfig>(consumer, _ => { });
                AssertThrows(() => bound.RegisterMigration<HotLoadProbeConfig>(consumer, _ => { }), "Duplicate owner/config migration registration must throw.");
                Assert(config.RemoveOwner(consumer.UniqueID) == 1 && config.CountOwner(consumer.UniqueID) == 0, "Config migration owner cleanup must leave zero roots.");
                active = false;
                AssertThrows(() => bound.GetConfigPath(consumer), "Stale config helper must reject access after owner deactivation.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void ConfigMenuKeybindDefaultResetRestoresDeclaredValue()
        {
            var menu = new ConfigMenuRegistry();
            Assert(menu is IDtmConfigMenuKeybindDefaultsApi, "The built-in config menu should expose the optional keybind-default capability.");
            Assert(typeof(IDtmConfigMenuApi).GetMethods().Count(method => method.Name == "AddKeybindOption") == 1, "The existing config-menu consumer interface should not gain a new abstract keybind overload.");
            IConfigMenuRuntime runtime = menu;
            var owner = new ManifestModel { Name = "Keybind Defaults", Author = "DTMAPI", Version = "1.0.0", UniqueID = "DTMAPI.Tests.KeybindDefaults", Type = "CodeMod" };
            string value = "F7";
            menu.Register(owner, () => value = "F6", () => { });
            menu.AddKeybindOption(owner, () => "Toggle", () => "Toggle key", () => value, next => value = next, () => "F6");
            runtime.BeginEditing(owner.UniqueID);
            IConfigMenuItem item = runtime.GetPage(owner.UniqueID)!.Items.Single(entry => entry.Kind == "Keybind");
            Assert(item.TrySetPendingValue("F8", out _), "Keybind default-reset test should accept a pending replacement.");
            var resettable = (IResettableKeybindConfigMenuItem)item;
            Assert(resettable.HasDefaultValue && resettable.DefaultValue == "F6", "Keybind options with an explicit default should expose the normalized reset value.");
            Assert(resettable.TryResetPendingValue(out _) && item.PendingValue == "F6", "Per-item keybind reset should restore the declared default, not clear the binding or reuse the pre-capture key.");

            var failingMenu = new ConfigMenuRegistry();
            IConfigMenuRuntime failingRuntime = failingMenu;
            failingMenu.Register(owner, () => { }, () => { });
            failingMenu.AddKeybindOption(owner, () => "Broken default", () => string.Empty, () => value, next => value = next, () => throw new InvalidOperationException("default failed"));
            failingRuntime.BeginEditing(owner.UniqueID);
            var failingReset = (IResettableKeybindConfigMenuItem)failingRuntime.GetPage(owner.UniqueID)!.Items.Single();
            Assert(!failingReset.TryResetPendingValue(out string resetError) && resetError.Contains("default failed", StringComparison.Ordinal), "A failing default getter should report validation failure instead of silently resetting the keybind to None.");
        }

        private static void ConfigMenuOwnerCleanupReleasesCallbacksHeldByStalePage()
        {
            var menu = new ConfigMenuRegistry();
            IConfigMenuRuntime runtime = menu;
            var owner = new ManifestModel
            {
                Name = "Config Callback Lifetime",
                Author = "DTMAPI",
                Version = "1.0.0",
                UniqueID = "DTMAPI.Tests.ConfigCallbackLifetime",
                Type = "CodeMod"
            };
            WeakReference captured = RegisterCapturedConfigPage(menu, runtime, owner, out IConfigMenuPage stalePage, out IConfigMenuItem staleItem);

            Assert(runtime.RemoveOwner(owner.UniqueID) == 1, "Config page owner cleanup should remove the registered page.");
            for (int i = 0; i < 8 && captured.IsAlive; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }

            GC.KeepAlive(stalePage);
            GC.KeepAlive(staleItem);
            Assert(!captured.IsAlive, "Config page cleanup must sever reset/save/item delegates even when external code retains stale page and item objects.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference RegisterCapturedConfigPage(
            ConfigMenuRegistry menu,
            IConfigMenuRuntime runtime,
            IManifest owner,
            out IConfigMenuPage stalePage,
            out IConfigMenuItem staleItem)
        {
            var payload = new WeakPayload();
            var weak = new WeakReference(payload);
            menu.Register(owner, () => payload.Buffer[0] = 1, () => payload.Buffer[0] = 2);
            menu.AddParagraph(owner, () => payload.Buffer.Length.ToString(CultureInfo.InvariantCulture));
            stalePage = runtime.GetPage(owner.UniqueID) ?? throw new InvalidOperationException("Config callback lifetime page should be registered.");
            staleItem = stalePage.Items.Single();
            return weak;
        }

        private static void ConfigPreviewAuditRecordsApplyAndRestore()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                var menu = new ConfigMenuRegistry();
                IConfigMenuRuntime menuRuntime = menu;
                var runtime = new DtmApiRuntime(new FakeHost(dir), menu);
                IManifest manifest = new ManifestModel
                {
                    Name = "Preview Audit",
                    Author = "DTMAPI",
                    Version = "1.0.0",
                    UniqueID = "DTMAPI.Tests.PreviewAudit"
                };
                bool value = false;
                menu.Register(manifest, () => { }, () => { });
                menu.AddBoolOption(manifest, () => "Preview", () => "", () => value, next => value = next);
                IConfigMenuPage page = menuRuntime.GetPage(manifest.UniqueID) ?? throw new InvalidOperationException("Preview audit page should exist.");
                menuRuntime.BeginEditing(manifest.UniqueID);
                Assert(page.Items[0].TrySetPendingValue("true", out _), "Preview audit bool option should accept pending true.");

                for (int i = 0; i < 200; i++)
                {
                    using (menuRuntime.PreviewPendingValues(page))
                    {
                        Assert(value, "Preview should still apply the pending value while auditing.");
                    }
                    Assert(!value, "Preview scope should still restore the original value.");
                }

                ModOwnerLedgerSnapshot snapshot = runtime.ModOwnerLedgerSnapshot;
                Assert(snapshot.ConfigPreviewObservedCount == 400, "Config preview audit should count apply and restore observations.");
                Assert(snapshot.ConfigPreviewAggregates.Count == 2, "Repeated successful previews should collapse into stable apply/restore aggregate records.");
                Assert(snapshot.Entries.All(e => !e.Kind.Equals("ConfigPreview", StringComparison.OrdinalIgnoreCase)), "Successful config previews should not grow the retained owner-entry list.");
                Assert(snapshot.ConfigPreviewWarningCount == 0 && snapshot.RecentConfigPreviewFailureCount == 0, "Successful preview aggregation should not retain failure samples.");
                Assert(runtime.Diagnostics.GetFeatureStatuses().Any(f => f.FeatureId == "Refactor.ConfigPreviewAudit" && f.Status == "observing"), "Config preview audit should publish an observing feature status.");
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

        private static void ConfigJsonEscapesRoundTripWithoutRewriting()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                var paths = new RuntimePaths(NewTempGameDir(), NewTempGameDir());
                var config = new ConfigService(paths, new DiagnosticsService(paths));
                var manifest = new ManifestModel { UniqueID = "Stranger.Config.Escapes" };
                var values = Enumerable.Range(0, 7).SelectMany(n => new[]
                {
                    new string('\\', n), new string('\\', n) + "\"",
                    new string('\\', n) + "\",:[]{}中文\n\t😀"
                }).ToArray();
                config.WriteConfig(manifest, values);
                using var parsed = JsonDocument.Parse(File.ReadAllText(config.GetConfigPath(manifest)));
                Assert(parsed.RootElement.EnumerateArray().Select(v => v.GetString()).SequenceEqual(values), "Independent JSON parser must preserve every escaped value.");
                Assert(DTMAPI.Core.Json.JsonFile.Read<string[]>(config.GetConfigPath(manifest)).SequenceEqual(values), "Runtime parser must preserve values too.");
            }
            finally { RestorePersistentRoot(previousRoot); }
        }

        private static void ConfigRecoveryFailuresPreserveOriginalBytes()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                var paths = new RuntimePaths(NewTempGameDir(), NewTempGameDir());
                var diagnostics = new DiagnosticsService(paths);
                var files = new ConfigFileOperations();
                var config = new ConfigService(paths, diagnostics, files);
                var manifest = new ManifestModel { UniqueID = "Stranger.Config.Faults" };
                string path = config.GetConfigPath(manifest);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                byte[] original = Encoding.UTF8.GetBytes("\uFEFF{ broken: 中文");
                File.WriteAllBytes(path, original);
                string[] Backups() => Directory.GetFiles(Path.GetDirectoryName(path)!, "*.bak");
                void Preserved() => Assert(File.ReadAllBytes(path).SequenceEqual(original), "Failed recovery must retain original bytes.");

                foreach (Exception failure in new Exception[] { new UnauthorizedAccessException("read denied"), new IOException("sharing violation") })
                {
                    files.ReadAllText = _ => throw failure;
                    AssertThrows<Exception>(() => config.ReadConfig<SampleConfig>(manifest), "Read failures must propagate.");
                    Preserved();
                    Assert(Backups().Length == 0, "IO failures are not corrupt JSON recovery.");
                }
                files.ReadAllText = p => File.ReadAllText(p, new UTF8Encoding(false, true));
                files.Copy = (_, _) => throw new UnauthorizedAccessException("backup denied");
                AssertThrows<Exception>(() => config.ReadConfig<SampleConfig>(manifest), "Backup failure must prevent default writes.");
                Preserved();
                // Primary remains writable even when the backup operation fails.
                File.WriteAllBytes(path, original);
                string collision = "";
                byte[] existing = Encoding.UTF8.GetBytes("previous recovery bytes");
                files.Copy = (source, target) => { collision = target; File.WriteAllBytes(target, existing); File.Copy(source, target, false); };
                AssertThrows<Exception>(() => config.ReadConfig<SampleConfig>(manifest), "A collided backup name must fail without overwriting either file.");
                Preserved();
                Assert(File.ReadAllBytes(collision).SequenceEqual(existing), "Collision must retain old backup.");

                files.Copy = (source, target) => File.Copy(source, target, false);
                files.Replace = (_, _) => throw new IOException("atomic replace denied");
                AssertThrows<Exception>(() => config.ReadConfig<SampleConfig>(manifest), "Default write failure must propagate.");
                Preserved();
                Assert(Backups().Any(p => File.ReadAllBytes(p).SequenceEqual(original)), "Default write failure must retain an exact backup.");
                Assert(Directory.GetFiles(Path.GetDirectoryName(path)!, "*.tmp").Length == 0, "Failed candidate write must remove temporary files.");
                files.Replace = ConfigService.ReplaceWithTempFile;
                Assert(config.ReadConfig<SampleConfig>(manifest).Count == 7, "Recovery after transient fault must succeed.");
                File.WriteAllBytes(path, original);
                config.ReadConfig<SampleConfig>(manifest);
                Assert(Backups().Length == 4, "Repeated recovery must produce unique backups and preserve collision bytes.");
            }
            finally { RestorePersistentRoot(previousRoot); }
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
            Assert(page.Items[4].TrySetPendingValue("Plus", out _), "Keybind option should accept Plus aliases.");
            Assert(page.Items[5].TrySetPendingValue("Equals", out _), "Keybind option should accept Equals aliases.");
            string aliasConflict = menu.GetKeybindConflicts(manifest.UniqueID).Single();
            Assert(aliasConflict.StartsWith("按键冲突：Equals", StringComparison.Ordinal), "Plus/Equals aliases should conflict by canonical physical key.");

            Assert(page.Items[4].TrySetPendingValue("LeftControl+F6", out _), "Keybind option should accept explicit left Control chords.");
            Assert(page.Items[5].TrySetPendingValue("RightControl+F6", out _), "Keybind option should accept explicit right Control chords.");
            Assert(menu.GetKeybindConflicts(manifest.UniqueID).Count == 0, "Left and right physical modifiers should not conflict with each other.");

            Assert(page.Items[4].TrySetPendingValue("Ctrl+F6", out _), "Keybind option should accept generic Control chords.");
            Assert(page.Items[5].TrySetPendingValue("LeftControl+F6", out _), "Keybind option should accept side-specific Control chords.");
            string genericConflict = menu.GetKeybindConflicts(manifest.UniqueID).Single();
            Assert(genericConflict.StartsWith("按键冲突：LeftControl+F6", StringComparison.Ordinal), "Generic Control chords should conflict with either physical side.");

            Assert(page.Items[4].TrySetPendingValue("F9", out _), "Keybind option should accept captured keys.");
            Assert(page.Items[5].TrySetPendingValue("F9", out _), "Keybind option should accept repeated captured keys.");
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

        private static void ConfigMenuCallbackFailuresRollbackAndStayInspectable()
        {
            var menu = new ConfigMenuRegistry();
            IConfigMenuRuntime menuRuntime = menu;
            IManifest manifest = new ManifestModel
            {
                Name = "Menu Failure Test",
                Author = "DTMAPI",
                Version = "1.0.0",
                UniqueID = "DTMAPI.Tests.MenuFailure"
            };

            bool first = false;
            bool second = false;
            bool failSecondSetter = true;
            bool failSave = false;
            int saveCalls = 0;
            menu.Register(manifest, () => { }, () =>
            {
                saveCalls++;
                if (failSave)
                    throw new InvalidOperationException("save-boom");
            });
            menu.AddBoolOption(manifest, () => "First", () => "", () => first, value => first = value);
            menu.AddBoolOption(manifest, () => "Second", () => "", () => second, value =>
            {
                if (failSecondSetter && value)
                    throw new InvalidOperationException("setter-boom");
                second = value;
            });

            IConfigMenuPage page = menuRuntime.GetPage(manifest.UniqueID) ?? throw new InvalidOperationException("Page should exist.");
            menuRuntime.BeginEditing(manifest.UniqueID);
            Assert(page.Items[0].TrySetPendingValue("true", out _), "First option should accept true.");
            Assert(page.Items[1].TrySetPendingValue("true", out _), "Second option should accept true.");
            AssertThrows(() => menuRuntime.Save(manifest.UniqueID), "Setter failure should reject save.");
            Assert(!first && !second && saveCalls == 0, "Setter failure should roll back prior applied values and skip save callback.");
            Assert(page.Items[1].ValidationError.Contains("setter-boom"), "Failing setter should remain visible on the config item.");
            Assert(page.HasPendingChanges, "Failed save should keep pending edits inspectable.");

            failSecondSetter = false;
            failSave = true;
            AssertThrows(() => menuRuntime.Save(manifest.UniqueID), "Save callback failure should reject save.");
            Assert(!first && !second && saveCalls == 1, "Save callback failure should roll back applied pending values.");
            Assert(page.HasPendingChanges, "Failed save callback should keep pending edits inspectable.");

            failSave = false;
            menuRuntime.Save(manifest.UniqueID);
            Assert(first && second && saveCalls == 2 && !page.HasPendingChanges, "Successful retry should apply and commit retained pending values.");

            var previewMenu = new ConfigMenuRegistry();
            IConfigMenuRuntime previewRuntime = previewMenu;
            IManifest previewManifest = new ManifestModel
            {
                Name = "Preview Failure Test",
                Author = "DTMAPI",
                Version = "1.0.0",
                UniqueID = "DTMAPI.Tests.PreviewFailure"
            };
            bool previewEnabled = false;
            previewMenu.Register(previewManifest, () => { }, () => { });
            previewMenu.AddBoolOption(previewManifest, () => "Preview", () => "", () => previewEnabled, value =>
            {
                if (value)
                    throw new InvalidOperationException("preview-boom");
                previewEnabled = value;
            });

            IConfigMenuPage previewPage = previewRuntime.GetPage(previewManifest.UniqueID) ?? throw new InvalidOperationException("Preview page should exist.");
            previewRuntime.BeginEditing(previewManifest.UniqueID);
            Assert(previewPage.Items[0].TrySetPendingValue("true", out _), "Preview option should accept pending true.");
            using (previewRuntime.PreviewPendingValues(previewPage) ?? throw new InvalidOperationException("Preview scope should be returned even after callback failure."))
            {
                Assert(!previewEnabled, "Failed preview setter should be rolled back immediately.");
            }
            Assert(!previewEnabled, "Preview dispose should leave the original value intact.");
            Assert(previewPage.Items[0].ValidationError.Contains("preview-boom"), "Preview failure should be visible on the config item.");

            var resetMenu = new ConfigMenuRegistry();
            IConfigMenuRuntime resetRuntime = resetMenu;
            IManifest resetManifest = new ManifestModel
            {
                Name = "Reset Failure Test",
                Author = "DTMAPI",
                Version = "1.0.0",
                UniqueID = "DTMAPI.Tests.ResetFailure"
            };
            bool resetValue = true;
            bool failReset = true;
            resetMenu.Register(resetManifest, () =>
            {
                if (failReset)
                    throw new InvalidOperationException("reset-boom");
                resetValue = false;
            }, () => { });
            resetMenu.AddBoolOption(resetManifest, () => "Reset", () => "", () => resetValue, value => resetValue = value);
            IConfigMenuPage resetPage = resetRuntime.GetPage(resetManifest.UniqueID) ?? throw new InvalidOperationException("Reset page should exist.");
            resetRuntime.BeginEditing(resetManifest.UniqueID);
            AssertThrows(() => resetRuntime.Reset(resetManifest.UniqueID), "Reset callback failure should be reported.");
            Assert(resetValue && resetPage.IsEditing && !resetPage.HasPendingChanges, "Reset callback failure should keep the committed value and editing state.");

            var resetGetterMenu = new ConfigMenuRegistry();
            IConfigMenuRuntime resetGetterRuntime = resetGetterMenu;
            IManifest resetGetterManifest = new ManifestModel
            {
                Name = "Reset Getter Failure Test",
                Author = "DTMAPI",
                Version = "1.0.0",
                UniqueID = "DTMAPI.Tests.ResetGetterFailure"
            };
            bool resetFirst = true;
            bool resetSecond = true;
            bool failResetGetter = false;
            resetGetterMenu.Register(resetGetterManifest, () =>
            {
                resetFirst = false;
                resetSecond = false;
                failResetGetter = true;
            }, () => { });
            resetGetterMenu.AddBoolOption(resetGetterManifest, () => "First", () => "", () => resetFirst, value => resetFirst = value);
            resetGetterMenu.AddBoolOption(resetGetterManifest, () => "Second", () => "", () =>
            {
                if (failResetGetter)
                    throw new InvalidOperationException("reset-getter-boom");
                return resetSecond;
            }, value => resetSecond = value);
            IConfigMenuPage resetGetterPage = resetGetterRuntime.GetPage(resetGetterManifest.UniqueID) ?? throw new InvalidOperationException("Reset getter page should exist.");
            resetGetterRuntime.BeginEditing(resetGetterManifest.UniqueID);
            AssertThrows(() => resetGetterRuntime.Reset(resetGetterManifest.UniqueID), "Reset getter failure should be reported.");
            failResetGetter = false;
            Assert(resetFirst && resetSecond, "Reset getter failure should roll back true config values.");
            Assert(resetGetterPage.IsEditing && !resetGetterPage.HasPendingChanges, "Reset getter failure should not leave partially refreshed pending values.");

            var cancelMenu = new ConfigMenuRegistry();
            IConfigMenuRuntime cancelRuntime = cancelMenu;
            IManifest cancelManifest = new ManifestModel
            {
                Name = "Cancel Failure Test",
                Author = "DTMAPI",
                Version = "1.0.0",
                UniqueID = "DTMAPI.Tests.CancelFailure"
            };
            bool cancelValue = false;
            bool failCancelRestore = false;
            cancelMenu.Register(cancelManifest, () => { }, () => { });
            cancelMenu.AddBoolOption(cancelManifest, () => "Cancel", () => "", () => cancelValue, value =>
            {
                if (failCancelRestore && !value)
                    throw new InvalidOperationException("cancel-boom");
                cancelValue = value;
            });
            IConfigMenuPage cancelPage = cancelRuntime.GetPage(cancelManifest.UniqueID) ?? throw new InvalidOperationException("Cancel page should exist.");
            cancelRuntime.BeginEditing(cancelManifest.UniqueID);
            Assert(cancelPage.Items[0].TrySetPendingValue("true", out _), "Cancel option should accept pending true.");
            cancelValue = true;
            failCancelRestore = true;
            AssertThrows(() => cancelRuntime.Cancel(cancelManifest.UniqueID), "Cancel restore failure should be reported.");
            Assert(cancelValue && cancelPage.IsEditing && cancelPage.HasPendingChanges, "Cancel restore failure should keep the page editing so the failed pending state remains visible.");
            Assert(cancelPage.Items[0].ValidationError.Contains("cancel-boom"), "Cancel restore failure should stay visible on the item.");

            var buttonMenu = new ConfigMenuRegistry();
            IConfigMenuRuntime buttonRuntime = buttonMenu;
            IManifest buttonManifest = new ManifestModel
            {
                Name = "Button Failure Test",
                Author = "DTMAPI",
                Version = "1.0.0",
                UniqueID = "DTMAPI.Tests.ButtonFailure"
            };
            buttonMenu.Register(buttonManifest, () => { }, () => { });
            buttonMenu.AddButton(buttonManifest, () => "Danger", () => "", () => throw new InvalidOperationException("button-boom"));
            IConfigMenuPage buttonPage = buttonRuntime.GetPage(buttonManifest.UniqueID) ?? throw new InvalidOperationException("Button page should exist.");
            AssertThrows(buttonPage.Items.Single(item => item.Kind == "Button").Invoke, "Button callback failure should be wrapped for the UI action guard.");
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

        private static void ConfigPreviewAuditBoundsRecentFailures()
        {
            var ledger = new ModOwnerLedgerService();
            for (int i = 0; i < 200; i++)
                ledger.RecordConfigPreview("DTMAPI.Tests.PreviewFailure", "scope", "ChangedItems", "restore", false, "failure=" + i.ToString(CultureInfo.InvariantCulture));

            ModOwnerLedgerSnapshot snapshot = ledger.GetSnapshot();
            Assert(snapshot.ConfigPreviewObservedCount == 200 && snapshot.ConfigPreviewWarningCount == 200, "Config preview aggregate counters should preserve the full failure history.");
            Assert(snapshot.ConfigPreviewAggregates.Count == 1, "Repeated failures for one preview scope should use one stable aggregate record.");
            Assert(snapshot.RecentConfigPreviewFailureCount == 64 && snapshot.Entries.Count == 64, "Config preview diagnostics should retain only the bounded 64 most recent failure samples.");
            Assert(snapshot.ConfigPreviewAggregates.All(aggregate => aggregate.LastDetails.Length == 0), "Config preview aggregate rows must retain counts only and keep LastDetails permanently empty.");
            Assert(snapshot.Entries.All(entry => entry.Kind == "ConfigPreview" && entry.Status == "Warning" && entry.RollbackResult.StartsWith("failure=", StringComparison.Ordinal)), "Arbitrary preview failure details should exist only in the bounded recent failure window.");
            Assert(snapshot.Entries.First().RollbackResult == "failure=136" && snapshot.Entries.Last().RollbackResult == "failure=199", "The bounded preview failure window should retain exactly the latest 64 scalar details.");

            var uniqueLedger = new ModOwnerLedgerService();
            for (int i = 0; i < 400; i++)
            {
                uniqueLedger.RecordConfigPreview(
                    "DTMAPI.Tests.PreviewUnique." + i.ToString(CultureInfo.InvariantCulture),
                    "scope-" + i.ToString(CultureInfo.InvariantCulture),
                    "Kind-" + i.ToString(CultureInfo.InvariantCulture),
                    "Operation-" + i.ToString(CultureInfo.InvariantCulture),
                    true,
                    "scalar");
            }

            ModOwnerLedgerSnapshot uniqueSnapshot = uniqueLedger.GetSnapshot();
            ModOwnerConfigPreviewAggregate overflow = uniqueSnapshot.ConfigPreviewAggregates.Single(aggregate =>
                aggregate.Kind == "other" && aggregate.Operation == "other");
            Assert(uniqueSnapshot.ConfigPreviewAggregates.Count == 256, "Unique config preview operation/kind pairs must retain at most 256 aggregate records including overflow.");
            Assert(overflow.ObservationCount == 145, "Config preview aggregate overflow should count every observation beyond the 255 retained specific keys.");
            Assert(uniqueSnapshot.ConfigPreviewAggregates.All(aggregate => aggregate.LastDetails.Length == 0), "Successful and overflow preview aggregates must never retain arbitrary LastDetails.");
            Assert(uniqueSnapshot.Entries.Count == 0 && uniqueSnapshot.RecentConfigPreviewFailureCount == 0, "Successful preview details should not enter the recent failure window.");
        }
    }
}
