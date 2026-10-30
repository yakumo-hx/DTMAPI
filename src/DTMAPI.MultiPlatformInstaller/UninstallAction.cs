using System;
using System.Linq;

namespace DTMAPI.MultiPlatformInstaller;

internal static class UninstallAction
{
    public static InstallerResult Run(string gameDir, PlatformEnvironment platform, bool confirmLegacyEquipmentRemoval = false, bool nonInteractive = false)
    {
        using InstallerLog log = new(gameDir);
        try
        {
            platform.ValidateGameDirectory(gameDir, "uninstall target");
            if (platform.IsGameRunning(gameDir, out string processDetail))
                throw new InstallerException(InstallerCodes.GameRunning, 3,
                    "请完全退出游戏后再卸载。", "Fully exit the game before uninstalling.", processDetail);
            using IDisposable mutationLock = platform.AcquireMutationLock(gameDir);
            using UninstallCleanup cleanup = new(gameDir, log);
            cleanup.Discover();
            if (cleanup.Equipment.Count > 0 && !confirmLegacyEquipmentRemoval)
            {
                Console.WriteLine("旧装备数据将永久删除，尚未采用的旧装备会丢失。原生存档不受影响。 / Legacy equipment data will be permanently lost; native saves are preserved.");
                foreach (string path in cleanup.Equipment.Take(20)) Console.WriteLine(path);
                log.Write("LegacyEquipment=" + cleanup.Equipment.Count);
                bool accepted = false;
                if (!nonInteractive && !Console.IsInputRedirected)
                {
                    Console.Write("确认卸载并删除这些数据？输入 YES / Type YES to uninstall and delete this data: ");
                    accepted = string.Equals(Console.ReadLine()?.Trim(), "YES", StringComparison.OrdinalIgnoreCase);
                }
                if (!accepted)
                {
                    log.Write("CANCELLED: no game files changed.");
                    return InstallerResult.Failure("DTM-W2001", 2, "已取消卸载；未修改游戏文件。",
                        "Uninstall cancelled; no game files changed.", "For unattended cleanup, explicitly pass --confirm-legacy-equipment-removal.", "Log=" + log.Path);
                }
            }
            if (platform.IsGameRunning(gameDir, out processDetail))
                throw new InstallerException(InstallerCodes.GameRunning, 3, "请关闭游戏。", "Close the game.", processDetail);
            cleanup.Execute();
            log.Write("Removed=" + cleanup.Removed + "; Remaining=" + cleanup.Problems.Count);
            if (cleanup.Problems.Count > 0)
                return InstallerResult.Failure("DTM-E2001", 3, "卸载尚未全部完成。请处理下列路径后重试；无需重新安装。",
                    "Uninstall is incomplete. Resolve the paths below and retry; reinstalling is unnecessary.",
                    "Log=" + log.Path, string.Join(Environment.NewLine, cleanup.Problems),
                    "Close applications holding these files; check folder permissions. Export this log for unknown backups or ownership conflicts.");
            return InstallerResult.Success(cleanup.Removed == 0 ? "DTM-S2002" : InstallerCodes.Uninstalled,
                cleanup.Removed == 0 ? "未发现已安装内容，无需删除。" : "DTMAPI 与 BepInEx 已卸载。官方 Mod 和原生存档已保留。",
                cleanup.Removed == 0 ? "No installed content found; nothing to remove." : "DTMAPI and BepInEx uninstalled. Official Mods and native saves were preserved.",
                "Third-party plugin files are preserved; they need BepInEx to load.", "Log=" + log.Path);
        }
        catch (Exception ex)
        {
            log.Write("FAILED " + ex);
            throw;
        }
    }
}
