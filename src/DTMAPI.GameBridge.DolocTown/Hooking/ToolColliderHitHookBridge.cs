using System.Reflection;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class ToolColliderHitHookBridge
    {
        internal bool PrefixPatched { get; private set; }

        internal bool PostfixPatched { get; private set; }

        internal bool RoutePatched => PrefixPatched && PostfixPatched;

        public void InstallHooks(HarmonyReflectionPatcher patcher)
        {
            if (!PrefixPatched)
            {
                PrefixPatched = patcher.TryPatchPrefix(
                    "DolocTown.ToolCollider, Assembly-CSharp",
                    "HandleTools",
                    typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.ToolColliderHandleToolsPrefix), BindingFlags.Public | BindingFlags.Static),
                    1);
            }

            if (!PostfixPatched)
            {
                PostfixPatched = patcher.TryPatchPostfix(
                    "DolocTown.ToolCollider, Assembly-CSharp",
                    "HandleTools",
                    typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.ToolColliderHandleToolsPostfix), BindingFlags.Public | BindingFlags.Static),
                    1);
            }
        }
    }
}
