using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DTMAPI.MultiPlatformInstaller;

internal enum InstallerAction
{
    Menu,
    Install,
    Uninstall,
    Status,
    CollectLogs,
    Help
}

internal enum FileHealth
{
    Healthy,
    NotInstalled,
    Partial,
    UpdateRequired,
    Blocked
}

internal enum LaunchIntegrationHealth
{
    NotRequired,
    Configured,
    Missing,
    Unverified
}

internal enum RuntimeObservationHealth
{
    FreshObserved,
    StaleOrMissing,
    Unbound
}

internal sealed class InstallerOptions
{
    public InstallerAction Action { get; set; } = InstallerAction.Menu;
    public string? GamePath { get; set; }
    public string? PackageRoot { get; set; }
    public string? OutputPath { get; set; }
    public bool NonInteractive { get; set; }
    public bool Pause { get; set; }
}

internal sealed class InstallerResult
{
    public string Code { get; set; } = "DTM-E9000";
    public int ExitCode { get; set; } = 10;
    public string Chinese { get; set; } = string.Empty;
    public string English { get; set; } = string.Empty;
    public List<string> Details { get; set; } = new();

    public static InstallerResult Success(string code, string chinese, string english, params string[] details)
    {
        return new InstallerResult
        {
            Code = code,
            ExitCode = 0,
            Chinese = chinese,
            English = english,
            Details = new List<string>(details)
        };
    }

    public static InstallerResult Failure(string code, int exitCode, string chinese, string english, params string[] details)
    {
        return new InstallerResult
        {
            Code = code,
            ExitCode = exitCode,
            Chinese = chinese,
            English = english,
            Details = new List<string>(details)
        };
    }
}

internal sealed class ReleaseManifestV1
{
    public int SchemaVersion { get; set; }
    public string DTMAPIVersion { get; set; } = string.Empty;
    public string BinaryVersion { get; set; } = string.Empty;
    public string BuildCommit { get; set; } = string.Empty;
    public string PackageKind { get; set; } = string.Empty;
    public List<RuntimeAssemblyReceipt> IncludedAssemblies { get; set; } = new();
    public List<OptionalComponentReceipt> OptionalComponents { get; set; } = new();
}

internal sealed class RuntimeAssemblyReceipt
{
    public string FileName { get; set; } = string.Empty;
    public long Length { get; set; }
    public string Sha256 { get; set; } = string.Empty;
    public string FileVersion { get; set; } = string.Empty;
}

internal sealed class OptionalComponentReceipt
{
    public string ComponentId { get; set; } = string.Empty;
    public string Distribution { get; set; } = string.Empty;
    public string LoadPolicy { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public long Length { get; set; }
    public string Sha256 { get; set; } = string.Empty;
    public string AssemblyName { get; set; } = string.Empty;
    public string AssemblyVersion { get; set; } = string.Empty;
    public string FileVersion { get; set; } = string.Empty;
    public string TargetFramework { get; set; } = string.Empty;
    public string DefaultLoadState { get; set; } = string.Empty;
    public bool IncludedInDownloadPackage { get; set; }
}

internal sealed class MultiPlatformPackageManifest
{
    public int SchemaVersion { get; set; }
    public string WorkshopId { get; set; } = string.Empty;
    public string InstallerBuildCommit { get; set; } = string.Empty;
    public CandidateRuntimeSource? RuntimeSource { get; set; }
    public string DistributionId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string InstallerVersion { get; set; } = string.Empty;
    public string RuntimeVersion { get; set; } = string.Empty;
    public string SourceWorkshopId { get; set; } = string.Empty;
    public string SourceWorkshopManifestId { get; set; } = string.Empty;
    public string SourcePlayerPayloadTreeSha256 { get; set; } = string.Empty;
    public PackageFileReceipt BepInExArchive { get; set; } = new();
    public List<PackageFileReceipt> HostArtifacts { get; set; } = new();
    public string RequiredLinuxLaunchOption { get; set; } = string.Empty;
    public string CrossOverDllOverride { get; set; } = string.Empty;
}

internal sealed class CandidateRuntimeSource
{
    public string Kind { get; set; } = string.Empty;
    public string BuildCommit { get; set; } = string.Empty;
    public string ReleaseManifestSha256 { get; set; } = string.Empty;
    public List<PackageFileReceipt> SharedFiles { get; set; } = new();
}

internal sealed class PackageFileReceipt
{
    public string Rid { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public long Length { get; set; }
    public string Sha256 { get; set; } = string.Empty;
}

internal sealed class InstallStateV1
{
    public int SchemaVersion { get; set; } = 1;
    public string InstalledAt { get; set; } = string.Empty;
    public string DTMAPIVersion { get; set; } = string.Empty;
    public string BinaryVersion { get; set; } = string.Empty;
    public string SourceRepoCommit { get; set; } = string.Empty;
    public string GameDir { get; set; } = string.Empty;
    public string PluginDir { get; set; } = string.Empty;
    public List<InstalledFileReceipt> FilesInstalled { get; set; } = new();
    public List<OptionalComponentReceipt> OptionalComponents { get; set; } = new();
    public bool BepInExDetectedBeforeInstall { get; set; }
    public bool BepInExInstalledByDTMAPI { get; set; }
    public List<string> BackupsCreated { get; set; } = new();
    public List<string> LegacyModsMoved { get; set; } = new();
    public List<string> LegacyDetections { get; set; } = new();
    public int QaFixturesInstalledCount { get; set; }
    public List<string> QaFixturesInstalled { get; set; } = new();
    public string InstallScriptVersion { get; set; } = "multiplatform-1";
    public bool DryRun { get; set; }
    public string InstallerDistribution { get; set; } = "DTMAPI.MultiPlatform";
    public string InstallerHost { get; set; } = string.Empty;
}

internal sealed class InstalledFileReceipt
{
    public string Kind { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string Sha256 { get; set; } = string.Empty;
    public long? Length { get; set; }
    public string FileVersion { get; set; } = string.Empty;
    public string ComponentId { get; set; } = string.Empty;
}

internal sealed class TransactionReceiptV1
{
    public int SchemaVersion { get; set; } = 1;
    public string TransactionId { get; set; } = string.Empty;
    public string CreatedAtUtc { get; set; } = string.Empty;
    public string GameDir { get; set; } = string.Empty;
    public string Engine { get; set; } = string.Empty;
    public string Phase { get; set; } = "Prepared";
    public List<TransactionOperation> Operations { get; set; } = new();
}

internal sealed class TransactionOperation
{
    public int Ordinal { get; set; }
    public string TargetRelativePath { get; set; } = string.Empty;
    public string CandidateRelativePath { get; set; } = string.Empty;
    public string BackupRelativePath { get; set; } = string.Empty;
    public string CandidateSha256 { get; set; } = string.Empty;
    public long CandidateLength { get; set; }
    public bool HadOriginal { get; set; }
    public string State { get; set; } = "Pending";
}

internal sealed class UninstallStateV1
{
    public int SchemaVersion { get; set; } = 1;
    public string RemovedAtUtc { get; set; } = string.Empty;
    public string GameDir { get; set; } = string.Empty;
    public string BackupRoot { get; set; } = string.Empty;
    public List<string> Removed { get; set; } = new();
    public List<string> Preserved { get; set; } = new();
    public bool NoOp { get; set; }
}

internal sealed class StatusReport
{
    public string GeneratedAtUtc { get; set; } = string.Empty;
    public string GameDir { get; set; } = string.Empty;
    public FileHealth Files { get; set; }
    public LaunchIntegrationHealth LaunchIntegration { get; set; }
    public RuntimeObservationHealth RuntimeObservation { get; set; }
    public List<string> Findings { get; set; } = new();
    public string RequiredAction { get; set; } = string.Empty;
}

internal sealed class LogCollectionManifest
{
    public int SchemaVersion { get; set; } = 1;
    public string CreatedAtUtc { get; set; } = string.Empty;
    public string GameDir { get; set; } = string.Empty;
    public List<CollectedLogReceipt> Files { get; set; } = new();
    public List<string> MissingOrSkipped { get; set; } = new();
    public string PrivacyNotice { get; set; } = string.Empty;
}

internal sealed class CollectedLogReceipt
{
    public string Source { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public long Length { get; set; }
    public string Sha256 { get; set; } = string.Empty;
}

internal sealed class InstallerSettings
{
    public string GamePath { get; set; } = string.Empty;
}

[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    WriteIndented = true,
    GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(ReleaseManifestV1))]
[JsonSerializable(typeof(MultiPlatformPackageManifest))]
[JsonSerializable(typeof(InstallStateV1))]
[JsonSerializable(typeof(TransactionReceiptV1))]
[JsonSerializable(typeof(UninstallStateV1))]
[JsonSerializable(typeof(StatusReport))]
[JsonSerializable(typeof(LogCollectionManifest))]
[JsonSerializable(typeof(InstallerSettings))]
internal partial class InstallerJsonContext : JsonSerializerContext
{
}
