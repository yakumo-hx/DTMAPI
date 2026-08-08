using System;
using System.Collections.Generic;
using global::DTMAPI.Abstractions;

namespace DTMAPI.MoreSaves
{
    public sealed class ModEntry : DtmMod, IDisposable
    {
        private IDtmHelper helper = null!;
        private MoreSavesConfig config = new MoreSavesConfig();
        private MoreSavesNativeRuntime nativeRuntime = null!;
        private bool entryStarted;
        private bool runtimeCreated;
        private bool updateSubscribed;
        private bool saveSubscribed;
        private bool titleSubscribed;
        private bool disposed;

        public override void Entry(IDtmHelper helper)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(ModEntry));
            this.helper = helper ?? throw new ArgumentNullException(nameof(helper));
            entryStarted = true;
            nativeRuntime = new MoreSavesNativeRuntime(helper.Monitor, retryDemandChanged: SetUpdateSubscription);
            runtimeCreated = true;
            try
            {
                config = helper.ReadConfig<MoreSavesConfig>();
                config.Normalize();
                nativeRuntime.Configure(config, "Entry");
                RegisterConfigMenu();
                helper.Events.Save.SaveLoaded += OnSaveLoaded; saveSubscribed = true;
                helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle; titleSubscribed = true;
                helper.Monitor.Log(T("mod.loaded", "DTMAPI More Saves ProductNative runtime loaded.") +
                    " owner=" + MoreSavesNativeOwnerCoordinator.ProductOwner +
                    " patches=" + nativeRuntime.InstalledPatchCount +
                    " " + nativeRuntime.StatusSummary);
            }
            catch (Exception entryFailure)
            {
                var failures = new List<Exception> { entryFailure };
                TryCleanup(UnsubscribeEvents, failures);
                if (runtimeCreated)
                    TryCleanup(() => nativeRuntime.DeactivateOwner("entry-failed"), failures);
                if (failures.Count == 1)
                    throw;
                throw new AggregateException("MoreSaves Entry failed and rollback encountered cleanup failures.", failures);
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;
            var failures = new List<Exception>();
            if (entryStarted)
            {
                updateSubscribed = false;
                saveSubscribed = false;
                titleSubscribed = false;
            }
            if (runtimeCreated)
                TryCleanup(() => nativeRuntime.DeactivateOwner("OwnerDeactivation"), failures);
            if (failures.Count > 0)
                throw new AggregateException("MoreSaves owner deactivation was incomplete.", failures);
            disposed = true;
        }

        private void RegisterConfigMenu()
        {
            IDtmConfigMenuApi? menu = helper.ModRegistry.GetApi<IDtmConfigMenuApi>("DTMAPI.ModConfigMenu");
            if (menu == null)
            {
                helper.Monitor.Log("DTMAPI config menu API is not available.", LogLevel.Warn);
                return;
            }
            menu.Register(helper.ModManifest, ResetConfig, SaveConfig);
            menu.SetDisplayName(helper.ModManifest, () => T("mod.name", "DTMAPI More Saves"));
            menu.AddSectionTitle(helper.ModManifest, () => T("config.section.slots", "Save slots"));
            menu.AddParagraph(helper.ModManifest, () => string.Format(T("config.status", "Status: {0}"), nativeRuntime.StatusSummary));
            menu.AddBoolOption(helper.ModManifest, () => T("config.enabled.name", "Enabled"), () => T("config.enabled.tooltip", "Expands the official save/load UI to 12 total slots."), () => config.Enabled, value => config.Enabled = value);
            menu.AddBoolOption(helper.ModManifest, () => T("config.verbose.name", "Verbose logs"), () => T("config.verbose.tooltip", "Write low-frequency archive-count lifecycle diagnostics."), () => config.VerboseLogging, value => config.VerboseLogging = value);
        }

        private void SaveConfig()
        {
            config.Normalize();
            helper.WriteConfig(config);
            nativeRuntime.Configure(config, "config saved");
        }

        private void ResetConfig()
        {
            config = new MoreSavesConfig();
            config.Normalize();
        }

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e) => nativeRuntime.Update();
        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e) => nativeRuntime.ResetBoundary("SaveLoaded");
        private void OnReturnedToTitle(object sender, EventArgs e) => nativeRuntime.ResetBoundary("ReturnedToTitle");
        private string T(string key, string fallback) => helper.Translation.Get(key, fallback);

        private void SetUpdateSubscription(bool active)
        {
            if (active == updateSubscribed)
                return;
            if (active)
                helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            else
                helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
            updateSubscribed = active;
        }

        private void UnsubscribeEvents()
        {
            if (updateSubscribed) { helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked; updateSubscribed = false; }
            if (saveSubscribed) { helper.Events.Save.SaveLoaded -= OnSaveLoaded; saveSubscribed = false; }
            if (titleSubscribed) { helper.Events.GameLoop.ReturnedToTitle -= OnReturnedToTitle; titleSubscribed = false; }
        }

        private static void TryCleanup(Action cleanup, ICollection<Exception> failures)
        {
            try { cleanup(); }
            catch (Exception ex) { failures.Add(ex); }
        }
    }
}
