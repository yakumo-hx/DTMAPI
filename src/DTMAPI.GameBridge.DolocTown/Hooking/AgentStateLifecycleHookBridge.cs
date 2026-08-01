using System.Reflection;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class AgentStateLifecycleHookBridge
    {
        internal bool ToolExitPatched { get; private set; }

        internal bool InteractExitPatched { get; private set; }

        internal bool BaseExitPatched { get; private set; }

        public void InstallHooks(HarmonyReflectionPatcher patcher)
        {
            InstallToolExitHook(patcher);
            InstallInteractExitHook(patcher);
            InstallBaseExitHook(patcher);
        }

        internal void InstallToolExitHook(HarmonyReflectionPatcher patcher)
        {
            if (!ToolExitPatched)
            {
                ToolExitPatched = patcher.TryPatchPostfix("DolocTown.AgentStateTool, Assembly-CSharp", "OnExit", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AgentStateToolExitPostfix), BindingFlags.Public | BindingFlags.Static), 0);
            }
        }

        internal void InstallInteractExitHook(HarmonyReflectionPatcher patcher)
        {
            if (!InteractExitPatched)
            {
                InteractExitPatched = patcher.TryPatchPostfix("DolocTown.AgentStateInteract, Assembly-CSharp", "OnExit", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AgentStateInteractExitPostfix), BindingFlags.Public | BindingFlags.Static), 0);
            }
        }

        internal void InstallBaseExitHook(HarmonyReflectionPatcher patcher)
        {
            if (!BaseExitPatched)
            {
                BaseExitPatched = patcher.TryPatchPostfix("AgentStateBase, Assembly-CSharp", "OnExit", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AgentStateBaseExitPostfix), BindingFlags.Public | BindingFlags.Static), 0);
            }
        }
    }
}
