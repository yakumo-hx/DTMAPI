using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.Json;
using DTMAPI.MoreEquipmentSlots;

internal static class ConfirmedAdoptionChecks
{
    internal static void Run(string root, string output)
    {
        var checks = new List<string>();
        void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); checks.Add(message); }
        void Reject(Action action, string message)
        { bool rejected = false; try { action(); } catch (InvalidOperationException) { rejected = true; } catch (IOException) { rejected = true; } Check(rejected, message); }
        var scope = new EquipmentSlotSaveScope { ArchiveIndex = 11, PlayerName = "MES confirmed fixture", TotalGameSeconds = 1000 };
        var full = EquipmentSlotDocumentStore.CreateEmpty(scope);
        full.Slots[0] = new EquipmentSlotStorageEntry { Index = 0, ItemId = "box_hat", DisplayName = "Box hat", IsShield = true,
            ShieldValue = 3, ShieldMaxValue = 100, ShieldDefend = 3,
            ItemState = new EquipmentSlotItemState { InstanceId = "11111111111111111111111111111111", NativeType = "DolocTown.ItemHatShield" } };
        full.Slots[1] = new EquipmentSlotStorageEntry { Index = 1, ItemId = "herbal_pouch", DisplayName = "Herb", ItemState = new EquipmentSlotItemState
            { InstanceId = "22222222222222222222222222222222", NativeType = "DolocTown.ItemHerbPackage", Filled = false } };
        full.Slots[2] = new EquipmentSlotStorageEntry { Index = 2, ItemId = "vulture_badge", DisplayName = "Badge", ItemState = new EquipmentSlotItemState
            { InstanceId = "33333333333333333333333333333333", NativeType = "DolocTown.ItemCooldown", Cooldown = 144 } };
        byte[] Serialize(EquipmentSlotStorageDocument doc)
        { using var memory = new MemoryStream(); new DataContractJsonSerializer(typeof(EquipmentSlotStorageDocument)).WriteObject(memory, doc); return memory.ToArray(); }
        var fixtureRoot = Path.Combine(root, "confirmed"); Directory.CreateDirectory(fixtureRoot);
        EquipmentSlotLegacySource Make(string name, EquipmentSlotStorageDocument? doc = null)
        {
            string directory = Path.Combine(fixtureRoot, name); Directory.CreateDirectory(directory);
            const string key = "equipment-slots-DTMAPI.MoreEquipmentSlotsMod.json";
            File.WriteAllBytes(Path.Combine(directory, key), Serialize(doc ?? full));
            return EquipmentSlotLegacySource.Capture(new DTMAPI.Core.Services.ProtectedSourceFileService(directory, () => { }, () => { }), key, directory, out _)!;
        }
        string current = EquipmentSlotEmbeddedPayload.Encode(full.Slots);
        string empty = EquipmentSlotEmbeddedPayload.Encode(EquipmentSlotStorageDocument.CreateEmptySlots());
        object archive = new(); const long epoch = 1; string nativePath = Path.Combine(fixtureRoot, "native-model.data");
        EquipmentSlotEmbeddedSession Session(EquipmentSlotLegacySource? source, bool exists = true, object? raw = null) =>
            new(archive, epoch, nativePath, exists, exists ? raw ?? current : null, source);

        var local = Make("local"); byte[] original = File.ReadAllBytes(local.Path);
        var session = Session(local, raw: empty);
        Check(session.State == EquipmentSlotEmbeddedState.NeedsChoice && session.Working == null,
            "load with old items never restores before explicit choice, including empty current");
        Check(EquipmentSlotEmbeddedSession.AdoptionWarning == "采用此电脑的旧装备会替换当前扩展槽装备，可能与背包中已有装备重复。",
            "confirmation wording states replacement and possible backpack duplication; UI rendering not exercised");
        session.Defer();
        Reject(() => session.Choose(true), "deferred prompt cannot execute an old confirmation");
        Check(File.ReadAllBytes(local.Path).SequenceEqual(original), "cancel preserves source bytes");
        local = Make("local"); // Reopening captures again; cancellation invalidated the prior snapshot.
        session = Session(local, raw: empty);
        session.ValidateContext(archive, epoch, nativePath, true, empty);
        session.Choose(true);
        Check(session.Working![0].ShieldValue == 3 && session.Working[1].ItemState!.Filled == false &&
            session.Working[2].ItemState!.Cooldown == 144, "confirmed local group preserves all mutable item states");
        Check(File.Exists(local.Path), "confirmation alone never cleans old source");
        var unsavedReload = Session(local, raw: empty);
        Check(unsavedReload.State == EquipmentSlotEmbeddedState.NeedsChoice && unsavedReload.Working == null,
            "new session has no persisted/replayed unsaved choice");
        var latest = EquipmentSlotGameplayCandidateCoordinator.CloneSlots(session.Working);
        latest[0].Clear(); latest[2].ItemState!.Cooldown = 7;
        string staged = session.Stage(latest, "native-before");
        Check(!session.FinishSaved("native-before", true, staged), "same native revision cannot stand in for a new commit");
        Check(!session.FinishSaved("native-after", true, empty) && File.Exists(local.Path),
            "SaveSaved-like notification with wrong on-disk payload cannot clean source");
        Check(EquipmentSlotEmbeddedPayload.Read(true, staged, out var decoded, out _) == EquipmentSlotEmbeddedReadKind.Supported &&
            decoded![0].ItemId == "" && decoded[2].ItemState!.Cooldown == 7, "Stage uses latest Working, including consume after choice");
        Check(session.FinishSaved("native-after", true, staged) && !File.Exists(local.Path),
            "verified model native revision disposes the exact selected file by protected handle");
        Check(!session.FinishSaved("native-after", true, staged), "completion does not replay cleanup");
        Check(!staged.Contains("adoptedSources") && !staged.Contains("journal") && !staged.Contains("source"),
            "payload contains no source history, transaction or cleanup ledger");

        var keep = Make("keep-current"); var keepSession = Session(keep);
        keepSession.Choose(false); keepSession.FinishKeepCurrent(false, null);
        Check(File.Exists(keep.Path), "keeping memory-only current group cannot clean source");
        keepSession.FinishKeepCurrent(true, current);
        Check(!File.Exists(keep.Path), "keeping matching committed current group cleans exact source without another save");
        var emptySource = Make("empty-source", EquipmentSlotDocumentStore.CreateEmpty(scope));
        var emptyChoice = Session(emptySource); emptyChoice.Choose(true);
        Check(emptyChoice.Working!.All(s => !s.IsOccupied), "valid empty local group is an explicit whole-group choice");

        var failSource = Make("cleanup-locked"); var failed = Session(failSource); failed.Choose(true);
        string failPayload = failed.Stage(full.Slots, "before");
        using (var fileLock = new FileStream(failSource.Path, FileMode.Open, FileAccess.Read, FileShare.Read))
            Check(failed.FinishSaved("after", true, failPayload) && File.Exists(failSource.Path) && failed.Message.Contains("未清理"),
                "OS cleanup failure preserves successful model commit and explains retained source");
        Check(!failed.FinishSaved("after2", true, failPayload) && File.Exists(failSource.Path),
            "cleanup failure does not create a background retry on later notification");
        Check(Session(failSource).State == EquipmentSlotEmbeddedState.NeedsChoice,
            "retained source on next session needs explicit confirmation again");

        var driftSource = Make("source-drift"); var driftSession = Session(driftSource);
        File.AppendAllText(driftSource.Path, " ");
        Reject(() => driftSession.Choose(true), "changed displayed source invalidates choice");
        var replacedSource = Make("source-replaced"); var replacement = Make("replacement-identical");
        File.Move(replacedSource.Path, replacedSource.Path + ".retained"); File.Move(replacement.Path, replacedSource.Path);
        Check(!replacedSource.IsUnchanged() && !replacedSource.TryRemove(out _) && File.Exists(replacedSource.Path),
            "same bytes under a different file identity cannot be adopted or deleted using stale selection");
        var context = Session(Make("context"));
        Reject(() => context.ValidateContext(new object(), epoch, nativePath, true, current), "archive object replacement invalidates choice");
        context = Session(Make("context-epoch"));
        Reject(() => context.ValidateContext(archive, epoch + 1, nativePath, true, current), "save epoch replacement invalidates choice");
        context = Session(Make("context-target"));
        Reject(() => context.ValidateContext(archive, epoch, nativePath + ".other", true, current), "target path replacement invalidates choice");
        context = Session(Make("context-display"));
        Reject(() => context.ValidateContext(archive, epoch, nativePath, true, empty), "displayed embedded data change invalidates choice");
        foreach (object raw in new object[] { "{bad", "{\"schema\":99,\"slots\":[]}", 17 })
        {
            var bad = Session(Make("bad-embedded-" + checks.Count), raw: raw);
            Check(bad.State == EquipmentSlotEmbeddedState.Unavailable && bad.Working == null, "bad/unknown/non-string embedded data is retained, not empty");
        }
        var badDoc = EquipmentSlotDocumentStore.CreateEmpty(scope); badDoc.SchemaVersion = 99;
        var unknownSource = Session(Make("unknown-source", badDoc));
        Check(unknownSource.State == EquipmentSlotEmbeddedState.Ready && unknownSource.SourceProblem.Length > 0 &&
            EquipmentSlotEmbeddedPayload.Encode(unknownSource.Working!) == current, "unknown old schema is retained while valid current stays usable");
        var pendingDoc = EquipmentSlotDocumentStore.CreateEmpty(scope); pendingDoc.Slots = full.Slots.Select(s => s.Clone()).ToList();
        EquipmentSlotTransactionCoordinator.PrepareRecovery(pendingDoc, new[] { 0 });
        var pendingSource = Session(Make("pending-source", pendingDoc));
        Check(pendingSource.State == EquipmentSlotEmbeddedState.Ready && pendingSource.SourceProblem.Length > 0 &&
            EquipmentSlotEmbeddedPayload.Encode(pendingSource.Working!) == current,
            "old unresolved recovery escrow is not interpreted or replayed; valid current stays usable");
        var incomplete = EquipmentSlotDocumentStore.CreateEmpty(scope); incomplete.SchemaVersion = 3;
        incomplete.Slots[0] = full.Slots[0].Clone(); incomplete.Slots[0].ItemState = null;
        var incompleteSource = Session(Make("incomplete-source", incomplete));
        Check(incompleteSource.State == EquipmentSlotEmbeddedState.Ready && incompleteSource.SourceProblem.Length > 0 &&
            EquipmentSlotEmbeddedPayload.Encode(incompleteSource.Working!) == current,
            "incomplete old item state is not manufactured; valid current stays usable");
        Check(Session(null, exists: false).State == EquipmentSlotEmbeddedState.Ready,
            "Missing with no source prepares empty Working without a sidecar");
        Check(File.ReadAllBytes(failSource.Path).SequenceEqual(Serialize(full)), "cleanup failure preserves original bytes");
        File.WriteAllText(Path.Combine(output, "confirmed-adoption-results.json"), JsonSerializer.Serialize(new
        {
            passed = true, boundary = "Actual product pure classes and Windows file handles; commit observations are models, not SaveGame.", checks
        }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("PASS: " + checks.Count + " confirmed adoption checks; actual Windows handle cleanup, modeled native commit.");
    }
}
