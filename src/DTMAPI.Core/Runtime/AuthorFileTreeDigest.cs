using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace DTMAPI.Core.Runtime
{
    internal static class AuthorFileTreeDigest
    {
        internal const string Algorithm = "DTMAPI-FileTree-SHA256-v1";

        public static string Compute(string rootPath)
        {
            string root = Path.GetFullPath(rootPath ?? string.Empty);
            string volumeRoot = Path.GetPathRoot(root) ?? string.Empty;
            if (root.Length > volumeRoot.Length)
                root = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (!Directory.Exists(root))
                throw new DirectoryNotFoundException("Author source tree does not exist: " + root);

            string[] files = EnumerateFilesRejectingReparsePoints(root)
                .OrderBy(path => Relative(root, path), StringComparer.Ordinal)
                .ToArray();
            using (IncrementalHash aggregate = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
            {
                foreach (string file in files)
                {
                    string relative = Relative(root, file);
                    aggregate.AppendData(Encoding.UTF8.GetBytes(relative));
                    aggregate.AppendData(new byte[] { 0 });

                    byte[] fileHash;
                    long length;
                    using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (SHA256 sha = SHA256.Create())
                    {
                        length = stream.Length;
                        fileHash = sha.ComputeHash(stream);
                        if (stream.Position != length)
                            throw new IOException("Author source file changed while hashing: " + relative);
                    }

                    byte[] lengthBytes = BitConverter.GetBytes(length);
                    if (!BitConverter.IsLittleEndian)
                        Array.Reverse(lengthBytes);
                    aggregate.AppendData(lengthBytes);
                    aggregate.AppendData(fileHash);
                }

                byte[] digest = aggregate.GetHashAndReset();
                var text = new StringBuilder(digest.Length * 2);
                foreach (byte value in digest)
                    text.Append(value.ToString("X2", CultureInfo.InvariantCulture));
                return text.ToString();
            }
        }

        private static IEnumerable<string> EnumerateFilesRejectingReparsePoints(string root)
        {
            var pending = new Stack<string>();
            pending.Push(root);
            while (pending.Count > 0)
            {
                string directory = pending.Pop();
                RejectReparsePoint(directory);
                foreach (string file in Directory.GetFiles(directory))
                {
                    RejectReparsePoint(file);
                    yield return file;
                }

                foreach (string child in Directory.GetDirectories(directory).OrderByDescending(path => path, StringComparer.Ordinal))
                {
                    RejectReparsePoint(child);
                    pending.Push(child);
                }
            }
        }

        private static void RejectReparsePoint(string path)
        {
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("Author source trees may not contain reparse points: " + path);
        }

        private static string Relative(string root, string path)
        {
            string prefix = root + Path.DirectorySeparatorChar;
            string full = Path.GetFullPath(path);
            if (!full.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Author source path escapes its root: " + full);
            return full.Substring(prefix.Length).Replace('\\', '/');
        }
    }
}
