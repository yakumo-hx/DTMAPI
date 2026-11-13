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
        string ProductVersion { get; }
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
    }

    internal interface IDebugConsoleNativeRuntime
    {
        IMonitor Monitor { get; }
        string NativeOwnerLabel { get; }
        IReadOnlyList<AnimalContentSource> GetAnimalContentSources();
        IReadOnlyDictionary<string, InventoryDebugSourceGroup>? GetContentSources(string table);
        void SetMovementMultiplier(object? player, double multiplier);
        void Status(string id, string status, string source, string details);
        void Error(string operation, System.Exception error);
    }

    internal interface IDebugConsoleInputRuntime
    {
        void Suppress(string button);
    }

    internal interface IDebugConsoleModalRuntime
    {
        DtmModalOpenResult OpenModal(
            string menuId,
            string ownerId);
    }
}
