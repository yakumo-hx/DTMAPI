using System;
using System.Collections.Generic;
using DTMAPI.Abstractions;
namespace DTMAPI.Zoom
{
    public sealed class ModEntry : DtmMod, IDisposable
    {
        private IDtmHelper helper = null!;
        private ZoomConfig config = new ZoomConfig();
        private ZoomInputBindings bindings = null!;
        private ZoomNativeRuntime runtime = null!;
        private readonly IInputRegistration?[] registrations = new IInputRegistration?[4];
        private bool subscribed, observing, disposed;
        private Func<string, bool> pressed = _ => false, down = _ => false, released = _ => false;
        public override void Entry(IDtmHelper helper)
        {
            this.helper = helper ?? throw new ArgumentNullException(nameof(helper));
            pressed = helper.Input.WasPressed; down = helper.Input.IsDown; released = helper.Input.WasReleased;
            runtime = new ZoomNativeRuntime(helper.Monitor, new ZoomHookInstaller(helper.Monitor));
            runtime.ObservationChanged += SetObservation;
            try
            {
                config = (helper.ReadConfig<ZoomConfig>() ?? new ZoomConfig()).Copy();
                runtime.Configure(config, "Entry");
                RefreshBindings();
                helper.Events.Input.KeybindPressed += OnKeybindPressed;
                helper.Events.Save.SaveLoaded += OnSaveLoaded;
                helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
                subscribed = true;
                RegisterConfigMenu();
                helper.Monitor.Log(T("mod.loaded", "DTMAPI Zoom loaded; use = / - to adjust the view.") + " owner=" + ZoomProductContract.HarmonyOwner);
            }
            catch (Exception failure)
            {
                try { Cleanup(ownerEnded: false); } catch (Exception cleanup) { throw new AggregateException(failure, cleanup); }
                throw;
            }
        }
        public void Dispose() => Cleanup(ownerEnded: true);
        private void Cleanup(bool ownerEnded)
        {
            if (disposed) return;
            var failures = new List<Exception>();
            // Core has begun deactivation before IDisposable and owns event-root
            // removal. Its helper cannot be called during that phase.
            if (runtime != null) runtime.ObservationChanged -= SetObservation;
            if (subscribed && !ownerEnded)
            {
                Try(() => helper.Events.Input.KeybindPressed -= OnKeybindPressed, failures);
                Try(() => helper.Events.Save.SaveLoaded -= OnSaveLoaded, failures);
                Try(() => helper.Events.GameLoop.ReturnedToTitle -= OnReturnedToTitle, failures);
            }
            if (!ownerEnded) Try(() => SetObservation(false), failures);
            subscribed = false; observing = false;
            for (int i = 0; i < registrations.Length; ++i)
            {
                int index = i;
                Try(() => { registrations[index]?.Dispose(); registrations[index] = null; }, failures);
            }
            if (runtime != null) Try(() => runtime.DeactivateOwner("OwnerDeactivation"), failures);
            if (failures.Count != 0) throw new AggregateException("Zoom owner cleanup remains retryable.", failures);
            disposed = true;
        }
        private void OnKeybindPressed(object sender, KeybindPressedEventArgs e)
        {
            if (!string.Equals(e.OwnerId, helper.ModManifest.UniqueID, StringComparison.OrdinalIgnoreCase)) return;
            int action = bindings.Select(pressed, down, released);
            if (action < 0 || !string.Equals(e.KeybindId, ZoomInputBindings.Ids[action], StringComparison.OrdinalIgnoreCase)) return;
            switch (action)
            {
                case 0: runtime.SetViewScale(config.MaxViewScale, "maximum key"); break;
                case 1: runtime.ResetToVanilla("reset key"); break;
                case 2: runtime.Step(1, "increase key"); break;
                case 3: runtime.Step(-1, "decrease key"); break;
            }
        }
        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e) => runtime.ResetToVanilla("SaveLoaded");
        private void OnReturnedToTitle(object sender, ReturnedToTitleEventArgs e) => runtime.ResetToVanilla("ReturnedToTitle");
        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e) => runtime.Tick();
        private void SetObservation(bool value)
        {
            if (observing == value) return;
            if (value) helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            else helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
            observing = value;
        }
        private void RefreshBindings()
        {
            bindings = new ZoomInputBindings(config);
            for (int i = 0; i < registrations.Length; ++i)
            {
                if (registrations[i] == null) registrations[i] = helper.Input.RegisterKeybind(ZoomInputBindings.Ids[i], bindings.Values[i], DtmInputScope.Gameplay);
                else registrations[i]!.Update(bindings.Values[i], DtmInputScope.Gameplay);
            }
        }
        private void SaveConfig()
        {
            config = config.Copy(); helper.WriteConfig(config);
            runtime.Configure(config, "config saved"); RefreshBindings();
        }
        private void ResetConfig() { config = new ZoomConfig(); }
        private void RegisterConfigMenu()
        {
            var menu = helper.ModRegistry.GetApi<IDtmConfigMenuApi>("DTMAPI.ModConfigMenu");
            if (menu == null) return;
            menu.Register(helper.ModManifest, ResetConfig, SaveConfig);
            menu.SetDisplayName(helper.ModManifest, () => T("mod.name", "DTMAPI Zoom"));
            menu.AddSectionTitle(helper.ModManifest, () => T("config.section.main", "Zoom"));
            menu.AddNumberOption(helper.ModManifest, () => T("config.maxScale.name", "Maximum view"), () => T("config.maxScale.tooltip", "Maximum visible range multiplier (1–10)."), () => config.MaxViewScale, v => config.MaxViewScale = v, 1d, 10d, 0.25d);
            menu.AddNumberOption(helper.ModManifest, () => T("config.step.name", "Step"), () => T("config.step.tooltip", "View scale changed per key press."), () => config.Step, v => config.Step = v, 0.05d, 2d, 0.05d);
            menu.AddKeybindOption(helper.ModManifest, () => T("config.increaseKey.name", "Increase view key"), () => T("config.increaseKey.tooltip", "Equals or keypad plus; physical Shift selects maximum by default."), () => config.IncreaseKey, v => config.IncreaseKey = v);
            menu.AddKeybindOption(helper.ModManifest, () => T("config.decreaseKey.name", "Decrease view key"), () => T("config.decreaseKey.tooltip", "Decrease visible range."), () => config.DecreaseKey, v => config.DecreaseKey = v);
            menu.AddKeybindOption(helper.ModManifest, () => T("config.maximumKey.name", "Maximum view key"), () => T("config.maximumKey.tooltip", "Go directly to the configured maximum."), () => config.MaximumKey, v => config.MaximumKey = v);
            menu.AddKeybindOption(helper.ModManifest, () => T("config.resetKey.name", "Reset view key"), () => T("config.resetKey.tooltip", "Return to the original view."), () => config.ResetKey, v => config.ResetKey = v);
        }
        private string T(string key, string fallback) => helper.Translation.Get(key, fallback);
        private static void Try(Action action, ICollection<Exception> failures)
        { try { action(); } catch (Exception ex) { failures.Add(ex); } }
    }
}
