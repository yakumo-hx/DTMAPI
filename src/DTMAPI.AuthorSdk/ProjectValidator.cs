using DTMAPI.Authoring.Contracts;
using DTMAPI.Internal.Authoring;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace DTMAPI.AuthorSdk;

internal sealed class AuthorProjectContext
{
    public required string RootPath { get; init; }
    public required string ManifestPath { get; init; }
    public required string AuthorProjectPath { get; init; }
    public required RuntimeManifest Manifest { get; init; }
    public required JsonObject ManifestJson { get; init; }
    public required AuthorProject AuthorProject { get; init; }
    public required AuthorProjectKind Kind { get; init; }
    public required AuthorCodeModKind CodeModKind { get; init; }
    public required bool ManifestDeclaresCodeModKind { get; init; }
    public required string ContentPath { get; init; }
    public List<ManagedPackageInput> BuiltLibraries { get; } = new();
    public string ApiTarget => AuthorProject.TargetDtmApiVersion;
}

internal static class ProjectValidator
{
    private static readonly Regex UniqueIdPattern = new(@"^[A-Za-z][A-Za-z0-9]*(?:\.[A-Za-z0-9][A-Za-z0-9_-]*)+$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex VersionPattern = new(@"^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(?:-[0-9A-Za-z.-]+)?$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex MinimumVersionPattern = new(@"^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static CommandReport ValidateCommand(ParsedCommand command)
    {
        command.RequireOnlyOptions();
        if (command.Positionals.Count != 1)
            throw new CommandLineException("validate requires one project directory.");
        return ValidateRoot(PathSafety.FullPath(command.Positionals[0]), "validate");
    }

    public static CommandReport ValidateRoot(string root, string commandName = "validate")
    {
        var report = new CommandReport { Command = commandName, RootPath = Path.GetFullPath(root) };
        try
        {
            AuthorProjectContext context = Load(root, report.Diagnostics);
            ValidateContext(context, report.Diagnostics);
            report.Values["uniqueID"] = context.Manifest.UniqueID;
            report.Values["version"] = context.Manifest.Version;
            report.Values["projectKind"] = context.Kind.ToString();
            report.Values["codeModKind"] = context.Kind == AuthorProjectKind.CodeMod ? context.CodeModKind.ToString() : string.Empty;
            report.Values["manifestAuthority"] = "manifest.json";
            report.TargetRuntimeVersion = context.ApiTarget;
            report.Values["targetRuntime"] = context.ApiTarget;
            report.Values["apiTarget"] = context.ApiTarget;
            report.Values["minimumRuntimeVersion"] = context.Manifest.MinimumDTMApiVersion;
            report.FileCount = Directory.EnumerateFiles(context.RootPath, "*", SearchOption.AllDirectories)
                .Count(path => !PathSafety.IsBuildPath(PathSafety.RelativePath(context.RootPath, path)));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        {
            report.Diagnostics.Add(Error("SDK100", ex.Message, report.RootPath));
        }

        report.Success = report.Diagnostics.All(diagnostic => diagnostic.Severity != DiagnosticSeverity.Error);
        if (report.Success)
            report.Diagnostics.Insert(0, Info("SDK000", "Project is valid for DTMAPI API target " + report.TargetRuntimeVersion + "."));
        return report;
    }

    public static AuthorProjectContext LoadValidated(string root, List<AuthorDiagnostic> diagnostics)
    {
        AuthorProjectContext context = Load(root, diagnostics);
        ValidateContext(context, diagnostics);
        return context;
    }

    internal static AuthorProjectContext LoadForRestore(string root) => Load(root, new List<AuthorDiagnostic>());

    private static AuthorProjectContext Load(string root, List<AuthorDiagnostic> diagnostics)
    {
        string fullRoot = Path.GetFullPath(root);
        if (!Directory.Exists(fullRoot))
            throw new DirectoryNotFoundException("Project directory was not found: " + fullRoot);
        PathSafety.RejectBepInExPluginDestination(fullRoot);

        if (File.Exists(Path.Combine(fullRoot, "dtmapi.library.json")))
            throw new InvalidDataException("dtmapi.library.json was an internal prototype input. Build ordinary libraries with their standard .csproj and reference them from the Mod project.");

        string manifestPath = Path.Combine(fullRoot, "manifest.json");
        string authorPath = Path.Combine(fullRoot, "dtmapi.author.json");
        if (!File.Exists(manifestPath))
            throw new InvalidDataException("manifest.json is required at the author project root.");
        if (!File.Exists(authorPath))
            throw new InvalidDataException("dtmapi.author.json is required; it stores SDK-only, pre-1.0 build/publish inputs.");

        RejectDuplicateTopLevelProperties(manifestPath);
        RejectDuplicateTopLevelProperties(authorPath);
        string manifestText = File.ReadAllText(manifestPath, Encoding.UTF8);
        RuntimeManifest manifest = JsonSerializer.Deserialize<RuntimeManifest>(manifestText, JsonSupport.RuntimeManifest)
            ?? throw new InvalidDataException("manifest.json must contain one JSON object.");
        JsonObject manifestJson = JsonNode.Parse(manifestText)?.AsObject()
            ?? throw new InvalidDataException("manifest.json must contain one JSON object.");
        AuthorProject author = JsonSerializer.Deserialize<AuthorProject>(File.ReadAllText(authorPath, Encoding.UTF8), JsonSupport.Tool)
            ?? throw new InvalidDataException("dtmapi.author.json must contain one JSON object.");
        if (author.SchemaVersion != StandardBuildIntegration.SchemaVersion)
            throw new InvalidDataException("Unsupported internal author input schema. The first public SDK requires schemaVersion 4 and standard MSBuild project inputs.");
        StandardBuildIntegration.ValidateAuthorInput(authorPath, author);
        author.AssemblyName = Path.GetFileNameWithoutExtension(manifest.EntryDll);
        _ = PackageDependencyContract.ReadManifest(Encoding.UTF8.GetBytes(manifestText));
        bool nativeSelected = NativePackageContract.Select(Encoding.UTF8.GetBytes(manifestText));
        if (nativeSelected != (author.NativeReferences != null)) throw new InvalidDataException("nativeReferences and explicit manifest NativeContractVersion=1 must be selected together.");
        if (!Enum.TryParse(author.ProjectKind, true, out AuthorProjectKind kind))
            throw new InvalidDataException("dtmapi.author.json projectKind must be CodeMod or ContentPack.");
        bool manifestDeclaresCodeModKind = manifestJson.ContainsKey("CodeModKind");
        AuthorCodeModKind codeModKind = AuthorCodeModKind.Strict;
        if (manifestDeclaresCodeModKind && !Enum.TryParse(manifest.CodeModKind, ignoreCase: false, out codeModKind))
            throw new InvalidDataException("manifest.json CodeModKind must be exactly Strict or Advanced.");

        string contentPath = PathSafety.ResolveUnderRoot(fullRoot, author.ContentDirectory, "contentDirectory");
        IEnumerable<string> existing = Directory.EnumerateFileSystemEntries(fullRoot, "*", SearchOption.AllDirectories);
        PathSafety.RejectReparsePoints(fullRoot, existing);
        if (File.Exists(Path.Combine(fullRoot, "info.json")))
            diagnostics.Add(Error("SDK116", "Do not hand-maintain info.json in the author project. pack projects it from manifest.json plus SDK publish metadata.", Path.Combine(fullRoot, "info.json")));

        return new AuthorProjectContext
        {
            RootPath = fullRoot,
            ManifestPath = manifestPath,
            AuthorProjectPath = authorPath,
            Manifest = manifest,
            ManifestJson = manifestJson,
            AuthorProject = author,
            Kind = kind,
            CodeModKind = codeModKind,
            ManifestDeclaresCodeModKind = manifestDeclaresCodeModKind,
            ContentPath = contentPath
        };
    }

    private static void ValidateContext(AuthorProjectContext context, List<AuthorDiagnostic> diagnostics)
    {
        RuntimeManifest manifest = context.Manifest;
        AuthorProject author = context.AuthorProject;
        Required(manifest.Name, "Name", context.ManifestPath, diagnostics);
        Required(manifest.Author, "Author", context.ManifestPath, diagnostics);
        Required(manifest.Description, "Description", context.ManifestPath, diagnostics);
        Required(manifest.UniqueID, "UniqueID", context.ManifestPath, diagnostics);
        Required(manifest.Version, "Version", context.ManifestPath, diagnostics);
        Required(manifest.MinimumDTMApiVersion, "MinimumDTMApiVersion", context.ManifestPath, diagnostics);
        Required(manifest.Type, "Type", context.ManifestPath, diagnostics);

        if (!UniqueIdPattern.IsMatch(manifest.UniqueID ?? string.Empty))
            diagnostics.Add(Error("SDK102", "UniqueID must be dotted and path-safe, for example Author.ModName.", context.ManifestPath));
        if (!ManagedPackageReferences.UsesContract(context) && !VersionPattern.IsMatch(manifest.Version ?? string.Empty))
            diagnostics.Add(Error("SDK103", "Version must use numeric major.minor.patch with an optional label.", context.ManifestPath));
        if (!MinimumVersionPattern.IsMatch(manifest.MinimumDTMApiVersion ?? string.Empty))
            diagnostics.Add(Error("SDK104", "MinimumDTMApiVersion must use numeric major.minor.patch.", context.ManifestPath));
        else if (!AuthorApiTargetCatalog.Current.TryValidatePackageTarget(context.ApiTarget, AuthorSdkContract.SdkVersion,
            manifest.MinimumDTMApiVersion ?? string.Empty, out _, out string targetReason))
            diagnostics.Add(Error("SDK105", targetReason, context.ManifestPath));

        ValidateAuthorProjectVersion(context, diagnostics);
        if (!manifest.Type.Equals(context.Kind.ToString(), StringComparison.Ordinal))
            diagnostics.Add(Error("SDK109", "manifest Type and dtmapi.author.json projectKind must match exactly.", context.ManifestPath));

        ValidateCodeModIdentity(context, diagnostics);

        if (!ManagedPackageReferences.UsesContract(context)) ValidateDependencies(manifest.Dependencies, context.ManifestPath, diagnostics);
        else ManagedPackageReferences.Resolve(context);
        if (context.Kind == AuthorProjectKind.CodeMod)
            ValidateCodeMod(context, diagnostics);
        else
            ValidateContentPack(context, diagnostics);
        ValidatePublishMetadata(author.Publish, context.AuthorProjectPath, diagnostics);
        if (context.Kind == AuthorProjectKind.CodeMod) _ = StandardBuildIntegration.ProjectPath(context);
    }

    private static void ValidateAuthorProjectVersion(AuthorProjectContext context, List<AuthorDiagnostic> diagnostics)
    {
        AuthorProject author = context.AuthorProject;
        if (author.TargetRuntimeVersion.Length != 0)
            diagnostics.Add(Error("SDK108", "schemaVersion 4 requires targetDtmApiVersion and does not accept targetRuntimeVersion.", context.AuthorProjectPath));
        if (!AuthorApiTargetCatalog.Current.TryGet(author.TargetDtmApiVersion, out var target) || target.State != "available")
            diagnostics.Add(Error("SDK108", "API target '" + author.TargetDtmApiVersion + "' is unknown or not yet available. Select an available target from target-catalog.json.", context.AuthorProjectPath));
        if (context.CodeModKind == AuthorCodeModKind.Advanced && !NativeProjectReferences.UsesContract(context) && author.TargetDtmApiVersion != AuthorSdkContract.TargetRuntimeVersion)
            diagnostics.Add(Error("SDK108", "The existing Advanced reference policy remains bound to API target 0.5.5.", context.AuthorProjectPath));
    }
    private static void ValidateCodeModIdentity(AuthorProjectContext context, List<AuthorDiagnostic> diagnostics)
    {
        AuthorProject author = context.AuthorProject;
        bool authorDeclaresKind = !string.IsNullOrWhiteSpace(author.CodeModKind);
        AuthorCodeModKind authorKind = AuthorCodeModKind.Strict;
        if (authorDeclaresKind && !Enum.TryParse(author.CodeModKind, ignoreCase: false, out authorKind))
        {
            diagnostics.Add(Error("SDK111", "dtmapi.author.json codeModKind must be exactly Strict or Advanced.", context.AuthorProjectPath));
            return;
        }

        if (context.Kind == AuthorProjectKind.ContentPack)
        {
            if (context.ManifestDeclaresCodeModKind || authorDeclaresKind || author.Advanced != null)
                diagnostics.Add(Error("SDK112", "ContentPack must not declare CodeModKind, codeModKind, or Advanced reference settings.", context.ManifestPath));
            return;
        }
        if (authorKind != context.CodeModKind)
            diagnostics.Add(Error("SDK113", "manifest CodeModKind and dtmapi.author.json codeModKind must resolve to the same Strict or Advanced identity.", context.AuthorProjectPath));
        if (context.CodeModKind == AuthorCodeModKind.Advanced)
        {
            if (NativeProjectReferences.UsesContract(context))
            {
                NativeProjectReferences.ValidateSettings(context);
                return;
            }
            if (!authorDeclaresKind)
                diagnostics.Add(Error("SDK114", "Advanced CodeMod requires explicit codeModKind Advanced author intent.", context.AuthorProjectPath));
            ValidateAdvancedSettings(context, diagnostics);
        }
        else if (author.Advanced != null)
        {
            diagnostics.Add(Error("SDK115", "Strict CodeMod must not carry Advanced reference settings.", context.AuthorProjectPath));
        }
    }

    private static void ValidateAdvancedSettings(AuthorProjectContext context, List<AuthorDiagnostic> diagnostics)
    {
        AdvancedAuthorProject? advanced = context.AuthorProject.Advanced;
        if (advanced == null)
        {
            diagnostics.Add(Error("SDK162", "Advanced CodeMod requires tracked advanced reference settings.", context.AuthorProjectPath));
            return;
        }
        try
        {
            AdvancedReferencePolicyRegistration registration = AdvancedReferenceAssets.ResolveRegistration(advanced.ReferencePolicyId);
            AdvancedReferenceAssets.RequireRegisteredUniqueId(registration, context.Manifest.UniqueID);
            if (CompareNumericVersion(context.Manifest.MinimumDTMApiVersion, registration.MinimumDtmApiVersion) < 0)
            {
                diagnostics.Add(Error(
                    "SDK166",
                    "Advanced reference policy " + registration.PolicyId + " requires MinimumDTMApiVersion " +
                    registration.MinimumDtmApiVersion + " or later; the manifest declares " +
                    context.Manifest.MinimumDTMApiVersion + ".",
                    context.ManifestPath));
            }
            (AdvancedReferencePolicy policy, _) = AdvancedReferenceAssets.LoadTrackedPolicy(registration.PolicyId);
            string[] actual = advanced.References.OrderBy(value => value, StringComparer.Ordinal).ToArray();
            string[] expected = policy.References.Select(reference => reference.AssemblyName).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            if (actual.Length != actual.Distinct(StringComparer.Ordinal).Count() || !actual.SequenceEqual(expected, StringComparer.Ordinal))
                diagnostics.Add(Error("SDK164", "Advanced references must exactly match the tracked policy: " + string.Join(", ", expected) + ".", context.AuthorProjectPath));
        }
        catch (InvalidDataException ex)
        {
            diagnostics.Add(Error("SDK165", ex.Message, context.AuthorProjectPath));
        }
    }

    private static void ValidateCodeMod(AuthorProjectContext context, List<AuthorDiagnostic> diagnostics)
    {
        RuntimeManifest manifest = context.Manifest;
        if (string.IsNullOrWhiteSpace(manifest.EntryDll) || !manifest.EntryDll.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) || Path.GetFileName(manifest.EntryDll) != manifest.EntryDll)
            diagnostics.Add(Error("SDK120", "CodeMod EntryDll must be one relative DLL filename with no directory or traversal.", context.ManifestPath));
        if (string.IsNullOrWhiteSpace(manifest.EntryType) || !manifest.EntryType.Contains('.', StringComparison.Ordinal))
            diagnostics.Add(Error("SDK121", "CodeMod EntryType must be a namespace-qualified type name.", context.ManifestPath));
    }

    private static void ValidateContentPack(AuthorProjectContext context, List<AuthorDiagnostic> diagnostics)
    {
        if (!string.IsNullOrWhiteSpace(context.Manifest.EntryDll) || !string.IsNullOrWhiteSpace(context.Manifest.EntryType))
            diagnostics.Add(Error("SDK130", "ContentPack must not declare EntryDll or EntryType.", context.ManifestPath));
        string[] dlls = Directory.EnumerateFiles(context.RootPath, "*.dll", SearchOption.AllDirectories)
            .Where(path => !PathSafety.IsBuildPath(PathSafety.RelativePath(context.RootPath, path)))
            .ToArray();
        foreach (string dll in dlls)
            diagnostics.Add(Error("SDK131", "ContentPack payloads cannot contain DLLs.", dll));
        if (!Directory.Exists(context.ContentPath))
            diagnostics.Add(Error("SDK132", "ContentPack contentDirectory does not exist.", context.ContentPath));
    }

    private static void ValidateDependencies(IEnumerable<RuntimeManifestDependency>? dependencies, string path, List<AuthorDiagnostic> diagnostics)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (RuntimeManifestDependency dependency in dependencies ?? Enumerable.Empty<RuntimeManifestDependency>())
        {
            if (!UniqueIdPattern.IsMatch(dependency.UniqueID ?? string.Empty))
                diagnostics.Add(Error("SDK140", "Dependency UniqueID is invalid.", path));
            else if (!seen.Add(dependency.UniqueID ?? string.Empty))
                diagnostics.Add(Error("SDK141", "Dependency UniqueID is duplicated: " + dependency.UniqueID, path));
            if (!string.IsNullOrWhiteSpace(dependency.MinimumVersion) && !VersionPattern.IsMatch(dependency.MinimumVersion))
                diagnostics.Add(Error("SDK142", "Dependency MinimumVersion is invalid for " + dependency.UniqueID + ".", path));
        }
    }

    private static void ValidatePublishMetadata(AuthorPublishMetadata? publish, string path, List<AuthorDiagnostic> diagnostics)
    {
        if (publish == null)
        {
            diagnostics.Add(Error("SDK150", "dtmapi.author.json publish metadata is required.", path));
            return;
        }
        if (publish.Tags.Any(tag => string.IsNullOrWhiteSpace(tag)))
            diagnostics.Add(Error("SDK151", "publish.tags cannot contain blank values.", path));
        if (publish.Tags.Distinct(StringComparer.OrdinalIgnoreCase).Count() != publish.Tags.Count)
            diagnostics.Add(Error("SDK152", "publish.tags cannot contain duplicates.", path));
    }

    private static void Required(string? value, string field, string path, List<AuthorDiagnostic> diagnostics)
    {
        if (string.IsNullOrWhiteSpace(value))
            diagnostics.Add(Error("SDK101", "manifest.json field " + field + " is required.", path));
    }

    private static int CompareNumericVersion(string left, string right)
    {
        string[] a = left.Split('-', 2)[0].Split('.');
        string[] b = right.Split('-', 2)[0].Split('.');
        for (int i = 0; i < 3; i++)
        {
            int comparison = a[i].Length.CompareTo(b[i].Length);
            if (comparison == 0)
                comparison = string.CompareOrdinal(a[i], b[i]);
            if (comparison != 0)
                return comparison;
        }
        return 0;
    }

    private static void RejectDuplicateTopLevelProperties(string path)
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path, Encoding.UTF8));
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException(Path.GetFileName(path) + " must contain a JSON object.");
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (JsonProperty property in document.RootElement.EnumerateObject())
        {
            if (!names.Add(property.Name))
                throw new InvalidDataException(Path.GetFileName(path) + " has a duplicate top-level property: " + property.Name);
        }
    }

    private static AuthorDiagnostic Error(string code, string message, string path) => new() { Code = code, Severity = DiagnosticSeverity.Error, Message = message, Path = path };
    private static AuthorDiagnostic Warning(string code, string message, string path) => new() { Code = code, Severity = DiagnosticSeverity.Warning, Message = message, Path = path };
    private static AuthorDiagnostic Info(string code, string message) => new() { Code = code, Severity = DiagnosticSeverity.Info, Message = message };
}
