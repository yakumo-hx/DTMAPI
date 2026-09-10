using DTMAPI.Authoring.Contracts;
using DTMAPI.Internal.Authoring;
using System.Text;

namespace DTMAPI.AuthorSdk;

public static class AuthorApplication
{
    public static async Task<int> RunAsync(string[] args, TextWriter? standardOutput = null, TextWriter? standardError = null)
    {
        TextWriter output = standardOutput ?? Console.Out;
        TextWriter error = standardError ?? Console.Error;
        try
        {
            ParsedCommand command = ParsedCommand.Parse(args);
            if (command.Name is "help" or "-h" or "--help")
            {
                await output.WriteAsync(HelpText).ConfigureAwait(false);
                return 0;
            }

            if (command.Name is "version" or "--version" or "-v")
            {
                await output.WriteLineAsync("DTMAPI Author SDK " + AuthorSdkContract.SdkVersion + " (default API target " + AuthorApiTargetCatalog.Current.DefaultTarget + ")").ConfigureAwait(false);
                return 0;
            }

            if (command.Name == "doctor")
                return await DoctorCommandService.RunAsync(command, output).ConfigureAwait(false);

            if (command.Name is "upload" or "publish")
                return await WriteFailureAsync(command, output, error, 2, "SDK002", "Author SDK 0.1.0 does not create Workshop items, store credentials, or upload.").ConfigureAwait(false);

            if (IsPausedLegacyGameModsMutation(command))
            {
                return await WriteFailureAsync(
                    command,
                    output,
                    error,
                    1,
                    "SDK003",
                    "Legacy <game>/Mods source local select is paused. Install packages into official MODS with deploy/install-local and enable through the game's Mod UI. Historical status, recover, withdraw and source local clear remain available.").ConfigureAwait(false);
            }

            CommandReport report;
            if (command.Name == "session")
                report = await AuthorSessionService.ExecuteAsync(command).ConfigureAwait(false);
            else if (command.Name == "restore")
                report = await LockedPackageRestore.Execute(command).ConfigureAwait(false);
            else
            {
                report = command.Name switch
                {
                    "new" => TemplateCreator.Create(command),
                    "migrate-build" => TemplateCreator.MigrateBuild(command),
                    "validate" => ProjectValidator.ValidateCommand(command),
                    "build" => CodeModBuilder.BuildCommand(command),
                    "pack" => DeterministicPackager.PackCommand(command),
                    "hash" => DeterministicPackager.HashCommand(command),
                    "symbols" => SymbolInspector.Execute(command),
                    "deploy" => DeploymentService.OfficialPackage(command, "deploy"),
                    "update" => DeploymentService.OfficialPackage(command, "update"),
                    "install-local" => DeploymentService.OfficialPackage(command, "install-local"),
                    "install-local-status" => DeploymentService.InstallLocalStatus(command),
                    "withdraw" => DeploymentService.Withdraw(command),
                    "recover" => DeploymentService.Recover(command),
                    "deployment-status" => DeploymentService.Status(command),
                    "source" => SourceStateService.Execute(command),
                    _ => throw new CommandLineException("Unknown command '" + command.Name + "'. Run dtmapi-author help.")
                };
            }

            await WriteReportAsync(command.Json, report, output, error).ConfigureAwait(false);
            return report.Success ? 0 : 1;
        }
        catch (CommandLineException ex)
        {
            var report = Failure("usage", "SDK001", ex.Message);
            await WriteReportAsync(args.Any(a => a.Equals("--json", StringComparison.OrdinalIgnoreCase)), report, output, error).ConfigureAwait(false);
            return 2;
        }
        catch (Exception ex)
        {
            var report = Failure("internal", "SDK999", ex.Message);
            report.Diagnostics[0].Guidance = "No package or destination was adopted. Re-run with --json and preserve the report for diagnosis.";
            await WriteReportAsync(args.Any(a => a.Equals("--json", StringComparison.OrdinalIgnoreCase)), report, output, error).ConfigureAwait(false);
            return 3;
        }
    }

    private static async Task<int> WriteFailureAsync(ParsedCommand command, TextWriter output, TextWriter error, int exitCode, string code, string message)
    {
        await WriteReportAsync(command.Json, Failure(command.Name, code, message), output, error).ConfigureAwait(false);
        return exitCode;
    }

    private static bool IsPausedLegacyGameModsMutation(ParsedCommand command)
    {
        return command.Name == "source" &&
               command.Positionals.Count >= 2 &&
               command.Positionals[0].Equals("local", StringComparison.OrdinalIgnoreCase) &&
               command.Positionals[1].Equals("select", StringComparison.OrdinalIgnoreCase);
    }

    private static CommandReport Failure(string command, string code, string message)
    {
        var report = new CommandReport { Command = command, Success = false };
        report.Diagnostics.Add(new AuthorDiagnostic
        {
            Code = code,
            Severity = DiagnosticSeverity.Error,
            Message = message
        });
        return report;
    }

    private static async Task WriteReportAsync(bool json, CommandReport report, TextWriter output, TextWriter error)
    {
        if (json)
        {
            await output.WriteAsync(JsonSupport.SerializeTool(report)).ConfigureAwait(false);
            return;
        }

        TextWriter destination = report.Success ? output : error;
        string status = report.Success ? "PASS" : "FAIL";
        await destination.WriteLineAsync("[" + status + "] " + report.Command + (report.RootPath.Length > 0 ? " " + report.RootPath : string.Empty)).ConfigureAwait(false);
        foreach (AuthorDiagnostic diagnostic in report.Diagnostics)
        {
            await destination.WriteLineAsync("[" + diagnostic.Severity.ToString().ToUpperInvariant() + "] " + diagnostic.Code + " " + diagnostic.Message).ConfigureAwait(false);
            if (diagnostic.Path.Length > 0)
                await destination.WriteLineAsync("  Path: " + diagnostic.Path).ConfigureAwait(false);
            if (diagnostic.Guidance.Length > 0)
                await destination.WriteLineAsync("  Guidance: " + diagnostic.Guidance).ConfigureAwait(false);
        }
        if (report.OutputPath.Length > 0)
            await destination.WriteLineAsync("Output: " + report.OutputPath).ConfigureAwait(false);
        if (report.Sha256.Length > 0)
            await destination.WriteLineAsync("SHA-256: " + report.Sha256).ConfigureAwait(false);
        foreach (KeyValuePair<string, string> pair in report.Values)
            if (pair.Key is not ("buildInputIdentity" or "referenceIdentities" or "boundAssemblyReferences"))
                await destination.WriteLineAsync(pair.Key + ": " + pair.Value).ConfigureAwait(false);
    }

    private const string HelpText = """
DTMAPI Author SDK 0.7.0 - available API targets from target-catalog.json

Usage:
  dtmapi-author new codemod <directory> --id Author.Mod --name "Mod" --author "Author" [--code-mod-kind Strict|Advanced] [--api-target <version>] [--game-root <installed-game-for-open-Advanced>] [--native-references <paths-separated-by-semicolons>] [--harmony-owner <owner>] [--json]
  dtmapi-author new contentpack <directory> --id Author.Pack --name "Pack" --author "Author" [--api-target <version>] [--json]
  dtmapi-author validate <directory> [--json]
  dtmapi-author migrate-build <directory> [--json]
  dtmapi-author build <directory> [--configuration Debug|Release] [--compatibility-root <directory>] [--game-root <directory-for-Advanced>] [--output <directory>] [--json]
  dtmapi-author pack <directory> [--configuration Debug|Release] [--symbols true|false] [--compatibility-root <directory>] [--game-root <directory-for-Advanced>] [--build-output <directory>] [--output <zip>] [--json]
  dtmapi-author hash <file-or-directory> [--json]
  dtmapi-author deploy <package.zip> --game-root <directory> [--json]
  dtmapi-author update <package.zip> --game-root <directory> [--json]
  dtmapi-author install-local <package.zip> --game-root <directory> --expected-unique-id <UniqueID> --expected-version <version> --expected-package-sha256 <sha256> [--json]
  dtmapi-author install-local-status <UniqueID> --game-root <directory> --expected-version <version> --expected-package-sha256 <sha256> [--json]
  dtmapi-author withdraw <UniqueID> --game-root <directory> [--json]
  dtmapi-author recover <UniqueID> --game-root <directory> [--json]
  dtmapi-author deployment-status <UniqueID> --game-root <directory> [--json]
  dtmapi-author source local select <UniqueID> <game/Mods/package> --game-root <directory> [--json]  (paused)
  dtmapi-author source local clear <UniqueID> --game-root <directory> [--json]
  dtmapi-author source workshop prepare|clear <UniqueID> --game-root <directory> [--json]
  dtmapi-author source reproduction begin --game-root <directory> [--json]
  dtmapi-author source reproduction restore <snapshotId> --game-root <directory> [--json]
  dtmapi-author source status [UniqueID] --game-root <directory> [--json]
  dtmapi-author session prepare --game-root <directory> [--api-target <version>] [--commands true|false] [--json]
  dtmapi-author session snapshot <UniqueID> <selectedRoot> --game-root <directory> [--timeout-seconds <1..60>] [--json]
  dtmapi-author session reload <UniqueID> <selectedRoot> --game-root <directory> [--timeout-seconds <1..60>] [--json]
  dtmapi-author new library <directory> --id <Assembly.Name> [--api-target <version>] [--role shared-contract|private-managed] [--json]
  dtmapi-author restore <project-directory> [--offline true|false] [--json]
    Explicit NuGet lock verification/download; build and pack never access package feeds.
  dtmapi-author session command <UniqueID> <selectedRoot> --game-root <directory> [--command-line <text>] [--timeout-seconds <1..60>] [--json]
    Prepare with --commands true to offer execute-command/1; the Host must accept it. Default command-line is help.
  dtmapi-author session clear --game-root <directory> [--json]
  dtmapi-author doctor <directory> [--json]
  dtmapi-author symbols <DLL> [--pdb <path>] [--json]
  dtmapi-author version

The CLI never uploads, adopts existing packages, starts or polls the game, or writes ordinary Mods under BepInEx/plugins.
""";
}
