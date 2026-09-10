using DTMAPI.Authoring.Contracts;
using DTMAPI.Internal.Authoring;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DTMAPI.AuthorSdk;

internal sealed record ManagedPackageInput(string SourcePath, PackageAssemblyRecord Record, IReadOnlyDictionary<string, string> Licenses);

internal static class ManagedPackageReferences
{
    public static bool UsesContract(AuthorProjectContext context) => context.AuthorProject.SchemaVersion == AuthorSdkContract.DependencyAuthorProjectSchemaVersion;

    public static IReadOnlyList<ManagedPackageInput> Resolve(AuthorProjectContext context)
    {
        var result = new List<ManagedPackageInput>();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (ManagedAuthorReference input in context.AuthorProject.ManagedReferences ?? new List<ManagedAuthorReference>())
        {
            if (!UsesContract(context)) throw new InvalidDataException("managedReferences requires author schema 3; explicitly migrate the manifest dependency contract.");
            if (input.Role is not ("shared-contract" or "private-managed")) throw new InvalidDataException("managedReferences role must be shared-contract or private-managed.");
            string path = Path.GetFullPath(input.Path, context.RootPath);
            if (!File.Exists(path)) throw new InvalidDataException("Managed reference missing: " + path);
            PathSafety.RejectReparsePoints(Path.GetDirectoryName(path)!, new[] { path });
            var record = PackagePortableMetadata.Inspect(File.ReadAllBytes(path));
            if (!names.Add(record.Identity.Name) || record.Identity.Name == context.AuthorProject.AssemblyName) throw new InvalidDataException("Managed reference name conflicts: " + record.Identity.Name);
            if (PackageHostReferences.IsReserved(record.Identity.Name)) throw new InvalidDataException("bundled-native-runtime-dependency: " + record.Identity.Name);
            if (record.TargetFramework != ".NETStandard,Version=v2.0" || Path.GetFileName(path) != record.Identity.Name + ".dll") throw new InvalidDataException("Managed reference must have its exact AssemblyName filename and target netstandard2.0: " + path);
            record.Path = (input.Role == "shared-contract" ? "lib/shared/" : "lib/private/") + Path.GetFileName(path);
            record.Role = input.Role; record.Distribution = input.Distribution; record.Source = "local-reference:" + Path.GetFileName(path);
            if (input.Distribution is not ("self-authored" or "licensed-third-party")) throw new InvalidDataException("Managed reference distribution is required: " + path);
            var licenses = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (string license in input.LicenseFiles)
            {
                string source = Path.GetFullPath(license, context.RootPath);
                PathSafety.RejectReparsePoints(Path.GetDirectoryName(source)!, new[] { source });
                if (!File.Exists(source) || new FileInfo(source).Length == 0) throw new InvalidDataException("Missing/empty license: " + source);
                string target = "licenses/" + record.Identity.Name + "/" + Path.GetFileName(source);
                if (!licenses.TryAdd(target, source)) throw new InvalidDataException("License basename collision: " + source);
            }
            if (input.Distribution == "licensed-third-party" && licenses.Count == 0) throw new InvalidDataException("Licensed third-party references require included licenseFiles: " + path);
            record.LicenseFiles = licenses.Keys.ToArray();
            result.Add(new ManagedPackageInput(path, record, licenses));
        }
        foreach (ManagedPackageInput library in LockedPackageRestore.ReadInputs(context).Concat(context.BuiltLibraries))
        {
            if (!names.Add(library.Record.Identity.Name)) throw new InvalidDataException("Project/managed reference name collision: " + library.Record.Identity.Name);
            result.Add(library);
        }
        var records = result.Select(input => input.Record).ToArray();
        if (context.AuthorProject.Build == null || context.GraphPrepared)
            RequireClosure(records, NativeProjectReferences.UsesContract(context) ? NativeProjectReferences.Resolve(context) : null);
        return result.OrderBy(input => input.Record.Path, StringComparer.Ordinal).ToArray();
    }

    public static void RequireClosure(IReadOnlyList<PackageAssemblyRecord> records, NativeProjectInput[]? native = null)
    {
        foreach (PackageAssemblyRecord record in records)
            foreach (PackageAssemblyIdentity reference in record.References)
            {
                PackageAssemblyRecord? provider = records.SingleOrDefault(item => item.Identity.Name.Equals(reference.Name, StringComparison.OrdinalIgnoreCase));
                if (provider != null ? provider.Identity.Key != reference.Key : !PackageHostReferences.IsStrictHostReference(reference) && !(native != null && NativeProjectReferences.AllowsReference(native, reference)))
                    throw new InvalidDataException("dependency-transitive-reference-missing-or-forbidden: " + record.Identity.Name + " -> " + reference.Key);
            }
    }

    public static void ValidateCompiledEntry(AuthorProjectContext context, string dll)
    {
        PackageAssemblyRecord entry = PackagePortableMetadata.Inspect(File.ReadAllBytes(dll));
        if (entry.Identity.Name != context.AuthorProject.AssemblyName || entry.TargetFramework != ".NETStandard,Version=v2.0" || PackageHostReferences.IsReserved(entry.Identity.Name))
            throw new InvalidDataException("Compiled entry identity/framework is not admitted.");
        RequireClosure(Resolve(context).Select(input => input.Record).Append(entry).ToArray(), NativeProjectReferences.UsesContract(context) ? NativeProjectReferences.Resolve(context) : null);
    }

    public static byte[] AddInventory(AuthorProjectContext context, SortedDictionary<string, byte[]> payload, byte[] manifest, string entryPath, bool includeSymbols = false)
    {
        var records = new List<PackageAssemblyRecord>();
        if (entryPath.Length > 0)
        {
            var entry = PackagePortableMetadata.Inspect(payload[entryPath]);
            entry.Path = entryPath; entry.Role = "entry"; entry.Distribution = "self-authored"; entry.Source = "author-source";
            records.Add(entry);
        }
        foreach (ManagedPackageInput input in Resolve(context))
        {
            byte[] bytes = File.ReadAllBytes(input.SourcePath);
            if (PackageDependencyContract.ComputeHash(bytes) != input.Record.Sha256) throw new InvalidDataException("Managed input changed during pack: " + input.SourcePath);
            payload.Add(input.Record.Path, bytes);
            foreach (string extension in includeSymbols ? new[] { ".xml", ".pdb" } : new[] { ".xml" })
            {
                string companion = Path.ChangeExtension(input.SourcePath, extension);
                if (File.Exists(companion)) payload.Add(Path.ChangeExtension(input.Record.Path, extension).Replace('\\', '/'), File.ReadAllBytes(companion));
            }
            foreach (var license in input.Licenses)
            {
                byte[] licenseBytes = File.ReadAllBytes(license.Value);
                if (payload.TryGetValue(license.Key, out byte[]? previous))
                {
                    if (!previous.SequenceEqual(licenseBytes)) throw new InvalidDataException("Conflicting package license path: " + license.Key);
                }
                else payload.Add(license.Key, licenseBytes);
            }
            records.Add(input.Record);
        }
        RequireClosure(records, NativeProjectReferences.UsesContract(context) ? NativeProjectReferences.Resolve(context) : null);
        foreach (var file in payload)
            if (file.Value.Length >= 2 && file.Value[0] == 'M' && file.Value[1] == 'Z' && !records.Any(record => record.Path == file.Key))
                throw new InvalidDataException("dependency-undeclared-executable: " + file.Key);
        JsonObject Identity(PackageAssemblyIdentity identity) => new()
        { ["name"] = identity.Name, ["assemblyVersion"] = identity.AssemblyVersion, ["culture"] = identity.Culture, ["publicKeyToken"] = identity.PublicKeyToken };
        JsonObject Assembly(PackageAssemblyRecord record)
        {
            var node = Identity(record.Identity);
            node["path"] = record.Path; node["role"] = record.Role; node["length"] = record.Length; node["sha256"] = record.Sha256;
            node["targetFramework"] = record.TargetFramework; node["distribution"] = record.Distribution; node["source"] = record.Source;
            node["licenseFiles"] = JsonSerializer.SerializeToNode(record.LicenseFiles);
            node["references"] = new JsonArray(record.References.OrderBy(reference => reference.Key, StringComparer.Ordinal).Select(reference => (JsonNode)Identity(reference)).ToArray());
            return node;
        }
        var dependency = PackageDependencyContract.ReadManifest(manifest) ?? throw new InvalidDataException("Schema 3 must declare DependencyContractVersion=1.");
        var document = new JsonObject
        {
            ["schemaVersion"] = 1, ["ownerId"] = context.Manifest.UniqueID, ["apiTarget"] = context.ApiTarget, ["entryPath"] = entryPath,
            ["dependencies"] = new JsonArray(dependency.Dependencies.OrderBy(d => d.Id, StringComparer.OrdinalIgnoreCase).Select(d =>
            {
                var range = new JsonObject { ["minimumInclusive"] = d.Range.Minimum.Text, ["includePrerelease"] = d.Range.IncludePrerelease };
                if (d.Range.Maximum != null) range["maximumExclusive"] = d.Range.Maximum.Text;
                return (JsonNode)new JsonObject { ["uniqueId"] = d.Id, ["required"] = d.Required, ["versionRange"] = range };
            }).ToArray()),
            ["assemblies"] = new JsonArray(records.OrderBy(record => record.Path, StringComparer.Ordinal).Select(record => (JsonNode)Assembly(record)).ToArray())
        };
        byte[] output = Encoding.UTF8.GetBytes(document.ToJsonString(JsonSupport.StrictToolContract) + "\n");
        PackageDependencyContract.RequireProjection(dependency, PackageDependencyContract.ReadInventory(output));
        payload.Add(PackageDependencyInventory.FileName, output);
        return output;
    }
}
