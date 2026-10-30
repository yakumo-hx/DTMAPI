using System;
using System.Collections.Generic;
using System.Linq;
using DTMAPI.Abstractions;
using DTMAPI.AudioContent;
using DTMAPI.MonsterContent;
using DTMAPI.Modules.Audio;

static partial class Program
{
    static void CommonAudioChecks()
    {
        var owner = new Manifest("A");
        var driver = new CommonDriver(); using var engine = new AudioEngine(driver, new HashSet<uint>(), _ => { });
        string root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "monster-scoped-sound-" + Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(root); System.IO.File.WriteAllBytes(System.IO.Path.Combine(root, "hurt.wav"), SoundWave());
        using var audio = new AudioModuleFixture(() => engine, _ => root);
        engine.ConfigureStreaming(audio.StreamingDirectory);
        var backend = new ScopedBackend(); using var monsters = new MonsterModuleFixture(() => backend);
        var api = monsters.Api(owner);
        var declaration = new AudioRegistration {
            Resources = new[] { new AudioResource { Key = "wav", Path = "hurt.wav" } },
            Events = new[] {
                new AudioEvent { Key = "hurt", Resource = "wav", Route = AudioRoute.NativeMonster },
                new AudioEvent { Key = "dead", Resource = "wav", Route = AudioRoute.NativeMonsterDeath },
                new AudioEvent { Key = "explosion", Resource = "wav", Route = AudioRoute.NativeMonsterExplosion },
                new AudioEvent { Key = "hum", Resource = "wav", Route = AudioRoute.NativeMonster, Loop = true },
                new AudioEvent { Key = "wrong", Resource = "wav", Route = AudioRoute.Ui } } };
        var registration = audio.Api(owner).Register(declaration);
        PrepareCommonAudio(engine, owner.UniqueID);
        MonsterDefinition Input(string id, IAudioSound sound)
        { var value = Definition(id); value.Sounds = new MonsterSounds { Hurt = new MonsterSound { Sound = sound } }; return value; }
        var first = api.RegisterDefinition(Input("one", registration.GetSound("hurt"))).Value!;
        var second = api.RegisterDefinition(Input("two", registration.GetSound("hurt"))).Value!;
        var record = backend.Definitions.Find("A", "A:two")!;
        Check(record.Definition.Sounds!.Hurt!.RegisteredSound?.Invoke() != null, "Same-Owner Audio handle is bound before native body publication.");
        first.Dispose();
        Check(second.Snapshot.State == MonsterDefinitionState.Creatable && registration.GetSnapshot().State == AudioPreparationState.Ready, "Definition release preserves sibling and media registrations.");
        using var wrong = api.RegisterDefinition(Input("wrong", registration.GetSound("wrong"))).Value!;
        Check(wrong.Snapshot.State == MonsterDefinitionState.Creatable && backend.Definitions.Find("A", "A:wrong")!.Definition.Sounds!.Hurt!.RegisteredSound!() == null, "Wrong route affects sound only and leaves native fallback.");
        using var foreign = monsters.Api(new Manifest("B")).RegisterDefinition(Input("foreign", registration.GetSound("hurt"))).Value!;
        Check(foreign.Snapshot.State == MonsterDefinitionState.Creatable && backend.Definitions.Find("B", "B:foreign")!.Definition.Sounds!.Hurt!.RegisteredSound!() == null, "Cross-Owner sound handles never bind while the body remains valid.");
        using var looping = api.RegisterDefinition(Input("looping", registration.GetSound("hum"))).Value!;
        Check(backend.Definitions.Find("A", "A:looping")!.Definition.Sounds!.Hurt!.RegisteredSound!() == null, "Only Humming accepts a looping Audio recipe.");
        backend.FailSoundStop = true;
        try { registration.Dispose(); } catch (InvalidOperationException) { }
        Check(second.Snapshot.State == MonsterDefinitionState.Creatable && record.Definition.Sounds.Hurt.RegisteredSound!() == null && engine.CountOwnerResources("A") > 0, "Sound withdrawal closes selection, retains body and holds media until native stop completes.");
        backend.FailSoundStop = false; registration.Dispose(); audio.RetryPending();
        Check(backend.SoundStops >= 2 && second.Snapshot.State == MonsterDefinitionState.Creatable, "Sound cleanup retry does not withdraw skills or definition.");
        second.Dispose();
    }
    static void PrepareCommonAudio(AudioEngine engine, params string[] owners)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        do
        {
            engine.Tick(true);
            if (!owners.Any(o => engine.Snapshot(o).State is AudioPreparationState.Registered or AudioPreparationState.Preparing)) return;
            if (watch.ElapsedMilliseconds > 10000) throw new Exception("Audio preparation timeout: " + string.Join("; ", owners.Select(o => engine.Snapshot(o).Diagnostic)));
            System.Threading.Thread.Sleep(1);
        } while (true);
    }
    sealed class CommonDriver : IOwnedSoundBackend, IFileSoundBackend
    {
#if AUDIO_MONSTER_ACCEPTED_SFX
        internal readonly Dictionary<uint, Action> Callbacks = new();
#else
        internal readonly Dictionary<uint, Action<SoundEndReceipt>> Callbacks = new();
#endif
        internal readonly HashSet<uint> Stopped = new();
        private uint sequence;
        public void AddMediaPath(string root) { }
        public SoundBankReceipt LoadFile(string path, Action<SoundBankReceipt> completed)
        {
            var receipt = new SoundBankReceipt(Load(System.IO.File.ReadAllBytes(path)), true, "Success");
            completed(receipt); return receipt;
        }
        public uint Load(byte[] bytes) => BitConverter.ToUInt32(bytes, 12);
        public void Unload(uint bank) { }
        public object CreateEmitter() => new object();
        public void Register(object emitter) { }
        public void Gain(object emitter, float gain) { }
        public void Position(object emitter, AudioPosition position) { }
        public void Pause(uint id, bool paused) { }
#if AUDIO_MONSTER_ACCEPTED_SFX
        public uint Play(uint evt, object emitter, Action ended) { uint id = ++sequence; Callbacks[id] = ended; return id; }
        public void Stop(uint id) { Stopped.Add(id); }
        public void CancelCallback(Action ended) { foreach (uint id in Callbacks.Where(p => p.Value == ended).Select(p => p.Key).ToArray()) Callbacks.Remove(id); }
        internal void End(uint id) => Callbacks[id]();
#else
        public uint Play(uint evt, object emitter, Action<SoundEndReceipt> ended) { uint id = ++sequence; Callbacks[id] = ended; return id; }
        public void Stop(uint id, int transitionMs, int curve) { Stopped.Add(id); }
        public void CancelCallback(Action<SoundEndReceipt> ended) { foreach (uint id in Callbacks.Where(p => p.Value == ended).Select(p => p.Key).ToArray()) Callbacks.Remove(id); }
        internal void End(uint id) => Callbacks[id](new SoundEndReceipt(id, SoundEndReceipt.EndOfEvent));
#endif
        public void Unregister(object emitter) { }
        public void Destroy(object emitter) { }
    }
}
