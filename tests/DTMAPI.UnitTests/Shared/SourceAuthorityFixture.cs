#pragma warning disable CS0618 // Frozen compatibility contracts are intentionally exercised.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;

using DTMAPI.Core.Runtime;

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {

        private static void WriteAuthorSourceSelectionState(
            string gameDir,
            string uniqueId,
            string mode,
            string sourcePath,
            bool playerReproductionActive,
            string? storedGameRoot = null,
            string? expectedTreeSha256 = null)
        {
            string statePath = AuthorSourceStateStore.GetSourceStatePath(gameDir);
            Directory.CreateDirectory(Path.GetDirectoryName(statePath)!);
            string treeSha256 = expectedTreeSha256 ?? (Directory.Exists(sourcePath) ? AuthorFileTreeDigest.Compute(sourcePath) : string.Empty);
            File.WriteAllText(
                statePath,
                "{ \"schemaVersion\": 1, \"gameRoot\": \"" + JsonEscapeForTest(storedGameRoot ?? gameDir) +
                "\", \"playerReproductionActive\": " + (playerReproductionActive ? "true" : "false") +
                ", \"reproductionSnapshotId\": \"unit-test-snapshot\", \"selections\": [{ \"uniqueId\": \"" + JsonEscapeForTest(uniqueId) +
                "\", \"mode\": \"" + JsonEscapeForTest(mode) + "\", \"sourcePath\": \"" + JsonEscapeForTest(sourcePath) +
                "\", \"expectedTreeSha256\": \"" + JsonEscapeForTest(treeSha256) + "\" }] }");
        }

        private static string JsonEscapeForTest(string value) => value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"");

        private static void WriteSourceAuthorityContentPack(string root, string uniqueId, string name)
        {
            Directory.CreateDirectory(root);
            File.WriteAllText(
                Path.Combine(root, "manifest.json"),
                "{ \"Name\": \"" + name + "\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"" + uniqueId + "\", \"Type\": \"ContentPack\" }");
        }

        private static bool PathsEqualForTest(string left, string right) => string.Equals(
            Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase);
    }
}
