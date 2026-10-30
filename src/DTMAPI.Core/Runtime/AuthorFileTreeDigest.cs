namespace DTMAPI.Core.Runtime
{
    internal static class AuthorFileTreeDigest
    {
        internal const string Algorithm = DTMAPI.Internal.AuthorTreeDigest.Algorithm;
        public static string Compute(string rootPath) => DTMAPI.Internal.AuthorTreeDigest.Capture(rootPath).Sha256;
    }
}
