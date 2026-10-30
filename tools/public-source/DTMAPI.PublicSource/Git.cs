using System.Diagnostics;
using System.Text;

namespace DTMAPI.PublicSource;

internal sealed record GitEntry(string Mode, string ObjectId, string Path);
internal sealed record GitResult(int ExitCode, byte[] Output, string Error)
{
    public string Text => Encoding.UTF8.GetString(Output).TrimEnd('\r', '\n');
}

internal sealed class Git
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    public string Root { get; }
    public Git(string root) => Root = System.IO.Path.GetFullPath(root);

    public GitResult Run(params string[] arguments) => Run(null, true, arguments);
    public GitResult Try(params string[] arguments) => Run(null, false, arguments);
    public GitResult Input(byte[] input, params string[] arguments) => Run(input, true, arguments);

    private GitResult Run(byte[]? input, bool requireSuccess, string[] arguments)
    {
        var start = new ProcessStartInfo("git")
        {
            WorkingDirectory = Root, UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true
        };
        foreach (string key in start.Environment.Keys.Where(key => key.StartsWith("GIT_", StringComparison.Ordinal)).ToArray()) start.Environment.Remove(key);
        foreach (string value in new[] { "-c", "core.hooksPath=" + System.IO.Path.Combine(Root, "disabled-hooks"), "-c", "core.autocrlf=false", "-c", "core.safecrlf=false", "-c", "core.quotePath=false", "-c", "commit.gpgSign=false", "-c", "user.name=DTMAPI Source Delivery", "-c", "user.email=source-delivery@dtmapi.invalid" }.Concat(arguments)) start.ArgumentList.Add(value);
        start.Environment["GIT_CONFIG_NOSYSTEM"] = "1";
        start.Environment["GIT_CONFIG_GLOBAL"] = OperatingSystem.IsWindows() ? "NUL" : "/dev/null";
        start.Environment["GIT_TERMINAL_PROMPT"] = "0";
        start.Environment["GIT_NO_REPLACE_OBJECTS"] = "1";
        using Process process = Process.Start(start) ?? throw new InvalidOperationException("Could not start Git.");
        using var buffer = new MemoryStream();
        Task output = process.StandardOutput.BaseStream.CopyToAsync(buffer);
        Task<string> error = process.StandardError.ReadToEndAsync();
        Task write = Task.Run(async () =>
        {
            if (input is not null) await process.StandardInput.BaseStream.WriteAsync(input);
            process.StandardInput.Close();
        });
        if (!process.WaitForExit(120000))
        {
            try { process.Kill(entireProcessTree: true); } catch (InvalidOperationException) when (process.HasExited) { }
            throw new TimeoutException("Git command timed out: " + arguments[0]);
        }
        if (!Task.WaitAll(new Task[] { output, error, write }, 10000)) throw new TimeoutException("Git output did not close.");
        var result = new GitResult(process.ExitCode, buffer.ToArray(), error.Result);
        if (requireSuccess && result.ExitCode != 0) throw new InvalidOperationException("Git " + arguments[0] + " failed: " + result.Error);
        return result;
    }

    public string Commit(string revision)
    {
        if (revision.StartsWith('-') || revision.Contains('\0')) throw new ArgumentException("Invalid source revision.");
        return Run("rev-parse", "--verify", revision + "^{commit}").Text;
    }

    public IReadOnlyList<GitEntry> Entries(string commit)
    {
        byte[] data = Run("ls-tree", "-r", "-z", "--full-tree", commit).Output;
        var result = new List<GitEntry>();
        int start = 0;
        for (int end = 0; end < data.Length; end++)
        {
            if (data[end] != 0) continue;
            string row = StrictUtf8.GetString(data, start, end - start);
            int tab = row.IndexOf('\t');
            if (tab < 0) throw new InvalidDataException("Malformed Git tree entry.");
            string[] header = row[..tab].Split(' ');
            if (header.Length != 3) throw new InvalidDataException("Malformed Git tree header.");
            result.Add(new GitEntry(header[0], header[2], row[(tab + 1)..]));
            start = end + 1;
        }
        if (start != data.Length) throw new InvalidDataException("Truncated Git tree listing.");
        return result;
    }

    public byte[] Blob(string objectId) => Run("cat-file", "blob", objectId).Output;
    public Dictionary<string, byte[]> Blobs(IEnumerable<string> objectIds)
    {
        string[] ids = objectIds.Distinct(StringComparer.Ordinal).ToArray();
        byte[] output = Input(Encoding.ASCII.GetBytes(string.Join('\n', ids) + "\n"), "cat-file", "--batch").Output;
        var result = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        int position = 0;
        foreach (string id in ids)
        {
            int end = Array.IndexOf(output, (byte)'\n', position);
            if (end < 0) throw new InvalidDataException("Truncated cat-file header.");
            string[] header = Encoding.ASCII.GetString(output, position, end - position).Split(' ');
            if (header.Length != 3 || header[0] != id || header[1] != "blob") throw new InvalidDataException("Missing or non-blob input: " + id);
            int size = int.Parse(header[2], System.Globalization.CultureInfo.InvariantCulture);
            position = end + 1;
            if (size < 0 || size > output.Length - position - 1 || output[position + size] != '\n') throw new InvalidDataException("Truncated cat-file body.");
            result.Add(id, output.AsSpan(position, size).ToArray());
            position += size + 1;
        }
        if (position != output.Length) throw new InvalidDataException("Unexpected cat-file output.");
        return result;
    }

    public string WriteTree(IReadOnlyDictionary<string, FileBlob> files)
    {
        using var input = new MemoryStream();
        void Line(string value) { input.Write(Encoding.UTF8.GetBytes(value + "\n")); }
        int index = 0;
        var marks = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach ((string path, FileBlob file) in files.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            marks.Add(path, ++index);
            Line("blob"); Line("mark :" + index); Line("data " + file.Bytes.Length);
            input.Write(file.Bytes); Line("");
        }
        string temporaryRef = "refs/dtmapi-staging/" + Guid.NewGuid().ToString("N");
        Line("commit " + temporaryRef);
        Line("committer DTMAPI Source Delivery <source-delivery@dtmapi.invalid> 1 +0000");
        Line("data 0"); Line("deleteall");
        foreach ((string path, FileBlob file) in files.OrderBy(pair => pair.Key, StringComparer.Ordinal)) Line($"M {file.Mode} :{marks[path]} \"{path}\"");
        Line(""); Line("done");
        Input(input.ToArray(), "fast-import", "--quiet", "--done");
        string tree = Tree(temporaryRef);
        Run("update-ref", "-d", temporaryRef);
        return tree;
    }

    public string CreateCommit(string tree, string message, params string[] parents)
    {
        var arguments = new List<string> { "commit-tree", tree };
        foreach (string parent in parents) { arguments.Add("-p"); arguments.Add(parent); }
        return Input(Encoding.UTF8.GetBytes(message + "\n"), arguments.ToArray()).Text;
    }

    public IReadOnlyDictionary<string, FileBlob> ReadFiles(string tree)
    {
        IReadOnlyList<GitEntry> entries = Entries(tree);
        Dictionary<string, byte[]> blobs = Blobs(entries.Select(entry => entry.ObjectId));
        return entries.ToDictionary(entry => entry.Path, entry => new FileBlob(entry.Mode, blobs[entry.ObjectId]), StringComparer.Ordinal);
    }
    public string HashBlob(byte[] data) => Input(data, "hash-object", "-w", "--stdin").Text;
    public string Tree(string commit) => Run("rev-parse", commit + "^{tree}").Text;
    public bool IsAncestor(string ancestor, string descendant) => Try("merge-base", "--is-ancestor", ancestor, descendant).ExitCode == 0;

    public void AssertIndependent()
    {
        string common = Run("rev-parse", "--path-format=absolute", "--git-common-dir").Text;
        Safety.NoLinks(common);
        if (Run("rev-parse", "--is-shallow-repository").Text == "true") throw new InvalidDataException("Complete Git history is required.");
        foreach (string relative in new[] { "objects/info/alternates", "info/grafts" })
            if (File.Exists(System.IO.Path.Combine(common, relative))) throw new InvalidDataException("Public object store must not borrow objects or graft history: " + relative);
        if (Run("for-each-ref", "--format=%(refname)", "refs/replace/").Text.Length != 0) throw new InvalidDataException("Public object store contains replacement refs.");
        if (Try("config", "--local", "--get-regexp", "^(filter\\.|merge\\..*\\.driver$)").ExitCode == 0) throw new InvalidDataException("Object store contains external filters or merge drivers.");
    }
}
