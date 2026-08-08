#pragma warning disable CS0618 // These tests intentionally exercise the frozen IEquipmentSlotsApi transaction boundary.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.Json.Nodes;
using DTMAPI.Core.Runtime;
using DTMAPI.GameBridge.DolocTown;
using DTMAPI.ModConfigMenu;
using DTMAPI.MoreEquipmentSlots;

namespace DTMAPI.UnitTests
{
    internal static class EquipmentSlotsCompatibilityHostTests
    {
        internal static void RunAll()
        {
            JournalRecoveryPolicyCoversAllThreeCrashWindows();
            ProductNativeSerializerCoversPreparedAndCommittedColdStates();
            RawV3StorePreservesFallbackAndRoundTripContracts();
            PhysicalHarmonyFixtureExecutesBothOrdersRollbackAndCleanup();
        }

        internal static void RunProductColdStorageOnly()
        {
            MandatoryProxyDiscoversPreviousWithoutDemandingActiveProduct();
            ColdProbeRechecksAfterInitialAbsence();
            ProductionHookDispatchesOneResidentRecoverySession();
            JournalRecoveryPolicyCoversAllThreeCrashWindows();
            ProductNativeSerializerCoversPreparedAndCommittedColdStates();
            RawV3StorePreservesFallbackAndRoundTripContracts();
            PhysicalColdHostFixtureExecutesReplacementCrashStates();
        }

        private static void
            ProductionHookDispatchesOneResidentRecoverySession()
        {
            string sessionRoot =
                Environment.GetEnvironmentVariable(
                    "DTMAPI_TEST_SESSION_ROOT")
                ?? throw new InvalidOperationException(
                    "Resident backend fixtures require the managed DTMAPI test session.");
            string gameRoot =
                Path.Combine(
                    sessionRoot,
                    "equipment-resident-backend-" +
                    Guid.NewGuid().ToString("N"));
            var runtime =
                new DtmApiRuntime(
                    new FakeHost(gameRoot),
                    new ConfigMenuRegistry());
            var bridge =
                new DolocTownGameBridge(runtime);
            EquipmentSlotsService service =
                bridge.EquipmentSlotsService
                ?? throw new InvalidOperationException(
                    "The production GameBridge did not register its EquipmentSlots feature.");
            string previousPath =
                Path.Combine(
                    runtime.Paths.ConfigPath,
                    "protected-items",
                    "equipment-slots",
                    "slot-2",
                    "equipment-slots-DTMAPI.MoreEquipmentSlotsMod.json.previous");
            WriteRawJson(
                previousPath,
                "{}");

            CompatibilityHostBroker broker =
                CompatibilityHostBroker.For(runtime);
            FieldInfo servicesField =
                typeof(CompatibilityHostBroker)
                    .GetField(
                        "services",
                        BindingFlags.Instance |
                        BindingFlags.NonPublic)
                ?? throw new MissingFieldException(
                    "CompatibilityHostBroker.services");
            var services =
                servicesField.GetValue(broker) as
                    Dictionary<string, object>
                ?? throw new InvalidOperationException(
                    "Compatibility broker service dictionary was unavailable.");
            var backend =
                new ResidentEquipmentSlotsBackend(
                    previousPath);
            services.Add(
                "EquipmentSlots",
                backend);

            try
            {
                DolocTownHookCallbacks.Runtime =
                    runtime;
                DolocTownHookCallbacks.Bridge =
                    bridge;
                DolocTownHookCallbacks
                    .AfterLoadArchiveDataPostfix(
                        isNewGame: false);
                bool nativeResult = false;
                bool nativeSaveAllowed =
                    DolocTownHookCallbacks.SaveGamePrefix(
                        2,
                        ref nativeResult);
                Assert(
                    nativeSaveAllowed,
                    "The single recovered EquipmentSlots session must allow the production SaveSaving boundary.");
                DolocTownHookCallbacks.SaveGamePostfix(
                    2,
                    __result: true);
                Assert(
                    backend.SaveLoadedCount == 1 &&
                    backend.RecoveryCount == 1 &&
                    backend.SaveSavingCount == 1 &&
                    backend.SaveSavedCount == 1 &&
                    backend.PreviousObserved &&
                    backend.Terminal,
                    "The production Hook -> feature fanout -> broker route must notify and recover exactly once, then commit the same session at SaveSaved.");

                DolocTownHookCallbacks
                    .ReturnHomePostfix();
                Assert(
                    backend.ReturnedToTitleCount == 1,
                    "The production title Hook must also dispatch the EquipmentSlots feature lifecycle exactly once.");
            }
            finally
            {
                DolocTownHookCallbacks.Bridge =
                    null;
                DolocTownHookCallbacks.Runtime =
                    null;
            }
        }

        private static void
            MandatoryProxyDiscoversPreviousWithoutDemandingActiveProduct()
        {
            string sessionRoot =
                Environment.GetEnvironmentVariable(
                    "DTMAPI_TEST_SESSION_ROOT")
                ?? throw new InvalidOperationException(
                    "Cold proxy fixtures require the managed DTMAPI test session.");
            string configRoot =
                Path.Combine(
                    sessionRoot,
                    "equipment-cold-proxy-" +
                    Guid.NewGuid().ToString("N"));
            string productPrevious =
                Path.Combine(
                    configRoot,
                    "protected-items",
                    "equipment-slots",
                    "slot-2",
                    "equipment-slots-DTMAPI.MoreEquipmentSlotsMod.json.previous");
            WriteRawJson(
                productPrevious,
                "{}");

            string[] absentProductCandidates =
                EquipmentSlotsService
                    .FindColdCompatibilityStorageCandidates(
                        configRoot,
                        _ => false);
            Assert(
                absentProductCandidates.Length == 1 &&
                string.Equals(
                    absentProductCandidates[0],
                    productPrevious,
                    StringComparison.OrdinalIgnoreCase),
                "A lone canonical .json.previous authority must wake the mandatory proxy so Compatibility cold recovery is reachable.");

            string[] activeProductCandidates =
                EquipmentSlotsService
                    .FindColdCompatibilityStorageCandidates(
                        configRoot,
                        ownerId =>
                            string.Equals(
                                ownerId,
                                MoreEquipmentSlotsProductContract
                                    .UniqueId,
                                StringComparison.OrdinalIgnoreCase));
            Assert(
                activeProductCandidates.Length == 0,
                "A normally loaded MoreEquipmentSlots Product must not publish cold Compatibility demand for its own live or previous storage.");

            string otherOwner =
                Path.Combine(
                    configRoot,
                    "protected-items",
                    "equipment-slots",
                    "slot-2",
                    "equipment-slots-Other.Owner.json");
            WriteRawJson(otherOwner, "{}");
            activeProductCandidates =
                EquipmentSlotsService
                    .FindColdCompatibilityStorageCandidates(
                        configRoot,
                        ownerId =>
                            string.Equals(
                                ownerId,
                                MoreEquipmentSlotsProductContract
                                    .UniqueId,
                                StringComparison.OrdinalIgnoreCase));
            Assert(
                activeProductCandidates.Length == 1 &&
                string.Equals(
                    activeProductCandidates[0],
                    otherOwner,
                    StringComparison.OrdinalIgnoreCase),
                "An active Product may suppress only its own cold demand; another orphan owner must still wake Compatibility.");

            string residueRoot =
                Path.Combine(
                    sessionRoot,
                    "equipment-cold-residue-" +
                    Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(residueRoot);
            string capture =
                Path.Combine(
                    residueRoot,
                    "equipment-slots-DTMAPI.MoreEquipmentSlotsMod.json.migration-capture-crash");
            string claimRoot =
                Path.Combine(
                    residueRoot,
                    ".equipment-slot-migration-claims",
                    MoreEquipmentSlotsProductContract.UniqueId);
            Directory.CreateDirectory(claimRoot);
            string winner =
                Path.Combine(claimRoot, "winner.json");
            string transition =
                Path.Combine(
                    claimRoot,
                    "ABC.json.transition-crash");
            string archive =
                Path.Combine(
                    residueRoot,
                    "equipment-slots-DTMAPI.MoreEquipmentSlotsMod.json.migrated-product-v3-" +
                    new string('A', 64));
            string backup =
                Path.Combine(
                    residueRoot,
                    "protected-items",
                    "equipment-slots",
                    "slot-2",
                    ".legacy-migrations",
                    new string('B', 64) +
                    ".flat.json");
            string product =
                Path.Combine(
                    residueRoot,
                    "protected-items",
                    "equipment-slots",
                    "slot-2",
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract
                        .UniqueId +
                    ".json");
            WriteRawJson(capture, "{}");
            WriteRawJson(winner, "{}");
            WriteRawJson(transition, "{}");
            WriteRawJson(archive, "{}");
            WriteRawJson(
                backup,
                "{\"ownerId\":\"" +
                MoreEquipmentSlotsProductContract
                    .UniqueId +
                "\"}");
            WriteRawJson(product, "{}");
            string[] loadedOwnerResidueCandidates =
                EquipmentSlotsService
                    .FindColdCompatibilityStorageCandidates(
                        residueRoot,
                        ownerId =>
                            string.Equals(
                                ownerId,
                                MoreEquipmentSlotsProductContract
                                    .UniqueId,
                                StringComparison.OrdinalIgnoreCase));
            Assert(
                loadedOwnerResidueCandidates.Length == 0,
                "A loaded healthy Product must suppress its canonical Product, capture, archive, backup, winner and transition terminal evidence instead of creating permanent Compatibility Host demand.");

            string[] absentOwnerResidueCandidates =
                EquipmentSlotsService
                    .FindColdCompatibilityStorageCandidates(
                        residueRoot,
                        _ => false);
            Assert(
                absentOwnerResidueCandidates.Length == 6 &&
                absentOwnerResidueCandidates.Contains(
                    capture,
                    StringComparer.OrdinalIgnoreCase) &&
                absentOwnerResidueCandidates.Contains(
                    winner,
                    StringComparer.OrdinalIgnoreCase) &&
                absentOwnerResidueCandidates.Contains(
                    transition,
                    StringComparer.OrdinalIgnoreCase) &&
                absentOwnerResidueCandidates.Contains(
                    archive,
                    StringComparer.OrdinalIgnoreCase) &&
                absentOwnerResidueCandidates.Contains(
                    backup,
                    StringComparer.OrdinalIgnoreCase) &&
                absentOwnerResidueCandidates.Contains(
                    product,
                    StringComparer.OrdinalIgnoreCase),
                "The same Product, capture, archive, backup, winner and transition artifacts must still wake Compatibility when their owner is absent.");

            string trueEmptyRoot =
                Path.Combine(
                    sessionRoot,
                    "equipment-cold-true-empty-" +
                    Guid.NewGuid().ToString("N"));
            Assert(
                EquipmentSlotsService
                    .FindColdCompatibilityStorageCandidates(
                        trueEmptyRoot,
                        _ => false).Length == 0,
                "A true empty configuration root must not create spurious Compatibility Host demand.");
        }

        private static void ColdProbeRechecksAfterInitialAbsence()
        {
            string sessionRoot =
                Environment.GetEnvironmentVariable(
                    "DTMAPI_TEST_SESSION_ROOT")
                ?? throw new InvalidOperationException(
                    "Cold probe fixtures require the managed DTMAPI test session.");
            string gameRoot =
                Path.Combine(
                    sessionRoot,
                    "equipment-cold-reprobe-" +
                    Guid.NewGuid().ToString("N"));
            var runtime =
                new DtmApiRuntime(
                    new FakeHost(gameRoot),
                    new ConfigMenuRegistry());
            var bridge =
                new DolocTownGameBridge(runtime);
            EquipmentSlotsService service =
                bridge.EquipmentSlotsService
                ?? throw new InvalidOperationException(
                    "The production GameBridge did not register its EquipmentSlots feature.");
            Assert(
                !service.HasColdCompatibilityStorage(),
                "A process with no orphan storage must begin without cold Compatibility demand.");

            string orphanPath =
                Path.Combine(
                    runtime.Paths.ConfigPath,
                    "equipment-slots-Other.Owner.json");
            WriteRawJson(
                orphanPath,
                "{}");
            Assert(
                service.HasColdCompatibilityStorage(),
                "A later lifecycle probe in the same process must discover orphan storage created after an earlier false result.");

            File.Delete(orphanPath);
            Assert(
                !service.HasColdCompatibilityStorage(),
                "Cold Compatibility demand must be recomputed at lifecycle boundaries instead of retaining a stale positive or negative process-lifetime cache.");
        }

        private static void
            RawV3StorePreservesFallbackAndRoundTripContracts()
        {
            FutureSchemaAndScopeMismatchDoNotFallback();
            StructuralRawV3FailuresUseExactPrevious();
            CommittedEscrowProjectionFailsClosed();
            ReorderedJournalProjectionFailsClosed();
            IncomingAfterOutgoingFailureFailsClosed();
            AdditiveShieldFieldsAndEscrowTraitsRoundTrip();
        }

        private static void
            FutureSchemaAndScopeMismatchDoNotFallback()
        {
            StoreFallbackFixture future =
                CreateStoreFallbackFixture("future-schema");
            EquipmentSlotStorageDocument futureDocument =
                CreateStoreDocument(
                    "future-live",
                    generation: 2,
                    future.Scope);
            futureDocument.SchemaVersion =
                MoreEquipmentSlotsProductContract
                    .StorageSchemaVersion + 1;
            WriteRawDocument(
                future.Path,
                futureDocument);
            AssertNoFallbackAndNoMutation(
                future,
                "schema-mismatch");

            StoreFallbackFixture mismatched =
                CreateStoreFallbackFixture("scope-mismatch");
            EquipmentSlotStorageDocument mismatchedDocument =
                CreateStoreDocument(
                    "wrong-save-live",
                    generation: 2,
                    new EquipmentSlotSaveScope
                    {
                        ArchiveIndex =
                            mismatched.Scope.ArchiveIndex,
                        PlayerName =
                            mismatched.Scope.PlayerName,
                        CustomPlayerName =
                            mismatched.Scope
                                .CustomPlayerName +
                            "-other",
                        TotalGameSeconds =
                            mismatched.Scope
                                .TotalGameSeconds
                    });
            WriteRawDocument(
                mismatched.Path,
                mismatchedDocument);
            AssertNoFallbackAndNoMutation(
                mismatched,
                "scope-mismatch");
        }

        private static void
            StructuralRawV3FailuresUseExactPrevious()
        {
            StoreFallbackFixture duplicate =
                CreateStoreFallbackFixture(
                    "duplicate-slot-index");
            EquipmentSlotStorageDocument duplicateDocument =
                CreateStoreDocument(
                    "duplicate-live",
                    generation: 2,
                    duplicate.Scope);
            duplicateDocument.Slots[1].Index = 0;
            AssertEligiblePreviousAndNextGeneration(
                duplicate,
                duplicateDocument,
                "duplicate raw v3 slot indexes");

            StoreFallbackFixture outOfRange =
                CreateStoreFallbackFixture(
                    "out-of-range-slot-index");
            EquipmentSlotStorageDocument outOfRangeDocument =
                CreateStoreDocument(
                    "out-of-range-live",
                    generation: 2,
                    outOfRange.Scope);
            outOfRangeDocument.Slots[2].Index =
                MoreEquipmentSlotsProductContract
                    .FixedSlotCount;
            AssertEligiblePreviousAndNextGeneration(
                outOfRange,
                outOfRangeDocument,
                "out-of-range raw v3 slot indexes");

            StoreFallbackFixture badJournal =
                CreateStoreFallbackFixture("bad-journal");
            EquipmentSlotStorageDocument badJournalDocument =
                CreateStoreDocument(
                    "bad-journal-live",
                    generation: 2,
                    badJournal.Scope);
            EquipmentSlotTransactionCoordinator.PrepareRecovery(
                badJournalDocument,
                new[] { 0 });
            badJournalDocument.Journal!.TransactionId =
                "not-a-v3-transaction-id";
            AssertEligiblePreviousAndNextGeneration(
                badJournal,
                badJournalDocument,
                "invalid raw v3 journal identity");
        }

        private static void CommittedEscrowProjectionFailsClosed()
        {
            string path = NewStorePath(
                "committed-escrow-occupied");
            EquipmentSlotSaveScope scope = StoreScope();
            EquipmentSlotStorageDocument document =
                CreateStoreDocument(
                    "escrow-item",
                    generation: 1,
                    scope);
            EquipmentSlotTransactionJournal journal =
                EquipmentSlotTransactionCoordinator
                    .PrepareRecovery(
                    document,
                    new[] { 0 });
            EquipmentSlotTransactionCoordinator.StartAttempt(
                document,
                "save-a",
                _ => StoreObservation(
                    "save-a",
                    backpack: 0,
                    mail: 0));
            EquipmentSlotTransactionCoordinator.RecordPlacement(
                journal,
                0,
                StorePlacement(
                    NativePlacementKind.Backpack),
                StoreObservation(
                    "save-a",
                    backpack: 1,
                    mail: 0));
            EquipmentSlotTransactionCoordinator
                .PromoteCommittedTombstone(
                    document,
                    "save-b");
            document.Slots[0].ItemId =
                "unexpected-sidecar-copy";
            document.Slots[0].DisplayName =
                "unexpected-sidecar-copy";
            WriteRawDocument(path, document);
            byte[] before = File.ReadAllBytes(path);

            Assert(
                !EquipmentSlotDocumentStore.TryReadValidated(
                    path,
                    scope,
                    out _,
                    out string failure) &&
                string.Equals(
                    failure,
                    "journal-committed-escrow-projection-invalid",
                    StringComparison.Ordinal) &&
                EqualBytes(
                    before,
                    File.ReadAllBytes(path)),
                "A committed escrow whose product slot is also occupied must fail closed without rewriting the raw sidecar.");
        }

        private static void
            IncomingAfterOutgoingFailureFailsClosed()
        {
            string path = NewStorePath(
                "incoming-after-outgoing-failure");
            EquipmentSlotSaveScope scope = StoreScope();
            EquipmentSlotStorageDocument document =
                CreateStoreDocument(
                    "outgoing-item",
                    generation: 1,
                    scope);
            EquipmentSlotTransactionJournal journal =
                EquipmentSlotTransactionCoordinator
                    .PrepareReplacement(
                        document,
                        0,
                        new EquipmentSlotStorageEntry
                        {
                            Index = 0,
                            ItemId = "incoming-item",
                            DisplayName = "incoming-item"
                        });
            EquipmentSlotTransactionCoordinator.StartAttempt(
                document,
                "save-a",
                itemId => StoreObservation(
                    "save-a",
                    backpack:
                        itemId == "incoming-item" ? 1 : 0,
                    mail: 0));
            EquipmentSlotTransactionCoordinator.RecordPlacement(
                journal,
                0,
                StorePlacement(
                    NativePlacementKind.Failure),
                StoreObservation(
                    "save-a",
                    backpack: 0,
                    mail: 0));
            EquipmentSlotTransactionCoordinator
                .RecordIncomingWithdrawal(
                    journal,
                    succeeded: true,
                    afterBackpackCount: 0);
            EquipmentSlotTransactionCoordinator
                .PromoteCommittedTombstone(
                    document,
                    "save-b");
            WriteRawDocument(path, document);

            Assert(
                !EquipmentSlotDocumentStore.TryReadValidated(
                    path,
                    scope,
                    out _,
                    out string failure) &&
                string.Equals(
                    failure,
                    "journal-incoming-after-outgoing-failure",
                    StringComparison.Ordinal),
                "A committed replacement cannot consume the incoming item after its outgoing escrow placement failed.");
        }

        private static void
            ReorderedJournalProjectionFailsClosed()
        {
            string path = NewStorePath(
                "reordered-journal-projection");
            EquipmentSlotSaveScope scope = StoreScope();
            EquipmentSlotStorageDocument document =
                CreateStoreDocument(
                    "escrow-item",
                    generation: 1,
                    scope);
            EquipmentSlotTransactionCoordinator.PrepareRecovery(
                document,
                new[] { 0 });
            document.Slots[0].ItemId =
                "duplicate-sidecar-item";
            document.Slots[0].DisplayName =
                "duplicate-sidecar-item";
            EquipmentSlotStorageEntry first =
                document.Slots[0];
            document.Slots[0] = document.Slots[2];
            document.Slots[2] = first;
            WriteRawDocument(path, document);

            Assert(
                !EquipmentSlotDocumentStore.TryReadValidated(
                    path,
                    scope,
                    out _,
                    out string failure) &&
                string.Equals(
                    failure,
                    "slot-order-invalid",
                    StringComparison.Ordinal),
                "Raw v3 slot order must be canonical before any journal projection indexes the list.");
        }

        private static void
            AdditiveShieldFieldsAndEscrowTraitsRoundTrip()
        {
            EquipmentSlotSaveScope scope = StoreScope();
            string oldV3Path =
                NewStorePath("old-v3-shield");
            EquipmentSlotStorageDocument oldV3 =
                CreateStoreDocument(
                    "old-v3-shield",
                    generation: 1,
                    scope);
            SetShieldTraits(
                oldV3.Slots[0],
                "old-skill",
                defense: 4,
                value: 6,
                maximum: 9,
                shieldDefend: 7);
            JsonNode raw =
                JsonNode.Parse(SerializeRaw(oldV3))
                ?? throw new InvalidDataException(
                    "Could not parse the old-v3 shield fixture.");
            foreach (JsonNode? slotNode in
                raw["slots"]!.AsArray())
            {
                slotNode?.AsObject().Remove(
                    "shieldDefend");
            }
            WriteRawJson(
                oldV3Path,
                raw.ToJsonString());
            Assert(
                EquipmentSlotDocumentStore.TryReadValidated(
                    oldV3Path,
                    scope,
                    out EquipmentSlotStorageDocument? oldLoaded,
                    out string oldFailure) &&
                oldLoaded!.SchemaVersion ==
                    MoreEquipmentSlotsProductContract
                        .StorageSchemaVersion &&
                oldLoaded.Slots[0].ShieldDefend == 0,
                "A raw v3 document written before shieldDefend existed must remain readable with the additive field defaulted to zero. failure=" +
                oldFailure);

            string roundTripPath =
                NewStorePath("full-trait-roundtrip");
            EquipmentSlotStorageDocument roundTrip =
                CreateStoreDocument(
                    "old-shield",
                    generation: 1,
                    scope);
            SetShieldTraits(
                roundTrip.Slots[0],
                "old-skill",
                defense: 3,
                value: 5,
                maximum: 8,
                shieldDefend: 2);
            var replacement =
                new EquipmentSlotStorageEntry
                {
                    Index = 0,
                    ItemId = "new-shield",
                    DisplayName = "New shield"
                };
            SetShieldTraits(
                replacement,
                "new-skill",
                defense: 6,
                value: 10,
                maximum: 12,
                shieldDefend: 4);
            EquipmentSlotTransactionCoordinator
                .PrepareReplacement(
                    roundTrip,
                    0,
                    replacement);
            roundTrip.Generation++;
            var store = new EquipmentSlotDocumentStore();
            store.WriteAtomic(
                roundTripPath,
                roundTrip);
            EquipmentSlotStorageDocument loaded =
                store.Load(
                    roundTripPath,
                    scope);
            EquipmentSlotEscrowEntry escrow =
                loaded.Journal!.Escrow.Single();
            EquipmentSlotStorageEntry loadedReplacement =
                loaded.Journal.Replacements.Single();
            Assert(
                escrow.ItemId == "old-shield" &&
                escrow.SkillId == "old-skill" &&
                escrow.DefenseBonus == 3 &&
                escrow.IsShield &&
                escrow.ShieldValue == 5 &&
                escrow.ShieldMaxValue == 8 &&
                escrow.ShieldDefend == 2 &&
                loadedReplacement.ItemId ==
                    "new-shield" &&
                loadedReplacement.SkillId ==
                    "new-skill" &&
                loadedReplacement.DefenseBonus == 6 &&
                loadedReplacement.IsShield &&
                loadedReplacement.ShieldValue == 10 &&
                loadedReplacement.ShieldMaxValue == 12 &&
                loadedReplacement.ShieldDefend == 4,
                "The raw v3 serializer must round-trip every slot, replacement, and escrow trait used to restore ProductNative behavior.");
        }

        private static void
            ProductNativeSerializerCoversPreparedAndCommittedColdStates()
        {
            string sessionRoot =
                Environment.GetEnvironmentVariable(
                    "DTMAPI_TEST_SESSION_ROOT")
                ?? throw new InvalidOperationException(
                    "Product cold-storage fixtures require the managed DTMAPI test session.");
            string root = Path.Combine(
                sessionRoot,
                "equipment-product-cold-storage");
            Directory.CreateDirectory(root);
            string path = Path.Combine(
                root,
                "equipment-slots-DTMAPI.MoreEquipmentSlotsMod.json");
            var scope =
                new EquipmentSlotSaveScope
                {
                    ArchiveIndex = 2,
                    PlayerName = "player",
                    CustomPlayerName = "custom",
                    TotalGameSeconds = 100
                };
            var document =
                new EquipmentSlotStorageDocument
                {
                    Scope = scope.Clone(),
                    Generation = 1
                };
            document.Slots[0].ItemId = "grandmas_button";
            document.Slots[0].DisplayName = "Protected item";
            var store = new EquipmentSlotDocumentStore();
            store.WriteAtomic(path, document);

            EquipmentSlotStorageDocument loaded =
                store.Load(path, scope);
            EquipmentSlotTransactionCoordinator.PrepareRecovery(
                loaded,
                new[] { 0 });
            loaded.Generation++;
            store.WriteAtomic(path, loaded);
            Assert(
                loaded.Slots.All(slot => !slot.IsOccupied) &&
                loaded.Journal?.Escrow.Count == 1 &&
                EquipmentSlotTransactionCoordinator.DecideRecovery(
                    loaded,
                    "save-a",
                    _ => new NativeRecoveryObservation(
                        "save-a",
                        0,
                        0)).Action ==
                    EquipmentSlotRecoveryAction.RetryPlacement,
                "The exact ProductNative serializer must preserve a prepared cold journal with one logical escrow item.");

            var mismatchedScope =
                new EquipmentSlotSaveScope
                {
                    ArchiveIndex = scope.ArchiveIndex,
                    PlayerName = scope.PlayerName,
                    CustomPlayerName =
                        scope.CustomPlayerName + "-other",
                    TotalGameSeconds =
                        scope.TotalGameSeconds
                };
            Assert(
                !EquipmentSlotDocumentStore.TryReadValidated(
                    path,
                    mismatchedScope,
                    out _,
                    out string scopeFailure) &&
                string.Equals(
                    scopeFailure,
                    "scope-mismatch",
                    StringComparison.Ordinal),
                "Cold ProductNative recovery must reject any exact PlayerName or CustomPlayerName scope drift even when the archive index matches.");

            EquipmentSlotTransactionCoordinator.StartAttempt(
                loaded,
                "save-a",
                _ => new NativeRecoveryObservation(
                    "save-a",
                    0,
                    0));
            EquipmentSlotTransactionCoordinator.RecordPlacement(
                loaded.Journal!,
                0,
                new NativePlacementResult(
                    NativePlacementKind.Backpack,
                    1,
                    0,
                    "native backpack"),
                new NativeRecoveryObservation(
                    "save-a",
                    1,
                    0));
            loaded.Generation++;
            store.WriteAtomic(path, loaded);
            Assert(
                EquipmentSlotTransactionCoordinator.DecideRecovery(
                    store.Load(path, scope),
                    "save-b",
                    _ => new NativeRecoveryObservation(
                        "save-b",
                        1,
                        0)).Action ==
                    EquipmentSlotRecoveryAction.FinalizeCommitted,
                "A prepared ProductNative journal must finalize only from the changed save fingerprint and exact native destination.");

            EquipmentSlotTransactionCoordinator
                .PromoteCommittedTombstone(
                    loaded,
                    "save-b");
            loaded.Generation++;
            store.WriteAtomic(path, loaded);
            EquipmentSlotStorageDocument committed =
                store.Load(path, scope);
            Assert(
                committed.Journal?.State ==
                    EquipmentSlotJournalState
                        .CommittedTombstone &&
                EquipmentSlotTransactionCoordinator.DecideRecovery(
                    committed,
                    "save-b",
                    _ => new NativeRecoveryObservation(
                        "save-b",
                        1,
                        0)).Action ==
                    EquipmentSlotRecoveryAction.FinalizeCommitted,
                "The exact ProductNative serializer must preserve a committed tombstone until native evidence is rechecked.");

            EquipmentSlotTransactionCoordinator.FinalizeCommitted(
                committed);
            committed.Generation++;
            store.WriteAtomic(path, committed);
            EquipmentSlotStorageDocument finalized =
                store.Load(path, scope);
            Assert(
                finalized.Journal == null &&
                finalized.Slots.All(slot => !slot.IsOccupied),
                "Final ProductNative cold recovery must leave no journal or sidecar duplicate after the native copy is proven.");
        }

        private static void JournalRecoveryPolicyCoversAllThreeCrashWindows()
        {
            AssertDecision(
                EquipmentSlotJournalRecoveryDecision.RetryPlacement,
                new EquipmentSlotJournalRecoverySnapshot(
                    EquipmentSlotJournalCheckpoint.Prepared,
                    attemptStarted: false,
                    placementRecorded: false,
                    preSaveFingerprint: "save-a",
                    postSaveFingerprint: string.Empty,
                    currentSaveFingerprint: "save-a",
                    beforeBackpackCount: 4,
                    expectedBackpackCount: -1,
                    currentBackpackCount: 4,
                    beforeMailCount: 0,
                    expectedMailCount: -1,
                    currentMailCount: 0),
                "A crash after durable Prepared but before native placement must retry from journal escrow.");

            AssertDecision(
                EquipmentSlotJournalRecoveryDecision.FinalizeCommitted,
                new EquipmentSlotJournalRecoverySnapshot(
                    EquipmentSlotJournalCheckpoint.Prepared,
                    attemptStarted: true,
                    placementRecorded: true,
                    preSaveFingerprint: "save-a",
                    postSaveFingerprint: string.Empty,
                    currentSaveFingerprint: "save-b",
                    beforeBackpackCount: 4,
                    expectedBackpackCount: 5,
                    currentBackpackCount: 5,
                    beforeMailCount: 0,
                    expectedMailCount: 0,
                    currentMailCount: 0),
                "A crash after native save but before NativeCommitted persistence must finalize the one native copy.");

            AssertDecision(
                EquipmentSlotJournalRecoveryDecision.FinalizeCommitted,
                new EquipmentSlotJournalRecoverySnapshot(
                    EquipmentSlotJournalCheckpoint.Committed,
                    attemptStarted: true,
                    placementRecorded: true,
                    preSaveFingerprint: "save-a",
                    postSaveFingerprint: "save-b",
                    currentSaveFingerprint: "save-b",
                    beforeBackpackCount: 4,
                    expectedBackpackCount: 5,
                    currentBackpackCount: 5,
                    beforeMailCount: 0,
                    expectedMailCount: 0,
                    currentMailCount: 0),
                "A crash after Committed persistence but before journal cleanup must only clean the committed journal.");

            AssertDecision(
                EquipmentSlotJournalRecoveryDecision.FinalizeCommitted,
                new EquipmentSlotJournalRecoverySnapshot(
                    EquipmentSlotJournalCheckpoint.NativeCommitted,
                    attemptStarted: true,
                    placementRecorded: true,
                    preSaveFingerprint: "save-a",
                    postSaveFingerprint: "save-b",
                    currentSaveFingerprint: "save-b",
                    beforeBackpackCount: 4,
                    expectedBackpackCount: 4,
                    currentBackpackCount: 4,
                    beforeMailCount: 0,
                    expectedMailCount: 1,
                    currentMailCount: 1),
                "The native-mail success path must finalize from a NativeCommitted journal.");

            AssertDecision(
                EquipmentSlotJournalRecoveryDecision.FailClosed,
                new EquipmentSlotJournalRecoverySnapshot(
                    EquipmentSlotJournalCheckpoint.Prepared,
                    attemptStarted: true,
                    placementRecorded: true,
                    preSaveFingerprint: "save-a",
                    postSaveFingerprint: string.Empty,
                    currentSaveFingerprint: "save-b",
                    beforeBackpackCount: 4,
                    expectedBackpackCount: 5,
                    currentBackpackCount: 4,
                    beforeMailCount: 0,
                    expectedMailCount: 0,
                    currentMailCount: 0),
                "A changed native save without destination evidence must retain journal escrow fail-closed.");

            AssertDecision(
                EquipmentSlotJournalRecoveryDecision.FailClosed,
                new EquipmentSlotJournalRecoverySnapshot(
                    EquipmentSlotJournalCheckpoint.Prepared,
                    attemptStarted: false,
                    placementRecorded: false,
                    preSaveFingerprint: "save-a",
                    postSaveFingerprint: string.Empty,
                    currentSaveFingerprint: "save-a",
                    beforeBackpackCount: 4,
                    expectedBackpackCount: -1,
                    currentBackpackCount: 5,
                    beforeMailCount: 0,
                    expectedMailCount: -1,
                    currentMailCount: 0),
                "Destination evidence without a started attempt must retain journal escrow fail-closed.");

            AssertDecision(
                EquipmentSlotJournalRecoveryDecision.FailClosed,
                new EquipmentSlotJournalRecoverySnapshot(
                    EquipmentSlotJournalCheckpoint.Prepared,
                    attemptStarted: true,
                    placementRecorded: false,
                    preSaveFingerprint: "save-a",
                    postSaveFingerprint: string.Empty,
                    currentSaveFingerprint: "save-b",
                    beforeBackpackCount: 4,
                    expectedBackpackCount: -1,
                    currentBackpackCount: 5,
                    beforeMailCount: 0,
                    expectedMailCount: -1,
                    currentMailCount: 0),
                "A delayed count-only observation must never promote an invocation that lacked one immediate exact destination.");

            AssertDecision(
                EquipmentSlotJournalRecoveryDecision.FailClosed,
                new EquipmentSlotJournalRecoverySnapshot(
                    EquipmentSlotJournalCheckpoint.NativeCommitted,
                    attemptStarted: true,
                    placementRecorded: true,
                    preSaveFingerprint: "save-a",
                    postSaveFingerprint: "save-b",
                    currentSaveFingerprint: "save-c",
                    beforeBackpackCount: 4,
                    expectedBackpackCount: 5,
                    currentBackpackCount: 5,
                    beforeMailCount: 0,
                    expectedMailCount: 0,
                    currentMailCount: 0),
                "A committed journal requires its exact post-save fingerprint as well as exact destination counts.");
        }

        private static void PhysicalHarmonyFixtureExecutesBothOrdersRollbackAndCleanup()
        {
            RunPhysicalFixture(
                string.Empty,
                "EquipmentSlotsHarmonyOwnerFixture: OK",
                "EquipmentSlots Harmony owner");
        }

        private static void
            PhysicalColdHostFixtureExecutesReplacementCrashStates()
        {
            RunPhysicalFixture(
                "--cold-host-only",
                "EquipmentSlotsColdHostFixture: OK",
                "EquipmentSlots real Compatibility Host cold recovery");
        }

        private static void RunPhysicalFixture(
            string arguments,
            string successMarker,
            string purpose)
        {
            string executable =
                Path.Combine(
                    FindRepositoryRoot(),
                    "tests",
                    "DTMAPI.UnitTests",
                    "Fixtures",
                    "EquipmentSlotsHarmonyOwnerFixture",
                    "bin",
                    "Release",
                    "net48",
                    "EquipmentSlotsHarmonyOwnerFixture.exe");
            if (!File.Exists(executable))
            {
                throw new FileNotFoundException(
                    "The focused Unit build did not produce the physical " +
                    purpose +
                    " fixture.",
                    executable);
            }

            var start =
                new ProcessStartInfo
                {
                    FileName = executable,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
            using Process process =
                Process.Start(start)
                ?? throw new InvalidOperationException(
                    "Could not start the physical " +
                    purpose +
                    " fixture.");
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            if (!process.WaitForExit(30000))
            {
                try
                {
                    process.Kill();
                }
                catch
                {
                }
                throw new TimeoutException(
                    "The physical " +
                    purpose +
                    " fixture did not exit within 30 seconds.");
            }

            Assert(
                process.ExitCode == 0 &&
                output.Contains(
                    successMarker,
                    StringComparison.Ordinal),
                "The physical " +
                purpose +
                " fixture failed. exit=" +
                process.ExitCode +
                ", output=" +
                output +
                ", error=" +
                error);
        }

        private static void AssertDecision(
            EquipmentSlotJournalRecoveryDecision expected,
            EquipmentSlotJournalRecoverySnapshot snapshot,
            string message)
        {
            EquipmentSlotJournalRecoveryDecision actual =
                EquipmentSlotJournalRecoveryPolicy.Decide(snapshot);
            Assert(
                actual == expected,
                message + " expected=" + expected + ", actual=" + actual + ".");
        }

        private static StoreFallbackFixture
            CreateStoreFallbackFixture(string caseName)
        {
            string path = NewStorePath(caseName);
            EquipmentSlotSaveScope scope = StoreScope();
            var store = new EquipmentSlotDocumentStore();
            EquipmentSlotStorageDocument previous =
                CreateStoreDocument(
                    "previous-item",
                    generation: 1,
                    scope);
            store.WriteAtomic(path, previous);
            EquipmentSlotStorageDocument live =
                CreateStoreDocument(
                    "current-item",
                    generation: 2,
                    scope);
            store.WriteAtomic(path, live);
            Assert(
                File.Exists(path + ".previous"),
                "The raw-v3 fallback fixture did not create an exact previous generation.");
            return new StoreFallbackFixture(
                path,
                scope,
                store);
        }

        private static void AssertNoFallbackAndNoMutation(
            StoreFallbackFixture fixture,
            string expectedFailure)
        {
            byte[] liveBefore =
                File.ReadAllBytes(fixture.Path);
            byte[] previousBefore =
                File.ReadAllBytes(
                    fixture.Path + ".previous");
            bool loaded =
                EquipmentSlotDocumentStore.TryLoadValidated(
                    fixture.Path,
                    fixture.Scope,
                    out EquipmentSlotStorageDocument? document,
                    out string sourcePath,
                    out string failure);
            Assert(
                !loaded &&
                document == null &&
                string.IsNullOrEmpty(sourcePath) &&
                string.Equals(
                    failure,
                    expectedFailure,
                    StringComparison.Ordinal) &&
                EqualBytes(
                    liveBefore,
                    File.ReadAllBytes(fixture.Path)) &&
                EqualBytes(
                    previousBefore,
                    File.ReadAllBytes(
                        fixture.Path + ".previous")),
                "A " +
                expectedFailure +
                " live sidecar must not fall back to or rewrite an older generation. actual=" +
                failure);
        }

        private static void
            AssertEligiblePreviousAndNextGeneration(
                StoreFallbackFixture fixture,
                EquipmentSlotStorageDocument invalidLive,
                string caseName)
        {
            WriteRawDocument(
                fixture.Path,
                invalidLive);
            byte[] liveBefore =
                File.ReadAllBytes(fixture.Path);
            byte[] previousBefore =
                File.ReadAllBytes(
                    fixture.Path + ".previous");
            Assert(
                EquipmentSlotDocumentStore.TryLoadValidated(
                    fixture.Path,
                    fixture.Scope,
                    out EquipmentSlotStorageDocument? loaded,
                    out string sourcePath,
                    out string loadMessage) &&
                string.Equals(
                    sourcePath,
                    fixture.Path + ".previous",
                    StringComparison.OrdinalIgnoreCase) &&
                loaded!.Generation == 1 &&
                loaded.Slots[0].ItemId ==
                    "previous-item" &&
                EqualBytes(
                    liveBefore,
                    File.ReadAllBytes(fixture.Path)) &&
                EqualBytes(
                    previousBefore,
                    File.ReadAllBytes(
                        fixture.Path + ".previous")),
                caseName +
                " must select the exact previous generation without mutating either raw file. message=" +
                loadMessage);

            loaded!.Generation++;
            loaded.Slots[0].DisplayName =
                caseName + " recovered";
            fixture.Store.WriteAtomic(
                fixture.Path,
                loaded);
            Assert(
                EquipmentSlotDocumentStore.TryReadValidated(
                    fixture.Path,
                    fixture.Scope,
                    out EquipmentSlotStorageDocument? rewritten,
                    out string rewriteFailure) &&
                rewritten!.Generation == 2 &&
                rewritten.Slots[0].ItemId ==
                    "previous-item" &&
                EqualBytes(
                    previousBefore,
                    File.ReadAllBytes(
                        fixture.Path + ".previous")) &&
                EquipmentSlotDocumentStore.TryReadValidated(
                    fixture.Path + ".previous",
                    fixture.Scope,
                    out EquipmentSlotStorageDocument?
                        preservedPrevious,
                    out _) &&
                preservedPrevious!.Generation == 1,
                caseName +
                " must permit one exact generation+1 atomic write while preserving the last validated previous authority. failure=" +
                rewriteFailure);
        }

        private static EquipmentSlotStorageDocument
            CreateStoreDocument(
                string itemId,
                long generation,
                EquipmentSlotSaveScope scope)
        {
            var document =
                new EquipmentSlotStorageDocument
                {
                    Scope = scope.Clone(),
                    Generation = generation
                };
            document.Slots[0].ItemId = itemId;
            document.Slots[0].DisplayName = itemId;
            return document;
        }

        private static EquipmentSlotSaveScope StoreScope() =>
            new EquipmentSlotSaveScope
            {
                ArchiveIndex = 2,
                PlayerName = "third-save",
                CustomPlayerName = "third-save",
                TotalGameSeconds = 100
            };

        private static NativeRecoveryObservation
            StoreObservation(
                string fingerprint,
                int backpack,
                int mail) =>
            new NativeRecoveryObservation(
                fingerprint,
                backpack,
                mail);

        private static NativePlacementResult StorePlacement(
            NativePlacementKind kind) =>
            new NativePlacementResult(
                kind,
                kind == NativePlacementKind.Backpack
                    ? 1
                    : 0,
                kind == NativePlacementKind.Mail ? 1 : 0,
                kind.ToString());

        private static void SetShieldTraits(
            EquipmentSlotStorageEntry entry,
            string skillId,
            int defense,
            int value,
            int maximum,
            int shieldDefend)
        {
            entry.SkillId = skillId;
            entry.DefenseBonus = defense;
            entry.IsShield = true;
            entry.ShieldValue = value;
            entry.ShieldMaxValue = maximum;
            entry.ShieldDefend = shieldDefend;
        }

        private static string NewStorePath(string caseName)
        {
            string sessionRoot =
                Environment.GetEnvironmentVariable(
                    "DTMAPI_TEST_SESSION_ROOT")
                ?? throw new InvalidOperationException(
                    "Raw-v3 store fixtures require the managed DTMAPI test session.");
            string root = Path.Combine(
                sessionRoot,
                "equipment-raw-v3-" +
                caseName +
                "-" +
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return Path.Combine(
                root,
                "equipment-slots-DTMAPI.MoreEquipmentSlotsMod.json");
        }

        private static void WriteRawDocument(
            string path,
            EquipmentSlotStorageDocument document) =>
            WriteRawJson(
                path,
                SerializeRaw(document));

        private static string SerializeRaw(
            EquipmentSlotStorageDocument document)
        {
            using var stream = new MemoryStream();
            new DataContractJsonSerializer(
                typeof(EquipmentSlotStorageDocument))
                .WriteObject(stream, document);
            return Encoding.UTF8.GetString(
                stream.ToArray());
        }

        private static void WriteRawJson(
            string path,
            string json)
        {
            string? directory =
                Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);
            File.WriteAllText(
                path,
                json,
                new UTF8Encoding(
                    encoderShouldEmitUTF8Identifier: false));
        }

        private static bool EqualBytes(
            byte[] first,
            byte[] second)
        {
            if (first.Length != second.Length)
                return false;
            for (int index = 0;
                 index < first.Length;
                 index++)
            {
                if (first[index] != second[index])
                    return false;
            }
            return true;
        }

        private static string FindRepositoryRoot()
        {
            foreach (string start in new[]
            {
                Directory.GetCurrentDirectory(),
                AppContext.BaseDirectory
            })
            {
                DirectoryInfo? current =
                    new DirectoryInfo(
                        Path.GetFullPath(start));
                while (current != null)
                {
                    if (File.Exists(
                        Path.Combine(
                            current.FullName,
                            "PROJECT.md")))
                    {
                        return current.FullName;
                    }
                    current = current.Parent;
                }
            }

            throw new DirectoryNotFoundException(
                "Could not locate the DTMAPI repository root.");
        }

        private static void Assert(
            bool condition,
            string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(
                    "EquipmentSlots compatibility test failed: " +
                    message);
            }
        }

        private sealed class ResidentEquipmentSlotsBackend
        {
            private readonly string previousPath;

            internal ResidentEquipmentSlotsBackend(
                string previousPath)
            {
                this.previousPath = previousPath;
            }

            internal int SaveLoadedCount { get; private set; }

            internal int RecoveryCount { get; private set; }

            internal int SaveSavingCount { get; private set; }

            internal int SaveSavedCount { get; private set; }

            internal int ReturnedToTitleCount { get; private set; }

            internal bool PreviousObserved { get; private set; }

            internal bool SessionActive { get; private set; }

            internal bool Terminal { get; private set; }

            public void NotifyEquipmentSlotsSaveLoaded(
                bool isNewGame)
            {
                SaveLoadedCount++;
                SessionActive = false;
                Terminal = false;
            }

            public bool RecoverOrphanEquipmentSlotsIfNeeded()
            {
                RecoveryCount++;
                PreviousObserved =
                    File.Exists(previousPath);
                if (!PreviousObserved ||
                    RecoveryCount != 1)
                {
                    return false;
                }
                SessionActive = true;
                return true;
            }

            public void NotifyEquipmentSlotsSaveSaving(
                int? slot)
            {
                if (!SessionActive ||
                    slot != 2)
                {
                    throw new InvalidOperationException(
                        "SaveSaving lost the recovered EquipmentSlots session.");
                }
                SaveSavingCount++;
            }

            public void NotifyEquipmentSlotsSaveSaved(
                int? slot)
            {
                if (!SessionActive ||
                    slot != 2)
                {
                    throw new InvalidOperationException(
                        "SaveSaved lost the recovered EquipmentSlots session.");
                }
                SaveSavedCount++;
                SessionActive = false;
                Terminal = true;
            }

            public void NotifyEquipmentSlotsReturnedToTitle()
            {
                ReturnedToTitleCount++;
                SessionActive = false;
            }
        }

        private sealed class FakeHost : IRuntimeHost
        {
            internal FakeHost(string gamePath)
            {
                GamePath = gamePath;
                PluginPath =
                    Path.Combine(
                        gamePath,
                        "BepInEx",
                        "plugins");
            }

            public string GamePath { get; }

            public string PluginPath { get; }

            public string HostName =>
                "EquipmentSlotsCompatibilityUnitTest";

            public List<string> Logs { get; } =
                new List<string>();

            public void Log(string message) =>
                Logs.Add(message ?? string.Empty);

            public void LogWarning(string message) =>
                Logs.Add(message ?? string.Empty);

            public void LogError(
                string message,
                Exception? exception = null) =>
                Logs.Add(
                    (message ?? string.Empty) +
                    (exception == null
                        ? string.Empty
                        : ": " + exception.Message));
        }

        private sealed class StoreFallbackFixture
        {
            internal StoreFallbackFixture(
                string path,
                EquipmentSlotSaveScope scope,
                EquipmentSlotDocumentStore store)
            {
                Path = path;
                Scope = scope;
                Store = store;
            }

            internal string Path { get; }

            internal EquipmentSlotSaveScope Scope { get; }

            internal EquipmentSlotDocumentStore Store { get; }
        }
    }
}
