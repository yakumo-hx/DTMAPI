using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DTMAPI.Abstractions;
using DTMAPI.AudioContent;
using DTMAPI.AudioContent.Authoring;
using DTMAPI.AudioContent.Hosting;
using DTMAPI.Modules.Audio;

internal static class E21Checks
{
    internal static void RunAnimalFixture(string root, Action<bool, string> check)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "PackData/content.json")));
        var input = AudioContentReader.Parse(document.RootElement.GetProperty("audio").GetRawText());
        var driver = new Fake(); using var engine = new AudioEngine(driver, new HashSet<uint>(), _ => { });
        using var registration = engine.Register("Animal.Fixture", 1, input, path => new FileSource(Path.Combine(root, path)), () => { });
        engine.Tick(true);
        check(registration.GetSnapshot().State == AudioPreparationState.Ready && input.Events.All(e => e.Route == AudioRoute.NativeAnimal), "Animal fixture prepares through common Audio with its native route.");
        foreach (var row in document.RootElement.GetProperty("animals").GetProperty("voices").EnumerateArray())
        {
            string native = row.GetProperty("nativeEvent").GetString()!;
            using var nomination = NativeReplacementScope.Enter(registration.GetSound(row.GetProperty("sound").GetString()!), "Animal.Fixture", native, 0);
            check(engine.TryReplace(native, 1000, out _), "A matched domain nomination reaches the admitted native Post.");
        }
    }
    private sealed class FileSource : IAudioSourceReference
    {
        internal FileSource(string path) { Key = path; }
        public string Key { get; }
        public OwnerDataResult<long> GetLength() => new(OwnerDataStatus.Found, new FileInfo(Key).Length);
        public OwnerDataResult<byte[]> ReadBytes(int maximumBytes) => new(OwnerDataStatus.Found, File.ReadAllBytes(Key));
    }
    internal static void Run(Action<bool, string> check, Action<Action, string> throws, Func<byte[]> wav)
    {
        AudioRegistration Input(string native = "") => new() {
            Resources = new[] { new AudioResource { Key = "r", Path = "r.wav" } },
            Events = new[] { new AudioEvent { Key = "same", Resource = "r", ReplaceNativeEvent = native, Route = AudioRoute.NativeAnimal } }
        };
        var driver = new Fake(); using var engine = new AudioEngine(driver, new HashSet<uint>(), _ => { });
        var first = engine.Register("Owner", 2, Input(), _ => new Source(wav()), () => { });
        var second = engine.Register("Owner", 2, Input(), _ => new Source(wav()), () => { });
        engine.Tick(true);
        var oldSound = first.GetSound("same"); var av = oldSound.Play(); var bv = second.GetSound("same").Play();
        throws(first.Dispose, "A shared Bank waits for the withdrawing registration's asynchronous voice.");
        check(!bv.HasEnded && engine.Snapshot("Owner").CleanupPendingCount == 1, "Pending release preserves a sibling's exact voice and media reference.");
        throws(() => oldSound.Play(), "A pending registration closes new plays.");
        throws(first.RetryPreparation, "Preparation retry cannot resurrect a closed registration.");
        driver.End(driver.Stopped.Single()); engine.Tick(true); first.Dispose();
        check(!bv.HasEnded && engine.Snapshot("Owner").RegistrationCount == 1, "Release retry completes only its own contribution.");
        driver.Fail = "unload"; throws(engine.RetireWorld, "World retirement retains failed native cleanup.");
        engine.Tick(false); engine.Tick(true);
        var current = second.GetSound("same").Play(); av.Stop(); bv.Stop(); first.Dispose();
        check(!current.HasEnded, "Previous world handles cannot stop the new world voice.");
        foreach (uint id in driver.Callbacks.Keys.ToArray()) driver.End(id); engine.Tick(true);
        using (NativeReplacementScope.Enter(second.GetSound("same"), "Owner", "PLAY_ANIMAL_TEST", 0))
        {
            check(engine.TryReplace("PLAY_ANIMAL_TEST", 1000, out _), "Domain nomination is consumed by the native-admitted Post.");
            using (NativeReplacementScope.Suppress())
                check(!engine.TryReplace("PLAY_ANIMAL_TEST", 1001, out _), "An unmatched nested animal call cannot inherit its outer nomination.");
            check(!engine.TryReplace("PLAY_ANIMAL_OTHER", 1002, out _), "A domain nomination cannot replace a different native event.");
            throws(() => NativeReplacementScope.Enter(second.GetSound("same"), "Other.Owner", "PLAY_ANIMAL_TEST", 0), "Domain consumers cannot borrow a different Owner's sound.");
        }
        check(NativeReplacementScope.Current == null, "Native nomination expires at the original call boundary.");
        using var generic = engine.Register("Later.Owner", 7, Input("PLAY_ANIMAL_TEST"), _ => new Source(wav()), () => { });
        using (NativeReplacementScope.Enter(second.GetSound("same"), "Owner", "PLAY_ANIMAL_TEST", 0))
            check(!engine.TryReplace("PLAY_ANIMAL_TEST", 2000, out string reason) && reason.StartsWith("NotReady"), "An unready higher-ranked generic winner falls back to native rather than a lower nomination.");
        second.Dispose();
        throws(() => oldSound.Play(), "Retired sound handles remain invalid after successful cleanup.");
        using var next = engine.Register("Owner", 2, Input(), _ => new Source(wav()), () => { });
        engine.Tick(true); check(next.GetSnapshot().State == AudioPreparationState.Ready, "Successful drain permits the same Owner and local keys again.");
    }
}
