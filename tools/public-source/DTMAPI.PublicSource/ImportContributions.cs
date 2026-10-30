using System.Text;

namespace DTMAPI.PublicSource;

internal static class ImportContributions
{
    public static string Prepare(string internalRepository, string internalCommit, string publicRepository, string[] commits, string prUrl, string output)
    {
        if (!Uri.TryCreate(prUrl, UriKind.Absolute, out Uri? uri) || uri.Scheme != "https") throw new InvalidDataException("An HTTPS PR provenance URL is required.");
        var source = new Git(internalRepository); source.AssertIndependent(); ExportJob.ExactCommit(source, internalCommit);
        var upstream = new Git(publicRepository); upstream.AssertIndependent();
        var policy = new Policy(source.Blob(source.Run("rev-parse", internalCommit + ":" + Policy.RepositoryPath).Text));
        using FileStream reservation = Safety.Reserve(output, internalRepository, publicRepository);
        Directory.CreateDirectory(Path.Combine(output, "internal-candidate.git"));
        var scratch = new Git(Path.Combine(output, "internal-candidate.git")); scratch.Run("init", "--bare", "--initial-branch=main");
        scratch.Run("fetch", "--no-tags", "--no-write-fetch-head", internalRepository, internalCommit);
        string head = internalCommit;
        string? previous = null;
        foreach (string commit in commits)
        {
            ExportJob.ExactCommit(upstream, commit);
            string[] parents = upstream.Run("show", "-s", "--format=%P", commit).Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parents.Length != 1 || (previous is not null && parents[0] != previous)) throw new InvalidDataException("Import requires an ordered, contiguous non-merge PR commit sequence.");
            previous = commit;
            IReadOnlyDictionary<string, FileBlob> Map(string revision)
            {
                IReadOnlyDictionary<string, FileBlob> files = upstream.ReadFiles(revision);
                if (files.TryGetValue(Policy.RepositoryPath, out FileBlob? publicPolicy))
                {
                    var other = new Policy(publicPolicy.Bytes);
                    if (other.Mappings.Count != policy.Mappings.Count || other.Mappings.Any(pair => !policy.Mappings.TryGetValue(pair.Key, out Mapping? mapping) || mapping != pair.Value)) throw new InvalidDataException("Root mappings changed; reverse mapping needs explicit migration.");
                }
                var mapped = new Dictionary<string, FileBlob>(StringComparer.Ordinal);
                foreach ((string path, FileBlob file) in files)
                {
                    if (policy.ClassifyTarget(path, file.Mode).Action != "include") throw new InvalidDataException("Cannot import undeclared public path: " + path);
                    if (!mapped.TryAdd(policy.Reverse(path), file)) throw new InvalidDataException("Ambiguous reverse mapping: " + path);
                }
                Safety.UniquePaths(mapped.Keys); return mapped;
            }
            string baseTree = scratch.WriteTree(Map(parents[0]));
            string changeTree = scratch.WriteTree(Map(commit));
            string basis = scratch.CreateCommit(baseTree, "Mapped public parent");
            string change = scratch.CreateCommit(changeTree, "Mapped public contribution", basis);
            GitResult merge = scratch.Try("merge-tree", "--write-tree", "--merge-base=" + basis, head, change);
            if (merge.ExitCode != 0)
            {
                File.WriteAllBytes(Path.Combine(output, "import-conflicts-" + commit[..12] + ".txt"), merge.Output);
                throw new InvalidDataException("Import conflict; internal checkout unchanged, stages retained in scratch.");
            }
            string tree = merge.Text.Split('\n')[0];
            string author = upstream.Run("show", "-s", "--format=%an <%ae> %at %ai", commit).Text;
            string nameEmail = upstream.Run("show", "-s", "--format=%an <%ae>", commit).Text;
            string timestamp = upstream.Run("show", "-s", "--format=%at", commit).Text;
            string date = upstream.Run("show", "-s", "--format=%ai", commit).Text;
            string zone = date.Split(' ')[^1];
            if (nameEmail.Any(character => character is '\n' or '\r' or '\0')) throw new InvalidDataException("Malformed original author identity.");
            string message = upstream.Run("show", "-s", "--format=%B", commit).Text + $"\n\nPublic-Commit: {commit}\nPublic-PR: {prUrl}\n";
            // Write an ordinary commit object to retain Author/AuthorDate without mutating process-wide Git environment.
            string content = $"tree {tree}\nparent {head}\nauthor {nameEmail} {timestamp} {zone}\ncommitter DTMAPI Source Delivery <source-delivery@dtmapi.invalid> {DateTimeOffset.UtcNow.ToUnixTimeSeconds()} +0000\n\n{message}";
            head = scratch.Input(Encoding.UTF8.GetBytes(content), "hash-object", "-t", "commit", "-w", "--stdin").Text;
        }
        scratch.Run("update-ref", "refs/heads/codex/import-public-contributions", head);
        ExportJob.Save(Path.Combine(output, "import-report.json"), new { InternalBase = internalCommit, Candidate = head, PublicCommits = commits, PublicPr = prUrl, Status = "review and affected tests required; internal checkout unchanged" });
        return head;
    }
}
