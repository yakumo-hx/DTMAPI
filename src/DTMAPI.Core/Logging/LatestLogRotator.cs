using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace DTMAPI.Core.Logging
{
    internal static class LatestLogRotator
    {
        internal const int RetainedHistoryCount = 10;

        private const string HistoryPrefix = "latest-";
        private const string HistoryExtension = ".log";

        internal static string HistorySearchPattern => HistoryPrefix + "*" + HistoryExtension;

        internal static string? TryRotateLatest(string latestLogPath, int retainedHistoryCount = RetainedHistoryCount)
        {
            try
            {
                RotateLatest(latestLogPath, retainedHistoryCount);
                return null;
            }
            catch (Exception ex)
            {
                return ex.GetType().Name + ": " + ex.Message;
            }
        }

        internal static string[] GetHistoryFiles(string logsPath, int retainedHistoryCount = RetainedHistoryCount)
        {
            if (string.IsNullOrWhiteSpace(logsPath) || !Directory.Exists(logsPath))
                return Array.Empty<string>();

            int retained = Math.Max(0, retainedHistoryCount);
            return Directory.GetFiles(logsPath, HistorySearchPattern)
                .Select(path => new FileInfo(path))
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .ThenByDescending(file => file.Name, StringComparer.OrdinalIgnoreCase)
                .Take(retained)
                .Select(file => file.FullName)
                .ToArray();
        }

        private static void RotateLatest(string latestLogPath, int retainedHistoryCount)
        {
            if (string.IsNullOrWhiteSpace(latestLogPath))
                return;

            string logDirectory = Path.GetDirectoryName(latestLogPath);
            if (string.IsNullOrWhiteSpace(logDirectory))
                logDirectory = ".";

            Directory.CreateDirectory(logDirectory);

            if (File.Exists(latestLogPath))
            {
                var latestInfo = new FileInfo(latestLogPath);
                if (latestInfo.Length > 0)
                    File.Move(latestLogPath, GetAvailableHistoryPath(logDirectory, latestInfo.LastWriteTimeUtc));
                else
                    File.Delete(latestLogPath);
            }

            TrimHistory(logDirectory, Math.Max(0, retainedHistoryCount));
        }

        private static string GetAvailableHistoryPath(string logDirectory, DateTime stampUtc)
        {
            if (stampUtc.Year < 2000)
                stampUtc = DateTime.UtcNow;

            string stamp = stampUtc.ToString("yyyyMMdd-HHmmssfff", CultureInfo.InvariantCulture);
            string path = Path.Combine(logDirectory, HistoryPrefix + stamp + HistoryExtension);
            int suffix = 1;
            while (File.Exists(path))
            {
                path = Path.Combine(logDirectory, HistoryPrefix + stamp + "-" + suffix + HistoryExtension);
                suffix++;
            }

            return path;
        }

        private static void TrimHistory(string logDirectory, int retainedHistoryCount)
        {
            foreach (FileInfo file in Directory.GetFiles(logDirectory, HistorySearchPattern)
                .Select(path => new FileInfo(path))
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .ThenByDescending(file => file.Name, StringComparer.OrdinalIgnoreCase)
                .Skip(retainedHistoryCount))
            {
                file.Delete();
            }
        }
    }
}
