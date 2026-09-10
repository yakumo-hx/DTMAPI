using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DTMAPI.AuthorSdk;

internal sealed record ProjectCompilerSettings(string[] Constants, bool Documentation, LanguageVersion Language, bool Checked, NullableContextOptions Nullable)
{
    internal static bool IsOption(string name) => name is "DefineConstants" or "GenerateDocumentationFile";
    internal static bool Condition(string? condition, string configuration)
    {
        if (string.IsNullOrWhiteSpace(condition)) return true;
        var match = Regex.Match(condition, "^\\s*'\\$\\(Configuration\\)'\\s*(==|!=)\\s*'(Debug|Release)'\\s*$", RegexOptions.CultureInvariant);
        if (!match.Success) throw new InvalidDataException("Unsupported configuration condition: " + condition + "; use '$(Configuration)' ==/!= 'Debug' or 'Release'.");
        bool equal = configuration == match.Groups[2].Value; return match.Groups[1].Value == "==" ? equal : !equal;
    }
    internal static string[] ConstantsFrom(string value, IEnumerable<string> previous)
    {
        string expanded = value.Replace("$(DefineConstants)", string.Join(";", previous), StringComparison.Ordinal);
        string[] symbols = expanded.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => s.Length != 0).ToArray();
        if (symbols.Any(s => !SyntaxFacts.IsValidIdentifier(s) || s.StartsWith('@') || s is "true" or "false") || symbols.Length > 256)
            throw new InvalidDataException("DefineConstants must contain C# identifiers separated by semicolons; only $(DefineConstants) self-append is supported.");
        return symbols.Distinct(StringComparer.Ordinal).OrderBy(s => s, StringComparer.Ordinal).ToArray();
    }
    internal static ProjectCompilerSettings Read(AuthorProjectContext context, string configuration)
    {
        string[] constants = configuration == "Debug" ? new[] { "DEBUG", "TRACE" } : new[] { "TRACE" };
        bool documentation = false;
        if (context.OrdinaryLibrary != null) constants = new[] { "TRACE" };
        foreach (string path in context.OrdinaryLibrary == null ? Directory.EnumerateFiles(context.RootPath, "*.csproj").OrderBy(p => p, StringComparer.Ordinal).ToArray() : new[] { context.OrdinaryLibrary.ProjectPath })
        {
            foreach (var group in XDocument.Load(path).Root!.Elements("PropertyGroup"))
            foreach (var property in group.Elements().Where(p => IsOption(p.Name.LocalName)))
            {
                try
                {
                    bool selected = Condition((string?)group.Attribute("Condition"), configuration) & Condition((string?)property.Attribute("Condition"), configuration);
                    if (property.Name == "DefineConstants")
                    {
                        var next = ConstantsFrom(property.Value, constants); if (selected) constants = next;
                    }
                    else
                    {
                        if (!bool.TryParse(property.Value.Trim(), out bool next)) throw new InvalidDataException("GenerateDocumentationFile requires true or false.");
                        if (selected) documentation = next;
                    }
                }
                catch (InvalidDataException ex) { throw new InvalidDataException(path + " / " + property.Name + ": " + ex.Message, ex); }
            }
        }
        // Microsoft.NET.Sdk appends configuration/framework symbols after evaluating
        // the project. A literal DefineConstants replacement must not remove them.
        if (context.OrdinaryLibrary != null)
            constants = constants.Concat(new[] { configuration.ToUpperInvariant(), "NETSTANDARD", "NETSTANDARD2_0", "NETSTANDARD2_0_OR_GREATER" })
                .Concat(Enumerable.Range(0, 7).Select(i => "NETSTANDARD1_" + i + "_OR_GREATER")).ToArray();
        return new ProjectCompilerSettings(constants.Distinct().OrderBy(s => s, StringComparer.Ordinal).ToArray(), documentation,
            context.OrdinaryLibrary?.Language ?? LanguageVersion.CSharp12, context.OrdinaryLibrary?.Checked ?? true,
            context.OrdinaryLibrary?.Nullable ?? NullableContextOptions.Enable);
    }
}
