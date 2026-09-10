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

        private static string WriteAtomicProbePackage(string gameDir, string ownerId, string dependenciesJson)
        {
            string modDir = Path.Combine(gameDir, "Mods", ownerId.Replace('.', '_'));
            Directory.CreateDirectory(modDir);
            Assembly probeAssembly = typeof(AtomicOwnerProbeMod).Assembly;
            string probeTargetFramework = probeAssembly
                .GetCustomAttribute<System.Runtime.Versioning.TargetFrameworkAttribute>()?
                .FrameworkName ?? string.Empty;
            Assert(!ReferenceEquals(probeAssembly, typeof(Program).Assembly) &&
                   probeTargetFramework.Equals(".NETStandard,Version=v2.0", StringComparison.Ordinal),
                "Runtime CodeMod probes must remain in their independent netstandard2.0 fixture assembly, never the net8 unit-test host.");
            string assemblyPath = probeAssembly.Location;
            string assemblyName = Path.GetFileName(assemblyPath);
            File.Copy(assemblyPath, Path.Combine(modDir, assemblyName), overwrite: true);
            File.WriteAllText(
                Path.Combine(modDir, "manifest.json"),
                "{ \"Name\": \"Atomic Owner Probe\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"" + ownerId + "\", \"EntryDll\": \"" + assemblyName + "\", \"EntryType\": \"" + (typeof(AtomicOwnerProbeMod).FullName ?? nameof(AtomicOwnerProbeMod)) + "\", \"Type\": \"CodeMod\", \"CodeModKind\": \"Strict\"" + dependenciesJson + " }");
            return modDir;
        }

        private static string InvokeFailedOwnerCleanup(DtmApiRuntime runtime, string ownerId)
        {
            MethodInfo cleanup = typeof(DtmApiRuntime).GetMethod("CleanupFailedCodeModOwner", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("CleanupFailedCodeModOwner should exist.");
            return (string)(cleanup.Invoke(runtime, new object[] { ownerId, string.Empty }) ?? string.Empty);
        }
    }
}
