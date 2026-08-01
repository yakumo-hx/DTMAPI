using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Diagnostics;
using DTMAPI.Core.Runtime;
using DTMAPI.Testing;

namespace DTMAPI.UnitTests
{
    internal static class Batch5ManifestContractTests
    {
        internal static void Run()
        {
            RequiredAliasCanonicalizationRejectsConflicts();
            UpdateKeysAreExplicitlyDiagnosedAsInactiveMetadata();
            ScannerAndRegistryDiagnosticsRemainBoundedWithExactTotals();
            RegistryDiffProjectionRemainsBoundedWithExactTotals();
            DiagnosticScalarsAndDuplicateDetailsAreBoundedEndToEnd();
        }

        private static void RequiredAliasCanonicalizationRejectsConflicts()
        {
            string root = NewRoot("manifest-required-alias");
            try
            {
                string matchingPath = Path.Combine(root, "matching.json");
                File.WriteAllText(
                    matchingPath,
                    "{ \"Name\": \"Matching\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.MatchingAliases\", \"Dependencies\": [{ \"UniqueID\": \"DTMAPI.Tests.Optional\", \"Required\": false, \"IsRequired\": false }] }");
                ManifestModel matching = new ManifestReader().Read(matchingPath);
                Assert(!matching.DependencyModels.Single().Required, "Matching Required/IsRequired aliases should normalize to the canonical Required value.");

                string conflictPath = Path.Combine(root, "conflict.json");
                File.WriteAllText(
                    conflictPath,
                    "{ \"Name\": \"Conflict\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.ConflictingAliases\", \"Dependencies\": [{ \"UniqueID\": \"DTMAPI.Tests.Dependency\", \"Required\": true, \"IsRequired\": false }] }");

                bool rejected = false;
                try
                {
                    new ManifestReader().Read(conflictPath);
                }
                catch (SerializationException ex)
                {
                    rejected = ex.Message.Contains("conflicting Required and IsRequired", StringComparison.Ordinal);
                }
                Assert(rejected, "Conflicting dependency aliases must be rejected instead of silently weakening the canonical Required value.");
            }
            finally
            {
                DeleteRoot(root);
            }
        }

        private static void UpdateKeysAreExplicitlyDiagnosedAsInactiveMetadata()
        {
            string root = NewRoot("manifest-update-keys");
            string? previousPersistentRoot = Environment.GetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT");
            try
            {
                string modRoot = Path.Combine(root, "Mods", "UpdateKeysMod");
                Directory.CreateDirectory(modRoot);
                File.WriteAllText(
                    Path.Combine(modRoot, "manifest.json"),
                    "{ \"Name\": \"Update Keys\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.UpdateKeys\", \"Type\": \"ContentPack\", \"UpdateKeys\": [\"Nexus:123\"] }");
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", Path.Combine(root, "Persistent"));

                var scanner = new ModScanner(new RuntimePaths(root, root));
                scanner.Discover();

                Assert(
                    scanner.Warnings.Any(warning =>
                        warning.Contains("DTMAPI.Tests.UpdateKeys", StringComparison.Ordinal) &&
                        warning.Contains("inactive schema-only metadata", StringComparison.Ordinal)),
                    "UpdateKeys must have an explicit inactive/schema-only diagnostic until an update service exists.");
            }
            finally
            {
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", previousPersistentRoot);
                DeleteRoot(root);
            }
        }

        private static void ScannerAndRegistryDiagnosticsRemainBoundedWithExactTotals()
        {
            string root = NewRoot("manifest-bounded-diagnostics");
            string? previousPersistentRoot = Environment.GetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT");
            try
            {
                string modsRoot = Path.Combine(root, "Mods");
                Directory.CreateDirectory(modsRoot);
                const int invalidCount = 180;
                const int warningSourceCount = 180;
                for (int index = 0; index < invalidCount; index++)
                {
                    string modRoot = Path.Combine(modsRoot, "Invalid-" + index);
                    Directory.CreateDirectory(modRoot);
                    File.WriteAllText(Path.Combine(modRoot, "manifest.json"), "{ not-json");
                }
                for (int index = 0; index < warningSourceCount; index++)
                {
                    string modRoot = Path.Combine(modsRoot, "Warning-" + index);
                    Directory.CreateDirectory(modRoot);
                    File.WriteAllText(
                        Path.Combine(modRoot, "manifest.json"),
                        "{ \"Name\": \"Warning " + index + "\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.Warning" + index + "\", \"Type\": \"ContentPack\", \"UpdateKeys\": [\"Nexus:" + index + "\"] }");
                }
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", Path.Combine(root, "Persistent"));

                var scanner = new ModScanner(new RuntimePaths(root, root));
                IReadOnlyList<DiscoveredMod> discovered = scanner.Discover();
                ModScannerDiagnosticTotals totals = scanner.DiagnosticTotals;

                Assert(totals.ErrorCount == invalidCount, "Scanner error total must remain exact after its sample capacity is reached.");
                Assert(scanner.Errors.Count == ModScanner.MaxDiagnosticSamplesPerSeverity, "Scanner must retain only the fixed error sample capacity.");
                Assert(totals.ErrorTrimmedCount == invalidCount - ModScanner.MaxDiagnosticSamplesPerSeverity, "Scanner error trimmed scalar must be exact.");
                Assert(totals.WarningCount > ModScanner.MaxDiagnosticSamplesPerSeverity, "Warning fixture must exceed the scanner sample capacity.");
                Assert(scanner.Warnings.Count == ModScanner.MaxDiagnosticSamplesPerSeverity, "Scanner must retain only the fixed warning sample capacity.");
                Assert(totals.WarningTrimmedCount == totals.WarningCount - scanner.Warnings.Count, "Scanner warning trimmed scalar must be exact.");

                ContentManifestRegistrySnapshot snapshot = new ContentManifestRegistry().Build(
                    discovered,
                    Array.Empty<DiscoveredMod>(),
                    scanner.Errors,
                    scanner.Warnings,
                    totals,
                    "bounded-diagnostic-test");

                Assert(snapshot.DiagnosticCount == totals.ErrorCount + totals.WarningCount, "Registry diagnostic total must include scanner entries omitted from the retained samples.");
                Assert(snapshot.Diagnostics.Count == ContentManifestRegistry.MaxDiagnosticSamples, "Registry must retain only its fixed diagnostic sample capacity.");
                Assert(snapshot.DiagnosticTrimmedCount == snapshot.DiagnosticCount - snapshot.Diagnostics.Count, "Registry diagnostic trimmed scalar must be exact.");
                Assert(snapshot.BadManifestCount == invalidCount, "Bad-manifest count must remain exact beyond the retained projection.");
                Assert(snapshot.FormatSummary().Contains("diagnosticsTrimmed=" + snapshot.DiagnosticTrimmedCount, StringComparison.Ordinal), "Runtime summary must expose the exact trimmed diagnostic count.");
            }
            finally
            {
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", previousPersistentRoot);
                DeleteRoot(root);
            }
        }

        private static void RegistryDiffProjectionRemainsBoundedWithExactTotals()
        {
            const int loadedOnlyCount = 200;
            var loadedOnly = new DiscoveredMod[loadedOnlyCount];
            for (int index = 0; index < loadedOnly.Length; index++)
            {
                var manifest = new ManifestModel
                {
                    Name = "Loaded " + index,
                    Author = "DTMAPI",
                    Version = "1.0.0",
                    UniqueID = "DTMAPI.Tests.LoadedOnly" + index,
                    Type = "ContentPack"
                };
                loadedOnly[index] = new DiscoveredMod(
                    manifest,
                    "missing-root-" + index,
                    "missing-manifest-" + index,
                    "Local",
                    null,
                    true,
                    true,
                    string.Empty,
                    false,
                    string.Empty);
            }

            ContentManifestRegistrySnapshot snapshot = new ContentManifestRegistry().Build(
                Array.Empty<DiscoveredMod>(),
                loadedOnly,
                Array.Empty<string>(),
                Array.Empty<string>(),
                "bounded-diff-test");

            Assert(snapshot.DiffCount == loadedOnlyCount, "Registry diff total must remain exact beyond the retained projection.");
            Assert(snapshot.Diffs.Count == ContentManifestRegistry.MaxDiffSamples, "Registry must retain only its fixed diff sample capacity.");
            Assert(snapshot.DiffTrimmedCount == loadedOnlyCount - ContentManifestRegistry.MaxDiffSamples, "Registry diff trimmed scalar must be exact.");
            Assert(snapshot.FormatDiffSummary().Contains("diffs=" + loadedOnlyCount, StringComparison.Ordinal), "Runtime diff summary must report the exact total.");
        }

        private static void DiagnosticScalarsAndDuplicateDetailsAreBoundedEndToEnd()
        {
            string root = NewRoot("manifest-bounded-scalars");
            string? previousPersistentRoot = Environment.GetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT");
            try
            {
                string modsRoot = Path.Combine(root, "Mods");
                Directory.CreateDirectory(modsRoot);
                string longId = "DTMAPI.Tests." + new string('X', 8_000);
                string longRoot = Path.Combine(modsRoot, "LongWarning");
                Directory.CreateDirectory(longRoot);
                File.WriteAllText(
                    Path.Combine(longRoot, "manifest.json"),
                    "{ \"Name\": \"Long\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"" + longId + "\", \"Type\": \"ContentPack\", \"UpdateKeys\": [\"Nexus:1\"] }");

                const int duplicateCandidates = 40;
                for (int index = 0; index < duplicateCandidates; index++)
                {
                    string candidateRoot = Path.Combine(modsRoot, "Duplicate-" + index.ToString("00"));
                    Directory.CreateDirectory(candidateRoot);
                    File.WriteAllText(
                        Path.Combine(candidateRoot, "manifest.json"),
                        "{ \"Name\": \"Duplicate\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.BoundedDuplicate\", \"Type\": \"ContentPack\" }");
                }
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", Path.Combine(root, "Persistent"));

                var scanner = new ModScanner(
                    new RuntimePaths(root, root),
                    NativeWorkshopSubscriptionSnapshot.Captured(
                        "batch5-bounded-duplicate-test",
                        string.Empty,
                        Array.Empty<NativeWorkshopSubscription>()));
                scanner.Discover();
                string duplicate = scanner.Warnings.Single(value => value.Contains("Duplicate UniqueID DTMAPI.Tests.BoundedDuplicate", StringComparison.Ordinal));
                Assert(scanner.Warnings.All(value => value.Length <= BoundedDiagnosticScalar.DetailsChars), "Every retained scanner diagnostic scalar must have a fixed character bound.");
                Assert(scanner.DiagnosticTotals.TrimmedBytes > 0, "Scanner diagnostics must report exact scalar bytes trimmed from a hostile long value.");
                Assert(duplicate.Contains("omittedCandidates=23", StringComparison.Ordinal), "Duplicate diagnostics must sample only 16 ignored candidates and report the exact omitted count.");

                string oversized = new string('E', 10_000);
                ContentManifestRegistrySnapshot diagnosticSnapshot = new ContentManifestRegistry().Build(
                    Array.Empty<DiscoveredMod>(),
                    Array.Empty<DiscoveredMod>(),
                    new[] { oversized },
                    Array.Empty<string>(),
                    "long-scalar-test");
                Assert(diagnosticSnapshot.Diagnostics.Single().Details.Length <= BoundedDiagnosticScalar.DetailsChars, "Registry diagnostics must defensively bound caller-provided scalars.");
                Assert(diagnosticSnapshot.DiagnosticTrimmedBytes > 0, "Registry diagnostics must expose scalar trimming bytes.");

                var longDiffManifest = new ManifestModel
                {
                    Name = "Long Diff",
                    Author = "DTMAPI",
                    Version = "1.0.0",
                    UniqueID = new string('D', 10_000),
                    Type = "ContentPack"
                };
                var longDiff = new DiscoveredMod(longDiffManifest, "root", "manifest", "Local", null, true, true, string.Empty, false, string.Empty);
                ContentManifestRegistrySnapshot diffSnapshot = new ContentManifestRegistry().Build(
                    Array.Empty<DiscoveredMod>(),
                    new[] { longDiff },
                    Array.Empty<string>(),
                    Array.Empty<string>(),
                    "long-diff-test");
                Assert(diffSnapshot.Diffs.Single().Length <= BoundedDiagnosticScalar.DetailsChars && diffSnapshot.DiffTrimmedBytes > 0, "Registry diff samples must be bounded with trimming bytes reported.");
            }
            finally
            {
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", previousPersistentRoot);
                DeleteRoot(root);
            }
        }

        private static string NewRoot(string name)
        {
            string root = Path.Combine(
                DtmApiTestSession.Current.RootPath,
                "batch5-manifest-contract",
                name + "-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return root;
        }

        private static void DeleteRoot(string root)
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
