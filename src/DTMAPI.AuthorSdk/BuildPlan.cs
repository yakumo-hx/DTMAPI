using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using DTMAPI.Authoring.Contracts;
using System.Text;

namespace DTMAPI.AuthorSdk;

// Internal compiler authority. IDE Build delegates to the CLI which creates this plan.
internal sealed class BuildPlan
{
    public required string Configuration { get; init; }
    public required string OutputRoot { get; init; }
    public required CodeModBuildInput Input { get; init; }
    public required string AssemblyInfo { get; init; }
    public required IReadOnlyList<MetadataReference> References { get; init; }
    public required ProjectCompilerSettings CompilerSettings { get; init; }
    public IReadOnlyDictionary<MetadataReference, string> InMemoryReferenceIdentities { get; init; } = new Dictionary<MetadataReference, string>();
    public string[] Constants => CompilerSettings.Constants;
    public CSharpParseOptions ParseOptions => new(CompilerSettings.Language, CompilerSettings.Documentation ? DocumentationMode.Diagnose : DocumentationMode.Parse, preprocessorSymbols: Constants);
    public CSharpCompilationOptions Options => new(OutputKind.DynamicallyLinkedLibrary,
        optimizationLevel: Configuration == "Debug" ? OptimizationLevel.Debug : OptimizationLevel.Release,
        checkOverflow: CompilerSettings.Checked, allowUnsafe: false, platform: Platform.AnyCpu, warningLevel: 4,
        deterministic: true, nullableContextOptions: CompilerSettings.Nullable, concurrentBuild: false);
    public string OptionsIdentity => CompilerSettings.Language + ";" + Configuration + ";" + string.Join(",", Constants) + ";netstandard2.0;AnyCpu;checked=" + CompilerSettings.Checked + ";unsafe=false;nullable=" + CompilerSettings.Nullable + ";warnings=4;deterministic;concurrent=false;documentation=" + (CompilerSettings.Documentation ? "xml" : "parse") + ";PortablePdb";

    public static string NormalizeConfiguration(string configuration) => configuration.ToLowerInvariant() switch
    {
        "debug" => "Debug", "release" or "" => "Release",
        _ => throw new InvalidDataException("Unsupported configuration: " + configuration + ". Use Debug or Release.")
    };

    public CSharpCompilation CreateCompilation(string assemblyName)
    {
        var trees = Input.Sources.Select(source => CSharpSyntaxTree.ParseText(source.Text, ParseOptions, source.Path, Encoding.UTF8)).ToList<SyntaxTree>();
        trees.Add(CSharpSyntaxTree.ParseText(AssemblyInfo, ParseOptions, "DTMAPI.Author.GeneratedAssemblyInfo.cs", Encoding.UTF8));
        return CSharpCompilation.Create(assemblyName, trees, References, Options);
    }

    public void AddReport(CommandReport report)
    {
        report.Values["configuration"] = Configuration;
        report.Values["compilerOptions"] = OptionsIdentity;
        report.Values["sourcePaths"] = string.Join(";", Input.Sources.Select(source => source.Path));
        report.Values["referenceIdentities"] = string.Join(";", References.OfType<PortableExecutableReference>()
            .Select(reference => reference.FilePath != null ? Path.GetFileName(reference.FilePath) + "=" + PathSafety.Sha256File(reference.FilePath)
                : InMemoryReferenceIdentities.TryGetValue(reference, out string? identity) ? identity : throw new InvalidDataException("BuildPlan has an unbound in-memory compiler reference.")).OrderBy(value => value, StringComparer.Ordinal));
        Input.AddIdentity(report, AssemblyInfo);
    }
}
