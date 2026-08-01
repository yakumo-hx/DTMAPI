using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace DTMAPI.Tooling.Metadata;

public sealed class TreeHasher
{
    public TreeHashSnapshot Capture(string rootPath)
    {
        string root = Path.GetFullPath(rootPath);
        var errors = new List<string>();
        string[] files = EnumerateFiles(root, errors)
            .OrderBy(path => Path.GetRelativePath(root, path).Replace('\\', '/'), StringComparer.Ordinal)
            .ToArray();
        int count = 0;
        long totalBytes = 0;

        using IncrementalHash treeHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Span<byte> header = stackalloc byte[12];
        foreach (string file in files)
        {
            string relative = Path.GetRelativePath(root, file).Replace('\\', '/');
            try
            {
                byte[] fileHash;
                long length;
                using (FileStream stream = ReadOnlyFiles.OpenSharedRead(file))
                {
                    length = stream.Length;
                    fileHash = SHA256.HashData(stream);
                }

                byte[] relativeBytes = Encoding.UTF8.GetBytes(relative);
                BinaryPrimitives.WriteInt32LittleEndian(header[..4], relativeBytes.Length);
                BinaryPrimitives.WriteInt64LittleEndian(header[4..], length);
                treeHash.AppendData(header);
                treeHash.AppendData(relativeBytes);
                treeHash.AppendData(fileHash);
                count++;
                totalBytes += length;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                errors.Add(relative + ": " + ex.GetType().Name + ": " + ex.Message);
            }
        }

        return new TreeHashSnapshot
        {
            RootPath = root,
            Sha256 = Convert.ToHexString(treeHash.GetHashAndReset()),
            FileCount = count,
            TotalBytes = totalBytes,
            Errors = errors.OrderBy(value => value, StringComparer.Ordinal).ToArray()
        };
    }

    private static IEnumerable<string> EnumerateFiles(string root, ICollection<string> errors)
    {
        if (File.Exists(root))
        {
            yield return root;
            yield break;
        }
        if (!Directory.Exists(root))
        {
            errors.Add("Root does not exist: " + root);
            yield break;
        }

        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            string directory = pending.Pop();
            string[] childFiles;
            string[] childDirectories;
            try
            {
                childFiles = Directory.GetFiles(directory);
                childDirectories = Directory.GetDirectories(directory);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                errors.Add(Path.GetRelativePath(root, directory).Replace('\\', '/') + ": " + ex.GetType().Name + ": " + ex.Message);
                continue;
            }

            foreach (string file in childFiles.OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
                yield return file;

            foreach (string child in childDirectories.OrderByDescending(value => value, StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    if ((File.GetAttributes(child) & FileAttributes.ReparsePoint) != 0)
                    {
                        errors.Add(Path.GetRelativePath(root, child).Replace('\\', '/') + ": skipped reparse-point directory");
                        continue;
                    }
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    errors.Add(Path.GetRelativePath(root, child).Replace('\\', '/') + ": " + ex.GetType().Name + ": " + ex.Message);
                    continue;
                }
                pending.Push(child);
            }
        }
    }
}
