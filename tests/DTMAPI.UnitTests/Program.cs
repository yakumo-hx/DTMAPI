using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using DTMAPI.Abstractions;
using DTMAPI.Core.Diagnostics;
using DTMAPI.Core.Manager;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;
using DTMAPI.Core.Services;
using DTMAPI.GameBridge.DolocTown;
using DTMAPI.ModConfigMenu;
using AutoFishingMod;

namespace DTMAPI.UnitTests
{
    internal static class Program
    {
        private static int Main()
        {
            try
            {
                RuntimeStartsWithEmptyMods();
                RuntimeRotatesLatestLogAndRetainsHistory();
                RuntimeApiCanRegisterBeforeStart();
                BrokenManifestDoesNotCrashDiscovery();
                ManifestDependencyIsRequiredAliasSupportsOptionalDependencies();
                DependencyVersionApiVersionAndCircularDependencyDiagnostics();
                EntryDllMustRemainInsideModRoot();
                EntryDllMustBeDllFile();
                EntryTypeSelectsEntryAndMissingEntryTypeRejectsAmbiguousDll();
                MinimumGameVersionWithoutDetectedGameVersionLogsWarning();
                DiagnosticsSnapshotApiExposesRuntimeState();
                DiagnosticsServiceCapsErrorsWarningsAndSummary();
                ManagerViewModelMapsDiagnosticsSnapshot();
                ManagerRuntimeProviderRefreshesSnapshotAfterReportExport();
                HelperModRegistryBindsApiRegistrationToOwner();
                HighFrequencyEventsDisableHandlersAfterConsecutiveFailures();
                EventRemoveIsOwnerBound();
                BadConfigJsonIsBackedUpAndDefaultedWithTempFileWrites();
                OffThreadTimerFallbackUpdateDoesNotDispatchOrdinaryModUpdates();
                ConfigMenuEditsSaveCancelAndDetectConflicts();
                ConfigMenuCallbackFailuresRollbackAndStayInspectable();
                ConfigMenuPendingPreviewDrivesConditionalVisibility();
                DisabledDiscoveredModLocksConfigPage();
                OfficialLocalModPackagesRespectOfficialEnablement();
                WorkshopReloadHotLoadsNewlyEnabledCodeModOnceAndLocksDisabledLoadedMod();
                RuntimeUiBoundariesBlockGameplayHotkeysAndModUpdates();
                Suppress_OneFrame_ClearsAfterUpdate();
                HookCallbackSafeFallbacksReturnFallbacksAndRecordDiagnostics();
                HarmonyTargetSignatureSelectsExactOverload();
                NativeUiLayoutDiagnosticsNormalizeOfficialMenuResetCounts();
                ToolColliderPostfixRoutesKeepOilDropIsolatedFromActionCompletionFailure();
                FishingAutomationApiIsFeatureOwnedNotExperimentalBridgeOwned();
                FishingAutomationServiceFailureThrottleRecordsOneDiagnosticPerOperation();
                FishingAutomationServiceFailureRecoveryStartsNewDiagnosticsEpisodeAfterStableSuccess();
                FishingAutomationRuntimeStateResetClearsTransientState();
                GameBridgeFeatureFailureThrottleRecordsOneDiagnosticsErrorButKeepsFailureCount();
                GameBridgeFeatureFailureRecoveryStartsNewDiagnosticsEpisodeAfterStableSuccess();
                GameBridgeFeatureExceptionSummaryUnwrapsTargetInvocation();
                DebugConsoleUsesOnlyCellPointerDownRightClickGivePath();
                MovementDebugLeaseClearsAtSaveBoundariesAndMissingMotionReset();
                OilCoalDropFeatureLifecycleClearsPendingHits();
                ChestLocatorPoliciesMergeEnabledOwners();
                StrongPlantingGunNormalizesToThreeSlotContract();
                SaveSlotsNormalizeToFixedTwelveContract();
                EquipmentSlotProtectedStoragePolicyUsesPerSaveTailRecovery();
                EquipmentSlotShieldPolicyMirrorsNativeShieldHat();
                FishingAutomationOptionsNormalizeNativeStageDefaults();
                FishingAutomationBiteActionPrecedence();
                FishingAutomationMiniGameInputDecisionMatchesNativeBars();
                FishingAutomationSkipMiniGamePreservesNativePullResult();
                FishingAutomationMirrorsNativeReelWhenInputEdgeIsAbsent();
                FishingAutomationInstantBiteDefersReelUntilWaitPlay();
                FishingAutomationAnimationSpeedOnlyRunsOnReadyCastAndPull();
                FishingAutomationReadyChargeSpeedTicksNativeCastTimer();
                FishingAutomationReadyChargeTargetControlsUseToolRelease();
                AutoFishingModConfigPreservesCustomToggleKey();
                AnimalViewerLocalizationGuardRecognizesUiLocalizationComponents();
                AnimalViewerFeatureResetsCloneLifecycleOnBoundaries();
                CustomEntityRegistriesValidateRegistrationDuplicateCleanupAndSnapshots();
                Console.WriteLine("DTMAPI.UnitTests: OK");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("DTMAPI.UnitTests: FAILED");
                Console.Error.WriteLine(ex);
                return 1;
            }
        }

        private static void RuntimeApiCanRegisterBeforeStart()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
            string dir = NewTempGameDir();
            var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
            IManifest bridgeManifest = new ManifestModel
            {
                Name = "Bridge",
                Author = "DTMAPI",
                Version = DtmApiRuntime.ApiVersion,
                UniqueID = "DTMAPI.GameBridge.DolocTown",
                Type = "RuntimeApi"
            };
            runtime.RegisterRuntimeApi<IActionCompletionApi>(bridgeManifest, new FakeActionCompletionApi());
            runtime.Start();
            RuntimeSnapshot snapshot = runtime.CreateSnapshot();
            Assert(snapshot.Registry.Any(m => m.UniqueID == "DTMAPI.GameBridge.DolocTown"), "Runtime API manifest should stay registered after Start.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void RuntimeStartsWithEmptyMods()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
            string dir = NewTempGameDir();
            var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
            runtime.Start();
            RuntimeSnapshot snapshot = runtime.CreateSnapshot();
            Assert(snapshot.Registry.Count >= 1, "Runtime manifest should be registered.");
            Assert(File.Exists(runtime.Diagnostics.GetLatestLogPath()), "Latest DTMAPI log should exist.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void RuntimeRotatesLatestLogAndRetainsHistory()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                string logsDir = Path.Combine(dir, "DTMAPI", "logs");
                Directory.CreateDirectory(logsDir);

                DateTime baseTime = new DateTime(2026, 6, 16, 0, 0, 0, DateTimeKind.Utc);
                string latestPath = Path.Combine(logsDir, "latest.log");
                File.WriteAllText(latestPath, "previous-run-line");
                File.SetLastWriteTimeUtc(latestPath, baseTime.AddMinutes(1));

                string oldestHistory = string.Empty;
                for (int i = 0; i < 10; i++)
                {
                    DateTime stamp = baseTime.AddSeconds(i);
                    string historyPath = Path.Combine(logsDir, "latest-" + stamp.ToString("yyyyMMdd-HHmmssfff") + ".log");
                    if (i == 0)
                        oldestHistory = historyPath;
                    File.WriteAllText(historyPath, "history-" + i);
                    File.SetLastWriteTimeUtc(historyPath, stamp);
                }

                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                runtime.Start();

                string latestText = File.ReadAllText(runtime.Diagnostics.GetLatestLogPath());
                Assert(latestText.Contains("DTMAPI runtime starting."), "Fresh latest log should receive current startup lines.");
                Assert(!latestText.Contains("previous-run-line"), "Fresh latest log should not append previous run content.");

                string[] histories = Directory.GetFiles(logsDir, "latest-*.log");
                Assert(histories.Length == 10, "Latest log rotation should keep exactly 10 history files.");
                Assert(!File.Exists(oldestHistory), "Latest log rotation should delete the oldest history file when retaining 10.");
                string rotatedHistory = histories.FirstOrDefault(path => File.ReadAllText(path).Contains("previous-run-line")) ?? string.Empty;
                Assert(rotatedHistory.Length > 0, "Previous latest log should be rotated into retained history.");

                string report = runtime.ExportLogs();
                string[] reportEntries = ReadZipEntryNames(report);
                Assert(reportEntries.Contains("DTMAPI-history/" + Path.GetFileName(rotatedHistory)), "Diagnostic report should include retained latest-log history.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void BrokenManifestDoesNotCrashDiscovery()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
            string dir = NewTempGameDir();
            string modDir = Path.Combine(dir, "Mods", "Broken");
            Directory.CreateDirectory(modDir);
            File.WriteAllText(Path.Combine(modDir, "manifest.json"), "{ \"Name\": \"Broken\" }");
            var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
            runtime.Start();
            RuntimeSnapshot snapshot = runtime.CreateSnapshot();
            Assert(snapshot.Errors.Count >= 1, "Broken manifest should be reported as an error.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void ManifestDependencyIsRequiredAliasSupportsOptionalDependencies()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                string modDir = Path.Combine(dir, "Mods", "OptionalAlias");
                Directory.CreateDirectory(modDir);
                File.WriteAllText(
                    Path.Combine(modDir, "manifest.json"),
                    "{ \"Name\": \"Optional Alias\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.OptionalAlias\", \"Type\": \"ContentPack\", \"Dependencies\": [ { \"UniqueID\": \"DTMAPI.Tests.Missing\", \"IsRequired\": false } ] }");

                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                runtime.Start();
                RuntimeSnapshot snapshot = runtime.CreateSnapshot();
                Assert(snapshot.LoadedMods.Any(m => m.Manifest.UniqueID == "DTMAPI.Tests.OptionalAlias"), "IsRequired=false alias should make a missing dependency optional.");
                Assert(!snapshot.Errors.Any(e => e.Owner == "DTMAPI.Tests.OptionalAlias" && e.Message.Contains("缺少必需依赖")), "Optional alias dependency must not be reported as required missing.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void DependencyVersionApiVersionAndCircularDependencyDiagnostics()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                Assert(DtmApiRuntime.ApiVersion == "0.5.2-alpha", "DTMAPI runtime API version should be 0.5.2-alpha for this dev baseline.");
                Assert(DtmApiRuntime.BinaryVersion == "0.5.2.0", "DTMAPI binary/plugin version should remain numeric for BepInEx and assembly metadata.");
                WriteManifest(dir, "Base", "{ \"Name\": \"Base\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.Base\", \"Type\": \"ContentPack\" }");
                WriteManifest(dir, "NeedsBase2", "{ \"Name\": \"Needs Base 2\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.NeedsBase2\", \"Type\": \"ContentPack\", \"Dependencies\": [ { \"UniqueID\": \"DTMAPI.Tests.Base\", \"MinimumVersion\": \"2.0.0\", \"Required\": true } ] }");
                WriteManifest(dir, "OptionalNeedsBase2", "{ \"Name\": \"Optional Needs Base 2\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.OptionalNeedsBase2\", \"Type\": \"ContentPack\", \"Dependencies\": [ { \"UniqueID\": \"DTMAPI.Tests.Base\", \"MinimumVersion\": \"2.0.0\", \"Required\": false } ] }");
                WriteManifest(dir, "NeedsCurrentAlphaApi", "{ \"Name\": \"Needs Current Alpha API\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.CurrentAlphaApi\", \"Type\": \"ContentPack\", \"MinimumDTMApiVersion\": \"0.5.2-alpha\" }");
                WriteManifest(dir, "Legacy042Api", "{ \"Name\": \"Legacy 0.4.2 API\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.Legacy042Api\", \"Type\": \"ContentPack\", \"MinimumDTMApiVersion\": \"0.4.2\" }");
                WriteManifest(dir, "Legacy031Api", "{ \"Name\": \"Legacy 0.3.1 API\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.Legacy031Api\", \"Type\": \"ContentPack\", \"MinimumDTMApiVersion\": \"0.3.1\" }");
                WriteManifest(dir, "NeedsFutureAlphaApi", "{ \"Name\": \"Needs Future Alpha API\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.FutureAlphaApi\", \"Type\": \"ContentPack\", \"MinimumDTMApiVersion\": \"0.5.3-alpha\" }");
                WriteManifest(dir, "NeedsFutureApi", "{ \"Name\": \"Needs Future API\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.FutureApi\", \"Type\": \"ContentPack\", \"MinimumDTMApiVersion\": \"99.0.0\" }");
                WriteManifest(dir, "CycleA", "{ \"Name\": \"Cycle A\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.CycleA\", \"Type\": \"ContentPack\", \"Dependencies\": [ { \"UniqueID\": \"DTMAPI.Tests.CycleB\", \"Required\": true } ] }");
                WriteManifest(dir, "CycleB", "{ \"Name\": \"Cycle B\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.CycleB\", \"Type\": \"ContentPack\", \"Dependencies\": [ { \"UniqueID\": \"DTMAPI.Tests.CycleA\", \"Required\": true } ] }");

                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                runtime.Start();
                RuntimeSnapshot snapshot = runtime.CreateSnapshot();
                Assert(snapshot.LoadedMods.Any(m => m.Manifest.UniqueID == "DTMAPI.Tests.Base"), "Base dependency should load.");
                Assert(!snapshot.LoadedMods.Any(m => m.Manifest.UniqueID == "DTMAPI.Tests.NeedsBase2"), "Required dependency version mismatch should block loading.");
                Assert(snapshot.LoadedMods.Any(m => m.Manifest.UniqueID == "DTMAPI.Tests.OptionalNeedsBase2"), "Optional dependency version mismatch should warn but not block loading.");
                Assert(snapshot.LoadedMods.Any(m => m.Manifest.UniqueID == "DTMAPI.Tests.CurrentAlphaApi"), "MinimumDTMApiVersion 0.5.2-alpha should load on the current alpha runtime.");
                Assert(snapshot.LoadedMods.Any(m => m.Manifest.UniqueID == "DTMAPI.Tests.Legacy042Api"), "Legacy MinimumDTMApiVersion 0.4.2 should still load on the current alpha runtime.");
                Assert(snapshot.LoadedMods.Any(m => m.Manifest.UniqueID == "DTMAPI.Tests.Legacy031Api"), "Legacy MinimumDTMApiVersion 0.3.1 should still load on the current alpha runtime.");
                Assert(!snapshot.LoadedMods.Any(m => m.Manifest.UniqueID == "DTMAPI.Tests.FutureAlphaApi"), "Future MinimumDTMApiVersion 0.5.3-alpha should block loading.");
                Assert(!snapshot.LoadedMods.Any(m => m.Manifest.UniqueID == "DTMAPI.Tests.FutureApi"), "Future MinimumDTMApiVersion should block loading.");
                Assert(!snapshot.LoadedMods.Any(m => m.Manifest.UniqueID == "DTMAPI.Tests.CycleA"), "CycleA should be blocked and must not load.");
                Assert(!snapshot.LoadedMods.Any(m => m.Manifest.UniqueID == "DTMAPI.Tests.CycleB"), "CycleB should be blocked and must not load.");
                Assert(snapshot.Errors.Any(e => e.Owner == "DTMAPI.Tests.NeedsBase2" && e.Message.Contains("依赖版本")), "Dependency version mismatch should be diagnosed.");
                Assert(!snapshot.Errors.Any(e => e.Owner == "DTMAPI.Tests.OptionalNeedsBase2"), "Optional dependency version mismatch should not be recorded as an error.");
                Assert(snapshot.Warnings.Any(w => w.Owner == "DTMAPI.Tests.OptionalNeedsBase2" && w.Message.Contains("可选依赖版本过低")), "Optional dependency version mismatch should be recorded as a structured warning.");
                Assert(snapshot.Errors.Any(e => e.Owner == "DTMAPI.Tests.FutureAlphaApi" && e.Message.Contains("API 版本")), "Future alpha MinimumDTMApiVersion mismatch should be diagnosed.");
                Assert(snapshot.Errors.Any(e => e.Owner == "DTMAPI.Tests.FutureApi" && e.Message.Contains("API 版本")), "MinimumDTMApiVersion mismatch should be diagnosed.");
                Assert(snapshot.Errors.Any(e => e.Message.Contains("依赖循环") && e.Details.Contains("DTMAPI.Tests.CycleA") && e.Details.Contains("DTMAPI.Tests.CycleB")), "Circular dependencies should be diagnosed with the cycle path.");
                Assert(snapshot.Errors.Any(e => e.Owner == "DTMAPI.Tests.CycleA" && e.Message.Contains("依赖循环阻止加载")), "CycleA should have an owner-specific blocked diagnostic.");
                Assert(snapshot.Errors.Any(e => e.Owner == "DTMAPI.Tests.CycleB" && e.Message.Contains("依赖循环阻止加载")), "CycleB should have an owner-specific blocked diagnostic.");
                IDtmDiagnosticsSnapshot diagnosticsSnapshot = runtime.CreateDiagnosticsSnapshot();
                Assert(diagnosticsSnapshot.Mods.Single(m => m.UniqueID == "DTMAPI.Tests.Base").StatusCode == "loaded", "Loaded dependency should expose loaded status code.");
                Assert(diagnosticsSnapshot.Mods.Single(m => m.UniqueID == "DTMAPI.Tests.NeedsBase2").StatusCode == "missing-dependency", "Dependency version failures should expose a dependency status code.");
                Assert(diagnosticsSnapshot.Mods.Single(m => m.UniqueID == "DTMAPI.Tests.OptionalNeedsBase2").Loaded && diagnosticsSnapshot.Mods.Single(m => m.UniqueID == "DTMAPI.Tests.OptionalNeedsBase2").StatusCode == "loaded", "Optional dependency warnings should leave loaded mods in loaded status.");
                Assert(diagnosticsSnapshot.Warnings.Any(w => w.Owner == "DTMAPI.Tests.OptionalNeedsBase2"), "Diagnostics snapshot should include optional dependency warnings.");
                Assert(diagnosticsSnapshot.Mods.Single(m => m.UniqueID == "DTMAPI.Tests.CurrentAlphaApi").StatusCode == "loaded", "Current alpha MinimumDTMApiVersion should expose loaded status code.");
                Assert(diagnosticsSnapshot.Mods.Single(m => m.UniqueID == "DTMAPI.Tests.Legacy042Api").StatusCode == "loaded", "Legacy 0.4.2 MinimumDTMApiVersion should expose loaded status code.");
                Assert(diagnosticsSnapshot.Mods.Single(m => m.UniqueID == "DTMAPI.Tests.Legacy031Api").StatusCode == "loaded", "Legacy 0.3.1 MinimumDTMApiVersion should expose loaded status code.");
                Assert(diagnosticsSnapshot.Mods.Single(m => m.UniqueID == "DTMAPI.Tests.FutureAlphaApi").StatusCode == "api-too-new", "Future alpha MinimumDTMApiVersion should expose api-too-new status code.");
                Assert(diagnosticsSnapshot.Mods.Single(m => m.UniqueID == "DTMAPI.Tests.FutureApi").StatusCode == "api-too-new", "Future MinimumDTMApiVersion should expose api-too-new status code.");
                Assert(diagnosticsSnapshot.Mods.Single(m => m.UniqueID == "DTMAPI.Tests.CycleA").StatusCode == "dependency-cycle", "CycleA should expose dependency-cycle status code.");
                Assert(diagnosticsSnapshot.Mods.Single(m => m.UniqueID == "DTMAPI.Tests.CycleB").StatusCode == "dependency-cycle", "CycleB should expose dependency-cycle status code.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void EntryDllMustRemainInsideModRoot()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                WriteManifest(dir, "EscapingDll", "{ \"Name\": \"Escaping DLL\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.EscapingDll\", \"EntryDll\": \"../Escaping.dll\", \"Type\": \"CodeMod\" }");

                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                runtime.Start();
                RuntimeSnapshot snapshot = runtime.CreateSnapshot();
                Assert(!snapshot.LoadedMods.Any(m => m.Manifest.UniqueID == "DTMAPI.Tests.EscapingDll"), "EntryDll escaping the mod root must not load.");
                Assert(snapshot.Errors.Any(e => e.Owner == "DTMAPI.Tests.EscapingDll" && e.Message.Contains("不能逃出")), "Escaping EntryDll should have a path-safety diagnostic.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void EntryDllMustBeDllFile()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                WriteManifest(dir, "NotDll", "{ \"Name\": \"Not DLL\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.NotDll\", \"EntryDll\": \"NotDll.txt\", \"Type\": \"CodeMod\" }");

                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                runtime.Start();
                RuntimeSnapshot snapshot = runtime.CreateSnapshot();
                Assert(!snapshot.LoadedMods.Any(m => m.Manifest.UniqueID == "DTMAPI.Tests.NotDll"), "EntryDll with a non-.dll extension must not load.");
                Assert(snapshot.Errors.Any(e => e.Owner == "DTMAPI.Tests.NotDll" && e.Message.Contains(".dll")), "Non-.dll EntryDll should have a clear diagnostic.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void EntryTypeSelectsEntryAndMissingEntryTypeRejectsAmbiguousDll()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                string assemblyPath = typeof(ApiOwnerProbeMod).Assembly.Location;
                string assemblyName = Path.GetFileName(assemblyPath);
                string selectedEntryType = typeof(ApiOwnerProbeMod).FullName ?? nameof(ApiOwnerProbeMod);

                string ambiguousDir = Path.Combine(dir, "Mods", "AmbiguousEntry");
                Directory.CreateDirectory(ambiguousDir);
                File.Copy(assemblyPath, Path.Combine(ambiguousDir, assemblyName), overwrite: true);
                File.WriteAllText(
                    Path.Combine(ambiguousDir, "manifest.json"),
                    "{ \"Name\": \"Ambiguous Entry\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.AmbiguousEntry\", \"EntryDll\": \"" + assemblyName + "\", \"Type\": \"CodeMod\" }");

                string selectedDir = Path.Combine(dir, "Mods", "SelectedEntry");
                Directory.CreateDirectory(selectedDir);
                File.Copy(assemblyPath, Path.Combine(selectedDir, assemblyName), overwrite: true);
                File.WriteAllText(
                    Path.Combine(selectedDir, "manifest.json"),
                    "{ \"Name\": \"Selected Entry\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.SelectedEntry\", \"EntryDll\": \"" + assemblyName + "\", \"EntryType\": \"" + selectedEntryType + "\", \"Type\": \"CodeMod\" }");

                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                runtime.Start();
                RuntimeSnapshot snapshot = runtime.CreateSnapshot();
                IManifest selectedManifest = snapshot.DiscoveredMods.Single(m => m.Manifest.UniqueID == "DTMAPI.Tests.SelectedEntry").Manifest;
                Assert(selectedManifest.EntryType == selectedEntryType, "IManifest should expose parsed EntryType.");
                Assert(!snapshot.LoadedMods.Any(m => m.Manifest.UniqueID == "DTMAPI.Tests.AmbiguousEntry"), "DLLs with multiple DtmMod subclasses require EntryType.");
                Assert(snapshot.Errors.Any(e => e.Owner == "DTMAPI.Tests.AmbiguousEntry" && e.Message.Contains("EntryType 缺失")), "Missing EntryType in an ambiguous DLL should be diagnosed.");
                Assert(snapshot.LoadedMods.Any(m => m.Manifest.UniqueID == "DTMAPI.Tests.SelectedEntry"), "EntryType should select the requested DtmMod subclass.");
                IModRegistry registry = GetModRegistry(runtime);
                IUnitProbeApi? selectedApi = registry.GetApi<IUnitProbeApi>("DTMAPI.Tests.SelectedEntry");
                Assert(selectedApi != null && selectedApi.Owner == "DTMAPI.Tests.SelectedEntry", "Selected EntryType should execute the chosen DtmMod Entry method.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void MinimumGameVersionWithoutDetectedGameVersionLogsWarning()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                WriteManifest(dir, "NeedsGameVersion", "{ \"Name\": \"Needs Game Version\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.NeedsGameVersion\", \"Type\": \"ContentPack\", \"MinimumGameVersion\": \"99.0.0\" }");

                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                runtime.Start();
                RuntimeSnapshot snapshot = runtime.CreateSnapshot();
                Assert(snapshot.LoadedMods.Any(m => m.Manifest.UniqueID == "DTMAPI.Tests.NeedsGameVersion"), "Missing game-version detection should warn but not block loading.");
                Assert(snapshot.Warnings.Any(w => w.Owner == "DTMAPI.Tests.NeedsGameVersion" && w.Message.Contains("MinimumGameVersion")), "Runtime snapshot should include structured MinimumGameVersion warning.");
                Assert(runtime.Diagnostics.GetWarnings().Any(w => w.Owner == "DTMAPI.Tests.NeedsGameVersion" && w.Details.Contains("MinimumGameVersion=99.0.0")), "Diagnostics helper should expose structured MinimumGameVersion warning.");
                string log = File.ReadAllText(runtime.Diagnostics.GetLatestLogPath());
                Assert(log.Contains("[Warn]") && log.Contains("MinimumGameVersion") && log.Contains("cannot detect the game version"), "MinimumGameVersion should emit an explicit warning when game version cannot be detected.");
                string report = runtime.ExportLogs();
                string summary = ReadZipText(report, "dtmapi-summary.txt");
                Assert(summary.Contains("Warnings: 1") && summary.Contains("WARNING") && summary.Contains("MinimumGameVersion"), "Diagnostic report summary should include structured warnings.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void DiagnosticsSnapshotApiExposesRuntimeState()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                WriteManifest(dir, "NeedsDiagnostics", "{ \"Name\": \"Needs Diagnostics\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.NeedsDiagnostics\", \"Type\": \"ContentPack\", \"MinimumGameVersion\": \"99.0.0\" }");
                WriteManifest(dir, "NeedsMissingDependency", "{ \"Name\": \"Needs Missing Dependency\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.NeedsMissingDependency\", \"Type\": \"ContentPack\", \"Dependencies\": [ { \"UniqueID\": \"DTMAPI.Tests.MissingDependency\", \"Required\": true } ] }");
                WriteManifest(dir, "BrokenEntryDll", "{ \"Name\": \"Broken Entry DLL\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.BrokenEntryDll\", \"Type\": \"CodeMod\", \"EntryDll\": \"BrokenEntryDll.txt\" }");
                WriteManifest(dir, "NeedsFutureApi", "{ \"Name\": \"Needs Future API\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.DiagnosticsFutureApi\", \"Type\": \"ContentPack\", \"MinimumDTMApiVersion\": \"99.0.0\" }");
                WriteManifest(dir, "UnknownStatus", "{ \"Name\": \"Unknown Status\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.UnknownStatus\", \"Type\": \"ContentPack\" }");
                string throwingDir = Path.Combine(dir, "Mods", "ThrowingEntry");
                Directory.CreateDirectory(throwingDir);
                string assemblyPath = typeof(ThrowingEntryProbeMod).Assembly.Location;
                string assemblyName = Path.GetFileName(assemblyPath);
                File.Copy(assemblyPath, Path.Combine(throwingDir, assemblyName), overwrite: true);
                File.WriteAllText(
                    Path.Combine(throwingDir, "manifest.json"),
                    "{ \"Name\": \"Throwing Entry\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.ThrowingEntry\", \"Type\": \"CodeMod\", \"EntryDll\": \"" + assemblyName + "\", \"EntryType\": \"" + (typeof(ThrowingEntryProbeMod).FullName ?? nameof(ThrowingEntryProbeMod)) + "\" }");

                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                runtime.Start();
                runtime.Diagnostics.RecordError("DTMAPI.Tests.UnknownStatus", "Unexpected unit diagnostic.", "No known classifier.");
                runtime.SetHookStatus("Feature.Camera", "ready", "test", "Feature status: id=Camera, lastOperation=InstallHooks, success=True, failureCount=0, lastError=none.");
                runtime.Diagnostics.SetFeatureStatus("Camera", "ready", "InstallHooks", true, 0, string.Empty, "Feature status: id=Camera, lastOperation=InstallHooks, success=True, failureCount=0, lastError=none.");
                string report = runtime.ExportLogs();

                IModRegistry registry = GetModRegistry(runtime);
                IDtmDiagnosticsApi? api = registry.GetApi<IDtmDiagnosticsApi>("DTMAPI");
                Assert(api != null, "Runtime should register IDtmDiagnosticsApi under the DTMAPI owner.");

                IDtmDiagnosticsSnapshot diagnosticSnapshot = api!.GetSnapshot();
                Assert(diagnosticSnapshot.LoadedMods.Any(m => m.UniqueID == "DTMAPI.Tests.NeedsDiagnostics" && m.Type == "ContentPack"), "Diagnostics snapshot should expose loaded mod rows without Core DiscoveredMod objects.");
                IDtmModStatusInfo loadedMod = diagnosticSnapshot.Mods.Single(m => m.UniqueID == "DTMAPI.Tests.NeedsDiagnostics");
                Assert(loadedMod.Loaded && loadedMod.Status == "loaded" && loadedMod.StatusCode == "loaded" && loadedMod.Type == "ContentPack", "Diagnostics snapshot should expose loaded mod status rows.");
                Assert(loadedMod.ManifestPath.EndsWith("manifest.json", StringComparison.OrdinalIgnoreCase) && Directory.Exists(loadedMod.RootPath), "Mod status rows should expose manifest and root paths.");
                IDtmModStatusInfo dependencyError = diagnosticSnapshot.Mods.Single(m => m.UniqueID == "DTMAPI.Tests.NeedsMissingDependency");
                Assert(!dependencyError.Loaded && dependencyError.Status == "error" && dependencyError.StatusCode == "missing-dependency" && dependencyError.Reason.Contains("缺少必需依赖"), "Diagnostics snapshot should expose dependency errors without parsing logs.");
                IDtmModStatusInfo entryDllError = diagnosticSnapshot.Mods.Single(m => m.UniqueID == "DTMAPI.Tests.BrokenEntryDll");
                Assert(!entryDllError.Loaded && entryDllError.Status == "error" && entryDllError.StatusCode == "entry-dll-error" && entryDllError.EntryDll == "BrokenEntryDll.txt" && entryDllError.Reason.Contains(".dll"), "Diagnostics snapshot should expose EntryDll errors and manifest entry fields.");
                Assert(diagnosticSnapshot.Mods.Single(m => m.UniqueID == "DTMAPI.Tests.DiagnosticsFutureApi").StatusCode == "api-too-new", "Diagnostics snapshot should expose API version status codes.");
                Assert(diagnosticSnapshot.Mods.Single(m => m.UniqueID == "DTMAPI.Tests.ThrowingEntry").StatusCode == "code-load-error", "Diagnostics snapshot should expose code-load error status codes.");
                Assert(diagnosticSnapshot.Mods.Single(m => m.UniqueID == "DTMAPI.Tests.UnknownStatus").StatusCode == "unknown-error", "Diagnostics snapshot should expose unknown-error status codes for unclassified errors.");
                Assert(diagnosticSnapshot.Warnings.Any(w => w.Owner == "DTMAPI.Tests.NeedsDiagnostics" && w.Message.Contains("MinimumGameVersion")), "Diagnostics snapshot should include structured warnings.");
                Assert(diagnosticSnapshot.HookStatuses.Any(h => h.HookId == "Feature.Camera" && h.Status == "ready"), "Diagnostics snapshot should include hook statuses.");
                Assert(diagnosticSnapshot.FeatureStatuses.Any(f => f.FeatureId == "Camera" && f.Status == "ready" && f.LastOperation == "InstallHooks" && f.Success), "Diagnostics snapshot should include structured feature statuses.");
                Assert(diagnosticSnapshot.Errors.Any(e => e.Owner == "DTMAPI.Tests.NeedsMissingDependency"), "Diagnostics snapshot should include current errors.");
                Assert(diagnosticSnapshot.LatestLogPath == runtime.Diagnostics.GetLatestLogPath() && File.Exists(diagnosticSnapshot.LatestLogPath), "Diagnostics snapshot should expose the latest log path.");
                Assert(diagnosticSnapshot.LatestReportPath == report && File.Exists(diagnosticSnapshot.LatestReportPath), "Diagnostics snapshot should expose the latest report path after export.");

                RuntimeSnapshot runtimeSnapshot = runtime.CreateSnapshot();
                Assert(runtimeSnapshot.FeatureStatuses.Any(f => f.FeatureId == "Camera" && f.LastOperation == "InstallHooks"), "Runtime snapshot should include feature statuses.");
                Assert(runtimeSnapshot.LatestLogPath == diagnosticSnapshot.LatestLogPath, "Runtime snapshot should include latest log path.");
                Assert(runtimeSnapshot.LatestReportPath == diagnosticSnapshot.LatestReportPath, "Runtime snapshot should include latest report path.");

                string summary = ReadZipText(report, "dtmapi-summary.txt");
                Assert(summary.Contains("Features: 1") && summary.Contains("FEATURE Camera: ready") && summary.Contains("LatestLogPath:") && summary.Contains("LatestReportPath:"), "Diagnostic report summary should include feature status and path fields.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void DiagnosticsServiceCapsErrorsWarningsAndSummary()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                runtime.Paths.Ensure();
                File.WriteAllText(
                    Path.Combine(runtime.Paths.DtmApiPath, "install-state.json"),
                    "{ \"DTMAPIVersion\": \"0.5.2-alpha\", \"BinaryVersion\": \"0.5.2.0\", \"LegacyModsMoved\": [ { \"ModId\": \"Old.AutoFishing\" } ], \"LegacyDetections\": [ { \"Kind\": \"legacy-smapi-runtime\" }, { \"Kind\": \"legacy-workshop-cache\" } ] }",
                    new UTF8Encoding(false));
                File.WriteAllText(
                    Path.Combine(runtime.Paths.DtmApiPath, "release-manifest.json"),
                    "{ \"DTMAPIVersion\": \"0.5.2-alpha\", \"BinaryVersion\": \"0.5.2.0\", \"PackageKind\": \"unit-test\" }",
                    new UTF8Encoding(false));
                MethodInfo recordWarning = runtime.Diagnostics.GetType().GetMethod("RecordWarning", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?? throw new InvalidOperationException("DiagnosticsService.RecordWarning should be available for runtime warnings.");

                for (int i = 0; i < 1005; i++)
                {
                    runtime.Diagnostics.RecordError("DTMAPI.Tests.DiagnosticsCap", "error-" + i, "details-" + i);
                    recordWarning.Invoke(runtime.Diagnostics, new object[] { "DTMAPI.Tests.DiagnosticsCap", "warning-" + i, "details-" + i });
                }

                IReadOnlyList<IDtmErrorInfo> errors = runtime.Diagnostics.GetErrors();
                IReadOnlyList<IDtmWarningInfo> warnings = runtime.Diagnostics.GetWarnings();
                Assert(errors.Count == 1000, "Diagnostics errors should be capped at 1000 entries.");
                Assert(warnings.Count == 1000, "Diagnostics warnings should be capped at 1000 entries.");
                Assert(errors[0].Message == "error-5" && errors[999].Message == "error-1004", "Diagnostics errors should drop the oldest entries and keep the latest window.");
                Assert(warnings[0].Message == "warning-5" && warnings[999].Message == "warning-1004", "Diagnostics warnings should drop the oldest entries and keep the latest window.");

                string report = runtime.ExportLogs();
                string summary = ReadZipText(report, "dtmapi-summary.txt");
                Assert(ReadZipText(report, "install-state.json").Contains("Old.AutoFishing"), "Diagnostic report zip should include install-state.json when present.");
                Assert(ReadZipText(report, "release-manifest.json").Contains("unit-test"), "Diagnostic report zip should include release-manifest.json when present.");
                Assert(summary.Contains("Errors: 1000") && summary.Contains("Warnings: 1000"), "Diagnostic report summary should report the retained window counts.");
                Assert(summary.Contains("InstallState: present") && summary.Contains("InstallStateDTMAPIVersion: 0.5.2-alpha") && summary.Contains("InstallStateBinaryVersion: 0.5.2.0") && summary.Contains("InstallStateLegacyMovedCount: 1") && summary.Contains("InstallStateLegacyDetectedCount: 2"), "Diagnostic report summary should include install-state version and legacy counts.");
                Assert(summary.Contains("ReleaseManifest: present") && summary.Contains("ReleaseManifestDTMAPIVersion: 0.5.2-alpha") && summary.Contains("ReleaseManifestBinaryVersion: 0.5.2.0"), "Diagnostic report summary should include release manifest version fields.");
                Assert(summary.Contains("DiagnosticsTrimmed: errors=5, warnings=5, maxPerKind=1000."), "Diagnostic report summary should describe internal trimming when entries are capped.");
                Assert(!summary.Contains("error-0") && summary.Contains("error-1004") && !summary.Contains("warning-0") && summary.Contains("warning-1004"), "Diagnostic report summary should include retained entries, not trimmed oldest entries.");

                string aggregateDir = NewTempGameDir();
                var aggregateRuntime = new DtmApiRuntime(new FakeHost(aggregateDir), new ConfigMenuRegistry());
                for (int i = 0; i < 1005; i++)
                {
                    aggregateRuntime.Diagnostics.RecordError("DTMAPI.Tests.DiagnosticsAggregate", "same-error", "error-details-" + i);
                    recordWarning.Invoke(aggregateRuntime.Diagnostics, new object[] { "DTMAPI.Tests.DiagnosticsAggregate", "same-warning", "warning-details-" + i });
                }

                string aggregateReport = aggregateRuntime.ExportLogs();
                string aggregateSummary = ReadZipText(aggregateReport, "dtmapi-summary.txt");
                Assert(aggregateRuntime.Diagnostics.GetErrors().Count == 1000 && aggregateRuntime.Diagnostics.GetWarnings().Count == 1000, "Diagnostics retained windows should remain capped when aggregate counters keep total counts.");
                Assert(aggregateSummary.Contains("DiagnosticsTrimmed: errors=5, warnings=5, maxPerKind=1000."), "Aggregate diagnostics summary should still include trim summary.");
                Assert(aggregateSummary.Contains("DiagnosticsAggregates: totalKeys=2, top=2."), "Diagnostic report summary should include aggregate counter header.");
                Assert(aggregateSummary.Contains("DIAGNOSTIC-AGGREGATE Error owner=DTMAPI.Tests.DiagnosticsAggregate message=same-error count=1005") && aggregateSummary.Contains("lastDetails=error-details-1004"), "Diagnostic aggregate counters should retain total repeated error count and last details.");
                Assert(aggregateSummary.Contains("DIAGNOSTIC-AGGREGATE Warning owner=DTMAPI.Tests.DiagnosticsAggregate message=same-warning count=1005") && aggregateSummary.Contains("lastDetails=warning-details-1004"), "Diagnostic aggregate counters should retain total repeated warning count and last details.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void ManagerViewModelMapsDiagnosticsSnapshot()
        {
            string latestLogPath = Path.GetTempFileName();
            string readyReportPath = Path.GetTempFileName();
            string longRootPath = Path.Combine(Path.GetTempPath(), "DTMAPI", new string('x', 128), "Mods", "Example");
            string missingReportPath = Path.Combine(Path.GetTempPath(), "dtmapi-manager-missing-report.zip");
            if (File.Exists(missingReportPath))
                File.Delete(missingReportPath);

            try
            {
                var olderError = new DtmErrorInfo("Example.Mod", "older error", "older stack");
                Thread.Sleep(2);
                var newerError = new DtmErrorInfo("Example.Mod", "failed to load asset", "stack trace");
                var olderWarning = new DtmWarningInfo("Example.Optional", "older warning", "older warning details");
                Thread.Sleep(2);
                var newerWarning = new DtmWarningInfo("Example.Optional", "optional dependency version too low", "wanted 1.0");

                var snapshot = new DtmDiagnosticsSnapshot(
                    DateTimeOffset.Now,
                    Array.Empty<IDtmLoadedModInfo>(),
                    new IDtmModStatusInfo[]
                    {
                        new DtmModStatusInfo(
                            "Example.Mod",
                            "Example Mod",
                            "1.2.3",
                            "Code",
                            "Local",
                            "Local.Example.Mod",
                            true,
                            true,
                            "official-enabled",
                            "Example.dll",
                            "ModEntry",
                            true,
                            "loaded",
                            "loaded",
                            "OK",
                            Path.Combine(longRootPath, "manifest.json"),
                            longRootPath),
                        new DtmModStatusInfo(
                            "Blocked.Mod",
                            "Blocked Mod",
                            "1.0.0",
                            "Code",
                            "Workshop",
                            "Workshop.Blocked.Mod",
                            true,
                            true,
                            "official-enabled",
                            "Blocked.dll",
                            "ModEntry",
                            false,
                            "error",
                            "missing-dependency",
                            "Missing dependency",
                            Path.Combine(Path.GetTempPath(), "blocked-manifest.json"),
                            Path.Combine(Path.GetTempPath(), "Blocked")),
                        new DtmModStatusInfo(
                            "Warning.Mod",
                            "Warning Mod",
                            "1.0.0",
                            "Code",
                            "Workshop",
                            "Workshop.Warning.Mod",
                            true,
                            true,
                            "official-enabled",
                            "Warning.dll",
                            "ModEntry",
                            true,
                            "warning",
                            "warning",
                            "Loaded with warning",
                            Path.Combine(Path.GetTempPath(), "warning-manifest.json"),
                            Path.Combine(Path.GetTempPath(), "Warning")),
                        new DtmModStatusInfo(
                            "ReasonOnly.Mod",
                            "Reason Only Mod",
                            "1.0.0",
                            "Code",
                            "Local",
                            "Local.ReasonOnly.Mod",
                            true,
                            true,
                            "official-enabled",
                            "ReasonOnly.dll",
                            "ModEntry",
                            true,
                            "loaded",
                            "loaded",
                            "Reason text mentions warning, blocked, and error but structured status is loaded.",
                            Path.Combine(Path.GetTempPath(), "reason-only-manifest.json"),
                            Path.Combine(Path.GetTempPath(), "ReasonOnly")),
                        new DtmModStatusInfo(
                            "Disabled.Mod",
                            "Disabled Mod",
                            "1.0.0",
                            "Code",
                            "Local",
                            "Local.Disabled.Mod",
                            false,
                            true,
                            "official-disabled",
                            "Disabled.dll",
                            "ModEntry",
                            false,
                            "disabled",
                            "disabled",
                            "Disabled by official mod list",
                            Path.Combine(Path.GetTempPath(), "disabled-manifest.json"),
                            Path.Combine(Path.GetTempPath(), "Disabled"))
                    },
                    new IDtmErrorInfo[]
                    {
                        olderError,
                        newerError
                    },
                    new IDtmWarningInfo[]
                    {
                        olderWarning,
                        newerWarning
                    },
                    new IHookStatusInfo[]
                    {
                        new HookStatusInfo("Fishing.Automation", "experimental", "AgentStateFishingWait.OnPlay", "ready"),
                        new HookStatusInfo("Feature.Camera", "ready", "CameraFeature", "ready"),
                        new HookStatusInfo("Feature.Broken", "failed", "BrokenFeature", "failed"),
                        new HookStatusInfo("Save.MoreSlotsApi", "missing", "SaveSlotsFeature", "missing")
                    },
                    new IDtmFeatureStatusInfo[]
                    {
                        new DtmFeatureStatusInfo("FishingAutomation", "ready", "Update", true, 2, string.Empty, "cumulative=2"),
                        new DtmFeatureStatusInfo("Camera", "ready", "Update", true, 0, string.Empty, "ready"),
                        new DtmFeatureStatusInfo("BrokenFeature", "failed", "Update", false, 3, "boom", "failed")
                    },
                    latestLogPath,
                    missingReportPath);

                DtmManagerViewModel model = DtmManagerViewModelFactory.FromSnapshot(snapshot);

                Assert(model.Mods.Count == 5, "Manager model should map mod status rows.");
                Assert(model.Mods[0].UniqueID == "Blocked.Mod" && model.Mods[1].UniqueID == "Warning.Mod" && model.Mods[2].UniqueID == "Disabled.Mod" && model.Mods[3].UniqueID == "Example.Mod" && model.Mods[4].UniqueID == "ReasonOnly.Mod", "Manager mods should sort blocked, warning, disabled, then loaded rows using structured status.");
                Assert(model.Mods[3].StatusCode == "loaded", "Manager mod row should keep structured status code.");
                Assert(model.Mods[3].RootPath == longRootPath, "Manager mod row should preserve long root paths.");
                ManagerModRow reasonOnlyMod = model.Mods.Single(m => m.UniqueID == "ReasonOnly.Mod");
                Assert(!reasonOnlyMod.IsWarning && !reasonOnlyMod.IsBlocked, "Manager mod row should not infer warning/blocking severity from free-text Reason.");
                Assert(model.Errors.Count == 2 && model.Errors[0].Severity == "Error" && model.Errors[0].Message == "failed to load asset", "Manager model should map diagnostics errors newest first.");
                Assert(model.Warnings.Count == 2 && model.Warnings[0].Severity == "Warning" && model.Warnings[0].Message == "optional dependency version too low", "Manager model should map diagnostics warnings newest first.");
                Assert(model.Hooks.Count == 4 && model.Hooks[0].HookId == "Feature.Broken" && model.Hooks[1].HookId == "Save.MoreSlotsApi", "Manager hooks should sort failed and missing before experimental/ready rows.");
                Assert(model.Hooks[0].IsFailed && !model.Hooks[0].IsMissing && !model.Hooks[1].IsFailed && model.Hooks[1].IsMissing, "Manager hook rows should distinguish failed and missing states.");
                Assert(model.Features.Count == 3 && model.Features[0].FeatureId == "BrokenFeature" && model.Features[1].FeatureId == "FishingAutomation" && model.Features[2].FeatureId == "Camera", "Manager features should sort failed, degraded, then ready rows.");
                Assert(model.Features[1].FailureCount == 2, "Manager feature row should keep cumulative failure count.");
                Assert(model.Summary.LoadedModCount == 3, "Manager summary should count loaded mods.");
                Assert(model.Summary.BlockedModCount == 1, "Manager summary should count blocked mods.");
                Assert(model.Summary.DisabledModCount == 1, "Manager summary should count disabled mods.");
                Assert(model.Summary.ErrorCount == 2 && model.Summary.WarningCount == 2, "Manager summary should count diagnostics rows.");
                Assert(model.Summary.FailedHookCount == 1, "Manager summary should count failed hooks separately from missing hooks.");
                Assert(model.Summary.MissingHookCount == 1, "Manager summary should count missing hooks.");
                Assert(model.Summary.FailedFeatureCount == 1, "Manager summary should count failed features.");
                Assert(model.Summary.DegradedFeatureCount == 1, "Manager summary should count degraded features.");
                Assert(model.Summary.OverallStatus == "failed", "Manager summary should mark blocked/error state as failed.");
                Assert(model.LatestLogPath == latestLogPath, "Manager model should expose latest log path.");
                Assert(model.LatestReportPath == missingReportPath, "Manager model should expose latest report path.");
                Assert(model.ExportReport.HasLatestLogPath && model.ExportReport.LatestLogExists, "Manager export status should detect an existing latest log path.");
                Assert(model.ExportReport.HasLatestReportPath && !model.ExportReport.LatestReportExists, "Manager export status should detect a missing latest report path.");
                Assert(model.ExportReport.Status == "missing-report", "Manager export status should mark a missing report path.");
                Assert(ManagerPageRowFormatter.ModelUnavailable(string.Empty) == "Manager model unavailable.", "Manager formatter should expose the empty-model fallback text.");
                Assert(ManagerPageRowFormatter.ModelUnavailable("IOException: boom").Contains("refresh failed"), "Manager formatter should expose refresh failure text.");
                string blockedModLine = ManagerPageRowFormatter.FormatModRow(model.Mods[0]);
                Assert(blockedModLine.Contains("missing-dependency") && blockedModLine.Contains("Blocked Mod") && blockedModLine.Contains("Blocked.Mod") && blockedModLine.Contains("loaded=false") && blockedModLine.Contains("Missing dependency"), "Manager mod formatter should include status code, name, id, loaded state, and blocked reason.");
                string diagnosticLine = ManagerPageRowFormatter.FormatDiagnosticRow(model.Errors[0]);
                Assert(diagnosticLine.Contains("[Example.Mod]") && diagnosticLine.Contains("failed to load asset"), "Manager diagnostic formatter should include owner and message.");
                string missingHookLine = ManagerPageRowFormatter.FormatHookRow(model.Hooks[1]);
                Assert(missingHookLine.Contains("Save.MoreSlotsApi") && missingHookLine.Contains("missing") && !model.Hooks[1].IsFailed && model.Hooks[1].IsMissing, "Manager hook formatter should keep missing hooks warning-like instead of failed.");
                string failedFeatureLine = ManagerPageRowFormatter.FormatFeatureRow(model.Features[0]);
                string degradedFeatureLine = ManagerPageRowFormatter.FormatFeatureRow(model.Features[1]);
                Assert(failedFeatureLine.Contains("BrokenFeature") && failedFeatureLine.Contains("failures=3") && failedFeatureLine.Contains("boom"), "Manager feature formatter should include failed feature details.");
                Assert(degradedFeatureLine.Contains("FishingAutomation") && degradedFeatureLine.Contains("success=true") && degradedFeatureLine.Contains("failures=2"), "Manager feature formatter should include degraded feature counters.");
                string logsStateLine = ManagerPageRowFormatter.FormatLogsExportState(ManagerLogsPageState.From(null, model));
                Assert(logsStateLine.Contains("not-exported") && logsStateLine.Contains("missing-report"), "Manager logs formatter should expose not-exported and snapshot report status.");
                string statusSummary = ManagerPageRowFormatter.FormatStatusSummary(model, string.Empty, null);
                Assert(statusSummary.Contains("overall=failed") && statusSummary.Contains("mods=loaded:3,blocked:1,disabled:1") && statusSummary.Contains("diagnostics=errors:2,warnings:2") && statusSummary.Contains("hooks=failed:1,missing:1") && statusSummary.Contains("features=failed:1,degraded:1") && statusSummary.Contains("install=missing") && statusSummary.Contains("report=missing-report") && statusSummary.Contains("log=present|") && statusSummary.Contains("reportPath=missing|"), "Manager status summary should include support-loop counters, install state, and report/log state.");
                DtmManagerViewModel installedModel = DtmManagerViewModelFactory.FromSnapshot(
                    snapshot,
                    ManagerInstallStateSummary.Present("0.5.2-alpha", "0.5.2.0", "2026-06-11T00:00:00Z", Path.Combine(Path.GetTempPath(), "install-state.json"), 2, 5, true));
                string installStateLine = ManagerPageRowFormatter.FormatInstallState(installedModel.InstallState);
                Assert(installStateLine.Contains("present") && installStateLine.Contains("version:0.5.2-alpha") && installStateLine.Contains("legacyMoved:2") && installStateLine.Contains("legacyDetected:5") && installStateLine.Contains("uninstall:available"), "Manager install-state formatter should expose install version, legacy counters, and uninstall script availability.");
                Assert(ManagerPageRowFormatter.FormatShowingFirst("Hooks", 17, 64) == "Hooks: showing first 17 of 64", "Manager formatter should expose showing-first row counts.");
                Assert(ManagerPageRowFormatter.FormatShowingFirst("Rows", 99, 3) == "Rows: showing first 3 of 3", "Manager formatter should clamp showing-first counts to total.");

                var warningOnlySnapshot = new DtmDiagnosticsSnapshot(
                    DateTimeOffset.Now,
                    Array.Empty<IDtmLoadedModInfo>(),
                    new IDtmModStatusInfo[]
                    {
                        new DtmModStatusInfo(
                            "WarningOnly.Loaded",
                            "Warning Only Loaded",
                            "1.0.0",
                            "Code",
                            "Local",
                            "Local.WarningOnly.Loaded",
                            true,
                            true,
                            "official-enabled",
                            "Loaded.dll",
                            "ModEntry",
                            true,
                            "loaded",
                            "loaded",
                            "Reason mentions warning but does not change severity.",
                            Path.Combine(Path.GetTempPath(), "warning-only-loaded-manifest.json"),
                            Path.Combine(Path.GetTempPath(), "WarningOnlyLoaded"))
                    },
                    Array.Empty<IDtmErrorInfo>(),
                    Array.Empty<IDtmWarningInfo>(),
                    new IHookStatusInfo[]
                    {
                        new HookStatusInfo("Save.MoreSlotsApi", "missing", "SaveSlotsFeature", "missing")
                    },
                    new IDtmFeatureStatusInfo[]
                    {
                        new DtmFeatureStatusInfo("FishingAutomation", "ready", "Update", true, 4, string.Empty, "cumulative=4")
                    },
                    latestLogPath,
                    readyReportPath);

                DtmManagerViewModel warningOnlyModel = DtmManagerViewModelFactory.FromSnapshot(warningOnlySnapshot);
                Assert(warningOnlyModel.Summary.FailedHookCount == 0 && warningOnlyModel.Summary.MissingHookCount == 1, "Missing hooks should be warning severity but not failed hooks.");
                Assert(warningOnlyModel.Summary.FailedFeatureCount == 0 && warningOnlyModel.Summary.DegradedFeatureCount == 1, "Historical successful feature failures should be degraded but not failed.");
                Assert(warningOnlyModel.Summary.OverallStatus == "warning", "Manager summary should mark missing hooks or degraded features as warning when there are no failed/error states.");
                Assert(!warningOnlyModel.Mods[0].IsWarning && !warningOnlyModel.Mods[0].IsBlocked, "Manager mod severity should ignore free-text Reason when structured status is loaded.");

                var emptyPathSnapshot = new DtmDiagnosticsSnapshot(
                    DateTimeOffset.Now,
                    Array.Empty<IDtmLoadedModInfo>(),
                    Array.Empty<IDtmModStatusInfo>(),
                    Array.Empty<IDtmErrorInfo>(),
                    Array.Empty<IDtmWarningInfo>(),
                    Array.Empty<IHookStatusInfo>(),
                    Array.Empty<IDtmFeatureStatusInfo>(),
                    string.Empty,
                    string.Empty);
                DtmManagerViewModel emptyPathModel = DtmManagerViewModelFactory.FromSnapshot(emptyPathSnapshot);
                string emptyPathSummary = ManagerPageRowFormatter.FormatStatusSummary(emptyPathModel, string.Empty, null);
                Assert(emptyPathSummary.Contains("log=unavailable") && emptyPathSummary.Contains("reportPath=unavailable"), "Manager status summary should tolerate null/empty log and report paths.");
            }
            finally
            {
                File.Delete(latestLogPath);
                File.Delete(readyReportPath);
            }
        }

        private static void ManagerRuntimeProviderRefreshesSnapshotAfterReportExport()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                runtime.Start();

                runtime.UI.OpenDtmApiStatusPage();
                DtmManagerViewModel openedModel = runtime.UI.CurrentManagerModel ?? throw new InvalidOperationException("Opening a DTMAPI manager page should refresh the internal manager view model.");

                int errorCountBeforeRefresh = openedModel.Summary.ErrorCount;
                runtime.Diagnostics.RecordError("DTMAPI.Tests.ManagerRefresh", "manager refresh test error", "details");
                runtime.UI.RefreshDtmManagerModel();
                Assert(runtime.UI.CurrentManagerModel != null && runtime.UI.CurrentManagerModel.Summary.ErrorCount == errorCountBeforeRefresh + 1, "Explicit manager refresh should update summary counters from diagnostics.");
                DtmManagerCopySummaryResult copyFallback = runtime.UI.CopyManagerSummary(_ => throw new InvalidOperationException("simulated clipboard unavailable"));
                Assert(copyFallback.Status == "copy-unavailable" && copyFallback.Text.Contains("overall=") && copyFallback.Text.Contains("report="), "Manager Copy Summary fallback should not throw and should keep the support summary text.");

                string report = runtime.UI.ExportLogs();
                Assert(File.Exists(report), "UI report export should return an existing report path.");
                DtmManagerReportExportResult exportResult = runtime.UI.LastManagerReportExport ?? throw new InvalidOperationException("UI report export should retain the manager export result.");
                Assert(exportResult.Status == "exported", "UI report export should verify the refreshed snapshot report path.");
                Assert(exportResult.SnapshotReportPathMatched, "UI report export should match the exported path to snapshot LatestReportPath.");
                Assert(runtime.UI.CurrentManagerModel != null && runtime.UI.CurrentManagerModel.LatestReportPath == report, "UI report export should refresh the current manager model after export.");
                ManagerLogsPageState exportedLogsState = ManagerLogsPageState.From(exportResult, runtime.UI.CurrentManagerModel);
                Assert(exportedLogsState.ExportStatus == "exported", "Manager Logs state should expose the exported status.");
                Assert(exportedLogsState.ExportedReportPath == report, "Manager Logs state should expose the exported report path.");
                Assert(exportedLogsState.SnapshotLatestReportPath == report, "Manager Logs state should expose the refreshed snapshot report path.");
                Assert(exportedLogsState.PathMatchStatus == "matched" && exportedLogsState.SnapshotReportPathMatched, "Manager Logs state should expose the matched report path state.");

                ManagerLogsPageState notExportedLogsState = ManagerLogsPageState.From(null, runtime.UI.CurrentManagerModel);
                Assert(notExportedLogsState.ExportStatus == "not-exported" && notExportedLogsState.PathMatchStatus == "not-exported", "Manager Logs state should expose not-exported before an export result exists.");

                string latestLogPath = Path.GetTempFileName();
                string exportedPath = Path.Combine(Path.GetTempPath(), "dtmapi-manager-exported.zip");
                string staleSnapshotPath = Path.Combine(Path.GetTempPath(), "dtmapi-manager-stale.zip");
                try
                {
                    File.WriteAllText(exportedPath, "exported");
                    File.WriteAllText(staleSnapshotPath, "stale");
                    var mismatchProvider = new DtmManagerRuntimeModelProvider(
                        new FakeDiagnosticsApi(() => new DtmDiagnosticsSnapshot(
                            DateTimeOffset.Now,
                            Array.Empty<IDtmLoadedModInfo>(),
                            Array.Empty<IDtmModStatusInfo>(),
                            Array.Empty<IDtmErrorInfo>(),
                            Array.Empty<IDtmWarningInfo>(),
                            Array.Empty<IHookStatusInfo>(),
                            Array.Empty<IDtmFeatureStatusInfo>(),
                            latestLogPath,
                            staleSnapshotPath)),
                        () => exportedPath);

                    DtmManagerReportExportResult mismatch = mismatchProvider.ExportReportAndRefresh();
                    Assert(mismatch.Status == "report-path-mismatch", "Manager provider should flag report path mismatch after export and snapshot refresh.");
                    Assert(!mismatch.SnapshotReportPathMatched, "Manager provider mismatch result should expose unmatched report paths.");
                    Assert(mismatch.ExportedReportPath == exportedPath, "Manager provider mismatch result should keep the exported path.");
                    DtmManagerViewModel mismatchModel = mismatch.RefreshedModel ?? throw new InvalidOperationException("Manager provider mismatch result should keep a refreshed snapshot model.");
                    Assert(mismatchModel.LatestReportPath == staleSnapshotPath, "Manager provider mismatch result should keep the refreshed snapshot path.");
                    ManagerLogsPageState mismatchLogsState = ManagerLogsPageState.From(mismatch, null);
                    Assert(mismatchLogsState.ExportStatus == "report-path-mismatch", "Manager Logs state should expose report-path-mismatch export status.");
                    Assert(mismatchLogsState.ExportedReportPath == exportedPath, "Manager Logs state should expose the mismatched exported path.");
                    Assert(mismatchLogsState.SnapshotLatestReportPath == staleSnapshotPath, "Manager Logs state should expose the mismatched snapshot report path.");
                    Assert(mismatchLogsState.PathMatchStatus == "report-path-mismatch" && !mismatchLogsState.SnapshotReportPathMatched, "Manager Logs state should expose path mismatch.");

                    DtmManagerReportExportResult missingExportPath = DtmManagerReportExportResult.FromExport(string.Empty, mismatchModel);
                    ManagerLogsPageState missingExportPathState = ManagerLogsPageState.From(missingExportPath, mismatchModel);
                    Assert(missingExportPathState.ExportStatus == "missing-export-path", "Manager Logs state should expose missing-export-path export status.");
                    Assert(missingExportPathState.ExportedReportPath == string.Empty, "Manager Logs state should keep an empty exported path for missing export path.");
                    Assert(missingExportPathState.SnapshotLatestReportPath == staleSnapshotPath, "Manager Logs state should keep the refreshed snapshot path when export path is missing.");
                    Assert(missingExportPathState.PathMatchStatus == "missing-export-path", "Manager Logs state should expose missing export path as its path-match state.");
                }
                finally
                {
                    File.Delete(latestLogPath);
                    File.Delete(exportedPath);
                    File.Delete(staleSnapshotPath);
                }

                var uiWithoutProvider = new UiRuntimeService(() => string.Empty, _ => { }, _ => { });
                uiWithoutProvider.OpenDtmApiStatusPage();
                uiWithoutProvider.RefreshDtmManagerModel();
                Assert(uiWithoutProvider.CurrentManagerModel == null, "Manager refresh without a provider should leave the model unavailable for UI fallback.");

                bool exportFailureRecorded = false;
                var exportFailureUi = new UiRuntimeService(
                    () => throw new IOException("simulated export failure"),
                    _ => { },
                    _ => { },
                    (owner, message, details) =>
                    {
                        exportFailureRecorded = owner == "DTMAPI.ManagerUI" &&
                            message.IndexOf("ExportLogs", StringComparison.Ordinal) >= 0 &&
                            details.IndexOf("simulated export failure", StringComparison.Ordinal) >= 0;
                    },
                    (_, _) => { });
                exportFailureUi.ManagerModelProvider = new DtmManagerRuntimeModelProvider(
                    new FakeDiagnosticsApi(() => new DtmDiagnosticsSnapshot(
                        DateTimeOffset.Now,
                        Array.Empty<IDtmLoadedModInfo>(),
                        Array.Empty<IDtmModStatusInfo>(),
                        Array.Empty<IDtmErrorInfo>(),
                        Array.Empty<IDtmWarningInfo>(),
                        Array.Empty<IHookStatusInfo>(),
                        Array.Empty<IDtmFeatureStatusInfo>(),
                        string.Empty,
                        string.Empty)),
                    () => throw new IOException("simulated export failure"));
                string failedReport = exportFailureUi.ExportLogs();
                Assert(failedReport == string.Empty, "Manager UI export failure should return an empty path instead of throwing.");
                DtmManagerReportExportResult exportFailureResult = exportFailureUi.LastManagerReportExport ?? throw new InvalidOperationException("Manager UI export failure should retain an export-failed result.");
                Assert(exportFailureResult.Status == "export-failed", "Manager UI export failure should retain an export-failed result.");
                Assert(exportFailureResult.ErrorMessage.IndexOf("simulated export failure", StringComparison.Ordinal) >= 0, "Manager UI export failure should expose the internal error text.");
                ManagerLogsPageState exportFailureLogsState = ManagerLogsPageState.From(exportFailureResult, exportFailureUi.CurrentManagerModel);
                Assert(exportFailureLogsState.ExportStatus == "export-failed", "Manager Logs state should expose export-failed status.");
                Assert(exportFailureLogsState.ExportedReportPath == string.Empty, "Manager Logs state should clear exported path after export failure.");
                Assert(exportFailureLogsState.PathMatchStatus == "export-failed", "Manager Logs state should expose export-failed path-match status.");
                Assert(exportFailureLogsState.ExportErrorMessage.IndexOf("simulated export failure", StringComparison.Ordinal) >= 0, "Manager Logs state should expose export failure error text.");
                Assert(ManagerPageRowFormatter.FormatLogsExportState(exportFailureLogsState).Contains("export-failed"), "Manager Logs formatter should expose export-failed text.");
                Assert(exportFailureRecorded, "Manager UI export failure should record diagnostics with DTMAPI.ManagerUI owner.");

                bool refreshFailureRecorded = false;
                bool throwOnRefresh = false;
                var refreshFailureUi = new UiRuntimeService(
                    () => string.Empty,
                    _ => { },
                    _ => { },
                    (owner, message, details) =>
                    {
                        refreshFailureRecorded = owner == "DTMAPI.ManagerUI" &&
                            message.IndexOf("RefreshDtmManagerModel", StringComparison.Ordinal) >= 0 &&
                            details.IndexOf("simulated snapshot failure", StringComparison.Ordinal) >= 0;
                    },
                    (_, _) => { });
                refreshFailureUi.ManagerModelProvider = new DtmManagerRuntimeModelProvider(
                    new FakeDiagnosticsApi(() =>
                    {
                        if (throwOnRefresh)
                            throw new InvalidOperationException("simulated snapshot failure");

                        return new DtmDiagnosticsSnapshot(
                            DateTimeOffset.Now,
                            Array.Empty<IDtmLoadedModInfo>(),
                            Array.Empty<IDtmModStatusInfo>(),
                            Array.Empty<IDtmErrorInfo>(),
                            Array.Empty<IDtmWarningInfo>(),
                            Array.Empty<IHookStatusInfo>(),
                            Array.Empty<IDtmFeatureStatusInfo>(),
                            string.Empty,
                            string.Empty);
                    }),
                    () => string.Empty);
                refreshFailureUi.RefreshDtmManagerModel();
                DtmManagerViewModel stableModel = refreshFailureUi.CurrentManagerModel ?? throw new InvalidOperationException("Initial manager refresh should create a model.");
                throwOnRefresh = true;
                refreshFailureUi.RefreshDtmManagerModel();
                Assert(refreshFailureUi.CurrentManagerModel == stableModel, "Manager refresh failure should preserve the last known model.");
                Assert(refreshFailureUi.LastManagerRefreshError.IndexOf("simulated snapshot failure", StringComparison.Ordinal) >= 0, "Manager refresh failure should retain an internal refresh error message.");
                Assert(refreshFailureRecorded, "Manager refresh failure should record diagnostics with DTMAPI.ManagerUI owner.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void HelperModRegistryBindsApiRegistrationToOwner()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                string modDir = Path.Combine(dir, "Mods", "ApiOwner");
                Directory.CreateDirectory(modDir);
                string assemblyPath = typeof(ApiOwnerProbeMod).Assembly.Location;
                string assemblyName = Path.GetFileName(assemblyPath);
                File.Copy(assemblyPath, Path.Combine(modDir, assemblyName), overwrite: true);
                File.WriteAllText(
                    Path.Combine(modDir, "manifest.json"),
                    "{ \"Name\": \"API Owner\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.ApiOwner\", \"EntryDll\": \"" + assemblyName + "\", \"EntryType\": \"" + (typeof(ApiOwnerProbeMod).FullName ?? nameof(ApiOwnerProbeMod)) + "\", \"Type\": \"CodeMod\" }");

                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                runtime.Start();
                IModRegistry registry = GetModRegistry(runtime);
                IUnitProbeApi? ownedApi = registry.GetApi<IUnitProbeApi>("DTMAPI.Tests.ApiOwner");
                Assert(ownedApi != null && ownedApi.Owner == "DTMAPI.Tests.ApiOwner", "Helper registry should register APIs under the helper's mod owner.");
                Assert(registry.GetApi<IUnitProbeApi>("DTMAPI.Tests.SpoofedOwner") == null, "Helper registry should not let mods spoof another API owner.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void HighFrequencyEventsDisableHandlersAfterConsecutiveFailures()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                IEventsHelper events = CreateEventsProxy(runtime, "DTMAPI.Tests.ThrowingEvents");
                int updateCalls = 0;
                int secondCalls = 0;
                events.GameLoop.UpdateTicked += (_, _) =>
                {
                    updateCalls++;
                    throw new InvalidOperationException("update failure");
                };
                events.GameLoop.OneSecondUpdateTicked += (_, _) =>
                {
                    secondCalls++;
                    throw new InvalidOperationException("second failure");
                };

                runtime.Start();
                for (int i = 0; i < 5; i++)
                    runtime.Update();
                for (uint i = 0; i < 5; i++)
                    DispatchOneSecondUpdateTicked(runtime, i);

                RuntimeSnapshot snapshot = runtime.CreateSnapshot();
                Assert(updateCalls == 3, "UpdateTicked handler should be disabled after three consecutive failures.");
                Assert(secondCalls == 3, "OneSecondUpdateTicked handler should be disabled after three consecutive failures.");
                Assert(snapshot.Errors.Any(e => e.Owner == "DTMAPI.Tests.ThrowingEvents" && e.Message.Contains("GameLoop.UpdateTicked") && e.Message.Contains("disabled")), "UpdateTicked circuit breaker should record a diagnostic.");
                Assert(snapshot.Errors.Any(e => e.Owner == "DTMAPI.Tests.ThrowingEvents" && e.Message.Contains("GameLoop.OneSecondUpdateTicked") && e.Message.Contains("disabled")), "OneSecondUpdateTicked circuit breaker should record a diagnostic.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void EventRemoveIsOwnerBound()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                IEventsHelper ownerA = CreateEventsProxy(runtime, "DTMAPI.Tests.OwnerA");
                IEventsHelper ownerB = CreateEventsProxy(runtime, "DTMAPI.Tests.OwnerB");
                int updateCalls = 0;
                EventHandler<UpdateTickedEventArgs> sharedHandler = (_, _) => updateCalls++;

                ownerA.GameLoop.UpdateTicked += sharedHandler;
                ownerB.GameLoop.UpdateTicked += sharedHandler;
                ownerA.GameLoop.UpdateTicked -= sharedHandler;

                runtime.Start();
                runtime.Update();
                Assert(updateCalls == 1, "Owner A removing a shared delegate must not remove Owner B's event subscription.");

                ownerB.GameLoop.UpdateTicked -= sharedHandler;
                runtime.Update();
                Assert(updateCalls == 1, "Owner B removing its own delegate should remove the remaining subscription.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void BadConfigJsonIsBackedUpAndDefaultedWithTempFileWrites()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                runtime.Start();
                IConfigHelper config = GetConfig(runtime);
                IManifest manifest = new ManifestModel
                {
                    Name = "Bad Config",
                    Author = "DTMAPI",
                    Version = "1.0.0",
                    UniqueID = "DTMAPI.Tests.BadConfig"
                };
                string path = config.GetConfigPath(manifest);
                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
                File.WriteAllText(path, "{ this is not valid json");

                SampleConfig restored = config.ReadConfig<SampleConfig>(manifest);
                Assert(restored.Enabled && restored.Count == 7, "Bad config JSON should restore default config values.");
                Assert(Directory.GetFiles(Path.GetDirectoryName(path) ?? ".", Path.GetFileName(path) + ".invalid-*.bak").Length == 1, "Bad config JSON should be backed up.");
                Assert(runtime.CreateSnapshot().Errors.Any(e => e.Owner == manifest.UniqueID && e.Message.Contains("配置 JSON 损坏")), "Bad config JSON should be diagnosed.");

                restored.Count = 11;
                config.WriteConfig(manifest, restored);
                Assert(config.ReadConfig<SampleConfig>(manifest).Count == 11, "Config writes should round-trip after recovery.");
                Assert(Directory.GetFiles(Path.GetDirectoryName(path) ?? ".", "*.tmp").Length == 0, "Config writes should not leave temp files behind.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void OffThreadTimerFallbackUpdateDoesNotDispatchOrdinaryModUpdates()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                IEventsHelper events = CreateEventsProxy(runtime, "DTMAPI.Tests.TimerFallback");
                int updates = 0;
                events.GameLoop.UpdateTicked += (_, _) => updates++;
                runtime.Start();

                Exception? threadError = null;
                var thread = new Thread(() =>
                {
                    try
                    {
                        runtime.Update();
                    }
                    catch (Exception ex)
                    {
                        threadError = ex;
                    }
                });
                thread.Start();
                thread.Join();
                if (threadError != null)
                    throw threadError;
                Assert(updates == 0, "Off-thread TimerFallback update must not dispatch ordinary UpdateTicked callbacks.");

                runtime.Update();
                Assert(updates == 1, "Runtime-thread update should still dispatch ordinary UpdateTicked callbacks.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void ConfigMenuEditsSaveCancelAndDetectConflicts()
        {
            var menu = new ConfigMenuRegistry();
            IConfigMenuRuntime menuRuntime = menu;
            IManifest manifest = new ManifestModel
            {
                Name = "Menu Test",
                Author = "DTMAPI",
                Version = "1.0.0",
                UniqueID = "DTMAPI.Tests.Menu"
            };
            bool enabled = false;
            double multiplier = 1.0;
            string label = "before";
            string choice = "Safe";
            string keybind = "F10";
            string alternateKeybind = "F9";
            int saved = 0;
            int reset = 0;

            menu.Register(manifest, () =>
            {
                reset++;
                enabled = false;
                multiplier = 1.0;
                label = "default";
                choice = "Safe";
                keybind = "F10";
            }, () => saved++);
            menu.AddBoolOption(manifest, () => "Enabled", () => "", () => enabled, value => enabled = value);
            menu.AddNumberOption(manifest, () => "Multiplier", () => "", () => multiplier, value => multiplier = value, 1, 4, 0.5);
            menu.AddTextOption(manifest, () => "Label", () => "", () => label, value => label = value);
            menu.AddChoiceOption(manifest, () => "Choice", () => "", () => choice, value => choice = value, new[] { "Safe", "Fast" });
            menu.AddKeybindOption(manifest, () => "Toggle", () => "", () => keybind, value => keybind = value);
            menu.AddKeybindOption(manifest, () => "Alternate", () => "", () => alternateKeybind, value => alternateKeybind = value);

            IConfigMenuPage page = menuRuntime.GetPage(manifest.UniqueID) ?? throw new InvalidOperationException("Page should exist.");
            menuRuntime.BeginEditing(manifest.UniqueID);
            Assert(page.Items[0].TrySetPendingValue("true", out _), "Bool option should accept true.");
            Assert(page.Items[1].TrySetPendingValue("3.25", out _), "Number option should accept numeric input.");
            Assert(page.Items[2].TrySetPendingValue("after", out _), "Text option should accept text input.");
            Assert(page.Items[3].TrySetPendingValue("Fast", out _), "Choice option should accept known choices.");
            Assert(page.HasPendingChanges, "Pending edits should be tracked.");

            menuRuntime.Cancel(manifest.UniqueID);
            Assert(!enabled && multiplier == 1.0 && label == "before" && choice == "Safe", "Cancel should discard pending edits.");

            menuRuntime.BeginEditing(manifest.UniqueID);
            page.Items[0].TrySetPendingValue("true", out _);
            page.Items[1].TrySetPendingValue("3.25", out _);
            page.Items[2].TrySetPendingValue("after", out _);
            page.Items[3].TrySetPendingValue("Fast", out _);
            menuRuntime.Save(manifest.UniqueID);
            Assert(saved == 1, "Save callback should run.");
            Assert(enabled && Math.Abs(multiplier - 3.5) < 0.001 && label == "after" && choice == "Fast", "Save should apply pending values.");

            menuRuntime.Reset(manifest.UniqueID);
            Assert(reset == 1, "Reset callback should run.");
            Assert(page.HasPendingChanges, "Reset should remain pending until save.");
            menuRuntime.Cancel(manifest.UniqueID);
            Assert(enabled && Math.Abs(multiplier - 3.5) < 0.001 && label == "after" && choice == "Fast", "Cancel after reset should restore committed values.");

            menuRuntime.BeginEditing(manifest.UniqueID);
            Assert(page.Items[4].TrySetPendingValue("F9", out _), "Keybind option should accept captured keys.");
            string conflict = menu.GetKeybindConflicts(manifest.UniqueID).Single();
            Assert(conflict.StartsWith("按键冲突：F9", StringComparison.Ordinal), "Duplicate keybinds should be reported with Chinese-first conflict text.");
            bool conflictBlocked = false;
            try
            {
                menuRuntime.Save(manifest.UniqueID);
            }
            catch (InvalidOperationException)
            {
                conflictBlocked = true;
            }
            Assert(conflictBlocked, "Save should block keybind conflicts.");
        }

        private static void ConfigMenuCallbackFailuresRollbackAndStayInspectable()
        {
            var menu = new ConfigMenuRegistry();
            IConfigMenuRuntime menuRuntime = menu;
            IManifest manifest = new ManifestModel
            {
                Name = "Menu Failure Test",
                Author = "DTMAPI",
                Version = "1.0.0",
                UniqueID = "DTMAPI.Tests.MenuFailure"
            };

            bool first = false;
            bool second = false;
            bool failSecondSetter = true;
            bool failSave = false;
            int saveCalls = 0;
            menu.Register(manifest, () => { }, () =>
            {
                saveCalls++;
                if (failSave)
                    throw new InvalidOperationException("save-boom");
            });
            menu.AddBoolOption(manifest, () => "First", () => "", () => first, value => first = value);
            menu.AddBoolOption(manifest, () => "Second", () => "", () => second, value =>
            {
                if (failSecondSetter && value)
                    throw new InvalidOperationException("setter-boom");
                second = value;
            });

            IConfigMenuPage page = menuRuntime.GetPage(manifest.UniqueID) ?? throw new InvalidOperationException("Page should exist.");
            menuRuntime.BeginEditing(manifest.UniqueID);
            Assert(page.Items[0].TrySetPendingValue("true", out _), "First option should accept true.");
            Assert(page.Items[1].TrySetPendingValue("true", out _), "Second option should accept true.");
            AssertThrows(() => menuRuntime.Save(manifest.UniqueID), "Setter failure should reject save.");
            Assert(!first && !second && saveCalls == 0, "Setter failure should roll back prior applied values and skip save callback.");
            Assert(page.Items[1].ValidationError.Contains("setter-boom"), "Failing setter should remain visible on the config item.");
            Assert(page.HasPendingChanges, "Failed save should keep pending edits inspectable.");

            failSecondSetter = false;
            failSave = true;
            AssertThrows(() => menuRuntime.Save(manifest.UniqueID), "Save callback failure should reject save.");
            Assert(!first && !second && saveCalls == 1, "Save callback failure should roll back applied pending values.");
            Assert(page.HasPendingChanges, "Failed save callback should keep pending edits inspectable.");

            failSave = false;
            menuRuntime.Save(manifest.UniqueID);
            Assert(first && second && saveCalls == 2 && !page.HasPendingChanges, "Successful retry should apply and commit retained pending values.");

            var previewMenu = new ConfigMenuRegistry();
            IConfigMenuRuntime previewRuntime = previewMenu;
            IManifest previewManifest = new ManifestModel
            {
                Name = "Preview Failure Test",
                Author = "DTMAPI",
                Version = "1.0.0",
                UniqueID = "DTMAPI.Tests.PreviewFailure"
            };
            bool previewEnabled = false;
            previewMenu.Register(previewManifest, () => { }, () => { });
            previewMenu.AddBoolOption(previewManifest, () => "Preview", () => "", () => previewEnabled, value =>
            {
                if (value)
                    throw new InvalidOperationException("preview-boom");
                previewEnabled = value;
            });

            IConfigMenuPage previewPage = previewRuntime.GetPage(previewManifest.UniqueID) ?? throw new InvalidOperationException("Preview page should exist.");
            previewRuntime.BeginEditing(previewManifest.UniqueID);
            Assert(previewPage.Items[0].TrySetPendingValue("true", out _), "Preview option should accept pending true.");
            using (previewRuntime.PreviewPendingValues(previewPage) ?? throw new InvalidOperationException("Preview scope should be returned even after callback failure."))
            {
                Assert(!previewEnabled, "Failed preview setter should be rolled back immediately.");
            }
            Assert(!previewEnabled, "Preview dispose should leave the original value intact.");
            Assert(previewPage.Items[0].ValidationError.Contains("preview-boom"), "Preview failure should be visible on the config item.");

            var resetMenu = new ConfigMenuRegistry();
            IConfigMenuRuntime resetRuntime = resetMenu;
            IManifest resetManifest = new ManifestModel
            {
                Name = "Reset Failure Test",
                Author = "DTMAPI",
                Version = "1.0.0",
                UniqueID = "DTMAPI.Tests.ResetFailure"
            };
            bool resetValue = true;
            bool failReset = true;
            resetMenu.Register(resetManifest, () =>
            {
                if (failReset)
                    throw new InvalidOperationException("reset-boom");
                resetValue = false;
            }, () => { });
            resetMenu.AddBoolOption(resetManifest, () => "Reset", () => "", () => resetValue, value => resetValue = value);
            IConfigMenuPage resetPage = resetRuntime.GetPage(resetManifest.UniqueID) ?? throw new InvalidOperationException("Reset page should exist.");
            resetRuntime.BeginEditing(resetManifest.UniqueID);
            AssertThrows(() => resetRuntime.Reset(resetManifest.UniqueID), "Reset callback failure should be reported.");
            Assert(resetValue && resetPage.IsEditing && !resetPage.HasPendingChanges, "Reset callback failure should keep the committed value and editing state.");

            var resetGetterMenu = new ConfigMenuRegistry();
            IConfigMenuRuntime resetGetterRuntime = resetGetterMenu;
            IManifest resetGetterManifest = new ManifestModel
            {
                Name = "Reset Getter Failure Test",
                Author = "DTMAPI",
                Version = "1.0.0",
                UniqueID = "DTMAPI.Tests.ResetGetterFailure"
            };
            bool resetFirst = true;
            bool resetSecond = true;
            bool failResetGetter = false;
            resetGetterMenu.Register(resetGetterManifest, () =>
            {
                resetFirst = false;
                resetSecond = false;
                failResetGetter = true;
            }, () => { });
            resetGetterMenu.AddBoolOption(resetGetterManifest, () => "First", () => "", () => resetFirst, value => resetFirst = value);
            resetGetterMenu.AddBoolOption(resetGetterManifest, () => "Second", () => "", () =>
            {
                if (failResetGetter)
                    throw new InvalidOperationException("reset-getter-boom");
                return resetSecond;
            }, value => resetSecond = value);
            IConfigMenuPage resetGetterPage = resetGetterRuntime.GetPage(resetGetterManifest.UniqueID) ?? throw new InvalidOperationException("Reset getter page should exist.");
            resetGetterRuntime.BeginEditing(resetGetterManifest.UniqueID);
            AssertThrows(() => resetGetterRuntime.Reset(resetGetterManifest.UniqueID), "Reset getter failure should be reported.");
            failResetGetter = false;
            Assert(resetFirst && resetSecond, "Reset getter failure should roll back true config values.");
            Assert(resetGetterPage.IsEditing && !resetGetterPage.HasPendingChanges, "Reset getter failure should not leave partially refreshed pending values.");

            var cancelMenu = new ConfigMenuRegistry();
            IConfigMenuRuntime cancelRuntime = cancelMenu;
            IManifest cancelManifest = new ManifestModel
            {
                Name = "Cancel Failure Test",
                Author = "DTMAPI",
                Version = "1.0.0",
                UniqueID = "DTMAPI.Tests.CancelFailure"
            };
            bool cancelValue = false;
            bool failCancelRestore = false;
            cancelMenu.Register(cancelManifest, () => { }, () => { });
            cancelMenu.AddBoolOption(cancelManifest, () => "Cancel", () => "", () => cancelValue, value =>
            {
                if (failCancelRestore && !value)
                    throw new InvalidOperationException("cancel-boom");
                cancelValue = value;
            });
            IConfigMenuPage cancelPage = cancelRuntime.GetPage(cancelManifest.UniqueID) ?? throw new InvalidOperationException("Cancel page should exist.");
            cancelRuntime.BeginEditing(cancelManifest.UniqueID);
            Assert(cancelPage.Items[0].TrySetPendingValue("true", out _), "Cancel option should accept pending true.");
            cancelValue = true;
            failCancelRestore = true;
            AssertThrows(() => cancelRuntime.Cancel(cancelManifest.UniqueID), "Cancel restore failure should be reported.");
            Assert(cancelValue && cancelPage.IsEditing && cancelPage.HasPendingChanges, "Cancel restore failure should keep the page editing so the failed pending state remains visible.");
            Assert(cancelPage.Items[0].ValidationError.Contains("cancel-boom"), "Cancel restore failure should stay visible on the item.");

            var buttonMenu = new ConfigMenuRegistry();
            IConfigMenuRuntime buttonRuntime = buttonMenu;
            IManifest buttonManifest = new ManifestModel
            {
                Name = "Button Failure Test",
                Author = "DTMAPI",
                Version = "1.0.0",
                UniqueID = "DTMAPI.Tests.ButtonFailure"
            };
            buttonMenu.Register(buttonManifest, () => { }, () => { });
            buttonMenu.AddButton(buttonManifest, () => "Danger", () => "", () => throw new InvalidOperationException("button-boom"));
            IConfigMenuPage buttonPage = buttonRuntime.GetPage(buttonManifest.UniqueID) ?? throw new InvalidOperationException("Button page should exist.");
            AssertThrows(buttonPage.Items.Single(item => item.Kind == "Button").Invoke, "Button callback failure should be wrapped for the UI action guard.");
        }

        private static void ConfigMenuPendingPreviewDrivesConditionalVisibility()
        {
            var menu = new ConfigMenuRegistry();
            IConfigMenuRuntime menuRuntime = menu;
            IManifest manifest = new ManifestModel
            {
                Name = "Conditional Menu Test",
                Author = "DTMAPI",
                Version = "1.0.0",
                UniqueID = "DTMAPI.Tests.ConditionalMenu"
            };
            string colorPreset = "Orange";
            string hex = "FF942E";

            menu.Register(manifest, () =>
            {
                colorPreset = "Orange";
                hex = "FF942E";
            }, () => { });
            menu.AddColorPresetOption(
                manifest,
                () => "Color",
                () => "",
                () => colorPreset,
                value => colorPreset = value,
                new[]
                {
                    new DtmColorPreset("Orange", "Orange", "FF942E"),
                    new DtmColorPreset("Custom", "Custom", hex)
                });
            Func<bool> customSelected = () => colorPreset.Equals("Custom", StringComparison.OrdinalIgnoreCase);
            menu.AddTextOption(manifest, () => "Hex", () => "", () => hex, value => hex = value, customSelected, customSelected);

            IConfigMenuPage page = menuRuntime.GetPage(manifest.UniqueID) ?? throw new InvalidOperationException("Page should exist.");
            menuRuntime.BeginEditing(manifest.UniqueID);
            Assert(page.Items.Count(item => item.Kind == "Text") == 0, "Non-custom color should hide the custom text input.");
            IConfigMenuItem colorItem = page.Items.Single(item => item.Kind == "ColorPreset");
            Assert(colorItem.TrySetPendingValue("Custom", out _), "Custom color preset should be selectable.");
            Assert(colorPreset == "Orange", "Pending edits should not permanently apply before save.");

            using (menuRuntime.PreviewPendingValues(page) ?? throw new InvalidOperationException("Pending preview should be available."))
            {
                Assert(colorPreset == "Custom", "Pending preview should temporarily expose the selected custom preset.");
                IConfigMenuItem textItem = page.Items.Single(item => item.Kind == "Text");
                Assert(textItem.CanEdit, "Custom color text input should be editable inside pending preview.");
            }

            Assert(colorPreset == "Orange", "Pending preview should restore the committed color after rendering.");
        }

        private static void DisabledDiscoveredModLocksConfigPage()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
            string dir = NewTempGameDir();
            string modDir = Path.Combine(dir, "Mods", "Disabled");
            Directory.CreateDirectory(modDir);
            File.WriteAllText(Path.Combine(modDir, "manifest.json"), "{ \"Name\": \"Disabled\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.Disabled\", \"EntryDll\": \"Disabled.dll\" }");
            File.WriteAllText(Path.Combine(modDir, "dtmapi.disabled"), "disabled by official path");

            var menu = new ConfigMenuRegistry();
            IConfigMenuRuntime menuRuntime = menu;
            IManifest manifest = new ManifestModel
            {
                Name = "Disabled",
                Author = "DTMAPI",
                Version = "1.0.0",
                UniqueID = "DTMAPI.Tests.Disabled"
            };
            bool enabled = false;
            int saved = 0;
            int reset = 0;
            menu.Register(manifest, () => reset++, () => saved++);
            menu.AddBoolOption(manifest, () => "Enabled", () => "", () => enabled, value => enabled = value);

            var runtime = new DtmApiRuntime(new FakeHost(dir), menu);
            runtime.Start();

            IConfigMenuPage page = menuRuntime.GetPage(manifest.UniqueID) ?? throw new InvalidOperationException("Disabled page should exist.");
            Assert(page.IsLocked, "Disabled discovered mod should lock its config page.");
            Assert(page.LockReason.IndexOf("本地 DTMAPI 禁用标记", StringComparison.OrdinalIgnoreCase) >= 0, "Disabled lock should explain the local marker with Chinese-first text.");

            menuRuntime.BeginEditing(manifest.UniqueID);
            Assert(page.Items[0].TrySetPendingValue("true", out _), "Locked page may stage text but must not apply it.");
            AssertThrows(() => menuRuntime.Save(manifest.UniqueID), "Locked page save should be rejected.");
            AssertThrows(() => menuRuntime.Reset(manifest.UniqueID), "Locked page reset should be rejected.");
            Assert(!enabled && saved == 0 && reset == 0, "Locked page actions must not mutate config.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void OfficialLocalModPackagesRespectOfficialEnablement()
        {
            string gameDir = NewTempGameDir();
            string persistentRoot = Path.Combine(Path.GetTempPath(), "DTMAPI-tests-persistent", Guid.NewGuid().ToString("N"));
            string packageRoot = Path.Combine(persistentRoot, "MODS", "Yuuka_DTMAPI_Test");
            string contentRoot = Path.Combine(packageRoot, "Content", "DTMAPI");
            Directory.CreateDirectory(contentRoot);
            File.WriteAllText(
                Path.Combine(contentRoot, "manifest.json"),
                "{ \"Name\": \"Official Test\", \"Author\": \"Yuuka\", \"Version\": \"1.0.0\", \"UniqueID\": \"Yuuka.DTMAPI.Test\", \"Type\": \"ContentPack\" }",
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            File.WriteAllText(
                Path.Combine(contentRoot, "item_tbitem.json"),
                "[{ \"id\": \"dtmapi_test_item\", \"sub_type\": \"material\", \"title\": { \"text\": \"测试物品\", \"english\": \"Test Item\" }, \"ui_sprite_asset\": { \"url\": \"icon_item_dtmapi_test_item\" } }]");

            string localDuplicate = Path.Combine(gameDir, "Mods", "Yuuka.DTMAPI.Test");
            Directory.CreateDirectory(localDuplicate);
            File.WriteAllText(
                Path.Combine(localDuplicate, "manifest.json"),
                "{ \"Name\": \"Local Duplicate\", \"Author\": \"Yuuka\", \"Version\": \"1.0.0\", \"UniqueID\": \"Yuuka.DTMAPI.Test\", \"Type\": \"ContentPack\" }");

            string? previousRoot = Environment.GetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT");
            try
            {
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", persistentRoot);
                WriteOfficialModInfos(persistentRoot, "Local.Yuuka_DTMAPI_Test", false);
                var disabledRuntime = new DtmApiRuntime(new FakeHost(gameDir), new ConfigMenuRegistry());
                disabledRuntime.Start();
                RuntimeSnapshot disabledSnapshot = disabledRuntime.CreateSnapshot();
                DiscoveredMod disabled = disabledSnapshot.DiscoveredMods.Single(m => m.Manifest.UniqueID == "Yuuka.DTMAPI.Test");
                Assert(disabled.Source == "OfficialLocal", "Official local package should take precedence over duplicate game Mods entries.");
                Assert(!disabled.OfficialEnabled, "Official disabled state should prevent loading.");
                Assert(!disabledSnapshot.LoadedMods.Any(m => m.Manifest.UniqueID == "Yuuka.DTMAPI.Test"), "Official-disabled package must not load.");
                Assert(disabledRuntime.GetIndexedContentItem("dtmapi_test_item") == null, "Default content item lookup should not return disabled official content.");
                IContentItemInfo? disabledAnyItem = disabledRuntime.GetAnyIndexedContentItem("dtmapi_test_item");
                Assert(disabledAnyItem != null && !disabledAnyItem.Enabled, "All-content item lookup should expose disabled content for diagnostics.");
                Assert(disabledRuntime.GetAllIndexedContentItems().Any(i => i.ItemId == "dtmapi_test_item" && !i.Enabled), "All-content list should include disabled official content.");
                Assert(!disabledRuntime.GetIndexedContentItems().Any(i => i.ItemId == "dtmapi_test_item"), "Default content list should include enabled content only.");
                IContentQueryHelper disabledContent = GetContent(disabledRuntime);
                Assert(!disabledContent.FindAssets("json").Any(a => a.RelativePath.EndsWith("item_tbitem.json", StringComparison.OrdinalIgnoreCase)), "Default content asset lookup should not expose disabled official package files.");
                Assert(!disabledContent.TryReadTextAsset(Path.Combine("Content", "DTMAPI", "item_tbitem.json"), out _), "Default text asset lookup should not read disabled official package files.");
                Assert(disabledRuntime.Diagnostics.GetWarnings().Any(w => w.Owner == "DTMAPI.ModScanner" && w.Details.Contains("Duplicate UniqueID Yuuka.DTMAPI.Test", StringComparison.OrdinalIgnoreCase) && w.Details.Contains("OfficialLocal", StringComparison.OrdinalIgnoreCase) && w.Details.Contains("Local", StringComparison.OrdinalIgnoreCase)), "Duplicate UniqueID selection should be exposed as a scanner warning.");
                IDtmModStatusInfo disabledStatus = disabledRuntime.CreateDiagnosticsSnapshot().Mods.Single(m => m.UniqueID == "Yuuka.DTMAPI.Test");
                Assert(!disabledStatus.Loaded && disabledStatus.Status == "disabled" && disabledStatus.StatusCode == "disabled" && !disabledStatus.OfficialEnabled && disabledStatus.OfficialEnablementManaged && disabledStatus.EnablementReason.Contains("官方"), "Diagnostics snapshot should expose official disabled mod status and enablement reason.");

                WriteOfficialModInfos(persistentRoot, "Local.Yuuka_DTMAPI_Test", true);
                var enabledRuntime = new DtmApiRuntime(new FakeHost(gameDir), new ConfigMenuRegistry());
                enabledRuntime.Start();
                RuntimeSnapshot enabledSnapshot = enabledRuntime.CreateSnapshot();
                Assert(enabledSnapshot.DiscoveredMods.Single(m => m.Manifest.UniqueID == "Yuuka.DTMAPI.Test").OfficialEnabled, "Official enabled state should be honored.");
                Assert(enabledSnapshot.LoadedMods.Any(m => m.Manifest.UniqueID == "Yuuka.DTMAPI.Test"), "Official-enabled content package should load/index.");
                IContentItemInfo? enabledItem = enabledRuntime.GetIndexedContentItem("dtmapi_test_item");
                Assert(enabledItem != null && enabledItem.Enabled && enabledItem.SourceKind == "DTMAPI" && enabledItem.SourceId == "Local.Yuuka_DTMAPI_Test", "Default content item lookup should expose enabled DTMAPI official-local content.");
                IContentQueryHelper enabledContent = GetContent(enabledRuntime);
                Assert(enabledContent.FindAssets("json").Any(a => a.RelativePath.EndsWith("item_tbitem.json", StringComparison.OrdinalIgnoreCase)), "Default content asset lookup should expose enabled official package files.");
                Assert(enabledContent.TryReadTextAsset(Path.Combine("Content", "DTMAPI", "item_tbitem.json"), out string enabledText) && enabledText.Contains("dtmapi_test_item", StringComparison.OrdinalIgnoreCase), "Default text asset lookup should read enabled official package files.");
                IDtmModStatusInfo enabledStatus = enabledRuntime.CreateDiagnosticsSnapshot().Mods.Single(m => m.UniqueID == "Yuuka.DTMAPI.Test");
                Assert(enabledStatus.Loaded && enabledStatus.Status == "loaded" && enabledStatus.StatusCode == "loaded" && enabledStatus.Source == "OfficialLocal", "Diagnostics snapshot should expose official loaded mod status.");

                File.Delete(Path.Combine(persistentRoot, "SAVE", "mod_infos.json"));
                var unknownRuntime = new DtmApiRuntime(new FakeHost(gameDir), new ConfigMenuRegistry());
                unknownRuntime.Start();
                DiscoveredMod unknown = unknownRuntime.CreateSnapshot().DiscoveredMods.Single(m => m.Manifest.UniqueID == "Yuuka.DTMAPI.Test");
                Assert(!unknown.OfficialEnabled, "Missing official enablement data should not default to enabled.");
                Assert(unknown.EnablementReason.IndexOf("官方启用状态文件", StringComparison.OrdinalIgnoreCase) >= 0, "Missing official state should have a clear Chinese-first reason.");
            }
            finally
            {
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", previousRoot);
            }
        }

        private static void WorkshopReloadHotLoadsNewlyEnabledCodeModOnceAndLocksDisabledLoadedMod()
        {
            string gameDir = NewTempGameDir();
            string persistentRoot = Path.Combine(Path.GetTempPath(), "DTMAPI-tests-persistent", Guid.NewGuid().ToString("N"));
            string packageRoot = Path.Combine(persistentRoot, "MODS", "Yuuka_DTMAPI_HotLoad");
            string contentRoot = Path.Combine(packageRoot, "Content", "DTMAPI");
            Directory.CreateDirectory(contentRoot);

            string assemblyPath = typeof(HotLoadProbeMod).Assembly.Location;
            string assemblyName = Path.GetFileName(assemblyPath);
            File.Copy(assemblyPath, Path.Combine(contentRoot, assemblyName), overwrite: true);
            File.WriteAllText(
                Path.Combine(contentRoot, "manifest.json"),
                "{ \"Name\": \"Hot Load Test\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.HotLoad\", \"EntryDll\": \"Content/DTMAPI/" + assemblyName + "\", \"EntryType\": \"" + (typeof(HotLoadProbeMod).FullName ?? nameof(HotLoadProbeMod)) + "\", \"Type\": \"CodeMod\" }");

            string? previousRoot = Environment.GetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT");
            try
            {
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", persistentRoot);
                WriteOfficialModInfos(persistentRoot, "Local.Yuuka_DTMAPI_HotLoad", false);

                var menu = new ConfigMenuRegistry();
                IConfigMenuRuntime menuRuntime = menu;
                var runtime = new DtmApiRuntime(new FakeHost(gameDir), menu);
                runtime.Start();
                Assert(!runtime.CreateSnapshot().LoadedMods.Any(m => m.Manifest.UniqueID == "DTMAPI.Tests.HotLoad"), "Disabled official package should not load at startup.");

                WriteOfficialModInfos(persistentRoot, "Local.Yuuka_DTMAPI_HotLoad", true);
                runtime.NotifyWorkshopModListChanged();
                RuntimeSnapshot loadedSnapshot = runtime.CreateSnapshot();
                Assert(loadedSnapshot.LoadedMods.Count(m => m.Manifest.UniqueID == "DTMAPI.Tests.HotLoad") == 1, "Official reload should hot-load a newly enabled code mod once.");
                Assert(ReadHotLoadEntryCount(gameDir) == 1, "Hot-loaded code mod Entry should run exactly once.");
                IConfigMenuPage loadedPage = menuRuntime.GetPage("DTMAPI.Tests.HotLoad") ?? throw new InvalidOperationException("Hot-loaded mod should register a config page.");
                Assert(!loadedPage.IsLocked, "Hot-loaded enabled page should be editable.");

                runtime.NotifyWorkshopModListChanged();
                Assert(runtime.CreateSnapshot().LoadedMods.Count(m => m.Manifest.UniqueID == "DTMAPI.Tests.HotLoad") == 1, "Repeated official reload must not duplicate loaded mods.");
                Assert(ReadHotLoadEntryCount(gameDir) == 1, "Repeated official reload must not re-run Entry for an already loaded mod.");

                WriteOfficialModInfos(persistentRoot, "Local.Yuuka_DTMAPI_HotLoad", false);
                runtime.NotifyWorkshopModListChanged();
                RuntimeSnapshot disabledAfterLoad = runtime.CreateSnapshot();
                Assert(disabledAfterLoad.LoadedMods.Any(m => m.Manifest.UniqueID == "DTMAPI.Tests.HotLoad"), "Runtime should not attempt to unload a DLL after official disable.");
                IConfigMenuPage lockedPage = menuRuntime.GetPage("DTMAPI.Tests.HotLoad") ?? throw new InvalidOperationException("Loaded disabled page should still exist.");
                Assert(lockedPage.IsLocked, "Loaded disabled page should be locked until restart.");
                Assert(lockedPage.LockReason.IndexOf("重启", StringComparison.OrdinalIgnoreCase) >= 0, "Loaded disabled page should explain restart is required with Chinese-first text.");
            }
            finally
            {
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", previousRoot);
            }
        }

        private static void RuntimeUiBoundariesBlockGameplayHotkeysAndModUpdates()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
            string dir = NewTempGameDir();
            var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
            IEventsHelper events = CreateEventsProxy(runtime, "DTMAPI.Tests.Events");
            int pressed = 0;
            int released = 0;
            int updates = 0;
            int menuOpened = 0;
            int menuClosed = 0;
            events.Input.ButtonPressed += (_, _) => pressed++;
            events.Input.ButtonReleased += (_, _) => released++;
            events.GameLoop.UpdateTicked += (_, _) => updates++;
            events.UI.MenuOpened += (_, _) => menuOpened++;
            events.UI.MenuClosed += (_, _) => menuClosed++;

            runtime.Start();
            runtime.UI.SetUiContext("Gameplay", canDrawOverlay: true, gameplayHotkeysAllowed: true, reason: "unit test");
            runtime.RecordInputPressed("F10");
            runtime.RecordInputReleased("F10");
            runtime.Update();
            Assert(pressed == 1 && released == 1 && updates == 1, $"Gameplay context should deliver input and update events. pressed={pressed} released={released} updates={updates} open={runtime.UI.IsOpen} context={runtime.UI.InputContext}");

            runtime.UI.OpenConfigPage("DTMAPI.Tests.Events");
            Assert(menuOpened == 1 && runtime.UI.BlocksGameplayHotkeys && runtime.UI.BlocksModUpdates, "Open DTMAPI menu should block gameplay input and mod updates.");
            runtime.RecordInputPressed("F10");
            runtime.RecordInputReleased("F10");
            runtime.Update();
            Assert(pressed == 1 && released == 1 && updates == 1, "Open DTMAPI menu must not deliver gameplay input or UpdateTicked.");
            Assert(!runtime.IsInputDown("F10"), "Blocked input should not leave a stuck down-state.");

            runtime.UI.Close();
            Assert(menuClosed == 1, "Close should dispatch the menu closed event.");
            runtime.UI.SetUiContext("ModUiState", canDrawOverlay: true, gameplayHotkeysAllowed: false, reason: "official mod menu");
            runtime.RecordInputPressed("F10");
            runtime.RecordInputReleased("F10");
            Assert(pressed == 1 && released == 1, "Official/title UI contexts should not deliver gameplay hotkeys.");
            Assert(!runtime.IsInputDown("F10"), "Blocked official-menu input should not leave a stuck down-state.");

            runtime.UI.SetUiContext("Gameplay", canDrawOverlay: true, gameplayHotkeysAllowed: true, reason: "unit test");
            runtime.RecordInputPressed("F10");
            runtime.RecordInputReleased("F10");
            runtime.Update();
            Assert(pressed == 2 && released == 2 && updates == 2, "Gameplay input and updates should resume after closing the menu.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void Suppress_OneFrame_ClearsAfterUpdate()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                IInputHelper input = GetInput(runtime);
                IEventsHelper events = CreateEventsProxy(runtime, "DTMAPI.Tests.Suppress");
                int pressed = 0;
                int released = 0;
                events.Input.ButtonPressed += (_, _) => pressed++;
                events.Input.ButtonReleased += (_, _) => released++;

                runtime.Start();
                runtime.UI.SetUiContext("Gameplay", canDrawOverlay: true, gameplayHotkeysAllowed: true, reason: "unit test");
                input.RegisterButton("F10");
                input.Suppress(" F10 ");
                runtime.RecordInputPressed("F10");
                runtime.RecordInputReleased("F10");
                Assert(pressed == 0 && released == 0, "Suppressed input should not dispatch DTMAPI input events during the current frame.");
                Assert(!input.WasPressed("F10"), "Suppressed input should not be reported as pressed.");
                Assert(!input.IsDown("F10"), "Suppressed input should not leave a down-state.");
                Assert(input.GetSuppressedButtons().Contains("F10", StringComparer.OrdinalIgnoreCase), "Suppressed input should be visible during the current frame.");

                runtime.Update();
                Assert(!input.WasPressed("F10"), "Runtime update should clear one-frame pressed input state.");
                Assert(!input.GetSuppressedButtons().Contains("F10", StringComparer.OrdinalIgnoreCase), "Runtime update should clear one-frame suppressed input state.");
                Assert(!input.IsDown("F10"), "Suppressed input should remain released after the frame clears.");

                runtime.RecordInputPressed("F10");
                Assert(pressed == 1 && input.WasPressed("F10") && input.IsDown("F10"), "Input should dispatch normally after one-frame suppression clears.");
                input.Suppress("F10");
                Assert(!input.WasPressed("F10") && !input.IsDown("F10"), "Suppressing after a press should clear helper pressed/down state even though the already-dispatched event cannot be undone.");
                runtime.RecordInputReleased("F10");
                Assert(released == 0, "Release in the same suppressed frame should not dispatch.");
                runtime.Update();
                runtime.RecordInputPressed("F10");
                runtime.RecordInputReleased("F10");
                Assert(pressed == 2 && released == 1, "Input release should dispatch normally after suppression clears.");
                Assert(!input.IsDown("F10"), "Released input should clear the down-state.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void HookCallbackSafeFallbacksReturnFallbacksAndRecordDiagnostics()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                Type callbacks = typeof(DolocTownHookCallbacks);
                callbacks.GetProperty(nameof(DolocTownHookCallbacks.Runtime))!.SetValue(null, runtime);
                callbacks.GetProperty(nameof(DolocTownHookCallbacks.Bridge))!.SetValue(null, null);

                MethodInfo safeResult = callbacks.GetMethod("SafeResult", BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(typeof(string));
                MethodInfo safePrefix = callbacks.GetMethod("SafePrefix", BindingFlags.NonPublic | BindingFlags.Static)!;
                MethodInfo safePostfix = callbacks.GetMethod("SafePostfix", BindingFlags.NonPublic | BindingFlags.Static)!;

                string result = (string)safeResult.Invoke(null, new object[] { "Test.SafeResult", "fallback", new Func<string>(() => throw new InvalidOperationException("safe-result-boom")) })!;
                bool prefix = (bool)safePrefix.Invoke(null, new object[] { "Test.SafePrefix", new Func<bool>(() => throw new InvalidOperationException("safe-prefix-boom")), true })!;
                safePostfix.Invoke(null, new object[] { "Test.SafePostfix", new Action(() => throw new InvalidOperationException("safe-postfix-boom")) });
                for (int i = 0; i < 6; i++)
                    safePostfix.Invoke(null, new object[] { "Test.SafePostfix.Repeated", new Action(() => throw new InvalidOperationException("safe-postfix-repeat-boom")) });

                Assert(result == "fallback", "SafeResult should return fallback when a hook callback throws.");
                Assert(prefix, "SafePrefix should use the native-pass fallback when a hook callback throws.");
                Assert(runtime.Diagnostics.GetErrors().Count(e => e.Owner == "DTMAPI.GameBridge.HookCallback") == 4, "Hook callback safe helpers should record one diagnostics error per failed operation.");
                Assert(runtime.Diagnostics.GetErrors().Count(e => e.Message.Contains("Test.SafePostfix.Repeated", StringComparison.Ordinal)) == 1, "Repeated hook callback failures should be throttled in diagnostics.");
            }
            finally
            {
                DolocTownHookCallbacks.Runtime = null;
                DolocTownHookCallbacks.Bridge = null;
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void NativeUiLayoutDiagnosticsNormalizeOfficialMenuResetCounts()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                Type serviceType = typeof(DolocTownGameBridge).Assembly.GetType("DTMAPI.GameBridge.DolocTown.NativeUiLayoutDiagnosticsService")
                    ?? throw new InvalidOperationException("NativeUiLayoutDiagnosticsService should exist.");
                object service = Activator.CreateInstance(
                    serviceType,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    binder: null,
                    args: new object[] { runtime },
                    culture: null)
                    ?? throw new InvalidOperationException("NativeUiLayoutDiagnosticsService should be constructable.");
                MethodInfo normalizeHomePage = serviceType.GetMethod("NormalizeHomePageTextMenuResetLayoutSize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("HomePageTextMenu ResetLayoutSize normalizer should exist.");
                MethodInfo normalizeMenuUi = serviceType.GetMethod("NormalizeMenuUiResetLayoutSize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("MenuUI ResetLayoutSize normalizer should exist.");
                MethodInfo normalizeGridLayout = serviceType.GetMethod("NormalizeGridLayoutConstraintCount", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("GridLayoutGroup constraintCount normalizer should exist.");

                var homeMenu = new DolocTown.UI.HomePageTextMenu();
                int homeCount = (int)(normalizeHomePage.Invoke(service, new object[] { homeMenu, 2 }) ?? -1);
                Assert(homeCount == 1, "HomePageTextMenu ResetLayoutSize must normalize stale two-column requests to one column.");

                var mainMenu = new DolocTown.UI.MenuUI();
                for (int i = 0; i < 8; i++)
                    mainMenu.slots.Add(new DolocTown.UI.MenuButton { isVisible = i != 3 });
                int mainMenuCount = (int)(normalizeMenuUi.Invoke(service, new object[] { mainMenu, 2 }) ?? -1);
                Assert(mainMenuCount == 7, "The concrete pause MenuUI should normalize two-column requests back to the visible icon count.");

                var smallMenu = new DolocTown.UI.MenuUI();
                smallMenu.slots.Add(new DolocTown.UI.MenuButton());
                smallMenu.slots.Add(new DolocTown.UI.MenuButton());
                int smallMenuCount = (int)(normalizeMenuUi.Invoke(service, new object[] { smallMenu, 2 }) ?? -1);
                Assert(smallMenuCount == 2, "Small concrete MenuUI instances should keep their native two-item request.");

                DolocAPI.userInput = new DolocAPI.FakeUserInput { CurrentState = new DolocTown.HomePageUiState { textMenu = homeMenu } };
                int homeGridCount = (int)(normalizeGridLayout.Invoke(service, new object[] { homeMenu.slotLayoutGroup, 2 }) ?? -1);
                Assert(homeGridCount == 1, "The active HomePageTextMenu GridLayoutGroup setter must normalize stale two-column writes before they land.");

                var activeMainState = new DolocTown.MainMenuUiState { panel = new DolocTown.UI.MainMenuPanel { menu = mainMenu } };
                DolocAPI.userInput = new DolocAPI.FakeUserInput { CurrentState = activeMainState };
                int mainGridCount = (int)(normalizeGridLayout.Invoke(service, new object[] { mainMenu.slotLayoutGroup, 2 }) ?? -1);
                Assert(mainGridCount == 7, "The active pause MenuUI GridLayoutGroup setter must normalize stale two-column writes before they land.");

                int unrelatedGridCount = (int)(normalizeGridLayout.Invoke(service, new object[] { new DolocTown.UI.FakeGridLayoutGroup { constraintCount = 2 }, 2 }) ?? -1);
                Assert(unrelatedGridCount == 2, "Unrelated GridLayoutGroup writes should keep the native requested count.");
            }
            finally
            {
                DolocAPI.userInput = null;
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void ToolColliderPostfixRoutesKeepOilDropIsolatedFromActionCompletionFailure()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                Type callbacks = typeof(DolocTownHookCallbacks);
                callbacks.GetProperty(nameof(DolocTownHookCallbacks.Runtime))!.SetValue(null, runtime);
                callbacks.GetProperty(nameof(DolocTownHookCallbacks.Bridge))!.SetValue(null, null);

                MethodInfo route = callbacks.GetMethod("RunToolColliderHandleToolsPostfixRoutes", BindingFlags.NonPublic | BindingFlags.Static)
                    ?? throw new InvalidOperationException("ToolCollider postfix route helper should exist.");
                int applyOilDropCount = 0;
                int clearCapturedCount = 0;

                route.Invoke(null, new object[]
                {
                    new Func<bool>(() => throw new InvalidOperationException("one-action-boom")),
                    new Action(() => applyOilDropCount++),
                    new Action(() => clearCapturedCount++)
                });
                route.Invoke(null, new object[]
                {
                    new Func<bool>(() => true),
                    new Action(() => applyOilDropCount++),
                    new Action(() => clearCapturedCount++)
                });
                route.Invoke(null, new object[]
                {
                    new Func<bool>(() => false),
                    new Action(() => throw new InvalidOperationException("oil-apply-boom")),
                    new Action(() => clearCapturedCount++)
                });
                route.Invoke(null, new object[]
                {
                    new Func<bool>(() => true),
                    new Action(() => applyOilDropCount++),
                    new Action(() => throw new InvalidOperationException("oil-clear-boom"))
                });

                Assert(applyOilDropCount == 1, "OilCoalDrop apply should still run once when ActionCompletion fails and falls back to false.");
                Assert(clearCapturedCount == 1, "OilCoalDrop clear should run only when ActionCompletion reports the hit was handled.");
                Assert(runtime.Diagnostics.GetErrors().Any(e => e.Message.Contains("ToolCollider.HandleTools.ActionCompletion", StringComparison.Ordinal)), "ActionCompletion route failures should use their own diagnostics key.");
                Assert(runtime.Diagnostics.GetErrors().Any(e => e.Message.Contains("ToolCollider.HandleTools.OilCoalDrop.ApplyAfterHit", StringComparison.Ordinal)), "OilCoalDrop apply failures should use their own diagnostics key.");
                Assert(runtime.Diagnostics.GetErrors().Any(e => e.Message.Contains("ToolCollider.HandleTools.OilCoalDrop.ClearCaptured", StringComparison.Ordinal)), "OilCoalDrop clear failures should use their own diagnostics key.");
            }
            finally
            {
                DolocTownHookCallbacks.Runtime = null;
                DolocTownHookCallbacks.Bridge = null;
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void FishingAutomationApiIsFeatureOwnedNotExperimentalBridgeOwned()
        {
            Assembly bridgeAssembly = typeof(DolocTownGameBridge).Assembly;
            Type experimentalBridgeType = bridgeAssembly.GetType("DTMAPI.GameBridge.DolocTown.DolocTownExperimentalBridgeApi")
                ?? throw new InvalidOperationException("DolocTownExperimentalBridgeApi type should exist.");
            Type serviceType = bridgeAssembly.GetType("DTMAPI.GameBridge.DolocTown.FishingAutomationService")
                ?? throw new InvalidOperationException("FishingAutomationService type should exist.");
            Type featureType = bridgeAssembly.GetType("DTMAPI.GameBridge.DolocTown.FishingAutomationFeature")
                ?? throw new InvalidOperationException("FishingAutomationFeature type should exist.");

            Assert(!typeof(IFishingAutomationApi).IsAssignableFrom(experimentalBridgeType), "DolocTownExperimentalBridgeApi should no longer implement IFishingAutomationApi.");
            Assert(typeof(IFishingAutomationApi).IsAssignableFrom(serviceType), "FishingAutomationService should implement IFishingAutomationApi.");

            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                object feature = Activator.CreateInstance(featureType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { runtime }, null)
                    ?? throw new InvalidOperationException("FishingAutomationFeature should be constructable for unit tests.");
                string id = (string)(featureType.GetProperty("Id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(feature) ?? string.Empty);
                object? service = featureType.GetProperty("Service", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(feature);
                Assert(id == "FishingAutomation", "FishingAutomationFeature should publish the FishingAutomation feature id.");
                Assert(service is IFishingAutomationApi, "FishingAutomationFeature should own the IFishingAutomationApi service.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void FishingAutomationServiceFailureThrottleRecordsOneDiagnosticPerOperation()
        {
            Assembly bridgeAssembly = typeof(DolocTownGameBridge).Assembly;
            Type serviceType = bridgeAssembly.GetType("DTMAPI.GameBridge.DolocTown.FishingAutomationService")
                ?? throw new InvalidOperationException("FishingAutomationService type should exist.");

            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                object service = Activator.CreateInstance(serviceType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { runtime }, null)
                    ?? throw new InvalidOperationException("FishingAutomationService should be constructable for unit tests.");
                MethodInfo recordFailure = serviceType.GetMethod("RecordFishingAutomationFailure", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("FishingAutomationService failure throttle helper should exist.");

                for (int i = 0; i < 6; i++)
                {
                    recordFailure.Invoke(service, new object?[]
                    {
                        "FishingAutomation.MiniGame.Update",
                        new InvalidOperationException("mini-game-boom"),
                        true,
                        "Smoke.AutoFishingMiniGameComplete",
                        "FishingGameScrollBar.UpdateGame Postfix"
                    });
                }

                Assert(runtime.Diagnostics.GetErrors().Count(e => e.Owner == "DTMAPI.GameBridge.FishingAutomation") == 1, "Repeated FishingAutomation service failures for the same operation should record only the first diagnostics error.");
                IHookStatusInfo hookStatus = runtime.Diagnostics.GetHookStatuses().Single(s => s.HookId == "Smoke.AutoFishingMiniGameComplete");
                Assert(hookStatus.Details.Contains("failureCount=3", StringComparison.Ordinal), "Repeated high-frequency FishingAutomation failures should publish hook status only through the short-warning limit.");
                Assert(!hookStatus.Details.Contains("failureCount=6", StringComparison.Ordinal), "Suppressed high-frequency FishingAutomation failures should not rewrite hook status on every repeat.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void FishingAutomationServiceFailureRecoveryStartsNewDiagnosticsEpisodeAfterStableSuccess()
        {
            Assembly bridgeAssembly = typeof(DolocTownGameBridge).Assembly;
            Type serviceType = bridgeAssembly.GetType("DTMAPI.GameBridge.DolocTown.FishingAutomationService")
                ?? throw new InvalidOperationException("FishingAutomationService type should exist.");

            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                object service = Activator.CreateInstance(serviceType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { runtime }, null)
                    ?? throw new InvalidOperationException("FishingAutomationService should be constructable for unit tests.");
                MethodInfo recordFailure = serviceType.GetMethod("RecordFishingAutomationFailure", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("FishingAutomationService failure throttle helper should exist.");
                MethodInfo recordSuccess = serviceType.GetMethod("RecordFishingAutomationSuccess", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("FishingAutomationService success recovery helper should exist.");
                IDictionary failureEpisodes = (IDictionary)(serviceType.GetField("fishingAutomationFailures", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(service)
                    ?? throw new InvalidOperationException("FishingAutomationService should keep service failure throttle state."));

                for (int i = 0; i < 6; i++)
                    recordFailure.Invoke(service, new object?[] { "FishingAutomation.MiniGame.Update", new InvalidOperationException("mini-game-boom"), true, "Smoke.AutoFishingMiniGameComplete", "FishingGameScrollBar.UpdateGame Postfix" });

                Assert(runtime.Diagnostics.GetErrors().Count(e => e.Owner == "DTMAPI.GameBridge.FishingAutomation") == 1, "Initial repeated FishingAutomation failures should record one diagnostics error.");
                recordSuccess.Invoke(service, new object?[] { "FishingAutomation.MiniGame.Update" });
                recordSuccess.Invoke(service, new object?[] { "FishingAutomation.MiniGame.Update" });
                Assert(failureEpisodes.Count == 1, "Two stable FishingAutomation successes should not clear a failure episode yet.");
                recordSuccess.Invoke(service, new object?[] { "FishingAutomation.MiniGame.Update" });
                Assert(failureEpisodes.Count == 0, "Three stable FishingAutomation successes should clear the failure episode.");

                recordFailure.Invoke(service, new object?[] { "FishingAutomation.MiniGame.Update", new InvalidOperationException("mini-game-boom-again"), true, "Smoke.AutoFishingMiniGameComplete", "FishingGameScrollBar.UpdateGame Postfix" });
                Assert(runtime.Diagnostics.GetErrors().Count(e => e.Owner == "DTMAPI.GameBridge.FishingAutomation") == 2, "A post-recovery FishingAutomation failure should start a new diagnostics episode.");
                IHookStatusInfo hookStatus = runtime.Diagnostics.GetHookStatuses().Single(s => s.HookId == "Smoke.AutoFishingMiniGameComplete");
                Assert(hookStatus.Details.Contains("failureCount=1", StringComparison.Ordinal), "Post-recovery FishingAutomation failure should publish as a fresh first failure.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void FishingAutomationRuntimeStateResetClearsTransientState()
        {
            Assembly bridgeAssembly = typeof(DolocTownGameBridge).Assembly;
            Type serviceType = bridgeAssembly.GetType("DTMAPI.GameBridge.DolocTown.FishingAutomationService")
                ?? throw new InvalidOperationException("FishingAutomationService type should exist.");

            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                object service = Activator.CreateInstance(serviceType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { runtime }, null)
                    ?? throw new InvalidOperationException("FishingAutomationService should be constructable for unit tests.");

                object miniGameHandle = new object();
                object poolOverride = new object();
                var animator = new FakeAnimator { speed = 3.0 };
                IDictionary miniGameStartedAt = (IDictionary)(serviceType.GetField("fishingMiniGameStartedAt", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(service)
                    ?? throw new InvalidOperationException("FishingAutomationService should keep mini-game handle state."));
                object loggedPhases = serviceType.GetField("loggedFishingPhases", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(service)
                    ?? throw new InvalidOperationException("FishingAutomationService should keep phase log cooldown state.");
                IDictionary failureEpisodes = (IDictionary)(serviceType.GetField("fishingAutomationFailures", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(service)
                    ?? throw new InvalidOperationException("FishingAutomationService should keep service failure throttle state."));
                IDictionary animatorSpeeds = (IDictionary)(serviceType.GetField("originalAnimatorSpeeds", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(service)
                    ?? throw new InvalidOperationException("FishingAutomationService should keep original animator speed state."));

                miniGameStartedAt[miniGameHandle] = DateTimeOffset.UtcNow;
                loggedPhases.GetType().GetMethod("Add", new[] { typeof(string) })?.Invoke(loggedPhases, new object[] { "Pull" });
                MethodInfo recordFailure = serviceType.GetMethod("RecordFishingAutomationFailure", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("FishingAutomationService failure throttle helper should exist.");
                recordFailure.Invoke(service, new object?[] { "FishingAutomation.MiniGame.Update", new InvalidOperationException("mini-game-boom"), true, null, null });
                animatorSpeeds[animator] = 1.25;

                serviceType.GetProperty("SuppressFishingAutoCastForSmoke", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(service, true);
                serviceType.GetProperty("ForceFishingNoWaterForSmoke", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(service, true);
                serviceType.GetProperty("ForceFishingNoRodForSmoke", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(service, true);
                serviceType.GetProperty("ForceFishingFishForSmoke", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(service, true);
                serviceType.GetProperty("ForceFishingNativeBiteForSmoke", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(service, true);
                serviceType.GetProperty("FishingPoolOverrideForSmoke", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(service, poolOverride);

                MethodInfo reset = serviceType.GetMethod("ResetFishingRuntimeState", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("FishingAutomationService reset helper should exist.");
                reset.Invoke(service, new object[] { "unit-test-reset" });

                Assert(miniGameStartedAt.Count == 0, "FishingAutomation reset should clear mini-game handle state.");
                int loggedPhaseCount = (int)(loggedPhases.GetType().GetProperty("Count")?.GetValue(loggedPhases) ?? -1);
                Assert(loggedPhaseCount == 0, "FishingAutomation reset should clear phase log cooldown state.");
                Assert(failureEpisodes.Count == 0, "FishingAutomation reset should clear service failure throttle state.");
                Assert(animatorSpeeds.Count == 0 && Math.Abs(animator.speed - 1.25) < 0.0001, "FishingAutomation reset should restore and clear animator speed snapshots.");
                Assert((bool)(serviceType.GetProperty("SuppressFishingAutoCastForSmoke", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(service) ?? true) == false, "FishingAutomation reset should clear suppress-auto-cast smoke override.");
                Assert((bool)(serviceType.GetProperty("ForceFishingNoWaterForSmoke", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(service) ?? true) == false, "FishingAutomation reset should clear no-water smoke override.");
                Assert((bool)(serviceType.GetProperty("ForceFishingNoRodForSmoke", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(service) ?? true) == false, "FishingAutomation reset should clear no-rod smoke override.");
                Assert((bool)(serviceType.GetProperty("ForceFishingFishForSmoke", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(service) ?? true) == false, "FishingAutomation reset should clear force-fish smoke override.");
                Assert((bool)(serviceType.GetProperty("ForceFishingNativeBiteForSmoke", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(service) ?? true) == false, "FishingAutomation reset should clear force-native-bite smoke override.");
                Assert(serviceType.GetProperty("FishingPoolOverrideForSmoke", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(service) == null, "FishingAutomation reset should clear fishing-pool smoke override.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void GameBridgeFeatureFailureThrottleRecordsOneDiagnosticsErrorButKeepsFailureCount()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                var bridge = new DolocTownGameBridge(runtime);
                IEventsHelper events = CreateEventsProxy(runtime, "DTMAPI.Tests.FeatureStatusObserver");
                int featureHookStatusEvents = 0;
                events.Diagnostics.HookStatusChanged += (_, e) =>
                {
                    if (e.HookId == "Feature.UnitFeature")
                        featureHookStatusEvents++;
                };

                MethodInfo recordFailure = typeof(DolocTownGameBridge).GetMethod("RecordGameBridgeFeatureDispatchFailure", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?? throw new InvalidOperationException("Feature failure throttle helper should exist.");
                MethodInfo publishStatus = typeof(DolocTownGameBridge)
                    .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                    .Single(m => m.Name == "PublishGameBridgeFeatureStatusIfNeeded" && m.GetParameters().Length == 5);
                MethodInfo formatStatus = typeof(DolocTownGameBridge).GetMethod("FormatGameBridgeFeatureStatus", BindingFlags.NonPublic | BindingFlags.Static)
                    ?? throw new InvalidOperationException("Feature status formatter should exist.");

                for (int i = 0; i < 6; i++)
                {
                    object?[] args = new object?[] { "UnitFeature", "Update", new InvalidOperationException("feature-update-boom"), null };
                    object dispatchStatus = recordFailure.Invoke(bridge, args) ?? throw new InvalidOperationException("Feature status should be returned.");
                    object publication = args[3] ?? throw new InvalidOperationException("Feature failure publication should be returned.");
                    PropertyInfo shouldPublishHookStatusProperty = publication.GetType().GetProperty("ShouldPublishHookStatus", BindingFlags.NonPublic | BindingFlags.Instance)
                        ?? throw new InvalidOperationException("Feature failure publication should expose hook-status publication decision.");
                    bool shouldPublishHookStatus = (bool)shouldPublishHookStatusProperty.GetValue(publication)!;
                    string details = "Update failed: InvalidOperationException: feature-update-boom. " + (string)formatStatus.Invoke(null, new[] { dispatchStatus })!;
                    publishStatus.Invoke(bridge, new object[] { dispatchStatus, "failed", "Update", details, shouldPublishHookStatus });
                }

                Assert(runtime.Diagnostics.GetErrors().Count(e => e.Owner == "DTMAPI.GameBridge.Feature.UnitFeature") == 1, "Repeated feature-host failures for the same operation should record only the first diagnostics error.");
                Assert(featureHookStatusEvents == 3, "Repeated feature-host failures should publish Feature.UnitFeature hook status only for the first full error and two short warnings.");

                FieldInfo featureStatusesField = typeof(DolocTownGameBridge).GetField("featureStatuses", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?? throw new InvalidOperationException("Feature status dictionary should exist.");
                var featureStatuses = (IDictionary)featureStatusesField.GetValue(bridge)!;
                object status = featureStatuses["UnitFeature"] ?? throw new InvalidOperationException("Feature status should be recorded.");
                PropertyInfo failureCountProperty = status.GetType().GetProperty("FailureCount", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?? throw new InvalidOperationException("Feature status failure count should be readable.");
                PropertyInfo lastErrorProperty = status.GetType().GetProperty("LastError", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?? throw new InvalidOperationException("Feature status last error should be readable.");

                Assert((int)failureCountProperty.GetValue(status)! == 6, "Feature status failure count should still grow for repeated failures.");
                Assert(((string)lastErrorProperty.GetValue(status)!).Contains("feature-update-boom"), "Feature status should keep the latest error text.");
                IDtmFeatureStatusInfo diagnosticStatus = runtime.Diagnostics.GetFeatureStatuses().Single(s => s.FeatureId == "UnitFeature");
                Assert(diagnosticStatus.FailureCount == 6, "Diagnostics feature snapshot should keep the latest failure count even when hook-status publication is throttled.");
                Assert(diagnosticStatus.LastError.Contains("feature-update-boom"), "Diagnostics feature snapshot should keep the latest error text.");
                IHookStatusInfo hookStatus = runtime.Diagnostics.GetHookStatuses().Single(s => s.HookId == "Feature.UnitFeature");
                Assert(hookStatus.Details.Contains("failureCount=3"), "Published hook status should stop at the last allowed short-warning publication.");
                Assert(hookStatus.Details.Contains("consecutiveFailureCount=3"), "Published hook status should expose the consecutive failure count from the last allowed short-warning publication.");
                Assert(!hookStatus.Details.Contains("failureCount=6"), "Suppressed repeated failures should not rewrite the hook status details every time failureCount changes.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void GameBridgeFeatureFailureRecoveryStartsNewDiagnosticsEpisodeAfterStableSuccess()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                var bridge = new DolocTownGameBridge(runtime);
                MethodInfo recordFailure = typeof(DolocTownGameBridge).GetMethod("RecordGameBridgeFeatureDispatchFailure", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?? throw new InvalidOperationException("Feature failure throttle helper should exist.");
                MethodInfo recordSuccess = typeof(DolocTownGameBridge).GetMethod("RecordGameBridgeFeatureSuccess", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?? throw new InvalidOperationException("Feature success helper should exist.");
                MethodInfo publishStatus = typeof(DolocTownGameBridge)
                    .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                    .Single(m => m.Name == "PublishGameBridgeFeatureStatusIfNeeded" && m.GetParameters().Length == 5);
                MethodInfo formatStatus = typeof(DolocTownGameBridge).GetMethod("FormatGameBridgeFeatureStatus", BindingFlags.NonPublic | BindingFlags.Static)
                    ?? throw new InvalidOperationException("Feature status formatter should exist.");

                void PublishFailure(string message)
                {
                    object?[] args = new object?[] { "UnitFeature", "Update", new InvalidOperationException(message), null };
                    object dispatchStatus = recordFailure.Invoke(bridge, args) ?? throw new InvalidOperationException("Feature status should be returned.");
                    object publication = args[3] ?? throw new InvalidOperationException("Feature failure publication should be returned.");
                    PropertyInfo shouldPublishHookStatusProperty = publication.GetType().GetProperty("ShouldPublishHookStatus", BindingFlags.NonPublic | BindingFlags.Instance)
                        ?? throw new InvalidOperationException("Feature failure publication should expose hook-status publication decision.");
                    bool shouldPublishHookStatus = (bool)shouldPublishHookStatusProperty.GetValue(publication)!;
                    string details = "Update failed: InvalidOperationException: " + message + ". " + (string)formatStatus.Invoke(null, new[] { dispatchStatus })!;
                    publishStatus.Invoke(bridge, new object[] { dispatchStatus, "failed", "Update", details, shouldPublishHookStatus });
                }

                void PublishSuccess()
                {
                    object dispatchStatus = recordSuccess.Invoke(bridge, new object[] { "UnitFeature", "Update" })
                        ?? throw new InvalidOperationException("Feature status should be returned.");
                    string details = "Update recovered. " + (string)formatStatus.Invoke(null, new[] { dispatchStatus })!;
                    publishStatus.Invoke(bridge, new object[] { dispatchStatus, "ready", "Update", details, false });
                }

                for (int i = 0; i < 6; i++)
                    PublishFailure("feature-update-boom");
                Assert(runtime.Diagnostics.GetErrors().Count(e => e.Owner == "DTMAPI.GameBridge.Feature.UnitFeature") == 1, "The initial repeated failure episode should record one diagnostics error.");

                for (int i = 0; i < 3; i++)
                    PublishSuccess();

                IDtmFeatureStatusInfo recoveredStatus = runtime.Diagnostics.GetFeatureStatuses().Single(s => s.FeatureId == "UnitFeature");
                Assert(recoveredStatus.Success, "Recovered feature status should report success after stable successes.");
                Assert(recoveredStatus.FailureCount == 6, "Recovered feature status should preserve cumulative failure count.");
                Assert(recoveredStatus.Details.Contains("consecutiveFailureCount=0"), "Recovered feature status should reset the consecutive failure count.");
                Assert(recoveredStatus.Details.Contains("lastRecoveredAt=") && !recoveredStatus.Details.Contains("lastRecoveredAt=none"), "Recovered feature status should record the recovery timestamp.");

                PublishFailure("feature-update-boom-after-recovery");

                Assert(runtime.Diagnostics.GetErrors().Count(e => e.Owner == "DTMAPI.GameBridge.Feature.UnitFeature") == 2, "A new failure after stable recovery should start a fresh diagnostics episode.");
                IDtmFeatureStatusInfo failedAgainStatus = runtime.Diagnostics.GetFeatureStatuses().Single(s => s.FeatureId == "UnitFeature");
                Assert(failedAgainStatus.FailureCount == 7, "FailureCount should remain cumulative across recovery episodes.");
                Assert(failedAgainStatus.Details.Contains("consecutiveFailureCount=1"), "A new episode should start with one consecutive failure.");
                Assert(failedAgainStatus.LastError.Contains("feature-update-boom-after-recovery"), "Diagnostics feature status should keep the newest post-recovery error.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void GameBridgeFeatureExceptionSummaryUnwrapsTargetInvocation()
        {
            MethodInfo formatSummary = typeof(DolocTownGameBridge).GetMethod("FormatGameBridgeExceptionSummary", BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new InvalidOperationException("Feature exception summary helper should exist.");
            MethodInfo formatDetails = typeof(DolocTownGameBridge).GetMethod("FormatGameBridgeExceptionDetails", BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new InvalidOperationException("Feature exception details helper should exist.");

            var root = new InvalidOperationException("animal clone missing");
            var wrapped = new TargetInvocationException("outer reflection call", new TargetInvocationException("inner reflection call", root));
            string summary = (string)(formatSummary.Invoke(null, new object[] { wrapped }) ?? string.Empty);
            string details = (string)(formatDetails.Invoke(null, new object[] { wrapped }) ?? string.Empty);

            Assert(summary.StartsWith("InvalidOperationException: animal clone missing", StringComparison.Ordinal), "Feature exception summaries should start with the unwrapped root cause.");
            Assert(summary.Contains("outer TargetInvocationException", StringComparison.Ordinal), "Feature exception summaries should keep the outer reflection wrapper as context.");
            Assert(details.Contains("RootCause: System.InvalidOperationException: animal clone missing", StringComparison.Ordinal), "Feature exception details should put the root cause before wrapper stack traces.");
            Assert(details.Contains("OuterException: System.Reflection.TargetInvocationException", StringComparison.Ordinal), "Feature exception details should still include the reflection wrapper.");
        }

        private static void DebugConsoleUsesOnlyCellPointerDownRightClickGivePath()
        {
            Assembly bootstrapAssembly = Assembly.Load("DTMAPI.BepInExBootstrap");
            Type consoleType = bootstrapAssembly.GetType("DTMAPI.BepInExBootstrap.ReflectedDebugConsoleUi")
                ?? throw new InvalidOperationException("ReflectedDebugConsoleUi type should exist.");

            Assert(consoleType.GetMethod("TryGivePointerHitItem", BindingFlags.Instance | BindingFlags.NonPublic) == null, "Debug console should not keep the global Mouse1 hit-test give path.");
            Assert(consoleType.GetNestedType("ItemCellHitTarget", BindingFlags.NonPublic) == null, "Debug console should not keep screen-rectangle item hit targets after consolidating right-click give.");
            Assert(consoleType.GetMethod("TryGiveRightClickItem", BindingFlags.Instance | BindingFlags.NonPublic) != null, "Debug console should keep the item-cell PointerDown right-click give path.");
            Assert(consoleType.GetMethod("IsAnyTextInputFocused", BindingFlags.Instance | BindingFlags.NonPublic) != null, "Debug console should guard Y-close while a search input field has focus.");
            Assert(consoleType.GetField("inputFields", BindingFlags.Instance | BindingFlags.NonPublic) != null, "Debug console should track reflected input fields for focus-aware Y handling.");
        }

        private static void MovementDebugLeaseClearsAtSaveBoundariesAndMissingMotionReset()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                var bridge = new DolocTownGameBridge(runtime);
                DolocTownExperimentalBridgeApi api = bridge.ExperimentalApi ?? throw new InvalidOperationException("Experimental bridge API should be registered.");

                SetPrivateField(api, "movementSpeedMultiplier", 3d);
                SetPrivateField(api, "movementSpeedOwnerId", "DTMAPI.UnitTests");
                bridge.NotifyGameBridgeFeaturesSaveLoaded(isNewGame: false);
                MovementDebugState saveState = ((IMovementDebugApi)api).GetState();
                Assert(saveState.IsDefault && Math.Abs(saveState.Multiplier - 1d) < 0.001, "Movement debug lease should not cross save-load boundaries.");
                IHookStatusInfo saveStatus = runtime.Diagnostics.GetHookStatuses().Single(h => h.HookId == "Debug.MovementLease");
                Assert(saveStatus.Status == "disabled" && saveStatus.Details.Contains("SaveLoaded", StringComparison.Ordinal), "SaveLoaded should publish a disabled movement lease status.");

                SetPrivateField(api, "movementSpeedMultiplier", 4d);
                SetPrivateField(api, "movementSpeedOwnerId", "DTMAPI.UnitTests");
                MovementSpeedResult reset = ((IMovementDebugApi)api).ResetSpeed(new ManifestModel { UniqueID = "DTMAPI.UnitTests" }, "unit-missing-motion");
                MovementDebugState resetState = ((IMovementDebugApi)api).GetState();
                Assert(reset.Success, "ResetSpeed should clear the DTMAPI lease even when native MotionAbility is unavailable.");
                Assert(resetState.IsDefault && Math.Abs(resetState.Multiplier - 1d) < 0.001, "ResetSpeed should not leave a future movement reapply lease behind.");
                IHookStatusInfo resetStatus = runtime.Diagnostics.GetHookStatuses().Single(h => h.HookId == "Debug.MovementLease");
                Assert(resetStatus.Status == "disabled" && resetStatus.Details.Contains("MotionAbility was not available", StringComparison.Ordinal), "Missing-motion reset should publish a disabled movement lease status.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void OilCoalDropFeatureLifecycleClearsPendingHits()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                Type featureType = typeof(DolocTownGameBridge).Assembly.GetType("DTMAPI.GameBridge.DolocTown.OilCoalDropFeature")
                    ?? throw new InvalidOperationException("OilCoalDropFeature should exist.");
                object feature = Activator.CreateInstance(
                    featureType,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    binder: null,
                    args: new object[] { runtime, new Func<bool>(() => true) },
                    culture: null)
                    ?? throw new InvalidOperationException("OilCoalDropFeature should be constructable.");
                object service = featureType.GetProperty("Service", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(feature)
                    ?? throw new InvalidOperationException("OilCoalDropFeature.Service should be available.");
                FieldInfo pendingHitsField = service.GetType().GetField("pendingOilResourceHits", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?? throw new InvalidOperationException("OilCoalDropService should retain pending hits internally.");
                var pendingHits = (IDictionary)pendingHitsField.GetValue(service)!;
                Type pendingHitType = service.GetType().GetNestedType("PendingOilResourceHit", BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("OilCoalDropService pending hit type should exist.");
                MethodInfo saveLoaded = featureType.GetMethod("SaveLoaded", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    ?? throw new InvalidOperationException("OilCoalDropFeature.SaveLoaded should exist.");
                MethodInfo returnedToTitle = featureType.GetMethod("ReturnedToTitle", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    ?? throw new InvalidOperationException("OilCoalDropFeature.ReturnedToTitle should exist.");
                MethodInfo environmentReset = featureType.GetMethod("EnvironmentReset", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    ?? throw new InvalidOperationException("OilCoalDropFeature.EnvironmentReset should exist.");

                void SeedPendingHit(string key)
                {
                    pendingHits[key] = Activator.CreateInstance(
                        pendingHitType,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        binder: null,
                        args: new object[] { new object(), "coal_mine", 7 },
                        culture: null)
                        ?? throw new InvalidOperationException("OilCoalDrop pending hit should be constructable.");
                }

                SeedPendingHit("save-loaded");
                saveLoaded.Invoke(feature, new object[] { false });
                Assert(pendingHits.Count == 0, "OilCoalDrop pending hits should clear when a save is loaded.");

                SeedPendingHit("returned-to-title");
                returnedToTitle.Invoke(feature, Array.Empty<object>());
                Assert(pendingHits.Count == 0, "OilCoalDrop pending hits should clear when returning to title.");

                SeedPendingHit("environment-reset");
                environmentReset.Invoke(feature, new object[] { "unit-test" });
                Assert(pendingHits.Count == 0, "OilCoalDrop pending hits should clear when the environment resets.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void ChestLocatorPoliciesMergeEnabledOwners()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                _ = new DolocTownGameBridge(runtime);
                IChestLocatorEnhancerApi api = GetModRegistry(runtime).GetApi<IChestLocatorEnhancerApi>("DTMAPI.GameBridge.DolocTown")
                    ?? throw new InvalidOperationException("ChestLocatorEnhancer API should be registered by the GameBridge runtime owner.");
                IManifest ownerA = new ManifestModel
                {
                    Name = "Chest Policy A",
                    Author = "DTMAPI",
                    Version = "1.0.0",
                    UniqueID = "DTMAPI.Tests.ChestPolicyA"
                };
                IManifest ownerB = new ManifestModel
                {
                    Name = "Chest Policy B",
                    Author = "DTMAPI",
                    Version = "1.0.0",
                    UniqueID = "DTMAPI.Tests.ChestPolicyB"
                };

                ChestLocatorEnhancerRegisterResult first = api.Register(ownerA, new ChestLocatorEnhancerOptions
                {
                    Enabled = true,
                    IncludeSharedCases = false,
                    IncludeSharedStorageShelfBoxes = false,
                    RespectNativeAutoUseBoxSetting = false,
                    VerboseLogging = false
                });
                ChestLocatorEnhancerRegisterResult second = api.Register(ownerB, new ChestLocatorEnhancerOptions
                {
                    Enabled = true,
                    IncludeSharedCases = true,
                    IncludeSharedStorageShelfBoxes = true,
                    RespectNativeAutoUseBoxSetting = true,
                    VerboseLogging = true
                });

                ChestLocatorEnhancerState stateA = api.GetState(ownerA.UniqueID);
                ChestLocatorEnhancerState stateB = api.GetState(ownerB.UniqueID);
                string messageA = stateA.LastMessage ?? string.Empty;
                string messageB = stateB.LastMessage ?? string.Empty;
                Assert(first.Success && second.Success, "ChestLocatorEnhancer owner registration should continue to succeed.");
                Assert(messageA.Contains("effectiveOwners=DTMAPI.Tests.ChestPolicyA|DTMAPI.Tests.ChestPolicyB", StringComparison.Ordinal), "Effective owner summary should include every enabled owner in deterministic order.");
                Assert(messageB.Contains("effectiveOwners=DTMAPI.Tests.ChestPolicyA|DTMAPI.Tests.ChestPolicyB", StringComparison.Ordinal), "Every owner state should expose the same effective owner summary.");
                Assert(messageA.Contains("includeSharedCases=True", StringComparison.Ordinal), "Merged policy should enable shared Case scanning when any enabled owner requests it.");
                Assert(messageA.Contains("includeSharedStorageShelfBoxes=True", StringComparison.Ordinal), "Merged policy should enable shared StorageShelf box scanning when any enabled owner requests it.");
                Assert(messageA.Contains("respectNativeAutoUseBox=False", StringComparison.Ordinal), "Merged policy should allow forced box scans when any enabled owner opts out of native auto-use-box.");
                Assert(messageA.Contains("verboseLogging=True", StringComparison.Ordinal), "Merged policy should enable verbose logging when any enabled owner requests it.");
                Assert(stateA.Status == "configured-pending-hook" && stateB.Status == "configured-pending-hook", "Merged policy should keep the existing pending-hook state before Harmony installation.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void StrongPlantingGunNormalizesToThreeSlotContract()
        {
            Assembly bridgeAssembly = typeof(DolocTownGameBridge).Assembly;
            Type serviceType = bridgeAssembly.GetType("DTMAPI.GameBridge.DolocTown.StrongPlantingGunService")
                ?? throw new InvalidOperationException("StrongPlantingGunService type should exist.");
            MethodInfo normalize = serviceType.GetMethod("NormalizeStrongPlantingGunOptions", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("StrongPlantingGunService should keep an internal normalize helper.");

            var oversized = new StrongPlantingGunOptions { Enabled = true, SlotCount = 12, IncludeSeeds = true, IncludeFilms = true, IncludeFertilizers = true, IncludeWater = true };
            var undersized = new StrongPlantingGunOptions { Enabled = true, SlotCount = 1, IncludeSeeds = true, IncludeFilms = true, IncludeFertilizers = true };
            var disabled = new StrongPlantingGunOptions { Enabled = false, SlotCount = 6 };

            StrongPlantingGunOptions normalizedOversized = (StrongPlantingGunOptions)(normalize.Invoke(null, new object?[] { oversized }) ?? throw new InvalidOperationException("Normalize should return options."));
            StrongPlantingGunOptions normalizedUndersized = (StrongPlantingGunOptions)(normalize.Invoke(null, new object?[] { undersized }) ?? throw new InvalidOperationException("Normalize should return options."));
            StrongPlantingGunOptions normalizedDisabled = (StrongPlantingGunOptions)(normalize.Invoke(null, new object?[] { disabled }) ?? throw new InvalidOperationException("Normalize should return options."));

            Assert(normalizedOversized.SlotCount == 3, "StrongPlantingGun oversized requests should normalize to the fixed three-slot contract.");
            Assert(normalizedUndersized.SlotCount == 3, "StrongPlantingGun undersized requests should normalize to the fixed three-slot contract.");
            Assert(!normalizedDisabled.Enabled && normalizedDisabled.SlotCount == 3, "StrongPlantingGun disabled policies should still report the fixed three-slot contract for compatibility.");
            Assert(normalizedOversized.IncludeWater, "StrongPlantingGun normalize should not silently rewrite unrelated option booleans.");
        }

        private static void SaveSlotsNormalizeToFixedTwelveContract()
        {
            Assembly bridgeAssembly = typeof(DolocTownGameBridge).Assembly;
            Type serviceType = bridgeAssembly.GetType("DTMAPI.GameBridge.DolocTown.SaveSlotsService")
                ?? throw new InvalidOperationException("SaveSlotsService type should exist.");
            MethodInfo normalize = serviceType.GetMethod("NormalizeSaveSlotsOptions", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("SaveSlotsService should keep an internal normalize helper.");

            var oversized = new SaveSlotsOptions { Enabled = true, SlotCount = 24, VerboseLogging = true };
            var undersized = new SaveSlotsOptions { Enabled = true, SlotCount = 6 };
            var disabled = new SaveSlotsOptions { Enabled = false, SlotCount = 24 };

            SaveSlotsOptions normalizedOversized = (SaveSlotsOptions)(normalize.Invoke(null, new object?[] { oversized }) ?? throw new InvalidOperationException("Normalize should return options."));
            SaveSlotsOptions normalizedUndersized = (SaveSlotsOptions)(normalize.Invoke(null, new object?[] { undersized }) ?? throw new InvalidOperationException("Normalize should return options."));
            SaveSlotsOptions normalizedDisabled = (SaveSlotsOptions)(normalize.Invoke(null, new object?[] { disabled }) ?? throw new InvalidOperationException("Normalize should return options."));

            Assert(normalizedOversized.Enabled && normalizedOversized.SlotCount == 12, "SaveSlots enabled oversized requests should normalize to 12 total official slots.");
            Assert(normalizedUndersized.Enabled && normalizedUndersized.SlotCount == 12, "SaveSlots enabled undersized requests should normalize to 12 total official slots.");
            Assert(!normalizedDisabled.Enabled && normalizedDisabled.SlotCount == 6, "SaveSlots disabled requests should normalize to the vanilla six-slot contract.");
            Assert(normalizedOversized.VerboseLogging, "SaveSlots normalize should preserve unrelated logging flags.");
        }

        private static void EquipmentSlotProtectedStoragePolicyUsesPerSaveTailRecovery()
        {
            Assert(EquipmentSlotProtectedStoragePolicy.BuildSaveScopeKey(3) == "slot-3", "Equipment-slot protected storage should key sidecars by archive slot.");
            Assert(EquipmentSlotProtectedStoragePolicy.MakeSafePathSegment("A:B/C") == "A_B_C", "Equipment-slot protected storage should sanitize path segments.");
            Assert(EquipmentSlotProtectedStoragePolicy.GetTailIndexFromEnd(0, 4) == 3, "Equipment-slot storage should record head slots as furthest from the tail.");
            Assert(EquipmentSlotProtectedStoragePolicy.GetTailIndexFromEnd(3, 4) == 0, "Equipment-slot storage should record the final slot as the tail.");

            int[] ordered = EquipmentSlotProtectedStoragePolicy.OrderTailFirst(new[] { 0, 1, 2, 3 }, value => value).ToArray();
            Assert(ordered.SequenceEqual(new[] { 3, 2, 1, 0 }), "Equipment-slot recovery should process tail slots before head slots.");

            Assert(EquipmentSlotProtectedStoragePolicy.IsStorageCompatible(3, "Yuuka", string.Empty, 100, 3, "Yuuka", string.Empty, 120, out _), "Matching save identity should allow protected storage.");
            Assert(!EquipmentSlotProtectedStoragePolicy.IsStorageCompatible(2, "Yuuka", string.Empty, 100, 3, "Yuuka", string.Empty, 120, out string archiveReason) && archiveReason.Contains("archive-index-mismatch"), "Protected storage should reject mismatched archive slots.");
            Assert(!EquipmentSlotProtectedStoragePolicy.IsStorageCompatible(3, "Other", string.Empty, 100, 3, "Yuuka", string.Empty, 120, out string playerReason) && playerReason.Contains("player-name-mismatch"), "Protected storage should reject clearly mismatched player identities.");
            Assert(!EquipmentSlotProtectedStoragePolicy.IsStorageCompatible(3, "Yuuka", string.Empty, 1000, 3, "Yuuka", string.Empty, 120, out string clockReason) && clockReason.Contains("total-game-seconds-regressed"), "Protected storage should reject strong save-clock regressions.");
        }

        private static void EquipmentSlotShieldPolicyMirrorsNativeShieldHat()
        {
            Assert(EquipmentSlotShieldPolicy.IsShieldSkill("shield", "DolocTown.ItemFunctionHatShield", "DolocTown.AgentEquipmentFuncProtoShield"), "Equipment-slot shield policy should recognize native shield hats by skill/function.");

            EquipmentSlotShieldBlockResult defended = EquipmentSlotShieldPolicy.Block(incomingDamage: 2, shieldValue: 10, shieldDefend: 3);
            Assert(defended.FullyBlocked && !defended.Broken && defended.BlockedDamage == 0 && defended.RemainingShieldValue == 10, "Shield Defend should fully block small hits without consuming shield value.");

            EquipmentSlotShieldBlockResult blocked = EquipmentSlotShieldPolicy.Block(incomingDamage: 10, shieldValue: 20, shieldDefend: 2);
            Assert(blocked.FullyBlocked && !blocked.Broken && blocked.BlockedDamage == 8 && blocked.RemainingShieldValue == 12, "Shield should consume damage after native Defend when it fully blocks the hit.");

            EquipmentSlotShieldBlockResult broken = EquipmentSlotShieldPolicy.Block(incomingDamage: 10, shieldValue: 5, shieldDefend: 2);
            Assert(!broken.FullyBlocked && broken.Broken && broken.BlockedDamage == 5 && broken.RemainingShieldValue == 0, "Broken shields should report only the shield value as blocked damage, matching native residual damage semantics.");
        }

        private static void HarmonyTargetSignatureSelectsExactOverload()
        {
            string dir = NewTempGameDir();
            var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
            var patcher = new HarmonyReflectionPatcher(runtime);
            string targetTypeName = typeof(HarmonySignatureProbe).AssemblyQualifiedName ?? throw new InvalidOperationException("Probe type should have an assembly-qualified name.");

            MethodInfo? loose = patcher.ResolveMethod(targetTypeName, nameof(HarmonySignatureProbe.Overload), 1);
            Assert(loose != null && loose.GetParameters().Length == 1, "Loose Harmony target lookup should keep the old parameter-count behavior.");

            MethodInfo? intOverload = patcher.ResolveMethod(
                targetTypeName,
                nameof(HarmonySignatureProbe.Overload),
                HarmonyTargetSignature.Exact(typeof(HarmonySignatureProbe), typeof(int), typeof(int)));
            Assert(intOverload != null && intOverload.ReturnType == typeof(int) && intOverload.GetParameters()[0].ParameterType == typeof(int), "Exact Harmony signature should select the int overload.");

            MethodInfo? stringOverload = patcher.ResolveMethod(
                targetTypeName,
                nameof(HarmonySignatureProbe.Overload),
                HarmonyTargetSignature.Exact(typeof(HarmonySignatureProbe), typeof(string), typeof(string)));
            Assert(stringOverload != null && stringOverload.ReturnType == typeof(string) && stringOverload.GetParameters()[0].ParameterType == typeof(string), "Exact Harmony signature should select the string overload.");

            MethodInfo? typeNameOverload = patcher.ResolveMethod(
                targetTypeName,
                nameof(HarmonySignatureProbe.Overload),
                HarmonyTargetSignature.Exact(
                    typeof(HarmonySignatureProbe).FullName ?? string.Empty,
                    typeof(string).FullName ?? string.Empty,
                    typeof(string).FullName ?? string.Empty));
            Assert(typeNameOverload == stringOverload, "Harmony signature matching should support runtime type-name signatures for game types.");

            MethodInfo? wrongReturn = patcher.ResolveMethod(
                targetTypeName,
                nameof(HarmonySignatureProbe.Overload),
                new HarmonyTargetSignature(returnType: typeof(bool), parameterTypes: new[] { typeof(int) }));
            Assert(wrongReturn == null, "Harmony signature matching should reject the right parameter list with the wrong return type.");

            MethodInfo? wrongDeclaringType = patcher.ResolveMethod(
                targetTypeName,
                nameof(HarmonySignatureProbe.Overload),
                HarmonyTargetSignature.Exact(typeof(string), typeof(int), typeof(int)));
            Assert(wrongDeclaringType == null, "Harmony signature matching should reject the right shape on the wrong declaring type.");
        }

        private static void FishingAutomationBiteActionPrecedence()
        {
            Assembly bridgeAssembly = typeof(DolocTownGameBridge).Assembly;
            Type serviceType = bridgeAssembly.GetType("DTMAPI.GameBridge.DolocTown.FishingAutomationService")
                ?? throw new InvalidOperationException("FishingAutomationService type should exist.");
            MethodInfo resolve = serviceType.GetMethod("ResolveFishingBiteAutomationAction", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("FishingAutomationService should keep an internal bite-action resolver.");

            string defaultFish = Convert.ToString(resolve.Invoke(null, new object?[] { new FishingAutomationOptions(), true, false })) ?? string.Empty;
            string defaultNonFish = Convert.ToString(resolve.Invoke(null, new object?[] { new FishingAutomationOptions(), false, false })) ?? string.Empty;
            string skipFish = Convert.ToString(resolve.Invoke(null, new object?[] { new FishingAutomationOptions { ResultMode = FishingResultMode.SkipMiniGameNativeResult }, true, true })) ?? string.Empty;
            string skipNonFish = Convert.ToString(resolve.Invoke(null, new object?[] { new FishingAutomationOptions { ResultMode = FishingResultMode.SkipMiniGameNativeResult }, false, false })) ?? string.Empty;
            string instantComplete = Convert.ToString(resolve.Invoke(null, new object?[] { new FishingAutomationOptions { BiteWaitMode = FishingBiteWaitMode.InstantNativeBite }, true, true })) ?? string.Empty;

            Assert(defaultFish == "NativeReel", "FishingAutomation default should reel bite-ready fish into the native minigame/result path.");
            Assert(defaultNonFish == "NativeReel", "FishingAutomation default should reel non-fish results through the native result path.");
            Assert(skipFish == "SkipMiniGameNativeResult", "FishingAutomation SkipMiniGame should use the native no-minigame result route for fish.");
            Assert(skipNonFish == "SkipMiniGameNativeResult", "FishingAutomation SkipMiniGame should use the native result route for non-fish.");
            Assert(instantComplete == "NativeReel", "FishingAutomation InstantBite should change wait timing only; result routing stays the normal native reel path.");
        }

        private static void FishingAutomationMiniGameInputDecisionMatchesNativeBars()
        {
            Assembly bridgeAssembly = typeof(DolocTownGameBridge).Assembly;
            Type serviceType = bridgeAssembly.GetType("DTMAPI.GameBridge.DolocTown.FishingAutomationService")
                ?? throw new InvalidOperationException("FishingAutomationService type should exist.");
            MethodInfo resolve = serviceType.GetMethod("ResolveFishingMiniGameInputDecision", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("FishingAutomationService should keep a minigame input decision helper.");

            string stable = Convert.ToString(resolve.Invoke(null, new object?[] { "Stable", 2.0, 1.5, 3.0, false })) ?? string.Empty;
            string beforeStable = Convert.ToString(resolve.Invoke(null, new object?[] { "Stable", 1.0, 1.5, 3.0, false })) ?? string.Empty;
            string bonusFirst = Convert.ToString(resolve.Invoke(null, new object?[] { "Bonus", 2.0, 1.5, 3.0, false })) ?? string.Empty;
            string bonusAfterTap = Convert.ToString(resolve.Invoke(null, new object?[] { "Bonus", 2.1, 1.5, 3.0, true })) ?? string.Empty;
            string delay = Convert.ToString(resolve.Invoke(null, new object?[] { "Delay", 0.5, 0.0, 1.0, false })) ?? string.Empty;

            Assert(stable == "HoldStable", "FishingAutomation minigame should hold during native green/stable bars.");
            Assert(beforeStable == "Release", "FishingAutomation minigame should release during red/off-note time before the next stable bar.");
            Assert(bonusFirst == "TapBonus", "FishingAutomation minigame should short-press a native yellow/bonus bar once.");
            Assert(bonusAfterTap == "Release", "FishingAutomation minigame should release after the yellow/bonus tap.");
            Assert(delay == "Release", "FishingAutomation minigame should not press during native delay notes.");
        }

        private static void FishingAutomationOptionsNormalizeNativeStageDefaults()
        {
            Assembly bridgeAssembly = typeof(DolocTownGameBridge).Assembly;
            Type serviceType = bridgeAssembly.GetType("DTMAPI.GameBridge.DolocTown.FishingAutomationService")
                ?? throw new InvalidOperationException("FishingAutomationService type should exist.");
            MethodInfo normalize = serviceType.GetMethod("NormalizeFishingAutomationOptions", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("FishingAutomationService should keep an options normalize helper.");

            FishingAutomationOptions defaults = (FishingAutomationOptions)(normalize.Invoke(null, new object?[] { null }) ?? throw new InvalidOperationException("Normalize should return options."));
            var invalid = new FishingAutomationOptions
            {
                BiteWaitMode = (FishingBiteWaitMode)999,
                ResultMode = (FishingResultMode)999,
                AnimationMode = (FishingAnimationMode)999,
                RecastDelaySeconds = -10,
                AnimationMultiplier = 100,
                CastChargeRatio = 2
            };
            FishingAutomationOptions normalizedInvalid = (FishingAutomationOptions)(normalize.Invoke(null, new object?[] { invalid }) ?? throw new InvalidOperationException("Normalize should return options."));

            Assert(defaults.BiteWaitMode == FishingBiteWaitMode.NativeWait, "FishingAutomation default wait policy should be native wait.");
            Assert(defaults.ResultMode == FishingResultMode.AutoCompleteVisibleMiniGame, "FishingAutomation default result policy should auto-complete the visible native minigame.");
            Assert(defaults.AnimationMode == FishingAnimationMode.Normal, "FishingAutomation default animation policy should be normal speed.");
            Assert(defaults.StopOnManualMove, "FishingAutomation default should keep manual movement cancellation enabled.");
            Assert(Math.Abs(defaults.RecastDelaySeconds - 0.25) < 0.0001, "FishingAutomation default recast delay should preserve the native-loop cadence.");
            Assert(Math.Abs(defaults.AnimationMultiplier - 3) < 0.0001, "FishingAutomation default fast-animation multiplier should remain three.");
            Assert(Math.Abs(defaults.CastChargeRatio) < 0.0001, "FishingAutomation default cast charge should be no charge.");
            Assert(normalizedInvalid.BiteWaitMode == FishingBiteWaitMode.NativeWait, "Invalid bite wait mode should normalize to native wait.");
            Assert(normalizedInvalid.ResultMode == FishingResultMode.AutoCompleteVisibleMiniGame, "Invalid result mode should normalize to visible minigame completion.");
            Assert(normalizedInvalid.AnimationMode == FishingAnimationMode.Normal, "Invalid animation mode should normalize to normal speed.");
            Assert(Math.Abs(normalizedInvalid.RecastDelaySeconds - 0.05) < 0.0001, "FishingAutomation recast delay should clamp to the supported minimum.");
            Assert(Math.Abs(normalizedInvalid.AnimationMultiplier - 4) < 0.0001, "FishingAutomation animation multiplier should clamp to the supported maximum.");
            Assert(Math.Abs(normalizedInvalid.CastChargeRatio - 1) < 0.0001, "FishingAutomation cast charge should clamp to full charge.");
        }

        private static void FishingAutomationSkipMiniGamePreservesNativePullResult()
        {
            Assembly bridgeAssembly = typeof(DolocTownGameBridge).Assembly;
            Type serviceType = bridgeAssembly.GetType("DTMAPI.GameBridge.DolocTown.FishingAutomationService")
                ?? throw new InvalidOperationException("FishingAutomationService type should exist.");
            Type actionType = serviceType.GetNestedType("FishingBiteAutomationAction", BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("FishingAutomationService should keep an internal bite-action enum.");
            MethodInfo advance = serviceType.GetMethod("TryAdvanceFishingBite", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("FishingAutomationService should keep a native bite advance helper.");

            DolocAPI.gameManager = new FakeDolocGameManager { gameInitConfig = new FakeDolocGameInitConfig { skipFishingGame = true } };
            try
            {
                var stateManager = new FakeFishingStateManager();
                var waitState = new FakeFishingWaitState(stateManager, new DolocTown.AgentStateFishingPull { IsFailed = true });
                object skipAction = Enum.Parse(actionType, "SkipMiniGameNativeResult");
                string targetState = Convert.ToString(advance.Invoke(null, new[] { waitState, skipAction })) ?? string.Empty;

                Assert(targetState == "AgentStateFishingPull", "FishingAutomation skip should advance to the native Pull result state.");
                Assert(stateManager.OverwrittenState is DolocTown.AgentStateFishingPull, "FishingAutomation skip should overwrite with the native Pull state returned by NextState.");
                Assert(((DolocTown.AgentStateFishingPull)stateManager.OverwrittenState!).IsFailed, "FishingAutomation skip must preserve the native Pull failure/success result instead of forcing success.");
                Assert(stateManager.ForceFlag == true, "FishingAutomation skip should keep the native state-manager overwrite flag path.");
            }
            finally
            {
                DolocAPI.gameManager = null;
            }
        }

        private static void FishingAutomationMirrorsNativeReelWhenInputEdgeIsAbsent()
        {
            Assembly bridgeAssembly = typeof(DolocTownGameBridge).Assembly;
            Type serviceType = bridgeAssembly.GetType("DTMAPI.GameBridge.DolocTown.FishingAutomationService")
                ?? throw new InvalidOperationException("FishingAutomationService type should exist.");
            Type actionType = serviceType.GetNestedType("FishingBiteAutomationAction", BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("FishingAutomationService should keep an internal bite-action enum.");
            MethodInfo advance = serviceType.GetMethod("TryAdvanceFishingBite", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("FishingAutomationService should keep a native bite advance helper.");

            DolocAPI.gameManager = new FakeDolocGameManager { gameInitConfig = new FakeDolocGameInitConfig { skipFishingGame = false } };
            DolocAPI.GlobalParameter = new FakeGlobalParameter { FishingEnergyCost = 7 };
            DolocAPI.CostEnergyCalls = 0;
            DolocAPI.LastEnergyCost = 0;
            try
            {
                object nativeReel = Enum.Parse(actionType, "NativeReel");
                var stateManager = new FakeFishingStateManager();
                var waitState = new FakeFishingWaitStateWithNativeMirror(stateManager, new FakeFishProto { Id = "unit_fish", IsFish = true }, 3);
                string battleTarget = Convert.ToString(advance.Invoke(null, new[] { waitState, nativeReel })) ?? string.Empty;

                Assert(battleTarget == "AgentStateFishingBattle", "FishingAutomation should mirror native reel into Battle when NextState stays in Wait because no input edge is present.");
                Assert(stateManager.OverwrittenState is DolocTown.AgentStateFishingBattle, "FishingAutomation should overwrite with the native Battle state for fish when SkipMiniGame is off.");
                Assert(DolocAPI.CostEnergyCalls == 1 && DolocAPI.LastEnergyCost == 7, "FishingAutomation mirrored reel should cost native fishing energy exactly once.");

                object skipAction = Enum.Parse(actionType, "SkipMiniGameNativeResult");
                var skipStateManager = new FakeFishingStateManager();
                var skipWaitState = new FakeFishingWaitStateWithNativeMirror(skipStateManager, new FakeFishProto { Id = "unit_skip_fish", IsFish = true }, 3);
                string pullTarget = Convert.ToString(advance.Invoke(null, new[] { skipWaitState, skipAction })) ?? string.Empty;

                Assert(pullTarget == "AgentStateFishingPull", "FishingAutomation SkipMiniGame should mirror native reel into Pull when no input edge is present.");
                Assert(skipStateManager.OverwrittenState is DolocTown.AgentStateFishingPull pull && !pull.IsFailed, "FishingAutomation SkipMiniGame should use the native successful Pull route for a valid hooked fish.");
                Assert(DolocAPI.gameManager is FakeDolocGameManager manager && !manager.gameInitConfig.skipFishingGame, "FishingAutomation temporary native skip flag should be restored after mirrored reel routing.");
            }
            finally
            {
                DolocAPI.gameManager = null;
                DolocAPI.GlobalParameter = null;
                DolocAPI.CostEnergyCalls = 0;
                DolocAPI.LastEnergyCost = 0;
            }
        }

        private static void FishingAutomationInstantBiteDefersReelUntilWaitPlay()
        {
            Assembly bridgeAssembly = typeof(DolocTownGameBridge).Assembly;
            Type serviceType = bridgeAssembly.GetType("DTMAPI.GameBridge.DolocTown.FishingAutomationService")
                ?? throw new InvalidOperationException("FishingAutomationService type should exist.");

            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                object service = Activator.CreateInstance(serviceType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { runtime }, null)
                    ?? throw new InvalidOperationException("FishingAutomationService should be constructable for unit tests.");
                var owner = new ManifestModel
                {
                    Name = "AutoFishing Tests",
                    Author = "DTMAPI",
                    Version = "1.0.0",
                    UniqueID = "DTMAPI.Tests.AutoFishing",
                    Type = "RuntimeApi"
                };
                ((IFishingAutomationApi)service).Configure(owner, new FishingAutomationOptions
                {
                    BiteWaitMode = FishingBiteWaitMode.InstantNativeBite,
                    ResultMode = FishingResultMode.AutoCompleteVisibleMiniGame
                });
                ((IFishingAutomationApi)service).SetEnabled(owner, true, "unit-test");

                MethodInfo applyWait = serviceType.GetMethod("ApplyFishingWaitAutomation", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(object), typeof(string) }, null)
                    ?? throw new InvalidOperationException("FishingAutomationService should expose the wait automation helper.");

                DolocAPI.gameManager = new FakeDolocGameManager { gameInitConfig = new FakeDolocGameInitConfig { skipFishingGame = false } };
                DolocAPI.GlobalParameter = new FakeGlobalParameter { FishingEnergyCost = 7 };
                DolocAPI.CostEnergyCalls = 0;
                DolocAPI.LastEnergyCost = 0;

                var stateManager = new FakeFishingStateManager();
                var waitState = new FakeFishingWaitStateWithInstantBite(stateManager, new FakeFishProto { Id = "unit_instant_fish", IsFish = true });

                bool enterApplied = (bool)(applyWait.Invoke(service, new object[] { waitState, "AgentStateFishingWait.OnEnter Postfix" }) ?? false);
                Assert(!enterApplied, "FishingAutomation InstantBite should not overwrite fishing state from Wait.OnEnter because native Cast->Wait will set current after OnEnter returns.");
                Assert(stateManager.OverwrittenState == null, "FishingAutomation InstantBite Wait.OnEnter should only prepare bite state.");
                Assert(DolocAPI.CostEnergyCalls == 0, "FishingAutomation InstantBite Wait.OnEnter must not consume fishing energy.");
                Assert(!waitState._waitForFishBite && waitState._hasRolled, "FishingAutomation InstantBite Wait.OnEnter should prepare a bite for the next Wait.OnPlay pass.");

                bool playApplied = (bool)(applyWait.Invoke(service, new object[] { waitState, "AgentStateFishingWait.OnPlay Postfix" }) ?? false);
                Assert(playApplied, "FishingAutomation InstantBite should reel from Wait.OnPlay after the native state transition is complete.");
                Assert(stateManager.OverwrittenState is DolocTown.AgentStateFishingBattle, "FishingAutomation InstantBite should advance to the visible native battle/minigame path for fish.");
                Assert(DolocAPI.CostEnergyCalls == 1 && DolocAPI.LastEnergyCost == 7, "FishingAutomation InstantBite should consume native fishing energy exactly once.");
            }
            finally
            {
                DolocAPI.gameManager = null;
                DolocAPI.GlobalParameter = null;
                DolocAPI.CostEnergyCalls = 0;
                DolocAPI.LastEnergyCost = 0;
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void FishingAutomationAnimationSpeedOnlyRunsOnReadyCastAndPull()
        {
            Assembly bridgeAssembly = typeof(DolocTownGameBridge).Assembly;
            Type serviceType = bridgeAssembly.GetType("DTMAPI.GameBridge.DolocTown.FishingAutomationService")
                ?? throw new InvalidOperationException("FishingAutomationService type should exist.");

            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                object service = Activator.CreateInstance(serviceType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { runtime }, null)
                    ?? throw new InvalidOperationException("FishingAutomationService should be constructable for unit tests.");
                var owner = new ManifestModel
                {
                    Name = "AutoFishing Tests",
                    Author = "DTMAPI",
                    Version = "1.0.0",
                    UniqueID = "DTMAPI.Tests.AutoFishing",
                    Type = "RuntimeApi"
                };
                ((IFishingAutomationApi)service).Configure(owner, new FishingAutomationOptions
                {
                    AnimationMode = FishingAnimationMode.FastCastPull,
                    AnimationMultiplier = 2
                });
                ((IFishingAutomationApi)service).SetEnabled(owner, true, "unit-test");
                MethodInfo notify = serviceType.GetMethod("NotifyFishingPhase", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("FishingAutomationService should keep phase notification helper.");
                PropertyInfo applicationCount = serviceType.GetProperty("FishingAutomationApplicationCount", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("FishingAutomationService should expose an internal application counter.");

                var readySource = new FakeFishingAnimationSource { animator = new FakeAnimator { speed = 1 } };
                notify.Invoke(service, new object?[] { "Ready", readySource });
                Assert((int)(applicationCount.GetValue(service) ?? -1) == 1 && Math.Abs(readySource.animator.speed - 2) < 0.0001, "FishingAutomation FastAnimations should run on Ready charge phase.");

                var castSource = new FakeFishingAnimationSource { animator = new FakeAnimator { speed = 1.25 } };
                notify.Invoke(service, new object?[] { "Cast", castSource });
                Assert((int)(applicationCount.GetValue(service) ?? -1) == 2 && Math.Abs(castSource.animator.speed - 2.5) < 0.0001, "FishingAutomation FastAnimations should run on Cast phase.");

                var waitSource = new FakeFishingAnimationSource { animator = new FakeAnimator { speed = 1 } };
                notify.Invoke(service, new object?[] { "Wait", waitSource });
                Assert((int)(applicationCount.GetValue(service) ?? -1) == 2 && Math.Abs(waitSource.animator.speed - 1) < 0.0001, "FishingAutomation FastAnimations must not run on Wait phase.");

                var pullSource = new FakeFishingAnimationSource { animator = new FakeAnimator { speed = 1.5 } };
                notify.Invoke(service, new object?[] { "Pull", pullSource });
                Assert((int)(applicationCount.GetValue(service) ?? -1) == 3 && Math.Abs(pullSource.animator.speed - 3) < 0.0001, "FishingAutomation FastAnimations should run on Pull phase.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void FishingAutomationReadyChargeSpeedTicksNativeCastTimer()
        {
            Assembly bridgeAssembly = typeof(DolocTownGameBridge).Assembly;
            Type serviceType = bridgeAssembly.GetType("DTMAPI.GameBridge.DolocTown.FishingAutomationService")
                ?? throw new InvalidOperationException("FishingAutomationService type should exist.");

            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                object service = Activator.CreateInstance(serviceType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { runtime }, null)
                    ?? throw new InvalidOperationException("FishingAutomationService should be constructable for unit tests.");
                var owner = new ManifestModel
                {
                    Name = "AutoFishing Tests",
                    Author = "DTMAPI",
                    Version = "1.0.0",
                    UniqueID = "DTMAPI.Tests.AutoFishing",
                    Type = "RuntimeApi"
                };
                ((IFishingAutomationApi)service).Configure(owner, new FishingAutomationOptions
                {
                    AnimationMode = FishingAnimationMode.FastCastPull,
                    AnimationMultiplier = 3
                });
                ((IFishingAutomationApi)service).SetEnabled(owner, true, "unit-test");
                MethodInfo applyReadyCharge = serviceType.GetMethod("ApplyFishingReadyChargeSpeed", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("FishingAutomationService should expose Ready charge speed helper.");

                var readyState = new FakeFishingReadyState();
                applyReadyCharge.Invoke(service, new object?[] { readyState });

                Assert(readyState._castTimer.TickCount == 1, "FishingAutomation Ready charge speed should tick the native CastTimer.");
                Assert(readyState._castTimer.LastDelta > 0.039 && readyState._castTimer.LastDelta < 0.041, "FishingAutomation Ready charge speed should add (multiplier-1)*fixedDeltaTime to CastTimer.");
                Assert(readyState._powerBar.Progress > 0.039 && readyState._powerBar.Progress < 0.041, "FishingAutomation Ready charge speed should refresh the native progress circle.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void FishingAutomationReadyChargeTargetControlsUseToolRelease()
        {
            Assembly bridgeAssembly = typeof(DolocTownGameBridge).Assembly;
            Type serviceType = bridgeAssembly.GetType("DTMAPI.GameBridge.DolocTown.FishingAutomationService")
                ?? throw new InvalidOperationException("FishingAutomationService type should exist.");

            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                object service = Activator.CreateInstance(serviceType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { runtime }, null)
                    ?? throw new InvalidOperationException("FishingAutomationService should be constructable for unit tests.");
                var owner = new ManifestModel
                {
                    Name = "AutoFishing Tests",
                    Author = "DTMAPI",
                    Version = "1.0.0",
                    UniqueID = "DTMAPI.Tests.AutoFishing",
                    Type = "RuntimeApi"
                };
                ((IFishingAutomationApi)service).Configure(owner, new FishingAutomationOptions
                {
                    CastChargeRatio = 0.5
                });
                ((IFishingAutomationApi)service).SetEnabled(owner, true, "unit-test");
                MethodInfo applyReady = serviceType.GetMethod("ApplyFishingReadyAutomation", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("FishingAutomationService should expose Ready automation helper.");
                MethodInfo overrideInput = serviceType.GetMethod("TryOverrideFishingReadyChargeInput", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("FishingAutomationService should expose Ready input override helper.");

                var readyState = new FakeFishingReadyState();
                applyReady.Invoke(service, new object?[] { readyState });
                object?[] holdArgs = { "NormalUseToolInProgress", false };
                bool holdHandled = (bool)(overrideInput.Invoke(service, holdArgs) ?? false);
                bool holdValue = (bool)(holdArgs[1] ?? false);
                Assert(holdHandled && holdValue, "FishingAutomation should hold the native use-tool input before the configured charge target.");

                readyState._castTimer.Tick(0.6f);
                object?[] releaseArgs = { "NormalUseToolInProgress", false };
                bool releaseHandled = (bool)(overrideInput.Invoke(service, releaseArgs) ?? false);
                bool releaseValue = (bool)(releaseArgs[1] ?? true);
                Assert(releaseHandled && !releaseValue, "FishingAutomation should release the native use-tool input after reaching the configured charge target.");

                ((IFishingAutomationApi)service).Configure(owner, new FishingAutomationOptions());
                var noChargeState = new FakeFishingReadyState();
                applyReady.Invoke(service, new object?[] { noChargeState });
                object?[] noChargeArgs = { "NormalUseToolInProgress", true };
                bool noChargeHandled = (bool)(overrideInput.Invoke(service, noChargeArgs) ?? false);
                bool noChargeValue = (bool)(noChargeArgs[1] ?? true);
                Assert(noChargeHandled && !noChargeValue, "FishingAutomation default cast charge should release immediately.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void AutoFishingModConfigPreservesCustomToggleKey()
        {
            var mod = new ModEntry();
            Type modType = typeof(ModEntry);
            FieldInfo configField = modType.GetField("config", BindingFlags.Instance | BindingFlags.NonPublic) ??
                throw new InvalidOperationException("AutoFishing config field should exist.");
            MethodInfo normalizeConfig = modType.GetMethod("NormalizeConfig", BindingFlags.Instance | BindingFlags.NonPublic) ??
                throw new InvalidOperationException("AutoFishing NormalizeConfig should exist.");

            var customConfig = new ModEntry.AutoFishingConfig { ToggleKey = "F7", AnimationMultiplier = 0, CastChargeRatio = 2 };
            configField.SetValue(mod, customConfig);
            normalizeConfig.Invoke(mod, null);
            Assert(customConfig.ToggleKey == "F7", "AutoFishing custom toggle key should not be reset to F6.");
            Assert(Math.Abs(customConfig.AnimationMultiplier - 3) < 0.0001, "AutoFishing missing animation multiplier should migrate to default three.");
            Assert(Math.Abs(customConfig.CastChargeRatio - 1) < 0.0001, "AutoFishing cast charge should still clamp to full charge.");

            var missingConfig = new ModEntry.AutoFishingConfig { ToggleKey = "  ", AnimationMultiplier = 3 };
            configField.SetValue(mod, missingConfig);
            normalizeConfig.Invoke(mod, null);
            Assert(missingConfig.ToggleKey == "F6", "AutoFishing missing toggle key should migrate to default F6.");

            var noneConfig = new ModEntry.AutoFishingConfig { ToggleKey = "None", AnimationMultiplier = 3 };
            configField.SetValue(mod, noneConfig);
            normalizeConfig.Invoke(mod, null);
            Assert(noneConfig.ToggleKey == "None", "AutoFishing explicit None toggle key should remain disabled.");
        }

        private static void AnimalViewerLocalizationGuardRecognizesUiLocalizationComponents()
        {
            Assembly bridgeAssembly = typeof(DolocTownGameBridge).Assembly;
            Type serviceType = bridgeAssembly.GetType("DTMAPI.GameBridge.DolocTown.AnimalViewerService")
                ?? throw new InvalidOperationException("AnimalViewerService type should exist.");
            MethodInfo looksLikeLocalization = serviceType.GetMethod("LooksLikeLocalizationComponent", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("AnimalViewerService should keep a localization guard helper.");

            Assert((bool)(looksLikeLocalization.Invoke(null, new object[] { "DolocTown.UI.UILocalization" }) ?? false), "AnimalViewer localization guard should recognize Doloc UI localization components.");
            Assert((bool)(looksLikeLocalization.Invoke(null, new object[] { "Some.Namespace.LocalizeText" }) ?? false), "AnimalViewer localization guard should recognize Localize-style components.");
            Assert(!(bool)(looksLikeLocalization.Invoke(null, new object[] { "UnityEngine.UI.Text" }) ?? true), "AnimalViewer localization guard must not disable plain text components.");
            Assert(!(bool)(looksLikeLocalization.Invoke(null, new object[] { "DolocTown.UI.ProgressBar" }) ?? true), "AnimalViewer localization guard must not disable the ProgressBar component itself.");
        }

        private static void AnimalViewerFeatureResetsCloneLifecycleOnBoundaries()
        {
            Assembly bridgeAssembly = typeof(DolocTownGameBridge).Assembly;
            Type featureType = bridgeAssembly.GetType("DTMAPI.GameBridge.DolocTown.AnimalViewerFeature")
                ?? throw new InvalidOperationException("AnimalViewerFeature type should exist.");

            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                object feature = Activator.CreateInstance(featureType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { runtime }, null)
                    ?? throw new InvalidOperationException("AnimalViewerFeature should be constructable for unit tests.");

                MethodInfo saveLoaded = featureType.GetMethod("SaveLoaded", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("AnimalViewerFeature.SaveLoaded should exist.");
                MethodInfo returnedToTitle = featureType.GetMethod("ReturnedToTitle", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("AnimalViewerFeature.ReturnedToTitle should exist.");
                MethodInfo environmentReset = featureType.GetMethod("EnvironmentReset", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("AnimalViewerFeature.EnvironmentReset should exist.");

                saveLoaded.Invoke(feature, new object[] { false });
                IHookStatusInfo saveStatus = runtime.Diagnostics.GetHookStatuses().Single(h => h.HookId == "Animals.ViewerRenderingLifecycle");
                Assert(saveStatus.Status == "reset" && saveStatus.Details.Contains("SaveLoaded", StringComparison.Ordinal), "AnimalViewer save-load boundary should reset cloned progress-bar runtime state.");

                returnedToTitle.Invoke(feature, Array.Empty<object>());
                IHookStatusInfo titleStatus = runtime.Diagnostics.GetHookStatuses().Single(h => h.HookId == "Animals.ViewerRenderingLifecycle");
                Assert(titleStatus.Status == "reset" && titleStatus.Details.Contains("ReturnedToTitle", StringComparison.Ordinal), "AnimalViewer title boundary should reset cloned progress-bar runtime state.");

                environmentReset.Invoke(feature, new object[] { "unit-test" });
                IHookStatusInfo resetStatus = runtime.Diagnostics.GetHookStatuses().Single(h => h.HookId == "Animals.ViewerRenderingLifecycle");
                Assert(resetStatus.Status == "reset" && resetStatus.Details.Contains("EnvironmentReset unit-test", StringComparison.Ordinal), "AnimalViewer environment reset should clear cloned progress-bar runtime state.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void CustomEntityRegistriesValidateRegistrationDuplicateCleanupAndSnapshots()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
            string dir = NewTempGameDir();
            var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
            runtime.Start();

            IModRegistry registry = GetModRegistry(runtime);
            Assert(registry.GetApi<ICustomAnimalApi>("DTMAPI") != null, "Custom animal registry API should remain registered by the runtime manifest.");
            Assert(registry.GetApi<ICustomMonsterApi>("DTMAPI") != null, "Custom monster registry API should remain registered by the runtime manifest.");
            Assert(registry.GetApi<ICustomAttackApi>("DTMAPI") != null, "Custom attack registry API should remain registered by the runtime manifest.");
            Assert(registry.GetApi<ICustomDroneApi>("DTMAPI") != null, "Custom drone registry API should remain registered by the runtime manifest.");

            IManifest owner = new ManifestModel
            {
                Name = "Custom Entity Test",
                Author = "DTMAPI",
                Version = "1.0.0",
                UniqueID = "DTMAPI.Tests.CustomEntity"
            };

            runtime.CustomEntities.AnimalLifecycleChanged += (_, __) => throw new InvalidOperationException("listener isolation probe");

            CustomAnimalRegistrationResult invalidAnimal = runtime.CustomEntities.RegisterSpecies(owner, new CustomAnimalSpeciesDefinition { SpeciesId = "NotNamespaced" });
            Assert(!invalidAnimal.Succeeded && invalidAnimal.Messages.Any(m => m.Code == "definition-id-not-namespaced"), "Invalid non-namespaced animal IDs should be rejected.");

            string animalId = owner.UniqueID + ".Animal";
            string monsterId = owner.UniqueID + ".Monster";
            string attackId = owner.UniqueID + ".Attack";
            string droneId = owner.UniqueID + ".Drone";
            CustomAnimalRegistrationResult animal = runtime.CustomEntities.RegisterSpecies(owner, new CustomAnimalSpeciesDefinition
            {
                SpeciesId = animalId,
                DisplayName = TestText("Animal"),
                Diet = new CustomAnimalDietPolicy { AcceptedItemIds = new[] { "hay" }, UnitsPerFeeding = 1 },
                Excrement = new CustomAnimalExcrementPolicy { Enabled = true, Outputs = new[] { new CustomAnimalItemOutput { ItemId = "poop", MinStack = 1, MaxStack = 1 } } },
                Breeding = new CustomAnimalBreedingPolicy { Enabled = true, CompatibleSpeciesIds = new[] { animalId }, OffspringCount = 1 },
                HiddenProducts = new[] { new CustomAnimalProductRule { ProductId = owner.UniqueID + ".HiddenProduct", RequiredProgress = 100, Outputs = new[] { new CustomAnimalItemOutput { ItemId = "hidden", MinStack = 1, MaxStack = 1 } } } },
                Persistence = TestPersistence()
            });
            Assert(animal.Succeeded, "Valid animal definition should register.");
            Assert(runtime.CreateSnapshot().Errors.Any(e => e.Owner == owner.UniqueID && e.Message.Contains("lifecycle listener")), "Throwing lifecycle listeners should be captured with owner attribution.");

            CustomAnimalRegistrationResult duplicateAnimal = runtime.CustomEntities.RegisterSpecies(owner, new CustomAnimalSpeciesDefinition { SpeciesId = animalId });
            Assert(!duplicateAnimal.Succeeded && duplicateAnimal.FailureReason == "duplicate-definition-id", "Duplicate animal definition IDs should be rejected.");

            CustomAttackRegistrationResult attack = runtime.CustomEntities.RegisterAttack(owner, new CustomAttackDefinition
            {
                AttackId = attackId,
                Damage = new CustomDamagePayload { Amount = 2, DamageType = "test" },
                Pattern = new CustomBarragePatternDefinition { Kind = CustomAttackPatternKind.Barrage, ProjectileCount = 3, DeterministicRandomSeed = true }
            });
            Assert(attack.Succeeded, "Valid attack definition should register.");

            CustomMonsterRegistrationResult monster = runtime.CustomEntities.RegisterMonster(owner, new CustomMonsterDefinition
            {
                MonsterId = monsterId,
                SpawnRules = new[] { new CustomMonsterSpawnRule { RuleId = owner.UniqueID + ".Spawn", RoomTags = new[] { "test" }, Probability = 1 } },
                AttackSlots = new[] { new CustomMonsterAttackSlot { SlotId = "primary", AttackId = attackId, CooldownSeconds = 1, Range = 5 } },
                Loot = new[] { new CustomMonsterLootRule { ItemId = "test-loot", MinStack = 1, MaxStack = 1, Chance = 1 } },
                Persistence = TestPersistence()
            });
            Assert(monster.Succeeded, "Valid monster definition should register.");
            CustomMonsterRegistrationResult spawnTable = runtime.CustomEntities.RegisterSpawnTable(owner, new CustomMonsterSpawnTableDefinition { SpawnTableId = owner.UniqueID + ".SpawnTable", MonsterIds = new[] { monsterId } });
            Assert(spawnTable.Succeeded, "Valid monster spawn table should register.");

            CustomDroneRegistrationResult drone = runtime.CustomEntities.RegisterDrone(owner, new CustomDroneDefinition
            {
                DroneId = droneId,
                SupportedModes = new[] { CustomDroneBehaviorMode.Follow, CustomDroneBehaviorMode.Guard, CustomDroneBehaviorMode.Attack },
                EquipmentSlots = new[] { new CustomDroneEquipmentSlotDefinition { SlotId = "weapon", AllowedItemTags = new[] { "weapon" } } },
                AttackIds = new[] { attackId },
                Persistence = TestPersistence()
            });
            Assert(drone.Succeeded, "Valid drone definition should register.");

            Assert(runtime.CustomEntities.GetAnimalSnapshot(owner.UniqueID).RegisteredDefinitionCount == 1, "Animal snapshot should count registered definitions.");
            Assert(runtime.CustomEntities.GetMonsterSnapshot(owner.UniqueID).RuntimeStatus == CustomEntityRuntimeStatus.ConfiguredNoRuntimeInstance, "Monster snapshot should report configured with no runtime instance.");
            Assert(runtime.CustomEntities.GetAttackStatus(owner.UniqueID).Status == "configured-no-runtime-instance", "Attack status should report configured-no-runtime-instance.");
            Assert(runtime.CustomEntities.GetDroneStatus(owner.UniqueID).FailureReason == "runtime-creation-blocked", "Drone status should expose runtime creation blocker.");

            Assert(IsBlocked(runtime.CustomEntities.RequestSpawn(owner, new CustomAnimalSpawnRequest { SpeciesId = animalId })), "Animal spawn should return runtime-creation-blocked before native adapters are verified.");
            Assert(IsBlocked(runtime.CustomEntities.RequestSpawn(owner, new CustomMonsterSpawnRequest { MonsterId = monsterId })), "Monster spawn should return runtime-creation-blocked before native adapters are verified.");
            Assert(IsBlocked(runtime.CustomEntities.ExecuteAttack(owner, new CustomAttackSpawnRequest { AttackId = attackId })), "Attack execution should return runtime-creation-blocked before native adapters are verified.");
            Assert(IsBlocked(runtime.CustomEntities.RequestSummon(owner, new CustomDroneSummonRequest { DroneId = droneId })), "Drone summon should return runtime-creation-blocked before native adapters are verified.");

            runtime.NotifyLoadGameRequested(2);
            runtime.NotifySaveLoaded(isNewGame: false);
            Assert(runtime.CustomEntities.GetAnimalSnapshot(owner.UniqueID).ActiveRuntimeInstanceCount == 0, "Save-load boundary should not keep stale custom animal runtime instances.");

            int removed = runtime.CustomEntities.RemoveOwner(owner.UniqueID, "unit test cleanup");
            Assert(removed >= 5, "Owner cleanup should remove four definitions plus the monster spawn table.");
            Assert(runtime.CustomEntities.GetAnimalSnapshot(owner.UniqueID).RegisteredDefinitionCount == 0, "Animal definitions should be removed by owner cleanup.");
            Assert(runtime.CustomEntities.GetMonsterSnapshot(owner.UniqueID).RegisteredDefinitionCount == 0, "Monster definitions should be removed by owner cleanup.");
            Assert(runtime.CustomEntities.GetAttackSnapshot(owner.UniqueID).RegisteredDefinitionCount == 0, "Attack definitions should be removed by owner cleanup.");
            Assert(runtime.CustomEntities.GetDroneSnapshot(owner.UniqueID).RegisteredDefinitionCount == 0, "Drone definitions should be removed by owner cleanup.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static string NewTempGameDir()
        {
            string dir = Path.Combine(Path.GetTempPath(), "DTMAPI-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(dir, "Mods"));
            return dir;
        }

        private static string? UseTempPersistentRoot()
        {
            string? previousRoot = Environment.GetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT");
            string root = Path.Combine(Path.GetTempPath(), "DTMAPI-tests-persistent", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(root, "SAVE"));
            Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", root);
            return previousRoot;
        }

        private static void RestorePersistentRoot(string? previousRoot)
        {
            Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", previousRoot);
        }

        private static void WriteOfficialModInfos(string persistentRoot, string officialId, bool enabled)
        {
            string saveDir = Path.Combine(persistentRoot, "SAVE");
            Directory.CreateDirectory(saveDir);
            File.WriteAllText(
                Path.Combine(saveDir, "mod_infos.json"),
                "{ \"modInfos\": { \"" + officialId + "\": { \"id\": \"" + officialId + "\", \"enabled\": " + (enabled ? "true" : "false") + ", \"priority\": -1, \"source\": \"Local\", \"title\": \"DTMAPI Test\" } } }");
        }

        private static void WriteManifest(string gameDir, string folderName, string json)
        {
            string modDir = Path.Combine(gameDir, "Mods", folderName);
            Directory.CreateDirectory(modDir);
            File.WriteAllText(Path.Combine(modDir, "manifest.json"), json);
        }

        private static int ReadHotLoadEntryCount(string gameDir)
        {
            string path = Path.Combine(gameDir, "DTMAPI", "config", "DTMAPI.Tests.HotLoad.hotload.txt");
            return File.Exists(path) && int.TryParse(File.ReadAllText(path), out int value) ? value : 0;
        }

        private static string ReadZipText(string zipPath, string entryName)
        {
            using (FileStream file = File.OpenRead(zipPath))
            using (var archive = new ZipArchive(file, ZipArchiveMode.Read))
            {
                ZipArchiveEntry entry = archive.GetEntry(entryName) ?? throw new InvalidOperationException("Zip entry not found: " + entryName);
                using (Stream stream = entry.Open())
                using (var reader = new StreamReader(stream))
                    return reader.ReadToEnd();
            }
        }

        private static string[] ReadZipEntryNames(string zipPath)
        {
            using (FileStream file = File.OpenRead(zipPath))
            using (var archive = new ZipArchive(file, ZipArchiveMode.Read))
                return archive.Entries.Select(entry => entry.FullName).ToArray();
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private static void AssertThrows(Action action, string message)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException)
            {
                return;
            }
            throw new InvalidOperationException(message);
        }

        private static void SetPrivateField(object target, string fieldName, object? value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Expected private field '" + fieldName + "' on " + target.GetType().FullName + ".");
            field.SetValue(target, value);
        }

        private static bool IsBlocked(CustomEntityRequestResult result)
        {
            return result != null && !result.Succeeded && result.FailureReason == "runtime-creation-blocked" && result.RuntimeStatus == CustomEntityRuntimeStatus.RuntimeCreationBlocked;
        }

        private static CustomEntityLocalizedText TestText(string text)
        {
            return new CustomEntityLocalizedText
            {
                Default = text,
                English = text,
                SimplifiedChinese = text
            };
        }

        private static CustomEntityPersistencePolicy TestPersistence()
        {
            return new CustomEntityPersistencePolicy
            {
                Kind = CustomEntityPersistenceKind.SaveScoped,
                SchemaVersion = 1,
                RemoveInstancesWhenOwnerMissing = true,
                SaveKeys = new[] { new CustomEntitySaveDataKey { Key = "state", Version = 1 } }
            };
        }

        private static IModRegistry GetModRegistry(DtmApiRuntime runtime)
        {
            PropertyInfo? registryProperty = typeof(DtmApiRuntime).GetProperty("ModRegistry", BindingFlags.Instance | BindingFlags.NonPublic);
            return (IModRegistry)(registryProperty?.GetValue(runtime) ?? throw new InvalidOperationException("Runtime mod registry should exist."));
        }

        private static IConfigHelper GetConfig(DtmApiRuntime runtime)
        {
            PropertyInfo? configProperty = typeof(DtmApiRuntime).GetProperty("Config", BindingFlags.Instance | BindingFlags.NonPublic);
            return (IConfigHelper)(configProperty?.GetValue(runtime) ?? throw new InvalidOperationException("Runtime config helper should exist."));
        }

        private static IInputHelper GetInput(DtmApiRuntime runtime)
        {
            PropertyInfo? inputProperty = typeof(DtmApiRuntime).GetProperty("Input", BindingFlags.Instance | BindingFlags.NonPublic);
            return (IInputHelper)(inputProperty?.GetValue(runtime) ?? throw new InvalidOperationException("Runtime input helper should exist."));
        }

        private static IContentQueryHelper GetContent(DtmApiRuntime runtime)
        {
            PropertyInfo? contentProperty = typeof(DtmApiRuntime).GetProperty("Content", BindingFlags.Instance | BindingFlags.NonPublic);
            return (IContentQueryHelper)(contentProperty?.GetValue(runtime) ?? throw new InvalidOperationException("Runtime content helper should exist."));
        }

        private static IEventsHelper CreateEventsProxy(DtmApiRuntime runtime, string owner)
        {
            PropertyInfo? eventsProperty = typeof(DtmApiRuntime).GetProperty("Events", BindingFlags.Instance | BindingFlags.NonPublic);
            object events = eventsProperty?.GetValue(runtime) ?? throw new InvalidOperationException("Runtime events should exist.");
            MethodInfo createProxy = events.GetType().GetMethod("CreateProxy", BindingFlags.Instance | BindingFlags.Public) ?? throw new InvalidOperationException("CreateProxy should exist.");
            return (IEventsHelper)(createProxy.Invoke(events, new object[] { owner }) ?? throw new InvalidOperationException("CreateProxy returned null."));
        }

        private static void DispatchOneSecondUpdateTicked(DtmApiRuntime runtime, uint second)
        {
            PropertyInfo? eventsProperty = typeof(DtmApiRuntime).GetProperty("Events", BindingFlags.Instance | BindingFlags.NonPublic);
            object events = eventsProperty?.GetValue(runtime) ?? throw new InvalidOperationException("Runtime events should exist.");
            MethodInfo dispatch = events.GetType().GetMethod("DispatchOneSecondUpdateTicked", BindingFlags.Instance | BindingFlags.Public) ?? throw new InvalidOperationException("DispatchOneSecondUpdateTicked should exist.");
            dispatch.Invoke(events, new object[] { second });
        }

        private sealed class FakeHost : IRuntimeHost
        {
            public FakeHost(string gamePath)
            {
                GamePath = gamePath;
                PluginPath = Path.Combine(gamePath, "BepInEx", "plugins");
            }

            public string GamePath { get; }
            public string PluginPath { get; }
            public string HostName => "UnitTest";
            public void Log(string message) { }
            public void LogWarning(string message) { }
            public void LogError(string message, Exception? exception = null) { }
        }

        private sealed class FakeDiagnosticsApi : IDtmDiagnosticsApi
        {
            private readonly Func<IDtmDiagnosticsSnapshot> getSnapshot;

            public FakeDiagnosticsApi(Func<IDtmDiagnosticsSnapshot> getSnapshot)
            {
                this.getSnapshot = getSnapshot;
            }

            public IDtmDiagnosticsSnapshot GetSnapshot()
            {
                return getSnapshot();
            }
        }

        private sealed class FakeActionCompletionApi : IActionCompletionApi
        {
            public void Configure(IManifest owner, ActionCompletionOptions options) { }
            public BridgeFeatureStatus GetStatus(string uniqueId) => new BridgeFeatureStatus("test", uniqueId);
        }

        private sealed class FakeAnimator
        {
            public double speed { get; set; }
        }

        private sealed class FakeFishingAnimationSource
        {
            public FakeAnimator animator { get; set; } = new FakeAnimator();
        }

        private sealed class FakeFishingReadyState
        {
            public FakeCastTimer _castTimer { get; } = new FakeCastTimer();
            public FakeProgressCircle _powerBar { get; } = new FakeProgressCircle();
            public FakeFishingReadyBody body { get; } = new FakeFishingReadyBody();
        }

        private sealed class FakeCastTimer
        {
            public double Progress { get; private set; }
            public int TickCount { get; private set; }
            public double LastDelta { get; private set; }

            public void Tick(float deltaTime)
            {
                TickCount++;
                LastDelta = deltaTime;
                Progress += deltaTime;
            }
        }

        private sealed class FakeProgressCircle
        {
            public float Progress { get; set; }
            public object? Color { get; set; }
        }

        private sealed class FakeFishingReadyBody
        {
            public FakeFishingReadyRodRenderer fishRodRenderer { get; } = new FakeFishingReadyRodRenderer();
        }

        private sealed class FakeFishingReadyRodRenderer
        {
            public string GetCastForceColor(float value)
            {
                return "color:" + value.ToString("0.###");
            }
        }

        private sealed class FakeFishingStateManager
        {
            public object? OverwrittenState { get; private set; }
            public bool? ForceFlag { get; private set; }

            public void Overwrite(object state, bool force)
            {
                OverwrittenState = state;
                ForceFlag = force;
            }
        }

        private sealed class FakeFishingBody
        {
            public FakeFishingBody(FakeFishingStateManager stateManager)
            {
                StateManager = stateManager;
            }

            public FakeFishingStateManager StateManager { get; }

            public object? FishingCache { get; set; }
        }

        private sealed class FakeFishingWaitState
        {
            private readonly object nextState;

            public FakeFishingWaitState(FakeFishingStateManager stateManager, object nextState)
            {
                body = new FakeFishingBody(stateManager);
                this.nextState = nextState;
            }

            public FakeFishingBody body { get; }

            public object NextState()
            {
                return nextState;
            }
        }

        private sealed class FakeFishingWaitStateWithNativeMirror
        {
            private readonly Dictionary<Type, object> states = new Dictionary<Type, object>();

            public FakeFishingWaitStateWithNativeMirror(FakeFishingStateManager stateManager, object fishProto, double hookDuration)
            {
                body = new FakeFishingBody(stateManager)
                {
                    FishingCache = new FakeFishingCache { FishProto = fishProto }
                };
                _fishOnHookDuration = hookDuration;
            }

            public FakeFishingBody body { get; }

            public double _fishOnHookDuration { get; }

            public object NextState()
            {
                return this;
            }

            public T GetState<T>() where T : class
            {
                Type type = typeof(T);
                if (!states.TryGetValue(type, out object? state))
                {
                    state = Activator.CreateInstance(type) ?? throw new InvalidOperationException("Fake fishing state should be constructable.");
                    states[type] = state;
                }
                return (T)state;
            }
        }

        private sealed class FakeFishingWaitStateWithInstantBite
        {
            private readonly Dictionary<Type, object> states = new Dictionary<Type, object>();

            public FakeFishingWaitStateWithInstantBite(FakeFishingStateManager stateManager, object fishProto)
            {
                body = new FakeFishingBody(stateManager)
                {
                    FishingCache = new FakeFishingCache { FishProto = fishProto }
                };
                _hasRolled = false;
                _hookProbability = 0f;
                _fishOnHookDuration = 0f;
            }

            public FakeFishingBody body { get; }

            public bool _waitForFishBite = true;

            public bool _hasRolled;

            public float _hookProbability;

            public float _fishOnHookDuration;

            public object NextState()
            {
                return this;
            }

            public bool RollFish()
            {
                var cache = body.FishingCache as FakeFishingCache;
                return cache?.FishProto != null;
            }

            public T GetState<T>() where T : class
            {
                Type type = typeof(T);
                if (!states.TryGetValue(type, out object? state))
                {
                    state = Activator.CreateInstance(type) ?? throw new InvalidOperationException("Fake fishing state should be constructable.");
                    states[type] = state;
                }
                return (T)state;
            }
        }

        private sealed class FakeFishingCache
        {
            public object? FishProto { get; set; }
        }

        private sealed class FakeFishProto
        {
            public string Id { get; set; } = string.Empty;

            public bool IsFish { get; set; }
        }

        private sealed class FakeDolocGameManager
        {
            public FakeDolocGameInitConfig gameInitConfig { get; set; } = new FakeDolocGameInitConfig();
        }

        private sealed class FakeDolocGameInitConfig
        {
            public bool skipFishingGame { get; set; }
        }

        private sealed class FakeGlobalParameter
        {
            public int FishingEnergyCost { get; set; }
        }

        private sealed class HarmonySignatureProbe
        {
            public int Overload(int value) => value;

            public string Overload(string value) => value;

            public int Overload(int value, string suffix) => value;
        }

        [DataContract]
        private sealed class SampleConfig
        {
            [DataMember] public bool Enabled { get; set; } = true;
            [DataMember] public int Count { get; set; } = 7;
        }

        public interface IUnitProbeApi
        {
            string Owner { get; }
        }

        public sealed class UnitProbeApi : IUnitProbeApi
        {
            public UnitProbeApi(string owner) => Owner = owner;
            public string Owner { get; }
        }
    }

    public sealed class HotLoadProbeMod : DtmMod
    {
        public override void Entry(IDtmHelper helper)
        {
            helper.ModRegistry.RegisterApi<Program.IUnitProbeApi>(new Program.UnitProbeApi(helper.ModManifest.UniqueID));

            string configPath = helper.Config.GetConfigPath(helper.ModManifest);
            string dir = Path.GetDirectoryName(configPath) ?? string.Empty;
            Directory.CreateDirectory(dir);
            string marker = Path.Combine(dir, helper.ModManifest.UniqueID + ".hotload.txt");
            int count = File.Exists(marker) && int.TryParse(File.ReadAllText(marker), out int existing) ? existing : 0;
            File.WriteAllText(marker, (count + 1).ToString());

            IDtmConfigMenuApi? menu = helper.ModRegistry.GetApi<IDtmConfigMenuApi>("DTMAPI.ModConfigMenu");
            if (menu != null)
            {
                menu.Register(helper.ModManifest, () => { }, () => { });
                menu.AddParagraph(helper.ModManifest, () => "Hot-load probe config page.");
            }
        }
    }

    public sealed class ThrowingEntryProbeMod : DtmMod
    {
        public override void Entry(IDtmHelper helper)
        {
            throw new InvalidOperationException("throwing-entry-probe");
        }
    }

    public sealed class ApiOwnerProbeMod : DtmMod
    {
        public override void Entry(IDtmHelper helper)
        {
            helper.ModRegistry.RegisterApi<Program.IUnitProbeApi>(new Program.UnitProbeApi(helper.ModManifest.UniqueID));
        }
    }
}

public static class DolocAPI
{
    public sealed class FakeUserInput
    {
        public object? CurrentState { get; set; }
    }

    public static object? gameManager;

    public static object? userInput;

    public static object? GlobalParameter;

    public static int CostEnergyCalls;

    public static int LastEnergyCost;

    public static bool CostEnergy(int value)
    {
        CostEnergyCalls++;
        LastEnergyCost = value;
        return true;
    }
}

namespace DolocTown.UI
{
    public enum FakeGridConstraint
    {
        Flexible,
        FixedColumnCount,
        FixedRowCount
    }

    public sealed class FakeGridLayoutGroup
    {
        public FakeGridConstraint constraint { get; set; } = FakeGridConstraint.FixedColumnCount;

        public int constraintCount { get; set; }
    }

    public sealed class TextButton
    {
        public bool isVisible { get; set; } = true;
    }

    public sealed class MenuButton
    {
        public bool isVisible { get; set; } = true;
    }

    public sealed class HomePageTextMenu
    {
        public FakeGridLayoutGroup slotLayoutGroup { get; set; } = new FakeGridLayoutGroup { constraintCount = 2 };

        public List<TextButton> slots { get; } = new List<TextButton>();

        public int totalCapacity { get; set; } = 5;

        public int lineCapacity { get; set; } = 1;

        public int rowCount { get; set; } = 5;

        public int RebuildCount { get; private set; }

        public void RebuildLayout()
        {
            RebuildCount++;
        }
    }

    public sealed class MenuUI
    {
        public FakeGridLayoutGroup slotLayoutGroup { get; set; } = new FakeGridLayoutGroup { constraintCount = 2 };

        public List<MenuButton> slots { get; } = new List<MenuButton>();

        public int totalCapacity { get; set; } = 8;

        public int lineCapacity { get; set; } = 8;

        public int rowCount { get; set; } = 1;

        public int RebuildCount { get; private set; }

        public void RebuildLayout()
        {
            RebuildCount++;
        }
    }

    public sealed class MainMenuPanel
    {
        public MenuUI menu { get; set; } = new MenuUI();
    }
}

namespace DolocTown
{
    public sealed class HomePageUiState
    {
        public DolocTown.UI.HomePageTextMenu textMenu { get; set; } = new DolocTown.UI.HomePageTextMenu();
    }

    public sealed class MainMenuUiState
    {
        public DolocTown.UI.MainMenuPanel panel { get; set; } = new DolocTown.UI.MainMenuPanel();
    }

    public sealed class AgentStateFishingBattle
    {
    }

    public sealed class AgentStateFishingPull
    {
        public bool IsFailed { get; set; }
    }
}
