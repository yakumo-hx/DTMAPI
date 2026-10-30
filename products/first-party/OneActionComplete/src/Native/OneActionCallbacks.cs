using System;
using DolocTown;
using UnityEngine;

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
        internal static void Detach(OneActionNativeRuntime value) { if (ReferenceEquals(runtime, value)) runtime = null; }

        public static void ToolColliderHandleToolsPrefix(ItemTool ___currentTool, Collider2D other, out OneActionEngine.ToolHit? __state)
        {
            __state = null;
            try { __state = runtime?.Engine.BeginToolHit(___currentTool, other); }
            catch (Exception ex) { runtime?.RecordCallbackFailure("ToolCollider.Before", ex); }
        }
        public static void ToolColliderHandleToolsPostfix(OneActionEngine.ToolHit? __state)
        {
            try { runtime?.Engine.CompleteToolHit(__state); }
            catch (Exception ex) { runtime?.RecordCallbackFailure("ToolCollider.After", ex); }
        }
        public static void AgentStateInteractExitPrefix(out OneActionEngine.FillAttempt? __state)
        {
            __state = null;
            try { __state = runtime?.Engine.BeginEquipmentFill(); }
            catch (Exception ex) { runtime?.RecordCallbackFailure("Interaction.Before", ex); }
        }
        public static void AgentStateInteractExitPostfix(OneActionEngine.FillAttempt? __state)
        {
            try { runtime?.Engine.CompleteEquipmentFill(__state); }
            catch (Exception ex) { runtime?.RecordCallbackFailure("Interaction.After", ex); }
        }
    }
}
