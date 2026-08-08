using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using DTMAPI.Abstractions;
using DTMAPI.Core.Diagnostics;
using DTMAPI.Core.Json;
using DTMAPI.Core.Runtime;

namespace DTMAPI.Core.Manager
{
    internal static class DtmManagerViewModelFactory
    {
        internal static DtmManagerViewModel FromSnapshot(
            IDtmDiagnosticsSnapshot snapshot,
            ManagerInstallStateSummary? installState = null,
            ContentManifestRegistrySnapshot? contentRegistry = null)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            installState ??= ManagerInstallStateSummary.Missing(string.Empty, string.Empty, false, string.Empty);
            ManagerReportExportStatus exportReport = new ManagerReportExportStatus(snapshot.LatestLogPath, snapshot.LatestReportPath);
            IReadOnlyDictionary<string, ContentManifestRegistryRow> registryRows = contentRegistry?.Rows
                .GroupBy(row => row.UniqueID, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase)
                ?? new Dictionary<string, ContentManifestRegistryRow>(StringComparer.OrdinalIgnoreCase);
            ManagerModRow[] mods = snapshot.Mods
                .Select(mod => ManagerModRow.FromMod(mod, registryRows.TryGetValue(mod.UniqueID, out ContentManifestRegistryRow registryRow) ? registryRow : null))
                .OrderBy(m => m.SortRank)
                .ThenBy(m => m.Source, StringComparer.OrdinalIgnoreCase)
                .ThenBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(m => m.UniqueID, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            ManagerDiagnosticRow[] errors = snapshot.Errors.Select(e => ManagerDiagnosticRow.FromError(e)).OrderByDescending(e => e.Time).ToArray();
            ManagerDiagnosticRow[] warnings = snapshot.Warnings.Select(w => ManagerDiagnosticRow.FromWarning(w)).OrderByDescending(w => w.Time).ToArray();
            ManagerHookRow[] hooks = snapshot.HookStatuses.Select(ManagerHookRow.FromHook).OrderBy(h => h.SortRank).ThenBy(h => h.HookId, StringComparer.OrdinalIgnoreCase).ToArray();
            ManagerFeatureRow[] features = snapshot.FeatureStatuses.Select(ManagerFeatureRow.FromFeature).OrderBy(f => f.SortRank).ThenBy(f => f.FeatureId, StringComparer.OrdinalIgnoreCase).ToArray();
            ManagerAdvancedDiagnostics advancedDiagnostics = ManagerAdvancedDiagnostics.From(contentRegistry, mods, hooks, features);

            return new DtmManagerViewModel(
                mods,
                errors,
                warnings,
                hooks,
                features,
                exportReport,
                installState,
                ManagerSummary.FromRows(mods, errors, warnings, hooks, features, exportReport),
                advancedDiagnostics);
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
            ManagerInstallStateSummary installState,
            ManagerSummary summary,
            ManagerAdvancedDiagnostics advancedDiagnostics)
        {
            Mods = mods;
            Errors = errors;
            Warnings = warnings;
            Hooks = hooks;
            Features = features;
            ExportReport = exportReport;
            InstallState = installState ?? ManagerInstallStateSummary.Missing(string.Empty, string.Empty, false, string.Empty);
            Summary = summary;
            AdvancedDiagnostics = advancedDiagnostics;
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
        internal ManagerInstallStateSummary InstallState { get; }
        internal ManagerSummary Summary { get; }
        internal ManagerAdvancedDiagnostics AdvancedDiagnostics { get; }
    }

    internal sealed class ManagerSummary
    {
        private ManagerSummary(
            int loadedModCount,
            int blockedModCount,
            int disabledModCount,
            int dependencyIssueModCount,
            int restartRequiredModCount,
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
            DependencyIssueModCount = dependencyIssueModCount;
            RestartRequiredModCount = restartRequiredModCount;
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
            int dependencyIssueModCount = mods.Count(m => m.HasDependencyIssue);
            int restartRequiredModCount = mods.Count(m => m.RequiresRestart);
            int errorCount = errors.Count;
            int warningCount = warnings.Count;
            int failedHookCount = hooks.Count(h => h.IsFailed);
            int missingHookCount = hooks.Count(h => h.IsMissing);
            int failedFeatureCount = features.Count(f => f.IsFailed);
            int degradedFeatureCount = features.Count(f => f.IsDegraded);

            string overallStatus;
            if (blockedModCount > 0 || errorCount > 0 || failedHookCount > 0 || failedFeatureCount > 0)
                overallStatus = "failed";
            else if (warningCount > 0 || disabledModCount > 0 || dependencyIssueModCount > 0 || restartRequiredModCount > 0 || missingHookCount > 0 || degradedFeatureCount > 0 || exportReport.Status == "missing-report" || exportReport.Status == "missing-log" || exportReport.Status == "unavailable")
                overallStatus = "warning";
            else
                overallStatus = "ready";

            return new ManagerSummary(loadedModCount, blockedModCount, disabledModCount, dependencyIssueModCount, restartRequiredModCount, errorCount, warningCount, failedHookCount, missingHookCount, failedFeatureCount, degradedFeatureCount, overallStatus);
        }

        internal int LoadedModCount { get; }
        internal int BlockedModCount { get; }
        internal int DisabledModCount { get; }
        internal int DependencyIssueModCount { get; }
        internal int RestartRequiredModCount { get; }
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
        private ManagerModRow(IDtmModStatusInfo mod, ContentManifestRegistryRow? registryRow)
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
            if (mod is DtmModStatusInfo classified)
            {
                ManagedIdentity = classified.ManagedIdentity;
                DeclaredKind = classified.DeclaredKind;
                EffectiveKind = classified.EffectiveKind;
                DeclarationProvenance = classified.DeclarationProvenance;
                ManagedPlacement = classified.ManagedPlacement;
                NativeRisk = classified.NativeRisk;
                GameCompatibility = classified.GameCompatibility;
                RestartPolicy = classified.RestartPolicy;
                ExpectedHarmonyOwner = classified.ExpectedHarmonyOwner;
                AdvancedReferenceVerified = classified.AdvancedReferenceVerified;
                ReferencePolicyId = classified.ReferencePolicyId;
                ReferencePolicyVersion = classified.ReferencePolicyVersion;
                TargetFramework = classified.TargetFramework;
                GameBuildId = classified.GameBuildId;
            }
            Dependencies = registryRow?.Dependencies.Select(ManagerDependencyRow.FromRegistry).ToArray()
                ?? Array.Empty<ManagerDependencyRow>();
            IsLoaded = Loaded || IsStatus("loaded");
            IsDisabled = IsStatus("disabled") || (OfficialEnablementManaged && !OfficialEnabled);
            IsBlocked = !IsDisabled && (IsStatus("missing-dependency") || IsStatus("dependency-cycle") || IsStatus("entry-dll-error") || IsStatus("code-load-error") || IsStatus("api-too-new") || IsStatus("unknown-error") || IsStatus("blocked") || IsStatus("error"));
            IsWarning = IsStatus("warning");
            HasDependencyIssue = Dependencies.Any(dependency => dependency.IsIssue);
            RequiresRestart = IsStatus("restart-required") || (Loaded && OfficialEnablementManaged && !OfficialEnabled);
            RestartHint = GetRestartHint();
            SortRank = GetSortRank();
        }

        internal static ManagerModRow FromMod(IDtmModStatusInfo mod, ContentManifestRegistryRow? registryRow = null)
        {
            return new ManagerModRow(mod, registryRow);
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
        internal string ManagedIdentity { get; } = string.Empty;
        internal string DeclaredKind { get; } = string.Empty;
        internal string EffectiveKind { get; } = string.Empty;
        internal string DeclarationProvenance { get; } = string.Empty;
        internal string ManagedPlacement { get; } = string.Empty;
        internal string NativeRisk { get; } = string.Empty;
        internal string GameCompatibility { get; } = string.Empty;
        internal string RestartPolicy { get; } = string.Empty;
        internal string ExpectedHarmonyOwner { get; } = string.Empty;
        internal bool AdvancedReferenceVerified { get; }
        internal string ReferencePolicyId { get; } = string.Empty;
        internal int ReferencePolicyVersion { get; }
        internal string TargetFramework { get; } = string.Empty;
        internal string GameBuildId { get; } = string.Empty;
        internal IReadOnlyList<ManagerDependencyRow> Dependencies { get; }
        internal bool IsLoaded { get; }
        internal bool IsBlocked { get; }
        internal bool IsDisabled { get; }
        internal bool IsWarning { get; }
        internal bool HasDependencyIssue { get; }
        internal bool RequiresRestart { get; }
        internal string RestartHint { get; }
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

        private string GetRestartHint()
        {
            if (ManagedIdentity.Equals("Third-party native compatibility CodeMod", StringComparison.Ordinal))
                return "Restart Doloc Town after disable, unsubscribe, or update; DTMAPI does not claim hot cleanup of author-owned native hooks.";

            if (IsStatus("restart-required"))
                return "Restart Doloc Town before this Mod can run again.";

            if (Loaded && OfficialEnablementManaged && !OfficialEnabled)
                return "Restart Doloc Town to unload this disabled CodeMod.";

            if (!string.IsNullOrWhiteSpace(RestartPolicy) &&
                !RestartPolicy.Equals("in-process-owner-lifecycle", StringComparison.OrdinalIgnoreCase))
                return "Assembly updates and disable/unload changes take effect after restart.";

            return string.Empty;
        }
    }

    internal sealed class ManagerDependencyRow
    {
        private ManagerDependencyRow(ContentManifestDependencyRow dependency)
        {
            UniqueID = dependency.UniqueID;
            Required = dependency.Required;
            MinimumVersion = dependency.MinimumVersion;
            Status = dependency.Status;
            Severity = dependency.Severity;
            Details = dependency.Details;
            IsIssue = Severity.Equals("error", StringComparison.OrdinalIgnoreCase) ||
                Severity.Equals("warning", StringComparison.OrdinalIgnoreCase);
        }

        internal static ManagerDependencyRow FromRegistry(ContentManifestDependencyRow dependency)
        {
            return new ManagerDependencyRow(dependency);
        }

        internal string UniqueID { get; }
        internal bool Required { get; }
        internal string MinimumVersion { get; }
        internal string Status { get; }
        internal string Severity { get; }
        internal string Details { get; }
        internal bool IsIssue { get; }
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

    internal sealed class ManagerAdvancedDiagnostics
    {
        private ManagerAdvancedDiagnostics(
            bool registryAvailable,
            DateTimeOffset capturedAt,
            string refreshReason,
            int registryRowCount,
            int manifestIssueCount,
            int dependencyErrorCount,
            int dependencyWarningCount,
            int apiTooNewCount,
            int diffCount,
            int advancedModCount,
            int advancedReferenceVerifiedCount,
            int nativeRiskModCount,
            int restartRequiredModCount,
            int failedOrMissingHookCount,
            int failedOrDegradedFeatureCount,
            IReadOnlyList<ManagerAdvancedDiagnosticRow> rows)
        {
            RegistryAvailable = registryAvailable;
            CapturedAt = capturedAt;
            RefreshReason = refreshReason ?? string.Empty;
            RegistryRowCount = registryRowCount;
            ManifestIssueCount = manifestIssueCount;
            DependencyErrorCount = dependencyErrorCount;
            DependencyWarningCount = dependencyWarningCount;
            ApiTooNewCount = apiTooNewCount;
            DiffCount = diffCount;
            AdvancedModCount = advancedModCount;
            AdvancedReferenceVerifiedCount = advancedReferenceVerifiedCount;
            NativeRiskModCount = nativeRiskModCount;
            RestartRequiredModCount = restartRequiredModCount;
            FailedOrMissingHookCount = failedOrMissingHookCount;
            FailedOrDegradedFeatureCount = failedOrDegradedFeatureCount;
            Rows = rows;
        }

        internal static ManagerAdvancedDiagnostics From(
            ContentManifestRegistrySnapshot? registry,
            IReadOnlyList<ManagerModRow> mods,
            IReadOnlyList<ManagerHookRow> hooks,
            IReadOnlyList<ManagerFeatureRow> features)
        {
            ManagerAdvancedDiagnosticRow[] rows = registry == null
                ? Array.Empty<ManagerAdvancedDiagnosticRow>()
                : registry.Diagnostics.Select(ManagerAdvancedDiagnosticRow.FromRegistry)
                    .Concat(registry.Diffs.Select(ManagerAdvancedDiagnosticRow.FromDiff))
                    .OrderBy(row => row.SortRank)
                    .ThenBy(row => row.Category, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(row => row.Owner, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

            return new ManagerAdvancedDiagnostics(
                registry != null,
                registry?.CapturedAt ?? default,
                registry?.Reason ?? string.Empty,
                registry?.Rows.Count ?? 0,
                registry?.ManifestDiagnosticCount ?? 0,
                registry?.DependencyErrorCount ?? 0,
                registry?.DependencyWarningCount ?? 0,
                registry?.ApiTooNewCount ?? 0,
                registry?.DiffCount ?? 0,
                mods.Count(mod => mod.EffectiveKind.Equals("Advanced", StringComparison.OrdinalIgnoreCase)),
                mods.Count(mod => mod.AdvancedReferenceVerified),
                mods.Count(mod => mod.NativeRisk.IndexOf("native-code", StringComparison.OrdinalIgnoreCase) >= 0),
                mods.Count(mod => mod.RequiresRestart),
                hooks.Count(hook => hook.IsFailed || hook.IsMissing),
                features.Count(feature => feature.IsFailed || feature.IsDegraded),
                rows);
        }

        internal bool RegistryAvailable { get; }
        internal DateTimeOffset CapturedAt { get; }
        internal string RefreshReason { get; }
        internal int RegistryRowCount { get; }
        internal int ManifestIssueCount { get; }
        internal int DependencyErrorCount { get; }
        internal int DependencyWarningCount { get; }
        internal int ApiTooNewCount { get; }
        internal int DiffCount { get; }
        internal int AdvancedModCount { get; }
        internal int AdvancedReferenceVerifiedCount { get; }
        internal int NativeRiskModCount { get; }
        internal int RestartRequiredModCount { get; }
        internal int FailedOrMissingHookCount { get; }
        internal int FailedOrDegradedFeatureCount { get; }
        internal IReadOnlyList<ManagerAdvancedDiagnosticRow> Rows { get; }
    }

    internal sealed class ManagerAdvancedDiagnosticRow
    {
        private ManagerAdvancedDiagnosticRow(string severity, string category, string owner, string details)
        {
            Severity = severity ?? string.Empty;
            Category = category ?? string.Empty;
            Owner = owner ?? string.Empty;
            Details = details ?? string.Empty;
            SortRank = Severity.Equals("error", StringComparison.OrdinalIgnoreCase)
                ? 0
                : Severity.Equals("warning", StringComparison.OrdinalIgnoreCase) ? 1 : 2;
        }

        internal static ManagerAdvancedDiagnosticRow FromRegistry(ContentManifestRegistryDiagnostic diagnostic)
        {
            return new ManagerAdvancedDiagnosticRow(diagnostic.Severity, diagnostic.Kind, diagnostic.OwnerId, diagnostic.Details);
        }

        internal static ManagerAdvancedDiagnosticRow FromDiff(string details)
        {
            return new ManagerAdvancedDiagnosticRow("info", "legacy-registry-diff", "DTMAPI.ContentRegistry", details);
        }

        internal string Severity { get; }
        internal string Category { get; }
        internal string Owner { get; }
        internal string Details { get; }
        internal int SortRank { get; }
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

    internal sealed class ManagerInstallStateSummary
    {
        private ManagerInstallStateSummary(
            string status,
            string installedVersion,
            string binaryVersion,
            string installedAt,
            string installStatePath,
            int legacyMovedCount,
            int legacyDetectedCount,
            int optionalComponentCount,
            int availableOptionalComponentCount,
            bool uninstallScriptAvailable,
            string errorMessage)
        {
            Status = status ?? string.Empty;
            InstalledVersion = installedVersion ?? string.Empty;
            BinaryVersion = binaryVersion ?? string.Empty;
            InstalledAt = installedAt ?? string.Empty;
            InstallStatePath = installStatePath ?? string.Empty;
            LegacyMovedCount = legacyMovedCount;
            LegacyDetectedCount = legacyDetectedCount;
            OptionalComponentCount = optionalComponentCount;
            AvailableOptionalComponentCount = availableOptionalComponentCount;
            UninstallScriptAvailable = uninstallScriptAvailable;
            ErrorMessage = errorMessage ?? string.Empty;
        }

        internal static ManagerInstallStateSummary Missing(string statePath, string version, bool uninstallScriptAvailable, string errorMessage)
        {
            return new ManagerInstallStateSummary("missing", version, string.Empty, string.Empty, statePath, 0, 0, 0, 0, uninstallScriptAvailable, errorMessage);
        }

        internal static ManagerInstallStateSummary Present(
            string installedVersion,
            string binaryVersion,
            string installedAt,
            string statePath,
            int legacyMovedCount,
            int legacyDetectedCount,
            bool uninstallScriptAvailable)
        {
            return Present(installedVersion, binaryVersion, installedAt, statePath, legacyMovedCount, legacyDetectedCount, 0, 0, uninstallScriptAvailable);
        }

        internal static ManagerInstallStateSummary Present(
            string installedVersion,
            string binaryVersion,
            string installedAt,
            string statePath,
            int legacyMovedCount,
            int legacyDetectedCount,
            int optionalComponentCount,
            int availableOptionalComponentCount,
            bool uninstallScriptAvailable)
        {
            return new ManagerInstallStateSummary("present", installedVersion, binaryVersion, installedAt, statePath, legacyMovedCount, legacyDetectedCount, optionalComponentCount, availableOptionalComponentCount, uninstallScriptAvailable, string.Empty);
        }

        internal static ManagerInstallStateSummary FromRuntimePaths(RuntimePaths paths)
        {
            if (paths == null)
                throw new ArgumentNullException(nameof(paths));

            string statePath = Path.Combine(paths.DtmApiPath, "install-state.json");
            bool uninstallAvailable = File.Exists(Path.Combine(paths.DtmApiPath, "tools", "uninstall-dtmapi.ps1")) ||
                File.Exists(Path.Combine(paths.DtmApiPath, "uninstall-dtmapi.ps1"));
            if (!File.Exists(statePath))
                return Missing(statePath, string.Empty, uninstallAvailable, string.Empty);

            try
            {
                ManagerInstallStateFile state = JsonFile.Read<ManagerInstallStateFile>(statePath);
                return Present(
                    state.DTMAPIVersion,
                    state.BinaryVersion,
                    state.InstalledAt,
                    statePath,
                    state.LegacyModsMoved?.Length ?? 0,
                    state.LegacyDetections?.Length ?? 0,
                    state.OptionalComponents?.Length ?? 0,
                    state.OptionalComponents?.Count(value => value != null && !string.IsNullOrWhiteSpace(value.Path) && File.Exists(value.Path)) ?? 0,
                    uninstallAvailable);
            }
            catch (Exception ex)
            {
                return new ManagerInstallStateSummary("read-error", string.Empty, string.Empty, string.Empty, statePath, 0, 0, 0, 0, uninstallAvailable, ex.GetType().Name + ": " + ex.Message);
            }
        }

        internal string Status { get; }
        internal string InstalledVersion { get; }
        internal string BinaryVersion { get; }
        internal string InstalledAt { get; }
        internal string InstallStatePath { get; }
        internal int LegacyMovedCount { get; }
        internal int LegacyDetectedCount { get; }
        internal int OptionalComponentCount { get; }
        internal int AvailableOptionalComponentCount { get; }
        internal bool UninstallScriptAvailable { get; }
        internal string ErrorMessage { get; }
    }

    [DataContract]
    internal sealed class ManagerInstallStateFile
    {
        [DataMember(Name = "InstalledAt")]
        public string InstalledAt { get; set; } = string.Empty;

        [DataMember(Name = "DTMAPIVersion")]
        public string DTMAPIVersion { get; set; } = string.Empty;

        [DataMember(Name = "BinaryVersion")]
        public string BinaryVersion { get; set; } = string.Empty;

        [DataMember(Name = "LegacyModsMoved")]
        public ManagerInstallStateEntry[] LegacyModsMoved { get; set; } = new ManagerInstallStateEntry[0];

        [DataMember(Name = "LegacyDetections")]
        public ManagerInstallStateEntry[] LegacyDetections { get; set; } = new ManagerInstallStateEntry[0];

        [DataMember(Name = "OptionalComponents")]
        public ManagerOptionalComponentEntry[] OptionalComponents { get; set; } = new ManagerOptionalComponentEntry[0];
    }

    [DataContract]
    internal sealed class ManagerOptionalComponentEntry
    {
        [DataMember(Name = "ComponentId")]
        public string ComponentId { get; set; } = string.Empty;

        [DataMember(Name = "Distribution")]
        public string Distribution { get; set; } = string.Empty;

        [DataMember(Name = "Path")]
        public string Path { get; set; } = string.Empty;
    }

    [DataContract]
    internal sealed class ManagerInstallStateEntry
    {
        [DataMember(Name = "Kind")]
        public string Kind { get; set; } = string.Empty;

        [DataMember(Name = "Path")]
        public string Path { get; set; } = string.Empty;
    }

    internal sealed class ManagerLogsPageState
    {
        private ManagerLogsPageState(
            string latestLogPath,
            string snapshotLatestReportPath,
            string snapshotReportStatus,
            string exportStatus,
            string exportedReportPath,
            string pathMatchStatus,
            bool snapshotReportPathMatched,
            string exportErrorMessage)
        {
            LatestLogPath = latestLogPath;
            SnapshotLatestReportPath = snapshotLatestReportPath;
            SnapshotReportStatus = snapshotReportStatus;
            ExportStatus = exportStatus;
            ExportedReportPath = exportedReportPath;
            PathMatchStatus = pathMatchStatus;
            SnapshotReportPathMatched = snapshotReportPathMatched;
            ExportErrorMessage = exportErrorMessage;
        }

        internal static ManagerLogsPageState From(DtmManagerReportExportResult? lastExport, DtmManagerViewModel? currentModel)
        {
            DtmManagerViewModel? snapshotModel = lastExport?.RefreshedModel ?? currentModel;
            string exportStatus = lastExport?.Status ?? "not-exported";
            string pathMatchStatus = GetPathMatchStatus(lastExport);

            return new ManagerLogsPageState(
                snapshotModel?.LatestLogPath ?? string.Empty,
                snapshotModel?.LatestReportPath ?? string.Empty,
                snapshotModel?.ExportReport.Status ?? "unavailable",
                exportStatus,
                lastExport?.ExportedReportPath ?? string.Empty,
                pathMatchStatus,
                lastExport?.SnapshotReportPathMatched ?? false,
                lastExport?.ErrorMessage ?? string.Empty);
        }

        internal string LatestLogPath { get; }
        internal string SnapshotLatestReportPath { get; }
        internal string SnapshotReportStatus { get; }
        internal string ExportStatus { get; }
        internal string ExportedReportPath { get; }
        internal string PathMatchStatus { get; }
        internal bool SnapshotReportPathMatched { get; }
        internal string ExportErrorMessage { get; }

        private static string GetPathMatchStatus(DtmManagerReportExportResult? lastExport)
        {
            if (lastExport == null)
                return "not-exported";

            if (string.Equals(lastExport.Status, "export-failed", StringComparison.OrdinalIgnoreCase))
                return "export-failed";

            if (string.IsNullOrWhiteSpace(lastExport.ExportedReportPath))
                return "missing-export-path";

            return lastExport.SnapshotReportPathMatched ? "matched" : "report-path-mismatch";
        }
    }
}
