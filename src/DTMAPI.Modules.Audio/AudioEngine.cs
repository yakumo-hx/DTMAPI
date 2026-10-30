using DTMAPI.AudioContent.Preparation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using DTMAPI.Abstractions;
using DTMAPI.AudioContent;
using DTMAPI.AudioContent.Hosting;

namespace DTMAPI.Modules.Audio
{
    internal sealed partial class AudioEngine : IAudioBackend
    {
        internal const long Budget = 64 * 1024 * 1024;
        private readonly IOwnedSoundBackend driver;
        private readonly HashSet<uint> nativeIds;
        private readonly Action<string> log;
        private readonly List<Batch> registrations = new List<Batch>();
        private long sequence;
        private bool disposed;
        private bool retiringWorld;
        internal AudioEngine(IOwnedSoundBackend driver, HashSet<uint> nativeIds, Action<string> log)
        { this.driver = driver; this.nativeIds = nativeIds; this.log = log; }
        private sealed class Batch
        {
            internal string Owner = "", Diagnostic = "";
            internal int Order;
            internal long Generation, Reserved;
            internal bool Closed;
            internal Action Guard = () => { };
            internal AudioRegistration Input = null!;
            internal AudioPreparationState State;
            internal readonly Dictionary<string, IAudioSourceReference> Sources = new Dictionary<string, IAudioSourceReference>(StringComparer.Ordinal);
            internal readonly Dictionary<string, IAudioSourceReference> Files = new Dictionary<string, IAudioSourceReference>(StringComparer.Ordinal);
            internal readonly List<Media> Media = new List<Media>();
            internal readonly Dictionary<string, Event> Events = new Dictionary<string, Event>(StringComparer.Ordinal);
            internal readonly List<OwnedSoundBank.Voice> Voices = new List<OwnedSoundBank.Voice>();
            internal readonly HashSet<Media> RetiredMedia = new HashSet<Media>();
        }
        private sealed class Media
        {
            internal string Digest = "";
            internal IAudioSourceReference Source = null!;
            internal PcmWavInfo Info = null!;
            internal AudioResource Resource = null!;
            internal readonly List<Event> Events = new List<Event>();
            internal uint BankId, MediaId;
            internal OwnedSoundBank? Bank;
            internal readonly List<OwnedSoundBank> DependencyBanks = new List<OwnedSoundBank>();
            internal int Bytes;
            internal string Compatibility = "";
            internal int References;
            internal bool Streaming, Requested;
            internal DTMAPI.Abstractions.Internal.PackageResourceMetadata? Metadata;
            internal AudioPreparationState StreamState;
            internal StreamingCache.Job? Job;
        }
        private sealed class Event
        {
            internal AudioEvent Definition = null!;
            internal WwisePcmBank.Recipe Recipe = null!;
            internal Media Media = null!;
            internal long LastStarted = long.MinValue;
            internal int Declaration;
        }
        public IAudioRegistration Register(string owner, int loadIndex, AudioRegistration input, Func<string, IAudioSourceReference> source, Action guard)
        {
            if (disposed) throw new ObjectDisposedException(nameof(AudioEngine));
            guard();
            var batch = new Batch { Owner = owner, Order = loadIndex, Input = AudioValidation.CopyAndValidate(input), Guard = guard, Generation = ++sequence };
            foreach (var other in Owned(owner))
            {
                if (other.Order != loadIndex) throw new InvalidOperationException("AudioOwnerLoadOrderChanged");
            }
            foreach (var resource in batch.Input.Resources)
            {
                var reference = source(resource.Path);
                long length = Length(reference);
                bool prepared = reference is IAudioStreamingSourceReference streaming && streaming.StreamingResource != null;
                if (length <= 0) throw new InvalidDataException("AudioResourceEmpty: " + resource.Path);
                if ((resource.Stream || resource.Kind == AudioResourceKind.Bank) && !prepared) throw new InvalidDataException("AudioResourceAuthorityRequired");
                batch.Reserved = checked(batch.Reserved + (prepared ? 0 : 3 * length) + batch.Input.Events.Count(e => e.Resource == resource.Key) * 512L + 256);
                batch.Sources.Add(resource.Key, reference);
                foreach (string path in new[] { resource.Path }.Concat(resource.Media).Concat(resource.Dependencies))
                    if (!batch.Files.ContainsKey(path)) batch.Files.Add(path, source(path));
            }
            // Reserve before reading any media arrays or constructing any Bank.
            if (batch.Reserved + registrations.Sum(b => b.Reserved) > Budget)
                throw new InvalidDataException("AudioMemoryBudget: short audio reservation exceeds 64 MiB including construction headroom.");
            guard(); registrations.Add(batch);
            log("Registered owner=" + owner + "; order=" + loadIndex + "; generation=" + batch.Generation + "; reserved=" + batch.Reserved);
            return new RegistrationHandle(this, batch);
        }
        private IEnumerable<Batch> Owned(string owner) => registrations.Where(b => string.Equals(b.Owner, owner, StringComparison.OrdinalIgnoreCase));
        internal bool HasMusic => registrations.Any(b => !b.Closed && b.Input.Events.Any(e => e.Use == AudioUse.Music));
        private static long Length(IAudioSourceReference source)
        {
            var result = source.GetLength();
            if (result.Status != OwnerDataStatus.Found) throw new InvalidDataException("AudioResource" + result.Status + ": " + source.Key);
            return result.Value;
        }
        private static byte[] Read(IAudioSourceReference source)
        {
            var result = source.ReadBytes(PcmWav.MaxBytes);
            if (result.Status != OwnerDataStatus.Found || result.Value == null) throw new InvalidDataException("AudioResource" + result.Status + ": " + source.Key + "; " + result.Message);
            return result.Value;
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Tuple<PcmWavInfo, string> Describe(IAudioSourceReference source, bool music)
        {
            byte[] wav = Read(source);
            return Tuple.Create(music ? PcmWav.InspectMusic(wav) : PcmWav.Inspect(wav), WwisePcmBank.Digest(wav));
        }
        private void Prepare(Batch batch)
        {
            batch.Guard(); batch.State = AudioPreparationState.Preparing;
            var groups = new Dictionary<string, Media>(StringComparer.Ordinal);
            foreach (var resource in batch.Input.Resources)
            {
                bool prepared = batch.Sources[resource.Key] is IAudioStreamingSourceReference controlled && controlled.StreamingResource != null;
                var description = prepared ? null : Describe(batch.Sources[resource.Key], batch.Input.Events.Where(e => e.Resource == resource.Key).All(e => e.Use == AudioUse.Music));
                var info = description?.Item1; string digest = description?.Item2 ?? string.Join("\n", "prepared:", resource.Kind, resource.Stream, resource.Path, resource.Sha256, string.Join("\n", resource.Media), string.Join("\n", resource.Dependencies));
                if (!prepared && resource.Sha256.Length != 0 && digest != resource.Sha256) throw new InvalidDataException("AudioResourceHashMismatch: " + resource.Path);
                if (!groups.TryGetValue(digest, out var media))
                {
                    media = new Media { Digest = digest, Source = batch.Sources[resource.Key], Info = info!, Resource = resource, Streaming = prepared, Requested = prepared && !resource.Stream };
                    groups.Add(digest, media); batch.Media.Add(media);
                }
                foreach (var definition in batch.Input.Events.Where(e => e.Resource == resource.Key))
                {
                    var evt = new Event { Definition = definition, Media = media, Declaration = Array.IndexOf(batch.Input.Events, definition) };
                    batch.Events.Add(definition.Key, evt); media.Events.Add(evt);
                }
                // Only descriptors survive this pass. No WAV enters a registry.
            }
            foreach (var media in batch.Media)
            {
                batch.Guard();
                // Compatibility is a complete ordered recipe shape. Names and
                // per-voice volumeDb are lookup/instance concerns; they do not alter
                // the borrowed parent or sound structure. Never mutate a live Bank.
                // Only generated parentless Sounds changed layout. Native-parent
                // recipes and author-supplied Banks keep their existing cache key.
                string layout = media.Resource.Kind == AudioResourceKind.Source && media.Events.Any(e => Parent(e.Definition.Route) == 0) ? "bank2:" : "";
                media.Compatibility = layout + (media.Resource.Stream ? "stream:" : "short:") + string.Join(",", media.Events.Select(e => ((int)e.Definition.Use).ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + (int)e.Definition.Route + ":" + e.Definition.BankEvent + (e.Definition.Loop ? ":loop" : ":finite")));
                if (media.Streaming) { media.References = 1; continue; }
                var shared = Owned(batch.Owner).Where(b => b != batch && !b.Closed).SelectMany(b => b.Media).Distinct()
                    .FirstOrDefault(m => m.Bank?.Loaded == true && m.Digest == media.Digest && m.Compatibility == media.Compatibility);
                if (shared != null)
                {
                    for (int i = 0; i < media.Events.Count; i++)
                    { media.Events[i].Recipe = shared.Events[i].Recipe; media.Events[i].Media = shared; }
                    shared.References++;
                    continue;
                }
                string prefix = "dtmapi.audio." + WwisePcmBank.Digest(System.Text.Encoding.UTF8.GetBytes(batch.Owner)) + "." + media.Digest + "." + batch.Generation;
                media.BankId = WwisePcmBank.HashId(prefix + ".bank"); media.MediaId = WwisePcmBank.HashId(prefix + ".media");
                // Public keys are ordinal Unicode identities. Wwise's name
                // hashing folds ASCII case; encode the exact UTF-8 identity
                // first so it cannot merge accepted keys during conversion.
                foreach (var evt in media.Events) evt.Recipe = new WwisePcmBank.Recipe(prefix + ".event." + WwisePcmBank.Hex(System.Text.Encoding.UTF8.GetBytes(evt.Definition.Key)), Parent(evt.Definition.Route), Bus(evt.Definition.Route), evt.Definition.Loop);
                uint[] ids = new[] { media.BankId, media.MediaId }.Concat(media.Events.SelectMany(e => new[] { e.Recipe.Sound, e.Recipe.Action, e.Recipe.Event })).ToArray();
                if (ids.Any(nativeIds.Contains) || ids.Distinct().Count() != ids.Length || registrations.SelectMany(b => b.Media).Where(m => m != media && m.Bank != null).Any(m => ids.Contains(m.BankId) || ids.Contains(m.MediaId) || m.Events.Any(e => ids.Contains(e.Recipe.Sound) || ids.Contains(e.Recipe.Action) || ids.Contains(e.Recipe.Event))))
                    throw new InvalidDataException("AudioNativeIdCollision");
                media.Bytes = WwisePcmBank.Estimate(media.Info.DataLength, media.Events.Count, media.Events.Count(e => e.Recipe.Pitch != 0), media.Events.Count(e => e.Recipe.Loop), media.Events.Count(e => e.Recipe.Parent == 0));
                if (media.Bytes > batch.Reserved) throw new InvalidDataException("AudioMemoryBudgetChanged");
                media.References = 1; Load(media, batch);
            }
            // Replace provisional descriptors only after every shared reference
            // has been acquired. Failure cleanup also follows the event mapping.
            for (int i = 0; i < batch.Media.Count; i++)
                if (batch.Media[i].Events.Count != 0) batch.Media[i] = batch.Media[i].Events[0].Media;
            batch.Guard(); batch.State = AudioPreparationState.Ready; batch.Diagnostic = "";
            log("Ready owner=" + batch.Owner + "; media=" + batch.Media.Count + "; events=" + batch.Events.Count + "; managedMediaBytes=0; nativeTemporaryBytes=0");
        }
        internal static uint Parent(AudioRoute route) => route == AudioRoute.NativeAnimal ? WwisePcmBank.AnimalParent :
            Spatial(route) ? WwisePcmBank.MonsterParent : route == AudioRoute.Music ? WwisePcmBank.MusicParent : 0;
        internal static bool Spatial(AudioRoute route) => route == AudioRoute.NativeMonster || route == AudioRoute.NativeMonsterDeath || route == AudioRoute.NativeMonsterExplosion;
        internal static uint Bus(AudioRoute route) => route == AudioRoute.Ui ? WwisePcmBank.UiBus : route == AudioRoute.Ambience ? WwisePcmBank.AmbienceBus :
            route == AudioRoute.Actor || route == AudioRoute.NativeMonsterExplosion ? WwisePcmBank.ActorBus : route == AudioRoute.NativeMonsterDeath ? WwisePcmBank.MonsterDeadBus : 0;
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static byte[] Build(Media media)
        {
            byte[] wav = Read(media.Source);
            if (WwisePcmBank.Digest(wav) != media.Digest) throw new InvalidDataException("AudioSourceChanged: retry a new resource generation.");
            return WwisePcmBank.Build(wav, media.BankId, media.MediaId, media.Events.Select(e => e.Recipe).ToArray());
        }
        private void Load(Media media, Batch batch)
        {
            byte[] bytes = Build(media);
            batch.Guard();
            media.Bank = new OwnedSoundBank(driver, bytes, media.BankId);
            // Load independently of Post: Copy success drops the Bank array even
            // if the following voice start fails. Local inputs die on return.
            media.Bank.Load();
            batch.Guard();
        }
        internal void Tick(bool ready, string unavailable = "NativePreparing")
        {
            if (retiringWorld) CompleteWorldRetirement();
            foreach (var batch in registrations.ToArray())
            {
                if (batch.Closed) continue;
                if (batch.State == AudioPreparationState.Registered && ready)
                {
                    try { Prepare(batch); }
                    catch (Exception ex)
                    {
                        batch.State = AudioPreparationState.Failed; batch.Diagnostic = ex.GetBaseException().Message;
                        log("PrepareFailed owner=" + batch.Owner + "; reason=" + batch.Diagnostic);
                        try { ReleaseBanks(batch); } catch (Exception cleanup) { batch.State = AudioPreparationState.CleanupPending; batch.Diagnostic += "; " + cleanup.Message; }
                    }
                }
                else if (batch.State == AudioPreparationState.Registered) batch.Diagnostic = unavailable;
                foreach (var media in batch.Media) media.Bank?.Tick(false, preservePause: true);
                batch.Voices.RemoveAll(v => v.Ended && !batch.Media.Any(m => m.Bank?.Voices.Contains(v) == true));
                foreach (var retired in batch.RetiredMedia.ToArray())
                    if (!batch.Voices.Any(v => retired.Bank?.Voices.Contains(v) == true)) ReleaseMedia(batch, retired);
            }
            DriveStreaming(ready);
            foreach (var batch in registrations.Where(b => b.Closed).ToArray())
                try { Remove(batch); } catch (Exception ex) { batch.Diagnostic = ex.GetBaseException().Message; }
        }
        internal void RetireWorld()
        {
            retiringWorld = true;
            CompleteWorldRetirement();
        }
        private void CompleteWorldRetirement()
        {
            var errors = new List<Exception>();
            // A world boundary retires every voice. Release shared Banks once
            // rather than making all leases wait on one another's asynchronous
            // stop. Failed native cleanup keeps every descriptor and reservation.
            foreach (var media in registrations.SelectMany(b => b.Media).SelectMany(m => m.Events.Select(e => e.Media).Concat(new[] { m })).Distinct())
            {
                try { CancelPreparation(media); media.Bank?.Release(); foreach (var dependency in media.DependencyBanks.AsEnumerable().Reverse()) dependency.Release(); } catch (Exception ex) { errors.Add(ex); }
            }
            if (errors.Count != 0)
            {
                foreach (var batch in registrations) { batch.State = AudioPreparationState.CleanupPending; batch.Diagnostic = "AudioWorldCleanupPending"; }
                throw new AggregateException("Audio world retirement requires retry.", errors);
            }
            foreach (var batch in registrations.Where(b => !b.Closed))
            {
                foreach (var media in batch.Media.Distinct()) if (media.References > 0) media.References--;
                batch.Media.Clear(); batch.RetiredMedia.Clear(); batch.Events.Clear(); batch.Voices.Clear();
                batch.Generation = ++sequence; batch.State = AudioPreparationState.Registered; batch.Diagnostic = "";
            }
            retiringWorld = false;
        }
        public void RetryPreparation(string owner)
        {
            var failed = Owned(owner).Where(b => !b.Closed && (b.State == AudioPreparationState.Failed || b.State == AudioPreparationState.CleanupPending || b.Media.Any(m => m.Streaming && m.StreamState == AudioPreparationState.Failed))).ToArray();
            if (failed.Length == 0) throw new InvalidOperationException("AudioRetryRequiresFailedPreparation");
            foreach (var batch in failed)
            {
                batch.Guard(); ReleaseBanks(batch); batch.Media.Clear(); batch.Events.Clear();
                batch.Generation = ++sequence; batch.Diagnostic = ""; batch.State = AudioPreparationState.Registered;
            }
        }
        internal bool TryReplace(string nativeEvent, long now, out string diagnostic)
        {
            diagnostic = "Unregistered";
            var selected = registrations.Where(b => !b.Closed).SelectMany((b, registration) => b.Input.Events.Select((e, i) => new { Batch = b, Definition = e, Index = i, Registration = registration }))
                .Where(e => e.Definition.ReplaceNativeEvent == nativeEvent && e.Definition.Use == AudioUse.Effect)
                .OrderByDescending(e => e.Batch.Order).ThenByDescending(e => e.Registration).ThenByDescending(e => e.Index).FirstOrDefault();
            var domain = NativeReplacementScope.Current;
            if (domain != null && domain.Expected == nativeEvent &&
                (selected == null || domain.Sound.Order > selected.Batch.Order || domain.Sound.Order == selected.Batch.Order && domain.Declaration >= selected.Index))
                return domain.Sound.TryPost(now, domain.Started, out diagnostic);
            if (selected == null) return false;
            return TryPost(selected.Batch, selected.Definition.Key, now, out diagnostic);
        }
        private bool TryPost(Batch batch, string key, long now, out string diagnostic, Action<AudioPlayback>? started = null)
        {
            if (batch.Closed) { diagnostic = "Closed"; return false; }
            if (batch.State != AudioPreparationState.Ready) { diagnostic = "NotReady:" + batch.State + ":" + batch.Diagnostic; return false; }
            var evt = batch.Events[key];
            if (evt.Media.Streaming && !ReadyStreaming(batch, evt, out diagnostic)) return false;
            // Preserve the old 50 ms Manbo cooldown intent: a skipped request
            // leaves native audio available; no second backend or fake Post.
            if (evt.LastStarted != long.MinValue && now - evt.LastStarted < evt.Definition.MinimumIntervalMs) { diagnostic = "Cooldown"; return false; }
            try
            {
                var playback = PlayEvent(batch, evt, 0); started?.Invoke(playback); evt.LastStarted = now;
                diagnostic = "PostStarted; owner=" + batch.Owner + "; generation=" + batch.Generation + "; event=" + evt.Definition.Key;
                return true;
            }
            catch (Exception ex) { diagnostic = "PostFailed:" + ex.GetBaseException().Message; return false; }
        }
        private AudioPlayback Play(Batch batch, string eventKey, float volumeDb, AudioPosition? position = null)
        {
            if (!AudioValidation.ValidVolumeDb(volumeDb)) throw new ArgumentOutOfRangeException(nameof(volumeDb));
            if (batch.Closed || batch.State != AudioPreparationState.Ready || !batch.Events.ContainsKey(eventKey))
                throw new InvalidOperationException("AudioNotReadyOrUnknownEvent");
            var candidate = batch.Events[eventKey];
            if (candidate.Media.Streaming && !ReadyStreaming(batch, candidate, out string detail)) throw new InvalidOperationException("AudioNotReady:" + detail);
            return PlayEvent(batch, batch.Events[eventKey], volumeDb, position);
        }
        internal void StopSfx()
        {
            foreach (var batch in registrations)
                foreach (var voice in batch.Voices.Where(v => !v.IsMusic).ToArray()) voice.Stop();
        }
        private AudioPlayback PlayEvent(Batch batch, Event evt, float volumeDb, AudioPosition? position = null)
        {
            var voice = StartVoice(batch, evt, volumeDb, position);
            bool spatial = Spatial(evt.Definition.Route);
            return new AudioPlayback(() => { if (!voice.Ended) voice.Stop(); }, () => voice.Ended,
                paused => { if (!batch.Closed) { batch.Guard(); voice.Pause(paused); } },
                value => { if (!batch.Closed) { batch.Guard(); voice.SetGain(AudioValidation.ToLinearVolume(evt.Definition.VolumeDb, value)); } },
                spatial ? value => { if (!batch.Closed) { batch.Guard(); voice.SetPosition(value); } } : (Action<AudioPosition>?)null);
        }
        internal OwnedSoundBank.Voice? TryPlayMusic(string logical, Action<SoundEndReceipt> ended, Action invalidate, out string diagnostic)
        {
            diagnostic = "Unregistered";
            var selected = registrations.Where(b => !b.Closed).SelectMany((b, registration) => b.Input.Events.Select((e, i) => new { Batch = b, Definition = e, Index = i, Registration = registration }))
                .Where(e => e.Definition.Use == AudioUse.Music && e.Definition.ReplaceNativeEvent == logical)
                .OrderByDescending(e => e.Batch.Order).ThenByDescending(e => e.Registration).ThenByDescending(e => e.Index).FirstOrDefault();
            if (selected == null) return null;
            var batch = selected.Batch;
            if (batch.State != AudioPreparationState.Ready) { diagnostic = "NotReady:" + batch.State + ":" + batch.Diagnostic; return null; }
            try
            {
                if (batch.Events[selected.Definition.Key].Media.Streaming && !ReadyStreaming(batch, batch.Events[selected.Definition.Key], out diagnostic)) return null;
                var voice = StartVoice(batch, batch.Events[selected.Definition.Key], 0, null, ended, invalidate);
                diagnostic = "owner=" + batch.Owner + "; generation=" + batch.Generation + "; event=" + selected.Definition.Key;
                return voice;
            }
            catch (Exception ex) { diagnostic = "PostFailed:" + ex.GetBaseException().Message; return null; }
        }
        private OwnedSoundBank.Voice StartVoice(Batch batch, Event evt, float volumeDb, AudioPosition? position,
            Action<SoundEndReceipt>? ended = null, Action? invalidate = null)
        {
            if (batch.Closed) throw new ObjectDisposedException("AudioRegistrationClosed");
            batch.Guard();
            if (registrations.SelectMany(b => b.Media).Distinct().Sum(m => m.Bank?.VoiceCount ?? 0) >= 64) throw new InvalidOperationException("AudioVoiceBudget");
            bool spatial = Spatial(evt.Definition.Route);
            if (position.HasValue && !spatial) throw new InvalidOperationException("AudioPositionUnsupported");
            var voice = evt.Media.Bank!.Play(evt.Recipe.Event, AudioValidation.ToLinearVolume(evt.Definition.VolumeDb, volumeDb), false, spatial ? position ?? default(AudioPosition) : (AudioPosition?)null, ended, invalidate);
            voice.IsMusic = evt.Definition.Use == AudioUse.Music;
            batch.Voices.Add(voice);
            return voice;
        }
        public AudioSnapshot Snapshot(string owner)
            => Snapshot(owner, Owned(owner).ToArray());
        private AudioSnapshot Snapshot(string owner, Batch[] batches)
        {
            if (batches.Length == 0) return new AudioSnapshot { State = AudioPreparationState.Released,
                CacheBytes = streamingCache?.Bytes ?? 0, OwnerCacheBytes = streamingCache?.OwnerBytes(owner) ?? 0, FilePinCount = streamingCache?.Pins ?? 0,
                PreparationWorkerCount = streamingCache?.Workers ?? 0, CacheDirectory = streamingCache?.Root ?? "" };
            var media = batches.SelectMany(b => b.Media).SelectMany(m => m.Events.Select(e => e.Media).Concat(new[] { m })).Distinct().ToArray();
            var state = batches.Any(b => b.State == AudioPreparationState.CleanupPending) ? AudioPreparationState.CleanupPending :
                batches.Any(b => b.State == AudioPreparationState.Failed) ? AudioPreparationState.Failed :
                media.Any(m => m.Streaming && m.StreamState == AudioPreparationState.Failed) ? AudioPreparationState.Failed :
                media.Any(m => m.Streaming && m.StreamState == AudioPreparationState.Preparing) ? AudioPreparationState.Preparing :
                media.Any(m => m.Streaming && m.StreamState == AudioPreparationState.Registered) ? AudioPreparationState.Registered :
                batches.Any(b => b.State != AudioPreparationState.Ready) ? AudioPreparationState.Registered : AudioPreparationState.Ready;
            return new AudioSnapshot { State = state, Diagnostic = string.Join("; ", batches.Select(b => b.Diagnostic).Where(d => d.Length != 0)), RegistrationCount = batches.Length,
                CleanupPendingCount = batches.Count(b => b.State == AudioPreparationState.CleanupPending), ReservedBytes = batches.Sum(b => b.Reserved),
                MediaCount = media.Count(m => m.Bank?.Loaded == true), BankCount = media.Count(m => m.Bank?.Loaded == true), VoiceCount = media.Sum(m => m.Bank?.VoiceCount ?? 0),
                ManagedMediaBytes = media.Sum(m => m.Bank?.ManagedBytes ?? 0), LoadedBankBytes = media.Where(m => m.Bank?.Loaded == true).Sum(m => (long)m.Bytes),
                CacheBytes = streamingCache?.Bytes ?? 0, OwnerCacheBytes = streamingCache?.OwnerBytes(owner) ?? 0, FilePinCount = streamingCache?.Pins ?? 0,
                PreparationWorkerCount = streamingCache?.Workers ?? 0, CacheDirectory = streamingCache?.Root ?? "" };
        }
        public int CountOwnerResources(string owner) { var snapshot = Snapshot(owner); return snapshot.RegistrationCount + snapshot.BankCount + snapshot.VoiceCount; }
        private sealed class RegistrationHandle : IAudioRegistration
        {
            private readonly AudioEngine engine;
            private readonly Batch batch;
            internal RegistrationHandle(AudioEngine engine, Batch batch) { this.engine = engine; this.batch = batch; }
            public AudioRegistrationSnapshot GetSnapshot()
            {
                if (batch.State == AudioPreparationState.Released) return new AudioRegistrationSnapshot(AudioPreparationState.Released);
                var value = engine.Snapshot(batch.Owner, new[] { batch });
                return new AudioRegistrationSnapshot(value.State, value.Diagnostic);
            }
            public IAudioSound GetSound(string key)
            {
                batch.Guard();
                if (batch.Closed) throw new ObjectDisposedException("AudioRegistration");
                int declaration = Array.FindIndex(batch.Input.Events, e => e.Key == key);
                if (declaration < 0) throw new KeyNotFoundException("AudioSound: " + key);
                return new SoundHandle(engine, batch, key, declaration);
            }
            public void RetryPreparation()
            {
                batch.Guard();
                if (batch.Closed) throw new ObjectDisposedException("AudioRegistration");
                if (batch.State != AudioPreparationState.Failed && batch.State != AudioPreparationState.CleanupPending && !batch.Media.Any(m => m.Streaming && m.StreamState == AudioPreparationState.Failed))
                    throw new InvalidOperationException("AudioRetryRequiresFailedPreparation");
                engine.ReleaseBanks(batch); batch.Media.Clear(); batch.Events.Clear();
                batch.Generation = ++engine.sequence; batch.Diagnostic = ""; batch.State = AudioPreparationState.Registered;
            }
            public void Dispose() => engine.Remove(batch);
        }
        private sealed class SoundHandle : IAudioNativeSound
        {
            private readonly AudioEngine engine;
            private readonly Batch batch;
            private readonly string key;
            internal SoundHandle(AudioEngine engine, Batch batch, string key, int declaration)
            { this.engine = engine; this.batch = batch; this.key = key; Declaration = declaration; }
            public string Owner => batch.Owner;
            public int Order => batch.Order;
            public int Declaration { get; }
            public AudioRoute Route => batch.Input.Events[Declaration].Route;
            public bool Loop => batch.Input.Events[Declaration].Loop;
            public void Guard() { if (batch.Closed) throw new ObjectDisposedException("AudioSound"); batch.Guard(); }
            public AudioPlayback Play(float volumeDb = 0, AudioPosition? position = null) { Guard(); return engine.Play(batch, key, volumeDb, position); }
            public bool TryPost(long now, Action<AudioPlayback>? started, out string diagnostic) { Guard(); return engine.TryPost(batch, key, now, out diagnostic, started); }
        }
        private void ReleaseMedia(Batch batch, Media descriptor)
        {
            var media = descriptor.Events.Count == 0 ? descriptor : descriptor.Events[0].Media;
            CancelPreparation(media);
            if (media.References > 1)
            {
                foreach (var voice in batch.Voices.Where(v => media.Bank?.Voices.Contains(v) == true).ToArray()) voice.Stop();
                media.Bank?.Tick(false, preservePause: true);
                if (batch.Voices.Any(v => media.Bank?.Voices.Contains(v) == true)) throw new InvalidOperationException("AudioVoiceStopPending");
            }
            else { media.Bank?.Release(); foreach (var dependency in media.DependencyBanks.AsEnumerable().Reverse()) dependency.Release(); }
            if (media.References > 0) media.References--;
            batch.Media.Remove(descriptor); batch.RetiredMedia.Remove(descriptor);
        }
        private void ReleaseBanks(Batch batch)
        {
            var errors = new List<Exception>();
            // During failed preparation an acquired shared reference can still
            // sit behind its provisional descriptor. Always follow that mapping.
            foreach (var descriptor in batch.Media.ToArray())
            {
                var media = descriptor.Events.Count == 0 ? descriptor : descriptor.Events[0].Media;
                try
                {
                    ReleaseMedia(batch, descriptor);
                }
                catch (Exception ex) { errors.Add(ex); }
            }
            if (batch.Media.Count == 0) batch.Voices.Clear();
            if (errors.Count != 0) throw new AggregateException("Audio bank cleanup requires retry.", errors);
        }
        private void Remove(Batch batch)
        {
            if (batch.State == AudioPreparationState.Released) return;
            batch.Closed = true; batch.State = AudioPreparationState.CleanupPending;
            ReleaseBanks(batch);
            batch.State = AudioPreparationState.Released; registrations.Remove(batch);
            log("Released owner=" + batch.Owner + "; generation=" + batch.Generation + "; remaining=0");
        }
        public int RemoveOwner(string owner)
        {
            int count = CountOwnerResources(owner);
            var errors = new List<Exception>();
            foreach (var batch in Owned(owner).Reverse().ToArray()) try { Remove(batch); } catch (Exception ex) { errors.Add(ex); }
            if (errors.Count != 0) throw new AggregateException("Audio owner cleanup requires retry.", errors);
            return count;
        }
        public void Dispose()
        {
            var errors = new List<Exception>();
            foreach (var batch in registrations.ToArray()) try { Remove(batch); } catch (Exception ex) { errors.Add(ex); }
            if (errors.Count != 0) throw new AggregateException("Audio cleanup requires retry.", errors);
            disposed = true;
        }
    }
}
