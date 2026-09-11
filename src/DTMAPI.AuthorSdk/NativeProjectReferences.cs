using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using DTMAPI.Authoring.Contracts;
using DTMAPI.Internal.Authoring;
using DTMAPI.Tooling.Metadata;

namespace DTMAPI.AuthorSdk;

internal sealed record NativeProjectInput(string SourcePath, byte[] Bytes, NativeHostReference Reference);
internal sealed record NativeCompilerInputs(string[] ReferencePaths, NativeHostReference[] MetadataDependencies, string InputSha256);

internal static class NativeProjectReferences
{
    public static bool UsesContract(AuthorProjectContext context) => context.Manifest.NativeContractVersion is 1 or 2;

    public static void ValidateSettings(AuthorProjectContext context)
    {
        NativeAuthorReferences settings = context.AuthorProject.NativeReferences ?? throw new InvalidDataException("nativeReferences is required for NativeContractVersion=1.");
        if (context.AuthorProject.SchemaVersion is not (3 or 4) || context.AuthorProject.CodeModKind != "Advanced" || context.AuthorProject.Advanced != null)
            throw new InvalidDataException("Native V1 requires author schema 3, explicit Advanced and nativeReferences; old advanced policy inputs cannot be mixed.");
        if (!AuthorApiTargetCatalog.Current.GetAvailable(context.ApiTarget).Capabilities.Contains("native-contract/1", StringComparer.Ordinal))
            throw new InvalidDataException("The API target must support native-contract/1 (first internal target 0.6.4).");
        if (context.Manifest.NativeContractVersion == 2 && context.ApiTarget != "0.7.0") throw new InvalidDataException("Native V2 requires current API target 0.7.0 and the V2-capable Runtime candidate.");
        var root = PackageDependencyContract.Object(PackageDependencyContract.ReadJson(File.ReadAllBytes(context.AuthorProjectPath)));
        var fields = PackageDependencyContract.Object(PackageDependencyContract.Required(root, "nativeReferences"), "gameRoot", "references", "harmonyOwner", "requiredMembers");
        PackageDependencyContract.Text(PackageDependencyContract.Required(fields, "gameRoot"));
        PackageDependencyContract.Text(PackageDependencyContract.Required(fields, "harmonyOwner"));
        foreach (var member in PackageDependencyContract.Array(PackageDependencyContract.Required(fields, "requiredMembers")))
        { if (context.Manifest.NativeContractVersion == 2 && member.Element("definition") != null) NativePackageContract.ReadGenericMember(member); else NativePackageContract.ReadMember(member); }
        foreach (var path in PackageDependencyContract.Array(PackageDependencyContract.Required(fields, "references"))) NativePackageContract.HostPath(PackageDependencyContract.Text(path));
        NativePackageContract.RequireOwner(context.Manifest.UniqueID, settings.HarmonyOwner);
        if (!Path.IsPathFullyQualified(settings.GameRoot) || !Directory.Exists(settings.GameRoot)) throw new InvalidDataException("nativeReferences.gameRoot must explicitly name the installed local game directory.");
        if (settings.References.Count == 0 || settings.References.Distinct(StringComparer.OrdinalIgnoreCase).Count() != settings.References.Count) throw new InvalidDataException("Native references must be nonempty and unique.");
    }

    public static NativeProjectInput[] Resolve(AuthorProjectContext context, string requestedGameRoot = "")
    {
        ValidateSettings(context);
        var settings = context.AuthorProject.NativeReferences!;
        if (requestedGameRoot.Length > 0 && !Path.GetFullPath(requestedGameRoot).TrimEnd(Path.DirectorySeparatorChar).Equals(Path.GetFullPath(settings.GameRoot).TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("--game-root must match the explicit nativeReferences.gameRoot.");
        var result = settings.References.OrderBy(path => path, StringComparer.Ordinal).Select(relative =>
        {
            string path = PackageDependencyContract.ResolveFile(settings.GameRoot, NativePackageContract.HostPath(relative));
            byte[] bytes = File.ReadAllBytes(path);
            var metadata = PackagePortableMetadata.Inspect(bytes);
            string name = metadata.Identity.Name;
            if (name.StartsWith("DTMAPI.", StringComparison.OrdinalIgnoreCase) || name == "netstandard" || name == "mscorlib" || name == "System" || name.StartsWith("System.", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Native reference cannot replace a frozen SDK/platform reference: " + name);
            if (Path.GetFileName(relative) != name + ".dll") throw new InvalidDataException("Native reference filename must match its CLR identity: " + relative);
            return new NativeProjectInput(path, bytes, new NativeHostReference { GameRelativePath = relative, AssemblyIdentity = NativePackageContract.IdentityString(metadata.Identity), Length = bytes.LongLength, Sha256 = PathSafety.Sha256Bytes(bytes), Mode = "metadata-surface" });
        }).ToArray();
        if (result.Select(input => input.Reference.AssemblyIdentity.Split(',')[0]).Distinct(StringComparer.OrdinalIgnoreCase).Count() != result.Length) throw new InvalidDataException("Native reference simple names conflict.");
        return result;
    }

    public static NativeCompilerInputs CompilerPaths(AuthorProjectContext context, NativeProjectInput[] inputs)
    {
        var surfaces = inputs.Select(input => NativeReferenceSurface.CreateDetailed(input.Bytes, inputs.Select(item => Path.GetDirectoryName(item.SourcePath)!))).ToArray();
        var support = new SortedDictionary<string, NativeHostReference>(StringComparer.Ordinal);
        string gameRoot = Path.GetFullPath(context.AuthorProject.NativeReferences!.GameRoot);
        foreach (var dependency in surfaces.SelectMany(surface => surface.MetadataDependencies))
        {
            string relative = NativePackageContract.HostPath(Path.GetRelativePath(gameRoot, dependency.Path).Replace('\\', '/'));
            PackageDependencyContract.ResolveFile(gameRoot, relative);
            NativeProjectInput? declared = inputs.SingleOrDefault(input => input.Reference.GameRelativePath == relative);
            if (declared != null)
            { if (declared.Reference.Sha256 != dependency.Sha256) throw new InvalidDataException("native-metadata-input-changed: " + relative); continue; }
            if (support.TryGetValue(relative, out var existing) && existing.Sha256 != dependency.Sha256) throw new InvalidDataException("native-metadata-input-changed: " + relative);
            support[relative] = new NativeHostReference { GameRelativePath = relative, AssemblyIdentity = dependency.AssemblyIdentity, Length = dependency.Length, Sha256 = dependency.Sha256 };
        }
        string inputHash = NativePackageContract.InputHash(inputs.Select(input => input.Reference).Concat(support.Values));
        string cache = Path.Combine(context.RootPath, "obj", "dtmapi-native", NativeReferenceSurface.Generation.Replace('/', '_'), inputHash);
        string directory = Path.GetFullPath(context.RootPath);
        PathSafety.RejectReparsePoints(directory, Array.Empty<string>());
        foreach (string segment in Path.GetRelativePath(directory, cache).Split(Path.DirectorySeparatorChar))
        {
            directory = Path.Combine(directory, segment);
            if (Directory.Exists(directory) || File.Exists(directory)) PathSafety.RejectReparsePoints(context.RootPath, new[] { directory });
            else Directory.CreateDirectory(directory);
        }
        var paths = new List<string>();
        for (int index = 0; index < inputs.Length; index++)
        {
            var input = inputs[index];
            byte[] surface = surfaces[index].Bytes;
            input.Reference.SurfaceSha256 = PathSafety.Sha256Bytes(surface);
            string path = Path.Combine(cache, Path.GetFileName(input.SourcePath));
            if (File.Exists(path))
            {
                PathSafety.RejectReparsePoints(context.RootPath, new[] { path });
                if (PathSafety.Sha256File(path) != input.Reference.SurfaceSha256) throw new InvalidDataException("native-reference-cache-changed: " + path);
                NativeReferenceSurface.Verify(File.ReadAllBytes(path));
            }
            else File.WriteAllBytes(path, surface);
            paths.Add(path);
        }
        foreach (var input in inputs.Select(input => input.Reference).Concat(support.Values))
            if (PathSafety.Sha256File(PackageDependencyContract.ResolveFile(gameRoot, input.GameRelativePath)) != input.Sha256) throw new InvalidDataException("native-metadata-input-changed: " + input.GameRelativePath);
        return new NativeCompilerInputs(paths.ToArray(), support.Values.ToArray(), inputHash);
    }

    public static bool AllowsReference(NativeProjectInput[] inputs, PackageAssemblyIdentity identity) => inputs.Any(input => input.Reference.AssemblyIdentity == NativePackageContract.IdentityString(identity));

    public static JsonArray RequiredMembers(AuthorProjectContext context, IEnumerable<(byte[] Dll, byte[]? Pdb)> assemblies, NativeProjectInput[] inputs)
    {
        var members = new SortedDictionary<string, JsonNode>(StringComparer.Ordinal);
        bool v2 = context.Manifest.NativeContractVersion == 2;
        void AddV2(byte[] bytes)
        {
            var parsed = NativePackageContract.ReadGenericMember(NativeGenericSignature.Read(bytes));
            members[parsed.Key] = JsonNode.Parse(bytes)!;
        }
        foreach (var assembly in assemblies)
        {
            if (v2)
                foreach (var member in NativeGenericMetadata.Extract(assembly.Dll, inputs.Select(i => i.SourcePath).ToArray(), inputs.Select(i => Path.GetDirectoryName(i.SourcePath)!), assembly.Pdb)) AddV2(member);
            else
                foreach (var member in NativeMemberMetadata.Extract(assembly.Dll, inputs.Select(i => i.SourcePath).ToArray(), inputs.Select(i => Path.GetDirectoryName(i.SourcePath)!)))
                    members[member.Key] = JsonSerializer.SerializeToNode(new { member.AssemblyIdentity, member.DeclaringType, member.Kind, member.Name, member.IsStatic, member.ReturnType, member.ParameterTypes }, JsonSupport.Tool)!;
        }
        var author = PackageDependencyContract.Object(PackageDependencyContract.ReadJson(File.ReadAllBytes(context.AuthorProjectPath)));
        var settings = PackageDependencyContract.Object(PackageDependencyContract.Required(author, "nativeReferences"));
        foreach (var row in PackageDependencyContract.Array(PackageDependencyContract.Required(settings, "requiredMembers")))
        {
            if (v2 && row.Element("definition") != null)
            {
                var required = NativePackageContract.ReadGenericMember(row);
                var host = inputs.SingleOrDefault(i => i.Reference.AssemblyIdentity == required.AssemblyIdentity) ?? throw new InvalidDataException("native-undeclared-host: " + required.AssemblyIdentity);
                byte[] bytes = NativeGenericSignature.Write(row);
                if (!NativeGenericMetadata.Contains(host.Bytes, bytes)) throw new InvalidDataException("native-required-member-missing: " + required.Key);
                AddV2(bytes);
            }
            else
            {
                var declared = NativePackageContract.ReadMember(row);
                var member = new NativeMemberDescription { AssemblyIdentity = declared.AssemblyIdentity, DeclaringType = declared.DeclaringType, Kind = declared.Kind, Name = declared.Name, IsStatic = declared.IsStatic, ReturnType = declared.ReturnType, ParameterTypes = declared.ParameterTypes };
                var host = inputs.SingleOrDefault(i => i.Reference.AssemblyIdentity == member.AssemblyIdentity) ?? throw new InvalidDataException("native-undeclared-host: " + member.AssemblyIdentity);
                if (v2) AddV2(NativeGenericMetadata.Promote(host.Bytes, member));
                else
                {
                    if (!NativeMemberMetadata.Contains(host.Bytes, member)) throw new InvalidDataException("native-required-member-missing: " + member.Key);
                    members[member.Key] = JsonNode.Parse(NativeGenericSignature.Write(row))!;
                }
            }
        }
        return new JsonArray(members.Values.ToArray());
    }

    public static byte[] BuildProvenance(AuthorProjectContext context, SortedDictionary<string, byte[]> payload, byte[] manifest, string entryPath, byte[] dependencies, string requestedGameRoot, string compiledInputSha256, IReadOnlyDictionary<string, byte[]>? diagnosticSymbols = null)
    {
        NativeProjectInput[] inputs = Resolve(context, requestedGameRoot);
        NativeCompilerInputs compiler = CompilerPaths(context, inputs);
        if (compiler.InputSha256 != compiledInputSha256) throw new InvalidDataException("native-metadata-input-changed: Host inputs changed between compilation and package provenance.");
        var inventory = PackageDependencyContract.ReadInventory(dependencies);
        var members = RequiredMembers(context, inventory.Assemblies.Select(a => (payload[a.Path],
            diagnosticSymbols != null && diagnosticSymbols.TryGetValue(a.Path, out var symbols) ? symbols : payload.GetValueOrDefault(Path.ChangeExtension(a.Path, ".pdb")))), inputs);
        var document = new JsonObject
        {
            ["schemaVersion"] = context.Manifest.NativeContractVersion!.Value, ["ownerId"] = context.Manifest.UniqueID, ["apiTarget"] = context.ApiTarget, ["targetFramework"] = "netstandard2.0",
            ["manifestSha256"] = PathSafety.Sha256Bytes(manifest), ["entrySha256"] = PathSafety.Sha256Bytes(payload[entryPath]), ["dependencyInventorySha256"] = PathSafety.Sha256Bytes(dependencies),
            ["gameBuild"] = "assembly-sha256:" + (inputs.FirstOrDefault(input => Path.GetFileName(input.SourcePath) == "Assembly-CSharp.dll")?.Reference.Sha256 ?? "not-referenced"),
            ["harmonyOwner"] = context.AuthorProject.NativeReferences!.HarmonyOwner,
            ["references"] = new JsonArray(inputs.Select(input => (JsonNode)JsonSerializer.SerializeToNode(input.Reference, JsonSupport.Tool)!).ToArray()),
            ["requiredMembers"] = members,
            ["referenceGeneration"] = new JsonObject { ["toolVersion"] = typeof(NativeReferenceSurface).Assembly.GetName().Version!.ToString(3), ["mode"] = NativeReferenceSurface.Generation, ["inputSha256"] = compiler.InputSha256,
                ["metadataDependencies"] = new JsonArray(compiler.MetadataDependencies.Select(input => (JsonNode)new JsonObject { ["gameRelativePath"] = input.GameRelativePath, ["assemblyIdentity"] = input.AssemblyIdentity, ["length"] = input.Length, ["sha256"] = input.Sha256 }).ToArray()) }
        };
        byte[] output = Encoding.UTF8.GetBytes(document.ToJsonString(JsonSupport.StrictToolContract) + "\n");
        NativePackageContract.Read(output).RequireBinding(context.Manifest.UniqueID, context.ApiTarget, manifest, payload[entryPath], dependencies);
        return output;
    }
}
