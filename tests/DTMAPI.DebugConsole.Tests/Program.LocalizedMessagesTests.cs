using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using DTMAPI.Abstractions;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;
using DTMAPI.DebugConsole;
using DTMAPI.ModConfigMenu;

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {
        private static void DebugConsoleUiMessagesStayLocalized()
        {
            var values = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(
                Path.Combine(FindRepositoryRoot(), "mods/DebugConsole/i18n/schinese.json")))!;
            var runtime = new DtmApiRuntime(new FakeHost(NewTempGameDir()), new ConfigMenuRegistry());
            var ui = new DebugConsoleUi(new DebugConsoleTestRuntime(runtime));
            SetPrivateField(ui, "text", new DebugConsoleText(new BrowserTranslation(values)));
            SetPrivateField(ui, "ownerManifest", new ManifestModel { UniqueID = "LocalizedBrowser", Name = "Browser" });
            SetPrivateField(ui, "advancedApi", new FakeDebugConsoleAdvancedActions());
            MethodInfo toggle = typeof(DebugConsoleUi).GetMethod("ToggleGodMode", BindingFlags.Instance | BindingFlags.NonPublic)!;
            toggle.Invoke(ui, null);
            Assert(GetPrivateField<string>(ui, "statusMessage") == values["debug.god.on"], "Enabling god mode must show the active locale, not the native action's English message.");
            toggle.Invoke(ui, null);
            Assert(GetPrivateField<string>(ui, "statusMessage") == values["debug.god.off"], "Disabling god mode must also stay localized.");
            MethodInfo spawn = typeof(DebugConsoleUi).GetMethod("FormatSpawnFailure", BindingFlags.Instance | BindingFlags.NonPublic)!;
            string reason = (string)spawn.Invoke(ui, new object[] { new SpawnDebugResult { FailureReason = "animal-postcondition-failed" }, "Animal postcondition verification failed.", true })!;
            Assert(reason == values["debug.animal.incomplete"], "An incomplete animal must show a readable localized reason and keep internal diagnostic codes out of the status line.");
        }

        private sealed class BrowserTranslation : ITranslationHelper
        {
            private readonly IReadOnlyDictionary<string, string> values;
            internal BrowserTranslation(IReadOnlyDictionary<string, string> values) => this.values = values;
            public string Language => "schinese";
            public string Get(string key, string fallback = "") => values.TryGetValue(key, out string value) ? value : fallback;
        }
    }
}
