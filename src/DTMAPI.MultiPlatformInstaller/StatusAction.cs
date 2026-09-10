using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DTMAPI.MultiPlatformInstaller;

internal static class StatusAction
{
    public static StatusReport Evaluate(PackageLayout package, string gameDir, PlatformEnvironment platform)
    {
        StatusReport report = new()
        {
            GeneratedAtUtc = DateTime.UtcNow.ToString("O"),
            GameDir = gameDir
        };

        RuntimeTransaction transaction = new(gameDir);
        IReadOnlyList<TransactionInspection> transactions = transaction.InspectAll();
        if (transactions.Count > 0)
        {
            report.Files = FileHealth.Blocked;
            foreach (TransactionInspection item in transactions)
                report.Findings.Add($"Transaction={item.Kind}; Path={item.Root}; Detail={item.Detail}");
        }
        else
        {
            EvaluateFiles(package, gameDir, report);
        }

        report.LaunchIntegration = platform.GetLaunchIntegration(out string launchDetail);
        report.Findings.Add(launchDetail);
        report.RuntimeObservation = EvaluateRuntimeObservation(gameDir, out string runtimeDetail);
        report.Findings.Add(runtimeDetail);

        if (platform.HostKind == HostKind.Linux)
        {
            report.RequiredAction = "Steam Deck / Linux 必须在启动选项粘贴：" + PlatformEnvironment.RequiredLinuxLaunchOption;
        }
        else if (platform.HostKind == HostKind.WineOrCrossOver)
        {
            report.RequiredAction = "CrossOver: use the existing Steam/game Bottle; Wine Configuration > Libraries > winhttp > Native, then Builtin.";
        }
        else
        {
            report.RequiredAction = report.Files == FileHealth.Healthy
                ? "Native Windows requires no Proton launch option."
                : "Run the installer again to repair the exact Runtime payload.";
        }

        return report;
    }

    private static void EvaluateFiles(PackageLayout package, string gameDir, StatusReport report)
    {
        int present = 0;
        int missing = 0;
        int invalid = 0;
        foreach (InstallFile expected in package.InstallFiles)
        {
            string target = FileSystemSafety.CombineUnder(gameDir, expected.TargetRelativePath);
            if (!File.Exists(target))
            {
                missing++;
                report.Findings.Add("MISSING " + expected.TargetRelativePath);
                continue;
            }
            present++;
            try
            {
                FileSystemSafety.AssertNoLinksOnExistingPath(gameDir, target);
                if (!FileSystemSafety.Matches(target, expected.Length, expected.Sha256))
                {
                    invalid++;
                    report.Findings.Add("INVALID " + expected.TargetRelativePath);
                }
            }
            catch (Exception ex)
            {
                invalid++;
                report.Findings.Add("BLOCKED " + expected.TargetRelativePath + ": " + ex.Message);
            }
        }

        string installStatePath = FileSystemSafety.CombineUnder(gameDir, "DTMAPI/install-state.json");
        if (!File.Exists(installStatePath))
        {
            missing++;
            report.Findings.Add("MISSING DTMAPI/install-state.json");
        }
        else
        {
            present++;
            try
            {
                FileSystemSafety.AssertNoLinksOnExistingPath(gameDir, installStatePath);
                InstallStateV1? state = JsonStore.Read(installStatePath, InstallerJsonContext.Default.InstallStateV1);
                if (!InstallStateAuthority.TryValidateExactState(package, gameDir, state, out string stateDetail))
                {
                    invalid++;
                    report.Findings.Add("INVALID DTMAPI/install-state.json authority: " + stateDetail);
                }
            }
            catch (Exception ex)
            {
                invalid++;
                report.Findings.Add("INVALID DTMAPI/install-state.json: " + ex.Message);
            }
        }

        if (!BepInExInstaller.IsComplete(gameDir))
        {
            missing++;
            report.Findings.Add("MISSING_OR_INVALID BepInEx/Doorstop bootstrap");
        }

        if (present == 0 && invalid == 0)
            report.Files = FileHealth.NotInstalled;
        else if (invalid > 0)
            report.Files = FileHealth.UpdateRequired;
        else if (missing > 0)
            report.Files = FileHealth.Partial;
        else
            report.Files = FileHealth.Healthy;
    }

    private static RuntimeObservationHealth EvaluateRuntimeObservation(string gameDir, out string detail)
    {
        DateTime installedUtc = DateTime.MinValue;
        string statePath = FileSystemSafety.CombineUnder(gameDir, "DTMAPI/install-state.json");
        try
        {
            if (File.Exists(statePath))
            {
                InstallStateV1? state = JsonStore.Read(statePath, InstallerJsonContext.Default.InstallStateV1);
                if (state is not null)
                    DateTime.TryParse(state.InstalledAt, null, System.Globalization.DateTimeStyles.RoundtripKind, out installedUtc);
            }
        }
        catch
        {
            // A malformed state becomes an unbound runtime observation.
        }

        List<string> candidates = new();
        string bepInExLog = FileSystemSafety.CombineUnder(gameDir, "BepInEx/LogOutput.log");
        if (File.Exists(bepInExLog))
        {
            try
            {
                FileSystemSafety.AssertNoLinksOnExistingPath(gameDir, bepInExLog);
                candidates.Add(bepInExLog);
            }
            catch
            {
                // Linked observation sources are never followed.
            }
        }
        string logsRoot = FileSystemSafety.CombineUnder(gameDir, "DTMAPI/logs");
        if (Directory.Exists(logsRoot))
        {
            try
            {
                FileSystemSafety.AssertNoLinksOnExistingPath(gameDir, logsRoot);
                foreach (string candidate in Directory.EnumerateFiles(logsRoot, "*.log", SearchOption.TopDirectoryOnly))
                {
                    FileSystemSafety.AssertNoLinksOnExistingPath(gameDir, candidate);
                    candidates.Add(candidate);
                }
            }
            catch
            {
                // Linked observation sources are never followed.
            }
        }

        foreach (string path in candidates.OrderByDescending(File.GetLastWriteTimeUtc))
        {
            try
            {
                DateTime writeUtc = File.GetLastWriteTimeUtc(path);
                using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                using StreamReader reader = new(stream);
                char[] buffer = new char[Math.Min(512 * 1024, (int)Math.Min(stream.Length, 512 * 1024))];
                int count = reader.Read(buffer, 0, buffer.Length);
                string text = new(buffer, 0, count);
                if (text.Contains("DTMAPI Bootstrap", StringComparison.OrdinalIgnoreCase) ||
                    (text.Contains("[DTMAPI]", StringComparison.OrdinalIgnoreCase) && text.Contains("Startup", StringComparison.OrdinalIgnoreCase)))
                {
                    if (installedUtc != DateTime.MinValue && writeUtc >= installedUtc.ToUniversalTime())
                    {
                        detail = "A post-install DTMAPI startup log was observed: " + path;
                        return RuntimeObservationHealth.FreshObserved;
                    }
                    detail = "A DTMAPI startup log exists, but it predates or cannot be bound to the current install receipt: " + path;
                    return RuntimeObservationHealth.Unbound;
                }
            }
            catch
            {
                // Optional observation sources never make status crash.
            }
        }

        detail = "No fresh DTMAPI startup observation was found. Static files alone do not prove injection.";
        return RuntimeObservationHealth.StaleOrMissing;
    }

    public static string Format(StatusReport report)
    {
        List<string> lines = new()
        {
            "DTMAPI-多平台 / DTMAPI - Multi-Platform",
            "Game: " + report.GameDir,
            "Files:               " + report.Files.ToString().ToUpperInvariant(),
            "Launch integration:  " + report.LaunchIntegration.ToString().ToUpperInvariant(),
            "Runtime observation: " + report.RuntimeObservation.ToString().ToUpperInvariant(),
            string.Empty,
            "Required action / 必须操作:",
            report.RequiredAction,
            string.Empty,
            "Findings / 详情:"
        };
        lines.AddRange(report.Findings.Select(item => "- " + item));
        return string.Join(Environment.NewLine, lines);
    }
}
