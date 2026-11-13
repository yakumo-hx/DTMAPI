using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using DTMAPI.Abstractions;
using DTMAPI.AudioContent;
using DTMAPI.AudioContent.Hosting;
using DTMAPI.AudioContent.Preparation;
using DTMAPI.Modules.Audio;

internal static class PreparationChecks
{
    internal static object Run(string root, string decoder, string fixtures, byte[] original, Action<bool, string> check, Action<Action, string> throws)
    {
        Directory.CreateDirectory(root);
        string inputs = Path.Combine(root, "inputs"); Directory.CreateDirectory(inputs);
        File.WriteAllBytes(Path.Combine(inputs, "original.wav"), original);
        string bundled = AudioDecoder.DefaultExecutable;
        Directory.CreateDirectory(Path.GetDirectoryName(bundled)!); File.Copy(decoder, bundled, true);
        var logs = new List<string>(); var rows = new List<object>();
        foreach (var format in new[] { ("float.wav", "pcm_f32le"), ("source.mp3", "libmp3lame"), ("source.flac", "flac"), ("source.aac", "aac"), ("source.m4a", "aac") })
        {
            File.Copy(Path.Combine(fixtures, format.Item1), Path.Combine(inputs, format.Item1));
            string source = Path.Combine(inputs, format.Item1);
            double duration = 0; int peak = 0;
            using (var input = File.OpenRead(source))
                AudioDecoder.Decode(input, decoder, Path.Combine(root, "decode-work"), (pcm, info) => {
                    duration = (double)info.Bytes / (info.Rate * info.Channels * 2);
                    pcm.Position = info.Offset;
                    using var reader = new BinaryReader(pcm, System.Text.Encoding.UTF8, true);
                    for (int i = 0; i < info.Bytes / 2; i++) peak = Math.Max(peak, Math.Abs((int)reader.ReadInt16()));
                    check(info.Rate == 48000 && info.Channels == 2, "Unsupported source PCM converts to the backend format.");
                }, CancellationToken.None);
            check(duration >= .59 && duration < .72 && peak > 1700 && peak < 4000, "The complete quiet signal is retained without automatic loudness gain: " + format.Item1 + "/" + duration + "/" + peak);
            var definition = AudioContentReader.Parse("{\"resources\":[{\"key\":\"sample\",\"path\":\"" + format.Item1 + "\"}],\"events\":[{\"key\":\"sample\",\"resource\":\"sample\",\"use\":\"effect\"}]}");
            string cache = Path.Combine(root, format.Item1 + "-cache");
            Exercise(inputs, definition, cache, decoder, check, logs);
            // A fresh process-equivalent cache reader reuses prepared media even
            // when no decoder executable is available on the warm path.
            Exercise(inputs, definition, cache, Path.Combine(root, "no-decoder.exe"), check, logs);
            rows.Add(new { format = format.Item1, duration, peak });
        }
        check(logs.Count(s => s.StartsWith("StreamPrepare ") && s.Contains("cache=Hot")) == 5, "Every representative ordinary source reused its persistent derived media.");
        foreach (bool stream in new[] { false, true })
        {
            string output = Path.Combine(root, stream ? "prebuilt-stream" : "prebuilt-short");
            var product = AudioPreparer.Prepare(Path.Combine(inputs, "source.flac"), output, stream);
            using var content = System.Text.Json.JsonDocument.Parse(File.ReadAllText(product.DeclarationPath));
            var definition = AudioContentReader.Parse(content.RootElement.GetProperty("audio").GetRawText());
            check(definition.Resources[0].Kind == AudioResourceKind.Bank && definition.Events[0].BankEvent.Length > 0 &&
                !Directory.GetFiles(output, "*", SearchOption.AllDirectories).Any(p => new[] { ".wav", ".mp3", ".flac", ".aac", ".m4a" }.Contains(Path.GetExtension(p))), "Prebuilt output contains only the Bank, required media and declaration.");
            Exercise(output, definition, Path.Combine(root, "bank-cache-" + stream), Path.Combine(root, "absent-decoder.exe"), check, logs);
            check(!File.ReadAllText(product.DeclarationPath).Contains("generator"), "The declared Bank entered without a generator or provenance marker.");
            if (stream)
            {
                CheckBankUpdates(output, definition.Resources[0], Path.Combine(root, "bank-update-cache"), check, throws, logs);
                File.Move(product.MediaPath!, product.MediaPath + ".missing");
                throws(() => Exercise(output, definition, Path.Combine(root, "missing-media-cache"), decoder, check, logs), "Missing declared WEM is reported before native loading.");
                File.Move(product.MediaPath + ".missing", product.MediaPath!);
            }
        }
        File.WriteAllText(Path.Combine(inputs, "broken.mp3"), "not audio");
        var broken = new AudioRegistration { Resources = new[] { new AudioResource { Key = "broken", Path = "broken.mp3" } }, Events = new[] { new AudioEvent { Key = "broken", Resource = "broken" } } };
        string failure = Exercise(inputs, broken, Path.Combine(root, "broken-cache"), decoder, check, logs, expectFailure: true);
        check(failure.Contains("AudioDecodeFailed") && failure.Contains("broken.mp3"), "Decode errors identify the resource and actual decoder reason: " + failure);
        broken.Resources[0].Path = "source.mp3"; broken.Resources[0].Sha256 = new string('0', 64);
        failure = Exercise(inputs, broken, Path.Combine(root, "hash-cache"), decoder, check, logs, expectFailure: true);
        check(failure.Contains("AudioResourceHashMismatch"), "Existing optional author SHA verification still applies to controlled source preparation.");
        using (var cancel = new CancellationTokenSource())
        {
            cancel.Cancel();
            throws(() => AudioPreparer.Prepare(Path.Combine(inputs, "source.mp3"), Path.Combine(root, "cancelled"), false, cancellationToken: cancel.Token), "Cancellation reports cancellation and leaves no product.");
        }
        // Repeat the quiet fixture only to hold a real decoder long enough to
        // cancel after output has begun; this is not a capacity/admission test.
        string cancellationInput = Path.Combine(inputs, "cancel.flac");
        File.Copy(Path.Combine(fixtures, "cancel.flac"), cancellationInput);
        string activeWork = Path.Combine(root, "active-cancel"); Directory.CreateDirectory(activeWork);
        using (var cancel = new CancellationTokenSource())
        {
            var work = System.Threading.Tasks.Task.Run(() => {
                using var input = File.OpenRead(cancellationInput);
                AudioDecoder.Decode(input, decoder, activeWork, (_, _) => throw new Exception("Cancellation arrived after conversion"), cancel.Token);
            });
            var clock = Stopwatch.StartNew(); bool outputStarted = false;
            while (!work.IsCompleted && clock.ElapsedMilliseconds < 10000)
            {
                outputStarted = Directory.GetFiles(activeWork, "*.pcm").Any(p => new FileInfo(p).Length > 65536);
                if (outputStarted) break;
                Thread.Sleep(1);
            }
            cancel.Cancel(); bool cancelled = false;
            try { work.GetAwaiter().GetResult(); } catch (OperationCanceledException) { cancelled = true; }
            check(outputStarted && cancelled, "Cancellation after real decoder output terminates the process and returns cancellation.");
            check(Directory.GetFiles(activeWork).Length == 0, "Active decoder cancellation removes both staged input and partial PCM.");
        }
        check(!Directory.GetFiles(root, "*.part", SearchOption.AllDirectories).Any() && !Directory.GetFiles(root, "*.input", SearchOption.AllDirectories).Any() && !Directory.GetFiles(root, "*.input.pcm", SearchOption.AllDirectories).Any(), "Failure/cancellation leave no decoder input or partial media.");
        File.WriteAllLines(Path.Combine(root, "preparation-trace.log"), logs);
        return new { scope = "real pinned Windows decoder, shared preparation, Core file authority and native doubles; game output recorded separately", formats = rows };
    }
    private static void CheckBankUpdates(string root, AudioResource resource, string cacheRoot, Action<bool, string> check, Action<Action, string> throws, List<string> logs)
    {
        const string owner = "Prepared.Update";
        var source = StreamingChecks.Reference(root, () => { }, resource.Path, owner + "/source");
        var media = resource.Media.Select(p => StreamingChecks.Reference(root, () => { }, p, owner + "/source")).ToArray();
        var dependencies = Array.Empty<DTMAPI.Abstractions.Internal.PackageResourceReference>();
        var cache = new StreamingCache(cacheRoot, logs.Add);
        StreamingCache.Job Begin(string id = owner) => cache.BeginBank(id, source, source.Inspect(), media, dependencies, long.MaxValue);
        void Finish(StreamingCache.Job job) { job.Work?.GetAwaiter().GetResult(); cache.Finish(job, true); }
        var first = Begin(); Finish(first);
        byte[] originalBank = File.ReadAllBytes(first.BankPath), originalMedia = File.ReadAllBytes(Path.Combine(root, resource.Media[0]));
        File.SetLastWriteTimeUtc(Path.Combine(root, resource.Path), File.GetLastWriteTimeUtc(Path.Combine(root, resource.Path)).AddSeconds(2));
        throws(() => Begin(), "Updating an authored Bank cannot replace media held by a live pin.");
        check(File.ReadAllBytes(first.BankPath).SequenceEqual(originalBank), "Rejected live replacement preserves the active Bank bytes.");
        first.Pin.Dispose();
        cache = new StreamingCache(cacheRoot, logs.Add);
        var replacement = Begin(); Finish(replacement);
        check(File.ReadAllBytes(replacement.BankPath).SequenceEqual(originalBank) &&
            File.ReadAllBytes(Path.Combine(cacheRoot, "Media", Path.GetFileName(resource.Media[0]))).SequenceEqual(originalMedia),
            "An updated package replaces only its unused Bank generation, preserving authored Bank/media bytes without decoding.");
        replacement.Pin.Dispose();
        throws(() => Begin("Another.Owner"), "Another owner cannot take an authored Bank cache identity.");
        string otherPath = resource.Path + ".other.bnk"; File.Copy(Path.Combine(root, resource.Path), Path.Combine(root, otherPath));
        var other = StreamingChecks.Reference(root, () => { }, otherPath, owner + "/source");
        throws(() => cache.BeginBank(owner, other, other.Inspect(), media, dependencies, long.MaxValue), "Different Banks still cannot claim the same external media ID.");
        cache = new StreamingCache(cacheRoot, logs.Add);
        var warm = Begin(); Finish(warm);
        check(warm.Hit, "The replaced authored Bank is reusable on the next cold cache reader."); warm.Pin.Dispose();
    }
    private static string Exercise(string sourceRoot, AudioRegistration input, string cache, string decoder, Action<bool, string> check, List<string> logs, bool expectFailure = false)
    {
        string owner = "Preparation.Owner"; int main = Environment.CurrentManagedThreadId;
        void Guard() { if (Environment.CurrentManagedThreadId != main) throw new InvalidOperationException("WrongThread"); }
        var driver = new StreamingChecks.FileDriver { Early = true }; using var engine = new AudioEngine(driver, new HashSet<uint>(), logs.Add);
        engine.ConfigureStreaming(cache); engine.ConfigureDecoder(decoder);
        var sources = input.Resources.SelectMany(r => new[] { r.Path }.Concat(r.Media).Concat(r.Dependencies)).Distinct().ToDictionary(path => path,
            path => new StreamingChecks.DiskSource(StreamingChecks.Reference(sourceRoot, Guard, path, owner + "/source")));
        foreach (var source in sources.Values) source.StreamingResource.Inspect();
        using var lease = engine.Register(owner, 1, input, path => sources[path], Guard);
        engine.Tick(true);
        if (input.Resources[0].Stream) { try { lease.GetSound(input.Events[0].Key).Play(0f); } catch (InvalidOperationException) { } }
        var watch = Stopwatch.StartNew();
        while (engine.Snapshot(owner).State is not (AudioPreparationState.Ready or AudioPreparationState.Failed))
        {
            engine.Tick(true); if (watch.ElapsedMilliseconds > 20000) throw new Exception("Preparation timeout: " + string.Join("\n", logs.TakeLast(4))); Thread.Sleep(1);
        }
        var snapshot = engine.Snapshot(owner);
        if (expectFailure) { check(snapshot.State == AudioPreparationState.Failed, "Expected preparation failure."); return snapshot.Diagnostic; }
        check(snapshot.State == AudioPreparationState.Ready, "Preparation succeeded: " + snapshot.Diagnostic);
        var playback = lease.GetSound(input.Events[0].Key).Play(0f); playback.Pause(true); playback.Pause(false); playback.Stop();
        lease.Dispose();
        check(driver.Posts == 1 && engine.CountOwnerResources(owner) == 0 && engine.Snapshot(owner).FilePinCount == 0, "Common Bank playback and withdrawal release their exact file/native ownership.");
        return "";
    }
}
