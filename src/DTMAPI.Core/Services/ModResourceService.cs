using System;
using System.IO;
using DTMAPI.Abstractions;
using DTMAPI.Abstractions.Internal;

namespace DTMAPI.Core.Services
{
    internal sealed class ModResourceService : IModResourceHelper
    {
        internal const int MaxBytes = 12 * 1024 * 1024;
        private readonly string root;
        private readonly Action ensureActive;
        internal ModResourceService(string root, Action ensureActive)
        { this.root = root; this.ensureActive = ensureActive; }

        public OwnerDataResult<byte[]> ReadBytes(string key)
            => ReadBytes(key, MaxBytes);

        internal PackageResourceReference BackgroundReference(string key, string sourceIdentity)
        {
            ensureActive(); key = OwnerFilePath.NormalizeKey(key);
            PackageResourceMetadata Inspect()
            {
                ensureActive();
                var info = new FileInfo(OwnerFilePath.Resolve(root, key));
                if (!info.Exists) throw new FileNotFoundException("PackageResourceMissing: " + key);
                var result = new PackageResourceMetadata(sourceIdentity, key, info.Length, info.LastWriteTimeUtc.Ticks);
                ensureActive(); return result;
            }
            Stream Open(PackageResourceMetadata expected, long maximumBytes)
            {
                if (maximumBytes <= 0 || expected.Length > maximumBytes) throw new IOException("PackageResourceBudget: " + key);
                if (!Inspect().Matches(expected)) throw new IOException("PackageResourceChanged: " + key);
                // Open on main while authority is valid. FileShare.Read prevents
                // replacement/writes during the worker's bounded preparation.
                var stream = new FileStream(OwnerFilePath.Resolve(root, key), FileMode.Open, FileAccess.Read, FileShare.Read, 65536, FileOptions.SequentialScan);
                try
                {
                    if (stream.Length != expected.Length || !Inspect().Matches(expected)) throw new IOException("PackageResourceChanged: " + key);
                    return stream;
                }
                catch { stream.Dispose(); throw; }
            }
            return new PackageResourceReference(Inspect, Open);
        }

        internal ResourceReference Reference(string key)
        {
            ensureActive();
            return new ResourceReference(this, OwnerFilePath.NormalizeKey(key));
        }

        internal sealed class ResourceReference
        {
            private readonly ModResourceService source;
            internal ResourceReference(ModResourceService source, string key) { this.source = source; Key = key; }
            public string Key { get; }
            public OwnerDataResult<long> GetLength()
            {
                source.ensureActive();
                try
                {
                    using (var stream = new FileStream(OwnerFilePath.Resolve(source.root, Key), FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        long length = stream.Length; source.ensureActive();
                        return new OwnerDataResult<long>(OwnerDataStatus.Found, length);
                    }
                }
                catch (FileNotFoundException) { return new OwnerDataResult<long>(OwnerDataStatus.Missing, 0); }
                catch (DirectoryNotFoundException) { return new OwnerDataResult<long>(OwnerDataStatus.Missing, 0); }
                catch (UnauthorizedAccessException) { return new OwnerDataResult<long>(OwnerDataStatus.AccessDenied, 0); }
                catch (IOException) { return new OwnerDataResult<long>(OwnerDataStatus.IoError, 0); }
            }
            public OwnerDataResult<byte[]> ReadBytes(int maximumBytes) => source.ReadBytes(Key, maximumBytes);
        }

        private OwnerDataResult<byte[]> ReadBytes(string key, int maximumBytes)
        {
            ensureActive();
            if (maximumBytes <= 0 || maximumBytes > MaxBytes) throw new ArgumentOutOfRangeException(nameof(maximumBytes));
            key = OwnerFilePath.NormalizeKey(key);
            try
            {
                using (var stream = new FileStream(OwnerFilePath.Resolve(root, key), FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (stream.Length > maximumBytes)
                        return new OwnerDataResult<byte[]>(OwnerDataStatus.IoError, null, message: "Package resource exceeds the requested bounded read limit (maximum 12 MiB).");
                    var bytes = new byte[(int)stream.Length];
                    int offset = 0;
                    while (offset < bytes.Length)
                    {
                        int read = stream.Read(bytes, offset, bytes.Length - offset);
                        if (read == 0) throw new IOException("Package resource changed while reading.");
                        offset += read;
                    }
                    if (stream.ReadByte() != -1) throw new IOException("Package resource changed while reading.");
                    ensureActive();
                    return new OwnerDataResult<byte[]>(OwnerDataStatus.Found, bytes);
                }
            }
            catch (FileNotFoundException) { return new OwnerDataResult<byte[]>(OwnerDataStatus.Missing, null); }
            catch (DirectoryNotFoundException) { return new OwnerDataResult<byte[]>(OwnerDataStatus.Missing, null); }
            catch (UnauthorizedAccessException) { return new OwnerDataResult<byte[]>(OwnerDataStatus.AccessDenied, null, message: "Package resource access denied."); }
            catch (IOException) { return new OwnerDataResult<byte[]>(OwnerDataStatus.IoError, null, message: "Package resource could not be read."); }
        }
    }
}
