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

        internal bool InteractContinuesPatched { get; private set; }

        internal bool AnimalRendererInteractPatched { get; private set; }

        internal bool BaseExitPatched => lifecycleHooks.BaseExitPatched;

        internal bool ToolHooksReady => ToolEnterPatched && ToolExitPatched;

        internal bool InteractionHooksReady => InteractEnterPatched && InteractExitPatched && EatEnterPatched && UseItemContinuesPatched && InteractContinuesPatched && AnimalRendererInteractPatched && BaseExitPatched;

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
                var useItemContinuesSignature = HarmonyTargetSignature.Exact("DolocTown.AgentControllerState", "System.Void", "System.Single");
                UseItemContinuesPatched = patcher.TryPatchPrefix("DolocTown.AgentControllerState, Assembly-CSharp", "UseItemContinues", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AgentControllerStateUseItemContinuesPrefix), BindingFlags.Public | BindingFlags.Static), useItemContinuesSignature);
            }

            if (!InteractContinuesPatched)
            {
                var interactContinuesSignature = HarmonyTargetSignature.Exact("DolocTown.AgentControllerState", "System.Boolean", "System.Single");
                InteractContinuesPatched = patcher.TryPatchPrefix("DolocTown.AgentControllerState, Assembly-CSharp", "InteractContinues", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AgentControllerStateInteractContinuesPrefix), BindingFlags.Public | BindingFlags.Static), interactContinuesSignature);
            }

            if (!AnimalRendererInteractPatched)
            {
                AnimalRendererInteractPatched = patcher.TryPatchPrefix("DolocTown.AnimalRenderer, Assembly-CSharp", "OnInteract", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AnimalRendererOnInteractPrefix), BindingFlags.Public | BindingFlags.Static), 0);
            }

            service.SetActionSpeedToolHooksInstalled(ToolHooksReady);
            service.SetActionSpeedInteractionHooksInstalled(InteractionHooksReady);
            PublishStatuses();
        }

        private void PublishStatuses()
        {
            runtime.SetHookStatus("ActionSpeed.ToolAnimation", ToolHooksReady ? "verified" : "pending", "Harmony Postfix: AgentStateTool.OnEnter/OnExit", ToolHooksReady ? "Patched tool animation speed and restore points; verified by ACTIONSPEED-001. Selected interaction slices are tracked separately in ACTIONSPEED-002." : "Waiting for AgentStateTool.OnEnter/OnExit to become patchable.");
            runtime.SetHookStatus("ActionSpeed.InteractionAnimation", InteractionHooksReady ? "experimental" : "pending", "Harmony Postfix/Prefix: AgentStateInteract/AgentStateEat/AgentControllerState.UseItemContinues/InteractContinues/AnimalRenderer.OnInteract", InteractionHooksReady ? "Patched shared interaction/eat animation speed points, native use-item/interact continuous timer scaling, and AnimalRenderer.OnInteract owner marking. Historical ACTIONSPEED-002 slices remain relevant; 2026-06-16 native-stage paths need fresh third-save smoke/manual QA." : "Waiting for AgentStateInteract/AgentStateEat/UseItemContinues/InteractContinues/AnimalRenderer.OnInteract hooks to become patchable.");
        }
    }
}
