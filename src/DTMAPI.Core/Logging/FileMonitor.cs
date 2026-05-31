using System;
using System.Collections.Generic;
using System.IO;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.Core.Logging
{
    internal sealed class FileMonitor : IMonitor
    {
        private readonly IRuntimeHost host;
        private readonly string owner;
        private readonly string logPath;
        private readonly HashSet<string> onceKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly object gate = new object();

        public FileMonitor(IRuntimeHost host, string owner, string logPath)
        {
            this.host = host;
            this.owner = owner;
            this.logPath = logPath;
        }

        public void Log(string message, LogLevel level = LogLevel.Info)
        {
            string line = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz} [{level}] [{owner}] {message}";
            lock (gate)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(logPath) ?? ".");
                File.AppendAllText(logPath, line + Environment.NewLine);
            }

            if (level >= LogLevel.Error)
                host.LogError(line);
            else if (level == LogLevel.Warn)
                host.LogWarning(line);
            else
                host.Log(line);
        }

        public void LogOnce(string key, string message, LogLevel level = LogLevel.Info)
        {
            lock (gate)
            {
                if (!onceKeys.Add(key))
                    return;
            }
            Log(message, level);
        }

        public void LogException(Exception exception, string message)
        {
            Log(message + Environment.NewLine + exception, LogLevel.Error);
        }
    }
}
