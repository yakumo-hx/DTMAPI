using System;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using DTMAPI.Abstractions;

namespace CropHarvestingQaMod
{
    public sealed class ModEntry : DtmMod
    {
        private IDtmHelper helper = null!;
        private CropHarvestingQaConfig config = new CropHarvestingQaConfig();
        private string registeredScanKey = string.Empty;
        private string registeredHarvestOneKey = string.Empty;
        private string registeredHarvestBatchKey = string.Empty;
        private string lastSummary = string.Empty;

        public override void Entry(IDtmHelper helper)
        {
            this.helper = helper;
            config = helper.ReadConfig<CropHarvestingQaConfig>();
            NormalizeConfig();
            RegisterInputKeys();
            RegisterConfigMenu();

            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.Save.SaveLoaded += (_, e) =>
            {
                ClearLastSummary("SaveLoaded slot=" + (e.SaveSlot?.ToString(CultureInfo.InvariantCulture) ?? "unknown"));
                helper.Monitor.Log("CropHarvesting QA SaveLoaded boundary OK.");
            };
            helper.Events.GameLoop.ReturnedToTitle += (_, __) =>
            {
                ClearLastSummary("ReturnedToTitle");
                helper.Monitor.Log("CropHarvesting QA ReturnedToTitle boundary OK.");
            };

            helper.Monitor.Log(T("mod.loaded", "Crop Harvesting QA loaded. Use the DTMAPI config page buttons, or bind optional scan/harvest hotkeys manually."));
        }

        private void RegisterConfigMenu()
        {
            IDtmConfigMenuApi? menu = helper.ModRegistry.GetApi<IDtmConfigMenuApi>("DTMAPI.ModConfigMenu");
            if (menu == null)
            {
                helper.Monitor.Log(T("mod.configMenuMissing", "DTMAPI config menu API is not available; hotkeys remain available."), LogLevel.Warn);
                return;
            }

            menu.Register(helper.ModManifest, ResetConfig, SaveConfig);
            menu.SetDisplayName(helper.ModManifest, () => T("mod.name", "Crop Harvesting QA"));
            menu.AddSectionTitle(helper.ModManifest, () => T("config.section.main", "Manual QA controls"));
            menu.AddParagraph(helper.ModManifest, BuildStatusText);
            menu.AddParagraph(helper.ModManifest, () => T("config.boundary", "Scope: crop-container API only. Tree-basin crops are scan-only/unsupported until their native owner is reviewed; grass, wild trees, and forage are not harvested by this API."));
            menu.AddButton(helper.ModManifest, () => T("config.button.scan", "Scan only"), () => T("config.button.scan.tooltip", "Run one dry-run scan."), () => RunScanOnly("config-button"));
            menu.AddButton(helper.ModManifest, () => T("config.button.harvestOne", "Harvest one"), () => T("config.button.harvestOne.tooltip", "Scan and harvest the first pending target."), () => RunHarvest("config-button-one", 1));
            menu.AddButton(helper.ModManifest, () => T("config.button.harvestBatch", "Harvest batch"), () => T("config.button.harvestBatch.tooltip", "Scan and harvest up to Max harvests pending targets."), () => RunHarvest("config-button-batch", config.MaxHarvests));
            menu.AddButton(helper.ModManifest, () => T("config.button.dumpLast", "Dump last summary"), () => T("config.button.dumpLast.tooltip", "Write the last operation summary to the DTMAPI log again."), DumpLastSummary);
            menu.AddKeybindOption(helper.ModManifest, () => T("config.scanKey.name", "Scan key"), () => T("config.scanKey.tooltip", "Runs a dry-run scan and logs kind/status counts."), () => config.ScanKey, value => config.ScanKey = value);
            menu.AddKeybindOption(helper.ModManifest, () => T("config.harvestOneKey.name", "Harvest one key"), () => T("config.harvestOneKey.tooltip", "Scans, then harvests one pending target by transient TargetId."), () => config.HarvestOneKey, value => config.HarvestOneKey = value);
            menu.AddKeybindOption(helper.ModManifest, () => T("config.harvestBatchKey.name", "Harvest batch key"), () => T("config.harvestBatchKey.tooltip", "Scans, then harvests up to Max harvests by transient TargetId."), () => config.HarvestBatchKey, value => config.HarvestBatchKey = value);
            menu.AddNumberOption(helper.ModManifest, () => T("config.maxHarvests.name", "Max harvests"), () => T("config.maxHarvests.tooltip", "Maximum pending targets for the batch operation."), () => config.MaxHarvests, value => config.MaxHarvests = (int)Math.Round(value), 1, 200, 1);
            menu.AddNumberOption(helper.ModManifest, () => T("config.targetRows.name", "Logged target rows"), () => T("config.targetRows.tooltip", "Maximum per-target rows written to the DTMAPI log after each operation."), () => config.TargetRowsToLog, value => config.TargetRowsToLog = (int)Math.Round(value), 0, 200, 1);
            menu.AddBoolOption(helper.ModManifest, () => T("config.includeOrdinary.name", "Include ordinary crops"), () => T("config.includeOrdinary.tooltip", "Includes ordinary PlantBasin crop-container targets."), () => config.IncludeOrdinaryCrops, value => config.IncludeOrdinaryCrops = value);
            menu.AddBoolOption(helper.ModManifest, () => T("config.includeVines.name", "Include vines"), () => T("config.includeVines.tooltip", "Includes PlantBasin-family targets classified as vines."), () => config.IncludeVines, value => config.IncludeVines = value);
            menu.AddBoolOption(helper.ModManifest, () => T("config.includeMushroomBags.name", "Include mushroom bags"), () => T("config.includeMushroomBags.tooltip", "Includes PlantBasin-family targets classified as mushroom bags."), () => config.IncludeMushroomBags, value => config.IncludeMushroomBags = value);
            menu.AddBoolOption(helper.ModManifest, () => T("config.includeBushes.name", "Include bushes"), () => T("config.includeBushes.tooltip", "Includes PlantBasin-family targets classified as bushes."), () => config.IncludeBushes, value => config.IncludeBushes = value);
            menu.AddBoolOption(helper.ModManifest, () => T("config.includeTreeBasinCrops.name", "Show tree-basin crops"), () => T("config.includeTreeBasinCrops.tooltip", "Includes tree-basin crop targets in scan summaries; they remain unsupported/not executed by this API slice."), () => config.IncludeTreeBasinCrops, value => config.IncludeTreeBasinCrops = value);
            menu.AddBoolOption(helper.ModManifest, () => T("config.verboseTargets.name", "Verbose target rows"), () => T("config.verboseTargets.tooltip", "Writes sample target rows with target kind, status, id, room, and message."), () => config.VerboseTargets, value => config.VerboseTargets = value);
            menu.AddBoolOption(helper.ModManifest, () => T("config.nativeMessage.name", "Native harvest message"), () => T("config.nativeMessage.tooltip", "Passes SendNativeMessage to the harvest request."), () => config.SendNativeMessage, value => config.SendNativeMessage = value);
        }

        private string BuildStatusText()
        {
            ICropHarvestingApi? api = GetApi(logMissing: false);
            if (api == null)
                return T("mod.apiMissing", "CropHarvesting API is not available.");

            BridgeFeatureStatus status = api.GetStatus(helper.ModManifest.UniqueID);
            string summary = string.IsNullOrWhiteSpace(lastSummary)
                ? T("config.summary.empty", "No crop QA operation has run yet.")
                : string.Format(CultureInfo.InvariantCulture, T("config.summary", "Last operation: {0}"), lastSummary);
            return "Feature=" + status.Status + ", details=" + status.Details + ". " + summary;
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (Matches(e.Button, config.ScanKey))
                RunScanOnly("key " + e.Button);
            else if (Matches(e.Button, config.HarvestOneKey))
                RunHarvest("key " + e.Button, 1);
            else if (Matches(e.Button, config.HarvestBatchKey))
                RunHarvest("key " + e.Button, config.MaxHarvests);
        }

        private void RunScanOnly(string reason)
        {
            ICropHarvestingApi? api = GetApi(logMissing: true);
            if (api == null)
                return;

            CropHarvestResult scan = api.ScanMatureCrops(helper.ModManifest, BuildRequest(dryRun: true));
            string summary = "scan reason=" + reason + " {" + FormatResult(scan) + "} kinds={" + FormatKindCounts(scan) + "} statuses={" + FormatStatusCounts(scan) + "}";
            RecordSummary(summary, scan.Success ? LogLevel.Info : LogLevel.Warn);
            LogTargetRows("scan", scan);
        }

        private void RunHarvest(string reason, int maxTargets)
        {
            ICropHarvestingApi? api = GetApi(logMissing: true);
            if (api == null)
                return;

            CropHarvestResult scan = api.ScanMatureCrops(helper.ModManifest, BuildRequest(dryRun: true));
            string[] targetIds = scan.Targets
                .Where(target => target.Status == CropHarvestTargetStatus.Pending)
                .Take(Math.Max(1, maxTargets))
                .Select(target => target.TargetId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToArray();

            if (targetIds.Length == 0)
            {
                string noTargetSummary = "harvest reason=" + reason + " no pending executable targets; scan={" + FormatResult(scan) + "} kinds={" + FormatKindCounts(scan) + "} statuses={" + FormatStatusCounts(scan) + "}";
                RecordSummary(noTargetSummary, scan.Success ? LogLevel.Info : LogLevel.Warn);
                LogTargetRows("scan-no-target", scan);
                return;
            }

            CropHarvestRequest harvestRequest = BuildRequest(dryRun: false);
            harvestRequest.TargetIds = targetIds;
            harvestRequest.MaxHarvests = targetIds.Length;
            CropHarvestResult harvest = api.HarvestMatureCrops(helper.ModManifest, harvestRequest);

            string summary = "harvest reason=" + reason +
                " targetIds=" + targetIds.Length +
                " scan={" + FormatResult(scan) + "}" +
                " harvest={" + FormatResult(harvest) + "}" +
                " scanKinds={" + FormatKindCounts(scan) + "}" +
                " harvestStatuses={" + FormatStatusCounts(harvest) + "}";
            RecordSummary(summary, scan.Success && harvest.Success ? LogLevel.Info : LogLevel.Warn);
            LogTargetRows("scan-before-harvest", scan);
            LogTargetRows("harvest", harvest);
        }

        private CropHarvestRequest BuildRequest(bool dryRun)
        {
            NormalizeConfig();
            return new CropHarvestRequest
            {
                IncludeOrdinaryCrops = config.IncludeOrdinaryCrops,
                IncludeVines = config.IncludeVines,
                IncludeMushroomBags = config.IncludeMushroomBags,
                IncludeBushes = config.IncludeBushes,
                IncludeTreeBasinCrops = config.IncludeTreeBasinCrops,
                MaxHarvests = config.MaxHarvests,
                DryRun = dryRun,
                SendNativeMessage = config.SendNativeMessage,
                VerboseLogging = true
            };
        }

        private ICropHarvestingApi? GetApi(bool logMissing)
        {
            ICropHarvestingApi? api = helper.ModRegistry.GetApi<ICropHarvestingApi>("DTMAPI.GameBridge.DolocTown");
            if (api == null && logMissing)
                helper.Monitor.Log(T("mod.apiMissing", "CropHarvesting API is not available."), LogLevel.Warn);
            return api;
        }

        private void LogTargetRows(string label, CropHarvestResult result)
        {
            if (!config.VerboseTargets || config.TargetRowsToLog <= 0)
                return;

            foreach (CropHarvestTargetResult target in result.Targets.Take(config.TargetRowsToLog))
            {
                helper.Monitor.Log(
                    "CropHarvesting QA target " + label +
                    " kind=" + target.Kind +
                    " status=" + target.Status +
                    " mature=" + target.IsMature +
                    " room=" + Shorten(target.RoomTitle, 48) +
                    " equipment=" + Shorten(target.EquipmentName, 48) +
                    " crop=" + Shorten(target.CropTitle, 48) +
                    " id=" + Shorten(target.TargetId, 72) +
                    " message=" + Shorten(target.Message, 96));
            }
        }

        private void RecordSummary(string summary, LogLevel level)
        {
            lastSummary = summary;
            helper.Monitor.Log("CropHarvesting QA " + summary, level);
        }

        private void DumpLastSummary()
        {
            if (string.IsNullOrWhiteSpace(lastSummary))
                helper.Monitor.Log(T("config.summary.empty", "No crop QA operation has run yet."));
            else
                helper.Monitor.Log("CropHarvesting QA last summary: " + lastSummary);
        }

        private void ClearLastSummary(string reason)
        {
            lastSummary = string.Empty;
            helper.Monitor.Log("CropHarvesting QA cleared transient summary reason=" + reason + ".");
        }

        private void RegisterInputKeys()
        {
            UnregisterKey(registeredScanKey);
            UnregisterKey(registeredHarvestOneKey);
            UnregisterKey(registeredHarvestBatchKey);

            registeredScanKey = NormalizeKey(config.ScanKey);
            registeredHarvestOneKey = NormalizeKey(config.HarvestOneKey);
            registeredHarvestBatchKey = NormalizeKey(config.HarvestBatchKey);

            RegisterKey(registeredScanKey);
            RegisterKey(registeredHarvestOneKey);
            RegisterKey(registeredHarvestBatchKey);
        }

        private void RegisterKey(string key)
        {
            if (!key.Equals("None", StringComparison.OrdinalIgnoreCase))
                helper.Input.RegisterButton(key);
        }

        private void UnregisterKey(string key)
        {
            if (!string.IsNullOrWhiteSpace(key) && !key.Equals("None", StringComparison.OrdinalIgnoreCase))
                helper.Input.UnregisterButton(key);
        }

        private void SaveConfig()
        {
            NormalizeConfig();
            helper.WriteConfig(config);
            RegisterInputKeys();
            helper.Monitor.Log("CropHarvesting QA config saved.");
        }

        private void ResetConfig()
        {
            config = new CropHarvestingQaConfig();
            NormalizeConfig();
        }

        private void NormalizeConfig()
        {
            config.ScanKey = NormalizeKey(config.ScanKey);
            config.HarvestOneKey = NormalizeKey(config.HarvestOneKey);
            config.HarvestBatchKey = NormalizeKey(config.HarvestBatchKey);
            config.MaxHarvests = Math.Max(1, Math.Min(200, config.MaxHarvests));
            config.TargetRowsToLog = Math.Max(0, Math.Min(200, config.TargetRowsToLog));
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

        private static string FormatResult(CropHarvestResult result)
        {
            return "success=" + result.Success +
                ", dryRun=" + result.DryRun +
                ", rooms=" + result.RoomsVisited +
                ", basins=" + result.PlantBasinsVisited +
                ", mature=" + result.MatureTargetsFound +
                ", harvested=" + result.HarvestedCount +
                ", skipped=" + result.SkippedCount +
                ", failed=" + result.FailedCount +
                ", reason=" + result.FailureReason +
                ", message=" + Shorten(result.Message, 140);
        }

        private static string FormatKindCounts(CropHarvestResult result)
        {
            return FormatGroups(result.Targets
                .GroupBy(target => target.Kind.ToString())
                .OrderBy(group => group.Key)
                .Select(group => new CountItem(group.Key, group.Count())));
        }

        private static string FormatStatusCounts(CropHarvestResult result)
        {
            return FormatGroups(result.Targets
                .GroupBy(target => target.Status.ToString())
                .OrderBy(group => group.Key)
                .Select(group => new CountItem(group.Key, group.Count())));
        }

        private static string FormatGroups(System.Collections.Generic.IEnumerable<CountItem> items)
        {
            StringBuilder builder = new StringBuilder();
            foreach (CountItem item in items)
            {
                if (builder.Length > 0)
                    builder.Append(", ");
                builder.Append(item.Name).Append("=").Append(item.Count.ToString(CultureInfo.InvariantCulture));
            }

            return builder.Length == 0 ? "none" : builder.ToString();
        }

        private static string Shorten(string value, int maxLength)
        {
            value ??= string.Empty;
            value = value.Replace("\r", " ").Replace("\n", " ").Trim();
            if (value.Length <= maxLength)
                return value;
            return value.Substring(0, Math.Max(0, maxLength - 3)) + "...";
        }

        private sealed class CountItem
        {
            public CountItem(string name, int count)
            {
                Name = name;
                Count = count;
            }

            public string Name { get; }
            public int Count { get; }
        }

        [DataContract]
        public sealed class CropHarvestingQaConfig
        {
            [DataMember] public string ScanKey { get; set; } = "None";
            [DataMember] public string HarvestOneKey { get; set; } = "None";
            [DataMember] public string HarvestBatchKey { get; set; } = "None";
            [DataMember] public int MaxHarvests { get; set; } = 24;
            [DataMember] public int TargetRowsToLog { get; set; } = 24;
            [DataMember] public bool IncludeOrdinaryCrops { get; set; } = true;
            [DataMember] public bool IncludeVines { get; set; } = true;
            [DataMember] public bool IncludeMushroomBags { get; set; } = true;
            [DataMember] public bool IncludeBushes { get; set; } = true;
            [DataMember] public bool IncludeTreeBasinCrops { get; set; } = true;
            [DataMember] public bool VerboseTargets { get; set; } = true;
            [DataMember] public bool SendNativeMessage { get; set; }
        }
    }
}
