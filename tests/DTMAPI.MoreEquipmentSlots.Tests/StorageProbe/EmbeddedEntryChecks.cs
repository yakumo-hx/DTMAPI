using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using DTMAPI.Core.Services;
using DTMAPI.MoreEquipmentSlots;

internal static class EmbeddedEntryChecks
{
    internal static void Run(string root, string output, string sample)
    {
        var checks = new List<string>();
        void Check(bool value, string text) { if (!value) throw new Exception(text); checks.Add(text); }
        const string name = "equipment-slots-DTMAPI.MoreEquipmentSlotsMod.json";
        string dir = Path.Combine(root, "entry"); Directory.CreateDirectory(dir);
        string scoped = Path.Combine(dir, "protected-items", "equipment-slots", "slot-11", name);
        string global = Path.Combine(dir, name);
        var files = new ProtectedSourceFileService(dir, () => { }, () => { });
        bool Select(bool exists, bool candidate, out EquipmentSlotLegacySource? source, out string error) =>
            EquipmentSlotEmbeddedEntry.TrySelect(exists, candidate, files, 11, dir, out source, out error);
        using (var source = Read(false, false, true, "three proven Missing sources select native backend without candidate"))
        using (var session = new EquipmentSlotEmbeddedSession(new object(), 1, "native11", false, null, source))
            Check(session.State == EquipmentSlotEmbeddedState.Ready && session.Working!.All(s => !s.IsOccupied), "normal source-free entry creates only an empty Working group");
        Check(!Directory.EnumerateFileSystemEntries(dir).Any(), "normal entry did not create sidecar, migration claims or candidate files");
        foreach (object? raw in new object?[] { null, 17, "{", "{\"schema\":99}" })
        {
            using var source = Read(true, false, true, "present embedded value always owns routing");
            using var session = new EquipmentSlotEmbeddedSession(new object(), 1, "native11", true, raw, source);
            Check(session.State == EquipmentSlotEmbeddedState.Unavailable && session.Working == null, "bad/present embedded data remains unavailable instead of empty/legacy fallback");
        }
        Directory.CreateDirectory(Path.GetDirectoryName(scoped)!);
        string original = File.ReadAllText(sample);
        foreach (string path in new[] { scoped, global })
        {
            File.WriteAllText(path, original);
            using (var source = Read(false, false, true, "complete source enters normal choice: " + Path.GetRelativePath(dir, path)))
            using (var session = new EquipmentSlotEmbeddedSession(new object(), 1, "native11", false, null, source))
            {
                Check(session.State == EquipmentSlotEmbeddedState.NeedsChoice && session.Working == null, "ordinary entry waits for explicit choice");
                session.Choose(true);
                Check(session.Working![0].ShieldValue == 3 && session.Working[1].ItemState!.Filled == true && session.Working[2].ItemState!.Cooldown == 144,
                    "ordinary explicit choice retains complete nonempty state");
            }
            using (var source = Read(false, true, true, "explicit candidate keeps existing adoption rule"))
                Check(source?.Slots?.Count == 3, "explicit candidate captures complete source");
            Check(File.ReadAllText(path) == original, "routing preserves source bytes");
            File.Delete(path);
        }
        foreach (int schema in new[] { 3, 4 })
        {
            var empty = JsonNode.Parse(original)!; empty["schemaVersion"] = schema;
            empty["slots"] = JsonNode.Parse(EquipmentSlotEmbeddedPayload.Encode(EquipmentSlotStorageDocument.CreateEmptySlots()))!["slots"]!.DeepClone();
            File.WriteAllText(global, empty.ToJsonString());
            using var source = Read(false, false, true, "complete Product empty group is a normal explicit choice, schema " + schema);
            using var session = new EquipmentSlotEmbeddedSession(new object(), 1, "native11", false, null, source);
            Check(session.State == EquipmentSlotEmbeddedState.NeedsChoice, "legal empty source is not silently treated as no source");
            session.Choose(true);
            Check(session.Working!.All(s => !s.IsOccupied) && File.Exists(global), "explicit empty choice waits for real commit before disposal");
            File.Delete(global);
        }
        foreach (string unsupported in new[] { "journal", "gameplayCandidate", "missing-state", "unknown-schema", "missing-shield" })
        {
            var raw = JsonNode.Parse(original)!;
            if (unsupported == "missing-state") raw["slots"]![0]!.AsObject().Remove("itemState");
            else if (unsupported == "missing-shield") raw["slots"]![0]!.AsObject().Remove("shieldValue");
            else if (unsupported == "unknown-schema") raw["schemaVersion"] = 99;
            else raw[unsupported] = new JsonObject { ["phase"] = 0 };
            string bytes = raw.ToJsonString(); File.WriteAllText(scoped, bytes);
            using (Read(false, false, true, unsupported + " remains preserved in the single normal route")) { }
            Check(File.ReadAllText(scoped) == bytes, unsupported + " raw input is preserved");
        }
        foreach (string bytes in new[] { "{\"schemaVersion\":1,\"ownerId\":\"DTMAPI.MoreEquipmentSlotsMod\",\"archiveIndex\":11,\"slots\":[]}",
            "{\"ownerId\":\"DTMAPI.MoreEquipmentSlotsMod\",\"savedAt\":\"2026-09-17T00:00:00Z\",\"slots\":[]}" })
        {
            File.WriteAllText(scoped, bytes);
            using (Read(false, false, true, "flat/pre-schema is interpreted or preserved in the single normal route")) { }
            Check(File.ReadAllText(scoped) == bytes, "flat/pre-schema input was neither claimed nor rewritten");
        }
        File.WriteAllText(scoped, "{");
        using (Read(false, false, true, "single malformed source remains unavailable in the normal session")) { }
        using (var locked = new FileStream(scoped, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            Check(Select(false, false, out var source, out var error) && source == null && error.Length > 0, "unreadable is unavailable, never a Missing source");
            using var session = new EquipmentSlotEmbeddedSession(new object(), 1, "native11", false, null, source, error);
            Check(session.State == EquipmentSlotEmbeddedState.Unavailable && session.Working == null, "unreadable path cannot establish empty Working");
        }
        File.WriteAllText(global, original);
        Check(Select(false, false, out var conflict, out var conflictError) && conflict == null && conflictError.Length > 0, "two sources cannot route to empty or choose a winner");
        File.Delete(scoped); File.Delete(global);
        string claims = EquipmentSlotLegacyMigration.GetPreSchemaGlobalClaimDirectory(global);
        foreach (string residual in new[] { scoped + ".previous", Path.Combine(Path.GetDirectoryName(scoped)!, ".legacy-migrations", "retained.flat.json"),
            Path.Combine(claims, "winner.json"), global + ".migrated-product-v3-retained" })
        {
            foreach (string primary in new[] { scoped, global })
            {
                File.WriteAllText(primary, original);
                Directory.CreateDirectory(Path.GetDirectoryName(residual)!); File.WriteAllText(residual, "recovery-input");
                using (Read(false, false, true, "normal routing preserves additional recovery responsibilities: " + Path.GetRelativePath(dir, residual))) { }
                Check(File.ReadAllText(primary) == original && File.ReadAllText(residual) == "recovery-input", "routing neither consumes nor modifies recovery input");
                File.Delete(primary); File.Delete(residual);
            }
            if (residual.Contains(".legacy-migrations")) Directory.Delete(Path.GetDirectoryName(residual)!);
        }
        File.WriteAllText(scoped, original);
        using (var source = Read(false, false, true, "ordinary prompt captured before new recovery input"))
        using (var session = new EquipmentSlotEmbeddedSession(new object(), 1, "native11", false, null, source))
        {
            File.WriteAllText(scoped + ".previous", original);
            bool rejected = false; try { session.Choose(true); } catch (IOException) { rejected = true; }
            Check(rejected && session.Working == null && File.ReadAllText(scoped) == original, "new recovery input invalidates the displayed normal choice");
            File.Delete(scoped + ".previous");
        }
        using (var source = Read(false, false, true, "ordinary source captured for model commit"))
        using (var session = new EquipmentSlotEmbeddedSession(new object(), 1, "native11", false, null, source))
        {
            session.Choose(true); string payload = session.Stage(session.Working!, "before");
            File.WriteAllText(scoped + ".previous", original);
            Check(session.FinishSaved("after", true, payload) && File.ReadAllText(scoped) == original && session.Message.Contains("未清理"),
                "new recovery input after choice preserves source without undoing model native commit");
            File.Delete(scoped + ".previous");
        }
        File.Delete(scoped);
        var previous = System.Text.Json.Nodes.JsonNode.Parse(original)!;
        previous["scope"]!["archiveIndex"] = 11;
        File.WriteAllText(scoped + ".previous", previous.ToJsonString());
        using (Read(false, false, true, "exact previous recovery enters the normal explicit-choice route when canonical sources are Missing")) { }
        var expected = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(EquipmentSlotStorageDocument));
        using (var input = File.OpenRead(scoped + ".previous"))
        {
            var doc = (EquipmentSlotStorageDocument)expected.ReadObject(input)!;
            Check(doc.Scope.ArchiveIndex == 11 && EquipmentSlotDocumentStore.TryLoadValidated(scoped, doc.Scope, out var recovered, out string path, out _) &&
                path == scoped + ".previous" && recovered!.Slots[0].ShieldValue == 3, "unchanged legacy reader still consumes previous as a real recovery input");
        }
        File.Delete(scoped + ".previous");
        Directory.CreateDirectory(claims); string claim = Path.Combine(claims, "winner.json"); File.WriteAllText(claim, "{");
        using (Read(false, false, true, "old migration state remains unavailable, never mistaken for source-free")) { }
        File.Delete(claim);
        foreach (var input in new[] { (slot: (int?)null, fresh: false, current: 11, epoch: (long?)1), (-1, true, 11, 1), (10, true, 11, 1), (11, false, -1, 1), (11, false, 11, null) })
        {
            bool rejected = false;
            try { EquipmentSlotEmbeddedEntry.RequireScope(input.slot, input.fresh, input.current, input.epoch); }
            catch (InvalidDataException) { rejected = true; }
            Check(rejected, "invalid scope rejected: " + input);
        }
        Check(EquipmentSlotEmbeddedEntry.RequireScope(null, true, 11, 1) == 11, "NewGame null event resolves only exact initialized native index");
        Check(EquipmentSlotEmbeddedEntry.RequireScope(11, false, 11, 2) == 11, "ordinary load requires matching explicit slot and active epoch");
        File.WriteAllText(Path.Combine(output, "entry-results.json"), JsonSerializer.Serialize(new { passed = true, checks,
            scope = "Routing with actual protected files and retained legacy reader; native first-save lifecycle is a separate runtime gate." }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("PASS: " + checks.Count + " entry and old-reader isolation checks.");
        EquipmentSlotLegacySource? Read(bool exists, bool candidate, bool expectedRoute, string text)
        {
            Check(Select(exists, candidate, out var source, out var error) == expectedRoute, text);
            if (!exists && (error.Length > 0 || (source != null && source.Slots == null)))
            {
                using var unavailable = new EquipmentSlotEmbeddedSession(new object(), 1, "native11", false, null, source, error);
                Check(unavailable.State == EquipmentSlotEmbeddedState.Unavailable && unavailable.Working == null,
                    "unsupported or unknown source never creates empty Working or a legacy gameplay writer");
            }
            return source;
        }
    }
}
