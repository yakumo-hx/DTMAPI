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
        private readonly RuntimePaths paths;
        // Mod logging can arrive from a background thread.
        private readonly object errorGate = new object();
        private readonly List<DtmErrorInfo> errors = new List<DtmErrorInfo>();
        private readonly List<DtmWarningInfo> warnings = new List<DtmWarningInfo>();
        private readonly Dictionary<string, HookStatusInfo> hooks = new Dictionary<string, HookStatusInfo>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, DtmFeatureStatusInfo> features = new Dictionary<string, DtmFeatureStatusInfo>(StringComparer.OrdinalIgnoreCase);

        public DiagnosticsService(RuntimePaths paths)
        {
            this.paths = paths;
        }

        internal IDiagnosticsHelper CreateOwnerBound(string ownerId, Action ensureRuntimeThread, Action ensureOwnerActive) =>
            new OwnerBoundDiagnosticsHelper(this, ensureRuntimeThread, ensureOwnerActive);

        public string LatestLogPath { get; set; } = string.Empty;
        public string LatestReportPath { get; private set; } = string.Empty;

        public IReadOnlyList<IDtmErrorInfo> GetErrors()
        {
            lock (errorGate)
                return errors.ToArray();
        }
        public IReadOnlyList<IDtmWarningInfo> GetWarnings()
        {
            return warnings.ToArray();
        }
        public IReadOnlyList<IHookStatusInfo> GetHookStatuses() => hooks.Values.OrderBy(hook => hook.HookId, StringComparer.OrdinalIgnoreCase).ToArray();
        public IReadOnlyList<IDtmFeatureStatusInfo> GetFeatureStatuses() => features.Values.OrderBy(feature => feature.FeatureId, StringComparer.OrdinalIgnoreCase).ToArray();
        public string GetLatestLogPath() => LatestLogPath;

        public string GetLatestReportPath()
        {
            if (LatestReportPath.Length != 0)
                return LatestReportPath;
            string pointer = Path.Combine(paths.ReportsPath, "latest-report.txt");
            return File.Exists(pointer) ? File.ReadAllText(pointer, Encoding.UTF8).Trim() : string.Empty;
        }

        public void RecordError(string owner, string message, string details)
        {
            lock (errorGate)
                errors.Add(new DtmErrorInfo(owner, message, details));
        }
        internal void ClearErrors(string owner)
        {
            lock (errorGate)
                errors.RemoveAll(error => error.Owner.Equals(owner, StringComparison.OrdinalIgnoreCase));
        }
        internal void RecordWarning(string owner, string message, string details)
        {
            warnings.Add(new DtmWarningInfo(owner, message, details));
        }

        public bool SetHookStatus(string hookId, string status, string source, string details)
        {
            if (hooks.TryGetValue(hookId, out HookStatusInfo existing) &&
                existing.Status == status && existing.Source == source && existing.Details == details)
                return false;
            hooks[hookId] = new HookStatusInfo(hookId, status, source, details);
            return true;
        }

        public void SetFeatureStatus(string featureId, string status, string lastOperation, bool success, int failureCount, string lastError, string details) =>
            features[featureId] = new DtmFeatureStatusInfo(featureId, status, lastOperation, success, failureCount, lastError, details);

        public string ExportLogs() => ExportLogs(string.Empty);

        public string ExportLogs(string runtimeContextSummary)
        {
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
                AddFileIfExists(archive, Path.Combine(paths.DtmApiPath, "debug-console-last-give.txt"), "DTMAPI-state/debug-console-last-give.txt");
                AddFileIfExists(archive, Path.Combine(paths.DtmApiPath, "install-state.json"), "install-state.json");
                AddFileIfExists(archive, Path.Combine(paths.DtmApiPath, "release-manifest.json"), "release-manifest.json");
                AddFiles(archive, paths.DtmApiPath, "install-state.failed-*.json", "DTMAPI-state");
                AddFiles(archive, paths.DtmApiPath, "uninstall-state-*.json", "DTMAPI-state");
                AddUnityCrashReports(archive);
                if (!string.IsNullOrWhiteSpace(runtimeContextSummary))
                    AddText(archive, "DTMAPI-runtime-context.txt", runtimeContextSummary);
                AddText(archive, "dtmapi-summary.txt", BuildSummary(runtimeContextSummary));
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
            IReadOnlyList<IDtmErrorInfo> currentErrors = GetErrors();
            IReadOnlyList<IDtmWarningInfo> currentWarnings = GetWarnings();
            return "DTMAPI diagnostic report" + Environment.NewLine +
                "Generated: " + DateTimeOffset.Now + Environment.NewLine +
                "Errors: " + currentErrors.Count + Environment.NewLine +
                "Warnings: " + currentWarnings.Count + Environment.NewLine +
                "Hooks: " + hooks.Count + Environment.NewLine +
                "Features: " + features.Count + Environment.NewLine +
                (string.IsNullOrWhiteSpace(runtimeContextSummary) ? string.Empty : "RuntimeContext: present; entry=DTMAPI-runtime-context.txt" + Environment.NewLine) +
                BuildJsonStatus("InstallState", Path.Combine(paths.DtmApiPath, "install-state.json"), includeLegacyCounts: true) +
                BuildJsonStatus("ReleaseManifest", Path.Combine(paths.DtmApiPath, "release-manifest.json"), includeLegacyCounts: false) +
                "LatestLogPath: " + LatestLogPath + Environment.NewLine +
                "LatestReportPath: " + LatestReportPath + Environment.NewLine +
                string.Join(Environment.NewLine, GetHookStatuses().Select(hook => $"HOOK {hook.HookId}: {hook.Status} - {hook.Details}")) + Environment.NewLine +
                string.Join(Environment.NewLine, GetFeatureStatuses().Select(feature => $"FEATURE {feature.FeatureId}: {feature.Status} lastOperation={feature.LastOperation} success={feature.Success} failureCount={feature.FailureCount} lastError={feature.LastError} details={feature.Details}")) + Environment.NewLine +
                string.Join(Environment.NewLine, currentWarnings.Select(warning => $"WARNING {warning.Time:o} [{warning.Owner}] {warning.Message}: {warning.Details}")) + Environment.NewLine +
                string.Join(Environment.NewLine, currentErrors.Select(error => $"ERROR {error.Time:o} [{error.Owner}] {error.Message}: {error.Details}"));
        }

        private static string BuildJsonStatus(string label, string path, bool includeLegacyCounts)
        {
            if (!File.Exists(path))
                return label + ": missing" + Environment.NewLine;
            try
            {
                string text = File.ReadAllText(path, Encoding.UTF8);
                string version = ExtractJsonString(text, "DTMAPIVersion");
                string binaryVersion = ExtractJsonString(text, "BinaryVersion");
                string result = label + ": present" + Environment.NewLine + label + "Path: " + path + Environment.NewLine;
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
                return label + ": read-error " + ex.GetType().Name + ": " + ex.Message + Environment.NewLine;
            }
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

        private static bool AddFileIfExists(ZipArchive archive, string path, string entryName)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return false;
            try
            {
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

        private static void AddFiles(ZipArchive archive, string directory, string filter, string entryRoot)
        {
            if (!Directory.Exists(directory))
                return;
            foreach (FileInfo file in new DirectoryInfo(directory).GetFiles(filter))
                AddFileIfExists(archive, file.FullName, entryRoot + "/" + MakeSafeFileName(file.Name));
        }

        private static void AddUnityCrashReports(ZipArchive archive)
        {
            var summary = new List<string> { "Generated: " + DateTimeOffset.Now };
            var missingDumps = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string rootPath in GetUnityCrashRootCandidates(summary))
            {
                summary.Add("CrashRoot: " + rootPath);
                if (!Directory.Exists(rootPath))
                    continue;
                DirectoryInfo[] directories;
                try
                {
                    directories = new DirectoryInfo(rootPath).GetDirectories();
                }
                catch (Exception ex)
                {
                    summary.Add("DirectoryListError: " + rootPath + " " + ex.Message);
                    continue;
                }
                foreach (DirectoryInfo directory in directories)
                {
                    if (!seen.Add(directory.FullName) || IsReparsePoint(directory))
                        continue;
                    string entryRoot = "Unity-Crashes/" + MakeCrashDirectoryEntryName(directory);
                    bool hasDump = false;
                    foreach (FileInfo file in EnumerateFiles(directory, summary))
                    {
                        bool isDump = file.Name.Equals("crash.dmp", StringComparison.OrdinalIgnoreCase);
                        hasDump |= isDump;
                        string entryName = entryRoot + "/" + MakeSafeEntryPath(GetRelativePath(directory.FullName, file.FullName));
                        if (AddFileIfExists(archive, file.FullName, entryName))
                            summary.Add("CopiedFile: " + file.FullName);
                        else if (isDump)
                            missingDumps.Add("Could not copy crash dump: " + file.FullName);
                    }
                    if (!hasDump)
                        missingDumps.Add("Missing crash dump: " + Path.Combine(directory.FullName, "crash.dmp"));
                }
            }
            if (missingDumps.Count != 0)
                AddText(archive, "Unity-Crashes/MISSING-CRASH-DUMP-README.txt", string.Join(Environment.NewLine, missingDumps));
            AddText(archive, "Unity-Crashes/summary.txt", string.Join(Environment.NewLine, summary));
        }

        private static IEnumerable<FileInfo> EnumerateFiles(DirectoryInfo root, List<string> summary)
        {
            var directories = new Stack<DirectoryInfo>();
            directories.Push(root);
            while (directories.Count != 0)
            {
                DirectoryInfo directory = directories.Pop();
                FileInfo[] files;
                try
                {
                    files = directory.GetFiles();
                    foreach (DirectoryInfo child in directory.GetDirectories())
                        if (!IsReparsePoint(child))
                            directories.Push(child);
                }
                catch (Exception ex)
                {
                    summary.Add("FileListError: " + directory.FullName + " " + ex.Message);
                    continue;
                }
                foreach (FileInfo file in files)
                    if (!IsReparsePoint(file))
                        yield return file;
            }
        }

        private static bool IsReparsePoint(FileSystemInfo item) =>
            (item.Attributes & FileAttributes.ReparsePoint) != 0;

        private static void AddText(ZipArchive archive, string entryName, string text)
        {
            ZipArchiveEntry entry = archive.CreateEntry(entryName);
            using (StreamWriter writer = new StreamWriter(entry.Open()))
                writer.Write(text);
        }

        private static IEnumerable<string> GetUnityCrashRootCandidates(List<string> summary)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string tempRoot in new[] { Path.GetTempPath(), Environment.GetEnvironmentVariable("TEMP") ?? string.Empty, Environment.GetEnvironmentVariable("TMP") ?? string.Empty })
            {
                if (string.IsNullOrWhiteSpace(tempRoot))
                    continue;
                foreach (string gameFolder in new[] { "DolocTown", "Doloc Town" })
                {
                    string root;
                    try
                    {
                        root = Path.GetFullPath(Path.Combine(tempRoot, "RedSawGames", gameFolder, "Crashes"));
                    }
                    catch (Exception ex)
                    {
                        summary.Add("CrashRootError: " + tempRoot + " " + ex.Message);
                        continue;
                    }
                    if (seen.Add(root))
                        yield return root;
                }
            }
        }

        private static string MakeSafeEntryPath(string relativePath)
        {
            return string.Join("/", relativePath
                .Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries)
                .Select(MakeSafeFileName));
        }

        private static string MakeCrashDirectoryEntryName(DirectoryInfo directory)
        {
            string safeName = MakeSafeFileName(directory.Name);
            string hash = ShortHash(directory.FullName);
            return safeName + "-" + hash;
        }

        private static string ShortHash(string value)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
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

    }
}
