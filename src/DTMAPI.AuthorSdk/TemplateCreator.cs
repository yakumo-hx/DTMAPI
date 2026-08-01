using DTMAPI.Authoring.Contracts;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace DTMAPI.AuthorSdk;

internal static class TemplateCreator
{
    public static CommandReport Create(ParsedCommand command)
    {
        command.RequireOnlyOptions("id", "name", "author", "description", "version", "code-mod-kind");
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
                ["{{UNIQUE_ID_JSON}}"] = JsonSerializer.Serialize(uniqueId),
                ["{{NAME_JSON}}"] = JsonSerializer.Serialize(name),
                ["{{AUTHOR_JSON}}"] = JsonSerializer.Serialize(author),
                ["{{DESCRIPTION_JSON}}"] = JsonSerializer.Serialize(description),
                ["{{VERSION_JSON}}"] = JsonSerializer.Serialize(version),
                ["{{ASSEMBLY_NAME_JSON}}"] = JsonSerializer.Serialize(uniqueId),
                ["{{ASSEMBLY_DLL_JSON}}"] = JsonSerializer.Serialize(uniqueId + ".dll"),
                ["{{ASSEMBLY_NAME_XML}}"] = uniqueId,
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
                AddAdvancedIntent(staging);

            CommandReport validation = ProjectValidator.ValidateRoot(staging, "new");
            if (!validation.Success)
                return validation;
            Directory.Move(staging, destination);

            var report = new CommandReport
            {
                Command = "new",
                Success = true,
                RootPath = destination,
                FileCount = Directory.EnumerateFiles(destination, "*", SearchOption.AllDirectories).Count()
            };
            report.Values["projectKind"] = kind.ToString();
            report.Values["codeModKind"] = kind == AuthorProjectKind.CodeMod ? codeModKind.ToString() : string.Empty;
            report.Values["uniqueID"] = uniqueId;
            report.Diagnostics.Add(new AuthorDiagnostic
            {
                Code = "SDK000",
                Severity = DiagnosticSeverity.Info,
                Message = "Created a Runtime 0.5.5 " + kind + " project in a new directory."
            });
            return report;
        }
        finally
        {
            if (Directory.Exists(staging))
                Directory.Delete(staging, true);
        }
    }

    private static void AddAdvancedIntent(string root)
    {
        string manifestPath = Path.Combine(root, "manifest.json");
        JsonObject manifest = JsonNode.Parse(File.ReadAllText(manifestPath, Encoding.UTF8))!.AsObject();
        string uniqueId = manifest["UniqueID"]?.GetValue<string>() ?? string.Empty;
        AdvancedReferencePolicyRegistration registration = AdvancedReferenceAssets.ResolveRegistrationForUniqueId(uniqueId);
        (AdvancedReferencePolicy policy, _) = AdvancedReferenceAssets.LoadTrackedPolicy(registration.PolicyId);
        manifest["CodeModKind"] = AuthorCodeModKind.Advanced.ToString();
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

    private static string FindTemplateRoot(string kind)
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
