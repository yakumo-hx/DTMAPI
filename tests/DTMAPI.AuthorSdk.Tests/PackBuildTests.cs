using DTMAPI.Authoring.Contracts;
using System.IO.Compression;
using System.Text;

namespace DTMAPI.AuthorSdk.Tests;

internal static partial class Program
{
    private static async Task TestPackBuildOutputAndIdentity(string temp, string compatibility)
    {
        // Regression: a misplaced diagnostic code used to invoke an unknown
        // command and pass merely because its usage error returned nonzero.
        var wrongCommand = await RunPublic("SDK191", "symbols", "missing.dll");
        bool invocationRejected = false;
        try { AssertValidationFailure("misrouted symbols", new[] { "symbols" }, wrongCommand.ExitCode, wrongCommand.Report); }
        catch (InvalidOperationException) { invocationRejected = true; }
        True(invocationRejected, "A usage error cannot satisfy a symbols validation test");
        await TestUnifiedBuildInputs(temp, compatibility).ConfigureAwait(false);
        await TestMigrationProjection(temp, compatibility).ConfigureAwait(false);
        string project = Path.Combine(temp, "编译项目 one");
        string other = Path.Combine(temp, "编译项目 two");
        foreach (string root in new[] { project, other })
            await ExpectSuccess("create pack-build project", "new", "codemod", "--api-target", "0.5.5", root, "--id", "Tests.PackBuild", "--name", "Pack build", "--author", "Tests").ConfigureAwait(false);
        string output = Path.Combine(temp, "实际编译 output");
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, "Tests.PackBuild.dll"), "stale output must be overwritten");
        CommandReport pack = await ExpectSuccess("pack compiles to explicit output", "pack", project, "--compatibility-root", compatibility,
            "--build-output", output, "--output", Path.Combine(temp, "pack-build-one.zip")).ConfigureAwait(false);
        Equal(Path.Combine(output, "Tests.PackBuild.dll"), pack.Values["buildOutputPath"], "Pack reports the actual build path");
        Equal(Sha256(pack.Values["buildOutputPath"]), pack.Values["buildOutputSha256"], "Build output hash names the actual file");
        Equal(pack.Values["buildOutputSha256"], pack.Values["entryDllSha256"], "Pack consumes the DLL from this compilation");
        True(!Directory.Exists(Path.Combine(project, "bin")), "Explicit pack output leaves no second default compilation directory");
        True(File.Exists(Path.Combine(output, "Tests.PackBuild.pdb")), "Symbols are emitted alongside the actual compilation");
        await AssertPublicSymbols(pack.Values["buildOutputPath"], Path.ChangeExtension(pack.Values["buildOutputPath"], ".pdb"), true);
        await AssertPublicSymbols(pack.Values["buildOutputPath"], Path.Combine(output, "missing.pdb"), false);
        using (ZipArchive archive = ZipFile.OpenRead(pack.OutputPath))
        {
            using var bytes = new MemoryStream();
            using Stream entry = archive.GetEntry("Content/DTMAPI/Tests.PackBuild.dll")!.Open();
            entry.CopyTo(bytes);
            True(bytes.ToArray().SequenceEqual(File.ReadAllBytes(pack.Values["buildOutputPath"])), "Pack contains the exact emitted bytes");
        }
        CommandReport same = await ExpectSuccess("pack equal inputs in another directory", "pack", other, "--compatibility-root", compatibility,
            "--output", Path.Combine(temp, "pack-build-two.zip")).ConfigureAwait(false);
        Equal(pack.Values["buildInputSha256"], same.Values["buildInputSha256"], "Input identity excludes machine-local output/source roots");
        Equal(pack.Sha256, same.Sha256, "Build-output selection preserves deterministic package bytes");
        string source = Directory.EnumerateFiles(Path.Combine(other, "src"), "*.cs").First();
        File.AppendAllText(source, "\n// changed source input\n", Encoding.UTF8);
        CommandReport changed = await ExpectSuccess("pack after source changed", "pack", other, "--compatibility-root", compatibility,
            "--output", Path.Combine(temp, "pack-build-changed.zip")).ConfigureAwait(false);
        True(changed.Values["buildInputSha256"] != pack.Values["buildInputSha256"], "Source changes invalidate compilation input identity");
        string content = Path.Combine(temp, "content no compilation");
        await ExpectSuccess("create content pack", "new", "contentpack", "--api-target", "0.5.5", content, "--id", "Tests.NoBuild", "--name", "Content", "--author", "Tests").ConfigureAwait(false);
        CommandReport refused = await ExpectFailure("content pack rejects build output", "pack", content, "--build-output", output).ConfigureAwait(false);
        True(refused.Diagnostics.Any(value => value.Code == "SDK303"), "Inapplicable build option is diagnosed");
    }

    private static async Task AssertPublicSymbols(string dll, string pdb, bool matched)
    {
        var result = await RunPublic("symbols", dll, "--pdb", pdb);
        True(result.ExitCode == (matched ? 0 : 1), "Symbols use the expected success/validation exit category, never usage or internal failure.");
        Equal("symbols", result.Report.Command, "The actual symbols command ran");
        True(result.Report.Success == matched, "Symbols pair outcome");
        HasCode(result.Report, matched ? "SDK190" : "SDK191", "Specific symbol diagnosis");
        Equal(Path.GetFullPath(dll), result.Report.RootPath, "Symbol DLL identity retained");
        Equal(Path.GetFullPath(pdb), result.Report.OutputPath, "Symbol PDB identity retained");
        if (!matched) True(result.Report.Diagnostics.Single(d => d.Code == "SDK191").Guidance.Contains("same build", StringComparison.Ordinal), "Symbol failure has actionable repair guidance.");
    }

    private static async Task TestMigrationProjection(string temp, string compatibility)
    {
        string root = Path.Combine(temp, "legacy projection 中文");
        await ExpectSuccess("new migration fixture", "new", "codemod", "--api-target", "0.5.5", root, "--id", "Tests.LegacyProjection", "--name", "Legacy", "--author", "Tests");
        string authorPath = Path.Combine(root, "dtmapi.author.json");
        var author = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(authorPath))!;
        author["sourceDirectory"] = "code 中文";
        author["assemblyName"] = "Independent.Assembly";
        WriteJson(authorPath, author);
        string manifestPath = Path.Combine(root, "manifest.json");
        var manifest = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(manifestPath))!;
        manifest["EntryDll"] = "Independent.Assembly.dll";
        WriteJson(manifestPath, manifest);
        Directory.CreateDirectory(Path.Combine(root, "code 中文"));
        foreach (string source in Directory.GetFiles(Path.Combine(root, "src"), "*.cs"))
            File.Copy(source, Path.Combine(root, "code 中文", Path.GetFileName(source)));
        string project = Directory.GetFiles(root, "*.csproj").Single();
        File.WriteAllText(project, "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><AssemblyName>Independent.Assembly</AssemblyName><RootNamespace>Unrelated.Namespace</RootNamespace></PropertyGroup></Project>");
        byte[] legacy = File.ReadAllBytes(project);
        CommandReport before = await ExpectSuccess("custom legacy source build", "build", root, "--compatibility-root", compatibility);
        CommandReport beforePack = await ExpectSuccess("custom legacy source pack", "pack", root, "--compatibility-root", compatibility, "--output", Path.Combine(temp, "legacy-before.zip"));
        CommandReport migrated = await ExpectSuccess("custom source migration", "migrate-build", root);
        True(File.ReadAllBytes(migrated.Values["backupPath"]).SequenceEqual(legacy), "Migration preserves original bytes");
        string current = File.ReadAllText(project);
        True(current.Contains("code 中文\\**\\*.cs", StringComparison.Ordinal), "Migration uses author sourceDirectory");
        True(current.Contains("<RootNamespace>Unrelated.Namespace</RootNamespace>", StringComparison.Ordinal), "Literal IDE namespace remains independent of assembly and mod identity");
        CommandReport after = await ExpectSuccess("migrated custom source build", "build", root, "--compatibility-root", compatibility);
        Equal(before.Values["buildInputSha256"], after.Values["buildInputSha256"], "Migration retains effective compilation inputs");
        Equal(before.Sha256, after.Sha256, "Migration retains emitted DLL");
        Equal(before.Values["symbolsSha256"], after.Values["symbolsSha256"], "Migration retains emitted PDB");
        CommandReport afterPack = await ExpectSuccess("migrated custom source pack", "pack", root, "--compatibility-root", compatibility, "--output", Path.Combine(temp, "legacy-after.zip"));
        Equal(beforePack.Sha256, afterPack.Sha256, "Migration retains package bytes");
        await ExpectSuccess("migration is idempotent", "migrate-build", root);
        Equal(current, File.ReadAllText(project), "Repeated migration keeps project bytes");
        foreach (string item in new[] { "Content", "None", "CustomCopiedAsset" })
        {
            string extra = "<ItemGroup><" + item + " Include=\"asset.txt\"><CopyToOutputDirectory>Always</CopyToOutputDirectory></" + item + "></ItemGroup>";
            File.WriteAllText(project, current.Replace("</Project>", extra + "</Project>", StringComparison.Ordinal));
            foreach (string command in new[] { "build", "pack", "migrate-build" })
            {
                var result = await RunPublic(command, root);
                Equal(command, result.Report.Command, "Requested project command executed");
                True(result.ExitCode == 1 && !result.Report.Success, "Unsupported items use validation failure");
                True(result.Report.Diagnostics.Any(d => d.Code == "SDK180" && d.Message.Contains("'" + item + "'", StringComparison.Ordinal)), "Specific unsupported item is identified");
            }
            True(File.ReadAllText(project).Contains(extra, StringComparison.Ordinal), "Rejected migration retains original project");
        }
        File.WriteAllText(project, current.Replace("<AssemblyName>Independent.Assembly</AssemblyName>", "<AssemblyName>Conflicting.Assembly</AssemblyName>", StringComparison.Ordinal));
        foreach (string command in new[] { "build", "pack" })
        {
            var result = await RunPublic(command, root);
            Equal(command, result.Report.Command, "Assembly conflict command executed");
            True(result.ExitCode == 1 && result.Report.Diagnostics.Any(d => d.Code == "SDK180" && d.Message.Contains("'AssemblyName'", StringComparison.Ordinal)), "Assembly conflicts are diagnosed against author JSON");
        }
        File.WriteAllText(project, current);
        await ExpectSuccess("restored supported projection", "build", root, "--compatibility-root", compatibility);
    }

    private static async Task TestUnifiedBuildInputs(string temp, string compatibility)
    {
        string root = Path.Combine(temp, "陌生 Build plan");
        await ExpectSuccess("new build plan", "new", "codemod", "--api-target", "0.5.5", root, "--id", "Visitor.BuildPlan", "--name", "Build plan", "--author", "Visitor");
        string source = Path.Combine(root, "src", "Symbols.cs");
        File.WriteAllText(source, "namespace Visitor { public class UnityEngine { public const string Text = \"BepInEx HarmonyLib Assembly-CSharp UnityEngine\"; } public class Probe { public static string Text = UnityEngine.Text; } } // UnityEngine.Object\n");
        CommandReport release = await ExpectSuccess("benign words build", "build", root, "--compatibility-root", compatibility);
        CommandReport debug = await ExpectSuccess("debug build", "build", root, "--configuration", "Debug", "--compatibility-root", compatibility);
        True(release.OutputPath != debug.OutputPath && File.Exists(release.OutputPath), "Debug preserves Release outputs");
        await AssertPublicSymbols(debug.OutputPath, Path.ChangeExtension(release.OutputPath, ".pdb"), false);
        True(release.Values["buildInputSha256"] != debug.Values["buildInputSha256"], "Configuration participates in input identity");
        CommandReport repeated = await ExpectSuccess("repeat Debug", "build", root, "--configuration", "Debug", "--compatibility-root", compatibility);
        Equal(debug.Sha256, repeated.Sha256, "Repeated Debug is deterministic");
        Equal(debug.Values["symbolsSha256"], repeated.Values["symbolsSha256"], "Symbols are deterministic too");
        File.AppendAllText(source, "\nusing broken syntax");
        CommandReport broken = await ExpectFailure("compile error", "build", root, "--compatibility-root", compatibility);
        True(broken.Diagnostics.Any(d => d.Code.StartsWith("CS", StringComparison.Ordinal)), "Unresolved source is a compiler error, not guessed SDK160");
        File.WriteAllText(source, "using Alias = global::UnityEngine.Object; namespace Visitor { public class Probe { public Alias Value; } }");
        await ExpectFailure("real unbound host alias", "build", root, "--compatibility-root", compatibility);
        File.WriteAllText(source, "// no host reference\n");
        string project = Directory.GetFiles(root, "*.csproj").Single();
        string original = File.ReadAllText(project);
        foreach (string input in new[] { "<ItemGroup><Reference Include=\"UnityEngine.CoreModule\" /></ItemGroup>", "<ItemGroup><PackageReference Include=\"Some.Package\" Version=\"1.0\" /></ItemGroup>", "<PropertyGroup><DefineConstants>INVALID-CONSTANT</DefineConstants></PropertyGroup>" })
        {
            File.WriteAllText(project, original.Replace("</Project>", input + "</Project>", StringComparison.Ordinal));
            CommandReport rejected = await ExpectFailure("unsupported or host project input", "build", root, "--compatibility-root", compatibility);
            HasCode(rejected, input.Contains("UnityEngine", StringComparison.Ordinal) ? "SDK160" : "SDK180", "Structured project diagnostic");
        }
        File.WriteAllText(project, original);
        string native = Path.Combine(temp, "BepInEx.dll");
        string bridge = Path.Combine(temp, "Bridge.dll");
        void Emit(string name, string code, string path, params string[] refs)
        {
            var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create(name,
                new[] { Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code) },
                refs.Prepend(typeof(object).Assembly.Location).Select(path => Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(path)),
                new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary));
            using var output = File.Create(path);
            True(compilation.Emit(output).Success, "Reference fixture compiled");
        }
        Emit("BepInEx", "public class Host {}", native);
        Emit("Bridge", "public class Indirect : Host {}", bridge, native);
        File.WriteAllText(project, original.Replace("</Project>", "<ItemGroup><Reference Include=\"Bridge\"><HintPath>" + bridge + "</HintPath></Reference></ItemGroup></Project>", StringComparison.Ordinal));
        HasCode(await ExpectFailure("indirect host closure", "validate", root), "SDK160", "Actual PE dependency closure rejected");
        File.WriteAllText(project, "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><AssemblyName>Visitor.BuildPlan</AssemblyName><RootNamespace>Visitor.BuildPlan</RootNamespace></PropertyGroup></Project>");
        byte[] legacy = File.ReadAllBytes(project);
        CommandReport migrated = await ExpectSuccess("explicit old template migration", "migrate-build", root);
        True(File.ReadAllBytes(migrated.Values["backupPath"]).SequenceEqual(legacy), "Migration preserves exact old project bytes");
        await ExpectSuccess("migrated build", "build", root, "--compatibility-root", compatibility);
        CommandReport symbolsPack = await ExpectSuccess("Debug package symbols", "pack", root, "--configuration", "Debug", "--compatibility-root", compatibility);
        using var zip = ZipFile.OpenRead(symbolsPack.OutputPath);
        True(zip.GetEntry("Content/DTMAPI/Visitor.BuildPlan.pdb") != null, "Debug package includes its matching emitted symbols");
    }
}
