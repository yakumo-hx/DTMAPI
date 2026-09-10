using System;
using System.Diagnostics;
using System.IO;

namespace DTMAPI.UnitTests
{
    internal static class MineProductTests
    {
        internal static void RunAll()
        {
            ProductSourceOwnsSessionDerivedNoSidecarBoundary();
            PhysicalHookAndRollbackFixturePasses();
        }

        private static void
            ProductSourceOwnsSessionDerivedNoSidecarBoundary()
        {
            string root = FindRepositoryRoot();
            string modEntry =
                File.ReadAllText(
                    Path.Combine(
                        root,
                        "products",
                        "first-party",
                        "Mine",
                        "src",
                        "ModEntry.cs"));
            string runtime =
                File.ReadAllText(
                    Path.Combine(
                        root,
                        "products",
                        "first-party",
                        "Mine",
                        "src",
                        "Native",
                        "MineNativeRuntime.cs"));
            string nativeRoot =
                Path.Combine(
                    root,
                    "products",
                    "first-party",
                    "Mine",
                    "src",
                    "Native");
            string nativeSource =
                string.Join(
                    "\n",
                    Array.ConvertAll(
                        Directory.GetFiles(
                            nativeRoot,
                            "*.cs",
                            SearchOption.TopDirectoryOnly),
                        File.ReadAllText));
            string production =
                File.ReadAllText(
                    Path.Combine(
                        nativeRoot,
                        "MineNativeRuntime.Production.cs"));
            string config =
                File.ReadAllText(
                    Path.Combine(
                        root,
                        "products",
                        "first-party",
                        "Mine",
                        "src",
                        "MineConfig.cs"));

            Assert(
                modEntry.Contains(
                    "helper.Events.Save.SaveLoaded +=",
                    StringComparison.Ordinal) &&
                modEntry.Contains(
                    "helper.Events.GameLoop.ReturnedToTitle +=",
                    StringComparison.Ordinal) &&
                modEntry.Contains(
                    "runtime.InitializeAtEntry(",
                    StringComparison.Ordinal) &&
                runtime.Contains(
                    "hookInstaller.InstallAtomically(this);",
                    StringComparison.Ordinal) &&
                runtime.IndexOf(
                    "hookInstaller.InstallAtomically(this);",
                    StringComparison.Ordinal) <
                runtime.IndexOf(
                    "internal void SaveLoaded(",
                    StringComparison.Ordinal) &&
                runtime.Contains(
                    "Enabled changed from cold false; restart is required",
                    StringComparison.Ordinal) &&
                runtime.Contains(
                    "ClearSessionState();",
                    StringComparison.Ordinal) &&
                runtime.Contains(
                    "schedulerMode=session-derived",
                    StringComparison.Ordinal) &&
                runtime.Contains(
                    "state.LastObservedTotalTUs",
                    StringComparison.Ordinal) &&
                runtime.Contains(
                    "state.NextDueTotalTUs",
                    StringComparison.Ordinal),
                "Mine lifecycle must register its supervised hook set during Entry, derive its scheduler after SaveLoaded and clear it at title/unload.");
            Assert(
                !config.Contains(
                    "Power",
                    StringComparison.Ordinal) &&
                !config.Contains(
                    "IncludeRuntimeModMinerals",
                    StringComparison.Ordinal) &&
                !runtime.Contains(
                    "sidecar",
                    StringComparison.OrdinalIgnoreCase) &&
                !nativeSource.Contains(
                    "Working",
                    StringComparison.Ordinal) &&
                !nativeSource.Contains(
                    "Committed",
                    StringComparison.Ordinal),
                "Mine must expose neither configurable power/runtime-mineral switches nor a Working/Committed sidecar scheduler.");
            Assert(
                nativeSource.Contains(
                    "CaptureMachineCycleSnapshot",
                    StringComparison.Ordinal) &&
                nativeSource.Contains(
                    "RestoreMachineCycleSnapshot",
                    StringComparison.Ordinal) &&
                nativeSource.Contains(
                    "Due retained at",
                    StringComparison.Ordinal),
                "Mine production must snapshot native power/inventory, restore failed output and retain the due cycle.");
            Assert(
                nativeSource.Contains(
                    "MineSessionScheduler",
                    StringComparison.Ordinal) &&
                nativeSource.Contains(
                    "ReferenceEqualityComparer",
                    StringComparison.Ordinal) &&
                nativeSource.Contains(
                    "EndAuthoritativeScan",
                    StringComparison.Ordinal) &&
                !nativeSource.Contains(
                    "BuildMachineRuntimeKey",
                    StringComparison.Ordinal),
                "Mine scheduler must use stable object identity and prune entries only after a complete authoritative scan.");
            Assert(
                production.IndexOf(
                    "if (!TryPreflightMineCycle(",
                    StringComparison.Ordinal) <
                production.IndexOf(
                    "MineOutputRule? output = PickMineOutput();",
                    StringComparison.Ordinal) &&
                production.Contains(
                    "Mine-owned storage is full; Launch() was not called.",
                    StringComparison.Ordinal) &&
                production.Contains(
                    "no output was selected or constructed and inventory was not copied.",
                    StringComparison.Ordinal) &&
                production.IndexOf(
                    "CaptureMachineCycleSnapshot(equipment)",
                    StringComparison.Ordinal) >
                production.IndexOf(
                    "preparedItems[i] = item;",
                    StringComparison.Ordinal) &&
                !nativeSource.Contains(
                    "AllowFuelMode",
                    StringComparison.Ordinal) &&
                !nativeSource.Contains(
                    "ProbabilityOverrides",
                    StringComparison.Ordinal),
                "Mine must complete storage/power/output preflight before native mutation and must not retain the generic fuel/probability engine.");
        }

        private static void
            PhysicalHookAndRollbackFixturePasses()
        {
            string executable =
                Path.Combine(
                    FindRepositoryRoot(),
                    "tests",
                    "DTMAPI.UnitTests",
                    "Fixtures",
                    "MineHarmonyOwnerFixture",
                    "bin",
                    "Release",
                    "net48",
                    "MineHarmonyOwnerFixture.exe");
            if (!File.Exists(executable))
            {
                throw new FileNotFoundException(
                    "The focused Unit build did not produce the Mine Hook/rollback fixture.",
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
                    "Could not start the Mine Hook/rollback fixture.");
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
                    "The Mine Hook/rollback fixture did not exit within 30 seconds.");
            }
            Assert(
                process.ExitCode == 0 &&
                output.Contains(
                    "MineHarmonyOwnerFixture: OK",
                    StringComparison.Ordinal),
                "The Mine Hook/rollback fixture failed. exit=" +
                process.ExitCode +
                "; stdout=" + output +
                "; stderr=" + error);
        }

        private static string FindRepositoryRoot()
        {
            DirectoryInfo? current =
                new DirectoryInfo(AppContext.BaseDirectory);
            while (current != null)
            {
                if (File.Exists(
                        Path.Combine(
                            current.FullName,
                            "PROJECT.md")))
                    return current.FullName;
                current = current.Parent;
            }
            throw new DirectoryNotFoundException(
                "Could not find the DTMAPI repository root.");
        }

        private static void Assert(
            bool condition,
            string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
