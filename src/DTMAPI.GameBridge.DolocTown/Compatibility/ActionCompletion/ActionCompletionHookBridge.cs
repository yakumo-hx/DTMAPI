using System;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class ActionCompletionHookBridge
    {
        private readonly DtmApiRuntime runtime;
        private readonly ActionCompletionService service;
        private readonly Func<bool> isToolColliderPostfixPatched;
        private readonly Func<bool> isInteractExitPatched;

        public ActionCompletionHookBridge(DtmApiRuntime runtime, ActionCompletionService service, Func<bool> isToolColliderPostfixPatched, Func<bool> isInteractExitPatched)
        {
            this.runtime = runtime;
            this.service = service;
            this.isToolColliderPostfixPatched = isToolColliderPostfixPatched;
            this.isInteractExitPatched = isInteractExitPatched;
        }

        internal bool ToolColliderPatched => isToolColliderPostfixPatched();

        internal bool InteractExitPatched => isInteractExitPatched();

        public void PublishHookStatuses()
        {
            PublishStatuses();
        }

        public void InstallHooks(HarmonyReflectionPatcher patcher)
        {
            service.SetActionHooksInstalled(ToolColliderPatched);
            PublishStatuses();
        }

        private void PublishStatuses()
        {
            runtime.SetHookStatus(
                "Actions.OneActionComplete",
                ToolColliderPatched ? "verified" : "pending",
                "Harmony Postfix: ToolCollider.HandleTools",
                ToolColliderPatched
                    ? "Patched resource/tool-hit path with native ResourceFellData validation; verified by ONEACTION-001/002. Fuel/feeder completion is tracked separately and verified by ONEACTION-002. Vegetation/dandelion uses native VegetationRenderer.OnFell and is verified as a non-DungeonResource exception in ONEACTION-003."
                    : "Waiting for ToolCollider.HandleTools to become patchable.");
            runtime.SetHookStatus(
                "Actions.OneActionFuelFeed",
                InteractExitPatched ? "verified" : "pending",
                "Harmony Postfix: AgentStateInteract.OnExit",
                InteractExitPatched
                    ? "Patched post-interact native CostSelf/AddFuel/AddFeeds path for fuel/feed targets; verified by ONEACTION-002 fuel/feed smoke."
                    : "Waiting for AgentStateInteract.OnExit to become patchable.");
        }
    }
}
