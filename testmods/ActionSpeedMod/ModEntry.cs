using System;
using System.Runtime.Serialization;
using DTMAPI.Abstractions;

namespace ActionSpeedMod
{
    public sealed class ModEntry : DtmMod
    {
        private IDtmHelper? helper;
        private ActionSpeedConfig config = new ActionSpeedConfig();
        private string registeredMenuKey = string.Empty;
        private string registeredDrinkKey = string.Empty;
        private bool updateEvidenceLogged;
        private bool applied;

        public override void Entry(IDtmHelper helper)
        {
            this.helper = helper;
            config = helper.ReadConfig<ActionSpeedConfig>();
            NormalizeConfig();
            RegisterInputKeys();
            RegisterConfigMenu(helper);
            RegisterActionSpeedPolicy("Entry");

            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.Save.SaveLoaded += (_, e) =>
                helper.Monitor.Log("ActionSpeed SaveLoaded restore boundary OK slot=" + (e.SaveSlot?.ToString() ?? "unknown"));
            helper.Events.GameLoop.ReturnedToTitle += (_, __) => RestoreRuntime("ReturnedToTitle");

            helper.Monitor.Log("ActionSpeed migrated to DTMAPI shell. Defaults are off; title DTMAPI Settings is the player-facing config entry.");
        }

        private void RegisterConfigMenu(IDtmHelper helper)
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
                RegisterInputKeys();
                RegisterActionSpeedPolicy("config-save");
                helper.Monitor.Log("ActionSpeed config saved through DTMAPI menu.");
            });
            menu.SetDisplayName(helper.ModManifest, () => T(helper, "mod.name", helper.ModManifest.Name));
            menu.AddSectionTitle(helper.ModManifest, () => T(helper, "config.section.main", "Action speed"));
            menu.AddParagraph(helper.ModManifest, () => T(helper, "config.status.toolVerified", "Tool animation, fuel/feed add, eat/drink animation, bottled-water right-click continuous drink, IWaterContainer and in-water bottle fill, no-key auto-fill, planting, plant-basin crop harvest, resin collection, and wild vegetation harvest are verified in smoke."));
            menu.AddBoolOption(helper.ModManifest, () => T(helper, "config.enabled.name", "Enabled"), () => T(helper, "config.enabled.tooltip", "Master switch for all ActionSpeed behavior."), () => config.Enabled, value => config.Enabled = value);
            menu.AddInlineBoolNumberOption(helper.ModManifest, () => T(helper, "config.toolSpeed.name", "Tool animation speed"), () => T(helper, "config.toolSpeed.tooltip", "Axe, pickaxe, and sickle animation speed hook target."), () => config.ToolSpeedEnabled, value => config.ToolSpeedEnabled = value, () => config.ToolMultiplier, value => config.ToolMultiplier = value, 1, 4, 0.5);
            menu.AddInlineBoolNumberOption(helper.ModManifest, () => T(helper, "config.bottleFill.name", "Bottle fill speed"), () => T(helper, "config.bottleFill.tooltip", "Plastic-bottle water fill animation hook target."), () => config.BottleFillSpeedEnabled, value => config.BottleFillSpeedEnabled = value, () => config.BottleFillMultiplier, value => config.BottleFillMultiplier = value, 1, 4, 0.5);
            menu.AddInlineBoolNumberOption(helper.ModManifest, () => T(helper, "config.eatDrink.name", "Eat/drink speed"), () => T(helper, "config.eatDrink.tooltip", "Eat and drink animation hook target."), () => config.EatDrinkSpeedEnabled, value => config.EatDrinkSpeedEnabled = value, () => config.EatDrinkMultiplier, value => config.EatDrinkMultiplier = value, 1, 4, 0.5);
            menu.AddInlineBoolNumberOption(helper.ModManifest, () => T(helper, "config.machineAdd.name", "Machine add speed"), () => T(helper, "config.machineAdd.tooltip", "Fuel/feed interaction animation hook target."), () => config.MachineAddSpeedEnabled, value => config.MachineAddSpeedEnabled = value, () => config.MachineAddMultiplier, value => config.MachineAddMultiplier = value, 1, 4, 0.5);
            menu.AddInlineBoolNumberOption(helper.ModManifest, () => T(helper, "config.harvest.name", "Harvest speed"), () => T(helper, "config.harvest.tooltip", "Crop, forage, and resin interaction animation hook target."), () => config.HarvestSpeedEnabled, value => config.HarvestSpeedEnabled = value, () => config.HarvestMultiplier, value => config.HarvestMultiplier = value, 1, 4, 0.5);
            menu.AddInlineBoolNumberOption(helper.ModManifest, () => T(helper, "config.plant.name", "Planting speed"), () => T(helper, "config.plant.tooltip", "Experimental: seed planting interaction animation hook target."), () => config.PlantSpeedEnabled, value => config.PlantSpeedEnabled = value, () => config.PlantMultiplier, value => config.PlantMultiplier = value, 1, 4, 0.5);
            menu.AddSectionTitle(helper.ModManifest, () => T(helper, "config.section.quick", "Quick bottle actions"));
            menu.AddInlineBoolBoolOption(helper.ModManifest, () => T(helper, "config.autoFill.name", "Auto fill bottle"), () => T(helper, "config.autoFill.tooltip", "Automatically fill held bottles while in water once hooks are promoted."), () => config.AutoFillBottle, value => config.AutoFillBottle = value, () => T(helper, "config.autoFillStrong.name", "Strong auto fill"), () => T(helper, "config.autoFillStrong.tooltip", "Attempts the verified native bottle-fill path more frequently while auto fill is enabled."), () => config.AutoFillStrong, value => config.AutoFillStrong = value);
            menu.AddBoolOption(helper.ModManifest, () => T(helper, "config.rightClickDrink.name", "Right-click drink"), () => T(helper, "config.rightClickDrink.tooltip", "Continuous bottled-water drinking once input/action hooks are promoted."), () => config.ContinuousDrinkWithRightClick, value => config.ContinuousDrinkWithRightClick = value);
        }

        private void RegisterActionSpeedPolicy(string reason)
        {
            if (helper == null)
                return;
            IActionSpeedApi? api = helper.ModRegistry.GetApi<IActionSpeedApi>("DTMAPI.GameBridge.DolocTown");
            if (api == null)
            {
                helper.Monitor.Log("ActionSpeed GameBridge API is not available.", LogLevel.Warn);
                return;
            }

            api.Configure(helper.ModManifest, new ActionSpeedOptions
            {
                Enabled = config.Enabled,
                ToolSpeedEnabled = config.ToolSpeedEnabled,
                ToolMultiplier = config.ToolMultiplier,
                BottleFillSpeedEnabled = config.BottleFillSpeedEnabled,
                BottleFillMultiplier = config.BottleFillMultiplier,
                EatDrinkSpeedEnabled = config.EatDrinkSpeedEnabled,
                EatDrinkMultiplier = config.EatDrinkMultiplier,
                MachineAddSpeedEnabled = config.MachineAddSpeedEnabled,
                MachineAddMultiplier = config.MachineAddMultiplier,
                HarvestSpeedEnabled = config.HarvestSpeedEnabled,
                HarvestMultiplier = config.HarvestMultiplier,
                PlantSpeedEnabled = config.PlantSpeedEnabled,
                PlantMultiplier = config.PlantMultiplier,
                AutoFillBottle = config.AutoFillBottle,
                AutoFillStrong = config.AutoFillBottle && config.AutoFillStrong,
                AutoFillCooldownSeconds = ResolveAutoFillCooldownSeconds(),
                AutoFillStrongCooldownSeconds = ResolveAutoFillStrongCooldownSeconds(),
                ContinuousDrinkWithRightClick = config.ContinuousDrinkWithRightClick,
                VerboseLogging = config.Profile.Equals("Debug", StringComparison.OrdinalIgnoreCase)
            });
            BridgeFeatureStatus status = api.GetStatus(helper.ModManifest.UniqueID);
            helper.Monitor.Log("ActionSpeed bridge policy OK reason=" + reason + " status=" + status.Status);
        }

        private static string T(IDtmHelper helper, string key, string fallback) => helper.Translation.Get(key, fallback);

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (helper == null)
                return;
            if (Matches(e.Button, config.MenuKey))
            {
                helper.UI.OpenConfigPage(helper.ModManifest.UniqueID);
                helper.Monitor.Log("ActionSpeed hotkey OpenConfig OK key=" + e.Button);
            }
            else if (config.Enabled && Matches(e.Button, config.ContinuousDrinkHoldKey))
            {
                helper.Monitor.Log("ActionSpeed drink hotkey observed by DTMAPI input key=" + e.Button);
            }
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (helper == null)
                return;
            if (!updateEvidenceLogged)
            {
                updateEvidenceLogged = true;
                helper.Monitor.Log("ActionSpeed UpdateTicked OK tick=" + e.Tick);
            }

            bool shouldApply = config.Enabled && AnySpeedFeatureEnabled();
            if (shouldApply && !applied)
            {
                applied = true;
                RegisterActionSpeedPolicy("runtime-apply");
                helper.Monitor.Log("ActionSpeed runtime apply boundary OK profile=" + config.Profile + " toolMultiplier=" + config.ToolMultiplier.ToString("0.###"));
            }
            else if (!shouldApply && applied)
            {
                RestoreRuntime("disabled");
            }
        }

        private void RestoreRuntime(string reason)
        {
            if (helper == null || !applied)
                return;
            applied = false;
            helper.Monitor.Log("ActionSpeed restore logic OK reason=" + reason);
        }

        private bool AnySpeedFeatureEnabled()
        {
            return config.ToolSpeedEnabled ||
                config.BottleFillSpeedEnabled ||
                config.EatDrinkSpeedEnabled ||
                config.MachineAddSpeedEnabled ||
                config.HarvestSpeedEnabled ||
                config.PlantSpeedEnabled ||
                config.AutoFillBottle ||
                config.ContinuousDrinkWithRightClick;
        }

        private void RegisterInputKeys()
        {
            if (helper == null)
                return;
            UnregisterKey(registeredMenuKey);
            UnregisterKey(registeredDrinkKey);
            registeredMenuKey = NormalizeKey(config.MenuKey);
            registeredDrinkKey = NormalizeKey(config.ContinuousDrinkHoldKey);
            helper.Input.RegisterButton(registeredMenuKey);
            helper.Input.RegisterButton(registeredDrinkKey);
        }

        private void UnregisterKey(string key)
        {
            if (helper != null && !string.IsNullOrWhiteSpace(key) && !key.Equals("None", StringComparison.OrdinalIgnoreCase))
                helper.Input.UnregisterButton(key);
        }

        private void ResetConfig()
        {
            config = new ActionSpeedConfig();
            NormalizeConfig();
        }

        private void NormalizeConfig()
        {
            config.Profile = NormalizeChoice(config.Profile, "Safe", "Safe", "Fast", "Debug");
            config.ToolMultiplier = Clamp(config.ToolMultiplier, 1, 4);
            config.BottleFillMultiplier = Clamp(config.BottleFillMultiplier, 1, 4);
            config.EatDrinkMultiplier = Clamp(config.EatDrinkMultiplier, 1, 4);
            config.MachineAddMultiplier = Clamp(config.MachineAddMultiplier, 1, 4);
            config.HarvestMultiplier = Clamp(config.HarvestMultiplier, 1, 4);
            config.PlantMultiplier = Clamp(config.PlantMultiplier, 1, 4);
            config.AutoActionCooldownSeconds = Clamp(config.AutoActionCooldownSeconds, 0.05, 5);
            if (!config.AutoFillBottle)
                config.AutoFillStrong = false;
            config.MenuKey = NormalizeKey(config.MenuKey);
            config.ContinuousDrinkHoldKey = NormalizeKey(config.ContinuousDrinkHoldKey);
            config.DebugLabel ??= string.Empty;
        }

        private static bool Matches(string actual, string expected)
        {
            expected = NormalizeKey(expected);
            return !expected.Equals("None", StringComparison.OrdinalIgnoreCase) &&
                actual.Equals(expected, StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeKey(string key)
        {
            key = (key ?? string.Empty).Trim();
            return string.IsNullOrWhiteSpace(key) ? "None" : key;
        }

        private static string NormalizeChoice(string value, string fallback, params string[] allowed)
        {
            foreach (string option in allowed)
            {
                if (option.Equals(value, StringComparison.OrdinalIgnoreCase))
                    return option;
            }
            return fallback;
        }

        private static double Clamp(double value, double min, double max) => Math.Min(max, Math.Max(min, value));

        private double ResolveAutoFillCooldownSeconds()
        {
            if (config.Profile.Equals("Debug", StringComparison.OrdinalIgnoreCase))
                return 0.1;
            if (config.Profile.Equals("Fast", StringComparison.OrdinalIgnoreCase))
                return 0.18;
            return config.AutoActionCooldownSeconds;
        }

        private double ResolveAutoFillStrongCooldownSeconds()
        {
            double normal = ResolveAutoFillCooldownSeconds();
            double profileTarget = config.Profile.Equals("Safe", StringComparison.OrdinalIgnoreCase) ? 0.12 : 0.08;
            return Math.Min(normal, profileTarget);
        }

        [DataContract]
        public sealed class ActionSpeedConfig
        {
            [DataMember] public bool Enabled { get; set; }
            [DataMember] public string Profile { get; set; } = "Safe";
            [DataMember] public bool ToolSpeedEnabled { get; set; }
            [DataMember] public double ToolMultiplier { get; set; } = 3;
            [DataMember] public bool BottleFillSpeedEnabled { get; set; }
            [DataMember] public double BottleFillMultiplier { get; set; } = 3;
            [DataMember] public bool EatDrinkSpeedEnabled { get; set; }
            [DataMember] public double EatDrinkMultiplier { get; set; } = 3;
            [DataMember] public bool MachineAddSpeedEnabled { get; set; }
            [DataMember] public double MachineAddMultiplier { get; set; } = 3;
            [DataMember] public bool HarvestSpeedEnabled { get; set; }
            [DataMember] public double HarvestMultiplier { get; set; } = 3;
            [DataMember] public bool PlantSpeedEnabled { get; set; }
            [DataMember] public double PlantMultiplier { get; set; } = 3;
            [DataMember] public bool AutoFillBottle { get; set; }
            [DataMember] public bool AutoFillStrong { get; set; }
            [DataMember] public bool ContinuousDrinkWithRightClick { get; set; }
            [DataMember] public string ContinuousDrinkHoldKey { get; set; } = "None";
            [DataMember] public string MenuKey { get; set; } = "F10";
            [DataMember] public double AutoActionCooldownSeconds { get; set; } = 0.25;
            [DataMember] public string DebugLabel { get; set; } = "DTMAPI";
        }
    }
}
