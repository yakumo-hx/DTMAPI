using System.IO.Compression;
using System.Xml.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace DTMAPI.AuthorSdk.Tests;

internal static partial class Program
{
    private static async Task TestFinalFieldAssets(string temporary)
    {
        string root = Path.Combine(temporary, "final field assets"), library = Path.Combine(root, "Library"), mod = Path.Combine(root, "Mod");
        string compatibility = Path.Combine(FindRepository(), ".tools/author-sdk-compatibility/0.7.0");
        Directory.CreateDirectory(library);
        File.WriteAllText(Path.Combine(library, "Birch.FieldApi.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>netstandard2.0</TargetFramework><DebugType>portable</DebugType></PropertyGroup></Project>");
        File.WriteAllText(Path.Combine(library, "Value.cs"), """
            namespace Birch.FieldApi {
              public class Base<T> { public T Generic; public static T GenericStatic; }
              public class Value : Base<int> {
                public static int SRead, SWrite, SAddress, TokenOnly;
                public int IRead, IWrite, IAddress;
                public static void TokenProbe() { }
              }
            }
            """);
        await ExpectSuccess("create field consumer", "new", "codemod", mod, "--id", "Birch.FieldConsumer", "--name", "Fields", "--author", "Birch", "--api-target", "0.7.0");
        string projectPath = Path.Combine(mod, "Birch.FieldConsumer.csproj");
        string project = File.ReadAllText(projectPath).Replace("</Project>", "<ItemGroup><ProjectReference Include=\"../Library/Birch.FieldApi.csproj\" DtmApiDistribution=\"self-authored\" /></ItemGroup></Project>");
        File.WriteAllText(projectPath, project);
        File.WriteAllText(Path.Combine(mod, "src/ModEntry.cs"), """
            using DTMAPI.Abstractions;
            using Birch.FieldApi;
            namespace Birch.FieldConsumer;
            public sealed class ModEntry : DtmMod {
              public override void Entry(IDtmHelper helper) { helper.Monitor.Log(Result().ToString()); }
              public static int Result() { var value = new Value(); value.Generic = 40; Value.GenericStatic = 2; return value.Generic + Value.GenericStatic; }
              public static class Nested {
                public static int ReadStatic() => Value.SRead;
                public static void WriteStatic(int n) { Value.SWrite = n; }
                public static ref int AddressStatic() => ref Value.SAddress;
                public static int ReadInstance(Value value) => value.IRead;
                public static void WriteInstance(Value value, int n) { value.IWrite = n; }
                public static ref int AddressInstance(Value value) => ref value.IAddress;
              }
            }
            """);
        await ExpectSuccess("restore field graph", "restore", mod);
        var valid = await ExpectSuccess("all legal field uses and inherited generic fields", "pack", mod, "--compatibility-root", compatibility, "--symbols", "true");
        Equal("0.7.0", valid.TargetRuntimeVersion, "field positive target");
        string original = Path.Combine(library, "bin/Release/netstandard2.0/Birch.FieldApi.dll");
        string extract = Path.Combine(root, "accepted-artifact");
        ZipFile.ExtractToDirectory(valid.OutputPath, extract);
        var load = new System.Runtime.Loader.AssemblyLoadContext("accepted-fields", true);
        load.Resolving += (_, name) => name.Name == "DTMAPI.Abstractions" ? typeof(DTMAPI.Abstractions.DtmMod).Assembly
            : load.LoadFromStream(new MemoryStream(File.ReadAllBytes(Path.Combine(extract, "lib/private", name.Name + ".dll"))));
        var loaded = load.LoadFromStream(new MemoryStream(File.ReadAllBytes(Path.Combine(extract, "Content/DTMAPI/Birch.FieldConsumer.dll"))));
        Equal("42", loaded.GetType("Birch.FieldConsumer.ModEntry")!.GetMethod("Result")!.Invoke(null, null)!.ToString()!, "actual accepted generic field invocation");
        load.Unload();
        foreach (var (fieldName, opcode, methodName) in new[] {
            ("SRead", "ldsfld", "ReadStatic"), ("SWrite", "stsfld", "WriteStatic"), ("SAddress", "ldsflda", "AddressStatic"),
            ("IRead", "ldfld", "ReadInstance"), ("IWrite", "stfld", "WriteInstance"), ("IAddress", "ldflda", "AddressInstance") })
        {
            string changedRoot = Path.Combine(root, fieldName); Directory.CreateDirectory(changedRoot);
            string changed = Path.Combine(changedRoot, "Birch.FieldApi.dll");
            using (var assembly = AssemblyDefinition.ReadAssembly(original, new ReaderParameters { ReadSymbols = true }))
            {
                var field = assembly.MainModule.Types.Single(t => t.Name == "Value").Fields.Single(f => f.Name == fieldName);
                field.IsStatic = !field.IsStatic;
                assembly.Write(changed, new WriterParameters { WriteSymbols = true });
            }
            var document = XDocument.Parse(project);
            document.Root!.Add(new XElement("Target", new XAttribute("Name", "SelectFinalRuntime"), new XAttribute("AfterTargets", "Build"),
                new XElement("Copy", new XAttribute("SourceFiles", changed + ";" + Path.ChangeExtension(changed, ".pdb")), new XAttribute("DestinationFolder", "$(TargetDir)"))));
            document.Save(projectPath);
            var failed = await ExpectFailure("final field instruction " + opcode, "pack", mod, "--compatibility-root", compatibility, "--symbols", "true");
            True(failed.Diagnostics.Any(d => d.Code != "SDK999" && d.Message.Contains("runtime-field-static-mismatch") && d.Message.Contains(opcode)
                && d.Message.Contains(fieldName) && d.Message.Contains(methodName) && d.Message.Contains("Birch.FieldConsumer")),
                "actual field rejection after paired DLL/PDB: " + string.Join(";", failed.Diagnostics.Select(d => d.Message)));
            Equal("0.7.0", failed.TargetRuntimeVersion, "failed field report retains actual target");
            Equal(valid.Sha256, Sha256(valid.OutputPath), "field rejection preserves original ZIP");
        }
        // ldtoken carries identity, not a required static/instance storage kind.
        string tokenRoot = Path.Combine(root, "token"); Directory.CreateDirectory(tokenRoot);
        string tokenDll = Path.Combine(tokenRoot, "Birch.FieldApi.dll");
        using (var assembly = AssemblyDefinition.ReadAssembly(original, new ReaderParameters { ReadSymbols = true }))
        {
            var type = assembly.MainModule.Types.Single(t => t.Name == "Value");
            var tokenField = type.Fields.Single(f => f.Name == "TokenOnly"); tokenField.IsStatic = false;
            var method = type.Methods.Single(m => m.Name == "TokenProbe");
            method.DebugInformation.SequencePoints.Clear(); method.DebugInformation.Scope = null;
            method.Body.Instructions.Clear();
            var il = method.Body.GetILProcessor(); il.Emit(OpCodes.Ldtoken, tokenField); il.Emit(OpCodes.Pop); il.Emit(OpCodes.Ret);
            assembly.Write(tokenDll, new WriterParameters { WriteSymbols = true });
        }
        var tokenProject = XDocument.Parse(project);
        tokenProject.Root!.Add(new XElement("Target", new XAttribute("Name", "SelectFinalRuntime"), new XAttribute("AfterTargets", "Build"),
            new XElement("Copy", new XAttribute("SourceFiles", tokenDll + ";" + Path.ChangeExtension(tokenDll, ".pdb")), new XAttribute("DestinationFolder", "$(TargetDir)"))));
        tokenProject.Save(projectPath);
        var tokenPack = await ExpectSuccess("ldtoken allows instance field after ref static field", "pack", mod, "--compatibility-root", compatibility, "--output", Path.Combine(root, "token.zip"));
        True((await Run("doctor", tokenPack.OutputPath, "--json")).ExitCode == 0, "field package read-only Doctor exit0");
    }
}
