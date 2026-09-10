using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using DTMAPI.Authoring.Contracts;
using DTMAPI.AuthorSdk;

namespace DTMAPI.AuthorSdk.Tests;

internal static partial class Program
{
    private static async Task TestPlatformSdkTargets(string temp, string compatibility, string repository)
    {
        TestSynchronousPlatformCallbacksUseResolvedSymbols();
        string root = Path.Combine(temp, "platform-sdk-targets");
        Directory.CreateDirectory(root);
        string frozenRoot = Path.Combine(repository, "author-sdk", "compatibility", "0.5.5");
        Dictionary<string, string> frozenBefore = Directory.EnumerateFiles(frozenRoot, "*", SearchOption.AllDirectories)
            .ToDictionary(path => path, Sha256, StringComparer.Ordinal);
        string payloadBefore = AuthorFileTreeDigest.Compute(compatibility);

        foreach (string kind in new[] { "codemod", "contentpack" })
        {
            foreach (bool explicitTarget in new[] { false, true })
            {
                string name = kind + (explicitTarget ? "-explicit" : "-default");
                string project = Path.Combine(root, name);
                var arguments = new List<string>
                {
                    "new", kind, project, "--id", "Tests.Target" + kind,
                    "--name", "Target selection", "--author", "Tests"
                };
                if (explicitTarget)
                    arguments.AddRange(new[] { "--api-target", "0.5.5" });
                await ExpectSuccess(name, arguments.ToArray()).ConfigureAwait(false);
                JsonObject author = ReadTargetAuthor(project);
                True(author["schemaVersion"]!.GetValue<int>() == (explicitTarget ? 2 : 3), "Explicit old targets retain schema 2; the new target selects dependency schema 3.");
                Equal(explicitTarget ? "0.5.5" : "0.7.0", author["targetDtmApiVersion"]!.GetValue<string>(), "Default and explicit targets select their own frozen API.");
                True(!author.ContainsKey("targetRuntimeVersion"), "Schema 2 does not write the legacy target field.");
                await ExpectSuccess("validate " + name, "validate", project).ConfigureAwait(false);
                AssertJsonMatchesSchema(JsonSerializer.SerializeToElement(author),
                    Path.Combine(repository, "author-sdk", "schemas", "dtmapi-author.schema.json"), name);
            }
        }

        string codeProject = Path.Combine(root, "codemod-explicit");
        string currentCompatibility = Path.Combine(repository, ".tools", "author-sdk-compatibility", "0.7.0");
        string currentCode = Path.Combine(root, "codemod-default");
        await AssertTargetPackage(currentCode, currentCompatibility, Path.Combine(root, "current-code-package"), "0.7.0", "0.7.0").ConfigureAwait(false);
        await AssertTargetPackage(Path.Combine(root, "contentpack-default"), currentCompatibility,
            Path.Combine(root, "current-content-package"), "0.7.0", "0.7.0").ConfigureAwait(false);
        await AssertTargetPayloadRejected(currentCode, compatibility, "new target cannot use old payload").ConfigureAwait(false);
        string contentProject = Path.Combine(root, "contentpack-explicit");
        CommandReport build = await ExpectSuccess("explicit frozen target build", "build", codeProject,
            "--compatibility-root", compatibility).ConfigureAwait(false);
        True(File.Exists(build.OutputPath), "Selected target compiles a real CodeMod DLL.");
        Equal("netstandard2.0", build.Values["targetFramework"], "Selected API still targets Unity Mono's netstandard2.0.");
        JsonObject frozenContract = JsonNode.Parse(File.ReadAllText(Path.Combine(frozenRoot, "compatibility.contract.json")))!.AsObject();
        Equal(frozenContract["abstractionsSha256"]!.GetValue<string>(), build.Values["abstractionsSha256"],
            "Compilation uses the exact frozen Abstractions bytes, independent of the installed Runtime.");
        var packages = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string project in new[] { codeProject, contentProject })
            packages.Add(project, await AssertTargetPackage(project, compatibility,
                Path.Combine(root, Path.GetFileName(project) + "-packages"), "0.5.5").ConfigureAwait(false));

        await TestLegacySdkTargetProject(root, codeProject, compatibility).ConfigureAwait(false);
        await TestUnsupportedSdkTargets(root, codeProject, contentProject, compatibility).ConfigureAwait(false);
        await TestSdkTargetMinimumRuntime(root, codeProject, compatibility).ConfigureAwait(false);
        await TestReadableHistoricalSdkTargetPackages(root, packages).ConfigureAwait(false);
        await TestSdkTargetPayloadBinding(root, codeProject, compatibility).ConfigureAwait(false);
        await TestSdkSessionTargetSelection(root).ConfigureAwait(false);

        Equal(payloadBefore, AuthorFileTreeDigest.Compute(compatibility), "Target selection and rejection leave the reusable frozen payload unchanged.");
        True(frozenBefore.Keys.OrderBy(path => path, StringComparer.Ordinal).SequenceEqual(
            Directory.EnumerateFiles(frozenRoot, "*", SearchOption.AllDirectories).OrderBy(path => path, StringComparer.Ordinal), StringComparer.Ordinal),
            "Target tests do not add or remove frozen compatibility contract files.");
        foreach (KeyValuePair<string, string> file in frozenBefore)
            Equal(file.Value, Sha256(file.Key), "Frozen compatibility contract remains byte-exact: " + Path.GetFileName(file.Key));
        Console.WriteLine("SDK target matrix: OK (CLI selection, schema 1/2, real build/pack, Runtime floors, frozen payload binding).");
    }

    private static async Task<string> AssertTargetPackage(string project, string compatibility, string output, string minimumRuntime, string apiTarget = "0.5.5")
    {
        CommandReport packed = await ExpectSuccess("selected target package", "pack", project,
            "--compatibility-root", compatibility, "--output", output).ConfigureAwait(false);
        using ZipArchive archive = ZipFile.OpenRead(packed.OutputPath);
        JsonObject marker = JsonNode.Parse(ReadZipText(archive, "Content/DTMAPI/dtmapi-package.json"))!.AsObject();
        JsonObject manifest = JsonNode.Parse(ReadZipText(archive, "Content/DTMAPI/manifest.json"))!.AsObject();
        True(marker["schemaVersion"]!.GetValue<int>() == (apiTarget is "0.6.3" or "0.6.4" or "0.6.5" or "0.7.0" ? 3 : 2), "Package marker follows the selected dependency contract.");
        Equal(apiTarget, marker["targetDtmApiVersion"]!.GetValue<string>(), "The package marker retains the selected compilation target.");
        Equal(minimumRuntime, manifest["MinimumDTMApiVersion"]!.GetValue<string>(), "The package preserves its independent minimum Runtime.");
        True(!marker.ContainsKey("targetRuntimeVersion"), "Current marker does not revive the legacy target field.");
        if (marker["packageKind"]!.GetValue<string>() == "CodeMod")
            True(archive.GetEntry(marker["entryDllPath"]!.GetValue<string>()) != null, "Target marker binds an actually built entry DLL.");
        else
            True(archive.Entries.All(entry => !entry.FullName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)), "ContentPack target selection never adds a code payload.");
        return packed.OutputPath;
    }

    private static async Task TestLegacySdkTargetProject(string root, string codeProject, string compatibility)
    {
        string legacy = Path.Combine(root, "legacy-schema1");
        CopyDirectory(codeProject, legacy);
        MutateJson(Path.Combine(legacy, "dtmapi.author.json"), author =>
        {
            author["schemaVersion"] = 1;
            author["targetRuntimeVersion"] = "0.5.5";
            author.Remove("targetDtmApiVersion");
            author.Remove("codeModKind");
        });
        string authorHash = Sha256(Path.Combine(legacy, "dtmapi.author.json"));
        await ExpectSuccess("legacy schema 1 target", "validate", legacy).ConfigureAwait(false);
        await AssertTargetPackage(legacy, compatibility, Path.Combine(root, "legacy-package"), "0.5.5").ConfigureAwait(false);
        Equal(authorHash, Sha256(Path.Combine(legacy, "dtmapi.author.json")), "Reading and packaging schema 1 does not silently rewrite the author's project.");

        MutateJson(Path.Combine(legacy, "dtmapi.author.json"), author => author["targetRuntimeVersion"] = "0.6.1");
        HasCode(await ExpectFailure("schema 1 cannot select a release as its API target", "validate", legacy).ConfigureAwait(false), "SDK108", "Legacy target remains frozen");
        MutateJson(Path.Combine(legacy, "dtmapi.author.json"), author =>
        {
            author["targetRuntimeVersion"] = "0.5.5";
            author["targetDtmApiVersion"] = "0.5.5";
        });
        HasCode(await ExpectFailure("schema 1 rejects mixed target fields", "validate", legacy).ConfigureAwait(false), "SDK108", "Legacy schema rejects a second target authority");

        string mixedSchema2 = Path.Combine(root, "schema2-mixed-target-fields");
        CopyDirectory(codeProject, mixedSchema2);
        MutateJson(Path.Combine(mixedSchema2, "dtmapi.author.json"), author => author["targetRuntimeVersion"] = "0.5.5");
        HasCode(await ExpectFailure("schema 2 rejects legacy target field", "validate", mixedSchema2).ConfigureAwait(false), "SDK108", "Current schema has exactly one target authority");
    }

    private static async Task TestUnsupportedSdkTargets(string root, string codeProject, string contentProject, string compatibility)
    {
        foreach (string target in new[] { "0.8.0", "0.6.1", "99.0.0" })
        {
            foreach (string kind in new[] { "codemod", "contentpack" })
            {
                string destination = Path.Combine(root, "new-rejected-" + kind + "-" + target);
                CommandReport refused = await ExpectUsageFailure("new rejects unavailable API " + target, target,
                    "new", kind, destination, "--id", "Tests.Unavailable", "--name", "Unavailable target", "--author", "Tests", "--api-target", target).ConfigureAwait(false);
                True(refused.Diagnostics.Any(diagnostic => diagnostic.Message.Contains(target, StringComparison.Ordinal)), "Target rejection identifies the requested API: " + target);
                HasCode(refused, "SDK001", "Unavailable API selection is an ordinary CLI input error");
                True(refused.Diagnostics.All(diagnostic => diagnostic.Code != "SDK999"), "Unavailable API selection is not an internal SDK failure.");
                True(!Directory.Exists(destination), "Rejected targets do not create project directories.");

                string project = Path.Combine(root, "existing-rejected-" + kind + "-" + target);
                CopyDirectory(kind == "codemod" ? codeProject : contentProject, project);
                MutateJson(Path.Combine(project, "dtmapi.author.json"), author => author["targetDtmApiVersion"] = target);
                await AssertTargetProjectRejected(project, compatibility, target).ConfigureAwait(false);
            }
        }
    }

    private static async Task TestSdkTargetMinimumRuntime(string root, string codeProject, string compatibility)
    {
        foreach (string minimum in new[] { "0.5.4", "0.6.3" })
        {
            string project = Path.Combine(root, "invalid-minimum-" + minimum);
            CopyDirectory(codeProject, project);
            MutateJson(Path.Combine(project, "manifest.json"), manifest => manifest["MinimumDTMApiVersion"] = minimum);
            await AssertTargetProjectRejected(project, compatibility, minimum).ConfigureAwait(false);
        }
        string currentRuntime = Path.Combine(root, "minimum-current-runtime");
        CopyDirectory(codeProject, currentRuntime);
        MutateJson(Path.Combine(currentRuntime, "manifest.json"), manifest => manifest["MinimumDTMApiVersion"] = "0.6.1");
        await ExpectSuccess("API 0.5.5 may require released Runtime 0.6.1", "validate", currentRuntime).ConfigureAwait(false);
        await AssertTargetPackage(currentRuntime, compatibility, Path.Combine(root, "current-runtime-package"), "0.6.1").ConfigureAwait(false);
    }

    private static async Task AssertTargetProjectRejected(string project, string compatibility, string rejectedVersion)
    {
        string projectBefore = AuthorFileTreeDigest.Compute(project);
        foreach (string command in new[] { "validate", "build", "pack" })
        {
            string output = Path.Combine(project, "rejected-" + command);
            string[] arguments = command == "validate"
                ? new[] { command, project }
                : new[] { command, project, "--compatibility-root", compatibility, "--output", output };
            CommandReport failure = await ExpectFailure(command + " rejects " + rejectedVersion, arguments).ConfigureAwait(false);
            True(failure.Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error && diagnostic.Message.Contains(rejectedVersion, StringComparison.Ordinal)),
                command + " explains the rejected target or Runtime floor " + rejectedVersion);
            True(!Directory.Exists(output) && !File.Exists(output), "Invalid target/floor produces no build or package output.");
        }
        Equal(projectBefore, AuthorFileTreeDigest.Compute(project), "Rejected target/floor leaves existing author artifacts unchanged.");
    }

    private static async Task TestReadableHistoricalSdkTargetPackages(string root, IReadOnlyDictionary<string, string> packages)
    {
        foreach (KeyValuePair<string, string> package in packages)
        {
            JsonObject sourceManifest = JsonNode.Parse(File.ReadAllText(Path.Combine(package.Key, "manifest.json")))!.AsObject();
            string uniqueId = sourceManifest["UniqueID"]!.GetValue<string>();
            string kind = sourceManifest["Type"]!.GetValue<string>();
            string historical = Path.Combine(root, "historical-low-floor-" + kind + ".zip");
            File.Copy(package.Value, historical);
            // Published SDK 0.1.0 allowed this lower minimum. Recreate those
            // historical bytes without relaxing the current writer's target gate.
            MutateZipJson(historical, "Content/DTMAPI/manifest.json", manifest => manifest["MinimumDTMApiVersion"] = "0.5.4");
            string manifestHash;
            using (ZipArchive archive = ZipFile.OpenRead(historical))
            using (Stream stream = archive.GetEntry("Content/DTMAPI/manifest.json")!.Open())
                manifestHash = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
            MutateZipJson(historical, "Content/DTMAPI/dtmapi-package.json", marker =>
            { marker["manifestSha256"] = manifestHash; marker["authorSdkVersion"] = "0.1.0"; });

            string game = NewGameRoot(root, "historical-low-floor-" + kind);
            HasCode(await ExpectPublicFailure("historical fixture must not reactivate legacy source selection", "source", "local", "select", uniqueId,
                Path.Combine(game, "Mods", uniqueId), "--game-root", game).ConfigureAwait(false), "SDK003", "Historical source selection remains paused");
            // Only the established internal compatibility fixture seeds an old
            // receipt. This is not a successful public deploy or player install.
            (int exitCode, CommandReport seeded, string error) = RunLegacyMutationCompatibilityFixture(
                new[] { "deploy", historical, "--game-root", game });
            True(exitCode == 0 && seeded.Success, "Internal historical receipt fixture: " + error + JsonSerializer.Serialize(seeded, JsonOptions));
            ValidatedPackageBinding currentBinding = DeploymentPackage.ValidateInstalledPackage(seeded.OutputPath, game);
            Equal(kind == "CodeMod" ? "Strict" : string.Empty, currentBinding.CodeModKind, "Schema-2 historical package retains its identity.");
            await ExpectSuccess("historical low-floor schema-2 status", "deployment-status", uniqueId, "--game-root", game).ConfigureAwait(false);

            LegacyDeploymentState legacy = ConvertInstalledDeploymentToLegacy(game, seeded.Values["journalPath"], uniqueId, kind);
            DeploymentPackage.ValidateInstalledPackage(legacy.DestinationPath, game, legacyDeployment: true, expectedUniqueId: uniqueId, expectedPackageKind: kind);
            await ExpectSuccess("historical low-floor schema-1 status", "deployment-status", uniqueId, "--game-root", game).ConfigureAwait(false);
            string historicalManifestPath = Path.Combine(legacy.DestinationPath, "Content", "DTMAPI", "manifest.json");
            string historicalManifestHash = Sha256(historicalManifestPath);
            PrepareLegacyUpdateForRecovery(legacy);
            CommandReport recovered = await ExpectSuccess("historical low-floor recovery", "recover", uniqueId, "--game-root", game).ConfigureAwait(false);
            Equal("true", recovered.Values["recovered"], "Old low-floor package can recover its prepared transaction.");
            Equal(historicalManifestHash, Sha256(historicalManifestPath), "Recovery preserves the exact old minimum declaration.");
            CommandReport withdrawn = await ExpectSuccess("historical low-floor withdrawal", "withdraw", uniqueId, "--game-root", game).ConfigureAwait(false);
            True(!Directory.Exists(legacy.DestinationPath) && Directory.Exists(withdrawn.Values["recoveryPath"]),
                "Old low-floor package can be withdrawn with its recovery tree retained.");
        }
    }

    private static async Task TestSdkSessionTargetSelection(string root)
    {
        string game = NewGameRoot(root, "selected-api-session");
        CommandReport prepared = await ExpectSuccess("default selected API session", "session", "prepare", "--game-root", game).ConfigureAwait(false);
        JsonObject descriptor = JsonNode.Parse(File.ReadAllText(prepared.Values["descriptorPath"]))!.AsObject();
        Equal("0.7.0", descriptor["apiTarget"]!.GetValue<string>(), "Default session reports the available compilation target.");
        Equal("0.7.0", descriptor["minimumRuntimeVersion"]!.GetValue<string>(),
            "New API raises the host minimum above the session protocol floor.");
        True(!descriptor.ContainsKey("runtimeVersion") && !descriptor.ContainsKey("hostVersion"), "Offline prepare does not fabricate actual host identity.");
        string descriptorBefore = Sha256(prepared.Values["descriptorPath"]);
        string credentialBefore = Sha256(prepared.Values["credentialPath"]);
        foreach (string target in new[] { "0.8.0", "0.6.1", "" })
        {
            CommandReport failure = await ExpectUsageFailure("session rejects unavailable API target", "API target", "session", "prepare", "--game-root", game, "--api-target", target).ConfigureAwait(false);
            HasCode(failure, "SDK001", "Unavailable session target is an ordinary CLI input error");
            True(!failure.Values.TryGetValue("statusCode", out string? status) || status != "session-io-error", "Unsupported target is not an IO failure.");
            Equal(descriptorBefore, Sha256(prepared.Values["descriptorPath"]), "Rejected selection does not overwrite an existing startup descriptor.");
            Equal(credentialBefore, Sha256(prepared.Values["credentialPath"]), "Rejected selection does not overwrite existing session credentials.");
        }
        await ExpectSuccess("clear selected API session", "session", "clear", "--game-root", game).ConfigureAwait(false);
    }

    private static async Task TestSdkTargetPayloadBinding(string root, string codeProject, string compatibility)
    {
        Equal(Path.GetFullPath(compatibility), CompatibilityAssets.Resolve(compatibility, "0.5.5").RootPath, "Explicit target resolves its matching frozen payload.");
        AssertTargetResolverRejected(compatibility, "0.6.3");
        string? previousCompatibility = Environment.GetEnvironmentVariable("DTMAPI_AUTHOR_COMPAT_ROOT");
        try
        {
            Environment.SetEnvironmentVariable("DTMAPI_AUTHOR_COMPAT_ROOT", compatibility);
            string missing = Path.Combine(root, "missing-compatibility");
            await AssertTargetPayloadRejected(codeProject, missing, "missing payload cannot fall back to the environment").ConfigureAwait(false);

            string missingFile = Path.Combine(root, "missing-file-compatibility");
            CopyDirectory(compatibility, missingFile);
            File.Delete(Path.Combine(missingFile, "DTMAPI.Abstractions.dll"));
            await AssertTargetPayloadRejected(codeProject, missingFile, "missing declared DLL").ConfigureAwait(false);

            string rebound = Path.Combine(root, "rebound-compatibility");
            CopyDirectory(compatibility, rebound);
            MutateJson(Path.Combine(rebound, "compatibility.json"), manifest => manifest["targetRuntimeVersion"] = "0.6.3");
            await AssertTargetPayloadRejected(codeProject, rebound, "payload manifest cannot rebind frozen bytes to another target").ConfigureAwait(false);
            foreach (string unavailable in new[] { "0.6.3", "0.6.1" })
                AssertTargetResolverRejected(rebound, unavailable);

            string extra = Path.Combine(root, "extra-file-compatibility");
            CopyDirectory(compatibility, extra);
            File.WriteAllText(Path.Combine(extra, "undeclared.txt"), "untrusted", new UTF8Encoding(false));
            await AssertTargetPayloadRejected(codeProject, extra, "undeclared payload file").ConfigureAwait(false);

            string tamper = Path.Combine(root, "tamper-compatibility");
            CopyDirectory(compatibility, tamper);
            await TestCompatibilityTamper(root, tamper).ConfigureAwait(false);
        }
        finally
        {
            Environment.SetEnvironmentVariable("DTMAPI_AUTHOR_COMPAT_ROOT", previousCompatibility);
        }
    }

    private static async Task AssertTargetPayloadRejected(string project, string compatibility, string label)
    {
        string projectBefore = AuthorFileTreeDigest.Compute(project);
        CommandReport rejected = await ExpectFailure(label, "build", project, "--compatibility-root", compatibility).ConfigureAwait(false);
        HasCode(rejected, "SDK202", "Selected compatibility payload fails closed: " + label);
        Equal(projectBefore, AuthorFileTreeDigest.Compute(project), "Rejected payload cannot replace a previously built author artifact.");
    }

    private static void AssertTargetResolverRejected(string root, string apiTarget)
    {
        try
        {
            CompatibilityAssets.Resolve(root, apiTarget);
        }
        catch (InvalidDataException)
        {
            return;
        }
        throw new InvalidOperationException("Unavailable API target must be rejected even when a payload is explicitly supplied: " + apiTarget);
    }

    private static JsonObject ReadTargetAuthor(string project) =>
        JsonNode.Parse(File.ReadAllText(Path.Combine(project, "dtmapi.author.json"), Encoding.UTF8))!.AsObject();
}
