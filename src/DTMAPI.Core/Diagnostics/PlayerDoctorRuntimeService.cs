using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using DTMAPI.Core.Runtime;

namespace DTMAPI.Core.Diagnostics
{
    internal sealed class PlayerDoctorRuntimeService
    {
        private const int TimeoutMilliseconds = 5000;
        private const int MaxSummaryCharacters = 4096;
        private bool attempted;
        private PlayerDoctorRuntimeResult latest = PlayerDoctorRuntimeResult.NotRun();

        public PlayerDoctorRuntimeResult RunOnce(RuntimePaths paths, string runtimeVersion)
        {
            if (attempted)
                return latest;
            attempted = true;
            DeleteStalePendingOutputsBestEffort(paths);

            if (!File.Exists(paths.PlayerDoctorPath))
            {
                DeleteOwnedOutputsBestEffort(paths);
                latest = PlayerDoctorRuntimeResult.Unavailable(
                    "Player Doctor helper is not installed. Run 3_check_dtmapi_status.bat for the offline installation check.");
                return latest;
            }

            string pendingSuffix = ".pending-" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
            string pendingJson = paths.PlayerDoctorJsonReportPath + pendingSuffix;
            string pendingText = paths.PlayerDoctorTextReportPath + pendingSuffix;
            string pendingSummary = paths.PlayerDoctorSummaryPath + pendingSuffix;
            try
            {
                DeleteOwnedOutputs(paths);

                var start = new ProcessStartInfo
                {
                    FileName = paths.PlayerDoctorPath,
                    Arguments = BuildArguments(paths, runtimeVersion, pendingJson, pendingText, pendingSummary),
                    WorkingDirectory = paths.GamePath,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false
                };
                using (Process? process = Process.Start(start))
                {
                    if (process == null)
                    {
                        latest = PlayerDoctorRuntimeResult.Failed("Player Doctor process could not be started.");
                        return latest;
                    }

                    if (!process.WaitForExit(TimeoutMilliseconds))
                    {
                        bool terminated = false;
                        try
                        {
                            process.Kill();
                            terminated = process.WaitForExit(1000);
                        }
                        catch
                        {
                            // The bounded child is best-effort terminated. Runtime startup must continue.
                        }
                        DeletePendingOutputsBestEffort(pendingJson, pendingText, pendingSummary);
                        latest = PlayerDoctorRuntimeResult.Timeout(
                            "Player Doctor exceeded the " + TimeoutMilliseconds.ToString(CultureInfo.InvariantCulture) +
                            " ms startup budget; child termination " + (terminated ? "completed." : "could not be confirmed."));
                        return latest;
                    }

                    string summary = ReadBoundedSummary(pendingSummary);
                    latest = PlayerDoctorRuntimeResult.FromExitCode(process.ExitCode, summary);
                    if (latest.Completed)
                    {
                        PublishOwnedOutput(pendingJson, paths.PlayerDoctorJsonReportPath);
                        PublishOwnedOutput(pendingText, paths.PlayerDoctorTextReportPath);
                        PublishOwnedOutput(pendingSummary, paths.PlayerDoctorSummaryPath);
                    }
                    else
                    {
                        DeletePendingOutputsBestEffort(pendingJson, pendingText, pendingSummary);
                    }
                    return latest;
                }
            }
            catch (Exception ex)
            {
                DeletePendingOutputsBestEffort(pendingJson, pendingText, pendingSummary);
                DeleteOwnedOutputsBestEffort(paths);
                latest = PlayerDoctorRuntimeResult.Failed(ex.GetType().Name + ": " + ex.Message);
                return latest;
            }
        }

        public PlayerDoctorRuntimeResult GetLatest() => latest;

        private static string BuildArguments(
            RuntimePaths paths,
            string runtimeVersion,
            string jsonOutput,
            string textOutput,
            string summaryOutput)
        {
            var arguments = new[]
            {
                "inspect",
                "--game-root", paths.GamePath,
                "--runtime-version", runtimeVersion,
                "--scan-context", "installed-game",
                "--json-output", jsonOutput,
                "--text-output", textOutput,
                "--summary-output", summaryOutput,
                "--quiet"
            };
            var builder = new StringBuilder();
            foreach (string argument in arguments)
            {
                if (builder.Length > 0)
                    builder.Append(' ');
                builder.Append(QuoteWindowsArgument(argument));
            }
            return builder.ToString();
        }

        private static string QuoteWindowsArgument(string value)
        {
            if (value.Length > 0 && value.IndexOfAny(new[] { ' ', '\t', '\n', '\v', '"' }) < 0)
                return value;

            var result = new StringBuilder(value.Length + 2);
            result.Append('"');
            int backslashes = 0;
            foreach (char character in value)
            {
                if (character == '\\')
                {
                    backslashes++;
                    continue;
                }
                if (character == '"')
                {
                    result.Append('\\', backslashes * 2 + 1);
                    result.Append('"');
                    backslashes = 0;
                    continue;
                }
                result.Append('\\', backslashes);
                backslashes = 0;
                result.Append(character);
            }
            result.Append('\\', backslashes * 2);
            result.Append('"');
            return result.ToString();
        }

        private static void DeleteOwnedOutput(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }

        private static void DeleteOwnedOutputs(RuntimePaths paths)
        {
            DeleteOwnedOutput(paths.PlayerDoctorJsonReportPath);
            DeleteOwnedOutput(paths.PlayerDoctorTextReportPath);
            DeleteOwnedOutput(paths.PlayerDoctorSummaryPath);
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

        private static void DeletePendingOutputsBestEffort(params string[] paths)
        {
            foreach (string path in paths)
            {
                try
                {
                    DeleteOwnedOutput(path);
                }
                catch
                {
                    // Pending output is never exported, so cleanup is best effort only.
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

        private static void PublishOwnedOutput(string pendingPath, string finalPath)
        {
            if (!File.Exists(pendingPath))
                return;
            DeleteOwnedOutput(finalPath);
            File.Move(pendingPath, finalPath);
        }

        private static string ReadBoundedSummary(string path)
        {
            if (!File.Exists(path))
                return string.Empty;
            using (var reader = new StreamReader(path, Encoding.UTF8, true, 1024))
            {
                char[] buffer = new char[MaxSummaryCharacters + 1];
                int read = reader.ReadBlock(buffer, 0, buffer.Length);
                string value = new string(buffer, 0, Math.Min(read, MaxSummaryCharacters)).Trim();
                return read > MaxSummaryCharacters ? value + " [trimmed]" : value;
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
