using System;
using System.Linq;
using System.Reflection;
using DTMAPI.Abstractions;
using DTMAPI.BepInExBootstrap;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Manager;
using DTMAPI.Core.Runtime;
using DTMAPI.ModConfigMenu;

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {
        private static void TitleSettingsRestartNoticeFollowsSuccessfulSwitchChanges()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                var registry = new ConfigMenuRegistry();
                var runtime = new DtmApiRuntime(new FakeHost(NewTempGameDir()), registry);
                runtime.Paths.Ensure();
                var ui = new ReflectedTitleMenuSettingsUi(runtime, registry);
                ui.ReadStartupBgmAppend();
                typeof(ReflectedTitleMenuSettingsUi).GetMethod("InitializeInputSettings", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(ui, null);
                var pages = (IConfigMenuRuntime)registry;
                IConfigMenuPage page = pages.GetPage("DTMAPI.TitleSettings")!;
                pages.BeginEditing(page.Manifest.UniqueID);
                runtime.UI.OpenConfigPage(page.Manifest.UniqueID);
                int notices = 0;
                runtime.ConfigDialogs.Show = _ => { notices++; return true; };
                void Save() => typeof(ReflectedTitleMenuSettingsUi).GetMethod("SaveConfigPage", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(ui, new object[] { page });
                void Close() => typeof(ReflectedTitleMenuSettingsUi).GetMethod("CloseMenu", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(ui, null);
                IConfigMenuItem bgm = page.Items.Last();
                bgm.TrySetPendingValue("false", out _); pages.Cancel(page.Manifest.UniqueID);
                Close();
                Assert(notices == 0, "Cancel does not publish a restart notice on close.");
                runtime.UI.OpenConfigPage(page.Manifest.UniqueID);
                page.Items[0].TrySetPendingValue("F9", out _); Save();
                Assert(notices == 0 && !pages.HasUnnotifiedRestartChanges(), "Saving a hotkey alone does not request restart.");
                bgm.TrySetPendingValue("false", out _); Save();
                Assert(notices == 0 && pages.HasUnnotifiedRestartChanges() && !page.HasPendingChanges, "Successful save records the switch without opening a dialog.");
                Close();
                Assert(notices == 1 && runtime.ConfigDialogs.Current != null, "Closing the menu shows one combined reminder.");
                runtime.ConfigDialogs.Current!.Select("ok"); Save();
                Assert(notices == 1 && !runtime.UI.IsOpen && !pages.HasUnnotifiedRestartChanges(), "Acknowledging closes the menu; unchanged saves do not repeat the notice.");
                bgm.TrySetPendingValue("true", out _); Save();
                Assert(notices == 1 && pages.GetPendingRestartChanges().Count == 0, "Restoring the startup value removes the need to restart.");
                var conflict = new ManifestModel { UniqueID = "Tests.HotkeyConflict", Version = "1.0.0" };
                registry.Register(conflict, () => { }, () => { });
                registry.AddKeybindOption(conflict, () => "Conflict", () => "", () => "F9", _ => { });
                bgm.TrySetPendingValue("false", out _); Save();
                Assert(notices == 1 && page.HasPendingChanges, "Rejected save retains its draft without a misleading restart notice.");
                pages.RemoveOwner(conflict.UniqueID);
                ui.Shutdown("restart notice test complete");
            }
            finally { RestorePersistentRoot(previousRoot); }
        }

        private static void TitleSettingsPresentationPreservesOrderAndDrafts()
        {
            var registry = new ConfigMenuRegistry();
            var runtime = new DtmApiRuntime(new FakeHost(NewTempGameDir()), registry);
            runtime.Paths.Ensure();
            foreach (string id in new[] { "Z.Mod", "A.Mod", "B.Mod" })
                registry.Register(new ManifestModel { UniqueID = id, Version = "1.0.0" }, () => { }, () => { });
            var ui = new ReflectedTitleMenuSettingsUi(runtime, registry);
            typeof(ReflectedTitleMenuSettingsUi).GetMethod("InitializeInputSettings", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(ui, null);
            var pages = (IConfigMenuRuntime)registry;
            IConfigMenuPage[] ordered = ReflectedTitleMenuSettingsUi.OrderConfigPages(pages.GetPages());
            Assert(ordered.Select(page => page.Manifest.UniqueID).SequenceEqual(new[] { "DTMAPI.TitleSettings", "A.Mod", "B.Mod", "Z.Mod" }),
                "Pinning the runtime page preserves the existing ID order of every product page.");
            runtime.UI.OpenConfigPage("B.Mod");
            ReflectedTitleMenuSettingsUi.OrderConfigPages(pages.GetPages());
            Assert(runtime.UI.RequestedConfigUniqueId == "B.Mod", "Display ordering must not replace an explicit product request.");
            IConfigMenuPage core = ordered[0];
            Assert(core.Items.Count == 3, "Runtime settings expose one keybind and two actual switches, without paragraph rows.");
            pages.BeginEditing(core.Manifest.UniqueID);
            IConfigMenuItem bgm = core.Items.Last();
            Assert(bgm.TrySetPendingValue("false", out _) && core.HasPendingChanges, "The BGM switch can be edited in the draft.");
            pages.Cancel(core.Manifest.UniqueID);
            Assert(!core.HasPendingChanges && ui.ReadStartupBgmAppend(), "Cancel restores the BGM choice without persisting it.");
            pages.BeginEditing(core.Manifest.UniqueID);
            Assert(bgm.TrySetPendingValue("false", out _), "A new edit accepts the BGM choice.");
            pages.Save(core.Manifest.UniqueID);
            Assert(!core.HasPendingChanges && !ui.ReadStartupBgmAppend(), "Save persists the actual BGM switch after presentation changes.");
            ui.Shutdown("presentation test complete");
        }

        private static void TitleSettingsDetailsRetainLongDiagnosticText()
        {
            var registry = new ContentManifestRegistry().Build(Array.Empty<DiscoveredMod>(), Array.Empty<DiscoveredMod>(),
                new[] { "one retained error" }, Array.Empty<string>(), new ModScannerDiagnosticTotals(700, 0, 0, 1, 0), "presentation count");
            var diagnostics = ManagerAdvancedDiagnostics.From(registry, Array.Empty<ManagerModRow>(), Array.Empty<ManagerHookRow>(), Array.Empty<ManagerFeatureRow>());
            Assert(diagnostics.ScanFailureCount == 700 && diagnostics.Rows.Count < 700,
                "The overview reports total scan failures even when registry evidence is sampled.");
            string original = "Manifest discovery failed: C:\\very-long-directory\\" + new string('W', 220) +
                "\\MODS\\ExampleMod\\Content\\DTMAPI\\manifest.json: advanced-package-marker-invalid: final actionable reason 中文😀e\u0301";
            string compact = ReflectedTitleMenuSettingsUi.CompactDiagnosticMessage(original);
            Assert(compact.StartsWith("ExampleMod — advanced-package-marker-invalid:", StringComparison.Ordinal) && compact.EndsWith("中文😀e\u0301", StringComparison.Ordinal),
                "The list identifies the failed Mod and reason before the long path; unrecognized messages remain intact.");
            Assert(ReflectedTitleMenuSettingsUi.CompactDiagnosticMessage(original.Substring("Manifest discovery failed: ".Length)) == compact,
                "Scanner Details and log-prefixed messages identify the same failed package.");
            string longBody = original + new string('中', 1300) + "END-OF-ERROR";
            var lines = ReflectedTitleMenuSettingsUi.WrapDetailText(longBody);
            Assert(lines.Count > 16 && string.Concat(lines) == longBody, "All detail pages together retain the entire error, including its tail.");
            Assert(lines.All(line => line.Length == 0 || !char.IsLowSurrogate(line[0]) && !char.IsHighSurrogate(line[line.Length - 1])),
                "Pagination must not split a Unicode surrogate pair.");
            Assert(ReflectedTitleMenuSettingsUi.CompactDiagnosticMessage("arbitrary author error") == "arbitrary author error",
                "Unknown diagnostic formats must not be reinterpreted or discarded.");
        }
    }
}
