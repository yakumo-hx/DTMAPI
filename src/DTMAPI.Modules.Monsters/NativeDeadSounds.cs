using System;
using System.Runtime.CompilerServices;
using DolocTown;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private readonly ConditionalWeakTable<Monster, object> nativeDeadHandled = new ConditionalWeakTable<Monster, object>();
        private int nativeDeadPlays, nativeDeadFallbacks;
        private static bool CanNativeDeadSound(Template template) => NativeDeadTarget(template);
        private static bool NativeDeadTarget(Template template) => DeadSoundBoundary && template.Prefab != null && template.Prefab.GetComponent<MonsterController>() != null;
        private bool TryPlayNativeDeadSound(MonsterController controller)
        {
            if (!DeadSoundBoundary || !NativeSoundController(controller, dead: true)) return false;
            var logical = controller.Monster;
            if (nativeDeadHandled.TryGetValue(logical, out _)) return true;
            if (nativeOverrides.ResolveDeadSound(logical.proto.Name) == null) return false;
            nativeDeadHandled.Add(logical, new object());
            try { StopNativeHurtSound(controller); }
            catch (Exception ex) { nativeDeadFallbacks++; Log("Native Dead cleanup retained reason=" + ex.Message); return false; }
            bool played = TryPlayNativeCommonSound(controller, "sounds.dead", "dead");
            if (played) nativeDeadPlays++; else nativeDeadFallbacks++;
            return played;
        }
        private void StopNativeDeadSounds(Func<NativeCommonSoundRequest, bool> predicate) => StopNativeCommonSounds(r => r.Use == "dead" && predicate(r));
    }
}
