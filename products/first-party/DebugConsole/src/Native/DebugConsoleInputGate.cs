namespace DTMAPI.DebugConsole
{
    internal static class DebugConsoleInputGate
    {
        internal static bool ModalOpen { get; set; }
        internal static bool NativeInputDrainActive { get; set; }

        internal static bool ShouldSuppress =>
            ModalOpen || NativeInputDrainActive;

        internal static void Reset()
        {
            ModalOpen = false;
            NativeInputDrainActive = false;
        }
    }
}
