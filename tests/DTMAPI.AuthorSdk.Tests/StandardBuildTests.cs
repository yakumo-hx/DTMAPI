using DTMAPI.Authoring.Contracts;
using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.Loader;
using System.Security;

namespace DTMAPI.AuthorSdk.Tests;

internal static partial class Program
{
    private static async Task TestStandardBuild(string temporary)
    {
        string repository = FindRepository();
        string compatibility = Path.Combine(repository, ".tools", "author-sdk-compatibility", "0.7.0");
        string dotnet = Environment.GetEnvironmentVariable("DTMAPI_AUTHOR_DOTNET") ?? Path.Combine(repository, ".tools", "dotnet", "dotnet.exe");
        string integration = Path.GetDirectoryName(typeof(DTMAPI.AuthorSdk.AuthorApplication).Assembly.Location)!;
        string workspace = Path.Combine(temporary, "标准 MSBuild workspace");
        Directory.CreateDirectory(workspace);
        string Xml(string text) => SecurityElement.Escape(text)!;
        void Write(string file, string text)
        { string path = Path.Combine(workspace, file); Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllText(path, text); }
        Write("global.json", "{\"sdk\":{\"version\":\"8.0.421\",\"rollForward\":\"disable\"}}");
        Write("Directory.Build.props", $"<Project><PropertyGroup><DTMAPI_AUTHOR_SDK_ROOT>{Xml(integration)}</DTMAPI_AUTHOR_SDK_ROOT><DtmApiCompatibilityRoot>{Xml(compatibility)}</DtmApiCompatibilityRoot><DtmApiDotNet>{Xml(dotnet)}</DtmApiDotNet><LangVersion>10.0</LangVersion><Deterministic>true</Deterministic><PathMap>{Xml(workspace)}=/_/standard</PathMap></PropertyGroup></Project>");
        Write("Leaf/Maple.Leaf.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>netstandard2.0</TargetFramework></PropertyGroup></Project>");
        Write("Leaf/Value.cs", "namespace Maple.Leaf; public static class Value { public static int Number => 42; }");
        Write("Middle/Maple.Middle.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFrameworks>netstandard2.0;net8.0</TargetFrameworks></PropertyGroup><ItemGroup><ProjectReference Include=\"../Leaf/Maple.Leaf.csproj\" /></ItemGroup></Project>");
        Write("Middle/Value.cs", "namespace Maple.Middle; public static class Value { public static int Number => Maple.Leaf.Value.Number+1; }");
        Write("Tool/Maple.Tool.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>");
        Write("Tool/Value.cs", "#if !NET8_0\n#error Tool graph was forced to the game framework\n#endif\nnamespace Maple.Tool; public static class Value { public static int Number=>8; }");
        Write("Generator/Maple.Generator.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>netstandard2.0</TargetFramework></PropertyGroup><ItemGroup><PackageReference Include=\"Microsoft.CodeAnalysis.CSharp\" Version=\"4.11.0\" PrivateAssets=\"all\" /></ItemGroup></Project>");
        Write("Generator/Generate.cs", """
            using Microsoft.CodeAnalysis;
            using Microsoft.CodeAnalysis.Diagnostics;
            using Microsoft.CodeAnalysis.CSharp.Syntax;
            using System.Collections.Immutable;
            using System.Linq;
            [Generator] public sealed class Generate:ISourceGenerator {
              public void Initialize(GeneratorInitializationContext c){}
              public void Execute(GeneratorExecutionContext c){
                var value=c.AdditionalFiles.Single(f=>f.Path.EndsWith("number.txt")).GetText(c.CancellationToken).ToString().Trim();
                if(value=="async") { c.AddSource("AsyncCallback.g.cs","using DTMAPI.Abstractions; public class GeneratedCallback { public static void Register(IDtmScheduler s){s.Post(async ()=>{await System.Threading.Tasks.Task.Yield();});} }"); value="7"; }
                c.AddSource("Generated.g.cs","namespace Maple.Standard { public static class Generated { public const int Number="+value+"; } }");
              }
            }
            [DiagnosticAnalyzer(LanguageNames.CSharp)] public sealed class AuthorRule:DiagnosticAnalyzer {
              static readonly DiagnosticDescriptor Rule=new DiagnosticDescriptor("MAPLE100","Author rule","Author diagnostic enabled","Author",DiagnosticSeverity.Warning,true);
              public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics=>ImmutableArray.Create(Rule);
              public override void Initialize(AnalysisContext c){c.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze|GeneratedCodeAnalysisFlags.ReportDiagnostics);
                c.RegisterSyntaxTreeAction(t=>{foreach(var token in t.Tree.GetRoot().DescendantTokens().Where(x=>x.ValueText=="AuthorError"))t.ReportDiagnostic(Diagnostic.Create(Rule,token.GetLocation()));});}
            }
            """);
        string mod = Path.Combine(workspace, "Mod");
        True((await Run("new", "codemod", mod, "--id", "Maple.Standard", "--name", "Standard", "--author", "Maple", "--api-target", "0.7.0", "--json")).Report.Success, "create standard fixture manifest");
        Write("Mod/dtmapi.author.json", "{\"schemaVersion\":4,\"projectKind\":\"CodeMod\",\"codeModKind\":\"Strict\",\"projectFile\":\"Maple.Standard.csproj\",\"targetDtmApiVersion\":\"0.7.0\"}");
        string projectText = """
            <Project>
              <Import Project="Sdk.props" Sdk="Microsoft.NET.Sdk" />
              <Import Project="$(DTMAPI_AUTHOR_SDK_ROOT)/build/DTMAPI.Author.props" />
              <PropertyGroup><AssemblyName>Maple.Standard</AssemblyName><Version>0.1.0</Version><GenerateDocumentationFile>true</GenerateDocumentationFile></PropertyGroup>
              <ItemDefinitionGroup><ReferenceCopyLocalPaths><DtmApiDistribution>self-authored</DtmApiDistribution></ReferenceCopyLocalPaths></ItemDefinitionGroup>
              <ItemGroup>
                <ProjectReference Include="../Middle/Maple.Middle.csproj" DtmApiDistribution="self-authored" />
                <ProjectReference Include="../Generator/Maple.Generator.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
                <ProjectReference Include="../Tool/Maple.Tool.csproj" ReferenceOutputAssembly="false" SkipGetTargetFrameworkProperties="true" />
                <AdditionalFiles Include="number.txt" />
                <EmbeddedResource Include="text.txt" LogicalName="Maple.Text" />
                <None Update="content.json" DtmApiPackagePath="Content/Maple/content.json" />
                <Compile Remove="excluded.cs" />
              </ItemGroup>
              <Import Project="Sdk.targets" Sdk="Microsoft.NET.Sdk" />
              <Import Project="$(DTMAPI_AUTHOR_SDK_ROOT)/build/DTMAPI.Author.targets" />
              <Target Name="MakeExtra" BeforeTargets="CoreCompile">
                <WriteLinesToFile File="$(IntermediateOutputPath)Extra.g.cs" Lines="namespace Maple.Standard { public static class Extra { public const int Number=3%3B } }" Overwrite="true" />
                <ItemGroup><Compile Include="$(IntermediateOutputPath)Extra.g.cs" /></ItemGroup>
              </Target>
            </Project>
            """;
        Write("Mod/Maple.Standard.csproj", projectText);
        Write("Mod/number.txt", "7"); Write("Mod/text.txt", "resource-value"); Write("Mod/content.json", "{\"value\":43}");
        Write("Mod/excluded.cs", "#error excluded standard item was included");
        string source = """
            using DTMAPI.Abstractions;
            namespace Maple.Standard;
            public sealed class ModEntry:DtmMod {
            #if NETSTANDARD2_0
              public static int Result=>Maple.Middle.Value.Number+Generated.Number+Extra.Number;
            #else
            #error NETSTANDARD2_0 is required
            #endif
              public override void Entry(IDtmHelper helper){helper.Monitor.Log("value="+Result);}
            }
            """;
        Write("Mod/src/ModEntry.cs", source);
        Write("Mod/.editorconfig", "root=true\n[*.cs]\ndotnet_diagnostic.CS0168.severity=error\ndotnet_diagnostic.MAPLE100.severity=error\n");
        var build = (await Run("build", mod, "--compatibility-root", compatibility, "--configuration", "AuthorTest", "--json")).Report;
        True(build.Success, "standard custom config: " + string.Join(";", build.Diagnostics.Select(d => d.Message)));
        Equal("MSBuild", build.Values["buildBackend"], "standard backend");
        Equal("8.0.421", build.Values["sdkVersion"], "exact compiler SDK");
        True(File.Exists(Path.Combine(workspace, "Tool/bin/AuthorTest/net8.0/Maple.Tool.dll")), "tool remains net8");
        int Value(string dll)
        {
            var loader = new AssemblyLoadContext("standard-test-" + Guid.NewGuid(), true);
            loader.Resolving += (_, name) => name.Name == "DTMAPI.Abstractions" ? typeof(DTMAPI.Abstractions.DtmMod).Assembly : loader.LoadFromStream(new MemoryStream(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(dll)!, name.Name + ".dll"))));
            var assembly = loader.LoadFromStream(new MemoryStream(File.ReadAllBytes(dll)));
            int value = (int)assembly.GetType("Maple.Standard.ModEntry")!.GetProperty("Result")!.GetValue(null)!;
            using var resource = assembly.GetManifestResourceStream("Maple.Text");
            Equal("resource-value", new StreamReader(resource!).ReadToEnd(), "actual resource lookup");
            loader.Unload(); return value;
        }
        True(Value(build.OutputPath) == 53, "two libraries, generator and standard target actually execute");
        Write("Mod/number.txt", "9");
        var changed = (await Run("build", mod, "--compatibility-root", compatibility, "--configuration", "AuthorTest", "--json")).Report;
        True(changed.Success, "generator input rebuild: " + string.Join(";", changed.Diagnostics.Select(d => d.Message))); True(Value(changed.OutputPath) == 55, "AdditionalFiles changes actual generated behavior");
        foreach (var fault in new[] { "editorconfig", "author-analyzer", "async-lambda", "async-method" })
        {
            string replacement = fault switch {
                "editorconfig" => source.Replace("helper.Monitor.Log", "int unused; helper.Monitor.Log"),
                "author-analyzer" => source.Replace("public static int Result", "public static int AuthorError=>1; public static int Result"),
                "async-lambda" => source.Replace("public override void Entry", "static void Test(IDtmScheduler scheduler){scheduler.Post(async () => { await System.Threading.Tasks.Task.Yield(); });} public override void Entry"),
                _ => source.Replace("public override void Entry", "static async void Bad(){await System.Threading.Tasks.Task.Yield();} static void Test(IDtmScheduler scheduler){scheduler.Post(Bad);} public override void Entry") };
            Write("Mod/src/ModEntry.cs", replacement);
            var failed = (await Run("build", mod, "--compatibility-root", compatibility, "--json")).Report;
            string code = fault == "editorconfig" ? "CS0168" : fault == "author-analyzer" ? "MAPLE100" : "SDK203";
            True(!failed.Success && failed.Diagnostics.Any(d => d.Code == code), fault + " requires actual " + code + ": " + string.Join(";", failed.Diagnostics.Select(d=>d.Message)));
        }
        Write("Mod/src/ModEntry.cs", source);
        Write("Mod/number.txt", "async");
        var generatedCallback = (await Run("build", mod, "--compatibility-root", compatibility, "--json")).Report;
        True(!generatedCallback.Success && generatedCallback.Diagnostics.Any(d => d.Code == "SDK203" && d.Path.Contains("AsyncCallback.g.cs", StringComparison.Ordinal)), "required synchronous callback analysis covers actual generator output");
        Write("Mod/number.txt", "9");
        var pack = (await Run("pack", mod, "--compatibility-root", compatibility, "--symbols", "true", "--json")).Report;
        True(pack.Success, "standard snapshot pack: " + string.Join(";", pack.Diagnostics.Select(d => d.Message)));
        using (var zip = ZipFile.OpenRead(pack.OutputPath))
        {
            True(zip.GetEntry("lib/private/Maple.Leaf.dll") != null && zip.GetEntry("lib/private/Maple.Middle.dll") != null, "actual transitive runtime outputs selected");
            True(!zip.Entries.Any(e => e.Name.Contains("Generator") || e.Name.Contains("Maple.Tool")), "build-time assets not delivered");
            True(zip.GetEntry("Content/Maple/content.json") != null, "explicit standard content included");
        }
        Write("Mod/Maple.Standard.csproj", projectText.Replace("<Version>0.1.0</Version>", "<Version>0.1.0</Version><RunAnalyzers>false</RunAnalyzers>"));
        var disabled = (await Run("pack", mod, "--compatibility-root", compatibility, "--json")).Report;
        True(!disabled.Success && disabled.Diagnostics.Any(d => d.Code == "SDK203"), "pack rejects disabled required analysis");
        Equal(pack.Sha256, Sha256(pack.OutputPath), "failed build preserves previous package");
        Write("Mod/Maple.Standard.csproj", projectText);
    }
}
