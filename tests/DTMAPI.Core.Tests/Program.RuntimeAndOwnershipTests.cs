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
                Assert(!started.LoadedMods.Any(mod => mod.Manifest.UniqueID == providerId) && started.Errors.Any(error => error.Owner == providerId && error.Message.Contains("保留", StringComparison.Ordinal)),
                    "An ordinary source collision with a dynamically registered process provider must be rejected before Assembly.LoadFrom. Discovery errors: " +
                    string.Join(" | ", started.Errors.Select(error => error.Owner + ": " + error.Message + " " + error.Details)));
                Assert(started.LoadedMods.Count(mod => mod.Manifest.UniqueID == consumerId) == 1 && AtomicOwnerProbeMod.EntryCount == 1, "An ordinary consumer should load once against the preserved dynamic process provider. " + string.Join(" | ", started.Errors.Select(error => error.Owner + ": " + error.Message + " " + error.Details)));
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
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry(), new BindingPatchCleanup(System.Threading.Thread.CurrentThread.ManagedThreadId));
                runtime.Start();

                DiscoveredMod ordinaryProvider = runtime.LoadedMods.Single(mod => mod.Manifest.UniqueID == providerId);
                Assert(runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == consumerId) && AtomicOwnerProbeMod.EntryCount == 1, "The ordinary-first collision fixture should begin with an active source provider and required code consumer.");
                AssertThrows(
                    () => runtime.RegisterRuntimeApi<ISecondProbeApi>(ordinaryProvider.Manifest, new DualProbeApi("late-platform-contract")),
                    "A process-lifetime provider must not claim an owner ID after an ordinary source owner is already loaded.");
                Assert(runtime.ModRegistry.GetApi<ISecondProbeApi>(providerId) == null &&
                    ReferenceEquals(runtime.ModRegistry.Get(providerId), ordinaryProvider.Manifest),
                    "Rejected ordinary-first collision must preserve the source manifest and publish no platform API contract.");

                int providerRoots = CountCoreOwnerRootsForTest(runtime, providerId);
                int consumerRoots = CountCoreOwnerRootsForTest(runtime, consumerId);
                IUnitProbeApi? consumerApi = runtime.ModRegistry.GetApi<IUnitProbeApi>(consumerId);
                SetFixtureModEnabled(ordinaryProvider.RootPath, false);
                runtime.NotifyWorkshopModListChanged();

                Assert(runtime.DiscoveredMods.Single(mod => mod.Manifest.UniqueID == providerId).OfficialEnabled == false &&
                    runtime.ModRegistry.IsLoaded(providerId) && runtime.ModRegistry.IsLoaded(consumerId),
                    "An official disable refreshes discovery while the running provider and its consumer stay loaded until restart.");
                Assert(CountCoreOwnerRootsForTest(runtime, providerId) == providerRoots && CountCoreOwnerRootsForTest(runtime, consumerId) == consumerRoots &&
                    ReferenceEquals(runtime.ModRegistry.GetApi<IUnitProbeApi>(consumerId), consumerApi),
                    "A list refresh must preserve actual owner registrations and the existing consumer API object.");
                Assert(runtime.ModRegistry.GetApi<ISecondProbeApi>(providerId) == null && AtomicOwnerProbeMod.EntryCount == 1,
                    "The rejected platform binding must remain absent and list refresh must not repeat Entry.");

                string cleanup = runtime.DeactivateOwner(providerId, ModOwnerCleanupReason.EntryFailed, shutdown: false);
                Assert(!runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == providerId || mod.Manifest.UniqueID == consumerId), "Explicit provider cleanup must still cascade to its required consumer after the rejected platform claim. " + cleanup);
                Assert(CountCoreOwnerRootsForTest(runtime, providerId) == 0 && CountCoreOwnerRootsForTest(runtime, consumerId) == 0, "Explicit provider cleanup and dependent cascade must leave no Core owner roots.");
                Assert(!runtime.OwnerRequiresRestart(providerId) && runtime.OwnerRequiresRestart(consumerId), "The non-code source provider should remain reusable while the explicitly deactivated consumer with a loaded assembly becomes restart-required.");
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

        private static void RuntimeDiscoveryIgnoresUnconsumedContent()
        {
            string? previousRoot = UseTempPersistentRoot();
            DtmApiRuntime? runtime = null;
            try
            {
                string dir = NewTempGameDir();
                const string owner = "DTMAPI.Tests.UnconsumedContent";
                WriteManifest(dir, owner, "{ \"Name\": \"Unconsumed content\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"" + owner + "\", \"Type\": \"ContentPack\" }");
                string content = CreateEnabledFixtureModDirectory(dir, owner, "Content", "DTMAPI");
                Directory.CreateDirectory(content);
                string unused = Path.Combine(content, "custom-animals.json");
                File.WriteAllText(unused, "not-json");
                using var held = new FileStream(unused, FileMode.Open, FileAccess.Read, FileShare.None);
                runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                runtime.Start();
                runtime.NotifyWorkshopModListChanged();
                Assert(runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == owner), "Unused content must not prevent the owner from loading.");
                Assert(!runtime.Diagnostics.GetWarnings().Any(w => w.Details.Contains("custom-animals", StringComparison.Ordinal)), "Discovery must not read or diagnose content owned by an absent domain consumer.");
            }
            finally
            {
                try { runtime?.NotifyRuntimeShutdown("unused-content-test"); }
                finally { RestorePersistentRoot(previousRoot); }
            }
        }

        private static void ContentManifestRegistryBuildsAuthoritativeIndexAndCapabilities()
        {
            string dir = NewTempGameDir();
            string goodRoot = Path.Combine(dir, "Mods", "GoodAnimal");
            Directory.CreateDirectory(Path.Combine(goodRoot, "Content", "DTMAPI"));
            File.WriteAllText(Path.Combine(goodRoot, "Content", "DTMAPI", "audio-replacements.json"), "[{ \"category\": \"AnimalVoice\" }]");
            File.WriteAllText(Path.Combine(goodRoot, "manifest.json"), "{ \"Name\": \"Good Animal\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.GoodAnimal\", \"Type\": \"ContentPack\" }");
            File.WriteAllText(Path.Combine(goodRoot, "Content", "DTMAPI", "custom-animals.json"), "[ { \"speciesId\": \"hatch\", \"templateSpeciesId\": \"chicken\" } ]");
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
            Assert(!row.Capabilities.Contains("CustomAnimals", StringComparer.OrdinalIgnoreCase), "Core does not interpret domain files or infer an Animals provider from filenames.");
            Assert(row.Capabilities.Contains("OfficialJson", StringComparer.OrdinalIgnoreCase), "Official JSON capability should be inferred without applying content.");
            Assert(row.Capabilities.Contains("OfficialLocal", StringComparer.OrdinalIgnoreCase), "Official local source capability should be retained.");
            Assert(snapshot.DiffCount == 0, "Registry should not diff when all legacy loaded content packs are indexed.");
            Assert(!snapshot.FormatSummary().Contains("customAnimalPacks="), "Core content summary must not maintain a second domain contribution ledger.");
            Assert(!row.Capabilities.Contains("AudioReplacement", StringComparer.OrdinalIgnoreCase) && !row.Capabilities.Contains("AnimalVoice", StringComparer.OrdinalIgnoreCase), "A retired audio sidecar must not create Core audio capabilities.");
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
            string modDir = CreateEnabledFixtureModDirectory(dir, "Broken");
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
                string modDir = CreateEnabledFixtureModDirectory(dir, "OptionalAlias");
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
                Assert(DtmApiRuntime.ApiVersion == "0.7.5", "DTMAPI runtime API version should project the 0.7.5 local candidate authority.");
                Assert(DtmApiRuntime.BinaryVersion == "0.7.5.0", "DTMAPI binary/plugin file version should remain numeric and project the 0.7.5 candidate authority.");
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
                Assert(snapshot.Errors.Any(e => e.Owner == "DTMAPI.Tests.EscapingDll" && e.Details.Contains("dependency-path-outside-package")), "Escaping EntryDll should have a path-safety diagnostic from the common path reader.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void EntryPathUsesActualAssemblyRegardlessOfSuffix()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                string package = WriteAtomicProbePackage(dir, "DTMAPI.Tests.NotDll", "");
                string manifestPath = Path.Combine(package, "manifest.json");
                string fileName = Path.GetFileName(typeof(AtomicOwnerProbeMod).Assembly.Location);
                File.Move(Path.Combine(package, fileName), Path.Combine(package, "Entry.bin"));
                File.WriteAllText(manifestPath, File.ReadAllText(manifestPath).Replace(fileName, "Entry.bin"));

                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                runtime.Start();
                RuntimeSnapshot snapshot = runtime.CreateSnapshot();
                Assert(snapshot.LoadedMods.Any(m => m.Manifest.UniqueID == "DTMAPI.Tests.NotDll"), "An explicitly selected valid assembly loads by path; its filename suffix is not a runtime qualification.");
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

                string ambiguousDir = CreateEnabledFixtureModDirectory(dir, "AmbiguousEntry");
                Directory.CreateDirectory(ambiguousDir);
                File.Copy(assemblyPath, Path.Combine(ambiguousDir, assemblyName), overwrite: true);
                File.WriteAllText(
                    Path.Combine(ambiguousDir, "manifest.json"),
                    "{ \"Name\": \"Ambiguous Entry\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.AmbiguousEntry\", \"EntryDll\": \"" + assemblyName + "\", \"Type\": \"CodeMod\", \"CodeModKind\": \"Strict\" }");

                string selectedDir = CreateEnabledFixtureModDirectory(dir, "SelectedEntry");
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
            RegistryLateFactoryResultsAreDiscarded();
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                string modDir = CreateEnabledFixtureModDirectory(dir, "ApiOwner");
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

        private static void ContentQueryUsesLoadedOwnersAndExplicitCleanup()
        {
            foreach (bool queryDuringColdEntry in new[] { false, true })
            foreach (bool queryBeforeCleanup in new[] { false, true })
            {
                string? previousRoot = UseTempPersistentRoot();
                DtmApiRuntime? runtime = null;
                try
                {
                    string gameDir = NewTempGameDir();
                    const string firstOwner = "DTMAPI.Tests.ContentEntry.First";
                    const string secondOwner = "DTMAPI.Tests.ContentEntry.Second";
                    string firstRoot = WriteAtomicProbePackage(gameDir, firstOwner, string.Empty);
                    string textPath = Path.Combine(firstRoot, "entry-query.txt");
                    File.WriteAllText(textPath, "at startup");
                    runtime = new DtmApiRuntime(new FakeHost(gameDir), new ConfigMenuRegistry());
                    AtomicOwnerProbeMod.EntryCount = 0;
                    bool coldCheckpointRead = false;
                    AtomicOwnerProbeMod.EntryObserved = helper =>
                    {
                        if (!queryDuringColdEntry)
                            return;
                        coldCheckpointRead = true;
                        Assert(runtime.Content.FindAssets("txt").Count == 0,
                            "Cold Entry must see the empty publication until complete active-owner inputs exist.");
                    };
                    runtime.Start();
                    Assert(coldCheckpointRead == queryDuringColdEntry,
                        "Cold Entry demand is fulfilled only at the completed LoadMods boundary.");
                    Assert(runtime.Content.CountOwnerResources(firstOwner) == 0,
                        "Completing startup publishes the owner inputs without enumerating their package assets.");

                    File.WriteAllText(textPath, "current file at first demand");
                    File.WriteAllText(Path.Combine(firstRoot, "added-after-startup.txt"), "visible on first demand");
                    if (queryBeforeCleanup)
                    {
                        IContentAssetInfo[] firstAssets = runtime.Content.FindAssets("txt").ToArray();
                        Assert(firstAssets.Length == 2 && firstAssets.All(asset => asset.SourceModId == firstOwner) &&
                            firstAssets.Any(asset => asset.RelativePath == "added-after-startup.txt"),
                            "The first post-startup query lazily indexes current files from the completed startup owner set, including after an empty cold Entry query.");
                        Assert(runtime.Content.TryReadTextAsset("entry-query.txt", out string firstText) && firstText == "current file at first demand",
                            "The first text asset query reads the current file bytes.");
                        File.WriteAllText(textPath, "current bytes after publication");
                        File.WriteAllText(Path.Combine(firstRoot, "added-after-publication.txt"), "requires a new process");
                    }
                    string secondRoot = WriteAtomicProbePackage(gameDir, secondOwner, string.Empty);
                    File.WriteAllText(Path.Combine(secondRoot, "second-entry.txt"), "not active until restart");
                    bool hotEntryObserved = false;
                    AtomicOwnerProbeMod.EntryObserved = _ => hotEntryObserved = true;
                    IUnitProbeApi? firstApi = runtime.ModRegistry.GetApi<IUnitProbeApi>(firstOwner);
                    runtime.NotifyWorkshopModListChanged();
                    Assert(hotEntryObserved && AtomicOwnerProbeMod.EntryCount == 2 &&
                        runtime.DiscoveredMods.Any(mod => mod.Manifest.UniqueID == secondOwner) && runtime.ModRegistry.IsLoaded(secondOwner) &&
                        ReferenceEquals(runtime.ModRegistry.GetApi<IUnitProbeApi>(firstOwner), firstApi),
                        "A list refresh loads the new owner while preserving the running owner's API.");
                    if (queryBeforeCleanup)
                    {
                        Assert(runtime.Content.FindAssets("txt").Count == 4 &&
                            runtime.Content.FindAssets("txt").Any(asset => asset.SourceModId == secondOwner) && runtime.Content.FindAssets("txt").Any(asset => asset.RelativePath == "added-after-publication.txt"),
                            "Loading a new owner refreshes the package resource list.");
                        Assert(runtime.Content.TryReadTextAsset("entry-query.txt", out string currentText) && currentText == "current bytes after publication",
                            "Published text asset paths still read the current bytes on explicit demand.");
                    }

                    runtime.DeactivateOwner(firstOwner, ModOwnerCleanupReason.EntryFailed, shutdown: false);
                    Assert(runtime.Content.FindAssets("txt").Count == 1 && runtime.Content.FindAssets("txt").All(asset => asset.SourceModId == secondOwner) &&
                        !runtime.Content.TryReadTextAsset("entry-query.txt", out _),
                        "Explicit owner cleanup removes package assets before or after their first query.");
                    runtime.NotifyWorkshopModListChanged();
                    Assert(runtime.Content.FindAssets("txt").Count == 1 && AtomicOwnerProbeMod.EntryCount == 2 &&
                        !runtime.ModRegistry.IsLoaded(firstOwner) && runtime.ModRegistry.IsLoaded(secondOwner),
                        "Subsequent discovery cannot resurrect a cleaned owner and keeps the newly loaded owner active.");
                }
                finally
                {
                    AtomicOwnerProbeMod.EntryObserved = null;
                    try { runtime?.NotifyRuntimeShutdown("ContentQuery startup-owner visibility test"); }
                    finally { RestorePersistentRoot(previousRoot); }
                }
            }
        }

        private static void HelperSharedAndOwnerBoundServicesEnforceThreadAndStaleSemantics()
        {
            ContentQueryUsesLoadedOwnersAndExplicitCleanup();
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

                _ = workshop.GetOfficialMods();
                _ = content.GetKnownContentTypes();
                _ = diagnostics.GetWarnings();
                _ = diagnostics.GetHookStatuses();
                Assert(translation.Get("missing", "fallback") == "fallback", "Shared runtime services are read on the runtime thread.");
                Exception? sharedReadFailure = null;
                var sharedReadThread = new Thread(() =>
                {
                    try
                    {
                        _ = diagnostics.GetErrors();
                        _ = diagnostics.GetLatestLogPath();
                    }
                    catch (Exception ex)
                    {
                        sharedReadFailure = ex;
                    }
                });
                sharedReadThread.Start();
                sharedReadThread.Join();
                Assert(sharedReadFailure == null, "Error logging and its read-only diagnostics remain available across threads.");

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

                runtime.DeactivateOwner(owner.UniqueID, ModOwnerCleanupReason.EntryFailed, shutdown: false);
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

                InvokeFailedOwnerCleanup(runtime, owner.UniqueID);

                runtime.Update();
                Assert(updateCalls == 0, "Failed code-mod cleanup should remove owner-bound event handlers.");
                Assert(runtime.ModRegistry.GetApi<IUnitProbeApi>(owner.UniqueID) == null, "Failed code-mod cleanup should remove owner-bound API registrations.");
                Assert(!GetInput(runtime).GetRegisteredButtons().Contains("F11", StringComparer.OrdinalIgnoreCase), "Failed code-mod cleanup should remove owner-bound input registrations.");
                Assert(menuRuntime.GetPage(owner.UniqueID) == null, "Failed code-mod cleanup should remove the failed mod's config page.");

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
                string first = InvokeOwnerDeactivation(runtime, ownerId, ModOwnerCleanupReason.EntryFailed, shutdown: false);
                Assert(AtomicOwnerProbeMod.DisposeCount == 1 && AtomicOwnerProbeMod.DisposeFailuresRemaining == 0,
                    "The first owner deactivation pass must invoke the optional Mod instance Dispose callback exactly once.");
                Assert(AtomicOwnerProbeMod.LastDeactivationReason == nameof(ModOwnerCleanupReason.EntryFailed),
                    "Reason-aware managed Mod deactivation must receive the exact Unload reason.");
                Assert(AtomicOwnerProbeMod.LastPrepareReason == nameof(ModOwnerCleanupReason.EntryFailed),
                    "Reason-aware managed Mod preparation must receive the exact Unload reason.");
                Assert(first.Contains("modDisposeFailures=1", StringComparison.Ordinal) &&
                    first.Contains("lifecycleInstancesRemoved=0", StringComparison.Ordinal) &&
                    first.Contains("remaining=1", StringComparison.Ordinal),
                    "A Dispose failure must remain visible as one retained lifecycle root instead of being hidden by generic owner cleanup. summary=" + first);
                Assert(!runtime.HasOwnerInstance(ownerId) && runtime.OwnerRequiresRestart(ownerId),
                    "Generic owner roots must still be removed after a Dispose failure while the loaded owner stays restart-required.");
                Assert(laterParticipant.OwnerIds.SequenceEqual(new[] { ownerId }),
                    "A product Dispose failure must not prevent later GameBridge/Advanced Harmony cleanup participants from running.");

                string second = InvokeOwnerDeactivation(runtime, ownerId, ModOwnerCleanupReason.EntryFailed, shutdown: false);
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

                UseTempPersistentRoot();
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
                        ModOwnerCleanupReason.EntryFailed,
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
                        prepareOwner) <
                        rootsBeforePrepareFailure &&
                    prepareParticipant.OwnerIds.Count == 0 &&
                    prepareRuntime.OwnerRequiresRestart(
                        prepareOwner),
                    "A failed preparation retains the instance and incomplete work, closes delivery/input roots and reports restart instead of pretending it restored Active. summary=" +
                    deferred);
                string preparedRetry =
                    InvokeOwnerDeactivation(
                        prepareRuntime,
                        prepareOwner,
                        ModOwnerCleanupReason.EntryFailed,
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

                UseTempPersistentRoot();
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
                var runtime = new DtmApiRuntime(new FakeHost(gameDir), new ConfigMenuRegistry());
                runtime.UpdateNativeWorkshopSubscriptions(
                    true,
                    "unit-test-minimum-runtime-workshop",
                    string.Empty,
                    new[] { new NativeWorkshopSubscription(workshopId, workshopRoot, true, 0, workshopOfficialId) });
                runtime.Start();

                DiscoveredMod discovered = runtime.DiscoveredMods.Single(mod => mod.Manifest.UniqueID == ownerId);
                Assert(discovered.Source == "Workshop" && discovered.OfficialId == workshopOfficialId && PathsEqual(discovered.RootPath, workshopRoot), "The stale-Runtime fixture must be discovered only from the enabled Workshop package.");
                Assert(discovered.Manifest.MinimumDTMApiVersion == "0.8.0" && !Directory.Exists(Path.Combine(persistentRoot, "MODS")), "The Workshop package must retain its future 0.8.0 minimum without an OfficialLocal shadow copy.");
                Assert(!runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == ownerId), "A Workshop CodeMod requiring 0.8.0 must not load on the current candidate Runtime.");
                Assert(AtomicOwnerProbeMod.ConstructorCount == 0 && AtomicOwnerProbeMod.EntryCount == 0, "The too-new Runtime contract must reject the CodeMod before construction and Entry.");
                Assert(!runtime.HasOwnerAssemblyLoadAttempt(ownerId), "MinimumDTMApiVersion rejection must not record an assembly owner.");
                Assert(!runtime.HasOwnerInstance(ownerId), "MinimumDTMApiVersion rejection must not publish a Mod instance.");
                Assert(!runtime.OwnerRequiresRestart(ownerId) && CountCoreOwnerRootsForTest(runtime, ownerId) == 0, "Pre-assembly stale-Runtime rejection must leave zero restart state, owner roots and active transactions.");

                RuntimeSnapshot snapshot = runtime.CreateSnapshot();
                IDtmErrorInfo ownerError = snapshot.Errors.Single(error => error.Owner == ownerId);
                Assert(ownerError.Message.Contains("前置版本过旧", StringComparison.Ordinal) &&
                    ownerError.Details.Contains("MinimumDTMApiVersion=0.8.0", StringComparison.Ordinal) &&
                    ownerError.Details.Contains("installed runtime=" + DtmApiRuntime.ApiVersion, StringComparison.Ordinal) &&
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

        private static void CodeModPreAssemblyFailuresRequireRestartToRetry()
        {
            string? previousRoot = UseTempPersistentRoot();
            DtmApiRuntime? runtime = null;
            try
            {
                string dir = NewTempGameDir();
                const string missingOwnerId = "DTMAPI.Tests.MissingAssemblyRetry";
                const string badImageOwnerId = "DTMAPI.Tests.BadImageRetry";
                string entryType = typeof(AtomicOwnerProbeMod).FullName ?? nameof(AtomicOwnerProbeMod);

                string missingDir = CreateEnabledFixtureModDirectory(dir, "MissingAssemblyRetry");
                Directory.CreateDirectory(missingDir);
                const string missingDllName = "missing-retry.dll";
                File.WriteAllText(
                    Path.Combine(missingDir, "manifest.json"),
                    "{ \"Name\": \"Missing Assembly Retry\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"" + missingOwnerId + "\", \"EntryDll\": \"" + missingDllName + "\", \"EntryType\": \"" + entryType + "\", \"Type\": \"CodeMod\", \"CodeModKind\": \"Strict\" }");

                string badImageDir = CreateEnabledFixtureModDirectory(dir, "BadImageRetry");
                Directory.CreateDirectory(badImageDir);
                const string badImageDllName = "bad-image-retry.dll";
                File.WriteAllText(Path.Combine(badImageDir, badImageDllName), "This is deliberately not a managed assembly.");
                File.WriteAllText(
                    Path.Combine(badImageDir, "manifest.json"),
                    "{ \"Name\": \"Bad Image Retry\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"" + badImageOwnerId + "\", \"EntryDll\": \"" + badImageDllName + "\", \"EntryType\": \"" + entryType + "\", \"Type\": \"CodeMod\", \"CodeModKind\": \"Strict\" }");

                AtomicOwnerProbeMod.EntryCount = 0;
                runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                runtime.Start();

                Assert(!runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == missingOwnerId || mod.Manifest.UniqueID == badImageOwnerId), "Missing and BadImage DLL owners must not publish any loaded state.");
                Assert(!runtime.HasOwnerAssemblyLoadAttempt(missingOwnerId) && !runtime.HasOwnerAssemblyLoadAttempt(badImageOwnerId), "Failures before a successful Assembly.LoadFrom must not create a restart gate.");
                Assert(CountCoreOwnerRootsForTest(runtime, missingOwnerId) == 0 && CountCoreOwnerRootsForTest(runtime, badImageOwnerId) == 0, "Pre-assembly failures must leave zero owner roots and no active transaction.");

                string failedReport = runtime.ExportLogs();
                string failedContext = ReadZipText(failedReport, "DTMAPI-runtime-context.txt");
                Assert(failedContext.Contains("entry-assembly-metadata-invalid", StringComparison.Ordinal) &&
                    runtime.DiscoveredMods.Any(mod => mod.Manifest.UniqueID == badImageOwnerId),
                    "BadImage stays visible and reports metadata preflight failure without an assembly side-effect or restart gate.");

                string validAssembly = typeof(AtomicOwnerProbeMod).Assembly.Location;
                File.Copy(validAssembly, Path.Combine(missingDir, missingDllName), overwrite: true);
                File.Copy(validAssembly, Path.Combine(badImageDir, badImageDllName), overwrite: true);
                runtime.NotifyWorkshopModListChanged();

                Assert(!runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == missingOwnerId || mod.Manifest.UniqueID == badImageOwnerId) && AtomicOwnerProbeMod.EntryCount == 0,
                    "Repairing missing and BadImage DLLs in place must not cause list refresh to enter either owner in the running session.");
                Assert(!runtime.HasOwnerAssemblyLoadAttempt(missingOwnerId) && !runtime.HasOwnerAssemblyLoadAttempt(badImageOwnerId) &&
                    CountCoreOwnerRootsForTest(runtime, missingOwnerId) == 0 && CountCoreOwnerRootsForTest(runtime, badImageOwnerId) == 0,
                    "Discovery of repaired files must not create assembly load attempts or active owner registrations.");

                runtime.NotifyRuntimeShutdown("pre-assembly repair awaits next startup");
                runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                runtime.Start();
                Assert(runtime.LoadedMods.Count(mod => mod.Manifest.UniqueID == missingOwnerId || mod.Manifest.UniqueID == badImageOwnerId) == 2 && AtomicOwnerProbeMod.EntryCount == 2,
                    "A fresh Runtime startup should admit each repaired owner exactly once.");
                IUnitProbeApi? missingApi = runtime.ModRegistry.GetApi<IUnitProbeApi>(missingOwnerId);
                IUnitProbeApi? badImageApi = runtime.ModRegistry.GetApi<IUnitProbeApi>(badImageOwnerId);
                int missingRoots = CountCoreOwnerRootsForTest(runtime, missingOwnerId);
                int badImageRoots = CountCoreOwnerRootsForTest(runtime, badImageOwnerId);

                SetFixtureModEnabled(missingDir, false);
                SetFixtureModEnabled(badImageDir, false);
                runtime.NotifyWorkshopModListChanged();
                Assert(runtime.ModRegistry.IsLoaded(missingOwnerId) && runtime.ModRegistry.IsLoaded(badImageOwnerId) &&
                    ReferenceEquals(runtime.ModRegistry.GetApi<IUnitProbeApi>(missingOwnerId), missingApi) &&
                    ReferenceEquals(runtime.ModRegistry.GetApi<IUnitProbeApi>(badImageOwnerId), badImageApi) &&
                    CountCoreOwnerRootsForTest(runtime, missingOwnerId) == missingRoots && CountCoreOwnerRootsForTest(runtime, badImageOwnerId) == badImageRoots,
                    "Disabling the source for the next startup must preserve both running owners and their actual APIs and registrations.");

                runtime.DeactivateOwner(missingOwnerId, ModOwnerCleanupReason.EntryFailed, shutdown: false);
                runtime.DeactivateOwner(badImageOwnerId, ModOwnerCleanupReason.EntryFailed, shutdown: false);
                Assert(!runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == missingOwnerId || mod.Manifest.UniqueID == badImageOwnerId) &&
                    CountCoreOwnerRootsForTest(runtime, missingOwnerId) == 0 && CountCoreOwnerRootsForTest(runtime, badImageOwnerId) == 0,
                    "Explicit deactivation of the repaired owners must remove their publications and every Core root.");
                Assert(runtime.OwnerRequiresRestart(missingOwnerId) && runtime.OwnerRequiresRestart(badImageOwnerId) && AtomicOwnerProbeMod.EntryCount == 2,
                    "Explicit deactivation after assembly load remains restart-required and must not repeat Entry.");
            }
            finally
            {
                try { runtime?.NotifyRuntimeShutdown("pre-assembly repair restart test"); }
                finally { RestorePersistentRoot(previousRoot); }
            }
        }

        private static void FileMonitorSinkFailuresDoNotBlockExplicitOwnerCleanup()
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
                WriteAtomicProbePackage(dir, provider, string.Empty);
                WriteAtomicProbePackage(dir, required, ", \"Dependencies\": [{ \"UniqueID\": \"" + provider + "\", \"MinimumVersion\": \"1.0.0\", \"Required\": true }]");
                PrepareFileSinkFailure(dir);
                var runtime = new DtmApiRuntime(new ThrowingLogHost(dir), new ConfigMenuRegistry(), new BindingPatchCleanup(System.Threading.Thread.CurrentThread.ManagedThreadId));
                AtomicOwnerProbeMod.EntryCount = 0;
                runtime.Start();
                Assert(runtime.LoadedMods.Count(mod => mod.Manifest.UniqueID == provider || mod.Manifest.UniqueID == required) == 2 && AtomicOwnerProbeMod.EntryCount == 2, "File and host log sink failures must not block initial ordinary Mod activation.");

                runtime.DeactivateOwner(provider, ModOwnerCleanupReason.EntryFailed, shutdown: false);

                Assert(!runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == provider || mod.Manifest.UniqueID == required), "Throwing file/host log sinks must not block explicit owner cleanup or its required dependency cascade.");
                Assert(runtime.OwnerRequiresRestart(provider) && runtime.OwnerRequiresRestart(required), "Loaded DLL owners deactivated under throwing log sinks should still become restart-required.");
                Assert(CountCoreOwnerRootsForTest(runtime, provider) == 0 && CountCoreOwnerRootsForTest(runtime, required) == 0, "Throwing log sinks must not prevent authoritative owner cleanup from reaching zero roots.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void RuntimeLoggingRetainsFullErrorEvidence()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());

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
                    log.Contains(nameof(RuntimeLoggingRetainsFullErrorEvidence), StringComparison.Ordinal),
                    "Runtime logging must not remove Error context, full inner exception text or the captured stack.");

                runtime.Diagnostics.RecordError("DTMAPI.Tests.Lightweight", context, captured.ToString());
                IDtmErrorInfo retained = runtime.Diagnostics.GetErrors().Single(error => error.Owner == "DTMAPI.Tests.Lightweight");
                Assert(retained.Message.Contains("operation=SaveLoaded", StringComparison.Ordinal) &&
                    retained.Details.Contains("outer-runtime-failure", StringComparison.Ordinal) &&
                    retained.Details.Contains("inner-causal-failure", StringComparison.Ordinal) &&
                    retained.Details.Contains(nameof(RuntimeLoggingRetainsFullErrorEvidence), StringComparison.Ordinal),
                    "Retained player diagnostics must discover an Error after diagnostic bookkeeping is removed.");
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
                string modDir = CreateEnabledFixtureModDirectory(dir, "ThrowAfterRegister");
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
                Assert(!GetContent(runtime).FindAssets("json").Any(asset => asset.SourceModId.Equals("DTMAPI.Tests.ThrowAfterRegister", StringComparison.OrdinalIgnoreCase)), "A throwing Entry must not publish its owner content.");

                IDtmModStatusInfo failure = runtime.CreateDiagnosticsSnapshot().Mods.Single(mod => mod.UniqueID == "DTMAPI.Tests.ThrowAfterRegister");
                Assert(failure.Status == "error" && failure.StatusCode == "code-load-error" && runtime.OwnerRequiresRestart(failure.UniqueID),
                    "Failed initialization reports failure and cannot enter again in this process.");
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
                string modDir = CreateEnabledFixtureModDirectory(dir, "MonitorError");
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
                Assert(runtime.Input.CountOwnerResources(owner.UniqueID) == 4, "The SaveLoaded fixture should contain one persistent registration and three transient owner-local snapshot watches.");
                Assert(runtime.Input.IsDown("F13") && runtime.Input.IsDown("F14") && runtime.Input.WasPressed("F13") && runtime.Input.WasPressed("F14") && runtime.Input.WasReleased("F15"), "The SaveLoaded fixture should contain transient down, pressed, and released state.");

                runtime.NotifySaveLoaded(isNewGame: false);

                Assert(runtime.Input.CountOwnerResources(owner.UniqueID) == 1, "SaveLoaded must remove owner-local snapshot watches while retaining exactly the persistent registration root.");
                Assert(!ownerInput.IsDown("F13") && !ownerInput.IsDown("F14") && !ownerInput.WasPressed("F13") && !ownerInput.WasPressed("F14") && !ownerInput.WasReleased("F15"), "SaveLoaded must hide held/edge state behind neutral rearm without erasing physical truth.");
                Assert(runtime.Input.GetRegisteredButtons().Contains("F13", StringComparer.OrdinalIgnoreCase), "SaveLoaded must preserve persistent input registrations.");
                Assert(runtime.ModRegistry.GetApi<IUnitProbeApi>(owner.UniqueID) != null &&
                    ((IConfigMenuRuntime)menu).GetPage(owner.UniqueID) != null &&
                    runtime.Config.CountOwner(owner.UniqueID) == 1 &&
                    runtime.Events.CountOwnerResources(owner.UniqueID) > 0,
                    "SaveLoaded must preserve process-lifetime Event, API, ConfigPage, and migration services.");

                runtime.RecordInputFrame(new[]
                {
                    new InputButtonSample("F14", true, true, false),
                    new InputButtonSample("F15", false, false, true)
                });
                ownerInput.IsDown(DtmButton.Parse("F14"));
                ownerInput.WasReleased(DtmButton.Parse("F15"));
                ownerInput.WasPressed(DtmButton.Parse("F16"));
                Assert(runtime.Input.CountOwnerResources(owner.UniqueID) == 4, "Transient input watches should be able to rebuild after SaveLoaded without duplicating the persistent registration.");
                runtime.NotifyReturnedToTitle();

                Assert(runtime.ModRegistry.GetApi<IUnitProbeApi>(owner.UniqueID) != null, "Save/title boundaries must preserve process-lifetime APIs.");
                Assert(((IConfigMenuRuntime)menu).GetPage(owner.UniqueID) != null, "Save/title boundaries must preserve config pages.");
                Assert(runtime.Config.CountOwner(owner.UniqueID) == 1, "Save/title boundaries must preserve config migrations.");
                Assert(runtime.Input.GetRegisteredButtons().Contains("F13", StringComparer.OrdinalIgnoreCase), "Save/title boundaries must preserve persistent input registrations.");
                Assert(runtime.Events.CountOwnerResources(owner.UniqueID) > 0, "Save/title boundaries must preserve event registrations.");
                Assert(runtime.Input.CountOwnerResources(owner.UniqueID) == 1, "ReturnedToTitle must clear rebuilt local snapshot watches while retaining the persistent registration.");
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

        private static DtmApiRuntime StartSourceAuthorityRuntime(
            string gameDir,
            ulong workshopId,
            string workshopRoot,
            bool nativeSnapshotAvailable,
            bool nativeEnabled = true,
            int nativePriority = 20)
        {
            var runtime = new DtmApiRuntime(
                new FakeHost(gameDir),
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
            string modDir = CreateEnabledFixtureModDirectory(gameDir, folderName);
            Directory.CreateDirectory(modDir);
            File.WriteAllText(Path.Combine(modDir, "manifest.json"), json);
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

        private sealed class ThrowingLogHost : IRuntimeHost
        {
            public ThrowingLogHost(string gamePath)
            {
                GamePath = gamePath;
                PluginPath = Path.Combine(gamePath, "BepInEx", "plugins");
            }

            public string GamePath { get; }
            public string PluginPath { get; }
            public string HostName => "ThrowingLogHost";
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

        private static void RegistryLateFactoryResultsAreDiscarded()
        {
            foreach (string operation in new[] { "provider-close", "consumer-close", "provider-replace" })
            {
                var registry = new ModRegistryService();
                var provider = new ManifestModel { UniqueID = "DTMAPI.Tests.LateProvider" };
                var consumer = new ManifestModel { UniqueID = "DTMAPI.Tests.LateConsumer" };
                bool active = true;
                var factory = new LateFactoryProbe(() =>
                {
                    if (operation == "consumer-close")
                    {
                        active = false;
                        registry.RemoveOwner(consumer.UniqueID);
                    }
                    else
                    {
                        registry.RemoveOwner(provider.UniqueID);
                        if (operation == "provider-replace")
                            registry.RegisterApiForOwner<IUnitProbeApi>(provider, new DualProbeApi("replacement"));
                    }
                });
                registry.RegisterApiForOwner<IUnitProbeApi>(provider, factory);
                IModRegistry owned = registry.CreateOwnerBoundRegistry(consumer, () =>
                {
                    if (!active) throw new InvalidOperationException("consumer inactive");
                });
                bool rejected = false;
                try { owned.GetApi<IUnitProbeApi>(provider.UniqueID); }
                catch (InvalidOperationException ex) { rejected = ex.Message.Contains("inactive", StringComparison.Ordinal); }
                Assert(rejected && factory.Facade.Deactivated && registry.CountOwner(consumer.UniqueID) == 0,
                    "A factory result after " + operation + " must be deactivated and never retained as a consumer facade.");
                if (operation == "provider-replace")
                    Assert(registry.GetApi<IUnitProbeApi>(provider.UniqueID)?.Owner == "replacement",
                        "Discarding an old factory result must not remove the replacement provider API.");
                registry.RemoveOwner(provider.UniqueID);
                Assert(registry.TotalRootCount == 0, "Late-factory rejection must leave no hidden API or facade roots.");
            }
        }

        private sealed class LateFactoryProbe : IUnitProbeApi, IOwnerBoundApiFactory
        {
            private readonly Action duringCreate;
            internal LateFactoryProbe(Action duringCreate) { this.duringCreate = duringCreate; }
            public string Owner => "factory";
            internal LateFacadeProbe Facade { get; } = new LateFacadeProbe();
            public object CreateOwnerBoundApi(Type apiType, IManifest consumer, Action ensureOwnerActive)
            {
                duringCreate();
                return Facade;
            }
        }

        private sealed class LateFacadeProbe : IUnitProbeApi, IOwnerBoundApiFacade
        {
            public string Owner => "facade";
            internal bool Deactivated { get; private set; }
            public void Deactivate() { Deactivated = true; }
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
