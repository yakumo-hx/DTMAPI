using System;
using System.IO;
using System.Text;
using DTMAPI.Abstractions;

namespace DTMAPI.Core.Services
{
    internal sealed class OwnerFileService : IOwnerFileHelper
    {
        internal const int MaxBytes = 1024 * 1024;
        private readonly string packageRoot;
        private readonly Action ensureActive;
        internal OwnerFileService(string packageRoot, Action ensureActive)
        { this.packageRoot = packageRoot; this.ensureActive = ensureActive; }

        public OwnerDataResult<string> ReadText(string key)
        {
            ensureActive();
            key = OwnerFilePath.NormalizeKey(key);
            try
            {
                string path = OwnerFilePath.Resolve(packageRoot, key);
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (stream.Length > MaxBytes) return new OwnerDataResult<string>(OwnerDataStatus.IoError, null, message: "Package text exceeds the 1 MiB limit.");
                    using (var reader = new StreamReader(stream, new UTF8Encoding(false, true), false))
                        return new OwnerDataResult<string>(OwnerDataStatus.Found, reader.ReadToEnd());
                }
            }
            catch (FileNotFoundException) { return new OwnerDataResult<string>(OwnerDataStatus.Missing, null); }
            catch (DirectoryNotFoundException) { return new OwnerDataResult<string>(OwnerDataStatus.Missing, null); }
            catch (UnauthorizedAccessException) { return new OwnerDataResult<string>(OwnerDataStatus.AccessDenied, null, message: "Package file access denied."); }
            catch (DecoderFallbackException) { return new OwnerDataResult<string>(OwnerDataStatus.Corrupt, null, message: "Package text is not valid UTF-8."); }
            catch (IOException) { return new OwnerDataResult<string>(OwnerDataStatus.IoError, null, message: "Package file could not be read."); }
        }
    }
}
