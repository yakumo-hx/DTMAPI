using System;
using System.Collections.Generic;
using System.Linq;
using DTMAPI.MonsterContent;
using DolocTown;
using DolocTown.Config;
using DolocTown.Config.General;
using DolocTown.Config.Item;
using DolocTown.GameData;
using DolocTown.MonsterAttackBehaviours;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private readonly Dictionary<MonsterSkin, SkinResources> nativeSkinResources = new Dictionary<MonsterSkin, SkinResources>();
        private readonly Dictionary<string, MonsterSkin> nativeSkins = new Dictionary<string, MonsterSkin>(StringComparer.Ordinal);
        private sealed class NativeSkinState
        {
            internal MonsterController Controller = null!;
            internal SpriteRenderer Renderer = null!;
            internal SkinResources Skin = null!;
            internal Sprite? Source, Mapped;
        }
        private readonly Dictionary<SpriteOverrideHandler, NativeSkinState> nativeSkinStates = new Dictionary<SpriteOverrideHandler, NativeSkinState>();
        private readonly Dictionary<MonsterDrops, ItemSpawnEntry> nativeDropEntries = new Dictionary<MonsterDrops, ItemSpawnEntry>();
        private MonsterDiagnostic? dropHookConflict;
        private Monster? extraDropMonster;
        private ItemSpawnEntry? extraDropEntry;

        private bool CanSkinNative(Template t) => CanTintNative(t) && t.Prefab!.GetComponent<SpriteOverrideHandler>() != null && PresentationInfo(t).CanMapAllFrames;
        // These native interfaces all dispatch through MonsterAttackBehaviourBullet.GenBullet.
        private static bool CanTintProjectiles(Template t) => t.Proto.AttackBehaviourProtos?.Any(a => a is IMonsterAttackBehaviourBullet bullet &&
            DolocAPI.assets.bullets.QueryData(bullet.BulletId, out _) && DolocAPI.assets.bulletMovers.QueryData(bullet.BulletMoverId, out _) &&
            (a is IShoot || a is IMultiShoot || a is ISalvoShoot || a is IDragonShoot || a is IScatteringShoot)) == true;
        private IReadOnlyList<string> NativeFields(Template t)
        {
            var fields = new List<string>();
            if (t.Errors.Count == 0 && overrideHookConflict == null) fields.AddRange(OverrideFields);
            if (CanTintNative(t)) fields.Add("tint");
            if (CanSkinNative(t)) fields.Add("skin");
            if (dropHookConflict == null) fields.Add("drops");
            if (CanTintProjectiles(t)) fields.Add("projectileTint");
            if (DescribeNativeAttacks(t).Any(a => a.Fields.Any(f => f.NativeOverride) || a.Sounds.Any(s => s.NativeOverride))) fields.Add("nativeAttacks");
            if (CanNativeHurtSound(t)) fields.Add("sounds.hurt");
            if (CanNativeDeadSound(t)) fields.Add("sounds.dead");
            if (CanNativeHummingSound(t)) fields.Add("sounds.humming");
            if (CanNativeRecoverySound(t)) fields.Add("sounds.recoveryWarning");
            return fields.AsReadOnly();
        }
        private void RefreshNativeContent()
        {
            RefreshNativeSounds();
            RefreshNativeTints();
            RefreshNativeAttackParameters();
            var desired = nativeOverrides.Targets("skin").ToDictionary(id => id, id => nativeOverrides.ResolveSkin(id)!, StringComparer.Ordinal);
            if (desired.Count != nativeSkins.Count || desired.Any(p => !nativeSkins.TryGetValue(p.Key, out var previous) || !ReferenceEquals(previous, p.Value)))
            {
                foreach (var state in nativeSkinStates.Values.ToArray()) RestoreNativeSkin(state.Controller);
                nativeSkins.Clear();
                foreach (var pair in desired) nativeSkins.Add(pair.Key, pair.Value);
                if (nativeSkins.Count != 0) foreach (var controller in Object.FindObjectsOfType<MonsterController>()) ApplyNativeSkin(controller);
            }
            var ready = nativeOverrides.ReadyValues.ToArray();
            foreach (var skin in nativeSkinResources.Keys.Where(s => !ready.Any(v => ReferenceEquals(v.Skin, s))).ToArray())
            { ReleaseSkin(nativeSkinResources[skin]); nativeSkinResources.Remove(skin); }
            foreach (var drops in nativeDropEntries.Keys.Where(d => !ready.Any(v => ReferenceEquals(v.Drops, d))).ToArray()) nativeDropEntries.Remove(drops);
            RefreshNativeProjectiles();
        }
        private void ClearNativeInstances()
        {
            StopNativeCommonSounds(_ => true);
            StopPlacedSounds(v => v.Source.Origin.Scope == null);
            StopNativeHurtSounds(_ => true);
            StopNativeDeadSounds(_ => true);
            StopNativeHummingSounds(_ => true);
            ClearNativeAttackParameters();
            ClearNativeTints();
            foreach (var state in nativeSkinStates.Values.ToArray()) RestoreNativeSkin(state.Controller);
            foreach (var entity in nativeProjectileStates.Keys.ToArray()) RestoreNativeProjectile(entity);
        }
        private void ClearNativeContent()
        {
            ClearNativeSounds();
            ClearNativeAttackParameters(clearTargets: true);
            ClearNativeInstances(); nativeTints.Clear();
            foreach (var state in nativeSkinStates.Values.ToArray()) RestoreNativeSkin(state.Controller);
            nativeSkins.Clear();
            foreach (var skin in nativeSkinResources.Values) ReleaseSkin(skin);
            nativeSkinResources.Clear(); nativeDropEntries.Clear();
            foreach (var entity in nativeProjectileStates.Keys.ToArray()) RestoreNativeProjectile(entity);
        }
        private void ApplyNativeInstanceOverrides(MonsterController controller) { ApplyNativeTint(controller); ApplyNativeSkin(controller); ApplyNativeAttackParameters(controller); }
        private void RestoreNativeInstanceOverrides(MonsterController controller) { StopNativeCommonSounds(v => v.Use != "dead" && ReferenceEquals(v.Controller, controller)); nativeSoundRentals.Remove(controller); RestoreNativeTint(controller); RestoreNativeSkin(controller); RestoreNativeAttackParameters(controller); }
        private void ApplyNativeSkin(MonsterController controller)
        {
            if (!Ready || controller == null || controller.Monster == null || bindings.ContainsKey(controller.Monster.proto.Name) ||
                !nativeSkins.TryGetValue(controller.Monster.proto.Name, out var skin) || !nativeSkinResources.TryGetValue(skin, out var resources)) return;
            var handler = controller.GetComponent<SpriteOverrideHandler>();
            var renderer = controller.Renderer?.SpriteRenderer;
            if (handler == null || renderer == null || nativeSkinStates.ContainsKey(handler)) return;
            nativeSkinStates.Add(handler, new NativeSkinState { Controller = controller, Renderer = renderer, Skin = resources });
            MapNativeSkin(handler);
        }
        private bool MapNativeSkin(SpriteOverrideHandler handler)
        {
            if (!nativeSkinStates.TryGetValue(handler, out var state)) return true;
            var source = state.Renderer.sprite;
            if (source == null || source == state.Mapped) return false;
            // A foreign override may have written a differently named sprite before late attach.
            // Wait for the animator's next known native frame; never index an unverified frame.
            if (!state.Skin.Sprites.TryGetValue(source.name, out var sprite)) return true;
            state.Source = source; state.Mapped = sprite; state.Renderer.sprite = sprite;
            return false;
        }
        private void RestoreNativeSkin(MonsterController controller)
        {
            foreach (var pair in nativeSkinStates.Where(p => ReferenceEquals(p.Value.Controller, controller)).ToArray())
            {
                var state = pair.Value;
                if (state.Renderer != null && state.Renderer.sprite == state.Mapped) state.Renderer.sprite = state.Source;
                nativeSkinStates.Remove(pair.Key);
            }
        }
        private static void NativeDropEntryRead(ref MonsterProto __instance, ref ItemSpawnEntry __result)
        {
            var module = current;
            if (module == null || !module.Ready) return;
            if (module.extraDropMonster != null && module.extraDropMonster.proto.Name == __instance.Name && module.extraDropEntry != null)
            { __result = module.extraDropEntry; return; }
            string target = module.bindings.TryGetValue(__instance.Name, out var binding) && !binding.NativeChild ? binding.Definition.Id : __instance.Name;
            var replacement = module.nativeOverrides.ResolveDrops(target);
            if (replacement != null && module.nativeDropEntries.TryGetValue(replacement, out var entry)) __result = entry;
        }
        private static void AppendNativeDrops(GuaranteedManager __instance, Monster monster)
        {
            var module = current;
            if (module == null || !module.Ready || module.extraDropMonster != null || monster?.Host == null) return;
            var entries = new List<ItemSpawnEntry>();
            string target = module.bindings.TryGetValue(monster.proto.Name, out var binding) && !binding.NativeChild ? binding.Definition.Id : monster.proto.Name;
            foreach (var contribution in module.nativeOverrides.AppendDrops(target))
                if (module.nativeDropEntries.TryGetValue(contribution, out var entry)) entries.Add(entry);
            var predicate = (Func<string, bool>)Delegate.CreateDelegate(typeof(Func<string, bool>), __instance, AccessTools.Method(typeof(GuaranteedManager), "IsValidMonsterDropId"));
            foreach (var entry in entries)
            {
                if (!entry.SpawnLut_Ref.SpawnDatas.Any(e => predicate(e.ItemName))) continue;
                try { module.extraDropMonster = monster; module.extraDropEntry = entry; __instance.SpawnMonsterDropItems(monster); }
                finally { module.extraDropEntry = null; module.extraDropMonster = null; }
            }
        }

        private sealed class NativeProjectileState
        {
            internal string Target = "";
            internal SpriteRenderer Renderer = null!;
            internal Color Original, Applied;
        }
        private readonly Dictionary<BulletEntity, NativeProjectileState> nativeProjectileStates = new Dictionary<BulletEntity, NativeProjectileState>();
        private void WrapNativeProjectile(MonsterController controller, ref Action<Bullet> created)
        {
            if (!Ready || controller == null || controller.Monster == null) return;
            string target = controller.Monster.proto.Name;
            if (bindings.ContainsKey(target) || nativeOverrides.ResolveProjectileTint(target) == null) return;
            var previous = created; long epoch = generation;
            created = bullet =>
            {
                previous?.Invoke(bullet);
                if (current != this || !Ready || epoch != generation) return;
                var text = nativeOverrides.ResolveProjectileTint(target);
                if (text == null || bullet.entity == null) return;
                var renderer = bullet.entity.GetComponent<SpriteRenderer>();
                if (renderer == null || !ColorUtility.TryParseHtmlString(text, out var color)) return;
                RestoreNativeProjectile(bullet.entity);
                var state = new NativeProjectileState { Target = target, Renderer = renderer, Original = renderer.color, Applied = renderer.color * color };
                nativeProjectileStates.Add(bullet.entity, state); renderer.color = state.Applied;
            };
        }
        private static void RestoreProjectile(DolocRecyclableObject __instance)
        { if (__instance is BulletEntity entity) current?.RestoreNativeProjectile(entity); }
        private void RestoreNativeProjectile(BulletEntity entity)
        {
            if (!nativeProjectileStates.TryGetValue(entity, out var state)) return;
            if (state.Renderer != null && state.Renderer.color == state.Applied) state.Renderer.color = state.Original;
            nativeProjectileStates.Remove(entity);
        }
        private void RefreshNativeProjectiles()
        {
            foreach (var pair in nativeProjectileStates.ToArray())
            {
                var state = pair.Value;
                string? text = nativeOverrides.ResolveProjectileTint(state.Target);
                if (text == null || state.Renderer == null || state.Renderer.color != state.Applied) { RestoreNativeProjectile(pair.Key); continue; }
                if (ColorUtility.TryParseHtmlString(text, out var color)) { state.Applied = state.Original * color; state.Renderer.color = state.Applied; }
            }
        }
    }
}
