using System;
using System.Linq;
using DG.Tweening;
using DTMAPI.MonsterContent;
using DolocTown;
using DolocTown.Config.Monster;
using DolocTown.GameData;
using DolocTown.MonsterAttackBehaviours;
using HarmonyLib;
using RedSaw.GameMap;
using UnityEngine;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private static readonly string[] SightFields = { "enemyThreshold", "minAlertAddition", "maxAlertAddition", "maxAlertDuration", "tooFarDistance" };
        private static readonly System.Reflection.MethodInfo PhysicalJumpStop = AccessTools.Method(typeof(MonsterAttackBehaviourAmoebaJumpAttack), "Stop");
        private static readonly System.Reflection.MethodInfo PhysicalJumpEnd = AccessTools.Method(typeof(MonsterAttackBehaviour), "End");
        private static Component? EditableSight(GameObject? prefab)
        {
            var sights = prefab == null ? Array.Empty<Component>() : prefab.GetComponentsInChildren<Component>(true)
                .Where(c => c is MonsterSight || c is MonsterSightEx).ToArray();
            if (sights.Length != 1) return null;
            var collider = sights[0].GetComponent<Collider2D>();
            return collider is CircleCollider2D || collider is BoxCollider2D ? sights[0] : null;
        }
        private static MonsterPhysicalInfo PhysicalInfo(Template template)
        {
            var proto = template.Proto.MoverProto;
            MonsterMovementParameters? movement = null; string kind = "unsupported";
            if (proto is MonsterMoverProtoGround ground)
            {
                kind = "ground";
                movement = new MonsterMovementParameters { MoveSpeed = ground.moveSpeed, JumpSpeed = ground.jumpSpeed, TargetRadius = ground.targetRadius };
            }
            else if (proto is MonsterMoverProtoCtxSteering context)
            {
                kind = "context-steering";
                movement = new MonsterMovementParameters { MoveSpeed = context.maxSpeed, MaxAcceleration = context.maxAcceleration,
                    TargetRadius = context.targetRadius, SlowRadius = context.slowRadius, TimeToTarget = context.timeToTarget,
                    DirectionCount = context.dirCount, DangerRadius = context.dangerRadius, Sensitivity = context.sensitive, RotationOffset = context.rotationOffset };
            }
            if (proto is MonsterMoverProtoShuttle shuttle) { kind = "shuttle"; movement = new MonsterMovementParameters { WaitGameSeconds = shuttle.waitDuration, MoveGameSeconds = shuttle.moveDuration }; }
            if (movement != null) { movement.AroundRange = proto.aroundRange; movement.Directional = proto.isDirectional; }
            var sight = EditableSight(template.Prefab);
            MonsterSightParameters? values = sight == null ? null : new MonsterSightParameters { RangeScale = 1, DisableEmotion = NativeAccess.Get<bool>(sight, sight is MonsterSight ? "DisableEmotion" : "disableEmotion"),
                EnemyThreshold = NativeAccess.Get<float>(sight, SightFields[0]), MinAlertAddition = NativeAccess.Get<float>(sight, SightFields[1]),
                MaxAlertAddition = NativeAccess.Get<float>(sight, SightFields[2]), MaxAlertDuration = NativeAccess.Get<float>(sight, SightFields[3]),
                TooFarDistance = NativeAccess.Get<float>(sight, SightFields[4]) };
            var attacks = template.Proto.AttackBehaviourProtos?.Where(a => a != null && a.AttackId != MonsterAttackId.Unset).ToArray();
            var collider = template.Prefab?.GetComponent<Collider2D>();
            var scale = template.Prefab?.transform.localScale ?? Vector3.zero;
            // This is a mechanism boundary, not an ID allow-list. Other native body owners
            // require their own trajectory/collider proof before advertising static scale.
            bool body = template.Errors.Count == 0 && proto is MonsterMoverProtoGround && template.Proto.GenType == MonsterGenType.Ground
                && (collider is BoxCollider2D || collider is CapsuleCollider2D || collider is CircleCollider2D) && Mathf.Abs(scale.x) == 1 && scale.y == 1
                && (!(collider is CircleCollider2D footprint) || footprint.radius - footprint.offset.y <= BodySkin)
                && attacks?.Length > 0 && attacks.All(a => a is IAmoebaJumpAttack);
            return new MonsterPhysicalInfo(body, body ? "" : "Static scale requires Ground/AmoebaJumpAttack with a unit-size root box, capsule or circle. Actual collider="
                + (collider?.GetType().Name ?? "missing") + "; scale=" + scale + "; mover=" + kind + "; attacks=" + string.Join(",", attacks?.Select(a => a.GetType().Name) ?? Array.Empty<string>()),
                template.Row?.Size ?? 0, template.Row?.Offset.x ?? 0, template.Row?.Offset.y ?? 0, kind, movement, sight?.GetType().Name ?? "unsupported", values);
        }
        private static MonsterMoverProto DefinitionMover(Template template, MonsterDefinition definition)
        {
            var p = definition.Movement; var native = template.Proto.MoverProto;
            if (p == null) return native;
            if (native is MonsterMoverProtoGround g)
                return new MonsterMoverProtoGround(g.pathFinderProto, p.Directional ?? g.isDirectional, p.AroundRange ?? g.aroundRange,
                    p.TargetRadius ?? g.targetRadius, p.MoveSpeed ?? g.moveSpeed, p.JumpSpeed ?? g.jumpSpeed);
            if (native is MonsterMoverProtoCtxSteering c)
                return new MonsterMoverProtoCtxSteering(c.pathFinderProto, p.Directional ?? c.isDirectional, p.AroundRange ?? c.aroundRange,
                    p.MoveSpeed ?? c.maxSpeed, p.MaxAcceleration ?? c.maxAcceleration, p.TargetRadius ?? c.targetRadius,
                    p.SlowRadius ?? c.slowRadius, p.TimeToTarget ?? c.timeToTarget, p.DirectionCount ?? c.dirCount,
                    p.DangerRadius ?? c.dangerRadius, p.Sensitivity ?? c.sensitive, p.RotationOffset ?? c.rotationOffset, c.Runtime);
            if (native is MonsterMoverProtoShuttle shuttle)
                return new MonsterMoverProtoShuttle(shuttle.pathFinderProto, p.Directional ?? shuttle.isDirectional, p.AroundRange ?? shuttle.aroundRange, p.WaitGameSeconds ?? shuttle.waitDuration, p.MoveGameSeconds ?? shuttle.moveDuration);
            throw new InvalidOperationException("MovementParametersUnsupported");
        }
        private static string PhysicalFingerprint(MonsterPhysicalInfo? p)
        {
            if (p == null) return "";
            var m = p.Movement; var s = p.Sight;
            string N(float? value) => value?.ToString("R", System.Globalization.CultureInfo.InvariantCulture) ?? "inherit";
            return string.Join(":", p.BodyScale, p.BodyReason, p.SpawnVolume, N(p.SpawnOffsetX), N(p.SpawnOffsetY), p.MovementKind,
                N(m?.WaitGameSeconds), N(m?.MoveGameSeconds), N(m?.MoveSpeed), N(m?.JumpSpeed), N(m?.TargetRadius), N(m?.MaxAcceleration), N(m?.SlowRadius), N(m?.TimeToTarget),
                N(m?.DangerRadius), N(m?.Sensitivity), N(m?.RotationOffset), m?.DirectionCount, m?.AroundRange, m?.Directional,
                p.SightKind, N(s?.RangeScale), N(s?.EnemyThreshold), N(s?.MinAlertAddition), N(s?.MaxAlertAddition), N(s?.MaxAlertDuration), N(s?.TooFarDistance), s?.DisableEmotion?.ToString() ?? "");
        }
        private static void PreparePhysical(Binding binding)
        {
            if (binding.Definition.Definition.Body == null && binding.Definition.Definition.Sight == null) return;
            float scale = binding.Definition.Definition.Body?.Scale ?? 1;
            binding.Container!.transform.localScale = Vector3.one * scale;
            var sight = EditableSight(binding.Prefab);
            if (sight == null) return;
            var p = binding.Definition.Definition.Sight;
            // Native alert distance uses world distance; the collider remains a local shape.
            float ratio = (p?.RangeScale ?? 1) / scale;
            var collider = sight.GetComponent<Collider2D>();
            if (collider is CircleCollider2D circle) circle.radius *= ratio;
            else if (collider is BoxCollider2D box) box.size *= ratio;
            NativeAccess.Set(sight, "_collider", null);
            if (p == null) return;
            if (p.DisableEmotion.HasValue) NativeAccess.Set(sight, sight is MonsterSight ? "DisableEmotion" : "disableEmotion", p.DisableEmotion.Value);
            float?[] values = { p.EnemyThreshold, p.MinAlertAddition, p.MaxAlertAddition, p.MaxAlertDuration, p.TooFarDistance };
            for (int i = 0; i < values.Length; i++) if (values[i].HasValue) NativeAccess.Set(sight, SightFields[i], values[i]!.Value);
        }
        private void InstallPhysicalHooks()
        {
            Patch(AccessTools.Method(typeof(MonsterMover), "TryFindPath"), postfix: nameof(PhysicalPathFound));
            Patch(AccessTools.Method(typeof(MonsterMoverGround), "OnUpdate"), prefix: nameof(PhysicalMoveBegin), postfix: nameof(PhysicalMoveEnd));
            Patch(AccessTools.Method(typeof(MonsterAttackBehaviourAmoebaJumpAttack), "GetAttackPointEx"), postfix: nameof(PhysicalAttackLanding));
            Patch(AccessTools.Method(typeof(MonsterAttackBehaviourAmoebaJumpAttack), "Invoke"), postfix: nameof(PhysicalAttackInvoked));
            Patch(AccessTools.Method(typeof(MonsterAttackBehaviourAmoebaJumpAttack), "OnUpdate"), prefix: nameof(PhysicalAttackBegin), postfix: nameof(PhysicalAttackEnd));
            Patch(AccessTools.PropertyGetter(typeof(MonsterSight), "Collider"), postfix: nameof(PhysicalSightCollider));
            Patch(AccessTools.PropertyGetter(typeof(MonsterSightEx), "Collider"), postfix: nameof(PhysicalSightCollider));
        }
        private static void PhysicalSightCollider(Component __instance, Collider2D __result)
        {
            if (current == null || !current.controllers.TryGetValue(__instance.GetComponentInParent<MonsterController>(), out var scope)
                || scope.Binding.Definition.Definition.Sight == null && scope.Binding.Definition.Definition.Body == null) return;
            float radius = __result is CircleCollider2D circle ? circle.radius : ((BoxCollider2D)__result).size.x * .5f;
            NativeAccess.SetProperty(__instance, "CircleRadius", radius * Mathf.Abs(__instance.transform.lossyScale.x));
        }
        private static bool HasPhysicalBody(InstanceScope? scope) => scope?.Binding.Definition.Definition.Body != null && !scope.Binding.NativeChild;
        private static InstanceScope? PhysicalScope(object owner)
        {
            if (current == null) return null;
            var controller = NativeAccess.Get<MonsterController>(owner, "_controller");
            return current.controllers.TryGetValue(controller, out var scope) && HasPhysicalBody(scope) ? scope : null;
        }
        private readonly RaycastHit2D[] physicalHits = new RaycastHit2D[32];
        private readonly Collider2D[] physicalOverlaps = new Collider2D[32];
        private const float BodySkin = .02f;
        private static void BodyBox(Binding binding, Vector2 root, out Vector2 center, out Vector2 size)
        {
            var collider = binding.Prefab!.GetComponent<Collider2D>();
            var localSize = collider is BoxCollider2D box ? box.size : collider is CircleCollider2D circle ? Vector2.one * circle.radius * 2 : ((CapsuleCollider2D)collider).size;
            float scale = binding.Definition.Definition.Body?.Scale ?? 1;
            // Cover both native facing signs, including asymmetric root offsets.
            center = root + new Vector2(0, collider.offset.y * scale);
            size = new Vector2(localSize.x + Mathf.Abs(collider.offset.x) * 2, localSize.y) * scale;
            if (collider is CircleCollider2D foot)
            {
                // Native circular amoebas rest at the map pivot with a sub-skin toe
                // below it (radiation: radius about 1.31, offsetY 1.30). Preserve native
                // floor contact under scale; lateral and top clearance stay complete.
                float inset = Mathf.Clamp(foot.radius - foot.offset.y, 0, BodySkin) * scale;
                center.y += inset * .5f; size.y -= inset;
            }
            size = Vector2.Max(size - Vector2.one * BodySkin * 2, Vector2.one * .01f);
        }
        private bool BodyClear(IMonsterHost host, Binding binding, Vector2 root, Transform? self = null, bool checkUnscaled = false)
        {
            if (binding.Definition.Definition.Body == null && !checkUnscaled || binding.NativeChild) return true;
            BodyBox(binding, root, out var center, out var size);
            var map = host.MonsterEnv.GetMap(binding.Proto.GenType);
            var min = map.WorldToCell(center - size * .5f); var max = map.WorldToCell(center + size * .5f);
            for (int x = min.x; x <= max.x; x++) for (int y = min.y; y <= max.y; y++)
                if (map.IsObstacleOrInvalid(new Vector2Int(x, y))) return false;
            var filter = new ContactFilter2D { useTriggers = false }; filter.SetLayerMask(DolocAPI.gameConfig.groundMask);
            int count = Physics2D.OverlapBox(center, size, 0, filter, physicalOverlaps);
            if (count == physicalOverlaps.Length) { Array.Clear(physicalOverlaps, 0, count); return false; }
            for (int i = 0; i < count; i++)
            {
                var c = physicalOverlaps[i]; physicalOverlaps[i] = null!;
                if (c != null && c.enabled && (self == null || !c.transform.IsChildOf(self)) && BattleUtils.IsWall(c))
                { Array.Clear(physicalOverlaps, i + 1, count - i - 1); return false; }
            }
            return true;
        }
        private bool GuardBodyStep(InstanceScope scope, Vector2 from)
        {
            var controller = scope.Controller!; Vector2 to = controller.position2d, delta = to - from;
            NormalizeBodyFacing(controller);
            if (GroundedBodyPosition(scope, from, out var ground)) scope.PhysicalGroundPosition = ground;
            float length = delta.magnitude; if (length < .00001f) return true;
            BodyBox(scope.Binding, from, out var center, out var size);
            Vector2 direction = delta / length; float allowed = length;
            var filter = new ContactFilter2D { useTriggers = false }; filter.SetLayerMask(DolocAPI.gameConfig.groundMask);
            int count = Physics2D.BoxCast(center, size, 0, direction, filter, physicalHits, length);
            if (count == physicalHits.Length) allowed = 0;
            for (int i = 0; i < count; i++)
            {
                var hit = physicalHits[i]; physicalHits[i] = default;
                if (hit.collider == null || hit.collider.transform.IsChildOf(controller.transform) || !BattleUtils.IsWall(hit.collider)
                    || Vector2.Dot(hit.normal, direction) >= -.001f) continue;
                allowed = Mathf.Min(allowed, Mathf.Max(0, hit.distance - BodySkin));
            }
            // Map-only obstacles and scene limits also apply, even without a physics wall.
            for (float step = Mathf.Min(.1f, allowed); step <= allowed && step > 0; step = Mathf.Min(step + .1f, allowed))
            {
                if (!BodyClear(scope.Host, scope.Binding, from + direction * step, controller.transform)) { allowed = Mathf.Max(0, step - .1f); break; }
                if (step == allowed) break;
            }
            if (allowed >= length) return true;
            var clipped = from + direction * allowed;
            // A stopped airborne native tween has no gravity owner. Return to the last
            // checked ground point instead of leaving a frozen body over a gap.
            controller.transform.position = GroundedBodyPosition(scope, clipped, out ground) ? ground : scope.PhysicalGroundPosition;
            return false;
        }
        private bool GroundedBodyPosition(InstanceScope scope, Vector2 position, out Vector2 ground)
        {
            var map = scope.Host.MonsterEnv.GroundMap;
            ground = NativeLanding(scope.Host, scope.Binding, position);
            // Ground cells have area. An airborne point in their upper part is not a
            // safe rest position: native Run will snap it down again on room reentry.
            return map.IsGround(map.WorldToCell(ground)) && Vector2.Distance(ground, position) <= BodySkin
                && BodyClear(scope.Host, scope.Binding, ground, scope.Controller!.transform);
        }
        private void SettleCancelledGroundAction(InstanceScope scope, ControlledAction action)
        {
            if (action.Attack is MonsterAttackBehaviourAmoebaJumpAttack jump)
            {
                // Invoke does not reset this native accumulator. A cancelled windup
                // must not shorten the next controlled attack's ready duration.
                NativeAccess.Set(jump, "_readyElapsed", 0f);
                NativeAccess.Set(jump, "_isReady", false);
                NativeAccess.Set(jump, "_valid", false);
            }
            var controller = scope.Controller;
            if (!scope.Active || controller == null || scope.Logical == null || scope.Logical.IsDead || ReadHealth(scope.Logical) <= 0) return;
            if (!(action.Attack is MonsterAttackBehaviourAmoebaJumpAttack) &&
                !(action.Moving && controller.GetComponent<MonsterMoverGroundRendererAmoeba>() != null)) return;
            // These native manual tweens have no gravity owner after cancellation. Retire
            // them first, then settle without invoking native landing damage/sound callbacks.
            var map = scope.Host.MonsterEnv.GroundMap;
            bool Safe(Vector2 point) => map.IsGround(map.WorldToCell(point)) &&
                BodyClear(scope.Host, scope.Binding, point, controller.transform, checkUnscaled: true);
            Vector2 ground;
            if (!GroundedBodyPosition(scope, controller.position2d, out ground) || !Safe(ground))
            {
                ground = NativeLanding(scope.Host, scope.Binding, HasPhysicalBody(scope) ? scope.PhysicalGroundPosition : action.StartPosition);
                if (!Safe(ground)) ground = NativeLanding(scope.Host, scope.Binding, action.StartPosition);
                if (!Safe(ground)) throw new InvalidOperationException("CancelledGroundActionHasNoSafeLanding");
            }
            var position = controller.transform.position;
            controller.transform.position = new Vector3(ground.x, ground.y, position.z);
            scope.PhysicalGroundPosition = ground;
            action.Position = ground;
        }
        private static void PhysicalPathFound(MonsterMover __instance, Vector2Int[] path, ref bool __result)
        {
            var scope = PhysicalScope(__instance); if (!__result || scope == null) return;
            var map = scope.Host.MonsterEnv.GetMap(scope.Binding.Proto.GenType);
            foreach (var cell in path)
                if (!current!.BodyClear(scope.Host, scope.Binding, map.CellToWorldPivot(cell, new Vector2(.5f, 0)), scope.Controller!.transform)) { __result = false; return; }
        }
        private static void PhysicalAttackLanding(MonsterAttackBehaviourAmoebaJumpAttack __instance, Vector2 result, ref bool __result)
        {
            var scope = PhysicalScope(__instance);
            if (!__result || scope == null || current!.BodyClear(scope.Host, scope.Binding, result, scope.Controller!.transform)) return;
            FinishPhysicalJump(__instance);
            __result = false;
        }
        private static void FinishPhysicalJump(MonsterAttackBehaviourAmoebaJumpAttack attack)
        { PhysicalJumpStop.Invoke(attack, null); PhysicalJumpEnd.Invoke(attack, null); }
        private static void PhysicalMoveBegin(MonsterMoverGround __instance, out Vector2 __state)
        { __state = PhysicalScope(__instance)?.Controller?.position2d ?? default; }
        private static void PhysicalMoveEnd(MonsterMoverGround __instance, Vector2 __state, ref bool __result)
        {
            var scope = PhysicalScope(__instance); if (scope == null || current!.GuardBodyStep(scope, __state)) return;
            var jump = scope.JumpHelper;
            if (jump != null)
            {
                if (NativeAccess.Get(jump, "_tween") is Tween tween) CancelTween(tween);
                NativeAccess.Set(jump, "_tween", null); NativeAccess.Set(jump, "_isReady", false); NativeAccess.Set(jump, "_isOver", true);
            }
            // Let native Move complete its own callback exactly once.
            __result = true;
        }
        private static void PhysicalAttackBegin(MonsterAttackBehaviourAmoebaJumpAttack __instance, out Vector2 __state)
        { __state = PhysicalScope(__instance)?.Controller?.position2d ?? default; }
        private static void NormalizeBodyFacing(MonsterController controller)
        { if (Mathf.Abs(controller.transform.localScale.x) < .001f) controller.transform.localScale = Vector3.one; }
        private static void PhysicalAttackInvoked(MonsterAttackBehaviourAmoebaJumpAttack __instance)
        { var scope = PhysicalScope(__instance); if (scope?.Controller != null) NormalizeBodyFacing(scope.Controller); }
        private static void PhysicalAttackEnd(MonsterAttackBehaviourAmoebaJumpAttack __instance, Vector2 __state, ref bool __result)
        {
            var scope = PhysicalScope(__instance); if (scope == null || current!.GuardBodyStep(scope, __state)) return;
            // This native stop retires its damage box and manual tween, and plays landing.
            FinishPhysicalJump(__instance);
            __result = true;
        }
    }
}
