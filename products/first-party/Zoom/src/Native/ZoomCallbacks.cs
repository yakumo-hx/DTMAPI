using System;
using System.Threading;

namespace DTMAPI.Zoom
{
    internal static class ZoomCallbacks
    {
        private static ZoomNativeRuntime? runtime;

        internal static void Attach(
            ZoomNativeRuntime value) =>
            Volatile.Write(ref runtime, value);

        internal static void Detach(
            ZoomNativeRuntime value)
        {
            if (ReferenceEquals(
                    Volatile.Read(ref runtime),
                    value))
            {
                Volatile.Write(ref runtime, null);
            }
        }

        public static void DolocApiSetEnvCameraPostfix() =>
            Volatile.Read(ref runtime)?
                .OnEnvironmentReset();

        public static void CameraControllerRefreshResolutionPrefix(
            out ZoomNativeRefreshState __state)
        {
            ZoomNativeRuntime? current =
                Volatile.Read(ref runtime);
            __state = current == null
                ? default
                : current.BeforeNativeRefreshResolution();
        }

        public static Exception? CameraControllerRefreshResolutionFinalizer(
            Exception? __exception,
            ZoomNativeRefreshState __state)
        {
            ZoomNativeRuntime? current =
                __state.Runtime ??
                Volatile.Read(ref runtime);
            return current == null
                ? __exception
                : current.AfterNativeRefreshResolution(
                    __state,
                    __exception);
        }
    }
}
