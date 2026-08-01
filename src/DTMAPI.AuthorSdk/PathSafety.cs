using System.Security.Cryptography;
using System.Text;

namespace DTMAPI.AuthorSdk;

internal static class PathSafety
{
    public static string FullPath(string path) => Path.GetFullPath(string.IsNullOrWhiteSpace(path) ? "." : path);

    public static string ResolveUnderRoot(string root, string relativePath, string label)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
            throw new InvalidDataException(label + " must be a non-empty relative path.");
        string fullRoot = EnsureTrailingSeparator(Path.GetFullPath(root));
        string resolved = Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!resolved.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException(label + " escapes the project root: " + relativePath);
        return resolved;
    }

    public static string RelativePath(string root, string path) => Path.GetRelativePath(root, path).Replace('\\', '/');

    public static void RejectBepInExPluginDestination(string path)
    {
        string normalized = Path.GetFullPath(path).Replace('\\', '/');
        if (normalized.Contains("/BepInEx/plugins/", StringComparison.OrdinalIgnoreCase) || normalized.EndsWith("/BepInEx/plugins", StringComparison.OrdinalIgnoreCase))
            throw new CommandLineException("Ordinary DTMAPI Mods must not be created or packaged under BepInEx/plugins.");
    }

    public static void RejectReparsePoints(string root, IEnumerable<string> paths)
    {
        string fullRoot = Path.GetFullPath(root);
        foreach (string path in paths.Append(fullRoot))
        {
            FileAttributes attributes = File.GetAttributes(path);
            if ((attributes & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("Symbolic links and other reparse points are not accepted: " + path);
        }
    }

    public static string Sha256File(string path)
    {
        using FileStream stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    public static string Sha256Bytes(ReadOnlySpan<byte> bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    public static string Sha256Tree(string root, IEnumerable<string>? selectedFiles = null)
    {
        string[] files = (selectedFiles ?? Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            .Select(Path.GetFullPath)
            .OrderBy(path => RelativePath(root, path), StringComparer.Ordinal)
            .ToArray();
        using var aggregate = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (string file in files)
        {
            string relative = RelativePath(root, file);
            byte[] header = Encoding.UTF8.GetBytes(relative + "\0" + new FileInfo(file).Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\0");
            aggregate.AppendData(header);
            using FileStream stream = File.OpenRead(file);
            byte[] buffer = new byte[81920];
            int read;
            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                aggregate.AppendData(buffer, 0, read);
        }
        return Convert.ToHexString(aggregate.GetHashAndReset()).ToLowerInvariant();
    }

    public static bool IsBuildPath(string relativePath)
    {
        string normalized = relativePath.Replace('\\', '/');
        return normalized.StartsWith("bin/", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("obj/", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith(".git/", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("dist/", StringComparison.OrdinalIgnoreCase);
    }

    private static string EnsureTrailingSeparator(string path) => path.EndsWith(Path.DirectorySeparatorChar) ? path : path + Path.DirectorySeparatorChar;
}
