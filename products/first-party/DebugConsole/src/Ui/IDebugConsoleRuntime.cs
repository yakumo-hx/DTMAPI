using DTMAPI.Abstractions;
using System.Collections.Generic;

namespace DTMAPI.DebugConsole
{
    internal interface IDebugConsoleRuntime
    {
        IMonitor RuntimeMonitor { get; }
        IDebugConsoleInputRuntime Input { get; }
        IDebugConsoleModalRuntime UI { get; }
        ITranslationHelper Translation { get; }
        string ApiVersion { get; }
        string DtmApiPath { get; }
        bool ModalOpen { set; }
        bool NativeInputDrainActive { set; }
        void SetHookStatus(
            string hookId,
            string status,
            string source,
            string details);
        void RecordError(
            string owner,
            string message,
            string details);
        bool TryDispatchLegacyModalButtonPressed(
            string menuId,
            string ownerId,
            string button);
    }

    internal interface IDebugConsoleNativeRuntime
    {
        IMonitor Monitor { get; }
        string NativeOwnerLabel { get; }
        IReadOnlyList<IContentItemInfo> GetIndexedItems();
        IContentItemInfo? GetIndexedItem(string itemId);
        string EvidencePath { get; }
        void SetCreativeHookDemand(bool enabled);
        void SetMovementMultiplier(object? player, double multiplier);
        void Status(string id, string status, string source, string details);
        void Error(string operation, System.Exception error);
    }

    internal interface IDebugConsoleInputRuntime
    {
        void Suppress(string button);
        bool HasOwnerLegacyButtonRegistration(
            string ownerId,
            string button);
        bool HasOwnerTypedKeybindForButton(
            string ownerId,
            string button);
    }

    internal interface IDebugConsoleModalRuntime
    {
        bool IsOpen { get; }
        string ActiveMenuId { get; }
        bool OpenOwnerBoundCustomMenu(
            string menuId,
            string ownerId);
        void Close();
    }
}
