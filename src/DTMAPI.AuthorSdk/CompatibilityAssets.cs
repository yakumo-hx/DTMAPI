using DTMAPI.Authoring.Contracts;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json;

namespace DTMAPI.AuthorSdk;

internal sealed class CompatibilityAssets
{
    private CompatibilityAssets(string rootPath, string manifestPath, CompatibilityManifest manifest, string abstractionsPath, IReadOnlyList<string> references)
    {
        RootPath = rootPath;
        ManifestPath = manifestPath;
        Manifest = manifest;
        AbstractionsPath = abstractionsPath;
        ReferencePaths = references;
    }

    public string RootPath { get; }
    public string ManifestPath { get; }
    public CompatibilityManifest Manifest { get; }
    public string AbstractionsPath { get; }
    public IReadOnlyList<string> ReferencePaths { get; }

    public static CompatibilityAssets Resolve(string requestedRoot)
    {
        var candidates = new List<string>();
        if (!string.IsNullOrWhiteSpace(requestedRoot))
            candidates.Add(requestedRoot);
        string environmentRoot = Environment.GetEnvironmentVariable("DTMAPI_AUTHOR_COMPAT_ROOT") ?? string.Empty;
        if (environmentRoot.Length > 0)
            candidates.Add(environmentRoot);
        candidates.Add(Path.Combine(AppContext.BaseDirectory, "compatibility", AuthorSdkContract.TargetRuntimeVersion));

        foreach (string candidate in candidates.Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            foreach (string expanded in Expand(candidate))
            {
                if (File.Exists(Path.Combine(expanded, "compatibility.json")))
                    return Load(expanded);
            }
        }

        throw new InvalidDataException(
            "The fixed Runtime 0.5.5 compatibility payload was not found. " +
            "Use the self-contained SDK distribution or pass --compatibility-root. The SDK never reads DTMAPI.Abstractions.dll from a player game install.");
    }

    private static IEnumerable<string> Expand(string path)
    {
        yield return path;
        yield return Path.Combine(path, AuthorSdkContract.TargetRuntimeVersion);
        yield return Path.Combine(path, "compatibility", AuthorSdkContract.TargetRuntimeVersion);
    }

    private static CompatibilityAssets Load(string root)
    {
        CompatibilityPayloadContract contract = LoadEmbeddedContract();
        string fullRoot = Path.GetFullPath(root);
        string manifestPath = Path.Combine(fullRoot, contract.ReleaseManifestName);
        CompatibilityManifest manifest = JsonSerializer.Deserialize<CompatibilityManifest>(File.ReadAllText(manifestPath, Encoding.UTF8), JsonSupport.Tool)
            ?? throw new InvalidDataException("compatibility.json must contain one object.");
        if (manifest.SchemaVersion != AuthorSdkContract.CompatibilitySchemaVersion)
            throw new InvalidDataException("Unsupported compatibility schemaVersion.");
        if (!manifest.SdkVersion.Equals(AuthorSdkContract.SdkVersion, StringComparison.Ordinal) || !manifest.TargetRuntimeVersion.Equals(AuthorSdkContract.TargetRuntimeVersion, StringComparison.Ordinal))
            throw new InvalidDataException("Compatibility payload version mismatch; expected SDK 0.1.0 and Runtime 0.5.5.");
        if (!manifest.AbstractionsAssemblyVersion.Equals(contract.AbstractionsAssemblyVersion, StringComparison.Ordinal)
            || !manifest.AbstractionsFileVersion.Equals(contract.AbstractionsFileVersion, StringComparison.Ordinal))
            throw new InvalidDataException("Compatibility payload abstraction versions do not match the SDK-embedded contract.");
        if (manifest.Files.Count == 0)
            throw new InvalidDataException("Compatibility payload has no fixed file inventory.");

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var declaredFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { contract.ReleaseManifestName };
        var references = new List<string>();
        var referenceFiles = new List<string>();
        var kindCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        string abstractions = string.Empty;
        string props = string.Empty;
        string license = string.Empty;
        string notice = string.Empty;
        foreach (CompatibilityFile file in manifest.Files.OrderBy(file => file.Path, StringComparer.Ordinal))
        {
            if (!seen.Add(file.Path))
                throw new InvalidDataException("Duplicate compatibility file path: " + file.Path);
            string path = PathSafety.ResolveUnderRoot(fullRoot, file.Path, "compatibility file");
            if (!File.Exists(path))
                throw new InvalidDataException("Compatibility file is missing: " + file.Path);
            if (!PathSafety.Sha256File(path).Equals(file.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Compatibility file hash mismatch: " + file.Path);
            if (!contract.RequiredKinds.Contains(file.Kind, StringComparer.Ordinal))
                throw new InvalidDataException("Compatibility payload contains an unsupported file kind: " + file.Kind);
            kindCounts[file.Kind] = kindCounts.TryGetValue(file.Kind, out int count) ? count + 1 : 1;
            declaredFiles.Add(PathSafety.RelativePath(fullRoot, path));
            if (file.Kind.Equals("abstractions", StringComparison.Ordinal))
            {
                if (abstractions.Length > 0)
                    throw new InvalidDataException("Compatibility payload declares more than one Abstractions file.");
                abstractions = path;
            }
            else if (file.Kind.Equals("reference", StringComparison.Ordinal))
            {
                referenceFiles.Add(path);
                if (path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                    references.Add(path);
            }
            else if (file.Kind.Equals("props", StringComparison.Ordinal))
                props = RequireSingleKindPath(props, path, "props");
            else if (file.Kind.Equals("license", StringComparison.Ordinal))
                license = RequireSingleKindPath(license, path, "license");
            else if (file.Kind.Equals("notice", StringComparison.Ordinal))
                notice = RequireSingleKindPath(notice, path, "notice");
        }

        string[] actualFiles = Directory.EnumerateFiles(fullRoot, "*", SearchOption.AllDirectories)
            .Select(path => PathSafety.RelativePath(fullRoot, path))
            .ToArray();
        string[] extras = actualFiles.Where(path => !declaredFiles.Contains(path)).OrderBy(path => path, StringComparer.Ordinal).ToArray();
        if (extras.Length > 0)
            throw new InvalidDataException("Compatibility payload contains undeclared files: " + string.Join(", ", extras));
        if (abstractions.Length == 0)
            throw new InvalidDataException("Compatibility payload is missing the fixed DTMAPI.Abstractions.dll.");
        if (references.Count == 0)
            throw new InvalidDataException("Compatibility payload has no netstandard2.0 reference assemblies.");

        foreach (string kind in contract.RequiredKinds)
        {
            if (!kindCounts.ContainsKey(kind))
                throw new InvalidDataException("Compatibility payload is missing required kind: " + kind);
        }
        if (kindCounts["abstractions"] != 1 || kindCounts["props"] != 1 || kindCounts["license"] != 1 || kindCounts["notice"] != 1)
            throw new InvalidDataException("Compatibility payload must contain exactly one abstractions/props/license/notice file.");
        if (referenceFiles.Count != contract.NetstandardReferenceFileCount)
            throw new InvalidDataException("Compatibility reference inventory count does not match the SDK-embedded contract.");
        RequireExactRelativePath(fullRoot, abstractions, "DTMAPI.Abstractions.dll");
        RequireExactRelativePath(fullRoot, props, "DTMAPI.Author.props");
        RequireExactRelativePath(fullRoot, license, "NETStandard.Library.LICENSE.TXT");
        RequireExactRelativePath(fullRoot, notice, "NETStandard.Library.THIRD-PARTY-NOTICES.TXT");

        string referenceRoot = Path.Combine(fullRoot, "ref", "netstandard2.0");
        if (referenceFiles.Any(path => !string.Equals(Path.GetDirectoryName(path), referenceRoot, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidDataException("Compatibility reference files must be immediate children of ref/netstandard2.0.");
        if (!AuthorFileTreeDigest.Compute(referenceRoot).Equals(contract.NetstandardReferenceInventorySha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Compatibility reference inventory does not match the SDK-embedded NETStandard.Library 2.0.3 contract.");
        VerifyContractHash(abstractions, contract.AbstractionsSha256, "DTMAPI.Abstractions.dll");
        VerifyContractHash(props, contract.AuthorPropsSha256, "DTMAPI.Author.props");
        VerifyContractHash(license, contract.NetstandardLicenseSha256, "NETStandard.Library license");
        VerifyContractHash(notice, contract.NetstandardNoticeSha256, "NETStandard.Library notice");

        VerifyAbstractionsIdentity(abstractions, manifest);
        PathSafety.RejectReparsePoints(fullRoot, Directory.EnumerateFileSystemEntries(fullRoot, "*", SearchOption.AllDirectories));
        return new CompatibilityAssets(fullRoot, manifestPath, manifest, abstractions, references.OrderBy(path => path, StringComparer.Ordinal).ToArray());
    }

    private static CompatibilityPayloadContract LoadEmbeddedContract()
    {
        using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("DTMAPI.AuthorSdk.compatibility.contract.json")
            ?? throw new InvalidDataException("The SDK-embedded compatibility contract is missing.");
        CompatibilityPayloadContract contract = JsonSerializer.Deserialize<CompatibilityPayloadContract>(stream, JsonSupport.Tool)
            ?? throw new InvalidDataException("The SDK-embedded compatibility contract is invalid.");
        string[] exactKinds = { "abstractions", "license", "notice", "props", "reference" };
        if (contract.SchemaVersion != AuthorSdkContract.CompatibilitySchemaVersion
            || !contract.SdkVersion.Equals(AuthorSdkContract.SdkVersion, StringComparison.Ordinal)
            || !contract.TargetRuntimeVersion.Equals(AuthorSdkContract.TargetRuntimeVersion, StringComparison.Ordinal)
            || !contract.NetstandardReferencePackage.Equals("NETStandard.Library", StringComparison.Ordinal)
            || !contract.NetstandardReferencePackageVersion.Equals("2.0.3", StringComparison.Ordinal)
            || !contract.NetstandardReferenceInventoryAlgorithm.Equals(AuthorFileTreeDigest.AlgorithmId, StringComparison.Ordinal)
            || !contract.ReleaseManifestName.Equals("compatibility.json", StringComparison.Ordinal)
            || !contract.RequiredKinds.OrderBy(value => value, StringComparer.Ordinal).SequenceEqual(exactKinds, StringComparer.Ordinal))
            throw new InvalidDataException("The SDK-embedded compatibility contract is inconsistent with SDK 0.1.0 / Runtime 0.5.5.");
        return contract;
    }

    private static string RequireSingleKindPath(string current, string path, string kind)
    {
        if (current.Length > 0)
            throw new InvalidDataException("Compatibility payload declares more than one " + kind + " file.");
        return path;
    }

    private static void VerifyContractHash(string path, string expected, string label)
    {
        if (path.Length == 0 || !PathSafety.Sha256File(path).Equals(expected, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException(label + " does not match the SDK-embedded fixed hash.");
    }

    private static void RequireExactRelativePath(string root, string path, string expected)
    {
        if (!PathSafety.RelativePath(root, path).Equals(expected, StringComparison.Ordinal))
            throw new InvalidDataException("Compatibility payload expected exact path " + expected + ".");
    }

    private static void VerifyAbstractionsIdentity(string path, CompatibilityManifest manifest)
    {
        using FileStream stream = File.OpenRead(path);
        using var pe = new PEReader(stream, PEStreamOptions.LeaveOpen);
        if (!pe.HasMetadata)
            throw new InvalidDataException("DTMAPI.Abstractions.dll has no managed metadata.");
        MetadataReader reader = pe.GetMetadataReader();
        AssemblyDefinition definition = reader.GetAssemblyDefinition();
        string name = reader.GetString(definition.Name);
        if (!name.Equals("DTMAPI.Abstractions", StringComparison.Ordinal))
            throw new InvalidDataException("Compatibility abstraction identity mismatch: " + name);
        if (!definition.Version.ToString().Equals(manifest.AbstractionsAssemblyVersion, StringComparison.Ordinal))
            throw new InvalidDataException("Compatibility abstraction AssemblyVersion mismatch.");
        string fileVersion = FileVersionInfo.GetVersionInfo(path).FileVersion ?? string.Empty;
        if (!NormalizeFourPartVersion(fileVersion).Equals(NormalizeFourPartVersion(manifest.AbstractionsFileVersion), StringComparison.Ordinal))
            throw new InvalidDataException("Compatibility abstraction FileVersion mismatch.");
    }

    private static string NormalizeFourPartVersion(string value)
    {
        string numeric = new string(value.TakeWhile(character => char.IsDigit(character) || character == '.').ToArray()).TrimEnd('.');
        return Version.TryParse(numeric, out Version? version) ? version.ToString(4) : value;
    }
}
