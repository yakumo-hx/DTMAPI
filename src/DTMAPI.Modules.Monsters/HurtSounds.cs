using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Hosting;
using DTMAPI.AudioContent;
using DolocTown;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private MonsterSoundDriver? soundDriver;
        private static readonly MethodInfo HurtSoundTarget = AccessTools.Method(typeof(MonsterController), "Hurt", new[] { typeof(Transform), typeof(float), typeof(bool), typeof(bool) });
        private static bool SoundBoundary(MethodInfo target, string transpiler)
        {
            var patches = Harmony.GetPatchInfo(target)?.Transpilers;
            return patches != null && patches.Any(p => p.owner == HarmonyOwner && p.PatchMethod.Name == transpiler) && patches.All(p => p.owner == HarmonyOwner);
        }
        private static bool HurtSoundBoundary => SoundBoundary(HurtSoundTarget, nameof(HurtSoundTranspiler));
        private static IReadOnlyList<string> TemplateSoundEvents(Template template)
        {
            if (template.Errors.Count != 0) return Array.Empty<string>();
            var events = new List<string>();
            if (HurtSoundBoundary) events.Add("hurt");
            if (DeadSoundBoundary) events.Add("dead");
            if (HummingSoundBoundary) events.Add("humming");
            if (NativeRecoveryTarget(template)) events.Add("recoveryWarning");
            return events;
        }
        private void InstallSoundHooks()
        {
            InstallAttackSoundHooks();
            InstallRecoverySoundHooks();
            Patch(HurtSoundTarget, transpiler: nameof(HurtSoundTranspiler));
            Patch(DeadSoundTarget, transpiler: nameof(DeadSoundTranspiler));
            Patch(HummingSoundTarget, transpiler: nameof(HummingSoundTranspiler));
            Patch(AccessTools.Method(typeof(MonsterManager), "SetIsRunning", new[] { typeof(bool) }), prefix: nameof(SoundRoomStopping));
        }
        private static IEnumerable<CodeInstruction> HurtSoundTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var call = AccessTools.Method(typeof(MonsterController), "PostSoundEvent", new[] { typeof(string) });
            var replacement = AccessTools.Method(typeof(NativeModule), nameof(PostOwnedHurtSound));
            var code = instructions.ToList(); var sites = code.Where(i => i.Calls(call)).ToArray();
            if (sites.Length != 1) throw new InvalidOperationException("Expected one exact Hurt sound-event call site.");
            sites[0].opcode = OpCodes.Call; sites[0].operand = replacement;
            return code;
        }
        private static void PostOwnedHurtSound(MonsterController controller, string nativeEvent)
        { if (current?.TryPlayHurtSound(controller) != true && current?.TryPlayNativeHurtSound(controller) != true) controller.PostSoundEvent(nativeEvent); }
        private bool TryPlayHurtSound(MonsterController controller)
        {
            if (!Ready || !controllers.TryGetValue(controller, out var scope) || !scope.Active || scope.PendingRemoveLogic ||
                scope.Binding.NativeChild || !ReferenceEquals(scope.Controller, controller) || scope.Logical?.IsDead != false || !scope.Binding.CommonSounds.ContainsKey("hurt")) return false;
            if (!HurtSoundBoundary) return false;
            try
            {
                StopOwnedSound(scope);
                return TryPlayCommonSound(scope, scope.Binding.CommonSounds["hurt"], "hurt");
            }
            catch (Exception ex) { Log("Hurt sound fallback definition=" + scope.Binding.Definition.Id + " reason=" + ex.Message); return false; }
        }
        private void EnsureSoundDriver()
        {
            if (soundDriver != null) return;
            var root = new GameObject("DTMAPI.MonsterSound");
            try { Object.DontDestroyOnLoad(root); soundDriver = root.AddComponent<MonsterSoundDriver>(); soundDriver.UpdateSounds = TickSounds; soundDriver.enabled = false; }
            catch { Object.Destroy(root); throw; }
        }
        private void TickSounds()
        {
            TickCommonSounds();
            TickPlacedSounds();
            TickNativeSounds();
            UpdateSoundDriver();
        }
        private void UpdateSoundDriver()
        { if (commonSoundRequests.Count == 0 && placedSoundVoices.Count == 0 && nativeCommonSoundRequests.Count == 0 && soundDriver != null) soundDriver.enabled = false; }
        private void StopOwnedSound(InstanceScope scope) => StopCommonSounds(r => ReferenceEquals(r.Scope, scope) && r.Rental.Equals(scope.Handle) && r.Use == "hurt");
        private void ReleaseSounds(Binding binding)
        {
            StopCommonSounds(r => ReferenceEquals(r.Scope.Binding.Definition, binding.Definition));
            binding.CommonSounds.Clear();
        }
        private int CountSoundResources(string owner) => nativeCommonSoundRequests.Count(r => r.Owner == owner) + commonSoundRequests.Count(r => r.Scope.Binding.Definition.Owner == owner);
        private void DisposeSounds()
        {
            if (commonSoundRequests.Count != 0 || nativeCommonSoundRequests.Count != 0 || placedSoundVoices.Count != 0)
                throw new InvalidOperationException("Public sound requests still require retirement.");
            if (soundDriver != null) { soundDriver.UpdateSounds = null; Object.Destroy(soundDriver.gameObject); soundDriver = null; }
            commonSoundRequests.Clear();
            placedSoundVoices.Clear(); placedSoundSkills.Clear(); placedSoundBooms.Clear(); invokingPlacedBoom = null;
        }
    }
    public sealed class MonsterSoundDriver : MonoBehaviour
    {
        internal Action? UpdateSounds;
        private void LateUpdate() => UpdateSounds?.Invoke();
        private void OnDestroy() { UpdateSounds = null; }
    }
}
