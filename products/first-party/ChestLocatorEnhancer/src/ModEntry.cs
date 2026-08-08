using System;
using System.Collections.Generic;
using System.Globalization;
using global::DTMAPI.Abstractions;

namespace DTMAPI.ChestLocatorEnhancer
{
    public sealed class ModEntry : DtmMod, IDisposable
    {
        private IDtmHelper helper = null!;
        private ChestLocatorEnhancerConfig config =
            new ChestLocatorEnhancerConfig();
        private ChestLocatorEnhancerNativeRuntime nativeRuntime = null!;
        private bool entryStarted;
        private bool runtimeCreated;
        private bool saveEventSubscribed;
        private bool titleEventSubscribed;
        private bool disposed;

        public override void Entry(IDtmHelper helper)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(ModEntry));
            this.helper =
                helper ?? throw new ArgumentNullException(nameof(helper));
            entryStarted = true;
            nativeRuntime =
                new ChestLocatorEnhancerNativeRuntime(helper.Monitor);
            runtimeCreated = true;
            try
            {
                config =
                    helper.ReadConfig<ChestLocatorEnhancerConfig>() ??
                    new ChestLocatorEnhancerConfig();
                nativeRuntime.Configure(config, "Entry");
                RegisterConfigMenu();
                helper.Events.Save.SaveLoaded += OnSaveLoaded;
                saveEventSubscribed = true;
                helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
                titleEventSubscribed = true;
                helper.Monitor.Log(
                    T(
                        "mod.loaded",
                        "DTMAPI Chest Locator Enhancer loaded; locator-marked containers can contribute materials across farm buildings.") +
                    " owner=" +
                    ChestLocatorEnhancerHookInstaller.HarmonyOwner +
                    " patches=" +
                    nativeRuntime.InstalledPatchCount +
                    ".");
            }
            catch (Exception entryFailure)
            {
                var failures = new List<Exception> { entryFailure };
                TryCleanup(UnsubscribeEvents, failures);
                if (runtimeCreated)
                {
                    TryCleanup(
                        () => nativeRuntime.DeactivateOwner("entry-failed"),
                        failures);
                }

                if (failures.Count == 1)
                    throw;
                throw new AggregateException(
                    "ChestLocatorEnhancer Entry failed and rollback encountered additional cleanup failures.",
                    failures);
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;

            var failures = new List<Exception>();
            if (entryStarted)
            {
                // Core owns platform event-root removal once this owner is
                // Deactivating; owner-bound event proxies reject mutations here.
                saveEventSubscribed = false;
                titleEventSubscribed = false;
            }
            if (runtimeCreated)
            {
                TryCleanup(
                    () => nativeRuntime.DeactivateOwner("OwnerDeactivation"),
                    failures);
            }
            if (failures.Count > 0)
            {
                throw new AggregateException(
                    "ChestLocatorEnhancer owner deactivation could not prove complete product-private cleanup.",
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
                () =>
                {
                    helper.WriteConfig(config);
                    nativeRuntime.Configure(config, "config saved");
                    helper.Monitor.Log(
                        "ChestLocatorEnhancer config saved through DTMAPI menu.");
                });
            menu.SetDisplayName(
                helper.ModManifest,
                () => T(
                    "mod.name",
                    "DTMAPI Chest Locator Enhancer"));
            menu.AddSectionTitle(
                helper.ModManifest,
                () => T(
                    "config.section.main",
                    "Chest locator"));
            menu.AddParagraph(
                helper.ModManifest,
                BuildStatusText);
            menu.AddBoolOption(
                helper.ModManifest,
                () => T("config.enabled.name", "Enabled"),
                () => T(
                    "config.enabled.tooltip",
                    "Extends official shared-container material lookup across farm building rooms."),
                () => config.Enabled,
                value => config.Enabled = value);
            menu.AddBoolOption(
                helper.ModManifest,
                () => T(
                    "config.cases.name",
                    "Shared chests"),
                () => T(
                    "config.cases.tooltip",
                    "Includes Case inventories marked shared by the official locator."),
                () => config.IncludeSharedCases,
                value => config.IncludeSharedCases = value);
            menu.AddBoolOption(
                helper.ModManifest,
                () => T(
                    "config.shelves.name",
                    "Storage shelves"),
                () => T(
                    "config.shelves.tooltip",
                    "Includes ItemBox inventories inside shared storage shelves."),
                () => config.IncludeSharedStorageShelfBoxes,
                value => config.IncludeSharedStorageShelfBoxes = value);
            menu.AddBoolOption(
                helper.ModManifest,
                () => T(
                    "config.autoUseBox.name",
                    "Respect auto-use boxes"),
                () => T(
                    "config.autoUseBox.tooltip",
                    "Keeps the game's auto-use box setting for shared storage shelves."),
                () => config.RespectNativeAutoUseBoxSetting,
                value => config.RespectNativeAutoUseBoxSetting = value);
        }

        private string BuildStatusText()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                T(
                    "config.status",
                    "Status={0}, hook={1}, appended={2}, cases={3}, shelfBoxes={4}. {5}"),
                nativeRuntime.Status,
                nativeRuntime.IsHookInstalled,
                nativeRuntime.LastAppendedInventoryCount,
                nativeRuntime.LastSharedCaseCount,
                nativeRuntime.LastSharedStorageBoxCount,
                nativeRuntime.LastMessage);
        }

        private void OnSaveLoaded(
            object sender,
            SaveLoadedEventArgs e)
        {
            nativeRuntime.ResetBoundary("SaveLoaded");
            helper.Monitor.Log(
                "ChestLocatorEnhancer SaveLoaded boundary reset slot=" +
                (e.SaveSlot?.ToString(
                    CultureInfo.InvariantCulture) ??
                 "unknown") +
                ".");
        }

        private void OnReturnedToTitle(
            object sender,
            EventArgs e)
        {
            nativeRuntime.ResetBoundary("ReturnedToTitle");
            helper.Monitor.Log(
                "ChestLocatorEnhancer ReturnedToTitle boundary reset.");
        }

        private void UnsubscribeEvents()
        {
            if (saveEventSubscribed)
            {
                helper.Events.Save.SaveLoaded -= OnSaveLoaded;
                saveEventSubscribed = false;
            }
            if (titleEventSubscribed)
            {
                helper.Events.GameLoop.ReturnedToTitle -= OnReturnedToTitle;
                titleEventSubscribed = false;
            }
        }

        private void ResetConfig()
        {
            config = new ChestLocatorEnhancerConfig();
        }

        private string T(
            string key,
            string fallback) =>
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
