using DTMAPI.MonsterContent;
using DolocTown;
using DolocTown.MonsterAttackBehaviours;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private sealed class ChomperBiteParameters : IChomperBite, INativeAttackParameters
        {
            private readonly IChomperBite source;
            private readonly MonsterNativeAttackParameters? definition;
            private readonly NativeModule? module;
            private readonly string target = "";
            internal ChomperBiteParameters(IChomperBite source, MonsterNativeAttackParameters value) { this.source = source; definition = value; }
            internal ChomperBiteParameters(IChomperBite source, NativeModule module, string target) { this.source = source; this.module = module; this.target = target; }
            public IMonsterAttackBehaviour Original => source;
            public bool NativeOverride => module != null;
            public bool HasStartCooldownEdit => module == null ? definition?.StartOnCooldown.HasValue == true :
                module.Ready && module.nativeAttackHookConflict == null && !double.IsNaN(module.nativeOverrides.Resolve(target,
                    NativeAttackValidation.Field(AttackId.ToString(), "startOnCooldown"), double.NaN));
            private float Read(string field, float baseline, float? configured) => module == null ? configured ?? baseline :
                !module.Ready || module.nativeAttackHookConflict != null ? baseline :
                (float)module.nativeOverrides.Resolve(target, NativeAttackValidation.Field(AttackId.ToString(), field), baseline);
            private static float? Flag(bool? value) => value.HasValue ? (value.Value ? 1f : 0f) : (float?)null;
            public MonsterAttackId AttackId => source.AttackId;
            public float CDDuration => Read("cooldownGameSeconds", source.CDDuration, definition?.CooldownGameSeconds);
            public bool CDOnStart => Read("startOnCooldown", source.CDOnStart ? 1 : 0, Flag(definition?.StartOnCooldown)) != 0;
            public float Damage => Read("damage", source.Damage, definition?.Damage);
            public float CriticalRate => Read("criticalRate", source.CriticalRate, definition?.CriticalRate);
            public bool HasReadyAction => Read("chomperBite.windupEnabled", source.HasReadyAction ? 1 : 0, Flag(definition?.ChomperBite?.WindupEnabled)) != 0;
            public float ReadyDuration => Read("windupGameSeconds", source.ReadyDuration, definition?.WindupGameSeconds);
            public float AttackRange => Read("chomperBite.range", source.AttackRange, definition?.ChomperBite?.Range);
            public string AnimName => source.AnimName;
        }
        private static void ChomperBiteStarted(MonsterAttackBehaviourChomperBite __instance)
        {
            if (current == null || !(__instance._proto is ChomperBiteParameters parameters) ||
                parameters.HasReadyAction && parameters.ReadyDuration > 0) return;
            var helper = NativeAccess.Get<MonsterAttackHelperAnim>(__instance, "_helperAnimController");
            NativeAccess.Set(helper, "shouldWait", false);
            NativeAccess.Set(helper, "waitDuration", 0f);
            NativeAccess.Set(helper, "callbackOnReady", null);
            helper.Invoke(0f, parameters.AnimName); // initializes the name; the native overload does not play at zero.
            if (parameters.HasReadyAction)
            {
                __instance.SetAttackParams(parameters.Damage, parameters.CriticalRate);
                helper.Invoke(parameters.AnimName);
            }
            NativeAccess.Get<UnityEngine.Animator>(helper, "animator").Update(0f);
        }
        private static void CancelChomperBite(MonsterAttackBehaviour? attack)
        {
            if (!(attack is MonsterAttackBehaviourChomperBite bite)) return;
            // Called only inside existing owned-instance cancellation. Original monsters keep native lifecycle.
            var helper = NativeAccess.Get<MonsterAttackHelperAnim>(bite, "_helperAnimController");
            NativeAccess.Set(helper, "shouldWait", false);
            NativeAccess.Set(helper, "waitDuration", 0f);
            NativeAccess.Set(helper, "callbackOnReady", null);
            var animator = NativeAccess.Get<UnityEngine.Animator>(helper, "animator");
            animator.Play("idle", 0, 0f);
            animator.Update(0f); // the old attack clip must not re-enable its damage collider next frame.
        }
        private static MonsterNativeAttackInfo DescribeChomperBite(IChomperBite a, bool definition, bool native)
        {
            MonsterNativeAttackFieldInfo F(string field, double value, string unit, string at) => new MonsterNativeAttackFieldInfo(field, value, unit, at, definition, native);
            return new MonsterNativeAttackInfo(a.AttackId.ToString(), "chomper-bite", new[] {
                F("cooldownGameSeconds", a.CDDuration, "game-seconds", "definition registration; native next available attack; preserve current remaining wait"),
                F("startOnCooldown", a.CDOnStart ? 1 : 0, "boolean", "spawn/reentry registration; no live rearming"),
                F("damage", a.Damage, "damage", "SetAttackParams after enabled wait or immediately without wait; current animation retains the snapshot"),
                F("criticalRate", a.CriticalRate, "fraction", "SetAttackParams before animation; current window retains the snapshot"),
                F("windupGameSeconds", a.ReadyDuration, "game-seconds", "Invoke captures enabled wait; current wait remains fixed"),
                F("chomperBite.windupEnabled", a.HasReadyAction ? 1 : 0, "boolean", "Invoke; false starts the actual native clip immediately"),
                F("chomperBite.range", a.AttackRange, "world-units", "native distance from FirePosition to target center; does not resize the damage collider") },
                "Animation=" + a.AnimName + ". Native clip controls the physical collider (current attack clip opens at 0.25s, closes at 0.416667s, ends at 0.516667s). Zero wait still plays the clip. Setting damage does not clear native hit deduplication; animation alone does not rearm a previously hit receiver. Input pause holds battle updates/cooldown but does not freeze Animator. Original override cancellation remains native; owned-instance cancellation retires the clip and pending helper. Clip resources, collider size, Mimicry and native shield/sword rules remain inherited.");
        }
    }
}
