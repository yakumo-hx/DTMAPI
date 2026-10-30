using System;
using System.Threading;
namespace DTMAPI.Zoom
{
    internal static class ZoomCallbacks
    {
        private static ZoomNativeRuntime? runtime;
        internal static void Attach(ZoomNativeRuntime value) => Volatile.Write(ref runtime, value);
        internal static void Detach(ZoomNativeRuntime value)
        { if (ReferenceEquals(Volatile.Read(ref runtime), value)) Volatile.Write(ref runtime, null); }
        public static void DolocApiSetEnvCameraPostfix() => Volatile.Read(ref runtime)?.OnEnvironmentReset();
        public static void ContentChangedPostfix() => Volatile.Read(ref runtime)?.OnNativeContentChanged();
        public static void BackgroundLoadPrefix(object __instance) => Volatile.Read(ref runtime)?.BeforeBackgroundReload(__instance);
        public static Exception? BackgroundLoadFinalizer(Exception? __exception)
        { Volatile.Read(ref runtime)?.OnNativeContentChanged(); return __exception; }
        public static void CameraControllerRefreshResolutionPrefix(object __instance, out ZoomNativeRefreshState __state)
        { __state = Volatile.Read(ref runtime)?.BeforeNativeRefreshResolution(__instance) ?? default; }
        public static Exception? CameraControllerRefreshResolutionFinalizer(Exception? __exception, ZoomNativeRefreshState __state) =>
            __state.Runtime?.AfterNativeRefreshResolution(__state, __exception) ?? __exception;
    }
}
