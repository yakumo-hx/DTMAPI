using System;
using System.Linq;
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
            helper.Events.GameLoop.ReturnedToTitle += (_, e) => RecoverExtraSlots("ReturnedToTitle");
            helper.Monitor.Log(T("mod.loaded", "DTMAPI More Equipment Slots loaded; experimental Equipment Slots API owns the runtime extension policy."));
        }

        private void RegisterConfigMenu()
        {
            IDtmConfigMenuApi? menu = helper.ModRegistry.GetApi<IDtmConfigMenuApi>("DTMAPI.ModConfigMenu");
            if (menu == null)
                return;

            menu.Register(helper.ModManifest, ResetConfig, SaveConfig);
            menu.SetDisplayName(helper.ModManifest, () => T("mod.name", "DTMAPI More Equipment Slots"));
            menu.AddSectionTitle(helper.ModManifest, () => T("config.section.slots", "Equipment slots"));
            menu.AddParagraph(helper.ModManifest, BuildStatusText);
            menu.AddBoolOption(helper.ModManifest, () => T("config.enabled.name", "Enabled"), () => T("config.enabled.tooltip", "Registers extra attribute-only equipment slots."), () => config.Enabled, value => config.Enabled = value);
            menu.AddNumberOption(helper.ModManifest, () => T("config.extra.name", "Extra slots"), () => T("config.extra.tooltip", "Extra attribute-only slots. Default visual slots keep their normal appearance effects."), () => config.ExtraAttributeSlots, value => config.ExtraAttributeSlots = (int)Math.Round(value), 0, 12, 1);
            menu.AddTextOption(helper.ModManifest, () => T("config.quickEquip.name", "Equip item id"), () => T("config.quickEquip.tooltip", "Passive item id to equip into the first empty DTMAPI extra slot."), () => config.QuickEquipItemId, value => config.QuickEquipItemId = (value ?? string.Empty).Trim());
            menu.AddButton(helper.ModManifest, () => T("config.equipFirst.name", "Equip first empty"), () => T("config.equipFirst.tooltip", "Consumes one matching passive item from the backpack and applies it as an attribute-only extra slot."), EquipFirstEmptySlot);
            menu.AddButton(helper.ModManifest, () => T("config.recover.name", "Recover all extra slots"), () => T("config.recover.tooltip", "Safely unequips all DTMAPI extra-slot items back to the backpack, with native overflow handling."), () => RecoverExtraSlots("config button"));
            menu.AddBoolOption(helper.ModManifest, () => T("config.safeRecovery.name", "Safe recovery"), () => T("config.safeRecovery.tooltip", "Requests safe unequip/recovery when the mod is disabled or missing."), () => config.SafeUnequipOnDisable, value => config.SafeUnequipOnDisable = value);
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

        private void RecoverExtraSlots(string reason)
        {
            if (equipmentSlotsApi == null || !config.SafeUnequipOnDisable)
                return;

            EquipmentSlotsRecoveryResult result = equipmentSlotsApi.RecoverExtraSlotItems(helper.ModManifest, reason);
            helper.Monitor.Log("MoreEquipmentSlots recovery success=" + result.Success + " recovered=" + result.RecoveredCount + " reason=" + reason + " message=" + result.Message, result.Success ? LogLevel.Info : LogLevel.Warn);
        }

        private void EquipFirstEmptySlot()
        {
            if (equipmentSlotsApi == null)
            {
                helper.Monitor.Log(T("mod.apiMissing", "Equipment Slots API is not available yet.") + " reason=equip button", LogLevel.Warn);
                return;
            }

            EquipmentSlotEquipResult result = equipmentSlotsApi.EquipExtraSlot(helper.ModManifest, string.Empty, config.QuickEquipItemId);
            helper.Monitor.Log("MoreEquipmentSlots equip success=" + result.Success + " slot=" + result.SlotId + " item=" + result.ItemId + " message=" + result.Message, result.Success ? LogLevel.Info : LogLevel.Warn);
        }

        private string BuildStatusText()
        {
            if (equipmentSlotsApi == null)
                return T("config.status.missing", "Equipment Slots API is not available.");

            EquipmentSlotsState state = equipmentSlotsApi.GetState(helper.ModManifest.UniqueID);
            string[] slots = equipmentSlotsApi.GetSlots(helper.ModManifest.UniqueID)
                .Select(slot => slot.SlotId + "=" + (slot.IsOccupied ? slot.DisplayName + "/" + slot.ItemId + (slot.IsApplied ? "[applied]" : "[stored]") : "empty"))
                .ToArray();
            string slotText = slots.Length == 0 ? "none" : string.Join("; ", slots);
            return string.Format(T("config.status", "Status={0}, extraSlots={1}, stored={2}, applied={3}, ui={4}, visualEffects={5}. Slots: {6}"), state.Status, state.ExtraAttributeSlots, state.StoredItemCount, state.AppliedItemCount, state.RuntimeUiHookInstalled, !state.ExtraSlotsAffectVisuals, slotText);
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
            config.ExtraAttributeSlots = Math.Max(0, Math.Min(12, config.ExtraAttributeSlots));
        }

        private string T(string key, string fallback) => helper.Translation.Get(key, fallback);

        [DataContract]
        public sealed class MoreEquipmentSlotsConfig
        {
            [DataMember] public bool Enabled { get; set; } = true;
            [DataMember] public int ExtraAttributeSlots { get; set; } = 3;
            [DataMember] public string QuickEquipItemId { get; set; } = "grandmas_button";
            [DataMember] public bool SafeUnequipOnDisable { get; set; } = true;
            [DataMember] public bool VerboseLogging { get; set; }
        }
    }
}
