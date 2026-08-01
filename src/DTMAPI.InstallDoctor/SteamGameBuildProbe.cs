using System;
using System.IO;

namespace DTMAPI.InstallDoctor;

internal sealed class SteamGameBuildProbe
{
    public string ManifestPath { get; init; } = string.Empty;
    public string BuildId { get; init; } = string.Empty;
    public string Error { get; init; } = string.Empty;

    public static SteamGameBuildProbe Inspect(string gameRoot)
    {
        string fullRoot = Path.GetFullPath(gameRoot);
        DirectoryInfo? common = Directory.GetParent(fullRoot);
        DirectoryInfo? steamApps = common?.Parent;
        string manifestPath = steamApps == null
            ? string.Empty
            : Path.Combine(steamApps.FullName, "appmanifest_2285550.acf");
        if (common == null || !common.Name.Equals("common", StringComparison.OrdinalIgnoreCase))
        {
            return new SteamGameBuildProbe
            {
                ManifestPath = manifestPath,
                Error = "Doloc Town game root is not under an explicit Steam steamapps/common directory."
            };
        }
        if (manifestPath.Length == 0 || !File.Exists(manifestPath))
        {
            return new SteamGameBuildProbe
            {
                ManifestPath = manifestPath,
                Error = "Steam appmanifest_2285550.acf was not found next to the game library."
            };
        }

        try
        {
            using FileStream stream = new(
                manifestPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete,
                bufferSize: 32 * 1024,
                options: FileOptions.SequentialScan);
            using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
            string appId = string.Empty;
            string buildId = string.Empty;
            int appIdCount = 0;
            int buildIdCount = 0;
            int appStateDepth = 0;
            bool appStateHeaderSeen = false;
            bool appStateClosed = false;
            while (reader.ReadLine() is { } line)
            {
                string trimmed = line.Trim();
                if (appStateDepth == 0)
                {
                    if (!appStateHeaderSeen)
                    {
                        if (trimmed.Equals("\"AppState\"", StringComparison.OrdinalIgnoreCase))
                            appStateHeaderSeen = true;
                        continue;
                    }
                    if (!appStateClosed && trimmed.Equals("{", StringComparison.Ordinal))
                    {
                        appStateDepth = 1;
                        continue;
                    }
                    if (trimmed.Length == 0)
                        continue;
                    appStateClosed = true;
                    continue;
                }

                if (trimmed.Equals("{", StringComparison.Ordinal))
                {
                    appStateDepth++;
                    continue;
                }
                if (trimmed.Equals("}", StringComparison.Ordinal))
                {
                    appStateDepth--;
                    if (appStateDepth == 0)
                        appStateClosed = true;
                    continue;
                }
                if (appStateDepth != 1)
                    continue;
                if (TryReadQuotedValue(line, "appid", out string parsedAppId))
                {
                    appId = parsedAppId;
                    appIdCount++;
                }
                if (TryReadQuotedValue(line, "buildid", out string parsedBuildId))
                {
                    buildId = parsedBuildId;
                    buildIdCount++;
                }
            }
            if (!appStateHeaderSeen || !appStateClosed || appStateDepth != 0 ||
                appIdCount != 1 || buildIdCount != 1 ||
                !appId.Equals("2285550", StringComparison.Ordinal) || !long.TryParse(buildId, out _))
                return new SteamGameBuildProbe
                {
                    ManifestPath = manifestPath,
                    Error = "Steam appmanifest identity/build is invalid for Doloc Town app 2285550."
                };
            return new SteamGameBuildProbe
            {
                ManifestPath = manifestPath,
                BuildId = buildId
            };
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new SteamGameBuildProbe
            {
                ManifestPath = manifestPath,
                Error = ex.GetType().Name + ": " + ex.Message
            };
        }
    }

    private static bool TryReadQuotedValue(string line, string key, out string value)
    {
        value = string.Empty;
        string trimmed = line.Trim();
        if (!trimmed.StartsWith("\"" + key + "\"", StringComparison.OrdinalIgnoreCase))
            return false;
        string[] quoted = trimmed.Split('"');
        if (quoted.Length < 4 || quoted[3].Length == 0)
            return false;
        value = quoted[3];
        return true;
    }
}
