using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using DTMAPI.Core.Runtime;

namespace DTMAPI.Core.Diagnostics
{
    internal sealed class PlayerDoctorRuntimeService
    {
        private bool attempted;
        private PlayerDoctorRuntimeResult latest = PlayerDoctorRuntimeResult.NotRun();

        public PlayerDoctorRuntimeResult PrepareManualDiagnostics(RuntimePaths paths)
        {
            if (attempted) return latest;
            attempted = true;
            DeleteStalePendingOutputsBestEffort(paths);
            DeleteOwnedOutputsBestEffort(paths);
            latest = PlayerDoctorRuntimeResult.ManualOnly();
            return latest;
        }

        public PlayerDoctorRuntimeResult GetLatest() => latest;

        private static void DeleteOwnedOutput(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }

        private static void DeleteOwnedOutputsBestEffort(RuntimePaths paths)
        {
            foreach (string path in new[]
            {
                paths.PlayerDoctorJsonReportPath,
                paths.PlayerDoctorTextReportPath,
                paths.PlayerDoctorSummaryPath
            })
            {
                try
                {
                    DeleteOwnedOutput(path);
                }
                catch
                {
                    // Failure reports must not hide the original startup diagnostic.
                }
            }
        }

        private static void DeleteStalePendingOutputsBestEffort(RuntimePaths paths)
        {
            foreach (string finalPath in new[]
            {
                paths.PlayerDoctorJsonReportPath,
                paths.PlayerDoctorTextReportPath,
                paths.PlayerDoctorSummaryPath
            })
            {
                try
                {
                    string? directory = Path.GetDirectoryName(finalPath);
                    if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                        continue;
                    string pattern = Path.GetFileName(finalPath) + ".pending-*";
                    foreach (string pendingPath in Directory.GetFiles(directory, pattern, SearchOption.TopDirectoryOnly))
                        DeleteOwnedOutput(pendingPath);
                }
                catch
                {
                    // A stale pending file is never exported; cleanup must not block Runtime startup.
                }
            }
        }

    }

    internal sealed class PlayerDoctorRuntimeResult
    {
        private PlayerDoctorRuntimeResult(string status, int exitCode, string summary, bool completed)
        {
            Status = status;
            ExitCode = exitCode;
            Summary = summary ?? string.Empty;
            Completed = completed;
            Fields = ParseFields(Summary);
        }

        public string Status { get; }
        public int ExitCode { get; }
        public string Summary { get; }
        public bool Completed { get; }
        public IReadOnlyDictionary<string, string> Fields { get; }
        public int ErrorCount => ReadCount("errors");
        public int WarningCount => ReadCount("warnings");

        public static PlayerDoctorRuntimeResult ManualOnly() => new PlayerDoctorRuntimeResult("manual-only", -1, "Run 3_check_dtmapi_status.bat for the offline installation check; no diagnostic process runs at game startup.", false);
        public static PlayerDoctorRuntimeResult NotRun() => new PlayerDoctorRuntimeResult("not-run", -1, "Player Doctor has not run.", false);
        public static PlayerDoctorRuntimeResult Unavailable(string summary) => new PlayerDoctorRuntimeResult("unavailable", -1, summary, false);
        public static PlayerDoctorRuntimeResult Timeout(string summary) => new PlayerDoctorRuntimeResult("timeout", -1, summary, false);
        public static PlayerDoctorRuntimeResult Failed(string summary) => new PlayerDoctorRuntimeResult("failed", -1, summary, false);

        public static PlayerDoctorRuntimeResult FromExitCode(int exitCode, string summary)
        {
            if (exitCode == 0)
            {
                var result = new PlayerDoctorRuntimeResult("ready", exitCode, EnsureSummary(summary, "Player Doctor completed without errors."), true);
                return result.WarningCount > 0
                    ? new PlayerDoctorRuntimeResult("warnings", exitCode, result.Summary, true)
                    : result;
            }
            if (exitCode == 2)
                return new PlayerDoctorRuntimeResult("findings", exitCode, EnsureSummary(summary, "Player Doctor found one or more errors."), true);
            return new PlayerDoctorRuntimeResult("failed", exitCode, EnsureSummary(summary, "Player Doctor failed with exit code " + exitCode.ToString(CultureInfo.InvariantCulture) + "."), false);
        }

        private int ReadCount(string key)
        {
            return Fields.TryGetValue(key, out string? value) && int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int count)
                ? Math.Max(0, count)
                : 0;
        }

        private static string EnsureSummary(string summary, string fallback) => string.IsNullOrWhiteSpace(summary) ? fallback : summary;

        private static IReadOnlyDictionary<string, string> ParseFields(string summary)
        {
            var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string part in (summary ?? string.Empty).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int separator = part.IndexOf('=');
                if (separator <= 0)
                    continue;
                string key = part.Substring(0, separator).Trim();
                string value = part.Substring(separator + 1).Trim();
                if (key.Length > 0 && !fields.ContainsKey(key))
                    fields[key] = value;
            }
            return fields;
        }
    }
}
