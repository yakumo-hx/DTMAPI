using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using DTMAPI.Abstractions;

namespace Yuuka.DTMAPI.FishBreedingAssistant
{
    public sealed class ModEntry : DtmMod
    {
        private IDtmHelper helper = null!;
        private FishBreedingAssistantConfig config = new FishBreedingAssistantConfig();
        private readonly Dictionary<string, CacheEntry> cache = new Dictionary<string, CacheEntry>(StringComparer.Ordinal);
        private bool updateEvidenceLogged;
        private DateTimeOffset lastCacheLog = DateTimeOffset.MinValue;

        public override void Entry(IDtmHelper helper)
        {
            this.helper = helper;
            config = helper.ReadConfig<FishBreedingAssistantConfig>();
            NormalizeConfig();
            RegisterConfigMenu();
            ConfigureBridge("Entry");

            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.Save.SaveLoaded += (_, e) => helper.Monitor.Log("FishBreedingAssistant SaveLoaded tooltip boundary OK slot=" + (e.SaveSlot?.ToString() ?? "unknown"));
            helper.Monitor.Log("FishBreedingAssistant migrated to DTMAPI shell. Tooltip provider registered with cacheSeconds=" + config.CacheSeconds + ".");
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
                cache.Clear();
                ConfigureBridge("config saved");
                helper.Monitor.Log("FishBreedingAssistant config saved through DTMAPI menu.");
            });

            menu.SetDisplayName(helper.ModManifest, () => T("mod.name", helper.ModManifest.Name));
            menu.AddSectionTitle(helper.ModManifest, () => T("config.section.main", "Fish roe tooltip"));
            menu.AddBoolOption(helper.ModManifest, () => T("config.enabled.name", "Enabled"), () => T("config.enabled.tooltip", "Master switch for fish roe display."), () => config.Enabled, value => config.Enabled = value);
            menu.AddBoolOption(helper.ModManifest, () => T("config.labelTitle.name", "Label title"), () => T("config.labelTitle.tooltip", "Append the hatch fish name to roe titles."), () => config.LabelFishRoeTitle, value => config.LabelFishRoeTitle = value);
            menu.AddBoolOption(helper.ModManifest, () => T("config.labelDetails.name", "Label details"), () => T("config.labelDetails.tooltip", "Append incubation and growth details."), () => config.LabelFishRoeDetails, value => config.LabelFishRoeDetails = value);
            menu.AddNumberOption(helper.ModManifest, () => T("config.cacheSeconds.name", "Cache seconds"), () => T("config.cacheSeconds.tooltip", "Cache fish lookup results for tooltip hooks."), () => config.CacheSeconds, value => config.CacheSeconds = (int)Math.Round(value), 0, 300, 5);
            menu.AddBoolOption(helper.ModManifest, () => T("config.verbose.name", "Verbose logs"), () => T("config.verbose.tooltip", "Write low-frequency cache diagnostics."), () => config.VerboseLogging, value => config.VerboseLogging = value);
        }

        private string T(string key, string fallback) => helper.Translation.Get(key, fallback);

        private void ConfigureBridge(string reason)
        {
            IItemTooltipApi? api = helper.ModRegistry.GetApi<IItemTooltipApi>("DTMAPI.GameBridge.DolocTown");
            if (api == null)
            {
                helper.Monitor.Log("Item tooltip GameBridge API is not available.", LogLevel.Warn);
                return;
            }

            api.ConfigureFishRoeProvider(helper.ModManifest, new FishRoeTooltipOptions
            {
                Enabled = config.Enabled,
                LabelFishRoeTitle = config.LabelFishRoeTitle,
                LabelFishRoeDetails = config.LabelFishRoeDetails,
                CacheSeconds = config.CacheSeconds,
                VerboseLogging = config.VerboseLogging
            }, LookupFishRoe);
            BridgeFeatureStatus status = api.GetStatus(helper.ModManifest.UniqueID);
            helper.Monitor.Log("FishBreedingAssistant bridge provider OK reason=" + reason + " status=" + status.Status);
        }

        private FishRoeDisplayInfo? LookupFishRoe(string fishId)
        {
            if (string.IsNullOrWhiteSpace(fishId))
                return null;

            DateTimeOffset now = DateTimeOffset.Now;
            if (cache.TryGetValue(fishId, out CacheEntry entry) && entry.ExpiresAt > now)
                return entry.Info;

            if (!FishBreedingLookup.TryGet(fishId, out FishRoeInfo info))
                return null;

            var mapped = new FishRoeDisplayInfo
            {
                FishId = info.FishId,
                FishTitle = info.FishTitle,
                RoeTitle = info.RoeTitle,
                IncubateText = info.IncubateText,
                GrowText = info.GrowText,
                ParentSummary = info.ParentSummary
            };
            cache[fishId] = new CacheEntry(mapped, now.AddSeconds(config.CacheSeconds));
            MaybeLogCache();
            return mapped;
        }

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!updateEvidenceLogged)
            {
                updateEvidenceLogged = true;
                helper.Monitor.Log("FishBreedingAssistant UpdateTicked OK tick=" + e.Tick);
            }
        }

        private void MaybeLogCache()
        {
            if (!config.VerboseLogging)
                return;
            DateTimeOffset now = DateTimeOffset.Now;
            if ((now - lastCacheLog).TotalSeconds < 30)
                return;
            lastCacheLog = now;
            helper.Monitor.Log("FishBreedingAssistant cache entries=" + cache.Count);
        }

        private void ResetConfig()
        {
            config = new FishBreedingAssistantConfig();
            NormalizeConfig();
        }

        private void NormalizeConfig()
        {
            config.CacheSeconds = Math.Max(0, Math.Min(300, config.CacheSeconds));
        }

        private sealed class CacheEntry
        {
            public CacheEntry(FishRoeDisplayInfo info, DateTimeOffset expiresAt)
            {
                Info = info;
                ExpiresAt = expiresAt;
            }

            public FishRoeDisplayInfo Info { get; }
            public DateTimeOffset ExpiresAt { get; }
        }

        [DataContract]
        public sealed class FishBreedingAssistantConfig
        {
            [DataMember] public bool Enabled { get; set; } = true;
            [DataMember] public bool LabelFishRoeTitle { get; set; } = true;
            [DataMember] public bool LabelFishRoeDetails { get; set; } = true;
            [DataMember] public int CacheSeconds { get; set; } = 30;
            [DataMember] public bool VerboseLogging { get; set; }
        }
    }
}
