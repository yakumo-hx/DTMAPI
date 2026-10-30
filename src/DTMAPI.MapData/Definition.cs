using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace DTMAPI.MapData
{
    public sealed class MapDiagnostic
    {
        internal MapDiagnostic(string code, string severity, string field, string? room, object? cell, string? point, string message)
        { Code = code; Severity = severity; Field = field; RoomId = room; PointId = point; Message = message; Cell = cell == null ? (MapCell?)null : new MapCell(Json.I(cell, "x"), Json.I(cell, "y")); }
        public string Code { get; }
        public string Severity { get; }
        public string Field { get; }
        public string? RoomId { get; }
        public string? PointId { get; }
        public MapCell? Cell { get; }
        public string Message { get; }
        internal string SortKey => Field + "\0" + Code + "\0" + Severity + "\0" + Cell?.Y.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\0" + Cell?.X.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\0" + PointId;
        internal object Data => Json.Map("code", Code, "severity", Severity, "field", Field, "roomId", RoomId, "cell", Cell.HasValue ? Json.Map("x", Cell.Value.X, "y", Cell.Value.Y) : null, "pointId", PointId, "message", Message);
    }
    public readonly struct MapCell
    {
        public MapCell(int x, int y) { X = x; Y = y; }
        public int X { get; }
        public int Y { get; }
    }
    public sealed class Compilation
    {
        internal Compilation(List<MapDiagnostic> diagnostics, Dictionary<string, object?>? data)
        { Diagnostics = diagnostics.AsReadOnly(); Map = data == null ? null : new MapDefinition(data); }
        public bool Success => Map != null;
        public IReadOnlyList<MapDiagnostic> Diagnostics { get; }
        public MapDefinition? Map { get; }
        public string ToJson() => Json.Write(Json.Map("ok", Success, "diagnostics", Diagnostics.Select(d => d.Data).ToArray(), "map", Map?.Data));
    }
    /// <summary>Validated offline result. Publishing native objects is a separate consumer responsibility.</summary>
    public sealed class MapDefinition
    {
        internal Dictionary<string, object?> Data { get; }
        internal MapDefinition(Dictionary<string, object?> data)
        {
            Data = data; var source = Json.Get(data, "source"); ProjectId = Json.S(source, "projectId"); Id = Json.S(Json.Get(source, "map"), "id"); Revision = Json.I(Json.Get(source, "map"), "revision");
            Rooms = ((List<object?>)data["rooms"]!).Select(r => new RoomDefinition(Json.D(r))).ToList().AsReadOnly();
        }
        public string ProjectId { get; }
        public string Id { get; }
        public int Revision { get; }
        public double CellSize => 1.5;
        public IReadOnlyList<RoomDefinition> Rooms { get; }
        public string ToJson() => Json.Write(Data);
    }
    public sealed class RoomDefinition
    {
        private readonly HashSet<int> solid;
        internal RoomDefinition(Dictionary<string, object?> d)
        {
            Id = Json.S(d, "id"); Width = Json.I(d, "width"); Height = Json.I(d, "height"); Origin = new MapCell(Json.I(d["origin"], "x"), Json.I(d["origin"], "y"));
            IReadOnlyList<int> Cells(string key) => ((List<int>)d[key]!).AsReadOnly();
            Solid = Cells("solid"); Platform = Cells("platform"); Background = Cells("background"); Ground = Cells("ground"); Standing = Cells("standing"); OpenEdges = Cells("openEdges"); solid = new HashSet<int>(Solid);
            Points = ((Dictionary<string, object?>[])d["points"]!).Select(p => new PointDefinition(p)).ToList().AsReadOnly();
            GeometryJson = Json.Write(Json.Map("platformRuns", d["platformRuns"], "exposedEdges", d["exposedEdges"], "cavities", d["cavities"]));
            AppearanceJson = Json.Write(Json.Map("tiles", d["tiles"], "hiddenRegions", d["hiddenRegions"]));
        }
        public string Id { get; }
        public int Width { get; }
        public int Height { get; }
        public MapCell Origin { get; }
        public IReadOnlyList<int> Solid { get; }
        public IReadOnlyList<int> Platform { get; }
        public IReadOnlyList<int> Background { get; }
        public IReadOnlyList<int> Ground { get; }
        public IReadOnlyList<int> Standing { get; }
        public IReadOnlyList<int> OpenEdges { get; }
        public IReadOnlyList<PointDefinition> Points { get; }
        public string GeometryJson { get; }
        public string AppearanceJson { get; }
        public bool IsSolid(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height && solid.Contains(y * Width + x);
    }
    public sealed class PointDefinition
    {
        internal PointDefinition(Dictionary<string, object?> p)
        { Id = Json.S(p, "id"); Kind = Json.S(p, "kind"); Anchor = Json.S(p, "anchor"); Cell = new MapCell(Json.I(p["cell"], "x"), Json.I(p["cell"], "y")); LocalX = Json.N(p["local"], "x"); LocalY = Json.N(p["local"], "y"); WorldX = Json.N(p["world"], "x"); WorldY = Json.N(p["world"], "y"); PropertiesJson = Json.Write(p["properties"]); ExitTargetKind = Kind == "exit" ? Json.S(Json.Get(p["properties"], "target"), "kind") : null; }
        public string Id { get; }
        public string Kind { get; }
        public string Anchor { get; }
        public MapCell Cell { get; }
        public double LocalX { get; }
        public double LocalY { get; }
        public double WorldX { get; }
        public double WorldY { get; }
        public string PropertiesJson { get; }
        public string? ExitTargetKind { get; }
    }
}
