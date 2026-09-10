#pragma warning disable CS0618 // Frozen compatibility contracts are intentionally exercised.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using DTMAPI.Abstractions;
using DTMAPI.Core.Diagnostics;
using DTMAPI.Core.Logging;
using DTMAPI.Core.Manager;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;
using DTMAPI.Core.Services;
using DTMAPI.ModConfigMenu;

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {
        internal const string AtomicCheckpointParticipantProviderId = RuntimeCodeModProbeContracts.AtomicCheckpointParticipantProviderId;

        private static void PlayerDoctorRuntimeServiceMissingHelperIsBoundedAndIdempotent()
        {
            string dir = NewTempGameDir();
            try
            {
                var paths = new RuntimePaths(dir, Path.Combine(dir, "BepInEx", "plugins", "DTMAPI"));
                paths.Ensure();
                File.WriteAllText(paths.PlayerDoctorJsonReportPath, "stale-json");
                File.WriteAllText(paths.PlayerDoctorTextReportPath, "stale-text");
                File.WriteAllText(paths.PlayerDoctorSummaryPath, "stale-summary");
                string stalePending = paths.PlayerDoctorJsonReportPath + ".pending-stale";
                File.WriteAllText(stalePending, "stale-pending");
                var service = new PlayerDoctorRuntimeService();
                PlayerDoctorRuntimeResult first = service.RunOnce(paths, DtmApiRuntime.ApiVersion);
                PlayerDoctorRuntimeResult second = service.RunOnce(paths, DtmApiRuntime.ApiVersion);
                Assert(first.Status == "unavailable" && !first.Completed, "Missing Player Doctor helper should be a non-fatal unavailable startup summary.");
                Assert(object.ReferenceEquals(first, second), "Player Doctor startup service must return its first bounded result and never rerun.");
                Assert(!File.Exists(paths.PlayerDoctorJsonReportPath) &&
                       !File.Exists(paths.PlayerDoctorTextReportPath) &&
                       !File.Exists(paths.PlayerDoctorSummaryPath) &&
                       !File.Exists(stalePending),
                    "Missing Player Doctor must remove stale owned latest reports so a later export cannot misrepresent an unavailable run.");
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }

        private static void PlayerDoctorRuntimeResultPreservesWarnings()
        {
            PlayerDoctorRuntimeResult clean = PlayerDoctorRuntimeResult.FromExitCode(0, "status=ok;errors=0;warnings=0");
            PlayerDoctorRuntimeResult warning = PlayerDoctorRuntimeResult.FromExitCode(0, "status=warning;errors=0;warnings=2");
            Assert(clean.Status == "ready" && clean.Completed, "A clean Player Doctor result should remain ready.");
            Assert(warning.Status == "warnings" && warning.WarningCount == 2 && warning.Completed, "Player Doctor warnings must remain visible to the in-game summary without becoming an error.");
        }

        private static void RuntimeApiRegistrationIsAtomicAndDynamicProviderReserved()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                const string providerId = "DTMAPI.Tests.DynamicProcessProvider";
                const string consumerId = "DTMAPI.Tests.DynamicProcessConsumer";
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                var canonical = new ManifestModel
                {
                    Name = "Dynamic Process Provider",
                    Author = "DTMAPI",
                    Version = "1.2.3",
                    UniqueID = providerId,
                    Type = "RuntimeApi"
                };
                var original = new UnitProbeApi("original");
                runtime.RegisterRuntimeApi<IUnitProbeApi>(canonical, original);

                AssertThrows(
                    () => runtime.RegisterRuntimeApi<IUnitProbeApi>(canonical, new UnitProbeApi("replacement")),
                    "A duplicate process-lifetime owner/contract registration must throw.");
                Assert(ReferenceEquals(runtime.ModRegistry.GetApi<IUnitProbeApi>(providerId), original) && ReferenceEquals(runtime.ModRegistry.Get(providerId), canonical), "Duplicate runtime API failure must preserve the original API object and canonical manifest instance.");

                var conflicting = new ManifestModel
                {
                    Name = "Conflicting Dynamic Provider",
                    Author = "Other",
                    Version = "9.9.9",
                    UniqueID = providerId,
                    Type = "RuntimeApi"
                };
                AssertThrows(
                    () => runtime.RegisterRuntimeApi<ISecondProbeApi>(conflicting, new DualProbeApi("conflicting")),
                    "A different canonical manifest for the same process owner must fail before publishing another contract.");
                Assert(runtime.ModRegistry.GetApi<ISecondProbeApi>(providerId) == null &&
                    ReferenceEquals(runtime.ModRegistry.Get(providerId), canonical) &&
                    runtime.ModRegistry.Get(providerId)?.Name == "Dynamic Process Provider" &&
                    runtime.ModRegistry.Get(providerId)?.Version == "1.2.3",
                    "Conflicting runtime manifest failure must be atomic and leave the canonical provider unchanged.");

                var dual = new DualProbeApi("second-contract");
                runtime.RegisterRuntimeApi<ISecondProbeApi>(canonical, dual);
                Assert(ReferenceEquals(runtime.ModRegistry.GetApi<IUnitProbeApi>(providerId), original) && ReferenceEquals(runtime.ModRegistry.GetApi<ISecondProbeApi>(providerId), dual), "One canonical process owner may register distinct API contracts without replacing earlier contracts.");

                WriteAtomicProbePackage(dir, providerId, string.Empty);
                WriteAtomicProbePackage(dir, consumerId, ", \"Dependencies\": [{ \"UniqueID\": \"" + providerId + "\", \"MinimumVersion\": \"1.2.3\", \"Required\": true }]");
                AtomicOwnerProbeMod.EntryCount = 0;
                runtime.Start();

                RuntimeSnapshot started = runtime.CreateSnapshot();
                Assert(!started.LoadedMods.Any(mod => mod.Manifest.UniqueID == providerId) && started.Errors.Any(error => error.Owner == providerId && error.Message.Contains("保留", StringComparison.Ordinal)), "An ordinary source collision with a dynamically registered process provider must be rejected before Assembly.LoadFrom.");
                Assert(started.LoadedMods.Count(mod => mod.Manifest.UniqueID == consumerId) == 1 && AtomicOwnerProbeMod.EntryCount == 1, "An ordinary consumer should load once against the preserved dynamic process provider.");
                Assert(ReferenceEquals(runtime.ModRegistry.Get(providerId), canonical) &&
                    ReferenceEquals(runtime.ModRegistry.GetApi<IUnitProbeApi>(providerId), original) &&
                    ReferenceEquals(runtime.ModRegistry.GetApi<ISecondProbeApi>(providerId), dual),
                    "Source collision rejection must preserve the canonical manifest and both runtime API contracts.");

                int consumerRoots = CountCoreOwnerRootsForTest(runtime, consumerId);
                runtime.NotifyWorkshopModListChanged();
                runtime.NotifyWorkshopModListChanged();
                Assert(runtime.LoadedMods.Count(mod => mod.Manifest.UniqueID == consumerId) == 1 && AtomicOwnerProbeMod.EntryCount == 1 && !runtime.OwnerRequiresRestart(consumerId), "No-op reconciliation must keep the dynamic-provider consumer active without repeating Entry.");
                Assert(CountCoreOwnerRootsForTest(runtime, consumerId) == consumerRoots &&
                    ReferenceEquals(runtime.ModRegistry.Get(providerId), canonical) &&
                    ReferenceEquals(runtime.ModRegistry.GetApi<IUnitProbeApi>(providerId), original) &&
                    ReferenceEquals(runtime.ModRegistry.GetApi<ISecondProbeApi>(providerId), dual),
                    "No-op reconciliation must preserve consumer roots and the dynamically reserved process provider.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void RuntimeApiCannotClaimLoadedOrdinaryOwner()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                const string providerId = "DTMAPI.Tests.OrdinaryFirstProvider";
                const string consumerId = "DTMAPI.Tests.OrdinaryFirstConsumer";
                WriteManifest(
                    dir,
                    "OrdinaryFirstProvider",
                    "{ \"Name\": \"Ordinary First Provider\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"" + providerId + "\", \"Type\": \"ContentPack\" }");
                WriteAtomicProbePackage(
                    dir,
                    consumerId,
                    ", \"Dependencies\": [{ \"UniqueID\": \"" + providerId + "\", \"MinimumVersion\": \"1.0.0\", \"Required\": true }]");

                AtomicOwnerProbeMod.EntryCount = 0;
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                runtime.Start();

                DiscoveredMod ordinaryProvider = runtime.LoadedMods.Single(mod => mod.Manifest.UniqueID == providerId);
                Assert(runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == consumerId) && AtomicOwnerProbeMod.EntryCount == 1, "The ordinary-first collision fixture should begin with an active source provider and required code consumer.");
                AssertThrows(
                    () => runtime.RegisterRuntimeApi<ISecondProbeApi>(ordinaryProvider.Manifest, new DualProbeApi("late-platform-contract")),
                    "A process-lifetime provider must not claim an owner ID after an ordinary source owner is already loaded.");
                Assert(runtime.ModRegistry.GetApi<ISecondProbeApi>(providerId) == null &&
                    ReferenceEquals(runtime.ModRegistry.Get(providerId), ordinaryProvider.Manifest),
                    "Rejected ordinary-first collision must preserve the source manifest and publish no platform API contract.");

                File.WriteAllText(Path.Combine(ordinaryProvider.RootPath, "dtmapi.disabled"), "disable ordinary provider after rejected platform claim");
                runtime.NotifyWorkshopModListChanged();

                Assert(!runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == providerId || mod.Manifest.UniqueID == consumerId), "Disabling the ordinary provider must still cascade to its required consumer after the rejected platform claim.");
                Assert(CountCoreOwnerRootsForTest(runtime, providerId) == 0 && CountCoreOwnerRootsForTest(runtime, consumerId) == 0, "Ordinary provider disable and dependent cascade must leave no Core owner roots.");
                Assert(!runtime.OwnerRequiresRestart(providerId) && runtime.OwnerRequiresRestart(consumerId), "The non-code source provider should remain reusable while the consumer with a loaded assembly becomes restart-required.");
                Assert(runtime.ModRegistry.GetApi<ISecondProbeApi>(providerId) == null && AtomicOwnerProbeMod.EntryCount == 1, "Rejected platform binding must not survive cleanup or cause consumer Entry to repeat.");
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
            Assert(!runtime.IsAuthorSessionActive, "Ordinary player startup without a one-shot descriptor must not create an author-session listener.");
            Assert(
                File.ReadAllText(runtime.Diagnostics.GetLatestLogPath()).Contains("no startup descriptor; no listener, watcher, timer or file poll was created", StringComparison.Ordinal),
                "Ordinary player startup should leave durable evidence that Author SDK transport facilities were not created.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void RuntimeSubsystemOptionsDefaultAndEnvironmentOverride()
        {
            string dir = NewTempGameDir();
            var paths = new RuntimePaths(dir, Path.Combine(dir, "BepInEx", "plugins", "DTMAPI"));
            paths.Ensure();
            string[] variables =
            {
                "DTMAPI_REFACTOR_LIFECYCLE_OBSERVATION",
                "DTMAPI_REFACTOR_SHADOW_CONTENT_REGISTRY",
                "DTMAPI_REFACTOR_SHADOW_RESOURCE_LOADER",
                "DTMAPI_REFACTOR_REGISTRY_TAKES_OVER",
                "DTMAPI_REFACTOR_RESOURCE_LIFECYCLE_LEDGER",
                "DTMAPI_REFACTOR_RESOURCE_LIFECYCLE_CLEANUP",
                "DTMAPI_REFACTOR_RESOURCE_LIFECYCLE_TITLE_ASSET_RELEASE",
                "DTMAPI_REFACTOR_HOOK_INSTALL_SCHEDULER",
                "DTMAPI_REFACTOR_HOOK_STATUS_QUEUE",
                "DTMAPI_REFACTOR_EVENT_MAIN_THREAD_BOUNDARY",
                "DTMAPI_REFACTOR_EVENT_HANDLER_TIMING_DIAGNOSTICS",
                "DTMAPI_REFACTOR_HOOK_READINESS_LAYERS",
                "DTMAPI_REFACTOR_SAVELOAD_REQUEST_COORDINATOR",
                "DTMAPI_REFACTOR_MOD_OWNER_LEDGER",
                "DTMAPI_REFACTOR_MOD_LOAD_TRANSACTION",
                "DTMAPI_REFACTOR_OWNER_BOUND_INPUT",
                "DTMAPI_REFACTOR_EVENT_HANDLER_QUARANTINE",
                "DTMAPI_REFACTOR_CONFIG_PREVIEW_AUDIT",
                "DTMAPI_REFACTOR_GAMEBRIDGE_FEATURE_CONTRACTS",
                "DTMAPI_REFACTOR_GAMEBRIDGE_FINAL_HEALTH_SNAPSHOT",
                "DTMAPI_REFACTOR_CONTENT_MANIFEST_REGISTRY"
            };
            Dictionary<string, string?> previous = variables.ToDictionary(name => name, name => Environment.GetEnvironmentVariable(name), StringComparer.OrdinalIgnoreCase);
            try
            {
                foreach (string variable in variables)
                    Environment.SetEnvironmentVariable(variable, null);

                RuntimeSubsystemOptions defaults = RuntimeSubsystemOptions.Load(paths, out string defaultSummary, out string defaultWarning);
                Assert(defaults.LifecycleObservation, "Lifecycle observation should default on.");
                Assert(defaults.ShadowContentRegistry, "Shadow content registry should default on.");
                Assert(!defaults.ShadowResourceLoader, "Shadow resource loader should default off.");
                Assert(!defaults.RegistryTakesOver, "Registry takeover should default off.");
                Assert(defaults.ResourceLifecycleLedger, "Resource lifecycle ledger should default on.");
                Assert(defaults.ResourceLifecycleCleanup, "Resource lifecycle cleanup should default on.");
                Assert(!defaults.ResourceLifecycleTitleAssetRelease, "Title asset release should default off.");
                Assert(defaults.HookInstallScheduler, "Hook install scheduler should default on.");
                Assert(defaults.HookStatusQueue, "Hook status queue should default on.");
                Assert(defaults.EventMainThreadBoundary, "Event main-thread boundary should default on.");
                Assert(!defaults.EventHandlerTimingDiagnostics, "Per-handler timing should default off on the player hot path.");
                Assert(defaults.HookReadinessLayers, "Hook readiness layers should default on.");
                Assert(defaults.SaveLoadRequestCoordinator, "SaveLoad request coordinator should default on.");
                Assert(defaults.ModOwnerLedger, "Mod owner ledger should default on.");
                Assert(defaults.ModLoadTransaction, "Mod load transaction should default on.");
                Assert(defaults.OwnerBoundInput, "Owner-bound input should default on.");
                Assert(defaults.EventHandlerQuarantine, "Event handler quarantine should default on.");
                Assert(defaults.ConfigPreviewAudit, "Config preview audit should default on.");
                Assert(defaults.GameBridgeFeatureContracts, "GameBridge feature contract diagnostics should default on.");
                Assert(defaults.GameBridgeFinalHealthSnapshot, "GameBridge final health snapshot should default on.");
                Assert(defaults.ContentManifestRegistry, "Content/manifest registry authoritative index should default on.");
                Assert(File.Exists(RuntimeSubsystemOptions.GetConfigPath(paths)), "Refactor scaffold config should be created when missing.");
                Assert(defaultSummary.Contains("LifecycleObservation=true"), "Default summary should include lifecycle flag.");
                Assert(defaultSummary.Contains("ResourceLifecycleLedger=true"), "Default summary should include resource ledger flag.");
                Assert(defaultSummary.Contains("HookInstallScheduler=true"), "Default summary should include hook scheduler flag.");
                Assert(defaultSummary.Contains("SaveLoadRequestCoordinator=true"), "Default summary should include SaveLoad coordinator flag.");
                Assert(defaultSummary.Contains("ModOwnerLedger=true"), "Default summary should include mod owner ledger flag.");
                Assert(defaultSummary.Contains("GameBridgeFeatureContracts=true"), "Default summary should include GameBridge feature contract flag.");
                Assert(defaultSummary.Contains("GameBridgeFinalHealthSnapshot=true"), "Default summary should include GameBridge final health flag.");
                Assert(defaultSummary.Contains("ContentManifestRegistry=true"), "Default summary should include content/manifest registry flag.");
                Assert(defaultSummary.Contains("EventHandlerTimingDiagnostics=false"), "Default summary should expose the disabled explicit timing mode.");
                Assert(string.IsNullOrWhiteSpace(defaultWarning), "Default options load should not warn.");
                Assert(!File.ReadAllText(RuntimeSubsystemOptions.GetConfigPath(paths)).Contains("GameBridgeFeatureUpdateBuckets", StringComparison.Ordinal),
                    "New refactor-scaffold configs must not publish the retired feature bucket option.");

                Environment.SetEnvironmentVariable("DTMAPI_REFACTOR_LIFECYCLE_OBSERVATION", "0");
                Environment.SetEnvironmentVariable("DTMAPI_REFACTOR_SHADOW_CONTENT_REGISTRY", "false");
                Environment.SetEnvironmentVariable("DTMAPI_REFACTOR_SHADOW_RESOURCE_LOADER", "yes");
                Environment.SetEnvironmentVariable("DTMAPI_REFACTOR_REGISTRY_TAKES_OVER", "on");
                Environment.SetEnvironmentVariable("DTMAPI_REFACTOR_RESOURCE_LIFECYCLE_LEDGER", "off");
                Environment.SetEnvironmentVariable("DTMAPI_REFACTOR_RESOURCE_LIFECYCLE_CLEANUP", "0");
                Environment.SetEnvironmentVariable("DTMAPI_REFACTOR_RESOURCE_LIFECYCLE_TITLE_ASSET_RELEASE", "true");
                Environment.SetEnvironmentVariable("DTMAPI_REFACTOR_HOOK_INSTALL_SCHEDULER", "0");
                Environment.SetEnvironmentVariable("DTMAPI_REFACTOR_HOOK_STATUS_QUEUE", "off");
                Environment.SetEnvironmentVariable("DTMAPI_REFACTOR_EVENT_MAIN_THREAD_BOUNDARY", "false");
                Environment.SetEnvironmentVariable("DTMAPI_REFACTOR_EVENT_HANDLER_TIMING_DIAGNOSTICS", "true");
                Environment.SetEnvironmentVariable("DTMAPI_REFACTOR_HOOK_READINESS_LAYERS", "no");
                Environment.SetEnvironmentVariable("DTMAPI_REFACTOR_SAVELOAD_REQUEST_COORDINATOR", "0");
                Environment.SetEnvironmentVariable("DTMAPI_REFACTOR_MOD_OWNER_LEDGER", "false");
                Environment.SetEnvironmentVariable("DTMAPI_REFACTOR_MOD_LOAD_TRANSACTION", "0");
                Environment.SetEnvironmentVariable("DTMAPI_REFACTOR_OWNER_BOUND_INPUT", "off");
                Environment.SetEnvironmentVariable("DTMAPI_REFACTOR_EVENT_HANDLER_QUARANTINE", "no");
                Environment.SetEnvironmentVariable("DTMAPI_REFACTOR_CONFIG_PREVIEW_AUDIT", "false");
                Environment.SetEnvironmentVariable("DTMAPI_REFACTOR_GAMEBRIDGE_FEATURE_CONTRACTS", "0");
                Environment.SetEnvironmentVariable("DTMAPI_REFACTOR_GAMEBRIDGE_FINAL_HEALTH_SNAPSHOT", "no");
                Environment.SetEnvironmentVariable("DTMAPI_REFACTOR_CONTENT_MANIFEST_REGISTRY", "false");
                RuntimeSubsystemOptions overridden = RuntimeSubsystemOptions.Load(paths, out string overrideSummary, out _);
                Assert(!overridden.LifecycleObservation, "Environment override should disable lifecycle observation.");
                Assert(!overridden.ShadowContentRegistry, "Environment override should disable shadow content registry.");
                Assert(overridden.ShadowResourceLoader, "Environment override should enable shadow resource loader flag.");
                Assert(overridden.RegistryTakesOver, "Environment override should enable registry takeover flag for diagnostics.");
                Assert(!overridden.ResourceLifecycleLedger, "Environment override should disable resource lifecycle ledger.");
                Assert(!overridden.ResourceLifecycleCleanup, "Environment override should disable resource lifecycle cleanup.");
                Assert(overridden.ResourceLifecycleTitleAssetRelease, "Environment override should enable title asset release flag for diagnostics.");
                Assert(!overridden.HookInstallScheduler, "Environment override should disable hook install scheduler.");
                Assert(!overridden.HookStatusQueue, "Environment override should disable hook status queue.");
                Assert(!overridden.EventMainThreadBoundary, "Environment override should disable event main-thread boundary.");
                Assert(overridden.EventHandlerTimingDiagnostics, "Environment override should enable explicit handler timing diagnostics.");
                Assert(!overridden.HookReadinessLayers, "Environment override should disable hook readiness layers.");
                Assert(!overridden.SaveLoadRequestCoordinator, "Environment override should disable SaveLoad request coordinator.");
                Assert(!overridden.ModOwnerLedger, "Environment override should disable mod owner ledger.");
                Assert(!overridden.ModLoadTransaction, "Environment override should disable mod load transaction.");
                Assert(!overridden.OwnerBoundInput, "Environment override should disable owner-bound input.");
                Assert(!overridden.EventHandlerQuarantine, "Environment override should disable event handler quarantine.");
                Assert(!overridden.ConfigPreviewAudit, "Environment override should disable config preview audit.");
                Assert(!overridden.GameBridgeFeatureContracts, "Environment override should disable GameBridge feature contract diagnostics.");
                Assert(!overridden.GameBridgeFinalHealthSnapshot, "Environment override should disable GameBridge final health snapshots.");
                Assert(!overridden.ContentManifestRegistry, "Environment override should disable content/manifest registry authoritative index.");
                Assert(overrideSummary.Contains("RegistryTakesOver=true"), "Override summary should include takeover flag.");
                Assert(overrideSummary.Contains("ResourceLifecycleTitleAssetRelease=true"), "Override summary should include title asset release flag.");
                Assert(overrideSummary.Contains("HookInstallScheduler=false"), "Override summary should include hook scheduler flag.");
                Assert(overrideSummary.Contains("SaveLoadRequestCoordinator=false"), "Override summary should include SaveLoad coordinator flag.");
                Assert(overrideSummary.Contains("ModOwnerLedger=false"), "Override summary should include mod owner ledger flag.");
                Assert(overrideSummary.Contains("GameBridgeFeatureContracts=false"), "Override summary should include GameBridge feature contract flag.");
                Assert(overrideSummary.Contains("GameBridgeFinalHealthSnapshot=false"), "Override summary should include GameBridge final health flag.");
                Assert(overrideSummary.Contains("ContentManifestRegistry=false"), "Override summary should include content/manifest registry flag.");
            }
            finally
            {
                foreach (KeyValuePair<string, string?> pair in previous)
                    Environment.SetEnvironmentVariable(pair.Key, pair.Value);
            }
        }

        private static void RuntimeSubsystemOptionsAllFlagsOffDisablesObservationAndShadowRegistry()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                string configDir = Path.Combine(dir, "DTMAPI", "config");
                Directory.CreateDirectory(configDir);
                File.WriteAllText(
                    Path.Combine(configDir, "refactor-scaffold.json"),
                    "{ \"LifecycleObservation\": false, \"ShadowContentRegistry\": false, \"ShadowResourceLoader\": false, \"RegistryTakesOver\": false, \"ResourceLifecycleLedger\": false, \"ResourceLifecycleCleanup\": false, \"ResourceLifecycleTitleAssetRelease\": false, \"HookInstallScheduler\": false, \"HookStatusQueue\": false, \"EventMainThreadBoundary\": false, \"HookReadinessLayers\": false, \"SaveLoadRequestCoordinator\": false, \"ModOwnerLedger\": false, \"ModLoadTransaction\": false, \"OwnerBoundInput\": false, \"EventHandlerQuarantine\": false, \"ConfigPreviewAudit\": false, \"GameBridgeFeatureContracts\": false, \"GameBridgeFeatureUpdateBuckets\": false, \"GameBridgeFinalHealthSnapshot\": false, \"ContentManifestRegistry\": false }");

                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                runtime.Start();

                Assert(!runtime.RefactorOptions.LifecycleObservation, "Runtime should read lifecycle observation disabled from config.");
                Assert(!runtime.RefactorOptions.ShadowContentRegistry, "Runtime should read shadow registry disabled from config.");
                Assert(!runtime.RefactorOptions.ResourceLifecycleLedger, "Runtime should read resource lifecycle ledger disabled from config.");
                Assert(!runtime.RefactorOptions.ResourceLifecycleCleanup, "Runtime should read resource lifecycle cleanup disabled from config.");
                Assert(!runtime.RefactorOptions.HookInstallScheduler, "Runtime should read hook install scheduler disabled from config.");
                Assert(!runtime.RefactorOptions.HookStatusQueue, "Runtime should read hook status queue disabled from config.");
                Assert(!runtime.RefactorOptions.EventMainThreadBoundary, "Runtime should read event main-thread boundary disabled from config.");
                Assert(!runtime.RefactorOptions.HookReadinessLayers, "Runtime should read hook readiness layers disabled from config.");
                Assert(!runtime.RefactorOptions.SaveLoadRequestCoordinator, "Runtime should read SaveLoad request coordinator disabled from config.");
                Assert(!runtime.RefactorOptions.ModOwnerLedger, "Runtime should read mod owner ledger disabled from config.");
                Assert(runtime.RefactorOptions.ModLoadTransaction, "Mod load transaction correctness must remain enforced when legacy config requests false.");
                Assert(runtime.RefactorOptions.OwnerBoundInput, "Owner-bound input correctness must remain enforced when legacy config requests false.");
                Assert(runtime.RefactorOptions.EventHandlerQuarantine, "Event quarantine delegate release must remain enforced when legacy config requests false.");
                Assert(runtime.CreateSnapshot().Warnings.Any(w => w.Message.Contains("correctness is enforced", StringComparison.OrdinalIgnoreCase)), "Ignored legacy false values should be reported as enforced.");
                Assert(!runtime.RefactorOptions.ConfigPreviewAudit, "Runtime should read config preview audit disabled from config.");
                Assert(!runtime.RefactorOptions.GameBridgeFeatureContracts, "Runtime should read GameBridge feature contract diagnostics disabled from config.");
                Assert(!runtime.RefactorOptions.GameBridgeFinalHealthSnapshot, "Runtime should read GameBridge final health snapshot disabled from config.");
                Assert(!runtime.RefactorOptions.ContentManifestRegistry, "Runtime should read content/manifest registry authoritative index disabled from config.");
                Assert(runtime.LifecycleObservationSnapshot.Entries.Count == 0, "Disabled lifecycle observation must not record phase entries.");
                Assert(runtime.LifecycleBoundaryContractSnapshot.Timeline.Count == 0, "Disabled lifecycle observation must not record lifecycle boundary contract entries.");
                Assert(runtime.ResourceLifecycleSnapshot.Timeline.Count == 0, "Disabled resource lifecycle ledger must not record entries.");
                Assert(runtime.ModOwnerLedgerSnapshot.Entries.Count == 0, "Disabled mod owner ledger must not record owner lifecycle entries.");
                runtime.NotifyLoadGameRequested(2);
                Assert(runtime.SaveLoadRequestSnapshot.Requests.Count == 0, "Disabled SaveLoad request coordinator must not record request entries.");
                Assert(runtime.LatestShadowContentRegistrySnapshot == null, "Disabled shadow content registry must not build a snapshot.");
                Assert(runtime.LatestContentManifestRegistrySnapshot == null, "Disabled content/manifest registry must not build a snapshot.");
                Assert(runtime.Diagnostics.GetFeatureStatuses().Any(f => f.FeatureId == "Refactor.LifecycleObservation" && f.Status == "disabled"), "Disabled lifecycle observation should be reported through feature status.");
                Assert(runtime.Diagnostics.GetFeatureStatuses().Any(f => f.FeatureId == "Refactor.LifecycleBoundaryContract" && f.Status == "disabled"), "Disabled lifecycle boundary contract should be reported through feature status.");
                Assert(runtime.Diagnostics.GetFeatureStatuses().Any(f => f.FeatureId == "Refactor.ShadowContentRegistry" && f.Status == "disabled"), "Disabled shadow registry should be reported through feature status.");
                Assert(runtime.Diagnostics.GetFeatureStatuses().Any(f => f.FeatureId == "Refactor.ResourceLifecycleLedger" && f.Status == "disabled"), "Disabled resource ledger should be reported through feature status.");
                Assert(runtime.Diagnostics.GetFeatureStatuses().Any(f => f.FeatureId == "Refactor.HookStatusQueue" && f.Status == "disabled"), "Disabled hook status queue should be reported through feature status.");
                Assert(runtime.Diagnostics.GetFeatureStatuses().Any(f => f.FeatureId == "Refactor.EventMainThreadBoundary" && f.Status == "disabled"), "Disabled event main-thread boundary should be reported through feature status.");
                Assert(runtime.Diagnostics.GetFeatureStatuses().Any(f => f.FeatureId == "Refactor.HookReadinessLayers" && f.Status == "disabled"), "Disabled hook readiness layers should be reported through feature status.");
                Assert(runtime.Diagnostics.GetFeatureStatuses().Any(f => f.FeatureId == "Refactor.SaveLoadRequestCoordinator" && f.Status == "disabled"), "Disabled SaveLoad coordinator should be reported through feature status.");
                Assert(runtime.Diagnostics.GetFeatureStatuses().Any(f => f.FeatureId == "Refactor.ModOwnerLifecycle" && f.Status == "disabled"), "Disabled mod owner lifecycle should be reported through feature status.");
                Assert(runtime.Diagnostics.GetFeatureStatuses().Any(f => f.FeatureId == "Refactor.GameBridgeFeatureContracts" && f.Status == "disabled"), "Disabled GameBridge feature contracts should be reported through feature status.");
                Assert(!runtime.Diagnostics.GetFeatureStatuses().Any(f => f.FeatureId == "Refactor.GameBridgeFeatureScheduler"), "The retired bucket scheduler must not publish a compatibility status.");
                Assert(runtime.Diagnostics.GetFeatureStatuses().Any(f => f.FeatureId == "Refactor.GameBridgeFinalHealthSnapshot" && f.Status == "disabled"), "Disabled GameBridge final health snapshot should be reported through feature status.");
                Assert(runtime.Diagnostics.GetFeatureStatuses().Any(f => f.FeatureId == "Refactor.ContentManifestRegistry" && f.Status == "disabled"), "Disabled content/manifest registry should be reported through feature status.");
                Assert(!File.ReadAllText(Path.Combine(configDir, "refactor-scaffold.json")).Contains("GameBridgeFeatureUpdateBuckets", StringComparison.Ordinal),
                    "Loading an old refactor-scaffold config must rewrite away the retired feature bucket option.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void HookStatusQueueRecordsSnapshotImmediatelyAndPublishesOnFlush()
        {
            var boundedQueue = new HookStatusPublicationQueue(maxPending: 8);
            string commonPrefix = new string('H', 300);
            boundedQueue.Enqueue(commonPrefix + "A", new string('S', 400), new string('O', 400), new string('D', 4_000), new string('P', 400), 2, 1);
            HookStatusQueueSnapshot boundedSnapshot = boundedQueue.Enqueue(commonPrefix + "B", "ready", "source", "details", "phase", 2, 1);
            Assert(boundedSnapshot.Pending == 2 && boundedSnapshot.TrimmedBytes > 0, "Bounded Hook status identities must retain distinct hash-suffixed keys and report scalar trimming.");
            IReadOnlyList<HookStatusPublication> boundedPublications = boundedQueue.Drain();
            Assert(boundedPublications.Count == 2 && boundedPublications.All(item => item.HookId.Length <= BoundedDiagnosticScalar.IdentifierChars && item.Details.Length <= BoundedDiagnosticScalar.DetailsChars), "Every retained Hook status scalar must obey its fixed bound.");

            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                runtime.Start();
                IEventsHelper events = CreateEventsProxy(runtime, "DTMAPI.Tests.HookStatusQueue");
                int published = 0;
                events.Diagnostics.HookStatusChanged += (_, e) =>
                {
                    if (e.HookId == "Unit.HookStatusQueue")
                        published++;
                };

                runtime.SetHookStatus("Unit.HookStatusQueue", "ready", "DTMAPI.UnitTests", "queued until runtime flush");

                IHookStatusInfo status = runtime.Diagnostics.GetHookStatuses().Single(s => s.HookId == "Unit.HookStatusQueue");
                Assert(status.Status == "ready", "Hook status diagnostics snapshot should update before publication flush.");
                Assert(published == 0, "HookStatusChanged event should not publish before runtime queue flush.");
                Assert(runtime.HookStatusQueueSnapshot.Pending >= 1, "Hook status queue should contain the pending publication.");

                runtime.FlushRuntimeQueues("UnitTest.HookStatusQueue");

                Assert(published == 1, "HookStatusChanged event should publish on runtime queue flush.");
                Assert(runtime.HookStatusQueueSnapshot.Pending == 0, "Hook status queue should be empty after flush.");
                Assert(runtime.Diagnostics.GetFeatureStatuses().Any(f => f.FeatureId == "Refactor.HookStatusQueue" && f.Success), "Hook status queue should report a successful feature status.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void EventMainThreadBoundaryQueuesSafeEventsAndRejectsUnsafeEvents()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                IEventsHelper events = CreateEventsProxy(runtime, "DTMAPI.Tests.EventBoundary");
                int saveLoaded = 0;
                int updates = 0;
                events.Save.SaveLoaded += (_, _) => saveLoaded++;
                events.GameLoop.UpdateTicked += (_, _) => updates++;
                runtime.Start();

                Exception? threadError = null;
                var saveThread = new Thread(() =>
                {
                    try
                    {
                        runtime.NotifyLoadGameRequested(3);
                        runtime.NotifySaveLoaded(isNewGame: false);
                    }
                    catch (Exception ex)
                    {
                        threadError = ex;
                    }
                });
                saveThread.Start();
                saveThread.Join();
                if (threadError != null)
                    throw threadError;

                EventDispatchBoundarySnapshot producerSnapshot = runtime.EventDispatchBoundarySnapshot;
                Assert(saveLoaded == 0, "Off-thread SaveLoaded must be rejected before runtime state or public event dispatch.");
                Assert(producerSnapshot.TotalQueued == 0 && producerSnapshot.Queued == 0, "Runtime lifecycle producers must not enqueue only the public tail after mutating state off-thread.");
                Assert(producerSnapshot.TotalRejected >= 2, "LoadGameRequested and SaveLoaded producer-entry gates should both record rejection.");
                Assert(runtime.SaveLoadRequestSnapshot.SaveLoadedDispatchCount == 0, "Rejected SaveLoaded must not mutate save-load coordination state.");

                runtime.FlushRuntimeQueues("UnitTest.EventBoundary.Safe");
                Assert(saveLoaded == 0, "A rejected off-thread lifecycle producer must not appear during a later flush.");

                var updateThread = new Thread(() =>
                {
                    try
                    {
                        runtime.Update();
                        runtime.RecordInputFrame(new[] { new InputButtonSample("Jump", true, true, false) });
                    }
                    catch (Exception ex)
                    {
                        threadError = ex;
                    }
                });
                updateThread.Start();
                updateThread.Join();
                if (threadError != null)
                    throw threadError;

                EventDispatchBoundarySnapshot rejectedSnapshot = runtime.EventDispatchBoundarySnapshot;
                Assert(updates == 0, "Off-thread Update/Input must be rejected and must not dispatch mod callbacks.");
                Assert(rejectedSnapshot.TotalRejected >= 4, "Event boundary should record rejected lifecycle, Update, and Input producers.");

                runtime.Update();
                Assert(updates == 1, "Runtime-thread Update should still dispatch ordinary mod callbacks.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void SaveLoadRequestCoordinatorSuppressesOnlyDtmapiDuplicates()
        {
            string runtimeSource = File.ReadAllText(Path.Combine(
                FindRepositoryRoot(),
                "src",
                "DTMAPI.Core",
                "Runtime",
                "DtmApiRuntime.cs"));
            Assert(!runtimeSource.Contains("fatal-window state", StringComparison.OrdinalIgnoreCase),
                "The coordinator diagnostics must not advertise the removed internal fatal-window state.");

            int threadId = Thread.CurrentThread.ManagedThreadId;
            var service = new SaveLoadRequestCoordinatorService();

            SaveLoadRequestDecision first = service.TryBeginDtmapiRequest(2, "DTMAPI.Smoke", "FirstFallback", threadId, "TitleObserved");
            Assert(!first.Suppressed, "First DTMAPI/smoke LoadGame request should be accepted.");
            Assert(first.Update.Snapshot.ActiveRequestId == "SL-0001", "First request should get a stable request id.");

            SaveLoadRequestDecision duplicate = service.TryBeginDtmapiRequest(2, "DTMAPI.Smoke", "SecondFallback", threadId, "TitleObserved");
            Assert(duplicate.Suppressed, "Duplicate DTMAPI/smoke LoadGame request for the active slot should be suppressed.");
            Assert(duplicate.Update.Snapshot.DuplicateRequests == 1, "Suppressed duplicate should be counted.");
            Assert(duplicate.Update.Snapshot.SuppressedDuplicateRequests == 1, "Suppressed duplicate count should be tracked separately.");
            Assert(!duplicate.Update.Snapshot.HasUnsuppressedDuplicates, "Suppressed DTMAPI duplicates should not be treated as unsuppressed native duplicates.");

            SaveLoadRequestUpdate nativeEnter = service.RecordNativeEnter(2, "NativeGame", "Harmony Prefix", threadId, "Update");
            Assert(nativeEnter.Snapshot.Requests.Count == 1, "Native enter should attach to the active DTMAPI request instead of creating a second request.");
            Assert(nativeEnter.Snapshot.Requests[0].NativeEntered, "Native enter should be recorded on the active request.");

            service.RecordNativeReturn(2, true, "NativeGame", "Harmony Postfix", threadId, "Update");
            SaveLoadRequestUpdate saveLoaded = service.RecordSaveLoaded(2, false, "NativeGame", "AfterLoadArchiveData", threadId, "SaveLoaded");
            SaveLoadRequestEntry closed = saveLoaded.Snapshot.Requests.Last();
            Assert(closed.Completed, "SaveLoaded should close the active request.");
            Assert(closed.SaveLoadedDispatched, "SaveLoaded dispatch should be recorded.");
            Assert(!saveLoaded.Snapshot.HasActiveRequest, "No active request should remain after SaveLoaded.");

            var delayedReturnService = new SaveLoadRequestCoordinatorService();
            delayedReturnService.RecordNativeEnter(4, "NativeGame", "Harmony Prefix", threadId, "TitleObserved");
            delayedReturnService.RecordSaveLoaded(4, false, "NativeGame", "AfterLoadArchiveData", threadId, "SaveLoaded");
            SaveLoadRequestUpdate delayedReturn = delayedReturnService.RecordNativeReturn(4, true, "NativeGame", "Harmony Postfix", threadId, "SaveLoaded");
            Assert(delayedReturn.Snapshot.Requests.Count == 1, "Native return after SaveLoaded should attach to the completed request instead of creating a second request.");
            Assert(delayedReturn.Snapshot.Requests[0].NativeReturned, "Delayed native return should still be recorded on the closed request.");
            Assert(delayedReturn.Snapshot.Requests[0].Status == SaveLoadRequestStatus.SaveLoaded, "SaveLoaded should remain the boundary status when native return is observed after dispatch.");

            var failedService = new SaveLoadRequestCoordinatorService();
            failedService.RecordNativeEnter(0, "NativeGame", "Harmony Prefix", threadId, "Update");
            SaveLoadRequestSnapshot failed = failedService.RecordNativeReturn(0, false, "NativeGame", "Harmony Postfix", threadId, "Update").Snapshot;
            Assert(!failed.HasActiveRequest && failed.Requests.Single().Completed, "A failed native load must release the pending request without waiting for SaveLoaded.");
            Assert(failed.Requests.Single().Status == SaveLoadRequestStatus.NativeFailed && !failed.Requests.Single().SaveLoadedDispatched, "A native false return must remain a failure, never a successful save event.");
            SaveLoadRequestDecision retry = failedService.TryBeginDtmapiRequest(0, "DTMAPI.Smoke", "Retry", threadId, "Update");
            Assert(!retry.Suppressed && retry.RequestId != failed.Requests.Single().RequestId, "A failed native request must not suppress a same-slot retry.");
            failedService.RecordNativeEnter(0, "NativeGame", "Harmony Prefix", threadId, "Update");
            failedService.RecordSaveLoaded(0, false, "NativeGame", "AfterLoadArchiveData", threadId, "SaveLoaded");
            SaveLoadRequestSnapshot recovered = failedService.RecordNativeReturn(0, true, "NativeGame", "Harmony Postfix", threadId, "SaveLoaded").Snapshot;
            Assert(recovered.Requests.Count == 2 && recovered.Requests[0].Status == SaveLoadRequestStatus.NativeFailed && recovered.Requests[1].Status == SaveLoadRequestStatus.SaveLoaded && !recovered.HasActiveRequest, "Successful retry must retain the prior failure as a separate terminal request.");

            var timedOutService = new SaveLoadRequestCoordinatorService();
            timedOutService.RecordNativeEnter(0, "NativeGame", "Harmony Prefix", threadId, "Update");
            timedOutService.RecordTimeout("DTMAPI.Smoke", "Deadline", "Timed out", threadId, "Update");
            SaveLoadRequestSnapshot lateReturn = timedOutService.RecordNativeReturn(0, false, "NativeGame", "Harmony Postfix", threadId, "Update").Snapshot;
            Assert(lateReturn.Requests.Single().Status == SaveLoadRequestStatus.Timeout && !lateReturn.HasActiveRequest, "A late native return must not rewrite an existing timeout terminal status.");

            service.RecordNativeEnter(3, "NativeGame", "Harmony Prefix", threadId, "TitleObserved");
            SaveLoadRequestUpdate duplicateNative = service.RecordNativeEnter(3, "NativeGame", "Harmony Prefix", threadId, "TitleObserved");
            Assert(duplicateNative.Snapshot.DuplicateRequests == 2, "Native duplicate should be diagnosed without suppression.");
            Assert(duplicateNative.Snapshot.SuppressedDuplicateRequests == 1, "Native duplicate should not increase suppressed duplicate count.");
            Assert(duplicateNative.Snapshot.HasUnsuppressedDuplicates, "Native duplicate should remain visible as an unsuppressed duplicate diagnostic.");
            Assert(duplicateNative.Snapshot.Status == "warning", "Unsuppressed native duplicate should make the coordinator warning.");
        }

        private static void TitleReturnBoundaryLedgerBuildsObjectGraphDeltas()
        {
            var service = new TitleReturnBoundaryLedgerService();
            service.RecordEvent("BeforeNextLoadGame", "unit", "first load", 2, "Update", "HomePageUiState", "requests=0", "records=1; SaveLifetime=0", "records=1", 1);
            service.RecordObjectGraphSnapshot(
                "BeforeNextLoadGame",
                "first load",
                "unit",
                2,
                "Update",
                "HomePageUiState",
                "requests=0; active=none",
                "records=1; SaveLifetime=0",
                "records=1; activeTransactions=0",
                new[]
                {
                    new TitleReturnObjectGraphSection("GameBridge", "saveSlots={saveUiStates=1; saveUiBinders=0}; audioReplacement={entries=11; pendingRequests=0}"),
                    new TitleReturnObjectGraphSection("BootstrapUi", "bootstrap={fallbackPumpAlive=true; fallbackTickQueued=0}; titleSettings={renderedObjects=0}")
                });

            TitleReturnBoundarySnapshot nativeEnter = service.RecordObjectGraphSnapshot(
                "LoadGameNativeEnter",
                "native enter",
                "unit",
                2,
                "Update",
                "HomePageUiState",
                "requests=1; active=SL-0001",
                "records=2; SaveLifetime=1",
                "records=4; activeTransactions=0; byOwner={Yuuka.DTMAPI.YKeyConsole={records=3; EventHandler=1; InputButton=2; ConfigPage=1; LoadedCodeMod=1}}",
                new[]
                {
                    new TitleReturnObjectGraphSection("GameBridge", "saveSlots={saveUiStates=1; saveUiBinders=1}; audioReplacement={entries=11; pendingRequests=0}"),
                    new TitleReturnObjectGraphSection("UI", "byOwner={Yuuka.DTMAPI.YKeyConsole={Canvas=1; EventSystem=1; Button=2; InputField=1; ScrollRect=0; UnityEventListeners=3; DynamicBinders=3; rootAlive=1}}"),
                    new TitleReturnObjectGraphSection("BootstrapUi", "bootstrap={fallbackPumpAlive=true; fallbackTickQueued=0}; titleSettings={renderedObjects=0}")
                });

            TitleReturnObjectGraphDeltaEntry? nativeEnterDelta = nativeEnter.LatestObjectGraphDelta;
            Assert(nativeEnterDelta != null, "Native-enter snapshot should produce a delta entry.");
            TitleReturnObjectGraphDeltaEntry nativeEnterDeltaValue = nativeEnterDelta!;
            Assert(nativeEnterDeltaValue.PreviousSnapshotDeltas.Any(delta => delta.Key == "GameBridge.saveSlots.saveUiBinders" && delta.PreviousValue == 0 && delta.CurrentValue == 1 && delta.Delta == 1), "Previous-snapshot delta should include save UI binder growth.");
            Assert(nativeEnterDeltaValue.Metrics.TryGetValue("ModOwner.byOwner.Yuuka.DTMAPI.YKeyConsole.InputButton", out long yConsoleButtons) && yConsoleButtons == 2, "Object graph metrics should include ModOwner per-owner input button counts.");
            Assert(nativeEnterDeltaValue.Metrics.TryGetValue("UI.byOwner.Yuuka.DTMAPI.YKeyConsole.DynamicBinders", out long yConsoleBinders) && yConsoleBinders == 3, "Object graph metrics should include UI per-owner dynamic binder counts.");
            Assert(nativeEnterDeltaValue.Format().Contains("ownerNonZero={", StringComparison.Ordinal) &&
                nativeEnterDeltaValue.Format().Contains("UI.byOwner.Yuuka.DTMAPI.YKeyConsole.DynamicBinders=3", StringComparison.Ordinal),
                "Object graph delta formatting should expose per-owner non-zero metrics without relying on the generic top-N summary.");

            service.RecordEvent("SaveLoaded", "unit", "save loaded", 2, "SaveLoaded", "Gameplay", "requests=1; saveLoaded=1", "records=3; SaveLifetime=2", "records=1", 1);
            service.RecordObjectGraphSnapshot(
                "SaveLoaded",
                "save loaded",
                "unit",
                2,
                "SaveLoaded",
                "Gameplay",
                "requests=1; saveLoaded=1",
                "records=3; SaveLifetime=2",
                "records=1; activeTransactions=0",
                new[]
                {
                    new TitleReturnObjectGraphSection("GameBridge", "saveSlots={saveUiStates=0; saveUiBinders=0}; audioReplacement={entries=11; pendingRequests=0}"),
                    new TitleReturnObjectGraphSection("BootstrapUi", "bootstrap={fallbackPumpAlive=true; fallbackTickQueued=0}; titleSettings={renderedObjects=0}")
                });

            service.RecordEvent("ReturnHomeRequested", "unit", "return home", 2, "Update", "Gameplay", "requests=1", "records=3; SaveLifetime=2", "records=1", 1);
            service.RecordObjectGraphSnapshot(
                "BeforeNextLoadGame",
                "second load",
                "unit",
                2,
                "Update",
                "HomePageUiState",
                "requests=2; active=SL-0002",
                "records=4; SaveLifetime=3",
                "records=1; activeTransactions=0",
                new[]
                {
                    new TitleReturnObjectGraphSection("GameBridge", "saveSlots={saveUiStates=2; saveUiBinders=0}; audioReplacement={entries=11; pendingRequests=0}"),
                    new TitleReturnObjectGraphSection("BootstrapUi", "bootstrap={fallbackPumpAlive=true; fallbackTickQueued=0}; titleSettings={renderedObjects=0}")
                });

            TitleReturnBoundarySnapshot secondEnter = service.RecordObjectGraphSnapshot(
                "LoadGameNativeEnter",
                "second native enter",
                "unit",
                2,
                "Update",
                "HomePageUiState",
                "requests=2; active=SL-0002",
                "records=5; SaveLifetime=4",
                "records=1; activeTransactions=0",
                new[]
                {
                    new TitleReturnObjectGraphSection("GameBridge", "saveSlots={saveUiStates=2; saveUiBinders=0}; audioReplacement={entries=11; pendingRequests=1}"),
                    new TitleReturnObjectGraphSection("BootstrapUi", "bootstrap={fallbackPumpAlive=true; fallbackTickQueued=0}; titleSettings={renderedObjects=0}")
                });

            TitleReturnObjectGraphDeltaEntry? latest = secondEnter.LatestObjectGraphDelta;
            Assert(latest != null, "Latest snapshot should produce a delta entry.");
            TitleReturnObjectGraphDeltaEntry latestValue = latest!;
            Assert(latestValue.SameBoundaryDeltas.Any(delta => delta.Key == "GameBridge.audioReplacement.pendingRequests" && delta.PreviousValue == 0 && delta.CurrentValue == 1 && delta.Delta == 1), "Same-boundary delta should compare LoadGameNativeEnter across cycles.");
            Assert(secondEnter.FormatLatestDeltas().Contains("sameBoundaryDelta", StringComparison.Ordinal), "Runtime report delta summary should include same-boundary deltas.");
        }

        private static void LifecycleObservationCountersRecordPhases()
        {
            var service = new LifecycleObservationService();
            service.Record("Startup", 7, null, null, 1, 0, 2);
            service.Record("SaveLoaded", 7, 2, false, 3, 2, 4);
            service.Record("SaveLoaded", 7, 2, false, 3, 2, 4);
            LifecycleObservationSnapshot snapshot = service.GetSnapshot();
            Assert(snapshot.Counts["Startup"] == 1, "Startup phase should be counted.");
            Assert(snapshot.Counts["SaveLoaded"] == 2, "Every ordinary SaveLoaded phase should use one repeatable classification.");
            Assert(snapshot.Entries.Last().Phase == "SaveLoaded", "Lifecycle entries should preserve the repeatable SaveLoaded phase.");
            Assert(service.FormatSummary().Contains("SaveLoaded=2"), "Lifecycle summary should count repeated save loads without inventing a second-load phase.");
        }

        private static void LifecycleBoundaryContractTracksPhaseRegistryAndHookDiagnostics()
        {
            var service = new LifecycleBoundaryContractService();
            LifecycleBoundaryContractUpdate startup = service.RecordPhase(RuntimeLifecyclePhase.Startup, null, null, 5, 0, 2);
            Assert(startup.Snapshot.Status == "ok", "Initial lifecycle boundary phase should be ok.");

            LifecycleBoundaryContractUpdate firstRegistry = service.RecordRegistryRefresh("DiscoverMods", 5, 0, 1, 2);
            Assert(firstRegistry.Snapshot.Status == "ok", "First registry refresh for a phase/reason should be ok.");
            LifecycleBoundaryContractUpdate duplicateRegistry = service.RecordRegistryRefresh("DiscoverMods", 5, 0, 1, 2);
            Assert(duplicateRegistry.NewDiagnostics.Any(d => d.Message.Contains("Shadow registry refresh repeated", StringComparison.Ordinal)), "Duplicate registry refresh should produce a diagnostic.");

            LifecycleBoundaryContractUpdate firstHook = service.RecordHookStatus("Save.SaveLoaded", "experimental", "Harmony Postfix: DolocAPI.AfterLoadArchiveData", "Hook installed.");
            Assert(!firstHook.NewDiagnostics.Any(), "First hook install signal should not warn.");
            LifecycleBoundaryContractUpdate duplicateHook = service.RecordHookStatus("Save.SaveLoaded", "experimental", "Harmony Postfix: DolocAPI.AfterLoadArchiveData", "Hook installed.");
            Assert(duplicateHook.NewDiagnostics.Any(d => d.Message.Contains("Hook install signal repeated", StringComparison.Ordinal)), "Repeated hook install signal should be diagnosed.");

            LifecycleBoundaryContractUpdate resourceEvent = service.RecordResourceEvent("AudioReplacement", "LocalWavReady", "DTMAPI.HatchAssets", "hatch-pet-child", "event=PLAY_ANIMAL_PET_CHICKEN_CHILD");
            Assert(!resourceEvent.NewDiagnostics.Any(), "Resource observations should be counted without producing diagnostics in phase two.");

            LifecycleBoundaryContractSnapshot snapshot = service.GetSnapshot();
            Assert(snapshot.Status == "warning", "Duplicate registry and hook observations should leave the contract in warning status.");
            Assert(snapshot.RegistryRefreshCounts[RuntimeLifecyclePhase.Startup + "::DiscoverMods"] == 2, "Registry refresh counts should be grouped by phase and reason.");
            Assert(snapshot.HookInstallSignalCounts["Save.SaveLoaded"] == 2, "Hook install signal count should be grouped by hook id.");
            Assert(snapshot.ResourceEventCounts[RuntimeLifecyclePhase.Startup + "::AudioReplacement::LocalWavReady"] == 1, "Resource event counts should be grouped by phase, category, and event.");
            Assert(snapshot.FormatSummary().Contains("warnings=2", StringComparison.Ordinal), "Contract summary should include warning counts.");
            Assert(snapshot.FormatSummary().Contains("resources=Startup::AudioReplacement::LocalWavReady=1", StringComparison.Ordinal), "Contract summary should include resource counts.");
            Assert(service.FormatPolicyCatalog().Contains("ReturnedToTitle", StringComparison.Ordinal), "Policy catalog should declare returned-to-title boundaries.");
        }

        private static void LifecycleBoundaryContractAllowsRepeatedSaveLoadedPhases()
        {
            var contract = new LifecycleBoundaryContractService();
            var resources = new ResourceLifecycleLedgerService();
            contract.RecordPhase(RuntimeLifecyclePhase.Startup, null, null, 0, 0, 0);
            resources.RecordPhase(RuntimeLifecyclePhase.Startup);

            for (int ordinal = 1; ordinal <= 5; ordinal++)
            {
                LifecycleBoundaryContractUpdate loaded = contract.RecordPhase(RuntimeLifecyclePhase.SaveLoaded, 2, false, 1, 1, 1);
                Assert(!loaded.NewDiagnostics.Any(), "A normal repeated SaveLoaded boundary must not be diagnosed as a duplicate at ordinal " + ordinal + ".");
                resources.RecordPhase(RuntimeLifecyclePhase.SaveLoaded);
                ResourceLifecycleLedgerUpdate opened = resources.BeginSaveGeneration(RuntimeLifecyclePhase.SaveLoaded, 2, false);
                Assert(opened.Snapshot.SaveGeneration == ordinal && opened.Snapshot.SaveGenerationOpen, "Every repeated SaveLoaded must still open exactly one resource save generation.");

                LifecycleBoundaryContractUpdate returned = contract.RecordPhase(RuntimeLifecyclePhase.ReturnedToTitle, null, null, 1, 1, 1);
                Assert(!returned.NewDiagnostics.Any(), "A normal repeated ReturnedToTitle boundary must remain diagnostic-free at ordinal " + ordinal + ".");
                resources.RecordPhase(RuntimeLifecyclePhase.ReturnedToTitle);
                ResourceLifecycleLedgerUpdate closed = resources.CloseSaveGeneration(RuntimeLifecyclePhase.ReturnedToTitle);
                Assert(!closed.Snapshot.SaveGenerationOpen, "ReturnedToTitle must close the resource save generation for ordinal " + ordinal + ".");
            }

            LifecycleBoundaryContractSnapshot snapshot = contract.GetSnapshot();
            Assert(snapshot.PhaseCounts[RuntimeLifecyclePhase.SaveLoaded] == 5 && snapshot.PhaseCounts[RuntimeLifecyclePhase.ReturnedToTitle] == 5, "The lifecycle contract must count five complete load/title cycles without special phase names.");
            Assert(!snapshot.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("SaveLoaded", StringComparison.OrdinalIgnoreCase)), "Repeated SaveLoaded phases must not leave a retained false-positive diagnostic.");
            Assert(!contract.FormatPolicyCatalog().Contains("SecondSaveLoaded", StringComparison.OrdinalIgnoreCase), "The internal policy catalog must not retain the removed second-load classification.");
        }

        private static void ResourceLifecycleLedgerTracksGenerationsOwnershipAndCleanup()
        {
            var service = new ResourceLifecycleLedgerService();
            service.RecordPhase(RuntimeLifecyclePhase.Startup);
            ResourceLifecycleLedgerUpdate firstContent = service.ObserveContentSignature("initial", "content-a");
            Assert(firstContent.Snapshot.ContentGeneration == 1, "Initial non-empty content signature should open content generation 1.");
            ResourceLifecycleLedgerUpdate unchangedContent = service.ObserveContentSignature("unchanged", "content-a");
            Assert(unchangedContent.Snapshot.ContentGeneration == 1, "Unchanged content signature must not advance content generation.");
            ResourceLifecycleLedgerUpdate changedContent = service.ObserveContentSignature("changed", "content-b");
            Assert(changedContent.Snapshot.ContentGeneration == 2, "Changed content signature should advance content generation.");

            service.RecordResource(
                "WavRequest",
                "hatch-pet-child",
                "DTMAPI.HatchAssets",
                "Content/Audio/hatch.wav",
                ResourceLifetime.TitleLifetime,
                ResourceOwnership.DtmapiOwned,
                ResourceLifecycleStatus.Acquired,
                "dispose-on-content-generation-replacement");

            service.RecordResource(
                "TemplateAnimatorController",
                "game_anim_animal_chicken_child",
                "DTMAPI.HatchAssets",
                string.Empty,
                ResourceLifetime.TitleLifetime,
                ResourceOwnership.BorrowedNative,
                ResourceLifecycleStatus.Ready,
                "borrowed-native-never-release");

            service.RecordPhase(RuntimeLifecyclePhase.SaveLoaded);
            ResourceLifecycleLedgerUpdate save = service.BeginSaveGeneration("SaveLoaded", 7, false);
            Assert(save.Snapshot.SaveGeneration == 1 && save.Snapshot.SaveGenerationOpen, "SaveLoaded should open save generation 1.");
            service.RecordResource(
                "AnimalSoundContext",
                "hatch:child:PLAY_ANIMAL_PET_CHICKEN_CHILD",
                "DTMAPI.GameBridge.AudioReplacement",
                string.Empty,
                ResourceLifetime.SaveLifetime,
                ResourceOwnership.DtmapiOwned,
                ResourceLifecycleStatus.Acquired,
                "clear-on-save-boundary");

            service.RecordPhase(RuntimeLifecyclePhase.ReturnedToTitle);
            ResourceLifecycleLedgerUpdate closeSave = service.CloseSaveGeneration("ReturnedToTitle");
            Assert(!closeSave.Snapshot.SaveGenerationOpen, "ReturnedToTitle should close the active save generation.");
            ResourceLifecycleLedgerUpdate cleanup = service.RecordCleanup("AudioReplacement", "ReturnedToTitle", 1, "state=AnimalSoundContextStack");
            Assert(cleanup.Snapshot.FormatReturnedToTitleCleanupPlan().Contains("AudioReplacement", StringComparison.Ordinal), "Cleanup plan should record the owner area.");

            service.RecordRefresh("CustomAnimals", "SaveLoaded", ResourceLifecycleStatus.Rebuilt, 2);
            ResourceLifecycleLedgerUpdate duplicateRebuild = service.RecordRefresh("CustomAnimals", "SaveLoaded", ResourceLifecycleStatus.Rebuilt, 2);
            Assert(duplicateRebuild.NewDiagnostics.Any(d => d.Message.Contains("rebuilt more than once", StringComparison.Ordinal)), "Duplicate rebuild in one phase/generation should be diagnosed.");

            service.RecordPhase(RuntimeLifecyclePhase.TitleObserved);
            service.RecordResource(
                "WavRequest",
                "hatch-pet-child",
                "DTMAPI.HatchAssets",
                "Content/Audio/hatch.wav",
                ResourceLifetime.TitleLifetime,
                ResourceOwnership.DtmapiOwned,
                ResourceLifecycleStatus.Acquired,
                "dispose-on-content-generation-replacement");
            ResourceLifecycleLedgerUpdate repeatedTitleIdleAcquire = service.RecordResource(
                "WavRequest",
                "hatch-pet-child",
                "DTMAPI.HatchAssets",
                "Content/Audio/hatch.wav",
                ResourceLifetime.TitleLifetime,
                ResourceOwnership.DtmapiOwned,
                ResourceLifecycleStatus.Acquired,
                "dispose-on-content-generation-replacement");
            Assert(repeatedTitleIdleAcquire.NewDiagnostics.Any(d => d.Message.Contains("title idle", StringComparison.OrdinalIgnoreCase)), "Repeated title-idle acquisition should be diagnosed.");

            service.RecordPhase(RuntimeLifecyclePhase.SaveLoaded);
            ResourceLifecycleLedgerUpdate secondSave = service.BeginSaveGeneration("SaveLoaded", 7, false);
            Assert(secondSave.Snapshot.SaveGeneration == 2 && secondSave.Snapshot.SaveGenerationOpen, "Second SaveLoaded should open save generation 2.");
            service.RecordResource(
                "AnimalSoundContext",
                "hatch:adult:PLAY_ANIMAL_PET_CHICKEN",
                "DTMAPI.GameBridge.AudioReplacement",
                string.Empty,
                ResourceLifetime.SaveLifetime,
                ResourceOwnership.DtmapiOwned,
                ResourceLifecycleStatus.Acquired,
                "clear-on-save-boundary");

            service.RecordPhase(RuntimeLifecyclePhase.ReturnedToTitle);
            ResourceLifecycleLedgerUpdate secondCloseSave = service.CloseSaveGeneration("ReturnedToTitle");
            Assert(secondCloseSave.Snapshot.PrunedSaveLifetimeRecords == 1, "ReturnedToTitle should prune stale SaveLifetime diagnostic records from older save generations.");
            Assert(!secondCloseSave.Snapshot.Records.Any(r => r.Lifetime == ResourceLifetime.SaveLifetime && r.Generation == 1), "Old SaveLifetime diagnostic records should not accumulate across save generations.");
            Assert(secondCloseSave.Snapshot.Records.Any(r => r.Lifetime == ResourceLifetime.SaveLifetime && r.Generation == 2), "Current SaveLifetime diagnostic records should remain available for evidence.");

            ResourceLifecycleSnapshot snapshot = service.GetSnapshot();
            Assert(snapshot.Records.Any(r => r.Ownership == ResourceOwnership.DtmapiOwned && r.Lifetime == ResourceLifetime.SaveLifetime), "Ledger should retain DTMAPI-owned SaveLifetime records.");
            Assert(snapshot.Records.Any(r => r.Ownership == ResourceOwnership.BorrowedNative && r.ReleasePolicy.Contains("never-release", StringComparison.Ordinal)), "Ledger should preserve borrowed native never-release policy.");
            Assert(snapshot.FormatSummary().Contains("contentGeneration=2", StringComparison.Ordinal), "Summary should include content generation.");
            Assert(snapshot.FormatTitleIdleResourceGrowth().Contains("status=warning", StringComparison.Ordinal), "Title idle growth summary should reflect repeated acquisition diagnostics.");
        }

        private static void ResourceLifecycleLedgerSkipsDuplicateSkippedRefreshSnapshots()
        {
            var service = new ResourceLifecycleLedgerService();
            service.RecordPhase(RuntimeLifecyclePhase.SaveLoaded);

            ResourceLifecycleLedgerUpdate first = service.RecordRefresh("AutoFishing", "watchdog", ResourceLifecycleStatus.SkippedUnchanged, 4);
            int firstSnapshotBuilds = first.Snapshot!.SnapshotBuilds;
            Assert(first.ShouldPublish, "First skipped refresh sample should publish one diagnostic snapshot.");
            Assert(first.Snapshot.PublishCountsByArea.TryGetValue("AutoFishing", out int firstPublishCount) && firstPublishCount == 1, "First skipped refresh should count one AutoFishing publication.");

            ResourceLifecycleLedgerUpdate duplicate = service.RecordRefresh("AutoFishing", "watchdog", ResourceLifecycleStatus.SkippedUnchanged, 4);
            Assert(!duplicate.ShouldPublish, "Repeated skipped refresh with unchanged phase/area/result/count/contentGeneration should use the no-publish fast path.");

            int scalarBuildCount = service.CurrentSnapshotBuildCount;
            for (int i = 0; i < 10000; i++)
            {
                Assert(service.CurrentSnapshotBuildCount == scalarBuildCount,
                    "Reading the resource snapshot-build scalar must not construct a diagnostic snapshot.");
            }

            ResourceLifecycleSnapshot afterDuplicate = service.GetSnapshot();
            Assert(service.CurrentSnapshotBuildCount == scalarBuildCount + 1,
                "One explicit GetSnapshot call must advance the resource snapshot-build scalar exactly once.");
            Assert(afterDuplicate.SnapshotBuilds == firstSnapshotBuilds + 1, "Duplicate skipped refresh should not build a snapshot; only the explicit GetSnapshot call should increment the counter.");
            Assert(afterDuplicate.SkippedRefreshCalls == 2, "Ledger should count skipped refresh calls even when the duplicate does not publish.");
            Assert(afterDuplicate.SkippedRefreshFastPathCount == 1, "Ledger should count no-publish skipped refresh fast-path hits.");
            Assert(afterDuplicate.PublishCountsByArea.TryGetValue("AutoFishing", out int afterDuplicatePublishCount) && afterDuplicatePublishCount == 1, "Duplicate skipped refresh should not increase publish count.");

            ResourceLifecycleLedgerUpdate changedCount = service.RecordRefresh("AutoFishing", "watchdog", ResourceLifecycleStatus.SkippedUnchanged, 5);
            Assert(changedCount.ShouldPublish, "Skipped refresh with a changed resource count should publish a new sample.");
            Assert(changedCount.Snapshot!.PublishCountsByArea.TryGetValue("AutoFishing", out int finalPublishCount) && finalPublishCount == 2, "Changed skipped refresh sample should increase publish count.");
            Assert(changedCount.Snapshot.SkippedRefreshCalls == 3, "Skipped refresh call counter should include published and fast-path samples.");
        }

        private static void ResourceLifecycleLedgerAggregatesHighChurnAutoFishingHandles()
        {
            var service = new ResourceLifecycleLedgerService();
            service.RecordPhase(RuntimeLifecyclePhase.SaveLoaded);
            service.BeginSaveGeneration("SaveLoaded", 5, false);

            for (int i = 0; i < 150; i++)
            {
                string handleId = "handle-" + i.ToString("000", CultureInfo.InvariantCulture);
                service.RecordResource(
                    "AutoFishing.MiniGameHandle",
                    handleId,
                    "Yuuka.DTMAPI.AutoFishing",
                    "FishingGameScrollBar",
                    ResourceLifetime.SaveLifetime,
                    ResourceOwnership.BorrowedNative,
                    ResourceLifecycleStatus.Acquired,
                    "clear DTMAPI key/reference only; do not destroy native object",
                    observationMode: ResourceLifecycleObservationMode.AggregatedCurrentState,
                    aggregationKey: "fishing-minigame-handle");
                service.ReleaseResource(
                    "AutoFishing.MiniGameHandle",
                    handleId,
                    "Yuuka.DTMAPI.AutoFishing",
                    "FishingGameScrollBar",
                    ResourceLifetime.SaveLifetime,
                    ResourceOwnership.BorrowedNative,
                    "clear DTMAPI key/reference only; do not destroy native object",
                    ResourceLifecycleStatus.Released,
                    observationMode: ResourceLifecycleObservationMode.AggregatedCurrentState,
                    aggregationKey: "fishing-minigame-handle");
            }

            ResourceLifecycleSnapshot snapshot = service.GetSnapshot();
            ResourceLifecycleRecord aggregate = snapshot.Records.Single(r => r.ResourceKind == "AutoFishing.MiniGameHandle");
            Assert(snapshot.RecordCount == 1, "High-churn AutoFishing handles should aggregate to one ledger record instead of one record per native handle.");
            Assert(snapshot.AggregatedRecordCount == 1, "Generic aggregated record count should report caller-declared aggregate diagnostic records.");
            Assert(snapshot.ReleasedCurrentSaveRecords == 1, "Released current-save count should include the aggregate released borrowed-native handle record.");
            Assert(aggregate.ResourceId == "aggregate:fishing-minigame-handle", "Aggregate record should use the caller-declared stable aggregation key.");
            Assert(aggregate.ObservationCount == 150 && aggregate.ReleaseCount == 150, "Aggregate record should keep observed/released counters.");
            Assert(aggregate.ReleasePolicy.Contains("aggregationKey=fishing-minigame-handle", StringComparison.Ordinal) &&
                aggregate.ReleasePolicy.Contains("firstSample=handle-000", StringComparison.Ordinal) &&
                aggregate.ReleasePolicy.Contains("lastSample=handle-149", StringComparison.Ordinal) &&
                aggregate.ReleasePolicy.Contains("observed=150", StringComparison.Ordinal) &&
                aggregate.ReleasePolicy.Contains("released=150", StringComparison.Ordinal), "Aggregate release policy should retain first/last samples and counters.");
            Assert(snapshot.SnapshotBuilds < 25, "High-churn AutoFishing observe/release should be sampled instead of building a snapshot per handle event.");
            Assert(snapshot.PublishCountsByArea.TryGetValue("AutoFishing.MiniGameHandle", out int publishCount) && publishCount < 15, "High-churn AutoFishing publish count should be sampled, not linear with handle count.");
        }

        private static void RuntimeLifecycleBoundaryContractReportsAndRespectsFlag()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                runtime.Start();
                runtime.NotifyLoadGameRequested(2);
                runtime.NotifySaveLoaded(false);
                runtime.NotifyReturnedToTitle();
                string report = runtime.ExportLogs();

                LifecycleBoundaryContractSnapshot snapshot = runtime.LifecycleBoundaryContractSnapshot;
                Assert(snapshot.PhaseCounts.ContainsKey(RuntimeLifecyclePhase.Startup), "Runtime contract should record Startup.");
                Assert(snapshot.PhaseCounts.ContainsKey(RuntimeLifecyclePhase.TitleObserved), "Runtime contract should record TitleObserved.");
                Assert(snapshot.PhaseCounts.ContainsKey(RuntimeLifecyclePhase.SaveLoaded), "Runtime contract should record SaveLoaded.");
                Assert(snapshot.PhaseCounts.ContainsKey(RuntimeLifecyclePhase.ReturnedToTitle), "Runtime contract should record ReturnedToTitle.");
                Assert(snapshot.PhaseCounts.ContainsKey(RuntimeLifecyclePhase.LogExport), "Runtime contract should record LogExport.");
                Assert(!snapshot.Diagnostics.Any(d => d.Message.Contains("current loading slot", StringComparison.OrdinalIgnoreCase)), "Returned-to-title state should be observed after current loading slot cleanup.");
                Assert(runtime.Diagnostics.GetFeatureStatuses().Any(f => f.FeatureId == "Refactor.LifecycleBoundaryContract" && f.Status == "ok"), "Lifecycle boundary contract feature status should be ok for the normal unit path.");
                Assert(runtime.Diagnostics.GetFeatureStatuses().Any(f => f.FeatureId == "Refactor.ResourceLifecycleLedger" && f.Status == "ok"), "Resource lifecycle ledger feature status should be ok for the normal unit path.");

                string reportContext = ReadZipText(report, "DTMAPI-runtime-context.txt");
                Assert(reportContext.Contains("LifecycleBoundaryContract:", StringComparison.Ordinal), "Runtime report should include lifecycle boundary contract summary.");
                Assert(reportContext.Contains("LifecycleBoundaryPolicies:", StringComparison.Ordinal) && reportContext.Contains("ReturnedToTitle", StringComparison.Ordinal), "Runtime report should include lifecycle boundary policies.");
                Assert(reportContext.Contains("ResourceLifecycleLedgerSummary:", StringComparison.Ordinal), "Runtime report should include resource lifecycle ledger summary.");
                Assert(reportContext.Contains("ReturnedToTitleCleanupPlan:", StringComparison.Ordinal), "Runtime report should include returned-to-title cleanup plan.");
                Assert(reportContext.Contains("TitleIdleResourceGrowth:", StringComparison.Ordinal), "Runtime report should include title-idle resource growth summary.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void ShadowContentRegistryParsesGoodAndBadContentPacks()
        {
            string dir = NewTempGameDir();
            string goodRoot = Path.Combine(dir, "Mods", "GoodAnimal");
            Directory.CreateDirectory(Path.Combine(goodRoot, "Content", "DTMAPI"));
            File.WriteAllText(Path.Combine(goodRoot, "manifest.json"), "{ \"Name\": \"Good Animal\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.GoodAnimal\", \"Type\": \"ContentPack\" }");
            File.WriteAllText(Path.Combine(goodRoot, "Content", "DTMAPI", "custom-animals.json"), "[ { \"speciesId\": \"hatch\", \"templateSpeciesId\": \"chicken\", \"animatorMode\": \"pngSpriteOverride\" } ]");
            File.WriteAllText(Path.Combine(goodRoot, "Content", "DTMAPI", "audio-replacements.json"), "[ { \"id\": \"hatch-pet\", \"category\": \"AnimalVoice\", \"nativeSoundEvent\": \"PLAY_ANIMAL_PET_CHICKEN\" } ]");
            File.WriteAllText(Path.Combine(goodRoot, "Content", "animal_tbanimal.json"), "[ { \"id\": \"hatch\" } ]");

            string badRoot = Path.Combine(dir, "Mods", "BadAnimal");
            Directory.CreateDirectory(Path.Combine(badRoot, "Content", "DTMAPI"));
            File.WriteAllText(Path.Combine(badRoot, "manifest.json"), "{ \"Name\": \"Bad Animal\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.BadAnimal\", \"Type\": \"ContentPack\" }");
            File.WriteAllText(Path.Combine(badRoot, "Content", "DTMAPI", "custom-animals.json"), "{ not json");
            File.WriteAllText(Path.Combine(badRoot, "Content", "DTMAPI", "audio-replacements.json"), "[ { \"id\": \"bad\", \"category\": \"AnimalVoice\" } ]");
            File.WriteAllText(Path.Combine(badRoot, "Content", "item_tbitem.json"), "not-json");

            var good = new DiscoveredMod(
                new ManifestModel { Name = "Good Animal", Author = "DTMAPI", Version = "1.0.0", UniqueID = "DTMAPI.Tests.GoodAnimal", Type = "ContentPack" },
                goodRoot,
                Path.Combine(goodRoot, "manifest.json"),
                "Local",
                null,
                true,
                true,
                string.Empty,
                false,
                string.Empty);
            var bad = new DiscoveredMod(
                new ManifestModel { Name = "Bad Animal", Author = "DTMAPI", Version = "1.0.0", UniqueID = "DTMAPI.Tests.BadAnimal", Type = "ContentPack" },
                badRoot,
                Path.Combine(badRoot, "manifest.json"),
                "Local",
                null,
                true,
                true,
                string.Empty,
                false,
                string.Empty);

            var registry = new ShadowContentRegistry();
            ShadowContentRegistrySnapshot snapshot = registry.Build(new[] { good, bad }, new[] { good }, "unit-test");
            ShadowContentRegistryRow goodRow = snapshot.Rows.Single(r => r.UniqueID == "DTMAPI.Tests.GoodAnimal");
            ShadowContentRegistryRow badRow = snapshot.Rows.Single(r => r.UniqueID == "DTMAPI.Tests.BadAnimal");
            Assert(goodRow.LoadedByOldSystem, "Shadow row should carry old loaded state.");
            Assert(goodRow.CustomAnimals.Count == 1 && goodRow.CustomAnimals.ParseStatus == "ok", "Good custom animal JSON should parse.");
            Assert(goodRow.AudioReplacements.Count == 1 && goodRow.AudioReplacements.ParseStatus == "ok", "Good audio replacement JSON should parse.");
            Assert(goodRow.OfficialJson.ExistsCount == 1 && goodRow.OfficialJson.InvalidCount == 0, "Good official JSON should be recognized as JSON-shaped.");
            Assert(badRow.CustomAnimals.ParseStatus == "parse-error", "Bad custom animal JSON should be diagnosed.");
            Assert(badRow.OfficialJson.InvalidCount == 1, "Bad official JSON shape should be diagnosed.");
            Assert(snapshot.DiffCount >= 2, "Bad shadow content should produce diff diagnostics.");
            Assert(snapshot.FormatSummary().Contains("customAnimals=1"), "Shadow summary should include custom animal count.");
        }

        private static void ContentManifestRegistryBuildsAuthoritativeIndexAndCapabilities()
        {
            string dir = NewTempGameDir();
            string goodRoot = Path.Combine(dir, "Mods", "GoodAnimal");
            Directory.CreateDirectory(Path.Combine(goodRoot, "Content", "DTMAPI"));
            File.WriteAllText(Path.Combine(goodRoot, "manifest.json"), "{ \"Name\": \"Good Animal\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.GoodAnimal\", \"Type\": \"ContentPack\" }");
            File.WriteAllText(Path.Combine(goodRoot, "Content", "DTMAPI", "custom-animals.json"), "[ { \"speciesId\": \"hatch\", \"templateSpeciesId\": \"chicken\" } ]");
            File.WriteAllText(Path.Combine(goodRoot, "Content", "DTMAPI", "audio-replacements.json"), "[ { \"id\": \"hatch-pet\", \"category\": \"AnimalVoice\", \"nativeSoundEvent\": \"PLAY_ANIMAL_PET_CHICKEN\" } ]");
            File.WriteAllText(Path.Combine(goodRoot, "Content", "animal_tbanimal.json"), "[ { \"id\": \"hatch\" } ]");

            DiscoveredMod good = CreateDiscoveredContentPack(goodRoot, "DTMAPI.Tests.GoodAnimal", "Good Animal", source: "OfficialLocal", officialManaged: true);
            var registry = new ContentManifestRegistry();
            ContentManifestRegistrySnapshot snapshot = registry.Build(
                new[] { good },
                new[] { good },
                Array.Empty<string>(),
                Array.Empty<string>(),
                "unit-test");

            ContentManifestRegistryRow row = snapshot.Rows.Single(r => r.UniqueID == "DTMAPI.Tests.GoodAnimal");
            Assert(row.LoadedByOldSystem, "Authoritative index should preserve old loader loaded state.");
            Assert(row.OwnerId == "DTMAPI.Tests.GoodAnimal", "Registry row owner should default to the manifest UniqueID.");
            Assert(row.Capabilities.Contains("ContentPack", StringComparer.OrdinalIgnoreCase), "Content pack capability should be inferred.");
            Assert(row.Capabilities.Contains("CustomAnimals", StringComparer.OrdinalIgnoreCase), "CustomAnimals capability should be inferred from DTMAPI content JSON.");
            Assert(row.Capabilities.Contains("AudioReplacement", StringComparer.OrdinalIgnoreCase), "AudioReplacement capability should be inferred from DTMAPI content JSON.");
            Assert(row.Capabilities.Contains("AnimalVoice", StringComparer.OrdinalIgnoreCase), "AnimalVoice capability should be inferred from replacement category text.");
            Assert(row.Capabilities.Contains("OfficialJson", StringComparer.OrdinalIgnoreCase), "Official JSON capability should be inferred without applying content.");
            Assert(row.Capabilities.Contains("OfficialLocal", StringComparer.OrdinalIgnoreCase), "Official local source capability should be retained.");
            Assert(snapshot.DiffCount == 0, "Registry should not diff when all legacy loaded content packs are indexed.");
            Assert(snapshot.FormatSummary().Contains("customAnimalPacks=1"), "Content registry summary should count custom animal packs.");
            Assert(snapshot.FormatSummary().Contains("animalVoicePacks=1"), "Content registry summary should count AnimalVoice packs.");
            Assert(snapshot.FormatOwnershipSummary().Contains("officialManaged=1"), "Ownership summary should expose official enablement ownership.");
        }

        private static void ContentManifestRegistryReportsManifestDependencyAndDuplicateDiagnostics()
        {
            string dir = NewTempGameDir();
            string baseRoot = Path.Combine(dir, "Mods", "Base");
            string needsRoot = Path.Combine(dir, "Mods", "NeedsMissing");
            string optionalRoot = Path.Combine(dir, "Mods", "OptionalNeedsBase2");
            string futureRoot = Path.Combine(dir, "Mods", "FutureApi");
            string needsRuntimeRoot = Path.Combine(dir, "Mods", "NeedsRuntime");
            Directory.CreateDirectory(baseRoot);
            Directory.CreateDirectory(needsRoot);
            Directory.CreateDirectory(optionalRoot);
            Directory.CreateDirectory(futureRoot);
            Directory.CreateDirectory(needsRuntimeRoot);

            DiscoveredMod baseMod = CreateDiscoveredContentPack(baseRoot, "DTMAPI.Tests.Base", "Base");
            DiscoveredMod needsMissing = CreateDiscoveredContentPack(needsRoot, "DTMAPI.Tests.NeedsMissing", "Needs Missing");
            needsMissing.Manifest.DependencyModels.Add(new ManifestDependencyModel { UniqueID = "DTMAPI.Tests.Missing", Required = true });
            DiscoveredMod optionalNeedsBase2 = CreateDiscoveredContentPack(optionalRoot, "DTMAPI.Tests.OptionalNeedsBase2", "Optional Needs Base 2");
            optionalNeedsBase2.Manifest.DependencyModels.Add(new ManifestDependencyModel { UniqueID = "DTMAPI.Tests.Base", MinimumVersion = "2.0.0", Required = false });
            DiscoveredMod futureApi = CreateDiscoveredContentPack(futureRoot, "DTMAPI.Tests.FutureApi", "Future API");
            futureApi.Manifest.MinimumDTMApiVersion = "99.0.0";
            DiscoveredMod needsRuntime = CreateDiscoveredContentPack(needsRuntimeRoot, "DTMAPI.Tests.NeedsRuntime", "Needs Runtime");
            needsRuntime.Manifest.DependencyModels.Add(new ManifestDependencyModel { UniqueID = "DTMAPI.Tests.RuntimeApi", MinimumVersion = "1.2.3", Required = true });
            var registeredRuntime = new ManifestModel { Name = "Runtime API", Author = "DTMAPI", Version = "1.2.3", UniqueID = "DTMAPI.Tests.RuntimeApi", Type = "RuntimeApi" };

            var registry = new ContentManifestRegistry();
            ContentManifestRegistrySnapshot snapshot = registry.Build(
                new[] { baseMod, needsMissing, optionalNeedsBase2, futureApi, needsRuntime },
                new[] { baseMod },
                new[] { "Broken manifest at Mods/Broken/manifest.json: required fields missing." },
                new[] { "Duplicate UniqueID DTMAPI.Tests.Base ignored by legacy scanner." },
                "unit-test",
                new IManifest[] { registeredRuntime });

            Assert(snapshot.ManifestDiagnosticCount == 2, "Bad manifest and duplicate UniqueID should be manifest diagnostics.");
            Assert(snapshot.DependencyErrorCount == 1, "Missing required dependency should be an error diagnostic.");
            Assert(snapshot.DependencyWarningCount == 1, "Optional dependency version mismatch should be a warning diagnostic.");
            Assert(snapshot.ApiTooNewCount == 1, "MinimumDTMApiVersion above runtime should be an api-too-new diagnostic.");
            Assert(snapshot.Rows.Single(r => r.UniqueID == "DTMAPI.Tests.NeedsRuntime").Dependencies.Single().Status == "satisfied", "Runtime-registered manifests should satisfy dependency diagnostics even when they are not discovered content packs.");
            Assert(snapshot.DiffCount == 0, "Diagnostics should not imply registry diff when legacy loaded mods are indexed.");
            Assert(snapshot.FormatManifestSummary().Contains("badManifests=1"), "Manifest summary should include bad manifest count.");
            Assert(snapshot.FormatManifestSummary().Contains("duplicateUniqueIds=1"), "Manifest summary should include duplicate UniqueID count.");
            Assert(snapshot.FormatDependencySummary().Contains("dependencyErrors=1"), "Dependency summary should include dependency errors.");
            Assert(snapshot.FormatDependencySummary().Contains("apiTooNew=1"), "Dependency summary should include API compatibility errors.");
        }

        private static void ContentManifestRegistryReportsRegistryDiffs()
        {
            string dir = NewTempGameDir();
            string indexedRoot = Path.Combine(dir, "Mods", "Indexed");
            string loadedOnlyRoot = Path.Combine(dir, "Mods", "LoadedOnly");
            Directory.CreateDirectory(indexedRoot);
            Directory.CreateDirectory(loadedOnlyRoot);

            DiscoveredMod indexed = CreateDiscoveredContentPack(indexedRoot, "DTMAPI.Tests.Indexed", "Indexed");
            DiscoveredMod loadedOnly = CreateDiscoveredContentPack(loadedOnlyRoot, "DTMAPI.Tests.LoadedOnly", "Loaded Only");

            var registry = new ContentManifestRegistry();
            ContentManifestRegistrySnapshot snapshot = registry.Build(
                new[] { indexed },
                new[] { indexed, loadedOnly },
                Array.Empty<string>(),
                Array.Empty<string>(),
                "unit-test");

            Assert(snapshot.DiffCount == 1, "Loaded legacy mod missing from discovered index should produce one registry diff.");
            Assert(snapshot.FormatDiffSummary().Contains("loaded-missing-from-index:DTMAPI.Tests.LoadedOnly"), "Diff summary should name the missing legacy-loaded mod.");
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

        private static void AdvancedCodeModWithoutReceiptFailsClosedBeforeDiscoveryAndAssemblyLoad()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                string modDir = Path.Combine(dir, "Mods", "ReservedCodeModKind");
                Directory.CreateDirectory(modDir);
                string assemblyPath = typeof(ApiOwnerProbeMod).Assembly.Location;
                string assemblyName = Path.GetFileName(assemblyPath);
                File.Copy(assemblyPath, Path.Combine(modDir, assemblyName));
                File.WriteAllText(
                    Path.Combine(modDir, "manifest.json"),
                    "{ \"Name\": \"Reserved CodeModKind\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.ReservedCodeModKind\", \"Type\": \"CodeMod\", \"CodeModKind\": \"Advanced\", \"EntryDll\": \"" + assemblyName + "\", \"EntryType\": \"" + (typeof(ApiOwnerProbeMod).FullName ?? nameof(ApiOwnerProbeMod)) + "\" }" );

                var checkpoints = new List<ModLoadCheckpoint>();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                runtime.ConfigureModLoadCheckpointForTests(checkpoints.Add);
                runtime.Start();
                RuntimeSnapshot snapshot = runtime.CreateSnapshot();

                Assert(!snapshot.LoadedMods.Any(mod => mod.Manifest.UniqueID == "DTMAPI.Tests.ReservedCodeModKind"), "Advanced CodeMod without its receipt must not produce a loaded owner.");
                Assert(!runtime.ModRegistry.IsLoaded("DTMAPI.Tests.ReservedCodeModKind"), "Advanced CodeMod without its receipt must not publish a registry owner.");
                Assert(checkpoints.Count == 0, "Advanced CodeMod without its receipt must be rejected before Assembly.LoadFrom and every mod-load checkpoint.");
                Assert(
                    snapshot.Errors.Any(error =>
                        error.Owner == "DTMAPI.ModScanner" &&
                        error.Message.Contains("Manifest discovery failed", StringComparison.Ordinal) &&
                        error.Details.Contains("advanced-reference-receipt-missing", StringComparison.Ordinal)),
                    "Advanced CodeMod without its receipt should produce the stable pre-load scanner failure code.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void RuntimePathsDoNotCreateOrAdvertiseLegacyDevelopmentRoot()
        {
            string dir = Path.Combine(Path.GetTempPath(), "DTMAPI-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var paths = new RuntimePaths(dir, Path.Combine(dir, "BepInEx", "plugins", "DTMAPI"));
                string expectedLegacyPath = Path.Combine(dir, "Mods");
                Assert(
                    string.Equals(paths.LegacyDevelopmentModsPath, expectedLegacyPath, StringComparison.OrdinalIgnoreCase),
                    "RuntimePaths should retain the historical path only as an explicitly named diagnostic/test boundary.");
                paths.Ensure();
                Assert(!Directory.Exists(expectedLegacyPath), "Ordinary RuntimePaths.Ensure must not create the retired <game>/Mods root.");

                string? previousRoot = UseTempPersistentRoot();
                try
                {
                    var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                    runtime.Start();
                    string log = File.ReadAllText(runtime.Diagnostics.GetLatestLogPath());
                    Assert(!log.Contains("ModsPath =", StringComparison.Ordinal), "Ordinary Runtime startup must not advertise the retired <game>/Mods root.");
                    Assert(!Directory.Exists(expectedLegacyPath), "Ordinary Runtime startup must leave an absent retired <game>/Mods root absent.");
                }
                finally
                {
                    RestorePersistentRoot(previousRoot);
                }
            }
            finally
            {
                Directory.Delete(dir, true);
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
                Assert(DtmApiRuntime.ApiVersion == "0.7.0", "DTMAPI runtime API version should project the 0.7.0 release authority.");
                Assert(DtmApiRuntime.BinaryVersion == "0.7.0.0", "DTMAPI binary/plugin file version should remain numeric and project the 0.7.0 authority.");
                WriteManifest(dir, "Base", "{ \"Name\": \"Base\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.Base\", \"Type\": \"ContentPack\" }");
                WriteManifest(dir, "NeedsBase2", "{ \"Name\": \"Needs Base 2\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.NeedsBase2\", \"Type\": \"ContentPack\", \"Dependencies\": [ { \"UniqueID\": \"DTMAPI.Tests.Base\", \"MinimumVersion\": \"2.0.0\", \"Required\": true } ] }");
                WriteManifest(dir, "OptionalNeedsBase2", "{ \"Name\": \"Optional Needs Base 2\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.OptionalNeedsBase2\", \"Type\": \"ContentPack\", \"Dependencies\": [ { \"UniqueID\": \"DTMAPI.Tests.Base\", \"MinimumVersion\": \"2.0.0\", \"Required\": false } ] }");
                WriteManifest(dir, "NeedsCurrentApi", "{ \"Name\": \"Needs Current API\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.CurrentApi\", \"Type\": \"ContentPack\", \"MinimumDTMApiVersion\": \"0.5.5\" }");
                WriteManifest(dir, "Legacy042Api", "{ \"Name\": \"Legacy 0.4.2 API\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.Legacy042Api\", \"Type\": \"ContentPack\", \"MinimumDTMApiVersion\": \"0.4.2\" }");
                WriteManifest(dir, "Legacy031Api", "{ \"Name\": \"Legacy 0.3.1 API\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.Legacy031Api\", \"Type\": \"ContentPack\", \"MinimumDTMApiVersion\": \"0.3.1\" }");
                WriteManifest(dir, "NeedsFutureAlphaApi", "{ \"Name\": \"Needs Future Alpha API\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.FutureAlphaApi\", \"Type\": \"ContentPack\", \"MinimumDTMApiVersion\": \"0.8.0-alpha\" }");
                WriteManifest(dir, "NeedsFutureApi", "{ \"Name\": \"Needs Future API\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.FutureApi\", \"Type\": \"ContentPack\", \"MinimumDTMApiVersion\": \"99.0.0\" }");
                WriteManifest(dir, "CycleA", "{ \"Name\": \"Cycle A\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.CycleA\", \"Type\": \"ContentPack\", \"Dependencies\": [ { \"UniqueID\": \"DTMAPI.Tests.CycleB\", \"Required\": true } ] }");
                WriteManifest(dir, "CycleB", "{ \"Name\": \"Cycle B\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.CycleB\", \"Type\": \"ContentPack\", \"Dependencies\": [ { \"UniqueID\": \"DTMAPI.Tests.CycleA\", \"Required\": true } ] }");

                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                runtime.Start();
                RuntimeSnapshot snapshot = runtime.CreateSnapshot();
                Assert(snapshot.LoadedMods.Any(m => m.Manifest.UniqueID == "DTMAPI.Tests.Base"), "Base dependency should load.");
                Assert(!snapshot.LoadedMods.Any(m => m.Manifest.UniqueID == "DTMAPI.Tests.NeedsBase2"), "Required dependency version mismatch should block loading.");
                Assert(snapshot.LoadedMods.Any(m => m.Manifest.UniqueID == "DTMAPI.Tests.OptionalNeedsBase2"), "Optional dependency version mismatch should warn but not block loading.");
                Assert(snapshot.LoadedMods.Any(m => m.Manifest.UniqueID == "DTMAPI.Tests.CurrentApi"), "MinimumDTMApiVersion 0.5.5 should load on the current Runtime.");
                Assert(snapshot.LoadedMods.Any(m => m.Manifest.UniqueID == "DTMAPI.Tests.Legacy042Api"), "Legacy MinimumDTMApiVersion 0.4.2 should still load on the current Runtime.");
                Assert(snapshot.LoadedMods.Any(m => m.Manifest.UniqueID == "DTMAPI.Tests.Legacy031Api"), "Legacy MinimumDTMApiVersion 0.3.1 should still load on the current Runtime.");
                Assert(!snapshot.LoadedMods.Any(m => m.Manifest.UniqueID == "DTMAPI.Tests.FutureAlphaApi"), "Future MinimumDTMApiVersion 0.8.0-alpha should block loading.");
                Assert(!snapshot.LoadedMods.Any(m => m.Manifest.UniqueID == "DTMAPI.Tests.FutureApi"), "Future MinimumDTMApiVersion should block loading.");
                Assert(!snapshot.LoadedMods.Any(m => m.Manifest.UniqueID == "DTMAPI.Tests.CycleA"), "CycleA should be blocked and must not load.");
                Assert(!snapshot.LoadedMods.Any(m => m.Manifest.UniqueID == "DTMAPI.Tests.CycleB"), "CycleB should be blocked and must not load.");
                IContentQueryHelper content = GetContent(runtime);
                Assert(!content.FindAssets("json").Any(asset => asset.SourceModId.Equals("DTMAPI.Tests.NeedsBase2", StringComparison.OrdinalIgnoreCase)), "A required-dependency failure must not publish the blocked owner's content.");
                Assert(!content.FindAssets("json").Any(asset => asset.SourceModId.Equals("DTMAPI.Tests.CycleA", StringComparison.OrdinalIgnoreCase) || asset.SourceModId.Equals("DTMAPI.Tests.CycleB", StringComparison.OrdinalIgnoreCase)), "Dependency-cycle failures must not publish either inactive owner's content.");
                Assert(content.FindAssets("json").Any(asset => asset.SourceModId.Equals("DTMAPI.Tests.OptionalNeedsBase2", StringComparison.OrdinalIgnoreCase)), "An optional-dependency warning must preserve the active owner's content publication.");
                Assert(snapshot.Errors.Any(e => e.Owner == "DTMAPI.Tests.NeedsBase2" && e.Message.Contains("依赖版本")), "Dependency version mismatch should be diagnosed.");
                Assert(!snapshot.Errors.Any(e => e.Owner == "DTMAPI.Tests.OptionalNeedsBase2"), "Optional dependency version mismatch should not be recorded as an error.");
                Assert(snapshot.Warnings.Any(w => w.Owner == "DTMAPI.Tests.OptionalNeedsBase2" && w.Message.Contains("可选依赖版本过低")), "Optional dependency version mismatch should be recorded as a structured warning.");
                Assert(snapshot.Errors.Any(e => e.Owner == "DTMAPI.Tests.FutureAlphaApi" && e.Message.Contains("前置版本过旧") && e.Details.Contains("Run 1_install_dtmapi.bat")), "Future alpha MinimumDTMApiVersion mismatch should tell players to update DTMAPI.");
                Assert(snapshot.Errors.Any(e => e.Owner == "DTMAPI.Tests.FutureApi" && e.Message.Contains("前置版本过旧") && e.Details.Contains("Run 1_install_dtmapi.bat")), "MinimumDTMApiVersion mismatch should tell players to update DTMAPI.");
                Assert(snapshot.Errors.Any(e => e.Message.Contains("依赖循环") && e.Details.Contains("DTMAPI.Tests.CycleA") && e.Details.Contains("DTMAPI.Tests.CycleB")), "Circular dependencies should be diagnosed with the cycle path.");
                Assert(snapshot.Errors.Any(e => e.Owner == "DTMAPI.Tests.CycleA" && e.Message.Contains("依赖循环阻止加载")), "CycleA should have an owner-specific blocked diagnostic.");
                Assert(snapshot.Errors.Any(e => e.Owner == "DTMAPI.Tests.CycleB" && e.Message.Contains("依赖循环阻止加载")), "CycleB should have an owner-specific blocked diagnostic.");
                IDtmDiagnosticsSnapshot diagnosticsSnapshot = runtime.CreateDiagnosticsSnapshot();
                Assert(diagnosticsSnapshot.Mods.Single(m => m.UniqueID == "DTMAPI.Tests.Base").StatusCode == "loaded", "Loaded dependency should expose loaded status code.");
                Assert(diagnosticsSnapshot.Mods.Single(m => m.UniqueID == "DTMAPI.Tests.NeedsBase2").StatusCode == "missing-dependency", "Dependency version failures should expose a dependency status code.");
                Assert(diagnosticsSnapshot.Mods.Single(m => m.UniqueID == "DTMAPI.Tests.OptionalNeedsBase2").Loaded && diagnosticsSnapshot.Mods.Single(m => m.UniqueID == "DTMAPI.Tests.OptionalNeedsBase2").StatusCode == "loaded", "Optional dependency warnings should leave loaded mods in loaded status.");
                Assert(diagnosticsSnapshot.Warnings.Any(w => w.Owner == "DTMAPI.Tests.OptionalNeedsBase2"), "Diagnostics snapshot should include optional dependency warnings.");
                Assert(diagnosticsSnapshot.Mods.Single(m => m.UniqueID == "DTMAPI.Tests.CurrentApi").StatusCode == "loaded", "Current MinimumDTMApiVersion should expose loaded status code.");
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
                    "{ \"Name\": \"Ambiguous Entry\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.AmbiguousEntry\", \"EntryDll\": \"" + assemblyName + "\", \"Type\": \"CodeMod\", \"CodeModKind\": \"Strict\" }");

                string selectedDir = Path.Combine(dir, "Mods", "SelectedEntry");
                Directory.CreateDirectory(selectedDir);
                File.Copy(assemblyPath, Path.Combine(selectedDir, assemblyName), overwrite: true);
                File.WriteAllText(
                    Path.Combine(selectedDir, "manifest.json"),
                    "{ \"Name\": \"Selected Entry\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.SelectedEntry\", \"EntryDll\": \"" + assemblyName + "\", \"EntryType\": \"" + selectedEntryType + "\", \"Type\": \"CodeMod\", \"CodeModKind\": \"Strict\" }");

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
                string warningCount = "Warnings: " + snapshot.Warnings.Count.ToString(CultureInfo.InvariantCulture);
                Assert(
                    summary.Contains(warningCount) &&
                    summary.Contains("WARNING") &&
                    summary.Contains("[DTMAPI.Tests.NeedsGameVersion]") &&
                    summary.Contains("MinimumGameVersion"),
                    "Diagnostic report summary should include the full structured warning set and the MinimumGameVersion warning.");
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
                    "{ \"Name\": \"API Owner\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.ApiOwner\", \"EntryDll\": \"" + assemblyName + "\", \"EntryType\": \"" + (typeof(ApiOwnerProbeMod).FullName ?? nameof(ApiOwnerProbeMod)) + "\", \"Type\": \"CodeMod\", \"CodeModKind\": \"Strict\" }");

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

        private static void HelperSharedAndOwnerBoundServicesEnforceThreadAndStaleSemantics()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                var host = new FakeHost(NewTempGameDir());
                var runtime = new DtmApiRuntime(host, new ConfigMenuRegistry());
                runtime.Start();
                var owner = new ManifestModel
                {
                    Name = "Helper Contract Owner",
                    Author = "DTMAPI",
                    Version = "1.0.0",
                    UniqueID = "DTMAPI.Tests.HelperContract"
                };
                runtime.BeginOwnerEntry(owner.UniqueID);
                runtime.ActivateOwnerEntry(owner);

                Action ensureRuntimeThread = () => runtime.EnsureModHelperRuntimeThread(owner.UniqueID);
                Action ensureOwnerActive = () => runtime.EnsureModOwnerRegistrationAllowed(owner.UniqueID);
                IUiHelper ui = runtime.UI.CreateOwnerBound(owner.UniqueID, ensureRuntimeThread, ensureOwnerActive);
                IDiagnosticsHelper diagnostics = runtime.Diagnostics.CreateOwnerBound(owner.UniqueID, ensureRuntimeThread, ensureOwnerActive);
                IWorkshopHelper workshop = runtime.Workshop;
                IContentQueryHelper content = runtime.Content;
                var translation = new TranslationService(NewTempGameDir(), "english");
                string monitorPath = Path.Combine(NewTempGameDir(), "helper-monitor.log");
                IMonitor monitor = new FileMonitor(host, owner.UniqueID, monitorPath);

                ui.OpenDtmApiStatusPage();
                Assert(runtime.UI.IsOpen, "An active owner should be able to mutate the shared DTMAPI UI through its owner-bound helper facade.");
                runtime.UI.Close();

                Exception? sharedReadFailure = null;
                var sharedReadThread = new Thread(() =>
                {
                    try
                    {
                        _ = workshop.GetOfficialMods();
                        _ = content.GetKnownContentTypes();
                        _ = diagnostics.GetErrors();
                        _ = diagnostics.GetWarnings();
                        _ = diagnostics.GetHookStatuses();
                        _ = diagnostics.GetLatestLogPath();
                        _ = translation.Get("missing", "fallback");
                    }
                    catch (Exception ex)
                    {
                        sharedReadFailure = ex;
                    }
                });
                sharedReadThread.Start();
                sharedReadThread.Join();
                Assert(sharedReadFailure == null, "Workshop, Content, Translation and read-only Diagnostics snapshots should be shared thread-safe reads.");

                Exception? offThreadUiFailure = null;
                Exception? offThreadDiagnosticsFailure = null;
                var mutationThread = new Thread(() =>
                {
                    try
                    {
                        ui.OpenErrorPage();
                    }
                    catch (Exception ex)
                    {
                        offThreadUiFailure = ex;
                    }

                    try
                    {
                        diagnostics.RecordEvidence("OFF-THREAD", "must be rejected before file mutation");
                    }
                    catch (Exception ex)
                    {
                        offThreadDiagnosticsFailure = ex;
                    }
                });
                mutationThread.Start();
                mutationThread.Join();
                Assert(offThreadUiFailure is InvalidOperationException, "Mutable UI helper operations must reject off-runtime-thread calls.");
                Assert(offThreadDiagnosticsFailure is InvalidOperationException, "Diagnostic evidence/export side effects must reject off-runtime-thread calls.");

                runtime.DeactivateOwner(owner.UniqueID, ModOwnerCleanupReason.Unload, shutdown: false, transactionId: string.Empty);
                AssertThrows(() => ui.OpenDtmApiStatusPage(), "A stale owner-bound UI helper must reject mutations after deactivation.");
                AssertThrows(() => diagnostics.ExportLogs(), "A stale Diagnostics helper must reject report-export side effects after deactivation.");
                AssertThrows(() => diagnostics.RecordEvidence("STALE", "must not write"), "A stale Diagnostics helper must reject evidence writes after deactivation.");
                Assert(diagnostics.GetErrors() != null && diagnostics.GetLatestLogPath() != null,
                    "Read-only process diagnostic snapshots remain deliberately valid through a stale helper.");
                Assert(workshop.GetOfficialMods() != null && content.GetKnownContentTypes() != null &&
                    translation.Get("missing", "fallback") == "fallback",
                    "Workshop, Content, and Translation reads must remain valid after owner deactivation because they retain no owner mutation authority.");
                monitor.Log("stale-owner final cleanup report");
                Assert(File.ReadAllText(monitorPath).Contains("stale-owner final cleanup report", StringComparison.Ordinal),
                    "The per-owner Monitor must remain valid for final cleanup reporting after owner deactivation.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void TranslationLocaleFallbackFollowsNativeLanguageAndDeveloperOverrides()
        {
            string root = NewTempGameDir();
            string i18n = Path.Combine(root, "i18n");
            Directory.CreateDirectory(i18n);
            File.WriteAllText(
                Path.Combine(i18n, "ENGLISH.JSON"),
                "{\"origin\":\"english\",\"onlyEnglish\":\"english-fallback\"}",
                new UTF8Encoding(false));
            File.WriteAllText(
                Path.Combine(i18n, "GeRmAn.JsOn"),
                "{\"origin\":\"german-base\"}",
                new UTF8Encoding(false));
            File.WriteAllText(
                Path.Combine(i18n, "pt_BR.json"),
                "{\"origin\":\"portuguese-region\"}",
                new UTF8Encoding(false));
            File.WriteAllText(
                Path.Combine(i18n, "brazilian.json"),
                "{\"origin\":\"portuguese-alias\"}",
                new UTF8Encoding(false));
            File.WriteAllText(
                Path.Combine(i18n, "japanese.json"),
                "{\"origin\":\"japanese-override\"}",
                new UTF8Encoding(false));
            File.WriteAllText(
                Path.Combine(i18n, "russian.json"),
                "{\"origin\":\"russian-ui-override\"}",
                new UTF8Encoding(false));
            File.WriteAllText(
                Path.Combine(i18n, "french.json"),
                "{broken-json",
                new UTF8Encoding(false));

            string? previousLanguage = Environment.GetEnvironmentVariable("DTMAPI_LANGUAGE");
            string? previousUiLanguage = Environment.GetEnvironmentVariable("DTMAPI_UI_LANGUAGE");
            try
            {
                Environment.SetEnvironmentVariable("DTMAPI_LANGUAGE", null);
                Environment.SetEnvironmentVariable("DTMAPI_UI_LANGUAGE", null);
                string nativeLanguage = "de-DE";
                var source = new RuntimeLanguageSource(() => nativeLanguage);
                var warnings = new List<string>();
                var translation = new TranslationService(root, source, warnings.Add);

                Assert(
                    translation.Get("origin") == "german-base" &&
                    translation.Get("onlyEnglish") == "english-fallback" &&
                    translation.Language.Equals("GeRmAn", StringComparison.OrdinalIgnoreCase),
                    "A full regional game language must fall back to its base-language alias and then English per key, with case-insensitive filenames.");

                nativeLanguage = "pt-BR";
                source.SetNativeLanguageProvider(() => nativeLanguage);
                Assert(
                    translation.Get("origin") == "portuguese-region" &&
                    translation.Language.Equals("pt_BR", StringComparison.OrdinalIgnoreCase),
                    "An exact regional catalog must win before the Steam/base-language alias.");

                nativeLanguage = "fr-CA";
                source.SetNativeLanguageProvider(() => nativeLanguage);
                Assert(
                    translation.Get("origin") == "english" &&
                    translation.Get("origin") == "english" &&
                    warnings.Count == 1,
                    "A corrupt regional/base catalog must be skipped, fall back to English, and log only once.");

                nativeLanguage = "es-MX";
                source.SetNativeLanguageProvider(() => nativeLanguage);
                Assert(
                    translation.Get("origin") == "english" && warnings.Count == 1,
                    "A missing regional and base catalog must silently fall back to English.");

                Environment.SetEnvironmentVariable("DTMAPI_LANGUAGE", "japanese");
                Assert(
                    translation.Get("origin") == "japanese-override",
                    "DTMAPI_LANGUAGE must remain an explicit developer override above the game language.");
                Environment.SetEnvironmentVariable("DTMAPI_LANGUAGE", null);
                Environment.SetEnvironmentVariable("DTMAPI_UI_LANGUAGE", "russian");
                Assert(
                    translation.Get("origin") == "russian-ui-override",
                    "DTMAPI_UI_LANGUAGE must remain the secondary explicit developer override.");

                Assert(
                    TranslationService.NormalizeLanguage("english") == "english" &&
                    TranslationService.NormalizeLanguage("de-DE") == "german" &&
                    TranslationService.NormalizeLanguage("fr-FR") == "french" &&
                    TranslationService.NormalizeLanguage("ja-JP") == "japanese" &&
                    TranslationService.NormalizeLanguage("ko-KR") == "koreana" &&
                    TranslationService.NormalizeLanguage("pt-BR") == "brazilian" &&
                    TranslationService.NormalizeLanguage("ru-RU") == "russian" &&
                    TranslationService.NormalizeLanguage("zh-CN") == "schinese" &&
                    TranslationService.NormalizeLanguage("zh-TW") == "tchinese",
                    "All nine product languages and Steam aliases must normalize to the stable catalog names.");
            }
            finally
            {
                Environment.SetEnvironmentVariable("DTMAPI_LANGUAGE", previousLanguage);
                Environment.SetEnvironmentVariable("DTMAPI_UI_LANGUAGE", previousUiLanguage);
            }
        }

        private static void ModLoadCheckpointsRollbackAllOwnerRoots()
        {
            foreach (ModLoadCheckpoint checkpoint in Enum.GetValues(typeof(ModLoadCheckpoint)))
            {
                string? previousRoot = UseTempPersistentRoot();
                try
                {
                    string dir = NewTempGameDir();
                    string ownerId = "DTMAPI.Tests.Atomic." + checkpoint;
                    var menu = new ConfigMenuRegistry();
                    var runtime = CreateAtomicProbeRuntime(dir, ownerId, menu);
                    var participant = new AtomicCheckpointCleanupParticipant();
                    runtime.RegisterRuntimeApi<IAtomicCheckpointParticipantApi>(new ManifestModel
                    {
                        Name = "Atomic Checkpoint Participant",
                        Author = "DTMAPI",
                        Version = DtmApiRuntime.ApiVersion,
                        UniqueID = AtomicCheckpointCleanupParticipant.ProviderId,
                        Type = "RuntimeApi"
                    }, participant);
                    runtime.RegisterModOwnerCleanupParticipant(participant);
                    runtime.ConfigureModLoadCheckpointForTests(current =>
                    {
                        if (current == checkpoint)
                            throw new InvalidOperationException("checkpoint-" + checkpoint);
                    });
                    AtomicOwnerProbeMod.DisposeCount = 0;
                    AtomicOwnerProbeMod.DisposeFailuresRemaining = 0;
                    runtime.Start();

                    Assert(!runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == ownerId), checkpoint + " failure must not publish loaded state.");
                    Assert(runtime.ModRegistry.GetApi<IUnitProbeApi>(ownerId) == null, checkpoint + " failure must remove API roots.");
                    Assert(((IConfigMenuRuntime)menu).GetPage(ownerId) == null, checkpoint + " failure must remove config callbacks.");
                    Assert(runtime.Config.CountOwner(ownerId) == 0, checkpoint + " failure must remove config migration roots.");
                    Assert(!runtime.Input.GetRegisteredButtons().Contains("F12", StringComparer.OrdinalIgnoreCase), checkpoint + " failure must dispose input roots.");
                    Assert(runtime.Events.GetHandlerCleanupSnapshot().Slots.All(slot => !slot.ByOwner.ContainsKey(ownerId)), checkpoint + " failure must remove event roots.");
                    Assert(!GetContent(runtime).FindAssets("json").Any(asset => asset.SourceModId.Equals(ownerId, StringComparison.OrdinalIgnoreCase)), checkpoint + " failure must not publish owner content assets.");
                    Assert(runtime.ModOwnerLedgerSnapshot.ActiveTransactions == 0, checkpoint + " failure must close the owner transaction.");
                    Assert(runtime.OwnerRequiresRestart(ownerId), checkpoint + " failure after assembly-load attempt should require restart.");
                    Assert(participant.CountOwnerResources(ownerId) == 0, checkpoint + " failure must leave zero participant roots.");
                    bool entryRan = (int)checkpoint >= (int)ModLoadCheckpoint.EntryReturned;
                    bool instanceCreated = (int)checkpoint >= (int)ModLoadCheckpoint.InstanceCreated;
                    Assert(AtomicOwnerProbeMod.DisposeCount == (instanceCreated ? 1 : 0), checkpoint + " rollback must dispose every constructed instance, including failures before Entry publication.");
                    Assert(participant.RegisterCalls == (entryRan ? 1 : 0), checkpoint + " should create a participant root exactly when Entry completed registration.");
                    Assert(participant.CleanupCalls == 1 && participant.RemovedResources == (entryRan ? 1 : 0), checkpoint + " rollback must invoke participant cleanup and remove every post-Entry root.");
                }
                finally
                {
                    RestorePersistentRoot(previousRoot);
                }
            }
        }

        private static void CompletionLogFailureDoesNotReverseSuccessfulEntry()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                const string ownerId = "DTMAPI.Tests.CompletionLog";
                var runtime = CreateAtomicProbeRuntime(dir, ownerId, new ConfigMenuRegistry());
                runtime.ConfigureModCompletionLogForTests(_ => throw new IOException("completion-log-failure"));
                AtomicOwnerProbeMod.EntryCount = 0;
                runtime.Start();
                Assert(runtime.LoadedMods.Count(mod => mod.Manifest.UniqueID == ownerId) == 1, "Completion logging failure must not reverse successful Entry publication.");
                Assert(AtomicOwnerProbeMod.EntryCount == 1, "Completion logging failure must not repeat Entry.");
                Assert(runtime.ModRegistry.GetApi<IUnitProbeApi>(ownerId) != null, "Completion logging failure must preserve the committed owner API.");
                Assert(GetContent(runtime).FindAssets("json").Any(asset => asset.SourceModId.Equals(ownerId, StringComparison.OrdinalIgnoreCase)), "A successfully committed owner must publish its content assets.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void TransactionDiagnosticFailuresCannotLeakOrReverseSuccessfulLoad()
        {
            foreach (string phase in new[] { "begin", "commit" })
            {
                string? previousRoot = UseTempPersistentRoot();
                try
                {
                    string ownerId = "DTMAPI.Tests.TransactionDiagnostic." + phase;
                    var runtime = CreateAtomicProbeRuntime(NewTempGameDir(), ownerId, new ConfigMenuRegistry());
                    int hookCalls = 0;
                    Action throwingDiagnostic = () =>
                    {
                        hookCalls++;
                        throw new IOException("transaction-" + phase + "-diagnostic-failure");
                    };
                    runtime.ConfigureModTransactionDiagnosticsForTests(
                        phase.Equals("begin", StringComparison.Ordinal) ? throwingDiagnostic : null,
                        phase.Equals("commit", StringComparison.Ordinal) ? throwingDiagnostic : null);
                    AtomicOwnerProbeMod.EntryCount = 0;

                    runtime.Start();

                    ModOwnerLedgerSnapshot ledger = runtime.ModOwnerLedgerSnapshot;
                    Assert(hookCalls == 1, "The injected transaction " + phase + " diagnostic should run exactly once.");
                    Assert(runtime.LoadedMods.Count(mod => mod.Manifest.UniqueID == ownerId) == 1 && AtomicOwnerProbeMod.EntryCount == 1, "A transaction " + phase + " diagnostic failure must not reverse or repeat a successful Entry.");
                    Assert(runtime.ModRegistry.GetApi<IUnitProbeApi>(ownerId) != null, "A transaction " + phase + " diagnostic failure must preserve committed owner API roots.");
                    Assert(ledger.ActiveTransactions == 0 && ledger.CommittedTransactions == 1 && ledger.RolledBackTransactions == 0, "A transaction " + phase + " diagnostic failure must still close and commit the owner transaction exactly once.");
                    Assert(!runtime.OwnerRequiresRestart(ownerId), "A best-effort transaction " + phase + " diagnostic failure must not deactivate a successfully loaded owner.");
                }
                finally
                {
                    RestorePersistentRoot(previousRoot);
                }
            }
        }

        private static DtmApiRuntime CreateAtomicProbeRuntime(string gameDir, string ownerId, ConfigMenuRegistry menu)
        {
            WriteAtomicProbePackage(gameDir, ownerId, string.Empty);
            return new DtmApiRuntime(new FakeHost(gameDir), menu);
        }

        private static ManifestModel RegisterReservedRuntimeProviderProbe(DtmApiRuntime runtime, string uniqueId, string version)
        {
            var manifest = new ManifestModel
            {
                Name = "Reserved Runtime Provider Probe",
                Author = "DTMAPI",
                Version = version,
                UniqueID = uniqueId,
                Type = "RuntimeApi"
            };
            runtime.RegisterRuntimeApi<IUnitProbeApi>(manifest, new UnitProbeApi(uniqueId));
            return manifest;
        }

        private static void StructuredOwnerKeysDoNotCollideOrOverClean()
        {
            var registry = new ModRegistryService();
            var providerOne = new ManifestModel { Name = "Provider One", Author = "DTMAPI", Version = "1.0.0", UniqueID = "C" };
            var providerTwo = new ManifestModel { Name = "Provider Two", Author = "DTMAPI", Version = "1.0.0", UniqueID = "B|C" };
            var consumerOne = new ManifestModel { Name = "Consumer One", Author = "DTMAPI", Version = "1.0.0", UniqueID = "A|B" };
            var consumerTwo = new ManifestModel { Name = "Consumer Two", Author = "DTMAPI", Version = "1.0.0", UniqueID = "A" };
            registry.RegisterApiForOwner<IDtmConfigMenuApi>(providerOne, new ConfigMenuRegistry());
            registry.RegisterApiForOwner<IDtmConfigMenuApi>(providerTwo, new ConfigMenuRegistry());

            IDtmConfigMenuApi first = registry.CreateOwnerBoundRegistry(consumerOne, () => { }).GetApi<IDtmConfigMenuApi>(providerOne.UniqueID)
                ?? throw new InvalidOperationException("First structured-key facade missing.");
            IDtmConfigMenuApi second = registry.CreateOwnerBoundRegistry(consumerTwo, () => { }).GetApi<IDtmConfigMenuApi>(providerTwo.UniqueID)
                ?? throw new InvalidOperationException("Second structured-key facade missing.");
            Assert(!ReferenceEquals(first, second), "Facade keys must distinguish consumer=A|B/provider=C from consumer=A/provider=B|C.");
            registry.RemoveOwner(providerOne.UniqueID);
            second.Register(consumerTwo, () => { }, () => { });

            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string gameDir = NewTempGameDir();
                var paths = new RuntimePaths(gameDir, gameDir);
                var config = new ConfigService(paths, new DiagnosticsService(paths));
                config.RegisterMigration<HotLoadProbeConfig>(consumerOne, _ => { });
                config.RegisterMigration<HotLoadProbeConfig>(consumerTwo, _ => { });
                Assert(config.RemoveOwner(consumerTwo.UniqueID) == 1, "Removing owner A should remove exactly its migration.");
                Assert(config.CountOwner(consumerOne.UniqueID) == 1, "Removing owner A must not prefix-match and remove owner A|B.");
                Assert(config.RemoveOwner(consumerOne.UniqueID) == 1 && config.TotalMigrationCount == 0, "Structured migration keys should still clean the exact pipe-containing owner.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void FailedCodeModCleanupRemovesOwnerBoundRuntimeState()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                var menu = new ConfigMenuRegistry();
                IConfigMenuRuntime menuRuntime = menu;
                var runtime = new DtmApiRuntime(new FakeHost(dir), menu);
                IManifest owner = new ManifestModel
                {
                    Name = "Throw After Register",
                    Author = "DTMAPI",
                    Version = "1.0.0",
                    UniqueID = "DTMAPI.Tests.ThrowAfterRegister",
                    Type = "CodeMod"
                };

                int updateCalls = 0;
                IEventsHelper events = CreateEventsProxy(runtime, owner.UniqueID);
                events.GameLoop.UpdateTicked += (_, _) => updateCalls++;
                runtime.ModRegistry.AddLoaded(owner);
                runtime.ModRegistry.RegisterApiForOwner<IUnitProbeApi>(owner, new UnitProbeApi(owner.UniqueID));
                ((InputService)GetInput(runtime)).CreateOwnerBound(owner.UniqueID).RegisterButton("F11");
                menu.Register(owner, () => { }, () => { });
                menu.AddParagraph(owner, () => "this getter must not survive failed load cleanup");
                runtime.CustomEntities.RegisterSpecies(owner, new CustomAnimalSpeciesDefinition { SpeciesId = owner.UniqueID + ".Animal" });

                MethodInfo cleanup = typeof(DtmApiRuntime).GetMethod("CleanupFailedCodeModOwner", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("CleanupFailedCodeModOwner should exist.");
                string summary = (string)(cleanup.Invoke(runtime, new object[] { owner.UniqueID, string.Empty }) ?? string.Empty);

                runtime.Update();
                Assert(updateCalls == 0, "Failed code-mod cleanup should remove owner-bound event handlers.");
                Assert(runtime.ModRegistry.GetApi<IUnitProbeApi>(owner.UniqueID) == null, "Failed code-mod cleanup should remove owner-bound API registrations.");
                Assert(!GetInput(runtime).GetRegisteredButtons().Contains("F11", StringComparer.OrdinalIgnoreCase), "Failed code-mod cleanup should remove owner-bound input registrations.");
                Assert(menuRuntime.GetPage(owner.UniqueID) == null, "Failed code-mod cleanup should remove the failed mod's config page.");
                Assert(runtime.CustomEntities.GetAnimalSnapshot(owner.UniqueID).RegisteredDefinitionCount == 0, "Failed code-mod cleanup should remove owner-bound custom entity definitions.");
                Assert(summary.Contains("eventHandlersRemoved=1", StringComparison.Ordinal) &&
                    summary.Contains("registryEntriesRemoved=", StringComparison.Ordinal) &&
                    summary.Contains("inputButtonsRemoved=1", StringComparison.Ordinal) &&
                    summary.Contains("configPagesRemoved=1", StringComparison.Ordinal) &&
                    summary.Contains("customEntitiesRemoved=1", StringComparison.Ordinal), "Failed code-mod cleanup summary should describe every owner-bound cleanup bucket.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void ModInstanceDisposeRunsBeforeOwnerRootReleaseAndRetries()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                const string ownerId = "DTMAPI.Tests.ModInstanceDispose";
                var runtime = CreateAtomicProbeRuntime(dir, ownerId, new ConfigMenuRegistry());
                var laterParticipant = new OwnerCleanupOrderProbe("DTMAPI.Tests.ModInstanceDispose.LaterParticipant");
                runtime.RegisterModOwnerCleanupParticipant(laterParticipant);
                AtomicOwnerProbeMod.DisposeCount = 0;
                AtomicOwnerProbeMod.DisposeFailuresRemaining = 1;
                AtomicOwnerProbeMod.PrepareFailuresRemaining = 0;
                AtomicOwnerProbeMod.LastDeactivationReason = string.Empty;
                AtomicOwnerProbeMod.LastPrepareReason = string.Empty;
                runtime.Start();

                Assert(runtime.HasOwnerInstance(ownerId), "The disposable probe must be published before owner deactivation.");
                string first = InvokeOwnerDeactivation(runtime, ownerId, ModOwnerCleanupReason.Unload, shutdown: false);
                Assert(AtomicOwnerProbeMod.DisposeCount == 1 && AtomicOwnerProbeMod.DisposeFailuresRemaining == 0,
                    "The first owner deactivation pass must invoke the optional Mod instance Dispose callback exactly once.");
                Assert(AtomicOwnerProbeMod.LastDeactivationReason == nameof(ModOwnerCleanupReason.Unload),
                    "Reason-aware managed Mod deactivation must receive the exact Unload reason.");
                Assert(AtomicOwnerProbeMod.LastPrepareReason == nameof(ModOwnerCleanupReason.Unload),
                    "Reason-aware managed Mod preparation must receive the exact Unload reason.");
                Assert(first.Contains("modDisposeFailures=1", StringComparison.Ordinal) &&
                    first.Contains("lifecycleInstancesRemoved=0", StringComparison.Ordinal) &&
                    first.Contains("remaining=1", StringComparison.Ordinal),
                    "A Dispose failure must remain visible as one retained lifecycle root instead of being hidden by generic owner cleanup. summary=" + first);
                Assert(!runtime.HasOwnerInstance(ownerId) && runtime.OwnerRequiresRestart(ownerId),
                    "Generic owner roots must still be removed after a Dispose failure while the loaded owner stays restart-required.");
                Assert(laterParticipant.OwnerIds.SequenceEqual(new[] { ownerId }),
                    "A product Dispose failure must not prevent later GameBridge/Advanced Harmony cleanup participants from running.");

                string second = InvokeOwnerDeactivation(runtime, ownerId, ModOwnerCleanupReason.Unload, shutdown: false);
                Assert(AtomicOwnerProbeMod.DisposeCount == 2,
                    "A later deactivation pass must retry the retained disposable instance without executing Entry again.");
                Assert(laterParticipant.OwnerIds.SequenceEqual(new[] { ownerId, ownerId }),
                    "Cleanup retries must continue to invoke later participants after the retained product instance succeeds.");
                Assert(second.Contains("modDisposeFailures=0", StringComparison.Ordinal) &&
                    second.Contains("lifecycleInstancesRemoved=1", StringComparison.Ordinal) &&
                    second.Contains("remaining=0", StringComparison.Ordinal),
                    "The successful retry must release the retained lifecycle root and prove zero owner roots. summary=" + second);
                Assert(runtime.OwnerRequiresRestart(ownerId) && CountCoreOwnerRootsForTest(runtime, ownerId) == 0,
                    "A successful product cleanup retry cannot make an already loaded Mono assembly live-reloadable.");

                string prepareDir = NewTempGameDir();
                const string prepareOwner =
                    "DTMAPI.Tests.ModInstancePrepareDeactivation";
                var prepareRuntime =
                    CreateAtomicProbeRuntime(
                        prepareDir,
                        prepareOwner,
                        new ConfigMenuRegistry());
                var prepareParticipant =
                    new OwnerCleanupOrderProbe(
                        "DTMAPI.Tests.ModInstancePrepareDeactivation.LaterParticipant");
                prepareRuntime.RegisterModOwnerCleanupParticipant(
                    prepareParticipant);
                AtomicOwnerProbeMod.DisposeCount = 0;
                AtomicOwnerProbeMod.DisposeFailuresRemaining = 0;
                AtomicOwnerProbeMod.PrepareFailuresRemaining = 1;
                AtomicOwnerProbeMod.LastPrepareReason = string.Empty;
                prepareRuntime.Start();
                int rootsBeforePrepareFailure =
                    CountCoreOwnerRootsForTest(
                        prepareRuntime,
                        prepareOwner);
                string deferred =
                    InvokeOwnerDeactivation(
                        prepareRuntime,
                        prepareOwner,
                        ModOwnerCleanupReason.Unload,
                        shutdown: false);
                Assert(
                    deferred.Contains(
                        "deactivationDeferred=true",
                        StringComparison.Ordinal) &&
                    AtomicOwnerProbeMod.DisposeCount == 0 &&
                    prepareRuntime.HasOwnerInstance(
                        prepareOwner) &&
                    CountCoreOwnerRootsForTest(
                        prepareRuntime,
                        prepareOwner) ==
                        rootsBeforePrepareFailure &&
                    prepareParticipant.OwnerIds.Count == 0 &&
                    !prepareRuntime.OwnerRequiresRestart(
                        prepareOwner),
                    "A failed reason-aware deactivation preparation must retain the active instance and every owner root for an atomic retry. summary=" +
                    deferred);
                string preparedRetry =
                    InvokeOwnerDeactivation(
                        prepareRuntime,
                        prepareOwner,
                        ModOwnerCleanupReason.Unload,
                        shutdown: false);
                Assert(
                    AtomicOwnerProbeMod.DisposeCount == 1 &&
                    prepareParticipant.OwnerIds.SequenceEqual(
                        new[] { prepareOwner }) &&
                    CountCoreOwnerRootsForTest(
                        prepareRuntime,
                        prepareOwner) == 0 &&
                    preparedRetry.Contains(
                        "deactivationDeferred",
                        StringComparison.Ordinal) == false,
                    "A later atomic deactivation retry must run preparation, cleanup, and all participants exactly once.");

                string shutdownDir = NewTempGameDir();
                const string shutdownOwner = "DTMAPI.Tests.ModInstanceDisposeShutdown";
                var shutdownRuntime = CreateAtomicProbeRuntime(shutdownDir, shutdownOwner, new ConfigMenuRegistry());
                AtomicOwnerProbeMod.DisposeCount = 0;
                AtomicOwnerProbeMod.DisposeFailuresRemaining = 0;
                AtomicOwnerProbeMod.PrepareFailuresRemaining = 0;
                AtomicOwnerProbeMod.LastDeactivationReason = string.Empty;
                AtomicOwnerProbeMod.LastPrepareReason = string.Empty;
                shutdownRuntime.Start();
                shutdownRuntime.NotifyRuntimeShutdown("unit-disposable-owner-shutdown");
                Assert(AtomicOwnerProbeMod.DisposeCount == 1 && CountCoreOwnerRootsForTest(shutdownRuntime, shutdownOwner) == 0,
                    "Runtime shutdown must reuse the same optional Mod instance cleanup callback exactly once and release every owner root.");
                Assert(AtomicOwnerProbeMod.LastDeactivationReason == nameof(ModOwnerCleanupReason.RuntimeShutdown),
                    "Reason-aware managed Mod deactivation must receive the exact RuntimeShutdown reason.");
                Assert(AtomicOwnerProbeMod.LastPrepareReason == nameof(ModOwnerCleanupReason.RuntimeShutdown),
                    "Reason-aware managed Mod preparation must receive the exact RuntimeShutdown reason.");
                Assert(!shutdownRuntime.OwnerRequiresRestart(shutdownOwner),
                    "A completed process shutdown cleanup is not an in-process restart-required transition.");
            }
            finally
            {
                AtomicOwnerProbeMod.DisposeFailuresRemaining = 0;
                AtomicOwnerProbeMod.PrepareFailuresRemaining = 0;
                AtomicOwnerProbeMod.LastDeactivationReason = string.Empty;
                AtomicOwnerProbeMod.LastPrepareReason = string.Empty;
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void CleanupParticipantExceptionsAreIsolated()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                var runtime = new DtmApiRuntime(new FakeHost(NewTempGameDir()), new ConfigMenuRegistry());
                const string ownerId = "DTMAPI.Tests.CleanupIsolation";
                var throwing = new ThrowingOwnerCleanupParticipant("DTMAPI.Tests.CleanupIsolation.Throwing");
                var later = new RetryingOwnerCleanupParticipant("DTMAPI.Tests.CleanupIsolation.Later", failFirstCleanup: false);
                runtime.RegisterModOwnerCleanupParticipant(throwing);
                runtime.RegisterModOwnerCleanupParticipant(later);

                ModOwnerParticipantCleanupSummary summary = runtime.CleanupModOwnerParticipants(ownerId, ModOwnerCleanupReason.EntryFailed);

                Assert(throwing.CallCount == 1, "The throwing cleanup participant should be invoked exactly once.");
                Assert(later.CallCount == 1 && later.RemainingResources == 0, "A throwing cleanup participant must not prevent a later participant from removing its owner roots.");
                Assert(summary.Entries.Any(entry => entry.ParticipantId == throwing.ParticipantId && !entry.Success) &&
                    summary.Entries.Any(entry => entry.ParticipantId == later.ParticipantId && entry.Success),
                    "Cleanup diagnostics should retain both explicit participant results alongside built-in Core participants.");
                Assert(summary.FailureCount == 1 && summary.RemainingResources == 0 && summary.RemovedResources == 1, "Participant exception isolation should report one failure while preserving later cleanup counts.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void CleanupParticipantThrowAndSinkFailuresRetryThroughDeactivation()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                PrepareFileSinkFailure(dir);
                var runtime = new DtmApiRuntime(new ThrowingLogHost(dir), new ConfigMenuRegistry());
                const string ownerId = "DTMAPI.Tests.CleanupThrowRetry";
                var callOrder = new List<string>();
                var throwOnce = new ThrowOnceOwnerCleanupParticipant("DTMAPI.Tests.CleanupThrowRetry.ThrowOnce", callOrder);
                var later = new RetryingOwnerCleanupParticipant("DTMAPI.Tests.CleanupThrowRetry.Later", failFirstCleanup: false, callOrder: callOrder);
                runtime.RegisterModOwnerCleanupParticipant(throwOnce);
                runtime.RegisterModOwnerCleanupParticipant(later);
                runtime.Start();

                string first = InvokeFailedOwnerCleanup(runtime, ownerId);
                Assert(callOrder.SequenceEqual(new[] { throwOnce.ParticipantId, later.ParticipantId }), "A throwing cleanup participant must not prevent the later participant from running in registration order under failed log sinks.");
                Assert(throwOnce.CallCount == 1 && throwOnce.RemainingResources == 1 && later.CallCount == 1 && later.RemainingResources == 0, "The first full deactivation pass should retain only the throw-once participant root.");
                Assert(first.Contains("participantCleanupFailures=1", StringComparison.Ordinal) && runtime.OwnerRequiresRestart(ownerId), "A participant exception must complete the deactivation finally path as inactive restart-required instead of remaining Deactivating.");

                string second = InvokeOwnerDeactivation(runtime, ownerId, ModOwnerCleanupReason.EntryFailed, shutdown: false);
                Assert(callOrder.SequenceEqual(new[] { throwOnce.ParticipantId, later.ParticipantId, throwOnce.ParticipantId, later.ParticipantId }), "Retry deactivation must invoke every participant again in the same deterministic order.");
                Assert(throwOnce.CallCount == 2 && throwOnce.RemainingResources == 0 && later.CallCount == 2, "The second full deactivation pass should retry and remove the root whose first cleanup threw.");
                Assert(second.Contains("participantCleanupFailures=0", StringComparison.Ordinal) && second.Contains("remaining=0", StringComparison.Ordinal), "The participant retry should prove zero current roots even when file, host, and diagnostic log sinks fail.");
                Assert(!runtime.OwnerRequiresRestart(ownerId) && CountCoreOwnerRootsForTest(runtime, ownerId) == 0, "A no-assembly owner should become reusable after a retry proves all participant and Core roots are zero.");

                string third = InvokeFailedOwnerCleanup(runtime, ownerId);
                Assert(!third.Contains("cleanupInProgressOrUnknown=true", StringComparison.Ordinal) && !runtime.OwnerRequiresRestart(ownerId), "A completed retry must remove the terminal lifecycle state so a later owner transaction cannot be stuck behind Deactivating.");
                Assert(throwOnce.CallCount == 3 && later.CallCount == 3, "A fresh no-assembly cleanup transaction should remain callable after participant recovery.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void CodeModMinimumRuntimeRejectsBeforeAssemblyLoad()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string gameDir = NewTempGameDir();
                string persistentRoot = Environment.GetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT") ?? throw new InvalidOperationException("Temporary persistent root was not configured.");
                const string ownerId = "DTMAPI.Tests.WorkshopNeeds062";
                const ulong workshopId = 3749000011UL;
                string workshopOfficialId = "Workshop." + workshopId.ToString(CultureInfo.InvariantCulture);
                string workshopRoot = Path.Combine(gameDir, "steamapps", "workshop", "content", "2285550", workshopId.ToString(CultureInfo.InvariantCulture));
                string contentRoot = Path.Combine(workshopRoot, "Content", "DTMAPI");
                Directory.CreateDirectory(contentRoot);

                string assemblyPath = typeof(AtomicOwnerProbeMod).Assembly.Location;
                string assemblyName = Path.GetFileName(assemblyPath);
                File.Copy(assemblyPath, Path.Combine(contentRoot, assemblyName), overwrite: true);
                File.WriteAllText(
                    Path.Combine(contentRoot, "manifest.json"),
                    "{ \"Name\": \"Workshop Needs 0.8.0\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"" + ownerId + "\", \"EntryDll\": \"Content/DTMAPI/" + assemblyName + "\", \"EntryType\": \"" + (typeof(AtomicOwnerProbeMod).FullName ?? nameof(AtomicOwnerProbeMod)) + "\", \"Type\": \"CodeMod\", \"CodeModKind\": \"Strict\", \"MinimumDTMApiVersion\": \"0.8.0\" }");
                WriteOfficialModInfos(persistentRoot, new OfficialModInfoTestEntry(workshopOfficialId, true, "Workshop"));

                AtomicOwnerProbeMod.ConstructorCount = 0;
                AtomicOwnerProbeMod.EntryCount = 0;
                var checkpoints = new List<ModLoadCheckpoint>();
                var runtime = new DtmApiRuntime(new FakeHost(gameDir), new ConfigMenuRegistry());
                runtime.UpdateNativeWorkshopSubscriptions(
                    true,
                    "unit-test-minimum-runtime-workshop",
                    string.Empty,
                    new[] { new NativeWorkshopSubscription(workshopId, workshopRoot, true, 0, workshopOfficialId) });
                runtime.ConfigureModLoadCheckpointForTests(checkpoints.Add);
                runtime.Start();

                DiscoveredMod discovered = runtime.DiscoveredMods.Single(mod => mod.Manifest.UniqueID == ownerId);
                Assert(discovered.Source == "Workshop" && discovered.OfficialId == workshopOfficialId && PathsEqual(discovered.RootPath, workshopRoot), "The stale-Runtime fixture must be discovered only from the enabled Workshop package.");
                Assert(discovered.Manifest.MinimumDTMApiVersion == "0.8.0" && !Directory.Exists(Path.Combine(persistentRoot, "MODS")), "The Workshop package must retain its future 0.8.0 minimum without an OfficialLocal shadow copy.");
                Assert(!runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == ownerId), "A Workshop CodeMod requiring 0.8.0 must not load on the current 0.7.0 Runtime.");
                Assert(AtomicOwnerProbeMod.ConstructorCount == 0 && AtomicOwnerProbeMod.EntryCount == 0, "The too-new Runtime contract must reject the CodeMod before construction and Entry.");
                Assert(checkpoints.Count == 0, "MinimumDTMApiVersion rejection must occur before the first post-Assembly.LoadFrom load checkpoint.");
                Assert(!GetPrivateField<HashSet<string>>(runtime, "loadedAssemblyOwnerIds").Contains(ownerId), "MinimumDTMApiVersion rejection must not record an assembly owner.");
                Assert(!GetPrivateField<Dictionary<string, DtmMod>>(runtime, "modInstances").ContainsKey(ownerId), "MinimumDTMApiVersion rejection must not publish a Mod instance.");
                Assert(!runtime.OwnerRequiresRestart(ownerId) && CountCoreOwnerRootsForTest(runtime, ownerId) == 0 && runtime.ModOwnerLedgerSnapshot.ActiveTransactions == 0, "Pre-assembly stale-Runtime rejection must leave zero restart state, owner roots and active transactions.");

                RuntimeSnapshot snapshot = runtime.CreateSnapshot();
                IDtmErrorInfo ownerError = snapshot.Errors.Single(error => error.Owner == ownerId);
                Assert(ownerError.Message.Contains("前置版本过旧", StringComparison.Ordinal) &&
                    ownerError.Details.Contains("MinimumDTMApiVersion=0.8.0", StringComparison.Ordinal) &&
                    ownerError.Details.Contains("installed runtime=0.7.0", StringComparison.Ordinal) &&
                    ownerError.Details.Contains("Run 1_install_dtmapi.bat", StringComparison.Ordinal),
                    "The blocked Workshop Mod must show its required/installed versions and exact Runtime update guidance.");
                IDtmModStatusInfo status = runtime.CreateDiagnosticsSnapshot().Mods.Single(mod => mod.UniqueID == ownerId);
                Assert(status.StatusCode == "api-too-new" && !status.Loaded, "The blocked Workshop CodeMod must expose a visible api-too-new status.");
                Assert(!snapshot.Errors.Any(error => error.Owner == ownerId &&
                    (error.Message.Contains("Failed to load code mod", StringComparison.OrdinalIgnoreCase) ||
                     error.Message.Contains("EntryDll", StringComparison.OrdinalIgnoreCase))),
                    "The stale-Runtime rejection must not fall through to EntryDll or code-load failure classification.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void CodeModPreAssemblyFailuresCanRepairInProcess()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                const string missingOwnerId = "DTMAPI.Tests.MissingAssemblyRetry";
                const string badImageOwnerId = "DTMAPI.Tests.BadImageRetry";
                string entryType = typeof(AtomicOwnerProbeMod).FullName ?? nameof(AtomicOwnerProbeMod);

                string missingDir = Path.Combine(dir, "Mods", "MissingAssemblyRetry");
                Directory.CreateDirectory(missingDir);
                const string missingDllName = "missing-retry.dll";
                File.WriteAllText(
                    Path.Combine(missingDir, "manifest.json"),
                    "{ \"Name\": \"Missing Assembly Retry\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"" + missingOwnerId + "\", \"EntryDll\": \"" + missingDllName + "\", \"EntryType\": \"" + entryType + "\", \"Type\": \"CodeMod\", \"CodeModKind\": \"Strict\" }");

                string badImageDir = Path.Combine(dir, "Mods", "BadImageRetry");
                Directory.CreateDirectory(badImageDir);
                const string badImageDllName = "bad-image-retry.dll";
                File.WriteAllText(Path.Combine(badImageDir, badImageDllName), "This is deliberately not a managed assembly.");
                File.WriteAllText(
                    Path.Combine(badImageDir, "manifest.json"),
                    "{ \"Name\": \"Bad Image Retry\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"" + badImageOwnerId + "\", \"EntryDll\": \"" + badImageDllName + "\", \"EntryType\": \"" + entryType + "\", \"Type\": \"CodeMod\", \"CodeModKind\": \"Strict\" }");

                AtomicOwnerProbeMod.EntryCount = 0;
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                runtime.Start();

                Assert(!runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == missingOwnerId || mod.Manifest.UniqueID == badImageOwnerId), "Missing and BadImage DLL owners must not publish any loaded state.");
                Assert(!runtime.OwnerRequiresRestart(missingOwnerId) && !runtime.OwnerRequiresRestart(badImageOwnerId), "Failures before a successful Assembly.LoadFrom must not create a restart gate.");
                Assert(CountCoreOwnerRootsForTest(runtime, missingOwnerId) == 0 && CountCoreOwnerRootsForTest(runtime, badImageOwnerId) == 0 && runtime.ModOwnerLedgerSnapshot.ActiveTransactions == 0, "Pre-assembly failures must leave zero owner roots and no active transaction.");

                string failedReport = runtime.ExportLogs();
                string failedContext = ReadZipText(failedReport, "DTMAPI-runtime-context.txt");
                string badImageFailure = failedContext.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .First(line => line.Contains("MANIFEST-LOAD-FAILURE owner=DTMAPI.ModScanner", StringComparison.Ordinal) && line.Contains("BadImageRetry", StringComparison.Ordinal));
                Assert(badImageFailure.Contains("entry-assembly-metadata-invalid", StringComparison.Ordinal), "BadImage diagnostics must expose the stable metadata pre-load failure without creating an assembly side-effect or restart gate.");

                string validAssembly = typeof(AtomicOwnerProbeMod).Assembly.Location;
                File.Copy(validAssembly, Path.Combine(missingDir, missingDllName), overwrite: true);
                File.Copy(validAssembly, Path.Combine(badImageDir, badImageDllName), overwrite: true);
                runtime.NotifyWorkshopModListChanged();

                Assert(runtime.LoadedMods.Count(mod => mod.Manifest.UniqueID == missingOwnerId || mod.Manifest.UniqueID == badImageOwnerId) == 2, "Repairing missing and BadImage DLLs in place should let both owners load in the same process.");
                Assert(AtomicOwnerProbeMod.EntryCount == 2 && !runtime.OwnerRequiresRestart(missingOwnerId) && !runtime.OwnerRequiresRestart(badImageOwnerId), "Each repaired owner should execute Entry exactly once without inheriting a stale restart gate.");

                File.WriteAllText(Path.Combine(missingDir, "dtmapi.disabled"), "disable after successful retry");
                File.WriteAllText(Path.Combine(badImageDir, "dtmapi.disabled"), "disable after successful retry");
                runtime.NotifyWorkshopModListChanged();
                Assert(!runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == missingOwnerId || mod.Manifest.UniqueID == badImageOwnerId), "Disabling repaired owners must remove their loaded publication.");
                Assert(CountCoreOwnerRootsForTest(runtime, missingOwnerId) == 0 && CountCoreOwnerRootsForTest(runtime, badImageOwnerId) == 0, "Repaired owner deactivation must clean every Core root.");
                Assert(runtime.OwnerRequiresRestart(missingOwnerId) && runtime.OwnerRequiresRestart(badImageOwnerId) && AtomicOwnerProbeMod.EntryCount == 2, "Only after Assembly.LoadFrom succeeds should deactivation become restart-required, without re-running Entry.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void FileMonitorSinkFailuresDoNotBlockOwnerReconciliation()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string directDir = NewTempGameDir();
                string invalidLogPath = Path.Combine(directDir, "log-path-is-a-directory");
                Directory.CreateDirectory(invalidLogPath);
                int diagnosticSinkCalls = 0;
                var monitor = new FileMonitor(
                    new ThrowingLogHost(directDir),
                    "DTMAPI.Tests.ThrowingLogSinks",
                    invalidLogPath,
                    (_, _, _) =>
                    {
                        diagnosticSinkCalls++;
                        throw new InvalidOperationException("injected-diagnostic-log-sink-failure");
                    });

                monitor.Log("info with every sink failing");
                monitor.Log("warning with every sink failing", LogLevel.Warn);
                monitor.Log("error with every sink failing", LogLevel.Error);
                monitor.LogException(new InvalidOperationException("logged-exception"), "exception with every sink failing");
                Assert(diagnosticSinkCalls >= 3, "The direct monitor fixture should exercise both failure reporting and error diagnostic sink calls without propagating exceptions.");

                string dir = NewTempGameDir();
                const string provider = "DTMAPI.Tests.ThrowingSinks.Provider";
                const string required = "DTMAPI.Tests.ThrowingSinks.Required";
                string providerDir = WriteAtomicProbePackage(dir, provider, string.Empty);
                WriteAtomicProbePackage(dir, required, ", \"Dependencies\": [{ \"UniqueID\": \"" + provider + "\", \"MinimumVersion\": \"1.0.0\", \"Required\": true }]");
                PrepareFileSinkFailure(dir);
                var runtime = new DtmApiRuntime(new ThrowingLogHost(dir), new ConfigMenuRegistry());
                AtomicOwnerProbeMod.EntryCount = 0;
                runtime.Start();
                Assert(runtime.LoadedMods.Count(mod => mod.Manifest.UniqueID == provider || mod.Manifest.UniqueID == required) == 2 && AtomicOwnerProbeMod.EntryCount == 2, "File and host log sink failures must not block initial ordinary Mod activation.");

                File.WriteAllText(Path.Combine(providerDir, "dtmapi.disabled"), "disable under throwing log sinks");
                runtime.NotifyWorkshopModListChanged();

                Assert(!runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == provider || mod.Manifest.UniqueID == required), "Throwing file/host log sinks must not block Workshop disable or required dependency cascade.");
                Assert(runtime.OwnerRequiresRestart(provider) && runtime.OwnerRequiresRestart(required), "Loaded DLL owners deactivated under throwing log sinks should still become restart-required.");
                Assert(CountCoreOwnerRootsForTest(runtime, provider) == 0 && CountCoreOwnerRootsForTest(runtime, required) == 0, "Throwing log sinks must not prevent authoritative owner cleanup from reaching zero roots.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void LightweightDefaultRetainsFullErrorEvidence()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                Assert(runtime.SaveLoadObjectSnapshotModeForDiagnostics == "Lite", "Ordinary player Runtime must default continuous object-graph evidence to Lite.");

                Exception captured;
                try
                {
                    throw new InvalidOperationException(
                        "outer-runtime-failure",
                        new FormatException("inner-causal-failure"));
                }
                catch (Exception ex)
                {
                    captured = ex;
                }

                string context = "owner=DTMAPI.Tests.Lightweight; operation=SaveLoaded; requestId=SL-unit";
                string logPath = Path.Combine(dir, "lightweight-error.log");
                var monitor = new FileMonitor(new FakeHost(dir), "DTMAPI.Tests.Lightweight", logPath);
                monitor.LogException(captured, context);
                string log = File.ReadAllText(logPath);
                Assert(log.Contains(context, StringComparison.Ordinal) &&
                    log.Contains("System.InvalidOperationException: outer-runtime-failure", StringComparison.Ordinal) &&
                    log.Contains("System.FormatException: inner-causal-failure", StringComparison.Ordinal) &&
                    log.Contains(nameof(LightweightDefaultRetainsFullErrorEvidence), StringComparison.Ordinal),
                    "Lite continuous diagnostics must not remove Error context, full inner exception text or the captured stack.");

                runtime.Diagnostics.RecordError("DTMAPI.Tests.Lightweight", context, captured.ToString());
                IDtmErrorInfo retained = runtime.Diagnostics.GetErrors().Single(error => error.Owner == "DTMAPI.Tests.Lightweight");
                Assert(retained.Message.Contains("operation=SaveLoaded", StringComparison.Ordinal) &&
                    retained.Details.Contains("outer-runtime-failure", StringComparison.Ordinal) &&
                    retained.Details.Contains("inner-causal-failure", StringComparison.Ordinal) &&
                    retained.Details.Contains(nameof(LightweightDefaultRetainsFullErrorEvidence), StringComparison.Ordinal),
                    "Retained player diagnostics must discover an Error without requiring Full continuous snapshots.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void FailedCodeModCleanupRunsAfterPartialEntryFailure()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                string modDir = Path.Combine(dir, "Mods", "ThrowAfterRegister");
                Directory.CreateDirectory(modDir);
                string assemblyPath = typeof(ThrowAfterRegisterProbeMod).Assembly.Location;
                string assemblyName = Path.GetFileName(assemblyPath);
                File.Copy(assemblyPath, Path.Combine(modDir, assemblyName), overwrite: true);
                File.WriteAllText(
                    Path.Combine(modDir, "manifest.json"),
                    "{ \"Name\": \"Throw After Register\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.ThrowAfterRegister\", \"Type\": \"CodeMod\", \"CodeModKind\": \"Strict\", \"EntryDll\": \"" + assemblyName + "\", \"EntryType\": \"" + (typeof(ThrowAfterRegisterProbeMod).FullName ?? nameof(ThrowAfterRegisterProbeMod)) + "\" }");

                ThrowAfterRegisterProbeMod.UpdateCalls = 0;
                var menu = new ConfigMenuRegistry();
                IConfigMenuRuntime menuRuntime = menu;
                var runtime = new DtmApiRuntime(new FakeHost(dir), menu);
                runtime.Start();
                runtime.Update();

                RuntimeSnapshot snapshot = runtime.CreateSnapshot();
                Assert(!snapshot.LoadedMods.Any(mod => mod.Manifest.UniqueID == "DTMAPI.Tests.ThrowAfterRegister"), "A code mod that throws after partial registration must not stay loaded.");
                Assert(snapshot.Errors.Any(error => error.Owner == "DTMAPI.Tests.ThrowAfterRegister" &&
                    (error.Message.Contains("throw-after-register-probe", StringComparison.Ordinal) ||
                    error.Details.Contains("throw-after-register-probe", StringComparison.Ordinal))), "The partial entry failure should remain visible in diagnostics.");
                Assert(ThrowAfterRegisterProbeMod.UpdateCalls == 0, "Failed code-mod cleanup should remove event handlers registered before Entry threw.");
                Assert(runtime.ModRegistry.GetApi<IUnitProbeApi>("DTMAPI.Tests.ThrowAfterRegister") == null, "Failed code-mod cleanup should remove APIs registered before Entry threw.");
                Assert(!GetInput(runtime).GetRegisteredButtons().Contains("F11", StringComparer.OrdinalIgnoreCase), "Failed code-mod cleanup should remove input buttons registered before Entry threw.");
                Assert(menuRuntime.GetPage("DTMAPI.Tests.ThrowAfterRegister") == null, "Failed code-mod cleanup should remove config pages registered before Entry threw.");
                Assert(runtime.CustomEntities.GetAnimalSnapshot("DTMAPI.Tests.ThrowAfterRegister").RegisteredDefinitionCount == 0, "Failed code-mod cleanup should remove custom entities registered before Entry threw.");
                Assert(!GetContent(runtime).FindAssets("json").Any(asset => asset.SourceModId.Equals("DTMAPI.Tests.ThrowAfterRegister", StringComparison.OrdinalIgnoreCase)), "A throwing Entry must not publish its owner content.");

                string report = runtime.ExportLogs();
                string reportContext = ReadZipText(report, "DTMAPI-runtime-context.txt");
                Assert(reportContext.Contains("MOD-LOAD-FAILURE owner=DTMAPI.Tests.ThrowAfterRegister statusCode=restart-required", StringComparison.Ordinal) &&
                    reportContext.Contains("throw-after-register-probe", StringComparison.Ordinal), "The runtime report should classify partial Entry failures under the terminal restart-required state while retaining the original Entry failure.");
                Assert(reportContext.Contains("AssemblyLoaded=True; RestartRequired=True", StringComparison.Ordinal) &&
                    reportContext.Contains("unknown Harmony/static/native/Unity side effects", StringComparison.Ordinal),
                    "The runtime report should tie restart-required side-effect risk to the successfully loaded managed assembly.");
                Assert(reportContext.Contains("FailedModRollback:", StringComparison.Ordinal) &&
                    reportContext.Contains("rolledBackTransactions=1", StringComparison.Ordinal), "The runtime report should include the failed mod transaction rollback summary.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void ModMonitorErrorsBecomeRuntimeDiagnostics()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                string modDir = Path.Combine(dir, "Mods", "MonitorError");
                Directory.CreateDirectory(modDir);
                string assemblyPath = typeof(MonitorErrorProbeMod).Assembly.Location;
                string assemblyName = Path.GetFileName(assemblyPath);
                File.Copy(assemblyPath, Path.Combine(modDir, assemblyName), overwrite: true);
                File.WriteAllText(
                    Path.Combine(modDir, "manifest.json"),
                    "{ \"Name\": \"Monitor Error\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.MonitorError\", \"Type\": \"CodeMod\", \"CodeModKind\": \"Strict\", \"EntryDll\": \"" + assemblyName + "\", \"EntryType\": \"" + (typeof(MonitorErrorProbeMod).FullName ?? nameof(MonitorErrorProbeMod)) + "\" }");

                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                runtime.Start();
                RuntimeSnapshot snapshot = runtime.CreateSnapshot();
                IDtmDiagnosticsSnapshot diagnosticsSnapshot = runtime.CreateDiagnosticsSnapshot();

                Assert(snapshot.LoadedMods.Any(mod => mod.Manifest.UniqueID == "DTMAPI.Tests.MonitorError"), "A mod that logs an internal error should remain loaded when Entry succeeds.");
                Assert(diagnosticsSnapshot.Mods.Any(mod => mod.UniqueID == "DTMAPI.Tests.MonitorError" && mod.StatusCode == "runtime-diagnostic"), "Loaded mod monitor errors should use a runtime-diagnostic status code instead of unknown-error.");
                Assert(snapshot.Errors.Any(error => error.Owner == "DTMAPI.Tests.MonitorError" && error.Message == "Mod monitor reported an error."), "Mod monitor Error lines should become structured runtime diagnostics.");

                string report = runtime.ExportLogs();
                string reportContext = ReadZipText(report, "DTMAPI-runtime-context.txt");
                Assert(reportContext.Contains("MOD-RUNTIME-DIAGNOSTIC owner=DTMAPI.Tests.MonitorError", StringComparison.Ordinal) &&
                    reportContext.Contains("loaded=true message=Mod monitor reported an error.", StringComparison.Ordinal), "Loaded mods that log errors should be classified as runtime diagnostics, not DTMAPI install failures.");
                Assert(!reportContext.Contains("MOD-LOAD-FAILURE owner=DTMAPI.Tests.MonitorError", StringComparison.Ordinal), "Monitor errors from loaded mods should not be misreported as load failures.");
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
                Assert(snapshot.Errors.Any(e => e.Owner == "DTMAPI.Tests.ThrowingEvents" && e.Message.Contains("GameLoop.UpdateTicked") && (e.Message.Contains("disabled") || e.Message.Contains("quarantined"))), "UpdateTicked circuit breaker should record a diagnostic.");
                Assert(snapshot.Errors.Any(e => e.Owner == "DTMAPI.Tests.ThrowingEvents" && e.Message.Contains("GameLoop.OneSecondUpdateTicked") && (e.Message.Contains("disabled") || e.Message.Contains("quarantined"))), "OneSecondUpdateTicked circuit breaker should record a diagnostic.");
                EventHandlerCleanupSnapshot cleanupSnapshot = runtime.Events.GetHandlerCleanupSnapshot();
                Assert(cleanupSnapshot.QuarantinedHandlers >= 2, "High-frequency failed handlers should be moved into quarantine instead of staying in the active dispatch path.");
                Assert(cleanupSnapshot.ActiveHandlers == 0, "Quarantined high-frequency handlers should no longer remain active.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void QuarantinedEventDelegateReleasesCapturedObject()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                var runtime = new DtmApiRuntime(new FakeHost(NewTempGameDir()), new ConfigMenuRegistry());
                runtime.Start();
                WeakReference captured = CreateAndQuarantineCapturedHandler(runtime);
                for (int i = 0; i < 8 && captured.IsAlive; i++)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                }
                Assert(!captured.IsAlive, "Quarantine must release the failed delegate so its captured object can be collected.");
                Assert(runtime.Events.GetHandlerCleanupSnapshot().QuarantinedHandlers == 1, "Quarantine should retain one bounded scalar owner/event count.");
                Assert(CountCoreOwnerRootsForTest(runtime, "DTMAPI.Tests.QuarantineWeak") == 1, "Authoritative Core root counting must include the named quarantine metadata that owner cleanup can remove.");
                Assert(runtime.Events.RemoveOwner("DTMAPI.Tests.QuarantineWeak") == 1 && CountCoreOwnerRootsForTest(runtime, "DTMAPI.Tests.QuarantineWeak") == 0, "Removing the quarantined owner should clear its final named Event root from the authoritative Core count.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference CreateAndQuarantineCapturedHandler(DtmApiRuntime runtime)
        {
            var payload = new WeakPayload();
            var weak = new WeakReference(payload);
            IEventsHelper events = CreateEventsProxy(runtime, "DTMAPI.Tests.QuarantineWeak");
            events.GameLoop.UpdateTicked += (_, _) =>
            {
                GC.KeepAlive(payload);
                throw new InvalidOperationException("quarantine-weak-probe");
            };
            runtime.Update();
            runtime.Update();
            runtime.Update();
            return weak;
        }

        private static void EventQuarantineOwnerMetadataIsBoundedAndCleanable()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                var runtime = new DtmApiRuntime(new FakeHost(NewTempGameDir()), new ConfigMenuRegistry());
                runtime.Start();
                const int ownerCount = 140;
                WeakReference captured = RegisterQuarantineOwnerStress(runtime, ownerCount);
                runtime.Update();
                runtime.Update();
                runtime.Update();

                EventSlotSnapshot slot = runtime.Events.GetHandlerCleanupSnapshot().Slots.Single(current =>
                    current.EventName == "GameLoop.UpdateTicked");
                Assert(slot.ActiveHandlers == 0 && slot.QuarantinedHandlers == 128, "Only the 128 named quarantine owners should count as current removable roots after all stress delegates are released.");
                Assert(slot.ByOwner.Count == 128 && !slot.ByOwner.ContainsKey("other"), "Overflow quarantine owners must not create a synthetic current-root bucket.");
                Assert(slot.TrimmedQuarantineCount == ownerCount - 128 && runtime.Events.GetHandlerCleanupSnapshot().TrimmedQuarantineCount == ownerCount - 128, "Overflow quarantines should remain only as a bounded historical trimmed scalar.");

                for (int i = 0; i < 8 && captured.IsAlive; i++)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                }
                Assert(!captured.IsAlive, "Bounded multi-owner quarantine must release captured delegates, not just aggregate their failure counts.");

                int trimmedRemoved = runtime.Events.RemoveOwner("DTMAPI.Tests.QuarantineOwner.139");
                EventSlotSnapshot afterTrimmedCleanup = runtime.Events.GetHandlerCleanupSnapshot().Slots.Single(current =>
                    current.EventName == "GameLoop.UpdateTicked");
                Assert(trimmedRemoved == 0 && afterTrimmedCleanup.QuarantinedHandlers == 128 && afterTrimmedCleanup.TrimmedQuarantineCount == ownerCount - 128, "Cleanup of a trimmed historical owner must remove no current root and must not mutate the trimmed scalar.");

                int removed = runtime.Events.RemoveOwner("DTMAPI.Tests.QuarantineOwner.000");
                EventSlotSnapshot afterCleanup = runtime.Events.GetHandlerCleanupSnapshot().Slots.Single(current =>
                    current.EventName == "GameLoop.UpdateTicked");
                Assert(removed == 1 && !afterCleanup.ByOwner.ContainsKey("DTMAPI.Tests.QuarantineOwner.000"), "Owner cleanup should delete its named quarantine aggregate.");
                Assert(afterCleanup.ByOwner.Count == 127 && afterCleanup.QuarantinedHandlers == 127 && afterCleanup.TrimmedQuarantineCount == ownerCount - 128, "Named owner cleanup must remove exactly one current root without changing bounded historical trimmed counts.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference RegisterQuarantineOwnerStress(DtmApiRuntime runtime, int ownerCount)
        {
            var payload = new WeakPayload();
            var weak = new WeakReference(payload);
            for (int i = 0; i < ownerCount; i++)
            {
                string ownerId = "DTMAPI.Tests.QuarantineOwner." + i.ToString("000", CultureInfo.InvariantCulture);
                IEventsHelper events = CreateEventsProxy(runtime, ownerId);
                if (i == 0)
                {
                    events.GameLoop.UpdateTicked += (_, _) =>
                    {
                        GC.KeepAlive(payload);
                        throw new InvalidOperationException("quarantine-owner-captured-probe");
                    };
                }
                else
                {
                    events.GameLoop.UpdateTicked += (_, _) => throw new InvalidOperationException("quarantine-owner-probe");
                }
            }
            return weak;
        }

        private static void SaveAndTitleBoundariesPreserveProcessLifetimeOwnerServices()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                var menu = new ConfigMenuRegistry();
                var runtime = new DtmApiRuntime(new FakeHost(dir), menu);
                runtime.Start();
                var owner = new ManifestModel { Name = "Process Owner", Author = "DTMAPI", Version = "1.0.0", UniqueID = "DTMAPI.Tests.ProcessOwner" };
                CreateEventsProxy(runtime, owner.UniqueID).GameLoop.UpdateTicked += (_, _) => { };
                IInputHelper ownerInput = runtime.Input.CreateOwnerBound(owner.UniqueID);
                ownerInput.RegisterButton("F13");
                runtime.ModRegistry.RegisterApiForOwner<IUnitProbeApi>(owner, new UnitProbeApi(owner.UniqueID));
                runtime.Config.RegisterMigration<HotLoadProbeConfig>(owner, _ => { });
                menu.Register(owner, () => { }, () => { });
                runtime.RecordInputFrame(new[]
                {
                    new InputButtonSample("F13", true, true, false),
                    new InputButtonSample("F14", true, true, false),
                    new InputButtonSample("F15", false, false, true)
                });
                ownerInput.IsDown(DtmButton.Parse("F14"));
                ownerInput.WasReleased(DtmButton.Parse("F15"));
                ownerInput.WasPressed(DtmButton.Parse("F16"));
                Assert(runtime.Input.CountOwnerResources(owner.UniqueID) == 4 && runtime.Input.GetLocalSnapshotDiagnostics().WatchCount == 3, "The SaveLoaded fixture should contain one persistent registration and three transient owner-local snapshot watches.");
                Assert(runtime.Input.IsDown("F13") && runtime.Input.IsDown("F14") && runtime.Input.WasPressed("F13") && runtime.Input.WasPressed("F14") && runtime.Input.WasReleased("F15"), "The SaveLoaded fixture should contain transient down, pressed, and released state.");

                runtime.NotifySaveLoaded(isNewGame: false);

                Assert(runtime.Input.CountOwnerResources(owner.UniqueID) == 1 && runtime.Input.GetLocalSnapshotDiagnostics().WatchCount == 0, "SaveLoaded must remove owner-local snapshot watches while retaining exactly the persistent registration root.");
                Assert(!ownerInput.IsDown("F13") && !ownerInput.IsDown("F14") && !ownerInput.WasPressed("F13") && !ownerInput.WasPressed("F14") && !ownerInput.WasReleased("F15"), "SaveLoaded must hide held/edge state behind neutral rearm without erasing physical truth.");
                Assert(runtime.Input.GetRegisteredButtons().Contains("F13", StringComparer.OrdinalIgnoreCase), "SaveLoaded must preserve persistent input registrations.");
                Assert(runtime.ModRegistry.GetApi<IUnitProbeApi>(owner.UniqueID) != null &&
                    ((IConfigMenuRuntime)menu).GetPage(owner.UniqueID) != null &&
                    runtime.Config.CountOwner(owner.UniqueID) == 1 &&
                    runtime.Events.GetHandlerCleanupSnapshot().Slots.Any(slot => slot.ByOwner.ContainsKey(owner.UniqueID)),
                    "SaveLoaded must preserve process-lifetime Event, API, ConfigPage, and migration services.");

                runtime.RecordInputFrame(new[]
                {
                    new InputButtonSample("F14", true, true, false),
                    new InputButtonSample("F15", false, false, true)
                });
                ownerInput.IsDown(DtmButton.Parse("F14"));
                ownerInput.WasReleased(DtmButton.Parse("F15"));
                ownerInput.WasPressed(DtmButton.Parse("F16"));
                Assert(runtime.Input.CountOwnerResources(owner.UniqueID) == 4 && runtime.Input.GetLocalSnapshotDiagnostics().WatchCount == 3, "Transient input watches should be able to rebuild after SaveLoaded without duplicating the persistent registration.");
                runtime.NotifyReturnedToTitle();

                Assert(runtime.ModRegistry.GetApi<IUnitProbeApi>(owner.UniqueID) != null, "Save/title boundaries must preserve process-lifetime APIs.");
                Assert(((IConfigMenuRuntime)menu).GetPage(owner.UniqueID) != null, "Save/title boundaries must preserve config pages.");
                Assert(runtime.Config.CountOwner(owner.UniqueID) == 1, "Save/title boundaries must preserve config migrations.");
                Assert(runtime.Input.GetRegisteredButtons().Contains("F13", StringComparer.OrdinalIgnoreCase), "Save/title boundaries must preserve persistent input registrations.");
                Assert(runtime.Events.GetHandlerCleanupSnapshot().Slots.Any(slot => slot.ByOwner.ContainsKey(owner.UniqueID)), "Save/title boundaries must preserve event registrations.");
                Assert(runtime.Input.CountOwnerResources(owner.UniqueID) == 1 && runtime.Input.GetLocalSnapshotDiagnostics().WatchCount == 0, "ReturnedToTitle must clear rebuilt local snapshot watches while retaining the persistent registration.");
                Assert(!ownerInput.IsDown("F13") && !ownerInput.IsDown("F14") && !ownerInput.WasPressed("F13") && !ownerInput.WasPressed("F14") && !ownerInput.WasReleased("F15"), "ReturnedToTitle must require neutral before exposing a new input cycle.");
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

        private static void DisabledDiscoveredModLocksConfigPage()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
            string dir = NewTempGameDir();
            string persistentRoot = Environment.GetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT")
                ?? throw new InvalidOperationException("Temporary persistent root was not configured.");
            string modDir = Path.Combine(persistentRoot, "MODS", "Disabled");
            Directory.CreateDirectory(modDir);
            File.WriteAllText(Path.Combine(modDir, "manifest.json"), "{ \"Name\": \"Disabled\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.Disabled\", \"EntryDll\": \"Disabled.dll\" }");
            WriteOfficialModInfos(persistentRoot, "Local.Disabled", enabled: false);

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

            var runtime = new DtmApiRuntime(
                new FakeHost(dir, includeLegacyDevelopmentModSourceForTests: false),
                menu);
            runtime.Start();

            IConfigMenuPage page = menuRuntime.GetPage(manifest.UniqueID) ?? throw new InvalidOperationException("Disabled page should exist.");
            Assert(page.IsLocked, "Disabled discovered mod should lock its config page.");
            Assert(page.LockReason.IndexOf("Doloc Town 官方 Mod 界面", StringComparison.OrdinalIgnoreCase) >= 0, "Disabled lock should explain the official disabled state with Chinese-first text.");

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

        private static void UnverifiedWorkshopAndGameModsCannotOverrideOfficialLocalState()
        {
            DiscoveredMod? enabledWorkshop = RunDuplicateSourceSelectionCase(
                "EnabledWorkshop",
                officialLocalEnabled: false,
                workshopEnabled: true,
                localDisabled: true,
                workshopId: 3749000001UL,
                out DtmApiRuntime enabledWorkshopRuntime);
            Assert(enabledWorkshop != null && enabledWorkshop.Source == "Local" && !enabledWorkshop.OfficialEnabled,
                "An unverified Workshop directory cannot become the selected source; the sole official Local row remains visible as disabled diagnostics.");
            Assert(!enabledWorkshopRuntime.LoadedMods.Any(mod => mod.Manifest.UniqueID == "Yuuka.DTMAPI.Duplicate.EnabledWorkshop"),
                "A Workshop enabled only in stale file state must not load without current native install-path proof.");

            DiscoveredMod? enabledOfficialAndWorkshop = RunDuplicateSourceSelectionCase(
                "EnabledOfficial",
                officialLocalEnabled: true,
                workshopEnabled: true,
                localDisabled: true,
                workshopId: 3749000002UL,
                out _);
            Assert(enabledOfficialAndWorkshop != null && enabledOfficialAndWorkshop.Source == "Local" && enabledOfficialAndWorkshop.OfficialEnabled,
                "An enabled official Local candidate must win when the duplicate Workshop directory lacks current subscription-path proof.");

            DiscoveredMod? allDisabled = RunDuplicateSourceSelectionCase(
                "AllDisabled",
                officialLocalEnabled: false,
                workshopEnabled: false,
                localDisabled: true,
                workshopId: 3749000003UL,
                out DtmApiRuntime allDisabledRuntime);
            Assert(allDisabled != null && !allDisabled.OfficialEnabled &&
                !allDisabledRuntime.LoadedMods.Any(mod => mod.Manifest.UniqueID == "Yuuka.DTMAPI.Duplicate.AllDisabled"),
                "All-disabled official candidates must remain unloaded even when raw duplicate directories exist.");

            DiscoveredMod? enabledLocal = RunDuplicateSourceSelectionCase(
                "EnabledLocal",
                officialLocalEnabled: false,
                workshopEnabled: false,
                localDisabled: false,
                workshopId: 3749000004UL,
                out DtmApiRuntime enabledLocalRuntime);
            Assert(enabledLocal != null && enabledLocal.Source == "Local" && !enabledLocal.OfficialEnabled &&
                !enabledLocalRuntime.LoadedMods.Any(mod => mod.Manifest.UniqueID == "Yuuka.DTMAPI.Duplicate.EnabledLocal"),
                "A same-ID package under <game>/Mods must not override disabled official Local/Workshop state.");
            AuthorSourceSelectionDecision decision = enabledLocalRuntime.AuthorSourceSelectionDecisions.Single(row =>
                row.UniqueId == "Yuuka.DTMAPI.Duplicate.EnabledLocal");
            Assert(decision.Candidates.Count == 2 && decision.Candidates.All(candidate =>
                !PathsEqualForTest(candidate.RootPath, Path.Combine(enabledLocalRuntime.Paths.LegacyDevelopmentModsPath, "Yuuka_DTMAPI_Duplicate_EnabledLocal"))),
                "Player discovery must retain only the two official physical candidates; <game>/Mods is not a candidate.");
        }

        private static DtmApiRuntime StartSourceAuthorityRuntime(
            string gameDir,
            ulong workshopId,
            string workshopRoot,
            bool nativeSnapshotAvailable,
            bool nativeEnabled = true,
            int nativePriority = 20)
        {
            var runtime = new DtmApiRuntime(
                new FakeHost(gameDir, includeLegacyDevelopmentModSourceForTests: false),
                new ConfigMenuRegistry());
            runtime.UpdateNativeWorkshopSubscriptions(
                nativeSnapshotAvailable,
                "unit-test-native-owner",
                nativeSnapshotAvailable ? string.Empty : "unit-test-native-owner-unavailable",
                nativeSnapshotAvailable
                    ? new[] { new NativeWorkshopSubscription(workshopId, workshopRoot, nativeEnabled: nativeEnabled, nativePriority: nativePriority, nativeOfficialId: "Workshop." + workshopId.ToString(CultureInfo.InvariantCulture)) }
                    : Array.Empty<NativeWorkshopSubscription>());
            runtime.Start();
            return runtime;
        }

        private static void RuntimeUiBoundariesIsolateInputWithoutStoppingModUpdates()
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
            int saveScopedPressed = 0;
            int gameplayScopedPressed = 0;
            events.Input.ButtonPressed += (_, _) => pressed++;
            events.Input.ButtonReleased += (_, _) => released++;
            events.Input.KeybindPressed += (_, e) =>
            {
                if (e.KeybindId == "save-ui-close")
                    saveScopedPressed++;
                else if (e.KeybindId == "gameplay-only")
                    gameplayScopedPressed++;
            };
            events.GameLoop.UpdateTicked += (_, _) => updates++;
            events.UI.MenuOpened += (_, _) => menuOpened++;
            events.UI.MenuClosed += (_, _) => menuClosed++;

            IInputHelper scopedInput = ((InputService)GetInput(runtime)).CreateOwnerBound("DTMAPI.Tests.Events");
            scopedInput.RegisterKeybind("save-ui-close", "Y", DtmInputScope.SaveLoaded);
            scopedInput.RegisterKeybind("gameplay-only", "F6", DtmInputScope.Gameplay);
            scopedInput.RegisterButton("F10");

            runtime.Start();
            runtime.UI.SetUiContext("Gameplay", canDrawOverlay: true, gameplayHotkeysAllowed: true, reason: "unit test");
            runtime.RecordInputFrame(new[] { new InputButtonSample("F10", true, true, false) });
            runtime.RecordInputFrame(new[] { new InputButtonSample("F10", false, false, true) });
            runtime.Update();
            Assert(pressed == 1 && released == 1 && updates == 1, $"Gameplay context should deliver input and update events. pressed={pressed} released={released} updates={updates} open={runtime.UI.IsOpen} context={runtime.UI.InputContext}");

            runtime.UI.OpenOwnerBoundCustomMenu("DTMAPI.Tests.OwnerModal", "DTMAPI.Tests.Events");
            Assert(menuOpened == 1 && runtime.UI.BlocksGameplayHotkeys, "An owner modal should block gameplay input while leaving the shared update clock independent.");
            runtime.Input.ClearFrame();
            runtime.RecordInputFrame(new[] { new InputButtonSample("Y", false, true, true) });
            runtime.Input.ClearFrame();
            runtime.RecordInputFrame(new[] { new InputButtonSample("F6", false, true, true) });
            Assert(saveScopedPressed == 1, "A SaveLoaded-scoped keybind must remain targeted to its owner while that owner's in-save modal is open.");
            Assert(gameplayScopedPressed == 0, "A Gameplay-scoped keybind must remain blocked while an owner modal is open.");
            int pressedAfterSaveScopedUiInput = pressed;
            int releasedAfterSaveScopedUiInput = released;
            runtime.RecordInputFrame(new[] { new InputButtonSample("F10", true, true, false) });
            runtime.RecordInputFrame(new[] { new InputButtonSample("F10", false, false, true) });
            runtime.Update();
            Assert(pressed == pressedAfterSaveScopedUiInput && released == releasedAfterSaveScopedUiInput && updates == 2, "Owner modal must not deliver gameplay input, while UpdateTicked continues with native world time.");
            Assert(!runtime.IsInputDown("F10"), "Blocked input should not leave a stuck down-state.");

            runtime.UI.Close();
            Assert(menuClosed == 1, "Close should dispatch the menu closed event.");
            runtime.UI.SetUiContext("ModUiState", canDrawOverlay: true, gameplayHotkeysAllowed: false, reason: "official mod menu");
            runtime.RecordInputFrame(new[] { new InputButtonSample("F10", true, true, false) });
            runtime.RecordInputFrame(new[] { new InputButtonSample("F10", false, false, true) });
            Assert(pressed == pressedAfterSaveScopedUiInput && released == releasedAfterSaveScopedUiInput, "Official UI contexts should expose a platform-modal audience and deliver no Mod input.");
            Assert(!runtime.IsInputDown("F10"), "Blocked official-menu input should not leave a stuck down-state.");
            runtime.Update();
            Assert(updates == 3, "Platform-modal input isolation must not stop the shared UpdateTicked clock.");

            runtime.UI.SetUiContext("Gameplay", canDrawOverlay: true, gameplayHotkeysAllowed: true, reason: "unit test");
            runtime.RecordInputFrame(new[] { new InputButtonSample("F10", true, true, false) });
            runtime.RecordInputFrame(new[] { new InputButtonSample("F10", false, false, true) });
            runtime.Update();
            Assert(pressed == pressedAfterSaveScopedUiInput + 1 && released == releasedAfterSaveScopedUiInput + 1 && updates == 4, $"Gameplay input should resume after closing the menu while updates remain continuous. pressed={pressed}/{pressedAfterSaveScopedUiInput + 1} released={released}/{releasedAfterSaveScopedUiInput + 1} updates={updates}/4");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void FrozenCompatibilityApisPublishSeparateStabilityDispositionAndWarnings()
        {
            Type[] frozenTypes =
            {
                typeof(IActionCompletionApi),
                typeof(ActionCompletionOptions),
                typeof(IFishingAutomationApi),
                typeof(FishingBiteWaitMode),
                typeof(FishingResultMode),
                typeof(FishingAnimationMode),
                typeof(FishingAutomationOptions),
                typeof(FishingAutomationState),
                typeof(IActionSpeedApi),
                typeof(ActionSpeedOptions),
                typeof(IItemTooltipApi),
                typeof(FishRoeTooltipOptions),
                typeof(FishRoeDisplayInfo),
                typeof(IAnimalViewerApi),
                typeof(AnimalHusbandryProgressOptions),
                typeof(IMachineProductionApi),
                typeof(MachineDefinition),
                typeof(MachineRecipeInput),
                typeof(MachineOutputRule),
                typeof(MachineRegisterResult),
                typeof(MachineProductionState),
                typeof(IEquipmentSlotsApi),
                typeof(EquipmentSlotsOptions),
                typeof(EquipmentSlotsRegisterResult),
                typeof(EquipmentSlotsState),
                typeof(EquipmentSlotInfo),
                typeof(EquipmentSlotEquipResult),
                typeof(EquipmentSlotsRecoveryResult),
                typeof(ISaveSlotsApi),
                typeof(SaveSlotsOptions),
                typeof(SaveSlotsRegisterResult),
                typeof(SaveSlotsState),
                typeof(ICameraViewApi),
                typeof(ICameraViewLease),
                typeof(CameraViewRequest),
                typeof(CameraViewResult),
                typeof(CameraViewState),
                typeof(ICameraZoomApi),
                typeof(CameraZoomOptions),
                typeof(CameraZoomRegisterResult),
                typeof(CameraZoomResult),
                typeof(CameraZoomState),
                typeof(IChestLocatorEnhancerApi),
                typeof(ChestLocatorEnhancerOptions),
                typeof(ChestLocatorEnhancerRegisterResult),
                typeof(ChestLocatorEnhancerState)
            };

            foreach (Type type in frozenTypes)
            {
                DtmApiStatusAttribute stability = type.GetCustomAttribute<DtmApiStatusAttribute>()
                    ?? throw new InvalidOperationException(type.Name + " must publish stability metadata independently of disposition.");
                Assert(stability.Status == DtmApiStatus.Experimental,
                    type.Name + " must remain Experimental while its compatibility disposition is frozen.");

                DtmApiDispositionAttribute disposition = type.GetCustomAttribute<DtmApiDispositionAttribute>()
                    ?? throw new InvalidOperationException(type.Name + " must publish a frozen disposition.");
                Assert(disposition.Disposition == DtmApiDisposition.Frozen && disposition.Since == "0.5.5",
                    type.Name + " must publish the 0.5.5 Frozen disposition without changing its stability level.");

                ObsoleteAttribute obsolete = type.GetCustomAttribute<ObsoleteAttribute>()
                    ?? throw new InvalidOperationException(type.Name + " must carry a source warning while its ABI is retained.");
                Assert(!obsolete.IsError && (obsolete.Message ?? string.Empty).Contains("frozen", StringComparison.OrdinalIgnoreCase),
                    type.Name + " must use Obsolete(..., false) with an explicit frozen warning.");
            }

            Assert(typeof(IItemDisplayNameApi).GetCustomAttribute<DtmApiDispositionAttribute>() == null,
                "An API without disposition metadata must retain the documented Open default instead of receiving an unrelated frozen classification.");
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

        private static void PrepareFileSinkFailure(string gameDir)
        {
            string latestLogPath = Path.Combine(gameDir, "DTMAPI", "logs", "latest.log");
            Directory.CreateDirectory(latestLogPath);
        }

        private static bool PathsEqual(string left, string right)
        {
            return string.Equals(
                Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsPathUnder(string path, string root)
        {
            string relative = Path.GetRelativePath(Path.GetFullPath(root), Path.GetFullPath(path));
            return !Path.IsPathRooted(relative) &&
                !relative.Equals("..", StringComparison.Ordinal) &&
                !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) &&
                !relative.StartsWith(".." + Path.AltDirectorySeparatorChar, StringComparison.Ordinal);
        }

        private static void DeleteDirectoryQuietly(string path)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
                    Directory.Delete(path, recursive: true);
            }
            catch
            {
            }
        }

        private sealed class ThrowingOwnerCleanupParticipant : IModOwnerCleanupParticipant
        {
            public ThrowingOwnerCleanupParticipant(string participantId)
            {
                ParticipantId = participantId;
            }

            public string ParticipantId { get; }

            public int CallCount { get; private set; }

            public ModOwnerCleanupParticipantResult RemoveOwner(string ownerId, ModOwnerCleanupReason reason)
            {
                CallCount++;
                throw new InvalidOperationException("cleanup-participant-probe");
            }
        }

        private sealed class ThrowOnceOwnerCleanupParticipant : IModOwnerCleanupParticipant
        {
            private readonly List<string> callOrder;

            public ThrowOnceOwnerCleanupParticipant(string participantId, List<string> callOrder)
            {
                ParticipantId = participantId;
                this.callOrder = callOrder;
                RemainingResources = 1;
            }

            public string ParticipantId { get; }

            public int CallCount { get; private set; }

            public int RemainingResources { get; private set; }

            public ModOwnerCleanupParticipantResult RemoveOwner(string ownerId, ModOwnerCleanupReason reason)
            {
                CallCount++;
                callOrder.Add(ParticipantId);
                if (CallCount == 1)
                    throw new InvalidOperationException("cleanup-participant-throw-once-probe");

                int removed = RemainingResources;
                RemainingResources = 0;
                return new ModOwnerCleanupParticipantResult(removed, RemainingResources, "Removed root after the injected participant exception.");
            }
        }

        private sealed class OwnerCleanupOrderProbe : IModOwnerCleanupParticipant
        {
            public OwnerCleanupOrderProbe(string participantId)
            {
                ParticipantId = participantId;
            }

            public string ParticipantId { get; }

            public List<string> OwnerIds { get; } = new List<string>();

            public ModOwnerCleanupParticipantResult RemoveOwner(string ownerId, ModOwnerCleanupReason reason)
            {
                OwnerIds.Add(ownerId ?? string.Empty);
                return new ModOwnerCleanupParticipantResult(0, 0, "Recorded owner cleanup order.");
            }
        }

        private sealed class AtomicCheckpointCleanupParticipant : IAtomicCheckpointParticipantApi, IModOwnerCleanupParticipant
        {
            public const string ProviderId = AtomicCheckpointParticipantProviderId;
            private readonly HashSet<string> owners = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            public string ParticipantId => ProviderId;

            public int RegisterCalls { get; private set; }

            public int CleanupCalls { get; private set; }

            public int RemovedResources { get; private set; }

            public void RegisterOwner(string ownerId)
            {
                RegisterCalls++;
                owners.Add(ownerId ?? string.Empty);
            }

            public int CountOwnerResources(string ownerId) => owners.Contains(ownerId ?? string.Empty) ? 1 : 0;

            public ModOwnerCleanupParticipantResult RemoveOwner(string ownerId, ModOwnerCleanupReason reason)
            {
                CleanupCalls++;
                string normalizedOwnerId = ownerId ?? string.Empty;
                int removed = owners.Remove(normalizedOwnerId) ? 1 : 0;
                RemovedResources += removed;
                return new ModOwnerCleanupParticipantResult(removed, CountOwnerResources(normalizedOwnerId), "Atomic checkpoint participant cleanup.");
            }
        }

        private static DiscoveredMod CreateDiscoveredContentPack(string rootPath, string uniqueId, string name, string source = "Local", bool officialManaged = false)
        {
            Directory.CreateDirectory(rootPath);
            string manifestPath = Path.Combine(rootPath, "manifest.json");
            if (!File.Exists(manifestPath))
                File.WriteAllText(manifestPath, "{ \"Name\": \"" + name + "\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"" + uniqueId + "\", \"Type\": \"ContentPack\" }");

            return new DiscoveredMod(
                new ManifestModel { Name = name, Author = "DTMAPI", Version = "1.0.0", UniqueID = uniqueId, Type = "ContentPack" },
                rootPath,
                manifestPath,
                source,
                null,
                true,
                true,
                officialManaged ? "Local." + Path.GetFileName(rootPath) : string.Empty,
                officialManaged,
                string.Empty);
        }

        private static void WriteManifest(string gameDir, string folderName, string json)
        {
            string modDir = Path.Combine(gameDir, "Mods", folderName);
            Directory.CreateDirectory(modDir);
            File.WriteAllText(Path.Combine(modDir, "manifest.json"), json);
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

        private static void DispatchOneSecondUpdateTicked(DtmApiRuntime runtime, uint second)
        {
            PropertyInfo? eventsProperty = typeof(DtmApiRuntime).GetProperty("Events", BindingFlags.Instance | BindingFlags.NonPublic);
            object events = eventsProperty?.GetValue(runtime) ?? throw new InvalidOperationException("Runtime events should exist.");
            MethodInfo dispatch = events.GetType().GetMethod("DispatchOneSecondUpdateTicked", BindingFlags.Instance | BindingFlags.Public) ?? throw new InvalidOperationException("DispatchOneSecondUpdateTicked should exist.");
            dispatch.Invoke(events, new object[] { second });
        }

        private sealed class ThrowingLogHost : IRuntimeHost, ILegacyDevelopmentModSourceTestHost
        {
            public ThrowingLogHost(string gamePath)
            {
                GamePath = gamePath;
                PluginPath = Path.Combine(gamePath, "BepInEx", "plugins");
            }

            public string GamePath { get; }
            public string PluginPath { get; }
            public string HostName => "ThrowingLogHost";
            public bool IncludeLegacyDevelopmentModSourceForTests => true;
            public void Log(string message) => throw new InvalidOperationException("injected-host-info-log-failure");
            public void LogWarning(string message) => throw new InvalidOperationException("injected-host-warning-log-failure");
            public void LogError(string message, Exception? exception = null) => throw new InvalidOperationException("injected-host-error-log-failure");
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

        [DataContract]
        private sealed class SampleConfig
        {
            [DataMember] public bool Enabled { get; set; } = true;
            [DataMember] public int Count { get; set; } = 7;
        }

        public interface ISecondProbeApi
        {
            string Owner { get; }
        }

        public sealed class DualProbeApi : IUnitProbeApi, ISecondProbeApi
        {
            public DualProbeApi(string owner) => Owner = owner;
            public string Owner { get; }
        }

        private sealed class WeakPayload
        {
            public byte[] Buffer { get; } = new byte[1024];
        }
    }
}
