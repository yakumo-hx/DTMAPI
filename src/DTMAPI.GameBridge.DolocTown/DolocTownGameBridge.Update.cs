namespace DTMAPI.GameBridge.DolocTown
{
    public sealed partial class DolocTownGameBridge
    {
        public void Update()
        {
            RefreshUiContext();
            UpdateRuntimeAutomation();
            SmokeUpdate();
        }
    }
}
