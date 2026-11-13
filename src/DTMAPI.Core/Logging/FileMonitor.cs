using System;
using System.Collections.Generic;
using System.IO;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.Core.Logging
{
    internal sealed class FileMonitor : IMonitor
    {
        private const int MaxLogOnceKeys = 256;
        private readonly IRuntimeHost host;
        private readonly string owner;
        private readonly string logPath;
        private readonly Action<string, string, string>? recordError;
        private readonly HashSet<string> onceKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly object gate = new object();
        private bool logOnceCapWarningPublished;
        private bool logSinkFailureReported;
        private int suppressedLogOnceKeyCount;

        public FileMonitor(IRuntimeHost host, string owner, string logPath, Action<string, string, string>? recordError = null)
        {
            this.host = host;
            this.owner = owner;
            this.logPath = logPath;
            this.recordError = recordError;
        }

        public void Log(string message, LogLevel level = LogLevel.Info)
        {
            string line = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz} [{level}] [{owner}] {message}";
            Exception? firstFailure = null;
            try
            {
                lock (gate)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(logPath) ?? ".");
                    File.AppendAllText(logPath, line + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                firstFailure = ex;
            }

            try
            {
                if (level >= LogLevel.Error)
                    host.LogError(line);
                else if (level == LogLevel.Warn)
                    host.LogWarning(line);
                else
                    host.Log(line);
            }
            catch (Exception ex)
            {
                firstFailure = firstFailure ?? ex;
            }

            if (level >= LogLevel.Error && recordError != null)
            {
                try
                {
                    recordError(owner, "Mod monitor reported an error.", line);
                }
                catch (Exception ex)
                {
                    firstFailure = firstFailure ?? ex;
                }
            }

            if (firstFailure != null)
                ReportLogSinkFailureOnce(firstFailure);
        }

        public void LogOnce(string key, string message, LogLevel level = LogLevel.Info)
        {
            bool publishCapWarning = false;
            lock (gate)
            {
                if (onceKeys.Contains(key))
                    return;
                if (onceKeys.Count >= MaxLogOnceKeys)
                {
                    suppressedLogOnceKeyCount++;
                    if (!logOnceCapWarningPublished)
                    {
                        logOnceCapWarningPublished = true;
                        publishCapWarning = true;
                    }
                    else
                        return;
                }
                else
                {
                    onceKeys.Add(key);
                }
            }
            if (publishCapWarning)
            {
                Log("LogOnce key cap reached (max=" + MaxLogOnceKeys + "); new unique keys are suppressed for this monitor.", LogLevel.Warn);
                return;
            }
            Log(message, level);
        }

        internal int RetainedLogOnceKeyCount { get { lock (gate) return onceKeys.Count; } }
        internal int SuppressedLogOnceKeyCount { get { lock (gate) return suppressedLogOnceKeyCount; } }

        public void LogException(Exception exception, string message)
        {
            Log(message + Environment.NewLine + exception, LogLevel.Error);
        }

        private void ReportLogSinkFailureOnce(Exception exception)
        {
            lock (gate)
            {
                if (logSinkFailureReported)
                    return;
                logSinkFailureReported = true;
            }

            try
            {
                recordError?.Invoke(
                    owner,
                    "Logging sink failed; runtime behavior continued.",
                    exception.GetType().Name + ": " + exception.Message);
            }
            catch
            {
                // Logging and logging diagnostics are observations only. They must never
                // block owner publication, reconciliation, deactivation, or shutdown.
            }
        }
    }
}
