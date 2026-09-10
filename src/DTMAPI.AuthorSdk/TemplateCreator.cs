using DTMAPI.Authoring.Contracts;
using DTMAPI.Internal.Authoring;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace DTMAPI.AuthorSdk;

internal static class TemplateCreator
{
    public static CommandReport MigrateBuild(ParsedCommand command)
    {
        command.RequireOnlyOptions();
        if (command.Positionals.Count != 1) throw new CommandLineException("migrate-build requires one project directory.");
        string root = PathSafety.FullPath(command.Positionals[0]);
        CommandReport report = ProjectValidator.ValidateRoot(root, "migrate-build");
        if (!report.Success) return report;
        var context = ProjectValidator.LoadValidated(root, report.Diagnostics);
        if (context.Kind != AuthorProjectKind.CodeMod) throw new CommandLineException("migrate-build requires a CodeMod.");
        string[] projects = Directory.GetFiles(root, "*.csproj");
        if (projects.Length != 1) throw new CommandLineException("migrate-build requires exactly one existing csproj.");
        string path = projects[0];
        var document = System.Xml.Linq.XDocument.Load(path);
        string rootNamespace = document.Descendants("RootNamespace").LastOrDefault()?.Value ?? ToNamespace(context.Manifest.UniqueID);
        string text = BuildProjectText(context, rootNamespace);
        var projection = XDocument.Parse(text);
        // Preserve author-owned compiler choices and their order/conditions when
        // replacing the build delegation. Repeated migration stays idempotent.
        var optionGroups = new List<XElement>();
        foreach (var group in projection.Root!.Elements("PropertyGroup"))
        {
            var defaults = group.Elements().Where(p => ProjectCompilerSettings.IsOption(p.Name.LocalName) && !document.Descendants(p.Name).Any()).ToArray();
            if (defaults.Length != 0) optionGroups.Add(new XElement("PropertyGroup", group.Attributes().Select(a => new XAttribute(a)), defaults.Select(p => new XElement(p))));
        }
        foreach (var group in document.Root!.Elements("PropertyGroup"))
        {
            var options = group.Elements().Where(p => ProjectCompilerSettings.IsOption(p.Name.LocalName)).ToArray();
            if (options.Length != 0) optionGroups.Add(new XElement("PropertyGroup", group.Attributes().Select(a => new XAttribute(a)), options.Select(p => new XElement(p))));
        }
        projection.Descendants().Where(p => ProjectCompilerSettings.IsOption(p.Name.LocalName)).Remove();
        projection.Root!.Add(optionGroups);
        if (context.AuthorProject.Build is { } build)
        {
            var items = new XElement("ItemGroup");
            foreach (var reference in build.ProjectReferences) items.Add(new XElement("ProjectReference", new XAttribute("Include", reference)));
            foreach (var source in build.GeneratedSourceFiles) items.Add(new XElement("Compile", new XAttribute("Include", source)));
            foreach (var resource in build.EmbeddedResources) items.Add(new XElement("EmbeddedResource", new XAttribute("Include", resource.Path), new XElement("LogicalName", resource.LogicalName)));
            if (items.HasElements) projection.Root!.Add(items);
        }
        text = projection.ToString() + "\n";
        try { ProjectBuildInputs.ValidateDocument(context, XDocument.Parse(text), path, report.Diagnostics); }
        catch (System.Xml.XmlException ex) { report.Diagnostics.Add(new AuthorDiagnostic { Code = "SDK180", Severity = DiagnosticSeverity.Error, Path = path, Message = "Candidate build project is invalid XML: " + ex.Message }); }
        if (report.Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
        {
            report.Success = false;
            return report;
        }
        if (XNode.DeepEquals(document, XDocument.Parse(text))) return report;
        string backup = path + ".pre-unified-" + Guid.NewGuid().ToString("N") + ".bak";
        string candidate = path + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            File.Copy(path, backup, false);
            File.WriteAllText(candidate, text, new UTF8Encoding(false));
            File.Move(candidate, path, true);
        }
        finally { if (File.Exists(candidate)) File.Delete(candidate); }
        report.OutputPath = path;
        report.Values["backupPath"] = backup;
        report.Diagnostics.Add(new AuthorDiagnostic { Code = "SDK000", Severity = DiagnosticSeverity.Info, Message = "Migrated IDE Build to the SDK CLI. Original csproj retained at backupPath; manifest and frozen target unchanged." });
        return report;
    }

    internal static string SourceGlob(string sourceDirectory)
    {
        // MSBuild escapes literals before the recursive glob; XML escaping is separate.
        string value = sourceDirectory.TrimEnd('/', '\\').Replace('/', '\\');
        foreach (char special in new[] { '%', '$', '@', ';', '\'', '(', ')', '*', '?' })
            value = value.Replace(special.ToString(), "%" + ((int)special).ToString("X2"), StringComparison.Ordinal);
        return value + "\\**\\*.cs";
    }

    internal static string BuildProjectText(AuthorProjectContext context, string rootNamespace) =>
        File.ReadAllText(Path.Combine(FindTemplateRoot("codemod"), "__UNIQUE_ID__.csproj.template"), Encoding.UTF8)
            .Replace("{{ASSEMBLY_NAME_XML}}", Xml(context.AuthorProject.AssemblyName), StringComparison.Ordinal)
            .Replace("{{ROOT_NAMESPACE}}", Xml(rootNamespace), StringComparison.Ordinal)
            .Replace("{{SOURCE_GLOB_XML}}", Xml(SourceGlob(context.AuthorProject.SourceDirectory)), StringComparison.Ordinal)
            .Replace("{{PAYLOAD_PATH_XML}}", AuthorApiTargetCatalog.Current.GetAvailable(context.ApiTarget).PayloadPath.Replace('/', '\\'), StringComparison.Ordinal);

    private static string Xml(string value) => System.Security.SecurityElement.Escape(value) ?? string.Empty;

    public static CommandReport Create(ParsedCommand command)
    {
        if (command.Positionals.FirstOrDefault()?.Equals("library", StringComparison.OrdinalIgnoreCase) == true) return ProjectGraph.CreateLibrary(command);
        command.RequireOnlyOptions("id", "name", "author", "description", "version", "code-mod-kind", "api-target", "game-root", "native-references", "harmony-owner");
        if (command.Positionals.Count != 2)
            throw new CommandLineException("new requires a template kind and a new destination directory.");

        string kindText = command.Positionals[0].ToLowerInvariant();
        AuthorProjectKind kind = kindText switch
        {
            "codemod" or "code" => AuthorProjectKind.CodeMod,
            "contentpack" or "content" => AuthorProjectKind.ContentPack,
            _ => throw new CommandLineException("Template kind must be codemod or contentpack.")
        };
        string requestedCodeModKind = command.Option("code-mod-kind", AuthorCodeModKind.Strict.ToString());
        if (!Enum.TryParse(requestedCodeModKind, ignoreCase: false, out AuthorCodeModKind codeModKind))
            throw new CommandLineException("--code-mod-kind must be exactly Strict or Advanced.");
        if (kind == AuthorProjectKind.ContentPack && command.HasOption("code-mod-kind"))
            throw new CommandLineException("ContentPack does not accept --code-mod-kind.");
        AuthorApiTarget apiTarget = SdkApiTargets.ForCommand(command.Option("api-target", AuthorApiTargetCatalog.Current.DefaultTarget));
        bool native = codeModKind == AuthorCodeModKind.Advanced && apiTarget.Capabilities.Contains("native-contract/1", StringComparer.Ordinal);
        if (codeModKind == AuthorCodeModKind.Advanced && !native && apiTarget.ApiTarget != AuthorSdkContract.TargetRuntimeVersion)
            throw new CommandLineException("Existing Advanced reference policies remain bound to API target 0.5.5.");
        if (native && string.IsNullOrWhiteSpace(command.Option("game-root"))) throw new CommandLineException("Open Advanced requires --game-root pointing to the installed game.");
        if (!native && new[] { "game-root", "native-references", "harmony-owner" }.Any(command.HasOption)) throw new CommandLineException("Native creation options require Advanced and a native-contract/1 API target.");
        string destination = PathSafety.FullPath(command.Positionals[1]);
        PathSafety.RejectBepInExPluginDestination(destination);
        if (Directory.Exists(destination) || File.Exists(destination))
            throw new CommandLineException("The new destination already exists. Author SDK 0.1.0 does not adopt or force-overwrite directories: " + destination);

        string uniqueId = Required(command, "id");
        string name = Required(command, "name");
        string author = Required(command, "author");
        string description = command.Option("description", "A DTMAPI " + (kind == AuthorProjectKind.CodeMod ? "code mod." : "content pack."));
        string version = command.Option("version", "0.1.0");
        if (!Regex.IsMatch(uniqueId, @"^[A-Za-z][A-Za-z0-9]*(?:\.[A-Za-z0-9][A-Za-z0-9_-]*)+$", RegexOptions.CultureInvariant))
            throw new CommandLineException("--id must be a dotted, path-safe UniqueID such as Author.ModName.");

        string templateRoot = FindTemplateRoot(kind == AuthorProjectKind.CodeMod ? "codemod" : "contentpack");
        if (!Directory.Exists(templateRoot))
            throw new InvalidOperationException("SDK template payload is missing: " + templateRoot);

        string parent = Directory.GetParent(destination)?.FullName ?? throw new CommandLineException("Destination must have a parent directory.");
        Directory.CreateDirectory(parent);
        string staging = Path.Combine(parent, ".dtmapi-author-new-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(staging);
            var replacements = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["{{API_TARGET_JSON}}"] = JsonSerializer.Serialize(apiTarget.ApiTarget),
                ["{{MINIMUM_RUNTIME_JSON}}"] = JsonSerializer.Serialize(apiTarget.MinimumRuntimeVersion),
                ["{{PAYLOAD_PATH_XML}}"] = apiTarget.PayloadPath.Replace('/', '\\'),
                ["{{UNIQUE_ID_JSON}}"] = JsonSerializer.Serialize(uniqueId),
                ["{{NAME_JSON}}"] = JsonSerializer.Serialize(name),
                ["{{AUTHOR_JSON}}"] = JsonSerializer.Serialize(author),
                ["{{DESCRIPTION_JSON}}"] = JsonSerializer.Serialize(description),
                ["{{VERSION_JSON}}"] = JsonSerializer.Serialize(version),
                ["{{ASSEMBLY_NAME_JSON}}"] = JsonSerializer.Serialize(uniqueId),
                ["{{ASSEMBLY_DLL_JSON}}"] = JsonSerializer.Serialize(uniqueId + ".dll"),
                ["{{ASSEMBLY_NAME_XML}}"] = uniqueId,
                ["{{SOURCE_GLOB_XML}}"] = SourceGlob("src"),
                ["{{ROOT_NAMESPACE}}"] = ToNamespace(uniqueId),
                ["{{ENTRY_TYPE_JSON}}"] = JsonSerializer.Serialize(ToNamespace(uniqueId) + ".ModEntry")
            };

            foreach (string source in Directory.EnumerateFiles(templateRoot, "*", SearchOption.AllDirectories).OrderBy(path => path, StringComparer.Ordinal))
            {
                string relative = Path.GetRelativePath(templateRoot, source);
                if (relative.EndsWith(".template", StringComparison.Ordinal))
                    relative = relative[..^".template".Length];
                relative = relative.Replace("__UNIQUE_ID__", uniqueId);
                string target = Path.Combine(staging, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                string text = File.ReadAllText(source, Encoding.UTF8);
                foreach (KeyValuePair<string, string> pair in replacements)
                    text = text.Replace(pair.Key, pair.Value, StringComparison.Ordinal);
                File.WriteAllText(target, text.Replace("\r\n", "\n", StringComparison.Ordinal), new UTF8Encoding(false));
            }

            if (kind == AuthorProjectKind.CodeMod && codeModKind == AuthorCodeModKind.Advanced)
            {
                if (native) AddNativeIntent(staging, command, uniqueId);
                else AddAdvancedIntent(staging);
            }
            if (apiTarget.Capabilities.Contains("package-dependencies/1", StringComparer.Ordinal))
            {
                string authorPath = Path.Combine(staging, "dtmapi.author.json"), manifestPath = Path.Combine(staging, "manifest.json");
                JsonObject authorDocument = JsonNode.Parse(File.ReadAllText(authorPath))!.AsObject();
                authorDocument["schemaVersion"] = AuthorSdkContract.DependencyAuthorProjectSchemaVersion;
                authorDocument["managedReferences"] = new JsonArray();
                JsonObject manifestDocument = JsonNode.Parse(File.ReadAllText(manifestPath))!.AsObject();
                manifestDocument["DependencyContractVersion"] = 1;
                File.WriteAllText(authorPath, authorDocument.ToJsonString(JsonSupport.StrictToolContract) + "\n", new UTF8Encoding(false));
                File.WriteAllText(manifestPath, manifestDocument.ToJsonString(JsonSupport.RuntimeManifest) + "\n", new UTF8Encoding(false));
            }

            CommandReport validation = ProjectValidator.ValidateRoot(staging, "new");
            if (!validation.Success)
                return validation;
            Directory.Move(staging, destination);

            var report = new CommandReport
            {
                Command = "new",
                Success = true,
                TargetRuntimeVersion = apiTarget.ApiTarget,
                RootPath = destination,
                FileCount = Directory.EnumerateFiles(destination, "*", SearchOption.AllDirectories).Count()
            };
            report.Values["projectKind"] = kind.ToString();
            report.Values["codeModKind"] = kind == AuthorProjectKind.CodeMod ? codeModKind.ToString() : string.Empty;
            report.Values["uniqueID"] = uniqueId;
            report.Values["apiTarget"] = apiTarget.ApiTarget;
            report.Diagnostics.Add(new AuthorDiagnostic
            {
                Code = "SDK000",
                Severity = DiagnosticSeverity.Info,
                Message = "Created a " + kind + " project with frozen API target " + apiTarget.ApiTarget + "."
            });
            return report;
        }
        finally
        {
            if (Directory.Exists(staging))
                Directory.Delete(staging, true);
        }
    }

    private static void AddNativeIntent(string root, ParsedCommand command, string uniqueId)
    {
        string manifestPath = Path.Combine(root, "manifest.json"), authorPath = Path.Combine(root, "dtmapi.author.json");
        var manifest = JsonNode.Parse(File.ReadAllText(manifestPath))!.AsObject();
        var author = JsonNode.Parse(File.ReadAllText(authorPath))!.AsObject();
        manifest["CodeModKind"] = "Advanced";
        manifest["NativeContractVersion"] = (string?)author["targetDtmApiVersion"] == "0.7.0" ? 2 : 1;
        if ((int)manifest["NativeContractVersion"]! == 2) manifest["MinimumDTMApiVersion"] = "0.7.0";
        author["codeModKind"] = "Advanced";
        author["nativeReferences"] = new JsonObject
        {
            ["gameRoot"] = Path.GetFullPath(command.Option("game-root")),
            ["references"] = new JsonArray(command.Option("native-references", "DolocTown_Data/Managed/Assembly-CSharp.dll;DolocTown_Data/Managed/UnityEngine.CoreModule.dll;BepInEx/core/0Harmony.dll").Split(';', StringSplitOptions.RemoveEmptyEntries).Select(path => (JsonNode)JsonValue.Create(path)!).ToArray()),
            ["harmonyOwner"] = command.Option("harmony-owner", uniqueId + ".Native"), ["requiredMembers"] = new JsonArray()
        };
        File.WriteAllText(manifestPath, manifest.ToJsonString(JsonSupport.RuntimeManifest) + "\n", new UTF8Encoding(false));
        File.WriteAllText(authorPath, author.ToJsonString(JsonSupport.Tool) + "\n", new UTF8Encoding(false));
    }

    private static void AddAdvancedIntent(string root)
    {
        string manifestPath = Path.Combine(root, "manifest.json");
        JsonObject manifest = JsonNode.Parse(File.ReadAllText(manifestPath, Encoding.UTF8))!.AsObject();
        string uniqueId = manifest["UniqueID"]?.GetValue<string>() ?? string.Empty;
        AdvancedReferencePolicyRegistration registration;
        try { registration = AdvancedReferenceAssets.ResolveRegistrationForUniqueId(uniqueId); }
        catch (InvalidDataException ex) { throw new CommandLineException(ex.Message); }
        (AdvancedReferencePolicy policy, _) = AdvancedReferenceAssets.LoadTrackedPolicy(registration.PolicyId);
        manifest["CodeModKind"] = AuthorCodeModKind.Advanced.ToString();
        manifest["MinimumDTMApiVersion"] = registration.MinimumDtmApiVersion;
        File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, JsonSupport.RuntimeManifest) + "\n", new UTF8Encoding(false));

        string authorPath = Path.Combine(root, "dtmapi.author.json");
        JsonObject author = JsonNode.Parse(File.ReadAllText(authorPath, Encoding.UTF8))!.AsObject();
        author["codeModKind"] = AuthorCodeModKind.Advanced.ToString();
        var references = new JsonArray();
        foreach (AdvancedReferencePolicyEntry reference in policy.References)
            references.Add(reference.AssemblyName);
        author["advanced"] = new JsonObject
        {
            ["referencePolicyId"] = registration.PolicyId,
            ["references"] = references
        };
        File.WriteAllText(authorPath, JsonSerializer.Serialize(author, JsonSupport.Tool) + "\n", new UTF8Encoding(false));
    }

    private static string Required(ParsedCommand command, string name)
    {
        string value = command.Option(name).Trim();
        if (value.Length == 0)
            throw new CommandLineException("new requires --" + name + ".");
        return value;
    }

    private static string ToNamespace(string uniqueId)
    {
        string value = Regex.Replace(uniqueId, "[^A-Za-z0-9_.]", "_");
        return string.Join(".", value.Split('.').Select(part => char.IsDigit(part[0]) ? "_" + part : part));
    }

    internal static string FindTemplateRoot(string kind)
    {
        string adjacent = Path.Combine(AppContext.BaseDirectory, "templates", kind);
        if (Directory.Exists(adjacent))
            return adjacent;

        DirectoryInfo? current = new DirectoryInfo(Environment.CurrentDirectory);
        while (current != null)
        {
            string sourceTree = Path.Combine(current.FullName, "author-sdk", "templates", kind);
            if (Directory.Exists(sourceTree))
                return sourceTree;
            current = current.Parent;
        }
        return adjacent;
    }

}
