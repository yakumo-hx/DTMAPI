using System;
using System.Collections.Generic;
using System.Globalization;
using global::DTMAPI.Abstractions;

namespace DTMAPI.StrongPlantingGun
{
    public sealed class ModEntry : DtmMod, IDisposable
    {
        private IDtmHelper helper = null!;
        private StrongPlantingGunNativeRuntime nativeRuntime = null!;
        private StrongPlantingGunConfig config = new StrongPlantingGunConfig();
        private bool runtimeCreated;
        private bool saveSubscribed;
        private bool titleSubscribed;
        private bool tickSubscribed;
        private bool disposed;

        public override void Entry(IDtmHelper helper)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(ModEntry));
            this.helper = helper ??
                throw new ArgumentNullException(nameof(helper));
            nativeRuntime =
                new StrongPlantingGunNativeRuntime(helper.Monitor);
            runtimeCreated = true;

            try
            {
                config = helper.ReadConfig<StrongPlantingGunConfig>();
                config.Range = StrongPlantingGunConfig.Normalize(config.Range);
                nativeRuntime.SetBatchContext(helper.GetRequiredService<IDtmRuntimeContext>("0.7.0", "0.7.0"));
                nativeRuntime.ApplyRange(config.Range);
                nativeRuntime.Configure("Entry");
                RegisterConfigMenu();
                helper.Events.Save.SaveLoaded += OnSaveLoaded;
                saveSubscribed = true;
                helper.Events.GameLoop.ReturnedToTitle +=
                    OnReturnedToTitle;
                titleSubscribed = true;
                helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
                tickSubscribed = true;
                helper.Monitor.Log(
                    T(
                        "mod.loaded",
                        "DTMAPI Strong Farming Gun loaded; native seed, fertilizer, film and water actions use a scoped delayed batch.") +
                    " owner=" +
                    StrongPlantingGunProductContract.HarmonyOwner +
                    " hooks=" +
                    nativeRuntime.InstalledPatchCount +
                    ".");
            }
            catch (Exception entryFailure)
            {
                var failures = new List<Exception>
                {
                    entryFailure
                };
                TryCleanup(UnsubscribeEvents, failures);
                TryCleanup(
                    () => nativeRuntime.DeactivateOwner(
                        "entry-failed"),
                    failures);
                if (failures.Count == 1)
                    throw;
                throw new AggregateException(
                    "StrongPlantingGun entry failed and rollback encountered additional cleanup failures.",
                    failures);
            }
        }

        private void RegisterConfigMenu()
        {
            IDtmConfigMenuApi? menu = helper.ModRegistry.GetApi<IDtmConfigMenuApi>("DTMAPI.ModConfigMenu");
            if (menu == null) return;
            menu.Register(helper.ModManifest, () => config = new StrongPlantingGunConfig(), SaveConfig);
            menu.SetDisplayName(helper.ModManifest, () => T("mod.name", "DTMAPI Strong Farming Gun"));
            menu.AddNumberOption(helper.ModManifest,
                () => T("config.range.name", "Range"),
                () => T("config.range.tooltip", "Square side length, 7–15 tiles. Uses the native origin and facing."),
                () => config.Range, value => config.Range = StrongPlantingGunConfig.Normalize(value),
                StrongPlantingGunConfig.DefaultRange, StrongPlantingGunConfig.MaximumRange, 1);
        }

        private void SaveConfig()
        {
            config.Range = StrongPlantingGunConfig.Normalize(config.Range);
            helper.WriteConfig(config);
            nativeRuntime.ApplyRange(config.Range);
        }

        public void Dispose()
        {
            if (disposed)
                return;

            var failures = new List<Exception>();
            // Core removes owner-bound event roots before invoking product
            // disposal; flags are cleared so this product keeps no roots.
            saveSubscribed = false;
            titleSubscribed = false;
            tickSubscribed = false;
            if (runtimeCreated)
            {
                TryCleanup(
                    () => nativeRuntime.DeactivateOwner(
                        "OwnerDeactivation"),
                    failures);
            }
            if (failures.Count > 0)
            {
                throw new AggregateException(
                    "StrongPlantingGun owner deactivation could not prove complete ProductNative cleanup.",
                    failures);
            }

            disposed = true;
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            int prepared =
                nativeRuntime.ConfigureLoadedSaveBoundary(
                    "SaveLoaded");
            helper.Monitor.Log(
                "StrongPlantingGun SaveLoaded boundary ready slot=" +
                (e.SaveSlot?.ToString(
                    CultureInfo.InvariantCulture) ??
                 "unknown") +
                " hooks=" +
                nativeRuntime.InstalledPatchCount +
                " loadedGuns=" +
                prepared.ToString(
                    CultureInfo.InvariantCulture) +
                ".");
        }

        private void OnReturnedToTitle(object sender, EventArgs e)
        {
            nativeRuntime.SuspendForTitle("ReturnedToTitle");
            helper.Monitor.Log(
                "StrongPlantingGun ReturnedToTitle restored native capacity and removed the ProductNative owner.");
        }

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e) => nativeRuntime.ObserveToolBatch();

        private void UnsubscribeEvents()
        {
            if (tickSubscribed)
            {
                helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
                tickSubscribed = false;
            }
            if (saveSubscribed)
            {
                helper.Events.Save.SaveLoaded -= OnSaveLoaded;
                saveSubscribed = false;
            }
            if (titleSubscribed)
            {
                helper.Events.GameLoop.ReturnedToTitle -=
                    OnReturnedToTitle;
                titleSubscribed = false;
            }
        }

        private string T(string key, string fallback) =>
            helper.Translation.Get(key, fallback);

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
