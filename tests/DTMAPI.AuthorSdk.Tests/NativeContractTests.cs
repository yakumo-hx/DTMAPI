using System.IO.Compression;
using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using DTMAPI.Tooling.Metadata;
using DTMAPI.InstallDoctor;
using CoreManagedModClassifier = DTMAPI.Core.Manifesting.ManagedModClassifier;

namespace DTMAPI.AuthorSdk.Tests;

internal static partial class Program
{
    private static async Task TestNativeContract(string temporary, string compatibility, string repository)
    {
        string root = Path.Combine(temporary, "native-contract"), game = Path.Combine(root, "game");
        string managed = Path.Combine(game, "DolocTown_Data", "Managed"); Directory.CreateDirectory(managed);
        string hostPath = Path.Combine(managed, "Meadow.NativeFixture.dll");
        var references = Directory.EnumerateFiles(Path.Combine(compatibility, "ref"), "*.dll", SearchOption.AllDirectories).Select(path => MetadataReference.CreateFromFile(path)).ToArray();
        const string source = "[assembly:System.Runtime.Versioning.TargetFramework(\".NETStandard,Version=v2.0\")][assembly:System.Reflection.AssemblyVersion(\"1.0.0.0\")] namespace Meadow { public class NativeFixture { static NativeFixture(){System.IO.File.WriteAllText(System.Environment.GetEnvironmentVariable(\"DTMAPI_NATIVE_TEST_TRACE\"),\"EXECUTED\");} public static int Value; public static int Echo(int x)=>x; public int Echo(string x)=>x.Length; public static void Ref(ref int x){} public static int[] Matrix(int[,] x)=>null; public static T Generic<T>(T x)=>x; } }";
        byte[] CompileHost(string code, string assemblyName = "Meadow.NativeFixture")
        {
            var compilation = CSharpCompilation.Create(assemblyName, new[] { CSharpSyntaxTree.ParseText(code) }, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, deterministic: true));
            using var output = new MemoryStream(); var result = compilation.Emit(output);
            True(result.Success, "Native test host compiles: " + string.Join(";", result.Diagnostics)); return output.ToArray();
        }
        byte[] host = CompileHost(source); File.WriteAllBytes(hostPath, host);
        byte[] surface = NativeReferenceSurface.Create(host, new[] { managed });
        True(surface.SequenceEqual(NativeReferenceSurface.Create(host, new[] { managed })), "Native metadata surface is deterministic.");
        NativeReferenceSurface.Verify(surface);
        True(!surface.SequenceEqual(host), "Native compiler surface excludes original executable code.");

        string project = Path.Combine(root, "author");
        await ExpectSuccess("new arbitrary native author", "new", "codemod", project, "--id", "Meadow.IndependentNative", "--name", "Native", "--author", "Meadow", "--api-target", "0.6.4", "--code-mod-kind", "Advanced", "--game-root", game, "--native-references", "DolocTown_Data/Managed/Meadow.NativeFixture.dll");
        string authorPath = Path.Combine(project, "dtmapi.author.json"), manifestPath = Path.Combine(project, "manifest.json");
        File.WriteAllText(Path.Combine(project, "src", "ModEntry.cs"), "using DTMAPI.Abstractions; namespace Meadow.IndependentNative; public sealed class ModEntry:DtmMod { public override void Entry(IDtmHelper helper){int x=NativeFixture.Echo(1);NativeFixture.Ref(ref x);int[] a=NativeFixture.Matrix(new int[1,1]);helper.Monitor.Log(NativeFixture.Value.ToString());} }");
        string current = Path.Combine(repository, ".tools", "author-sdk-compatibility", "0.6.4");
        await ExpectSuccess("restore standard native author", "restore", project, "--offline", "true");
        var packed = await ExpectSuccess("native provenance pack", "pack", project, "--compatibility-root", current);
        var again = await ExpectSuccess("native provenance deterministic pack", "pack", project, "--compatibility-root", current);
        Equal(packed.Sha256, again.Sha256, "Native provenance and bundle bytes are deterministic.");
        string package = Path.Combine(root, "package"); ZipFile.ExtractToDirectory(packed.OutputPath, package);
        True(!Directory.EnumerateFiles(package, "*", SearchOption.AllDirectories).Any(path => Path.GetFileName(path) == "Meadow.NativeFixture.dll"), "Host and local reference surface are excluded from the package.");
        var provenance = JsonNode.Parse(File.ReadAllText(Path.Combine(package, "dtmapi-native-build.json")))!.AsObject();
        True(!provenance.ToJsonString().Contains(game), "Native provenance contains no absolute game path.");
        var members = provenance["requiredMembers"]!.AsArray();
        var echo = members.OfType<JsonObject>().Single(member => (string?)member["name"] == "Echo");
        Equal("[bcl]System.Int32", (string)echo["parameterTypes"]![0]!, "Actual int overload recorded.");
        True((bool)echo["isStatic"]!, "Staticness is recorded.");

        string trace = Path.Combine(root, "host-static-executed.txt");
        Environment.SetEnvironmentVariable("DTMAPI_NATIVE_TEST_TRACE", trace);
        try
        {
            string boundManifest = Path.Combine(package, "Content", "DTMAPI", "manifest.json");
            var reader = new DTMAPI.Core.Manifesting.ManifestReader();
            var parsed = reader.Read(boundManifest);
            var classified = new DTMAPI.Core.Manifesting.ManagedModClassifier(game).Classify(parsed, package, boundManifest, "Local", true, null);
            Equal("sdk-native-contract-v1-verified", classified.DeclarationProvenance, "New Advanced branch is independent of old receipts/catalog IDs.");
            True(!File.Exists(trace), "Classifying native host signatures executes no host static initializer or author Entry.");
            var doctor = new DoctorEngine().Inspect(package, new DoctorOptions { ScanContext = DoctorScanContext.PackageArtifact });
            True(doctor.ErrorCount == 0, "Doctor accepts the same complete Native V1 artifact: " + string.Join(";", doctor.Findings.Select(finding => finding.Message)));

            string nativePath = Path.Combine(package, "dtmapi-native-build.json"), markerPath = Path.Combine(package, "Content", "DTMAPI", "dtmapi-package.json");
            byte[] originalNative = File.ReadAllBytes(nativePath), originalMarker = File.ReadAllBytes(markerPath);
            foreach (string fault in new[] { "unknown-version", "missing-generation-inputs", "generation-digest", "required-member", "entry-binding", "unknown-field", "mis-cased-field" })
            {
                var changed = JsonNode.Parse(originalNative)!.AsObject();
                switch (fault)
                {
                    case "unknown-version": changed["schemaVersion"] = 2; break;
                    case "missing-generation-inputs": changed["referenceGeneration"]!.AsObject().Remove("metadataDependencies"); break;
                    case "generation-digest": changed["referenceGeneration"]!["inputSha256"] = new string('0', 64); break;
                    case "required-member": changed["requiredMembers"]!.AsArray().OfType<JsonObject>().First(member => (string?)member["kind"] == "method")["name"] = "MissingRequiredMethod"; break;
                    case "entry-binding": changed["entrySha256"] = new string('0', 64); break;
                    case "unknown-field": changed["legacyFallback"] = true; break;
                    case "mis-cased-field": changed["OwnerId"] = changed["ownerId"]!.DeepClone(); changed.Remove("ownerId"); break;
                }
                File.WriteAllText(nativePath, changed.ToJsonString());
                string digest = PathSafety.Sha256File(nativePath);
                var marker = JsonNode.Parse(originalMarker)!.AsObject();
                marker["nativeBuildSha256"] = digest;
                var row = marker["files"]!.AsArray().OfType<JsonObject>().Single(row => (string?)row["path"] == "dtmapi-native-build.json");
                row["sha256"] = digest; row["length"] = new FileInfo(nativePath).Length;
                File.WriteAllText(markerPath, marker.ToJsonString());
                bool rejected = false;
                try { new DTMAPI.Core.Manifesting.ManagedModClassifier(game).Classify(reader.Read(boundManifest), package, boundManifest, "Local", true, null); }
                catch (IOException) { rejected = true; }
                True(rejected, "Core rejects rebound native contract fault: " + fault);
                // A standalone package cannot prove native signature presence. Other contract faults are fully shared.
                if (fault != "required-member") True(new DoctorEngine().Inspect(package, new DoctorOptions { ScanContext = DoctorScanContext.PackageArtifact }).ErrorCount > 0, "Doctor rejects rebound contract fault: " + fault);
                File.WriteAllBytes(nativePath, originalNative); File.WriteAllBytes(markerPath, originalMarker);
            }

            string originalManifest = File.ReadAllText(manifestPath);
            foreach (string changed in new[] { originalManifest.Replace("\"NativeContractVersion\": 1", "\"NativeContractVersion\": 2"), originalManifest.Replace("NativeContractVersion", "nativeContractVersion"), originalManifest.Replace("\"Advanced\"", "\"Strict\"") })
            {
                File.WriteAllText(manifestPath, changed);
                var invalid = await Run("validate", project);
                True(!invalid.Report.Success, "SDK rejects native selector/kind fault without fallback.");
                bool rejected = false;
                try { reader.Read(manifestPath); } catch (InvalidDataException) { rejected = true; }
                True(rejected, "Core rejects the same native selector/kind fault without fallback.");
                True(new DoctorEngine().Inspect(project, new DoctorOptions { ScanContext = DoctorScanContext.PackageArtifact }).ErrorCount > 0, "Doctor rejects the same native selector/kind fault.");
            }
            File.WriteAllText(manifestPath, originalManifest);
            var metadataMember = new NativeMemberDescription { AssemblyIdentity = (string)echo["assemblyIdentity"]!, DeclaringType = (string)echo["declaringType"]!, Kind = "method", Name = "Echo", IsStatic = true, ReturnType = "[bcl]System.Int32", ParameterTypes = new[] { "[bcl]System.Int32" } };
            True(NativeMemberMetadata.Contains(host, metadataMember), "Original native exact signature accepted.");
            True(!NativeMemberMetadata.Contains(CompileHost(source.Replace("static int Echo(int x)=>x", "static int Echo(long x)=>(int)x")), metadataMember), "Native overload/parameter drift rejected.");
            metadataMember.IsStatic = false;
            True(!NativeMemberMetadata.Contains(host, metadataMember), "Static/instance mismatch rejected.");
            metadataMember.IsStatic = true; metadataMember.ReturnType = "[bcl]System.String";
            True(!NativeMemberMetadata.Contains(host, metadataMember), "Return type mismatch rejected.");
            metadataMember.ReturnType = "[bcl]System.Int32";
            True(NativeMemberMetadata.Contains(CompileHost(source.Replace("Echo(int x)=>x;", "Echo(int x)=>x+1;")), metadataMember), "Body-only host drift preserves the required signature; byte drift remains a warning gate.");
            string genericSource = "public class UsesGeneric { public int Run()=>Meadow.NativeFixture.Generic<int>(1); }";
            var compiler = CSharpCompilation.Create("GenericConsumer", new[] { CSharpSyntaxTree.ParseText(genericSource) }, references.Append(MetadataReference.CreateFromImage(surface)), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            using var genericOutput = new MemoryStream(); True(compiler.Emit(genericOutput).Success, "Generic use corpus compiles.");
            bool unsupported = false;
            try { NativeMemberMetadata.Extract(genericOutput.ToArray(), new[] { hostPath }, new[] { managed }); }
            catch (InvalidDataException ex) { unsupported = ex.Message.StartsWith("native-signature-unsupported:", StringComparison.Ordinal); }
            True(unsupported, "Native V1 rejects unsupported generic signatures explicitly.");

            // Exercise the complete classifier -> source identity -> owner supervision -> loader -> Entry path.
            // The earlier metadata-only assertion cannot detect a mismatched before-load fingerprint algorithm.
            string runtimePackage = Path.Combine(game, "Mods", "NativeAuthor");
            ZipFile.ExtractToDirectory(packed.OutputPath, runtimePackage);
            string? previousPersistent = Environment.GetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT");
            try
            {
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", Path.Combine(root, "runtime-persistent"));
                var runtimeHost = new DependencyHost(game);
                var runtime = new DTMAPI.Core.Runtime.DtmApiRuntime(runtimeHost);
                runtime.ConfigureAdvancedHarmonyInspectorForTests(new EmptyNativeHarmonyInspector());
                runtime.Start();
                True(runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == "Meadow.IndependentNative"), "Actual Native V1 Entry is reached: " + string.Join(";", runtimeHost.Messages.Where(message => message.Contains("Failed") || message.Contains("native-") || message.Contains("fingerprint"))));
                True(File.Exists(trace), "Only actual Entry executes the host static initializer.");
                runtime.DeactivateOwner("Meadow.IndependentNative", DTMAPI.Core.Runtime.ModOwnerCleanupReason.Unload, false, "native test complete");
                True(!runtime.HasOwnerInstance("Meadow.IndependentNative"), "Actual Native V1 owner closes.");
            }
            finally { Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", previousPersistent); }

            string memoryPath = Path.Combine(managed, "Meadow.MemoryHost.dll");
            byte[] memoryBytes = CompileHost(source, "Meadow.MemoryHost");
            File.WriteAllBytes(memoryPath, memoryBytes);
            string memoryProject = Path.Combine(root, "memory-author");
            await ExpectSuccess("new memory-host author", "new", "codemod", memoryProject, "--id", "Meadow.MemoryAuthor", "--name", "Memory", "--author", "Meadow", "--api-target", "0.6.4", "--code-mod-kind", "Advanced", "--game-root", game, "--native-references", "DolocTown_Data/Managed/Meadow.MemoryHost.dll");
            File.WriteAllText(Path.Combine(memoryProject, "src", "ModEntry.cs"), "using DTMAPI.Abstractions; namespace Meadow.MemoryAuthor; public sealed class ModEntry:DtmMod { public override void Entry(IDtmHelper h){NativeFixture.Echo(1);} }");
            await ExpectSuccess("restore standard memory author", "restore", memoryProject, "--offline", "true");
            var memoryPack = (await Run("pack", memoryProject, "--compatibility-root", current)).Report;
            True(memoryPack.Success, "Memory origin corpus publicly packs: " + string.Join(";", memoryPack.Diagnostics.Select(item => item.Message)));
            string memoryPackage = Path.Combine(root, "memory-package");
            ZipFile.ExtractToDirectory(memoryPack.OutputPath, memoryPackage);
            string memoryManifestPath = Path.Combine(memoryPackage, "Content", "DTMAPI", "manifest.json");
            var memoryManifest = reader.Read(memoryManifestPath);
            var residentMemory = System.Reflection.Assembly.Load(memoryBytes);
            True(string.IsNullOrEmpty(residentMemory.Location), "Memory host fixture has no CLR file location.");
            void ExpectMemoryReject(string expected)
            {
                bool rejected = false;
                try { new CoreManagedModClassifier(game).Classify(memoryManifest, memoryPackage, memoryManifestPath, "Local", true, null); }
                catch (IOException ex) { rejected = ex.Message.Contains(expected, StringComparison.Ordinal); }
                True(rejected, "Memory origin rejection: " + expected);
            }
            ExpectMemoryReject("native-host-origin-unavailable");
            bool wrongOrigin = false;
            try { DTMAPI.Core.Manifesting.NativePackageVerifier.ObservePreloaderOrigin(residentMemory, hostPath); }
            catch (InvalidDataException) { wrongOrigin = true; }
            True(wrongOrigin, "Preloader map cannot bind a different identity/MVID.");
            DTMAPI.Core.Manifesting.NativePackageVerifier.ObservePreloaderOrigin(residentMemory, memoryPath);
            var boundMemory = new CoreManagedModClassifier(game).Classify(memoryManifest, memoryPackage, memoryManifestPath, "Local", true, null);
            True(boundMemory.IsAdvanced, "Pre-observed original file admits memory host required signatures.");
            True(boundMemory.GameCompatibility.Contains("native-host-preloader-patched", StringComparison.Ordinal), "Patched original-file provenance is not reported as exact resident bytes.");
            File.WriteAllBytes(memoryPath, CompileHost(source.Replace("Echo(int x)=>x;", "Echo(int x)=>x+1;"), "Meadow.MemoryHost"));
            ExpectMemoryReject("native-host-origin-unavailable");
            bool replacedOrigin = false;
            try { DTMAPI.Core.Manifesting.NativePackageVerifier.ObservePreloaderOrigin(residentMemory, memoryPath); }
            catch (InvalidDataException) { replacedOrigin = true; }
            True(replacedOrigin, "Re-observation cannot bless replaced original bytes.");
            File.WriteAllBytes(memoryPath, memoryBytes);
        }
        finally { Environment.SetEnvironmentVariable("DTMAPI_NATIVE_TEST_TRACE", null); }
    }

    private sealed class EmptyNativeHarmonyInspector : DTMAPI.Core.Runtime.IAdvancedHarmonyInspector
    {
        public DTMAPI.Core.Runtime.AdvancedHarmonySnapshot Capture() => DTMAPI.Core.Runtime.AdvancedHarmonySnapshot.Captured();
        public DTMAPI.Core.Runtime.AdvancedHarmonyUnpatchResult UnpatchOwner(string owner) => new(true, 0, 0, "No actual Harmony patches in the metadata host fixture.");
    }
}
