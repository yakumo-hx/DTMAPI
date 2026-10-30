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
            Directory.CreateDirectory(Path.GetDirectoryName(statePath) ?? throw new InvalidOperationException("Author source state directory is unavailable."));
            string treeSha256 = expectedTreeSha256 ?? (Directory.Exists(sourcePath) ? AuthorFileTreeDigest.Compute(sourcePath) : string.Empty);
            File.WriteAllText(
                statePath,
                "{ \"schemaVersion\": 1, \"gameRoot\": \"" + JsonEscapeForTest(storedGameRoot ?? gameDir) +
                "\", \"playerReproductionActive\": " + (playerReproductionActive ? "true" : "false") +
                ", \"reproductionSnapshotId\": \"unit-test-snapshot\", \"selections\": [{ \"uniqueId\": \"" + JsonEscapeForTest(uniqueId) +
                "\", \"mode\": \"" + JsonEscapeForTest(mode) + "\", \"sourcePath\": \"" + JsonEscapeForTest(sourcePath) +
                "\", \"expectedTreeSha256\": \"" + JsonEscapeForTest(treeSha256) + "\" }] }");
        }

        private static string JsonEscapeForTest(string value) => (value ?? string.Empty)
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"");

        private static void WriteAuthorSessionDescriptor(
            string gameDir,
            string sessionId,
            string token,
            string pipeName,
            TimeSpan? lifetime = null)
        {
            string path = AuthorSourceStateStore.GetAuthorSessionPath(gameDir);
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? throw new InvalidOperationException("Author session state directory is unavailable."));
            DateTimeOffset now = DateTimeOffset.UtcNow;
            File.WriteAllText(
                path,
                "{ \"schemaVersion\": 1, \"gameRoot\": \"" + JsonEscapeForTest(gameDir) +
                "\", \"runtimeVersion\": \"" + AuthorSessionProtocol.LegacyWireVersion +
                "\", \"sessionId\": \"" + sessionId +
                "\", \"token\": \"" + token +
                "\", \"pipeName\": \"" + pipeName +
                "\", \"createdAtUtc\": \"" + now.AddSeconds(-1).ToString("O", CultureInfo.InvariantCulture) +
                "\", \"expiresAtUtc\": \"" + now.Add(lifetime ?? TimeSpan.FromMinutes(5)).ToString("O", CultureInfo.InvariantCulture) + "\" }");
        }

        private static IEventsHelper CreateEventsProxy(DtmApiRuntime runtime, string owner)
        {
            PropertyInfo? eventsProperty = typeof(DtmApiRuntime).GetProperty("Events", BindingFlags.Instance | BindingFlags.NonPublic);
            object events = eventsProperty?.GetValue(runtime) ?? throw new InvalidOperationException("Runtime events should exist.");
            MethodInfo createProxy = events.GetType().GetMethod("CreateProxy", BindingFlags.Instance | BindingFlags.Public) ?? throw new InvalidOperationException("CreateProxy should exist.");
            return (IEventsHelper)(createProxy.Invoke(events, new object[] { owner }) ?? throw new InvalidOperationException("CreateProxy returned null."));
        }
    }
}
