using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DTMAPI.Abstractions;

namespace DTMAPI.Core.Manager
{
    internal static class DtmManagerViewModelFactory
    {
        internal static DtmManagerViewModel FromSnapshot(IDtmDiagnosticsSnapshot snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            return new DtmManagerViewModel(
                snapshot.Mods.Select(ManagerModRow.FromMod).ToArray(),
                snapshot.Errors.Select(e => ManagerDiagnosticRow.FromError(e)).ToArray(),
                snapshot.Warnings.Select(w => ManagerDiagnosticRow.FromWarning(w)).ToArray(),
                snapshot.HookStatuses.Select(ManagerHookRow.FromHook).ToArray(),
                snapshot.FeatureStatuses.Select(ManagerFeatureRow.FromFeature).ToArray(),
                new ManagerReportExportStatus(snapshot.LatestLogPath, snapshot.LatestReportPath));
        }
    }

    internal sealed class DtmManagerViewModel
    {
        internal DtmManagerViewModel(
            IReadOnlyList<ManagerModRow> mods,
            IReadOnlyList<ManagerDiagnosticRow> errors,
            IReadOnlyList<ManagerDiagnosticRow> warnings,
            IReadOnlyList<ManagerHookRow> hooks,
            IReadOnlyList<ManagerFeatureRow> features,
            ManagerReportExportStatus exportReport)
        {
            Mods = mods;
            Errors = errors;
            Warnings = warnings;
            Hooks = hooks;
            Features = features;
            ExportReport = exportReport;
            LatestLogPath = exportReport.LatestLogPath;
            LatestReportPath = exportReport.LatestReportPath;
        }

        internal IReadOnlyList<ManagerModRow> Mods { get; }
        internal IReadOnlyList<ManagerDiagnosticRow> Errors { get; }
        internal IReadOnlyList<ManagerDiagnosticRow> Warnings { get; }
        internal IReadOnlyList<ManagerHookRow> Hooks { get; }
        internal IReadOnlyList<ManagerFeatureRow> Features { get; }
        internal string LatestLogPath { get; }
        internal string LatestReportPath { get; }
        internal ManagerReportExportStatus ExportReport { get; }
    }

    internal sealed class ManagerModRow
    {
        private ManagerModRow(IDtmModStatusInfo mod)
        {
            UniqueID = mod.UniqueID;
            Name = mod.Name;
            Version = mod.Version;
            Type = mod.Type;
            Source = mod.Source;
            EntryType = mod.EntryType;
            EntryDll = mod.EntryDll;
            Loaded = mod.Loaded;
            Status = mod.Status;
            StatusCode = mod.StatusCode;
            Reason = mod.Reason;
            OfficialEnabled = mod.OfficialEnabled;
            OfficialEnablementManaged = mod.OfficialEnablementManaged;
            EnablementReason = mod.EnablementReason;
            ManifestPath = mod.ManifestPath;
            RootPath = mod.RootPath;
        }

        internal static ManagerModRow FromMod(IDtmModStatusInfo mod)
        {
            return new ManagerModRow(mod);
        }

        internal string UniqueID { get; }
        internal string Name { get; }
        internal string Version { get; }
        internal string Type { get; }
        internal string Source { get; }
        internal string EntryType { get; }
        internal string EntryDll { get; }
        internal bool Loaded { get; }
        internal string Status { get; }
        internal string StatusCode { get; }
        internal string Reason { get; }
        internal bool OfficialEnabled { get; }
        internal bool OfficialEnablementManaged { get; }
        internal string EnablementReason { get; }
        internal string ManifestPath { get; }
        internal string RootPath { get; }
    }

    internal sealed class ManagerDiagnosticRow
    {
        private ManagerDiagnosticRow(string severity, DateTimeOffset time, string owner, string message, string details)
        {
            Severity = severity;
            Time = time;
            Owner = owner ?? string.Empty;
            Message = message ?? string.Empty;
            Details = details ?? string.Empty;
        }

        internal static ManagerDiagnosticRow FromError(IDtmErrorInfo error)
        {
            return new ManagerDiagnosticRow("Error", error.Time, error.Owner, error.Message, error.Details);
        }

        internal static ManagerDiagnosticRow FromWarning(IDtmWarningInfo warning)
        {
            return new ManagerDiagnosticRow("Warning", warning.Time, warning.Owner, warning.Message, warning.Details);
        }

        internal string Severity { get; }
        internal DateTimeOffset Time { get; }
        internal string Owner { get; }
        internal string Message { get; }
        internal string Details { get; }
    }

    internal sealed class ManagerHookRow
    {
        private ManagerHookRow(IHookStatusInfo hook)
        {
            HookId = hook.HookId;
            Status = hook.Status;
            Target = hook.Source;
            Details = hook.Details;
            UpdatedAt = hook.UpdatedAt;
        }

        internal static ManagerHookRow FromHook(IHookStatusInfo hook)
        {
            return new ManagerHookRow(hook);
        }

        internal string HookId { get; }
        internal string Status { get; }
        internal string Target { get; }
        internal string Details { get; }
        internal DateTimeOffset UpdatedAt { get; }
    }

    internal sealed class ManagerFeatureRow
    {
        private ManagerFeatureRow(IDtmFeatureStatusInfo feature)
        {
            FeatureId = feature.FeatureId;
            Status = feature.Status;
            LastOperation = feature.LastOperation;
            Success = feature.Success;
            FailureCount = feature.FailureCount;
            LastError = feature.LastError;
            Details = feature.Details;
            UpdatedAt = feature.UpdatedAt;
        }

        internal static ManagerFeatureRow FromFeature(IDtmFeatureStatusInfo feature)
        {
            return new ManagerFeatureRow(feature);
        }

        internal string FeatureId { get; }
        internal string Status { get; }
        internal string LastOperation { get; }
        internal bool Success { get; }
        internal int FailureCount { get; }
        internal string LastError { get; }
        internal string Details { get; }
        internal DateTimeOffset UpdatedAt { get; }
    }

    internal sealed class ManagerReportExportStatus
    {
        internal ManagerReportExportStatus(string latestLogPath, string latestReportPath)
        {
            LatestLogPath = latestLogPath ?? string.Empty;
            LatestReportPath = latestReportPath ?? string.Empty;
            HasLatestLogPath = !string.IsNullOrWhiteSpace(LatestLogPath);
            HasLatestReportPath = !string.IsNullOrWhiteSpace(LatestReportPath);
            LatestLogExists = HasLatestLogPath && File.Exists(LatestLogPath);
            LatestReportExists = HasLatestReportPath && File.Exists(LatestReportPath);
            Status = GetStatus();
        }

        internal string LatestLogPath { get; }
        internal string LatestReportPath { get; }
        internal bool HasLatestLogPath { get; }
        internal bool HasLatestReportPath { get; }
        internal bool LatestLogExists { get; }
        internal bool LatestReportExists { get; }
        internal string Status { get; }

        private string GetStatus()
        {
            if (!HasLatestLogPath && !HasLatestReportPath)
                return "unavailable";

            if (HasLatestReportPath && !LatestReportExists)
                return "missing-report";

            if (HasLatestLogPath && !LatestLogExists)
                return "missing-log";

            if (LatestReportExists)
                return "ready";

            return "no-report";
        }
    }
}
