using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.Json;
using DTMAPI.MoreEquipmentSlots;
using DTMAPI.Testing;

// Measures existing readers; these observations are not a new production migrator.
internal static class MigrationChecks
{
    static readonly List<string> checks = new List<string>();
    static void Check(bool ok, string text) { if (!ok) throw new Exception(text); checks.Add(text); }
    static string Serialize(EquipmentSlotStorageDocument value)
    {
        using var stream = new MemoryStream();
        new DataContractJsonSerializer(typeof(EquipmentSlotStorageDocument)).WriteObject(stream, value);
        return Encoding.UTF8.GetString(stream.ToArray());
    }
    static EquipmentSlotSaveScope Scope(int slot = 4, string name = "MES fixture", long clock = 1000) =>
        new EquipmentSlotSaveScope { ArchiveIndex = slot, PlayerName = name, CustomPlayerName = "", TotalGameSeconds = clock };
    static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--managed-cleanup-worker")
            return ManagedCleanupBoundaryChecks.Worker(args);
        using var session = DtmApiTestSession.Start("mes-storage-migration-evidence");
        string root = Path.Combine(Path.GetTempPath(), "migration-inputs");
        Directory.CreateDirectory(root);
        string output = Path.GetFullPath(args[0]);
        Directory.CreateDirectory(output);
        if (args.Length == 3 && (args[1] == "--source-capability" || args[1] == "--source-capability-baseline"))
        {
            SourceCapabilityChecks.Run(root, output, args[2], args[1].EndsWith("-baseline", StringComparison.Ordinal));
            session.MarkSucceeded(); return 0;
        }
        if (args.Length == 3 && args[1] == "--embedded-entry")
        {
            EmbeddedEntryChecks.Run(root, output, args[2]);
            session.MarkSucceeded(); return 0;
        }
        if (args.Length == 3 && args[1] == "--input-session")
        {
            InputSessionChecks.Run(root, output, args[2]);
            ConfirmedAdoptionChecks.Run(root, output);
            session.MarkSucceeded(); return 0;
        }
        if (args.Length == 3 && args[1] == "--managed-cleanup-boundary")
        {
            ManagedCleanupBoundaryChecks.Run(root, output, args[2]);
            session.MarkSucceeded(); return 0;
        }
        var sources = new List<object>();
        var document = EquipmentSlotDocumentStore.CreateEmpty(Scope());
        document.Generation = 7;
        document.Slots[0] = new EquipmentSlotStorageEntry { Index = 0, ItemId = "box_hat", DisplayName = "box_hat", IsShield = true, ShieldValue = 3, ShieldMaxValue = 100, ShieldDefend = 3,
            ItemState = new EquipmentSlotItemState { InstanceId = "11111111111111111111111111111111", NativeType = "DolocTown.ItemHatShield" } };
        document.Slots[1] = new EquipmentSlotStorageEntry { Index = 1, ItemId = "herbal_pouch", DisplayName = "herbal_pouch",
            ItemState = new EquipmentSlotItemState { InstanceId = "22222222222222222222222222222222", NativeType = "DolocTown.ItemHerbPackage", Filled = false } };
        document.Slots[2] = new EquipmentSlotStorageEntry { Index = 2, ItemId = "vulture_badge", DisplayName = "vulture_badge",
            ItemState = new EquipmentSlotItemState { InstanceId = "33333333333333333333333333333333", NativeType = "DolocTown.ItemCooldown", Cooldown = 144 } };
        string v4 = Path.Combine(root, "schema4.json");
        File.WriteAllText(v4, Serialize(document));
        Check(EquipmentSlotDocumentStore.TryLoadValidated(v4, Scope(), out var read4, out _, out _), "schema4 reader accepts complete supported state");
        Check(read4!.Slots[0].ShieldValue == 3 && read4.Slots[1].ItemState!.Filled == false && read4.Slots[2].ItemState!.Cooldown == 144, "schema4 worn shield / empty herb / cooldown preserved");
        Check(read4.Slots[2].ItemState!.InstanceId == document.Slots[2].ItemState!.InstanceId, "instance ID preserved");
        sources.Add(new { name = "schema4", raw = File.ReadAllText(v4), payload = Serialize(read4), sha256 = EquipmentSlotLegacyMigration.ComputeFileSha256(v4), attribution = "explicit-disposable-fixture" });
        Check(!EquipmentSlotDocumentStore.TryLoadValidated(v4, Scope(5), out _, out _, out _), "different slot rejected");
        Check(!EquipmentSlotDocumentStore.TryLoadValidated(v4, Scope(name: "renamed"), out _, out _, out _), "old reader rejects a renamed archive");
        Check(EquipmentSlotDocumentStore.TryLoadValidated(v4, Scope(clock: 2000), out _, out _, out _), "counterexample: same-slot same-name newer clock is indistinguishable from a recreated archive");

        // Schema 3 can retain shield metadata but has no filled/cooldown/instance fields.
        var old = EquipmentSlotDocumentStore.CreateEmpty(Scope()); old.SchemaVersion = 3;
        old.Slots[0] = document.Slots[0].Clone(); old.Slots[0].ItemState = null;
        string v3 = Path.Combine(root, "schema3.json"); File.WriteAllText(v3, Serialize(old));
        Check(EquipmentSlotDocumentStore.TryLoadValidated(v3, Scope(), out var read3, out _, out _), "schema3 supported input remains readable");
        Check(read3!.Slots[0].ShieldValue == 3 && read3.Slots[0].ItemState == null, "schema3 preserves existing shield data without inventing unavailable state");
        sources.Add(new { name = "schema3", raw = File.ReadAllText(v3), payload = Serialize(read3), sha256 = EquipmentSlotLegacyMigration.ComputeFileSha256(v3), attribution = "explicit-disposable-fixture" });

        string global = Path.Combine(root, "global.json");
        string legacy = "{\"ownerId\":\"DTMAPI.MoreEquipmentSlotsMod\",\"savedAt\":\"2026-06-01T01:02:03Z\",\"slots\":[{\"index\":0,\"slotId\":\"dtmapi.extra.1\",\"itemId\":\"grandmas_button\",\"displayName\":\"Button\",\"skillId\":\"\"},{\"index\":1,\"slotId\":\"dtmapi.extra.2\",\"itemId\":\"\",\"displayName\":\"\",\"skillId\":\"\"},{\"index\":2,\"slotId\":\"dtmapi.extra.3\",\"itemId\":\"\",\"displayName\":\"\",\"skillId\":\"\"}]}";
        legacy = legacy.Replace("\"skillId\":\"\"}", "\"skillId\":\"\",\"lastMessage\":\"fixture\"}");
        File.WriteAllText(global, legacy);
        Check(EquipmentSlotLegacyMigration.TryConvert(global, Scope(), true, out var globalA, out string digest, out string failure), "historical global payload decodes: " + failure);
        Check(EquipmentSlotLegacyMigration.TryConvert(global, Scope(5, "other archive"), true, out var globalB, out _, out _), "counterexample: identical unscoped global bytes decode for A and B; conversion does not prove ownership");
        Check(globalA!.Slots[0].ItemId == globalB!.Slots[0].ItemId, "unscoped ambiguity concerns the same item");
        sources.Add(new { name = "global", raw = legacy, payload = Serialize(globalA), sha256 = digest, attribution = "explicit-disposable-fixture-only; never automatic player attribution" });
        var result = new { passed = true, checks, findings = new[] {
            "Existing slot/name/clock matching cannot prove archive incarnation.",
            "An unscoped global file needs existing exact migration evidence or explicit attribution.",
            "Schema 3 cannot recover instance fields absent from its bytes.",
            "Do not call LoadOrMigrate for the new backend: it publishes/archives sidecars before a native commit." }, sources };
        File.WriteAllText(Path.Combine(output, "migration-reader-results.json"), JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("PASS: " + checks.Count + " existing-reader observations; automatic attribution remains restricted.");
        AdoptionQualificationChecks.Run(root, output);
        ConfirmedAdoptionChecks.Run(root, output);
        session.MarkSucceeded(); return 0;
    }
}
