using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DTMAPI.MultiPlatformInstaller;

internal static class CollectLogsAction
{
    public static InstallerResult Run(
        PackageLayout package,
        string gameDir,
        PlatformEnvironment platform,
        string? outputParent)
    {
        string parent = string.IsNullOrWhiteSpace(outputParent)
            ? platform.ResolveDefaultOutputParent()
            : Path.GetFullPath(outputParent);
        Directory.CreateDirectory(parent);
        string token = Guid.NewGuid().ToString("N");
        string name = "DTMAPI-logs-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + token[..8];
        string staging = Path.Combine(parent, "." + name + ".partial-" + token);
        string final = Path.Combine(parent, name);
        if (Directory.Exists(staging) || Directory.Exists(final))
            throw new IOException("Log output collision: " + final);

        Directory.CreateDirectory(staging);
        LogCollectionManifest manifest = new()
        {
            CreatedAtUtc = DateTime.UtcNow.ToString("O"),
            GameDir = gameDir,
            PrivacyNotice = "Logs may contain local usernames and file paths. Review this folder before sharing. / 日志可能包含本机用户名和路径，发送前请检查。"
        };

        try
        {
            List<(string Source, string Group, string SafetyRoot)> sources = DiscoverSources(gameDir, platform, manifest);
            int ordinal = 0;
            foreach ((string source, string group, string safetyRoot) in sources)
            {
                string destinationRelative = Path.Combine(group,
                    ordinal.ToString("D2") + "-" + SanitizeFileName(Path.GetFileName(source)));
                string destination = Path.Combine(staging, destinationRelative);
                try
                {
                    AssertSafeSource(safetyRoot, source);
                    FileSystemSafety.CopyStableFile(source, destination);
                    FileInfo info = new(destination);
                    manifest.Files.Add(new CollectedLogReceipt
                    {
                        Source = source,
                        Destination = destinationRelative.Replace('\\', '/'),
                        Length = info.Length,
                        Sha256 = FileSystemSafety.Sha256(destination)
                    });
                    ordinal++;
                }
                catch (Exception ex)
                {
                    manifest.MissingOrSkipped.Add(source + ": " + ex.Message);
                }
            }

            StatusReport status = StatusAction.Evaluate(package, gameDir, platform);
            File.WriteAllText(Path.Combine(staging, "status.txt"), StatusAction.Format(status), new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(staging, "environment.txt"), BuildEnvironmentText(platform), new UTF8Encoding(false));
            JsonStore.WriteAtomic(Path.Combine(staging, "collection-manifest.json"), manifest,
                InstallerJsonContext.Default.LogCollectionManifest,
                value => value is not null && value.SchemaVersion == 1 && value.GameDir == gameDir);

            Directory.Move(staging, final);
        }
        catch
        {
            try
            {
                if (Directory.Exists(staging) &&
                    string.Equals(Path.GetDirectoryName(Path.GetFullPath(staging)), Path.GetFullPath(parent).TrimEnd('\\', '/'),
                        FileSystemSafety.PathComparison) &&
                    Path.GetFileName(staging).StartsWith("." + name + ".partial-", StringComparison.Ordinal))
                    Directory.Delete(staging, true);
            }
            catch
            {
                // Preserve an ambiguous failed staging root rather than deleting broadly.
            }
            throw;
        }

        return InstallerResult.Success(InstallerCodes.LogsCollected,
            "日志收集完成。发送前请检查其中的用户名和本机路径。",
            "Log collection completed. Review usernames and local paths before sharing.",
            "Output=" + final,
            "Collected=" + manifest.Files.Count,
            "MissingOrSkipped=" + manifest.MissingOrSkipped.Count,
            manifest.PrivacyNotice);
    }

    private static List<(string Source, string Group, string SafetyRoot)> DiscoverSources(
        string gameDir,
        PlatformEnvironment platform,
        LogCollectionManifest manifest)
    {
        List<(string Source, string Group, string SafetyRoot)> sources = new();
        string logsRoot = FileSystemSafety.CombineUnder(gameDir, "DTMAPI/logs");
        if (Directory.Exists(logsRoot))
        {
            try
            {
                FileSystemSafety.AssertNoLinksOnExistingPath(gameDir, logsRoot);
                List<(string Source, string Group, string SafetyRoot)> logFiles = new();
                foreach (string path in Directory.EnumerateFiles(logsRoot, "*.log", SearchOption.TopDirectoryOnly))
                {
                    FileSystemSafety.AssertNoLinksOnExistingPath(gameDir, path);
                    logFiles.Add((path, "DTMAPI", gameDir));
                }
                sources.AddRange(logFiles
                    .Select(item => (Info: new FileInfo(item.Source), item.Group, item.SafetyRoot))
                    .OrderByDescending(item => item.Info.LastWriteTimeUtc)
                    .ThenBy(item => item.Info.FullName, StringComparer.Ordinal)
                    .Take(10)
                    .Select(item => (item.Info.FullName, item.Group, item.SafetyRoot)));
            }
            catch (Exception ex)
            {
                manifest.MissingOrSkipped.Add(logsRoot + " was not followed: " + ex.Message);
            }
        }
        else
        {
            manifest.MissingOrSkipped.Add("DTMAPI/logs directory is missing.");
        }

        AddIfPresent(sources, manifest, FileSystemSafety.CombineUnder(gameDir, "BepInEx/LogOutput.log"), "BepInEx", gameDir);
        AddIfPresent(sources, manifest, FileSystemSafety.CombineUnder(gameDir, "DTMAPI/install-state.json"), "State", gameDir);
        AddIfPresent(sources, manifest, FileSystemSafety.CombineUnder(gameDir, "DTMAPI/release-manifest.json"), "State", gameDir);

        string stateRoot = FileSystemSafety.CombineUnder(gameDir, "DTMAPI");
        if (Directory.Exists(stateRoot))
        {
            try
            {
                FileSystemSafety.AssertNoLinksOnExistingPath(gameDir, stateRoot);
                foreach (string state in Directory.EnumerateFiles(stateRoot, "install-state.failed-*.json", SearchOption.TopDirectoryOnly)
                             .Concat(Directory.EnumerateFiles(stateRoot, "uninstall-state-*.json", SearchOption.TopDirectoryOnly))
                             .Select(path => new FileInfo(path))
                             .OrderByDescending(info => info.LastWriteTimeUtc)
                             .Take(6)
                             .Select(info => info.FullName))
                {
                    FileSystemSafety.AssertNoLinksOnExistingPath(gameDir, state);
                    sources.Add((state, "State", gameDir));
                }
            }
            catch (Exception ex)
            {
                manifest.MissingOrSkipped.Add(stateRoot + " was not followed: " + ex.Message);
            }
        }

        foreach (string transactionRoot in Directory.EnumerateDirectories(gameDir, ".dtmapi-*-install-*", SearchOption.TopDirectoryOnly))
        {
            try
            {
                FileSystemSafety.AssertNoLinksOnExistingPath(gameDir, transactionRoot);
                string receipt = Path.Combine(transactionRoot, "transaction.json");
                if (File.Exists(receipt))
                {
                    FileSystemSafety.AssertNoLinksOnExistingPath(gameDir, receipt);
                    sources.Add((receipt, "Transactions", gameDir));
                }
            }
            catch (Exception ex)
            {
                manifest.MissingOrSkipped.Add(transactionRoot + " was not followed: " + ex.Message);
            }
        }

        try
        {
            foreach ((string playerLog, string safetyRoot) in DiscoverPlayerLogs(gameDir, platform))
                AddIfPresent(sources, manifest, playerLog, "Unity", safetyRoot);
        }
        catch (Exception ex)
        {
            manifest.MissingOrSkipped.Add("Unity player-log roots were not followed: " + ex.Message);
        }

        return sources
            .GroupBy(item => Path.GetFullPath(item.Source), FileSystemSafety.PathComparison == StringComparison.OrdinalIgnoreCase
                ? StringComparer.OrdinalIgnoreCase
                : StringComparer.Ordinal)
            .Select(group => group.First())
            .ToList();
    }

    private static IEnumerable<(string Path, string SafetyRoot)> DiscoverPlayerLogs(string gameDir, PlatformEnvironment platform)
    {
        string user = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrWhiteSpace(user))
        {
            yield return (Path.Combine(user, "AppData", "LocalLow", "RedSawGames", "DolocTown", "Player.log"), user);
            yield return (Path.Combine(user, "AppData", "LocalLow", "RedSawGames", "DolocTown", "Player-prev.log"), user);
        }

        if (platform.HostKind != HostKind.Linux)
            yield break;

        DirectoryInfo? current = new(gameDir);
        while (current is not null && !string.Equals(current.Name, "steamapps", StringComparison.OrdinalIgnoreCase))
            current = current.Parent;
        if (current is null)
            yield break;

        string usersRoot = Path.Combine(current.FullName, "compatdata", "2285550", "pfx", "drive_c", "users");
        if (!Directory.Exists(usersRoot))
            yield break;
        FileSystemSafety.AssertNoLinksOnExistingPath(current.FullName, usersRoot);
        foreach (string wineUser in Directory.EnumerateDirectories(usersRoot))
        {
            FileSystemSafety.AssertNoLinksOnExistingPath(current.FullName, wineUser);
            yield return (Path.Combine(wineUser, "AppData", "LocalLow", "RedSawGames", "DolocTown", "Player.log"), current.FullName);
            yield return (Path.Combine(wineUser, "AppData", "LocalLow", "RedSawGames", "DolocTown", "Player-prev.log"), current.FullName);
        }
    }

    private static void AddIfPresent(
        ICollection<(string Source, string Group, string SafetyRoot)> sources,
        LogCollectionManifest manifest,
        string path,
        string group,
        string safetyRoot)
    {
        if (File.Exists(path))
        {
            try
            {
                AssertSafeSource(safetyRoot, path);
                sources.Add((path, group, safetyRoot));
            }
            catch (Exception ex)
            {
                manifest.MissingOrSkipped.Add(path + " was not followed: " + ex.Message);
            }
        }
        else
            manifest.MissingOrSkipped.Add(path + " is missing.");
    }

    private static string BuildEnvironmentText(PlatformEnvironment platform)
    {
        return string.Join(Environment.NewLine, new[]
        {
            "GeneratedAtUtc=" + DateTime.UtcNow.ToString("O"),
            "Host=" + platform.HostLabel,
            "OS=" + Environment.OSVersion,
            "Framework=" + System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            "ProcessArchitecture=" + System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture,
            "RequiredLinuxLaunchOption=" + PlatformEnvironment.RequiredLinuxLaunchOption,
            "CrossOverOverride=" + PlatformEnvironment.CrossOverOverride,
            "Privacy=This report can contain usernames and local paths."
        });
    }

    private static string SanitizeFileName(string name)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars())
            name = name.Replace(invalid, '_');
        return string.IsNullOrWhiteSpace(name) ? "log.txt" : name;
    }

    private static void AssertSafeSource(string safetyRoot, string source)
    {
        string full = Path.GetFullPath(source);
        FileSystemSafety.AssertNoLinksOnExistingPath(safetyRoot, full);
    }
}
