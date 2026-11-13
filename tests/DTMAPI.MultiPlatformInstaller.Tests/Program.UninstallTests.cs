using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DTMAPI.MultiPlatformInstaller.Tests;

internal static partial class Program
{
    private static void WithUninstallGame(Action<string> action)
    {
        string root = CreateManagedTestDirectory();
        string? runtime = Environment.GetEnvironmentVariable("DTMAPI_RUNTIME_DIR");
        string? state = Environment.GetEnvironmentVariable("DTMAPI_STATE_DIR");
        try
        {
            Environment.SetEnvironmentVariable("DTMAPI_RUNTIME_DIR", null);
            Environment.SetEnvironmentVariable("DTMAPI_STATE_DIR", null);
            Put(root, "DolocTown.exe", "fixture");
            Put(root, "DolocTown_Data/Managed/Assembly-CSharp.dll", "fixture");
            action(root);
        }
        finally
        {
            Environment.SetEnvironmentVariable("DTMAPI_RUNTIME_DIR", runtime);
            Environment.SetEnvironmentVariable("DTMAPI_STATE_DIR", state);
            Directory.Delete(root, true);
        }
    }

    private static void Put(string root, string relative, string value = "fixture")
    {
        string path = Path.Combine(root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, value);
    }

    private static int Uninstall(string root)
    {
        Assembly asm = PackageLayoutType().Assembly;
        object platform = Activator.CreateInstance(asm.GetType("DTMAPI.MultiPlatformInstaller.PlatformEnvironment", true)!)!;
        object result = asm.GetType("DTMAPI.MultiPlatformInstaller.UninstallAction", true)!.GetMethod("Run")!
            .Invoke(null, new[] { (object)root, platform })!;
        return (int)result.GetType().GetProperty("ExitCode")!.GetValue(result)!;
    }

    private static void CompleteUninstallRemovesOwnedContentAndIsRepeatable() => WithUninstallGame(root =>
    {
        foreach (string path in new[] { "BepInEx/plugins/DTMAPI/old.dll", "BepInEx/core/BepInEx.dll",
            "BepInEx/config/dev.dtmapi.bootstrap.cfg", "BepInEx/cache/preload.dat", "DTMAPI/components/audio/old.dll",
            "DTMAPI/cache/audio/v1/a.wav", "DTMAPI/logs/latest.log", "DTMAPI/config/settings.json", "DTMAPI/global-data/mod.json",
            "DTMAPI/backups/unknown/player.dat", "DTMAPI/backups/bepinex-install-old/winhttp.dll" }) Put(root, path);
        Put(root, "DTMAPI/install-state.json", "corrupt old record");
        foreach (string path in new[] { "doorstop_config.ini", "winhttp.dll", ".doorstop_version", "changelog.txt" })
            Put(root, path, "unrecognized file content");
        File.SetAttributes(Path.Combine(root, "DTMAPI", "config", "settings.json"), FileAttributes.ReadOnly);
        string[] preserved = { "MODS/OtherMod/content.json", "SAVE/slot.dat", "SAVE/mod_infos.json" };
        foreach (string path in preserved) Put(root, path, "preserve byte for byte");
        Assert(Uninstall(root) == 0, "Complete uninstall failed.");
        Assert(!Directory.Exists(Path.Combine(root, "DTMAPI")), "Runtime data or backups remained.");
        Assert(!Directory.Exists(Path.Combine(root, "BepInEx")), "Unused BepInEx remained.");
        foreach (string path in new[] { "doorstop_config.ini", "winhttp.dll", ".doorstop_version", "changelog.txt" })
            Assert(!File.Exists(Path.Combine(root, path)), "Loader file remained: " + path);
        foreach (string path in preserved) Assert(File.ReadAllText(Path.Combine(root, path)) == "preserve byte for byte", "Unrelated data changed: " + path);
        Assert(Uninstall(root) == 0 && !Directory.Exists(Path.Combine(root, "DTMAPI")), "Repeat uninstall recreated Runtime data.");
    });

    private static void LegacyEquipmentSurvivesUninstall() => WithUninstallGame(root =>
    {
        const string equipment = "DTMAPI/config/protected-items/equipment-slots/slot-1/equipment-slots-DTMAPI.MoreEquipmentSlotsMod.json.previous";
        Put(root, equipment, "old equipment");
        Put(root, "DTMAPI/config/other.json");
        Put(root, "DTMAPI/components/old.dll");
        Put(root, "BepInEx/plugins/DTMAPI/old.dll");
        Assert(Uninstall(root) == 0, "Legacy equipment prevented uninstall.");
        Assert(File.ReadAllText(Path.Combine(root, equipment)) == "old equipment", "Legacy equipment was changed.");
        Assert(!File.Exists(Path.Combine(root, "DTMAPI/config/other.json")) && !Directory.Exists(Path.Combine(root, "DTMAPI/components")), "Unrelated Runtime data remained.");
        Assert(!Directory.Exists(Path.Combine(root, "BepInEx")), "Legacy equipment kept the loader installed.");
        Assert(Uninstall(root) == 0 && File.Exists(Path.Combine(root, equipment)), "Repeat uninstall removed legacy equipment.");
    });

    private static void UninstallRemovesInterruptedDirectoriesByName() => WithUninstallGame(root =>
    {
        foreach (string prefix in new[] { ".dtmapi-runtime-install-", ".dtmapi-multiplatform-install-",
            ".dtmapi-multiplatform-genesis-", ".dtmapi-multiplatform-bepinex-source-" })
        {
            Put(root, prefix + "interrupted/transaction.json", "broken record");
            Put(root, prefix + "interrupted/unknown.bin");
        }
        Put(root, "DTMAPI/.runtime-install-transaction-old/backup/old.dll");
        Put(root, ".unrelated-directory/keep.txt");
        Assert(Uninstall(root) == 0, "Interrupted installation prevented uninstall.");
        Assert(!Directory.EnumerateDirectories(root, ".dtmapi-*").Any() && !Directory.Exists(Path.Combine(root, "DTMAPI")), "Interrupted files remained.");
        Assert(File.Exists(Path.Combine(root, ".unrelated-directory/keep.txt")), "Unrelated directory was removed.");
    });

    private static void OtherPluginsKeepBepInEx() => WithUninstallGame(root =>
    {
        foreach (string other in new[] { "BepInEx/plugins/Other/other.dll", "BepInEx/patchers/other.dll" })
        {
            string[] preserved = { other, "BepInEx/core/BepInEx.dll", "BepInEx/config/other.cfg", "winhttp.dll", "doorstop_config.ini", ".doorstop_version", "changelog.txt" };
            foreach (string path in preserved) Put(root, path, "keep");
            Put(root, "BepInEx/plugins/DTMAPI/old.dll");
            Put(root, "BepInEx/config/dev.dtmapi.bootstrap.cfg");
            Put(root, "DTMAPI/components/old.dll");
            Assert(Uninstall(root) == 0, "Other plugin blocked Runtime uninstall.");
            Assert(!Directory.Exists(Path.Combine(root, "DTMAPI")) && !Directory.Exists(Path.Combine(root, "BepInEx/plugins/DTMAPI")), "Runtime remained beside another plugin.");
            Assert(!File.Exists(Path.Combine(root, "BepInEx/config/dev.dtmapi.bootstrap.cfg")), "DTMAPI BepInEx configuration remained.");
            foreach (string path in preserved) Assert(File.ReadAllText(Path.Combine(root, path)) == "keep", "Other plugin dependency changed: " + path);
            Directory.Delete(Path.Combine(root, "BepInEx"), true);
        }
    });

    private static void CustomRuntimeIsNotAnUninstallTarget() => WithUninstallGame(root =>
    {
        string custom = Path.Combine(root, "custom-runtime");
        Put(custom, "config/settings.json", "custom data");
        Put(custom, "install-state.json", "old installation record");
        Environment.SetEnvironmentVariable("DTMAPI_RUNTIME_DIR", custom);
        Assert(Uninstall(root) == 0 && File.ReadAllText(Path.Combine(custom, "config/settings.json")) == "custom data", "Custom Runtime directory was used as a target.");
    });

    private static void LockedFileCanBeRemovedOnRetry() => WithUninstallGame(root =>
    {
        if (!OperatingSystem.IsWindows()) return;
        Put(root, "DTMAPI/tools/locked.dll");
        Put(root, "DTMAPI/cache/remove.txt");
        string locked = Path.Combine(root, "DTMAPI/tools/locked.dll");
        using (FileStream stream = new(locked, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            Assert(Uninstall(root) == 3 && File.Exists(locked), "Locked file did not report incomplete cleanup.");
            Assert(!Directory.Exists(Path.Combine(root, "DTMAPI/cache")), "Locked file prevented independent cleanup.");
        }
        Assert(Uninstall(root) == 0 && !Directory.Exists(Path.Combine(root, "DTMAPI")), "Retry did not remove the released file.");
    });
}
