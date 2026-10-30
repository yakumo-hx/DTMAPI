using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using DolocTown;
using DolocTown.MonsterAttackBehaviours;
using HarmonyLib;
using UnityEngine;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private static readonly MethodInfo PlacedMineLand = AccessTools.DeclaredMethod(typeof(Landmine), "Drop");
        private static readonly MethodInfo PlacedBombLand = AccessTools.DeclaredMethod(typeof(TimeBomb), "OnLandGround");
        private static readonly MethodInfo PlacedMineWait = AccessTools.DeclaredMethod(typeof(Landmine), "Wait", new[] { typeof(float) });
        private static readonly MethodInfo PlacedBombWait = AccessTools.DeclaredMethod(typeof(TimeBomb), "Wait", new[] { typeof(float) });
        private static readonly MethodInfo PlacedBoomEffects = AccessTools.DeclaredMethod(typeof(Boom), "BoomEffectsWithoutScreenTwist");
        private static readonly MethodInfo PlacedBoomInvoke = AccessTools.DeclaredMethod(typeof(Boom), "Invoke", new[] { typeof(Vector2), typeof(float), typeof(float), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(Action<Collider2D>), typeof(Action<Vector2>) });
        private static readonly MethodInfo PlacedSoundPost = AccessTools.DeclaredMethod(typeof(WwiseSoundManager), "PostSoundEvent", new[] { typeof(SoundEvents), typeof(GameObject), typeof(AkCallbackManager.EventCallback), typeof(bool) });
        private static bool PlacedSoundBoundary(IMonsterAttackBehaviour proto, string field) =>
            (proto is ILandmine || proto is ITimeBomb) && PlacedSoundBoundary(proto is ILandmine, field);
        private static bool SoundPrefixBoundary(MethodInfo target, string prefix)
        {
            var patches = Harmony.GetPatchInfo(target);
            return patches != null && patches.Prefixes.Any(p => p.owner == HarmonyOwner && p.PatchMethod.Name == prefix) &&
                patches.Transpilers.All(p => p.owner == HarmonyOwner);
        }
        private static bool PlacedSoundBoundary(bool landmine, string field)
        {
            if (!SoundPrefixBoundary(AccessTools.Method(typeof(Skill), "OnReuse"), nameof(NativeSkillReusing)) ||
                !SoundPrefixBoundary(AccessTools.Method(typeof(Skill), "OnRecycle"), nameof(NativeSkillRecycling))) return false;
            var drop = AccessTools.Method(landmine ? typeof(MonsterAttackBehaviourSkillLandmine) : typeof(MonsterAttackBehaviourSkillTimeBomb), landmine ? "DropLandmine" : "DropTimeBomb");
            if (!SoundPrefixBoundary(drop, nameof(NativeSkillDropping)) ||
                Harmony.GetPatchInfo(drop)?.Finalizers.Any(p => p.owner == HarmonyOwner && p.PatchMethod.Name == nameof(NativeSkillDropped)) != true) return false;
            var raise = Harmony.GetPatchInfo(AccessTools.Method(typeof(SkillManager), "Raise"));
            if (raise?.Postfixes.Any(p => p.owner == HarmonyOwner && p.PatchMethod.Name == nameof(PlacedSoundRaised)) != true ||
                raise.Transpilers.Any(p => p.owner != HarmonyOwner)) return false;
            if (field == "landSound") return SoundBoundary(landmine ? PlacedMineLand : PlacedBombLand, nameof(PlacedSoundTranspiler));
            if (field != "countdownSound" && field != "explosionSound") return false;
            if (field == "explosionSound")
            {
                var recycle = Harmony.GetPatchInfo(AccessTools.Method(typeof(Boom), "OnRecycle"));
                if (!SoundPrefixBoundary(AccessTools.Method(typeof(Boom), "OnReuse"), nameof(NativeBoomRetiring)) ||
                    recycle?.Postfixes.Any(p => p.owner == HarmonyOwner && p.PatchMethod.Name == nameof(NativeBoomRetiring)) != true ||
                    recycle.Transpilers.Any(p => p.owner != HarmonyOwner)) return false;
            }
            return SoundBoundary(landmine ? PlacedMineWait : PlacedBombWait, nameof(PlacedSoundTranspiler)) &&
                (field != "explosionSound" || SoundBoundary(PlacedBoomEffects, nameof(PlacedSoundTranspiler)));
        }
        private void InstallPlacedSoundHooks()
        {
            Patch(AccessTools.Method(typeof(SkillManager), "Raise"), postfix: nameof(PlacedSoundRaised));
            foreach (var method in new[] { PlacedMineLand, PlacedBombLand, PlacedMineWait, PlacedBombWait, PlacedBoomEffects })
                Patch(method, transpiler: nameof(PlacedSoundTranspiler));
        }
        private static IEnumerable<CodeInstruction> PlacedSoundTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            var code = instructions.ToList();
            bool waiting = __originalMethod.Equals(PlacedMineWait) || __originalMethod.Equals(PlacedBombWait);
            bool boom = __originalMethod.Equals(PlacedBoomEffects);
            var expected = waiting ? new[] { SoundEvents.STOP_DRONE_BOMB_COUNT_DOWN, SoundEvents.PLAY_DRONE_BOMB_COUNT_DOWN } :
                new[] { boom ? SoundEvents.PLAY_DRONE_ATTACK_BOMB_EXPLOSIVE : SoundEvents.PLAY_DRONE_ATTACK_THROW_BOMB };
            var sites = code.Select((instruction, index) => new { instruction, index }).Where(p => p.instruction.Calls(PlacedSoundPost)).ToArray();
            if (sites.Length != expected.Length || code.Count(i => i.Calls(PlacedBoomInvoke)) != (waiting ? 1 : 0))
                throw new InvalidOperationException("Native placed sound/boom call count changed: " + __originalMethod);
            for (int i = 0; i < sites.Length; i++)
            {
                int at = sites[i].index;
                if (at < 5 || !code[at - 5].LoadsConstant((int)expected[i]) || code[at - 4].opcode != OpCodes.Ldarg_0 ||
                    !code[at - 3].Calls(AccessTools.PropertyGetter(typeof(Component), "gameObject")) || code[at - 2].opcode != OpCodes.Ldnull ||
                    !code[at - 1].LoadsConstant(0) || sites[i].instruction.blocks.Count != 0)
                    throw new InvalidOperationException("Native placed sound arguments changed: " + __originalMethod);
            }
            foreach (var instruction in code)
            {
                bool sound = instruction.Calls(PlacedSoundPost);
                if (sound || instruction.Calls(PlacedBoomInvoke))
                {
                    if (instruction.blocks.Count != 0) throw new InvalidOperationException("Unexpected placed sound exception boundary.");
                    instruction.opcode = OpCodes.Ldarg_0; instruction.operand = null;
                    yield return instruction;
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NativeModule), sound ?
                        (boom ? nameof(PostPlacedBoomSound) : nameof(PostPlacedSkillSound)) : nameof(InvokePlacedSoundBoom)));
                }
                else yield return instruction;
            }
        }
        private static bool PostPlacedSkillSound(WwiseSoundManager manager, SoundEvents nativeEvent, GameObject emitter,
            AkCallbackManager.EventCallback callback, bool ignoreOccupy, Skill skill)
        {
            var module = current;
            if (module != null && module.placedSoundSkills.TryGetValue(skill, out var source) && emitter == skill.gameObject)
            {
                if (nativeEvent == SoundEvents.STOP_DRONE_BOMB_COUNT_DOWN)
                    module.StopPlacedSounds(v => ReferenceEquals(v.Source, source) && v.Field == "countdownSound");
                else if (module.TryPlayPlacedSound(source, nativeEvent == SoundEvents.PLAY_DRONE_ATTACK_THROW_BOMB ? "landSound" : "countdownSound", skill.position2d)) return true;
            }
            // Retain native STOP even after a custom post: an earlier inherited
            // or failed custom start may still be playing on the native emitter.
            return manager.PostSoundEvent(nativeEvent, emitter, callback, ignoreOccupy);
        }
        private static bool PostPlacedBoomSound(WwiseSoundManager manager, SoundEvents nativeEvent, GameObject emitter,
            AkCallbackManager.EventCallback callback, bool ignoreOccupy, Boom boom)
        {
            var module = current;
            if (module != null && emitter == boom.gameObject && module.placedSoundBooms.TryGetValue(boom, out var source) &&
                (module.invokingPlacedBoom == null || ReferenceEquals(module.invokingPlacedBoom, source)) &&
                module.TryPlayPlacedSound(source, "explosionSound", boom.transform.position)) return true;
            return manager.PostSoundEvent(nativeEvent, emitter, callback, ignoreOccupy);
        }
        private static void InvokePlacedSoundBoom(Boom boom, Vector2 position, float damage, float criticalRate, bool screenTwist,
            bool workOnMonster, bool workOnPlayer, bool workOnEnvironment, Action<Collider2D> onHit, Action<Vector2> effects, Skill skill)
        {
            var module = current;
            PlacedSoundSource? source = null;
            if (module != null && module.placedSoundSkills.TryGetValue(skill, out var parent) && !parent.Retired && !parent.Cancelled && PlacedOriginActive(parent.Origin))
            {
                source = new PlacedSoundSource(parent.Origin, boom, ++module.placedSoundGeneration);
                module.placedSoundBooms[boom] = source;
            }
            var previous = module?.invokingPlacedBoom;
            if (module != null) module.invokingPlacedBoom = source;
            try { boom.Invoke(position, damage, criticalRate, screenTwist, workOnMonster, workOnPlayer, workOnEnvironment, onHit, effects); }
            catch
            {
                if (module != null && source != null)
                {
                    source.Cancelled = true;
                    module.StopPlacedSounds(v => ReferenceEquals(v.Source, source));
                    if (module.placedSoundBooms.TryGetValue(boom, out var active) && ReferenceEquals(active, source)) module.placedSoundBooms.Remove(boom);
                }
                throw;
            }
            finally { if (module != null) module.invokingPlacedBoom = previous; }
        }
    }
}
