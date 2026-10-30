using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using DTMAPI.Core.Services;
using DTMAPI.MoreEquipmentSlots;

internal static class InputSessionChecks
{
    internal static void Run(string root, string output, string sample)
    {
        var checks = new List<string>();
        void Check(bool value, string detail) { if (!value) throw new InvalidOperationException(detail); checks.Add(detail); }
        void Reject(Action action, string detail)
        { bool rejected = false; try { action(); } catch (InvalidOperationException) { rejected = true; } catch (IOException) { rejected = true; } Check(rejected, detail); }
        string fixture = Path.Combine(root, "input-session"); Directory.CreateDirectory(fixture);
        const string name = "equipment-slots-DTMAPI.MoreEquipmentSlotsMod.json";
        const string scoped = "protected-items/equipment-slots/slot-4/" + name;
        string original = File.ReadAllText(sample);
        JsonObject Copy() => JsonNode.Parse(original)!.AsObject();
        JsonArray Empty() { var slots = Copy()["slots"]!.DeepClone().AsArray(); foreach (var entry in slots) { var s = entry!.AsObject(); s["itemId"] = ""; s["displayName"] = ""; s["skillId"] = ""; s["defenseBonus"] = 0; s["isShield"] = false; s["shieldValue"] = 0; s["shieldMaxValue"] = 0; s["shieldDefend"] = 0; s.Remove("itemState"); } return slots; }
        EquipmentSlotLegacySource? Read(string id, string raw, out string path)
        {
            string directory = Path.Combine(fixture, id); Directory.CreateDirectory(directory); path = Path.Combine(directory, name);
            File.WriteAllText(path, raw, new UTF8Encoding(false));
            var source = EquipmentSlotLegacySource.Capture(new ProtectedSourceFileService(directory, () => { }, () => { }), name, directory, out string error);
            Check(error.Length == 0 && File.ReadAllText(path) == raw, id + ": captured bytes unchanged"); return source;
        }
        void Bad(string id, JsonObject value)
        { string raw = value.ToJsonString(); using var src = Read(id, raw, out string path); Check(src?.Slots == null && src?.Error.Length > 0 && File.ReadAllText(path) == raw, id + ": diagnosed without defaulting or rewriting"); }
        using (var src = Read("historical-product4", original, out _))
            Check(src?.Slots?.Count == 3 && src.Slots[0].ShieldValue == 3 && src.Slots[1].ItemState?.Filled == true && src.Slots[2].ItemState?.Cooldown == 144, "D6 concrete Product4 instance fields preserved");
        var zero = Copy(); zero["slots"]![0]!["shieldValue"] = 0; zero["slots"]![1]!["itemState"]!["filled"] = false; zero["slots"]![2]!["itemState"]!["cooldown"] = 0;
        using (var src = Read("explicit-zero-false", zero.ToJsonString(), out _))
            Check(src?.Slots?[0].ShieldValue == 0 && src.Slots[1].ItemState?.Filled == false && src.Slots[2].ItemState?.Cooldown == 0, "explicit zero/false is valid and differs from missing state");
        foreach (int schema in new[] { 3, 4 })
        {
            var empty = Copy(); empty["schemaVersion"] = schema; empty["slots"] = Empty();
            using var src = Read("empty-product" + schema, empty.ToJsonString(), out _);
            Check(src?.Slots?.All(s => !s.IsOccupied) == true, "complete Product" + schema + " empty is available, not invalid");
        }
        foreach (string field in new[] { "shieldValue", "shieldMaxValue", "shieldDefend", "isShield", "index" })
        { var bad = Copy(); bad["slots"]![0]!.AsObject().Remove(field); Bad("missing-" + field, bad); }
        var oldDefault = Copy(); oldDefault["slots"]![0]!.AsObject().Remove("shieldValue");
        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(oldDefault.ToJsonString())))
        {
            var decoded = (EquipmentSlotStorageDocument)new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(EquipmentSlotStorageDocument)).ReadObject(stream)!;
            Check(EquipmentSlotDocumentStore.TryValidateRawDocument(decoded, decoded.Scope, out _) && EquipmentSlotEmbeddedPayload.Validate(decoded.Slots, out _) && decoded.Slots[0].ShieldValue == 0,
                "D7 DTO/value-only validation demonstrably accepts absent shieldValue as zero; new byte-shape check is required");
        }
        foreach (var item in new[] { (1, "filled"), (2, "cooldown"), (0, "instanceId"), (0, "nativeType") })
        { var bad = Copy(); bad["slots"]![item.Item1]!["itemState"]!.AsObject().Remove(item.Item2); Bad("missing-" + item.Item2, bad); }
        var wrongType = Copy(); wrongType["slots"]![0]!["shieldValue"] = "3"; Bad("string-shield-value", wrongType);
        var nullValue = Copy(); nullValue["slots"]![0]!["shieldValue"] = null; Bad("null-shield-value", nullValue);
        var noState = Copy(); noState["schemaVersion"] = 3; foreach (var s in noState["slots"]!.AsArray()) s!.AsObject().Remove("itemState"); Bad("historical-product3-incomplete", noState);
        var unknown = Copy(); unknown["schemaVersion"] = 99; Bad("unknown-schema", unknown);
        var extra = Copy(); extra["slots"]![0]!["itemState"]!["extraState"] = 9; Bad("unexplained-instance-field", extra);
        var mixed = Copy(); mixed["ownerId"] = "DTMAPI.MoreEquipmentSlotsMod"; Bad("mixed-flat-product", mixed);
        foreach (string tx in new[] { "journal", "gameplayCandidate" }) { var bad = Copy(); bad[tx] = new JsonObject { ["phase"] = 0 }; Bad("unexplained-" + tx, bad); }
        using (var src = Read("bad-json", "{", out _)) Check(src?.Slots == null, "invalid JSON never becomes an empty group");
        foreach (string raw in new[] {
            "{\"schemaVersion\":1,\"ownerId\":\"DTMAPI.MoreEquipmentSlotsMod\",\"archiveIndex\":4,\"slots\":[]}",
            "{\"ownerId\":\"DTMAPI.MoreEquipmentSlotsMod\",\"savedAt\":\"2026-09-17T00:00:00Z\",\"slots\":[]}" })
            using (var src = Read("legacy-unsupported-" + checks.Count, raw, out _)) Check(src?.Slots == null, "flat/pre-schema reader is not silently promoted to complete instance support");
        var embedded = new JsonObject { ["schema"] = 1, ["slots"] = Copy()["slots"]!.DeepClone() };
        embedded["slots"]![0]!.AsObject().Remove("shieldValue");
        Check(EquipmentSlotEmbeddedPayload.Read(true, embedded.ToJsonString(), out _, out string detail) == EquipmentSlotEmbeddedReadKind.Invalid && detail.Contains("shieldValue"), "embedded incomplete shield is also preserved, not decoded as zero");

        string selection = Path.Combine(fixture, "selection"); Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(selection, scoped))!);
        var service = new ProtectedSourceFileService(selection, () => { }, () => { });
        void Put(string key, string raw) => File.WriteAllText(Path.Combine(selection, key), raw, new UTF8Encoding(false));
        EquipmentSlotLegacySource? Select(out string error) => EquipmentSlotLegacySource.CaptureForSlot(service, 4, selection, out error);
        Put(scoped, original);
        using (var src = Select(out string error)) Check(error.Length == 0 && src?.Path == Path.Combine(selection, scoped.Replace('/', Path.DirectorySeparatorChar)), "single scoped source selected by exact canonical path");
        Put(name, original);
        using (var src = Select(out string error)) Check(src == null && error.Contains("多个"), "two valid sources stop; neither wins");
        Put(scoped, "{");
        using (var src = Select(out string error)) Check(src == null && error.Length > 0, "bad scoped plus readable global stops; no fallback");
        using (var locked = new FileStream(Path.Combine(selection, scoped), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        using (var src = Select(out string error)) Check(src == null && error.Length > 0, "unreadable scoped plus readable global stops; no fallback");
        File.Delete(Path.Combine(selection, scoped));
        using (var src = Select(out string error)) Check(error.Length == 0 && src?.Path == Path.Combine(selection, name), "single global Product source selected by path, not internal scope");
        using (var src = Select(out _)) { Put(scoped, original); Check(!src!.IsUnchanged(), "another source appearing invalidates previously single source"); }
        File.Delete(Path.Combine(selection, scoped));
        object archive = new(); string payload = EquipmentSlotEmbeddedPayload.Encode(EquipmentSlotStorageDocument.CreateEmptySlots());
        EquipmentSlotEmbeddedSession Session(EquipmentSlotLegacySource source) => new(archive, 3, "native4.data", true, payload, source);
        using (var src = Select(out _))
        using (var session = Session(src!)) { File.Delete(Path.Combine(selection, name)); Reject(() => session.Choose(true), "deleted displayed source invalidates adopt"); Check(session.State == EquipmentSlotEmbeddedState.Unavailable && session.Working == null && session.Message.Contains("已改变"), "stale source has actionable unavailable message and no Working"); }
        Put(name, original);
        using (var src = Select(out _))
        using (var session = Session(src!)) { Put(name, original + " "); Reject(session.Defer, "changed displayed source also invalidates old cancel"); }
        using (var src = Select(out _))
        using (var session = Session(src!)) { Reject(() => session.ValidateContext(archive, 4, "native4.data", true, payload), "real epoch contract refuses old context"); Check(session.Message.Contains("已改变"), "context expiry exposes reason to reopen UI"); }
        File.WriteAllText(Path.Combine(output, "input-session-results.json"), JsonSerializer.Serialize(new { passed = true, sample, sampleSha256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(sample))), boundary = "Product reader/session plus actual protected files. UI delegate expiry is checked separately in native runtime; no native save here.", checks }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("PASS: " + checks.Count + " input/session observations.");
    }
}
