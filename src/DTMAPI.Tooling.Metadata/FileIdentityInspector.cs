using System;
using System.IO;
using System.Security.Cryptography;

namespace DTMAPI.Tooling.Metadata;

public sealed class FileIdentityInspector
{
    public FileIdentitySnapshot Inspect(string path)
    {
        string fullPath = System.IO.Path.GetFullPath(path);
        try
        {
            using FileStream stream = ReadOnlyFiles.OpenSharedRead(fullPath);
            long length = stream.Length;
            string sha256 = Convert.ToHexString(SHA256.HashData(stream));
            return new FileIdentitySnapshot
            {
                Path = fullPath,
                Length = length,
                Sha256 = sha256
            };
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new FileIdentitySnapshot
            {
                Path = fullPath,
                Error = ex.GetType().Name + ": " + ex.Message
            };
        }
    }
}
