using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using DTMAPI.MapData;

namespace DTMAPI.Internal.HeartMap
{
    // Internal geometry-only consumer. Visual imports and interactive/persistent points
    // need their own acceptance; accepting an offline document does not enable them.
    internal sealed class MapContent
    {
        internal readonly MapDefinition Definition;
        internal readonly RoomDefinition Room;
        internal readonly string DungeonName, SceneName, RoomName, RoomId;
        internal readonly PointDefinition Spawn, Exit;
        internal const float CellSize = 1.5f;
        internal MapContent(MapDefinition definition)
        {
            Definition = definition;
            if (definition.Rooms.Count != 1) throw new InvalidOperationException("runtime-room-count: this slice requires one room per map.");
            Room = definition.Rooms[0];
            if (Room.Width > 48 || Room.Height > 40 || Room.Origin.X != 0 || Room.Origin.Y != 0 || Room.OpenEdges.Count != 0)
                throw new InvalidOperationException("runtime-geometry-scope: closed room, origin (0,0), at most 48x40 required.");
            if (Room.Points.Count(p => p.Kind == "spawn") != 1 || Room.Points.Count(p => p.Kind == "exit") != 1 || Room.Points.Any(p => p.Kind != "spawn" && p.Kind != "exit"))
                throw new InvalidOperationException("runtime-point-scope: one spawn and one home exit required; interactive points are not enabled.");
            Spawn = Room.Points.Single(p => p.Kind == "spawn"); Exit = Room.Points.Single(p => p.Kind == "exit");
            // The compiler has validated the property structure. Only home exits are
            // consumed by this private fixture; no guessed cross-room transport.
            if (Exit.ExitTargetKind != "home")
                throw new InvalidOperationException("runtime-exit-scope: only a home exit is supported.");
            DungeonName = "dtmap" + Hash("DTMAPI.Internal.HeartMap\0" + definition.Id);
            SceneName = "dungeon_" + DungeonName;
            RoomName = "r" + Hash(definition.Id + "\0" + Room.Id);
            RoomId = SceneName + "." + RoomName;
        }
        private static string Hash(string value)
        {
            using var hash = SHA256.Create();
            return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", "").ToLowerInvariant().Substring(0, 24);
        }
    }

    internal sealed class MapCatalog
    {
        private readonly Dictionary<string, MapContent> maps = new Dictionary<string, MapContent>(StringComparer.Ordinal);
        private readonly HashSet<string> nativeNames = new HashSet<string>(StringComparer.Ordinal);
        internal IEnumerable<MapContent> Maps => maps.Values;
        internal bool TryAdd(string json, out MapContent? map, out string error)
        {
            map = null; error = "";
            try
            {
                Compilation result = MapCompiler.CompileForPrivateRuntime(json);
                if (!result.Success)
                {
                    error = string.Join("; ", result.Diagnostics.Take(8).Select(d => d.Code + " field=" + d.Field + " room=" + d.RoomId + " cell=" + (d.Cell.HasValue ? d.Cell.Value.X + "," + d.Cell.Value.Y : "-")));
                    return false;
                }
                var candidate = new MapContent(result.Map!);
                if (maps.ContainsKey(candidate.Definition.Id) || nativeNames.Contains(candidate.SceneName))
                    throw new InvalidOperationException("runtime-id-conflict: map/native name already reserved; original retained.");
                maps.Add(candidate.Definition.Id, candidate);
                nativeNames.Add(candidate.SceneName);
                map = candidate;
                return true;
            }
            catch (Exception ex) { error = ex.Message; return false; }
        }
    }
}
