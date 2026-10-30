using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;
using DTMAPI.Core.Services;

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {
        private static void OwnerFilesAndGlobalDataIsolateOwnersAndRetainAcrossPackageWithdrawal()
        {
            string root = NewTempGameDir();
            string aRoot = Path.Combine(root, "package-a");
            string bRoot = Path.Combine(root, "package-b");
            Directory.CreateDirectory(Path.Combine(aRoot, "data"));
            Directory.CreateDirectory(Path.Combine(bRoot, "data"));
            File.WriteAllText(Path.Combine(aRoot, "data/settings.json"), "owner A");
            File.WriteAllText(Path.Combine(bRoot, "data/settings.json"), "owner B");
            string global = Path.Combine(root, "global");
            var manager = new OwnerServiceManager();
            var a = manager.Create("Owner.A", () => { }, () => { }, aRoot, global);
            var b = manager.Create("Owner.B", () => { }, () => { }, bRoot, global);
            var aFiles = a.GetService<IOwnerFileHelper>()!;
            var bFiles = b.GetService<IOwnerFileHelper>()!;
            Assert(aFiles.ReadText("data/settings.json").Value == "owner A" && bFiles.ReadText("data/settings.json").Value == "owner B", "Same relative package key must resolve inside its own owner package.");
            Assert(aFiles.ReadText("missing.json").Status == OwnerDataStatus.Missing, "Missing package file must remain distinct.");
            var aData = a.GetService<IGlobalDataHelper>()!;
            var bData = b.GetService<IGlobalDataHelper>()!;
            Assert(aData.WriteGlobal("same/key", new GlobalProbeData { Value = 11 }, 1).Status == OwnerDataStatus.Found, "Owner A write failed.");
            Assert(bData.WriteGlobal("same/key", new GlobalProbeData { Value = 22 }, 1).Status == OwnerDataStatus.Found, "Owner B write failed.");
            Assert(aData.ReadGlobal<GlobalProbeData>("same/key", 1).Value!.Value == 11 && bData.ReadGlobal<GlobalProbeData>("same/key", 1).Value!.Value == 22, "Global keys must isolate owner values.");
            manager.RemoveOwner("Owner.A", ModOwnerCleanupReason.Unload);
            AssertThrows(() => aFiles.ReadText("data/settings.json"), "Stale package reader must be rejected.");
            AssertThrows(() => aData.ReadGlobal<GlobalProbeData>("same/key", 1), "Stale global reader must be rejected.");
            File.Delete(Path.Combine(aRoot, "data/settings.json"));
            Directory.Delete(Path.Combine(aRoot, "data"));
            Directory.Delete(aRoot);
            var restarted = new GlobalDataService("OWNER.A", global, () => { });
            Assert(restarted.ReadGlobal<GlobalProbeData>("same/key", 1).Value!.Value == 11, "Package withdrawal and a new service lifetime must retain global data.");
            Assert(bData.ReadGlobal<GlobalProbeData>("same/key", 1).Value!.Value == 22, "Owner B remains active during A cleanup.");
            Assert(restarted.DeleteGlobal("same/key", 1).Status == OwnerDataStatus.Found && restarted.ReadGlobal<GlobalProbeData>("same/key", 1).Status == OwnerDataStatus.Missing, "Explicit delete must have an observable missing result.");
            manager.RemoveOwner("Owner.B", ModOwnerCleanupReason.RuntimeShutdown);
            restarted.Dispose();
        }

        private static void OwnerDataRejectsBadPathsAndLinkedRoots()
        {
            string root = NewTempGameDir();
            var reader = new OwnerFileService(root, () => { });
            foreach (string key in new[] { "../escape", "/absolute", "C:/drive", "a//b", "a/./b", "a/../b", "CON.txt", "folder/NUL", "LPT1.json", "a.", "a ", "x:stream", "\\\\server\\share" })
                AssertThrows<ArgumentException>(() => reader.ReadText(key), "Invalid portable relative key must be rejected: " + key);
            File.WriteAllText(Path.Combine(root, "Case.txt"), "case");
            AssertThrows<ArgumentException>(() => reader.ReadText("case.txt"), "A mismatched case must be rejected consistently across filesystems.");
            string outside = NewTempGameDir();
            File.WriteAllText(Path.Combine(outside, "secret.txt"), "outside");
            string link = Path.Combine(root, "linked");
            Directory.CreateSymbolicLink(link, outside);
            try
            {
                AssertThrows<ArgumentException>(() => reader.ReadText("linked/secret.txt"), "Linked child path must not escape its package.");
                AssertThrows<ArgumentException>(() => new OwnerFileService(link, () => { }).ReadText("secret.txt"), "Linked package root must also be rejected.");
                var data = new GlobalDataService("Owner.A", link, () => { });
                AssertThrows<ArgumentException>(() => data.WriteGlobal("settings", new GlobalProbeData(), 1), "Global root links must be rejected before creating directories.");
            }
            finally { Directory.Delete(link); }
            File.WriteAllBytes(Path.Combine(root, "bad.txt"), new byte[] { 0xff, 0xff });
            Assert(reader.ReadText("bad.txt").Status == OwnerDataStatus.Corrupt, "Invalid UTF-8 cannot be silently replaced.");
            using (var held = new FileStream(Path.Combine(root, "Case.txt"), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                Assert(reader.ReadText("Case.txt").Status == OwnerDataStatus.IoError, "A locked package file is an IO failure, not Missing.");
        }

        private static void GlobalDataPreservesBytesOnCorruptionFutureSchemaAndAtomicFailure()
        {
            string root = NewTempGameDir();
            var files = new GlobalDataFileOperations();
            var data = new GlobalDataService("Owner.A", root, () => { }, files);
            Assert(data.ReadGlobal<GlobalProbeData>("settings", 1).Status == OwnerDataStatus.Missing, "Initially absent data must report Missing.");
            data.WriteGlobal("settings", new GlobalProbeData { Value = 10 }, 1);
            string path = Directory.GetFiles(root, "settings.json", SearchOption.AllDirectories).Single();
            string original = File.ReadAllText(path);
            var commit = files.Commit;
            files.Commit = (temp, dest, exists) => { Assert(File.Exists(temp), "Failure must occur after durable staging."); throw new IOException("injected replacement failure"); };
            Assert(data.WriteGlobal("settings", new GlobalProbeData { Value = 20 }, 1).Status == OwnerDataStatus.IoError, "Atomic replacement failure must report IO failure.");
            Assert(File.ReadAllText(path) == original && !Directory.GetFiles(root, "*.tmp", SearchOption.AllDirectories).Any(), "Failed publication must retain old bytes and remove its own temporary file.");
            files.Commit = commit;
            var future = JsonNode.Parse(original)!;
            future["schema"] = 9;
            File.WriteAllText(path, future.ToJsonString());
            string futureBytes = File.ReadAllText(path);
            Assert(data.ReadGlobal<GlobalProbeData>("settings", 1).Status == OwnerDataStatus.UnsupportedSchema, "Future author schema must be distinct.");
            Assert(data.WriteGlobal("settings", new GlobalProbeData(), 1).Status == OwnerDataStatus.UnsupportedSchema && data.DeleteGlobal("settings", 1).Status == OwnerDataStatus.UnsupportedSchema, "Old schema must not overwrite or delete future data.");
            Assert(File.ReadAllText(path) == futureBytes, "Future bytes must be unchanged.");
            future["schema"] = 1;
            future["format"] = 2;
            File.WriteAllText(path, future.ToJsonString());
            Assert(data.ReadGlobal<GlobalProbeData>("settings", 1).Status == OwnerDataStatus.UnsupportedSchema, "Future platform format must be distinct.");
            File.WriteAllText(path, "{ broken");
            Assert(data.ReadGlobal<GlobalProbeData>("settings", 1).Status == OwnerDataStatus.Corrupt && data.WriteGlobal("settings", new GlobalProbeData(), 1).Status == OwnerDataStatus.Corrupt, "Corrupt data cannot be replaced with defaults.");
            Assert(File.ReadAllText(path) == "{ broken", "Corrupt bytes must survive failed reads and writes.");
            File.WriteAllText(path, original);
            var tampered = JsonNode.Parse(original)!;
            tampered["payload"] = "{}";
            File.WriteAllText(path, tampered.ToJsonString());
            Assert(data.ReadGlobal<GlobalProbeData>("settings", 1).Status == OwnerDataStatus.Corrupt, "Payload digest mismatch must be rejected.");
            tampered["payload"] = "{ malformed";
            tampered["sha256"] = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("{ malformed"))).ToLowerInvariant();
            File.WriteAllText(path, tampered.ToJsonString());
            string malformedPayload = File.ReadAllText(path);
            Assert(data.WriteGlobal("settings", new GlobalProbeData(), 1).Status == OwnerDataStatus.Corrupt && File.ReadAllText(path) == malformedPayload, "A digest-valid but malformed payload must not be silently overwritten.");
            AssertThrows<ArgumentException>(() => data.WriteGlobal("oversize", new string('x', OwnerFileService.MaxBytes + 1), 1), "Oversized serialization must fail before publishing a file.");
            Assert(!Directory.GetFiles(root, "oversize.json", SearchOption.AllDirectories).Any(), "Oversized value cannot create committed data.");
            files.Read = _ => throw new UnauthorizedAccessException("denied");
            Assert(data.ReadGlobal<GlobalProbeData>("settings", 1).Status == OwnerDataStatus.AccessDenied && data.WriteGlobal("settings", new GlobalProbeData(), 1).Status == OwnerDataStatus.AccessDenied, "Permission failures must not be classified as Missing.");
            data.Dispose();
        }

        private static void GlobalDataMigrationsPublishOnceAndPreserveOriginalOnFailure()
        {
            string root = NewTempGameDir();
            var data = new GlobalDataService("Owner.A", root, () => { });
            data.WriteGlobal("settings", new GlobalProbeData { Value = 1 }, 1);
            string path = Directory.GetFiles(root, "settings.json", SearchOption.AllDirectories).Single();
            string original = File.ReadAllText(path);
            Assert(data.ReadGlobal<GlobalProbeData>("settings", 3).Status == OwnerDataStatus.MigrationRequired && data.WriteGlobal("settings", new GlobalProbeData(), 3).Status == OwnerDataStatus.MigrationRequired, "Schema changes need an explicit migration chain before writing.");
            int runs = 0;
            data.RegisterMigration<GlobalProbeData>("settings", 1, value => { runs++; value.Value++; return value; });
            data.RegisterMigration<GlobalProbeData>("settings", 2, value => throw new InvalidOperationException("failed migration"));
            Assert(data.ReadGlobal<GlobalProbeData>("settings", 3).Status == OwnerDataStatus.MigrationFailed && File.ReadAllText(path) == original, "Failure at a later migration must retain the original schema and bytes.");
            data.Dispose();
            data = new GlobalDataService("Owner.A", root, () => { });
            data.RegisterMigration<GlobalProbeData>("settings", 1, value => { runs++; value.Value++; return value; });
            data.RegisterMigration<GlobalProbeData>("settings", 2, value => { runs++; value.Value++; return value; });
            Assert(data.ReadGlobal<GlobalProbeData>("settings", 3).Value!.Value == 3 && runs == 3, "Complete migration chain must publish the final value once.");
            Assert(data.ReadGlobal<GlobalProbeData>("settings", 3).Value!.Value == 3 && runs == 3, "Repeated reads must not migrate again.");
            string committed = File.ReadAllText(path);
            Assert(!committed.Contains("__type") && !committed.Contains("DTMAPI.UnitTests"), "Persisted JSON must not carry CLR type metadata.");
            data.Dispose();
            var restarted = new GlobalDataService("Owner.A", root, () => { });
            Assert(restarted.ReadGlobal<GlobalProbeData>("settings", 3).Value!.Value == 3 && File.ReadAllText(path) == committed, "New service lifetime must read the committed schema without migration callbacks.");
            restarted.Dispose();
        }

        private static void GlobalDataMigrationFailureWindowsRetainCommittedBytes()
        {
            string root = NewTempGameDir();
            var files = new GlobalDataFileOperations();
            bool active = true;
            var data = new GlobalDataService("Owner.A", root, () => { if (!active) throw new ObjectDisposedException("owner"); }, files);
            data.WriteGlobal("settings", new GlobalProbeData { Value = 5 }, 1);
            string path = Directory.GetFiles(root, "settings.json", SearchOption.AllDirectories).Single();
            string original = File.ReadAllText(path);
            data.RegisterMigration<GlobalProbeData>("settings", 1, value => { value.Value++; return value; });
            files.Commit = (_, __, ___) => throw new IOException("migration publication interrupted");
            Assert(data.ReadGlobal<GlobalProbeData>("settings", 2).Status == OwnerDataStatus.IoError && File.ReadAllText(path) == original, "Failed migration publication must retain the entire old envelope.");
            data.Dispose();
            data = new GlobalDataService("Owner.A", root, () => { if (!active) throw new ObjectDisposedException("owner"); });
            data.RegisterMigration<GlobalProbeData>("settings", 1, value =>
            {
                AssertThrows(() => data.ReadGlobal<GlobalProbeData>("settings", 1), "Migration must not reenter the same data service.");
                active = false;
                value.Value++;
                return value;
            });
            AssertThrows<ObjectDisposedException>(() => data.ReadGlobal<GlobalProbeData>("settings", 2), "Owner closure during migration must prevent publication.");
            Assert(File.ReadAllText(path) == original, "Closing owner in a migration must retain the original bytes.");
            data.Dispose();
        }

        [DataContract]
        private sealed class GlobalProbeData
        {
            [DataMember] public int Value { get; set; }
        }


    }
}
