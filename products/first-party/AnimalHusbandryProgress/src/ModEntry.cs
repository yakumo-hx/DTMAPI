using System;
using System.Collections.Generic;
using global::DTMAPI.Abstractions;

namespace Yuuka.DTMAPI.AnimalHusbandryProgress
{
    public sealed class ModEntry : DtmMod, IDisposable
    {
        private IDtmHelper helper = null!;
        private AnimalHusbandryConfig config = new AnimalHusbandryConfig();
        private AnimalHusbandryNativeRuntime nativeRuntime = null!;
        private bool entryStarted;
        private bool runtimeCreated;
        private bool updateSubscribed;
        private bool saveSubscribed;
        private bool titleSubscribed;
        private bool disposed;

        public override void Entry(IDtmHelper helper)
        {
            if (disposed) throw new ObjectDisposedException(nameof(ModEntry));
            this.helper = helper ?? throw new ArgumentNullException(nameof(helper));
            entryStarted = true;
            IItemDisplayNameApi itemNames = helper.ModRegistry.GetApi<IItemDisplayNameApi>("DTMAPI.GameBridge.DolocTown")
                ?? throw new InvalidOperationException("The shared GameBridge item display-name adapter is required.");
            nativeRuntime = new AnimalHusbandryNativeRuntime(helper.Monitor, itemNames);
            runtimeCreated = true;
            try
            {
                nativeRuntime.InstallHooksAtomically();
                config = helper.ReadConfig<AnimalHusbandryConfig>();
                config.Normalize();
                nativeRuntime.Configure(config);
                RegisterConfigMenu();
                helper.Events.GameLoop.UpdateTicked += OnUpdateTicked; updateSubscribed = true;
                helper.Events.Save.SaveLoaded += OnSaveLoaded; saveSubscribed = true;
                helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle; titleSubscribed = true;
                helper.Monitor.Log("AnimalHusbandryProgress ProductNative runtime ready owner=" + AnimalHusbandryHookInstaller.HarmonyOwner + " patches=" + nativeRuntime.InstalledPatchCount + ".");
            }
            catch (Exception entryFailure)
            {
                var failures = new List<Exception> { entryFailure };
                TryCleanup(UnsubscribeEvents, failures);
                if (runtimeCreated) TryCleanup(() => nativeRuntime.DeactivateOwner("entry-failed"), failures);
                if (failures.Count == 1) throw;
                throw new AggregateException("AnimalHusbandryProgress Entry failed and rollback encountered cleanup failures.", failures);
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            var failures = new List<Exception>();
            if (entryStarted)
            {
                updateSubscribed = false;
                saveSubscribed = false;
                titleSubscribed = false;
            }
            if (runtimeCreated) TryCleanup(() => nativeRuntime.DeactivateOwner("OwnerDeactivation"), failures);
            if (failures.Count > 0) throw new AggregateException("AnimalHusbandryProgress owner deactivation was incomplete.", failures);
            disposed = true;
        }

        private void RegisterConfigMenu()
        {
            IDtmConfigMenuApi? menu = helper.ModRegistry.GetApi<IDtmConfigMenuApi>("DTMAPI.ModConfigMenu");
            if (menu == null) { helper.Monitor.Log("DTMAPI config menu API is not available.", LogLevel.Warn); return; }
            menu.Register(helper.ModManifest, ResetConfig, () =>
            {
                config.Normalize();
                helper.WriteConfig(config);
                nativeRuntime.Configure(config);
                helper.Monitor.Log("AnimalHusbandryProgress config saved through DTMAPI menu.");
            });
            menu.SetDisplayName(helper.ModManifest, () => T("mod.name", helper.ModManifest.Name));
            menu.AddSectionTitle(helper.ModManifest, () => T("config.section.main", "Animal viewer"));
            menu.AddBoolOption(helper.ModManifest, () => T("config.enabled.name", "Enabled"), () => T("config.enabled.tooltip", "Show special-produce progress in the animal bell viewer."), () => config.Enabled, value => config.Enabled = value);
            menu.AddColorPresetOption(helper.ModManifest, () => T("config.colorPreset.name", "Color preset"), () => T("config.colorPreset.tooltip", "Pick the progress bar color directly."), () => config.ColorPreset, value => config.ColorPreset = value, BuildColorPresets());
            Func<bool> custom = () => config.ColorPreset.Equals("Custom", StringComparison.OrdinalIgnoreCase);
            menu.AddTextOption(helper.ModManifest, () => T("config.fillColor.name", "Fill color"), () => T("config.fillColor.tooltip", "Hex RGB color, for example FF942E."), () => config.ProgressColorHex, value => config.ProgressColorHex = value, custom, custom);
            menu.AddBoolOption(helper.ModManifest, () => T("config.verbose.name", "Verbose logs"), () => T("config.verbose.tooltip", "Write low-frequency viewer diagnostics."), () => config.VerboseLogging, value => config.VerboseLogging = value);
        }

        private IReadOnlyList<DtmColorPreset> BuildColorPresets() => new[]
        {
            new DtmColorPreset("Orange", T("config.color.orange", "Orange"), "FF942E"),
            new DtmColorPreset("Green", T("config.color.green", "Green"), "70C978"),
            new DtmColorPreset("Blue", T("config.color.blue", "Blue"), "65A6FF"),
            new DtmColorPreset("Pink", T("config.color.pink", "Pink"), "FF7AC8"),
            new DtmColorPreset("White", T("config.color.white", "White"), "F0F0F0"),
            new DtmColorPreset("Custom", T("config.color.custom", "Custom"), config.ProgressColorHex)
        };

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e) => nativeRuntime.Update();
        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e) => nativeRuntime.ResetBoundary("SaveLoaded");
        private void OnReturnedToTitle(object sender, EventArgs e) => nativeRuntime.ResetBoundary("ReturnedToTitle");
        private string T(string key, string fallback) => helper.Translation.Get(key, fallback);
        private void ResetConfig() { config = new AnimalHusbandryConfig(); config.Normalize(); }

        private void UnsubscribeEvents()
        {
            if (updateSubscribed) { helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked; updateSubscribed = false; }
            if (saveSubscribed) { helper.Events.Save.SaveLoaded -= OnSaveLoaded; saveSubscribed = false; }
            if (titleSubscribed) { helper.Events.GameLoop.ReturnedToTitle -= OnReturnedToTitle; titleSubscribed = false; }
        }

        private static void TryCleanup(Action cleanup, ICollection<Exception> failures) { try { cleanup(); } catch (Exception ex) { failures.Add(ex); } }
    }
}
