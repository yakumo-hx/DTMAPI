using System;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using DTMAPI.Abstractions;

namespace AutoHarvestMod
{
    public sealed class ModEntry : DtmMod
    {
        private IDtmHelper helper = null!;
        private AutoHarvestConfig config = new AutoHarvestConfig();
        private uint lastAutoRunSecond;
        private string registeredManualKey = string.Empty;

        public override void Entry(IDtmHelper helper)
        {
            this.helper = helper;
            config = helper.ReadConfig<AutoHarvestConfig>();
            NormalizeConfig();
            RegisterInputKeys();
            RegisterConfigMenu();

            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.GameLoop.OneSecondUpdateTicked += OnOneSecondUpdateTicked;
            helper.Events.Save.SaveLoaded += (_, e) => helper.Monitor.Log("AutoHarvest SaveLoaded boundary OK slot=" + (e.SaveSlot?.ToString(CultureInfo.InvariantCulture) ?? "unknown"));
            helper.Events.GameLoop.ReturnedToTitle += (_, __) => helper.Monitor.Log("AutoHarvest ReturnedToTitle boundary OK.");
            helper.Monitor.Log(T("mod.loaded", "Auto Harvest loaded; disabled by default and using the DTMAPI CropHarvesting API."));
        }

        private void RegisterConfigMenu()
        {
            IDtmConfigMenuApi? menu = helper.ModRegistry.GetApi<IDtmConfigMenuApi>("DTMAPI.ModConfigMenu");
            if (menu == null)
            {
                helper.Monitor.Log(T("mod.apiMissing", "CropHarvesting API is not available yet."), LogLevel.Warn);
                return;
            }

            menu.Register(helper.ModManifest, ResetConfig, () =>
            {
                NormalizeConfig();
                helper.WriteConfig(config);
                RegisterInputKeys();
                helper.Monitor.Log("AutoHarvest config saved through DTMAPI menu.");
            });
            menu.SetDisplayName(helper.ModManifest, () => T("mod.name", helper.ModManifest.Name));
            menu.AddSectionTitle(helper.ModManifest, () => T("config.section.main", "Auto harvest"));
            menu.AddParagraph(helper.ModManifest, BuildStatusText);
            menu.AddBoolOption(helper.ModManifest, () => T("config.enabled.name", "Enabled"), () => T("config.enabled.tooltip", "Automatically scan mature crops on an interval."), () => config.Enabled, value => config.Enabled = value);
            menu.AddKeybindOption(helper.ModManifest, () => T("config.hotkey.name", "Manual harvest key"), () => T("config.hotkey.tooltip", "Runs one crop scan and harvest request."), () => config.ManualHarvestKey, value => config.ManualHarvestKey = value);
            menu.AddNumberOption(helper.ModManifest, () => T("config.interval.name", "Scan interval"), () => T("config.interval.tooltip", "Seconds between automatic scans while enabled."), () => config.IntervalSeconds, value => config.IntervalSeconds = (int)Math.Round(value), 5, 600, 5);
            menu.AddNumberOption(helper.ModManifest, () => T("config.maxHarvests.name", "Max harvests per run"), () => T("config.maxHarvests.tooltip", "Limits how many mature crops one API call can harvest."), () => config.MaxHarvestsPerRun, value => config.MaxHarvestsPerRun = (int)Math.Round(value), 1, 200, 1);
            menu.AddBoolOption(helper.ModManifest, () => T("config.includeVines.name", "Include vines"), () => T("config.includeVines.tooltip", "Includes PlantBasin crops classified as vines by the API."), () => config.IncludeVines, value => config.IncludeVines = value);
            menu.AddBoolOption(helper.ModManifest, () => T("config.includeMushrooms.name", "Include mushrooms"), () => T("config.includeMushrooms.tooltip", "Includes PlantBasin crops classified as mushrooms/fungi by the API."), () => config.IncludeMushrooms, value => config.IncludeMushrooms = value);
            menu.AddBoolOption(helper.ModManifest, () => T("config.verbose.name", "Verbose logs"), () => T("config.verbose.tooltip", "Writes scan/harvest summaries to the DTMAPI log."), () => config.VerboseLogging, value => config.VerboseLogging = value);
        }

        private string BuildStatusText()
        {
            ICropHarvestingApi? api = helper.ModRegistry.GetApi<ICropHarvestingApi>("DTMAPI.GameBridge.DolocTown");
            if (api == null)
                return T("mod.apiMissing", "CropHarvesting API is not available yet.");

            BridgeFeatureStatus status = api.GetStatus(helper.ModManifest.UniqueID);
            return string.Format(CultureInfo.InvariantCulture, T("config.status", "Status={0}, details={1}"), status.Status, status.Details);
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (Matches(e.Button, config.ManualHarvestKey))
                RunHarvest("manual-key " + e.Button);
        }

        private void OnOneSecondUpdateTicked(object? sender, OneSecondUpdateTickedEventArgs e)
        {
            if (!config.Enabled)
                return;

            if (lastAutoRunSecond != 0 && e.Second - lastAutoRunSecond < config.IntervalSeconds)
                return;

            lastAutoRunSecond = e.Second;
            RunHarvest("auto-interval");
        }

        private void RunHarvest(string reason)
        {
            ICropHarvestingApi? api = helper.ModRegistry.GetApi<ICropHarvestingApi>("DTMAPI.GameBridge.DolocTown");
            if (api == null)
            {
                helper.Monitor.Log(T("mod.apiMissing", "CropHarvesting API is not available yet.") + " reason=" + reason, LogLevel.Warn);
                return;
            }

            CropHarvestRequest scanRequest = BuildRequest(dryRun: true);
            CropHarvestResult scan = api.ScanMatureCrops(helper.ModManifest, scanRequest);
            string[] targetIds = scan.Targets
                .Where(target => target.Status == CropHarvestTargetStatus.Pending)
                .Take(Math.Max(1, config.MaxHarvestsPerRun))
                .Select(target => target.TargetId)
                .ToArray();
            if (targetIds.Length == 0)
            {
                if (config.VerboseLogging)
                    helper.Monitor.Log("AutoHarvest no mature crop targets reason=" + reason + " scan=" + FormatResult(scan));
                return;
            }

            CropHarvestRequest harvestRequest = BuildRequest(dryRun: false);
            harvestRequest.TargetIds = targetIds;
            CropHarvestResult harvest = api.HarvestMatureCrops(helper.ModManifest, harvestRequest);
            helper.Monitor.Log("AutoHarvest run reason=" + reason + " targets=" + targetIds.Length + " scan={" + FormatResult(scan) + "} harvest={" + FormatResult(harvest) + "}", harvest.Success ? LogLevel.Info : LogLevel.Warn);
        }

        private CropHarvestRequest BuildRequest(bool dryRun)
        {
            return new CropHarvestRequest
            {
                IncludeOrdinaryCrops = true,
                IncludeVines = config.IncludeVines,
                IncludeMushrooms = config.IncludeMushrooms,
                IncludeTrees = false,
                MaxHarvests = config.MaxHarvestsPerRun,
                DryRun = dryRun,
                SendNativeMessage = config.SendNativeMessage,
                VerboseLogging = config.VerboseLogging
            };
        }

        private static string FormatResult(CropHarvestResult result)
        {
            return "success=" + result.Success +
                ", mature=" + result.MatureTargetsFound +
                ", harvested=" + result.HarvestedCount +
                ", skipped=" + result.SkippedCount +
                ", failed=" + result.FailedCount +
                ", reason=" + result.FailureReason;
        }

        private void RegisterInputKeys()
        {
            UnregisterKey(registeredManualKey);
            registeredManualKey = NormalizeKey(config.ManualHarvestKey);
            helper.Input.RegisterButton(registeredManualKey);
        }

        private void UnregisterKey(string key)
        {
            if (!string.IsNullOrWhiteSpace(key) && !key.Equals("None", StringComparison.OrdinalIgnoreCase))
                helper.Input.UnregisterButton(key);
        }

        private void ResetConfig()
        {
            config = new AutoHarvestConfig();
            NormalizeConfig();
        }

        private void NormalizeConfig()
        {
            config.ManualHarvestKey = NormalizeKey(config.ManualHarvestKey);
            config.IntervalSeconds = Math.Max(5, Math.Min(600, config.IntervalSeconds));
            config.MaxHarvestsPerRun = Math.Max(1, Math.Min(200, config.MaxHarvestsPerRun));
        }

        private string T(string key, string fallback) => helper.Translation.Get(key, fallback);

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
        public sealed class AutoHarvestConfig
        {
            [DataMember] public bool Enabled { get; set; }
            [DataMember] public string ManualHarvestKey { get; set; } = "F7";
            [DataMember] public int IntervalSeconds { get; set; } = 30;
            [DataMember] public int MaxHarvestsPerRun { get; set; } = 24;
            [DataMember] public bool IncludeVines { get; set; } = true;
            [DataMember] public bool IncludeMushrooms { get; set; } = true;
            [DataMember] public bool SendNativeMessage { get; set; }
            [DataMember] public bool VerboseLogging { get; set; }
        }
    }
}
