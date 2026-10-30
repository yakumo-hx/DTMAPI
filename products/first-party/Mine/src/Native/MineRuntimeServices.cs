using System;
using System.Collections.Generic;
using DTMAPI.Abstractions;

namespace DTMAPI.Mine
{
    internal sealed class MineRuntimeServices
    {
        private readonly IMonitor monitor;
        private readonly HashSet<string> once =
            new HashSet<string>(StringComparer.Ordinal);

        internal MineRuntimeServices(IMonitor monitor)
        {
            this.monitor = monitor ??
                throw new ArgumentNullException(nameof(monitor));
            RuntimeMonitor = new RuntimeLog(this);
            Diagnostics = new RuntimeDiagnostics(this);
        }

        internal RuntimeLog RuntimeMonitor { get; }
        internal RuntimeDiagnostics Diagnostics { get; }

        internal void SetHookStatus(
            string id,
            string status,
            string owner,
            string message)
        {
            if (status.Equals(
                    "failed",
                    StringComparison.OrdinalIgnoreCase))
            {
                monitor.Log(
                    id + " failed owner=" + owner + " " + message,
                    LogLevel.Error);
            }
        }

        internal sealed class RuntimeLog
        {
            private readonly MineRuntimeServices owner;

            internal RuntimeLog(MineRuntimeServices owner)
            {
                this.owner = owner;
            }

            internal void Log(
                string message,
                LogLevel level = LogLevel.Info) =>
                owner.monitor.Log(message, level);

            internal void LogOnce(string key, string message)
            {
                if (owner.once.Add(key))
                    owner.monitor.Log(message);
            }
        }

        internal sealed class RuntimeDiagnostics
        {
            private readonly MineRuntimeServices owner;

            internal RuntimeDiagnostics(MineRuntimeServices owner)
            {
                this.owner = owner;
            }

            internal void RecordError(
                string source,
                string message,
                string detail) =>
                owner.monitor.Log(
                    source + ": " + message + " " + detail,
                    LogLevel.Error);
        }
    }
}
