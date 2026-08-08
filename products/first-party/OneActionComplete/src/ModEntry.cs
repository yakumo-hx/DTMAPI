using System;
using System.Collections.Generic;
using global::DTMAPI.Abstractions;

namespace Yuuka.DTMAPI.OneActionComplete
{
    public sealed class ModEntry : DtmMod, IDisposable
    {
        private const string MenuRegistrationId = "Yuuka.DTMAPI.OneActionComplete.Menu";
        private IDtmHelper helper = null!;
        private OneActionConfig config = new OneActionConfig();
        private OneActionNativeRuntime nativeRuntime = null!;
        private IInputRegistration? menuRegistration;
        private bool entryStarted;
        private bool nativeRuntimeCreated;
        private bool keybindEventSubscribed;
        private bool saveEventSubscribed;
        private bool titleEventSubscribed;
        private bool disposed;

        public override void Entry(IDtmHelper helper)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(ModEntry));

            this.helper = helper ?? throw new ArgumentNullException(nameof(helper));
            entryStarted = true;
            nativeRuntime = new OneActionNativeRuntime(helper);
            nativeRuntimeCreated = true;
            try
            {
                nativeRuntime.InstallHooksAtomically();
                config = helper.ReadConfig<OneActionConfig>();
                NormalizeConfig();
                nativeRuntime.Configure(config);
                RegisterConfigMenu();
                menuRegistration = helper.Input.RegisterKeybind(MenuRegistrationId, DtmKeybindList.Parse(config.MenuKey), DtmInputScope.Gameplay);
                helper.Events.Input.KeybindPressed += OnKeybindPressed;
                keybindEventSubscribed = true;
                helper.Events.Save.SaveLoaded += OnSaveLoaded;
                saveEventSubscribed = true;
                helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
                titleEventSubscribed = true;
                helper.Monitor.Log("OneActionComplete product-native runtime ready owner=" + OneActionHookInstaller.HarmonyOwner + " patches=" + nativeRuntime.InstalledPatchCount + ".");
            }
            catch (Exception entryFailure)
            {
                var failures = new List<Exception> { entryFailure };
                TryCleanup(() =>
                {
                    menuRegistration?.Dispose();
                    menuRegistration = null;
                }, failures);
                TryCleanup(UnsubscribeEvents, failures);
                if (nativeRuntimeCreated)
                    TryCleanup(() => nativeRuntime.DeactivateOwner("entry-failed"), failures);
                if (failures.Count == 1)
                    throw;
                throw new AggregateException("OneActionComplete Entry failed and rollback encountered additional cleanup failures.", failures);
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;

            var failures = new List<Exception>();
            if (entryStarted)
            {
                // Core has already moved this owner to Deactivating. It owns removal of
                // platform event roots; owner-bound event proxies reject mutations here.
                keybindEventSubscribed = false;
                saveEventSubscribed = false;
                titleEventSubscribed = false;
                TryCleanup(() =>
                {
                    menuRegistration?.Dispose();
                    menuRegistration = null;
                }, failures);
            }
            if (nativeRuntimeCreated)
                TryCleanup(() => nativeRuntime.DeactivateOwner("OwnerDeactivation"), failures);
            if (failures.Count > 0)
                throw new AggregateException("OneActionComplete owner deactivation could not prove complete product-private cleanup.", failures);
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
                NormalizeConfig();
                helper.WriteConfig(config);
                menuRegistration?.Update(DtmKeybindList.Parse(config.MenuKey), DtmInputScope.Gameplay);
                nativeRuntime.Configure(config);
                helper.Monitor.Log("OneActionComplete config saved through DTMAPI menu.");
            });
            menu.SetDisplayName(helper.ModManifest, () => T("mod.name", helper.ModManifest.Name));
            menu.AddSectionTitle(helper.ModManifest, () => T("config.section.main", "One action complete"));
            menu.AddParagraph(helper.ModManifest, () => T("config.status.experimental", "Supports resource/tool-hit completion, the tree/ore/garbage/weeds wrong-tool matrix, and fuel/feeder native consume/fill. Vegetation/dandelion remains a native exception path."));
            menu.AddBoolOption(helper.ModManifest, () => T("config.enabled.name", "Enabled"), () => T("config.enabled.tooltip", "Master switch for all one-action behavior."), () => config.Enabled, value => config.Enabled = value);
            menu.AddBoolOption(helper.ModManifest, () => T("config.trees.name", "Trees"), () => T("config.trees.tooltip", "Finish trees after one normal tool hit."), () => config.CompleteTrees, value => config.CompleteTrees = value);
            menu.AddBoolOption(helper.ModManifest, () => T("config.ores.name", "Ores"), () => T("config.ores.tooltip", "Finish ore resources after one normal tool hit."), () => config.CompleteOres, value => config.CompleteOres = value);
            menu.AddBoolOption(helper.ModManifest, () => T("config.garbage.name", "Garbage"), () => T("config.garbage.tooltip", "Finish pickaxe garbage resources after one normal tool hit."), () => config.CompleteGarbage, value => config.CompleteGarbage = value);
            menu.AddBoolOption(helper.ModManifest, () => T("config.weeds.name", "Weeds"), () => T("config.weeds.tooltip", "Finish grass and weeds after one normal tool hit."), () => config.CompleteWeeds, value => config.CompleteWeeds = value);
            menu.AddBoolOption(helper.ModManifest, () => T("config.fuel.name", "Fuel machines"), () => T("config.fuel.tooltip", "Fill fuel machines after one direct add interaction."), () => config.CompleteMachineFuel, value => config.CompleteMachineFuel = value);
            menu.AddBoolOption(helper.ModManifest, () => T("config.feeders.name", "Feeders"), () => T("config.feeders.tooltip", "Fill feeders after one direct add interaction."), () => config.CompleteFeeder, value => config.CompleteFeeder = value);
            menu.AddBoolOption(helper.ModManifest, () => T("config.verbose.name", "Verbose logs"), () => T("config.verbose.tooltip", "Write low-frequency product diagnostics."), () => config.VerboseLogging, value => config.VerboseLogging = value);
            if (menu is IDtmConfigMenuKeybindDefaultsApi keybindDefaults)
                keybindDefaults.AddKeybindOption(helper.ModManifest, () => T("config.menuKey.name", "Menu key"), () => T("config.menuKey.tooltip", "Opens this DTMAPI config page."), () => config.MenuKey, value => config.MenuKey = value, () => "F11");
            else
                menu.AddKeybindOption(helper.ModManifest, () => T("config.menuKey.name", "Menu key"), () => T("config.menuKey.tooltip", "Opens this DTMAPI config page."), () => config.MenuKey, value => config.MenuKey = value);
        }

        private void OnKeybindPressed(object sender, KeybindPressedEventArgs e)
        {
            if (!e.OwnerId.Equals(helper.ModManifest.UniqueID, StringComparison.OrdinalIgnoreCase) ||
                !e.KeybindId.Equals(MenuRegistrationId, StringComparison.Ordinal))
                return;
            helper.UI.OpenConfigPage(helper.ModManifest.UniqueID);
            helper.Monitor.Log("OneActionComplete hotkey OpenConfig OK key=" + e.TriggerButton);
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            nativeRuntime.ResetBoundary("SaveLoaded");
            helper.Monitor.Log("OneActionComplete SaveLoaded restore boundary OK slot=" + (e.SaveSlot?.ToString() ?? "unknown"));
        }

        private void OnReturnedToTitle(object sender, EventArgs e)
        {
            nativeRuntime.ResetBoundary("ReturnedToTitle");
            helper.Monitor.Log("OneActionComplete ReturnedToTitle restore boundary OK.");
        }

        private void UnsubscribeEvents()
        {
            if (keybindEventSubscribed)
            {
                helper.Events.Input.KeybindPressed -= OnKeybindPressed;
                keybindEventSubscribed = false;
            }
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
            config = new OneActionConfig();
            NormalizeConfig();
        }

        private void NormalizeConfig()
        {
            config.MenuKey = (config.MenuKey ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(config.MenuKey))
                config.MenuKey = "None";
        }

        private static void TryCleanup(Action cleanup, ICollection<Exception> failures)
        {
            try { cleanup(); }
            catch (Exception ex) { failures.Add(ex); }
        }
    }
}
