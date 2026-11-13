using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;

namespace DTMAPI.MultiPlatformInstaller;

internal sealed class InstallFile
{
    public string Kind { get; init; } = string.Empty;
    public string SourcePath { get; init; } = string.Empty;
    public string TargetRelativePath { get; init; } = string.Empty;
    public long Length { get; init; }
    public string Sha256 { get; init; } = string.Empty;
    public string FileVersion { get; init; } = string.Empty;
    public string ComponentId { get; init; } = string.Empty;
}

internal sealed class PackageLayout
{
    private static readonly string CurrentRuntimeVersion = typeof(PackageLayout).Assembly
        .GetCustomAttributes<AssemblyMetadataAttribute>().Single(attribute => attribute.Key == "DtmApiRuntimeReleaseVersion").Value!;
    private static readonly string CurrentRuntimeBinaryVersion = typeof(PackageLayout).Assembly
        .GetCustomAttributes<AssemblyMetadataAttribute>().Single(attribute => attribute.Key == "DtmApiRuntimeBinaryFileVersion").Value!;

    public string Root { get; }
    public ReleaseManifestV1 ReleaseManifest { get; }
    public MultiPlatformPackageManifest HostManifest { get; }
    public string ReleaseManifestPath { get; }
    public string HostManifestPath { get; }
    public string BepInExArchivePath { get; }
    public IReadOnlyList<InstallFile> InstallFiles { get; }

    private PackageLayout(
        string root,
        ReleaseManifestV1 releaseManifest,
        MultiPlatformPackageManifest hostManifest,
        string releaseManifestPath,
        string hostManifestPath,
        string bepInExArchivePath,
        IReadOnlyList<InstallFile> installFiles)
    {
        Root = root;
        ReleaseManifest = releaseManifest;
        HostManifest = hostManifest;
        ReleaseManifestPath = releaseManifestPath;
        HostManifestPath = hostManifestPath;
        BepInExArchivePath = bepInExArchivePath;
        InstallFiles = installFiles;
    }

    public static string FindPackageRoot(string? explicitRoot)
    {
        if (!string.IsNullOrWhiteSpace(explicitRoot))
            return Path.GetFullPath(explicitRoot);

        DirectoryInfo? current = new(AppContext.BaseDirectory);
        for (int depth = 0; current is not null && depth < 8; depth++, current = current.Parent)
        {
            if (HasRootShape(current.FullName))
                return Path.GetFullPath(current.FullName);
        }

        // Diagnostic actions remain usable when the package itself is incomplete.
        return Path.GetFullPath(AppContext.BaseDirectory);
    }

    private static bool HasRootShape(string root)
    {
        return File.Exists(Path.Combine(root, RuntimePackageFiles.ReleaseManifest)) ||
               File.Exists(Path.Combine(root, RuntimePackageFiles.MultiPlatformManifest));
    }

    public static PackageLayout Load(string root)
    {
        try
        {
            root = Path.GetFullPath(root);
            string releasePath = FileSystemSafety.CombineUnder(root, RuntimePackageFiles.ReleaseManifest);
            string hostPath = FileSystemSafety.CombineUnder(root, RuntimePackageFiles.MultiPlatformManifest);
            ReleaseManifestV1 release = JsonStore.Read(releasePath, InstallerJsonContext.Default.ReleaseManifestV1)
                ?? throw new InvalidDataException("release-manifest.json is empty.");
            MultiPlatformPackageManifest host = JsonStore.Read(hostPath, InstallerJsonContext.Default.MultiPlatformPackageManifest)
                ?? throw new InvalidDataException("multiplatform-package.json is empty.");

            ValidateManifestIdentity(release, host);
            if (host.SchemaVersion == 2)
                ValidateCandidateSource(releasePath, host);
            List<InstallFile> installFiles = ReadRuntimePayload(root, release, host.RuntimeSource);

            string archiveRelative = FileSystemSafety.NormalizeRelativePath(host.BepInExArchive.RelativePath);
            string archivePath = FileSystemSafety.CombineUnder(root, archiveRelative);

            AddInstalledTools(root, installFiles, host.SchemaVersion == 2, host.RuntimeVersion, host.RuntimeSource);
            FileInfo releaseInfo = new(releasePath);
            installFiles.Add(new InstallFile
            {
                Kind = "release-manifest",
                SourcePath = releasePath,
                TargetRelativePath = "DTMAPI/release-manifest.json",
                Length = releaseInfo.Length,
                Sha256 = FileSystemSafety.Sha256(releasePath)
            });

            if (host.RuntimeSource is { } source)
            {
                foreach (InstallFile file in installFiles)
                {
                    string relative = Path.GetRelativePath(root, file.SourcePath).Replace('\\', '/');
                    PackageFileReceipt receipt = source.SharedFiles.Single(item => item.RelativePath == relative);
                    if (receipt.Length != file.Length || !string.Equals(receipt.Sha256, file.Sha256, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("Installed input disagrees with the imported source declaration: " + relative);
                }
            }
            return new PackageLayout(root, release, host, releasePath, hostPath, archivePath, installFiles);
        }
        catch (InstallerException)
        {
            throw;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or System.Text.Json.JsonException)
        {
            throw new InstallerException(InstallerCodes.PackageInvalid, 3,
                "DTMAPI-多平台包校验失败，安装前未修改游戏文件。",
                "DTMAPI multi-platform package validation failed; no game files were changed.", ex.Message, ex);
        }
    }

    private static void ValidateManifestIdentity(ReleaseManifestV1 release, MultiPlatformPackageManifest host)
    {
        if (release.SchemaVersion != 1 || host.SchemaVersion is not (1 or 2))
            throw new InvalidDataException("Unsupported Runtime/package schema version.");
        ValidateRuntimeVersion(host.SchemaVersion, release.DTMAPIVersion, release.BinaryVersion);
        if (release.PackageKind != "workshop-runtime")
            throw new InvalidDataException("The Runtime version or package identity is not supported by this schema.");
        if (host.SchemaVersion == 1)
        {
            if (host.RuntimeSource is not null || host.SourceWorkshopId != "3743016467" ||
                host.SourceWorkshopManifestId != "918505309011394484" ||
                host.SourcePlayerPayloadTreeSha256 != "b4ec6a441b4930b5174d4caed8799748fb4e4701ac0d8e72fc6bf6bd48eee4aa")
                throw new InvalidDataException("Schema 1 requires the exact observed 0.6.1 publication.");
        }
        else
        {
            CandidateRuntimeSource? source = host.RuntimeSource;
            if (source is null || source.Kind != "Candidate" ||
                !Regex.IsMatch(source.BuildCommit, "^[0-9a-f]{40}$") ||
                !Regex.IsMatch(release.BuildCommit, "^[0-9a-f]{12,40}$") ||
                !source.BuildCommit.StartsWith(release.BuildCommit, StringComparison.Ordinal) ||
                !Regex.IsMatch(source.ReleaseManifestSha256, "^[0-9a-f]{64}$") ||
                host.SourceWorkshopId.Length != 0 || host.SourceWorkshopManifestId.Length != 0 ||
                host.SourcePlayerPayloadTreeSha256.Length != 0 ||
                !Regex.IsMatch(host.InstallerBuildCommit, "^[0-9a-f]{40}$") ||
                host.InstallerVersion != "0.2.0-experimental")
                throw new InvalidDataException("Candidate Runtime/build provenance is invalid or mixes published observations.");
            string? builtVersion = typeof(PackageLayout).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (builtVersion != host.InstallerVersion + "+" + host.InstallerBuildCommit)
                throw new InvalidDataException("Candidate requires the installer built from its declared commit.");
        }
        if (!string.Equals(host.RuntimeVersion, release.DTMAPIVersion, StringComparison.Ordinal) ||
            !string.Equals(host.DistributionId, "dtmapi-multiplatform", StringComparison.Ordinal))
            throw new InvalidDataException("Host and Runtime distribution identity disagree.");
        if (!string.Equals(host.RequiredLinuxLaunchOption, PlatformEnvironment.RequiredLinuxLaunchOption, StringComparison.Ordinal))
            throw new InvalidDataException("The package does not carry the exact required Linux launch option.");
    }

    private static void ValidateRuntimeVersion(int schema, string version, string binaryVersion)
    {
        bool supported = schema == 1
            ? version == "0.6.1" && binaryVersion == "0.6.1.0"
            : schema == 2 && ((version == "0.7.0" && binaryVersion == "0.7.0.0") ||
                              (version == CurrentRuntimeVersion && binaryVersion == CurrentRuntimeBinaryVersion));
        if (!supported)
            throw new InvalidDataException("The Runtime version or binary version is not supported by this schema.");
    }

    private static IEnumerable<string> GetInstalledToolFiles(bool candidate, string runtimeVersion)
    {
        return runtimeVersion == CurrentRuntimeVersion
            ? Array.Empty<string>()
            : RuntimePackageFiles.GetInstalledTools(candidate, false);
    }

    private static void ValidateCandidateSource(string releasePath, MultiPlatformPackageManifest host)
    {
        CandidateRuntimeSource source = host.RuntimeSource!;
        if (FileSystemSafety.Sha256(releasePath) != source.ReleaseManifestSha256)
            throw new InvalidDataException("Candidate release manifest differs from its imported source.");
        IEnumerable<string> expected = RuntimePackageFiles.GetSharedFiles(host.RuntimeVersion == CurrentRuntimeVersion);
        if (!source.SharedFiles.Select(file => file.RelativePath).OrderBy(path => path, StringComparer.Ordinal)
            .SequenceEqual(expected.OrderBy(path => path, StringComparer.Ordinal), StringComparer.Ordinal))
            throw new InvalidDataException("Candidate shared file set is not exact.");
    }

    private static List<InstallFile> ReadRuntimePayload(string root, ReleaseManifestV1 release, CandidateRuntimeSource? sourceReceipt)
    {
        if (release.IncludedAssemblies.Count != RuntimePackageFiles.Assemblies.Length)
            throw new InvalidDataException("The Runtime manifest is missing an assembly.");

        string[] names = release.IncludedAssemblies.Select(entry => entry.FileName).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        string[] expected = RuntimePackageFiles.Assemblies.OrderBy(value => value, StringComparer.Ordinal).ToArray();
        if (!names.SequenceEqual(expected, StringComparer.Ordinal))
            throw new InvalidDataException("The Runtime assembly set does not match the package files.");

        List<InstallFile> result = new();
        foreach (RuntimeAssemblyReceipt entry in release.IncludedAssemblies)
        {
            string source = FileSystemSafety.CombineUnder(root,
                RuntimePackageFiles.PayloadDirectory + RuntimePackageFiles.RuntimeDirectory + entry.FileName);
            if (entry.FileVersion != release.BinaryVersion)
                throw new InvalidDataException("Runtime file version disagrees with the release: " + entry.FileName);
            result.Add(new InstallFile
            {
                Kind = "runtime-assembly",
                SourcePath = source,
                TargetRelativePath = RuntimePackageFiles.RuntimeDirectory + entry.FileName,
                Length = entry.Length,
                Sha256 = entry.Sha256.ToLowerInvariant(),
                FileVersion = entry.FileVersion
            });
        }

        string branding = FileSystemSafety.CombineUnder(root,
            RuntimePackageFiles.PayloadDirectory + RuntimePackageFiles.BrandingFile);
        PackageFileReceipt brandingInfo = ReadSupplementalReceipt(root, branding, sourceReceipt);
        result.Add(new InstallFile
        {
            Kind = "runtime-asset",
            SourcePath = branding,
            TargetRelativePath = RuntimePackageFiles.BrandingFile,
            Length = brandingInfo.Length,
            Sha256 = brandingInfo.Sha256
        });

        bool currentModules = release.DTMAPIVersion == CurrentRuntimeVersion;
        var expectedComponents = RuntimePackageFiles.GetComponents(currentModules)
            .ToDictionary(file => file.Key, file => file.Value, StringComparer.Ordinal);
        if (release.OptionalComponents.Count != expectedComponents.Count)
            throw new InvalidDataException("The Runtime component file set is incomplete.");
        foreach (OptionalComponentReceipt component in release.OptionalComponents)
        {
            string path = component.RelativePath.Replace('\\', '/');
            if (!expectedComponents.TryGetValue(path, out string? id) || component.ComponentId != id)
                throw new InvalidDataException("Unexpected or duplicate Runtime component: " + path);
            expectedComponents.Remove(path);
            bool auxiliary = path.StartsWith("DTMAPI/components/audio/audio-decoder/", StringComparison.Ordinal);
            if (component.FileVersion != (auxiliary ? "" : release.BinaryVersion))
                throw new InvalidDataException("Runtime component file version disagrees with the release: " + path);
            result.Add(new InstallFile
            {
                Kind = "optional-framework-component", ComponentId = component.ComponentId,
                SourcePath = FileSystemSafety.CombineUnder(root, RuntimePackageFiles.PayloadDirectory + path),
                TargetRelativePath = path, Length = component.Length, Sha256 = component.Sha256.ToLowerInvariant(), FileVersion = component.FileVersion
            });
        }

        return result;
    }

    private static void AddInstalledTools(string root, ICollection<InstallFile> result, bool candidate, string runtimeVersion, CandidateRuntimeSource? sourceReceipt)
    {
        foreach (string fileName in GetInstalledToolFiles(candidate, runtimeVersion))
        {
            string source = FileSystemSafety.CombineUnder(root, RuntimePackageFiles.ToolsDirectory + fileName);
            PackageFileReceipt info = ReadSupplementalReceipt(root, source, sourceReceipt);
            result.Add(new InstallFile
            {
                Kind = fileName.EndsWith(".props", StringComparison.OrdinalIgnoreCase) ? "version-authority" :
                    fileName.Contains("collect", StringComparison.OrdinalIgnoreCase) || fileName.Contains("analyze", StringComparison.OrdinalIgnoreCase)
                        ? "diagnostic-tool" : "installer-tool",
                SourcePath = source,
                TargetRelativePath = "DTMAPI/tools/" + fileName,
                Length = info.Length,
                Sha256 = info.Sha256
            });
        }
    }

    private static PackageFileReceipt ReadSupplementalReceipt(string root, string path, CandidateRuntimeSource? source)
    {
        string relative = Path.GetRelativePath(root, path).Replace('\\', '/');
        if (source is not null)
            return source.SharedFiles.Single(item => item.RelativePath == relative);

        // Published schema 1 did not include tool/branding receipts. Read only those inputs.
        FileInfo info = new(path);
        if (!info.Exists || info.Length == 0)
            throw new InvalidDataException("Package input is missing: " + relative);
        return new PackageFileReceipt { RelativePath = relative, Length = info.Length, Sha256 = FileSystemSafety.Sha256(path) };
    }

    public void ValidateInstallPayload()
    {
        try
        {
            foreach (InstallFile file in InstallFiles)
            {
                AssertReceipt(file.SourcePath, file.Length, file.Sha256, file.TargetRelativePath);
                if (file.FileVersion.Length > 0 && FileVersionInfo.GetVersionInfo(file.SourcePath).FileVersion != file.FileVersion)
                    throw new InvalidDataException("Runtime file version disagrees with the release: " + file.TargetRelativePath);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            throw new InstallerException(InstallerCodes.PackageInvalid, 3,
                "待安装文件校验失败，未修改游戏文件。",
                "Install payload validation failed; no game files were changed.", ex.Message, ex);
        }
    }

    private static void AssertReceipt(string path, long length, string sha256, string label)
    {
        if (length <= 0 || string.IsNullOrWhiteSpace(sha256) || !FileSystemSafety.Matches(path, length, sha256))
            throw new InvalidDataException(label + " does not match its declared length/SHA-256: " + path);
    }
}

