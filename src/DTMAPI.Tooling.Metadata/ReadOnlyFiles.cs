using System;
using System.IO;

namespace DTMAPI.Tooling.Metadata;

internal static class ReadOnlyFiles
{
    public static FileStream OpenSharedRead(string path)
    {
        return new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            bufferSize: 128 * 1024,
            options: FileOptions.SequentialScan);
    }
}
