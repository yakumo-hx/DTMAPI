using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DolocTown;
using DolocTown.MonsterAttackBehaviours;
using DTMAPI.MonsterContent;
using HarmonyLib;
using RedSaw;
using UnityEngine;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private sealed class NativeSkillManagerOwner
        {
            internal readonly NativeModule Module;
            internal readonly SkillManager Manager;
            internal readonly BattleSystem Battle;
            internal readonly HashSet<NativeSkillLease> Leases = new HashSet<NativeSkillLease>();
            internal int Depth;
            internal bool Draining;
            internal NativeSkillManagerOwner(NativeModule module, SkillManager manager, BattleSystem battle)
            { Module = module; Manager = manager; Battle = battle; manager.AddReference(); }
        }
        private sealed class NativeSkillInvocation
        {
            internal readonly NativeModule Module;
            internal readonly InstanceScope? Scope;
            internal readonly NativeSkillInvocation? Previous;
            internal readonly MonsterActionHandle? Action;
            internal readonly PlacedSoundOrigin? SoundOrigin;
            internal bool Cancelled;
            internal NativeSkillInvocation(NativeModule module, InstanceScope? scope, MonsterAttackBehaviour? attack = null)
            { Module = module; Scope = scope; Action = scope?.Action?.Handle; Previous = module.raisingNativeSkill;
                SoundOrigin = attack == null ? null : module.CapturePlacedSoundOrigin(attack, scope); }
        }
        private sealed class NativeSkillLease
        {
            internal readonly InstanceScope Scope;
            internal readonly MonsterHandle Instance;
            internal readonly MonsterActionHandle? Action;
            internal readonly NativeSkillManagerOwner Owner;
            internal readonly Skill Skill;
            internal readonly RigidbodyType2D BodyType;
            internal bool Cancelled, NativeEnded;
            internal NativeSkillLease(InstanceScope scope, NativeSkillManagerOwner owner, Skill skill)
            { Scope = scope; Instance = scope.Handle; Action = scope.Action?.Handle; Owner = owner; Skill = skill; BodyType = skill.RB.bodyType; }
            internal bool Valid => !Cancelled && !NativeEnded && Scope.Active && Scope.Handle.Equals(Instance);
        }
        private sealed class NativeBoomLease
        {
            internal readonly NativeSkillLease Source;
            internal readonly Boom Boom;
            internal bool Cancelled, Invoking;
            internal NativeBoomLease(NativeSkillLease source, Boom boom) { Source = source; Boom = boom; }
            // The source skill normally recycles immediately after Invoke; its explosion is a separate lease.
            internal bool Valid => !Cancelled && Source.Scope.Active && Source.Scope.Handle.Equals(Source.Instance);
        }
        private readonly struct NativeSkillTick
        {
            internal readonly NativeSkillLease? Previous, Lease;
            internal NativeSkillTick(NativeSkillLease? previous, NativeSkillLease? lease) { Previous = previous; Lease = lease; }
        }
        private readonly Dictionary<SkillManager, NativeSkillManagerOwner> nativeSkillManagers = new Dictionary<SkillManager, NativeSkillManagerOwner>();
        private readonly Dictionary<Skill, NativeSkillLease> nativeSkills = new Dictionary<Skill, NativeSkillLease>();
        private readonly Dictionary<Boom, NativeBoomLease> nativeBooms = new Dictionary<Boom, NativeBoomLease>();
        private NativeSkillInvocation? raisingNativeSkill;
        private NativeSkillLease? advancingNativeSkill;

        private static string? NativeSkillAdmission(IMonsterAttackBehaviourSkill attack)
        {
            if (!(attack is ILandmine) && !(attack is ITimeBomb) && !(attack is IShadowImpact) && !(attack is IChomperMimicry) && !(attack is IThunderPile)) return "Only native Landmine/TimeBomb/ShadowImpact/ChomperMimicry/ThunderPile skill lifetimes are admitted; this skill still needs its own owner boundary.";
            if (!DolocAPI.assets.skills.QueryData(attack.SkillId, out var proto) || proto.prefab == null) return "Missing skill prefab: " + attack.SkillId;
            var skill = proto.prefab.GetComponent<Skill>();
            var expected = attack is ILandmine ? typeof(Landmine) : attack is ITimeBomb ? typeof(TimeBomb) : attack is IShadowImpact ? typeof(ShadowBox) : attack is IChomperMimicry ? typeof(ChomperMimicry) : typeof(ThunderPile);
            if (skill == null || skill.GetType() != expected) return "Skill prefab type must match " + expected.Name + ": " + attack.SkillId;
            if (!skill.GetComponent<Rigidbody2D>() || !skill.GetComponent<Collider2D>() || !skill.GetComponent<SpriteRenderer>()) return "Skill body/render resources are incomplete: " + attack.SkillId;
            if (attack is IChomperMimicry) return null;
            if (attack is IThunderPile) return LocMaterials.GAME_MAT_HOLOGRAM_THUNDER_PILE == null || LocMaterials.GAME_MAT_EFFECTS_SHINE == null ? "ThunderPile effect materials are missing." : null;
            if (attack is IShadowImpact)
                return LocMaterials.GAME_MAT_HOLOGRAM_RED == null || LocMaterials.GAME_MAT_HOLOGRAM_GREEN == null
                    ? "ShadowImpact hologram materials are missing." : null;
            if (!(NativeAccess.Get(skill, "counterSprites") is Sprite[] frames) || frames.Length == 0 || frames.Any(s => s == null) ||
                !(NativeAccess.Get(skill, "counterSr") is SpriteRenderer) || !(NativeAccess.Get(skill, "dropSprite") is Sprite) || !(NativeAccess.Get(skill, "normalSprite") is Sprite))
                return "Skill countdown/render resources are incomplete: " + attack.SkillId;
            if (skill is Landmine && !(NativeAccess.Get(skill, "trigger") is SimpleTrigger)) return "Landmine trigger is missing: " + attack.SkillId;
            return null;
        }
        private void InstallNativeSkillHooks()
        {
            InstallNativeShadowHooks();
            InstallNativeMimicryHooks();
            InstallNativeThunderHooks();
            Patch(AccessTools.DeclaredMethod(typeof(MonsterController), "Attack"), prefix: nameof(NativeSkillAttackRequested));
            Patch(AccessTools.DeclaredMethod(typeof(MonsterController), "Hurt", new[] { typeof(UnityEngine.Transform), typeof(float), typeof(bool), typeof(bool) }), prefix: nameof(NativeSkillHurt));
            Patch(AccessTools.Method(typeof(MonsterAttackBehaviourSkillLandmine), "DropLandmine"), prefix: nameof(NativeSkillDropping), finalizer: nameof(NativeSkillDropped));
            Patch(AccessTools.Method(typeof(MonsterAttackBehaviourSkillTimeBomb), "DropTimeBomb"), prefix: nameof(NativeSkillDropping), finalizer: nameof(NativeSkillDropped));
            Patch(AccessTools.Method(typeof(SkillManager), "Raise"), prefix: nameof(NativeSkillRaising), finalizer: nameof(NativeSkillRaised));
            Patch(AccessTools.Method(typeof(SkillManager), "OnFixedUpdate"), prefix: nameof(NativeSkillManagerEntering), finalizer: nameof(NativeSkillManagerExited));
            Patch(AccessTools.Method(typeof(SkillManager), "ClearSkills"), prefix: nameof(NativeSkillManagerEntering), finalizer: nameof(NativeSkillManagerExited));
            Patch(AccessTools.Method(typeof(Skill), "OnReuse"), prefix: nameof(NativeSkillReusing));
            Patch(AccessTools.Method(typeof(Skill), "OnRecycle"), prefix: nameof(NativeSkillRecycling));
            Patch(AccessTools.Method(typeof(Landmine), "OnFixedUpdate"), prefix: nameof(NativeSkillAdvancing), finalizer: nameof(NativeSkillAdvanced));
            Patch(AccessTools.Method(typeof(TimeBomb), "OnFixedUpdate"), prefix: nameof(NativeSkillAdvancing), finalizer: nameof(NativeSkillAdvanced));
            Patch(AccessTools.DeclaredMethod(typeof(Boom), "Invoke", new[] { typeof(Vector2), typeof(float), typeof(float), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(Action<Collider2D>), typeof(Action<Vector2>) }), prefix: nameof(NativeBoomInvoking), finalizer: nameof(NativeBoomInvoked));
            Patch(AccessTools.Method(typeof(Boom), "OnTriggerEnter2D"), prefix: nameof(NativeBoomHitting));
            Patch(AccessTools.Method(typeof(Boom), "OnReuse"), prefix: nameof(NativeBoomRetiring));
            Patch(AccessTools.Method(typeof(Boom), "OnRecycle"), postfix: nameof(NativeBoomRetiring));
        }
        private static bool NativeSkillDropping(MonsterAttackBehaviour __instance, out NativeSkillInvocation? __state)
        {
            __state = null;
            var module = current;
            if (module == null) return true;
            module.controllers.TryGetValue(NativeAccess.Get<MonsterController>(__instance, "_controller"), out var scope);
            __state = new NativeSkillInvocation(module, scope, __instance);
            module.raisingNativeSkill = __state;
            return scope == null || scope.Active;
        }
        private static Exception? NativeSkillDropped(NativeSkillInvocation? __state, Exception? __exception)
        {
            if (__state == null) return __exception;
            __state.Module.raisingNativeSkill = __state.Previous;
            if (__exception != null && __state.SoundOrigin != null)
            { __state.SoundOrigin.Cancelled = true; __state.Module.RetirePlacedOrigins(o => ReferenceEquals(o, __state.SoundOrigin)); }
            if (__exception != null && __state.Scope != null) __state.Module.FailAction(__state.Scope, "NativeSkill.Drop", __exception);
            return __exception;
        }
        private static bool NativeSkillRaising(SkillManager __instance, ref Skill skill, ref bool __result, out NativeSkillManagerOwner? __state)
        {
            __state = null;
            var module = current;
            var scope = module?.raisingNativeSkill?.Scope;
            if (module == null || scope == null) return true;
            if (!scope.Active || module.raisingNativeSkill!.Cancelled) { skill = null!; __result = false; return false; }
            if (!module.nativeSkillManagers.TryGetValue(__instance, out __state))
            {
                __state = new NativeSkillManagerOwner(module, __instance, scope.Battle);
                module.nativeSkillManagers.Add(__instance, __state);
            }
            // Also defer releases through pool.Next/RunSkill and their callbacks.
            __state.Depth++;
            return true;
        }
        private static Exception? NativeSkillRaised(Skill skill, bool __result, NativeSkillManagerOwner? __state, Exception? __exception)
        {
            if (__state == null) return __exception;
            var module = __state.Module;
            var scope = module.raisingNativeSkill!.Scope!;
            try
            {
                if (skill != null && (__result || __exception != null))
                {
                    if (module.nativeSkills.TryGetValue(skill, out var previous)) module.ForgetNativeSkill(previous);
                    var lease = new NativeSkillLease(scope, __state, skill);
                    module.nativeSkills.Add(skill, lease); scope.NativeSkills.Add(lease); __state.Leases.Add(lease);
                    if (skill is TimeBomb bomb) bomb.IsItemBomb = false;
                    if (__exception == null) module.Fault("NativeSkillRaised");
                    lease.Cancelled = __exception != null || !scope.Active || module.raisingNativeSkill!.Cancelled;
                }
            }
            catch (Exception error)
            {
                __exception = error;
                if (skill != null && module.nativeSkills.TryGetValue(skill, out var lease)) lease.Cancelled = true;
            }
            finally { __state.Depth--; }
            try { module.DrainNativeSkillManager(__state); }
            catch (Exception error) { __exception = __exception == null ? error : new AggregateException(__exception, error); }
            return __exception;
        }
        private static void NativeSkillManagerEntering(SkillManager __instance, out NativeSkillManagerOwner? __state)
        {
            __state = null;
            if (current != null && current.nativeSkillManagers.TryGetValue(__instance, out __state)) __state.Depth++;
        }
        private static Exception? NativeSkillManagerExited(NativeSkillManagerOwner? __state, Exception? __exception)
        {
            if (__state == null) return __exception;
            __state.Depth--;
            try { __state.Module.DrainNativeSkillManager(__state); }
            catch (Exception error) { return __exception == null ? error : new AggregateException(__exception, error); }
            return __exception;
        }
        private static void NativeSkillReusing(Skill __instance)
        {
            if (current == null) return;
            current.RetirePlacedSkill(__instance);
            if (current.nativeSkills.TryGetValue(__instance, out var lease))
            {
                current.RetireNativeSkill(__instance);
                current.ForgetNativeSkill(lease);
            }
            if (__instance is ShadowBox shadow) current.retiredNativeShadows.Remove(shadow);
            current.ReuseNativeMimicry(__instance);
            current.ReuseNativeThunder(__instance);
        }
        private static void NativeSkillRecycling(Skill __instance)
        {
            current?.RetirePlacedSkill(__instance);
            if (current == null || !current.nativeSkills.TryGetValue(__instance, out var lease)) return;
            current.RetireNativeSkill(__instance);
            // A paused object otherwise misses OnGameResumed after it leaves the active list.
            if (__instance.RB != null) { __instance.RB.bodyType = lease.BodyType; __instance.RB.velocity = Vector2.zero; __instance.RB.angularVelocity = 0; }
            lease.NativeEnded = true;
        }
        private static bool NativeSkillAdvancing(Skill __instance, ref bool __result, out NativeSkillTick __state)
        {
            var module = current;
            NativeSkillLease? lease = null;
            module?.nativeSkills.TryGetValue(__instance, out lease);
            __state = new NativeSkillTick(module?.advancingNativeSkill, lease);
            if (module != null) module.advancingNativeSkill = lease;
            if (lease != null && !lease.Valid) { __result = true; return false; }
            return true;
        }
        private static Exception? NativeSkillAdvanced(NativeSkillTick __state, Exception? __exception)
        {
            var module = __state.Lease?.Owner.Module ?? current;
            if (module != null) module.advancingNativeSkill = __state.Previous;
            if (__exception != null && __state.Lease != null) module!.FailAction(__state.Lease.Scope, "NativeSkill.Update", __exception);
            return __exception;
        }
        private void ForgetNativeSkill(NativeSkillLease lease)
        {
            lease.NativeEnded = true;
            if (nativeSkills.TryGetValue(lease.Skill, out var active) && ReferenceEquals(active, lease)) nativeSkills.Remove(lease.Skill);
            lease.Scope.NativeSkills.Remove(lease); lease.Owner.Leases.Remove(lease);
        }
        private void DrainNativeSkillManager(NativeSkillManagerOwner owner)
        {
            if (owner.Depth != 0 || owner.Draining || owner.Leases.Any(s => s.Scope.NativeSkillDepth > 0)) return;
            owner.Draining = true;
            try
            {
                foreach (var lease in owner.Leases.ToArray())
                {
                    if (lease.NativeEnded || lease.Skill == null) { ForgetNativeSkill(lease); continue; }
                    if (!lease.Cancelled) continue;
                    RetireNativeSkill(lease.Skill);
                    lease.Skill.Damage = 0; lease.Skill.CriticalRate = 0;
                    NativeAccess.Get<IList>(owner.Manager, "skills").Remove(lease.Skill);
                    FilterQueue(NativeAccess.Get(owner.Manager, "recycleQueue")!, item => !ReferenceEquals(item, lease.Skill));
                    Fault("NativeSkillRecycle");
                    NativeAccess.Get<NashObjectPool<Skill>>(owner.Manager, "pool").Recycle(lease.Skill);
                    ForgetNativeSkill(lease);
                }
            }
            finally { owner.Draining = false; }
            if (owner.Leases.Count != 0) return;
            // Last attack disposal may have happened reentrantly during OnFixedUpdate.
            // Release our one extra reference only after native enumeration/pool operations finish.
            nativeSkillManagers.Remove(owner.Manager);
            owner.Battle.RemoveSkillManager(owner.Manager);
        }
        private static bool NativeBoomInvoking(Boom __instance, out NativeBoomLease? __state)
        {
            __state = null;
            var module = current;
            var source = module?.advancingNativeSkill;
            if (module == null || source == null) return true;
            var lease = new NativeBoomLease(source, __instance) { Cancelled = !source.Valid, Invoking = true };
            module.nativeBooms.Add(__instance, lease); source.Scope.NativeBooms.Add(lease);
            __state = lease;
            return !lease.Cancelled;
        }
        private static Exception? NativeBoomInvoked(NativeBoomLease? __state, Exception? __exception)
        {
            if (__state == null) return __exception;
            __state.Invoking = false;
            if (__exception != null) __state.Cancelled = true;
            if (__state.Cancelled)
            {
                try { __state.Source.Owner.Module.ReleaseNativeBoom(__state); }
                catch (Exception error) { return __exception == null ? error : new AggregateException(__exception, error); }
            }
            return __exception;
        }
        private static bool NativeBoomHitting(Boom __instance) => current == null || !current.nativeBooms.TryGetValue(__instance, out var lease) || lease.Valid;
        private static void NativeBoomRetiring(Boom __instance)
        {
            // Unregistering the native Boom does not stop its one-shot tail.
            if (current != null && current.placedSoundBooms.TryGetValue(__instance, out var soundSource))
            { soundSource.Retired = true; current.placedSoundBooms.Remove(__instance); }
            if (current == null || !current.nativeBooms.TryGetValue(__instance, out var lease)) return;
            current.ForgetNativeBoom(lease);
        }
        private void ForgetNativeBoom(NativeBoomLease lease)
        {
            lease.Source.Scope.NativeBooms.Remove(lease);
            if (nativeBooms.TryGetValue(lease.Boom, out var active) && ReferenceEquals(active, lease)) nativeBooms.Remove(lease.Boom);
        }
        private void ReleaseNativeBoom(NativeBoomLease lease)
        {
            if (!nativeBooms.TryGetValue(lease.Boom, out var active) || !ReferenceEquals(active, lease)) return;
            lease.Cancelled = true;
            if (lease.Invoking) return;
            if (lease.Boom == null) { ForgetNativeBoom(lease); return; }
            NativeAccess.Set(lease.Boom, "_damage", 0f);
            NativeAccess.Set(lease.Boom, "_shouldWorkOnMonster", false);
            NativeAccess.Set(lease.Boom, "_shouldWorkOnPlayer", false);
            NativeAccess.Set(lease.Boom, "_shouldWorkOnEnvironment", false);
            NativeAccess.Set(lease.Boom, "_onHitCallback", null);
            NativeAccess.Set(lease.Boom, "_customEffects", null);
            Fault("NativeSkillBoomRecycle");
            DolocAPI.EntitySystem.Recycle(lease.Boom);
            ForgetNativeBoom(lease);
        }
        private void CancelNativeSkills(InstanceScope scope, MonsterActionHandle? action = null)
        {
            bool Selected(MonsterActionHandle? handle) => action == null || handle.HasValue && handle.Value.Equals(action.Value);
            for (var invocation = raisingNativeSkill; invocation != null; invocation = invocation.Previous)
                if (ReferenceEquals(invocation.Scope, scope) && Selected(invocation.Action)) invocation.Cancelled = true;
            var skills = scope.NativeSkills.Where(s => Selected(s.Action)).ToArray();
            foreach (var skill in skills) skill.Cancelled = true;
            var errors = new List<Exception>();
            Attempt(errors, () => RetirePlacedOrigins(o => ReferenceEquals(o.Scope, scope) && Selected(o.Action)));
            Attempt(errors, () => CancelNativeMimicry(scope, action));
            Attempt(errors, () => CancelNativeThunder(scope, action));
            // Retire all independent callbacks/colliders before a fallible pool release.
            foreach (var skill in skills) Attempt(errors, () => RetireNativeSkill(skill.Skill));
            foreach (var boom in scope.NativeBooms.Where(b => Selected(b.Source.Action)).ToArray()) Attempt(errors, () => ReleaseNativeBoom(boom));
            foreach (var owner in skills.Select(s => s.Owner).Distinct()) Attempt(errors, () => DrainNativeSkillManager(owner));
            ThrowCleanup(errors);
        }
        private void RetireNativeSkill(Skill skill) { RetirePlacedSkill(skill); RetireNativeShadow(skill); RetireNativeMimicry(skill); RetireNativeThunder(skill); }
    }
}
