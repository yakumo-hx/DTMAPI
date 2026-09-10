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
        command.RequireOnlyOptions("compatibility-root", "output", "game-root", "configuration");
        if (command.Positionals.Count != 1)
            throw new CommandLineException("build requires one project directory.");
        return Build(PathSafety.FullPath(command.Positionals[0]), command.Option("compatibility-root"), command.Option("output"), command.Option("game-root"), command.Option("configuration", "Release"));
    }

    public static CommandReport Build(string root, string compatibilityRoot, string requestedOutput, string gameRoot = "", string configuration = "Release", AuthorProjectContext? suppliedContext = null, bool singleNode = false)
    {
        var report = new CommandReport { Command = "build", RootPath = Path.GetFullPath(root) };
        AuthorProjectContext context;
        try
        {
            context = suppliedContext ?? ProjectValidator.LoadValidated(root, report.Diagnostics);
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
        report.TargetRuntimeVersion = context.ApiTarget;
        report.Values["apiTarget"] = context.ApiTarget;

        try
        {
            configuration = BuildPlan.NormalizeConfiguration(configuration);
            if (!singleNode && context.AuthorProject.Build != null)
            {
                IReadOnlyList<AuthorProjectContext> graph = ProjectGraph.Load(context, report.Diagnostics);
                if (report.Diagnostics.Any(d => d.Severity == DTMAPI.Authoring.Contracts.DiagnosticSeverity.Error)) return Finish(report);
                string intermediate = Path.Combine(context.RootPath, "obj", "dtmapi-author", "project-build", Guid.NewGuid().ToString("N"));
                var products = new Dictionary<string, ManagedPackageInput>(StringComparer.OrdinalIgnoreCase);
                var identities = new SortedDictionary<string, string>(StringComparer.Ordinal);
                foreach (var node in graph)
                {
                    foreach (var dependency in ProjectGraph.DirectDependencies(node, graph))
                    {
                        ProjectGraph.AddLibraries(node, ManagedPackageReferences.Resolve(dependency).Append(products[dependency.RootPath]));
                    }
                    node.GraphPrepared = true;
                    node.BuildSnapshot ??= ProjectGraph.Snapshot(node);
                    if (ReferenceEquals(node, context)) break;
                    CommandReport child = Build(node.RootPath, compatibilityRoot, Path.Combine(intermediate, node.AuthorProject.AssemblyName), "", configuration, node, true);
                    report.Diagnostics.AddRange(child.Diagnostics.Where(d => d.Severity != DTMAPI.Authoring.Contracts.DiagnosticSeverity.Info));
                    if (!child.Success) return Finish(report);
                    products.Add(node.RootPath, ProjectGraph.AsLibrary(node, child.OutputPath));
                    identities.Add(node.AuthorProject.AssemblyName, child.Values["buildInputSha256"]);
                    context.GraphContent.AddRange(node.BuildSnapshot.Content);
                }
                report.Values["projectGraphInputs"] = System.Text.Json.JsonSerializer.Serialize(identities);
            }
            context.GraphPrepared = true;
            CompatibilityAssets compatibility = CompatibilityAssets.Resolve(compatibilityRoot, context.ApiTarget);
            NativeProjectInput[]? nativeReferences = NativeProjectReferences.UsesContract(context) ? NativeProjectReferences.Resolve(context, gameRoot) : null;
            ResolvedAdvancedReferenceSet? advancedReferences = context.CodeModKind == AuthorCodeModKind.Advanced && nativeReferences == null
                ? AdvancedReferenceAssets.Resolve(context, gameRoot)
                : null;
            string outputRoot = string.IsNullOrWhiteSpace(requestedOutput)
                ? Path.Combine(context.RootPath, "bin", "dtmapi-author", configuration)
                : Path.GetFullPath(requestedOutput, context.RootPath);
            PathSafety.RejectBepInExPluginDestination(outputRoot);
            Directory.CreateDirectory(outputRoot);
            string dllPath = Path.Combine(outputRoot, context.AuthorProject.AssemblyName + ".dll");
            string pdbPath = Path.Combine(outputRoot, context.AuthorProject.AssemblyName + ".pdb");
            string xmlPath = Path.ChangeExtension(dllPath, ".xml");

            ProjectGraph.Inputs snapshot = context.BuildSnapshot ??= ProjectGraph.Snapshot(context);
            CodeModBuildInput input = snapshot.Sources;
            string assemblyInfo = CreateAssemblyInfo(context);
            AdvancedCompilationReferenceSet? advancedCompilation = advancedReferences == null
                ? null
                : AdvancedCompilationReferences.Create(compatibility, advancedReferences);
            IEnumerable<MetadataReference> references = advancedCompilation != null
                ? advancedCompilation.References
                : compatibility.ReferencePaths
                    .Concat(context.OrdinaryLibrary == null ? new[] { compatibility.AbstractionsPath } : Array.Empty<string>())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(path => MetadataReference.CreateFromFile(path));
            if (ManagedPackageReferences.UsesContract(context))
                references = references.Concat(ManagedPackageReferences.Resolve(context).Select(input => MetadataReference.CreateFromFile(input.SourcePath)));
            NativeCompilerInputs? nativeCompiler = nativeReferences == null ? null : NativeProjectReferences.CompilerPaths(context, nativeReferences);
            if (nativeCompiler != null)
                references = references.Concat(nativeCompiler.ReferencePaths.Select(path => MetadataReference.CreateFromFile(path)));
            var plan = new BuildPlan { Configuration = configuration, OutputRoot = outputRoot, Input = input, AssemblyInfo = assemblyInfo, References = references.ToArray(),
                CompilerSettings = ProjectCompilerSettings.Read(context, configuration),
                InMemoryReferenceIdentities = advancedCompilation?.InMemoryReferenceIdentities ?? new Dictionary<MetadataReference, string>() };
            CSharpCompilation compilation = plan.CreateCompilation(context.AuthorProject.AssemblyName);
            report.Diagnostics.AddRange(SynchronousCallbackInspector.Inspect(compilation));
            if (report.Diagnostics.Any(diagnostic => diagnostic.Code == "SDK203")) return Finish(report);
            report.Values["boundAssemblyReferences"] = string.Join(";", compilation.GetUsedAssemblyReferences()
                .Select(reference => compilation.GetAssemblyOrModuleSymbol(reference)?.ToDisplayString() ?? "unresolved")
                .OrderBy(value => value, StringComparer.Ordinal));

            string temporaryDll = dllPath + ".tmp-" + Guid.NewGuid().ToString("N");
            string temporaryPdb = pdbPath + ".tmp-" + Guid.NewGuid().ToString("N");
            string temporaryXml = xmlPath + ".tmp-" + Guid.NewGuid().ToString("N");
            try
            {
                EmitResult emit;
                using (FileStream dllStream = new(temporaryDll, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (FileStream pdbStream = new(temporaryPdb, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (FileStream? xmlStream = plan.CompilerSettings.Documentation ? new FileStream(temporaryXml, FileMode.CreateNew, FileAccess.Write, FileShare.None) : null)
                {
                    emit = compilation.Emit(
                        dllStream,
                        pdbStream,
                        xmlDocumentationStream: xmlStream,
                        manifestResources: snapshot.Resources,
                        options: new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb, pdbFilePath: Path.GetFileName(pdbPath)));
                    dllStream.Flush(true);
                    pdbStream.Flush(true);
                    xmlStream?.Flush(true);
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
                if (nativeReferences != null)
                {
                    _ = NativeProjectReferences.RequiredMembers(context,
                        new[] { (File.ReadAllBytes(temporaryDll), (byte[]?)File.ReadAllBytes(temporaryPdb)) }.Concat(ManagedPackageReferences.Resolve(context).Select(l => (File.ReadAllBytes(l.SourcePath), (byte[]?)null))), nativeReferences);
                    if (NativeProjectReferences.CompilerPaths(context, NativeProjectReferences.Resolve(context, gameRoot)).InputSha256 != nativeCompiler!.InputSha256)
                        throw new InvalidDataException("native-metadata-input-changed: Host changed during build.");
                    if (ProjectGraph.Snapshot(context).Identity != snapshot.Identity)
                        throw new InvalidDataException("native-author-input-changed: Source/resources changed during build; retry with stable inputs.");
                }
                if (ManagedPackageReferences.UsesContract(context)) ManagedPackageReferences.ValidateCompiledEntry(context, temporaryDll);
                else ManagedAssemblyInspector.ValidateCodeMod(
                    temporaryDll,
                    context.CodeModKind,
                    advancedReferences?.Policy.References.Select(reference => reference.AssemblyName) ?? Array.Empty<string>(),
                    context.AuthorProject.AssemblyName);
                PublishOutputs(new[] { (temporaryDll, dllPath), (temporaryPdb, pdbPath), (plan.CompilerSettings.Documentation ? temporaryXml : null, xmlPath) });
                foreach (var file in snapshot.Content.Concat(context.GraphContent))
                {
                    string destination = PathSafety.ResolveUnderRoot(outputRoot, file.Name, "content output");
                    ProjectGraph.RequireUnder(outputRoot, destination);
                    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                    File.WriteAllBytes(destination, file.Bytes);
                }
                foreach (var library in context.BuiltLibraries)
                {
                    string destination = PathSafety.ResolveUnderRoot(outputRoot, library.Record.Path, "library output");
                    ProjectGraph.RequireUnder(outputRoot, destination);
                    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                    File.Copy(library.SourcePath, destination, true);
                    foreach (string extension in new[] { ".xml", ".pdb" })
                        if (File.Exists(Path.ChangeExtension(library.SourcePath, extension))) File.Copy(Path.ChangeExtension(library.SourcePath, extension), Path.ChangeExtension(destination, extension), true);
                }
            }
            finally
            {
                if (File.Exists(temporaryDll)) File.Delete(temporaryDll);
                if (File.Exists(temporaryPdb)) File.Delete(temporaryPdb);
                if (File.Exists(temporaryXml)) File.Delete(temporaryXml);
            }

            report.OutputPath = dllPath;
            report.Sha256 = PathSafety.Sha256File(dllPath);
            report.FileCount = input.Sources.Length;
            report.Values["assemblyName"] = context.AuthorProject.AssemblyName;
            report.Values["codeModKind"] = context.CodeModKind.ToString();
            report.Values["targetFramework"] = "netstandard2.0";
            report.Values["compatibilityManifestSha256"] = PathSafety.Sha256File(compatibility.ManifestPath);
            report.Values["abstractionsSha256"] = PathSafety.Sha256File(compatibility.AbstractionsPath);
            report.Values["sourceTreeSha256"] = input.SourceTreeSha256;
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
            else if (nativeReferences != null)
            {
                report.Values["nativeContractVersion"] = context.Manifest.NativeContractVersion!.Value.ToString(CultureInfo.InvariantCulture);
                report.Values["harmonyOwner"] = context.AuthorProject.NativeReferences!.HarmonyOwner;
                report.Values["nativeReferenceInputSha256"] = nativeCompiler!.InputSha256;
                report.Diagnostics.Insert(0, Info("SDK000", "Advanced CodeMod compiled for netstandard2.0 from explicit installed host metadata; reference surfaces remain local."));
            }
            else
            {
                report.Diagnostics.Insert(0, Info("SDK000", "Strict CodeMod compiled in-process for netstandard2.0 with only the frozen API target " + context.ApiTarget + " compatibility payload."));
            }
            if (context.AuthorProject.Build != null) report.Values["projectResourceInputsSha256"] = snapshot.Identity;
            if (context.AuthorProject.Build?.Restore is { Packages.Count: > 0 } restore)
                report.Values["restoreLockSha256"] = PathSafety.Sha256File(PathSafety.ResolveUnderRoot(context.RootPath, restore.LockFile, "lockFile"));
            plan.AddReport(report);
            report.Values["symbolsPath"] = pdbPath;
            report.Values["symbolsSha256"] = PathSafety.Sha256File(pdbPath);
            if (plan.CompilerSettings.Documentation) { report.Values["documentationPath"] = xmlPath; report.Values["documentationSha256"] = PathSafety.Sha256File(xmlPath); }
            Finish(report);
            File.WriteAllText(dllPath + ".build.json", JsonSupport.SerializeTool(report), new UTF8Encoding(false));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            report.Diagnostics.Add(Error("SDK202", ex.Message, report.RootPath));
        }
        return Finish(report);
    }

    private static string CreateAssemblyInfo(AuthorProjectContext context)
    {
        string numeric = context.Manifest.Version.Split(new[] { '-', '+' }, 2)[0];
        if (numeric.Split('.').Length == 3)
            numeric += ".0";
        string assemblyVersion = context.OrdinaryLibrary?.AssemblyVersion ?? numeric;
        string fileVersion = context.OrdinaryLibrary?.FileVersion ?? numeric;
        return "using System.Reflection;\n" +
            "using System.Runtime.Versioning;\n" +
            "[assembly: AssemblyVersion(\"" + assemblyVersion + "\")]\n" +
            "[assembly: AssemblyFileVersion(\"" + fileVersion + "\")]\n" +
            "[assembly: AssemblyInformationalVersion(\"" + Escape(context.Manifest.Version) + "\")]\n" +
            "[assembly: TargetFramework(\".NETStandard,Version=v2.0\", FrameworkDisplayName = \".NET Standard 2.0\")]\n";
    }

    private static void PublishOutputs(IEnumerable<(string? Temporary, string Destination)> files)
    {
        var backups = new List<(string Destination, string? Backup)>();
        try
        {
            // Move every old companion out first: a locked XML cannot leave a new DLL
            // paired with old documentation. Rollback restores the exact old files.
            foreach (var file in files)
            {
                string? backup = File.Exists(file.Destination) ? file.Destination + ".tmp-previous-" + Guid.NewGuid().ToString("N") : null;
                if (backup != null) File.Move(file.Destination, backup);
                backups.Add((file.Destination, backup));
            }
            foreach (var file in files) if (file.Temporary != null) File.Move(file.Temporary, file.Destination);
        }
        catch
        {
            foreach (var previous in backups.AsEnumerable().Reverse())
            {
                if (File.Exists(previous.Destination)) File.Delete(previous.Destination);
                if (previous.Backup != null) File.Move(previous.Backup, previous.Destination);
            }
            throw;
        }
        foreach (var previous in backups) if (previous.Backup != null) File.Delete(previous.Backup);
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
