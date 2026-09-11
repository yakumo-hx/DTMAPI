using DTMAPI.Authoring.Contracts;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

namespace DTMAPI.AuthorSdk.Tests;

internal static partial class Program
{
    private static async Task TestPackBuildOutputAndIdentity(string temp, string compatibility)
    {
        var wrongCommand = await RunPublic("SDK191", "symbols", "missing.dll");
        bool invocationRejected = false;
        try { AssertValidationFailure("misrouted symbols", new[] { "symbols" }, wrongCommand.ExitCode, wrongCommand.Report); }
        catch (InvalidOperationException) { invocationRejected = true; }
        True(invocationRejected, "A usage error cannot satisfy a symbols validation test");
        string one = Path.Combine(temp, "编译项目 one"), two = Path.Combine(temp, "移动目录 two");
        string output = Path.Combine(temp, "实际编译 output");
        foreach (string root in new[] { one, two })
        {
            await ExpectSuccess("create standard pack project", "new", "codemod", root, "--api-target", "0.5.5", "--id", "Tests.PackBuild", "--name", "Pack build", "--author", "Tests");
            string project = Path.Combine(root, "Tests.PackBuild.csproj");
            var document = XDocument.Load(project);
            document.Root!.Elements("Import").First(e => (string?)e.Attribute("Project") == "Sdk.targets").AddBeforeSelf(
                new XElement("PropertyGroup", new XElement("PathMap", root + "=/_/Author," + output + "=/_/Author/bin/Release/netstandard2.0"), new XElement("GenerateDocumentationFile", "true")));
            document.Save(project);
            Directory.CreateDirectory(Path.Combine(root, "content", "nested"));
            File.WriteAllText(Path.Combine(root, "content", "nested", "data.json"), "{\"value\":42}");
            await ExpectSuccess("restore standard pack project", "restore", root, "--offline", "true");
        }
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, "Tests.PackBuild.dll"), "stale output must be overwritten");
        File.WriteAllText(Path.Combine(output, "unrelated.txt"), "preserve me");
        CommandReport pack = await ExpectSuccess("pack explicit standard output", "pack", one, "--compatibility-root", compatibility,
            "--build-output", output, "--output", Path.Combine(temp, "pack-build-one.zip"), "--symbols", "true");
        Equal(Path.Combine(output, "Tests.PackBuild.dll"), pack.Values["buildOutputPath"], "actual standard TargetPath");
        Equal(Sha256(pack.Values["buildOutputPath"]), pack.Values["buildOutputSha256"], "actual DLL hash");
        Equal(pack.Values["buildOutputSha256"], pack.Values["entryDllSha256"], "package consumes this build");
        Equal("preserve me", File.ReadAllText(Path.Combine(output, "unrelated.txt")), "Build preserves non SDK output");
        await AssertPublicSymbols(pack.Values["buildOutputPath"], Path.ChangeExtension(pack.Values["buildOutputPath"], ".pdb"), true);
        await AssertPublicSymbols(pack.Values["buildOutputPath"], Path.Combine(output, "missing.pdb"), false);
        using (var zip = ZipFile.OpenRead(pack.OutputPath))
        {
            True(zip.GetEntry("Content/DTMAPI/nested/data.json") != null, "template preserves nested official content path");
            True(!zip.Entries.Any(e => e.Name == "unrelated.txt"), "no output directory scan");
        }
        var moved = await ExpectSuccess("same inputs in second absolute directory", "pack", two, "--compatibility-root", compatibility,
            "--output", Path.Combine(temp, "pack-build-two.zip"), "--symbols", "true");
        if (pack.Values["buildOutputSha256"] != moved.Values["buildOutputSha256"])
        {
            string evidence = Path.Combine(FindRepository(), "artifacts", "pn041", "sdk-msbuild-evidence", "repro-diagnostic"); Directory.CreateDirectory(evidence);
            File.Copy(pack.Values["buildFactsPath"], Path.Combine(evidence, "one-facts.xml"), true);
            File.Copy(moved.Values["buildFactsPath"], Path.Combine(evidence, "two-facts.xml"), true);
            foreach (var pair in new[] { ("one", one, pack.Values["buildOutputPath"]), ("two", two, moved.Values["buildOutputPath"]) })
            {
                File.Copy(pair.Item3, Path.Combine(evidence, pair.Item1 + ".dll"), true);
                File.Copy(Path.ChangeExtension(pair.Item3, ".pdb"), Path.Combine(evidence, pair.Item1 + ".pdb"), true);
                string config = Directory.EnumerateFiles(Path.Combine(pair.Item2, "obj"), "Tests.PackBuild.GeneratedMSBuildEditorConfig.editorconfig", SearchOption.AllDirectories).Single();
                File.Copy(config, Path.Combine(evidence, pair.Item1 + ".editorconfig"), true);
            }
        }
        foreach (string key in new[] { "buildOutputSha256", "symbolsSha256", "documentationSha256" }) Equal(pack.Values[key], moved.Values[key], "mapped two directory " + key);
        Equal(pack.Sha256, moved.Sha256, "mapped two directory ZIP determinism");
        var release = await ExpectSuccess("standard Release", "build", two, "--compatibility-root", compatibility);
        var debug = await ExpectSuccess("standard Debug", "build", two, "--configuration", "Debug", "--compatibility-root", compatibility);
        True(debug.OutputPath != release.OutputPath, "configurations own different target paths");
        Equal(release.Sha256, Sha256(release.OutputPath), "Debug preserves Release");
        await AssertPublicSymbols(debug.OutputPath, Path.ChangeExtension(release.OutputPath, ".pdb"), false);
        var repeat = await ExpectSuccess("repeat Debug", "build", two, "--configuration", "Debug", "--compatibility-root", compatibility);
        Equal(debug.Sha256, repeat.Sha256, "repeat Debug DLL");
        Equal(debug.Values["symbolsSha256"], repeat.Values["symbolsSha256"], "repeat Debug PDB");
        var concurrent = await Task.WhenAll(new[] { "Debug", "Release" }.Select(configuration => Task.Run(() =>
            ExpectSuccess("concurrent " + configuration, "pack", two, "--configuration", configuration,
                "--compatibility-root", compatibility, "--output", Path.Combine(temp, "parallel-" + configuration + ".zip")))));
        True(concurrent[0].Values["buildOutputPath"] != concurrent[1].Values["buildOutputPath"], "Concurrent configurations own distinct DLL outputs");
        foreach (var result in concurrent)
        {
            Equal(result.Values["buildOutputSha256"], Sha256(result.Values["buildOutputPath"]), "Concurrent publication captures its actual DLL");
            using var archive = ZipFile.OpenRead(result.OutputPath);
            using var entry = archive.GetEntry("Content/DTMAPI/Tests.PackBuild.dll")!.Open();
            Equal(result.Values["buildOutputSha256"], Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(entry)).ToLowerInvariant(), "Concurrent ZIP contains its configuration output");
        }
        string projectPath = Path.Combine(one, "Tests.PackBuild.csproj"), original = File.ReadAllText(projectPath);
        string sourcePath = Path.Combine(one, "src", "ModEntry.cs"), source = File.ReadAllText(sourcePath);
        File.AppendAllText(sourcePath, "\nusing broken syntax");
        var broken = await ExpectFailure("compile failure refuses stale output", "pack", one, "--compatibility-root", compatibility, "--output", pack.OutputPath);
        True(broken.Diagnostics.Any(d => d.Code.StartsWith("CS", StringComparison.Ordinal)), "actual compiler diagnostic");
        Equal(pack.Sha256, Sha256(pack.OutputPath), "compile failure preserves published package");
        File.WriteAllText(sourcePath, source);
        string tampered = Path.Combine(one, "assets", "DTMAPI.Abstractions.dll");
        Directory.CreateDirectory(Path.GetDirectoryName(tampered)!);
        File.WriteAllBytes(tampered, File.ReadAllBytes(CompatibilityAssets.Resolve(compatibility, "0.5.5").AbstractionsPath).Concat(new byte[] { 0 }).ToArray());
        string Xml(string value) => System.Security.SecurityElement.Escape(value)!;
        string frozen = CompatibilityAssets.Resolve(compatibility, "0.5.5").AbstractionsPath;
        foreach (string fault in new[] { "wrong-pdb", "late-api", "content-escape", "content-case-collision", "host-content" })
        {
            string extension = fault switch
            {
                "wrong-pdb" => $"<Target Name=\"ReplacePdb\" AfterTargets=\"Build\"><Copy SourceFiles=\"{Xml(debug.Values["symbolsPath"])}\" DestinationFiles=\"$(TargetDir)$(TargetName).pdb\" /></Target>",
                "late-api" => $"<Target Name=\"LateReference\" AfterTargets=\"DtmApiCheckResolvedReferences\" BeforeTargets=\"CoreCompile\"><ItemGroup><ReferencePathWithRefAssemblies Remove=\"{Xml(frozen)}\" /><ReferencePathWithRefAssemblies Include=\"{Xml(tampered)}\" /></ItemGroup></Target>",
                "content-escape" => "<ItemGroup><None Include=\"manifest.json\" DtmApiPackagePath=\"../escape.json\" /></ItemGroup>",
                "content-case-collision" => "<ItemGroup><None Include=\"manifest.json\" DtmApiPackagePath=\"content/dtmapi/NESTED/DATA.JSON\" /></ItemGroup>",
                _ => $"<ItemGroup><None Include=\"{Xml(tampered)}\" DtmApiPackagePath=\"Content/Host.data\" /></ItemGroup>"
            };
            File.WriteAllText(projectPath, original.Replace("</Project>", extension + "</Project>"));
            var refused = await ExpectFailure(fault, "pack", one, "--compatibility-root", compatibility, "--output", pack.OutputPath);
            True(refused.Diagnostics.All(d => d.Code != "SDK999"), fault + " gives a validation failure");
            if (fault == "late-api") True(refused.Diagnostics.Any(d => d.Message.Contains("Resolved Csc reference differs")), "actual Csc task arguments expose late frozen API replacement");
            Equal(pack.Sha256, Sha256(pack.OutputPath), fault + " preserves old package");
        }
        File.WriteAllText(projectPath, original.Replace("</Project>", "<Target Name=\"FinalXml\" AfterTargets=\"Build\"><WriteLinesToFile File=\"$(TargetDir)$(TargetName).xml\" Lines=\"FINAL-XML\" Overwrite=\"true\" /></Target></Project>"));
        var finalXml = await ExpectSuccess("capture after full Build", "pack", one, "--compatibility-root", compatibility, "--output", Path.Combine(temp, "postprocess.zip"));
        using (var zip = ZipFile.OpenRead(finalXml.OutputPath))
        using (var reader = new StreamReader(zip.GetEntry("Content/DTMAPI/Tests.PackBuild.xml")!.Open())) Equal("FINAL-XML", reader.ReadToEnd().Trim(), "final XML output captured");
        File.WriteAllText(projectPath, original);
        string content = Path.Combine(temp, "content no compilation");
        await ExpectSuccess("create content pack", "new", "contentpack", content, "--api-target", "0.5.5", "--id", "Tests.NoBuild", "--name", "Content", "--author", "Tests");
        string? previousDotnet = Environment.GetEnvironmentVariable("DTMAPI_AUTHOR_DOTNET");
        try
        {
            Environment.SetEnvironmentVariable("DTMAPI_AUTHOR_DOTNET", Path.Combine(temp, "missing-dotnet.exe"));
            var contentPack = await ExpectSuccess("ContentPack without compiler", "pack", content);
            using (var lease = new FileStream(contentPack.OutputPath + ".publish.lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                var conflict = await ExpectFailure("same output publication conflict", "pack", content);
                HasCode(conflict, "SDK302", "Concurrent publication fails explicitly");
                Equal(contentPack.Sha256, Sha256(contentPack.OutputPath), "Concurrent publication preserves the previous ZIP");
            }
            await ExpectSuccess("publication lease released", "pack", content);
            using var doctorOutput = new StringWriter();
            int doctorExit = await AuthorApplication.RunAsync(new[] { "doctor", contentPack.OutputPath, "--json" }, doctorOutput);
            True(doctorExit == 0 && doctorOutput.ToString().Contains("Tests.NoBuild"), "ContentPack ZIP Doctor without compiler");
            True(!doctorOutput.ToString().Contains("DTMAPI-Doctor-", StringComparison.Ordinal), "ZIP report does not expose deleted staging paths");
            string maliciousZip = Path.Combine(temp, "untrusted-build.zip"), sentinel = Path.Combine(temp, "doctor-executed-build.txt");
            File.Copy(contentPack.OutputPath, maliciousZip);
            using (var zip = ZipFile.Open(maliciousZip, ZipArchiveMode.Update))
            using (var writer = new StreamWriter(zip.CreateEntry("untrusted.csproj").Open()))
                writer.Write("<Project DefaultTargets=\"Build\"><Target Name=\"Build\"><WriteLinesToFile File=\"" + Xml(sentinel) + "\" Lines=\"EXECUTED\" /></Target></Project>");
            using var untrustedOutput = new StringWriter();
            int untrustedExit = await AuthorApplication.RunAsync(new[] { "doctor", maliciousZip, "--json" }, untrustedOutput);
            True(untrustedExit is 0 or 2 && !File.Exists(sentinel), "Doctor never executes archive MSBuild targets");
            string traversalZip = Path.Combine(temp, "unsafe-members.zip");
            using (var zip = ZipFile.Open(traversalZip, ZipArchiveMode.Create))
            using (var writer = new StreamWriter(zip.CreateEntry("../doctor-executed-build.txt").Open())) writer.Write("escaped");
            using var traversalOutput = new StringWriter();
            True(await AuthorApplication.RunAsync(new[] { "doctor", traversalZip, "--json" }, traversalOutput) == 2 && !File.Exists(sentinel), "Doctor rejects traversal ZIP before extraction outside staging");
        }
        finally { Environment.SetEnvironmentVariable("DTMAPI_AUTHOR_DOTNET", previousDotnet); }
        var inapplicable = await ExpectFailure("ContentPack rejects build output", "pack", content, "--build-output", output);
        HasCode(inapplicable, "SDK303", "inapplicable compiler option");
    }

    private static async Task AssertPublicSymbols(string dll, string pdb, bool matched)
    {
        var result = await RunPublic("symbols", dll, "--pdb", pdb);
        True(result.ExitCode == (matched ? 0 : 1), "Symbols use expected success/validation exit, never usage/internal failure");
        Equal("symbols", result.Report.Command, "actual symbols command");
        True(result.Report.Success == matched, "Symbols pair outcome");
        HasCode(result.Report, matched ? "SDK190" : "SDK191", "Specific symbol diagnosis");
        Equal(Path.GetFullPath(dll), result.Report.RootPath, "Symbol DLL identity");
        Equal(Path.GetFullPath(pdb), result.Report.OutputPath, "Symbol PDB identity");
        if (!matched) True(result.Report.Diagnostics.Single(d => d.Code == "SDK191").Guidance.Contains("same build", StringComparison.Ordinal), "Actionable symbol repair guidance");
    }
}
