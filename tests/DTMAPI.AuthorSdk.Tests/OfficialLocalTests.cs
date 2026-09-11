using DTMAPI.AuthorSdk;
using DTMAPI.Authoring.Contracts;
using System.Text.Json;
using System.Diagnostics;

namespace DTMAPI.AuthorSdk.Tests;

internal static partial class Program
{
    private static async Task TestOfficialLocalTransactions(string temp, string compatibility)
    {
        string? previous = Environment.GetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT");
        string persistent = Path.Combine(temp, "official profile");
        Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", persistent);
        try
        {
            string game = NewGameRoot(temp, "official game");
            string executable = Path.Combine(game, "DolocTown.exe");
            File.Copy(Environment.ProcessPath!, executable, true);
            using (var lease = ColdGameMutationLease.Acquire(game))
            {
                try
                {
                    using var launched = Process.Start(new ProcessStartInfo(executable, "--info") { UseShellExecute = false, CreateNoWindow = true });
                    launched?.WaitForExit(10000);
                    throw new InvalidOperationException("Executable lease did not block launch.");
                }
                catch (System.ComponentModel.Win32Exception ex) { True(ex.NativeErrorCode == 32, "Launch was denied by sharing violation, not a missing dependency."); }
            }
            string project = Path.Combine(temp, "official stranger");
            const string id = "Unregistered.AuthorJourney";
            await ExpectSuccess("new official stranger", "new", "codemod", "--api-target", "0.5.5", project, "--id", id, "--name", "Stranger", "--author", "Independent");
            await ExpectSuccess("restore official stranger", "restore", project);
            CommandReport first = await ExpectSuccess("pack official stranger", "pack", project, "--compatibility-root", compatibility);
            async Task<CommandReport> Public(bool success, params string[] args)
            {
                var result = await RunPublic(args);
                True(result.Report.Success == success && (result.ExitCode == 0) == success, "Public " + args[0] + ": " + JsonSerializer.Serialize(result.Report));
                return result.Report;
            }
            await Public(false, "install-local", first.OutputPath, "--game-root", game, "--expected-unique-id", id, "--expected-version", "wrong", "--expected-package-sha256", first.Sha256);
            CommandReport installed = await Public(true, "install-local", first.OutputPath, "--game-root", game, "--expected-unique-id", id, "--expected-version", "0.1.0", "--expected-package-sha256", first.Sha256);
            string destination = Path.Combine(persistent, "MODS", id);
            Equal(destination, installed.OutputPath, "Official destination derives from arbitrary author ID");
            using (JsonDocument info = JsonDocument.Parse(File.ReadAllText(Path.Combine(destination, "info.json"))))
                foreach (string field in new[] { "localized_name", "localized_description" })
                    foreach (string locale in new[] { "schinese", "tchinese", "english" })
                        True(info.RootElement.GetProperty(field).GetProperty(locale).ValueKind == JsonValueKind.String, "Native discovery must not migrate an incomplete localization field.");
            True(!Directory.Exists(Path.Combine(game, "Mods")) && !File.Exists(Path.Combine(persistent, "SAVE", "mod_infos.json")), "SDK neither creates legacy Mods nor enables official Mod");
            var scanner = new DTMAPI.Core.Manifesting.ModScanner(new DTMAPI.Core.Runtime.RuntimePaths(game, game));
            var discovered = scanner.Discover().Single(mod => mod.Manifest.UniqueID == id);
            Equal("Local." + id, discovered.OfficialId, "Real Runtime scanner sees identical official ID");
            await Public(true, "install-local-status", id, "--game-root", game, "--expected-version", "0.1.0", "--expected-package-sha256", first.Sha256);

            MutateJson(Path.Combine(project, "manifest.json"), json => json["Version"] = "0.2.0");
            string projectFile = Directory.GetFiles(project, "*.csproj").Single();
            File.WriteAllText(projectFile, File.ReadAllText(projectFile).Replace("<Version>0.1.0</Version>", "<Version>0.2.0</Version>"));
            CommandReport second = await ExpectSuccess("pack second", "pack", project, "--compatibility-root", compatibility);
            string dll = Path.Combine(destination, "Content", "DTMAPI", id + ".dll");
            string original = Sha256(dll);
            foreach (string point in new[] { "update.prepare.before-journal", "update.recovery.after-move", "update.publish.after-move", "update.commit.before-journal" })
            {
                Environment.SetEnvironmentVariable("DTMAPI_AUTHOR_FAULT", "crash:" + point);
                try { await Public(false, "update", second.OutputPath, "--game-root", game); }
                finally { Environment.SetEnvironmentVariable("DTMAPI_AUTHOR_FAULT", null); }
                await Public(true, "recover", id, "--game-root", game);
                await Public(true, "recover", id, "--game-root", game);
                Equal(original, Sha256(dll), "Recovery restores original official bytes after " + point);
            }
            using (var competing = new FileStream(executable, FileMode.Open, FileAccess.Read, FileShare.None))
                await Public(false, "update", second.OutputPath, "--game-root", game);
            Environment.SetEnvironmentVariable("DTMAPI_AUTHOR_FAULT", "fail:update.publish.after-move");
            try
            {
                CommandReport failed = await Public(false, "update", second.OutputPath, "--game-root", game);
                Equal("restored-last-committed", failed.Values["rollback"], "Automatic rollback retains the existing lock instead of reacquiring it");
            }
            finally { Environment.SetEnvironmentVariable("DTMAPI_AUTHOR_FAULT", null); }
            Equal(original, Sha256(dll), "Launch/lease conflict preserves installed bytes");
            string unknown = Path.Combine(destination, "unknown.txt");
            File.WriteAllText(unknown, "not SDK owned");
            await Public(false, "withdraw", id, "--game-root", game);
            True(File.ReadAllText(unknown) == "not SDK owned", "Unknown assets preserved");
            File.Delete(unknown);
            await Public(true, "update", second.OutputPath, "--game-root", game);
            True(original != Sha256(dll), "Update replaces disk bytes only after cold guard");
            await Public(true, "withdraw", id, "--game-root", game);
            True(!Directory.Exists(destination), "Public withdraw removes only owned tree");
        }
        finally { Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", previous); }
    }
}
