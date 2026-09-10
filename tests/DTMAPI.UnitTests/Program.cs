using System;
using System.Diagnostics;
using System.IO;

namespace DTMAPI.UnitTests
{
    // Compatibility launcher. Each selected suite owns its build and runs in a
    // separate process; this assembly has no production/project references.
    internal static class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);
                while (directory != null && !File.Exists(Path.Combine(directory.FullName, "PROJECT.md")))
                    directory = directory.Parent;
                string repo = directory?.FullName ?? throw new InvalidOperationException("Could not locate the repository root.");
                string script = Path.Combine(repo, "tools", "scripts", "test-unit.ps1");
                string shell = OperatingSystem.IsWindows()
                    ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe")
                    : "pwsh";
                var start = new ProcessStartInfo(shell) { UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = repo };
                foreach (string argument in new[] { "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", script,
                    "-Configuration", new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name, "-NoBuild" })
                    start.ArgumentList.Add(argument);
                for (int index = 0; index < args.Length; index++)
                {
                    if (args[index] == "--list") start.ArgumentList.Add("-List");
                    else if (args[index] == "--focus" && index + 1 < args.Length)
                    {
                        string focus = args[++index];
                        if (string.IsNullOrWhiteSpace(focus))
                            throw new ArgumentException("Unknown DTMAPI Unit test focus: " + focus + ". Default tests were not run.");
                        start.ArgumentList.Add("-Focus");
                        start.ArgumentList.Add(focus);
                    }
                    else throw new ArgumentException("Unknown DTMAPI Unit test argument: " + args[index]);
                }
                using Process process = Process.Start(start) ?? throw new InvalidOperationException("Could not start the Unit route.");
                process.WaitForExit();
                return process.ExitCode;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception);
                return 1;
            }
        }
    }
}
