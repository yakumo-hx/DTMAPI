namespace DTMAPI.InstallDoctor;

internal sealed class DoctorIdentityProjection
{
    public DoctorManagedIdentity ManagedIdentity { get; init; } = DoctorManagedIdentity.Unknown;
    public string DeclaredCodeModKind { get; init; } = string.Empty;
    public string EffectiveCodeModKind { get; init; } = string.Empty;
    public DoctorDeclarationStatus DeclarationStatus { get; init; } = DoctorDeclarationStatus.NotApplicable;
    public DoctorProvenanceStatus ProvenanceStatus { get; set; } = DoctorProvenanceStatus.NotApplicable;
    public string ProvenanceReceiptPath { get; set; } = string.Empty;
    public DoctorNativeRisk NativeRisk { get; set; } = DoctorNativeRisk.Unknown;
    public DoctorCompatibilityStatus ReferenceCompatibility { get; set; } = DoctorCompatibilityStatus.NotApplicable;
    public DoctorCompatibilityStatus GameCompatibility { get; set; } = DoctorCompatibilityStatus.NotApplicable;
    public string ReferencePolicyId { get; set; } = string.Empty;
    public int ReferencePolicyVersion { get; set; }
    public string ReferencePolicySha256 { get; set; } = string.Empty;
    public int ReferenceCount { get; set; }
    public string GameBuildId { get; set; } = string.Empty;
    public string GameAssemblySha256 { get; set; } = string.Empty;
    public string ExpectedHarmonyOwner { get; set; } = string.Empty;
    public DoctorRestartPolicy RestartPolicy { get; init; } = DoctorRestartPolicy.NotApplicable;
}
