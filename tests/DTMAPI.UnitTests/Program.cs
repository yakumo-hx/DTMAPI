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

namespace DTMAPI.UnitTests
{
    internal static class Program
    {
        private static int Main()
        {
            try
            {
                RuntimeStartsWithEmptyMods();
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
                ConfigMenuPendingPreviewDrivesConditionalVisibility();
                DisabledDiscoveredModLocksConfigPage();
                OfficialLocalModPackagesRespectOfficialEnablement();
                WorkshopReloadHotLoadsNewlyEnabledCodeModOnceAndLocksDisabledLoadedMod();
                RuntimeUiBoundariesBlockGameplayHotkeysAndModUpdates();
                Suppress_OneFrame_ClearsAfterUpdate();
                HookCallbackSafeFallbacksReturnFallbacksAndRecordDiagnostics();
                ToolColliderPostfixRoutesKeepOilDropIsolatedFromActionCompletionFailure();
                FishingAutomationApiIsFeatureOwnedNotExperimentalBridgeOwned();
                FishingAutomationServiceFailureThrottleRecordsOneDiagnosticPerOperation();
                FishingAutomationServiceFailureRecoveryStartsNewDiagnosticsEpisodeAfterStableSuccess();
                FishingAutomationRuntimeStateResetClearsTransientState();
                GameBridgeFeatureFailureThrottleRecordsOneDiagnosticsErrorButKeepsFailureCount();
                GameBridgeFeatureFailureRecoveryStartsNewDiagnosticsEpisodeAfterStableSuccess();
                OilCoalDropFeatureLifecycleClearsPendingHits();
                ChestLocatorPoliciesMergeEnabledOwners();
                StrongPlantingGunNormalizesToThreeSlotContract();
                AnimalViewerLocalizationGuardRecognizesUiLocalizationComponents();
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
                Assert(DtmApiRuntime.ApiVersion == "0.5.0-alpha", "DTMAPI runtime API version should be 0.5.0-alpha for this dev baseline.");
                Assert(DtmApiRuntime.BinaryVersion == "0.5.0.0", "DTMAPI binary/plugin version should remain numeric for BepInEx and assembly metadata.");
                WriteManifest(dir, "Base", "{ \"Name\": \"Base\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.Base\", \"Type\": \"ContentPack\" }");
                WriteManifest(dir, "NeedsBase2", "{ \"Name\": \"Needs Base 2\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.NeedsBase2\", \"Type\": \"ContentPack\", \"Dependencies\": [ { \"UniqueID\": \"DTMAPI.Tests.Base\", \"MinimumVersion\": \"2.0.0\", \"Required\": true } ] }");
                WriteManifest(dir, "OptionalNeedsBase2", "{ \"Name\": \"Optional Needs Base 2\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.OptionalNeedsBase2\", \"Type\": \"ContentPack\", \"Dependencies\": [ { \"UniqueID\": \"DTMAPI.Tests.Base\", \"MinimumVersion\": \"2.0.0\", \"Required\": false } ] }");
                WriteManifest(dir, "NeedsCurrentAlphaApi", "{ \"Name\": \"Needs Current Alpha API\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.CurrentAlphaApi\", \"Type\": \"ContentPack\", \"MinimumDTMApiVersion\": \"0.5.0-alpha\" }");
                WriteManifest(dir, "Legacy042Api", "{ \"Name\": \"Legacy 0.4.2 API\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.Legacy042Api\", \"Type\": \"ContentPack\", \"MinimumDTMApiVersion\": \"0.4.2\" }");
                WriteManifest(dir, "Legacy031Api", "{ \"Name\": \"Legacy 0.3.1 API\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.Legacy031Api\", \"Type\": \"ContentPack\", \"MinimumDTMApiVersion\": \"0.3.1\" }");
                WriteManifest(dir, "NeedsFutureAlphaApi", "{ \"Name\": \"Needs Future Alpha API\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.FutureAlphaApi\", \"Type\": \"ContentPack\", \"MinimumDTMApiVersion\": \"0.5.1-alpha\" }");
                WriteManifest(dir, "NeedsFutureApi", "{ \"Name\": \"Needs Future API\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.FutureApi\", \"Type\": \"ContentPack\", \"MinimumDTMApiVersion\": \"99.0.0\" }");
                WriteManifest(dir, "CycleA", "{ \"Name\": \"Cycle A\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.CycleA\", \"Type\": \"ContentPack\", \"Dependencies\": [ { \"UniqueID\": \"DTMAPI.Tests.CycleB\", \"Required\": true } ] }");
                WriteManifest(dir, "CycleB", "{ \"Name\": \"Cycle B\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.CycleB\", \"Type\": \"ContentPack\", \"Dependencies\": [ { \"UniqueID\": \"DTMAPI.Tests.CycleA\", \"Required\": true } ] }");

                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                runtime.Start();
                RuntimeSnapshot snapshot = runtime.CreateSnapshot();
                Assert(snapshot.LoadedMods.Any(m => m.Manifest.UniqueID == "DTMAPI.Tests.Base"), "Base dependency should load.");
                Assert(!snapshot.LoadedMods.Any(m => m.Manifest.UniqueID == "DTMAPI.Tests.NeedsBase2"), "Required dependency version mismatch should block loading.");
                Assert(snapshot.LoadedMods.Any(m => m.Manifest.UniqueID == "DTMAPI.Tests.OptionalNeedsBase2"), "Optional dependency version mismatch should warn but not block loading.");
                Assert(snapshot.LoadedMods.Any(m => m.Manifest.UniqueID == "DTMAPI.Tests.CurrentAlphaApi"), "MinimumDTMApiVersion 0.5.0-alpha should load on the current alpha runtime.");
                Assert(snapshot.LoadedMods.Any(m => m.Manifest.UniqueID == "DTMAPI.Tests.Legacy042Api"), "Legacy MinimumDTMApiVersion 0.4.2 should still load on the current alpha runtime.");
                Assert(snapshot.LoadedMods.Any(m => m.Manifest.UniqueID == "DTMAPI.Tests.Legacy031Api"), "Legacy MinimumDTMApiVersion 0.3.1 should still load on the current alpha runtime.");
                Assert(!snapshot.LoadedMods.Any(m => m.Manifest.UniqueID == "DTMAPI.Tests.FutureAlphaApi"), "Future MinimumDTMApiVersion 0.5.1-alpha should block loading.");
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
                Assert(summary.Contains("Errors: 1000") && summary.Contains("Warnings: 1000"), "Diagnostic report summary should report the retained window counts.");
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
                    ManagerInstallStateSummary.Present("0.5.0-alpha", "0.5.0.0", "2026-06-11T00:00:00Z", Path.Combine(Path.GetTempPath(), "install-state.json"), 2, 5, true));
                string installStateLine = ManagerPageRowFormatter.FormatInstallState(installedModel.InstallState);
                Assert(installStateLine.Contains("present") && installStateLine.Contains("version:0.5.0-alpha") && installStateLine.Contains("legacyMoved:2") && installStateLine.Contains("legacyDetected:5") && installStateLine.Contains("uninstall:available"), "Manager install-state formatter should expose install version, legacy counters, and uninstall script availability.");
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
                IDtmModStatusInfo disabledStatus = disabledRuntime.CreateDiagnosticsSnapshot().Mods.Single(m => m.UniqueID == "Yuuka.DTMAPI.Test");
                Assert(!disabledStatus.Loaded && disabledStatus.Status == "disabled" && disabledStatus.StatusCode == "disabled" && !disabledStatus.OfficialEnabled && disabledStatus.OfficialEnablementManaged && disabledStatus.EnablementReason.Contains("官方"), "Diagnostics snapshot should expose official disabled mod status and enablement reason.");

                WriteOfficialModInfos(persistentRoot, "Local.Yuuka_DTMAPI_Test", true);
                var enabledRuntime = new DtmApiRuntime(new FakeHost(gameDir), new ConfigMenuRegistry());
                enabledRuntime.Start();
                RuntimeSnapshot enabledSnapshot = enabledRuntime.CreateSnapshot();
                Assert(enabledSnapshot.DiscoveredMods.Single(m => m.Manifest.UniqueID == "Yuuka.DTMAPI.Test").OfficialEnabled, "Official enabled state should be honored.");
                Assert(enabledSnapshot.LoadedMods.Any(m => m.Manifest.UniqueID == "Yuuka.DTMAPI.Test"), "Official-enabled content package should load/index.");
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

                runtime.Start();
                runtime.UI.SetUiContext("Gameplay", canDrawOverlay: true, gameplayHotkeysAllowed: true, reason: "unit test");
                input.RegisterButton("F10");
                runtime.RecordInputPressed("F10");
                input.Suppress("F10");

                Assert(input.WasPressed("F10"), "Pressed input should be visible during the frame it is recorded.");
                Assert(input.IsDown("F10"), "Pressed input should remain down until release.");
                Assert(input.GetSuppressedButtons().Contains("F10", StringComparer.OrdinalIgnoreCase), "Suppressed input should be visible during the current frame.");

                runtime.Update();
                Assert(!input.WasPressed("F10"), "Runtime update should clear one-frame pressed input state.");
                Assert(!input.GetSuppressedButtons().Contains("F10", StringComparer.OrdinalIgnoreCase), "Runtime update should clear one-frame suppressed input state.");
                Assert(input.IsDown("F10"), "Clearing frame state should not release the input down-state.");

                runtime.RecordInputReleased("F10");
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
