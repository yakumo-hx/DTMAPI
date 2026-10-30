namespace DTMAPI.PublicSource;

internal static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            if (args.Length == 1 && args[0] == "self-test") return Contracts.Run();
            if (args.Length < 1) throw new ArgumentException("Expected plan, export, prepare, check, finalize or import.");
            var options = new Dictionary<string, string>(StringComparer.Ordinal);
            for (int index = 1; index < args.Length; index += 2)
            {
                if (index + 1 >= args.Length || !args[index].StartsWith("--", StringComparison.Ordinal) || !options.TryAdd(args[index][2..], args[index + 1])) throw new ArgumentException("Options require unique --name value pairs.");
            }
            string[] allowed = args[0] switch
            {
                "plan" or "export" => ["source", "commit", "output", "public", "base", "protected"],
                "prepare" => ["job", "public", "main", "bootstrap", "decisions"],
                "check" => ["repository", "policy-commit", "tree-commit"],
                "finalize" => ["job", "public", "accepted", "validation"],
                "import" => ["source", "commit", "public", "commits", "pr", "output"],
                _ => throw new ArgumentException("Unknown command: " + args[0])
            };
            foreach (string name in options.Keys)
                if (!allowed.Contains(name, StringComparer.Ordinal)) throw new ArgumentException("Unknown option: --" + name);
            if (options.TryGetValue("bootstrap", out string? bootstrap) && bootstrap != "true") throw new ArgumentException("--bootstrap accepts only true; omit it for an ordinary update.");
            string Get(string name) => options.TryGetValue(name, out string? value) ? value : throw new ArgumentException("Missing --" + name);
            string? Optional(string name) => options.GetValueOrDefault(name);
            switch (args[0])
            {
                case "plan": case "export":
                    ExportReport report = ExportJob.Export(new Git(Get("source")), Get("commit"), Get("output"), Optional("public"), Optional("base"), args[0] == "plan", Optional("protected")?.Split('|', StringSplitOptions.RemoveEmptyEntries));
                    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(report, ExportJob.Json)); break;
                case "prepare":
                    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(Synchronize.Prepare(Get("job"), Get("public"), Get("main"), Optional("bootstrap") == "true", Optional("decisions")), ExportJob.Json)); break;
                case "check":
                    var git = new Git(Get("repository")); git.AssertIndependent();
                    string trusted = ExportJob.ExactCommit(git, Get("policy-commit"));
                    var policy = new Policy(git.Blob(git.Run("rev-parse", trusted + ":" + Policy.RepositoryPath).Text));
                    Boundary.Validate(policy, git.ReadFiles(ExportJob.ExactCommit(git, Get("tree-commit"))));
                    Console.WriteLine("PASS: public path, payload, license and local-link boundary."); break;
                case "finalize": Synchronize.Finalize(Get("job"), Get("public"), Get("accepted"), Get("validation")); Console.WriteLine("PASS: pure export ref finalized."); break;
                case "import": Console.WriteLine(ImportContributions.Prepare(Get("source"), Get("commit"), Get("public"), Get("commits").Split(','), Get("pr"), Get("output"))); break;
                default: throw new ArgumentException("Unknown command: " + args[0]);
            }
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error.GetType().Name + ": " + error.Message); return 1; }
    }
}
