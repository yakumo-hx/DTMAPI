using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DTMAPI.MonsterContent.Authoring
{
    internal static class SoundCodec
    {
        internal static Dictionary<string, byte[]> ReadAssets(object? value, ContentInput input)
        {
            var assets = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            foreach (var row in Json.Array(value, "audioAssets"))
            {
                var a = Json.Object(row, "audioAssets[]", "id name dataUrl");
                string id = Json.Text(a["id"], "audioAssets.id"), data = Json.Text(a["dataUrl"], "audioAssets.dataUrl");
                Json.Text(a["name"], "audioAssets.name");
                if (!MonsterAdmission.ValidLocalId(id) || assets.ContainsKey(id)) throw new AuthoringException("DuplicateId", "audioAssets.id", "Invalid or duplicate audio ID.");
                const string prefix = "data:audio/wav;base64,";
                if (!data.StartsWith(prefix, StringComparison.Ordinal)) throw new AuthoringException("InvalidSound", "audioAssets.dataUrl", "Expected embedded PCM16 WAV.");
                byte[] bytes;
                try { bytes = Convert.FromBase64String(data.Substring(prefix.Length)); }
                catch (FormatException) { throw new AuthoringException("InvalidSound", "audioAssets.dataUrl", "Invalid WAV base64."); }
                Check(bytes, "audioAssets.dataUrl");
                assets.Add(id, bytes); input.Audio[ContentCodec.AudioPath(bytes)] = bytes;
            }
            return assets;
        }
        private static void Check(byte[] bytes, string field)
        {
            try { DTMAPI.AudioContent.PcmWav.Inspect(bytes); }
            catch (InvalidDataException error) { throw new AuthoringException("InvalidSound", field, error.Message); }
        }
        internal static MonsterSounds Read(object? value, IDictionary<string, byte[]> assets, bool payload, Func<string, byte[]>? readBytes, string at = "sounds")
        {
            var sounds = Json.Object(value, at, "", "hurt dead humming recoveryWarning");
            if (sounds.Count == 0) throw new AuthoringException("InvalidSound", at, "An explicit sounds object requires an event.");
            return new MonsterSounds {
                Hurt = sounds.ContainsKey("hurt") ? ReadWav(sounds["hurt"], at + ".hurt", assets, payload, readBytes) : null,
                Dead = sounds.ContainsKey("dead") ? ReadWav(sounds["dead"], at + ".dead", assets, payload, readBytes) : null,
                Humming = sounds.ContainsKey("humming") ? ReadWav(sounds["humming"], at + ".humming", assets, payload, readBytes) : null,
                RecoveryWarning = sounds.ContainsKey("recoveryWarning") ? ReadWav(sounds["recoveryWarning"], at + ".recoveryWarning", assets, payload, readBytes) : null };
        }
        internal static MonsterSound ReadWav(object? value, string at, IDictionary<string, byte[]> assets, bool payload, Func<string, byte[]>? readBytes)
        {
            var sound = Json.Object(value, at, payload ? "sound" : "assetId", payload ? "volumeDb" : "volumeDb loop");
            string field = at + (payload ? ".sound" : ".assetId");
            string key = Json.Text(sound[payload ? "sound" : "assetId"], field);
            byte[] bytes;
            if (payload)
            {
                if (!MonsterAdmission.ValidLocalId(key)) throw new AuthoringException("InvalidSound", field, "Expected a local Audio sound key.");
                return new MonsterSound { ContentSoundId = key, VolumeDb = ReadVolume(sound, at) };
            }
            else if (!assets.TryGetValue(key, out bytes!)) throw new AuthoringException("ResourceMissing", field, "Audio ID does not exist.");
            return new MonsterSound { ResourcePath = ContentCodec.AudioPath(bytes), VolumeDb = ReadVolume(sound, at), Loop = sound.ContainsKey("loop") ? Json.Bool(sound["loop"], at + ".loop") : (bool?)null };
        }
        private static float ReadVolume(Dictionary<string, object?> sound, string at)
        {
            if (!sound.TryGetValue("volumeDb", out var value)) return 0;
            if (value is string mute && mute == "-inf") return float.NegativeInfinity;
            float db = (float)Json.Number(value, at + ".volumeDb");
            if (float.IsInfinity(db) || !DTMAPI.AudioContent.AudioValidation.ValidVolumeDb(db)) throw new AuthoringException("InvalidSound", at + ".volumeDb", "Invalid dB volume.");
            return db;
        }
        internal static object Write(MonsterSounds sounds)
        {
            var result = Json.Map();
            foreach (var sound in SoundValidation.Events(sounds)) result[sound.Key] = WriteWav(sound.Value);
            if (result.Count == 0) throw new AuthoringException("InvalidSound", "sounds", "An explicit sounds object requires an event.");
            return result;
        }
        internal static object WriteWav(MonsterSound sound)
        {
            var result = Json.Map("sound", sound.ContentSoundId, "volumeDb", float.IsNegativeInfinity(sound.VolumeDb) ? (object)"-inf" : sound.VolumeDb);
            return result;
        }
    }
}
