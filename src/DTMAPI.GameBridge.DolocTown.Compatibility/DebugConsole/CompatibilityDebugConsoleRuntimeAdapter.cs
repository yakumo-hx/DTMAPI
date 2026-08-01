using System;
using System.Collections.Generic;
using System.Globalization;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.DebugConsole
{
    internal sealed class CompatibilityDebugConsoleRuntimeAdapter :
        IDebugConsoleRuntime,
        IDebugConsoleInputRuntime,
        IDebugConsoleModalRuntime,
        IDebugConsoleNativeRuntime
    {
        private readonly DtmApiRuntime runtime;
        private readonly CompatibilityDebugConsoleHookOwner hooks;
        private readonly ITranslationHelper translation =
            new CompatibilityTranslation();

        internal CompatibilityDebugConsoleRuntimeAdapter(
            DtmApiRuntime runtime,
            bool installNativeHooks = true)
        {
            this.runtime = runtime ??
                throw new ArgumentNullException(nameof(runtime));
            hooks = new CompatibilityDebugConsoleHookOwner(
                runtime,
                installNativeHooks);
        }

        internal CompatibilityDebugConsoleRuntimeAdapter(
            DtmApiRuntime runtime,
            ICompatibilityDebugConsolePatchBackend backend)
        {
            this.runtime = runtime ??
                throw new ArgumentNullException(nameof(runtime));
            hooks = new CompatibilityDebugConsoleHookOwner(
                runtime,
                backend);
        }

        public IMonitor RuntimeMonitor => runtime.RuntimeMonitor;
        public IMonitor Monitor => runtime.RuntimeMonitor;
        public string NativeOwnerLabel => "Compatibility";
        public IDebugConsoleInputRuntime Input => this;
        public IDebugConsoleModalRuntime UI => this;
        public ITranslationHelper Translation => translation;
        public string ApiVersion => DtmApiRuntime.ApiVersion;
        public string DtmApiPath => runtime.Paths.DtmApiPath;
        public string EvidencePath => runtime.Paths.EvidencePath;
        public bool IsOpen => runtime.UI.IsOpen &&
            runtime.UI.ActiveMenuId.Equals(
                "DTMAPI.DebugConsole",
                StringComparison.OrdinalIgnoreCase);
        public string ActiveMenuId =>
            runtime.UI.ActiveMenuId;

        public bool ModalOpen
        {
            set => hooks.SetModalOpen(value);
        }

        public bool NativeInputDrainActive
        {
            set => hooks.SetNativeInputDrainActive(value);
        }

        internal int InstalledPatchCount =>
            hooks.InstalledPatchCount;
        internal int PatchOperationCount =>
            hooks.PatchOperationCount;
        internal int TopologyTransitionCount =>
            hooks.TopologyTransitionCount;
        internal int StatusPublicationCount =>
            hooks.StatusPublicationCount;
        internal bool CleanupPending =>
            hooks.CleanupPending;

        internal void ShutdownHooks(string reason) =>
            hooks.Shutdown(reason);

        public bool OpenOwnerBoundCustomMenu(
            string menuId,
            string ownerId) =>
            runtime.UI.TryOpenOwnerBoundCustomMenu(
                menuId,
                ownerId);

        public void Close() => runtime.UI.Close();

        public void Suppress(string button) =>
            runtime.Input.Suppress(button);

        public bool HasOwnerLegacyButtonRegistration(
            string ownerId,
            string button) =>
            runtime.Input.HasOwnerLegacyButtonRegistration(
                ownerId,
                button);

        public bool HasOwnerTypedKeybindForButton(
            string ownerId,
            string button) =>
            runtime.Input.HasOwnerTypedKeybindForButton(
                ownerId,
                button);

        public bool TryDispatchLegacyModalButtonPressed(
            string menuId,
            string ownerId,
            string button) =>
            runtime.TryDispatchLegacyModalButtonPressed(
                menuId,
                ownerId,
                button);

        public void SetHookStatus(
            string hookId,
            string status,
            string source,
            string details) =>
            runtime.SetHookStatus(
                hookId,
                status,
                source,
                details);

        public void RecordError(
            string owner,
            string message,
            string details) =>
            runtime.Diagnostics.RecordError(
                owner,
                message,
                details);

        public IReadOnlyList<IContentItemInfo> GetIndexedItems() =>
            runtime.GetIndexedContentItems();

        public IContentItemInfo? GetIndexedItem(string itemId) =>
            runtime.GetIndexedContentItem(itemId);

        public void SetCreativeHookDemand(bool enabled) =>
            hooks.SetCreativeEnabled(enabled);

        public void SetMovementMultiplier(
            object? player,
            double multiplier) =>
            hooks.SetMovementMultiplier(player, multiplier);

        public void Status(
            string id,
            string status,
            string source,
            string details) =>
            SetHookStatus(id, status, source, details);

        public void Error(string operation, Exception error) =>
            RecordError(
                "DTMAPI.DebugConsoleHost",
                operation + " failed.",
                error.ToString());

        private sealed class CompatibilityTranslation :
            ITranslationHelper
        {
            public string Language =>
                CultureInfo.CurrentUICulture.Name;

            public string Get(
                string key,
                string fallback = "") =>
                fallback ?? string.Empty;
        }
    }
}
