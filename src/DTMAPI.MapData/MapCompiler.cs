using System;
using System.Collections.Generic;
using System.Linq;

namespace DTMAPI.MapData
{
    /// <summary>Private experimental authoring compiler; no Runtime or Unity dependency.</summary>
    public static class MapCompiler
    {
        public const int Version = 1;
        public static Compilation CompileForPrivateRuntime(string text) => PrivateRuntime.Validate(Compile(text));
        public static Compilation Compile(string text)
        {
            var diagnostics = new List<MapDiagnostic>(); int omitted = 0; bool hasErrors = false;
            void Add(string code, string severity, string field, RoomGrid? room = null, object? cell = null, string? pointId = null, string? message = null)
            {
                var item = new MapDiagnostic(code, severity, field, room?.Id, cell, pointId, message ?? code);
                hasErrors |= severity == "error";
                int lo = 0, hi = diagnostics.Count;
                while (lo < hi) { int mid = (lo + hi) / 2; if (StringComparer.Ordinal.Compare(diagnostics[mid].SortKey, item.SortKey) <= 0) lo = mid + 1; else hi = mid; }
                if (lo < 1000) diagnostics.Insert(lo, item); else omitted++;
                if (diagnostics.Count > 1000) { diagnostics.RemoveAt(1000); omitted++; }
            }
            Dictionary<string, object?> d; object? candidate = null;
            try { d = Input.Parse(text, out candidate); }
            catch (InputException ex)
            {
                string field = ex.Field.StartsWith("$.", StringComparison.Ordinal) ? ex.Field.Substring(2) : ex.Field;
                var location = Locate(candidate, field);
                diagnostics.Add(new MapDiagnostic(ex.Code, "error", field, location.Room, location.Cell, null, ex.Message));
                return new Compilation(diagnostics, null);
            }
            bool expanded = Json.S(d, "profile") != "layout-v0", auto = Json.S(d, "profile") == "layout-v2";
            var assets = expanded ? Json.D(d["assets"]) : Json.Map(); var tiles = expanded ? Json.D(d["tiles"]) : Json.Map();
            foreach (var pair in assets) try { Input.CheckPng(pair.Key, pair.Value); } catch (FormatException ex) { Add("asset-integrity", "error", "assets." + pair.Key, message: ex.Message); }
            var rooms = Json.A(d["rooms"]).Select(r => new RoomGrid(r)).ToList();
            var pointIndex = new Dictionary<string, object?>(StringComparer.Ordinal); var writers = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var r in rooms) foreach (var p in Json.A(r.Data["points"]))
            {
                pointIndex.Add(r.Id + "/" + Json.S(p, "id"), p);
                if (Json.S(p, "kind") == "switch" && Json.Get(Json.Get(p, "properties"), "target") is object t) { string key = Json.S(t, "roomId") + "/" + Json.S(t, "pointId"); writers[key] = writers.TryGetValue(key, out int n) ? n + 1 : 1; }
            }
            var compiledRooms = new List<object?>();
            for (int ri = 0; ri < rooms.Count; ri++)
            {
                var r = rooms[ri]; string rp = "rooms[" + ri + "]"; int count = r.Width * r.Height;
                var solid = new List<int>(); var platform = new List<int>(); var background = new List<int>(); var ground = new List<int>(); var standing = new List<int>(); var openEdges = new List<int>();
                var exposedEdges = new List<object?>(); var runs = new List<object?>();
                for (int i = 0; i < count; i++)
                {
                    int x = i % r.Width, y = i / r.Width;
                    if (r.Occupied("solid", x, y))
                    {
                        solid.Add(i);
                        void Edge(string side, int dx, int dy) { if (!r.Occupied("solid", x + dx, y + dy)) exposedEdges.Add(Json.Map("index", i, "side", side)); }
                        Edge("n", 0, 1); Edge("e", 1, 0); Edge("s", 0, -1); Edge("w", -1, 0);
                    }
                    else
                    {
                        if (y > 0 && (r.Occupied("solid", x, y - 1) || r.Occupied("platform", x, y - 1))) ground.Add(i);
                        if (r.Clearance(x, y)) standing.Add(i);
                        if (r.Edge(i)) openEdges.Add(i);
                    }
                    if (r.Occupied("platform", x, y))
                    {
                        platform.Add(i); string field = rp + ".terrain.platform[" + i + "]";
                        if (r.Occupied("solid", x, y)) Add("terrain-conflict", "error", field, r, r.Cell(i));
                        if (y + 2 >= r.Height || r.Occupied("solid", x, y + 1) || r.Occupied("solid", x, y + 2)) Add("platform-clearance", "error", field, r, r.Cell(i));
                        if (!r.Occupied("platform", x - 1, y)) { int length = 1; while (r.Occupied("platform", x + length, y)) length++; runs.Add(Json.Map("x", x, "y", y, "length", length)); }
                    }
                    if (r.Occupied("background", x, y)) background.Add(i);
                }
                if (openEdges.Count > 0) Add("edge-open", "warning", rp + ".terrain.solid", r, r.Cell(openEdges[0]));
                var cc = r.Components(Enumerable.Range(0, count).Where(i => !r.Occupied("solid", i % r.Width, i / r.Width)));
                var cavities = cc.Select(c => Json.Map("first", c[0], "count", c.Count, "touchesEdge", c.Any(r.Edge))).ToArray();
                if (cc.Count > 1) foreach (var c in cc) Add("disconnected-cavity", "warning", rp + ".terrain.solid", r, r.Cell(c[0]));
                var points = Json.A(r.Data["points"]);
                if (points.Count(p => Json.S(p, "kind") == "spawn" && Json.B(Json.Get(p, "properties"), "primary")) != 1) Add("spawn-count", "error", rp + ".points", r);
                if (!points.Any(p => Json.S(p, "kind") == "exit")) Add("exit-missing", "error", rp + ".points", r);
                for (int pi = 0; pi < points.Count; pi++)
                {
                    var p = Json.D(points[pi]); var q = Json.D(p["properties"]); var cell = p["cell"];
                    int x = Json.I(cell, "x"), y = Json.I(cell, "y"); string pp = rp + ".points[" + pi + "]", kind = Json.S(p, "kind"), id = Json.S(p, "id");
                    void PointIssue(string code, string suffix, string severity = "error") => Add(code, severity, pp + suffix, r, cell, id);
                    if (!r.InBounds(x, y)) PointIssue("point-bounds", ".cell");
                    else if ((kind == "spawn" || kind == "exit") && !r.Clearance(x, y)) PointIssue("standing-clearance", ".cell");
                    else if (kind != "spawn" && kind != "exit" && kind != "gate" && r.Occupied("solid", x, y)) PointIssue("point-in-solid", ".cell");
                    if (kind == "exit" && !r.InBounds(x + Json.I(q["trigger"], "width") - 1, y + Json.I(q["trigger"], "height") - 1)) PointIssue("trigger-bounds", ".properties.trigger");
                    if (kind == "resource" || kind == "monster") { bool present = Json.S(q, "prototypeId").Length > 0; PointIssue(present ? "prototype-unverified" : "prototype-empty", ".properties.prototypeId", present ? "warning" : "error"); }
                    if (kind == "decoration") PointIssue("decoration-marker", ".kind", "warning");
                    if (kind == "switch")
                    {
                        var t = q["target"]; string key = t == null ? "" : Json.S(t, "roomId") + "/" + Json.S(t, "pointId");
                        if (!pointIndex.TryGetValue(key, out var target) || Json.S(target, "kind") != "gate") PointIssue(target == null ? "dangling-target" : "target-kind", ".properties.target");
                        if (writers.TryGetValue(key, out int writeCount) && writeCount > 1) PointIssue("multiple-writers", ".properties.target");
                    }
                    if (kind == "gate")
                    {
                        int w = Json.I(q["size"], "width"), h = Json.I(q["size"], "height"), dx = Json.I(q["displacement"], "x"), dy = Json.I(q["displacement"], "y");
                        if (!r.InBounds(x, y) || !r.InBounds(x + w - 1, y + h - 1)) PointIssue("gate-bounds", ".properties.size");
                        if (!r.InBounds(x + dx, y + dy) || !r.InBounds(x + dx + w - 1, y + dy + h - 1)) PointIssue("gate-bounds", ".properties.displacement");
                        int sx = Math.Min(x, x + dx), sy = Math.Min(y, y + dy), sw = Math.Abs(dx) + w, sh = Math.Abs(dy) + h;
                        if (points.Any(pt => { string k = Json.S(pt, "kind"); var c = Json.Get(pt, "cell"); int px = Json.I(c, "x"), py = Json.I(c, "y"); return (k == "spawn" || k == "exit") && sx < px + 2 && px < sx + sw && sy < py + 2 && py < sy + sh; })) PointIssue("gate-blocks-required", ".properties.displacement");
                    }
                    if (kind == "chest")
                    {
                        var rw = q["rewards"]; var items = Json.A(Json.Get(rw, "items"));
                        bool bad = Json.S(rw, "mode") == "reference" ? string.IsNullOrWhiteSpace(Json.S(rw, "listRef")) : items.Count == 0 || items.Any(a => string.IsNullOrWhiteSpace(Json.S(a, "itemRef")));
                        PointIssue(bad ? "chest-rewards-empty" : "chest-rewards-unverified", ".properties.rewards", bad ? "error" : "warning");
                    }
                }
                object[] Art(object? raw, string field, string layer)
                {
                    return Json.D(raw).OrderBy(p => int.Parse(p.Key)).Select(pair =>
                    {
                        int i = int.Parse(pair.Key); string? reference = Json.Get(pair.Value, "ref") as string;
                        if (auto && Input.Layers.Contains(layer))
                        {
                            var selected = Adjacency.Resolve(d, r, layer, i); reference = selected.Ref;
                            if (selected.Code != null) Add(selected.Code, selected.Code == "auto-fallback" ? "warning" : "error", field + "[" + i + "]", r, r.Cell(i));
                        }
                        if (reference != null && !tiles.ContainsKey(reference)) Add("tile-missing", "error", field + "[" + i + "].ref", r, r.Cell(i));
                        else if (reference != null && Json.S(tiles[reference], "source") == "native") Add("native-unresolved", "warning", field + "[" + i + "].ref", r, r.Cell(i));
                        return (object)Json.Map("index", i, "ref", reference);
                    }).ToArray();
                }
                var compiledTiles = Json.Map(); foreach (string layer in Input.ArtLayers) compiledTiles[layer] = expanded ? Art(Json.Get(r.Data["tiles"], layer), rp + ".tiles." + layer, layer) : Array.Empty<object>();
                var hidden = new List<object?>();
                if (expanded)
                {
                    var hs = Json.A(r.Data["hiddenRegions"]);
                    for (int hi = 0; hi < hs.Count; hi++)
                    {
                        var h = Json.D(hs[hi]); string field = rp + ".hiddenRegions[" + hi + "]"; var hc = r.Components(Json.D(h["cells"]).Keys.Select(int.Parse)); foreach (var c in hc) c.Sort();
                        if (hc.Count == 0) Add("mask-empty", "warning", field + ".cells", r);
                        if (hc.Count > 1) Add("mask-disconnected", "warning", field + ".cells", r, r.Cell(hc[0][0]));
                        if (hc.Count > 0 && hc.SelectMany(c => c).All(i => r.Occupied("solid", i % r.Width, i / r.Width))) Add("mask-all-solid", "error", field + ".cells", r, r.Cell(hc[0][0]));
                        hidden.Add(Json.Map("id", h["id"], "name", h["name"], "material", h["material"], "alpha", h["alpha"], "fadeSeconds", h["fadeSeconds"], "components", hc, "tiles", Art(h["cells"], field + ".cells", "hidden")));
                    }
                }
                int ox = Json.I(r.Data["origin"], "x"), oy = Json.I(r.Data["origin"], "y");
                compiledRooms.Add(Json.Map("id", r.Id, "name", r.Data["name"], "width", r.Width, "height", r.Height, "origin", r.Data["origin"],
                    "bounds", Json.Map("minX", ox * 1.5, "minY", oy * 1.5, "maxX", (ox + r.Width) * 1.5, "maxY", (oy + r.Height) * 1.5),
                    "solid", solid, "platform", platform, "background", background, "ground", ground, "standing", standing, "openEdges", openEdges, "exposedEdges", exposedEdges, "platformRuns", runs, "cavities", cavities, "tiles", compiledTiles, "hiddenRegions", hidden,
                    "points", points.Select(pt =>
                    {
                        var p = Json.D(pt); bool gate = Json.S(p, "kind") == "gate"; double x = (Json.I(p["cell"], "x") + (gate ? 0 : .5)) * 1.5, y = Json.I(p["cell"], "y") * 1.5 + (gate ? 0 : .1);
                        return Json.Map("id", p["id"], "name", p["name"], "kind", p["kind"], "cell", p["cell"], "properties", p["properties"], "anchor", gate ? "lower-left" : "feet", "local", Json.Map("x", x, "y", y), "world", Json.Map("x", x + ox * 1.5, "y", y + oy * 1.5));
                    }).ToArray()));
            }
            if (omitted > 0) diagnostics.Insert(0, new MapDiagnostic("diagnostic-limit", hasErrors ? "error" : "warning", "$", null, null, null, "More than 1000 diagnostics; showing the first 1000 in field order."));
            return new Compilation(diagnostics, hasErrors ? null : Json.Map("format", "dtmapi.map-compiled", "version", Version,
                "source", Json.Map("projectId", d["projectId"], "profile", d["profile"], "map", d["map"]), "cellSize", 1.5, "rule", auto ? Json.Map("id", Adjacency.Rule, "version", Adjacency.Version) : null,
                "appearance", d["appearance"], "rooms", compiledRooms, "tiles", tiles, "assets", assets));
        }
        private static (string? Room, object? Cell) Locate(object? candidate, string field)
        {
            var roomMatch = System.Text.RegularExpressions.Regex.Match(field, @"^rooms\[(\d+)\]");
            if (!roomMatch.Success || !(candidate is Dictionary<string, object?> document) || !(Json.Get(document, "rooms") is List<object?> rooms) || !int.TryParse(roomMatch.Groups[1].Value, out int index) || index >= rooms.Count || !(rooms[index] is Dictionary<string, object?> room)) return (null, null);
            object? cell = null;
            var gridMatch = System.Text.RegularExpressions.Regex.Match(field, @"\.(?:terrain|tiles)\.[a-z]+(?:\[|\.)(\d+)");
            if (gridMatch.Success && int.TryParse(gridMatch.Groups[1].Value, out int gridIndex) && Json.Get(room, "width") is double width && width > 0 && width <= int.MaxValue && width == Math.Truncate(width)) cell = Json.Map("x", gridIndex % (int)width, "y", gridIndex / (int)width);
            var pointMatch = System.Text.RegularExpressions.Regex.Match(field, @"\.points\[(\d+)\]");
            if (pointMatch.Success && int.TryParse(pointMatch.Groups[1].Value, out int pointIndex) && Json.Get(room, "points") is List<object?> points && pointIndex < points.Count && points[pointIndex] is Dictionary<string, object?> point && Json.Get(point, "cell") is Dictionary<string, object?> c && Json.Get(c, "x") is double x && Json.Get(c, "y") is double y && Math.Truncate(x) == x && Math.Truncate(y) == y && x >= int.MinValue && x <= int.MaxValue && y >= int.MinValue && y <= int.MaxValue) cell = c;
            return (Json.Get(room, "id") as string, cell);
        }
    }
}
