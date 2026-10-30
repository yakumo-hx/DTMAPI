using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

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

    private static int Uninstall(string root, bool confirm = false)
    {
        Assembly asm = PackageLayoutType().Assembly;
        object platform = Activator.CreateInstance(asm.GetType("DTMAPI.MultiPlatformInstaller.PlatformEnvironment", true)!)!;
        object result = asm.GetType("DTMAPI.MultiPlatformInstaller.UninstallAction", true)!.GetMethod("Run")!
            .Invoke(null, new object[] { root, platform, confirm, true })!;
        return (int)result.GetType().GetProperty("ExitCode")!.GetValue(result)!;
    }

    private static void CompleteUninstallRemovesOwnedContentAndIsRepeatable() => WithUninstallGame(root =>
    {
        foreach (string path in new[] { "BepInEx/plugins/DTMAPI/old.dll", "BepInEx/core/BepInEx.dll", "BepInEx/core/BepInEx.xml", "BepInEx/config/BepInEx.cfg", "BepInEx/cache/preload.dat",
                     "DTMAPI/components/audio/old.dll", "DTMAPI/cache/audio/v1/a.wav", "DTMAPI/cache/DTMAPI.Audio/v1/a.wav", "DTMAPI/logs/latest.log", "DTMAPI/config/settings.json",
                     "DTMAPI/.bepinex-install-20261028-010101-001-1234abcd/extract-1/core.dll", ".dtmapi-multiplatform-bepinex-source-0123456789abcdef0123456789abcdef/bepinex-extract/core.dll" }) Put(root, path);
        Put(root, "DTMAPI/install-state.json", "corrupt old receipt");
        Put(root, "doorstop_config.ini", "target_assembly=BepInEx/core/BepInEx.Preloader.dll\n");
        Put(root, "winhttp.dll", "UnityDoorstop fixture");
        Put(root, ".doorstop_version", "4.3.0");
        Put(root, "changelog.txt", "BepInEx changelog");
        string[] preserved = { "MODS/OtherMod/content.json", "SAVE/slot.dat", "SAVE/mod_infos.json", "BepInEx/plugins/other.dll", "BepInEx/patchers/other.dll", "BepInEx/config/other.cfg" };
        foreach (string path in preserved) Put(root, path, "preserve byte for byte");
        Assert(Uninstall(root) == 0, "Complete uninstall failed.");
        Assert(!Directory.Exists(Path.Combine(root, "DTMAPI")), "Uninstall recreated/retained Runtime state.");
        Assert(!File.Exists(Path.Combine(root, "winhttp.dll")), "Doorstop remained.");
        Assert(!Directory.Exists(Path.Combine(root, "BepInEx/core")), "BepInEx core metadata remained.");
        Assert(!Directory.EnumerateDirectories(root, ".dtmapi-*").Any(), "BepInEx source scratch remained.");
        foreach (string path in preserved) Assert(File.ReadAllText(Path.Combine(root, path)) == "preserve byte for byte", "Unrelated data changed: " + path);
        Assert(Uninstall(root) == 0 && !Directory.Exists(Path.Combine(root, "DTMAPI")), "Repeat uninstall was not a clean no-op.");
    });

    private static void LegacyEquipmentRequiresExplicitConsent() => WithUninstallGame(root =>
    {
        Put(root, "BepInEx/plugins/DTMAPI/old.dll");
        Put(root, "DTMAPI/config/protected-items/equipment-slots/slot-1/equipment-slots-DTMAPI.MoreEquipmentSlotsMod.json.previous");
        var before = Directory.GetFiles(root, "*", SearchOption.AllDirectories).ToDictionary(p => p, File.ReadAllText);
        Assert(Uninstall(root) == 2, "Unattended cleanup must not accept equipment loss.");
        Assert(Directory.GetFiles(root, "*", SearchOption.AllDirectories).Length == before.Count && before.All(p => File.ReadAllText(p.Key) == p.Value), "Cancellation changed game files.");
        Assert(Uninstall(root, true) == 0 && !Directory.Exists(Path.Combine(root, "DTMAPI")), "Explicit equipment deletion did not complete.");
        const string txn = ".dtmapi-runtime-install-20261028-010101-001-1234abcd";
        Put(root, txn + "/transaction.json", JsonSerializer.Serialize(new
        {
            SchemaVersion = 1, Engine = "DTMAPI.MultiPlatform/uninstall", Operations = new[] { new
            { TargetRelativePath = "DTMAPI/config/equipment-slots-DTMAPI.MoreEquipmentSlotsMod.json", CandidateRelativePath = "", BackupRelativePath = "backup/0000.bin" } }
        }));
        Put(root, txn + "/backup/0000.bin", "equipment data under an opaque transaction filename");
        Assert(Uninstall(root) == 2 && File.Exists(Path.Combine(root, txn, "backup/0000.bin")), "Transaction mapping bypassed equipment consent.");
        Assert(Uninstall(root, true) == 0 && !Directory.Exists(Path.Combine(root, txn)), "Confirmed transaction equipment was not deleted.");
    });

    private static void UninstallTerminatesBothTransactionFamilies() => WithUninstallGame(root =>
    {
        const string stamp = "20261028-010101-001-1234abcd";
        string ps = ".dtmapi-runtime-install-" + stamp;
        Put(root, ps + "/transaction.json", "{\"SchemaVersion\":1,\"CandidatePlugin\":\"obsolete absolute path\",\"RecoveryPlugin\":\"obsolete absolute path\"}");
        Put(root, ps + "/recovery-plugin/old.dll");
        Put(root, "DTMAPI/.runtime-install-transaction-" + stamp + "/recovery/tools/old.ps1");
        Put(root, ".dtmapi-runtime-install-20261028-010102-001-1234abcd/transaction.json", JsonSerializer.Serialize(new
        {
            SchemaVersion = 1, Engine = "DTMAPI.MultiPlatform/install", Operations = new[] { new
            { TargetRelativePath = "BepInEx/plugins/DTMAPI/old.dll", CandidateRelativePath = "candidate/0000.bin", BackupRelativePath = "backup/0000.bin" } }
        }));
        Put(root, ".dtmapi-runtime-install-20261028-010102-001-1234abcd/candidate/0000.bin");
        Put(root, ".dtmapi-runtime-install-20261028-010102-001-1234abcd/backup/0000.bin");
        Assert(Uninstall(root) == 0, "Known transactions could not be terminated without an old package.");
        Assert(!Directory.EnumerateDirectories(root, ".dtmapi-*").Any() && !Directory.Exists(Path.Combine(root, "DTMAPI")), "A replayable transaction remained.");
    });

    private static void UninstallRestoresForeignOriginalAndPreservesAmbiguousBackup() => WithUninstallGame(root =>
    {
        Put(root, "winhttp.dll", "UnityDoorstop current");
        Put(root, "doorstop_config.ini", "target_assembly=BepInEx/core/BepInEx.Preloader.dll");
        Put(root, "DTMAPI/backups/bepinex-install-old/winhttp.dll", "other injector original");
        Assert(Uninstall(root) == 0, "Foreign original restoration failed.");
        Assert(File.ReadAllText(Path.Combine(root, "winhttp.dll")) == "other injector original", "Foreign original was deleted.");
        Put(root, "winhttp.dll", "UnityDoorstop current");
        Put(root, "doorstop_config.ini", "target_assembly=BepInEx/core/BepInEx.Preloader.dll");
        Put(root, "DTMAPI/backups/bepinex-install-doorstop/winhttp.dll", "UnityDoorstop original for another loader");
        Put(root, "DTMAPI/backups/bepinex-install-doorstop/doorstop_config.ini", "target_assembly=OtherLoader.dll");
        Assert(Uninstall(root) == 0, "Foreign Doorstop restoration failed.");
        Assert(File.ReadAllText(Path.Combine(root, "winhttp.dll")) == "UnityDoorstop original for another loader"
            && File.ReadAllText(Path.Combine(root, "doorstop_config.ini")) == "target_assembly=OtherLoader.dll", "Foreign Doorstop files were deleted as framework files.");
        Put(root, "DTMAPI/backups/uninstall-old/unknown/player.dat", "unmapped user data");
        Put(root, "DTMAPI/cache/audio/v1/a.wav");
        Assert(Uninstall(root) == 3, "Ambiguous backup was falsely reported as fully removed.");
        Assert(File.ReadAllText(Path.Combine(root, "DTMAPI/backups/uninstall-old/unknown/player.dat")) == "unmapped user data", "Unknown backup was deleted.");
        Assert(!Directory.Exists(Path.Combine(root, "DTMAPI/cache/audio")), "An ambiguous backup blocked independent cleanup.");
    });

    private static void CustomRuntimeRequiresOwnership() => WithUninstallGame(root =>
    {
        string custom = Path.Combine(root, "custom-runtime");
        Put(custom, "config/settings.json", "custom data");
        Environment.SetEnvironmentVariable("DTMAPI_RUNTIME_DIR", custom);
        Assert(Uninstall(root) == 3 && File.Exists(Path.Combine(custom, "config/settings.json")), "Unproven custom directory was deleted.");
        Put(custom, "install-state.json", JsonSerializer.Serialize(new { GameDir = root }));
        Assert(Uninstall(root) == 0 && !Directory.Exists(custom), "Owned custom Runtime was not cleaned.");
    });
}
