using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DTMAPI.MultiPlatformInstaller.Tests;

internal static partial class Program
{
    private static void DiagnosticActionsDoNotRequireAnIntactPackage()
    {
        string root = CreateManagedTestDirectory();
        try
        {
            string game = Path.Combine(root, "game");
            string package = Path.Combine(root, "package");
            string output = Path.Combine(root, "output");
            Directory.CreateDirectory(Path.Combine(game, "DTMAPI", "logs"));
            File.WriteAllText(Path.Combine(game, "DolocTown.exe"), "private fake game");
            File.WriteAllText(Path.Combine(game, "DTMAPI", "logs", "scope.log"), "scope-fixture-log");

            (int code, string text) = InvokeInstaller("collect-logs", game, package, output);
            Assert(code == 0 && text.Contains("DTM-S4001"), "Missing package prevented log export: " + text);
            string bundle = Directory.GetDirectories(output).Single();
            Assert(File.ReadAllText(Path.Combine(bundle, "status.txt")).Contains("PACKAGE_UNAVAILABLE"), "Missing package was not recorded.");
            Assert(Directory.GetFiles(Path.Combine(bundle, "DTMAPI")).Any(file => File.ReadAllText(file) == "scope-fixture-log"), "Available game log was not collected.");

            string owned = Path.Combine(game, "BepInEx", "plugins", "DTMAPI", "DTMAPI.Core.dll");
            string unknown = Path.Combine(game, "BepInEx", "plugins", "Other", "keep-unknown.txt");
            string commonModule = Path.Combine(game, "DTMAPI", "tools", "common", "files.ps1");
            Directory.CreateDirectory(Path.GetDirectoryName(owned)!);
            Directory.CreateDirectory(Path.GetDirectoryName(unknown)!);
            Directory.CreateDirectory(Path.GetDirectoryName(commonModule)!);
            File.WriteAllText(owned, "owned-runtime-fixture");
            File.WriteAllText(unknown, "unowned-file");
            File.WriteAllText(commonModule, "owned-common-module-fixture");
            (code, text) = InvokeInstaller("uninstall", game, package, output);
            Assert(code == 0 && !File.Exists(owned), "Missing package blocked Runtime uninstall: " + text);
            Assert(!Directory.Exists(Path.Combine(game, "DTMAPI", "tools")), "Uninstall left empty common/tools directories.");
            Assert(File.ReadAllText(unknown) == "unowned-file", "Uninstall changed third-party files.");
            Assert(!Directory.Exists(Path.Combine(game, "DTMAPI")), "Uninstall retained logs/backups or recreated state.");
            Directory.CreateDirectory(Path.Combine(game, "DTMAPI", "logs"));
            File.WriteAllText(Path.Combine(game, "DTMAPI", "logs", "scope.log"), "scope-fixture-log");

            string releasePath = WriteDiagnosticPackage(package);
            (code, text) = InvokeInstaller("status", game, package, output);
            Assert(code == 1 && text.Contains("Files:") && !text.Contains("PACKAGE_UNAVAILABLE"), "Missing unused hosts/runtime source blocked status: " + text);

            // The installer still rejects consumed input before touching the game.
            string[] before = Directory.GetFiles(game, "*", SearchOption.AllDirectories);
            (code, text) = InvokeInstaller("install", game, package, output);
            Assert(code == 3 && text.Contains("DTM-E1201"), "Missing actual install input was accepted: " + text);
            Assert(before.SequenceEqual(Directory.GetFiles(game, "*", SearchOption.AllDirectories)), "Rejected install changed the game tree.");

            string pending = Path.Combine(game, ".dtmapi-runtime-install-invalid");
            Directory.CreateDirectory(pending);
            File.WriteAllText(Path.Combine(pending, "keep.txt"), "unowned transaction state");
            (code, text) = InvokeInstaller("uninstall", game, Path.Combine(root, "missing-package"), output);
            Assert(code == 3 && text.Contains("DTM-E2001") && File.Exists(Path.Combine(pending, "keep.txt")), "Uninstall bypassed transaction ownership: " + text);
            Directory.CreateDirectory(Path.Combine(game, "DTMAPI", "logs"));
            File.WriteAllText(Path.Combine(game, "DTMAPI", "logs", "scope.log"), "scope-fixture-log");
            Assert(File.Exists(Path.Combine(pending, "keep.txt")), "Uninstall removed unowned transaction state.");

            // An exception during optional status generation must not discard collected logs.
            JsonObject release = JsonNode.Parse(File.ReadAllText(releasePath))!.AsObject();
            release["IncludedAssemblies"] = null;
            File.WriteAllText(releasePath, release.ToJsonString());
            (code, text) = InvokeInstaller("collect-logs", game, package, output);
            Assert(code == 0, "Status exception discarded the collection: " + text);
            string second = Directory.GetDirectories(output).Single(path => path != bundle);
            Assert(File.ReadAllText(Path.Combine(second, "status.txt")).Contains("STATUS_UNAVAILABLE"), "Status exception was not recorded.");
            Assert(Directory.GetFiles(Path.Combine(second, "DTMAPI")).Any(file => File.ReadAllText(file) == "scope-fixture-log"), "Status exception lost available logs.");
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, recursive: true); }
    }

    private static (int Code, string Text) InvokeInstaller(string action, string game, string package, string output)
    {
        MethodInfo main = PackageLayoutType().Assembly.GetType("DTMAPI.MultiPlatformInstaller.Program", true)!
            .GetMethod("Main", BindingFlags.Static | BindingFlags.NonPublic)!;
        TextWriter previous = Console.Out;
        using var captured = new StringWriter();
        try
        {
            Console.SetOut(captured);
            int code = (int)main.Invoke(null, new object[] { new[] { action, "--game-path", game,
                "--package-root", package, "--output", output, "--non-interactive" } })!;
            return (code, captured.ToString());
        }
        finally { Console.SetOut(previous); }
    }

    private static string WriteDiagnosticPackage(string root)
    {
        string installer = Path.Combine(root, "Content", "DTMAPIInstaller");
        Directory.CreateDirectory(Path.Combine(installer, "tools"));
        foreach (string file in new[] { "common.ps1", "release-common.ps1", "uninstall-dtmapi.ps1",
            "check-dtmapi-status.ps1", "collect-logs.ps1", "analyze-startup-evidence.ps1", "dtmapi-runtime-version.props" })
            File.WriteAllText(Path.Combine(installer, "tools", file), "fixture");
        string branding = Path.Combine(installer, "Payload", "BepInEx", "plugins", "DTMAPI", "assets", "branding");
        Directory.CreateDirectory(branding);
        File.WriteAllText(Path.Combine(branding, "dtmapi-icon.png"), "fixture");
        string release = Path.Combine(root, "Content", "DTMAPI", "release-manifest.json");
        Directory.CreateDirectory(Path.GetDirectoryName(release)!);
        File.WriteAllText(release, JsonSerializer.Serialize(new
        {
            SchemaVersion = 1, DTMAPIVersion = "0.6.1", BinaryVersion = "0.6.1.0",
            BuildCommit = new string('a', 40), PackageKind = "workshop-runtime",
            IncludedAssemblies = new[] { "DTMAPI.Abstractions.dll", "DTMAPI.BepInExBootstrap.dll", "DTMAPI.Core.dll",
                "DTMAPI.GameBridge.DolocTown.dll", "DTMAPI.ModConfigMenu.dll" }.Select(name => new
                { FileName = name, Length = 10, Sha256 = new string('a', 64), FileVersion = "0.6.1.0" }),
            OptionalComponents = new[] { new { ComponentId = "gamebridge-compatibility-host",
                RelativePath = "DTMAPI/components/compatibility/DTMAPI.GameBridge.DolocTown.Compatibility.dll",
                Length = 10, Sha256 = new string('a', 64), FileVersion = "0.6.1.0" } }
        }));
        File.WriteAllText(Path.Combine(installer, "multiplatform-package.json"), JsonSerializer.Serialize(new
        {
            SchemaVersion = 1, WorkshopId = "3792681186", RuntimeVersion = "0.6.1",
            DistributionId = "dtmapi-multiplatform", SourceWorkshopId = "3743016467",
            SourceWorkshopManifestId = "918505309011394484",
            SourcePlayerPayloadTreeSha256 = "b4ec6a441b4930b5174d4caed8799748fb4e4701ac0d8e72fc6bf6bd48eee4aa",
            BepInExArchive = new { RelativePath = "Content/.tools/bepinex/BepInEx_win_x64_5.4.23.5.zip" },
            HostArtifacts = new[] { new { Rid = "win-x64", RelativePath = "missing.exe", Length = 10, Sha256 = new string('a', 64) },
                new { Rid = "linux-x64", RelativePath = "missing-elf", Length = 10, Sha256 = new string('a', 64) } },
            RequiredLinuxLaunchOption = "WINEDLLOVERRIDES=\"winhttp=n,b\" %command%"
        }));
        return release;
    }
}
