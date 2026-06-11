using System;
using System.Globalization;

namespace DTMAPI.Core.Manager
{
    internal static class ManagerPageRowFormatter
    {
        internal static string ModelUnavailable(string refreshError)
        {
            return string.IsNullOrWhiteSpace(refreshError)
                ? "Manager model unavailable."
                : "Manager model refresh failed: " + refreshError;
        }

        internal static string FormatModRow(ManagerModRow mod)
        {
            if (mod == null)
                throw new ArgumentNullException(nameof(mod));

            string status = FirstText(mod.StatusCode, mod.Status, "unknown");
            string line = status +
                " / " + FirstText(mod.Status, "unknown") +
                " | " + FirstText(mod.Name, "(unnamed)") +
                " | " + FirstText(mod.UniqueID, "(no id)") +
                " | " + FirstText(mod.Version, "(no version)") +
                " | source=" + FirstText(mod.Source, "(unknown)") +
                " | loaded=" + (mod.Loaded ? "true" : "false");

            if ((mod.IsBlocked || mod.IsWarning || mod.IsDisabled) && !string.IsNullOrWhiteSpace(mod.Reason))
                line += " | reason=" + mod.Reason;

            return line;
        }

        internal static string FormatDiagnosticRow(ManagerDiagnosticRow diagnostic)
        {
            if (diagnostic == null)
                throw new ArgumentNullException(nameof(diagnostic));

            return diagnostic.Time.ToString("HH:mm:ss", CultureInfo.InvariantCulture) +
                " [" + FirstText(diagnostic.Owner, "DTMAPI") + "] " +
                FirstText(diagnostic.Message, "(no message)");
        }

        internal static string FormatHookRow(ManagerHookRow hook)
        {
            if (hook == null)
                throw new ArgumentNullException(nameof(hook));

            string line = FirstText(hook.HookId, "(no hook)") +
                ": " + FirstText(hook.Status, "unknown") +
                " | " + FirstText(hook.Target, "(no target)");
            if ((hook.IsFailed || hook.IsMissing) && !string.IsNullOrWhiteSpace(hook.Details))
                line += " | " + hook.Details;

            return line;
        }

        internal static string FormatFeatureRow(ManagerFeatureRow feature)
        {
            if (feature == null)
                throw new ArgumentNullException(nameof(feature));

            string finalDetail = !string.IsNullOrWhiteSpace(feature.LastError)
                ? feature.LastError
                : feature.Details;
            string line = FirstText(feature.FeatureId, "(no feature)") +
                ": " + FirstText(feature.Status, "unknown") +
                " | operation=" + FirstText(feature.LastOperation, "(none)") +
                " | success=" + (feature.Success ? "true" : "false") +
                " | failures=" + feature.FailureCount.ToString(CultureInfo.InvariantCulture);
            if (!string.IsNullOrWhiteSpace(finalDetail))
                line += " | " + finalDetail;

            return line;
        }

        internal static string FormatStatusSummary(DtmManagerViewModel? manager, string refreshError, DtmManagerReportExportResult? lastExport)
        {
            if (manager == null)
                return ModelUnavailable(refreshError);

            ManagerReportExportStatus report = manager.ExportReport;
            string refresh = string.IsNullOrWhiteSpace(refreshError)
                ? "refreshed"
                : "refresh failed: " + refreshError;
            string exportStatus = lastExport?.Status ?? report.Status;

            return "overall=" + FirstText(manager.Summary.OverallStatus, "unknown") +
                "; mods=loaded:" + manager.Summary.LoadedModCount.ToString(CultureInfo.InvariantCulture) +
                    ",blocked:" + manager.Summary.BlockedModCount.ToString(CultureInfo.InvariantCulture) +
                    ",disabled:" + manager.Summary.DisabledModCount.ToString(CultureInfo.InvariantCulture) +
                "; diagnostics=errors:" + manager.Summary.ErrorCount.ToString(CultureInfo.InvariantCulture) +
                    ",warnings:" + manager.Summary.WarningCount.ToString(CultureInfo.InvariantCulture) +
                "; hooks=failed:" + manager.Summary.FailedHookCount.ToString(CultureInfo.InvariantCulture) +
                    ",missing:" + manager.Summary.MissingHookCount.ToString(CultureInfo.InvariantCulture) +
                "; features=failed:" + manager.Summary.FailedFeatureCount.ToString(CultureInfo.InvariantCulture) +
                    ",degraded:" + manager.Summary.DegradedFeatureCount.ToString(CultureInfo.InvariantCulture) +
                "; install=" + FormatInstallState(manager.InstallState) +
                "; refresh=" + refresh +
                "; report=" + exportStatus +
                "; log=" + FormatPathState(report.HasLatestLogPath, report.LatestLogExists, manager.LatestLogPath) +
                "; reportPath=" + FormatPathState(report.HasLatestReportPath, report.LatestReportExists, manager.LatestReportPath);
        }

        internal static string FormatInstallState(ManagerInstallStateSummary installState)
        {
            if (installState == null)
                throw new ArgumentNullException(nameof(installState));

            string version = FirstText(installState.InstalledVersion, "(no version)");
            string uninstall = installState.UninstallScriptAvailable ? "uninstall:available" : "uninstall:missing";
            string line = installState.Status +
                "|version:" + version +
                "|legacyMoved:" + installState.LegacyMovedCount.ToString(CultureInfo.InvariantCulture) +
                "|legacyDetected:" + installState.LegacyDetectedCount.ToString(CultureInfo.InvariantCulture) +
                "|" + uninstall;
            if (!string.IsNullOrWhiteSpace(installState.ErrorMessage))
                line += "|error:" + installState.ErrorMessage;

            return line;
        }

        internal static string FormatLogsExportState(ManagerLogsPageState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            string line = "export=" + state.ExportStatus +
                "; pathMatch=" + state.PathMatchStatus +
                "; snapshotReport=" + state.SnapshotReportStatus;
            if (!string.IsNullOrWhiteSpace(state.ExportErrorMessage))
                line += "; error=" + state.ExportErrorMessage;

            return line;
        }

        internal static string FormatShowingFirst(string label, int shown, int total)
        {
            if (shown < 0)
                shown = 0;
            if (total < 0)
                total = 0;
            if (shown > total)
                shown = total;

            return FirstText(label, "Rows") +
                ": showing first " + shown.ToString(CultureInfo.InvariantCulture) +
                " of " + total.ToString(CultureInfo.InvariantCulture);
        }

        private static string FormatPathState(bool hasPath, bool exists, string path)
        {
            if (!hasPath)
                return "unavailable";

            return (exists ? "present" : "missing") + "|" + FirstText(path, "(empty)");
        }

        private static string FirstText(params string[] values)
        {
            foreach (string value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }

            return string.Empty;
        }
    }
}
