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
