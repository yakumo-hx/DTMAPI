using System;
using System.Globalization;
using System.Runtime.Serialization;
using DTMAPI.Abstractions;

namespace AnimalHusbandryProgressMod
{
    public sealed class ModEntry : DtmMod
    {
        private const string DefaultProgressLabel = "隐藏产物";
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
            menu.AddTextOption(helper.ModManifest, () => T("config.progressLabel.name", "Progress label"), () => T("config.progressLabel.tooltip", "Label for the appended progress bar."), () => config.ProgressLabel, value => config.ProgressLabel = value);
            menu.AddTextOption(helper.ModManifest, () => T("config.fillColor.name", "Fill color"), () => T("config.fillColor.tooltip", "Hex RGB color, for example FF942E."), () => config.ProgressColorHex, value => config.ProgressColorHex = value);
            menu.AddNumberOption(helper.ModManifest, () => T("config.cacheSeconds.name", "Cache seconds"), () => T("config.cacheSeconds.tooltip", "Cache animal viewer data before recomputing."), () => config.CacheSeconds, value => config.CacheSeconds = (int)Math.Round(value), 0, 120, 5);
            menu.AddBoolOption(helper.ModManifest, () => T("config.verbose.name", "Verbose logs"), () => T("config.verbose.tooltip", "Write low-frequency viewer diagnostics."), () => config.VerboseLogging, value => config.VerboseLogging = value);
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
                ProgressLabel = string.IsNullOrWhiteSpace(config.ProgressLabel) ? DefaultProgressLabel : config.ProgressLabel,
                ProgressColor = ParseColor(config.ProgressColorHex),
                CacheSeconds = config.CacheSeconds,
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
                helper.Monitor.Log("AnimalHusbandryProgress low-frequency status enabled=" + config.Enabled + " cacheSeconds=" + config.CacheSeconds);
            }
        }

        private void ResetConfig()
        {
            config = new AnimalProgressConfig();
            NormalizeConfig();
        }

        private void NormalizeConfig()
        {
            config.ProgressLabel = string.IsNullOrWhiteSpace(config.ProgressLabel) || config.ProgressLabel.Trim().Equals("Special produce", StringComparison.OrdinalIgnoreCase)
                ? DefaultProgressLabel
                : config.ProgressLabel.Trim();
            config.ProgressColorHex = NormalizeHex(config.ProgressColorHex);
            config.CacheSeconds = Math.Max(0, Math.Min(120, config.CacheSeconds));
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
            [DataMember] public string ProgressLabel { get; set; } = DefaultProgressLabel;
            [DataMember] public string ProgressColorHex { get; set; } = "FF942E";
            [DataMember] public int CacheSeconds { get; set; } = 10;
            [DataMember] public bool VerboseLogging { get; set; }
        }
    }
}
