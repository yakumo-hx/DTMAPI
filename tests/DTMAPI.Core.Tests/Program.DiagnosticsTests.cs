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
using DTMAPI.ModConfigMenu;

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {

        private static void RuntimeRotatesLatestLogAndRetainsHistory()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                string logsDir = Path.Combine(dir, "DTMAPI", "logs");
                Directory.CreateDirectory(logsDir);

                DateTime baseTime = new DateTime(2026, 6, 16, 0, 0, 0, DateTimeKind.Utc);
                string latestPath = Path.Combine(logsDir, "latest.log");
                File.WriteAllText(latestPath, "previous-run-line");
                File.SetLastWriteTimeUtc(latestPath, baseTime.AddMinutes(1));

                string oldestHistory = string.Empty;
                for (int i = 0; i < 10; i++)
                {
                    DateTime stamp = baseTime.AddSeconds(i);
                    string historyPath = Path.Combine(logsDir, "latest-" + stamp.ToString("yyyyMMdd-HHmmssfff") + ".log");
                    if (i == 0)
                        oldestHistory = historyPath;
                    File.WriteAllText(historyPath, "history-" + i);
                    File.SetLastWriteTimeUtc(historyPath, stamp);
                }

                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                runtime.Start();

                string latestText = File.ReadAllText(runtime.Diagnostics.GetLatestLogPath());
                Assert(latestText.Contains("DTMAPI runtime starting."), "Fresh latest log should receive current startup lines.");
                Assert(!latestText.Contains("previous-run-line"), "Fresh latest log should not append previous run content.");

                string[] histories = Directory.GetFiles(logsDir, "latest-*.log");
                Assert(histories.Length == 10, "Latest log rotation should keep exactly 10 history files.");
                Assert(!File.Exists(oldestHistory), "Latest log rotation should delete the oldest history file when retaining 10.");
                string rotatedHistory = histories.FirstOrDefault(path => File.ReadAllText(path).Contains("previous-run-line")) ?? string.Empty;
                Assert(rotatedHistory.Length > 0, "Previous latest log should be rotated into retained history.");

                string report = runtime.ExportLogs();
                string[] reportEntries = ReadZipEntryNames(report);
                Assert(reportEntries.Contains("DTMAPI-history/" + Path.GetFileName(rotatedHistory)), "Diagnostic report should include retained latest-log history.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void DiagnosticsSnapshotApiExposesRuntimeState()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                WriteManifest(dir, "NeedsDiagnostics", "{ \"Name\": \"Needs Diagnostics\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.NeedsDiagnostics\", \"Type\": \"ContentPack\", \"MinimumGameVersion\": \"99.0.0\" }");
                WriteManifest(dir, "NeedsMissingDependency", "{ \"Name\": \"Needs Missing Dependency\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.NeedsMissingDependency\", \"Type\": \"ContentPack\", \"Dependencies\": [ { \"UniqueID\": \"DTMAPI.Tests.MissingDependency\", \"Required\": true } ] }");
                WriteManifest(dir, "BrokenEntryDll", "{ \"Name\": \"Broken Entry DLL\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.BrokenEntryDll\", \"Type\": \"CodeMod\", \"EntryDll\": \"BrokenEntryDll.txt\" }");
                WriteManifest(dir, "NeedsFutureApi", "{ \"Name\": \"Needs Future API\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.DiagnosticsFutureApi\", \"Type\": \"ContentPack\", \"MinimumDTMApiVersion\": \"99.0.0\" }");
                WriteManifest(dir, "UnknownStatus", "{ \"Name\": \"Unknown Status\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.UnknownStatus\", \"Type\": \"ContentPack\" }");
                string throwingDir = CreateEnabledFixtureModDirectory(dir, "ThrowingEntry");
                Directory.CreateDirectory(throwingDir);
                string assemblyPath = typeof(ThrowingEntryProbeMod).Assembly.Location;
                string assemblyName = Path.GetFileName(assemblyPath);
                File.Copy(assemblyPath, Path.Combine(throwingDir, assemblyName), overwrite: true);
                File.WriteAllText(
                    Path.Combine(throwingDir, "manifest.json"),
                    "{ \"Name\": \"Throwing Entry\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.ThrowingEntry\", \"Type\": \"CodeMod\", \"CodeModKind\": \"Strict\", \"EntryDll\": \"" + assemblyName + "\", \"EntryType\": \"" + (typeof(ThrowingEntryProbeMod).FullName ?? nameof(ThrowingEntryProbeMod)) + "\" }");

                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                runtime.Start();
                runtime.Diagnostics.RecordError("DTMAPI.Tests.UnknownStatus", "Unexpected unit diagnostic.", "No known classifier.");
                runtime.SetHookStatus("Feature.Camera", "ready", "test", "Feature status: id=Camera, lastOperation=InstallHooks, success=True, failureCount=0, lastError=none.");
                runtime.Diagnostics.SetFeatureStatus("Camera", "ready", "InstallHooks", true, 0, string.Empty, "Feature status: id=Camera, lastOperation=InstallHooks, success=True, failureCount=0, lastError=none.");
                string report = runtime.ExportLogs();

                IModRegistry registry = GetModRegistry(runtime);
                IDtmDiagnosticsApi? api = registry.GetApi<IDtmDiagnosticsApi>("DTMAPI");
                Assert(api != null, "Runtime should register IDtmDiagnosticsApi under the DTMAPI owner.");

                IDtmDiagnosticsSnapshot diagnosticSnapshot = api!.GetSnapshot();
                Assert(diagnosticSnapshot.LoadedMods.Any(m => m.UniqueID == "DTMAPI.Tests.NeedsDiagnostics" && m.Type == "ContentPack"), "Diagnostics snapshot should expose loaded mod rows without Core DiscoveredMod objects.");
                IDtmModStatusInfo loadedMod = diagnosticSnapshot.Mods.Single(m => m.UniqueID == "DTMAPI.Tests.NeedsDiagnostics");
                Assert(loadedMod.Loaded && loadedMod.Status == "loaded" && loadedMod.StatusCode == "loaded" && loadedMod.Type == "ContentPack", "Diagnostics snapshot should expose loaded mod status rows.");
                Assert(loadedMod.ManifestPath.EndsWith("manifest.json", StringComparison.OrdinalIgnoreCase) && Directory.Exists(loadedMod.RootPath), "Mod status rows should expose manifest and root paths.");
                IDtmModStatusInfo dependencyError = diagnosticSnapshot.Mods.Single(m => m.UniqueID == "DTMAPI.Tests.NeedsMissingDependency");
                Assert(!dependencyError.Loaded && dependencyError.Status == "error" && dependencyError.StatusCode == "missing-dependency" && dependencyError.Reason.Contains("缺少必需依赖"), "Diagnostics snapshot should expose dependency errors without parsing logs.");
                IDtmModStatusInfo entryDllError = diagnosticSnapshot.Mods.Single(m => m.UniqueID == "DTMAPI.Tests.BrokenEntryDll");
                Assert(!entryDllError.Loaded && entryDllError.Status == "error" && entryDllError.StatusCode == "entry-dll-error" && entryDllError.EntryDll == "BrokenEntryDll.txt" && entryDllError.Reason.Contains("缺失"), "Diagnostics snapshot should expose actual missing-entry errors and manifest entry fields.");
                Assert(diagnosticSnapshot.Mods.Single(m => m.UniqueID == "DTMAPI.Tests.DiagnosticsFutureApi").StatusCode == "api-too-new", "Diagnostics snapshot should expose API version status codes.");
                IDtmModStatusInfo throwingEntryStatus = diagnosticSnapshot.Mods.Single(m => m.UniqueID == "DTMAPI.Tests.ThrowingEntry");
                Assert(throwingEntryStatus.Status == "error" && throwingEntryStatus.StatusCode == "code-load-error" && throwingEntryStatus.Reason.Contains("throwing-entry-probe", StringComparison.Ordinal), "A failed Entry displays the load failure and its cause.");
                Assert(diagnosticSnapshot.Mods.Single(m => m.UniqueID == "DTMAPI.Tests.UnknownStatus").StatusCode == "unknown-error", "Diagnostics snapshot should expose unknown-error status codes for unclassified errors.");
                Assert(diagnosticSnapshot.Warnings.Any(w => w.Owner == "DTMAPI.Tests.NeedsDiagnostics" && w.Message.Contains("MinimumGameVersion")), "Diagnostics snapshot should include structured warnings.");
                Assert(diagnosticSnapshot.HookStatuses.Any(h => h.HookId == "Feature.Camera" && h.Status == "ready"), "Diagnostics snapshot should include hook statuses.");
                Assert(diagnosticSnapshot.FeatureStatuses.Any(f => f.FeatureId == "Camera" && f.Status == "ready" && f.LastOperation == "InstallHooks" && f.Success), "Diagnostics snapshot should include structured feature statuses.");
                Assert(diagnosticSnapshot.Errors.Any(e => e.Owner == "DTMAPI.Tests.NeedsMissingDependency"), "Diagnostics snapshot should include current errors.");
                Assert(diagnosticSnapshot.LatestLogPath == runtime.Diagnostics.GetLatestLogPath() && File.Exists(diagnosticSnapshot.LatestLogPath), "Diagnostics snapshot should expose the latest log path.");
                Assert(diagnosticSnapshot.LatestReportPath == report && File.Exists(diagnosticSnapshot.LatestReportPath), "Diagnostics snapshot should expose the latest report path after export.");
                RuntimeSnapshot runtimeSnapshot = runtime.CreateSnapshot();
                Assert(runtimeSnapshot.FeatureStatuses.Any(f => f.FeatureId == "Camera" && f.LastOperation == "InstallHooks"), "Runtime snapshot should include feature statuses.");
                Assert(runtimeSnapshot.LatestLogPath == diagnosticSnapshot.LatestLogPath, "Runtime snapshot should include latest log path.");
                Assert(runtimeSnapshot.LatestReportPath == diagnosticSnapshot.LatestReportPath, "Runtime snapshot should include latest report path.");

                string summary = ReadZipText(report, "dtmapi-summary.txt");
                Assert(summary.Contains("Features:") && summary.Contains("FEATURE Camera: ready") && summary.Contains("LatestLogPath:") && summary.Contains("LatestReportPath:"), "Diagnostic report summary should include feature status and path fields.");
                Assert(summary.Contains("RuntimeContext: present; entry=DTMAPI-runtime-context.txt", StringComparison.Ordinal), "Diagnostic report summary should point out that runtime context was included.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void RuntimeReportContextScansLogHeadAndTailForModFailures()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                runtime.Start();

                var lines = new List<string>();
                lines.Add("2026-06-23 00:00:00.000 [Info] [DTMAPI] unit log start.");
                lines.Add("[Error  : Unity Log] HarmonyException: early com.user.dolocnoweeds patch failure");
                for (int i = 0; i < 1800; i++)
                    lines.Add("filler-line-" + i.ToString(CultureInfo.InvariantCulture));
                lines.Add("[Error  : Unity Log] HarmonyException: tail plugin patch failure");
                File.WriteAllLines(runtime.Diagnostics.GetLatestLogPath(), lines, new UTF8Encoding(false));

                string report = runtime.ExportLogs();
                string reportContext = ReadZipText(report, "DTMAPI-runtime-context.txt");
                Assert(reportContext.Contains("BEPINEX-OR-MOD-ERROR-CANDIDATE line=[Error  : Unity Log] HarmonyException: early com.user.dolocnoweeds patch failure", StringComparison.Ordinal), "Runtime report should scan the log head so startup plugin failures are not lost in long logs.");
                Assert(reportContext.Contains("BEPINEX-OR-MOD-ERROR-CANDIDATE line=[Error  : Unity Log] HarmonyException: tail plugin patch failure", StringComparison.Ordinal), "Runtime report should keep scanning the log tail for late plugin failures.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void DiagnosticsReportIncludesDebugConsoleLastGiveBreadcrumb()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                Directory.CreateDirectory(runtime.Paths.DtmApiPath);
                Directory.CreateDirectory(runtime.Paths.ReportsPath);
                File.WriteAllText(Path.Combine(runtime.Paths.DtmApiPath, "debug-console-last-give.txt"), "item=item_test" + Environment.NewLine + "requested=10", new UTF8Encoding(false));

                string report = runtime.ExportLogs();
                string breadcrumb = ReadZipText(report, "DTMAPI-state/debug-console-last-give.txt");
                Assert(breadcrumb.Contains("item=item_test", StringComparison.Ordinal) && breadcrumb.Contains("requested=10", StringComparison.Ordinal), "In-game diagnostics export should include the debug-console last-give breadcrumb.");


            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void DiagnosticsServiceMissingCrashDumpWritesReadme()
        {
            string? previousRoot = UseTempPersistentRoot();
            string crashDir = Path.Combine(Path.GetTempPath(), "RedSawGames", "DolocTown", "Crashes", "dtmapi-unit-" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(crashDir);
                File.WriteAllText(Path.Combine(crashDir, "error.log"), "unit fatal without dump", new UTF8Encoding(false));

                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                string report = runtime.ExportLogs();
                string summary = ReadZipText(report, "Unity-Crashes/summary.txt");
                string readme = ReadZipText(report, "Unity-Crashes/MISSING-CRASH-DUMP-README.txt");

                Assert(readme.Contains(Path.Combine(crashDir, "crash.dmp"), StringComparison.Ordinal), "The report must identify the missing crash dump path.");
            }
            finally
            {
                try
                {
                    if (Directory.Exists(crashDir))
                        Directory.Delete(crashDir, recursive: true);
                }
                catch
                {
                }
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void DiagnosticsServiceCrashDirectoriesWithSameLeafUseDistinctZipEntries()
        {
            string? previousRoot = UseTempPersistentRoot();
            string leaf = "dtmapi-unit-same-" + Guid.NewGuid().ToString("N");
            string compactRootCrashDir = Path.Combine(Path.GetTempPath(), "RedSawGames", "DolocTown", "Crashes", leaf);
            string spacedRootCrashDir = Path.Combine(Path.GetTempPath(), "RedSawGames", "Doloc Town", "Crashes", leaf);
            try
            {
                Directory.CreateDirectory(compactRootCrashDir);
                Directory.CreateDirectory(spacedRootCrashDir);
                File.WriteAllText(Path.Combine(compactRootCrashDir, "error.log"), "unit fatal compact root", new UTF8Encoding(false));
                File.WriteAllBytes(Path.Combine(compactRootCrashDir, "crash.dmp"), new byte[] { 5, 6, 7, 8 });
                File.WriteAllText(Path.Combine(spacedRootCrashDir, "error.log"), "unit fatal spaced root", new UTF8Encoding(false));
                File.WriteAllBytes(Path.Combine(spacedRootCrashDir, "crash.dmp"), new byte[] { 1, 2, 3, 4 });
                DateTime compactTime = DateTime.UtcNow.AddDays(90);
                DateTime spacedTime = compactTime.AddMinutes(1);
                Directory.SetLastWriteTimeUtc(compactRootCrashDir, compactTime);
                File.SetLastWriteTimeUtc(Path.Combine(compactRootCrashDir, "error.log"), compactTime);
                File.SetLastWriteTimeUtc(Path.Combine(compactRootCrashDir, "crash.dmp"), compactTime);
                Directory.SetLastWriteTimeUtc(spacedRootCrashDir, spacedTime);
                File.SetLastWriteTimeUtc(Path.Combine(spacedRootCrashDir, "error.log"), spacedTime);
                File.SetLastWriteTimeUtc(Path.Combine(spacedRootCrashDir, "crash.dmp"), spacedTime);

                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                string report = runtime.ExportLogs();
                string[] entries = ReadZipEntryNames(report);
                string summary = ReadZipText(report, "Unity-Crashes/summary.txt");
                string[] errorEntries = entries
                    .Where(entry => entry.StartsWith("Unity-Crashes/" + leaf + "-", StringComparison.Ordinal) &&
                        entry.EndsWith("/error.log", StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                string[] entryDirectories = errorEntries
                    .Select(entry => entry.Split('/')[1])
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();

                Assert(errorEntries.Length >= 2 && entryDirectories.Length >= 2, "Diagnostics crash collection should keep same-leaf Unity crash directories from both Temp root spellings under distinct zip entry directories.");
                Assert(summary.Contains(compactRootCrashDir, StringComparison.OrdinalIgnoreCase) && summary.Contains(spacedRootCrashDir, StringComparison.OrdinalIgnoreCase), "Diagnostics crash summary should name both same-leaf crash directories so support can see which root supplied each report.");
            }
            finally
            {
                DeleteDirectoryQuietly(compactRootCrashDir);
                DeleteDirectoryQuietly(spacedRootCrashDir);
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void DiagnosticsServiceCrashDumpKeepsFollowingTextEvidence()
        {
            string? previousRoot = UseTempPersistentRoot();
            string crashDir = Path.Combine(Path.GetTempPath(), "RedSawGames", "DolocTown", "Crashes", "dtmapi-unit-dump-text-" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(crashDir);
                using (FileStream dump = new FileStream(Path.Combine(crashDir, "crash.dmp"), FileMode.CreateNew, FileAccess.Write, FileShare.Read))
                    dump.WriteByte(42);
                File.WriteAllText(Path.Combine(crashDir, "error.log"), "unit fatal after dump", new UTF8Encoding(false));
                Directory.SetLastWriteTimeUtc(crashDir, DateTime.UtcNow.AddMinutes(12));

                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                string report = runtime.ExportLogs();
                string[] entries = ReadZipEntryNames(report);
                string summary = ReadZipText(report, "Unity-Crashes/summary.txt");

                Assert(entries.Any(entry => entry.StartsWith("Unity-Crashes/", StringComparison.Ordinal) && entry.EndsWith("/crash.dmp", StringComparison.OrdinalIgnoreCase)), "Diagnostics crash collection should include a crash.dmp.");
                Assert(entries.Any(entry => entry.StartsWith("Unity-Crashes/", StringComparison.Ordinal) && entry.EndsWith("/error.log", StringComparison.OrdinalIgnoreCase)), "Diagnostics crash collection should include error.log alongside crash.dmp.");
            }
            finally
            {
                DeleteDirectoryQuietly(crashDir);
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void ManagerViewModelMapsDiagnosticsSnapshot()
        {
            string latestLogPath = Path.GetTempFileName();
            string readyReportPath = Path.GetTempFileName();
            string longRootPath = Path.Combine(Path.GetTempPath(), "DTMAPI", new string('x', 128), "Mods", "Example");
            string missingReportPath = Path.Combine(Path.GetTempPath(), "dtmapi-manager-missing-report.zip");
            if (File.Exists(missingReportPath))
                File.Delete(missingReportPath);

            try
            {
                var olderError = new DtmErrorInfo("Example.Mod", "older error", "older stack");
                Thread.Sleep(2);
                var newerError = new DtmErrorInfo("Example.Mod", "failed to load asset", "stack trace");
                var olderWarning = new DtmWarningInfo("Example.Optional", "older warning", "older warning details");
                Thread.Sleep(2);
                var newerWarning = new DtmWarningInfo("Example.Optional", "optional dependency version too low", "wanted 1.0");

                var snapshot = new DtmDiagnosticsSnapshot(
                    DateTimeOffset.Now,
                    Array.Empty<IDtmLoadedModInfo>(),
                    new IDtmModStatusInfo[]
                    {
                        new DtmModStatusInfo(
                            "Example.Mod",
                            "Example Mod",
                            "1.2.3",
                            "Code",
                            "Local",
                            "Local.Example.Mod",
                            true,
                            true,
                            "official-enabled",
                            "Example.dll",
                            "ModEntry",
                            true,
                            "loaded",
                            "loaded",
                            "OK",
                            Path.Combine(longRootPath, "manifest.json"),
                            longRootPath),
                        new DtmModStatusInfo(
                            "Blocked.Mod",
                            "Blocked Mod",
                            "1.0.0",
                            "Code",
                            "Workshop",
                            "Workshop.Blocked.Mod",
                            true,
                            true,
                            "official-enabled",
                            "Blocked.dll",
                            "ModEntry",
                            false,
                            "error",
                            "missing-dependency",
                            "Missing dependency",
                            Path.Combine(Path.GetTempPath(), "blocked-manifest.json"),
                            Path.Combine(Path.GetTempPath(), "Blocked")),
                        new DtmModStatusInfo(
                            "Warning.Mod",
                            "Warning Mod",
                            "1.0.0",
                            "Code",
                            "Workshop",
                            "Workshop.Warning.Mod",
                            true,
                            true,
                            "official-enabled",
                            "Warning.dll",
                            "ModEntry",
                            true,
                            "warning",
                            "warning",
                            "Loaded with warning",
                            Path.Combine(Path.GetTempPath(), "warning-manifest.json"),
                            Path.Combine(Path.GetTempPath(), "Warning")),
                        new DtmModStatusInfo(
                            "ReasonOnly.Mod",
                            "Reason Only Mod",
                            "1.0.0",
                            "Code",
                            "Local",
                            "Local.ReasonOnly.Mod",
                            true,
                            true,
                            "official-enabled",
                            "ReasonOnly.dll",
                            "ModEntry",
                            true,
                            "loaded",
                            "loaded",
                            "Reason text mentions warning, blocked, and error but structured status is loaded.",
                            Path.Combine(Path.GetTempPath(), "reason-only-manifest.json"),
                            Path.Combine(Path.GetTempPath(), "ReasonOnly")),
                        new DtmModStatusInfo(
                            "Disabled.Mod",
                            "Disabled Mod",
                            "1.0.0",
                            "Code",
                            "Local",
                            "Local.Disabled.Mod",
                            false,
                            true,
                            "official-disabled",
                            "Disabled.dll",
                            "ModEntry",
                            false,
                            "disabled",
                            "disabled",
                            "Disabled by official mod list",
                            Path.Combine(Path.GetTempPath(), "disabled-manifest.json"),
                            Path.Combine(Path.GetTempPath(), "Disabled"))
                    },
                    new IDtmErrorInfo[]
                    {
                        olderError,
                        newerError
                    },
                    new IDtmWarningInfo[]
                    {
                        olderWarning,
                        newerWarning
                    },
                    new IHookStatusInfo[]
                    {
                        new HookStatusInfo("Fishing.Automation", "experimental", "AgentStateFishingWait.OnPlay", "ready"),
                        new HookStatusInfo("Feature.Camera", "ready", "CameraFeature", "ready"),
                        new HookStatusInfo("Feature.Broken", "failed", "BrokenFeature", "failed"),
                        new HookStatusInfo("Save.MoreSlotsApi", "missing", "SaveSlotsFeature", "missing")
                    },
                    new IDtmFeatureStatusInfo[]
                    {
                        new DtmFeatureStatusInfo("FishingAutomation", "ready", "Update", true, 2, string.Empty, "cumulative=2"),
                        new DtmFeatureStatusInfo("Camera", "ready", "Update", true, 0, string.Empty, "ready"),
                        new DtmFeatureStatusInfo("BrokenFeature", "failed", "Update", false, 3, "boom", "failed")
                    },
                    latestLogPath,
                    missingReportPath);

                var blockedDependency = new ContentManifestDependencyRow(
                    "Missing.Provider",
                    true,
                    "2.0.0",
                    "required-missing",
                    "error",
                    "Dependency was not discovered.");
                var contentRegistry = new ContentManifestRegistrySnapshot(
                    "unit-manager",
                    DateTimeOffset.Now,
                    new[]
                    {
                        new ContentManifestRegistryRow(
                            "Blocked.Mod",
                            "Blocked Mod",
                            "1.0.0",
                            "Code",
                            "Blocked.dll",
                            "ModEntry",
                            string.Empty,
                            string.Empty,
                            "Workshop",
                            "Workshop.Blocked.Mod",
                            true,
                            true,
                            false,
                            false,
                            Path.Combine(Path.GetTempPath(), "blocked-manifest.json"),
                            Path.Combine(Path.GetTempPath(), "Blocked"),
                            "Blocked.Mod",
                            new[] { "CodeMod" },
                            new[] { blockedDependency })
                    },
                    new[] { ContentManifestRegistryDiagnostic.Error("dependency", "Blocked.Mod", "Missing.Provider") },
                    Array.Empty<string>());

                DtmManagerViewModel model = DtmManagerViewModelFactory.FromSnapshot(snapshot, null, contentRegistry);

                Assert(model.Mods.Count == 5, "Manager model should map mod status rows.");
                Assert(model.Mods.Select(m => m.UniqueID).SequenceEqual(new[] { "Blocked.Mod", "Warning.Mod", "Example.Mod", "ReasonOnly.Mod", "Disabled.Mod" }),
                    "Enabled rows precede disabled rows, preserving existing status/source/name order within each group.");
                Assert(model.Mods[2].StatusCode == "loaded", "Manager mod row should keep structured status code.");
                Assert(model.Mods[2].RootPath == longRootPath, "Manager mod row should preserve long root paths.");
                Assert(model.Mods[0].Dependencies.Count == 1 && model.Mods[0].Dependencies[0].UniqueID == "Missing.Provider" && model.Mods[0].HasDependencyIssue, "Manager mod rows should map authoritative dependency status from the content/manifest registry.");
                Assert(ManagerPageRowFormatter.FormatModListRow(model.Mods[0]).Contains("deps=1 (1 issue)"), "Manager compact mod rows should expose dependency issue counts before truncation.");
                Assert(ManagerPageRowFormatter.FormatModDetailLines(model.Mods[0]).Any(line => line.Contains("Missing.Provider>=2.0.0") && line.Contains("required-missing")), "Manager selected-mod details should expose dependency identity, minimum version, and resolved status.");
                ManagerModRow reasonOnlyMod = model.Mods.Single(m => m.UniqueID == "ReasonOnly.Mod");
                Assert(!reasonOnlyMod.IsWarning && !reasonOnlyMod.IsBlocked, "Manager mod row should not infer warning/blocking severity from free-text Reason.");
                Assert(model.Errors.Count == 2 && model.Errors[0].Severity == "Error" && model.Errors[0].Message == "failed to load asset", "Manager model should map diagnostics errors newest first.");
                Assert(model.Warnings.Count == 2 && model.Warnings[0].Severity == "Warning" && model.Warnings[0].Message == "optional dependency version too low", "Manager model should map diagnostics warnings newest first.");
                Assert(model.Hooks.Count == 4 && model.Hooks[0].HookId == "Feature.Broken" && model.Hooks[1].HookId == "Save.MoreSlotsApi", "Manager hooks should sort failed and missing before experimental/ready rows.");
                Assert(model.Hooks[0].IsFailed && !model.Hooks[0].IsMissing && !model.Hooks[1].IsFailed && model.Hooks[1].IsMissing, "Manager hook rows should distinguish failed and missing states.");
                Assert(model.Features.Count == 3 && model.Features[0].FeatureId == "BrokenFeature" && model.Features[1].FeatureId == "FishingAutomation" && model.Features[2].FeatureId == "Camera", "Manager features should sort failed, degraded, then ready rows.");
                Assert(model.Features[1].FailureCount == 2, "Manager feature row should keep cumulative failure count.");
                Assert(model.Summary.LoadedModCount == 3, "Manager summary should count loaded mods.");
                Assert(model.Summary.BlockedModCount == 1, "Manager summary should count blocked mods.");
                Assert(model.Summary.DisabledModCount == 1, "Manager summary should count disabled mods.");
                Assert(model.Summary.DependencyIssueModCount == 1, "Manager summary should count Mods with authoritative dependency issues.");
                Assert(model.Summary.ErrorCount == 2 && model.Summary.WarningCount == 2, "Manager summary should count diagnostics rows.");
                Assert(model.Summary.FailedHookCount == 1, "Manager summary should count failed hooks separately from missing hooks.");
                Assert(model.Summary.MissingHookCount == 1, "Manager summary should count missing hooks.");
                Assert(model.Summary.FailedFeatureCount == 1, "Manager summary should count failed features.");
                Assert(model.Summary.DegradedFeatureCount == 1, "Manager summary should count degraded features.");
                Assert(model.Summary.OverallStatus == "failed", "Manager summary should mark blocked/error state as failed.");
                Assert(model.LatestLogPath == latestLogPath, "Manager model should expose latest log path.");
                Assert(model.LatestReportPath == missingReportPath, "Manager model should expose latest report path.");
                Assert(model.ExportReport.HasLatestLogPath && model.ExportReport.LatestLogExists, "Manager export status should detect an existing latest log path.");
                Assert(model.ExportReport.HasLatestReportPath && !model.ExportReport.LatestReportExists, "Manager export status should detect a missing latest report path.");
                Assert(model.ExportReport.Status == "missing-report", "Manager export status should mark a missing report path.");
                Assert(model.AdvancedDiagnostics.RegistryAvailable && model.AdvancedDiagnostics.DependencyErrorCount == 1 && model.AdvancedDiagnostics.Rows.Count == 1, "Manager advanced diagnostics should expose registry counters and sampled evidence.");
                Assert(ManagerPageRowFormatter.FormatAdvancedSummary(model.AdvancedDiagnostics).Contains("dependencyErrors=1"), "Manager advanced summary should expose dependency error counters.");
                Assert(ManagerPageRowFormatter.ModelUnavailable(string.Empty) == "Manager model unavailable.", "Manager formatter should expose the empty-model fallback text.");
                Assert(ManagerPageRowFormatter.ModelUnavailable("IOException: boom").Contains("refresh failed"), "Manager formatter should expose refresh failure text.");
                string blockedModLine = ManagerPageRowFormatter.FormatModRow(model.Mods[0]);
                Assert(blockedModLine.Contains("missing-dependency") && blockedModLine.Contains("Blocked Mod") && blockedModLine.Contains("Blocked.Mod") && blockedModLine.Contains("loaded=false") && blockedModLine.Contains("Missing dependency"), "Manager mod formatter should include status code, name, id, loaded state, and blocked reason.");
                string diagnosticLine = ManagerPageRowFormatter.FormatDiagnosticRow(model.Errors[0]);
                Assert(diagnosticLine.Contains("[Example.Mod]") && diagnosticLine.Contains("failed to load asset"), "Manager diagnostic formatter should include owner and message.");
                string missingHookLine = ManagerPageRowFormatter.FormatHookRow(model.Hooks[1]);
                Assert(missingHookLine.Contains("Save.MoreSlotsApi") && missingHookLine.Contains("missing") && !model.Hooks[1].IsFailed && model.Hooks[1].IsMissing, "Manager hook formatter should keep missing hooks warning-like instead of failed.");
                string failedFeatureLine = ManagerPageRowFormatter.FormatFeatureRow(model.Features[0]);
                string degradedFeatureLine = ManagerPageRowFormatter.FormatFeatureRow(model.Features[1]);
                Assert(failedFeatureLine.Contains("BrokenFeature") && failedFeatureLine.Contains("failures=3") && failedFeatureLine.Contains("boom"), "Manager feature formatter should include failed feature details.");
                Assert(degradedFeatureLine.Contains("FishingAutomation") && degradedFeatureLine.Contains("success=true") && degradedFeatureLine.Contains("failures=2"), "Manager feature formatter should include degraded feature counters.");
                string logsStateLine = ManagerPageRowFormatter.FormatLogsExportState(ManagerLogsPageState.From(null, model));
                Assert(logsStateLine.Contains("not-exported") && logsStateLine.Contains("missing-report"), "Manager logs formatter should expose not-exported and snapshot report status.");
                string statusSummary = ManagerPageRowFormatter.FormatStatusSummary(model, string.Empty, null);
                Assert(statusSummary.Contains("overall=failed") && statusSummary.Contains("mods=loaded:3,blocked:1,disabled:1") && statusSummary.Contains("diagnostics=errors:2,warnings:2") && statusSummary.Contains("hooks=failed:1,missing:1") && statusSummary.Contains("features=failed:1,degraded:1") && statusSummary.Contains("install=missing") && statusSummary.Contains("report=missing-report") && statusSummary.Contains("log=present|") && statusSummary.Contains("reportPath=missing|"), "Manager status summary should include support-loop counters, install state, and report/log state.");
                DtmManagerViewModel installedModel = DtmManagerViewModelFactory.FromSnapshot(
                    snapshot,
                    ManagerInstallStateSummary.Present("0.5.3-alpha", "0.5.3.0", "2026-06-11T00:00:00Z", Path.Combine(Path.GetTempPath(), "install-state.json"), 2, 5, true));
                string installStateLine = ManagerPageRowFormatter.FormatInstallState(installedModel.InstallState);
                Assert(installStateLine.Contains("present") && installStateLine.Contains("version:0.5.3-alpha") && installStateLine.Contains("legacyMoved:2") && installStateLine.Contains("legacyDetected:5") && installStateLine.Contains("uninstall:available"), "Manager install-state formatter should expose install version, legacy counters, and uninstall script availability.");
                Assert(ManagerPageRowFormatter.FormatShowingFirst("Hooks", 17, 64) == "Hooks: showing first 17 of 64", "Manager formatter should expose showing-first row counts.");
                ManagerPageWindow middlePage = ManagerPagination.Create(37, 1, 15);
                ManagerPageWindow clampedLastPage = ManagerPagination.Create(37, 99, 15);
                ManagerPageWindow emptyPage = ManagerPagination.Create(0, -4, 0);
                Assert(middlePage.Start == 15 && middlePage.End == 30 && middlePage.PageCount == 3, "Manager pagination should return stable middle-page bounds.");
                Assert(clampedLastPage.PageIndex == 2 && clampedLastPage.Start == 30 && clampedLastPage.End == 37, "Manager pagination should clamp stale page indexes after row counts shrink.");
                Assert(emptyPage.IsEmpty && emptyPage.PageIndex == 0 && emptyPage.PageCount == 1 && emptyPage.PageSize == 1, "Manager pagination should keep empty pages and invalid sizes safe.");

                ManagerModRow restartRequiredRow = ManagerModRow.FromMod(new DtmModStatusInfo(
                    "Restart.Mod",
                    "Restart Mod",
                    "1.0.0",
                    "Code",
                    "Workshop",
                    "Workshop.Restart.Mod",
                    true,
                    true,
                    "official-enabled",
                    "Restart.dll",
                    "ModEntry",
                    false,
                    "inactive",
                    "restart-required",
                    "Platform roots were deactivated.",
                    Path.Combine(Path.GetTempPath(), "restart-manifest.json"),
                    Path.Combine(Path.GetTempPath(), "Restart")));
                Assert(restartRequiredRow.RequiresRestart && restartRequiredRow.RestartHint == restartRequiredRow.Reason, "Manager should expose an actionable restart hint for restart-required Mod state.");

                var warningOnlySnapshot = new DtmDiagnosticsSnapshot(
                    DateTimeOffset.Now,
                    Array.Empty<IDtmLoadedModInfo>(),
                    new IDtmModStatusInfo[]
                    {
                        new DtmModStatusInfo(
                            "WarningOnly.Loaded",
                            "Warning Only Loaded",
                            "1.0.0",
                            "Code",
                            "Local",
                            "Local.WarningOnly.Loaded",
                            true,
                            true,
                            "official-enabled",
                            "Loaded.dll",
                            "ModEntry",
                            true,
                            "loaded",
                            "loaded",
                            "Reason mentions warning but does not change severity.",
                            Path.Combine(Path.GetTempPath(), "warning-only-loaded-manifest.json"),
                            Path.Combine(Path.GetTempPath(), "WarningOnlyLoaded"))
                    },
                    Array.Empty<IDtmErrorInfo>(),
                    Array.Empty<IDtmWarningInfo>(),
                    new IHookStatusInfo[]
                    {
                        new HookStatusInfo("Save.MoreSlotsApi", "missing", "SaveSlotsFeature", "missing")
                    },
                    new IDtmFeatureStatusInfo[]
                    {
                        new DtmFeatureStatusInfo("FishingAutomation", "ready", "Update", true, 4, string.Empty, "cumulative=4")
                    },
                    latestLogPath,
                    readyReportPath);

                DtmManagerViewModel warningOnlyModel = DtmManagerViewModelFactory.FromSnapshot(warningOnlySnapshot);
                Assert(warningOnlyModel.Summary.FailedHookCount == 0 && warningOnlyModel.Summary.MissingHookCount == 1, "Missing hooks should be warning severity but not failed hooks.");
                Assert(warningOnlyModel.Summary.FailedFeatureCount == 0 && warningOnlyModel.Summary.DegradedFeatureCount == 1, "Historical successful feature failures should be degraded but not failed.");
                Assert(warningOnlyModel.Summary.OverallStatus == "warning", "Manager summary should mark missing hooks or degraded features as warning when there are no failed/error states.");
                Assert(!warningOnlyModel.Mods[0].IsWarning && !warningOnlyModel.Mods[0].IsBlocked, "Manager mod severity should ignore free-text Reason when structured status is loaded.");

                var emptyPathSnapshot = new DtmDiagnosticsSnapshot(
                    DateTimeOffset.Now,
                    Array.Empty<IDtmLoadedModInfo>(),
                    Array.Empty<IDtmModStatusInfo>(),
                    Array.Empty<IDtmErrorInfo>(),
                    Array.Empty<IDtmWarningInfo>(),
                    Array.Empty<IHookStatusInfo>(),
                    Array.Empty<IDtmFeatureStatusInfo>(),
                    string.Empty,
                    string.Empty);
                DtmManagerViewModel emptyPathModel = DtmManagerViewModelFactory.FromSnapshot(emptyPathSnapshot);
                string emptyPathSummary = ManagerPageRowFormatter.FormatStatusSummary(emptyPathModel, string.Empty, null);
                Assert(emptyPathSummary.Contains("log=unavailable") && emptyPathSummary.Contains("reportPath=unavailable"), "Manager status summary should tolerate null/empty log and report paths.");
            }
            finally
            {
                File.Delete(latestLogPath);
                File.Delete(readyReportPath);
            }
        }

        private static void ManagerRuntimeProviderRefreshesSnapshotAfterReportExport()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                runtime.Start();

                runtime.UI.OpenDtmApiStatusPage();
                DtmManagerViewModel openedModel = runtime.UI.CurrentManagerModel ?? throw new InvalidOperationException("Opening a DTMAPI manager page should refresh the internal manager view model.");

                int errorCountBeforeRefresh = openedModel.Summary.ErrorCount;
                runtime.Diagnostics.RecordError("DTMAPI.Tests.ManagerRefresh", "manager refresh test error", "details");
                runtime.UI.RefreshDtmManagerModel();
                Assert(runtime.UI.CurrentManagerModel != null && runtime.UI.CurrentManagerModel.Summary.ErrorCount == errorCountBeforeRefresh + 1, "Explicit manager refresh should update summary counters from diagnostics.");
                DtmManagerCopySummaryResult copyFallback = runtime.UI.CopyManagerSummary(_ => throw new InvalidOperationException("simulated clipboard unavailable"));
                Assert(copyFallback.Status == "copy-unavailable" && copyFallback.Text.Contains("overall=") && copyFallback.Text.Contains("report="), "Manager Copy Summary fallback should not throw and should keep the support summary text.");

                string report = runtime.UI.ExportLogs();
                Assert(File.Exists(report), "UI report export should return an existing report path.");
                DtmManagerReportExportResult exportResult = runtime.UI.LastManagerReportExport ?? throw new InvalidOperationException("UI report export should retain the manager export result.");
                Assert(exportResult.Status == "exported", "UI report export should verify the refreshed snapshot report path.");
                Assert(exportResult.SnapshotReportPathMatched, "UI report export should match the exported path to snapshot LatestReportPath.");
                Assert(runtime.UI.CurrentManagerModel != null && runtime.UI.CurrentManagerModel.LatestReportPath == report, "UI report export should refresh the current manager model after export.");
                ManagerLogsPageState exportedLogsState = ManagerLogsPageState.From(exportResult, runtime.UI.CurrentManagerModel);
                Assert(exportedLogsState.ExportStatus == "exported", "Manager Logs state should expose the exported status.");
                Assert(exportedLogsState.ExportedReportPath == report, "Manager Logs state should expose the exported report path.");
                Assert(exportedLogsState.SnapshotLatestReportPath == report, "Manager Logs state should expose the refreshed snapshot report path.");
                Assert(exportedLogsState.PathMatchStatus == "matched" && exportedLogsState.SnapshotReportPathMatched, "Manager Logs state should expose the matched report path state.");

                ManagerLogsPageState notExportedLogsState = ManagerLogsPageState.From(null, runtime.UI.CurrentManagerModel);
                Assert(notExportedLogsState.ExportStatus == "not-exported" && notExportedLogsState.PathMatchStatus == "not-exported", "Manager Logs state should expose not-exported before an export result exists.");

                string latestLogPath = Path.GetTempFileName();
                string exportedPath = Path.Combine(Path.GetTempPath(), "dtmapi-manager-exported.zip");
                string staleSnapshotPath = Path.Combine(Path.GetTempPath(), "dtmapi-manager-stale.zip");
                try
                {
                    File.WriteAllText(exportedPath, "exported");
                    File.WriteAllText(staleSnapshotPath, "stale");
                    var mismatchProvider = new DtmManagerRuntimeModelProvider(
                        new FakeDiagnosticsApi(() => new DtmDiagnosticsSnapshot(
                            DateTimeOffset.Now,
                            Array.Empty<IDtmLoadedModInfo>(),
                            Array.Empty<IDtmModStatusInfo>(),
                            Array.Empty<IDtmErrorInfo>(),
                            Array.Empty<IDtmWarningInfo>(),
                            Array.Empty<IHookStatusInfo>(),
                            Array.Empty<IDtmFeatureStatusInfo>(),
                            latestLogPath,
                            staleSnapshotPath)),
                        () => exportedPath);

                    DtmManagerReportExportResult mismatch = mismatchProvider.ExportReportAndRefresh();
                    Assert(mismatch.Status == "report-path-mismatch", "Manager provider should flag report path mismatch after export and snapshot refresh.");
                    Assert(!mismatch.SnapshotReportPathMatched, "Manager provider mismatch result should expose unmatched report paths.");
                    Assert(mismatch.ExportedReportPath == exportedPath, "Manager provider mismatch result should keep the exported path.");
                    DtmManagerViewModel mismatchModel = mismatch.RefreshedModel ?? throw new InvalidOperationException("Manager provider mismatch result should keep a refreshed snapshot model.");
                    Assert(mismatchModel.LatestReportPath == staleSnapshotPath, "Manager provider mismatch result should keep the refreshed snapshot path.");
                    ManagerLogsPageState mismatchLogsState = ManagerLogsPageState.From(mismatch, null);
                    Assert(mismatchLogsState.ExportStatus == "report-path-mismatch", "Manager Logs state should expose report-path-mismatch export status.");
                    Assert(mismatchLogsState.ExportedReportPath == exportedPath, "Manager Logs state should expose the mismatched exported path.");
                    Assert(mismatchLogsState.SnapshotLatestReportPath == staleSnapshotPath, "Manager Logs state should expose the mismatched snapshot report path.");
                    Assert(mismatchLogsState.PathMatchStatus == "report-path-mismatch" && !mismatchLogsState.SnapshotReportPathMatched, "Manager Logs state should expose path mismatch.");

                    DtmManagerReportExportResult missingExportPath = DtmManagerReportExportResult.FromExport(string.Empty, mismatchModel);
                    ManagerLogsPageState missingExportPathState = ManagerLogsPageState.From(missingExportPath, mismatchModel);
                    Assert(missingExportPathState.ExportStatus == "missing-export-path", "Manager Logs state should expose missing-export-path export status.");
                    Assert(missingExportPathState.ExportedReportPath == string.Empty, "Manager Logs state should keep an empty exported path for missing export path.");
                    Assert(missingExportPathState.SnapshotLatestReportPath == staleSnapshotPath, "Manager Logs state should keep the refreshed snapshot path when export path is missing.");
                    Assert(missingExportPathState.PathMatchStatus == "missing-export-path", "Manager Logs state should expose missing export path as its path-match state.");
                }
                finally
                {
                    File.Delete(latestLogPath);
                    File.Delete(exportedPath);
                    File.Delete(staleSnapshotPath);
                }

                var uiWithoutProvider = new UiRuntimeService(() => string.Empty, _ => { }, _ => { });
                uiWithoutProvider.OpenDtmApiStatusPage();
                uiWithoutProvider.RefreshDtmManagerModel();
                Assert(uiWithoutProvider.CurrentManagerModel == null, "Manager refresh without a provider should leave the model unavailable for UI fallback.");

                bool exportFailureRecorded = false;
                var exportFailureUi = new UiRuntimeService(
                    () => throw new IOException("simulated export failure"),
                    _ => { },
                    _ => { },
                    (owner, message, details) =>
                    {
                        exportFailureRecorded = owner == "DTMAPI.ManagerUI" &&
                            message.IndexOf("ExportLogs", StringComparison.Ordinal) >= 0 &&
                            details.IndexOf("simulated export failure", StringComparison.Ordinal) >= 0;
                    },
                    (_, _) => { });
                exportFailureUi.ManagerModelProvider = new DtmManagerRuntimeModelProvider(
                    new FakeDiagnosticsApi(() => new DtmDiagnosticsSnapshot(
                        DateTimeOffset.Now,
                        Array.Empty<IDtmLoadedModInfo>(),
                        Array.Empty<IDtmModStatusInfo>(),
                        Array.Empty<IDtmErrorInfo>(),
                        Array.Empty<IDtmWarningInfo>(),
                        Array.Empty<IHookStatusInfo>(),
                        Array.Empty<IDtmFeatureStatusInfo>(),
                        string.Empty,
                        string.Empty)),
                    () => throw new IOException("simulated export failure"));
                string failedReport = exportFailureUi.ExportLogs();
                Assert(failedReport == string.Empty, "Manager UI export failure should return an empty path instead of throwing.");
                DtmManagerReportExportResult exportFailureResult = exportFailureUi.LastManagerReportExport ?? throw new InvalidOperationException("Manager UI export failure should retain an export-failed result.");
                Assert(exportFailureResult.Status == "export-failed", "Manager UI export failure should retain an export-failed result.");
                Assert(exportFailureResult.ErrorMessage.IndexOf("simulated export failure", StringComparison.Ordinal) >= 0, "Manager UI export failure should expose the internal error text.");
                ManagerLogsPageState exportFailureLogsState = ManagerLogsPageState.From(exportFailureResult, exportFailureUi.CurrentManagerModel);
                Assert(exportFailureLogsState.ExportStatus == "export-failed", "Manager Logs state should expose export-failed status.");
                Assert(exportFailureLogsState.ExportedReportPath == string.Empty, "Manager Logs state should clear exported path after export failure.");
                Assert(exportFailureLogsState.PathMatchStatus == "export-failed", "Manager Logs state should expose export-failed path-match status.");
                Assert(exportFailureLogsState.ExportErrorMessage.IndexOf("simulated export failure", StringComparison.Ordinal) >= 0, "Manager Logs state should expose export failure error text.");
                Assert(ManagerPageRowFormatter.FormatLogsExportState(exportFailureLogsState).Contains("export-failed"), "Manager Logs formatter should expose export-failed text.");
                Assert(exportFailureRecorded, "Manager UI export failure should record diagnostics with DTMAPI.ManagerUI owner.");

                bool refreshFailureRecorded = false;
                bool throwOnRefresh = false;
                var refreshFailureUi = new UiRuntimeService(
                    () => string.Empty,
                    _ => { },
                    _ => { },
                    (owner, message, details) =>
                    {
                        refreshFailureRecorded = owner == "DTMAPI.ManagerUI" &&
                            message.IndexOf("RefreshDtmManagerModel", StringComparison.Ordinal) >= 0 &&
                            details.IndexOf("simulated snapshot failure", StringComparison.Ordinal) >= 0;
                    },
                    (_, _) => { });
                refreshFailureUi.ManagerModelProvider = new DtmManagerRuntimeModelProvider(
                    new FakeDiagnosticsApi(() =>
                    {
                        if (throwOnRefresh)
                            throw new InvalidOperationException("simulated snapshot failure");

                        return new DtmDiagnosticsSnapshot(
                            DateTimeOffset.Now,
                            Array.Empty<IDtmLoadedModInfo>(),
                            Array.Empty<IDtmModStatusInfo>(),
                            Array.Empty<IDtmErrorInfo>(),
                            Array.Empty<IDtmWarningInfo>(),
                            Array.Empty<IHookStatusInfo>(),
                            Array.Empty<IDtmFeatureStatusInfo>(),
                            string.Empty,
                            string.Empty);
                    }),
                    () => string.Empty);
                refreshFailureUi.RefreshDtmManagerModel();
                DtmManagerViewModel stableModel = refreshFailureUi.CurrentManagerModel ?? throw new InvalidOperationException("Initial manager refresh should create a model.");
                throwOnRefresh = true;
                refreshFailureUi.RefreshDtmManagerModel();
                Assert(refreshFailureUi.CurrentManagerModel == stableModel, "Manager refresh failure should preserve the last known model.");
                Assert(refreshFailureUi.LastManagerRefreshError.IndexOf("simulated snapshot failure", StringComparison.Ordinal) >= 0, "Manager refresh failure should retain an internal refresh error message.");
                Assert(refreshFailureRecorded, "Manager refresh failure should record diagnostics with DTMAPI.ManagerUI owner.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static string ReadZipText(string zipPath, string entryName)
        {
            using (FileStream file = File.OpenRead(zipPath))
            using (var archive = new ZipArchive(file, ZipArchiveMode.Read))
            {
                ZipArchiveEntry entry = archive.GetEntry(entryName) ?? throw new InvalidOperationException("Zip entry not found: " + entryName);
                using (Stream stream = entry.Open())
                using (var reader = new StreamReader(stream))
                    return reader.ReadToEnd();
            }
        }

        private static string[] ReadZipEntryNames(string zipPath)
        {
            using (FileStream file = File.OpenRead(zipPath))
            using (var archive = new ZipArchive(file, ZipArchiveMode.Read))
                return archive.Entries.Select(entry => entry.FullName).ToArray();
        }

    }
}
