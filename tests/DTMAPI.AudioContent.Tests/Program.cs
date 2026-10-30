using DTMAPI.AudioContent.Preparation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using DTMAPI.Abstractions;
using DTMAPI.AudioContent;
using DTMAPI.AudioContent.Authoring;
using DTMAPI.AudioContent.Hosting;
using DTMAPI.Modules.Audio;

int checks = 0;
void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; }
void Throws(Action action, string message) { try { action(); } catch { checks++; return; } throw new Exception(message); }
byte[] Wav(int channels = 1, int rate = 8000, double seconds = .1)
{
    int frames = (int)(rate * seconds), size = frames * channels * 2;
    var bytes = new byte[size + 44]; using var s = new MemoryStream(bytes, true); using var w = new BinaryWriter(s);
    w.Write(Encoding.ASCII.GetBytes("RIFF")); w.Write(size + 36); w.Write(Encoding.ASCII.GetBytes("WAVEfmt ")); w.Write(16);
    w.Write((ushort)1); w.Write((ushort)channels); w.Write(rate); w.Write(rate * channels * 2); w.Write((ushort)(channels * 2)); w.Write((ushort)16);
    w.Write(Encoding.ASCII.GetBytes("data")); w.Write(size);
    for (int i = 0; i < frames; i++) for (int c = 0; c < channels; c++) w.Write((short)(2800 * Math.Sin(2 * Math.PI * (c == 0 ? 440 : 660) * i / rate)));
    return bytes;
}
if (args.Contains("--package-read"))
{
    string packageRoot = Path.GetFullPath(args[Array.IndexOf(args, "--package-read") + 1]);
    string manifestPath = Path.Combine(packageRoot, "Content/DTMAPI/manifest.json");
    using var manifestDocument = JsonDocument.Parse(File.ReadAllText(manifestPath)); var manifest = manifestDocument.RootElement;
    string Field(string name) => manifest.GetProperty(name).GetString()!;
    var core = typeof(DTMAPI.Core.Runtime.DtmApiRuntime).Assembly;
    var bundleType = core.GetType("DTMAPI.Internal.Authoring.PackageDependencyBundle", true)!;
    var read = bundleType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).Single(m => m.Name == "Read" && m.GetParameters().Length == 11);
    var verify = core.GetType("DTMAPI.Core.Manifesting.DependencyPackageVerifier", true)!.GetMethod("VerifyCurrentPackage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
    object?[] parameters = { packageRoot, manifestPath, Field("UniqueID"), Field("Version"), Field("Type"), Field("CodeModKind"), Path.Combine(packageRoot, Field("EntryDll")), Field("MinimumDTMApiVersion"), null, null, false };
    var first = read.Invoke(null, parameters)!; verify.Invoke(null, new[] { packageRoot, first });
    parameters[8] = parameters[9]; var reused = read.Invoke(null, parameters)!; verify.Invoke(null, new[] { packageRoot, reused });
    Check(!ReferenceEquals(first, reused), "Startup reads return separate package snapshots.");
    Console.WriteLine("Audio provider actual package passed initial and reused Core preflight: " + checks); return;
}
if (args.Contains("--runtime-startup"))
{
    ModuleChecks.Run(Check, Throws, () => Wav());
    Console.WriteLine("Runtime module checks passed: " + checks); return;
}
if (args.Contains("--preparation"))
{
    string preparationOutput = Path.GetFullPath(args[Array.IndexOf(args, "--output") + 1]);
    var result = PreparationChecks.Run(preparationOutput, args[Array.IndexOf(args, "--decoder") + 1], args[Array.IndexOf(args, "--inputs") + 1], Wav(2, 48000, .6), Check, Throws);
    File.WriteAllText(Path.Combine(preparationOutput, "preparation.json"), JsonSerializer.Serialize(new { checks, result }, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine("Preparation checks passed: " + checks); return;
}
if (args.Contains("--music-events"))
{
    var result = MusicEventChecks.Run(Check, Throws, () => Wav());
    int at = Array.IndexOf(args, "--output");
    if (at >= 0) { Directory.CreateDirectory(args[at + 1]); File.WriteAllText(Path.Combine(args[at + 1], "music-events.json"), JsonSerializer.Serialize(new { checks, result }, new JsonSerializerOptions { WriteIndented = true })); }
    Console.WriteLine("Music event checks passed: " + checks); return;
}
if (args.Contains("--music-withdrawal"))
{
    var result = MusicWithdrawalChecks.Run(Check, Throws, () => Wav());
    var summary = new { scope = "direct music withdrawal and current native playback gate; fake I/O/native state, no game loop claim", checks, result };
    int at = Array.IndexOf(args, "--output");
    if (at >= 0) { Directory.CreateDirectory(args[at + 1]); File.WriteAllText(Path.Combine(args[at + 1], "withdrawal.json"), JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true })); }
    Console.WriteLine(JsonSerializer.Serialize(summary)); return;
}
if (args.Contains("--streaming"))
{
    int at = Array.IndexOf(args, "--output");
    string streamOutput = at >= 0 ? Path.GetFullPath(args[at + 1]) : Path.Combine(Path.GetTempPath(), "dtmapi-streaming-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(streamOutput);
    var result = StreamingChecks.Run(streamOutput, Check, Throws);
    File.WriteAllText(Path.Combine(streamOutput, "streaming.json"), JsonSerializer.Serialize(new { checks, result }, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine("Streaming checks passed: " + checks); return;
}
AudioRegistration Input(int events = 1) => new() { Resources = new[] { new AudioResource { Key = "media", Path = "sound.wav" } }, Events = Enumerable.Range(0, events).Select(i => new AudioEvent { Key = "event" + i, Resource = "media", VolumeDb = i == 0 ? -12.0412f : -2.4987747f, ReplaceNativeEvent = "PLAY_RESOURCE_PAPER_BOX" }).ToArray() };
OwnedSoundBank NewBank(Fake driver)
{
    byte[] wav = Wav(); var recipe = new WwisePcmBank.Recipe("test.sound");
    return new OwnedSoundBank(driver, WwisePcmBank.Build(wav, 11, 12, new[] { recipe }), 11);
}
uint EventId = WwisePcmBank.HashId("test.sound.event");
{
    var driver = new Fake(); var bank = NewBank(driver);
    var a = bank.Play(EventId, .25f, false); var b = bank.Play(EventId, .75f, false);
    Check(bank.ManagedBytes == 0 && driver.Loads == 1, "Copy success drops the complete construction Bank and reuses loaded media.");
    Check(a.PlayingId != b.PlayingId && driver.Gains[a.Emitter!] == .25f && driver.Gains[b.Emitter!] == .75f, "Two requests have independent IDs, emitters and gains.");
    a.Stop(); Check(bank.VoiceCount == 2 && driver.Stopped.Contains(a.PlayingId), "Stop request retains asynchronous native responsibility.");
    Throws(a.Finish, "Unacknowledged stop cannot discard its callback/emitter.");
    driver.End(a.PlayingId); bank.Tick(false);
    Check(bank.VoiceCount == 1 && driver.Emitters.Count == 1, "End acknowledgement releases only its own voice.");
    driver.End(b.PlayingId); bank.Tick(false); bank.Release();
    Check(driver.Empty && !bank.Loaded && bank.ManagedBytes == 0, "Natural end then withdrawal releases all handles.");
    a.Callback(); b.Callback(); Check(driver.Empty, "Late completion cannot resurrect a retired request.");
}
foreach (string stage in new[] { "load", "create", "register", "gain", "play" })
{
    var driver = new Fake { Fail = stage }; var bank = NewBank(driver);
    Throws(() => bank.Play(EventId, 1, false), "Failed " + stage + " must be observable.");
    bank.Release(); Check(driver.Empty, "Failed " + stage + " retains enough responsibility for complete compensation.");
}
foreach (string stage in new[] { "unload", "cancel", "unregister", "destroy" })
{
    var driver = new Fake(); var bank = NewBank(driver); bank.Play(EventId, 1, false); driver.Fail = stage;
    Throws(bank.Release, "Cleanup failure " + stage + " must not be hidden.");
    Check(bank.VoiceCount == 1 && driver.Emitters.Count == 1, "Failed cleanup retains its handle.");
    if (stage == "unload") Check(bank.Loaded && driver.Callbacks.Count == 1, "A failed unload cannot free a live callback.");
    bank.Release(); bank.Release(); Check(driver.Empty && driver.Unloads == 1, "Retry " + stage + " does not repeat successful unload.");
}
{
    var driver = new Fake { ZeroPlay = true }; var bank = NewBank(driver);
    Throws(() => bank.Play(EventId, 1, false), "Invalid Playing ID is a failed start.");
    bank.Release(); Check(driver.Empty, "Failed Post callback package is cancelled.");
}
{
    var driver = new Fake(); var bank = NewBank(driver); var a = bank.Play(EventId, .5f, false); var b = bank.Play(EventId, .7f, false);
    driver.Fail = "stop"; Throws(a.Stop, "Failed Stop preserves pending responsibility."); bank.Tick(false);
    Check(driver.Stopped.Contains(a.PlayingId) && !driver.Stopped.Contains(b.PlayingId), "Retry targets the exact request.");
    driver.End(a.PlayingId); driver.End(b.PlayingId); bank.Tick(false); bank.Release(); Check(driver.Empty, "Stop failure retires cleanly.");
}
var logs = new List<string>();
{
    var driver = new Fake(); using var engine = new AudioEngine(driver, new HashSet<uint>(), logs.Add);
    var source = new Source(Wav());
    var input = Input(2);
    using var a = engine.Register("Z.Earlier", 10, input, _ => source, () => { });
    Check(source.Reads == 0 && driver.Loads == 0, "Registration holds descriptors only; no media is read until preparation.");
    Check(!engine.TryReplace("PLAY_RESOURCE_PAPER_BOX", 0, out var reason) && reason.StartsWith("NotReady"), "Interaction before Ready fails open.");
    engine.Tick(false);
    Check(engine.Snapshot("Z.Earlier").Diagnostic.Length > 0, "Engine readiness has an observable waiting diagnostic.");
    engine.Tick(true);
    var snapshot = engine.Snapshot("Z.Earlier");
    Check(snapshot.Diagnostic.Length == 0, "Successful preparation clears the transient readiness diagnostic.");
    Check(snapshot.State == AudioPreparationState.Ready && snapshot.MediaCount == 1 && snapshot.BankCount == 1 && snapshot.ManagedMediaBytes == 0, "One owner media serves two separate recipes without retained WAV/Bank arrays.");
    var v1 = a.GetSound("event0").Play(0f); var v2 = a.GetSound("event1").Play(0f);
    Check(driver.Events.Distinct().Count() == 2 && driver.Gains.Values.Any(g => Math.Abs(g - .25f) < 1e-6f) && driver.Gains.Values.Any(g => Math.Abs(g - .75f) < 1e-6f), "Recipes and instance gains remain independent while sharing DATA.");
    foreach (uint id in driver.Callbacks.Keys.ToArray()) driver.End(id); engine.Tick(true);
    for (int i = 0; i < 100; i++) { a.GetSound("event0").Play(0f); driver.End(driver.Callbacks.Keys.Single()); engine.Tick(true); }
    Check(driver.Loads == 1 && engine.Snapshot("Z.Earlier").VoiceCount == 0 && driver.Emitters.Count == 0, "100 deterministic starts/ends reuse one loaded Bank and return voices to zero.");
    var later = Input(); later.Events[0].VolumeDb = -0.915149811f;
    using var b = engine.Register("A.Later", 20, later, _ => source, () => { }); engine.Tick(true);
    Check(engine.TryReplace("PLAY_RESOURCE_PAPER_BOX", 1000, out reason) && reason.Contains("owner=A.Later"), "Core rank wins independently of owner lexicographic order.");
    b.Dispose();
    Check(engine.TryReplace("PLAY_RESOURCE_PAPER_BOX", 2000, out reason) && reason.Contains("owner=Z.Earlier") && reason.Contains("event=event1"), "Withdrawing the winner recomputes the surviving owner and its last declaration.");
    Check(!engine.TryReplace("STOP_ALL", 2001, out _) && !engine.TryReplace("PLAY_RESOURCE_PAPER_BOX_BROKEN", 2001, out _), "STOP and adjacent paper-box broken events are isolated.");
    a.Dispose(); Check(driver.Empty && engine.CountOwnerResources("Z.Earlier") == 0, "Last batch withdrawal releases shared media after its voices.");
}
{
    var driver = new Fake(); using var engine = new AudioEngine(driver, new HashSet<uint>(), logs.Add);
    var source = new Source(Wav()); var input = Input(2);
    input.Resources = new[] { input.Resources[0], new AudioResource { Key = "same", Path = "same.wav" } }; input.Events[1].Resource = "same";
    using var lease = engine.Register("Same.Owner", 1, input, _ => source, () => { }); engine.Tick(true);
    Check(driver.Loads == 1 && engine.Snapshot("Same.Owner").MediaCount == 1, "Content identity, rather than file name or resource key, shares the media body.");
}
{
    var source = new Source(Wav()) { Length = PcmWav.MaxBytes + 1L }; var driver = new Fake(); using var engine = new AudioEngine(driver, new HashSet<uint>(), logs.Add);
    Throws(() => engine.Register("A", 1, Input(), _ => source, () => { }), "Oversized input rejects before allocation.");
    Check(source.Reads == 0 && driver.Loads == 0, "Rejected metadata never reads WAV arrays or loads native memory.");
}
{
    var source = new Source(Wav()); var driver = new Fake(); using var engine = new AudioEngine(driver, new HashSet<uint>(), logs.Add);
    bool alive = true;
    using var lease = engine.Register("Guard", 1, Input(), _ => source, () => { if (!alive) throw new ObjectDisposedException("owner"); });
    alive = false; engine.Tick(true);
    Check(engine.Snapshot("Guard").State == AudioPreparationState.Failed && source.Reads == 0 && driver.Loads == 0, "Closing the owner before preparation cannot publish late resources.");
}
{
    var source = new Source(Wav()) { Corrupt = true }; var driver = new Fake(); using var engine = new AudioEngine(driver, new HashSet<uint>(), logs.Add);
    using var lease = engine.Register("Retry", 1, Input(), _ => source, () => { }); engine.Tick(true);
    Check(engine.Snapshot("Retry").State == AudioPreparationState.Failed && !engine.TryReplace("PLAY_RESOURCE_PAPER_BOX", 0, out _), "Bad WAV is diagnosed and preserves native fallback.");
    int reads = source.Reads; for (int i = 0; i < 20; i++) engine.Tick(true); Check(source.Reads == reads, "A failed resource does not retry media I/O every frame.");
    source.Corrupt = false; lease.RetryPreparation(); engine.Tick(true);
    Check(engine.Snapshot("Retry").State == AudioPreparationState.Ready, "Explicit retry prepares a fresh valid generation.");
    lease.GetSound("event0").Play(0f); driver.Fail = "unload";
    Throws(lease.Dispose, "Failed owner withdrawal remains retryable.");
    Check(engine.CountOwnerResources("Retry") > 0 && !engine.TryReplace("PLAY_RESOURCE_PAPER_BOX", 1, out _), "A closing batch is removed from arbitration but retains cleanup resources.");
    lease.Dispose(); Check(driver.Empty, "Retry finishes the exact failed withdrawal.");
}
ModuleChecks.Run(Check, Throws, () => Wav());
string validJson = "{\"resources\":[{\"key\":\"manbo\",\"path\":\"assets/manbo.wav\"}],\"events\":[{\"key\":\"box\",\"resource\":\"manbo\",\"use\":\"effect\",\"replaceNativeEvent\":\"PLAY_RESOURCE_PAPER_BOX\",\"volumeDb\":1,\"minimumIntervalMs\":50}]}";
Check(AudioContentReader.Parse(validJson).Events[0].MinimumIntervalMs == 50, "JSON and direct definitions use the same validated model.");
foreach (string bad in new[] { validJson.Replace("\"volumeDb\":1", "\"volumeDb\":1,\"priority\":99"), validJson.Replace("PLAY_RESOURCE_PAPER_BOX", "STOP_ALL"), validJson.Replace("assets/manbo.wav", "../outside.wav"), validJson.Replace("\"volumeDb\":1", "\"volumeDb\":1,\"volumeDb\":2"), validJson.Replace("\"volumeDb\":1", "\"volumeDb\":1e300") })
    Throws(() => AudioContentReader.Parse(bad), "Malformed, forged or unsupported declaration fails schema admission.");
foreach (string[] keys in new[] { new[] { "轻声", "重声" }, new[] { "Box", "box" }, new[] { "quiet", "loud" } })
{
    string json = "{\"resources\":[{\"key\":\"shared\",\"path\":\"sound.wav\"}],\"events\":[" +
        string.Join(",", keys.Select((key, i) => "{\"key\":\"" + key + "\",\"resource\":\"shared\",\"use\":\"effect\",\"volumeDb\":" + (i == 0 ? "-12.0412" : "-2.4987747") + ",\"replaceNativeEvent\":\"PLAY_RESOURCE_PAPER_BOX\"}")) + "]}";
    var input = AudioContentReader.Parse(json);
    Check(input.Events.Select(e => e.Key).SequenceEqual(keys), "JSON preserves distinct accepted Unicode/case event identities.");
    var driver = new Fake(); using var engine = new AudioEngine(driver, new HashSet<uint>(), logs.Add);
    using var lease = engine.Register("Key.Owner", 1, input, _ => new Source(Wav()), () => { });
    engine.Tick(true); var snapshot = engine.Snapshot("Key.Owner");
    Check(snapshot.State == AudioPreparationState.Ready && snapshot.BankCount == 1 && snapshot.MediaCount == 1 && driver.Loads == 1, "Accepted key pairs prepare together and share one media/Bank.");
    var first = lease.GetSound(keys[0]).Play(0f); var second = lease.GetSound(keys[1]).Play(0f);
    Check(driver.Events.Distinct().Count() == 2 && driver.Gains.Values.Any(g => Math.Abs(g - .25f) < 1e-6f) && driver.Gains.Values.Any(g => Math.Abs(g - .75f) < 1e-6f), "Accepted key pairs address distinct recipes and gains.");
    first.Stop(); driver.End(driver.Stopped.Single()); engine.Tick(true);
    Check(first.HasEnded && !second.HasEnded && engine.Snapshot("Key.Owner").VoiceCount == 1, "Stopping the first accepted key cannot end the second key's voice.");
    foreach (uint id in driver.Callbacks.Keys.ToArray()) driver.End(id); engine.Tick(true);
    Check(engine.TryReplace("PLAY_RESOURCE_PAPER_BOX", 1000, out var selected) && selected.Contains("event=" + keys[1]), "Ordinal public lookup and last declaration still select the exact accepted key.");
    if (keys[0] == "Box") Throws(() => lease.GetSound("BOX").Play(0f), "Public event lookup remains ordinal rather than inheriting native case folding.");
    foreach (uint id in driver.Callbacks.Keys.ToArray()) driver.End(id); engine.Tick(true); lease.Dispose();
    Check(driver.Empty, "Accepted key pair withdraws its shared media and all independent voices.");
}
E21Checks.Run(Check, Throws, () => Wav());
VolumeChecks.Run(Check, Throws, () => Wav());
MonsterChecks.Run(Check, Throws, () => Wav());
MusicChecks.Run(Check, Throws, () => Wav());
int dailyAt = Array.IndexOf(args, "--daily-pack");
if (dailyAt >= 0) GenericSfxChecks.Run(Check, Throws, () => Wav(), args[dailyAt + 1]);
string? animalFixture = Environment.GetEnvironmentVariable("DTMAPI_E21_ANIMAL_ROOT");
if (!string.IsNullOrEmpty(animalFixture)) E21Checks.RunAnimalFixture(animalFixture, Check);
// Exact old builder vs new builder, same near-limit legal short PCM. This is an
// offline .NET allocation comparison, not Unity/Wwise native pool attribution.
byte[] sample = Wav(2, 48000, 10);
var oldIds = DTMAPI.Modules.Monsters.WwisePcmBank.Ids("Measure.Owner:short", sample);
_ = DTMAPI.Modules.Monsters.WwisePcmBank.Build(Wav(), oldIds);
_ = WwisePcmBank.Build(Wav(), 11, 12, new[] { new WwisePcmBank.Recipe("measure") });
long before = GC.GetAllocatedBytesForCurrentThread();
byte[] oldBank = DTMAPI.Modules.Monsters.WwisePcmBank.Build(sample, oldIds);
long oldAllocated = GC.GetAllocatedBytesForCurrentThread() - before;
before = GC.GetAllocatedBytesForCurrentThread();
byte[] newBank = WwisePcmBank.Build(sample, 11, 12, new[] { new WwisePcmBank.Recipe("measure") });
long newAllocated = GC.GetAllocatedBytesForCurrentThread() - before;
Check(newAllocated < oldAllocated && newBank.Length < oldBank.Length + 1024, "Same-input construction allocation must decrease without dropping PCM.");
uint U32(byte[] bytes, int offset) => BitConverter.ToUInt32(bytes, offset);
int hirc = 80 + (int)U32(newBank, 76);
Check(U32(newBank, 56) == 12 && U32(newBank, hirc + 8) == 3, "One DIDX media and independent Sound/Action/Event structures.");
Check(U32(newBank, hirc + 17 + 18 + 8) == WwisePcmBank.PaperBoxParent, "Paper-box recipe borrows its actual native parent rather than monster bus defaults.");
var process = Process.GetCurrentProcess(); process.Refresh();
var report = new { scope = "offline .NET 8 builder allocation; native load/output/process peak still requires game evidence", sampleBytes = sample.Length, oldBankBytes = oldBank.Length, newBankBytes = newBank.Length, oldAllocatedBytes = oldAllocated, newAllocatedBytes = newAllocated, oldAllocatedMiB = oldAllocated / 1048576.0, newAllocatedMiB = newAllocated / 1048576.0, oldModuleReadyHeldBytes = sample.Length + oldBank.Length, newModuleReadyHeldBytes = 0, managedHeapBytes = GC.GetTotalMemory(false), privateBytes = process.PrivateMemorySize64, workingSet = process.WorkingSet64, checks };
int outputAt = Array.IndexOf(args, "--output");
string? output = outputAt >= 0 && outputAt + 1 < args.Length ? Path.GetFullPath(args[outputAt + 1]) : null;
if (output != null)
{
    Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output, "offline-memory.json"), JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
    File.WriteAllLines(Path.Combine(output, "trace.log"), logs);
    File.WriteAllBytes(Path.Combine(output, "sample.wav"), sample); File.WriteAllBytes(Path.Combine(output, "sample.bnk"), newBank);
}
Console.WriteLine(JsonSerializer.Serialize(report)); Console.WriteLine("Common audio checks passed: " + checks);

sealed class Source : IAudioSourceReference
{
    private readonly byte[] bytes;
    internal Source(byte[] bytes) { this.bytes = bytes; Length = bytes.Length; }
    internal long Length; internal int Reads; internal bool Corrupt;
    public string Key => "sound.wav";
    public OwnerDataResult<long> GetLength() => new(OwnerDataStatus.Found, Length);
    public OwnerDataResult<byte[]> ReadBytes(int maximumBytes)
    {
        Reads++;
        if (Length > maximumBytes) return new(OwnerDataStatus.IoError, null);
        return new(OwnerDataStatus.Found, Corrupt ? new byte[44] : (byte[])bytes.Clone());
    }
}
sealed class Fake : IOwnedSoundBackend
{
    internal string? Fail;
    internal bool ZeroPlay;
    internal bool EarlyEnd;
    internal int Loads, Unloads;
    private uint next;
    internal readonly Dictionary<uint, int> Banks = new();
    internal readonly HashSet<object> Emitters = new(), Registered = new();
    internal readonly Dictionary<object, float> Gains = new();
    internal readonly Dictionary<object, AudioPosition> Positions = new();
    internal readonly Dictionary<uint, Action> Callbacks = new();
    private readonly Dictionary<Action<SoundEndReceipt>, uint> callbackIds = new();
    internal readonly List<(uint Id, int Ms, int Curve)> Stops = new();
    internal readonly HashSet<uint> Stopped = new();
    internal readonly List<uint> Events = new();
    internal readonly Dictionary<uint, bool> Pauses = new();
    internal bool Empty => Banks.Count == 0 && Emitters.Count == 0 && Registered.Count == 0 && Callbacks.Count == 0;
    private void Hit(string stage) { if (Fail == stage) { Fail = null; throw new IOException("injected " + stage); } }
    public uint Load(byte[] bytes) { Hit("load"); uint id = BitConverter.ToUInt32(bytes, 12); Banks.Add(id, bytes.Length); Loads++; return id; }
    public void Unload(uint bankId) { Hit("unload"); Banks.Remove(bankId); Unloads++; }
    public object CreateEmitter() { Hit("create"); var emitter = new object(); Emitters.Add(emitter); return emitter; }
    public void Register(object emitter) { Registered.Add(emitter); Hit("register"); }
    public void Gain(object emitter, float gain) { Hit("gain"); Gains[emitter] = gain; }
    public void Position(object emitter, AudioPosition position) { Hit("position"); Positions[emitter] = position; }
    public uint Play(uint eventId, object emitter, Action<SoundEndReceipt> ended) { Hit("play"); uint id = ++next; Callbacks.Add(id, () => ended(new SoundEndReceipt(id, 1))); callbackIds.Add(ended, id); Events.Add(eventId); if (EarlyEnd) Callbacks[id](); return ZeroPlay ? 0 : id; }
    public void Pause(uint playingId, bool paused) { Hit("pause"); Pauses[playingId] = paused; }
    public void Stop(uint playingId, int transitionMs, int curve) { Hit("stop"); Stopped.Add(playingId); Stops.Add((playingId, transitionMs, curve)); }
    public void CancelCallback(Action<SoundEndReceipt> ended) { Hit("cancel"); if (callbackIds.Remove(ended, out var id)) Callbacks.Remove(id); }
    public void Unregister(object emitter) { Hit("unregister"); Registered.Remove(emitter); }
    public void Destroy(object emitter) { Hit("destroy"); Emitters.Remove(emitter); Gains.Remove(emitter); }
    internal void End(uint id) => Callbacks[id]();
}
sealed class Manifest : IManifest
{
    internal Manifest(string id) { UniqueID = id; }
    public string UniqueID { get; } public string Name => UniqueID; public string Author => "Test"; public string Version => "1.0.0";
    public string Description => ""; public string EntryDll => ""; public string EntryType => ""; public string MinimumDTMApiVersion => "0.7.5"; public string MinimumGameVersion => ""; public string Type => "CodeMod";
    public IReadOnlyList<IManifestDependency> Dependencies => Array.Empty<IManifestDependency>(); public IReadOnlyList<string> UpdateKeys => Array.Empty<string>();
}
