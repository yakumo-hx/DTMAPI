using System;
using DolocTown;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private int nativeHurtPlays, nativeHurtFallbacks;
        private static bool CanNativeHurtSound(Template template) => NativeHurtTarget(template);
        private static bool NativeHurtTarget(Template template) => HurtSoundBoundary && template.Prefab != null && template.Prefab.GetComponent<MonsterController>() != null;
        private bool TryPlayNativeHurtSound(MonsterController controller)
        {
            if (!HurtSoundBoundary || !NativeSoundController(controller) || nativeOverrides.ResolveHurtSound(controller.Monster.proto.Name) == null) return false;
            try { StopNativeHurtSound(controller); }
            catch (Exception ex) { nativeHurtFallbacks++; Log("Native Hurt stop retained reason=" + ex.Message); return false; }
            bool played = TryPlayNativeCommonSound(controller, "sounds.hurt", "hurt");
            if (played) nativeHurtPlays++; else nativeHurtFallbacks++;
            return played;
        }
        private void StopNativeHurtSound(MonsterController controller) => StopNativeHurtSounds(r => ReferenceEquals(r.Controller, controller));
        private void StopNativeHurtSounds(Func<NativeCommonSoundRequest, bool> predicate) => StopNativeCommonSounds(r => r.Use == "hurt" && predicate(r));
    }
}
