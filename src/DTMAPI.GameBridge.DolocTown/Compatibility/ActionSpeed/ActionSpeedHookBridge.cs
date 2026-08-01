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

        internal bool InteractionStageHooksReady => InteractEnterPatched && EatEnterPatched && UseItemContinuesPatched && InteractContinuesPatched && AnimalRendererInteractPatched;

        internal bool DemandedToolHooksReady => !service.RequiresToolHooks || ToolHooksReady;

        internal bool DemandedInteractionHooksReady => !service.RequiresInteractionHooks ||
            (InteractionStageHooksReady &&
             (!service.RequiresInteractExit || InteractExitPatched) &&
             (!service.RequiresBaseExit || BaseExitPatched));

        public void PublishHookStatuses()
        {
            PublishStatuses();
        }

        public void InstallHooks(HarmonyReflectionPatcher patcher)
        {
            if (service.RequiresToolHooks && !ToolEnterPatched)
            {
                ToolEnterPatched = patcher.TryPatchPostfix("DolocTown.AgentStateTool, Assembly-CSharp", "OnEnter", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AgentStateToolEnterPostfix), BindingFlags.Public | BindingFlags.Static), 0);
            }

            if (service.RequiresInteractionHooks && !InteractEnterPatched)
            {
                InteractEnterPatched = patcher.TryPatchPostfix("DolocTown.AgentStateInteract, Assembly-CSharp", "OnEnter", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AgentStateInteractEnterPostfix), BindingFlags.Public | BindingFlags.Static), 0);
            }

            if (service.RequiresInteractionHooks && !EatEnterPatched)
            {
                EatEnterPatched = patcher.TryPatchPostfix("DolocTown.AgentStateEat, Assembly-CSharp", "OnEnter", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AgentStateEatEnterPostfix), BindingFlags.Public | BindingFlags.Static), 0);
            }

            if (service.RequiresInteractionHooks && !UseItemContinuesPatched)
            {
                var useItemContinuesSignature = HarmonyTargetSignature.Exact("DolocTown.AgentControllerState", "System.Void", "System.Single");
                UseItemContinuesPatched = patcher.TryPatchPrefix("DolocTown.AgentControllerState, Assembly-CSharp", "UseItemContinues", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AgentControllerStateUseItemContinuesPrefix), BindingFlags.Public | BindingFlags.Static), useItemContinuesSignature);
            }

            if (service.RequiresInteractionHooks && !InteractContinuesPatched)
            {
                var interactContinuesSignature = HarmonyTargetSignature.Exact("DolocTown.AgentControllerState", "System.Boolean", "System.Single");
                InteractContinuesPatched = patcher.TryPatchPrefix("DolocTown.AgentControllerState, Assembly-CSharp", "InteractContinues", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AgentControllerStateInteractContinuesPrefix), BindingFlags.Public | BindingFlags.Static), interactContinuesSignature);
            }

            if (service.RequiresInteractionHooks && !AnimalRendererInteractPatched)
            {
                AnimalRendererInteractPatched = patcher.TryPatchPrefix("DolocTown.AnimalRenderer, Assembly-CSharp", "OnInteract", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AnimalRendererOnInteractPrefix), BindingFlags.Public | BindingFlags.Static), 0);
            }

            service.SetActionSpeedToolHooksInstalled(DemandedToolHooksReady);
            service.SetActionSpeedInteractionHooksInstalled(DemandedInteractionHooksReady);
            PublishStatuses();
        }

        private void PublishStatuses()
        {
            runtime.SetHookStatus("ActionSpeed.ToolAnimation", DemandedToolHooksReady ? "verified" : "pending", "Harmony Postfix: AgentStateTool.OnEnter/OnExit", DemandedToolHooksReady ? "The currently demanded tool-stage closure is ready; verified tool policy remains tracked by ACTIONSPEED-001." : "Waiting for the demanded AgentStateTool.OnEnter/OnExit closure to become patchable.");
            runtime.SetHookStatus("ActionSpeed.InteractionAnimation", DemandedInteractionHooksReady ? "experimental" : "pending", "Harmony Postfix/Prefix: AgentStateInteract/AgentStateEat/AgentControllerState.UseItemContinues/InteractContinues/AnimalRenderer.OnInteract + exact exit dependencies", DemandedInteractionHooksReady ? "The currently demanded interaction-stage closure is ready. InteractExit and BaseExit are included only for policies which own those restore paths." : "Waiting for the demanded interaction-stage and exact restore closure to become patchable.");
        }
    }
}
