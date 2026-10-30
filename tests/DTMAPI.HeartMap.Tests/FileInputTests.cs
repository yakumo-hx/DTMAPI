using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DTMAPI.Internal.HeartMap;

internal static class FileInputTests
{
    internal static void Run()
    {
        string root = Path.Combine(Path.GetTempPath(), "DTMAPI-map05j-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            string input = Path.Combine(root, "author.dtmap.json"), cache = Path.Combine(root, "cache");
            byte[] a = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "inputs/map05j-a.dtmap.json"));
            byte[] b = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "inputs/map05j-b.dtmap.json"));
            var messages = new List<string>(); var loader = new MapFileInput(cache, messages.Add);
            File.WriteAllBytes(input, a); var first = new MapCatalog();
            Check(loader.Load(input, first, out var original) && original!.Room.Width == 48, "A admitted");
            string cached = Directory.GetFiles(cache).Single(); Check(File.ReadAllBytes(cached).SequenceEqual(a), "cache is exact author bytes");
            File.WriteAllBytes(input, b);
            Check(first.Maps.Single().Room.Width == 48 && ReferenceEquals(first.Maps.Single(), original), "file edit cannot mutate active definition");
            var restarted = new MapCatalog(); Check(loader.Load(input, restarted, out var edited) && edited!.Room.Width == 32 && edited.Room.Platform.Count == 11, "restart applies B actual geometry");
            Check(File.ReadAllBytes(cached).SequenceEqual(b), "new valid revision atomically cached");
            // Duplicate identity must not poison the last-valid file for this source.
            File.WriteAllBytes(input, a);
            Check(!loader.Load(input, restarted, out _) && File.ReadAllBytes(cached).SequenceEqual(b), "duplicate does not replace existing map/cache");
            File.WriteAllText(input, "{broken");
            var fallback = new MapCatalog(); Check(loader.Load(input, fallback, out var retained) && retained!.Room.Width == 32, "bad restart candidate retains B");
            Check(messages.Any(m => m.Contains("invalid-json") && m.Contains("field=$")) && messages.Any(m => m.StartsWith("file-retained")), "original rejection and fallback observable");
            Check(File.ReadAllText(input) == "{broken" && File.ReadAllBytes(cached).SequenceEqual(b), "rejection rewrites neither input nor last-valid cache");
            File.Delete(input); Check(loader.Load(input, new MapCatalog(), out _), "missing selected file can retain last valid");
            File.WriteAllBytes(input, new byte[] { 0xc3, 0x28 }); Check(loader.Load(input, new MapCatalog(), out _), "strict invalid UTF8 rejected with retained input");
            using (var oversized = File.Create(input)) oversized.SetLength(MapFileInput.MaxBytes + 1L);
            Check(loader.Load(input, new MapCatalog(), out _) && messages.Any(m => m.Contains("exceeds 16 MiB")), "oversized input bounded before parsing");
            File.WriteAllText(cached, "{}");
            Check(!loader.Load(input, new MapCatalog(), out _), "corrupt cache is revalidated and refused");
            Check(!loader.Load("relative.dtmap.json", new MapCatalog(), out _) && !loader.Load(Path.Combine(root, "author.json"), new MapCatalog(), out _), "explicit absolute project path required");
            if (Path.DirectorySeparatorChar == '\\') Check(!loader.Load("C:relative.dtmap.json", new MapCatalog(), out _) && !loader.Load("\\root-relative.dtmap.json", new MapCatalog(), out _), "drive/current-directory-relative paths rejected");
            File.WriteAllBytes(input, b);
            string unwritable = Path.Combine(root, "file-not-directory"); File.WriteAllText(unwritable, "occupied");
            Check(new MapFileInput(unwritable, messages.Add).Load(input, new MapCatalog(), out _) && messages.Any(m => m.StartsWith("file-cache-write-failed")), "cache write failure cannot block valid current content");
            Console.WriteLine("PASS file input: actual A/B, cold revision, retained bad/missing/UTF8/oversized candidate, duplicate, corrupt cache, I/O failure");
        }
        finally { Directory.Delete(root, true); }
    }
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
}
