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
        private sealed class FollowingEffect
        {
            internal readonly InstanceScope Scope;
            internal readonly ContinuesParticleEffects Effect;
            internal readonly MonsterHandle Instance;
            internal readonly MonsterActionHandle? Action;
            internal readonly MonsterAttackBehaviourBattering? BatteringOwner;
            internal bool Releasing;
            internal FollowingEffect(InstanceScope scope, ContinuesParticleEffects effect, MonsterAttackBehaviourBattering? batteringOwner)
            { Scope = scope; Effect = effect; Instance = scope.Handle; Action = scope.Action?.Handle; BatteringOwner = batteringOwner; }
        }
        private readonly Dictionary<ContinuesParticleEffects, FollowingEffect> followingEffects = new Dictionary<ContinuesParticleEffects, FollowingEffect>();
        private static MonsterAttackBehaviour? FollowingAttack(Func<Vector2>? getter)
        {
            if (getter == null) return null;
            if (getter.Target is MonsterAttackBehaviour attack) return attack;
            var target = getter.Target;
            if (target == null) return null;
            var type = target.GetType();
            // These two native Battering getters capture this through the same display class.
            // Do not walk arbitrary delegate closures or infer ownership from their contents.
            if (type != getter.Method.DeclaringType || type.DeclaringType != typeof(MonsterAttackBehaviourBattering) ||
                type.Name != "<>c__DisplayClass13_0" ||
                getter.Method.Name != "<BuildBatteringTween>b__0" && getter.Method.Name != "<BuildBatteringTween>b__2") return null;
            return NativeAccess.Get<MonsterAttackBehaviourBattering>(target, "<>4__this");
        }
        private static void RetireBatteringEffectReference(FollowingEffect owned)
        {
            // Dispose must not recycle this pooled object after a different consumer reacquires it.
            // Only clear the exact reference belonging to the retiring lease.
            if (owned.BatteringOwner != null && ReferenceEquals(NativeAccess.Get(owned.BatteringOwner, "_groundEffects"), owned.Effect))
                NativeAccess.Set(owned.BatteringOwner, "_groundEffects", null);
        }
        private static void EffectReusing(ContinuesParticleEffects __instance)
        {
            var module = current;
            if (module == null || !module.followingEffects.TryGetValue(__instance, out var previous)) return;
            // Pool reacquisition proves the old native lease has ended, including position-only
            // reuse which never assigns a new getter. A later old-owner retry must not recycle it.
            RetireBatteringEffectReference(previous);
            previous.Scope.Effects.Remove(previous); module.followingEffects.Remove(__instance);
            if (previous.Releasing) module.Log("Effect pool reuse retired failed lease=" + previous.Instance + "; effect=" + __instance.GetInstanceID());
        }
        private static void EffectPositionAssigned(ContinuesParticleEffects __instance, ref Func<Vector2>? value)
        {
            var module = current;
            if (module == null) return;
            // The native setter is the acquisition boundary, before Play can fail. Null assignment
            // on native recycle also detaches our record before the shared pool can reuse the effect.
            if (module.followingEffects.TryGetValue(__instance, out var previous))
            {
                if (previous.Releasing && value == null) return;
                // A fresh non-null acquisition may follow native automatic recycling after
                // our failed cleanup. Retire the old lease before assigning the reused object.
                RetireBatteringEffectReference(previous);
                previous.Scope.Effects.Remove(previous); module.followingEffects.Remove(__instance);
            }
            MonsterController? controller = (value?.Target as Component)?.GetComponent<MonsterController>();
            var attack = FollowingAttack(value);
            if (controller == null && attack != null) controller = NativeAccess.Get<MonsterController>(attack, "_controller");
            if (controller == null || !module.controllers.TryGetValue(controller, out var scope)) return;
            if (!scope.Active) { value = null; return; }
            var owned = new FollowingEffect(scope, __instance, attack as MonsterAttackBehaviourBattering);
            module.followingEffects.Add(__instance, owned); scope.Effects.Add(owned);
        }
        private void CancelFollowingEffects(InstanceScope scope, MonsterActionHandle? action = null)
        {
            var errors = new List<Exception>();
            foreach (var owned in scope.Effects.Where(e => action == null || e.Action.HasValue && e.Action.Value.Equals(action.Value)).ToArray())
            {
                if (!followingEffects.TryGetValue(owned.Effect, out var held) || !ReferenceEquals(owned, held)) continue;
                Attempt(errors, () => {
                    owned.Releasing = true;
                    owned.Effect.positionGetter = null!;
                    Fault("EffectRecycle");
                    owned.Effect.Recycle(); // Keep ownership until native recycling succeeds.
                    RetireBatteringEffectReference(owned);
                    scope.Effects.Remove(owned); followingEffects.Remove(owned.Effect);
                });
            }
            ThrowCleanup(errors);
        }
    }
}
