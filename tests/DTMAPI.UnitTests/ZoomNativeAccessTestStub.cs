namespace DTMAPI.Zoom
{
    // The focused runtime tests inject native readers/writers. This compile-time
    // stub keeps the ProductNative state machine independent from game binaries.
    internal static class ZoomNativeAccess
    {
        internal static bool TryReadOrthographicSize(
            out double value)
        {
            value = 0d;
            return false;
        }

        internal static bool TryWriteOrthographicSize(
            double value) =>
            false;

        internal static bool TryRefreshCameraControllerResolution() =>
            true;

        internal static bool IsMainCameraDefinitelyAbsent() =>
            false;
    }
}
