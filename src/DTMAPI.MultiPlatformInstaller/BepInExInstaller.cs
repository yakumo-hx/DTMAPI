using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;

namespace DTMAPI.MultiPlatformInstaller;

internal static class BepInExInstaller
{
    private const string ExpectedArchiveSha256 = "82f9878551030f54657792c0740d9d51a09500eeae1fba21106b0c441e6732c4";
    private const long ExpectedArchiveLength = 639118;
    private const string DownloadUrl = "https://github.com/BepInEx/BepInEx/releases/download/v5.4.23.5/BepInEx_win_x64_5.4.23.5.zip";
    internal static readonly string[] RequiredRelativePaths =
    {
        ".doorstop_version",
        "changelog.txt",
        "winhttp.dll",
        "doorstop_config.ini",
        "BepInEx/core/0Harmony20.dll",
        "BepInEx/core/BepInEx.dll",
        "BepInEx/core/BepInEx.Preloader.dll",
        "BepInEx/core/BepInEx.Harmony.dll",
        "BepInEx/core/0Harmony.dll",
        "BepInEx/core/HarmonyXInterop.dll",
        "BepInEx/core/Mono.Cecil.dll",
        "BepInEx/core/Mono.Cecil.Mdb.dll",
        "BepInEx/core/Mono.Cecil.Pdb.dll",
        "BepInEx/core/Mono.Cecil.Rocks.dll",
        "BepInEx/core/MonoMod.RuntimeDetour.dll",
        "BepInEx/core/MonoMod.Utils.dll"
    };

    public static bool IsComplete(string gameDir)
    {
        try
        {
            foreach (string relative in RequiredRelativePaths)
            {
                string path = FileSystemSafety.CombineUnder(gameDir, relative);
                if (!File.Exists(path) || new FileInfo(path).Length == 0)
                    return false;
                FileSystemSafety.AssertNoLinksOnExistingPath(gameDir, path);
            }

            Dictionary<string, string> values = ReadIniValues(FileSystemSafety.CombineUnder(gameDir, "doorstop_config.ini"));
            return values.TryGetValue("enabled", out string? enabled) &&
                   string.Equals(enabled, "true", StringComparison.OrdinalIgnoreCase) &&
                   values.TryGetValue("target_assembly", out string? target) &&
                   string.Equals(target.Replace('/', '\\'), "BepInEx\\core\\BepInEx.Preloader.dll", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static Dictionary<string, string> ReadIniValues(string path)
    {
        Dictionary<string, string> result = new(StringComparer.OrdinalIgnoreCase);
        foreach (string rawLine in File.ReadLines(path))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith(';') || line.StartsWith('#') || line.StartsWith('['))
                continue;
            int separator = line.IndexOf('=');
            if (separator <= 0)
                continue;
            string key = line[..separator].Trim();
            string value = line[(separator + 1)..].Trim();
            if (key.Length > 0)
                result[key] = value;
        }
        return result;
    }

    public static IReadOnlyList<InstallFile> PrepareFiles(PackageLayout package, string transactionScratchRoot)
    {
        string archive = ResolveArchive(package, transactionScratchRoot);
        string extractionRoot = Path.Combine(transactionScratchRoot, "bepinex-extract");
        Directory.CreateDirectory(extractionRoot);
        ExtractSafely(archive, extractionRoot);

        List<InstallFile> files = new();
        foreach (string source in Directory.EnumerateFiles(extractionRoot, "*", SearchOption.AllDirectories))
        {
            FileSystemSafety.AssertNoLinksOnExistingPath(extractionRoot, source);
            string relative = FileSystemSafety.NormalizeRelativePath(Path.GetRelativePath(extractionRoot, source));
            FileInfo info = new(source);
            files.Add(new InstallFile
            {
                Kind = "bepinex",
                SourcePath = source,
                TargetRelativePath = relative,
                Length = info.Length,
                Sha256 = FileSystemSafety.Sha256(source)
            });
        }
        return files;
    }

    private static string ResolveArchive(PackageLayout package, string scratchRoot)
    {
        if (FileSystemSafety.Matches(package.BepInExArchivePath, ExpectedArchiveLength, ExpectedArchiveSha256))
            return package.BepInExArchivePath;

        string destination = Path.Combine(scratchRoot, "BepInEx_win_x64_5.4.23.5.zip");
        try
        {
            using HttpClient client = new() { Timeout = TimeSpan.FromMinutes(2) };
            using HttpResponseMessage response = client.GetAsync(DownloadUrl, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
            {
                using Stream input = response.Content.ReadAsStream();
                using FileStream output = new(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                byte[] buffer = new byte[81920];
                long total = 0;
                while (true)
                {
                    int read = input.Read(buffer, 0, buffer.Length);
                    if (read == 0)
                        break;
                    total += read;
                    if (total > 50L * 1024 * 1024)
                        throw new InvalidDataException("BepInEx download exceeded the bounded size.");
                    output.Write(buffer, 0, read);
                }
                output.Flush(true);
            }
            if (!FileSystemSafety.Matches(destination, ExpectedArchiveLength, ExpectedArchiveSha256))
                throw new InvalidDataException("Downloaded BepInEx bytes failed fixed SHA-256 validation.");
            return destination;
        }
        catch (Exception ex)
        {
            throw new InstallerException(InstallerCodes.NetworkFallback, 3,
                "包内 BepInEx 压缩包无效，且官方固定哈希在线备用下载失败。",
                "The bundled BepInEx archive was invalid and the fixed-hash official online fallback failed.",
                ex.Message, ex);
        }
    }

    private static void ExtractSafely(string archivePath, string destinationRoot)
    {
        using ZipArchive archive = ZipFile.OpenRead(archivePath);
        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            string name = entry.FullName.Replace('\\', '/');
            if (string.IsNullOrEmpty(name) || name.EndsWith("/", StringComparison.Ordinal))
                continue;
            int unixType = (entry.ExternalAttributes >> 16) & 0xF000;
            if (unixType == 0xA000)
                throw new InvalidDataException("BepInEx ZIP contains a symbolic link entry: " + name);
            string relative = FileSystemSafety.NormalizeRelativePath(name);
            string destination = FileSystemSafety.CombineUnder(destinationRoot, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            using Stream input = entry.Open();
            using FileStream output = new(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            input.CopyTo(output);
        }
    }
}
