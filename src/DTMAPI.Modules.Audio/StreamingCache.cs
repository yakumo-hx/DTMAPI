using DTMAPI.AudioContent.Preparation;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DTMAPI.Abstractions.Internal;
using DTMAPI.AudioContent;

namespace DTMAPI.Modules.Audio
{
    // Audio-owned physical accounting. Main owns admission, pins and publishing;
    // exactly one worker owns its source handle and bounded output preparation.
    internal sealed class StreamingCache
    {
        private readonly Action<string> log;
        private readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>(StringComparer.Ordinal);
        private long unknownBytes;
        private Job? active;
        internal string Root { get; }
        internal string DecoderExecutable = AudioDecoder.DefaultExecutable;
        internal long Bytes => unknownBytes + entries.Values.Sum(e => e.InFlight ? Measure(e) : e.Charge);
        internal long OwnerBytes(string owner) => entries.Values.Where(e => SameOwner(e.Owner, owner)).Sum(e => e.InFlight ? Measure(e) : e.Charge);
        internal int Pins => entries.Values.Sum(e => e.Pins);
        internal int Workers => active?.Work == null ? 0 : 1;
        internal bool Busy => active != null;
        internal static string Identity(PackageResourceMetadata m) => string.Join("\n", "wwise2023.1.16-v150/" + AudioDecoder.Profile, m.Source, m.Key,
            m.Length.ToString(CultureInfo.InvariantCulture), m.ModifiedUtcTicks.ToString(CultureInfo.InvariantCulture));
        // This hashes a small metadata key, never source, Bank or WEM contents.
        internal static string Key(PackageResourceMetadata m, string expectedSha = "") => WwisePcmBank.Digest(Encoding.UTF8.GetBytes(Identity(m) + (expectedSha.Length == 0 ? "" : "\nsha256=" + expectedSha)));
        internal static uint MediaId(string key) => WwisePcmBank.HashId("dtmapi.audio.stream." + key + ".media");
        private static bool SameOwner(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        internal sealed class Entry
        {
            internal string Owner = "", Key = "", Identity = "", Directory = "", Wem = "";
            internal uint MediaId;
            internal long Charge;
            internal int Pins;
            internal bool Complete, InFlight;
            internal bool External;
            internal readonly Dictionary<string, long> Files = new Dictionary<string, long>(StringComparer.Ordinal);
            internal StreamingPcmInfo? Info;
            internal string Record => Path.Combine(Directory, "entry");
            internal string Marker => Path.Combine(Directory, "complete");
        }
        internal sealed class Pin : IDisposable
        {
            private Entry? entry;
            internal Pin(Entry entry) { this.entry = entry; entry.Pins++; }
            public void Dispose() { if (entry != null) { entry.Pins--; entry = null; } }
        }
        internal sealed class Job
        {
            internal Entry Entry = null!;
            internal Pin Pin = null!;
            internal PackageResourceReference Source = null!;
            internal PackageResourceMetadata Metadata = null!;
            internal string BankPath = "";
            internal bool WriteMedia, WriteBank, Hit, Finished;
            internal Task<StreamingPcmInfo>? Work;
            internal readonly List<Tuple<PackageResourceReference, PackageResourceMetadata>> Additional = new List<Tuple<PackageResourceReference, PackageResourceMetadata>>();
            internal string[] DependencyBanks = Array.Empty<string>();
            internal readonly List<string> Publish = new List<string>();
            internal long ResidentBytes;
            internal readonly CancellationTokenSource Cancel = new CancellationTokenSource();
            internal bool Done => Work == null || Work.IsCompleted;
            internal void Revoke() => Cancel.Cancel();
        }
        internal StreamingCache(string root, Action<string> log)
        {
            if (!Path.IsPathRooted(root)) throw new ArgumentException("AudioCacheRootRequired");
            Root = Path.GetFullPath(root); this.log = log;
            Safe(Root); Directory.CreateDirectory(Path.Combine(Root, "Entries")); Directory.CreateDirectory(Path.Combine(Root, "Media"));
            foreach (string directory in Directory.GetDirectories(Path.Combine(Root, "Entries")))
            {
                Safe(directory);
                try
                {
                    string key = Path.GetFileName(directory);
                    if (key.Length != 64 || key.Any(c => !"0123456789abcdef".Contains(c))) throw new InvalidDataException("AudioCacheEntryKey");
                    var entry = new Entry { Key = key, Directory = directory };
                    using (var r = new BinaryReader(new MemoryStream(ReadRecord(Path.Combine(directory, "entry"))), Encoding.UTF8))
                    {
                        int version = r.ReadInt32();
                        if (version != 1 && version != 2) throw new InvalidDataException("AudioCacheFormat");
                        entry.External = version == 2;
                        entry.Owner = r.ReadString(); entry.Identity = r.ReadString(); entry.MediaId = r.ReadUInt32();
                        if (entry.External)
                        {
                            int count = r.ReadInt32(); if (count < 1 || count > 1024) throw new InvalidDataException("AudioCacheRecord");
                            for (int i = 0; i < count; i++) { string relative = r.ReadString(); if (relative.Contains("..") || Path.IsPathRooted(relative)) throw new InvalidDataException("AudioCacheRecord"); entry.Files.Add(Path.Combine(Root, relative), r.ReadInt64()); }
                        }
                        else entry.Info = new StreamingPcmInfo { Channels = r.ReadInt32(), Rate = r.ReadInt32(), Offset = r.ReadInt32(), Bytes = r.ReadInt32() };
                        if (r.BaseStream.Position != r.BaseStream.Length || !entry.External && entry.MediaId != MediaId(key)) throw new InvalidDataException("AudioCacheRecord");
                    }
                    if (!entry.External) entry.Wem = Path.Combine(Root, "Media", entry.MediaId.ToString(CultureInfo.InvariantCulture) + ".wem");
                    entry.Complete = ValidComplete(entry);
                    entry.Charge = Measure(entry); entries.Add(key, entry);
                }
                catch (Exception ex) { log("CacheRecordRetained path=" + directory + "; reason=" + ex.Message); }
            }
            long physical = Files(Root).Sum(p => new FileInfo(p).Length);
            unknownBytes = Math.Max(0, physical - entries.Values.Sum(e => e.Charge));
            log("StreamingCache directory=" + Root + "; bytes=" + Bytes);
        }
        private static byte[] ReadRecord(string path)
        { if (new FileInfo(path).Length > 16384) throw new InvalidDataException("AudioCacheRecordSize"); return File.ReadAllBytes(path); }
        private static void Safe(string path)
        {
            for (string? p = Path.GetFullPath(path); p != null; p = Path.GetDirectoryName(p))
                if ((Directory.Exists(p) || File.Exists(p)) && (File.GetAttributes(p) & FileAttributes.ReparsePoint) != 0) throw new IOException("AudioCacheReparsePoint: " + p);
        }
        private static string[] Files(string root)
        {
            if (!Directory.Exists(root)) return Array.Empty<string>();
            foreach (string directory in Directory.GetDirectories(root, "*", SearchOption.AllDirectories)) Safe(directory);
            var files = Directory.GetFiles(root, "*", SearchOption.AllDirectories); foreach (string file in files) Safe(file); return files;
        }
        private static long Size(string path) { if (path.Length == 0) return 0; Safe(path); return File.Exists(path) ? new FileInfo(path).Length : 0; }
        private static bool Exists(string path, long bytes) { Safe(path); return File.Exists(path) && new FileInfo(path).Length == bytes; }
        private static bool ValidComplete(Entry e) => Exists(e.Marker, 1) && (e.External ? e.Files.All(f => Exists(f.Key, f.Value)) :
            e.Info != null && e.Info.Bytes > 0 && e.Info.Channels >= 1 && e.Info.Channels <= 2 && e.Info.Rate >= 8000 && e.Info.Rate <= 48000 && Exists(e.Wem, e.Info.WemBytes));
        private static long Measure(Entry e) => Files(e.Directory).Sum(p => new FileInfo(p).Length) + (e.External ?
            e.Files.Keys.Where(p => !p.StartsWith(e.Directory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)).Sum(p => Size(p) + Size(p + ".part")) : Size(e.Wem) + Size(e.Wem + ".part"));
        private byte[] Record(Entry e, StreamingPcmInfo? info)
        {
            using (var s = new MemoryStream())
            using (var w = new BinaryWriter(s, Encoding.UTF8, true))
            {
                w.Write(e.External ? 2 : 1); w.Write(e.Owner); w.Write(e.Identity); w.Write(e.MediaId);
                if (e.External)
                {
                    w.Write(e.Files.Count);
                    foreach (var file in e.Files) { w.Write(file.Key.Substring(Root.Length).TrimStart(Path.DirectorySeparatorChar)); w.Write(file.Value); }
                }
                else { w.Write(info?.Channels ?? 0); w.Write(info?.Rate ?? 0); w.Write(info?.Offset ?? 0); w.Write(info?.Bytes ?? 0); }
                w.Flush(); return s.ToArray();
            }
        }
        internal Job Begin(string owner, PackageResourceReference source, PackageResourceMetadata metadata, uint bankId, WwisePcmBank.Recipe[] recipes, bool stream = true, long memoryAvailable = long.MaxValue, string expectedSha = "")
        {
            if (active != null) throw new InvalidOperationException("AudioPreparationBusy");
            source.Validate(metadata);
            string key = Key(metadata, expectedSha); bool existing = entries.TryGetValue(key, out var entry);
            if (entry == null) entry = new Entry { Owner = owner, Key = key, Identity = Identity(metadata), Directory = Path.Combine(Root, "Entries", key), MediaId = MediaId(key) };
            if (!SameOwner(entry.Owner, owner) || entry.Identity != Identity(metadata)) throw new IOException("AudioCacheIdentityCollision");
            entry.Wem = Path.Combine(Root, "Media", entry.MediaId.ToString(CultureInfo.InvariantCulture) + ".wem");
            if (entries.Values.Any(e => e.Key != key && e.MediaId == entry.MediaId)) throw new IOException("AudioCacheMediaIdCollision");
            if (!existing && (File.Exists(entry.Wem) || File.Exists(entry.Wem + ".part") || Directory.Exists(entry.Directory)))
                throw new IOException("AudioCacheUnownedPhysicalEntry: retained in cache; exit before manual cleanup.");
            entry.Complete = existing && ValidComplete(entry);
            byte[] bank = stream ? WwisePcmBank.BuildStreaming(bankId, entry.MediaId, recipes) : Array.Empty<byte>();
            string bankPath = Path.Combine(entry.Directory, bankId.ToString(CultureInfo.InvariantCulture) + ".bnk");
            long bankBytes = stream ? bank.Length : entry.Info == null ? -1 : WwisePcmBank.FileSize(entry.Info, recipes);
            if (bankBytes > memoryAvailable) throw new InvalidDataException("AudioMemoryBudget: resident Bank exceeds available audio memory.");
            bool writeBank = bankBytes < 0 || !Exists(bankPath, bankBytes), writeMedia = !entry.Complete;
            if ((writeMedia || writeBank && File.Exists(bankPath)) && entry.Pins != 0) throw new IOException("AudioCachePinnedGeneration");
            // Retry only this entry's unfinished files; deletion failure retains
            // its physical charge. Active or old-generation pins are never deleted.
            if (entry.Pins == 0 && existing)
            {
                try
                {
                    foreach (string path in Files(entry.Directory).Where(p => p.EndsWith(".part", StringComparison.Ordinal))) File.Delete(path);
                    Safe(entry.Wem + ".part"); File.Delete(entry.Wem + ".part");
                    if (writeMedia) { File.Delete(entry.Wem); File.Delete(entry.Marker); }
                    if (writeBank) File.Delete(bankPath);
                }
                finally { entry.Charge = Measure(entry); }
            }
            byte[] record = Record(entry, entry.Info);
            long needed = (writeMedia ? checked(metadata.Length + 8 + record.Length + 1) : 0) + (writeBank ? bank.Length : 0);
            if (record.Length > 16384) throw new IOException("AudioCacheRecordSize");
            if (!existing) entries.Add(key, entry);
            entry.Charge = checked(entry.Charge + needed); entry.InFlight = true;
            PackageResourceReadLease? input = null;
            var job = new Job { Entry = entry, Source = source, Metadata = metadata, BankPath = bankPath, WriteMedia = writeMedia, WriteBank = writeBank,
                Hit = !writeMedia, Pin = new Pin(entry), ResidentBytes = Math.Max(0, bankBytes) };
            try
            {
                if (writeMedia) input = source.Acquire(metadata, metadata.Length);
                Safe(entry.Directory); Directory.CreateDirectory(entry.Directory);
                if (!existing) File.WriteAllBytes(entry.Record, record);
                if (!writeMedia && !writeBank) { job.Work = null; }
                else
                {
                    var reader = input; input = null;
                    job.Work = Task.Run(() => {
                        using (reader)
                        {
                            job.Cancel.Token.ThrowIfCancellationRequested();
                            void Prepare(Stream pcm, StreamingPcmInfo layout)
                            {
                                job.ResidentBytes = stream ? bank.Length : WwisePcmBank.FileSize(layout, recipes);
                                if (job.ResidentBytes > memoryAvailable) throw new InvalidDataException("AudioMemoryBudget: resident PCM Bank exceeds available audio memory; use streaming music for long tracks.");
                                if (writeMedia) layout.WriteWem(pcm, entry.Wem + ".part", job.Cancel.Token);
                                if (writeBank)
                                    using (var output = new FileStream(bankPath + ".part", FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
                                        WwisePcmBank.Write(output, pcm, layout, bankId, entry.MediaId, recipes, stream, job.Cancel.Token);
                            }
                            if (writeMedia)
                            {
                                VerifyDeclaredHash(reader!, expectedSha, metadata.Key, job.Cancel.Token);
                                return AudioDecoder.Decode(reader!, DecoderExecutable, entry.Directory, Prepare, job.Cancel.Token);
                            }
                            using (var pcm = File.OpenRead(entry.Wem))
                            {
                                var info = new StreamingPcmInfo { Channels = entry.Info!.Channels, Rate = entry.Info.Rate, Offset = 52, Bytes = entry.Info.Bytes };
                                Prepare(pcm, info); return info;
                            }
                        }
                    });
                }
                active = job;
                log("StreamPrepare owner=" + owner + "; key=" + key + "; cache=" + (job.Hit ? "Hot" : "Cold") + "; charged=" + entry.Charge + "; pin=" + entry.Pins);
                return job;
            }
            catch
            {
                input?.Dispose(); entry.InFlight = false; entry.Charge = Measure(entry); job.Pin.Dispose(); job.Cancel.Dispose(); throw;
            }
        }
        private static void VerifyDeclaredHash(Stream input, string expected, string resource, CancellationToken cancel)
        {
            if (expected.Length == 0) return; // Preserve the existing explicit author opt-in only.
            using (var hash = System.Security.Cryptography.SHA256.Create())
            {
                var buffer = new byte[65536]; int count;
                while ((count = input.Read(buffer, 0, buffer.Length)) != 0)
                { cancel.ThrowIfCancellationRequested(); hash.TransformBlock(buffer, 0, count, null, 0); }
                hash.TransformFinalBlock(Array.Empty<byte>(), 0, 0); input.Position = 0;
                if (WwisePcmBank.Hex(hash.Hash!) != expected) throw new InvalidDataException("AudioResourceHashMismatch: " + resource);
            }
        }
        internal static string BankKey(PackageResourceMetadata bank, IEnumerable<PackageResourceMetadata> dependencies, string expectedSha = "") =>
            WwisePcmBank.Digest(Encoding.UTF8.GetBytes("declared-bank/1\n" + Identity(bank) + "\n" + string.Join("\n", dependencies.Select(Identity)) + "\n" + expectedSha));

        internal Job BeginBank(string owner, PackageResourceReference source, PackageResourceMetadata metadata,
            IReadOnlyList<PackageResourceReference> media, IReadOnlyList<PackageResourceReference> dependencies, long memoryAvailable, string expectedSha = "")
        {
            if (active != null) throw new InvalidOperationException("AudioPreparationBusy");
            source.Validate(metadata);
            var extra = media.Concat(dependencies).Select(r => Tuple.Create(r, r.Inspect())).ToArray();
            string key = BankKey(metadata, extra.Select(p => p.Item2), expectedSha);
            long resident = metadata.Length + extra.Skip(media.Count).Sum(p => p.Item2.Length);
            if (resident > memoryAvailable) throw new InvalidDataException("AudioMemoryBudget: declared Bank residency exceeds available audio memory.");
            if (!entries.TryGetValue(key, out var entry))
            {
                entry = new Entry { Owner = owner, Key = key, Identity = "declared-bank/1", Directory = Path.Combine(Root, "Entries", key), External = true };
                entries.Add(key, entry);
            }
            if (!entry.External || !SameOwner(entry.Owner, owner)) throw new IOException("AudioCacheIdentityCollision");
            string bankPath = Path.Combine(entry.Directory, Path.GetFileName(metadata.Key));
            var inputs = new[] { Tuple.Create(source, metadata) }.Concat(extra).ToArray();
            var paths = new[] { bankPath }.Concat(extra.Select((p, i) => Path.Combine(i < media.Count ? Path.Combine(Root, "Media") : entry.Directory, Path.GetFileName(p.Item2.Key)))).ToArray();
            if (paths.Distinct(StringComparer.OrdinalIgnoreCase).Count() != paths.Length) throw new InvalidDataException("AudioBankDependencyNameCollision");
            var previous = entries.Values.Where(e => e != entry && paths.Any(p => e.Wem == p || e.Files.ContainsKey(p))).ToArray();
            // An updated package changes metadata identity while keeping authored
            // media IDs. Only retire an unused generation of this owner's same
            // Bank; a live pin, another Bank or another owner is still a collision.
            foreach (var old in previous)
                if (!old.External || old.Pins != 0 || old.InFlight || !SameOwner(old.Owner, owner) ||
                    !old.Files.ContainsKey(Path.Combine(old.Directory, Path.GetFileName(metadata.Key))))
                    throw new IOException("AudioMediaNameCollision: " + Path.GetFileName(paths.First(p => old.Wem == p || old.Files.ContainsKey(p))) + "; declare unique compatible media IDs.");
            for (int i = 0; i < paths.Length; i++)
            {
                Safe(paths[i]);
                if (!entry.Files.ContainsKey(paths[i]) && !previous.Any(e => e.Files.ContainsKey(paths[i])) && File.Exists(paths[i]))
                    throw new IOException("AudioCacheUnownedPhysicalEntry: " + paths[i]);
            }
            foreach (var old in previous)
            {
                foreach (string path in old.Files.Keys.SelectMany(p => new[] { p, p + ".part" }).Concat(new[] { old.Marker, old.Record })) { Safe(path); File.Delete(path); }
                if (Directory.Exists(old.Directory) && !Directory.EnumerateFileSystemEntries(old.Directory).Any()) Directory.Delete(old.Directory);
                entries.Remove(old.Key);
                log("BankCacheGenerationReplaced owner=" + owner + "; bank=" + Path.GetFileName(metadata.Key));
            }
            for (int i = 0; i < paths.Length; i++) entry.Files[paths[i]] = inputs[i].Item2.Length;
            bool hit = ValidComplete(entry);
            if (!hit && entry.Pins != 0) throw new IOException("AudioCachePinnedGeneration");
            var job = new Job { Entry = entry, Source = source, Metadata = metadata, BankPath = bankPath, Hit = hit,
                Pin = new Pin(entry), DependencyBanks = paths.Skip(1 + media.Count).ToArray(), ResidentBytes = resident };
            job.Additional.AddRange(extra);
            var readers = new List<PackageResourceReadLease>();
            try
            {
                Directory.CreateDirectory(entry.Directory);
                if (!hit)
                {
                    File.Delete(entry.Marker);
                    for (int i = 0; i < inputs.Length; i++)
                    {
                        File.Delete(paths[i] + ".part"); File.Delete(paths[i]); job.Publish.Add(paths[i]);
                        readers.Add(inputs[i].Item1.Acquire(inputs[i].Item2, inputs[i].Item2.Length));
                    }
                    File.WriteAllBytes(entry.Record, Record(entry, null));
                    job.Work = Task.Run(() => {
                        try
                        {
                            VerifyDeclaredHash(readers[0], expectedSha, metadata.Key, job.Cancel.Token);
                            for (int i = 0; i < readers.Count; i++)
                                using (var output = new FileStream(paths[i] + ".part", FileMode.CreateNew, FileAccess.Write, FileShare.None))
                                    WwisePcmBank.Copy(readers[i], output, readers[i].Length, job.Cancel.Token);
                            return new StreamingPcmInfo();
                        }
                        finally { foreach (var reader in readers) reader.Dispose(); }
                    });
                }
                entry.InFlight = true; active = job; return job;
            }
            catch { foreach (var reader in readers) reader.Dispose(); job.Pin.Dispose(); job.Cancel.Dispose(); throw; }
        }

        // Caller rechecks its registration/session guard before invoking this.
        // No late prepared result can itself acquire playback authority.
        internal void Finish(Job job, bool accept)
        {
            if (job.Finished) return;
            if (!ReferenceEquals(active, job) || !job.Done) throw new InvalidOperationException("AudioPreparationPending");
            try
            {
                var info = job.Work == null ? job.Entry.Info! : job.Work.GetAwaiter().GetResult();
                if (!accept || job.Cancel.IsCancellationRequested) throw new OperationCanceledException("AudioPreparationRevoked");
                job.Source.Validate(job.Metadata);
                foreach (var additional in job.Additional) additional.Item1.Validate(additional.Item2);
                if (job.Entry.External)
                {
                    foreach (string path in job.Publish) { if (!Exists(path + ".part", job.Entry.Files[path])) throw new IOException("AudioPreparedResourceLength"); File.Move(path + ".part", path); }
                    File.WriteAllBytes(job.Entry.Marker, new byte[] { 1 });
                    job.Entry.Complete = ValidComplete(job.Entry);
                    if (!job.Entry.Complete) throw new IOException("AudioPreparedCompletionMissing");
                    log("BankPrepared owner=" + job.Entry.Owner + "; cache=" + (job.Hit ? "Hot" : "Cold"));
                    return;
                }
                if (job.WriteMedia)
                {
                    if (!Exists(job.Entry.Wem + ".part", info.WemBytes)) throw new IOException("AudioPreparedMediaLength");
                    File.Move(job.Entry.Wem + ".part", job.Entry.Wem);
                    job.Entry.Info = info; File.WriteAllBytes(job.Entry.Record, Record(job.Entry, info));
                    File.WriteAllBytes(job.Entry.Marker, new byte[] { 1 });
                }
                if (job.WriteBank) File.Move(job.BankPath + ".part", job.BankPath);
                job.Entry.Complete = ValidComplete(job.Entry);
                if (!job.Entry.Complete) throw new IOException("AudioPreparedCompletionMissing");
                log("StreamPrepared owner=" + job.Entry.Owner + "; key=" + job.Entry.Key + "; cache=" + (job.Hit ? "Hot" : "Cold") + "; wemBytes=" + info.WemBytes);
            }
            catch
            {
                foreach (string path in (job.Entry.External ? job.Publish.Select(p => p + ".part") : new[] { job.Entry.Wem + ".part", job.BankPath + ".part" }))
                    try { Safe(path); File.Delete(path); } catch (Exception ex) { log("CachePartRetained path=" + path + "; reason=" + ex.Message); }
                throw;
            }
            finally
            {
                job.Finished = true; job.Entry.InFlight = false; job.Entry.Charge = Measure(job.Entry); active = null; job.Cancel.Dispose();
            }
        }
    }
}
