using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DTMAPI.Abstractions;
using DTMAPI.Abstractions.Internal;
using DTMAPI.AudioContent.Hosting;

namespace DTMAPI.AudioContent.Hosting
{
    internal interface IAudioSourceReference
    {
        string Key { get; }
        OwnerDataResult<long> GetLength();
        OwnerDataResult<byte[]> ReadBytes(int maximumBytes);
    }
    internal interface IAudioStreamingSourceReference : IAudioSourceReference { PackageResourceReference? StreamingResource { get; } }
    internal interface IAudioStreamingBackend { void ConfigureStreaming(string cacheDirectory); }
    internal interface IAudioPreparationBackend { void ConfigureDecoder(string executable); }
    internal interface IAudioBackend : IDisposable
    {
        IAudioRegistration Register(string owner, int loadIndex, AudioRegistration input, Func<string, IAudioSourceReference> source, Action guard);
        AudioSnapshot Snapshot(string owner);
        int CountOwnerResources(string owner);
        int RemoveOwner(string owner);
    }
    internal interface IAudioDemandBackend { void SetDemand(bool active); }
    internal interface IAudioNativeSound : IAudioSound
    {
        string Owner { get; }
        int Order { get; }
        int Declaration { get; }
        AudioRoute Route { get; }
        bool Loop { get; }
        bool TryPost(long now, Action<AudioPlayback>? started, out string diagnostic);
        void Guard();
    }
    internal interface IAudioSoundBinding { IDisposable Bind(IDisposable consumer); }
    // Domain nomination expires with the original synchronous native call.
    internal static class NativeReplacementScope
    {
        [ThreadStatic] private static Candidate? current;
        internal static Candidate? Current => current;
        internal sealed class Candidate : IDisposable
        {
            private readonly Candidate? previous;
            private bool disposed;
            internal Candidate(IAudioNativeSound sound, string expected, int declaration, Action<AudioPlayback>? started)
            { Sound = sound; Expected = expected.ToUpperInvariant(); Declaration = declaration; Started = started; previous = current; current = this; }
            internal IAudioNativeSound Sound { get; }
            internal string Expected { get; }
            internal int Declaration { get; }
            internal Action<AudioPlayback>? Started { get; }
            public void Dispose() { if (!disposed) { current = previous; disposed = true; } }
        }
        private sealed class Empty : IDisposable
        {
            private readonly Candidate? previous = current;
            internal Empty() { current = null; }
            public void Dispose() { current = previous; }
        }
        internal static IDisposable Suppress() => new Empty();
        internal static IDisposable Enter(IAudioSound sound, string owner, string expected, int declaration, Action<AudioPlayback>? started = null)
        {
            if (!(sound is IAudioNativeSound native) || !string.Equals(native.Owner, owner, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("AudioSoundOwnerMismatch");
            native.Guard();
            return new Candidate(native, expected, declaration, started);
        }
    }
}
namespace DTMAPI.AudioContent
{
    internal sealed class AudioService : IRuntimeModule
    {
        private IRuntimeModuleContext context = null!;
        private readonly HashSet<string> owners = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private IAudioBackend? backend;
        private bool closed;
        public void Register(IRuntimeModuleContext value)
        {
            context = value;
            context.RegisterApi<IAudioContentApi>(owner => new Facade(this, owner));
            context.PublishStatus("NotRequested", "playback resources=0");
        }
        public void BeginClose() { closed = true; }
        public RuntimeModuleCleanup TryClose()
        {
            BeginClose();
            try { backend?.Dispose(); backend = null; owners.Clear(); return new RuntimeModuleCleanup(0); }
            catch (Exception ex) { return new RuntimeModuleCleanup(1, ex.GetBaseException().Message); }
        }
        private void Check()
        {
            if (!context.Runtime.IsMainThread) throw new InvalidOperationException("AudioWrongThread");
            if (closed) throw new ObjectDisposedException(nameof(AudioService));
        }
        private IAudioBackend Backend()
        {
            Check();
            if (backend != null) return backend;
            backend = context.CreateBackend<IAudioBackend>("DTMAPI.Modules.Audio", context);
            if (backend is IAudioPreparationBackend preparation) preparation.ConfigureDecoder(Path.Combine(context.ComponentDirectory, "audio-decoder", "ffmpeg.exe"));
            if (backend is IAudioStreamingBackend streaming) streaming.ConfigureStreaming(Path.Combine(context.CacheDirectory, "v1", "wwise2023.1.16-v150-pcm16"));
            return backend;
        }
        private void DrainIfIdle()
        {
            if (backend == null || owners.Any(owner => backend.CountOwnerResources(owner) != 0)) return;
            if (backend is IAudioDemandBackend demand) demand.SetDemand(false);
            context.PublishStatus("Idle", "registered demand=0; native callbacks may still be draining");
        }
        private sealed class SourceReference : IAudioStreamingSourceReference
        {
            internal SourceReference(string key, PackageResourceReference reference) { Key = key; StreamingResource = reference; }
            public string Key { get; }
            public PackageResourceReference StreamingResource { get; }
            public OwnerDataResult<long> GetLength() => new OwnerDataResult<long>(OwnerDataStatus.Found, StreamingResource.Inspect().Length);
            public OwnerDataResult<byte[]> ReadBytes(int maximumBytes)
            {
                var metadata = StreamingResource.Inspect();
                using (var stream = StreamingResource.Acquire(metadata, maximumBytes))
                {
                    var bytes = new byte[checked((int)metadata.Length)];
                    int offset = 0;
                    while (offset < bytes.Length)
                    {
                        int count = stream.Read(bytes, offset, Math.Min(65536, bytes.Length - offset));
                        if (count == 0) throw new EndOfStreamException(Key);
                        offset += count;
                    }
                    return new OwnerDataResult<byte[]>(OwnerDataStatus.Found, bytes);
                }
            }
        }
        private sealed class Facade : IAudioContentApi
        {
            private readonly AudioService service;
            private readonly IRuntimeModuleOwnerContext owner;
            internal Facade(AudioService service, IRuntimeModuleOwnerContext owner) { this.service = service; this.owner = owner; }
            public IAudioRegistration Register(AudioRegistration input)
            {
                service.Check(); owner.Guard();
                var copy = AudioValidation.CopyAndValidate(input);
                var sources = copy.Resources.SelectMany(r => new[] { r.Path }.Concat(r.Media).Concat(r.Dependencies)).Distinct(StringComparer.Ordinal)
                    .ToDictionary(path => path, path => new SourceReference(path, owner.Reference(path)), StringComparer.Ordinal);
                foreach (var source in sources.Values) if (source.GetLength().Value <= 0) throw new InvalidDataException("AudioResourceEmpty: " + source.Key);
                var scope = owner.CreateContribution("audio");
                scope.UseProvider(AudioModule.ProviderId);
                try
                {
                    var backend = service.Backend();
                    if (backend is IAudioDemandBackend demand) demand.SetDemand(true);
                    service.owners.Add(owner.Manifest.UniqueID);
                    var registration = scope.Activate(() => new BackendRegistration(service,
                        backend.Register(owner.Manifest.UniqueID, owner.OfficialOrder, copy, path => sources[path], scope.Guard)), _ => { });
                    service.context.PublishStatus("Active", "owner=" + owner.Manifest.UniqueID);
                    return new Registration(service, scope, registration);
                }
                catch
                {
                    scope.Close(); scope.TryClose(out _); service.DrainIfIdle(); throw;
                }
            }
        }
        private sealed class BackendRegistration : IAudioRegistration
        {
            private readonly AudioService service;
            private readonly IAudioRegistration value;
            private readonly List<IDisposable> consumers = new List<IDisposable>();
            private bool closed;
            internal BackendRegistration(AudioService service, IAudioRegistration value) { this.service = service; this.value = value; }
            public AudioRegistrationSnapshot GetSnapshot()
            {
                var snapshot = value.GetSnapshot();
                // Domain consumers close before native media. The native engine may
                // therefore still be Ready while this registration is already closing.
                return closed && snapshot.State != AudioPreparationState.Released
                    ? new AudioRegistrationSnapshot(AudioPreparationState.CleanupPending,
                        snapshot.Diagnostic.Length == 0 ? "AudioConsumerCleanupPending" : snapshot.Diagnostic)
                    : snapshot;
            }
            public IAudioSound GetSound(string key)
            {
                if (closed) throw new ObjectDisposedException("Audio registration");
                return new BoundSound(this, (IAudioNativeSound)value.GetSound(key));
            }
            public void RetryPreparation() => value.RetryPreparation();
            public void Dispose()
            {
                closed = true;
                while (consumers.Count != 0)
                {
                    var consumer = consumers[consumers.Count - 1];
                    consumer.Dispose(); consumers.Remove(consumer);
                }
                value.Dispose(); service.DrainIfIdle();
            }
            private sealed class Binding : IDisposable
            {
                private readonly BackendRegistration registration;
                private readonly IDisposable consumer;
                internal Binding(BackendRegistration registration, IDisposable consumer) { this.registration = registration; this.consumer = consumer; }
                public void Dispose() => registration.consumers.Remove(consumer);
            }
            private sealed class BoundSound : IAudioNativeSound, IAudioSoundBinding
            {
                private readonly BackendRegistration registration;
                private readonly IAudioNativeSound sound;
                internal BoundSound(BackendRegistration registration, IAudioNativeSound sound) { this.registration = registration; this.sound = sound; }
                public string Owner => sound.Owner;
                public int Order => sound.Order;
                public int Declaration => sound.Declaration;
                public AudioRoute Route => sound.Route;
                public bool Loop => sound.Loop;
                public void Guard() { if (registration.closed) throw new ObjectDisposedException("Audio sound"); sound.Guard(); }
                public AudioPlayback Play(float volumeDb = 0, AudioPosition? position = null) { Guard(); return sound.Play(volumeDb, position); }
                public bool TryPost(long now, Action<AudioPlayback>? started, out string diagnostic) { Guard(); return sound.TryPost(now, started, out diagnostic); }
                public IDisposable Bind(IDisposable consumer)
                { Guard(); registration.consumers.Add(consumer); return new Binding(registration, consumer); }
            }
        }
        private sealed class Registration : IAudioRegistration
        {
            private readonly AudioService service;
            private readonly IRuntimeContribution scope;
            private readonly IAudioRegistration value;
            internal Registration(AudioService service, IRuntimeContribution scope, IAudioRegistration value)
            { this.service = service; this.scope = scope; this.value = value; }
            public AudioRegistrationSnapshot GetSnapshot() => value.GetSnapshot();
            public IAudioSound GetSound(string key) { scope.Guard(); return value.GetSound(key); }
            public void RetryPreparation() { service.Check(); scope.Guard(); value.RetryPreparation(); }
            public void Dispose()
            {
                if (!service.context.Runtime.IsMainThread) throw new InvalidOperationException("AudioWrongThread");
                scope.Close();
                if (!scope.TryClose(out string failure)) throw new InvalidOperationException("AudioCleanupPending: " + failure);
                service.DrainIfIdle();
            }
        }
    }
}
