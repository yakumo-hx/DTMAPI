using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DTMAPI.MonsterContent;
using DolocTown;
using DolocTown.MonsterAttackBehaviours;
using HarmonyLib;
using RedSaw;
using UnityEngine;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private interface INativeAttackParameters : IMonsterAttackBehaviour
        {
            IMonsterAttackBehaviour Original { get; }
            bool NativeOverride { get; }
            bool HasStartCooldownEdit { get; }
        }
        private sealed class AmoebaJumpParameters : IAmoebaJumpAttack, INativeAttackParameters
        {
            internal readonly IAmoebaJumpAttack Source;
            private readonly MonsterNativeAttackParameters? definition;
            private readonly NativeModule? module;
            private readonly string target = "";
            public IMonsterAttackBehaviour Original => Source;
            public bool NativeOverride => module != null;
            public bool HasStartCooldownEdit => module == null ? definition?.StartOnCooldown.HasValue == true :
                module.Ready && module.nativeAttackHookConflict == null && !double.IsNaN(module.nativeOverrides.Resolve(target,
                    NativeAttackValidation.Field(AttackId.ToString(), "startOnCooldown"), double.NaN));
            internal AmoebaJumpParameters(IAmoebaJumpAttack source, MonsterNativeAttackParameters value) { Source = source; definition = value; }
            internal AmoebaJumpParameters(IAmoebaJumpAttack source, NativeModule module, string target) { Source = source; this.module = module; this.target = target; }
            private float Read(string field, float baseline, float? configured) => module == null ? configured ?? baseline :
                !module.Ready || module.nativeAttackHookConflict != null ? baseline :
                (float)module.nativeOverrides.Resolve(target, NativeAttackValidation.Field(AttackId.ToString(), field), baseline);
            public MonsterAttackId AttackId => Source.AttackId;
            public bool CDOnStart => Read("startOnCooldown", Source.CDOnStart ? 1 : 0,
                definition?.StartOnCooldown.HasValue == true ? (definition.StartOnCooldown.Value ? 1f : 0f) : (float?)null) != 0;
            public bool HasReadyAction => Source.HasReadyAction;
            public float MaxJumpHeight => Source.MaxJumpHeight;
            public float CDDuration => Read("cooldownGameSeconds", Source.CDDuration, definition?.CooldownGameSeconds);
            public float Damage => Read("damage", Source.Damage, definition?.Damage);
            public float CriticalRate => Read("criticalRate", Source.CriticalRate, definition?.CriticalRate);
            public float ReadyDuration => Read("windupGameSeconds", Source.ReadyDuration, definition?.WindupGameSeconds);
            public float AttackRange => Read("amoebaJump.range", Source.AttackRange, definition?.AmoebaJump?.Range);
            public float JumpSpeed => Read("amoebaJump.speed", Source.JumpSpeed, definition?.AmoebaJump?.Speed);
            public int JumpDistance => (int)Read("amoebaJump.distanceCells", Source.JumpDistance, definition?.AmoebaJump?.DistanceCells);
        }
        private static readonly FieldInfo AmoebaProtoField = AccessTools.Field(typeof(MonsterAttackBehaviourAmoebaJumpAttack), "_proto");
        private static readonly FieldInfo AttackProtoField = AccessTools.Field(typeof(MonsterAttackBehaviour), "_proto");
        private static readonly FieldInfo BulletProtoField = AccessTools.Field(typeof(MonsterAttackBehaviourBullet), "_proto");
        private static readonly FieldInfo[] RandomShootParameterHolders = { AttackProtoField, BulletProtoField, AccessTools.Field(typeof(MonsterAttackBehaviourRandomShoot), "_proto") };
        private static readonly FieldInfo ShootProtoField = AccessTools.Field(typeof(MonsterAttackBehaviourShoot), "_proto");
        private static readonly FieldInfo MultiShootProtoField = AccessTools.Field(typeof(MonsterAttackBehaviourMultiShoot), "_multiShoot");
        private static readonly FieldInfo SalvoShootProtoField = AccessTools.Field(typeof(MonsterAttackBehaviourSalvoShoot), "_proto");
        private static readonly FieldInfo ScatteringShootProtoField = AccessTools.Field(typeof(MonsterAttackBehaviourScatteringShoot), "_proto");
        private static readonly FieldInfo DragonShootProtoField = AccessTools.Field(typeof(MonsterAttackBehaviourDragonShoot), "_proto");
        private static readonly FieldInfo ScatteringThrowProtoField = AccessTools.Field(typeof(MonsterAttackBehaviourScatteringThrow), "_scatteringThrow");
        private static readonly FieldInfo BombingDiveProtoField = AccessTools.Field(typeof(MonsterAttackBehaviourBombingDive), "_proto");
        private static readonly FieldInfo ChomperBiteProtoField = AccessTools.Field(typeof(MonsterAttackBehaviourChomperBite), "_proto");
        private static readonly FieldInfo[] ChomperBiteParameterHolders = { AttackProtoField, ChomperBiteProtoField };
        private static readonly FieldInfo StingProtoField = AccessTools.Field(typeof(MonsterAttackBehaviourSting), "_proto");
        private static readonly FieldInfo[] StingParameterHolders = { AttackProtoField, StingProtoField };
        private static readonly FieldInfo BulletTrainParamsField = AccessTools.Field(typeof(MonsterAttackBehaviourBulletTrain), "_params");
        private static readonly FieldInfo[] BulletTrainParameterHolders = { AttackProtoField, BulletProtoField, BulletTrainParamsField };
        private static readonly FieldInfo[] CallParameterHolders = { AttackProtoField, AccessTools.Field(typeof(MonsterAttackBehaviourCall), "_proto") };
        private static readonly FieldInfo SkillAttackProtoField = AccessTools.Field(typeof(MonsterAttackBehaviourSkill), "_proto");
        private static readonly FieldInfo[] LandmineParameterHolders = { AttackProtoField, SkillAttackProtoField, AccessTools.Field(typeof(MonsterAttackBehaviourSkillLandmine), "_proto") };
        private static readonly FieldInfo[] TimeBombParameterHolders = { AttackProtoField, SkillAttackProtoField, AccessTools.Field(typeof(MonsterAttackBehaviourSkillTimeBomb), "_proto") };
        private static readonly FieldInfo ThunderProtoField = AccessTools.Field(typeof(MonsterAttackBehaviourSkillThunderPile), "proto");
        private static readonly FieldInfo[] ThunderParameterHolders = { AttackProtoField, SkillAttackProtoField, ThunderProtoField };
        private static readonly FieldInfo MimicryProtoField = AccessTools.Field(typeof(MonsterAttackBehaviourSkillChomperMimicry), "_proto");
        private static readonly FieldInfo[] MimicryParameterHolders = { AttackProtoField, SkillAttackProtoField, MimicryProtoField };
        private static readonly FieldInfo ShadowImpactProtoField = AccessTools.Field(typeof(MonsterAttackBehaviourShadowImpact), "_proto");
        private static readonly FieldInfo[] ShadowImpactParameterHolders = { AttackProtoField, SkillAttackProtoField, ShadowImpactProtoField };
        private static readonly FieldInfo[] MultiImpactParameterHolders = { AttackProtoField, AccessTools.Field(typeof(MonsterAttackBehaviourMultiImpact), "_proto") };
        private static readonly FieldInfo ImpactProtoField = AccessTools.Field(typeof(MonsterAttackBehaviourImpact), "_proto");
        private static readonly FieldInfo[] ImpactParameterHolders = { AttackProtoField, ImpactProtoField };
        private static readonly FieldInfo FungusShootProtoField = AccessTools.Field(typeof(MonsterAttackBehaviourFungusShoot), "fungusShoot");
        private static readonly FieldInfo[] FungusShootParameterHolders = { AttackProtoField, BulletProtoField, FungusShootProtoField };
        private static readonly FieldInfo BatteringProtoField = AccessTools.Field(typeof(MonsterAttackBehaviourBattering), "proto");
        private static readonly FieldInfo[] BatteringParameterHolders = { AttackProtoField, BatteringProtoField };
        private static readonly FieldInfo BatteringBulletProtoField = AccessTools.Field(typeof(MonsterAttackBehaviourBatteringBullet), "proto");
        private static readonly FieldInfo[] BatteringBulletParameterHolders = { AttackProtoField, BulletProtoField, BatteringBulletProtoField };
        private static readonly FieldInfo[] AmoebaParameterHolders = { AttackProtoField, AmoebaProtoField };
        private static readonly FieldInfo[] ShootParameterHolders = { AttackProtoField, BulletProtoField, ShootProtoField };
        private static readonly FieldInfo[] MultiShootParameterHolders = { AttackProtoField, BulletProtoField, MultiShootProtoField };
        private static readonly FieldInfo[] SalvoShootParameterHolders = { AttackProtoField, BulletProtoField, SalvoShootProtoField };
        private static readonly FieldInfo[] ScatteringShootParameterHolders = { AttackProtoField, BulletProtoField, ScatteringShootProtoField };
        private static readonly FieldInfo[] DragonShootParameterHolders = { AttackProtoField, BulletProtoField, DragonShootProtoField };
        private static readonly FieldInfo[] ScatteringThrowParameterHolders = { AttackProtoField, BulletProtoField, ScatteringThrowProtoField };
        private static readonly FieldInfo[] BombingDiveParameterHolders = { AttackProtoField, BulletProtoField, BombingDiveProtoField };
        private static FieldInfo[]? NativeParameterHolders(MonsterAttackBehaviour attack) => attack is MonsterAttackBehaviourAmoebaJumpAttack ? AmoebaParameterHolders :
            attack is MonsterAttackBehaviourRandomShoot ? RandomShootParameterHolders : attack is MonsterAttackBehaviourShoot ? ShootParameterHolders : attack is MonsterAttackBehaviourMultiShoot ? MultiShootParameterHolders :
            attack is MonsterAttackBehaviourSalvoShoot ? SalvoShootParameterHolders : attack is MonsterAttackBehaviourScatteringShoot ? ScatteringShootParameterHolders : attack is MonsterAttackBehaviourDragonShoot ? DragonShootParameterHolders : attack is MonsterAttackBehaviourScatteringThrow ? ScatteringThrowParameterHolders : attack is MonsterAttackBehaviourBombingDive ? BombingDiveParameterHolders : attack is MonsterAttackBehaviourBatteringBullet ? BatteringBulletParameterHolders : attack is MonsterAttackBehaviourBattering ? BatteringParameterHolders : attack is MonsterAttackBehaviourFungusShoot ? FungusShootParameterHolders : attack is MonsterAttackBehaviourSting ? StingParameterHolders : attack is MonsterAttackBehaviourMultiImpact ? MultiImpactParameterHolders : attack is MonsterAttackBehaviourImpact ? ImpactParameterHolders : attack is MonsterAttackBehaviourBulletTrain ? BulletTrainParameterHolders : attack is MonsterAttackBehaviourShadowImpact ? ShadowImpactParameterHolders : attack is MonsterAttackBehaviourCall ? CallParameterHolders : attack is MonsterAttackBehaviourSkillLandmine ? LandmineParameterHolders : attack is MonsterAttackBehaviourSkillTimeBomb ? TimeBombParameterHolders : attack is MonsterAttackBehaviourSkillThunderPile ? ThunderParameterHolders : attack is MonsterAttackBehaviourSkillChomperMimicry ? MimicryParameterHolders : attack is MonsterAttackBehaviourChomperBite ? ChomperBiteParameterHolders : null;
        private sealed class NativeAttackParameterState
        {
            internal MonsterAttackBehaviour Attack = null!;
            internal INativeAttackParameters Parameters = null!;
            internal FieldInfo[] Holders = null!;
            internal MonsterAttackBehaviourManager<MonsterAttackId> Manager = null!;
        }
        private readonly Dictionary<MonsterController, List<NativeAttackParameterState>> nativeAttackParameters = new Dictionary<MonsterController, List<NativeAttackParameterState>>();
        private readonly HashSet<string> nativeAttackTargets = new HashSet<string>(StringComparer.Ordinal);
        private MonsterDiagnostic? nativeAttackHookConflict;

        private static MonsterNativeAttackInfo[] DescribeNativeAttacks(Template template, bool encounterPart = false)
        {
            bool part = IsParameterPart(template);
            bool definition = template.Errors.Count == 0 || encounterPart && part;
            bool native = (template.Errors.Count == 0 || part) && current?.nativeAttackHookConflict == null;
            return (template.Proto.AttackBehaviourProtos ?? Array.Empty<IMonsterAttackBehaviour>())
                // MultiImpact has a dedicated adapter; its inherited Arc/Path fields are never consumed.
                .Where(a => a != null && a.AttackId != MonsterAttackId.Unset).GroupBy(a => a.AttackId)
                .Where(g => g.Count() == 1 && (g.First() is IRandomShoot || g.First() is IMonsterAttackBehaviourCall || g.First() is IAmoebaJumpAttack || g.First() is IShoot || g.First() is IMultiShoot || g.First() is ISalvoShoot || g.First() is IScatteringShoot || g.First() is IDragonShoot || g.First() is IScatteringThrow || g.First() is IBombingDive || g.First() is IBatteringBullet || g.First() is IBattering || g.First() is IFungusShoot || g.First() is ISting || g.First() is IImpact || g.First() is IBulletTrain || g.First() is IShadowImpact || g.First() is ILandmine || g.First() is ITimeBomb || g.First() is IThunderPile || g.First() is IChomperMimicry || g.First() is IChomperBite)).Select(g => {
                    if (g.First() is IMonsterAttackBehaviourCall call) return DescribeCall(call, definition, native);
                    if (g.First() is IRandomShoot random) return DescribeRandomShoot(random, definition, native);
                    if (g.First() is IShoot shoot) return DescribeShoot(shoot, definition, native);
                    if (g.First() is IMultiShoot multi) return DescribeMultiShoot(multi, definition, native);
                    if (g.First() is ISalvoShoot salvo) return DescribeSalvoShoot(salvo, definition, native);
                    if (g.First() is IScatteringShoot scattering) return DescribeScatteringShoot(scattering, definition, native);
                    if (g.First() is IDragonShoot dragon) return DescribeDragonShoot(dragon, definition, native);
                    if (g.First() is IScatteringThrow scatteringThrow) return DescribeScatteringThrow(scatteringThrow, definition, native);
                    if (g.First() is IBombingDive dive) return DescribeBombingDive(dive, definition, native);
                    if (g.First() is IBatteringBullet battering) return DescribeBatteringBullet(battering, definition, native);
                    if (g.First() is IChomperBite bite) return DescribeChomperBite(bite, definition, native);
                    if (g.First() is ISting sting) return DescribeSting(sting, definition, native);
                    if (g.First() is IBulletTrain train) return DescribeBulletTrain(train, definition, native);
                    if (g.First() is ILandmine || g.First() is ITimeBomb) return DescribePlaced((IMonsterAttackBehaviourSkill)g.First(), definition, native);
                    if (g.First() is IThunderPile thunder) return DescribeThunder(thunder, definition, native);
                    if (g.First() is IChomperMimicry mimicry) return DescribeMimicry(mimicry, definition, native);
                    if (g.First() is IShadowImpact shadow) return DescribeShadowImpact(shadow, definition, native);
                    if (g.First() is IMultiImpact repeated) return DescribeMultiImpact(repeated, definition, native);
                    if (g.First() is IImpact impact) return DescribeImpact(impact, definition, native);
                    if (g.First() is IFungusShoot fungus) return DescribeFungusShoot(fungus, definition, native);
                    if (g.First() is IBattering body) return DescribeBattering(body, definition, native);
                    var a = (IAmoebaJumpAttack)g.First();
                    MonsterNativeAttackFieldInfo F(string field, double value, string unit, string at) => new MonsterNativeAttackFieldInfo(field, value, unit, at, definition, native);
                    return new MonsterNativeAttackInfo(a.AttackId.ToString(), "amoeba-jump", new[] {
                        F("cooldownGameSeconds", a.CDDuration, "game-seconds", "definition: registration; native override: next available attack; current cooldown keeps its remaining time"),
                        F("startOnCooldown", a.CDOnStart ? 1 : 0, "boolean", "native attack registration at spawn/reentry; no change to an existing live cooldown"),
                        F("damage", a.Damage, "damage", "native jump hit-window opening; an already-open window retains its value"),
                        F("criticalRate", a.CriticalRate, "fraction", "native jump hit-window opening"),
                        F("windupGameSeconds", a.ReadyDuration, "game-seconds", "each native windup update"),
                        F("amoebaJump.range", a.AttackRange, "world-units", "native target-range query"),
                        F("amoebaJump.speed", a.JumpSpeed, "world-units-per-game-second", "native tween creation; existing tween duration is unchanged"),
                        F("amoebaJump.distanceCells", a.JumpDistance, "map-cells", "native GetJumpPoints after windup") },
                        "MaxJumpHeight belongs to an unused alternate path; actual height search is fixed at 4. HasReadyAction does not gate this attack. Mover jump speed is separate.");
                }).Select(info => WithAttackSounds(template, info, definition, template.Errors.Count == 0 || part)).ToArray();
        }
        private static string NativeAttackFingerprint(MonsterNativeAttackInfo a) => a.AttackId + ":" + a.Kind + ":" + a.Limitation + ":" +
            string.Join(";", a.Sounds.Select(s => s.Field + ":" + s.ReadAt + ":" + s.Definition + ":" + s.NativeOverride)) + ":" +
            string.Join(";", a.Fields.Select(f => f.Field + ":" + f.Value.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ":" + f.Unit + ":" + f.ReadAt + ":" + f.Definition + ":" + f.NativeOverride));
        private void InstallNativeAttackParameterHooks()
        {
            Patch(AccessTools.Method(typeof(MonsterAttackBehaviourManager<MonsterAttackId>), "Attack"), prefix: nameof(NativeAttackCooldown));
            Patch(AccessTools.Method(typeof(MonsterAttackBehaviourManager<MonsterAttackId>), "RegisterAttackBehaviour"), postfix: nameof(NativeAttackRegistered));
            Patch(AccessTools.DeclaredMethod(typeof(MonsterAttackBehaviourMultiShoot), "Invoke"), prefix: nameof(MultiShootStarting));
            Patch(AccessTools.DeclaredMethod(typeof(MonsterAttackBehaviourFungusShoot), "Invoke"), postfix: nameof(FungusShootStarted));
            Patch(AccessTools.DeclaredMethod(typeof(MonsterAttackBehaviourChomperBite), "Invoke"), postfix: nameof(ChomperBiteStarted));
        }
        private void CheckNativeAttackParameterHooks()
        {
            // Only shared manager entry points can reject all attack parameter overrides.
            // A patch on one concrete attack does not establish a conflict for other kinds.
            var methods = new[] { AccessTools.Method(typeof(MonsterAttackBehaviourManager<MonsterAttackId>), "Attack"),
                AccessTools.Method(typeof(MonsterAttackBehaviourManager<MonsterAttackId>), "RegisterAttackBehaviour") };
            var owners = methods.SelectMany(method =>
                Harmony.GetPatchInfo(method)?.Owners.AsEnumerable() ?? Enumerable.Empty<string>())
                .Where(o => o != HarmonyOwner).Distinct().OrderBy(o => o, StringComparer.Ordinal).ToArray();
            nativeAttackHookConflict = owners?.Length > 0 ? new MonsterDiagnostic("ExternalOverrideHookConflict",
                "Native attack parameter boundary has unmanaged patches: " + string.Join(", ", owners), field: "fields.nativeAttacks") : null;
        }
        private double? NativeAttackBaseline(string target, string field)
        {
            if (!templates.TryGetValue(target, out var template)) return null;
            foreach (var slot in DescribeNativeAttacks(template))
                foreach (var value in slot.Fields) if (NativeAttackValidation.Field(slot.AttackId, value.Field) == field) return value.Value;
            return null;
        }
        private void RefreshNativeAttackParameters()
        {
            var desired = nativeAttackHookConflict == null ? new HashSet<string>(nativeOverrides.NativeAttackTargets, StringComparer.Ordinal) : new HashSet<string>();
            foreach (var controller in nativeAttackParameters.Keys.ToArray())
                if (controller == null || controller.Monster == null || !desired.Contains(controller.Monster.proto.Name)) RestoreNativeAttackParameters(controller);
            bool added = desired.Except(nativeAttackTargets).Any();
            nativeAttackTargets.Clear(); nativeAttackTargets.UnionWith(desired);
            if (added) foreach (var controller in UnityEngine.Object.FindObjectsOfType<MonsterController>()) ApplyNativeAttackParameters(controller);
        }
        private void ApplyNativeAttackParameters(MonsterController controller)
        {
            if (controller.Monster == null || !nativeAttackTargets.Contains(controller.Monster.proto.Name) || controllers.ContainsKey(controller)) return;
            foreach (var attack in controller.attackBehaviourManager.AttackBehaviours)
                BindNativeAttackParameters(controller, attack);
        }
        private bool NativeAttackHoldersMatch(Template template)
        {
            foreach (var controller in UnityEngine.Object.FindObjectsOfType<MonsterController>())
            {
                if (controller.Monster?.proto.Name != template.Proto.Name || controllers.ContainsKey(controller)) continue;
                foreach (var attack in controller.attackBehaviourManager.AttackBehaviours)
                {
                    var holders = NativeParameterHolders(attack);
                    if (holders == null) continue;
                    if (holders.Any(f => !ReferenceEquals(attack._proto, f.GetValue(attack)))) return false;
                    var raw = attack._proto is INativeAttackParameters p ? p.Original : attack._proto;
                    if (!template.Proto.AttackBehaviourProtos.Any(a => ReferenceEquals(a, raw))) return false;
                }
            }
            return true;
        }
        private void BindNativeAttackParameters(MonsterController controller, MonsterAttackBehaviour attack)
        {
            var holders = NativeParameterHolders(attack);
            if (holders == null || controller.Monster == null || controllers.ContainsKey(controller)) return;
            string target = controller.Monster.proto.Name;
            if (!nativeAttackTargets.Contains(target) || !templates.TryGetValue(target, out var template)) return;
            if (nativeAttackParameters.TryGetValue(controller, out var states) && states.Any(s => ReferenceEquals(s.Attack, attack))) return;
            var source = attack._proto;
            if (holders.Any(f => !ReferenceEquals(f.GetValue(attack), source)) ||
                !template.Proto.AttackBehaviourProtos.Any(a => ReferenceEquals(a, source)))
                throw new InvalidOperationException("NativeAttackParameterHolderMismatch: " + target);
            INativeAttackParameters parameters = source is IAmoebaJumpAttack jump ? (INativeAttackParameters)new AmoebaJumpParameters(jump, this, target) :
                source is IMonsterAttackBehaviourCall call ? new CallParameters(call, this, target) : source is IRandomShoot random ? new RandomShootParameters(random, this, target) : source is IShoot shoot ? new ShootParameters(shoot, this, target) : source is IMultiShoot multi ? new MultiShootParameters(multi, this, target) :
                source is ISalvoShoot salvo ? new SalvoShootParameters(salvo, this, target) : source is IScatteringShoot scattering ? new ScatteringShootParameters(scattering, this, target) : source is IDragonShoot dragon ? new DragonShootParameters(dragon, this, target) : source is IScatteringThrow throwing ? new ScatteringThrowParameters(throwing, this, target) : source is IBombingDive dive ? new BombingDiveParameters(dive, this, target) : source is IBatteringBullet battering ? new BatteringBulletParameters(battering, this, target) : source is IBattering body ? new BatteringParameters(body, this, target) : source is IFungusShoot fungus ? new FungusShootParameters(fungus, this, target) : source is ISting sting ? new StingParameters(sting, this, target) : source is IMultiImpact repeated ? new MultiImpactParameters(repeated, this, target) : source is IImpact impact ? new ImpactParameters(impact, this, target) : source is IBulletTrain train ? new BulletTrainParameters(train, this, target) : source is IShadowImpact shadow ? new ShadowImpactParameters(shadow, this, target) : source is ILandmine mine ? new LandmineParameters(mine, this, target) : source is ITimeBomb bomb ? new TimeBombParameters(bomb, this, target) : source is IThunderPile thunder ? new ThunderParameters(thunder, this, target) : source is IChomperMimicry mimicry ? new MimicryParameters(mimicry, this, target) : new ChomperBiteParameters((IChomperBite)source, this, target);
            var state = new NativeAttackParameterState { Attack = attack, Parameters = parameters, Holders = holders, Manager = controller.attackBehaviourManager };
            if (states == null) { states = new List<NativeAttackParameterState>(); nativeAttackParameters.Add(controller, states); }
            states.Add(state); // Own restoration before the first native mutation.
            foreach (var holder in holders) holder.SetValue(attack, parameters);
        }
        private static RSTimer? NativeCooldownTimer(MonsterAttackBehaviourManager<MonsterAttackId> manager, MonsterAttackId slot)
        {
            var counter = NativeAccess.Get<CDCounter<MonsterAttackId>>(manager, "counter");
            var timers = NativeAccess.Get<RSTimer[]>(counter, "timers"); int index = (int)slot;
            return index >= 0 && index < timers.Length ? timers[index] : null;
        }
        private static void NativeAttackCooldown(MonsterAttackBehaviourManager<MonsterAttackId> __instance, MonsterAttackId type, Transform target)
        {
            if (current == null || target == null || !__instance.IsAvailable(type) || !__instance.TryGetAttackBehaviour(type, out var attack) ||
                !(attack._proto is INativeAttackParameters parameters) || !parameters.NativeOverride) return;
            var timer = NativeCooldownTimer(__instance, type);
            // An unchanged interval keeps the native counter's overshoot semantics.
            if (timer != null && timer.currentInterval != parameters.CDDuration) timer.SetInterval(parameters.CDDuration);
        }
        private static void NativeAttackRegistered(MonsterAttackBehaviourManager<MonsterAttackId> __instance,
            MonsterAttackId type, MonsterAttackBehaviour behaviour, bool __result)
        {
            if (!__result || !(behaviour._proto is INativeAttackParameters parameters) ||
                !parameters.HasStartCooldownEdit || parameters.CDOnStart) return;
            // Native registration resets the timer but leaves the pool's old active-slot list intact.
            // Explicit false must clear only this slot; inherited false retains native behavior.
            var counter = NativeAccess.Get<CDCounter<MonsterAttackId>>(__instance, "counter");
            NativeAccess.Get<List<int>>(counter, "currentCdList").Remove((int)type);
        }
        private void RestoreNativeAttackParameters(MonsterController controller)
        {
            if (!nativeAttackParameters.TryGetValue(controller, out var states)) return;
            foreach (var state in states)
            {
                bool ownsAll = state.Holders.All(f => ReferenceEquals(f.GetValue(state.Attack), state.Parameters));
                foreach (var holder in state.Holders)
                    if (ReferenceEquals(holder.GetValue(state.Attack), state.Parameters)) holder.SetValue(state.Attack, state.Parameters.Original);
                if (ownsAll)
                {
                    var timer = NativeCooldownTimer(state.Manager, state.Parameters.AttackId);
                    float baseline = state.Parameters.Original.CDDuration;
                    if (timer != null && timer.currentInterval != baseline)
                    {
                        bool active = !state.Manager.IsAvailable(state.Parameters.AttackId);
                        float remaining = timer.currentInterval - NativeAccess.Get<float>(timer, "currentTime");
                        timer.SetInterval(baseline);
                        if (active) timer.Offset(baseline - remaining); // Preserve the old wait, including a longer-than-baseline remainder.
                    }
                }
                else Log("Native attack parameter holder changed externally; restored only exact owned references.");
            }
            nativeAttackParameters.Remove(controller);
        }
        private void ClearNativeAttackParameters(bool clearTargets = false)
        {
            foreach (var controller in nativeAttackParameters.Keys.ToArray()) RestoreNativeAttackParameters(controller);
            if (clearTargets) nativeAttackTargets.Clear();
        }
    }
}
