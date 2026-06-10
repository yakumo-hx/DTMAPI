using System;
using DTMAPI.Abstractions;

namespace DTMAPI.Core.Manager
{
    internal sealed class DtmManagerRuntimeModelProvider
    {
        private readonly IDtmDiagnosticsApi diagnosticsApi;
        private readonly Func<string> exportReport;

        internal DtmManagerRuntimeModelProvider(IDtmDiagnosticsApi diagnosticsApi, Func<string> exportReport)
        {
            this.diagnosticsApi = diagnosticsApi ?? throw new ArgumentNullException(nameof(diagnosticsApi));
            this.exportReport = exportReport ?? throw new ArgumentNullException(nameof(exportReport));
        }

        internal DtmManagerViewModel GetCurrentModel()
        {
            return DtmManagerViewModelFactory.FromSnapshot(diagnosticsApi.GetSnapshot());
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
        private DtmManagerReportExportResult(string exportedReportPath, DtmManagerViewModel refreshedModel, bool snapshotReportPathMatched, string status)
        {
            ExportedReportPath = exportedReportPath;
            RefreshedModel = refreshedModel;
            SnapshotReportPathMatched = snapshotReportPathMatched;
            Status = status;
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

            return new DtmManagerReportExportResult(normalizedExportedPath, refreshedModel, matched, status);
        }

        internal string ExportedReportPath { get; }
        internal DtmManagerViewModel RefreshedModel { get; }
        internal bool SnapshotReportPathMatched { get; }
        internal string Status { get; }
    }
}
