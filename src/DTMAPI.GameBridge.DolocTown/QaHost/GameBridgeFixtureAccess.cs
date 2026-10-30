using System;
using System.IO;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class GameBridgeFixtureAccess
    {
        private readonly DtmApiRuntime runtime;
        private readonly Func<bool> initialized;
        private readonly DolocTownGameBridge? bridge;

        internal GameBridgeFixtureAccess(
            DtmApiRuntime runtime,
            string runId,
            string evidenceRoot,
            Func<bool> initialized,
            DolocTownGameBridge? bridge = null)
        {
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            RunId = runId ?? throw new ArgumentNullException(nameof(runId));
            EvidenceRoot = evidenceRoot ?? throw new ArgumentNullException(nameof(evidenceRoot));
            this.initialized = initialized ?? throw new ArgumentNullException(nameof(initialized));
            this.bridge = bridge;
        }

        internal string RunId { get; }

        internal string EvidenceRoot { get; }

        internal string RuntimeEvidenceRoot => runtime.Paths.EvidencePath;

        internal DtmApiRuntime Runtime => runtime;

        internal DolocTownGameBridge Bridge => RequireBridge();

        internal string LatestLogPath => runtime.Diagnostics.GetLatestLogPath();

        internal bool IsBridgeInitialized => initialized();

        internal string InputContext => runtime.UI.InputContext ?? string.Empty;

        internal void Log(string message, LogLevel level = LogLevel.Info)
        {
            runtime.RuntimeMonitor.Log(message ?? string.Empty, level);
        }

        internal void PublishStatus(string status, string details)
        {
            runtime.SetHookStatus(
                "InternalFixture.QaHost",
                status ?? string.Empty,
                "Optional internal fixture participant",
                details ?? string.Empty);
        }

        internal void SetHookStatus(string hookId, string status, string source, string details) =>
            runtime.SetHookStatus(hookId ?? string.Empty, status ?? string.Empty, source ?? string.Empty, details ?? string.Empty);

        internal string ExportDiagnostics() => runtime.ExportLogs();

        internal IDtmDiagnosticsSnapshot GetDiagnosticsSnapshot() => ((IDtmDiagnosticsApi)runtime).GetSnapshot();

        internal string ReadLatestLogText()
        {
            string path = LatestLogPath;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return string.Empty;
            try
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                using (var reader = new StreamReader(stream))
                    return reader.ReadToEnd();
            }
            catch (IOException)
            {
                return string.Empty;
            }
        }

        private DolocTownGameBridge RequireBridge() => bridge ?? throw new InvalidOperationException("G4 fixture access requires an attached DolocTown GameBridge.");
    }
}
