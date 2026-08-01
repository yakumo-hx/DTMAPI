using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using DTMAPI.Abstractions;
using DTMAPI.Core.Logging;
using DTMAPI.Core.Runtime;

namespace DTMAPI.Core.Diagnostics
{
    public sealed class DiagnosticsService : IDiagnosticsHelper
    {
        private const int MaxDiagnosticEntriesPerKind = BoundedDiagnosticScalar.MaxDiagnosticRows;
        private const int MaxHookStatuses = BoundedDiagnosticScalar.MaxStatusRows;
        private const int MaxFeatureStatuses = BoundedDiagnosticScalar.MaxStatusRows;
        private const int MaxDiagnosticAggregates = 512;
        private const int MaxJsonStatusInputChars = 512 * 1024;
        private const int MaxUnityCrashReportDirectories = 6;
        private const int MaxUnityCrashReportFilesPerDirectory = 80;
        private const int MaxUnityCrashReportFilesTotal = 120;
        private const long MaxUnityCrashReportFileBytes = 96L * 1024L * 1024L;
        private const long MaxUnityCrashReportDumpFileBytes = 384L * 1024L * 1024L;
        private const long MaxUnityCrashReportTotalBytes = 128L * 1024L * 1024L;
        private const long MaxUnityCrashReportTotalBytesWithDump = 512L * 1024L * 1024L;
        private static readonly string[] PreferredUnityCrashReportFiles =
        {
            "crash.dmp",
            "error.log",
            "Player.log",
            "Player-prev.log"
        };
        private readonly RuntimePaths paths;
        private readonly List<DtmErrorInfo> errors = new List<DtmErrorInfo>();
        private readonly List<DtmWarningInfo> warnings = new List<DtmWarningInfo>();
        private readonly Dictionary<string, HookStatusInfo> hooks = new Dictionary<string, HookStatusInfo>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, DtmFeatureStatusInfo> features = new Dictionary<string, DtmFeatureStatusInfo>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, DiagnosticAggregateCounter> diagnosticCounters = new Dictionary<string, DiagnosticAggregateCounter>(StringComparer.Ordinal);
        private HookStatusInfo? trimmedHookStatus;
        private DtmFeatureStatusInfo? trimmedFeatureStatus;
        private DiagnosticAggregateCounter? trimmedDiagnosticCounter;
        private readonly object gate = new object();
        private int trimmedErrorCount;
        private int trimmedWarningCount;
        private int trimmedHookStatusCount;
        private int trimmedFeatureStatusCount;
        private int trimmedDiagnosticAggregateCount;
        private long trimmedDiagnosticBytes;
        private string latestLogPath = string.Empty;
        private string latestReportPath = string.Empty;

        public DiagnosticsService(RuntimePaths paths)
        {
            this.paths = paths;
        }

        internal IDiagnosticsHelper CreateOwnerBound(string ownerId, Action ensureRuntimeThread, Action ensureOwnerActive)
        {
            if (string.IsNullOrWhiteSpace(ownerId))
                throw new ArgumentException("Owner id is required.", nameof(ownerId));
            return new OwnerBoundDiagnosticsHelper(
                this,
                ensureRuntimeThread ?? throw new ArgumentNullException(nameof(ensureRuntimeThread)),
                ensureOwnerActive ?? throw new ArgumentNullException(nameof(ensureOwnerActive)));
        }

        public string LatestLogPath
        {
            get { lock (gate) return latestLogPath; }
            set
            {
                long trimmed = 0;
                string safeValue = BoundedDiagnosticScalar.Sanitize(value, BoundedDiagnosticScalar.DetailsChars, ref trimmed);
                lock (gate)
                {
                    BoundedDiagnosticScalar.AddTrimmedBytes(ref trimmedDiagnosticBytes, trimmed);
                    latestLogPath = safeValue;
                }
            }
        }

        public string LatestReportPath
        {
            get { lock (gate) return latestReportPath; }
            private set
            {
                long trimmed = 0;
                string safeValue = BoundedDiagnosticScalar.Sanitize(value, BoundedDiagnosticScalar.DetailsChars, ref trimmed);
                lock (gate)
                {
                    BoundedDiagnosticScalar.AddTrimmedBytes(ref trimmedDiagnosticBytes, trimmed);
                    latestReportPath = safeValue;
                }
            }
        }

        public IReadOnlyList<IDtmErrorInfo> GetErrors()
        {
            lock (gate)
                return errors.Cast<IDtmErrorInfo>().ToArray();
        }

        public IReadOnlyList<IDtmWarningInfo> GetWarnings()
        {
            lock (gate)
                return warnings.Cast<IDtmWarningInfo>().ToArray();
        }

        public IReadOnlyList<IHookStatusInfo> GetHookStatuses()
        {
            lock (gate)
                return hooks.Values
                    .Concat(trimmedHookStatus == null ? Enumerable.Empty<HookStatusInfo>() : new[] { trimmedHookStatus! })
                    .OrderBy(h => h.HookId, StringComparer.OrdinalIgnoreCase)
                    .Cast<IHookStatusInfo>()
                    .ToArray();
        }

        public IReadOnlyList<IDtmFeatureStatusInfo> GetFeatureStatuses()
        {
            lock (gate)
                return features.Values
                    .Concat(trimmedFeatureStatus == null ? Enumerable.Empty<DtmFeatureStatusInfo>() : new[] { trimmedFeatureStatus! })
                    .OrderBy(f => f.FeatureId, StringComparer.OrdinalIgnoreCase)
                    .Cast<IDtmFeatureStatusInfo>()
                    .ToArray();
        }

        public string GetLatestLogPath() => LatestLogPath;

        public string GetLatestReportPath()
        {
            if (!string.IsNullOrWhiteSpace(LatestReportPath))
                return LatestReportPath;

            string pointer = Path.Combine(paths.ReportsPath, "latest-report.txt");
            if (!File.Exists(pointer))
                return string.Empty;

            try
            {
                long trimmed = 0;
                string safePath;
                using (var reader = new StreamReader(pointer, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
                    safePath = BoundedDiagnosticScalar.Sanitize(reader, BoundedDiagnosticScalar.DetailsChars, ref trimmed).Trim();
                lock (gate)
                    BoundedDiagnosticScalar.AddTrimmedBytes(ref trimmedDiagnosticBytes, trimmed);
                return safePath;
            }
            catch
            {
                return string.Empty;
            }
        }

        public void RecordError(string owner, string message, string details)
        {
            long trimmed = 0;
            string ownerIdentity = BoundedDiagnosticScalar.StableIdentity(owner);
            string safeOwner = BoundedDiagnosticScalar.Sanitize(owner, BoundedDiagnosticScalar.OwnerIdChars, ref trimmed);
            string safeMessage = BoundedDiagnosticScalar.Sanitize(message, BoundedDiagnosticScalar.MessageChars, ref trimmed);
            string safeDetails = BoundedDiagnosticScalar.Sanitize(details, BoundedDiagnosticScalar.DetailsChars, ref trimmed);
            lock (gate)
            {
                BoundedDiagnosticScalar.AddTrimmedBytes(ref trimmedDiagnosticBytes, trimmed);
                RecordDiagnosticCounter("Error", safeOwner, safeMessage, safeDetails);
                AddBounded(errors, new DtmErrorInfo(safeOwner, safeMessage, safeDetails, ownerIdentity), ref trimmedErrorCount);
            }
        }

        internal void RecordWarning(string owner, string message, string details)
        {
            long trimmed = 0;
            string ownerIdentity = BoundedDiagnosticScalar.StableIdentity(owner);
            string safeOwner = BoundedDiagnosticScalar.Sanitize(owner, BoundedDiagnosticScalar.OwnerIdChars, ref trimmed);
            string safeMessage = BoundedDiagnosticScalar.Sanitize(message, BoundedDiagnosticScalar.MessageChars, ref trimmed);
            string safeDetails = BoundedDiagnosticScalar.Sanitize(details, BoundedDiagnosticScalar.DetailsChars, ref trimmed);
            lock (gate)
            {
                BoundedDiagnosticScalar.AddTrimmedBytes(ref trimmedDiagnosticBytes, trimmed);
                RecordDiagnosticCounter("Warning", safeOwner, safeMessage, safeDetails);
                AddBounded(warnings, new DtmWarningInfo(safeOwner, safeMessage, safeDetails, ownerIdentity), ref trimmedWarningCount);
            }
        }

        public bool SetHookStatus(string hookId, string status, string source, string details)
        {
            long trimmed = 0;
            string safeHookId = BoundedDiagnosticScalar.Sanitize(hookId, BoundedDiagnosticScalar.IdentifierChars, ref trimmed);
            string safeStatus = BoundedDiagnosticScalar.Sanitize(status, BoundedDiagnosticScalar.ShortTextChars, ref trimmed);
            string safeSource = BoundedDiagnosticScalar.Sanitize(source, BoundedDiagnosticScalar.ShortTextChars, ref trimmed);
            string safeDetails = BoundedDiagnosticScalar.Sanitize(details, BoundedDiagnosticScalar.DetailsChars, ref trimmed);
            lock (gate)
            {
                BoundedDiagnosticScalar.AddTrimmedBytes(ref trimmedDiagnosticBytes, trimmed);
                if (hooks.TryGetValue(safeHookId, out HookStatusInfo existing) &&
                    existing.Status == safeStatus &&
                    existing.Source == safeSource &&
                    existing.Details == safeDetails)
                    return false;
                if (!hooks.ContainsKey(safeHookId) && hooks.Count >= MaxHookStatuses - 1)
                {
                    trimmedHookStatusCount++;
                    trimmedHookStatus = new HookStatusInfo("other/trimmed", "trimmed", "DiagnosticsService", "Additional unique hook statuses=" + trimmedHookStatusCount + ".");
                    return false;
                }
                hooks[safeHookId] = new HookStatusInfo(safeHookId, safeStatus, safeSource, safeDetails);
                return true;
            }
        }

        public void SetFeatureStatus(string featureId, string status, string lastOperation, bool success, int failureCount, string lastError, string details)
        {
            if (string.IsNullOrWhiteSpace(featureId))
                return;

            long trimmed = 0;
            string safeFeatureId = BoundedDiagnosticScalar.Sanitize(featureId, BoundedDiagnosticScalar.IdentifierChars, ref trimmed);
            string safeStatus = BoundedDiagnosticScalar.Sanitize(status, BoundedDiagnosticScalar.ShortTextChars, ref trimmed);
            string safeOperation = BoundedDiagnosticScalar.Sanitize(lastOperation, BoundedDiagnosticScalar.ShortTextChars, ref trimmed);
            string safeError = BoundedDiagnosticScalar.Sanitize(lastError, BoundedDiagnosticScalar.MessageChars, ref trimmed);
            string safeDetails = BoundedDiagnosticScalar.Sanitize(details, BoundedDiagnosticScalar.DetailsChars, ref trimmed);
            lock (gate)
            {
                BoundedDiagnosticScalar.AddTrimmedBytes(ref trimmedDiagnosticBytes, trimmed);
                if (!features.ContainsKey(safeFeatureId) && features.Count >= MaxFeatureStatuses - 1)
                {
                    trimmedFeatureStatusCount++;
                    trimmedFeatureStatus = new DtmFeatureStatusInfo("other/trimmed", "trimmed", "DiagnosticsService", false, trimmedFeatureStatusCount, "Unique feature status cap reached.", "Additional unique feature statuses are aggregated.");
                    return;
                }
                features[safeFeatureId] = new DtmFeatureStatusInfo(safeFeatureId, safeStatus, safeOperation, success, failureCount, safeError, safeDetails);
            }
        }

        public string ExportLogs()
        {
            return ExportLogs(string.Empty);
        }

        public string ExportLogs(string runtimeContextSummary)
        {
            long trimmed = 0;
            string safeRuntimeContext = BoundedDiagnosticScalar.Sanitize(runtimeContextSummary, BoundedDiagnosticScalar.RuntimeContextChars, ref trimmed);
            lock (gate)
                BoundedDiagnosticScalar.AddTrimmedBytes(ref trimmedDiagnosticBytes, trimmed);
            Directory.CreateDirectory(paths.ReportsPath);
            string stamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss");
            string zipPath = Path.Combine(paths.ReportsPath, $"dtmapi-report-{stamp}.zip");
            LatestReportPath = zipPath;
            using (FileStream file = File.Create(zipPath))
            using (var archive = new ZipArchive(file, ZipArchiveMode.Create))
            {
                AddFileIfExists(archive, LatestLogPath, "DTMAPI-latest.log");
                foreach (string historyLog in LatestLogRotator.GetHistoryFiles(paths.LogsPath))
                    AddFileIfExists(archive, historyLog, "DTMAPI-history/" + Path.GetFileName(historyLog));
                AddFileIfExists(archive, Path.Combine(paths.GamePath, "BepInEx", "LogOutput.log"), "BepInEx-LogOutput.log");
                AddFileIfExists(archive, FindPlayerLogPath(), "Unity-Player.log");
                AddFileIfExists(archive, FindPlayerPreviousLogPath(), "Unity-Player-prev.log");
                AddFileIfExists(archive, Path.Combine(paths.DtmApiPath, "debug-console-last-give.txt"), "DTMAPI-state/debug-console-last-give.txt", 16 * 1024);
                AddFileIfExists(archive, Path.Combine(paths.DtmApiPath, "install-state.json"), "install-state.json");
                AddFileIfExists(archive, Path.Combine(paths.DtmApiPath, "release-manifest.json"), "release-manifest.json");
                AddFileIfExists(archive, paths.PlayerDoctorJsonReportPath, "PlayerDoctor/player-doctor.json", 2 * 1024 * 1024);
                AddFileIfExists(archive, paths.PlayerDoctorTextReportPath, "PlayerDoctor/player-doctor.txt", 2 * 1024 * 1024);
                AddFileIfExists(archive, paths.PlayerDoctorSummaryPath, "PlayerDoctor/player-doctor-summary.txt", 16 * 1024);
                AddRecentFiles(archive, paths.DtmApiPath, "install-state.failed-*.json", "DTMAPI-state", 5);
                AddRecentFiles(archive, paths.DtmApiPath, "uninstall-state-*.json", "DTMAPI-state", 5);
                AddRecentUnityCrashReports(archive);
                if (!string.IsNullOrWhiteSpace(safeRuntimeContext))
                    AddText(archive, "DTMAPI-runtime-context.txt", safeRuntimeContext);
                AddText(archive, "dtmapi-summary.txt", BuildSummary(safeRuntimeContext));
            }

            File.WriteAllText(Path.Combine(paths.ReportsPath, "latest-report.txt"), zipPath);
            return zipPath;
        }

        public void RecordEvidence(string caseId, string summary)
        {
            long trimmed = 0;
            string safeCase = MakeSafeFileName(BoundedDiagnosticScalar.Sanitize(string.IsNullOrWhiteSpace(caseId) ? "CASE" : caseId, BoundedDiagnosticScalar.IdentifierChars, ref trimmed));
            string safeSummary = BoundedDiagnosticScalar.Sanitize(summary, BoundedDiagnosticScalar.RuntimeContextChars, ref trimmed);
            lock (gate)
                BoundedDiagnosticScalar.AddTrimmedBytes(ref trimmedDiagnosticBytes, trimmed);
            string dir = Path.Combine(paths.EvidencePath, safeCase, DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss"));
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "summary.txt"), safeSummary);
            if (File.Exists(LatestLogPath))
                File.Copy(LatestLogPath, Path.Combine(dir, "DTMAPI-latest.log"), overwrite: true);
        }

        private sealed class OwnerBoundDiagnosticsHelper : IDiagnosticsHelper
        {
            private readonly DiagnosticsService inner;
            private readonly Action ensureRuntimeThread;
            private readonly Action ensureOwnerActive;

            public OwnerBoundDiagnosticsHelper(DiagnosticsService inner, Action ensureRuntimeThread, Action ensureOwnerActive)
            {
                this.inner = inner;
                this.ensureRuntimeThread = ensureRuntimeThread;
                this.ensureOwnerActive = ensureOwnerActive;
            }

            public IReadOnlyList<IDtmErrorInfo> GetErrors() => inner.GetErrors();
            public IReadOnlyList<IDtmWarningInfo> GetWarnings() => inner.GetWarnings();
            public IReadOnlyList<IHookStatusInfo> GetHookStatuses() => inner.GetHookStatuses();
            public string GetLatestLogPath() => inner.GetLatestLogPath();

            public string ExportLogs()
            {
                ensureRuntimeThread();
                ensureOwnerActive();
                return inner.ExportLogs();
            }

            public void RecordEvidence(string caseId, string summary)
            {
                ensureRuntimeThread();
                ensureOwnerActive();
                inner.RecordEvidence(caseId, summary);
            }
        }

        private string BuildSummary(string runtimeContextSummary)
        {
            IReadOnlyList<IDtmErrorInfo> errorSnapshot = GetErrors();
            IReadOnlyList<IDtmWarningInfo> warningSnapshot = GetWarnings();
            IReadOnlyList<IHookStatusInfo> hookSnapshot = GetHookStatuses();
            IReadOnlyList<IDtmFeatureStatusInfo> featureSnapshot = GetFeatureStatuses();
            long newlyTrimmedBytes = 0;
            string safeLatestLogPath = BoundedDiagnosticScalar.Sanitize(GetLatestLogPath(), BoundedDiagnosticScalar.DetailsChars, ref newlyTrimmedBytes);
            string safeLatestReportPath = BoundedDiagnosticScalar.Sanitize(GetLatestReportPath(), BoundedDiagnosticScalar.DetailsChars, ref newlyTrimmedBytes);
            string installReleaseSummary = BuildInstallReleaseSummary(ref newlyTrimmedBytes);
            int trimmedErrors;
            int trimmedWarnings;
            int aggregateKeyCount;
            long scalarTrimmedBytes;
            DiagnosticAggregateCounterSnapshot[] aggregateSnapshot;
            lock (gate)
            {
                BoundedDiagnosticScalar.AddTrimmedBytes(ref trimmedDiagnosticBytes, newlyTrimmedBytes);
                trimmedErrors = trimmedErrorCount;
                trimmedWarnings = trimmedWarningCount;
                aggregateKeyCount = diagnosticCounters.Count + (trimmedDiagnosticCounter == null ? 0 : 1);
                scalarTrimmedBytes = trimmedDiagnosticBytes;
                aggregateSnapshot = diagnosticCounters.Values
                    .Concat(trimmedDiagnosticCounter == null ? Enumerable.Empty<DiagnosticAggregateCounter>() : new[] { trimmedDiagnosticCounter! })
                    .OrderByDescending(c => c.Count)
                    .ThenByDescending(c => c.LastSeen)
                    .ThenBy(c => c.Kind, StringComparer.Ordinal)
                    .ThenBy(c => c.Owner, StringComparer.Ordinal)
                    .ThenBy(c => c.Message, StringComparer.Ordinal)
                    .Take(20)
                    .Select(c => c.ToSnapshot())
                    .ToArray();
            }

            string trimSummary = trimmedErrors > 0 || trimmedWarnings > 0
                ? "DiagnosticsTrimmed: errors=" + trimmedErrors + ", warnings=" + trimmedWarnings + ", maxPerKind=" + MaxDiagnosticEntriesPerKind + "." + Environment.NewLine
                : string.Empty;
            string aggregateSummary = aggregateSnapshot.Length > 0
                ? "DiagnosticsAggregates: totalKeys=" + aggregateKeyCount + ", top=" + aggregateSnapshot.Length + "." + Environment.NewLine +
                    string.Join(Environment.NewLine, aggregateSnapshot.Select(FormatDiagnosticAggregate)) +
                    Environment.NewLine
                : string.Empty;
            string prefix =
                "DTMAPI diagnostic report" + Environment.NewLine +
                "Generated: " + DateTimeOffset.Now + Environment.NewLine +
                "Errors: " + errorSnapshot.Count + Environment.NewLine +
                "Warnings: " + warningSnapshot.Count + Environment.NewLine +
                "Hooks: " + hookSnapshot.Count + Environment.NewLine +
                "Features: " + featureSnapshot.Count + Environment.NewLine +
                trimSummary;
            string suffix =
                aggregateSummary +
                (string.IsNullOrWhiteSpace(runtimeContextSummary)
                    ? string.Empty
                    : "RuntimeContext: present; entry=DTMAPI-runtime-context.txt; chars=" + runtimeContextSummary.Length + Environment.NewLine) +
                installReleaseSummary +
                "LatestLogPath: " + safeLatestLogPath + Environment.NewLine +
                "LatestReportPath: " + safeLatestReportPath + Environment.NewLine +
                Environment.NewLine +
                string.Join(Environment.NewLine, hookSnapshot.Select(h => $"HOOK {h.HookId}: {h.Status} - {h.Details}")) +
                Environment.NewLine +
                string.Join(Environment.NewLine, featureSnapshot.Select(f => $"FEATURE {f.FeatureId}: {f.Status} lastOperation={f.LastOperation} success={f.Success} failureCount={f.FailureCount} lastError={f.LastError} details={f.Details}")) +
                Environment.NewLine +
                string.Join(Environment.NewLine, warningSnapshot.Select(w => $"WARNING {w.Time:o} [{w.Owner}] {w.Message}: {w.Details}")) +
                Environment.NewLine +
                string.Join(Environment.NewLine, errorSnapshot.Select(e => $"ERROR {e.Time:o} [{e.Owner}] {e.Message}: {e.Details}"));

            string emptyScalarCounter = "DiagnosticsScalarTrimmedBytes: " + 0L.ToString("D19") + "." + Environment.NewLine;
            string emptySummaryCounter = "DiagnosticsSummaryTrimmedBytes: " + 0L.ToString("D19") + "." + Environment.NewLine;
            long projectedLength = (long)prefix.Length + emptyScalarCounter.Length + emptySummaryCounter.Length + suffix.Length;
            long summaryTrimmedBytes = projectedLength > BoundedDiagnosticScalar.ReportSummaryChars
                ? (projectedLength - BoundedDiagnosticScalar.ReportSummaryChars) * sizeof(char)
                : 0;
            long displayedTrimmedBytes = scalarTrimmedBytes;
            BoundedDiagnosticScalar.AddTrimmedBytes(ref displayedTrimmedBytes, summaryTrimmedBytes);
            string summary = prefix +
                "DiagnosticsScalarTrimmedBytes: " + displayedTrimmedBytes.ToString("D19") + "." + Environment.NewLine +
                "DiagnosticsSummaryTrimmedBytes: " + summaryTrimmedBytes.ToString("D19") + "." + Environment.NewLine +
                suffix;
            long measuredSummaryTrimmedBytes = 0;
            string boundedSummary = BoundedDiagnosticScalar.Sanitize(summary, BoundedDiagnosticScalar.ReportSummaryChars, ref measuredSummaryTrimmedBytes);
            lock (gate)
                BoundedDiagnosticScalar.AddTrimmedBytes(ref trimmedDiagnosticBytes, measuredSummaryTrimmedBytes);
            return boundedSummary;
        }

        private string BuildInstallReleaseSummary(ref long trimmedBytes)
        {
            string installPath = Path.Combine(paths.DtmApiPath, "install-state.json");
            string releasePath = Path.Combine(paths.DtmApiPath, "release-manifest.json");
            return BuildJsonStatus("InstallState", installPath, includeLegacyCounts: true, ref trimmedBytes) +
                BuildJsonStatus("ReleaseManifest", releasePath, includeLegacyCounts: false, ref trimmedBytes);
        }

        private static string BuildJsonStatus(string label, string path, bool includeLegacyCounts, ref long trimmedBytes)
        {
            if (!File.Exists(path))
                return label + ": missing" + Environment.NewLine;

            try
            {
                string text = ReadBoundedStatusText(path, out bool inputTruncated);
                string safePath = BoundedDiagnosticScalar.Sanitize(path, BoundedDiagnosticScalar.DetailsChars, ref trimmedBytes);
                string version = BoundedDiagnosticScalar.Sanitize(ExtractJsonString(text, "DTMAPIVersion"), BoundedDiagnosticScalar.IdentifierChars, ref trimmedBytes);
                string binaryVersion = BoundedDiagnosticScalar.Sanitize(ExtractJsonString(text, "BinaryVersion"), BoundedDiagnosticScalar.IdentifierChars, ref trimmedBytes);
                string result = label + ": present" + Environment.NewLine +
                    label + "Path: " + safePath + Environment.NewLine;
                if (inputTruncated)
                    result += label + "InputTruncated: true; maxChars=" + MaxJsonStatusInputChars + Environment.NewLine;
                if (!string.IsNullOrWhiteSpace(version))
                    result += label + "DTMAPIVersion: " + version + Environment.NewLine;
                if (!string.IsNullOrWhiteSpace(binaryVersion))
                    result += label + "BinaryVersion: " + binaryVersion + Environment.NewLine;
                if (includeLegacyCounts)
                {
                    result += label + "LegacyMovedCount: " + CountJsonArrayObjects(text, "LegacyModsMoved") + Environment.NewLine;
                    result += label + "LegacyDetectedCount: " + CountJsonArrayObjects(text, "LegacyDetections") + Environment.NewLine;
                }

                return result;
            }
            catch (Exception ex)
            {
                string safeError = BoundedDiagnosticScalar.Sanitize(ex.Message, BoundedDiagnosticScalar.MessageChars, ref trimmedBytes);
                return label + ": read-error " + ex.GetType().Name + ": " + safeError + Environment.NewLine;
            }
        }

        private static string ReadBoundedStatusText(string path, out bool truncated)
        {
            var buffer = new char[MaxJsonStatusInputChars];
            int total = 0;
            using (var reader = new StreamReader(path, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
            {
                while (total < buffer.Length)
                {
                    int read = reader.Read(buffer, total, buffer.Length - total);
                    if (read <= 0)
                        break;
                    total += read;
                }
                truncated = reader.Read() >= 0;
            }
            return new string(buffer, 0, total);
        }

        private static string ExtractJsonString(string json, string propertyName)
        {
            Match match = Regex.Match(json, "\"" + Regex.Escape(propertyName) + "\"\\s*:\\s*\"(?<value>[^\"]*)\"");
            return match.Success ? match.Groups["value"].Value : string.Empty;
        }

        private static int CountJsonArrayObjects(string json, string propertyName)
        {
            Match match = Regex.Match(json, "\"" + Regex.Escape(propertyName) + "\"\\s*:\\s*\\[(?<value>.*?)\\]", RegexOptions.Singleline);
            if (!match.Success)
                return 0;

            return Regex.Matches(match.Groups["value"].Value, "\\{").Count;
        }

        private static bool AddFileIfExists(ZipArchive archive, string path, string entryName, long maxBytes = long.MaxValue)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return false;
            try
            {
                var info = new FileInfo(path);
                if (info.Length > maxBytes)
                {
                    AddText(archive, entryName + ".skipped.txt", "File skipped because it exceeded diagnostic file budget." + Environment.NewLine +
                        "Path: " + path + Environment.NewLine +
                        "Bytes: " + info.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + Environment.NewLine +
                        "MaxBytes: " + maxBytes.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    return false;
                }

                ZipArchiveEntry entry = archive.CreateEntry(entryName);
                using (Stream target = entry.Open())
                using (FileStream source = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                    source.CopyTo(target);
                return true;
            }
            catch (Exception ex)
            {
                AddText(archive, entryName + ".error.txt", "Failed to include " + path + Environment.NewLine + ex);
                return false;
            }
        }

        private static void AddRecentFiles(ZipArchive archive, string directory, string filter, string entryRoot, int count)
        {
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                return;

            foreach (FileInfo file in new DirectoryInfo(directory)
                .GetFiles(filter, SearchOption.TopDirectoryOnly)
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .Take(count))
            {
                AddFileIfExists(archive, file.FullName, entryRoot + "/" + MakeSafeFileName(file.Name));
            }
        }

        private static void AddRecentUnityCrashReports(ZipArchive archive)
        {
            var summary = new List<string>
            {
                "Generated: " + DateTimeOffset.Now,
                "MaxDirectories: " + MaxUnityCrashReportDirectories,
                "MaxFilesPerDirectory: " + MaxUnityCrashReportFilesPerDirectory,
                "MaxFilesTotal: " + MaxUnityCrashReportFilesTotal,
                "MaxFileBytes: " + MaxUnityCrashReportFileBytes,
                "MaxDumpFileBytes: " + MaxUnityCrashReportDumpFileBytes,
                "MaxTotalBytes: " + MaxUnityCrashReportTotalBytes,
                "MaxTotalBytesWithDump: " + MaxUnityCrashReportTotalBytesWithDump,
                string.Empty
            };
            int copiedDirectories = 0;
            int copiedFiles = 0;
            int consideredFiles = 0;
            long copiedBytes = 0;
            bool copiedPrimaryCrashDump = false;
            var missingCrashDumpInstructions = new List<string>();
            List<DirectoryInfo> crashDirectories = GetRecentUnityCrashDirectoryCandidates(summary, missingCrashDumpInstructions, MaxUnityCrashReportDirectories);

            foreach (string crashRoot in GetUnityCrashRootCandidates())
            {
                summary.Add("CrashRoot: " + crashRoot);
                if (!Directory.Exists(crashRoot))
                {
                    summary.Add("Exists: false");
                    summary.Add(string.Empty);
                    continue;
                }

                summary.Add("Exists: true");
                summary.Add("RecentDirectoryCountFromRoot: " + crashDirectories.Count(d => IsSameOrChildPath(d.FullName, crashRoot)));
                summary.Add(string.Empty);
            }

            summary.Add("GlobalRecentDirectoryCount: " + crashDirectories.Count);
            foreach (DirectoryInfo directory in crashDirectories)
            {
                copiedDirectories++;
                string directoryEntryRoot = "Unity-Crashes/" + MakeCrashDirectoryEntryName(directory);
                summary.Add("Directory: " + directory.FullName);
                summary.Add("DirectoryLastWrite: " + directory.LastWriteTimeUtc.ToString("o"));

                int directoryFilesConsidered = 0;
                bool directorySawCrashDump = false;
                int directoryBudget = Math.Min(MaxUnityCrashReportFilesPerDirectory, MaxUnityCrashReportFilesTotal - consideredFiles);
                if (directoryBudget <= 0)
                {
                    summary.Add("SkippedTotalFileLimitBeforeDirectory: " + directory.FullName);
                    summary.Add("DirectoryFilesConsidered: 0");
                    continue;
                }

                foreach (FileInfo file in EnumerateUnityCrashReportFiles(directory, summary, directoryBudget))
                {
                    directoryFilesConsidered++;
                    consideredFiles++;
                    if (IsUnityCrashDump(file))
                        directorySawCrashDump = true;
                    TryAddUnityCrashReportFile(archive, directory.FullName, directoryEntryRoot, file, summary, missingCrashDumpInstructions, ref copiedFiles, ref copiedBytes, ref copiedPrimaryCrashDump);
                }

                if (!directorySawCrashDump)
                {
                    string expectedCrashDumpPath = Path.Combine(directory.FullName, "crash.dmp");
                    summary.Add("MissingCrashDumpFile: " + expectedCrashDumpPath);
                    AddMissingCrashDumpInstruction(missingCrashDumpInstructions, "missing-file", expectedCrashDumpPath, "Unity crash directory existed but no crash.dmp file was found or enumerable.");
                }
                if (directoryFilesConsidered >= MaxUnityCrashReportFilesPerDirectory)
                    summary.Add("SkippedDirectoryFileLimit: " + directory.FullName);
                if (consideredFiles >= MaxUnityCrashReportFilesTotal)
                    summary.Add("SkippedTotalFileLimitAfterDirectory: " + directory.FullName);

                summary.Add("DirectoryFilesConsidered: " + directoryFilesConsidered);
            }

            if (copiedDirectories == 0)
                summary.Add("No Unity crash report directories were found for the current Windows user.");
            if (missingCrashDumpInstructions.Count > 0)
                AddText(archive, "Unity-Crashes/MISSING-CRASH-DUMP-README.txt", string.Join(Environment.NewLine, missingCrashDumpInstructions));

            summary.Add("FilesConsideredTotal: " + consideredFiles);
            summary.Add("FilesCopiedTotal: " + copiedFiles);
            summary.Add("BytesCopiedTotal: " + copiedBytes);
            AddText(archive, "Unity-Crashes/summary.txt", string.Join(Environment.NewLine, summary));
        }

        private static List<DirectoryInfo> GetRecentUnityCrashDirectoryCandidates(List<string> summary, List<string> missingCrashDumpInstructions, int maxDirectories)
        {
            var allCandidates = new List<DirectoryInfo>();
            var candidatesByRoot = new List<KeyValuePair<string, List<DirectoryInfo>>>();
            foreach (string crashRoot in GetUnityCrashRootCandidates())
            {
                if (!Directory.Exists(crashRoot))
                    continue;

                var rootCandidates = new List<DirectoryInfo>();
                try
                {
                    int rootDirectoryCount = 0;
                    foreach (DirectoryInfo directory in new DirectoryInfo(crashRoot).EnumerateDirectories())
                    {
                        if (IsReparsePoint(directory))
                        {
                            summary.Add("SkippedReparseDirectory: " + directory.FullName);
                            continue;
                        }

                        rootDirectoryCount++;
                        rootCandidates.Add(directory);
                        allCandidates.Add(directory);
                    }

                    summary.Add("CrashRootDirectoryCount: " + crashRoot + " count=" + rootDirectoryCount);
                    candidatesByRoot.Add(new KeyValuePair<string, List<DirectoryInfo>>(crashRoot, rootCandidates));
                }
                catch (Exception ex)
                {
                    summary.Add("ReadError: " + crashRoot + " " + ex.GetType().Name + ": " + ex.Message);
                    AddMissingCrashDumpInstruction(missingCrashDumpInstructions, "directory-read-error", Path.Combine(crashRoot, "crash.dmp"), ex.GetType().Name + ": " + ex.Message);
                }
            }

            var selected = new List<DirectoryInfo>();
            if (maxDirectories <= 0 || allCandidates.Count == 0)
                return selected;

            int latestBudget = Math.Min(maxDirectories, Math.Max(candidatesByRoot.Count, Math.Min(3, allCandidates.Count)));
            foreach (KeyValuePair<string, List<DirectoryInfo>> root in candidatesByRoot)
            {
                DirectoryInfo? newestForRoot = root.Value
                    .OrderByDescending(directory => GetUnityCrashDirectorySortTime(directory, HasUnityCrashDump(directory)))
                    .FirstOrDefault();
                if (newestForRoot != null)
                    AddUniqueDirectoryCandidate(selected, newestForRoot, maxDirectories);
            }

            foreach (DirectoryInfo directory in allCandidates.OrderByDescending(directory => GetUnityCrashDirectorySortTime(directory, HasUnityCrashDump(directory))))
            {
                if (selected.Count >= latestBudget)
                    break;
                AddUniqueDirectoryCandidate(selected, directory, maxDirectories);
            }

            foreach (DirectoryInfo directory in allCandidates.OrderBy(directory => directory, Comparer<DirectoryInfo>.Create(CompareUnityCrashDirectoryPriority)))
            {
                if (selected.Count >= maxDirectories)
                    break;
                AddUniqueDirectoryCandidate(selected, directory, maxDirectories);
            }

            foreach (DirectoryInfo directory in allCandidates.OrderByDescending(directory => GetUnityCrashDirectorySortTime(directory, HasUnityCrashDump(directory))))
            {
                if (selected.Count >= maxDirectories)
                    break;
                AddUniqueDirectoryCandidate(selected, directory, maxDirectories);
            }

            summary.Add("CrashDirectorySelectionMode: latest-per-root-plus-latest-then-dump-priority");
            summary.Add("CrashDirectoryLatestBudget: " + latestBudget);
            foreach (KeyValuePair<string, List<DirectoryInfo>> root in candidatesByRoot)
            {
                int kept = selected.Count(directory => IsSameOrChildPath(directory.FullName, root.Key));
                int skipped = Math.Max(0, root.Value.Count - kept);
                if (skipped > 0)
                    summary.Add("CrashRootDirectorySkippedByBudget: " + root.Key + " count=" + skipped);
            }

            return selected;
        }

        private static bool AddUniqueDirectoryCandidate(List<DirectoryInfo> selected, DirectoryInfo directory, int maxDirectories)
        {
            if (selected.Any(existing => IsSamePath(existing.FullName, directory.FullName)))
                return true;
            if (selected.Count >= maxDirectories)
                return false;
            selected.Add(directory);
            return true;
        }

        private static bool AddRecentDirectoryCandidate(List<DirectoryInfo> recent, DirectoryInfo directory, int maxDirectories)
        {
            int insertAt = recent.FindIndex(existing => CompareUnityCrashDirectoryPriority(directory, existing) < 0);
            if (insertAt < 0)
                recent.Add(directory);
            else
                recent.Insert(insertAt, directory);
            if (recent.Count > maxDirectories)
                recent.RemoveAt(recent.Count - 1);
            return recent.Any(existing => IsSamePath(existing.FullName, directory.FullName));
        }

        private static int CompareUnityCrashDirectoryPriority(DirectoryInfo left, DirectoryInfo right)
        {
            bool leftHasDump = HasUnityCrashDump(left);
            bool rightHasDump = HasUnityCrashDump(right);
            if (leftHasDump != rightHasDump)
                return leftHasDump ? -1 : 1;

            DateTime leftTime = GetUnityCrashDirectorySortTime(left, leftHasDump);
            DateTime rightTime = GetUnityCrashDirectorySortTime(right, rightHasDump);
            return rightTime.CompareTo(leftTime);
        }

        private static bool HasUnityCrashDump(DirectoryInfo directory)
        {
            return File.Exists(Path.Combine(directory.FullName, "crash.dmp"));
        }

        private static DateTime GetUnityCrashDirectorySortTime(DirectoryInfo directory, bool hasDump)
        {
            DateTime latest = directory.LastWriteTimeUtc;
            if (!hasDump)
                return latest;

            try
            {
                DateTime dumpTime = File.GetLastWriteTimeUtc(Path.Combine(directory.FullName, "crash.dmp"));
                if (dumpTime > latest)
                    latest = dumpTime;
            }
            catch
            {
            }

            return latest;
        }

        private static bool IsSamePath(string left, string right)
        {
            try
            {
                return Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    .Equals(Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
            }
        }

        private static bool IsSameOrChildPath(string path, string root)
        {
            try
            {
                string fullPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                return fullPath.Equals(fullRoot, StringComparison.OrdinalIgnoreCase) ||
                    fullPath.StartsWith(fullRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                    fullPath.StartsWith(fullRoot + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static DirectoryInfo[] GetRecentUnityCrashDirectories(DirectoryInfo crashRoot, List<string> summary, int maxDirectories)
        {
            var recent = new List<DirectoryInfo>();
            try
            {
                foreach (DirectoryInfo directory in crashRoot.EnumerateDirectories())
                {
                    if (IsReparsePoint(directory))
                    {
                        summary.Add("SkippedReparseDirectory: " + directory.FullName);
                        continue;
                    }

                    AddRecentDirectoryCandidate(recent, directory, maxDirectories);
                }
            }
            catch (Exception ex)
            {
                summary.Add("ReadError: " + crashRoot.FullName + " " + ex.GetType().Name + ": " + ex.Message);
            }

            return recent.ToArray();
        }

        private static IEnumerable<FileInfo> EnumerateUnityCrashReportFiles(DirectoryInfo directory, List<string> summary, int maxFiles)
        {
            if (maxFiles <= 0)
                yield break;

            var yielded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int count = 0;
            foreach (string preferredName in PreferredUnityCrashReportFiles)
            {
                foreach (FileInfo file in EnumerateFilesSafe(directory, preferredName, summary, maxFiles - count))
                {
                    if (yielded.Add(file.FullName))
                    {
                        count++;
                        yield return file;
                        if (count >= maxFiles)
                            yield break;
                    }
                }
            }

            foreach (FileInfo file in EnumerateFilesSafe(directory, "*", summary, maxFiles - count))
            {
                if (yielded.Add(file.FullName))
                {
                    count++;
                    yield return file;
                    if (count >= maxFiles)
                        yield break;
                }
            }
        }

        private static IEnumerable<FileInfo> EnumerateFilesSafe(DirectoryInfo root, string pattern, List<string> summary, int maxFiles)
        {
            if (maxFiles <= 0)
                yield break;

            var directories = new Stack<DirectoryInfo>();
            directories.Push(root);
            int yieldedCount = 0;
            while (directories.Count > 0)
            {
                DirectoryInfo directory = directories.Pop();
                if (IsReparsePoint(directory))
                {
                    summary.Add("SkippedReparseDirectory: " + directory.FullName);
                    continue;
                }

                IEnumerator<FileInfo>? files = null;
                try
                {
                    files = directory.EnumerateFiles(pattern, SearchOption.TopDirectoryOnly).GetEnumerator();
                }
                catch (Exception ex)
                {
                    summary.Add("FileListError: " + directory.FullName + " pattern=" + pattern + " " + ex.GetType().Name + ": " + ex.Message);
                }

                while (files != null)
                {
                    FileInfo? file = null;
                    bool moved;
                    try
                    {
                        moved = files.MoveNext();
                        if (moved)
                            file = files.Current;
                    }
                    catch (Exception ex)
                    {
                        summary.Add("FileListError: " + directory.FullName + " pattern=" + pattern + " " + ex.GetType().Name + ": " + ex.Message);
                        break;
                    }

                    if (!moved)
                        break;

                    if (file != null)
                    {
                        if (IsReparsePoint(file))
                        {
                            summary.Add("SkippedReparseFile: " + file.FullName + " pattern=" + pattern);
                            continue;
                        }

                        yield return file;
                        yieldedCount++;
                        if (yieldedCount >= maxFiles)
                        {
                            files.Dispose();
                            yield break;
                        }
                    }
                }
                files?.Dispose();

                try
                {
                    foreach (DirectoryInfo child in directory.EnumerateDirectories())
                        directories.Push(child);
                }
                catch (Exception ex)
                {
                    summary.Add("DirectoryListError: " + directory.FullName + " " + ex.GetType().Name + ": " + ex.Message);
                }
            }
        }

        private static bool IsReparsePoint(FileSystemInfo item)
        {
            return item != null && (item.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;
        }

        private static bool TryAddUnityCrashReportFile(ZipArchive archive, string directoryPath, string directoryEntryRoot, FileInfo file, List<string> summary, List<string> missingCrashDumpInstructions, ref int copiedFiles, ref long copiedBytes, ref bool copiedPrimaryCrashDump)
        {
            try
            {
                file.Refresh();
                if (!file.Exists)
                {
                    summary.Add("SkippedMissingFile: " + file.FullName);
                    return false;
                }

                bool isCrashDump = IsUnityCrashDump(file);
                bool isPrimaryCrashDump = isCrashDump && !copiedPrimaryCrashDump;
                long maxFileBytes = isPrimaryCrashDump ? MaxUnityCrashReportDumpFileBytes : MaxUnityCrashReportFileBytes;
                long maxTotalBytes = (isPrimaryCrashDump || copiedPrimaryCrashDump) ? MaxUnityCrashReportTotalBytesWithDump : MaxUnityCrashReportTotalBytes;

                if (file.Length > maxFileBytes)
                {
                    summary.Add("SkippedLargeFile: " + file.FullName + " bytes=" + file.Length + " maxBytes=" + maxFileBytes);
                    if (isCrashDump)
                        AddMissingCrashDumpInstruction(missingCrashDumpInstructions, "large-file", file, "size=" + file.Length + ", maxBytes=" + maxFileBytes);
                    return false;
                }

                if (copiedBytes + file.Length > maxTotalBytes)
                {
                    summary.Add("SkippedTotalByteLimit: " + file.FullName + " bytes=" + file.Length + " currentBytes=" + copiedBytes + " maxTotalBytes=" + maxTotalBytes);
                    if (isCrashDump)
                        AddMissingCrashDumpInstruction(missingCrashDumpInstructions, "total-byte-budget", file, "size=" + file.Length + ", currentBytes=" + copiedBytes + ", maxTotalBytes=" + maxTotalBytes);
                    return false;
                }

                string relativePath = GetRelativePath(directoryPath, file.FullName);
                string entryName = directoryEntryRoot + "/" + MakeSafeEntryPath(relativePath);
                if (!AddFileIfExists(archive, file.FullName, entryName))
                {
                    summary.Add("CopyFailed: " + file.FullName);
                    if (isCrashDump)
                        AddMissingCrashDumpInstruction(missingCrashDumpInstructions, "copy-failed", file, "entry=" + entryName);
                    return false;
                }

                copiedFiles++;
                copiedBytes += file.Length;
                if (isPrimaryCrashDump)
                    copiedPrimaryCrashDump = true;
                summary.Add("CopiedFile: " + file.FullName + " bytes=" + file.Length);
                return true;
            }
            catch (Exception ex)
            {
                summary.Add("FileReadError: " + file.FullName + " " + ex.GetType().Name + ": " + ex.Message);
                if (IsUnityCrashDump(file))
                    AddMissingCrashDumpInstruction(missingCrashDumpInstructions, "read-error", file, ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }

        private static bool IsUnityCrashDump(FileInfo file)
        {
            return file != null && string.Equals(file.Name, "crash.dmp", StringComparison.OrdinalIgnoreCase);
        }

        private static void AddMissingCrashDumpInstruction(List<string> instructions, string reason, FileInfo file, string details)
        {
            AddMissingCrashDumpInstruction(instructions, reason, file.FullName, details);
        }

        private static void AddMissingCrashDumpInstruction(List<string> instructions, string reason, string path, string details)
        {
            instructions.Add("DTMAPI could not include a Unity crash dump in the report.");
            instructions.Add("Reason: " + reason);
            instructions.Add("Path: " + path);
            instructions.Add("Details: " + details);
            instructions.Add("Please send this crash.dmp separately together with the DTMAPI report if possible.");
            instructions.Add(string.Empty);
        }

        private static void AddText(ZipArchive archive, string entryName, string text)
        {
            ZipArchiveEntry entry = archive.CreateEntry(entryName);
            using (StreamWriter writer = new StreamWriter(entry.Open()))
                writer.Write(text);
        }

        private static IEnumerable<string> GetUnityCrashRootCandidates()
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string tempRoot in new[]
            {
                Path.GetTempPath(),
                Environment.GetEnvironmentVariable("TEMP") ?? string.Empty,
                Environment.GetEnvironmentVariable("TMP") ?? string.Empty
            })
            {
                if (string.IsNullOrWhiteSpace(tempRoot))
                    continue;

                string root;
                try
                {
                    root = Path.GetFullPath(Path.Combine(tempRoot, "RedSawGames", "DolocTown", "Crashes")).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                }
                catch
                {
                    continue;
                }

                if (seen.Add(root))
                    yield return root;

                try
                {
                    root = Path.GetFullPath(Path.Combine(tempRoot, "RedSawGames", "Doloc Town", "Crashes")).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                }
                catch
                {
                    continue;
                }

                if (seen.Add(root))
                    yield return root;
            }
        }

        private static string MakeSafeEntryPath(string relativePath)
        {
            return string.Join("/", (relativePath ?? string.Empty)
                .Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries)
                .Select(MakeSafeFileName));
        }

        private static string MakeCrashDirectoryEntryName(DirectoryInfo directory)
        {
            string safeName = MakeSafeFileName(directory?.Name ?? "crash");
            string hash = ShortHash(directory?.FullName ?? safeName);
            return safeName + "-" + hash;
        }

        private static string ShortHash(string value)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty));
                return BitConverter.ToString(bytes, 0, 6).Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        private static string GetRelativePath(string basePath, string path)
        {
            string baseFull = Path.GetFullPath(basePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string pathFull = Path.GetFullPath(path);
            string prefix = baseFull + Path.DirectorySeparatorChar;
            if (pathFull.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return pathFull.Substring(prefix.Length);

            return Path.GetFileName(pathFull);
        }

        private static string FindPlayerLogPath()
        {
            string localLow = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "LocalLow", "RedSawGames");
            return FindNewestExistingFile(
                Path.Combine(localLow, "DolocTown", "Player.log"),
                Path.Combine(localLow, "Doloc Town", "Player.log"));
        }

        private static string FindPlayerPreviousLogPath()
        {
            string localLow = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "LocalLow", "RedSawGames");
            return FindNewestExistingFile(
                Path.Combine(localLow, "DolocTown", "Player-prev.log"),
                Path.Combine(localLow, "Doloc Town", "Player-prev.log"));
        }

        private static string FindNewestExistingFile(params string[] paths)
        {
            FileInfo? newest = null;
            foreach (string path in paths)
            {
                if (!File.Exists(path))
                    continue;

                var file = new FileInfo(path);
                if (newest == null || file.LastWriteTimeUtc > newest.LastWriteTimeUtc)
                    newest = file;
            }

            return newest?.FullName ?? paths.FirstOrDefault() ?? string.Empty;
        }

        private static string MakeSafeFileName(string value)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                value = value.Replace(c, '_');
            return value;
        }

        private void RecordDiagnosticCounter(string kind, string owner, string message, string details)
        {
            DateTimeOffset now = DateTimeOffset.Now;
            string key = kind + "\u001F" + owner + "\u001F" + message;
            if (!diagnosticCounters.TryGetValue(key, out DiagnosticAggregateCounter? counter))
            {
                if (diagnosticCounters.Count >= MaxDiagnosticAggregates - 1)
                {
                    trimmedDiagnosticAggregateCount++;
                    if (trimmedDiagnosticCounter != null)
                    {
                        trimmedDiagnosticCounter.Record(now, "Additional unique diagnostic aggregate keys=" + trimmedDiagnosticAggregateCount + ".");
                        return;
                    }
                    trimmedDiagnosticCounter = new DiagnosticAggregateCounter("Other", "other", "trimmed", now, "Additional unique diagnostic aggregate keys=" + trimmedDiagnosticAggregateCount + ".");
                    return;
                }
                diagnosticCounters[key] = new DiagnosticAggregateCounter(kind, owner, message, now, details);
                return;
            }

            counter.Record(now, details);
        }

        internal int RetainedHookStatusCount { get { lock (gate) return hooks.Count + (trimmedHookStatus == null ? 0 : 1); } }
        internal int RetainedFeatureStatusCount { get { lock (gate) return features.Count + (trimmedFeatureStatus == null ? 0 : 1); } }
        internal int RetainedDiagnosticAggregateCount { get { lock (gate) return diagnosticCounters.Count + (trimmedDiagnosticCounter == null ? 0 : 1); } }
        internal int TrimmedHookStatusCount { get { lock (gate) return trimmedHookStatusCount; } }
        internal int TrimmedFeatureStatusCount { get { lock (gate) return trimmedFeatureStatusCount; } }
        internal int TrimmedDiagnosticAggregateCount { get { lock (gate) return trimmedDiagnosticAggregateCount; } }
        internal long TrimmedDiagnosticBytes { get { lock (gate) return trimmedDiagnosticBytes; } }

        private static string FormatDiagnosticAggregate(DiagnosticAggregateCounterSnapshot counter)
        {
            return "DIAGNOSTIC-AGGREGATE " + counter.Kind +
                " owner=" + SingleLine(counter.Owner) +
                " message=" + SingleLine(counter.Message) +
                " count=" + counter.Count +
                " first=" + counter.FirstSeen.ToString("o") +
                " last=" + counter.LastSeen.ToString("o") +
                " lastDetails=" + SingleLine(counter.LastDetails);
        }

        private static string SingleLine(string value)
        {
            return (value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ');
        }

        private static void AddBounded<T>(List<T> list, T item, ref int trimmedCount)
        {
            list.Add(item);
            if (list.Count <= MaxDiagnosticEntriesPerKind)
                return;

            int removeCount = list.Count - MaxDiagnosticEntriesPerKind;
            list.RemoveRange(0, removeCount);
            trimmedCount += removeCount;
        }

        private sealed class DiagnosticAggregateCounter
        {
            public DiagnosticAggregateCounter(string kind, string owner, string message, DateTimeOffset time, string details)
            {
                Kind = kind;
                Owner = owner;
                Message = message;
                Count = 1;
                FirstSeen = time;
                LastSeen = time;
                LastDetails = details;
            }

            public string Kind { get; }

            public string Owner { get; }

            public string Message { get; }

            public int Count { get; private set; }

            public DateTimeOffset FirstSeen { get; private set; }

            public DateTimeOffset LastSeen { get; private set; }

            public string LastDetails { get; private set; }

            public void Record(DateTimeOffset time, string details)
            {
                Count++;
                LastSeen = time;
                LastDetails = details;
            }

            public DiagnosticAggregateCounterSnapshot ToSnapshot()
            {
                return new DiagnosticAggregateCounterSnapshot(Kind, Owner, Message, Count, FirstSeen, LastSeen, LastDetails);
            }
        }

        private sealed class DiagnosticAggregateCounterSnapshot
        {
            public DiagnosticAggregateCounterSnapshot(string kind, string owner, string message, int count, DateTimeOffset firstSeen, DateTimeOffset lastSeen, string lastDetails)
            {
                Kind = kind;
                Owner = owner;
                Message = message;
                Count = count;
                FirstSeen = firstSeen;
                LastSeen = lastSeen;
                LastDetails = lastDetails;
            }

            public string Kind { get; }

            public string Owner { get; }

            public string Message { get; }

            public int Count { get; }

            public DateTimeOffset FirstSeen { get; }

            public DateTimeOffset LastSeen { get; }

            public string LastDetails { get; }
        }
    }
}
