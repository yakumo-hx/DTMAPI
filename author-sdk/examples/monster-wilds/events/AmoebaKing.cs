using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DTMAPI.Abstractions;
using DTMAPI.MonsterContent;
using DolocTown;
using HarmonyLib;
using UnityEngine;

namespace Samples.MonsterWilds
{
    public sealed partial class ModEntry
    {
        const string BlueprintId = "samples_monsterwilds_royal_beacon_blueprint";
        const string BeaconId = "samples_monsterwilds_royal_beacon";
        const string ArenaId = "dungeon_旧城市废墟.郊野低地-1";
        const string KingId = "amoeba-king", MinionId = "royal-minion";
        const int MinionLimit = 4, HealPerSecond = 6;
        static readonly FieldInfo HealthField = AccessTools.Field(typeof(Monster), "currentHealth");
        readonly Dictionary<Monster, KingEncounter> kings = new Dictionary<Monster, KingEncounter>();
        readonly Dictionary<Monster, KingEncounter> minionParents = new Dictionary<Monster, KingEncounter>();
        readonly Dictionary<MonsterController, GameObject> crowns = new Dictionary<MonsterController, GameObject>();
        MonsterDefinitionLease? kingDefinition, minionDefinition;
        KingEncounter? spawningFor;
        bool usingBeacon;

        sealed class KingEncounter
        {
            internal readonly Monster Monster;
            internal readonly HashSet<Monster> Children = new HashSet<Monster>();
            internal bool PhaseTwo, Retiring, HasSummoned;
            internal float SummonWait = 4, HealCredit, HealSecondsRemaining = 20;
            internal int Healed;
            internal KingEncounter(Monster monster) { Monster = monster; }
        }

        void InstallKing(IDtmHelper helper)
        {
            if (HealthField == null || HealthField.FieldType != typeof(int)) throw new InvalidOperationException("Native monster health field does not match.");
            minionDefinition = Require(api!.RegisterDefinition(new MonsterDefinition {
                LocalId = MinionId, TemplateId = "amoeba", Health = 40, Defense = 1,
                Body = new MonsterBodyParameters { Scale = .75f }, Appearance = new MonsterAppearance { Tint = "#70B4FA" },
                Display = new MonsterDisplay { Name = "王冠幼体", Nameplate = true, HealthBar = MonsterHealthBar.Damaged }
            }));
            kingDefinition = Require(api.RegisterDefinition(new MonsterDefinition {
                LocalId = KingId, TemplateId = "amoeba", Health = 700, Defense = 4,
                Drops = new MonsterDrops { Mode = MonsterDropMode.Replace, MinCount = 25, MaxCount = 25, Entries = new List<MonsterDropEntry> {
                    new MonsterDropEntry { ItemId = "titanium_ore", MinCount = 5, MaxCount = 5 },
                    new MonsterDropEntry { ItemId = "slime_gel", MinCount = 20, MaxCount = 20 } } },
                Body = new MonsterBodyParameters { Scale = 2 },
                Appearance = new MonsterAppearance { Tint = "#4389E8", Skin = LoadKingSkin() },
                Display = new MonsterDisplay { Name = "蓝冠变形虫王", Nameplate = true, HealthBar = MonsterHealthBar.Always, BossBar = true }
            }));
            harmony!.Patch(AccessTools.DeclaredMethod(typeof(Item), "OnUseAsItem"), postfix: new HarmonyMethod(typeof(ModEntry), nameof(AfterUseBeacon)));
            harmony.Patch(AccessTools.DeclaredMethod(typeof(Item), "OnUseAsTool"), postfix: new HarmonyMethod(typeof(ModEntry), nameof(AfterUseBeacon)));
            harmony.Patch(AccessTools.DeclaredMethod(typeof(MonsterController), nameof(MonsterController.OnUpdate)), postfix: new HarmonyMethod(typeof(ModEntry), nameof(AfterKingUpdate)));
            harmony.Patch(AccessTools.DeclaredMethod(typeof(MonsterManager), nameof(MonsterManager.RemoveMonster)), postfix: new HarmonyMethod(typeof(ModEntry), nameof(AfterMonsterRemoved)));
            harmony.Patch(AccessTools.DeclaredMethod(typeof(MonsterManager), nameof(MonsterManager.Clear)), prefix: new HarmonyMethod(typeof(ModEntry), nameof(BeforeMonstersCleared)));
            harmony.Patch(AccessTools.DeclaredMethod(typeof(Monster), nameof(Monster.InvokeDropLib)), prefix: new HarmonyMethod(typeof(ModEntry), nameof(BeforeMinionDrops)));
            harmony.Patch(AccessTools.DeclaredMethod(typeof(MonsterManager), nameof(MonsterManager.SetIsRunning)), prefix: new HarmonyMethod(typeof(ModEntry), nameof(BeforeMonstersRunningChanged)));
            helper.GetRequiredService<IDtmCommands>("0.7.0", "0.7.0").Register("king", c => {
                c.WriteLine("definition=" + kingDefinition.Snapshot.State + "; encounters=" + kings.Count + "; minions=" + minionParents.Count + "; crowns=" + crowns.Count);
                foreach (var state in kings.Values) c.WriteLine("phase=" + (state.PhaseTwo ? 2 : 1) + "; hp=" + HealthField.GetValue(state.Monster)
                    + "/" + state.Monster.proto.Health + "; healed=" + state.Healed + "; healSecondsRemaining=" + state.HealSecondsRemaining + "; summonWait=" + state.SummonWait + "; minions=" + state.Children.Count + "; visible=" + (state.Monster.Controller != null));
            }, description: "Inspect the actual Wilds king encounters; summon by crafting and using a Blue Crown Beacon.");
        }

        static void AfterUseBeacon(Item __instance)
        {
            var owner = current;
            if (owner == null || owner.stopped || owner.usingBeacon || __instance.name != BeaconId) return;
            owner.usingBeacon = true;
            try { owner.UseBeacon(__instance); }
            catch (Exception ex) { owner.Monitor.Log("Blue Crown Beacon failed: " + ex, LogLevel.Error); DolocAPI.ShowMessageBoxSmallErr("信标未能完成召唤，请查看模组日志。"); }
            finally { owner.usingBeacon = false; }
        }

        void UseBeacon(Item item)
        {
            var inventory = DolocAPI.archiveHandle?.InventorySystem.inventory;
            if (inventory == null || item.count <= 0 || inventory.IndexOf(item) < 0) return;
            if (DolocAPI.CurrentRoom?.RoomId != ArenaId) { DolocAPI.ShowMessageBoxSmallErr("请在废弃城市的郊野低地中央区域使用蓝冠信标。"); return; }
            if (kings.Keys.Any(m => !m.IsDead && m.Host.DM_monster.AllMonsters.Contains(m))) { DolocAPI.ShowMessageBoxSmallErr("蓝冠变形虫王仍在附近，先结束这场遭遇。"); return; }
            if (kingDefinition!.Snapshot.State != MonsterDefinitionState.Creatable) { DolocAPI.ShowMessageBoxSmallErr("变形虫王尚未就绪，信标没有消耗。"); return; }
            var spawn = SpawnAround(KingId, DolocAPI.AgentPosition, KingOffsets);
            if (!spawn.Success) { Monitor.Log("Beacon spawn rejected: " + spawn.Diagnostic!.Code + ": " + spawn.Diagnostic.Message, LogLevel.Warn); DolocAPI.ShowMessageBoxSmallErr("附近没有足够空间，信标没有消耗。"); return; }
            int before = item.count;
            Exception? costError = null;
            try { item.CostSelf(false); } catch (Exception ex) { costError = ex; }
            // Observe the native inventory operation: an exception after notification cannot refund a consumed item or spawn twice.
            bool consumed = inventory.IndexOf(item) < 0 || item.count == before - 1;
            if (!consumed) {
                var undo = api!.Despawn(spawn.Value!.Handle);
                if (!undo.Success) throw new InvalidOperationException("Beacon consumption failed and spawn compensation failed: " + undo.Diagnostic!.Message, costError);
                Monitor.Log("Beacon was not consumed; the new king was withdrawn.", LogLevel.Warn);
                DolocAPI.ShowMessageBoxSmallErr("信标未扣除，召唤已取消。"); return;
            }
            if (costError != null) Monitor.Log("Beacon consumed and king retained; inventory notification failed: " + costError.Message, LogLevel.Warn);
            Monitor.Log("Blue Crown Beacon consumed once; king=" + spawn.Value!.Handle);
            DolocAPI.ShowMessageBoxSmall("蓝冠变形虫王现身了！");
        }

        static readonly Vector2[] KingOffsets = {
            new Vector2(3, 0), new Vector2(-3, 0), new Vector2(4, 0), new Vector2(-4, 0),
            new Vector2(6, 0), new Vector2(-6, 0), new Vector2(8, 0), new Vector2(-8, 0),
            new Vector2(10, 0), new Vector2(-10, 0), new Vector2(6, 2), new Vector2(-6, 2)
        };
        static readonly Vector2[] MinionOffsets = {
            new Vector2(-3, 0), new Vector2(3, 0), new Vector2(-5, 0), new Vector2(5, 0),
            new Vector2(-3, 2), new Vector2(3, 2)
        };
        MonsterResult<MonsterInstanceSnapshot> SpawnAround(string id, Vector2 origin, Vector2[] offsets)
        {
            MonsterResult<MonsterInstanceSnapshot>? result = null;
            foreach (var offset in offsets) {
                var point = origin + offset;
                result = api!.Spawn(new MonsterSpawnRequest { DefinitionId = Owner + ":" + id, X = point.x, Y = point.y });
                if (result.Success && Vector2.Distance(point, new Vector2(result.Value!.X, result.Value.Y)) > 3) {
                    // Native landing can select a distant lower floor. Keep this encounter near its summoner.
                    // Check only creation: the native AI remains free to move and re-enter at its later position.
                    var undo = api.Despawn(result.Value.Handle);
                    if (!undo.Success) return MonsterResult<MonsterInstanceSnapshot>.Fail(undo.Diagnostic!);
                    result = MonsterResult<MonsterInstanceSnapshot>.Fail(new MonsterDiagnostic("InvalidPosition",
                        "Native landing is more than three units from the nearby candidate.", Owner, Owner + ":" + id, stage: "SpawnAround"));
                }
                if (result.Success || result.Diagnostic!.Code != "InvalidPosition") return result;
            }
            return result!;
        }

        void ObserveKingRun(MonsterController controller)
        {
            var monster = controller.Monster;
            if (kingDefinition != null && kingDefinition.Snapshot.State == MonsterDefinitionState.Creatable && monster.Name == kingDefinition.Snapshot.NativeId) {
                if (!kings.TryGetValue(monster, out var state)) kings.Add(monster, state = new KingEncounter(monster));
                state.SummonWait = Math.Max(state.SummonWait, 3);
                AttachCrown(controller, state.PhaseTwo);
            } else if (spawningFor != null && minionDefinition != null && monster.Name == minionDefinition.Snapshot.NativeId) {
                minionParents.Add(monster, spawningFor); spawningFor.Children.Add(monster);
            }
        }

        static void AfterKingUpdate(MonsterController __instance, float dt)
        {
            var owner = current;
            if (owner == null || owner.stopped || __instance.Monster == null || !owner.kings.TryGetValue(__instance.Monster, out var state)
                || state.Retiring || state.Monster.IsDead || dt <= 0 || float.IsNaN(dt) || float.IsInfinity(dt)
                || DolocAPI.gameLoop.IsPaused || DolocAPI.gameLoop.IsGlobalPaused
                || !ReferenceEquals(state.Monster.Controller, __instance) || !ReferenceEquals(state.Monster.Host.CurrentRoom, DolocAPI.CurrentRoom)) return;
            try { owner.TickKing(state, dt); }
            catch (Exception ex) { state.Retiring = true; owner.Monitor.Log("King encounter stopped after failure: " + ex, LogLevel.Error); owner.RetireMinions(state); }
        }

        void TickKing(KingEncounter state, float dt)
        {
            int health = (int)HealthField.GetValue(state.Monster), maximum = state.Monster.proto.Health;
            if (health <= 0) return;
            if (!state.PhaseTwo && health <= maximum / 2) {
                state.PhaseTwo = true; if (state.HasSummoned) state.SummonWait = Math.Min(state.SummonWait, 3);
                if (crowns.TryGetValue(state.Monster.Controller, out var crown)) SetCrownColor(crown.GetComponent<LineRenderer>(), true);
                Monitor.Log("King phase 2 entered at actual health=" + health + "/" + maximum);
            }
            if (state.PhaseTwo && state.HealSecondsRemaining > 0) {
                float elapsed = Math.Min(dt, state.HealSecondsRemaining);
                state.HealSecondsRemaining -= elapsed;
                state.HealCredit += elapsed * HealPerSecond;
                int amount = Math.Min(120 - state.Healed, Math.Min(maximum - health, (int)(state.HealCredit + .0001f)));
                if (amount > 0) { HealthField.SetValue(state.Monster, health + amount); state.HealCredit -= amount; state.Healed += amount; }
            }
            state.SummonWait -= dt;
            if (state.SummonWait > 0) return;
            state.SummonWait = state.PhaseTwo ? 10 : 15;
            state.HasSummoned = true;
            for (int i = 0; i < 2 && state.Children.Count < MinionLimit; i++) {
                int before = state.Children.Count;
                spawningFor = state;
                MonsterResult<MonsterInstanceSnapshot> spawn;
                try { spawn = SpawnAround(MinionId, state.Monster.Controller.position2d, MinionOffsets); }
                finally { spawningFor = null; }
                if (!spawn.Success) { Monitor.Log("King minion skipped: " + spawn.Diagnostic!.Code, LogLevel.Debug); break; }
                if (state.Children.Count != before + 1) { api!.Despawn(spawn.Value!.Handle); throw new InvalidOperationException("A minion was not bound to its exact encounter."); }
            }
        }

        static void AfterMonsterRemoved(Monster monster, bool __result)
        {
            if (__result) current?.ForgetMonster(monster);
        }
        static bool BeforeMinionDrops(Monster __instance)
        {
            var identity = current?.minionDefinition?.Snapshot;
            // Native count ranges clamp zero draws to one; suppress this exact product definition instead.
            return identity == null || identity.State != MonsterDefinitionState.Creatable || identity.NativeId.Length == 0
                || __instance.Name != identity.NativeId;
        }
        static void BeforeMonstersCleared(MonsterManager __instance)
        {
            var owner = current;
            if (owner == null) return;
            foreach (var monster in __instance.AllMonsters.ToArray()) owner.ForgetMonster(monster);
        }
        static void BeforeMonstersRunningChanged(MonsterManager __instance, bool __0)
        {
            var owner = current;
            if (owner == null || __0) return;
            // HideAllMonsters sets IsRunning=false before iterating its live logical list.
            foreach (var state in owner.kings.Values.ToArray())
                if (ReferenceEquals(state.Monster.Host.DM_monster, __instance)) owner.RetireMinions(state);
        }
        void ForgetMonster(Monster monster)
        {
            if (minionParents.TryGetValue(monster, out var parent)) { parent.Children.Remove(monster); minionParents.Remove(monster); }
            if (kings.TryGetValue(monster, out var state)) {
                state.Retiring = true; RetireMinions(state);
                if (monster.Controller != null) RemoveCrown(monster.Controller);
                kings.Remove(monster);
            }
        }
        void RetireKingExecution(MonsterController controller)
        {
            RemoveCrown(controller);
            if (controller.Monster != null && kings.TryGetValue(controller.Monster, out var state)) {
                // Bulk clear is still enumerating here. Its later Manager.Clear retires the logical children.
                state.SummonWait = state.HasSummoned ? (state.PhaseTwo ? 10 : 15) : state.SummonWait;
            }
        }
        void RetireMinions(KingEncounter state)
        {
            var errors = new List<Exception>();
            foreach (var child in state.Children.ToArray()) {
                try {
                    if (!minionParents.TryGetValue(child, out var parent) || !ReferenceEquals(parent, state)) throw new InvalidOperationException("Minion ownership changed.");
                    if (child.Controller == null) child.Host.DM_monster.RemoveMonster(child);
                    else child.Host.RemoveMonster(child);
                    if (child.Host.DM_monster.AllMonsters.Contains(child)) throw new InvalidOperationException("Native minion removal did not complete.");
                    state.Children.Remove(child); minionParents.Remove(child);
                } catch (Exception ex) { errors.Add(ex); }
            }
            if (errors.Count > 0) throw new AggregateException("King minion cleanup incomplete.", errors);
        }

        Material GetGoldMaterial()
        {
            if (haloMaterial != null) return haloMaterial;
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) throw new InvalidOperationException("Sprites/Default shader is unavailable.");
            return haloMaterial = new Material(shader) { name = Owner + ".HaloMaterial" };
        }
        void AttachCrown(MonsterController controller, bool phaseTwo)
        {
            RemoveCrown(controller);
            var sprite = controller.Renderer.SpriteRenderer;
            var crown = new GameObject(Owner + ".Crown"); crowns.Add(controller, crown);
            crown.transform.SetParent(sprite.transform, false);
            var follower = crown.AddComponent<CrownFollower>(); follower.Owner = this; follower.Renderer = sprite;
            PositionCrown(crown.transform, sprite);
            var line = crown.AddComponent<LineRenderer>(); line.sharedMaterial = GetGoldMaterial();
            line.useWorldSpace = false; line.loop = true; line.startWidth = line.endWidth = .09f;
            line.sortingLayerID = sprite.sortingLayerID; line.sortingOrder = sprite.sortingOrder + 1;
            var points = new[] { new Vector3(-.6f, 0), new Vector3(-.75f, .55f), new Vector3(-.25f, .3f), new Vector3(0, .8f),
                new Vector3(.25f, .3f), new Vector3(.75f, .55f), new Vector3(.6f, 0) };
            line.positionCount = points.Length; line.SetPositions(points); SetCrownColor(line, phaseTwo);
        }
        static void SetCrownColor(LineRenderer line, bool phaseTwo) => line.startColor = line.endColor = phaseTwo ? new Color(1, .35f, .12f, 1) : new Color(1, .8f, .15f, 1);
        void RemoveCrown(MonsterController controller)
        {
            if (!crowns.TryGetValue(controller, out var crown)) return;
            crowns.Remove(controller); if (crown != null) { crown.SetActive(false); UnityEngine.Object.Destroy(crown); }
        }
        void StopKing()
        {
            foreach (var state in kings.Values.ToArray()) { state.Retiring = true; RetireMinions(state); }
            kingDefinition?.Dispose(); minionDefinition?.Dispose();
            foreach (var controller in crowns.Keys.ToArray()) RemoveCrown(controller);
            kings.Clear(); minionParents.Clear(); spawningFor = null;
        }
    }
}
