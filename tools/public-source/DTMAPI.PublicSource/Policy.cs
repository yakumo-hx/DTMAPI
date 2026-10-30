using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DTMAPI.PublicSource;

internal sealed record OwnedRule(string Id, string Path, string Action, string[] Extensions, string[] ExcludedExtensions, string[] ExcludedDirectories);
internal sealed record ExactRule(string Id, string Path, string Action, bool Required);
internal sealed record Mapping(string Source, string Target, bool Required);
internal sealed record Disposition(string Action, string Target, string Rule);

internal sealed class Policy
{
    public const string RepositoryPath = "tools/public-source/export-policy.json";
    public byte[] Bytes { get; }
    public string Sha256 => Convert.ToHexString(SHA256.HashData(Bytes)).ToLowerInvariant();
    public List<OwnedRule> Trees { get; } = new();
    public Dictionary<string, ExactRule> Exact { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, Mapping> Mappings { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, string> BinaryHashes { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, string> BinaryInventories { get; } = new(StringComparer.Ordinal);
    public string[] ReservedSegments { get; }
    public string[] PrivateNames { get; }

    public Policy(byte[] bytes)
    {
        Bytes = bytes;
        using JsonDocument document = ParseStrict(bytes);
        JsonElement root = document.RootElement;
        Fields(root, "schemaVersion", "lifecycle", "intendedPath", "description", "matching", "pathSafety", "ownedTrees", "exactFiles", "rootMappings", "binaryInputs", "activationRequirements");
        if (root.GetProperty("schemaVersion").GetInt32() != 1 || root.GetProperty("lifecycle").GetString() != "active") throw new InvalidDataException("Export policy is not active schema 1.");
        if (root.GetProperty("intendedPath").GetString() != RepositoryPath) throw new InvalidDataException("Wrong export policy identity.");
        JsonElement matching = root.GetProperty("matching");
        Fields(matching, "pathSeparator", "caseSensitive", "precedence", "unknownRoot", "unknownComponent", "unknownIncludedFileType", "newDescendantWithinOwnedComponent", "unknownWithinExcludedTree", "duplicateOrConflictingRules");
        if (Text(matching, "pathSeparator") != "/" || !matching.GetProperty("caseSensitive").GetBoolean() || Text(matching, "unknownRoot") != "error" || Text(matching, "unknownComponent") != "error" || Text(matching, "unknownIncludedFileType") != "error" || Text(matching, "duplicateOrConflictingRules") != "error" || Text(matching, "newDescendantWithinOwnedComponent") != "inherit" || Text(matching, "unknownWithinExcludedTree") != "exclude") throw new InvalidDataException("Unsupported matching semantics.");
        if (!Strings(matching, "precedence").SequenceEqual(new[] { "pathSafety", "reservedOutput", "exactFileOrMapping", "longestOwnedTree", "unknownIsError" })) throw new InvalidDataException("Unsupported precedence.");
        JsonElement safety = root.GetProperty("pathSafety");
        Fields(safety, "allowedGitModes", "reject", "reservedSegments", "privateFileNames");
        if (!Strings(safety, "allowedGitModes").SequenceEqual(new[] { "100644", "100755" })) throw new InvalidDataException("Unsupported Git modes in policy.");
        ReservedSegments = Strings(safety, "reservedSegments");
        PrivateNames = Strings(safety, "privateFileNames");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var prefixes = new HashSet<string>(StringComparer.Ordinal);
        foreach (JsonElement row in root.GetProperty("ownedTrees").EnumerateArray())
        {
            Fields(row, "id", "paths", "action", "extensions", "excludedExtensions", "excludedDirectories", "reason");
            string id = Text(row, "id"), action = Action(row);
            if (!ids.Add(id)) throw new InvalidDataException("Duplicate rule id: " + id);
            foreach (string path in Strings(row, "paths"))
            {
                ValidatePath(path);
                if (!prefixes.Add(path)) throw new InvalidDataException("Conflicting owned tree: " + path);
                Trees.Add(new(id, path, action, row.TryGetProperty("extensions", out _) ? Strings(row, "extensions") : Array.Empty<string>(), row.TryGetProperty("excludedExtensions", out _) ? Strings(row, "excludedExtensions") : Array.Empty<string>(), row.TryGetProperty("excludedDirectories", out _) ? Strings(row, "excludedDirectories") : Array.Empty<string>()));
            }
        }
        foreach (JsonElement row in root.GetProperty("exactFiles").EnumerateArray())
        {
            Fields(row, "id", "paths", "action", "required", "reason");
            string id = Text(row, "id"), action = Action(row);
            if (!ids.Add(id)) throw new InvalidDataException("Duplicate rule id: " + id);
            foreach (string path in Strings(row, "paths"))
            {
                ValidatePath(path);
                if (!Exact.TryAdd(path, new(id, path, action, row.TryGetProperty("required", out var required) && required.GetBoolean()))) throw new InvalidDataException("Conflicting exact rule: " + path);
            }
        }
        var targets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (JsonElement row in root.GetProperty("rootMappings").EnumerateArray())
        {
            Fields(row, "source", "target", "required", "reverse", "replaceInternalSource");
            string source = Text(row, "source"), target = Text(row, "target");
            ValidatePath(source); ValidatePath(target);
            if (source != "docs/public-root/" + target || target is not ("README.md" or "README.en.md" or "CONTRIBUTING.md" or "CHANGELOG.md" or ".gitignore" or ".gitattributes") || !row.GetProperty("reverse").GetBoolean()) throw new InvalidDataException("Only reversible public root documents and Git configuration may be mapped.");
            if (Exact.ContainsKey(source) || !Mappings.TryAdd(source, new(source, target, row.GetProperty("required").GetBoolean())) || !targets.Add(target)) throw new InvalidDataException("Conflicting root mapping: " + source);
        }
        foreach (JsonElement row in root.GetProperty("binaryInputs").EnumerateArray())
        {
            Fields(row, "path", "sha256", "licenseInventory", "reason");
            string path = Text(row, "path"), hash = Text(row, "sha256");
            ValidatePath(path);
            string inventory = Text(row, "licenseInventory"); ValidatePath(inventory);
            if (Exact.ContainsKey(path) || Mappings.ContainsKey(path) || !Regex.IsMatch(hash, "^[a-f0-9]{64}$") || !BinaryHashes.TryAdd(path, hash)) throw new InvalidDataException("Unresolved or duplicate binary input: " + path);
            BinaryInventories.Add(path, inventory);
        }
        if (new[] { "README.md", "README.en.md", "CONTRIBUTING.md", "CHANGELOG.md" }.Any(target => !targets.Contains(target))) throw new InvalidDataException("All four root document mappings are required.");
        foreach (OwnedRule tree in Trees.Where(tree => tree.Action == "include"))
        {
            string[] extensions = tree.Extensions.Concat(tree.ExcludedExtensions).ToArray();
            if (tree.Extensions.Length == 0 || extensions.Distinct(StringComparer.OrdinalIgnoreCase).Count() != extensions.Length || extensions.Any(extension => !extension.StartsWith('.') || extension.Contains('/'))) throw new InvalidDataException("Invalid or conflicting extensions: " + tree.Id);
            foreach (string directory in tree.ExcludedDirectories)
            {
                ValidatePath(directory);
                if (directory.Contains('/')) throw new InvalidDataException("Excluded directory must be a single segment: " + directory);
            }
        }
    }

    public Disposition ClassifyTarget(string path, string mode)
    {
        Mapping? mapping = Mappings.Values.SingleOrDefault(row => row.Target == path);
        return Classify(mapping?.Source ?? path, mode) with { Target = path };
    }

    public string Reverse(string path) => Mappings.Values.SingleOrDefault(row => row.Target == path)?.Source ?? path;

    public Disposition Classify(string path, string mode)
    {
        ValidatePath(path);
        if (mode is not ("100644" or "100755")) throw new InvalidDataException("Unsupported Git mode: " + mode + " " + path);
        if (path.Split('/').Any(segment => ReservedSegments.Contains(segment, StringComparer.OrdinalIgnoreCase)) || PrivateNames.Contains(System.IO.Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)) return new("exclude", path, "reserved/private");
        if (Mappings.TryGetValue(path, out Mapping? mapping)) return new("include", mapping.Target, "root-document");
        if (Exact.TryGetValue(path, out ExactRule? exact)) return new(exact.Action, path, exact.Id);
        if (BinaryHashes.ContainsKey(path)) return new("include", path, "licensed-binary");
        OwnedRule? rule = Trees.Where(row => path.StartsWith(row.Path + "/", StringComparison.Ordinal)).OrderByDescending(row => row.Path.Length).FirstOrDefault();
        if (rule is null) throw new InvalidDataException("Unclassified source path: " + path);
        if (path[(rule.Path.Length + 1)..].Split('/').SkipLast(1).Any(segment => rule.ExcludedDirectories.Contains(segment, StringComparer.OrdinalIgnoreCase))) return new("exclude", path, rule.Id + ":internal-directory");
        if (rule.ExcludedExtensions.Contains(System.IO.Path.GetExtension(path), StringComparer.OrdinalIgnoreCase)) return new("exclude", path, rule.Id + ":excluded-extension");
        if (rule.Action == "include" && !rule.Extensions.Contains(System.IO.Path.GetExtension(path), StringComparer.OrdinalIgnoreCase)) throw new InvalidDataException("Unknown included file type: " + path);
        return new(rule.Action, path, rule.Id);
    }

    public static void ValidatePath(string path)
    {
        if (string.IsNullOrEmpty(path) || path.StartsWith('/') || path.Contains('\\') || path.Contains(':') || path.Contains('\0') || path.Any(char.IsControl)) throw new InvalidDataException("Unsafe repository path: " + path);
        foreach (string part in path.Split('/'))
            if (part is "" or "." or ".." || part.Equals(".git", StringComparison.OrdinalIgnoreCase) || part.EndsWith(' ') || part.EndsWith('.') || part.IndexOfAny(new[] {'<','>','"','|','?','*'}) >= 0 || Regex.IsMatch(part, "^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(?:\\.|$)", RegexOptions.IgnoreCase)) throw new InvalidDataException("Unsafe path component: " + path);
    }

    public static JsonDocument ParseStrict(byte[] bytes)
    {
        ReadOnlyMemory<byte> data = bytes;
        if (bytes.Length >= 3 && bytes[0] == 239 && bytes[1] == 187 && bytes[2] == 191) data = data[3..];
        JsonDocument document = JsonDocument.Parse(data);
        CheckDuplicates(document.RootElement);
        return document;
    }
    private static void CheckDuplicates(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (JsonProperty property in value.EnumerateObject())
            {
                if (!names.Add(property.Name)) throw new InvalidDataException("Duplicate JSON property: " + property.Name);
                CheckDuplicates(property.Value);
            }
        }
        else if (value.ValueKind == JsonValueKind.Array) foreach (JsonElement child in value.EnumerateArray()) CheckDuplicates(child);
    }
    private static void Fields(JsonElement row, params string[] allowed)
    {
        foreach (JsonProperty property in row.EnumerateObject()) if (!allowed.Contains(property.Name, StringComparer.Ordinal)) throw new InvalidDataException("Unknown policy field: " + property.Name);
    }
    private static string Text(JsonElement row, string name) => row.GetProperty(name).GetString() ?? throw new InvalidDataException("Empty policy field: " + name);
    private static string[] Strings(JsonElement row, string name) => row.GetProperty(name).EnumerateArray().Select(value => value.GetString() ?? throw new InvalidDataException("Non-string policy value.")).ToArray();
    private static string Action(JsonElement row) => Text(row, "action") is string action && action is "include" or "exclude" ? action : throw new InvalidDataException("Unknown export action.");
}
