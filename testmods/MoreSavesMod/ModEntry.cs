using System;
using System.Runtime.Serialization;
using DTMAPI.Abstractions;

namespace MoreSavesMod
{
    public sealed class ModEntry : DtmMod
    {
        private IDtmHelper helper = null!;
        private MoreSavesConfig config = new MoreSavesConfig();
        private ISaveSlotsApi? saveSlotsApi;
        private SaveSlotsRegisterResult? lastRegisterResult;

        public override void Entry(IDtmHelper helper)
        {
            this.helper = helper;
            config = helper.ReadConfig<MoreSavesConfig>();
            NormalizeConfig();
            RegisterConfigMenu();
            BindSaveSlotsApi("Entry");
            helper.Monitor.Log(T("mod.loaded", "DTMAPI More Saves loaded; official save UI and LocalSave paths own the expanded archive slots."));
        }

        private void RegisterConfigMenu()
        {
            IDtmConfigMenuApi? menu = helper.ModRegistry.GetApi<IDtmConfigMenuApi>("DTMAPI.ModConfigMenu");
            if (menu == null)
                return;

            menu.Register(helper.ModManifest, ResetConfig, SaveConfig);
            menu.SetDisplayName(helper.ModManifest, () => T("mod.name", "DTMAPI More Saves"));
            menu.AddSectionTitle(helper.ModManifest, () => T("config.section.slots", "Save slots"));
            menu.AddParagraph(helper.ModManifest, BuildStatusText);
            menu.AddBoolOption(helper.ModManifest, () => T("config.enabled.name", "Enabled"), () => T("config.enabled.tooltip", "Expands the official save/load UI slot count."), () => config.Enabled, value => config.Enabled = value);
            menu.AddNumberOption(helper.ModManifest, () => T("config.slotCount.name", "Slot count"), () => T("config.slotCount.tooltip", "Total official save slots to show. The first six remain vanilla-compatible."), () => config.SlotCount, value => config.SlotCount = (int)Math.Round(value), 6, 60, 1);
        }

        private void BindSaveSlotsApi(string reason)
        {
            saveSlotsApi = helper.ModRegistry.GetApi<ISaveSlotsApi>("DTMAPI.GameBridge.DolocTown");
            if (saveSlotsApi == null)
            {
                helper.Monitor.Log(T("mod.apiMissing", "Save Slots API is not available yet.") + " reason=" + reason, LogLevel.Warn);
                return;
            }

            lastRegisterResult = saveSlotsApi.RegisterSlots(helper.ModManifest, new SaveSlotsOptions
            {
                Enabled = config.Enabled,
                SlotCount = config.SlotCount,
                VerboseLogging = config.VerboseLogging
            });
            helper.Monitor.Log("MoreSaves API register success=" + lastRegisterResult.Success + " reason=" + reason + " requested=" + lastRegisterResult.RequestedSlotCount + " applied=" + lastRegisterResult.AppliedSlotCount + " message=" + lastRegisterResult.Message, lastRegisterResult.Success ? LogLevel.Info : LogLevel.Warn);
        }

        private string BuildStatusText()
        {
            if (saveSlotsApi == null)
                return T("config.status.missing", "Save Slots API is not available.");

            SaveSlotsState state = saveSlotsApi.GetState(helper.ModManifest.UniqueID);
            return string.Format(T("config.status", "Status={0}, native={1}, requested={2}, applied={3}. {4}"), state.Status, state.NativeSlotCount, state.RequestedSlotCount, state.AppliedSlotCount, state.LastMessage);
        }

        private void SaveConfig()
        {
            NormalizeConfig();
            helper.WriteConfig(config);
            BindSaveSlotsApi("config saved");
        }

        private void ResetConfig()
        {
            config = new MoreSavesConfig();
            NormalizeConfig();
        }

        private void NormalizeConfig()
        {
            config.SlotCount = Math.Max(6, Math.Min(60, config.SlotCount));
        }

        private string T(string key, string fallback) => helper.Translation.Get(key, fallback);

        [DataContract]
        public sealed class MoreSavesConfig
        {
            [DataMember] public bool Enabled { get; set; } = true;
            [DataMember] public int SlotCount { get; set; } = 12;
            [DataMember] public bool VerboseLogging { get; set; }
        }
    }
}
