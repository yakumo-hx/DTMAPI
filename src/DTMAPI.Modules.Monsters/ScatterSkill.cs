using System;
using System.Linq;
using DTMAPI.MonsterContent;
using DolocTown;
using DolocTown.GameData;
using UnityEngine;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        // Borrows the already acquired attack's manager. No new pool/reference, native
        // proto mutation, animation damage event or independent decision clock.
        private sealed class ScatterExecution
        {
            internal MonsterScatter Parameters = null!;
            internal BulletManager Manager = null!;
            internal Transform? Target;
            internal bool Fired;
            internal float FiredAt;
            internal void Hit(Bullet bullet, Collider2D other)
            {
                if (bullet.IsRecycled) return; // Multiple colliders in one physics step.
                if (Parameters.Damage > 0)
                    BattleUtils.OnBulletHitPlayer(Manager, Parameters.Damage, 0, bullet, other);
                else
                {
                    var kind = other.GetComponent<IAttackable>()?.attackableType;
                    if (BattleUtils.IsWall(other) || kind == AttackableType.Player || kind == AttackableType.None) bullet.Recycle();
                }
            }
        }
        private MonsterResult<MonsterActionHandle> BeginSkill(InstanceScope scope, string id, float limit)
        {
            string? error = Admission(scope, limit);
            if (error != null) return ActionError(scope, error);
            var skill = scope.Binding.Definition.Definition.Skills.FirstOrDefault(s => s.Id == id);
            if (skill == null) return ActionError(scope, "SkillMissing");
            if (skill.Kind == "dash") return BeginDash(scope, skill, limit);
            var parameters = scope.HealthPhase?.Scale(skill.Scatter!) ?? skill.Scatter!;
            var controller = scope.Controller!;
            if (controller.MonsterAI is CodeAi ai && !ai.HasNativeTargetProvider) return ActionError(scope, "TargetingUnsupported");
            var target = Target(scope);
            if (target == null) return ActionError(scope, "TargetUnavailable");
            if (!controller.RayTest(target, parameters.Range)) return ActionError(scope, "TargetOutOfRange");
            if (scope.SkillReadyAt.TryGetValue(id, out var ready) && scope.SkillClock < ready) return ActionError(scope, "SkillCoolingDown");
            if (!Enum.TryParse(parameters.ProjectileAttackId, out MonsterAttackId sourceId) ||
                !controller.attackBehaviourManager.TryGetAttackBehaviour(sourceId, out var source) || !(source is MonsterAttackBehaviourBullet))
                return ActionError(scope, "ScatterUnsupported");
            var manager = NativeAccess.Get<BulletManager>(source, "_bulletManager");
            if (manager == null || manager.IsDisposed || !(manager.MoverProto is BulletMoverProtoLinear)) return ActionError(scope, "ScatterUnsupported");
            var action = NewAction(scope, limit, false);
            action.Target = target.position;
            action.Scatter = new ScatterExecution { Parameters = parameters, Manager = manager, Target = target };
            scope.SkillReadyAt[id] = scope.SkillClock + parameters.CooldownGameSeconds;
            Fault("ActionStart");
            BeginAnimation(scope, action, id);
            return MonsterResult<MonsterActionHandle>.Ok(action.Handle);
        }
        private void AdvanceScatter(InstanceScope scope, ControlledAction action)
        {
            var scatter = action.Scatter!;
            var parameters = scatter.Parameters;
            if (!scatter.Fired)
            {
                var target = Target(scope);
                if (target == null || !ReferenceEquals(target, scatter.Target) || !scope.Controller!.RayTest(target, parameters.Range))
                { EndAction(scope, MonsterActionState.Interrupted, MonsterActionInterruptionReason.TargetInvalidated); return; }
                if (action.Elapsed < parameters.WindupGameSeconds) { AdvanceAnimation(action); return; }
                Vector2 origin = scope.Controller!.FirePosition;
                Vector2 direction = (Vector2)target.position - origin;
                if (direction.sqrMagnitude < .000001f) { EndAction(scope, MonsterActionState.Interrupted, MonsterActionInterruptionReason.AimUnavailable); return; }
                direction.Normalize();
                action.Target = target.position;
                scatter.Fired = true; scatter.FiredAt = action.Elapsed; scatter.Target = null;
                AdvanceAnimation(action); // The firing frame and the volley share this exact fixed step.
                for (int i = 0; i < parameters.BulletCount; i++)
                {
                    if (!scope.Active || action.Stopping || action.State != MonsterActionState.Running) return;
                    float degrees = parameters.BulletCount == 1 ? 0 : parameters.SpreadDegrees * (i / (float)(parameters.BulletCount - 1) - .5f);
                    float angle = degrees * Mathf.Deg2Rad;
                    var rotated = new Vector2(direction.x * Mathf.Cos(angle) - direction.y * Mathf.Sin(angle), direction.x * Mathf.Sin(angle) + direction.y * Mathf.Cos(angle));
                    var shot = TrackShot(scope, scatter.Manager, scatter.Hit, null);
                    scatter.Manager.InvokeBullet(origin, rotated, parameters.BulletSpeed, parameters.BulletLifetimeGameSeconds, shot.OnHit, shot.OnCreated, true);
                }
            }
            AdvanceAnimation(action);
            action.Position = scope.Controller!.position2d;
            if (action.Elapsed - scatter.FiredAt < parameters.RecoveryGameSeconds) return;
            // Keep the cancellation token alive until both recovery and the finite
            // volley end. Consequently a completed skill leaves no damaging tail.
            foreach (var shot in scope.Shots)
                if (shot.ActionHandle.HasValue && shot.ActionHandle.Value.Equals(action.Handle)) return;
            EndAction(scope, MonsterActionState.Completed);
        }
    }
}
