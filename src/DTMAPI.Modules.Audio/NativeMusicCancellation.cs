using System;
using System.Reflection;

namespace DTMAPI.Modules.Audio
{
    // Targeted cancellation of an owned, already terminated native request.
    // Pause/Restart would broadcast MUSIC Stop; completion would fake an End.
    // Release only the native playback gate, leaving every playlist/timer alone.
    internal sealed class NativeMusicCancellation
    {
        private readonly PropertyInfo name;
        private readonly FieldInfo playing, stopped;
        internal NativeMusicCancellation(Type manager)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            name = manager.GetProperty("currentBgmEventName", flags) ?? throw new MissingMemberException("AudioMusicCancelNameUnsupported");
            playing = manager.GetField("isBgmPlaying", flags) ?? throw new MissingFieldException("AudioMusicCancelPlayingUnsupported");
            stopped = manager.GetField("isBgmStoped", flags) ?? throw new MissingFieldException("AudioMusicCancelStoppedUnsupported");
            if (name.PropertyType != typeof(string) || name.GetGetMethod(true) == null || name.GetSetMethod(true) == null ||
                playing.FieldType != typeof(bool) || stopped.FieldType != typeof(bool) || playing.IsInitOnly)
                throw new MissingMemberException("AudioMusicCancelSignatureUnsupported");
        }
        internal bool Release(object manager, string logical)
        {
            if (!(bool)playing.GetValue(manager)! || (bool)stopped.GetValue(manager)! || (string?)name.GetValue(manager) != logical) return false;
            name.SetValue(manager, string.Empty);
            try { playing.SetValue(manager, false); }
            catch { name.SetValue(manager, logical); throw; }
            return true;
        }
        internal bool Resume(object manager, string logical)
        {
            if ((bool)playing.GetValue(manager)! || (bool)stopped.GetValue(manager)! || !string.IsNullOrEmpty((string?)name.GetValue(manager))) return false;
            name.SetValue(manager, logical);
            try { playing.SetValue(manager, true); }
            catch { name.SetValue(manager, string.Empty); throw; }
            return true;
        }
    }
}
