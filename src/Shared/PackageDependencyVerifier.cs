using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DTMAPI.Internal.Authoring
{
    internal static class PackageDependencyVerifier
    {
        public static void Verify(string root, PackageDependencyInventory inventory,
            Func<byte[], PackageAssemblyRecord> inspect, Func<PackageAssemblyIdentity, bool> hostReference,
            ISet<string> reservedNames, IEnumerable<string> inventoriedPaths, NativePackageContract? native = null)
        {
            var files = new HashSet<string>(inventoriedPaths, StringComparer.OrdinalIgnoreCase);
            var pending = new Stack<string>();
            pending.Push(Path.GetFullPath(root));
            while (pending.Count > 0)
            {
                string directory = pending.Pop();
                foreach (string entry in Directory.EnumerateFileSystemEntries(directory))
                {
                    FileAttributes attributes = File.GetAttributes(entry);
                    if ((attributes & FileAttributes.ReparsePoint) != 0) throw new InvalidDataException("dependency-reparse-path: " + entry);
                    if ((attributes & FileAttributes.Directory) != 0) { pending.Push(entry); continue; }
                    string relative = entry.Substring(Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar).Length + 1).Replace('\\', '/');
                    // install-local owns this deployment metadata after package creation;
                    // it is not a package payload and never participates in CLR resolution.
                    if (relative == ".dtmapi-author-receipt.json")
                    {
                        using (FileStream receipt = File.OpenRead(entry))
                            if (receipt.Length >= 2 && receipt.ReadByte() == 'M' && receipt.ReadByte() == 'Z')
                                throw new InvalidDataException("dependency-undeclared-executable: " + relative);
                        continue;
                    }
                    if (relative != PackageDependencyBundle.MarkerPath && !files.Contains(relative)) throw new InvalidDataException("dependency-uninventoried-file: " + relative);
                }
            }
            var assemblies = inventory.Assemblies.ToDictionary(a => a.Identity.Name, StringComparer.OrdinalIgnoreCase);
            foreach (PackageAssemblyRecord record in inventory.Assemblies)
            {
                if (!files.Contains(record.Path)) throw new InvalidDataException("dependency-file-not-in-inventory: " + record.Path);
                if (reservedNames.Contains(record.Identity.Name)) throw new InvalidDataException("reserved-assembly-name: " + record.Identity.Name);
                byte[] bytes = File.ReadAllBytes(PackageDependencyContract.ResolveFile(root, record.Path));
                PackageAssemblyRecord actual = inspect(bytes);
                if (native?.SchemaVersion == 2)
                {
                    string identity = NativePackageContract.IdentityString(actual.Identity);
                    foreach (var node in native.RequiredMembers.SelectMany(m => NativeGenericSignature.NamedNodes(m.GenericUse!)))
                        if (NativeGenericSignature.Text(NativeGenericSignature.Get(node, "assemblyIdentity")) == identity
                            && !actual.DefinedTypes.Contains(NativeGenericSignature.Text(NativeGenericSignature.Get(node, "name")), StringComparer.Ordinal))
                            throw new InvalidDataException("native-type-argument-missing: " + record.Path + " / " + NativeGenericSignature.Text(NativeGenericSignature.Get(node, "name")));
                }
                if (record.Length != bytes.LongLength || record.Sha256 != PackageDependencyContract.ComputeHash(bytes)) throw new InvalidDataException("dependency-file-changed: " + record.Path);
                if (record.Identity.Key != actual.Identity.Key || record.TargetFramework != actual.TargetFramework ||
                    !record.References.Select(r => r.Key).OrderBy(k => k, StringComparer.Ordinal).SequenceEqual(actual.References.Select(r => r.Key).OrderBy(k => k, StringComparer.Ordinal)))
                    throw new InvalidDataException("dependency-pe-inventory-mismatch: " + record.Path);
                foreach (string license in record.LicenseFiles)
                {
                    if (!files.Contains(license)) throw new InvalidDataException("dependency-license-not-in-inventory: " + license);
                    if (new FileInfo(PackageDependencyContract.ResolveFile(root, license)).Length == 0) throw new InvalidDataException("dependency-license-empty: " + license);
                }
                foreach (PackageAssemblyIdentity reference in record.References)
                {
                    if (assemblies.TryGetValue(reference.Name, out PackageAssemblyRecord? library))
                    {
                        if (library.Identity.Key != reference.Key) throw new InvalidDataException("dependency-clr-reference-mismatch: " + record.Path + " -> " + reference.Name);
                    }
                    else if (!hostReference(reference)) throw new InvalidDataException("dependency-transitive-reference-missing: " + record.Path + " -> " + reference.Key);
                }
            }
            // MZ checks are independent of extension, and use the verified inventory as the allow-list.
            foreach (string path in files)
            {
                string absolute = PackageDependencyContract.ResolveFile(root, path);
                using (FileStream stream = File.OpenRead(absolute))
                {
                    if (stream.Length < 2 || stream.ReadByte() != 'M' || stream.ReadByte() != 'Z') continue;
                    if (!inventory.Assemblies.Any(record => string.Equals(record.Path, path, StringComparison.Ordinal)))
                        throw new InvalidDataException("dependency-undeclared-executable: " + path);
                }
            }
        }
    }
}
