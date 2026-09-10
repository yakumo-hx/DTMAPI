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
                ProductFirstFailsClosed();
                CompatibilityFirstFailsClosed();
                ProductPartialInstallRollsBackAtomically();
                ResidualCompatibilityOwnerIsRemovedAndRefused();
                ResidualProductOwnerFailsClosed();
                ProductDeactivationRemovesExactOwner();
                ProductFiveHookCallbacksAreRuntimeCompatible();
                ProductNewGameReusedSlotResetCommitsCleanAuthority();
                ProductRowFollowsNativeTailAndReusesRoots();
                ProductItemAdmissionMatchesZeroThreeOne();
                ProductShieldProviderHookUsesOfficialAttackTail();
                ProductShieldTailPreservesNativeSemantics();
                CompatibilityShieldHookPreservesCurrentNativeSemantics();
                SameItemRuntimeWithdrawalEvidenceIsExact();
                ProductIncomingUnknownBlocksReplayAndSave();
                ProductDurableIncomingUnknownNeverReplaysInProcess();
                NativeBufferWithdrawalEvidenceIsExactAndFailClosed();
                NativeMailEvidenceFiltersUnrelatedMail();
                ProductMailOutcomeUnknownReconcilesImmediatelyWithoutReplay();
                ProductOutcomeUnknownNoSaveTitleRetainsSidecar();
                ProductDurableMailOutcomeUnknownRemainsQuarantined();
                CompatibilityMailOutcomeUnknownReconcilesImmediatelyWithoutReplay();
                CompatibilityDurableMailOutcomeUnknownRemainsQuarantined();
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
            ProductNewGameReusedSlotResetCommitsCleanAuthority()
        {
            RunProductNewGameReusedSlotReset(
                "same-name",
                "same-name");
            RunProductNewGameReusedSlotReset(
                "deleted-owner",
                "different-owner");
        }

        private static void RunProductNewGameReusedSlotReset(
            string deletedOwnerName,
            string newOwnerName)
        {
            const int archiveIndex = 11;
            ResetOwners();
            DolocAPI.ResetInventory();
            var manager =
                new DolocTown.GameData.AgentEquipmentManager();
            string scenarioRoot =
                CreateFixtureScenarioRoot(
                    "product-newgame-slot-reset");
            MoreEquipmentSlotsNativeRuntime product =
                new MoreEquipmentSlotsNativeRuntime(
                    NullMonitor.Instance,
                    Path.Combine(
                        scenarioRoot,
                        "config.json"));
            try
            {
                product.Configure(
                    new MoreEquipmentSlotsConfig
                    {
                        Enabled = true
                    },
                    "NewGame reused-slot fixture");
                string configRoot =
                    (string)(GetField(
                        product,
                        "configRoot").GetValue(product)
                        ?? throw new InvalidOperationException(
                            "NewGame fixture config root was unavailable."));
                string equipmentRoot = Path.Combine(
                    configRoot,
                    "protected-items",
                    "equipment-slots");
                string slotDirectory = Path.Combine(
                    equipmentRoot,
                    "slot-11");
                string siblingDirectory = Path.Combine(
                    equipmentRoot,
                    "slot-10");
                string fileName =
                    "equipment-slots-" +
                    MoreEquipmentSlotsProductContract
                        .UniqueId +
                    ".json";
                string sidecarPath = Path.Combine(
                    slotDirectory,
                    fileName);
                string siblingPath = Path.Combine(
                    siblingDirectory,
                    fileName);
                var staleDocument =
                    new EquipmentSlotStorageDocument
                    {
                        Scope = new EquipmentSlotSaveScope
                        {
                            ArchiveIndex = archiveIndex,
                            PlayerName = deletedOwnerName,
                            CustomPlayerName = deletedOwnerName,
                            TotalGameSeconds = 100
                        },
                        Generation = 1
                    };
                string staleItemId =
                    "stale-" + deletedOwnerName;
                staleDocument.Slots[0].ItemId =
                    staleItemId;
                staleDocument.Slots[0].DisplayName =
                    staleItemId;
                new EquipmentSlotDocumentStore()
                    .WriteAtomic(
                        sidecarPath,
                        staleDocument);
                File.Copy(
                    sidecarPath,
                    sidecarPath + ".previous");
                Directory.CreateDirectory(siblingDirectory);
                File.WriteAllText(siblingPath, "sibling");

                string nativeSavePath =
                    ConfigureProductNativeSaveFingerprint(
                        archiveIndex);
                DolocAPI.archiveHandle =
                    new ColdFixtureArchive(
                        archiveIndex,
                        string.Empty,
                        string.Empty,
                        manager);

                bool eventIndexDriftRejected = false;
                try
                {
                    product.OnSaveLoaded(
                        slot: archiveIndex - 1,
                        isNewGame: true);
                }
                catch (InvalidDataException)
                {
                    eventIndexDriftRejected = true;
                }
                Assert(
                    eventIndexDriftRejected &&
                    File.Exists(sidecarPath),
                    "NewGame must reject event/native archive-index drift before deleting the retained Product owner.");

                bool existingCurrentRejected = false;
                try
                {
                    product.OnSaveLoaded(
                        slot: null,
                        isNewGame: true);
                }
                catch (InvalidDataException)
                {
                    existingCurrentRejected = true;
                }
                Assert(
                    File.Exists(nativeSavePath) &&
                    existingCurrentRejected &&
                    File.Exists(sidecarPath),
                    "NewGame must require a missing native current file before deleting the retained Product owner.");

                File.Delete(nativeSavePath);

                product.OnSaveLoaded(
                    slot: null,
                    isNewGame: true);
                EquipmentSlotStorageDocument pending =
                    (EquipmentSlotStorageDocument)(
                        GetField(
                            product,
                            "document").GetValue(product)
                        ?? throw new InvalidOperationException(
                            "NewGame pending document was unavailable."));
                Assert(
                    !Directory.Exists(slotDirectory) &&
                    File.Exists(siblingPath) &&
                    pending.Slots.Count == 3 &&
                    !pending.Slots.Any(slot => slot.IsOccupied) &&
                    (bool)(GetField(
                        product,
                        "newGamePending").GetValue(product)
                        ?? false) &&
                    product.InstalledPatchCount == 5 &&
                    DolocAPI.CountItem(
                        staleItemId,
                        false) == 0,
                    "A proven NewGame boundary did not remove the complete reused Product directory, preserve its sibling, expose three clean slots, and keep the deleted owner's item detached.");

                DolocAPI.archiveHandle =
                    new ColdFixtureArchive(
                        archiveIndex,
                        newOwnerName,
                        newOwnerName,
                        manager);
                product.OnSaveSaving(archiveIndex);
                EquipmentSlotGameplayCandidate candidate =
                    pending.GameplayCandidate
                    ?? throw new InvalidOperationException(
                        "First NewGame SaveSaving did not prepare an empty gameplay candidate.");
                Assert(
                    File.Exists(sidecarPath) &&
                    candidate.Scope.PlayerName == newOwnerName &&
                    candidate.Scope.CustomPlayerName ==
                        newOwnerName &&
                    candidate.PreSaveFingerprint.IndexOf(
                        "|current=missing|",
                        StringComparison.Ordinal) >= 0,
                    "First NewGame SaveSaving did not rebind the post-dialogue name and persist a current=missing candidate.");

                File.WriteAllText(
                    nativeSavePath,
                    "newgame-native-committed-" +
                    newOwnerName);
                product.OnSaveSaved(archiveIndex);
                Assert(
                    pending.GameplayCandidate == null &&
                    !(bool)(GetField(
                        product,
                        "newGamePending").GetValue(product)
                        ?? true) &&
                    !pending.Slots.Any(slot => slot.IsOccupied) &&
                    DolocAPI.CountItem(
                        staleItemId,
                        false) == 0,
                    "SaveSaved did not finalize the clean NewGame Product authority without attaching the deleted owner's item.");

                product.ReturnedToTitle();
                product.OnSaveLoaded(
                    archiveIndex,
                    isNewGame: false);
                EquipmentSlotStorageDocument cold =
                    (EquipmentSlotStorageDocument)(
                        GetField(
                            product,
                            "document").GetValue(product)
                        ?? throw new InvalidOperationException(
                            "Cold NewGame Product document was unavailable."));
                Assert(
                    cold.GameplayCandidate == null &&
                    cold.Journal == null &&
                    cold.Scope.PlayerName == newOwnerName &&
                    cold.Slots.Count == 3 &&
                    !cold.Slots.Any(slot => slot.IsOccupied) &&
                    DolocAPI.CountItem(
                        staleItemId,
                        false) == 0,
                    "The clean same-slot NewGame authority did not cold-load independently from the deleted owner.");
            }
            finally
            {
                product.DeactivateOwner(
                    "RuntimeShutdown");
                AssertOwnerCount(ProductOwner, 0);
                DolocAPI.ResetInventory();
                DolocAPI.archiveHandle = null;
                DolocAPI.dataPersistenceManager = null;
            }
        }

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

        private static void
            ProductDurableMailOutcomeUnknownRemainsQuarantined()
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
            catch (InvalidOperationException)
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
            bool delayedEvidenceRejected = false;
            try
            {
                product.OnSaveSaving(archiveIndex);
            }
            catch (InvalidOperationException)
            {
                delayedEvidenceRejected = true;
            }
            Assert(
                delayedEvidenceRejected &&
                DolocAPI.SendItemAsEmailCallCount == 1 &&
                !journal.Escrow[0].AttemptCompleted &&
                document.Journal != null,
                "Delayed mail evidence must not promote a durable attempt that lacked one exact immediate observation.");
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
            ProductMailOutcomeUnknownReconcilesImmediatelyWithoutReplay()
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
                    archive.farmData.emailManager
                        .SetReadSequence(
                            new object(),
                            exactMail);
                    return string.Equals(
                            itemId,
                            "fixture-shield",
                            StringComparison.Ordinal) &&
                        count == 1;
                };
            Assert(
                product.RequestUnequip(0) &&
                DolocAPI.SendItemAsEmailCallCount == 1 &&
                product.GetSlotItemId(0) ==
                    string.Empty,
                "Product did not reconcile the one unreadable post-mail observation from the exact immediate same-call observation.");
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
                "Product SaveSaving did not preserve the already-proven immediate +1 mail result without replay.");
            File.WriteAllText(
                nativeSavePath,
                "product-mail-unknown-saved");
            product.OnSaveSaved(archiveIndex);
            Assert(
                DolocAPI.SendItemAsEmailCallCount == 1 &&
                !document.Slots[0].IsOccupied &&
                document.GameplayCandidate == null,
                "Product did not commit zero sidecar copies after immediate outcome-unknown mail reconciliation.");
            product.ReturnedToTitle();
            product.DeactivateOwner("RuntimeShutdown");
            DolocAPI.ResetInventory();
        }

        private static void
            CompatibilityDurableMailOutcomeUnknownRemainsQuarantined()
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
            bool delayedEvidenceRejected = false;
            try
            {
                Invoke(
                    service,
                    "NotifyEquipmentSlotsSaveSaving",
                    archiveIndex);
            }
            catch (Exception)
            {
                delayedEvidenceRejected = true;
            }
            Assert(
                delayedEvidenceRejected &&
                DolocAPI.SendItemAsEmailCallCount == 1 &&
                CountOwnerResources(service, ownerId) > 0,
                "Delayed mail evidence must not promote a Compatibility journal that lacked one exact immediate observation.");
            Invoke(
                service,
                "NotifyEquipmentSlotsReturnedToTitle");
            RemoveOwner(
                service,
                ownerId,
                "RuntimeShutdown");
            Assert(
                CountOwnerResources(service, ownerId) == 0,
                "Compatibility durable mail fixture retained owner resources after no-save session cleanup.");
            Cleanup(CompatibilityOwner);
            DolocAPI.ResetInventory();
        }

        private static void
            CompatibilityMailOutcomeUnknownReconcilesImmediatelyWithoutReplay()
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
                    archive.farmData.emailManager
                        .SetReadSequence(
                            new object(),
                            exactMail);
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
                unknown.Success &&
                DolocAPI.SendItemAsEmailCallCount == 1 &&
                !GetSlots(service, ownerId)
                    .Any(slot => slot.IsOccupied),
                "Compatibility Host did not reconcile the one unreadable post-mail observation from the exact immediate same-call observation.");
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
                "Compatibility Host SaveSaving did not preserve the already-proven immediate +1 mail result without replay.");
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
                "Compatibility Host did not commit zero sidecar copies after immediate outcome-unknown mail reconciliation.");
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
                .Compute(path, backupCount: 5);
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
            AssertOwnerCount(ProductOwner, 5);
            object service = CreateService();
            EquipmentSlotsRegisterResult result =
                Register(service, "DTMAPI.Tests.EquipmentSlots.ProductFirst", enabled: true);
            Assert(
                !result.Success,
                "A real product-first five-Hook owner did not reject frozen compatibility.");
            AssertOwnerCount(ProductOwner, 5);
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

        private static void ResidualCompatibilityOwnerIsRemovedAndRefused()
        {
            ResetOwners();
            InstallOwner(
                CompatibilityOwner,
                new[] { ReloadParamsTarget });
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
                (EquipmentSlotStorageEntry)Invoke(
                    product,
                    "BuildStorageEntry",
                    "fixture-hat",
                    0);
            var shieldHat =
                (EquipmentSlotStorageEntry)Invoke(
                    product,
                    "BuildStorageEntry",
                    "fixture-shield",
                    1);
            var passive =
                (EquipmentSlotStorageEntry)Invoke(
                    product,
                    "BuildStorageEntry",
                    "fixture-passive",
                    2);
            var herb =
                (EquipmentSlotStorageEntry)Invoke(
                    product,
                    "BuildStorageEntry",
                    "fixture-herb-package",
                    0);
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
                    Invoke(
                        product,
                        "BuildStorageEntry",
                        rejectedId,
                        0);
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
                Invoke(
                    product,
                    "BuildStorageEntry",
                    "fixture-shield",
                    0);
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
            CompatibilityShieldHookPreservesCurrentNativeSemantics()
        {
            ResetOwners();
            const string ownerId =
                "fixture.compatibility.current-attack";
            const int archiveIndex = 2;
            string scenarioRoot =
                CreateFixtureScenarioRoot(
                    "equipment-compat-current-attack");
            var runtime = new DtmApiRuntime(
                new FakeHost(scenarioRoot),
                new ConfigMenuRegistry());
            string nativeSavePath = Path.Combine(
                scenarioRoot,
                "doloc-save-2.data");
            File.WriteAllText(
                nativeSavePath,
                "compatibility-current-attack-preimage");
            var manager =
                new DolocTown.GameData.AgentEquipmentManager();
            ConfigureCompatibilitySaveFixtureNative(
                manager,
                nativeSavePath,
                archiveIndex,
                "fixture-player",
                backpackShieldCount: 3);
            object service = CreateService(runtime);
            Assert(
                Register(
                    service,
                    ownerId,
                    enabled: true,
                    extraSlots: 3).Success &&
                Equip(
                    service,
                    ownerId,
                    "fixture.extra.1",
                    "fixture-shield").Success &&
                Equip(
                    service,
                    ownerId,
                    "fixture.extra.2",
                    "fixture-shield").Success &&
                Equip(
                    service,
                    ownerId,
                    "fixture.extra.3",
                    "fixture-shield").Success,
                "The retained Compatibility Host current-attack fixture could not equip three managed shields.");
            SetCompatibilityShieldDefend(
                service,
                ownerId,
                0);
            AssertOwnerCount(CompatibilityOwner, 4);

            DolocAPI.ResetAttackEvidence();
            var faintBody = new DolocTown.BodyController
            {
                IsFaint = true
            };
            Assert(
                !faintBody.OnAttacked(
                    20f,
                    false,
                    new UnityEngine.Vector2(),
                    DolocTown.AttackProperties.Normal,
                    out bool faintDead) &&
                !faintDead &&
                CompatibilityShieldSummary(
                    service,
                    ownerId).Contains(
                        "fixture.extra.3:fixture-shield:10/10"),
                "A faint body must bypass the retained Compatibility Host shield without consuming charge.");

            DolocAPI.ResetAttackEvidence();
            var unattackableBody =
                new DolocTown.BodyController();
            unattackableBody.SetAttackable(false);
            Assert(
                !unattackableBody.OnAttacked(
                    20f,
                    false,
                    new UnityEngine.Vector2(),
                    DolocTown.AttackProperties.Normal,
                    out bool unattackableDead) &&
                !unattackableDead &&
                DolocAPI.DamageTipCount == 0 &&
                CompatibilityShieldSummary(
                    service,
                    ownerId).Contains(
                        "fixture.extra.3:fixture-shield:10/10"),
                "An unattackable body must bypass the retained Compatibility Host shield and all attack effects.");

            manager.HasNativeShield = true;
            DolocAPI.ResetAttackEvidence();
            var nativeShieldBody =
                new DolocTown.BodyController();
            Assert(
                nativeShieldBody.OnAttacked(
                    20f,
                    false,
                    new UnityEngine.Vector2(),
                    DolocTown.AttackProperties.Normal,
                    out bool nativeShieldDead) &&
                !nativeShieldDead &&
                nativeShieldBody.HatRenderer.ShineCount == 1 &&
                CompatibilityShieldSummary(
                    service,
                    ownerId).Contains(
                        "fixture.extra.3:fixture-shield:10/10"),
                "The official native shield must retain priority over a retained Compatibility Host shield.");
            manager.HasNativeShield = false;

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
                DolocAPI.LastDamageTip == 0 &&
                DolocAPI.HurtBroadcastCount == 0 &&
                blockedBody.HatRenderer.ShineCount == 1 &&
                blockedBody.HitBackCount == 1 &&
                CompatibilityShieldSummary(
                    service,
                    ownerId).Contains(
                        "fixture.extra.3:fixture-shield:6/10"),
                "A retained Compatibility Host shield full block must preserve the official zero-tip, shine and hit-back branch.");

            DolocAPI.ResetAttackEvidence();
            var exactBody = new DolocTown.BodyController();
            exactBody.StateManager.current =
                new DolocTown.AgentStateFishing();
            var exactThunder = new DolocTown.AttackProperties
            {
                attackType =
                    DolocTown.AttackPropertyType.Thunder
            };
            Assert(
                exactBody.OnAttacked(
                    6f,
                    false,
                    new UnityEngine.Vector2(),
                    exactThunder,
                    out bool exactDead) &&
                !exactDead &&
                DolocAPI.LastDamageTip == 0 &&
                DolocAPI.LastDamageTipHeavy &&
                DolocAPI.LastHealthCost == 0 &&
                DolocAPI.HurtBroadcastCount == 1 &&
                exactBody.InvincibilityStartCount == 1 &&
                exactBody.InvincibilityRestoreCount == 1 &&
                DolocTown.AgentStateFishing
                    .UnsetUiControlCount == 1 &&
                !exactBody.fishRodRenderer.Visible &&
                exactBody.StateManager.LastOverwriteType ==
                    typeof(DolocTown.AgentStateHit) &&
                exactBody.HitBackCount == 1 &&
                !GetSlots(service, ownerId)
                    .Single(slot =>
                        slot.SlotId ==
                        "fixture.extra.3")
                    .IsOccupied,
                "Exact retained-shield depletion must preserve the current zero-health Thunder, invincibility, fishing and hit-state tail.");

            DolocAPI.ResetAttackEvidence();
            var criticalBody = new DolocTown.BodyController();
            criticalBody.StateManager.current =
                new DolocTown.AgentStateFishing();
            Assert(
                criticalBody.OnAttacked(
                    7.5f,
                    true,
                    new UnityEngine.Vector2(),
                    DolocTown.AttackProperties.Normal,
                    out bool criticalDead) &&
                !criticalDead &&
                DolocAPI.LastDamageTip == 5 &&
                DolocAPI.LastDamageTipHeavy &&
                DolocAPI.LastHealthCost == 5 &&
                criticalBody.InvincibilityStartCount == 1 &&
                DolocTown.AgentStateFishing
                    .UnsetUiControlCount == 1 &&
                criticalBody.StateManager.LastOverwriteType ==
                    typeof(DolocTown.AgentStateHit) &&
                criticalBody.HitBackCount == 1 &&
                !GetSlots(service, ownerId)
                    .Single(slot =>
                        slot.SlotId ==
                        "fixture.extra.2")
                    .IsOccupied,
                "A broken retained shield must rewrite critical input to exact residual damage and delegate the full current official non-fatal tail.");

            DolocAPI.ResetAttackEvidence();
            DolocAPI.CostHealthReturnsDead = true;
            var fatalBody = new DolocTown.BodyController();
            fatalBody.StateManager.current =
                new DolocTown.AgentStateFishing();
            var fatalThunder = new DolocTown.AttackProperties
            {
                attackType =
                    DolocTown.AttackPropertyType.Thunder
            };
            Assert(
                fatalBody.OnAttacked(
                    15f,
                    false,
                    new UnityEngine.Vector2(),
                    fatalThunder,
                    out bool fatalDead) &&
                fatalDead &&
                DolocAPI.LastDamageTip == 5 &&
                DolocAPI.LastDamageTipHeavy &&
                DolocAPI.LastHealthCost == 5 &&
                DolocAPI.AgentController.droneController
                    .EscapeCombatCount == 1 &&
                DolocTown.AgentStateFishing
                    .UnsetUiControlCount == 0 &&
                fatalBody.InvincibilityStartCount == 0 &&
                fatalBody.StateManager.LastOverwriteType == null &&
                fatalBody.HitBackCount == 1 &&
                !GetSlots(service, ownerId)
                    .Single(slot =>
                        slot.SlotId ==
                        "fixture.extra.1")
                    .IsOccupied,
                "A fatal Thunder residual must remain on the current official death/drone branch after retained-shield breakage.");
            DolocAPI.CostHealthReturnsDead = false;

            Invoke(
                service,
                "NotifyEquipmentSlotsReturnedToTitle");
            Cleanup(CompatibilityOwner);
            DolocAPI.ResetInventory();
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
            ConfigureProductNativeSaveFingerprint(
                archiveIndex: 2);
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
            ConfigureProductSaveFixtureNative(
                new DolocTown.GameData.AgentEquipmentManager(),
                archiveIndex: 2,
                playerName: "fixture-player");
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
            ProductIncomingUnknownBlocksReplayAndSave()
        {
            ResetOwners();
            DolocAPI.ResetInventory();
            var manager =
                new DolocTown.GameData.AgentEquipmentManager();
            ConfigureProductSaveFixtureNative(
                manager,
                archiveIndex: 2,
                playerName: "fixture-player");
            MoreEquipmentSlotsNativeRuntime product =
                CreateProductRuntime();
            EquipmentSlotStorageDocument document =
                ConfigureSeparatedProductShield(
                    product,
                    shieldValue: 5);
            document.Slots[0].Clear();
            GetField(product, "workingSlots").SetValue(
                product,
                EquipmentSlotGameplayCandidateCoordinator
                    .CloneSlots(document.Slots));
            GetField(product, "archiveIndex").SetValue(
                product,
                2);
            DolocAPI.SetItemCount(
                "fixture-shield",
                1);
            DolocAPI.CostItemThrowAfterMutation = true;

            Assert(
                !product.EquipFromBackpack(
                    "fixture-shield",
                    0) &&
                DolocAPI.GetStoredItemCount(
                    "fixture-shield") == 0 &&
                DolocAPI.CostItemCallCount == 1 &&
                GetField(
                    product,
                    "pendingGameplayIncomingWithdrawal")
                    .GetValue(product) != null,
                "A gameplay CostItem mutate-then-throw path must retain one outcome-unknown guard after the native count changed exactly once.");
            Assert(
                !product.EquipFromBackpack(
                    "fixture-shield",
                    0) &&
                !product.RequestUnequip(0) &&
                DolocAPI.CostItemCallCount == 1,
                "A gameplay incoming outcome-unknown guard must block every later Product slot operation without replaying CostItem.");

            int rejectedSaves = 0;
            for (int attempt = 0; attempt < 2; attempt++)
            {
                try
                {
                    product.OnSaveSaving(2);
                }
                catch (InvalidOperationException)
                {
                    rejectedSaves++;
                }
            }
            Assert(
                rejectedSaves == 2 &&
                DolocAPI.CostItemCallCount == 1 &&
                document.GameplayCandidate == null &&
                !(bool)(GetField(
                    product,
                    "pendingNativeSave").GetValue(
                        product) ?? true),
                "Repeated SaveSaving must remain blocked by gameplay incoming outcome-unknown without replay or sidecar candidate promotion.");

            product.DeactivateOwner("RuntimeShutdown");
            DolocAPI.ResetInventory();
        }

        private static void
            ProductDurableIncomingUnknownNeverReplaysInProcess()
        {
            ResetOwners();
            DolocAPI.ResetInventory();
            var manager =
                new DolocTown.GameData.AgentEquipmentManager();
            ConfigureProductSaveFixtureNative(
                manager,
                archiveIndex: 2,
                playerName: "fixture-player");
            MoreEquipmentSlotsNativeRuntime product =
                CreateProductRuntime();
            EquipmentSlotStorageDocument document =
                ConfigureSeparatedProductShield(
                    product,
                    shieldValue: 5);
            document.Slots[0].Clear();
            document.Slots[0].ItemId = "outgoing-item";
            document.Slots[0].DisplayName = "outgoing-item";
            EquipmentSlotTransactionJournal journal =
                EquipmentSlotTransactionCoordinator
                    .PrepareReplacement(
                        document,
                        0,
                        new EquipmentSlotStorageEntry
                        {
                            Index = 0,
                            ItemId = "fixture-shield",
                            DisplayName = "fixture-shield",
                            SkillId = "shield",
                            IsShield = true,
                            ShieldValue = 5,
                            ShieldMaxValue = 5
                        });
            GetField(product, "workingSlots").SetValue(
                product,
                EquipmentSlotGameplayCandidateCoordinator
                    .CloneSlots(document.Slots));
            GetField(product, "archiveIndex").SetValue(
                product,
                2);
            string sidecarPath =
                (string)(GetField(
                    product,
                    "sidecarPath").GetValue(product) ??
                    throw new InvalidOperationException(
                        "Durable incoming fixture sidecar path was unavailable."));
            new EquipmentSlotDocumentStore().WriteAtomic(
                sidecarPath,
                document);
            DolocAPI.SetItemCount(
                "fixture-shield",
                1);
            DolocAPI.CostItemThrowAfterMutation = true;

            bool firstRejected = false;
            try
            {
                product.OnSaveSaving(2);
            }
            catch (
                EquipmentSlotNativeMutationOutcomeUnknownException)
            {
                firstRejected = true;
            }
            bool secondRejected = false;
            try
            {
                product.OnSaveSaving(2);
            }
            catch (InvalidOperationException)
            {
                secondRejected = true;
            }
            IDictionary pending =
                (IDictionary)(GetField(
                    product,
                    "pendingJournalPlacements")
                    .GetValue(product) ??
                    throw new InvalidOperationException(
                        "Durable incoming pending map was unavailable."));
            Assert(
                firstRejected &&
                secondRejected &&
                DolocAPI.TryPlaceInBackpackCallCount == 1 &&
                DolocAPI.CostItemCallCount == 1 &&
                DolocAPI.GetStoredItemCount(
                    "outgoing-item") == 1 &&
                DolocAPI.GetStoredItemCount(
                    "fixture-shield") == 0 &&
                pending.Contains(-1) &&
                journal.AttemptStarted &&
                journal.Escrow[0].AttemptCompleted &&
                !journal.IncomingAttemptCompleted &&
                document.Journal != null &&
                (bool)(GetField(
                    product,
                    "journalRecoveryBlocked").GetValue(
                        product) ?? false),
                "A durable incoming outcome-unknown must preserve one outgoing destination, one incoming mutation and a non-replayed -1 quarantine entry across repeated SaveSaving calls.");

            product.DeactivateOwner("RuntimeShutdown");
            DolocAPI.ResetInventory();
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
                5);
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
                    5);
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
                5);

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
            Assert(
                !breakRuntime.TryBlockNativeAttack(
                    10,
                    out int breakBlocked) &&
                breakBlocked == 3,
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
            Assert(
                damageRuntime.TryBlockNativeAttack(
                    3,
                    out int firstBlocked) &&
                firstBlocked == 3,
                "The dirty-deactivation fixture could not consume partial Working shield charge.");
            Assert(
                damageRuntime.TryBlockNativeAttack(
                    1,
                    out int secondBlocked) &&
                secondBlocked == 1 &&
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
            AssertOwnerCount(ProductOwner, 5);
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

        private static string CompatibilityShieldSummary(
            object service,
            string ownerId) =>
            (string)Invoke(
                service,
                "GetEquipmentSlotsStateSummaryForFixture",
                ownerId);

        private static void SetCompatibilityShieldDefend(
            object service,
            string ownerId,
            int shieldDefend)
        {
            var owners =
                GetField(
                    service,
                    "equipmentSlotEntries").GetValue(
                        service) as IDictionary
                ?? throw new InvalidOperationException(
                    "Compatibility shield entry map was unavailable.");
            var entries = owners[ownerId] as IEnumerable
                ?? throw new InvalidOperationException(
                    "Compatibility shield owner entries were unavailable.");
            foreach (object entry in entries)
            {
                PropertyInfo property = entry.GetType()
                    .GetProperty(
                        "ShieldDefend",
                        BindingFlags.Public |
                        BindingFlags.Instance)
                    ?? throw new MissingMemberException(
                        entry.GetType().FullName,
                        "ShieldDefend");
                property.SetValue(
                    entry,
                    shieldDefend,
                    null);
            }
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

        private static string ConfigureProductSaveFixtureNative(
            DolocTown.GameData.AgentEquipmentManager manager,
            int archiveIndex,
            string playerName)
        {
            string nativeSavePath =
                ConfigureProductNativeSaveFingerprint(
                    archiveIndex);
            DolocAPI.archiveHandle =
                new ColdFixtureArchive(
                    archiveIndex,
                    playerName,
                    playerName,
                    manager);
            return nativeSavePath;
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

            public int backupCount => 5;

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
                currentItem = new FixtureBufferItem(itemId);
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
                        reads > 1)
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
