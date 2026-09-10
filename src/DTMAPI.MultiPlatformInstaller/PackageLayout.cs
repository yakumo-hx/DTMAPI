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
    private static readonly string[] ExpectedRuntimeFiles =
    {
        "DTMAPI.Abstractions.dll",
        "DTMAPI.BepInExBootstrap.dll",
        "DTMAPI.Core.dll",
        "DTMAPI.GameBridge.DolocTown.dll",
        "DTMAPI.ModConfigMenu.dll"
    };

    private static readonly string[] InstalledToolFiles =
    {
        "common.ps1",
        "release-common.ps1",
        "uninstall-dtmapi.ps1",
        "check-dtmapi-status.ps1",
        "collect-logs.ps1",
        "analyze-startup-evidence.ps1",
        "dtmapi-runtime-version.props"
    };

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
            return ValidateRootShape(explicitRoot);

        DirectoryInfo? current = new(AppContext.BaseDirectory);
        for (int depth = 0; current is not null && depth < 8; depth++, current = current.Parent)
        {
            if (HasRootShape(current.FullName))
                return Path.GetFullPath(current.FullName);
        }

        throw new InstallerException(InstallerCodes.PackageInvalid, 3,
            "找不到完整的 DTMAPI-多平台 Workshop 包。请不要单独移动安装器文件。",
            "The complete DTMAPI multi-platform Workshop package could not be found. Do not move the host tool by itself.",
            AppContext.BaseDirectory);
    }

    private static string ValidateRootShape(string path)
    {
        string root = Path.GetFullPath(path);
        if (!HasRootShape(root))
            throw new InstallerException(InstallerCodes.PackageInvalid, 3,
                "指定目录不是完整的 DTMAPI-多平台包。",
                "The selected directory is not a complete DTMAPI multi-platform package.", root);
        return root;
    }

    private static bool HasRootShape(string root)
    {
        return File.Exists(Path.Combine(root, "Content", "DTMAPI", "release-manifest.json")) &&
               File.Exists(Path.Combine(root, "Content", "DTMAPIInstaller", "multiplatform-package.json"));
    }

    public static PackageLayout LoadAndValidate(string root)
    {
        try
        {
            root = Path.GetFullPath(root);
            string releasePath = FileSystemSafety.CombineUnder(root, "Content/DTMAPI/release-manifest.json");
            string hostPath = FileSystemSafety.CombineUnder(root, "Content/DTMAPIInstaller/multiplatform-package.json");
            ReleaseManifestV1 release = JsonStore.Read(releasePath, InstallerJsonContext.Default.ReleaseManifestV1)
                ?? throw new InvalidDataException("release-manifest.json is empty.");
            MultiPlatformPackageManifest host = JsonStore.Read(hostPath, InstallerJsonContext.Default.MultiPlatformPackageManifest)
                ?? throw new InvalidDataException("multiplatform-package.json is empty.");

            ValidateManifestIdentity(release, host);
            if (host.SchemaVersion == 2)
                ValidateCandidateSource(root, releasePath, host);
            List<InstallFile> installFiles = ValidateRuntimePayload(root, release);
            ValidateHostArtifacts(root, host);

            string archiveRelative = FileSystemSafety.NormalizeRelativePath(host.BepInExArchive.RelativePath);
            string archivePath = FileSystemSafety.CombineUnder(root, archiveRelative);

            AddInstalledTools(root, installFiles, host.SchemaVersion == 2);
            FileInfo releaseInfo = new(releasePath);
            installFiles.Add(new InstallFile
            {
                Kind = "release-manifest",
                SourcePath = releasePath,
                TargetRelativePath = "DTMAPI/release-manifest.json",
                Length = releaseInfo.Length,
                Sha256 = FileSystemSafety.Sha256(releasePath)
            });

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
        string version = host.SchemaVersion == 1 ? "0.6.1" : "0.7.0";
        if (release.DTMAPIVersion != version || release.BinaryVersion != version + ".0" ||
            release.PackageKind != "workshop-runtime" || host.WorkshopId != "3792681186")
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

    private static void ValidateCandidateSource(string root, string releasePath, MultiPlatformPackageManifest host)
    {
        CandidateRuntimeSource source = host.RuntimeSource!;
        if (FileSystemSafety.Sha256(releasePath) != source.ReleaseManifestSha256)
            throw new InvalidDataException("Candidate release manifest differs from its imported source.");
        IEnumerable<string> expected = ExpectedRuntimeFiles.Select(name => "Content/DTMAPIInstaller/Payload/BepInEx/plugins/DTMAPI/" + name)
            .Concat(InstalledToolFiles.Concat(new[] { "install-bepinex.ps1", "install-to-game.ps1", "invoke-dtmapi-action.cmd", "probe-powershell-host.ps1", "dtmapi-product-definitions.json" })
                .Select(name => "Content/DTMAPIInstaller/tools/" + name))
            .Concat(new[] {
                "Content/DTMAPI/release-manifest.json",
                "Content/.tools/bepinex/BepInEx_win_x64_5.4.23.5.zip",
                "Content/DTMAPIInstaller/Payload/BepInEx/plugins/DTMAPI/assets/branding/dtmapi-icon.png",
                "Content/DTMAPIInstaller/Payload/DTMAPI/components/compatibility/DTMAPI.GameBridge.DolocTown.Compatibility.dll"
            });
        if (!source.SharedFiles.Select(file => file.RelativePath).OrderBy(path => path, StringComparer.Ordinal)
            .SequenceEqual(expected.OrderBy(path => path, StringComparer.Ordinal), StringComparer.Ordinal))
            throw new InvalidDataException("Candidate shared file set is not exact.");
        foreach (PackageFileReceipt file in source.SharedFiles)
            AssertReceipt(FileSystemSafety.CombineUnder(root, file.RelativePath), file.Length, file.Sha256, file.RelativePath);
    }

    private static List<InstallFile> ValidateRuntimePayload(string root, ReleaseManifestV1 release)
    {
        if (release.IncludedAssemblies.Count != ExpectedRuntimeFiles.Length)
            throw new InvalidDataException("The Runtime manifest must contain exactly five assemblies.");

        string[] names = release.IncludedAssemblies.Select(entry => entry.FileName).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        string[] expected = ExpectedRuntimeFiles.OrderBy(value => value, StringComparer.Ordinal).ToArray();
        if (!names.SequenceEqual(expected, StringComparer.Ordinal))
            throw new InvalidDataException("The Runtime assembly set is not the exact five-file production set.");

        List<InstallFile> result = new();
        foreach (RuntimeAssemblyReceipt entry in release.IncludedAssemblies)
        {
            string source = FileSystemSafety.CombineUnder(root,
                "Content/DTMAPIInstaller/Payload/BepInEx/plugins/DTMAPI/" + entry.FileName);
            AssertReceipt(source, entry.Length, entry.Sha256, entry.FileName);
            if (entry.FileVersion != release.BinaryVersion || FileVersionInfo.GetVersionInfo(source).FileVersion != release.BinaryVersion)
                throw new InvalidDataException("Runtime file version disagrees with the release: " + entry.FileName);
            result.Add(new InstallFile
            {
                Kind = "runtime-assembly",
                SourcePath = source,
                TargetRelativePath = "BepInEx/plugins/DTMAPI/" + entry.FileName,
                Length = entry.Length,
                Sha256 = entry.Sha256.ToLowerInvariant(),
                FileVersion = entry.FileVersion
            });
        }

        string branding = FileSystemSafety.CombineUnder(root,
            "Content/DTMAPIInstaller/Payload/BepInEx/plugins/DTMAPI/assets/branding/dtmapi-icon.png");
        FileInfo brandingInfo = new(branding);
        if (!brandingInfo.Exists || brandingInfo.Length == 0)
            throw new InvalidDataException("The Runtime branding asset is missing.");
        result.Add(new InstallFile
        {
            Kind = "runtime-asset",
            SourcePath = branding,
            TargetRelativePath = "BepInEx/plugins/DTMAPI/assets/branding/dtmapi-icon.png",
            Length = brandingInfo.Length,
            Sha256 = FileSystemSafety.Sha256(branding)
        });

        if (release.OptionalComponents.Count != 1)
            throw new InvalidDataException("The Runtime package must carry exactly one dormant compatibility component.");
        OptionalComponentReceipt component = release.OptionalComponents[0];
        if (!string.Equals(component.ComponentId, "gamebridge-compatibility-host", StringComparison.Ordinal) ||
            !string.Equals(component.RelativePath.Replace('\\', '/'),
                "DTMAPI/components/compatibility/DTMAPI.GameBridge.DolocTown.Compatibility.dll", StringComparison.Ordinal))
            throw new InvalidDataException("The compatibility component identity is invalid.");
        string componentSource = FileSystemSafety.CombineUnder(root,
            "Content/DTMAPIInstaller/Payload/" + component.RelativePath.Replace('\\', '/'));
        AssertReceipt(componentSource, component.Length, component.Sha256, component.ComponentId);
        if (component.FileVersion != release.BinaryVersion || FileVersionInfo.GetVersionInfo(componentSource).FileVersion != release.BinaryVersion)
            throw new InvalidDataException("Compatibility file version disagrees with the release.");
        result.Add(new InstallFile
        {
            Kind = "optional-framework-component",
            ComponentId = component.ComponentId,
            SourcePath = componentSource,
            TargetRelativePath = component.RelativePath.Replace('\\', '/'),
            Length = component.Length,
            Sha256 = component.Sha256.ToLowerInvariant(),
            FileVersion = component.FileVersion
        });

        return result;
    }

    private static void AddInstalledTools(string root, ICollection<InstallFile> result, bool candidate)
    {
        foreach (string fileName in candidate ? InstalledToolFiles.Append("dtmapi-product-definitions.json") : InstalledToolFiles)
        {
            string source = FileSystemSafety.CombineUnder(root, "Content/DTMAPIInstaller/tools/" + fileName);
            FileInfo info = new(source);
            if (!info.Exists || info.Length == 0)
                throw new InvalidDataException("Accepted Windows support tool is missing: " + fileName);
            result.Add(new InstallFile
            {
                Kind = fileName.EndsWith(".props", StringComparison.OrdinalIgnoreCase) ? "version-authority" :
                    fileName.Contains("collect", StringComparison.OrdinalIgnoreCase) || fileName.Contains("analyze", StringComparison.OrdinalIgnoreCase)
                        ? "diagnostic-tool" : "installer-tool",
                SourcePath = source,
                TargetRelativePath = "DTMAPI/tools/" + fileName,
                Length = info.Length,
                Sha256 = FileSystemSafety.Sha256(source)
            });
        }
    }

    private static void ValidateHostArtifacts(string root, MultiPlatformPackageManifest host)
    {
        if (host.HostArtifacts.Count != 2 ||
            host.HostArtifacts.Count(item => string.Equals(item.Rid, "win-x64", StringComparison.Ordinal)) != 1 ||
            host.HostArtifacts.Count(item => string.Equals(item.Rid, "linux-x64", StringComparison.Ordinal)) != 1)
            throw new InvalidDataException("The package must contain exactly one win-x64 host and one linux-x64 host.");

        foreach (PackageFileReceipt artifact in host.HostArtifacts)
        {
            string path = FileSystemSafety.CombineUnder(root, FileSystemSafety.NormalizeRelativePath(artifact.RelativePath));
            AssertReceipt(path, artifact.Length, artifact.Sha256, artifact.Rid + " host");
        }
    }

    private static void AssertReceipt(string path, long length, string sha256, string label)
    {
        if (length <= 0 || string.IsNullOrWhiteSpace(sha256) || !FileSystemSafety.Matches(path, length, sha256))
            throw new InvalidDataException(label + " does not match its declared length/SHA-256: " + path);
    }
}
