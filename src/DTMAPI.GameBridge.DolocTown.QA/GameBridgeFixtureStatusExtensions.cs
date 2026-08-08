namespace DTMAPI.GameBridge.DolocTown.QA
{
    internal static class GameBridgeFixtureStatusExtensions
    {
        internal static void PublishG3Status(this GameBridgeFixtureAccess access, string hookId, string status, string source, string details) => access.SetHookStatus(hookId, status, source, details);
        internal static void PublishG4Status(this GameBridgeFixtureAccess access, string hookId, string status, string source, string details) => access.SetHookStatus(hookId, status, source, details);
        internal static void PublishG5Status(this GameBridgeFixtureAccess access, string status, string details) => access.SetHookStatus("Smoke.QaG5WorldMutation", status, "optional QA G5 world-mutation coordinator", details);
        internal static void PublishG6Status(this GameBridgeFixtureAccess access, string status, string details) => access.SetHookStatus("Smoke.QaG6Lifecycle", status, "optional QA G6 compatibility/lifecycle/save-load coordinator", details);
        internal static CustomEntityRegistrationFixtureSession OpenCustomEntityRegistrationSession(this GameBridgeFixtureAccess access) => new CustomEntityRegistrationFixtureSession(access.Runtime, access.RunId);
    }
}
