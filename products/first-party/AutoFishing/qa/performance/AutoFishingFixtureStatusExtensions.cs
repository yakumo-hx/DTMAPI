namespace DTMAPI.GameBridge.DolocTown.QA
{
    internal static class AutoFishingFixtureStatusExtensions
    {
        internal static void PublishAutoFishingPerformanceStatus(this GameBridgeFixtureAccess access, string hookId, string status, string source, string details) =>
            access.SetHookStatus(hookId, status, source, details);
    }
}
