using System.Collections.Generic;
using System.Linq;

namespace DTMAPI.MapData
{
    // The private geometry consumer has a deliberately smaller scope than the editor.
    // Keep these checks paired with compiler.js and the shared private-runtime corpus.
    internal static class PrivateRuntime
    {
        internal static Compilation Validate(Compilation result)
        {
            if (!result.Success) return result;
            var map = result.Map!;
            var issues = new List<MapDiagnostic>(result.Diagnostics);
            void Add(string code, string field, RoomDefinition? room = null, MapCell? cell = null, string? point = null) =>
                issues.Add(new MapDiagnostic(code, "error", field, room?.Id, cell.HasValue ? Json.Map("x", cell.Value.X, "y", cell.Value.Y) : null, point, code));
            if (map.Rooms.Count != 1) Add("runtime-room-count", "rooms");
            for (int ri = 0; ri < map.Rooms.Count; ri++)
            {
                var r = map.Rooms[ri]; string path = "rooms[" + ri + "]";
                if (r.Width > 48) Add("runtime-size", path + ".width", r);
                if (r.Height > 40) Add("runtime-size", path + ".height", r);
                if (r.Origin.X != 0 || r.Origin.Y != 0) Add("runtime-origin", path + ".origin", r);
                if (r.OpenEdges.Count != 0) { int i = r.OpenEdges[0]; Add("runtime-open-edge", path + ".terrain.solid[" + i + "]", r, new MapCell(i % r.Width, i / r.Width)); }
                if (r.Points.Count(p => p.Kind == "spawn") != 1 || r.Points.Count(p => p.Kind == "exit") != 1) Add("runtime-point-count", path + ".points", r);
                for (int pi = 0; pi < r.Points.Count; pi++)
                {
                    var p = r.Points[pi];
                    if (p.Kind != "spawn" && p.Kind != "exit") Add("runtime-point-kind", path + ".points[" + pi + "].kind", r, p.Cell, p.Id);
                }
                var appearance = Json.D(Json.Read(r.AppearanceJson));
                foreach (var layer in Json.D(appearance["tiles"]))
                    foreach (var tile in (IEnumerable<object?>)layer.Value!)
                        if (Json.Get(tile, "ref") != null) { int i = Json.I(tile, "index"); Add("runtime-art", path + ".tiles." + layer.Key + "[" + i + "].ref", r, new MapCell(i % r.Width, i / r.Width)); break; }
                if (Json.A(appearance["hiddenRegions"]).Count != 0) Add("runtime-hidden", path + ".hiddenRegions", r);
            }
            issues.Sort((a, b) => System.StringComparer.Ordinal.Compare(a.SortKey, b.SortKey));
            return new Compilation(issues, issues.Any(d => d.Severity == "error") ? null : map.Data);
        }
    }
}
