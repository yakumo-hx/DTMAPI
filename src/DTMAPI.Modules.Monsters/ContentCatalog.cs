using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using DTMAPI.MonsterContent;
using DolocTown;
using DolocTown.Config;
using DolocTown.Config.Monster;
using DolocTown.Config.Item;
using DTMAPI.MonsterContent.Hosting;
using DolocTown.GameData;
using UnityEngine;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private const string EffectiveSource = "NativeMergedTables (individual mod provenance unavailable)";
        private static string Fingerprint(string value)
        { using var hash = SHA256.Create(); return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", "").ToLowerInvariant(); }
        private static string Number(float value) => value.ToString("R", CultureInfo.InvariantCulture);
        private static string PointIdentity(IEnumerable<Vector2> points) => string.Join(";", points.Select(p => Number(p.x) + "," + Number(p.y)));
        private static string RegionId(MonsterGenType type, SpawnPoint point) => type + "/" + point.Index + "@" + Fingerprint(PointIdentity(point.Positions)).Substring(0, 12);
        private static IEnumerable<(MonsterGenType Type, SpawnPoint Point)> RegionPoints(MonsterGenInfoProto info)
        {
            foreach (var pair in new[] { (MonsterGenType.Air, info.airSpawnGroup), (MonsterGenType.Ground, info.groundSpawnGroup), (MonsterGenType.Surface, info.surfaceSpawnGroup) })
                foreach (var point in pair.Item2.SpawnPoints ?? Array.Empty<SpawnPoint>())
                    if (point.IsValid) yield return (pair.Item1, point);
        }
        private Dictionary<string, RoomProto> RoomPrototypes()
        {
            var result = new Dictionary<string, RoomProto>(StringComparer.Ordinal);
            foreach (var room in DolocAPI.assets.cityRooms?.AllRooms ?? Array.Empty<RoomProto>()) result[room.sceneInfo.name] = room;
            foreach (var dungeon in DolocAPI.assets.dungeons?.totalDungeons ?? Array.Empty<DungeonProto>())
                foreach (var room in dungeon.rooms) result[room.sceneInfo.name + "." + room.name] = room;
            foreach (var room in encounters.Keys) result[room.RoomId] = room.baseProto;
            if (context.Snapshot.IsWorldReady && DolocAPI.CurrentRoom != null) result[DolocAPI.CurrentRoom.RoomId] = DolocAPI.CurrentRoom.baseProto;
            return result;
        }
        private bool TryResolveRoom(string id, out Room? room)
        {
            room = null;
            if (!context.Snapshot.IsWorldReady || !context.Snapshot.SaveSessionEpoch.HasValue) return false;
            var archive = DolocAPI.archiveHandle;
            if (archive == null) return false;
            // Use the native save's read-only lookups, without QueryRoom's error log
            // for catalog-only prototypes. No encounter, render or creation side effect.
            if (id.StartsWith("city_", StringComparison.Ordinal))
            {
                if (archive.cityData.cityRooms.TryGetValue(id.Substring(5), out var city)) room = city;
            }
            else if (id.StartsWith("dungeon_", StringComparison.Ordinal))
            {
                if (archive.QueryDungeonRoom(id.Substring(8), out var dungeon)) room = dungeon;
            }
            else if (id.StartsWith("farm_", StringComparison.Ordinal))
            {
                room = archive.AllFarms.FirstOrDefault(f => f.RoomId == id);
                if (room == null && id.Contains(".")) archive.QueryTemplateRoomFromGlobal(id.Substring(id.LastIndexOf('.') + 1), out room);
            }
            return room != null && room.RoomId == id;
        }
        private MonsterDropPoolInfo DescribeDropPool(ItemSpawnInfo row)
        {
            var owned = dropPools.Values.FirstOrDefault(p => ReferenceEquals(p.Row, row));
            var error = owned != null && owned.State != MonsterContentState.Ready
                ? owned.Diagnostic ?? ContentError(owned.Owner, owned.Id, "DropPoolUnavailable", "Catalog", "pool", "The managed pool is closed or awaiting cleanup.")
                : ValidateDropPool(row, owned?.Owner ?? "", owned?.Id ?? row.Id);
            return new MonsterDropPoolInfo(new MonsterCatalogEntry(owned?.Id ?? row.Id, "DropPool", owned?.Owner ?? EffectiveSource,
                owned == null ? Application.version : ownerMetadata(owned.Owner)?.Version ?? "unknown",
                Fingerprint(row.ToString() + "|ceiling=" + row.Ceiling + "|guarantees=" + string.Join(";", row.SpawnDatas?.Where(e => e != null).Select(e => DolocConfig.Tables.TbGlobalGuaranteed.Get(DolocTown.Config.Resource.GuaranteedType.Monster, e.ItemName)?.ToString() ?? "") ?? Array.Empty<string>())), error == null ? null : new[] { error }),
                owned == null ? MonsterPoolSource.Effective : MonsterPoolSource.Managed,
                row.SpawnDatas?.Where(e => e != null).Select(e => new MonsterDropEntryInfo(e.ItemName, e.SpawnWeight, e.MinCount, e.Unlimited ? (int?)null : e.MaxCount)) ?? Array.Empty<MonsterDropEntryInfo>());
        }
        private MonsterDiagnostic? AdmitDefinition(DefinitionRecord record, Template template)
        {
            var input = record.Definition;
            var selected = template.Snapshot(generation, verified.Contains(template.Proto.Name));
            // Frame-file identity and mapping are required only by a skin here.
            // Public Templates/Catalog still produce complete, verified metadata.
            if (input.Appearance.Skin != null) selected.Presentation = PresentationInfo(template);
            MonsterBehaviorInfo? behavior = null;
            if (UsesControlledBehavior(input.Behavior) || input.NativeAttacks.Count > 0 || input.NativeParts.Count > 0) selected.Actions = DescribeActions(template);
            if (input.Behavior == "code-provider" && behaviors.TryGetValue(input.BehaviorId, out var registration))
            {
                behavior = new MonsterBehaviorInfo(registration.Id, ownerMetadata(registration.Owner)?.Version ?? "unknown", !registration.Closed, registration.Requirements);
            }
            MonsterDropPoolInfo? pool = null;
            if (input.Drops.ReferencesPool)
            {
                // Resolve through the current native table, not a cached lease row.
                // A replacement row loses managed ownership exactly as in Catalog.
                string nativeId = input.Drops.PoolId;
                if (input.Drops.Source == MonsterPoolSource.Managed)
                    nativeId = dropPools.TryGetValue(input.Drops.PoolId, out var managed) ? managed.NativeId : "";
                var row = DolocConfig.Tables.TbItemSpawn.GetOrDefault(nativeId);
                if (row != null) pool = DescribeDropPool(row);
            }
            var body = input;
            if (SoundValidation.HasSounds(input)) { body = input.Copy(); SoundValidation.ReleaseInputs(body); }
            return MonsterAdmission.Definition(record.Owner, body, selected, behavior, pool);
        }
        public MonsterContentCatalog Catalog
        {
            get
            {
                using var trace = OptimizationTrace.Begin(5);
                RequireThread();
                RefreshNativeOverrides();
                var part = OptimizationTrace.Begin(6);
                var items = DolocConfig.Tables.TbItem.DataList.OrderBy(r => r.Id, StringComparer.Ordinal).Select(r =>
                    new MonsterCatalogEntry(r.Id, "Item", EffectiveSource, Application.version, Fingerprint(r.ToString()))).ToArray();
                part.Dispose(); part = OptimizationTrace.Begin(7);
                var pools = DolocConfig.Tables.TbItemSpawn.DataList.OrderBy(r => r.Id, StringComparer.Ordinal).Select(DescribeDropPool).ToArray();
                part.Dispose(); part = OptimizationTrace.Begin(8);
                var rooms = RoomPrototypes().OrderBy(p => p.Key, StringComparer.Ordinal).Select(pair =>
                {
                    var regions = RegionPoints(pair.Value.monsterInfo).Select(p => new MonsterSpawnRegion(RegionId(p.Type, p.Point), p.Type.ToString(), p.Point.MaxCapacity)).ToArray();
                    string geometry = PointIdentity(pair.Value.geometry.obstacles.Select(p => (Vector2)p));
                    return new MonsterRoomInfo(new MonsterCatalogEntry(pair.Key, "Room", "NativeResources", Application.version, Fingerprint(pair.Key + geometry + string.Join(";", regions.Select(r => r.Id)))), regions,
                        TryResolveRoom(pair.Key, out _));
                }).ToArray();
                part.Dispose(); part = OptimizationTrace.Begin(9);
                var templateSnapshot = Templates;
                part.Dispose();
                string fingerprint = Fingerprint(string.Join("|", items.Select(i => i.Fingerprint).Concat(pools.Select(p => p.Reference.Fingerprint)).Concat(rooms.Select(r => r.Reference.Fingerprint)).Concat(templateSnapshot.Select(t => t.Reference?.Fingerprint + ":phasedNative=" + t.Actions.PhasedNativeActions + ":physical=" + PhysicalFingerprint(t.Physical) + ":" + t.Presentation.Fingerprint + ":" + string.Join(",", t.NativeOverrideFields) + ":attacks=" + string.Join(";", t.Actions.NativeAttacks.Select(NativeAttackFingerprint)) + ":parts=" + string.Join(";", t.Actions.NativeParts.Select(p => p.TemplateId + ":" + string.Join(";", p.NativeAttacks.Select(NativeAttackFingerprint))))))));
                return new MonsterContentCatalog(generation, Application.version, MonsterModule.ContractVersion, fingerprint, templateSnapshot, items, pools, rooms)
                {
                    Behaviors = Array.AsReadOnly(behaviors.Values.OrderBy(b => b.Id, StringComparer.Ordinal).Select(b => new MonsterBehaviorInfo(b.Id, ownerMetadata(b.Owner)?.Version ?? "unknown", !b.Closed, b.Requirements)).ToArray()),
                    Observation = new MonsterCatalogObservation(context.Snapshot.SaveSessionEpoch, context.Snapshot.IsWorldReady)
                };
            }
        }
    }
}
