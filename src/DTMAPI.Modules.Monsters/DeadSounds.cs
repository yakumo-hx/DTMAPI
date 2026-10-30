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
        private static readonly MethodInfo DeadSoundTarget = AccessTools.Method(typeof(Monster), "InvokeDeadEffects", new[] { typeof(Vector2) });
        private static bool DeadSoundBoundary => SoundBoundary(DeadSoundTarget, nameof(DeadSoundTranspiler));
        private static IEnumerable<CodeInstruction> DeadSoundTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var call = AccessTools.Method(typeof(MonsterController), "PostSoundEvent", new[] { typeof(string) });
            var code = instructions.ToList(); var sites = code.Where(i => i.Calls(call)).ToArray();
            if (sites.Length != 1) throw new InvalidOperationException("Expected one exact Dead sound-event call site.");
            sites[0].opcode = OpCodes.Call; sites[0].operand = AccessTools.Method(typeof(NativeModule), nameof(PostOwnedDeadSound));
            return code;
        }
        private static void PostOwnedDeadSound(MonsterController controller, string nativeEvent)
        { if (current?.TryPlayDeadSound(controller) != true && current?.TryPlayNativeDeadSound(controller) != true) controller.PostSoundEvent(nativeEvent); }
        private bool TryPlayDeadSound(MonsterController controller)
        {
            if (!Ready || !controllers.TryGetValue(controller, out var scope) || scope.PendingRemoveLogic ||
                scope.Binding.NativeChild || !ReferenceEquals(scope.Controller, controller) || scope.Logical?.IsDead != true || !scope.Binding.CommonSounds.ContainsKey("dead") || !DeadSoundBoundary) return false;
            if (scope.DeadSoundHandled) return true;
            scope.DeadSoundHandled = true;
            try
            {
                StopOwnedSound(scope);
                return TryPlayCommonSound(scope, scope.Binding.CommonSounds["dead"], "dead");
            }
            catch (Exception ex) { Log("Dead sound fallback definition=" + scope.Binding.Definition.Id + " reason=" + ex.Message); return false; }
        }
        private static void SoundRoomStopping(MonsterManager __instance, bool value)
        {
            if (value || current == null) return;
            var errors = new List<Exception>();
            Attempt(errors, () => current.StopNativeCommonSounds(r => ReferenceEquals(r.Manager, __instance)));
            Attempt(errors, () => current.StopCommonSounds(r => ReferenceEquals(r.Scope.Host.DM_monster, __instance)));
            current.StopPlacedSounds(v => ReferenceEquals(v.Source.Origin.Manager, __instance));
            Attempt(errors, () => current.StopNativeHurtSounds(v => ReferenceEquals(v.Manager, __instance)));
            Attempt(errors, () => current.StopNativeDeadSounds(v => ReferenceEquals(v.Manager, __instance)));
            Attempt(errors, () => current.StopNativeHummingSounds(v => ReferenceEquals(v.Manager, __instance)));
            ThrowCleanup(errors);
        }
    }
}
