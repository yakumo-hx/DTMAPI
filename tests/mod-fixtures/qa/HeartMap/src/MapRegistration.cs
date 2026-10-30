using System;
using System.Collections.Generic;
using System.Linq;
using DolocTown;
using DolocTown.Config;
using DolocTown.Config.Monster;
using DolocTown.Config.Resource;
using DolocTown.Config.Room;
using DolocTown.GameData;
using UnityEngine;
using SceneInfo = DolocTown.SceneInfo;
using RangeInt = DolocTown.Config.General.RangeInt;

namespace DTMAPI.Internal.HeartMap
{
    internal sealed class MapRegistration
    {
        internal readonly MapContent Content;
        internal readonly SceneInfo SceneInfo;
        internal MapScene? Scene;
        internal bool Quarantined;
        private DungeonDatabase? database;
        private DungeonProto? prototype;
        private Tables? tables;
        private RoomInfo? roomRow;
        private DolocTown.Config.Room.SceneInfo? sceneRow;
        internal bool Registered => ReferenceEquals(database, DolocAPI.assets?.dungeons) && ReferenceEquals(tables, DolocConfig.Tables) && prototype != null &&
            ReferenceEquals(tables?.TbRoom.GetOrDefault(Content.RoomId), roomRow) && roomRow != null &&
            ReferenceEquals(tables?.TbScene.GetOrDefault(Content.SceneName), sceneRow) && sceneRow != null &&
            database!.QueryDungeonProto(Content.DungeonName, out var found) && ReferenceEquals(found, prototype);
        internal Vector2 Spawn => new Vector2((float)Content.Spawn.WorldX, (float)Content.Spawn.WorldY);
        internal Vector2 Exit => new Vector2((float)Content.Exit.WorldX, (float)Content.Exit.WorldY);
        internal MapRegistration(MapContent content)
        { Content = content; SceneInfo = new SceneInfo(-1, content.SceneName, content.DungeonName, SceneType.DUNGEON, ""); }

        internal void Register(int injectAfterWrite = 0)
        {
            var nextTables = DolocConfig.Tables;
            var nextDatabase = DolocAPI.assets?.dungeons;
            if (nextTables == null || nextDatabase == null) return;
            if (DolocAPI.sceneManager != null && DolocAPI.sceneManager.QuerySceneBuildIndex(Content.SceneName, out _))
                throw new InvalidOperationException("native-built-scene-conflict " + Content.SceneName);
            bool hasProto = nextDatabase.QueryDungeonProto(Content.DungeonName, out var existingProto);
            bool hasRoom = nextTables.TbRoom.DataMap.TryGetValue(Content.RoomId, out var existingRoom);
            bool hasScene = nextTables.TbScene.DataMap.TryGetValue(Content.SceneName, out var existingScene);
            if ((hasProto && (!ReferenceEquals(nextDatabase, database) || !ReferenceEquals(existingProto, prototype))) ||
                (hasRoom && !ReferenceEquals(existingRoom, roomRow)) || (hasScene && !ReferenceEquals(existingScene, sceneRow)))
                throw new InvalidOperationException("native-id-conflict " + Content.Definition.Id + "; foreign records retained");
            if (hasProto && hasRoom && hasScene && ReferenceEquals(nextTables, tables)) return;

            var data = Content.Room;
            var size = new Vector2(data.Width, data.Height) * MapContent.CellSize;
            var empty = new TerrainConstraintProto(); var noResources = new ResourceGenInfoProto(empty, false);
            Vector2Int[] Cells(IReadOnlyList<int> cells) => cells.Select(i => new Vector2Int(i % data.Width, i / data.Width)).ToArray();
            var geometry = new RoomGeometry(Vector2.zero, size, Vector4.zero, Vector2.zero, size,
                Vector2Int.zero, new Vector2Int(data.Width, data.Height), Spawn, Cells(data.Solid), Cells(data.Ground), Array.Empty<Vector2Int>(), Cells(data.Platform));
            var room = new RoomProto(Content.RoomName, RoomType.Dungeon, SceneInfo, geometry, false, false, Array.Empty<Vector2>(), 9,
                default, null!, Array.Empty<RoomPresetObjectProto>(), new MonsterGenInfoProto(null!, null!, null!, false),
                noResources, noResources, noResources, empty, Array.Empty<string>());
            var candidate = hasProto ? existingProto : new DungeonProto(Content.DungeonName, SceneInfo, new[] { room }, Content.RoomName, null!);
            var count = new RangeInt(0, 0);
            var spawns = new RoomSpawnInfo(new ResourceSpawnEntry("", count), new VegetationSpawnEntry("", count), new MonsterSpawnEntry("", count), new EnvObjectSpawnEntry("", count));
            var originalRoom = nextTables.TbRoom.DataList.First(r => !r.IsInhouse);
            var row = new RoomInfo(Content.RoomId, Content.SceneName, originalRoom.SeasonGroupId, false, spawns, new RoomConstructInfo(false, false, true, false, false, false), true);
            var originalScene = nextTables.TbScene.DataList.First(s => s.HighLevel_Ref != null && s.MapId_Ref != null);
            var scene = new DolocTown.Config.Room.SceneInfo(Content.SceneName, originalScene.HighLevel, originalScene.MapId, originalScene.BgmEnvironmentType, Content.Definition.Id, "内部地图 · 不保存");
            var resolve = new Dictionary<string, object>
            {
                ["Time.TbSeasonGroup"] = nextTables.TbSeasonGroup, ["Resource.TbResourceSpawn"] = nextTables.TbResourceSpawn,
                ["Resource.TbVegetationSpawn"] = nextTables.TbVegetationSpawn, ["Resource.TbEnvObjectSpawn"] = nextTables.TbEnvObjectSpawn,
                ["Monster.TbMonsterSpawn"] = nextTables.TbMonsterSpawn, ["Room.TbBackgroundHighLevel"] = nextTables.TbBackgroundHighLevel,
                ["Room.TbMapType"] = nextTables.TbMapType
            };
            row.Resolve(resolve); scene.Resolve(resolve);
            // Each publish is transactional. AddDungeonProto is the final commit;
            // it has no remove API, so no fallible publication follows a successful add.
            using (var transaction = new MapPublication())
            {
                if (!hasRoom)
                {
                    transaction.Apply(() => nextTables.TbRoom.DataMap.Add(Content.RoomId, row), () => RemoveRow(nextTables, row, null));
                    transaction.Apply(() => nextTables.TbRoom.DataList.Add(row), () => nextTables.TbRoom.DataList.Remove(row));
                }
                if (injectAfterWrite == 1) throw new InvalidOperationException("MAP-INJECTED: registration after room");
                if (!hasScene)
                {
                    transaction.Apply(() => nextTables.TbScene.DataMap.Add(Content.SceneName, scene), () => RemoveRow(nextTables, null, scene));
                    transaction.Apply(() => nextTables.TbScene.DataList.Add(scene), () => nextTables.TbScene.DataList.Remove(scene));
                }
                if (injectAfterWrite == 2) throw new InvalidOperationException("MAP-INJECTED: registration after scene");
                if (!hasProto && !nextDatabase.AddDungeonProto(candidate)) throw new InvalidOperationException("native-prototype-conflict");
                transaction.Commit();
            }
            if (!ReferenceEquals(tables, nextTables)) RemoveOwnedRows();
            database = nextDatabase; prototype = candidate; tables = nextTables;
            roomRow = hasRoom ? existingRoom : row; sceneRow = hasScene ? existingScene : scene;
        }

        internal void RemoveOwnedRows() { if (tables != null) RemoveRow(tables, roomRow, sceneRow); tables = null; roomRow = null; sceneRow = null; }
        private static void RemoveRow(Tables owner, RoomInfo? room, DolocTown.Config.Room.SceneInfo? scene)
        {
            if (room != null && ReferenceEquals(owner.TbRoom.GetOrDefault(room.Id), room)) { owner.TbRoom.DataMap.Remove(room.Id); owner.TbRoom.DataList.Remove(room); }
            if (scene != null && ReferenceEquals(owner.TbScene.GetOrDefault(scene.Id), scene)) { owner.TbScene.DataMap.Remove(scene.Id); owner.TbScene.DataList.Remove(scene); }
        }
    }
}
