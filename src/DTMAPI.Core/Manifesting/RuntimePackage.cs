using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DTMAPI.Internal.Authoring;

namespace DTMAPI.Core.Manifesting
{
    // Scanning only records paths. Unused candidates are never opened or hashed.
    internal sealed class RuntimePackage
    {
        private readonly Dictionary<string, List<Candidate>> candidates = new Dictionary<string, List<Candidate>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, PackageAssemblyRecord> selected = new Dictionary<string, PackageAssemblyRecord>(StringComparer.OrdinalIgnoreCase);
        internal RuntimePackage(ManifestModel manifest, string root)
        {
            Manifest = manifest; Root = Path.GetFullPath(root);
            if (manifest.Type == "ContentPack") return;
            Add(manifest.EntryDll, "entry");
            string entryDirectory = Path.GetDirectoryName(Path.Combine(Root, PackageDependencyContract.Relative(manifest.EntryDll)));
            if (Directory.Exists(entryDirectory) && !entryDirectory.Equals(Root, StringComparison.OrdinalIgnoreCase))
                foreach (string path in Directory.EnumerateFiles(entryDirectory, "*.dll")) Add(Relative(path), "private-managed");
            foreach (string path in Directory.EnumerateFiles(Root, "*", SearchOption.TopDirectoryOnly))
                if (path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)) Add(Relative(path), "private-managed");
            Enumerate(Path.Combine(Root, "lib", "private"), "private-managed");
            Enumerate(Path.Combine(Root, "lib", "shared"), "shared-contract");
            foreach (var assembly in manifest.AssemblyModels ?? new List<ManifestAssemblyModel>()) Add(assembly.Path, assembly.Role);
            // Only path/role hints are useful from old metadata. Broken optional hints
            // cannot turn a generator receipt into a loading prerequisite.
            string hints = Path.Combine(Root, "dtmapi-dependencies.json");
            if (manifest.ManifestVersion == 0 && File.Exists(hints))
            {
                try
                {
                    var fields = PackageDependencyContract.Object(PackageDependencyContract.ReadJson(File.ReadAllBytes(hints)));
                    if (fields.TryGetValue("assemblies", out var rows))
                        foreach (var row in PackageDependencyContract.Array(rows))
                        {
                            var value = PackageDependencyContract.Object(row);
                            if (value.TryGetValue("path", out var path) && value.TryGetValue("role", out var role))
                                AddLegacyHint(PackageDependencyContract.Text(path), PackageDependencyContract.Text(role));
                        }
                }
                catch (Exception ex) when (ex is IOException || ex is InvalidDataException || ex is UnauthorizedAccessException || ex is System.Xml.XmlException || ex is System.Runtime.Serialization.SerializationException) { }
            }
        }
        internal ManifestModel Manifest { get; }
        internal string Root { get; }
        internal IEnumerable<PackageAssemblyRecord> Selected => selected.Values;
        internal bool HasCandidate(string name, bool sharedOnly = false) => candidates.TryGetValue(name, out var paths) && paths.Any(p => !sharedOnly || p.Role == "shared-contract" || p.Role == "entry");
        internal PackageAssemblyRecord SelectEntry() => ReadCandidate(new Candidate { Path = PackageDependencyContract.Relative(Manifest.EntryDll), Role = "entry" });
        internal PackageAssemblyRecord Select(string name, bool sharedOnly = false, string? requesterPath = null)
        {
            if (!candidates.TryGetValue(name, out var options)) throw new FileNotFoundException("dependency-file-missing: " + name);
            // Visibility is decided before probing. A declared provider does not
            // expose its private dependencies to the requesting Mod.
            var matches = options.Where(p => !sharedOnly || p.Role == "shared-contract" || p.Role == "entry")
                .GroupBy(p => Path.GetFullPath(Path.Combine(Root, p.Path)), StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First()).ToArray();
            if (matches.Length == 0) throw new FileNotFoundException("dependency-file-missing: " + name);
            string? directory = string.IsNullOrEmpty(requesterPath) ? null : Path.GetDirectoryName(Path.GetFullPath(requesterPath));
            string entryDirectory = Path.GetDirectoryName(Path.Combine(Root, PackageDependencyContract.Relative(Manifest.EntryDll)))!;
            int Priority(Candidate candidate)
            {
                string parent = Path.GetDirectoryName(Path.Combine(Root, candidate.Path))!;
                if (directory != null && parent.Equals(directory, StringComparison.OrdinalIgnoreCase)) return 0;
                if (parent.Equals(entryDirectory, StringComparison.OrdinalIgnoreCase)) return 1;
                if (parent.Equals(Root, StringComparison.OrdinalIgnoreCase)) return 2;
                if (candidate.Path.StartsWith("lib/private/", StringComparison.OrdinalIgnoreCase)) return 3;
                if (candidate.Path.StartsWith("lib/shared/", StringComparison.OrdinalIgnoreCase)) return 4;
                return 5;
            }
            int priority = matches.Min(Priority);
            var nearest = matches.Where(p => Priority(p) == priority).ToArray();
            if (nearest.Length != 1)
                throw new InvalidDataException("dependency-candidate-ambiguous: owner=" + Manifest.UniqueID + "; request=" + name +
                    "; requester=" + (requesterPath ?? "unknown") + "; candidates=" +
                    string.Join(", ", nearest.Select(p => p.Path).OrderBy(path => path, StringComparer.OrdinalIgnoreCase)));
            return ReadCandidate(nearest[0]);
        }
        private PackageAssemblyRecord ReadCandidate(Candidate candidate)
        {
            // Metadata belongs to this request, not a certificate that freezes the
            // candidate's bytes for the rest of the process.
            byte[] bytes = File.ReadAllBytes(PackageDependencyContract.ResolveFile(Root, candidate.Path));
            PackageAssemblyRecord record = DependencyPackageVerifier.Inspect(bytes);
            record.Path = candidate.Path; record.Role = candidate.Role;
            selected[candidate.Path] = record;
            AddName(record.Identity.Name, candidate);
            return record;
        }
        internal string Resolve(PackageAssemblyRecord record) => PackageDependencyContract.ResolveFile(Root, record.Path);
        internal static string FindRoot(string manifestPath)
        {
            string parent = Path.GetDirectoryName(Path.GetFullPath(manifestPath))!;
            return Path.GetFileName(parent).Equals("DTMAPI", StringComparison.OrdinalIgnoreCase) &&
                Path.GetFileName(Path.GetDirectoryName(parent)).Equals("Content", StringComparison.OrdinalIgnoreCase)
                ? Path.GetDirectoryName(Path.GetDirectoryName(parent))! : parent;
        }
        private string Relative(string path) => path.Substring(Root.TrimEnd(Path.DirectorySeparatorChar).Length + 1).Replace('\\', '/');
        private void Enumerate(string directory, string role)
        {
            if (!Directory.Exists(directory) || (File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0) return;
            foreach (string path in Directory.EnumerateFileSystemEntries(directory))
            {
                var attributes = File.GetAttributes(path);
                if ((attributes & FileAttributes.ReparsePoint) != 0) continue;
                if ((attributes & FileAttributes.Directory) != 0) Enumerate(path, role);
                else if (path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)) Add(Relative(path), role);
            }
        }
        private void AddLegacyHint(string path, string role)
        {
            if (role != "private-managed" && role != "shared-contract") return;
            try
            {
                PackageDependencyContract.ResolveFile(Root, path);
                string name = Path.GetFileNameWithoutExtension(path);
                // Optional old hints may describe a real extra location, but cannot
                // override or make an already discovered candidate ambiguous.
                if (candidates.TryGetValue(name, out var existing) && !existing.Any(c => c.Path.Equals(path, StringComparison.OrdinalIgnoreCase))) return;
                Add(path, role);
            }
            catch (Exception ex) when (ex is IOException || ex is InvalidDataException || ex is UnauthorizedAccessException) { }
        }
        private void Add(string path, string role)
        {
            if (string.IsNullOrEmpty(path)) return;
            path = PackageDependencyContract.Relative(path);
            AddName(Path.GetFileNameWithoutExtension(path), new Candidate { Path = path, Role = role });
        }
        private void AddName(string name, Candidate candidate)
        {
            if (!candidates.TryGetValue(name, out var list)) candidates.Add(name, list = new List<Candidate>());
            var existing = list.FirstOrDefault(p => p.Path.Equals(candidate.Path, StringComparison.OrdinalIgnoreCase));
            if (existing != null) { if (existing.Role != "entry" && (candidate.Role == "entry" || candidate.Role == "shared-contract")) existing.Role = candidate.Role; return; }
            list.Add(candidate);
        }
        private sealed class Candidate { internal string Path = ""; internal string Role = ""; }
    }
}
