using System;
using System.Linq;
using System.Runtime.CompilerServices;
using DG.Tweening;
using DolocTown;
using DolocTown.GameData;
using DTMAPI.MonsterContent;
using HarmonyLib;
using RedSaw;
using UnityEngine;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private sealed class TrainingTargetLease
        {
            internal readonly NativeModule Module;
            internal readonly InstanceScope Scope;
            internal readonly MonsterHandle Handle;
            internal readonly MonsterController Controller;
            internal readonly MonsterDecoratorTarget Decorator;
            internal readonly Target Target;
            internal readonly Vector3 LocalPosition;
            internal long Revision;
            internal TrainingTargetLease(NativeModule module, InstanceScope scope, MonsterDecoratorTarget decorator, Target target)
            { Module = module; Scope = scope; Handle = scope.Handle; Controller = scope.Controller!; Decorator = decorator; Target = target; LocalPosition = target.transform.localPosition; }
            internal bool Valid => ReferenceEquals(current, Module) && ReferenceEquals(Scope.TrainingTarget, this)
                && Scope.Active && !Scope.PendingRemoveLogic && Scope.Handle.Equals(Handle)
                && ReferenceEquals(Scope.Controller, Controller) && ReferenceEquals(Controller.Monster, Scope.Logical)
                && Scope.Logical?.IsDead == false && Scope.Binding.Definition.State == MonsterDefinitionState.Creatable;
        }
        // Retain a weak tombstone after pooling: direct calls on a retired child must
        // not fall through to the unowned path. A new Run installs a fresh lease.
        private readonly ConditionalWeakTable<Target, TrainingTargetLease> trainingTargets = new ConditionalWeakTable<Target, TrainingTargetLease>();
        private static bool IsTrainingDecorator(MonsterDecorator decorator) => decorator.GetType() == typeof(MonsterDecoratorTarget);
        private void ValidateTraining()
        {
            foreach (var template in templates.Values.Where(t => t.Proto.Name == "target_01" || t.Proto.Name == "scarecrow"))
            {
                bool target = template.Proto.Name == "target_01";
                var children = template.Prefab?.GetComponentsInChildren<Target>(true) ?? Array.Empty<Target>();
                bool valid = template.Proto.AttackBehaviourProtos.Length == 0 && template.Ai == (target ? typeof(MonsterAI_Target) : typeof(MonsterAI_Empty))
                    && template.Proto.MoverProto?.GetType() == (target ? typeof(MonsterMoverProtoDoTween) : typeof(MonsterMoverProtoNone))
                    && template.Prefab?.GetComponent<MonsterDecorator>()?.GetType() == (target ? typeof(MonsterDecoratorTarget) : typeof(MonsterDecorator));
                if (target) valid &= children.Length == 1 && children[0].GetComponent<Collider2D>() != null
                    && children[0].GetComponent<Rigidbody2D>() != null && children[0].GetComponent<SpriteRenderer>() != null;
                if (!valid) template.Errors.Add(new MonsterDiagnostic("ResourceMissing", "Training template requires its exact native AI, mover, decorator, empty attack list and target child resources.",
                    templateId: template.Proto.Name, stage: "Template", field: "decorator.training"));
            }
        }
        private void InstallTrainingHooks()
        {
            Patch(AccessTools.DeclaredMethod(typeof(MonsterDecoratorTarget), "_ResetTarget"), prefix: nameof(TrainingResetting), postfix: nameof(TrainingReset));
            Patch(AccessTools.DeclaredMethod(typeof(MonsterDecoratorTarget), "OnUpdate"), prefix: nameof(TrainingUpdating));
            Patch(AccessTools.DeclaredMethod(typeof(Target), "CallbackOnBroken"), prefix: nameof(TrainingBrokenAssigned));
            Patch(AccessTools.DeclaredMethod(typeof(Target), "OnAttacked"), prefix: nameof(TrainingAttacking), finalizer: nameof(TrainingAttacked));
            Patch(AccessTools.DeclaredMethod(typeof(MonsterMoverDoTween), "TryMoveTo"), prefix: nameof(TrainingMoving), postfix: nameof(TrainingMoved));
        }
        private TrainingTargetLease BindTraining(InstanceScope scope, MonsterDecoratorTarget decorator)
        {
            if (scope.TrainingTarget != null) return scope.TrainingTarget;
            var target = decorator.GetComponentInChildren<Target>(true);
            var lease = new TrainingTargetLease(this, scope, decorator, target);
            trainingTargets.Remove(target); trainingTargets.Add(target, lease); scope.TrainingTarget = lease;
            return lease;
        }
        private static bool TrainingResetting(MonsterDecoratorTarget __instance)
        {
            var module = current;
            if (module == null || !module.controllers.TryGetValue(__instance.GetComponent<MonsterController>(), out var scope)) return true;
            var lease = module.BindTraining(scope, __instance);
            if (!lease.Valid) return false;
            lease.Revision++;
            StopTrainingVisuals(lease);
            NativeAccess.Get<RSTimer>(__instance, "_timer").Reset();
            return true;
        }
        private static void TrainingReset(MonsterDecoratorTarget __instance)
        {
            if (current != null && current.controllers.TryGetValue(__instance.GetComponent<MonsterController>(), out var scope) && scope.TrainingTarget?.Valid == true)
            {
                // The tiny native registration method may already be inlined in
                // this caller. Own the actual stored delegate after reset returns.
                var lease = scope.TrainingTarget;
                NativeAccess.Set(lease.Target, "_callbackOnBroken", TrainingBrokenCallback(lease, NativeAccess.Get<Action>(lease.Target, "_callbackOnBroken")));
                current.Fault("TrainingLoaded");
            }
        }
        private static bool TrainingUpdating(MonsterDecoratorTarget __instance)
        {
            var target = __instance.GetComponentInChildren<Target>(true);
            return current == null || target == null || !current.trainingTargets.TryGetValue(target, out var lease) || lease.Valid;
        }
        private static void TrainingBrokenAssigned(Target __instance, ref Action callback)
        {
            if (current == null || !current.trainingTargets.TryGetValue(__instance, out var lease)) return;
            callback = TrainingBrokenCallback(lease, callback);
        }
        private static Action TrainingBrokenCallback(TrainingTargetLease lease, Action original)
        {
            var revision = lease.Revision; bool called = false;
            return () =>
            {
                if (called || !lease.Valid || lease.Revision != revision) return;
                called = true;
                NativeAccess.Get<RSTimer>(lease.Decorator, "_timer").Reset();
                original?.Invoke();
            };
        }
        private static bool TrainingAttacking(Target __instance, ref bool __result, ref bool isDead, out TrainingTargetLease? __state)
        {
            __state = null;
            if (current == null || !current.trainingTargets.TryGetValue(__instance, out var lease)) return true;
            if (!lease.Valid || !__instance.gameObject.activeInHierarchy || !NativeAccess.Get<bool>(lease.Decorator, "_hasTargetLoaded") || NativeAccess.Get<int>(__instance, "_health") <= 0)
            { __result = false; isDead = false; return false; }
            __state = lease; lease.Scope.NativeCallDepth++;
            return true;
        }
        private static Exception? TrainingAttacked(TrainingTargetLease? __state, Exception? __exception)
        {
            if (__state == null) return __exception;
            var shaker = NativeAccess.Get(__state.Target, "_shaker");
            if (__state.Valid && shaker != null && NativeAccess.Get(shaker, "tween") is Tween tween)
            {
                var callback = tween.onComplete; var revision = __state.Revision;
                tween.onComplete = () => { if (__state.Valid && __state.Revision == revision) callback?.Invoke(); };
            }
            // Broken callbacks/events may withdraw the owner before OnAttacked's
            // remaining effects and broadcast have returned.
            return FinishTrafficCall(__state.Scope, __exception);
        }
        private static void StopTrainingVisuals(TrainingTargetLease lease)
        {
            var target = lease.Target;
            var shaker = NativeAccess.Get(target, "_shaker");
            if (shaker != null) CancelTweens(shaker);
            target.transform.localPosition = lease.LocalPosition;
            var shiner = NativeAccess.Get(target, "_shiner");
            if (shiner != null && NativeAccess.Get(shiner, "coroutine") is Coroutine coroutine)
            {
                DolocAPI.StopCoroutine(coroutine); NativeAccess.Set(shiner, "coroutine", null);
                var renderer = target.GetComponent<SpriteRenderer>();
                renderer.sharedMaterial = NativeAccess.Get<Material>(shiner, "originalMat"); renderer.color = NativeAccess.Get<Color>(shiner, "originColor");
            }
        }
        private void CancelTraining(InstanceScope scope)
        {
            if (scope.Controller?.Decorator is not MonsterDecoratorTarget decorator) return;
            var lease = BindTraining(scope, decorator); lease.Revision++; scope.TrainingMoveRevision++;
            NativeAccess.Set(lease.Target, "_callbackOnBroken", null);
            NativeAccess.Set(decorator, "_hasTargetLoaded", false);
            NativeAccess.Get<RSTimer>(decorator, "_timer").Reset();
            lease.Target.gameObject.SetActive(false);
            StopTrainingVisuals(lease);
            Fault("TrainingCleanup");
        }
        private void RestoreUnownedTraining(MonsterController controller, Monster monster)
        {
            if (bindings.ContainsKey(monster.Name)) return;
            var target = controller.GetComponentInChildren<Target>(true);
            if (target != null) trainingTargets.Remove(target);
        }
        private static bool TrainingMoving(MonsterMoverDoTween __instance, ref Action callback, ref bool __result, out InstanceScope? __state)
        {
            __state = null;
            var module = current;
            var controller = NativeAccess.Get<MonsterController>(__instance, "_controller");
            if (module == null || !module.controllers.TryGetValue(controller, out var scope) || scope.Binding.Template.Ai != typeof(MonsterAI_Target)) return true;
            if (!scope.Active || scope.PendingRemoveLogic || !ReferenceEquals(controller.mover, __instance)) { __result = false; return false; }
            __state = scope; var handle = scope.Handle; var revision = ++scope.TrainingMoveRevision; var original = callback; bool called = false;
            CancelTweens(__instance);
            callback = () =>
            {
                if (called || !TrainingMoveValid(module, scope, __instance, handle, revision)) return;
                called = true; original?.Invoke();
            };
            return true;
        }
        private static bool TrainingMoveValid(NativeModule module, InstanceScope scope, MonsterMoverDoTween mover, MonsterHandle handle, long revision) =>
            ReferenceEquals(current, module) && scope.Active && !scope.PendingRemoveLogic && scope.Handle.Equals(handle)
            && scope.TrainingMoveRevision == revision && ReferenceEquals(scope.Controller?.mover, mover);
        private static void TrainingMoved(MonsterMoverDoTween __instance, InstanceScope? __state)
        {
            var module = current;
            if (module == null || __state == null || !__state.Active || NativeAccess.Get(__instance, "_tween") is not Tween tween) return;
            var handle = __state.Handle; var revision = __state.TrainingMoveRevision;
            var update = tween.onUpdate; var complete = tween.onComplete;
            tween.onUpdate = () => { if (TrainingMoveValid(module, __state, __instance, handle, revision)) update?.Invoke(); };
            tween.onComplete = () => { if (TrainingMoveValid(module, __state, __instance, handle, revision)) complete?.Invoke(); };
        }
    }
}
