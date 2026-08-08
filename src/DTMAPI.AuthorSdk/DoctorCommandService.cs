using DTMAPI.InstallDoctor;

namespace DTMAPI.AuthorSdk;

internal static class DoctorCommandService
{
    public static async Task<int> RunAsync(ParsedCommand command, TextWriter output)
    {
        command.RequireOnlyOptions();
        if (command.Positionals.Count != 1)
            throw new CommandLineException("doctor requires exactly one directory path.");

        string root = Path.GetFullPath(command.Positionals[0]);
        DoctorReport report = new DoctorEngine().Inspect(root);
        string rendered = command.Json
            ? DoctorReportFormatter.ToJson(report)
            : DoctorReportFormatter.ToHuman(report);
        await output.WriteLineAsync(rendered).ConfigureAwait(false);
        return report.ErrorCount == 0 ? 0 : 2;
    }
}
