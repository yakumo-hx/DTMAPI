using System;
using System.IO;
using System.Reflection;
using DTMAPI.Abstractions;

namespace DTMAPI.DebugConsole
{
    internal sealed class ProductRuntimeAdapter :
        IDebugConsoleRuntime,
        IDebugConsoleInputRuntime,
        IDebugConsoleModalRuntime,
        IDebugConsoleNativeRuntime
    {
        private const string MenuId = "DTMAPI.DebugConsole";
        private readonly IDtmHelper helper;
        private readonly object modal;
        private readonly MethodInfo tryOpenModal;
        private readonly MethodInfo isModalOpen;
        private readonly MethodInfo closeModal;

        internal ProductRuntimeAdapter(IDtmHelper helper)
        {
            this.helper = helper ??
                throw new ArgumentNullException(nameof(helper));
            modal = helper.UI;
            Type modalType = modal.GetType();
            tryOpenModal = ResolveModalMethod(
                modalType,
                "TryOpenModal");
            isModalOpen = ResolveModalMethod(
                modalType,
                "IsModalOpen");
            closeModal = ResolveModalMethod(
                modalType,
                "CloseModal");
            string configPath =
                helper.Config.GetConfigPath(helper.ModManifest);
            DtmApiPath =
                Directory.GetParent(
                    Directory.GetParent(configPath)?.FullName ??
                    string.Empty)?.FullName ??
                string.Empty;
            if (DtmApiPath.Length == 0)
            {
                throw new InvalidOperationException(
                    "Could not resolve the DTMAPI state directory from the owner-bound config path.");
            }
        }

        public IMonitor RuntimeMonitor => helper.Monitor;
        IMonitor IDebugConsoleNativeRuntime.Monitor => helper.Monitor;
        public string NativeOwnerLabel => "ProductNative";
        public IDebugConsoleInputRuntime Input => this;
        public IDebugConsoleModalRuntime UI => this;
        public ITranslationHelper Translation => helper.Translation;
        public string ApiVersion => "0.5.5";
        public string DtmApiPath { get; }
        public string EvidencePath =>
            Path.Combine(DtmApiPath, "evidence");

        public bool ModalOpen
        {
            set => DebugConsoleInputGate.ModalOpen = value;
        }

        public bool NativeInputDrainActive
        {
            set => DebugConsoleInputGate.NativeInputDrainActive = value;
        }

        public bool IsOpen =>
            isModalOpen.Invoke(
                modal,
                new object[] { MenuId }) is bool open &&
            open;
        public string ActiveMenuId => IsOpen ? MenuId : string.Empty;

        public bool OpenOwnerBoundCustomMenu(
            string menuId,
            string ownerId)
        {
            if (!ownerId.Equals(
                    helper.ModManifest.UniqueID,
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            return tryOpenModal.Invoke(
                modal,
                new object[] { menuId }) is bool opened &&
                opened;
        }

        public void Close()
        {
            closeModal.Invoke(
                modal,
                new object[] { MenuId });
        }

        public void Suppress(string button)
        {
            helper.Input.Suppress(button);
        }

        public bool HasOwnerLegacyButtonRegistration(
            string ownerId,
            string button) => false;

        public bool HasOwnerTypedKeybindForButton(
            string ownerId,
            string button) => true;

        public bool TryDispatchLegacyModalButtonPressed(
            string menuId,
            string ownerId,
            string button) => false;

        public void SetHookStatus(
            string hookId,
            string status,
            string source,
            string details)
        {
            helper.Monitor.LogOnce(
                "debugconsole-status-" + hookId + "-" + status,
                "DebugConsole status " + hookId +
                "=" + status +
                " source=" + source +
                " details=" + details + ".");
        }

        public void RecordError(
            string owner,
            string message,
            string details)
        {
            helper.Monitor.Log(
                owner + ": " + message + " " + details,
                LogLevel.Error);
        }

        public System.Collections.Generic.IReadOnlyList<IContentItemInfo>
            GetIndexedItems() =>
            helper.Content.GetAllIndexedItems();

        public IContentItemInfo? GetIndexedItem(string itemId) =>
            helper.Content.GetAnyIndexedItem(itemId);

        public void SetCreativeHookDemand(bool enabled)
        {
            // ProductNative owns the complete hook set for its assembly lifetime.
            // The callback gate itself is toggled by DebugConsoleNativeActions.
        }

        public void SetMovementMultiplier(
            object? player,
            double multiplier) =>
            DebugConsoleMovementHooks.SetMultiplier(
                player,
                multiplier);

        public void Status(
            string id,
            string status,
            string source,
            string details) =>
            SetHookStatus(id, status, source, details);

        public void Error(string operation, Exception error) =>
            RecordError(
                "DTMAPI.DebugConsoleMod",
                operation + " failed.",
                error.ToString());

        private static MethodInfo ResolveModalMethod(
            Type type,
            string name)
        {
            return type.GetMethod(
                       name,
                       BindingFlags.Public |
                       BindingFlags.Instance,
                       null,
                       new[] { typeof(string) },
                       null) ??
                throw new InvalidOperationException(
                    "DTMAPI 0.5.5 owner-bound modal helper method '" +
                    name +
                    "' is unavailable.");
        }
    }
}
