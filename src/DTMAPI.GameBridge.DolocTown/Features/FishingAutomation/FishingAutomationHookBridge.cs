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

        internal bool CastEnterPatched { get; private set; }

        internal bool WaitEnterPatched { get; private set; }

        internal bool WaitPlayPatched { get; private set; }

        internal bool MiniGameStartPatched { get; private set; }

        internal bool MiniGameUpdatePatched { get; private set; }

        internal bool MiniGameStopPatched { get; private set; }

        internal bool PullEnterPatched { get; private set; }

        internal bool PullExitPatched { get; private set; }

        internal bool HooksReady => ReadyEnterPatched && CastEnterPatched && WaitEnterPatched && WaitPlayPatched && MiniGameStartPatched && MiniGameUpdatePatched && MiniGameStopPatched && PullEnterPatched && PullExitPatched;

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

            if (!MiniGameStopPatched)
            {
                MiniGameStopPatched = patcher.TryPatchPostfix("DolocTown.FishingGameScrollBar, Assembly-CSharp", "StopGame", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FishingMiniGameStopPostfix), BindingFlags.Public | BindingFlags.Static), 0);
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
            runtime.SetHookStatus("Fishing.Automation", HooksReady ? "experimental" : "pending", "Harmony Postfix: fishing state/input phases", HooksReady ? "Patched fishing phase observation hooks plus native BodyController.UseFishRod auto-cast, wait-phase InstantBite, and delayed FishingGameScrollBar.UpdateGame auto-complete; F6 auto-cast and wait phase verified by AUTOFISH-001." : "Waiting for fishing phase targets to become patchable.");
        }
    }
}
