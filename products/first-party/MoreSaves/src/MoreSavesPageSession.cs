using System;

namespace DTMAPI.MoreSaves
{
    internal enum ArchiveAvailability { Empty, Ready, Unavailable }

    // An observation of one native current file, not a second save identity system.
    internal sealed class MoreSavesArchive
    {
        internal MoreSavesArchive(int index, ArchiveAvailability availability, string identity, object? data = null)
        { Index = index; Availability = availability; Identity = identity; Data = data; }
        internal int Index { get; }
        internal ArchiveAvailability Availability { get; }
        internal string Identity { get; }
        internal object? Data { get; }
    }

    internal interface IMoreSavesPageHost
    {
        MoreSavesArchive Read(int index);
        bool Matches(MoreSavesArchive archive);
        void Render(int page, MoreSavesArchive[] archives);
        void Select(int globalIndex, int cell, ArchiveAvailability availability);
        void ConfirmLoad(MoreSavesArchive archive, Action confirmed);
        void HandoffLoad(Action dispatch);
        void Load(bool isNew, int globalIndex);
        void RejectLoad();
        void ConfirmDelete(int globalIndex, Action confirmed);
        MoreSavesOperationResult Delete(MoreSavesArchive archive, Func<bool> current);
        MoreSavesOperationResult Duplicate(MoreSavesArchive archive, Func<bool> current);
        MoreSavesOperationResult Rename(MoreSavesArchive archive, string name, Func<bool> current);
        void ResetRenameInput(MoreSavesArchive? archive);
        void RenderRenamed(int cell, MoreSavesArchive archive);
        void NoticeOperation(string operation, MoreSavesOperationResult result);
        void Notice(string key);
    }

    internal sealed class MoreSavesPageSession
    {
        internal const int PageSize = 12;
        internal const int PageCount = 5;
        private readonly IMoreSavesPageHost host;
        private readonly Func<bool> ownsNative;
        private MoreSavesArchive[] archives = Array.Empty<MoreSavesArchive>();
        private int selectionRevision;
        private int launchRevision;
        private int? loadingTransition;
        private bool retired;
        private MoreSavesArchive? renameTarget;
        private int renameRevision;
        internal string DraftName { get; private set; } = string.Empty;

        internal MoreSavesPageSession(IMoreSavesPageHost host, Func<bool> ownsNative)
        { this.host = host; this.ownsNative = ownsNative; }
        internal bool IsOpen { get; private set; }
        internal int Page { get; private set; }
        internal int Cell { get; private set; }
        internal int GlobalIndex => Page * PageSize + Cell;

        internal void Open()
        {
            if (retired || !ownsNative()) throw new InvalidOperationException("MoreSaves page has no native owner.");
            launchRevision++;
            loadingTransition = null;
            IsOpen = true;
            Page = 0;
            Cell = 0;
            Refresh();
        }

        internal void Close()
        {
            IsOpen = false;
            selectionRevision++;
            ResetRename(null);
            // Native loading closes this UI before its deferred callback. Do not revoke an accepted handoff here.
        }

        internal void Retire()
        {
            bool cancelOwnTransition = loadingTransition == launchRevision;
            loadingTransition = null;
            retired = true;
            launchRevision++;
            Close();
            if (cancelOwnTransition) host.RejectLoad();
        }

        internal bool TurnPage(int delta)
        {
            if (!Usable || Math.Abs(delta) != 1 || Page + delta < 0 || Page + delta >= PageCount) return false;
            Page += delta;
            Refresh();
            return true;
        }

        internal void SelectCell(int cell)
        {
            if (!Usable || cell < 0 || cell >= PageSize) return;
            bool changed = Cell != cell;
            if (changed) selectionRevision++;
            Cell = cell;
            if (changed) ResetRename(archives[Cell]);
            host.Select(GlobalIndex, Cell, archives[Cell].Availability);
        }

        internal void Refresh()
        {
            if (!Usable) return;
            selectionRevision++;
            var next = new MoreSavesArchive[PageSize];
            for (int cell = 0; cell < PageSize; cell++) next[cell] = host.Read(Page * PageSize + cell);
            archives = next;
            ResetRename(archives[Cell]);
            host.Render(Page, archives);
            SelectCell(Cell);
        }

        internal void Confirm()
        {
            MoreSavesArchive? archive = Capture(allowEmpty: true);
            if (archive == null) return;
            int selection = ++selectionRevision;
            ResetRename(archive);
            bool pending = true;
            host.ConfirmLoad(archive, () =>
            {
                if (!pending) return;
                pending = false;
                if (!CurrentSelection(archive, selection)) return;
                if (!host.Matches(archive)) { host.Notice("target.changed"); return; }
                int launch = ++launchRevision;
                loadingTransition = launch;
                bool dispatched = false;
                host.HandoffLoad(() =>
                {
                    if (dispatched) return;
                    dispatched = true;
                    // A replaced request has no right to close a newer panel or loading transition.
                    if (retired || launch != launchRevision) return;
                    loadingTransition = null;
                    if (!ownsNative() || !host.Matches(archive))
                    { host.RejectLoad(); return; }
                    host.Load(archive.Availability == ArchiveAvailability.Empty, archive.Index);
                });
            });
        }

        internal void Delete()
        {
            MoreSavesArchive? archive = Capture(allowEmpty: false);
            if (archive == null) return;
            int selection = ++selectionRevision;
            ResetRename(archive);
            bool pending = true;
            host.ConfirmDelete(archive.Index, () =>
            {
                if (!pending) return;
                pending = false;
                if (!CurrentSelection(archive, selection)) return;
                if (!host.Matches(archive)) { host.Notice("target.changed"); return; }
                MoreSavesOperationResult result = host.Delete(archive, () => CurrentSelection(archive, selection));
                if (!CurrentSelection(archive, selection)) return;
                if (result.Status == MoreSavesOperationStatus.Success) Refresh();
                else host.NoticeOperation("delete", result);
            });
        }

        internal void Duplicate()
        {
            MoreSavesArchive? archive = Capture(allowEmpty: false);
            if (archive == null) return;
            int selection = ++selectionRevision;
            ResetRename(archive);
            MoreSavesOperationResult result = host.Duplicate(archive, () => CurrentSelection(archive, selection));
            if (!CurrentSelection(archive, selection)) return;
            if (result.Status != MoreSavesOperationStatus.Success) { host.NoticeOperation("copy", result); return; }
            int target = result.Index;
            if (target < 0 || target >= PageSize * PageCount) throw new InvalidOperationException("Native copy returned an invalid slot.");
            Page = target / PageSize;
            Cell = target % PageSize;
            Refresh();
        }

        private void ResetRename(MoreSavesArchive? archive)
        {
            DraftName = string.Empty;
            renameTarget = archive;
            renameRevision = selectionRevision;
            host.ResetRenameInput(archive);
        }

        internal void EditName(string text)
        {
            if (renameTarget != null && renameTarget.Availability == ArchiveAvailability.Ready && CurrentSelection(renameTarget, renameRevision))
                DraftName = text;
        }

        internal void Rename()
        {
            MoreSavesArchive? archive = renameTarget;
            int revision = renameRevision;
            if (archive == null || archive.Availability != ArchiveAvailability.Ready || !CurrentSelection(archive, revision)) return;
            if (!host.Matches(archive)) { host.Notice("target.changed"); return; }
            string requested = DraftName;
            MoreSavesOperationResult result = host.Rename(archive, requested, () => CurrentSelection(archive, revision));
            if (!CurrentSelection(archive, revision)) return;
            if (result.Status != MoreSavesOperationStatus.Success) { host.NoticeOperation("rename", result); return; }
            MoreSavesArchive? verified = result.VerifiedArchive;
            if (verified == null || verified.Availability != ArchiveAvailability.Ready || verified.Index != archive.Index || !host.Matches(verified))
            {
                host.NoticeOperation("rename", new MoreSavesOperationResult(MoreSavesOperationStatus.CommittedUnverified, archive.Index));
                return;
            }
            // Reuse the exact summary whose raw name and committed identity storage verified; no second fallible summary read.
            archives[Cell] = verified;
            host.RenderRenamed(Cell, archives[Cell]);
            selectionRevision++;
            ResetRename(archives[Cell]);
            host.Notice("rename.success");
        }

        private bool Usable => IsOpen && !retired && ownsNative();
        private bool CurrentSelection(MoreSavesArchive archive, int revision) =>
            Usable && revision == selectionRevision && GlobalIndex == archive.Index;

        private MoreSavesArchive? Capture(bool allowEmpty)
        {
            if (!Usable) return null;
            MoreSavesArchive archive = archives[Cell];
            if (archive.Availability == ArchiveAvailability.Unavailable || !host.Matches(archive))
            { host.Notice("archive.unavailable"); return null; }
            if (!allowEmpty && archive.Availability == ArchiveAvailability.Empty)
            { host.Notice("archive.empty"); return null; }
            return archive;
        }
    }
}
