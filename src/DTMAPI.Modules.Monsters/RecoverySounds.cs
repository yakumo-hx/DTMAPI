using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using DTMAPI.MonsterContent;
using DolocTown;
using HarmonyLib;
using UnityEngine;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private static readonly Type[] RecoveryAiTypes = { typeof(MonsterAI_Nian), typeof(MonsterAI_NianHead), typeof(MonsterAI_NianTail) };
        private static readonly MethodInfo[] RecoverySoundTargets = RecoveryAiTypes.Select(t =>
            AccessTools.DeclaredMethod(AccessTools.Inner(t, "DropState"), "OnFixedUpdate", new[] { typeof(float) })).ToArray();
        private static readonly MethodInfo RecoverySoundStopTarget = AccessTools.DeclaredMethod(typeof(MonsterController), "PostSoundEvent", new[] { typeof(SoundEvents) });
        private static bool RecoverySoundBoundary(Type ai)
        {
            int index = Array.IndexOf(RecoveryAiTypes, ai);
            if (index < 0 || RecoverySoundTargets[index] == null || !SoundBoundary(RecoverySoundTargets[index], nameof(RecoverySoundTranspiler))) return false;
            var patches = Harmony.GetPatchInfo(RecoverySoundStopTarget);
            return patches != null && patches.Prefixes.Any(p => p.owner == HarmonyOwner && p.PatchMethod.Name == nameof(RecoverySoundStopping)) &&
                patches.Transpilers.All(p => p.owner == HarmonyOwner);
        }
        private static bool NativeRecoveryTarget(Template template) =>
            (template.Errors.Count == 0 || IsParameterPart(template)) && template.Prefab?.GetComponent<MonsterController>() != null && RecoverySoundBoundary(template.Ai);
        private static bool CanNativeRecoverySound(Template template) => NativeRecoveryTarget(template);
        private static IReadOnlyList<string> PartSoundEvents(Template template) =>
            IsParameterPart(template) && CanNativeRecoverySound(template) ? new[] { "recoveryWarning" } : Array.Empty<string>();
        private void InstallRecoverySoundHooks()
        {
            foreach (var target in RecoverySoundTargets) Patch(target, transpiler: nameof(RecoverySoundTranspiler));
            Patch(RecoverySoundStopTarget, prefix: nameof(RecoverySoundStopping));
        }
        private static IEnumerable<CodeInstruction> RecoverySoundTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            var code = instructions.ToList();
            int site = code.FindIndex(i => i.Calls(RecoverySoundStopTarget));
            if (site < 1 || code.Count(i => i.Calls(RecoverySoundStopTarget)) != 1 ||
                !code[site - 1].LoadsConstant((int)SoundEvents.PLAY_DRONE_BOMB_COUNT_DOWN) || code[site].blocks.Count != 0)
                throw new InvalidOperationException("Native recovery warning call/constant changed: " + __originalMethod);
            code[site].opcode = OpCodes.Call;
            code[site].operand = AccessTools.Method(typeof(NativeModule), nameof(PostRecoverySound));
            return code;
        }
        private static void PostRecoverySound(MonsterController controller, SoundEvents nativeEvent)
        { if (current?.TryPlayCommonRecovery(controller) != true) controller.PostSoundEvent(nativeEvent); }
        private static void RecoverySoundStopping(MonsterController __instance, SoundEvents __0)
        {
            if (__0 != SoundEvents.STOP_DRONE_BOMB_COUNT_DOWN || current == null) return;
            current.StopCommonCountdown(__instance);
            // The original 104 still handles native/fallback voices.
        }
    }
}
