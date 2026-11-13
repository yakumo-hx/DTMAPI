using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DTMAPI.MoreSaves;

namespace DTMAPI.UnitTests
{
    internal static class MoreSavesPagingTests
    {
        private static int assertions;
        internal static void RunAll()
        {
            MappingAndDemandReads();
            ChangedSummariesAreReadAgain();
            ConfirmationInvalidation();
            AcceptedLoadLifetime();
            DispatchTargetsAndUnavailableFiles();
            RealCurrentFileIdentity();
            MoreSavesStorageTests.RunAll();
            Console.WriteLine("MoreSaves paging: " + assertions + " assertions passed.");
        }

        private static void ChangedSummariesAreReadAgain()
        {
            var f = new Host();
            f.Unavailable.Add(1);
            f.Empty.Add(2);
            f.Session.Open();
            f.Session.TurnPage(1);
            int reads = f.Reads;
            f.Session.TurnPage(-1);
            Check(f.Reads == reads + 1, "returning to a page reuses unchanged summaries and retries unreadable ones");
            f.Unavailable.Remove(1); f.Revisions[1]++;
            f.Empty.Remove(2); f.Revisions[2]++;
            f.Revisions[3]++;
            reads = f.Reads;
            f.Session.Refresh();
            Check(f.Reads == reads + 3, "recovered, newly occupied and changed files are read again");
            f.Empty.Add(3); f.Revisions[3]++;
            reads = f.Reads;
            f.Session.Close(); f.Session.Open();
            Check(f.Reads == reads + 1, "reopening rechecks file identity and refreshes a deleted slot");
            f.Session.SelectCell(3); f.Session.Confirm(); f.Confirmation!(); f.Deferred!();
            Check(f.LastNew && f.Loads.Count == 1, "a deleted cached file becomes an empty new-game slot");
        }

        private static void MappingAndDemandReads()
        {
            var f = new Host();
            f.Session.Open();
            Check(f.Target == 0 && f.Labels.SequenceEqual(Enumerable.Range(1,12)), "first page native targets and labels");
            Check(f.Reads == 12 && !f.Session.TurnPage(-1), "open reads one page; first boundary");
            f.Session.SelectCell(11);
            Check(f.Target == 11 && f.LocalCell == 11, "#12 via product selection");
            f.Session.TurnPage(1);
            Check(f.Target == 23 && f.LocalCell == 11, "page turn retains cell and changes global target");
            f.Session.SelectCell(0);
            Check(f.Target == 12 && f.Labels.First() == 13, "#13 via product mapping");
            f.Session.TurnPage(1); f.Session.TurnPage(1); f.Session.TurnPage(1); f.Session.SelectCell(11);
            Check(f.Target == 59 && f.Labels.Last() == 60 && !f.Session.TurnPage(1), "#60 and last boundary");
            int reads = f.Reads;
            f.Session.SelectCell(0); f.Session.SelectCell(11);
            Check(f.Reads == reads, "selection does not enumerate files");
            f.Session.TurnPage(-1);
            Check(f.Target == 47 && f.LocalCell == 11, "backward page boundary");
            f.Session.Close(); f.Session.Open();
            Check(f.Target == 0, "new UI opening resets explicit selection");
        }

        private static void ConfirmationInvalidation()
        {
            foreach (string change in new[] { "page", "slot", "close", "file", "owner", "reopen" })
            {
                var f = new Host(); f.Session.Open(); f.Session.TurnPage(1);
                f.Session.Confirm(); Action old = f.Confirmation!;
                Change(f, change); old(); old();
                Check(f.Handoffs == 0 && f.Loads.Count == 0, "pending load invalidated by " + change);
                f = new Host(); f.Session.Open(); f.Session.TurnPage(1); f.Session.Delete(); old = f.Confirmation!;
                Change(f, change); old(); old();
                Check(f.Deleted.Count == 0, "pending delete invalidated by " + change);
            }
            var newest = new Host(); newest.Session.Open(); newest.Session.Confirm(); Action first = newest.Confirmation!;
            newest.Session.Confirm(); first();
            Check(newest.Handoffs == 0, "second confirmation replaces first request");
        }

        private static void Change(Host f, string change)
        {
            switch (change)
            {
                case "page": f.Session.TurnPage(1); break;
                case "slot": f.Session.SelectCell(1); break;
                case "close": f.Session.Close(); break;
                case "file": f.Revisions[f.Target]++; break;
                case "owner": f.Owner = false; f.Session.Retire(); break;
                case "reopen": f.Session.Close(); f.Session.Open(); break;
            }
        }

        private static void AcceptedLoadLifetime()
        {
            foreach (int index in new[] { 0, 11, 12, 59 })
            {
                var f = new Host(); f.Session.Open();
                for (int page = 0; page < index / 12; page++) f.Session.TurnPage(1);
                f.Session.SelectCell(index % 12); f.Session.Confirm();
                f.Confirmation!(); f.Confirmation!();
                Check(!f.Session.IsOpen && f.Handoffs == 1, "accepted native transition closes UI once #" + (index+1));
                f.Deferred!(); f.Deferred!();
                Check(f.Loads.Count == 1 && f.Loads[0] == index && !f.LastNew, "fixed load survives normal close #" + (index+1));
            }
            foreach (string change in new[] { "file", "owner", "reopen" })
            {
                var f = new Host(); f.Session.Open(); f.Session.TurnPage(1); f.Session.Confirm(); f.Confirmation!();
                Action old = f.Deferred!; Change(f, change); old();
                Check(f.Loads.Count == 0 && f.Rejected == (change == "reopen" ? 0 : 1), "accepted load rejected without stale UI effects after " + change);
            }
            var newer = new Host(); newer.Session.Open(); newer.Session.Confirm(); newer.Confirmation!();
            Action oldLoad = newer.Deferred!;
            newer.Session.Open(); newer.Session.TurnPage(1); newer.Session.Confirm(); newer.Confirmation!();
            Action newLoad = newer.Deferred!;
            oldLoad();
            Check(newer.Rejected == 0 && newer.Loads.Count == 0, "obsolete A cannot close B's loading transition or reopen title");
            newLoad(); newLoad();
            Check(newer.Loads.SequenceEqual(new[]{12}), "B still loads its fixed target exactly once");
        }

        private static void DispatchTargetsAndUnavailableFiles()
        {
            var f = new Host(); f.Session.Open();
            for (int i=0;i<4;i++) f.Session.TurnPage(1);
            f.Session.SelectCell(11); f.Session.Delete();
            Check(f.DeleteQuestionTarget == 59, "delete question fixed to #60");
            f.Confirmation!(); f.Confirmation!();
            Check(f.Deleted.SequenceEqual(new[] {59}), "delete dispatch exactly once to global index");
            f.CopyTarget = 12; f.Session.Duplicate();
            Check(f.CopySource == 59 && f.Target == 12 && f.Session.Page == 1, "copy uses global source and selects returned page");
            f.Empty.Add(12); f.Revisions[12]++; f.Session.Refresh(); f.Session.Confirm(); f.Confirmation!(); f.Deferred!();
            Check(f.LastNew && f.Loads.Last() == 12, "empty #13 new-game dispatch binding");
            f = new Host(); f.Unavailable.Add(12); f.Session.Open(); f.Session.TurnPage(1); f.Session.Confirm();
            Check(f.Confirmation == null && f.Loads.Count == 0, "existing unreadable summary cannot become new game");
            f.Session.TurnPage(-1); f.CopyStatus = MoreSavesOperationStatus.Unavailable; f.Session.Duplicate();
            Check(f.LastOperation?.Status == MoreSavesOperationStatus.Unavailable && f.Target == 0, "storage unavailable result retains original page and reports failure");
            int reads = f.Reads; f.DeleteStatus = MoreSavesOperationStatus.Failed; f.Session.Delete(); f.Confirmation!();
            Check(f.Reads == reads && f.Target == 0 && f.LastOperation?.Status == MoreSavesOperationStatus.Failed, "failed delete does not refresh its card to empty");
            f = new Host(); f.Empty.Add(59); f.Session.Open(); for(int i=0;i<4;i++)f.Session.TurnPage(1);
            f.Session.SelectCell(11); f.Session.Confirm(); f.Revisions[59]++; f.Confirmation!();
            Check(f.Handoffs == 0, "new target appearing before confirmation cannot be overwritten");
        }

        private static void RealCurrentFileIdentity()
        {
            string root = Path.Combine(Path.GetTempPath(), "dtmapi-s20-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            string path = Path.Combine(root,"doloc-save-12.data");
            try
            {
                string absent = MoreSavesFileIdentity.ReadStamp(path, out bool exists); Check(!exists, "missing native current");
                File.WriteAllText(path,"abc"); DateTime stamp = File.GetLastWriteTimeUtc(path);
                string original = MoreSavesFileIdentity.ReadStamp(path, out exists); Check(exists && original != absent, "appearance changes identity");
                string displayStamp = MoreSavesFileIdentity.ReadStamp(path, out _);
                using (var locked = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                    Check(MoreSavesFileIdentity.ReadStamp(path, out _) == displayStamp, "display observation never opens archive content");
                File.WriteAllText(path,"xyz"); File.SetLastWriteTimeUtc(path,stamp);
                Check(File.ReadAllText(path) == "xyz", "fixture changed content without changing attributes");
                Check(MoreSavesFileIdentity.ReadStamp(path, out _) == displayStamp, "display stamp is deliberately not a content proof");
                File.Delete(path); Directory.CreateDirectory(path);
                bool rejected = false; try { MoreSavesFileIdentity.ReadStamp(path,out _); } catch(IOException) { rejected=true; }
                Check(rejected,"directory is unavailable, not empty");
            }
            finally { Directory.Delete(root,true); }
        }

        private static void Check(bool condition, string message)
        { assertions++; if(!condition) throw new InvalidOperationException(message); }

        private sealed class Host : IMoreSavesPageHost
        {
            internal readonly MoreSavesPageSession Session;
            internal bool Owner = true;
            internal readonly int[] Revisions = new int[60];
            internal readonly HashSet<int> Empty = new HashSet<int>();
            internal readonly HashSet<int> Unavailable = new HashSet<int>();
            internal int Target, LocalCell, Reads, Handoffs, Rejected, CopyTarget, DeleteQuestionTarget;
            internal int CopySource = -1;
            internal int[] Labels = Array.Empty<int>();
            internal readonly List<int> Loads = new List<int>();
            internal readonly List<int> Deleted = new List<int>();
            internal bool LastNew;
            internal MoreSavesOperationStatus CopyStatus = MoreSavesOperationStatus.Success, DeleteStatus = MoreSavesOperationStatus.Success;
            internal MoreSavesOperationResult? LastOperation;
            internal Action? Confirmation, Deferred;
            internal Host() { Session = new MoreSavesPageSession(this, () => Owner); }
            public MoreSavesArchive Read(int index) { Reads++; return new MoreSavesArchive(index, Unavailable.Contains(index) ? ArchiveAvailability.Unavailable : Empty.Contains(index) ? ArchiveAvailability.Empty : ArchiveAvailability.Ready, index+":"+Revisions[index]); }
            public bool Matches(MoreSavesArchive a) => a.Identity == a.Index+":"+Revisions[a.Index];
            public void Render(int page, MoreSavesArchive[] archives) { Labels = archives.Select(a=>a.Index+1).ToArray(); }
            public void Select(int globalIndex,int cell,ArchiveAvailability availability) { Target=globalIndex; LocalCell=cell; }
            public void ConfirmLoad(MoreSavesArchive archive,Action confirmed) { Confirmation=confirmed; }
            public void HandoffLoad(Action dispatch) { Handoffs++; Session.Close(); Deferred=dispatch; }
            public void Load(bool isNew,int globalIndex) { LastNew=isNew; Loads.Add(globalIndex); }
            public void RejectLoad() { Rejected++; }
            public void ConfirmDelete(int globalIndex,Action confirmed) { DeleteQuestionTarget=globalIndex; Confirmation=confirmed; }
            public MoreSavesOperationResult Delete(MoreSavesArchive archive,Func<bool> current) { if(current()) Deleted.Add(archive.Index); return new MoreSavesOperationResult(DeleteStatus,archive.Index); }
            public MoreSavesOperationResult Duplicate(MoreSavesArchive archive,Func<bool> current) { if(current()) CopySource=archive.Index; return new MoreSavesOperationResult(CopyStatus,CopyTarget); }
            public MoreSavesOperationResult Rename(MoreSavesArchive archive,string name,Func<bool> current) => new MoreSavesOperationResult(MoreSavesOperationStatus.Failed,archive.Index);
            public void ResetRenameInput(MoreSavesArchive? archive) { }
            public void RenderRenamed(int cell,MoreSavesArchive archive) { }
            public void NoticeOperation(string operation,MoreSavesOperationResult result) { LastOperation=result; }
            public void Notice(string key) { }
        }
    }
}
