namespace DTMAPI.InstallDoctor;

internal sealed class ForbiddenNativePayload
{
    public string Path { get; init; } = string.Empty;
    public string AssemblyName { get; init; } = string.Empty;
    public bool MatchedTrackedReferenceBytes { get; init; }
    public bool UnverifiedExecutable { get; init; }
    public bool Unreadable { get; init; }
    public string InspectionError { get; init; } = string.Empty;
}
