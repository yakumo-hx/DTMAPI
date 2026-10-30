using DTMAPI.AudioContent.Preparation;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using DTMAPI.AudioContent;
using DTMAPI.AudioContent.Hosting;

namespace DTMAPI.Modules.Audio
{
    internal sealed partial class AudioEngine
    {
        private StreamingCache? streamingCache;
        private string decoderExecutable = AudioDecoder.DefaultExecutable;
        internal void ConfigureDecoder(string executable)
        {
            if (string.IsNullOrWhiteSpace(executable)) return;
            decoderExecutable = executable;
            if (streamingCache != null) streamingCache.DecoderExecutable = executable;
        }
        private static DTMAPI.Abstractions.Internal.PackageResourceReference Controlled(IAudioSourceReference source) =>
            (source as IAudioStreamingSourceReference)?.StreamingResource ?? throw new InvalidDataException("AudioResourceAuthorityRequired: " + source.Key);
        private static string PreparedKey(Batch batch, Media media, DTMAPI.Abstractions.Internal.PackageResourceMetadata metadata) =>
            media.Resource.Kind == AudioResourceKind.Bank ? StreamingCache.BankKey(metadata,
                media.Resource.Media.Concat(media.Resource.Dependencies).Select(p => Controlled(batch.Files[p]).Inspect()), media.Resource.Sha256) : StreamingCache.Key(metadata, media.Resource.Sha256);
        private long AvailableMemory(Media current) => Budget - registrations.Sum(b => b.Reserved) -
            registrations.SelectMany(b => b.Media).Distinct().Where(m => m != current).Sum(m => (long)m.Bytes);
        private sealed class SharedPin
        {
            private readonly IDisposable pin;
            private int remaining;
            internal SharedPin(IDisposable pin, int count) { this.pin = pin; remaining = count; }
            internal IDisposable Take() => new AudioLease(() => { if (--remaining == 0) pin.Dispose(); });
        }
        internal void ConfigureStreaming(string directory)
        {
            if (streamingCache != null) { if (streamingCache.Root != Path.GetFullPath(directory)) throw new InvalidOperationException("AudioCacheRootChanged"); return; }
            streamingCache = new StreamingCache(directory, log);
            streamingCache.DecoderExecutable = decoderExecutable;
        }
        private bool ReadyStreaming(Batch batch, Event evt, out string diagnostic)
        {
            batch.Guard();
            var source = ((IAudioStreamingSourceReference)batch.Sources[evt.Definition.Resource]).StreamingResource!;
            var metadata = source.Inspect(); string key = PreparedKey(batch, evt.Media, metadata);
            var media = evt.Media;
            if (media.Metadata != null && media.Digest != key)
            {
                // Keep the old immutable file/Bank generation until this batch's
                // old voices finish. A new demand receives its own pin and charge.
                var next = new Media { Source = batch.Sources[evt.Definition.Resource], Resource = media.Resource, Streaming = true, References = 1, Compatibility = media.Compatibility };
                foreach (var old in batch.Events.Values.Where(e => ReferenceEquals(e.Media, media)).ToArray())
                {
                    var replacement = new Event { Definition = old.Definition, Declaration = old.Declaration, Media = next };
                    next.Events.Add(replacement); batch.Events[replacement.Definition.Key] = replacement;
                }
                batch.RetiredMedia.Add(media); batch.Media.Add(next); media = next;
            }
            if (media.StreamState == AudioPreparationState.Ready && media.Bank?.Loaded == true)
            { diagnostic = "Ready"; return true; }
            if (media.StreamState == AudioPreparationState.Failed)
            { diagnostic = "NotReady:Failed:" + batch.Diagnostic; return false; }
            if (streamingCache == null) throw new InvalidOperationException("AudioStreamingCacheUnavailable");
            media.Requested = true; diagnostic = "NotReady:" + media.StreamState;
            return false;
        }
        private void CancelPreparation(Media media)
        {
            var job = media.Job;
            if (job == null) return;
            job.Revoke();
            if (!job.Done) throw new InvalidOperationException("AudioWorkerCleanupPending");
            try { streamingCache!.Finish(job, false); }
            catch (OperationCanceledException) { }
            finally { if (job.Finished) { job.Pin.Dispose(); media.Job = null; } }
        }
        private void DriveStreaming(bool ready)
        {
            foreach (var batch in registrations.ToArray())
            foreach (var media in batch.Media.Where(m => m.Streaming).Distinct().ToArray())
            {
                if (media.Job != null && media.Job.Done)
                {
                    var job = media.Job;
                    try
                    {
                        bool accept = !batch.Closed && !retiringWorld && !batch.RetiredMedia.Contains(media);
                        if (accept) batch.Guard();
                        streamingCache!.Finish(job, accept);
                        // The native ticket owns this already-acquired file pin
                        // before LoadBank, including an early native callback.
                        batch.Guard();
                        if (job.ResidentBytes > AvailableMemory(media)) throw new InvalidDataException("AudioMemoryBudget: prepared Bank exceeds current available memory.");
                        ((IFileSoundBackend)driver).AddMediaPath(streamingCache.Root);
                        var pins = new SharedPin(job.Pin, 1 + job.DependencyBanks.Length);
                        foreach (string path in job.DependencyBanks) media.DependencyBanks.Add(new OwnedSoundBank(driver, path, pins.Take()));
                        media.Bank = new OwnedSoundBank(driver, job.BankPath, pins.Take());
                        media.Bytes = checked((int)job.ResidentBytes);
                        media.Job = null;
                        foreach (var dependency in media.DependencyBanks) dependency.Load();
                        if (media.DependencyBanks.Count == 0) media.Bank.Load();
                    }
                    catch (Exception ex)
                    {
                        if (!job.Finished)
                            try { streamingCache!.Finish(job, false); } catch (Exception cleanup) { log("StreamDiscard reason=" + cleanup.GetBaseException().Message); }
                        if (media.Bank == null) job.Pin.Dispose();
                        media.Job = null; media.StreamState = AudioPreparationState.Failed;
                        batch.Diagnostic = "AudioResource " + media.Resource.Path + ": " + ex.GetBaseException().Message;
                        log("StreamPrepareFailed owner=" + batch.Owner + "; reason=" + batch.Diagnostic);
                    }
                }
                if (!batch.Closed && !batch.RetiredMedia.Contains(media) && media.Bank != null && media.StreamState != AudioPreparationState.Ready && media.StreamState != AudioPreparationState.Failed)
                {
                    foreach (var dependency in media.DependencyBanks) dependency.Tick(false, preservePause: true);
                    string dependencyError = media.DependencyBanks.Select(b => b.LoadError).FirstOrDefault(e => e.Length != 0) ?? "";
                    if (dependencyError.Length != 0)
                    { media.StreamState = AudioPreparationState.Failed; batch.Diagnostic = dependencyError; continue; }
                    if (media.DependencyBanks.Any(b => !b.Loaded)) continue;
                    media.Bank.Load();
                    media.Bank.Tick(false, preservePause: true);
                    if (media.Bank.LoadError.Length != 0)
                    {
                        media.StreamState = AudioPreparationState.Failed; batch.Diagnostic = media.Bank.LoadError;
                        media.Bank.Release(); media.Bank = null;
                        log("StreamNativeFailed owner=" + batch.Owner + "; reason=" + batch.Diagnostic);
                    }
                    else if (media.Bank.Loaded)
                    {
                        try { batch.Guard(); }
                        catch { media.Bank.Release(); media.Bank = null; media.StreamState = AudioPreparationState.Failed; throw; }
                        media.StreamState = AudioPreparationState.Ready;
                        if (!batch.Media.Any(m => m.StreamState == AudioPreparationState.Failed)) batch.Diagnostic = "";
                        log("StreamReady owner=" + batch.Owner + "; key=" + media.Digest + "; bankBytes=" + media.Bytes + "; request=none");
                    }
                }
            }
            if (!ready || streamingCache == null || streamingCache.Busy || retiringWorld) return;
            foreach (var batch in registrations.Where(b => !b.Closed && b.State == AudioPreparationState.Ready).ToArray())
            {
                var media = batch.Media.FirstOrDefault(m => m.Streaming && m.Requested && m.StreamState == AudioPreparationState.Registered && !batch.RetiredMedia.Contains(m));
                if (media == null) continue;
                try
                {
                    batch.Guard();
                    var source = ((IAudioStreamingSourceReference)media.Source).StreamingResource!;
                    var metadata = source.Inspect(); string key = PreparedKey(batch, media, metadata);
                    media.Digest = key; media.Metadata = metadata;
                    var shared = Owned(batch.Owner).SelectMany(b => b.Media).Distinct().FirstOrDefault(m =>
                        m != media && m.Streaming && m.Digest == key && m.Compatibility == media.Compatibility && m.Bank != null && !m.Bank.Released);
                    if (shared != null)
                    {
                        // A pending or retiring ticket already owns this exact
                        // file Bank. Do not submit it twice and accidentally
                        // turn Wwise's Bank reference count into two owners of
                        // the same voice structures. Share only after Ready,
                        // or wait for the revoked ticket's precise compensation.
                        if (!shared.Bank!.Loaded || !Owned(batch.Owner).Any(b => !b.Closed && b.Media.Contains(shared))) continue;
                        for (int i = 0; i < media.Events.Count; i++) { media.Events[i].Recipe = shared.Events[i].Recipe; media.Events[i].Media = shared; }
                        shared.References++; batch.Media[batch.Media.IndexOf(media)] = shared; continue;
                    }
                    string prefix = "dtmapi.audio.prepared." + key + "." + media.Compatibility;
                    media.BankId = WwisePcmBank.HashId(prefix + ".bank"); media.MediaId = StreamingCache.MediaId(key);
                    for (int i = 0; i < media.Events.Count; i++)
                    {
                        var definition = media.Events[i].Definition;
                        media.Events[i].Recipe = new WwisePcmBank.Recipe(prefix + "." + i, Parent(definition.Route), Bus(definition.Route), definition.Loop);
                        if (media.Resource.Kind == AudioResourceKind.Bank) media.Events[i].Recipe.Event = WwisePcmBank.HashId(definition.BankEvent);
                    }
                    uint[] ids = new[] { media.BankId, media.MediaId }.Concat(media.Events.SelectMany(e => new[] { e.Recipe.Sound, e.Recipe.Action, e.Recipe.Event })).ToArray();
                    if (media.Resource.Kind != AudioResourceKind.Bank && (ids.Any(id => id == 0 || id == uint.MaxValue || nativeIds.Contains(id)) || ids.Distinct().Count() != ids.Length ||
                        registrations.SelectMany(b => b.Media).Distinct().Where(m => m != media && m.Bank != null).Any(m =>
                            ids.Any(id => id != media.MediaId && (id == m.BankId || id == m.MediaId || m.Events.Any(e => e.Recipe != null && (e.Recipe.Sound == id || e.Recipe.Action == id || e.Recipe.Event == id)))))))
                        throw new InvalidDataException("AudioNativeIdCollision");
                    media.StreamState = AudioPreparationState.Preparing;
                    media.Job = media.Resource.Kind == AudioResourceKind.Bank ?
                        streamingCache.BeginBank(batch.Owner, source, metadata,
                            media.Resource.Media.Select(p => Controlled(batch.Files[p])).ToArray(), media.Resource.Dependencies.Select(p => Controlled(batch.Files[p])).ToArray(), AvailableMemory(media), media.Resource.Sha256) :
                        streamingCache.Begin(batch.Owner, source, metadata, media.BankId, media.Events.Select(e => e.Recipe).ToArray(), media.Resource.Stream, AvailableMemory(media), media.Resource.Sha256);
                }
                catch (Exception ex)
                {
                    media.StreamState = AudioPreparationState.Failed; batch.Diagnostic = "AudioResource " + media.Resource.Path + ": " + ex.GetBaseException().Message;
                    log("StreamPrepareFailed owner=" + batch.Owner + "; reason=" + batch.Diagnostic);
                }
                break; // One preparation worker for the whole Audio Provider.
            }
        }
    }
}
