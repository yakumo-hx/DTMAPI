using System;
using System.IO;
using System.Linq;
using DTMAPI.Abstractions;

namespace DTMAPI.GameBridge.DolocTown.QA
{
    internal sealed class DiagnosticsSnapshotProbe
    {
        private readonly GameBridgeFixtureAccess access;
        private readonly string scenario;
        private readonly string[] expectedFeatureIds;

        internal DiagnosticsSnapshotProbe(GameBridgeFixtureAccess access, string scenario, string[] expectedFeatureIds)
        {
            this.access = access ?? throw new ArgumentNullException(nameof(access));
            this.scenario = string.IsNullOrWhiteSpace(scenario) ? "G3" : scenario;
            this.expectedFeatureIds = expectedFeatureIds ?? Array.Empty<string>();
        }

        internal string Run()
        {
            string reportPath = access.ExportDiagnostics();
            IDtmDiagnosticsSnapshot snapshot = access.GetDiagnosticsSnapshot();
            string[] missingFeatures = expectedFeatureIds
                .Where(id => !snapshot.FeatureStatuses.Any(status =>
                    status.FeatureId.Equals(id, StringComparison.OrdinalIgnoreCase) &&
                    status.Status.Equals("ready", StringComparison.OrdinalIgnoreCase)))
                .ToArray();
            string[] missingHooks = expectedFeatureIds
                .Where(id => !snapshot.HookStatuses.Any(status =>
                    status.HookId.Equals("Feature." + id, StringComparison.OrdinalIgnoreCase) &&
                    status.Status.Equals("ready", StringComparison.OrdinalIgnoreCase)))
                .ToArray();
            if (missingFeatures.Length > 0 || missingHooks.Length > 0)
                throw new InvalidOperationException("Missing feature diagnostics. statuses=" + string.Join(",", missingFeatures) + "; hooks=" + string.Join(",", missingHooks) + ".");
            if (string.IsNullOrWhiteSpace(snapshot.LatestLogPath) || !File.Exists(snapshot.LatestLogPath))
                throw new InvalidOperationException("LatestLogPath is missing: " + snapshot.LatestLogPath);
            if (string.IsNullOrWhiteSpace(snapshot.LatestReportPath) || !File.Exists(snapshot.LatestReportPath))
                throw new InvalidOperationException("LatestReportPath is missing: " + snapshot.LatestReportPath);
            if (!snapshot.LatestReportPath.Equals(reportPath, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Exported report path and diagnostics snapshot disagree.");
            if (snapshot.Mods.Count == 0)
                throw new InvalidOperationException("Diagnostic mod status rows are missing.");
            if (snapshot.Mods.Any(status => string.IsNullOrWhiteSpace(status.StatusCode)))
                throw new InvalidOperationException("At least one diagnostic mod row has no StatusCode.");
            if (snapshot.LoadedMods.Any(loaded => !snapshot.Mods.Any(status =>
                status.UniqueID.Equals(loaded.UniqueID, StringComparison.OrdinalIgnoreCase) &&
                status.Loaded && status.StatusCode.Equals("loaded", StringComparison.OrdinalIgnoreCase))))
            {
                throw new InvalidOperationException("At least one loaded mod has no matching loaded diagnostic row.");
            }

            return "scenario=" + scenario + ", expectedFeatures=" + string.Join(",", expectedFeatureIds) +
                "; loadedMods=" + snapshot.LoadedMods.Count +
                "; mods=" + snapshot.Mods.Count +
                "; errors=" + snapshot.Errors.Count +
                "; warnings=" + snapshot.Warnings.Count +
                "; hooks=" + snapshot.HookStatuses.Count +
                "; features=" + snapshot.FeatureStatuses.Count +
                "; latestLog=" + snapshot.LatestLogPath +
                "; latestReport=" + snapshot.LatestReportPath +
                "; owner=qa; fallback=false";
        }
    }
}
