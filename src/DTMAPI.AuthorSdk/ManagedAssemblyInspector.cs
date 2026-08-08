using DTMAPI.Authoring.Contracts;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace DTMAPI.AuthorSdk;

internal static class ManagedAssemblyInspector
{
    private const string NetStandard20 = ".NETStandard,Version=v2.0";

    public static void ValidateCodeMod(
        string path,
        AuthorCodeModKind kind,
        IEnumerable<string> advancedReferenceNames,
        string expectedAssemblyName)
    {
        using FileStream stream = File.OpenRead(path);
        using var pe = new PEReader(stream, PEStreamOptions.LeaveOpen);
        if (!pe.HasMetadata)
            throw new InvalidDataException("CodeMod entry DLL has no managed metadata.");
        MetadataReader reader = pe.GetMetadataReader();
        string assemblyName = reader.GetString(reader.GetAssemblyDefinition().Name);
        if (IsNativeAssemblyName(assemblyName))
            throw new InvalidDataException("bundled-native-runtime-dependency: CodeMod EntryDll impersonates a native/runtime assembly: " + assemblyName + ".");
        if (!assemblyName.Equals(expectedAssemblyName, StringComparison.Ordinal))
            throw new InvalidDataException("CodeMod EntryDll internal AssemblyName must exactly match its declared filename/author assemblyName: expected " + expectedAssemblyName + "; observed " + assemblyName + ".");
        string targetFramework = ReadTargetFramework(reader);
        if (!targetFramework.Equals(NetStandard20, StringComparison.Ordinal))
            throw new InvalidDataException("CodeMod entry DLL TargetFramework must be exactly netstandard2.0; observed " + (targetFramework.Length == 0 ? "missing" : targetFramework) + ".");

        AssemblyReference[] assemblyReferences = reader.AssemblyReferences.Select(reader.GetAssemblyReference).ToArray();
        AssemblyReference? netstandard = assemblyReferences.FirstOrDefault(reference => reader.GetString(reference.Name).Equals("netstandard", StringComparison.Ordinal));
        if (netstandard is AssemblyReference netstandardReference && netstandardReference.Version > new Version(2, 0, 0, 0))
            throw new InvalidDataException("CodeMod entry DLL references netstandard " + netstandardReference.Version + "; the maximum admitted assembly reference is 2.0.0.0.");

        string[] references = assemblyReferences.Select(reference => reader.GetString(reference.Name)).ToArray();
        string[] native = references.Where(reference => !IsPlatformManagedReference(reference)).OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray();
        if (kind == AuthorCodeModKind.Strict)
        {
            if (native.Length > 0)
                throw new InvalidDataException("Strict CodeMod entry DLL contains forbidden native assembly references: " + string.Join(", ", native) + ".");
            return;
        }

        string[] allowed = advancedReferenceNames.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray();
        string[] actual = native.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray();
        if (!actual.SequenceEqual(allowed, StringComparer.OrdinalIgnoreCase))
            throw new InvalidDataException("Advanced CodeMod native AssemblyRefs must exactly match its tracked reference receipt: expected " + string.Join(", ", allowed) + "; observed " + string.Join(", ", actual) + ".");
    }

    public static void ValidateNoBundledNativePayloads(
        IEnumerable<KeyValuePair<string, byte[]>> payload,
        string admittedEntryPath)
    {
        AdvancedReferencePolicy[] policies = AdvancedReferenceAssets.LoadAllTrackedPolicies().Select(value => value.Policy).ToArray();
        foreach (KeyValuePair<string, byte[]> file in payload)
            ValidatePayloadBytes(file.Key, file.Value, policies, admittedEntryPath);
    }

    public static void ValidateNoBundledNativePayloads(string root, string admittedEntryPath)
    {
        AdvancedReferencePolicy[] policies = AdvancedReferenceAssets.LoadAllTrackedPolicies().Select(value => value.Policy).ToArray();
        foreach (string path in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
            ValidatePayloadBytes(Path.GetRelativePath(root, path).Replace('\\', '/'), File.ReadAllBytes(path), policies, admittedEntryPath);
    }

    private static void ValidatePayloadBytes(
        string displayPath,
        byte[] bytes,
        IReadOnlyList<AdvancedReferencePolicy> policies,
        string admittedEntryPath)
    {
        AdvancedReferencePolicyEntry? tracked = policies.SelectMany(policy => policy.References).FirstOrDefault(reference =>
            reference.Length == bytes.LongLength &&
            reference.Sha256.Equals(PathSafety.Sha256Bytes(bytes), StringComparison.OrdinalIgnoreCase));
        if (tracked != null)
        {
            throw new InvalidDataException(
                "bundled-native-runtime-dependency: Package contains tracked game/runtime bytes under an arbitrary payload name: " +
                displayPath + " (" + tracked.AssemblyName + ").");
        }
        if (bytes.Length < 2 || bytes[0] != (byte)'M' || bytes[1] != (byte)'Z')
            return;

        if (admittedEntryPath.Length > 0 && displayPath.Equals(admittedEntryPath, StringComparison.OrdinalIgnoreCase))
            return;

        try
        {
            using var stream = new MemoryStream(bytes, writable: false);
            using var pe = new PEReader(stream, PEStreamOptions.LeaveOpen);
            if (!pe.HasMetadata)
                throw new InvalidDataException("Executable payload has no verifiable managed metadata.");
            MetadataReader reader = pe.GetMetadataReader();
            string assemblyName = reader.GetString(reader.GetAssemblyDefinition().Name);
            throw new InvalidDataException(
                "bundled-native-runtime-dependency: Package contains an executable managed assembly outside its sole declared EntryDll: " +
                displayPath + " (" + assemblyName + ").");
        }
        catch (InvalidDataException ex) when (!ex.Message.StartsWith("bundled-native-runtime-dependency:", StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                "bundled-native-runtime-dependency: Package contains an MZ executable payload that cannot be verified as an admitted managed asset: " +
                displayPath + ".",
                ex);
        }
        catch (BadImageFormatException ex)
        {
            throw new InvalidDataException(
                "bundled-native-runtime-dependency: Package contains an MZ executable payload with invalid managed metadata: " + displayPath + ".",
                ex);
        }
    }

    private static bool IsPlatformManagedReference(string name) =>
        name.Equals("DTMAPI.Abstractions", StringComparison.OrdinalIgnoreCase)
        || name.Equals("netstandard", StringComparison.OrdinalIgnoreCase)
        || name.Equals("mscorlib", StringComparison.OrdinalIgnoreCase)
        || name.Equals("System", StringComparison.OrdinalIgnoreCase)
        || name.StartsWith("System.", StringComparison.OrdinalIgnoreCase)
        || name.Equals("Microsoft.CSharp", StringComparison.OrdinalIgnoreCase);

    private static bool IsNativeAssemblyName(string name) =>
        name.Equals("Assembly-CSharp", StringComparison.OrdinalIgnoreCase)
        || name.Equals("0Harmony", StringComparison.OrdinalIgnoreCase)
        || name.StartsWith("Harmony", StringComparison.OrdinalIgnoreCase)
        || name.StartsWith("BepInEx", StringComparison.OrdinalIgnoreCase)
        || name.StartsWith("UnityEngine", StringComparison.OrdinalIgnoreCase)
        || name.StartsWith("Unity.", StringComparison.OrdinalIgnoreCase);

    private static string ReadTargetFramework(MetadataReader reader)
    {
        foreach (CustomAttributeHandle handle in reader.GetAssemblyDefinition().GetCustomAttributes())
        {
            CustomAttribute attribute = reader.GetCustomAttribute(handle);
            if (!GetAttributeTypeName(reader, attribute.Constructor).Equals("System.Runtime.Versioning.TargetFrameworkAttribute", StringComparison.Ordinal))
                continue;
            BlobReader blob = reader.GetBlobReader(attribute.Value);
            if (blob.ReadUInt16() != 1)
                throw new InvalidDataException("CodeMod TargetFrameworkAttribute has an invalid custom-attribute prolog.");
            return blob.ReadSerializedString() ?? string.Empty;
        }
        return string.Empty;
    }

    private static string GetAttributeTypeName(MetadataReader reader, EntityHandle constructor)
    {
        EntityHandle type = constructor.Kind switch
        {
            HandleKind.MemberReference => reader.GetMemberReference((MemberReferenceHandle)constructor).Parent,
            HandleKind.MethodDefinition => reader.GetMethodDefinition((MethodDefinitionHandle)constructor).GetDeclaringType(),
            _ => default
        };
        if (type.IsNil)
            return string.Empty;
        return type.Kind switch
        {
            HandleKind.TypeReference => FullName(reader, reader.GetTypeReference((TypeReferenceHandle)type)),
            HandleKind.TypeDefinition => FullName(reader, reader.GetTypeDefinition((TypeDefinitionHandle)type)),
            _ => string.Empty
        };
    }

    private static string FullName(MetadataReader reader, TypeReference type)
    {
        string name = reader.GetString(type.Name);
        string ns = reader.GetString(type.Namespace);
        return ns.Length == 0 ? name : ns + "." + name;
    }

    private static string FullName(MetadataReader reader, TypeDefinition type)
    {
        string name = reader.GetString(type.Name);
        string ns = reader.GetString(type.Namespace);
        return ns.Length == 0 ? name : ns + "." + name;
    }
}
