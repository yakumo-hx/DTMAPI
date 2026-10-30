using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

// Bounded Windows experiments, not a replacement cleanup implementation.
// Uses only public managed IO. No native imports, reflected IO, game, or player files.
internal static class ManagedCleanupBoundaryChecks
{
    private const string Original = "MES complete old snapshot A\n";
    private const string Changed = "MES changed source snapshot B\n";
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    internal static void Run(string root, string output, string dotnet)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Windows sharing semantics are the subject of this experiment.");
        var checks = new List<string>();
        var cases = new List<object>();
        string caseRoot = Path.Combine(root, "managed-cleanup"); Directory.CreateDirectory(caseRoot);
        void Check(bool result, string message) { if (!result) throw new InvalidOperationException(message); checks.Add(message); }
        string Fresh(string name)
        {
            string folder = Path.Combine(caseRoot, name); Directory.CreateDirectory(folder);
            string file = Path.Combine(folder, "source.json"); File.WriteAllText(file, Original, new UTF8Encoding(false)); return file;
        }
        Operation Compete(string path, string operation)
        {
            var start = new ProcessStartInfo(dotnet) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
            start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
            foreach (string arg in new[] { "--managed-cleanup-worker", operation, path }) start.ArgumentList.Add(arg);
            using var process = Process.Start(start) ?? throw new IOException("Could not start managed competitor.");
            if (!process.WaitForExit(10000)) { process.Kill(true); throw new TimeoutException("Managed competitor exceeded 10 seconds."); }
            string stdout = process.StandardOutput.ReadToEnd(), stderr = process.StandardError.ReadToEnd();
            if (process.ExitCode != 0) throw new IOException("Competitor did not produce a valid observation: " + stderr + stdout);
            var observation = JsonSerializer.Deserialize<Operation>(stdout)!;
            Check(observation.ProcessId != Environment.ProcessId, "competitor is a distinct process: " + operation);
            return observation;
        }

        // Candidate A: hold a normal read handle through a path-based disposition.
        // Strong sharing blocks the competitors, but also the caller's disposition.
        string strict = Fresh("A1-strict-share");
        using (var guard = Open(strict, FileShare.Read))
        {
            Check(Read(guard) == Original, "A1 frozen bytes observed while guard is live");
            Operation writer = Compete(strict, "write");
            Operation replacer = Compete(strict, "replace-same");
            Operation remove = Observe("delete while same guard stays open", () => File.Delete(strict));
            Operation move = Observe("move while same guard stays open", () => File.Move(strict, strict + ".retired"));
            Check(SharingDenied(writer) && SharingDenied(replacer), "A1 foreign write and same-byte replacement are denied by Windows sharing");
            Check(SharingDenied(remove) && SharingDenied(move), "A1 both protected path deletion and move are denied to the candidate itself");
            Check(Read(guard) == Original && File.Exists(strict), "A1 rejection preserves original bytes");
            cases.Add(new { id = "A1", candidate = "live FileStream + path disposition; FileShare.Read", writer, replacer, remove, move,
                outcome = "rejected: every normal deletion or retirement fails while protection remains held" });
        }

        // Weakening only delete sharing permits disposition but also detaches the path
        // from the checked live stream. No scheduler race: swap occurs at the exact gap.
        foreach (bool identical in new[] { false, true })
        {
            string path = Fresh(identical ? "A3-same-byte-move" : "A2-changed-delete");
            using var guard = Open(path, FileShare.Read | FileShare.Delete);
            Check(Read(guard) == Original, "A2/A3 checked stream before competitor replacement");
            Operation writer = Compete(path, "write");
            Check(SharingDenied(writer), "A2/A3 write sharing remains denied; this is a name replacement counterexample");
            Operation replace = Compete(path, identical ? "replace-same" : "replace-changed");
            Check(replace.Succeeded, "A2/A3 separate process renamed original and created a distinct source file");
            Check(Read(guard) == Original && File.ReadAllText(path + ".displaced") == Original, "A2/A3 still-open checked stream remains the displaced original");
            string substituted = File.ReadAllText(path);
            Check(substituted == (identical ? Original : Changed), "A2/A3 substituted path has expected bytes");
            Operation dispose = identical
                ? Observe("move checked name", () => File.Move(path, path + ".retired"))
                : Observe("delete checked name", () => File.Delete(path));
            Check(dispose.Succeeded && !File.Exists(path) && File.Exists(path + ".displaced"), "A2/A3 disposition processes the replacement, leaving the actual checked original elsewhere");
            if (identical) Check(File.ReadAllText(path + ".retired") == Original, "A3 same bytes do not stop the wrong physical file from being moved");
            cases.Add(new { id = identical ? "A3" : "A2", candidate = "live FileStream + path disposition; FileShare.Read | FileShare.Delete", identical,
                writer, replace, dispose, checkedOriginalSha256 = Hash(Original), substitutedSha256 = Hash(substituted),
                originalRemainsAt = Path.GetFileName(path + ".displaced"), outcome = "rejected: protection follows the open object, while disposition follows the replaceable path" });
        }

        // Candidate B: managed DeleteOnClose gives handle-based deletion, but has no
        // conditional commit/cancel step. A protected ordinary handle cannot upgrade it.
        string upgrade = Fresh("B1-upgrade");
        using (var guard = Open(upgrade, FileShare.Read))
        {
            Check(Read(guard) == Original, "B1 checked bytes with strict guard");
            Operation acquire = Observe("open DeleteOnClose while strict guard is live", () => { using var deletion = DeleteOnClose(upgrade); });
            Check(SharingDenied(acquire) && File.Exists(upgrade) && Read(guard) == Original, "B1 strict guard prevents a second delete-on-close handle from opening");
            cases.Add(new { id = "B1", candidate = "FileOptions.DeleteOnClose after protected inspection", acquire,
                outcome = "rejected: handle acquisition needs delete sharing that the guard intentionally denies" });
        }

        string late = Fresh("B2-rejected-bytes");
        string expected = File.ReadAllText(late);
        Operation change = Compete(late, "replace-changed");
        Check(change.Succeeded, "B2 source changed between display and later cleanup");
        bool accepted;
        using (var deletion = DeleteOnClose(late)) accepted = Read(deletion) == expected;
        Check(!accepted && !File.Exists(late) && File.ReadAllText(late + ".displaced") == Original,
            "B2 disposing after rejecting changed bytes nevertheless deletes the rejected replacement");
        cases.Add(new { id = "B2", candidate = "reopen with DeleteOnClose, then compare", change, accepted,
            rejectedSourceDeleted = !File.Exists(late), outcome = "rejected: mismatch path cannot cancel the already armed deletion" });

        string early = Fresh("B3-cancel-before-save");
        bool nativeCommitted = false;
        using (var deletion = DeleteOnClose(early)) Check(Read(deletion) == Original, "B3 full snapshot was readable at display");
        Check(!nativeCommitted && !File.Exists(early), "B3 cancel or unsaved session disposal deletes the source without a native commit");
        cases.Add(new { id = "B3", candidate = "hold DeleteOnClose from display until choice/save", nativeCommitted,
            sourceDeletedOnCancel = !File.Exists(early), outcome = "rejected: ordinary cancel/shutdown disposal destroys uncommitted source" });

        // Failure control: an exclusive reader elsewhere causes a safe refusal to open,
        // rather than a successful deletion disguised as a failed comparison.
        string occupied = Fresh("control-occupied");
        using (var exclusive = Open(occupied, FileShare.None))
        {
            Operation attempt = Compete(occupied, "delete-on-close");
            Check(SharingDenied(attempt) && Read(exclusive) == Original, "occupied source rejects managed cleanup acquisition and retains bytes");
            cases.Add(new { id = "control", candidate = "occupied input", attempt, sourcePreserved = true });
        }
        Check(File.ReadAllText(occupied) == Original, "occupancy control still exists after releasing guard");

        var result = new {
            passed = true, meaning = "Counterexamples reproduced; neither candidate is accepted for production.",
            runtime = RuntimeInformation.FrameworkDescription, os = RuntimeInformation.OSDescription,
            processId = Environment.ProcessId, dotnet, fileSystem = new DriveInfo(Path.GetPathRoot(caseRoot)!).DriveFormat,
            candidateCount = 2, noGameOrMonoExecution = true, checks, cases
        };
        File.WriteAllText(Path.Combine(output, "managed-cleanup-boundary-results.json"), JsonSerializer.Serialize(result, Json));
        Console.WriteLine("PASS: " + checks.Count + " Windows observations; 2 managed cleanup candidates rejected with deterministic cross-process counterexamples; no game/Mono acceptance.");
    }

    internal static int Worker(string[] args)
    {
        if (args.Length != 3) return 2;
        string operation = args[1], path = Path.GetFullPath(args[2]);
        string root = Environment.GetEnvironmentVariable("DTMAPI_TEST_SESSION_ROOT") ?? "";
        if (root.Length == 0 || !path.StartsWith(Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) return 3;
        Operation result = Observe(operation, () =>
        {
            if (operation == "write") File.WriteAllText(path, Changed, new UTF8Encoding(false));
            else if (operation == "replace-same" || operation == "replace-changed")
            {
                DateTime timestamp = File.GetLastWriteTimeUtc(path);
                File.Move(path, path + ".displaced");
                using (var fresh = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    byte[] data = Encoding.UTF8.GetBytes(operation == "replace-same" ? Original : Changed);
                    fresh.Write(data, 0, data.Length);
                }
                File.SetLastWriteTimeUtc(path, timestamp);
            }
            else if (operation == "delete-on-close") { using var deletion = DeleteOnClose(path); }
            else throw new ArgumentException("Unknown competitor operation.");
        });
        Console.WriteLine(JsonSerializer.Serialize(result));
        return 0;
    }
    private static FileStream Open(string path, FileShare share) => new(path, FileMode.Open, FileAccess.Read, share, 1, FileOptions.None);
    private static FileStream DeleteOnClose(string path) => new(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1, FileOptions.DeleteOnClose);
    private static string Read(FileStream stream)
    {
        stream.Position = 0;
        using var memory = new MemoryStream(); stream.CopyTo(memory); return Encoding.UTF8.GetString(memory.ToArray());
    }
    private static string Hash(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    private static bool SharingDenied(Operation result) => !result.Succeeded && result.ErrorType == typeof(IOException).FullName && (result.HResult & 0xffff) == 32;
    private static Operation Observe(string name, Action action)
    {
        var result = new Operation { Name = name, ProcessId = Environment.ProcessId };
        try { action(); result.Succeeded = true; }
        catch (IOException ex) { result.ErrorType = ex.GetType().FullName!; result.HResult = ex.HResult; result.Message = ex.Message; }
        return result;
    }
    internal sealed class Operation
    {
        public string Name { get; set; } = "";
        public int ProcessId { get; set; }
        public bool Succeeded { get; set; }
        public string ErrorType { get; set; } = "";
        public int HResult { get; set; }
        public string Message { get; set; } = "";
    }
}
