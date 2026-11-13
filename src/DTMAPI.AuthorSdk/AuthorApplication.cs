using DTMAPI.Authoring.Contracts;

namespace DTMAPI.AuthorSdk;

public static class AuthorApplication
{
    public static async Task<int> RunAsync(string[] args, TextWriter? standardOutput = null, TextWriter? standardError = null)
    {
        TextWriter output = standardOutput ?? Console.Out;
        TextWriter error = standardError ?? Console.Error;
        try
        {
            if (args.Length > 0 && args[0] is "build" or "restore") return await OptionalTools.BuildAsync(args, output, error);
            ParsedCommand command = ParsedCommand.Parse(args);
            if (command.Name is "help" or "-h" or "--help")
            {
                await output.WriteAsync(HelpText).ConfigureAwait(false);
                return 0;
            }

            if (command.Name is "version" or "--version" or "-v")
            {
                await output.WriteLineAsync("DTMAPI Author SDK " + AuthorSdkContract.SdkVersion + " (optional tools; standard .NET projects)").ConfigureAwait(false);
                return 0;
            }


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

            CommandReport report = command.Name switch
            {
                "new" => OptionalTools.New(command),
                "native-references" => OptionalTools.NativeReferences(command),
                "status" => OptionalTools.Status(command),
                "validate" => OptionalTools.Validate(command),
                "pack" => OptionalTools.Pack(command),
                "hash" => OptionalTools.Hash(command),
                "symbols" => SymbolInspector.Execute(command),
                "audio-prepare" => AudioPrepareCommand.Execute(command),
                "deploy" => OptionalTools.Deploy(command),
                "update" => OptionalTools.Deploy(command),
                "install-local" => OptionalTools.Deploy(command),
                "install-local-status" => OptionalTools.Status(command),
                "withdraw" => OptionalTools.Withdraw(command),
                "recover" => OptionalTools.Recover(command),
                "deployment-status" => OptionalTools.Status(command),
                "source" => SourceStateService.Execute(command),
                _ => throw new CommandLineException("Unknown command '" + command.Name + "'. Run dtmapi-author help.")
            };

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
            report.Diagnostics[0].Guidance = "Inspect the operation error and affected path. Deployment can be retried after correcting the file error; recover applies only when a preserved directory actually exists.";
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
            if (report.Command.StartsWith("msbuild-", StringComparison.Ordinal) && diagnostic.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning)
            {
                string location = diagnostic.Path.Length > 0 ? diagnostic.Path : report.RootPath;
                await destination.WriteLineAsync(location + ": " + diagnostic.Severity.ToString().ToLowerInvariant() + " " + diagnostic.Code + ": " + diagnostic.Message).ConfigureAwait(false);
                continue;
            }
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
DTMAPI 0.7.5 optional author tools
  new codemod|contentpack <directory> --id Author.Mod [--references <API-directory>]
  build|restore <project-or-directory> [standard dotnet arguments]
  native-references <DLL-or-directory> [...] --output <directory>
  validate <package-directory>
  pack <package-directory> --output <zip> [--replace]
  deploy <package-directory-or-zip> --game-root <game> [--clean] [--target <MODS-directory>]
  status|withdraw|recover <UniqueID> --game-root <game> [--replace]
  symbols <dll> [--pdb <pdb>]
  audio-prepare <audio-file> --use effect|music --output <directory>

Build is standard dotnet passthrough. Pack only archives existing files.
Deploy overlays this package without a backup; files absent from it remain, including old DLLs/resources.
Same-name package settings are overwritten. The target's existing root workshop.json is preserved.
--clean clears the old installation after preparing the new package outside it, without an old backup.
Failed clean staging contains the new package to install, not recoverable old settings.
Use the failure's --target path when retrying, even if the old manifest was removed.
Legacy deploy --replace is redundant overlay confirmation, never --clean.
Withdraw/recover explicitly preserve/restore a directory; status labels their last recovery operation.
The game must be closed for deployment; enable installed Mods in its official Mod page.
""";
}
