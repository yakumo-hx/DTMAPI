using System.IO.Compression;
using System.Text;
using System.Text.Json.Nodes;
using System.Security.Cryptography;

namespace DTMAPI.AuthorSdk.Tests;

internal static partial class Program
{
    private static async Task TestLockedRestore(string temporary)
    {
        string root = Path.Combine(temporary, "locked restore"), library = Path.Combine(root, "Library"), mod = Path.Combine(root, "Mod"), feed = Path.Combine(root, "feed");
        string compatibility = Path.Combine(FindRepository(), ".tools", "author-sdk-compatibility", "0.6.4");
        True((await Run("new", "library", library, "--id", "Aspen.RestoreLibrary", "--api-target", "0.6.4", "--json")).Report.Success, "restore fixture library");
        File.WriteAllText(Path.Combine(library, "src", "Library.cs"), "namespace Aspen.RestoreLibrary; public delegate int TransformValue(int value); public delegate T TransformGeneric<T>(T value); public delegate T RefTransform<T>(ref T value, out int count); public static class Library { public static int Value { get { TransformValue plain = x => x + 1; TransformGeneric<int> generic = x => x * 2; return generic(plain(20)); } } }");
        var build = (await Run("build", library, "--compatibility-root", compatibility, "--json")).Report;
        True(build.Success, "restore fixture build: " + string.Join(";", build.Diagnostics.Select(d => d.Message)));
        byte[] dll = File.ReadAllBytes(build.OutputPath);
        True((await Run("new", "codemod", mod, "--id", "Aspen.RestoreMod", "--name", "Restore", "--author", "Aspen", "--api-target", "0.6.4", "--json")).Report.Success, "restore fixture Mod");
        Directory.CreateDirectory(feed); File.WriteAllText(Path.Combine(mod, "LICENSE.txt"), "Self-authored package fixture, permission to use and redistribute for this test.");
        string package = Path.Combine(feed, "Aspen.Package.1.0.0.nupkg");
        void Package(string? extra = null, byte[]? assembly = null, IReadOnlyDictionary<string, byte[]>? extras = null, bool includeLib = true)
        {
            using var zip = ZipFile.Open(package, ZipArchiveMode.Create);
            void Add(string name, byte[] bytes) { using var stream = zip.CreateEntry(name).Open(); stream.Write(bytes); }
            Add("Aspen.Package.nuspec", Encoding.UTF8.GetBytes("<?xml version=\"1.0\"?><package><metadata><id>Aspen.Package</id><version>1.0.0</version><authors>Aspen</authors><description>Self-authored test</description><dependencies><group targetFramework=\"netstandard2.0\" /></dependencies></metadata></package>"));
            if (includeLib) Add("lib/netstandard2.0/Aspen.RestoreLibrary.dll", assembly ?? dll);
            if (extra != null) Add(extra, new byte[] { 1, 2, 3 });
            if (extras != null) foreach (var file in extras) Add(file.Key, file.Value);
        }
        Package();
        string authorPath = Path.Combine(mod, "dtmapi.author.json");
        var author = JsonNode.Parse(File.ReadAllText(authorPath))!.AsObject();
        author["build"] = new JsonObject { ["restore"] = new JsonObject { ["sources"] = new JsonArray(feed), ["packages"] = new JsonObject { ["Aspen.Package"] = "1.0.0" },
            ["licenseFiles"] = new JsonObject { ["Aspen.Package"] = new JsonArray("LICENSE.txt") } } };
        File.WriteAllText(authorPath, author.ToJsonString());
        JsonObject Lock() => new() { ["version"] = 1, ["dependencies"] = new JsonObject { [".NETStandard,Version=v2.0"] = new JsonObject { ["Aspen.Package"] = new JsonObject
            { ["type"] = "Direct", ["requested"] = "[1.0.0, 1.0.0]", ["resolved"] = "1.0.0", ["contentHash"] = Convert.ToBase64String(SHA512.HashData(File.ReadAllBytes(package))) } } } };
        string lockPath = Path.Combine(mod, "packages.lock.json"); File.WriteAllText(lockPath, Lock().ToJsonString());
        var empty = (await Run("restore", mod, "--offline", "true", "--json")).Report;
        True(!empty.Success && empty.Diagnostics.Any(d => d.Message.Contains("restore-cache-missing")), "explicit offline empty-cache diagnostic");
        True(!(await Run("build", mod, "--compatibility-root", compatibility, "--json")).Report.Success, "build never restores missing package");
        var restored = (await RunPublic("restore", mod, "--json")).Report;
        True(restored.Success, "explicit local feed restore: " + string.Join(";", restored.Diagnostics.Select(d => d.Message)));
        True((await RunPublic("restore", mod, "--offline", "true", "--json")).Report.Success, "offline warm cache");
        File.WriteAllText(Path.Combine(mod, "src", "ModEntry.cs"), "using DTMAPI.Abstractions; namespace Aspen.RestoreMod; public sealed class ModEntry:DtmMod { public override void Entry(IDtmHelper h){h.Monitor.Log(Aspen.RestoreLibrary.Library.Value.ToString());} }");
        True((await RunPublic("build", mod, "--compatibility-root", compatibility, "--json")).Report.Success, "public build uses delegate package");
        var packed = (await RunPublic("pack", mod, "--compatibility-root", compatibility, "--json")).Report;
        True(packed.Success, "restored library pack: " + string.Join(";", packed.Diagnostics.Select(d => d.Message)));
        using (var zip = ZipFile.OpenRead(packed.OutputPath))
        {
            True(zip.GetEntry("lib/private/Aspen.RestoreLibrary.dll") != null, "restored library package");
            True(zip.GetEntry("licenses/Aspen.Package/LICENSE.txt") != null, "restored license package");
        }
        string ordinaryMod = Path.Combine(root, "OrdinaryMod"), ordinary = Path.Combine(root, "Plain");
        await ExpectSuccess("ordinary locked parent", "new", "codemod", ordinaryMod, "--id", "Aspen.PlainCaller", "--name", "Plain", "--author", "Aspen", "--api-target", "0.6.4");
        Directory.CreateDirectory(ordinary);
        File.WriteAllText(Path.Combine(ordinary, "Plain.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>netstandard2.0</TargetFramework></PropertyGroup><ItemGroup><PackageReference Include=\"Aspen.Package\" Version=\"1.0.0\" /></ItemGroup></Project>");
        File.WriteAllText(Path.Combine(ordinary, "Plain.cs"), "public static class Plain { public static int Value => Aspen.RestoreLibrary.Library.Value; }");
        File.WriteAllText(Path.Combine(ordinary, "packages.lock.json"), Lock().ToJsonString());
        string ordinaryAuthorPath = Path.Combine(ordinaryMod, "dtmapi.author.json");
        var ordinaryAuthor = JsonNode.Parse(File.ReadAllText(ordinaryAuthorPath))!;
        string middle = Path.Combine(root, "Middle"); Directory.CreateDirectory(middle);
        File.WriteAllText(Path.Combine(middle, "Middle.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>netstandard2.0</TargetFramework></PropertyGroup><ItemGroup><ProjectReference Include=\"../Plain/Plain.csproj\" /></ItemGroup></Project>");
        File.WriteAllText(Path.Combine(middle, "Middle.cs"), "public static class Middle { public static int Value => Plain.Value; }");
        ordinaryAuthor["build"] = new JsonObject { ["workspaceRoot"] = "..", ["projectReferences"] = new JsonArray("../Middle/Middle.csproj"),
            ["restore"] = new JsonObject { ["sources"] = new JsonArray(feed), ["packages"] = new JsonObject(), ["licenseFiles"] = new JsonObject { ["Aspen.Package"] = new JsonArray("../Mod/LICENSE.txt") } } };
        File.WriteAllText(ordinaryAuthorPath, ordinaryAuthor.ToJsonString());
        File.WriteAllText(Path.Combine(ordinaryMod, "src/ModEntry.cs"), "using DTMAPI.Abstractions; public sealed class ModEntry:DtmMod { public override void Entry(IDtmHelper h){h.Monitor.Log(Plain.Value.ToString());} }");
        await ExpectSuccess("ordinary child locked restore", "restore", ordinaryMod);
        await ExpectSuccess("ordinary child offline restore", "restore", ordinaryMod, "--offline", "true");
        var ordinaryPack = await ExpectSuccess("ordinary child locked pack", "pack", ordinaryMod, "--compatibility-root", compatibility);
        using (var zip = ZipFile.OpenRead(ordinaryPack.OutputPath))
            True(zip.GetEntry("lib/private/Plain.dll") != null && zip.GetEntry("lib/private/Aspen.RestoreLibrary.dll") != null && zip.GetEntry("licenses/Aspen.Package/LICENSE.txt") != null, "Ordinary child package closure and inherited license are delivered");
        string cachePackage = Path.Combine(mod, "obj/dtmapi-author/packages/aspen.package/1.0.0/package.nupkg");
        byte[] cached = File.ReadAllBytes(cachePackage); var changed = cached.ToArray(); changed[0] ^= 1; File.WriteAllBytes(cachePackage, changed);
        True(!(await Run("restore", mod, "--offline", "true", "--json")).Report.Success, "cached package hash drift");
        File.WriteAllBytes(cachePackage, cached);
        foreach (string fault in new[] { "pinvoke", "unmanaged", "internal-call", "native-code-type", "invalid-runtime-delegate", "mixed-runtime", "delegate-shape", "non-il-only" })
        {
            using var input = new MemoryStream(dll);
            using var mutated = Mono.Cecil.AssemblyDefinition.ReadAssembly(input);
            var type = mutated.MainModule.Types.Single(t => t.Name == "Library");
            var method = type.Methods.Single(m => m.Name == "get_Value");
            if (fault == "pinvoke")
            {
                var module = new Mono.Cecil.ModuleReference("missing-native-fixture"); mutated.MainModule.ModuleReferences.Add(module);
                method.PInvokeInfo = new Mono.Cecil.PInvokeInfo(Mono.Cecil.PInvokeAttributes.CallConvCdecl, "native_value", module);
                method.IsPInvokeImpl = true; method.Body = null;
            }
            if (fault == "unmanaged") method.IsUnmanaged = true;
            if (fault == "internal-call") { method.IsInternalCall = true; method.Body = null; }
            if (fault == "native-code-type") { method.ImplAttributes = Mono.Cecil.MethodImplAttributes.Native; method.Body = null; }
            if (fault == "invalid-runtime-delegate") { method.ImplAttributes = Mono.Cecil.MethodImplAttributes.Runtime; method.Body = null; }
            if (fault is "mixed-runtime" or "delegate-shape")
            {
                method = mutated.MainModule.Types.Single(t => t.Name == "TransformValue").Methods.Single(m => m.Name == "Invoke");
                if (fault == "mixed-runtime") method.IsInternalCall = true;
                else method.Name = "PretendInvoke";
            }
            using var output = new MemoryStream(); mutated.Write(output); byte[] negative = output.ToArray();
            if (fault == "non-il-only")
            {
                using var image = new System.Reflection.PortableExecutable.PEReader(new MemoryStream(negative));
                int offset = image.PEHeaders.CorHeaderStartOffset + 16;
                negative[offset] &= 0xfe;
            }
            File.Move(package, package + ".previous"); Package(assembly: negative);
            author["build"]!["restore"]!["cacheDirectory"] = "obj/method-" + fault;
            File.WriteAllText(authorPath, author.ToJsonString()); File.WriteAllText(lockPath, Lock().ToJsonString());
            var rejected = await ExpectPublicFailure("real method classification " + fault, "restore", mod, "--json");
            string category = fault == "mixed-runtime" ? "internal-call" : fault == "delegate-shape" ? "invalid-runtime-delegate" : fault;
            True(rejected.Diagnostics.Any(d => d.Code == "SDK701" && d.Message.Contains("Aspen.Package/1.0.0/lib/netstandard2.0/Aspen.RestoreLibrary.dll")
                && (fault == "non-il-only" ? d.Message.Contains("restore-native-assembly") : d.Message.Contains(category) && d.Message.Contains(fault == "delegate-shape" ? "TransformValue" : method.Name))),
                "precise asset/member/category: " + fault + ": " + string.Join(";", rejected.Diagnostics.Select(d => d.Message)));
            File.Delete(package); File.Move(package + ".previous", package);
        }
        foreach (string fault in new[] { "runtimes/win-x64/native/test.dll", "buildTransitive/target.props", "analyzers/dotnet/cs/generator.dll", "wrong-tfm" })
        {
            File.Move(package, package + ".previous");
            byte[]? wrong = null;
            if (fault == "wrong-tfm")
            {
                wrong = dll.ToArray(); byte[] needle = Encoding.UTF8.GetBytes(".NETStandard,Version=v2.0");
                int position = wrong.AsSpan().IndexOf(needle); True(position >= 0, "fixture TFM located"); wrong[position + needle.Length - 3] = (byte)'8';
            }
            Package(fault == "wrong-tfm" ? null : fault, wrong);
            author["build"]!["restore"]!["cacheDirectory"] = "obj/fault-" + Guid.NewGuid().ToString("N");
            File.WriteAllText(authorPath, author.ToJsonString()); File.WriteAllText(lockPath, Lock().ToJsonString());
            var rejected = (await Run("restore", mod, "--json")).Report;
            True(!rejected.Success && rejected.Diagnostics.All(d => d.Code != "SDK999"), "restore explicit unsupported diagnostic: " + fault);
            File.Delete(package); File.Move(package + ".previous", package);
        }
        foreach (string scenario in new[] { "unrelated-assets", "same-ref-lib", "different-ref-lib", "only-ref", "applicable-build", "applicable-generator", "placeholder-nearest" })
        {
            var extras = new Dictionary<string, byte[]>();
            if (scenario == "unrelated-assets")
            {
                extras["lib/net8.0/Aspen.RestoreLibrary.dll"] = new byte[] { 1, 2, 3 };
                extras["ref/net8.0/Aspen.RestoreLibrary.dll"] = new byte[] { 4, 5, 6 };
                extras["build/net8.0/Aspen.Package.targets"] = Encoding.UTF8.GetBytes("<Project><Target Name=\"MustNotRun\" /></Project>");
                extras["buildTransitive/net8.0/Aspen.Package.props"] = Encoding.UTF8.GetBytes("<Project />");
                extras["runtimes/linux-x64/lib/net8.0/Aspen.RestoreLibrary.dll"] = new byte[] { 1 };
            }
            if (scenario is "same-ref-lib" or "different-ref-lib" or "only-ref") extras["ref/netstandard2.0/Aspen.RestoreLibrary.dll"] = scenario == "different-ref-lib" ? dll.Concat(new byte[] { 0 }).ToArray() : dll;
            if (scenario == "applicable-build") extras["build/netstandard2.0/Aspen.Package.targets"] = Encoding.UTF8.GetBytes("<Project />");
            if (scenario == "applicable-generator") extras["analyzers/dotnet/cs/Generator.dll"] = dll;
            if (scenario == "placeholder-nearest")
            {
                extras["build/netstandard1.0/Aspen.Package.targets"] = Encoding.UTF8.GetBytes("<Project />");
                extras["build/netstandard2.0/_._"] = Array.Empty<byte>();
            }
            File.Move(package, package + ".previous"); Package(extras: extras, includeLib: scenario != "only-ref");
            author["build"]!["restore"]!["cacheDirectory"] = "obj/assets-" + scenario;
            File.WriteAllText(authorPath, author.ToJsonString()); File.WriteAllText(lockPath, Lock().ToJsonString());
            bool allowed = scenario is "unrelated-assets" or "same-ref-lib" or "placeholder-nearest";
            if (allowed)
            {
                var selected = await ExpectSuccess("selected assets " + scenario, "restore", mod);
                True(selected.Values.Any(p => p.Key.EndsWith("/lib/netstandard2.0/Aspen.RestoreLibrary.dll") && p.Value.StartsWith("selected:")), "Restore reports the execution asset");
                if (scenario != "same-ref-lib") True(selected.Values.Any(p => p.Value.StartsWith("excluded:")), "Restore reports excluded groups");
                await ExpectSuccess("selected assets offline " + scenario, "restore", mod, "--offline", "true");
                await ExpectSuccess("selected assets build " + scenario, "build", mod, "--compatibility-root", compatibility);
                var selectedPack = await ExpectSuccess("selected assets pack " + scenario, "pack", mod, "--compatibility-root", compatibility);
                using var zip = ZipFile.OpenRead(selectedPack.OutputPath);
                True(zip.Entries.All(e => !e.FullName.StartsWith("ref/") && !e.FullName.Contains("net8.0") && !e.FullName.StartsWith("build/")), "Only selected execution DLL is packaged");
            }
            else
            {
                var rejected = await ExpectPublicFailure("selected asset rejection " + scenario, "restore", mod);
                string category = scenario == "different-ref-lib" ? "restore-unsupported-ref-runtime-model" : scenario == "only-ref" ? "restore-unsupported-tfm" : "restore-unsupported-assets";
                True(rejected.Diagnostics.Any(d => d.Code == "SDK701" && d.Message.Contains(category)), "Specific asset model rejection " + scenario);
                True(rejected.Values.Any(p => p.Key.StartsWith("asset.Aspen.Package/1.0.0/")), "Rejected selection retains its actual asset report");
            }
            File.Delete(package); File.Move(package + ".previous", package);
        }
    }
}
