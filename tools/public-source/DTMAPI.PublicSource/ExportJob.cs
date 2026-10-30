using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DTMAPI.PublicSource;

internal sealed record ExportPlan(string SourceCommit, string SourceTree, string PolicyBlob, string ToolTree, Policy Policy, IReadOnlyDictionary<string, FileBlob> Files, object[] Paths, string IgnoredWorktreeStatus);

internal sealed class ExportReport
{
    public int SchemaVersion { get; set; } = 1;
    public string SourceRepository { get; set; } = "";
    public string SourceCommit { get; set; } = "";
    public string SourceTree { get; set; } = "";
    public string PolicyBlob { get; set; } = "";
    public string ToolTree { get; set; } = "";
    public string ExportTree { get; set; } = "";
    public string ExportCommit { get; set; } = "";
    public string? ExportBase { get; set; }
    public string? PublicMain { get; set; }
    public string? CandidateTree { get; set; }
    public string? CandidateCommit { get; set; }
    public string Status { get; set; } = "planned";
}

internal static class ExportJob
{
    public const string ExportRef = "refs/heads/codex/source-export";
    public static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    public static void Save(string path, object value) => File.WriteAllText(path, JsonSerializer.Serialize(value, Json) + "\n", new UTF8Encoding(false));
    public static T Read<T>(string path) => JsonSerializer.Deserialize<T>(File.ReadAllText(path)) ?? throw new InvalidDataException("Empty JSON: " + path);

    public static string ExactCommit(Git git, string value)
    {
        if (!Regex.IsMatch(value, "^(?:[a-f0-9]{40}|[a-f0-9]{64})$") || git.Commit(value) != value) throw new InvalidDataException("An exact full commit OID is required.");
        return value;
    }

    public static ExportPlan Plan(Git source, string commit, bool links = true)
    {
        source.AssertIndependent(); ExactCommit(source, commit);
        IReadOnlyList<GitEntry> entries = source.Entries(commit);
        GitEntry policyEntry = entries.SingleOrDefault(entry => entry.Path == Policy.RepositoryPath) ?? throw new InvalidDataException("Source commit has no export policy.");
        var policy = new Policy(source.Blob(policyEntry.ObjectId));
        Safety.UniquePaths(entries.Select(entry => entry.Path));
        var selected = new Dictionary<string, GitEntry>(StringComparer.Ordinal);
        var paths = new List<object>(); var errors = new List<string>();
        foreach (GitEntry entry in entries)
        {
            try
            {
                Disposition disposition = policy.Classify(entry.Path, entry.Mode);
                paths.Add(new { Source = entry.Path, disposition.Target, entry.Mode, Blob = entry.ObjectId, disposition.Action, disposition.Rule });
                if (disposition.Action == "include" && !selected.TryAdd(disposition.Target, entry)) throw new InvalidDataException("Duplicate mapped target: " + disposition.Target);
            }
            catch (InvalidDataException error) { errors.Add(error.Message); }
        }
        if (errors.Count > 0) throw new InvalidDataException(string.Join('\n', errors));
        Dictionary<string, byte[]> blobs = source.Blobs(selected.Values.Select(entry => entry.ObjectId));
        var files = selected.ToDictionary(pair => pair.Key, pair => new FileBlob(pair.Value.Mode, blobs[pair.Value.ObjectId]), StringComparer.Ordinal);
        Boundary.Validate(policy, files, links);
        return new(commit, source.Tree(commit), policyEntry.ObjectId, source.Run("rev-parse", commit + ":tools/public-source/DTMAPI.PublicSource").Text, policy, files, paths.ToArray(), source.Run("status", "--porcelain=v1", "--untracked-files=normal").Text);
    }

    public static ExportReport Export(Git source, string commit, string output, string? publicRepository = null, string? exportBase = null, bool planOnly = false, string[]? protectedPaths = null)
    {
        string common = source.Run("rev-parse", "--path-format=absolute", "--git-common-dir").Text;
        using FileStream reservation = Safety.Reserve(output, new[] { source.Root, common, publicRepository ?? "" }.Concat(protectedPaths ?? Array.Empty<string>()).ToArray());
        try
        {
            ExportPlan plan = Plan(source, commit);
            Save(Path.Combine(output, "paths.json"), plan.Paths);
            File.WriteAllText(Path.Combine(output, "excluded-worktree-status.txt"), plan.IgnoredWorktreeStatus, new UTF8Encoding(false));
            var report = new ExportReport { SourceRepository = source.Root, SourceCommit = commit, SourceTree = plan.SourceTree, PolicyBlob = plan.PolicyBlob, ToolTree = plan.ToolTree, ExportBase = exportBase };
            if (!planOnly)
            {
                Directory.CreateDirectory(Path.Combine(output, "objects.git"));
                var target = new Git(Path.Combine(output, "objects.git")); target.Run("init", "--bare", "--initial-branch=main");
                target.AssertIndependent();
                if (exportBase is not null)
                {
                    if (publicRepository is null) throw new InvalidDataException("ExportBase requires its public repository.");
                    var upstream = new Git(publicRepository); upstream.AssertIndependent(); ExactCommit(upstream, exportBase);
                    if (upstream.Try("rev-parse", "--verify", ExportRef).Text != exportBase) throw new InvalidDataException("E is not the public pure-export ref.");
                    string main = upstream.Commit("refs/heads/main");
                    if (!upstream.IsAncestor(exportBase, main)) throw new InvalidDataException("Export base has not been merged into public main.");
                    AssertPureChain(upstream, exportBase);
                    target.Run("fetch", "--no-tags", "--no-write-fetch-head", publicRepository, exportBase);
                }
                report.ExportTree = target.WriteTree(plan.Files);
                report.ExportCommit = target.CreateCommit(report.ExportTree,
                    $"Export public source\n\nSource-Commit: {commit}\nSource-Tree: {plan.SourceTree}\nExport-Policy-Blob: {plan.PolicyBlob}\nExport-Tool-Commit: {commit}\nExport-Tool-Tree: {plan.ToolTree}\nPure-Export-Tree: {report.ExportTree}\nExport-Base: {exportBase ?? "none"}", exportBase is null ? Array.Empty<string>() : new[] { exportBase });
                target.Run("update-ref", "refs/heads/codex/new-export", report.ExportCommit);
                Safety.Materialize(Path.Combine(output, "tree"), plan.Files);
                report.Status = "exported; build validation pending";
            }
            Save(Path.Combine(output, "report.json"), report);
            File.WriteAllText(Path.Combine(output, "report.md"), $"# Public source {(planOnly ? "plan" : "export")}\n\n- Source: `{commit}`\n- Policy blob: `{plan.PolicyBlob}`\n- Tool tree: `{plan.ToolTree}`\n- Public files: {plan.Files.Count}\n- Export tree: `{report.ExportTree}`\n- Export commit: `{report.ExportCommit}`\n\nExact blobs and modes are in paths.json. Working-tree changes listed separately were not included.\n", new UTF8Encoding(false));
            return report;
        }
        catch (Exception error) { Save(Path.Combine(output, "failure.json"), new { Status = "failed", error.Message }); throw; }
    }

    public static void AssertPureChain(Git git, string commit)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        while (seen.Add(commit))
        {
            string message = git.Run("show", "-s", "--format=%B", commit).Text;
            string tree = git.Tree(commit);
            if (!message.Contains("Pure-Export-Tree: " + tree, StringComparison.Ordinal) || !Regex.IsMatch(message, @"(?m)^Source-Commit: [a-f0-9]{40}(?:[a-f0-9]{24})?$") || !message.Contains("Export-Policy-Blob: ", StringComparison.Ordinal)) throw new InvalidDataException("Not a pure export commit: " + commit);
            string[] parents = git.Run("show", "-s", "--format=%P", commit).Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parents.Length == 0) return;
            if (parents.Length != 1 || !message.Contains("Export-Base: " + parents[0], StringComparison.Ordinal)) throw new InvalidDataException("Invalid pure-export ancestry: " + commit);
            commit = parents[0];
        }
        throw new InvalidDataException("Cyclic pure-export history.");
    }
}
