using System;
using System.IO;
using System.Text;

namespace DTMAPI.MoreSaves
{
    internal enum MoreSavesOperationStatus { Success, Full, Unavailable, Changed, Failed, CommittedUnverified, InvalidName }

    internal sealed class MoreSavesOperationResult
    {
        internal MoreSavesOperationResult(MoreSavesOperationStatus status, int index, string detail = "", MoreSavesArchive? verifiedArchive = null)
        { Status = status; Index = index; Detail = detail; VerifiedArchive = verifiedArchive; }
        internal MoreSavesOperationStatus Status { get; }
        internal int Index { get; }
        internal string Detail { get; }
        internal MoreSavesArchive? VerifiedArchive { get; }
    }

    internal interface IMoreSavesStorageHost
    {
        bool OwnsStorage { get; }
        string PathFor(int index);
        string BackupPathFor(int index);
        MoreSavesArchive Read(int index);
        string Decode(string encoded);
        string CopyWithNativeIndex(string encoded, int targetIndex);
        bool DeleteNative(int index);
    }

    // These narrow disk operations are also exercised against real, disposable files by the fault fixtures.
    internal class MoreSavesArchiveFiles
    {
        internal virtual string Stamp(string path, out bool exists) => MoreSavesFileIdentity.ReadStamp(path, out exists);
        internal virtual Stream OpenSource(string path) => new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        internal virtual string ReadText(string path) => File.ReadAllText(path);
        internal virtual void WriteNew(string path, string text)
        {
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false))) writer.Write(text);
        }
        internal virtual void MoveNew(string source, string target) => File.Move(source, target);
        internal virtual void RemoveTemporary(string path) => File.Delete(path);
    }

    internal sealed class MoreSavesArchiveStorage
    {
        private readonly IMoreSavesStorageHost host;
        private readonly MoreSavesArchiveFiles files;
        internal MoreSavesArchiveStorage(IMoreSavesStorageHost host, MoreSavesArchiveFiles? files = null)
        { this.host = host; this.files = files ?? new MoreSavesArchiveFiles(); }

        internal MoreSavesOperationResult Duplicate(MoreSavesArchive source, Func<bool> current)
        {
            int target = -1;
            string? temporary = null;
            bool attempted = false, committed = false;
            try
            {
                if (!Valid(source, current)) return Result(MoreSavesOperationStatus.Changed, source.Index);
                // A bad summary before the first proven hole prevents choosing the
                // lowest empty slot. Later slots cannot change that choice.
                for (int index = 0; index < MoreSavesPageSession.PageSize * MoreSavesPageSession.PageCount; index++)
                {
                    MoreSavesArchive candidate = host.Read(index);
                    if (candidate.Availability == ArchiveAvailability.Unavailable) return Result(MoreSavesOperationStatus.Unavailable, index);
                    string identity = files.Stamp(host.PathFor(index), out bool exists);
                    if (identity != candidate.Identity) return Result(MoreSavesOperationStatus.Changed, index);
                    if (!exists) { target = index; break; }
                }
                if (target < 0) return Result(MoreSavesOperationStatus.Full, source.Index);
                string destination = Path.GetFullPath(host.PathFor(target));
                string emptyIdentity = files.Stamp(destination, out bool targetExists);
                if (targetExists) return Result(MoreSavesOperationStatus.Changed, target);
                // On Windows this also denies writes/replacement of the source throughout encoding and commit.
                using (Stream heldSource = files.OpenSource(host.PathFor(source.Index)))
                {
                    string encoded;
                    using (var reader = new StreamReader(heldSource, Encoding.UTF8, true, 4096, true)) encoded = reader.ReadToEnd();
                    if (!Valid(source, current)) return Result(MoreSavesOperationStatus.Changed, source.Index);
                    string originalJson = host.Decode(encoded);
                    string copy = host.CopyWithNativeIndex(encoded, target);
                    MoreSavesArchiveDocument.AssertCopy(originalJson, host.Decode(copy), source.Index, target);
                    temporary = destination + ".moresaves-" + Guid.NewGuid().ToString("N") + ".tmp";
                    files.WriteNew(temporary, copy);
                    MoreSavesArchiveDocument.AssertCopy(originalJson, host.Decode(files.ReadText(temporary)), source.Index, target);
                    if (!Valid(source, current) || files.Stamp(destination, out _) != emptyIdentity)
                        return Result(MoreSavesOperationStatus.Changed, target);
                    // File.Move(source, destination) is the no-overwrite commit, including a target appearing after the check.
                    attempted = true;
                    files.MoveNew(temporary, destination);
                    committed = true;
                    temporary = null;
                    string written = files.Stamp(destination, out bool exists);
                    MoreSavesArchiveDocument.AssertCopy(originalJson, host.Decode(files.ReadText(destination)), source.Index, target);
                    MoreSavesArchive summary = host.Read(target);
                    if (!exists || summary.Availability != ArchiveAvailability.Ready || summary.Identity != files.Stamp(destination, out _) ||
                        files.Stamp(destination, out _) != written || !current() || !host.OwnsStorage)
                        return Result(MoreSavesOperationStatus.CommittedUnverified, target);
                    return Result(MoreSavesOperationStatus.Success, target);
                }
            }
            catch (Exception ex)
            {
                bool uncertain = committed;
                if (attempted && !committed)
                {
                    try
                    {
                        files.Stamp(temporary!, out bool tempExists);
                        // A consumed unique temporary means the move may have completed.
                        uncertain = !tempExists;
                    }
                    catch { uncertain = true; }
                }
                if (uncertain) temporary = null; // Retain evidence; never retry or remove a possibly committed target.
                return Result(uncertain ? MoreSavesOperationStatus.CommittedUnverified : MoreSavesOperationStatus.Failed,
                    target >= 0 ? target : source.Index, ex.GetType().Name + ": " + ex.Message);
            }
            finally
            {
                // Only the unique uncommitted temporary is ours to remove. Never remove a committed target or restore a source.
                if (temporary != null) { try { files.RemoveTemporary(temporary); } catch { } }
            }
        }

        internal MoreSavesOperationResult Delete(MoreSavesArchive source, Func<bool> current)
        {
            bool attempted = false;
            try
            {
                string path = Path.GetFullPath(host.PathFor(source.Index));
                string backup = Path.GetFullPath(host.BackupPathFor(source.Index));
                files.Stamp(backup, out _); // Reject a non-file backup before native deletion starts.
                if (!Valid(source, current)) return Result(MoreSavesOperationStatus.Changed, source.Index);
                attempted = true;
                bool accepted = host.DeleteNative(source.Index);
                files.Stamp(path, out bool exists);
                string actualBackup = files.Stamp(backup, out bool backupExists);
                string expectedBackup = backup + source.Identity.Substring(path.Length);
                if (accepted && !exists && backupExists && actualBackup == expectedBackup)
                    return Result(MoreSavesOperationStatus.Success, source.Index);
                return Result(exists ? MoreSavesOperationStatus.Failed : MoreSavesOperationStatus.CommittedUnverified, source.Index,
                    "Native delete returned " + accepted + "; current present=" + exists + "; matching backup=" + (backupExists && actualBackup == expectedBackup));
            }
            catch (Exception ex)
            { return Result(attempted ? MoreSavesOperationStatus.CommittedUnverified : MoreSavesOperationStatus.Failed, source.Index, ex.GetType().Name + ": " + ex.Message); }
        }

        private bool Valid(MoreSavesArchive source, Func<bool> current) => current() && host.OwnsStorage &&
            source.Availability == ArchiveAvailability.Ready && files.Stamp(host.PathFor(source.Index), out bool exists) == source.Identity && exists;
        private static MoreSavesOperationResult Result(MoreSavesOperationStatus status, int index, string detail = "") => new MoreSavesOperationResult(status, index, detail);
    }
}
