using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DTMAPI.InstallDoctor;

public static class DoctorReportFormatter
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    public static string ToJson(DoctorReport report) => JsonSerializer.Serialize(report, JsonOptions);

    public static string ToHuman(DoctorReport report)
    {
        var output = new StringBuilder();
        output.AppendLine("DTMAPI Install Doctor (read-only)");
        output.AppendLine("Root: " + report.RootPath);
        output.AppendLine("Scan context: " + report.ScanContext);
        if (report.RuntimeVersionCheckRequested)
            output.AppendLine("Installed DTMAPI Runtime: " + (report.InstalledDtmApiVersion.Length == 0 ? "missing/unavailable" : report.InstalledDtmApiVersion));
        output.AppendLine("Artifacts: " + report.Artifacts.Count + "; errors: " + report.ErrorCount + "; warnings: " + report.WarningCount);
        if (report.ReadOnlyVerificationRequested)
        {
            output.AppendLine("Tree hash unchanged: " + (report.TreeUnchanged == true ? "yes" : report.TreeUnchanged == false ? "NO" : "unknown"));
            if (report.InitialTree != null)
                output.AppendLine("Tree SHA-256: " + report.InitialTree.Sha256 + " (" + report.InitialTree.FileCount + " files)");
        }

        foreach (DoctorArtifact artifact in report.Artifacts)
        {
            output.AppendLine();
            output.Append("[" + artifact.Kind + "/" + artifact.Placement + "] " + artifact.Path);
            if (artifact.UniqueId.Length > 0)
                output.Append(" (" + artifact.UniqueId + " " + artifact.Version + ")");
            output.AppendLine();
            if (artifact.ManagedIdentity != DoctorManagedIdentity.Unknown)
                output.AppendLine("  Managed identity: " + artifact.ManagedIdentity);
            if (artifact.ManagedIdentity is DoctorManagedIdentity.StrictCodeMod or DoctorManagedIdentity.LegacyNativeCodeMod or DoctorManagedIdentity.AdvancedCodeMod)
            {
                output.AppendLine("  Declaration: declared=" + (artifact.DeclaredCodeModKind.Length == 0 ? "omitted" : artifact.DeclaredCodeModKind) +
                                  "; effective=" + artifact.EffectiveCodeModKind +
                                  "; status=" + artifact.DeclarationStatus);
            }
            if (artifact.ProvenanceStatus != DoctorProvenanceStatus.NotApplicable)
            {
                output.AppendLine("  Provenance: " + artifact.ProvenanceStatus +
                                  (artifact.ProvenanceReceiptPath.Length == 0 ? string.Empty : " (" + artifact.ProvenanceReceiptPath + ")"));
            }
            if (artifact.NativeRisk != DoctorNativeRisk.NotApplicable)
                output.AppendLine("  Native risk: " + artifact.NativeRisk);
            if (artifact.ReferenceCompatibility != DoctorCompatibilityStatus.NotApplicable ||
                artifact.GameCompatibility != DoctorCompatibilityStatus.NotApplicable)
            {
                output.AppendLine("  Compatibility: references=" + artifact.ReferenceCompatibility +
                                  "; game=" + artifact.GameCompatibility +
                                  (artifact.GameBuildId.Length == 0 ? string.Empty : "; build=" + artifact.GameBuildId));
            }
            if (artifact.ReferencePolicyId.Length > 0)
            {
                output.AppendLine("  Reference policy: " + artifact.ReferencePolicyId +
                                  " v" + artifact.ReferencePolicyVersion +
                                  "; refs=" + artifact.ReferenceCount +
                                  "; sha256=" + artifact.ReferencePolicySha256);
            }
            if (artifact.ExpectedHarmonyOwner.Length > 0)
                output.AppendLine("  Harmony owner: " + artifact.ExpectedHarmonyOwner);
            if (artifact.RestartPolicy != DoctorRestartPolicy.NotApplicable)
                output.AppendLine("  Restart policy: " + artifact.RestartPolicy);
            if (artifact.ManagedIdentity == DoctorManagedIdentity.LegacyNativeCodeMod)
                output.AppendLine("  Ownership: third-party author manages native hooks, static state, save side effects, and cleanup; DTMAPI does not claim hot unload.");
            if (artifact.MinimumDtmApiVersion.Length > 0)
                output.AppendLine("  Minimum DTMAPI: " + artifact.MinimumDtmApiVersion + " (" + artifact.MinimumVersionStatus + ")");
            AppendFindings(output, artifact.Findings);
        }
        if (report.Findings.Count > 0)
        {
            output.AppendLine();
            output.AppendLine("Scan findings:");
            AppendFindings(output, report.Findings);
        }
        return output.ToString().TrimEnd();
    }

    public static string ToSummary(DoctorReport report)
    {
        string status = report.ErrorCount > 0 ? "error" : report.WarningCount > 0 ? "warning" : "ok";
        string runtime = !report.RuntimeVersionCheckRequested
            ? "not-checked"
            : report.InstalledDtmApiVersion.Length == 0
                ? "missing"
                : SanitizeSummaryValue(report.InstalledDtmApiVersion);
        return "status=" + status +
               ";artifacts=" + report.Artifacts.Count +
               ";errors=" + report.ErrorCount +
               ";warnings=" + report.WarningCount +
               ";misplaced=" + report.MisplacedCount +
               ";minimumBlocked=" + report.MinimumBlockedCount +
               ";runtime=" + runtime;
    }

    private static void AppendFindings(StringBuilder output, IEnumerable<DoctorFinding> findings)
    {
        foreach (DoctorFinding finding in findings)
        {
            output.AppendLine("  - " + finding.Severity.ToString().ToUpperInvariant() + " " + finding.Code + ": " + finding.Message);
            if (finding.Guidance.Length > 0)
                output.AppendLine("    Guidance: " + finding.Guidance);
        }
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private static string SanitizeSummaryValue(string value)
    {
        string sanitized = value.Replace(';', '_').Replace('\r', '_').Replace('\n', '_').Trim();
        return sanitized.Length <= 64 ? sanitized : sanitized.Substring(0, 64);
    }
}
