using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using DTMAPI.Tooling.Metadata;

namespace DTMAPI.InstallDoctor;

public enum InstalledRuntimeVersionProbeStatus
{
    Consistent,
    Missing,
    Invalid,
    Inconsistent
}

public sealed class InstalledRuntimeVersionProbeResult
{
    public InstalledRuntimeVersionProbeStatus Status { get; init; }
    public string InstalledDtmApiVersion { get; init; } = string.Empty;
    public IReadOnlyList<DoctorFinding> Findings { get; init; } = Array.Empty<DoctorFinding>();
}

public static class InstalledRuntimeVersionProbe
{
    private static readonly RuntimeAssemblySpec[] RuntimeAssemblies =
    {
        new("DTMAPI.BepInExBootstrap.dll", "DTMAPI.BepInExBootstrap"),
        new("DTMAPI.Abstractions.dll", "DTMAPI.Abstractions"),
        new("DTMAPI.Core.dll", "DTMAPI.Core"),
        new("DTMAPI.GameBridge.DolocTown.dll", "DTMAPI.GameBridge.DolocTown"),
        new("DTMAPI.ModConfigMenu.dll", "DTMAPI.ModConfigMenu")
    };

    public static InstalledRuntimeVersionProbeResult Inspect(string gameRoot)
    {
        string root = Path.GetFullPath(gameRoot);
        var findings = new List<DoctorFinding>();
        string stateRoot = Path.Combine(root, "DTMAPI");
        VersionReceipt installState = ReadReceipt(Path.Combine(stateRoot, "install-state.json"), "install-state", findings);
        VersionReceipt releaseManifest = ReadReceipt(Path.Combine(stateRoot, "release-manifest.json"), "release-manifest", findings);
        ValidateOptionalComponents(root, installState, releaseManifest, findings);

        string pluginRoot = Path.Combine(root, "BepInEx", "plugins", "DTMAPI");
        var dllVersions = new List<KeyValuePair<string, string>>();
        var missingDlls = new List<string>();
        bool assemblyIdentityInvalid = false;
        var metadataInspector = new PeMetadataInspector();
        foreach (RuntimeAssemblySpec assembly in RuntimeAssemblies)
        {
            string path = Path.Combine(pluginRoot, assembly.FileName);
            if (!File.Exists(path))
            {
                missingDlls.Add(assembly.FileName);
                continue;
            }

            PeMetadataInspection inspection = metadataInspector.Inspect(path);
            if (!inspection.IsManaged ||
                !inspection.AssemblyName.Equals(assembly.AssemblyName, StringComparison.Ordinal))
            {
                assemblyIdentityInvalid = true;
                findings.Add(Error(
                    "installed-runtime-assembly-identity-mismatch",
                    path,
                    "The installer-owned filename " + assembly.FileName + " contains assembly identity '" +
                    (inspection.AssemblyName.Length == 0 ? "<unreadable-or-native>" : inspection.AssemblyName) +
                    "', expected '" + assembly.AssemblyName + "'.",
                    "Do not trust a renamed or substituted DLL. Run 1_install_dtmapi.bat to restore the exact five-file Runtime set."));
            }

            try
            {
                string fileVersion = FileVersionInfo.GetVersionInfo(path).FileVersion ?? string.Empty;
                if (string.IsNullOrWhiteSpace(fileVersion))
                {
                    findings.Add(Error(
                        "installed-runtime-file-version-missing",
                        path,
                        "The Runtime DLL has no readable FileVersion.",
                        "Run 1_install_dtmapi.bat to repair the complete Runtime set."));
                }
                else
                {
                    dllVersions.Add(new KeyValuePair<string, string>(path, fileVersion.Trim()));
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                findings.Add(Error(
                    "installed-runtime-file-version-unreadable",
                    path,
                    "The Runtime DLL FileVersion could not be read: " + ex.GetType().Name + ": " + ex.Message,
                    "Stop writers or sync tools and repeat the read-only check; Doctor will not load or replace this DLL."));
            }
        }

        if (missingDlls.Count > 0)
        {
            findings.Add(Error(
                "installed-runtime-assembly-set-incomplete",
                pluginRoot,
                "The installed Runtime set is incomplete; missing " + missingDlls.Count + " of " + RuntimeAssemblies.Length + " DLLs: " + string.Join(", ", missingDlls) + ".",
                "Run 1_install_dtmapi.bat to repair the five installer-owned Runtime DLLs."));
        }

        bool missing = !installState.Exists || !releaseManifest.Exists || missingDlls.Count > 0;
        bool invalid = findings.Any(value => value.Code.Contains("invalid", StringComparison.Ordinal) ||
                                             value.Code.Contains("unreadable", StringComparison.Ordinal) ||
                                             value.Code.Contains("version-missing", StringComparison.Ordinal)) ||
                       assemblyIdentityInvalid;
        if (missing || invalid)
        {
            return Result(
                missing ? InstalledRuntimeVersionProbeStatus.Missing : InstalledRuntimeVersionProbeStatus.Invalid,
                string.Empty,
                findings);
        }

        if (!installState.DtmApiVersion.Equals(releaseManifest.DtmApiVersion, StringComparison.Ordinal) ||
            !installState.BinaryVersion.Equals(releaseManifest.BinaryVersion, StringComparison.Ordinal))
        {
            findings.Add(Error(
                "installed-runtime-receipt-version-inconsistent",
                stateRoot,
                "install-state.json and release-manifest.json do not project the same DTMAPI/Binary versions.",
                "Do not select either receipt as authoritative. Run 1_install_dtmapi.bat to restore one coherent Runtime transaction."));
        }

        string[] binaryVersions = dllVersions.Select(value => value.Value)
            .Append(installState.BinaryVersion)
            .Append(releaseManifest.BinaryVersion)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (binaryVersions.Length != 1)
        {
            findings.Add(Error(
                "installed-runtime-binary-version-inconsistent",
                pluginRoot,
                "The five Runtime DLL FileVersions and the two receipt BinaryVersion values disagree: " + string.Join(", ", binaryVersions) + ".",
                "Do not load a mixed Runtime. Run 1_install_dtmapi.bat to restore one complete version."));
        }

        if (!TryParseRuntimeVersion(installState.DtmApiVersion, out Version productVersion) ||
            !TryParseRuntimeVersion(installState.BinaryVersion, out Version binaryVersion))
        {
            findings.Add(Error(
                "installed-runtime-version-invalid",
                stateRoot,
                "The installed DTMAPI or Binary version is not parseable with Runtime compatibility semantics.",
                "Run 1_install_dtmapi.bat to replace the invalid receipts and Runtime set."));
        }
        else if (CompareVersions(productVersion, binaryVersion) != 0)
        {
            findings.Add(Error(
                "installed-runtime-product-binary-version-inconsistent",
                stateRoot,
                "The DTMAPI product version and Runtime DLL Binary/FileVersion do not represent the same numeric version.",
                "Do not choose one source. Run 1_install_dtmapi.bat to restore a coherent Runtime transaction."));
        }

        if (findings.Any(value => value.Severity == DoctorSeverity.Error))
            return Result(InstalledRuntimeVersionProbeStatus.Inconsistent, string.Empty, findings);

        return Result(InstalledRuntimeVersionProbeStatus.Consistent, installState.DtmApiVersion, findings);
    }

    private static VersionReceipt ReadReceipt(string path, string label, ICollection<DoctorFinding> findings)
    {
        if (!File.Exists(path))
        {
            findings.Add(Error(
                "installed-runtime-" + label + "-missing",
                path,
                label + " is missing, so the installed Runtime version cannot be established consistently.",
                "Run 1_install_dtmapi.bat to restore the installer-owned Runtime receipt."));
            return new VersionReceipt(false, string.Empty, string.Empty, false, Array.Empty<OptionalComponentReceipt>());
        }

        try
        {
            using FileStream stream = new(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete,
                16 * 1024,
                FileOptions.SequentialScan);
            using JsonDocument document = JsonDocument.Parse(stream, new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            });
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException("receipt root must be a JSON object");

            string productVersion = ReadString(document.RootElement, "DTMAPIVersion");
            string binaryVersion = ReadString(document.RootElement, "BinaryVersion");
            bool hasOptionalComponents = document.RootElement.TryGetProperty("OptionalComponents", out _);
            OptionalComponentReceipt[] optionalComponents = ReadOptionalComponents(document.RootElement);
            if (productVersion.Length == 0 || binaryVersion.Length == 0)
            {
                findings.Add(Error(
                    "installed-runtime-" + label + "-version-missing",
                    path,
                    label + " does not contain non-empty DTMAPIVersion and BinaryVersion values.",
                    "Run 1_install_dtmapi.bat to restore a complete Runtime receipt."));
            }
            return new VersionReceipt(true, productVersion, binaryVersion, hasOptionalComponents, optionalComponents);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException or InvalidDataException)
        {
            findings.Add(Error(
                "installed-runtime-" + label + "-invalid",
                path,
                label + " could not be parsed: " + ex.GetType().Name + ": " + ex.Message,
                "Run 1_install_dtmapi.bat to restore the installer-owned Runtime receipt."));
            return new VersionReceipt(true, string.Empty, string.Empty, false, Array.Empty<OptionalComponentReceipt>());
        }
    }

    private static OptionalComponentReceipt[] ReadOptionalComponents(JsonElement root)
    {
        if (!root.TryGetProperty("OptionalComponents", out JsonElement array) || array.ValueKind != JsonValueKind.Array)
            return Array.Empty<OptionalComponentReceipt>();
        return array.EnumerateArray()
            .Where(value => value.ValueKind == JsonValueKind.Object)
            .Select(value => new OptionalComponentReceipt(
                ReadString(value, "ComponentId"),
                ReadString(value, "Distribution"),
                ReadString(value, "RelativePath"),
                ReadString(value, "Path"),
                ReadString(value, "Sha256"),
                value.TryGetProperty("Length", out JsonElement length) && length.TryGetInt64(out long bytes) ? bytes : 0,
                ReadString(value, "AssemblyName"),
                ReadString(value, "AssemblyVersion"),
                ReadString(value, "FileVersion"),
                ReadString(value, "TargetFramework"),
                ReadString(value, "LoadPolicy"),
                ReadString(value, "DefaultLoadState"),
                value.TryGetProperty("IncludedInDownloadPackage", out JsonElement included) &&
                    included.ValueKind == JsonValueKind.True))
            .ToArray();
    }

    private static void ValidateOptionalComponents(
        string gameRoot,
        VersionReceipt installState,
        VersionReceipt releaseManifest,
        ICollection<DoctorFinding> findings)
    {
        const string compatibilityComponentId = "gamebridge-compatibility-host";
        const string compatibilityRelativePath = "DTMAPI/components/compatibility/DTMAPI.GameBridge.DolocTown.Compatibility.dll";
        const string compatibilityAssemblyName = "DTMAPI.GameBridge.DolocTown.Compatibility";
        const string compatibilityAssemblyVersion = "0.5.3.0";
        const string compatibilityTargetFramework = "netstandard2.0";
        const string compatibilityMetadataTargetFramework = ".NETStandard,Version=v2.0";
        if (!installState.Exists || !releaseManifest.Exists)
            return;
        bool hostRequired =
            RequiresCompatibilityHost(installState.DtmApiVersion) ||
            RequiresCompatibilityHost(releaseManifest.DtmApiVersion);
        if (!installState.HasOptionalComponentsProperty && !releaseManifest.HasOptionalComponentsProperty)
        {
            if (hostRequired)
            {
                findings.Add(Error(
                    "installed-runtime-optional-component-missing",
                    Path.Combine(gameRoot, compatibilityRelativePath.Replace('/', Path.DirectorySeparatorChar)),
                    "DTMAPI 0.5.5 and later require both Runtime receipts to project the one dormant Compatibility Host.",
                    "Run 1_install_dtmapi.bat to restore the Catalog-driven Compatibility Host and coherent receipts."));
            }
            return;
        }
        if (installState.HasOptionalComponentsProperty != releaseManifest.HasOptionalComponentsProperty)
        {
            findings.Add(Error(
                "installed-runtime-optional-component-receipts-inconsistent",
                Path.Combine(gameRoot, "DTMAPI"),
                "Only one Runtime receipt projects OptionalComponents.",
                "Run 1_install_dtmapi.bat to restore one coherent optional-component transaction."));
            return;
        }
        if (releaseManifest.OptionalComponents.Count != installState.OptionalComponents.Count ||
            (hostRequired &&
             (releaseManifest.OptionalComponents.Count != 1 ||
              installState.OptionalComponents.Count != 1)) ||
            (!hostRequired && releaseManifest.OptionalComponents.Count == 0))
        {
            findings.Add(Error(
                "installed-runtime-optional-component-receipts-inconsistent",
                Path.Combine(gameRoot, "DTMAPI"),
                "The dormant-shipped optional component receipts are missing or disagree between install-state.json and release-manifest.json.",
                "Run 1_install_dtmapi.bat to restore the Catalog-driven optional framework component set."));
            return;
        }

        foreach (OptionalComponentReceipt release in releaseManifest.OptionalComponents)
        {
            OptionalComponentReceipt[] stateMatches = installState.OptionalComponents
                .Where(value => value.ComponentId.Equals(release.ComponentId, StringComparison.Ordinal))
                .ToArray();
            string normalizedRelative = release.RelativePath.Replace('\\', '/');
            string expectedPath = Path.GetFullPath(Path.Combine(gameRoot, normalizedRelative.Replace('/', Path.DirectorySeparatorChar)));
            string componentsRoot = Path.GetFullPath(Path.Combine(gameRoot, "DTMAPI", "components")) + Path.DirectorySeparatorChar;
            if (!release.ComponentId.Equals(compatibilityComponentId, StringComparison.Ordinal) ||
                !release.RelativePath.Equals(compatibilityRelativePath, StringComparison.Ordinal) ||
                !release.AssemblyName.Equals(compatibilityAssemblyName, StringComparison.Ordinal) ||
                !release.AssemblyVersion.Equals(compatibilityAssemblyVersion, StringComparison.Ordinal) ||
                !release.TargetFramework.Equals(compatibilityTargetFramework, StringComparison.Ordinal) ||
                stateMatches.Length != 1 ||
                !normalizedRelative.StartsWith("DTMAPI/components/", StringComparison.Ordinal) ||
                !expectedPath.StartsWith(componentsRoot, StringComparison.OrdinalIgnoreCase) ||
                !release.Distribution.Equals("dormant-shipped", StringComparison.Ordinal) ||
                !release.TargetFramework.Equals("netstandard2.0", StringComparison.Ordinal) ||
                !release.LoadPolicy.Equals("first-frozen-abi-call", StringComparison.Ordinal) ||
                !release.DefaultLoadState.Equals("dormant", StringComparison.Ordinal) ||
                !release.IncludedInDownloadPackage ||
                release.Length <= 0 || release.Sha256.Length != 64 ||
                !stateMatches[0].RelativePath.Equals(release.RelativePath, StringComparison.Ordinal) ||
                !stateMatches[0].Path.Equals(expectedPath, StringComparison.OrdinalIgnoreCase) ||
                !stateMatches[0].Sha256.Equals(release.Sha256, StringComparison.OrdinalIgnoreCase) ||
                stateMatches[0].Length != release.Length ||
                !stateMatches[0].AssemblyName.Equals(release.AssemblyName, StringComparison.Ordinal) ||
                !stateMatches[0].AssemblyVersion.Equals(release.AssemblyVersion, StringComparison.Ordinal) ||
                !stateMatches[0].FileVersion.Equals(release.FileVersion, StringComparison.Ordinal) ||
                !stateMatches[0].TargetFramework.Equals(release.TargetFramework, StringComparison.Ordinal) ||
                !stateMatches[0].Distribution.Equals(release.Distribution, StringComparison.Ordinal) ||
                !stateMatches[0].LoadPolicy.Equals(release.LoadPolicy, StringComparison.Ordinal) ||
                !stateMatches[0].DefaultLoadState.Equals(release.DefaultLoadState, StringComparison.Ordinal) ||
                stateMatches[0].IncludedInDownloadPackage != release.IncludedInDownloadPackage)
            {
                findings.Add(Error(
                    "installed-runtime-optional-component-receipt-invalid",
                    expectedPath,
                    "An optional framework component receipt has an unsafe path or inconsistent identity.",
                    "Do not load the component. Run 1_install_dtmapi.bat to restore the package-owned bytes."));
                continue;
            }
            if (!File.Exists(expectedPath))
            {
                findings.Add(Error(
                    "installed-runtime-optional-component-missing",
                    expectedPath,
                    "A dormant-shipped optional framework component is missing.",
                    "Run 1_install_dtmapi.bat to restore it outside the BepInEx plugin scan path."));
                continue;
            }
            try
            {
                var file = new FileInfo(expectedPath);
                string hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(expectedPath)));
                PeMetadataInspection inspection = new PeMetadataInspector().Inspect(expectedPath);
                string fileVersion = FileVersionInfo.GetVersionInfo(expectedPath).FileVersion ?? string.Empty;
                if (file.Length != release.Length ||
                    !hash.Equals(release.Sha256, StringComparison.OrdinalIgnoreCase) ||
                    !inspection.IsManaged || !inspection.AssemblyName.Equals(release.AssemblyName, StringComparison.Ordinal) ||
                    !string.Equals(inspection.AssemblyVersion?.ToString() ?? string.Empty, compatibilityAssemblyVersion, StringComparison.Ordinal) ||
                    !inspection.TargetFramework.Equals(compatibilityMetadataTargetFramework, StringComparison.Ordinal) ||
                    !fileVersion.Equals(release.FileVersion, StringComparison.Ordinal))
                {
                    throw new InvalidDataException("length/hash/managed identity/FileVersion mismatch");
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
            {
                findings.Add(Error(
                    "installed-runtime-optional-component-bytes-invalid",
                    expectedPath,
                    "The optional framework component does not match its installer receipts: " + ex.Message,
                    "Do not load the component. Run 1_install_dtmapi.bat to restore the exact package-owned bytes."));
            }
        }
    }

    private static bool RequiresCompatibilityHost(string value) =>
        TryParseRuntimeVersion(value, out Version version) &&
        CompareVersions(version, new Version(0, 5, 5)) >= 0;

    private static string ReadString(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out JsonElement value) || value.ValueKind != JsonValueKind.String)
            return string.Empty;
        return (value.GetString() ?? string.Empty).Trim();
    }

    private static bool TryParseRuntimeVersion(string value, out Version version)
    {
        version = new Version(0, 0, 0, 0);
        string text = (value ?? string.Empty).Trim();
        int suffixIndex = text.IndexOfAny(new[] { '-', '+' });
        if (suffixIndex >= 0)
            text = text.Substring(0, suffixIndex);
        if (!Version.TryParse(text, out Version? parsed) || parsed == null)
            return false;
        version = parsed;
        return true;
    }

    private static int CompareVersions(Version left, Version right)
    {
        int[] leftParts = { left.Major, left.Minor, Math.Max(left.Build, 0), Math.Max(left.Revision, 0) };
        int[] rightParts = { right.Major, right.Minor, Math.Max(right.Build, 0), Math.Max(right.Revision, 0) };
        for (int index = 0; index < leftParts.Length; index++)
        {
            int comparison = leftParts[index].CompareTo(rightParts[index]);
            if (comparison != 0)
                return comparison;
        }
        return 0;
    }

    private static DoctorFinding Error(string code, string path, string message, string guidance)
    {
        return new DoctorFinding
        {
            Code = code,
            Severity = DoctorSeverity.Error,
            Path = path,
            Message = message,
            Guidance = guidance
        };
    }

    private static InstalledRuntimeVersionProbeResult Result(
        InstalledRuntimeVersionProbeStatus status,
        string version,
        IReadOnlyList<DoctorFinding> findings)
    {
        return new InstalledRuntimeVersionProbeResult
        {
            Status = status,
            InstalledDtmApiVersion = version,
            Findings = findings
                .OrderByDescending(value => value.Severity)
                .ThenBy(value => value.Code, StringComparer.Ordinal)
                .ThenBy(value => value.Path, StringComparer.OrdinalIgnoreCase)
                .ToArray()
        };
    }

    private sealed class VersionReceipt
    {
        public VersionReceipt(bool exists, string dtmApiVersion, string binaryVersion, bool hasOptionalComponentsProperty, IReadOnlyList<OptionalComponentReceipt> optionalComponents)
        {
            Exists = exists;
            DtmApiVersion = dtmApiVersion;
            BinaryVersion = binaryVersion;
            HasOptionalComponentsProperty = hasOptionalComponentsProperty;
            OptionalComponents = optionalComponents;
        }

        public bool Exists { get; }
        public string DtmApiVersion { get; }
        public string BinaryVersion { get; }
        public bool HasOptionalComponentsProperty { get; }
        public IReadOnlyList<OptionalComponentReceipt> OptionalComponents { get; }
    }

    private sealed record OptionalComponentReceipt(
        string ComponentId,
        string Distribution,
        string RelativePath,
        string Path,
        string Sha256,
        long Length,
        string AssemblyName,
        string AssemblyVersion,
        string FileVersion,
        string TargetFramework,
        string LoadPolicy,
        string DefaultLoadState,
        bool IncludedInDownloadPackage);

    private sealed class RuntimeAssemblySpec
    {
        public RuntimeAssemblySpec(string fileName, string assemblyName)
        {
            FileName = fileName;
            AssemblyName = assemblyName;
        }

        public string FileName { get; }
        public string AssemblyName { get; }
    }
}
