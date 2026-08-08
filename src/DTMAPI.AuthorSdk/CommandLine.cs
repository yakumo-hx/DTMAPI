namespace DTMAPI.AuthorSdk;

internal sealed class ParsedCommand
{
    private readonly Dictionary<string, string> options;

    private ParsedCommand(string name, List<string> positionals, Dictionary<string, string> options, bool json)
    {
        Name = name;
        Positionals = positionals;
        this.options = options;
        Json = json;
    }

    public string Name { get; }
    public IReadOnlyList<string> Positionals { get; }
    public bool Json { get; }

    public string Option(string name, string defaultValue = "") => options.TryGetValue(name, out string? value) ? value : defaultValue;
    public bool HasOption(string name) => options.ContainsKey(name);
    public void RequireOnlyOptions(params string[] allowed)
    {
        var accepted = new HashSet<string>(allowed, StringComparer.OrdinalIgnoreCase);
        string[] unknown = options.Keys.Where(key => !accepted.Contains(key)).OrderBy(key => key, StringComparer.OrdinalIgnoreCase).ToArray();
        if (unknown.Length > 0)
            throw new CommandLineException("Unsupported option(s) for " + Name + ": " + string.Join(", ", unknown.Select(key => "--" + key)) + ".");
    }

    public static ParsedCommand Parse(string[] args)
    {
        if (args.Length == 0)
            return new ParsedCommand("help", new List<string>(), new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), false);

        string name = args[0].Trim().ToLowerInvariant();
        var positionals = new List<string>();
        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        bool json = false;
        for (int i = 1; i < args.Length; i++)
        {
            string token = args[i];
            if (token.Equals("--json", StringComparison.OrdinalIgnoreCase))
            {
                json = true;
                continue;
            }

            if (token.StartsWith("--", StringComparison.Ordinal))
            {
                string key = token[2..];
                if (key.Length == 0)
                    throw new CommandLineException("Empty option name.");
                if (key.Equals("force", StringComparison.OrdinalIgnoreCase) || key.Equals("adopt", StringComparison.OrdinalIgnoreCase))
                    throw new CommandLineException("Author SDK 0.1.0 intentionally has no --force or --adopt operation.");
                if (i + 1 >= args.Length || args[i + 1].StartsWith("--", StringComparison.Ordinal))
                    throw new CommandLineException("Option --" + key + " requires a value.");
                if (!options.TryAdd(key, args[++i]))
                    throw new CommandLineException("Option --" + key + " was specified more than once.");
                continue;
            }

            positionals.Add(token);
        }

        return new ParsedCommand(name, positionals, options, json);
    }
}

internal sealed class CommandLineException : Exception
{
    public CommandLineException(string message) : base(message) { }
}
