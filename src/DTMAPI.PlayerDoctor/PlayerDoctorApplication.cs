using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using DTMAPI.InstallDoctor;

namespace DTMAPI.PlayerDoctor;

public static class PlayerDoctorApplication
{
    public static int Run(string[] args, TextWriter? standardOutput = null, TextWriter? standardError = null)
    {
        TextWriter output = standardOutput ?? Console.Out;
        TextWriter error = standardError ?? Console.Error;
        try
        {
            if (args.Length == 0 || IsHelp(args[0]))
            {
                output.Write(HelpText);
                return 0;
            }

            if (IsVersion(args[0]))
            {
                if (args.Length != 1)
                    throw new PlayerDoctorUsageException("The version command accepts no options.");
                output.WriteLine("DTMAPI Player Doctor " + ToolVersion + " (read-only)");
                return 0;
            }

            if (!args[0].Equals("inspect", StringComparison.OrdinalIgnoreCase))
                throw new PlayerDoctorUsageException("Unknown command '" + args[0] + "'. Only inspect, help, and version are available.");

            InspectCommand command = InspectCommand.Parse(args.Skip(1).ToArray());
            if (command.Help)
            {
                output.Write(HelpText);
                return 0;
            }

            string root = Path.GetFullPath(command.GameRoot);
            ValidateOutputPaths(command, root);

            string? installedVersion = command.RuntimeVersion;
            IReadOnlyList<DoctorFinding> contextFindings = Array.Empty<DoctorFinding>();
            if (!command.RuntimeModeSpecified)
            {
                InstalledRuntimeVersionProbeResult probe = InstalledRuntimeVersionProbe.Inspect(root);
                installedVersion = probe.InstalledDtmApiVersion;
                contextFindings = probe.Findings;
            }

            DoctorReport report = new DoctorEngine().Inspect(root, new DoctorOptions
            {
                ScanContext = command.ScanContext,
                InstalledDtmApiVersion = installedVersion,
                ContextFindings = contextFindings
            });
            string human = DoctorReportFormatter.ToHuman(report);
            string json = DoctorReportFormatter.ToJson(report);
            string summary = DoctorReportFormatter.ToSummary(report);

            WriteExplicitOutput(command.JsonOutputPath, json);
            WriteExplicitOutput(command.TextOutputPath, human + Environment.NewLine);
            WriteExplicitOutput(command.SummaryOutputPath, summary + Environment.NewLine);
            if (!command.Quiet)
                output.WriteLine(human);

            return report.ErrorCount > 0 ? 2 : 0;
        }
        catch (PlayerDoctorUsageException ex)
        {
            error.WriteLine("ERROR usage: " + ex.Message);
            error.WriteLine("Run dtmapi-player-doctor help for the read-only command contract.");
            return 1;
        }
        catch (Exception ex)
        {
            error.WriteLine("ERROR tool-failure: " + ex.GetType().Name + ": " + ex.Message);
            return 1;
        }
    }

    private static void ValidateOutputPaths(InspectCommand command, string root)
    {
        string[] outputs = new[] { command.JsonOutputPath, command.TextOutputPath, command.SummaryOutputPath }
            .Where(value => value.Length > 0)
            .Select(Path.GetFullPath)
            .ToArray();
        if (outputs.Distinct(StringComparer.OrdinalIgnoreCase).Count() != outputs.Length)
            throw new PlayerDoctorUsageException("Each report output path must be distinct.");

        string[] scannedRoots = command.ScanContext == DoctorScanContext.PackageArtifact
            ? new[] { root }
            : new[]
            {
                Path.Combine(root, "BepInEx", "plugins"),
                Path.Combine(root, "Mods")
            };
        foreach (string output in outputs)
        {
            if (scannedRoots.Any(scannedRoot => IsUnder(output, scannedRoot)))
                throw new PlayerDoctorUsageException("Report outputs must remain outside the read-only scanned tree: " + output);
            string? parent = Path.GetDirectoryName(output);
            if (string.IsNullOrEmpty(parent) || !Directory.Exists(parent))
                throw new PlayerDoctorUsageException("Report output parent does not exist: " + output);
        }
    }

    private static void WriteExplicitOutput(string path, string content)
    {
        if (path.Length == 0)
            return;
        File.WriteAllText(Path.GetFullPath(path), content, new UTF8Encoding(false));
    }

    private static bool IsUnder(string candidate, string root)
    {
        string relative = Path.GetRelativePath(Path.GetFullPath(root), Path.GetFullPath(candidate));
        return !relative.Equals("..", StringComparison.Ordinal) &&
               !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) &&
               !Path.IsPathRooted(relative);
    }

    private static bool IsHelp(string value) => value.Equals("help", StringComparison.OrdinalIgnoreCase) || value is "-h" or "--help";
    private static bool IsVersion(string value) => value.Equals("version", StringComparison.OrdinalIgnoreCase) || value is "-v" or "--version";

    private static string ToolVersion => typeof(PlayerDoctorApplication).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";

    private const string HelpText = """
DTMAPI Player Doctor - read-only player installation diagnostics

Usage:
  dtmapi-player-doctor inspect --game-root PATH [--runtime-version VERSION | --runtime-missing]
      [--scan-context installed-game|package-artifact]
      [--json-output PATH] [--text-output PATH] [--summary-output PATH] [--quiet]
  dtmapi-player-doctor help
  dtmapi-player-doctor version

When no Runtime option is supplied, inspect cross-checks DTMAPI/install-state.json,
DTMAPI/release-manifest.json, and all five installed Runtime DLL FileVersions. It
reports missing or inconsistent sources and never selects one conflicting source.

Exit codes: 0 = completed without errors; 2 = completed with Doctor errors;
1 = usage or tool failure. The tool never loads scanned DLLs and never installs,
moves, deletes, adopts, enables, disables, builds, deploys, repairs, or uploads them.
Output files are written only when their explicit options are present and must stay
outside the scanned plugin/Mods or package-artifact tree.
""";

    private sealed class InspectCommand
    {
        public string GameRoot { get; private set; } = string.Empty;
        public string? RuntimeVersion { get; private set; }
        public bool RuntimeModeSpecified { get; private set; }
        public DoctorScanContext ScanContext { get; private set; } = DoctorScanContext.InstalledGame;
        public string JsonOutputPath { get; private set; } = string.Empty;
        public string TextOutputPath { get; private set; } = string.Empty;
        public string SummaryOutputPath { get; private set; } = string.Empty;
        public bool Quiet { get; private set; }
        public bool Help { get; private set; }

        public static InspectCommand Parse(string[] args)
        {
            var command = new InspectCommand();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < args.Length; index++)
            {
                string option = args[index];
                if (IsHelp(option))
                {
                    command.Help = true;
                    continue;
                }
                if (option.Equals("--quiet", StringComparison.OrdinalIgnoreCase))
                {
                    RequireUnique(seen, option);
                    command.Quiet = true;
                    continue;
                }
                if (option.Equals("--runtime-missing", StringComparison.OrdinalIgnoreCase))
                {
                    RequireUnique(seen, "runtime-mode");
                    command.RuntimeModeSpecified = true;
                    command.RuntimeVersion = string.Empty;
                    continue;
                }

                string value = RequireValue(args, ref index, option);
                switch (option.ToLowerInvariant())
                {
                    case "--game-root":
                        RequireUnique(seen, option);
                        command.GameRoot = value;
                        break;
                    case "--runtime-version":
                        RequireUnique(seen, "runtime-mode");
                        if (string.IsNullOrWhiteSpace(value))
                            throw new PlayerDoctorUsageException("--runtime-version requires a non-empty value; use --runtime-missing when unavailable.");
                        command.RuntimeModeSpecified = true;
                        command.RuntimeVersion = value;
                        break;
                    case "--scan-context":
                        RequireUnique(seen, option);
                        command.ScanContext = value.ToLowerInvariant() switch
                        {
                            "installed-game" => DoctorScanContext.InstalledGame,
                            "package-artifact" => DoctorScanContext.PackageArtifact,
                            _ => throw new PlayerDoctorUsageException("--scan-context must be installed-game or package-artifact.")
                        };
                        break;
                    case "--json-output":
                        RequireUnique(seen, option);
                        command.JsonOutputPath = value;
                        break;
                    case "--text-output":
                        RequireUnique(seen, option);
                        command.TextOutputPath = value;
                        break;
                    case "--summary-output":
                        RequireUnique(seen, option);
                        command.SummaryOutputPath = value;
                        break;
                    default:
                        throw new PlayerDoctorUsageException("Unknown inspect option '" + option + "'.");
                }
            }

            if (!command.Help && string.IsNullOrWhiteSpace(command.GameRoot))
                throw new PlayerDoctorUsageException("inspect requires --game-root PATH.");
            return command;
        }

        private static string RequireValue(string[] args, ref int index, string option)
        {
            if (!option.StartsWith("--", StringComparison.Ordinal))
                throw new PlayerDoctorUsageException("Unexpected argument '" + option + "'.");
            if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
                throw new PlayerDoctorUsageException(option + " requires a value.");
            index++;
            return args[index];
        }

        private static void RequireUnique(ISet<string> seen, string option)
        {
            if (!seen.Add(option))
                throw new PlayerDoctorUsageException("Option may be specified only once: " + option + ".");
        }
    }

    private sealed class PlayerDoctorUsageException : Exception
    {
        public PlayerDoctorUsageException(string message) : base(message)
        {
        }
    }
}
