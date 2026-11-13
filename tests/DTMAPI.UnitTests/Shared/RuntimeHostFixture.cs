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
using System.Text.Json.Nodes;
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

        private static string NewTempGameDir()
        {
            string dir = Path.Combine(Path.GetTempPath(), "DTMAPI-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            return dir;
        }

        private static string CreateEnabledFixtureModDirectory(string gameDir, string folderName, params string[] relativePath)
        {
            string persistentRoot = Environment.GetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT")
                ?? throw new InvalidOperationException("UseTempPersistentRoot must precede an enabled package fixture.");
            string packageRoot = Path.Combine(persistentRoot, "MODS", folderName);
            Directory.CreateDirectory(packageRoot);
            string statePath = Path.Combine(persistentRoot, "SAVE", "mod_infos.json");
            Directory.CreateDirectory(Path.GetDirectoryName(statePath)!);
            JsonObject state = File.Exists(statePath)
                ? JsonNode.Parse(File.ReadAllText(statePath))!.AsObject()
                : new JsonObject();
            JsonObject entries;
            if (state["modInfos"] is JsonObject existing)
                entries = existing;
            else
            {
                entries = new JsonObject();
                state["modInfos"] = entries;
            }
            string officialId = "Local." + folderName;
            entries[officialId] = new JsonObject
            {
                ["id"] = officialId,
                ["enabled"] = true,
                ["priority"] = -1,
                ["source"] = "Local",
                ["title"] = "DTMAPI Test"
            };
            File.WriteAllText(statePath, state.ToJsonString());
            string result = Path.Combine(new[] { packageRoot }.Concat(relativePath).ToArray());
            Directory.CreateDirectory(result);
            return result;
        }

        private static void SetFixtureModEnabled(string packageRoot, bool enabled)
        {
            string persistentRoot = Environment.GetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT")
                ?? throw new InvalidOperationException("UseTempPersistentRoot must precede an enabled package fixture.");
            string statePath = Path.Combine(persistentRoot, "SAVE", "mod_infos.json");
            JsonObject state = JsonNode.Parse(File.ReadAllText(statePath))!.AsObject();
            string officialId = "Local." + Path.GetFileName(packageRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            state["modInfos"]![officialId]!["enabled"] = enabled;
            File.WriteAllText(statePath, state.ToJsonString());
        }

        private static IEventsHelper CreateEventsProxy(DtmApiRuntime runtime, string owner)
        {
            object events = typeof(DtmApiRuntime).GetProperty("Events", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(runtime)!;
            return (IEventsHelper)events.GetType().GetMethod("CreateProxy", BindingFlags.Instance | BindingFlags.Public)!.Invoke(events, new object[] { owner })!;
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
            public List<string> Logs { get; } = new List<string>();
            public List<string> Warnings { get; } = new List<string>();
            public void Log(string message) => Logs.Add(message);
            public void LogWarning(string message) => Warnings.Add(message);
            public void LogError(string message, Exception? exception = null) { }
        }
    }
}
