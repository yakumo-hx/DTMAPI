using System.Reflection;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class ActionSpeedHookBridge
    {
        private readonly DtmApiRuntime runtime;
        private readonly ActionSpeedService service;
        private readonly AgentStateLifecycleHookBridge lifecycleHooks;

        public ActionSpeedHookBridge(DtmApiRuntime runtime, ActionSpeedService service, AgentStateLifecycleHookBridge lifecycleHooks)
        {
            this.runtime = runtime;
            this.service = service;
            this.lifecycleHooks = lifecycleHooks;
        }

        internal bool ToolEnterPatched { get; private set; }

        internal bool ToolExitPatched => lifecycleHooks.ToolExitPatched;

        internal bool InteractEnterPatched { get; private set; }

        internal bool InteractExitPatched => lifecycleHooks.InteractExitPatched;

        internal bool EatEnterPatched { get; private set; }

        internal bool UseItemContinuesPatched { get; private set; }

        internal bool BaseExitPatched => lifecycleHooks.BaseExitPatched;

        internal bool ToolHooksReady => ToolEnterPatched && ToolExitPatched;

        internal bool InteractionHooksReady => InteractEnterPatched && InteractExitPatched && EatEnterPatched && UseItemContinuesPatched && BaseExitPatched;

        public void PublishHookStatuses()
        {
            PublishStatuses();
        }

        public void InstallHooks(HarmonyReflectionPatcher patcher)
        {
            if (!ToolEnterPatched)
            {
                ToolEnterPatched = patcher.TryPatchPostfix("DolocTown.AgentStateTool, Assembly-CSharp", "OnEnter", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AgentStateToolEnterPostfix), BindingFlags.Public | BindingFlags.Static), 0);
            }

            if (!InteractEnterPatched)
            {
                InteractEnterPatched = patcher.TryPatchPostfix("DolocTown.AgentStateInteract, Assembly-CSharp", "OnEnter", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AgentStateInteractEnterPostfix), BindingFlags.Public | BindingFlags.Static), 0);
            }

            if (!EatEnterPatched)
            {
                EatEnterPatched = patcher.TryPatchPostfix("DolocTown.AgentStateEat, Assembly-CSharp", "OnEnter", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AgentStateEatEnterPostfix), BindingFlags.Public | BindingFlags.Static), 0);
            }

            if (!UseItemContinuesPatched)
            {
                UseItemContinuesPatched = patcher.TryPatchPrefix("DolocTown.AgentControllerState, Assembly-CSharp", "UseItemContinues", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AgentControllerStateUseItemContinuesPrefix), BindingFlags.Public | BindingFlags.Static), 1);
            }

            service.SetActionSpeedToolHooksInstalled(ToolHooksReady);
            service.SetActionSpeedInteractionHooksInstalled(InteractionHooksReady);
            PublishStatuses();
        }

        private void PublishStatuses()
        {
            runtime.SetHookStatus("ActionSpeed.ToolAnimation", ToolHooksReady ? "verified" : "pending", "Harmony Postfix: AgentStateTool.OnEnter/OnExit", ToolHooksReady ? "Patched tool animation speed and restore points; verified by ACTIONSPEED-001. Selected interaction slices are tracked separately in ACTIONSPEED-002." : "Waiting for AgentStateTool.OnEnter/OnExit to become patchable.");
            runtime.SetHookStatus("ActionSpeed.InteractionAnimation", InteractionHooksReady ? "experimental" : "pending", "Harmony Postfix/Prefix: AgentStateInteract/AgentStateEat/AgentControllerState.UseItemContinues", InteractionHooksReady ? "Patched shared interaction/eat animation speed points plus right-click continuous timer scaling. ACTIONSPEED-002 verifies fuel/feed add, eat/drink animation, bottled-water right-click continuous drink, IWaterContainer and in-water bottle fill, no-key auto-fill, planting, plant-basin crop harvest, resin collection, and wild vegetation harvest." : "Waiting for AgentStateInteract/AgentStateEat/UseItemContinues hooks to become patchable.");
        }
    }
}
