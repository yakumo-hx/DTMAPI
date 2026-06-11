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

            ManagerReportExportStatus exportReport = new ManagerReportExportStatus(snapshot.LatestLogPath, snapshot.LatestReportPath);
            ManagerModRow[] mods = snapshot.Mods.Select(ManagerModRow.FromMod).OrderBy(m => m.SortRank).ThenBy(m => m.Source, StringComparer.OrdinalIgnoreCase).ThenBy(m => m.Name, StringComparer.OrdinalIgnoreCase).ThenBy(m => m.UniqueID, StringComparer.OrdinalIgnoreCase).ToArray();
            ManagerDiagnosticRow[] errors = snapshot.Errors.Select(e => ManagerDiagnosticRow.FromError(e)).OrderByDescending(e => e.Time).ToArray();
            ManagerDiagnosticRow[] warnings = snapshot.Warnings.Select(w => ManagerDiagnosticRow.FromWarning(w)).OrderByDescending(w => w.Time).ToArray();
            ManagerHookRow[] hooks = snapshot.HookStatuses.Select(ManagerHookRow.FromHook).OrderBy(h => h.SortRank).ThenBy(h => h.HookId, StringComparer.OrdinalIgnoreCase).ToArray();
            ManagerFeatureRow[] features = snapshot.FeatureStatuses.Select(ManagerFeatureRow.FromFeature).OrderBy(f => f.SortRank).ThenBy(f => f.FeatureId, StringComparer.OrdinalIgnoreCase).ToArray();

            return new DtmManagerViewModel(
                mods,
                errors,
                warnings,
                hooks,
                features,
                exportReport,
                ManagerSummary.FromRows(mods, errors, warnings, hooks, features, exportReport));
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
            ManagerReportExportStatus exportReport,
            ManagerSummary summary)
        {
            Mods = mods;
            Errors = errors;
            Warnings = warnings;
            Hooks = hooks;
            Features = features;
            ExportReport = exportReport;
            Summary = summary;
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
        internal ManagerSummary Summary { get; }
    }

    internal sealed class ManagerSummary
    {
        private ManagerSummary(
            int loadedModCount,
            int blockedModCount,
            int disabledModCount,
            int errorCount,
            int warningCount,
            int failedHookCount,
            int missingHookCount,
            int failedFeatureCount,
            int degradedFeatureCount,
            string overallStatus)
        {
            LoadedModCount = loadedModCount;
            BlockedModCount = blockedModCount;
            DisabledModCount = disabledModCount;
            ErrorCount = errorCount;
            WarningCount = warningCount;
            FailedHookCount = failedHookCount;
            MissingHookCount = missingHookCount;
            FailedFeatureCount = failedFeatureCount;
            DegradedFeatureCount = degradedFeatureCount;
            OverallStatus = overallStatus;
        }

        internal static ManagerSummary FromRows(
            IReadOnlyList<ManagerModRow> mods,
            IReadOnlyList<ManagerDiagnosticRow> errors,
            IReadOnlyList<ManagerDiagnosticRow> warnings,
            IReadOnlyList<ManagerHookRow> hooks,
            IReadOnlyList<ManagerFeatureRow> features,
            ManagerReportExportStatus exportReport)
        {
            int loadedModCount = mods.Count(m => m.IsLoaded);
            int blockedModCount = mods.Count(m => m.IsBlocked);
            int disabledModCount = mods.Count(m => m.IsDisabled);
            int errorCount = errors.Count;
            int warningCount = warnings.Count;
            int failedHookCount = hooks.Count(h => h.IsFailed);
            int missingHookCount = hooks.Count(h => h.IsMissing);
            int failedFeatureCount = features.Count(f => f.IsFailed);
            int degradedFeatureCount = features.Count(f => f.IsDegraded);

            string overallStatus;
            if (blockedModCount > 0 || errorCount > 0 || failedHookCount > 0 || failedFeatureCount > 0)
                overallStatus = "failed";
            else if (warningCount > 0 || disabledModCount > 0 || missingHookCount > 0 || degradedFeatureCount > 0 || exportReport.Status == "missing-report" || exportReport.Status == "missing-log" || exportReport.Status == "unavailable")
                overallStatus = "warning";
            else
                overallStatus = "ready";

            return new ManagerSummary(loadedModCount, blockedModCount, disabledModCount, errorCount, warningCount, failedHookCount, missingHookCount, failedFeatureCount, degradedFeatureCount, overallStatus);
        }

        internal int LoadedModCount { get; }
        internal int BlockedModCount { get; }
        internal int DisabledModCount { get; }
        internal int ErrorCount { get; }
        internal int WarningCount { get; }
        internal int FailedHookCount { get; }
        internal int MissingHookCount { get; }
        internal int FailedFeatureCount { get; }
        internal int DegradedFeatureCount { get; }
        internal string OverallStatus { get; }
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
            IsLoaded = Loaded || IsStatus("loaded");
            IsDisabled = IsStatus("disabled") || (OfficialEnablementManaged && !OfficialEnabled);
            IsBlocked = !IsDisabled && (IsStatus("missing-dependency") || IsStatus("dependency-cycle") || IsStatus("entry-dll-error") || IsStatus("code-load-error") || IsStatus("api-too-new") || IsStatus("unknown-error") || IsStatus("blocked") || IsStatus("error"));
            IsWarning = IsStatus("warning");
            SortRank = GetSortRank();
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
        internal bool IsLoaded { get; }
        internal bool IsBlocked { get; }
        internal bool IsDisabled { get; }
        internal bool IsWarning { get; }
        internal int SortRank { get; }

        private int GetSortRank()
        {
            if (IsBlocked)
                return 0;

            if (IsWarning)
                return 1;

            if (IsDisabled)
                return 2;

            if (IsLoaded)
                return 3;

            return 4;
        }

        private bool IsStatus(string value)
        {
            return string.Equals(StatusCode, value, StringComparison.OrdinalIgnoreCase) || string.Equals(Status, value, StringComparison.OrdinalIgnoreCase);
        }

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
            IsFailed = IsStatus("failed");
            IsMissing = IsStatus("missing");
            SortRank = GetSortRank();
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
        internal bool IsFailed { get; }
        internal bool IsMissing { get; }
        internal int SortRank { get; }

        private int GetSortRank()
        {
            if (IsStatus("failed"))
                return 0;

            if (IsStatus("missing"))
                return 1;

            if (IsStatus("pending"))
                return 2;

            if (IsStatus("experimental"))
                return 3;

            if (IsStatus("verified"))
                return 4;

            if (IsStatus("ready"))
                return 5;

            return 6;
        }

        private bool IsStatus(string value)
        {
            return string.Equals(Status, value, StringComparison.OrdinalIgnoreCase);
        }
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
            IsFailed = !Success || IsStatus("failed") || !string.IsNullOrWhiteSpace(LastError);
            IsDegraded = !IsFailed && (IsStatus("degraded") || FailureCount > 0);
            SortRank = GetSortRank();
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
        internal bool IsFailed { get; }
        internal bool IsDegraded { get; }
        internal int SortRank { get; }

        private int GetSortRank()
        {
            if (IsFailed)
                return 0;

            if (IsDegraded)
                return 1;

            if (IsStatus("ready"))
                return 2;

            return 3;
        }

        private bool IsStatus(string value)
        {
            return string.Equals(Status, value, StringComparison.OrdinalIgnoreCase);
        }
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
