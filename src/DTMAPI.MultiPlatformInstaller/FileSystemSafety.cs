using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace DTMAPI.MultiPlatformInstaller;

internal static class FileSystemSafety
{
    private static readonly int[] RetryDelaysMs = { 0, 200, 400, 800, 1200, 1600 };

    public static StringComparison PathComparison => OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

    public static string NormalizeRoot(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new InvalidDataException("The path is empty.");

        string full = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string root = (Path.GetPathRoot(full) ?? string.Empty).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (string.Equals(full, root, PathComparison))
            throw new InvalidDataException("A filesystem root cannot be used as the Doloc Town game directory.");

        return full;
    }

    public static string CombineUnder(string root, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
            throw new InvalidDataException("An owned path must be a non-empty relative path: " + relativePath);

        string normalizedRoot = NormalizeRoot(root);
        string candidate = Path.GetFullPath(Path.Combine(normalizedRoot, relativePath));
        string prefix = normalizedRoot + Path.DirectorySeparatorChar;
        if (!candidate.StartsWith(prefix, PathComparison))
            throw new InvalidDataException("An owned path escaped its root: " + relativePath);

        return candidate;
    }

    public static string NormalizeRelativePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
            throw new InvalidDataException("Expected a relative path.");

        string normalized = relativePath.Replace('\\', '/').Trim('/');
        foreach (string segment in normalized.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment is "." or "..")
                throw new InvalidDataException("Dot path segments are not allowed: " + relativePath);
        }

        return normalized;
    }

    public static void AssertNoLinksOnExistingPath(string root, string target)
    {
        string normalizedRoot = NormalizeRoot(root);
        string normalizedTarget = Path.GetFullPath(target);
        string prefix = normalizedRoot + Path.DirectorySeparatorChar;
        if (!string.Equals(normalizedRoot, normalizedTarget, PathComparison) &&
            !normalizedTarget.StartsWith(prefix, PathComparison))
            throw new InvalidDataException("Path escaped the expected root: " + normalizedTarget);

        string current = normalizedRoot;
        AssertNotLink(current);
        string relative = Path.GetRelativePath(normalizedRoot, normalizedTarget);
        if (relative == ".")
            return;

        foreach (string part in relative.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, part);
            if (!EntryExistsIncludingLink(current))
                break;
            AssertNotLink(current);
        }
    }

    private static void AssertNotLink(string path)
    {
        try
        {
            if (new FileInfo(path).LinkTarget is not null || new DirectoryInfo(path).LinkTarget is not null)
                throw new InvalidDataException("Symlink/reparse-point paths are not installer-owned: " + path);
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch
        {
            // Attribute inspection below remains authoritative for ordinary
            // existing entries on hosts which cannot expose LinkTarget.
        }
        FileAttributes attributes = File.GetAttributes(path);
        if ((attributes & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("Symlink/reparse-point paths are not installer-owned: " + path);
    }

    public static bool EntryExistsIncludingLink(string path)
    {
        string full = Path.GetFullPath(path);
        string? parent = Path.GetDirectoryName(full);
        if (string.IsNullOrWhiteSpace(parent) || !Directory.Exists(parent))
            return File.Exists(full) || Directory.Exists(full);
        string leaf = Path.GetFileName(full);
        foreach (string entry in Directory.EnumerateFileSystemEntries(parent, "*", SearchOption.TopDirectoryOnly))
        {
            if (string.Equals(Path.GetFileName(entry), leaf, PathComparison))
                return true;
        }
        return false;
    }

    public static void EnsureDirectoryUnder(string root, string directory)
    {
        AssertNoLinksOnExistingPath(root, directory);
        Directory.CreateDirectory(directory);
        AssertNoLinksOnExistingPath(root, directory);
    }

    public static string Sha256(string path)
    {
        using FileStream input = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        byte[] digest = SHA256.HashData(input);
        return Convert.ToHexString(digest).ToLowerInvariant();
    }

    public static string Sha256Bytes(byte[] bytes)
    {
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    public static bool Matches(string path, long length, string sha256)
    {
        FileInfo info = new(path);
        return info.Exists && info.Length == length &&
               string.Equals(Sha256(path), sha256, StringComparison.OrdinalIgnoreCase);
    }

    public static void WriteAllTextAtomicWithRetry(string path, string text, Func<string, bool>? validator = null)
    {
        Exception? primary = null;
        string directory = Path.GetDirectoryName(path) ?? throw new InvalidDataException("Receipt has no parent directory.");
        Directory.CreateDirectory(directory);

        foreach (int delay in RetryDelaysMs)
        {
            if (delay > 0)
                Thread.Sleep(delay);
            string token = Guid.NewGuid().ToString("N");
            string temporary = path + ".tmp-" + token;
            string backup = path + ".bak-" + token;
            string rejected = path + ".rejected-" + token;
            bool hadOriginal = false;
            bool published = false;
            try
            {
                File.WriteAllText(temporary, text, new UTF8Encoding(false));
                using (FileStream written = new(temporary, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
                    written.Flush(true);

                if (File.Exists(path))
                {
                    hadOriginal = true;
                    // File.Replace is one same-volume filesystem operation: the old
                    // receipt remains either at the final name or at backup. There is
                    // no Move(old) -> crash -> missing-final window.
                    File.Replace(temporary, path, backup, ignoreMetadataErrors: true);
                    published = true;
                }
                else
                {
                    File.Move(temporary, path);
                    published = true;
                }

                if (validator is not null && !validator(path))
                    throw new InvalidDataException("The published receipt failed read-back validation: " + path);
                TryDeleteFile(backup);
                return;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                primary = ex;
                if (!TryRestorePublishedFile(path, backup, rejected, hadOriginal, published, out string restoreDetail))
                    throw new IOException("Receipt publication failed and the previous receipt could not be restored atomically: " + restoreDetail, ex);
            }
            catch
            {
                if (!TryRestorePublishedFile(path, backup, rejected, hadOriginal, published, out string restoreDetail))
                    throw new IOException("Receipt validation failed and the previous receipt could not be restored atomically: " + restoreDetail);
                throw;
            }
            finally
            {
                TryDeleteFile(temporary);
                TryDeleteFile(rejected);
            }
        }

        throw new IOException("The receipt could not be published after bounded retries: " + path, primary);
    }

    private static bool TryRestorePublishedFile(
        string path,
        string backup,
        string rejected,
        bool hadOriginal,
        bool published,
        out string detail)
    {
        try
        {
            if (hadOriginal)
            {
                if (!File.Exists(backup))
                {
                    // Replace did not publish, so the original final receipt must
                    // still be present. Never delete it merely because this attempt
                    // failed before publication.
                    if (File.Exists(path) && !published)
                    {
                        detail = "The original receipt remained at the final path.";
                        return true;
                    }

                    detail = "Neither the original final receipt nor its atomic backup is available.";
                    return false;
                }

                if (File.Exists(path))
                {
                    File.Replace(backup, path, rejected, ignoreMetadataErrors: true);
                    TryDeleteFile(rejected);
                }
                else
                {
                    // This branch is only a recovery from an external interruption;
                    // the normal File.Replace publication never removes both names.
                    File.Move(backup, path);
                }
                detail = "The previous receipt was restored.";
                return File.Exists(path);
            }

            if (published && File.Exists(path))
                File.Delete(path);
            detail = "The newly-created receipt was removed.";
            return !File.Exists(path);
        }
        catch (Exception ex)
        {
            detail = ex.Message;
            return false;
        }
    }

    public static void AssertTreeHasNoLinks(string root)
    {
        string normalizedRoot = NormalizeRoot(root);
        if (!Directory.Exists(normalizedRoot))
            return;

        Stack<string> pending = new();
        pending.Push(normalizedRoot);
        while (pending.Count > 0)
        {
            string directory = pending.Pop();
            AssertNoLinksOnExistingPath(normalizedRoot, directory);
            foreach (string entry in Directory.EnumerateFileSystemEntries(directory, "*", SearchOption.TopDirectoryOnly))
            {
                AssertNoLinksOnExistingPath(normalizedRoot, entry);
                if (Directory.Exists(entry))
                    pending.Push(entry);
            }
        }
    }

    public static void CopyStableFile(string source, string destination, int attempts = 3)
    {
        Exception? last = null;
        for (int attempt = 0; attempt < attempts; attempt++)
        {
            try
            {
                FileInfo before = new(source);
                if (!before.Exists)
                    throw new FileNotFoundException("Source file is missing.", source);
                long length = before.Length;
                DateTime writeUtc = before.LastWriteTimeUtc;
                string hash = Sha256(source);

                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Copy(source, destination, true);

                FileInfo after = new(source);
                if (!after.Exists || after.Length != length || after.LastWriteTimeUtc != writeUtc ||
                    !string.Equals(hash, Sha256(source), StringComparison.OrdinalIgnoreCase) ||
                    !Matches(destination, length, hash))
                    throw new IOException("The source changed while it was being copied: " + source);
                return;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                last = ex;
                TryDeleteFile(destination);
                Thread.Sleep(100 * (attempt + 1));
            }
        }

        throw new IOException("Stable copy failed: " + source, last);
    }

    public static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // Cleanup is best-effort and must not replace the primary failure.
        }
    }

    public static void DeleteDirectoryIfEmpty(string root, string path)
    {
        try
        {
            AssertNoLinksOnExistingPath(root, path);
            if (Directory.Exists(path) && Directory.GetFileSystemEntries(path).Length == 0)
            {
                AssertNoLinksOnExistingPath(root, path);
                Directory.Delete(path);
            }
        }
        catch
        {
            // Link, race and empty-directory cleanup are non-authoritative.
        }
    }
}
