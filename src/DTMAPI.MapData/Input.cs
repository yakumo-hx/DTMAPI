using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace DTMAPI.MapData
{
    internal static class Input
    {
        internal static readonly string[] Layers = { "solid", "platform", "background" };
        internal static readonly string[] ArtLayers = { "solid", "platform", "background", "decoration" };
        internal static Dictionary<string, object?> Parse(string text, out object? candidate)
        {
            candidate = Json.Read(text); var data = candidate; Schema.Check(data); var d = Json.D(data);
            bool expanded = Json.S(d, "profile") != "layout-v0", auto = Json.S(d, "profile") == "layout-v2";
            void Fail(string field) => throw new InputException(field, "Invalid dimensions, duplicate identity or total budget.");
            if (Encoding.UTF8.GetByteCount(Json.Write(d)) > (expanded ? 16 : 4) * 1048576) Fail("$");
            if (expanded) Schema.Check(d, "expanded");
            if (auto) Schema.Check(Json.Get(d, "autoTiles"), "auto", "autoTiles");
            int cells = 0, points = 0, regions = 0, masks = 0;
            var ids = new HashSet<string>(StringComparer.Ordinal); var rooms = Json.A(d["rooms"]);
            for (int ri = 0; ri < rooms.Count; ri++)
            {
                var r = Json.D(rooms[ri]); string p = "rooms[" + ri + "]"; int n = Json.I(r, "width") * Json.I(r, "height");
                if (!ids.Add(Json.S(r, "id"))) Fail(p + ".id");
                cells += n; var ps = Json.A(r["points"]); points += ps.Count;
                if (cells > 65536 || points > 4096) Fail("rooms");
                foreach (string l in Layers) if (Json.A(Json.Get(r["terrain"], l)).Count != n) Fail(p + ".terrain." + l);
                var pointIds = new HashSet<string>(StringComparer.Ordinal);
                for (int pi = 0; pi < ps.Count; pi++)
                {
                    var pt = Json.D(ps[pi]); string pp = p + ".points[" + pi + "]", kind = Json.S(pt, "kind");
                    if (!pointIds.Add(Json.S(pt, "id"))) Fail(pp + ".id"); Schema.Check(pt["properties"], kind, pp + ".properties");
                    if (!expanded && (kind == "switch" || kind == "gate" || kind == "chest")) Fail(pp + ".kind");
                    if (kind == "switch" && (Json.D(pt["properties"]).ContainsKey("initialOpen") || Json.D(pt["properties"]).ContainsKey("initialState"))) Fail(pp + ".properties");
                }
                if (!expanded) continue;
                Schema.Check(r, "expandedRoom", p);
                void CheckCells(object? cs, string path, string layer)
                {
                    foreach (var pair in Json.D(cs))
                    {
                        if (!int.TryParse(pair.Key, out int index) || index >= n) Fail(path + "[" + pair.Key + "]");
                        var entry = Json.D(pair.Value);
                        if (auto && (entry.ContainsKey("family") || entry.ContainsKey("manual")) && (!Layers.Contains(layer) || !(Json.Get(entry, "family") is string) || !(Json.Get(entry, "manual") is bool))) Fail(path + "[" + pair.Key + "]");
                    }
                }
                foreach (string l in ArtLayers) CheckCells(Json.Get(r["tiles"], l), p + ".tiles." + l, l);
                var hs = Json.A(r["hiddenRegions"]); var regionIds = new HashSet<string>(StringComparer.Ordinal);
                for (int hi = 0; hi < hs.Count; hi++)
                {
                    var h = Json.D(hs[hi]); string hp = p + ".hiddenRegions[" + hi + "]";
                    if (!regionIds.Add(Json.S(h, "id"))) Fail(hp + ".id");
                    CheckCells(h["cells"], hp + ".cells", "hidden"); regions++; masks += Json.D(h["cells"]).Count;
                }
                if (regions > 256 || masks > 65536) Fail(p + ".hiddenRegions");
            }
            if (expanded)
            {
                var assets = Json.D(d["assets"]); long byteCount = 0, pixels = 0;
                foreach (var a in assets.Values) { byteCount += Json.I(a, "byteLength"); pixels += Json.I(a, "width") * Json.I(a, "height"); }
                if (byteCount > 4194304 || pixels > 4194304) Fail("assets");
                foreach (var pair in Json.D(d["tiles"]))
                {
                    var t = Json.D(pair.Value); string p = "tiles." + pair.Key; bool author = Json.S(t, "source") == "author";
                    Schema.Check(t, author ? "authorTile" : "nativeTile", p);
                    if (author)
                    {
                        if (!assets.TryGetValue(Json.S(t, "asset"), out var a)) Fail(p + ".asset");
                        var rect = t["rect"];
                        if (Json.I(rect, "x") + Json.I(rect, "width") > Json.I(a, "width") || Json.I(rect, "y") + Json.I(rect, "height") > Json.I(a, "height")) Fail(p + ".rect");
                    }
                    else if (new[] { "data", "image", "path", "asset" }.Any(t.ContainsKey)) Fail(p);
                    var size = author ? t["rect"] : t;
                    if (Json.I(size, "width") / Json.N(t, "ppu") > 6 || Json.I(size, "height") / Json.N(t, "ppu") > 6) Fail(p + ".ppu");
                }
            }
            return d;
        }

        // Integrity/preflight only. Texture decoding and native asset resolution
        // remain an explicit consumer gate; compilation does not create bitmaps.
        internal static void CheckPng(string hash, object? asset)
        {
            string text = Json.S(asset, "data");
            if (text.Length != ((Json.I(asset, "byteLength") + 2) / 3) * 4 || !System.Text.RegularExpressions.Regex.IsMatch(text, "^[A-Za-z0-9+/]*={0,2}$") || text.EndsWith("\n", StringComparison.Ordinal)) throw new FormatException("PNG base64 length or alphabet.");
            byte[] data = Convert.FromBase64String(text);
            if (data.Length < 57 || data.Length > 4194304 || data.Length != Json.I(asset, "byteLength") || !data.Take(8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) throw new FormatException("PNG bytes or signature.");
            using (var sha = SHA256.Create()) if (BitConverter.ToString(sha.ComputeHash(data)).Replace("-", "").ToLowerInvariant() != hash) throw new FormatException("PNG hash.");
            uint U(int i) => ((uint)data[i] << 24) | ((uint)data[i + 1] << 16) | ((uint)data[i + 2] << 8) | data[i + 3];
            bool header = false, idat = false, end = false;
            for (int offset = 8; offset < data.Length;)
            {
                if (offset + 12 > data.Length) throw new FormatException("PNG truncated chunk.");
                uint length = U(offset); if (length > data.Length - offset - 12) throw new FormatException("PNG chunk length.");
                int stop = offset + 12 + (int)length; string type = Encoding.ASCII.GetString(data, offset + 4, 4);
                uint crc = 0xffffffff;
                for (int i = offset + 4; i < stop - 4; i++) { crc ^= data[i]; for (int k = 0; k < 8; k++) crc = (crc & 1) != 0 ? 0xedb88320 ^ (crc >> 1) : crc >> 1; }
                if ((crc ^ 0xffffffff) != U(stop - 4) || !System.Text.RegularExpressions.Regex.IsMatch(type, "^[A-Za-z]{4}$")) throw new FormatException("PNG CRC.");
                if (type == "acTL" || type == "fcTL" || type == "fdAT") throw new FormatException("Animated PNG unsupported.");
                if (!header && type != "IHDR") throw new FormatException("Missing first IHDR.");
                if (type == "IHDR")
                {
                    if (header || length != 13 || U(offset + 8) != Json.I(asset, "width") || U(offset + 12) != Json.I(asset, "height") || data[offset + 18] != 0 || data[offset + 19] != 0 || data[offset + 20] > 1) throw new FormatException("PNG header or dimensions.");
                    header = true;
                }
                if (type == "IDAT") idat = true;
                if (type == "IEND") { if (length != 0 || !idat || stop != data.Length) throw new FormatException("PNG end or trailing bytes."); end = true; }
                offset = stop;
            }
            if (!end) throw new FormatException("Missing PNG end.");
        }
    }
}
