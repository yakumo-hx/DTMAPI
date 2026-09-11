using DTMAPI.InstallDoctor;
using System.Text.Json;

namespace DTMAPI.AuthorSdk;

internal static class DoctorCommandService
{
    public static async Task<int> RunAsync(ParsedCommand command, TextWriter output)
    {
        command.RequireOnlyOptions();
        if (command.Positionals.Count != 1)
            throw new CommandLineException("doctor requires exactly one directory or ZIP path.");

        string root = Path.GetFullPath(command.Positionals[0]);
        string? staging = null;
        try
        {
            DoctorReport report;
            if (root.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                staging = Path.Combine(Path.GetTempPath(), "DTMAPI-Doctor-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(staging);
                try
                {
                    DeploymentPackage.ExtractZip(root, staging);
                    report = new DoctorEngine().Inspect(staging, new DoctorOptions { ScanContext = DoctorScanContext.PackageArtifact });
                }
                catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
                {
                    report = new DoctorReport { RootPath = root, ScanContext = DoctorScanContext.PackageArtifact,
                        Findings = new[] { new DoctorFinding { Code = "SDK-DOCTOR-ZIP", Severity = DoctorSeverity.Error, Path = root, Message = ex.Message } } };
                }
            }
            else report = new DoctorEngine().Inspect(root);
            string rendered = command.Json ? DoctorReportFormatter.ToJson(report) : DoctorReportFormatter.ToHuman(report);
            // Reports name the archive member, never an already deleted temporary path.
            if (staging != null)
                rendered = command.Json
                    ? rendered.Replace(JsonSerializer.Serialize(staging)[1..^1], JsonSerializer.Serialize(root + "!")[1..^1], StringComparison.Ordinal)
                    : rendered.Replace(staging, root + "!", StringComparison.Ordinal);
            await output.WriteLineAsync(rendered).ConfigureAwait(false);
            return report.ErrorCount == 0 ? 0 : 2;
        }
        finally { if (staging != null && Directory.Exists(staging)) Directory.Delete(staging, recursive: true); }
    }
}
