using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.Core.Diagnostics
{
    public sealed class DiagnosticsService : IDiagnosticsHelper
    {
        private const int MaxDiagnosticEntriesPerKind = 1000;
        private readonly RuntimePaths paths;
        private readonly List<DtmErrorInfo> errors = new List<DtmErrorInfo>();
        private readonly List<DtmWarningInfo> warnings = new List<DtmWarningInfo>();
        private readonly Dictionary<string, HookStatusInfo> hooks = new Dictionary<string, HookStatusInfo>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, DtmFeatureStatusInfo> features = new Dictionary<string, DtmFeatureStatusInfo>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, DiagnosticAggregateCounter> diagnosticCounters = new Dictionary<string, DiagnosticAggregateCounter>(StringComparer.Ordinal);
        private readonly object gate = new object();
        private int trimmedErrorCount;
        private int trimmedWarningCount;

        public DiagnosticsService(RuntimePaths paths)
        {
            this.paths = paths;
        }

        public string LatestLogPath { get; set; } = string.Empty;

        public string LatestReportPath { get; private set; } = string.Empty;

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
                return hooks.Values.OrderBy(h => h.HookId, StringComparer.OrdinalIgnoreCase).Cast<IHookStatusInfo>().ToArray();
        }

        public IReadOnlyList<IDtmFeatureStatusInfo> GetFeatureStatuses()
        {
            lock (gate)
                return features.Values.OrderBy(f => f.FeatureId, StringComparer.OrdinalIgnoreCase).Cast<IDtmFeatureStatusInfo>().ToArray();
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
                return File.ReadAllText(pointer).Trim();
            }
            catch
            {
                return string.Empty;
            }
        }

        public void RecordError(string owner, string message, string details)
        {
            lock (gate)
            {
                RecordDiagnosticCounter("Error", owner, message, details);
                AddBounded(errors, new DtmErrorInfo(owner, message, details), ref trimmedErrorCount);
            }
        }

        internal void RecordWarning(string owner, string message, string details)
        {
            lock (gate)
            {
                RecordDiagnosticCounter("Warning", owner, message, details);
                AddBounded(warnings, new DtmWarningInfo(owner, message, details), ref trimmedWarningCount);
            }
        }

        public bool SetHookStatus(string hookId, string status, string source, string details)
        {
            lock (gate)
            {
                if (hooks.TryGetValue(hookId, out HookStatusInfo existing) &&
                    existing.Status == status &&
                    existing.Source == source &&
                    existing.Details == details)
                    return false;
                hooks[hookId] = new HookStatusInfo(hookId, status, source, details);
                return true;
            }
        }

        public void SetFeatureStatus(string featureId, string status, string lastOperation, bool success, int failureCount, string lastError, string details)
        {
            if (string.IsNullOrWhiteSpace(featureId))
                return;

            lock (gate)
                features[featureId] = new DtmFeatureStatusInfo(featureId, status, lastOperation, success, failureCount, lastError, details);
        }

        public string ExportLogs()
        {
            Directory.CreateDirectory(paths.ReportsPath);
            string stamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss");
            string zipPath = Path.Combine(paths.ReportsPath, $"dtmapi-report-{stamp}.zip");
            LatestReportPath = zipPath;
            using (FileStream file = File.Create(zipPath))
            using (var archive = new ZipArchive(file, ZipArchiveMode.Create))
            {
                AddFileIfExists(archive, LatestLogPath, "DTMAPI-latest.log");
                AddFileIfExists(archive, Path.Combine(paths.GamePath, "BepInEx", "LogOutput.log"), "BepInEx-LogOutput.log");
                AddFileIfExists(archive, FindPlayerLogPath(), "Unity-Player.log");
                AddText(archive, "dtmapi-summary.txt", BuildSummary());
            }

            File.WriteAllText(Path.Combine(paths.ReportsPath, "latest-report.txt"), zipPath);
            return zipPath;
        }

        public void RecordEvidence(string caseId, string summary)
        {
            string safeCase = MakeSafeFileName(string.IsNullOrWhiteSpace(caseId) ? "CASE" : caseId);
            string dir = Path.Combine(paths.EvidencePath, safeCase, DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss"));
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "summary.txt"), summary);
            if (File.Exists(LatestLogPath))
                File.Copy(LatestLogPath, Path.Combine(dir, "DTMAPI-latest.log"), overwrite: true);
        }

        private string BuildSummary()
        {
            IReadOnlyList<IDtmErrorInfo> errorSnapshot = GetErrors();
            IReadOnlyList<IDtmWarningInfo> warningSnapshot = GetWarnings();
            IReadOnlyList<IHookStatusInfo> hookSnapshot = GetHookStatuses();
            IReadOnlyList<IDtmFeatureStatusInfo> featureSnapshot = GetFeatureStatuses();
            int trimmedErrors;
            int trimmedWarnings;
            int aggregateKeyCount;
            DiagnosticAggregateCounterSnapshot[] aggregateSnapshot;
            lock (gate)
            {
                trimmedErrors = trimmedErrorCount;
                trimmedWarnings = trimmedWarningCount;
                aggregateKeyCount = diagnosticCounters.Count;
                aggregateSnapshot = diagnosticCounters.Values
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

            return
                "DTMAPI diagnostic report" + Environment.NewLine +
                "Generated: " + DateTimeOffset.Now + Environment.NewLine +
                "Errors: " + errorSnapshot.Count + Environment.NewLine +
                "Warnings: " + warningSnapshot.Count + Environment.NewLine +
                "Hooks: " + hookSnapshot.Count + Environment.NewLine +
                "Features: " + featureSnapshot.Count + Environment.NewLine +
                trimSummary +
                aggregateSummary +
                "LatestLogPath: " + GetLatestLogPath() + Environment.NewLine +
                "LatestReportPath: " + GetLatestReportPath() + Environment.NewLine +
                Environment.NewLine +
                string.Join(Environment.NewLine, hookSnapshot.Select(h => $"HOOK {h.HookId}: {h.Status} - {h.Details}")) +
                Environment.NewLine +
                string.Join(Environment.NewLine, featureSnapshot.Select(f => $"FEATURE {f.FeatureId}: {f.Status} lastOperation={f.LastOperation} success={f.Success} failureCount={f.FailureCount} lastError={f.LastError} details={f.Details}")) +
                Environment.NewLine +
                string.Join(Environment.NewLine, warningSnapshot.Select(w => $"WARNING {w.Time:o} [{w.Owner}] {w.Message}: {w.Details}")) +
                Environment.NewLine +
                string.Join(Environment.NewLine, errorSnapshot.Select(e => $"ERROR {e.Time:o} [{e.Owner}] {e.Message}: {e.Details}"));
        }

        private static void AddFileIfExists(ZipArchive archive, string path, string entryName)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return;
            try
            {
                ZipArchiveEntry entry = archive.CreateEntry(entryName);
                using (Stream target = entry.Open())
                using (FileStream source = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                    source.CopyTo(target);
            }
            catch (Exception ex)
            {
                AddText(archive, entryName + ".error.txt", "Failed to include " + path + Environment.NewLine + ex);
            }
        }

        private static void AddText(ZipArchive archive, string entryName, string text)
        {
            ZipArchiveEntry entry = archive.CreateEntry(entryName);
            using (StreamWriter writer = new StreamWriter(entry.Open()))
                writer.Write(text);
        }

        private static string FindPlayerLogPath()
        {
            string localLow = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "LocalLow", "RedSawGames");
            string noSpace = Path.Combine(localLow, "DolocTown", "Player.log");
            if (File.Exists(noSpace))
                return noSpace;
            return Path.Combine(localLow, "Doloc Town", "Player.log");
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
            string normalizedOwner = owner ?? string.Empty;
            string normalizedMessage = message ?? string.Empty;
            string key = kind + "\u001F" + normalizedOwner + "\u001F" + normalizedMessage;
            if (!diagnosticCounters.TryGetValue(key, out DiagnosticAggregateCounter? counter))
            {
                diagnosticCounters[key] = new DiagnosticAggregateCounter(kind, normalizedOwner, normalizedMessage, now, details ?? string.Empty);
                return;
            }

            counter.Record(now, details ?? string.Empty);
        }

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
