using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using DolocTown;
using DolocTown.MonsterAttackBehaviours;
using HarmonyLib;
using UnityEngine;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private static readonly MethodInfo ThrowLandmineTarget = AccessTools.DeclaredMethod(typeof(MonsterAttackBehaviourSkillLandmine), "DropLandmine", new[] { typeof(Vector2) });
        private static readonly MethodInfo ThrowTimeBombTarget = AccessTools.DeclaredMethod(typeof(MonsterAttackBehaviourSkillTimeBomb), "DropTimeBomb", new[] { typeof(Vector2), typeof(float) });
        private static readonly MethodInfo VolleySoundTarget = AccessTools.DeclaredMethod(typeof(MonsterAttackBehaviourBulletTrain), "Shoot");
        private static bool AttackEventSoundBoundary(IMonsterAttackBehaviour proto, string field)
        {
            var target = field == "throwSound" ? proto is ILandmine ? ThrowLandmineTarget : proto is ITimeBomb ? ThrowTimeBombTarget : null :
                field == "volleySound" && proto is IBulletTrain ? VolleySoundTarget : null;
            return target != null && SoundBoundary(target, nameof(AttackEventSoundTranspiler));
        }
        private void InstallAttackEventSoundHooks()
        {
            foreach (var method in new[] { ThrowLandmineTarget, ThrowTimeBombTarget, VolleySoundTarget })
                Patch(method, transpiler: nameof(AttackEventSoundTranspiler));
        }
        private static IEnumerable<CodeInstruction> AttackEventSoundTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            bool volley = __originalMethod.Equals(VolleySoundTarget);
            if (!volley && !__originalMethod.Equals(ThrowLandmineTarget) && !__originalMethod.Equals(ThrowTimeBombTarget))
                throw new InvalidOperationException("Unknown native attack event sound boundary.");
            var expected = volley ? SoundEvents.PLAY_DRONE_ATTACK : SoundEvents.PLAY_DRONE_ATTACK_THROW_BOMB;
            var code = instructions.ToList();
            var post = AccessTools.Method(typeof(MonsterController), "PostSoundEvent", new[] { typeof(SoundEvents) });
            int site = code.FindIndex(i => i.Calls(post));
            if (site < 1 || code.Count(i => i.Calls(post)) != 1 || !code[site - 1].LoadsConstant((int)expected) || code[site].blocks.Count != 0)
                throw new InvalidOperationException("Native attack event sound call/constant changed: " + __originalMethod);
            // Keep labels on the inserted argument so any original incoming
            // branch still reaches a complete controller/event/attack stack.
            code[site].opcode = OpCodes.Ldarg_0; code[site].operand = null;
            code.Insert(site + 1, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NativeModule), volley ? nameof(PostVolleySound) : nameof(PostThrowSound))));
            return code;
        }
        private static void PostThrowSound(MonsterController controller, SoundEvents nativeEvent, MonsterAttackBehaviour attack)
        { if (current?.TryPlayAttackSound(controller, attack, "throwSound") != true) controller.PostSoundEvent(nativeEvent); }
        private static void PostVolleySound(MonsterController controller, SoundEvents nativeEvent, MonsterAttackBehaviour attack)
        { if (current?.TryPlayAttackSound(controller, attack, "volleySound") != true) controller.PostSoundEvent(nativeEvent); }
    }
}
