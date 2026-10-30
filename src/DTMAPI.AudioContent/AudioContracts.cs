using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DTMAPI.AudioContent
{
    public static class AudioModule
    {
        public const string ProviderId = "DTMAPI.Audio";
    }
    public enum AudioPreparationState { Registered, Preparing, Ready, Failed, CleanupPending, Released }
    public enum AudioUse { Effect, Music }
    public enum AudioRoute { Actor, Ui, Ambience, NativeAnimal, NativeMonster, NativeMonsterDeath, NativeMonsterExplosion, Music }
    public enum AudioResourceKind { Source, Bank }
    /// <summary>World coordinates; only spatial Monster recipes accept position control.</summary>
    public readonly struct AudioPosition
    {
        public AudioPosition(float x, float y, float z = 0)
        {
            if (!Finite(x) || !Finite(y) || !Finite(z)) throw new ArgumentOutOfRangeException(nameof(x), "Audio position must be finite.");
            X = x; Y = y; Z = z;
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        public float X { get; }
        public float Y { get; }
        public float Z { get; }
    }
    public sealed class AudioResource
    {
        public string Key { get; set; } = "";
        public string Path { get; set; } = "";
        public string Sha256 { get; set; } = "";
        public bool Stream { get; set; }
        public AudioResourceKind Kind { get; set; }
        public string[] Media { get; set; } = Array.Empty<string>();
        public string[] Dependencies { get; set; } = Array.Empty<string>();
    }
    public sealed class AudioEvent
    {
        public string Key { get; set; } = "";
        public string Resource { get; set; } = "";
        public AudioUse Use { get; set; }
        public AudioRoute Route { get; set; }
        public string BankEvent { get; set; } = "";
        // Logical music name supplied by official Content (for example CD.bgm_event).
        public string NativeEvent { get; set; } = "";
        public string ReplaceNativeEvent { get; set; } = "";
        public float VolumeDb { get; set; } = 0;
        public bool Loop { get; set; }
        public int MinimumIntervalMs { get; set; }
    }
    public sealed class AudioRegistration
    {
        public AudioResource[] Resources { get; set; } = Array.Empty<AudioResource>();
        public AudioEvent[] Events { get; set; } = Array.Empty<AudioEvent>();
    }
    public sealed class AudioPlayback
    {
        private readonly Action stop;
        private readonly Func<bool> ended;
        private readonly Action<bool>? pause;
        private readonly Action<float>? volumeDb;
        private readonly Action<AudioPosition>? position;
        public AudioPlayback(Action stop, Func<bool> ended, Action<bool>? pause = null, Action<float>? volumeDb = null, Action<AudioPosition>? position = null)
        { this.stop = stop; this.ended = ended; this.pause = pause; this.volumeDb = volumeDb; this.position = position; }
        public bool HasEnded => ended();
        public void Stop() => stop();
        public void Pause(bool value) => pause?.Invoke(value);
        public void SetVolumeDb(float value) { if (!AudioValidation.ValidVolumeDb(value)) throw new ArgumentOutOfRangeException(nameof(value)); volumeDb?.Invoke(value); }
        public void SetPosition(AudioPosition value) => (position ?? throw new InvalidOperationException("AudioPositionUnsupported"))(value);
    }
    public interface IAudioContentApi
    {
        IAudioRegistration Register(AudioRegistration registration);
    }
    public interface IAudioRegistration : IDisposable
    {
        AudioRegistrationSnapshot GetSnapshot();
        IAudioSound GetSound(string key);
        void RetryPreparation();
    }
    public interface IAudioSound
    {
        AudioPlayback Play(float volumeDb = 0, AudioPosition? position = null);
    }
    public sealed class AudioRegistrationSnapshot
    {
        public AudioRegistrationSnapshot(AudioPreparationState state, string diagnostic = "") { State = state; Diagnostic = diagnostic; }
        public AudioPreparationState State { get; }
        public string Diagnostic { get; }
    }
    internal static class AudioValidation
    {
        public static AudioRegistration CopyAndValidate(AudioRegistration input)
        {
            if (input?.Resources == null || input.Events == null || input.Resources.Length == 0 || input.Events.Length == 0)
                throw new InvalidDataException("AudioDefinition: require at least one resource and event.");
            var resources = input.Resources.Select(r => r == null ? throw new InvalidDataException("AudioResourceNull") : new AudioResource { Key = r.Key, Path = r.Path, Sha256 = r.Sha256, Stream = r.Stream, Kind = r.Kind, Media = r.Media?.ToArray() ?? throw new InvalidDataException("AudioMediaNull"), Dependencies = r.Dependencies?.ToArray() ?? throw new InvalidDataException("AudioDependenciesNull") }).ToArray();
            var events = input.Events.Select(e => e == null ? throw new InvalidDataException("AudioEventNull") : new AudioEvent { Key = e.Key, Resource = e.Resource, Use = e.Use, Route = e.Use == AudioUse.Music ? AudioRoute.Music : e.Route, BankEvent = e.BankEvent, NativeEvent = e.NativeEvent, ReplaceNativeEvent = e.ReplaceNativeEvent?.ToUpperInvariant()!, VolumeDb = e.VolumeDb, Loop = e.Loop, MinimumIntervalMs = e.MinimumIntervalMs }).ToArray();
            if (resources.Any(r => !Key(r.Key) || !Path(r.Path) || r.Sha256 == null || r.Sha256.Length != 0 && (r.Sha256.Length != 64 || r.Sha256.Any(c => !(c >= '0' && c <= '9' || c >= 'a' && c <= 'f')))) || resources.Select(r => r.Key).Distinct(StringComparer.Ordinal).Count() != resources.Length)
                throw new InvalidDataException("AudioResource: unique keys and controlled relative paths required.");
            if (resources.Any(r => !Enum.IsDefined(typeof(AudioResourceKind), r.Kind) || r.Media.Concat(r.Dependencies).Any(p => !Path(p)) ||
                r.Kind == AudioResourceKind.Source && (r.Media.Length != 0 || r.Dependencies.Length != 0)))
                throw new InvalidDataException("AudioBankResources: controlled media/dependency paths required for Bank resources.");
            if (events.Any(e => e.BankEvent == null || !Enum.IsDefined(typeof(AudioRoute), e.Route) ||
                (resources.SingleOrDefault(r => r.Key == e.Resource)?.Kind == AudioResourceKind.Bank ? !Key(e.BankEvent) : e.BankEvent != "")))
                throw new InvalidDataException("AudioBankEvent: Bank resources require an entry event; source resources generate their own event.");
            if (events.Any(e => !Key(e.Key) || !resources.Any(r => r.Key == e.Resource) || !Supported(e) ||
                !ValidVolumeDb(e.VolumeDb) || e.MinimumIntervalMs < 0) || events.Select(e => e.Key).Distinct(StringComparer.Ordinal).Count() != events.Length)
                throw new InvalidDataException("AudioEvent: invalid reference/volumeDb/use/selector or unsupported replacement.");
            if (resources.Any(r => r.Stream && (r.Sha256.Length != 0 || !events.Any(e => e.Resource == r.Key) || events.Any(e => e.Resource == r.Key && e.Use != AudioUse.Music))))
                throw new InvalidDataException("AudioStreamingResource: music only; source identity uses trusted generation, size and mtime.");
            return new AudioRegistration { Resources = resources, Events = events };
        }
        public static bool ValidVolumeDb(float volumeDb) => DTMAPI.Internal.AudioVolume.Valid(volumeDb);
        public static float ToLinearVolume(float declarationDb, float offsetDb = 0) => DTMAPI.Internal.AudioVolume.Gain(declarationDb, offsetDb);
        private static bool Supported(AudioEvent e)
        {
            if (!Enum.IsDefined(typeof(AudioUse), e.Use) || !Enum.IsDefined(typeof(AudioRoute), e.Route) || e.ReplaceNativeEvent == null || e.NativeEvent == null) return false;
            if (e.Use == AudioUse.Music)
                return e.Route == AudioRoute.Music && !e.Loop && e.MinimumIntervalMs == 0 &&
                    (e.NativeEvent == "" || Key(e.NativeEvent) && e.ReplaceNativeEvent == "") &&
                    (e.ReplaceNativeEvent == "" || e.ReplaceNativeEvent.StartsWith("PLAY_BGM_", StringComparison.Ordinal) && e.ReplaceNativeEvent.Length > 9 && e.ReplaceNativeEvent.Skip(9).All(c => c >= '0' && c <= '9'));
            return e.Route != AudioRoute.Music && e.NativeEvent == "" &&
                (e.ReplaceNativeEvent == "" || Key(e.ReplaceNativeEvent) && !new[] { "STOP_", "PAUSE_", "RESUME_", "PLAY_BGM_" }.Any(p => e.ReplaceNativeEvent.StartsWith(p, StringComparison.Ordinal)));
        }
        private static bool Key(string value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 128 && value.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '.');
        private static bool Path(string value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 256 && value.IndexOfAny(new[] { '\\', ':', '\0' }) < 0 && !value.StartsWith("/", StringComparison.Ordinal) && value.Split('/').All(p => p.Length > 0 && p != "." && p != "..");
    }
}
