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
                string throwingDir = Path.Combine(dir, "Mods", "ThrowingEntry");
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
                Assert(!entryDllError.Loaded && entryDllError.Status == "error" && entryDllError.StatusCode == "entry-dll-error" && entryDllError.EntryDll == "BrokenEntryDll.txt" && entryDllError.Reason.Contains(".dll"), "Diagnostics snapshot should expose EntryDll errors and manifest entry fields.");
                Assert(diagnosticSnapshot.Mods.Single(m => m.UniqueID == "DTMAPI.Tests.DiagnosticsFutureApi").StatusCode == "api-too-new", "Diagnostics snapshot should expose API version status codes.");
                IDtmModStatusInfo throwingEntryStatus = diagnosticSnapshot.Mods.Single(m => m.UniqueID == "DTMAPI.Tests.ThrowingEntry");
                Assert(throwingEntryStatus.StatusCode == "restart-required" && throwingEntryStatus.Reason.Contains("Prior diagnostics", StringComparison.Ordinal), "Diagnostics snapshot should expose the terminal restart-required state while retaining the original code-load failure context.");
                Assert(diagnosticSnapshot.Mods.Single(m => m.UniqueID == "DTMAPI.Tests.UnknownStatus").StatusCode == "unknown-error", "Diagnostics snapshot should expose unknown-error status codes for unclassified errors.");
                Assert(diagnosticSnapshot.Warnings.Any(w => w.Owner == "DTMAPI.Tests.NeedsDiagnostics" && w.Message.Contains("MinimumGameVersion")), "Diagnostics snapshot should include structured warnings.");
                Assert(diagnosticSnapshot.HookStatuses.Any(h => h.HookId == "Feature.Camera" && h.Status == "ready"), "Diagnostics snapshot should include hook statuses.");
                Assert(diagnosticSnapshot.FeatureStatuses.Any(f => f.FeatureId == "Camera" && f.Status == "ready" && f.LastOperation == "InstallHooks" && f.Success), "Diagnostics snapshot should include structured feature statuses.");
                Assert(diagnosticSnapshot.Errors.Any(e => e.Owner == "DTMAPI.Tests.NeedsMissingDependency"), "Diagnostics snapshot should include current errors.");
                Assert(diagnosticSnapshot.LatestLogPath == runtime.Diagnostics.GetLatestLogPath() && File.Exists(diagnosticSnapshot.LatestLogPath), "Diagnostics snapshot should expose the latest log path.");
                Assert(diagnosticSnapshot.LatestReportPath == report && File.Exists(diagnosticSnapshot.LatestReportPath), "Diagnostics snapshot should expose the latest report path after export.");
                string reportContext = ReadZipText(report, "DTMAPI-runtime-context.txt");
                Assert(reportContext.Contains("MOD DTMAPI.Tests.ThrowingEntry statusCode=restart-required", StringComparison.Ordinal), "Diagnostic report should include the terminal per-mod restart-required status context.");
                Assert(reportContext.Contains("MOD-LOAD-FAILURE owner=DTMAPI.Tests.ThrowingEntry statusCode=restart-required", StringComparison.Ordinal) &&
                    reportContext.Contains("throwing-entry-probe", StringComparison.Ordinal), "Diagnostic report should classify the terminal restart-required Mod as a load failure while retaining the original Entry error context.");
                Assert(reportContext.Contains("OnApplicationQuitObserved: false", StringComparison.Ordinal), "Diagnostic report should include an OnApplicationQuit observation flag.");

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

        private static void DiagnosticsServiceCapsErrorsWarningsAndSummary()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                runtime.Paths.Ensure();
                File.WriteAllText(
                    Path.Combine(runtime.Paths.DtmApiPath, "install-state.json"),
                    "{ \"DTMAPIVersion\": \"0.5.3-alpha\", \"BinaryVersion\": \"0.5.3.0\", \"LegacyModsMoved\": [ { \"ModId\": \"Old.AutoFishing\" } ], \"LegacyDetections\": [ { \"Kind\": \"legacy-smapi-runtime\" }, { \"Kind\": \"legacy-workshop-cache\" } ] }",
                    new UTF8Encoding(false));
                File.WriteAllText(
                    Path.Combine(runtime.Paths.DtmApiPath, "release-manifest.json"),
                    "{ \"DTMAPIVersion\": \"0.5.3-alpha\", \"BinaryVersion\": \"0.5.3.0\", \"PackageKind\": \"unit-test\" }",
                    new UTF8Encoding(false));
                MethodInfo recordWarning = runtime.Diagnostics.GetType().GetMethod("RecordWarning", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?? throw new InvalidOperationException("DiagnosticsService.RecordWarning should be available for runtime warnings.");

                for (int i = 0; i < 1005; i++)
                {
                    runtime.Diagnostics.RecordError("DTMAPI.Tests.DiagnosticsCap", "error-" + i, "details-" + i);
                    recordWarning.Invoke(runtime.Diagnostics, new object[] { "DTMAPI.Tests.DiagnosticsCap", "warning-" + i, "details-" + i });
                }

                IReadOnlyList<IDtmErrorInfo> errors = runtime.Diagnostics.GetErrors();
                IReadOnlyList<IDtmWarningInfo> warnings = runtime.Diagnostics.GetWarnings();
                Assert(errors.Count == 1000, "Diagnostics errors should be capped at 1000 entries.");
                Assert(warnings.Count == 1000, "Diagnostics warnings should be capped at 1000 entries.");
                Assert(errors[0].Message == "error-5" && errors[999].Message == "error-1004", "Diagnostics errors should drop the oldest entries and keep the latest window.");
                Assert(warnings[0].Message == "warning-5" && warnings[999].Message == "warning-1004", "Diagnostics warnings should drop the oldest entries and keep the latest window.");

                string report = runtime.ExportLogs();
                string summary = ReadZipText(report, "dtmapi-summary.txt");
                Assert(ReadZipText(report, "install-state.json").Contains("Old.AutoFishing"), "Diagnostic report zip should include install-state.json when present.");
                Assert(ReadZipText(report, "release-manifest.json").Contains("unit-test"), "Diagnostic report zip should include release-manifest.json when present.");
                Assert(summary.Contains("Errors: 1000") && summary.Contains("Warnings: 1000"), "Diagnostic report summary should report the retained window counts.");
                Assert(summary.Contains("InstallState: present") && summary.Contains("InstallStateDTMAPIVersion: 0.5.3-alpha") && summary.Contains("InstallStateBinaryVersion: 0.5.3.0") && summary.Contains("InstallStateLegacyMovedCount: 1") && summary.Contains("InstallStateLegacyDetectedCount: 2"), "Diagnostic report summary should include install-state version and legacy counts.");
                Assert(summary.Contains("ReleaseManifest: present") && summary.Contains("ReleaseManifestDTMAPIVersion: 0.5.3-alpha") && summary.Contains("ReleaseManifestBinaryVersion: 0.5.3.0"), "Diagnostic report summary should include release manifest version fields.");
                Assert(summary.Contains("DiagnosticsTrimmed: errors=5, warnings=5, maxPerKind=1000."), "Diagnostic report summary should describe internal trimming when entries are capped.");
                Assert(!summary.Contains("error-0") && summary.Contains("error-1004") && !summary.Contains("warning-0") && summary.Contains("warning-1004"), "Diagnostic report summary should include retained entries, not trimmed oldest entries.");

                string aggregateDir = NewTempGameDir();
                var aggregateRuntime = new DtmApiRuntime(new FakeHost(aggregateDir), new ConfigMenuRegistry());
                for (int i = 0; i < 1005; i++)
                {
                    aggregateRuntime.Diagnostics.RecordError("DTMAPI.Tests.DiagnosticsAggregate", "same-error", "error-details-" + i);
                    recordWarning.Invoke(aggregateRuntime.Diagnostics, new object[] { "DTMAPI.Tests.DiagnosticsAggregate", "same-warning", "warning-details-" + i });
                }

                string aggregateReport = aggregateRuntime.ExportLogs();
                string aggregateSummary = ReadZipText(aggregateReport, "dtmapi-summary.txt");
                Assert(aggregateRuntime.Diagnostics.GetErrors().Count == 1000 && aggregateRuntime.Diagnostics.GetWarnings().Count == 1000, "Diagnostics retained windows should remain capped when aggregate counters keep total counts.");
                Assert(aggregateSummary.Contains("DiagnosticsTrimmed: errors=5, warnings=5, maxPerKind=1000."), "Aggregate diagnostics summary should still include trim summary.");
                Assert(aggregateSummary.Contains("DiagnosticsAggregates: totalKeys=2, top=2."), "Diagnostic report summary should include aggregate counter header.");
                Assert(aggregateSummary.Contains("DIAGNOSTIC-AGGREGATE Error owner=DTMAPI.Tests.DiagnosticsAggregate message=same-error count=1005") && aggregateSummary.Contains("lastDetails=error-details-1004"), "Diagnostic aggregate counters should retain total repeated error count and last details.");
                Assert(aggregateSummary.Contains("DIAGNOSTIC-AGGREGATE Warning owner=DTMAPI.Tests.DiagnosticsAggregate message=same-warning count=1005") && aggregateSummary.Contains("lastDetails=warning-details-1004"), "Diagnostic aggregate counters should retain total repeated warning count and last details.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void DiagnosticsServiceCrashDumpBudgetWritesMissingDumpReadme()
        {
            string? previousRoot = UseTempPersistentRoot();
            string crashDir = Path.Combine(Path.GetTempPath(), "RedSawGames", "DolocTown", "Crashes", "dtmapi-unit-" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(crashDir);
                File.WriteAllText(Path.Combine(crashDir, "error.log"), "unit fatal", new UTF8Encoding(false));
                using (FileStream dump = new FileStream(Path.Combine(crashDir, "crash.dmp"), FileMode.CreateNew, FileAccess.Write, FileShare.Read))
                    dump.SetLength(385L * 1024L * 1024L);
                DateTime futureTime = DateTime.UtcNow.AddDays(30);
                foreach (string file in Directory.GetFiles(crashDir))
                    File.SetLastWriteTimeUtc(file, futureTime);
                Directory.SetLastWriteTimeUtc(crashDir, futureTime);

                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                string report = runtime.ExportLogs();
                string summary = ReadZipText(report, "Unity-Crashes/summary.txt");
                string readme = ReadZipText(report, "Unity-Crashes/MISSING-CRASH-DUMP-README.txt");

                Assert(summary.Contains("SkippedLargeFile:", StringComparison.Ordinal) && summary.Contains("crash.dmp", StringComparison.Ordinal), "Diagnostics crash collection should record when a crash dump exceeds the package budget.");
                Assert(summary.Contains("FilesConsideredTotal:", StringComparison.Ordinal) && summary.Contains("FilesCopiedTotal:", StringComparison.Ordinal), "Diagnostics crash summary should distinguish considered files from copied files.");
                Assert(readme.Contains("Reason: large-file", StringComparison.Ordinal) && readme.Contains("Please send this crash.dmp separately", StringComparison.Ordinal), "Diagnostics crash collection should include player-readable instructions when crash.dmp is too large to package.");
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
                File.WriteAllText(runtime.Paths.PlayerDoctorJsonReportPath, "{\"readOnlyByDesign\":true}", new UTF8Encoding(false));
                File.WriteAllText(runtime.Paths.PlayerDoctorTextReportPath, "DTMAPI Player Doctor read-only", new UTF8Encoding(false));
                File.WriteAllText(runtime.Paths.PlayerDoctorSummaryPath, "status=ready;errors=0;warnings=0", new UTF8Encoding(false));

                string report = runtime.ExportLogs();
                string breadcrumb = ReadZipText(report, "DTMAPI-state/debug-console-last-give.txt");
                Assert(breadcrumb.Contains("item=item_test", StringComparison.Ordinal) && breadcrumb.Contains("requested=10", StringComparison.Ordinal), "In-game diagnostics export should include the bounded debug-console last-give breadcrumb.");
                Assert(ReadZipText(report, "PlayerDoctor/player-doctor.json").Contains("readOnlyByDesign", StringComparison.Ordinal) &&
                    ReadZipText(report, "PlayerDoctor/player-doctor.txt").Contains("read-only", StringComparison.Ordinal) &&
                    ReadZipText(report, "PlayerDoctor/player-doctor-summary.txt").Contains("status=ready", StringComparison.Ordinal),
                    "In-game diagnostics export should include the bounded startup Player Doctor reports.");

                File.WriteAllText(Path.Combine(runtime.Paths.DtmApiPath, "debug-console-last-give.txt"), new string('x', 32 * 1024), new UTF8Encoding(false));
                string oversizedReport = runtime.ExportLogs();
                string skipped = ReadZipText(oversizedReport, "DTMAPI-state/debug-console-last-give.txt.skipped.txt");
                Assert(skipped.Contains("File skipped because it exceeded diagnostic file budget", StringComparison.Ordinal), "In-game diagnostics export should cap debug-console breadcrumb size.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void DiagnosticsServiceCrashDumpPrioritySurvivesFileCountBudget()
        {
            string? previousRoot = UseTempPersistentRoot();
            string crashDir = Path.Combine(Path.GetTempPath(), "RedSawGames", "Doloc Town", "Crashes", "dtmapi-unit-" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(crashDir);
                for (int i = 0; i < 140; i++)
                    File.WriteAllText(Path.Combine(crashDir, "noise-" + i.ToString("000", CultureInfo.InvariantCulture) + ".txt"), "noise", new UTF8Encoding(false));
                File.WriteAllBytes(Path.Combine(crashDir, "crash.dmp"), new byte[] { 1, 2, 3, 4 });
                File.WriteAllText(Path.Combine(crashDir, "error.log"), "unit fatal", new UTF8Encoding(false));

                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                string report = runtime.ExportLogs();
                string[] entries = ReadZipEntryNames(report);
                string summary = ReadZipText(report, "Unity-Crashes/summary.txt");

                Assert(entries.Any(entry => entry.EndsWith("/crash.dmp", StringComparison.OrdinalIgnoreCase)), "Diagnostics crash collection should prioritize crash.dmp even when the crash directory contains more files than the collection budget.");
                Assert(summary.Contains("RedSawGames", StringComparison.Ordinal) && summary.Contains("Doloc Town", StringComparison.Ordinal), "Diagnostics crash collection should inspect the spaced Unity crash root variant.");
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

                Assert(summary.Contains("MissingCrashDumpFile:", StringComparison.Ordinal) && summary.Contains("crash.dmp", StringComparison.Ordinal), "Diagnostics crash collection should record when a Unity crash directory has no crash.dmp.");
                Assert(readme.Contains("Reason: missing-file", StringComparison.Ordinal) && readme.Contains("Please send this crash.dmp separately", StringComparison.Ordinal), "Diagnostics crash collection should include player-readable instructions when the crash dump is missing.");
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

        private static void DiagnosticsServiceLatestCrashWithoutDumpSurvivesOlderDumpBudget()
        {
            string? previousRoot = UseTempPersistentRoot();
            var createdDirs = new List<string>();
            string testId = "dtmapi-unit-latest-nodump-" + Guid.NewGuid().ToString("N");
            string compactRoot = Path.Combine(Path.GetTempPath(), "RedSawGames", "DolocTown", "Crashes");
            string spacedRoot = Path.Combine(Path.GetTempPath(), "RedSawGames", "Doloc Town", "Crashes");
            string latestNoDumpDir = Path.Combine(compactRoot, testId + "-latest");
            try
            {
                DateTime baseTime = DateTime.UtcNow.AddDays(60);
                for (int i = 0; i < 8; i++)
                {
                    string root = i % 2 == 0 ? compactRoot : spacedRoot;
                    string crashDir = Path.Combine(root, testId + "-old-dump-" + i.ToString("00", CultureInfo.InvariantCulture));
                    createdDirs.Add(crashDir);
                    Directory.CreateDirectory(crashDir);
                    File.WriteAllBytes(Path.Combine(crashDir, "crash.dmp"), new byte[] { 1, 2, 3, 4 });
                    File.WriteAllText(Path.Combine(crashDir, "error.log"), "old dump " + i.ToString(CultureInfo.InvariantCulture), new UTF8Encoding(false));
                    Directory.SetLastWriteTimeUtc(crashDir, baseTime.AddMinutes(i));
                    File.SetLastWriteTimeUtc(Path.Combine(crashDir, "crash.dmp"), baseTime.AddMinutes(i));
                    File.SetLastWriteTimeUtc(Path.Combine(crashDir, "error.log"), baseTime.AddMinutes(i));
                }

                createdDirs.Add(latestNoDumpDir);
                Directory.CreateDirectory(latestNoDumpDir);
                File.WriteAllText(Path.Combine(latestNoDumpDir, "error.log"), "latest crash without dump", new UTF8Encoding(false));
                Directory.SetLastWriteTimeUtc(latestNoDumpDir, baseTime.AddMinutes(120));
                File.SetLastWriteTimeUtc(Path.Combine(latestNoDumpDir, "error.log"), baseTime.AddMinutes(120));

                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                string report = runtime.ExportLogs();
                string summary = ReadZipText(report, "Unity-Crashes/summary.txt");
                string readme = ReadZipText(report, "Unity-Crashes/MISSING-CRASH-DUMP-README.txt");

                Assert(summary.Contains("CrashDirectorySelectionMode: latest-per-root-plus-latest-then-dump-priority", StringComparison.Ordinal), "Diagnostics crash collection should document that newest crash directories are protected before dump-priority fill.");
                Assert(summary.Contains("Directory: " + latestNoDumpDir, StringComparison.OrdinalIgnoreCase), "Diagnostics crash collection should keep the newest crash directory even when it has no crash.dmp and older dump directories exceed the budget.");
                Assert(summary.Contains("MissingCrashDumpFile: " + Path.Combine(latestNoDumpDir, "crash.dmp"), StringComparison.OrdinalIgnoreCase), "Diagnostics crash collection should record the newest no-dump crash directory as missing a dump.");
                Assert(readme.Contains(latestNoDumpDir, StringComparison.OrdinalIgnoreCase) && readme.Contains("Reason: missing-file", StringComparison.Ordinal), "Diagnostics missing-dump README should mention the newest no-dump crash directory.");
            }
            finally
            {
                foreach (string dir in createdDirs)
                    DeleteDirectoryQuietly(dir);
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
                    dump.SetLength(129L * 1024L * 1024L);
                File.WriteAllText(Path.Combine(crashDir, "error.log"), "unit fatal after dump", new UTF8Encoding(false));
                Directory.SetLastWriteTimeUtc(crashDir, DateTime.UtcNow.AddMinutes(12));

                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                string report = runtime.ExportLogs();
                string[] entries = ReadZipEntryNames(report);
                string summary = ReadZipText(report, "Unity-Crashes/summary.txt");

                Assert(entries.Any(entry => entry.StartsWith("Unity-Crashes/", StringComparison.Ordinal) && entry.EndsWith("/crash.dmp", StringComparison.OrdinalIgnoreCase)), "Diagnostics crash collection should include a primary crash.dmp that fits the dump budget.");
                Assert(entries.Any(entry => entry.StartsWith("Unity-Crashes/", StringComparison.Ordinal) && entry.EndsWith("/error.log", StringComparison.OrdinalIgnoreCase)), "Diagnostics crash collection should keep error.log after accepting a primary crash.dmp by expanding the total evidence budget.");
                Assert(summary.Contains("MaxTotalBytesWithDump:", StringComparison.Ordinal) && summary.Contains("CopiedFile:", StringComparison.Ordinal) && !summary.Contains("SkippedTotalByteLimit: " + Path.Combine(crashDir, "error.log"), StringComparison.Ordinal), "Diagnostics crash summary should show dump-aware budget handling without starving text evidence.");
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
                    1,
                    0,
                    0,
                    0,
                    0,
                    1,
                    0,
                    0,
                    Array.Empty<string>(),
                    0,
                    0,
                    0);

                DtmManagerViewModel model = DtmManagerViewModelFactory.FromSnapshot(snapshot, null, contentRegistry);

                Assert(model.Mods.Count == 5, "Manager model should map mod status rows.");
                Assert(model.Mods[0].UniqueID == "Blocked.Mod" && model.Mods[1].UniqueID == "Warning.Mod" && model.Mods[2].UniqueID == "Disabled.Mod" && model.Mods[3].UniqueID == "Example.Mod" && model.Mods[4].UniqueID == "ReasonOnly.Mod", "Manager mods should sort blocked, warning, disabled, then loaded rows using structured status.");
                Assert(model.Mods[3].StatusCode == "loaded", "Manager mod row should keep structured status code.");
                Assert(model.Mods[3].RootPath == longRootPath, "Manager mod row should preserve long root paths.");
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
                Assert(ManagerPageRowFormatter.FormatShowingFirst("Rows", 99, 3) == "Rows: showing first 3 of 3", "Manager formatter should clamp showing-first counts to total.");
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
                Assert(restartRequiredRow.RequiresRestart && restartRequiredRow.RestartHint.Contains("Restart Doloc Town"), "Manager should expose an actionable restart hint for restart-required Mod state.");

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

        private static void DiagnosticGraphsAndLogOnceKeysAreBounded()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                var paths = new RuntimePaths(dir, dir);
                var diagnostics = new DiagnosticsService(paths);
                diagnostics.SetHookStatus("other/trimmed", "user-value", "test", "ordinary hook ID");
                diagnostics.SetFeatureStatus("other/trimmed", "user-value", "test", true, 0, string.Empty, "ordinary feature ID");
                for (int i = 0; i < 700; i++)
                {
                    diagnostics.SetHookStatus("hook-" + i, "ok", "test", string.Empty);
                    diagnostics.SetFeatureStatus("feature-" + i, "ok", "test", true, 0, string.Empty, string.Empty);
                    diagnostics.RecordError("owner-" + i, "message-" + i, "scalar");
                }
                Assert(diagnostics.RetainedHookStatusCount <= 512 && diagnostics.TrimmedHookStatusCount > 0, "Hook status graph must have a fixed retained cap and trimmed aggregate.");
                Assert(diagnostics.RetainedFeatureStatusCount <= 512 && diagnostics.TrimmedFeatureStatusCount > 0, "Feature status graph must have a fixed retained cap and trimmed aggregate.");
                Assert(diagnostics.RetainedDiagnosticAggregateCount <= 512 && diagnostics.TrimmedDiagnosticAggregateCount > 0, "Diagnostic aggregate graph must have a fixed retained cap and trimmed aggregate.");
                Assert(diagnostics.GetHookStatuses().Any(status => status.HookId == "other/trimmed" && status.Status == "user-value") && diagnostics.GetHookStatuses().Any(status => status.HookId == "other/trimmed" && status.Status == "trimmed"), "Hook overflow aggregation must not overwrite an ordinary caller using the display ID.");
                Assert(diagnostics.GetFeatureStatuses().Any(status => status.FeatureId == "other/trimmed" && status.Status == "user-value") && diagnostics.GetFeatureStatuses().Any(status => status.FeatureId == "other/trimmed" && status.Status == "trimmed"), "Feature overflow aggregation must stay outside the ordinary caller key space.");

                var monitor = new FileMonitor(new FakeHost(dir), "DTMAPI.Tests.LogOnceCap", Path.Combine(dir, "logonce.log"));
                for (int i = 0; i < 400; i++)
                    monitor.LogOnce("key-" + i, "message-" + i);
                Assert(monitor.RetainedLogOnceKeyCount == 256 && monitor.SuppressedLogOnceKeyCount == 144, "Each monitor must retain at most 256 LogOnce keys and count suppressed unique keys.");

                var ledger = new ModOwnerLedgerService();
                ledger.RecordCleanup("owner-success", "GameBridgeOwnerResource", 1, "phase", "failedSteps=none");
                Assert(ledger.GetSnapshot().CleanupFailures == 0, "Successful cleanup details containing a word like failedSteps must not be inferred as a cleanup failure.");
                ledger.RecordCleanup("owner-failure", "GameBridgeOwnerResource", 0, "phase", "explicit failure", success: false);
                Assert(ledger.GetSnapshot().CleanupFailures == 1, "Cleanup failures must be recorded through an explicit structured signal.");
                for (int i = 0; i < 300; i++)
                {
                    ledger.RecordRegistration("owner-" + i, "UniqueKind-" + i, "key", "phase", "cleanup");
                    ledger.RecordNeedsRestart("owner-" + i, "key", "phase", "failure-" + i);
                }
                ModOwnerLedgerSnapshot snapshot = ledger.GetSnapshot();
                Assert(snapshot.Entries.Count == 64 && snapshot.RegistrationCounts.Count == 1 && snapshot.RegistrationCounts.ContainsKey("Other"), "Owner ledger must retain only 64 scalar failures and fixed registration-kind counters.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void DiagnosticRetainedStringsAndTrimmedBytesAreBounded()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                var paths = new RuntimePaths(dir, dir);
                paths.Ensure();
                var diagnostics = new DiagnosticsService(paths);
                string ownerPrefix = new string('O', 4096);
                string messagePrefix = new string('M', 8192);
                string detailsPrefix = new string('D', 16384);
                string ownerA = ownerPrefix + "owner-tail-A";
                string ownerB = ownerPrefix + "owner-tail-B";
                string messageA = messagePrefix + "message-tail-A";
                string messageB = messagePrefix + "message-tail-B";
                string detailsA = detailsPrefix + "details-tail-A";
                string detailsB = detailsPrefix + "details-tail-B";

                var directError = new DtmErrorInfo(ownerA, messageA, detailsA);
                var directWarning = new DtmWarningInfo(ownerA, messageA, detailsA);
                var directHook = new HookStatusInfo(new string('H', 4096), new string('S', 4096), new string('R', 4096), detailsA);
                var directFeature = new DtmFeatureStatusInfo(new string('F', 4096), new string('T', 4096), new string('P', 4096), false, 1, messageA, detailsA);
                Assert(directError.Owner.Length <= BoundedDiagnosticScalar.OwnerIdChars && directError.Message.Length <= BoundedDiagnosticScalar.MessageChars && directError.Details.Length <= BoundedDiagnosticScalar.DetailsChars &&
                    directWarning.Owner.Length <= BoundedDiagnosticScalar.OwnerIdChars && directWarning.Message.Length <= BoundedDiagnosticScalar.MessageChars && directWarning.Details.Length <= BoundedDiagnosticScalar.DetailsChars &&
                    directHook.HookId.Length <= BoundedDiagnosticScalar.IdentifierChars && directHook.Status.Length <= BoundedDiagnosticScalar.ShortTextChars && directHook.Source.Length <= BoundedDiagnosticScalar.ShortTextChars && directHook.Details.Length <= BoundedDiagnosticScalar.DetailsChars &&
                    directFeature.FeatureId.Length <= BoundedDiagnosticScalar.IdentifierChars && directFeature.Status.Length <= BoundedDiagnosticScalar.ShortTextChars && directFeature.LastOperation.Length <= BoundedDiagnosticScalar.ShortTextChars && directFeature.LastError.Length <= BoundedDiagnosticScalar.MessageChars && directFeature.Details.Length <= BoundedDiagnosticScalar.DetailsChars,
                    "Direct diagnostic DTO construction must defensively apply the same centralized scalar bounds as the retained service path.");
                var foreignError = new UnboundedErrorInfo(ownerA, messageA, detailsA);
                var defensiveSnapshot = new DtmDiagnosticsSnapshot(
                    DateTimeOffset.Now,
                    Array.Empty<IDtmLoadedModInfo>(),
                    Array.Empty<IDtmModStatusInfo>(),
                    Enumerable.Repeat<IDtmErrorInfo>(foreignError, BoundedDiagnosticScalar.MaxDiagnosticRows + 25).ToArray(),
                    Array.Empty<IDtmWarningInfo>(),
                    Array.Empty<IHookStatusInfo>(),
                    Array.Empty<IDtmFeatureStatusInfo>(),
                    new string('L', 4096),
                    new string('R', 4096));
                Assert(defensiveSnapshot.Errors.Count == BoundedDiagnosticScalar.MaxDiagnosticRows && !ReferenceEquals(defensiveSnapshot.Errors[0], foreignError), "Diagnostics snapshots must cap and defensively project caller-supplied interface rows instead of retaining their arbitrary object graph.");
                Assert(defensiveSnapshot.Errors.All(error => error.Owner.Length <= BoundedDiagnosticScalar.OwnerIdChars && error.Message.Length <= BoundedDiagnosticScalar.MessageChars && error.Details.Length <= BoundedDiagnosticScalar.DetailsChars) &&
                    defensiveSnapshot.LatestLogPath.Length <= BoundedDiagnosticScalar.DetailsChars && defensiveSnapshot.LatestReportPath.Length <= BoundedDiagnosticScalar.DetailsChars,
                    "Diagnostics snapshot rows and path fields must remain bounded even when a foreign implementation supplies long values.");

                diagnostics.RecordError(ownerA, messageA, detailsA);
                diagnostics.RecordError(ownerB, messageB, detailsB);
                MethodInfo recordWarning = diagnostics.GetType().GetMethod("RecordWarning", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?? throw new InvalidOperationException("DiagnosticsService.RecordWarning should remain available for bounded warning coverage.");
                recordWarning.Invoke(diagnostics, new object[] { ownerA, messageB, detailsA });
                diagnostics.SetHookStatus(new string('H', 4096), new string('S', 4096), new string('R', 4096), detailsA);
                diagnostics.SetFeatureStatus(new string('F', 4096), new string('T', 4096), new string('P', 4096), false, 1, messageA, detailsB);

                IReadOnlyList<IDtmErrorInfo> errors = diagnostics.GetErrors();
                Assert(errors.Count == 2 && errors.All(error =>
                    error.Owner.Length <= BoundedDiagnosticScalar.OwnerIdChars &&
                    error.Message.Length <= BoundedDiagnosticScalar.MessageChars &&
                    error.Details.Length <= BoundedDiagnosticScalar.DetailsChars),
                    "Every retained diagnostic error scalar must stay within its centralized field limit.");
                Assert(errors.All(error => error.Owner.Contains("~[sha256:", StringComparison.Ordinal) && error.Message.Contains("~[sha256:", StringComparison.Ordinal) && error.Details.Contains("~[sha256:", StringComparison.Ordinal)), "Every truncated diagnostic scalar should retain a stable hash suffix.");
                Assert(!string.Equals(errors[0].Owner, errors[1].Owner, StringComparison.Ordinal) && !string.Equals(errors[0].Message, errors[1].Message, StringComparison.Ordinal), "Long values with identical retained prefixes but different tails must remain distinct through their stable hashes.");
                IDtmWarningInfo warning = diagnostics.GetWarnings().Single();
                Assert(warning.Owner.Length <= BoundedDiagnosticScalar.OwnerIdChars && warning.Message.Length <= BoundedDiagnosticScalar.MessageChars && warning.Details.Length <= BoundedDiagnosticScalar.DetailsChars, "Retained warning fields must use the same centralized bounds as errors.");
                Assert(diagnostics.RetainedDiagnosticAggregateCount == 3, "Bounded aggregate keys must distinguish error/warning kind and different long owner/message values without retaining their full input.");

                IHookStatusInfo hook = diagnostics.GetHookStatuses().Single();
                Assert(hook.HookId.Length <= BoundedDiagnosticScalar.IdentifierChars && hook.Status.Length <= BoundedDiagnosticScalar.ShortTextChars && hook.Source.Length <= BoundedDiagnosticScalar.ShortTextChars && hook.Details.Length <= BoundedDiagnosticScalar.DetailsChars, "Hook-status keys and values must use centralized scalar bounds.");
                IDtmFeatureStatusInfo feature = diagnostics.GetFeatureStatuses().Single();
                Assert(feature.FeatureId.Length <= BoundedDiagnosticScalar.IdentifierChars && feature.Status.Length <= BoundedDiagnosticScalar.ShortTextChars && feature.LastOperation.Length <= BoundedDiagnosticScalar.ShortTextChars && feature.LastError.Length <= BoundedDiagnosticScalar.MessageChars && feature.Details.Length <= BoundedDiagnosticScalar.DetailsChars, "Feature-status keys and values must use centralized scalar bounds.");
                Assert(diagnostics.TrimmedDiagnosticBytes > 0, "Diagnostics should expose a positive saturating trimmed-byte count after truncating retained scalars.");

                DiscoveredMod longOwnerMod = CreateDiscoveredContentPack(Path.Combine(dir, "LongOwnerStatus"), ownerA, new string('N', 4096));
                IDtmModStatusInfo longOwnerStatus = RuntimeSnapshotFactory.CreateModStatusSnapshot(
                    new[] { longOwnerMod },
                    Array.Empty<DiscoveredMod>(),
                    diagnostics.GetErrors()).Single();
                Assert(longOwnerStatus.Status == "error" && longOwnerStatus.UniqueID.Length <= BoundedDiagnosticScalar.OwnerIdChars, "A bounded display owner must retain fixed SHA-256 identity association with its full manifest owner in diagnostics status rows.");
                var projectedDiagnostics = new DtmDiagnosticsSnapshot(
                    DateTimeOffset.Now,
                    Array.Empty<IDtmLoadedModInfo>(),
                    Array.Empty<IDtmModStatusInfo>(),
                    diagnostics.GetErrors(),
                    diagnostics.GetWarnings(),
                    diagnostics.GetHookStatuses(),
                    diagnostics.GetFeatureStatuses(),
                    string.Empty,
                    string.Empty);
                IDtmModStatusInfo projectedLongOwnerStatus = RuntimeSnapshotFactory.CreateModStatusSnapshot(
                    new[] { longOwnerMod },
                    Array.Empty<DiscoveredMod>(),
                    projectedDiagnostics.Errors).Single();
                Assert(projectedLongOwnerStatus.Status == "error", "Defensive snapshot projection must preserve the full-owner correlation identity of retained diagnostic rows.");
                Assert(longOwnerStatus.Name.Length <= BoundedDiagnosticScalar.MessageChars && longOwnerStatus.Reason.Length <= BoundedDiagnosticScalar.DetailsChars && longOwnerStatus.ManifestPath.Length <= BoundedDiagnosticScalar.DetailsChars && longOwnerStatus.RootPath.Length <= BoundedDiagnosticScalar.DetailsChars, "Diagnostics snapshot model fields must remain bounded end-to-end.");
                var loadedInfo = new DtmLoadedModInfo(new ManifestModel
                {
                    UniqueID = ownerA,
                    Name = new string('L', 4096),
                    Version = new string('V', 4096),
                    Type = new string('T', 4096),
                    EntryType = new string('E', 4096)
                });
                Assert(loadedInfo.UniqueID.Length <= BoundedDiagnosticScalar.OwnerIdChars && loadedInfo.Name.Length <= BoundedDiagnosticScalar.MessageChars && loadedInfo.Version.Length <= BoundedDiagnosticScalar.IdentifierChars && loadedInfo.Type.Length <= BoundedDiagnosticScalar.IdentifierChars && loadedInfo.EntryType.Length <= BoundedDiagnosticScalar.MessageChars, "Loaded-Mod diagnostic snapshot fields must use the same fixed scalar bounds.");

                long firstTrimmed = 0;
                long secondTrimmed = 0;
                string firstBounded = BoundedDiagnosticScalar.Sanitize(detailsA, BoundedDiagnosticScalar.MessageChars, ref firstTrimmed);
                string secondBounded = BoundedDiagnosticScalar.Sanitize(detailsA, BoundedDiagnosticScalar.MessageChars, ref secondTrimmed);
                Assert(firstBounded == secondBounded && firstTrimmed == secondTrimmed && firstTrimmed == (long)(detailsA.Length - BoundedDiagnosticScalar.MessageChars) * sizeof(char), "The centralized sanitizer should produce a deterministic hash suffix and exact UTF-16 trimmed-byte count.");
                long streamedTrimmed = 0;
                string streamedBounded;
                using (var reader = new StringReader(detailsA))
                    streamedBounded = BoundedDiagnosticScalar.Sanitize(reader, BoundedDiagnosticScalar.MessageChars, ref streamedTrimmed);
                Assert(streamedBounded == firstBounded && streamedTrimmed == firstTrimmed, "Streaming pointer-file truncation must match the centralized scalar hash and trimmed-byte accounting without retaining the full input.");

                string latestLogTail = "latest-log-tail";
                string latestReportTail = "latest-report-tail";
                string installVersionTail = "install-version-tail";
                diagnostics.LatestLogPath = new string('L', 4096) + latestLogTail;
                File.WriteAllText(Path.Combine(paths.ReportsPath, "latest-report.txt"), new string('R', 4096) + latestReportTail);
                File.WriteAllText(Path.Combine(paths.DtmApiPath, "install-state.json"), "{\"DTMAPIVersion\":\"" + new string('V', 4096) + installVersionTail + "\"}");
                MethodInfo buildSummary = typeof(DiagnosticsService).GetMethod("BuildSummary", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?? throw new InvalidOperationException("DiagnosticsService.BuildSummary should remain available for bounded summary coverage.");
                string pathAndStateSummary = (string)(buildSummary.Invoke(diagnostics, new object[] { string.Empty }) ?? string.Empty);
                Assert(pathAndStateSummary.Length <= BoundedDiagnosticScalar.ReportSummaryChars && pathAndStateSummary.Contains("~[sha256:", StringComparison.Ordinal), "Diagnostic summary path and install-state scalars must be bounded with stable hash suffixes.");
                Assert(!pathAndStateSummary.Contains(latestLogTail, StringComparison.Ordinal) && !pathAndStateSummary.Contains(latestReportTail, StringComparison.Ordinal) && !pathAndStateSummary.Contains(installVersionTail, StringComparison.Ordinal), "Diagnostic summary must not retain discarded path or install-state tails.");
                diagnostics.LatestLogPath = string.Empty;
                for (int i = 0; i < 300; i++)
                    diagnostics.RecordError("summary-owner", "summary-message", new string('Z', BoundedDiagnosticScalar.DetailsChars));

                string runtimeContext = new string('X', BoundedDiagnosticScalar.RuntimeContextChars + 4096) + "runtime-context-tail";
                string report = diagnostics.ExportLogs(runtimeContext);
                string summary = ReadZipText(report, "dtmapi-summary.txt");
                string boundedContext = ReadZipText(report, "DTMAPI-runtime-context.txt");
                Assert(summary.Length <= BoundedDiagnosticScalar.ReportSummaryChars && summary.Contains("DiagnosticsScalarTrimmedBytes: ", StringComparison.Ordinal) && summary.Contains("DiagnosticsSummaryTrimmedBytes: ", StringComparison.Ordinal) && !summary.Contains("DiagnosticsSummaryTrimmedBytes: 0000000000000000000.", StringComparison.Ordinal) && summary.Contains("~[sha256:", StringComparison.Ordinal), "Diagnostic reports should expose scalar and total-summary trimming while retaining a fixed total character budget and hash suffix.");
                Assert(!summary.Contains("owner-tail-A", StringComparison.Ordinal) && !summary.Contains("message-tail-A", StringComparison.Ordinal) && !summary.Contains("details-tail-A", StringComparison.Ordinal), "Diagnostic reports must not retain discarded scalar tails.");
                Assert(boundedContext.Length <= BoundedDiagnosticScalar.RuntimeContextChars && boundedContext.Contains("~[sha256:", StringComparison.Ordinal) && !boundedContext.Contains("runtime-context-tail", StringComparison.Ordinal), "The exported runtime context must have a fixed total character bound and hash the discarded tail.");

                var ledger = new ModOwnerLedgerService();
                ledger.RecordNeedsRestart(ownerA, messageA, new string('P', 4096), detailsA);
                ledger.RecordCleanup(ownerB, "GameBridgeOwnerResource", 0, new string('Q', 4096), detailsB, success: false);
                ledger.RecordConfigPreview(ownerA, new string('I', 4096), new string('K', 4096), new string('C', 4096), false, detailsB);
                ModOwnerLedgerSnapshot ledgerSnapshot = ledger.GetSnapshot();

                Assert(ledgerSnapshot.Entries.Count == 3 && ledgerSnapshot.Entries.All(entry =>
                    entry.OwnerId.Length <= BoundedDiagnosticScalar.OwnerIdChars &&
                    entry.TransactionId.Length <= BoundedDiagnosticScalar.IdentifierChars &&
                    entry.Kind.Length <= BoundedDiagnosticScalar.IdentifierChars &&
                    entry.Key.Length <= BoundedDiagnosticScalar.ShortTextChars &&
                    entry.Phase.Length <= BoundedDiagnosticScalar.ShortTextChars &&
                    entry.Status.Length <= BoundedDiagnosticScalar.IdentifierChars &&
                    entry.RollbackResult.Length <= BoundedDiagnosticScalar.DetailsChars),
                    "Every retained owner-ledger failure field must stay within the centralized scalar limits.");
                Assert(ledgerSnapshot.ConfigPreviewAggregates.Single().Kind.Length <= BoundedDiagnosticScalar.IdentifierChars && ledgerSnapshot.ConfigPreviewAggregates.Single().Operation.Length <= BoundedDiagnosticScalar.ShortTextChars, "Config-preview aggregate keys must be bounded before dictionary retention.");
                Assert(ledgerSnapshot.TrimmedBytes > 0 && ledgerSnapshot.FormatSummary().Contains("trimmedBytes=" + ledgerSnapshot.TrimmedBytes.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal), "Owner-ledger snapshots should report their saturating trimmed-byte count.");
                Assert(ledgerSnapshot.Entries.All(entry => entry.OwnerId.Contains("~[sha256:", StringComparison.Ordinal) || entry.RollbackResult.Contains("~[sha256:", StringComparison.Ordinal)), "Owner-ledger truncation should preserve a stable hash without retaining discarded tails.");
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

        private sealed class UnboundedErrorInfo : IDtmErrorInfo
        {
            public UnboundedErrorInfo(string owner, string message, string details)
            {
                Time = DateTimeOffset.Now;
                Owner = owner;
                Message = message;
                Details = details;
            }

            public DateTimeOffset Time { get; }
            public string Owner { get; }
            public string Message { get; }
            public string Details { get; }
        }
    }
}
