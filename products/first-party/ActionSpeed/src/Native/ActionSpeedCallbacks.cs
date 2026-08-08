using System;

namespace Yuuka.DTMAPI.ActionSpeed
{
    internal static class ActionSpeedCallbacks
    {
        private static ActionSpeedNativeRuntime? runtime;

        internal static void Attach(ActionSpeedNativeRuntime value)
        {
            if (runtime != null && !ReferenceEquals(runtime, value))
                throw new InvalidOperationException("ActionSpeed callback runtime is already attached.");
            runtime = value ?? throw new ArgumentNullException(nameof(value));
        }

        internal static void Detach(ActionSpeedNativeRuntime value)
        {
            if (ReferenceEquals(runtime, value))
                runtime = null;
        }

        public static void AgentStateToolEnterPostfix(object __instance) => Invoke("AgentStateTool.OnEnter", current => current.ApplyToolEnter(__instance));
        public static void AgentStateToolExitPostfix() => Invoke("AgentStateTool.OnExit", current => current.Restore("AgentStateTool.OnExit"));
        public static void AgentStateWaterEnterPostfix(object __instance) => Invoke("AgentStateWater.OnEnter", current => current.ApplyToolEnter(__instance));
        public static void AgentStateWaterExitPostfix() => Invoke("AgentStateWater.OnExit", current => current.Restore("AgentStateWater.OnExit"));
        public static void AgentStateInteractEnterPostfix(object __instance) => Invoke("AgentStateInteract.OnEnter", current => current.ApplyInteractEnter(__instance));
        public static void AgentStateInteractExitPostfix() => Invoke("AgentStateInteract.OnExit", current => current.Restore("AgentStateInteract.OnExit"));
        public static void AgentStateEatEnterPostfix(object __instance) => Invoke("AgentStateEat.OnEnter", current => current.ApplyEatEnter(__instance));
        public static void AnimalRendererOnInteractPrefix(object __instance) => Invoke("AnimalRenderer.OnInteract", current => current.MarkAnimalInteract(__instance));
        public static void AgentStateBaseExitPostfix() => Invoke("AgentStateBase.OnExit", current => current.Restore("AgentStateBase.OnExit"));

        public static void AgentControllerStateUseItemContinuesPrefix(ref float __0)
        {
            ActionSpeedNativeRuntime? current = runtime;
            if (current == null)
                return;
            float original = __0;
            try { current.AdjustUseItemContinues(ref __0); }
            catch (Exception ex)
            {
                __0 = original;
                current.RecordCallbackFailure("AgentControllerState.UseItemContinues", ex);
            }
        }

        public static void AgentControllerStateInteractContinuesPrefix(ref float __0)
        {
            ActionSpeedNativeRuntime? current = runtime;
            if (current == null)
                return;
            float original = __0;
            try { current.AdjustInteractContinues(ref __0); }
            catch (Exception ex)
            {
                __0 = original;
                current.RecordCallbackFailure("AgentControllerState.InteractContinues", ex);
            }
        }

        private static void Invoke(string operation, Action<ActionSpeedNativeRuntime> callback)
        {
            ActionSpeedNativeRuntime? current = runtime;
            if (current == null)
                return;
            try { callback(current); }
            catch (Exception ex) { current.RecordCallbackFailure(operation, ex); }
        }
    }
}
