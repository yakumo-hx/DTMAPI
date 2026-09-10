using System.Security.Cryptography;
using NuGet.Frameworks;
using NuGet.Packaging;

namespace DTMAPI.AuthorSdk;

internal static class PackageAssetSelection
{
    private static readonly NuGetFramework Target = NuGetFramework.ParseFolder("netstandard2.0");
    internal static string[] Select(PackageArchiveReader reader, string identity, IDictionary<string, string>? report)
    {
        string[] files = reader.GetFiles().OrderBy(p => p, StringComparer.Ordinal).ToArray();
        var decisions = new SortedDictionary<string, string>(StringComparer.Ordinal);
        var unsupported = new List<string>();
        bool Placeholder(string path) => Path.GetFileName(path) == "_._";
        FrameworkSpecificGroup? Nearest(IEnumerable<FrameworkSpecificGroup> groups)
        {
            var all = groups.ToArray(); var nearest = new FrameworkReducer().GetNearest(Target, all.Select(g => g.TargetFramework));
            return all.SingleOrDefault(g => g.TargetFramework.Equals(nearest));
        }
        FrameworkSpecificGroup? Group(string folder, IEnumerable<FrameworkSpecificGroup> groups, bool supported)
        {
            var all = groups.ToArray(); var selected = Nearest(all);
            foreach (var group in all)
            foreach (string file in group.Items)
            {
                bool applicable = ReferenceEquals(group, selected);
                bool unknownExecutionAsset = supported && !Placeholder(file) && Path.GetExtension(file).ToLowerInvariant() is not (".dll" or ".xml" or ".pdb");
                decisions[file] = !applicable ? "excluded: TFM group not selected for netstandard2.0"
                    : Placeholder(file) ? "excluded: selected empty-group placeholder"
                    : supported ? "selected: " + folder + "/" + group.TargetFramework.GetShortFolderName()
                    : "unsupported: applicable " + folder + "/" + group.TargetFramework.GetShortFolderName();
                if (applicable && !Placeholder(file) && (!supported || unknownExecutionAsset)) { decisions[file] = "unsupported: selected " + folder + " asset kind"; unsupported.Add(file); }
            }
            return selected;
        }
        var lib = Group("lib", reader.GetLibItems(), true);
        var reference = Group("ref", reader.GetItems("ref"), true);
        Group("build", reader.GetBuildItems(), false);
        Group("buildTransitive", reader.GetItems("buildTransitive"), false);
        Group("content", reader.GetContentItems(), false);
        Group("tools", reader.GetToolItems(), false);
        foreach (string file in files.Where(f => !decisions.ContainsKey(f)))
        {
            string[] parts = file.Split('/'); string top = parts[0].ToLowerInvariant();
            if (Placeholder(file)) { decisions[file] = "excluded: empty asset placeholder"; continue; }
            if (top == "buildmultitargeting") { decisions[file] = "excluded: single TargetFramework has no outer multi-target build"; continue; }
            if (top == "runtimes" && parts.Length >= 5 && parts[2] == "lib")
            {
                var framework = NuGetFramework.ParseFolder(parts[3]);
                if (!framework.IsUnsupported && !DefaultCompatibilityProvider.Instance.IsCompatible(Target, framework))
                { decisions[file] = "excluded: RID library TFM is incompatible with netstandard2.0"; continue; }
            }
            if (top == "contentfiles" && parts.Length >= 4)
            {
                var framework = NuGetFramework.ParseFolder(parts[2]);
                if (parts[1] is not ("cs" or "any") || (!framework.IsUnsupported && !DefaultCompatibilityProvider.Instance.IsCompatible(Target, framework)))
                { decisions[file] = "excluded: content language/TFM is not applicable"; continue; }
            }
            if (top is "runtimes" or "native" or "analyzers" or "contentfiles" or "build" or "buildtransitive" or "content" or "tools" or "ref" or "lib")
            { decisions[file] = "unsupported: applicable or unrecognized " + top + " asset model"; unsupported.Add(file); }
            else decisions[file] = "excluded: package metadata/documentation (not an execution asset)";
        }
        void Report() { if (report != null) foreach (var item in decisions) report["asset." + identity + "/" + item.Key] = item.Value; }
        Report();
        if (unsupported.Count != 0) throw new InvalidDataException("restore-unsupported-assets: " + identity + "/" + unsupported[0] + "; current asset model does not execute or silently discard applicable build/generator/native/RID/content assets.");
        if (lib == null || !lib.TargetFramework.Equals(Target)) throw new InvalidDataException("restore-unsupported-tfm: current execution asset model requires an explicit netstandard2.0 lib group: " + identity);
        string[] assets = lib.Items.Where(p => p.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)).OrderBy(p => p, StringComparer.Ordinal).ToArray();
        if (assets.Length is 0 or > 32 || assets.Any(p => p.Split('/').Length != 3)) throw new InvalidDataException("restore-unsupported-lib-group: empty/satellite/oversized selected lib group: " + identity);
        if (reference != null)
        {
            string[] refs = reference.Items.Where(p => p.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)).OrderBy(p => p, StringComparer.Ordinal).ToArray();
            bool same = reference.TargetFramework.Equals(Target) && refs.Select(Path.GetFileName).SequenceEqual(assets.Select(Path.GetFileName), StringComparer.Ordinal);
            if (same)
                for (int i = 0; i < refs.Length; i++)
                { using var left = reader.GetStream(refs[i]); using var right = reader.GetStream(assets[i]); if (!SHA256.HashData(left).SequenceEqual(SHA256.HashData(right))) { same = false; break; } }
            if (!same) throw new InvalidDataException("restore-unsupported-ref-runtime-model: " + identity + "; selected ref/lib must be the same netstandard2.0 execution bytes; separate reference assemblies belong to the later asset model.");
        }
        else
        {
            var compile = Nearest(reader.GetReferenceItems());
            if (compile == null || !compile.Items.Where(p => p.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)).OrderBy(p => p, StringComparer.Ordinal).SequenceEqual(assets, StringComparer.Ordinal))
                throw new InvalidDataException("restore-unsupported-ref-runtime-model: " + identity + "; nuspec compile filtering differs from the execution lib set.");
        }
        return assets;
    }
}
