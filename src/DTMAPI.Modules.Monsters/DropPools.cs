using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DTMAPI.MonsterContent;
using DolocTown;
using DolocTown.Config;
using DolocTown.Config.General;
using DolocTown.Config.Item;
using DolocTown.Config.Resource;
using HarmonyLib;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private readonly Dictionary<string, DropPoolRecord> dropPools = new Dictionary<string, DropPoolRecord>(StringComparer.Ordinal);
        private sealed class DropPoolRecord
        {
            internal readonly string Owner, Id, NativeId;
            internal readonly MonsterDropPool Input;
            internal MonsterContentState State = MonsterContentState.WaitingNative;
            internal MonsterDiagnostic? Diagnostic;
            internal long Generation;
            internal ItemSpawnInfo? Row;
            internal TbItemSpawn? Table;
            internal DropPoolRecord(string owner, MonsterDropPool pool)
            { Owner = owner; Id = owner + ":" + pool.LocalId; NativeId = NativeContentId("pool", owner, pool.LocalId); Input = pool.Copy(); }
            internal MonsterContentSnapshot Snapshot => new MonsterContentSnapshot(Id, State, Generation, Diagnostic);
        }
        private static string NativeContentId(string kind, string owner, string id) => "dtm_mc_" + kind + "_" + BitConverter.ToString(Encoding.UTF8.GetBytes(owner)).Replace("-", "").ToLowerInvariant() + "_" + BitConverter.ToString(Encoding.UTF8.GetBytes(id)).Replace("-", "").ToLowerInvariant();
        private static MonsterDiagnostic ContentError(string owner, string id, string code, string stage, string field, string message) =>
            new MonsterDiagnostic(code, message, owner, id, stage: stage, field: field, recovery: code == "RestartRequired" ? "Restart the game after resolving the ownership conflict." : "Correct the referenced content and restart changed packages.");

        public MonsterResult<MonsterContentLease> RegisterDropPool(string owner, MonsterDropPool pool)
        {
            RequireThread();
            var error = ContentValidation.Pool(owner, pool);
            if (error != null) return MonsterResult<MonsterContentLease>.Fail(error);
            if (nativeStateFaulted) return MonsterResult<MonsterContentLease>.Fail(ContentError(owner, "", "RestartRequired", "DropPool", "", "Native generation is faulted."));
            var record = new DropPoolRecord(owner, pool);
            if (dropPools.ContainsKey(record.Id)) return MonsterResult<MonsterContentLease>.Fail(ContentError(owner, record.Id, "DuplicateId", "DropPool", "localId", "Pool ID already registered in this process."));
            dropPools.Add(record.Id, record);
            PublishDropPool(record);
            if (record.Diagnostic != null) return MonsterResult<MonsterContentLease>.FailedOwned(record.Diagnostic, new MonsterContentLease(() => record.Snapshot, () => WithdrawDropPool(record)));
            return MonsterResult<MonsterContentLease>.Ok(new MonsterContentLease(() => record.Snapshot, () => WithdrawDropPool(record)));
        }
        private void PublishDropPool(DropPoolRecord record)
        {
            if (!Ready) return;
            record.Generation = generation;
            try
            {
                var admission = MonsterAdmission.Pool(record.Owner, record.Input, Catalog);
                if (admission != null) { record.Diagnostic = admission; record.State = MonsterContentState.Rejected; return; }
                var table = DolocConfig.Tables.TbItemSpawn;
                if (table.DataMap.ContainsKey(record.NativeId)) throw new InvalidOperationException("Native pool identity is occupied.");
                var row = new ItemSpawnInfo(record.NativeId, record.Input.Entries.Select(e => new ItemSpawnData(e.Weight, e.MinCount, e.MaxCount ?? 0, e.ItemId)).ToList());
                row.Resolve(new Dictionary<string, object> { ["Item.TbItem"] = DolocConfig.Tables.TbItem });
                var error = ValidateDropPool(row, record.Owner, record.Id);
                if (error != null) { record.Diagnostic = error; record.State = MonsterContentState.Rejected; return; }
                record.Table = table; record.Row = row;
                Fault("DropPool.Table"); table.DataMap.Add(record.NativeId, row);
                Fault("DropPool.List"); table.DataList.Add(row);
                record.State = MonsterContentState.Ready; record.Diagnostic = null;
                Log("DropPool published " + record.Id);
            }
            catch (Exception ex)
            {
                try { RemoveDropPoolRow(record); record.State = MonsterContentState.Rejected; }
                catch (Exception cleanup) { record.State = MonsterContentState.RestartRequired; ex = new InvalidOperationException(ex.Message + "; cleanup=" + cleanup.Message); }
                record.Diagnostic = ContentError(record.Owner, record.Id, record.State == MonsterContentState.RestartRequired ? "RestartRequired" : "RegistrationFailed", "DropPool.Publish", "pool", ex.Message);
            }
        }
        private MonsterDiagnostic? ValidateDropPool(ItemSpawnInfo row, string owner, string id, int? maximumDraws = null)
        {
            if (row.SpawnDatas == null || row.SpawnDataList == null) return ContentError(owner, id, "ResourceMissing", "DropPool.Validate", "drops.pool", "Pool is not resolved.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < row.SpawnDatas.Count; i++)
            {
                var entry = row.SpawnDatas[i]; string path = "drops.entries[" + i + "]";
                if (entry == null || string.IsNullOrWhiteSpace(entry.ItemName) || !ids.Add(entry.ItemName) || !ContentValidation.Finite(entry.SpawnWeight) || entry.SpawnWeight < 0 || entry.MinCount < 0)
                    return ContentError(owner, id, "InvalidDropPool", "DropPool.Validate", path, "Malformed native drop entry.");
                // Native ceiling shortcuts, minima and guarantee candidates can consume zero
                // weight entries. Only an actually inert reference may omit a resource.
                var guarantee = DolocConfig.Tables.TbGlobalGuaranteed.Get(GuaranteedType.Monster, entry.ItemName);
                bool consumed = guarantee?.GuaranteeThreshold > 0 || (maximumDraws ?? int.MaxValue) > 0 &&
                    (entry.SpawnWeight > 0 || entry.MinCount > 0 || !entry.Unlimited && (!maximumDraws.HasValue || maximumDraws.Value >= row.Ceiling));
                if (!consumed) continue;
                var item = DolocConfig.Tables.TbItem.GetOrDefault(entry.ItemName);
                if (item == null || !ReferenceEquals(item, entry.ItemName_Ref)) return ContentError(owner, id, "ItemMissing", "DropPool.Validate", path + ".itemId", "Missing or stale effective item: " + entry.ItemName);
                if (guarantee != null && !ReferenceEquals(guarantee.SpawnId_Ref, item)) return ContentError(owner, id, "GuaranteedReferenceInvalid", "DropPool.Validate", path + ".guarantee", "Guarantee references a missing/stale item: " + entry.ItemName);
            }
            if (!ContentValidation.Finite(row.SpawnDatas.Sum(e => e.SpawnWeight)) || row.SpawnDatas.Sum(e => (long)e.MinCount) >= int.MaxValue ||
                !row.SpawnDatas.Any(e => e.Unlimited) && row.SpawnDatas.Sum(e => (long)e.MaxCount) >= int.MaxValue)
                return ContentError(owner, id, "InvalidDropPool", "DropPool.Validate", "drops.entries", "Native weight/count accumulation would overflow.");
            return null;
        }
        private MonsterDiagnostic? ResolveDropEntry(MonsterDrops input, ItemSpawnEntry? inherited, string owner, string id, out ItemSpawnEntry? entry)
        {
            entry = inherited;
            ItemSpawnInfo? pool = inherited?.SpawnLut_Ref;
            if (input.Entries != null)
            {
                pool = new ItemSpawnInfo("dtm_mc_private_drops", input.Entries.Select(e => new ItemSpawnData(e.Weight, e.MinCount, e.MaxCount ?? 0, e.ItemId)).ToList());
                pool.Resolve(new Dictionary<string, object> { ["Item.TbItem"] = DolocConfig.Tables.TbItem });
            }
            else if (input.ReferencesPool)
            {
                if (input.Source == MonsterPoolSource.Managed)
                {
                    if (!dropPools.TryGetValue(input.PoolId, out var record) || record.State != MonsterContentState.Ready)
                        return ContentError(owner, id, "DropPoolUnavailable", "DropPool.Resolve", "drops.poolId", "Register a ready managed pool before its consumers.");
                    pool = record.Row;
                }
                else
                {
                    if (dropPools.Values.Any(p => p.NativeId == input.PoolId)) return ContentError(owner, id, "DropPoolDependencyRequired", "DropPool.Resolve", "drops.poolId", "Use the owner-qualified managed reference.");
                    pool = DolocConfig.Tables.TbItemSpawn.GetOrDefault(input.PoolId);
                }
            }
            if (pool == null)
            {
                if (input.Mode != MonsterDropMode.Inherit || inherited != null && !string.IsNullOrEmpty(inherited.SpawnLut))
                    return ContentError(owner, id, "DropPoolMissing", "DropPool.Resolve", "drops.poolId", "Drop pool is unresolved.");
                return null;
            }
            int min = input.MinCount ?? inherited?.CountRange?.MinCount ?? 1;
            int max = input.MaxCount ?? inherited?.CountRange?.MaxCount ?? min;
            var error = ValidateDropPool(pool, owner, id, max); if (error != null) return error;
            entry = new ItemSpawnEntry(pool.Id, new RangeInt(min, max));
            NativeAccess.SetProperty(entry, "SpawnLut_Ref", pool);
            return null;
        }
        private void RemoveDropPoolRow(DropPoolRecord record)
        {
            if (record.Table == null || record.Row == null) return;
            if (record.Table.DataMap.TryGetValue(record.NativeId, out var row) && !ReferenceEquals(row, record.Row)) throw new NativeIdentityConflictException("Drop pool ownership changed; retaining pending registration.");
            Fault("DropPool.Withdraw");
            if (row != null) record.Table.DataMap.Remove(record.NativeId);
            record.Table.DataList.Remove(record.Row);
            record.Row = null; record.Table = null;
        }
        private void WithdrawDropPool(DropPoolRecord record)
        {
            RequireThread();
            if (record.State == MonsterContentState.Withdrawn) return;
            record.State = MonsterContentState.CleanupPending;
            definitions.RemoveDropPool(record.Id); nativeOverrides.RemoveDropPool(record.Id);
            try
            {
                if (definitions.Records.Any(d => d.Definition.Drops.ReferencesPool && d.Definition.Drops.Source == MonsterPoolSource.Managed && d.Definition.Drops.PoolId == record.Id && d.State != MonsterDefinitionState.Withdrawn))
                    throw new InvalidOperationException("Dependent definition cleanup is incomplete.");
                RemoveDropPoolRow(record); record.State = MonsterContentState.Withdrawn; record.Diagnostic = null;
                if (dropPools.TryGetValue(record.Id, out var current) && ReferenceEquals(current, record)) dropPools.Remove(record.Id);
            }
            catch (Exception ex) { record.Diagnostic = ContentError(record.Owner, record.Id, ex is NativeIdentityConflictException ? "RestartRequired" : "CleanupPending", "DropPool.Withdraw", "pool", ex.Message); throw; }
        }
        private void RetireDropPools()
        {
            foreach (var pool in dropPools.Values.Where(p => p.State == MonsterContentState.Ready).ToArray())
            { RemoveDropPoolRow(pool); pool.State = MonsterContentState.WaitingNative; }
        }
        private void RefreshDropPools()
        { foreach (var pool in dropPools.Values.Where(p => p.State == MonsterContentState.WaitingNative).ToArray()) PublishDropPool(pool); }
        private static bool OwnedDropPrefix(Monster monster)
        {
            var module = current;
            if (module == null || monster == null || !module.instances.TryGetValue(monster, out var scope)) return true;
            var pool = monster.proto.DropSpawnEntry?.SpawnLut_Ref;
            if (pool == null) return true;
            // Delegate eligibility to the native guarantee owner. An empty filtered array would
            // otherwise reach array[0] in ISpawnLut.SpawnInternal when Ceiling is still positive.
            var manager = DolocAPI.archiveHandle.dungeonData.guaranteedManager;
            var predicate = (Func<string, bool>)Delegate.CreateDelegate(typeof(Func<string, bool>), manager, AccessTools.Method(typeof(GuaranteedManager), "IsValidMonsterDropId"));
            if (pool.SpawnDatas.Any(e => predicate(e.ItemName))) return true;
            module.Log("DropPool empty eligible set definition=" + scope.Binding.Definition.Id);
            return false;
        }
    }
}
