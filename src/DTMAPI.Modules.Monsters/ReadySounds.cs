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
        private static readonly MethodInfo ReadyBatteringTarget = AccessTools.DeclaredMethod(typeof(MonsterAttackBehaviourBattering), "Invoke");
        private static readonly MethodInfo ReadyBatteringBulletTarget = AccessTools.DeclaredMethod(typeof(MonsterAttackBehaviourBatteringBullet), "Invoke");
        private static readonly MethodInfo ReadyImpactTarget = AccessTools.DeclaredMethod(typeof(MonsterAttackBehaviourImpact), "Invoke");
        private static readonly MethodInfo ReadyMultiImpactTarget = AccessTools.DeclaredMethod(typeof(MonsterAttackBehaviourMultiImpact), "ReadyForImpact", new[] { typeof(int) });
        private static readonly MethodInfo ReadyShadowTarget = AccessTools.DeclaredMethod(typeof(MonsterAttackBehaviourShadowImpact), "Invoke");
        private static readonly MethodInfo ReadyStingTarget = AccessTools.DeclaredMethod(typeof(MonsterAttackBehaviourSting), "Invoke");
        private static MethodInfo? ReadySoundTarget(IMonsterAttackBehaviour proto) => proto is IMultiImpact ? ReadyMultiImpactTarget :
            proto is IImpact ? ReadyImpactTarget : proto is IShadowImpact ? ReadyShadowTarget : proto is ISting ? ReadyStingTarget :
            proto is IBatteringBullet ? ReadyBatteringBulletTarget : proto is IBattering ? ReadyBatteringTarget : null;
        private static bool ReadySoundBoundary(IMonsterAttackBehaviour proto)
        {
            var target = ReadySoundTarget(proto);
            if (target == null || !SoundBoundary(target, nameof(ReadySoundTranspiler))) return false;
            if (proto is ISting) return true;
            if (!SoundBoundary(DashSoundTarget(proto)!, nameof(ReadyStopTranspiler))) return false;
            return true;
        }
        private void InstallReadySoundHooks()
        {
            foreach (var method in new[] { ReadyBatteringTarget, ReadyBatteringBulletTarget, ReadyImpactTarget, ReadyMultiImpactTarget, ReadyShadowTarget, ReadyStingTarget })
                Patch(method, transpiler: nameof(ReadySoundTranspiler));
            foreach (var method in new[] { DashBatteringTarget, DashBatteringBulletTarget, DashImpactTarget, DashMultiImpactTarget, DashShadowTarget })
                Patch(method, transpiler: nameof(ReadyStopTranspiler));
        }
        private static IEnumerable<CodeInstruction> RewriteReadySound(IEnumerable<CodeInstruction> instructions, MethodBase method, SoundEvents expected, string handler)
        {
            var code = instructions.ToList();
            var post = AccessTools.Method(typeof(MonsterController), "PostSoundEvent", new[] { typeof(SoundEvents) });
            int site = code.FindIndex(i => i.Calls(post));
            if (site < 1 || code.Count(i => i.Calls(post)) != 1 || !code[site - 1].LoadsConstant((int)expected) || code[site].blocks.Count != 0)
                throw new InvalidOperationException("Native preparation sound call/constant changed: " + method);
            code[site].opcode = OpCodes.Ldarg_0; code[site].operand = null;
            code.Insert(site + 1, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NativeModule), handler)));
            return code;
        }
        private static IEnumerable<CodeInstruction> ReadySoundTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
            => RewriteReadySound(instructions, __originalMethod, __originalMethod.Equals(ReadyStingTarget) ? SoundEvents.PLAY_DRONE_BEE_PRE : SoundEvents.PLAY_DRONE_BOMB_COUNT_DOWN, nameof(PostReadySound));
        private static IEnumerable<CodeInstruction> ReadyStopTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
            => RewriteReadySound(instructions, __originalMethod, SoundEvents.STOP_DRONE_BOMB_COUNT_DOWN, nameof(StopReadySound));
        private static void PostReadySound(MonsterController controller, SoundEvents nativeEvent, MonsterAttackBehaviour attack)
        {
            if (current?.TryPlayCommonReady(controller, attack, nativeEvent) != true) controller.PostSoundEvent(nativeEvent);
        }
        private static void StopReadySound(MonsterController controller, SoundEvents nativeEvent, MonsterAttackBehaviour attack)
        {
            // The existing PostSoundEvent prefix stops this rental's countdown
            // requests and lets the native 104 stop earlier fallback voices.
            controller.PostSoundEvent(nativeEvent);
        }
    }
}
