using System;
using System.IO;
using System.Linq;

namespace DTMAPI.MultiPlatformInstaller;

internal static class UninstallAction
{
    public static InstallerResult Run(PackageLayout package, string gameDir, PlatformEnvironment platform)
    {
        if (platform.IsGameRunning(gameDir, out string processDetail))
            throw new InstallerException(InstallerCodes.GameRunning, 3,
                "检测到《多洛可小镇》仍在运行。请完全退出游戏后再卸载。",
                "Doloc Town is still running. Fully exit the game before uninstalling.", processDetail);

        using IDisposable mutationLock = platform.AcquireMutationLock(gameDir);
        RuntimeTransaction transaction = new(gameDir);
        transaction.AssertSafeForUninstall();

        var allowlist = package.InstallFiles.Select(file => file.TargetRelativePath).ToList();
        allowlist.Add("DTMAPI/install-state.json");
        UninstallExecutionResult execution = transaction.ExecuteUninstall(allowlist);
        UninstallStateV1 state = execution.State;

        CleanupEmptyOwnedDirectories(gameDir);
        string warning = string.IsNullOrWhiteSpace(execution.StateReceiptWarning)
            ? "Uninstall recovery receipt: verified."
            : "WARNING: " + execution.StateReceiptWarning;
        return InstallerResult.Success(InstallerCodes.Uninstalled,
            state.NoOp ? "DTMAPI Runtime 已处于未安装状态；没有删除文件。" : "DTMAPI Runtime 已卸载，备份保留在 DTMAPI/backups。",
            state.NoOp ? "DTMAPI Runtime was already absent; no files were removed." : "DTMAPI Runtime was uninstalled; backups remain under DTMAPI/backups.",
            "Removed=" + state.Removed.Count,
            "BackupRoot=" + execution.BackupRoot,
            warning,
            "BepInEx, External plugins, Mods, configs, logs, reports and player data were preserved.");
    }

    private static void CleanupEmptyOwnedDirectories(string gameDir)
    {
        string[] directories =
        {
            "BepInEx/plugins/DTMAPI/assets/branding",
            "BepInEx/plugins/DTMAPI/assets",
            "BepInEx/plugins/DTMAPI",
            "DTMAPI/components/compatibility",
            "DTMAPI/components",
            "DTMAPI/tools"
        };
        foreach (string relative in directories)
            FileSystemSafety.DeleteDirectoryIfEmpty(gameDir, FileSystemSafety.CombineUnder(gameDir, relative));
    }
}
