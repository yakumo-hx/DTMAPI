using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DTMAPI.MultiPlatformInstaller;

internal sealed class InstallerLog : IDisposable
{
    private readonly StreamWriter _writer;
    public string Path { get; }

    public static IEnumerable<string> Roots => new[]
    {
        System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DTMAPI", "InstallerLogs"),
        System.IO.Path.Combine(System.IO.Path.GetTempPath(), "DTMAPI", "InstallerLogs")
    }.Distinct();

    public InstallerLog(string gameDir)
    {
        foreach (string root in Roots)
        {
            try
            {
                Directory.CreateDirectory(root);
                Path = System.IO.Path.Combine(root, "uninstall-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + "-" + Guid.NewGuid().ToString("N")[..8] + ".log");
                _writer = new StreamWriter(new FileStream(Path, FileMode.CreateNew, FileAccess.Write, FileShare.Read), new UTF8Encoding(false)) { AutoFlush = true };
                Write("GameDir=" + gameDir);
                Console.WriteLine("Log=" + Path);
                return;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { Console.WriteLine("Log location unavailable: " + root + " | " + ex.Message); }
        }
        throw new IOException("Cannot create the uninstall log in local application data or the temporary directory. No game files were changed.");
    }

    public void Write(string message) => _writer.WriteLine(DateTime.UtcNow.ToString("O") + " " + message);
    public void Dispose() => _writer.Dispose();
}
