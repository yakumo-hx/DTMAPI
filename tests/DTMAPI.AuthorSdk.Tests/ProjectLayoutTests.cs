using System.Diagnostics;
using System.IO.Compression;
using System.Security;
using System.Text.Json.Nodes;
using System.Xml.Linq;

namespace DTMAPI.AuthorSdk.Tests;

internal static partial class Program
{
    private static async Task TestStandardProjectLayouts(string temporary)
    {
        string workspace = Path.Combine(temporary, "作者 工程布局"), repository = FindRepository();
        string compatibility = Path.Combine(repository, ".tools/author-sdk-compatibility/0.7.0");
        string dotnet = Environment.GetEnvironmentVariable("DTMAPI_AUTHOR_DOTNET") ?? Path.Combine(repository, ".tools/dotnet/dotnet.exe");
        string sdkRoot = Path.GetDirectoryName(typeof(AuthorApplication).Assembly.Location)!;
        Directory.CreateDirectory(workspace);
        string Xml(string text) => SecurityElement.Escape(text)!;
        void Write(string file, string text) { Directory.CreateDirectory(Path.GetDirectoryName(file)!); File.WriteAllText(file, text); }
        Write(Path.Combine(workspace, "global.json"), "{\"sdk\":{\"version\":\"8.0.421\",\"rollForward\":\"disable\"}}");
        Write(Path.Combine(workspace, "Directory.Build.props"), $"<Project><PropertyGroup><DTMAPI_AUTHOR_SDK_ROOT>{Xml(sdkRoot)}</DTMAPI_AUTHOR_SDK_ROOT><DtmApiDotNet>{Xml(dotnet)}</DtmApiDotNet><DtmApiCompatibilityRoot>{Xml(compatibility)}</DtmApiCompatibilityRoot><LangVersion>10.0</LangVersion><Deterministic>true</Deterministic><PathMap>{Xml(workspace)}=/_/layout</PathMap></PropertyGroup></Project>");
        string library = Path.Combine(workspace, "Library/Willow.LayoutLogic.csproj");
        Write(library, "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>netstandard2.0</TargetFramework></PropertyGroup></Project>");
        Write(Path.Combine(workspace, "Library/Value.cs"), "namespace Willow.LayoutLogic; public static class Value { public static int Number => 39; }");
        async Task<(int Exit, string Text)> Direct(string project, params string[] arguments)
        {
            var info = new ProcessStartInfo(dotnet) { WorkingDirectory = temporary, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (string arg in new[] { "msbuild", project, "-nologo", "-v:minimal", "-nr:false" }.Concat(arguments)) info.ArgumentList.Add(arg);
            using var process = Process.Start(info)!;
            var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync(); return (process.ExitCode, await stdout + await stderr);
        }
        foreach (bool nested in new[] { false, true })
        {
            string root = Path.Combine(workspace, nested ? "子目录 作者" : "根目录 作者");
            await ExpectSuccess("new layout author", "new", "codemod", root, "--id", "Willow.Layout", "--name", "Layout", "--author", "Willow", "--api-target", "0.7.0");
            string projectDirectory = nested ? Path.Combine(root, "src") : root;
            string project = Path.Combine(projectDirectory, "Willow.Layout.csproj");
            var author = JsonNode.Parse(File.ReadAllText(Path.Combine(root, "dtmapi.author.json")))!.AsObject();
            author["projectFile"] = Path.GetRelativePath(root, project).Replace('\\', '/');
            Write(Path.Combine(root, "dtmapi.author.json"), author.ToJsonString());
            string source = Path.Combine(root, "code/Entry.cs"), content = Path.Combine(root, "assets/data.json");
            Write(source, "using DTMAPI.Abstractions; namespace Willow.Layout; public sealed class ModEntry:DtmMod { public override void Entry(IDtmHelper h){} public static int Result=>Willow.LayoutLogic.Value.Number+Generated.Number; }");
            Write(content, "{\"layout\":true}");
            string projectText = $$"""
                <Project>
                  <Import Project="Sdk.props" Sdk="Microsoft.NET.Sdk" />
                  <Import Project="$(DTMAPI_AUTHOR_SDK_ROOT)/build/DTMAPI.Author.props" />
                  <PropertyGroup><Version>0.1.0</Version><EnableDefaultCompileItems>false</EnableDefaultCompileItems><GenerateDocumentationFile>true</GenerateDocumentationFile></PropertyGroup>
                  <ItemGroup>
                    <Compile Include="{{Xml(Path.GetRelativePath(projectDirectory, source))}}" Link="Entry.cs" />
                    <Content Include="{{Xml(Path.GetRelativePath(projectDirectory, content))}}" DtmApiPackagePath="Content/DTMAPI/data.json" />
                    <ProjectReference Include="{{Xml(Path.GetRelativePath(projectDirectory, library))}}" DtmApiDistribution="self-authored" />
                  </ItemGroup>
                  <Import Project="Sdk.targets" Sdk="Microsoft.NET.Sdk" />
                  <Import Project="$(DTMAPI_AUTHOR_SDK_ROOT)/build/DTMAPI.Author.targets" />
                  <Target Name="GenerateLayoutInput" BeforeTargets="CoreCompile">
                    <WriteLinesToFile File="$(IntermediateOutputPath)Generated.g.cs" Lines="public static class Generated { public const int Number=3%3B }" Overwrite="true" />
                    <ItemGroup><Compile Include="$(IntermediateOutputPath)Generated.g.cs" /></ItemGroup>
                  </Target>
                </Project>
                """;
            Write(project, projectText);
            await ExpectSuccess("layout static metadata", "validate", root);
            await ExpectSuccess("layout restore", "restore", root);
            foreach (string configuration in new[] { "Debug", "Release" })
            {
                var packed = await ExpectSuccess("layout pack " + configuration, "pack", root, "--configuration", configuration, "--symbols", "true", "--compatibility-root", compatibility, "--output", Path.Combine(root, configuration + ".zip"));
                var facts = XDocument.Load(packed.Values["buildFactsPath"]);
                var properties = facts.Root!.Element("Properties")!.Elements("Property").ToDictionary(p => (string)p.Attribute("Name")!, p => p.Value);
                Equal(projectDirectory, properties["ProjectDirectory"], "actual csproj directory in facts");
                Equal(root, properties["AuthorProjectRoot"], "nearest author metadata owner");
                foreach (string key in new[] { "TargetPath", "IntermediateOutputPath", "DocumentationFile", "ProjectAssetsFile" })
                    True(Path.IsPathFullyQualified(properties[key]), "absolute MSBuild " + key);
                True(packed.Values["buildFactsPath"].StartsWith(Path.Combine(projectDirectory, "obj") + Path.DirectorySeparatorChar), "CLI facts belong to selected project");
                True(!nested || !Directory.Exists(Path.Combine(root, "obj")), "no stale or newly scattered parent obj");
                using (var zip = ZipFile.OpenRead(packed.OutputPath))
                {
                    True(zip.GetEntry("Content/DTMAPI/Willow.Layout.xml") != null && zip.GetEntry("Content/DTMAPI/Willow.Layout.pdb") != null, "actual XML/PDB packaged");
                    using var reader = new StreamReader(zip.GetEntry("Content/DTMAPI/data.json")!.Open());
                    Equal(File.ReadAllText(content), reader.ReadToEnd(), "relative MSBuild content preserved");
                }
                string dll = packed.Values["buildOutputPath"], pdb = Path.ChangeExtension(dll, ".pdb"), xml = Path.ChangeExtension(dll, ".xml");
                string[] hashes = new[] { dll, pdb, xml }.Select(Sha256).ToArray();
                var direct = await Direct(project, "-restore", "-t:Clean;DtmApiCollect", "-p:Configuration=" + configuration, "-p:UseSharedCompilation=false");
                True(direct.Exit == 0, "direct Clean/Build from unrelated cwd: " + direct.Text);
                True(hashes.SequenceEqual(new[] { dll, pdb, xml }.Select(Sha256)), "CLI and direct standard DLL/PDB/XML agree");
                True(!nested || !Directory.Exists(Path.Combine(root, "obj")), "direct build did not create parent obj");
            }
            Write(project, projectText.Replace("<Version>", "<DtmApiAuthorProjectRoot>..</DtmApiAuthorProjectRoot><Version>"));
            var explicitRoot = await Run("build", root, "--compatibility-root", compatibility, "--json");
            if (nested) True(explicitRoot.Report.Success, "explicit correct parent author root");
            else True(!explicitRoot.Report.Success, "wrong explicit owner must not fall back");
            Write(project, projectText.Replace("<Version>", "<DtmApiAuthorProjectRoot>missing-owner</DtmApiAuthorProjectRoot><Version>"));
            var wrongRoot = await ExpectFailure("explicit missing owner", "build", root, "--compatibility-root", compatibility);
            True(wrongRoot.Diagnostics.Any(d => d.Message.Contains("missing-owner")), "wrong root diagnostic names requested owner");
            Write(project, projectText);
            string other = Path.Combine(root, "Other.csproj"); Write(other, "<Project />");
            author["projectFile"] = "Other.csproj"; Write(Path.Combine(root, "dtmapi.author.json"), author.ToJsonString());
            var mismatched = await Direct(project, "-t:Rebuild", "-p:Configuration=Release");
            True(mismatched.Exit != 0 && mismatched.Text.Contains("Author metadata projectFile selects") && mismatched.Text.Contains("Other.csproj"), "metadata selecting another project is refused: " + mismatched.Text);
        }
    }
}
