using System;
using System.Collections.Generic;
using System.Globalization;
using global::DTMAPI.Abstractions;

namespace DTMAPI.Zoom
{
    public sealed class ModEntry : DtmMod, IDisposable
    {
        private const string IncreaseKeybindId =
            "zoom.increase";
        private const string DecreaseKeybindId =
            "zoom.decrease";

        private IDtmHelper helper = null!;
        private ZoomConfig config = new ZoomConfig();
        private ZoomNativeRuntime runtime = null!;
        private IInputRegistration? increaseRegistration;
        private IInputRegistration? decreaseRegistration;
        private bool inputSubscribed;
        private bool saveSubscribed;
        private bool titleSubscribed;
        private bool runtimeCreated;
        private bool disposed;
        private Func<string, bool> isButtonDown =
            _ => false;
        private Func<string, bool> wasButtonPressed =
            _ => false;
        private Func<string, bool> wasButtonReleased =
            _ => false;

        public override void Entry(IDtmHelper helper)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(ModEntry));
            this.helper = helper ??
                throw new ArgumentNullException(nameof(helper));
            isButtonDown = helper.Input.IsDown;
            wasButtonPressed = helper.Input.WasPressed;
            wasButtonReleased = helper.Input.WasReleased;
            runtime = new ZoomNativeRuntime(
                helper.Monitor,
                new ZoomHookInstaller(helper.Monitor));
            runtimeCreated = true;
            try
            {
                config =
                    helper.ReadConfig<ZoomConfig>() ??
                    new ZoomConfig();
                NormalizeConfig();
                runtime.Configure(config, "Entry");
                RefreshInputBindings();
                RegisterConfigMenu();
                helper.Events.Save.SaveLoaded += OnSaveLoaded;
                saveSubscribed = true;
                helper.Events.GameLoop.ReturnedToTitle +=
                    OnReturnedToTitle;
                titleSubscribed = true;
                helper.Monitor.Log(
                    T(
                        "mod.loaded",
                        "DTMAPI Zoom loaded; press + / - to adjust the view.") +
                    " owner=" +
                    ZoomProductContract.HarmonyOwner +
                    " patches=" +
                    runtime.InstalledPatchCount +
                    ".");
            }
            catch (Exception entryFailure)
            {
                var failures =
                    new List<Exception> { entryFailure };
                TryCleanup(UnsubscribeAll, failures);
                if (runtimeCreated)
                {
                    TryCleanup(
                        () => runtime.DeactivateOwner(
                            "entry-failed"),
                        failures);
                }
                if (failures.Count == 1)
                    throw;
                throw new AggregateException(
                    "Zoom Entry failed and rollback encountered additional cleanup failures.",
                    failures);
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;
            var failures = new List<Exception>();
            // Core removes platform event roots during owner deactivation.
            inputSubscribed = false;
            saveSubscribed = false;
            titleSubscribed = false;
            TryCleanup(DisposeInputRegistrations, failures);
            if (runtimeCreated)
            {
                TryCleanup(
                    () => runtime.DeactivateOwner(
                        "OwnerDeactivation"),
                    failures);
            }
            if (failures.Count > 0)
            {
                throw new AggregateException(
                    "Zoom owner deactivation could not prove native restoration and exact-owner cleanup.",
                    failures);
            }
            disposed = true;
        }

        private void RegisterConfigMenu()
        {
            IDtmConfigMenuApi? menu =
                helper.ModRegistry.GetApi<IDtmConfigMenuApi>(
                    "DTMAPI.ModConfigMenu");
            if (menu == null)
                return;
            menu.Register(
                helper.ModManifest,
                ResetConfig,
                SaveConfig);
            menu.SetDisplayName(
                helper.ModManifest,
                () => T("mod.name", "DTMAPI Zoom"));
            menu.AddSectionTitle(
                helper.ModManifest,
                () => T("config.section.main", "Zoom"));
            menu.AddParagraph(
                helper.ModManifest,
                () => runtime.StatusSummary);
            menu.AddBoolOption(
                helper.ModManifest,
                () => T("config.enabled.name", "Enabled"),
                () => T(
                    "config.enabled.tooltip",
                    "Allows + / - to adjust the world camera view."),
                () => config.Enabled,
                value => config.Enabled = value);
            menu.AddNumberOption(
                helper.ModManifest,
                () => T(
                    "config.maxScale.name",
                    "Maximum view"),
                () => T(
                    "config.maxScale.tooltip",
                    "Maximum playable view scale."),
                () => config.MaxViewScale,
                value => config.MaxViewScale = value,
                1d,
                4d,
                0.25d);
            menu.AddNumberOption(
                helper.ModManifest,
                () => T("config.step.name", "Step"),
                () => T(
                    "config.step.tooltip",
                    "View scale changed per key press."),
                () => config.Step,
                value => config.Step = value,
                0.05d,
                1d,
                0.05d);
            menu.AddKeybindOption(
                helper.ModManifest,
                () => T(
                    "config.increaseKey.name",
                    "Increase view key"),
                () => T(
                    "config.increaseKey.tooltip",
                    "Increase visible range."),
                () => config.IncreaseKey,
                value => config.IncreaseKey = value);
            menu.AddKeybindOption(
                helper.ModManifest,
                () => T(
                    "config.decreaseKey.name",
                    "Decrease view key"),
                () => T(
                    "config.decreaseKey.tooltip",
                    "Decrease visible range down to vanilla."),
                () => config.DecreaseKey,
                value => config.DecreaseKey = value);
        }

        private void OnKeybindPressed(
            object sender,
            KeybindPressedEventArgs e)
        {
            if (!config.Enabled ||
                !e.OwnerId.Equals(
                    helper.ModManifest.UniqueID,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            if (e.KeybindId.Equals(
                    IncreaseKeybindId,
                    StringComparison.OrdinalIgnoreCase))
            {
                runtime.Step(
                    1,
                    "hotkey " + e.TriggerButton);
            }
            else if (e.KeybindId.Equals(
                         DecreaseKeybindId,
                         StringComparison.OrdinalIgnoreCase))
            {
                if (increaseRegistration?
                    .Keybinds.IsPressed(
                        wasButtonPressed,
                        isButtonDown,
                        wasButtonReleased) == true)
                {
                    return;
                }
                runtime.Step(
                    -1,
                    "hotkey " + e.TriggerButton);
            }
        }

        private void OnSaveLoaded(
            object sender,
            SaveLoadedEventArgs e)
        {
            runtime.ResetToVanilla(
                "SaveLoaded slot=" +
                (e.SaveSlot?.ToString(
                    CultureInfo.InvariantCulture) ??
                 "unknown"));
        }

        private void OnReturnedToTitle(
            object sender,
            EventArgs e) =>
            runtime.ResetToVanilla(
                "ReturnedToTitle");

        private void SaveConfig()
        {
            NormalizeConfig();
            helper.WriteConfig(config);
            runtime.Configure(
                config,
                "config saved");
            RefreshInputBindings();
        }

        private void ResetConfig()
        {
            config = new ZoomConfig();
            NormalizeConfig();
        }

        private void NormalizeConfig()
        {
            config = config.Copy();
            config.IncreaseKey =
                DtmKeybindList
                    .Parse(config.IncreaseKey)
                    .ToString();
            config.DecreaseKey =
                DtmKeybindList
                    .Parse(config.DecreaseKey)
                    .ToString();
        }

        private void RefreshInputBindings()
        {
            if (!config.Enabled)
            {
                if (inputSubscribed)
                {
                    helper.Events.Input.KeybindPressed -=
                        OnKeybindPressed;
                    inputSubscribed = false;
                }
                DisposeInputRegistrations();
                return;
            }

            DtmKeybindList increase =
                DtmKeybindList.Parse(
                    config.IncreaseKey);
            DtmKeybindList decrease =
                DtmKeybindList.Parse(
                    config.DecreaseKey);
            if (increaseRegistration == null)
            {
                increaseRegistration =
                    helper.Input.RegisterKeybind(
                        IncreaseKeybindId,
                        increase,
                        DtmInputScope.Gameplay);
            }
            else
            {
                increaseRegistration.Update(
                    increase,
                    DtmInputScope.Gameplay);
            }
            if (decreaseRegistration == null)
            {
                decreaseRegistration =
                    helper.Input.RegisterKeybind(
                        DecreaseKeybindId,
                        decrease,
                        DtmInputScope.Gameplay);
            }
            else
            {
                decreaseRegistration.Update(
                    decrease,
                    DtmInputScope.Gameplay);
            }
            if (!inputSubscribed)
            {
                helper.Events.Input.KeybindPressed +=
                    OnKeybindPressed;
                inputSubscribed = true;
            }
        }

        private void UnsubscribeAll()
        {
            if (inputSubscribed)
            {
                helper.Events.Input.KeybindPressed -=
                    OnKeybindPressed;
                inputSubscribed = false;
            }
            if (saveSubscribed)
            {
                helper.Events.Save.SaveLoaded -=
                    OnSaveLoaded;
                saveSubscribed = false;
            }
            if (titleSubscribed)
            {
                helper.Events.GameLoop.ReturnedToTitle -=
                    OnReturnedToTitle;
                titleSubscribed = false;
            }
            DisposeInputRegistrations();
        }

        private void DisposeInputRegistrations()
        {
            increaseRegistration?.Dispose();
            decreaseRegistration?.Dispose();
            increaseRegistration = null;
            decreaseRegistration = null;
        }

        private string T(
            string key,
            string fallback) =>
            helper.Translation.Get(key, fallback);

        private static void TryCleanup(
            Action cleanup,
            ICollection<Exception> failures)
        {
            try
            {
                cleanup();
            }
            catch (Exception ex)
            {
                failures.Add(ex);
            }
        }
    }
}
