using System;
using System.IO;
using System.Text;

namespace DTMAPI.MoreSaves
{
    internal interface IMoreSavesRenameHost : IMoreSavesStorageHost
    {
        bool CanRename { get; }
        int NameLimit { get; }
        string DefaultName { get; }
        string TempPathFor(int index);
        string PrevPathFor(int index, int previous);
        int BackupCount { get; }
        void ScrollBackups(int index);
        string EncodeDocument(object document);
        string SummaryName(MoreSavesArchive archive);
    }

    internal class MoreSavesRenameFiles : MoreSavesArchiveFiles
    {
        internal virtual void WriteTemporary(string path, string text, Action acquired)
        {
            using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                acquired();
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false))) writer.Write(text);
            }
        }
        internal virtual void ReplaceCurrent(string temporary, string current, string previous) => File.Replace(temporary, current, previous);
    }

    internal sealed class MoreSavesArchiveRename
    {
        private readonly IMoreSavesRenameHost host;
        private readonly MoreSavesRenameFiles files;
        internal MoreSavesArchiveRename(IMoreSavesRenameHost host, MoreSavesRenameFiles? files = null)
        { this.host = host; this.files = files ?? new MoreSavesRenameFiles(); }

        internal static bool TryName(string input, int limit, string defaultName, out string stored)
        {
            stored = input == defaultName ? string.Empty : input;
            return !string.IsNullOrWhiteSpace(input) && limit > 0 && input.Length <= limit;
        }

        internal MoreSavesOperationResult Rename(MoreSavesArchive source, string input, Func<bool> current)
        {
            MoreSavesOperationResult Result(MoreSavesOperationStatus status, string detail = "") => new MoreSavesOperationResult(status, source.Index, detail);
            bool Valid() => current() && host.OwnsStorage && host.CanRename && source.Availability == ArchiveAvailability.Ready &&
                files.Stamp(host.PathFor(source.Index), out bool exists) == source.Identity && exists;
            string? temporary = null;
            bool acquired = false, attempted = false, committed = false;
            try
            {
                if (!TryName(input, host.NameLimit, host.DefaultName, out string stored)) return Result(MoreSavesOperationStatus.InvalidName);
                if (!Valid()) return Result(MoreSavesOperationStatus.Changed);
                string path = Path.GetFullPath(host.PathFor(source.Index));
                temporary = Path.GetFullPath(host.TempPathFor(source.Index));
                string previous = Path.GetFullPath(host.PrevPathFor(source.Index, 0));
                if (host.BackupCount < 1 || Path.GetDirectoryName(path) != Path.GetDirectoryName(temporary) ||
                    temporary == path || previous != path + ".prev0") throw new InvalidDataException("Unsupported native rename file policy.");
                files.Stamp(temporary, out bool tempExists);
                if (tempExists) return Result(MoreSavesOperationStatus.Failed, "The native temporary already exists; it was retained.");
                for (int i = 0; i < host.BackupCount; i++) files.Stamp(host.PrevPathFor(source.Index, i), out _);
                string original, encoded;
                using (Stream sourceStream = files.OpenSource(path))
                {
                    using (var reader = new StreamReader(sourceStream, Encoding.UTF8, true, 4096, true)) original = host.Decode(reader.ReadToEnd());
                    if (!Valid()) return Result(MoreSavesOperationStatus.Changed);
                    encoded = host.EncodeDocument(MoreSavesArchiveDocument.WithName(original, stored));
                    MoreSavesArchiveDocument.AssertRename(original, host.Decode(encoded), stored);
                    files.WriteTemporary(temporary, encoded, () => acquired = true);
                    MoreSavesArchiveDocument.AssertRename(original, host.Decode(files.ReadText(temporary)), stored);
                    if (!Valid()) return Result(MoreSavesOperationStatus.Changed);
                }
                // Native SaveGame's exact same-slot rotation and atomic replace; never serialize a live archive handle.
                if (!Valid()) return Result(MoreSavesOperationStatus.Changed);
                host.ScrollBackups(source.Index);
                if (!Valid()) return Result(MoreSavesOperationStatus.Changed, "Native previous files may already have rotated.");
                attempted = true;
                files.ReplaceCurrent(temporary, path, previous);
                committed = true;
                acquired = false;
                string written = files.Stamp(path, out bool exists);
                MoreSavesArchiveDocument.AssertRename(original, host.Decode(files.ReadText(path)), stored);
                string expectedPrevious = previous + source.Identity.Substring(path.Length);
                MoreSavesArchive summary = host.Read(source.Index);
                if (!exists || files.Stamp(previous, out bool prevExists) != expectedPrevious || !prevExists ||
                    summary.Availability != ArchiveAvailability.Ready || summary.Identity != files.Stamp(path, out _) || host.SummaryName(summary) != stored ||
                    files.Stamp(path, out _) != written || !current() || !host.OwnsStorage || !host.CanRename)
                    return Result(MoreSavesOperationStatus.CommittedUnverified);
                return new MoreSavesOperationResult(MoreSavesOperationStatus.Success, source.Index, verifiedArchive: summary);
            }
            catch (Exception ex)
            {
                bool uncertain = committed;
                if (attempted && !committed)
                {
                    try
                    {
                        files.Stamp(temporary!, out bool tempExists);
                        uncertain = !tempExists || files.Stamp(host.PathFor(source.Index), out _) != source.Identity;
                    }
                    catch { uncertain = true; }
                }
                if (uncertain) acquired = false; // Keep all remaining commit evidence; no rollback or automatic resubmission.
                return Result(uncertain ? MoreSavesOperationStatus.CommittedUnverified : MoreSavesOperationStatus.Failed,
                    ex.GetType().Name + ": " + ex.Message + "; native previous files may have rotated.");
            }
            finally
            {
                if (acquired && temporary != null) { try { files.RemoveTemporary(temporary); } catch { } }
            }
        }
    }
}
