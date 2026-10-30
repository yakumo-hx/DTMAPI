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
        private MoreSavesPagingRuntime? paging;
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
                nativeRuntime.Configure(config, "Entry");
                paging = new MoreSavesPagingRuntime(nativeRuntime, helper.Monitor, T);
                paging.Install();
                RegisterConfigMenu();
                helper.Events.Save.SaveLoaded += OnSaveLoaded; saveSubscribed = true;
                helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle; titleSubscribed = true;
                helper.Monitor.Log(T("mod.loaded", "DTMAPI More Saves ProductNative runtime loaded.") +
                    " owner=" + MoreSavesNativeOwnerCoordinator.ProductOwner +
                    " patches=" + paging.InstalledPatchCount +
                    " " + nativeRuntime.StatusSummary);
            }
            catch (Exception entryFailure)
            {
                var failures = new List<Exception> { entryFailure };
                TryCleanup(UnsubscribeEvents, failures);
                TryCleanup(() => paging?.Suspend(), failures);
                if (runtimeCreated)
                    TryCleanup(() => nativeRuntime.DeactivateOwner("entry-failed"), failures);
                if (!nativeRuntime.LeaseHeld) TryCleanup(() => paging?.RestoreAndDispose(), failures);
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
            // Core has already closed owner-bound event access before Dispose. Its owner cleanup
            // removes these subscriptions; entry rollback above is the still-active removal path.
            if (entryStarted) { updateSubscribed = false; saveSubscribed = false; titleSubscribed = false; }
            TryCleanup(() => paging?.Suspend(), failures);
            if (runtimeCreated)
                TryCleanup(() => nativeRuntime.DeactivateOwner("OwnerDeactivation"), failures);
            if (!nativeRuntime.LeaseHeld) TryCleanup(() => paging?.RestoreAndDispose(), failures);
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
            menu.AddBoolOption(helper.ModManifest, () => T("config.verbose.name", "Verbose logs"), () => T("config.verbose.tooltip", "Write low-frequency archive-count lifecycle diagnostics."), () => config.VerboseLogging, value => config.VerboseLogging = value);
        }

        private void SaveConfig()
        {
            helper.WriteConfig(config);
            nativeRuntime.Configure(config, "config saved");
        }

        private void ResetConfig()
        {
            config = new MoreSavesConfig();
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
