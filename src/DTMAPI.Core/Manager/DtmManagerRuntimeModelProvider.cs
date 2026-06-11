using System;
using DTMAPI.Abstractions;

namespace DTMAPI.Core.Manager
{
    internal sealed class DtmManagerRuntimeModelProvider
    {
        private readonly IDtmDiagnosticsApi diagnosticsApi;
        private readonly Func<string> exportReport;
        private readonly Func<ManagerInstallStateSummary>? installStateProvider;

        internal DtmManagerRuntimeModelProvider(IDtmDiagnosticsApi diagnosticsApi, Func<string> exportReport, Func<ManagerInstallStateSummary>? installStateProvider = null)
        {
            this.diagnosticsApi = diagnosticsApi ?? throw new ArgumentNullException(nameof(diagnosticsApi));
            this.exportReport = exportReport ?? throw new ArgumentNullException(nameof(exportReport));
            this.installStateProvider = installStateProvider;
        }

        internal DtmManagerViewModel GetCurrentModel()
        {
            ManagerInstallStateSummary? installState = installStateProvider?.Invoke();
            return DtmManagerViewModelFactory.FromSnapshot(diagnosticsApi.GetSnapshot(), installState);
        }

        internal DtmManagerReportExportResult ExportReportAndRefresh()
        {
            string exportedPath = exportReport() ?? string.Empty;
            DtmManagerViewModel refreshedModel = GetCurrentModel();
            return DtmManagerReportExportResult.FromExport(exportedPath, refreshedModel);
        }
    }

    internal sealed class DtmManagerReportExportResult
    {
        private DtmManagerReportExportResult(string exportedReportPath, DtmManagerViewModel? refreshedModel, bool snapshotReportPathMatched, string status, string errorMessage)
        {
            ExportedReportPath = exportedReportPath;
            RefreshedModel = refreshedModel;
            SnapshotReportPathMatched = snapshotReportPathMatched;
            Status = status;
            ErrorMessage = errorMessage;
        }

        internal static DtmManagerReportExportResult FromExport(string exportedReportPath, DtmManagerViewModel refreshedModel)
        {
            if (refreshedModel == null)
                throw new ArgumentNullException(nameof(refreshedModel));

            string normalizedExportedPath = exportedReportPath ?? string.Empty;
            bool matched = !string.IsNullOrWhiteSpace(normalizedExportedPath) &&
                normalizedExportedPath.Equals(refreshedModel.LatestReportPath, StringComparison.OrdinalIgnoreCase);

            string status;
            if (string.IsNullOrWhiteSpace(normalizedExportedPath))
                status = "missing-export-path";
            else if (!matched)
                status = "report-path-mismatch";
            else
                status = "exported";

            return new DtmManagerReportExportResult(normalizedExportedPath, refreshedModel, matched, status, string.Empty);
        }

        internal static DtmManagerReportExportResult FromFailure(Exception exception, DtmManagerViewModel? refreshedModel)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            return new DtmManagerReportExportResult(
                string.Empty,
                refreshedModel,
                false,
                "export-failed",
                exception.GetType().Name + ": " + exception.Message);
        }

        internal string ExportedReportPath { get; }
        internal DtmManagerViewModel? RefreshedModel { get; }
        internal bool SnapshotReportPathMatched { get; }
        internal string Status { get; }
        internal string ErrorMessage { get; }
    }

    internal sealed class DtmManagerCopySummaryResult
    {
        private DtmManagerCopySummaryResult(string status, string text, string errorMessage)
        {
            Status = status;
            Text = text ?? string.Empty;
            ErrorMessage = errorMessage ?? string.Empty;
        }

        internal static DtmManagerCopySummaryResult FromCopied(string text)
        {
            return new DtmManagerCopySummaryResult("copied", text, string.Empty);
        }

        internal static DtmManagerCopySummaryResult FromUnavailable(string text, string errorMessage)
        {
            return new DtmManagerCopySummaryResult("copy-unavailable", text, errorMessage);
        }

        internal string Status { get; }
        internal string Text { get; }
        internal string ErrorMessage { get; }
        internal bool Copied => string.Equals(Status, "copied", StringComparison.OrdinalIgnoreCase);
    }
}
