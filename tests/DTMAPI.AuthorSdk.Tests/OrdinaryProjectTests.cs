using System.IO.Compression;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json.Nodes;
using System.Xml.Linq;

namespace DTMAPI.AuthorSdk.Tests;

internal static partial class Program
{
    private static async Task TestOrdinaryProjects(string temporary)
    {
        string root = Path.Combine(temporary, "ordinary graph"), mod = Path.Combine(root, "Mod"), library = Path.Combine(root, "Library"), leaf = Path.Combine(root, "Leaf"), sdk = Path.Combine(root, "SDK");
        string compatibility = Path.Combine(FindRepository(), ".tools", "author-sdk-compatibility", "0.7.0");
        await ExpectSuccess("ordinary graph entry", "new", "codemod", mod, "--id", "Cedar.Options", "--name", "Options", "--author", "Cedar");
        Directory.CreateDirectory(sdk); Directory.CreateDirectory(library); Directory.CreateDirectory(leaf);
        File.WriteAllText(Path.Combine(sdk, "Cedar.Shared.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>netstandard2.0</TargetFramework></PropertyGroup></Project>");
        File.WriteAllText(Path.Combine(sdk, "Library.cs"), "namespace Cedar.Shared { public static class Library { public static int Value => 42; } }");
        File.WriteAllText(Path.Combine(leaf, "Leaf.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>netstandard2.0</TargetFramework><DefineConstants>LEAF_ONLY</DefineConstants></PropertyGroup></Project>");
        File.WriteAllText(Path.Combine(leaf, "Leaf.cs"), """
            #if !LEAF_ONLY || TRACE || !NETSTANDARD1_0_OR_GREATER || !NETSTANDARD1_6_OR_GREATER || !NETSTANDARD2_0_OR_GREATER || (!DEBUG && !RELEASE)
            #error Ordinary library effective constants differ from Microsoft.NET.Sdk
            #endif
            public static class Leaf { public static int Value => 1; }
            """);
        File.WriteAllText(Path.Combine(root, "Linked.cs"), "public static class Linked { public const int Value=3; }");
        string ordinaryProject = Path.Combine(library, "OriginalFilename.csproj");
        const string ordinaryXml = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup><TargetFramework>netstandard2.0</TargetFramework><AssemblyName>Bare</AssemblyName><RootNamespace>BareSpace</RootNamespace><Version>2.3.4</Version><AssemblyVersion>2.3.4.5</AssemblyVersion><FileVersion>2.3.4.6</FileVersion><GenerateDocumentationFile>true</GenerateDocumentationFile></PropertyGroup>
              <ItemGroup><Compile Remove="Excluded.cs"/><Compile Include="../Linked.cs" Link="Linked.cs"/><ProjectReference Include="../Leaf/Leaf.csproj"/><EmbeddedResource Include="note.txt"><LogicalName>Bare.Note</LogicalName></EmbeddedResource></ItemGroup>
            </Project>
            """;
        File.WriteAllText(ordinaryProject, ordinaryXml);
        File.WriteAllText(Path.Combine(library, "note.txt"), "R");
        File.WriteAllText(Path.Combine(library, "Strings.resx"), "<root><resheader name=\"resmimetype\"><value>text/microsoft-resx</value></resheader><resheader name=\"version\"><value>2.0</value></resheader><data name=\"Greeting\" xml:space=\"preserve\"><value>Hello</value></data></root>");
        File.WriteAllText(Path.Combine(library, "Excluded.cs"), "this must not compile");
        foreach (string directory in new[] { "bin", "obj" }) { Directory.CreateDirectory(Path.Combine(library, directory)); File.WriteAllText(Path.Combine(library, directory, "Noise.cs"), "also not C#"); }
        File.WriteAllText(Path.Combine(library, "Library.cs"), """
            /// <summary>Ordinary reusable library.</summary>
            public static class Bare {
              /// <summary>Reads a real embedded resource and a transitive library.</summary>
              public static int Value { get { using(var stream=typeof(Bare).Assembly.GetManifestResourceStream("Bare.Note")) return Leaf.Value + Linked.Value + (int)stream.Length + new System.Resources.ResourceManager("BareSpace.Strings",typeof(Bare).Assembly).GetString("Greeting").Length - 5; } }
            }
            """);
        string projectPath = Path.Combine(mod, "Cedar.Options.csproj"), originalProject = File.ReadAllText(projectPath);
        string options = "<PropertyGroup><DefineConstants>$(DefineConstants);AUTHOR_FEATURE;AUTHOR_FEATURE</DefineConstants><GenerateDocumentationFile>true</GenerateDocumentationFile></PropertyGroup><PropertyGroup Condition=\"'$(Configuration)' == 'Release'\"><DefineConstants>$(DefineConstants);AUTHOR_RELEASE</DefineConstants></PropertyGroup>";
        options += "<ItemDefinitionGroup><ReferenceCopyLocalPaths><DtmApiDistribution>self-authored</DtmApiDistribution></ReferenceCopyLocalPaths></ItemDefinitionGroup><ItemGroup><ProjectReference Include=\"../Library/OriginalFilename.csproj\" DtmApiDistribution=\"self-authored\"/><ProjectReference Include=\"../SDK/Cedar.Shared.csproj\" DtmApiRole=\"shared-contract\" DtmApiDistribution=\"self-authored\"/></ItemGroup>";
        File.WriteAllText(projectPath, originalProject.Replace("<Import Project=\"Sdk.targets\"", options + "<Import Project=\"Sdk.targets\""));
        await ExpectSuccess("standard ordinary graph restore", "restore", mod);
        string source = Path.Combine(mod, "src", "ModEntry.cs");
        File.WriteAllText(source, """
            using DTMAPI.Abstractions;
            namespace Cedar.Options;
            /// <summary>Documented entry.</summary>
            public sealed class ModEntry:DtmMod {
             /// <summary>Real conditional compilation result.</summary>
             public static int Value => Bare.Value + Cedar.Shared.Library.Value
            #if AUTHOR_FEATURE && AUTHOR_RELEASE
              + 7;
            #else
              + 100;
            #endif
             public override void Entry(IDtmHelper h){h.Monitor.Log(Value.ToString());}
            }
            """);
        var release = await ExpectSuccess("ordinary library and constants pack", "pack", mod, "--compatibility-root", compatibility);
        var repeated = await ExpectSuccess("incremental standard author options", "build", mod, "--compatibility-root", compatibility);
        Equal(release.Values["buildOutputSha256"], repeated.Sha256, "Incremental standard DLL");
        Equal(release.Values["symbolsSha256"], repeated.Values["symbolsSha256"], "Incremental standard PDB");
        True(XDocument.Load(release.Values["buildFactsPath"]).Descendants("Item").Any(e => (string?)e.Attribute("Kind") == "CompilerArgument" && ((string?)e.Attribute("Path"))?.Contains("AUTHOR_RELEASE") == true), "Actual Csc receives author conditional constants");
        True(XDocument.Load(release.Values["documentationPath"]).Descendants("member").Any(e => (string?)e.Attribute("name") == "P:Cedar.Options.ModEntry.Value"), "XML comes from current API source");
        string extracted = Path.Combine(root, "extracted"); ZipFile.ExtractToDirectory(release.OutputPath, extracted);
        True(File.Exists(Path.Combine(extracted, "lib/private/Bare.xml")) && File.Exists(Path.Combine(extracted, "lib/shared/Cedar.Shared.dll")), "Library documentation and retained shared contract are packaged");
        True(!File.Exists(Path.Combine(library, "manifest.json")) && !File.Exists(Path.Combine(library, "dtmapi.library.json")) && File.ReadAllText(ordinaryProject) == ordinaryXml, "Ordinary source project is unchanged and has no Mod identity");
        int Value(string dll, string dependencyRoot)
        {
            var loader = new AssemblyLoadContext("ordinary-options-" + Guid.NewGuid(), true);
            loader.Resolving += (_, name) => name.Name == "DTMAPI.Abstractions" ? typeof(DTMAPI.Abstractions.DtmMod).Assembly :
                loader.LoadFromStream(new MemoryStream(File.ReadAllBytes(Directory.EnumerateFiles(dependencyRoot, name.Name + ".dll", SearchOption.AllDirectories).Single())));
            var assembly = loader.LoadFromStream(new MemoryStream(File.ReadAllBytes(dll)));
            int value = (int)assembly.GetType("Cedar.Options.ModEntry")!.GetProperty("Value")!.GetValue(null)!;
            var bare = loader.Assemblies.Single(a => a.GetName().Name == "Bare"); Equal("2.3.4.5", bare.GetName().Version!.ToString(), "Ordinary library owns its assembly version");
            loader.Unload(); return value;
        }
        True(Value(Path.Combine(extracted, "Content/DTMAPI/Cedar.Options.dll"), Path.Combine(extracted, "lib")) == 54, "Actual Release branch, ordinary/transitive/shared library and resource calls");
        var debug = await ExpectSuccess("conditional Debug", "build", mod, "--configuration", "Debug", "--compatibility-root", compatibility);
        True(Value(debug.OutputPath, Path.GetDirectoryName(debug.OutputPath)!) == 147, "Actual Debug branch");
        string dllHash = PathSafety.Sha256File(debug.OutputPath), xmlPath = debug.Values["documentationPath"], xmlHash = PathSafety.Sha256File(xmlPath), validSource = File.ReadAllText(source);
        File.AppendAllText(source, "\ninvalid C#");
        _ = await ExpectFailure("compile failure preserves DLL/XML", "build", mod, "--configuration", "Debug", "--compatibility-root", compatibility);
        Equal(dllHash, PathSafety.Sha256File(debug.OutputPath), "Failed compile preserves DLL"); Equal(xmlHash, PathSafety.Sha256File(xmlPath), "Failed compile preserves XML");
        File.WriteAllText(source, validSource);
        using (var locked = new FileStream(xmlPath, FileMode.Open, FileAccess.Read, FileShare.None))
            _ = await ExpectFailure("locked XML refuses package", "pack", mod, "--configuration", "Debug", "--compatibility-root", compatibility);
        File.WriteAllText(projectPath, File.ReadAllText(projectPath).Replace("<GenerateDocumentationFile>true</GenerateDocumentationFile>", "<GenerateDocumentationFile>false</GenerateDocumentationFile>"));
        var noDocs = await ExpectSuccess("disable documentation", "pack", mod, "--configuration", "Debug", "--compatibility-root", compatibility, "--output", Path.Combine(root, "no-docs.zip"));
        using (var zip = ZipFile.OpenRead(noDocs.OutputPath)) True(zip.GetEntry("Content/DTMAPI/Cedar.Options.xml") == null, "Disabled documentation never packages stale XML");
        File.WriteAllText(ordinaryProject, ordinaryXml.Replace("</Project>", "<Target Name=\"GenerateMore\" AfterTargets=\"Build\"><WriteLinesToFile File=\"$(TargetDir)ordinary-target.txt\" Lines=\"EXECUTED\" Overwrite=\"true\"/></Target></Project>"));
        await ExpectSuccess("ordinary custom target executes", "build", mod, "--compatibility-root", compatibility);
        True(File.ReadAllText(Path.Combine(library, "bin/Release/netstandard2.0/ordinary-target.txt")).Trim() == "EXECUTED", "Standard child target is honored");
        File.WriteAllText(ordinaryProject, ordinaryXml);
        File.WriteAllText(projectPath, File.ReadAllText(projectPath).Replace("$(DefineConstants);AUTHOR_FEATURE;AUTHOR_FEATURE", "BAD-NAME"));
        var invalid = await ExpectSuccess("standard invalid constant warning", "build", mod, "--compatibility-root", compatibility);
        True(invalid.Diagnostics.Any(d => d.Code == "MSB3052"), "Invalid constant is a standard compiler task diagnostic");
    }
}
