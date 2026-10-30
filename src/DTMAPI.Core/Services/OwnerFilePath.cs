using System;
using System.IO;
using System.Linq;

namespace DTMAPI.Core.Services
{
    internal static class OwnerFilePath
    {
        internal static string NormalizeKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key) || key.Length > 240) throw new ArgumentException("A nonempty relative key of at most 240 characters is required.", nameof(key));
            string normalized = key.Replace('\\', '/');
            foreach (string segment in normalized.Split('/'))
            {
                string device = segment.Split('.')[0].ToUpperInvariant();
                if (segment.Length == 0 || segment == "." || segment == ".." || segment.Trim() != segment ||
                    segment.EndsWith(".", StringComparison.Ordinal) || segment.Any(c => c < 32 || "<>:\"|?*".IndexOf(c) >= 0) ||
                    device == "CON" || device == "PRN" || device == "AUX" || device == "NUL" ||
                    device == "CONIN$" || device == "CONOUT$" ||
                    (device.Length == 4 && (device.StartsWith("COM", StringComparison.Ordinal) || device.StartsWith("LPT", StringComparison.Ordinal)) && "123456789¹²³".Contains(device[3])))
                    throw new ArgumentException("Invalid relative file key: " + key, nameof(key));
            }
            return normalized;
        }

        internal static string Resolve(string root, string key, bool createParents = false)
        {
            string path = Path.GetFullPath(root);
            // Ancestors are checked too: a safe relative key must not escape through a linked root.
            CheckAncestors(path);
            if (createParents) Directory.CreateDirectory(path);
            foreach (string part in NormalizeKey(key).Split('/'))
            {
                if (Directory.Exists(path))
                {
                    string[] matches = Directory.EnumerateFileSystemEntries(path)
                        .Where(p => string.Equals(Path.GetFileName(p), part, StringComparison.OrdinalIgnoreCase)).ToArray();
                    if (matches.Length > 1 || (matches.Length == 1 && !string.Equals(Path.GetFileName(matches[0]), part, StringComparison.Ordinal)))
                        throw new ArgumentException("File key has an ambiguous or conflicting case: " + key, nameof(key));
                }
                path = Path.Combine(path, part);
                CheckNode(path);
            }
            if (createParents)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                CheckAncestors(path);
            }
            return path;
        }

        private static void CheckAncestors(string path)
        {
            for (string? cursor = path; cursor != null; cursor = Path.GetDirectoryName(cursor)) CheckNode(cursor);
        }

        private static void CheckNode(string path)
        {
            try
            {
                if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                    throw new ArgumentException("File access through a symbolic link or reparse point is not supported: " + path);
            }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
        }
    }
}
