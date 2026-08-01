#pragma warning disable CS0618 // This fixture intentionally exercises the frozen IEquipmentSlotsApi surface.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using DTMAPI.Abstractions;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;
using DTMAPI.MoreEquipmentSlots;
using DTMAPI.ModConfigMenu;
using HarmonyLib;

namespace DTMAPI.EquipmentSlotsHarmonyOwnerFixture
{
    internal static class Program
    {
        private const string ProductOwner =
            "dtmapi.mod.dtmapi.moreequipmentslotsmod";
        private const string CompatibilityOwner =
            "dtmapi.gamebridge.doloctown.equipmentslots.compatibility";
        private const string UnrelatedOwner =
            "dtmapi.tests.equipmentslots.unrelated";
        private static readonly MethodInfo[] Targets =
        {
            typeof(DolocTown.GameData.AgentEquipmentManager).GetMethod(
                "ReloadParams",
                BindingFlags.Public | BindingFlags.Instance)
                ?? throw new MissingMethodException("AgentEquipmentManager.ReloadParams"),
            typeof(DolocTown.BodyController).GetMethod(
                "OnAttacked",
                BindingFlags.Public | BindingFlags.Instance)
                ?? throw new MissingMethodException("BodyController.OnAttacked"),
            typeof(DolocTown.UI.AccessoriesBar).GetMethod(
                "__Init",
                BindingFlags.Public | BindingFlags.Instance)
                ?? throw new MissingMethodException("AccessoriesBar.__Init"),
            typeof(DolocTown.UI.AccessoriesBar).GetMethod(
                "OnStartShow",
                BindingFlags.Public | BindingFlags.Instance)
                ?? throw new MissingMethodException("AccessoriesBar.OnStartShow")
        };
        private static readonly HarmonyMethod OwnerPostfix =
            new HarmonyMethod(
                typeof(Program).GetMethod(
                    nameof(ExactTargetPostfix),
                    BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new MissingMethodException(
                    typeof(Program).FullName,
                    nameof(ExactTargetPostfix)));
        private static int fixtureSequence;

        private static int Main(string[] args)
        {
            try
            {
                if (args.Length == 1 &&
                    string.Equals(
                        args[0],
                        "--cold-host-only",
                        StringComparison.Ordinal))
                {
                    ProductColdRecoveryExecutesRealHostRoutes();
                    Console.WriteLine(
                        "EquipmentSlotsColdHostFixture: OK");
                    return 0;
                }
                ProductFirstFailsClosed();
                CompatibilityFirstFailsClosed();
                ProductPartialInstallRollsBackAtomically();
                ResidualCompatibilityOwnerIsRemovedAndRefused();
                ResidualProductOwnerFailsClosed();
                ProductDeactivationRemovesExactOwner();
                ProductShieldTailPreservesNativeSemantics();
                SameItemRuntimeWithdrawalEvidenceIsExact();
                NativeMailEvidenceFiltersUnrelatedMail();
                ProductMailOutcomeUnknownReconcilesWithoutReplay();
                ProductOutcomeUnknownNoSaveTitleRetainsSidecar();
                ProductDurableMailOutcomeUnknownReconcilesWithoutReplay();
                CompatibilityMailOutcomeUnknownReconcilesWithoutReplay();
                CompatibilityDurableMailOutcomeUnknownReconcilesWithoutReplay();
                ProductTraitRefreshCommitsOnlyAfterNativeSave();
                ProductRuntimeShutdownDiscardsUnsavedWorkingState();
                ProductDirtyDeactivationDefersUntilCommitOrDiscard();
                ProductDirtyConfigurationDisableDefersAndRetries();
                ProductConfigWriteFailureLeavesCommittedStateUntouched();
                ProductCleanDisableRestartRetainsOnlyTypedRecovery();
                AmbiguousJournalCannotPromoteOnNextSave();
                ProductFailClosedSaveSavingCancelsCoreBoundary();
                ProductDefenseOnlyCleanupReloadsWithoutFunctionLease();
                ProductCleanupReloadsAndStillUnpatchesOnFailure();
                ConfigurationDisableRestartAndLifecycleCleanup();
                CompatibilityGameplayCommitAndOwnerRecoverySemantics();
                CompatibilityRuntimeShutdownDiscardsUnsavedWorkingState();
                CompatibilityPresentJournalFailuresRemainBlocked();
                CompatibilityDirtyOwnerDeactivationDefersThenCleanRecovery();
                CompatibilityInvalidScopedStorageFailsClosed();
                CompatibilityTemporaryWriteValidationFailsClosed();
                Console.WriteLine(
                    "EquipmentSlotsHarmonyOwnerFixture: OK");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                Cleanup(ProductOwner);
                Cleanup(CompatibilityOwner);
                Cleanup(UnrelatedOwner);
                return 1;
            }
        }

        private static void
            ProductColdRecoveryExecutesRealHostRoutes()
        {
            AssertProductAssemblyIsNotLoaded();
            RunProductColdRecoveryScenario(
                ProductColdRecoveryState
                    .ReplacementPreparedRetry);
            RunProductColdRecoveryScenario(
                ProductColdRecoveryState
                    .ReplacementNativeCommittedPrepared);
            RunProductColdRecoveryScenario(
                ProductColdRecoveryState
                    .ReplacementCommittedTombstone);
            RunProductGameplayCandidateColdRecoveryScenario(
                changePreviousArchive: false);
            RunProductGameplayCandidateColdRecoveryScenario(
                changePreviousArchive: true);
            RunLegacyFlatColdRecoveryRoutingScenario(2);
            RunLegacyFlatColdRecoveryRoutingScenario(3);
            RunAmbiguousColdRecoveryRoutingScenario();
            RunProductPreviousColdRecoveryRoutingScenarios();
            RunNonCanonicalProductColdPathScenario();
            RunScopedAuthorityBlocksGlobalFallbackScenarios();
            AssertProductAssemblyIsNotLoaded();
            RunActiveProductStorageIsNotOrphanRecovery();
        }

        private static void
            RunLegacyFlatColdRecoveryRoutingScenario(int schema)
        {
            const int archiveIndex = 2;
            const string playerName = "fixture-player";
            string itemId =
                "legacy-cold-item-" + schema;
            string sessionRoot =
                Environment.GetEnvironmentVariable(
                    "DTMAPI_TEST_SESSION_ROOT")
                ?? throw new InvalidOperationException(
                    "Cold Host fixtures require the managed DTMAPI test session.");
            string scenarioRoot =
                @"\\?\" +
                Path.Combine(
                    sessionRoot,
                    "ech-legacy-cold-" +
                    schema +
                    "-" +
                    (++fixtureSequence).ToString());
            Directory.CreateDirectory(
                Path.Combine(
                    scenarioRoot,
                    "BepInEx",
                    "plugins"));
            var runtime =
                new DtmApiRuntime(
                    new FakeHost(scenarioRoot),
                    new ConfigMenuRegistry());
            object service = CreateService(runtime);
            string nativeSavePath =
                Path.Combine(
                    scenarioRoot,
                    "native-save-2.sav");
            File.WriteAllText(
                nativeSavePath,
                "legacy-cold-preimage-" + schema);
            DolocAPI.ResetInventory();
            DolocAPI.archiveHandle =
                new ColdFixtureArchive(
                    archiveIndex,
                    playerName,
                    playerName,
                    new DolocTown.GameData
                        .AgentEquipmentManager());
            DolocAPI.dataPersistenceManager =
                new ColdFixtureDataPersistenceManager(
                    nativeSavePath);

            string sidecarPath =
                Path.Combine(
                    runtime.Paths.ConfigPath,
                    "protected-items",
                    "equipment-slots",
                    "slot-2",
                    "equipment-slots-DTMAPI.MoreEquipmentSlotsMod.json");
            Directory.CreateDirectory(
                Path.GetDirectoryName(sidecarPath)
                ?? throw new InvalidOperationException(
                    "Legacy cold sidecar directory was unavailable."));
            File.WriteAllText(
                sidecarPath,
                BuildLegacyFlatColdJson(schema, itemId));
            byte[] before = File.ReadAllBytes(sidecarPath);
            object hostScope =
                CreateHostScope(
                    service,
                    archiveIndex,
                    playerName);
            object?[] productArguments =
            {
                sidecarPath,
                hostScope,
                false,
                0,
                string.Empty
            };
            object? productResult =
                RequireMethod(
                    service.GetType(),
                    "TryRecoverMoreEquipmentSlotsProductStorage")
                    .Invoke(service, productArguments);
            Assert(
                productResult is bool productRecovered &&
                !productRecovered &&
                productArguments[2] is bool productHandled &&
                !productHandled &&
                EqualBytes(
                    before,
                    File.ReadAllBytes(sidecarPath)),
                "Flat schema " +
                schema +
                " must be left unhandled and byte-identical by the Product-v3 cold parser.");

            object? orphanResult =
                RequireMethod(
                    service.GetType(),
                    "RecoverOrphanEquipmentSlotsIfNeeded")
                    .Invoke(service, Array.Empty<object>());
            Assert(
                orphanResult is bool scanCompleted &&
                scanCompleted &&
                DolocAPI.CountItem(itemId, false) == 1 &&
                CountProductColdRecoverySessions(service) == 0,
                "Flat schema " +
                schema +
                " did not fall through to the frozen legacy parser and return exactly one item to native storage.");

            InvokeVoid(
                service,
                "NotifyEquipmentSlotsSaveSaving",
                archiveIndex);
            File.WriteAllText(
                nativeSavePath,
                "legacy-cold-committed-" + schema);
            InvokeVoid(
                service,
                "NotifyEquipmentSlotsSaveSaved",
                archiveIndex);
            string finalJson =
                File.ReadAllText(sidecarPath);
            Assert(
                DolocAPI.CountItem(itemId, false) == 1 &&
                EquipmentSlotStorageFormatClassifier.Probe(
                    sidecarPath)
                    .Format ==
                    EquipmentSlotStorageFormat.LegacyFlat &&
                finalJson.IndexOf(
                    "\"itemId\":\"" + itemId + "\"",
                    StringComparison.Ordinal) < 0 &&
                finalJson.IndexOf(
                    "\"journal\"",
                    StringComparison.Ordinal) < 0,
                "Flat schema " +
                schema +
                " cold recovery must commit one native item and clear the legacy journal without converting through Product v3.");
            DolocAPI.ResetInventory();
        }

        private static void
            RunAmbiguousColdRecoveryRoutingScenario()
        {
            const int archiveIndex = 2;
            const string playerName = "fixture-player";
            string sessionRoot =
                Environment.GetEnvironmentVariable(
                    "DTMAPI_TEST_SESSION_ROOT")
                ?? throw new InvalidOperationException(
                    "Cold Host fixtures require the managed DTMAPI test session.");
            string scenarioRoot =
                @"\\?\" +
                Path.Combine(
                    sessionRoot,
                    "ech-ambiguous-cold-" +
                    (++fixtureSequence).ToString());
            Directory.CreateDirectory(
                Path.Combine(
                    scenarioRoot,
                    "BepInEx",
                    "plugins"));
            var runtime =
                new DtmApiRuntime(
                    new FakeHost(scenarioRoot),
                    new ConfigMenuRegistry());
            object service = CreateService(runtime);
            string sidecarPath =
                Path.Combine(
                    runtime.Paths.ConfigPath,
                    "protected-items",
                    "equipment-slots",
                    "slot-2",
                    "equipment-slots-DTMAPI.MoreEquipmentSlotsMod.json");
            Directory.CreateDirectory(
                Path.GetDirectoryName(sidecarPath)
                ?? throw new InvalidOperationException(
                    "Ambiguous cold sidecar directory was unavailable."));
            string json =
                "{\"schemaVersion\":3," +
                "\"ownerId\":\"DTMAPI.MoreEquipmentSlotsMod\"," +
                "\"archiveIndex\":2," +
                "\"scope\":{\"archiveIndex\":2,\"playerName\":\"fixture-player\",\"customPlayerName\":\"fixture-player\"}," +
                "\"generation\":1,\"slots\":[]}";
            File.WriteAllText(sidecarPath, json);
            byte[] before = File.ReadAllBytes(sidecarPath);
            object?[] arguments =
            {
                sidecarPath,
                CreateHostScope(
                    service,
                    archiveIndex,
                    playerName),
                false,
                0,
                string.Empty
            };
            object? result =
                RequireMethod(
                    service.GetType(),
                    "TryRecoverMoreEquipmentSlotsProductStorage")
                    .Invoke(service, arguments);
            Assert(
                result is bool recovered &&
                !recovered &&
                arguments[2] is bool handled &&
                handled &&
                (arguments[4] as string ?? string.Empty)
                    .IndexOf(
                        "Ambiguous",
                        StringComparison.Ordinal) >= 0 &&
                EqualBytes(
                    before,
                    File.ReadAllBytes(sidecarPath)),
                "An ambiguous same-filename document must be owned by neither parser, remain byte-identical, and fail closed at the Product route.");
        }

        private static void
            RunProductPreviousColdRecoveryRoutingScenarios()
        {
            foreach (
                (string name, string? live, bool shouldRecover)
                scenario in
                new[]
                {
                    ("missing-live", (string?)null, true),
                    ("corrupt-live", "{ invalid-json ]", true),
                    (
                        "future-live",
                        "{\"schemaVersion\":4,\"scope\":{\"archiveIndex\":2,\"playerName\":\"fixture-player\",\"customPlayerName\":\"fixture-player\"}}",
                        false),
                    (
                        "ambiguous-live",
                        "{\"schemaVersion\":3,\"ownerId\":\"DTMAPI.MoreEquipmentSlotsMod\",\"scope\":{\"archiveIndex\":2,\"playerName\":\"fixture-player\",\"customPlayerName\":\"fixture-player\"}}",
                        false)
                })
            {
                const int archiveIndex = 2;
                const string playerName = "fixture-player";
                string itemId =
                    "previous-cold-" + scenario.name;
                string sessionRoot =
                    Environment.GetEnvironmentVariable(
                        "DTMAPI_TEST_SESSION_ROOT")
                    ?? throw new InvalidOperationException(
                        "Cold Host fixtures require the managed DTMAPI test session.");
                string scenarioRoot =
                    @"\\?\" +
                    Path.Combine(
                        sessionRoot,
                        "ech-previous-" +
                        scenario.name +
                        "-" +
                        (++fixtureSequence).ToString());
                Directory.CreateDirectory(
                    Path.Combine(
                        scenarioRoot,
                        "BepInEx",
                        "plugins"));
                var runtime =
                    new DtmApiRuntime(
                        new FakeHost(scenarioRoot),
                        new ConfigMenuRegistry());
                object service = CreateService(runtime);
                DolocAPI.ResetInventory();
                DolocAPI.archiveHandle =
                    new ColdFixtureArchive(
                        archiveIndex,
                        playerName,
                        playerName,
                        new DolocTown.GameData
                            .AgentEquipmentManager());
                string savePath =
                    Path.Combine(
                        scenarioRoot,
                        "native-save-2.sav");
                File.WriteAllText(savePath, "previous-cold");
                DolocAPI.dataPersistenceManager =
                    new ColdFixtureDataPersistenceManager(
                        savePath);

                string sidecarPath =
                    Path.Combine(
                        runtime.Paths.ConfigPath,
                        "protected-items",
                        "equipment-slots",
                        "slot-2",
                        "equipment-slots-DTMAPI.MoreEquipmentSlotsMod.json");
                Directory.CreateDirectory(
                    Path.GetDirectoryName(sidecarPath)!);
                var document =
                    new EquipmentSlotStorageDocument
                    {
                        Scope =
                            new EquipmentSlotSaveScope
                            {
                                ArchiveIndex = archiveIndex,
                                PlayerName = playerName,
                                CustomPlayerName = playerName,
                                TotalGameSeconds = 1
                            },
                        Generation = 1
                    };
                document.Slots[0].ItemId = itemId;
                document.Slots[0].DisplayName = itemId;
                new EquipmentSlotDocumentStore().WriteAtomic(
                    sidecarPath + ".previous",
                    document);
                if (scenario.live != null)
                    File.WriteAllText(sidecarPath, scenario.live);
                byte[] previousBefore =
                    File.ReadAllBytes(sidecarPath + ".previous");
                byte[]? liveBefore =
                    File.Exists(sidecarPath)
                        ? File.ReadAllBytes(sidecarPath)
                        : null;

                object? recovered =
                    RequireMethod(
                        service.GetType(),
                        "RecoverOrphanEquipmentSlotsIfNeeded")
                        .Invoke(service, Array.Empty<object>());
                Assert(
                    recovered is bool completed &&
                    completed &&
                    DolocAPI.CountItem(itemId, false) ==
                        (scenario.shouldRecover ? 1 : 0),
                    scenario.name +
                    " did not obey the reachable previous-generation cold recovery rule.");
                if (!scenario.shouldRecover)
                {
                    Assert(
                        EqualBytes(
                            previousBefore,
                            File.ReadAllBytes(
                                sidecarPath + ".previous")) &&
                        liveBefore != null &&
                        EqualBytes(
                            liveBefore,
                            File.ReadAllBytes(sidecarPath)),
                        scenario.name +
                        " must keep both live and previous authorities byte-identical when fallback is forbidden.");
                }
                DolocAPI.ResetInventory();
            }
        }

        private static void
            RunNonCanonicalProductColdPathScenario()
        {
            const int archiveIndex = 2;
            const string playerName = "fixture-player";
            string scenarioRoot =
                CreateFixtureScenarioRoot(
                    "ech-noncanonical-product-cold");
            var runtime =
                new DtmApiRuntime(
                    new FakeHost(scenarioRoot),
                    new ConfigMenuRegistry());
            object service = CreateService(runtime);
            object hostScope =
                CreateHostScope(
                    service,
                    archiveIndex,
                    playerName);
            string wrongPath =
                Path.Combine(
                    runtime.Paths.ConfigPath,
                    "protected-items",
                    "equipment-slots",
                    "slot-3",
                    "equipment-slots-DTMAPI.MoreEquipmentSlotsMod.json");
            var document =
                new EquipmentSlotStorageDocument
                {
                    Scope =
                        new EquipmentSlotSaveScope
                        {
                            ArchiveIndex = archiveIndex,
                            PlayerName = playerName,
                            CustomPlayerName = playerName,
                            TotalGameSeconds = 1
                        },
                    Generation = 1
                };
            document.Slots[0].ItemId =
                "wrong-canonical-path-item";
            document.Slots[0].DisplayName =
                "wrong-canonical-path-item";
            new EquipmentSlotDocumentStore()
                .WriteAtomic(
                    wrongPath,
                    document);
            byte[] before =
                File.ReadAllBytes(wrongPath);
            object?[] arguments =
            {
                wrongPath,
                hostScope,
                false,
                0,
                string.Empty
            };
            object? recovered =
                RequireMethod(
                    service.GetType(),
                    "TryRecoverMoreEquipmentSlotsProductStorage")
                    .Invoke(service, arguments);
            Assert(
                recovered is bool result &&
                !result &&
                arguments[2] is bool handled &&
                !handled &&
                EqualBytes(
                    before,
                    File.ReadAllBytes(wrongPath)),
                "A Product-shaped file with the right filename outside the current canonical scoped path must not be claimed by Product cold recovery.");
        }

        private static void
            RunActiveProductStorageIsNotOrphanRecovery()
        {
            const int archiveIndex = 2;
            const string playerName = "fixture-player";
            string scenarioRoot =
                CreateFixtureScenarioRoot(
                    "ech-active-product-not-orphan");
            var host = new FakeHost(scenarioRoot);
            var runtime =
                new DtmApiRuntime(
                    host,
                    new ConfigMenuRegistry());
            object service = CreateService(runtime);
            DolocAPI.ResetInventory();
            DolocAPI.archiveHandle =
                new ColdFixtureArchive(
                    archiveIndex,
                    playerName,
                    playerName,
                    new DolocTown.GameData
                        .AgentEquipmentManager());

            string sidecarPath =
                Path.Combine(
                    runtime.Paths.ConfigPath,
                    "protected-items",
                    "equipment-slots",
                    "slot-2",
                    "equipment-slots-DTMAPI.MoreEquipmentSlotsMod.json");
            var document =
                new EquipmentSlotStorageDocument
                {
                    Scope =
                        new EquipmentSlotSaveScope
                        {
                            ArchiveIndex = archiveIndex,
                            PlayerName = playerName,
                            CustomPlayerName = playerName,
                            TotalGameSeconds = 1
                        },
                    Generation = 1
                };
            document.Slots[0].ItemId =
                "active-product-item";
            document.Slots[0].DisplayName =
                "active-product-item";
            new EquipmentSlotDocumentStore()
                .WriteAtomic(
                    sidecarPath,
                    document);
            byte[] before =
                File.ReadAllBytes(sidecarPath);

            AppDomain.CurrentDomain.DefineDynamicAssembly(
                new AssemblyName(
                    "DTMAPI.MoreEquipmentSlots"),
                AssemblyBuilderAccess.Run);
            object? completed =
                RequireMethod(
                    service.GetType(),
                    "RecoverOrphanEquipmentSlotsIfNeeded")
                    .Invoke(service, Array.Empty<object>());
            Assert(
                completed is bool result &&
                result &&
                DolocAPI.CountItem(
                    "active-product-item",
                    false) == 0 &&
                CountProductColdRecoverySessions(service) == 0 &&
                EqualBytes(
                    before,
                    File.ReadAllBytes(sidecarPath)) &&
                !host.Logs.Any(log =>
                    log.IndexOf(
                        "orphan recovery failed owner=DTMAPI.MoreEquipmentSlotsMod",
                        StringComparison.OrdinalIgnoreCase) >= 0),
                "A normally loaded Product must be skipped without cold recovery, false orphan failure status, or storage mutation.");
            DolocAPI.ResetInventory();
        }

        private static void
            RunScopedAuthorityBlocksGlobalFallbackScenarios()
        {
            foreach (
                (string name, string scopedJson) scenario in
                new[]
                {
                    (
                        "invalid",
                        "{ invalid-json ]"),
                    (
                        "future",
                        "{\"schemaVersion\":4,\"scope\":{\"archiveIndex\":2,\"playerName\":\"fixture-player\",\"customPlayerName\":\"fixture-player\"}}"),
                    (
                        "ambiguous",
                        "{\"schemaVersion\":3,\"ownerId\":\"DTMAPI.MoreEquipmentSlotsMod\",\"scope\":{\"archiveIndex\":2,\"playerName\":\"fixture-player\",\"customPlayerName\":\"fixture-player\"}}")
                })
            {
                const int archiveIndex = 2;
                const string playerName = "fixture-player";
                string itemId =
                    "blocked-global-" + scenario.name;
                string sessionRoot =
                    Environment.GetEnvironmentVariable(
                        "DTMAPI_TEST_SESSION_ROOT")
                    ?? throw new InvalidOperationException(
                        "Cold Host fixtures require the managed DTMAPI test session.");
                string scenarioRoot =
                    @"\\?\" +
                    Path.Combine(
                        sessionRoot,
                        "ech-scoped-block-" +
                        scenario.name +
                        "-" +
                        (++fixtureSequence).ToString());
                Directory.CreateDirectory(
                    Path.Combine(
                        scenarioRoot,
                        "BepInEx",
                        "plugins"));
                var runtime =
                    new DtmApiRuntime(
                        new FakeHost(scenarioRoot),
                        new ConfigMenuRegistry());
                object service = CreateService(runtime);
                DolocAPI.ResetInventory();
                DolocAPI.archiveHandle =
                    new ColdFixtureArchive(
                        archiveIndex,
                        playerName,
                        playerName,
                        new DolocTown.GameData
                            .AgentEquipmentManager());
                DolocAPI.dataPersistenceManager =
                    new ColdFixtureDataPersistenceManager(
                        Path.Combine(
                            scenarioRoot,
                            "native-save-2.sav"));

                string fileName =
                    "equipment-slots-DTMAPI.MoreEquipmentSlotsMod.json";
                string scopedPath =
                    Path.Combine(
                        runtime.Paths.ConfigPath,
                        "protected-items",
                        "equipment-slots",
                        "slot-2",
                        fileName);
                string globalPath =
                    Path.Combine(
                        runtime.Paths.ConfigPath,
                        fileName);
                Directory.CreateDirectory(
                    Path.GetDirectoryName(scopedPath)!);
                File.WriteAllText(
                    scopedPath,
                    scenario.scopedJson);
                File.WriteAllText(
                    globalPath,
                    BuildLegacyFlatColdJson(3, itemId));
                byte[] scopedBefore =
                    File.ReadAllBytes(scopedPath);
                byte[] globalBefore =
                    File.ReadAllBytes(globalPath);

                object? recovered =
                    RequireMethod(
                        service.GetType(),
                        "RecoverOrphanEquipmentSlotsIfNeeded")
                        .Invoke(service, Array.Empty<object>());
                Assert(
                    recovered is bool completed &&
                    completed &&
                    DolocAPI.CountItem(itemId, false) == 0 &&
                    EqualBytes(
                        scopedBefore,
                        File.ReadAllBytes(scopedPath)) &&
                    EqualBytes(
                        globalBefore,
                        File.ReadAllBytes(globalPath)),
                    "A " +
                    scenario.name +
                    " canonical scoped authority must block same-owner global recovery without mutating either file.");
                DolocAPI.ResetInventory();
            }
        }

        private static string BuildLegacyFlatColdJson(
            int schema,
            string itemId) =>
            "{" +
            "\"schemaVersion\":" + schema + "," +
            "\"ownerId\":\"DTMAPI.MoreEquipmentSlotsMod\"," +
            "\"storageScope\":\"slot-2\"," +
            "\"archiveIndex\":2," +
            "\"playerName\":\"fixture-player\"," +
            "\"customPlayerName\":\"fixture-player\"," +
            "\"savedTotalGameSeconds\":100," +
            "\"generation\":4," +
            "\"slots\":[{" +
            "\"index\":0," +
            "\"slotId\":\"dtmapi.extra.1\"," +
            "\"itemId\":\"" + itemId + "\"," +
            "\"displayName\":\"" + itemId + "\"," +
            "\"skillId\":\"\"," +
            "\"defenseBonus\":0," +
            "\"isShieldHat\":false," +
            "\"shieldMaxValue\":0," +
            "\"shieldValue\":0," +
            "\"shieldDefend\":0}]}";

        private static void
            ProductTraitRefreshCommitsOnlyAfterNativeSave()
        {
            RunProductTraitRefreshDiscard(
                useRuntimeShutdown: false);
            RunProductTraitRefreshDiscard(
                useRuntimeShutdown: true);
            RunProductTraitRefreshCommit();
        }

        private static void RunProductTraitRefreshDiscard(
            bool useRuntimeShutdown)
        {
            ProductTraitRefreshFixture fixture =
                CreateProductTraitRefreshFixture(
                    useRuntimeShutdown
                        ? "runtime-shutdown"
                        : "returned-title");
            try
            {
                Invoke(
                    fixture.Runtime,
                    "RehydrateWorkingTraits",
                    "fixture native trait change");
                AssertRefreshedWorkingOnly(fixture);
                if (useRuntimeShutdown)
                {
                    fixture.Runtime.DeactivateOwner(
                        "RuntimeShutdown");
                }
                else
                {
                    fixture.Runtime.ReturnedToTitle();
                }
                Assert(
                    EqualBytes(
                        fixture.CommittedBytes,
                        File.ReadAllBytes(
                            fixture.SidecarPath)) &&
                    fixture.Document.Generation ==
                        fixture.Generation,
                    "No-save " +
                    (useRuntimeShutdown
                        ? "RuntimeShutdown"
                        : "ReturnedToTitle") +
                    " persisted refreshed native traits or advanced committed generation.");
            }
            finally
            {
                DolocTown.ItemFactory.ShieldMaxValue = 10;
                DolocAPI.ResetInventory();
            }
        }

        private static void RunProductTraitRefreshCommit()
        {
            ProductTraitRefreshFixture fixture =
                CreateProductTraitRefreshFixture(
                    "native-save");
            try
            {
                Invoke(
                    fixture.Runtime,
                    "RehydrateWorkingTraits",
                    "fixture native trait change");
                AssertRefreshedWorkingOnly(fixture);
                fixture.Runtime.OnSaveSaving(2);
                File.WriteAllText(
                    fixture.NativeSavePath,
                    "trait-refresh-native-committed");
                fixture.Runtime.OnSaveSaved(2);
                EquipmentSlotStorageDocument reloaded =
                    new EquipmentSlotDocumentStore()
                        .Load(
                            fixture.SidecarPath,
                            fixture.Document.Scope);
                Assert(
                    reloaded.GameplayCandidate == null &&
                    reloaded.Journal == null &&
                    reloaded.Slots[0].DefenseBonus == 7 &&
                    reloaded.Slots[0].ShieldMaxValue == 6 &&
                    reloaded.Slots[0].ShieldValue == 6 &&
                    reloaded.Slots[0].ShieldDefend == 2 &&
                    reloaded.Generation >
                        fixture.Generation,
                    "SaveSaving/SaveSaved did not promote refreshed Working traits into the committed sidecar.");
            }
            finally
            {
                fixture.Runtime.DeactivateOwner(
                    "RuntimeShutdown");
                DolocTown.ItemFactory.ShieldMaxValue = 10;
                DolocAPI.ResetInventory();
            }
        }

        private static ProductTraitRefreshFixture
            CreateProductTraitRefreshFixture(
                string suffix)
        {
            DolocAPI.ResetInventory();
            MoreEquipmentSlotsNativeRuntime runtime =
                CreateProductRuntime();
            EquipmentSlotStorageDocument document =
                ConfigureSeparatedProductShield(
                    runtime,
                    shieldValue: 8,
                    shieldDefend: 1,
                    defenseBonus: 3);
            document.Slots[0].ShieldMaxValue = 10;
            var working =
                (List<EquipmentSlotStorageEntry>)(
                    GetField(
                        runtime,
                        "workingSlots").GetValue(runtime)
                    ?? throw new InvalidOperationException(
                        "Trait-refresh Working projection was unavailable."));
            working[0].ShieldMaxValue = 10;
            GetField(runtime, "archiveIndex").SetValue(
                runtime,
                2);

            string scenarioRoot =
                CreateFixtureScenarioRoot(
                    "product-trait-" + suffix);
            string nativeSavePath =
                Path.Combine(
                    scenarioRoot,
                    "native-save-2.sav");
            File.WriteAllText(
                nativeSavePath,
                "trait-refresh-native-preimage");
            var manager =
                new DolocTown.GameData.AgentEquipmentManager();
            DolocAPI.archiveHandle =
                new ColdFixtureArchive(
                    2,
                    "fixture-player",
                    "fixture-player",
                    manager);
            DolocAPI.dataPersistenceManager =
                new ColdFixtureDataPersistenceManager(
                    nativeSavePath);
            string sidecarPath =
                (string)(GetField(
                    runtime,
                    "sidecarPath").GetValue(runtime)
                    ?? throw new InvalidOperationException(
                        "Trait-refresh sidecar path was unavailable."));
            new EquipmentSlotDocumentStore()
                .WriteAtomic(
                    sidecarPath,
                    document);
            byte[] committed =
                File.ReadAllBytes(sidecarPath);
            long generation = document.Generation;
            DolocTown.ItemFactory.ShieldMaxValue = 6;
            return new ProductTraitRefreshFixture(
                runtime,
                document,
                working,
                sidecarPath,
                nativeSavePath,
                committed,
                generation);
        }

        private static void AssertRefreshedWorkingOnly(
            ProductTraitRefreshFixture fixture)
        {
            Assert(
                fixture.Working[0].DefenseBonus == 7 &&
                fixture.Working[0].ShieldMaxValue == 6 &&
                fixture.Working[0].ShieldValue == 6 &&
                fixture.Working[0].ShieldDefend == 2 &&
                fixture.Document.Slots[0].DefenseBonus == 3 &&
                fixture.Document.Slots[0].ShieldMaxValue == 10 &&
                fixture.Document.Slots[0].ShieldValue == 8 &&
                fixture.Document.Slots[0].ShieldDefend == 1 &&
                fixture.Document.Generation ==
                    fixture.Generation &&
                (bool)(GetField(
                    fixture.Runtime,
                    "workingDirty").GetValue(
                        fixture.Runtime) ?? false) &&
                EqualBytes(
                    fixture.CommittedBytes,
                    File.ReadAllBytes(
                        fixture.SidecarPath)),
                "SaveLoaded trait refresh did not remain an in-memory dirty Working projection with committed authority byte-identical.");
        }

        private static void
            NativeMailEvidenceFiltersUnrelatedMail()
        {
            var manager =
                new DolocTown.GameData.AgentEquipmentManager();
            var archive =
                new ColdFixtureArchive(
                    2,
                    "fixture-player",
                    "fixture-player",
                    manager);
            archive.farmData.emailManager.emails =
                new object[]
                {
                    new FixtureEmail
                    {
                        Id = "send_item_template",
                        emailAttaches = new object[]
                        {
                            new DolocTown.EmailAttachReward
                            {
                                isAccept = false,
                                reward =
                                    new DolocTown.RewardItem
                                    {
                                        itemName =
                                            "fixture-shield",
                                        itemCount = 1
                                    }
                            },
                            new DolocTown.QuestEmailAttachReward
                            {
                                isAccept = false,
                                reward =
                                    new DolocTown.RewardItem
                                    {
                                        itemName =
                                            "fixture-shield",
                                        itemCount = 5
                                    }
                            },
                            new DolocTown.EmailAttachReward
                            {
                                isAccept = true,
                                reward =
                                    new DolocTown.RewardItem
                                    {
                                        itemName =
                                            "fixture-shield",
                                        itemCount = 7
                                    }
                            }
                        }
                    },
                    new FixtureEmail
                    {
                        Id = "quest_reward",
                        emailAttaches = new object[]
                        {
                            new DolocTown.EmailAttachReward
                            {
                                isAccept = false,
                                reward =
                                    new DolocTown.RewardItem
                                    {
                                        itemName =
                                            "fixture-shield",
                                        itemCount = 11
                                    }
                            }
                        }
                    }
                };
            DolocAPI.archiveHandle = archive;
            Assert(
                EquipmentSlotNativeMailEvidence
                    .CountUnacceptedDtmapiItemMail(
                        typeof(DolocAPI),
                    "fixture-shield") == 1,
                "Native mail evidence counted non-DTMAPI templates, quest attachment types, or accepted attachments as product placement.");

            archive.farmData.emailManager.emails =
                new object();
            bool nonEnumerableRejected = false;
            try
            {
                EquipmentSlotNativeMailEvidence
                    .CountUnacceptedDtmapiItemMail(
                        typeof(DolocAPI),
                        "fixture-shield");
            }
            catch (InvalidDataException)
            {
                nonEnumerableRejected = true;
            }
            archive.farmData.emailManager.emails =
                new object[] { new object() };
            bool missingIdRejected = false;
            try
            {
                EquipmentSlotNativeMailEvidence
                    .CountUnacceptedDtmapiItemMail(
                        typeof(DolocAPI),
                        "fixture-shield");
            }
            catch (InvalidDataException)
            {
                missingIdRejected = true;
            }
            archive.farmData.emailManager.emails =
                new object[]
                {
                    new FixtureEmail
                    {
                        Id = "send_item_template",
                        emailAttaches = new object()
                    }
                };
            bool attachmentsRejected = false;
            try
            {
                EquipmentSlotNativeMailEvidence
                    .CountUnacceptedDtmapiItemMail(
                        typeof(DolocAPI),
                        "fixture-shield");
            }
            catch (InvalidDataException)
            {
                attachmentsRejected = true;
            }
            Assert(
                nonEnumerableRejected &&
                missingIdRejected &&
                attachmentsRejected,
                "Production native mail evidence must fail closed when the top-level list, email identity, or exact-template attachment authority is unreadable.");
            DolocAPI.ResetInventory();
        }

        private static void
            ProductDurableMailOutcomeUnknownReconcilesWithoutReplay()
        {
            ResetOwners();
            DolocAPI.ResetInventory();
            const int archiveIndex = 2;
            string scenarioRoot =
                CreateFixtureScenarioRoot(
                    "equipment-product-durable-mail-unknown");
            string nativeSavePath =
                Path.Combine(
                    scenarioRoot,
                    "native-save-2.sav");
            File.WriteAllText(
                nativeSavePath,
                "product-durable-mail-preimage");
            var manager =
                new DolocTown.GameData.AgentEquipmentManager();
            var archive =
                new ColdFixtureArchive(
                    archiveIndex,
                    "fixture-player",
                    "fixture-player",
                    manager);
            DolocAPI.archiveHandle = archive;
            DolocAPI.dataPersistenceManager =
                new ColdFixtureDataPersistenceManager(
                    nativeSavePath);
            MoreEquipmentSlotsNativeRuntime product =
                CreateProductRuntime();
            EquipmentSlotStorageDocument document =
                ConfigureSeparatedProductShield(
                    product,
                    shieldValue: 5);
            EquipmentSlotTransactionJournal journal =
                EquipmentSlotTransactionCoordinator
                    .PrepareRecovery(
                        document,
                        new[] { 0 },
                        EquipmentSlotTransactionOrigin
                            .OwnerRecovery);
            GetField(product, "workingSlots").SetValue(
                product,
                EquipmentSlotGameplayCandidateCoordinator
                    .CloneSlots(document.Slots));
            GetField(product, "archiveIndex").SetValue(
                product,
                archiveIndex);
            string sidecarPath =
                (string)(GetField(
                    product,
                    "sidecarPath").GetValue(product)
                    ?? throw new InvalidOperationException(
                        "Product durable mail sidecar path was unavailable."));
            new EquipmentSlotDocumentStore()
                .WriteAtomic(sidecarPath, document);

            DolocAPI.BackpackPlacementAvailable = false;
            object[] exactMail =
            {
                new FixtureEmail
                {
                    Id = "send_item_template",
                    emailAttaches = new object[]
                    {
                        new DolocTown.EmailAttachReward
                        {
                            isAccept = false,
                            reward =
                                new DolocTown.RewardItem
                                {
                                    itemName =
                                        "fixture-shield",
                                    itemCount = 1
                                }
                        }
                    }
                }
            };
            DolocAPI.SendItemAsEmailOverride =
                (_, _) =>
                {
                    archive.farmData.emailManager.emails =
                        new object();
                    return true;
                };
            bool firstSaveRejected = false;
            try
            {
                product.OnSaveSaving(archiveIndex);
            }
            catch (
                EquipmentSlotNativeMutationOutcomeUnknownException)
            {
                firstSaveRejected = true;
            }
            bool secondSaveRejected = false;
            try
            {
                product.OnSaveSaving(archiveIndex);
            }
            catch (
                EquipmentSlotNativeMutationOutcomeUnknownException)
            {
                secondSaveRejected = true;
            }
            Assert(
                firstSaveRejected &&
                secondSaveRejected &&
                DolocAPI.SendItemAsEmailCallCount == 1 &&
                journal.AttemptStarted &&
                !journal.Escrow[0].AttemptCompleted &&
                document.Journal != null,
                "Product durable journal did not retain one non-replayed outcome-unknown mail attempt while SaveGame was blocked.");

            archive.farmData.emailManager.emails =
                exactMail;
            product.OnSaveSaving(archiveIndex);
            Assert(
                DolocAPI.SendItemAsEmailCallCount == 1 &&
                journal.Escrow[0].AttemptCompleted &&
                journal.Escrow[0].Placement ==
                    NativePlacementKind.Mail,
                "Product durable journal did not reconcile exact +1 mail evidence without replay.");
            File.WriteAllText(
                nativeSavePath,
                "product-durable-mail-saved");
            product.OnSaveSaved(archiveIndex);
            Assert(
                document.Journal == null &&
                !document.Slots[0].IsOccupied &&
                DolocAPI.SendItemAsEmailCallCount == 1,
                "Product durable journal did not finalize to one mail and zero sidecar copies.");
            product.ReturnedToTitle();
            product.DeactivateOwner("RuntimeShutdown");
            DolocAPI.ResetInventory();
        }

        private static void
            ProductOutcomeUnknownNoSaveTitleRetainsSidecar()
        {
            ResetOwners();
            DolocAPI.ResetInventory();
            const int archiveIndex = 2;
            string scenarioRoot =
                CreateFixtureScenarioRoot(
                    "equipment-product-mail-no-save");
            string nativeSavePath =
                Path.Combine(
                    scenarioRoot,
                    "native-save-2.sav");
            File.WriteAllText(
                nativeSavePath,
                "product-mail-no-save-preimage");
            var manager =
                new DolocTown.GameData.AgentEquipmentManager();
            var archive =
                new ColdFixtureArchive(
                    archiveIndex,
                    "fixture-player",
                    "fixture-player",
                    manager);
            DolocAPI.archiveHandle = archive;
            DolocAPI.dataPersistenceManager =
                new ColdFixtureDataPersistenceManager(
                    nativeSavePath);
            MoreEquipmentSlotsNativeRuntime product =
                CreateProductRuntime();
            EquipmentSlotStorageDocument document =
                ConfigureSeparatedProductShield(
                    product,
                    shieldValue: 5);
            GetField(product, "archiveIndex").SetValue(
                product,
                archiveIndex);
            string sidecarPath =
                (string)(GetField(
                    product,
                    "sidecarPath").GetValue(product)
                    ?? throw new InvalidOperationException(
                        "Product no-save sidecar path was unavailable."));
            new EquipmentSlotDocumentStore()
                .WriteAtomic(sidecarPath, document);
            byte[] committedBytes =
                File.ReadAllBytes(sidecarPath);

            DolocAPI.BackpackPlacementAvailable = false;
            DolocAPI.SendItemAsEmailOverride =
                (_, _) =>
                {
                    archive.farmData.emailManager.emails =
                        new object();
                    return false;
                };
            Assert(
                !product.RequestUnequip(0) &&
                DolocAPI.SendItemAsEmailCallCount == 1 &&
                product.GetSlotItemId(0) ==
                    "fixture-shield",
                "Product no-save fixture did not retain its Working item after an unchanged but unreadable mail attempt.");
            product.ReturnedToTitle();
            archive.farmData.emailManager.emails =
                Array.Empty<object>();
            Assert(
                EqualBytes(
                    committedBytes,
                    File.ReadAllBytes(sidecarPath)) &&
                document.Slots[0].IsOccupied &&
                EquipmentSlotNativeMailEvidence
                    .CountUnacceptedDtmapiItemMail(
                        typeof(DolocAPI),
                        "fixture-shield") == 0 &&
                GetField(
                    product,
                    "pendingGameplayPlacement")
                    .GetValue(product) == null,
                "No-save title did not discard the in-process outcome-unknown guard while retaining exactly one committed sidecar item and zero mail.");
            product.DeactivateOwner("RuntimeShutdown");
            DolocAPI.ResetInventory();
        }

        private static void
            ProductMailOutcomeUnknownReconcilesWithoutReplay()
        {
            ResetOwners();
            DolocAPI.ResetInventory();
            const int archiveIndex = 2;
            string scenarioRoot =
                CreateFixtureScenarioRoot(
                    "equipment-product-mail-unknown");
            string nativeSavePath =
                Path.Combine(
                    scenarioRoot,
                    "native-save-2.sav");
            File.WriteAllText(
                nativeSavePath,
                "product-mail-unknown-preimage");
            var manager =
                new DolocTown.GameData.AgentEquipmentManager();
            var archive =
                new ColdFixtureArchive(
                    archiveIndex,
                    "fixture-player",
                    "fixture-player",
                    manager);
            DolocAPI.archiveHandle = archive;
            DolocAPI.dataPersistenceManager =
                new ColdFixtureDataPersistenceManager(
                    nativeSavePath);
            MoreEquipmentSlotsNativeRuntime product =
                CreateProductRuntime();
            EquipmentSlotStorageDocument document =
                ConfigureSeparatedProductShield(
                    product,
                    shieldValue: 5);
            GetField(product, "archiveIndex").SetValue(
                product,
                archiveIndex);
            string sidecarPath =
                (string)(GetField(
                    product,
                    "sidecarPath").GetValue(product)
                    ?? throw new InvalidOperationException(
                        "Product mail fixture sidecar path was unavailable."));
            new EquipmentSlotDocumentStore()
                .WriteAtomic(sidecarPath, document);

            DolocAPI.BackpackPlacementAvailable = false;
            archive.farmData.emailManager.emails =
                new object();
            bool preflightRejected =
                !product.RequestUnequip(0);
            Assert(
                preflightRejected &&
                DolocAPI.SendItemAsEmailCallCount == 0 &&
                product.GetSlotItemId(0) ==
                    "fixture-shield",
                "Unreadable Product mail preflight called native mail or released the Working sidecar item.");

            archive.farmData.emailManager.emails =
                Array.Empty<object>();
            object[] exactMail =
            {
                new FixtureEmail
                {
                    Id = "send_item_template",
                    emailAttaches = new object[]
                    {
                        new DolocTown.EmailAttachReward
                        {
                            isAccept = false,
                            reward =
                                new DolocTown.RewardItem
                                {
                                    itemName =
                                        "fixture-shield",
                                    itemCount = 1
                                }
                        }
                    }
                }
            };
            DolocAPI.SendItemAsEmailOverride =
                (itemId, count) =>
                {
                    archive.farmData.emailManager.emails =
                        new object();
                    return string.Equals(
                            itemId,
                            "fixture-shield",
                            StringComparison.Ordinal) &&
                        count == 1;
                };
            Assert(
                !product.RequestUnequip(0) &&
                DolocAPI.SendItemAsEmailCallCount == 1 &&
                product.GetSlotItemId(0) ==
                    "fixture-shield",
                "Product did not retain exactly one Working sidecar item after post-mail evidence became unreadable.");
            bool unreadableSaveRejected = false;
            try
            {
                product.OnSaveSaving(archiveIndex);
            }
            catch (
                EquipmentSlotNativeMutationOutcomeUnknownException)
            {
                unreadableSaveRejected = true;
            }
            Assert(
                unreadableSaveRejected &&
                DolocAPI.SendItemAsEmailCallCount == 1,
                "Product did not reject SaveGame, or retried native mail, while placement evidence remained unreadable.");

            archive.farmData.emailManager.emails =
                exactMail;
            product.OnSaveSaving(archiveIndex);
            Assert(
                DolocAPI.SendItemAsEmailCallCount == 1 &&
                product.GetSlotItemId(0) ==
                    string.Empty &&
                document.GameplayCandidate != null &&
                EquipmentSlotNativeMailEvidence
                    .CountUnacceptedDtmapiItemMail(
                        typeof(DolocAPI),
                        "fixture-shield") == 1,
                "Exact +1 mail evidence did not converge the Product Working item without a second native mail call.");
            File.WriteAllText(
                nativeSavePath,
                "product-mail-unknown-saved");
            product.OnSaveSaved(archiveIndex);
            Assert(
                DolocAPI.SendItemAsEmailCallCount == 1 &&
                !document.Slots[0].IsOccupied &&
                document.GameplayCandidate == null,
                "Product did not commit zero sidecar copies after exact outcome-unknown mail reconciliation.");
            product.ReturnedToTitle();
            product.DeactivateOwner("RuntimeShutdown");
            DolocAPI.ResetInventory();
        }

        private static void
            CompatibilityDurableMailOutcomeUnknownReconcilesWithoutReplay()
        {
            ResetOwners();
            const string ownerId =
                "fixture.compatibility.durable-mail-unknown";
            const int archiveIndex = 2;
            string scenarioRoot =
                CreateFixtureScenarioRoot(
                    "equipment-compat-durable-mail-unknown");
            var runtime =
                new DtmApiRuntime(
                    new FakeHost(scenarioRoot),
                    new ConfigMenuRegistry());
            string nativeSavePath =
                Path.Combine(
                    scenarioRoot,
                    "native-save-2.sav");
            File.WriteAllText(
                nativeSavePath,
                "compat-durable-mail-preimage");
            var manager =
                new DolocTown.GameData.AgentEquipmentManager();
            ConfigureCompatibilitySaveFixtureNative(
                manager,
                nativeSavePath,
                archiveIndex,
                "fixture-player",
                backpackShieldCount: 1);
            var archive =
                (ColdFixtureArchive)(DolocAPI.archiveHandle ??
                    throw new InvalidOperationException(
                        "Compatibility durable mail archive was unavailable."));
            object service = CreateService(runtime);
            Assert(
                Register(
                    service,
                    ownerId,
                    enabled: true,
                    extraSlots: 24).Success &&
                Equip(
                    service,
                    ownerId,
                    "fixture.extra.1",
                    "fixture-shield").Success,
                "Compatibility durable mail fixture could not create one Working item.");
            Invoke(
                service,
                "NotifyEquipmentSlotsSaveSaving",
                archiveIndex);
            File.WriteAllText(
                nativeSavePath,
                "compat-durable-mail-committed");
            Invoke(
                service,
                "NotifyEquipmentSlotsSaveSaved",
                archiveIndex);

            DolocAPI.BackpackPlacementAvailable = false;
            object[] exactMail =
            {
                new FixtureEmail
                {
                    Id = "send_item_template",
                    emailAttaches = new object[]
                    {
                        new DolocTown.EmailAttachReward
                        {
                            isAccept = false,
                            reward =
                                new DolocTown.RewardItem
                                {
                                    itemName =
                                        "fixture-shield",
                                    itemCount = 1
                                }
                        }
                    }
                }
            };
            DolocAPI.SendItemAsEmailOverride =
                (_, _) =>
                {
                    archive.farmData.emailManager.emails =
                        new object();
                    return true;
                };
            bool ownerRemovalDeferred = false;
            try
            {
                RemoveOwner(
                    service,
                    ownerId,
                    "durable mail outcome-unknown fixture");
            }
            catch (InvalidOperationException)
            {
                ownerRemovalDeferred = true;
            }
            bool unreadableSaveRejected = false;
            try
            {
                Invoke(
                    service,
                    "NotifyEquipmentSlotsSaveSaving",
                    archiveIndex);
            }
            catch (Exception)
            {
                unreadableSaveRejected = true;
            }
            Assert(
                ownerRemovalDeferred &&
                unreadableSaveRejected &&
                DolocAPI.SendItemAsEmailCallCount == 1 &&
                CountOwnerResources(service, ownerId) > 0,
                "Compatibility durable owner recovery did not retain one non-replayed outcome-unknown journal while SaveGame was blocked.");

            archive.farmData.emailManager.emails =
                exactMail;
            Invoke(
                service,
                "NotifyEquipmentSlotsSaveSaving",
                archiveIndex);
            Assert(
                DolocAPI.SendItemAsEmailCallCount == 1 &&
                !GetSlots(service, ownerId)
                    .Any(slot => slot.IsOccupied),
                "Compatibility durable journal did not reconcile exact +1 mail evidence without replay.");
            File.WriteAllText(
                nativeSavePath,
                "compat-durable-mail-saved");
            Invoke(
                service,
                "NotifyEquipmentSlotsSaveSaved",
                archiveIndex);
            Assert(
                DolocAPI.SendItemAsEmailCallCount == 1 &&
                !GetSlots(service, ownerId)
                    .Any(slot => slot.IsOccupied),
                "Compatibility durable journal did not finalize to one mail and zero sidecar copies.");
            RemoveOwner(
                service,
                ownerId,
                "RuntimeShutdown");
            Assert(
                CountOwnerResources(service, ownerId) == 0,
                "Compatibility durable mail fixture retained owner resources after terminal cleanup.");
            Cleanup(CompatibilityOwner);
            DolocAPI.ResetInventory();
        }

        private static void
            CompatibilityMailOutcomeUnknownReconcilesWithoutReplay()
        {
            ResetOwners();
            const string ownerId =
                "fixture.compatibility.mail-outcome-unknown";
            const int archiveIndex = 2;
            string scenarioRoot =
                CreateFixtureScenarioRoot(
                    "equipment-compat-mail-unknown");
            var runtime =
                new DtmApiRuntime(
                    new FakeHost(scenarioRoot),
                    new ConfigMenuRegistry());
            string nativeSavePath =
                Path.Combine(
                    scenarioRoot,
                    "native-save-2.sav");
            File.WriteAllText(
                nativeSavePath,
                "compat-mail-unknown-preimage");
            var manager =
                new DolocTown.GameData.AgentEquipmentManager();
            ConfigureCompatibilitySaveFixtureNative(
                manager,
                nativeSavePath,
                archiveIndex,
                "fixture-player",
                backpackShieldCount: 1);
            var archive =
                (ColdFixtureArchive)(DolocAPI.archiveHandle ??
                    throw new InvalidOperationException(
                        "Compatibility mail fixture archive was unavailable."));
            object service = CreateService(runtime);
            Assert(
                Register(
                    service,
                    ownerId,
                    enabled: true,
                    extraSlots: 24).Success &&
                Equip(
                    service,
                    ownerId,
                    "fixture.extra.1",
                    "fixture-shield").Success,
                "Compatibility mail fixture could not create one Working extra-slot item.");
            Invoke(
                service,
                "NotifyEquipmentSlotsSaveSaving",
                archiveIndex);
            File.WriteAllText(
                nativeSavePath,
                "compat-mail-unknown-committed");
            Invoke(
                service,
                "NotifyEquipmentSlotsSaveSaved",
                archiveIndex);

            DolocAPI.BackpackPlacementAvailable = false;
            archive.farmData.emailManager.emails =
                new object();
            EquipmentSlotEquipResult preflight =
                Unequip(
                    service,
                    ownerId,
                    "fixture.extra.1");
            Assert(
                !preflight.Success &&
                DolocAPI.SendItemAsEmailCallCount == 0 &&
                GetSlots(service, ownerId)
                    .Single(slot =>
                        slot.SlotId ==
                        "fixture.extra.1")
                    .IsOccupied,
                "Unreadable Compatibility Host mail preflight called native mail or released the sidecar item.");

            archive.farmData.emailManager.emails =
                Array.Empty<object>();
            object[] exactMail =
            {
                new FixtureEmail
                {
                    Id = "send_item_template",
                    emailAttaches = new object[]
                    {
                        new DolocTown.EmailAttachReward
                        {
                            isAccept = false,
                            reward =
                                new DolocTown.RewardItem
                                {
                                    itemName =
                                        "fixture-shield",
                                    itemCount = 1
                                }
                        }
                    }
                }
            };
            DolocAPI.SendItemAsEmailOverride =
                (itemId, count) =>
                {
                    archive.farmData.emailManager.emails =
                        exactMail;
                    archive.farmData.emailManager.emails =
                        new object();
                    return string.Equals(
                            itemId,
                            "fixture-shield",
                            StringComparison.Ordinal) &&
                        count == 1;
                };
            EquipmentSlotEquipResult unknown =
                Unequip(
                    service,
                    ownerId,
                    "fixture.extra.1");
            Assert(
                !unknown.Success &&
                DolocAPI.SendItemAsEmailCallCount == 1 &&
                GetSlots(service, ownerId)
                    .Single(slot =>
                        slot.SlotId ==
                        "fixture.extra.1")
                    .IsOccupied,
                "Compatibility Host did not retain exactly one Working sidecar item after post-mail evidence became unreadable.");

            bool unreadableSaveRejected = false;
            try
            {
                Invoke(
                    service,
                    "NotifyEquipmentSlotsSaveSaving",
                    archiveIndex);
            }
            catch (Exception)
            {
                unreadableSaveRejected = true;
            }
            Assert(
                unreadableSaveRejected &&
                DolocAPI.SendItemAsEmailCallCount == 1,
                "Compatibility Host did not reject SaveGame, or retried native mail, while placement evidence remained unreadable.");

            archive.farmData.emailManager.emails =
                exactMail;
            Invoke(
                service,
                "NotifyEquipmentSlotsSaveSaving",
                archiveIndex);
            Assert(
                DolocAPI.SendItemAsEmailCallCount == 1 &&
                !GetSlots(service, ownerId)
                    .Any(slot => slot.IsOccupied) &&
                EquipmentSlotNativeMailEvidence
                    .CountUnacceptedDtmapiItemMail(
                        typeof(DolocAPI),
                        "fixture-shield") == 1,
                "Exact +1 mail evidence did not converge the Compatibility Host Working item without a second native mail call.");
            File.WriteAllText(
                nativeSavePath,
                "compat-mail-unknown-saved");
            Invoke(
                service,
                "NotifyEquipmentSlotsSaveSaved",
                archiveIndex);
            Assert(
                DolocAPI.SendItemAsEmailCallCount == 1 &&
                !GetSlots(service, ownerId)
                    .Any(slot => slot.IsOccupied),
                "Compatibility Host did not commit zero sidecar copies after exact outcome-unknown mail reconciliation.");
            Invoke(
                service,
                "NotifyEquipmentSlotsReturnedToTitle");
            Cleanup(CompatibilityOwner);
            DolocAPI.ResetInventory();
        }

        private static void
            CompatibilityGameplayCommitAndOwnerRecoverySemantics()
        {
            ResetOwners();
            const string ownerId =
                "fixture.compatibility.save-semantics";
            const int archiveIndex = 2;
            const string playerName = "fixture-player";
            string sessionRoot =
                Environment.GetEnvironmentVariable(
                    "DTMAPI_TEST_SESSION_ROOT")
                ?? throw new InvalidOperationException(
                    "Compatibility save-semantics fixtures require the managed DTMAPI test session.");
            string scenarioRoot =
                @"\\?\" +
                Path.Combine(
                    sessionRoot,
                    "equipment-compat-save-" +
                    (++fixtureSequence).ToString());
            Directory.CreateDirectory(
                Path.Combine(
                    scenarioRoot,
                    "BepInEx",
                    "plugins"));
            var host =
                new FakeHost(scenarioRoot);
            var runtime =
                new DtmApiRuntime(
                    host,
                    new ConfigMenuRegistry());
            string nativeSavePath =
                Path.Combine(
                    scenarioRoot,
                    "native-save-2.sav");
            File.WriteAllText(
                nativeSavePath,
                "compatibility-native-preimage");
            var manager =
                new DolocTown.GameData.AgentEquipmentManager();
            ConfigureCompatibilitySaveFixtureNative(
                manager,
                nativeSavePath,
                archiveIndex,
                playerName,
                backpackShieldCount: 2);
            string sidecarPath =
                Path.Combine(
                    runtime.Paths.ConfigPath,
                    "protected-items",
                    "equipment-slots",
                    "slot-2",
                    "equipment-slots-" +
                    ownerId +
                    ".json");

            object service = CreateService(runtime);
            EquipmentSlotsRegisterResult registered =
                Register(
                    service,
                    ownerId,
                    enabled: true,
                    extraSlots: 24);
            Assert(
                registered.Success &&
                registered.ExtraAttributeSlots == 24,
                "The frozen Compatibility Host did not retain the legacy 0..24 slot range.");
            EquipmentSlotEquipResult firstWorking =
                Equip(
                    service,
                    ownerId,
                    "fixture.extra.1",
                    "fixture-shield");
            Assert(
                firstWorking.Success &&
                DolocAPI.CountItem(
                    "fixture-shield",
                    false) == 1 &&
                !File.Exists(sidecarPath),
                "Ordinary compatibility equip must remain in-memory Working before SaveSaving.");

            try
            {
                Invoke(
                    service,
                    "NotifyEquipmentSlotsSaveSaving",
                    archiveIndex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Compatibility gameplay SaveSaving failed. logs=" +
                    string.Join(
                        " | ",
                        host.Logs),
                    ex);
            }
            Assert(
                File.Exists(sidecarPath) &&
                File.ReadAllText(sidecarPath)
                    .IndexOf(
                        "\"gameplayCandidate\"",
                        StringComparison.Ordinal) >= 0,
                "SaveSaving did not persist an explicitly uncommitted compatibility gameplay candidate.");

            Cleanup(CompatibilityOwner);
            ConfigureCompatibilitySaveFixtureNative(
                manager,
                nativeSavePath,
                archiveIndex,
                playerName,
                backpackShieldCount: 2);
            object discardedService =
                CreateService(runtime);
            Assert(
                Register(
                    discardedService,
                    ownerId,
                    enabled: true,
                    extraSlots: 24).Success &&
                !GetSlots(discardedService, ownerId)
                    .Any(slot => slot.IsOccupied) &&
                File.ReadAllText(sidecarPath)
                    .IndexOf(
                        "\"gameplayCandidate\"",
                        StringComparison.Ordinal) < 0,
                "An unchanged native preimage must discard the uncommitted gameplay candidate without native replay.");

            EquipmentSlotEquipResult committedEquip =
                Equip(
                    discardedService,
                    ownerId,
                    "fixture.extra.1",
                    "fixture-shield");
            Assert(
                committedEquip.Success,
                "Compatibility normal-save setup equip failed.");
            Invoke(
                discardedService,
                "NotifyEquipmentSlotsSaveSaving",
                archiveIndex);
            File.WriteAllText(
                nativeSavePath,
                "compatibility-native-committed-1");
            Invoke(
                discardedService,
                "NotifyEquipmentSlotsSaveSaved",
                archiveIndex);
            byte[] committedSidecar =
                File.ReadAllBytes(sidecarPath);
            Assert(
                GetSlots(discardedService, ownerId)
                    .Single(slot =>
                        slot.SlotId ==
                        "fixture.extra.1")
                    .IsOccupied &&
                File.ReadAllText(sidecarPath)
                    .IndexOf(
                        "\"gameplayCandidate\"",
                        StringComparison.Ordinal) < 0,
                "SaveSaved did not promote and clean the compatibility gameplay candidate.");

            EquipmentSlotEquipResult workingUnequip =
                Unequip(
                    discardedService,
                    ownerId,
                    "fixture.extra.1");
            Assert(
                workingUnequip.Success &&
                EqualBytes(
                    committedSidecar,
                    File.ReadAllBytes(sidecarPath)),
                "Ordinary compatibility unequip wrote the committed sidecar before native SaveGame.");
            Invoke(
                discardedService,
                "NotifyEquipmentSlotsReturnedToTitle");
            Cleanup(CompatibilityOwner);
            ConfigureCompatibilitySaveFixtureNative(
                manager,
                nativeSavePath,
                archiveIndex,
                playerName,
                backpackShieldCount: 1);
            object noSaveReload =
                CreateService(runtime);
            Assert(
                Register(
                    noSaveReload,
                    ownerId,
                    enabled: true,
                    extraSlots: 24).Success &&
                GetSlots(noSaveReload, ownerId)
                    .Single(slot =>
                        slot.SlotId ==
                        "fixture.extra.1")
                    .IsOccupied &&
                EqualBytes(
                    committedSidecar,
                    File.ReadAllBytes(sidecarPath)),
                "No-save title/cold reload did not restore the last committed compatibility slot.");

            object?[] shieldArguments =
            {
                ownerId,
                "fixture.extra.1",
                3,
                0
            };
            RequireMethod(
                noSaveReload.GetType(),
                "TryBlockExtraSlotShield")
                .Invoke(
                    noSaveReload,
                    shieldArguments);
            Assert(
                EqualBytes(
                    committedSidecar,
                    File.ReadAllBytes(sidecarPath)),
                "Compatibility shield damage persisted ahead of native SaveGame.");
            Invoke(
                noSaveReload,
                "NotifyEquipmentSlotsReturnedToTitle");
            Cleanup(CompatibilityOwner);
            ConfigureCompatibilitySaveFixtureNative(
                manager,
                nativeSavePath,
                archiveIndex,
                playerName,
                backpackShieldCount: 1);
            object shieldReload =
                CreateService(runtime);
            Assert(
                Register(
                    shieldReload,
                    ownerId,
                    enabled: true,
                    extraSlots: 24).Success &&
                ((string)Invoke(
                    shieldReload,
                    "GetEquipmentSlotsStateSummaryForFixture",
                    ownerId)).IndexOf(
                        ":10/10",
                        StringComparison.Ordinal) >= 0,
                "No-save compatibility shield damage did not roll back to the committed shield value.");

            Assert(
                Unequip(
                    shieldReload,
                    ownerId,
                    "fixture.extra.1").Success,
                "Native-success crash-window unequip failed.");
            Invoke(
                shieldReload,
                "NotifyEquipmentSlotsSaveSaving",
                archiveIndex);
            File.WriteAllText(
                nativeSavePath,
                "compatibility-native-committed-2");
            Cleanup(CompatibilityOwner);
            ConfigureCompatibilitySaveFixtureNative(
                manager,
                nativeSavePath,
                archiveIndex,
                playerName,
                backpackShieldCount: 2);
            object promotedOnRestart =
                CreateService(runtime);
            Assert(
                Register(
                    promotedOnRestart,
                    ownerId,
                    enabled: true,
                    extraSlots: 24).Success &&
                !GetSlots(
                    promotedOnRestart,
                    ownerId).Any(slot =>
                        slot.IsOccupied) &&
                File.ReadAllText(sidecarPath)
                    .IndexOf(
                        "\"gameplayCandidate\"",
                        StringComparison.Ordinal) < 0,
                "A proven native commit before SaveSaved did not promote the compatibility working projection.");

            Assert(
                Equip(
                    promotedOnRestart,
                    ownerId,
                    "fixture.extra.1",
                    "fixture-shield").Success,
                "Committed-tombstone crash-window equip failed.");
            Invoke(
                promotedOnRestart,
                "NotifyEquipmentSlotsSaveSaving",
                archiveIndex);
            File.WriteAllText(
                nativeSavePath,
                "compatibility-native-committed-3");
            PersistCompatibilityGameplayTombstone(
                promotedOnRestart,
                ownerId,
                nativeSavePath);
            Assert(
                File.ReadAllText(sidecarPath)
                    .IndexOf(
                        "\"phase\":1",
                        StringComparison.Ordinal) >= 0,
                "Fixture did not persist the committed gameplay tombstone.");
            Cleanup(CompatibilityOwner);
            ConfigureCompatibilitySaveFixtureNative(
                manager,
                nativeSavePath,
                archiveIndex,
                playerName,
                backpackShieldCount: 1);
            object tombstoneReload =
                CreateService(runtime);
            Assert(
                Register(
                    tombstoneReload,
                    ownerId,
                    enabled: true,
                    extraSlots: 24).Success &&
                GetSlots(tombstoneReload, ownerId)
                    .Single(slot =>
                        slot.SlotId ==
                        "fixture.extra.1")
                    .IsOccupied &&
                File.ReadAllText(sidecarPath)
                    .IndexOf(
                        "\"gameplayCandidate\"",
                        StringComparison.Ordinal) < 0,
                "Committed gameplay tombstone cleanup did not preserve the promoted slot exactly once.");

            object?[] damagedShieldArguments =
            {
                ownerId,
                "fixture.extra.1",
                3,
                0
            };
            RequireMethod(
                tombstoneReload.GetType(),
                "TryBlockExtraSlotShield")
                .Invoke(
                    tombstoneReload,
                    damagedShieldArguments);
            byte[] committedBeforeDirtyProjection =
                File.ReadAllBytes(sidecarPath);
            object?[] projectionArguments =
            {
                ownerId,
                string.Empty
            };
            object? projectionResult =
                RequireMethod(
                    tombstoneReload.GetType(),
                    "PrepareEquipmentSlotOwnerRecoveryProjection")
                    .Invoke(
                        tombstoneReload,
                        projectionArguments);
            Assert(
                projectionResult is bool projected &&
                !projected &&
                EqualBytes(
                    committedBeforeDirtyProjection,
                    File.ReadAllBytes(sidecarPath)) &&
                (projectionArguments[1] as string ??
                 string.Empty).IndexOf(
                    "deferred",
                    StringComparison.OrdinalIgnoreCase) >= 0,
                "Explicit OwnerRecovery must defer rather than convert unsaved shield damage into durable recovery. message=" +
                (projectionArguments[1] as string ??
                 string.Empty));
            Invoke(
                tombstoneReload,
                "NotifyEquipmentSlotsReturnedToTitle");
            Cleanup(CompatibilityOwner);
            ConfigureCompatibilitySaveFixtureNative(
                manager,
                nativeSavePath,
                archiveIndex,
                playerName,
                backpackShieldCount: 1);
            tombstoneReload = CreateService(runtime);
            Assert(
                Register(
                    tombstoneReload,
                    ownerId,
                    enabled: true,
                    extraSlots: 24).Success &&
                ((string)Invoke(
                    tombstoneReload,
                    "GetEquipmentSlotsStateSummaryForFixture",
                    ownerId)).IndexOf(
                        ":10/10",
                        StringComparison.Ordinal) >= 0 &&
                EqualBytes(
                    committedBeforeDirtyProjection,
                    File.ReadAllBytes(sidecarPath)),
                "Title/restart did not discard unsaved shield damage before clean owner recovery.");

            DolocAPI.SetItemCount(
                "fixture-shield",
                1);
            Assert(
                Equip(
                    tombstoneReload,
                    ownerId,
                    "fixture.extra.1",
                    "fixture-shield").Success,
                "Same-item replacement setup failed.");
            byte[] committedBeforeDirtyOwnerRemoval =
                File.ReadAllBytes(sidecarPath);
            bool dirtyOwnerRemovalDeferred = false;
            try
            {
                RemoveOwner(
                    tombstoneReload,
                    ownerId);
            }
            catch (InvalidOperationException)
            {
                dirtyOwnerRemovalDeferred = true;
            }
            Assert(
                dirtyOwnerRemovalDeferred &&
                CountOwnerResources(
                    tombstoneReload,
                    ownerId) > 0 &&
                EqualBytes(
                    committedBeforeDirtyOwnerRemoval,
                    File.ReadAllBytes(sidecarPath)),
                "Dirty compatibility owner removal must defer, retain roots, and leave committed authority unchanged.");
            Invoke(
                tombstoneReload,
                "NotifyEquipmentSlotsReturnedToTitle");
            Cleanup(CompatibilityOwner);
            ConfigureCompatibilitySaveFixtureNative(
                manager,
                nativeSavePath,
                archiveIndex,
                playerName,
                backpackShieldCount: 1);
            tombstoneReload = CreateService(runtime);
            Assert(
                Register(
                    tombstoneReload,
                    ownerId,
                    enabled: true,
                    extraSlots: 24).Success,
                "Clean owner-recovery reload failed after discarding dirty replacement.");
            Invoke(
                tombstoneReload,
                "NotifyEquipmentSlotsSaveSaving",
                archiveIndex);
            File.WriteAllText(
                nativeSavePath,
                "compatibility-native-committed-4");
            Invoke(
                tombstoneReload,
                "NotifyEquipmentSlotsSaveSaved",
                archiveIndex);
            RemoveOwner(
                tombstoneReload,
                ownerId);
            string recoveryJson =
                File.ReadAllText(sidecarPath);
            Assert(
                DolocAPI.CountItem(
                    "fixture-shield",
                    false) == 2 &&
                recoveryJson.IndexOf(
                    "\"origin\":2",
                    StringComparison.Ordinal) >= 0 &&
                recoveryJson.IndexOf(
                    "\"gameplayCandidate\"",
                    StringComparison.Ordinal) < 0 &&
                CountOwnerResources(
                    tombstoneReload,
                    ownerId) == 0,
                "Owner cleanup did not move the exact Working/Committed held-item union into typed OwnerRecovery without duplication. native=" +
                DolocAPI.CountItem(
                    "fixture-shield",
                    false) +
                ", resources=" +
                CountOwnerResources(
                    tombstoneReload,
                    ownerId) +
                ", json=" +
                recoveryJson);

            string unknownJson =
                Regex.Replace(
                    recoveryJson,
                    ",?\"origin\":2",
                    string.Empty,
                    RegexOptions.CultureInvariant);
            File.WriteAllText(
                sidecarPath,
                unknownJson);
            byte[] unknownBefore =
                File.ReadAllBytes(sidecarPath);
            int nativeBefore =
                DolocAPI.CountItem(
                    "fixture-shield",
                    false);
            Cleanup(CompatibilityOwner);
            object unknownReload =
                CreateService(runtime);
            Register(
                unknownReload,
                ownerId,
                enabled: true,
                extraSlots: 24);
            Assert(
                DolocAPI.CountItem(
                    "fixture-shield",
                    false) == nativeBefore &&
                EqualBytes(
                    unknownBefore,
                    File.ReadAllBytes(sidecarPath)),
                "An old journal without typed origin was replayed or rewritten instead of retained fail-closed.");
            Cleanup(CompatibilityOwner);
            DolocAPI.ResetInventory();
        }

        private static void
            CompatibilityRuntimeShutdownDiscardsUnsavedWorkingState()
        {
            RunCompatibilityRuntimeShutdownScenario(
                equipWorkingItem: true);
            RunCompatibilityRuntimeShutdownScenario(
                equipWorkingItem: false);
        }

        private static void
            RunCompatibilityRuntimeShutdownScenario(
                bool equipWorkingItem)
        {
            ResetOwners();
            const int archiveIndex = 2;
            const string playerName = "fixture-player";
            string ownerId =
                "fixture.compatibility.runtime-shutdown." +
                (equipWorkingItem ? "equip" : "unequip");
            string scenarioRoot =
                CreateFixtureScenarioRoot(
                    "equipment-compat-runtime-shutdown");
            var runtime =
                new DtmApiRuntime(
                    new FakeHost(scenarioRoot),
                    new ConfigMenuRegistry());
            string nativeSavePath =
                Path.Combine(
                    scenarioRoot,
                    "native-save-2.sav");
            File.WriteAllText(
                nativeSavePath,
                "runtime-shutdown-preimage");
            var manager =
                new DolocTown.GameData.AgentEquipmentManager();
            ConfigureCompatibilitySaveFixtureNative(
                manager,
                nativeSavePath,
                archiveIndex,
                playerName,
                backpackShieldCount: 1);
            string sidecarPath =
                GetCompatibilitySidecarPath(
                    runtime,
                    ownerId);
            object service =
                CreateService(runtime);
            Assert(
                Register(
                    service,
                    ownerId,
                    enabled: true,
                    extraSlots: 24).Success &&
                Equip(
                    service,
                    ownerId,
                    "fixture.extra.1",
                    "fixture-shield").Success,
                "Compatibility RuntimeShutdown fixture could not prepare committed state.");
            Invoke(
                service,
                "NotifyEquipmentSlotsSaveSaving",
                archiveIndex);
            File.WriteAllText(
                nativeSavePath,
                "runtime-shutdown-committed");
            Invoke(
                service,
                "NotifyEquipmentSlotsSaveSaved",
                archiveIndex);
            byte[] committed =
                File.ReadAllBytes(sidecarPath);

            bool mutated;
            if (equipWorkingItem)
            {
                DolocAPI.SetItemCount(
                    "fixture-shield",
                    1);
                mutated =
                    Equip(
                        service,
                        ownerId,
                        "fixture.extra.2",
                        "fixture-shield").Success;
            }
            else
            {
                mutated =
                    Unequip(
                        service,
                        ownerId,
                        "fixture.extra.1").Success;
            }
            Assert(
                mutated &&
                EqualBytes(
                    committed,
                    File.ReadAllBytes(sidecarPath)),
                "Compatibility RuntimeShutdown fixture wrote an unsaved Working " +
                (equipWorkingItem ? "equip" : "unequip") +
                ".");
            RemoveOwner(
                service,
                ownerId,
                "RuntimeShutdown");
            Assert(
                EqualBytes(
                    committed,
                    File.ReadAllBytes(sidecarPath)) &&
                CountOwnerResources(
                    service,
                    ownerId) == 0,
                "Compatibility RuntimeShutdown recovered/persisted unsaved Working state or retained owner roots.");
            AssertOwnerCount(
                CompatibilityOwner,
                0);
            DolocAPI.ResetInventory();
        }

        private static void
            CompatibilityPresentJournalFailuresRemainBlocked()
        {
            RunCompatibilityBlockedJournalScenario(
                "unknown-origin",
                origin: 0,
                attemptStarted: false,
                fingerprintDisagrees: false,
                failDurableWrite: false,
                expectNativePlacement: false);
            RunCompatibilityBlockedJournalScenario(
                "fingerprint-native-disagreement",
                origin: 2,
                attemptStarted: true,
                fingerprintDisagrees: true,
                failDurableWrite: false,
                expectNativePlacement: false);
            RunCompatibilityBlockedJournalScenario(
                "placement-write-failure",
                origin: 2,
                attemptStarted: false,
                fingerprintDisagrees: false,
                failDurableWrite: true,
                expectNativePlacement: false);
        }

        private static void RunCompatibilityBlockedJournalScenario(
            string label,
            int origin,
            bool attemptStarted,
            bool fingerprintDisagrees,
            bool failDurableWrite,
            bool expectNativePlacement)
        {
            ResetOwners();
            const int archiveIndex = 2;
            const string playerName = "fixture-player";
            string ownerId =
                "fixture.compatibility.blocked." +
                label;
            string scenarioRoot =
                CreateFixtureScenarioRoot(
                    "equipment-compat-blocked-journal");
            var runtime =
                new DtmApiRuntime(
                    new FakeHost(scenarioRoot),
                    new ConfigMenuRegistry());
            string nativeSavePath =
                Path.Combine(
                    scenarioRoot,
                    "native-save-2.sav");
            File.WriteAllText(
                nativeSavePath,
                "blocked-journal-" + label);
            var manager =
                new DolocTown.GameData.AgentEquipmentManager();
            ConfigureCompatibilitySaveFixtureNative(
                manager,
                nativeSavePath,
                archiveIndex,
                playerName,
                backpackShieldCount: 0);
            string sidecarPath =
                GetCompatibilitySidecarPath(
                    runtime,
                    ownerId);
            Directory.CreateDirectory(
                Path.GetDirectoryName(sidecarPath) ??
                scenarioRoot);
            string currentFingerprint =
                BuildFixtureSaveFingerprint(
                    nativeSavePath);
            string preFingerprint =
                fingerprintDisagrees
                    ? "fixture-different-native-fingerprint"
                    : currentFingerprint;
            File.WriteAllText(
                sidecarPath,
                BuildCompatibilityJournalJson(
                    ownerId,
                    archiveIndex,
                    playerName,
                    origin,
                    attemptStarted,
                    preFingerprint,
                    beforeBackpackCount: 0));
            byte[] sidecarAuthority =
                File.ReadAllBytes(sidecarPath);
            string legacyPath =
                Path.Combine(
                    runtime.Paths.ConfigPath,
                    "equipment-slots-" +
                    ownerId +
                    ".json");
            File.WriteAllText(
                legacyPath,
                "{\"legacy\":\"must-not-load-or-overwrite\"}");
            byte[] legacyAuthority =
                File.ReadAllBytes(legacyPath);

            object service =
                failDurableWrite
                    ? CreateService(
                        runtime,
                        _ => false)
                    : CreateService(runtime);
            EquipmentSlotsRegisterResult registered =
                Register(
                    service,
                    ownerId,
                    enabled: true,
                    extraSlots: 24);
            int nativeAfterReconcile =
                DolocAPI.CountItem(
                    "fixture-shield",
                    false);
            EquipmentSlotEquipResult equip =
                Equip(
                    service,
                    ownerId,
                    "fixture.extra.1",
                    "fixture-shield");
            EquipmentSlotEquipResult unequip =
                Unequip(
                    service,
                    ownerId,
                    "fixture.extra.1");
            bool saveSavingRejected = false;
            try
            {
                Invoke(
                    service,
                    "NotifyEquipmentSlotsSaveSaving",
                    archiveIndex);
            }
            catch (InvalidOperationException)
            {
                saveSavingRejected = true;
            }

            Assert(
                !registered.Success &&
                !equip.Success &&
                !unequip.Success &&
                saveSavingRejected &&
                GetSlots(
                    service,
                    ownerId).Count == 0 &&
                CountOwnerResources(
                    service,
                    ownerId) > 0 &&
                EqualBytes(
                    sidecarAuthority,
                    File.ReadAllBytes(sidecarPath)) &&
                EqualBytes(
                    legacyAuthority,
                    File.ReadAllBytes(legacyPath)) &&
                nativeAfterReconcile ==
                    (expectNativePlacement ? 1 : 0),
                "Compatibility present-journal fail-closed invariant regressed for " +
                label +
                ": register=" +
                registered.Success +
                ", equip=" +
                equip.Success +
                ", unequip=" +
                unequip.Success +
                ", saveSavingRejected=" +
                saveSavingRejected +
                ", resources=" +
                CountOwnerResources(
                    service,
                    ownerId) +
                ", native=" +
                nativeAfterReconcile +
                ".");
            RemoveOwner(
                service,
                ownerId,
                "RuntimeShutdown");
            Assert(
                CountOwnerResources(
                    service,
                    ownerId) == 0 &&
                EqualBytes(
                    sidecarAuthority,
                    File.ReadAllBytes(sidecarPath)) &&
                EqualBytes(
                    legacyAuthority,
                    File.ReadAllBytes(legacyPath)),
                "RuntimeShutdown cleanup changed blocked journal authority or retained owner roots for " +
                label +
                ".");
            AssertOwnerCount(
                CompatibilityOwner,
                0);
            DolocAPI.ResetInventory();
        }

        private static void
            CompatibilityDirtyOwnerDeactivationDefersThenCleanRecovery()
        {
            ResetOwners();
            const string ownerId =
                "fixture.compatibility.empty-owner-recovery";
            const int archiveIndex = 2;
            const string playerName = "fixture-player";
            string sessionRoot =
                Environment.GetEnvironmentVariable(
                    "DTMAPI_TEST_SESSION_ROOT")
                ?? throw new InvalidOperationException(
                    "Compatibility empty-recovery fixture requires the managed DTMAPI test session.");
            string scenarioRoot =
                @"\\?\" +
                Path.Combine(
                    sessionRoot,
                    "equipment-compat-empty-" +
                    (++fixtureSequence).ToString());
            Directory.CreateDirectory(
                Path.Combine(
                    scenarioRoot,
                    "BepInEx",
                    "plugins"));
            var emptyRecoveryHost =
                new FakeHost(scenarioRoot);
            var runtime =
                new DtmApiRuntime(
                    emptyRecoveryHost,
                    new ConfigMenuRegistry());
            string nativeSavePath =
                Path.Combine(
                    scenarioRoot,
                    "native-save-2.sav");
            File.WriteAllText(
                nativeSavePath,
                "empty-recovery-preimage");
            var manager =
                new DolocTown.GameData.AgentEquipmentManager();
            ConfigureCompatibilitySaveFixtureNative(
                manager,
                nativeSavePath,
                archiveIndex,
                playerName,
                backpackShieldCount: 2);
            string sidecarPath =
                Path.Combine(
                    runtime.Paths.ConfigPath,
                    "protected-items",
                    "equipment-slots",
                    "slot-2",
                    "equipment-slots-" +
                    ownerId +
                    ".json");

            object service = CreateService(runtime);
            Assert(
                Register(
                    service,
                    ownerId,
                    enabled: true).Success &&
                Equip(
                    service,
                    ownerId,
                    "fixture.extra.1",
                    "fixture-shield").Success,
                "Empty OwnerRecovery fixture setup failed.");
            try
            {
                Invoke(
                    service,
                    "NotifyEquipmentSlotsSaveSaving",
                    archiveIndex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Empty OwnerRecovery SaveSaving failed. logs=" +
                    string.Join(
                        " | ",
                        emptyRecoveryHost.Logs),
                    ex);
            }
            File.WriteAllText(
                nativeSavePath,
                "empty-recovery-committed");
            Invoke(
                service,
                "NotifyEquipmentSlotsSaveSaved",
                archiveIndex);
            byte[] committed =
                File.ReadAllBytes(sidecarPath);
            Assert(
                Unequip(
                    service,
                    ownerId,
                    "fixture.extra.1").Success,
                "Empty OwnerRecovery fixture could not create an in-memory empty Working projection.");
            bool dirtyRemovalDeferred = false;
            try
            {
                RemoveOwner(
                    service,
                    ownerId);
            }
            catch (InvalidOperationException)
            {
                dirtyRemovalDeferred = true;
            }
            Assert(
                dirtyRemovalDeferred &&
                EqualBytes(
                    committed,
                    File.ReadAllBytes(sidecarPath)) &&
                CountOwnerResources(
                    service,
                    ownerId) > 0,
                "Dirty empty Working projection did not defer owner removal with committed authority and roots retained.");
            AssertOwnerCount(
                CompatibilityOwner,
                4);
            EquipmentSlotsRegisterResult dirtyDisable =
                Register(
                    service,
                    ownerId,
                    enabled: false);
            Assert(
                !dirtyDisable.Success &&
                EqualBytes(
                    committed,
                    File.ReadAllBytes(sidecarPath)) &&
                CountOwnerResources(
                    service,
                    ownerId) > 0,
                "Dirty configuration disable must defer with committed authority and the compatibility owner/hooks retained.");
            AssertOwnerCount(
                CompatibilityOwner,
                4);
            Invoke(
                service,
                "NotifyEquipmentSlotsReturnedToTitle");
            Cleanup(CompatibilityOwner);
            ConfigureCompatibilitySaveFixtureNative(
                manager,
                nativeSavePath,
                archiveIndex,
                playerName,
                backpackShieldCount: 1);
            service = CreateService(runtime);
            Assert(
                Register(
                    service,
                    ownerId,
                    enabled: true).Success &&
                GetSlots(
                    service,
                    ownerId).Single(slot =>
                        slot.SlotId ==
                        "fixture.extra.1").IsOccupied &&
                EqualBytes(
                    committed,
                    File.ReadAllBytes(sidecarPath)),
                "Title/restart did not discard dirty Working and restore the last committed occupied projection.");
            Invoke(
                service,
                "NotifyEquipmentSlotsSaveSaving",
                archiveIndex);
            File.WriteAllText(
                nativeSavePath,
                "empty-recovery-reloaded-committed");
            Invoke(
                service,
                "NotifyEquipmentSlotsSaveSaved",
                archiveIndex);
            RemoveOwner(
                service,
                ownerId);
            string recoveredJson =
                File.ReadAllText(sidecarPath);
            Assert(
                recoveredJson.IndexOf(
                    "\"origin\":2",
                    StringComparison.Ordinal) >= 0 &&
                DolocAPI.CountItem(
                    "fixture-shield",
                    false) == 2 &&
                CountOwnerResources(
                    service,
                    ownerId) == 0,
                "Clean committed owner removal did not stage a typed OwnerRecovery journal and release owner roots. native=" +
                DolocAPI.CountItem(
                    "fixture-shield",
                    false).ToString() +
                ", resources=" +
                CountOwnerResources(
                    service,
                    ownerId).ToString() +
                ", json=" +
                recoveredJson);

            File.WriteAllText(
                nativeSavePath,
                "empty-recovery-native-committed");
            Cleanup(CompatibilityOwner);
            ConfigureCompatibilitySaveFixtureNative(
                manager,
                nativeSavePath,
                archiveIndex,
                playerName,
                backpackShieldCount: 2);
            object coldService =
                CreateService(runtime);
            Assert(
                Register(
                    coldService,
                    ownerId,
                    enabled: true).Success &&
                !GetSlots(
                    coldService,
                    ownerId).Any(slot =>
                        slot.IsOccupied) &&
                DolocAPI.CountItem(
                    "fixture-shield",
                    false) == 2,
                "Cold reconciliation did not finalize typed OwnerRecovery after native save evidence.");
            RemoveOwner(
                coldService,
                ownerId);
            Cleanup(CompatibilityOwner);
            DolocAPI.ResetInventory();
        }

        private static void
            CompatibilityInvalidScopedStorageFailsClosed()
        {
            ResetOwners();
            const string ownerId =
                "fixture.compatibility.invalid-storage";
            const int archiveIndex = 2;
            const string playerName = "fixture-player";
            string sessionRoot =
                Environment.GetEnvironmentVariable(
                    "DTMAPI_TEST_SESSION_ROOT")
                ?? throw new InvalidOperationException(
                    "Compatibility invalid-storage fixture requires the managed DTMAPI test session.");
            string scenarioRoot =
                @"\\?\" +
                Path.Combine(
                    sessionRoot,
                    "equipment-compat-invalid-" +
                    (++fixtureSequence).ToString());
            Directory.CreateDirectory(
                Path.Combine(
                    scenarioRoot,
                    "BepInEx",
                    "plugins"));
            var runtime =
                new DtmApiRuntime(
                    new FakeHost(scenarioRoot),
                    new ConfigMenuRegistry());
            string nativeSavePath =
                Path.Combine(
                    scenarioRoot,
                    "native-save-2.sav");
            File.WriteAllText(
                nativeSavePath,
                "invalid-storage-preimage");
            var manager =
                new DolocTown.GameData.AgentEquipmentManager();
            ConfigureCompatibilitySaveFixtureNative(
                manager,
                nativeSavePath,
                archiveIndex,
                playerName,
                backpackShieldCount: 2);
            string sidecarPath =
                Path.Combine(
                    runtime.Paths.ConfigPath,
                    "protected-items",
                    "equipment-slots",
                    "slot-2",
                    "equipment-slots-" +
                    ownerId +
                    ".json");
            string legacyPath =
                Path.Combine(
                    runtime.Paths.ConfigPath,
                    "equipment-slots-" +
                    ownerId +
                    ".json");
            object service = CreateService(runtime);
            Assert(
                Register(
                    service,
                    ownerId,
                    enabled: true).Success &&
                Equip(
                    service,
                    ownerId,
                    "fixture.extra.1",
                    "fixture-shield").Success,
                "Invalid-storage fixture setup failed.");
            Invoke(
                service,
                "NotifyEquipmentSlotsSaveSaving",
                archiveIndex);
            string validCandidate =
                File.ReadAllText(sidecarPath);
            File.Copy(
                sidecarPath,
                legacyPath);
            string invalidCandidate =
                validCandidate.Replace(
                    "\"origin\":1",
                    "\"origin\":0");
            Assert(
                !string.Equals(
                    validCandidate,
                    invalidCandidate,
                    StringComparison.Ordinal),
                "Invalid-storage fixture did not locate the gameplay origin field.");
            File.WriteAllText(
                sidecarPath,
                invalidCandidate);
            byte[] invalidBefore =
                File.ReadAllBytes(sidecarPath);
            byte[] legacyBefore =
                File.ReadAllBytes(legacyPath);

            Cleanup(CompatibilityOwner);
            object blockedService =
                CreateService(runtime);
            EquipmentSlotsRegisterResult blocked =
                Register(
                    blockedService,
                    ownerId,
                    enabled: true);
            EquipmentSlotEquipResult mutation =
                Equip(
                    blockedService,
                    ownerId,
                    "fixture.extra.1",
                    "fixture-shield");
            Assert(
                !blocked.Success &&
                !mutation.Success &&
                GetSlots(
                    blockedService,
                    ownerId).Count == 0 &&
                EqualBytes(
                    invalidBefore,
                    File.ReadAllBytes(sidecarPath)) &&
                EqualBytes(
                    legacyBefore,
                    File.ReadAllBytes(legacyPath)) &&
                CountOwnerResources(
                    blockedService,
                    ownerId) > 0,
                "Present-but-invalid scoped storage fell back to legacy/empty state, accepted mutation, or was overwritten.");
            RemoveOwner(
                blockedService,
                ownerId);
            AssertOwnerCount(
                CompatibilityOwner,
                0);
            Assert(
                CountOwnerResources(
                    blockedService,
                    ownerId) == 0,
                "Blocked storage owner cleanup retained in-memory resources.");
            DolocAPI.ResetInventory();
        }

        private static void
            CompatibilityTemporaryWriteValidationFailsClosed()
        {
            ResetOwners();
            const string ownerId =
                "fixture.compatibility.write-validation";
            const int archiveIndex = 2;
            const string playerName = "fixture-player";
            string sessionRoot =
                Environment.GetEnvironmentVariable(
                    "DTMAPI_TEST_SESSION_ROOT")
                ?? throw new InvalidOperationException(
                    "Compatibility write-validation fixture requires the managed DTMAPI test session.");
            string scenarioRoot =
                @"\\?\" +
                Path.Combine(
                    sessionRoot,
                    "equipment-compat-write-" +
                    (++fixtureSequence).ToString());
            Directory.CreateDirectory(
                Path.Combine(
                    scenarioRoot,
                    "BepInEx",
                    "plugins"));
            var runtime =
                new DtmApiRuntime(
                    new FakeHost(scenarioRoot),
                    new ConfigMenuRegistry());
            string nativeSavePath =
                Path.Combine(
                    scenarioRoot,
                    "native-save-2.sav");
            File.WriteAllText(
                nativeSavePath,
                "write-validation-preimage");
            var manager =
                new DolocTown.GameData.AgentEquipmentManager();
            ConfigureCompatibilitySaveFixtureNative(
                manager,
                nativeSavePath,
                archiveIndex,
                playerName,
                backpackShieldCount: 2);
            string sidecarPath =
                Path.Combine(
                    runtime.Paths.ConfigPath,
                    "protected-items",
                    "equipment-slots",
                    "slot-2",
                    "equipment-slots-" +
                    ownerId +
                    ".json");
            object baselineService =
                CreateService(runtime);
            Assert(
                Register(
                    baselineService,
                    ownerId,
                    enabled: true).Success &&
                Equip(
                    baselineService,
                    ownerId,
                    "fixture.extra.1",
                    "fixture-shield").Success,
                "Write-validation fixture setup failed.");
            Invoke(
                baselineService,
                "NotifyEquipmentSlotsSaveSaving",
                archiveIndex);
            File.WriteAllText(
                nativeSavePath,
                "write-validation-committed");
            Invoke(
                baselineService,
                "NotifyEquipmentSlotsSaveSaved",
                archiveIndex);
            byte[] committed =
                File.ReadAllBytes(sidecarPath);

            Cleanup(CompatibilityOwner);
            ConfigureCompatibilitySaveFixtureNative(
                manager,
                nativeSavePath,
                archiveIndex,
                playerName,
                backpackShieldCount: 1);
            object gatedService =
                CreateService(
                    runtime,
                    _ => false);
            Assert(
                Register(
                    gatedService,
                    ownerId,
                    enabled: true).Success &&
                Unequip(
                    gatedService,
                    ownerId,
                    "fixture.extra.1").Success,
                "Write-validation fixture could not prepare an ordinary Working mutation.");
            bool rejected = false;
            try
            {
                Invoke(
                    gatedService,
                    "NotifyEquipmentSlotsSaveSaving",
                    archiveIndex);
            }
            catch (InvalidOperationException)
            {
                rejected = true;
            }
            string sidecarDirectory =
                Path.GetDirectoryName(sidecarPath)
                ?? scenarioRoot;
            Assert(
                rejected &&
                EqualBytes(
                    committed,
                    File.ReadAllBytes(sidecarPath)) &&
                !Directory.EnumerateFiles(
                    sidecarDirectory,
                    "*.tmp-*",
                    SearchOption.TopDirectoryOnly).Any(),
                "Rejected temporary sidecar validation replaced the committed file or left a temporary artifact.");
            Cleanup(CompatibilityOwner);
            DolocAPI.ResetInventory();
        }

        private static void RunProductColdRecoveryScenario(
            ProductColdRecoveryState state)
        {
            bool sameItem =
                state ==
                ProductColdRecoveryState
                    .ReplacementPreparedRetry;
            string outgoingItemId =
                sameItem
                    ? "same-item"
                    : "outgoing-item";
            string incomingItemId =
                sameItem
                    ? "same-item"
                    : "incoming-item";
            const int archiveIndex = 2;
            const string playerName = "fixture-player";
            string sessionRoot =
                Environment.GetEnvironmentVariable(
                    "DTMAPI_TEST_SESSION_ROOT")
                ?? throw new InvalidOperationException(
                    "Cold Host fixtures require the managed DTMAPI test session.");
            string scenarioRoot =
                @"\\?\" +
                Path.Combine(
                    sessionRoot,
                    "ech-" +
                    (++fixtureSequence).ToString());
            Directory.CreateDirectory(
                Path.Combine(
                    scenarioRoot,
                    "BepInEx",
                    "plugins"));
            var host = new FakeHost(scenarioRoot);
            var runtime =
                new DtmApiRuntime(
                    host,
                    new ConfigMenuRegistry());
            object service = CreateService(runtime);

            string nativeSavePath =
                Path.Combine(
                    scenarioRoot,
                    "native-save-2.sav");
            File.WriteAllText(
                nativeSavePath,
                "native-preimage-" + state);
            var manager =
                new DolocTown.GameData.AgentEquipmentManager();
            DolocAPI.ResetInventory();
            DolocAPI.SetItemCount(
                sameItem
                    ? outgoingItemId
                    : incomingItemId,
                1);
            DolocAPI.archiveHandle =
                new ColdFixtureArchive(
                    archiveIndex,
                    playerName,
                    playerName,
                    manager);
            DolocAPI.dataPersistenceManager =
                new ColdFixtureDataPersistenceManager(
                    nativeSavePath);

            var scope =
                new EquipmentSlotSaveScope
                {
                    ArchiveIndex = archiveIndex,
                    PlayerName = playerName,
                    CustomPlayerName = playerName,
                    TotalGameSeconds = 1
                };
            var document =
                new EquipmentSlotStorageDocument
                {
                    Scope = scope.Clone(),
                    Generation = 1
                };
            document.Slots[0].ItemId = outgoingItemId;
            document.Slots[0].DisplayName = outgoingItemId;
            EquipmentSlotTransactionJournal journal =
                EquipmentSlotTransactionCoordinator
                    .PrepareReplacement(
                        document,
                        0,
                        new EquipmentSlotStorageEntry
                        {
                            Index = 0,
                            ItemId = incomingItemId,
                            DisplayName = incomingItemId,
                            SkillId = "passive"
                        });

            string preFingerprint =
                BuildFixtureSaveFingerprint(
                    nativeSavePath);
            if (state !=
                ProductColdRecoveryState
                    .ReplacementPreparedRetry)
            {
                EquipmentSlotTransactionCoordinator
                    .StartAttempt(
                        document,
                        preFingerprint,
                        itemId => new NativeRecoveryObservation(
                            preFingerprint,
                            string.Equals(
                                itemId,
                                incomingItemId,
                                StringComparison.Ordinal)
                                ? 1
                                : 0,
                            0));
                EquipmentSlotTransactionCoordinator
                    .RecordPlacement(
                        journal,
                        0,
                        new NativePlacementResult(
                            NativePlacementKind.Backpack,
                            1,
                            0,
                            "fixture outgoing native destination"),
                        new NativeRecoveryObservation(
                            preFingerprint,
                            sameItem ? 2 : 1,
                            0));
                EquipmentSlotTransactionCoordinator
                    .RecordIncomingWithdrawal(
                        journal,
                        succeeded: true,
                        afterBackpackCount: 0);
                DolocAPI.ResetInventory();
                DolocAPI.SetItemCount(
                    outgoingItemId,
                    1);
                DolocAPI.archiveHandle =
                    new ColdFixtureArchive(
                        archiveIndex,
                        playerName,
                        playerName,
                        manager);
                DolocAPI.dataPersistenceManager =
                    new ColdFixtureDataPersistenceManager(
                        nativeSavePath);
                File.WriteAllText(
                    nativeSavePath,
                    "native-committed-" + state);
                string committedFingerprint =
                    (string)InvokeStaticRequired(
                        service.GetType(),
                        "GetProductNativeSaveFingerprint",
                        archiveIndex);
                if (state ==
                    ProductColdRecoveryState
                        .ReplacementCommittedTombstone)
                {
                    EquipmentSlotTransactionCoordinator
                        .PromoteCommittedTombstone(
                            document,
                            committedFingerprint);
                }
            }

            string sidecarPath =
                Path.Combine(
                    runtime.Paths.ConfigPath,
                    "protected-items",
                    "equipment-slots",
                    "slot-2",
                    "equipment-slots-DTMAPI.MoreEquipmentSlotsMod.json");
            Directory.CreateDirectory(
                Path.GetDirectoryName(sidecarPath)
                ?? throw new InvalidOperationException(
                    "Cold Host sidecar directory was unavailable."));
            document.Generation++;
            var store = new EquipmentSlotDocumentStore();
            store.WriteAtomic(sidecarPath, document);

            object hostScope =
                CreateHostScope(
                    service,
                    archiveIndex,
                    playerName);
            MethodInfo recover =
                RequireMethod(
                    service.GetType(),
                    "TryRecoverMoreEquipmentSlotsProductStorage");
            object?[] recoverArguments =
            {
                sidecarPath,
                hostScope,
                false,
                0,
                string.Empty
            };
            object? recoveryResult =
                recover.Invoke(
                    service,
                    recoverArguments);
            Assert(
                recoveryResult is bool recovered &&
                recovered &&
                recoverArguments[2] is bool handled &&
                handled,
                "The real Compatibility Host did not handle " +
                state +
                ". message=" +
                (recoverArguments[4] as string ??
                 string.Empty));
            Assert(
                CountProductColdRecoverySessions(service) == 1,
                "The real Host route must retain exactly one durable cold-recovery session until SaveSaved for " +
                state +
                ".");

            InvokeVoid(
                service,
                "NotifyEquipmentSlotsSaveSaving",
                archiveIndex);
            File.WriteAllText(
                nativeSavePath,
                "host-save-committed-" + state);
            InvokeVoid(
                service,
                "NotifyEquipmentSlotsSaveSaved",
                archiveIndex);

            EquipmentSlotStorageDocument finalDocument =
                store.Load(sidecarPath, scope);
            Assert(
                (sameItem
                    ? DolocAPI.CountItem(
                        outgoingItemId,
                        false) == 2
                    : DolocAPI.CountItem(
                            outgoingItemId,
                            false) == 1 &&
                        DolocAPI.CountItem(
                            incomingItemId,
                            false) == 1),
                "Cold replacement recovery must preserve both exact logical items in native storage for " +
                state +
                ".");
            Assert(
                finalDocument.Slots.All(
                    slot => !slot.IsOccupied) &&
                finalDocument.Journal == null,
                "Cold replacement recovery must leave all three ProductNative slots empty and clear the journal for " +
                state +
                ".");
            Assert(
                CountProductColdRecoverySessions(service) == 0,
                "SaveSaved must clear the exact ProductNative cold-recovery session for " +
                state +
                ".");
            AssertProductAssemblyIsNotLoaded();
            DolocAPI.ResetInventory();
        }

        private static void
            RunProductGameplayCandidateColdRecoveryScenario(
                bool changePreviousArchive)
        {
            const int archiveIndex = 2;
            const string playerName = "fixture-player";
            const string itemId = "gameplay-candidate-item";
            string sessionRoot =
                Environment.GetEnvironmentVariable(
                    "DTMAPI_TEST_SESSION_ROOT")
                ?? throw new InvalidOperationException(
                    "Cold Host fixtures require the managed DTMAPI test session.");
            string scenarioRoot =
                @"\\?\" +
                Path.Combine(
                    sessionRoot,
                    "ech-gameplay-" +
                    (++fixtureSequence).ToString());
            Directory.CreateDirectory(
                Path.Combine(
                    scenarioRoot,
                    "BepInEx",
                    "plugins"));
            var runtime =
                new DtmApiRuntime(
                    new FakeHost(scenarioRoot),
                    new ConfigMenuRegistry());
            object service = CreateService(runtime);
            string nativeSavePath =
                Path.Combine(
                    scenarioRoot,
                    "native-save-2.sav");
            File.WriteAllText(
                nativeSavePath,
                "identical-native-bytes");
            DolocAPI.ResetInventory();
            DolocAPI.archiveHandle =
                new ColdFixtureArchive(
                    archiveIndex,
                    playerName,
                    playerName,
                    new DolocTown.GameData
                        .AgentEquipmentManager());
            DolocAPI.dataPersistenceManager =
                new ColdFixtureDataPersistenceManager(
                    nativeSavePath);

            var scope =
                new EquipmentSlotSaveScope
                {
                    ArchiveIndex = archiveIndex,
                    PlayerName = playerName,
                    CustomPlayerName = playerName,
                    TotalGameSeconds = 1
                };
            var document =
                new EquipmentSlotStorageDocument
                {
                    Scope = scope.Clone(),
                    Generation = 7
                };
            List<EquipmentSlotStorageEntry> working =
                EquipmentSlotGameplayCandidateCoordinator
                    .CloneSlots(document.Slots);
            working[0].ItemId = itemId;
            working[0].DisplayName = itemId;
            working[0].SkillId = "passive";
            string preFingerprint =
                BuildFixtureSaveFingerprint(
                    nativeSavePath);
            EquipmentSlotGameplayCandidateCoordinator
                .Prepare(
                    document,
                    working,
                    preFingerprint,
                    _ => new NativeRecoveryObservation(
                        preFingerprint,
                        0,
                        0));

            if (changePreviousArchive)
            {
                string previousPath =
                    Path.Combine(
                        Path.GetDirectoryName(
                            nativeSavePath) ??
                            scenarioRoot,
                        Path.GetFileNameWithoutExtension(
                            nativeSavePath) +
                        "-prev" +
                        Path.GetExtension(nativeSavePath));
                File.WriteAllText(
                    previousPath,
                    "previous-archive-committed");
            }
            else
            {
                DateTime changedWriteTime =
                    File.GetLastWriteTimeUtc(
                        nativeSavePath).AddSeconds(2);
                File.SetLastWriteTimeUtc(
                    nativeSavePath,
                    changedWriteTime);
            }
            string changedFingerprint =
                (string)InvokeStaticRequired(
                    service.GetType(),
                    "GetProductNativeSaveFingerprint",
                    archiveIndex);
            Assert(
                changedFingerprint.StartsWith(
                    "native-v2|",
                    StringComparison.Ordinal) &&
                !string.Equals(
                    changedFingerprint,
                    preFingerprint,
                    StringComparison.Ordinal),
                "Cold Host must share ProductNative's native-v2 current/prev/bak commit fingerprint for " +
                (changePreviousArchive
                    ? "previous-archive change"
                    : "identical-byte rewrite") +
                ".");

            string sidecarPath =
                Path.Combine(
                    runtime.Paths.ConfigPath,
                    "protected-items",
                    "equipment-slots",
                    "slot-2",
                    "equipment-slots-DTMAPI.MoreEquipmentSlotsMod.json");
            Directory.CreateDirectory(
                Path.GetDirectoryName(sidecarPath)
                ?? throw new InvalidOperationException(
                    "Cold Host sidecar directory was unavailable."));
            new EquipmentSlotDocumentStore()
                .WriteAtomic(sidecarPath, document);

            object?[] recoverArguments =
            {
                sidecarPath,
                CreateHostScope(
                    service,
                    archiveIndex,
                    playerName),
                false,
                0,
                string.Empty
            };
            object? recoveryResult =
                RequireMethod(
                    service.GetType(),
                    "TryRecoverMoreEquipmentSlotsProductStorage")
                    .Invoke(
                        service,
                        recoverArguments);
            Assert(
                recoveryResult is bool recovered &&
                recovered &&
                recoverArguments[2] is bool handled &&
                handled &&
                recoverArguments[3] is int recoveredCount &&
                recoveredCount == 1 &&
                DolocAPI.CountItem(
                    itemId,
                    false) == 1,
                "Cold Host did not promote the proven ProductNative gameplay candidate and recover exactly one logical item for " +
                (changePreviousArchive
                    ? "previous-archive change"
                    : "identical-byte rewrite") +
                ". message=" +
                (recoverArguments[4] as string ??
                 string.Empty));

            InvokeVoid(
                service,
                "NotifyEquipmentSlotsSaveSaving",
                archiveIndex);
            File.WriteAllText(
                nativeSavePath,
                "cold-host-final-save-" +
                (changePreviousArchive
                    ? "previous"
                    : "identical"));
            InvokeVoid(
                service,
                "NotifyEquipmentSlotsSaveSaved",
                archiveIndex);

            EquipmentSlotStorageDocument finalDocument =
                new EquipmentSlotDocumentStore()
                    .Load(sidecarPath, scope);
            Assert(
                finalDocument.GameplayCandidate == null &&
                finalDocument.Journal == null &&
                finalDocument.Slots.All(
                    slot => !slot.IsOccupied) &&
                CountProductColdRecoverySessions(service) == 0 &&
                DolocAPI.CountItem(
                    itemId,
                    false) == 1,
                "Cold Host did not finalize the promoted gameplay candidate without loss or duplication.");
            AssertProductAssemblyIsNotLoaded();
            DolocAPI.ResetInventory();
        }

        private static object CreateService(
            DtmApiRuntime runtime)
        {
            Assembly hostAssembly =
                Assembly.LoadFrom(
                    Path.Combine(
                        AppContext.BaseDirectory,
                        "DTMAPI.GameBridge.DolocTown.Compatibility.dll"));
            Type serviceType =
                hostAssembly.GetType(
                    "DTMAPI.GameBridge.DolocTown.EquipmentSlotsCompatibilityService",
                    throwOnError: true)
                ?? throw new TypeLoadException(
                    "EquipmentSlotsCompatibilityService");
            ConstructorInfo constructor =
                serviceType.GetConstructors(
                        BindingFlags.Instance |
                        BindingFlags.NonPublic)
                    .Single(current =>
                        current.GetParameters().Length == 1);
            return constructor.Invoke(
                new object[] { runtime });
        }

        private static object CreateService(
            DtmApiRuntime runtime,
            Func<string, bool> storageValidationGate)
        {
            Assembly hostAssembly =
                Assembly.LoadFrom(
                    Path.Combine(
                        AppContext.BaseDirectory,
                        "DTMAPI.GameBridge.DolocTown.Compatibility.dll"));
            Type serviceType =
                hostAssembly.GetType(
                    "DTMAPI.GameBridge.DolocTown.EquipmentSlotsCompatibilityService",
                    throwOnError: true)
                ?? throw new TypeLoadException(
                    "EquipmentSlotsCompatibilityService");
            ConstructorInfo constructor =
                serviceType.GetConstructors(
                        BindingFlags.Instance |
                        BindingFlags.NonPublic)
                    .Single(current =>
                        current.GetParameters().Length == 3);
            return constructor.Invoke(
                new object?[]
                {
                    runtime,
                    null,
                    storageValidationGate
                });
        }

        private static MethodInfo RequireMethod(
            Type type,
            string name) =>
            type.GetMethod(
                name,
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance)
            ?? throw new MissingMethodException(
                type.FullName,
                name);

        private static object CreateHostScope(
            object service,
            int archiveIndex,
            string playerName)
        {
            Type scopeType =
                service.GetType().GetNestedType(
                    "EquipmentSlotSaveScope",
                    BindingFlags.NonPublic)
                ?? throw new TypeLoadException(
                    "EquipmentSlotSaveScope");
            return Activator.CreateInstance(
                scopeType,
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic,
                binder: null,
                args: new object[]
                {
                    archiveIndex,
                    "slot-" + archiveIndex,
                    playerName,
                    playerName,
                    "fixture-scene",
                    100L
                },
                culture: null)
                ?? throw new InvalidOperationException(
                    "Could not construct exact Host save scope.");
        }

        private static object InvokeRequired(
            object instance,
            string methodName,
            params object[] arguments) =>
            RequireMethod(
                instance.GetType(),
                methodName).Invoke(
                    instance,
                    arguments)
            ?? throw new InvalidOperationException(
                methodName + " returned null.");

        private static object InvokeStaticRequired(
            Type type,
            string methodName,
            params object[] arguments) =>
            type.GetMethod(
                methodName,
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Static)?.Invoke(
                    null,
                    arguments)
            ?? throw new InvalidOperationException(
                methodName + " returned null.");

        private static void InvokeVoid(
            object instance,
            string methodName,
            params object[] arguments) =>
            RequireMethod(
                instance.GetType(),
                methodName).Invoke(
                    instance,
                    arguments);

        private static int CountProductColdRecoverySessions(
            object service)
        {
            FieldInfo field =
                service.GetType().GetField(
                    "productColdRecoverySessions",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic)
                ?? throw new MissingFieldException(
                    service.GetType().FullName,
                    "productColdRecoverySessions");
            var sessions =
                field.GetValue(service) as IDictionary
                ?? throw new InvalidOperationException(
                    "Product cold-recovery session map was unavailable.");
            return sessions.Count;
        }

        private static void AssertProductAssemblyIsNotLoaded()
        {
            Assert(
                !AppDomain.CurrentDomain.GetAssemblies().Any(
                    assembly => string.Equals(
                        assembly.GetName().Name,
                        "DTMAPI.MoreEquipmentSlots",
                        StringComparison.OrdinalIgnoreCase)),
                "The cold Compatibility Host fixture loaded the ProductNative assembly.");
        }

        private static string BuildFixtureSaveFingerprint(
            string path)
        {
            return EquipmentSlotNativeCommitFingerprint
                .Compute(path);
        }

        private static void ProductFirstFailsClosed()
        {
            ResetOwners();
            MoreEquipmentSlotsNativeRuntime product =
                CreateProductRuntime();
            product.Configure(
                new MoreEquipmentSlotsConfig
                {
                    Enabled = true
                },
                "physical product-first fixture");
            AssertOwnerCount(ProductOwner, 4);
            object service = CreateService();
            EquipmentSlotsRegisterResult result =
                Register(service, "DTMAPI.Tests.EquipmentSlots.ProductFirst", enabled: true);
            Assert(
                !result.Success,
                "A real product-first four-Hook owner did not reject frozen compatibility.");
            AssertOwnerCount(ProductOwner, 4);
            AssertOwnerCount(CompatibilityOwner, 0);
            Assert(
                CountOwnerResources(service, result.OwnerId) == 0,
                "A rejected product-first registration retained Host owner state.");
            product.DeactivateOwner(
                "physical product-first fixture cleanup");
            AssertOwnerCount(ProductOwner, 0);
        }

        private static void CompatibilityFirstFailsClosed()
        {
            ResetOwners();
            object service = CreateService();
            string ownerId =
                "DTMAPI.Tests.EquipmentSlots.CompatibilityFirst";
            EquipmentSlotsRegisterResult result =
                Register(service, ownerId, enabled: true);
            Assert(
                result.Success,
                "Compatibility-first registration did not acquire all four exact targets.");
            AssertOwnerCount(CompatibilityOwner, 4);
            AssertOwnerCount(ProductOwner, 0);

            MoreEquipmentSlotsNativeRuntime product =
                CreateProductRuntime();
            bool rejected = false;
            try
            {
                product.Configure(
                    new MoreEquipmentSlotsConfig
                    {
                        Enabled = true
                    },
                    "physical compatibility-first fixture");
            }
            catch (InvalidOperationException)
            {
                rejected = true;
            }
            Assert(
                rejected,
                "The real ProductNative installer did not reject a physical compatibility-first owner.");
            AssertOwnerCount(ProductOwner, 0);
            AssertOwnerCount(CompatibilityOwner, 4);

            RemoveOwner(
                service,
                ownerId,
                "RuntimeShutdown");
            AssertOwnerCount(CompatibilityOwner, 0);
            Assert(
                CountOwnerResources(service, ownerId) == 0,
                "Compatibility-first owner cleanup retained Host resources.");
        }

        private static void ProductPartialInstallRollsBackAtomically()
        {
            ResetOwners();
            InstallOwner(UnrelatedOwner, Targets);
            MoreEquipmentSlotsNativeRuntime product =
                CreateProductRuntime(
                    ordinal => ordinal != 3);
            bool rejected = false;
            try
            {
                product.Configure(
                    new MoreEquipmentSlotsConfig
                    {
                        Enabled = true
                    },
                    "physical product partial rollback fixture");
            }
            catch (InvalidOperationException)
            {
                rejected = true;
            }
            Assert(
                rejected,
                "The injected third product Hook failure did not fail installation.");
            AssertOwnerCount(CompatibilityOwner, 0);
            AssertOwnerCount(ProductOwner, 0);
            AssertOwnerCount(UnrelatedOwner, 4);
            Cleanup(UnrelatedOwner);
            Assert(
                GetProductCallbackRuntime() == null,
                "The real product partial-install rollback retained its static callback root.");
        }

        private static void ResidualCompatibilityOwnerIsRemovedAndRefused()
        {
            ResetOwners();
            InstallOwner(
                CompatibilityOwner,
                new[] { Targets[0] });
            AssertOwnerCount(CompatibilityOwner, 1);

            MoreEquipmentSlotsNativeRuntime product =
                CreateProductRuntime();
            bool productRejected = false;
            try
            {
                product.Configure(
                    new MoreEquipmentSlotsConfig
                    {
                        Enabled = true
                    },
                    "physical residual compatibility fixture");
            }
            catch (InvalidOperationException)
            {
                productRejected = true;
            }
            Assert(
                productRejected,
                "The real ProductNative installer did not fail closed on a residual compatibility owner.");
            AssertOwnerCount(ProductOwner, 0);
            AssertOwnerCount(CompatibilityOwner, 1);

            object service = CreateService();
            EquipmentSlotsRegisterResult result =
                Register(
                    service,
                    "DTMAPI.Tests.EquipmentSlots.ResidualCompatibility",
                    enabled: true);
            Assert(
                !result.Success,
                "A residual 1/4 compatibility owner did not fail registration closed.");
            AssertOwnerCount(CompatibilityOwner, 0);
            Assert(
                CountOwnerResources(service, result.OwnerId) == 0,
                "Residual-owner reconciliation retained Host owner state.");
        }

        private static void ResidualProductOwnerFailsClosed()
        {
            ResetOwners();
            InstallOwner(
                ProductOwner,
                new[] { Targets[0] });
            MoreEquipmentSlotsNativeRuntime product =
                CreateProductRuntime();
            bool rejected = false;
            try
            {
                product.Configure(
                    new MoreEquipmentSlotsConfig
                    {
                        Enabled = true
                    },
                    "physical residual product fixture");
            }
            catch (InvalidOperationException)
            {
                rejected = true;
            }
            Assert(
                rejected,
                "The real ProductNative installer did not fail closed on a residual product owner.");
            AssertOwnerCount(ProductOwner, 1);
            AssertOwnerCount(CompatibilityOwner, 0);
            Cleanup(ProductOwner);
        }

        private static void ProductDeactivationRemovesExactOwner()
        {
            ResetOwners();
            InstallOwner(UnrelatedOwner, Targets);
            MoreEquipmentSlotsNativeRuntime product =
                CreateProductRuntime();
            product.Configure(
                new MoreEquipmentSlotsConfig
                {
                    Enabled = true
                },
                "physical product deactivation fixture");
            AssertOwnerCount(ProductOwner, 4);
            AssertOwnerCount(UnrelatedOwner, 4);
            product.DeactivateOwner(
                "physical Loader-style deactivation fixture");
            AssertOwnerCount(ProductOwner, 0);
            AssertOwnerCount(UnrelatedOwner, 4);
            Assert(
                GetProductCallbackRuntime() == null,
                "Real product deactivation retained its static callback root.");
            Cleanup(UnrelatedOwner);
        }

        private static void ProductShieldTailPreservesNativeSemantics()
        {
            ResetOwners();
            var manager =
                new DolocTown.GameData.AgentEquipmentManager();
            DolocAPI.archiveHandle =
                new FixtureArchive(manager);

            MoreEquipmentSlotsNativeRuntime faintRuntime =
                CreateProductRuntime();
            EquipmentSlotStorageDocument faintDocument =
                ConfigureProductShield(
                    faintRuntime,
                    shieldValue: 5);
            var faintBody = new DolocTown.BodyController
            {
                IsFaint = true
            };
            bool faintDead = false;
            bool faintResult = false;
            bool runOriginal = faintRuntime.HandleAttackPrefix(
                faintBody,
                10f,
                false,
                new UnityEngine.Vector2(),
                ref faintDead,
                ref faintResult);
            Assert(
                runOriginal &&
                faintDocument.Slots[0].ShieldValue == 5,
                "An already faint body must bypass ProductNative shield consumption and run the official method.");

            MoreEquipmentSlotsNativeRuntime nativeShieldRuntime =
                CreateProductRuntime();
            EquipmentSlotStorageDocument nativeShieldDocument =
                ConfigureProductShield(
                    nativeShieldRuntime,
                    shieldValue: 5);
            manager.HasNativeShield = true;
            bool nativeShieldDead = false;
            bool nativeShieldResult = false;
            Assert(
                nativeShieldRuntime.HandleAttackPrefix(
                    new DolocTown.BodyController(),
                    10f,
                    false,
                    new UnityEngine.Vector2(),
                    ref nativeShieldDead,
                    ref nativeShieldResult) &&
                nativeShieldDocument.Slots[0].ShieldValue == 5,
                "The official native shield must retain priority over the ProductNative shield.");
            manager.HasNativeShield = false;

            DolocAPI.ResetAttackEvidence();
            MoreEquipmentSlotsNativeRuntime probeFailureRuntime =
                CreateProductRuntime();
            EquipmentSlotStorageDocument probeFailureDocument =
                ConfigureProductShield(
                    probeFailureRuntime,
                    shieldValue: 5);
            manager.ThrowOnShieldProbe = true;
            bool probeFailureDead = false;
            bool probeFailureResult = false;
            Assert(
                probeFailureRuntime.HandleAttackPrefix(
                    new DolocTown.BodyController(),
                    10f,
                    false,
                    new UnityEngine.Vector2(),
                    ref probeFailureDead,
                    ref probeFailureResult) &&
                probeFailureDocument.Slots[0].ShieldValue == 5,
                "A native shield-owner probe exception must fail closed to the official attack path without consuming ProductNative charge.");
            manager.ThrowOnShieldProbe = false;

            DolocAPI.ResetAttackEvidence();
            MoreEquipmentSlotsNativeRuntime defendRuntime =
                CreateProductRuntime();
            EquipmentSlotStorageDocument defendDocument =
                ConfigureProductShield(
                    defendRuntime,
                    shieldValue: 10,
                    shieldDefend: 5);
            string defendSidecar =
                (string)(GetField(
                    defendRuntime,
                    "sidecarPath").GetValue(
                        defendRuntime)
                    ?? throw new InvalidOperationException(
                        "Defend-only sidecar path was unavailable."));
            Directory.CreateDirectory(defendSidecar);
            long defendGeneration =
                defendDocument.Generation;
            var defendBody = new DolocTown.BodyController();
            bool defendDead = false;
            bool defendResult = false;
            Assert(
                !defendRuntime.HandleAttackPrefix(
                    defendBody,
                    4f,
                    false,
                    new UnityEngine.Vector2(),
                    ref defendDead,
                    ref defendResult) &&
                defendResult &&
                defendDocument.Slots[0].ShieldValue == 10 &&
                defendDocument.Generation ==
                    defendGeneration &&
                defendBody.HatRenderer.ShineCount == 1 &&
                DolocAPI.HurtBroadcastCount == 0,
                "ShieldDefend must fully block before charge without consuming the shield.");

            DolocAPI.ResetAttackEvidence();
            MoreEquipmentSlotsNativeRuntime blockedRuntime =
                CreateProductRuntime();
            EquipmentSlotStorageDocument blockedDocument =
                ConfigureProductShield(
                    blockedRuntime,
                    shieldValue: 10);
            var blockedBody = new DolocTown.BodyController();
            bool blockedDead = false;
            bool blockedResult = false;
            Assert(
                !blockedRuntime.HandleAttackPrefix(
                    blockedBody,
                    4f,
                    false,
                    new UnityEngine.Vector2(),
                    ref blockedDead,
                    ref blockedResult) &&
                blockedResult &&
                !blockedDead &&
                blockedDocument.Slots[0].ShieldValue == 6 &&
                DolocAPI.LastHealthCost == 0 &&
                blockedBody.HatRenderer.ShineCount == 1 &&
                blockedBody.HitBackCount == 1,
                "A fully blocked product shield hit must preserve the official zero-damage tip, shine and hit-back tail.");

            DolocAPI.ResetAttackEvidence();
            MoreEquipmentSlotsNativeRuntime defenseOnceRuntime =
                CreateProductRuntime();
            ConfigureProductShield(
                defenseOnceRuntime,
                shieldValue: 3,
                defenseBonus: 3);
            var defenseOnceBody =
                new DolocTown.BodyController
                {
                    CurrentDefend = 2f
                };
            bool defenseOnceDead = false;
            bool defenseOnceResult = false;
            Assert(
                !defenseOnceRuntime.HandleAttackPrefix(
                    defenseOnceBody,
                    10f,
                    false,
                    new UnityEngine.Vector2(),
                    ref defenseOnceDead,
                    ref defenseOnceResult) &&
                DolocAPI.LastHealthCost == 5,
                "Hat DefenseBonus is already represented by native CurrentDefend and must not be subtracted a second time by the shield policy.");

            DolocAPI.ResetAttackEvidence();
            manager.BaseDefense = 2;
            MoreEquipmentSlotsNativeRuntime equalRuntime =
                CreateProductRuntime();
            EquipmentSlotStorageDocument equalDocument =
                ConfigureProductShield(
                    equalRuntime,
                    shieldValue: 3,
                    defenseBonus: 7);
            equalDocument.Slots[1].ItemId =
                "fixture-defense-hat";
            equalDocument.Slots[1].DisplayName =
                "fixture-defense-hat";
            equalDocument.Slots[1].DefenseBonus = 4;
            var brokenShieldItem = new object();
            var brokenShieldFunction =
                new FixtureDisposable();
            manager.functions[brokenShieldItem] =
                brokenShieldFunction;
            AddProductFunctionLease(
                equalRuntime,
                manager.functions,
                brokenShieldItem,
                brokenShieldFunction);
            var equalBody = new DolocTown.BodyController();
            bool equalDead = false;
            bool equalResult = false;
            int reloadBefore = manager.ReloadCount;
            Assert(
                !equalRuntime.HandleAttackPrefix(
                    equalBody,
                    3f,
                    false,
                    new UnityEngine.Vector2(),
                    ref equalDead,
                    ref equalResult) &&
                equalResult &&
                !equalDocument.Slots[0].IsOccupied &&
                DolocAPI.LastHealthCost == 0 &&
                DolocAPI.HurtBroadcastCount == 1 &&
                equalBody.HatRenderer.ShineCount == 0 &&
                equalBody.StateManager.LastOverwriteType ==
                    typeof(DolocTown.AgentStateHit) &&
                manager.ReloadCount == reloadBefore + 1 &&
                manager.EquipmentAbility.defence == 6 &&
                manager.functions.Count == 0 &&
                brokenShieldFunction.DisposeCount == 1,
                "Equal shield charge must break, run the zero-health Hurt/Hit tail, remove that hat defense and retain other ProductNative defense after ReloadParams.");
            manager.BaseDefense = 0;

            DolocAPI.ResetAttackEvidence();
            MoreEquipmentSlotsNativeRuntime defendedBreakRuntime =
                CreateProductRuntime();
            ConfigureProductShield(
                defendedBreakRuntime,
                shieldValue: 3,
                shieldDefend: 2);
            bool defendedBreakDead = false;
            bool defendedBreakResult = false;
            Assert(
                !defendedBreakRuntime.HandleAttackPrefix(
                    new DolocTown.BodyController(),
                    10f,
                    false,
                    new UnityEngine.Vector2(),
                    ref defendedBreakDead,
                    ref defendedBreakResult) &&
                DolocAPI.LastHealthCost == 7,
                "A broken shield with ShieldDefend must leave nativeDamage minus charge as residual damage; ShieldDefend is not subtracted from the health tail.");

            DolocAPI.ResetAttackEvidence();
            MoreEquipmentSlotsNativeRuntime persistFailureRuntime =
                CreateProductRuntime();
            EquipmentSlotStorageDocument persistFailureDocument =
                ConfigureProductShield(
                    persistFailureRuntime,
                    shieldValue: 5,
                    shieldDefend: 1,
                    defenseBonus: 2);
            string failingSidecar =
                (string)(GetField(
                    persistFailureRuntime,
                    "sidecarPath").GetValue(
                        persistFailureRuntime)
                    ?? throw new InvalidOperationException(
                        "Shield sidecar fixture path was unavailable."));
            Directory.CreateDirectory(failingSidecar);
            bool persistFailureDead = false;
            bool persistFailureResult = false;
            long shieldGenerationBefore =
                persistFailureDocument.Generation;
            Assert(
                !persistFailureRuntime.HandleAttackPrefix(
                    new DolocTown.BodyController(),
                    10f,
                    false,
                    new UnityEngine.Vector2(),
                    ref persistFailureDead,
                    ref persistFailureResult) &&
                persistFailureResult &&
                !persistFailureDocument.Slots[0].IsOccupied &&
                persistFailureDocument.Generation ==
                    shieldGenerationBefore &&
                (bool)(GetField(
                    persistFailureRuntime,
                    "workingDirty").GetValue(
                        persistFailureRuntime)
                    ?? false) &&
                DolocAPI.DamageTipCount == 1 &&
                DolocAPI.HurtBroadcastCount == 1,
                "Shield damage and break must update only in-memory Working state during the day; a deliberately unwritable sidecar path must not be touched per hit.");

            DolocAPI.ResetAttackEvidence();
            MoreEquipmentSlotsNativeRuntime rehydrateRuntime =
                CreateProductRuntime();
            EquipmentSlotStorageDocument rehydrateDocument =
                ConfigureSeparatedProductShield(
                    rehydrateRuntime,
                    shieldValue: 5,
                    shieldDefend: 2,
                    defenseBonus: 3);
            rehydrateDocument.Slots[0].ShieldMaxValue = 10;
            var rehydrateWorking =
                (List<EquipmentSlotStorageEntry>)(
                    GetField(
                        rehydrateRuntime,
                        "workingSlots").GetValue(
                            rehydrateRuntime)
                    ?? throw new InvalidOperationException(
                        "The rehydrate Working projection was unavailable."));
            rehydrateWorking[0].ShieldMaxValue = 10;
            DolocTown.ItemFactory.ThrowOnGenerate = true;
            Invoke(
                rehydrateRuntime,
                "RehydrateWorkingTraits",
                "fixture transient failure");
            bool inertDead = false;
            bool inertResult = false;
            Assert(
                rehydrateRuntime.HandleAttackPrefix(
                    new DolocTown.BodyController(),
                    10f,
                    false,
                    new UnityEngine.Vector2(),
                    ref inertDead,
                    ref inertResult) &&
                rehydrateWorking[0].IsShield &&
                rehydrateWorking[0].ShieldValue == 5 &&
                rehydrateWorking[0].ShieldMaxValue == 10 &&
                rehydrateWorking[0].ShieldDefend == 2 &&
                rehydrateWorking[0].DefenseBonus == 3 &&
                rehydrateDocument.Slots[0].DefenseBonus == 3,
                "A transient native trait lookup failure must leave durable partial charge unchanged while the slot effect is inert.");
            DolocTown.ItemFactory.ThrowOnGenerate = false;
            Invoke(
                rehydrateRuntime,
                "RehydrateWorkingTraits",
                "fixture retry success");
            Assert(
                rehydrateWorking[0].IsShield &&
                rehydrateWorking[0].ShieldValue == 5 &&
                rehydrateWorking[0].ShieldMaxValue == 10 &&
                rehydrateWorking[0].ShieldDefend == 2 &&
                rehydrateWorking[0].DefenseBonus == 7 &&
                rehydrateDocument.Slots[0].DefenseBonus == 3,
                "A later successful native trait lookup must refresh only Working traits while preserving partial shield charge and the committed document.");

            DolocTown.ItemFactory.ShieldMaxValue = 0;
            bool malformedShieldRejected = false;
            try
            {
                rehydrateRuntime.EquipFromBackpack(
                    "fixture-shield",
                    1);
            }
            catch (InvalidOperationException)
            {
                malformedShieldRejected = true;
            }
            finally
            {
                DolocTown.ItemFactory.ShieldMaxValue = 10;
            }
            Assert(
                malformedShieldRejected &&
                rehydrateDocument.Journal == null &&
                !rehydrateDocument.Slots[1].IsOccupied,
                "A malformed native shield with no positive capacity must be rejected before a journal or slot mutation is staged.");

            DolocAPI.ResetAttackEvidence();
            MoreEquipmentSlotsNativeRuntime nonFatalRuntime =
                CreateProductRuntime();
            ConfigureProductShield(
                nonFatalRuntime,
                shieldValue: 3);
            var nonFatalBody = new DolocTown.BodyController();
            nonFatalBody.StateManager.current =
                new DolocTown.AgentStateFishing();
            bool nonFatalDead = false;
            bool nonFatalResult = false;
            Assert(
                !nonFatalRuntime.HandleAttackPrefix(
                    nonFatalBody,
                    10f,
                    false,
                    new UnityEngine.Vector2(),
                    ref nonFatalDead,
                    ref nonFatalResult) &&
                nonFatalResult &&
                !nonFatalDead &&
                DolocAPI.LastHealthCost == 7 &&
                DolocAPI.LastHurtReason ==
                    DolocTown.HurtReason.MonsterAttack &&
                DolocAPI.HurtBroadcastCount == 1 &&
                DolocTown.AgentStateFishing
                    .UnsetUiControlCount == 1 &&
                !nonFatalBody.fishRodRenderer.Visible &&
                nonFatalBody.StateManager.LastOverwriteType ==
                    typeof(DolocTown.AgentStateHit) &&
                nonFatalBody.StateManager.LastShouldQuit &&
                nonFatalBody.HitBackCount == 1,
                "A non-fatal residual hit must use MonsterAttack, clear fishing UI/rod state and enter AgentStateHit.");

            DolocAPI.ResetAttackEvidence();
            DolocAPI.CostHealthReturnsDead = true;
            MoreEquipmentSlotsNativeRuntime fatalRuntime =
                CreateProductRuntime();
            ConfigureProductShield(
                fatalRuntime,
                shieldValue: 3);
            var fatalBody = new DolocTown.BodyController();
            fatalBody.StateManager.current =
                new DolocTown.AgentStateFishing();
            bool fatalDead = false;
            bool fatalResult = false;
            Assert(
                !fatalRuntime.HandleAttackPrefix(
                    fatalBody,
                    10f,
                    false,
                    new UnityEngine.Vector2(),
                    ref fatalDead,
                    ref fatalResult) &&
                fatalResult &&
                fatalDead &&
                DolocAPI.LastHurtReason ==
                    DolocTown.HurtReason.MonsterAttack &&
                DolocTown.AgentStateFishing
                    .UnsetUiControlCount == 0 &&
                fatalBody.StateManager.LastOverwriteType == null &&
                fatalBody.HitBackCount == 1,
                "A fatal residual hit must keep the official death branch and skip the non-fatal state transition.");
            DolocAPI.CostHealthReturnsDead = false;
            DolocAPI.archiveHandle = null;
        }

        private static void
            ProductDefenseOnlyCleanupReloadsWithoutFunctionLease()
        {
            ResetOwners();
            var manager =
                new DolocTown.GameData.AgentEquipmentManager
                {
                    BaseDefense = 2
                };
            DolocAPI.archiveHandle =
                new FixtureArchive(manager);
            MoreEquipmentSlotsNativeRuntime product =
                CreateProductRuntime();
            EquipmentSlotStorageDocument document =
                ConfigureProductShield(
                    product,
                    shieldValue: 0);
            document.Slots[0].IsShield = false;
            document.Slots[0].DefenseBonus = 7;

            manager.EquipmentAbility =
                new DolocTown.GameData.EquipmentAbilityData(
                    9,
                    0f,
                    false,
                    0f,
                    0f,
                    0,
                    Array.Empty<string>(),
                    0f);
            product.Configure(
                new MoreEquipmentSlotsConfig
                {
                    Enabled = false
                },
                "physical defense-only cleanup fixture");
            Assert(
                manager.ReloadCount == 1 &&
                manager.EquipmentAbility.defence == 2,
                "Defense-only cleanup must recompute native aggregates even when no skill function lease exists.");
            DolocAPI.archiveHandle = null;
        }

        private static void
            AmbiguousJournalCannotPromoteOnNextSave()
        {
            MoreEquipmentSlotsNativeRuntime runtime =
                CreateProductRuntime();
            EquipmentSlotStorageDocument document =
                ConfigureProductShield(
                    runtime,
                    shieldValue: 5);
            EquipmentSlotTransactionJournal journal =
                EquipmentSlotTransactionCoordinator
                    .PrepareRecovery(
                        document,
                        new[] { 0 });
            EquipmentSlotTransactionCoordinator.StartAttempt(
                document,
                "fixture-preimage",
                _ => new NativeRecoveryObservation(
                    "fixture-preimage",
                    0,
                    0));
            EquipmentSlotTransactionCoordinator.RecordPlacement(
                journal,
                0,
                new NativePlacementResult(
                    NativePlacementKind.Backpack,
                    1,
                    0,
                    "fixture recorded placement"),
                new NativeRecoveryObservation(
                    "fixture-preimage",
                    1,
                    0));
            GetField(runtime, "archiveIndex").SetValue(
                runtime,
                2);
            DolocAPI.ResetInventory();
            DolocAPI.SetItemCount(
                "fixture-shield",
                5);
            long generation = document.Generation;
            Invoke(
                runtime,
                "RecoverDurableJournal",
                "fixture ambiguous restart");
            Assert(
                (bool)(GetField(
                    runtime,
                    "journalRecoveryBlocked").GetValue(
                        runtime)
                    ?? false),
                "Ambiguous durable journal evidence must quarantine the journal for the session.");

            bool saveRejected = false;
            try
            {
                runtime.OnSaveSaving(2);
            }
            catch (InvalidOperationException)
            {
                saveRejected = true;
            }
            Assert(
                saveRejected &&
                ReferenceEquals(
                    document.Journal,
                    journal) &&
                document.Journal.State ==
                    EquipmentSlotJournalState.Prepared &&
                document.Generation == generation &&
                !(bool)(GetField(
                    runtime,
                    "pendingNativeSave").GetValue(
                        runtime)
                    ?? true),
                "A later normal save must not replay, promote or finalize a quarantined ambiguous journal.");
            DolocAPI.ResetInventory();
        }

        private static void
            ProductFailClosedSaveSavingCancelsCoreBoundary()
        {
            RunProductCandidateFailClosedThroughCore();
            RunProductJournalFailClosedThroughCore();
        }

        private static void
            RunProductCandidateFailClosedThroughCore()
        {
            DolocAPI.ResetInventory();
            MoreEquipmentSlotsNativeRuntime product =
                CreateProductRuntime();
            EquipmentSlotStorageDocument document =
                ConfigureSeparatedProductShield(
                    product,
                    shieldValue: 5);
            GetField(product, "archiveIndex").SetValue(
                product,
                2);
            EquipmentSlotGameplayCandidate candidate =
                EquipmentSlotGameplayCandidateCoordinator
                    .Prepare(
                        document,
                        EquipmentSlotGameplayCandidateCoordinator
                            .CloneSlots(document.Slots),
                        "fixture-candidate-preimage",
                        _ => new NativeRecoveryObservation(
                            "fixture-candidate-preimage",
                            0,
                            0));
            string sidecarPath =
                (string)(GetField(
                    product,
                    "sidecarPath").GetValue(
                        product)
                    ?? throw new InvalidOperationException(
                        "Candidate fail-closed sidecar path was unavailable."));
            new EquipmentSlotDocumentStore()
                .WriteAtomic(
                    sidecarPath,
                    document);
            byte[] authority =
                File.ReadAllBytes(sidecarPath);
            long generation =
                document.Generation;
            candidate.Origin =
                EquipmentSlotTransactionOrigin.Unknown;

            DtmApiRuntime core =
                CreateFixtureCoreRuntime(
                    "product-candidate-save-fail");
            IEventsHelper events =
                CreateEventsProxy(
                    core,
                    "fixture.product.candidate-save-fail");
            events.Save.SaveSaving +=
                (_, e) => product.OnSaveSaving(e.SaveSlot);
            core.Start();
            Assert(
                !core.TryNotifySaveSaving(2) &&
                ReferenceEquals(
                    document.GameplayCandidate,
                    candidate) &&
                document.Generation == generation &&
                document.Journal == null &&
                EqualBytes(
                    authority,
                    File.ReadAllBytes(sidecarPath)),
                "A fail-closed Product gameplay candidate did not veto Core TryNotifySaveSaving or rewrote sidecar authority.");
            product.DeactivateOwner("RuntimeShutdown");
            DolocAPI.ResetInventory();
        }

        private static void
            RunProductJournalFailClosedThroughCore()
        {
            DolocAPI.ResetInventory();
            MoreEquipmentSlotsNativeRuntime product =
                CreateProductRuntime();
            EquipmentSlotStorageDocument document =
                ConfigureSeparatedProductShield(
                    product,
                    shieldValue: 5);
            EquipmentSlotTransactionJournal journal =
                EquipmentSlotTransactionCoordinator
                    .PrepareRecovery(
                        document,
                        new[] { 0 });
            EquipmentSlotTransactionCoordinator.StartAttempt(
                document,
                "fixture-journal-preimage",
                _ => new NativeRecoveryObservation(
                    "fixture-journal-preimage",
                    0,
                    0));
            EquipmentSlotTransactionCoordinator.RecordPlacement(
                journal,
                0,
                new NativePlacementResult(
                    NativePlacementKind.Backpack,
                    1,
                    0,
                    "fixture journal placement"),
                new NativeRecoveryObservation(
                    "fixture-journal-preimage",
                    1,
                    0));
            GetField(product, "archiveIndex").SetValue(
                product,
                2);
            DolocAPI.SetItemCount(
                "fixture-shield",
                5);
            string sidecarPath =
                (string)(GetField(
                    product,
                    "sidecarPath").GetValue(
                        product)
                    ?? throw new InvalidOperationException(
                        "Journal fail-closed sidecar path was unavailable."));
            new EquipmentSlotDocumentStore()
                .WriteAtomic(
                    sidecarPath,
                    document);
            byte[] authority =
                File.ReadAllBytes(sidecarPath);
            long generation =
                document.Generation;
            Invoke(
                product,
                "RecoverDurableJournal",
                "fixture Core SaveSaving veto");
            Assert(
                (bool)(GetField(
                    product,
                    "journalRecoveryBlocked").GetValue(
                        product) ?? false),
                "The Product journal fixture did not enter fail-closed quarantine.");

            DtmApiRuntime core =
                CreateFixtureCoreRuntime(
                    "product-journal-save-fail");
            IEventsHelper events =
                CreateEventsProxy(
                    core,
                    "fixture.product.journal-save-fail");
            events.Save.SaveSaving +=
                (_, e) => product.OnSaveSaving(e.SaveSlot);
            core.Start();
            Assert(
                !core.TryNotifySaveSaving(2) &&
                ReferenceEquals(
                    document.Journal,
                    journal) &&
                document.Generation == generation &&
                document.GameplayCandidate == null &&
                EqualBytes(
                    authority,
                    File.ReadAllBytes(sidecarPath)),
                "A quarantined Product recovery journal did not veto Core TryNotifySaveSaving or rewrote sidecar authority.");
            product.DeactivateOwner("RuntimeShutdown");
            DolocAPI.ResetInventory();
        }

        private static void
            SameItemRuntimeWithdrawalEvidenceIsExact()
        {
            MoreEquipmentSlotsNativeRuntime runtime =
                CreateProductRuntime();
            EquipmentSlotStorageDocument document =
                ConfigureProductShield(
                    runtime,
                    shieldValue: 5);
            GetField(runtime, "archiveIndex").SetValue(
                runtime,
                2);
            DolocAPI.ResetInventory();
            DolocAPI.SetItemCount(
                "fixture-shield",
                1);
            Assert(
                runtime.EquipFromBackpack(
                    "fixture-shield",
                    0),
                "The same-item physical replacement fixture could not stage its replacement.");

            runtime.OnSaveSaving(2);
            EquipmentSlotGameplayCandidate candidate =
                document.GameplayCandidate ??
                throw new InvalidOperationException(
                    "The same-item gameplay candidate disappeared before native save.");
            Assert(
                candidate.Origin ==
                    EquipmentSlotTransactionOrigin
                        .GameplayMutation &&
                candidate.State ==
                    EquipmentSlotGameplayCandidateState.Prepared &&
                candidate.WorkingSlots[0].ItemId ==
                    "fixture-shield" &&
                candidate.NativeExpectations.Count == 1 &&
                candidate.NativeExpectations[0]
                    .BackpackCount == 1 &&
                DolocAPI.CountItem(
                    "fixture-shield",
                    false) == 1,
                "Production Working mutation plus SaveSaving candidate must aggregate same-ID outgoing placement and incoming withdrawal into one exact net-zero native expectation.");
            DolocAPI.ResetInventory();
        }

        private static void
            ProductRuntimeShutdownDiscardsUnsavedWorkingState()
        {
            RunProductRuntimeShutdownScenario(
                equipWorkingItem: true);
            RunProductRuntimeShutdownScenario(
                equipWorkingItem: false);
        }

        private static void RunProductRuntimeShutdownScenario(
            bool equipWorkingItem)
        {
            ResetOwners();
            DolocAPI.ResetInventory();
            var manager =
                new DolocTown.GameData.AgentEquipmentManager();
            DolocAPI.archiveHandle =
                new FixtureArchive(manager);
            MoreEquipmentSlotsNativeRuntime product =
                CreateProductRuntime();
            product.Configure(
                new MoreEquipmentSlotsConfig
                {
                    Enabled = true
                },
                "RuntimeShutdown Working fixture");
            EquipmentSlotStorageDocument document =
                ConfigureSeparatedProductShield(
                    product,
                    shieldValue: 5);
            if (equipWorkingItem)
            {
                document.Slots[0].Clear();
                GetField(product, "workingSlots").SetValue(
                    product,
                    EquipmentSlotGameplayCandidateCoordinator
                        .CloneSlots(document.Slots));
                DolocAPI.SetItemCount(
                    "fixture-shield",
                    1);
            }

            string sidecarPath =
                (string)(GetField(
                    product,
                    "sidecarPath").GetValue(
                        product)
                    ?? throw new InvalidOperationException(
                        "Product RuntimeShutdown sidecar path was unavailable."));
            new EquipmentSlotDocumentStore()
                .WriteAtomic(
                    sidecarPath,
                    document);
            byte[] committedBytes =
                File.ReadAllBytes(sidecarPath);
            long committedGeneration =
                document.Generation;
            bool mutated = equipWorkingItem
                ? product.EquipFromBackpack(
                    "fixture-shield",
                    0)
                : product.RequestUnequip(0);
            Assert(
                mutated &&
                (bool)(GetField(
                    product,
                    "workingDirty").GetValue(
                        product) ?? false) &&
                EqualBytes(
                    committedBytes,
                    File.ReadAllBytes(sidecarPath)),
                "Product RuntimeShutdown fixture could not create a Working-only " +
                (equipWorkingItem ? "equip" : "unequip") +
                " mutation.");

            product.DeactivateOwner("RuntimeShutdown");
            MoreEquipmentSlotsDiagnosticsSnapshot snapshot =
                product.GetDiagnosticsSnapshot();
            Assert(
                EqualBytes(
                    committedBytes,
                    File.ReadAllBytes(sidecarPath)) &&
                document.Generation ==
                    committedGeneration &&
                document.GameplayCandidate == null &&
                document.Journal == null &&
                snapshot.PatchCount == 0 &&
                snapshot.CloneCount == 0 &&
                snapshot.ListenerCount == 0 &&
                snapshot.FunctionCount == 0 &&
                snapshot.CallbackCount == 0 &&
                snapshot.RootCount == 0 &&
                GetProductCallbackRuntime() == null,
                "Product RuntimeShutdown persisted unsaved Working state or retained roots/hooks for " +
                (equipWorkingItem ? "equip" : "unequip") +
                ".");
            AssertOwnerCount(
                ProductOwner,
                0);
            DolocAPI.ResetInventory();
            DolocAPI.archiveHandle = null;
        }

        private static void
            ProductCleanDisableRestartRetainsOnlyTypedRecovery()
        {
            ResetOwners();
            DolocAPI.ResetInventory();
            string scenarioRoot =
                CreateFixtureScenarioRoot(
                    "product-clean-disable-restart");
            string nativeSavePath =
                Path.Combine(
                    scenarioRoot,
                    "native-save-2.sav");
            File.WriteAllText(
                nativeSavePath,
                "clean-disable-native-preimage");
            var manager =
                new DolocTown.GameData.AgentEquipmentManager();
            DolocAPI.archiveHandle =
                new ColdFixtureArchive(
                    2,
                    "fixture-player",
                    "fixture-player",
                    manager);
            DolocAPI.dataPersistenceManager =
                new ColdFixtureDataPersistenceManager(
                    nativeSavePath);

            MoreEquipmentSlotsNativeRuntime product =
                CreateProductRuntime();
            product.Configure(
                new MoreEquipmentSlotsConfig
                {
                    Enabled = true
                },
                "clean disable/restart fixture");
            EquipmentSlotStorageDocument committed =
                ConfigureSeparatedProductShield(
                    product,
                    shieldValue: 5);
            GetField(product, "archiveIndex").SetValue(
                product,
                2);
            string sidecarPath =
                (string)(GetField(
                    product,
                    "sidecarPath").GetValue(product)
                    ?? throw new InvalidOperationException(
                        "Clean disable/restart sidecar path was unavailable."));
            new EquipmentSlotDocumentStore()
                .WriteAtomic(
                    sidecarPath,
                    committed);

            product.Configure(
                new MoreEquipmentSlotsConfig
                {
                    Enabled = false
                },
                "clean committed disable");
            AssertOwnerCount(
                ProductOwner,
                0);
            EquipmentSlotStorageDocument staged =
                (EquipmentSlotStorageDocument)(
                    GetField(
                        product,
                        "document").GetValue(product)
                    ?? throw new InvalidOperationException(
                        "Typed OwnerRecovery document was unavailable after clean disable."));
            Assert(
                staged.Journal?.Origin ==
                    EquipmentSlotTransactionOrigin.OwnerRecovery &&
                staged.Journal.Escrow.Count == 1 &&
                staged.Journal.Escrow[0].ItemId ==
                    "fixture-shield" &&
                !staged.Slots.Any(slot =>
                    slot.IsOccupied) &&
                DolocAPI.CountItem(
                    "fixture-shield",
                    false) == 0,
                "Clean disable did not replace committed A with exactly one typed OwnerRecovery escrow.");

            product.Configure(
                new MoreEquipmentSlotsConfig
                {
                    Enabled = true
                },
                "same-process restart");
            AssertOwnerCount(
                ProductOwner,
                4);
            var restartedWorking =
                (List<EquipmentSlotStorageEntry>)(
                    GetField(
                        product,
                        "workingSlots").GetValue(product)
                    ?? throw new InvalidOperationException(
                        "Restarted Working projection was unavailable."));
            MoreEquipmentSlotsDiagnosticsSnapshot restarted =
                product.GetDiagnosticsSnapshot();
            Assert(
                !restartedWorking.Any(slot =>
                    slot.IsOccupied) &&
                staged.Journal?.Escrow.Count == 1 &&
                staged.Journal.Escrow[0].ItemId ==
                    "fixture-shield" &&
                restarted.FunctionCount == 0 &&
                manager.functions.Count == 0 &&
                DolocAPI.CountItem(
                    "fixture-shield",
                    false) == 0,
                "Same-process restart resurrected the old Working slot/effect or duplicated typed recovery authority.");

            product.OnSaveSaving(2);
            Assert(
                staged.Journal != null &&
                staged.Journal.Escrow.Count == 1 &&
                DolocAPI.CountItem(
                    "fixture-shield",
                    false) == 1,
                "OwnerRecovery SaveSaving did not place exactly one A while retaining one journal authority.");
            File.WriteAllText(
                nativeSavePath,
                "clean-disable-native-committed");
            product.OnSaveSaved(2);
            Assert(
                staged.Journal == null &&
                !staged.Slots.Any(slot =>
                    slot.IsOccupied) &&
                DolocAPI.CountItem(
                    "fixture-shield",
                    false) == 1 &&
                !((List<EquipmentSlotStorageEntry>)(
                    GetField(
                        product,
                        "workingSlots").GetValue(product)
                    ?? throw new InvalidOperationException(
                        "Final Working projection was unavailable.")))
                    .Any(slot => slot.IsOccupied),
                "SaveSaving/SaveSaved did not finalize exactly one restored A without reviving product slot ownership.");

            product.DeactivateOwner(
                "RuntimeShutdown");
            AssertOwnerCount(
                ProductOwner,
                0);
            DolocAPI.ResetInventory();
        }

        private static void
            ProductConfigWriteFailureLeavesCommittedStateUntouched()
        {
            ResetOwners();
            DolocAPI.ResetInventory();
            var manager =
                new DolocTown.GameData.AgentEquipmentManager();
            DolocAPI.archiveHandle =
                new FixtureArchive(manager);
            string scenarioRoot =
                CreateFixtureScenarioRoot(
                    "product-config-write-fault");
            string configPath =
                Path.Combine(
                    scenarioRoot,
                    "config",
                    "DTMAPI.MoreEquipmentSlotsMod.json");
            var helper =
                new ProductModHelper(
                    Manifest(
                        "DTMAPI.MoreEquipmentSlotsMod"),
                    configPath);
            var entry = new ModEntry();
            try
            {
                entry.Entry(helper);
                MoreEquipmentSlotsNativeRuntime runtime =
                    (MoreEquipmentSlotsNativeRuntime)(
                        GetField(
                            entry,
                            "runtime").GetValue(
                                entry)
                        ?? throw new InvalidOperationException(
                            "ModEntry runtime was unavailable."));
                runtime.OnSaveLoaded(2);
                EquipmentSlotStorageDocument document =
                    (EquipmentSlotStorageDocument)(
                        GetField(
                            runtime,
                            "document").GetValue(
                                runtime)
                        ?? throw new InvalidOperationException(
                            "ModEntry committed document was unavailable."));
                document.Slots[0] =
                    new EquipmentSlotStorageEntry
                    {
                        Index = 0,
                        ItemId = "fixture-shield",
                        DisplayName = "Fixture shield",
                        SkillId = "shield",
                        IsShield = true,
                        ShieldValue = 5,
                        ShieldMaxValue = 5,
                        ShieldDefend = 1
                    };
                GetField(
                    runtime,
                    "workingSlots").SetValue(
                        runtime,
                        EquipmentSlotGameplayCandidateCoordinator
                            .CloneSlots(document.Slots));
                string sidecarPath =
                    (string)(GetField(
                        runtime,
                        "sidecarPath").GetValue(
                            runtime)
                        ?? throw new InvalidOperationException(
                            "ModEntry sidecar path was unavailable."));
                new EquipmentSlotDocumentStore()
                    .WriteAtomic(
                        sidecarPath,
                        document);
                byte[] sidecarBefore =
                    File.ReadAllBytes(sidecarPath);
                byte[] configBefore =
                    File.ReadAllBytes(configPath);
                long generationBefore =
                    document.Generation;

                MoreEquipmentSlotsConfig requested =
                    (MoreEquipmentSlotsConfig)(
                        GetField(
                            entry,
                            "config").GetValue(
                                entry)
                        ?? throw new InvalidOperationException(
                            "ModEntry requested config was unavailable."));
                requested.Enabled = false;
                helper.ConfigStore.FailNextWrite = true;
                bool rejected = false;
                try
                {
                    Invoke(
                        entry,
                        "SaveConfig");
                }
                catch (IOException)
                {
                    rejected = true;
                }

                EquipmentSlotStorageDocument after =
                    (EquipmentSlotStorageDocument)(
                        GetField(
                            runtime,
                            "document").GetValue(
                                runtime)
                        ?? throw new InvalidOperationException(
                            "ModEntry committed document disappeared."));
                MoreEquipmentSlotsConfig applied =
                    (MoreEquipmentSlotsConfig)(
                        GetField(
                            entry,
                            "appliedConfig").GetValue(
                                entry)
                        ?? throw new InvalidOperationException(
                            "ModEntry applied config disappeared."));
                MoreEquipmentSlotsConfig visible =
                    (MoreEquipmentSlotsConfig)(
                        GetField(
                            entry,
                            "config").GetValue(
                                entry)
                        ?? throw new InvalidOperationException(
                            "ModEntry visible config disappeared."));
                Assert(
                    rejected &&
                    EqualBytes(
                        sidecarBefore,
                        File.ReadAllBytes(sidecarPath)) &&
                    EqualBytes(
                        configBefore,
                        File.ReadAllBytes(configPath)) &&
                    after.Generation == generationBefore &&
                    after.Journal == null &&
                    after.GameplayCandidate == null &&
                    after.Slots[0].ItemId ==
                        "fixture-shield" &&
                    ((List<EquipmentSlotStorageEntry>)(
                        GetField(
                            runtime,
                            "workingSlots").GetValue(
                                runtime)
                        ?? throw new InvalidOperationException(
                            "ModEntry Working slots disappeared.")))[0]
                        .ItemId == "fixture-shield" &&
                    applied.Enabled &&
                    visible.Enabled &&
                    helper.ConfigStore.Persisted.Enabled,
                    "A failed config-file write mutated Product Working/Committed authority, created a journal, or changed the enabled config.");
                AssertOwnerCount(
                    ProductOwner,
                    4);
            }
            finally
            {
                entry.DtmApiDeactivateOwner(
                    "RuntimeShutdown");
                DolocAPI.ResetInventory();
            }
        }

        private static void
            ProductDirtyConfigurationDisableDefersAndRetries()
        {
            ResetOwners();
            var manager =
                new DolocTown.GameData.AgentEquipmentManager();
            DolocAPI.ResetInventory();
            DolocAPI.archiveHandle =
                new FixtureArchive(manager);
            MoreEquipmentSlotsNativeRuntime product =
                CreateProductRuntime();
            product.Configure(
                new MoreEquipmentSlotsConfig
                {
                    Enabled = true
                },
                "dirty configuration fixture");
            EquipmentSlotStorageDocument document =
                ConfigureSeparatedProductShield(
                    product,
                    shieldValue: 5);
            string sidecarPath =
                (string)(GetField(
                    product,
                    "sidecarPath").GetValue(product)
                    ?? throw new InvalidOperationException(
                        "Dirty configuration sidecar path was unavailable."));
            new EquipmentSlotDocumentStore()
                .WriteAtomic(
                    sidecarPath,
                    document);
            byte[] committed =
                File.ReadAllBytes(sidecarPath);
            Assert(
                product.RequestUnequip(0),
                "Dirty configuration fixture could not create Working-only unequip.");

            bool disableDeferred = false;
            try
            {
                product.Configure(
                    new MoreEquipmentSlotsConfig
                    {
                        Enabled = false
                    },
                    "dirty configuration disable");
            }
            catch (InvalidOperationException)
            {
                disableDeferred = true;
            }
            MoreEquipmentSlotsConfig retainedConfig =
                (MoreEquipmentSlotsConfig)(
                    GetField(
                        product,
                        "config").GetValue(product)
                    ?? throw new InvalidOperationException(
                        "Product applied config was unavailable."));
            Assert(
                disableDeferred &&
                retainedConfig.Enabled &&
                EqualBytes(
                    committed,
                    File.ReadAllBytes(sidecarPath)) &&
                GetProductCallbackRuntime() == product,
                "Dirty Product config disable did not retain the enabled owner and byte-identical committed sidecar.");
            AssertOwnerCount(
                ProductOwner,
                4);

            product.ReturnedToTitle();
            product.Configure(
                new MoreEquipmentSlotsConfig
                {
                    Enabled = false
                },
                "clean configuration retry");
            MoreEquipmentSlotsConfig disabledConfig =
                (MoreEquipmentSlotsConfig)(
                    GetField(
                        product,
                        "config").GetValue(product)
                    ?? throw new InvalidOperationException(
                        "Product disabled config was unavailable."));
            Assert(
                !disabledConfig.Enabled &&
                EqualBytes(
                    committed,
                    File.ReadAllBytes(sidecarPath)) &&
                GetProductCallbackRuntime() == null,
                "Product config disable retry after title discard did not clear hooks while retaining committed authority.");
            AssertOwnerCount(
                ProductOwner,
                0);
            DolocAPI.ResetInventory();
        }

        private static void
            ProductDirtyDeactivationDefersUntilCommitOrDiscard()
        {
            var manager =
                new DolocTown.GameData.AgentEquipmentManager();

            DolocAPI.ResetInventory();
            DolocAPI.archiveHandle =
                new FixtureArchive(manager);
            MoreEquipmentSlotsNativeRuntime unequipRuntime =
                CreateProductRuntime();
            EquipmentSlotStorageDocument unequipDocument =
                ConfigureSeparatedProductShield(
                    unequipRuntime,
                    shieldValue: 5);
            Assert(
                unequipRuntime.RequestUnequip(0) &&
                DolocAPI.CountItem(
                    "fixture-shield",
                    false) == 1 &&
                unequipDocument.Slots[0].IsOccupied,
                "Working unequip must leave committed occupied while placing exactly one item into native memory.");
            bool unequipDeferred = false;
            try
            {
                unequipRuntime.DeactivateOwner("Unload");
            }
            catch (InvalidOperationException)
            {
                unequipDeferred = true;
            }
            Assert(
                unequipDeferred &&
                unequipDocument.Slots[0].IsOccupied &&
                unequipDocument.Journal == null &&
                DolocAPI.CountItem(
                    "fixture-shield",
                    false) == 1,
                "Explicit Unload after Working unequip must defer and retain the committed authority until gameplay is saved or discarded.");

            DolocAPI.ResetInventory();
            DolocAPI.archiveHandle =
                new FixtureArchive(manager);
            MoreEquipmentSlotsNativeRuntime equipRuntime =
                CreateProductRuntime();
            EquipmentSlotStorageDocument equipDocument =
                ConfigureSeparatedProductShield(
                    equipRuntime,
                    shieldValue: 5);
            equipDocument.Slots[0].Clear();
            GetField(equipRuntime, "workingSlots").SetValue(
                equipRuntime,
                EquipmentSlotStorageDocument
                    .CreateEmptySlots());
            DolocAPI.SetItemCount("fixture-shield", 1);
            Assert(
                equipRuntime.EquipFromBackpack(
                    "fixture-shield",
                    0) &&
                DolocAPI.CountItem(
                    "fixture-shield",
                    false) == 0,
                "Working equip must withdraw exactly one native item without changing committed sidecar slots.");
            bool equipDeferred = false;
            try
            {
                equipRuntime.DeactivateOwner("Unload");
            }
            catch (InvalidOperationException)
            {
                equipDeferred = true;
            }
            Assert(
                equipDeferred &&
                equipDocument.Journal == null &&
                !equipDocument.Slots[0].IsOccupied &&
                DolocAPI.CountItem(
                    "fixture-shield",
                    false) == 0,
                "Explicit Unload after Working equip must defer without converting unsaved gameplay into durable recovery.");

            DolocAPI.ResetInventory();
            DolocAPI.archiveHandle =
                new FixtureArchive(manager);
            MoreEquipmentSlotsNativeRuntime breakRuntime =
                CreateProductRuntime();
            EquipmentSlotStorageDocument breakDocument =
                ConfigureSeparatedProductShield(
                    breakRuntime,
                    shieldValue: 3);
            bool dead = false;
            bool handled = false;
            Assert(
                !breakRuntime.HandleAttackPrefix(
                    new DolocTown.BodyController(),
                    10f,
                    false,
                    new UnityEngine.Vector2(),
                    ref dead,
                    ref handled),
                "The dirty-deactivation fixture could not break its Working shield.");
            bool breakDeferred = false;
            try
            {
                breakRuntime.DeactivateOwner(
                    "OwnerDeactivation");
            }
            catch (InvalidOperationException)
            {
                breakDeferred = true;
            }
            Assert(
                breakDeferred &&
                breakDocument.Journal == null &&
                breakDocument.Slots[0].ShieldValue == 3 &&
                DolocAPI.CountItem(
                    "fixture-shield",
                    false) == 0,
                "Explicit owner deactivation after an unsaved shield break must defer without persisting the Working break.");

            DolocAPI.ResetInventory();
            DolocAPI.archiveHandle =
                new FixtureArchive(manager);
            MoreEquipmentSlotsNativeRuntime damageRuntime =
                CreateProductRuntime();
            EquipmentSlotStorageDocument damageDocument =
                ConfigureSeparatedProductShield(
                    damageRuntime,
                    shieldValue: 5);
            long damageGeneration =
                damageDocument.Generation;
            bool damageDead = false;
            bool damageHandled = false;
            Assert(
                !damageRuntime.HandleAttackPrefix(
                    new DolocTown.BodyController(),
                    3f,
                    false,
                    new UnityEngine.Vector2(),
                    ref damageDead,
                    ref damageHandled),
                "The dirty-deactivation fixture could not consume partial Working shield charge.");
            bool secondDamageDead = false;
            bool secondDamageHandled = false;
            Assert(
                !damageRuntime.HandleAttackPrefix(
                    new DolocTown.BodyController(),
                    1f,
                    false,
                    new UnityEngine.Vector2(),
                    ref secondDamageDead,
                    ref secondDamageHandled) &&
                damageDocument.Generation ==
                    damageGeneration &&
                damageDocument.GameplayCandidate == null,
                "Repeated shield hits before SaveSaving must allocate no durable JSON generation or gameplay candidate.");
            bool damageDeferred = false;
            try
            {
                damageRuntime.DeactivateOwner(
                    "OwnerDeactivation");
            }
            catch (InvalidOperationException)
            {
                damageDeferred = true;
            }
            Assert(
                damageDeferred &&
                damageDocument.Journal == null &&
                damageDocument.Slots[0].ShieldValue == 5,
                "Explicit owner deactivation after unsaved shield damage must defer and preserve committed durability.");

            DolocAPI.ResetInventory();
        }

        private static void
            ProductCleanupReloadsAndStillUnpatchesOnFailure()
        {
            ResetOwners();
            var manager =
                new DolocTown.GameData.AgentEquipmentManager();
            DolocAPI.archiveHandle =
                new FixtureArchive(manager);
            MoreEquipmentSlotsNativeRuntime product =
                CreateProductRuntime();
            product.Configure(
                new MoreEquipmentSlotsConfig
                {
                    Enabled = true
                },
                "physical cleanup exception fixture");
            AssertOwnerCount(ProductOwner, 4);

            var item = new object();
            var function = new FixtureDisposable();
            manager.functions[item] = function;
            AddProductFunctionLease(
                product,
                manager.functions,
                item,
                function);
            manager.ThrowOnReload = true;
            bool rejected = false;
            try
            {
                product.Configure(
                    new MoreEquipmentSlotsConfig
                    {
                        Enabled = false
                    },
                    "physical cleanup exception fixture");
            }
            catch (AggregateException)
            {
                rejected = true;
            }
            Assert(
                rejected &&
                manager.functions.Count == 0 &&
                function.Disposed &&
                manager.ReloadCount == 1,
                "Function cleanup must remove/dispose the exact lease and attempt one native ReloadParams recomputation.");
            AssertOwnerCount(ProductOwner, 4);
            Assert(
                GetProductCallbackRuntime() == product,
                "A cleanup restoration failure must retain the enabled Product owner for an explicit retry.");
            manager.ThrowOnReload = false;
            product.Configure(
                new MoreEquipmentSlotsConfig
                {
                    Enabled = false
                },
                "physical cleanup exception retry");
            AssertOwnerCount(ProductOwner, 0);
            Assert(
                GetProductCallbackRuntime() == null,
                "A successful retry after cleanup restoration failure did not remove the exact Product owner.");
            DolocAPI.archiveHandle = null;
        }

        private static void ConfigurationDisableRestartAndLifecycleCleanup()
        {
            ResetOwners();
            object service = CreateService();
            string ownerId =
                "DTMAPI.Tests.EquipmentSlots.Lifecycle";
            Assert(
                Register(service, ownerId, enabled: true).Success,
                "Lifecycle fixture could not install the four compatibility Hooks.");
            AssertOwnerCount(CompatibilityOwner, 4);

            IList clones =
                (IList)GetField(
                    service,
                    "activeEquipmentSlotUiObjects").GetValue(service);
            IList binders =
                (IList)GetField(
                    service,
                    "equipmentSlotUiEventBinders").GetValue(service);
            clones.Add(new object());
            binders.Add(new object());
            GetField(
                service,
                "equipmentSlotsUiRendered").SetValue(service, true);

            Invoke(
                service,
                "NotifyEquipmentSlotsEnvironmentReset",
                "fixture EnvironmentReset");
            Assert(
                clones.Count == 1 &&
                binders.Count == 1,
                "EnvironmentReset destroyed live equipment UI roots instead of preserving the current panel lifecycle.");
            AssertOwnerCount(CompatibilityOwner, 4);

            Invoke(service, "NotifyEquipmentSlotsReturnedToTitle");
            Assert(
                clones.Count == 0 &&
                binders.Count == 0,
                "ReturnedToTitle did not clear Host-owned clone/listener roots.");

            EquipmentSlotsRegisterResult disabled =
                Register(service, ownerId, enabled: false);
            Assert(
                disabled.Success,
                "Configuration disable did not complete exact-owner cleanup.");
            AssertOwnerCount(CompatibilityOwner, 0);

            EquipmentSlotsRegisterResult restarted =
                Register(service, ownerId, enabled: true);
            Assert(
                restarted.Success,
                "Same-process configuration restart did not reinstall the all-or-none Hook set.");
            AssertOwnerCount(CompatibilityOwner, 4);

            RemoveOwner(
                service,
                ownerId,
                "RuntimeShutdown");
            AssertOwnerCount(CompatibilityOwner, 0);
            Assert(
                CountOwnerResources(service, ownerId) == 0,
                "Final Loader-style owner cleanup retained compatibility resources.");
            string summary =
                (string)Invoke(
                    service,
                    "GetEquipmentSlotsLifecycleSummary");
            Assert(
                summary.Contains("equipmentClones=0") &&
                summary.Contains("equipmentBinders=0") &&
                summary.Contains("equipmentJournals=0") &&
                summary.Contains("equipmentEntries=0"),
                "Final lifecycle summary did not prove clone/listener/journal/entry zero state: " +
                summary);
        }

        private static object CreateService(
            Func<int, bool>? installGate = null)
        {
            string sessionRoot =
                Environment.GetEnvironmentVariable(
                    "DTMAPI_TEST_SESSION_ROOT")
                ?? throw new InvalidOperationException(
                    "The physical fixture must run inside the managed DTMAPI test session.");
            string gamePath =
                Path.Combine(
                    sessionRoot,
                    "equipment-slots-harmony-" +
                    (++fixtureSequence).ToString());
            Directory.CreateDirectory(
                Path.Combine(
                    gamePath,
                    "BepInEx",
                    "plugins"));
            var runtime =
                new DtmApiRuntime(
                    new FakeHost(gamePath),
                    new ConfigMenuRegistry());
            Assembly hostAssembly =
                Assembly.LoadFrom(
                    Path.Combine(
                        AppContext.BaseDirectory,
                        "DTMAPI.GameBridge.DolocTown.Compatibility.dll"));
            Type serviceType =
                hostAssembly.GetType(
                    "DTMAPI.GameBridge.DolocTown.EquipmentSlotsCompatibilityService",
                    throwOnError: true)
                ?? throw new TypeLoadException(
                    "EquipmentSlotsCompatibilityService");
            ConstructorInfo? constructor =
                serviceType.GetConstructors(
                        BindingFlags.Instance |
                        BindingFlags.NonPublic)
                    .SingleOrDefault(current =>
                        current.GetParameters().Length ==
                        (installGate == null ? 1 : 2));
            if (constructor == null)
                throw new MissingMethodException(
                    serviceType.FullName,
                    ".ctor");
            return constructor.Invoke(
                installGate == null
                    ? new object[] { runtime }
                    : new object[] { runtime, installGate });
        }

        private static MoreEquipmentSlotsNativeRuntime
            CreateProductRuntime(
                Func<int, bool>? installGate = null)
        {
            string sessionRoot =
                Environment.GetEnvironmentVariable(
                    "DTMAPI_TEST_SESSION_ROOT")
                ?? throw new InvalidOperationException(
                    "The physical fixture must run inside the managed DTMAPI test session.");
            string configPath =
                Path.Combine(
                    sessionRoot,
                    "equipment-slots-product-" +
                    (++fixtureSequence).ToString(),
                    "config.json");
            Directory.CreateDirectory(
                Path.GetDirectoryName(configPath)
                ?? sessionRoot);
            return new MoreEquipmentSlotsNativeRuntime(
                NullMonitor.Instance,
                configPath,
                installGate);
        }

        private static EquipmentSlotStorageDocument
            ConfigureProductShield(
                MoreEquipmentSlotsNativeRuntime runtime,
                int shieldValue,
                int shieldDefend = 0,
                int defenseBonus = 0)
        {
            var document =
                new EquipmentSlotStorageDocument
                {
                    Scope = new EquipmentSlotSaveScope
                    {
                        ArchiveIndex = 2,
                        PlayerName = "fixture-player",
                        CustomPlayerName = "fixture-player",
                        TotalGameSeconds = 1
                    },
                    Generation = 1
                };
            EquipmentSlotStorageEntry shield =
                document.Slots[0];
            shield.ItemId = "fixture-shield";
            shield.DisplayName = "fixture-shield";
            shield.IsShield = true;
            shield.ShieldValue = shieldValue;
            shield.ShieldMaxValue = shieldValue;
            shield.ShieldDefend = shieldDefend;
            shield.DefenseBonus = defenseBonus;
            GetField(runtime, "config").SetValue(
                runtime,
                new MoreEquipmentSlotsConfig
                {
                    Enabled = true
                });
            GetField(runtime, "document").SetValue(
                runtime,
                document);
            GetField(runtime, "workingSlots").SetValue(
                runtime,
                document.Slots);
            string sidecarPath = Path.Combine(
                Environment.GetEnvironmentVariable(
                    "DTMAPI_TEST_SESSION_ROOT")
                    ?? throw new InvalidOperationException(
                        "Shield fixtures require the managed DTMAPI test session."),
                "equipment-slots-shield-" +
                (++fixtureSequence).ToString(),
                "sidecar.json");
            GetField(runtime, "sidecarPath").SetValue(
                runtime,
                sidecarPath);
            return document;
        }

        private static EquipmentSlotStorageDocument
            ConfigureSeparatedProductShield(
                MoreEquipmentSlotsNativeRuntime runtime,
                int shieldValue,
                int shieldDefend = 0,
                int defenseBonus = 0)
        {
            EquipmentSlotStorageDocument committed =
                ConfigureProductShield(
                    runtime,
                    shieldValue,
                    shieldDefend,
                    defenseBonus);
            GetField(runtime, "workingSlots").SetValue(
                runtime,
                EquipmentSlotGameplayCandidateCoordinator
                    .CloneSlots(committed.Slots));
            return committed;
        }

        private static void AddProductFunctionLease(
            MoreEquipmentSlotsNativeRuntime runtime,
            IDictionary dictionary,
            object item,
            object function)
        {
            Type leaseType =
                typeof(MoreEquipmentSlotsNativeRuntime)
                    .GetNestedType(
                        "NativeFunctionLease",
                        BindingFlags.NonPublic)
                ?? throw new TypeLoadException(
                    "NativeFunctionLease");
            object lease =
                Activator.CreateInstance(
                    leaseType,
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic,
                    binder: null,
                    args: new[]
                    {
                        (object)dictionary,
                        item,
                        function
                    },
                    culture: null)
                ?? throw new InvalidOperationException(
                    "Could not create the product function lease fixture.");
            IList leases =
                (IList)(GetField(
                    runtime,
                    "functionLeases").GetValue(runtime)
                    ?? throw new InvalidOperationException(
                        "Product function lease list was null."));
            leases.Add(lease);
        }

        private static EquipmentSlotsRegisterResult Register(
            object service,
            string ownerId,
            bool enabled,
            int extraSlots = 3)
        {
            object? value =
                Invoke(
                    service,
                    "RegisterSlots",
                    Manifest(ownerId),
                    new EquipmentSlotsOptions
                    {
                        Enabled = enabled,
                        ExtraAttributeSlots = extraSlots,
                        SlotIdPrefix = "fixture.extra",
                        PreserveVanillaVisualSlots = true,
                        ExtraSlotsAffectVisuals = false,
                        SafeUnequipOnDisable = true,
                        AutoRecoverOnMissingMod = true,
                        VerboseLogging = false
                    });
            return value as EquipmentSlotsRegisterResult
                ?? throw new InvalidOperationException(
                    "RegisterSlots did not return EquipmentSlotsRegisterResult.");
        }

        private static EquipmentSlotEquipResult Equip(
            object service,
            string ownerId,
            string slotId,
            string itemId) =>
            Invoke(
                service,
                "EquipExtraSlot",
                Manifest(ownerId),
                slotId,
                itemId) as EquipmentSlotEquipResult
            ?? throw new InvalidOperationException(
                "EquipExtraSlot did not return EquipmentSlotEquipResult.");

        private static EquipmentSlotEquipResult Unequip(
            object service,
            string ownerId,
            string slotId) =>
            Invoke(
                service,
                "UnequipExtraSlot",
                Manifest(ownerId),
                slotId,
                "fixture ordinary unequip") as
                EquipmentSlotEquipResult
            ?? throw new InvalidOperationException(
                "UnequipExtraSlot did not return EquipmentSlotEquipResult.");

        private static IReadOnlyList<EquipmentSlotInfo> GetSlots(
            object service,
            string ownerId) =>
            Invoke(
                service,
                "GetSlots",
                ownerId) as IReadOnlyList<EquipmentSlotInfo>
            ?? throw new InvalidOperationException(
                "GetSlots did not return the frozen ABI slot list.");

        private static void ConfigureCompatibilitySaveFixtureNative(
            DolocTown.GameData.AgentEquipmentManager manager,
            string nativeSavePath,
            int archiveIndex,
            string playerName,
            int backpackShieldCount)
        {
            DolocAPI.ResetInventory();
            DolocAPI.SetItemCount(
                "fixture-shield",
                backpackShieldCount);
            DolocAPI.archiveHandle =
                new ColdFixtureArchive(
                    archiveIndex,
                    playerName,
                    playerName,
                    manager);
            DolocAPI.dataPersistenceManager =
                new ColdFixtureDataPersistenceManager(
                    nativeSavePath);
        }

        private static void PersistCompatibilityGameplayTombstone(
            object service,
            string ownerId,
            string nativeSavePath)
        {
            Assert(
                File.Exists(nativeSavePath),
                "The compatibility native save fixture disappeared before tombstone persistence.");
            var candidates =
                GetField(
                    service,
                    "equipmentSlotGameplayCandidates")
                    .GetValue(service) as IDictionary
                ?? throw new InvalidOperationException(
                    "Compatibility gameplay candidate map was unavailable.");
            object candidate =
                candidates[ownerId]
                ?? throw new InvalidOperationException(
                    "Compatibility gameplay candidate was not prepared.");
            PropertyInfo state =
                candidate.GetType().GetProperty(
                    "State",
                    BindingFlags.Public |
                    BindingFlags.Instance)
                ?? throw new MissingMemberException(
                    candidate.GetType().FullName,
                    "State");
            state.SetValue(
                candidate,
                Enum.Parse(
                    state.PropertyType,
                    "CommittedTombstone"));
            PropertyInfo postFingerprint =
                candidate.GetType().GetProperty(
                    "PostSaveFingerprint",
                    BindingFlags.Public |
                    BindingFlags.Instance)
                ?? throw new MissingMemberException(
                    candidate.GetType().FullName,
                    "PostSaveFingerprint");
            postFingerprint.SetValue(
                candidate,
                InvokeStaticRequired(
                    service.GetType(),
                    "GetNativeSaveFingerprint",
                    2));
            object persisted =
                Invoke(
                    service,
                    "PersistEquipmentSlotStorage",
                    ownerId,
                    "fixture committed tombstone before cleanup");
            Assert(
                persisted is bool ok && ok,
                "Could not persist the fixture committed gameplay tombstone.");
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

        private static string CreateFixtureScenarioRoot(
            string prefix)
        {
            string sessionRoot =
                Environment.GetEnvironmentVariable(
                    "DTMAPI_TEST_SESSION_ROOT")
                ?? throw new InvalidOperationException(
                    "EquipmentSlots physical fixtures require the managed DTMAPI test session.");
            string scenarioRoot =
                @"\\?\" +
                Path.Combine(
                    sessionRoot,
                    (prefix ?? "equipment-slots") +
                    "-" +
                    (++fixtureSequence).ToString());
            Directory.CreateDirectory(
                Path.Combine(
                    scenarioRoot,
                    "BepInEx",
                    "plugins"));
            return scenarioRoot;
        }

        private static DtmApiRuntime CreateFixtureCoreRuntime(
            string prefix) =>
            new DtmApiRuntime(
                new FakeHost(
                    CreateFixtureScenarioRoot(prefix)),
                new ConfigMenuRegistry());

        private static IEventsHelper CreateEventsProxy(
            DtmApiRuntime runtime,
            string ownerId)
        {
            PropertyInfo eventsProperty =
                typeof(DtmApiRuntime).GetProperty(
                    "Events",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic)
                ?? throw new MissingMemberException(
                    typeof(DtmApiRuntime).FullName,
                    "Events");
            object events =
                eventsProperty.GetValue(runtime)
                ?? throw new InvalidOperationException(
                    "Runtime events were unavailable.");
            MethodInfo createProxy =
                events.GetType().GetMethod(
                    "CreateProxy",
                    BindingFlags.Instance |
                    BindingFlags.Public)
                ?? throw new MissingMethodException(
                    events.GetType().FullName,
                    "CreateProxy");
            return (IEventsHelper)(
                createProxy.Invoke(
                    events,
                    new object[]
                    {
                        ownerId
                    })
                ?? throw new InvalidOperationException(
                    "CreateProxy returned null."));
        }

        private static string GetCompatibilitySidecarPath(
            DtmApiRuntime runtime,
            string ownerId) =>
            Path.Combine(
                runtime.Paths.ConfigPath,
                "protected-items",
                "equipment-slots",
                "slot-2",
                "equipment-slots-" +
                ownerId +
                ".json");

        private static string BuildCompatibilityJournalJson(
            string ownerId,
            int archiveIndex,
            string playerName,
            int origin,
            bool attemptStarted,
            string preSaveFingerprint,
            int beforeBackpackCount)
        {
            return
                "{" +
                "\"schemaVersion\":1," +
                "\"ownerId\":\"" + ownerId + "\"," +
                "\"storageScope\":\"slot-2\"," +
                "\"archiveIndex\":" + archiveIndex + "," +
                "\"playerName\":\"" + playerName + "\"," +
                "\"customPlayerName\":\"" + playerName + "\"," +
                "\"currentScene\":\"fixture-scene\"," +
                "\"savedTotalGameSeconds\":1," +
                "\"savedAt\":\"fixture\"," +
                "\"generation\":7," +
                "\"slots\":[]," +
                "\"journal\":{" +
                "\"transactionId\":\"fixture-" + ownerId + "\"," +
                "\"state\":0," +
                "\"archiveIndex\":" + archiveIndex + "," +
                "\"slotIndex\":0," +
                "\"slotId\":\"fixture.extra.1\"," +
                "\"itemId\":\"fixture-shield\"," +
                "\"displayName\":\"fixture-shield\"," +
                "\"skillId\":\"fixture-passive\"," +
                "\"defenseBonus\":0," +
                "\"isShieldHat\":false," +
                "\"shieldMaxValue\":0," +
                "\"shieldValue\":0," +
                "\"shieldDefend\":0," +
                "\"attemptStarted\":" +
                (attemptStarted ? "true" : "false") +
                "," +
                "\"preSaveFingerprint\":\"" +
                preSaveFingerprint +
                "\"," +
                "\"postSaveFingerprint\":\"\"," +
                "\"beforeBackpackCount\":" +
                beforeBackpackCount +
                "," +
                "\"beforeMailCount\":0," +
                "\"placement\":" +
                (attemptStarted ? "1" : "0") +
                "," +
                "\"origin\":" +
                origin +
                "}" +
                "}";
        }

        private static void RemoveOwner(
            object service,
            string ownerId) =>
            RemoveOwner(
                service,
                ownerId,
                "physical fixture cleanup");

        private static void RemoveOwner(
            object service,
            string ownerId,
            string reason) =>
            Invoke(
                service,
                "RemoveOwner",
                ownerId,
                reason);

        private static int CountOwnerResources(
            object service,
            string ownerId) =>
            (int)Invoke(
                service,
                "CountOwnerResources",
                ownerId);

        private static object Invoke(
            object instance,
            string name,
            params object[] arguments)
        {
            MethodInfo? method =
                instance.GetType().GetMethod(
                    name,
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance);
            if (method == null)
                throw new MissingMethodException(
                    instance.GetType().FullName,
                    name);
            try
            {
                return method.Invoke(instance, arguments)
                    ?? string.Empty;
            }
            catch (TargetInvocationException ex)
                when (ex.InnerException != null)
            {
                throw ex.InnerException;
            }
        }

        private static FieldInfo GetField(
            object instance,
            string name) =>
            instance.GetType().GetField(
                name,
                BindingFlags.NonPublic |
                BindingFlags.Instance)
            ?? throw new MissingFieldException(
                instance.GetType().FullName,
                name);

        private static object? GetProductCallbackRuntime() =>
            typeof(MoreEquipmentSlotsCallbacks).GetField(
                "runtime",
                BindingFlags.NonPublic |
                BindingFlags.Static)?.GetValue(null);

        private static IManifest Manifest(string uniqueId) =>
            new ManifestModel
            {
                Name = uniqueId,
                Author = "DTMAPI",
                Version = "1.0.0",
                UniqueID = uniqueId,
                Type = "CodeMod"
            };

        private static void InstallOwner(
            string owner,
            IEnumerable<MethodInfo> targets)
        {
            var harmony = new Harmony(owner);
            foreach (MethodInfo target in targets)
            {
                harmony.Patch(
                    target,
                    postfix: OwnerPostfix);
            }
        }

        private static void Cleanup(string owner) =>
            Harmony.UnpatchID(owner);

        private static void ResetOwners()
        {
            Cleanup(ProductOwner);
            Cleanup(CompatibilityOwner);
            Cleanup(UnrelatedOwner);
            AssertOwnerCount(ProductOwner, 0);
            AssertOwnerCount(CompatibilityOwner, 0);
            AssertOwnerCount(UnrelatedOwner, 0);
        }

        private static int CountOwner(string owner) =>
            Targets.Count(target =>
            {
                Patches? info =
                    Harmony.GetPatchInfo(target);
                return info != null &&
                    info.Owners.Any(
                        current =>
                            current.Equals(
                                owner,
                                StringComparison.Ordinal));
            });

        private static void AssertOwnerCount(
            string owner,
            int expected)
        {
            int actual = CountOwner(owner);
            Assert(
                actual == expected,
                "Unexpected exact-target owner count for " +
                owner +
                ": expected=" +
                expected +
                ", actual=" +
                actual +
                ".");
        }

        private static void ExactTargetPostfix()
        {
        }

        private static void Assert(
            bool condition,
            string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private enum ProductColdRecoveryState
        {
            ReplacementPreparedRetry = 0,
            ReplacementNativeCommittedPrepared = 1,
            ReplacementCommittedTombstone = 2
        }

        private sealed class ProductTraitRefreshFixture
        {
            internal ProductTraitRefreshFixture(
                MoreEquipmentSlotsNativeRuntime runtime,
                EquipmentSlotStorageDocument document,
                List<EquipmentSlotStorageEntry> working,
                string sidecarPath,
                string nativeSavePath,
                byte[] committedBytes,
                long generation)
            {
                Runtime = runtime;
                Document = document;
                Working = working;
                SidecarPath = sidecarPath;
                NativeSavePath = nativeSavePath;
                CommittedBytes = committedBytes;
                Generation = generation;
            }

            internal MoreEquipmentSlotsNativeRuntime Runtime
            {
                get;
            }

            internal EquipmentSlotStorageDocument Document
            {
                get;
            }

            internal List<EquipmentSlotStorageEntry> Working
            {
                get;
            }

            internal string SidecarPath { get; }

            internal string NativeSavePath { get; }

            internal byte[] CommittedBytes { get; }

            internal long Generation { get; }
        }

        private sealed class ColdFixtureDataPersistenceManager
        {
            internal ColdFixtureDataPersistenceManager(
                string savePath) =>
                fileDataHandler =
                    new ColdFixtureFileDataHandler(
                        savePath);

            public ColdFixtureFileDataHandler
                fileDataHandler { get; }
        }

        private sealed class ColdFixtureFileDataHandler
        {
            private readonly string savePath;

            internal ColdFixtureFileDataHandler(
                string savePath) =>
                this.savePath = savePath;

            public string GetDataFullPath(
                int archiveIndex) =>
                savePath;
        }

        private sealed class ColdFixtureArchive
        {
            internal ColdFixtureArchive(
                int archive,
                string playerName,
                string customPlayerName,
                DolocTown.GameData.AgentEquipmentManager manager)
            {
                archiveIndex = archive;
                farmData =
                    new ColdFixtureFarmData(
                        playerName,
                        customPlayerName,
                        manager);
                baseDataOnLoad =
                    new ColdFixtureBaseData(
                        customPlayerName);
            }

            public int archiveIndex { get; }

            public ColdFixtureFarmData farmData { get; }

            public ColdFixtureBaseData baseDataOnLoad { get; }
        }

        private sealed class ColdFixtureFarmData
        {
            internal ColdFixtureFarmData(
                string playerName,
                string customPlayerName,
                DolocTown.GameData.AgentEquipmentManager manager)
            {
                agentData =
                    new ColdFixtureAgentData(
                        playerName,
                        customPlayerName,
                        manager);
            }

            public ColdFixtureAgentData agentData { get; }

            public ColdFixtureEmailManager emailManager { get; } =
                new ColdFixtureEmailManager();

            public string currentSceneName { get; } =
                "fixture-scene";
        }

        private sealed class ColdFixtureAgentData
        {
            internal ColdFixtureAgentData(
                string player,
                string custom,
                DolocTown.GameData.AgentEquipmentManager manager)
            {
                playerName = player;
                customPlayerName = custom;
                agentEquipment = manager;
            }

            public string playerName { get; }

            public string customPlayerName { get; }

            public DolocTown.GameData.AgentEquipmentManager
                agentEquipment { get; }
        }

        private sealed class ColdFixtureBaseData
        {
            internal ColdFixtureBaseData(
                string custom) =>
                customPlayerName = custom;

            public string customPlayerName { get; }

            public string currentScene { get; } =
                "fixture-scene";

            public long totalGameSeconds { get; } = 1;
        }

        private sealed class ColdFixtureEmailManager
        {
            public object emails { get; set; } =
                Array.Empty<object>();
        }

        private sealed class FixtureEmail
        {
            public string Id { get; set; } =
                string.Empty;

            public object emailAttaches { get; set; } =
                Array.Empty<object>();
        }

        private sealed class ProductModHelper : IDtmHelper
        {
            private readonly ProductEvents events =
                new ProductEvents();
            private readonly ProductRegistry registry =
                new ProductRegistry();
            private readonly ProductTranslation translation =
                new ProductTranslation();

            internal ProductModHelper(
                IManifest manifest,
                string configPath)
            {
                ModManifest = manifest;
                ConfigStore =
                    new ProductConfigStore(configPath);
            }

            internal ProductConfigStore ConfigStore { get; }

            public IManifest ModManifest { get; }

            public IMonitor Monitor => NullMonitor.Instance;

            public IEventsHelper Events => events;

            public IConfigHelper Config => ConfigStore;

            public IModRegistry ModRegistry => registry;

            public IWorkshopHelper Workshop => null!;

            public IUiHelper UI => null!;

            public IDiagnosticsHelper Diagnostics => null!;

            public IContentQueryHelper Content => null!;

            public IInputHelper Input => null!;

            public ITranslationHelper Translation =>
                translation;

            public TConfig ReadConfig<TConfig>()
                where TConfig : new() =>
                ConfigStore.ReadConfig<TConfig>(
                    ModManifest);

            public void WriteConfig<TConfig>(
                TConfig config) =>
                ConfigStore.WriteConfig(
                    ModManifest,
                    config);
        }

        private sealed class ProductConfigStore : IConfigHelper
        {
            private readonly string path;

            internal ProductConfigStore(string path)
            {
                this.path = Path.GetFullPath(path);
                Persisted =
                    new MoreEquipmentSlotsConfig
                    {
                        Enabled = true
                    };
                WritePersistedFile();
            }

            internal bool FailNextWrite { get; set; }

            internal MoreEquipmentSlotsConfig Persisted
            {
                get;
                private set;
            }

            public TConfig ReadConfig<TConfig>(
                IManifest manifest)
                where TConfig : new()
            {
                if (typeof(TConfig) ==
                    typeof(MoreEquipmentSlotsConfig))
                {
                    return (TConfig)(object)
                        new MoreEquipmentSlotsConfig
                        {
                            Enabled = Persisted.Enabled,
                            VerboseLogging =
                                Persisted.VerboseLogging
                        };
                }
                return new TConfig();
            }

            public void WriteConfig<TConfig>(
                IManifest manifest,
                TConfig config)
            {
                if (FailNextWrite)
                {
                    FailNextWrite = false;
                    throw new IOException(
                        "fixture config write failure");
                }
                if (config is
                    MoreEquipmentSlotsConfig equipment)
                {
                    Persisted =
                        new MoreEquipmentSlotsConfig
                        {
                            Enabled = equipment.Enabled,
                            VerboseLogging =
                                equipment.VerboseLogging
                        };
                    WritePersistedFile();
                }
            }

            public string GetConfigPath(
                IManifest manifest) =>
                path;

            public void RegisterMigration<TConfig>(
                IManifest manifest,
                Action<TConfig> migrate)
                where TConfig : new()
            {
            }

            private void WritePersistedFile()
            {
                Directory.CreateDirectory(
                    Path.GetDirectoryName(path) ??
                    throw new InvalidOperationException(
                        "Fixture config path has no parent."));
                File.WriteAllText(
                    path,
                    "{\"Enabled\":" +
                    (Persisted.Enabled
                        ? "true"
                        : "false") +
                    ",\"VerboseLogging\":" +
                    (Persisted.VerboseLogging
                        ? "true"
                        : "false") +
                    "}");
            }
        }

        private sealed class ProductRegistry : IModRegistry
        {
            public bool IsLoaded(string uniqueId) =>
                false;

            public IManifest? Get(string uniqueId) =>
                null;

            public IReadOnlyList<IManifest> GetAll() =>
                Array.Empty<IManifest>();

            public TApi? GetApi<TApi>(string uniqueId)
                where TApi : class =>
                null;

            public void RegisterApi<TApi>(TApi api)
                where TApi : class
            {
            }
        }

        private sealed class ProductTranslation :
            ITranslationHelper
        {
            public string Language => "english";

            public string Get(
                string key,
                string fallback = "") =>
                fallback ?? string.Empty;
        }

        private sealed class ProductEvents : IEventsHelper
        {
            internal ProductEvents()
            {
                GameLoop = new ProductGameLoopEvents();
                Save = new ProductSaveEvents();
            }

            public IGameLoopEvents GameLoop { get; }

            public IInputEvents Input => null!;

            public ISaveEvents Save { get; }

            public IUiEvents UI => null!;

            public IWorkshopEvents Workshop => null!;

            public IDiagnosticsEvents Diagnostics =>
                null!;
        }

        private sealed class ProductGameLoopEvents :
            IGameLoopEvents
        {
            public event EventHandler<GameLaunchedEventArgs>?
                GameLaunched
            {
                add { }
                remove { }
            }
            public event EventHandler<UpdateTickedEventArgs>?
                UpdateTicked
            {
                add { }
                remove { }
            }
            public event
                EventHandler<OneSecondUpdateTickedEventArgs>?
                OneSecondUpdateTicked
            {
                add { }
                remove { }
            }
            public event EventHandler<ReturnedToTitleEventArgs>?
                ReturnedToTitle
            {
                add { }
                remove { }
            }
        }

        private sealed class ProductSaveEvents : ISaveEvents
        {
            public event EventHandler<SaveLoadedEventArgs>?
                SaveLoaded
            {
                add { }
                remove { }
            }
            public event EventHandler<SaveSavingEventArgs>?
                SaveSaving
            {
                add { }
                remove { }
            }
            public event EventHandler<SaveSavedEventArgs>?
                SaveSaved
            {
                add { }
                remove { }
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
                "EquipmentSlotsHarmonyOwnerFixture";

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

        private sealed class FixtureArchive
        {
            internal FixtureArchive(
                DolocTown.GameData.AgentEquipmentManager manager)
            {
                farmData = new FixtureFarmData(manager);
                baseDataOnLoad = new FixtureBaseData();
            }

            public FixtureFarmData farmData { get; }

            public FixtureBaseData baseDataOnLoad { get; }
        }

        private sealed class FixtureFarmData
        {
            internal FixtureFarmData(
                DolocTown.GameData.AgentEquipmentManager manager) =>
                agentData = new FixtureAgentData(manager);

            public FixtureAgentData agentData { get; }

            public ColdFixtureEmailManager emailManager { get; } =
                new ColdFixtureEmailManager();
        }

        private sealed class FixtureAgentData
        {
            internal FixtureAgentData(
                DolocTown.GameData.AgentEquipmentManager manager) =>
                agentEquipment = manager;

            public DolocTown.GameData.AgentEquipmentManager
                agentEquipment { get; }

            public string playerName { get; } =
                "fixture-player";

            public string customPlayerName { get; } =
                "fixture-player";
        }

        private sealed class FixtureBaseData
        {
            public string customPlayerName { get; } =
                "fixture-player";

            public long totalGameSeconds { get; } = 1;
        }

        private sealed class FixtureDisposable
        {
            internal bool Disposed { get; private set; }

            internal int DisposeCount { get; private set; }

            public void Dispose()
            {
                Disposed = true;
                DisposeCount++;
            }
        }
    }
}
