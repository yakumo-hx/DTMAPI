using System;
using System.Runtime.Serialization;
using DTMAPI.Abstractions;

namespace AutoFishingMod
{
    public sealed class ModEntry : DtmMod
    {
        private static readonly string[] ManualCancelKeys = { "W", "A", "S", "D", "Space", "LeftShift", "RightShift" };
        private IDtmHelper helper = null!;
        private AutoFishingConfig config = new AutoFishingConfig();
        private string registeredToggleKey = string.Empty;
        private bool updateEvidenceLogged;
        private bool enabled;

        public override void Entry(IDtmHelper helper)
        {
            this.helper = helper;
            config = helper.ReadConfig<AutoFishingConfig>();
            NormalizeConfig();
            RegisterInputKeys();
            RegisterConfigMenu();
            ConfigureBridge("Entry");

            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.Save.SaveLoaded += (_, e) => helper.Monitor.Log("AutoFishing SaveLoaded boundary OK slot=" + (e.SaveSlot?.ToString() ?? "unknown"));
            helper.Events.GameLoop.ReturnedToTitle += (_, __) => SetAutomation(false, "ReturnedToTitle");
            helper.Monitor.Log("AutoFishing native loop policy registered. Toggle=" + NormalizeKey(config.ToggleKey) + ".");
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
                RegisterInputKeys();
                ConfigureBridge("config saved");
                helper.Monitor.Log("AutoFishing config saved through DTMAPI menu.");
            });

            menu.SetDisplayName(helper.ModManifest, () => T("mod.name", helper.ModManifest.Name));
            menu.AddSectionTitle(helper.ModManifest, () => T("config.section.main", "Auto fishing"));
            menu.AddParagraph(helper.ModManifest, () => string.Format(T("config.status.experimental", "{0} toggles the native auto-fishing loop. Current state: {1}."), FormatToggleKey(), FormatRuntimeStatus(menu)));
            menu.AddParagraph(helper.ModManifest, () => T("config.boundary.current", "Default loop: cast with the configured charge, wait for a native bite, reel, show and auto-complete the real minigame, collect the result, then recast. Optional switches only change bite waiting, minigame skipping, or charge/cast/pull animation speed."));
            menu.AddKeybindOption(helper.ModManifest, () => T("config.toggleKey.name", "Toggle key"), () => T("config.toggleKey.tooltip", "Press this key in-game to toggle AutoFishing. Set to None to disable the hotkey."), () => config.ToggleKey, value => config.ToggleKey = value);
            menu.AddBoolOption(helper.ModManifest, () => T("config.instantBite.name", "Instant bite"), () => T("config.instantBite.tooltip", "Skip the native waiting period after the hook reaches water, then reel into the normal minigame/result path."), () => config.InstantBite, value => config.InstantBite = value);
            menu.AddBoolOption(helper.ModManifest, () => T("config.skipMinigame.name", "Skip minigame"), () => T("config.skipMinigame.tooltip", "Route bite-ready results through the native no-minigame result path. Native failure/success is preserved."), () => config.SkipMiniGame, value => config.SkipMiniGame = value);
            menu.AddNumberOption(helper.ModManifest, () => T("config.castCharge.name", "Cast charge"), () => T("config.castCharge.tooltip", "Target cast charge before releasing the rod. 0 means no charge; 1 means full charge. Fast animations also speed this charge phase."), () => config.CastChargeRatio, value => config.CastChargeRatio = value, 0, 1, 0.05);
            menu.AddInlineBoolNumberOption(helper.ModManifest, () => T("config.fastAnimations.name", "Fast cast/pull animations"), () => T("config.fastAnimations.tooltip", "Speed up only the native charge, cast hook flight, and pull phases."), () => config.FastAnimations, value => config.FastAnimations = value, () => config.AnimationMultiplier, value => config.AnimationMultiplier = value, 1, 4, 0.5);
        }

        private string T(string key, string fallback) => helper.Translation.Get(key, fallback);

        private void ConfigureBridge(string reason)
        {
            IFishingAutomationApi? api = helper.ModRegistry.GetApi<IFishingAutomationApi>("DTMAPI.GameBridge.DolocTown");
            if (api == null)
            {
                helper.Monitor.Log("Fishing automation GameBridge API is not available.", LogLevel.Warn);
                return;
            }

            api.Configure(helper.ModManifest, new FishingAutomationOptions
            {
                BiteWaitMode = config.InstantBite ? FishingBiteWaitMode.InstantNativeBite : FishingBiteWaitMode.NativeWait,
                ResultMode = config.SkipMiniGame ? FishingResultMode.SkipMiniGameNativeResult : FishingResultMode.AutoCompleteVisibleMiniGame,
                AnimationMode = config.FastAnimations ? FishingAnimationMode.FastCastPull : FishingAnimationMode.Normal,
                StopOnManualMove = true,
                RecastDelaySeconds = 0.25,
                CastChargeRatio = config.CastChargeRatio,
                AnimationMultiplier = config.AnimationMultiplier,
                VerboseLogging = config.VerboseLogging
            });
            BridgeFeatureStatus status = api.GetStatus(helper.ModManifest.UniqueID);
            helper.Monitor.Log("AutoFishing bridge policy OK reason=" + reason + " status=" + status.Status);
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (enabled && IsManualCancelKey(e.Button))
            {
                SetAutomation(false, "manual-move " + e.Button);
                return;
            }

            if (Matches(e.Button, config.ToggleKey))
            {
                SetAutomation(!enabled, "hotkey " + e.Button);
            }
        }

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!updateEvidenceLogged)
            {
                updateEvidenceLogged = true;
                helper.Monitor.Log("AutoFishing UpdateTicked OK tick=" + e.Tick);
            }
        }

        private void SetAutomation(bool value, string reason)
        {
            enabled = value;
            IFishingAutomationApi? api = helper.ModRegistry.GetApi<IFishingAutomationApi>("DTMAPI.GameBridge.DolocTown");
            api?.SetEnabled(helper.ModManifest, enabled, reason);
            helper.Monitor.Log("AutoFishing automation " + (enabled ? "enabled" : "disabled") + " reason=" + reason);
        }

        private string FormatRuntimeStatus(IDtmConfigMenuApi menu)
        {
            IFishingAutomationApi? api = helper.ModRegistry.GetApi<IFishingAutomationApi>("DTMAPI.GameBridge.DolocTown");
            FishingAutomationState state = api?.GetState(helper.ModManifest.UniqueID) ?? new FishingAutomationState { Enabled = enabled };
            string phase = string.IsNullOrWhiteSpace(state.Phase) ? "Idle" : state.Phase;
            string reason = string.IsNullOrWhiteSpace(state.LastReason) ? "-" : state.LastReason;
            string bucket = FormatStateBucket(state.Enabled || enabled, phase);
            string status = bucket + " / phase=" + phase + " / reason=" + reason;

            var conflicts = menu.GetKeybindConflicts(helper.ModManifest.UniqueID);
            if (conflicts.Count > 0)
                status += " / " + T("state.conflict", "按键冲突") + ": " + string.Join("; ", conflicts);

            return status;
        }

        private string FormatToggleKey()
        {
            string key = NormalizeKey(config.ToggleKey);
            return key.Equals("None", StringComparison.OrdinalIgnoreCase)
                ? T("state.hotkeyDisabled", "hotkey disabled")
                : key;
        }

        private string FormatStateBucket(bool isEnabled, string phase)
        {
            if (!isEnabled)
                return T("state.notEnabled", "未启用");
            if (phase.Equals("Idle", StringComparison.OrdinalIgnoreCase) || phase.Equals("Starting", StringComparison.OrdinalIgnoreCase))
                return T("state.waitingFishing", "等待钓鱼");
            return T("state.experimental", "实验阶段");
        }

        private void RegisterInputKeys()
        {
            UnregisterKey(registeredToggleKey);
            registeredToggleKey = NormalizeKey(config.ToggleKey);
            RegisterKey(registeredToggleKey);
            foreach (string key in ManualCancelKeys)
                RegisterKey(key);
        }

        private void RegisterKey(string key)
        {
            if (!string.IsNullOrWhiteSpace(key) && !key.Equals("None", StringComparison.OrdinalIgnoreCase))
                helper.Input.RegisterButton(key);
        }

        private void UnregisterKey(string key)
        {
            if (!string.IsNullOrWhiteSpace(key) && !key.Equals("None", StringComparison.OrdinalIgnoreCase))
                helper.Input.UnregisterButton(key);
        }

        private void ResetConfig()
        {
            config = new AutoFishingConfig();
            NormalizeConfig();
        }

        private void NormalizeConfig()
        {
            config.ToggleKey = string.IsNullOrWhiteSpace(config.ToggleKey)
                ? "F6"
                : NormalizeKey(config.ToggleKey);
            if (double.IsNaN(config.AnimationMultiplier) || double.IsInfinity(config.AnimationMultiplier) || config.AnimationMultiplier <= 0)
                config.AnimationMultiplier = 3;
            config.AnimationMultiplier = Math.Min(4, Math.Max(1, config.AnimationMultiplier));
            if (double.IsNaN(config.CastChargeRatio) || double.IsInfinity(config.CastChargeRatio))
                config.CastChargeRatio = 0;
            config.CastChargeRatio = Math.Min(1, Math.Max(0, config.CastChargeRatio));
        }

        private static bool Matches(string actual, string expected)
        {
            expected = NormalizeKey(expected);
            return !expected.Equals("None", StringComparison.OrdinalIgnoreCase) &&
                actual.Equals(expected, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsManualCancelKey(string button)
        {
            foreach (string key in ManualCancelKeys)
            {
                if (key.Equals(button, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static string NormalizeKey(string key)
        {
            key = (key ?? string.Empty).Trim();
            return string.IsNullOrWhiteSpace(key) ? "None" : key;
        }

        [DataContract]
        public sealed class AutoFishingConfig
        {
            [DataMember] public string ToggleKey { get; set; } = "F6";
            [DataMember] public bool SkipMiniGame { get; set; }
            [DataMember] public bool InstantBite { get; set; }
            [DataMember] public bool FastAnimations { get; set; }
            [DataMember] public double AnimationMultiplier { get; set; } = 3;
            [DataMember] public double CastChargeRatio { get; set; }
            [DataMember] public bool VerboseLogging { get; set; }
        }
    }
}
