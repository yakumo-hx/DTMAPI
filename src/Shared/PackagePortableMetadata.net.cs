using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;

namespace DTMAPI.Internal.Authoring
{
    // Desktop tools only. Runtime supplies its existing ECMA-335 reader, avoiding an extra game DLL.
    internal static class PackagePortableMetadata
    {
        public static PackageAssemblyRecord Inspect(byte[] bytes)
        {
            using (var stream = new MemoryStream(bytes, writable: false))
            using (var pe = new PEReader(stream))
            {
                if (!pe.HasMetadata) throw new InvalidDataException("dependency-pe-invalid: no CLR metadata.");
                MetadataReader reader = pe.GetMetadataReader();
                if (!reader.IsAssembly) throw new InvalidDataException("dependency-pe-invalid: not an assembly.");
                AssemblyDefinition definition = reader.GetAssemblyDefinition();
                string TypeName(TypeDefinitionHandle handle, int depth = 0)
                {
                    if (depth > 64) throw new InvalidDataException("dependency-pe-invalid: nested type depth.");
                    var type = reader.GetTypeDefinition(handle); string name = reader.GetString(type.Name);
                    var parent = type.GetDeclaringType(); string ns = reader.GetString(type.Namespace);
                    return parent.IsNil ? (ns.Length == 0 ? name : ns + "." + name) : TypeName(parent, depth + 1) + "+" + name;
                }
                string target = string.Empty;
                foreach (CustomAttributeHandle handle in definition.GetCustomAttributes())
                {
                    CustomAttribute attribute = reader.GetCustomAttribute(handle);
                    if (attribute.Constructor.Kind != HandleKind.MemberReference) continue;
                    EntityHandle parent = reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Parent;
                    if (parent.Kind != HandleKind.TypeReference) continue;
                    TypeReference type = reader.GetTypeReference((TypeReferenceHandle)parent);
                    if (reader.GetString(type.Namespace) != "System.Runtime.Versioning" || reader.GetString(type.Name) != "TargetFrameworkAttribute") continue;
                    if (target.Length != 0) throw new InvalidDataException("dependency-pe-invalid: duplicate target framework attribute.");
                    BlobReader blob = reader.GetBlobReader(attribute.Value);
                    if (blob.ReadUInt16() != 1) throw new InvalidDataException("dependency-pe-invalid: invalid attribute prolog.");
                    target = blob.ReadSerializedString() ?? string.Empty;
                }
                return new PackageAssemblyRecord
                {
                    Length = bytes.LongLength, Sha256 = PackageDependencyContract.ComputeHash(bytes), TargetFramework = target,
                    DefinedTypes = reader.TypeDefinitions.Select(h => TypeName(h)).ToArray(),
                    Identity = Identity(reader, definition.Name, definition.Version, definition.Culture, definition.PublicKey, true),
                    References = reader.AssemblyReferences.Select(handle =>
                    {
                        AssemblyReference reference = reader.GetAssemblyReference(handle);
                        return Identity(reader, reference.Name, reference.Version, reference.Culture, reference.PublicKeyOrToken, (reference.Flags & AssemblyFlags.PublicKey) != 0);
                    }).OrderBy(identity => identity.Key, StringComparer.Ordinal).ToArray()
                };
            }
        }

        private static PackageAssemblyIdentity Identity(MetadataReader reader, StringHandle name, Version version,
            StringHandle culture, BlobHandle key, bool fullKey)
        {
            byte[] bytes = key.IsNil ? Array.Empty<byte>() : reader.GetBlobBytes(key);
            if (bytes.Length != 0 && fullKey)
                using (SHA1 sha = SHA1.Create()) bytes = sha.ComputeHash(bytes).Reverse().Take(8).ToArray();
            if (bytes.Length != 0 && bytes.Length != 8) throw new InvalidDataException("dependency-pe-invalid: malformed public key token.");
            return new PackageAssemblyIdentity
            {
                Name = reader.GetString(name), AssemblyVersion = version.ToString(), Culture = reader.GetString(culture),
                PublicKeyToken = BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant()
            };
        }
    }
}
