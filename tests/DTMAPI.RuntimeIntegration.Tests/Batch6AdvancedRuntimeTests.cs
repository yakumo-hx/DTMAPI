using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using DTMAPI.Abstractions;
using DTMAPI.BepInExBootstrap;
using DTMAPI.Core.Diagnostics;
using DTMAPI.Core.Manager;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;
using DTMAPI.Testing;

namespace DTMAPI.UnitTests
{
    internal static class Batch6AdvancedRuntimeTests
    {
        public static void RunTargetCompatibility() => AdvancedSdk01PackagesLoadAcrossCompatibilityContextsAndIsolateFailure();

        public static void RunAll()
        {
            StrictNativeAssemblyReferenceFailsClosedBeforeLoad();
            StrictTransitiveNativeAssemblyReferenceFailsClosedBeforeLoad();
            LegacyNativeCompatibilityAllowsOmittedKindWithoutOpeningStrict();
            LegacyNativeCompatibilityColdLoadsAndIsolatesEntryFailure();
            LegacyNativeColdStartSourceAndAssemblyIdentityMatrix();
            ManifestWireContradictionsFailClosed();
            AdvancedReceiptReferenceRowsRequireExactFields();
            AdvancedPolicyRegistryRequiresExactUniqueIdBindings();
            AdvancedInstalledGameContextAllowsExactDriftAndUnknown();
            AdvancedSdk01PackagesLoadAcrossCompatibilityContextsAndIsolateFailure();
            RenamedManagedNativePayloadFailsClosed();
            AdvancedWrongTargetFrameworkFailsClosed();
            HarmonySupervisorRejectsWrongOwnerDuplicateAndLateDrift();
            AdvancedLoadWindowAndEntryIdentityFailClosed();
            AdvancedSameVersionSourceDriftChangesIdentity();
            ManagerProjectsAdvancedClassificationWithoutPublicAbiExpansion();
            ManagerProjectsLegacyAuthorOwnershipAndRestartBoundary();
        }

        private static void StrictNativeAssemblyReferenceFailsClosedBeforeLoad()
        {
            string root = Path.Combine(DtmApiTestSession.Current.RootPath, "batch6-strict-native-" + Guid.NewGuid().ToString("N"));
            string modRoot = Path.Combine(root, "Mods", "StrictNativeBypass");
            Directory.CreateDirectory(modRoot);
            string entrySource = typeof(BootstrapPlugin).Assembly.Location;
            string entryName = Path.GetFileName(entrySource);
            File.Copy(entrySource, Path.Combine(modRoot, entryName));
            File.WriteAllText(
                Path.Combine(modRoot, "manifest.json"),
                "{ \"Name\": \"Strict Native Bypass\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.StrictNativeBypass\", \"Type\": \"CodeMod\", \"CodeModKind\": \"Strict\", \"EntryDll\": \"" + entryName + "\", \"EntryType\": \"Synthetic.Entry\" }",
                new UTF8Encoding(false));

            string? previousPersistentRoot = Environment.GetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT");
            Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", Path.Combine(root, "Persistent"));
            try
            {
                var checkpoints = new List<ModLoadCheckpoint>();
                var runtime = new DtmApiRuntime(new Batch6Host(root));
                runtime.ConfigureModLoadCheckpointForTests(checkpoints.Add);
                runtime.Start();
                Assert(!runtime.DiscoveredMods.Any(mod => mod.Manifest.UniqueID == "DTMAPI.Tests.StrictNativeBypass"), "Strict native-reference bypass must fail during classification.");
                Assert(checkpoints.Count == 0, "Strict native-reference bypass must not reach Assembly.LoadFrom checkpoints.");
                Assert(runtime.CreateDiagnosticsSnapshot().Errors.Any(error =>
                    error.Owner == "DTMAPI.ModScanner" && error.Details.Contains("strict-native-reference-forbidden", StringComparison.Ordinal)),
                    "Strict native-reference bypass must expose the stable Core failure code.");
            }
            finally
            {
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", previousPersistentRoot);
            }
        }

        private static void StrictTransitiveNativeAssemblyReferenceFailsClosedBeforeLoad()
        {
            string root = Path.Combine(DtmApiTestSession.Current.RootPath, "batch6-strict-transitive-native-" + Guid.NewGuid().ToString("N"));
            string modRoot = Path.Combine(root, "Mods", "StrictTransitiveBypass");
            Directory.CreateDirectory(modRoot);
            string entrySource = typeof(DtmMod).Assembly.Location;
            string entryName = "Entry.dll";
            File.Copy(entrySource, Path.Combine(modRoot, entryName));
            File.Copy(typeof(BootstrapPlugin).Assembly.Location, Path.Combine(modRoot, "Helper.dll"));
            File.WriteAllText(
                Path.Combine(modRoot, "manifest.json"),
                "{ \"Name\": \"Strict Transitive Native Bypass\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.StrictTransitiveBypass\", \"Type\": \"CodeMod\", \"CodeModKind\": \"Strict\", \"EntryDll\": \"" + entryName + "\", \"EntryType\": \"Synthetic.Entry\" }",
                new UTF8Encoding(false));

            string? previousPersistentRoot = Environment.GetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT");
            Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", Path.Combine(root, "Persistent"));
            try
            {
                var checkpoints = new List<ModLoadCheckpoint>();
                var runtime = new DtmApiRuntime(new Batch6Host(root));
                runtime.ConfigureModLoadCheckpointForTests(checkpoints.Add);
                runtime.Start();
                Assert(!runtime.DiscoveredMods.Any(mod => mod.Manifest.UniqueID == "DTMAPI.Tests.StrictTransitiveBypass"), "Strict helper-chain native-reference bypass must fail during classification.");
                Assert(checkpoints.Count == 0, "Strict helper-chain native-reference bypass must not reach Assembly.LoadFrom checkpoints.");
                Assert(runtime.CreateDiagnosticsSnapshot().Errors.Any(error =>
                    error.Owner == "DTMAPI.ModScanner" && error.Details.Contains("strict-native-reference-forbidden", StringComparison.Ordinal)),
                    "Strict helper-chain bypass must expose the same stable pre-load failure code.");
            }
            finally
            {
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", previousPersistentRoot);
            }
        }

        private static void LegacyNativeCompatibilityAllowsOmittedKindWithoutOpeningStrict()
        {
            const ulong workshopId = 3749000099UL;
            const string uniqueId = "DTMAPI.Tests.LegacyExternal";
            string root = Path.Combine(DtmApiTestSession.Current.RootPath, "batch6-legacy-external-" + Guid.NewGuid().ToString("N"));
            string modRoot = Path.Combine(root, workshopId.ToString());
            Directory.CreateDirectory(modRoot);
            string entryPath = Path.Combine(modRoot, "Entry.dll");
            string fixtureOutput = GetFixtureOutput("LegacyNativeCodeModFixture");
            File.Copy(Path.Combine(fixtureOutput, "DTMAPI.LegacyNativeCodeModFixture.dll"), entryPath);
            string manifestPath = Path.Combine(modRoot, "manifest.json");
            File.WriteAllText(
                manifestPath,
                "{\"Name\":\"Legacy external\",\"Author\":\"Tests\",\"Version\":\"1.0.0\",\"UniqueID\":\"" + uniqueId +
                "\",\"Type\":\"CodeMod\",\"EntryDll\":\"Entry.dll\",\"EntryType\":\"DTMAPI.LegacyNativeCodeModFixture.LegacyNativeProbeMod\"}",
                new UTF8Encoding(false));
            string entrySha256 = HashFile(entryPath);
            var authority = LegacyExternalCompatibilityAuthority.FromRows(new[]
            {
                new LegacyExternalCompatibilityCatalogRow(
                    workshopId.ToString(),
                    uniqueId,
                    "ActualLoadLanePending",
                    "Entry.dll",
                    entrySha256)
            });
            var classifier = new ManagedModClassifier(root, authority);
            var explicitTypeManifest = new ManifestModel
            {
                Name = "Legacy external",
                Author = "Tests",
                Version = "1.0.0",
                UniqueID = uniqueId,
                Type = "CodeMod",
                EntryDll = "Entry.dll",
                EntryType = "Synthetic.Entry"
            };

            ManagedModClassification admitted = classifier.Classify(
                explicitTypeManifest,
                modRoot,
                manifestPath,
                "Workshop",
                nativeWorkshopSourceVerified: true,
                workshopId: workshopId);
            Assert(
                admitted.Identity == ManagedModIdentity.LegacyNativeCodeMod &&
                admitted.IsLegacyNativeCompatibility &&
                admitted.IsLegacyExternalCompatibility &&
                admitted.EntryDllSha256.Equals(entrySha256, StringComparison.OrdinalIgnoreCase) &&
                admitted.EntryModuleMvid.Length > 0,
                "An exact retained Workshop input may expose stronger provenance while remaining in the author-managed legacy native compatibility lane.");

            var omittedTypeManifest = new ManifestModel
            {
                Name = "Legacy external omitted type",
                Author = "Tests",
                Version = "1.0.0",
                UniqueID = uniqueId,
                EntryDll = "Entry.dll",
                EntryType = "Synthetic.Entry"
            };
            Assert(
                classifier.Classify(
                    omittedTypeManifest,
                    modRoot,
                    manifestPath,
                    "Workshop",
                    nativeWorkshopSourceVerified: true,
                    workshopId: workshopId).IsLegacyExternalCompatibility,
                "A pre-Type legacy manifest must use the same exact compatibility identity rather than being inferred as Advanced.");

            ManagedModClassification local = classifier.Classify(explicitTypeManifest, modRoot, manifestPath, "Local", true, workshopId);
            ManagedModClassification rawWorkshop = classifier.Classify(explicitTypeManifest, modRoot, manifestPath, "Workshop", false, workshopId);
            ManagedModClassification otherWorkshop = classifier.Classify(explicitTypeManifest, modRoot, manifestPath, "Workshop", true, workshopId + 1);
            Assert(
                new[] { local, rawWorkshop, otherWorkshop }.All(value =>
                    value.IsLegacyNativeCompatibility &&
                    !value.IsLegacyExternalCompatibility &&
                    value.EntryDllSha256.Equals(entrySha256, StringComparison.OrdinalIgnoreCase) &&
                    value.EntryModuleMvid.Length > 0 &&
                    value.SourceFingerprint.Length == 64),
                "Catalog mismatch or an unverified source must lose only the stronger exact provenance while retaining classified code-closure identity for the general cold-load lane.");

            var wrongUniqueId = new ManifestModel
            {
                Name = "Wrong identity",
                Author = "Tests",
                Version = "1.0.0",
                UniqueID = uniqueId + ".Changed",
                Type = "CodeMod",
                EntryDll = "Entry.dll"
            };
            ManagedModClassification changedIdentity = classifier.Classify(
                wrongUniqueId,
                modRoot,
                manifestPath,
                "Workshop",
                true,
                workshopId);
            Assert(changedIdentity.IsLegacyNativeCompatibility && !changedIdentity.IsLegacyExternalCompatibility,
                "A changed legacy identity must not inherit exact Catalog provenance, but it remains an author-managed omitted-kind input.");

            var wrongPath = new ManifestModel
            {
                Name = "Wrong path",
                Author = "Tests",
                Version = "1.0.0",
                UniqueID = uniqueId,
                Type = "CodeMod",
                EntryDll = "Other.dll"
            };
            ManagedModClassification changedPath = classifier.Classify(
                wrongPath,
                modRoot,
                manifestPath,
                "Workshop",
                true,
                workshopId);
            Assert(
                changedPath.IsLegacyNativeCompatibility &&
                !changedPath.IsLegacyExternalCompatibility &&
                changedPath.EntryDllSha256.Length == 0,
                "A missing changed EntryDll must lose exact Catalog provenance while preserving the ordinary owner-specific loader path diagnostic.");

            var explicitStrict = new ManifestModel
            {
                Name = "Explicit Strict",
                Author = "Tests",
                Version = "1.0.0",
                UniqueID = uniqueId,
                Type = "CodeMod",
                CodeModKind = "Strict",
                EntryDll = "Entry.dll"
            };
            AssertClassificationFailure(
                "strict-native-reference-forbidden",
                () => classifier.Classify(explicitStrict, modRoot, manifestPath, "Workshop", true, workshopId),
                "Explicit Strict must not use the omitted-kind legacy compatibility route.");

            var wrongHashAuthority = LegacyExternalCompatibilityAuthority.FromRows(new[]
            {
                new LegacyExternalCompatibilityCatalogRow(
                    workshopId.ToString(),
                    uniqueId,
                    "ActualLoadLanePending",
                    "Entry.dll",
                    new string('A', 64))
            });
            ManagedModClassification changedHash = new ManagedModClassifier(root, wrongHashAuthority).Classify(
                explicitTypeManifest,
                modRoot,
                manifestPath,
                "Workshop",
                true,
                workshopId);
            Assert(changedHash.IsLegacyNativeCompatibility && !changedHash.IsLegacyExternalCompatibility,
                "A changed legacy entry hash must downgrade to unverified author-managed provenance instead of disabling an otherwise loadable historical Mod.");

            File.Copy(
                Path.Combine(fixtureOutput, "DTMAPI.LegacyNativeHelperFixture.dll"),
                Path.Combine(modRoot, "PrivateHelper.dll"));
            ManagedModClassification withHelper = classifier.Classify(
                explicitTypeManifest,
                modRoot,
                manifestPath,
                "Workshop",
                true,
                workshopId);
            Assert(withHelper.IsLegacyExternalCompatibility,
                "An auxiliary DLL must not invalidate the exact entry-byte provenance or block the separate author-managed dependency lane.");

            File.Copy(Path.Combine(fixtureOutput, "0Harmony.dll"), Path.Combine(modRoot, "CopiedPlatform.dll"));
            AssertClassificationFailure(
                "bundled-native-runtime-dependency",
                () => classifier.Classify(explicitTypeManifest, modRoot, manifestPath, "Workshop", true, workshopId),
                "Legacy compatibility permits direct Harmony references and private helpers, but must not package a copied Harmony/game/DTMAPI platform assembly.");
        }

        private static void LegacyNativeCompatibilityColdLoadsAndIsolatesEntryFailure()
        {
            const string validOwner = "DTMAPI.Tests.LegacyNative.Valid";
            const string failingOwner = "DTMAPI.Tests.LegacyNative.Failing";
            string root = Path.Combine(DtmApiTestSession.Current.RootPath, "batch6-legacy-native-runtime-" + Guid.NewGuid().ToString("N"));
            string fixtureOutput = GetFixtureOutput("LegacyNativeCodeModFixture");
            string throwingFixtureOutput = GetFixtureOutput("LegacyNativeThrowingCodeModFixture");
            string entrySource = Path.Combine(fixtureOutput, "DTMAPI.LegacyNativeCodeModFixture.dll");
            string failingEntrySource = Path.Combine(throwingFixtureOutput, "DTMAPI.LegacyNativeThrowingCodeModFixture.dll");
            string helperSource = Path.Combine(fixtureOutput, "DTMAPI.LegacyNativeHelperFixture.dll");
            string harmonySource = Path.Combine(fixtureOutput, "0Harmony.dll");
            string gameSource = Path.Combine(fixtureOutput, "Assembly-CSharp.dll");
            Assert(
                new[] { entrySource, failingEntrySource, helperSource, harmonySource, gameSource }.All(File.Exists),
                "Legacy native runtime fixtures and their unbundled platform-reference stubs must be built before the focused load test.");
            string[] entryReferences = PortableAssemblyReferenceInspector.Inspect(entrySource).AssemblyReferences.ToArray();
            Assert(
                new[] { "BepInEx", "0Harmony", "Assembly-CSharp", "DTMAPI.LegacyNativeHelperFixture" }
                    .All(expected => entryReferences.Contains(expected, StringComparer.OrdinalIgnoreCase)),
                "The executable legacy fixture must retain direct BepInEx, Harmony, game-assembly, and private-helper AssemblyRefs.");
            EnsureAssemblyLoaded(harmonySource, "0Harmony");
            EnsureAssemblyLoaded(gameSource, "Assembly-CSharp");

            WriteLegacyNativePackage(
                root,
                "Valid",
                validOwner,
                "DTMAPI.LegacyNativeCodeModFixture.LegacyNativeProbeMod",
                entrySource,
                helperSource);
            WriteLegacyNativePackage(
                root,
                "Failing",
                failingOwner,
                "DTMAPI.LegacyNativeThrowingCodeModFixture.ThrowingLegacyNativeProbeMod",
                failingEntrySource,
                helperSource);

            string? previousPersistentRoot = Environment.GetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT");
            Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", Path.Combine(root, "Persistent"));
            try
            {
                var runtime = new DtmApiRuntime(new Batch6Host(root));
                runtime.Start();

                DiscoveredMod loaded = runtime.LoadedMods.Single(mod => mod.Manifest.UniqueID == validOwner);
                Assert(
                    loaded.Classification.Identity == ManagedModIdentity.LegacyNativeCodeMod &&
                    loaded.Classification.IsLegacyNativeCompatibility &&
                    loaded.Classification.NativeRisk.Contains("author-managed", StringComparison.Ordinal) &&
                    loaded.Classification.RestartPolicy.Contains("no-managed-hook-cleanup", StringComparison.Ordinal),
                    "The omitted-kind direct-BepInEx fixture must cold-load with an explicit author-managed/restart-required classification.");
                Assert(!runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == failingOwner) &&
                    runtime.OwnerRequiresRestart(failingOwner),
                    "A throwing legacy Entry must be isolated to its owner and retain the conservative restart boundary.");
                Assert(runtime.CreateDiagnosticsSnapshot().Errors.Any(error =>
                        error.Owner == failingOwner &&
                        error.Details.Contains("legacy-native-entry-probe", StringComparison.Ordinal)),
                    "Legacy Entry failure diagnostics must identify the exact third-party owner while allowing the valid sibling to remain loaded.");

                string validDisabledMarker = Path.Combine(root, "Mods", "Valid", "dtmapi.disabled");
                File.WriteAllText(validDisabledMarker, string.Empty, new UTF8Encoding(false));
                runtime.NotifyWorkshopModListChanged();
                Assert(!runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == validOwner) &&
                    runtime.OwnerRequiresRestart(validOwner),
                    "Disabling a loaded legacy native compatibility source must expose restart-required instead of claiming hot unload.");
                File.Delete(validDisabledMarker);
                runtime.NotifyWorkshopModListChanged();
                Assert(
                    !runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == validOwner) &&
                    runtime.OwnerRequiresRestart(validOwner) &&
                    ReadStaticInt(
                        "DTMAPI.LegacyNativeCodeModFixture",
                        "DTMAPI.LegacyNativeCodeModFixture.LegacyNativeProbeMod",
                        "EntryCount") == 1,
                    "Same-process re-enable must not execute legacy Entry again; a clean process restart remains required.");
            }
            finally
            {
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", previousPersistentRoot);
            }
        }

        private static void LegacyNativeColdStartSourceAndAssemblyIdentityMatrix()
        {
            const string winnerOwner = "DTMAPI.Tests.LegacyCollision.A";
            const string loserOwner = "DTMAPI.Tests.LegacyCollision.B";
            string root = Path.Combine(DtmApiTestSession.Current.RootPath, "batch6-legacy-native-cold-source-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(root, "Mods"));
            string helperSource = Path.Combine(GetFixtureOutput("LegacyNativeCodeModFixture"), "DTMAPI.LegacyNativeHelperFixture.dll");
            string winnerSource = Path.Combine(GetFixtureOutput("LegacyCollisionA"), "DTMAPI.LegacyCollisionFixture.dll");
            string loserSource = Path.Combine(GetFixtureOutput("LegacyCollisionB"), "DTMAPI.LegacyCollisionFixture.dll");
            Assert(
                File.Exists(winnerSource) &&
                File.Exists(loserSource) &&
                PortableAssemblyReferenceInspector.Inspect(winnerSource).ModuleMvid != PortableAssemblyReferenceInspector.Inspect(loserSource).ModuleMvid,
                "Collision fixtures must use one CLR AssemblyName/version with distinct MVIDs.");

            string? previousPersistentRoot = Environment.GetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT");
            Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", Path.Combine(root, "Persistent"));
            try
            {
                var hotRuntime = new DtmApiRuntime(new Batch6Host(root));
                hotRuntime.Start();
                string winnerRoot = WriteLegacyNativePackage(
                    root,
                    "A",
                    winnerOwner,
                    "DTMAPI.LegacyCollisionFixture.CollisionProbeMod",
                    winnerSource,
                    helperSource);
                WriteLegacyNativePackage(
                    root,
                    "B",
                    loserOwner,
                    "DTMAPI.LegacyCollisionFixture.CollisionProbeMod",
                    loserSource,
                    helperSource);
                hotRuntime.NotifyWorkshopModListChanged();
                Assert(
                    !hotRuntime.LoadedMods.Any(mod => mod.Manifest.UniqueID == winnerOwner || mod.Manifest.UniqueID == loserOwner) &&
                    hotRuntime.OwnerRequiresRestart(winnerOwner) &&
                    hotRuntime.OwnerRequiresRestart(loserOwner) &&
                    !AppDomain.CurrentDomain.GetAssemblies().Any(assembly =>
                        assembly.GetName().Name?.Equals("DTMAPI.LegacyCollisionFixture", StringComparison.OrdinalIgnoreCase) == true),
                    "Legacy packages discovered after startup must remain cold-start-only and must not execute Assembly.LoadFrom or Entry.");

                var coldRuntime = new DtmApiRuntime(new Batch6Host(root));
                coldRuntime.Start();
                Assert(
                    coldRuntime.LoadedMods.Any(mod => mod.Manifest.UniqueID == winnerOwner) &&
                    !coldRuntime.LoadedMods.Any(mod => mod.Manifest.UniqueID == loserOwner) &&
                    ReadStaticInt(
                        "DTMAPI.LegacyCollisionFixture",
                        "DTMAPI.LegacyCollisionFixture.CollisionProbeMod",
                        "EntryCount") == 1,
                    "A clean Runtime cold start must load the first package exactly once and reject the same-identity loser.");
                Assert(
                    coldRuntime.CreateDiagnosticsSnapshot().Errors.Any(error =>
                        error.Owner == loserOwner &&
                        error.Details.Contains("legacy-native-entry-assembly-identity-collision", StringComparison.Ordinal)) &&
                    !coldRuntime.CreateDiagnosticsSnapshot().Errors.Any(error =>
                        error.Owner == loserOwner &&
                        error.Details.Contains("legacy-collision-B-executed", StringComparison.Ordinal)),
                    "The loser must fail causally at the resident assembly identity/MVID guard before its Entry can execute.");

                string helperPath = Path.Combine(winnerRoot, "Content", "DTMAPI", Path.GetFileName(helperSource));
                AppendProbeByte(helperPath, 0x41);
                coldRuntime.NotifyWorkshopModListChanged();
                Assert(
                    !coldRuntime.LoadedMods.Any(mod => mod.Manifest.UniqueID == winnerOwner) &&
                    coldRuntime.OwnerRequiresRestart(winnerOwner),
                    "Same-version private-helper drift must deactivate the resident owner and require restart instead of leaving stale code silently active.");

                var entryDriftRuntime = new DtmApiRuntime(new Batch6Host(root));
                entryDriftRuntime.Start();
                Assert(entryDriftRuntime.LoadedMods.Any(mod => mod.Manifest.UniqueID == winnerOwner),
                    "The updated helper closure may load on the next clean Runtime boundary.");
                string replacementEntry = Path.Combine(winnerRoot, "Content", "DTMAPI", "ReplacementEntry.dll");
                File.Copy(loserSource, replacementEntry);
                File.WriteAllText(
                    Path.Combine(winnerRoot, "manifest.json"),
                    "{ \"Name\": \"Legacy Native A\", \"Author\": \"Third Party Fixture\", \"Version\": \"1.0.0\", \"UniqueID\": \"" + winnerOwner +
                    "\", \"Type\": \"CodeMod\", \"EntryDll\": \"Content/DTMAPI/ReplacementEntry.dll\", \"EntryType\": \"DTMAPI.LegacyCollisionFixture.CollisionProbeMod\" }",
                    new UTF8Encoding(false));
                entryDriftRuntime.NotifyWorkshopModListChanged();
                Assert(
                    !entryDriftRuntime.LoadedMods.Any(mod => mod.Manifest.UniqueID == winnerOwner) &&
                    entryDriftRuntime.OwnerRequiresRestart(winnerOwner),
                    "Same-version Entry DLL drift must deactivate the resident owner and require restart without hot-loading changed bytes.");
            }
            finally
            {
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", previousPersistentRoot);
            }
        }

        private static void ManifestWireContradictionsFailClosed()
        {
            ExpectClassificationFailure(
                "unknown-manifest-type",
                new ManifestModel { UniqueID = "DTMAPI.Tests.UnknownType", Type = "AdvancedCodeMod", EntryDll = "Entry.dll" });
            ExpectClassificationFailure(
                "unknown-manifest-type",
                new ManifestModel { UniqueID = "DTMAPI.Tests.EmptyType", Type = "", EntryDll = "Entry.dll" });
            ExpectClassificationFailure(
                "unknown-code-mod-kind",
                new ManifestModel { UniqueID = "DTMAPI.Tests.UnknownKind", Type = "CodeMod", CodeModKind = "advanced", EntryDll = "Entry.dll" });
            ExpectClassificationFailure(
                "code-mod-kind-on-content-pack",
                new ManifestModel { UniqueID = "DTMAPI.Tests.ContentKind", Type = "ContentPack", CodeModKind = "Strict" });
            ExpectClassificationFailure(
                "content-pack-with-code-fields",
                new ManifestModel { UniqueID = "DTMAPI.Tests.ContentCode", Type = "ContentPack", EntryDll = "Entry.dll" });
        }

        private static void ExpectClassificationFailure(string code, ManifestModel manifest)
        {
            try
            {
                ManagedModClassifier.ClassifyDeclaredIdentity(manifest);
            }
            catch (ManagedModClassificationException ex) when (ex.Code == code)
            {
                return;
            }
            throw new InvalidOperationException("Expected managed manifest classification failure code " + code + ".");
        }

        private static void AdvancedReceiptReferenceRowsRequireExactFields()
        {
            string root = Path.Combine(DtmApiTestSession.Current.RootPath, "batch6-receipt-row-schema-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            MethodInfo validate = typeof(ManagedModClassifier).GetMethod("ValidateExactReferenceProperties", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Advanced receipt nested exact-field validator is unavailable.");
            foreach ((string name, string row) in new[]
            {
                ("missing-copy-local", "{\"gameRelativePath\":\"a\",\"assemblyName\":\"A\",\"length\":1,\"sha256\":\"" + new string('A', 64) + "\"}"),
                ("unknown-field", "{\"gameRelativePath\":\"a\",\"assemblyName\":\"A\",\"length\":1,\"sha256\":\"" + new string('A', 64) + "\",\"copyLocal\":false,\"unexpected\":true}"),
                ("duplicate-field", "{\"gameRelativePath\":\"a\",\"assemblyName\":\"A\",\"length\":1,\"sha256\":\"" + new string('A', 64) + "\",\"copyLocal\":false,\"copyLocal\":false}")
            })
            {
                string path = Path.Combine(root, name + ".json");
                File.WriteAllText(path, "{\"references\":[" + row + "]}", new UTF8Encoding(false));
                try
                {
                    validate.Invoke(null, new object[] { path });
                }
                catch (TargetInvocationException ex) when (ex.InnerException is ManagedModClassificationException failure && failure.Code == "advanced-reference-receipt-invalid")
                {
                    continue;
                }
                throw new InvalidOperationException("Advanced receipt reference row must fail exact-field validation: " + name + ".");
            }
        }

        private static void AdvancedPolicyRegistryRequiresExactUniqueIdBindings()
        {
            AdvancedReferencePolicyAuthority authority = AdvancedReferencePolicyAuthority.Load();
            AdvancedReferenceReceipt actionSpeed = CreateRegisteredReceipt(
                "doloctown-23762374-actionspeed-v1",
                "BEA3A26C2F831387127A996C326FE0C3F650CCCD7F9555192D76998A2B472473",
                "Yuuka.DTMAPI.ActionSpeed");
            Assert(authority.Matches(actionSpeed, "0.5.5", out string actionSpeedMismatch),
                "The exactly admitted third real-product policy/UniqueID binding must be accepted: " + actionSpeedMismatch);

            AdvancedReferenceReceipt synthetic = CreateRegisteredReceipt(
                "doloctown-23762374-g2-v1",
                "2E06DB24C3CBBB5E15639FB444BCB4D981F5961402773C0D66A24FAFEEDB8153",
                "DTMAPI.AdvancedFixture");
            Assert(authority.Matches(synthetic, "0.5.5", out string syntheticMismatch),
                "The frozen G2 fixture policy/UniqueID binding must remain admitted: " + syntheticMismatch);

            AdvancedReferenceReceipt autoFishing = CreateRegisteredReceipt(
                "doloctown-24456188-autofishing-v1",
                "0D9A1FFDB8DAB1A5B43F4B88BCAE77588EEA1C5CA3C5AEDDBBF9F013683DE628",
                "Yuuka.DTMAPI.AutoFishing");
            Assert(authority.Matches(autoFishing, "0.6.0", out string autoFishingMismatch),
                "The first real-product pilot policy/UniqueID binding must remain admitted: " + autoFishingMismatch);
            Assert(!authority.Matches(autoFishing, "0.5.5", out string understatedFloorMismatch) &&
                   understatedFloorMismatch.Contains("MinimumDTMApiVersion", StringComparison.Ordinal),
                "The current AutoFishing policy must reject a package that understates its 0.6 Runtime floor.");

            AdvancedReferenceReceipt historicalAutoFishing = CreateRegisteredReceipt(
                "doloctown-23762374-autofishing-v1",
                "C434506848F53E21A6371C487B43941FB3408031479FA85E96BB77145AAD5557",
                "Yuuka.DTMAPI.AutoFishing");
            Assert(authority.Matches(historicalAutoFishing, "0.5.5", out string historicalAutoFishingMismatch),
                "The exact already-issued AutoFishing 0.5.5 receipt policy must remain Runtime-accepted: " + historicalAutoFishingMismatch);

            AdvancedReferenceReceipt historicalDebugConsole = CreateRegisteredReceipt(
                "doloctown-23762374-debugconsole-v1",
                "483B48BC009F7E0FB80053BB0CC045C5AB57B09D9E049E5B5171115A6F24C165",
                "DTMAPI.DebugConsoleMod");
            Assert(authority.Matches(historicalDebugConsole, "0.5.5", out string historicalDebugConsoleMismatch),
                "The exact already-issued DebugConsole 0.5.5 receipt policy must remain Runtime-accepted: " + historicalDebugConsoleMismatch);
            historicalDebugConsole.ReferencePolicySha256 = new string('0', 64);
            Assert(!authority.Matches(historicalDebugConsole, "0.5.5", out string forgedHistoryMismatch) &&
                   forgedHistoryMismatch.Contains("identity/hash", StringComparison.Ordinal),
                "Historical acceptance must still reject a forged policy hash.");

            AdvancedReferenceReceipt moreEquipmentSlots = CreateRegisteredReceipt(
                "doloctown-24456188-moreequipmentslots-v1",
                "21655C30DF79470CCC0A9A5222DCC566F2A44E3AFBFA24D202180C29D2C6D4A1",
                "DTMAPI.MoreEquipmentSlotsMod");
            Assert(authority.Matches(moreEquipmentSlots, "0.6.0", out string moreEquipmentSlotsMismatch),
                "The current MoreEquipmentSlots policy must be admitted at its 0.6 Runtime floor: " + moreEquipmentSlotsMismatch);
            Assert(!authority.Matches(moreEquipmentSlots, "0.5.5", out string moreEquipmentSlotsUnderstatedFloorMismatch) &&
                   moreEquipmentSlotsUnderstatedFloorMismatch.Contains("MinimumDTMApiVersion", StringComparison.Ordinal),
                "The current MoreEquipmentSlots policy must reject a package that understates its 0.6 Runtime floor.");

            AdvancedReferenceReceipt historicalMoreEquipmentSlots = CreateRegisteredReceipt(
                "doloctown-23762374-moreequipmentslots-v1",
                "4B796B1E04921A411CEF505DF1B1CD63E14944A3B3A64F48EBC2E4860806B951",
                "DTMAPI.MoreEquipmentSlotsMod");
            Assert(authority.Matches(historicalMoreEquipmentSlots, "0.5.5", out string historicalMoreEquipmentSlotsMismatch),
                "The exact already-issued MoreEquipmentSlots 0.5.5 receipt policy must remain Runtime-accepted: " + historicalMoreEquipmentSlotsMismatch);

            AdvancedReferenceReceipt oneActionComplete = CreateRegisteredReceipt(
                "doloctown-23762374-oneactioncomplete-v1",
                "A44AF9101D19941B5F019D625A7E74D4D49CCE7CBCF7833E8B4ADCB6BED80A8D",
                "Yuuka.DTMAPI.OneActionComplete");
            Assert(authority.Matches(oneActionComplete, "0.5.5", out string oneActionMismatch),
                "The exactly admitted second real-product policy/UniqueID binding must be accepted: " + oneActionMismatch);

            AdvancedReferenceReceipt wrongBinding = CreateRegisteredReceipt(
                "doloctown-24456188-autofishing-v1",
                "0D9A1FFDB8DAB1A5B43F4B88BCAE77588EEA1C5CA3C5AEDDBBF9F013683DE628",
                "DTMAPI.AdvancedFixture");
            Assert(!authority.Matches(wrongBinding, "0.6.0", out string wrongBindingMismatch) &&
                   wrongBindingMismatch.Contains("UniqueID", StringComparison.Ordinal),
                "A registered policy presented by the wrong product identity must fail closed.");

            AdvancedReferenceReceipt unknownPolicy = CreateRegisteredReceipt(
                "doloctown-23762374-unknown-v1",
                synthetic.ReferencePolicySha256,
                synthetic.UniqueId);
            Assert(!authority.Matches(unknownPolicy, "0.5.5", out string unknownMismatch) &&
                   unknownMismatch.Contains("exact registry", StringComparison.Ordinal),
                "An unknown Advanced policy must fail closed before runtime loading.");
        }

        private static AdvancedReferenceReceipt CreateRegisteredReceipt(string policyId, string policySha256, string uniqueId)
        {
            bool currentBuild = policyId.StartsWith("doloctown-24456188-", StringComparison.Ordinal);
            string gameBuildId = currentBuild ? "24456188" : "23762374";
            string gameAssemblySha256 = currentBuild
                ? "E861E07E3CB82A6A21EEFA292456452F5AD12C25EC57972A59762AD3F3530923"
                : "C416D461C2559DDE8FB34D6B279BA84330E1403D18AB2D32A0224C6760D06404";
            long gameAssemblyLength = currentBuild ? 6384128L : 5993984L;
            return new AdvancedReferenceReceipt
            {
                SchemaVersion = 1,
                ReceiptKind = "DTMAPI.AdvancedCodeMod.ReferenceReceipt",
                UniqueId = uniqueId,
                CodeModKind = "Advanced",
                ReferencePolicyId = policyId,
                ReferencePolicyVersion = 1,
                ReferencePolicySha256 = policySha256,
                TargetFramework = "netstandard2.0",
                GameBuildId = gameBuildId,
                GameAssemblyRelativePath = "DolocTown_Data/Managed/Assembly-CSharp.dll",
                GameAssemblySha256 = gameAssemblySha256,
                References = new List<AdvancedReferenceReceiptItem>
                {
                    new AdvancedReferenceReceiptItem
                    {
                        GameRelativePath = "BepInEx/core/0Harmony.dll",
                        AssemblyName = "0Harmony",
                        Length = 204800,
                        Sha256 = "1A21CC03424FC82C3DD1346905D16494536B9595AE4162228D99FB7C285C1031",
                        CopyLocal = false
                    },
                    new AdvancedReferenceReceiptItem
                    {
                        GameRelativePath = "DolocTown_Data/Managed/Assembly-CSharp.dll",
                        AssemblyName = "Assembly-CSharp",
                        Length = gameAssemblyLength,
                        Sha256 = gameAssemblySha256,
                        CopyLocal = false
                    }
                }
            };
        }

        private static void AdvancedInstalledGameContextAllowsExactDriftAndUnknown()
        {
            string steamApps = Path.Combine(DtmApiTestSession.Current.RootPath, "batch6-advanced-context-" + Guid.NewGuid().ToString("N"), "steamapps");
            string gameRoot = Path.Combine(steamApps, "common", "Doloc Town");
            string gameAssembly = Path.Combine(gameRoot, "DolocTown_Data", "Managed", "Assembly-CSharp.dll");
            string harmony = Path.Combine(gameRoot, "BepInEx", "core", "0Harmony.dll");
            Directory.CreateDirectory(Path.GetDirectoryName(gameAssembly)!);
            Directory.CreateDirectory(Path.GetDirectoryName(harmony)!);
            File.WriteAllText(gameAssembly, "exact synthetic game assembly", new UTF8Encoding(false));
            File.WriteAllText(harmony, "exact synthetic Harmony", new UTF8Encoding(false));
            string appManifest = Path.Combine(steamApps, "appmanifest_2285550.acf");
            File.WriteAllText(appManifest, "\"AppState\"\n{\n  \"appid\" \"2285550\"\n  \"buildid\" \"23762374\"\n}\n", new UTF8Encoding(false));

            var receipt = new AdvancedReferenceReceipt
            {
                GameBuildId = "23762374",
                References = new List<AdvancedReferenceReceiptItem>
                {
                    new AdvancedReferenceReceiptItem
                    {
                        GameRelativePath = "DolocTown_Data/Managed/Assembly-CSharp.dll",
                        AssemblyName = "Assembly-CSharp",
                        Length = new FileInfo(gameAssembly).Length,
                        Sha256 = HashFile(gameAssembly),
                        CopyLocal = false
                    },
                    new AdvancedReferenceReceiptItem
                    {
                        GameRelativePath = "BepInEx/core/0Harmony.dll",
                        AssemblyName = "0Harmony",
                        Length = new FileInfo(harmony).Length,
                        Sha256 = HashFile(harmony),
                        CopyLocal = false
                    }
                }
            };
            var classifier = new ManagedModClassifier(gameRoot);

            AdvancedGameCompatibilityObservation exact = classifier.ObserveAdvancedGameCompatibility(receipt);
            Assert(exact.Context == AdvancedGameCompatibilityContext.Exact &&
                   exact.ExactReferenceCount == 2 &&
                   exact.DriftReferenceCount == 0 &&
                   exact.UnavailableReferenceCount == 0 &&
                   exact.ToDiagnosticString().StartsWith("Exact;", StringComparison.Ordinal),
                "An exact installed build and exact receipt-baseline references must be recorded as Exact without bypassing later activation.");

            File.AppendAllText(gameAssembly, " drift", new UTF8Encoding(false));
            AdvancedGameCompatibilityObservation drift = classifier.ObserveAdvancedGameCompatibility(receipt);
            Assert(drift.Context == AdvancedGameCompatibilityContext.Drift &&
                   drift.ExactReferenceCount == 1 &&
                   drift.DriftReferenceCount == 1 &&
                   drift.ToDiagnosticString().StartsWith("Drift;", StringComparison.Ordinal),
                "A readable installed build with reference-byte drift must become non-blocking Drift context rather than a pre-load classification failure.");

            File.Delete(appManifest);
            AdvancedGameCompatibilityObservation unknown = classifier.ObserveAdvancedGameCompatibility(receipt);
            Assert(unknown.Context == AdvancedGameCompatibilityContext.Unknown &&
                   unknown.ToDiagnosticString().Contains("installedBuild=unknown", StringComparison.Ordinal),
                "An unavailable Steam build mapping must become non-blocking Unknown context rather than a pre-load classification failure.");

            File.WriteAllText(appManifest, "\"AppState\"\n{\n  \"appid\" \"2285550\"\n  \"buildid\" \"99999999\"\n}\n", new UTF8Encoding(false));
            File.Delete(harmony);
            AdvancedGameCompatibilityObservation knownMissingReference = classifier.ObserveAdvancedGameCompatibility(receipt);
            Assert(knownMissingReference.Context == AdvancedGameCompatibilityContext.Drift &&
                   knownMissingReference.UnavailableReferenceCount == 1,
                "A recognized installed build with a missing current reference remains Drift context; real load/Entry/product activation decides the product result.");
        }

        private static void AdvancedSdk01PackagesLoadAcrossCompatibilityContextsAndIsolateFailure()
        {
            const string successOwner = "DTMAPI.AdvancedFixture";
            const string failureOwner = "Yuuka.DTMAPI.ActionSpeed";
            string root = Path.Combine(DtmApiTestSession.Current.RootPath, "batch6-advanced-sdk01-runtime-" + Guid.NewGuid().ToString("N"));
            string gameRoot = Path.Combine(root, "Game");
            Directory.CreateDirectory(gameRoot);
            AdvancedSdk01PackageFixture successPackage = WriteAdvancedSdk01Package(
                Path.Combine(root, "Packages", "Success"),
                successOwner,
                "doloctown-23762374-g2-v1",
                "2E06DB24C3CBBB5E15639FB444BCB4D981F5961402773C0D66A24FAFEEDB8153",
                Path.Combine(GetFixtureOutput("AdvancedRuntimeSuccessFixture"), "DTMAPI.AdvancedRuntimeSuccessFixture.dll"),
                "DTMAPI.AdvancedRuntimeSuccessFixture.SuccessProbeMod");
            AdvancedSdk01PackageFixture failurePackage = WriteAdvancedSdk01Package(
                Path.Combine(root, "Packages", "Failure"),
                failureOwner,
                "doloctown-23762374-actionspeed-v1",
                "BEA3A26C2F831387127A996C326FE0C3F650CCCD7F9555192D76998A2B472473",
                Path.Combine(GetFixtureOutput("AdvancedRuntimeFailureFixture"), "DTMAPI.AdvancedRuntimeFailureFixture.dll"),
                "DTMAPI.AdvancedRuntimeFailureFixture.FailureProbeMod");
            AdvancedSdk01PackageFixture currentFloorPackage = WriteAdvancedSdk01Package(
                Path.Combine(root, "Packages", "CurrentAutoFishingFloor"),
                "Yuuka.DTMAPI.AutoFishing",
                "doloctown-24456188-autofishing-v1",
                "0D9A1FFDB8DAB1A5B43F4B88BCAE77588EEA1C5CA3C5AEDDBBF9F013683DE628",
                Path.Combine(GetFixtureOutput("AdvancedRuntimeSuccessFixture"), "DTMAPI.AdvancedRuntimeSuccessFixture.dll"),
                "DTMAPI.AdvancedRuntimeSuccessFixture.SuccessProbeMod",
                minimumDtmApiVersion: "0.6.0");

            ManagedModClassification currentFloorClassification = new ManagedModClassifier(
                gameRoot,
                _ => new AdvancedGameCompatibilityObservation(
                    AdvancedGameCompatibilityContext.Exact,
                    "24456188",
                    "24456188",
                    2,
                    0,
                    0,
                    2)).Classify(
                    currentFloorPackage.Manifest,
                    currentFloorPackage.RootPath,
                    currentFloorPackage.ManifestPath,
                    "Local",
                    nativeWorkshopSourceVerified: true,
                    workshopId: null);
            Assert(currentFloorClassification.IsAdvanced &&
                   currentFloorClassification.AdvancedReferenceVerified &&
                   currentFloorClassification.ReferencePolicyId == "doloctown-24456188-autofishing-v1" &&
                   currentFloorPackage.Manifest.MinimumDTMApiVersion == "0.6.0",
                "The current Core must classify the SDK 0.1/frozen-API package whose exact current AutoFishing policy truthfully raises only its Runtime floor to 0.6.0.");

            string? previousPersistentRoot = Environment.GetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT");
            try
            {
                int expectedEntryCount = 0;
                foreach (AdvancedGameCompatibilityContext context in new[]
                {
                    AdvancedGameCompatibilityContext.Exact,
                    AdvancedGameCompatibilityContext.Drift,
                    AdvancedGameCompatibilityContext.Unknown
                })
                {
                    AdvancedGameCompatibilityObservation observation = CreateCompatibilityObservation(context);
                    var classifier = new ManagedModClassifier(gameRoot, _ => observation);
                    ManagedModClassification classification = classifier.Classify(
                        successPackage.Manifest,
                        successPackage.RootPath,
                        successPackage.ManifestPath,
                        "Local",
                        nativeWorkshopSourceVerified: true,
                        workshopId: null);
                    Assert(classification.IsAdvanced && classification.AdvancedReferenceVerified &&
                           classification.GameCompatibility.StartsWith(context + ";", StringComparison.Ordinal),
                        "A tracked SDK 0.1/target 0.5.5 package must complete strict classification on the 0.6 Runtime for " + context + " context.");
                    if (context != AdvancedGameCompatibilityContext.Exact)
                        AssertManagerProjectsActualAdvancedContext(successPackage, classification, context);

                    Environment.SetEnvironmentVariable(
                        "DTMAPI_DOLOC_PERSISTENT_ROOT",
                        Path.Combine(root, "Persistent-" + context));
                    var checkpoints = new List<ModLoadCheckpoint>();
                    var runtime = new DtmApiRuntime(new Batch6Host(gameRoot));
                    runtime.ConfigureAdvancedHarmonyInspectorForTests(new FakeHarmonyInspector());
                    runtime.ConfigureModLoadCheckpointForTests(checkpoints.Add);
                    try
                    {
                        runtime.Start();
                        bool loaded = InvokeLoadCodeMod(runtime, successPackage, classification);
                        Assert(loaded && runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == successOwner),
                            "The classified SDK 0.1 package must pass Assembly.LoadFrom, Entry, publication, and commit for " + context + " context.");
                        Assert(checkpoints.SequenceEqual(new[]
                        {
                            ModLoadCheckpoint.AssemblyLoaded,
                            ModLoadCheckpoint.InstanceCreated,
                            ModLoadCheckpoint.ContextAttached,
                            ModLoadCheckpoint.EntryReturned,
                            ModLoadCheckpoint.LoadedRegistryPublished,
                            ModLoadCheckpoint.InstancePublished,
                            ModLoadCheckpoint.LoadedListPublished,
                            ModLoadCheckpoint.BeforeCommit
                        }), "The " + context + " package must traverse the complete Advanced Classify-to-commit load sequence.");
                        expectedEntryCount++;
                        Assert(ReadStaticInt(
                                "DTMAPI.AdvancedRuntimeSuccessFixture",
                                "DTMAPI.AdvancedRuntimeSuccessFixture.SuccessProbeMod",
                                "EntryCount") == expectedEntryCount,
                            "The real Advanced fixture Entry must execute exactly once for " + context + " context.");
                        Assert(runtime.Diagnostics.GetWarnings().All(warning => warning.Owner != successOwner),
                            "Successful " + context + " activation must not publish a product Warning.");
                        string log = File.ReadAllText(runtime.Diagnostics.LatestLogPath, Encoding.UTF8);
                        if (context != AdvancedGameCompatibilityContext.Exact)
                        {
                            Assert(log.Contains("Advanced compatibility activation passed owner=" + successOwner, StringComparison.Ordinal) &&
                                   log.Contains("gameCompatibility=" + context + ";", StringComparison.Ordinal),
                                "Successful " + context + " activation must publish one informational activation-passed record.");
                        }
                    }
                    finally
                    {
                        runtime.NotifyRuntimeShutdown("AdvancedSdk01ContextTest");
                    }
                }

                var driftClassifier = new ManagedModClassifier(
                    gameRoot,
                    _ => CreateCompatibilityObservation(AdvancedGameCompatibilityContext.Drift));
                ManagedModClassification validClassification = driftClassifier.Classify(
                    successPackage.Manifest,
                    successPackage.RootPath,
                    successPackage.ManifestPath,
                    "Local",
                    true,
                    null);
                ManagedModClassification failureClassification = driftClassifier.Classify(
                    failurePackage.Manifest,
                    failurePackage.RootPath,
                    failurePackage.ManifestPath,
                    "Local",
                    true,
                    null);
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", Path.Combine(root, "Persistent-failure"));
                var isolationRuntime = new DtmApiRuntime(new Batch6Host(gameRoot));
                isolationRuntime.ConfigureAdvancedHarmonyInspectorForTests(new FakeHarmonyInspector());
                var failureCheckpoints = new List<ModLoadCheckpoint>();
                try
                {
                    isolationRuntime.Start();
                    Assert(InvokeLoadCodeMod(isolationRuntime, successPackage, validClassification),
                        "The valid Advanced sibling must load before the isolated failure case.");
                    isolationRuntime.ConfigureModLoadCheckpointForTests(failureCheckpoints.Add);
                    Assert(!InvokeLoadCodeMod(isolationRuntime, failurePackage, failureClassification),
                        "A throwing Advanced Entry must reject only its own product.");
                    Assert(isolationRuntime.LoadedMods.Any(mod => mod.Manifest.UniqueID == successOwner) &&
                           !isolationRuntime.LoadedMods.Any(mod => mod.Manifest.UniqueID == failureOwner),
                        "Advanced Entry failure cleanup must preserve the already committed sibling and keep the failing owner unpublished.");
                    Assert(failureCheckpoints.SequenceEqual(new[]
                    {
                        ModLoadCheckpoint.AssemblyLoaded,
                        ModLoadCheckpoint.InstanceCreated,
                        ModLoadCheckpoint.ContextAttached
                    }), "The failing owner must stop at Entry and never reach registry/list publication or commit.");
                    Assert(isolationRuntime.OwnerRequiresRestart(failureOwner) &&
                           isolationRuntime.Diagnostics.GetErrors().Any(error =>
                               error.Owner == failureOwner &&
                               error.Details.Contains("throwing-advanced-sdk01-entry-probe", StringComparison.Ordinal) &&
                               error.Details.Contains("remaining=0", StringComparison.OrdinalIgnoreCase)),
                        "The failing Advanced owner must expose its Entry cause, zero-resource owner cleanup, and conservative post-load restart boundary.");
                    Assert(isolationRuntime.Diagnostics.GetWarnings().All(warning => warning.Owner != successOwner),
                        "The valid Drift sibling must remain warning-free when another owner fails.");
                }
                finally
                {
                    isolationRuntime.NotifyRuntimeShutdown("AdvancedSdk01FailureIsolationTest");
                }

                string markerText = File.ReadAllText(successPackage.MarkerPath, Encoding.UTF8);
                File.WriteAllText(
                    successPackage.MarkerPath,
                    markerText.Replace("\"targetDtmApiVersion\": \"0.5.5\"", "\"targetDtmApiVersion\": \"0.6.0\"", StringComparison.Ordinal),
                    new UTF8Encoding(false));
                AssertClassificationFailure(
                    "advanced-package-marker-invalid",
                    () => new ManagedModClassifier(gameRoot).Classify(
                        successPackage.Manifest,
                        successPackage.RootPath,
                        successPackage.ManifestPath,
                        "Local",
                        true,
                        null),
                    "A package marker rebound to the running 0.6 Runtime must still fail before Assembly.LoadFrom; only tracked SDK 0.1/target 0.5.5 is admitted.");
                foreach ((string Field, string Original, string Rebound) mutation in new[]
                {
                    ("targetDtmApiVersion", "0.5.5", "0.6.1"),
                    ("targetDtmApiVersion", "0.5.5", "0.7.0"),
                    ("authorSdkVersion", "0.1.0", "0.3.0")
                })
                {
                    File.WriteAllText(successPackage.MarkerPath, markerText.Replace(
                        "\"" + mutation.Field + "\": \"" + mutation.Original + "\"",
                        "\"" + mutation.Field + "\": \"" + mutation.Rebound + "\"", StringComparison.Ordinal), new UTF8Encoding(false));
                    AssertClassificationFailure("advanced-package-marker-invalid",
                        () => new ManagedModClassifier(gameRoot).Classify(successPackage.Manifest, successPackage.RootPath,
                            successPackage.ManifestPath, "Local", true, null),
                        "Advanced receipt must not be rebound to unknown or planned SDK/API target metadata: " + mutation.Rebound);
                }
            }
            finally
            {
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", previousPersistentRoot);
            }
        }

        private static AdvancedGameCompatibilityObservation CreateCompatibilityObservation(AdvancedGameCompatibilityContext context)
        {
            switch (context)
            {
                case AdvancedGameCompatibilityContext.Exact:
                    return new AdvancedGameCompatibilityObservation(context, "23762374", "23762374", 2, 0, 0, 2);
                case AdvancedGameCompatibilityContext.Drift:
                    return new AdvancedGameCompatibilityObservation(context, "23762374", "24456188", 1, 1, 0, 2);
                case AdvancedGameCompatibilityContext.Unknown:
                    return new AdvancedGameCompatibilityObservation(context, "23762374", string.Empty, 0, 0, 2, 2);
                default:
                    throw new InvalidOperationException("Unexpected Advanced compatibility context: " + context + ".");
            }
        }

        private static void AssertManagerProjectsActualAdvancedContext(
            AdvancedSdk01PackageFixture package,
            ManagedModClassification classification,
            AdvancedGameCompatibilityContext context)
        {
            var status = new DtmModStatusInfo(
                package.Manifest.UniqueID,
                package.Manifest.Name,
                package.Manifest.Version,
                package.Manifest.Type,
                "Local",
                string.Empty,
                true,
                false,
                string.Empty,
                package.Manifest.EntryDll,
                package.Manifest.EntryType,
                true,
                "loaded",
                "loaded",
                "Loaded by SDK 0.1 compatibility fixture.",
                package.ManifestPath,
                package.RootPath,
                classification);
            string formatted = ManagerPageRowFormatter.FormatModRow(ManagerModRow.FromMod(status));
            Assert(formatted.Contains("gameCompatibility=" + context + ";", StringComparison.Ordinal) &&
                   formatted.Contains("referenceReceipt=verified|policy:" + classification.ReferencePolicyId, StringComparison.Ordinal),
                "Manager must project the actual classified " + context + " context and receipt authority without a parallel public API.");
        }

        private static bool InvokeLoadCodeMod(
            DtmApiRuntime runtime,
            AdvancedSdk01PackageFixture package,
            ManagedModClassification classification)
        {
            var discovered = new DiscoveredMod(
                package.Manifest,
                package.RootPath,
                package.ManifestPath,
                "Local",
                null,
                true,
                true,
                string.Empty,
                false,
                string.Empty,
                classification: classification);
            MethodInfo load = typeof(DtmApiRuntime).GetMethod("LoadCodeMod", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("DtmApiRuntime.LoadCodeMod is unavailable to the Advanced integration fixture.");
            return (bool)(load.Invoke(runtime, new object?[] { discovered, null }) ?? false);
        }

        private static AdvancedSdk01PackageFixture WriteAdvancedSdk01Package(
            string root,
            string uniqueId,
            string referencePolicyId,
            string referencePolicySha256,
            string entrySource,
            string entryType,
            string minimumDtmApiVersion = "0.5.5")
        {
            const string manifestRelativePath = "Content/DTMAPI/manifest.json";
            const string entryRelativePath = "Content/DTMAPI/Entry.dll";
            const string receiptRelativePath = "Content/DTMAPI/dtmapi-advanced-references.json";
            string packageRoot = Path.GetFullPath(root);
            string contentRoot = Path.Combine(packageRoot, "Content", "DTMAPI");
            Directory.CreateDirectory(contentRoot);
            string entryPath = Path.Combine(contentRoot, "Entry.dll");
            File.Copy(entrySource, entryPath, overwrite: true);
            string manifestPath = Path.Combine(contentRoot, "manifest.json");
            File.WriteAllText(
                manifestPath,
                "{\n" +
                "  \"Name\": \"SDK 0.1 Advanced Runtime Fixture\",\n" +
                "  \"Author\": \"DTMAPI\",\n" +
                "  \"Version\": \"1.0.0\",\n" +
                "  \"UniqueID\": \"" + uniqueId + "\",\n" +
                "  \"Type\": \"CodeMod\",\n" +
                "  \"CodeModKind\": \"Advanced\",\n" +
                "  \"EntryDll\": \"" + entryRelativePath + "\",\n" +
                "  \"EntryType\": \"" + entryType + "\",\n" +
                "  \"MinimumDTMApiVersion\": \"" + minimumDtmApiVersion + "\"\n" +
                "}\n",
                new UTF8Encoding(false));
            string manifestSha256 = HashFile(manifestPath);
            string entrySha256 = HashFile(entryPath);
            long entryLength = new FileInfo(entryPath).Length;
            string harmonyOwner = ManagedModClassifier.GetExpectedHarmonyOwner(uniqueId);
            bool currentBuild = referencePolicyId.StartsWith("doloctown-24456188-", StringComparison.Ordinal);
            string gameBuildId = currentBuild ? "24456188" : "23762374";
            string gameAssemblySha256 = currentBuild
                ? "E861E07E3CB82A6A21EEFA292456452F5AD12C25EC57972A59762AD3F3530923"
                : "C416D461C2559DDE8FB34D6B279BA84330E1403D18AB2D32A0224C6760D06404";
            long gameAssemblyLength = currentBuild ? 6384128L : 5993984L;
            string receiptPath = Path.Combine(contentRoot, "dtmapi-advanced-references.json");
            File.WriteAllText(
                receiptPath,
                "{\n" +
                "  \"schemaVersion\": 1,\n" +
                "  \"receiptKind\": \"DTMAPI.AdvancedCodeMod.ReferenceReceipt\",\n" +
                "  \"uniqueId\": \"" + uniqueId + "\",\n" +
                "  \"codeModKind\": \"Advanced\",\n" +
                "  \"referencePolicyId\": \"" + referencePolicyId + "\",\n" +
                "  \"referencePolicyVersion\": 1,\n" +
                "  \"referencePolicySha256\": \"" + referencePolicySha256 + "\",\n" +
                "  \"targetFramework\": \"netstandard2.0\",\n" +
                "  \"gameBuildId\": \"" + gameBuildId + "\",\n" +
                "  \"gameAssemblyRelativePath\": \"DolocTown_Data/Managed/Assembly-CSharp.dll\",\n" +
                "  \"gameAssemblySha256\": \"" + gameAssemblySha256 + "\",\n" +
                "  \"manifestPath\": \"" + manifestRelativePath + "\",\n" +
                "  \"manifestSha256\": \"" + manifestSha256 + "\",\n" +
                "  \"entryDllPath\": \"" + entryRelativePath + "\",\n" +
                "  \"entryDllLength\": " + entryLength + ",\n" +
                "  \"entryDllSha256\": \"" + entrySha256 + "\",\n" +
                "  \"harmonyOwner\": \"" + harmonyOwner + "\",\n" +
                "  \"references\": [\n" +
                "    { \"gameRelativePath\": \"BepInEx/core/0Harmony.dll\", \"assemblyName\": \"0Harmony\", \"length\": 204800, \"sha256\": \"1A21CC03424FC82C3DD1346905D16494536B9595AE4162228D99FB7C285C1031\", \"copyLocal\": false },\n" +
                "    { \"gameRelativePath\": \"DolocTown_Data/Managed/Assembly-CSharp.dll\", \"assemblyName\": \"Assembly-CSharp\", \"length\": " + gameAssemblyLength + ", \"sha256\": \"" + gameAssemblySha256 + "\", \"copyLocal\": false }\n" +
                "  ]\n" +
                "}\n",
                new UTF8Encoding(false));
            string receiptSha256 = HashFile(receiptPath);
            string markerPath = Path.Combine(contentRoot, "dtmapi-package.json");
            File.WriteAllText(
                markerPath,
                "{\n" +
                "  \"schemaVersion\": 2,\n" +
                "  \"owner\": \"DTMAPI\",\n" +
                "  \"uniqueId\": \"" + uniqueId + "\",\n" +
                "  \"version\": \"1.0.0\",\n" +
                "  \"packageKind\": \"CodeMod\",\n" +
                "  \"codeModKind\": \"Advanced\",\n" +
                "  \"authorSdkVersion\": \"0.1.0\",\n" +
                "  \"targetDtmApiVersion\": \"0.5.5\",\n" +
                "  \"manifestPath\": \"" + manifestRelativePath + "\",\n" +
                "  \"manifestSha256\": \"" + manifestSha256 + "\",\n" +
                "  \"entryDllPath\": \"" + entryRelativePath + "\",\n" +
                "  \"entryDllSha256\": \"" + entrySha256 + "\",\n" +
                "  \"advancedReferenceReceiptPath\": \"" + receiptRelativePath + "\",\n" +
                "  \"advancedReferenceReceiptSha256\": \"" + receiptSha256 + "\",\n" +
                "  \"authority\": \"dtmapi-author-sdk-package-binding\"\n" +
                "}\n",
                new UTF8Encoding(false));
            var manifest = new ManifestModel
            {
                Name = "SDK 0.1 Advanced Runtime Fixture",
                Author = "DTMAPI",
                Version = "1.0.0",
                UniqueID = uniqueId,
                Type = "CodeMod",
                CodeModKind = "Advanced",
                EntryDll = entryRelativePath,
                EntryType = entryType,
                MinimumDTMApiVersion = minimumDtmApiVersion
            };
            manifest.Normalize();
            return new AdvancedSdk01PackageFixture(packageRoot, manifestPath, markerPath, manifest);
        }

        private static void HarmonySupervisorRejectsWrongOwnerDuplicateAndLateDrift()
        {
            const string ownerId = "DTMAPI.Tests.AdvancedSynthetic";
            const string expectedOwner = "dtmapi.mod.dtmapi.tests.advancedsynthetic";
            string moduleMvid = typeof(Batch6AdvancedRuntimeTests).Assembly.ManifestModule.ModuleVersionId.ToString("D");
            var classification = new ManagedModClassification(
                ManagedModIdentity.AdvancedCodeMod,
                "Advanced",
                "sdk-reference-receipt-verified",
                "managed-local",
                "native-code/receipt-baseline-bound",
                "Exact; compiledBuild=23762374; installedBuild=23762374; references=2/2; drifted=0; unavailable=0",
                "restart-required-after-load",
                expectedOwner,
                true,
                "receipt.json",
                "doloctown-23762374-g2-v1",
                1,
                new string('A', 64),
                "netstandard2.0",
                "23762374");

            var wrongInspector = new FakeHarmonyInspector();
            var wrongSupervisor = new AdvancedHarmonySupervisor(wrongInspector);
            wrongSupervisor.BeginEntry(ownerId, classification, typeof(Batch6AdvancedRuntimeTests).Assembly);
            wrongInspector.Patches.Add(Patch("wrong.owner", "target:1", "patch:1", moduleMvid));
            AssertViolation("advanced-harmony-owner-mismatch", () => wrongSupervisor.CompleteEntry(ownerId));
            ModOwnerCleanupParticipantResult wrongCleanup = wrongSupervisor.RemoveOwner(ownerId, ModOwnerCleanupReason.EntryFailed);
            Assert(wrongCleanup.RemainingResources == 1 && wrongInspector.UnpatchedOwners.SequenceEqual(new[] { expectedOwner }) &&
                   wrongCleanup.Details.Contains("advanced-harmony-cleanup-failed", StringComparison.Ordinal),
                "Wrong-owner rollback must unpatch only the canonical owner, retain contamination evidence, and expose the cleanup-failed machine code.");
            var wrongCleanupSummary = new ModOwnerParticipantCleanupSummary(
                ownerId,
                ModOwnerCleanupReason.EntryFailed,
                new[]
                {
                    new ModOwnerParticipantCleanupEntry(
                        wrongSupervisor.ParticipantId,
                        wrongCleanup.RemovedResources,
                        success: wrongCleanup.FailureCount == 0 && wrongCleanup.RemainingResources == 0,
                        wrongCleanup.Details,
                        wrongCleanup.RemainingResources,
                        wrongCleanup.FailureCount)
                });
            Assert(wrongCleanupSummary.FormatSummary().Contains("advanced-harmony-cleanup-failed", StringComparison.Ordinal),
                "The authoritative participant summary must preserve the cleanup failure code for runtime logs and exported reports.");

            var duplicateInspector = new FakeHarmonyInspector();
            var duplicateSupervisor = new AdvancedHarmonySupervisor(duplicateInspector);
            duplicateSupervisor.BeginEntry(ownerId, classification, typeof(Batch6AdvancedRuntimeTests).Assembly);
            duplicateInspector.Patches.Add(Patch(expectedOwner, "target:2", "patch:2", moduleMvid));
            duplicateInspector.Patches.Add(Patch(expectedOwner, "target:2", "patch:2", moduleMvid));
            AssertViolation("advanced-harmony-duplicate-patch", () => duplicateSupervisor.CompleteEntry(ownerId));
            ModOwnerCleanupParticipantResult duplicateCleanup = duplicateSupervisor.RemoveOwner(ownerId, ModOwnerCleanupReason.EntryFailed);
            Assert(duplicateCleanup.RemainingResources == 0 && duplicateInspector.UnpatchedOwners.SequenceEqual(new[] { expectedOwner }), "Duplicate canonical patches must be removed through owner-only rollback.");

            var lateInspector = new FakeHarmonyInspector();
            var lateSupervisor = new AdvancedHarmonySupervisor(lateInspector);
            lateSupervisor.BeginEntry(ownerId, classification, typeof(Batch6AdvancedRuntimeTests).Assembly);
            lateInspector.Patches.Add(Patch(expectedOwner, "target:3", "patch:3", moduleMvid));
            lateSupervisor.CompleteEntry(ownerId);
            lateInspector.Patches.Add(Patch("late.wrong.owner", "target:4", "patch:4", moduleMvid));
            AdvancedHarmonyAuditIssue issue = lateSupervisor.AuditActiveOwners().Single();
            Assert(issue.OwnerId == ownerId && issue.Code == "advanced-harmony-late-owner-drift", "Late drift must be attributed by the synthetic entry module without touching a sibling.");
            ModOwnerCleanupParticipantResult lateCleanup = lateSupervisor.RemoveOwner(ownerId, ModOwnerCleanupReason.EntryFailed);
            Assert(lateCleanup.RemainingResources == 1 && lateInspector.Patches.Single().Owner == "late.wrong.owner", "Late cleanup must remove only canonical patches and retain wrong-owner drift evidence.");

            var siblingInspector = new FakeHarmonyInspector();
            var siblingSupervisor = new AdvancedHarmonySupervisor(siblingInspector);
            siblingSupervisor.BeginEntry(ownerId, classification, typeof(Batch6AdvancedRuntimeTests).Assembly);
            siblingSupervisor.CompleteEntry(ownerId);
            siblingInspector.Patches.Add(Patch("strict.or.external.sibling", "target:5", "patch:5", Guid.NewGuid().ToString("D")));
            Assert(siblingSupervisor.AuditActiveOwners().Count == 0, "A late patch whose owner and module do not belong to an Advanced entry must remain sibling-owned, not become an Advanced violation.");
            Assert(siblingSupervisor.AuditActiveOwners().Count == 0, "An accepted sibling snapshot must not be reported again on the next audit.");
            siblingInspector.Patches.Add(Patch("second.sibling", "target:6", "patch:6", Guid.NewGuid().ToString("D")));
            siblingInspector.Patches.Add(Patch("late.wrong.owner", "target:7", "patch:7", moduleMvid));
            AdvancedHarmonyAuditIssue mixedIssue = siblingSupervisor.AuditActiveOwners().Single();
            Assert(mixedIssue.OwnerId == ownerId && mixedIssue.Code == "advanced-harmony-late-owner-drift" &&
                   !mixedIssue.Details.Contains("target:6", StringComparison.Ordinal) && mixedIssue.Details.Contains("target:7", StringComparison.Ordinal),
                "A mixed audit must report only the patch attributable to the Advanced entry module and leave the sibling out of the violation.");
            siblingInspector.Patches.Add(Patch(expectedOwner, "target:8", "patch:8", Guid.NewGuid().ToString("D")));
            AdvancedHarmonyAuditIssue canonicalIssue = siblingSupervisor.AuditActiveOwners().Single();
            Assert(canonicalIssue.OwnerId == ownerId && canonicalIssue.Details.Contains("target:8", StringComparison.Ordinal),
                "The canonical Harmony owner must remain attributable even when the patch method comes from a foreign or dynamic module.");
            Assert(siblingInspector.UnpatchedOwners.Count == 0, "Unattributed sibling patches must never trigger owner-scoped cleanup.");

            var reactivationInspector = new FakeHarmonyInspector();
            var reactivationSupervisor = new AdvancedHarmonySupervisor(reactivationInspector);
            reactivationSupervisor.BeginEntry(ownerId, classification, typeof(Batch6AdvancedRuntimeTests).Assembly);
            AdvancedHarmonyPatchRecord entryPatch = Patch(expectedOwner, "target:reactivation", "patch:reactivation", moduleMvid);
            reactivationInspector.Patches.Add(entryPatch);
            reactivationSupervisor.CompleteEntry(ownerId);
            reactivationInspector.Patches.Clear();
            Assert(reactivationSupervisor.AuditActiveOwners().Count == 0,
                "Removing an Entry-observed canonical patch while the product is suspended must not create late drift.");
            reactivationInspector.Patches.Add(entryPatch);
            Assert(reactivationSupervisor.AuditActiveOwners().Count == 0,
                "Reinstalling the exact Entry-observed canonical patch after title suspension must be accepted.");
            reactivationInspector.Patches.Add(entryPatch);
            AdvancedHarmonyAuditIssue duplicateReactivationIssue = reactivationSupervisor.AuditActiveOwners().Single();
            Assert(duplicateReactivationIssue.OwnerId == ownerId &&
                   duplicateReactivationIssue.Code == "advanced-harmony-late-owner-drift" &&
                   duplicateReactivationIssue.Details.Contains("target:reactivation", StringComparison.Ordinal),
                "An exact identity reactivation that exceeds the Entry-observed patch count must still fail closed as late drift.");

            var unavailable = new AdvancedHarmonySupervisor(new UnavailableHarmonyInspector());
            AssertViolation("advanced-harmony-unavailable", () => unavailable.BeginEntry(ownerId, classification, typeof(Batch6AdvancedRuntimeTests).Assembly));
        }

        private static void RenamedManagedNativePayloadFailsClosed()
        {
            string root = Path.Combine(DtmApiTestSession.Current.RootPath, "batch6-renamed-native-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            string entryPath = Path.Combine(root, "Entry.dll");
            File.Copy(typeof(Batch6AdvancedRuntimeTests).Assembly.Location, entryPath);
            string renamedNative = Path.Combine(root, "assets", "payload.dat");
            Directory.CreateDirectory(Path.GetDirectoryName(renamedNative)!);
            File.Copy(typeof(BootstrapPlugin).Assembly.Location, renamedNative);

            MethodInfo validate = typeof(ManagedModClassifier).GetMethod("ValidateAdvancedPackageShape", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Advanced package-shape validator is unavailable.");
            AssertClassificationInvocation(
                "bundled-native-runtime-dependency",
                () => validate.Invoke(null, new object[] { root, entryPath }),
                "A BepInEx/native assembly renamed to a non-DLL payload must fail before load.");
        }

        private static void AdvancedWrongTargetFrameworkFailsClosed()
        {
            string root = Path.Combine(DtmApiTestSession.Current.RootPath, "batch6-wrong-tfm-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            string entryPath = Path.Combine(root, "Entry.dll");
            File.Copy(typeof(Batch6AdvancedRuntimeTests).Assembly.Location, entryPath);
            var manifest = new ManifestModel
            {
                Name = "Wrong TFM",
                Author = "DTMAPI",
                Version = "1.0.0",
                UniqueID = "DTMAPI.Tests.WrongTfm",
                Type = "CodeMod",
                CodeModKind = "Advanced",
                EntryDll = "Entry.dll",
                EntryType = "Synthetic.Entry"
            };
            MethodInfo validate = typeof(ManagedModClassifier).GetMethod("ValidateCodeModAssemblyBoundary", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("CodeMod assembly-boundary validator is unavailable.");
            AssertClassificationInvocation(
                "wrong-target-framework",
                () => validate.Invoke(null, new object[] { manifest, root, true, Array.Empty<string>(), false }),
                "Advanced entry must fail closed when its TargetFramework is not exactly netstandard2.0.");
        }

        private static void AdvancedLoadWindowAndEntryIdentityFailClosed()
        {
            const string ownerId = "DTMAPI.Tests.AdvancedLoadWindow";
            string expectedOwner = ManagedModClassifier.GetExpectedHarmonyOwner(ownerId);
            var inspector = new FakeHarmonyInspector();
            var supervisor = new AdvancedHarmonySupervisor(inspector);
            ManagedModClassification supervision = CreateAdvancedClassification(expectedOwner, new string('A', 64), new string('B', 64), Guid.NewGuid().ToString("D"));
            supervisor.BeginLoad(ownerId, supervision);
            inspector.Patches.Add(Patch("wrong.load.owner", "target:load", "patch:load", typeof(Batch6AdvancedRuntimeTests).Assembly.ManifestModule.ModuleVersionId.ToString("D")));
            supervisor.BindEntryAssembly(ownerId, typeof(Batch6AdvancedRuntimeTests).Assembly);
            AssertViolation("advanced-harmony-owner-mismatch", () => supervisor.CompleteEntry(ownerId));
            ModOwnerCleanupParticipantResult cleanup = supervisor.RemoveOwner(ownerId, ModOwnerCleanupReason.EntryFailed);
            Assert(cleanup.RemainingResources == 1 && cleanup.Details.Contains("advanced-harmony-cleanup-failed", StringComparison.Ordinal),
                "Wrong-owner Harmony additions during Assembly.LoadFrom must remain visible as incomplete cleanup/restart evidence.");

            var canonicalInspector = new FakeHarmonyInspector();
            var canonicalSupervisor = new AdvancedHarmonySupervisor(canonicalInspector);
            canonicalSupervisor.BeginLoad(ownerId, supervision);
            canonicalInspector.Patches.Add(Patch(expectedOwner, "target:canonical-load", "patch:canonical-load", typeof(Batch6AdvancedRuntimeTests).Assembly.ManifestModule.ModuleVersionId.ToString("D")));
            ModOwnerCleanupParticipantResult canonicalCleanup = canonicalSupervisor.RemoveOwner(ownerId, ModOwnerCleanupReason.EntryFailed);
            Assert(canonicalCleanup.RemainingResources == 0 && canonicalInspector.UnpatchedOwners.SequenceEqual(new[] { expectedOwner }),
                "A canonical patch added before a throwing Assembly.LoadFrom must still be removed from the pre-load snapshot.");
            string lifecycleRoot = Path.Combine(DtmApiTestSession.Current.RootPath, "batch6-load-attempt-lifecycle-" + Guid.NewGuid().ToString("N"));
            var lifecycleRuntime = new DtmApiRuntime(new Batch6Host(lifecycleRoot));
            lifecycleRuntime.BeginOwnerEntry(ownerId);
            lifecycleRuntime.MarkManagedAssemblyLoadAttempt(ownerId);
            string loadFailureCleanup = lifecycleRuntime.DeactivateOwner(ownerId, ModOwnerCleanupReason.EntryFailed, shutdown: false, transactionId: string.Empty);
            Assert(lifecycleRuntime.OwnerRequiresRestart(ownerId) && loadFailureCleanup.Contains("assemblyLoaded=True", StringComparison.Ordinal),
                "An Advanced Assembly.LoadFrom attempt must conservatively require restart even if load-time canonical cleanup proves zero.");

            string root = Path.Combine(DtmApiTestSession.Current.RootPath, "batch6-entry-identity-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            string copiedEntry = Path.Combine(root, "Entry.dll");
            File.Copy(typeof(Batch6AdvancedRuntimeTests).Assembly.Location, copiedEntry);
            PortableAssemblyMetadata metadata = PortableAssemblyReferenceInspector.Inspect(copiedEntry);
            string fingerprint = ManagedModClassifier.ComputeSourceFingerprint(root);
            ManagedModClassification classification = CreateAdvancedClassification(expectedOwner, fingerprint, HashFile(copiedEntry), metadata.ModuleMvid);
            var manifest = new ManifestModel
            {
                Name = "Entry Identity",
                Author = "DTMAPI",
                Version = "1.0.0",
                UniqueID = ownerId,
                Type = "CodeMod",
                CodeModKind = "Advanced",
                EntryDll = "Entry.dll",
                EntryType = "Synthetic.Entry"
            };
            var discovered = new DiscoveredMod(manifest, root, Path.Combine(root, "manifest.json"), "Local", null, true, true, string.Empty, false, string.Empty, classification: classification);
            MethodInfo validateLoaded = typeof(DtmApiRuntime).GetMethod("ValidateAdvancedLoadedAssemblyIdentity", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Advanced loaded-assembly identity validator is unavailable.");
            AssertInvalidDataInvocation(
                "advanced-entry-identity-mismatch",
                () => validateLoaded.Invoke(null, new object[] { discovered, copiedEntry, typeof(Batch6AdvancedRuntimeTests).Assembly }),
                "Assembly.LoadFrom returning a same-MVID assembly from a sibling path must fail before type discovery.");

            string originalFingerprint = classification.SourceFingerprint;
            File.AppendAllText(copiedEntry, "tamper", new UTF8Encoding(false));
            MethodInfo validateBefore = typeof(DtmApiRuntime).GetMethod("ValidateAdvancedSourceIdentityBeforeLoad", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Advanced pre-load source identity validator is unavailable.");
            AssertInvalidDataInvocation(
                "advanced-source-fingerprint-mismatch",
                () => validateBefore.Invoke(null, new object[] { discovered, copiedEntry }),
                "Classification-to-load entry tampering must fail before Assembly.LoadFrom.");
            Assert(originalFingerprint != ManagedModClassifier.ComputeSourceFingerprint(root), "Synthetic tamper must change the immutable source fingerprint.");
        }

        private static void AdvancedSameVersionSourceDriftChangesIdentity()
        {
            var manifest = new ManifestModel
            {
                Name = "Advanced Drift",
                Author = "DTMAPI",
                Version = "1.0.0",
                UniqueID = "DTMAPI.Tests.AdvancedDrift",
                Type = "CodeMod",
                CodeModKind = "Advanced",
                EntryDll = "Entry.dll",
                EntryType = "Synthetic.Entry"
            };
            string owner = ManagedModClassifier.GetExpectedHarmonyOwner(manifest.UniqueID);
            var loaded = new DiscoveredMod(manifest, "C:\\Synthetic\\Advanced", "C:\\Synthetic\\Advanced\\manifest.json", "Local", null, true, true, string.Empty, false, string.Empty,
                classification: CreateAdvancedClassification(owner, new string('A', 64), new string('B', 64), Guid.NewGuid().ToString("D")));
            var current = new DiscoveredMod(manifest, "C:\\Synthetic\\Advanced", "C:\\Synthetic\\Advanced\\manifest.json", "Local", null, true, true, string.Empty, false, string.Empty,
                classification: CreateAdvancedClassification(owner, new string('C', 64), new string('B', 64), loaded.Classification.EntryModuleMvid));
            MethodInfo sameIdentity = typeof(DtmApiRuntime).GetMethod("HasSameSourceIdentity", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Runtime source-identity comparator is unavailable.");
            bool same = (bool)(sameIdentity.Invoke(null, new object[] { loaded, current }) ?? true);
            Assert(!same, "Same-version Advanced manifest/entry/receipt/marker drift must trigger owner deactivation and restart instead of projecting new classification over resident code.");
        }

        private static ManagedModClassification CreateAdvancedClassification(string expectedOwner, string fingerprint, string entrySha256, string entryMvid) =>
            new ManagedModClassification(
                ManagedModIdentity.AdvancedCodeMod,
                "Advanced",
                "sdk-reference-receipt-verified",
                "managed-local",
                "native-code/receipt-baseline-bound",
                "Exact; compiledBuild=23762374; installedBuild=23762374; references=2/2; drifted=0; unavailable=0",
                "restart-required-after-load",
                expectedOwner,
                true,
                "receipt.json",
                "doloctown-23762374-g2-v1",
                1,
                new string('D', 64),
                "netstandard2.0",
                "23762374",
                fingerprint,
                entrySha256,
                entryMvid);

        private static string WriteLegacyNativePackage(
            string gameRoot,
            string directoryName,
            string ownerId,
            string entryType,
            string entrySource,
            string helperSource)
        {
            string modRoot = Path.Combine(gameRoot, "Mods", directoryName);
            string contentRoot = Path.Combine(modRoot, "Content", "DTMAPI");
            Directory.CreateDirectory(contentRoot);
            File.Copy(entrySource, Path.Combine(contentRoot, Path.GetFileName(entrySource)));
            File.Copy(helperSource, Path.Combine(contentRoot, Path.GetFileName(helperSource)));
            File.WriteAllText(
                Path.Combine(modRoot, "manifest.json"),
                "{ \"Name\": \"Legacy Native " + directoryName +
                "\", \"Author\": \"Third Party Fixture\", \"Version\": \"1.0.0\", \"UniqueID\": \"" + ownerId +
                "\", \"Type\": \"CodeMod\", \"EntryDll\": \"Content/DTMAPI/" + Path.GetFileName(entrySource) +
                "\", \"EntryType\": \"" + entryType + "\" }",
                new UTF8Encoding(false));
            return modRoot;
        }

        private static string FindRepositoryRoot()
        {
            string? current = Path.GetDirectoryName(typeof(Batch6AdvancedRuntimeTests).Assembly.Location);
            while (!string.IsNullOrWhiteSpace(current))
            {
                if (File.Exists(Path.Combine(current, "PROJECT.md")) &&
                    Directory.Exists(Path.Combine(current, "src")))
                {
                    return current;
                }
                current = Directory.GetParent(current)?.FullName;
            }
            throw new DirectoryNotFoundException("Could not locate the DTMAPI repository root from the Unit test output.");
        }

        private static string GetFixtureOutput(string projectName)
        {
            string configuration = new DirectoryInfo(
                Path.GetDirectoryName(typeof(Batch6AdvancedRuntimeTests).Assembly.Location)!)
                .Parent?.Name ?? "Release";
            return Path.Combine(
                FindRepositoryRoot(),
                "tests",
                "DTMAPI.UnitTests",
                "Fixtures",
                projectName,
                "bin",
                configuration,
                "netstandard2.0");
        }

        private static void EnsureAssemblyLoaded(string path, string expectedSimpleName)
        {
            Assembly? existing = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(value =>
                value.GetName().Name?.Equals(expectedSimpleName, StringComparison.OrdinalIgnoreCase) == true);
            if (existing != null)
                return;
            Assembly loaded = Assembly.LoadFrom(path);
            Assert(
                loaded.GetName().Name?.Equals(expectedSimpleName, StringComparison.OrdinalIgnoreCase) == true,
                "Platform-reference fixture loaded an unexpected AssemblyName.");
        }

        private static int ReadStaticInt(string assemblyName, string typeName, string propertyName)
        {
            Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().Single(value =>
                value.GetName().Name?.Equals(assemblyName, StringComparison.OrdinalIgnoreCase) == true);
            Type type = assembly.GetType(typeName, throwOnError: true)!;
            PropertyInfo property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static)
                ?? throw new InvalidOperationException(typeName + "." + propertyName + " is unavailable.");
            return (int)(property.GetValue(null) ?? -1);
        }

        private static void AppendProbeByte(string path, byte value)
        {
            using var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read);
            stream.WriteByte(value);
        }

        private static string HashFile(string path)
        {
            using FileStream stream = File.OpenRead(path);
            using SHA256 hash = SHA256.Create();
            return Convert.ToHexString(hash.ComputeHash(stream));
        }

        private static void AssertClassificationInvocation(string code, Action action, string message)
        {
            try
            {
                action();
            }
            catch (TargetInvocationException ex) when (ex.InnerException is ManagedModClassificationException failure && failure.Code == code)
            {
                return;
            }
            throw new InvalidOperationException(message);
        }

        private static void AssertClassificationFailure(string code, Action action, string message)
        {
            try
            {
                action();
            }
            catch (ManagedModClassificationException failure) when (failure.Code == code)
            {
                return;
            }
            throw new InvalidOperationException(message);
        }

        private static void AssertInvalidDataInvocation(string code, Action action, string message)
        {
            try
            {
                action();
            }
            catch (TargetInvocationException ex) when (ex.InnerException is InvalidDataException failure && failure.Message.Contains(code, StringComparison.Ordinal))
            {
                return;
            }
            throw new InvalidOperationException(message);
        }

        private static void ManagerProjectsAdvancedClassificationWithoutPublicAbiExpansion()
        {
            const string expectedOwner = "dtmapi.mod.dtmapi.tests.advancedmanager";
            ManagerModRow row = CreateAdvancedManagerRow();
            string line = ManagerPageRowFormatter.FormatModRow(row);
            Assert(row.ManagedIdentity == "Advanced CodeMod" && row.AdvancedReferenceVerified, "Manager must preserve the Core Advanced classification without extending IDtmModStatusInfo.");
            foreach (string token in new[]
            {
                "identity=Advanced CodeMod",
                "declared=Advanced",
                "effective=Advanced",
                "provenance=sdk-reference-receipt-verified",
                "placement=managed-local",
                "nativeRisk=native-code/receipt-baseline-bound",
                "gameCompatibility=Exact; compiledBuild=23762374; installedBuild=23762374; references=2/2; drifted=0; unavailable=0",
                "restart=restart-required-after-load",
                "harmonyOwner=" + expectedOwner,
                "referenceReceipt=verified|policy:doloctown-23762374-g2-v1@1|tfm:netstandard2.0|gameBuild:23762374"
            })
            {
                Assert(line.Contains(token, StringComparison.Ordinal), "Manager Advanced projection is missing: " + token);
            }
        }

        private static ManagerModRow CreateAdvancedManagerRow()
        {
            const string expectedOwner = "dtmapi.mod.dtmapi.tests.advancedmanager";
            var classification = new ManagedModClassification(
                ManagedModIdentity.AdvancedCodeMod,
                "Advanced",
                "sdk-reference-receipt-verified",
                "managed-local",
                "native-code/receipt-baseline-bound",
                "Exact; compiledBuild=23762374; installedBuild=23762374; references=2/2; drifted=0; unavailable=0",
                "restart-required-after-load",
                expectedOwner,
                true,
                "Content/DTMAPI/dtmapi-advanced-references.json",
                "doloctown-23762374-g2-v1",
                1,
                new string('A', 64),
                "netstandard2.0",
                "23762374");
            var status = new DtmModStatusInfo(
                "DTMAPI.Tests.AdvancedManager",
                "Advanced Manager Fixture",
                "1.0.0",
                "CodeMod",
                "Local",
                string.Empty,
                true,
                false,
                string.Empty,
                "Fixture.dll",
                "Fixture.ModEntry",
                true,
                "loaded",
                "loaded",
                "Loaded by synthetic test.",
                "manifest.json",
                "Mods/DTMAPI.Tests.AdvancedManager",
                classification);
            return ManagerModRow.FromMod(status);
        }

        private static void ManagerProjectsLegacyAuthorOwnershipAndRestartBoundary()
        {
            ManagedModClassification classification = ManagedModClassifier.ClassifyDeclaredIdentity(new ManifestModel
            {
                Name = "Legacy Manager Fixture",
                Author = "Third Party",
                Version = "1.0.0",
                UniqueID = "DTMAPI.Tests.LegacyManager",
                Type = "CodeMod",
                EntryDll = "Legacy.dll"
            });
            var status = new DtmModStatusInfo(
                "DTMAPI.Tests.LegacyManager",
                "Legacy Manager Fixture",
                "1.0.0",
                "CodeMod",
                "Workshop",
                string.Empty,
                true,
                true,
                string.Empty,
                "Legacy.dll",
                "Legacy.Entry",
                true,
                "loaded",
                "loaded",
                "Loaded by synthetic test.",
                "manifest.json",
                "Mods/DTMAPI.Tests.LegacyManager",
                classification);

            ManagerModRow row = ManagerModRow.FromMod(status);
            string diagnostic = ManagerPageRowFormatter.FormatModRow(row);
            string[] details = ManagerPageRowFormatter.FormatModDetailLines(row).ToArray();
            Assert(
                row.ManagedIdentity == "Third-party native compatibility CodeMod" &&
                row.EffectiveKind == "LegacyNativeCompatibility" &&
                diagnostic.Contains("nativeRisk=native-code/third-party-author-managed", StringComparison.Ordinal) &&
                details.Any(line => line.Contains("third-party author manages native hooks", StringComparison.Ordinal)) &&
                details.Any(line => line.Contains("does not claim hot cleanup", StringComparison.Ordinal)),
                "Manager must label omitted-kind native compatibility as third-party author-managed/restart-required rather than Strict, Advanced, or safely hot-unloadable.");
            ManagerAdvancedDiagnostics mixed = ManagerAdvancedDiagnostics.From(
                null,
                new[] { CreateAdvancedManagerRow(), row },
                Array.Empty<ManagerHookRow>(),
                Array.Empty<ManagerFeatureRow>());
            Assert(
                mixed.AdvancedModCount == 1 &&
                mixed.AdvancedReferenceVerifiedCount == 1 &&
                mixed.NativeRiskModCount == 2,
                "Manager mixed Advanced + legacy summary must count the one receipt-bound Advanced product without treating the legacy author-managed row as Advanced.");
        }

        private static AdvancedHarmonyPatchRecord Patch(string owner, string target, string patch, string moduleMvid) =>
            new AdvancedHarmonyPatchRecord(owner, target, "prefix", patch, moduleMvid);

        private static void AssertViolation(string code, Action action)
        {
            try
            {
                action();
            }
            catch (AdvancedHarmonyViolationException ex) when (ex.Code == code)
            {
                return;
            }
            throw new InvalidOperationException("Expected Advanced Harmony failure code " + code + ".");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private sealed class FakeHarmonyInspector : IAdvancedHarmonyInspector
        {
            public List<AdvancedHarmonyPatchRecord> Patches { get; } = new List<AdvancedHarmonyPatchRecord>();
            public List<string> UnpatchedOwners { get; } = new List<string>();

            public AdvancedHarmonySnapshot Capture() => new AdvancedHarmonySnapshot(true, Patches.ToArray(), string.Empty);

            public AdvancedHarmonyUnpatchResult UnpatchOwner(string harmonyOwner)
            {
                UnpatchedOwners.Add(harmonyOwner);
                int before = Patches.Count;
                Patches.RemoveAll(patch => patch.Owner.Equals(harmonyOwner, StringComparison.Ordinal));
                return new AdvancedHarmonyUnpatchResult(true, before - Patches.Count, 0, "fake owner-only cleanup");
            }
        }

        private sealed class UnavailableHarmonyInspector : IAdvancedHarmonyInspector
        {
            public AdvancedHarmonySnapshot Capture() => AdvancedHarmonySnapshot.Unavailable("synthetic unavailable");
            public AdvancedHarmonyUnpatchResult UnpatchOwner(string harmonyOwner) => new AdvancedHarmonyUnpatchResult(false, 0, 0, "unavailable");
        }

        private sealed class AdvancedSdk01PackageFixture
        {
            public AdvancedSdk01PackageFixture(
                string rootPath,
                string manifestPath,
                string markerPath,
                ManifestModel manifest)
            {
                RootPath = rootPath;
                ManifestPath = manifestPath;
                MarkerPath = markerPath;
                Manifest = manifest;
            }

            public string RootPath { get; }
            public string ManifestPath { get; }
            public string MarkerPath { get; }
            public ManifestModel Manifest { get; }
        }

        private sealed class Batch6Host : IRuntimeHost, ILegacyDevelopmentModSourceTestHost
        {
            public Batch6Host(string gamePath)
            {
                GamePath = gamePath;
                PluginPath = Path.Combine(gamePath, "BepInEx", "plugins", "DTMAPI");
                Directory.CreateDirectory(PluginPath);
            }

            public string GamePath { get; }
            public string PluginPath { get; }
            public string HostName => "Batch6AdvancedRuntimeTests";
            public bool IncludeLegacyDevelopmentModSourceForTests => true;
            public void Log(string message) { }
            public void LogWarning(string message) { }
            public void LogError(string message, Exception? exception = null) { }
        }
    }
}
