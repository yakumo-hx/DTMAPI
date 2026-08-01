using System;
using System.Collections.Generic;
using global::DTMAPI.Abstractions;

namespace Yuuka.DTMAPI.ActionSpeed
{
    public sealed class ModEntry : DtmMod, IDisposable
    {
        private const string MenuRegistrationId = "Yuuka.DTMAPI.ActionSpeed.Menu";
        private IDtmHelper helper = null!;
        private ActionSpeedConfig config = new ActionSpeedConfig();
        private ActionSpeedNativeRuntime nativeRuntime = null!;
        private IInputRegistration? menuRegistration;
        private bool entryStarted;
        private bool nativeRuntimeCreated;
        private bool updateSubscribed;
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
            nativeRuntime = new ActionSpeedNativeRuntime(helper);
            nativeRuntimeCreated = true;
            try
            {
                nativeRuntime.InstallHooksAtomically();
                config = helper.ReadConfig<ActionSpeedConfig>();
                config.Normalize();
                nativeRuntime.Configure(config);
                RegisterConfigMenu();
                menuRegistration = helper.Input.RegisterKeybind(MenuRegistrationId, DtmKeybindList.Parse(config.MenuKey), DtmInputScope.Gameplay);
                helper.Events.Input.KeybindPressed += OnKeybindPressed;
                keybindEventSubscribed = true;
                helper.Events.Save.SaveLoaded += OnSaveLoaded;
                saveEventSubscribed = true;
                helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
                titleEventSubscribed = true;
                SetUpdateSubscription(config.Enabled && config.AutoFillBottle);
                helper.Monitor.Log("ActionSpeed product-native runtime ready owner=" + ActionSpeedHookInstaller.HarmonyOwner + " patches=" + nativeRuntime.InstalledPatchCount + ".");
            }
            catch (Exception entryFailure)
            {
                var failures = new List<Exception> { entryFailure };
                TryCleanup(UnsubscribeEvents, failures);
                TryCleanup(() =>
                {
                    menuRegistration?.Dispose();
                    menuRegistration = null;
                }, failures);
                if (nativeRuntimeCreated)
                    TryCleanup(() => nativeRuntime.DeactivateOwner("entry-failed"), failures);
                if (failures.Count == 1)
                    throw;
                throw new AggregateException("ActionSpeed Entry failed and rollback encountered additional cleanup failures.", failures);
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
                updateSubscribed = false;
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
                throw new AggregateException("ActionSpeed owner deactivation could not prove complete product-private cleanup.", failures);
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
                menuRegistration?.Update(DtmKeybindList.Parse(config.MenuKey), DtmInputScope.Gameplay);
                nativeRuntime.Configure(config);
                SetUpdateSubscription(config.Enabled && config.AutoFillBottle);
                helper.Monitor.Log("ActionSpeed config saved through DTMAPI menu.");
            });
            menu.SetDisplayName(helper.ModManifest, () => T("mod.name", helper.ModManifest.Name));
            menu.AddSectionTitle(helper.ModManifest, () => T("config.section.main", "Action speed"));
            menu.AddParagraph(helper.ModManifest, () => T("config.status.toolVerified", "Tool, interaction, eat/drink, continuous-use, bottle, plant, harvest, animal, and electric paths use the product-owned native Hook set."));
            menu.AddBoolOption(helper.ModManifest, () => T("config.enabled.name", "Enabled"), () => T("config.enabled.tooltip", "Master switch for all ActionSpeed behavior."), () => config.Enabled, value => config.Enabled = value);
            menu.AddInlineBoolNumberOption(helper.ModManifest, () => T("config.toolSpeed.name", "Tool animation speed"), () => T("config.toolSpeed.tooltip", "Axe, pickaxe, and sickle animation speed."), () => config.ToolSpeedEnabled, value => config.ToolSpeedEnabled = value, () => config.ToolMultiplier, value => config.ToolMultiplier = value, 1, 4, 0.5);
            menu.AddInlineBoolNumberOption(helper.ModManifest, () => T("config.bottleFill.name", "Bottle fill speed"), () => T("config.bottleFill.tooltip", "Plastic-bottle water fill animation."), () => config.BottleFillSpeedEnabled, value => config.BottleFillSpeedEnabled = value, () => config.BottleFillMultiplier, value => config.BottleFillMultiplier = value, 1, 4, 0.5);
            menu.AddInlineBoolNumberOption(helper.ModManifest, () => T("config.eatDrink.name", "Eat/drink speed"), () => T("config.eatDrink.tooltip", "Eat and drink animation."), () => config.EatDrinkSpeedEnabled, value => config.EatDrinkSpeedEnabled = value, () => config.EatDrinkMultiplier, value => config.EatDrinkMultiplier = value, 1, 4, 0.5);
            menu.AddInlineBoolNumberOption(helper.ModManifest, () => T("config.machineAdd.name", "Interaction speed"), () => T("config.machineAdd.tooltip", "Fuel/feed, animal, and electric-machine interactions."), () => config.MachineAddSpeedEnabled, value => config.MachineAddSpeedEnabled = value, () => config.MachineAddMultiplier, value => config.MachineAddMultiplier = value, 1, 4, 0.5);
            menu.AddInlineBoolNumberOption(helper.ModManifest, () => T("config.harvest.name", "Harvest speed"), () => T("config.harvest.tooltip", "Crop, forage, and resin interactions."), () => config.HarvestSpeedEnabled, value => config.HarvestSpeedEnabled = value, () => config.HarvestMultiplier, value => config.HarvestMultiplier = value, 1, 4, 0.5);
            menu.AddInlineBoolNumberOption(helper.ModManifest, () => T("config.plant.name", "Planting speed"), () => T("config.plant.tooltip", "Seed, fertilizer, and crop-film interactions."), () => config.PlantSpeedEnabled, value => config.PlantSpeedEnabled = value, () => config.PlantMultiplier, value => config.PlantMultiplier = value, 1, 4, 0.5);
            menu.AddSectionTitle(helper.ModManifest, () => T("config.section.quick", "Quick bottle actions"));
            menu.AddInlineBoolBoolOption(helper.ModManifest, () => T("config.autoFill.name", "Auto fill bottle"), () => T("config.autoFill.tooltip", "Automatically fill a held empty bottle while in water."), () => config.AutoFillBottle, value => config.AutoFillBottle = value, () => T("config.autoFillStrong.name", "Strong auto fill"), () => T("config.autoFillStrong.tooltip", "Attempt the native bottle-fill path more frequently."), () => config.AutoFillStrong, value => config.AutoFillStrong = value);
            menu.AddBoolOption(helper.ModManifest, () => T("config.rightClickDrink.name", "Right-click drink"), () => T("config.rightClickDrink.tooltip", "Scale the native continuous bottled-water timer."), () => config.ContinuousDrinkWithRightClick, value => config.ContinuousDrinkWithRightClick = value);
            if (menu is IDtmConfigMenuKeybindDefaultsApi keybindDefaults)
                keybindDefaults.AddKeybindOption(helper.ModManifest, () => T("config.menuKey.name", "Menu key"), () => T("config.menuKey.tooltip", "Opens this DTMAPI config page."), () => config.MenuKey, value => config.MenuKey = value, () => "F10");
            else
                menu.AddKeybindOption(helper.ModManifest, () => T("config.menuKey.name", "Menu key"), () => T("config.menuKey.tooltip", "Opens this DTMAPI config page."), () => config.MenuKey, value => config.MenuKey = value);
        }

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e) => nativeRuntime.Update();

        private void OnKeybindPressed(object sender, KeybindPressedEventArgs e)
        {
            if (!e.OwnerId.Equals(helper.ModManifest.UniqueID, StringComparison.OrdinalIgnoreCase) ||
                !e.KeybindId.Equals(MenuRegistrationId, StringComparison.Ordinal))
                return;
            helper.UI.OpenConfigPage(helper.ModManifest.UniqueID);
            helper.Monitor.Log("ActionSpeed hotkey OpenConfig OK key=" + e.TriggerButton);
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            nativeRuntime.ResetBoundary("SaveLoaded");
            helper.Monitor.Log("ActionSpeed SaveLoaded restore boundary OK slot=" + (e.SaveSlot?.ToString() ?? "unknown") + ".");
        }

        private void OnReturnedToTitle(object sender, EventArgs e)
        {
            nativeRuntime.ResetBoundary("ReturnedToTitle");
            helper.Monitor.Log("ActionSpeed ReturnedToTitle restore boundary OK.");
        }

        private void SetUpdateSubscription(bool value)
        {
            if (updateSubscribed == value)
                return;
            if (value)
                helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            else
                helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
            updateSubscribed = value;
        }

        private void UnsubscribeEvents()
        {
            if (updateSubscribed)
            {
                helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
                updateSubscribed = false;
            }
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
            config = new ActionSpeedConfig();
            config.Normalize();
        }

        private static void TryCleanup(Action cleanup, ICollection<Exception> failures)
        {
            try { cleanup(); }
            catch (Exception ex) { failures.Add(ex); }
        }
    }
}
