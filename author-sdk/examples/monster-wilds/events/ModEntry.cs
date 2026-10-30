using System;
using System.Collections.Generic;
using System.Linq;
using DTMAPI.Abstractions;
using DTMAPI.MonsterContent;
using DolocTown;
using HarmonyLib;
using UnityEngine;

namespace Samples.MonsterWilds
{
    public sealed partial class ModEntry : DtmMod, IDisposable
    {
        public const string Owner = "Samples.MonsterWilds";
        const string LocalId = "hive-guardian";
        static ModEntry? current;
        readonly Dictionary<MonsterController, GameObject> halos = new Dictionary<MonsterController, GameObject>();
        IMonsterContentApi? api;
        MonsterLease? demand;
        MonsterDefinitionLease? guardian;
        Harmony? harmony;
        Material? haloMaterial;
        bool stopped, disposed;

        public override void Entry(IDtmHelper helper)
        {
            api = helper.ModRegistry.GetApi<IMonsterContentApi>(MonsterModule.ProviderId)
                ?? throw new InvalidOperationException("Update DTMAPI Runtime to the matching candidate.");
            helper.GetRequiredService<IDtmOwnedResources>("0.7.0", "0.7.0").Register(this);
            current = this;
            try
            {
                demand = Require(api.RequestModule());
                guardian = Require(api.RegisterDefinition(new MonsterDefinition {
                    LocalId = LocalId, TemplateId = "bee", Health = 120, Defense = 3,
                    Drops = new MonsterDrops { Mode = MonsterDropMode.Append, MinCount = 4, MaxCount = 4, Entries = new List<MonsterDropEntry> {
                        new MonsterDropEntry { ItemId = "gold_ore", Weight = 30 },
                        new MonsterDropEntry { ItemId = "titanium_ore", Weight = 10 },
                        new MonsterDropEntry { ItemId = BlueprintId, Weight = 1, MaxCount = 1 } } },
                    Display = new MonsterDisplay { Name = "巢穴守卫者", Nameplate = true, HealthBar = MonsterHealthBar.Damaged }
                }));
                harmony = new Harmony(Owner + ".Native");
                harmony.Patch(AccessTools.DeclaredMethod(typeof(DungeonResource), "OnCompleteFell"),
                    prefix: new HarmonyMethod(typeof(ModEntry), nameof(BeforeFell)),
                    postfix: new HarmonyMethod(typeof(ModEntry), nameof(AfterFell)));
                harmony.Patch(AccessTools.DeclaredMethod(typeof(MonsterController), nameof(MonsterController.Run)),
                    postfix: new HarmonyMethod(typeof(ModEntry), nameof(AfterRun)) { priority = Priority.Last });
                harmony.Patch(AccessTools.DeclaredMethod(typeof(MonsterController), nameof(MonsterController.OnRecycle)),
                    prefix: new HarmonyMethod(typeof(ModEntry), nameof(BeforeRecycle)) { priority = Priority.First });
                InstallKing(helper);
                helper.GetRequiredService<IDtmCommands>("0.7.0", "0.7.0").Register("guardian", c => {
                    if (c.Arguments.Count == 1 && c.Arguments[0] == "withdraw") {
                        Dispose(); c.WriteLine("withdrawn " + guardian!.Snapshot.State);
                    } else c.WriteLine("definition=" + guardian!.Snapshot.Id + "; native=" + guardian.Snapshot.NativeId
                        + "; state=" + guardian.Snapshot.State + "; halos=" + halos.Count);
                }, description: "Inspect or withdraw the Wilds hive guardian and its product-owned halo.");
            }
            catch { Dispose(); throw; }
        }

        sealed class FellAttempt
        {
            internal readonly ModEntry Owner;
            internal readonly IDungeonResourceHost Host;
            internal readonly Room Room;
            internal readonly Vector2 Position;
            internal bool Finished;
            internal FellAttempt(ModEntry owner, DungeonResource resource)
            { Owner = owner; Host = resource.Host; Room = resource.Host.CurrentRoom; Position = resource.Renderer.position2d; }
        }

        static void BeforeFell(DungeonResource __instance, out FellAttempt? __state)
        {
            __state = null;
            var owner = current;
            if (owner == null || owner.stopped || !(__instance is DungeonResourceHive) || __instance.IsRemoved
                || !__instance.isRender || __instance.Renderer == null || __instance.Host == null
                || !ReferenceEquals(__instance.Host.CurrentRoom, DolocAPI.CurrentRoom)
                || !__instance.Host.DM_dungeonResource.AllDungeonResources.Contains(__instance)) return;
            __state = new FellAttempt(owner, __instance);
        }

        static void AfterFell(DungeonResource __instance, FellAttempt? __state)
        {
            if (__state == null || __state.Finished) return;
            __state.Finished = true;
            var owner = __state.Owner;
            if (owner.stopped || !ReferenceEquals(current, owner) || !__instance.IsRemoved
                || __state.Host.DM_dungeonResource.AllDungeonResources.Contains(__instance)
                || !ReferenceEquals(__state.Room, DolocAPI.CurrentRoom)) return;
            // The native operation has already committed its working-world removal.
            // A product failure must not throw back into native loot/message handling.
            try { owner.SpawnGuardian(__state.Position); }
            catch (Exception ex) { owner.Monitor.Log("Hive guardian failed after native removal: " + ex.Message, LogLevel.Error); }
        }

        void SpawnGuardian(Vector2 hive)
        {
            if (guardian!.Snapshot.State != MonsterDefinitionState.Creatable) {
                Monitor.Log("Hive guardian unavailable: " + guardian.Snapshot.State, LogLevel.Warn); return;
            }
            string failure = "No valid position";
            foreach (var offset in SpawnOffsets)
            {
                var point = hive + offset;
                var result = api!.Spawn(new MonsterSpawnRequest { DefinitionId = Owner + ":" + LocalId, X = point.x, Y = point.y });
                if (result.Success) {
                    Monitor.Log("Hive guardian spawned once: " + result.Value!.Handle + "; position=" + result.Value.X + "," + result.Value.Y);
                    return;
                }
                failure = result.Diagnostic!.Code + ": " + result.Diagnostic.Message;
                if (result.Diagnostic.Code != "InvalidPosition") break;
            }
            Monitor.Log("Hive destroyed; guardian could not spawn: " + failure, LogLevel.Warn);
        }

        static readonly Vector2[] SpawnOffsets = {
            new Vector2(0, 3), new Vector2(-1.5f, 3), new Vector2(1.5f, 3),
            new Vector2(0, 4.5f), new Vector2(-3, 3), new Vector2(3, 3),
            new Vector2(-1.5f, 4.5f), new Vector2(1.5f, 4.5f)
        };

        static void AfterRun(MonsterController __instance)
        {
            var owner = current;
            if (owner == null || owner.stopped || owner.guardian == null || __instance.Monster == null) return;
            try { owner.ObserveKingRun(__instance); }
            catch (Exception ex) { owner.RemoveCrown(__instance); owner.Monitor.Log("King presentation failed: " + ex.Message, LogLevel.Error); }
            var identity = owner.guardian.Snapshot;
            if (identity.State != MonsterDefinitionState.Creatable || identity.NativeId.Length == 0
                || __instance.Monster.Name != identity.NativeId) return;
            try { owner.AttachHalo(__instance); }
            catch (Exception ex) { owner.RemoveHalo(__instance); owner.Monitor.Log("Guardian halo failed: " + ex.Message, LogLevel.Error); }
        }

        static void BeforeRecycle(MonsterController __instance)
        {
            current?.RemoveHalo(__instance);
            current?.RetireKingExecution(__instance);
        }

        void AttachHalo(MonsterController controller)
        {
            RemoveHalo(controller);
            var sprite = controller.Renderer.SpriteRenderer;
            var halo = new GameObject(Owner + ".Halo");
            halos.Add(controller, halo);
            halo.transform.SetParent(sprite.transform, false);
            halo.transform.position = sprite.bounds.center;
            var ring = halo.AddComponent<LineRenderer>();
            ring.sharedMaterial = GetGoldMaterial();
            ring.useWorldSpace = false; ring.loop = true; ring.positionCount = 40;
            ring.startWidth = ring.endWidth = .12f;
            ring.startColor = ring.endColor = new Color(1, .76f, .16f, .8f);
            ring.sortingLayerID = sprite.sortingLayerID; ring.sortingOrder = sprite.sortingOrder - 1;
            float radius = Mathf.Max(sprite.sprite.bounds.extents.x, sprite.sprite.bounds.extents.y) + .3f;
            for (int i = 0; i < ring.positionCount; i++) {
                float angle = i * Mathf.PI * 2 / ring.positionCount;
                ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0));
            }
        }

        void RemoveHalo(MonsterController controller)
        {
            if (!halos.TryGetValue(controller, out var halo)) return;
            halos.Remove(controller);
            if (halo != null) { halo.SetActive(false); UnityEngine.Object.Destroy(halo); }
        }

        public void Dispose()
        {
            if (disposed) return;
            stopped = true;
            var errors = new List<Exception>();
            try { StopKing(); } catch (Exception ex) { errors.Add(ex); }
            try { guardian?.Dispose(); } catch (Exception ex) { errors.Add(ex); }
            foreach (var controller in halos.Keys.ToArray()) RemoveHalo(controller);
            if (haloMaterial != null) { UnityEngine.Object.Destroy(haloMaterial); haloMaterial = null; }
            try { harmony?.UnpatchSelf(); } catch (Exception ex) { errors.Add(ex); }
            if (ReferenceEquals(current, this)) current = null;
            try { demand?.Dispose(); } catch (Exception ex) { errors.Add(ex); }
            if (errors.Count > 0) throw new AggregateException("Wilds guardian cleanup incomplete.", errors);
            disposed = true;
        }

        static T Require<T>(MonsterResult<T> value) => value.Success ? value.Value! : throw new InvalidOperationException(value.Diagnostic!.Code + ": " + value.Diagnostic.Message);
    }
}
