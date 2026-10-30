using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using DTMAPI.MonsterContent;
using DolocTown;
using DolocTown.MonsterAttackBehaviours;
using HarmonyLib;
using UnityEngine;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private static readonly MethodInfo FireVectorTarget = AccessTools.DeclaredMethod(typeof(MonsterAttackBehaviourBullet), "GenBullet", new[] { typeof(MonsterController), typeof(Vector2), typeof(bool) });
        private static readonly MethodInfo FireTransformTarget = AccessTools.DeclaredMethod(typeof(MonsterAttackBehaviourBullet), "GenBullet", new[] { typeof(MonsterController), typeof(Transform), typeof(bool) });
        private static readonly MethodInfo FireScatterTarget = AccessTools.DeclaredMethod(typeof(MonsterAttackBehaviourScatteringShoot), "ShootAction", new[] { typeof(Vector2) });
        private static readonly MethodInfo FireThrowTarget = AccessTools.DeclaredMethod(typeof(MonsterAttackBehaviourScatteringThrow), "<StartScatteringThrow>b__5_0", new[] { typeof(Vector2) });
        private static readonly MethodInfo FireFungusTarget = AccessTools.DeclaredMethod(typeof(MonsterAttackBehaviourFungusShoot), "DoAttack");
        private static readonly MethodInfo FireEmptyTarget = typeof(DolocPatch).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m => m.Name == "IsNullOrEmpty" && m.IsGenericMethodDefinition && m.GetParameters().Length == 1 &&
                m.GetParameters()[0].ParameterType.IsGenericType && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>)).MakeGenericMethod(typeof(char));
        private static bool FireSoundBoundary(IMonsterAttackBehaviour proto)
        {
            if (!(proto is IShoot || proto is IMultiShoot || proto is ISalvoShoot || proto is IScatteringShoot ||
                proto is IDragonShoot || proto is IScatteringThrow || proto is IBombingDive || proto is IFungusShoot || proto is IRandomShoot || proto is IBulletTrain)) return false;
            if (!SoundBoundary(FireVectorTarget, nameof(FireSoundTranspiler)) || !SoundBoundary(FireTransformTarget, nameof(FireSoundTranspiler))) return false;
            return proto is IScatteringShoot ? SoundBoundary(FireScatterTarget, nameof(FireSoundTranspiler)) :
                proto is IScatteringThrow ? SoundBoundary(FireThrowTarget, nameof(FireSoundTranspiler)) :
                !(proto is IFungusShoot) || SoundBoundary(FireFungusTarget, nameof(FireSoundTranspiler));
        }
        private static string FireReadAt(IMonsterAttackBehaviour proto)
        {
            string at = proto is IScatteringShoot ? "each native burst, once for all pellets" : proto is IScatteringThrow ? "native recoil callback, once for the entire toss" :
                proto is IFungusShoot ? "native attack animation event, once for all pellets" : "each native GenBullet call with shouldRaiseSound=true";
            return proto is IBulletTrain ? at + "; the separate volleySound field controls the native per-volley event" : at;
        }
        private void InstallFireSoundHooks()
        {
            foreach (var method in new[] { FireVectorTarget, FireTransformTarget, FireScatterTarget, FireThrowTarget, FireFungusTarget })
                Patch(method, transpiler: nameof(FireSoundTranspiler));
        }
        private static IEnumerable<CodeInstruction> FireSoundTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
            => RewriteAttackSoundPost(instructions, __originalMethod, nameof(PostFireSound));
        private static bool FireSoundMissing(string nativeEvent, MonsterAttackBehaviour attack) => string.IsNullOrEmpty(nativeEvent) &&
            current?.FindCommonAttackSound((MonsterController)AttackSoundControllerField.GetValue(attack), attack, "fireSound", out _) == null;
        private static void PostFireSound(MonsterController controller, string nativeEvent, MonsterAttackBehaviour attack)
        { if (current?.TryPlayAttackSound(controller, attack, "fireSound") != true) controller.PostSoundEvent(nativeEvent); }
    }
}
