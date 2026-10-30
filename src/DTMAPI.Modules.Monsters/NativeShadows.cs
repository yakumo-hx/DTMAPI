using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using DolocTown;
using DolocTown.MonsterAttackBehaviours;
using DTMAPI.MonsterContent;
using HarmonyLib;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private sealed class NativeShadowSource
        {
            internal readonly InstanceScope Scope;
            internal readonly MonsterHandle Instance;
            internal NativeShadowSource(InstanceScope scope) { Scope = scope; Instance = scope.Handle; }
            internal bool Valid => Scope.Active && Scope.Handle.Equals(Instance);
        }
        private readonly ConditionalWeakTable<MonsterAttackBehaviourShadowImpact, NativeShadowSource> nativeShadowSources = new ConditionalWeakTable<MonsterAttackBehaviourShadowImpact, NativeShadowSource>();
        // A recycled object can still have a queued collision. No old scope is retained by this tombstone.
        private readonly ConditionalWeakTable<ShadowBox, object> retiredNativeShadows = new ConditionalWeakTable<ShadowBox, object>();

        private void InstallNativeShadowHooks()
        {
            Patch(AccessTools.DeclaredMethod(typeof(MonsterAttackBehaviourShadowImpact), "Invoke"), prefix: nameof(NativeShadowInvoking));
            Patch(AccessTools.DeclaredMethod(typeof(MonsterAttackBehaviourShadowImpact), "StartImpact"), prefix: nameof(NativeShadowStarting), finalizer: nameof(NativeShadowStarted));
            Patch(AccessTools.DeclaredMethod(typeof(ShadowBox), "OnFixedUpdate"), prefix: nameof(NativeSkillAdvancing), finalizer: nameof(NativeSkillAdvanced));
            Patch(AccessTools.DeclaredMethod(typeof(ShadowBox), "OnTriggerEnter2D"), prefix: nameof(NativeShadowHitting));
        }
        private static bool NativeShadowInvoking(MonsterAttackBehaviourShadowImpact __instance)
        { return current == null || !current.nativeShadowSources.TryGetValue(__instance, out var source) || source.Valid; }

        private static bool NativeShadowStarting(MonsterAttackBehaviourShadowImpact __instance, out NativeSkillInvocation? __state)
        {
            __state = null;
            var module = current;
            if (module == null) return true;
            InstanceScope? scope = null;
            if (module.nativeShadowSources.TryGetValue(__instance, out var source))
            {
                if (!source.Valid) return false;
                var proto = (IShadowImpact)__instance._proto;
                if (proto.HasReadyAction && NativeAccess.Get(NativeAccess.Get(__instance, "_handle")!, "_tween") == null) return false;
                scope = source.Scope;
                scope.NativeSkillDepth++;
            }
            // Even an unrelated native attack masks an outer author's acquisition scope.
            __state = new NativeSkillInvocation(module, scope);
            module.raisingNativeSkill = __state;
            return true;
        }
        private static Exception? NativeShadowStarted(MonsterAttackBehaviourShadowImpact __instance, NativeSkillInvocation? __state, Exception? __exception)
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
                // The original continuation can enable the body box/create a tween after Raise returns.
                Attempt(errors, () => CancelTweens(__instance));
                Attempt(errors, () => module.CancelNativeSkills(scope, __state.Action));
                if (scope.Controller != null)
                {
                    var active = NativeAccess.Get(scope.Controller.attackBehaviourManager, "_currentBehaviour");
                    if (active == null || ReferenceEquals(active, __instance))
                    {
                        foreach (var box in scope.Controller.GetComponentsInChildren<PhysicalDamageBox>(true))
                            Attempt(errors, () => { box.OnAttackedCallback = null; box.Enabled = false; box.ClearBuffer(); });
                        if (scope.Active) Attempt(errors, () => RestoreActionPresentation(scope, false));
                    }
                }
            }
            foreach (var owner in module.nativeSkillManagers.Values.Where(o => o.Leases.Any(l => ReferenceEquals(l.Scope, scope))).ToArray())
                Attempt(errors, () => module.DrainNativeSkillManager(owner));
            if (scope.PendingRemoveLogic) Attempt(errors, () => module.Revoke(scope, true));
            if (__exception != null || errors.Count != 0)
                module.FailAction(scope, "NativeShadow.Start", new AggregateException("Native ShadowImpact creation/cleanup failed", __exception == null ? errors : new[] { __exception }.Concat(errors)));
            return null;
        }
        private void RetireNativeShadow(Skill skill)
        {
            if (!(skill is ShadowBox shadow)) return;
            retiredNativeShadows.GetValue(shadow, _ => new object());
            CancelTweens(shadow);
        }
        private static bool NativeShadowHitting(ShadowBox __instance)
        {
            var module = current;
            return module == null || !module.retiredNativeShadows.TryGetValue(__instance, out _)
                && (!module.nativeSkills.TryGetValue(__instance, out var lease) || lease.Valid);
        }
    }
}
