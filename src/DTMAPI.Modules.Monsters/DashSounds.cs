using System.Collections.Generic;
using System.Reflection;
using DolocTown;
using DolocTown.MonsterAttackBehaviours;
using HarmonyLib;
using UnityEngine;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private static readonly MethodInfo DashBatteringTarget = AccessTools.DeclaredMethod(typeof(MonsterAttackBehaviourBattering), "StartBattering");
        private static readonly MethodInfo DashBatteringBulletTarget = AccessTools.DeclaredMethod(typeof(MonsterAttackBehaviourBatteringBullet), "StartBattering");
        private static readonly MethodInfo DashImpactTarget = AccessTools.DeclaredMethod(typeof(MonsterAttackBehaviourImpact), "StartImpact");
        private static readonly MethodInfo DashMultiImpactTarget = AccessTools.DeclaredMethod(typeof(MonsterAttackBehaviourMultiImpact), "DoImpact", new[] { typeof(int) });
        private static readonly MethodInfo DashShadowTarget = AccessTools.DeclaredMethod(typeof(MonsterAttackBehaviourShadowImpact), "_StartImpact", new[] { typeof(Vector2) });
        private static readonly MethodInfo DashStingTarget = AccessTools.DeclaredMethod(typeof(MonsterAttackBehaviourSting), "StartSting");
        private static MethodInfo? DashSoundTarget(IMonsterAttackBehaviour proto) => proto is IMultiImpact ? DashMultiImpactTarget :
            proto is IImpact ? DashImpactTarget : proto is IShadowImpact ? DashShadowTarget : proto is ISting ? DashStingTarget :
            proto is IBatteringBullet ? DashBatteringBulletTarget : proto is IBattering ? DashBatteringTarget : null;
        private static bool DashSoundBoundary(IMonsterAttackBehaviour proto)
        {
            var target = DashSoundTarget(proto);
            return target != null && SoundBoundary(target, nameof(DashSoundTranspiler));
        }
        private void InstallDashSoundHooks()
        {
            foreach (var method in new[] { DashBatteringTarget, DashBatteringBulletTarget, DashImpactTarget, DashMultiImpactTarget, DashShadowTarget, DashStingTarget })
                Patch(method, transpiler: nameof(DashSoundTranspiler));
        }
        private static IEnumerable<CodeInstruction> DashSoundTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
            => RewriteAttackSoundPost(instructions, __originalMethod, nameof(PostDashSound));
        private static void PostDashSound(MonsterController controller, string nativeEvent, MonsterAttackBehaviour attack)
        { if (current?.TryPlayAttackSound(controller, attack, "dashSound") != true) controller.PostSoundEvent(nativeEvent); }
    }
}
