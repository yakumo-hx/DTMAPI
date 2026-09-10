using System;
using DTMAPI.Abstractions;

namespace DTMAPI.DebugConsole
{
    internal sealed class DebugConsoleCompatibilityService :
        IDebugConsoleApi,
        IOwnerBoundApiHost
    {
        private readonly CompatibilityDebugConsoleRuntimeAdapter runtime;
        private readonly DebugConsoleUi ui;
        private readonly CompatibilityDebugActionService actionService;
        private readonly DebugConsoleNativeActions actions;
        private bool warningLogged;

        internal DebugConsoleCompatibilityService(
            DTMAPI.Core.Runtime.DtmApiRuntime runtime,
            CompatibilityDebugActionService actionService)
        {
            this.actionService = actionService ??
                throw new ArgumentNullException(nameof(actionService));
            this.runtime = actionService.Runtime;
            actions = actionService.Actions;
            ui = new DebugConsoleUi(this.runtime);
        }

        public bool IsOpen => ui.IsOpen;

        public void Bind(
            IManifest owner,
            IInventoryDebugApi? inventoryApi,
            IWeatherDebugApi? weatherApi,
            ITeleportDebugApi? teleportApi,
            ITimeDebugApi? timeApi,
            IMovementDebugApi? movementApi,
            IInstantSaveDebugApi? instantSaveApi = null)
        {
            WarnFrozen();
            ui.Bind(
                owner,
                inventoryApi == null ? null : actions,
                weatherApi == null ? null : actions,
                teleportApi == null ? null : actions,
                timeApi == null ? null : actions,
                movementApi == null ? null : actions,
                instantSaveApi == null ? null : actions);
        }

        public void BindAdvanced(
            IManifest owner,
            IAdvancedDebugApi? advancedDebugApi)
        {
            WarnFrozen();
            ui.BindAdvanced(
                owner,
                advancedDebugApi == null ? null : actions);
        }

        public void SetLanguage(
            IManifest owner,
            string language)
        {
            WarnFrozen();
            runtime.Monitor.LogOnce(
                "debug-console-compat-language-ignored",
                "The frozen DebugConsole SetLanguage call is retained for ABI compatibility but the UI now follows the game language.",
                LogLevel.Info);
        }

        public void Open(IManifest owner, string reason) =>
            ui.Open(owner, reason);

        public void Close(IManifest owner, string reason) =>
            ui.Close(owner, reason);

        public void Toggle(IManifest owner, string reason) =>
            ui.Toggle(owner, reason);

        public BridgeFeatureStatus GetStatus(string uniqueId) =>
            ui.GetStatus(uniqueId);

        public int CountOwnerResources(string ownerId) =>
            ui.CountOwnerResources(ownerId) +
            actionService.CountOwnerResources(ownerId);

        public int RemoveOwner(string ownerId, string reason)
        {
            int removed = ui.RemoveOwner(ownerId, reason);
            return removed + actionService.RemoveOwner(ownerId, reason);
        }

        public void Update()
        {
            ui.Update();
        }

        public void ResetForSaveBoundary(
            int? saveSlot,
            bool isNewGame) =>
            ui.ResetForSaveBoundary(saveSlot, isNewGame);

        public void ResetForTitleBoundary()
        {
            ui.ResetForTitleBoundary();
        }

        public void Shutdown(string reason)
        {
            ui.Shutdown(reason);
        }

        public string GetLifecycleSummary() =>
            ui.GetLifecycleSummary() +
            "; " + actionService.GetLifecycleSummary();

        public string GetOwnerObjectGraphSummary() =>
            ui.GetOwnerObjectGraphSummary();

        public bool ConsumedInputThisFrame =>
            ui.ConsumedInputThisFrame;

        private void WarnFrozen()
        {
            if (warningLogged)
                return;
            warningLogged = true;
            runtime.Monitor.Log(
                "Loaded frozen DebugConsole 0.3.1 compatibility UI. Install/update the SDK-generated 1.1 Advanced product to use ProductNative ownership.",
                LogLevel.Warn);
        }
    }
}
