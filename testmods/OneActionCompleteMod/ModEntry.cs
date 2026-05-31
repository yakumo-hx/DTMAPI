using System;
using System.Runtime.Serialization;
using DTMAPI.Abstractions;

namespace OneActionCompleteMod
{
    public sealed class ModEntry : DtmMod
    {
        private IDtmHelper helper = null!;
        private OneActionConfig config = new OneActionConfig();
        private string registeredMenuKey = string.Empty;
        private bool updateEvidenceLogged;

        public override void Entry(IDtmHelper helper)
        {
            this.helper = helper;
            config = helper.ReadConfig<OneActionConfig>();
            NormalizeConfig();
            RegisterInputKeys();
            RegisterConfigMenu();
            ConfigureBridge("Entry");

            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.Save.SaveLoaded += (_, e) => helper.Monitor.Log("OneActionComplete SaveLoaded restore boundary OK slot=" + (e.SaveSlot?.ToString() ?? "unknown"));
            helper.Monitor.Log("OneActionComplete migrated to DTMAPI shell. Defaults are off; title DTMAPI Settings is the player-facing config entry.");
        }

        private void RegisterConfigMenu()
        {
            IDtmConfigMenuApi? menu = helper.ModRegistry.GetApi<IDtmConfigMenuApi>("DTMAPI.ModConfigMenu");
            if (menu == null)
            {
                helper.Monitor.Log("DTMAPI config menu API is not available.", LogLevel.Warn);
                return;
            }

            menu.Register(helper.ModManifest, ResetConfig, () =>
            {
                NormalizeConfig();
                helper.WriteConfig(config);
                RegisterInputKeys();
                ConfigureBridge("config saved");
                helper.Monitor.Log("OneActionComplete config saved through DTMAPI menu.");
            });

            menu.SetDisplayName(helper.ModManifest, () => T("mod.name", helper.ModManifest.Name));
            menu.AddSectionTitle(helper.ModManifest, () => T("config.section.main", "One action complete"));
            menu.AddParagraph(helper.ModManifest, () => T("config.status.experimental", "Resource/tool-hit completion, the tree/ore/garbage/weeds wrong-tool matrix, and fuel/feeder native consume/fill are verified. Vegetation/dandelion is recorded as a native tool-constraint exception path, not a DungeonResource one-action path."));
            menu.AddBoolOption(helper.ModManifest, () => T("config.enabled.name", "Enabled"), () => T("config.enabled.tooltip", "Master switch for all one-action behavior."), () => config.Enabled, value => config.Enabled = value);
            menu.AddBoolOption(helper.ModManifest, () => T("config.trees.name", "Trees"), () => T("config.trees.tooltip", "Finish trees after one normal tool hit."), () => config.CompleteTrees, value => config.CompleteTrees = value);
            menu.AddBoolOption(helper.ModManifest, () => T("config.ores.name", "Ores"), () => T("config.ores.tooltip", "Finish ore resources after one normal tool hit."), () => config.CompleteOres, value => config.CompleteOres = value);
            menu.AddBoolOption(helper.ModManifest, () => T("config.garbage.name", "Garbage"), () => T("config.garbage.tooltip", "Finish pickaxe garbage resources after one normal tool hit."), () => config.CompleteGarbage, value => config.CompleteGarbage = value);
            menu.AddBoolOption(helper.ModManifest, () => T("config.weeds.name", "Weeds"), () => T("config.weeds.tooltip", "Finish grass and weeds after one normal tool hit."), () => config.CompleteWeeds, value => config.CompleteWeeds = value);
            menu.AddBoolOption(helper.ModManifest, () => T("config.fuel.name", "Fuel machines"), () => T("config.fuel.tooltip", "Fill fuel machines after one direct add interaction."), () => config.CompleteMachineFuel, value => config.CompleteMachineFuel = value);
            menu.AddBoolOption(helper.ModManifest, () => T("config.feeders.name", "Feeders"), () => T("config.feeders.tooltip", "Fill feeders after one direct add interaction."), () => config.CompleteFeeder, value => config.CompleteFeeder = value);
            menu.AddBoolOption(helper.ModManifest, () => T("config.verbose.name", "Verbose logs"), () => T("config.verbose.tooltip", "Write low-frequency migration diagnostics."), () => config.VerboseLogging, value => config.VerboseLogging = value);
            menu.AddKeybindOption(helper.ModManifest, () => T("config.menuKey.name", "Menu key"), () => T("config.menuKey.tooltip", "Opens this DTMAPI config page."), () => config.MenuKey, value => config.MenuKey = value);
        }

        private string T(string key, string fallback) => helper.Translation.Get(key, fallback);

        private void ConfigureBridge(string reason)
        {
            IActionCompletionApi? api = helper.ModRegistry.GetApi<IActionCompletionApi>("DTMAPI.GameBridge.DolocTown");
            if (api == null)
            {
                helper.Monitor.Log("Action completion GameBridge API is not available.", LogLevel.Warn);
                return;
            }

            api.Configure(helper.ModManifest, new ActionCompletionOptions
            {
                Enabled = config.Enabled,
                CompleteTrees = config.CompleteTrees,
                CompleteOres = config.CompleteOres,
                CompleteGarbage = config.CompleteGarbage,
                CompleteWeeds = config.CompleteWeeds,
                CompleteMachineFuel = config.CompleteMachineFuel,
                CompleteFeeder = config.CompleteFeeder,
                VerboseLogging = config.VerboseLogging
            });
            BridgeFeatureStatus status = api.GetStatus(helper.ModManifest.UniqueID);
            helper.Monitor.Log("OneActionComplete bridge policy OK reason=" + reason + " status=" + status.Status);
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (Matches(e.Button, config.MenuKey))
            {
                helper.UI.OpenConfigPage(helper.ModManifest.UniqueID);
                helper.Monitor.Log("OneActionComplete hotkey OpenConfig OK key=" + e.Button);
            }
        }

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!updateEvidenceLogged)
            {
                updateEvidenceLogged = true;
                helper.Monitor.Log("OneActionComplete UpdateTicked OK tick=" + e.Tick);
            }
        }

        private void RegisterInputKeys()
        {
            UnregisterKey(registeredMenuKey);
            registeredMenuKey = NormalizeKey(config.MenuKey);
            helper.Input.RegisterButton(registeredMenuKey);
        }

        private void UnregisterKey(string key)
        {
            if (!string.IsNullOrWhiteSpace(key) && !key.Equals("None", StringComparison.OrdinalIgnoreCase))
                helper.Input.UnregisterButton(key);
        }

        private void ResetConfig()
        {
            config = new OneActionConfig();
            NormalizeConfig();
        }

        private void NormalizeConfig()
        {
            config.MenuKey = NormalizeKey(config.MenuKey);
        }

        private static bool Matches(string actual, string expected)
        {
            expected = NormalizeKey(expected);
            return !expected.Equals("None", StringComparison.OrdinalIgnoreCase) &&
                actual.Equals(expected, StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeKey(string key)
        {
            key = (key ?? string.Empty).Trim();
            return string.IsNullOrWhiteSpace(key) ? "None" : key;
        }

        [DataContract]
        public sealed class OneActionConfig
        {
            [DataMember] public bool Enabled { get; set; }
            [DataMember] public bool CompleteTrees { get; set; }
            [DataMember] public bool CompleteOres { get; set; }
            [DataMember] public bool CompleteGarbage { get; set; }
            [DataMember] public bool CompleteWeeds { get; set; }
            [DataMember] public bool CompleteMachineFuel { get; set; }
            [DataMember] public bool CompleteFeeder { get; set; }
            [DataMember] public bool VerboseLogging { get; set; }
            [DataMember] public string MenuKey { get; set; } = "F11";
        }
    }
}
