using System.Globalization;
using System.IO;

namespace DTMAPI.MoreSaves
{
    internal static class MoreSavesFileIdentity
    {
        // A lightweight observation of the selected file, not a content proof.
        // Commit operations and field-preservation checks own the data boundary.
        internal static string ReadStamp(string path, out bool exists)
        {
            path = Path.GetFullPath(path);
            try
            {
                FileAttributes attributes = File.GetAttributes(path);
                exists = true;
                if ((attributes & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0)
                    throw new IOException("The native archive is not an ordinary file.");
                var info = new FileInfo(path);
                return path + "|" + info.Length.ToString(CultureInfo.InvariantCulture) + "|" +
                    info.LastWriteTimeUtc.Ticks.ToString(CultureInfo.InvariantCulture);
            }
            catch (FileNotFoundException) { exists = false; return path + "|absent"; }
            catch (DirectoryNotFoundException) { exists = false; return path + "|absent"; }
        }
    }
}
