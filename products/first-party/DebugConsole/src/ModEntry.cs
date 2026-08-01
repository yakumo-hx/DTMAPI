using System;
using System.Collections.Generic;
using DTMAPI.Abstractions;

namespace DTMAPI.DebugConsole
{
    public sealed class ModEntry : DtmMod, IDisposable
    {
        private const string ToggleId = "debug-console.toggle";
        private const string CloseId = "debug-console.close";
        private IDtmHelper helper = null!;
        private DebugConsoleConfig config = new DebugConsoleConfig();
        private ProductRuntimeAdapter runtime = null!;
        private DebugConsoleNativeActions actions = null!;
        private DebugConsoleUi ui = null!;
        private DebugConsoleHookInstaller hooks = null!;
        private IInputRegistration? toggle;
        private IInputRegistration? close;
        private readonly DebugConsoleSaveSessionGate
            saveSession =
                new DebugConsoleSaveSessionGate();
        private bool updateSubscribed;
        private bool saveSubscribed;
        private bool titleSubscribed;
        private bool inputSubscribed;
        private bool disposed;

        public override void Entry(IDtmHelper helper)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(ModEntry));
            this.helper = helper ??
                throw new ArgumentNullException(nameof(helper));
            var failures = new List<Exception>();
            try
            {
                config = helper.ReadConfig<DebugConsoleConfig>() ??
                    new DebugConsoleConfig();
                config.Normalize();
                runtime = new ProductRuntimeAdapter(helper);
                actions = new DebugConsoleNativeActions(runtime);
                ui = new DebugConsoleUi(runtime);
                hooks = new DebugConsoleHookInstaller();
                hooks.InstallAtomically();
                ui.Bind(
                    helper.ModManifest,
                    actions,
                    actions,
                    actions,
                    actions,
                    actions,
                    actions);
                ui.BindAdvanced(helper.ModManifest, actions);
                ui.SetLanguage(helper.ModManifest, config.Language);
                toggle = helper.Input.RegisterKeybind(
                    ToggleId,
                    "Y",
                    DtmInputScope.SaveLoaded);
                close = helper.Input.RegisterKeybind(
                    CloseId,
                    "Escape",
                    DtmInputScope.SaveLoaded);
                helper.Events.Save.SaveLoaded += OnSaveLoaded;
                saveSubscribed = true;
                helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
                titleSubscribed = true;
                helper.Events.Input.KeybindPressed += OnKeybindPressed;
                inputSubscribed = true;
                helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
                updateSubscribed = true;
                RegisterConfigMenu();
                helper.Monitor.Log(
                    T(
                        "mod.loaded",
                        "Y-Key Console loaded; ProductNative UI and actions activate only after SaveLoaded.") +
                    " owner=" + DebugConsoleHookInstaller.HarmonyOwner +
                    " compatibilityApisConsumed=false.");
            }
            catch (Exception error)
            {
                failures.Add(error);
                Cleanup(
                    "entry-failed",
                    platformRootsAlreadyRemoved: false,
                    failures);
                if (failures.Count == 1)
                    throw;
                throw new AggregateException(
                    "DebugConsole Entry failed and rollback was incomplete.",
                    failures);
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;
            var failures = new List<Exception>();
            Cleanup(
                "owner-deactivation",
                platformRootsAlreadyRemoved: true,
                failures);
            if (failures.Count > 0)
            {
                throw new AggregateException(
                    "DebugConsole owner cleanup failed.",
                    failures);
            }
            disposed = true;
        }

        private void OnSaveLoaded(
            object sender,
            SaveLoadedEventArgs e)
        {
            saveSession.Enter(
                () =>
                    actions.RestoreTransientState(
                        "SaveLoaded"),
                () =>
                    ui.ResetForSaveBoundary(
                        e.SaveSlot,
                        e.IsNewGame));
            helper.Monitor.Log(
                "DebugConsole SaveLoaded slot=" +
                (e.SaveSlot?.ToString() ?? "unknown") +
                " inputPatches=3 totalPatches=" +
                hooks.InstalledPatchCount + ".");
        }

        private void OnReturnedToTitle(
            object sender,
            ReturnedToTitleEventArgs e)
        {
            saveSession.Leave();
            ui.ResetForTitleBoundary();
            actions.RestoreTransientState("ReturnedToTitle");
            helper.Monitor.Log(
                "DebugConsole ReturnedToTitle cleanup UI={" +
                ui.GetOwnerObjectGraphSummary() +
                "} leases={" + actions.BuildLeaseSummary() +
                "} patches=" + hooks.InstalledPatchCount + ".");
        }

        private void OnKeybindPressed(
            object sender,
            KeybindPressedEventArgs e)
        {
            if (!e.OwnerId.Equals(
                    helper.ModManifest.UniqueID,
                    StringComparison.OrdinalIgnoreCase))
                return;
            if (e.KeybindId.Equals(ToggleId, StringComparison.OrdinalIgnoreCase))
            {
                if (saveSession.IsActive)
                {
                    bool wasOpen = ui.IsOpen;
                    ui.Toggle(helper.ModManifest, "hotkey Y");
                    RestoreAfterConsoleClose(wasOpen, "hotkey Y");
                }
                return;
            }
            if (e.KeybindId.Equals(CloseId, StringComparison.OrdinalIgnoreCase) &&
                ui.IsOpen)
            {
                ui.Close(helper.ModManifest, "Escape");
                RestoreAfterConsoleClose(true, "Escape");
            }
        }

        private void OnUpdateTicked(
            object sender,
            UpdateTickedEventArgs e)
        {
            if (!saveSession.IsActive)
                return;
            actions.Update();
            bool wasOpen = ui.IsOpen;
            ui.Update();
            RestoreAfterConsoleClose(wasOpen, "console UI close");
        }

        private void RestoreAfterConsoleClose(
            bool wasOpen,
            string reason)
        {
            if (wasOpen && !ui.IsOpen)
                actions.RestoreModalScopedState(reason);
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
                () => T("mod.name", "Y-Key Console"));
            menu.AddSectionTitle(
                helper.ModManifest,
                () => T("config.section.main", "Console"));
            menu.AddChoiceOption(
                helper.ModManifest,
                () => T("config.language.name", "Display language"),
                () => T(
                    "config.language.tooltip",
                    "Auto follows the DTMAPI language; Chinese and English may be selected explicitly."),
                () => config.Language,
                value => config.Language = value,
                new[] { "Auto", "schinese", "english" });
        }

        private void ResetConfig()
        {
            config = new DebugConsoleConfig();
            config.Normalize();
        }

        private void SaveConfig()
        {
            config.Normalize();
            helper.WriteConfig(config);
            ui.SetLanguage(helper.ModManifest, config.Language);
        }

        private void SetUpdateSubscription(bool enabled)
        {
            if (enabled == updateSubscribed)
                return;
            if (enabled)
                helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            else
                helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
            updateSubscribed = enabled;
        }

        private void Cleanup(
            string reason,
            bool platformRootsAlreadyRemoved,
            IList<Exception> failures)
        {
            if (!platformRootsAlreadyRemoved)
            {
                Try(
                    () => SetUpdateSubscription(false),
                    failures);
                if (inputSubscribed)
                {
                    Try(
                        () => helper.Events.Input.KeybindPressed -= OnKeybindPressed,
                        failures);
                }
                if (saveSubscribed)
                {
                    Try(
                        () => helper.Events.Save.SaveLoaded -= OnSaveLoaded,
                        failures);
                }
                if (titleSubscribed)
                {
                    Try(
                        () => helper.Events.GameLoop.ReturnedToTitle -= OnReturnedToTitle,
                        failures);
                }
                Try(() => toggle?.Dispose(), failures);
                Try(() => close?.Dispose(), failures);
            }
            updateSubscribed = false;
            inputSubscribed = false;
            saveSubscribed = false;
            titleSubscribed = false;
            Try(() => ui?.Shutdown(reason), failures);
            Try(() => actions?.RestoreTransientState(reason), failures);
            Try(() => hooks?.Unpatch(), failures);
            toggle = null;
            close = null;
            saveSession.Leave();
        }

        private static void Try(
            Action action,
            IList<Exception> failures)
        {
            try
            {
                action();
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        }

        private string T(string key, string fallback) =>
            helper.Translation.Get(key, fallback);
    }
}
