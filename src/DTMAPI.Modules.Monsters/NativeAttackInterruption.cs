using System;
using System.Collections.Generic;
using DolocTown;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private bool RunningNativeSkill(MonsterAttackBehaviour? attack, out DTMAPI.MonsterContent.MonsterActionHandle? action)
        {
            action = null;
            if (attack is MonsterAttackBehaviourSkillChomperMimicry mimicry && nativeMimicrySources.TryGetValue(mimicry, out var m) && m.Executing)
            { action = m.Action; return true; }
            if (attack is MonsterAttackBehaviourSkillThunderPile thunder && nativeThunderSources.TryGetValue(thunder, out var t) && t.Executing)
            { action = t.Action; return true; }
            return false;
        }
        private static bool NativeSkillAttackRequested(MonsterController __instance, MonsterAttackId id, ref UnityEngine.Transform target)
        {
            var module = current;
            if (module == null || !module.controllers.TryGetValue(__instance, out var scope) || !scope.Active) return true;
            if (target == null && __instance.attackBehaviourManager.TryGetAttackBehaviour(id, out var attack) && IsNativeSelfAttack(attack)) target = __instance.transform;
            if (target == null) return true;
            try
            {
                if (module.RunningNativeSkill(NativeAccess.Get(__instance.attackBehaviourManager, "_currentBehaviour") as MonsterAttackBehaviour, out var action))
                    module.CancelNativeSkills(scope, action);
                return true;
            }
            catch (Exception ex) { module.FailAction(scope, "NativeSkill.Replace", ex); return false; }
        }
        private static void NativeSkillHurt(MonsterController __instance, bool clearStatus)
        {
            if (!clearStatus || current == null || !current.controllers.TryGetValue(__instance, out var scope) || !scope.Active || UsesCode(scope)
                || !current.RunningNativeSkill(NativeAccess.Get(__instance.attackBehaviourManager, "_currentBehaviour") as MonsterAttackBehaviour, out _)) return;
            try { current.CancelNativeSkills(scope, null); }
            catch (Exception ex) { current.FailAction(scope, "NativeSkill.Hurt", ex); }
        }
        private static bool NativeAttackStopping(MonsterController __instance, out InstanceScope? __state)
        {
            __state = null;
            var module = current;
            if (module == null || !module.controllers.TryGetValue(__instance, out var scope) || !scope.Active || UsesCode(scope)) return true;
            try
            {
                if (scope.Bastion != null) CancelBastionAttack(scope.Bastion);
                var attack = NativeAccess.Get(__instance.attackBehaviourManager, "_currentBehaviour") as MonsterAttackBehaviour;
                if (attack == null) { if (scope.Bastion != null) __state = scope; return true; }
                // Native StopAttack drops the manager pointer without retiring its tween.
                // Retire owned work before its callback can start another attack; never invoke End.
                var errors = new List<Exception>();
                Attempt(errors, () => CancelChomperBite(attack));
                Attempt(errors, () => CancelTweens(attack));
                Attempt(errors, () => CancelTweens(__instance.Renderer.AttackBehaviourRenderer));
                Attempt(errors, () => NativeAccess.Set(__instance, "animationEvtCallback", null));
                Attempt(errors, () => module.CancelFollowingEffects(scope));
                Attempt(errors, () => module.CancelShots(scope));
                Attempt(errors, () => module.CancelNativeSkills(scope));
                foreach (var box in __instance.GetComponentsInChildren<PhysicalDamageBox>(true))
                    Attempt(errors, () => { box.OnAttackedCallback = null; box.Enabled = false; box.ClearBuffer(); });
                ThrowCleanup(errors);
                __state = scope;
                return true;
            }
            catch (Exception ex) { module.FailAction(scope, "NativeInterrupt", ex); return false; }
        }
        private static void NativeAttackStopped(MonsterController __instance, InstanceScope? __state)
        {
            var module = current;
            if (module == null || __state == null || !__state.Active || __state.Controller != __instance || __instance.attackBehaviourManager.IsAttacking) return;
            try { RestoreActionPresentation(__state, false); }
            catch (Exception ex) { module.FailAction(__state, "NativeInterrupt.Presentation", ex); }
        }
    }
}
