using System;
using System.Linq;
using DolocTown;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private int nativeHummingPlays, nativeHummingFallbacks;
        private static bool CanNativeHummingSound(Template template) => NativeHummingTarget(template);
        private static bool NativeHummingTarget(Template template) => HummingSoundBoundary && template.Prefab != null && template.Prefab.GetComponent<MonsterController>() != null;
        private bool TryPlayNativeHummingSound(MonsterController controller)
        {
            if (!HummingSoundBoundary || !NativeSoundController(controller)) return false;
            if (nativeCommonSoundRequests.Any(r => r.Use == "humming" && ReferenceEquals(r.Controller, controller) && r.Rental == NativeSoundRental(controller))) return true;
            bool played = TryPlayNativeCommonSound(controller, "sounds.humming", "humming");
            if (played) nativeHummingPlays++; else if (nativeOverrides.ResolveHummingSound(controller.Monster.proto.Name) != null) nativeHummingFallbacks++;
            return played;
        }
        private void StopNativeHummingSound(MonsterController controller) => StopNativeHummingSounds(r => ReferenceEquals(r.Controller, controller));
        private void StopNativeHummingSounds(Func<NativeCommonSoundRequest, bool> predicate) => StopNativeCommonSounds(r => r.Use == "humming" && predicate(r));
    }
}
