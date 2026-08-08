using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using DTMAPI.Authoring.Contracts;

namespace DTMAPI.AuthorSdk;

internal static class AuthorFileTreeDigest
{
    public const string AlgorithmId = "DTMAPI-FileTree-SHA256-v1";

    public static string Compute(string root)
    {
        string fullRoot = Path.GetFullPath(root);
        if (!Directory.Exists(fullRoot))
            throw new DirectoryNotFoundException("Tree root was not found: " + fullRoot);
        string[] entries = Directory.EnumerateFileSystemEntries(fullRoot, "*", SearchOption.AllDirectories).ToArray();
        PathSafety.RejectReparsePoints(fullRoot, entries);
        string[] files = Directory.EnumerateFiles(fullRoot, "*", SearchOption.AllDirectories)
            .OrderBy(path => PathSafety.RelativePath(fullRoot, path), StringComparer.Ordinal)
            .ToArray();
        using var aggregate = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Span<byte> length = stackalloc byte[8];
        foreach (string file in files)
        {
            string relative = PathSafety.RelativePath(fullRoot, file);
            byte[] pathBytes = Encoding.UTF8.GetBytes(relative);
            aggregate.AppendData(pathBytes);
            aggregate.AppendData(new byte[] { 0 });
            BinaryPrimitives.WriteInt64LittleEndian(length, new FileInfo(file).Length);
            aggregate.AppendData(length);
            using FileStream stream = File.OpenRead(file);
            byte[] fileHash = SHA256.HashData(stream);
            aggregate.AppendData(fileHash);
        }
        return Convert.ToHexString(aggregate.GetHashAndReset());
    }

    public static string Compute(DeploymentTreeInventory inventory)
    {
        if (inventory?.Files == null)
            throw new InvalidDataException("Deployment inventory is missing its file list.");

        using var aggregate = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Span<byte> length = stackalloc byte[8];
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (DeploymentInventoryFile file in inventory.Files.OrderBy(row => row.Path, StringComparer.Ordinal))
        {
            if (file == null ||
                string.IsNullOrWhiteSpace(file.Path) ||
                !seen.Add(file.Path) ||
                file.Length < 0 ||
                file.Sha256.Length != 64 ||
                file.Sha256.Any(character => !Uri.IsHexDigit(character)))
            {
                throw new InvalidDataException("Deployment inventory cannot derive an exact source-tree digest.");
            }

            byte[] pathBytes = Encoding.UTF8.GetBytes(file.Path);
            aggregate.AppendData(pathBytes);
            aggregate.AppendData(new byte[] { 0 });
            BinaryPrimitives.WriteInt64LittleEndian(length, file.Length);
            aggregate.AppendData(length);
            aggregate.AppendData(Convert.FromHexString(file.Sha256));
        }
        return Convert.ToHexString(aggregate.GetHashAndReset());
    }
}
