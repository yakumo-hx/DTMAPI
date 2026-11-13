using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;
using DTMAPI.Testing;

namespace DTMAPI.UnitTests
{
    internal static class ManifestContractTests
    {
        internal static void Run()
        {
            RequiredAliasCanonicalizationRejectsConflicts();
            UpdateKeysAreExplicitlyDiagnosedAsInactiveMetadata();
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
                string modRoot = Path.Combine(root, "Persistent", "MODS", "UpdateKeysMod");
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

        private static string NewRoot(string name)
        {
            string root = Path.Combine(
                DtmApiTestSession.Current.RootPath,
                "manifest-contract",
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
