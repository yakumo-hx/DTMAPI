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
using DTMAPI.GameBridge.DolocTown;

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {

        private static void StageCompatibilityHostFixture(string gamePath)
        {
            const string fileName = "DTMAPI.GameBridge.DolocTown.Compatibility.dll";
            string source = Path.Combine(AppContext.BaseDirectory, "CompatibilityHostFixture", fileName);
            if (!File.Exists(source))
                throw new FileNotFoundException("The Unit build did not stage the Compatibility Host fixture.", source);
            string componentDirectory = Path.Combine(gamePath, "DTMAPI", "components", "compatibility");
            Directory.CreateDirectory(componentDirectory);
            string target = Path.Combine(componentDirectory, fileName);
            File.Copy(source, target, overwrite: true);
            string sha256;
            using (FileStream stream = File.OpenRead(target))
            using (SHA256 hash = SHA256.Create())
                sha256 = BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", string.Empty);
            var info = new FileInfo(target);
            var receipt = new
            {
                OptionalComponents = new[]
                {
                    new
                    {
                        ComponentId = "gamebridge-compatibility-host",
                        Distribution = "dormant-shipped",
                        LoadPolicy = "first-frozen-abi-call",
                        RelativePath = "DTMAPI/components/compatibility/" + fileName,
                        Length = info.Length,
                        Sha256 = sha256,
                        AssemblyName = "DTMAPI.GameBridge.DolocTown.Compatibility",
                        AssemblyVersion = AssemblyName.GetAssemblyName(target).Version?.ToString() ?? string.Empty,
                        FileVersion = FileVersionInfo.GetVersionInfo(target).FileVersion ?? string.Empty,
                        TargetFramework = "netstandard2.0",
                        DefaultLoadState = "dormant",
                        IncludedInDownloadPackage = true
                    }
                }
            };
            File.WriteAllText(Path.Combine(gamePath, "DTMAPI", "release-manifest.json"), JsonSerializer.Serialize(receipt), new UTF8Encoding(false));
        }

        private static int CountGameBridgeOwnerResourcesForTest(DolocTownGameBridge bridge, string ownerId)
        {
            MethodInfo count = typeof(DolocTownGameBridge).GetMethod("CountGameBridgeOwnerResources", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("DolocTownGameBridge.CountGameBridgeOwnerResources should expose authoritative owner-root counting.");
            return (int)(count.Invoke(bridge, new object[] { ownerId }) ?? 0);
        }
    }
}
