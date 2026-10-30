using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.Json;
using DTMAPI.MoreEquipmentSlots;
using MesAdoptionFixture;

internal static class AdoptionQualificationChecks
{
    private static readonly List<string> checks = new();
    private static readonly List<object> counterexamples = new();
    private static readonly List<LegacyInput> inputs = new();
    private static readonly EquipmentSlotNativeItem items = new(typeof(MesAdoptionFixture.Api));
    private static string root = "";
    private static EquipmentSlotSaveScope Scope(int index = 2, long clock = 1000) => new()
    { ArchiveIndex = index, PlayerName = "qualification-fixture", CustomPlayerName = "", TotalGameSeconds = clock };
    private static EquipmentSlotStorageDocument Empty() => EquipmentSlotDocumentStore.CreateEmpty(Scope());
    private static Embedded Missing => Embedded.Read(false, null);
    private static Embedded EmptyEmbedded(params string[] adopted) => Embedded.FromSlots(Empty().Slots, adopted);
    private static void Check(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); checks.Add(message); }
    private static Qualification Decide(Embedded current, LegacyInput source, CheckpointObservation? native = null) =>
        QualificationPrototype.Evaluate(current, new[] { source }, native);
    private static string Serialize(EquipmentSlotStorageDocument document)
    {
        using var stream = new MemoryStream();
        new DataContractJsonSerializer(typeof(EquipmentSlotStorageDocument)).WriteObject(stream, document);
        return Encoding.UTF8.GetString(stream.ToArray());
    }
    private static LegacyInput Source(string name, string raw, EquipmentSlotSaveScope? scope = null)
    {
        string path = Path.Combine(root, name + ".json");
        File.WriteAllText(path, raw, new UTF8Encoding(false));
        var source = LegacyInput.Read(path, scope ?? Scope());
        inputs.Add(source);
        return source;
    }
    private static LegacyInput Source(string name, EquipmentSlotStorageDocument document) => Source(name, Serialize(document));
    private static EquipmentSlotStorageDocument Occupied(string id = "box_hat", int index = 0)
    {
        var doc = Empty();
        object item = items.Create(id);
        var slot = new EquipmentSlotStorageEntry
        {
            Index = index, ItemId = id, DisplayName = id,
            ItemState = items.Capture(item, "11111111111111111111111111111111")
        };
        if (id == "box_hat")
        { slot.IsShield = true; slot.ShieldValue = 3; slot.ShieldMaxValue = 100; slot.ShieldDefend = 3; }
        if (id == "herbal_pouch") slot.ItemState.Filled = false;
        if (id == "vulture_badge") slot.ItemState.Cooldown = 144;
        doc.Slots[index] = slot;
        return doc;
    }
    private static CheckpointObservation NativeCheckpoint(string name, string raw, int backpack = 0, int mail = 0)
    {
        string path = Path.Combine(root, name + ".model-native");
        File.WriteAllText(path, raw, new UTF8Encoding(false));
        File.SetLastWriteTimeUtc(path, new DateTime(2026, 10, 3, 0, 0, 0, DateTimeKind.Utc));
        string fingerprint = EquipmentSlotNativeCommitFingerprint.Compute(path, 1);
        return new(fingerprint, _ => new NativeRecoveryObservation(fingerprint, backpack, mail));
    }

    internal static void Run(string parent, string output)
    {
        root = Path.Combine(parent, "adoption-qualification");
        Directory.CreateDirectory(root);
        AdapterIdentityBoundary();
        SameSourceReplay();
        DifferentBytesAndIndistinguishableHistories();
        ChoiceAndMinimumFacts();
        ExactCheckpointAndGameplayTransactions();
        OwnerAndOrphanTransactions();
        FormatsCompatibilityAndPreservation();
        CopyPreviousAndScope();
        foreach (var input in inputs)
            Check(File.ReadAllBytes(input.Path).SequenceEqual(input.Bytes),
                "raw source preserved: " + Path.GetFileName(input.Path));
        string fixtureOutput = Path.Combine(output, "qualification-inputs");
        Directory.CreateDirectory(fixtureOutput);
        foreach (string path in Directory.GetFiles(root))
            File.Copy(path, Path.Combine(fixtureOutput, Path.GetFileName(path)), overwrite: true);
        var report = new
        {
            passed = true,
            boundary = "Offline model and actual DTMAPI reader/adapter/coordinator code; no native SaveGame, production entry, UI or cloud run.",
            checks,
            counterexamples,
            sources = inputs.Select(i => new
            {
                file = "qualification-inputs/" + Path.GetFileName(i.Path), i.Digest,
                format = i.Format.ToString(), readable = i.Document != null, i.Failure
            }),
            conclusion = "A source digest set blocks known-source replay only. Unknown nonempty snapshots do not establish exclusive custody. Exact historical transaction checkpoints are a narrow first-import witness, not general B-machine conflict permission."
        };
        File.WriteAllText(Path.Combine(output, "adoption-qualification-results.json"),
            JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("PASS: " + checks.Count + " adoption qualification assertions; " +
            counterexamples.Count + " explicit counterexamples; no production/native acceptance claimed.");
    }

    private static void AdapterIdentityBoundary()
    {
        foreach (string id in new[] { "box_hat", "herbal_pouch", "vulture_badge" })
        {
            var slot = Occupied(id).Slots[0];
            object restored = items.Restore(slot);
            var captured = items.Capture(restored, slot.ItemState!.InstanceId);
            Check(EquipmentSlotItemState.Same(slot.ItemState, captured), "actual adapter preserves complete state: " + id);
            if (id == "box_hat") Check(((DolocTown.ItemHatShield)restored).shieldValue == 3, "actual adapter preserves worn shield");
            Check(items.Capture(restored).InstanceId != slot.ItemState.InstanceId,
                "actual Capture without session map does not recover old MES GUID: " + id);
            Check(restored.GetType().GetField("instanceId") == null && restored.GetType().GetProperty("InstanceId") == null,
                "restored fixture has no stamped MES identity: " + id);
        }
    }

    private static void SameSourceReplay()
    {
        var y = Source("y-adopted", Occupied());
        // Model: Y was imported, then moved to backpack and saved as Z. A new process
        // reads a native-shaped item without the old session's nativeItems dictionary.
        var nativeBackpackItem = (DolocTown.ItemHatShield)items.Restore(y.Document!.Slots[0]);
        var coldBackpackItem = new DolocTown.ItemHatShield
        { name = nativeBackpackItem.name, count = 1, shieldValue = nativeBackpackItem.shieldValue };
        Embedded zWithoutFacts = EmptyEmbedded();
        Embedded zWithFacts = EmptyEmbedded(y.Digest);
        var survivingItems = new List<object> { coldBackpackItem };
        int beforeAlive = survivingItems.Count;
        var replay = (DolocTown.ItemHatShield)items.Restore(y.Document.Slots[0]);
        survivingItems.Add(replay);
        Check(!ReferenceEquals(coldBackpackItem, replay) && coldBackpackItem.name == replay.name,
            "counterexample 1: zero-fact restore makes a second native-shaped item after transfer");
        Check(Decide(zWithFacts, y).Option == LocalOption.AlreadyAdopted,
            "known source fact blocks Y after model commit and cleanup failure/crash");
        Check(!Decide(zWithoutFacts, y).CanStageLocal,
            "zero facts are safe only when unexplained nonempty adoption is withheld");
        Check(!File.Exists(y.Path + ".previous"), "experiment never migrates or publishes a sidecar");
        // With the native item consumed, Restore is resurrection instead of concurrent duplication.
        var consumedHistory = new VisibleHistory(zWithFacts);
        consumedHistory.TransferFromOldSlots(y.Document.Slots[0]);
        consumedHistory.ConsumeBackpack();
        object resurrected = items.Restore(y.Document.Slots[0]);
        Check(resurrected is DolocTown.ItemHatShield, "counterexample 1: consumed history can also be resurrected");
        counterexamples.Add(new
        {
            id = "same-source-after-transfer-or-consumption", source = y.Digest,
            transfer = new { beforeAlive, unsafeAfterAlive = survivingItems.Count },
            consumption = new { beforeAlive = consumedHistory.Backpack.Count, consumed = consumedHistory.Consumed,
                unsafeAfterAlive = consumedHistory.Backpack.Append(resurrected).Count() },
            zeroFacts = "unsafe if local adoption remains enabled", exactSourceSet = "blocks",
            restrictedQualification = "blocks without inventing item identity"
        });
    }

    private static void DifferentBytesAndIndistinguishableHistories()
    {
        var a = Source("a-history", Occupied());
        var changed = Occupied();
        changed.Generation = 19; // Same historical item, different legitimate old snapshot bytes.
        changed.Slots[0].ShieldValue = 2;
        var b = Source("b-first-unseen-history", changed);
        var z = EmptyEmbedded(a.Digest);
        Check(a.Digest != b.Digest && !z.Value!.AdoptedSources!.Contains(b.Digest),
            "counterexample 2: B first encounters different bytes containing the same historical item");
        object alreadyInBackpack = items.Restore(a.Document!.Slots[0]);
        object duplicate = items.Restore(b.Document!.Slots[0]);
        Check(!ReferenceEquals(alreadyInBackpack, duplicate),
            "counterexample 2: exact-source-only eligibility creates two items");
        Check(Decide(z, b).Option == LocalOption.InsufficientEvidence,
            "restricted qualification refuses unknown B even when source hash is new");

        changed.Slots[0].ItemState!.InstanceId = "99999999999999999999999999999999";
        var recaptured = Source("b-recaptured-guid", changed);
        Check(Decide(z, recaptured).Option == LocalOption.InsufficientEvidence,
            "new MES snapshot GUID does not turn historical item into unique property");

        // Two possible histories have exactly the same available data. No hidden oracle
        // label is passed to the qualifier. The consumed item cannot be found by scanning.
        var freshHistory = new VisibleHistory(z);
        var consumedHistory = new VisibleHistory(z);
        // This may have happened under the OLD sidecar backend; no new-format source
        // fact for B exists. The new-format facts for other sources do not explain B.
        consumedHistory.TransferFromOldSlots(b.Document.Slots[0]);
        consumedHistory.ConsumeBackpack();
        string observedFresh = freshHistory.Observe(b);
        string observedConsumed = consumedHistory.Observe(b);
        Check(freshHistory.Consumed == 0 && consumedHistory.Consumed == 1,
            "indistinguishability fixture executes different transfer/consume histories");
        Check(observedFresh == observedConsumed,
            "fresh-owned and already-consumed histories are observationally indistinguishable");
        counterexamples.Add(new
        {
            id = "different-source-same-history", a = a.Digest, b = b.Digest,
            sameSnapshotGuid = a.Document.Slots[0].ItemState!.InstanceId == b.Document.Slots[0].ItemState!.InstanceId,
            zeroFacts = "duplicates/resurrects", exactSourceSet = "misses B", restrictedQualification = "withholds B",
            indistinguishable = new[] { "B independently owns an unimported item", "same item was imported from another revision and consumed" },
            availableInputEqual = observedFresh == observedConsumed
        });
    }

    private static void ChoiceAndMinimumFacts()
    {
        var local = Source("choice-items", Occupied());
        var same = Embedded.FromSlots(local.Document!.Slots);
        Check(Decide(same, local).Option == LocalOption.ReuseCurrentGroup,
            "identical complete group reuses current items; it does not re-restore legacy items");
        var differentState = Occupied(); differentState.Slots[0].ShieldValue = 2;
        var localDifferent = Source("choice-state-differs", differentState);
        Check(Decide(same, localDifferent).Option == LocalOption.InsufficientEvidence,
            "same GUID with different mutable state is not a safe whole-group adoption proof");
        var empty = Source("choice-empty", Empty());
        var occupiedWithFact = Embedded.FromSlots(local.Document.Slots, new[] { local.Digest });
        Check(Decide(occupiedWithFact, empty).Option == LocalOption.ChooseEmptyGroup,
            "valid empty local group can be explicitly chosen without minting items");
        Embedded working = QualificationPrototype.StageLocal(occupiedWithFact, empty);
        Check(working.Value!.Slots!.All(s => !s.IsOccupied) && occupiedWithFact.Value!.Slots![0].IsOccupied,
            "empty choice only changes model Working, leaving loaded committed input intact");
        Embedded reloaded = Embedded.Read(true, working.Raw); // Model persistence, NOT native commit.
        Check(reloaded.Value!.AdoptedSources!.Count == 2 && Decide(reloaded, local).Option == LocalOption.AlreadyAdopted,
            "all prior adopted source facts survive a subsequent choice and model reload");
        Embedded lastOnly = EmptyEmbedded(empty.Digest);
        Check(!lastOnly.Value!.AdoptedSources!.Contains(local.Digest),
            "last-source-only record forgets Y and cannot identify its replay");
        Check(Decide(EmptyEmbedded(), local).Option == LocalOption.InsufficientEvidence,
            "valid embedded empty does not imply permission to fill from old local items");
        Check(QualificationPrototype.Evaluate(same, new[] { local, localDifferent }).Option == LocalOption.Unavailable,
            "multiple unexplained legacy authorities are preserved rather than guessed by generation");
        foreach (object raw in new object[] { "{broken", "{\"schemaVersion\":99,\"future\":\"保全\"}", 7,
            "42", "[]", "{\"schemaVersion\":\"1\"}" })
        {
            var embedded = Embedded.Read(true, raw);
            Check(embedded.Kind == ReadKind.Invalid || embedded.Kind == ReadKind.Unsupported,
                "present bad/unknown/non-string embedded input is distinct from Missing");
            Check(Decide(embedded, local).Option == LocalOption.Unavailable,
                "bad embedded input cannot be overwritten by readable local source");
            if (raw is string text) Check(embedded.Raw == text, "embedded original text preserved");
            else Check(Equals(embedded.Original, raw), "non-string embedded original value preserved");
        }
    }

    private sealed class VisibleHistory
    {
        private readonly Embedded current;
        internal readonly List<DolocTown.Item> Backpack = new();
        internal int Consumed { get; private set; } // Test oracle, deliberately not serialized native data.
        internal VisibleHistory(Embedded current) => this.current = current;
        internal void TransferFromOldSlots(EquipmentSlotStorageEntry slot) => Backpack.Add((DolocTown.Item)items.Restore(slot));
        internal void ConsumeBackpack() { Consumed += Backpack.Count; Backpack.Clear(); }
        internal string Observe(LegacyInput source) => JsonSerializer.Serialize(new
        {
            current = current.Raw,
            nativeBackpack = Backpack.Select(i => new { i.name, i.count,
                shield = i is DolocTown.ItemHatShield shield ? shield.shieldValue : 0 }),
            old = Convert.ToHexString(source.Bytes)
        });
    }

    private static void ExactCheckpointAndGameplayTransactions()
    {
        var pre = NativeCheckpoint("gameplay-pre", "{\"model\":\"before\",\"backpack\":0}");
        var post = NativeCheckpoint("gameplay-post", "{\"model\":\"after\",\"backpack\":0}");
        var doc = Occupied();
        var workingSlots = Empty().Slots;
        var candidate = EquipmentSlotGameplayCandidateCoordinator.Prepare(doc, workingSlots, pre.Fingerprint, pre.Observe);
        candidate.TransactionId = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        var prepared = Source("gameplay-prepared", doc);
        Check(prepared.Document != null, "real Product reader accepts typed prepared GameplayMutation fixture");
        Check(Decide(Missing, prepared).Option == LocalOption.Unavailable, "unobserved gameplay transaction cannot be imported");
        Check(Decide(Missing, prepared, pre).Option == LocalOption.AdoptExactLegacyCheckpoint,
            "exact old native preimage permits committed slots and discards abandoned Working");
        Embedded staged = QualificationPrototype.StageLocal(Missing, prepared, pre);
        Check(staged.Value!.Slots![0].IsOccupied && Missing.Value == null,
            "first-import model stages committed item without committing or replaying abandoned empty choice");
        Embedded gameplayAfterChoice = Embedded.FromSlots(Empty().Slots, staged.Value.AdoptedSources);
        Embedded modelReload = Embedded.Read(true, gameplayAfterChoice.Raw);
        Check(modelReload.Value!.Slots!.All(s => !s.IsOccupied) &&
            Decide(modelReload, prepared).Option == LocalOption.AlreadyAdopted,
            "latest Working and source fact travel together even when item is consumed before model save");
        Check(EquipmentSlotGameplayCandidateCoordinator.DecideRecovery(prepared.Document!, post.Fingerprint, post.Observe).Action ==
            EquipmentSlotGameplayRecoveryAction.PromoteCommitted,
            "old policy can infer promotion from changed native fingerprint and matching counts");
        Check(Decide(Missing, prepared, post).Option == LocalOption.Unavailable,
            "changed fingerprint plus counts is insufficient for new cross-history adoption qualification");

        var tombstoneDoc = Empty();
        var tombstone = EquipmentSlotGameplayCandidateCoordinator.Prepare(tombstoneDoc, Occupied().Slots,
            pre.Fingerprint, pre.Observe);
        tombstone.TransactionId = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
        EquipmentSlotGameplayCandidateCoordinator.PromoteCommitted(tombstoneDoc, post.Fingerprint);
        var committed = Source("gameplay-committed-tombstone", tombstoneDoc);
        Check(Decide(Missing, committed, post).Option == LocalOption.AdoptExactLegacyCheckpoint,
            "exact retained old gameplay tombstone permits bounded first import");
        Check(Decide(Missing, committed, pre).Option == LocalOption.Unavailable,
            "tombstone for another native revision is not a checkpoint witness");
        var wrongCounts = new CheckpointObservation(post.Fingerprint, _ => new NativeRecoveryObservation(post.Fingerprint, 1, 0));
        Check(Decide(Missing, committed, wrongCounts).Option == LocalOption.Unavailable,
            "checkpoint witness still requires existing transaction destination evidence");
        Check(Decide(EmptyEmbedded(), committed, post).Option == LocalOption.InsufficientEvidence,
            "old checkpoint witness is not permission to replace an existing embedded history");
        candidate.Origin = EquipmentSlotTransactionOrigin.Unknown;
        var unknown = Source("gameplay-unknown-origin", doc);
        Check(unknown.Document == null, "actual old decoder rejects unknown gameplay origin");
    }

    private static void OwnerAndOrphanTransactions()
    {
        foreach (var origin in new[] { EquipmentSlotTransactionOrigin.OwnerRecovery, EquipmentSlotTransactionOrigin.OrphanRecovery })
        {
            var pre = NativeCheckpoint(origin + "-pre", "{\"model\":\"before-recovery\",\"backpack\":0}");
            var post = NativeCheckpoint(origin + "-post", "{\"model\":\"after-recovery\",\"backpack\":1}", backpack: 1);
            var doc = Occupied();
            var journal = EquipmentSlotTransactionCoordinator.PrepareRecovery(doc, new[] { 0 }, origin);
            journal.TransactionId = "cccccccccccccccccccccccccccccccc";
            var pending = Source(origin + "-unstarted", doc);
            Check(pending.Document != null && pending.Document.Slots.All(s => !s.IsOccupied), "prepared recovery has empty slots but escrow: " + origin);
            Check(Decide(Missing, pending, pre).Option == LocalOption.Unavailable,
                "empty projection with unresolved escrow cannot be offered as empty: " + origin);
            EquipmentSlotTransactionCoordinator.StartAttempt(doc, pre.Fingerprint, pre.Observe);
            var started = Source(origin + "-outcome-unknown", doc);
            Check(Decide(Missing, started, post).Option == LocalOption.Unavailable,
                "mutation/throw window without immediate destination evidence stays unresolved: " + origin);
            EquipmentSlotTransactionCoordinator.RecordPlacement(journal, 0,
                new NativePlacementResult(NativePlacementKind.Backpack, 1, 0, "fixture-observation"), post.Observe("box_hat"));
            EquipmentSlotTransactionCoordinator.PromoteCommittedTombstone(doc, post.Fingerprint);
            var committed = Source(origin + "-tombstone", doc);
            Check(Decide(Missing, committed, post).Option == LocalOption.ChooseEmptyGroup,
                "exact recovery postimage finalizes once and excludes returned escrow from import: " + origin);
            Check(committed.Document!.Journal != null,
                "qualification does not mutate retained source journal: " + origin);
            Check(Decide(Missing, committed, pre).Option == LocalOption.Unavailable,
                "recovery tombstone cannot follow native rollback blindly: " + origin);
        }
    }

    private static string Flat(int schema) => "{\"schemaVersion\":" + schema +
        ",\"ownerId\":\"DTMAPI.MoreEquipmentSlotsMod\",\"storageScope\":\"slot-2\",\"archiveIndex\":2," +
        "\"playerName\":\"qualification-fixture\",\"customPlayerName\":\"\",\"savedTotalGameSeconds\":1000,\"generation\":7," +
        "\"slots\":[{\"index\":0,\"itemId\":\"grandmas_button\",\"displayName\":\"Button\"}," +
        "{\"index\":1,\"itemId\":\"\",\"displayName\":\"\"},{\"index\":2,\"itemId\":\"\",\"displayName\":\"\"}]}";
    private static string Global() => "{\"ownerId\":\"DTMAPI.MoreEquipmentSlotsMod\",\"savedAt\":\"2026-06-01T01:02:03Z\"," +
        "\"slots\":[{\"index\":0,\"slotId\":\"dtmapi.extra.1\",\"itemId\":\"grandmas_button\",\"displayName\":\"Button\",\"skillId\":\"\",\"lastMessage\":\"stored\"}," +
        "{\"index\":1,\"slotId\":\"dtmapi.extra.2\",\"itemId\":\"\",\"displayName\":\"\",\"skillId\":\"\",\"lastMessage\":\"empty\"}," +
        "{\"index\":2,\"slotId\":\"dtmapi.extra.3\",\"itemId\":\"\",\"displayName\":\"\",\"skillId\":\"\",\"lastMessage\":\"empty\"}]}";

    private static void FormatsCompatibilityAndPreservation()
    {
        var product = Source("host-product", Occupied("grandmas_button"));
        var flat = Source("host-flat", Flat(3));
        var global = Source("host-global", Global());
        foreach (int version in new[] { 1, 2 })
            Check(Source("flat-schema-" + version, Flat(version)).Document != null,
                "actual legacy decoder supports flat schema " + version);
        var oldProduct = Occupied(); oldProduct.SchemaVersion = 3; oldProduct.Slots[0].ItemState = null;
        var v3 = Source("product-schema3", oldProduct);
        Check(v3.Document != null && v3.Document.Slots[0].ItemState == null,
            "Product schema3 decodes without inventing missing instance state");
        Check(Decide(Missing, v3).Option == LocalOption.InsufficientEvidence,
            "decodable incomplete Product schema3 is not a lossless adoption candidate");
        Check(Decide(Missing, flat).Option == LocalOption.InsufficientEvidence,
            "decodable flat source lacks complete instance state");
        Check(Decide(Missing, global).Reason == "unscoped-global-has-no-archive-attribution",
            "global conversion is not archive ownership evidence");
        var bad = Source("bad-json", "{invalid");
        var futureDoc = Occupied(); futureDoc.SchemaVersion = 99;
        var future = Source("future-schema", futureDoc);
        Check(bad.Document == null && future.Document == null,
            "actual decoders preserve and reject bad/unsupported legacy sources");
        var futureEmbedded = Embedded.Read(true, "{\"schemaVersion\":99,\"opaque\":\"保留\"}");
        foreach (var route in new[] { product, flat, global })
        {
            Check(route.Document?.Slots[0].IsOccupied == true, "Compatibility route fixture actually decodes: " + route.Format);
            object alreadyReturned = items.Restore(route.Document!.Slots[0]);
            object ungated = items.Restore(route.Document.Slots[0]);
            Check(!ReferenceEquals(alreadyReturned, ungated),
                "ungated recovery model can duplicate residual data: " + route.Format);
            foreach (Embedded state in new[] { EmptyEmbedded(), futureEmbedded, Embedded.Read(true, "{bad"), Embedded.Read(true, 42) })
                Check(!QualificationPrototype.MayEnterOldCompatibilityRecovery(MoreEquipmentSlotsProductContract.UniqueId, state),
                    "proposed presence gate blocks old recovery with " + state.Kind + ": " + route.Format);
            Check(QualificationPrototype.MayEnterOldCompatibilityRecovery(MoreEquipmentSlotsProductContract.UniqueId, Missing),
                "Missing delegates to existing old recovery qualification, not unconditional restore: " + route.Format);
        }
        Check(QualificationPrototype.MayEnterOldCompatibilityRecovery("Unrelated.Owner", futureEmbedded),
            "MES model presence gate does not change another owner's recovery contract");
    }

    private static void CopyPreviousAndScope()
    {
        var source = Source("scope-original", Occupied());
        string carried = EmptyEmbedded(source.Digest).Raw!;
        string nativeCopyModel = JsonSerializer.Serialize(new { archiveIndex = 13, playerName = "renamed", mes = carried });
        using var parsed = JsonDocument.Parse(nativeCopyModel);
        Embedded copy = Embedded.Read(true, parsed.RootElement.GetProperty("mes").GetString());
        Check(Decide(copy, source).Option == LocalOption.AlreadyAdopted,
            "copy/rename model retains embedded source facts; copied GUID is not new item identity");
        Check(Decide(Missing, source).Option == LocalOption.InsufficientEvidence,
            "pre-migration prev model has no facts and cannot borrow latest residual sidecar");
        Check(LegacyInput.Read(source.Path, Scope(clock: 5000)).Document != null,
            "actual old reader still accepts same-slot same-name later-clock recreation counterexample");
        Check(LegacyInput.Read(source.Path, Scope(index: 13)).Document == null,
            "source slot matching is distinct from carrying the embedded state on native copy");
        string brokenPath = Path.Combine(root, "broken-with-previous.json");
        File.WriteAllText(brokenPath, "{broken", new UTF8Encoding(false));
        File.WriteAllBytes(brokenPath + ".previous", source.Bytes);
        Check(EquipmentSlotDocumentStore.TryLoadValidated(brokenPath, Scope(), out _, out string selected, out _) &&
            selected == brokenPath + ".previous", "existing loader may select valid .previous behind corrupt live source");
        var exact = LegacyInput.Read(brokenPath, Scope()); inputs.Add(exact);
        Check(exact.Document == null && Decide(Missing, exact).Option == LocalOption.Unavailable,
            "new private reader preserves exact bad source and never silently changes its identity to .previous");
        counterexamples.Add(new
        {
            id = "copy-prev-recreation", copy = "carries facts with payload",
            prevBeforeMigration = "facts absent; latest old file remains unproven",
            sameNameSameSlotLaterClockAcceptedByOldReader = true,
            previousFallback = "existing loader succeeds but exact read remains invalid"
        });
    }
}
