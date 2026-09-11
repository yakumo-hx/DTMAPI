using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.Loader;
using System.Security;
using System.Xml.Linq;
using DTMAPI.Authoring.Contracts;

namespace DTMAPI.AuthorSdk.Tests;

internal static partial class Program
{
    private static async Task TestStandardAssets(string temporary)
    {
        string repository = FindRepository(), workspace = Path.Combine(temporary, "standard NuGet assets");
        string compatibility = Path.Combine(repository, ".tools/author-sdk-compatibility/0.7.0");
        string dotnet = Environment.GetEnvironmentVariable("DTMAPI_AUTHOR_DOTNET") ?? Path.Combine(repository, ".tools/dotnet/dotnet.exe");
        string sdkRoot = Path.GetDirectoryName(typeof(DTMAPI.AuthorSdk.AuthorApplication).Assembly.Location)!;
        string Xml(string text) => SecurityElement.Escape(text)!;
        void Write(string path, string value) { path = Path.Combine(workspace, path); Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllText(path, value); }
        async Task DotNet(params string[] args)
        {
            var info = new ProcessStartInfo(dotnet) { WorkingDirectory = workspace, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (string arg in args) info.ArgumentList.Add(arg);
            using var process = Process.Start(info)!;
            Task<string> output = process.StandardOutput.ReadToEndAsync(), error = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            True(process.ExitCode == 0, "standard NuGet fixture compiler: " + await output + await error);
        }
        Write("global.json", "{\"sdk\":{\"version\":\"8.0.421\",\"rollForward\":\"disable\"}}");
        Write("Directory.Build.props", $"<Project><PropertyGroup><LangVersion>10.0</LangVersion><DTMAPI_AUTHOR_SDK_ROOT>{Xml(sdkRoot)}</DTMAPI_AUTHOR_SDK_ROOT><DtmApiDotNet>{Xml(dotnet)}</DtmApiDotNet><DtmApiCompatibilityRoot>{Xml(compatibility)}</DtmApiCompatibilityRoot><PathMap>{Xml(workspace)}=/_/asset-test</PathMap></PropertyGroup></Project>");
        Write("Library/Maple.AssetProbe.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>netstandard2.0</TargetFramework><Version>1.0.0</Version><ProduceReferenceAssembly>true</ProduceReferenceAssembly></PropertyGroup></Project>");
        Write("Library/Value.cs", "namespace Maple.AssetProbe; public class Base<T>{ public T Echo(T value)=>value;} public sealed class Child:Base<int>{ public static int Number=>61; }");
        await DotNet("pack", Path.Combine(workspace, "Library/Maple.AssetProbe.csproj"), "-c", "Release", "-o", Path.Combine(workspace, "feed"), "--nologo", "-v:quiet");
        string package = Path.Combine(workspace, "feed/Maple.AssetProbe.1.0.0.nupkg");
        string implementation = Path.Combine(workspace, "Library/bin/Release/netstandard2.0/Maple.AssetProbe.dll");
        string reference = Path.Combine(workspace, "Library/obj/Release/netstandard2.0/ref/Maple.AssetProbe.dll");
        True(File.Exists(reference) && Sha256(reference) != Sha256(implementation), "actual compiler generated distinct ref/lib bytes");
        using (var archive = ZipFile.Open(package, ZipArchiveMode.Update))
        {
            archive.CreateEntryFromFile(reference, "ref/netstandard2.0/Maple.AssetProbe.dll");
            using var writer = new StreamWriter(archive.CreateEntry("buildTransitive/Maple.AssetProbe.targets").Open());
            writer.Write("<Project><Target Name=\"MaplePackageGenerator\" BeforeTargets=\"CoreCompile\"><WriteLinesToFile File=\"$(IntermediateOutputPath)PackageGenerated.g.cs\" Lines=\"public static class PackageGenerated {public const int Value=2%3B}\" Overwrite=\"true\"/><ItemGroup><Compile Include=\"$(IntermediateOutputPath)PackageGenerated.g.cs\"/></ItemGroup></Target></Project>");
        }
        const string centralVersions = "<Project><PropertyGroup><ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally></PropertyGroup><ItemGroup><PackageVersion Include=\"Maple.AssetProbe\" Version=\"1.0.0\"/><PackageVersion Include=\"Newtonsoft.Json\" Version=\"13.0.3\"/></ItemGroup></Project>";
        Write("Directory.Packages.props", centralVersions);
        string mod = Path.Combine(workspace, "Mod"), cache = Path.Combine(workspace, "cache");
        True((await Run("new", "codemod", mod, "--id", "Maple.AssetMod", "--name", "Assets", "--author", "Maple", "--api-target", "0.7.0", "--json")).Report.Success, "new standard asset Mod");
        Write("Mod/dtmapi.author.json", "{\"schemaVersion\":4,\"projectKind\":\"CodeMod\",\"codeModKind\":\"Strict\",\"projectFile\":\"Maple.AssetMod.csproj\",\"targetDtmApiVersion\":\"0.7.0\"}");
        string project = $"""
            <Project>
              <Import Project="Sdk.props" Sdk="Microsoft.NET.Sdk" />
              <Import Project="$(DTMAPI_AUTHOR_SDK_ROOT)/build/DTMAPI.Author.props" />
              <PropertyGroup><RestorePackagesPath>{Xml(cache)}</RestorePackagesPath><RestoreAdditionalProjectSources>{Xml(Path.Combine(workspace,"feed"))}</RestoreAdditionalProjectSources></PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Maple.AssetProbe" PrivateAssets="all" DtmApiDistribution="self-authored" />
                <PackageReference Include="Newtonsoft.Json" DtmApiDistribution="licensed-third-party" DtmApiLicenseFiles="Newtonsoft-LICENSE.txt" />
              </ItemGroup>
              <Import Project="Sdk.targets" Sdk="Microsoft.NET.Sdk" />
              <Import Project="$(DTMAPI_AUTHOR_SDK_ROOT)/build/DTMAPI.Author.targets" />
            </Project>
            """;
        Write("Mod/Maple.AssetMod.csproj", project);
        Write("Mod/Newtonsoft-LICENSE.txt", File.ReadAllText(Path.Combine(repository, "author-sdk/licenses/Newtonsoft.Json-13.0.3-LICENSE.txt")));
        Write("Mod/src/ModEntry.cs", "using DTMAPI.Abstractions; namespace Maple.AssetMod; public sealed class ModEntry:DtmMod { public override void Entry(IDtmHelper helper){} public static int Result=>new Maple.AssetProbe.Child().Echo(Maple.AssetProbe.Child.Number)+PackageGenerated.Value-2; }");
        var noLock = (await Run("pack", mod, "--compatibility-root", compatibility, "--json")).Report;
        True(!noLock.Success && noLock.Diagnostics.Any(d => d.Code == "SDK206"), "pack refuses missing standard lock before restore writes it: " + string.Join(";", noLock.Diagnostics.Select(d=>d.Message)));
        True(!File.Exists(Path.Combine(mod, "packages.lock.json")), "pack did not create a missing lock");
        var restore = (await Run("restore", mod, "--json")).Report;
        True(restore.Success, "ordinary standard restore: " + string.Join(";", restore.Diagnostics.Select(d=>d.Message)));
        var locked = (await Run("restore", mod, "--locked", "true", "--offline", "true", "--json")).Report;
        True(locked.Success, "locked offline cache replay: " + string.Join(";", locked.Diagnostics.Select(d=>d.Message)));
        var build = (await Run("build", mod, "--compatibility-root", compatibility, "--offline", "true", "--json")).Report;
        True(build.Success, "compile from actual ref assets: " + string.Join(";", build.Diagnostics.Select(d=>d.Message)));
        True(AssemblyName.GetAssemblyName(build.OutputPath).Version!.ToString() == "1.0.0.0", "omitted Version retains standard SDK GenerateAssemblyInfo default");
        var facts = XDocument.Load(build.Values["buildFactsPath"]);
        True(facts.Descendants("Item").Any(i => (string?)i.Attribute("Kind") == "Reference" && ((string?)i.Attribute("Path"))!.Replace('\\','/').EndsWith("ref/netstandard2.0/Maple.AssetProbe.dll")), "Csc uses real NuGet ref");
        var packed = (await Run("pack", mod, "--compatibility-root", compatibility, "--offline", "true", "--json")).Report;
        True(packed.Success, "different ref/lib runtime closure pack: " + string.Join(";", packed.Diagnostics.Select(d=>d.Message)));
        using (var zip = ZipFile.OpenRead(packed.OutputPath))
        {
            using var library = zip.GetEntry("lib/private/Maple.AssetProbe.dll")!.Open();
            using var bytes = new MemoryStream(); library.CopyTo(bytes);
            True(bytes.ToArray().SequenceEqual(File.ReadAllBytes(implementation)), "only actual implementation bytes enter package");
            True(zip.GetEntry("licenses/Newtonsoft.Json/Newtonsoft-LICENSE.txt") != null, "real external package license included");
            True(!zip.Entries.Any(e => e.FullName.Contains("buildTransitive") || e.FullName.EndsWith(".targets")), "NuGet buildTransitive executes without shipping build files");
        }
        var loader = new AssemblyLoadContext("standard-ref-lib", true);
        loader.Resolving += (_, name) => name.Name == "DTMAPI.Abstractions" ? typeof(DTMAPI.Abstractions.DtmMod).Assembly : loader.LoadFromStream(new MemoryStream(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(build.OutputPath)!, name.Name + ".dll"))));
        var assembly = loader.LoadFromStream(new MemoryStream(File.ReadAllBytes(build.OutputPath)));
        True((int)assembly.GetType("Maple.AssetMod.ModEntry")!.GetProperty("Result")!.GetValue(null)! == 61, "generic inherited member actually resolves to runtime lib"); loader.Unload();
        string cachedDll = Path.Combine(cache, "maple.assetprobe/1.0.0/lib/netstandard2.0/Maple.AssetProbe.dll");
        byte[] original = File.ReadAllBytes(cachedDll);
        try
        {
            File.WriteAllBytes(cachedDll, File.ReadAllBytes(reference));
            var tampered = (await Run("build", mod, "--compatibility-root", compatibility, "--offline", "true", "--json")).Report;
            True(!tampered.Success && tampered.Diagnostics.Any(d => d.Message.Contains("nuget-extracted-asset-changed", StringComparison.Ordinal)), "changed runtime asset rejected against original nupkg");
        }
        finally { File.WriteAllBytes(cachedDll, original); }
        foreach (string id in new[] { "maple.assetprobe/1.0.0/maple.assetprobe.1.0.0.nupkg", "newtonsoft.json/13.0.3/newtonsoft.json.13.0.3.nupkg" })
        {
            string path = Path.Combine(cache, id); byte[] archiveBytes = File.ReadAllBytes(path);
            try
            {
                using (var zip = ZipFile.Open(path, ZipArchiveMode.Update))
                { var extra = zip.CreateEntry("tampered.txt"); using var writer = new StreamWriter(extra.Open()); writer.Write("changed archive"); }
                var tampered = (await Run("build", mod, "--compatibility-root", compatibility, "--offline", "true", "--json")).Report;
                True(!tampered.Success && tampered.Diagnostics.Any(d => d.Code == "SDK204" && d.Message.Contains("nuget-", StringComparison.Ordinal)), "unsigned/signed archive integrity refuses " + id);
            }
            finally { File.WriteAllBytes(path, archiveBytes); }
        }
        Write("Bad/Maple.AssetProbe.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>netstandard2.0</TargetFramework><Version>1.0.0</Version></PropertyGroup></Project>");
        Write("Bad/Value.cs", "namespace Maple.AssetProbe; public class Base<T>{public T Echo(T value)=>value;} public sealed class Child:Base<int>{}");
        await DotNet("build", Path.Combine(workspace, "Bad/Maple.AssetProbe.csproj"), "-c", "Release", "--nologo", "-v:quiet");
        Write("Mod/Maple.AssetMod.csproj", project.Replace("</Project>", $"<Target Name=\"ReplaceRuntime\" AfterTargets=\"Build\"><Copy SourceFiles=\"{Xml(Path.Combine(workspace,"Bad/bin/Release/netstandard2.0/Maple.AssetProbe.dll"))}\" DestinationFiles=\"$(TargetDir)Maple.AssetProbe.dll\" /></Target></Project>"));
        var missingMember = (await Run("pack", mod, "--compatibility-root", compatibility, "--offline", "true", "--json")).Report;
        True(!missingMember.Success && missingMember.Diagnostics.Any(d=>d.Message.Contains("runtime-member-missing", StringComparison.Ordinal)), "postprocessed implementation must satisfy actual MemberRef: " + string.Join(";", missingMember.Diagnostics.Select(d=>d.Message)));
        Write("Bad/Value.cs", "namespace Maple.AssetProbe; public class Base<T>{public T Echo(T value)=>value;} public sealed class Child:Base<int>{private static int Number=>61;}");
        await DotNet("build", Path.Combine(workspace, "Bad/Maple.AssetProbe.csproj"), "-c", "Release", "--nologo", "-v:quiet");
        var inaccessible = (await Run("pack", mod, "--compatibility-root", compatibility, "--offline", "true", "--output", Path.Combine(workspace, "inaccessible.zip"), "--json")).Report;
        True(!inaccessible.Success && inaccessible.Diagnostics.Any(d => d.Message.Contains("runtime-member-inaccessible", StringComparison.Ordinal)), "same signature but private final implementation is not callable from the Mod");
        Write("Bad/Value.cs", "namespace Maple.AssetProbe; public class Base<T>{public T Echo(T value)=>value;} public sealed class Child:Base<int>{internal static int Number=>61;}");
        await DotNet("build", Path.Combine(workspace, "Bad/Maple.AssetProbe.csproj"), "-c", "Release", "--nologo", "-v:quiet");
        inaccessible = (await Run("pack", mod, "--compatibility-root", compatibility, "--offline", "true", "--output", Path.Combine(workspace, "internal.zip"), "--json")).Report;
        True(!inaccessible.Success && inaccessible.Diagnostics.Any(d => d.Message.Contains("runtime-member-inaccessible", StringComparison.Ordinal)), "internal final member requires friend access");
        Write("Bad/Value.cs", "[assembly:System.Runtime.CompilerServices.InternalsVisibleTo(\"Maple.AssetMod\")] namespace Maple.AssetProbe; public class Base<T>{public T Echo(T value)=>value;} public sealed class Child:Base<int>{internal static int Number=>61;}");
        await DotNet("build", Path.Combine(workspace, "Bad/Maple.AssetProbe.csproj"), "-c", "Release", "--nologo", "-v:quiet");
        var friendPack = (await Run("pack", mod, "--compatibility-root", compatibility, "--offline", "true", "--output", Path.Combine(workspace, "friend.zip"), "--json")).Report;
        True(friendPack.Success, "actual InternalsVisibleTo permits the selected internal implementation: " + string.Join(";", friendPack.Diagnostics.Select(d => d.Message)));
        Write("Mod/Maple.AssetMod.csproj", project);
        Write("Directory.Packages.props", centralVersions.Replace("Version=\"1.0.0\"", "Version=\"2.0.0\""));
        var mismatch = (await Run("pack", mod, "--compatibility-root", compatibility, "--offline", "true", "--json")).Report;
        True(!mismatch.Success && mismatch.Diagnostics.Any(d => d.Code == "NU1004"), "standard locked restore refuses changed requested version");
        Write("Directory.Packages.props", centralVersions);
        Write("Mod/Maple.AssetMod.csproj", project.Replace(Xml(cache), Xml(Path.Combine(workspace, "empty-cache"))));
        var missing = (await Run("restore", mod, "--locked", "true", "--offline", "true", "--json")).Report;
        True(!missing.Success && missing.Diagnostics.Any(d => d.Code.StartsWith("NU", StringComparison.Ordinal)), "offline empty cache has actual NuGet failure");
        Equal(packed.Sha256, Sha256(packed.OutputPath), "restore failures preserve published package");
        Write("Mod/Maple.AssetMod.csproj", project);
    }
}
