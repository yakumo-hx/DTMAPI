using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DTMAPI.PublicSource;

internal static partial class Boundary
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public static void Validate(Policy policy, IReadOnlyDictionary<string, FileBlob> files, bool links = true)
    {
        Safety.UniquePaths(files.Keys);
        foreach ((string path, FileBlob file) in files)
        {
            if (policy.ClassifyTarget(path, file.Mode).Action != "include") throw new InvalidDataException("Forbidden public path: " + path);
            if (policy.BinaryHashes.TryGetValue(path, out string? expected))
            {
                if (file.Hash != expected) throw new InvalidDataException("Binary digest mismatch: " + path);
                continue;
            }
            byte[] bytes = file.Bytes;
            if ((bytes.Length >= 2 && bytes[0] == 'M' && bytes[1] == 'Z') || bytes.Contains((byte)0)) throw new InvalidDataException("Unlicensed binary payload: " + path);
            string text;
            try { text = StrictUtf8.GetString(bytes); } catch (DecoderFallbackException) { throw new InvalidDataException("Undeclared binary/non-UTF8 source: " + path); }
            if (text.StartsWith("version https://git-lfs.github.com/spec/v1", StringComparison.Ordinal)) throw new InvalidDataException("Unresolved LFS pointer: " + path);
            if (Regex.IsMatch(text, "-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----|(?:gh[pousr]_[A-Za-z0-9]{30,}|AKIA[A-Z0-9]{16})")) throw new InvalidDataException("Potential credential/private key: " + path);
            if (Path.GetFileName(path) == ".gitattributes" && Regex.IsMatch(text, @"(?m)^[^#\r\n]*\b(?:filter=|merge=(?!text\b|binary\b|union\b))")) throw new InvalidDataException("External attribute filter/merge driver: " + path);
        }
        foreach (ExactRule required in policy.Exact.Values.Where(rule => rule.Required && rule.Action == "include")) Require(files, required.Path);
        foreach (Mapping mapping in policy.Mappings.Values.Where(mapping => mapping.Required)) Require(files, mapping.Target);
        ValidateLicenses(policy, files);
        // A merged candidate cannot weaken its own boundary. The trusted policy is selected separately.
        if (Require(files, Policy.RepositoryPath).Hash != new FileBlob("100644", policy.Bytes).Hash) throw new InvalidDataException("Candidate changes the trusted export policy; review and regenerate N before merging.");
        if (links) ValidateLinks(files);
    }

    internal static void ValidateLicenses(Policy policy, IReadOnlyDictionary<string, FileBlob> files)
    {
        foreach ((string path, string hash) in policy.BinaryHashes) if (Require(files, path).Hash != hash) throw new InvalidDataException("Binary digest mismatch: " + path);
        foreach ((string path, string inventory) in policy.BinaryInventories)
        {
            using JsonDocument record = Policy.ParseStrict(Require(files, inventory).Bytes);
            JsonElement declared = record.RootElement;
            string archivePath = declared.GetProperty("archivePath").GetString()!;
            string sourcePath = declared.GetProperty("correspondingSource").GetProperty("path").GetString()!;
            if (path != archivePath && path != sourcePath) throw new InvalidDataException("Binary input is not covered by its license inventory: " + path);
        }
        foreach (string inventory in policy.BinaryInventories.Values.Distinct(StringComparer.Ordinal)) ValidateInventory(files, inventory);
    }

    private static FileBlob Require(IReadOnlyDictionary<string, FileBlob> files, string path) => files.TryGetValue(path, out FileBlob? value) ? value : throw new InvalidDataException("Required public input is missing: " + path);

    private static void ValidateInventory(IReadOnlyDictionary<string, FileBlob> files, string inventoryPath)
    {
        using JsonDocument document = Policy.ParseStrict(Require(files, inventoryPath).Bytes);
        JsonElement root = document.RootElement;
        if (root.GetProperty("schemaVersion").GetInt32() != 1) throw new InvalidDataException("Unsupported license inventory.");
        FileBlob archive = Require(files, root.GetProperty("archivePath").GetString()!);
        if (archive.Hash != root.GetProperty("archiveSha256").GetString()) throw new InvalidDataException("Fixture archive hash mismatch.");
        var components = new HashSet<string>(StringComparer.Ordinal);
        foreach (JsonElement component in root.GetProperty("components").EnumerateArray())
        {
            if (!components.Add(component.GetProperty("id").GetString()!)) throw new InvalidDataException("Duplicate license component.");
            if (component.GetProperty("license").GetString() is not ("MIT" or "LGPL-2.1-only")) throw new InvalidDataException("Unreviewed license component.");
            if (Require(files, component.GetProperty("licensePath").GetString()!).Hash != component.GetProperty("licenseSha256").GetString()) throw new InvalidDataException("License text digest mismatch.");
        }
        var members = root.GetProperty("members").EnumerateArray().ToDictionary(row => row.GetProperty("path").GetString()!, StringComparer.Ordinal);
        using var zip = new ZipArchive(new MemoryStream(archive.Bytes), ZipArchiveMode.Read);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (ZipArchiveEntry entry in zip.Entries)
        {
            if (entry.FullName.EndsWith('/')) continue;
            Policy.ValidatePath(entry.FullName);
            if (!seen.Add(entry.FullName) || !members.TryGetValue(entry.FullName, out JsonElement member) || !components.Contains(member.GetProperty("component").GetString()!)) throw new InvalidDataException("Unlicensed ZIP member: " + entry.FullName);
            using var output = new MemoryStream(); using (Stream input = entry.Open()) input.CopyTo(output);
            if (entry.Length != member.GetProperty("length").GetInt64() || new FileBlob("100644", output.ToArray()).Hash != member.GetProperty("sha256").GetString()) throw new InvalidDataException("Fixture ZIP member mismatch: " + entry.FullName);
        }
        if (seen.Count != members.Count) throw new InvalidDataException("Missing licensed ZIP members.");
        JsonElement source = root.GetProperty("correspondingSource");
        if (!source.GetProperty("includesBuildInputs").GetBoolean() || source.GetProperty("fileCount").GetInt32() <= 0 || source.GetProperty("submoduleCount").GetInt32() != 0 || Require(files, source.GetProperty("path").GetString()!).Hash != source.GetProperty("sha256").GetString()) throw new InvalidDataException("Complete corresponding source is missing or changed.");
    }

    public static void ValidateLinks(IReadOnlyDictionary<string, FileBlob> files)
    {
        var failures = new List<string>();
        foreach ((string path, FileBlob blob) in files.Where(pair => pair.Key.EndsWith(".md", StringComparison.OrdinalIgnoreCase)))
        {
            string text = Encoding.UTF8.GetString(blob.Bytes);
            // Code examples and provenance literals are not navigation. Both inline and reference links are checked.
            text = Regex.Replace(text, @"(?ms)^\s*```.*?^\s*```[^\r\n]*", "");
            IEnumerable<string> targets = Regex.Matches(text, @"\]\((<[^>]+>|[^\s)]+)(?:\s+[^)]*)?\)").Select(match => match.Groups[1].Value.Trim('<', '>'))
                .Concat(Regex.Matches(text, @"(?m)^\s*\[[^\]]+\]:\s*(<[^>]+>|\S+)").Select(match => match.Groups[1].Value.Trim('<', '>')));
            foreach (string target in targets)
            {
                if (target.StartsWith('#') || Regex.IsMatch(target, @"^[a-zA-Z][a-zA-Z0-9+.-]*:")) continue;
                string local = Uri.UnescapeDataString(target.Split('#', '?')[0]);
                if (local.Length == 0) continue;
                string resolved = Path.GetRelativePath("/public", Path.GetFullPath(Path.Combine("/public", Path.GetDirectoryName(path) ?? "", local))).Replace('\\', '/');
                if (resolved.StartsWith("../", StringComparison.Ordinal) || (!files.ContainsKey(resolved) && !files.Keys.Any(file => file.StartsWith(resolved.TrimEnd('/') + "/", StringComparison.Ordinal)))) failures.Add(path + " -> " + target);
            }
        }
        if (failures.Count > 0) throw new InvalidDataException("Broken public links (" + failures.Count + "):\n" + string.Join('\n', failures));
    }
}
