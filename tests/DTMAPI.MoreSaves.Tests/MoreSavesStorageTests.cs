using System;
using System.IO;
using DTMAPI.MoreSaves;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DTMAPI.UnitTests
{
    internal static class MoreSavesStorageTests
    {
        private static int assertions;
        internal static void RunAll()
        {
            SummaryReads();
            AllocationAndPreservation();
            CopyFailures();
            DeletePostconditions();
            CodecLossChecks();
            Console.WriteLine("MoreSaves storage: " + assertions + " assertions passed (real disposable files; fixture codec/native-delete delegates).");
        }

        private static void SummaryReads()
        {
            foreach (string json in new[] {
                "{\"baseData\":{\"customPlayerName\":\"测试\",\"archiveIndex\":12,\"version\":\"1.0.0\"},\"farmData\":{\"items\":[1,2,3]}}",
                "{\"farmData\":{\"baseData\":{\"customPlayerName\":\"nested\"}},\"baseData\":{\"customPlayerName\":\"actual\"}}",
                "{\"baseData\":{\"customPlayerName\":\"first\"},\"baseData\":{\"customPlayerName\":\"last\"}}",
                "/*before*/ {\"baseData\":/*value*/{\"realTimeStamp\":\"2026-11-08T10:20:30Z\",\"extra\":1.5}} /*after*/",
                "{\"baseData\":null}", "{\"farmData\":[]}" })
            {
                JToken? expected = JObject.Parse(json)["baseData"];
                JObject? actual = MoreSavesArchiveDocument.ReadSummary<JObject>(json);
                Check(JToken.DeepEquals(expected?.Type == JTokenType.Null ? null : expected, actual),
                    "summary retains native values, null handling and last duplicate property");
            }
            foreach (string json in new[] { "[]", "null", "{\"baseData\":{},\"farmData\":[1,}",
                "{\"baseData\":{},\"farmData\":{", "{\"baseData\":{}} {}", "{\"baseData\":{}" })
            {
                bool rejected = false;
                try { MoreSavesArchiveDocument.ReadSummary<JObject>(json); }
                catch (JsonException) { rejected = true; }
                catch (InvalidDataException) { rejected = true; }
                Check(rejected, "a summary never hides corrupt world data or trailing content");
            }
        }

        private static void AllocationAndPreservation()
        {
            foreach (int occupied in new[] { 1, 12, 59, 60 })
            using (var f = new Fixture())
            {
                f.Fill(occupied);
                int source = occupied - 1;
                string before = f.Content(source);
                var result = f.Storage.Duplicate(f.Read(source), () => true);
                Check(result.Status == (occupied == 60 ? MoreSavesOperationStatus.Full : MoreSavesOperationStatus.Success), "allocation result for " + occupied);
                Check(f.Content(source) == before, "copy never writes its source");
                if (occupied == 60) { Check(f.Files.Moves == 0, "full differs from I/O and performs no commit"); continue; }
                Check(result.Index == occupied, "lowest actual empty slot: #" + (occupied + 1));
                Check(f.DecodeCalls == 2, "only the source and final encoded copy are decoded for semantic preservation");
                var a = JObject.Parse(File.ReadAllText(f.PathFor(source)));
                var b = JObject.Parse(File.ReadAllText(f.PathFor(result.Index)));
                a["archiveIndex"] = result.Index; a["baseData"]!["archiveIndex"] = result.Index;
                Check(JToken.DeepEquals(a,b), "only two indices differ; unknown arrays/values and opaque MES String preserved");
                Check(!File.Exists(f.BackupPathFor(result.Index)) && Directory.GetFiles(f.Root).Length == occupied + 1, "copy does not duplicate any file family or sidecar");
            }
            using (var f = new Fixture())
            {
                f.Fill(5); File.Delete(f.PathFor(1));
                var result = f.Storage.Duplicate(f.Read(4), () => true);
                Check(result.Status == MoreSavesOperationStatus.Success && result.Index == 1, "earlier hole wins over append");
            }
            using (var f = new Fixture())
            {
                f.Fill(1); f.Files.ExclusiveSource = true;
                var result = f.Storage.Duplicate(f.Read(0), () => true);
                Check(result.Status == MoreSavesOperationStatus.Success, "copy uses the held source stream without reopening it for hashing");
                MoreSavesArchiveDocument.AssertCopy(f.Content(0), f.Content(1), 0, 1); assertions++;
            }
        }

        private static void CopyFailures()
        {
            using (var f = new Fixture())
            {
                f.Fill(1); var source = f.Read(0);
                DateTime stamp = File.GetLastWriteTimeUtc(f.PathFor(0));
                f.OnRead = index =>
                {
                    if (index != 1) return;
                    f.OnRead = null;
                    File.WriteAllText(f.PathFor(0), File.ReadAllText(f.PathFor(0)).Replace("7654321", "7654322"));
                    File.SetLastWriteTimeUtc(f.PathFor(0), stamp);
                };
                var result = f.Storage.Duplicate(source, () => true);
                Check(f.Read(0).Identity == source.Identity && result.Status == MoreSavesOperationStatus.Success && f.Files.Moves == 1,
                    "Same-stamp external mutation is not a promised content-identity check.");
                MoreSavesArchiveDocument.AssertCopy(f.Content(0), f.Content(1), 0, 1); assertions++;
            }
            using (var f = new Fixture())
            {
                f.Fill(1); File.WriteAllText(f.PathFor(59), "unreadable");
                var result = f.Storage.Duplicate(f.Read(0), () => true);
                Check(result.Status == MoreSavesOperationStatus.Success && result.Index == 1 && f.Files.Moves == 1, "a bad later slot cannot block the first proven empty slot");
                Check(f.Content(59) == "unreadable", "later bad slot is preserved");
            }
            using (var f = new Fixture())
            {
                f.Fill(1); File.WriteAllText(f.PathFor(1), "unreadable");
                var result = f.Storage.Duplicate(f.Read(0), () => true);
                Check(result.Status == MoreSavesOperationStatus.Unavailable && result.Index == 1 && f.Files.Moves == 0, "a bad slot before a proven hole cannot be skipped or overwritten");
            }
            using (var f = new Fixture())
            {
                f.Fill(1); var source = f.Read(0);
                f.OnRead = index => { if(index == 1) File.AppendAllText(f.PathFor(0), " "); };
                var result = f.Storage.Duplicate(source, () => true);
                Check(result.Status == MoreSavesOperationStatus.Changed && !File.Exists(f.PathFor(1)), "changed source during scan invalidates fixed request");
            }
            using (var f = new Fixture())
            {
                f.Fill(1); f.BeforeEncode = () => f.Owner = false;
                var result = f.Storage.Duplicate(f.Read(0), () => true);
                Check(result.Status == MoreSavesOperationStatus.Changed && f.Files.Moves == 0, "owner changed before commit cannot write");
                Check(Directory.GetFiles(f.Root).Length == 1, "uncommitted temporary cleaned");
            }
            using (var f = new Fixture())
            {
                f.Fill(1); f.Files.BeforeMove = (_, target) => File.WriteAllText(target,"competing writer");
                var result = f.Storage.Duplicate(f.Read(0), () => true);
                Check(result.Status == MoreSavesOperationStatus.Failed && File.ReadAllText(f.PathFor(1)) == "competing writer", "target appears after check: actual File.Move never overwrites it");
                Check(f.Files.Moves == 1 && Directory.GetFiles(f.Root).Length == 2, "no second target, no blind retry or leftover temporary");
            }
            using (var f = new Fixture())
            {
                f.Fill(1); f.Files.FailWrite = true;
                var result = f.Storage.Duplicate(f.Read(0), () => true);
                Check(result.Status == MoreSavesOperationStatus.Failed && f.Files.Moves == 0 && !File.Exists(f.PathFor(1)), "write failure is not full or success");
            }
            using (var f = new Fixture())
            {
                f.Fill(12); string source = f.Content(11); f.Files.FailTargetRead = true;
                var result = f.Storage.Duplicate(f.Read(11), () => true);
                Check(result.Status == MoreSavesOperationStatus.CommittedUnverified && result.Index == 12, "post-commit read failure preserves exact #13 and uncertain result");
                Check(File.Exists(f.PathFor(12)) && f.Files.Moves == 1 && f.Content(11) == source, "committed copy retained; no retry, compensation or source writeback");
            }
            using (var f = new Fixture())
            {
                f.Fill(1); string source = f.Content(0); f.Files.ThrowAfterMove = true;
                var result = f.Storage.Duplicate(f.Read(0), () => true);
                Check(result.Status == MoreSavesOperationStatus.CommittedUnverified && result.Index == 1,
                    "move consumed the temporary before throwing: report the possibly committed target");
                Check(f.Files.Moves == 1 && f.Content(0) == source && File.Exists(f.PathFor(1)) && Directory.GetFiles(f.Root).Length == 2,
                    "post-move exception never retries, removes the target or writes back the source");
            }
        }

        private static void DeletePostconditions()
        {
            using (var f = new Fixture())
            {
                f.Fill(2); File.WriteAllText(f.BackupPathFor(1), "old backup");
                File.WriteAllText(f.PathFor(1)+".prev0", "previous"); File.WriteAllText(Path.Combine(f.Root,"mes-extra.json"),"opaque sidecar");
                string content = File.ReadAllText(f.PathFor(1)), other = f.Content(0);
                var result = f.Storage.Delete(f.Read(1), () => true);
                Check(result.Status == MoreSavesOperationStatus.Success && !File.Exists(f.PathFor(1)), "native bool plus true current disappearance");
                Check(File.ReadAllText(f.BackupPathFor(1)) == content && f.Content(0) == other, "exact confirmed bytes moved to that slot backup");
                Check(File.ReadAllText(f.PathFor(1)+".prev0") == "previous" && File.ReadAllText(Path.Combine(f.Root,"mes-extra.json")) == "opaque sidecar", "prev, other slot and MES sidecar untouched");
            }
            using (var f = new Fixture())
            {
                f.Fill(1); File.WriteAllText(f.BackupPathFor(0),"old"); f.DeleteMode = "false-after-backup";
                string before = f.Content(0); var result = f.Storage.Delete(f.Read(0), () => true);
                Check(result.Status == MoreSavesOperationStatus.Failed && f.Content(0) == before && !File.Exists(f.BackupPathFor(0)), "false is failure even after native removes old backup; no backup restoration");
            }
            using (var f = new Fixture())
            {
                f.Fill(1); f.DeleteMode = "wrong-backup";
                var result = f.Storage.Delete(f.Read(0), () => true);
                Check(result.Status == MoreSavesOperationStatus.CommittedUnverified, "true plus wrong backup cannot claim deletion success");
            }
            foreach (string mode in new[] { "missing-backup", "false-after-move", "throw-after-move", "true-with-current" })
            using (var f = new Fixture())
            {
                f.Fill(1); f.DeleteMode = mode;
                var result = f.Storage.Delete(f.Read(0), () => true);
                Check(result.Status == (mode == "true-with-current" ? MoreSavesOperationStatus.Failed : MoreSavesOperationStatus.CommittedUnverified),
                    "delete reports the actual postcondition: " + mode);
                Check(f.Deletes == 1 && File.Exists(f.PathFor(0)) == (mode == "true-with-current"), "no delete replay or source restoration: " + mode);
            }
            using (var f = new Fixture())
            {
                f.Fill(1); string original = f.Content(0); f.DeleteMode = "lock-backup";
                var result = f.Storage.Delete(f.Read(0), () => true);
                Check(result.Status == MoreSavesOperationStatus.Success, "delete checks backup attributes without opening the backup content");
                f.BackupLock!.Dispose(); f.BackupLock = null;
                Check(File.ReadAllText(f.BackupPathFor(0)) == original, "native move still preserves exact backup content");
            }
            using (var f = new Fixture())
            {
                f.Fill(1); var selected = f.Read(0); File.AppendAllText(f.PathFor(0)," ");
                var result = f.Storage.Delete(selected, () => true);
                Check(result.Status == MoreSavesOperationStatus.Changed && f.Deletes == 0 && File.Exists(f.PathFor(0)), "replaced confirmed file is never deleted");
                result = f.Storage.Delete(f.Read(0), () => false);
                Check(result.Status == MoreSavesOperationStatus.Changed && f.Deletes == 0, "closed UI request cannot delete");
            }
        }

        private static void CodecLossChecks()
        {
            using (var f = new Fixture())
            {
                f.Fill(1);
                f.AfterEncode = text => text.Replace("7654321", "7654322");
                var result = f.Storage.Duplicate(f.Read(0), () => true);
                Check(result.Status == MoreSavesOperationStatus.Failed && f.Files.Moves == 0 && !File.Exists(f.PathFor(1)),
                    "A lossy final codec result is refused in memory before any destination is committed.");
            }

            foreach (var pair in new[] {
                ("0.125", "0.126"),
                ("9007199254740993", "9007199254740992"),
                ("\"2026-10-04T12:00:00+08:00\"", "\"2026-10-04T04:00:00Z\""),
                ("\"{\\\"schema\\\":999,\\\"String\\\":\\\"未知\\\"}\"", "\"\"") })
            {
                string a = "{\"archiveIndex\":0,\"baseData\":{\"archiveIndex\":0},\"unknown\":" + pair.Item1 + "}";
                string b = "{\"archiveIndex\":1,\"baseData\":{\"archiveIndex\":1},\"unknown\":" + pair.Item2 + "}";
                bool rejected=false; try { MoreSavesArchiveDocument.AssertCopy(a,b,0,1); } catch(InvalidDataException) { rejected=true; }
                Check(rejected,"non-index numeric/string value changes are rejected");
            }
            // The game's writer prints this float with 17 digits; the value is the same.
            MoreSavesArchiveDocument.AssertCopy(
                "{\"archiveIndex\":0,\"baseData\":{\"archiveIndex\":0},\"y\":10.4999981}",
                "{\"archiveIndex\":1,\"baseData\":{\"archiveIndex\":1},\"y\":10.499998099999999}", 0, 1); assertions++;
            bool missing=false;
            try { MoreSavesArchiveDocument.AssertCopy("{\"archiveIndex\":0}","{\"archiveIndex\":1}",0,1); } catch(InvalidDataException) { missing=true; }
            Check(missing,"missing index projections are refused, not repaired");
        }

        private static void Check(bool condition,string message) { assertions++; if(!condition)throw new InvalidOperationException(message); }
        private sealed class FaultFiles : MoreSavesArchiveFiles
        {
            internal int Moves;
            internal bool FailTargetRead, FailWrite, ThrowAfterMove, ExclusiveSource;
            internal string? Target;
            internal Action<string,string>? BeforeMove;
            internal override Stream OpenSource(string path) => ExclusiveSource ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None) : base.OpenSource(path);
            internal override void WriteNew(string path,string text) { if(FailWrite)throw new IOException("fixture write failed"); base.WriteNew(path,text); }
            internal override void MoveNew(string source,string target) { Moves++; BeforeMove?.Invoke(source,target); base.MoveNew(source,target); Target=target; if(ThrowAfterMove)throw new IOException("fixture already moved"); }
        }
        private sealed class Fixture : IMoreSavesStorageHost, IDisposable
        {
            internal readonly string Root=Path.Combine(Path.GetTempPath(),"dtmapi-s21-"+Guid.NewGuid().ToString("N"));
            internal readonly FaultFiles Files=new FaultFiles();
            internal readonly MoreSavesArchiveStorage Storage;
            internal bool Owner=true;
            internal Action<int>? OnRead;
            internal Action? BeforeEncode;
            internal Func<string, string>? AfterEncode;
            internal string DeleteMode="normal";
            internal Stream? BackupLock;
            internal int Deletes;
            internal int DecodeCalls;
            internal Fixture() { Directory.CreateDirectory(Root); Storage=new MoreSavesArchiveStorage(this,Files); }
            public bool OwnsStorage=>Owner;
            public string PathFor(int index)=>Path.Combine(Root,"doloc-save-"+index+".data");
            public string BackupPathFor(int index)=>PathFor(index)+".bak";
            internal string Content(int index)=>File.ReadAllText(PathFor(index));
            internal void Fill(int count)
            {
                for(int i=0;i<count;i++) File.WriteAllText(PathFor(i),JsonConvert.SerializeObject(new {
                    archiveIndex=i,baseData=new {archiveIndex=i,customPlayerName="测试"},money=7654321L,time=1.25,
                    unknown=new {values=new object[]{true,"future",0.5}}, embeddedString="{\"schema\":999,\"String\":\"不透明 MES\",\"empty\":[]}" }));
            }
            public MoreSavesArchive Read(int index)
            {
                if (Files.FailTargetRead && PathFor(index) == Files.Target) throw new IOException("fixture summary read after commit failed");
                OnRead?.Invoke(index);
                string id=MoreSavesFileIdentity.ReadStamp(PathFor(index),out bool exists);
                if(!exists)return new MoreSavesArchive(index,ArchiveAvailability.Empty,id);
                try { JObject.Parse(File.ReadAllText(PathFor(index))); return new MoreSavesArchive(index,ArchiveAvailability.Ready,id); }
                catch { return new MoreSavesArchive(index,ArchiveAvailability.Unavailable,id); }
            }
            public string Decode(string encoded) { DecodeCalls++; return encoded; }
            public string CopyWithNativeIndex(string encoded,int index)
            {
                BeforeEncode?.Invoke();
                var fixtureDocument=JObject.Parse(encoded);
                fixtureDocument["archiveIndex"]=index; fixtureDocument["baseData"]!["archiveIndex"]=index;
                string result = fixtureDocument.ToString(Formatting.None);
                return AfterEncode?.Invoke(result) ?? result;
            }
            public bool DeleteNative(int index)
            {
                Deletes++;
                if(DeleteMode=="true-with-current")return true;
                if(File.Exists(BackupPathFor(index)))File.Delete(BackupPathFor(index));
                if(DeleteMode=="false-after-backup")return false;
                File.Move(PathFor(index),BackupPathFor(index));
                if(DeleteMode=="wrong-backup")File.WriteAllText(BackupPathFor(index),"different bytes");
                if(DeleteMode=="missing-backup")File.Delete(BackupPathFor(index));
                if(DeleteMode=="false-after-move")return false;
                if(DeleteMode=="throw-after-move")throw new IOException("fixture already deleted");
                if(DeleteMode=="lock-backup")BackupLock=new FileStream(BackupPathFor(index),FileMode.Open,FileAccess.Read,FileShare.None);
                return true;
            }
            public void Dispose() { BackupLock?.Dispose(); Directory.Delete(Root,true); }
        }
    }
}
