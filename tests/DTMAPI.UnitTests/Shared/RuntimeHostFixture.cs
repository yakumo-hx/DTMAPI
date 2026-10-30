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
            Directory.CreateDirectory(Path.Combine(dir, "Mods"));
            return dir;
        }

        private sealed class FakeHost : IRuntimeHost, ILegacyDevelopmentModSourceTestHost
        {
            public FakeHost(string gamePath, bool includeLegacyDevelopmentModSourceForTests = true)
            {
                GamePath = gamePath;
                PluginPath = Path.Combine(gamePath, "BepInEx", "plugins");
                IncludeLegacyDevelopmentModSourceForTests = includeLegacyDevelopmentModSourceForTests;
            }

            public string GamePath { get; }
            public string PluginPath { get; }
            public string HostName => "UnitTest";
            public bool IncludeLegacyDevelopmentModSourceForTests { get; }
            public List<string> Logs { get; } = new List<string>();
            public List<string> Warnings { get; } = new List<string>();
            public void Log(string message) => Logs.Add(message ?? string.Empty);
            public void LogWarning(string message) => Warnings.Add(message ?? string.Empty);
            public void LogError(string message, Exception? exception = null) { }
        }
    }
}
