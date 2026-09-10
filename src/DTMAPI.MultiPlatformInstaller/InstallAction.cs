using System;
using System.Collections.Generic;
using System.IO;

namespace DTMAPI.MultiPlatformInstaller;

internal static class InstallAction
{
    public static InstallerResult Run(PackageLayout package, string gameDir, PlatformEnvironment platform)
    {
        if (platform.IsGameRunning(gameDir, out string processDetail))
            throw new InstallerException(InstallerCodes.GameRunning, 3,
                "检测到《多洛可小镇》仍在运行。请完全退出游戏后重试。",
                "Doloc Town is still running. Fully exit the game before retrying.", processDetail);

        using IDisposable mutationLock = platform.AcquireMutationLock(gameDir);
        RuntimeTransaction transaction = new(gameDir);
        transaction.PrepareForInstall();
        InstallStateAuthority.AssertCompatibleForInstall(package, gameDir);

        bool bepInExWasComplete = BepInExInstaller.IsComplete(gameDir);
        if (!bepInExWasComplete)
        {
            string scratch = FileSystemSafety.CombineUnder(gameDir,
                ".dtmapi-multiplatform-bepinex-source-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(scratch);
            try
            {
                IReadOnlyList<InstallFile> bepInExFiles = BepInExInstaller.PrepareFiles(package, scratch);
                transaction.Execute(bepInExFiles, null, "bepinex");
            }
            finally
            {
                try
                {
                    if (Directory.Exists(scratch))
                    {
                        FileSystemSafety.AssertNoLinksOnExistingPath(gameDir, scratch);
                        Directory.Delete(scratch, true);
                    }
                }
                catch
                {
                    // This scratch contains package/download bytes only and is non-authoritative.
                }
            }
        }

        if (!BepInExInstaller.IsComplete(gameDir))
            throw new InstallerException(InstallerCodes.LocalAccess, 3,
                "BepInEx/Doorstop 安装后仍不完整，Runtime 安装已停止。",
                "BepInEx/Doorstop remained incomplete after repair; Runtime installation stopped.");

        InstallStateV1 state = BuildInstallState(package, gameDir, platform, bepInExWasComplete);
        transaction.Execute(package.InstallFiles, state, "runtime");

        StatusReport status = StatusAction.Evaluate(package, gameDir, platform);
        if (status.Files != FileHealth.Healthy)
            throw new InstallerException(InstallerCodes.PackageInvalid, 3,
                "Runtime 文件已写入，但最终只读校验未通过。",
                "Runtime files were written, but the final read-only validation did not pass.", StatusAction.Format(status));

        List<string> details = new()
        {
            "GameDir=" + gameDir,
            "Runtime=" + package.ReleaseManifest.DTMAPIVersion,
            "Host=" + platform.HostLabel,
            StatusAction.Format(status)
        };
        if (platform.HostKind == HostKind.Linux)
        {
            details.Add("[FILES INSTALLED — ONE REQUIRED STEP REMAINS]");
            details.Add("Steam Deck / Linux setup is not finished. Open Steam > Doloc Town > Properties > General > Launch Options and paste exactly:");
            details.Add(PlatformEnvironment.RequiredLinuxLaunchOption);
            details.Add("Without this setting, the game may open normally but DTMAPI will not load.");
        }
        else if (platform.HostKind == HostKind.WineOrCrossOver)
        {
            details.Add("CrossOver setup still requires this existing game Bottle: Wine Configuration > Libraries > winhttp > Native, then Builtin.");
        }

        return InstallerResult.Success(InstallerCodes.Installed,
            platform.HostKind == HostKind.Linux
                ? "DTMAPI 文件安装完成；仍必须填写 Steam 启动选项。"
                : "DTMAPI Runtime 安装并校验完成。",
            platform.HostKind == HostKind.Linux
                ? "DTMAPI files are installed; the Steam launch option is still required."
                : "DTMAPI Runtime installation and verification completed.", details.ToArray());
    }

    private static InstallStateV1 BuildInstallState(PackageLayout package, string gameDir, PlatformEnvironment platform, bool bepInExWasComplete)
    {
        InstallStateV1 state = new()
        {
            InstalledAt = DateTime.UtcNow.ToString("O"),
            DTMAPIVersion = package.ReleaseManifest.DTMAPIVersion,
            BinaryVersion = package.ReleaseManifest.BinaryVersion,
            SourceRepoCommit = package.ReleaseManifest.BuildCommit,
            GameDir = gameDir,
            PluginDir = FileSystemSafety.CombineUnder(gameDir, "BepInEx/plugins/DTMAPI"),
            BepInExDetectedBeforeInstall = bepInExWasComplete,
            BepInExInstalledByDTMAPI = !bepInExWasComplete,
            InstallerHost = platform.HostLabel,
            OptionalComponents = package.ReleaseManifest.OptionalComponents
        };
        foreach (InstallFile file in package.InstallFiles)
        {
            state.FilesInstalled.Add(new InstalledFileReceipt
            {
                Kind = file.Kind,
                Path = FileSystemSafety.CombineUnder(gameDir, file.TargetRelativePath),
                RelativePath = file.TargetRelativePath,
                Sha256 = file.Sha256,
                Length = file.Length,
                FileVersion = file.FileVersion,
                ComponentId = file.ComponentId
            });
        }
        state.FilesInstalled.Add(new InstalledFileReceipt
        {
            Kind = "install-state",
            Path = FileSystemSafety.CombineUnder(gameDir, "DTMAPI/install-state.json"),
            RelativePath = "DTMAPI/install-state.json"
        });
        return state;
    }
}
