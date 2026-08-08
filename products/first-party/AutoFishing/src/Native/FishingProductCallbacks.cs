using System;

namespace Yuuka.DTMAPI.AutoFishing
{
    internal static class FishingProductCallbacks
    {
        private static AutoFishingNativeRuntime? runtime;

        internal static void Attach(AutoFishingNativeRuntime value)
        {
            runtime = value ?? throw new ArgumentNullException(nameof(value));
        }

        internal static void Detach(AutoFishingNativeRuntime value)
        {
            if (ReferenceEquals(runtime, value))
                runtime = null;
        }

        internal static void AgentStateBaseExitPostfix()
        {
            FishingPrimitiveHookRuntime? active = runtime?.CallbackRuntime;
            if (active == null)
                return;
            Safe("AgentStateBase.OnExit.Restore", () => active.RestoreExperimentalAnimatorSpeeds("AgentStateBase.OnExit"));
        }

        internal static void FishingReadyEnterPostfix(object __instance)
        {
            FishingPrimitiveHookRuntime? active = runtime?.CallbackRuntime;
            if (active == null)
                return;
            Safe("Ready.OnEnter", () => active.NotifyFishingPhase("Ready", __instance));
        }

        internal static void FishingReadyPlayPostfix(object __instance)
        {
            FishingPrimitiveHookRuntime? active = runtime?.CallbackRuntime;
            if (active == null)
                return;
            Safe("Ready.OnPlay", () => active.ApplyFishingReadyAutomation(__instance));
        }

        internal static void FishingCastEnterPostfix(object __instance)
        {
            FishingPrimitiveHookRuntime? active = runtime?.CallbackRuntime;
            if (active == null)
                return;
            Safe("Cast.OnEnter", () => active.NotifyFishingPhase("Cast", __instance));
        }

        internal static void FishingWaitEnterPostfix(object __instance)
        {
            FishingPrimitiveHookRuntime? active = runtime?.CallbackRuntime;
            if (active == null)
                return;
            Safe("Wait.OnEnter.Notify", () => active.NotifyFishingPhase("Wait", __instance));
            Safe("Wait.OnEnter.Apply", () => active.ApplyFishingWaitAutomation(__instance, "AgentStateFishingWait.OnEnter Postfix"));
        }

        internal static void FishingWaitPlayPostfix(object __instance)
        {
            FishingPrimitiveHookRuntime? active = runtime?.CallbackRuntime;
            if (active == null)
                return;
            Safe("Wait.OnPlay", () => active.ApplyFishingWaitAutomation(__instance, "AgentStateFishingWait.OnPlay Postfix"));
        }

        internal static void FishingWaitNextStatePostfix(object __instance, object? __result)
        {
            FishingPrimitiveHookRuntime? active = runtime?.CallbackRuntime;
            if (active == null)
                return;
            Safe("Wait.NextState", () => active.ConfirmFishingWaitNativeReelAccepted(__instance, __result));
        }

        internal static void FishingMiniGameStartPostfix(object __instance)
        {
            FishingPrimitiveHookRuntime? active = runtime?.CallbackRuntime;
            if (active == null)
                return;
            Safe("MiniGame.Start", () => active.NotifyFishingMiniGameStart(__instance));
        }

        internal static void FishingMiniGameUpdatePrefix(object __instance)
        {
            FishingPrimitiveHookRuntime? active = runtime?.CallbackRuntime;
            if (active == null)
                return;
            Safe("MiniGame.Update.Prefix", () => active.PrepareFishingMiniGameAutomationInput(__instance));
        }

        internal static void FishingMiniGameUpdatePostfix(object __instance)
        {
            FishingPrimitiveHookRuntime? active = runtime?.CallbackRuntime;
            if (active == null)
                return;
            Safe("MiniGame.Update.Postfix", () => active.ApplyFishingMiniGameAutomationTick(__instance));
        }

        internal static void FishingMiniGameStopPostfix(object __instance)
        {
            FishingPrimitiveHookRuntime? active = runtime?.CallbackRuntime;
            if (active == null)
                return;
            Safe("MiniGame.Stop", () => active.NotifyFishingMiniGameStop(__instance));
        }

        internal static bool FishingInputNormalUseToolPrefix(ref bool __result) =>
            TryMiniGameInputOverride("NormalUseTool", ref __result);

        internal static bool FishingInputNormalUseToolInProgressPrefix(ref bool __result)
        {
            FishingPrimitiveHookRuntime? active = runtime?.CallbackRuntime;
            if (active == null)
                return true;
            try
            {
                if (active.TryOverrideFishingReadyChargeInput("NormalUseToolInProgress", out bool value))
                {
                    __result = value;
                    return false;
                }
            }
            catch (Exception ex)
            {
                runtime?.RecordCallbackFailure("Ready.Input.NormalUseToolInProgress", ex);
            }
            return TryMiniGameInputOverride("NormalUseToolInProgress", ref __result);
        }

        internal static bool FishingInputNormalUseItemPrefix(ref bool __result) =>
            TryMiniGameInputOverride("NormalUseItem", ref __result);

        internal static bool FishingInputNormalUseItemInProgressPrefix(ref bool __result) =>
            TryMiniGameInputOverride("NormalUseItemInProgress", ref __result);

        internal static bool FishingInputNormalFishingPrefix(ref bool __result) =>
            TryMiniGameInputOverride("NormalFishing", ref __result);

        internal static bool FishingInputNormalFishingInProgressPrefix(ref bool __result) =>
            TryMiniGameInputOverride("NormalFishingInProgress", ref __result);

        internal static void FishRodRendererCastHookPostfix(object __instance)
        {
            FishingPrimitiveHookRuntime? active = runtime?.CallbackRuntime;
            if (active == null)
                return;
            Safe("FishRodRenderer.CastHook", () => active.AdjustFishingCastHookPhysics(__instance));
        }

        internal static void FishRodRendererPullPostfix(ref float __result)
        {
            FishingPrimitiveHookRuntime? active = runtime?.CallbackRuntime;
            if (active == null)
                return;
            try
            {
                active.AdjustFishingPullDurationResult(ref __result, "FishRodRenderer.Pull");
            }
            catch (Exception ex)
            {
                runtime?.RecordCallbackFailure("FishRodRenderer.Pull", ex);
            }
        }

        internal static void FishRodRendererPullCancelPostfix(ref float __result)
        {
            FishingPrimitiveHookRuntime? active = runtime?.CallbackRuntime;
            if (active == null)
                return;
            try
            {
                active.AdjustFishingPullDurationResult(ref __result, "FishRodRenderer.PullCancel");
            }
            catch (Exception ex)
            {
                runtime?.RecordCallbackFailure("FishRodRenderer.PullCancel", ex);
            }
        }

        internal static void FishingPullEnterPostfix(object __instance)
        {
            FishingPrimitiveHookRuntime? active = runtime?.CallbackRuntime;
            if (active == null)
                return;
            Safe("Pull.OnEnter", () => active.NotifyFishingPhase("Pull", __instance));
        }

        internal static void FishingPullExitPostfix()
        {
            FishingPrimitiveHookRuntime? active = runtime?.CallbackRuntime;
            if (active == null)
                return;
            Safe("Pull.OnExit.Notify", () => active.NotifyFishingPhase("Cooldown", null));
            try
            {
                active.RestoreExperimentalAnimatorSpeeds("AgentStateFishingPull.OnExit");
            }
            catch (Exception ex)
            {
                runtime?.RecordCallbackFailure("Pull.OnExit.Restore", ex);
            }
            Safe("Pull.OnExit.Lifecycle", () => active.NotifyFishingNativeExit("AgentStateFishingPull.OnExit"));
        }

        private static bool TryMiniGameInputOverride(string inputName, ref bool result)
        {
            FishingPrimitiveHookRuntime? active = runtime?.CallbackRuntime;
            if (active == null)
                return true;
            try
            {
                if (active.TryOverrideFishingMiniGameInput(inputName, out bool value))
                {
                    result = value;
                    return false;
                }
            }
            catch (Exception ex)
            {
                runtime?.RecordCallbackFailure("Input." + inputName, ex);
            }
            return true;
        }

        private static void Safe(string operation, Action callback)
        {
            try
            {
                callback();
            }
            catch (Exception ex)
            {
                runtime?.RecordCallbackFailure(operation, ex);
            }
        }
    }
}
