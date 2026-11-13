using DTMAPI.AudioContent.Preparation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using DTMAPI.Abstractions;
using DTMAPI.Abstractions.Internal;
using DTMAPI.AudioContent;
using DTMAPI.AudioContent.Hosting;
using DTMAPI.Core.Runtime;
using DTMAPI.Modules.Audio;

internal static class StreamingChecks
{
    // Real Core path/lease code and real Audio preparation; only native I/O and
    // main-thread owner state are doubles. Runtime acceptance is separate.
    internal static PackageResourceReference Reference(string root, Action guard, string key, string identity)
    {
        var type = typeof(DtmApiRuntime).Assembly.GetType("DTMAPI.Core.Services.ModResourceService", true)!;
        var service = Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { root, guard }, null)!;
        try { return (PackageResourceReference)type.GetMethod("BackgroundReference", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(service, new object[] { key, identity })!; }
        catch (TargetInvocationException ex) { throw ex.InnerException!; }
    }
    private static void Wav(string path, int dataBytes, int paddingBytes = 0)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var s = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
        using var w = new BinaryWriter(s, Encoding.UTF8, true);
        w.Write(Encoding.ASCII.GetBytes("RIFF")); w.Write(checked(dataBytes + 36 + (paddingBytes == 0 ? 0 : paddingBytes + 8))); w.Write(Encoding.ASCII.GetBytes("WAVEfmt ")); w.Write(16);
        w.Write((ushort)1); w.Write((ushort)2); w.Write(48000); w.Write(192000); w.Write((ushort)4); w.Write((ushort)16);
        if (paddingBytes != 0) { w.Write(Encoding.ASCII.GetBytes("JUNK")); w.Write(paddingBytes); s.Seek(paddingBytes, SeekOrigin.Current); }
        w.Write(Encoding.ASCII.GetBytes("data")); w.Write(dataBytes);
        var block = new byte[65536]; for (int i = 0; i < block.Length; i++) block[i] = (byte)(i % 127);
        for (int left = dataBytes; left > 0;) { int count = Math.Min(left, block.Length); s.Write(block, 0, count); left -= count; }
    }
    private static void Until(Action tick, Func<bool> ready)
    {
        var clock = Stopwatch.StartNew();
        while (!ready()) { tick(); if (clock.ElapsedMilliseconds > 10000) throw new Exception("Streaming test timeout"); Thread.Sleep(1); }
    }
    internal static object Run(string root, Action<bool, string> check, Action<Action, string> throws)
    {
        check(new[] { typeof(PackageResourceMetadata), typeof(PackageResourceReference), typeof(PackageResourceReadLease) }.All(t =>
            t.IsSealed && t.GetConstructors().Length == 0),
            "Only Core can construct these sealed resource types.");
        var logs = new List<string>(); string sourceRoot = Path.Combine(root, "source");
        Wav(Path.Combine(sourceRoot, "long.wav"), 17280000);
        int main = Environment.CurrentManagedThreadId; bool pack = true, host = true;
        void Guard() { if (Environment.CurrentManagedThreadId != main) throw new InvalidOperationException("WrongThread"); if (!pack || !host) throw new ObjectDisposedException("PackAndHost"); }
        var reference = Reference(sourceRoot, Guard, "long.wav", "RealCoreTest/Owner/SourceGeneration1");
        var input = AudioContentReader.Parse("{\"resources\":[{\"key\":\"long\",\"path\":\"long.wav\",\"stream\":true}],\"events\":[{\"key\":\"music\",\"resource\":\"long\",\"use\":\"music\",\"replaceNativeEvent\":\"PLAY_BGM_23\"}]}");
        check(input.Resources[0].Stream, "Ordinary JSON selects the same streaming resource contract.");
        var bad = AudioValidation.CopyAndValidate(input); bad.Events[0].Use = AudioUse.Effect; bad.Events[0].ReplaceNativeEvent = "";
        throws(() => AudioValidation.CopyAndValidate(bad), "Streaming cannot widen short SFX limits.");
        throws(() => Reference(sourceRoot, Guard, "../long.wav", "Bad"), "Real Core rejects traversal before issuing a worker capability.");
        var metadata = reference.Inspect();
        using (var lease = reference.Acquire(metadata, metadata.Length))
        {
            check(lease.Length == 17280044 && !lease.CanWrite, "Large file uses a bounded read-only handle rather than ReadBytes.");
            throws(() => lease.Read(new byte[65537], 0, 65537), "Worker reads are capped at 64 KiB.");
        }
        string cacheRoot = Path.Combine(root, "production-cache");
        var driver = new FileDriver(); var engine = new AudioEngine(driver, new HashSet<uint>(), logs.Add); engine.ConfigureStreaming(cacheRoot);
        var source = new DiskSource(reference);
        var registration = engine.Register("Actual.Owner", 2, input, _ => source, Guard); engine.Tick(true);
        check(driver.Submitted.Count == 0 && engine.Snapshot("Actual.Owner").PreparationWorkerCount == 0 && engine.Snapshot("Actual.Owner").FilePinCount == 0,
            "Registration holds directory metadata; it does not prepare the library.");
        check(engine.TryPlayMusic("PLAY_BGM_23", _ => { }, () => { }, out _) == null, "Cold request returns native fallback without claiming a voice.");
        Until(() => { engine.Tick(true); if (engine.Snapshot("Actual.Owner").State == AudioPreparationState.Failed) throw new Exception(string.Join("\n", logs)); }, () => driver.Submitted.Count == 1);
        check(driver.Posts == 0 && engine.Snapshot("Actual.Owner").FilePinCount == 1 && engine.Snapshot("Actual.Owner").State == AudioPreparationState.Preparing,
            "A submitted native load remains Preparing and pinned before the real callback.");
        var siblingInput = AudioValidation.CopyAndValidate(input);
        siblingInput.Resources[0].Key = "sibling"; siblingInput.Events[0].Key = "sibling"; siblingInput.Events[0].Resource = "sibling"; siblingInput.Events[0].ReplaceNativeEvent = "PLAY_BGM_24";
        var sibling = engine.Register("Actual.Owner", 2, siblingInput, _ => source, Guard); engine.Tick(true);
        engine.TryPlayMusic("PLAY_BGM_24", _ => { }, () => { }, out _); engine.Tick(true);
        check(driver.Submitted.Count == 1 && engine.Snapshot("Actual.Owner").FilePinCount == 1,
            "A second contribution waits on the exact pending file ticket rather than double-loading the same native Bank.");
        driver.Complete(); engine.Tick(true);
        check(engine.Snapshot("Actual.Owner").State == AudioPreparationState.Ready && driver.Posts == 0, "Ready does not restart or late-post a native fallback request.");
        sibling.Dispose();
        check(driver.Unloaded.Count == 0 && engine.Snapshot("Actual.Owner").BankCount == 1 && engine.Snapshot("Actual.Owner").FilePinCount == 1,
            "The newly Ready contribution shares the original Bank; releasing it retains its live sibling's sole native pin.");
        var voice = engine.TryPlayMusic("PLAY_BGM_23", _ => { }, () => { }, out _)!;
        check(voice != null && driver.Posts == 1 && engine.Snapshot("Actual.Owner").LoadedBankBytes < 4096 && source.Reads == 0, "A later real request posts the small Bank; long media never calls whole-array ReadBytes.");
        long coldCharge = engine.Snapshot("Actual.Owner").CacheBytes;
        var recordTimes = Directory.GetFiles(cacheRoot, "*", SearchOption.AllDirectories).Where(p => p.EndsWith(".wem") || p.EndsWith(".bnk")).ToDictionary(p => p, File.GetLastWriteTimeUtc);
        string legacyMusicPrefix = "dtmapi.audio.prepared." + StreamingCache.Key(metadata) + ".stream:9:0::finite";
        check(Directory.GetFiles(cacheRoot, "*.bnk", SearchOption.AllDirectories).Single().EndsWith(WwisePcmBank.HashId(legacyMusicPrefix + ".bank") + ".bnk"),
            "Unchanged native Music layout retains its pre-migration Bank identity.");
        registration.Dispose(); check(voice!.Ended && driver.Unloaded.Single() == driver.Submitted.Single().Id, "Release unloads the returned/callback native ID, independently of authored BKHD ID.");
        check(engine.Snapshot("Actual.Owner").CacheBytes == coldCharge && engine.Snapshot("Actual.Owner").OwnerCacheBytes == coldCharge && engine.Snapshot("Actual.Owner").FilePinCount == 0,
            "Released registrations report persistent physical bytes separately from zero active file pins.");
        engine.Dispose();
        check(Directory.GetFiles(cacheRoot, "*.wem", SearchOption.AllDirectories).Length == 1, "Normal exit retains completed persistent media.");
        var hotDriver = new FileDriver { Early = true }; using var hotEngine = new AudioEngine(hotDriver, new HashSet<uint>(), logs.Add); hotEngine.ConfigureStreaming(cacheRoot);
        var retuned = AudioValidation.CopyAndValidate(input); retuned.Events[0].VolumeDb = -24;
        using var hotRegistration = hotEngine.Register("Actual.Owner", 2, retuned, _ => source, Guard); hotEngine.Tick(true);
        hotEngine.TryPlayMusic("PLAY_BGM_23", _ => { }, () => { }, out _);
        Until(() => hotEngine.Tick(true), () => hotEngine.Snapshot("Actual.Owner").State == AudioPreparationState.Ready);
        check(recordTimes.All(p => File.GetLastWriteTimeUtc(p.Key) == p.Value) && hotEngine.Snapshot("Actual.Owner").CacheBytes == coldCharge && logs.Any(l => l.Contains("cache=Hot")),
            "Changing only dB reuses persisted WEM and Bank without rewriting either file.");
        hotRegistration.Dispose();

        // Host loss after preparation starts, before main publishes.
        var revokedDriver = new FileDriver(); using var revokedEngine = new AudioEngine(revokedDriver, new HashSet<uint>(), logs.Add);
        revokedEngine.ConfigureStreaming(Path.Combine(root, "revoked-cache"));
        var revoked = revokedEngine.Register("Revoked.Owner", 2, input, _ => source, Guard); revokedEngine.Tick(true);
        revokedEngine.TryPlayMusic("PLAY_BGM_23", _ => { }, () => { }, out _); revokedEngine.Tick(true); host = false;
        Until(() => revokedEngine.Tick(true), () => revokedEngine.Snapshot("Revoked.Owner").State == AudioPreparationState.Failed);
        check(revokedDriver.Submitted.Count == 0 && !Directory.GetFiles(Path.Combine(root, "revoked-cache"), "complete", SearchOption.AllDirectories).Any(),
            "Revoked Host cannot publish or load a late worker result.");
        revoked.Dispose(); host = true;

        var lateDriver = new FileDriver(); using var lateEngine = new AudioEngine(lateDriver, new HashSet<uint>(), logs.Add);
        lateEngine.ConfigureStreaming(Path.Combine(root, "late-cache"));
        var late = lateEngine.Register("Late.Owner", 2, input, _ => source, Guard); lateEngine.Tick(true);
        lateEngine.TryPlayMusic("PLAY_BGM_23", _ => { }, () => { }, out _);
        Until(() => lateEngine.Tick(true), () => lateDriver.Submitted.Count == 1);
        throws(() => late.Dispose(), "Withdrawal retains pending native I/O responsibility.");
        check(lateEngine.Snapshot("Late.Owner").FilePinCount == 1 && lateDriver.Unloaded.Count == 0, "Pending load keeps its file pin and exact ticket.");
        lateDriver.FailUnload = true; lateDriver.Complete(); lateEngine.Tick(true);
        check(lateEngine.Snapshot("Late.Owner").FilePinCount == 1 && lateEngine.CountOwnerResources("Late.Owner") > 0, "Failed late-success unload retains its native ticket and physical pin for retry.");
        lateEngine.Tick(true); late.Dispose();
        check(lateDriver.Posts == 0 && lateDriver.Unloaded.SequenceEqual(lateDriver.Submitted.Select(s => s.Id)) && lateEngine.CountOwnerResources("Late.Owner") == 0,
            "Late success after withdrawal is precisely unloaded, never posted.");

        // Cross the retired disk admission thresholds without a large media
        // workload: retained bytes and WAV padding are not decoded audio.
        string sharedSource = Path.Combine(root, "shared-sources"), sharedRoot = Path.Combine(root, "shared-cache");
        foreach (string key in new[] { "a", "b" }) Wav(Path.Combine(sharedSource, key + ".wav"), 60000);
        Directory.CreateDirectory(sharedRoot);
        string retainedPath = Path.Combine(sharedRoot, "retained-file");
        using (var retained = File.Create(retainedPath)) retained.SetLength(512L * 1024 * 1024 + 1);
        var shared = new StreamingCache(sharedRoot, logs.Add);
        check(shared.Bytes > 512L * 1024 * 1024 && shared.OwnerBytes("Owner.A") == 0,
            "Unattributed retained files count once in total usage, not against every owner's usage.");
        StreamingCache.Job Start(string owner, string file)
        {
            var r = Reference(sharedSource, Guard, file + ".wav", owner + "/source1"); var m = r.Inspect(); string k = StreamingCache.Key(m);
            uint id = WwisePcmBank.HashId(k + ".bank");
            return shared.Begin(owner, r, m, id, new[] { new WwisePcmBank.Recipe(k, WwisePcmBank.MusicParent) });
        }
        void Complete(StreamingCache.Job job) { Until(() => { }, () => job.Done); shared.Finish(job, true); }
        var a = Start("Owner.A", "a"); Complete(a);
        check(shared.OwnerBytes("Owner.A") > 60000 && shared.OwnerBytes("Owner.A") < 65000, "Preparation succeeds above the retired global quota and accounts for media, Bank, record and marker.");
        long oneRecipe = shared.OwnerBytes("Owner.A");
        DateTime decodedTime = File.GetLastWriteTimeUtc(a.Entry.Wem);
        var sameReference = Reference(sharedSource, Guard, "a.wav", "Owner.A/source1"); var sameMetadata = sameReference.Inspect(); string sameKey = StreamingCache.Key(sameMetadata);
        var extraRecipes = shared.Begin("Owner.A", sameReference, sameMetadata, WwisePcmBank.HashId(sameKey + ".two-recipes.bank"),
            new[] { new WwisePcmBank.Recipe(sameKey + ".one", WwisePcmBank.MusicParent), new WwisePcmBank.Recipe(sameKey + ".two", WwisePcmBank.MusicParent) });
        Complete(extraRecipes); extraRecipes.Pin.Dispose();
        check(File.GetLastWriteTimeUtc(a.Entry.Wem) == decodedTime, "A new Bank layout/recipe identity reuses completed decoded media without rewriting it.");
        check(shared.OwnerBytes("Owner.A") - oneRecipe == 264 && a.Entry.Pins == 1,
            "Two more event recipes add only the actual small Bank, sharing one WEM and retaining the existing native file pin.");
        a.Pin.Dispose();
        var b = Start("Owner.A", "b"); Complete(b); b.Pin.Dispose();
        check(File.Exists(a.Entry.Wem) && File.Exists(b.Entry.Wem) && File.Exists(retainedPath),
            "New preparation retains completed unused media and unrelated files instead of evicting them for capacity.");
        File.Delete(retainedPath);

        string largePath = Path.Combine(sharedSource, "large.wav");
        Wav(largePath, 60000, 256 * 1024 * 1024);
        long largeSourceBytes = new FileInfo(largePath).Length;
        var largeInput = AudioValidation.CopyAndValidate(input); largeInput.Resources[0].Path = "large.wav";
        var largeDriver = new FileDriver { Early = true }; var largeEngine = new AudioEngine(largeDriver, new HashSet<uint>(), logs.Add);
        string largeCache = Path.Combine(root, "large-source-cache"); largeEngine.ConfigureStreaming(largeCache);
        using (var service = new AudioModuleFixture(() => largeEngine, _ => sharedSource))
        {
            var api = service.Api(new Manifest("Large.Owner"), Guard);
            using var largeRegistration = api.Register(largeInput); largeEngine.Tick(true);
            check(largeSourceBytes > 256L * 1024 * 1024 && largeDriver.Submitted.Count == 0,
                "Public API and Engine accept a source above the retired 256 MiB quota without eagerly preparing it.");
            largeEngine.TryPlayMusic("PLAY_BGM_23", _ => { }, () => { }, out _);
            Until(() => largeEngine.Tick(true), () => largeRegistration.GetSnapshot().State is AudioPreparationState.Ready or AudioPreparationState.Failed);
            check(largeRegistration.GetSnapshot().State == AudioPreparationState.Ready, "Large-source preparation failed: " + string.Join("\n", logs.TakeLast(6)));
            string wem = Directory.GetFiles(Path.Combine(largeCache, "Media"), "*.wem").Single();
            check(largeDriver.Submitted.Count == 1 && new FileInfo(wem).Length == 60052,
                "Core source lease and cache preparation accept that source, skipping padding and writing only its PCM data.");
        }
        File.Delete(largePath);

        string partRoot = Path.Combine(root, "part-cache"); var parts = new StreamingCache(partRoot, logs.Add);
        var partMeta = reference.Inspect(); string partKey = StreamingCache.Key(partMeta);
        var partJob = parts.Begin("Part.Owner", reference, partMeta, 12345, new[] { new WwisePcmBank.Recipe("part", WwisePcmBank.MusicParent) });
        Until(() => { }, () => partJob.Done);
        using (var locked = new FileStream(partJob.Entry.Wem + ".part", FileMode.Open, FileAccess.Read, FileShare.None))
        {
            throws(() => parts.Finish(partJob, false), "Cancelled preparation never publishes its completed worker file.");
            partJob.Pin.Dispose();
            check(parts.Bytes >= 17280052 && !File.Exists(partJob.Entry.Marker), "Undeletable part is retained in real physical accounting without a completion marker.");
        }
        var recovered = new StreamingCache(partRoot, logs.Add);
        check(recovered.Bytes == parts.Bytes && recovered.OwnerBytes("Part.Owner") >= 17280052, "Restart reconstructs incomplete physical charges and attribution.");
        File.WriteAllLines(Path.Combine(root, "streaming-trace.log"), logs);
        return new { scope = "real Core resource lease/files, real streaming preparation/accounting, native doubles; no game acceptance", coldCharge,
            longSourceBytes = metadata.Length, paddedSourceBytes = largeSourceBytes, readsThroughReadBytes = source.Reads, sourceRoot, cacheRoot };
    }
    internal sealed class DiskSource : IAudioStreamingSourceReference
    {
        internal DiskSource(PackageResourceReference reference) { StreamingResource = reference; }
        internal int Reads;
        public PackageResourceReference StreamingResource { get; }
        public string Key => "long.wav";
        public OwnerDataResult<long> GetLength() => new(OwnerDataStatus.Found, StreamingResource.Inspect().Length);
        public OwnerDataResult<byte[]> ReadBytes(int maximumBytes) { Reads++; throw new Exception("StreamingMustNotReadBytes"); }
    }
    internal sealed class FileDriver : IOwnedSoundBackend, IFileSoundBackend
    {
        private readonly Fake voices = new();
        internal readonly List<(uint Id, Action<SoundBankReceipt> Callback)> Submitted = new();
        internal readonly List<uint> Unloaded = new();
        internal bool Early;
        internal bool FailUnload;
        internal int Posts;
        public void AddMediaPath(string root) { if (!Directory.Exists(Path.Combine(root, "Media"))) throw new Exception("Missing resolver media directory"); }
        public SoundBankReceipt LoadFile(string path, Action<SoundBankReceipt> completed)
        {
            if (!File.Exists(path)) throw new Exception("Missing Bank");
            uint id = 900000u + (uint)Submitted.Count; Submitted.Add((id, completed));
            if (Early) completed(new SoundBankReceipt(id, true, "Success"));
            return new SoundBankReceipt(id, true, "Submitted");
        }
        internal void Complete() { foreach (var t in Submitted) t.Callback(new SoundBankReceipt(t.Id, true, "Success")); }
        public uint Load(byte[] bytes) => throw new Exception("FileRouteOnly");
        public void Unload(uint bankId) { if (!Submitted.Any(t => t.Id == bankId)) throw new Exception("WrongNativeBankId"); if (FailUnload) { FailUnload = false; throw new IOException("InjectedUnloadFailure"); } Unloaded.Add(bankId); }
        public object CreateEmitter() => voices.CreateEmitter();
        public void Register(object e) => voices.Register(e);
        public void Gain(object e, float g) => voices.Gain(e, g);
        public void Position(object e, AudioPosition p) => voices.Position(e, p);
        public uint Play(uint id, object e, Action<SoundEndReceipt> end) { Posts++; return voices.Play(id, e, end); }
        public void Pause(uint id, bool value) => voices.Pause(id, value);
        public void Stop(uint id, int ms, int curve) => voices.Stop(id, ms, curve);
        public void CancelCallback(Action<SoundEndReceipt> end) => voices.CancelCallback(end);
        public void Unregister(object e) => voices.Unregister(e);
        public void Destroy(object e) => voices.Destroy(e);
    }
}
