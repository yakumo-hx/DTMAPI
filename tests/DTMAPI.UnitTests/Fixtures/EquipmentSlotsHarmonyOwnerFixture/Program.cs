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
    internal static partial class Program
    {
        private const string ProductOwner =
            "DTMAPI.MoreEquipmentSlotsMod.Native";
        private const string CompatibilityOwner =
            "dtmapi.gamebridge.doloctown.equipmentslots.compatibility";
        private const string UnrelatedOwner =
            "dtmapi.tests.equipmentslots.unrelated";
        private static readonly MethodInfo ReloadParamsTarget =
            typeof(DolocTown.GameData.AgentEquipmentManager).GetMethod(
                "ReloadParams",
                BindingFlags.Public | BindingFlags.Instance)
                ?? throw new MissingMethodException("AgentEquipmentManager.ReloadParams");
        private static readonly MethodInfo NativeShieldTarget =
            typeof(DolocTown.GameData.AgentEquipmentManager).GetMethod(
                "TryGetShieldItem",
                BindingFlags.Public | BindingFlags.Instance)
                ?? throw new MissingMethodException("AgentEquipmentManager.TryGetShieldItem");
        private static readonly MethodInfo NativeAttackTarget =
            typeof(DolocTown.BodyController).GetMethod(
                "OnAttacked",
                BindingFlags.Public | BindingFlags.Instance)
                ?? throw new MissingMethodException("BodyController.OnAttacked");
        private static readonly MethodInfo AccessoriesInitTarget =
            typeof(DolocTown.UI.AccessoriesBar).GetMethod(
                "__Init",
                BindingFlags.Public | BindingFlags.Instance)
                ?? throw new MissingMethodException("AccessoriesBar.__Init");
        private static readonly MethodInfo AccessoriesShowTarget =
            typeof(DolocTown.UI.AccessoriesBar).GetMethod(
                "OnStartShow",
                BindingFlags.Public | BindingFlags.Instance)
                ?? throw new MissingMethodException("AccessoriesBar.OnStartShow");
        private static readonly MethodInfo AccessoriesRenderTarget =
            typeof(DolocTown.UI.AccessoriesBar).GetMethod(
                "RenderPassiveItems",
                BindingFlags.Public | BindingFlags.Instance)
                ?? throw new MissingMethodException(
                    "AccessoriesBar.RenderPassiveItems");
        private static readonly MethodInfo AccessoriesSelectablesTarget =
            typeof(DolocTown.UI.AccessoriesBar).GetProperty(
                "allSelectablesArray",
                BindingFlags.Public | BindingFlags.Instance)?
                .GetGetMethod()
                ?? throw new MissingMethodException(
                    "AccessoriesBar.get_allSelectablesArray");
        private static readonly MethodInfo AccessoriesClearTarget =
            typeof(DolocTown.UI.AccessoriesBar).GetMethod(
                "ClearCallBack",
                BindingFlags.Public | BindingFlags.Instance)
                ?? throw new MissingMethodException(
                    "AccessoriesBar.ClearCallBack");
        private static readonly MethodInfo[] ProductTargets =
        {
            ReloadParamsTarget,
            NativeShieldTarget,
            AccessoriesRenderTarget,
            AccessoriesSelectablesTarget,
            AccessoriesClearTarget
        };
        private static readonly MethodInfo[] CompatibilityTargets =
        {
            ReloadParamsTarget,
            NativeAttackTarget,
            AccessoriesInitTarget,
            AccessoriesShowTarget
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
                ProductPartialInstallRollsBackAtomically();
                ResidualProductOwnerFailsClosed();
                ProductDeactivationRemovesExactOwner();
                ProductFiveHookCallbacksAreRuntimeCompatible();
                ProductNewGameUsesEmbeddedScopeAndExactSourceCleanup();
                ProductRowFollowsNativeTailAndReusesRoots();
                ProductItemAdmissionMatchesZeroThreeOne();
                ProductShieldProviderHookUsesOfficialAttackTail();
                ProductShieldTailPreservesNativeSemantics();
                SameItemEmbeddedReplacementPreservesInstances();
                ProductIncomingUnknownBlocksReplayAndSave();
                ProductLegacyTransactionsRemainUnavailable();
                NativeBufferWithdrawalEvidenceIsExactAndFailClosed();
                NativeMailEvidenceFiltersUnrelatedMail();
                ProductMailOutcomeUnknownReconcilesImmediatelyWithoutReplay();
                ProductOutcomeUnknownNoSaveTitlePreservesAuthorities();
                ProductTraitsCommitOnlyExactEmbeddedNativeSave();
                ProductRuntimeShutdownDiscardsUnsavedWorkingState();
                ProductOwnerDeactivationStagesMemoryAndCleansRoots();
                ProductOldFalseConfigurationKeepsGameplay();
                ProductConfigWriteFailureLeavesCommittedStateUntouched();
                ProductCleanOwnerRestartUsesNativeCommitted();
                ProductFailClosedSaveSavingCancelsCoreBoundary();
                ProductDefenseOnlyCleanupReloadsWithoutFunctionLease();
                ProductCleanupReloadsAndStillUnpatchesOnFailure();
                ProductNativeItemStateRoundTrips();
                ProductEffectsRetainUnchangedItems();
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
            RunProductColdRecoveryMutationExceptionScenario(
                NativePlacementKind.Backpack,
                immediateEvidenceFails: false);
            RunProductColdRecoveryMutationExceptionScenario(
                NativePlacementKind.Backpack,
                immediateEvidenceFails: true);
            RunProductColdRecoveryMutationExceptionScenario(
                NativePlacementKind.Mail,
                immediateEvidenceFails: false);
            RunProductColdRecoveryMutationExceptionScenario(
                NativePlacementKind.Mail,
                immediateEvidenceFails: true);
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
            RunNativeEmbeddedPresenceBlocksColdRecovery();
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
            DolocAPI.archiveHandle = NewArchive(archiveIndex, playerName);
            Assert(!EquipmentSlotEmbeddedArchive.HasCurrentKey(),
                "Ambiguous-format control requires readable native key absence.");
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
                        "{\"schemaVersion\":5,\"scope\":{\"archiveIndex\":2,\"playerName\":\"fixture-player\",\"customPlayerName\":\"fixture-player\"}}",
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
                        "{\"schemaVersion\":5,\"scope\":{\"archiveIndex\":2,\"playerName\":\"fixture-player\",\"customPlayerName\":\"fixture-player\"}}"),
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
            archive.farmData.emailManager.emails =
                "not-a-mail-collection";
            bool stringCollectionRejected = false;
            try
            {
                EquipmentSlotNativeMailEvidence
                    .CountUnacceptedDtmapiItemMail(
                        typeof(DolocAPI),
                        "fixture-shield");
            }
            catch (InvalidDataException)
            {
                stringCollectionRejected = true;
            }
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
                                reward = new object()
                            }
                        }
                    }
                };
            bool wrongRewardTypeRejected = false;
            try
            {
                EquipmentSlotNativeMailEvidence
                    .CountUnacceptedDtmapiItemMail(
                        typeof(DolocAPI),
                        "fixture-shield");
            }
            catch (InvalidDataException)
            {
                wrongRewardTypeRejected = true;
            }
            Assert(
                nonEnumerableRejected &&
                missingIdRejected &&
                attachmentsRejected &&
                stringCollectionRejected &&
                wrongRewardTypeRejected,
                "Production native mail evidence must fail closed when the top-level list, email identity, exact-template attachment collection, or exact RewardItem authority is unreadable.");
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
            DolocAPI.archiveHandle = new ColdFixtureArchive(archiveIndex, playerName, playerName, manager);
            Assert(!EquipmentSlotEmbeddedArchive.HasCurrentKey(),
                "Cold replacement positive requires readable native key absence before recovery.");
            DolocAPI.SetItemCount(
                sameItem
                    ? outgoingItemId
                    : incomingItemId,
                1);
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

            DolocAPI.archiveHandle =
                new ColdFixtureArchive(
                    archiveIndex,
                    playerName,
                    playerName,
                    manager);
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
            RunProductColdRecoveryMutationExceptionScenario(
                NativePlacementKind destination,
                bool immediateEvidenceFails)
        {
            const int archiveIndex = 2;
            const string playerName = "fixture-player";
            string itemId =
                "cold-mutation-" +
                destination +
                "-" +
                (immediateEvidenceFails
                    ? "unreadable"
                    : "resolved");
            string scenarioRoot =
                CreateFixtureScenarioRoot(
                    "ech-cold-mutation-" +
                    destination.ToString().ToLowerInvariant() +
                    "-" +
                    (immediateEvidenceFails
                        ? "unreadable"
                        : "resolved"));
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
                "cold-native-mutation-preimage");
            var manager =
                new DolocTown.GameData.AgentEquipmentManager();
            var archive =
                new ColdFixtureArchive(
                    archiveIndex,
                    playerName,
                    playerName,
                    manager);
            DolocAPI.ResetInventory();
            DolocAPI.archiveHandle = archive;
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
            document.Slots[0].ItemId = itemId;
            document.Slots[0].DisplayName = itemId;
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
                    "Cold mutation fixture sidecar directory was unavailable."));
            var store = new EquipmentSlotDocumentStore();
            store.WriteAtomic(sidecarPath, document);

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
                                    itemName = itemId,
                                    itemCount = 1
                                }
                        }
                    }
                }
            };
            if (destination == NativePlacementKind.Backpack)
            {
                DolocAPI.TryPlaceInBackpackOverride =
                    (placedItemId, count, _) =>
                    {
                        DolocAPI.SetItemCount(
                            placedItemId,
                            DolocAPI.GetStoredItemCount(
                                placedItemId) +
                            count);
                        DolocAPI.NativeMutationObservationPending =
                            true;
                        DolocAPI.ThrowOnNativeMutationObservation =
                            immediateEvidenceFails;
                        throw new InvalidOperationException(
                            "fixture backpack mutated then threw");
                    };
            }
            else
            {
                DolocAPI.BackpackPlacementAvailable = false;
                DolocAPI.SendItemAsEmailOverride =
                    (_, _) =>
                    {
                        archive.farmData.emailManager.emails =
                            immediateEvidenceFails
                                ? new object()
                                : exactMail;
                        throw new InvalidOperationException(
                            "fixture mail mutated then threw");
                    };
            }

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
            bool recovered =
                recoveryResult is bool value && value;
            Assert(
                recoverArguments[2] is bool handled &&
                handled &&
                CountProductColdRecoverySessions(service) == 1 &&
                (destination == NativePlacementKind.Backpack
                    ? DolocAPI.TryPlaceInBackpackCallCount == 1
                    : DolocAPI.SendItemAsEmailCallCount == 1),
                "Cold mutation exception did not retain exactly one pre-registered quarantine session and one native call for " +
                destination +
                ".");

            EquipmentSlotStorageDocument afterRecovery =
                store.Load(sidecarPath, scope);
            EquipmentSlotTransactionJournal journal =
                afterRecovery.Journal ??
                throw new InvalidOperationException(
                    "Cold mutation exception lost its durable journal.");
            if (!immediateEvidenceFails)
            {
                Assert(
                    recovered &&
                    journal.AttemptStarted &&
                    journal.Escrow[0].AttemptCompleted &&
                    journal.Escrow[0].Placement == destination &&
                    (destination != NativePlacementKind.Backpack ||
                     DolocAPI.NativeMutationObservationCallCount == 1),
                    "The one permitted immediate observation did not resolve the native mutation exactly once for " +
                    destination +
                    ".");
                InvokeVoid(
                    service,
                    "NotifyEquipmentSlotsSaveSaving",
                    archiveIndex);
                File.WriteAllText(
                    nativeSavePath,
                    "cold-native-mutation-committed");
                InvokeVoid(
                    service,
                    "NotifyEquipmentSlotsSaveSaved",
                    archiveIndex);
                EquipmentSlotStorageDocument committed =
                    store.Load(sidecarPath, scope);
                Assert(
                    committed.Journal == null &&
                    committed.Slots.All(slot =>
                        !slot.IsOccupied) &&
                    CountProductColdRecoverySessions(service) == 0,
                    "Resolved cold mutation did not commit and clear exactly once for " +
                    destination +
                    ".");
            }
            else
            {
                Assert(
                    !recovered &&
                    journal.AttemptStarted &&
                    !journal.Escrow[0].AttemptCompleted &&
                    (destination != NativePlacementKind.Backpack ||
                     DolocAPI.NativeMutationObservationCallCount == 1),
                    "Unreadable immediate evidence did not retain an incomplete durable escrow for " +
                    destination +
                    ".");
                if (destination == NativePlacementKind.Backpack)
                {
                    DolocAPI.ThrowOnNativeMutationObservation = false;
                }
                else
                {
                    archive.farmData.emailManager.emails =
                        exactMail;
                }
                bool saveSavingBlocked = false;
                try
                {
                    InvokeVoid(
                        service,
                        "NotifyEquipmentSlotsSaveSaving",
                        archiveIndex);
                }
                catch (TargetInvocationException ex)
                    when (ex.InnerException is
                        InvalidOperationException)
                {
                    saveSavingBlocked = true;
                }
                File.WriteAllText(
                    nativeSavePath,
                    "cold-native-mutation-should-not-promote");
                InvokeVoid(
                    service,
                    "NotifyEquipmentSlotsSaveSaved",
                    archiveIndex);
                EquipmentSlotStorageDocument retained =
                    store.Load(sidecarPath, scope);
                Assert(
                    saveSavingBlocked &&
                    retained.Journal != null &&
                    retained.Journal.AttemptStarted &&
                    !retained.Journal.Escrow[0]
                        .AttemptCompleted &&
                    CountProductColdRecoverySessions(service) == 1 &&
                    (destination == NativePlacementKind.Backpack
                        ? DolocAPI.TryPlaceInBackpackCallCount == 1
                        : DolocAPI.SendItemAsEmailCallCount == 1),
                    "Delayed evidence promoted, replayed, or released a quarantined cold mutation for " +
                    destination +
                    ".");
            }
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
                    nativeSavePath + ".prev4";
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
                    "native-v3|backupCount=5|",
                    StringComparison.Ordinal) &&
                !string.Equals(
                    changedFingerprint,
                    preFingerprint,
                    StringComparison.Ordinal),
                "Cold Host must share ProductNative's native-v3 full configured archive-family commit fingerprint for " +
                (changePreviousArchive
                    ? "later previous-archive change"
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
                        current.GetParameters().Length == 2);
            return constructor.Invoke(
                new object?[]
                {
                    runtime,
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
                .Compute(path, backupCount: 5);
        }

        private static void ProductPartialInstallRollsBackAtomically()
        {
            ResetOwners();
            InstallOwner(UnrelatedOwner, ProductTargets);
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
            AssertOwnerCount(UnrelatedOwner, 5);
            Cleanup(UnrelatedOwner);
            Assert(
                GetProductCallbackRuntime() == null,
                "The real product partial-install rollback retained its static callback root.");
        }

        private static void ResidualProductOwnerFailsClosed()
        {
            ResetOwners();
            InstallOwner(
                ProductOwner,
                new[] { ReloadParamsTarget });
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
            InstallOwner(UnrelatedOwner, ProductTargets);
            MoreEquipmentSlotsNativeRuntime product =
                CreateProductRuntime();
            product.Configure(
                new MoreEquipmentSlotsConfig
                {
                    Enabled = true
                },
                "physical product deactivation fixture");
            AssertOwnerCount(ProductOwner, 5);
            AssertOwnerCount(UnrelatedOwner, 5);
            product.DeactivateOwner(
                "physical Loader-style deactivation fixture");
            AssertOwnerCount(ProductOwner, 0);
            AssertOwnerCount(UnrelatedOwner, 5);
            Assert(
                GetProductCallbackRuntime() == null,
                "Real product deactivation retained its static callback root.");
            Cleanup(UnrelatedOwner);
        }

        private static void
            ProductFiveHookCallbacksAreRuntimeCompatible()
        {
            ResetOwners();
            MoreEquipmentSlotsNativeRuntime product =
                CreateProductRuntime();
            product.Configure(
                new MoreEquipmentSlotsConfig
                {
                    Enabled = true
                },
                "physical five-Hook callback fixture");
            var bar = new DolocTown.UI.AccessoriesBar();
            bar.RenderPassiveItems(
                Array.Empty<UnityEngine.Sprite>());
            UnityEngine.UI.Selectable[] selectables =
                bar.allSelectablesArray;
            bar.ClearCallBack();
            Assert(
                selectables.Length == 2,
                "The Array-typed Product postfix did not preserve the exact native Selectable[] return when no Product UI session exists.");
            product.DeactivateOwner(
                "physical five-Hook callback fixture cleanup");
            AssertOwnerCount(ProductOwner, 0);
        }

        private static void
            ProductRowFollowsNativeTailAndReusesRoots()
        {
            ResetOwners();
            MoreEquipmentSlotsNativeRuntime product =
                CreateProductRuntime();
            product.Configure(
                new MoreEquipmentSlotsConfig
                {
                    Enabled = true
                },
                "physical Product dynamic-row fixture");
            var document =
                new EquipmentSlotStorageDocument();
            GetField(product, "document").SetValue(
                product,
                document);
            GetField(product, "workingSlots").SetValue(
                product,
                EquipmentSlotGameplayCandidateCoordinator
                    .CloneSlots(document.Slots));

            int cloneBaseline =
                DolocTown.UI.AccessorySlot.CloneCount;
            var bar = new DolocTown.UI.AccessoriesBar();
            bar.RenderPassiveItems(
                new[] { new UnityEngine.Sprite() });
            MoreEquipmentSlotsDiagnosticsSnapshot first =
                product.GetDiagnosticsSnapshot();
            Assert(
                first.CloneCount == 3 &&
                first.RootCount == 1 &&
                first.UiVisible &&
                !first.UiLayoutBlocked &&
                DolocTown.UI.AccessorySlot.CloneCount -
                    cloneBaseline == 3 &&
                bar.allSelectablesArray.Length == 6,
                "The Product must append exactly three reusable slots after the one-passive native selectable set.");

            object session =
                GetField(product, "uiSession").GetValue(product) ??
                throw new InvalidOperationException(
                    "The Product UI session was unavailable.");
            var rowRoot =
                (UnityEngine.GameObject)GetPropertyValue(
                    session,
                    "ProductRowRoot");
            var rowRect =
                (UnityEngine.RectTransform)rowRoot.transform;
            Assert(
                rowRect.sizeDelta.x == 360f &&
                rowRect.sizeDelta.y == 112f &&
                rowRect.anchoredPosition.x == 372f &&
                rowRect.anchoredPosition.y == 0f &&
                ((UnityEngine.UI.LayoutElement)(
                    rowRoot.GetComponent(
                        typeof(UnityEngine.UI.LayoutElement)) ??
                    throw new InvalidOperationException(
                        "The Product row LayoutElement was absent.")))
                    .ignoreLayout,
                "The Product row must ignore native layout and begin exactly 12px after the actual one-passive tail.");

            IList leases =
                (IList)(GetField(product, "uiLeases")
                    .GetValue(product) ??
                    throw new InvalidOperationException(
                        "The Product slot leases were unavailable."));
            Assert(
                leases.Count == 3,
                "The dynamic Product row must own exactly three slot leases.");
            object[] retainedSlots = new object[leases.Count];
            for (int index = 0; index < leases.Count; index++)
            {
                var slot =
                    (DolocTown.UI.AccessorySlot)GetPropertyValue(
                        leases[index] ??
                            throw new InvalidOperationException(),
                        "Slot");
                retainedSlots[index] = slot;
                AssertSanitizedProductSlot(
                    slot,
                    "Product slot " + index);
                Assert(
                    slot.rectTransform.sizeDelta.x == 112f &&
                    slot.rectTransform.sizeDelta.y == 112f &&
                    slot.rectTransform.anchoredPosition.x ==
                        56f + 124f * index &&
                    slot.rectTransform.anchoredPosition.y == 56f,
                    "The three Product slots must continue horizontally at full size with native 12px spacing.");
            }
            var firstSlot = (DolocTown.UI.AccessorySlot)retainedSlots[0];
            int hideHoverBefore = DolocAPI.HideHoverBoxCallCount;
            firstSlot.button.onPointerEnter.Invoke();
            Assert(
                !string.IsNullOrWhiteSpace(firstSlot.LastHint),
                "A Product slot must publish its localized hint through an initialized cloned RectTransform.");
            firstSlot.button.onPointerExit.Invoke();
            Assert(
                DolocAPI.HideHoverBoxCallCount ==
                    hideHoverBefore + 1,
                "Product pointer exit must select the exact zero-parameter DolocAPI.HideHoverBox overload.");

            int rebuildBeforeGrowth =
                bar.Panel.RebuildNavigationCount;
            bar.RenderPassiveItems(
                new[]
                {
                    new UnityEngine.Sprite(),
                    new UnityEngine.Sprite()
                });
            Assert(
                DolocTown.UI.AccessorySlot.CloneCount -
                    cloneBaseline == 3 &&
                ReferenceEquals(
                    rowRoot,
                    GetPropertyValue(session, "ProductRowRoot")) &&
                rowRect.anchoredPosition.x == 496f &&
                rowRect.anchoredPosition.y == 0f &&
                bar.allSelectablesArray.Length == 7 &&
                bar.DroneSelectable.interactable &&
                bar.DroneSelectable.navigation.mode ==
                    UnityEngine.UI.Navigation.Mode.Automatic &&
                bar.DroneGraphic.raycastTarget &&
                bar.Panel.RebuildNavigationCount ==
                    rebuildBeforeGrowth + 1,
                "Official passive growth 1->2 must move the same Product row by exactly one native 124px step, append all three Product Selectables, and leave drone state untouched.");
            for (int index = 0; index < leases.Count; index++)
            {
                Assert(
                    ReferenceEquals(
                        retainedSlots[index],
                        GetPropertyValue(
                            leases[index] ??
                                throw new InvalidOperationException(),
                            "Slot")),
                    "Official passive growth recreated a Product slot instead of moving the retained row.");
            }

            for (int passiveCount = 3;
                 passiveCount <= 5;
                 passiveCount++)
            {
                int rebuildBeforeStep =
                    bar.Panel.RebuildNavigationCount;
                bar.RenderPassiveItems(
                    new UnityEngine.Sprite[passiveCount]);
                Assert(
                    DolocTown.UI.AccessorySlot.CloneCount -
                        cloneBaseline == 3 &&
                    ReferenceEquals(
                        rowRoot,
                        GetPropertyValue(
                            session,
                            "ProductRowRoot")) &&
                    rowRect.anchoredPosition.x ==
                        372f + 124f * (passiveCount - 1) &&
                    rowRect.anchoredPosition.y == 0f &&
                    bar.allSelectablesArray.Length ==
                        5 + passiveCount &&
                    bar.DroneSelectable.interactable &&
                    bar.DroneSelectable.navigation.mode ==
                        UnityEngine.UI.Navigation.Mode.Automatic &&
                    bar.DroneGraphic.raycastTarget &&
                    bar.Panel.RebuildNavigationCount ==
                        rebuildBeforeStep + 1,
                    "Official passive growth through five slots must move the same Product row by one native 124px step, append all three Product Selectables, and leave drone state untouched.");
                for (int index = 0;
                     index < leases.Count;
                     index++)
                {
                    Assert(
                        ReferenceEquals(
                            retainedSlots[index],
                            GetPropertyValue(
                                leases[index] ??
                                    throw new InvalidOperationException(),
                                "Slot")),
                        "Official passive growth through five slots recreated a Product slot instead of moving the retained row.");
                }
            }

            bar.ClearCallBack();
            Assert(
                !product.GetDiagnosticsSnapshot().UiVisible &&
                !rowRoot.activeSelf &&
                bar.allSelectablesArray.Length == 7 &&
                bar.DroneSelectable.interactable &&
                bar.DroneGraphic.raycastTarget,
                "AccessoriesBar.ClearCallBack must hide only the Product row and leave native drone state untouched.");

            bar.RenderPassiveItems(
                new UnityEngine.Sprite[5]);
            Assert(
                product.GetDiagnosticsSnapshot().UiVisible &&
                rowRoot.activeSelf &&
                bar.allSelectablesArray.Length == 10 &&
                DolocTown.UI.AccessorySlot.CloneCount -
                    cloneBaseline == 3,
                "Reopening the same AccessoriesBar generation must reactivate and reuse the dynamic Product row.");

            bar.RenderPassiveItems(
                new UnityEngine.Sprite[6]);
            MoreEquipmentSlotsDiagnosticsSnapshot blocked =
                product.GetDiagnosticsSnapshot();
            Assert(
                blocked.UiLayoutBlocked &&
                !blocked.UiVisible &&
                bar.allSelectablesArray.Length == 8 &&
                bar.DroneSelectable.interactable &&
                bar.DroneGraphic.raycastTarget,
                "A sixth unreviewed official passive slot must hide Product UI and fail closed without mutating native Selectables or drone interaction.");

            product.ReturnedToTitle();
            MoreEquipmentSlotsDiagnosticsSnapshot cleared =
                product.GetDiagnosticsSnapshot();
            Assert(
                cleared.CloneCount == 0 &&
                cleared.RootCount == 0 &&
                !cleared.UiVisible &&
                bar.DroneSelectable.interactable &&
                bar.DroneGraphic.raycastTarget,
                "ReturnedToTitle must destroy Product roots without ever changing native drone state.");
            product.DeactivateOwner(
                "physical Product dynamic-row fixture cleanup");
            AssertOwnerCount(ProductOwner, 0);
        }

        private static void AssertSanitizedProductSlot(
            DolocTown.UI.AccessorySlot slot,
            string authority)
        {
            Assert(
                slot.ClickCallbacksCleared &&
                slot.TotalSlotListenerCount == 0 &&
                slot.TotalButtonListenerCount == 5 &&
                slot.button.onLeftContinuesClick == null &&
                slot.button.onRightContinuesClick == null,
                authority +
                " retained an inherited native click/select/pointer/move listener or continuous-click callback.");
        }

        private static void
            ProductItemAdmissionMatchesZeroThreeOne()
        {
            MoreEquipmentSlotsNativeRuntime product =
                CreateProductRuntime();
            DolocTown.ItemFactory.ShieldMaxValue = 10;
            var ordinaryHat =
                (EquipmentSlotStorageEntry)BuildFixtureEntry(product, "fixture-hat", 0);
            var shieldHat =
                (EquipmentSlotStorageEntry)BuildFixtureEntry(product, "fixture-shield", 1);
            var passive =
                (EquipmentSlotStorageEntry)BuildFixtureEntry(product, "fixture-passive", 2);
            var herb =
                (EquipmentSlotStorageEntry)BuildFixtureEntry(product, "fixture-herb-package", 0);
            Assert(
                ordinaryHat.IsOccupied &&
                ordinaryHat.DefenseBonus == 0 &&
                ordinaryHat.SkillId == string.Empty &&
                !ordinaryHat.IsShield &&
                shieldHat.IsShield &&
                shieldHat.ShieldMaxValue == 10 &&
                shieldHat.ShieldValue == 10 &&
                passive.SkillId ==
                    "fixture-passive-skill" &&
                herb.SkillId == "fixture-herb-skill",
                "Physical item reflection must preserve 0.3.1 semantics for ordinary zero-defense hats, shield hats, ItemFunctionPassive and ItemFunctionHerbPackage.");

            foreach (string rejectedId in new[]
            {
                "fixture-skillless-passive",
                "fixture-active"
            })
            {
                bool rejected = false;
                try
                {
                    BuildFixtureEntry(product, rejectedId, 0);
                }
                catch (InvalidOperationException)
                {
                    rejected = true;
                }
                Assert(
                    rejected,
                    "Physical item reflection accepted rejected item " +
                    rejectedId +
                    ".");
            }
            DolocTown.ItemFactory.ShieldMaxValue = 0;
            bool zeroShieldRejected = false;
            try
            {
                BuildFixtureEntry(product, "fixture-shield", 0);
            }
            catch (InvalidOperationException)
            {
                zeroShieldRejected = true;
            }
            DolocTown.ItemFactory.ShieldMaxValue = 10;
            Assert(
                zeroShieldRejected,
                "Physical item reflection accepted a shield hat with non-positive MaxShieldValue.");
        }

        private static void
            ProductShieldProviderHookUsesOfficialAttackTail()
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
                "physical native shield-provider fixture");
            EquipmentSlotStorageDocument document =
                ConfigureProductShield(
                    product,
                    shieldValue: 10);

            DolocAPI.ResetAttackEvidence();
            var blockedBody = new DolocTown.BodyController();
            Assert(
                blockedBody.OnAttacked(
                    4f,
                    false,
                    new UnityEngine.Vector2(),
                    DolocTown.AttackProperties.Normal,
                    out bool blockedDead) &&
                !blockedDead &&
                document.Slots[0].ShieldValue == 6 &&
                DolocAPI.LastHealthCost == 0 &&
                DolocAPI.LastDamageTip == 0 &&
                DolocAPI.HurtBroadcastCount == 0 &&
                blockedBody.HatRenderer.ShineCount == 1 &&
                blockedBody.HitBackCount == 1 &&
                DolocAPI.uiSystem.agentStatusBar
                    .UpdateHealthCount == 1,
                "The physical TryGetShieldItem postfix must inject the ProductNative adapter and let the official attack method own the full-block tail.");

            product.ReturnedToTitle();
            document = ConfigureProductShield(
                product,
                shieldValue: 10);
            manager.HasNativeShield = true;
            DolocAPI.ResetAttackEvidence();
            var nativeBody = new DolocTown.BodyController();
            Assert(
                nativeBody.OnAttacked(
                    4f,
                    false,
                    new UnityEngine.Vector2(),
                    DolocTown.AttackProperties.Normal,
                    out bool nativeDead) &&
                !nativeDead &&
                document.Slots[0].ShieldValue == 10 &&
                nativeBody.HatRenderer.ShineCount == 1 &&
                DolocAPI.HurtBroadcastCount == 0,
                "An official native shield result must keep priority over the ProductNative provider postfix.");
            manager.HasNativeShield = false;

            product.ReturnedToTitle();
            product.DeactivateOwner(
                "physical native shield-provider cleanup");
            AssertOwnerCount(ProductOwner, 0);
            DolocAPI.archiveHandle = null;
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
            Assert(
                !AttackThroughNativeBody(
                    faintRuntime,
                    manager,
                    faintBody,
                    10f,
                    false,
                    DolocTown.AttackProperties.Normal,
                    out bool faintDead) &&
                !faintDead &&
                faintDocument.Slots[0].ShieldValue == 5,
                "An already faint body must bypass ProductNative shield consumption and run the official method.");

            DolocAPI.ResetAttackEvidence();
            MoreEquipmentSlotsNativeRuntime unattackableRuntime =
                CreateProductRuntime();
            EquipmentSlotStorageDocument unattackableDocument =
                ConfigureProductShield(
                    unattackableRuntime,
                    shieldValue: 5);
            var unattackableBody =
                new DolocTown.BodyController();
            unattackableBody.SetAttackable(false);
            Assert(
                !AttackThroughNativeBody(
                    unattackableRuntime,
                    manager,
                    unattackableBody,
                    10f,
                    false,
                    DolocTown.AttackProperties.Normal,
                    out bool unattackableDead) &&
                !unattackableDead &&
                unattackableDocument.Slots[0].ShieldValue == 5 &&
                DolocAPI.DamageTipCount == 0,
                "An unattackable body must exit before ProductNative shield consumption or attack effects.");

            MoreEquipmentSlotsNativeRuntime nativeShieldRuntime =
                CreateProductRuntime();
            EquipmentSlotStorageDocument nativeShieldDocument =
                ConfigureProductShield(
                    nativeShieldRuntime,
                    shieldValue: 5);
            manager.HasNativeShield = true;
            Assert(
                AttackThroughNativeBody(
                    nativeShieldRuntime,
                    manager,
                    new DolocTown.BodyController(),
                    10f,
                    false,
                    DolocTown.AttackProperties.Normal,
                    out bool nativeShieldDead) &&
                !nativeShieldDead &&
                nativeShieldDocument.Slots[0].ShieldValue == 5,
                "The official native shield must retain priority over the ProductNative shield.");
            manager.HasNativeShield = false;

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
            Assert(
                AttackThroughNativeBody(
                    defendRuntime,
                    manager,
                    defendBody,
                    4f,
                    false,
                    DolocTown.AttackProperties.Normal,
                    out bool defendDead) &&
                !defendDead &&
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
            Assert(
                AttackThroughNativeBody(
                    blockedRuntime,
                    manager,
                    blockedBody,
                    4f,
                    false,
                    DolocTown.AttackProperties.Normal,
                    out bool blockedDead) &&
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
            Assert(
                AttackThroughNativeBody(
                    defenseOnceRuntime,
                    manager,
                    defenseOnceBody,
                    10f,
                    false,
                    DolocTown.AttackProperties.Normal,
                    out bool defenseOnceDead) &&
                !defenseOnceDead &&
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
            int reloadBefore = manager.ReloadCount;
            Assert(
                AttackThroughNativeBody(
                    equalRuntime,
                    manager,
                    equalBody,
                    3f,
                    false,
                    DolocTown.AttackProperties.Normal,
                    out bool equalDead) &&
                !equalDead &&
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
            Assert(
                AttackThroughNativeBody(
                    defendedBreakRuntime,
                    manager,
                    new DolocTown.BodyController(),
                    10f,
                    false,
                    DolocTown.AttackProperties.Normal,
                    out bool defendedBreakDead) &&
                !defendedBreakDead &&
                DolocAPI.LastHealthCost == 7,
                "A broken shield with ShieldDefend must leave nativeDamage minus charge as residual damage; ShieldDefend is not subtracted from the health tail.");

            DolocAPI.ResetAttackEvidence();
            MoreEquipmentSlotsNativeRuntime criticalRuntime =
                CreateProductRuntime();
            ConfigureProductShield(
                criticalRuntime,
                shieldValue: 3);
            Assert(
                AttackThroughNativeBody(
                    criticalRuntime,
                    manager,
                    new DolocTown.BodyController(),
                    5f,
                    true,
                    DolocTown.AttackProperties.Normal,
                    out bool criticalDead) &&
                !criticalDead &&
                DolocAPI.LastDamageTip == 7 &&
                DolocAPI.LastDamageTipHeavy &&
                DolocAPI.LastHealthCost == 7,
                "Critical damage must be calculated once by the official attack method before ProductNative shield blocking.");

            DolocAPI.ResetAttackEvidence();
            MoreEquipmentSlotsNativeRuntime thunderRuntime =
                CreateProductRuntime();
            ConfigureProductShield(
                thunderRuntime,
                shieldValue: 3);
            var thunder = new DolocTown.AttackProperties
            {
                attackType =
                    DolocTown.AttackPropertyType.Thunder
            };
            Assert(
                AttackThroughNativeBody(
                    thunderRuntime,
                    manager,
                    new DolocTown.BodyController(),
                    10f,
                    false,
                    thunder,
                    out bool thunderDead) &&
                !thunderDead &&
                DolocAPI.LastDamageTip == 7 &&
                DolocAPI.LastDamageTipHeavy &&
                DolocAPI.LastHealthCost == 7,
                "Thunder residual damage must stay on the current official heavy-tip branch.");

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
            long shieldGenerationBefore =
                persistFailureDocument.Generation;
            Assert(
                AttackThroughNativeBody(
                    persistFailureRuntime,
                    manager,
                    new DolocTown.BodyController(),
                    10f,
                    false,
                    DolocTown.AttackProperties.Normal,
                    out bool persistFailureDead) &&
                !persistFailureDead &&
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
            Assert(
                AttackThroughNativeBody(
                    rehydrateRuntime,
                    manager,
                    new DolocTown.BodyController(),
                    10f,
                    false,
                    DolocTown.AttackProperties.Normal,
                    out bool inertDead) &&
                !inertDead &&
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
            DolocAPI.SetItemCount("fixture-shield", 1);
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
            Assert(
                AttackThroughNativeBody(
                    nonFatalRuntime,
                    manager,
                    nonFatalBody,
                    10f,
                    false,
                    DolocTown.AttackProperties.Normal,
                    out bool nonFatalDead) &&
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
                nonFatalBody.AttackableType ==
                    DolocTown.AttackableType.Unattackable &&
                nonFatalBody.InvincibilityStartCount == 1 &&
                nonFatalBody.HitBackCount == 1,
                "A non-fatal residual hit must use MonsterAttack, start native invincibility, clear fishing UI/rod state and enter AgentStateHit.");

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
            Assert(
                AttackThroughNativeBody(
                    fatalRuntime,
                    manager,
                    fatalBody,
                    10f,
                    false,
                    DolocTown.AttackProperties.Normal,
                    out bool fatalDead) &&
                fatalDead &&
                DolocAPI.LastHurtReason ==
                    DolocTown.HurtReason.MonsterAttack &&
                DolocTown.AgentStateFishing
                    .UnsetUiControlCount == 0 &&
                fatalBody.StateManager.LastOverwriteType == null &&
                fatalBody.HitBackCount == 1 &&
                DolocAPI.AgentController.droneController
                    .EscapeCombatCount == 1,
                "A fatal residual hit must keep the official death/drone branch and skip the non-fatal state transition.");
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
            document.Slots[0].ItemId = "fixture-defense-hat";
            document.Slots[0].ShieldMaxValue = 0;
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
            product.DeactivateOwner("RuntimeShutdown");
            Assert(
                manager.ReloadCount == 1 &&
                manager.EquipmentAbility.defence == 2,
                "Defense-only cleanup must recompute native aggregates even when no skill function lease exists.");
            DolocAPI.archiveHandle = null;
        }

        private static void
            NativeBufferWithdrawalEvidenceIsExactAndFailClosed()
        {
            var success = new FixtureNativeBuffer(
                "fixture-shield",
                FixtureNativeBufferMode.Success);
            Assert(
                InvokeNativeBufferWithdrawal(
                    success,
                    out bool succeeded) &&
                succeeded &&
                success.CurrentItem == null,
                "The native held-item buffer must succeed only when the matching object is returned and the buffer becomes empty.");

            foreach (
                FixtureNativeBufferMode mode in
                new[]
                {
                    FixtureNativeBufferMode.ClearThenThrow,
                    FixtureNativeBufferMode.WrongReturn,
                    FixtureNativeBufferMode.FalseReturn,
                    FixtureNativeBufferMode.RetainAfterReturn,
                    FixtureNativeBufferMode.UnreadableAfter
                })
            {
                var buffer = new FixtureNativeBuffer(
                    "fixture-shield",
                    mode);
                bool unknown = false;
                try
                {
                    InvokeNativeBufferWithdrawal(
                        buffer,
                        out _);
                }
                catch (
                    EquipmentSlotNativeMutationOutcomeUnknownException)
                {
                    unknown = true;
                }
                Assert(
                    unknown,
                    "Native held-item buffer mode " +
                    mode +
                    " must be classified outcome-unknown rather than replayable failure or success.");
            }
            DolocAPI.ResetInventory();
        }

        private static bool InvokeNativeBufferWithdrawal(
            FixtureNativeBuffer buffer,
            out bool succeeded)
        {
            DolocAPI.archiveHandle =
                new FixtureBufferArchive(buffer);
            MethodInfo method =
                typeof(MoreEquipmentSlotsNativeRuntime).GetMethod(
                    "TryTakeMatchingNativeBuffer",
                    BindingFlags.NonPublic |
                    BindingFlags.Static) ??
                throw new MissingMethodException(
                    typeof(MoreEquipmentSlotsNativeRuntime)
                        .FullName,
                    "TryTakeMatchingNativeBuffer");
            object[] arguments =
            {
                "fixture-shield",
                false
            };
            try
            {
                bool attempted =
                    (bool)(method.Invoke(null, arguments) ?? false);
                succeeded = (bool)arguments[1];
                return attempted;
            }
            catch (TargetInvocationException ex)
                when (ex.InnerException != null)
            {
                throw ex.InnerException;
            }
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
            AssertOwnerCount(ProductOwner, 5);

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
                product.DeactivateOwner("RuntimeShutdown");
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
            AssertOwnerCount(ProductOwner, 0);
            Assert(
                GetProductCallbackRuntime() == null,
                "A native cleanup failure must still remove the exact Product hooks and callback root.");
            manager.ThrowOnReload = false;
            product.DeactivateOwner("RuntimeShutdown");
            AssertOwnerCount(ProductOwner, 0);
            Assert(
                GetProductCallbackRuntime() == null,
                "A successful retry after cleanup restoration failure did not remove the exact Product owner.");
            DolocAPI.archiveHandle = null;
        }

        private static object CreateService()
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
                        1);
            if (constructor == null)
                throw new MissingMethodException(
                    serviceType.FullName,
                    ".ctor");
            return constructor.Invoke(new object[] { runtime });
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
            if (DolocAPI.dataPersistenceManager == null)
            {
                ConfigureProductNativeSaveFingerprint(
                    archiveIndex: 2);
            }
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

        private static bool AttackThroughNativeBody(
            MoreEquipmentSlotsNativeRuntime runtime,
            DolocTown.GameData.AgentEquipmentManager manager,
            DolocTown.BodyController body,
            float attack,
            bool critical,
            DolocTown.AttackProperties properties,
            out bool isDead)
        {
            DolocTown.IAgentEquipmentShieldItem? previous =
                manager.ShieldOverride;
            if (!manager.HasNativeShield)
            {
                manager.ShieldOverride =
                    runtime.GetNativeShieldAdapter();
            }
            try
            {
                return body.OnAttacked(
                    attack,
                    critical,
                    new UnityEngine.Vector2(),
                    properties,
                    out isDead);
            }
            finally
            {
                manager.ShieldOverride = previous;
            }
        }

        private static void AddProductFunctionLease(
            MoreEquipmentSlotsNativeRuntime runtime,
            IDictionary dictionary,
            object item,
            object function)
        {
            dictionary.Remove(item);
            var effects = (EquipmentSlotEffects)GetField(runtime, "functionLeases").GetValue(runtime);
            effects.Reconcile(dictionary, new[] { new EquipmentSlotEffects.EffectInput(0, item, "fixture") },
                (i, skill) => function);
        }

        private static EquipmentSlotStorageEntry BuildFixtureEntry(MoreEquipmentSlotsNativeRuntime product, string id, int index)
        {
            DolocTown.ItemFactory.GenerateItem(id, 1, out var item);
            return (EquipmentSlotStorageEntry)Invoke(product, "BuildStorageEntry", item!, index);
        }

        private static string ConfigureProductNativeSaveFingerprint(
            int archiveIndex)
        {
            string sessionRoot =
                Environment.GetEnvironmentVariable(
                    "DTMAPI_TEST_SESSION_ROOT")
                ?? throw new InvalidOperationException(
                    "The physical fixture must run inside the managed DTMAPI test session.");
            string nativeSavePath = Path.Combine(
                sessionRoot,
                "equipment-slots-product-native-" +
                (++fixtureSequence).ToString(),
                "doloc-save-" +
                archiveIndex.ToString() +
                ".data");
            Directory.CreateDirectory(
                Path.GetDirectoryName(nativeSavePath)
                ?? sessionRoot);
            File.WriteAllBytes(
                nativeSavePath,
                new byte[] { 0x44, 0x54, 0x4D, 0x41, 0x50, 0x49 });
            DolocAPI.dataPersistenceManager =
                new ColdFixtureDataPersistenceManager(
                    nativeSavePath);
            return nativeSavePath;
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

        private static object GetPropertyValue(
            object instance,
            string name) =>
            instance.GetType().GetProperty(
                name,
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance)?.GetValue(instance) ??
            throw new MissingMemberException(
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

        private static int CountOwner(string owner)
        {
            MethodInfo[] targets = string.Equals(
                    owner,
                    CompatibilityOwner,
                    StringComparison.Ordinal)
                ? CompatibilityTargets
                : ProductTargets;
            return targets.Count(target =>
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
        }

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

            public int backupCount => 5;

            internal ColdFixtureFileDataHandler(
                string savePath) =>
                this.savePath = savePath;

            public string GetDataFullPath(
                int archiveIndex) =>
                savePath;

            public string TryDecryptData(string text) => text;
        }

        private sealed class ColdFixtureArchive
        {
            public FixtureInventorySystem InventorySystem { get; } = new FixtureInventorySystem();
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

            public ColdFixtureCity? cityData { get; set; } = new ColdFixtureCity();
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

            public string playerName { get; set; }

            public string customPlayerName { get; set; }

            public DolocTown.GameData.AgentEquipmentManager
                agentEquipment { get; }
        }

        private sealed class ColdFixtureBaseData
        {
            internal ColdFixtureBaseData(
                string custom) =>
                customPlayerName = custom;

            public string customPlayerName { get; set; }

            public string currentScene { get; } =
                "fixture-scene";

            public long totalGameSeconds { get; } = 1;
        }

        private sealed class ColdFixtureEmailManager
        {
            private object value = Array.Empty<object>();
            private object[]? readSequence;
            private int readIndex;

            public object emails
            {
                get
                {
                    if (readSequence == null ||
                        readSequence.Length == 0)
                    {
                        return value;
                    }
                    int index = Math.Min(
                        readIndex++,
                        readSequence.Length - 1);
                    return readSequence[index];
                }
                set
                {
                    this.value = value;
                    readSequence = null;
                    readIndex = 0;
                }
            }

            internal void SetReadSequence(
                params object[] values)
            {
                if (values == null || values.Length == 0)
                {
                    throw new ArgumentException(
                        "At least one mail evidence value is required.",
                        nameof(values));
                }
                value = values[values.Length - 1];
                readSequence = values;
                readIndex = 0;
            }
        }

        private sealed class FixtureEmail
        {
            public string Id { get; set; } =
                string.Empty;

            public object emailAttaches { get; set; } =
                Array.Empty<object>();
        }

        private sealed class ProductModHelper : IDtmHelper, IDtmHelperServices
        {
            private readonly IProtectedSourceFiles sources;
            private readonly IDtmRuntimeContext context;
            private readonly ProductEvents events =
                new ProductEvents();
            private readonly ProductRegistry registry =
                new ProductRegistry();
            private readonly ProductTranslation translation =
                new ProductTranslation();

            internal ProductModHelper(
                IManifest manifest,
                string configPath,
                IProtectedSourceFiles sources,
                Func<long?> epoch,
                bool enabled = true)
            {
                ModManifest = manifest;
                this.sources = sources;
                context = (IDtmRuntimeContext)new FixtureRuntimeContext(epoch).GetTransparentProxy();
                ConfigStore =
                    new ProductConfigStore(configPath, enabled);
            }

            public TService? GetService<TService>() where TService : class =>
                typeof(TService) == typeof(IProtectedSourceFiles) ? (TService)(object)sources :
                typeof(TService) == typeof(IDtmRuntimeContext) ? (TService)(object)context : null;

            internal void RaiseSaveLoaded(int slot) =>
                ((ProductSaveEvents)events.Save).RaiseLoaded(this, slot);

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

            internal ProductConfigStore(string path, bool enabled)
            {
                this.path = Path.GetFullPath(path);
                Persisted =
                    new MoreEquipmentSlotsConfig
                    {
                        Enabled = enabled
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
                    using (var stream = File.OpenRead(path))
                        return (TConfig)new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(TConfig)).ReadObject(stream);
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
                SaveLoaded;
            internal void RaiseLoaded(object sender, int slot) =>
                SaveLoaded?.Invoke(sender, new SaveLoadedEventArgs(slot, false));
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

        private enum FixtureNativeBufferMode
        {
            Success,
            ClearThenThrow,
            WrongReturn,
            FalseReturn,
            RetainAfterReturn,
            UnreadableAfter
        }

        private sealed class FixtureBufferItem
        {
            internal FixtureBufferItem(string itemId) =>
                name = itemId;

            public string name { get; }
        }

        private sealed class FixtureNativeBuffer
        {
            private readonly FixtureNativeBufferMode mode;
            private object? currentItem;
            private int reads;

            internal FixtureNativeBuffer(
                string itemId,
                FixtureNativeBufferMode mode)
            {
                DolocTown.ItemFactory.GenerateItem(itemId, 1, out var item);
                currentItem = item ?? (object)new FixtureBufferItem(itemId);
                this.mode = mode;
            }

            public object? CurrentItem
            {
                get
                {
                    reads++;
                    if (mode ==
                            FixtureNativeBufferMode
                                .UnreadableAfter &&
                        currentItem == null)
                    {
                        throw new InvalidOperationException(
                            "fixture held-item post-state unreadable");
                    }
                    return currentItem;
                }
            }

            public object? Take()
            {
                object? taken = currentItem;
                if (mode !=
                    FixtureNativeBufferMode.RetainAfterReturn)
                {
                    currentItem = null;
                }
                if (mode ==
                    FixtureNativeBufferMode.ClearThenThrow)
                {
                    throw new InvalidOperationException(
                        "fixture held-item clear-then-throw");
                }
                if (mode ==
                    FixtureNativeBufferMode.WrongReturn)
                {
                    return new FixtureBufferItem(
                        "different-item");
                }
                if (mode ==
                    FixtureNativeBufferMode.FalseReturn)
                {
                    return false;
                }
                return taken;
            }
        }

        private sealed class FixtureBufferInventorySystem
        {
            internal FixtureBufferInventorySystem(
                FixtureNativeBuffer value) =>
                buffer = value;

            public FixtureNativeBuffer buffer { get; }
        }

        private sealed class FixtureBufferArchive
        {
            internal FixtureBufferArchive(
                FixtureNativeBuffer buffer) =>
                InventorySystem =
                    new FixtureBufferInventorySystem(buffer);

            public FixtureBufferInventorySystem InventorySystem
            {
                get;
            }
        }

        private sealed class FixtureArchive
        {
            public FixtureInventorySystem InventorySystem { get; } = new FixtureInventorySystem();
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

            public void OnCreate() { }
            public void OnRemove() => Dispose();
            public void Dispose()
            {
                Disposed = true;
                DisposeCount++;
            }
        }
    }
}
