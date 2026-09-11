using DTMAPI.Authoring.Contracts;
using DTMAPI.Internal.Authoring;
using System.Xml.Linq;

namespace DTMAPI.AuthorSdk;

// Only this build's evaluated items are candidates. No directory scan discovers payloads.
internal sealed class StandardPackageSnapshot : IDisposable
{
    private readonly string directory;
    private readonly Dictionary<string, (string Source, string Hash)> files = new(StringComparer.OrdinalIgnoreCase);
    internal string EntryPath { get; private set; } = "";
    internal string DocumentationPath { get; private set; } = "";
    internal sealed record ContentFile(string Name, byte[] Bytes);
    internal List<ContentFile> Content { get; } = new();
    internal Dictionary<string, byte[]> DiagnosticSymbols { get; } = new(StringComparer.Ordinal);

    private StandardPackageSnapshot(string directory) { this.directory = directory; Directory.CreateDirectory(directory); }

    internal static StandardPackageSnapshot Capture(AuthorProjectContext context, CommandReport build, string gameRoot = "")
    {
        var snapshot = new StandardPackageSnapshot(Path.Combine(Path.GetDirectoryName(build.Values["buildFactsPath"])!, "package-stage"));
        try
        {
            if (PathSafety.Sha256File(build.Values["buildFactsPath"]) != build.Values["buildFactsSha256"])
                throw new InvalidDataException("MSBuild facts changed after collection.");
            var facts = XDocument.Load(build.Values["buildFactsPath"]);
            var items = facts.Root!.Element("Items")!.Elements("Item").ToArray();
            string Meta(XElement item, string name) => item.Elements("Metadata").SingleOrDefault(m => string.Equals((string?)m.Attribute("Name"), name, StringComparison.OrdinalIgnoreCase))?.Value ?? "";
            string Kind(XElement item) => (string)item.Attribute("Kind")!;
            string ItemPath(XElement item) => (string)item.Attribute("Path")!;
            snapshot.EntryPath = snapshot.Copy(build.OutputPath, "entry/" + Path.GetFileName(build.OutputPath));
            if (PathSafety.Sha256File(snapshot.EntryPath) != build.Sha256) throw new InvalidDataException("Entry output changed after collection.");
            foreach (var companion in new[] { ("symbolsPath", ".pdb"), ("documentationPath", ".xml") })
                if (build.Values.TryGetValue(companion.Item1, out string? path))
                {
                    string copied = snapshot.Copy(path, "entry/" + Path.GetFileNameWithoutExtension(build.OutputPath) + companion.Item2);
                    string hashKey = companion.Item1 == "symbolsPath" ? "symbolsSha256" : "documentationSha256";
                    if (PathSafety.Sha256File(copied) != build.Values[hashKey])
                        throw new InvalidDataException("Companion output changed after collection: " + path);
                    if (companion.Item1 == "documentationPath") snapshot.DocumentationPath = copied;
                }
            if (build.Values.ContainsKey("symbolsPath"))
            {
                string pdb = Path.ChangeExtension(snapshot.EntryPath, ".pdb");
                var symbols = SymbolInspector.Execute(ParsedCommand.Parse(new[] { "symbols", snapshot.EntryPath, "--pdb", pdb }));
                if (!symbols.Success) throw new InvalidDataException(string.Join("; ", symbols.Diagnostics.Select(d => d.Message)));
                snapshot.DiagnosticSymbols.Add("Content/DTMAPI/" + Path.GetFileName(snapshot.EntryPath), File.ReadAllBytes(pdb));
            }
            context.BuiltLibraries.Clear();
            foreach (var runtime in items.Where(i => Kind(i) == "Runtime"))
            {
                string source = ItemPath(runtime);
                string extension = Path.GetExtension(source).ToLowerInvariant();
                if (extension is ".pdb" or ".xml") continue;
                if (extension != ".dll") throw new InvalidDataException("unsupported-runtime-asset: " + source + "; native/RID assets are not admitted by the current package reader.");
                string destination = Meta(runtime, "DestinationSubDirectory") + Path.GetFileName(source);
                if (destination.Replace('\\', '/').Contains('/')) throw new InvalidDataException("unsupported-runtime-asset: satellite/RID subdirectory " + destination);
                string finalRuntime = Path.Combine(Path.GetDirectoryName(build.OutputPath)!, destination);
                if (!File.Exists(finalRuntime)) throw new InvalidDataException("Selected runtime output is missing after Build: " + finalRuntime);
                var record = PackagePortableMetadata.Inspect(File.ReadAllBytes(finalRuntime));
                if (PackageHostReferences.IsReserved(record.Identity.Name)) throw new InvalidDataException("bundled-native-runtime-dependency: " + record.Identity.Name);
                if (record.TargetFramework != ".NETStandard,Version=v2.0" || Path.GetFileName(finalRuntime) != record.Identity.Name + ".dll")
                    throw new InvalidDataException("runtime-asset-framework-or-identity: " + finalRuntime + "; current reader requires netstandard2.0 and exact assembly filename.");

                string projectOrigin = Meta(runtime, "MSBuildSourceProjectFile");
                string packageId = Meta(runtime, "NuGetPackageId");
                var declared = items.Where(i => Kind(i) is "DeclaredReference" or "ProjectReference" or "PackageReference").Where(i =>
                    projectOrigin.Length > 0 && Kind(i) == "ProjectReference" && ItemPath(i).Equals(projectOrigin, StringComparison.OrdinalIgnoreCase) ||
                    packageId.Length > 0 && Kind(i) == "PackageReference" && Path.GetFileName(ItemPath(i)).Equals(packageId, StringComparison.OrdinalIgnoreCase) ||
                    Kind(i) == "DeclaredReference" && Path.GetFileNameWithoutExtension(ItemPath(i)).Equals(record.Identity.Name, StringComparison.OrdinalIgnoreCase)).ToArray();
                string Setting(string name)
                {
                    string own = Meta(runtime, name);
                    var values = declared.Select(i => Meta(i, name)).Append(own).Where(v => v.Length > 0).Distinct(StringComparer.Ordinal).ToArray();
                    if (values.Length > 1) throw new InvalidDataException("Conflicting " + name + " for " + record.Identity.Name);
                    return values.SingleOrDefault() ?? "";
                }
                record.Role = Setting("DtmApiRole");
                if (record.Role.Length == 0) record.Role = "private-managed";
                if (record.Role is not ("private-managed" or "shared-contract")) throw new InvalidDataException("Invalid DtmApiRole for " + record.Identity.Name);
                record.Distribution = Setting("DtmApiDistribution");
                if (record.Distribution is not ("self-authored" or "licensed-third-party"))
                    throw new InvalidDataException("Set DtmApiDistribution on the Reference/ProjectReference/PackageReference for " + record.Identity.Name);
                record.Path = (record.Role == "shared-contract" ? "lib/shared/" : "lib/private/") + Path.GetFileName(finalRuntime);
                record.Source = packageId.Length > 0 ? "nuget:" + packageId + "/" + Meta(runtime, "NuGetPackageVersion") : "standard-project:" + record.Identity.Name;
                string captured = snapshot.Copy(finalRuntime, record.Path);
                if (PathSafety.Sha256File(captured) != record.Sha256) throw new InvalidDataException("Runtime output changed during capture: " + finalRuntime);
                foreach (var companion in items.Where(i => Kind(i) == "Runtime" && Path.GetFileNameWithoutExtension(ItemPath(i)) == record.Identity.Name && Path.GetExtension(ItemPath(i)) is ".pdb" or ".xml"))
                {
                    string companionPath = Path.ChangeExtension(finalRuntime, Path.GetExtension(ItemPath(companion)));
                    snapshot.Copy(companionPath, Path.ChangeExtension(record.Path, Path.GetExtension(companionPath)));
                }
                if (File.Exists(Path.ChangeExtension(captured, ".pdb")))
                {
                    var symbols = SymbolInspector.Execute(ParsedCommand.Parse(new[] { "symbols", captured }));
                    if (!symbols.Success) throw new InvalidDataException("Runtime library symbols mismatch: " + record.Identity.Name);
                    snapshot.DiagnosticSymbols.Add(record.Path, File.ReadAllBytes(Path.ChangeExtension(captured, ".pdb")));
                }
                var licenses = new SortedDictionary<string, string>(StringComparer.Ordinal);
                foreach (string license in Setting("DtmApiLicenseFiles").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    string path = Path.GetFullPath(license, context.RootPath);
                    string target = "licenses/" + record.Identity.Name + "/" + Path.GetFileName(path);
                    if (new FileInfo(path).Length == 0) throw new InvalidDataException("Empty license: " + path);
                    licenses.Add(target, snapshot.Copy(path, target));
                }
                if (record.Distribution == "licensed-third-party" && licenses.Count == 0) throw new InvalidDataException("DtmApiLicenseFiles is required for " + record.Identity.Name);
                record.LicenseFiles = licenses.Keys.ToArray();
                context.BuiltLibraries.Add(new ManagedPackageInput(captured, record, licenses));
            }
            foreach (var content in items.Where(i => Kind(i) == "Content"))
            {
                string target = Meta(content, "DtmApiPackagePath").Replace('\\', '/');
                string captured = snapshot.Copy(ItemPath(content), target);
                snapshot.Content.Add(new ContentFile(target, File.ReadAllBytes(captured)));
            }
            snapshot.VerifyStable();
            if (ManagedPackageReferences.UsesContract(context)) ManagedPackageReferences.ValidateCompiledEntry(context, snapshot.EntryPath);
            else
            {
                if (context.BuiltLibraries.Count > 0) throw new InvalidDataException("This API target/package format does not support runtime libraries.");
                ManagedAssemblyInspector.ValidateCodeMod(snapshot.EntryPath, context.CodeModKind,
                    context.CodeModKind == AuthorCodeModKind.Advanced ? AdvancedReferenceAssets.Resolve(context, gameRoot).Policy.References.Select(r => r.AssemblyName) : Array.Empty<string>(), context.AuthorProject.AssemblyName);
            }
            var compileReferences = items.Where(i => Kind(i) == "Reference").Select(ItemPath).Where(File.Exists);
            var runtimeNames = context.BuiltLibraries.Select(l => l.Record.Identity.Name).Append(context.AuthorProject.AssemblyName).ToHashSet(StringComparer.OrdinalIgnoreCase);
            ManagedRuntimeSurface.Verify(context.BuiltLibraries.Select(l => l.SourcePath).Prepend(snapshot.EntryPath),
                compileReferences.Where(path => !runtimeNames.Contains(Path.GetFileNameWithoutExtension(path))));
            return snapshot;
        }
        catch { snapshot.Dispose(); throw; }
    }

    private string Copy(string source, string relative)
    {
        string target = PathSafety.ResolveUnderRoot(directory, relative, "standard build package item");
        if (!files.TryAdd(relative, (source, ""))) throw new InvalidDataException("Package item collision: " + relative);
        PathSafety.RejectReparsePoints(Path.GetDirectoryName(source)!, new[] { source });
        // Deny writes/deletes while each file is copied; a complete second hash pass
        // detects changes between file acquisitions before accepting the snapshot.
        using (var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            using var output = new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            input.CopyTo(output);
        }
        files[relative] = (source, PathSafety.Sha256File(target));
        return target;
    }

    private void VerifyStable()
    {
        foreach (var file in files.Values)
            if (PathSafety.Sha256File(file.Source) != file.Hash) throw new InvalidDataException("Build output changed during capture: " + file.Source);
    }

    public void Dispose()
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
    }
}
