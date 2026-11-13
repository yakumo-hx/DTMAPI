using System;
using System.Collections.Generic;

namespace DTMAPI.MultiPlatformInstaller;

internal static class UninstallAction
{
    public static InstallerResult Run(string gameDir, PlatformEnvironment platform)
    {
        platform.ValidateGameDirectory(gameDir, "uninstall target");
        if (platform.IsGameRunning(gameDir, out string processDetail))
            throw new InstallerException(InstallerCodes.GameRunning, 3,
                "请完全退出游戏后再卸载。", "Fully exit the game before uninstalling.", processDetail);
        using IDisposable mutationLock = platform.AcquireMutationLock(gameDir);
        using InstallerLog log = new(gameDir);
        UninstallCleanup cleanup = new(gameDir, log);
        cleanup.Execute();
        log.Write("Removed=" + cleanup.Removed + "; Remaining=" + cleanup.Problems.Count);
        List<string> details = new() { "Log=" + log.Path };
        if (cleanup.PreservedEquipment)
            details.Add("保留了旧装备文件：DTMAPI/config/protected-items/equipment-slots/");
        if (cleanup.OtherPlugins.Count > 0)
            details.Add("保留 BepInEx，以下插件仍需要它：" + Environment.NewLine + string.Join(Environment.NewLine, cleanup.OtherPlugins));
        if (cleanup.Problems.Count > 0)
        {
            details.AddRange(cleanup.Problems);
            return InstallerResult.Failure("DTM-E2001", 3,
                "卸载尚未全部完成。请关闭占用下列文件的程序、检查目录权限，再运行一次卸载。",
                "Uninstall is incomplete. Close programs using the listed files, check folder permissions, and retry.", details.ToArray());
        }
        return InstallerResult.Success(cleanup.Removed == 0 ? "DTM-S2002" : InstallerCodes.Uninstalled,
            cleanup.Removed == 0 ? "没有需要删除的内容。" : "DTMAPI 已卸载。官方 Mod 和原生存档已保留。",
            cleanup.Removed == 0 ? "Nothing to remove." : "DTMAPI uninstalled. Official Mods and native saves were preserved.", details.ToArray());
    }
}
