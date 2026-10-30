using DTMAPI.MonsterContent;
using DolocTown;
using DolocTown.GameData;
using DolocTown.MonsterAttackBehaviours;
using UnityEngine;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private sealed class MultiImpactParameters : IMultiImpact, INativeAttackParameters
        {
            private readonly IMultiImpact source;
            private readonly MonsterNativeAttackParameters? definition;
            private readonly NativeModule? module;
            private readonly string target = "";
            internal MultiImpactParameters(IMultiImpact source, MonsterNativeAttackParameters value) { this.source = source; definition = value; }
            internal MultiImpactParameters(IMultiImpact source, NativeModule module, string target) { this.source = source; this.module = module; this.target = target; }
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
            public bool HasReadyAction => Read("multiImpact.windupEnabled", source.HasReadyAction ? 1 : 0, Flag(definition?.MultiImpact?.WindupEnabled)) != 0;
            public float ReadyDuration => Read("windupGameSeconds", source.ReadyDuration, definition?.WindupGameSeconds);
            public float DashDistance => Read("multiImpact.distance", source.DashDistance, definition?.MultiImpact?.Distance);
            public float DashSpeed => Read("multiImpact.travelSpeed", source.DashSpeed, definition?.MultiImpact?.TravelSpeed);
            public float DashDuration => DashDistance / DashSpeed;
            public Vector2Int TimesRange => new Vector2Int(
                (int)Read("multiImpact.minExtraDashes", source.TimesRange.x, definition?.MultiImpact?.MinExtraDashes),
                (int)Read("multiImpact.maxExtraDashes", source.TimesRange.y, definition?.MultiImpact?.MaxExtraDashes));
            public ImpactPath Path => source.Path;
            public float ArcDistance => source.ArcDistance;
            public DG.Tweening.Ease DashEase => source.DashEase;
            public string DashSound => source.DashSound;
            public bool HasAfterImage => source.HasAfterImage;
            public bool ShakeScreen => source.ShakeScreen;
        }
        private static MonsterNativeAttackInfo DescribeMultiImpact(IMultiImpact a, bool definition, bool native)
        {
            MonsterNativeAttackFieldInfo F(string field, double value, string unit, string at) => new MonsterNativeAttackFieldInfo(field, value, unit, at, definition, native);
            return new MonsterNativeAttackInfo(a.AttackId.ToString(), "multi-impact", new[] {
                F("cooldownGameSeconds", a.CDDuration, "game-seconds", "registration; next available attack; preserve remaining cooldown"),
                F("startOnCooldown", a.CDOnStart ? 1 : 0, "boolean", "spawn/reentry registration; no live rearming"),
                F("damage", a.Damage, "damage", "each DoImpact opens a new body window; current window retains its snapshot"),
                F("criticalRate", a.CriticalRate, "fraction", "each DoImpact; each leg resets native hit deduplication"),
                F("windupGameSeconds", a.ReadyDuration, "game-seconds", "each ReadyForImpact captures its own wait; an existing wait is unchanged"),
                F("multiImpact.windupEnabled", a.HasReadyAction ? 1 : 0, "boolean", "each ReadyForImpact; false starts that leg immediately"),
                F("multiImpact.distance", a.DashDistance, "world-units", "target-distance gate; each DoImpact captures current origin, target direction and endpoint"),
                F("multiImpact.travelSpeed", a.DashSpeed, "world-units-per-game-second", "each DoImpact captures duration = distance/speed; inherited easing remains"),
                F("multiImpact.minExtraDashes", a.TimesRange.x, "extra-dashes", "Invoke samples one inclusive value; total legs = value + 1, reversed/equal bounds use min"),
                F("multiImpact.maxExtraDashes", a.TimesRange.y, "extra-dashes", "Invoke samples once; later edits do not change the current total") },
                "Extra-dash endpoints inherit independently. The native minimum is one extra dash (two total); explicit endpoints admit 1..63. " +
                "Each leg retargets after its optional windup. Body damage stays enabled during subsequent windups, retaining the previous leg's damage until the next DoImpact; the final leg closes it. " +
                "No Arc/Path input is consumed and no additional terrain sweep is added. Existing movement/windows retain snapshots on edits or withdrawal. " +
                "Easing, afterimages, non-final-leg screen shake, sounds and exact native ownership remain inherited.");
        }
    }
}
