using DTMAPI.Authoring.Contracts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System.Globalization;
using System.Text;

namespace DTMAPI.AuthorSdk;

internal static class CodeModBuilder
{
    public static CommandReport BuildCommand(ParsedCommand command)
    {
        command.RequireOnlyOptions("compatibility-root", "output", "game-root");
        if (command.Positionals.Count != 1)
            throw new CommandLineException("build requires one project directory.");
        return Build(PathSafety.FullPath(command.Positionals[0]), command.Option("compatibility-root"), command.Option("output"), command.Option("game-root"));
    }

    public static CommandReport Build(string root, string compatibilityRoot, string requestedOutput, string gameRoot = "")
    {
        var report = new CommandReport { Command = "build", RootPath = Path.GetFullPath(root) };
        AuthorProjectContext context;
        try
        {
            context = ProjectValidator.LoadValidated(root, report.Diagnostics);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException or InvalidDataException)
        {
            report.Diagnostics.Add(Error("SDK200", ex.Message, report.RootPath));
            return Finish(report);
        }
        if (context.Kind != AuthorProjectKind.CodeMod)
        {
            report.Diagnostics.Add(Error("SDK201", "build is only valid for CodeMod projects; ContentPack has no executable build step.", context.RootPath));
            return Finish(report);
        }
        if (report.Diagnostics.Any(diagnostic => diagnostic.Severity == DTMAPI.Authoring.Contracts.DiagnosticSeverity.Error))
            return Finish(report);

        try
        {
            CompatibilityAssets compatibility = CompatibilityAssets.Resolve(compatibilityRoot);
            ResolvedAdvancedReferenceSet? advancedReferences = context.CodeModKind == AuthorCodeModKind.Advanced
                ? AdvancedReferenceAssets.Resolve(context, gameRoot)
                : null;
            string outputRoot = string.IsNullOrWhiteSpace(requestedOutput)
                ? Path.Combine(context.RootPath, "bin", "dtmapi-author")
                : Path.GetFullPath(requestedOutput, context.RootPath);
            PathSafety.RejectBepInExPluginDestination(outputRoot);
            Directory.CreateDirectory(outputRoot);
            string dllPath = Path.Combine(outputRoot, context.AuthorProject.AssemblyName + ".dll");
            string pdbPath = Path.Combine(outputRoot, context.AuthorProject.AssemblyName + ".pdb");

            string[] sourceFiles = Directory.EnumerateFiles(context.SourcePath, "*.cs", SearchOption.AllDirectories)
                .OrderBy(path => PathSafety.RelativePath(context.SourcePath, path), StringComparer.Ordinal)
                .ToArray();
            var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp12).WithDocumentationMode(DocumentationMode.Parse);
            var syntaxTrees = sourceFiles.Select(path => CSharpSyntaxTree.ParseText(
                File.ReadAllText(path, Encoding.UTF8),
                parseOptions,
                PathSafety.RelativePath(context.SourcePath, path),
                Encoding.UTF8)).ToList<SyntaxTree>();
            syntaxTrees.Add(CSharpSyntaxTree.ParseText(CreateAssemblyInfo(context), parseOptions, "DTMAPI.Author.GeneratedAssemblyInfo.cs", Encoding.UTF8));

            AdvancedCompilationReferenceSet? advancedCompilation = advancedReferences == null
                ? null
                : AdvancedCompilationReferences.Create(compatibility, advancedReferences);
            IEnumerable<MetadataReference> references = advancedCompilation != null
                ? advancedCompilation.References
                : compatibility.ReferencePaths
                    .Append(compatibility.AbstractionsPath)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(path => MetadataReference.CreateFromFile(path));
            var options = new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: OptimizationLevel.Release,
                checkOverflow: true,
                allowUnsafe: false,
                platform: Platform.AnyCpu,
                warningLevel: 4,
                deterministic: true,
                nullableContextOptions: NullableContextOptions.Enable,
                concurrentBuild: false);
            CSharpCompilation compilation = CSharpCompilation.Create(context.AuthorProject.AssemblyName, syntaxTrees, references, options);

            string temporaryDll = dllPath + ".tmp-" + Guid.NewGuid().ToString("N");
            string temporaryPdb = pdbPath + ".tmp-" + Guid.NewGuid().ToString("N");
            try
            {
                EmitResult emit;
                using (FileStream dllStream = new(temporaryDll, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (FileStream pdbStream = new(temporaryPdb, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    emit = compilation.Emit(
                        dllStream,
                        pdbStream,
                        options: new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb, pdbFilePath: Path.GetFileName(pdbPath)));
                    dllStream.Flush(true);
                    pdbStream.Flush(true);
                }
                foreach (Diagnostic diagnostic in emit.Diagnostics.Where(diagnostic => diagnostic.Severity is Microsoft.CodeAnalysis.DiagnosticSeverity.Warning or Microsoft.CodeAnalysis.DiagnosticSeverity.Error).OrderBy(diagnostic => diagnostic.Location.GetLineSpan().Path, StringComparer.Ordinal).ThenBy(diagnostic => diagnostic.Location.GetLineSpan().StartLinePosition.Line))
                {
                    FileLinePositionSpan line = diagnostic.Location.GetLineSpan();
                    string location = line.IsValid ? line.Path + ":" + (line.StartLinePosition.Line + 1).ToString(CultureInfo.InvariantCulture) : context.RootPath;
                    report.Diagnostics.Add(new AuthorDiagnostic
                    {
                        Code = diagnostic.Id,
                        Severity = diagnostic.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error ? DTMAPI.Authoring.Contracts.DiagnosticSeverity.Error : DTMAPI.Authoring.Contracts.DiagnosticSeverity.Warning,
                        Message = diagnostic.GetMessage(CultureInfo.InvariantCulture),
                        Path = location
                    });
                }
                if (!emit.Success)
                    return Finish(report);
                ManagedAssemblyInspector.ValidateCodeMod(
                    temporaryDll,
                    context.CodeModKind,
                    advancedReferences?.Policy.References.Select(reference => reference.AssemblyName) ?? Array.Empty<string>(),
                    context.AuthorProject.AssemblyName);
                File.Move(temporaryDll, dllPath, true);
                File.Move(temporaryPdb, pdbPath, true);
            }
            finally
            {
                if (File.Exists(temporaryDll)) File.Delete(temporaryDll);
                if (File.Exists(temporaryPdb)) File.Delete(temporaryPdb);
            }

            report.OutputPath = dllPath;
            report.Sha256 = PathSafety.Sha256File(dllPath);
            report.FileCount = sourceFiles.Length;
            report.Values["assemblyName"] = context.AuthorProject.AssemblyName;
            report.Values["codeModKind"] = context.CodeModKind.ToString();
            report.Values["targetFramework"] = "netstandard2.0";
            report.Values["compatibilityManifestSha256"] = PathSafety.Sha256File(compatibility.ManifestPath);
            report.Values["abstractionsSha256"] = PathSafety.Sha256File(compatibility.AbstractionsPath);
            report.Values["sourceTreeSha256"] = PathSafety.Sha256Tree(context.SourcePath, sourceFiles);
            if (advancedReferences != null)
            {
                report.Values["referencePolicyId"] = advancedReferences.Policy.PolicyId;
                report.Values["referencePolicyVersion"] = advancedReferences.Policy.PolicyVersion.ToString(CultureInfo.InvariantCulture);
                report.Values["referencePolicySha256"] = advancedReferences.PolicySha256;
                report.Values["gameBuildId"] = advancedReferences.Policy.GameBuildId;
                report.Values["gameAssemblySha256"] = advancedReferences.Policy.GameAssemblySha256.ToLowerInvariant();
                report.Values["harmonyOwner"] = AdvancedReferenceAssets.ExpectedHarmonyOwner(context.Manifest.UniqueID);
                report.Values["compilerReferenceSurfaceSha256"] = advancedCompilation!.SurfaceSha256;
                report.Diagnostics.Insert(0, Info("SDK000", "Advanced CodeMod compiled in-process for netstandard2.0 using the explicit game root, tracked hash-fixed reference policy, and its policy-limited native compiler surface."));
            }
            else
            {
                report.Diagnostics.Insert(0, Info("SDK000", "Strict CodeMod compiled in-process for netstandard2.0 with only the fixed Runtime 0.5.5 compatibility payload."));
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            report.Diagnostics.Add(Error("SDK202", ex.Message, report.RootPath));
        }
        return Finish(report);
    }

    private static string CreateAssemblyInfo(AuthorProjectContext context)
    {
        string numeric = context.Manifest.Version.Split('-', 2)[0];
        if (numeric.Split('.').Length == 3)
            numeric += ".0";
        return "using System.Reflection;\n" +
            "using System.Runtime.Versioning;\n" +
            "[assembly: AssemblyVersion(\"" + numeric + "\")]\n" +
            "[assembly: AssemblyFileVersion(\"" + numeric + "\")]\n" +
            "[assembly: AssemblyInformationalVersion(\"" + Escape(context.Manifest.Version) + "\")]\n" +
            "[assembly: TargetFramework(\".NETStandard,Version=v2.0\", FrameworkDisplayName = \".NET Standard 2.0\")]\n";
    }

    private static string Escape(string value) => value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);

    private static CommandReport Finish(CommandReport report)
    {
        report.Success = report.Diagnostics.All(diagnostic => diagnostic.Severity != DTMAPI.Authoring.Contracts.DiagnosticSeverity.Error);
        return report;
    }

    private static AuthorDiagnostic Error(string code, string message, string path) => new() { Code = code, Severity = DTMAPI.Authoring.Contracts.DiagnosticSeverity.Error, Message = message, Path = path };
    private static AuthorDiagnostic Info(string code, string message) => new() { Code = code, Severity = DTMAPI.Authoring.Contracts.DiagnosticSeverity.Info, Message = message };
}
