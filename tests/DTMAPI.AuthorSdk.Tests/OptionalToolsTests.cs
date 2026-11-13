using System.Diagnostics;
using System.Text.Json;
using DTMAPI.AuthorSdk;
using DTMAPI.Testing;

namespace DTMAPI.AuthorSdk.Tests;
internal static class OptionalToolsTests
{
    private static async Task<int> Main(string[] args)
    {
        using var session = DtmApiTestSession.Start("OptionalAuthorTools");
        string root = session.RootPath;
        string? oldPersistent = Environment.GetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT"), oldState = Environment.GetEnvironmentVariable("DTMAPI_AUTHOR_STATE_ROOT");
        try
        {
            string project = Path.Combine(root, "independent-project"), references = Path.GetDirectoryName(typeof(DTMAPI.Abstractions.DtmMod).Assembly.Location)!;
            await Run("new", "codemod", project, "--id", "Test.123-mod", "--references", references);
            Assert(!File.Exists(Path.Combine(project, "dtmapi.author.json")), "New projects have no author proof file.");
            await Run("build", project, "-c", "Release");
            string package = Path.Combine(project, "package"), entry = Path.Combine(package, "Content/DTMAPI/Test.123-mod.dll");
            Assert(File.Exists(entry), "Standard MSBuild creates a directly deployable directory.");
            DateTime write = File.GetLastWriteTimeUtc(entry);
            File.WriteAllText(Path.Combine(package, "Unused.dll"), "unrelated non-PE candidate");
            string zip = Path.Combine(root, "package.zip");
            await Run("pack", package, "--output", zip);
            Assert(File.GetLastWriteTimeUtc(entry) == write && !File.Exists(Path.Combine(package, "dtmapi-native-build.json")), "Packing does not rebuild or generate receipts.");
            string game = Path.Combine(root, "game"); Directory.CreateDirectory(game); File.WriteAllText(Path.Combine(game, "DolocTown.exe"), "test lease target");
            Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", Path.Combine(root, "persistent"));
            Environment.SetEnvironmentVariable("DTMAPI_AUTHOR_STATE_ROOT", Path.Combine(root, "state"));
            await Run("deploy", package, "--game-root", game);
            string defaultDirectory = Path.Combine(root, "persistent/MODS/Test.123-mod");
            string installed = Path.GetFullPath(Path.Combine(root, "persistent/MODS/Custom Mod Folder"));
            Directory.Move(defaultDirectory, installed);
            File.WriteAllText(Path.Combine(installed, "user-edited.json"), "keep this actual file");
            File.WriteAllText(Path.Combine(installed, "old-name.dll"), "old renamed output");
            File.WriteAllText(Path.Combine(installed, "settings.json"), "old package setting");
            File.WriteAllText(Path.Combine(installed, "workshop.json"), "existing native workshop identity");
            File.WriteAllText(Path.Combine(package, "settings.json"), "new package setting");
            File.WriteAllText(Path.Combine(package, "workshop.json"), "source workshop identity");
            await Run("pack", package, "--output", zip, "--replace");
            await Run("deploy", zip, "--game-root", game);
            string recovery = Path.Combine(root, "persistent/DTMAPI/author-recovery");
            Assert(File.ReadAllText(Path.Combine(installed, "user-edited.json")) == "keep this actual file" && File.Exists(Path.Combine(installed, "old-name.dll")), "Overlay deliberately leaves files absent from the new package, including renamed DLLs.");
            Assert(File.ReadAllText(Path.Combine(installed, "settings.json")) == "new package setting", "Same-name package settings follow this deployment's payload.");
            Assert(File.ReadAllText(Path.Combine(installed, "workshop.json")) == "existing native workshop identity", "Overlay retains the target's native workshop identity.");
            Assert(!Directory.Exists(recovery) && !Directory.EnumerateFiles(Path.Combine(root, "state"), "Test.123-mod.json", SearchOption.AllDirectories).Any(), "Ordinary deployment creates no whole-directory backup or deployment journal.");
            await Run("deploy", package, "--game-root", game, "--replace");
            Assert(File.Exists(Path.Combine(installed, "old-name.dll")) && !Directory.Exists(recovery), "Legacy replace is overlay confirmation, never clean or an implicit backup.");
            Directory.CreateDirectory(Path.Combine(root, "persistent/SAVE"));
            string enablement = "{\"modInfos\":{\"Local.Custom Mod Folder\":{\"enabled\":true}}}";
            File.WriteAllText(Path.Combine(root, "persistent/SAVE/mod_infos.json"), enablement);
            Directory.CreateDirectory(Path.Combine(game, "DTMAPI/config"));
            File.WriteAllText(Path.Combine(game, "DTMAPI/config/Test.123-mod.json"), "package-external setting");
            File.WriteAllText(Path.Combine(package, "blocked.txt"), "retry payload");
            Directory.CreateDirectory(Path.Combine(installed, "blocked.txt"));
            var failedCopy = await Capture("deploy", package, "--game-root", game, "--json");
            Assert(failedCopy.Code != 0 && failedCopy.Text.Contains("blocked.txt") && failedCopy.Text.Contains("partially overwritten"), "A real file/directory collision reports the failed path and partial overlay, without claiming rollback.");
            Directory.Delete(Path.Combine(installed, "blocked.txt"));
            await Run("deploy", package, "--game-root", game);
            Assert(File.ReadAllText(Path.Combine(installed, "blocked.txt")) == "retry payload", "Retry succeeds once the actual file error is corrected.");
            var invalidTarget = await Capture("deploy", package, "--game-root", game, "--target", Path.GetDirectoryName(installed)!, "--clean");
            Assert(invalidTarget.Code != 0 && invalidTarget.Text.Contains("ordinary child of MODS") && File.Exists(Path.Combine(installed, "settings.json")), "Explicit targeting cannot clear the MODS root itself.");
            if (OperatingSystem.IsWindows())
            {
                string lockedPath = Path.Combine(installed, "locked-old-file.txt");
                File.WriteAllText(lockedPath, "old file retained by an actual open handle");
                using (var held = new FileStream(lockedPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    var failedClean = await Capture("deploy", package, "--game-root", game, "--clean");
                    Assert(failedClean.Code != 0 && failedClean.Text.Contains("not an old-installation backup") && failedClean.Text.Contains("New-package staging"), "Failed clean describes the prepared new package, not recoverable old settings.");
                    Assert(failedClean.Text.Contains("--target \"" + installed + "\""), "Clean retry identifies the original target, including spaces, independently of its manifest.");
                    string stage = Directory.EnumerateDirectories(Path.Combine(root, "persistent/MODS"), ".dtmapi-stage-*").Single();
                    Assert(!stage.StartsWith(installed + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) && File.Exists(Path.Combine(stage, "Content/DTMAPI/Test.123-mod.dll")), "The complete new staging is outside the cleared target and survives failure.");
                    Assert(File.ReadAllText(Path.Combine(stage, "workshop.json")) == "existing native workshop identity", "Workshop identity is copied into staging before clearing starts.");
                    // The failed clean may already have removed old files, including
                    // workshop.json. Retry from the retained prepared package.
                }
                string prepared = Directory.EnumerateDirectories(Path.Combine(root, "persistent/MODS"), ".dtmapi-stage-*").Single();
                // Directory deletion order is unspecified: exercise the failure
                // state where the manifest was already removed before the lock.
                string removedManifest = Path.Combine(installed, "Content/DTMAPI/manifest.json");
                if (File.Exists(removedManifest)) File.Delete(removedManifest);
                await Run("deploy", prepared, "--game-root", game, "--target", installed + Path.DirectorySeparatorChar, "--clean");
            }
            else await Run("deploy", package, "--game-root", game, "--clean");
            Assert(File.Exists(Path.Combine(installed, "Content/DTMAPI/Test.123-mod.dll")) && !Directory.Exists(defaultDirectory), "Retry stays in the original non-ID directory instead of creating another installation.");
            Assert(!File.Exists(Path.Combine(installed, "old-name.dll")) && !File.Exists(Path.Combine(installed, "user-edited.json")), "Only explicit clean removes files absent from the package.");
            Assert(File.ReadAllText(Path.Combine(installed, "workshop.json")) == "existing native workshop identity" && !Directory.Exists(recovery), "Clean retains identity but does not back up the old installation.");
            Assert(File.ReadAllText(Path.Combine(game, "DTMAPI/config/Test.123-mod.json")) == "package-external setting" && File.ReadAllText(Path.Combine(root, "persistent/SAVE/mod_infos.json")) == enablement, "Neither deployment mode changes external settings or official enablement.");
            File.WriteAllText(Path.Combine(installed, "new-local-file.txt"), "new edits");
            var status = new DTMAPI.Authoring.Contracts.CommandReport();
            OfficialLocalPaths.AddStatus(status, Path.GetFileName(installed));
            Assert(status.Values["officialEnabled"] == "true", "Enablement is read from the native SAVE/mod_infos.json.");
            await Run("status", "Test.123-mod", "--game-root", game, "--json");
            string legacyFolder = Path.Combine(root, "persistent/MODS/LegacyOptionalFolder");
            Directory.Move(installed, legacyFolder);
            File.WriteAllText(Path.Combine(root, "persistent/SAVE/mod_infos.json"), "{\"modInfos\":{\"Local.LegacyOptionalFolder\":{\"enabled\":true}}}");
            var legacyStatus = OptionalTools.Status(ParsedCommand.Parse(new[] { "status", "Test.123-mod", "--game-root", game }));
            Assert(legacyStatus.Values["officialEnabled"] == "true", "Status resolves the installed folder before reading its official enablement ID.");
            Directory.Move(legacyFolder, installed);
            await Run("withdraw", "Test.123-mod", "--game-root", game);
            Assert(!Directory.Exists(installed), "Withdraw moves the current directory out of official MODS.");
            await Run("recover", "Test.123-mod", "--game-root", game);
            Assert(File.ReadAllText(Path.Combine(installed, "new-local-file.txt")) == "new edits", "Recovery restores current files without a byte certificate.");
            string traversal = Path.Combine(root, "traversal.zip");
            using (var archive = System.IO.Compression.ZipFile.Open(traversal, System.IO.Compression.ZipArchiveMode.Create))
            using (var writer = new StreamWriter(archive.CreateEntry("../escape.txt").Open())) writer.Write("bad");
            Assert(await Exit("deploy", traversal, "--game-root", game) != 0 && !File.Exists(Path.Combine(root, "escape.txt")), "ZIP paths cannot escape staging.");
            await Run("deploy", package, "--game-root", game);
            var recoveryStatus = OptionalTools.Status(ParsedCommand.Parse(new[] { "status", "Test.123-mod", "--game-root", game }));
            Assert(recoveryStatus.Values["lastRecoveryOperationPhase"] == "recovered" && !recoveryStatus.Values.ContainsKey("operationPhase"), "A later overlay does not turn a historical recovery record into its own transaction state.");
            Console.WriteLine("Optional author tools: PASS (external build, layout-preserving pack, overlay/clean, retained identity, actual failure/retry, explicit withdraw/recovery).");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
        finally { Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", oldPersistent); Environment.SetEnvironmentVariable("DTMAPI_AUTHOR_STATE_ROOT", oldState); }
    }
    private static async Task Run(params string[] args) { int exit = await Exit(args); Assert(exit == 0, string.Join(" ", args) + ": exit=" + exit); }
    private static async Task<int> Exit(params string[] args) { using var output = new StringWriter(); using var error = new StringWriter(); int exit = await AuthorApplication.RunAsync(args, output, error); if (exit != 0) Console.WriteLine(output.ToString() + error); return exit; }
    private static async Task<(int Code, string Text)> Capture(params string[] args) { using var output = new StringWriter(); using var error = new StringWriter(); int code = await AuthorApplication.RunAsync(args, output, error); return (code, output.ToString() + error); }
    private static void Assert(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
