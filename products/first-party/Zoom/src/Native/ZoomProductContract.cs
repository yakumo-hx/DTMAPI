namespace DTMAPI.Zoom
{
    internal interface IZoomHookOwner
    {
        bool IsInstalled { get; }

        int InstalledPatchCount { get; }

        void InstallAtomically(ZoomNativeRuntime runtime);

        void UnpatchOwnedHooks(ZoomNativeRuntime runtime);
    }

    internal static class ZoomProductContract
    {
        internal const string HarmonyOwner =
            "DTMAPI.ZoomMod.Native";

        internal const string CompatibilityOwner =
            "dtmapi.gamebridge.doloctown.camera.compatibility";

        internal const int ExpectedHookCount = 7;
    }
}
