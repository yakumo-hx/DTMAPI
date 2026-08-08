using System;

namespace Yuuka.DTMAPI.OneActionComplete
{
    internal static class OneActionCallbacks
    {
        private static OneActionNativeRuntime? runtime;

        internal static void Attach(OneActionNativeRuntime value)
        {
            if (runtime != null && !ReferenceEquals(runtime, value))
                throw new InvalidOperationException("OneActionComplete callback runtime is already attached.");
            runtime = value ?? throw new ArgumentNullException(nameof(value));
        }

        internal static void Detach(OneActionNativeRuntime value)
        {
            if (ReferenceEquals(runtime, value))
                runtime = null;
        }

        public static void ToolColliderHandleToolsPostfix(object __instance, object other)
        {
            OneActionNativeRuntime? current = runtime;
            if (current == null)
                return;
            try { current.ApplyToolHit(__instance, other); }
            catch (Exception ex) { current.RecordCallbackFailure("ToolCollider.HandleTools", ex); }
        }

        public static void AgentStateInteractExitPostfix()
        {
            OneActionNativeRuntime? current = runtime;
            if (current == null)
                return;
            try { current.ApplyEquipmentFill(); }
            catch (Exception ex) { current.RecordCallbackFailure("AgentStateInteract.OnExit", ex); }
        }
    }
}
