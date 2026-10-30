using System.Text;

namespace DTMAPI.PublicSource;

internal sealed record BootstrapDecision(string Path, string? PublicHash, string? ExportHash, string Action, string Reason);

internal static class Synchronize
{
    public static ExportReport Prepare(string job, string publicRepository, string publicMain, bool bootstrap, string? decisionsPath)
    {
        ExportReport report = ExportJob.Read<ExportReport>(Path.Combine(job, "report.json"));
        var source = new Git(report.SourceRepository); ExportPlan plan = ExportJob.Plan(source, report.SourceCommit);
        var upstream = new Git(publicRepository); upstream.AssertIndependent(); ExportJob.ExactCommit(upstream, publicMain);
        foreach (string input in new[] { source.Root, publicRepository }) if (Safety.Overlaps(job, input)) throw new InvalidDataException("Public/source repository overlaps job.");
        if (upstream.Commit("refs/heads/main") != publicMain) throw new InvalidDataException("Public main moved; prepare again with the exact new P.");
        var target = new Git(Path.Combine(job, "objects.git")); target.AssertIndependent();
        if (target.Tree(report.ExportCommit) != target.WriteTree(plan.Files)) throw new InvalidDataException("N does not match the exact source export.");
        target.Run("fetch", "--no-tags", "--no-write-fetch-head", publicRepository, publicMain);
        report.PublicMain = publicMain;
        string tree;
        if (report.ExportBase is null)
        {
            if (!bootstrap) throw new InvalidDataException("First delivery requires explicit Bootstrap review.");
            IReadOnlyDictionary<string, FileBlob> publicFiles = target.ReadFiles(publicMain);
            IReadOnlyDictionary<string, FileBlob> newFiles = target.ReadFiles(report.ExportCommit);
            var review = publicFiles.Where(pair => !newFiles.TryGetValue(pair.Key, out FileBlob? next) || !Same(pair.Value, next))
                .Select(pair => new BootstrapDecision(pair.Key, Identity(pair.Value), newFiles.TryGetValue(pair.Key, out FileBlob? next) ? Identity(next) : null, "unresolved", "")).ToArray();
            ExportJob.Save(Path.Combine(job, "bootstrap-review.json"), review);
            if (decisionsPath is null) { report.Status = "bootstrap decisions required"; ExportJob.Save(Path.Combine(job, "report.json"), report); return report; }
            BootstrapDecision[] decisions = ExportJob.Read<BootstrapDecision[]>(decisionsPath);
            if (decisions.Length != review.Length || decisions.Select(row => row.Path).Distinct(StringComparer.Ordinal).Count() != review.Length) throw new InvalidDataException("Bootstrap decisions must cover each changed/P-only path exactly once.");
            var merged = new Dictionary<string, FileBlob>(newFiles, StringComparer.Ordinal);
            foreach (BootstrapDecision expected in review)
            {
                BootstrapDecision decision = decisions.Single(row => row.Path == expected.Path);
                if (decision.PublicHash != expected.PublicHash || decision.ExportHash != expected.ExportHash || string.IsNullOrWhiteSpace(decision.Reason)) throw new InvalidDataException("Stale or unexplained bootstrap decision: " + decision.Path);
                switch (decision.Action)
                {
                    case "keep-public": merged[decision.Path] = publicFiles[decision.Path]; break;
                    case "take-export" when newFiles.ContainsKey(decision.Path): break;
                    case "delete" when !newFiles.ContainsKey(decision.Path): merged.Remove(decision.Path); break;
                    default: throw new InvalidDataException("Unresolved bootstrap decision: " + decision.Path);
                }
            }
            Boundary.Validate(plan.Policy, merged);
            tree = target.WriteTree(merged);
        }
        else
        {
            if (bootstrap) throw new InvalidDataException("Bootstrap is only valid without E.");
            if (upstream.Try("rev-parse", "--verify", ExportJob.ExportRef).Text != report.ExportBase) throw new InvalidDataException("E moved; export again.");
            foreach ((string name, string first, string second) in new[] { ("public-contributions", report.ExportBase, publicMain), ("new-export", report.ExportBase, report.ExportCommit), ("public-vs-export", publicMain, report.ExportCommit) })
                File.WriteAllBytes(Path.Combine(job, name + ".diff"), target.Run("diff", "--binary", "--no-ext-diff", "--no-textconv", "--find-renames", first, second).Output);
            GitResult merge = target.Try("merge-tree", "--write-tree", "--merge-base=" + report.ExportBase, publicMain, report.ExportCommit);
            File.WriteAllBytes(Path.Combine(job, "merge-result.txt"), merge.Output);
            if (merge.ExitCode != 0)
            {
                report.Status = merge.ExitCode == 1 ? "conflicts; stages retained in merge-result.txt" : "merge tool failed: " + merge.Error;
                ExportJob.Save(Path.Combine(job, "report.json"), report);
                throw new InvalidDataException(report.Status);
            }
            tree = merge.Text.Split('\n')[0];
            Boundary.Validate(plan.Policy, target.ReadFiles(tree));
        }
        report.CandidateTree = tree;
        report.CandidateCommit = target.CreateCommit(tree, $"Prepare public source update\n\nPublic-Base: {publicMain}\nExport-Base: {report.ExportBase ?? "none"}\nNew-Export: {report.ExportCommit}\nSource-Commit: {report.SourceCommit}\nValidated-Tree: pending\n", publicMain, report.ExportCommit);
        string branch = "refs/heads/codex/public-source-" + report.SourceCommit[..12];
        target.Run("update-ref", branch, report.CandidateCommit);
        string[] newCommits = target.Run("rev-list", report.CandidateCommit, "--not", publicMain).Text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (newCommits.Any(value => value != report.CandidateCommit && value != report.ExportCommit && (report.ExportBase is null || !target.IsAncestor(value, report.ExportBase)))) throw new InvalidDataException("Unapproved new ancestry in candidate.");
        Safety.Materialize(Path.Combine(job, "candidate"), target.ReadFiles(tree));
        report.Status = "candidate prepared; Windows/closure validation pending";
        ExportJob.Save(Path.Combine(job, "report.json"), report);
        return report;
    }

    public static void Finalize(string job, string publicRepository, string acceptedCommit, string validationPath)
    {
        ExportReport report = ExportJob.Read<ExportReport>(Path.Combine(job, "report.json"));
        if (report.CandidateCommit is null || report.CandidateTree is null || report.PublicMain is null) throw new InvalidDataException("No reviewed candidate.");
        var git = new Git(publicRepository); git.AssertIndependent(); ExportJob.ExactCommit(git, acceptedCommit);
        string main = git.Commit("refs/heads/main");
        if (!git.IsAncestor(acceptedCommit, main) || !git.IsAncestor(report.CandidateCommit, acceptedCommit)) throw new InvalidDataException("The exact candidate was not merged into main.");
        string[] acceptedParents = git.Run("show", "-s", "--format=%P", acceptedCommit).Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (acceptedParents.Length != 2 || acceptedParents[0] != report.PublicMain || acceptedParents[1] != report.CandidateCommit || git.Tree(acceptedCommit) != report.CandidateTree) throw new InvalidDataException("Main/base/tree changed or unsupported squash/rebase/merge result.");
        string[] candidateParents = git.Run("show", "-s", "--format=%P", report.CandidateCommit).Text.Split(' ');
        if (!candidateParents.SequenceEqual(new[] { report.PublicMain, report.ExportCommit })) throw new InvalidDataException("Candidate ancestry mismatch.");
        ExportPlan plan = ExportJob.Plan(new Git(report.SourceRepository), report.SourceCommit);
        var local = new Git(Path.Combine(job, "objects.git"));
        if (local.WriteTree(plan.Files) != git.Tree(report.ExportCommit) || git.Tree(report.ExportCommit) != report.ExportTree) throw new InvalidDataException("N is not the recomputed pure export.");
        ExportJob.AssertPureChain(git, report.ExportCommit);
        string[] parents = git.Run("show", "-s", "--format=%P", report.ExportCommit).Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (!parents.SequenceEqual(report.ExportBase is null ? Array.Empty<string>() : new[] { report.ExportBase })) throw new InvalidDataException("N parent mismatch.");
        Boundary.Validate(plan.Policy, git.ReadFiles(report.CandidateCommit));
        using var validation = Policy.ParseStrict(File.ReadAllBytes(validationPath));
        var root = validation.RootElement;
        if (!root.TryGetProperty("SchemaVersion", out var schema) || schema.GetInt32() is not (2 or 3) ||
            root.GetProperty("Status").GetString() != "passed" || !root.GetProperty("IdentityUnchanged").GetBoolean() ||
            root.GetProperty("SourceCommit").GetString() != report.CandidateCommit || root.GetProperty("SourceTree").GetString() != report.CandidateTree ||
            root.GetProperty("Dirty").GetBoolean() || root.GetProperty("Profile").GetString() != "PublicWindows" ||
            root.GetProperty("Failures").GetArrayLength() != 0)
            throw new InvalidDataException("Validation is not bound to the complete clean candidate.");
        var end = root.GetProperty("EndSource");
        if (end.GetProperty("Commit").GetString() != report.CandidateCommit || end.GetProperty("Tree").GetString() != report.CandidateTree ||
            end.GetProperty("Dirty").GetBoolean())
            throw new InvalidDataException("Validation source identity changed during execution.");
        // Retained schema 2 reports also supplied a whole-checkout fingerprint.
        // Schema 3 binds execution to evaluated inputs/outputs; Finalize still
        // requires the exact complete candidate and the complete suite selection.
        if (schema.GetInt32() == 2)
        {
            string sourceIdentity = root.GetProperty("SourceIdentity").GetString() ?? "";
            if (sourceIdentity.Length != 64 || !sourceIdentity.All(Uri.IsHexDigit) || sourceIdentity != end.GetProperty("Fingerprint").GetString())
                throw new InvalidDataException("Validation source identity changed during execution.");
        }
        foreach (string key in new[] { "BuildInput", "BuildOutput" })
        {
            string value = root.GetProperty(key).GetString() ?? "";
            if (value.Length != 64 || !value.All(Uri.IsHexDigit) || value != root.GetProperty("End" + key).GetString())
                throw new InvalidDataException("Validation build inputs or outputs changed during execution.");
        }
        using var registry = Policy.ParseStrict(git.ReadFiles(report.CandidateCommit)["tests/DTMAPI.UnitTests/suites.json"].Bytes);
        var runners = registry.RootElement.GetProperty("suites").EnumerateArray().Where(row => row.GetProperty("distribution").GetString() == "public" && row.GetProperty("enabledByDefault").GetBoolean() && row.GetProperty("platforms").EnumerateArray().Any(platform => platform.GetString() == "Windows")).ToDictionary(row => row.GetProperty("id").GetString()!, row => row.GetProperty("runner").GetString()!, StringComparer.Ordinal);
        string[] expected = runners.Keys.OrderBy(value => value, StringComparer.Ordinal).ToArray();
        string[] actual = root.GetProperty("Results").EnumerateArray().Select(row => row.GetProperty("Id").GetString()!).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        if (expected.Length == 0 || !expected.SequenceEqual(actual)) throw new InvalidDataException("Validation omitted or duplicated selected PublicWindows entries.");
        foreach (var row in root.GetProperty("Results").EnumerateArray())
        {
            string runner = runners[row.GetProperty("Id").GetString()!];
            if (runner is not ("unit-exe" or "existing-exe-focus" or "powershell" or "build-only") ||
                row.GetProperty("Runner").GetString() != runner || row.GetProperty("Status").GetString() != (runner == "build-only" ? "built" : "passed"))
                throw new InvalidDataException("Validation runner/result category does not match the selected entry.");
        }
        string current = git.Try("rev-parse", "--verify", ExportJob.ExportRef).Text;
        if (current == report.ExportCommit) return; // Crash after CAS: all identity checks still run before this idempotent return.
        if (current != (report.ExportBase ?? "")) throw new InvalidDataException("Export base CAS lost; do not overwrite another delivery.");
        git.Run("update-ref", ExportJob.ExportRef, report.ExportCommit, report.ExportBase ?? new string('0', report.ExportCommit.Length));
    }

    private static bool Same(FileBlob first, FileBlob second) => first.Mode == second.Mode && first.Bytes.AsSpan().SequenceEqual(second.Bytes);
    private static string Identity(FileBlob file) => file.Mode + ":" + file.Hash;
}
