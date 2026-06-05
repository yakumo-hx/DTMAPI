using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;
using DTMAPI.Abstractions;

namespace AnimalHusbandryProgressMod
{
    public sealed class ModEntry : DtmMod
    {
        private IDtmHelper helper = null!;
        private AnimalProgressConfig config = new AnimalProgressConfig();
        private bool updateEvidenceLogged;
        private DateTimeOffset lastStatusLog = DateTimeOffset.MinValue;

        public override void Entry(IDtmHelper helper)
        {
            this.helper = helper;
            config = helper.ReadConfig<AnimalProgressConfig>();
            NormalizeConfig();
            RegisterConfigMenu();
            ConfigureBridge("Entry");

            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.Save.SaveLoaded += (_, e) => helper.Monitor.Log("AnimalHusbandryProgress SaveLoaded viewer boundary OK slot=" + (e.SaveSlot?.ToString() ?? "unknown"));
            helper.Monitor.Log("AnimalHusbandryProgress migrated to DTMAPI shell. Viewer policy registered.");
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
                ConfigureBridge("config saved");
                helper.Monitor.Log("AnimalHusbandryProgress config saved through DTMAPI menu.");
            });

            menu.SetDisplayName(helper.ModManifest, () => T("mod.name", helper.ModManifest.Name));
            menu.AddSectionTitle(helper.ModManifest, () => T("config.section.main", "Animal viewer"));
            menu.AddBoolOption(helper.ModManifest, () => T("config.enabled.name", "Enabled"), () => T("config.enabled.tooltip", "Show special-produce progress in the animal bell viewer."), () => config.Enabled, value => config.Enabled = value);
            menu.AddColorPresetOption(helper.ModManifest, () => T("config.colorPreset.name", "Color preset"), () => T("config.colorPreset.tooltip", "Pick the progress bar color directly."), () => config.ColorPreset, value => config.ColorPreset = value, BuildColorPresets());
            Func<bool> customColorSelected = () => config.ColorPreset.Equals("Custom", StringComparison.OrdinalIgnoreCase);
            menu.AddTextOption(helper.ModManifest, () => T("config.fillColor.name", "Fill color"), () => T("config.fillColor.tooltip", "Hex RGB color, for example FF942E."), () => config.ProgressColorHex, value => config.ProgressColorHex = value, customColorSelected, customColorSelected);
            menu.AddBoolOption(helper.ModManifest, () => T("config.verbose.name", "Verbose logs"), () => T("config.verbose.tooltip", "Write low-frequency viewer diagnostics."), () => config.VerboseLogging, value => config.VerboseLogging = value);
        }

        private IReadOnlyList<DtmColorPreset> BuildColorPresets()
        {
            return new[]
            {
                new DtmColorPreset("Orange", T("config.color.orange", "Orange"), "FF942E"),
                new DtmColorPreset("Green", T("config.color.green", "Green"), "70C978"),
                new DtmColorPreset("Blue", T("config.color.blue", "Blue"), "65A6FF"),
                new DtmColorPreset("Pink", T("config.color.pink", "Pink"), "FF7AC8"),
                new DtmColorPreset("White", T("config.color.white", "White"), "F0F0F0"),
                new DtmColorPreset("Custom", T("config.color.custom", "Custom"), config.ProgressColorHex)
            };
        }

        private string T(string key, string fallback) => helper.Translation.Get(key, fallback);

        private void ConfigureBridge(string reason)
        {
            IAnimalViewerApi? api = helper.ModRegistry.GetApi<IAnimalViewerApi>("DTMAPI.GameBridge.DolocTown");
            if (api == null)
            {
                helper.Monitor.Log("Animal viewer GameBridge API is not available.", LogLevel.Warn);
                return;
            }

            api.ConfigureSpecialProduceProgress(helper.ModManifest, new AnimalHusbandryProgressOptions
            {
                Enabled = config.Enabled,
                ProgressColor = ParseColor(config.ProgressColorHex),
                CacheSeconds = 0,
                VerboseLogging = config.VerboseLogging
            });
            BridgeFeatureStatus status = api.GetStatus(helper.ModManifest.UniqueID);
            helper.Monitor.Log("AnimalHusbandryProgress bridge policy OK reason=" + reason + " status=" + status.Status);
        }

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!updateEvidenceLogged)
            {
                updateEvidenceLogged = true;
                helper.Monitor.Log("AnimalHusbandryProgress UpdateTicked OK tick=" + e.Tick);
            }

            if (config.VerboseLogging && (DateTimeOffset.Now - lastStatusLog).TotalSeconds >= 30)
            {
                lastStatusLog = DateTimeOffset.Now;
                helper.Monitor.Log("AnimalHusbandryProgress low-frequency status enabled=" + config.Enabled + " colorPreset=" + config.ColorPreset + ".");
            }
        }

        private void ResetConfig()
        {
            config = new AnimalProgressConfig();
            NormalizeConfig();
        }

        private void NormalizeConfig()
        {
            config.ColorPreset = NormalizeChoice(config.ColorPreset, "Orange", "Orange", "Green", "Blue", "Pink", "White", "Custom");
            if (!config.ColorPreset.Equals("Custom", StringComparison.OrdinalIgnoreCase))
                config.ProgressColorHex = ColorPresetToHex(config.ColorPreset);
            config.ProgressColorHex = NormalizeHex(config.ProgressColorHex);
            config.CacheSeconds = 0;
        }

        private static string NormalizeChoice(string value, string fallback, params string[] allowed)
        {
            foreach (string option in allowed)
            {
                if (option.Equals(value, StringComparison.OrdinalIgnoreCase))
                    return option;
            }
            return fallback;
        }

        private static string ColorPresetToHex(string preset)
        {
            if (preset.Equals("Green", StringComparison.OrdinalIgnoreCase))
                return "70C978";
            if (preset.Equals("Blue", StringComparison.OrdinalIgnoreCase))
                return "65A6FF";
            if (preset.Equals("Pink", StringComparison.OrdinalIgnoreCase))
                return "FF7AC8";
            if (preset.Equals("White", StringComparison.OrdinalIgnoreCase))
                return "F0F0F0";
            return "FF942E";
        }

        private static string NormalizeHex(string value)
        {
            value = (value ?? string.Empty).Trim().TrimStart('#');
            if (value.Length != 6)
                return "FF942E";
            for (int i = 0; i < value.Length; i++)
            {
                if (!Uri.IsHexDigit(value[i]))
                    return "FF942E";
            }
            return value.ToUpperInvariant();
        }

        private static DtmColor ParseColor(string hex)
        {
            hex = NormalizeHex(hex);
            int r = int.Parse(hex.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            int g = int.Parse(hex.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            int b = int.Parse(hex.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return new DtmColor(r / 255d, g / 255d, b / 255d, 1);
        }

        [DataContract]
        public sealed class AnimalProgressConfig
        {
            [DataMember] public bool Enabled { get; set; } = true;
            [DataMember] public string ColorPreset { get; set; } = "Orange";
            [DataMember] public string ProgressColorHex { get; set; } = "FF942E";
            [DataMember] public int CacheSeconds { get; set; } = 10;
            [DataMember] public bool VerboseLogging { get; set; }
        }
    }
}
