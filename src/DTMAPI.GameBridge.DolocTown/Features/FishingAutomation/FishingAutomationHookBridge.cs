using System.Reflection;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class FishingAutomationHookBridge
    {
        private readonly DtmApiRuntime runtime;
        private readonly FishingAutomationService service;

        public FishingAutomationHookBridge(DtmApiRuntime runtime, FishingAutomationService service)
        {
            this.runtime = runtime;
            this.service = service;
        }

        internal bool ReadyEnterPatched { get; private set; }

        internal bool ReadyPlayPatched { get; private set; }

        internal bool CastEnterPatched { get; private set; }

        internal bool WaitEnterPatched { get; private set; }

        internal bool WaitPlayPatched { get; private set; }

        internal bool MiniGameStartPatched { get; private set; }

        internal bool MiniGameUpdatePrefixPatched { get; private set; }

        internal bool MiniGameUpdatePatched { get; private set; }

        internal bool MiniGameStopPatched { get; private set; }

        internal bool InputUseToolPatched { get; private set; }

        internal bool InputUseToolInProgressPatched { get; private set; }

        internal bool InputUseItemPatched { get; private set; }

        internal bool InputUseItemInProgressPatched { get; private set; }

        internal bool InputFishingPatched { get; private set; }

        internal bool InputFishingInProgressPatched { get; private set; }

        internal bool FishRodCastHookPatched { get; private set; }

        internal bool FishRodPullPatched { get; private set; }

        internal bool FishRodPullCancelPatched { get; private set; }

        internal bool PullEnterPatched { get; private set; }

        internal bool PullExitPatched { get; private set; }

        internal bool HooksReady => ReadyEnterPatched &&
            ReadyPlayPatched &&
            CastEnterPatched &&
            WaitEnterPatched &&
            WaitPlayPatched &&
            MiniGameStartPatched &&
            MiniGameUpdatePrefixPatched &&
            MiniGameUpdatePatched &&
            MiniGameStopPatched &&
            InputUseToolPatched &&
            InputUseToolInProgressPatched &&
            InputUseItemPatched &&
            InputUseItemInProgressPatched &&
            InputFishingPatched &&
            InputFishingInProgressPatched &&
            FishRodCastHookPatched &&
            FishRodPullPatched &&
            FishRodPullCancelPatched &&
            PullEnterPatched &&
            PullExitPatched;

        public void PublishHookStatuses()
        {
            PublishStatuses();
        }

        public void InstallHooks(HarmonyReflectionPatcher patcher)
        {
            if (!ReadyEnterPatched)
            {
                ReadyEnterPatched = patcher.TryPatchPostfix("DolocTown.AgentStateFishingReady, Assembly-CSharp", "OnEnter", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FishingReadyEnterPostfix), BindingFlags.Public | BindingFlags.Static), 0);
            }

            if (!ReadyPlayPatched)
            {
                ReadyPlayPatched = patcher.TryPatchPostfix("DolocTown.AgentStateFishingReady, Assembly-CSharp", "OnPlay", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FishingReadyPlayPostfix), BindingFlags.Public | BindingFlags.Static), 0);
            }

            if (!CastEnterPatched)
            {
                CastEnterPatched = patcher.TryPatchPostfix("DolocTown.AgentStateFishingCast, Assembly-CSharp", "OnEnter", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FishingCastEnterPostfix), BindingFlags.Public | BindingFlags.Static), 0);
            }

            if (!WaitEnterPatched)
            {
                WaitEnterPatched = patcher.TryPatchPostfix("DolocTown.AgentStateFishingWait, Assembly-CSharp", "OnEnter", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FishingWaitEnterPostfix), BindingFlags.Public | BindingFlags.Static), 0);
            }

            if (!WaitPlayPatched)
            {
                WaitPlayPatched = patcher.TryPatchPostfix("DolocTown.AgentStateFishingWait, Assembly-CSharp", "OnPlay", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FishingWaitPlayPostfix), BindingFlags.Public | BindingFlags.Static), 0);
            }

            if (!MiniGameStartPatched)
            {
                MiniGameStartPatched = patcher.TryPatchPostfix("DolocTown.FishingGameScrollBar, Assembly-CSharp", "StartGame", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FishingMiniGameStartPostfix), BindingFlags.Public | BindingFlags.Static), 2);
            }

            if (!MiniGameUpdatePatched)
            {
                MiniGameUpdatePatched = patcher.TryPatchPostfix("DolocTown.FishingGameScrollBar, Assembly-CSharp", "UpdateGame", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FishingMiniGameUpdatePostfix), BindingFlags.Public | BindingFlags.Static), 1);
            }

            if (!MiniGameUpdatePrefixPatched)
            {
                MiniGameUpdatePrefixPatched = patcher.TryPatchPrefix("DolocTown.FishingGameScrollBar, Assembly-CSharp", "UpdateGame", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FishingMiniGameUpdatePrefix), BindingFlags.Public | BindingFlags.Static), 1);
            }

            if (!MiniGameStopPatched)
            {
                MiniGameStopPatched = patcher.TryPatchPostfix("DolocTown.FishingGameScrollBar, Assembly-CSharp", "StopGame", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FishingMiniGameStopPostfix), BindingFlags.Public | BindingFlags.Static), 0);
            }

            if (!InputUseToolPatched)
            {
                InputUseToolPatched = patcher.TryPatchPrefix("DolocTown.DolocUserInput, Assembly-CSharp", "get_NormalUseTool", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FishingInputNormalUseToolPrefix), BindingFlags.Public | BindingFlags.Static), 0);
            }

            if (!InputUseToolInProgressPatched)
            {
                InputUseToolInProgressPatched = patcher.TryPatchPrefix("DolocTown.DolocUserInput, Assembly-CSharp", "get_NormalUseToolInProgress", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FishingInputNormalUseToolInProgressPrefix), BindingFlags.Public | BindingFlags.Static), 0);
            }

            if (!InputUseItemPatched)
            {
                InputUseItemPatched = patcher.TryPatchPrefix("DolocTown.DolocUserInput, Assembly-CSharp", "get_NormalUseItem", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FishingInputNormalUseItemPrefix), BindingFlags.Public | BindingFlags.Static), 0);
            }

            if (!InputUseItemInProgressPatched)
            {
                InputUseItemInProgressPatched = patcher.TryPatchPrefix("DolocTown.DolocUserInput, Assembly-CSharp", "get_NormalUseItemInProgress", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FishingInputNormalUseItemInProgressPrefix), BindingFlags.Public | BindingFlags.Static), 0);
            }

            if (!InputFishingPatched)
            {
                InputFishingPatched = patcher.TryPatchPrefix("DolocTown.DolocUserInput, Assembly-CSharp", "get_NormalFishing", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FishingInputNormalFishingPrefix), BindingFlags.Public | BindingFlags.Static), 0);
            }

            if (!InputFishingInProgressPatched)
            {
                InputFishingInProgressPatched = patcher.TryPatchPrefix("DolocTown.DolocUserInput, Assembly-CSharp", "get_NormalFishingInProgress", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FishingInputNormalFishingInProgressPrefix), BindingFlags.Public | BindingFlags.Static), 0);
            }

            if (!FishRodCastHookPatched)
            {
                FishRodCastHookPatched = patcher.TryPatchPostfix("DolocTown.FishRodRenderer, Assembly-CSharp", "CastHook", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FishRodRendererCastHookPostfix), BindingFlags.Public | BindingFlags.Static), 0);
            }

            if (!FishRodPullPatched)
            {
                FishRodPullPatched = patcher.TryPatchPostfix("DolocTown.FishRodRenderer, Assembly-CSharp", "Pull", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FishRodRendererPullPostfix), BindingFlags.Public | BindingFlags.Static), 1);
            }

            if (!FishRodPullCancelPatched)
            {
                FishRodPullCancelPatched = patcher.TryPatchPostfix("DolocTown.FishRodRenderer, Assembly-CSharp", "PullCancel", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FishRodRendererPullCancelPostfix), BindingFlags.Public | BindingFlags.Static), 0);
            }

            if (!PullEnterPatched)
            {
                PullEnterPatched = patcher.TryPatchPostfix("DolocTown.AgentStateFishingPull, Assembly-CSharp", "OnEnter", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FishingPullEnterPostfix), BindingFlags.Public | BindingFlags.Static), 0);
            }

            if (!PullExitPatched)
            {
                PullExitPatched = patcher.TryPatchPostfix("DolocTown.AgentStateFishingPull, Assembly-CSharp", "OnExit", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FishingPullExitPostfix), BindingFlags.Public | BindingFlags.Static), 0);
            }

            service.SetFishingHooksInstalled(HooksReady);
            PublishStatuses();
        }

        private void PublishStatuses()
        {
            runtime.SetHookStatus("Fishing.Automation", HooksReady ? "experimental" : "pending", "Harmony fishing native phases/input", HooksReady ? "Patched native fishing phase hooks plus BodyController.UseFishRod auto-cast, AgentStateFishingReady charge timing, AgentStateFishingWait bite/reel routing, FishingGameScrollBar native input automation, native skip-result routing, FishRodRenderer.CastHook hook physics, and cast/pull animation-duration paths." : "Waiting for fishing phase, input, minigame, or FishRodRenderer targets to become patchable.");
        }
    }
}
