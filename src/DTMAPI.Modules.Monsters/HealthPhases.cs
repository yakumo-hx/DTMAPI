using DTMAPI.MonsterContent;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private void ObserveHealthPhase(InstanceScope scope)
        {
            var state = scope.HealthPhase;
            var monster = scope.Logical;
            if (state == null || monster == null || monster.IsDead || scope.Removed || scope.ActionFailure != null) return;
            int hp = ReadHealth(monster);
            state.Observe(hp, monster.proto.Health);
            if (hp <= 0 || !state.Pending || !scope.Active) return;
            if (scope.Action?.State == MonsterActionState.Running &&
                scope.Binding.Definition.Definition.HealthPhases!.Transition == MonsterPhaseTransition.CancelCurrent)
                EndAction(scope, MonsterActionState.PhaseChanged);
            CommitHealthPhase(scope);
        }
        private static void CommitHealthPhase(InstanceScope scope)
        {
            if (!scope.Active || scope.Logical == null || scope.Logical.IsDead || scope.Removed || scope.ActionFailure != null ||
                scope.Action?.Stopping == true || scope.Action?.CleanupPending == true) return;
            scope.HealthPhase?.Commit(ReadHealth(scope.Logical), scope.Action?.State == MonsterActionState.Running);
        }
    }
}
