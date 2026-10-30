using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using DolocTown;
using HarmonyLib;
using UnityEngine;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private void MarkSoundWithdrawal(Func<InstanceScope, bool> predicate)
        {
            // Mark before withdrawing spawn rules/children: those may recycle first.
            foreach (var scope in instances.Values.Where(predicate)) scope.SoundWithdrawalRental = scope.Handle;
        }
        private static bool IsSoundWithdrawal(InstanceScope scope) => scope.SoundWithdrawalRental.HasValue && scope.SoundWithdrawalRental.Value.Equals(scope.Handle);
        private static IEnumerable<CodeInstruction> RecycleSoundTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var post = AccessTools.Method(typeof(WwiseSoundManager), "PostSoundEvent", new[] {
                typeof(SoundEvents), typeof(GameObject), typeof(AkCallbackManager.EventCallback), typeof(bool) });
            var code = instructions.ToList();
            var sites = code.Where(i => i.Calls(post)).ToArray();
            if (sites.Length != 1) throw new InvalidOperationException("Expected one exact Monster OnRecycle sound call.");
            // Only this native call site is redirected. OnStop, Unregister,
            // decorator/attack disposal and battle removal still execute natively.
            var site = sites[0];
            site.opcode = OpCodes.Call;
            site.operand = AccessTools.Method(typeof(NativeModule), nameof(PostRecycleSound));
            return code;
        }
        private static bool PostRecycleSound(WwiseSoundManager sound, SoundEvents evt, GameObject emitter,
            AkCallbackManager.EventCallback callback, bool ignoreOccupy)
        {
            var module = current;
            var controller = emitter == null ? null : emitter.GetComponent<MonsterController>();
            if (evt != SoundEvents.STOP_MONSTER_BUS || module == null || controller == null ||
                !module.controllers.TryGetValue(controller, out var scope) ||
                !ReferenceEquals(scope.Controller, controller) || !IsSoundWithdrawal(scope))
                return sound.PostSoundEvent(evt, emitter, callback, ignoreOccupy);
            StopWithdrawingNativeEmitter(controller);
            return true;
        }
        private static void StopWithdrawingNativeEmitter(MonsterController controller)
        {
            // Native/fallback voices use the controller emitter; public Audio
            // voices are stopped by their exact rental's AudioPlayback in CancelActions.
            // Never call the no-argument/global StopAll overload here.
            if (AkSoundEngine.IsInitialized()) AkSoundEngine.StopAll(controller.gameObject);
        }
    }
}
