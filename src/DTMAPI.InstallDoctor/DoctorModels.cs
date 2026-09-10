using System;
using System.Collections.Generic;
using System.Linq;
using DTMAPI.Tooling.Metadata;

namespace DTMAPI.InstallDoctor;

public enum DoctorArtifactKind
{
    DtmApiRuntime,
    DtmApiCodeMod,
    DtmApiContentPack,
    ExternalBepInExPlugin,
    NativeBinary,
    DamagedAssembly,
    Unknown
}

public enum DoctorPlacement
{
    Expected,
    Misplaced,
    Unknown,
    NotApplicable
}

public enum DoctorSeverity
{
    Info,
    Warning,
    Error
}

public enum DoctorScanContext
{
    InstalledGame,
    PackageArtifact
}

public enum DoctorMinimumVersionStatus
{
    NotChecked,
    NotDeclared,
    Compatible,
    RuntimeUnavailable,
    RuntimeVersionInvalid,
    MinimumVersionInvalid,
    RequiresNewerRuntime
}

public enum DoctorManagedIdentity
{
    Unknown,
    DtmApiRuntime,
    StrictCodeMod,
    LegacyNativeCodeMod,
    AdvancedCodeMod,
    ContentPack,
    ExternalBepInExPlugin,
    NativeBinary
}

public enum DoctorDeclarationStatus
{
    NotApplicable,
    Explicit,
    CompatibilityDefault,
    LegacyCompatibility,
    Missing,
    Invalid
}

public enum DoctorProvenanceStatus
{
    NotApplicable,
    InvalidDeclaration,
    DeclaredOnly,
    Unverified,
    VerifiedReferenceReceipt,
    MissingReferenceReceipt,
    InvalidReferenceReceipt,
    MissingPackageBinding,
    InvalidPackageBinding,
    ReferenceReceiptMismatch,
    ExternalUnmanaged,
    VerifiedNativeContract
}

public enum DoctorNativeRisk
{
    Unknown,
    NotApplicable,
    NoCode,
    StableApiOnly,
    ThirdPartyAuthorManaged,
    ProductNative,
    ForbiddenNativeReference,
    ExternalUnmanaged
}

public enum DoctorCompatibilityStatus
{
    NotApplicable,
    NotChecked,
    ReceiptVerified,
    Compatible,
    Unverifiable,
    Incompatible,
    Exact,
    Drift,
    Unknown
}

public enum DoctorRestartPolicy
{
    NotApplicable,
    RestartRequiredAfterLoad,
    AuthorManagedRestartRequired,
    OutsideDtmApiManagement
}

public sealed class DoctorFinding
{
    public string Code { get; init; } = string.Empty;
    public DoctorSeverity Severity { get; init; }
    public string Path { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Guidance { get; init; } = string.Empty;
}

public sealed class DoctorArtifact
{
    public DoctorArtifactKind Kind { get; init; }
    public DoctorPlacement Placement { get; init; }
    public string Path { get; init; } = string.Empty;
    public string ManifestPath { get; init; } = string.Empty;
    public string UniqueId { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string MinimumDtmApiVersion { get; init; } = string.Empty;
    public DoctorMinimumVersionStatus MinimumVersionStatus { get; init; }
    public string AssemblyName { get; init; } = string.Empty;
    public string AssemblyVersion { get; init; } = string.Empty;
    public string Sha256 { get; init; } = string.Empty;
    public DoctorManagedIdentity ManagedIdentity { get; init; }
    public string DeclaredCodeModKind { get; init; } = string.Empty;
    public string EffectiveCodeModKind { get; init; } = string.Empty;
    public DoctorDeclarationStatus DeclarationStatus { get; init; }
    public DoctorProvenanceStatus ProvenanceStatus { get; init; }
    public string ProvenanceReceiptPath { get; init; } = string.Empty;
    public DoctorNativeRisk NativeRisk { get; init; }
    public DoctorCompatibilityStatus ReferenceCompatibility { get; init; }
    public DoctorCompatibilityStatus GameCompatibility { get; init; }
    public string ReferencePolicyId { get; init; } = string.Empty;
    public int ReferencePolicyVersion { get; init; }
    public string ReferencePolicySha256 { get; init; } = string.Empty;
    public int ReferenceCount { get; init; }
    public string GameBuildId { get; init; } = string.Empty;
    public string GameAssemblySha256 { get; init; } = string.Empty;
    public string ExpectedHarmonyOwner { get; init; } = string.Empty;
    public DoctorRestartPolicy RestartPolicy { get; init; }
    public IReadOnlyList<DoctorFinding> Findings { get; init; } = Array.Empty<DoctorFinding>();
}

public sealed class DoctorReport
{
    public int SchemaVersion { get; init; } = 2;
    public string RootPath { get; init; } = string.Empty;
    public DoctorScanContext ScanContext { get; init; }
    public bool ReadOnlyByDesign { get; init; } = true;
    public bool RuntimeVersionCheckRequested { get; init; }
    public string InstalledDtmApiVersion { get; init; } = string.Empty;
    public bool ReadOnlyVerificationRequested { get; init; }
    public bool? TreeUnchanged { get; init; }
    public TreeHashSnapshot? InitialTree { get; init; }
    public TreeHashSnapshot? FinalTree { get; init; }
    public IReadOnlyList<DoctorArtifact> Artifacts { get; init; } = Array.Empty<DoctorArtifact>();
    public IReadOnlyList<DoctorFinding> Findings { get; init; } = Array.Empty<DoctorFinding>();

    public int ErrorCount => CountSeverity(DoctorSeverity.Error);
    public int WarningCount => CountSeverity(DoctorSeverity.Warning);
    public int MisplacedCount => Artifacts.Count(value => value.Placement == DoctorPlacement.Misplaced);
    public int MinimumBlockedCount => Artifacts.Count(value => value.MinimumVersionStatus is
        DoctorMinimumVersionStatus.RuntimeUnavailable or
        DoctorMinimumVersionStatus.RuntimeVersionInvalid or
        DoctorMinimumVersionStatus.MinimumVersionInvalid or
        DoctorMinimumVersionStatus.RequiresNewerRuntime);

    private int CountSeverity(DoctorSeverity severity)
    {
        int count = 0;
        foreach (DoctorFinding finding in Findings)
        {
            if (finding.Severity == severity)
                count++;
        }
        foreach (DoctorArtifact artifact in Artifacts)
        {
            foreach (DoctorFinding finding in artifact.Findings)
            {
                if (finding.Severity == severity)
                    count++;
            }
        }
        return count;
    }
}

public sealed class DoctorOptions
{
    public bool VerifyReadOnlyTreeHash { get; init; }
    // Preserve the original unpacked-package Doctor behavior for Author SDK callers.
    // Player Doctor explicitly selects InstalledGame at its CLI boundary.
    public DoctorScanContext ScanContext { get; init; } = DoctorScanContext.PackageArtifact;
    public IReadOnlyList<DoctorFinding> ContextFindings { get; init; } = Array.Empty<DoctorFinding>();

    // null means the caller intentionally skipped compatibility checking;
    // empty means the caller checked and no installed Runtime version was available.
    public string? InstalledDtmApiVersion { get; init; }
}
