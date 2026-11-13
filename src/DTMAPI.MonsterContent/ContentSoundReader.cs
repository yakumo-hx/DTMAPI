using System.Collections.Generic;

namespace DTMAPI.MonsterContent
{
    internal static class ContentSoundReader
    {
        internal static MonsterSounds Read(object? value, ContentReadContext context, string at = "sounds")
        {
            var json = context.Json;
            var sounds = json.Object(value, at, "", "hurt dead humming recoveryWarning");
            if (sounds.Count == 0) throw json.Error("InvalidSound", at, "An explicit sounds object requires an event.");
            return new MonsterSounds {
                Hurt = sounds.ContainsKey("hurt") ? context.ReadSound(sounds["hurt"], at + ".hurt") : null,
                Dead = sounds.ContainsKey("dead") ? context.ReadSound(sounds["dead"], at + ".dead") : null,
                Humming = sounds.ContainsKey("humming") ? context.ReadSound(sounds["humming"], at + ".humming") : null,
                RecoveryWarning = sounds.ContainsKey("recoveryWarning") ? context.ReadSound(sounds["recoveryWarning"], at + ".recoveryWarning") : null };
        }
        internal static MonsterSound ReadEvent(object? value, string at, ContentJson json)
        {
            var sound = json.Object(value, at, "sound", "volumeDb");
            string field = at + ".sound";
            string key = json.Text(sound["sound"], field);
            if (!MonsterAdmission.ValidLocalId(key)) throw json.Error("InvalidSound", field, "Expected a local Audio sound key.");
            return new MonsterSound { ContentSoundId = key, VolumeDb = ReadVolume(sound, at, json) };
        }
        internal static float ReadVolume(Dictionary<string, object?> sound, string at, ContentJson json)
        {
            if (!sound.TryGetValue("volumeDb", out var value)) return 0;
            if (value is string mute && mute == "-inf") return float.NegativeInfinity;
            float db = (float)json.Number(value, at + ".volumeDb");
            if (float.IsInfinity(db) || !DTMAPI.AudioContent.AudioValidation.ValidVolumeDb(db)) throw json.Error("InvalidSound", at + ".volumeDb", "Invalid dB volume.");
            return db;
        }
    }
}
