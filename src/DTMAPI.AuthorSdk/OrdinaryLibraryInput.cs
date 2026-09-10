using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using DTMAPI.Authoring.Contracts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DTMAPI.AuthorSdk;

// Adapter into the existing graph. It never evaluates targets or rewrites the input project.
internal sealed class OrdinaryLibraryInput
{
    public required string ProjectPath { get; init; }
    public required string Workspace { get; init; }
    public required string AssemblyVersion { get; init; }
    public required string FileVersion { get; init; }
    public required LanguageVersion Language { get; init; }
    public required bool Checked { get; init; }
    public required NullableContextOptions Nullable { get; init; }
    public required bool DefaultCompileItems { get; init; }
    public required XElement[] CompileItems { get; init; }

    internal static AuthorProjectContext Load(string projectPath, string apiTarget, string workspace)
    {
        try { return Read(projectPath, apiTarget, workspace); }
        catch (Exception ex) when (ex is InvalidDataException or System.Xml.XmlException or ArgumentException or FormatException)
        { throw new InvalidDataException("ordinary-library-unsupported: " + projectPath + ": " + ex.Message + "; build this library with standard MSBuild and use managedReferences for unsupported inputs.", ex); }
    }
    private static AuthorProjectContext Read(string path, string apiTarget, string workspace)
    {
        ProjectGraph.RequireUnder(workspace, path);
        string root = Path.GetDirectoryName(path)!;
        if (File.Exists(Path.Combine(root, "manifest.json")) || File.Exists(Path.Combine(root, "dtmapi.author.json"))) throw new InvalidDataException("A Mod cannot be used as an ordinary library.");
        var document = XDocument.Load(path);
        var project = document.Root;
        if (project?.Name != "Project" || (string?)project.Attribute("Sdk") != "Microsoft.NET.Sdk" || !Attrs(project, "Sdk")) throw new InvalidDataException("Expected Microsoft.NET.Sdk library project.");
        var properties = new Dictionary<string, string>(StringComparer.Ordinal);
        var items = new List<XElement>();
        foreach (var group in project.Elements())
        {
            if (group.Name == "PropertyGroup")
            {
                if (!Attrs(group, "Condition", "Label")) throw new InvalidDataException("Unsupported PropertyGroup attributes.");
                _ = ProjectCompilerSettings.Condition((string?)group.Attribute("Condition"), "Debug");
                foreach (var property in group.Elements())
                {
                    string propertyName = property.Name.LocalName;
                    if (property.HasElements || !Attrs(property, "Condition", "Label")) throw new InvalidDataException("Unsupported property " + propertyName);
                    if (ProjectCompilerSettings.IsOption(propertyName)) continue;
                    if (group.Attribute("Condition") != null || property.Attribute("Condition") != null) throw new InvalidDataException("Conditional " + propertyName + " is outside the ordinary-library subset.");
                    if (!new[] { "TargetFramework", "OutputType", "AssemblyName", "RootNamespace", "Version", "AssemblyVersion", "FileVersion", "LangVersion", "Nullable", "CheckForOverflowUnderflow", "EnableDefaultCompileItems", "GenerateAssemblyInfo", "GenerateTargetFrameworkAttribute", "ImplicitUsings", "AllowUnsafeBlocks", "Deterministic", "RestorePackagesWithLockFile", "RestoreLockedMode" }.Contains(propertyName)) throw new InvalidDataException("Unsupported property " + propertyName);
                    if (property.Value.Contains("$(") || property.Value.Contains("@(")) throw new InvalidDataException("Property expressions are unsupported: " + propertyName);
                    properties[propertyName] = property.Value.Trim();
                }
            }
            else if (group.Name == "ItemGroup" && Attrs(group, "Label")) items.AddRange(group.Elements().Select(e => new XElement(e)));
            else throw new InvalidDataException("Unsupported element/conditional group " + group.Name);
        }
        string Get(string key, string fallback) => properties.TryGetValue(key, out string? value) ? value : fallback;
        bool Flag(string key, bool fallback) => bool.TryParse(Get(key, fallback.ToString()), out bool value) ? value : throw new InvalidDataException("Invalid boolean " + key);
        if (Get("TargetFramework", "") != "netstandard2.0" || Get("OutputType", "Library") != "Library") throw new InvalidDataException("Only a single netstandard2.0 Library target is supported.");
        if (!Flag("GenerateAssemblyInfo", true) || !Flag("GenerateTargetFrameworkAttribute", true) || Flag("AllowUnsafeBlocks", false) || !Flag("Deterministic", true)
            || Get("ImplicitUsings", "disable") is not ("disable" or "false")) throw new InvalidDataException("Unsupported compiler/generated-input option.");
        string name = Get("AssemblyName", Path.GetFileNameWithoutExtension(path));
        if (!Regex.IsMatch(name, @"^[A-Za-z_][A-Za-z0-9_.-]*$")) throw new InvalidDataException("Invalid AssemblyName.");
        string version = Get("Version", "1.0.0");
        if (!Regex.IsMatch(version, @"^\d+\.\d+\.\d+(?:-[A-Za-z0-9.-]+)?$")) throw new InvalidDataException("Version requires major.minor.patch with optional prerelease.");
        string numeric = version.Split('-')[0] + ".0";
        string VersionValue(string key) => Version.TryParse(Get(key, numeric), out var value) && value.Major <= 65535 && value.Minor <= 65535 && value.Build is >= 0 and <= 65535 && value.Revision is >= 0 and <= 65535 ? value.ToString() : throw new InvalidDataException("Invalid " + key);
        string nullable = Get("Nullable", "disable");
        if (nullable is not ("enable" or "disable")) throw new InvalidDataException("Nullable supports enable/disable.");
        LanguageVersion language = Get("LangVersion", "7.3") switch { "7.3" => LanguageVersion.CSharp7_3, "12.0" => LanguageVersion.CSharp12, _ => throw new InvalidDataException("LangVersion supports explicit 7.3 or 12.0 in this subset.") };
        var build = new AuthorBuildSettings { WorkspaceRoot = Path.GetRelativePath(root, workspace) };
        var compile = new List<XElement>();
        var packages = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var licenses = new SortedDictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            string include = (string?)item.Attribute("Include") ?? "";
            if (item.Name == "Compile")
            {
                if (!Attrs(item, "Include", "Remove", "Link") || item.Elements().Any(e => e.Name != "Link" || e.HasAttributes || e.HasElements)
                    || (item.Attribute("Include") == null) == (item.Attribute("Remove") == null)) throw new InvalidDataException("Compile supports Include/Remove with an optional literal Link.");
                compile.Add(item);
            }
            else if (item.Name == "ProjectReference" && Attrs(item, "Include") && !item.HasElements) build.ProjectReferences.Add(include);
            else if (item.Name == "EmbeddedResource" && Attrs(item, "Include", "LogicalName") && item.Elements().All(e => e.Name == "LogicalName" && !e.HasAttributes && !e.HasElements))
            {
                if (include.EndsWith(".resx", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("resx generation is not supported; provide the externally generated resource.");
                build.EmbeddedResources.Add(new AuthorEmbeddedResource { Path = include, LogicalName = (string?)item.Attribute("LogicalName") ?? item.Element("LogicalName")?.Value ?? Get("RootNamespace", name) + "." + include.Replace('/', '.').Replace('\\', '.') });
            }
            else if (item.Name == "PackageReference" && Attrs(item, "Include", "Version", "DtmApiLicenseFiles") && !item.HasElements)
            {
                string packageVersion = ((string?)item.Attribute("Version") ?? "").Trim('[', ']');
                if (!NuGet.Versioning.NuGetVersion.TryParse(packageVersion, out var parsed) || packageVersion != parsed.ToNormalizedString() || !packages.TryAdd(include, packageVersion)) throw new InvalidDataException("PackageReference requires a unique exact version.");
                licenses[include] = ((string?)item.Attribute("DtmApiLicenseFiles") ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
            }
            else throw new InvalidDataException("Unsupported item " + item.Name + ". Explicit non-code resources must be representable without MSBuild tasks.");
        }
        if (Directory.EnumerateFiles(root, "*.resx", SearchOption.AllDirectories).Any(p => !IsExcluded(root, p))) throw new InvalidDataException("Implicit resx resources require an external standard build.");
        if (packages.Count > 0)
        {
            // Sources and all transitive licenses come from the existing parent restore
            // declaration; package versions stay owned by this csproj and its NuGet lock.
            build.Restore = new AuthorRestoreSettings { Packages = packages, LicenseFiles = licenses };
        }
        var input = new OrdinaryLibraryInput { ProjectPath = path, Workspace = workspace, AssemblyVersion = VersionValue("AssemblyVersion"), FileVersion = VersionValue("FileVersion"), Language = language,
            Checked = Flag("CheckForOverflowUnderflow", false), Nullable = nullable == "enable" ? NullableContextOptions.Enable : NullableContextOptions.Disable,
            DefaultCompileItems = Flag("EnableDefaultCompileItems", true), CompileItems = compile.ToArray() };
        var manifest = new RuntimeManifest { Name = name, Version = version, Type = "CodeMod", CodeModKind = "Strict", DependencyContractVersion = 1 };
        var context = new AuthorProjectContext { RootPath = root, AuthorProjectPath = path, ManifestPath = path, Manifest = manifest, ManifestJson = new JsonObject(),
            AuthorProject = new AuthorProject { SchemaVersion = 3, ProjectKind = "CodeMod", CodeModKind = "Strict", AssemblyName = name, SourceDirectory = ".", TargetDtmApiVersion = apiTarget, Build = build, ManagedReferences = new() },
            SourcePath = root, ContentPath = Path.Combine(root, "content"), Kind = AuthorProjectKind.CodeMod, CodeModKind = AuthorCodeModKind.Strict, ManifestDeclaresCodeModKind = true, IsLibrary = true, OrdinaryLibrary = input };
        _ = ProjectCompilerSettings.Read(context, "Debug"); _ = ProjectCompilerSettings.Read(context, "Release");
        return context;
    }
    private static bool Attrs(XElement element, params string[] names) => element.Attributes().All(a => names.Contains(a.Name.LocalName));
    private static bool IsExcluded(string root, string file) => Path.GetRelativePath(root, file).Replace('\\', '/').Split('/').Any(p => p is "bin" or "obj" or ".git" or ".vs");
    internal IReadOnlyDictionary<string, string> Sources()
    {
        string root = Path.GetDirectoryName(ProjectPath)!;
        var sources = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (DefaultCompileItems)
            foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories).Where(p => !IsExcluded(root, p))) sources[file] = Path.GetRelativePath(root, file).Replace('\\', '/');
        foreach (var item in CompileItems)
        {
            bool remove = item.Attribute("Remove") != null;
            string pattern = ((string?)item.Attribute(remove ? "Remove" : "Include"))!.Replace('\\', '/');
            if (pattern.Contains("$(") || pattern.Contains("@(") || pattern.Contains(';')) throw new InvalidDataException("ordinary-library-unsupported: " + ProjectPath + ": unsupported Compile expression.");
            string? link = (string?)item.Attribute("Link") ?? item.Element("Link")?.Value;
            string fullPattern = Path.GetFullPath(pattern, root).Replace('\\', '/');
            ProjectGraph.RequireUnder(Workspace, fullPattern);
            string regex = "^" + Regex.Escape(fullPattern).Replace(@"\*\*/", "(?:.*/)?").Replace(@"\*", "[^/]*").Replace(@"\?", "[^/]") + "$";
            var matches = (remove ? sources.Keys.ToArray() : pattern.IndexOfAny(new[] { '*', '?' }) < 0 ? new[] { Path.GetFullPath(pattern, root) }
                : Directory.EnumerateFiles(Workspace, "*.cs", SearchOption.AllDirectories).Where(p => !IsExcluded(Workspace, p)))
                .Where(p => Regex.IsMatch(p.Replace('\\', '/'), regex, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).ToArray();
            if (link != null && matches.Length != 1) throw new InvalidDataException("ordinary-library-unsupported: Link must select one source: " + ProjectPath);
            foreach (var file in matches)
            {
                if (remove) { sources.Remove(file); continue; }
                ProjectGraph.RequireUnder(Workspace, file);
                if (!File.Exists(file) || !file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Ordinary library source missing/not C#: " + file);
                if (sources.ContainsKey(file)) throw new InvalidDataException("Duplicate Compile Include: " + file);
                string logical = link ?? Path.GetRelativePath(root, file).Replace('\\', '/');
                if (logical.Contains("$(") || Path.IsPathRooted(logical) || logical.Split('/').Any(p => p is ".." or "." or "")) throw new InvalidDataException("Use a literal relative Link for source outside the library: " + file);
                sources.Add(file, logical);
            }
        }
        if (sources.Values.Distinct(StringComparer.OrdinalIgnoreCase).Count() != sources.Count) throw new InvalidDataException("Ordinary library Compile Link collision: " + ProjectPath);
        foreach (var file in sources.Keys) ProjectGraph.RequireUnder(Workspace, file);
        return sources;
    }
}
