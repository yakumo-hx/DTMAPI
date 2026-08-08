using System;
using System.Collections.Generic;
using System.Globalization;
using global::DTMAPI.Abstractions;

namespace DTMAPI.StrongPlantingGun
{
    public sealed class ModEntry : DtmMod, IDisposable
    {
        private IDtmHelper helper = null!;
        private StrongPlantingGunConfig config =
            new StrongPlantingGunConfig();
        private StrongPlantingGunNativeRuntime nativeRuntime = null!;
        private bool runtimeCreated;
        private bool saveSubscribed;
        private bool titleSubscribed;
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
                config = helper.ReadConfig<StrongPlantingGunConfig>() ??
                    new StrongPlantingGunConfig();
                nativeRuntime.Configure(config, "Entry");
                RegisterConfigMenu();
                helper.Events.Save.SaveLoaded += OnSaveLoaded;
                saveSubscribed = true;
                helper.Events.GameLoop.ReturnedToTitle +=
                    OnReturnedToTitle;
                titleSubscribed = true;
                helper.Monitor.Log(
                    T(
                        "mod.loaded",
                        "DTMAPI Strong Planting Gun loaded; the official farming gun has fixed seed, film, and fertilizer slots.") +
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

        public void Dispose()
        {
            if (disposed)
                return;

            var failures = new List<Exception>();
            // Core removes owner-bound event roots before invoking product
            // disposal; flags are cleared so this product keeps no roots.
            saveSubscribed = false;
            titleSubscribed = false;
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
                    "DTMAPI Strong Planting Gun"));
            menu.AddSectionTitle(
                helper.ModManifest,
                () => T(
                    "config.section.main",
                    "Strong planting gun"));
            menu.AddParagraph(
                helper.ModManifest,
                BuildStatusText);
            menu.AddParagraph(
                helper.ModManifest,
                () => T(
                    "config.contract",
                    "Fixed contract: three slots for seed, film, and fertilizer. Water and range expansion are not supported."));
            menu.AddBoolOption(
                helper.ModManifest,
                () => T("config.enabled.name", "Enabled"),
                () => T(
                    "config.enabled.tooltip",
                    "Enable the fixed three-slot farming gun policy."),
                () => config.Enabled,
                value => config.Enabled = value);
            menu.AddBoolOption(
                helper.ModManifest,
                () => T("config.seeds.name", "Use seeds"),
                () => T(
                    "config.seeds.tooltip",
                    "Plant seeds using official basin checks."),
                () => config.IncludeSeeds,
                value => config.IncludeSeeds = value);
            menu.AddBoolOption(
                helper.ModManifest,
                () => T("config.films.name", "Use films"),
                () => T(
                    "config.films.tooltip",
                    "Apply films using official basin checks."),
                () => config.IncludeFilms,
                value => config.IncludeFilms = value);
            menu.AddBoolOption(
                helper.ModManifest,
                () => T(
                    "config.fertilizers.name",
                    "Use fertilizers"),
                () => T(
                    "config.fertilizers.tooltip",
                    "Apply fertilizers using official basin checks."),
                () => config.IncludeFertilizers,
                value => config.IncludeFertilizers = value);
            menu.AddBoolOption(
                helper.ModManifest,
                () => T(
                    "config.verbose.name",
                    "Verbose logs"),
                () => T(
                    "config.verbose.tooltip",
                    "Write bounded Strong Planting Gun observations."),
                () => config.VerboseLogging,
                value => config.VerboseLogging = value);
        }

        private string BuildStatusText() =>
            string.Format(
                CultureInfo.InvariantCulture,
                T(
                    "config.status",
                    "Status={0}, hooks={1}/5, expanded={2}, consumed={3}. {4}"),
                nativeRuntime.Status,
                nativeRuntime.InstalledPatchCount,
                nativeRuntime.ExpandedGunCount,
                nativeRuntime.LastConsumedItemCount,
                nativeRuntime.LastMessage);

        private void SaveConfig()
        {
            helper.WriteConfig(config);
            nativeRuntime.Configure(config, "config saved");
        }

        private void ResetConfig() =>
            config = new StrongPlantingGunConfig();

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            int prepared =
                nativeRuntime.ConfigureLoadedSaveBoundary(
                    config,
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

        private void UnsubscribeEvents()
        {
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
