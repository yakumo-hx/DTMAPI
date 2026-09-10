using DTMAPI.Authoring.Contracts;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Xml.Linq;

namespace DTMAPI.AuthorSdk;

internal static class ProjectBuildInputs
{
    internal static void Validate(AuthorProjectContext context, List<AuthorDiagnostic> diagnostics)
    {
        if (context.OrdinaryLibrary != null) return; // The ordinary-library adapter validates its complete input subset.
        foreach (string project in Directory.EnumerateFiles(context.RootPath, "*.csproj", SearchOption.TopDirectoryOnly))
        {
            try { ValidateDocument(context, XDocument.Load(project), project, diagnostics); }
            catch (System.Xml.XmlException ex) { Add(diagnostics, project, "SDK180", "Invalid project XML: " + ex.Message); }
        }
        foreach (string filename in new[] { "Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props" })
            if (File.Exists(Path.Combine(context.RootPath, filename)))
                Add(diagnostics, Path.Combine(context.RootPath, filename), "SDK180", "Custom " + filename + " is not consumed by the SDK backend.");
    }

    // A supported projection, not a partial MSBuild evaluator. Every project item
    // outside this grammar is diagnosed instead of silently discarded by the CLI.
    internal static void ValidateDocument(AuthorProjectContext context, XDocument document, string project, List<AuthorDiagnostic> diagnostics)
    {
        void Reject(string message) => Add(diagnostics, project, "SDK180", message);
        XElement? root = document.Root;
        if (root?.Name != "Project" || !OnlyAttributes(root, "Sdk", "Label") ||
            (root.Attribute("Sdk") != null && (string?)root.Attribute("Sdk") != "Microsoft.NET.Sdk"))
        {
            Reject("Unsupported project root/SDK. Use the SDK's Microsoft.NET.Sdk project projection.");
            return;
        }
        string payload = DTMAPI.Internal.Authoring.AuthorApiTargetCatalog.Current.GetAvailable(context.ApiTarget).PayloadPath.Replace('/', '\\');
        string props = "$(DTMAPI_AUTHOR_SDK_ROOT)\\" + payload + "\\DTMAPI.Author.props";
        var expectedTargets = XDocument.Parse(TemplateCreator.BuildProjectText(context, ""))
            .Root!.Elements("Target").ToArray();
        foreach (XElement group in root.Elements())
        {
            switch (group.Name.ToString())
            {
                case "PropertyGroup":
                    bool optionsOnly = group.Elements().All(p => ProjectCompilerSettings.IsOption(p.Name.LocalName));
                    if (!OnlyAttributes(group, optionsOnly ? new[] { "Label", "Condition" } : new[] { "Label" })) Reject("Conditional PropertyGroup supports only DefineConstants/GenerateDocumentationFile.");
                    if (optionsOnly)
                    { try { _ = ProjectCompilerSettings.Condition((string?)group.Attribute("Condition"), "Debug"); } catch (InvalidDataException ex) { Reject(ex.Message); } }
                    foreach (XElement property in group.Elements())
                    {
                        string name = property.Name.ToString();
                        if (property.HasElements || !OnlyAttributes(property, "Condition", "Label") ||
                            !AllowedProperty(context, name, property.Value, (string?)property.Attribute("Condition")))
                            Reject("Unsupported or conflicting project property '" + name + "'. AssemblyName must match the SDK identity; constants/XML allow literals, configuration conditions and DefineConstants self-append. Other options keep their documented subset.");
                    }
                    break;
                case "ItemGroup":
                    if (!OnlyAttributes(group, "Label")) Reject("Conditional or extended ItemGroup is unsupported.");
                    foreach (XElement item in group.Elements())
                    {
                        if (item.Name == "Reference")
                            ValidateReference(context, item, project, diagnostics);
                        else if (context.AuthorProject.Build != null && ProjectGraph.AcceptProjectItem(context, item)) { }
                        else if (item.Name != "Compile" || item.HasElements || !OnlyAttributes(item, "Include", "Label") ||
                            ((string?)item.Attribute("Include"))?.Replace('/', '\\') != TemplateCreator.SourceGlob(context.AuthorProject.SourceDirectory))
                            Reject("Unsupported or undeclared project input '" + item.Name + "'. Declare exact ProjectReference/resource/generated-source inputs in the SDK build settings; arbitrary MSBuild items remain unsupported.");
                    }
                    break;
                case "Import":
                    string import = (string?)group.Attribute("Project") ?? "";
                    bool sdkImport = import is "Sdk.props" or "Sdk.targets";
                    bool supported = !group.HasElements && (sdkImport
                        ? OnlyAttributes(group, "Project", "Sdk", "Label") && (string?)group.Attribute("Sdk") == "Microsoft.NET.Sdk"
                        : import == props && OnlyAttributes(group, "Project", "Condition", "Label") &&
                          (group.Attribute("Condition") == null || (string?)group.Attribute("Condition") == "Exists('" + props + "')"));
                    if (!supported) Reject("Unsupported project import: " + import);
                    break;
                case "Target":
                    XElement? expected = expectedTargets.FirstOrDefault(target => (string?)target.Attribute("Name") == (string?)group.Attribute("Name"));
                    if (expected == null || !XNode.DeepEquals(expected, group))
                        Reject("Custom or modified build targets are unsupported. Keep the SDK template Build delegation unchanged.");
                    break;
                default:
                    Reject("Unsupported project element '" + group.Name + "'. The SDK does not evaluate arbitrary MSBuild inputs.");
                    break;
            }
        }
    }

    private static bool OnlyAttributes(XElement element, params string[] names) =>
        element.Attributes().All(attribute => names.Contains(attribute.Name.ToString(), StringComparer.Ordinal));

    private static bool AllowedProperty(AuthorProjectContext context, string name, string value, string? condition)
    {
        if (ProjectCompilerSettings.IsOption(name))
        {
            try { _ = ProjectCompilerSettings.Condition(condition, "Debug"); if (name == "DefineConstants") _ = ProjectCompilerSettings.ConstantsFrom(value, Array.Empty<string>()); else if (!bool.TryParse(value.Trim(), out _)) return false; return true; }
            catch (InvalidDataException) { return false; }
        }
        if (condition != null) return false;
        return name switch
        {
            "AssemblyName" => value == context.AuthorProject.AssemblyName,
            // C# namespaces are declared by the source; this is IDE metadata, not
            // an assembly/UniqueID authority. Preserve literal author values.
            "RootNamespace" => !value.Contains("$(", StringComparison.Ordinal) && !value.Contains("@(", StringComparison.Ordinal),
            "LangVersion" => value == "12.0",
            "DtmApiUnifiedBuild" => value == "1",
            "CheckForOverflowUnderflow" => value == "true",
            "EnableDefaultCompileItems" => value == "false",
            _ => false
        };
    }

    private static void ValidateReference(AuthorProjectContext context, XElement element, string project, List<AuthorDiagnostic> diagnostics)
    {
        string identity = ((string?)element.Attribute("Include") ?? "").Split(',')[0];
        string hint = element.Elements().FirstOrDefault(child => child.Name.LocalName == "HintPath")?.Value ?? "";
        bool forbidden = IsForbidden(identity);
        if (hint.Length != 0 && !hint.Contains("$(", StringComparison.Ordinal))
        {
            try
            {
                string reference = Path.GetFullPath(hint.Replace('\\', Path.DirectorySeparatorChar), context.RootPath);
                forbidden |= ContainsForbiddenDependency(reference, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            }
            catch (Exception ex) when (ex is IOException or InvalidDataException or BadImageFormatException or UnauthorizedAccessException or ArgumentException)
            { Add(diagnostics, project, "SDK180", "Cannot resolve project reference: " + ex.Message); }
        }
        if (context.CodeModKind == AuthorCodeModKind.Strict && forbidden)
            Add(diagnostics, project, "SDK160", "Strict project references a forbidden host assembly or dependency: " + identity);
        else
            Add(diagnostics, project, "SDK180", "Explicit project references are not supported by this build backend. Use the frozen target references; dependency projects belong to the later SDK dependency model.");
    }

    private static void Add(List<AuthorDiagnostic> diagnostics, string path, string code, string message) =>
        diagnostics.Add(new AuthorDiagnostic { Code = code, Severity = DiagnosticSeverity.Error, Path = path, Message = message });

    private static bool IsForbidden(string name) => name is "Assembly-CSharp" or "0Harmony" ||
        name.StartsWith("UnityEngine", StringComparison.OrdinalIgnoreCase) || name.StartsWith("Unity.", StringComparison.OrdinalIgnoreCase) ||
        name.StartsWith("BepInEx", StringComparison.OrdinalIgnoreCase) || name.StartsWith("Harmony", StringComparison.OrdinalIgnoreCase) ||
        name is "DTMAPI.Core" or "DTMAPI.GameBridge.DolocTown";

    private static bool ContainsForbiddenDependency(string path, HashSet<string> visited)
    {
        if (!visited.Add(path)) return false;
        using var stream = File.OpenRead(path);
        using var pe = new PEReader(stream);
        MetadataReader metadata = pe.GetMetadataReader();
        if (IsForbidden(metadata.GetString(metadata.GetAssemblyDefinition().Name))) return true;
        foreach (AssemblyReferenceHandle handle in metadata.AssemblyReferences)
        {
            string name = metadata.GetString(metadata.GetAssemblyReference(handle).Name);
            if (IsForbidden(name)) return true;
            string dependency = Path.Combine(Path.GetDirectoryName(path)!, name + ".dll");
            if (File.Exists(dependency) && ContainsForbiddenDependency(dependency, visited)) return true;
        }
        return false;
    }
}
