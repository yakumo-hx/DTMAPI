#pragma warning disable CS0618 // These tests intentionally exercise the frozen IChestLocatorEnhancerApi surface.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using DTMAPI.Abstractions;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;
using DTMAPI.GameBridge.DolocTown;
using DTMAPI.ModConfigMenu;
using DTMAPI.Testing;

namespace DTMAPI.UnitTests
{
    internal static class ChestLocatorCompatibilityHostTests
    {
        private static int fixtureSequence;

        internal static void RunAll()
        {
            NoDemandHookPassKeepsCompatibilityHostDormant();
            ManagedProductFirstRefusesFrozenCompatibilityWithoutDemand();
            CompatibilityFirstReconcilesBeforeThePendingPostfixInstalls();
            PhysicalHarmonyFixtureExecutesBothOrdersAndLifecycle();
        }

        private static void NoDemandHookPassKeepsCompatibilityHostDormant()
        {
            CreateFixture(
                out DtmApiRuntime runtime,
                out DolocTownGameBridge bridge);
            var compatibility =
                new ChestLocatorEnhancerService(runtime);
            var hooks =
                new ChestLocatorEnhancerHookBridge(
                    runtime,
                    compatibility);

            hooks.InstallHooks(
                new HarmonyReflectionPatcher(runtime));

            Assert(
                !CompatibilityHostBroker.For(runtime).IsLoaded,
                "A no-demand Hook pass must not activate the dormant Compatibility Host.");
            Assert(
                !hooks.AvailableInventoriesPatched,
                "A no-demand Hook pass must not install the compatibility Postfix.");

            bridge.Shutdown(
                "ChestLocator no-demand Unit cleanup");
        }

        private static void ManagedProductFirstRefusesFrozenCompatibilityWithoutDemand()
        {
            CreateFixture(out DtmApiRuntime runtime, out _);
            var compatibility =
                new ChestLocatorEnhancerService(
                    runtime,
                    () => true);
            IManifest owner =
                Manifest(
                    "DTMAPI.Tests.ChestLocator.ProductFirst");

            AssertThrows<InvalidOperationException>(
                () => compatibility.Register(
                    owner,
                    EnabledOptions()),
                "A product-owned exact target must reject a later frozen compatibility request.");
            Assert(
                compatibility.CountOwnerResources(
                    owner.UniqueID) == 0,
                "A refused product-first registration must retain no compatibility owner state.");
            Assert(
                runtime.DemandCoordinator
                    .GetOwnerDemandCount(owner.UniqueID) == 0,
                "A refused product-first registration must publish no compatibility demand.");
        }

        private static void CompatibilityFirstReconcilesBeforeThePendingPostfixInstalls()
        {
            CreateFixture(
                out DtmApiRuntime runtime,
                out DolocTownGameBridge bridge);
            bool managedProductOwnerPresent = false;
            var compatibility =
                new ChestLocatorEnhancerService(
                    runtime,
                    () => managedProductOwnerPresent);
            IManifest owner =
                Manifest(
                    "DTMAPI.Tests.ChestLocator.CompatibilityFirst");

            ChestLocatorEnhancerRegisterResult registered =
                compatibility.Register(
                    owner,
                    EnabledOptions());
            Assert(
                registered.Success &&
                compatibility.CountOwnerResources(
                    owner.UniqueID) == 2 &&
                runtime.DemandCoordinator
                    .GetOwnerDemandCount(owner.UniqueID) > 0,
                "The compatibility-first fixture must begin with one accepted policy/state pair and live demand.");

            managedProductOwnerPresent = true;
            var hooks =
                new ChestLocatorEnhancerHookBridge(
                    runtime,
                    compatibility);
            hooks.InstallHooks(
                new HarmonyReflectionPatcher(runtime));

            Assert(
                !hooks.AvailableInventoriesPatched,
                "Compatibility reconciliation must stop before the GameBridge Postfix is installed.");
            Assert(
                compatibility.CountOwnerResources(
                    owner.UniqueID) == 0,
                "Compatibility reconciliation must remove every old owner root.");
            Assert(
                runtime.DemandCoordinator
                    .GetOwnerDemandCount(owner.UniqueID) == 0,
                "Compatibility reconciliation must synchronously release the old demand.");
            Assert(
                compatibility.GetState(owner.UniqueID).Status ==
                    "not-configured",
                "A reconciled owner must return the frozen not-configured state instead of stale configured state.");

            bridge.Shutdown(
                "ChestLocator compatibility-first Unit cleanup");
        }

        private static void PhysicalHarmonyFixtureExecutesBothOrdersAndLifecycle()
        {
            string executable =
                Path.Combine(
                    FindRepositoryRoot(),
                    "tests",
                    "DTMAPI.UnitTests",
                    "Fixtures",
                    "ChestLocatorHarmonyOwnerFixture",
                    "bin",
                    "Release",
                    "net48",
                    "ChestLocatorHarmonyOwnerFixture.exe");
            if (!File.Exists(executable))
            {
                throw new FileNotFoundException(
                    "The focused Unit build did not produce the physical Harmony owner fixture.",
                    executable);
            }

            var start =
                new ProcessStartInfo
                {
                    FileName = executable,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
            using Process process =
                Process.Start(start)
                ?? throw new InvalidOperationException(
                    "Could not start the physical Harmony owner fixture.");
            string output =
                process.StandardOutput.ReadToEnd();
            string error =
                process.StandardError.ReadToEnd();
            if (!process.WaitForExit(30000))
            {
                try
                {
                    process.Kill();
                }
                catch
                {
                }
                throw new TimeoutException(
                    "The physical Harmony owner fixture did not exit within 30 seconds.");
            }

            Assert(
                process.ExitCode == 0 &&
                output.Contains(
                    "ChestLocatorHarmonyOwnerFixture: OK",
                    StringComparison.Ordinal),
                "The physical Harmony owner fixture failed. exit=" +
                process.ExitCode +
                ", output=" +
                output +
                ", error=" +
                error);
        }

        private static void CreateFixture(
            out DtmApiRuntime runtime,
            out DolocTownGameBridge bridge)
        {
            string gamePath =
                Path.Combine(
                    DtmApiTestSession.Current.RootPath,
                    "chest-locator-compatibility-" +
                    (++fixtureSequence).ToString(
                        CultureInfo.InvariantCulture));
            Directory.CreateDirectory(
                Path.Combine(
                    gamePath,
                    "BepInEx",
                    "plugins"));
            StageCompatibilityHostFixture(gamePath);
            runtime =
                new DtmApiRuntime(
                    new FakeHost(gamePath),
                    new ConfigMenuRegistry());
            bridge = new DolocTownGameBridge(runtime);
        }

        private static void StageCompatibilityHostFixture(
            string gamePath)
        {
            const string fileName =
                "DTMAPI.GameBridge.DolocTown.Compatibility.dll";
            string source =
                Path.Combine(
                    AppContext.BaseDirectory,
                    "CompatibilityHostFixture",
                    fileName);
            if (!File.Exists(source))
            {
                throw new FileNotFoundException(
                    "The Unit build did not stage the Compatibility Host fixture.",
                    source);
            }

            string componentDirectory =
                Path.Combine(
                    gamePath,
                    "DTMAPI",
                    "components",
                    "compatibility");
            string manifestDirectory =
                Path.Combine(gamePath, "DTMAPI");
            Directory.CreateDirectory(componentDirectory);
            Directory.CreateDirectory(manifestDirectory);
            string target =
                Path.Combine(
                    componentDirectory,
                    fileName);
            File.Copy(source, target, overwrite: true);

            var info = new FileInfo(target);
            string sha256;
            using (FileStream stream = File.OpenRead(target))
            using (SHA256 hash = SHA256.Create())
            {
                sha256 =
                    BitConverter
                        .ToString(hash.ComputeHash(stream))
                        .Replace("-", string.Empty);
            }

            string fileVersion =
                FileVersionInfo
                    .GetVersionInfo(target)
                    .FileVersion ??
                string.Empty;
            string manifest =
                "{\"OptionalComponents\":[{" +
                "\"ComponentId\":\"gamebridge-compatibility-host\"," +
                "\"Distribution\":\"dormant-shipped\"," +
                "\"LoadPolicy\":\"first-frozen-abi-call\"," +
                "\"RelativePath\":\"DTMAPI/components/compatibility/" +
                fileName +
                "\"," +
                "\"Length\":" +
                info.Length.ToString(
                    CultureInfo.InvariantCulture) +
                "," +
                "\"Sha256\":\"" +
                sha256 +
                "\"," +
                "\"AssemblyName\":\"DTMAPI.GameBridge.DolocTown.Compatibility\"," +
                "\"AssemblyVersion\":\"" +
                CompatibilityHostBroker.ExpectedAssemblyVersion +
                "\"," +
                "\"FileVersion\":\"" +
                fileVersion +
                "\"," +
                "\"TargetFramework\":\"netstandard2.0\"," +
                "\"DefaultLoadState\":\"dormant\"," +
                "\"IncludedInDownloadPackage\":true}]}";
            File.WriteAllText(
                Path.Combine(
                    manifestDirectory,
                    "release-manifest.json"),
                manifest,
                new UTF8Encoding(false));
        }

        private static ChestLocatorEnhancerOptions EnabledOptions() =>
            new ChestLocatorEnhancerOptions
            {
                Enabled = true,
                IncludeSharedCases = true,
                IncludeSharedStorageShelfBoxes = true,
                RespectNativeAutoUseBoxSetting = true,
                VerboseLogging = false
            };

        private static IManifest Manifest(string uniqueId) =>
            new ManifestModel
            {
                Name = uniqueId,
                Author = "DTMAPI",
                Version = "1.0.0",
                UniqueID = uniqueId,
                Type = "CodeMod"
            };

        private static string FindRepositoryRoot()
        {
            foreach (string start in new[]
            {
                Directory.GetCurrentDirectory(),
                AppContext.BaseDirectory
            })
            {
                DirectoryInfo? current =
                    new DirectoryInfo(
                        Path.GetFullPath(start));
                while (current != null)
                {
                    if (File.Exists(
                        Path.Combine(
                            current.FullName,
                            "PROJECT.md")))
                    {
                        return current.FullName;
                    }
                    current = current.Parent;
                }
            }

            throw new DirectoryNotFoundException(
                "Could not locate the DTMAPI repository root.");
        }

        private static void Assert(
            bool condition,
            string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(
                    "ChestLocator compatibility test failed: " +
                    message);
            }
        }

        private static void AssertThrows<TException>(
            Action action,
            string message)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException(
                "ChestLocator compatibility test failed: " +
                message);
        }

        private sealed class FakeHost : IRuntimeHost
        {
            internal FakeHost(string gamePath)
            {
                GamePath = gamePath;
                PluginPath =
                    Path.Combine(
                        gamePath,
                        "BepInEx",
                        "plugins");
            }

            public string GamePath { get; }

            public string PluginPath { get; }

            public string HostName =>
                "ChestLocatorCompatibilityUnitTest";

            public List<string> Logs { get; } =
                new List<string>();

            public void Log(string message) =>
                Logs.Add(message ?? string.Empty);

            public void LogWarning(string message) =>
                Logs.Add(message ?? string.Empty);

            public void LogError(
                string message,
                Exception? exception = null) =>
                Logs.Add(
                    (message ?? string.Empty) +
                    (exception == null
                        ? string.Empty
                        : ": " + exception.Message));
        }
    }
}
