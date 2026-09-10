using System.IO.Compression;
using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using DTMAPI.InstallDoctor;

namespace DTMAPI.AuthorSdk.Tests;

internal static partial class Program
{
    private static async Task TestPackageDependencies(string temporary, string compatibility, string repository)
    {
        string root = Path.Combine(temporary, "dependency-contract"); Directory.CreateDirectory(root);
        string library = Path.Combine(root, "Independent.Contract.dll");
        var references = Directory.EnumerateFiles(Path.Combine(compatibility, "ref"), "*.dll", SearchOption.AllDirectories).Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("Independent.Contract", new[] { CSharpSyntaxTree.ParseText("[assembly:System.Runtime.Versioning.TargetFramework(\".NETStandard,Version=v2.0\")][assembly:System.Reflection.AssemblyVersion(\"1.0.0.0\")] namespace Independent { public interface IValue { int Read(); } }") }, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, deterministic: true));
        var emitted = compilation.Emit(library);
        True(emitted.Success, "Independent pure contract compiles: " + string.Join(";", emitted.Diagnostics));
        string project = Path.Combine(root, "consumer");
        await ExpectSuccess("new dependency consumer", "new", "codemod", project, "--id", "Independent.Consumer", "--name", "Consumer", "--author", "Independent", "--api-target", "0.6.3");
        JsonObject author = JsonNode.Parse(File.ReadAllText(Path.Combine(project, "dtmapi.author.json")))!.AsObject();
        author["schemaVersion"] = 3;
        author["managedReferences"] = new JsonArray(new JsonObject { ["path"] = library, ["role"] = "shared-contract", ["distribution"] = "self-authored", ["licenseFiles"] = new JsonArray() });
        File.WriteAllText(Path.Combine(project, "dtmapi.author.json"), author.ToJsonString());
        JsonObject manifest = JsonNode.Parse(File.ReadAllText(Path.Combine(project, "manifest.json")))!.AsObject();
        manifest["DependencyContractVersion"] = 1;
        manifest["Version"] = "1.0.0+build";
        manifest["Dependencies"] = new JsonArray(new JsonObject
        {
            ["UniqueID"] = "Independent.Provider", ["Required"] = true,
            ["VersionRange"] = new JsonObject { ["minimumInclusive"] = "1.0.0", ["maximumExclusive"] = "2.0.0", ["includePrerelease"] = false }
        });
        File.WriteAllText(Path.Combine(project, "manifest.json"), manifest.ToJsonString());
        File.WriteAllText(Path.Combine(project, "src", "ModEntry.cs"), "using System; using DTMAPI.Abstractions; namespace Independent.Consumer; public sealed class ModEntry:DtmMod, IDisposable { IDtmHelper h=null!; bool retried; public override void Entry(IDtmHelper helper) { h=helper; IValue value=helper.ModRegistry.GetApi<IValue>(\"Independent.Provider\") ?? throw new Exception(\"provider missing\"); helper.Monitor.Log(\"CONSUMER PASS value=\"+value.Read()+\" exact=\"+ReferenceEquals(typeof(IValue),value.GetType().GetInterfaces()[0])); } public void Dispose(){if(!retried){retried=true;throw new Exception(\"expected consumer cleanup retry\");}h.Monitor.Log(\"CONSUMER CLOSED\");} }");
        string current = Path.Combine(repository, ".tools", "author-sdk-compatibility", "0.6.3");
        var pack = await ExpectSuccess("dependency pack", "pack", project, "--compatibility-root", current);
        var repeat = await ExpectSuccess("dependency pack deterministic", "pack", project, "--compatibility-root", current);
        Equal(pack.Sha256, repeat.Sha256, "Schema 3 deterministic package bytes.");
        string package = Path.Combine(root, "package"); ZipFile.ExtractToDirectory(pack.OutputPath, package);
        void Classify(string directory)
        {
            string path = Path.Combine(directory, "Content", "DTMAPI", "manifest.json");
            var parsed = new DTMAPI.Core.Manifesting.ManifestReader().Read(path);
            new DTMAPI.Core.Manifesting.ManagedModClassifier(directory).Classify(parsed, directory, path, "Local", true, null);
        }
        Classify(package);
        var doctor = new DoctorEngine().Inspect(package, new DoctorOptions { ScanContext = DoctorScanContext.PackageArtifact });
        True(doctor.ErrorCount == 0, "Doctor accepts the same complete dependency package: " + string.Join(";", doctor.Findings.Select(finding => finding.Message)));
        File.WriteAllText(Path.Combine(package, ".dtmapi-author-receipt.json"), "{\"deploymentMetadata\":true}");
        Classify(package);
        True(new DoctorEngine().Inspect(package, new DoctorOptions { ScanContext = DoctorScanContext.PackageArtifact }).ErrorCount == 0,
            "Deployment-owned receipt is outside the immutable package inventory.");
        File.Copy(library, Path.Combine(package, ".dtmapi-author-receipt.json"), true);
        bool receiptExecutableRejected = false;
        try { Classify(package); } catch (DTMAPI.Core.Manifesting.ManagedModClassificationException) { receiptExecutableRejected = true; }
        True(receiptExecutableRejected, "Deployment receipt name cannot conceal an executable.");
        string game = Path.Combine(root, "game"); Directory.CreateDirectory(Path.Combine(game, "Mods"));
        ZipFile.ExtractToDirectory(pack.OutputPath, Path.Combine(game, "Mods", "Consumer"));
        foreach (string name in new[] { "Provider", "Control" })
        {
            string peer = Path.Combine(root, name);
            await ExpectSuccess("new " + name, "new", "codemod", peer, "--id", "Independent." + name, "--name", name, "--author", "Independent", "--api-target", "0.6.3");
            var peerAuthor = JsonNode.Parse(File.ReadAllText(Path.Combine(peer, "dtmapi.author.json")))!.AsObject();
            peerAuthor["schemaVersion"] = 3;
            peerAuthor["managedReferences"] = name == "Provider" ? author["managedReferences"]!.DeepClone() : new JsonArray();
            File.WriteAllText(Path.Combine(peer, "dtmapi.author.json"), peerAuthor.ToJsonString());
            var peerManifest = JsonNode.Parse(File.ReadAllText(Path.Combine(peer, "manifest.json")))!.AsObject();
            peerManifest["DependencyContractVersion"] = 1; peerManifest["Version"] = "1.0.0";
            File.WriteAllText(Path.Combine(peer, "manifest.json"), peerManifest.ToJsonString());
            string source = name == "Provider"
                ? "using System; using DTMAPI.Abstractions; namespace Independent.Provider; public sealed class ModEntry:DtmMod,IDisposable { IDtmHelper h=null!; public override void Entry(IDtmHelper helper){h=helper;helper.ModRegistry.RegisterApi<IValue>(new Value());helper.Monitor.Log(\"PROVIDER ENTRY\");} public void Dispose(){h.Monitor.Log(\"PROVIDER CLOSED\");} private sealed class Value:IValue { public int Read()=>62; } }"
                : "using DTMAPI.Abstractions; namespace Independent.Control; public sealed class ModEntry:DtmMod { public override void Entry(IDtmHelper helper){helper.Monitor.Log(\"CONTROL ENTRY\");} }";
            File.WriteAllText(Path.Combine(peer, "src", "ModEntry.cs"), source);
            var peerPack = await ExpectSuccess("pack " + name, "pack", peer, "--compatibility-root", current);
            ZipFile.ExtractToDirectory(peerPack.OutputPath, Path.Combine(game, "Mods", name));
        }
        string? previousPersistent = Environment.GetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT");
        try
        {
            Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", Path.Combine(root, "persistent"));
            var host = new DependencyHost(game);
            var runtime = new DTMAPI.Core.Runtime.DtmApiRuntime(host);
            runtime.Start();
            True(host.Messages.Any(message => message.Contains("CONSUMER PASS value=62 exact=True")), "Actual runtime shared CLR Type and provider result: " + string.Join("\n", host.Messages.Where(message => message.Contains("Dependency") || message.Contains("Failed") || message.Contains("Manifest discovery"))));
            string pending = runtime.DeactivateOwner("Independent.Provider", DTMAPI.Core.Runtime.ModOwnerCleanupReason.Unload, false, "");
            True(pending.Contains("requiredDependentCleanupPending") && runtime.HasOwnerInstance("Independent.Provider"), "Failed consumer cleanup retains provider for the existing retry path.");
            runtime.DeactivateOwner("Independent.Provider", DTMAPI.Core.Runtime.ModOwnerCleanupReason.Unload, false, "");
            int consumerClosed = host.Messages.FindIndex(message => message.Contains("CONSUMER CLOSED"));
            int providerClosed = host.Messages.FindIndex(message => message.Contains("PROVIDER CLOSED"));
            True(consumerClosed >= 0 && providerClosed > consumerClosed, "Required consumer closes before provider resources.");
            True(runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == "Independent.Control"), "Unrelated Control remains active.");
            // Replace the selected packages on disk while their old CLR images remain resident.
            // Moving the previous directory also models a receipt-bound package swap on Windows.
            string updatedLibraryRoot = Path.Combine(root, "updated-library"); Directory.CreateDirectory(updatedLibraryRoot);
            string updatedLibrary = Path.Combine(updatedLibraryRoot, "Independent.Contract.dll");
            True(compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText("[assembly:System.Reflection.AssemblyMetadata(\"updated\",\"true\")]")).Emit(updatedLibrary).Success,
                "Compile same identity with different bytes for a disk update.");
            foreach (string name in new[] { "Consumer", "Provider" })
            {
                string ownerProject = name == "Consumer" ? project : Path.Combine(root, name);
                var updateAuthor = JsonNode.Parse(File.ReadAllText(Path.Combine(ownerProject, "dtmapi.author.json")))!.AsObject();
                updateAuthor["managedReferences"]![0]!["path"] = updatedLibrary;
                File.WriteAllText(Path.Combine(ownerProject, "dtmapi.author.json"), updateAuthor.ToJsonString());
                var updateManifest = JsonNode.Parse(File.ReadAllText(Path.Combine(ownerProject, "manifest.json")))!.AsObject();
                updateManifest["Version"] = "1.0.1";
                File.WriteAllText(Path.Combine(ownerProject, "manifest.json"), updateManifest.ToJsonString());
                var replacement = await ExpectSuccess("pack disk update " + name, "pack", ownerProject, "--compatibility-root", current);
                string installed = Path.Combine(game, "Mods", name);
                Directory.Move(installed, Path.Combine(root, "previous-" + name));
                ZipFile.ExtractToDirectory(replacement.OutputPath, installed);
            }
            int beforeRefresh = host.Messages.Count;
            runtime.NotifyWorkshopModListChanged();
            True(host.Messages.Skip(beforeRefresh).Any(message => message.Contains("resident-conflict/restart-required") && message.Contains("Independent.Contract")),
                "Actual resident evidence survives a disk swap and blocks changed bytes before binding.");
            True(runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == "Independent.Control"), "Control survives resident conflict.");
            runtime.NotifyRuntimeShutdown("dependency test finished");
        }
        finally { Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", previousPersistent); }
        foreach (string fault in new[] { "missing", "tampered", "extra", "projection" })
        {
            string bad = Path.Combine(root, fault); ZipFile.ExtractToDirectory(pack.OutputPath, bad);
            string dll = Path.Combine(bad, "lib", "shared", "Independent.Contract.dll");
            if (fault == "missing") File.Delete(dll);
            if (fault == "tampered") File.AppendAllText(dll, "changed");
            if (fault == "extra") File.Copy(library, Path.Combine(bad, "unlisted.data"));
            if (fault == "projection") File.AppendAllText(Path.Combine(bad, "dtmapi-dependencies.json"), " ");
            bool rejected = false;
            try { Classify(bad); } catch (Exception ex) when (ex is IOException || ex is DTMAPI.Core.Manifesting.ManagedModClassificationException) { rejected = true; }
            True(rejected, "Core rejects " + fault + " before assembly execution.");
            True(new DoctorEngine().Inspect(bad, new DoctorOptions { ScanContext = DoctorScanContext.PackageArtifact }).ErrorCount > 0, "Doctor rejects " + fault + " with the same inventory contract.");
        }
        author["managedReferences"]![0]!["distribution"] = "licensed-third-party";
        File.WriteAllText(Path.Combine(project, "dtmapi.author.json"), author.ToJsonString());
        await ExpectFailure("missing license material", "validate", project);
        author["managedReferences"]![0]!["distribution"] = "self-authored";
        string validAuthor = author.ToJsonString();
        File.WriteAllText(Path.Combine(project, "dtmapi.author.json"), validAuthor.Replace("\"role\":", "\"Role\":\"private-managed\",\"role\":"));
        await ExpectFailure("duplicate nested reference field", "validate", project);
        File.WriteAllText(Path.Combine(project, "dtmapi.author.json"), validAuthor.Replace("\"role\":", "\"Role\":"));
        await ExpectFailure("mis-cased nested reference field", "validate", project);
        File.WriteAllText(Path.Combine(project, "dtmapi.author.json"), validAuthor);
        string CompileLibrary(string name, string source, params string[] additionalReferences)
        {
            string path = Path.Combine(root, name + ".dll");
            var result = CSharpCompilation.Create(name,
                new[] { CSharpSyntaxTree.ParseText("[assembly:System.Runtime.Versioning.TargetFramework(\".NETStandard,Version=v2.0\")]" + source) },
                references.Concat(additionalReferences.Select(file => MetadataReference.CreateFromFile(file))),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, deterministic: true)).Emit(path);
            True(result.Success, "Compile metadata fault library " + name + ": " + string.Join(";", result.Diagnostics));
            return path;
        }
        string leaf = CompileLibrary("Independent.Leaf", "namespace Leaf {public class Value {}}");
        string transitive = CompileLibrary("Independent.Transitive", "public class Transitive {public Leaf.Value Value=null!;}", leaf);
        string native = CompileLibrary("UnityEngine.CoreModule", "namespace UnityEngine {public class FixtureNative {}}");
        string indirectNative = CompileLibrary("Independent.IndirectNative", "public class IndirectNative {public UnityEngine.FixtureNative Value=null!;}", native);
        string disguised = Path.Combine(root, "Disguised.dll"); File.Copy(native, disguised);
        foreach (var fault in new[] { ("missing transitive library", transitive), ("Strict indirect native reference", indirectNative), ("renamed host assembly", disguised) })
        {
            author["managedReferences"]![0]!["path"] = fault.Item2;
            File.WriteAllText(Path.Combine(project, "dtmapi.author.json"), author.ToJsonString());
            await ExpectFailure(fault.Item1, "validate", project);
        }
        File.WriteAllText(Path.Combine(project, "dtmapi.author.json"), validAuthor);
    }

    private sealed class DependencyHost(string path) : DTMAPI.Core.Runtime.IRuntimeHost, DTMAPI.Core.Runtime.ILegacyDevelopmentModSourceTestHost
    {
        public string GamePath => path;
        public string PluginPath => Path.Combine(path, "BepInEx", "plugins");
        public string HostName => "DependencyTest";
        public bool IncludeLegacyDevelopmentModSourceForTests => true;
        public List<string> Messages { get; } = new();
        public void Log(string message) => Messages.Add(message);
        public void LogWarning(string message) => Messages.Add(message);
        public void LogError(string message, Exception? exception = null) => Messages.Add(message + exception);
    }
}
