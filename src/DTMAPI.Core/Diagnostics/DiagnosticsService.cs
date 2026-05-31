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
        private readonly RuntimePaths paths;
        private readonly List<DtmErrorInfo> errors = new List<DtmErrorInfo>();
        private readonly Dictionary<string, HookStatusInfo> hooks = new Dictionary<string, HookStatusInfo>(StringComparer.OrdinalIgnoreCase);
        private readonly object gate = new object();

        public DiagnosticsService(RuntimePaths paths)
        {
            this.paths = paths;
        }

        public string LatestLogPath { get; set; } = string.Empty;

        public IReadOnlyList<IDtmErrorInfo> GetErrors()
        {
            lock (gate)
                return errors.Cast<IDtmErrorInfo>().ToArray();
        }

        public IReadOnlyList<IHookStatusInfo> GetHookStatuses()
        {
            lock (gate)
                return hooks.Values.OrderBy(h => h.HookId, StringComparer.OrdinalIgnoreCase).Cast<IHookStatusInfo>().ToArray();
        }

        public string GetLatestLogPath() => LatestLogPath;

        public void RecordError(string owner, string message, string details)
        {
            lock (gate)
                errors.Add(new DtmErrorInfo(owner, message, details));
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

        public string ExportLogs()
        {
            Directory.CreateDirectory(paths.ReportsPath);
            string stamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss");
            string zipPath = Path.Combine(paths.ReportsPath, $"dtmapi-report-{stamp}.zip");
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
            IReadOnlyList<IHookStatusInfo> hookSnapshot = GetHookStatuses();
            return
                "DTMAPI diagnostic report" + Environment.NewLine +
                "Generated: " + DateTimeOffset.Now + Environment.NewLine +
                "Errors: " + errorSnapshot.Count + Environment.NewLine +
                "Hooks: " + hookSnapshot.Count + Environment.NewLine +
                Environment.NewLine +
                string.Join(Environment.NewLine, hookSnapshot.Select(h => $"HOOK {h.HookId}: {h.Status} - {h.Details}")) +
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
    }
}
