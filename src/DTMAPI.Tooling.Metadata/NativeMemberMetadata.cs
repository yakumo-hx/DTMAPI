using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using DTMAPI.Internal.Authoring;

namespace DTMAPI.Tooling.Metadata;

public sealed class NativeMemberDescription
{
    public string AssemblyIdentity { get; set; } = "";
    public string DeclaringType { get; set; } = "";
    public string Kind { get; set; } = "";
    public string Name { get; set; } = "";
    public bool IsStatic { get; set; }
    public string ReturnType { get; set; } = "";
    public string[] ParameterTypes { get; set; } = Array.Empty<string>();
    public string Key => NativeSignature.MemberKey(AssemblyIdentity, DeclaringType, Kind, Name, IsStatic, ReturnType, ParameterTypes);
}

/// <summary>Reads only metadata from author output and installed host files; never loads their code.</summary>
public static class NativeMemberMetadata
{
    public static IReadOnlyList<NativeMemberDescription> Extract(byte[] authorAssembly, IReadOnlyList<string> hostFiles, IEnumerable<string> resolverDirectories)
    {
        using var resolver = new DefaultAssemblyResolver();
        foreach (string directory in resolverDirectories.Concat(hostFiles.Select(path => Path.GetDirectoryName(path)!)).Distinct(StringComparer.OrdinalIgnoreCase))
            resolver.AddSearchDirectory(directory);
        var hosts = new Dictionary<string, AssemblyDefinition>(StringComparer.Ordinal);
        try
        {
            foreach (string path in hostFiles)
            {
                var host = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { AssemblyResolver = resolver, InMemory = true });
                if (!hosts.TryAdd(host.Name.FullName, host)) { host.Dispose(); throw new InvalidDataException("native-reference-duplicate: " + path); }
            }
            using var stream = new MemoryStream(authorAssembly, writable: false);
            using var author = AssemblyDefinition.ReadAssembly(stream, new ReaderParameters { AssemblyResolver = resolver, InMemory = true });
            var result = new SortedDictionary<string, NativeMemberDescription>(StringComparer.Ordinal);
            void Add(NativeMemberDescription member) => result[member.Key] = member;
            foreach (TypeReference reference in author.MainModule.GetTypeReferences())
            {
                if (!hosts.TryGetValue(Scope(reference), out var host)) continue;
                TypeDefinition type = FindType(host, reference.FullName);
                Add(Describe(type));
            }
            foreach (MemberReference reference in author.MainModule.GetMemberReferences())
            {
                if (!hosts.TryGetValue(Scope(reference.DeclaringType), out var host)) continue;
                TypeDefinition type = FindType(host, reference.DeclaringType.FullName);
                if (reference is MethodReference method)
                {
                    if (method.HasGenericParameters || method is GenericInstanceMethod || method.DeclaringType is TypeSpecification) throw NativeSignature.Unsupported(method.FullName);
                    string returns = TypeName(method.ReturnType);
                    string[] parameters = method.Parameters.Select(parameter => TypeName(parameter.ParameterType)).ToArray();
                    MethodDefinition[] matches = type.Methods.Where(candidate => candidate.Name == method.Name && candidate.IsStatic == !method.HasThis && Matches(candidate, returns, parameters)).ToArray();
                    if (matches.Length != 1) throw new InvalidDataException("native-member-unresolved: " + method.FullName);
                    Add(Describe(matches[0]));
                }
                else if (reference is FieldReference field)
                {
                    FieldDefinition[] matches = type.Fields.Where(candidate => candidate.Name == field.Name && TypeName(candidate.FieldType) == TypeName(field.FieldType)).ToArray();
                    if (matches.Length != 1) throw new InvalidDataException("native-member-unresolved: " + field.FullName);
                    Add(Describe(matches[0]));
                }
                else throw NativeSignature.Unsupported(reference.FullName);
            }
            return result.Values.ToArray();
        }
        finally { foreach (var host in hosts.Values) host.Dispose(); }
    }

    public static bool Contains(byte[] hostBytes, NativeMemberDescription required)
    {
        using var stream = new MemoryStream(hostBytes, writable: false);
        using var host = AssemblyDefinition.ReadAssembly(stream);
        TypeDefinition? type = NativeReferenceSurface.Types(host.MainModule.Types).SingleOrDefault(candidate => candidate.FullName.Replace('/', '+') == required.DeclaringType);
        if (type == null || host.Name.FullName != required.AssemblyIdentity) return false;
        if (required.Kind == "type") return Describe(type).Key == required.Key;
        if (required.Kind == "field") return type.Fields.Any(field => SafeKey(() => Describe(field)) == required.Key);
        if (required.Kind == "method") return type.Methods.Any(method => SafeKey(() => Describe(method)) == required.Key);
        return false;
    }

    private static string SafeKey(Func<NativeMemberDescription> read)
    { try { return read().Key; } catch (InvalidDataException ex) when (ex.Message.StartsWith("native-signature-unsupported:", StringComparison.Ordinal)) { return ""; } }

    private static bool Matches(MethodDefinition method, string returns, string[] parameters)
    { try { return !method.HasGenericParameters && TypeName(method.ReturnType) == returns && method.Parameters.Select(p => TypeName(p.ParameterType)).SequenceEqual(parameters); } catch (InvalidDataException) { return false; } }

    private static TypeDefinition FindType(AssemblyDefinition host, string name)
        => NativeReferenceSurface.Types(host.MainModule.Types).SingleOrDefault(type => type.FullName == name)
            ?? throw new InvalidDataException("native-type-unresolved: " + host.Name.FullName + " / " + name);

    private static string Scope(TypeReference type)
    {
        while (type is TypeSpecification specification) type = specification.ElementType;
        while (type.DeclaringType != null) type = type.DeclaringType;
        return type.Scope is AssemblyNameReference assembly ? assembly.FullName : "";
    }

    private static NativeMemberDescription Base(TypeDefinition type, string kind, string name, bool isStatic)
    {
        if (type.HasGenericParameters) throw NativeSignature.Unsupported(type.FullName);
        return new NativeMemberDescription { AssemblyIdentity = type.Module.Assembly.Name.FullName, DeclaringType = type.FullName.Replace('/', '+'), Kind = kind, Name = name, IsStatic = isStatic };
    }
    private static NativeMemberDescription Describe(TypeDefinition type) => Base(type, "type", "", false);
    private static NativeMemberDescription Describe(FieldDefinition field)
    { var result = Base(field.DeclaringType, "field", field.Name, field.IsStatic); result.ReturnType = TypeName(field.FieldType); return result; }
    private static NativeMemberDescription Describe(MethodDefinition method)
    {
        if (method.HasGenericParameters || method.CallingConvention == MethodCallingConvention.VarArg) throw NativeSignature.Unsupported(method.FullName);
        var result = Base(method.DeclaringType, "method", method.Name, method.IsStatic);
        result.ReturnType = TypeName(method.ReturnType); result.ParameterTypes = method.Parameters.Select(parameter => TypeName(parameter.ParameterType)).ToArray();
        return result;
    }
    private static string TypeName(TypeReference type)
    {
        if (type is ByReferenceType byRef) return TypeName(byRef.ElementType) + "&";
        if (type is PointerType pointer) return TypeName(pointer.ElementType) + "*";
        if (type is ArrayType array)
        {
            if (!array.IsVector && (array.Rank == 1 || array.Dimensions.Any(dimension => dimension.UpperBound.HasValue || (dimension.LowerBound.HasValue && dimension.LowerBound.Value != 0)))) throw NativeSignature.Unsupported(array.FullName);
            return TypeName(array.ElementType) + "[" + new string(',', array.Rank - 1) + "]";
        }
        if (type is TypeSpecification || type.IsGenericParameter || type.HasGenericParameters) throw NativeSignature.Unsupported(type.FullName);
        AssemblyNameReference assembly = type.Scope is AssemblyNameReference reference ? reference : type.Module.Assembly.Name;
        string token = BitConverter.ToString(assembly.PublicKeyToken ?? Array.Empty<byte>()).Replace("-", "").ToLowerInvariant();
        return NativeSignature.NamedType(type.FullName, assembly.Name, token, assembly.FullName);
    }
}
