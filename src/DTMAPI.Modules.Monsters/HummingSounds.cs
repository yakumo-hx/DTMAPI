using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using DolocTown;
using HarmonyLib;
using UnityEngine;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private static readonly MethodInfo HummingSoundTarget = AccessTools.Method(typeof(MonsterController), "Run", new[] { typeof(MonsterEnv), typeof(Monster), typeof(BattleSystem) });
        private static bool HummingSoundBoundary => SoundBoundary(HummingSoundTarget, nameof(HummingSoundTranspiler));
        private static IEnumerable<CodeInstruction> HummingSoundTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var call = AccessTools.Method(typeof(MonsterController), "PostSoundEvent", new[] { typeof(string) });
            var code = instructions.ToList(); var sites = code.Where(i => i.Calls(call)).ToArray();
            if (sites.Length != 1) throw new InvalidOperationException("Expected one exact Run Humming sound-event call site.");
            sites[0].opcode = OpCodes.Call; sites[0].operand = AccessTools.Method(typeof(NativeModule), nameof(PostOwnedHummingSound));
            return code;
        }
        private static void PostOwnedHummingSound(MonsterController controller, string nativeEvent)
        { if (current?.TryPlayHummingSound(controller) != true && current?.TryPlayNativeHummingSound(controller) != true) controller.PostSoundEvent(nativeEvent); }
        private bool TryPlayHummingSound(MonsterController controller)
        {
            if (!Ready || !controllers.TryGetValue(controller, out var scope) || !scope.Active || scope.PendingRemoveLogic ||
                scope.Binding.NativeChild || !ReferenceEquals(scope.Controller, controller) || scope.Logical?.IsDead != false ||
                !scope.Binding.CommonSounds.ContainsKey("humming") || !HummingSoundBoundary) return false;
            return TryPlayCommonSound(scope, scope.Binding.CommonSounds["humming"], "humming");
        }
        private void StopHummingSound(InstanceScope scope) => StopCommonSounds(r => ReferenceEquals(r.Scope, scope) && r.Rental.Equals(scope.Handle) && r.Use == "humming");
    }
}
