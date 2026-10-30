using System.Security.Cryptography;
using DTMAPI.Authoring.Contracts;

namespace DTMAPI.AuthorSdk;

internal static class AuthorFileTreeDigest
{
    public const string AlgorithmId = DTMAPI.Internal.AuthorTreeDigest.Algorithm;

    public static string Compute(string root) => Capture(root).Sha256;

    internal static DTMAPI.Internal.AuthorTreeDigestSnapshot Capture(string root) => DTMAPI.Internal.AuthorTreeDigest.Capture(root);

    public static string Compute(DeploymentTreeInventory inventory)
    {
        if (inventory?.Files == null)
            throw new InvalidDataException("Deployment inventory is missing its file list.");

        using var aggregate = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
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

            DTMAPI.Internal.AuthorTreeDigest.Append(aggregate, file.Path, file.Length, Convert.FromHexString(file.Sha256));
        }
        return DTMAPI.Internal.AuthorTreeDigest.Finish(aggregate);
    }
}
