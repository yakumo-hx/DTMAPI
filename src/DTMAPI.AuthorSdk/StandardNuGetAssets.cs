using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using NuGet.Packaging;
using System.Threading;

namespace DTMAPI.AuthorSdk;

internal static class StandardNuGetAssets
{
    // Inspect the standard restore result; never perform package/TFM/asset selection.
    internal static void Verify(string assetsFile)
    {
        try { VerifyCore(assetsFile); }
        catch (Exception ex) when (ex is NuGet.Packaging.Core.PackagingException or CryptographicException)
        { throw new InvalidDataException("nuget-package-integrity: " + ex.Message, ex); }
    }

    private static void VerifyCore(string assetsFile)
    {
        var assets = JsonNode.Parse(File.ReadAllBytes(assetsFile))!.AsObject();
        string[] folders = assets["packageFolders"]!.AsObject().Select(p => p.Key).ToArray();
        foreach (var pair in assets["libraries"]!.AsObject())
        {
            var library = pair.Value!.AsObject();
            if ((string?)library["type"] != "package") continue;
            string relative = (string)library["path"]!;
            string root = folders.Select(folder => Path.GetFullPath(relative, folder)).FirstOrDefault(Directory.Exists)
                ?? throw new InvalidDataException("Standard NuGet package cache is missing: " + pair.Key);
            string name = pair.Key.Replace('/', '.').ToLowerInvariant() + ".nupkg";
            string package = Path.Combine(root, name);
            if (!File.Exists(package)) throw new InvalidDataException("Standard NuGet archive is missing: " + package);
            using var reader = new PackageArchiveReader(package);
            string hash = reader.GetContentHash(CancellationToken.None, () => Convert.ToBase64String(SHA512.HashData(File.ReadAllBytes(package))));
            if (hash != (string?)library["sha512"]) throw new InvalidDataException("nuget-package-hash-mismatch: " + pair.Key);
            try
            {
                var signature = reader.GetPrimarySignatureAsync(CancellationToken.None).GetAwaiter().GetResult();
                if (signature != null) reader.ValidateIntegrityAsync(signature.SignatureContent, CancellationToken.None).GetAwaiter().GetResult();
            }
            catch (Exception ex) when (ex is NuGet.Packaging.Signing.SignatureException or CryptographicException)
            { throw new InvalidDataException("nuget-signed-package-integrity: " + pair.Key + ": " + ex.Message, ex); }
            var identity = reader.GetIdentity();
            if (!(identity.Id + "/" + identity.Version.ToNormalizedString()).Equals(pair.Key, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("nuget-package-identity-mismatch: " + pair.Key);
            using var archive = ZipFile.OpenRead(package);
            foreach (var entry in archive.Entries.Where(e => !e.FullName.EndsWith('/')))
            {
                // NuGet extracts its package payload; OPC metadata is deliberately not extracted.
                if (entry.FullName is "[Content_Types].xml" or ".signature.p7s" || entry.FullName.StartsWith("_rels/", StringComparison.Ordinal) || entry.FullName.StartsWith("package/", StringComparison.Ordinal)) continue;
                string path = PathSafety.ResolveUnderRoot(root, Uri.UnescapeDataString(entry.FullName), "NuGet payload");
                // NuGet normalizes the nuspec filename; compilation assets retain exact names.
                if (entry.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase)) continue;
                if (!File.Exists(path)) throw new InvalidDataException("nuget-extracted-asset-missing: " + pair.Key + "/" + entry.FullName);
                PathSafety.RejectReparsePoints(root, new[] { path });
                using var original = entry.Open();
                using var actual = File.OpenRead(path);
                if (!SHA256.HashData(original).SequenceEqual(SHA256.HashData(actual)))
                    throw new InvalidDataException("nuget-extracted-asset-changed: " + pair.Key + "/" + entry.FullName);
            }
        }
    }
}
