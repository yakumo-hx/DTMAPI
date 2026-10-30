using System;
using System.Collections.Generic;
using System.Linq;

namespace DTMAPI.MapData
{
    /// <summary>Experimental original radius-one rule: N E S W NW NE SW SE bits.</summary>
    public static class Adjacency
    {
        public const string Rule = "dtmapi.adjacency13";
        public const int Version = 1;
        public const int Radius = 1;
        public static string? RoleForMask(int mask)
        {
            if (mask < 0 || mask > 255) throw new ArgumentOutOfRangeException(nameof(mask));
            int[] a = { 1, 1, 4, 4 }, b = { 8, 2, 8, 2 }, diagonal = { 16, 32, 64, 128 };
            string[] inner = { "innerTopLeft", "innerTopRight", "innerBottomLeft", "innerBottomRight" };
            var notches = new List<string>();
            for (int i = 0; i < 4; i++) if ((mask & a[i]) != 0 && (mask & b[i]) != 0 && (mask & diagonal[i]) == 0) notches.Add(inner[i]);
            int cardinal = mask & 15;
            if (cardinal == 15) return notches.Count == 0 ? "core" : notches.Count == 1 ? notches[0] : null;
            if (notches.Count != 0) return null;
            switch (cardinal)
            {
                case 14: return "top"; case 13: return "right"; case 11: return "bottom"; case 7: return "left";
                case 6: return "topLeft"; case 12: return "topRight"; case 3: return "bottomLeft"; case 9: return "bottomRight";
                default: return null;
            }
        }
        internal static (string? Ref, string? Code) Resolve(Dictionary<string, object?> document, RoomGrid room, string layer, int index)
        {
            var entry = room.Art(layer, index);
            string? original = Json.Get(entry, "ref") as string;
            if (!(Json.Get(entry, "family") is string family)) return (original, null);
            var families = Json.D(Json.Get(document["autoTiles"], "families"));
            if (!families.TryGetValue(family, out var f)) return (null, "auto-family-missing");
            if (Json.B(entry, "manual") || Json.B(entry, "locked")) return (original, original == null ? "auto-reference-empty" : null);
            if (!room.Occupied(layer, index % room.Width, index / room.Width)) return (null, "auto-unoccupied");
            int[,] directions = { { 0, 1 }, { 1, 0 }, { 0, -1 }, { -1, 0 }, { -1, 1 }, { 1, 1 }, { -1, -1 }, { 1, -1 } };
            int mask = 0, x = index % room.Width, y = index / room.Width;
            for (int i = 0; i < 8; i++)
            {
                int nx = x + directions[i, 0], ny = y + directions[i, 1];
                if (room.Occupied(layer, nx, ny) && Equals(Json.Get(room.Art(layer, ny * room.Width + nx), "family"), family)) mask |= 1 << i;
            }
            string? role = RoleForMask(mask), candidate = role == null ? null : Json.Get(Json.Get(f, "roles"), role) as string;
            var tiles = Json.D(document["tiles"]);
            string? code = role == null ? "auto-unsupported" : candidate == null ? "auto-role-missing" : !tiles.ContainsKey(candidate) ? "auto-tile-missing" : null;
            if (code == null) return (candidate, null);
            string? fallback = Json.Get(f, "fallback") as string;
            if (code != "auto-tile-missing" && fallback != null && tiles.ContainsKey(fallback)) return (fallback, "auto-fallback");
            return (candidate ?? fallback, code);
        }
    }
    internal sealed class RoomGrid
    {
        internal RoomGrid(object? data) { Data = Json.D(data); Width = Json.I(data, "width"); Height = Json.I(data, "height"); Id = Json.S(data, "id"); }
        internal Dictionary<string, object?> Data { get; }
        internal int Width { get; }
        internal int Height { get; }
        internal string Id { get; }
        internal bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;
        internal bool Occupied(string layer, int x, int y) => InBounds(x, y) && (double)Json.A(Json.Get(Data["terrain"], layer))[y * Width + x]! == 1;
        internal Dictionary<string, object?> Art(string layer, int index) => Data.TryGetValue("tiles", out var tiles) && Json.D(Json.Get(tiles, layer)).TryGetValue(index.ToString(), out var entry) ? Json.D(entry) : Json.Map();
        internal bool Clearance(int x, int y)
        {
            if (y < 1 || !InBounds(x, y) || !InBounds(x + 1, y + 1)) return false;
            for (int xx = x; xx < x + 2; xx++) { if (!Occupied("solid", xx, y - 1) && !Occupied("platform", xx, y - 1)) return false; for (int yy = y; yy < y + 2; yy++) if (Occupied("solid", xx, yy)) return false; }
            return true;
        }
        internal Dictionary<string, object?> Cell(int i) => Json.Map("x", i % Width, "y", i / Width);
        internal bool Edge(int i) => i % Width == 0 || i / Width == 0 || i % Width == Width - 1 || i / Width == Height - 1;
        internal List<List<int>> Components(IEnumerable<int> indices)
        {
            var remaining = new SortedSet<int>(indices); var result = new List<List<int>>();
            while (remaining.Count > 0)
            {
                int first = remaining.Min; var queue = new List<int> { first }; remaining.Remove(first);
                for (int i = 0; i < queue.Count; i++)
                {
                    int x = queue[i] % Width, y = queue[i] / Width;
                    void Visit(int nx, int ny) { int key = ny * Width + nx; if (InBounds(nx, ny) && remaining.Remove(key)) queue.Add(key); }
                    Visit(x - 1, y); Visit(x + 1, y); Visit(x, y - 1); Visit(x, y + 1);
                }
                result.Add(queue);
            }
            return result;
        }
    }
}
