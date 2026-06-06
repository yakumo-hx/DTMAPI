using System;
using System.Runtime.Serialization;
using DTMAPI.Abstractions;

namespace MoreEquipmentSlotsMod
{
    public sealed class ModEntry : DtmMod
    {
        private IDtmHelper helper = null!;
        private MoreEquipmentSlotsConfig config = new MoreEquipmentSlotsConfig();
        private IEquipmentSlotsApi? equipmentSlotsApi;
        private EquipmentSlotsRegisterResult? lastRegisterResult;

        public override void Entry(IDtmHelper helper)
        {
            this.helper = helper;
            config = helper.ReadConfig<MoreEquipmentSlotsConfig>();
            NormalizeConfig();
            RegisterConfigMenu();
            BindEquipmentSlotsApi("Entry");
            helper.Events.Save.SaveLoaded += (_, e) => BindEquipmentSlotsApi("SaveLoaded slot=" + (e.SaveSlot?.ToString() ?? "unknown"));
            helper.Monitor.Log(T("mod.loaded", "DTMAPI More Equipment Slots loaded; experimental Equipment Slots API owns the runtime extension policy."));
        }

        private void RegisterConfigMenu()
        {
            IDtmConfigMenuApi? menu = helper.ModRegistry.GetApi<IDtmConfigMenuApi>("DTMAPI.ModConfigMenu");
            if (menu == null)
                return;

            menu.Register(helper.ModManifest, ResetConfig, SaveConfig);
            menu.SetDisplayName(helper.ModManifest, () => T("mod.name", "DTMAPI More Equipment Slots"));
            menu.AddBoolOption(helper.ModManifest, () => T("config.enabled.name", "Enabled"), () => T("config.enabled.tooltip", "Registers extra attribute-only equipment slots."), () => config.Enabled, value => config.Enabled = value);
        }

        private void BindEquipmentSlotsApi(string reason)
        {
            equipmentSlotsApi = helper.ModRegistry.GetApi<IEquipmentSlotsApi>("DTMAPI.GameBridge.DolocTown");
            if (equipmentSlotsApi == null)
            {
                helper.Monitor.Log(T("mod.apiMissing", "Equipment Slots API is not available yet.") + " reason=" + reason, LogLevel.Warn);
                return;
            }

            lastRegisterResult = equipmentSlotsApi.RegisterSlots(helper.ModManifest, new EquipmentSlotsOptions
            {
                Enabled = config.Enabled,
                ExtraAttributeSlots = config.ExtraAttributeSlots,
                SlotIdPrefix = "dtmapi.more_equipment",
                PreserveVanillaVisualSlots = true,
                ExtraSlotsAffectVisuals = false,
                SafeUnequipOnDisable = config.SafeUnequipOnDisable,
                AutoRecoverOnMissingMod = true,
                VerboseLogging = config.VerboseLogging
            });
            helper.Monitor.Log("MoreEquipmentSlots API register success=" + lastRegisterResult.Success + " reason=" + reason + " message=" + lastRegisterResult.Message, lastRegisterResult.Success ? LogLevel.Info : LogLevel.Warn);
        }

        private void SaveConfig()
        {
            NormalizeConfig();
            helper.WriteConfig(config);
            BindEquipmentSlotsApi("config saved");
        }

        private void ResetConfig()
        {
            config = new MoreEquipmentSlotsConfig();
            NormalizeConfig();
        }

        private void NormalizeConfig()
        {
            config.ExtraAttributeSlots = 3;
            config.QuickEquipItemId = string.Empty;
            config.SafeUnequipOnDisable = true;
            config.VerboseLogging = false;
        }

        private string T(string key, string fallback) => helper.Translation.Get(key, fallback);

        [DataContract]
        public sealed class MoreEquipmentSlotsConfig
        {
            [DataMember] public bool Enabled { get; set; } = true;
            [DataMember] public int ExtraAttributeSlots { get; set; } = 3;
            [DataMember] public string QuickEquipItemId { get; set; } = string.Empty;
            [DataMember] public bool SafeUnequipOnDisable { get; set; } = true;
            [DataMember] public bool VerboseLogging { get; set; }
        }
    }
}
