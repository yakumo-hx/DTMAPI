using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using DTMAPI.Authoring.Contracts;
using DTMAPI.Tooling.Metadata;

namespace DTMAPI.AuthorSdk;

// Convenience commands have no role in Runtime admission or standard MSBuild.
internal static class OptionalTools
{
    internal static async Task<int> BuildAsync(string[] args, TextWriter output, TextWriter error)
    {
        string executable = Environment.GetEnvironmentVariable("DTMAPI_DOTNET_EXE") ?? Environment.GetEnvironmentVariable("DTMAPI_AUTHOR_DOTNET") ?? "";
        string bundled = Path.Combine(AppContext.BaseDirectory, "toolchain", OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet");
        if (executable.Length == 0) executable = File.Exists(bundled) ? bundled : "dotnet";
        var start = new ProcessStartInfo(executable) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add(args[0]);
        foreach (string arg in args.Skip(1).Where(arg => arg != "--" && arg != "--json")) start.ArgumentList.Add(arg);
        using var process = Process.Start(start) ?? throw new IOException("Could not start the .NET build host.");
        var stdout = Pump(process.StandardOutput, output);
        var stderr = Pump(process.StandardError, error);
        await Task.WhenAll(stdout, stderr, process.WaitForExitAsync());
        return process.ExitCode;
    }
    private static async Task Pump(StreamReader reader, TextWriter writer)
    { while (await reader.ReadLineAsync() is string line) await writer.WriteLineAsync(line); }

    internal static CommandReport New(ParsedCommand command)
    {
        if (command.Positionals.Count != 2 || command.Positionals[0] is not ("codemod" or "contentpack")) throw new CommandLineException("new codemod|contentpack <directory> --id Author.Mod");
        string root = Path.GetFullPath(command.Positionals[1]);
        string id = Id(command.Option("id"));
        if (Directory.Exists(root) && Directory.EnumerateFileSystemEntries(root).Any()) throw new IOException("Destination is not empty: " + root);
        Directory.CreateDirectory(root);
        bool code = command.Positionals[0] == "codemod";
        // UniqueIDs may contain hyphens or numeric segments; they are not C# namespaces.
        const string codeNamespace = "DtmApiMod";
        var manifest = new JsonObject { ["ManifestVersion"] = 2, ["UniqueID"] = id, ["Type"] = code ? "CodeMod" : "ContentPack", ["MinimumDTMApiVersion"] = "0.7.5" };
        if (code) { manifest["EntryDll"] = "Content/DTMAPI/" + id + ".dll"; manifest["EntryType"] = codeNamespace + ".ModEntry"; }
        if (code && command.Option("harmony-owner").Length > 0) manifest["HarmonyOwner"] = command.Option("harmony-owner");
        string host = command.Option("host");
        if (host.Length > 0)
        {
            manifest["ContentPackFor"] = new JsonObject { ["HostId"] = Id(host), ["Schema"] = command.Option("content-schema", "author.content/1"),
                ["ContractVersionRange"] = new JsonObject { ["minimumInclusive"] = command.Option("host-contract", "1.0.0"), ["includePrerelease"] = false } };
            Directory.CreateDirectory(Path.Combine(root, "package", "PackData"));
        }
        string content = Path.Combine(root, "package", "Content", "DTMAPI"); Directory.CreateDirectory(content);
        WriteJson(Path.Combine(content, "manifest.json"), manifest);
        WriteJson(Path.Combine(root, "package", "info.json"), new { name = command.Option("name", id), author = command.Option("author", ""), version = command.Option("version", "1.0.0"), description = command.Option("description", "") });
        if (code)
        {
            string refs = Path.GetFullPath(command.Option("references", Path.Combine(AppContext.BaseDirectory, "references")));
            string xmlRefs = System.Security.SecurityElement.Escape(refs)!;
            File.WriteAllText(Path.Combine(root, id + ".csproj"), $"""
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <AssemblyName>{id}</AssemblyName>
    <DtmApiReferences Condition="'$(DtmApiReferences)' == ''">{xmlRefs}</DtmApiReferences>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="DTMAPI.Abstractions" HintPath="$(DtmApiReferences)/DTMAPI.Abstractions.dll" Private="false" />
  </ItemGroup>
  <Target Name="CopyModEntry" AfterTargets="Build">
    <Copy SourceFiles="$(TargetPath)" DestinationFolder="$(MSBuildProjectDirectory)/package/Content/DTMAPI" />
  </Target>
</Project>
""");
            File.WriteAllText(Path.Combine(root, "ModEntry.cs"), $$"""
using DTMAPI.Abstractions;
namespace {{codeNamespace}}
{
    public sealed class ModEntry : DtmMod
    {
        public override void Entry(IDtmHelper helper) { helper.Monitor.Log("Mod loaded."); }
    }
}
""");
        }
        File.WriteAllText(Path.Combine(root, "README.md"), (code ? "Build with dotnet build. API and game reference DLLs must use Private=false. " : "Edit the package content directly. ") + "The package directory can be copied to official MODS, or passed to dtmapi-author pack/deploy. Enable it in the game's Mod page.\n");
        return Report(command.Name, root, root);
    }

    internal static CommandReport Validate(ParsedCommand command)
    {
        string root = OnePath(command);
        var package = ReadPackage(root);
        var report = Report(command.Name, root, "");
        report.Values["uniqueId"] = package.Id; report.Values["version"] = package.Version;
        report.Values["coverage"] = "manifest and entry path; optional author advice, no Runtime certificate";
        return report;
    }

    internal static CommandReport Pack(ParsedCommand command)
    {
        string root = OnePath(command);
        if (!Directory.Exists(root)) throw new DirectoryNotFoundException(root);
        string output = Path.GetFullPath(command.Option("output", root.TrimEnd(Path.DirectorySeparatorChar) + ".zip"));
        if (IsUnder(root, output)) throw new IOException("Place the ZIP outside its source directory.");
        if (File.Exists(output) && !command.HasOption("replace")) throw new IOException("Output exists; use --replace: " + output);
        string temp = output + ".tmp-" + Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        int count = 0;
        try
        {
            using (var archive = ZipFile.Open(temp, ZipArchiveMode.Create))
                foreach (string file in PathSafety.EnumeratePayloadFiles(root, root).OrderBy(file => file, StringComparer.Ordinal))
                { archive.CreateEntryFromFile(file, Path.GetRelativePath(root, file).Replace('\\', '/')); count++; }
            File.Move(temp, output, overwrite: command.HasOption("replace"));
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
        var report = Report("pack", root, output); report.FileCount = count; return report;
    }

    internal static CommandReport Hash(ParsedCommand command)
    {
        string path = OnePath(command);
        var report = Report("hash", path, "");
        report.Sha256 = Directory.Exists(path) ? PathSafety.Sha256Tree(path, PathSafety.EnumeratePayloadFiles(path, path)) : PathSafety.Sha256File(path);
        return report;
    }

    internal static CommandReport NativeReferences(ParsedCommand command)
    {
        if (command.Positionals.Count == 0) throw new CommandLineException("native-references <DLL-or-directory> [...] --output <directory>");
        string output = Path.GetFullPath(command.Option("output", "native-references"));
        string[] files = command.Positionals.SelectMany(path => Directory.Exists(path) ? Directory.GetFiles(path, "*.dll") : new[] { path }).Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        string[] directories = files.Select(path => Path.GetDirectoryName(path)!).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (files.Any(file => IsUnder(output, file))) throw new IOException("Reference output must be separate from the source DLL directory.");
        Directory.CreateDirectory(output);
        var references = new List<string>();
        foreach (string file in files)
        {
            string target = Path.Combine(output, Path.GetFileName(file));
            File.WriteAllBytes(target, NativeReferenceSurface.Create(File.ReadAllBytes(file), directories));
            references.Add($"    <Reference Include=\"{System.Security.SecurityElement.Escape(Path.GetFileNameWithoutExtension(file))}\" HintPath=\"$(MSBuildThisFileDirectory){System.Security.SecurityElement.Escape(Path.GetFileName(file))}\" Private=\"false\" />");
        }
        File.WriteAllText(Path.Combine(output, "NativeReferences.props"), "<Project>\n  <ItemGroup>\n" + string.Join("\n", references) + "\n  </ItemGroup>\n</Project>\n");
        return Report(command.Name, string.Join(";", files), output);
    }

    internal static CommandReport Deploy(ParsedCommand command)
    {
        string source = OnePath(command), game = Game(command);
        using var operation = GameOperationLock.Acquire(game);
        using var cold = ColdGameMutationLease.Acquire(game);
        string work = Path.Combine(OfficialLocalPaths.PersistentRoot, "DTMAPI", "author-work", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(work);
        try
        {
            string input = source;
            if (File.Exists(source)) { input = Path.Combine(work, "input"); Extract(source, input); }
            var package = ReadPackage(input);
            Directory.CreateDirectory(OfficialLocalPaths.ModsRoot);
            string target = command.HasOption("target") ? Path.TrimEndingDirectorySeparator(Path.GetFullPath(command.Option("target"))) : FindDestination(package.Id);
            if (!IsDirectChild(OfficialLocalPaths.ModsRoot, target)) throw new IOException("Deployment directory must be an ordinary child of MODS.");
            if (IsUnder(target, source) || IsUnder(source, target)) throw new IOException("Source and deployment destination must be separate.");
            bool clean = command.HasOption("clean");
            string workshop = Path.Combine(target, "workshop.json");
            bool preserveIdentity = File.Exists(workshop);
            if (clean)
            {
                // A sibling staging directory survives clearing the target. It is
                // the new package to install, never a backup of the old installation.
                string stage = Path.Combine(OfficialLocalPaths.ModsRoot, ".dtmapi-stage-" + Guid.NewGuid().ToString("N"));
                bool prepared = false;
                try
                {
                    CopyTree(input, stage);
                    if (preserveIdentity)
                    {
                        PathSafety.RejectReparsePoints(target, new[] { workshop });
                        File.Copy(workshop, Path.Combine(stage, "workshop.json"), overwrite: true);
                    }
                    prepared = true;
                    if (Directory.Exists(target)) Directory.Delete(target, recursive: true);
                    Directory.Move(stage, target);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    throw new IOException("Clean installation failed at " + target + ". New-package staging: " + stage +
                        (prepared ? ". Preparation completed; the old installation may have been partly or fully cleared. " : ". Preparation incomplete; the existing installation was not cleared. ") +
                        "This staging directory is not an old-installation backup. " +
                        "Correct the file error and retry: dtmapi-author deploy \"" + (prepared ? stage : source) +
                        "\" --game-root \"" + game + "\" --target \"" + target + "\" --clean. " + ex.Message, ex);
                }
            }
            else
            {
                try { CopyTree(input, target, overwrite: true, preserveWorkshopIdentity: preserveIdentity); }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    throw new IOException("Deployment to " + target + " may have partially overwritten files; no automatic backup was created. Correct the file error and retry deploy. " + ex.Message, ex);
                }
            }
            var report = Report(command.Name, source, target); report.Values["uniqueId"] = package.Id; report.Values["version"] = package.Version;
            report.Values["deploymentMode"] = clean ? "clean" : "overlay";
            report.Values["recoveryDirectory"] = "none";
            report.Values["workshopIdentity"] = preserveIdentity ? "existing target preserved" : "package content";
            report.Values["oldFiles"] = clean ? "old installation cleared; no automatic backup" : "files absent from this package remain, including renamed/deleted DLLs and resources; use --clean to remove them";
            OfficialLocalPaths.AddStatus(report, Path.GetFileName(target)); return report;
        }
        finally { if (Directory.Exists(work)) Directory.Delete(work, recursive: true); }
    }

    internal static CommandReport Status(ParsedCommand command)
    {
        string id = Id(One(command)), game = Game(command), target = FindDestination(id);
        var report = Report(command.Name, target, ""); report.Values["uniqueId"] = id;
        report.Values["directoryExists"] = Directory.Exists(target).ToString().ToLowerInvariant();
        if (Directory.Exists(target))
        { try { var package = ReadPackage(target); report.Values["version"] = package.Version; report.Values["manifest"] = package.Manifest; } catch (Exception ex) when (ex is IOException or JsonException or InvalidDataException) { report.Values["manifestError"] = ex.Message; } }
        string journalPath = JournalPath(game, id);
            if (File.Exists(journalPath))
            {
                var journal = ReadJournal(game, id);
                report.Values["lastRecoveryOperationPhase"] = journal.Phase;
                report.Values["recoveryDirectory"] = Directory.Exists(journal.Backup) ? journal.Backup : "none";
            }
        OfficialLocalPaths.AddStatus(report, Path.GetFileName(target));
        return report;
    }

    internal static CommandReport Withdraw(ParsedCommand command)
    {
        string id = Id(One(command)), game = Game(command);
        using var operation = GameOperationLock.Acquire(game); using var cold = ColdGameMutationLease.Acquire(game);
        string target = FindDestination(id);
        if (!Directory.Exists(target)) return Report(command.Name, target, "");
        var journal = new DeploymentJournal { Id = id, Destination = target, Backup = RecoveryPath(id), Phase = "withdrawing" };
        SaveJournal(game, journal); Directory.Move(target, journal.Backup);
        journal.Phase = "withdrawn"; SaveJournal(game, journal);
        return Report(command.Name, target, journal.Backup);
    }

    internal static CommandReport Recover(ParsedCommand command)
    {
        string id = Id(One(command)), game = Game(command);
        using var operation = GameOperationLock.Acquire(game); using var cold = ColdGameMutationLease.Acquire(game);
        var journal = ReadJournal(game, id);
        if (!Directory.Exists(journal.Backup)) throw new IOException("No preserved directory is available: " + journal.Backup);
        if (Directory.Exists(journal.Destination))
        {
            if (!command.HasOption("replace")) throw new IOException("Current destination exists; --replace preserves it before restoring the previous directory.");
            Directory.Move(journal.Destination, RecoveryPath(id));
        }
        Directory.Move(journal.Backup, journal.Destination);
        // An interrupted staging directory is preserved as evidence, never overwritten or deleted by recovery.
        journal.Phase = "recovered"; SaveJournal(game, journal);
        return Report(command.Name, journal.Backup, journal.Destination);
    }

    internal sealed record PackageInfo(string Id, string Version, string Manifest);
    internal static PackageInfo ReadPackage(string root, bool requireEntry = true)
    {
        string manifest = DTMAPI.Internal.Authoring.PackageDocumentFacts.FindManifest(root) ?? throw new FileNotFoundException("Package manifest.json was not found: " + root);
        var data = JsonNode.Parse(File.ReadAllText(manifest))!.AsObject();
        string id = Id(data["UniqueID"]?.GetValue<string>() ?? "");
        string version = data["Version"]?.GetValue<string>() ?? "";
        if (data["ManifestVersion"]?.GetValue<int>() == 2)
        { string info = Path.Combine(root, "info.json"); version = File.Exists(info) && DTMAPI.Internal.Authoring.PackageDocumentFacts.ReadInfo(File.ReadAllBytes(info), out _).TryGetValue("version", out string? displayVersion) ? displayVersion : ""; }
        string entry = data["EntryDll"]?.GetValue<string>() ?? "";
        string type = data["Type"]?.GetValue<string>() ?? "CodeMod";
        if (requireEntry && !type.Equals("ContentPack", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(entry))
            throw new InvalidDataException("CodeMod EntryDll is required.");
        if (requireEntry && entry.Length > 0 && !File.Exists(PathSafety.ResolveUnderRoot(root, entry, "EntryDll"))) throw new FileNotFoundException("Entry DLL missing: " + entry);
        return new PackageInfo(id, version, manifest);
    }
    internal static void CopyTree(string source, string target, bool overwrite = false, bool preserveWorkshopIdentity = false)
    {
        Directory.CreateDirectory(target);
        foreach (string file in PathSafety.EnumeratePayloadFiles(source, source))
        {
            string relative = Path.GetRelativePath(source, file);
            if (preserveWorkshopIdentity && relative.Equals("workshop.json", StringComparison.OrdinalIgnoreCase)) continue;
            string path = PathSafety.ResolveUnderRoot(target, relative, "package file");
            try
            {
                string existing = path;
                while (!File.Exists(existing) && !Directory.Exists(existing)) existing = Path.GetDirectoryName(existing)!;
                PathSafety.RejectReparsePoints(target, new[] { existing });
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.Copy(file, path, overwrite);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            { throw new IOException("Could not copy package file " + relative + " to " + path + ": " + ex.Message, ex); }
        }
    }
    internal static void Extract(string zip, string target)
    {
        Directory.CreateDirectory(target);
        using var archive = ZipFile.OpenRead(zip);
        foreach (var entry in archive.Entries)
        {
            string path = PathSafety.ResolveUnderRoot(target, entry.FullName, "ZIP entry");
            if ((entry.ExternalAttributes >> 16 & 0xf000) == 0xa000) throw new InvalidDataException("ZIP symbolic links are not supported.");
            if (entry.FullName.EndsWith("/")) { Directory.CreateDirectory(path); continue; }
            Directory.CreateDirectory(Path.GetDirectoryName(path)!); entry.ExtractToFile(path, overwrite: false);
        }
    }
    private static string FindDestination(string id)
    {
        var matches = new List<string>();
        if (Directory.Exists(OfficialLocalPaths.ModsRoot)) foreach (string directory in Directory.EnumerateDirectories(OfficialLocalPaths.ModsRoot).Where(path => !Path.GetFileName(path).StartsWith('.')))
        { try { if (ReadPackage(directory, requireEntry: false).Id == id) matches.Add(directory); } catch (Exception ex) when (ex is IOException or JsonException or InvalidDataException) { } }
        if (matches.Count > 1) throw new IOException("Multiple installed directories have this ID: " + string.Join(", ", matches));
        string target = matches.SingleOrDefault() ?? Path.Combine(OfficialLocalPaths.ModsRoot, id);
        if (!IsDirectChild(OfficialLocalPaths.ModsRoot, target)) throw new IOException("Deployment directory must be an ordinary child of MODS.");
        return target;
    }
    private static string RecoveryRoot => Path.Combine(OfficialLocalPaths.PersistentRoot, "DTMAPI", "author-recovery");
    private static string RecoveryPath(string id) { Directory.CreateDirectory(RecoveryRoot); return Path.Combine(RecoveryRoot, id + "-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + "-" + Guid.NewGuid().ToString("N")); }
    private static string JournalPath(string game, string id) => Path.Combine(AuthorStatePaths.InstallationStateRoot(game), "operations", id + ".json");
    private static void SaveJournal(string game, DeploymentJournal journal) => AtomicStateFile.Write(JournalPath(game, journal.Id), journal);
    private static DeploymentJournal ReadJournal(string game, string id)
    {
        var journal = AtomicStateFile.ReadStrict<DeploymentJournal>(JournalPath(game, id));
        if (journal.Id != id || !IsDirectChild(OfficialLocalPaths.ModsRoot, journal.Destination) || !IsDirectChild(RecoveryRoot, journal.Backup) ||
            journal.Stage.Length > 0 && !IsDirectChild(OfficialLocalPaths.ModsRoot, journal.Stage)) throw new InvalidDataException("Journal paths do not belong to this installation.");
        return journal;
    }
    private static bool IsDirectChild(string root, string path)
    {
        string full = Path.GetFullPath(path);
        if (!string.Equals(Path.GetDirectoryName(full), Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase)) return false;
        return !Directory.Exists(full) || (File.GetAttributes(full) & FileAttributes.ReparsePoint) == 0;
    }
    private static bool IsUnder(string root, string path) => Path.GetFullPath(path).StartsWith(Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || Path.GetFullPath(root).Equals(Path.GetFullPath(path), StringComparison.OrdinalIgnoreCase);
    private static string Id(string id) => DTMAPI.Internal.Authoring.PackageDocumentFacts.Id(id);
    private static string One(ParsedCommand command) => command.Positionals.Count == 1 ? command.Positionals[0] : throw new CommandLineException(command.Name + " requires one input.");
    private static string OnePath(ParsedCommand command) => Path.GetFullPath(One(command));
    private static string Game(ParsedCommand command) => command.Option("game-root") is string value && value.Length > 0 ? Path.GetFullPath(value) : throw new CommandLineException("--game-root is required for installation operations.");
    private static void WriteJson(string path, object value) => File.WriteAllText(path, JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true }) + "\n");
    private static CommandReport Report(string command, string root, string output) => new() { Command = command, Success = true, RootPath = root, OutputPath = output };
    internal sealed class DeploymentJournal { public string Id { get; set; } = ""; public string Destination { get; set; } = ""; public string Stage { get; set; } = ""; public string Backup { get; set; } = ""; public string Phase { get; set; } = ""; }
}
