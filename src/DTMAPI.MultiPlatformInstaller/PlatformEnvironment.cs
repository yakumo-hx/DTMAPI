using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace DTMAPI.MultiPlatformInstaller;

internal enum HostKind
{
    Windows,
    Linux,
    WineOrCrossOver
}

internal sealed class PlatformEnvironment
{
    public const string RequiredLinuxLaunchOption = "WINEDLLOVERRIDES=\"winhttp=n,b\" %command%";
    public const string CrossOverOverride = "winhttp: Native, then Builtin";

    public HostKind HostKind { get; }
    public string HostLabel => HostKind switch
    {
        HostKind.Linux => "linux-x64",
        HostKind.WineOrCrossOver => "win-x64-wine",
        _ => "win-x64"
    };

    public PlatformEnvironment()
    {
        HostKind = DetectHostKind();
    }

    private static HostKind DetectHostKind()
    {
        if (OperatingSystem.IsLinux())
            return HostKind.Linux;
        if (!OperatingSystem.IsWindows())
            return HostKind.Windows;

        string[] wineVariables = { "WINEPREFIX", "WINELOADERNOEXEC", "CX_BOTTLE", "CX_ROOT", "WINEDATADIR" };
        if (wineVariables.Any(name => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(name))))
            return HostKind.WineOrCrossOver;

        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Wine");
            if (key is not null)
                return HostKind.WineOrCrossOver;
        }
        catch
        {
            // Registry inspection is advisory only.
        }

        return HostKind.Windows;
    }

    public LaunchIntegrationHealth GetLaunchIntegration(out string detail)
    {
        if (HostKind == HostKind.Windows)
        {
            detail = "Native Windows does not require the Proton launch option. / 原生 Windows 不需要 Proton 启动选项。";
            return LaunchIntegrationHealth.NotRequired;
        }

        if (HostKind == HostKind.Linux)
        {
            string? current = Environment.GetEnvironmentVariable("WINEDLLOVERRIDES");
            if (!string.IsNullOrWhiteSpace(current) && current.Contains("winhttp=n,b", StringComparison.OrdinalIgnoreCase))
            {
                detail = "The current process exposes WINEDLLOVERRIDES=winhttp=n,b, but this does not prove the Steam launch option was saved. Steam persistence remains unverified. / 当前进程含正确变量，但这不能证明 Steam 启动项已保存，仍为未验证。";
                return LaunchIntegrationHealth.Unverified;
            }

            detail = "Steam launch options are not modified or read. Paste exactly: " + RequiredLinuxLaunchOption;
            return LaunchIntegrationHealth.Unverified;
        }

        string value = OperatingSystem.IsWindows() ? ReadWineWinHttpOverride() : string.Empty;
        if (value.Contains("native", StringComparison.OrdinalIgnoreCase) &&
            value.Contains("builtin", StringComparison.OrdinalIgnoreCase))
        {
            detail = "CrossOver/Wine Bottle override detected: winhttp=" + value;
            return LaunchIntegrationHealth.Configured;
        }
        if (!string.IsNullOrWhiteSpace(value))
        {
            detail = "CrossOver/Wine has winhttp=" + value + "; set it to Native, then Builtin in this game Bottle.";
            return LaunchIntegrationHealth.Missing;
        }

        detail = "CrossOver Bottle setting was not verified. In the existing Steam/game Bottle open Wine Configuration > Libraries, add winhttp, and select Native, then Builtin.";
        return LaunchIntegrationHealth.Unverified;
    }

    [SupportedOSPlatform("windows")]
    private static string ReadWineWinHttpOverride()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Wine\DllOverrides");
            return Convert.ToString(key?.GetValue("winhttp")) ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    public string ResolveDefaultOutputParent()
    {
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        if (!string.IsNullOrWhiteSpace(desktop) && Directory.Exists(desktop))
            return desktop;
        if (!string.IsNullOrWhiteSpace(home) && Directory.Exists(home))
            return home;
        return Environment.CurrentDirectory;
    }

    public string? DiscoverGameDirectory(string packageRoot, string? explicitPath)
    {
        if (!string.IsNullOrWhiteSpace(explicitPath))
            return ValidateGameDirectory(explicitPath, "--game-path");

        string? environmentPath = Environment.GetEnvironmentVariable("DTMAPI_GAME_DIR");
        if (!string.IsNullOrWhiteSpace(environmentPath))
            return ValidateGameDirectory(environmentPath, "DTMAPI_GAME_DIR");

        string settingsPath = Path.Combine(packageRoot, "dtmapi-installer.settings.json");
        if (File.Exists(settingsPath))
        {
            InstallerSettings? settings = JsonStore.Read<InstallerSettings>(settingsPath, InstallerJsonContext.Default.InstallerSettings);
            if (settings is not null && !string.IsNullOrWhiteSpace(settings.GamePath))
                return ValidateGameDirectory(settings.GamePath, "dtmapi-installer.settings.json");
        }

        if (File.Exists(Path.Combine(packageRoot, "DolocTown.exe")))
            return ValidateGameDirectory(packageRoot, "package-colocated game");

        DirectoryInfo? current = new(packageRoot);
        while (current is not null)
        {
            if (string.Equals(current.Name, "steamapps", StringComparison.OrdinalIgnoreCase))
            {
                string manifestPath = Path.Combine(current.FullName, "appmanifest_2285550.acf");
                if (File.Exists(manifestPath))
                {
                    string text = File.ReadAllText(manifestPath);
                    Match match = Regex.Match(text, "\\\"installdir\\\"\\s+\\\"(?<name>[^\\\"]+)\\\"", RegexOptions.CultureInvariant);
                    if (match.Success)
                    {
                        string candidate = Path.Combine(current.FullName, "common", match.Groups["name"].Value);
                        if (File.Exists(Path.Combine(candidate, "DolocTown.exe")))
                            return ValidateGameDirectory(candidate, "current Workshop library appmanifest");
                    }
                }
                break;
            }
            current = current.Parent;
        }

        return null;
    }

    public string ValidateGameDirectory(string path, string source)
    {
        string root = FileSystemSafety.NormalizeRoot(path);
        if (!Directory.Exists(root) || !File.Exists(Path.Combine(root, "DolocTown.exe")))
            throw new InvalidDataException($"{source} is not a complete Doloc Town directory: {root}");
        FileSystemSafety.AssertNoLinksOnExistingPath(root, root);
        return root;
    }

    public bool IsGameRunning(string gameDir, out string detail)
    {
        string expected = Path.GetFullPath(Path.Combine(gameDir, "DolocTown.exe"));
        foreach (Process process in Process.GetProcesses())
        {
            using (process)
            {
                string name;
                try { name = process.ProcessName; }
                catch { continue; }
                if (!name.Contains("DolocTown", StringComparison.OrdinalIgnoreCase))
                    continue;

                try
                {
                    string? executable = process.MainModule?.FileName;
                    if (!string.IsNullOrWhiteSpace(executable) &&
                        string.Equals(Path.GetFullPath(executable), expected, FileSystemSafety.PathComparison))
                    {
                        detail = $"PID={process.Id}; Path={executable}";
                        return true;
                    }
                }
                catch
                {
                    detail = $"PID={process.Id}; a DolocTown process exists but its executable path could not be inspected.";
                    return true;
                }
            }
        }

        if (OperatingSystem.IsLinux() && Directory.Exists("/proc"))
        {
            foreach (string processDir in Directory.EnumerateDirectories("/proc"))
            {
                try
                {
                    if (!int.TryParse(Path.GetFileName(processDir), out _))
                        continue;
                    string cmdline = Path.Combine(processDir, "cmdline");
                    if (!File.Exists(cmdline))
                        continue;
                    string value = File.ReadAllText(cmdline).Replace('\0', ' ');
                    if (value.Contains(expected, StringComparison.Ordinal))
                    {
                        detail = cmdline + ": " + value;
                        return true;
                    }
                }
                catch
                {
                    // Processes can exit while /proc is inspected.
                }
            }
        }

        detail = "No matching DolocTown.exe process was found.";
        return false;
    }

    public IDisposable AcquireMutationLock(string gameDir)
    {
        string stateRoot = FileSystemSafety.CombineUnder(gameDir, "DTMAPI");
        FileSystemSafety.EnsureDirectoryUnder(gameDir, stateRoot);
        string fileLockPath = FileSystemSafety.CombineUnder(gameDir, "DTMAPI/.multiplatform-installer.lock");
        FileSystemSafety.AssertNoLinksOnExistingPath(gameDir, fileLockPath);
        FileStream fileLock;
        try
        {
            fileLock = new FileStream(fileLockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        catch (IOException ex)
        {
            throw new InstallerException(InstallerCodes.TransactionBusy, 3,
                "同一游戏目录已有另一个多平台安装操作。请等待结束后重试。",
                "Another multi-platform installer action is already running for this game directory.", ex.Message);
        }
        try
        {
            // Recheck after opening to close the ordinary check/open race. The
            // lock stream is disposed before any linked entry is reported.
            FileSystemSafety.AssertNoLinksOnExistingPath(gameDir, fileLockPath);
        }
        catch
        {
            fileLock.Dispose();
            throw;
        }

        Mutex? mutex = null;
        bool mutexOwned = false;
        try
        {
            if (OperatingSystem.IsWindows())
            {
                string identity = Path.GetFullPath(gameDir).TrimEnd('\\', '/').ToUpperInvariant();
                string digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity))).ToLowerInvariant();
                mutex = new Mutex(false, @"Local\DTMAPI.RuntimeInstaller." + digest);
                try { mutexOwned = mutex.WaitOne(0); }
                catch (AbandonedMutexException) { mutexOwned = true; }
                if (!mutexOwned)
                    throw new InstallerException(InstallerCodes.TransactionBusy, 3,
                        "同一游戏目录已有另一个 DTMAPI 安装或卸载操作。",
                        "Another DTMAPI install or uninstall is already running for this game directory.");
            }
            return new CombinedMutationLock(fileLock, mutex, mutexOwned);
        }
        catch
        {
            fileLock.Dispose();
            mutex?.Dispose();
            throw;
        }
    }

    private sealed class CombinedMutationLock : IDisposable
    {
        private FileStream? _fileLock;
        private Mutex? _mutex;
        private bool _mutexOwned;

        public CombinedMutationLock(FileStream fileLock, Mutex? mutex, bool mutexOwned)
        {
            _fileLock = fileLock;
            _mutex = mutex;
            _mutexOwned = mutexOwned;
        }

        public void Dispose()
        {
            if (_mutexOwned && _mutex is not null)
            {
                try { _mutex.ReleaseMutex(); }
                catch { }
            }
            _mutexOwned = false;
            _mutex?.Dispose();
            _mutex = null;
            _fileLock?.Dispose();
            _fileLock = null;
        }
    }
}

internal sealed class InstallerException : Exception
{
    public string Code { get; }
    public int ExitCode { get; }
    public string Chinese { get; }
    public string English { get; }
    public string Detail { get; }

    public InstallerException(string code, int exitCode, string chinese, string english, string detail = "", Exception? inner = null)
        : base(english, inner)
    {
        Code = code;
        ExitCode = exitCode;
        Chinese = chinese;
        English = english;
        Detail = detail;
    }
}
