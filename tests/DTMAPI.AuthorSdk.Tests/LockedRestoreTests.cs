using System.Xml.Linq;

namespace DTMAPI.AuthorSdk.Tests;

internal static partial class Program
{
    // Standard restore/ref/lib cases live in StandardAssetTests. These cases keep
    // the existing method/PE delivery gates, now exercised on final MSBuild output.
    private static async Task TestFinalManagedAssets(string temporary)
    {
        string root = Path.Combine(temporary, "final managed assets"), library = Path.Combine(root, "Library"), mod = Path.Combine(root, "Mod");
        string compatibility = Path.Combine(FindRepository(), ".tools/author-sdk-compatibility/0.7.0");
        Directory.CreateDirectory(library);
        File.WriteAllText(Path.Combine(library, "Aspen.RestoreLibrary.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>netstandard2.0</TargetFramework><DebugType>none</DebugType></PropertyGroup></Project>");
        File.WriteAllText(Path.Combine(library, "Library.cs"), "namespace Aspen { public delegate int TransformValue(int value); public static class Library { public static int Value { get { TransformValue transform = value => value + 1; return transform(41); } } } }");
        await ExpectSuccess("create final asset Mod", "new", "codemod", mod, "--id", "Aspen.RestoreMod", "--name", "Assets", "--author", "Aspen");
        string projectPath = Path.Combine(mod, "Aspen.RestoreMod.csproj");
        string project = File.ReadAllText(projectPath).Replace("</Project>", "<ItemGroup><ProjectReference Include=\"../Library/Aspen.RestoreLibrary.csproj\" DtmApiDistribution=\"self-authored\" /></ItemGroup></Project>");
        File.WriteAllText(projectPath, project);
        File.WriteAllText(Path.Combine(mod, "src/ModEntry.cs"), "using DTMAPI.Abstractions; namespace Aspen.RestoreMod; public sealed class ModEntry:DtmMod { public override void Entry(IDtmHelper helper){helper.Monitor.Log(Aspen.Library.Value.ToString());} }");
        await ExpectSuccess("restore actual project graph", "restore", mod);
        var valid = await ExpectSuccess("normal runtime delegate pack", "pack", mod, "--compatibility-root", compatibility);
        byte[] dll = File.ReadAllBytes(Path.Combine(library, "bin/Release/netstandard2.0/Aspen.RestoreLibrary.dll"));
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
            string changed = Path.Combine(root, "postprocessed.dll"); File.WriteAllBytes(changed, negative);
            var document = XDocument.Parse(project);
            document.Root!.Add(new XElement("Target", new XAttribute("Name", "Postprocess"), new XAttribute("AfterTargets", "Build"),
                new XElement("Copy", new XAttribute("SourceFiles", changed), new XAttribute("DestinationFiles", "$(TargetDir)Aspen.RestoreLibrary.dll"))));
            document.Save(projectPath);
            var failed = await ExpectFailure("final managed method " + fault, "pack", mod, "--compatibility-root", compatibility);
            string category = fault == "mixed-runtime" ? "internal-call" : fault == "delegate-shape" ? "invalid-runtime-delegate" : fault;
            True(failed.Diagnostics.Any(d => d.Code != "SDK999" && d.Message.Contains("Aspen.RestoreLibrary") && (fault == "non-il-only" || d.Message.Contains(category))),
                "Final method/PE refusal " + fault + ": " + string.Join(";", failed.Diagnostics.Select(d => d.Message)));
            Equal(valid.Sha256, Sha256(valid.OutputPath), "method rejection preserves old package " + fault);
        }
        File.WriteAllText(projectPath, project);
        string forwarded = Path.Combine(root, "Forwarded"), facade = Path.Combine(root, "Facade");
        Directory.CreateDirectory(forwarded); Directory.CreateDirectory(facade);
        File.WriteAllText(Path.Combine(forwarded, "Aspen.ForwardedLibrary.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>netstandard2.0</TargetFramework><DebugType>none</DebugType></PropertyGroup></Project>");
        File.Copy(Path.Combine(library, "Library.cs"), Path.Combine(forwarded, "Library.cs"));
        File.WriteAllText(Path.Combine(facade, "Facade.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>netstandard2.0</TargetFramework><AssemblyName>Aspen.RestoreLibrary</AssemblyName><DebugType>none</DebugType></PropertyGroup><ItemGroup><ProjectReference Include=\"../Forwarded/Aspen.ForwardedLibrary.csproj\" /></ItemGroup></Project>");
        File.WriteAllText(Path.Combine(facade, "Forwarders.cs"), "[assembly:System.Runtime.CompilerServices.TypeForwardedTo(typeof(Aspen.Library))] [assembly:System.Runtime.CompilerServices.TypeForwardedTo(typeof(Aspen.TransformValue))]");
        File.WriteAllText(Path.Combine(mod, "src/ModEntry.cs"), "using DTMAPI.Abstractions; namespace Aspen.RestoreMod; public sealed class ModEntry:DtmMod { public static int Value => Aspen.Library.Value; public override void Entry(IDtmHelper helper){helper.Monitor.Log(Value.ToString());} }");
        var forwardedProject = XDocument.Parse(project);
        forwardedProject.Root!.Add(new XElement("ItemGroup",
            new XElement("ProjectReference", new XAttribute("Include", "../Forwarded/Aspen.ForwardedLibrary.csproj"), new XAttribute("Aliases", "Forwarded"), new XAttribute("DtmApiDistribution", "self-authored")),
            new XElement("ProjectReference", new XAttribute("Include", "../Facade/Facade.csproj"), new XAttribute("ReferenceOutputAssembly", "false"), new XAttribute("Private", "false"))));
        forwardedProject.Root.Add(new XElement("Target", new XAttribute("Name", "ForwardImplementation"), new XAttribute("AfterTargets", "Build"),
            new XElement("Copy", new XAttribute("SourceFiles", Path.Combine(facade, "bin/Release/netstandard2.0/Aspen.RestoreLibrary.dll")), new XAttribute("DestinationFiles", "$(TargetDir)Aspen.RestoreLibrary.dll"))));
        forwardedProject.Save(projectPath);
        await ExpectSuccess("restore forwarded runtime graph", "restore", mod);
        var forwardedPack = await ExpectSuccess("actual final type forwarders", "pack", mod, "--compatibility-root", compatibility, "--output", Path.Combine(root, "forwarded.zip"));
        var load = new System.Runtime.Loader.AssemblyLoadContext("forwarded-assets", true);
        string directory = Path.GetDirectoryName(forwardedPack.Values["buildOutputPath"])!;
        load.Resolving += (_, name) => name.Name == "DTMAPI.Abstractions" ? typeof(DTMAPI.Abstractions.DtmMod).Assembly : load.LoadFromStream(new MemoryStream(File.ReadAllBytes(Path.Combine(directory, name.Name + ".dll"))));
        var loaded = load.LoadFromStream(new MemoryStream(File.ReadAllBytes(forwardedPack.Values["buildOutputPath"])));
        Equal("42", loaded.GetType("Aspen.RestoreMod.ModEntry")!.GetProperty("Value")!.GetValue(null)!.ToString()!, "Final facade forwards the actual method invocation");
        load.Unload();
    }
}
