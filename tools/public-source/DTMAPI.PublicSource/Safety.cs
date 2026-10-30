using System.Security.Cryptography;
using System.Text;

namespace DTMAPI.PublicSource;

internal sealed record FileBlob(string Mode, byte[] Bytes)
{
    public string Hash => Convert.ToHexString(SHA256.HashData(Bytes)).ToLowerInvariant();
}

internal static class Safety
{
    public static bool Overlaps(string first, string second)
    {
        first = Path.GetFullPath(first).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        second = Path.GetFullPath(second).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        StringComparison comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return first.Equals(second, comparison) || first.StartsWith(second + Path.DirectorySeparatorChar, comparison) || second.StartsWith(first + Path.DirectorySeparatorChar, comparison);
    }

    public static void NoLinks(string path)
    {
        for (string? current = Path.GetFullPath(path); current is not null; current = Path.GetDirectoryName(current))
            if ((Directory.Exists(current) || File.Exists(current)) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) throw new InvalidDataException("Reparse point/symlink in path: " + current);
    }

    public static FileStream Reserve(string root, params string[] inputs)
    {
        root = Path.GetFullPath(root);
        NoLinks(root);
        foreach (string input in inputs.Where(input => !string.IsNullOrWhiteSpace(input)))
        {
            NoLinks(input);
            if (Overlaps(root, input)) throw new InvalidDataException("Output overlaps protected input: " + input);
        }
        if (File.Exists(root) || (Directory.Exists(root) && Directory.EnumerateFileSystemEntries(root).Any())) throw new InvalidDataException("Output must be a new or empty directory: " + root);
        Directory.CreateDirectory(root);
        NoLinks(root);
        var reservation = new FileStream(Path.Combine(root, ".reservation"), FileMode.CreateNew, FileAccess.Write, FileShare.None);
        if (Directory.EnumerateFileSystemEntries(root).Any(path => Path.GetFileName(path) != ".reservation")) { reservation.Dispose(); throw new InvalidDataException("Output was occupied while reserving it."); }
        return reservation;
    }

    public static void Materialize(string root, IReadOnlyDictionary<string, FileBlob> files)
    {
        NoLinks(root);
        if (Directory.Exists(root) && Directory.EnumerateFileSystemEntries(root).Any()) throw new InvalidDataException("Materialization target is occupied: " + root);
        Directory.CreateDirectory(root);
        foreach ((string path, FileBlob file) in files)
        {
            Policy.ValidatePath(path);
            string target = Path.Combine(root, path);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            NoLinks(target);
            using (var stream = new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.None)) stream.Write(file.Bytes);
            if (!OperatingSystem.IsWindows() && file.Mode == "100755") File.SetUnixFileMode(target, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.GroupRead | UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
        }
    }

    public static void UniquePaths(IEnumerable<string> paths)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string path in paths)
        {
            Policy.ValidatePath(path);
            string normalized = path.Normalize(NormalizationForm.FormC);
            if (!names.Add(normalized)) throw new InvalidDataException("Case/Unicode path collision: " + path);
            files.Add(normalized);
        }
        foreach (string path in files)
            for (int slash = path.IndexOf('/'); slash >= 0; slash = path.IndexOf('/', slash + 1))
                if (files.Contains(path[..slash])) throw new InvalidDataException("Directory/file collision: " + path);
    }
}
