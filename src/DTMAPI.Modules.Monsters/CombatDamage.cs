using System;
using DTMAPI.MonsterContent;
using DolocTown;
using HarmonyLib;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private struct DamageObservation
        {
            internal InstanceScope? Scope;
            internal CombatSequence? Behavior;
            internal MonsterHandle Handle;
            internal int Health;
        }
        private void InstallCombatDamageHooks()
        {
            Patch(AccessTools.Method(typeof(Monster), "Damage", new[] { typeof(float), typeof(bool), typeof(int).MakeByRefType() }),
                prefix: nameof(DamageBefore), postfix: nameof(DamageAfter));
            Patch(AccessTools.Method(typeof(Monster), "Damage", new[] { typeof(float), typeof(bool), typeof(float), typeof(int).MakeByRefType() }),
                prefix: nameof(DamageBefore), postfix: nameof(DamageAfter));
            Patch(AccessTools.Method(typeof(Monster), "ManualDamage", new[] { typeof(int) }), postfix: nameof(PhaseHealthChanged));
        }
        private static void DamageBefore(Monster __instance, out DamageObservation __state)
        {
            __state = default;
            if (current == null || __instance.IsDead || !current.instances.TryGetValue(__instance, out var scope)
                || !scope.Active || scope.Controller == null || scope.Binding.Definition.Definition.Combat?.Mode != "retaliate"
                || !(scope.Behavior is CombatSequence combat)) return;
            __state = new DamageObservation { Scope = scope, Behavior = combat, Handle = scope.Handle, Health = ReadHealth(__instance) };
        }
        private static void PhaseHealthChanged(Monster __instance)
        {
            if (current != null && !__instance.IsDead && current.instances.TryGetValue(__instance, out var phased))
            {
                try { current.ObserveHealthPhase(phased); }
                catch (Exception ex) { current.FailAction(phased, "HealthPhase.Damage", ex); }
            }
        }
        private static void DamageAfter(Monster __instance, DamageObservation __state)
        {
            PhaseHealthChanged(__instance);
            var scope = __state.Scope;
            // Death/recycle/reentrant owner cancellation takes precedence. A pooled controller
            // cannot inherit a hit observed for an older behavior object or instance generation.
            if (scope == null || !scope.Active || __instance.IsDead || !scope.Handle.Equals(__state.Handle)
                || !ReferenceEquals(scope.Logical, __instance) || !ReferenceEquals(scope.Behavior, __state.Behavior)) return;
            int health = ReadHealth(__instance);
            if (health > 0 && health < __state.Health) __state.Behavior!.OnEffectiveHit();
        }
    }
}
