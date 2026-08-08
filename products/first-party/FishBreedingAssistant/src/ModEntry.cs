using System;
using System.Collections.Generic;
using global::DTMAPI.Abstractions;

namespace Yuuka.DTMAPI.FishBreedingAssistant
{
    public sealed class ModEntry : DtmMod, IDisposable
    {
        private IDtmHelper helper = null!;
        private FishBreedingConfig config = new FishBreedingConfig();
        private FishBreedingNativeRuntime nativeRuntime = null!;
        private bool entryStarted;
        private bool nativeRuntimeCreated;
        private bool saveEventSubscribed;
        private bool titleEventSubscribed;
        private bool disposed;

        public override void Entry(IDtmHelper helper)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(ModEntry));
            this.helper = helper ?? throw new ArgumentNullException(nameof(helper));
            entryStarted = true;
            IItemDisplayNameApi itemDisplayNames = helper.ModRegistry.GetApi<IItemDisplayNameApi>("DTMAPI.GameBridge.DolocTown")
                ?? throw new InvalidOperationException("The shared GameBridge item display-name adapter is required.");
            nativeRuntime = new FishBreedingNativeRuntime(helper.Monitor, itemDisplayNames);
            nativeRuntimeCreated = true;
            try
            {
                nativeRuntime.InstallHooksAtomically();
                config = helper.ReadConfig<FishBreedingConfig>();
                config.Normalize();
                nativeRuntime.Configure(config);
                RegisterConfigMenu();
                helper.Events.Save.SaveLoaded += OnSaveLoaded;
                saveEventSubscribed = true;
                helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
                titleEventSubscribed = true;
                helper.Monitor.Log("FishBreedingAssistant ProductNative runtime ready owner=" + FishBreedingHookInstaller.HarmonyOwner + " patches=" + nativeRuntime.InstalledPatchCount + ".");
            }
            catch (Exception entryFailure)
            {
                var failures = new List<Exception> { entryFailure };
                TryCleanup(UnsubscribeEvents, failures);
                if (nativeRuntimeCreated)
                    TryCleanup(() => nativeRuntime.DeactivateOwner("entry-failed"), failures);
                if (failures.Count == 1)
                    throw;
                throw new AggregateException("FishBreedingAssistant Entry failed and rollback encountered additional cleanup failures.", failures);
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;
            var failures = new List<Exception>();
            if (entryStarted)
            {
                // Core owns platform event-root removal after BeginDeactivation.
                saveEventSubscribed = false;
                titleEventSubscribed = false;
            }
            if (nativeRuntimeCreated)
                TryCleanup(() => nativeRuntime.DeactivateOwner("OwnerDeactivation"), failures);
            if (failures.Count > 0)
                throw new AggregateException("FishBreedingAssistant owner deactivation could not prove complete product-private cleanup.", failures);
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

            menu.Register(helper.ModManifest, ResetConfig, () =>
            {
                config.Normalize();
                helper.WriteConfig(config);
                nativeRuntime.Configure(config);
                helper.Monitor.Log("FishBreedingAssistant config saved through DTMAPI menu.");
            });
            menu.SetDisplayName(helper.ModManifest, () => T("mod.name", helper.ModManifest.Name));
            menu.AddSectionTitle(helper.ModManifest, () => T("config.section.main", "Fish roe tooltip"));
            menu.AddBoolOption(helper.ModManifest, () => T("config.enabled.name", "Enabled"), () => T("config.enabled.tooltip", "Master switch for fish roe display."), () => config.Enabled, value => config.Enabled = value);
            menu.AddParagraph(helper.ModManifest, () => T("config.scope", "Shows the parent fish name in the roe title only; incubation/growth details stay hidden."));
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            nativeRuntime.ResetBoundary("SaveLoaded");
            helper.Monitor.Log("FishBreedingAssistant SaveLoaded tooltip boundary OK slot=" + (e.SaveSlot?.ToString() ?? "unknown") + ".");
        }

        private void OnReturnedToTitle(object sender, EventArgs e)
        {
            nativeRuntime.ResetBoundary("ReturnedToTitle");
            helper.Monitor.Log("FishBreedingAssistant ReturnedToTitle tooltip boundary OK.");
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

        private string T(string key, string fallback) => helper.Translation.Get(key, fallback);

        private void ResetConfig()
        {
            config = new FishBreedingConfig();
            config.Normalize();
        }

        private static void TryCleanup(Action cleanup, ICollection<Exception> failures)
        {
            try { cleanup(); }
            catch (Exception ex) { failures.Add(ex); }
        }
    }
}
