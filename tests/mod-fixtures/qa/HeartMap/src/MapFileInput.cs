using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace DTMAPI.Internal.HeartMap
{
    // Cold-entry only. The cache contains author definitions, never gameplay state.
    // Every fallback is recompiled by the current consumer before registration.
    internal sealed class MapFileInput
    {
        internal const int MaxBytes = 16 * 1024 * 1024;
        private readonly string cacheRoot;
        private readonly Action<string> log;
        internal MapFileInput(string cacheRoot, Action<string> log) { this.cacheRoot = cacheRoot; this.log = log; }
        internal bool Load(string path, MapCatalog catalog, out MapContent? content)
        {
            content = null;
            string full;
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path) ||
                    (Path.DirectorySeparatorChar == '\\' && (Path.GetPathRoot(path)?.Length ?? 0) < 3) ||
                    !path.EndsWith(".dtmap.json", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("absolute .dtmap.json path required");
                full = Path.GetFullPath(path);
            }
            catch (Exception ex) { log("file-rejected field=EditorFiles; " + ex.Message); return false; }
            string key = Hash(Encoding.UTF8.GetBytes(Path.DirectorySeparatorChar == '\\' ? full.ToUpperInvariant() : full));
            string cache = Path.Combine(cacheRoot, key + ".dtmap.json");
            string error;
            try
            {
                byte[] bytes = ReadBounded(full);
                if (catalog.TryAdd(Decode(bytes), out content, out error))
                {
                    log("file-accepted path=" + full + "; sha256=" + Hash(bytes) + "; revision=" + content!.Definition.Revision + "; boundary=process-start");
                    try { SaveCache(cache, bytes); }
                    catch (Exception ex) { log("file-cache-write-failed path=" + full + "; current input accepted; " + ex.Message); }
                    return true;
                }
            }
            catch (Exception ex) { error = "field=EditorFiles; " + ex.Message; }
            log("file-rejected path=" + full + "; " + error);
            try
            {
                byte[] bytes = ReadBounded(cache);
                if (catalog.TryAdd(Decode(bytes), out content, out error))
                {
                    log("file-retained path=" + full + "; sha256=" + Hash(bytes) + "; revision=" + content!.Definition.Revision + "; last-valid author input revalidated");
                    return true;
                }
            }
            catch (Exception ex) { error = ex.Message; }
            log("file-unavailable path=" + full + "; previous maps retained; fallback=" + error);
            return false;
        }
        private static string Decode(byte[] bytes) => new UTF8Encoding(false, true).GetString(bytes).TrimStart('\uFEFF');
        internal static string Hash(byte[] bytes) { using var hash = SHA256.Create(); return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
        private static byte[] ReadBounded(string path)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (stream.Length > MaxBytes) throw new InvalidDataException("input exceeds 16 MiB");
            using var data = new MemoryStream(); var buffer = new byte[8192]; int count;
            while ((count = stream.Read(buffer, 0, buffer.Length)) != 0) { if (data.Length + count > MaxBytes) throw new InvalidDataException("input exceeds 16 MiB"); data.Write(buffer, 0, count); }
            return data.ToArray();
        }
        private void SaveCache(string cache, byte[] bytes)
        {
            Directory.CreateDirectory(cacheRoot);
            string temp = cache + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllBytes(temp, bytes);
                if (File.Exists(cache)) File.Replace(temp, cache, null); else File.Move(temp, cache);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }
    }
}
