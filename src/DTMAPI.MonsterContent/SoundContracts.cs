using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DTMAPI.MonsterContent
{
    public sealed class MonsterSounds
    {
        public MonsterSound? Hurt { get; set; }
        public MonsterSound? Dead { get; set; }
        public MonsterSound? Humming { get; set; }
        /// <summary>One shot at the native AI recovery warning; an actual native stop or instance cleanup ends its tail.</summary>
        public MonsterSound? RecoveryWarning { get; set; }
        internal MonsterSounds Copy() => new MonsterSounds { Hurt = Hurt?.Copy(), Dead = Dead?.Copy(), Humming = Humming?.Copy(), RecoveryWarning = RecoveryWarning?.Copy() };
    }
    /// <summary>A sound from an Audio registration belonging to the same Owner. Native Monster semantics select when to play it.</summary>
    public sealed class MonsterSound
    {
        public DTMAPI.AudioContent.IAudioSound? Sound { get; set; }
        // Offline author input only; the Runtime content composer binds it before domain admission.
        internal string ResourcePath { get; set; } = "";
        internal string ContentSoundId = "";
        internal Func<DTMAPI.AudioContent.IAudioSound?>? RegisteredSound;
        public float VolumeDb { get; set; } = 0;
        /// <summary>Root/override ResourcePath Humming only: null inherits the template event; false is finite, true loops.</summary>
        internal bool? Loop { get; set; }
        internal MonsterSound Copy() => new MonsterSound { Sound = Sound, ResourcePath = ResourcePath, ContentSoundId = ContentSoundId, RegisteredSound = RegisteredSound, VolumeDb = VolumeDb, Loop = Loop };
    }
    internal static class SoundValidation
    {
        internal static IEnumerable<KeyValuePair<string, MonsterSound>> CommonEvents(MonsterDefinition definition) =>
            Events(definition.Sounds).Concat(definition.NativeAttacks.SelectMany(a => NativeAttackValidation.Sounds(a)
                .Where(s => CommonAttackField(s.Key)).Select(s => new KeyValuePair<string, MonsterSound>(CommonAttackKey(s.Key, a.AttackId), s.Value))))
                .Concat(definition.NativeParts.SelectMany(p => Events(p.Sounds).Concat(p.NativeAttacks.SelectMany(a => NativeAttackValidation.Sounds(a)
                    .Select(s => new KeyValuePair<string, MonsterSound>(CommonAttackKey(s.Key, a.AttackId), s.Value))))
                    .Select(s => new KeyValuePair<string, MonsterSound>(CommonPartKey(p.TemplateId, s.Key), s.Value))));
        internal static string CommonPartKey(string template, string key) => "part/" + template + "/" + key;
        internal static string CommonUseKey(string key) => key.Substring(key.LastIndexOf('/') + 1);
        internal static bool CommonAttackField(string field) => field == "readySound" || field == "fireSound" || field == "dashSound" || field == "throwSound" || field == "volleySound" || PlacedField(field);
        internal static bool PlacedField(string field) => field == "landSound" || field == "countdownSound" || field == "explosionSound";
        internal static IEnumerable<KeyValuePair<string, MonsterSound>> CommonEvents(MonsterNativeOverride value) =>
            Events(value.Sounds).Concat(value.NativeAttacks.SelectMany(a => NativeAttackValidation.Sounds(a)
                .Select(s => new KeyValuePair<string, MonsterSound>(CommonAttackKey(s.Key, a.AttackId), s.Value))));
        internal static string CommonAttackKey(string field, string slot) => field.Substring(0, field.Length - "Sound".Length) + "." + slot;
        internal static IEnumerable<KeyValuePair<string, MonsterSound>> Events(MonsterSounds? sounds)
        {
            if (sounds?.Hurt != null) yield return new KeyValuePair<string, MonsterSound>("hurt", sounds.Hurt);
            if (sounds?.Dead != null) yield return new KeyValuePair<string, MonsterSound>("dead", sounds.Dead);
            if (sounds?.Humming != null) yield return new KeyValuePair<string, MonsterSound>("humming", sounds.Humming);
            if (sounds?.RecoveryWarning != null) yield return new KeyValuePair<string, MonsterSound>("recoveryWarning", sounds.RecoveryWarning);
        }
        internal static bool HasSounds(MonsterDefinition definition) => definition.Sounds != null ||
            definition.NativeAttacks.Any(a => NativeAttackValidation.Sounds(a).Any()) || definition.NativeParts.Any(p => p.Sounds != null || p.NativeAttacks.Any(a => NativeAttackValidation.Sounds(a).Any()));
        internal static void ReleaseInputs(MonsterDefinition definition)
        {
            definition.Sounds = null;
            foreach (var part in definition.NativeParts) part.Sounds = null;
            foreach (var attack in definition.NativeAttacks.Concat(definition.NativeParts.SelectMany(p => p.NativeAttacks))) { attack.FireSound = null; attack.DashSound = null; attack.ThrowSound = null; attack.VolleySound = null; attack.ReadySound = null; attack.LandSound = null; attack.CountdownSound = null; attack.ExplosionSound = null; }
        }
        internal static MonsterDiagnostic? ValidateResource(string owner, string localId, string templateId, MonsterSound sound, string field)
        {
            MonsterDiagnostic Error(string at, string message) => new MonsterDiagnostic("InvalidSound", message, owner, owner + ":" + localId, templateId, "Validate", at);
            if (sound.Sound == null && sound.RegisteredSound == null && string.IsNullOrEmpty(sound.ContentSoundId) && string.IsNullOrEmpty(sound.ResourcePath)) return Error(field + ".sound", "Monster sounds require a same-Owner Audio handle.");
            if (!DTMAPI.AudioContent.AudioValidation.ValidVolumeDb(sound.VolumeDb)) return Error(field + ".volumeDb", "Volume must be dB or negative infinity for mute.");
            if (sound.Loop == true && field != "sounds.humming" && !field.EndsWith(".sounds.humming", StringComparison.Ordinal)) return Error(field + ".loop", "Only Humming supports an explicit loop.");
            try { if (sound.Sound == null && sound.RegisteredSound == null && sound.ResourcePath.Length != 0) DTMAPI.AudioContent.AudioValidation.CopyAndValidate(new DTMAPI.AudioContent.AudioRegistration {
                Resources = new[] { new DTMAPI.AudioContent.AudioResource { Key = "media", Path = sound.ResourcePath } },
                Events = new[] { new DTMAPI.AudioContent.AudioEvent { Key = "sound", Resource = "media", Use = DTMAPI.AudioContent.AudioUse.Effect, Route = DTMAPI.AudioContent.AudioRoute.NativeMonster } } }); }
            catch (InvalidDataException ex) { return Error(field + ".resourcePath", ex.Message); }
            return null;
        }
        internal static MonsterDiagnostic? Validate(string owner, MonsterDefinition definition)
            => Validate(owner, definition.LocalId, definition.TemplateId, definition.Sounds, "sounds");
        internal static MonsterDiagnostic? Validate(string owner, string localId, string templateId, MonsterSounds? sounds, string prefix)
        {
            MonsterDiagnostic Error(string field, string message) => new MonsterDiagnostic("InvalidSound", message, owner, owner + ":" + localId, templateId, "Validate", field);
            if (sounds == null) return null;
            if (!Events(sounds).Any()) return Error(prefix, "An explicitly supplied sounds object requires an event.");
            foreach (var entry in Events(sounds))
            {
                var error = ValidateResource(owner, localId, templateId, entry.Value, prefix + "." + entry.Key);
                if (error != null) return error;
            }
            return null;
        }
    }
}
