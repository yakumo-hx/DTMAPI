using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace DTMAPI.MonsterContent
{
    internal static class ContentImages
    {
        internal static string Hash(byte[] bytes) { using var hash = SHA256.Create(); return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
        internal static string ImagePath(byte[] bytes) => "images/" + Hash(bytes) + ".png";
        internal static void ValidateResourcePath(string path, ContentJson json)
        {
            if (path.Length != 75 || !path.StartsWith("images/", StringComparison.Ordinal) || !path.EndsWith(".png", StringComparison.Ordinal) || !path.Substring(7, 64).All(c => c >= '0' && c <= '9' || c >= 'a' && c <= 'f'))
                throw json.Error("InvalidResourcePath", "image", "Only content-addressed package-relative PNG paths are accepted.");
        }
        internal static void CheckPng(byte[] bytes, ContentJson json, int? width = null, int? height = null)
        {
            if (bytes.Length < 33 || !bytes.Take(8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) || Encoding.ASCII.GetString(bytes, 12, 4) != "IHDR")
                throw json.Error("InvalidImage", "image", "Expected PNG with IHDR.");
            long Size(int i) => (long)bytes[i] << 24 | (long)bytes[i + 1] << 16 | (long)bytes[i + 2] << 8 | bytes[i + 3];
            long w = Size(16), h = Size(20);
            if (w < 1 || h < 1 || width.HasValue && width != w || height.HasValue && height != h)
                throw json.Error("InvalidImage", "image.dimensions", "PNG dimensions must match and be positive.");
        }
    }
}
