using System;
using System.Collections.Generic;
using global::DTMAPI.Abstractions;

namespace DTMAPI.MoreEquipmentSlots
{
    public sealed class ModEntry : DtmMod, IDisposable
    {
        private IDtmHelper helper = null!;
        private MoreEquipmentSlotsConfig config =
            new MoreEquipmentSlotsConfig();
        private MoreEquipmentSlotsConfig appliedConfig =
            new MoreEquipmentSlotsConfig();
        private MoreEquipmentSlotsNativeRuntime runtime = null!;
        private bool runtimeCreated;
        private bool entryStarted;
        private bool saveLoadedSubscribed;
        private bool saveSavingSubscribed;
        private bool saveSavedSubscribed;
        private bool titleSubscribed;
        private bool disposed;

        public override void Entry(IDtmHelper helper)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(ModEntry));
            this.helper = helper ??
                throw new ArgumentNullException(nameof(helper));
            entryStarted = true;
            runtime = new MoreEquipmentSlotsNativeRuntime(
                helper.Monitor,
                helper.Config.GetConfigPath(
                    helper.ModManifest));
            runtimeCreated = true;
            try
            {
                runtime.ConfigureUiText(
                    T(
                        "ui.slot.hint",
                        "Extra equipment slot {0}"));
                config =
                    helper.ReadConfig<MoreEquipmentSlotsConfig>() ??
                    new MoreEquipmentSlotsConfig();
                runtime.Configure(config, "Entry");
                appliedConfig = CloneConfig(config);
                RegisterConfigMenu();
                helper.Events.Save.SaveLoaded += OnSaveLoaded;
                saveLoadedSubscribed = true;
                helper.Events.Save.SaveSaving += OnSaveSaving;
                saveSavingSubscribed = true;
                helper.Events.Save.SaveSaved += OnSaveSaved;
                saveSavedSubscribed = true;
                helper.Events.GameLoop.ReturnedToTitle +=
                    OnReturnedToTitle;
                titleSubscribed = true;
                helper.Monitor.Log(
                    T(
                        "mod.loaded",
                        "DTMAPI More Equipment Slots ProductNative runtime loaded.") +
                    " owner=" +
                    MoreEquipmentSlotsProductContract.HarmonyOwner +
                    " patches=" +
                    runtime.InstalledPatchCount +
                    " slotCount=" +
                    MoreEquipmentSlotsProductContract
                        .FixedSlotCount +
                    ".");
            }
            catch (Exception entryFailure)
            {
                var failures =
                    new List<Exception> { entryFailure };
                TryCleanup(UnsubscribeEvents, failures);
                if (runtimeCreated)
                {
                    TryCleanup(
                        () => runtime.DeactivateOwner(
                            "entry-failed"),
                        failures);
                }
                if (failures.Count == 1)
                    throw;
                throw new AggregateException(
                    "MoreEquipmentSlots Entry failed and rollback encountered cleanup failures.",
                    failures);
            }
        }

        public void Dispose()
        {
            DtmApiDeactivateOwner(
                "OwnerDeactivation");
        }

        internal void DtmApiPrepareOwnerDeactivation(
            string reason)
        {
            if (runtimeCreated)
            {
                runtime.PrepareOwnerDeactivation(
                    reason);
            }
        }

        internal void DtmApiDeactivateOwner(
            string reason)
        {
            if (disposed)
                return;
            var failures = new List<Exception>();
            if (entryStarted)
            {
                // Core removes owner-bound event roots before invoking
                // ProductNative deactivation. These flags reflect that
                // external removal and avoid mutating stale proxies.
                saveLoadedSubscribed = false;
                saveSavingSubscribed = false;
                saveSavedSubscribed = false;
                titleSubscribed = false;
            }
            if (runtimeCreated)
            {
                TryCleanup(
                    () => runtime.DeactivateOwner(
                        reason),
                    failures);
            }
            if (failures.Count > 0)
            {
                throw new AggregateException(
                    "MoreEquipmentSlots owner deactivation could not prove complete cleanup.",
                    failures);
            }
            disposed = true;
        }

        private void RegisterConfigMenu()
        {
            IDtmConfigMenuApi? menu =
                helper.ModRegistry.GetApi<IDtmConfigMenuApi>(
                    "DTMAPI.ModConfigMenu");
            if (menu == null)
            {
                helper.Monitor.Log(
                    "DTMAPI config menu API is not available.",
                    LogLevel.Warn);
                return;
            }

            menu.Register(
                helper.ModManifest,
                ResetConfig,
                SaveConfig);
            menu.SetDisplayName(
                helper.ModManifest,
                () => T(
                    "mod.name",
                    "DTMAPI More Equipment Slots"));
            menu.AddSectionTitle(
                helper.ModManifest,
                () => T(
                    "config.section.main",
                    "Extra equipment slots"));
            menu.AddParagraph(
                helper.ModManifest,
                () => runtime.StatusSummary);
            menu.AddBoolOption(
                helper.ModManifest,
                () => T(
                    "config.enabled.name",
                    "Enabled"),
                () => T(
                    "config.enabled.tooltip",
                    "Adds exactly three product-owned attribute equipment slots."),
                () => config.Enabled,
                value => config.Enabled = value);
            menu.AddBoolOption(
                helper.ModManifest,
                () => T(
                    "config.verbose.name",
                    "Verbose logs"),
                () => T(
                    "config.verbose.tooltip",
                    "Writes low-frequency lifecycle and protected-recovery diagnostics."),
                () => config.VerboseLogging,
                value => config.VerboseLogging = value);
        }

        private void SaveConfig()
        {
            MoreEquipmentSlotsConfig requested =
                CloneConfig(config);
            MoreEquipmentSlotsConfig previous =
                CloneConfig(appliedConfig);
            try
            {
                runtime.PrepareConfiguration(
                    requested,
                    "config save preflight");
            }
            catch
            {
                config = CloneConfig(previous);
                throw;
            }
            try
            {
                helper.WriteConfig(requested);
                runtime.Configure(
                    requested,
                    "config saved");
                appliedConfig = CloneConfig(requested);
                config = CloneConfig(requested);
            }
            catch (Exception applyFailure)
            {
                var failures =
                    new List<Exception>
                    {
                        applyFailure
                    };
                TryCleanup(
                    () => runtime.Configure(
                        previous,
                        "config save rollback"),
                    failures);
                TryCleanup(
                    () => helper.WriteConfig(previous),
                    failures);
                config = CloneConfig(previous);
                appliedConfig = CloneConfig(previous);
                if (failures.Count == 1)
                    throw;
                throw new AggregateException(
                    "MoreEquipmentSlots config save failed and rollback encountered cleanup failures.",
                    failures);
            }
        }

        private void ResetConfig() =>
            config = new MoreEquipmentSlotsConfig();

        private static MoreEquipmentSlotsConfig CloneConfig(
            MoreEquipmentSlotsConfig source) =>
            new MoreEquipmentSlotsConfig
            {
                Enabled = source?.Enabled ?? true,
                VerboseLogging =
                    source?.VerboseLogging ?? false
            };

        private void OnSaveLoaded(
            object sender,
            SaveLoadedEventArgs e) =>
            runtime.OnSaveLoaded(
                e.SaveSlot,
                e.IsNewGame);

        private void OnSaveSaving(
            object sender,
            SaveSavingEventArgs e) =>
            runtime.OnSaveSaving(e.SaveSlot);

        private void OnSaveSaved(
            object sender,
            SaveSavedEventArgs e) =>
            runtime.OnSaveSaved(e.SaveSlot);

        private void OnReturnedToTitle(
            object sender,
            ReturnedToTitleEventArgs e) =>
            runtime.ReturnedToTitle();

        private string T(
            string key,
            string fallback) =>
            helper.Translation.Get(key, fallback);

        private void UnsubscribeEvents()
        {
            if (saveLoadedSubscribed)
            {
                helper.Events.Save.SaveLoaded -= OnSaveLoaded;
                saveLoadedSubscribed = false;
            }
            if (saveSavingSubscribed)
            {
                helper.Events.Save.SaveSaving -= OnSaveSaving;
                saveSavingSubscribed = false;
            }
            if (saveSavedSubscribed)
            {
                helper.Events.Save.SaveSaved -= OnSaveSaved;
                saveSavedSubscribed = false;
            }
            if (titleSubscribed)
            {
                helper.Events.GameLoop.ReturnedToTitle -=
                    OnReturnedToTitle;
                titleSubscribed = false;
            }
        }

        private static void TryCleanup(
            Action cleanup,
            ICollection<Exception> failures)
        {
            try
            {
                cleanup();
            }
            catch (Exception ex)
            {
                failures.Add(ex);
            }
        }
    }
}
