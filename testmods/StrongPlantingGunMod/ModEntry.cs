using System;
using System.Globalization;
using System.Runtime.Serialization;
using DTMAPI.Abstractions;

namespace StrongPlantingGunMod
{
    public sealed class ModEntry : DtmMod
    {
        private IDtmHelper helper = null!;
        private StrongPlantingGunConfig config = new StrongPlantingGunConfig();
        private IStrongPlantingGunApi? strongPlantingGunApi;
        private StrongPlantingGunRegisterResult? lastRegisterResult;

        public override void Entry(IDtmHelper helper)
        {
            this.helper = helper;
            config = helper.ReadConfig<StrongPlantingGunConfig>();
            RegisterConfigMenu();
            BindStrongPlantingGunApi("Entry");
            helper.Events.Save.SaveLoaded += (_, e) => BindStrongPlantingGunApi("SaveLoaded slot=" + (e.SaveSlot?.ToString(CultureInfo.InvariantCulture) ?? "unknown"));
            helper.Monitor.Log(T("mod.loaded", "DTMAPI Strong Planting Gun loaded; the official farming gun can carry seed, film, and fertilizer slots."));
        }

        private void RegisterConfigMenu()
        {
            IDtmConfigMenuApi? menu = helper.ModRegistry.GetApi<IDtmConfigMenuApi>("DTMAPI.ModConfigMenu");
            if (menu == null)
                return;

            menu.Register(helper.ModManifest, ResetConfig, SaveConfig);
            menu.SetDisplayName(helper.ModManifest, () => T("mod.name", "DTMAPI Strong Planting Gun"));
            menu.AddSectionTitle(helper.ModManifest, () => T("config.section.main", "Strong planting gun"));
            menu.AddParagraph(helper.ModManifest, BuildStatusText);
            menu.AddBoolOption(helper.ModManifest, () => T("config.enabled.name", "Enabled"), () => T("config.enabled.tooltip", "Expands the official farming gun and applies supported seed, film, and fertilizer slots."), () => config.Enabled, value => config.Enabled = value);
            menu.AddNumberOption(helper.ModManifest, () => T("config.slots.name", "Slots"), () => T("config.slots.tooltip", "Number of official farming gun slots to expose. Three slots match seed, film, and fertilizer."), () => config.SlotCount, value => config.SlotCount = (int)Math.Round(value), 1, 6, 1);
            menu.AddBoolOption(helper.ModManifest, () => T("config.seeds.name", "Use seeds"), () => T("config.seeds.tooltip", "Plant seeds from farming gun slots using official basin checks."), () => config.IncludeSeeds, value => config.IncludeSeeds = value);
            menu.AddBoolOption(helper.ModManifest, () => T("config.films.name", "Use films"), () => T("config.films.tooltip", "Apply film from farming gun slots when official checks allow it."), () => config.IncludeFilms, value => config.IncludeFilms = value);
            menu.AddBoolOption(helper.ModManifest, () => T("config.fertilizers.name", "Use fertilizers"), () => T("config.fertilizers.tooltip", "Apply fertilizer from farming gun slots when official checks allow it."), () => config.IncludeFertilizers, value => config.IncludeFertilizers = value);
            menu.AddBoolOption(helper.ModManifest, () => T("config.verbose.name", "Verbose logs"), () => T("config.verbose.tooltip", "Write low-frequency Strong Planting Gun diagnostics."), () => config.VerboseLogging, value => config.VerboseLogging = value);
        }

        private void BindStrongPlantingGunApi(string reason)
        {
            strongPlantingGunApi = helper.ModRegistry.GetApi<IStrongPlantingGunApi>("DTMAPI.GameBridge.DolocTown");
            if (strongPlantingGunApi == null)
            {
                helper.Monitor.Log(T("mod.apiMissing", "Strong Planting Gun API is not available yet.") + " reason=" + reason, LogLevel.Warn);
                return;
            }

            lastRegisterResult = strongPlantingGunApi.Register(helper.ModManifest, new StrongPlantingGunOptions
            {
                Enabled = config.Enabled,
                SlotCount = config.SlotCount,
                IncludeSeeds = config.IncludeSeeds,
                IncludeFilms = config.IncludeFilms,
                IncludeFertilizers = config.IncludeFertilizers,
                IncludeWater = false,
                VerboseLogging = config.VerboseLogging
            });
            helper.Monitor.Log("StrongPlantingGun API register success=" + lastRegisterResult.Success + " reason=" + reason + " enabled=" + lastRegisterResult.Enabled + " slots=" + lastRegisterResult.SlotCount + " toolHook=" + lastRegisterResult.ToolHookInstalled + " uiHook=" + lastRegisterResult.UiHookInstalled + " message=" + lastRegisterResult.Message, lastRegisterResult.Success ? LogLevel.Info : LogLevel.Warn);
        }

        private string BuildStatusText()
        {
            if (strongPlantingGunApi == null)
                return T("config.status.missing", "Strong Planting Gun API is not available.");

            StrongPlantingGunState state = strongPlantingGunApi.GetState(helper.ModManifest.UniqueID);
            return string.Format(
                CultureInfo.InvariantCulture,
                T("config.status", "Status={0}, toolHook={1}, uiHook={2}, slots={3}, expanded={4}, seed={5}, film={6}, fertilizer={7}. {8}"),
                state.Status,
                state.ToolHookInstalled,
                state.UiHookInstalled,
                state.SlotCount,
                state.ExpandedGunCount,
                state.LastSeedActions,
                state.LastFilmActions,
                state.LastFertilizerActions,
                state.LastMessage);
        }

        private void SaveConfig()
        {
            helper.WriteConfig(config);
            BindStrongPlantingGunApi("config saved");
        }

        private void ResetConfig()
        {
            config = new StrongPlantingGunConfig();
        }

        private string T(string key, string fallback) => helper.Translation.Get(key, fallback);

        [DataContract]
        public sealed class StrongPlantingGunConfig
        {
            [DataMember] public bool Enabled { get; set; } = true;
            [DataMember] public int SlotCount { get; set; } = 3;
            [DataMember] public bool IncludeSeeds { get; set; } = true;
            [DataMember] public bool IncludeFilms { get; set; } = true;
            [DataMember] public bool IncludeFertilizers { get; set; } = true;
            [DataMember] public bool VerboseLogging { get; set; }
        }
    }
}
