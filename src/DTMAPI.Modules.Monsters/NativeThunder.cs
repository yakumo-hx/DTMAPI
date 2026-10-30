using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using DG.Tweening;
using DolocTown;
using DTMAPI.MonsterContent;
using HarmonyLib;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private sealed class NativeThunderSource
        {
            internal readonly InstanceScope Scope;
            internal readonly MonsterHandle Instance;
            internal MonsterActionHandle? Action;
            internal bool Executing;
            internal NativeThunderSource(InstanceScope scope) { Scope = scope; Instance = scope.Handle; }
            internal bool Valid => Scope.Active && Scope.Handle.Equals(Instance);
        }
        private sealed class RetiredThunder
        {
            internal readonly bool ColliderEnabled;
            internal RetiredThunder(ThunderPile skill) { ColliderEnabled = skill.Collider.enabled; }
        }
        private readonly ConditionalWeakTable<MonsterAttackBehaviourSkillThunderPile, NativeThunderSource> nativeThunderSources = new ConditionalWeakTable<MonsterAttackBehaviourSkillThunderPile, NativeThunderSource>();
        private readonly ConditionalWeakTable<ThunderPile, RetiredThunder> retiredNativeThunder = new ConditionalWeakTable<ThunderPile, RetiredThunder>();
        private readonly ConditionalWeakTable<ThunderPile, Tween> nativeThunderFades = new ConditionalWeakTable<ThunderPile, Tween>();
        private void InstallNativeThunderHooks()
        {
            Patch(AccessTools.DeclaredMethod(typeof(MonsterAttackBehaviourSkillThunderPile), "Invoke"), prefix: nameof(NativeThunderInvoking), finalizer: nameof(NativeThunderReturned));
            Patch(AccessTools.DeclaredMethod(typeof(MonsterAttackBehaviourSkillThunderPile), "OnUpdate"), prefix: nameof(NativeThunderUpdating), finalizer: nameof(NativeThunderReturned));
            foreach (var name in new[] { "RaiseThunderPile", "DropThunderPile" })
                Patch(AccessTools.DeclaredMethod(typeof(MonsterAttackBehaviourSkillThunderPile), name), prefix: nameof(NativeThunderContinuing), finalizer: nameof(NativeThunderReturned));
            Patch(AccessTools.DeclaredMethod(typeof(ThunderPile), "OnFixedUpdate"), prefix: nameof(NativeThunderAdvancing), finalizer: nameof(NativeThunderAdvanced));
            Patch(AccessTools.DeclaredMethod(typeof(ThunderPile), "OnTriggerEnter2D"), prefix: nameof(NativeThunderHitting));
        }
        private static bool EnterNativeThunder(MonsterAttackBehaviourSkillThunderPile attack, bool start, out NativeSkillInvocation? state)
        {
            state = null; var module = current;
            if (module == null) return true;
            InstanceScope? scope = null;
            if (module.nativeThunderSources.TryGetValue(attack, out var source))
            {
                if (!source.Valid || !start && (!source.Executing || NativeAccess.Get(NativeAccess.Get(attack, "_handle")!, "_tween") == null)) return false;
                scope = source.Scope;
                if (start) { source.Executing = true; source.Action = scope.Action?.Handle; }
                scope.NativeSkillDepth++;
            }
            state = new NativeSkillInvocation(module, scope); module.raisingNativeSkill = state;
            return true;
        }
        private static bool NativeThunderInvoking(MonsterAttackBehaviourSkillThunderPile __instance, out NativeSkillInvocation? __state)
            => EnterNativeThunder(__instance, true, out __state);
        private static bool NativeThunderContinuing(MonsterAttackBehaviourSkillThunderPile __instance, out NativeSkillInvocation? __state)
            => EnterNativeThunder(__instance, false, out __state);
        private static bool NativeThunderUpdating(MonsterAttackBehaviourSkillThunderPile __instance, ref bool __result, out NativeSkillInvocation? __state)
        { if (EnterNativeThunder(__instance, false, out __state)) return true; __result = true; return false; }
        private static Exception? NativeThunderReturned(MonsterAttackBehaviourSkillThunderPile __instance, NativeSkillInvocation? __state, Exception? __exception)
        {
            if (__state == null) return __exception;
            var module = __state.Module; module.raisingNativeSkill = __state.Previous;
            var scope = __state.Scope;
            if (scope == null) return __exception;
            scope.NativeSkillDepth--;
            var errors = new List<Exception>();
            if (__exception != null || __state.Cancelled || !scope.Active)
            {
                Attempt(errors, () => CancelTweens(__instance));
                Attempt(errors, () => module.CancelNativeSkills(scope, __state.Action));
            }
            if (NativeAccess.Get(NativeAccess.Get(__instance, "_handle")!, "_tween") == null
                && module.nativeThunderSources.TryGetValue(__instance, out var source)) source.Executing = false;
            foreach (var owner in module.nativeSkillManagers.Values.Where(o => o.Leases.Any(l => ReferenceEquals(l.Scope, scope))).ToArray())
                Attempt(errors, () => module.DrainNativeSkillManager(owner));
            if (scope.PendingRemoveLogic) Attempt(errors, () => module.Revoke(scope, true));
            if (__exception != null || errors.Count != 0)
                module.FailAction(scope, "NativeThunder", new AggregateException("ThunderPile creation/update cleanup failed", __exception == null ? errors : new[] { __exception }.Concat(errors)));
            return null;
        }
        private void CancelNativeThunder(InstanceScope scope, MonsterActionHandle? action)
        {
            foreach (var attack in scope.AcquiredAttacks.OfType<MonsterAttackBehaviourSkillThunderPile>())
                if (nativeThunderSources.TryGetValue(attack, out var source) && (action == null || source.Action.HasValue && source.Action.Value.Equals(action.Value)))
                { source.Executing = false; CancelTweens(attack); }
        }
        private void RetireNativeThunder(Skill skill)
        {
            if (!(skill is ThunderPile thunder)) return;
            retiredNativeThunder.GetValue(thunder, s => new RetiredThunder(s));
            nativeThunderFades.Remove(thunder);
            thunder.Collider.enabled = false; CancelTweens(thunder);
        }
        private void ReuseNativeThunder(Skill skill)
        {
            if (!(skill is ThunderPile thunder) || !retiredNativeThunder.TryGetValue(thunder, out var retired)) return;
            thunder.Collider.enabled = retired.ColliderEnabled; retiredNativeThunder.Remove(thunder);
        }
        private static bool NativeThunderAdvancing(ThunderPile __instance, ref bool __result, out NativeSkillTick __state)
        {
            if (current != null && current.retiredNativeThunder.TryGetValue(__instance, out _))
            { __state = new NativeSkillTick(current.advancingNativeSkill, null); __result = true; return false; }
            return NativeSkillAdvancing(__instance, ref __result, out __state);
        }
        private static bool NativeThunderHitting(ThunderPile __instance)
        {
            var module = current;
            return module == null || !module.retiredNativeThunder.TryGetValue(__instance, out _)
                && (!module.nativeSkills.TryGetValue(__instance, out var lease) || lease.Valid);
        }
        private static Exception? NativeThunderAdvanced(ThunderPile __instance, NativeSkillTick __state, Exception? __exception)
        {
            var module = current; var lease = __state.Lease;
            if (__exception == null && module != null && lease?.Valid == true && NativeAccess.Get(__instance, "fadeTween") is Tween tween
                && (!module.nativeThunderFades.TryGetValue(__instance, out var previous) || !ReferenceEquals(previous, tween)))
            {
                module.nativeThunderFades.Remove(__instance); module.nativeThunderFades.Add(__instance, tween);
                var done = tween.onComplete;
                tween.onComplete = () => { if (lease.Valid && module.nativeSkills.TryGetValue(__instance, out var active) && ReferenceEquals(active, lease)) done?.Invoke(); };
            }
            return NativeSkillAdvanced(__state, __exception);
        }
    }
}
