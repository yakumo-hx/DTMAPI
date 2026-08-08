using System.Reflection;

namespace DTMAPI.GameBridge.DolocTown
{
    /// <summary>Frozen ToolCollider hook for legacy IActionCompletionApi consumers.</summary>
    internal sealed class ToolColliderHitHookBridge
    {
        internal bool PostfixPatched { get; private set; }

        public void InstallHooks(HarmonyReflectionPatcher patcher)
        {
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
