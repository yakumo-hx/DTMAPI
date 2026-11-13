using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using DolocTown;
using DTMAPI.MonsterContent;
using HarmonyLib;
using UnityEngine;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private sealed class NativeMimicryBody
        {
            internal readonly Collider2D Collider;
            internal readonly bool Enabled;
            internal long AnimationRevision;
            internal NativeMimicryBody(Collider2D collider) { Collider = collider; Enabled = collider.enabled; }
        }
        private sealed class NativeMimicrySource
        {
            internal readonly InstanceScope Scope;
            internal readonly MonsterHandle Instance;
            internal MonsterActionHandle? Action;
            internal bool Executing;
            internal long UpdateRevision;
            internal NativeMimicrySource(InstanceScope scope) { Scope = scope; Instance = scope.Handle; }
            internal bool Valid => Scope.Active && Scope.Handle.Equals(Instance);
        }
        private sealed class RetiredMimicry
        {
            internal readonly bool ColliderEnabled;
            internal RetiredMimicry(ChomperMimicry skill) { ColliderEnabled = skill.Collider.enabled; }
        }
        private readonly ConditionalWeakTable<MonsterAttackBehaviourSkillChomperMimicry, NativeMimicrySource> nativeMimicrySources = new ConditionalWeakTable<MonsterAttackBehaviourSkillChomperMimicry, NativeMimicrySource>();
        private readonly ConditionalWeakTable<ChomperMimicry, RetiredMimicry> retiredNativeMimicries = new ConditionalWeakTable<ChomperMimicry, RetiredMimicry>();

        private static bool IsNativeSelfAttack(MonsterAttackBehaviour attack) => attack is MonsterAttackBehaviourSkillChomperMimicry;
        private void BindNativeMimicry(InstanceScope scope, MonsterAttackBehaviourSkillChomperMimicry attack)
        {
            if (scope.MimicryBody == null) scope.MimicryBody = new NativeMimicryBody(scope.Controller!.Collider);
            nativeMimicrySources.Add(attack, new NativeMimicrySource(scope));
        }
        private void InstallNativeMimicryHooks()
        {
            Patch(AccessTools.DeclaredMethod(typeof(MonsterAttackBehaviourSkillChomperMimicry), "Invoke"), prefix: nameof(NativeMimicryInvoking), finalizer: nameof(NativeMimicryReturned));
            Patch(AccessTools.DeclaredMethod(typeof(MonsterAttackBehaviourSkillChomperMimicry), "OnUpdate"), prefix: nameof(NativeMimicryUpdating), finalizer: nameof(NativeMimicryReturned));
            Patch(AccessTools.DeclaredMethod(typeof(MonsterAttackBehaviourSkillChomperMimicry), "Stop"), postfix: nameof(NativeMimicryCompleted));
            Patch(AccessTools.DeclaredMethod(typeof(MonsterAI_Chomper), "OnFixedUpdate"), prefix: nameof(NativeMimicryAiEntering), postfix: nameof(NativeMimicryAiUpdated));
            Patch(AccessTools.DeclaredMethod(typeof(MonsterRenderer), "PlayAnimation"), prefix: nameof(NativeMimicryAnimation));
            Patch(AccessTools.DeclaredMethod(typeof(ChomperMimicry), "OnFixedUpdate"), prefix: nameof(NativeMimicryAdvancing), finalizer: nameof(NativeSkillAdvanced));
            Patch(AccessTools.DeclaredMethod(typeof(ChomperMimicry), "OnFell"), prefix: nameof(NativeMimicryFelling));
        }
        private static NativeMimicrySource? MimicryAiSource(MonsterAI_Chomper ai)
        {
            if (current == null || !(NativeAccess.Get(ai, "_controller") is MonsterController controller)
                || !current.controllers.TryGetValue(controller, out var scope) || !scope.Active) return null;
            var attack = scope.AcquiredAttacks.OfType<MonsterAttackBehaviourSkillChomperMimicry>().FirstOrDefault();
            return attack != null && current.nativeMimicrySources.TryGetValue(attack, out var source) ? source : null;
        }
        private static void NativeMimicryAiEntering(MonsterAI_Chomper __instance, out long __state)
            => __state = MimicryAiSource(__instance)?.UpdateRevision ?? -1;
        private static void NativeMimicryAiUpdated(MonsterAI_Chomper __instance, float dt, long __state)
        {
            var source = MimicryAiSource(__instance);
            if (source == null || source.UpdateRevision != __state || dt <= 0
                || DolocAPI.gameLoop.IsPaused || DolocAPI.gameLoop.IsGlobalPaused) return;
            // Wander advances its shuttle only. An attack state may already have advanced the slot.
            var manager = source.Scope.Controller!.attackBehaviourManager;
            try
            {
                if (NativeAccess.Get(manager, "_currentBehaviour") is MonsterAttackBehaviourSkillChomperMimicry) manager.Update(dt);
                else if (source.Executing) current!.CancelNativeSkills(source.Scope, source.Action);
            }
            catch (Exception ex) { current!.FailAction(source.Scope, "NativeMimicry.AiUpdate", ex); }
        }
        private static void NativeMimicryAnimation(MonsterRenderer __instance, ref Action? callback)
        {
            var module = current;
            var controller = __instance.GetComponent<MonsterController>();
            if (module == null || controller == null || !module.controllers.TryGetValue(controller, out var scope) || scope.MimicryBody == null) return;
            var body = scope.MimicryBody; var instance = scope.Handle; long revision = ++body.AnimationRevision;
            if (callback == null) return;
            var original = callback;
            callback = () => { if (scope.Active && scope.Handle.Equals(instance) && body.AnimationRevision == revision) original(); };
        }
        private static bool EnterNativeMimicry(MonsterAttackBehaviourSkillChomperMimicry attack, bool start, out NativeSkillInvocation? state)
        {
            state = null;
            var module = current;
            if (module == null) return true;
            InstanceScope? scope = null;
            if (module.nativeMimicrySources.TryGetValue(attack, out var source))
            {
                if (!source.Valid || !start && !source.Executing) return false;
                scope = source.Scope;
                if (start) { source.Executing = true; source.Action = scope.Action?.Handle; }
                else source.UpdateRevision++;
                scope.NativeSkillDepth++;
            }
            state = new NativeSkillInvocation(module, scope);
            module.raisingNativeSkill = state;
            return true;
        }
        private static bool NativeMimicryInvoking(MonsterAttackBehaviourSkillChomperMimicry __instance, out NativeSkillInvocation? __state)
            => EnterNativeMimicry(__instance, true, out __state);
        private static bool NativeMimicryUpdating(MonsterAttackBehaviourSkillChomperMimicry __instance, ref bool __result, out NativeSkillInvocation? __state)
        { if (EnterNativeMimicry(__instance, false, out __state)) return true; __result = true; return false; }
        private static void NativeMimicryCompleted(MonsterAttackBehaviourSkillChomperMimicry __instance)
        { if (current != null && current.nativeMimicrySources.TryGetValue(__instance, out var source)) source.Executing = false; }
        private static Exception? NativeMimicryReturned(MonsterAttackBehaviourSkillChomperMimicry __instance, NativeSkillInvocation? __state, Exception? __exception)
        {
            if (__state == null) return __exception;
            var module = __state.Module;
            module.raisingNativeSkill = __state.Previous;
            var scope = __state.Scope;
            if (scope == null) return __exception;
            scope.NativeSkillDepth--;
            var errors = new List<Exception>();
            if (__exception != null || __state.Cancelled || !scope.Active)
            {
                // Invoke can install its sequence after Raise has already requested withdrawal.
                Attempt(errors, () => CancelTweens(__instance));
                Attempt(errors, () => module.CancelNativeSkills(scope, __state.Action));
                Attempt(errors, () => RestoreNativeMimicryBody(scope));
            }
            foreach (var owner in module.nativeSkillManagers.Values.Where(o => o.Leases.Any(l => ReferenceEquals(l.Scope, scope))).ToArray())
                Attempt(errors, () => module.DrainNativeSkillManager(owner));
            if (scope.PendingRemoveLogic) Attempt(errors, () => module.Revoke(scope, true));
            if (__exception != null || errors.Count != 0)
                module.FailAction(scope, "NativeMimicry", new AggregateException("Native Mimicry creation/update cleanup failed", __exception == null ? errors : new[] { __exception }.Concat(errors)));
            return null;
        }
        private static void RestoreNativeMimicryBody(InstanceScope scope)
        {
            var body = scope.MimicryBody; var controller = scope.Controller;
            if (body == null || controller == null) return;
            body.AnimationRevision++;
            NativeAccess.Set(controller.Renderer, "onAnimationDone", null);
            NativeAccess.Set(controller.Renderer, "shouldCheckAnimationDone", false);
            body.Collider.enabled = body.Enabled;
            if (scope.Active) controller.Renderer.PlayAnimation("idle");
        }
        private void CancelNativeMimicry(InstanceScope scope, MonsterActionHandle? action)
        {
            bool restore = !scope.Active;
            foreach (var attack in scope.AcquiredAttacks.OfType<MonsterAttackBehaviourSkillChomperMimicry>())
                if (nativeMimicrySources.TryGetValue(attack, out var source) && source.Executing
                    && (action == null || source.Action.HasValue && source.Action.Value.Equals(action.Value)))
                { source.Executing = false; CancelTweens(attack); restore = true; }
            if (restore) RestoreNativeMimicryBody(scope);
        }
        private void RetireNativeMimicry(Skill skill)
        {
            if (!(skill is ChomperMimicry mimicry)) return;
            retiredNativeMimicries.GetValue(mimicry, s => new RetiredMimicry(s));
            mimicry.Collider.enabled = false;
        }
        private void ReuseNativeMimicry(Skill skill)
        {
            if (!(skill is ChomperMimicry mimicry) || !retiredNativeMimicries.TryGetValue(mimicry, out var retired)) return;
            mimicry.Collider.enabled = retired.ColliderEnabled;
            retiredNativeMimicries.Remove(mimicry);
        }
        private static bool NativeMimicryAdvancing(ChomperMimicry __instance, ref bool __result, out NativeSkillTick __state)
        {
            if (current != null && current.retiredNativeMimicries.TryGetValue(__instance, out _))
            { __state = new NativeSkillTick(current.advancingNativeSkill, null); __result = true; return false; }
            return NativeSkillAdvancing(__instance, ref __result, out __state);
        }
        private static bool NativeMimicryFelling(ChomperMimicry __instance, ref bool __result)
        {
            var module = current;
            if (module == null || !module.retiredNativeMimicries.TryGetValue(__instance, out _)
                && (!module.nativeSkills.TryGetValue(__instance, out var lease) || lease.Valid)) return true;
            __result = false; return false;
        }
    }
}
