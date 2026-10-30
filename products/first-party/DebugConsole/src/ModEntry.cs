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
        private DebugConsoleConfig config = new DebugConsoleConfig();
        private int savedRightClickCount = 10;
        private int savedCropMaturityRadius = 25;

        public override void Entry(IDtmHelper helper)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(ModEntry));
            this.helper = helper ??
                throw new ArgumentNullException(nameof(helper));
            var failures = new List<Exception>();
            try
            {
                runtime = new ProductRuntimeAdapter(helper);
                config = helper.ReadConfig<DebugConsoleConfig>();
                config.CustomTimeMultiplier = DebugConsoleConfig.Normalize(config.CustomTimeMultiplier);
                config.RightClickCount = DebugConsoleConfig.NormalizeDispatchCount(config.RightClickCount);
                savedRightClickCount = config.RightClickCount;
                config.CropMaturityRadius = DebugConsoleConfig.NormalizeCropRadius(config.CropMaturityRadius);
                savedCropMaturityRadius = config.CropMaturityRadius;
                RegisterConfigMenu();
                actions = new DebugConsoleNativeActions(runtime);
                actions.SetCropMaturityContext(helper.GetRequiredService<IDtmRuntimeContext>("0.7.5", "0.7.5"));
                actions.SetDeletionContext(helper.GetRequiredService<IDtmRuntimeContext>("0.7.5", "0.7.5"));
                ui = new DebugConsoleUi(runtime, () => config.CustomTimeMultiplier, () => savedRightClickCount, () => savedCropMaturityRadius);
                DebugConsoleModeHooks.Actions = actions;
                DebugConsoleModeHooks.EndSessionBeforeLoad = EndSessionBeforeNativeLoad;
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

        // The existing Core preparation boundary keeps native observers and the
        // owner instance alive if releasing a mode still needs a retry.
        public void DtmApiPrepareOwnerDeactivation(string reason)
        {
            saveSession.Leave();
            // Core has already entered Deactivating: owner-bound helper event
            // access is closed. Core removes subscriptions after preparation;
            // on a failed preparation the inactive save gate lets Update retire
            // its subscription after Core restores the Active owner state.
            var failures = new List<Exception>();
            Try(() => actions?.EndSaveSession(reason), failures);
            Try(() => ui?.Shutdown(reason), failures);
            if (failures.Count > 0)
                throw new AggregateException("DebugConsole session preparation remains pending.", failures);
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
                    actions.EndSaveSession(
                        "SaveLoaded"),
                () => {
                    ui.ResetForSaveBoundary(
                        e.SaveSlot,
                        e.IsNewGame);
                    actions.BeginSaveSession();
                });
            actions.InvalidateCatalogs();
            RefreshUpdateSubscription();
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
            actions.EndSaveSession("ReturnedToTitle");
            actions.InvalidateCatalogs();
            RefreshUpdateSubscription();
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
                if (saveSession.IsActive && actions.IsCurrentSaveSession)
                {
                    if (ui.TryConsumeFocusedTextInputY())
                        return;
                    ui.Toggle(helper.ModManifest, "hotkey Y");
                    RefreshUpdateSubscription();
                }
                return;
            }
            if (e.KeybindId.Equals(CloseId, StringComparison.OrdinalIgnoreCase) &&
                ui.IsOpen)
            {
                ui.Close(helper.ModManifest, "Escape");
                RefreshUpdateSubscription();
            }
        }

        private void OnUpdateTicked(
            object sender,
            UpdateTickedEventArgs e)
        {
            if (!saveSession.IsActive || !actions.IsCurrentSaveSession)
            {
                RefreshUpdateSubscription();
                return;
            }
            if (actions.NeedsUpdate)
                actions.Update();
            if (ui.NeedsUpdate)
                ui.Update();
            RefreshUpdateSubscription();
        }

        private void RefreshUpdateSubscription()
        {
            bool needed = saveSession.IsActive && actions.IsCurrentSaveSession &&
                ((ui != null && ui.NeedsUpdate) ||
                 (actions != null && actions.NeedsUpdate));
            SetUpdateSubscription(needed);
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
            bool modesReleased = false;
            Try(() => { actions?.EndSaveSession(reason); modesReleased = true; }, failures);
            // Keep the native-write observer and load boundary until the retained
            // restoration responsibility is resolved. Effects are already inactive.
            if (modesReleased)
                Try(() => hooks?.Unpatch(), failures);
            toggle = null;
            close = null;
            saveSession.Leave();
        }

        private bool EndSessionBeforeNativeLoad()
        {
            saveSession.Leave();
            SetUpdateSubscription(false);
            try
            {
                ui.ResetForTitleBoundary();
                actions.EndSaveSession("native load entry");
                return true;
            }
            catch (Exception error)
            {
                helper.Monitor.Log("DebugConsole session restoration is pending; native load was not started. Retry loading after restoration succeeds. " + error, LogLevel.Error);
                return false;
            }
        }

        private void RegisterConfigMenu()
        {
            IDtmConfigMenuApi? menu = helper.ModRegistry.GetApi<IDtmConfigMenuApi>("DTMAPI.ModConfigMenu");
            if (menu == null)
                throw new InvalidOperationException("DTMAPI.ModConfigMenu is required for DebugConsole settings.");
            menu.Register(helper.ModManifest, () => config = new DebugConsoleConfig(), () =>
            {
                config.CustomTimeMultiplier = DebugConsoleConfig.Normalize(config.CustomTimeMultiplier);
                config.RightClickCount = DebugConsoleConfig.NormalizeDispatchCount(config.RightClickCount);
                config.CropMaturityRadius = DebugConsoleConfig.NormalizeCropRadius(config.CropMaturityRadius);
                helper.WriteConfig(config);
                // Pending previews, Reset and a failed save cannot change dispatch.
                savedRightClickCount = config.RightClickCount;
                savedCropMaturityRadius = config.CropMaturityRadius;
                ui?.RefreshDispatchCount();
            });
            menu.SetDisplayName(helper.ModManifest, () => T("mod.name", "Y Key Console"));
            menu.AddNumberOption(helper.ModManifest,
                () => T("config.customTime.name", "Custom time multiplier"),
                () => T("config.customTime.description", "Integer 1–20. Saving does not change current speed; click Custom time in Y to apply."),
                () => config.CustomTimeMultiplier,
                value => config.CustomTimeMultiplier = DebugConsoleConfig.Normalize(value),
                1, 20, 1);
            menu.AddNumberOption(helper.ModManifest,
                () => T("config.rightClickCount.name", "Right-click quantity"),
                () => T("config.rightClickCount.description", "Integer 1–100 for items, monsters and animals. Left-click gives 1. Saving does not give or spawn anything."),
                () => config.RightClickCount,
                value => config.RightClickCount = DebugConsoleConfig.NormalizeDispatchCount(value),
                1, 100, 1);
            menu.AddNumberOption(helper.ModManifest,
                () => T("config.cropRadius.name", "Crop maturity radius"),
                () => T("config.cropRadius.description", "Integer 5–100 cells; default 25. Next click affects only this room. A basin is included if any covered cell touches the circle; anchor is used only when coverage is absent."),
                () => config.CropMaturityRadius,
                value => config.CropMaturityRadius = DebugConsoleConfig.NormalizeCropRadius(value),
                5, 100, 1);
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
