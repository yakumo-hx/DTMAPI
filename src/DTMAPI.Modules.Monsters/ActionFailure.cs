using System;
using System.Collections.Generic;
using DTMAPI.MonsterContent;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        // A failed resource remains owned until its release actually succeeds.
        private static void Attempt(List<Exception> errors, Action release)
        { try { release(); } catch (Exception ex) { errors.Add(ex); } }
        private static void ThrowCleanup(List<Exception> errors)
        { if (errors.Count != 0) throw new AggregateException("Native cleanup incomplete", errors); }

        private MonsterDiagnostic FailAction(InstanceScope scope, string stage, Exception error)
        {
            if (scope.ActionFailure == null)
            {
                scope.Active = false;
                scope.FaultPending = true;
                if (scope.Action != null)
                { scope.Action.CleanupPending |= scope.Action.State == MonsterActionState.Running; scope.Action.InterruptionReason = MonsterActionInterruptionReason.None; scope.Action.State = MonsterActionState.Failed; }
                var definition = scope.Binding.Definition;
                scope.ActionFailure = new MonsterDiagnostic("NativeActionFailed", error.ToString(), definition.Owner,
                    definition.Id, definition.Definition.TemplateId, "Action." + stage, field: "",
                    recovery: "The instance has stopped; inspect native action diagnostics.",
                    providerId: scope.BehaviorRegistration?.Owner ?? "", instance: scope.Handle, action: scope.Action?.Handle);
                Log("Native action fault stage=" + stage + "; owner=" + definition.Owner + "; provider=" + scope.BehaviorRegistration?.Owner +
                    "; definition=" + definition.Id + "; instance=" + scope.Handle + "; action=" + scope.Action?.Handle + "; " + error);
            }
            DrainActionFailure(scope);
            return scope.ActionFailure;
        }
        private void DrainActionFailure(InstanceScope scope)
        {
            if (!scope.FaultPending || scope.AuthorDepth != 0 || scope.DrainingFault) return;
            scope.FaultPending = false; scope.DrainingFault = true;
            try { Revoke(scope, true); }
            catch (Exception cleanup)
            {
                // Revoke records RestartRequired on the exact definition. Keep all scopes and
                // resource records alive for owner withdrawal; never recycle a poisoned controller.
                Log("Native action cleanup RestartRequired instance=" + scope.Handle + "; " + cleanup);
                if (scope.Controller != null) scope.Controller.gameObject.SetActive(false);
            }
            finally { scope.DrainingFault = false; }
        }
        private MonsterResult<T> NativeCall<T>(InstanceScope scope, string stage, Func<MonsterResult<T>> work)
        {
            try { return work(); }
            catch (Exception ex) { return MonsterResult<T>.Fail(FailAction(scope, stage, ex)); }
        }
    }
}
