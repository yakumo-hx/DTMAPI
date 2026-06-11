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
        private string registeredInfoKey = string.Empty;
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
            helper.Monitor.Log("AutoFishing migrated to DTMAPI shell. Defaults are safe; toggle=" + config.ToggleKey + ", info=" + config.InfoKey + ".");
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
            menu.AddParagraph(helper.ModManifest, () => string.Format(T("config.status.experimental", "Experimental: F6 input is used only to prove the hotkey chain; full fishing automation remains experimental. Current state: {0}."), FormatRuntimeStatus(menu)));
            menu.AddParagraph(helper.ModManifest, () => T("config.boundary.current", "Current boundary: the selected fishing rod is required, auto recast is always normalized on, auto-complete is delayed success forcing, skip is not an independent minigame skip, and fast animation can no-op unless the animator path is available."));
            menu.AddKeybindOption(helper.ModManifest, () => T("config.toggleKey.name", "Toggle key"), () => T("config.toggleKey.tooltip", "Toggles auto fishing. DTMAPI configuration is opened from the title settings button."), () => config.ToggleKey, value => config.ToggleKey = value);
            menu.AddKeybindOption(helper.ModManifest, () => T("config.infoKey.name", "Info key"), () => T("config.infoKey.tooltip", "Opens this config page until the fish-info UI page is promoted."), () => config.InfoKey, value => config.InfoKey = value);
            menu.AddBoolOption(helper.ModManifest, () => T("config.stopManual.name", "Stop on manual move"), () => T("config.stopManual.tooltip", "Movement, jump, dash, menus, or cancel should stop automation."), () => config.StopOnManualMove, value => config.StopOnManualMove = value);
            menu.AddNumberOption(helper.ModManifest, () => T("config.castRelease.name", "Cast release progress"), () => T("config.castRelease.tooltip", "0 skips charging; 1 waits for full charge."), () => config.CastReleaseProgress, value => config.CastReleaseProgress = value, 0, 1, 0.05);
            menu.AddNumberOption(helper.ModManifest, () => T("config.recastDelay.name", "Recast delay"), () => T("config.recastDelay.tooltip", "Delay before the next cast after pull/result. Auto recast itself is currently forced on by the experimental service."), () => config.RecastDelaySeconds, value => config.RecastDelaySeconds = value, 0.05, 10, 0.05);
            menu.AddBoolOption(helper.ModManifest, () => T("config.instantBite.name", "Instant bite"), () => T("config.instantBite.tooltip", "Verified for the wait phase, still experimental; changes bite timing."), () => config.InstantBite, value => config.InstantBite = value);
            menu.AddBoolOption(helper.ModManifest, () => T("config.autoCompleteMinigame.name", "Delayed minigame success"), () => T("config.autoCompleteMinigame.tooltip", "Experimental: after a real minigame has been visible briefly, force its native status to Success. This is not a progress-aware solver."), () => config.AutoCompleteMiniGame, value => config.AutoCompleteMiniGame = value);
            menu.AddBoolOption(helper.ModManifest, () => T("config.skipMinigame.name", "Skip after instant bite"), () => T("config.skipMinigame.tooltip", "Experimental: only affects the Instant Bite transition by routing directly to pull/result. It is not an independent minigame skip."), () => config.SkipMiniGame, value => config.SkipMiniGame = value);
            menu.AddBoolOption(helper.ModManifest, () => T("config.fastAnimations.name", "Fast animations (may no-op)"), () => T("config.fastAnimations.tooltip", "Attempts to speed cast/pull animators when those animator fields are reachable; otherwise it safely does nothing."), () => config.FastAnimations, value => config.FastAnimations = value);
            menu.AddChoiceOption(helper.ModManifest, () => T("config.fastMultiplier.name", "Animation multiplier"), () => T("config.fastMultiplier.tooltip", "Multiplier used only when a supported fishing animator path is found."), () => config.FastAnimationMultiplier.ToString("0"), value => config.FastAnimationMultiplier = ParseDouble(value, 3), new[] { "2", "3", "4", "5" });
            menu.AddBoolOption(helper.ModManifest, () => T("config.verbose.name", "Verbose logs"), () => T("config.verbose.tooltip", "Write low-frequency migration diagnostics."), () => config.VerboseLogging, value => config.VerboseLogging = value);
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
                AutoRecast = config.AutoRecast,
                StopOnManualMove = config.StopOnManualMove,
                RequireSelectedFishingRod = config.RequireSelectedFishingRod,
                CastReleaseProgress = config.CastReleaseProgress,
                RecastDelaySeconds = config.RecastDelaySeconds,
                AutoCompleteMiniGame = config.AutoCompleteMiniGame,
                SkipMiniGame = config.SkipMiniGame,
                InstantBite = config.InstantBite,
                FastAnimations = config.FastAnimations,
                FastAnimationMultiplier = config.FastAnimationMultiplier,
                VerboseLogging = config.VerboseLogging
            });
            BridgeFeatureStatus status = api.GetStatus(helper.ModManifest.UniqueID);
            helper.Monitor.Log("AutoFishing bridge policy OK reason=" + reason + " status=" + status.Status);
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (enabled && config.StopOnManualMove && IsManualCancelKey(e.Button))
            {
                SetAutomation(false, "manual-move " + e.Button);
                return;
            }

            if (Matches(e.Button, config.ToggleKey))
            {
                SetAutomation(!enabled, "hotkey " + e.Button);
            }
            else if (Matches(e.Button, config.InfoKey))
            {
                helper.UI.OpenConfigPage(helper.ModManifest.UniqueID);
                helper.Monitor.Log("AutoFishing fish-info boundary opened config page key=" + e.Button);
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
            UnregisterKey(registeredInfoKey);
            registeredToggleKey = NormalizeKey(config.ToggleKey);
            registeredInfoKey = NormalizeKey(config.InfoKey);
            helper.Input.RegisterButton(registeredToggleKey);
            helper.Input.RegisterButton(registeredInfoKey);
            foreach (string key in ManualCancelKeys)
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
            config.ToggleKey = NormalizeKey(config.ToggleKey);
            config.InfoKey = NormalizeKey(config.InfoKey);
            config.AutoRecast = true;
            config.RequireSelectedFishingRod = true;
            config.CastReleaseProgress = Clamp(config.CastReleaseProgress, 0, 1);
            config.RecastDelaySeconds = Clamp(config.RecastDelaySeconds, 0.05, 10);
            config.FastAnimationMultiplier = Clamp(config.FastAnimationMultiplier, 2, 5);
            config.FastAnimationMultiplier = Math.Round(config.FastAnimationMultiplier);
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

        private static double Clamp(double value, double min, double max) => Math.Min(max, Math.Max(min, value));

        private static double ParseDouble(string value, double fallback)
        {
            return double.TryParse(value, out double parsed) ? parsed : fallback;
        }

        [DataContract]
        public sealed class AutoFishingConfig
        {
            [DataMember] public string ToggleKey { get; set; } = "F6";
            [DataMember] public string InfoKey { get; set; } = "F9";
            [DataMember] public bool AutoRecast { get; set; } = true;
            [DataMember] public bool StopOnManualMove { get; set; } = true;
            [DataMember] public bool RequireSelectedFishingRod { get; set; } = true;
            [DataMember] public double CastReleaseProgress { get; set; }
            [DataMember] public double RecastDelaySeconds { get; set; } = 0.25;
            [DataMember] public bool SkipMiniGame { get; set; }
            [DataMember] public bool AutoCompleteMiniGame { get; set; }
            [DataMember] public bool InstantBite { get; set; }
            [DataMember] public bool FastAnimations { get; set; }
            [DataMember] public double FastAnimationMultiplier { get; set; } = 3;
            [DataMember] public bool VerboseLogging { get; set; }
        }
    }
}
