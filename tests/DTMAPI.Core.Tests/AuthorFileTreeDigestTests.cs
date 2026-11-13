using System;
using System.IO;
using System.Text;
using DTMAPI.Core.Runtime;

namespace DTMAPI.UnitTests
{
    internal static class AuthorFileTreeDigestTests
    {
        internal static void AuthorFileTreeDigestMatchesFrozenCrossToolVectors()
        {
            string root = Path.Combine(Path.GetTempPath(), "DTMAPI-file-tree-v1", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                Assert(
                    AuthorFileTreeDigest.Compute(root) == "E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855",
                    "The empty DTMAPI-FileTree-SHA256-v1 vector should remain frozen across Runtime and Author SDK implementations.");

                File.WriteAllText(Path.Combine(root, "a.txt"), "A", new UTF8Encoding(false));
                string nested = Path.Combine(root, "sub");
                Directory.CreateDirectory(nested);
                File.WriteAllBytes(Path.Combine(nested, "β.bin"), new byte[] { 0x00, 0x01, 0x02, 0xFF });
                Assert(
                    AuthorFileTreeDigest.Compute(root) == "8E7C6E58D4982A1C5D749524A8A56A4C2171D7301144EE5640ABAC7AE74CB4CD",
                    "The mixed text/binary DTMAPI-FileTree-SHA256-v1 vector should remain frozen across Runtime and Author SDK implementations.");

                File.WriteAllBytes(Path.Combine(nested, "β.bin"), new byte[] { 0x00, 0x01, 0x02, 0xFE });
                Assert(
                    AuthorFileTreeDigest.Compute(root) != "8E7C6E58D4982A1C5D749524A8A56A4C2171D7301144EE5640ABAC7AE74CB4CD",
                    "Changing file content should change the frozen source-tree digest.");
            }
            finally
            {
                if (Directory.Exists(root))
                    Directory.Delete(root, recursive: true);
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
