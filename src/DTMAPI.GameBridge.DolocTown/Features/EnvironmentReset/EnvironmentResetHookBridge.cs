using System.Reflection;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    /// <summary>Shared physical owner for the native environment-reset boundary.</summary>
    internal sealed class EnvironmentResetHookBridge
    {
        private readonly DtmApiRuntime runtime;

        internal EnvironmentResetHookBridge(DtmApiRuntime runtime)
        {
            this.runtime = runtime;
        }

        internal bool SetEnvCameraPatched { get; private set; }

        internal void InstallHooks(HarmonyReflectionPatcher patcher)
        {
            if (!SetEnvCameraPatched)
            {
                SetEnvCameraPatched = patcher.TryPatchPostfix(
                    "DolocAPI, Assembly-CSharp",
                    "SetEnvCamera",
                    typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.DolocApiSetEnvCameraPostfix), BindingFlags.Public | BindingFlags.Static),
                    5);
            }

            runtime.SetHookStatus(
                "SharedNative.EnvironmentReset",
                SetEnvCameraPatched ? "experimental" : "pending",
                "Harmony Postfix: DolocAPI.SetEnvCamera",
                SetEnvCameraPatched
                    ? "The shared environment-reset Hook serves only ItemDisplayName cache invalidation; Camera compatibility and ProductNative Zoom have independent exact owners."
                    : "Waiting for DolocAPI.SetEnvCamera to become patchable; ItemDisplayName remains uncached until this Hook is ready.");
        }
    }
}
