using System.IO.Compression;
using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using DTMAPI.Tooling.Metadata;
using DTMAPI.InstallDoctor;

namespace DTMAPI.AuthorSdk.Tests;

internal static partial class Program
{
    private static async Task TestNativeGenericContract(string temporary, string compatibility, string repository)
    {
        string root = Path.Combine(temporary, "native-v2"), game = Path.Combine(root, "game"), project = Path.Combine(root, "author");
        string managed = Path.Combine(game, "DolocTown_Data", "Managed"); Directory.CreateDirectory(managed);
        string hostPath = Path.Combine(managed, "Birch.GenericHost.dll");
        var references = Directory.EnumerateFiles(Path.Combine(compatibility, "ref"), "*.dll", SearchOption.AllDirectories).Select(p => MetadataReference.CreateFromFile(p)).ToArray();
        const string hostSource = """
            [assembly:System.Runtime.Versioning.TargetFramework(".NETStandard,Version=v2.0")]
            [assembly:System.Reflection.AssemblyVersion("1.0.0.0")]
            namespace Birch {
              public class Component { public int Value = 42; }
              public static class Native {
                static Native(){System.IO.File.WriteAllText(System.Environment.GetEnvironmentVariable("DTMAPI_GENERIC_TRACE"),"host executed");}
                public static T Get<T>() where T:Component,new() => new T();
                public static Component Get(System.Type type) => (Component)System.Activator.CreateInstance(type);
                public static T[] Echo<T>(ref T value, out int count) { count=1; return new[]{value}; }
                public static int Variable(int value,__arglist)=>value;
              }
              public class Box<T> where T:Component { public T Value; public T Read()=>Value; public U Combine<U>(T item,U other)=>other; public class Nested<U> { public System.Collections.Generic.List<T[]> Mix(U input) => null; } }
            }
            """;
        byte[] Compile(string source)
        {
            using var output = new MemoryStream();
            var result = CSharpCompilation.Create("Birch.GenericHost", new[] { CSharpSyntaxTree.ParseText(source) }, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, deterministic: true)).Emit(output);
            True(result.Success, "V2 host fixture compile: " + string.Join(";", result.Diagnostics)); return output.ToArray();
        }
        byte[] host = Compile(hostSource); File.WriteAllBytes(hostPath, host);
        await ExpectSuccess("current V2 author", "new", "codemod", project, "--id", "Birch.GenericAuthor", "--name", "Generic", "--author", "Birch", "--code-mod-kind", "Advanced", "--game-root", game, "--native-references", "DolocTown_Data/Managed/Birch.GenericHost.dll");
        True(JsonNode.Parse(File.ReadAllText(Path.Combine(project, "manifest.json")))!["NativeContractVersion"]!.GetValue<int>() == 2, "Current Advanced selects V2");
        string sourcePath = Path.Combine(project, "src", "ModEntry.cs");
        File.WriteAllText(sourcePath, """
            using DTMAPI.Abstractions;
            using Birch;
            namespace Birch.GenericAuthor;
            public sealed class Custom:Component { static Custom(){System.IO.File.WriteAllText(System.Environment.GetEnvironmentVariable("DTMAPI_GENERIC_AUTHOR_TRACE"),"author executed");} }
            public sealed class ModEntry:DtmMod {
              public static int Run(){ var x=Native.Get<Custom>(); var y=Native.Get(typeof(Custom)); int count; var array=Native.Echo(ref x,out count); var box=new Box<Custom>{Value=x}; var nested=new Box<Custom>.Nested<int>(); nested.Mix(1); return box.Combine(box.Read(),array[0].Value)+count+y.Value; }
              public static T Open<T>(ref T value) where T:Component,new(){ int count; Native.Echo<T>(ref value,out count); return Native.Get<T>(); }
              public override void Entry(IDtmHelper h){h.Monitor.Log("generic="+Run());}
            }
            """);
        string current = Path.Combine(repository, ".tools", "author-sdk-compatibility", "0.7.0");
        var build = await ExpectSuccess("V2 standard build", "build", project, "--compatibility-root", current);
        var packed = await ExpectSuccess("V2 public pack", "pack", project, "--compatibility-root", current);
        var repeated = await ExpectSuccess("V2 deterministic pack", "pack", project, "--compatibility-root", current);
        Equal(packed.Sha256, repeated.Sha256, "V2 deterministic package bytes");
        string validSource = File.ReadAllText(sourcePath);
        File.WriteAllText(sourcePath, validSource.Replace("public static int Run(){", "public static int Run(){ Native.Variable(1,__arglist(2));"));
        await ExpectSuccess("standard Csc accepts vararg syntax", "build", project, "--compatibility-root", current);
        var unsupported = await ExpectFailure("vararg refused at package contract boundary", "pack", project, "--compatibility-root", current);
        True(unsupported.Diagnostics.Any(d => d.Message.Contains("native-signature-unsupported") && d.Message.Contains("Run") && d.Message.Contains("ModEntry.cs:")), "Unsupported syntax has actual PDB member/source context: " + string.Join(";", unsupported.Diagnostics.Select(d => d.Message)));
        Equal(packed.Sha256, PathSafety.Sha256File(packed.OutputPath), "Unsupported native syntax preserves prior package");
        File.WriteAllText(sourcePath, validSource);
        string package = Path.Combine(root, "package"); ZipFile.ExtractToDirectory(packed.OutputPath, package);
        string nativePath = Path.Combine(package, "dtmapi-native-build.json"), manifestPath = Path.Combine(package, "Content/DTMAPI/manifest.json"), markerPath = Path.Combine(package, "Content/DTMAPI/dtmapi-package.json");
        byte[] nativeBytes = File.ReadAllBytes(nativePath), markerBytes = File.ReadAllBytes(markerPath);
        var native = JsonNode.Parse(nativeBytes)!.AsObject();
        var members = native["requiredMembers"]!.AsArray().OfType<JsonObject>().ToArray();
        True(members.Any(m => m["methodArguments"]!.AsArray().Any(a => (string?)a?["name"] == "Birch.GenericAuthor.Custom")), "MethodSpec retains the author's own type argument");
        True(members.Any(m => m["definition"]!["declaringType"]!["name"]!.GetValue<string>().Contains("Nested`1")), "Nested generic declaring type is recorded");
        string hostTrace = Path.Combine(root, "host-executed"), authorTrace = Path.Combine(root, "author-executed");
        Environment.SetEnvironmentVariable("DTMAPI_GENERIC_TRACE", hostTrace); Environment.SetEnvironmentVariable("DTMAPI_GENERIC_AUTHOR_TRACE", authorTrace);
        try
        {
            var reader = new DTMAPI.Core.Manifesting.ManifestReader();
            var classifier = new DTMAPI.Core.Manifesting.ManagedModClassifier(game);
            var classification = classifier.Classify(reader.Read(manifestPath), package, manifestPath, "Local", true, null);
            Equal("sdk-native-contract-v2-verified", classification.DeclarationProvenance, "Runtime V2 definition/constraints match disk metadata");
            True(!File.Exists(hostTrace) && !File.Exists(authorTrace) && !AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "Birch.GenericAuthor"), "Preflight loads no author assembly or initializer");
            True(new DoctorEngine().Inspect(package, new DoctorOptions { ScanContext = DoctorScanContext.PackageArtifact }).ErrorCount == 0, "Doctor reads V2 package");
            foreach (var member in members)
            {
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(member.ToJsonString());
                True(NativeGenericMetadata.Contains(host, bytes), "Disk V2 matches every extracted use");
            }
            byte[] drift = Compile(hostSource.Replace("where T:Component,new() => new T()", "where T:Component => default(T)"));
            var get = members.First(m => (string?)m["definition"]!["name"] == "Get" && m["definition"]!["methodParameters"]!.AsArray().Count == 1);
            True(!NativeGenericMetadata.Contains(drift, System.Text.Encoding.UTF8.GetBytes(get.ToJsonString())), "Constraint drift rejected by disk metadata");
            foreach (string fault in new[] { "marker-version", "arity", "constraint", "removed-member", "argument-reference", "argument-type", "parameter-position", "unknown-node", "unknown-field", "duplicate-field" })
            {
                var changed = JsonNode.Parse(nativeBytes)!.AsObject(); var marker = JsonNode.Parse(markerBytes)!.AsObject();
                var changedMembers = changed["requiredMembers"]!.AsArray();
                var target = changedMembers.OfType<JsonObject>().First(m => (string?)m["definition"]!["name"] == "Get" && m["methodArguments"]!.AsArray().Count == 1 && (string?)m["methodArguments"]![0]!["kind"] == "named");
                if (fault == "marker-version") marker["nativeContractVersion"] = 1;
                if (fault == "arity") target["methodArguments"]!.AsArray().Add(target["methodArguments"]![0]!.DeepClone());
                if (fault == "constraint") target["definition"]!["methodParameters"]![0]!["flags"] = 0;
                if (fault == "removed-member") target["definition"]!["name"] = "Gone";
                if (fault == "argument-reference") target["methodArguments"]![0]!["assemblyIdentity"] = "Missing.Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";
                if (fault == "argument-type") target["methodArguments"]![0]!["name"] = "Birch.GenericAuthor.Missing";
                if (fault == "parameter-position") target["methodArguments"]![0] = new JsonObject { ["kind"] = "methodParameter", ["position"] = 4096 };
                if (fault == "unknown-node") target["methodArguments"]![0]!["kind"] = "functionPointer";
                if (fault == "unknown-field") target["fallback"] = true;
                string changedText = changed.ToJsonString();
                if (fault == "duplicate-field") changedText = changedText.Replace("\"contextTypeArity\":", "\"contextTypeArity\":0,\"contextTypeArity\":");
                File.WriteAllText(nativePath, changedText);
                marker["nativeBuildSha256"] = PathSafety.Sha256File(nativePath);
                var inventory = marker["files"]!.AsArray().OfType<JsonObject>().Single(r => (string?)r["path"] == "dtmapi-native-build.json");
                inventory["sha256"] = PathSafety.Sha256File(nativePath); inventory["length"] = new FileInfo(nativePath).Length;
                File.WriteAllText(markerPath, marker.ToJsonString());
                bool rejected = false;
                try { classifier.Classify(reader.Read(manifestPath), package, manifestPath, "Local", true, null); }
                catch (IOException ex) { rejected = ex.Message.Contains("native") || ex.Message.Contains("dependency"); }
                True(rejected, "Runtime rejects V2 before Entry: " + fault);
                True(!File.Exists(hostTrace) && !File.Exists(authorTrace), "No initializer for rejected " + fault);
                if (fault is not ("constraint" or "removed-member"))
                    True(new DoctorEngine().Inspect(package, new DoctorOptions { ScanContext = DoctorScanContext.PackageArtifact }).ErrorCount > 0, "Doctor rejects structured fault " + fault);
                File.WriteAllBytes(nativePath, nativeBytes); File.WriteAllBytes(markerPath, markerBytes);
            }
            // Classification alone misses the supervision branch between preflight and Entry.
            string runtimePackage = Path.Combine(game, "Mods", "GenericAuthor");
            ZipFile.ExtractToDirectory(packed.OutputPath, runtimePackage);
            string? previousPersistent = Environment.GetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT");
            try
            {
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", Path.Combine(root, "runtime-persistent"));
                var runtimeHost = new DependencyHost(game);
                var runtime = new DTMAPI.Core.Runtime.DtmApiRuntime(runtimeHost);
                runtime.ConfigureAdvancedHarmonyInspectorForTests(new EmptyNativeHarmonyInspector());
                runtime.Start();
                True(runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == "Birch.GenericAuthor"), "Actual V2 Entry passes owner supervision: " + string.Join(";", runtimeHost.Messages.Where(m => m.Contains("Failed") || m.Contains("native-") || m.Contains("harmony"))));
                True(File.Exists(hostTrace) && File.Exists(authorTrace) && runtimeHost.Messages.Any(m => m.Contains("generic=85")), "Actual Entry executes generic host and author constructors/results");
                runtime.DeactivateOwner("Birch.GenericAuthor", DTMAPI.Core.Runtime.ModOwnerCleanupReason.Unload, false, "generic test complete");
                True(!runtime.HasOwnerInstance("Birch.GenericAuthor"), "Actual V2 owner closes");
            }
            finally { Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", previousPersistent); }
        }
        finally { Environment.SetEnvironmentVariable("DTMAPI_GENERIC_TRACE", null); Environment.SetEnvironmentVariable("DTMAPI_GENERIC_AUTHOR_TRACE", null); }
    }
}
