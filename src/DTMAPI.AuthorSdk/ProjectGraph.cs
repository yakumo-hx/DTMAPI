using DTMAPI.Authoring.Contracts;
using DTMAPI.Internal.Authoring;
using Microsoft.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace DTMAPI.AuthorSdk;

// This is a bounded SDK grammar, not an MSBuild evaluator. Load the entire DAG
// before compilation and carry its products in the current build context.
internal static class ProjectGraph
{
    private static readonly JsonSerializerOptions Strict = new(JsonSupport.Tool)
    { PropertyNameCaseInsensitive = false, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };

    internal static void ValidateBuildJson(string path, AuthorBuildSettings? settings)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        foreach (var property in document.RootElement.EnumerateObject())
            if (property.Name.Equals("build", StringComparison.OrdinalIgnoreCase))
            {
                if (property.Name != "build") throw new InvalidDataException("Use exact build spelling.");
                RequireUnique(property.Value);
                _ = JsonSerializer.Deserialize<AuthorBuildSettings>(property.Value.GetRawText(), Strict)
                    ?? throw new InvalidDataException("build must be an object.");
            }
    }

    private static void RequireUnique(JsonElement node)
    {
        if (node.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in node.EnumerateObject())
            {
                if (!names.Add(property.Name)) throw new InvalidDataException("Duplicate project input: " + property.Name);
                RequireUnique(property.Value);
            }
        }
        else if (node.ValueKind == JsonValueKind.Array) foreach (var child in node.EnumerateArray()) RequireUnique(child);
    }

    internal static AuthorProjectContext LoadLibrary(string root)
    {
        if (File.Exists(Path.Combine(root, "manifest.json")) || File.Exists(Path.Combine(root, "dtmapi.author.json")))
            throw new InvalidDataException("A library cannot also be a Mod project.");
        string path = Path.Combine(root, "dtmapi.library.json");
        PathSafety.RejectReparsePoints(root, Directory.EnumerateFileSystemEntries(root, "*", SearchOption.AllDirectories));
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        RequireUnique(document.RootElement);
        var library = JsonSerializer.Deserialize<AuthorLibraryProject>(document.RootElement.GetRawText(), Strict)
            ?? throw new InvalidDataException("Invalid library descriptor.");
        if (library.SchemaVersion != 1 || library.TargetFramework != "netstandard2.0" || library.Role is not ("shared-contract" or "private-managed"))
            throw new InvalidDataException("Library requires schema 1, netstandard2.0 and a managed package role.");
        var target = AuthorApiTargetCatalog.Current.GetAvailable(library.ApiTarget);
        if (!target.Capabilities.Contains("package-dependencies/1", StringComparer.Ordinal)) throw new InvalidDataException("Library target must support package-dependencies/1.");
        var manifest = new RuntimeManifest { Name = library.AssemblyName, Author = "Library author", Version = library.Version,
            Description = "Self-authored managed library", UniqueID = library.AssemblyName, Type = "CodeMod", CodeModKind = "Strict",
            EntryDll = library.AssemblyName + ".dll", EntryType = library.AssemblyName + ".Library", MinimumDTMApiVersion = target.MinimumRuntimeVersion, DependencyContractVersion = 1 };
        return new AuthorProjectContext { RootPath = root, AuthorProjectPath = path, ManifestPath = path, Manifest = manifest,
            ManifestJson = JsonSerializer.SerializeToNode(manifest, JsonSupport.RuntimeManifest)!.AsObject(),
            AuthorProject = new AuthorProject { SchemaVersion = 3, ProjectKind = "CodeMod", CodeModKind = "Strict", TargetDtmApiVersion = library.ApiTarget,
                AssemblyName = library.AssemblyName, SourceDirectory = library.SourceDirectory, Build = library.Build, ManagedReferences = library.ManagedReferences },
            SourcePath = PathSafety.ResolveUnderRoot(root, library.SourceDirectory, "sourceDirectory"), ContentPath = Path.Combine(root, "content"),
            Kind = AuthorProjectKind.CodeMod, CodeModKind = AuthorCodeModKind.Strict, ManifestDeclaresCodeModKind = true, IsLibrary = true, LibraryRole = library.Role };
    }

    internal static void ValidateLocal(AuthorProjectContext context)
    {
        if (context.AuthorProject.Build is not { } build) return;
        if (!ManagedPackageReferences.UsesContract(context) || context.Kind != AuthorProjectKind.CodeMod)
            throw new InvalidDataException("build inputs require a schema 3 CodeMod or library.");
        if (build.ProjectReferences == null || build.GeneratedSourceFiles == null || build.EmbeddedResources == null || build.ContentFiles == null || string.IsNullOrWhiteSpace(build.WorkspaceRoot))
            throw new InvalidDataException("Build collections and workspaceRoot cannot be null/empty.");
        if (build.ProjectReferences.Any(string.IsNullOrWhiteSpace) || build.GeneratedSourceFiles.Any(string.IsNullOrWhiteSpace) || build.EmbeddedResources.Any(r => r == null) || build.ContentFiles.Any(r => r == null))
            throw new InvalidDataException("Build input entries cannot be null or blank.");
        if (build.ProjectReferences.Count > 32 || build.GeneratedSourceFiles.Count > 1024 || build.EmbeddedResources.Count > 256 || build.ContentFiles.Count > 1024)
            throw new InvalidDataException("Project input capacity exceeded.");
        if (build.ProjectReferences.Distinct(StringComparer.OrdinalIgnoreCase).Count() != build.ProjectReferences.Count)
            throw new InvalidDataException("Duplicate ProjectReference.");
        if (build.ProjectReferenceMetadata == null || build.ProjectReferenceMetadata.Keys.Any(k => !build.ProjectReferences.Contains(k, StringComparer.Ordinal)))
            throw new InvalidDataException("ProjectReferenceMetadata must name an exact declared ProjectReference.");
        _ = Snapshot(context);
    }

    internal static IReadOnlyList<AuthorProjectContext> LoadForRestore(AuthorProjectContext root) => Load(root, new List<AuthorDiagnostic>(), false);

    internal static IReadOnlyList<AuthorProjectContext> Load(AuthorProjectContext root, List<AuthorDiagnostic> diagnostics, bool validateCache = true)
    {
        string workspace = Path.GetFullPath(root.AuthorProject.Build?.WorkspaceRoot ?? ".", root.RootPath);
        RequireUnder(workspace, root.RootPath);
        var sorted = new List<AuthorProjectContext>();
        var visited = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void Visit(AuthorProjectContext current, AuthorProjectContext? inheritedRestore = null)
        {
            var restoreOwner = current.AuthorProject.Build?.Restore != null ? current : inheritedRestore;
            for (string? directory = current.RootPath; directory != null; directory = Path.GetDirectoryName(directory))
            {
                foreach (string name in new[] { "Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props" })
                    if (File.Exists(Path.Combine(directory, name))) throw new InvalidDataException("Unsupported inherited project input: " + Path.Combine(directory, name));
                if (directory.Equals(workspace, StringComparison.OrdinalIgnoreCase)) break;
            }
            if (visited.TryGetValue(current.RootPath, out int state))
            {
                if (state == 1) throw new InvalidDataException("ProjectReference cycle: " + current.RootPath);
                var prior = sorted.Single(n => n.RootPath.Equals(current.RootPath, StringComparison.OrdinalIgnoreCase));
                if (prior.LibraryRole != current.LibraryRole || prior.LibraryDistribution != current.LibraryDistribution || !prior.LibraryLicenseFiles.SequenceEqual(current.LibraryLicenseFiles))
                    throw new InvalidDataException("ProjectReference metadata conflict: " + current.RootPath);
                return;
            }
            if (visited.Count >= 32) throw new InvalidDataException("ProjectReference graph exceeds 32 projects.");
            visited.Add(current.RootPath, 1);
            if (!names.Add(current.AuthorProject.AssemblyName)) throw new InvalidDataException("Duplicate project AssemblyName: " + current.AuthorProject.AssemblyName);
            if (current.ApiTarget != root.ApiTarget) throw new InvalidDataException("ProjectReference API targets must match exactly.");
            foreach (string reference in (current.AuthorProject.Build?.ProjectReferences ?? new()).OrderBy(value => value, StringComparer.Ordinal))
            {
                if (!reference.EndsWith(".csproj", StringComparison.Ordinal)) throw new InvalidDataException("ProjectReference must name an explicit .csproj file.");
                string file = Path.GetFullPath(reference, current.RootPath);
                RequireUnder(workspace, file);
                if (!File.Exists(file)) throw new InvalidDataException("ProjectReference missing: " + file);
                string childRoot = Path.GetDirectoryName(file)!;
                if (childRoot.Equals(root.RootPath, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("ProjectReference cycle to entry project.");
                bool ordinary = !File.Exists(Path.Combine(childRoot, "dtmapi.library.json"));
                var child = ordinary ? OrdinaryLibraryInput.Load(file, root.ApiTarget, workspace) : validateCache ? ProjectValidator.LoadValidated(childRoot, diagnostics) : ProjectValidator.LoadForRestore(childRoot);
                if (current.AuthorProject.Build!.ProjectReferenceMetadata.TryGetValue(reference, out var metadata))
                {
                    if (metadata == null || metadata.Role is not ("private-managed" or "shared-contract") || metadata.Distribution is not ("self-authored" or "licensed-third-party") || metadata.LicenseFiles == null)
                        throw new InvalidDataException("Invalid ProjectReference metadata: " + file);
                    if (!ordinary && metadata.Role != child.LibraryRole) throw new InvalidDataException("SDK library role conflicts with reference metadata: " + file);
                    child.LibraryRole = metadata.Role; child.LibraryDistribution = metadata.Distribution;
                    child.LibraryLicenseFiles = metadata.LicenseFiles.Select(p => Path.GetFullPath(p, current.RootPath)).ToList();
                    foreach (var license in child.LibraryLicenseFiles) { RequireUnder(workspace, license); if (!File.Exists(license) || new FileInfo(license).Length == 0) throw new InvalidDataException("ProjectReference license missing: " + license); }
                    if (metadata.Distribution == "licensed-third-party" && child.LibraryLicenseFiles.Count == 0) throw new InvalidDataException("ProjectReference third-party license required: " + file);
                }
                if (ordinary && child.AuthorProject.Build?.Restore is { } restore)
                {
                    var parentRestore = restoreOwner?.AuthorProject.Build?.Restore ?? throw new InvalidDataException("ordinary-library-unsupported: " + file + ": PackageReference needs explicit sources/transitive licenseFiles in the parent restore settings.");
                    restore.Sources = parentRestore.Sources.Select(s => Uri.TryCreate(s, UriKind.Absolute, out var uri) && !uri.IsFile ? s : Path.GetFullPath(s, restoreOwner!.RootPath)).ToList();
                    foreach (var license in parentRestore.LicenseFiles)
                        if (!restore.LicenseFiles.TryGetValue(license.Key, out var own) || own.Count == 0)
                            restore.LicenseFiles[license.Key] = license.Value.Select(p => Path.GetRelativePath(childRoot, Path.GetFullPath(p, restoreOwner!.RootPath))).ToList();
                }
                if (!child.IsLibrary || (!ordinary && Path.GetFileName(file) != child.AuthorProject.AssemblyName + ".csproj"))
                    throw new InvalidDataException("ProjectReference requires a self-authored SDK library and its exact project filename.");
                Visit(child, restoreOwner);
            }
            visited[current.RootPath] = 2;
            sorted.Add(current);
        }
        Visit(root);
        var contentNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var node in sorted)
        {
            node.BuildSnapshot = Snapshot(node);
            foreach (var file in node.BuildSnapshot.Content)
                if (!contentNames.Add(file.Name)) throw new InvalidDataException("Graph content target collision: " + file.Name);
        }
        return sorted;
    }

    internal static void RequireUnder(string root, string path)
    {
        string boundary = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        string full = Path.GetFullPath(path);
        if (!full.Equals(boundary, StringComparison.OrdinalIgnoreCase) && !full.StartsWith(boundary + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Project input escapes declared workspace: " + path);
        // Check every ancestor, including ancestors of the declared boundary.
        for (string? current = full; current != null; current = Path.GetDirectoryName(current))
            if (File.Exists(current) || Directory.Exists(current)) PathSafety.RejectReparsePoints(current, Array.Empty<string>());
    }

    internal sealed record FileInput(string Path, string Name, byte[] Bytes);
    internal sealed record Inputs(CodeModBuildInput Sources, FileInput[] Embedded, FileInput[] Content, string Identity)
    {
        internal ResourceDescription[] Resources => Embedded.Select(file => new ResourceDescription(file.Name, () => new MemoryStream(file.Bytes, false), true)).ToArray();
    }

    internal static Inputs Snapshot(AuthorProjectContext context)
    {
        var build = context.AuthorProject.Build;
        var ordinarySources = context.OrdinaryLibrary?.Sources();
        string[] files = (ordinarySources?.Keys ?? Directory.EnumerateFiles(context.SourcePath, "*.cs", SearchOption.AllDirectories)).OrderBy(p => p, StringComparer.Ordinal).ToArray();
        foreach (string file in files) RequireUnder(context.OrdinaryLibrary?.Workspace ?? context.RootPath, file);
        var seenSources = new HashSet<string>(files, StringComparer.OrdinalIgnoreCase);
        foreach (string generated in build?.GeneratedSourceFiles ?? new())
        {
            string file = PathSafety.ResolveUnderRoot(context.RootPath, generated, "generatedSourceFiles");
            RequireUnder(context.RootPath, file);
            if (!File.Exists(file) || !file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) || !seenSources.Add(file))
                throw new InvalidDataException("Generated source missing, duplicated or not C#: " + generated);
        }
        var embedded = new List<FileInput>();
        var content = new List<FileInput>();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var resource in build?.EmbeddedResources ?? new())
        {
            if (string.IsNullOrWhiteSpace(resource.LogicalName) || resource.LogicalName.Length > 512 || resource.LogicalName.Any(char.IsControl) || !names.Add(resource.LogicalName))
                throw new InvalidDataException("Embedded resource logical name missing or colliding.");
            embedded.Add(ReadFile(context.RootPath, resource.Path, resource.LogicalName));
        }
        names.Clear();
        foreach (var file in build?.ContentFiles ?? new())
        {
            string normalized = file.TargetPath;
            if (string.IsNullOrEmpty(normalized) || normalized.Contains('\\') || normalized.Split('/').Any(p => p.Length == 0 || p is "." or ".." || p.EndsWith('.') || p.EndsWith(' ') || p.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0))
                throw new InvalidDataException("contentFiles target must be a normalized relative path.");
            if (!normalized.StartsWith("Content/", StringComparison.Ordinal) || normalized.StartsWith("Content/DTMAPI/", StringComparison.OrdinalIgnoreCase) || !names.Add(normalized))
                throw new InvalidDataException("contentFiles target must be unique under Content/ outside SDK-owned Content/DTMAPI/.");
            var input = ReadFile(context.RootPath, file.Path, normalized);
            if (input.Bytes.Length >= 2 && input.Bytes[0] == 'M' && input.Bytes[1] == 'Z') throw new InvalidDataException("Content resources cannot hide executable payloads.");
            content.Add(input);
        }
        var sourceInput = CodeModBuildInput.Read(build == null ? context.SourcePath : context.RootPath, seenSources.OrderBy(p => p, StringComparer.Ordinal), ordinarySources == null ? null : file => ordinarySources[file]);
        string identity = PathSafety.Sha256Bytes(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
        { sources = sourceInput.SourceTreeSha256, embedded = embedded.Select(f => new { f.Name, sha256 = PathSafety.Sha256Bytes(f.Bytes) }), content = content.Select(f => new { f.Name, sha256 = PathSafety.Sha256Bytes(f.Bytes) }) })));
        return new Inputs(sourceInput, embedded.OrderBy(f => f.Name, StringComparer.Ordinal).ToArray(), content.OrderBy(f => f.Name, StringComparer.Ordinal).ToArray(), identity);
    }

    private static FileInput ReadFile(string root, string relative, string name)
    {
        string path = PathSafety.ResolveUnderRoot(root, relative, "resource input");
        RequireUnder(root, path);
        if (!File.Exists(path) || new FileInfo(path).Length > 16 * 1024 * 1024) throw new InvalidDataException("Resource missing or larger than 16 MiB: " + relative);
        return new FileInput(relative.Replace('\\', '/'), name, File.ReadAllBytes(path));
    }

    internal static ManagedPackageInput AsLibrary(AuthorProjectContext context, string path)
    {
        var record = PackagePortableMetadata.Inspect(File.ReadAllBytes(path));
        record.Path = (context.LibraryRole == "shared-contract" ? "lib/shared/" : "lib/private/") + Path.GetFileName(path);
        record.Role = context.LibraryRole; record.Distribution = context.LibraryDistribution; record.Source = "project-reference:" + context.AuthorProject.AssemblyName;
        var licenses = context.LibraryLicenseFiles.ToDictionary(p => "licenses/" + record.Identity.Name + "/" + Path.GetFileName(p), p => p, StringComparer.Ordinal);
        record.LicenseFiles = licenses.Keys.ToArray();
        return new ManagedPackageInput(path, record, licenses);
    }

    internal static IEnumerable<AuthorProjectContext> DirectDependencies(AuthorProjectContext context, IReadOnlyList<AuthorProjectContext> graph)
        => (context.AuthorProject.Build?.ProjectReferences ?? new()).Select(reference =>
            graph.Single(node => node.RootPath.Equals(Path.GetDirectoryName(Path.GetFullPath(reference, context.RootPath)), StringComparison.OrdinalIgnoreCase)));

    internal static void AddLibraries(AuthorProjectContext context, IEnumerable<ManagedPackageInput> inputs)
    {
        foreach (var input in inputs)
        {
            var existing = context.BuiltLibraries.SingleOrDefault(item => item.Record.Identity.Name.Equals(input.Record.Identity.Name, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                if (existing.Record.Sha256 != input.Record.Sha256 || existing.Record.Role != input.Record.Role)
                    throw new InvalidDataException("Graph library identity/role/bytes conflict: " + input.Record.Identity.Name);
                continue;
            }
            context.BuiltLibraries.Add(input);
        }
    }

    internal static bool AcceptProjectItem(AuthorProjectContext context, XElement item)
    {
        var build = context.AuthorProject.Build!;
        string include = ((string?)item.Attribute("Include") ?? "").Replace('\\', '/');
        if (item.Name == "PackageReference")
            return !item.HasElements && item.Attributes().All(a => a.Name == "Include" || a.Name == "Version" || a.Name == "Label") &&
                build.Restore?.Packages.TryGetValue(include, out string? version) == true && (string?)item.Attribute("Version") == "[" + version + "]";
        if (item.Attributes().Any(a => a.Name != "Include" && a.Name != "Label")) return false;
        if (item.Name == "ProjectReference") return !item.HasElements && build.ProjectReferences.Any(p => p.Replace('\\', '/') == include);
        if (item.Name == "Compile") return !item.HasElements && build.GeneratedSourceFiles.Any(p => p.Replace('\\', '/') == include);
        if (item.Name == "EmbeddedResource")
            return item.Elements().Count() == 1 && item.Element("LogicalName") is { HasElements: false, HasAttributes: false } logical &&
                build.EmbeddedResources.Any(r => r.Path.Replace('\\', '/') == include && r.LogicalName == logical.Value);
        if (item.Name == "Content")
            return item.Elements().Count() == 2 && item.Element("TargetPath") is { HasElements: false, HasAttributes: false } target &&
                item.Element("CopyToOutputDirectory") is { HasElements: false, HasAttributes: false } copy && copy.Value == "PreserveNewest" &&
                build.ContentFiles.Any(r => r.Path.Replace('\\', '/') == include && r.TargetPath == target.Value);
        return false;
    }

    internal static CommandReport CreateLibrary(ParsedCommand command)
    {
        command.RequireOnlyOptions("id", "version", "api-target", "role");
        if (command.Positionals.Count != 2) throw new CommandLineException("new library requires a new directory and --id Assembly.Name.");
        string root = PathSafety.FullPath(command.Positionals[1]);
        PathSafety.RejectBepInExPluginDestination(root);
        if (Directory.Exists(root) || File.Exists(root)) throw new CommandLineException("Library destination already exists.");
        var descriptor = new AuthorLibraryProject { AssemblyName = command.Option("id"), Version = command.Option("version", "0.1.0"),
            ApiTarget = command.Option("api-target", AuthorApiTargetCatalog.Current.DefaultTarget), Role = command.Option("role", "private-managed") };
        if (!System.Text.RegularExpressions.Regex.IsMatch(descriptor.AssemblyName, @"^[A-Za-z][A-Za-z0-9]*(?:\.[A-Za-z][A-Za-z0-9_]*)+$"))
            throw new CommandLineException("Library --id must be a dotted C# namespace/assembly name.");
        string staging = Path.Combine(Path.GetDirectoryName(root)!, ".dtmapi-library-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(staging, "src"));
        File.WriteAllText(Path.Combine(staging, "dtmapi.library.json"), JsonSupport.SerializeTool(descriptor), new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(staging, "src", "Library.cs"), "namespace " + descriptor.AssemblyName + ";\npublic static class Library { public static int Value => 42; }\n", new UTF8Encoding(false));
        var diagnostics = new List<AuthorDiagnostic>();
        var context = LoadLibrary(staging);
        File.WriteAllText(Path.Combine(staging, descriptor.AssemblyName + ".csproj"), TemplateCreator.BuildProjectText(context, descriptor.AssemblyName), new UTF8Encoding(false));
        _ = ProjectValidator.LoadValidated(staging, diagnostics);
        var report = new CommandReport { Command = "new library", RootPath = root, TargetRuntimeVersion = descriptor.ApiTarget };
        report.Diagnostics.AddRange(diagnostics);
        if (diagnostics.Any(d => d.Severity == DTMAPI.Authoring.Contracts.DiagnosticSeverity.Error)) return report;
        Directory.Move(staging, root);
        report.Success = true; report.OutputPath = root;
        return report;
    }
}
