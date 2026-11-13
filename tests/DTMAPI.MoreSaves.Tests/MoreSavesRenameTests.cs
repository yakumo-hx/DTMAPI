using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using DTMAPI.MoreSaves;

namespace DTMAPI.UnitTests
{
    internal static class MoreSavesRenameTests
    {
        private static int assertions;
        internal static void RunAll()
        {
            foreach (string input in new[] { "", " \t\r\n", new string('字', 9) })
                Check(!MoreSavesArchiveRename.TryName(input, 8, "默认", out _), "blank and overlong names rejected");
            Check(MoreSavesArchiveRename.TryName("默认", 8, "默认", out string stored) && stored == "", "native default name stores an empty string");
            Check(MoreSavesArchiveRename.TryName("新名字", 8, "默认", out stored) && stored == "新名字", "ordinary name retained");
            using (var f = new Fixture())
            {
                string original = File.ReadAllText(f.PathFor(12)); string identity = f.Read(12).Identity;
                for (int i = 0; i < 3; i++) File.WriteAllText(f.PrevPathFor(12, i), "old-" + i);
                File.WriteAllText(f.BackupPathFor(12), "delete-backup"); File.WriteAllText(f.PathFor(11), "other-slot");
                File.WriteAllText(Path.Combine(f.Root, "sidecar"), "owned elsewhere");
                var result = f.Rename.Rename(f.Read(12), "改名", () => true);
                Check(result.Status == MoreSavesOperationStatus.Success && f.Files.Replaces == 1, "one actual atomic replacement succeeds");
                Check(f.DecodeCalls == 2, "only source and final encoded rename are decoded for semantic preservation");
                MoreSavesArchiveDocument.AssertRename(original, File.ReadAllText(f.PathFor(12)), "改名"); assertions++;
                Check(f.Read(12).Identity != identity && File.ReadAllText(f.PrevPathFor(12, 0)) == original, "previous0 is exact pre-rename current");
                Check(File.ReadAllText(f.PrevPathFor(12, 1)) == "old-0" && File.ReadAllText(f.PrevPathFor(12, 2)) == "old-1", "native previous rotation retains exact predecessors");
                Check(File.ReadAllText(f.BackupPathFor(12)) == "delete-backup" && File.ReadAllText(f.PathFor(11)) == "other-slot" && File.ReadAllText(Path.Combine(f.Root, "sidecar")) == "owned elsewhere", "bak, other slot and sidecar untouched");
                Check(!File.Exists(f.TempPathFor(12)), "committed native temporary consumed");
                result = f.Rename.Rename(f.Read(12), "默认", () => true);
                Check(result.Status == MoreSavesOperationStatus.Success && f.SummaryName(f.Read(12)) == "", "default name commits native raw representation");
            }
            using (var f = new Fixture())
            {
                string original = File.ReadAllText(f.PathFor(12));
                f.Files.ExclusiveSource = true; f.Files.LockPrevious = true;
                var result = f.Rename.Rename(f.Read(12), "新名", () => true);
                Check(result.Status == MoreSavesOperationStatus.Success, "rename neither reopens its held source nor hashes its backup");
                f.Files.PreviousLock!.Dispose(); f.Files.PreviousLock = null;
                Check(File.ReadAllText(f.PrevPathFor(12, 0)) == original, "atomic replacement still preserves exact previous content");
                MoreSavesArchiveDocument.AssertRename(original, File.ReadAllText(f.PathFor(12)), "新名"); assertions++;
            }
            using (var f = new Fixture())
            {
                // The game's writer prints this float with 17 digits; the value is the same.
                f.SetJson(Fixture.Json.Replace("0.125", "10.4999981"));
                f.AfterEncode = json => json.Replace("10.4999981", "10.499998099999999");
                var result = f.Rename.Rename(f.Read(12), "新名", () => true);
                Check(result.Status == MoreSavesOperationStatus.Success && f.Files.Replaces == 1, "same number printed with more digits is accepted");
            }
            foreach (string mutation in new[] { "missing", "wrong-type", "number", "unknown", "mes" })
            using (var f = new Fixture())
            {
                if (mutation == "missing") f.SetJson(Fixture.Json.Replace("\"customPlayerName\":\"原名\",", ""));
                if (mutation == "wrong-type") f.SetJson(Fixture.Json.Replace("\"customPlayerName\":\"原名\"", "\"customPlayerName\":12"));
                if (mutation == "number") f.AfterEncode = json => json.Replace("0.125", "0.126");
                if (mutation == "unknown") f.AfterEncode = json => json.Replace("future value", "lost");
                if (mutation == "mes") f.AfterEncode = json => json.Replace("opaque MES", "lost");
                string before = f.Read(12).Identity;
                var result = f.Rename.Rename(f.Read(12), "新名", () => true);
                Check(result.Status == MoreSavesOperationStatus.Failed && f.Read(12).Identity == before && f.Files.Replaces == 0, "loss or unsupported field refuses commit: " + mutation);
            }
            foreach (string failure in new[] { "write", "replace", "post-read", "post-throw", "post-throw-same-stamp", "owner", "selected", "existing-temp", "stale-file", "active-game", "summary" })
            using (var f = new Fixture())
            {
                var source = f.Read(12); string original = File.ReadAllText(f.PathFor(12)); bool current = true;
                if (failure == "write") f.Files.FailWrite = true;
                if (failure == "replace") f.Files.FailReplace = true;
                if (failure == "post-read") f.Files.FailPostRead = true;
                if (failure == "post-throw") f.Files.ThrowAfterReplace = true;
                if (failure == "post-throw-same-stamp") { f.Files.ThrowAfterReplace = true; f.Files.PreserveStamp = true; }
                if (failure == "owner") f.BeforeEncode = () => f.Owner = false;
                if (failure == "selected") f.BeforeEncode = () => current = false;
                if (failure == "existing-temp") File.WriteAllText(f.TempPathFor(12), "not ours");
                if (failure == "stale-file") File.AppendAllText(f.PathFor(12), " ");
                if (failure == "active-game") f.AtTitle = false;
                if (failure == "summary") f.WrongSummary = true;
                var result = f.Rename.Rename(source, "新名", () => current);
                bool committed = failure == "post-read" || failure == "post-throw" || failure == "post-throw-same-stamp" || failure == "summary";
                if (failure == "post-throw-same-stamp") Check(f.Read(12).Identity == source.Identity, "consumed temporary identifies an uncertain replacement even when current attributes did not change");
                Check(result.Status == (committed ? MoreSavesOperationStatus.CommittedUnverified : failure == "owner" || failure == "selected" || failure == "stale-file" || failure == "active-game" ? MoreSavesOperationStatus.Changed : MoreSavesOperationStatus.Failed), "failure classification: " + failure);
                Check(File.ReadAllText(f.PathFor(12)) == (committed ? JsonConvert.SerializeObject(MoreSavesArchiveDocument.WithName(original, "新名")) : original + (failure == "stale-file" ? " " : "")), "exact current retained according to real commit: " + failure);
                Check(f.Files.Replaces <= 1, "no retry: " + failure);
                if (failure == "existing-temp") Check(File.ReadAllText(f.TempPathFor(12)) == "not ours", "foreign native temporary retained");
            }
            var h = new PageHost(); h.Session.Open(); h.Session.EditName("未提交");
            Check(h.Calls == 0 && h.Session.DraftName == "未提交", "typing has no commit");
            h.Session.SelectCell(1); Check(h.Session.DraftName == "" && h.Calls == 0, "selection discards unsubmitted draft");
            h.Session.EditName("未提交"); h.Session.TurnPage(1); Check(h.Session.DraftName == "" && h.Calls == 0, "page change discards draft");
            h.Session.EditName("旧稿"); h.Session.Close(); h.Session.Rename(); Check(h.Calls == 0 && h.Session.DraftName == "", "closed UI cannot commit a late action");
            h.Session.Open(); h.Session.EditName("新名"); h.Stale = true; h.Session.Rename(); Check(h.Calls == 0, "file replaced since selection rejects rename");
            h.Stale = false; h.Session.Rename(); Check(h.Calls == 1 && h.LastIndex == 0 && h.LastName == "新名" && h.Session.DraftName == "", "explicit action binds selected target and refreshes one card");
            h.Session.EditName("旧稿"); h.Owner = false; h.Session.Rename(); Check(h.Calls == 1, "withdrawn Owner refuses action");
            h = new PageHost(); h.Availability = ArchiveAvailability.Empty; h.Session.Open(); h.Session.EditName("新名"); h.Session.Rename(); Check(h.Calls == 0, "empty slot has no rename");
            h = new PageHost(); h.Availability = ArchiveAvailability.Unavailable; h.Session.Open(); h.Session.EditName("新名"); h.Session.Rename(); Check(h.Calls == 0, "unavailable summary has no rename");
            h = new PageHost(); h.Session.Open(); h.FailReadAfterRename = true; h.Session.EditName("准确摘要"); h.Session.Rename();
            Check(h.Reads == 12 && h.Rendered == 1 && h.LastNotice == "rename.success", "post-commit page reuses verified summary even if another summary read would be unavailable");
            h = new PageHost(); h.Session.Open(); h.ReplaceAfterRename = true; h.Session.EditName("新名"); h.Session.Rename();
            Check(h.Rendered == 0 && h.LastResult == MoreSavesOperationStatus.CommittedUnverified && h.LastNotice != "rename.success", "replaced target after storage success retains card and reports committed unconfirmed");
            Console.WriteLine("MoreSaves rename: " + assertions + " assertions passed (real files; fixture native codec/rotation delegates).");
        }
        private static void Check(bool value, string message) { assertions++; if (!value) throw new Exception(message); }
        private sealed class Files : MoreSavesRenameFiles
        {
            internal bool FailWrite, FailReplace, FailPostRead, ThrowAfterReplace, PreserveStamp, ExclusiveSource, LockPrevious;
            internal Stream? PreviousLock;
            internal int Replaces;
            internal string? target;
            internal override Stream OpenSource(string path) => ExclusiveSource ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None) : base.OpenSource(path);
            internal override void WriteTemporary(string path, string text, Action acquired) { if (FailWrite) throw new IOException("write fixture"); base.WriteTemporary(path, text, acquired); }
            internal override void ReplaceCurrent(string temp, string current, string previous)
            {
                Replaces++; if (FailReplace) throw new IOException("replace fixture");
                DateTime time = File.GetLastWriteTimeUtc(current);
                base.ReplaceCurrent(temp, current, previous); target = current;
                if (PreserveStamp) File.SetLastWriteTimeUtc(current, time);
                if (LockPrevious) PreviousLock = new FileStream(previous, FileMode.Open, FileAccess.Read, FileShare.None);
                if (ThrowAfterReplace) throw new IOException("already replaced fixture");
            }
        }
        private sealed class Fixture : IMoreSavesRenameHost, IDisposable
        {
            internal const string Json = "{\"archiveIndex\":12,\"baseData\":{\"customPlayerName\":\"原名\",\"archiveIndex\":12,\"time\":\"2026-10-01T12:00:00+08:00\"},\"farmData\":{\"agentData\":{\"customPlayerName\":\"原名\",\"money\":300}},\"unknown\":{\"value\":0.125,\"String\":\"future value\",\"array\":[true,null,999]},\"MES\":\"opaque MES\"}";
            internal readonly string Root = Path.Combine(Path.GetTempPath(), "dtmapi-s22-" + Guid.NewGuid().ToString("N"));
            internal readonly Files Files = new Files();
            internal readonly MoreSavesArchiveRename Rename;
            internal bool Owner = true, AtTitle = true, WrongSummary;
            internal int DecodeCalls;
            internal Action? BeforeEncode;
            internal Func<string, string>? AfterEncode;
            internal Fixture() { Directory.CreateDirectory(Root); SetJson(Json); Rename = new MoreSavesArchiveRename(this, Files); }
            internal void SetJson(string json) => File.WriteAllText(PathFor(12), json);
            public bool OwnsStorage => Owner;
            public bool CanRename => AtTitle;
            public int NameLimit => 8;
            public string DefaultName => "默认";
            public int BackupCount => 3;
            public string PathFor(int index) => Path.Combine(Root, "doloc-save-" + index + ".data");
            public string TempPathFor(int index) => Path.Combine(Root, "doloc-save-" + index + "-tmp.data");
            public string PrevPathFor(int index, int previous) => PathFor(index) + ".prev" + previous;
            public string BackupPathFor(int index) => PathFor(index) + ".bak";
            public string Decode(string text) { DecodeCalls++; return text; }
            public string EncodeDocument(object document) { BeforeEncode?.Invoke(); string json = JsonConvert.SerializeObject(document); return AfterEncode?.Invoke(json) ?? json; }
            public MoreSavesArchive Read(int index)
            {
                if (Files.FailPostRead && Files.target == PathFor(index)) throw new IOException("fixture summary read after rename failed");
                return new MoreSavesArchive(index, ArchiveAvailability.Ready, MoreSavesFileIdentity.ReadStamp(PathFor(index), out _));
            }
            public string SummaryName(MoreSavesArchive archive) => WrongSummary ? "wrong" : (string)JObject.Parse(File.ReadAllText(PathFor(archive.Index)))["baseData"]!["customPlayerName"]!;
            public void ScrollBackups(int index) { File.Delete(PrevPathFor(index, BackupCount - 1)); for (int i = BackupCount - 2; i >= 0; i--) if (File.Exists(PrevPathFor(index, i))) File.Move(PrevPathFor(index, i), PrevPathFor(index, i + 1)); }
            public string CopyWithNativeIndex(string text, int target) => throw new NotSupportedException();
            public bool DeleteNative(int index) => throw new NotSupportedException();
            public void Dispose() { Files.PreviousLock?.Dispose(); Directory.Delete(Root, true); }
        }
        private sealed class PageHost : IMoreSavesPageHost
        {
            internal readonly MoreSavesPageSession Session;
            internal bool Owner = true, Stale, FailReadAfterRename, ReplaceAfterRename;
            internal int Calls, LastIndex, Reads, Rendered;
            internal string LastNotice = "";
            internal MoreSavesOperationStatus LastResult;
            internal string LastName = "";
            internal ArchiveAvailability Availability = ArchiveAvailability.Ready;
            internal PageHost() { Session = new MoreSavesPageSession(this, () => Owner); }
            public MoreSavesArchive Read(int i) { Reads++; return new MoreSavesArchive(i, FailReadAfterRename && Calls > 0 ? ArchiveAvailability.Unavailable : Availability, "identity"); }
            public bool Matches(MoreSavesArchive a) => !Stale;
            public void Render(int p, MoreSavesArchive[] a) { }
            public void Select(int i, int c, ArchiveAvailability a) { }
            public void ResetRenameInput(MoreSavesArchive? a) { }
            public void RenderRenamed(int c, MoreSavesArchive a) { Rendered++; }
            public MoreSavesOperationResult Rename(MoreSavesArchive a, string name, Func<bool> current) { Check(current(), "fixed UI request remains current"); Calls++; LastIndex = a.Index; LastName = name; if(ReplaceAfterRename) Stale = true; return new MoreSavesOperationResult(MoreSavesOperationStatus.Success, a.Index, verifiedArchive: new MoreSavesArchive(a.Index, ArchiveAvailability.Ready, "identity")); }
            public MoreSavesOperationResult Delete(MoreSavesArchive a, Func<bool> c) => throw new NotSupportedException();
            public MoreSavesOperationResult Duplicate(MoreSavesArchive a, Func<bool> c) => throw new NotSupportedException();
            public void ConfirmLoad(MoreSavesArchive a, Action c) { }
            public void ConfirmDelete(int i, Action c) { }
            public void HandoffLoad(Action d) { }
            public void Load(bool n, int i) { }
            public void RejectLoad() { }
            public void NoticeOperation(string o, MoreSavesOperationResult r) { LastResult = r.Status; }
            public void Notice(string k) { LastNotice = k; }
        }
    }
}
