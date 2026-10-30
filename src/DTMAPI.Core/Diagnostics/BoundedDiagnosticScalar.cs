using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace DTMAPI.Core.Diagnostics
{
    /// <summary>Bounds retained diagnostic strings without caching caller-owned values.</summary>
    internal static class BoundedDiagnosticScalar
    {
        internal const int OwnerIdChars = 160;
        internal const int IdentifierChars = 192;
        internal const int ShortTextChars = 256;
        internal const int MessageChars = 512;
        internal const int DetailsChars = 2048;
        internal const int RuntimeContextChars = 256 * 1024;
        internal const int ReportSummaryChars = 512 * 1024;
        internal const int MaxDiagnosticRows = 1000;
        internal const int MaxStatusRows = 512;
        internal const int MaxModRows = 4096;

        private const int HashSuffixChars = 26; // ~[sha256:0123456789abcdef]
        private const int HashBufferBytes = 4096;

        internal static string Sanitize(string? value, int maxChars, ref long trimmedBytes)
        {
            string scalar = value ?? string.Empty;
            if (scalar.Length <= maxChars)
                return scalar;
            if (maxChars < HashSuffixChars)
                throw new ArgumentOutOfRangeException(nameof(maxChars), "A bounded diagnostic scalar must retain the complete stable-hash suffix.");

            // Retained diagnostic strings are managed UTF-16 scalars. Count the net
            // UTF-16 bytes omitted after the fixed-size hash suffix is retained.
            AddTrimmedBytes(ref trimmedBytes, (long)(scalar.Length - maxChars) * sizeof(char));
            string suffix = "~[sha256:" + ComputeSha256(scalar, ignoreCase: false).Substring(0, 16) + "]";
            return scalar.Substring(0, maxChars - suffix.Length) + suffix;
        }

        internal static string StableIdentity(string? value)
        {
            string scalar = value ?? string.Empty;
            return scalar.Length <= OwnerIdChars
                ? "value:" + scalar
                : "sha256:" + ComputeSha256(scalar, ignoreCase: true);
        }

        internal static string Sanitize(TextReader reader, int maxChars, ref long trimmedBytes)
        {
            if (reader == null)
                return string.Empty;
            if (maxChars < HashSuffixChars)
                throw new ArgumentOutOfRangeException(nameof(maxChars), "A bounded diagnostic scalar must retain the complete stable-hash suffix.");

            var retained = new StringBuilder(maxChars);
            var chars = new char[HashBufferBytes / sizeof(char)];
            var input = new byte[HashBufferBytes];
            var output = new byte[HashBufferBytes];
            long totalChars = 0;
            using (SHA256 hash = SHA256.Create())
            {
                int read;
                while ((read = reader.Read(chars, 0, chars.Length)) > 0)
                {
                    int byteCount = 0;
                    for (int i = 0; i < read; i++)
                    {
                        char current = chars[i];
                        if (retained.Length < maxChars)
                            retained.Append(current);
                        if (totalChars < long.MaxValue)
                            totalChars++;
                        input[byteCount++] = (byte)current;
                        input[byteCount++] = (byte)(current >> 8);
                    }
                    hash.TransformBlock(input, 0, byteCount, output, 0);
                }
                hash.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                if (totalChars <= maxChars)
                    return retained.ToString();

                long omittedChars = totalChars - maxChars;
                AddTrimmedBytes(ref trimmedBytes, omittedChars > long.MaxValue / sizeof(char) ? long.MaxValue : omittedChars * sizeof(char));
                string suffix = "~[sha256:" + FormatDigest(hash.Hash).Substring(0, 16) + "]";
                return retained.ToString(0, maxChars - suffix.Length) + suffix;
            }
        }

        private static string ComputeSha256(string scalar, bool ignoreCase)
        {
            using (SHA256 hash = SHA256.Create())
            {
                var input = new byte[HashBufferBytes];
                var output = new byte[HashBufferBytes];
                int buffered = 0;
                for (int i = 0; i < scalar.Length; i++)
                {
                    char current = ignoreCase ? char.ToUpperInvariant(scalar[i]) : scalar[i];
                    input[buffered++] = (byte)current;
                    input[buffered++] = (byte)(current >> 8);
                    if (buffered == input.Length)
                    {
                        hash.TransformBlock(input, 0, buffered, output, 0);
                        buffered = 0;
                    }
                }
                hash.TransformFinalBlock(input, 0, buffered);
                return FormatDigest(hash.Hash);
            }
        }

        private static string FormatDigest(byte[]? digest)
        {
            byte[] bytes = digest ?? Array.Empty<byte>();
            var builder = new StringBuilder(bytes.Length * 2);
            for (int i = 0; i < bytes.Length; i++)
                builder.Append(bytes[i].ToString("x2", CultureInfo.InvariantCulture));
            return builder.ToString();
        }

        internal static void AddTrimmedBytes(ref long total, long value)
        {
            if (value <= 0 || total == long.MaxValue)
                return;
            total = total > long.MaxValue - value ? long.MaxValue : total + value;
        }

    }
}
