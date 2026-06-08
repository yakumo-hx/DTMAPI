using System;
using System.Globalization;
using System.Runtime.Serialization;
using DTMAPI.Abstractions;

namespace ZoomMod
{
    public sealed class ModEntry : DtmMod
    {
        private IDtmHelper helper = null!;
        private ZoomConfig config = new ZoomConfig();
        private ICameraViewApi? cameraViewApi;
        private ICameraViewLease? cameraViewLease;
        private string registeredIncreaseKey = string.Empty;
        private string registeredDecreaseKey = string.Empty;
        private string registeredPlusKey = string.Empty;
        private string registeredKeypadIncreaseKey = string.Empty;
        private string registeredKeypadDecreaseKey = string.Empty;

        public override void Entry(IDtmHelper helper)
        {
            this.helper = helper;
            config = helper.ReadConfig<ZoomConfig>();
            NormalizeConfig();
            RegisterInputKeys();
            RegisterConfigMenu();
            BindZoomApi("Entry");

            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.GameLoop.ReturnedToTitle += (_, __) => ResetZoom("ReturnedToTitle");
            helper.Events.Save.SaveLoaded += (_, e) =>
            {
                BindZoomApi("SaveLoaded slot=" + (e.SaveSlot?.ToString(CultureInfo.InvariantCulture) ?? "unknown"));
                ResetZoom("SaveLoaded");
            };

            helper.Monitor.Log(T("mod.loaded", "DTMAPI Zoom loaded; press + / - to adjust the view."));
        }

        private void RegisterConfigMenu()
        {
            IDtmConfigMenuApi? menu = helper.ModRegistry.GetApi<IDtmConfigMenuApi>("DTMAPI.ModConfigMenu");
            if (menu == null)
                return;

            menu.Register(helper.ModManifest, ResetConfig, SaveConfig);
            menu.SetDisplayName(helper.ModManifest, () => T("mod.name", "DTMAPI Zoom"));
            menu.AddSectionTitle(helper.ModManifest, () => T("config.section.main", "Zoom"));
            menu.AddParagraph(helper.ModManifest, BuildStatusText);
            menu.AddBoolOption(helper.ModManifest, () => T("config.enabled.name", "Enabled"), () => T("config.enabled.tooltip", "Allows + / - to adjust the world camera view."), () => config.Enabled, value => config.Enabled = value);
            menu.AddNumberOption(helper.ModManifest, () => T("config.maxScale.name", "Maximum view"), () => T("config.maxScale.tooltip", "Maximum view scale. 4x targets roughly half-farm visibility; lower this if the game camera is unstable."), () => config.MaxViewScale, value => config.MaxViewScale = value, 1, 4, 0.25);
            menu.AddNumberOption(helper.ModManifest, () => T("config.step.name", "Step"), () => T("config.step.tooltip", "View scale changed per key press."), () => config.Step, value => config.Step = value, 0.05, 1, 0.05);
            menu.AddKeybindOption(helper.ModManifest, () => T("config.increaseKey.name", "Increase view key"), () => T("config.increaseKey.tooltip", "Increase visible range. Keyboard + is often captured as Equals or Plus."), () => config.IncreaseKey, value => config.IncreaseKey = value);
            menu.AddKeybindOption(helper.ModManifest, () => T("config.decreaseKey.name", "Decrease view key"), () => T("config.decreaseKey.tooltip", "Decrease visible range down to vanilla."), () => config.DecreaseKey, value => config.DecreaseKey = value);
        }

        private void BindZoomApi(string reason)
        {
            cameraViewApi = helper.ModRegistry.GetApi<ICameraViewApi>("DTMAPI.GameBridge.DolocTown");
            if (cameraViewApi == null)
            {
                helper.Monitor.Log(T("mod.apiMissing", "Camera View API is not available yet.") + " reason=" + reason, LogLevel.Warn);
                return;
            }

            var request = new CameraViewRequest
            {
                Enabled = config.Enabled,
                ViewScale = cameraViewLease?.GetState().CurrentViewScale ?? 1,
                MinViewScale = 1,
                MaxViewScale = config.MaxViewScale,
                Step = config.Step,
                Priority = 0,
                LeaseName = "ZoomMod playable view",
                VerboseLogging = config.VerboseLogging
            };

            CameraViewResult result;
            if (cameraViewLease == null || cameraViewLease.IsReleased)
            {
                cameraViewLease = cameraViewApi.AcquireLease(helper.ModManifest, request);
                result = cameraViewLease.LastResult;
            }
            else
            {
                result = cameraViewLease.Update(request, reason);
            }
            helper.Monitor.Log(string.Format(CultureInfo.InvariantCulture, T("mod.register", "CameraView lease: success={0} current={1} message={2}"), result.Success, Format(result.AppliedViewScale), result.Message), result.Success ? LogLevel.Info : LogLevel.Warn);
        }

        private string BuildStatusText()
        {
            if (cameraViewApi == null)
                return T("config.status.missing", "Camera View API is not available.");

            CameraViewState state = cameraViewLease?.GetState() ?? cameraViewApi.GetState(helper.ModManifest.UniqueID);
            return string.Format(CultureInfo.InvariantCulture, T("config.status", "Status={0}, current={1}, applied={2}, range={3}-{4}, active={5}, arbitration={6}, camera={7}, owner={8}, nativeRefresh={9}, lifecycle={10}, vanillaSize={11}, appliedSize={12}. {13}"), state.Status, Format(state.CurrentViewScale), Format(state.AppliedViewScale), Format(state.MinViewScale), Format(state.MaxViewScale), state.ActiveOwnerId, state.ArbitrationStatus, state.CameraAvailable, state.CameraOwnerStatus, state.NativeRefreshStatus, state.LifecycleStatus, Format(state.VanillaOrthographicSize), Format(state.AppliedOrthographicSize), state.LastMessage);
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!config.Enabled || cameraViewLease == null)
                return;

            if (Matches(e.Button, config.IncreaseKey) || e.Button.Equals("KeypadPlus", StringComparison.OrdinalIgnoreCase) || e.Button.Equals("Plus", StringComparison.OrdinalIgnoreCase))
            {
                StepZoom(1, e.Button);
            }
            else if (Matches(e.Button, config.DecreaseKey) || e.Button.Equals("KeypadMinus", StringComparison.OrdinalIgnoreCase))
            {
                StepZoom(-1, e.Button);
            }
        }

        private void StepZoom(int direction, string key)
        {
            if (cameraViewLease == null)
                return;

            CameraViewState state = cameraViewLease.GetState();
            double current = state.CurrentViewScale <= 0 ? 1 : state.CurrentViewScale;
            CameraViewResult result = cameraViewLease.SetViewScale(current + (config.Step * direction), "hotkey " + key);
            helper.Monitor.Log(string.Format(CultureInfo.InvariantCulture, T("mod.step", "Zoom {0}: success={1} before={2} after={3} message={4}"), direction > 0 ? "+" : "-", result.Success, Format(result.BeforeViewScale), Format(result.AfterViewScale), result.Message), result.Success ? LogLevel.Info : LogLevel.Warn);
        }

        private void ResetZoom(string reason)
        {
            if (cameraViewLease == null)
                return;

            CameraViewResult result = cameraViewLease.SetViewScale(1, reason);
            helper.Monitor.Log(string.Format(CultureInfo.InvariantCulture, T("mod.reset", "Zoom restored to vanilla view: success={0} message={1}"), result.Success, result.Message), result.Success ? LogLevel.Info : LogLevel.Warn);
        }

        private void SaveConfig()
        {
            NormalizeConfig();
            helper.WriteConfig(config);
            RegisterInputKeys();
            BindZoomApi("config saved");
        }

        private void ResetConfig()
        {
            config = new ZoomConfig();
            NormalizeConfig();
        }

        private void RegisterInputKeys()
        {
            UnregisterKey(registeredIncreaseKey);
            UnregisterKey(registeredDecreaseKey);
            UnregisterKey(registeredPlusKey);
            UnregisterKey(registeredKeypadIncreaseKey);
            UnregisterKey(registeredKeypadDecreaseKey);

            registeredIncreaseKey = NormalizeKey(config.IncreaseKey);
            registeredDecreaseKey = NormalizeKey(config.DecreaseKey);
            registeredPlusKey = "Plus";
            registeredKeypadIncreaseKey = "KeypadPlus";
            registeredKeypadDecreaseKey = "KeypadMinus";
            RegisterKey(registeredIncreaseKey);
            RegisterKey(registeredDecreaseKey);
            RegisterKey(registeredPlusKey);
            RegisterKey(registeredKeypadIncreaseKey);
            RegisterKey(registeredKeypadDecreaseKey);
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

        private void NormalizeConfig()
        {
            config.MaxViewScale = Clamp(config.MaxViewScale, 1, 4);
            config.Step = Clamp(config.Step, 0.05, 1);
            config.IncreaseKey = NormalizeKey(config.IncreaseKey);
            config.DecreaseKey = NormalizeKey(config.DecreaseKey);
        }

        private string T(string key, string fallback) => helper.Translation.Get(key, fallback);

        private static bool Matches(string actual, string expected)
        {
            expected = NormalizeKey(expected);
            return !expected.Equals("None", StringComparison.OrdinalIgnoreCase) && actual.Equals(expected, StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeKey(string key)
        {
            key = (key ?? string.Empty).Trim();
            return string.IsNullOrWhiteSpace(key) ? "None" : key;
        }

        private static double Clamp(double value, double min, double max) => Math.Min(max, Math.Max(min, value));

        private static string Format(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        [DataContract]
        public sealed class ZoomConfig
        {
            [DataMember] public bool Enabled { get; set; } = true;
            [DataMember] public double MaxViewScale { get; set; } = 4;
            [DataMember] public double Step { get; set; } = 0.25;
            [DataMember] public string IncreaseKey { get; set; } = "Equals";
            [DataMember] public string DecreaseKey { get; set; } = "Minus";
            [DataMember] public bool VerboseLogging { get; set; }
        }
    }
}
