using System.Text;
using System.Text.Json.Nodes;

namespace DTMAPI.PublicSource;

internal static class Contracts
{
    private static int passed;
    public static int Run()
    {
        string repository = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(repository, "DTMAPI.sln"))) repository = Path.GetDirectoryName(repository) ?? throw new DirectoryNotFoundException("Repository root not found.");
        string root = Path.Combine(Path.GetTempPath(), "DTMAPI export contracts 中文 " + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        byte[] realPolicyBytes = File.ReadAllBytes(Path.Combine(repository, Policy.RepositoryPath));
        var realPolicy = new Policy(realPolicyBytes);
        foreach (string path in new[] { "products/first-party/Zoom/new-notes/PLAN.MD", "products/first-party/Zoom/qa/research.txt", "products/first-party/Zoom/research/memory.json", "products/first-party/Zoom/src/AGENTS.md", "products/first-party/Zoom/.agents/state.json", "docs/updates/new-record.md", "tools/release/dtmapi-product-catalog.json", "tools/release/dtmapi-mod-publish-zh.json" })
            Assert(realPolicy.Classify(path, "100644").Action == "exclude", "Public boundary excludes " + path);
        foreach (string path in new[] { "products/first-party/Zoom/src/NewFeature.cs", "products/first-party/Zoom/content/manifest.json", "author-sdk/API-STATUS.md" })
            Assert(realPolicy.Classify(path, "100644").Action == "include", "Public boundary preserves " + path);
        Reject(() => realPolicy.Classify("products/first-party/UnreviewedMod/entry.cs", "100644"), "Unknown product requires classification", "Unclassified");
        foreach (string product in new[] { "EveningStarCassette", "TouhouMusicPoolPack" })
        {
            string productRoot = "products/first-party/" + product + "/";
            Assert(realPolicy.Classify(productRoot + "audio-data/content.json", "100644").Action == "include", "Public audio Mod content source " + product);
            foreach (string privatePath in new[] { "README.md", "research/session.json", "assets/track.mp3", "assets/track.flac", "assets/track.wav" })
                Assert(realPolicy.Classify(productRoot + privatePath, "100644").Action == "exclude", "Audio Mod private input stays local " + product + "/" + privatePath);
        }
        foreach (string path in new[] { "tests/archive/retired/Fixture.cs", "tests/DTMAPI.RuntimeModules.Probe/ModEntry.cs", "tests/DTMAPI.AudioPrepare.UiTests/Program.cs", "author-sdk/examples/monster-chromatic/Chromatic.dtmonster.json" })
            Assert(realPolicy.Classify(path, "100644").Action == "exclude", "Retired or live-only input stays local " + path);
        foreach (string path in new[] { "src/DTMAPI.NativeReferences/Program.cs", "tools/AudioPrepare/Program.cs", "tests/DTMAPI.MonsterExamples.Build/DTMAPI.MonsterExamples.Build.csproj", "tools/scripts/build-api-references.ps1", "tools/scripts/test-uninstall-runtime.ps1", "tools/scripts/uninstall-targets.json", "tools/release/audio-decoder.json", "tools/audio-decoder/build-minimal.sh" })
            Assert(realPolicy.Classify(path, "100644").Action == "include", "Current public build input " + path);
        byte[] policyBytes = FixturePolicy(realPolicyBytes);
        var policy = new Policy(policyBytes);
        Dictionary<string, FileBlob> files = FixtureFiles(policyBytes);
        var source = NewGit(Path.Combine(root, "source"));
        string s1 = Commit(source, files, "initial internal source");
        source.Run("update-ref", "refs/heads/main", s1);
        Safety.Materialize(Path.Combine(root, "materialized"), files);
        File.WriteAllText(Path.Combine(source.Root, "untracked.cs"), "dirty input ignored");
        string job1 = Path.Combine(root, "job-one");
        ExportReport n1 = ExportJob.Export(source, s1, job1);
        var git1 = new Git(Path.Combine(job1, "objects.git"));
        Assert(!git1.Entries(n1.ExportCommit).Any(entry => entry.Path == "untracked.cs"), "X01 exact committed blobs ignore untracked/working source");
        Assert(git1.Try("cat-file", "-e", s1 + "^{commit}").ExitCode != 0, "X01 internal commit is absent from public object store");
        source.Run("config", "core.autocrlf", "true");
        ExportReport repeated = ExportJob.Export(source, s1, Path.Combine(root, "repeat"));
        Assert(repeated.ExportTree == n1.ExportTree, "X02 deterministic tree across checkout EOL settings");
        Assert(git1.ReadFiles(n1.ExportCommit)["src/Product/Value.cs"].Bytes.AsSpan().SequenceEqual(files["src/Product/Value.cs"].Bytes), "X02 byte identity and mapped roots");
        Reject(() => new Policy(Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(policyBytes).Replace("\"active\"", "\"draft\""))), "X03 draft policy", "active");
        Reject(() => new Policy(Encoding.UTF8.GetBytes("{\"schemaVersion\":1,\"schemaVersion\":1}")), "X03 duplicate JSON", "Duplicate");
        JsonObject unknownPolicy = JsonNode.Parse(policyBytes)!.AsObject(); unknownPolicy["unknown"] = true;
        Reject(() => new Policy(Encoding.UTF8.GetBytes(unknownPolicy.ToJsonString())), "X03 unknown rule field", "Unknown");
        Reject(() => policy.Classify("new-root/file.cs", "100644"), "X04 unknown root", "Unclassified");
        Reject(() => policy.Classify("src/NewComponent/file.cs", "100644"), "X04 unknown component", "Unclassified");
        Assert(policy.Classify("src/Product/new/subdirectory/file.cs", "100644").Action == "include", "X04 owned C# descendant inherits");
        Reject(() => policy.Classify("src/Product/new.bin", "100644"), "X04 unknown file type", "Unknown included");
        foreach (string bad in new[] { "../out.cs", "src/Product/CON.cs", "src/Product/file:stream", "/root.cs", "src/Product/end. ", "src/.git/file" }) Reject(() => Policy.ValidatePath(bad), "X05 unsafe path " + bad, "Unsafe");
        Reject(() => policy.Classify("src/Product/link.cs", "120000"), "X05 symlink", "mode");
        Reject(() => policy.Classify("src/Product/submodule", "160000"), "X05 gitlink", "mode");
        Reject(() => Safety.UniquePaths(new[] { "src/Product/A.cs", "src/Product/a.cs" }), "X05 case collision", "collision");
        Reject(() => Safety.UniquePaths(new[] { "src/Product/é.cs", "src/Product/e\u0301.cs" }), "X05 Unicode collision", "collision");
        Dictionary<string, FileBlob> projected = new(git1.ReadFiles(n1.ExportCommit), StringComparer.Ordinal);
        var pe = new Dictionary<string, FileBlob>(projected) { ["src/Product/hidden.dat"] = new("100644", new byte[] { (byte)'M', (byte)'Z', 0, 1 }) };
        Reject(() => Boundary.Validate(policy, pe), "X05 renamed PE", "binary");
        var lfs = new Dictionary<string, FileBlob>(projected) { ["src/Product/pointer.cs"] = Text("version https://git-lfs.github.com/" + "spec/v1\noid sha256:123\n") };
        Reject(() => Boundary.Validate(policy, lfs), "X05 LFS pointer", "LFS");
        Reject(() => Safety.Reserve(Path.Combine(source.Root, "nested"), source.Root), "X06 child output", "overlaps");
        Reject(() => Safety.Reserve(root, source.Root), "X06 ancestor output", "overlaps");
        Reject(() => Safety.Reserve(job1, source.Root), "X06 occupied output", "new or empty");
        using (FileStream reservation = Safety.Reserve(Path.Combine(root, "reserved"), source.Root)) Reject(() => Safety.Reserve(Path.Combine(root, "reserved"), source.Root), "X06 reservation race", "new or empty");
        string link = Path.Combine(root, "link");
        if (OperatingSystem.IsWindows())
        {
            // Windows junctions need no symlink privilege. This invokes one native shell solely to create a test link.
            using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("cmd.exe") { UseShellExecute = false, CreateNoWindow = true, ArgumentList = { "/c", "mklink", "/J", link, source.Root } })!;
            process.WaitForExit(10000); if (process.ExitCode != 0) throw new InvalidOperationException("Could not create junction safety fixture.");
        }
        else Directory.CreateSymbolicLink(link, source.Root);
        Reject(() => Safety.Reserve(Path.Combine(link, "output")), "X06 reparse/symlink ancestor", "Reparse");
        // Real fixture identity: mutations must fail before any archive extraction or game action.
        var licensed = new Dictionary<string, FileBlob>();
        foreach (string path in realPolicy.BinaryHashes.Keys.Concat(realPolicy.BinaryInventories.Values).Distinct()) licensed[path] = new("100644", File.ReadAllBytes(Path.Combine(repository, path)));
        foreach (string path in Directory.EnumerateFiles(Path.Combine(repository, "tools/public-source/licenses"), "*.txt")) licensed[Path.GetRelativePath(repository, path).Replace('\\', '/')] = new("100644", File.ReadAllBytes(path));
        Boundary.ValidateLicenses(realPolicy, licensed);
        Assert(true, "X07 exact ZIP member and complete source/license inventory");
        foreach (string victim in licensed.Keys.ToArray())
        {
            var corrupt = new Dictionary<string, FileBlob>(licensed); corrupt.Remove(victim);
            Reject(() => Boundary.ValidateLicenses(realPolicy, corrupt), "X07 missing licensed input " + Path.GetFileName(victim), "");
        }
        var corruptZip = new Dictionary<string, FileBlob>(licensed); string zipPath = realPolicy.BinaryHashes.Keys.Single(path => path.EndsWith(".zip", StringComparison.Ordinal)); corruptZip[zipPath] = Text("changed");
        Reject(() => Boundary.ValidateLicenses(realPolicy, corruptZip), "X07 changed archive", "digest");
        var missingInput = new Dictionary<string, FileBlob>(projected); missingInput.Remove("README.md");
        Reject(() => Boundary.Validate(policy, missingInput), "X09 missing required navigation", "missing");
        var brokenLink = new Dictionary<string, FileBlob>(projected) { ["README.md"] = Text("[missing](src/Product/missing.cs)") };
        Reject(() => Boundary.Validate(policy, brokenLink), "X12 residual deleted link", "Broken public");

        // First public delivery: explicit decision list, retained public history and no premature E.
        var publicGit = NewGit(Path.Combine(root, "public"));
        var publicFiles = new Dictionary<string, FileBlob>(projected) { ["src/Product/Contribution.cs"] = Text("public contribution\n") };
        publicFiles["README.md"] = Text("public old readme\n");
        string p0 = Commit(publicGit, publicFiles, "external author history"); publicGit.Run("update-ref", "refs/heads/main", p0);
        ExportReport pending = Synchronize.Prepare(job1, publicGit.Root, p0, true, null);
        Assert(pending.CandidateCommit is null && publicGit.Try("rev-parse", "--verify", ExportJob.ExportRef).ExitCode != 0, "X13 unresolved bootstrap does not advance E");
        BootstrapDecision[] decisions = ExportJob.Read<BootstrapDecision[]>(Path.Combine(job1, "bootstrap-review.json"))
            .Select(row => row with { Action = row.ExportHash is null ? "keep-public" : "take-export", Reason = "Retain external addition; accept current mapped entry." }).ToArray();
        string decisionsPath = Path.Combine(root, "decisions.json"); ExportJob.Save(decisionsPath, decisions);
        ExportReport candidate1 = Synchronize.Prepare(job1, publicGit.Root, p0, true, decisionsPath);
        Assert(git1.ReadFiles(candidate1.CandidateCommit!).ContainsKey("src/Product/Contribution.cs"), "X10/X13 public-only contribution retained in bootstrap");
        publicGit.Run("fetch", "--no-tags", git1.Root, candidate1.CandidateCommit!);
        string h1 = publicGit.CreateCommit(candidate1.CandidateTree!, "merge reviewed candidate", p0, candidate1.CandidateCommit!);
        string validation1 = Validation(root, candidate1);
        Reject(() => Synchronize.Finalize(job1, publicGit.Root, h1, validation1), "X17 unmerged candidate", "not merged");
        publicGit.Run("update-ref", "refs/heads/main", h1);
        foreach (string mutation in new[] { "executable-built", "build-only-passed", "runner", "input", "source", "failed" })
        {
            JsonObject invalid = JsonNode.Parse(File.ReadAllText(validation1))!.AsObject();
            if (mutation == "executable-built") invalid["Results"]![0]!["Status"] = "built";
            if (mutation == "build-only-passed") invalid["Results"]![1]!["Status"] = "passed";
            if (mutation == "runner") invalid["Results"]![0]!["Runner"] = "build-only";
            if (mutation == "input") invalid["EndBuildInput"] = new string('b', 64);
            if (mutation == "source") invalid["EndSource"]!["Commit"] = p0;
            if (mutation == "failed") invalid["Status"] = "failed";
            string invalidPath = Path.Combine(root, "invalid-" + mutation + ".json");
            File.WriteAllText(invalidPath, invalid.ToJsonString());
            Reject(() => Synchronize.Finalize(job1, publicGit.Root, h1, invalidPath), "X17 inaccurate acceptance " + mutation, "Validation");
        }
        Synchronize.Finalize(job1, publicGit.Root, h1, validation1);
        Synchronize.Finalize(job1, publicGit.Root, h1, validation1);
        Assert(publicGit.Run("rev-parse", ExportJob.ExportRef).Text == n1.ExportCommit, "X17 finalize and idempotent retry advance E only to N");
        files["src/Product/Value.cs"] = Text("internal update\n"); files["src/Product/New.cs"] = Text("N-only addition\n");
        string s2 = Commit(source, files, "second internal update", s1); source.Run("update-ref", "refs/heads/main", s2);
        string job2 = Path.Combine(root, "job-two");
        ExportReport n2 = ExportJob.Export(source, s2, job2, publicGit.Root, n1.ExportCommit);
        ExportReport candidate2 = Synchronize.Prepare(job2, publicGit.Root, h1, false, null);
        var git2 = new Git(Path.Combine(job2, "objects.git"));
        Assert(git2.ReadFiles(candidate2.CandidateCommit!).ContainsKey("src/Product/Contribution.cs") && git2.ReadFiles(candidate2.CandidateCommit!).ContainsKey("src/Product/New.cs"), "X14 two export rounds preserve unimported public contribution");
        publicGit.Run("fetch", "--no-tags", git2.Root, candidate2.CandidateCommit!);
        string h2 = publicGit.CreateCommit(candidate2.CandidateTree!, "second merge", h1, candidate2.CandidateCommit!);
        publicGit.Run("update-ref", "refs/heads/main", h2);
        string validation2 = Validation(root, candidate2);
        publicGit.Run("update-ref", ExportJob.ExportRef, p0);
        Reject(() => Synchronize.Finalize(job2, publicGit.Root, h2, validation2), "X16 export CAS lost", "CAS");
        publicGit.Run("update-ref", ExportJob.ExportRef, n1.ExportCommit);
        Synchronize.Finalize(job2, publicGit.Root, h2, validation2);
        Assert(publicGit.Run("rev-parse", ExportJob.ExportRef).Text == n2.ExportCommit, "X14 E excludes contribution after second merge");
        string staleJob = Path.Combine(root, "stale-main"); ExportJob.Export(source, s2, staleJob, publicGit.Root, n2.ExportCommit);
        Reject(() => Synchronize.Prepare(staleJob, publicGit.Root, h1, false, null), "X16 main race before preparation", "moved");
        string squash = publicGit.CreateCommit(candidate2.CandidateTree!, "unsupported squash", h1); publicGit.Run("update-ref", "refs/heads/main", squash);
        Reject(() => Synchronize.Finalize(job2, publicGit.Root, squash, validation2), "X17 squash loses exact U ancestry", "not merged"); publicGit.Run("update-ref", "refs/heads/main", h2);
        ConflictCases(Path.Combine(root, "conflicts"));
        var changedPolicy = new Dictionary<string, FileBlob>(projected); changedPolicy[Policy.RepositoryPath] = Text(Encoding.UTF8.GetString(policyBytes) + " ");
        Reject(() => Boundary.Validate(policy, changedPolicy), "X15 candidate self-approval policy change", "trusted");
        var forbidden = new Dictionary<string, FileBlob>(projected) { ["internal/private.cs"] = Text("forbidden") };
        Reject(() => Boundary.Validate(policy, forbidden), "X15 forbidden file in merged tree", "Forbidden");
        var importBase = new Dictionary<string, FileBlob>(projected);
        string c0 = Commit(publicGit, importBase, "import base");
        importBase["README.md"] = Text("new contributor root entry\n");
        importBase[".gitignore"] = Text("contributor-output/\n");
        importBase[".gitattributes"] = Text("*.cs text eol=lf\n");
        importBase["src/Product/Renamed.cs"] = importBase["src/Product/Value.cs"]; importBase.Remove("src/Product/Value.cs");
        string c1 = AuthoredCommit(publicGit, importBase, c0, "Alice Example", "alice@example.invalid", "1000000000", "root and rename");
        importBase["src/Product/New.cs"] = new("100755", Encoding.UTF8.GetBytes("second author\n"));
        string c2 = AuthoredCommit(publicGit, importBase, c1, "Bob Example", "bob@example.invalid", "1000001000", "executable new source");
        string imported = ImportContributions.Prepare(source.Root, s1, publicGit.Root, new[] { c1, c2 }, "https://example.invalid/pull/1", Path.Combine(root, "import"));
        var importGit = new Git(Path.Combine(root, "import/internal-candidate.git"));
        Assert(Encoding.UTF8.GetString(importGit.ReadFiles(imported)["README.md"].Bytes) == "internal entry\n" && Encoding.UTF8.GetString(importGit.ReadFiles(imported)["docs/public-root/README.md"].Bytes).Contains("contributor"), "X18 root reverse map preserves internal README");
        foreach (string config in new[] { ".gitignore", ".gitattributes" })
            Assert(Encoding.UTF8.GetString(importGit.ReadFiles(imported)[config].Bytes) == "# internal settings\n" && importGit.ReadFiles(imported)["docs/public-root/" + config].Bytes.SequenceEqual(importBase[config].Bytes), "X18 public Git configuration preserves internal " + config);
        Assert(importGit.Run("show", "-s", "--format=%an|%at", imported).Text == "Bob Example|1000001000" && importGit.Run("show", "-s", "--format=%an|%at", imported + "^").Text == "Alice Example|1000000000", "X18 ordered author and date preservation");
        Assert(importGit.ReadFiles(imported)["src/Product/New.cs"].Mode == "100755" && !importGit.ReadFiles(imported).ContainsKey("src/Product/Value.cs"), "X18 rename/delete and executable mode round trip");
        Console.WriteLine($"PASS: {passed} export/synchronization assertions. Scratch evidence: {root}");
        return 0;
    }

    private static void ConflictCases(string root)
    {
        var git = NewGit(root);
        var original = new Dictionary<string, FileBlob> { ["file.txt"] = Text("one\ntwo\nthree\n"), ["binary.dat"] = new("100644", new byte[] { 0, 1, 2 }), ["dir/child"] = Text("child\n") };
        string basis = Commit(git, original, "base");
        foreach (string kind in new[] { "same-line", "binary", "add/add", "modify/delete", "rename/rename", "directory/file" })
        {
            var ours = new Dictionary<string, FileBlob>(original); var theirs = new Dictionary<string, FileBlob>(original);
            switch (kind)
            {
                case "same-line": ours["file.txt"] = Text("ours\n"); theirs["file.txt"] = Text("theirs\n"); break;
                case "binary": ours["binary.dat"] = new("100644", new byte[] { 0, 5 }); theirs["binary.dat"] = new("100644", new byte[] { 0, 9 }); break;
                case "add/add": ours["added"] = Text("ours\n"); theirs["added"] = Text("theirs\n"); break;
                case "modify/delete": ours["file.txt"] = Text("modified\n"); theirs.Remove("file.txt"); break;
                case "rename/rename": ours["first"] = ours["file.txt"]; ours.Remove("file.txt"); theirs["second"] = theirs["file.txt"]; theirs.Remove("file.txt"); break;
                case "directory/file": ours["new"] = Text("file\n"); theirs["new/child"] = Text("directory\n"); break;
            }
            string p = Commit(git, ours, "ours", basis), n = Commit(git, theirs, "theirs", basis);
            GitResult merge = git.Try("merge-tree", "--write-tree", "--merge-base=" + basis, p, n);
            Assert(merge.ExitCode == 1 && merge.Text.Contains("CONFLICT", StringComparison.Ordinal), "X11 stages retained for " + kind);
        }
        var rename = new Dictionary<string, FileBlob>(original); rename["renamed.txt"] = rename["file.txt"]; rename.Remove("file.txt");
        var edit = new Dictionary<string, FileBlob>(original) { ["file.txt"] = Text("one\nchanged\nthree\n") }; edit.Remove("dir/child");
        string renamed = Commit(git, rename, "rename", basis), edited = Commit(git, edit, "edit/delete", basis);
        GitResult result = git.Try("merge-tree", "--write-tree", "--merge-base=" + basis, renamed, edited);
        Assert(result.ExitCode == 0 && !git.ReadFiles(result.Text.Split('\n')[0]).ContainsKey("dir/child") && Encoding.UTF8.GetString(git.ReadFiles(result.Text.Split('\n')[0])["renamed.txt"].Bytes).Contains("changed"), "X12 rename/edit and unopposed deletion");
    }
    private static string Validation(string root, ExportReport report)
    {
        string path = Path.Combine(root, "validation-" + report.ExportCommit[..12] + ".json");
        string hash = new string('a', 64);
        ExportJob.Save(path, new { SchemaVersion = 3, Status = "passed", IdentityUnchanged = true, SourceCommit = report.CandidateCommit, SourceTree = report.CandidateTree, Dirty = false,
            BuildSourceCommit = report.CandidateCommit, EndSource = new { Commit = report.CandidateCommit, Tree = report.CandidateTree, Dirty = false },
            BuildInput = hash, EndBuildInput = hash, BuildOutput = hash, EndBuildOutput = hash, Failures = Array.Empty<string>(), Profile = "PublicWindows",
            Results = new[] { new { Id = "synthetic-protocol-only", Runner = "existing-exe-focus", Status = "passed" }, new { Id = "synthetic-build-only", Runner = "build-only", Status = "built" } } }); return path;
    }
    private static byte[] FixturePolicy(byte[] original)
    {
        JsonObject root = JsonNode.Parse(original)!.AsObject();
        root["binaryInputs"] = new JsonArray();
        root["exactFiles"] = new JsonArray(JsonNode.Parse("{\"id\":\"internal-root\",\"paths\":[\"README.md\",\".gitignore\",\".gitattributes\"],\"action\":\"exclude\"}"));
        root["ownedTrees"] = JsonNode.Parse("[{\"id\":\"code\",\"paths\":[\"src/Product\",\"tools/public-source\",\"tests/DTMAPI.UnitTests\"],\"action\":\"include\",\"extensions\":[\".cs\",\".json\",\".md\",\".dat\"]},{\"id\":\"internal\",\"paths\":[\"internal\",\"docs/public-root\"],\"action\":\"exclude\"}]");
        return Encoding.UTF8.GetBytes(root.ToJsonString());
    }
    private static Dictionary<string, FileBlob> FixtureFiles(byte[] policy) => new(StringComparer.Ordinal)
    {
        [Policy.RepositoryPath] = new("100644", policy), ["tools/public-source/DTMAPI.PublicSource/Tool.cs"] = Text("tool input\n"),
        ["tests/DTMAPI.UnitTests/suites.json"] = Text("{\"schemaVersion\":2,\"suites\":[{\"id\":\"synthetic-protocol-only\",\"runner\":\"existing-exe-focus\",\"distribution\":\"public\",\"platforms\":[\"Windows\"],\"enabledByDefault\":true},{\"id\":\"synthetic-build-only\",\"runner\":\"build-only\",\"distribution\":\"public\",\"platforms\":[\"Windows\"],\"enabledByDefault\":true}]}"),
        ["README.md"] = Text("internal entry\n"), ["internal/note.md"] = Text("internal excluded\n"),
        [".gitignore"] = Text("# internal settings\n"), [".gitattributes"] = Text("# internal settings\n"),
        ["docs/public-root/.gitignore"] = Text("bin/\nobj/\n"), ["docs/public-root/.gitattributes"] = Text("*.json text eol=lf\n"),
        ["docs/public-root/README.md"] = Text("[contribute](CONTRIBUTING.md)\n"), ["docs/public-root/README.en.md"] = Text("[中文](README.md)\n"),
        ["docs/public-root/CONTRIBUTING.md"] = Text("[source](src/Product/Value.cs)\n"), ["docs/public-root/CHANGELOG.md"] = Text("public changes\n"),
        ["src/Product/Value.cs"] = Text("source value\n")
    };
    private static Git NewGit(string root) { Directory.CreateDirectory(root); var git = new Git(root); git.Run("init", "--initial-branch=main"); return git; }
    private static string Commit(Git git, IReadOnlyDictionary<string, FileBlob> files, string message, params string[] parents) => git.CreateCommit(git.WriteTree(files), message, parents);
    private static string AuthoredCommit(Git git, IReadOnlyDictionary<string, FileBlob> files, string parent, string name, string email, string timestamp, string message) => git.Input(Encoding.UTF8.GetBytes($"tree {git.WriteTree(files)}\nparent {parent}\nauthor {name} <{email}> {timestamp} +0800\ncommitter Fixture <fixture@example.invalid> {timestamp} +0800\n\n{message}\n"), "hash-object", "-t", "commit", "-w", "--stdin").Text;
    private static FileBlob Text(string value) => new("100644", Encoding.UTF8.GetBytes(value));
    private static void Assert(bool condition, string name) { if (!condition) throw new InvalidOperationException("FAIL: " + name); Console.WriteLine("PASS: " + name); passed++; }
    private static void Reject(Action action, string name, string diagnostic)
    {
        try { action(); } catch (Exception error) when (error is InvalidDataException or IOException or ArgumentException or InvalidOperationException)
        {
            if (!error.Message.Contains(diagnostic, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException(name + " failed for the wrong reason: " + error.Message);
            Assert(true, name); return;
        }
        throw new InvalidOperationException("FAIL (accepted invalid input): " + name);
    }
}
