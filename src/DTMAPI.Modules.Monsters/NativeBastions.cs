using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using DG.Tweening;
using DolocTown;
using DolocTown.Config.Monster;
using DolocTown.GameData;
using DTMAPI.MonsterContent;
using HarmonyLib;
using UnityEngine;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private sealed class BastionExecution
        {
            internal readonly NativeModule Module;
            internal readonly InstanceScope Scope;
            internal readonly MonsterHandle Handle;
            internal readonly MonsterController Controller;
            internal readonly HashSet<object> Idle = new HashSet<object>();
            internal readonly HashSet<Tween> Tweens = new HashSet<Tween>();
            internal long Revision;
            internal BastionExecution(NativeModule module, InstanceScope scope)
            { Module = module; Scope = scope; Handle = scope.Handle; Controller = scope.Controller!; }
            internal bool Valid => ReferenceEquals(current, Module) && ReferenceEquals(Scope.Bastion, this)
                && Scope.Active && !Scope.PendingRemoveLogic && Scope.Handle.Equals(Handle)
                && ReferenceEquals(Scope.Controller, Controller) && ReferenceEquals(Controller.Monster, Scope.Logical)
                && Scope.Logical?.IsDead == false && Scope.Binding.Definition.State == MonsterDefinitionState.Creatable;
        }
        private static bool IsBastionDecorator(MonsterDecorator decorator) => decorator.GetType() == typeof(MonsterDecoratorBastion);
        private void ValidateBastions()
        {
            foreach (var template in templates.Values.Where(t => t.Ai == typeof(MonsterAI_Bastion) || t.Prefab?.GetComponent<MonsterDecoratorBastion>() != null))
                if (template.Ai != typeof(MonsterAI_Bastion) || template.Proto.MoverProto?.GetType() != typeof(MonsterMoverProtoFixed)
                    || template.Prefab?.GetComponent<MonsterDecorator>()?.GetType() != typeof(MonsterDecoratorBastion)
                    || template.Prefab.GetComponent<Animator>()?.runtimeAnimatorController == null
                    || template.Prefab.GetComponentInChildren<MonsterSight>(true) == null
                    || template.Prefab.GetComponent<AttackBehaviourRendererBastion>() == null
                    || template.Prefab.GetComponentInChildren<LineRenderer>(true) == null)
                    template.Errors.Add(new MonsterDiagnostic("ResourceMissing", "Bastion requires the exact native Fixed body, AI, decorator, animator, sight and aiming renderer.",
                        templateId: template.Proto.Name, stage: "Template", field: "decorator.bastion"));
        }
        private static BastionExecution? BastionFor(MonsterController controller)
        {
            var module = current;
            if (module == null || !module.controllers.TryGetValue(controller, out var scope)
                || scope.Binding.Template.Ai != typeof(MonsterAI_Bastion)) return null;
            return scope.Bastion ??= new BastionExecution(module, scope);
        }
        private MonsterMover? FixedBastionMover(MonsterEnv env, Transform transform, MonsterMoverProto proto, MonsterGenType genType)
        {
            var controller = transform.GetComponent<MonsterController>();
            if (controller == null || !controllers.TryGetValue(controller, out var scope) || !scope.Active
                || scope.Binding.Template.Ai != typeof(MonsterAI_Bastion) || !IsBastionDecorator(controller.Decorator)
                || proto.GetType() != typeof(MonsterMoverProtoFixed) || !ReferenceEquals(proto, scope.Binding.Proto.MoverProto)) return null;
            // Native factory has no Fixed branch. Keep the actual proto/pathfinder;
            // supply its stationary implementation only for this admitted owned body.
            return new MonsterMoverNone(env, transform, proto, genType);
        }
        private void InstallBastionHooks()
        {
            Patch(AccessTools.DeclaredMethod(typeof(MonsterDecoratorBastion), "OnAttackBegin"), prefix: nameof(BastionAttackStarting));
            Patch(AccessTools.DeclaredMethod(typeof(MonsterDecoratorBastion), "OnAttackEnd"), prefix: nameof(BastionEnding), transpiler: nameof(BastionDelayAdapter));
            Patch(AccessTools.DeclaredMethod(typeof(MonsterDecoratorBastion), "Recoil"), postfix: nameof(BastionRecoilCreated));
            Patch(AccessTools.DeclaredMethod(typeof(AttackBehaviourRendererBastion), "DoReadyAction"), postfix: nameof(BastionReadyCreated));
            Patch(AccessTools.DeclaredMethod(typeof(MonsterAttackBehaviourMultiShoot), "MultiShoot"), postfix: nameof(BastionVolleyCreated));
        }
        private static bool BastionAttackStarting(MonsterDecoratorBastion __instance)
        {
            var execution = BastionFor(__instance.GetComponent<MonsterController>());
            if (execution == null) return true;
            if (!execution.Valid) return false;
            CancelBastionAttack(execution);
            return true;
        }
        private static bool BastionEnding(MonsterDecoratorBastion __instance) => BastionFor(__instance.GetComponent<MonsterController>())?.Valid ?? true;
        private static IEnumerable<CodeInstruction> BastionDelayAdapter(IEnumerable<CodeInstruction> codes)
        {
            int count = 0;
            foreach (var code in codes)
            {
                if (code.operand is MethodInfo method && method.Name == "ContinueWith"
                    && method.DeclaringType?.FullName == "Cysharp.Threading.Tasks.UniTaskExtensions"
                    && method.GetParameters().Length == 2 && method.GetParameters()[1].ParameterType == typeof(Action))
                {
                    var invoke = new CodeInstruction(code.opcode, code.operand);
                    // Retain branch/exception annotations on the first inserted instruction.
                    code.opcode = OpCodes.Ldarg_0; code.operand = null;
                    yield return code;
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NativeModule), nameof(BastionIdleCallback)));
                    yield return invoke;
                    count++; continue;
                }
                yield return code;
            }
            if (count != 1) throw new InvalidOperationException("Bastion delayed idle continuation boundary changed.");
        }
        private static Action BastionIdleCallback(Action callback, MonsterDecoratorBastion decorator)
        {
            var execution = BastionFor(decorator.GetComponent<MonsterController>());
            if (execution == null) return callback;
            var ticket = new object(); var revision = execution.Revision;
            if (execution.Valid) execution.Idle.Add(ticket);
            return () =>
            {
                if (!execution.Idle.Remove(ticket) || !execution.Valid || execution.Controller.attackBehaviourManager.IsAttacking) return;
                RunBastionCallback(execution, revision, callback);
            };
        }
        private static void RunBastionCallback(BastionExecution execution, long revision, Action callback)
        {
            if (!execution.Valid || execution.Revision != revision) return;
            var scope = execution.Scope; scope.NativeCallDepth++;
            Exception? error = null;
            try { callback(); } catch (Exception ex) { error = ex; }
            error = FinishTrafficCall(scope, error);
            if (error != null) throw error;
        }
        private static void BastionRecoilCreated(MonsterDecoratorBastion __instance, Tween __result) => OwnBastionTween(__instance.GetComponent<MonsterController>(), __result);
        private static void BastionReadyCreated(AttackBehaviourRendererBastion __instance, Tween __result) => OwnBastionTween(__instance.GetComponent<MonsterController>(), __result);
        private static void BastionVolleyCreated(MonsterAttackBehaviourMultiShoot __instance)
        {
            var execution = BastionFor(NativeAccess.Get<MonsterController>(__instance, "_controller"));
            if (execution == null || NativeAccess.Get(__instance, "_tween") is not Tween tween || tween.onComplete == null) return;
            // MultiShoot installs its recursive completion after Recoil returns.
            var callback = tween.onComplete; var revision = execution.Revision;
            tween.onComplete = () => RunBastionCallback(execution, revision, () => callback());
        }
        private static void OwnBastionTween(MonsterController controller, Tween tween)
        {
            var execution = BastionFor(controller);
            if (execution == null || tween == null) return;
            if (!execution.Valid) { CancelTween(tween); return; }
            execution.Tweens.RemoveWhere(t => !t.IsActive()); execution.Tweens.Add(tween);
            var revision = execution.Revision;
            TweenCallback? Guard(TweenCallback? callback) => callback == null ? null : () => RunBastionCallback(execution, revision, () => callback());
            tween.onUpdate = Guard(tween.onUpdate);
            tween.onComplete = Guard(tween.onComplete);
            tween.onStepComplete = Guard(tween.onStepComplete);
            var onKill = tween.onKill;
            tween.onKill = () =>
            {
                execution.Tweens.Remove(tween);
                if (onKill != null) RunBastionCallback(execution, revision, () => onKill());
            };
        }
        private static void CancelBastionAttack(BastionExecution execution)
        {
            execution.Revision++; execution.Idle.Clear();
            var errors = new List<Exception>();
            foreach (var tween in execution.Tweens.ToArray())
                Attempt(errors, () => { CancelTween(tween); execution.Tweens.Remove(tween); });
            var line = execution.Controller.GetComponentInChildren<LineRenderer>(true);
            if (line != null) line.gameObject.SetActive(false);
            ThrowCleanup(errors);
        }
        private void CancelBastion(InstanceScope scope)
        {
            if (scope.Bastion == null) return;
            CancelBastionAttack(scope.Bastion);
            Fault("BastionCleanup");
        }
    }
}
