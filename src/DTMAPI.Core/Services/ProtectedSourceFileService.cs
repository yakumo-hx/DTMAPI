using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using DTMAPI.Abstractions;
using Microsoft.Win32.SafeHandles;

namespace DTMAPI.Core.Services
{
    // OS responsibility only: MES owns decoding, player choice and native commit proof.
    internal sealed class ProtectedSourceFileService : IProtectedSourceFiles, IDisposable
    {
        internal const string Owner = "DTMAPI.MoreEquipmentSlotsMod";
        private const string FileName = "equipment-slots-" + Owner + ".json";
        private const string SlotPrefix = "protected-items/equipment-slots/slot-";
        private const int MaximumBytes = 1024 * 1024;
        private readonly string root;
        private readonly Action guard;
        private readonly Action threadGuard;
        private readonly HashSet<Snapshot> snapshots = new HashSet<Snapshot>();
        private bool disposed;

        internal ProtectedSourceFileService(string configRoot, Action guard, Action threadGuard)
        { root = Path.GetFullPath(configRoot); this.guard = guard; this.threadGuard = threadGuard; }

        private bool IsActive()
        {
            threadGuard();
            if (disposed) return false;
            try { guard(); return true; }
            catch (ObjectDisposedException) { return false; }
        }

        public ProtectedSourceResult Capture(string relativeKey)
        {
            if (!IsActive()) return new ProtectedSourceResult(ProtectedSourceStatus.Expired);
            string key;
            try { key = OwnerFilePath.NormalizeKey(relativeKey); }
            catch (ArgumentException) { return new ProtectedSourceResult(ProtectedSourceStatus.Rejected, "Invalid relative key."); }
            if (!AllowedKey(key)) return new ProtectedSourceResult(ProtectedSourceStatus.Rejected, "Only the MES canonical sources and exact scoped previous source are supported.");
            return Operate(key, null, false);
        }

        private static bool AllowedKey(string key)
        {
            if (key == FileName) return true;
            string suffix = "/" + FileName;
            if (key.EndsWith(suffix + ".previous", StringComparison.Ordinal)) suffix += ".previous";
            if (!key.StartsWith(SlotPrefix, StringComparison.Ordinal) || !key.EndsWith(suffix, StringComparison.Ordinal)) return false;
            string number = key.Substring(SlotPrefix.Length, key.Length - SlotPrefix.Length - suffix.Length);
            return int.TryParse(number, NumberStyles.None, CultureInfo.InvariantCulture, out int index) && index >= 0 &&
                number == index.ToString(CultureInfo.InvariantCulture);
        }

        private ProtectedSourceResult Operate(string key, Snapshot? expected, bool delete)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return new ProtectedSourceResult(ProtectedSourceStatus.Unsupported, "Protected sources require Windows local NTFS.");
            var handles = new List<SafeFileHandle>();
            var identities = new List<Identity>();
            byte[]? bytes = null;
            bool committed = false;
            ProtectedSourceResult result = new ProtectedSourceResult(ProtectedSourceStatus.IoError);
            try
            {
                string path = Path.Combine(root, key.Replace('/', Path.DirectorySeparatorChar));
                string drive = Path.GetPathRoot(path)!;
                if (drive.Length != 3 || drive[1] != ':' || !char.IsLetter(drive[0]) || GetDriveType(drive) != 3)
                    throw new SourceFailure(ProtectedSourceStatus.Unsupported, "Only local fixed NTFS volumes are supported.");
                // Lock top-down, including ancestors outside the config root. Every next
                // path component is opened while its parents still deny write/delete.
                string cursor = drive;
                var directories = new List<string> { cursor };
                string parent = Path.GetDirectoryName(path)!;
                foreach (string part in parent.Substring(drive.Length).Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries))
                { cursor = Path.Combine(cursor, part); directories.Add(cursor); }
                foreach (string directory in directories)
                {
                    SafeFileHandle handle = Open(directory, 0x80, true);
                    handles.Add(handle);
                    FileInformation info = Inspect(handle, directory, true);
                    if (identities.Count == 0)
                    {
                        var fileSystem = new StringBuilder(64);
                        if (!GetVolumeInformationByHandle(handle, null, 0, out uint _, out uint _, out uint _, fileSystem, fileSystem.Capacity)) ThrowNative();
                        if (fileSystem.ToString() != "NTFS") throw new SourceFailure(ProtectedSourceStatus.Unsupported, "File identity is supported only on NTFS.");
                    }
                    else if (info.Volume != identities[0].Volume)
                        throw new SourceFailure(ProtectedSourceStatus.Rejected, "A source ancestor crossed a volume boundary.");
                    identities.Add(new Identity(info));
                }
                SafeFileHandle file = Open(path, 0x80000000u | (delete ? 0x00010000u : 0u), false);
                handles.Add(file);
                FileInformation fileInfo = Inspect(file, path, false);
                if (fileInfo.Volume != identities[0].Volume || fileInfo.Links != 1)
                    throw new SourceFailure(ProtectedSourceStatus.Rejected, "Source must be an unaliased ordinary file on the protected volume.");
                identities.Add(new Identity(fileInfo));
                if (expected != null && !identities.SequenceEqual(expected.Identities))
                    throw new SourceFailure(ProtectedSourceStatus.Changed, "The source or an ancestor was replaced.");
                if (fileInfo.SizeHigh != 0 || fileInfo.SizeLow > MaximumBytes)
                    throw new SourceFailure(ProtectedSourceStatus.Rejected, "Source exceeds the 1 MiB limit.");
                bytes = Read(file, (int)fileInfo.SizeLow);
                if (expected != null && !bytes.SequenceEqual(expected.Bytes!))
                    throw new SourceFailure(ProtectedSourceStatus.Changed, "Source bytes changed.");
                // Recheck actual handles and final names before disposition; never close
                // protection and then delete by path. No OS handle survives this method.
                for (int i = 0; i < directories.Count; i++)
                    if (!new Identity(Inspect(handles[i], directories[i], true)).Equals(identities[i]))
                        throw new SourceFailure(ProtectedSourceStatus.Changed, "Source ancestry changed during validation.");
                if (!new Identity(Inspect(file, path, false)).Equals(identities[identities.Count - 1]))
                    throw new SourceFailure(ProtectedSourceStatus.Changed, "Source identity changed during validation.");
                if (delete)
                {
                    var disposition = new FileDisposition { DeleteFile = 1 };
                    if (!SetFileInformationByHandle(file, 4, ref disposition, 1)) ThrowNative();
                    committed = true;
                }
                result = new ProtectedSourceResult(delete ? ProtectedSourceStatus.DeleteCommitted : ProtectedSourceStatus.Unchanged);
            }
            catch (SourceFailure ex) { result = new ProtectedSourceResult(ex.Status, ex.Message); }
            catch (DllNotFoundException) { result = new ProtectedSourceResult(ProtectedSourceStatus.Unsupported, "Required Windows file API is unavailable."); }
            catch (EntryPointNotFoundException) { result = new ProtectedSourceResult(ProtectedSourceStatus.Unsupported, "Required Windows file API is unavailable."); }
            catch (PlatformNotSupportedException) { result = new ProtectedSourceResult(ProtectedSourceStatus.Unsupported, "Required file handle support is unavailable."); }
            catch (UnauthorizedAccessException) { result = new ProtectedSourceResult(ProtectedSourceStatus.AccessDenied, "Source access was denied."); }
            catch (IOException) { result = new ProtectedSourceResult(ProtectedSourceStatus.IoError, "Source IO failed."); }
            catch (ArgumentException) { result = new ProtectedSourceResult(ProtectedSourceStatus.Rejected, "Unsupported source path."); }
            finally
            {
                // SafeFileHandle owns each native handle. A disposition that succeeded
                // cannot be reported as retained, even if releasing a handle fails.
                for (int i = handles.Count - 1; i >= 0; i--)
                {
                    try { handles[i].Dispose(); }
                    catch (Exception) { result = new ProtectedSourceResult(ProtectedSourceStatus.IoError, "Source handle release failed."); }
                }
            }
            if (committed) return new ProtectedSourceResult(ProtectedSourceStatus.DeleteCommitted,
                "Deletion was committed on the validated handle; existing handles can delay final removal.");
            if (expected == null && result.Status == ProtectedSourceStatus.Unchanged)
            {
                var snapshot = new Snapshot(this, key, bytes!, identities.ToArray());
                snapshots.Add(snapshot);
                return new ProtectedSourceResult(ProtectedSourceStatus.Captured, snapshot: snapshot);
            }
            return result;
        }

        public void Dispose()
        {
            threadGuard();
            if (disposed) return;
            disposed = true;
            foreach (Snapshot snapshot in snapshots) snapshot.Clear();
            snapshots.Clear();
        }

        private sealed class Snapshot : IProtectedSourceSnapshot
        {
            private readonly ProtectedSourceFileService service;
            private readonly string key;
            internal byte[]? Bytes { get; private set; }
            internal Identity[] Identities { get; private set; }
            private bool consumed;
            internal Snapshot(ProtectedSourceFileService service, string key, byte[] bytes, Identity[] identities)
            { this.service = service; this.key = key; Bytes = bytes; Identities = identities; }
            private bool Active() => service.IsActive() && Bytes != null && !consumed;
            public string RelativeKey { get { RequireActive(); return key; } }
            public byte[] CopyBytes() { RequireActive(); return (byte[])Bytes!.Clone(); }
            private void RequireActive() { if (!Active()) throw new ObjectDisposedException(nameof(IProtectedSourceSnapshot)); }
            public ProtectedSourceResult Revalidate() => Active() ? service.Operate(key, this, false) : new ProtectedSourceResult(ProtectedSourceStatus.Expired);
            public ProtectedSourceResult TryDelete()
            {
                if (!Active()) return new ProtectedSourceResult(ProtectedSourceStatus.Expired);
                consumed = true;
                return service.Operate(key, this, true);
            }
            public void Dispose() { service.threadGuard(); Clear(); service.snapshots.Remove(this); }
            internal void Clear() { Bytes = null; Identities = Array.Empty<Identity>(); consumed = true; }
        }

        private readonly struct Identity : IEquatable<Identity>
        {
            internal readonly uint Volume;
            private readonly uint high, low;
            private readonly long creation;
            internal Identity(FileInformation info) { Volume = info.Volume; high = info.IndexHigh; low = info.IndexLow; creation = info.Creation; }
            public bool Equals(Identity other) => Volume == other.Volume && high == other.high && low == other.low && creation == other.creation;
        }

        private static SafeFileHandle Open(string path, uint access, bool directory)
        {
            SafeFileHandle handle = CreateFile(path, access, 1, IntPtr.Zero, 3, 0x00200000u | 0x02000000u, IntPtr.Zero);
            if (handle.IsInvalid) { int error = Marshal.GetLastWin32Error(); handle.Dispose(); ThrowNative(error); }
            return handle;
        }

        private static FileInformation Inspect(SafeFileHandle handle, string expectedPath, bool directory)
        {
            if (GetFileType(handle) != 1) throw new SourceFailure(ProtectedSourceStatus.Rejected, "Source is not a disk file.");
            if (!GetFileInformationByHandle(handle, out FileInformation info)) ThrowNative();
            if ((info.Attributes & 0x400) != 0 || ((info.Attributes & 0x10) != 0) != directory)
                throw new SourceFailure(ProtectedSourceStatus.Rejected, "Reparse points and nonordinary source paths are rejected.");
            if (info.IndexHigh == 0 && info.IndexLow == 0) throw new SourceFailure(ProtectedSourceStatus.Unsupported, "Reliable file identity is unavailable.");
            var path = new StringBuilder(32768);
            uint length = GetFinalPathNameByHandle(handle, path, path.Capacity, 0);
            if (length == 0) ThrowNative();
            if (length >= path.Capacity || !string.Equals(path.ToString().TrimEnd('\\'), ("\\\\?\\" + expectedPath).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                throw new SourceFailure(ProtectedSourceStatus.Rejected, "Opened source path is outside its exact expected location.");
            return info;
        }

        private static byte[] Read(SafeFileHandle file, int length)
        {
            byte[] bytes = new byte[length], chunk = new byte[65536];
            int offset = 0;
            while (offset < length)
            {
                if (!ReadFile(file, chunk, (uint)Math.Min(chunk.Length, length - offset), out uint count, IntPtr.Zero)) ThrowNative();
                if (count == 0) throw new SourceFailure(ProtectedSourceStatus.Changed, "Source length changed while reading.");
                Buffer.BlockCopy(chunk, 0, bytes, offset, (int)count); offset += (int)count;
            }
            if (!ReadFile(file, chunk, 1, out uint extra, IntPtr.Zero)) ThrowNative();
            if (extra != 0) throw new SourceFailure(ProtectedSourceStatus.Changed, "Source length changed while reading.");
            return bytes;
        }

        private sealed class SourceFailure : Exception
        {
            internal ProtectedSourceStatus Status { get; }
            internal SourceFailure(ProtectedSourceStatus status, string message) : base(message) { Status = status; }
        }
        private static void ThrowNative(int error = -1)
        {
            if (error == -1) error = Marshal.GetLastWin32Error();
            ProtectedSourceStatus status = error == 2 || error == 3 ? ProtectedSourceStatus.Missing :
                error == 32 || error == 33 || error == 303 ? ProtectedSourceStatus.Busy :
                error == 5 || error == 19 || error == 1314 ? ProtectedSourceStatus.AccessDenied :
                error == 1 || error == 50 || error == 120 ? ProtectedSourceStatus.Unsupported :
                error == 123 || error == 267 ? ProtectedSourceStatus.Rejected : ProtectedSourceStatus.IoError;
            throw new SourceFailure(status, "Windows source operation failed (" + error.ToString(CultureInfo.InvariantCulture) + ").");
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct FileInformation
        {
            internal uint Attributes;
            // FILETIME consists of two DWORDs; explicit uint fields avoid 8-byte padding.
            internal uint CreationLow, CreationHigh, AccessLow, AccessHigh, WriteLow, WriteHigh;
            internal uint Volume, SizeHigh, SizeLow, Links, IndexHigh, IndexLow;
            internal long Creation => ((long)CreationHigh << 32) | CreationLow;
        }
        [StructLayout(LayoutKind.Sequential)] private struct FileDisposition { internal byte DeleteFile; }
        [DllImport("kernel32.dll", EntryPoint = "CreateFileW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern SafeFileHandle CreateFile(string path, uint access, uint share, IntPtr security, uint mode, uint flags, IntPtr template);
        [DllImport("kernel32.dll", EntryPoint = "GetDriveTypeW", CharSet = CharSet.Unicode)]
        private static extern uint GetDriveType(string root);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern uint GetFileType(SafeFileHandle handle);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool GetFileInformationByHandle(SafeFileHandle handle, out FileInformation info);
        [DllImport("kernel32.dll", EntryPoint = "GetVolumeInformationByHandleW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool GetVolumeInformationByHandle(SafeFileHandle handle, StringBuilder? name, int nameSize, out uint serial, out uint componentLength, out uint flags, StringBuilder fileSystem, int fileSystemSize);
        [DllImport("kernel32.dll", EntryPoint = "GetFinalPathNameByHandleW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint GetFinalPathNameByHandle(SafeFileHandle handle, StringBuilder path, int size, uint flags);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool ReadFile(SafeFileHandle handle, byte[] bytes, uint size, out uint read, IntPtr overlapped);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool SetFileInformationByHandle(SafeFileHandle handle, int kind, ref FileDisposition value, uint size);
    }
}
