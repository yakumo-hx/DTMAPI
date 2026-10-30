using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace DTMAPI.Internal
{
    internal static class AuthorTreeDigest
    {
        internal const string Algorithm = "DTMAPI-FileTree-SHA256-v1";
        private static readonly byte[] Separator = { 0 };

        internal static AuthorTreeDigestSnapshot Capture(string rootPath)
        {
            string root = Path.GetFullPath(rootPath ?? string.Empty);
            string volumeRoot = Path.GetPathRoot(root) ?? string.Empty;
            if (root.Length > volumeRoot.Length)
                root = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (!Directory.Exists(root))
                throw new DirectoryNotFoundException("Author source tree does not exist: " + root);
            string prefix = root.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? root : root + Path.DirectorySeparatorChar;
            var files = EnumerateFiles(root).Select(path => new { Path = path, Relative = path.Substring(prefix.Length).Replace('\\', '/') })
                .OrderBy(file => file.Relative, StringComparer.Ordinal).ToArray();
            using (IncrementalHash aggregate = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
            using (SHA256 sha = SHA256.Create())
            {
                foreach (var file in files)
                {
                    using (var stream = new FileStream(file.Path, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        long length = stream.Length;
                        byte[] hash = sha.ComputeHash(stream);
                        if (stream.Position != length || stream.Length != length)
                            throw new IOException("Author source file changed while hashing: " + file.Relative);
                        Append(aggregate, file.Relative, length, hash);
                    }
                }
                return new AuthorTreeDigestSnapshot(Finish(aggregate), files.Length);
            }
        }

        internal static void Append(IncrementalHash aggregate, string relative, long length, byte[] hash)
        {
            aggregate.AppendData(Encoding.UTF8.GetBytes(relative));
            aggregate.AppendData(Separator);
            byte[] lengthBytes = BitConverter.GetBytes(length);
            if (!BitConverter.IsLittleEndian) Array.Reverse(lengthBytes);
            aggregate.AppendData(lengthBytes);
            aggregate.AppendData(hash);
        }

        internal static string Finish(IncrementalHash aggregate)
        {
            var text = new StringBuilder(64);
            foreach (byte value in aggregate.GetHashAndReset())
                text.Append(value.ToString("X2", CultureInfo.InvariantCulture));
            return text.ToString();
        }

        private static IEnumerable<string> EnumerateFiles(string root)
        {
            RejectReparsePoint(root);
            var pending = new Stack<string>();
            pending.Push(root);
            while (pending.Count > 0)
            {
                string directory = pending.Pop();
                RejectReparsePoint(directory);
                foreach (string path in Directory.EnumerateFileSystemEntries(directory))
                {
                    FileAttributes attributes = RejectReparsePoint(path);
                    if ((attributes & FileAttributes.Directory) != 0)
                        pending.Push(path);
                    else
                        yield return path;
                }
            }
        }

        private static FileAttributes RejectReparsePoint(string path)
        {
            FileAttributes attributes = File.GetAttributes(path);
            if ((attributes & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("Author source trees may not contain reparse points: " + path);
            return attributes;
        }
    }

    internal readonly struct AuthorTreeDigestSnapshot
    {
        internal AuthorTreeDigestSnapshot(string sha256, int fileCount) { Sha256 = sha256; FileCount = fileCount; }
        internal string Sha256 { get; }
        internal int FileCount { get; }
    }
}
