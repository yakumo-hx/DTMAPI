using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text.Json;
using DTMAPI.Abstractions;
using DTMAPI.Core.Services;
using DTMAPI.MoreEquipmentSlots;

internal static class SourceCapabilityChecks
{
    internal static void Run(string root, string output, string sample, bool baseline)
    {
        var checks = new List<string>();
        void Check(bool value, string text) { if (!value) throw new Exception(text); checks.Add(text); }
        using var input = File.OpenRead(sample);
        var document = (EquipmentSlotStorageDocument)new DataContractJsonSerializer(typeof(EquipmentSlotStorageDocument)).ReadObject(input)!;
        string full = EquipmentSlotEmbeddedPayload.Encode(document.Slots);
        string empty = EquipmentSlotEmbeddedPayload.Encode(EquipmentSlotStorageDocument.CreateEmptySlots());
        if (!baseline) { Verify(root, output, document, full, empty); return; }
        foreach (string raw in new[] { full, empty })
        {
            var files = new UnavailableFiles(ProtectedSourceStatus.Unsupported);
            Check(EquipmentSlotEmbeddedPayload.Read(true, raw, out _, out _) == EquipmentSlotEmbeddedReadKind.Supported, "baseline current payload is valid, including the explicit empty group");
            Check(EquipmentSlotEmbeddedEntry.TrySelect(true, false, files, 11, root, out var source, out string error) &&
                source == null && error.Contains("Unsupported") && files.Calls == 2, "both old-source capability results reach the actual entry as errors, never Missing");
            using var session = new EquipmentSlotEmbeddedSession(new object(), 1, "native11", true, raw, source, error);
            Check(session.State == EquipmentSlotEmbeddedState.Unavailable && session.Working == null,
                "M15 coupling reproduced: valid current is blocked by old-source Unsupported");
        }
        File.WriteAllText(Path.Combine(output, "source-capability-baseline.json"), JsonSerializer.Serialize(new { passed = true,
            scope = "Source-level reproduction with injected Unsupported; not a non-Windows game run.", checks }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("PASS: " + checks.Count + " coupling reproduction observations.");
    }

    private static void Verify(string root, string output, EquipmentSlotStorageDocument document, string full, string empty)
    {
        var checks = new List<string>();
        void Check(bool value, string text) { if (!value) throw new Exception(text); checks.Add(text); }
        void Reject(Action action, string text)
        {
            bool rejected = false;
            try { action(); } catch (InvalidOperationException) { rejected = true; } catch (IOException) { rejected = true; }
            Check(rejected, text);
        }
        string dir = Path.Combine(root, "source-capability");
        const string name = "equipment-slots-DTMAPI.MoreEquipmentSlotsMod.json";
        string scoped = Path.Combine(dir, "protected-items", "equipment-slots", "slot-11", name);
        string global = Path.Combine(dir, name);
        Directory.CreateDirectory(Path.GetDirectoryName(scoped)!);
        document.Scope.ArchiveIndex = 11;
        using var buffer = new MemoryStream();
        new DataContractJsonSerializer(typeof(EquipmentSlotStorageDocument)).WriteObject(buffer, document);
        byte[] sourceBytes = buffer.ToArray();
        File.WriteAllBytes(scoped, sourceBytes); File.WriteAllBytes(global, sourceBytes);
        using var nativeFiles = new ProtectedSourceFileService(dir, () => { }, () => { });
        object archive = new();
        EquipmentSlotEmbeddedSession Open(RecordingFiles files, bool exists, object? raw)
        {
            Check(EquipmentSlotEmbeddedEntry.TrySelect(exists, false, files, 11, dir, out var source, out string error), "input routes through the actual embedded entry");
            return new EquipmentSlotEmbeddedSession(archive, 7, "native11", exists, raw, source, error);
        }
        void Retained(RecordingFiles files, string label) => Check(files.Deletes == 0 &&
            File.ReadAllBytes(scoped).SequenceEqual(sourceBytes) && File.ReadAllBytes(global).SequenceEqual(sourceBytes), label + ": both actual source files retained without cleanup authority");

        foreach (var status in new[] { ProtectedSourceStatus.Unsupported, ProtectedSourceStatus.Busy,
            ProtectedSourceStatus.AccessDenied, ProtectedSourceStatus.IoError, ProtectedSourceStatus.Rejected,
            ProtectedSourceStatus.Changed, ProtectedSourceStatus.Expired, (ProtectedSourceStatus)999 })
        {
            foreach (string raw in new[] { full, empty })
            {
                var files = new RecordingFiles(nativeFiles, status);
                using var session = Open(files, true, raw);
                Check(session.State == EquipmentSlotEmbeddedState.Ready && session.SourceProblem.Length > 0 &&
                    EquipmentSlotEmbeddedPayload.Encode(session.Working!) == raw, status + ": exact valid current/explicit empty remains usable with a source explanation");
                Reject(() => session.Choose(true), status + ": no adoption button authority");
                Reject(() => session.Choose(false), status + ": using current is not permission to discard old sources");
                session.ValidateContext(archive, 7, "native11", true, raw);
                session.FinishKeepCurrent(true, raw);
                var latest = EquipmentSlotGameplayCandidateCoordinator.CloneSlots(session.Working!);
                latest[0] = document.Slots[0].Clone(); latest[0].ShieldValue = 2;
                string staged = session.Stage(latest, "before");
                Check(!session.FinishSaved("before", true, staged) && session.FinishSaved("after", true, staged), status + ": unchanged native commit proof still governs model completion");
                Check(files.Captures == 2 && files.Revalidations == 0 && files.Deletes == 0, status + ": current save neither retries nor disposes unavailable old sources");
                Retained(files, status.ToString());
            }
            using var missing = Open(new RecordingFiles(nativeFiles, status), false, null);
            Check(missing.State == EquipmentSlotEmbeddedState.Unavailable && missing.Working == null,
                status + ": absent current plus indeterminate sources never creates empty Working");
        }
        foreach (object? raw in new object?[] { null, 17, "{", "{\"schema\":99,\"slots\":[]}" })
        {
            using var invalid = Open(new RecordingFiles(nativeFiles, ProtectedSourceStatus.Unsupported), true, raw);
            Check(invalid.State == EquipmentSlotEmbeddedState.Unavailable && invalid.Working == null,
                "invalid/unknown/non-string embedded data stays unavailable despite old-source failure");
        }
        var thrown = new RecordingFiles(nativeFiles) { ThrowCapture = true };
        using (var current = Open(thrown, true, full))
            Check(current.State == EquipmentSlotEmbeddedState.Ready && current.SourceProblem.Contains("bounded I/O"), "capture exception is separated from valid current");
        using (var missing = Open(thrown, false, null))
            Check(missing.State == EquipmentSlotEmbeddedState.Unavailable && missing.Working == null, "capture exception is not Missing without current");

        var conflictFiles = new RecordingFiles(nativeFiles);
        using (var conflict = Open(conflictFiles, true, full))
        {
            Check(conflict.State == EquipmentSlotEmbeddedState.Ready && conflict.Source == null && conflict.SourceProblem.Contains("多个"), "two actual old sources cannot prevent valid current or choose a winner");
            Reject(() => conflict.Choose(true), "conflicting sources grant no explicit-adoption authority");
            Retained(conflictFiles, "conflict");
        }
        using (var conflict = Open(new RecordingFiles(nativeFiles), false, null))
            Check(conflict.State == EquipmentSlotEmbeddedState.Unavailable && conflict.Working == null, "two actual old sources do not establish a source-free save");
        File.Delete(global);
        foreach (string invalid in new[] { "{", "{\"schemaVersion\":99}" })
        {
            File.WriteAllText(scoped, invalid);
            var files = new RecordingFiles(nativeFiles);
            using var current = Open(files, true, full);
            Check(current.State == EquipmentSlotEmbeddedState.Ready && current.Source?.Slots == null && current.SourceProblem.Length > 0 &&
                EquipmentSlotEmbeddedPayload.Encode(current.Working!) == full, "uninterpretable actual old bytes are preserved while exact current remains usable");
            string staged = current.Stage(current.Working!, "before"); current.FinishSaved("after", true, staged);
            Check(File.ReadAllText(scoped) == invalid && files.Deletes == 0, "saving current does not remove uninterpretable source bytes");
            using var noCurrent = Open(new RecordingFiles(nativeFiles), true, "{");
            Check(noCurrent.State == EquipmentSlotEmbeddedState.Unavailable && noCurrent.Working == null, "bad current plus bad old source is not recovered by guessing");
        }
        File.WriteAllBytes(scoped, sourceBytes);
        var selectableFiles = new RecordingFiles(nativeFiles);
        using (var selectable = Open(selectableFiles, true, full))
        {
            Check(selectable.State == EquipmentSlotEmbeddedState.NeedsChoice && selectable.Working == null && selectable.SourceProblem.Length == 0, "valid single source retains explicit whole-group choice");
            File.AppendAllText(scoped, " ");
            Reject(() => selectable.Choose(true), "source replacement still invalidates an explicit confirmation");
            Check(selectable.State == EquipmentSlotEmbeddedState.Unavailable && selectableFiles.Deletes == 0, "stale choice does not adopt or dispose the changed source");
        }
        File.WriteAllBytes(scoped, sourceBytes);
        var legacyFiles = new RecordingFiles(nativeFiles);
        Check(EquipmentSlotEmbeddedEntry.TrySelect(false, false, legacyFiles, 11, dir, out var normalSource, out string oldError) && oldError.Length == 0 && normalSource?.Slots != null,
            "no current with a complete readable old source reaches normal explicit choice");
        normalSource?.Dispose();
        foreach (int change in Enumerable.Range(0, 4))
        {
            using var current = Open(new RecordingFiles(nativeFiles, ProtectedSourceStatus.Unsupported), true, full);
            Reject(() => current.ValidateContext(change == 0 ? new object() : archive, change == 1 ? 8 : 7,
                change == 2 ? "other" : "native11", true, change == 3 ? empty : full), "current-use source error cannot bypass archive/epoch/target/raw expiry: " + change);
            Check(current.State == EquipmentSlotEmbeddedState.Unavailable, "expired current-use session is unusable");
        }
        File.WriteAllText(Path.Combine(output, "source-capability-results.json"), JsonSerializer.Serialize(new { passed = true,
            scope = "Actual MES entry/session with injected capability statuses and real protected Windows source files. Model commit checks do not replace native SaveGame.", checks }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("PASS: " + checks.Count + " source-capability/current-use checks.");
        ConfirmedAdoptionChecks.Run(root, output);
    }

    private sealed class RecordingFiles : IProtectedSourceFiles
    {
        private readonly IProtectedSourceFiles actual;
        private readonly ProtectedSourceStatus? forced;
        internal int Captures, Revalidations, Deletes;
        internal bool ThrowCapture;
        internal RecordingFiles(IProtectedSourceFiles actual, ProtectedSourceStatus? forced = null) { this.actual = actual; this.forced = forced; }
        public ProtectedSourceResult Capture(string key)
        {
            Captures++;
            if (ThrowCapture) throw new IOException("bounded I/O fixture");
            if (forced.HasValue) return new ProtectedSourceResult(forced.Value, "bounded capability fixture");
            var result = actual.Capture(key);
            return result.Snapshot == null ? result : new ProtectedSourceResult(result.Status, result.Detail, new RecordedSnapshot(this, result.Snapshot));
        }
        private sealed class RecordedSnapshot : IProtectedSourceSnapshot
        {
            private readonly RecordingFiles owner;
            private readonly IProtectedSourceSnapshot inner;
            internal RecordedSnapshot(RecordingFiles owner, IProtectedSourceSnapshot inner) { this.owner = owner; this.inner = inner; }
            public string RelativeKey => inner.RelativeKey;
            public byte[] CopyBytes() => inner.CopyBytes();
            public ProtectedSourceResult Revalidate() { owner.Revalidations++; return inner.Revalidate(); }
            public ProtectedSourceResult TryDelete() { owner.Deletes++; return inner.TryDelete(); }
            public void Dispose() => inner.Dispose();
        }
    }

    private sealed class UnavailableFiles : IProtectedSourceFiles
    {
        private readonly ProtectedSourceStatus status;
        internal int Calls { get; private set; }
        internal UnavailableFiles(ProtectedSourceStatus status) { this.status = status; }
        public ProtectedSourceResult Capture(string relativeKey) { Calls++; return new ProtectedSourceResult(status, "bounded capability fixture"); }
    }
}
