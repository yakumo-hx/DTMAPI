using DTMAPI.Authoring.Contracts;
using DTMAPI.Internal.Authoring;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace DTMAPI.AuthorSdk;

// MSBuild owns evaluation, restore and Csc. These operations only prepare and inspect files.
internal static class StandardBuildIntegration
{
    internal const int SchemaVersion = 4;
    internal const string SdkVersion = "8.0.421";

    internal static CommandReport RestoreCommand(ParsedCommand command)
    {
        command.RequireOnlyOptions("offline", "locked", "configuration");
        if (command.Positionals.Count != 1) throw new CommandLineException("restore requires one standard project directory.");
        bool offline = BooleanOption(command, "offline"), locked = BooleanOption(command, "locked");
        var report = NewReport("restore", command.Positionals[0]);
        try
        {
            var context = ProjectValidator.LoadValidated(report.RootPath, report.Diagnostics);
            report.TargetRuntimeVersion = context.ApiTarget;
            if (report.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error)) return Finish(report);
            string projectDirectory = Path.GetDirectoryName(ProjectPath(context))!;
            string dotnet = ResolveDotNet(projectDirectory), sdkRoot = Path.GetDirectoryName(typeof(StandardBuildIntegration).Assembly.Location)!;
            var args = new List<string> { "msbuild", ProjectPath(context), "-t:Restore", "-nologo", "-v:minimal", "-nr:false",
                "-p:DTMAPI_AUTHOR_SDK_ROOT=" + EscapeProperty(sdkRoot), "-p:Configuration=" + EscapeProperty(command.Option("configuration", "Release")) };
            if (locked) args.Add("-p:RestoreLockedMode=true");
            if (offline) AddOfflineProperties(args, sdkRoot, projectDirectory);
            var result = Run(dotnet, args, projectDirectory);
            report.Values["restoreLog"] = result.Text;
            report.Values["sdkVersion"] = SdkVersion; report.Values["dotnetPath"] = dotnet;
            report.Values["offline"] = offline.ToString(); report.Values["locked"] = locked.ToString();
            ReadDiagnostics(report, result.Text);
            if (result.ExitCode != 0 && !report.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error)) report.Diagnostics.Add(Error("SDK205", result.Text, context.RootPath));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or Win32Exception or JsonException)
        { report.Diagnostics.Add(Error("SDK204", ex.Message, report.RootPath)); }
        return Finish(report);
    }

    internal static bool BooleanOption(ParsedCommand command, string name)
    {
        if (!bool.TryParse(command.Option(name, "false"), out bool value)) throw new CommandLineException("--" + name + " requires true or false.");
        return value;
    }

    private static void AddOfflineProperties(List<string> args, string sdkRoot, string root)
    {
        string source = Path.Combine(sdkRoot, "offline-packages");
        if (!Directory.Exists(source)) { source = Path.Combine(root, "obj", "dtmapi-empty-offline-source"); Directory.CreateDirectory(source); }
        args.Add("-p:RestoreSources=" + EscapeProperty(source));
        args.Add("-p:RestoreAdditionalProjectSources=");
        args.Add("-p:NuGetAudit=false");
    }

    internal static bool IsStandardProject(string root)
    {
        string file = Path.Combine(root, "dtmapi.author.json");
        return File.Exists(file) && JsonNode.Parse(File.ReadAllText(file))?["schemaVersion"]?.GetValue<int>() == SchemaVersion;
    }

    internal static string ProjectPath(AuthorProjectContext context)
    {
        string relative = context.AuthorProject.ProjectFile;
        if (string.IsNullOrWhiteSpace(relative) || !relative.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("author schema 4 requires projectFile naming the standard Mod csproj.");
        string path = PathSafety.ResolveUnderRoot(context.RootPath, relative, "projectFile");
        if (!File.Exists(path)) throw new InvalidDataException("Standard project is missing: " + path);
        return path;
    }

    internal static void ValidateAuthorInput(string path, AuthorProject author)
    {
        var json = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        string[] allowed = { "schemaVersion", "projectKind", "targetDtmApiVersion", "projectFile", "codeModKind", "nativeReferences", "advanced", "publish", "contentDirectory" };
        foreach (string key in json.Select(pair => pair.Key))
            if (!allowed.Contains(key, StringComparer.Ordinal))
                throw new InvalidDataException("Author schema 4 does not accept '" + key + "'; declare ordinary build inputs in the standard project.");
        if (author.ProjectKind == "CodeMod" && json.ContainsKey("contentDirectory"))
            throw new InvalidDataException("CodeMod content belongs to Content/None items with DtmApiPackagePath; contentDirectory is only for ContentPack.");
    }

    public static CommandReport PrepareCommand(ParsedCommand command)
    {
        command.RequireOnlyOptions("compatibility-root", "output", "game-root", "project-file");
        if (command.Positionals.Count != 1) throw new CommandLineException("msbuild-prepare requires one author root.");
        var report = NewReport("msbuild-prepare", command.Positionals[0]);
        try
        {
            var context = ProjectValidator.LoadValidated(report.RootPath, report.Diagnostics);
            report.TargetRuntimeVersion = context.ApiTarget;
            if (report.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error)) return Finish(report);
            RequireSelectedProject(context, command);
            var assets = CompatibilityAssets.Resolve(command.Option("compatibility-root"), context.ApiTarget);
            string output = AbsoluteFileOption(command, "output");
            PathSafety.RejectBepInExPluginDestination(output);
            Directory.CreateDirectory(Path.GetDirectoryName(output)!);
            File.WriteAllLines(output, PrepareReferencePaths(context, assets, command.Option("game-root")), new UTF8Encoding(false));
            report.OutputPath = output;
            report.Values["compatibilityManifestSha256"] = PathSafety.Sha256File(assets.ManifestPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or BadImageFormatException)
        { report.Diagnostics.Add(Error("SDK204", ex.Message, report.RootPath)); }
        return Finish(report);
    }

    public static CommandReport CheckReferencesCommand(ParsedCommand command)
    {
        command.RequireOnlyOptions("compatibility-root", "references", "assets", "compiler-arguments", "game-root", "project-file");
        if (command.Positionals.Count != 1) throw new CommandLineException("msbuild-check-references requires one author root.");
        var report = NewReport("msbuild-check-references", command.Positionals[0]);
        try
        {
            var context = ProjectValidator.LoadValidated(report.RootPath, report.Diagnostics);
            report.TargetRuntimeVersion = context.ApiTarget;
            if (report.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error)) return Finish(report);
            string projectDirectory = Path.GetDirectoryName(RequireSelectedProject(context, command))!;
            var assets = CompatibilityAssets.Resolve(command.Option("compatibility-root"), context.ApiTarget);
            if (command.HasOption("assets")) StandardNuGetAssets.Verify(AbsoluteFileOption(command, "assets"));
            string[] actual, actualAnalyzers;
            if (command.HasOption("compiler-arguments"))
            {
                string[] arguments = File.ReadAllLines(AbsoluteFileOption(command, "compiler-arguments"));
                string[] Paths(string option) => arguments.Where(a => a.StartsWith(option, StringComparison.OrdinalIgnoreCase))
                    .Select(a => a.Substring(option.Length))
                    .Select(p => option == "/reference:" ? Regex.Replace(p, @"^[A-Za-z_][A-Za-z0-9_]*(?:,[A-Za-z_][A-Za-z0-9_]*)*=", "") : p)
                    .Select(p => p.Trim('"'))
                    .Select(p => Path.GetFullPath(p, projectDirectory)).ToArray();
                actual = Paths("/reference:"); actualAnalyzers = Paths("/analyzer:");
            }
            else
            {
                string references = AbsoluteFileOption(command, "references");
                actual = File.ReadAllLines(references).Where(p => p.Length > 0).Select(p => Path.GetFullPath(p, projectDirectory)).ToArray();
                string analyzerList = Path.Combine(Path.GetDirectoryName(references)!, "resolved-analyzers.txt");
                actualAnalyzers = File.Exists(analyzerList) ? File.ReadAllLines(analyzerList).Where(p => p.Length > 0).Select(p => Path.GetFullPath(p, projectDirectory)).ToArray() : Array.Empty<string>();
            }
            string[] preparedReferences = PrepareReferencePaths(context, assets, command.Option("game-root")).ToArray();
            foreach (string expected in preparedReferences)
            {
                string[] matches = actual.Where(p => Path.GetFileName(p).Equals(Path.GetFileName(expected), StringComparison.OrdinalIgnoreCase)).ToArray();
                if (matches.Length != 1 || PathSafety.Sha256File(matches[0]) != PathSafety.Sha256File(expected))
                    throw new InvalidDataException("Resolved Csc reference differs from the frozen API/BCL: " + expected);
            }
            foreach (string reference in actual)
            {
                string name = AssemblyName.GetAssemblyName(reference).Name!;
                if (PackageHostReferences.IsReserved(name) && !preparedReferences.Any(p =>
                    Path.GetFileName(p).Equals(Path.GetFileName(reference), StringComparison.OrdinalIgnoreCase) && PathSafety.Sha256File(p) == PathSafety.Sha256File(reference)))
                    throw new InvalidDataException("bundled-native-runtime-dependency: Unadmitted compiler reference " + name);
            }
            string analyzer = Path.Combine(AppContext.BaseDirectory, "analyzers", "DTMAPI.Author.Analyzers.dll");
            if (actualAnalyzers.Count(path => File.Exists(path) &&
                Path.GetFileName(path) == Path.GetFileName(analyzer) && PathSafety.Sha256File(path) == PathSafety.Sha256File(analyzer)) != 1)
                throw new InvalidDataException("Required SDK203 analyzer is missing or was replaced in the actual Csc inputs.");
            report.Values["resolvedReferenceCount"] = actual.Length.ToString();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or BadImageFormatException)
        { report.Diagnostics.Add(Error("SDK204", ex.Message, report.RootPath)); }
        return Finish(report);
    }

    private static string AbsoluteFileOption(ParsedCommand command, string name)
    {
        string path = command.Option(name);
        if (!Path.IsPathFullyQualified(path)) throw new InvalidDataException("MSBuild must supply an absolute --" + name + " path: " + path);
        return Path.GetFullPath(path);
    }

    private static string RequireSelectedProject(AuthorProjectContext context, ParsedCommand command)
    {
        string actual = AbsoluteFileOption(command, "project-file"), selected = ProjectPath(context);
        if (!actual.Equals(selected, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            throw new InvalidDataException("Author metadata projectFile selects '" + selected + "', but MSBuild is building '" + actual + "'. Check DtmApiAuthorProjectRoot and projectFile.");
        return actual;
    }

    private static IEnumerable<string> PrepareReferencePaths(AuthorProjectContext context, CompatibilityAssets assets, string gameRoot = "")
    {
        if (NativeProjectReferences.UsesContract(context))
            return assets.ReferencePaths.Append(assets.AbstractionsPath).Concat(NativeProjectReferences.CompilerPaths(context, NativeProjectReferences.Resolve(context, gameRoot)).ReferencePaths);
        if (context.CodeModKind == AuthorCodeModKind.Advanced)
            return AdvancedCompilationReferences.PreparePaths(assets, AdvancedReferenceAssets.Resolve(context, gameRoot), context.RootPath);
        return assets.ReferencePaths.Append(assets.AbstractionsPath);
    }

    internal static CommandReport Build(string root, string compatibilityRoot, string requestedOutput, string gameRoot, string configuration, AuthorProjectContext? suppliedContext = null, bool packaging = false, bool offline = false)
    {
        var report = NewReport("build", root);
        try
        {
            var context = suppliedContext ?? ProjectValidator.LoadValidated(report.RootPath, report.Diagnostics);
            report.TargetRuntimeVersion = context.ApiTarget;
            if (report.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error)) return Finish(report);
            if (context.Kind != AuthorProjectKind.CodeMod) throw new InvalidDataException("build requires a CodeMod standard project.");
            if (string.IsNullOrWhiteSpace(configuration) || configuration.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || configuration is "." or "..")
                throw new InvalidDataException("Configuration must be a nonempty directory-safe MSBuild configuration name.");
            string project = ProjectPath(context);
            string projectDirectory = Path.GetDirectoryName(project)!;
            report.TargetRuntimeVersion = context.ApiTarget;
            if (compatibilityRoot.Length > 0) compatibilityRoot = Path.GetFullPath(compatibilityRoot);
            string dotnet = ResolveDotNet(projectDirectory);
            string sdkRoot = Path.GetDirectoryName(typeof(StandardBuildIntegration).Assembly.Location)!;
            var compatibility = CompatibilityAssets.Resolve(compatibilityRoot, context.ApiTarget);
            if (context.CodeModKind == AuthorCodeModKind.Advanced && !NativeProjectReferences.UsesContract(context))
            {
                var advanced = AdvancedReferenceAssets.Resolve(context, gameRoot);
                report.Values["gameBuildId"] = advanced.Policy.GameBuildId;
                report.Values["referencePolicyId"] = advanced.Policy.PolicyId;
                report.Values["referencePolicySha256"] = advanced.PolicySha256;
                report.Values["compilerReferenceSurfaceSha256"] = advanced.Registration.CompilerSurfaceSha256.ToLowerInvariant();
                report.Values["harmonyOwner"] = "dtmapi.mod." + context.Manifest.UniqueID.ToLowerInvariant();
            }
            string nativeHash = NativeProjectReferences.UsesContract(context) ? NativeProjectReferences.CompilerPaths(context, NativeProjectReferences.Resolve(context, gameRoot)).InputSha256 : "";
            if (!File.Exists(Path.Combine(sdkRoot, "build", "DTMAPI.Author.targets"))) throw new InvalidDataException("SDK build integration is missing.");
            string intermediate = Path.Combine(projectDirectory, "obj", "dtmapi-author", configuration, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(intermediate);
            string errorLog = Path.Combine(intermediate, "compiler.sarif");
            string factsFile = Path.Combine(intermediate, "build-facts.xml");
            // MSBuild escapes literal semicolons and other special characters in global properties.
            string Property(string name, string value) => "-p:" + name + "=" + EscapeProperty(value);
            var args = new List<string> { "msbuild", project, "-restore", "-t:DtmApiCollect", "-nologo", "-v:minimal", "-nr:false",
                Property("Configuration", configuration), Property("DTMAPI_AUTHOR_SDK_ROOT", sdkRoot), Property("DtmApiDotNet", dotnet),
                Property("DtmApiCompatibilityRoot", compatibilityRoot), Property("DtmApiErrorLog", errorLog),
                Property("DtmApiFactsFile", factsFile), "-p:UseSharedCompilation=false" };
            if (packaging) { args.Add("-p:DtmApiPackaging=true"); args.Add("-p:RestoreLockedMode=true"); }
            if (gameRoot.Length > 0) args.Add(Property("DtmApiGameRoot", Path.GetFullPath(gameRoot)));
            if (offline) AddOfflineProperties(args, sdkRoot, projectDirectory);
            if (requestedOutput.Length > 0)
            {
                string output = Path.GetFullPath(requestedOutput, root);
                PathSafety.RejectBepInExPluginDestination(output);
                args.Add(Property("DtmApiRequestedOutput", output + Path.DirectorySeparatorChar));
            }
            if (File.Exists(errorLog)) File.Delete(errorLog);
            var result = Run(dotnet, args, projectDirectory);
            report.Values["buildLog"] = result.Text;
            report.Values["dotnetPath"] = dotnet;
            report.Values["sdkVersion"] = SdkVersion;
            report.Values["projectPath"] = project;
            report.Values["projectDirectory"] = projectDirectory;
            report.Values["authorProjectRoot"] = context.RootPath;
            report.Values["configuration"] = configuration;
            report.Values["buildBackend"] = "MSBuild";
            ReadDiagnostics(report, result.Text);
            if (result.ExitCode != 0)
            {
                if (!report.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error)) report.Diagnostics.Add(Error("SDK205", result.Text, project));
                return Finish(report);
            }
            var facts = XDocument.Load(factsFile);
            var properties = facts.Root!.Element("Properties")!.Elements("Property").ToDictionary(p => (string)p.Attribute("Name")!, p => p.Value);
            string dll = properties["TargetPath"];
            if (!File.Exists(dll) || Path.GetFileName(dll) != context.Manifest.EntryDll)
                throw new InvalidDataException("Standard TargetPath must produce manifest EntryDll: " + context.Manifest.EntryDll);
            string entryName = AssemblyName.GetAssemblyName(dll).Name!;
            if (PackageHostReferences.IsReserved(entryName))
                throw new InvalidDataException("bundled-native-runtime-dependency: " + entryName);
            report.OutputPath = dll;
            report.Sha256 = PathSafety.Sha256File(dll);
            report.TargetRuntimeVersion = context.ApiTarget;
            report.Values["targetFramework"] = properties["TargetFramework"];
            if (properties["DebugType"] == "portable" && File.Exists(Path.ChangeExtension(dll, ".pdb")))
            {
                report.Values["symbolsPath"] = Path.ChangeExtension(dll, ".pdb");
                report.Values["symbolsSha256"] = PathSafety.Sha256File(report.Values["symbolsPath"]);
            }
            report.Values["intermediatePath"] = Path.GetFullPath(properties["IntermediateOutputPath"], properties["ProjectDirectory"]);
            report.Values["msbuildVersion"] = properties["MSBuildVersion"];
            report.Values["buildFactsPath"] = factsFile;
            report.Values["buildFactsSha256"] = PathSafety.Sha256File(factsFile);
            report.Values["compatibilityManifestSha256"] = PathSafety.Sha256File(compatibility.ManifestPath);
            report.Values["abstractionsSha256"] = PathSafety.Sha256File(compatibility.AbstractionsPath);
            string compiler = Path.Combine(properties["MSBuildSDKsPath"], "..", "Roslyn", "bincore", "csc.dll");
            if (properties["CscToolPath"].Length > 0) compiler = Path.Combine(properties["CscToolPath"], properties["CscToolExe"]);
            report.Values["compilerPath"] = Path.GetFullPath(compiler);
            report.Values["compilerSha256"] = PathSafety.Sha256File(compiler);
            string pinnedCompiler = Path.Combine(Path.GetDirectoryName(dotnet)!, "sdk", SdkVersion, "Roslyn", "bincore", "csc.dll");
            if (PathSafety.Sha256File(pinnedCompiler) != report.Values["compilerSha256"])
                throw new InvalidDataException("The actual compiler differs from the selected .NET SDK " + SdkVersion + " Csc. Remove the custom CscToolPath/CscToolExe override.");
            report.Values["compilerVersion"] = FileVersionInfo.GetVersionInfo(compiler).FileVersion ?? "";
            report.Values["assemblyName"] = context.AuthorProject.AssemblyName;
            report.Values["codeModKind"] = context.CodeModKind.ToString();
            if (context.AuthorProject.Advanced != null)
                report.Values["referencePolicyId"] = context.AuthorProject.Advanced.ReferencePolicyId;
            if (properties["DocumentationFile"].Length > 0)
            {
                report.Values["documentationPath"] = Path.GetFullPath(properties["DocumentationFile"], properties["ProjectDirectory"]);
                report.Values["documentationSha256"] = PathSafety.Sha256File(report.Values["documentationPath"]);
            }
            if (nativeHash.Length > 0)
            {
                if (NativeProjectReferences.CompilerPaths(context, NativeProjectReferences.Resolve(context, gameRoot)).InputSha256 != nativeHash)
                    throw new InvalidDataException("native-metadata-input-changed: Host changed during standard build.");
                report.Values["nativeReferenceInputSha256"] = nativeHash;
            }
            report.FileCount = facts.Root.Element("Items")!.Elements("Item").Count(item => (string?)item.Attribute("Kind") == "Compile");
            report.Diagnostics.Add(new AuthorDiagnostic { Code = "SDK000", Severity = DiagnosticSeverity.Info, Message = "Built the standard project with MSBuild/Csc." });
            Finish(report);
            File.WriteAllText(dll + ".build.json", JsonSupport.SerializeTool(report));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or JsonException or Win32Exception or BadImageFormatException)
        { report.Diagnostics.Add(Error("SDK204", ex.Message, report.RootPath)); }
        return Finish(report);
    }

    internal static string ResolveDotNet(string projectRoot)
    {
        StandardSdkIntegrity.VerifyBundledSdk(AppContext.BaseDirectory);
        string explicitPath = Environment.GetEnvironmentVariable("DTMAPI_AUTHOR_DOTNET") ?? "";
        string bundled = Path.Combine(AppContext.BaseDirectory, "toolchain", "dotnet", OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet");
        string selected = explicitPath.Length > 0 ? explicitPath : File.Exists(bundled) ? bundled :
            Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? (Path.GetFileNameWithoutExtension(Environment.ProcessPath) == "dotnet" ? Environment.ProcessPath! : "dotnet");
        if (explicitPath.Length > 0 && (!Path.IsPathFullyQualified(selected) || !File.Exists(selected)))
            throw new InvalidDataException("DTMAPI_AUTHOR_DOTNET must name an existing absolute dotnet executable; no fallback was attempted.");
        if (!Path.IsPathFullyQualified(selected))
        {
            string executable = OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet";
            selected = (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator)
                .Where(Directory.Exists).Select(p => Path.Combine(p, executable)).FirstOrDefault(File.Exists)
                ?? throw new InvalidDataException("No supported .NET SDK was found. Use the bundled SDK development environment or set DTMAPI_AUTHOR_DOTNET.");
        }
        var version = Run(selected, new[] { "--version" }, projectRoot);
        if (version.ExitCode != 0 || version.Text.Trim() != SdkVersion)
            throw new InvalidDataException("Select .NET SDK " + SdkVersion + " and check the project's ancestor global.json. " + version.Text);
        return selected;
    }

    private static string EscapeProperty(string value) => string.Concat(value.Select(c => c is '%' or ';' or '$' or '@' or '(' or ')' or '\'' or '*' or '?' ? "%" + ((int)c).ToString("X2") : c.ToString()));

    private static void ReadDiagnostics(CommandReport report, string output)
    {
        // MSBuild's console diagnostic format preserves language-independent IDs and severity.
        foreach (string line in output.Split('\n'))
        {
            Match match = Regex.Match(line.Trim(), @"^(?<path>.+?)(?:\((?<line>\d+),(?<column>\d+)\))?\s*:\s*(?<level>error|warning)\s+(?<code>[A-Z]+\d+)\s*:\s*(?<message>.*?)(?:\s+\[.*\])?$", RegexOptions.CultureInvariant);
            if (!match.Success) continue;
            var diagnostic = new AuthorDiagnostic { Code = match.Groups["code"].Value,
                Severity = match.Groups["level"].Value == "error" ? DiagnosticSeverity.Error : DiagnosticSeverity.Warning,
                Path = match.Groups["path"].Value + (match.Groups["line"].Success ? ":" + match.Groups["line"].Value + ":" + match.Groups["column"].Value : ""),
                Message = match.Groups["message"].Value };
            if (!report.Diagnostics.Any(d => d.Code == diagnostic.Code && d.Path == diagnostic.Path && d.Message == diagnostic.Message)) report.Diagnostics.Add(diagnostic);
        }
    }

    private static (int ExitCode, string Text) Run(string executable, IEnumerable<string> args, string root)
    {
        var start = new ProcessStartInfo(executable) { WorkingDirectory = root, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        start.Environment["DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE"] = "true";
        start.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        foreach (string arg in args) start.ArgumentList.Add(arg);
        using var process = Process.Start(start) ?? throw new IOException("Could not start " + executable);
        Task<string> stdout = process.StandardOutput.ReadToEndAsync(), stderr = process.StandardError.ReadToEndAsync();
        int cancelled = 0;
        ConsoleCancelEventHandler cancel = (_, e) =>
        {
            e.Cancel = true;
            Interlocked.Exchange(ref cancelled, 1);
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
            catch (InvalidOperationException) { }
        };
        Console.CancelKeyPress += cancel;
        try
        {
            process.WaitForExit();
            string output = stdout.GetAwaiter().GetResult() + stderr.GetAwaiter().GetResult();
            return (cancelled != 0 ? 130 : process.ExitCode, output + (cancelled != 0 ? "\nBuild cancelled; the SDK stopped its MSBuild process tree." : ""));
        }
        finally { Console.CancelKeyPress -= cancel; }
    }

    private static CommandReport NewReport(string command, string root) => new() { Command = command, RootPath = Path.GetFullPath(root) };
    private static CommandReport Finish(CommandReport report) { report.Success = !report.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error); return report; }
    private static AuthorDiagnostic Error(string code, string message, string path) => new() { Code = code, Severity = DiagnosticSeverity.Error, Message = message, Path = path };
}
