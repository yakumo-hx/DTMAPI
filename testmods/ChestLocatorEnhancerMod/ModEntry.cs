using System;
using System.Globalization;
using System.Runtime.Serialization;
using DTMAPI.Abstractions;

namespace ChestLocatorEnhancerMod
{
    public sealed class ModEntry : DtmMod
    {
        private IDtmHelper helper = null!;
        private ChestLocatorEnhancerConfig config = new ChestLocatorEnhancerConfig();
        private IChestLocatorEnhancerApi? chestLocatorApi;
        private ChestLocatorEnhancerRegisterResult? lastRegisterResult;

        public override void Entry(IDtmHelper helper)
        {
            this.helper = helper;
            config = helper.ReadConfig<ChestLocatorEnhancerConfig>();
            RegisterConfigMenu();
            BindChestLocatorApi("Entry");
            helper.Events.Save.SaveLoaded += (_, e) => BindChestLocatorApi("SaveLoaded slot=" + (e.SaveSlot?.ToString(CultureInfo.InvariantCulture) ?? "unknown"));
            helper.Monitor.Log(T("mod.loaded", "DTMAPI Chest Locator Enhancer loaded; locator-marked containers can contribute materials across farm buildings."));
        }

        private void RegisterConfigMenu()
        {
            IDtmConfigMenuApi? menu = helper.ModRegistry.GetApi<IDtmConfigMenuApi>("DTMAPI.ModConfigMenu");
            if (menu == null)
                return;

            menu.Register(helper.ModManifest, ResetConfig, SaveConfig);
            menu.SetDisplayName(helper.ModManifest, () => T("mod.name", "DTMAPI Chest Locator Enhancer"));
            menu.AddSectionTitle(helper.ModManifest, () => T("config.section.main", "Chest locator"));
            menu.AddParagraph(helper.ModManifest, BuildStatusText);
            menu.AddBoolOption(helper.ModManifest, () => T("config.enabled.name", "Enabled"), () => T("config.enabled.tooltip", "Extends official shared-container material lookup across farm building rooms."), () => config.Enabled, value => config.Enabled = value);
            menu.AddBoolOption(helper.ModManifest, () => T("config.cases.name", "Shared chests"), () => T("config.cases.tooltip", "Includes Case inventories marked shared by the official locator."), () => config.IncludeSharedCases, value => config.IncludeSharedCases = value);
            menu.AddBoolOption(helper.ModManifest, () => T("config.shelves.name", "Storage shelves"), () => T("config.shelves.tooltip", "Includes ItemBox inventories inside shared storage shelves."), () => config.IncludeSharedStorageShelfBoxes, value => config.IncludeSharedStorageShelfBoxes = value);
            menu.AddBoolOption(helper.ModManifest, () => T("config.autoUseBox.name", "Respect auto-use boxes"), () => T("config.autoUseBox.tooltip", "Keeps the game's auto-use box setting for shared storage shelves."), () => config.RespectNativeAutoUseBoxSetting, value => config.RespectNativeAutoUseBoxSetting = value);
        }

        private void BindChestLocatorApi(string reason)
        {
            chestLocatorApi = helper.ModRegistry.GetApi<IChestLocatorEnhancerApi>("DTMAPI.GameBridge.DolocTown");
            if (chestLocatorApi == null)
            {
                helper.Monitor.Log(T("mod.apiMissing", "Chest Locator Enhancer API is not available yet.") + " reason=" + reason, LogLevel.Warn);
                return;
            }

            lastRegisterResult = chestLocatorApi.Register(helper.ModManifest, new ChestLocatorEnhancerOptions
            {
                Enabled = config.Enabled,
                IncludeSharedCases = config.IncludeSharedCases,
                IncludeSharedStorageShelfBoxes = config.IncludeSharedStorageShelfBoxes,
                RespectNativeAutoUseBoxSetting = config.RespectNativeAutoUseBoxSetting,
                VerboseLogging = config.VerboseLogging
            });
            helper.Monitor.Log("ChestLocatorEnhancer API register success=" + lastRegisterResult.Success + " reason=" + reason + " enabled=" + lastRegisterResult.Enabled + " hook=" + lastRegisterResult.HookInstalled + " message=" + lastRegisterResult.Message, lastRegisterResult.Success ? LogLevel.Info : LogLevel.Warn);
        }

        private string BuildStatusText()
        {
            if (chestLocatorApi == null)
                return T("config.status.missing", "Chest Locator Enhancer API is not available.");

            ChestLocatorEnhancerState state = chestLocatorApi.GetState(helper.ModManifest.UniqueID);
            return string.Format(
                CultureInfo.InvariantCulture,
                T("config.status", "Status={0}, hook={1}, appended={2}, cases={3}, shelfBoxes={4}. {5}"),
                state.Status,
                state.HookInstalled,
                state.LastAppendedInventoryCount,
                state.LastSharedCaseCount,
                state.LastSharedStorageBoxCount,
                state.LastMessage);
        }

        private void SaveConfig()
        {
            helper.WriteConfig(config);
            BindChestLocatorApi("config saved");
        }

        private void ResetConfig()
        {
            config = new ChestLocatorEnhancerConfig();
        }

        private string T(string key, string fallback) => helper.Translation.Get(key, fallback);

        [DataContract]
        public sealed class ChestLocatorEnhancerConfig
        {
            [DataMember] public bool Enabled { get; set; } = true;
            [DataMember] public bool IncludeSharedCases { get; set; } = true;
            [DataMember] public bool IncludeSharedStorageShelfBoxes { get; set; } = true;
            [DataMember] public bool RespectNativeAutoUseBoxSetting { get; set; } = true;
            [DataMember] public bool VerboseLogging { get; set; }
        }
    }
}
