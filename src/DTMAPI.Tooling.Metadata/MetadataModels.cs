using System;
using System.Collections.Generic;

namespace DTMAPI.Tooling.Metadata;

public enum PortableBinaryKind
{
    ManagedAssembly,
    NativePortableExecutable,
    DamagedOrUnknown
}

public sealed class ObsoleteDeclaration
{
    public string SymbolKind { get; init; } = string.Empty;
    public string DeclaringType { get; init; } = string.Empty;
    public string SymbolName { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public bool IsError { get; init; }

    public string DisplayName => SymbolKind.Equals("Type", StringComparison.Ordinal)
        ? DeclaringType
        : DeclaringType + "::" + SymbolName;
}

public sealed class PeMetadataInspection
{
    public string Path { get; init; } = string.Empty;
    public PortableBinaryKind Kind { get; init; }
    public string Sha256 { get; init; } = string.Empty;
    public string AssemblyName { get; init; } = string.Empty;
    public Version? AssemblyVersion { get; init; }
    public string TargetFramework { get; init; } = string.Empty;
    public string Error { get; init; } = string.Empty;
    public IReadOnlyList<string> AssemblyReferences { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> DefinedTypes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> ReferencedTypes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> ReferencedMembers { get; init; } = Array.Empty<string>();
    public IReadOnlyList<ObsoleteDeclaration> ObsoleteDeclarations { get; init; } = Array.Empty<ObsoleteDeclaration>();
    public bool DefinesDtmModSubclass { get; init; }
    public bool DefinesBepInExPlugin { get; init; }

    public bool IsManaged => Kind == PortableBinaryKind.ManagedAssembly;
}

public sealed class TreeHashSnapshot
{
    public string RootPath { get; init; } = string.Empty;
    public string Sha256 { get; init; } = string.Empty;
    public int FileCount { get; init; }
    public long TotalBytes { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
}

public sealed class FileIdentitySnapshot
{
    public string Path { get; init; } = string.Empty;
    public long Length { get; init; }
    public string Sha256 { get; init; } = string.Empty;
    public string Error { get; init; } = string.Empty;

    public bool IsComplete => Error.Length == 0 && Length >= 0 && Sha256.Length == 64;
}
