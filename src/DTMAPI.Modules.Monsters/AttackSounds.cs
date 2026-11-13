using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using DTMAPI.MonsterContent;
using DolocTown;
using DolocTown.MonsterAttackBehaviours;
using HarmonyLib;
using UnityEngine;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private static readonly FieldInfo AttackSoundControllerField = AccessTools.Field(typeof(MonsterAttackBehaviour), "_controller");
        private static bool AttackSoundBoundary(IMonsterAttackBehaviour proto, string field) => field == "fireSound" ? FireSoundBoundary(proto) : field == "dashSound" ? DashSoundBoundary(proto) : field == "readySound" ? ReadySoundBoundary(proto) : AttackEventSoundBoundary(proto, field) || PlacedSoundBoundary(proto, field);
        private static MonsterNativeAttackInfo WithAttackSounds(Template template, MonsterNativeAttackInfo info, bool definition, bool native)
        {
            var proto = template.Proto.AttackBehaviourProtos.Single(a => a.AttackId.ToString() == info.AttackId);
            var sounds = new List<MonsterNativeAttackSoundInfo>();
            if (FireSoundBoundary(proto)) sounds.Add(new MonsterNativeAttackSoundInfo("fireSound", FireReadAt(proto) + "; next post uses the current SET winner; overlapping one-shots", definition, native));
            if (DashSoundBoundary(proto)) sounds.Add(new MonsterNativeAttackSoundInfo("dashSound", (proto is IMultiImpact ? "each native impact segment" : "native dash start") + "; next post uses the current SET winner; preparation is a separate readySound event", definition, native));
            if (ReadySoundBoundary(proto)) sounds.Add(new MonsterNativeAttackSoundInfo("readySound", proto is ISting ? "native Sting preparation; one-shot tail survives ordinary attack cancellation" :
                (proto is IMultiImpact ? "each native segment preparation" : "native preparation") + "; one-shot tail survives ordinary cancel; actual 104 stops this rental ready/recovery countdown set", definition, native));
            if (AttackEventSoundBoundary(proto, "throwSound")) sounds.Add(new MonsterNativeAttackSoundInfo("throwSound", "each successful native Landmine/TimeBomb placement; zero count and failed allocation do not post", definition, native));
            if (AttackEventSoundBoundary(proto, "volleySound")) sounds.Add(new MonsterNativeAttackSoundInfo("volleySound", "each native BulletTrain volley, including zero bullets; FireSound remains per bullet", definition, native));
            if (PlacedSoundBoundary(proto, "landSound")) sounds.Add(new MonsterNativeAttackSoundInfo("landSound", "native skill landing; skill position; next post reads current SET; stopped by skill recycle", definition, native));
            if (PlacedSoundBoundary(proto, "countdownSound")) sounds.Add(new MonsterNativeAttackSoundInfo("countdownSound", "each native skill countdown step; overlapping one-shots; native countdown stop or skill recycle stops this skill generation", definition, native));
            if (PlacedSoundBoundary(proto, "explosionSound")) sounds.Add(new MonsterNativeAttackSoundInfo("explosionSound", "placed skill successor Boom at its position; tail survives natural skill/Boom recycle; room/source withdrawal stops it", definition, native));
            return new MonsterNativeAttackInfo(info.AttackId, info.Kind, info.Fields, info.Limitation, sounds);
        }
        private void InstallAttackSoundHooks() { InstallFireSoundHooks(); InstallDashSoundHooks(); InstallAttackEventSoundHooks(); InstallReadySoundHooks(); InstallPlacedSoundHooks(); }
        private static IEnumerable<CodeInstruction> RewriteAttackSoundPost(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod, string postMethod)
        {
            var code = instructions.ToList();
            var post = AccessTools.Method(typeof(MonsterController), "PostSoundEvent", new[] { typeof(string) });
            var sites = code.Where(i => i.Calls(post)).ToArray();
            var empty = code.Where(i => i.Calls(FireEmptyTarget)).ToArray();
            if (sites.Length != 1 || empty.Length != (__originalMethod.Equals(FireVectorTarget) ? 1 : 0))
                throw new InvalidOperationException("Native attack sound post/empty guard boundary changed: " + __originalMethod);
            foreach (var instruction in code)
            {
                if (instruction.Calls(post) || instruction.Calls(FireEmptyTarget))
                {
                    bool guard = instruction.Calls(FireEmptyTarget);
                    if (instruction.blocks.Count != 0) throw new InvalidOperationException("Unexpected exception boundary at the native attack sound post.");
                    // Keep the original instruction and its branch labels at the
                    // inserted argument; the following call consumes the full stack.
                    instruction.opcode = OpCodes.Ldarg_0; instruction.operand = null;
                    yield return instruction;
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NativeModule), guard ? nameof(FireSoundMissing) : postMethod));
                    continue;
                }
                yield return instruction;
            }
        }
        private bool TryPlayAttackSound(MonsterController controller, MonsterAttackBehaviour attack, string field)
        {
            var common = FindCommonAttackSound(controller, attack, field, out var commonScope);
            if (common != null) return TryPlayCommonSound(commonScope!, common, field, attack, attack._proto.AttackId.ToString());
            if (!controllers.ContainsKey(controller))
            {
                // Ready has already tried its 103/109 adapter. Never retry it.
                if (field == "readySound" || SoundValidation.PlacedField(field) || !NativeSoundController(controller) ||
                    !ReferenceEquals(AttackSoundControllerField.GetValue(attack), controller) ||
                    !controller.attackBehaviourManager.AttackBehaviours.Contains(attack) || !AttackSoundBoundary(attack._proto, field)) return false;
                return TryPlayNativeCommonSound(controller, NativeAttackValidation.Field(attack._proto.AttackId.ToString(), field), field, attack, attack._proto.AttackId.ToString());
            }
            return false;
        }
    }
}
