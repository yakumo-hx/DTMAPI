using System;
using System.Collections.Generic;

namespace DTMAPI.MultiPlatformInstaller
{
    // Both installers and the package builders read this file. Keep C# 5 syntax
    // so Windows PowerShell 5.1 can load it with Add-Type.
    public static class RuntimePackageFiles
    {
        public const string PayloadDirectory = "Content/DTMAPIInstaller/Payload/";
        public const string RuntimeDirectory = "BepInEx/plugins/DTMAPI/";
        public const string ToolsDirectory = "Content/DTMAPIInstaller/tools/";
        public const string ReleaseManifest = "Content/DTMAPI/release-manifest.json";
        public const string BepInExArchive = "Content/.tools/bepinex/BepInEx_win_x64_5.4.23.5.zip";
        public const string BrandingFile = RuntimeDirectory + "assets/branding/dtmapi-icon.png";
        public const string WindowsHost = "DTMAPI-MultiPlatform-Installer.exe";
        public const string LinuxHost = "Content/DTMAPIInstaller/hosts/linux-x64/dtmapi-installer";
        public const string HostManifest = "Content/DTMAPIInstaller/hosts/host-artifacts.json";
        public const string MultiPlatformManifest = "Content/DTMAPIInstaller/multiplatform-package.json";
        public const string CompatibilityComponent = "DTMAPI/components/compatibility/DTMAPI.GameBridge.DolocTown.Compatibility.dll";

        public static readonly string[] WindowsEntries =
        {
            "1_install_dtmapi.bat", "2_uninstall_dtmapi.bat",
            "3_check_dtmapi_status.bat", "4_collect_dtmapi_logs.bat"
        };

        public static readonly string[] LinuxEntries =
        {
            "1_install_dtmapi.sh", "2_uninstall_dtmapi.sh",
            "3_check_dtmapi_status.sh", "4_collect_dtmapi_logs.sh"
        };

        public static readonly string[] Assemblies =
        {
            "DTMAPI.Abstractions.dll",
            "DTMAPI.BepInExBootstrap.dll",
            "DTMAPI.Core.dll",
            "DTMAPI.GameBridge.DolocTown.dll",
            "DTMAPI.ModConfigMenu.dll"
        };

        public static readonly Dictionary<string, string> Components = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "DTMAPI/components/modules/DTMAPI.RuntimeModules.dll", "runtime-modules" },
            { "DTMAPI/components/animals/DTMAPI.AnimalContent.dll", "animal-contract" },
            { "DTMAPI/components/animals/DTMAPI.Modules.Animals.dll", "animal-backend" },
            { "DTMAPI/components/monsters/DTMAPI.MonsterContent.dll", "monster-contract" },
            { "DTMAPI/components/monsters/DTMAPI.Modules.Monsters.dll", "monster-backend" },
            { "DTMAPI/components/audio/DTMAPI.AudioContent.dll", "audio-contract" },
            { "DTMAPI/components/audio/DTMAPI.AudioPreparation.dll", "audio-preparation" },
            { "DTMAPI/components/audio/DTMAPI.Modules.Audio.dll", "audio-backend" },
            { "DTMAPI/components/audio/audio-decoder/ffmpeg.exe", "audio-decoder-ffmpeg.exe" },
            { "DTMAPI/components/audio/audio-decoder/LICENSE.txt", "audio-decoder-LICENSE.txt" },
            { "DTMAPI/components/audio/audio-decoder/distribution.json", "audio-decoder-distribution.json" }
        };

        public static readonly string[] InstalledTools =
        {
            "common.ps1", "release-common.ps1", "uninstall-dtmapi.ps1",
            "check-dtmapi-status.ps1", "collect-logs.ps1", "analyze-startup-evidence.ps1",
            "dtmapi-runtime-version.props"
        };

        public static readonly string[] CommonTools =
        {
            "common/repository.ps1", "common/files.ps1", "common/toolchain.ps1",
            "common/runtime-lock.ps1", "common/game-session.ps1", "common/installer.ps1"
        };

        public static readonly string[] CurrentTools =
        {
            "RuntimePackageFiles.cs", "uninstall-common.ps1", "uninstall-targets.json"
        };

        public static readonly string[] PackageTools =
        {
            "install-bepinex.ps1", "install-to-game.ps1", "invoke-dtmapi-action.cmd", "probe-powershell-host.ps1"
        };

        public static IEnumerable<string> GetInstalledTools(bool candidate, bool currentRuntime)
        {
            foreach (string file in InstalledTools) yield return file;
            if (!candidate) yield break;
            if (!currentRuntime)
            {
                // The published 0.7.0 scripts need their original metadata file.
                yield return "dtmapi-product-definitions.json";
                yield break;
            }
            foreach (string file in CommonTools) yield return file;
            foreach (string file in CurrentTools) yield return file;
        }

        public static IEnumerable<string> GetPackageTools(bool currentRuntime)
        {
            foreach (string file in GetInstalledTools(true, currentRuntime)) yield return file;
            foreach (string file in PackageTools) yield return file;
        }

        public static IEnumerable<string> GetSharedFiles(bool currentRuntime)
        {
            foreach (string file in Assemblies) yield return PayloadDirectory + RuntimeDirectory + file;
            if (!currentRuntime)
                foreach (string file in GetPackageTools(false)) yield return ToolsDirectory + file;
            yield return ReleaseManifest;
            yield return BepInExArchive;
            yield return PayloadDirectory + BrandingFile;
            foreach (KeyValuePair<string, string> component in GetComponents(currentRuntime))
                yield return PayloadDirectory + component.Key;
        }

        public static IEnumerable<KeyValuePair<string, string>> GetComponents(bool currentRuntime)
        {
            if (!currentRuntime)
            {
                yield return new KeyValuePair<string, string>(CompatibilityComponent, "gamebridge-compatibility-host");
                yield break;
            }
            foreach (KeyValuePair<string, string> component in Components) yield return component;
        }
    }
}

