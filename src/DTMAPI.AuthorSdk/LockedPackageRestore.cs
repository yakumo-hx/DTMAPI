using DTMAPI.Authoring.Contracts;
using DTMAPI.Internal.Authoring;
using NuGet.Common;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using System.Text.Json;
using System.Security.Cryptography;
using System.Threading;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace DTMAPI.AuthorSdk;

// NuGet owns downloads, nuspec/TFM/version parsing and content hash semantics.
// This adapter consumes an existing NuGet lock; it never resolves floating
// versions or evaluates package build assets. Build/pack only call ReadInputs.
internal static class LockedPackageRestore
{
    private static readonly NuGetFramework Framework = NuGetFramework.ParseFolder("netstandard2.0");
    private sealed record Locked(string Id, NuGetVersion Version, string Hash, bool Direct, IReadOnlyDictionary<string, string> Dependencies);

    internal static async Task<CommandReport> Execute(ParsedCommand command)
    {
        command.RequireOnlyOptions("offline");
        if (command.Positionals.Count != 1 || !bool.TryParse(command.Option("offline", "false"), out bool offline))
            throw new CommandLineException("restore requires a project directory and optional --offline true|false.");
        string root = PathSafety.FullPath(command.Positionals[0]);
        var report = new CommandReport { Command = "restore", RootPath = root };
        try
        {
            // Loading does not validate cache presence: restore is the operation
            // that makes those inputs available before normal project validation.
            AuthorProjectContext context = ProjectValidator.LoadForRestore(root);
            var graph = ProjectGraph.LoadForRestore(context);
            foreach (var node in graph)
            {
                if (node.AuthorProject.Build?.Restore is not { } settings) continue;
                var packages = ReadLock(node, settings);
                string cacheRoot = CacheRoot(node, settings);
                foreach (var package in packages)
                {
                    string destination = PackagePath(cacheRoot, package);
                    ProjectGraph.RequireUnder(cacheRoot, destination);
                    if (File.Exists(destination)) { ValidatePackage(destination, package, packages, report.Values); continue; }
                    if (offline) throw new InvalidDataException("restore-cache-missing: " + package.Id + " " + package.Version + "; run explicit online restore with the declared source.");
                    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                    string temporary = destination + ".tmp-" + Guid.NewGuid().ToString("N");
                    try
                    {
                        bool found = false;
                        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                        foreach (string source in settings.Sources)
                        {
                            string selected = ValidateSource(node.RootPath, source);
                            var repository = Repository.Factory.GetCoreV3(selected);
                            var resource = await repository.GetResourceAsync<FindPackageByIdResource>(timeout.Token)
                                ?? throw new InvalidDataException("Source does not provide package download capability.");
                            using var stream = new FileStream(temporary, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
                            using var sourceCache = new SourceCacheContext { NoCache = true, DirectDownload = true };
                            found = await resource.CopyNupkgToStreamAsync(package.Id, package.Version, stream, sourceCache, NullLogger.Instance, timeout.Token);
                            if (stream.Length > 64 * 1024 * 1024) throw new InvalidDataException("Package exceeds 64 MiB bound.");
                            if (found) break;
                        }
                        if (!found) throw new InvalidDataException("Locked package unavailable from the explicit sources: " + package.Id);
                        ValidatePackage(temporary, package, packages, report.Values);
                        File.Move(temporary, destination, false);
                    }
                    finally { if (File.Exists(temporary)) File.Delete(temporary); }
                }
                var inputs = ReadInputs(node);
                report.FileCount += inputs.Count;
                if (packages.Count != 0) report.Values["lock." + node.AuthorProject.AssemblyName] = PathSafety.Sha256File(PathSafety.ResolveUnderRoot(node.RootPath, settings.LockFile, "lockFile"));
            }
            report.Success = true;
            report.TargetRuntimeVersion = context.ApiTarget;
            report.Diagnostics.Add(new AuthorDiagnostic { Code = "SDK700", Severity = DiagnosticSeverity.Info, Message = "Locked NuGet packages verified; pure managed netstandard2.0 assets prepared. Build/pack remain offline; no package targets or generators executed." });
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or JsonException or ArgumentException or FormatException or KeyNotFoundException or InvalidOperationException or NuGetProtocolException or OperationCanceledException or BadImageFormatException)
        {
            report.Diagnostics.Add(new AuthorDiagnostic { Code = "SDK701", Severity = DiagnosticSeverity.Error, Message = ex.Message,
                Guidance = "Keep direct versions exact and retain the NuGet-generated packages.lock.json. Check every transitive package, license input and supported asset type before retrying explicit restore." });
        }
        return report;
    }

    internal static IReadOnlyList<ManagedPackageInput> ReadInputs(AuthorProjectContext context)
    {
        if (context.AuthorProject.Build?.Restore is not { } settings) return Array.Empty<ManagedPackageInput>();
        var packages = ReadLock(context, settings);
        string cacheRoot = CacheRoot(context, settings);
        var result = new List<ManagedPackageInput>();
        foreach (var package in packages)
        {
            string path = PackagePath(cacheRoot, package);
            ProjectGraph.RequireUnder(cacheRoot, path);
            if (!File.Exists(path)) throw new InvalidDataException("restore-cache-missing: " + package.Id + "; build/pack do not download. Run dtmapi-author restore explicitly.");
            var files = ValidatePackage(path, package, packages);
            var licenses = new SortedDictionary<string, string>(StringComparer.Ordinal);
            if (!settings.LicenseFiles.TryGetValue(package.Id, out var licenseFiles) || licenseFiles.Count == 0)
                throw new InvalidDataException("Every locked package needs explicit local licenseFiles: " + package.Id);
            foreach (string license in licenseFiles)
            {
                string source = context.OrdinaryLibrary == null ? PathSafety.ResolveUnderRoot(context.RootPath, license, "package license") : Path.GetFullPath(license, context.RootPath);
                ProjectGraph.RequireUnder(context.OrdinaryLibrary?.Workspace ?? context.RootPath, source);
                if (!File.Exists(source) || new FileInfo(source).Length == 0) throw new InvalidDataException("Package license missing/empty: " + package.Id);
                string target = "licenses/" + package.Id + "/" + Path.GetFileName(source);
                if (!licenses.TryAdd(target, source)) throw new InvalidDataException("Package license basename collision: " + package.Id);
            }
            using var reader = new PackageArchiveReader(path);
            foreach (string asset in files)
            {
                using var input = reader.GetStream(asset);
                byte[] data = ReadAsset(input);
                RequirePureManaged(data, package.Id + "/" + package.Version.ToNormalizedString() + "/" + asset);
                var record = PackagePortableMetadata.Inspect(data);
                if (record.TargetFramework != ".NETStandard,Version=v2.0" || PackageHostReferences.IsReserved(record.Identity.Name) || Path.GetFileName(asset) != record.Identity.Name + ".dll")
                    throw new InvalidDataException("Unsupported package assembly TFM, reserved host identity or filename: " + package.Id + "/" + asset);
                string local = Path.Combine(cacheRoot, "assets", PathSafety.Sha256Bytes(data), record.Identity.Name + ".dll");
                ProjectGraph.RequireUnder(cacheRoot, local);
                if (File.Exists(local))
                {
                    if (PathSafety.Sha256File(local) != record.Sha256) throw new InvalidDataException("Restored asset cache was modified: " + package.Id);
                }
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(local)!);
                    using var output = new FileStream(local, FileMode.CreateNew, FileAccess.Write, FileShare.None); output.Write(data);
                }
                record.Path = "lib/private/" + record.Identity.Name + ".dll"; record.Role = "private-managed";
                record.Distribution = "licensed-third-party"; record.Source = "nuget:" + package.Id + "/" + package.Version.ToNormalizedString();
                record.LicenseFiles = licenses.Keys.ToArray();
                result.Add(new ManagedPackageInput(local, record, licenses));
            }
        }
        if (result.Select(r => r.Record.Identity.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != result.Count)
            throw new InvalidDataException("Locked packages contain conflicting assembly simple names.");
        ManagedPackageReferences.RequireClosure(result.Select(r => r.Record).ToArray());
        return result;
    }

    private static IReadOnlyList<Locked> ReadLock(AuthorProjectContext context, AuthorRestoreSettings settings)
    {
        if (settings.Packages?.Count == 0 && context.AuthorProject.Build?.ProjectReferences.Count > 0 && settings.Sources?.Count is > 0 and <= 8)
        { foreach (string source in settings.Sources) _ = ValidateSource(context.RootPath, source); return Array.Empty<Locked>(); }
        if (!ManagedPackageReferences.UsesContract(context) || settings.Sources == null || settings.Sources.Count is < 1 or > 8 || settings.Packages == null || settings.Packages.Count is < 1 or > 32 || settings.LicenseFiles == null)
            throw new InvalidDataException("restore requires schema 3, 1..8 explicit sources and 1..32 exact direct packages.");
        foreach (string source in settings.Sources) _ = ValidateSource(context.RootPath, source);
        string path = PathSafety.ResolveUnderRoot(context.RootPath, settings.LockFile, "lockFile"); ProjectGraph.RequireUnder(context.RootPath, path);
        if (!File.Exists(path) || new FileInfo(path).Length > 1024 * 1024) throw new InvalidDataException("A NuGet-generated packages.lock.json (at most 1 MiB) is required before restore.");
        using var document = JsonDocument.Parse(File.ReadAllBytes(path));
        Unique(document.RootElement);
        var root = document.RootElement;
        if (root.GetProperty("version").GetInt32() is not (1 or 2)) throw new InvalidDataException("Unsupported NuGet lock version.");
        var frameworks = root.GetProperty("dependencies").EnumerateObject().ToArray();
        if (frameworks.Length != 1 || !NuGetFramework.Parse(frameworks[0].Name).Equals(Framework)) throw new InvalidDataException("Lock must contain exactly netstandard2.0 without RID targets.");
        var packages = new List<Locked>();
        foreach (var entry in frameworks[0].Value.EnumerateObject())
        {
            if (packages.Count >= 64 || !System.Text.RegularExpressions.Regex.IsMatch(entry.Name, @"^[A-Za-z0-9][A-Za-z0-9_.-]{0,99}$")) throw new InvalidDataException("Locked package ID/count unsupported.");
            var value = entry.Value; string type = value.GetProperty("type").GetString()!;
            if (type is not ("Direct" or "Transitive")) throw new InvalidDataException("Lock project/platform dependencies must be represented by SDK ProjectReference/frozen BCL, not package restore.");
            var version = NuGetVersion.Parse(value.GetProperty("resolved").GetString()!);
            string hash = value.GetProperty("contentHash").GetString()!;
            if (Convert.FromBase64String(hash).Length != 64) throw new InvalidDataException("Lock contentHash must be NuGet SHA-512.");
            if (type == "Direct")
            {
                if (!settings.Packages.TryGetValue(entry.Name, out string? requested) || NuGetVersion.Parse(requested) != version) throw new InvalidDataException("Direct package differs from SDK restore settings: " + entry.Name);
                var range = VersionRange.Parse(value.GetProperty("requested").GetString()!);
                if (range.IsFloating || !range.IsMinInclusive || !range.IsMaxInclusive || range.MinVersion != version || range.MaxVersion != version)
                    throw new InvalidDataException("Direct NuGet lock requests must be exact [version], not floating/ranged: " + entry.Name);
            }
            var dependencies = value.TryGetProperty("dependencies", out var dependenciesNode)
                ? dependenciesNode.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString()!, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            packages.Add(new Locked(entry.Name, version, hash, type == "Direct", dependencies));
        }
        if (packages.Count(p => p.Direct) != settings.Packages.Count) throw new InvalidDataException("Direct package set differs from lock.");
        foreach (var package in packages)
            foreach (var dependency in package.Dependencies)
                if (packages.SingleOrDefault(p => p.Id.Equals(dependency.Key, StringComparison.OrdinalIgnoreCase)) is not { } target || !VersionRange.Parse(dependency.Value).Satisfies(target.Version))
                    throw new InvalidDataException("Locked transitive version is missing/incompatible: " + package.Id + " -> " + dependency.Key);
        var reachable = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void Visit(Locked package) { if (!reachable.Add(package.Id)) return; foreach (var dep in package.Dependencies.Keys) Visit(packages.Single(p => p.Id.Equals(dep, StringComparison.OrdinalIgnoreCase))); }
        foreach (var direct in packages.Where(p => p.Direct)) Visit(direct);
        if (reachable.Count != packages.Count) throw new InvalidDataException("Lock contains unrelated transitive packages.");
        return packages.OrderBy(p => p.Id, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static string[] ValidatePackage(string path, Locked package, IReadOnlyList<Locked> packages, IDictionary<string, string>? selection = null)
    {
        if (new FileInfo(path).Length > 64 * 1024 * 1024) throw new InvalidDataException("Package exceeds 64 MiB.");
        using var reader = new PackageArchiveReader(path);
        string hash = reader.GetContentHash(CancellationToken.None, () => Convert.ToBase64String(SHA512.HashData(File.ReadAllBytes(path))));
        if (hash != package.Hash) throw new InvalidDataException("restore-content-hash-mismatch: " + package.Id);
        var identity = reader.GetIdentity();
        if (!identity.Id.Equals(package.Id, StringComparison.OrdinalIgnoreCase) || identity.Version != package.Version) throw new InvalidDataException("Locked NuGet package identity mismatch.");
        string[] files = reader.GetFiles().ToArray();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string file in files)
        {
            if (!seen.Add(file) || file.Contains('\\') || file.Split('/').Any(p => p is "." or ".." || p.Contains(':'))) throw new InvalidDataException("Ambiguous or traversing package entry.");
        }
        string[] assets = PackageAssetSelection.Select(reader, package.Id + "/" + package.Version.ToNormalizedString(), selection);
        var dependencyGroups = reader.GetPackageDependencies().ToArray();
        var nearest = new FrameworkReducer().GetNearest(Framework, dependencyGroups.Select(g => g.TargetFramework));
        if (selection != null) selection["dependencies." + package.Id + "/" + package.Version.ToNormalizedString()] = nearest?.GetShortFolderName() ?? "none";
        var dependencies = dependencyGroups.FirstOrDefault(g => g.TargetFramework.Equals(nearest))?.Packages.ToArray() ?? Array.Empty<NuGet.Packaging.Core.PackageDependency>();
        if (dependencies.Length != package.Dependencies.Count) throw new InvalidDataException("NuGet lock omits/adds nuspec dependencies: " + package.Id);
        foreach (var dependency in dependencies)
        {
            var selected = packages.SingleOrDefault(p => p.Id.Equals(dependency.Id, StringComparison.OrdinalIgnoreCase));
            if (selected == null || !package.Dependencies.ContainsKey(dependency.Id) || !dependency.VersionRange.Satisfies(selected.Version)) throw new InvalidDataException("Nuspec dependency differs from locked closure: " + package.Id + " -> " + dependency.Id);
        }
        return assets;
    }

    private static string CacheRoot(AuthorProjectContext context, AuthorRestoreSettings settings)
    {
        string root = PathSafety.ResolveUnderRoot(context.RootPath, settings.CacheDirectory, "restore cache");
        if (!PathSafety.RelativePath(context.RootPath, root).StartsWith("obj/", StringComparison.Ordinal)) throw new InvalidDataException("Restore cache must remain under project obj/.");
        ProjectGraph.RequireUnder(context.RootPath, root); return root;
    }
    private static string PackagePath(string cache, Locked package) => Path.Combine(cache, package.Id.ToLowerInvariant(), package.Version.ToNormalizedString().ToLowerInvariant(), "package.nupkg");
    private static string ValidateSource(string root, string source)
    {
        if (Uri.TryCreate(source, UriKind.Absolute, out var uri) && !uri.IsFile)
        {
            if (uri.Scheme != "https" || uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0) throw new InvalidDataException("Package sources require HTTPS without embedded credentials/query, or an explicit local directory.");
            return uri.AbsoluteUri;
        }
        string path = Path.GetFullPath(source, root);
        if (!Directory.Exists(path)) throw new InvalidDataException("Local NuGet source directory missing.");
        return path;
    }
    private static void Unique(JsonElement node)
    {
        if (node.ValueKind != JsonValueKind.Object) return;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in node.EnumerateObject()) { if (!seen.Add(property.Name)) throw new InvalidDataException("Duplicate lock property."); Unique(property.Value); }
    }

    private static byte[] ReadAsset(Stream input)
    {
        using var output = new MemoryStream(); byte[] buffer = new byte[81920]; int count;
        while ((count = input.Read(buffer, 0, buffer.Length)) != 0)
        {
            if (output.Length + count > 16 * 1024 * 1024) throw new InvalidDataException("Expanded managed asset exceeds 16 MiB.");
            output.Write(buffer, 0, count);
        }
        return output.ToArray();
    }

    private static void RequirePureManaged(byte[] bytes, string id)
    {
        using var stream = new MemoryStream(bytes, false); using var pe = new PEReader(stream);
        if (!pe.HasMetadata || pe.PEHeaders.CorHeader == null || (pe.PEHeaders.CorHeader.Flags & CorFlags.ILOnly) == 0)
            throw new InvalidDataException("restore-native-assembly: " + id);
        DTMAPI.Tooling.Metadata.ManagedMethodMetadata.RequireManagedMethods(bytes, id);
    }
}
