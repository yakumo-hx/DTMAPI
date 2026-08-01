using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;

namespace DTMAPI.Tooling.Metadata;

/// <summary>
/// Reads ECMA-335 metadata without loading the inspected assembly into the CLR.
/// </summary>
public sealed class PeMetadataInspector
{
    public PeMetadataInspection Inspect(string path)
    {
        string fullPath = System.IO.Path.GetFullPath(path);
        string hash = string.Empty;
        try
        {
            using (FileStream hashStream = ReadOnlyFiles.OpenSharedRead(fullPath))
                hash = Convert.ToHexString(SHA256.HashData(hashStream));

            using FileStream stream = ReadOnlyFiles.OpenSharedRead(fullPath);
            using var pe = new PEReader(stream, PEStreamOptions.LeaveOpen);
            if (!pe.HasMetadata)
            {
                return new PeMetadataInspection
                {
                    Path = fullPath,
                    Kind = PortableBinaryKind.NativePortableExecutable,
                    Sha256 = hash
                };
            }

            MetadataReader reader = pe.GetMetadataReader();
            return InspectManaged(fullPath, hash, reader);
        }
        catch (Exception ex) when (ex is BadImageFormatException or IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            return new PeMetadataInspection
            {
                Path = fullPath,
                Kind = PortableBinaryKind.DamagedOrUnknown,
                Sha256 = hash,
                Error = ex.GetType().Name + ": " + ex.Message
            };
        }
    }

    private static PeMetadataInspection InspectManaged(string path, string hash, MetadataReader reader)
    {
        string assemblyName = string.Empty;
        Version? version = null;
        if (reader.IsAssembly)
        {
            AssemblyDefinition assembly = reader.GetAssemblyDefinition();
            assemblyName = reader.GetString(assembly.Name);
            version = assembly.Version;
        }

        string[] assemblyReferences = reader.AssemblyReferences
            .Select(handle => reader.GetString(reader.GetAssemblyReference(handle).Name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var definedTypes = new List<string>();
        var referencedTypes = new HashSet<string>(StringComparer.Ordinal);
        var referencedMembers = new HashSet<string>(StringComparer.Ordinal);
        var obsolete = new List<ObsoleteDeclaration>();
        bool dtmModSubclass = false;
        bool bepInExPlugin = false;
        string targetFramework = string.Empty;

        foreach (TypeReferenceHandle handle in reader.TypeReferences)
        {
            string name = GetTypeReferenceFullName(reader, handle);
            if (name.Length > 0)
                referencedTypes.Add(name);
        }

        foreach (MemberReferenceHandle handle in reader.MemberReferences)
        {
            MemberReference member = reader.GetMemberReference(handle);
            string parent = GetEntityTypeFullName(reader, member.Parent);
            string name = reader.GetString(member.Name);
            if (parent.Length > 0 && name.Length > 0)
                referencedMembers.Add(parent + "::" + name);
        }

        foreach (TypeDefinitionHandle handle in reader.TypeDefinitions)
        {
            TypeDefinition type = reader.GetTypeDefinition(handle);
            string fullName = GetTypeDefinitionFullName(reader, handle);
            if (fullName.Length > 0 && fullName != "<Module>")
                definedTypes.Add(fullName);

            string baseType = GetEntityTypeFullName(reader, type.BaseType);
            if (baseType.Equals("DTMAPI.Abstractions.DtmMod", StringComparison.Ordinal))
                dtmModSubclass = true;
            if (baseType.Equals("BepInEx.BaseUnityPlugin", StringComparison.Ordinal))
                bepInExPlugin = true;

            foreach (CustomAttributeHandle attributeHandle in type.GetCustomAttributes())
            {
                string attributeType = GetAttributeTypeFullName(reader, attributeHandle);
                if (attributeType.Equals("BepInEx.BepInPlugin", StringComparison.Ordinal) ||
                    attributeType.Equals("BepInEx.BepInPluginAttribute", StringComparison.Ordinal))
                    bepInExPlugin = true;
                if (attributeType.Equals("System.ObsoleteAttribute", StringComparison.Ordinal))
                    obsolete.Add(ReadObsolete(reader, attributeHandle, "Type", fullName, string.Empty));
            }

            foreach (MethodDefinitionHandle methodHandle in type.GetMethods())
            {
                MethodDefinition method = reader.GetMethodDefinition(methodHandle);
                AddObsoleteDeclarations(reader, method.GetCustomAttributes(), "Method", fullName, reader.GetString(method.Name), obsolete);
            }

            foreach (PropertyDefinitionHandle propertyHandle in type.GetProperties())
            {
                PropertyDefinition property = reader.GetPropertyDefinition(propertyHandle);
                AddObsoleteDeclarations(reader, property.GetCustomAttributes(), "Property", fullName, reader.GetString(property.Name), obsolete);
            }

            foreach (FieldDefinitionHandle fieldHandle in type.GetFields())
            {
                FieldDefinition field = reader.GetFieldDefinition(fieldHandle);
                AddObsoleteDeclarations(reader, field.GetCustomAttributes(), "Field", fullName, reader.GetString(field.Name), obsolete);
            }
        }

        if (reader.IsAssembly)
        {
            AssemblyDefinition assembly = reader.GetAssemblyDefinition();
            foreach (CustomAttributeHandle handle in assembly.GetCustomAttributes())
            {
                if (!GetAttributeTypeFullName(reader, handle).Equals("System.Runtime.Versioning.TargetFrameworkAttribute", StringComparison.Ordinal))
                    continue;
                targetFramework = ReadFirstStringFixedArgument(reader, handle);
                break;
            }
        }

        return new PeMetadataInspection
        {
            Path = path,
            Kind = PortableBinaryKind.ManagedAssembly,
            Sha256 = hash,
            AssemblyName = assemblyName,
            AssemblyVersion = version,
            TargetFramework = targetFramework,
            AssemblyReferences = assemblyReferences,
            DefinedTypes = definedTypes.OrderBy(value => value, StringComparer.Ordinal).ToArray(),
            ReferencedTypes = referencedTypes.OrderBy(value => value, StringComparer.Ordinal).ToArray(),
            ReferencedMembers = referencedMembers.OrderBy(value => value, StringComparer.Ordinal).ToArray(),
            ObsoleteDeclarations = obsolete.OrderBy(value => value.DisplayName, StringComparer.Ordinal).ToArray(),
            DefinesDtmModSubclass = dtmModSubclass,
            DefinesBepInExPlugin = bepInExPlugin
        };
    }

    private static void AddObsoleteDeclarations(
        MetadataReader reader,
        CustomAttributeHandleCollection attributes,
        string symbolKind,
        string declaringType,
        string symbolName,
        ICollection<ObsoleteDeclaration> output)
    {
        foreach (CustomAttributeHandle handle in attributes)
        {
            if (GetAttributeTypeFullName(reader, handle).Equals("System.ObsoleteAttribute", StringComparison.Ordinal))
                output.Add(ReadObsolete(reader, handle, symbolKind, declaringType, symbolName));
        }
    }

    private static ObsoleteDeclaration ReadObsolete(
        MetadataReader reader,
        CustomAttributeHandle handle,
        string symbolKind,
        string declaringType,
        string symbolName)
    {
        string message = string.Empty;
        bool isError = false;
        try
        {
            BlobReader blob = reader.GetBlobReader(reader.GetCustomAttribute(handle).Value);
            if (blob.ReadUInt16() == 1 && blob.RemainingBytes > 0)
            {
                message = blob.ReadSerializedString() ?? string.Empty;
                if (blob.RemainingBytes >= 1)
                    isError = blob.ReadByte() != 0;
            }
        }
        catch (BadImageFormatException)
        {
            // Keep the declaration visible even when its optional message blob is malformed.
        }

        return new ObsoleteDeclaration
        {
            SymbolKind = symbolKind,
            DeclaringType = declaringType,
            SymbolName = symbolName,
            Message = message,
            IsError = isError
        };
    }

    private static string ReadFirstStringFixedArgument(MetadataReader reader, CustomAttributeHandle handle)
    {
        try
        {
            BlobReader blob = reader.GetBlobReader(reader.GetCustomAttribute(handle).Value);
            return blob.ReadUInt16() == 1 ? blob.ReadSerializedString() ?? string.Empty : string.Empty;
        }
        catch (BadImageFormatException)
        {
            return string.Empty;
        }
    }

    private static string GetAttributeTypeFullName(MetadataReader reader, CustomAttributeHandle handle)
    {
        CustomAttribute attribute = reader.GetCustomAttribute(handle);
        return attribute.Constructor.Kind switch
        {
            HandleKind.MemberReference => GetEntityTypeFullName(reader, reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Parent),
            HandleKind.MethodDefinition => GetTypeDefinitionFullName(reader, reader.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor).GetDeclaringType()),
            _ => string.Empty
        };
    }

    private static string GetEntityTypeFullName(MetadataReader reader, EntityHandle handle)
    {
        if (handle.IsNil)
            return string.Empty;
        return handle.Kind switch
        {
            HandleKind.TypeReference => GetTypeReferenceFullName(reader, (TypeReferenceHandle)handle),
            HandleKind.TypeDefinition => GetTypeDefinitionFullName(reader, (TypeDefinitionHandle)handle),
            _ => string.Empty
        };
    }

    private static string GetTypeReferenceFullName(MetadataReader reader, TypeReferenceHandle handle)
    {
        TypeReference type = reader.GetTypeReference(handle);
        string name = reader.GetString(type.Name);
        string ns = reader.GetString(type.Namespace);
        if (type.ResolutionScope.Kind == HandleKind.TypeReference)
        {
            string parent = GetTypeReferenceFullName(reader, (TypeReferenceHandle)type.ResolutionScope);
            return parent.Length == 0 ? name : parent + "+" + name;
        }
        return ns.Length == 0 ? name : ns + "." + name;
    }

    private static string GetTypeDefinitionFullName(MetadataReader reader, TypeDefinitionHandle handle)
    {
        TypeDefinition type = reader.GetTypeDefinition(handle);
        string name = reader.GetString(type.Name);
        string ns = reader.GetString(type.Namespace);
        TypeDefinitionHandle parent = type.GetDeclaringType();
        if (!parent.IsNil)
        {
            string parentName = GetTypeDefinitionFullName(reader, parent);
            return parentName.Length == 0 ? name : parentName + "+" + name;
        }
        return ns.Length == 0 ? name : ns + "." + name;
    }
}
