using System;
using System.Collections.Generic;
using DTMAPI.MonsterContent;
using DolocTown;
using DolocTown.Config.Monster;
using DolocTown.GameData;
using UnityEngine;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private const float DashWallSkin = .02f;
        private sealed class DashExecution
        {
            internal MonsterDash Parameters = null!;
            internal Transform? Target;
            internal Vector2 Direction, BodyOffset, BodySize;
            internal bool Started, Recovering, Closed;
            internal float RecoveryAt;
            internal string StopReason = "";
            internal readonly RaycastHit2D[] Hits = new RaycastHit2D[64];
            internal readonly HashSet<IAttackable> Receivers = new HashSet<IAttackable>();
            internal void Close()
            { Closed = true; Target = null; Receivers.Clear(); Array.Clear(Hits, 0, Hits.Length); }
        }
        private static bool SupportsDash(Template template)
        {
            bool mover = template.Proto.GenType == MonsterGenType.Ground && template.Proto.MoverProto is MonsterMoverProtoGround ||
                template.Proto.GenType == MonsterGenType.Air && template.Proto.MoverProto is MonsterMoverProtoCtxSteering;
            return mover && template.Prefab != null && template.Prefab.GetComponent<Collider2D>() != null &&
                template.Prefab.GetComponentInChildren<IMonsterSight>(true) != null;
        }
        private static bool DashTargetInRange(InstanceScope scope, Transform target, float range)
        {
            // Native Sting admits a physical dash by distance, not the shooting
            // ray's player-layer mask. The body sweep below owns wall clipping.
            return scope.Controller!.DistanceToTarget(target) <= range;
        }
        private MonsterResult<MonsterActionHandle> BeginDash(InstanceScope scope, MonsterSkill skill, float limit)
        {
            var controller = scope.Controller!;
            if (!SupportsDash(scope.Binding.Template)) return ActionError(scope, "DashUnsupported");
            var p = scope.HealthPhase?.Scale(skill.Dash!) ?? skill.Dash!;
            var target = Target(scope);
            if (target == null) return ActionError(scope, "TargetUnavailable");
            Physics2D.SyncTransforms();
            if (!DashTargetInRange(scope, target, p.Range)) return ActionError(scope, "TargetOutOfRange");
            if (scope.SkillReadyAt.TryGetValue(skill.Id, out var ready) && scope.SkillClock < ready) return ActionError(scope, "SkillCoolingDown");
            // Capture the native body footprint once. No collider, rigidbody, prototype,
            // mover speed or animation event is changed by this skill.
            var body = controller.Collider;
            var bounds = body.bounds;
            if (!body.enabled || !body.gameObject.activeInHierarchy || bounds.size.x <= .002f || bounds.size.y <= .002f)
                return ActionError(scope, "DashBodyUnavailable");
            var action = NewAction(scope, limit, false);
            action.Target = target.position;
            action.Dash = new DashExecution { Parameters = p, Target = target, BodySize = (Vector2)bounds.size - Vector2.one * .002f,
                BodyOffset = (Vector2)bounds.center - controller.position2d };
            scope.SkillReadyAt[skill.Id] = scope.SkillClock + p.CooldownGameSeconds;
            Fault("ActionStart");
            return MonsterResult<MonsterActionHandle>.Ok(action.Handle);
        }
        private static int DashCast(DashExecution dash, Vector2 origin, float length, int mask, bool triggers)
        {
            var filter = new ContactFilter2D { useTriggers = triggers };
            filter.SetLayerMask(mask);
            int count = Physics2D.BoxCast(origin, dash.BodySize, 0, dash.Direction, filter, dash.Hits, length);
            // A saturated result cannot prove that the nearest wall/receiver was included.
            if (count == dash.Hits.Length) throw new InvalidOperationException("DashCollisionCapacityExceeded");
            return count;
        }
        private void AdvanceDash(InstanceScope scope, ControlledAction action, float dt)
        {
            var dash = action.Dash!; var p = dash.Parameters; var controller = scope.Controller!;
            if (dash.Closed) return;
            if (dash.Recovering)
            {
                if (action.Elapsed - dash.RecoveryAt >= p.RecoveryGameSeconds) EndAction(scope, MonsterActionState.Completed);
                return;
            }
            if (!dash.Started)
            {
                var target = Target(scope);
                if (target == null || !ReferenceEquals(target, dash.Target) || !DashTargetInRange(scope, target, p.Range))
                { EndAction(scope, MonsterActionState.Interrupted, MonsterActionInterruptionReason.TargetInvalidated); return; }
                if (action.Elapsed < p.WindupGameSeconds) return;
                var direction = (Vector2)target.position - controller.position2d;
                if (scope.Logical!.proto.GenType == MonsterGenType.Ground) direction.y = 0;
                if (direction.sqrMagnitude < .000001f) { EndAction(scope, MonsterActionState.Interrupted, MonsterActionInterruptionReason.AimUnavailable); return; }
                dash.Direction = direction.normalized; dash.Started = true; dash.Target = null;
                action.Target = controller.position2d + dash.Direction * p.Distance;
                return; // Windup ends here; movement uses the following fixed game step.
            }
            var from = controller.position2d;
            // A position mismatch does not identify its writer. Never sweep a stale segment.
            if (Vector2.Distance(from, action.Position) > .05f) { EndAction(scope, MonsterActionState.Interrupted, MonsterActionInterruptionReason.ExternalDisplacement); return; }
            float requested = Math.Min(p.Speed * dt, p.Distance - action.Travel);
            float allowed = requested; string stop = "";
            int walls = DashCast(dash, from + dash.BodyOffset, requested + DashWallSkin, DolocAPI.gameConfig.groundMask, false);
            for (int i = 0; i < walls; i++)
            {
                var hit = dash.Hits[i];
                if (hit.collider == null || hit.collider.transform.IsChildOf(controller.transform) || !BattleUtils.IsWall(hit.collider)) continue;
                // Tangential floor contact is not a wall in the horizontal direction.
                if (Vector2.Dot(hit.normal, dash.Direction) >= -.001f) continue;
                float bound = Math.Max(0, hit.distance - DashWallSkin);
                if (bound <= allowed) { allowed = bound; stop = "Wall"; }
            }
            Array.Clear(dash.Hits, 0, dash.Hits.Length);
            var map = scope.Host.MonsterEnv.GetMap(scope.Logical!.proto.GenType);
            float safe = 0;
            int steps = Math.Max(1, (int)Math.Ceiling(allowed / .1f));
            for (int i = 1; i <= steps; i++)
            {
                float distance = allowed * i / steps;
                var cell = map.WorldToCell(from + dash.Direction * distance);
                if (map.IsObstacleOrInvalid(cell) || scope.Logical.proto.GenType == MonsterGenType.Ground && !map.IsGround(cell))
                { allowed = safe; stop = "MapBoundary"; break; }
                safe = distance;
            }
            // Clip both ends of the damage sweep to this step's actual traversed window.
            float hitFrom = Math.Max(action.Travel, p.Distance * p.HitStartFraction);
            float hitTo = Math.Min(action.Travel + allowed, p.Distance * p.HitEndFraction);
            if (hitTo > hitFrom)
            {
                int hits = DashCast(dash, from + dash.BodyOffset + dash.Direction * (hitFrom - action.Travel), hitTo - hitFrom,
                    Physics2D.AllLayers, true);
                for (int i = 0; i < hits; i++)
                {
                    if (dash.Closed || !scope.Active || action.State != MonsterActionState.Running) break;
                    var collider = dash.Hits[i].collider;
                    // Match PhysicalDamageBox's receiver rules, including a
                    // Default-layer player and the native perfect-dodge state.
                    if (collider == null || !collider.CompareTag("Player")) continue;
                    var receiver = collider.GetComponent<IAttackable>();
                    if (receiver == null || !dash.Receivers.Add(receiver) || p.Damage == 0) continue;
                    if (receiver is BodyController body && body.IsPerfectDodge) { DolocAPI.PerfectDodgeEffects(); continue; }
                    receiver.OnAttacked(p.Damage, false, from, AttackProperties.Physical, out _);
                }
                Array.Clear(dash.Hits, 0, dash.Hits.Length);
                if (dash.Closed || !scope.Active || action.State != MonsterActionState.Running) return;
            }
            controller.position2d = from + dash.Direction * allowed;
            action.Position = controller.position2d; action.Travel += allowed;
            if (stop.Length != 0 || action.Travel >= p.Distance - .00001f)
            {
                dash.StopReason = stop.Length == 0 ? "Distance" : stop;
                dash.Recovering = true; dash.RecoveryAt = action.Elapsed; dash.Receivers.Clear();
                if (p.RecoveryGameSeconds == 0) EndAction(scope, MonsterActionState.Completed);
            }
        }
    }
}
