using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Hosting;
using DolocTown;
using DolocTown.Config;
using DolocTown.Config.Monster;
using DolocTown.GameData;
using HarmonyLib;
using RedSaw.GameMap;
using UnityEngine;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private readonly Dictionary<string, SpawnRuleRecord> spawnRules = new Dictionary<string, SpawnRuleRecord>(StringComparer.Ordinal);
        private readonly Dictionary<Room, RoomEncounter> encounters = new Dictionary<Room, RoomEncounter>();
        private ConditionalWeakTable<Monster, SpawnSlotAssignment> nativeAssignments = new ConditionalWeakTable<Monster, SpawnSlotAssignment>();
        private readonly Dictionary<string, Queue<NativeAllocation>> pendingAllocations = new Dictionary<string, Queue<NativeAllocation>>(StringComparer.Ordinal);
        private ConditionalWeakTable<Monster, NativeAllocation> allocatingMonsters = new ConditionalWeakTable<Monster, NativeAllocation>();
        private sealed class NativeAllocation
        {
            internal readonly Vector2 Position;
            internal readonly SpawnSlotAssignment Claim;
            internal NativeAllocation(Vector2 position, SpawnSlotAssignment claim) { Position = position; Claim = claim; }
        }
        private sealed class SpawnRuleRecord
        {
            internal readonly string Owner, Id;
            internal readonly MonsterSpawnRule Input;
            internal MonsterContentState State = MonsterContentState.WaitingNative;
            internal MonsterDiagnostic? Diagnostic;
            internal long Generation;
            internal int Spawned;
            internal SpawnRuleRecord(string owner, MonsterSpawnRule input) { Owner = owner; Id = owner + ":" + input.LocalId; Input = input.Copy(); }
            internal MonsterContentSnapshot Snapshot => new MonsterContentSnapshot(Id, State, Generation, Diagnostic, Spawned);
        }
        private sealed class RoomEncounter
        {
            internal readonly Room Room;
            internal readonly MonsterManager Manager;
            internal readonly EncounterLedger Ledger = new EncounterLedger();
            internal readonly HashSet<string> Reservations = new HashSet<string>(StringComparer.Ordinal);
            internal readonly HashSet<Monster> Accounted = new HashSet<Monster>();
            internal readonly long? Session;
            internal readonly Dictionary<MonsterGenType, string> Uncertain = new Dictionary<MonsterGenType, string>();
            internal bool Applying;
            internal RoomEncounter(Room room, long? session) { Room = room; Manager = room.DM_monster; Session = session; }
            internal void Cleared() { Ledger.ConfirmClear(); Reservations.Clear(); Accounted.Clear(); Uncertain.Clear(); }
        }
        private RoomEncounter Encounter(Room room)
        {
            if (!encounters.TryGetValue(room, out var state) || !ReferenceEquals(state.Manager, room.DM_monster) || state.Session != context.Snapshot.SaveSessionEpoch) encounters[room] = state = new RoomEncounter(room, context.Snapshot.SaveSessionEpoch);
            return state;
        }
        public MonsterResult<MonsterContentLease> RegisterSpawnRule(string owner, MonsterSpawnRule input)
        {
            RequireThread();
            var error = ContentValidation.Rule(owner, input);
            if (error != null) return MonsterResult<MonsterContentLease>.Fail(error);
            if (nativeStateFaulted) return MonsterResult<MonsterContentLease>.Fail(ContentError(owner, "", "RestartRequired", "Spawning", "", "Native generation is faulted."));
            var rule = new SpawnRuleRecord(owner, input);
            if (spawnRules.ContainsKey(rule.Id)) return MonsterResult<MonsterContentLease>.Fail(ContentError(owner, rule.Id, "DuplicateId", "Spawning", "localId", "Rule ID already registered in this process."));
            spawnRules.Add(rule.Id, rule);
            ValidateRule(rule);
            if (rule.State == MonsterContentState.Rejected) return MonsterResult<MonsterContentLease>.FailedOwned(rule.Diagnostic!, new MonsterContentLease(() => rule.Snapshot, () => WithdrawRule(rule)));
            RefreshRoomRules();
            // A run-time placement failure still returns its lease so authors can inspect/release it.
            return MonsterResult<MonsterContentLease>.Ok(new MonsterContentLease(() => rule.Snapshot, () => WithdrawRule(rule)));
        }
        private void ValidateRule(SpawnRuleRecord rule)
        {
            if (!Ready || rule.State == MonsterContentState.Withdrawn || rule.State == MonsterContentState.CleanupPending || rule.State == MonsterContentState.RestartRequired) return;
            var definition = definitions.Records.FirstOrDefault(d => d.Id == rule.Input.DefinitionId);
            MonsterDiagnostic? error = null;
            if (definition == null || definition.State == MonsterDefinitionState.Rejected || definition.State == MonsterDefinitionState.Withdrawn || definition.State == MonsterDefinitionState.RestartRequired)
                error = ContentError(rule.Owner, rule.Id, "DefinitionUnavailable", "Spawning.Validate", "definitionId", "Definition is missing or unavailable.");
            else if (definition.State == MonsterDefinitionState.WaitingNative) return;
            else
            {
                var binding = bindings[definition.NativeId];
                error = MonsterAdmission.Rule(rule.Owner, rule.Input, binding.Template.Snapshot(generation, false), Catalog, binding.Definition.Definition.Body);
            }
            if (error != null || rule.Diagnostic?.Code != "SpawnRuleFailed") rule.Diagnostic = error;
            rule.State = error == null ? MonsterContentState.Ready : MonsterContentState.Rejected;
        }
        private void RefreshRoomRules()
        {
            if (!Ready || !context.Snapshot.IsWorldReady) return;
            foreach (var rule in spawnRules.Values.Where(r => r.State == MonsterContentState.WaitingNative).ToArray()) ValidateRule(rule);
            foreach (string id in spawnRules.Values.Where(r => r.State == MonsterContentState.Ready).Select(r => r.Input.RoomId).Distinct().ToArray())
            {
                if (TryResolveRoom(id, out var room)) ApplyRoomRules(room!);
                else foreach (var rule in spawnRules.Values.Where(r => r.Input.RoomId == id && r.State == MonsterContentState.Ready))
                    rule.Diagnostic = ContentError(rule.Owner, rule.Id, "RoomHostUnavailable", "Spawning.Resolve", "roomId", "Catalog room has no live host in this save session; waiting for its native host.");
            }
        }
        private static string SlotKey(MonsterGenType type, SpawnPoint point, int slot) => RegionId(type, point) + ":" + slot;
        private void SeedReservations(RoomEncounter encounter)
        {
            foreach (var monster in encounter.Manager.AllMonsters)
            {
                if (!encounter.Accounted.Add(monster)) continue;
                var regions = RegionPoints(encounter.Room.MonsterGenInfo).Where(p => p.Type == monster.proto.GenType).ToArray();
                if (nativeAssignments.TryGetValue(monster, out var assignment) && assignment.Diagnostic == null &&
                    assignment.Slots.All(key => regions.Any(r => Enumerable.Range(0, r.Point.MaxCapacity).Any(i => SlotKey(r.Type, r.Point, i) == key))))
                {
                    foreach (string key in assignment.Slots) encounter.Reservations.Add(key);
                    continue;
                }
                encounter.Uncertain[monster.proto.GenType] = assignment?.Diagnostic ?? "NativeSpawnAssignmentUnavailable: original allocation was not observed or its region changed; this GenType is conservatively occupied until a real rebuild.";
                foreach (var region in regions) for (int i = 0; i < region.Point.MaxCapacity; i++) encounter.Reservations.Add(SlotKey(region.Type, region.Point, i));
            }
        }
        private void ApplyRoomRules(Room room)
        {
            if (!Ready || !context.Snapshot.SaveSessionEpoch.HasValue || room.MonsterEnv == null) return;
            if (!spawnRules.Values.Any(r => r.Input.RoomId == room.RoomId && r.State == MonsterContentState.Ready)) return;
            var encounter = Encounter(room);
            if (!encounter.Ledger.CanSpawn)
            {
                if (encounter.Ledger.RecoveryPending && !encounter.Ledger.IsRebuilding)
                    foreach (var rule in spawnRules.Values.Where(r => r.Input.RoomId == room.RoomId && r.State == MonsterContentState.Ready))
                        rule.Diagnostic = ContentError(rule.Owner, rule.Id, "SpawnRebuildPending", "Spawning.Rebuild", "roomId", "Native list was cleared but regeneration failed; ordinary render/ready cannot resume it. A successful native rebuild is required.");
                return;
            }
            if (encounter.Applying) return;
            encounter.Applying = true;
            try
            {
                SeedReservations(encounter);
                foreach (var rule in spawnRules.Values.Where(r => r.Input.RoomId == room.RoomId && r.State == MonsterContentState.Ready).OrderBy(r => r.Id, StringComparer.Ordinal).ToArray())
                {
                    if (!encounter.Ledger.TryBegin(rule.Id)) continue;
                    rule.Generation = encounter.Ledger.Generation; rule.Spawned = 0; rule.Diagnostic = null;
                    var created = new List<InstanceScope>();
                    var reserved = new List<string>();
                    try
                    {
                        var definition = definitions.Records.FirstOrDefault(d => d.Id == rule.Input.DefinitionId && d.State == MonsterDefinitionState.Creatable);
                        if (definition == null || !bindings.TryGetValue(definition.NativeId, out var binding)) throw new InvalidOperationException("DefinitionUnavailable");
                        int volume = binding.Proto.Volume;
                        if (volume <= 0) throw new InvalidOperationException("SpawnVolumeInvalid");
                        var positions = new List<Vector2>();
                        string placementFailure = encounter.Uncertain.TryGetValue(binding.Proto.GenType, out var uncertainty) ? uncertainty : "No unreserved native group capacity remains.";
                        foreach (var region in RegionPoints(room.MonsterGenInfo).Where(p => p.Type == binding.Proto.GenType && (rule.Input.RegionId.Length == 0 || RegionId(p.Type, p.Point) == rule.Input.RegionId)))
                        {
                            for (int index = 0; index + volume <= region.Point.MaxCapacity && positions.Count < rule.Input.Count; index++)
                            {
                                if (Enumerable.Range(index, volume).Any(s => encounter.Reservations.Contains(SlotKey(region.Type, region.Point, s)))) continue;
                                Vector2 position = region.Point.Positions[index];
                                var positionError = PlacementError(room, binding, position + binding.Row.Offset, room.DM_monster.IsRunning);
                                if (positionError != null) { placementFailure = positionError; continue; }
                                positions.Add(position);
                                foreach (int slot in Enumerable.Range(index, volume)) { var key = SlotKey(region.Type, region.Point, slot); encounter.Reservations.Add(key); reserved.Add(key); }
                            }
                        }
                        if (positions.Count != rule.Input.Count) throw new InvalidOperationException("SpawnCapacityOrPositionUnavailable: " + placementFailure);
                        // Adjacent dungeon scenery may be rendered with includeMonster=false.
                        // Only the native monster manager identifies an active monster render.
                        foreach (var position in positions) { var scope = CreateOwned(binding, room, position, binding.Row.Offset, room.DM_monster.IsRunning, MonsterPositionPolicy.Native, 0.01f, rule.Id); created.Add(scope); encounter.Accounted.Add(scope.Logical!); }
                        rule.Spawned = created.Count;
                        Log("Encounter spawned rule=" + rule.Id + " room=" + room.RoomId + " listGeneration=" + rule.Generation + " count=" + rule.Spawned);
                    }
                    catch (Exception ex)
                    {
                        var errors = new List<Exception>();
                        foreach (var scope in created) Attempt(errors, () => Revoke(scope, true));
                        bool pending = errors.Count > 0 || instances.Values.Any(s => s.SpawnRuleId == rule.Id && !s.Removed);
                        if (!pending) foreach (var key in reserved) encounter.Reservations.Remove(key);
                        rule.State = pending ? MonsterContentState.RestartRequired : MonsterContentState.Ready;
                        rule.Diagnostic = ContentError(rule.Owner, rule.Id, pending ? "RestartRequired" : "SpawnRuleFailed", "Spawning.Create", "regionId", ex.Message + (errors.Count == 0 ? "" : "; cleanup=" + errors[0].Message));
                        Log("Encounter failure " + rule.Id + " " + rule.Diagnostic.Message);
                    }
                }
            }
            finally { encounter.Applying = false; }
        }
        private void WithdrawRule(SpawnRuleRecord rule)
        {
            RequireThread();
            if (rule.State == MonsterContentState.Withdrawn) return;
            rule.State = MonsterContentState.CleanupPending;
            try
            {
                RemoveInstances(s => s.SpawnRuleId == rule.Id);
                if (instances.Values.Any(s => s.SpawnRuleId == rule.Id && !s.Removed)) throw new InvalidOperationException("Rule cleanup is pending native ownership release.");
                rule.State = MonsterContentState.Withdrawn; rule.Diagnostic = null;
                if (spawnRules.TryGetValue(rule.Id, out var current) && ReferenceEquals(current, rule)) spawnRules.Remove(rule.Id);
            }
            catch (Exception ex) { rule.Diagnostic = ContentError(rule.Owner, rule.Id, "CleanupPending", "Spawning.Withdraw", "rule", ex.Message); throw; }
        }
        private void RemoveDefinitionRules(string definitionId)
        { foreach (var rule in spawnRules.Values.Where(r => r.Input.DefinitionId == definitionId && r.State != MonsterContentState.Withdrawn).ToArray()) WithdrawRule(rule); }
        private static Vector2 NativeLanding(IMonsterHost host, Binding binding, Vector2 position)
        { return binding.Proto.GenType == MonsterGenType.Ground && host.MonsterEnv.GroundMap.RaycastToGround(position, out var result) ? result : position; }
        private static bool NativePlacementCellIsClear(IGameMap map, MonsterGenType type, Vector2 position)
        {
            if (!map.IsObstacleOrInvalid(map.WorldToCell(position))) return true;
            if (type != MonsterGenType.Ground) return false;
            // RaycastToGround returns a bottom-center pivot. Native WorldToCell rounds
            // that half-cell X into the next column for odd columns; it is not an
            // inverse of CellToWorldPivot. Recognize only the exact native ground pivot.
            var relative = position - map.RoomPosition;
            var cell = new Vector2Int(Mathf.FloorToInt(relative.x * map.CellSizeReciprocal.x), Mathf.RoundToInt(relative.y * map.CellSizeReciprocal.y));
            return map.IsValidPosition(cell) && !map.IsObstacle(cell) && map.IsGround(cell)
                && Vector2.Distance(position, map.CellToWorldPivot(cell, new Vector2(.5f, 0))) <= .0001f;
        }
        private string? PlacementError(IMonsterHost host, Binding binding, Vector2 position, bool dynamic)
        {
            var map = host.MonsterEnv?.GetMap(binding.Proto.GenType);
            if (map == null || !NativePlacementCellIsClear(map, binding.Proto.GenType, position)) return "Input position is outside the template's native map or obstructed.";
            Vector2 resolved = NativeLanding(host, binding, position);
            if (!NativePlacementCellIsClear(map, binding.Proto.GenType, resolved)) return "Native resolved position is outside the valid map.";
            if (!BodyClear(host, binding, resolved)) return "The configured body overlaps the native map boundary or a wall.";
            if (dynamic && Physics2D.OverlapPointAll(resolved + Vector2.up * 0.05f).Any(c => !c.isTrigger && c.enabled)) return "Native resolved position is occupied by a solid collider.";
            return null;
        }
        private InstanceScope CreateOwned(Binding binding, IMonsterHost host, Vector2 requested, Vector2 offset, bool render, MonsterPositionPolicy policy, float tolerance, string ruleId)
        {
            using var trace = OptimizationTrace.Begin(44);
            if (creating != null) throw new InvalidOperationException("ReentrantSpawn");
            var scope = new InstanceScope(binding, host) { RequestedPosition = requested, SpawnOffset = offset, PositionPolicy = policy, PositionTolerance = tolerance, SpawnRuleId = ruleId };
            if (host is Room room && ruleId.Length > 0) scope.SpawnBatchGeneration = Encounter(room).Ledger.Generation;
            creating = scope;
            try
            {
                var position = requested + offset;
                scope.ResolvedPosition = NativeLanding(host, binding, position);
                if (policy == MonsterPositionPolicy.Exact && Vector2.Distance(requested, scope.ResolvedPosition) > tolerance) throw new InvalidOperationException("StrictPositionMismatch: native landing differs from requested coordinates.");
                using (OptimizationTrace.Begin(45)) scope.Logical = host.GenerateMonster(binding.Proto, position, render);
                if (scope.ActionFailure != null) throw new SpawnFailure(scope.ActionFailure);
                if (scope.Logical == null || !host.DM_monster.AllMonsters.Contains(scope.Logical) || !ReferenceEquals(scope.Logical.Host, host)) throw new InvalidOperationException("Logical creation postcondition failed.");
                if (render) { ValidateInstance(scope); scope.Initialized = true; scope.Active = true; verified.Add(binding.Template.Proto.Name); }
                return scope;
            }
            catch (Exception ex)
            {
                Revoke(scope, true);
                if (scope.ActionFailure != null) throw new SpawnFailure(scope.ActionFailure);
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex).Throw();
                throw;
            }
            finally { creating = null; }
        }
        private sealed class SpawnFailure : Exception
        {
            internal readonly MonsterDiagnostic Diagnostic;
            internal SpawnFailure(MonsterDiagnostic diagnostic) : base(diagnostic.Message) { Diagnostic = diagnostic; }
        }
        private void CompensateReentry(InstanceScope failed, Exception error)
        {
            var batch = failed.SpawnRuleId.Length == 0 ? new[] { failed } : instances.Values.Where(s => s.SpawnRuleId == failed.SpawnRuleId && s.SpawnBatchGeneration == failed.SpawnBatchGeneration && ReferenceEquals(s.Host, failed.Host)).ToArray();
            var errors = new List<Exception>();
            foreach (var scope in batch) Attempt(errors, () => Revoke(scope, true));
            bool pending = errors.Count > 0 || batch.Any(s => !s.Removed);
            if (spawnRules.TryGetValue(failed.SpawnRuleId, out var rule))
            {
                rule.State = pending ? MonsterContentState.RestartRequired : MonsterContentState.Ready;
                rule.Spawned = 0;
                rule.Diagnostic = ContentError(rule.Owner, rule.Id, pending ? "RestartRequired" : "SpawnRuleFailed", "Spawning.Render", "regionId", error.Message + (errors.Count == 0 ? "" : "; cleanup=" + errors[0].Message));
            }
            Log("Native re-entry compensated batch=" + failed.SpawnRuleId + " count=" + batch.Length + " pending=" + pending + " error=" + error.Message);
        }
        private void CheckPlacement(InstanceScope scope)
        {
            Vector2 actual = scope.Controller!.position2d;
            if (!ContentValidation.Finite(actual.x) || !ContentValidation.Finite(actual.y)) throw new InvalidOperationException("Non-finite native placement.");
            var map = scope.Host.MonsterEnv.GetMap(scope.Binding.Proto.GenType);
            if (!NativePlacementCellIsClear(map, scope.Binding.Proto.GenType, actual)) throw new InvalidOperationException("Native actual position is outside the valid map.");
            if (!BodyClear(scope.Host, scope.Binding, actual, scope.Controller.transform)) throw new InvalidOperationException("Configured body overlaps the native map boundary or a wall: definition=" + scope.Binding.Definition.Id + "; logical=" + scope.Logical!.position + "; actual=" + actual + ".");
            scope.PhysicalGroundPosition = actual;
            if (scope.SpawnRuleId.Length > 0 && Physics2D.OverlapPointAll(actual + Vector2.up * 0.05f).Any(c => !c.isTrigger && c.enabled && !c.transform.IsChildOf(scope.Controller.transform))) throw new InvalidOperationException("Native actual position is dynamically occupied.");
            if (scope.PositionPolicy == MonsterPositionPolicy.Exact && Vector2.Distance(actual, scope.RequestedPosition) > scope.PositionTolerance) throw new InvalidOperationException("StrictPositionMismatch");
            string reason = Vector2.Distance(scope.ResolvedPosition, actual) <= 0.01f ? "NativeLanding" : "NativeControllerAdjustment";
            scope.Placement = new MonsterPlacement(scope.RequestedPosition.x, scope.RequestedPosition.y, scope.SpawnOffset.x, scope.SpawnOffset.y, scope.ResolvedPosition.x, scope.ResolvedPosition.y, actual.x, actual.y, reason);
        }
        private void InstallSpawnHooks()
        {
            Patch(AccessTools.Method(typeof(Room), "__AfterNewGame"), postfix: nameof(RoomReady));
            Patch(AccessTools.Method(typeof(Room), "AfterLoadData"), postfix: nameof(RoomReady));
            Patch(AccessTools.Method(typeof(Room), "Render"), prefix: nameof(RoomRendering));
            Patch(AccessTools.Method(typeof(Room), "ReGenRoomDatas"), prefix: nameof(RoomRebuilding), finalizer: nameof(RoomRebuilt));
            Patch(AccessTools.Method(typeof(DungeonRoom), "ReGenRoomDatas"), prefix: nameof(RoomRebuilding), finalizer: nameof(RoomRebuilt));
            Patch(AccessTools.Method(typeof(DolocAPI), "Command_RefreshMonster"), prefix: nameof(CommandRebuilding), finalizer: nameof(CommandRebuilt));
            Patch(AccessTools.Method(typeof(MonsterGenInfoProto), "AssignMonstersToSpawnGroups"), postfix: nameof(NativeAllocated));
            Patch(AccessTools.PropertySetter(typeof(Monster), "Host"), postfix: nameof(NativeHostAssigned));
            Patch(AccessTools.Method(typeof(Room), "ClearRender"), prefix: nameof(RebuildClearRender));
            Patch(AccessTools.Method(typeof(GuaranteedManager), "SpawnMonsterDropItems"), prefix: nameof(OwnedDropPrefix));
            Patch(AccessTools.Method(typeof(Tables), "MergeModExtension"), postfix: nameof(ContentMerged));
        }
        private static void RoomReady(Room __instance, System.Reflection.MethodBase __originalMethod)
        {
            if (current == null) return;
            if (current.spawnRules.Values.Any(r => r.Input.RoomId == __instance.RoomId)) current.Log("Native room ready method=" + __originalMethod.Name + " room=" + __instance.RoomId);
            current.ApplyRoomRules(__instance);
        }
        private static void RoomRendering(Room __instance, bool includeMonster) { if (includeMonster) current?.ApplyRoomRules(__instance); }
        private static void RoomRebuilding(Room __instance)
        { current?.Encounter(__instance).Ledger.BeginRebuild(); }
        private static Exception? RoomRebuilt(Room __instance, bool __result, Exception? __exception)
            => CompleteRebuild(__instance, __exception, __result);
        private static Exception? CompleteRebuild(Room room, Exception? error, bool succeeded)
        {
            if (current != null)
            {
                var state = current.Encounter(room);
                state.Ledger.EndRebuild(error == null && succeeded);
                // DungeonRoom.Render and command RunMonsters may already have run. Only
                // now publish the batch, rendering it if the native manager is running.
                if (!state.Ledger.IsRebuilding && ((error == null && succeeded) || state.Ledger.RecoveryPending)) current.ApplyRoomRules(room);
            }
            return error;
        }
        private static void CommandRebuilding(string sceneName, out Room? __state)
        {
            __state = null; if (current == null) return;
            __state = string.IsNullOrEmpty(sceneName) ? DolocAPI.CurrentRoom : DolocAPI.GetRoom(sceneName);
            if (__state == null) return;
            RoomRebuilding(__state);
            // Native ClearMonsters returns before Manager.Clear when the list is
            // already empty. This is an observed empty list, not a promised clear.
            if (!__state.DM_monster.AllMonsters.Any()) current.Encounter(__state).Cleared();
        }
        private static Exception? CommandRebuilt(Room? __state, Exception? __exception) => __state == null ? __exception : CompleteRebuild(__state, __exception, true);
        private static void RebuildClearRender(Room __instance)
        { if (current != null && current.encounters.TryGetValue(__instance, out var state) && state.Ledger.IsRebuilding) current.Fault("Spawn.BeforeClear"); }
        private static void NativeAllocated(MonsterGenInfoProto __instance, Dictionary<string, List<Vector2>> __result)
        {
            if (current == null) return;
            current.pendingAllocations.Clear();
            foreach (var pair in __result)
            {
                var row = DolocConfig.Tables.TbMonster.GetOrDefault(pair.Key);
                if (row == null) continue;
                var points = RegionPoints(__instance).Where(p => p.Type == row.GenType).SelectMany(p =>
                    p.Point.Positions.Select((v, i) => (RegionId(p.Type, p.Point), v.x, v.y, i, p.Point.MaxCapacity))).ToArray();
                var queue = new Queue<NativeAllocation>();
                foreach (var position in pair.Value)
                {
                    var initial = position + row.Offset;
                    queue.Enqueue(new NativeAllocation(initial, SpawnSlotAssignment.Resolve(points, initial.x, initial.y, row.Offset.x, row.Offset.y, row.Size)));
                }
                current.pendingAllocations[pair.Key] = queue;
            }
        }
        private static void NativeHostAssigned(Monster __instance)
        {
            if (current == null || !current.allocatingMonsters.TryGetValue(__instance, out var allocation)) return;
            current.allocatingMonsters.Remove(__instance);
            // The concrete setter follows the native initial-position assignment.
            // Validate consumption; explicit spawns cannot borrow a different slot.
            var claim = __instance.Host is Room && Vector2.Distance(__instance.position, allocation.Position) <= .001f ? allocation.Claim :
                new SpawnSlotAssignment(Array.Empty<string>(), "NativeSpawnAssignmentUncertain: native allocation consumption did not match the initial host/position.");
            current.nativeAssignments.Add(__instance, claim);
        }
        private static void ContentMerged(Tables __instance)
        { if (current != null && ReferenceEquals(current.tablesIdentity, __instance)) { current.contentMergeDirty = true; current.Observe("Tables.MergeModExtension"); } }
    }
}
