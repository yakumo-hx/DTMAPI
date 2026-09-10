using System;
using System.IO;
using System.Reflection;
using DTMAPI.Core.Manifesting;
using DTMAPI.GameBridge.DolocTown;
using DTMAPI.Internal.Authoring;
using DTMAPI.Testing;

namespace DTMAPI.UnitTests
{
    internal static class PlatformSdkTargetRuntimeTests
    {
        public static void RunAll()
        {
            AuthorApiTargetCatalog catalog = AuthorApiTargetCatalog.Current;
            Require(catalog.DefaultTarget == "0.7.0", "Default selects the complete M3 target.");
            Require(catalog.GetAvailable("0.5.5").TargetFramework == "netstandard2.0", "Old target must keep Unity Mono-compatible references.");
            Require(catalog.TryGet("0.6.3", out AuthorApiTarget current) && current.State == "available" && current.ContractSha256.Length == 64 && current.TargetFramework == "netstandard2.0", "Internal target binds its own Mono-compatible payload.");
            Require(System.Linq.Enumerable.Contains(current.Capabilities, "reflection/1"), "The new internal target includes validated reflection.");
            AuthorApiTarget complete = catalog.GetAvailable("0.7.0");
            Require(complete.ContractSha256.Length == 64 && System.Linq.Enumerable.Contains(complete.Capabilities, "package-dependencies/1") &&
                System.Linq.Enumerable.Contains(complete.Capabilities, "native-contract/1"), "Complete target binds the M3 dependency and native contracts.");
            try { catalog.GetAvailable("0.8.0"); throw new Exception("Unknown API target was accepted."); }
            catch (InvalidDataException) { }
            Require(!catalog.TryValidatePackageTarget("0.5.5", "0.1.0", "0.5.4", out _, out string lowFloorReason) && lowFloorReason.Contains("0.5.4", StringComparison.Ordinal),
                "New writers must reject and identify a minimum below their compiler target.");
            Require(catalog.TryValidateReadablePackageTarget("0.5.5", "0.1.0", "0.5.4", out string readCode, out string readReason) && readCode.Length == 0 && readReason.Length == 0,
                "Historical issued package readers must retain that exact compatibility combination without stale errors.");
            string root = Path.Combine(DtmApiTestSession.Current.RootPath, "platform-sdk-targets");
            PlatformPackageTargetMatrix.Run(root, (packageRoot, name, accepted) =>
            {
                string path = Path.Combine(packageRoot, "Content", "DTMAPI", "manifest.json");
                ManifestModel manifest = new ManifestReader().Read(path);
                bool classified;
                try { classified = new ManagedModClassifier(packageRoot).Classify(manifest, packageRoot, path, "Local", true, null).IsContentPack; }
                catch (Exception ex) when (ex.Message.Contains("package-marker-invalid", StringComparison.Ordinal)) { classified = false; }
                Require(classified == accepted, "Core package target case " + name + " must be " + accepted + ".");
                var selected = new DiscoveredMod(manifest, packageRoot, path, "Local", null, true, false, string.Empty, false, string.Empty);
                MethodInfo validate = typeof(DolocTownGameBridge).GetMethod("ValidateSdkPackageMetadata", BindingFlags.NonPublic | BindingFlags.Static)!;
                object? result = validate.Invoke(null, new object?[] { selected, Path.GetDirectoryName(path), null });
                Require((result == null) == accepted, "GameBridge package target case " + name + " must agree with Core.");
            });
        }

        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
