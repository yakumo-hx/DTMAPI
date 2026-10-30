using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using DTMAPI.Tooling.Metadata;
using DTMAPI.Internal.Authoring;

namespace DTMAPI.InstallDoctor;

public sealed class DoctorEngine
{
    private readonly PeMetadataInspector metadata = new();
    public DoctorReport Inspect(string rootPath, DoctorOptions? options = null)
    {
        options ??= new DoctorOptions();
        string root = Path.GetFullPath(rootPath);
        if (!Directory.Exists(root)) throw new DirectoryNotFoundException(root);
        var findings = new List<DoctorFinding>(options.ContextFindings);
        var artifacts = new List<DoctorArtifact>();
        var before = options.VerifyReadOnlyTreeHash ? new TreeHasher().Capture(root) : null;
        string? installed = options.InstalledDtmApiVersion;
        if (options.ScanContext == DoctorScanContext.InstalledGame && installed == null)
        { var probe = InstalledRuntimeVersionProbe.Inspect(root); installed = probe.InstalledDtmApiVersion; findings.AddRange(probe.Findings); }
        var packageRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (options.ScanContext == DoctorScanContext.PackageArtifact) packageRoots.Add(root);
        else
        {
            foreach (string parent in new[] { OfficialModsRoot, Path.Combine(root, "Mods"), WorkshopRoot(root) }.Where(Directory.Exists))
                foreach (string directory in Directory.EnumerateDirectories(parent).Where(p => !Path.GetFileName(p).StartsWith('.')))
                    packageRoots.Add(directory);
        }
        foreach (string package in packageRoots.OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
        {
            string? manifest = ManifestPath(package);
            if (manifest != null) artifacts.Add(InspectPackage(package, manifest, root, installed, options));
        }
        if (options.ScanContext == DoctorScanContext.InstalledGame)
        {
            string plugins = Path.Combine(root, "BepInEx", "plugins");
            foreach (string file in Files(plugins).Where(p => p.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)))
            {
                var pe = metadata.Inspect(file, computeHash: false);
                bool runtime = pe.AssemblyName is "DTMAPI.Core" or "DTMAPI.Abstractions" or "DTMAPI.BepInExBootstrap" or "DTMAPI.GameBridge.DolocTown" or "DTMAPI.ModConfigMenu";
                artifacts.Add(new DoctorArtifact { Path = file, AssemblyName = pe.AssemblyName, AssemblyVersion = pe.AssemblyVersion?.ToString() ?? "",
                    Kind = runtime ? DoctorArtifactKind.DtmApiRuntime : pe.IsManaged ? DoctorArtifactKind.ExternalBepInExPlugin : DoctorArtifactKind.DamagedAssembly,
                    ManagedIdentity = runtime ? DoctorManagedIdentity.DtmApiRuntime : DoctorManagedIdentity.ExternalBepInExPlugin,
                    Placement = DoctorPlacement.Expected, Findings = pe.Error.Length == 0 ? Array.Empty<DoctorFinding>() : new[] { Finding("plugin-unreadable", file, pe.Error, DoctorSeverity.Error) } });
            }
        }
        if (artifacts.Count == 0) findings.Add(Finding("no-artifacts", root, "No runtime or package entry was found.", DoctorSeverity.Info));
        findings.Add(Finding("inspection-coverage", root,
            "Checked manifests, entry metadata and installed runtime version. Actual reference binding, unused DLL candidates, optional modules, dynamic loads and game execution are not checked. SDK proofs are not admission evidence.", DoctorSeverity.Info));
        var after = options.VerifyReadOnlyTreeHash ? new TreeHasher().Capture(root) : null;
        return new DoctorReport { RootPath = root, ScanContext = options.ScanContext, InstalledDtmApiVersion = installed ?? "", RuntimeVersionCheckRequested = installed != null,
            Artifacts = artifacts, Findings = findings, ReadOnlyVerificationRequested = options.VerifyReadOnlyTreeHash,
            InitialTree = before, FinalTree = after, TreeUnchanged = before == null || after == null ? null : before.Errors.Count == 0 && after.Errors.Count == 0 && before.Sha256 == after.Sha256 };
    }

    private DoctorArtifact InspectPackage(string package, string manifest, string game, string? installed, DoctorOptions options)
    {
        var findings = new List<DoctorFinding>();
        string id = "", version = "", minimum = "", owner = "", type = "", name = "", assemblyVersion = "";
        int selected = 0;
        var minimumStatus = DoctorMinimumVersionStatus.NotChecked;
        try
        {
            var data = JsonNode.Parse(File.ReadAllText(manifest))!.AsObject();
            string Text(string key) => data[key]?.GetValue<string>() ?? "";
            id = PackageDocumentFacts.Id(Text("UniqueID")); type = Text("Type"); if (type.Length == 0) type = "CodeMod";
            version = Text("Version"); minimum = Text("MinimumDTMApiVersion"); if (minimum.Length == 0) minimum = Text("MinimumApiVersion");
            owner = Text("HarmonyOwner");
            if (data["ManifestVersion"]?.GetValue<int>() == 2)
            {
                string info = Path.Combine(package, "info.json"); version = "";
                if (File.Exists(info))
                {
                    var display = PackageDocumentFacts.ReadInfo(File.ReadAllBytes(info), out string warning);
                    if (display.TryGetValue("version", out string? displayVersion)) version = displayVersion;
                    if (warning.Length > 0) findings.Add(Finding("display-metadata-unreadable", info, warning, DoctorSeverity.Warning));
                }
            }
            if (id.Length == 0) throw new InvalidDataException("Manifest UniqueID is missing.");
            minimumStatus = CheckMinimum(minimum, installed);
            if (minimumStatus is DoctorMinimumVersionStatus.RequiresNewerRuntime or DoctorMinimumVersionStatus.RuntimeUnavailable or DoctorMinimumVersionStatus.MinimumVersionInvalid)
                findings.Add(Finding("runtime-minimum", manifest, "Required runtime=" + minimum + "; installed=" + (installed ?? "not checked") + "; " + minimumStatus, DoctorSeverity.Error));
            else if (minimumStatus == DoctorMinimumVersionStatus.RuntimeVersionInvalid)
                findings.Add(Finding("runtime-version-unknown", manifest, "Installed runtime version is not comparable: " + installed + "; minimum compatibility is unknown.", DoctorSeverity.Warning));
            if (type == "CodeMod")
            {
                string entry = Resolve(package, Text("EntryDll"));
                var pe = metadata.Inspect(entry, computeHash: false); selected = 1;
                name = pe.AssemblyName; assemblyVersion = pe.AssemblyVersion?.ToString() ?? "";
                if (!pe.IsManaged) findings.Add(Finding("selected-code-unreadable", entry, pe.Error.Length > 0 ? pe.Error : "Entry is not managed code.", DoctorSeverity.Error));
                if (pe.TargetFramework.Length > 0)
                    findings.Add(Finding("selected-code-framework", entry, "Declared TFM=" + pe.TargetFramework + "; execution compatibility is not established by this label.", DoctorSeverity.Info));
                findings.Add(Finding("reference-not-checked", entry, pe.AssemblyReferences.Count + " static references observed; actual host/provider binding and optional use require runtime evidence.", DoctorSeverity.Info));
                findings.Add(Finding("selected-code-coverage", manifest, "Entry metadata inspected; dependency candidates were not opened or hashed.", DoctorSeverity.Info));
            }
        }
        catch (Exception ex) when (ex is System.Text.Json.JsonException or IOException or InvalidDataException or InvalidOperationException or ArgumentException)
        { findings.Add(Finding("package-invalid", manifest, ex.Message, DoctorSeverity.Error)); }
        return new DoctorArtifact { Path = package, ManifestPath = manifest, UniqueId = id, Version = version, MinimumDtmApiVersion = minimum, MinimumVersionStatus = minimumStatus,
            Kind = type == "ContentPack" ? DoctorArtifactKind.DtmApiContentPack : DoctorArtifactKind.DtmApiCodeMod,
            ManagedIdentity = type == "ContentPack" ? DoctorManagedIdentity.ContentPack : DoctorManagedIdentity.CodeMod,
            EffectiveCodeModKind = type, DeclarationStatus = DoctorDeclarationStatus.Explicit, ProvenanceStatus = DoctorProvenanceStatus.DeclaredOnly,
            NativeRisk = type == "ContentPack" ? DoctorNativeRisk.NoCode : DoctorNativeRisk.ThirdPartyAuthorManaged,
            ReferenceCompatibility = DoctorCompatibilityStatus.NotChecked,
            GameCompatibility = DoctorCompatibilityStatus.NotChecked, ExpectedHarmonyOwner = owner, AssemblyName = name, AssemblyVersion = assemblyVersion, ReferenceCount = selected,
            Placement = DoctorPlacement.Expected, RestartPolicy = type == "ContentPack" ? DoctorRestartPolicy.NotApplicable : DoctorRestartPolicy.RestartRequiredAfterLoad, Findings = findings };
    }
    private static DoctorMinimumVersionStatus CheckMinimum(string minimum, string? installed)
    {
        if (minimum.Length == 0) return DoctorMinimumVersionStatus.NotDeclared;
        PackageSemanticVersion required, actual;
        try { required = PackageSemanticVersion.Parse(minimum); } catch (InvalidDataException) { return DoctorMinimumVersionStatus.MinimumVersionInvalid; }
        if (installed == null) return DoctorMinimumVersionStatus.NotChecked;
        if (installed.Length == 0) return DoctorMinimumVersionStatus.RuntimeUnavailable;
        try { actual = PackageSemanticVersion.Parse(installed); } catch (InvalidDataException) { return DoctorMinimumVersionStatus.RuntimeVersionInvalid; }
        return actual.CompareTo(required) >= 0 ? DoctorMinimumVersionStatus.Compatible : DoctorMinimumVersionStatus.RequiresNewerRuntime;
    }
    private static bool IsPlatform(string name) => name is "netstandard" or "mscorlib" or "System" || name.StartsWith("System.", StringComparison.Ordinal) || name.StartsWith("Microsoft.CSharp", StringComparison.Ordinal) || name == "DTMAPI.Abstractions";
    private static string Resolve(string root, string relative)
    {
        if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative)) throw new InvalidDataException("EntryDll must be relative to the package.");
        string path = Path.GetFullPath(Path.Combine(root, relative));
        if (!path.StartsWith(Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("EntryDll escapes the package.");
        return path;
    }
    private static string? ManifestPath(string root) => PackageDocumentFacts.FindManifest(root);
    private static IEnumerable<string> Files(string root)
    {
        if (!Directory.Exists(root) || (File.GetAttributes(root) & FileAttributes.ReparsePoint) != 0) yield break;
        foreach (string entry in Directory.EnumerateFileSystemEntries(root))
        {
            var attributes = File.GetAttributes(entry); if ((attributes & FileAttributes.ReparsePoint) != 0) continue;
            if ((attributes & FileAttributes.Directory) != 0) { foreach (string file in Files(entry)) yield return file; }
            else yield return entry;
        }
    }
    internal static string OfficialModsRoot => Path.Combine(Environment.GetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT") ??
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData/LocalLow/RedSawGames/DolocTown"), "MODS");
    private static string WorkshopRoot(string game) => Path.GetFullPath(Path.Combine(game, "../../workshop/content/2285550"));
    private static DoctorFinding Finding(string code, string path, string message, DoctorSeverity severity) => new() { Code = code, Path = path, Message = message, Severity = severity };
}
