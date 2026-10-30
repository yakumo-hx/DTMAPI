using System;
using System.IO;
using System.Linq;
using System.Threading;
using DTMAPI.Abstractions;
using DTMAPI.Core.Services;

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {
        private static void ProtectedSourcesEnforceOwnerScope()
        {
            string root = Path.Combine(Path.GetTempPath(), "mes-owner-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            const string key = "equipment-slots-DTMAPI.MoreEquipmentSlotsMod.json";
            File.WriteAllText(Path.Combine(root, key), "source");
            var manager = new OwnerServiceManager();
            try
            {
                var other = manager.Create("Other.Owner", () => { }, () => { }, configRoot: root);
                Assert(other.GetService<IProtectedSourceFiles>() == null, "Other owner cannot acquire MES file capability.");
                var scope = manager.Create(ProtectedSourceFileService.Owner, () => { }, () => { }, configRoot: root);
                var service = scope.GetService<IProtectedSourceFiles>()!;
                var snapshot = service.Capture(key).Snapshot!;
                Assert(ReferenceEquals(service, scope.GetService<IProtectedSourceFiles>()), "MES service is scope-local and cached.");
                manager.RemoveOwner(ProtectedSourceFileService.Owner, DTMAPI.Core.Runtime.ModOwnerCleanupReason.RuntimeShutdown);
                var next = manager.Create(ProtectedSourceFileService.Owner, () => { }, () => { }, configRoot: root);
                Assert(!ReferenceEquals(service, next.GetService<IProtectedSourceFiles>()), "New activation receives new service.");
                Assert(service.Capture(key).Status == ProtectedSourceStatus.Expired && snapshot.TryDelete().Status == ProtectedSourceStatus.Expired && File.Exists(Path.Combine(root, key)), "Old service and snapshot remain expired after same-ID reactivation.");
                snapshot.Dispose();
                manager.RemoveOwner(ProtectedSourceFileService.Owner, DTMAPI.Core.Runtime.ModOwnerCleanupReason.RuntimeShutdown);
                manager.RemoveOwner("Other.Owner", DTMAPI.Core.Runtime.ModOwnerCleanupReason.RuntimeShutdown);
            }
            finally { Directory.Delete(root, true); }
        }
        private static void ProtectedSourcesEnforceFileBoundaries()
        {
            string root = Path.Combine(Path.GetTempPath(), "dtmapi-protected-source-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            const string flat = "equipment-slots-DTMAPI.MoreEquipmentSlotsMod.json";
            const string scoped = "protected-items/equipment-slots/slot-0/" + flat;
            int thread = Thread.CurrentThread.ManagedThreadId;
            bool alive = true;
            Action runtimeThread = () => { if (Thread.CurrentThread.ManagedThreadId != thread) throw new InvalidOperationException("runtime thread"); };
            using var service = new ProtectedSourceFileService(root, () => { if (!alive) throw new ObjectDisposedException("scope"); }, runtimeThread);
            int count = 0;
            void Check(bool ok, string name) { if (!ok) throw new Exception(name); Console.WriteLine("PASS protected source: " + ++count + " " + name); }
            string FilePath(string key) => Path.Combine(root, key.Replace('/', Path.DirectorySeparatorChar));
            void Put(string key, string value = "same bytes") { Directory.CreateDirectory(Path.GetDirectoryName(FilePath(key))!); File.WriteAllText(FilePath(key), value); }
            IProtectedSourceSnapshot Capture(string key)
            {
                var result = service.Capture(key);
                Check(result.Status == ProtectedSourceStatus.Captured && result.Snapshot != null, "capture " + key + " = " + result.Status + ": " + result.Detail);
                return result.Snapshot!;
            }
            try
            {
                foreach (string bad in new[] { "", "../" + flat, "/" + flat, "C:/" + flat, "//?/C:/" + flat, flat + ":ads", flat + ".previous", flat + ".journal", "CON", "equipment-slots-Other.Owner.json", "protected-items/equipment-slots/slot-01/" + flat, "protected-items/equipment-slots/slot--1/" + flat, "protected-items/equipment-slots/slot-2147483648/" + flat, "protected-items/equipment-slots/slot-1/x/" + flat })
                    Check(service.Capture(bad).Status == ProtectedSourceStatus.Rejected, "reject key " + bad);
                Check(service.Capture(flat).Status == ProtectedSourceStatus.Missing, "missing is explicit");
                Put(flat);
                using (var source = Capture(flat))
                {
                    source.CopyBytes()[0] ^= 31;
                    Check(source.Revalidate().Status == ProtectedSourceStatus.Unchanged, "copy mutation leaves host baseline unchanged");
                    using (File.Open(FilePath(flat), FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
                    Check(true, "capture and revalidate leave no file handle across calls");
                    Check(source.TryDelete().Status == ProtectedSourceStatus.DeleteCommitted && !File.Exists(FilePath(flat)), "same handle deletion succeeds");
                    Put(flat, "replacement after committed delete");
                    Check(source.TryDelete().Status == ProtectedSourceStatus.Expired && File.Exists(FilePath(flat)), "consumed snapshot never deletes new file");
                }
                using (var source = Capture(flat))
                {
                    File.AppendAllText(FilePath(flat), " changed");
                    Check(source.Revalidate().Status == ProtectedSourceStatus.Changed, "changed bytes reject revalidation");
                    Check(source.TryDelete().Status == ProtectedSourceStatus.Changed && File.Exists(FilePath(flat)), "changed bytes retained");
                    Check(source.TryDelete().Status == ProtectedSourceStatus.Expired, "failed deletion consumes attempt");
                }
                Put(flat);
                using (var source = Capture(flat))
                {
                    File.Move(FilePath(flat), FilePath(flat) + ".old"); Put(flat);
                    Check(source.TryDelete().Status == ProtectedSourceStatus.Changed && File.Exists(FilePath(flat)), "same-byte replacement retained by identity");
                }
                using (var source = Capture(flat))
                {
                    using (var held = File.Open(FilePath(flat), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                    {
                        Check(service.Capture(flat).Status == ProtectedSourceStatus.Busy, "busy capture is not missing");
                        Check(source.TryDelete().Status == ProtectedSourceStatus.Busy, "busy delete retains source");
                    }
                    Check(source.TryDelete().Status == ProtectedSourceStatus.Expired && File.Exists(FilePath(flat)), "unlock does not permit retry");
                }
                using (var source = Capture(flat))
                {
                    File.SetAttributes(FilePath(flat), FileAttributes.ReadOnly);
                    Check(source.TryDelete().Status == ProtectedSourceStatus.AccessDenied && File.Exists(FilePath(flat)), "read-only refusal retained");
                    File.SetAttributes(FilePath(flat), FileAttributes.Normal);
                }
                using (var source = Capture(flat))
                {
                    source.Dispose(); source.Dispose();
                    Check(source.TryDelete().Status == ProtectedSourceStatus.Expired && File.Exists(FilePath(flat)), "cancel/dispose is idempotent and never deletes");
                }
                Put(scoped);
                using (var source = Capture(scoped))
                {
                    string parent = Path.GetDirectoryName(FilePath(scoped))!;
                    Directory.Move(parent, parent + "-retained"); Directory.CreateDirectory(parent);
                    File.Move(Path.Combine(parent + "-retained", flat), FilePath(scoped));
                    Check(source.TryDelete().Status == ProtectedSourceStatus.Changed && File.Exists(FilePath(scoped)), "ancestor replacement rejected even with original file identity and bytes");
                }
                using (var source = Capture(scoped))
                {
                    string directory = Path.Combine(root, "protected-items");
                    Directory.Move(directory, directory + "-retained");
                    Directory.CreateSymbolicLink(directory, directory + "-retained");
                    Check(service.Capture(scoped).Status == ProtectedSourceStatus.Rejected, "capture rejects reparse ancestor");
                    Check(source.TryDelete().Status == ProtectedSourceStatus.Rejected, "snapshot rejects reparse ancestor");
                    Directory.Delete(directory); Directory.Move(directory + "-retained", directory);
                }
                File.Delete(FilePath(flat)); Directory.CreateDirectory(FilePath(flat));
                Check(service.Capture(flat).Status == ProtectedSourceStatus.Rejected, "directory is not a source");
                Directory.Delete(FilePath(flat)); Put(flat);
                File.WriteAllBytes(FilePath(flat), new byte[1024 * 1024 + 1]);
                Check(service.Capture(flat).Status == ProtectedSourceStatus.Rejected, "oversize is retained");
                Put(flat);
                using (var source = Capture(flat))
                {
                    Exception? error = null;
                    var other = new Thread(() => { try { source.TryDelete(); } catch (Exception ex) { error = ex; } });
                    other.Start(); other.Join();
                    Check(error is InvalidOperationException && File.Exists(FilePath(flat)), "wrong thread rejected before IO or consumption");
                    alive = false;
                    Check(source.TryDelete().Status == ProtectedSourceStatus.Expired && File.Exists(FilePath(flat)), "old scope cannot delete");
                    alive = true;
                    service.Dispose();
                    using var replacementService = new ProtectedSourceFileService(root, () => { }, runtimeThread);
                    Check(source.TryDelete().Status == ProtectedSourceStatus.Expired && File.Exists(FilePath(flat)), "same-owner replacement service never revives old snapshot");
                }
                Console.WriteLine("Protected source filesystem checks completed: " + count);
            }
            finally { Directory.Delete(root, true); }
        }
    }
}
