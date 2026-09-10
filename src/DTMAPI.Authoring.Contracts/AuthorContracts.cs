using System.Text.Json.Serialization;

namespace DTMAPI.Authoring.Contracts;

public static class AuthorSdkContract
{
    public const string SdkVersion = "0.7.0";
    // Legacy target/default values retained for existing contracts. Current
    // project selection and supported package combinations come from target-catalog.json.
    // The frozen payload's SDK version is independent of the running tool version.
    public const string TargetRuntimeVersion = "0.5.5";
    public const string HighestSupportedRuntimeVersion = "0.7.0";
    public const int AuthorProjectSchemaVersion = 2;
    public const int DependencyAuthorProjectSchemaVersion = 3;
    public const int LegacyAuthorProjectSchemaVersion = 1;
    public const int CompatibilitySchemaVersion = 1;
    public const int PackageReportSchemaVersion = 2;
    public const int AdvancedReferencePolicyRegistrySchemaVersion = 2;
    public const int AdvancedReferencePolicySchemaVersion = 1;
    public const int AdvancedReferenceReceiptSchemaVersion = 1;
    public const int DeploymentSchemaVersion = 2;
    public const int DeploymentJournalSchemaVersion = 3;
    public const int OfficialDeploymentJournalSchemaVersion = 4;
    public const string AdvancedReferenceReceiptKind = "DTMAPI.AdvancedCodeMod.ReferenceReceipt";
    public const string AdvancedReferenceReceiptPath = "Content/DTMAPI/dtmapi-advanced-references.json";
    public const string PackageManifestPath = "Content/DTMAPI/manifest.json";
}

public static class AuthorSessionContract
{
    public const int SchemaVersion = 2;
    public const int ProtocolMajor = 1;
    public const int MinimumMinor = 0;
    public const int MaximumMinor = 0;
    public const string MinimumRuntimeVersion = "0.6.1";
    public const string LegacyWireVersion = "0.5.5";
    public const string SnapshotCapability = "get-source-snapshot/1";
    public const string ReloadCapability = "reload-content/1";
    public const string CommandCapability = "execute-command/1";
}

public enum AuthorProjectKind
{
    CodeMod,
    ContentPack
}

public enum AuthorCodeModKind
{
    Strict,
    Advanced
}

public enum DiagnosticSeverity
{
    Info,
    Warning,
    Error
}

public sealed class AuthorDiagnostic
{
    public string Code { get; set; } = string.Empty;
    public DiagnosticSeverity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Guidance { get; set; } = string.Empty;
}

public sealed class CommandReport
{
    public string Command { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string SdkVersion { get; set; } = AuthorSdkContract.SdkVersion;
    public string TargetRuntimeVersion { get; set; } = AuthorSdkContract.TargetRuntimeVersion;
    public string RootPath { get; set; } = string.Empty;
    public string OutputPath { get; set; } = string.Empty;
    public string Sha256 { get; set; } = string.Empty;
    public int FileCount { get; set; }
    public List<AuthorDiagnostic> Diagnostics { get; set; } = new();
    public SortedDictionary<string, string> Values { get; set; } = new(StringComparer.Ordinal);
}

public sealed class RuntimeManifest
{
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public int? NativeContractVersion { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public int? DependencyContractVersion { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string UniqueID { get; set; } = string.Empty;
    public string EntryDll { get; set; } = string.Empty;
    public string EntryType { get; set; } = string.Empty;
    public string MinimumDTMApiVersion { get; set; } = string.Empty;
    public string MinimumGameVersion { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string CodeModKind { get; set; } = string.Empty;
    public List<RuntimeManifestDependency> Dependencies { get; set; } = new();
    public List<string> UpdateKeys { get; set; } = new();
}

public sealed class RuntimeManifestDependency
{
    public string UniqueID { get; set; } = string.Empty;
    public string MinimumVersion { get; set; } = string.Empty;
    public bool Required { get; set; } = true;
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public DependencyVersionRange? VersionRange { get; set; }
}

public sealed class DependencyVersionRange
{
    [System.Text.Json.Serialization.JsonPropertyName("minimumInclusive")]
    public string MinimumInclusive { get; set; } = string.Empty;
    [System.Text.Json.Serialization.JsonPropertyName("maximumExclusive")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? MaximumExclusive { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("includePrerelease")]
    public bool IncludePrerelease { get; set; }
}

public sealed class ManagedAuthorReference
{
    public string Path { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Distribution { get; set; } = string.Empty;
    public List<string> LicenseFiles { get; set; } = new();
}

public sealed class AuthorProject
{
    public int SchemaVersion { get; set; }
    public string ProjectKind { get; set; } = string.Empty;
    public string TargetRuntimeVersion { get; set; } = string.Empty;
    public string TargetDtmApiVersion { get; set; } = string.Empty;
    public string CodeModKind { get; set; } = string.Empty;
    public string SourceDirectory { get; set; } = "src";
    public string ContentDirectory { get; set; } = "content";
    public string AssemblyName { get; set; } = string.Empty;
    public AdvancedAuthorProject? Advanced { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public NativeAuthorReferences? NativeReferences { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public List<ManagedAuthorReference>? ManagedReferences { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AuthorBuildSettings? Build { get; set; }
    public AuthorPublishMetadata Publish { get; set; } = new();
}

public sealed class AuthorBuildSettings
{
    public string WorkspaceRoot { get; set; } = ".";
    public List<string> ProjectReferences { get; set; } = new();
    public SortedDictionary<string, AuthorProjectReferenceMetadata> ProjectReferenceMetadata { get; set; } = new(StringComparer.Ordinal);
    public List<AuthorEmbeddedResource> EmbeddedResources { get; set; } = new();
    public List<AuthorContentFile> ContentFiles { get; set; } = new();
    public List<string> GeneratedSourceFiles { get; set; } = new();
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AuthorRestoreSettings? Restore { get; set; }
}

public sealed class AuthorProjectReferenceMetadata
{
    public string Role { get; set; } = "private-managed";
    public string Distribution { get; set; } = "self-authored";
    public List<string> LicenseFiles { get; set; } = new();
}

public sealed class AuthorRestoreSettings
{
    public string LockFile { get; set; } = "packages.lock.json";
    public string CacheDirectory { get; set; } = "obj/dtmapi-author/packages";
    public List<string> Sources { get; set; } = new();
    public SortedDictionary<string, string> Packages { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    // Explicit local license material, keyed by every package ID in the lock.
    public SortedDictionary<string, List<string>> LicenseFiles { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class AuthorEmbeddedResource
{
    public string Path { get; set; } = "";
    public string LogicalName { get; set; } = "";
}

public sealed class AuthorContentFile
{
    public string Path { get; set; } = "";
    public string TargetPath { get; set; } = "";
}

public sealed class AuthorLibraryProject
{
    public int SchemaVersion { get; set; } = 1;
    public string AssemblyName { get; set; } = "";
    public string Version { get; set; } = "0.1.0";
    public string ApiTarget { get; set; } = "0.7.0";
    public string TargetFramework { get; set; } = "netstandard2.0";
    public string Role { get; set; } = "private-managed";
    public string SourceDirectory { get; set; } = "src";
    public List<ManagedAuthorReference> ManagedReferences { get; set; } = new();
    public AuthorBuildSettings Build { get; set; } = new();
}

public sealed class AdvancedAuthorProject
{
    public string ReferencePolicyId { get; set; } = string.Empty;
    public List<string> References { get; set; } = new();
}

public sealed class NativeAuthorReferences
{
    public string GameRoot { get; set; } = "";
    public List<string> References { get; set; } = new();
    public string HarmonyOwner { get; set; } = "";
    public List<NativeAuthorMember> RequiredMembers { get; set; } = new();
}

public sealed class NativeAuthorMember
{
    // The shared reader strictly validates structured V2 rows and rejects mixed fields.
    [JsonExtensionData]
    public Dictionary<string, System.Text.Json.JsonElement>? StructuredFields { get; set; }
    public string AssemblyIdentity { get; set; } = "";
    public string DeclaringType { get; set; } = "";
    public string Kind { get; set; } = "";
    public string Name { get; set; } = "";
    public bool IsStatic { get; set; }
    public string ReturnType { get; set; } = "";
    public string[] ParameterTypes { get; set; } = System.Array.Empty<string>();
}

public sealed class AuthorPublishMetadata
{
    public List<string> Tags { get; set; } = new();
    public SortedDictionary<string, string> LocalizedName { get; set; } = new(StringComparer.Ordinal);
    public SortedDictionary<string, string> LocalizedDescription { get; set; } = new(StringComparer.Ordinal);
}

public sealed class CompatibilityManifest
{
    public int SchemaVersion { get; set; }
    public string SdkVersion { get; set; } = string.Empty;
    public string TargetRuntimeVersion { get; set; } = string.Empty;
    public string AbstractionsAssemblyVersion { get; set; } = string.Empty;
    public string AbstractionsFileVersion { get; set; } = string.Empty;
    public List<CompatibilityFile> Files { get; set; } = new();
}

public sealed class CompatibilityFile
{
    public string Path { get; set; } = string.Empty;
    public string Sha256 { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
}

public sealed class CompatibilityPayloadContract
{
    public int SchemaVersion { get; set; }
    public string SdkVersion { get; set; } = string.Empty;
    public string TargetRuntimeVersion { get; set; } = string.Empty;
    public string AbstractionsAssemblyVersion { get; set; } = string.Empty;
    public string AbstractionsFileVersion { get; set; } = string.Empty;
    public string AbstractionsSha256 { get; set; } = string.Empty;
    public string AuthorPropsSha256 { get; set; } = string.Empty;
    public string NetstandardReferencePackage { get; set; } = string.Empty;
    public string NetstandardReferencePackageVersion { get; set; } = string.Empty;
    public int NetstandardReferenceFileCount { get; set; }
    public string NetstandardReferenceInventoryAlgorithm { get; set; } = string.Empty;
    public string NetstandardReferenceInventorySha256 { get; set; } = string.Empty;
    public string NetstandardLicenseSha256 { get; set; } = string.Empty;
    public string NetstandardNoticeSha256 { get; set; } = string.Empty;
    public List<string> RequiredKinds { get; set; } = new();
    public string ReleaseManifestName { get; set; } = string.Empty;
}

public sealed class DeterministicPackageReport
{
    public int SchemaVersion { get; set; } = AuthorSdkContract.PackageReportSchemaVersion;
    public string SdkVersion { get; set; } = AuthorSdkContract.SdkVersion;
    public string TargetRuntimeVersion { get; set; } = AuthorSdkContract.TargetRuntimeVersion;
    public string UniqueID { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string ProjectKind { get; set; } = string.Empty;
    public string CodeModKind { get; set; } = string.Empty;
    public string PackageFileName { get; set; } = string.Empty;
    public string PackageSha256 { get; set; } = string.Empty;
    public string ManifestSha256 { get; set; } = string.Empty;
    public string EntryDllSha256 { get; set; } = string.Empty;
    public string AdvancedReferenceReceiptSha256 { get; set; } = string.Empty;
    public List<PackagedFile> Files { get; set; } = new();
}

public sealed class PackagedFile
{
    public string Path { get; set; } = string.Empty;
    public long Length { get; set; }
    public string Sha256 { get; set; } = string.Empty;
}

public sealed class DeploymentReceipt
{
    public int SchemaVersion { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public string GameRootKey { get; set; } = string.Empty;
    public string UniqueId { get; set; } = string.Empty;
    public string PackageKind { get; set; } = string.Empty;
    public string CodeModKind { get; set; } = string.Empty;
    public string DestinationRelativePath { get; set; } = string.Empty;
    public string PackageSha256 { get; set; } = string.Empty;
    public string ManifestSha256 { get; set; } = string.Empty;
    public string EntryDllSha256 { get; set; } = string.Empty;
    public string AdvancedReferenceReceiptSha256 { get; set; } = string.Empty;
    public string PackageMarkerSha256 { get; set; } = string.Empty;
    public string PayloadTreeSha256 { get; set; } = string.Empty;
    public int PayloadFileCount { get; set; }
    public int PayloadDirectoryCount { get; set; }
}

public sealed class DeploymentTreeInventory
{
    public string TreeSha256 { get; set; } = string.Empty;
    public List<DeploymentInventoryFile> Files { get; set; } = new();
    public List<string> Directories { get; set; } = new();
}

public sealed class DeploymentInventoryFile
{
    public string Path { get; set; } = string.Empty;
    public long Length { get; set; }
    public string Sha256 { get; set; } = string.Empty;
}

public sealed class DeploymentRecord
{
    public string TransactionId { get; set; } = string.Empty;
    public string PackageSha256 { get; set; } = string.Empty;
    public string PayloadTreeSha256 { get; set; } = string.Empty;
    public string ReceiptSha256 { get; set; } = string.Empty;
    public DeploymentTreeInventory Inventory { get; set; } = new();
}

public sealed class DeploymentTransaction
{
    public string TransactionId { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string Phase { get; set; } = string.Empty;
    public string StagingPath { get; set; } = string.Empty;
    public string RecoveryPath { get; set; } = string.Empty;
    public string FailedPath { get; set; } = string.Empty;
    public DeploymentRecord? Previous { get; set; }
    public DeploymentRecord? Next { get; set; }
}

public sealed class DeploymentRecoveryArtifact
{
    public string Role { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string TreeSha256 { get; set; } = string.Empty;
}

public sealed class LocalInstallTransaction
{
    public string TransactionId { get; set; } = string.Empty;
    public string Phase { get; set; } = string.Empty;
    public string ExpectedVersion { get; set; } = string.Empty;
    public string ExpectedPackageSha256 { get; set; } = string.Empty;
    public bool JournalBeforeExisted { get; set; }
    public string JournalBeforeBase64 { get; set; } = string.Empty;
    public bool SourceStateBeforeExisted { get; set; }
    public string SourceStateBeforeBase64 { get; set; } = string.Empty;
    public string SourceStatePath { get; set; } = string.Empty;
    public string StagingPath { get; set; } = string.Empty;
    public string RecoveryPath { get; set; } = string.Empty;
    public string FailedPath { get; set; } = string.Empty;
    public DeploymentRecord? Previous { get; set; }
    public DeploymentRecord Next { get; set; } = new();
}

public sealed class DeploymentJournal
{
    public int SchemaVersion { get; set; }
    public string GameRoot { get; set; } = string.Empty;
    public string GameRootKey { get; set; } = string.Empty;
    public string UniqueId { get; set; } = string.Empty;
    public string PackageKind { get; set; } = string.Empty;
    public string CodeModKind { get; set; } = string.Empty;
    public string DestinationPath { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DeploymentRecord? Committed { get; set; }
    public DeploymentTransaction? Active { get; set; }
    public LocalInstallTransaction? LocalInstall { get; set; }
    public List<DeploymentRecoveryArtifact> RecoveryArtifacts { get; set; } = new();
}

public sealed class AdvancedReferencePolicy
{
    public int SchemaVersion { get; set; }
    public string PolicyId { get; set; } = string.Empty;
    public int PolicyVersion { get; set; }
    public string GameBuildId { get; set; } = string.Empty;
    public string GameAssemblyRelativePath { get; set; } = string.Empty;
    public string GameAssemblySha256 { get; set; } = string.Empty;
    public List<AdvancedReferencePolicyEntry> References { get; set; } = new();
}

public sealed class AdvancedReferencePolicyRegistry
{
    public int SchemaVersion { get; set; }
    public List<AdvancedReferencePolicyRegistration> Policies { get; set; } = new();
}

public sealed class AdvancedReferencePolicyRegistration
{
    public string PolicyId { get; set; } = string.Empty;
    public string PolicySha256 { get; set; } = string.Empty;
    public string RequiredUniqueId { get; set; } = string.Empty;
    public string MinimumDtmApiVersion { get; set; } = string.Empty;
    public string CompilerSurfaceSha256 { get; set; } = string.Empty;
}

public sealed class AdvancedReferencePolicyEntry
{
    public string GameRelativePath { get; set; } = string.Empty;
    public string AssemblyName { get; set; } = string.Empty;
    public long Length { get; set; }
    public string Sha256 { get; set; } = string.Empty;
    public bool CopyLocal { get; set; }
}

public sealed class AdvancedReferenceReceipt
{
    public int SchemaVersion { get; set; }
    public string ReceiptKind { get; set; } = string.Empty;
    public string ReferencePolicyId { get; set; } = string.Empty;
    public int ReferencePolicyVersion { get; set; }
    public string ReferencePolicySha256 { get; set; } = string.Empty;
    public string UniqueId { get; set; } = string.Empty;
    public string CodeModKind { get; set; } = string.Empty;
    public string TargetFramework { get; set; } = string.Empty;
    public string GameBuildId { get; set; } = string.Empty;
    public string GameAssemblyRelativePath { get; set; } = string.Empty;
    public string GameAssemblySha256 { get; set; } = string.Empty;
    public string ManifestPath { get; set; } = string.Empty;
    public string ManifestSha256 { get; set; } = string.Empty;
    public string EntryDllPath { get; set; } = string.Empty;
    public long EntryDllLength { get; set; }
    public string EntryDllSha256 { get; set; } = string.Empty;
    public string HarmonyOwner { get; set; } = string.Empty;
    public List<AdvancedReferenceReceiptEntry> References { get; set; } = new();
}

public sealed class AdvancedReferenceReceiptEntry
{
    public string GameRelativePath { get; set; } = string.Empty;
    public string AssemblyName { get; set; } = string.Empty;
    public long Length { get; set; }
    public string Sha256 { get; set; } = string.Empty;
    public bool CopyLocal { get; set; }
}

public sealed class AuthorSourceState
{
    public int SchemaVersion { get; set; }
    public string GameRoot { get; set; } = string.Empty;
    public bool PlayerReproductionActive { get; set; }
    public string ReproductionSnapshotId { get; set; } = string.Empty;
    public List<AuthorSourceSelection> Selections { get; set; } = new();
}

public sealed class AuthorSourceSelection
{
    public string UniqueId { get; set; } = string.Empty;
    public string Mode { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public string ExpectedTreeSha256 { get; set; } = string.Empty;
}

public sealed class AuthorSourceSnapshot
{
    public int SchemaVersion { get; set; }
    public string SnapshotId { get; set; } = string.Empty;
    public string GameRoot { get; set; } = string.Empty;
    public List<AuthorSourceSelection> Selections { get; set; } = new();
}
