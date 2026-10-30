using System;
using System.Collections.Generic;
using System.Linq;

namespace DTMAPI.MonsterContent
{
    public enum MonsterPositionPolicy { Native, Exact }
    public enum MonsterDropMode { Inherit, Append, Replace }
    public enum MonsterPoolSource { Effective, Managed }
    public enum MonsterContentState { WaitingNative, Ready, Rejected, Withdrawn, RestartRequired, CleanupPending }

    public sealed class MonsterDrops
    {
        public MonsterDropMode Mode { get; set; }
        public MonsterPoolSource Source { get; set; }
        public string PoolId { get; set; } = "";
        public int? MinCount { get; set; }
        public int? MaxCount { get; set; }
        public List<MonsterDropEntry>? Entries { get; set; }
        internal bool ReferencesPool => Mode != MonsterDropMode.Inherit && Entries == null;
        internal MonsterDrops Copy() { var copy = (MonsterDrops)MemberwiseClone(); copy.Entries = Entries?.Select(e => e.Copy()).ToList(); return copy; }
    }
    public sealed class MonsterDropEntry
    {
        public string ItemId { get; set; } = "";
        public float Weight { get; set; } = 1;
        public int MinCount { get; set; }
        /// <summary>Null means the native unlimited per-item ceiling. Zero is not a disable flag.</summary>
        public int? MaxCount { get; set; }
        internal MonsterDropEntry Copy() => (MonsterDropEntry)MemberwiseClone();
    }
    public sealed class MonsterDropPool
    {
        public string LocalId { get; set; } = "";
        public List<MonsterDropEntry> Entries { get; set; } = new List<MonsterDropEntry>();
        internal MonsterDropPool Copy() => new MonsterDropPool { LocalId = LocalId, Entries = Entries.Select(e => e.Copy()).ToList() };
    }
    public sealed class MonsterSpawnRule
    {
        public string LocalId { get; set; } = "";
        public string DefinitionId { get; set; } = "";
        public string RoomId { get; set; } = "";
        /// <summary>Empty selects all matching native spawn groups; otherwise use an ID from GetCatalog.</summary>
        public string RegionId { get; set; } = "";
        public int Count { get; set; } = 1;
        internal MonsterSpawnRule Copy() => (MonsterSpawnRule)MemberwiseClone();
    }
    public sealed class MonsterContentSnapshot
    {
        public MonsterContentSnapshot(string id, MonsterContentState state, long generation, MonsterDiagnostic? diagnostic = null, int spawnedCount = 0)
        { Id = id; State = state; Generation = generation; Diagnostic = diagnostic; SpawnedCount = spawnedCount; }
        public string Id { get; }
        public MonsterContentState State { get; }
        /// <summary>Pool: native directory generation. Rule: last observed room-list generation.</summary>
        public long Generation { get; }
        public MonsterDiagnostic? Diagnostic { get; }
        public int SpawnedCount { get; }
    }
    public sealed class MonsterContentLease : IDisposable
    {
        private readonly Func<MonsterContentSnapshot> snapshot;
        private readonly MonsterLease lease;
        internal MonsterContentLease(Func<MonsterContentSnapshot> snapshot, Action release)
        {
            this.snapshot = snapshot;
            lease = new MonsterLease(() =>
            {
                release();
                var result = snapshot();
                if (result.State != MonsterContentState.Withdrawn)
                    throw new InvalidOperationException("Content cleanup is incomplete: " + result.Id + "; " + result.State + "; " + result.Diagnostic?.Message);
            });
        }
        public MonsterContentSnapshot Snapshot => snapshot();
        public void Dispose() => lease.Dispose();
    }
    public sealed class MonsterPlacement
    {
        public MonsterPlacement(float requestedX, float requestedY, float offsetX, float offsetY, float resolvedX, float resolvedY, float actualX, float actualY, string reason)
        { RequestedX = requestedX; RequestedY = requestedY; OffsetX = offsetX; OffsetY = offsetY; ResolvedX = resolvedX; ResolvedY = resolvedY; ActualX = actualX; ActualY = actualY; Reason = reason; }
        public float RequestedX { get; }
        public float RequestedY { get; }
        public float OffsetX { get; }
        public float OffsetY { get; }
        public float ResolvedX { get; }
        public float ResolvedY { get; }
        public float ActualX { get; }
        public float ActualY { get; }
        public string Reason { get; }
    }
    public sealed class MonsterCatalogEntry
    {
        public MonsterCatalogEntry(string id, string kind, string source, string version, string fingerprint, IEnumerable<MonsterDiagnostic>? diagnostics = null)
        { Id = id; Kind = kind; Source = source; Version = version; Fingerprint = fingerprint; Diagnostics = Array.AsReadOnly((diagnostics ?? Array.Empty<MonsterDiagnostic>()).ToArray()); }
        public string Id { get; }
        public string Kind { get; }
        public string Source { get; }
        public string Version { get; }
        public string Fingerprint { get; }
        public bool CanUse => Diagnostics.Count == 0;
        public IReadOnlyList<MonsterDiagnostic> Diagnostics { get; }
    }
    public sealed class MonsterDropEntryInfo
    {
        public MonsterDropEntryInfo(string itemId, float weight, int minCount, int? maxCount)
        { ItemId = itemId; Weight = weight; MinCount = minCount; MaxCount = maxCount; }
        public string ItemId { get; }
        public float Weight { get; }
        public int MinCount { get; }
        public int? MaxCount { get; }
    }
    public sealed class MonsterDropPoolInfo
    {
        public MonsterDropPoolInfo(MonsterCatalogEntry reference, MonsterPoolSource source, IEnumerable<MonsterDropEntryInfo> entries)
        { Reference = reference; Source = source; Entries = Array.AsReadOnly(entries.ToArray()); }
        public MonsterCatalogEntry Reference { get; }
        public MonsterPoolSource Source { get; }
        public IReadOnlyList<MonsterDropEntryInfo> Entries { get; }
    }
    public sealed class MonsterSpawnRegion
    {
        public MonsterSpawnRegion(string id, string generationType, int capacity)
        { Id = id; GenerationType = generationType; Capacity = capacity; }
        public string Id { get; }
        public string GenerationType { get; }
        public int Capacity { get; }
    }
    public sealed class MonsterRoomInfo
    {
        public MonsterRoomInfo(MonsterCatalogEntry reference, IEnumerable<MonsterSpawnRegion> regions, bool hostAvailable)
        { Reference = reference; Regions = Array.AsReadOnly(regions.ToArray()); HostAvailable = hostAvailable; }
        public MonsterCatalogEntry Reference { get; }
        public IReadOnlyList<MonsterSpawnRegion> Regions { get; }
        public bool HostAvailable { get; }
    }
    public sealed class MonsterContentCatalog
    {
        public MonsterContentCatalog(long generation, string gameVersion, string moduleVersion, string fingerprint, IEnumerable<MonsterTemplate> templates,
            IEnumerable<MonsterCatalogEntry> items, IEnumerable<MonsterDropPoolInfo> pools, IEnumerable<MonsterRoomInfo> rooms)
        { Generation = generation; GameVersion = gameVersion; ModuleVersion = moduleVersion; Fingerprint = fingerprint;
            Templates = Array.AsReadOnly(templates.ToArray()); Items = Array.AsReadOnly(items.ToArray()); DropPools = Array.AsReadOnly(pools.ToArray()); Rooms = Array.AsReadOnly(rooms.ToArray()); }
        public long Generation { get; }
        public string GameVersion { get; }
        public string ModuleVersion { get; }
        public string Fingerprint { get; }
        public IReadOnlyList<MonsterTemplate> Templates { get; }
        public IReadOnlyList<MonsterCatalogEntry> Items { get; }
        public IReadOnlyList<MonsterDropPoolInfo> DropPools { get; }
        public IReadOnlyList<MonsterRoomInfo> Rooms { get; }
        public IReadOnlyList<MonsterBehaviorInfo> Behaviors { get; internal set; } = Array.Empty<MonsterBehaviorInfo>();
        public MonsterCatalogObservation Observation { get; internal set; } = new MonsterCatalogObservation(null, false);
    }

    internal static class ContentValidation
    {
        internal static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        internal static MonsterDiagnostic? Drops(string owner, MonsterDrops? value)
        {
            string field = value == null ? "drops" : !Enum.IsDefined(typeof(MonsterDropMode), value.Mode) ? "drops.mode" :
                !Enum.IsDefined(typeof(MonsterPoolSource), value.Source) ? "drops.source" :
                value.ReferencesPool && (string.IsNullOrWhiteSpace(value.PoolId) || (value.Source == MonsterPoolSource.Managed && !Hosting.MonsterValidation.ValidQualifiedId(value.PoolId))) ? "drops.poolId" :
                value.MinCount.HasValue != value.MaxCount.HasValue || value.MinCount < 0 || value.MaxCount < value.MinCount || value.MaxCount == int.MaxValue ? "drops.countRange" : "";
            if (field.Length != 0) return Error(owner, "InvalidDrops", field);
            if (value!.Mode == MonsterDropMode.Inherit && (value.Entries != null || value.PoolId.Length != 0)) return Error(owner, "InvalidDrops", "drops");
            if (value.Entries != null)
            {
                if (value.PoolId.Length != 0) return Error(owner, "InvalidDrops", "drops.poolId");
                return Pool(owner, new MonsterDropPool { LocalId = "inline", Entries = value.Entries });
            }
            return null;
        }
        internal static MonsterDiagnostic? Pool(string owner, MonsterDropPool? value)
        {
            if (value == null || !Hosting.MonsterValidation.ValidId(value.LocalId)) return Error(owner, "InvalidDropPool", "localId");
            if (value.Entries == null || value.Entries.Count == 0) return Error(owner, "InvalidDropPool", "entries");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < value.Entries.Count; i++)
            {
                var entry = value.Entries[i]; string path = "entries[" + i + "]";
                if (entry == null) return Error(owner, "InvalidDropPool", path);
                if (string.IsNullOrWhiteSpace(entry.ItemId) || !ids.Add(entry.ItemId)) return Error(owner, "InvalidDropPool", path + ".itemId");
                if (!Finite(entry.Weight) || entry.Weight < 0) return Error(owner, "InvalidDropPool", path + ".weight");
                if (entry.MinCount < 0 || (entry.MaxCount.HasValue && (entry.MaxCount <= 0 || entry.MaxCount < entry.MinCount))) return Error(owner, "InvalidDropPool", path + ".countRange");
            }
            if (value.Entries.Where(e => e.MaxCount.HasValue).Sum(e => (long)e.MaxCount!.Value) >= int.MaxValue) return Error(owner, "InvalidDropPool", "entries.countRange");
            if (value.Entries.Sum(e => (long)e.MinCount) >= int.MaxValue) return Error(owner, "InvalidDropPool", "entries.countRange");
            if (!Finite(value.Entries.Sum(e => e.Weight))) return Error(owner, "InvalidDropPool", "entries.weight");
            return null;
        }
        internal static MonsterDiagnostic? Rule(string owner, MonsterSpawnRule? value)
        {
            string field = value == null ? "rule" : !Hosting.MonsterValidation.ValidId(value.LocalId) ? "localId" :
                !Hosting.MonsterValidation.ValidQualifiedId(value.DefinitionId) ? "definitionId" : string.IsNullOrWhiteSpace(value.RoomId) ? "roomId" :
                value.RegionId == null ? "regionId" : value.Count <= 0 ? "count" : "";
            return field.Length == 0 ? null : Error(owner, "InvalidSpawnRule", field);
        }
        private static MonsterDiagnostic Error(string owner, string code, string field) => new MonsterDiagnostic(code, "Invalid content field: " + field, owner, stage: "Validate", field: field);
    }

    // Independent of WorldEpoch: leaving a room does not rebuild its logical list.

}
