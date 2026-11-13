using System;
using System.Collections.Generic;
using System.Linq;
using DTMAPI.MonsterContent;
using DolocTown;
using UnityEngine;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private sealed class ControlledAction
        {
            internal MonsterActionHandle Handle;
            internal MonsterActionState State = MonsterActionState.Running;
            internal MonsterActionInterruptionReason InterruptionReason;
            internal float Elapsed, Travel, Limit, MaxTravel;
            internal Vector2 Position, Target, StartPosition;
            internal bool Moving;
            internal bool Stopping, CleanupPending;
            internal MonsterAttackBehaviour? Attack;
            internal ScatterExecution? Scatter;
            internal DashExecution? Dash;
            internal AnimationExecution? Animation;
            internal MonsterActionSnapshot Snapshot() => new MonsterActionSnapshot(Handle, State, InterruptionReason, Elapsed, Travel, Position.x, Position.y, Target.x, Target.y);
        }
        private static bool Finite(float n) => !float.IsNaN(n) && !float.IsInfinity(n);
        private static bool HurtLocked(MonsterController controller)
        {
            var timer = NativeAccess.Get(controller, "_hurtTimer")!;
            return (bool)timer.GetType().GetProperty("IsLocked")!.GetValue(timer)!;
        }
        private MonsterResult<MonsterActionHandle> ActionError(InstanceScope scope, string code) => MonsterResult<MonsterActionHandle>.Fail(new MonsterDiagnostic(code, code, scope.Binding.Definition.Owner, scope.Binding.Definition.Id, stage: "Action"));
        private string? Admission(InstanceScope scope, float limit)
        {
            if (scope.ActionFailure != null || !scope.Active) return "NativeActionFailed";
            if (scope.Action?.Stopping == true || scope.Action?.CleanupPending == true) return "ActionStopping";
            if (scope.HealthPhase != null && (scope.Logical!.IsDead || ReadHealth(scope.Logical) <= 0)) return "InstanceDead";
            ObserveHealthPhase(scope);
            if (DolocAPI.gameLoop.IsPaused || DolocAPI.gameLoop.IsGlobalPaused) return "GamePaused";
            if (!Finite(limit) || limit <= 0) return "InvalidActionLimit";
            if (scope.Action?.State == MonsterActionState.Running) return "ActionBusy";
            if (HurtLocked(scope.Controller!)) return "HurtLocked";
            return null;
        }
        private Transform? Target(InstanceScope scope)
        {
            var target = scope.Controller!.MonsterAI.LockedTarget;
            return scope.Host == (IMonsterHost)DolocAPI.CurrentRoom && target != null && target.gameObject.activeInHierarchy ? target : null;
        }
        private IReadOnlyList<MonsterAttackOption> AttackOptions(InstanceScope scope)
        {
             {
            var target = Target(scope);
            var manager = scope.Controller!.attackBehaviourManager;
            var attacks = scope.AttackDescriptors ?? (scope.AttackDescriptors = manager.AttackBehaviours.Select(a => new AttackDescriptor(a)).ToArray());
            var result = new MonsterAttackOption[attacks.Length];
            for (int i = 0; i < attacks.Length; i++)
            { var a = attacks[i]; result[i] = new MonsterAttackOption(a.Id, manager.IsAvailable(a.Behavior._proto.AttackId), IsNativeSelfAttack(a.Behavior) || target != null && a.Behavior.IsTargetInAttackRange(target), a.Behavior.CDDuration); }
            return result;
            }
        }
        private sealed class AttackDescriptor
        {
            internal readonly MonsterAttackBehaviour Behavior;
            internal readonly string Id;
            internal AttackDescriptor(MonsterAttackBehaviour behavior) { Behavior = behavior; Id = behavior._proto.AttackId.ToString(); }
        }
        private ControlledAction NewAction(InstanceScope scope, float limit, bool move)
        {
            var action = new ControlledAction { Handle = new MonsterActionHandle(scope.Handle, ++scope.ActionSequence), Limit = limit, Moving = move, Position = scope.Controller!.position2d, StartPosition = scope.Controller.position2d };
            scope.Action = action;
            return action;
        }
        private MonsterResult<MonsterActionHandle> BeginAttack(InstanceScope scope, string id, float limit)
        {
            string? error = Admission(scope, limit);
            if (error != null) return ActionError(scope, error);
            var controller = scope.Controller!;
            var manager = controller.attackBehaviourManager;
            if (!Enum.TryParse(id, out MonsterAttackId attackId) || !manager.TryGetAttackBehaviour(attackId, out var attack)) return ActionError(scope, "AttackMissing");
            bool selfAttack = IsNativeSelfAttack(attack);
            if (!selfAttack && controller.MonsterAI is CodeAi ai && !ai.HasNativeTargetProvider) return ActionError(scope, "TargetingUnsupported");
            var target = selfAttack ? controller.transform : Target(scope);
            if (target == null) return ActionError(scope, "TargetUnavailable");
            if (!selfAttack && !attack.IsTargetInAttackRange(target)) return ActionError(scope, "TargetOutOfRange");
            if (!manager.IsAvailable(attackId)) return ActionError(scope, "AttackCoolingDown");
            var action = NewAction(scope, limit, false); action.Attack = attack; action.Target = target.position;
            Fault("ActionStart"); controller.Attack(attackId, target);
            return MonsterResult<MonsterActionHandle>.Ok(action.Handle);
        }
        private MonsterResult<MonsterActionHandle> BeginMove(InstanceScope scope, float x, float y, float limit, float maxTravel)
        {
            string? error = Admission(scope, limit);
            if (error != null) return ActionError(scope, error);
            if (!Finite(x) || !Finite(y) || !Finite(maxTravel) || maxTravel <= 0) return ActionError(scope, "InvalidMoveLimit");
            var mover = scope.Controller!.mover;
            if (mover is MonsterMoverNone) return ActionError(scope, "MovementUnsupported");
            if (!(mover is MonsterMoverGround) && !(mover is MonsterMoverContextSteering)) return ActionError(scope, "MovementUnsupported");
            var map = scope.Host.MonsterEnv.GetMap(scope.Logical!.proto.GenType);
            if (!map.IsValidPosition(map.WorldToCell(new Vector2(x, y)))) return ActionError(scope, "InvalidPosition");
            var action = NewAction(scope, limit, true); action.MaxTravel = maxTravel;
            Fault("ActionStart");
                if (!mover.TryMoveTo(new Vector2(x, y))) { EndAction(scope, MonsterActionState.Failed); return ActionError(scope, "PathUnavailable"); }
                var path = mover.originPath;
                if (path == null || path.Length == 0) { EndAction(scope, MonsterActionState.Failed); return ActionError(scope, "PathUnavailable"); }
                action.Target = path[path.Length - 1];
                float length = 0; var previous = action.Position;
                foreach (var point in path) { length += Vector2.Distance(previous, point); previous = point; }
                if (length > maxTravel) { EndAction(scope, MonsterActionState.TravelLimit); return ActionError(scope, "TravelLimit"); }
                return MonsterResult<MonsterActionHandle>.Ok(action.Handle);
        }
        private static bool ControlledFixedPrefix(MonsterController __instance, float dt)
        {
            if (current == null || !current.controllers.TryGetValue(__instance, out var scope)) return true;
            if (scope.ActionFailure != null) return false;
            if (!scope.Active || !UsesCode(scope)) return true;
            if (DolocAPI.gameLoop.IsPaused || DolocAPI.gameLoop.IsGlobalPaused) return false;
            try { current.ObserveHealthPhase(scope); }
            catch (Exception ex) { current.FailAction(scope, "HealthPhase.Fixed", ex); return false; }
            if (dt > 0) scope.SkillClock += dt;
            if (dt > 0 && scope.Behavior is IConfiguredBehavior configured && HurtLocked(__instance)) configured.TickHurtWait(dt);
            var action = scope.Action;
            if (action == null || action.State != MonsterActionState.Running || dt <= 0) return true;
            if ((action.Scatter != null || action.Dash != null) && HurtLocked(__instance))
            {
                try { current.EndAction(scope, MonsterActionState.Interrupted, MonsterActionInterruptionReason.Hurt); }
                catch (Exception ex) { current.FailAction(scope, "HurtInterrupt", ex); }
                return scope.Active;
            }
            // This native renderer replaces jump-ready/landing animation on hurt recovery.
            // Retiring the controlled move avoids waiting forever on an animation it replaced.
            if (action.Moving && __instance.GetComponent<MonsterMoverGroundRendererAmoeba>() != null && HurtLocked(__instance))
            {
                try { current.EndAction(scope, MonsterActionState.Interrupted, MonsterActionInterruptionReason.Hurt); }
                catch (Exception ex) { current.FailAction(scope, "HurtInterrupt", ex); }
                return scope.Active;
            }
            action.Elapsed += dt; // Native fixed game time, including hurt lock, never wall time/catch-up.
            if (action.Elapsed >= action.Limit)
            {
                action.Elapsed = action.Limit;
                try { current.EndAction(scope, MonsterActionState.TimedOut); }
                catch (Exception ex) { current.FailAction(scope, "Timeout", ex); }
            }
            return scope.Active;
        }
        private void AdvanceAction(InstanceScope scope, float dt)
        {
            var action = scope.Action;
            if (action == null || action.State != MonsterActionState.Running) return;
            Fault("ActionAdvance");
            if (action.Scatter != null) { AdvanceScatter(scope, action); return; }
            if (action.Dash != null) { AdvanceDash(scope, action, dt); return; }
            var controller = scope.Controller!;
            if (!action.Moving && !controller.attackBehaviourManager.IsAttacking) { EndAction(scope, MonsterActionState.Interrupted, MonsterActionInterruptionReason.NativeAttackStopped); return; }
            bool complete = action.Moving ? controller.mover.Move(dt) : controller.attackBehaviourManager.Update(dt);
            var next = controller.position2d;
            float segment = Vector2.Distance(action.Position, next);
            if (action.Moving && action.Travel + segment > action.MaxTravel)
            {
                var bounded = Vector2.Lerp(action.Position, next, Math.Max(0, action.MaxTravel - action.Travel) / segment);
                var position = controller.transform.position;
                controller.transform.position = new Vector3(bounded.x, bounded.y, position.z);
                action.Position = bounded; action.Travel = action.MaxTravel;
                EndAction(scope, MonsterActionState.TravelLimit);
            }
            else { action.Travel += segment; action.Position = next; if (complete) EndAction(scope, MonsterActionState.Completed); }
        }
        private void EndAction(InstanceScope scope, MonsterActionState state, MonsterActionInterruptionReason interruptionReason = MonsterActionInterruptionReason.None)
        {
            var action = scope.Action;
            if (action == null || action.Stopping || (action.State != MonsterActionState.Running && !action.CleanupPending)) return;
            action.Stopping = true;
            action.CleanupPending = state != MonsterActionState.Completed;
            var errors = new List<Exception>();
            try
            {
                if (action.Scatter != null) action.Scatter.Target = null;
                if (action.Dash != null) action.Dash.Close();
                Attempt(errors, () => EndAnimation(action));
                Attempt(errors, () => { if (scope.Controller != null) action.Position = scope.Controller.position2d; });
                if (action.CleanupPending)
                {
                    Attempt(errors, () => { Fault("ActionCancelEffects"); CancelFollowingEffects(scope, action.Handle); });
                    if (action.Moving) Attempt(errors, () => { Fault("ActionCancelMovement"); CancelMovement(scope); });
                    else if (scope.Controller != null)
                    {
                        var controller = scope.Controller;
                        Attempt(errors, () => { if (action.Attack != null) { CancelChomperBite(action.Attack); CancelTweens(action.Attack); } });
                        Attempt(errors, () => NativeAccess.Set(controller.attackBehaviourManager, "_callback", null));
                        Attempt(errors, () => NativeAccess.Set(controller.attackBehaviourManager, "_currentBehaviour", null));
                        Attempt(errors, () => NativeAccess.Set(controller, "animationEvtCallback", null));
                        Attempt(errors, () => CancelTweens(controller.Renderer.AttackBehaviourRenderer));
                        foreach (var box in controller.GetComponentsInChildren<PhysicalDamageBox>(true))
                            Attempt(errors, () => { box.OnAttackedCallback = null; box.Enabled = false; box.ClearBuffer(); });
                        Attempt(errors, () => { Fault("ActionCancelShots"); CancelShots(scope, action.Handle); });
                        Attempt(errors, () => CancelNativeSkills(scope, action.Handle));
                    }
                    Attempt(errors, () => SettleCancelledGroundAction(scope, action));
                    if (action.Dash == null) Attempt(errors, () => RestoreActionPresentation(scope, action.Moving));
                }
                else if (action.Moving) Attempt(errors, () => RestoreActionPresentation(scope, true));
                ThrowCleanup(errors);
                action.CleanupPending = false;
                if (scope.ActionFailure == null)
                {
                    // Publish a cause only after cleanup succeeds; a fault never rearms combat.
                    action.InterruptionReason = state == MonsterActionState.Interrupted ? interruptionReason : MonsterActionInterruptionReason.None;
                    action.State = state;
                }
            }
            finally { action.Stopping = false; }
            CommitHealthPhase(scope);
        }
    }
}
