using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using DG.Tweening;
using DolocTown;
using DolocTown.Config.Item;
using DolocTown.GameData;
using DTMAPI.MonsterContent;
using HarmonyLib;
using UnityEngine;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private sealed class NianEncounter
        {
            internal readonly InstanceScope Root;
            internal readonly List<InstanceScope> Parts = new List<InstanceScope>();
            internal bool Splitting, Split, Retired, Settled;
            internal NianEncounter(InstanceScope root) { Root = root; }
        }
        private sealed class NianDeath
        {
            internal readonly NianDeath? Previous;
            internal readonly InstanceScope? Scope;
            internal bool Entered;
            internal NianDeath(NianDeath? previous, InstanceScope? scope) { Previous = previous; Scope = scope; }
        }
        private sealed class NianGeneration
        {
            internal readonly NianGeneration? Previous;
            internal readonly NianEncounter Encounter;
            internal readonly string Target;
            internal InstanceScope? Child;
            internal NianGeneration(NianGeneration? previous, NianEncounter encounter, string target)
            { Previous = previous; Encounter = encounter; Target = target; }
        }
        private sealed class NianTailLease
        {
            internal readonly InstanceScope Scope;
            internal readonly MonsterHandle Handle;
            internal readonly bool ColliderEnabled;
            internal readonly int Layer;
            internal readonly object? Light;
            internal readonly float Intensity;
            internal NianTailLease(InstanceScope scope, NianTail tail)
            {
                Scope = scope; Handle = scope.Handle; ColliderEnabled = tail.GetComponent<Collider2D>().enabled;
                Layer = tail.gameObject.layer; Light = NativeAccess.Get(tail, "tailLight");
                if (Light != null) Intensity = (float)AccessTools.Property(Light.GetType(), "intensity").GetValue(Light);
            }
            internal bool Valid => Scope.Active && Scope.Handle.Equals(Handle) && Scope.Logical?.IsDead == false
                && !Scope.PendingRemoveLogic && Scope.Binding.Definition.State == MonsterDefinitionState.Creatable;
        }
        private sealed class NianLightState
        {
            internal readonly object Light;
            internal readonly float Intensity;
            internal readonly HashSet<Tween> Tweens = new HashSet<Tween>();
            internal Sequence? Latest;
            internal long Revision;
            internal NianLightState(object light) { Light = light; Intensity = (float)AccessTools.Property(light.GetType(), "intensity").GetValue(light); }
        }
        private readonly ConditionalWeakTable<NianTail, NianTailLease> nianTails = new ConditionalWeakTable<NianTail, NianTailLease>();
        private readonly ConditionalWeakTable<NianTailLight, NianLightState> nianLights = new ConditionalWeakTable<NianTailLight, NianLightState>();
        private readonly Dictionary<Tween, InstanceScope> nianDeathEffects = new Dictionary<Tween, InstanceScope>();
        private NianDeath? nianDeath;
        private NianGeneration? generatingNian;
        private NianLightState? creatingNianLight;

        private static bool IsNianDecorator(MonsterDecorator decorator) => decorator.GetType() == typeof(MonsterDecoratorNian);
        private void ValidateNativeNians()
        {
            var family = templates.Values.Where(t => t.Prefab?.GetComponent<MonsterDecoratorNian>() != null).ToArray();
            foreach (var template in family)
            {
                var decorator = template.Prefab!.GetComponent<MonsterDecoratorNian>();
                if (NativeAccess.Get(decorator, "nianConfig") == null || NativeAccess.Get<GameObject>(decorator, "borderRenderer") == null
                    || template.Prefab.GetComponentInChildren<MonsterSight>(true) == null)
                    template.Errors.Add(new MonsterDiagnostic("ResourceMissing", "Nian requires its config, outline and native sight.", templateId: template.Proto.Name, stage: "Template", field: "decorator.nian"));
            }
            foreach (var template in family.Where(t => NativeAccess.Get<bool>(t.Prefab!.GetComponent<MonsterDecoratorNian>(), "firstStage")))
            {
                bool complete = NativeAccess.Get<Transform>(template.Prefab!.GetComponent<MonsterDecoratorNian>(), "connectionPosition") != null
                    && new[] { "nian_tail", "nian_head" }.All(id => templates.TryGetValue(id, out var part) && part.Errors.Count == 0
                        && part.Prefab?.GetComponent<MonsterDecoratorNian>() is MonsterDecoratorNian d && !NativeAccess.Get<bool>(d, "firstStage"));
                if (!complete) template.Errors.Add(new MonsterDiagnostic("AdditionalLifecycleOwner", "Nian requires both complete native second-stage parts and its weak-tail connection.", templateId: template.Proto.Name, stage: "Template", field: "decorator.parts"));
            }
            foreach (var template in family.Where(t => !NativeAccess.Get<bool>(t.Prefab!.GetComponent<MonsterDecoratorNian>(), "firstStage")))
                template.Errors.Add(new MonsterDiagnostic("EncounterPart", "This native part is created by a nian encounter; inherit the complete nian template.", templateId: template.Proto.Name, stage: "Template", field: "decorator.parent"));
        }
        private void InstallNativeNianHooks()
        {
            Patch(AccessTools.DeclaredMethod(typeof(MonsterDecoratorNian), "OnMonsterLoaded"), prefix: nameof(NianLoading), postfix: nameof(NianLoaded));
            Patch(AccessTools.PropertyGetter(typeof(MonsterDecoratorNian), "IsLastNianInRoom"), prefix: nameof(NianLastPart));
            Patch(AccessTools.DeclaredMethod(typeof(MonsterDecoratorNian), "OnDead"), prefix: nameof(NianDying), finalizer: nameof(NianDied), transpiler: nameof(NianDeathAdapter));
            Patch(AccessTools.DeclaredMethod(typeof(MonsterDecoratorNian), "Drop"), transpiler: nameof(NianDelayAdapter));
            Patch(AccessTools.DeclaredMethod(typeof(MonsterDecoratorNian), "_InvokeDeadEffects"), transpiler: nameof(NianDeathEffectsAdapter));
            Patch(AccessTools.DeclaredMethod(typeof(NianTail), "ConfigureTail"), prefix: nameof(NianTailConfiguring), postfix: nameof(NianTailConfigured));
            foreach (var name in new[] { "OnAttacked", "OnSwordAttack", "OnFell", "OnBomb", "FixedUpdate", "OnTriggerEnter2D", "Hurt", "Drop" })
                Patch(AccessTools.DeclaredMethod(typeof(NianTail), name), prefix: nameof(NianTailInvoking));
            Patch(AccessTools.DeclaredMethod(typeof(NianTail), "Flicker"), prefix: nameof(NianTailFlickering), postfix: nameof(NianTailFlickered));
            Patch(AccessTools.DeclaredMethod(typeof(NianTailLight), "Flicker"), prefix: nameof(NianLightEntering), finalizer: nameof(NianLightReturned), transpiler: nameof(NianLightAdapter));
            Patch(AccessTools.Method(typeof(GuaranteedManager), "SpawnMonsterDropItems"), transpiler: nameof(NianDropAdapter));
            Patch(AccessTools.Method(typeof(Room), "ClearRender"), prefix: nameof(NianRoomHiding));
        }
        private static void NianLoading(MonsterDecoratorNian __instance, Monster monster)
        {
            var module = current;
            if (module == null || !module.instances.TryGetValue(monster, out var scope)) return;
            scope.Nian ??= new NianEncounter(scope);
            scope.NianFinalDeath = false;
            NativeAccess.Set(__instance, "_isWaitingForDrop", false); NativeAccess.Set(__instance, "_isGroundCheckAvailable", false);
            NativeAccess.Set(__instance, "_dropTimer", 0f); NativeAccess.Set(__instance, "_fellCount", 0);
            NativeAccess.Get<RedSaw.RSTimerLock>(__instance, "_immunityTimer").Unlock();
            __instance.InteractWithGround = false; __instance.ShowOutline = false;
            module.StopNianLights(__instance.GetComponent<MonsterController>());
        }
        private static void NianLoaded(MonsterDecoratorNian __instance)
        {
            if (current == null || !current.controllers.TryGetValue(__instance.GetComponent<MonsterController>(), out var scope)) return;
            if (NativeAccess.Get<bool>(__instance, "firstStage") && NativeAccess.Get<GameEntitySlot<NianTail>>(__instance, "_tailSlot").Fetch() == null)
                throw new InvalidOperationException("Nian weak-tail acquisition did not complete.");
            foreach (var light in __instance.GetComponentsInChildren<NianTailLight>(true)) light.OnReuse();
            current.Fault("NianLoaded");
        }
        private static bool NianLastPart(MonsterDecoratorNian __instance, ref bool __result)
        {
            var module = current; var monster = __instance.GetComponent<MonsterController>().Monster;
            if (module == null || monster == null) return true;
            if (module.instances.TryGetValue(monster, out var scope) && scope.Nian != null)
                __result = scope.NianFinalDeath && !scope.Nian.Retired;
            else __result = !monster.Host.DM_monster.AllMonsters.Any(m => !ReferenceEquals(m, monster) && m.Name.Contains("nian") && !module.instances.ContainsKey(m));
            return false;
        }
        private static bool NianDying(MonsterDecoratorNian __instance, out NianDeath? __state)
        {
            __state = null; var module = current;
            if (module == null) return true;
            module.controllers.TryGetValue(__instance.GetComponent<MonsterController>(), out var scope);
            __state = new NianDeath(module.nianDeath, scope); module.nianDeath = __state;
            if (scope?.Nian == null) return true;
            var encounter = scope.Nian;
            if (encounter.Retired || scope.PendingRemoveLogic || scope.Logical?.IsDead != true || scope.NianDeathStarted) return false;
            scope.NianDeathStarted = true; scope.NativeCallDepth++;
            __state.Entered = true;
            if (ReferenceEquals(encounter.Root, scope)) encounter.Splitting = true;
            else if (!encounter.Splitting && encounter.Split && !encounter.Settled && encounter.Parts.All(p => p.Logical?.IsDead == true))
            { scope.NianFinalDeath = true; encounter.Settled = true; }
            return true;
        }
        private static Exception? NianDied(NianDeath? __state, Exception? __exception)
        {
            var module = current;
            if (module == null || __state == null) return __exception;
            module.nianDeath = __state.Previous;
            var scope = __state.Scope; var encounter = scope?.Nian;
            if (scope == null || encounter == null || !__state.Entered) return __exception;
            if (ReferenceEquals(encounter.Root, scope))
            {
                encounter.Splitting = false;
                encounter.Split = __exception == null && !encounter.Retired && encounter.Parts.Count == 2 && encounter.Parts.All(p => p.Active && p.Initialized);
                if (!encounter.Split && __exception == null) __exception = new InvalidOperationException("Nian stage split did not complete both native parts.");
            }
            scope.NativeCallDepth--;
            if (__exception != null || scope.PendingRemoveLogic)
            {
                encounter.Retired = true;
                try { module.RevokeNianParts(encounter); }
                catch (Exception cleanup) { __exception = __exception == null ? cleanup : new AggregateException(__exception, cleanup); }
                // Kill still reads this controller for its final effects/drop. Its finalizer owns removal.
                scope.PendingRemoveLogic = true;
                scope.ActionFailure = Error(scope.Binding.Definition, "NianSplitFailed", "decorator.parts", __exception?.GetBaseException().Message ?? "Encounter withdrawn during split.");
                module.Log("Nian split rejected: " + scope.ActionFailure.Message);
            }
            return null;
        }
        private static IEnumerable<CodeInstruction> NianDeathAdapter(IEnumerable<CodeInstruction> instructions)
        {
            var call = AccessTools.Method(typeof(MonsterEnv), "CallMonster");
            var broadcast = AccessTools.Method(typeof(DolocAPI), "BroadcastString");
            int calls = 0, ids = 0, broadcasts = 0;
            foreach (var code in instructions)
            {
                if (code.Calls(call)) { code.opcode = OpCodes.Call; code.operand = AccessTools.Method(typeof(NativeModule), nameof(GenerateNianPart)); calls++; }
                else if (code.Calls(broadcast)) { code.operand = AccessTools.Method(typeof(NativeModule), nameof(NianBroadcast)); broadcasts++; }
                else if (code.opcode == OpCodes.Ldstr && Equals(code.operand, "nian"))
                { code.opcode = OpCodes.Call; code.operand = AccessTools.Method(typeof(NativeModule), nameof(NianSettlementId)); ids++; }
                yield return code;
            }
            if (calls != 2 || ids != 2 || broadcasts != 2) throw new InvalidOperationException("Nian death/identity boundary changed.");
        }
        private static string NianSettlementId() => current?.nianDeath?.Scope?.Binding.Definition.NativeId ?? "nian";
        private static void NianBroadcast(GameEventType type, string value)
        { if (current?.nianDeath?.Scope == null || type != GameEventType.CUSTOM) DolocAPI.BroadcastString(type, value); }
        private static bool GenerateNianPart(MonsterEnv env, string id, out Monster? monster)
        {
            var module = current; var parent = module?.nianDeath?.Scope;
            if (parent?.Nian == null) return env.CallMonster(id, out monster);
            var encounter = parent.Nian; monster = null;
            if (encounter.Retired || parent.PendingRemoveLogic || parent.Binding.Definition.State != MonsterDefinitionState.Creatable) return false;
            if (!ReferenceEquals(env, parent.Host.MonsterEnv) || (id != "nian_tail" && id != "nian_head")) throw new InvalidOperationException("Unexpected Nian split source.");
            if (encounter.Parts.Count == 1) module!.Fault("NianSecondPart");
            var frame = new NianGeneration(module!.generatingNian, encounter, id); module.generatingNian = frame;
            try
            {
                string nativeId = parent.Binding.PrivateParts.TryGetValue(id, out var privatePart) ? privatePart.Proto.Name : id;
                if (!env.CallMonster(nativeId, out monster) || frame.Child == null || !ReferenceEquals(monster, frame.Child.Logical)
                    || !frame.Child.Initialized || !frame.Child.Active) throw new InvalidOperationException("Nian part did not finish native initialization: " + id);
                module.Fault("NianPartCreated");
                return !encounter.Retired && !parent.PendingRemoveLogic;
            }
            finally
            {
                module.generatingNian = frame.Previous;
                if (frame.Child != null) frame.Child.NativeCallDepth--;
            }
        }
        private void CaptureNianPart(MonsterManager manager, Monster monster)
        {
            var frame = generatingNian;
            if (frame == null || frame.Child != null || !ReferenceEquals(manager, frame.Encounter.Root.Host.DM_monster) || monster.Name != (frame.Encounter.Root.Binding.PrivateParts.TryGetValue(frame.Target, out var expectedPart) ? expectedPart.Proto.Name : frame.Target)) return;
            var root = frame.Encounter.Root; var template = templates[frame.Target];
            var binding = root.Binding.PrivateParts.TryGetValue(frame.Target, out var privatePart) ? privatePart : new Binding(root.Binding.Definition, template, template.Row!, root.Binding.Table, root.Binding.Database, root.Binding.Manager, nativeChild: true,
                nativePartAttacks: root.Binding.Definition.Definition.NativeParts.FirstOrDefault(p => p.TemplateId == frame.Target)?.NativeAttacks);
            var part = new InstanceScope(binding, root.Host) { Logical = monster, Nian = frame.Encounter, NativeCallDepth = 1 };
            frame.Child = part; frame.Encounter.Parts.Add(part); instances.Add(monster, part);
        }
        private void RevokeNianParts(NianEncounter encounter)
        {
            encounter.Retired = true;
            var errors = new List<Exception>();
            foreach (var part in encounter.Parts.Where(p => !p.Removed || p.Controller != null).ToArray()) Attempt(errors, () => Revoke(part, true));
            CancelNianDeathEffects(s => ReferenceEquals(s.Nian, encounter));
            ThrowCleanup(errors);
        }
        private static IEnumerable<CodeInstruction> NianDropAdapter(IEnumerable<CodeInstruction> instructions)
        {
            var proto = AccessTools.Field(typeof(Monster), "proto");
            var getter = AccessTools.PropertyGetter(typeof(MonsterProto), "DropSpawnEntry");
            var codes = instructions.ToArray(); int count = 0;
            for (int i = 0; i < codes.Length; i++)
            {
                if (codes[i].opcode == OpCodes.Ldflda && Equals(codes[i].operand, proto))
                {
                    if (i + 1 >= codes.Length || !codes[i + 1].Calls(getter)) throw new InvalidOperationException("Native monster drop getter boundary changed.");
                    codes[i].opcode = OpCodes.Call; codes[i].operand = AccessTools.Method(typeof(NativeModule), nameof(NianDropEntry));
                    codes[i + 1].opcode = OpCodes.Nop; codes[i + 1].operand = null; count++;
                }
                yield return codes[i];
            }
            if (count != 1) throw new InvalidOperationException("Native monster drop identity boundary changed.");
        }
        private static ItemSpawnEntry NianDropEntry(Monster monster)
        {
            var proto = current != null && current.instances.TryGetValue(monster, out var scope)
                && scope.NianFinalDeath && scope.Nian != null ? scope.Nian.Root.Binding.Proto : monster.proto;
            return proto.DropSpawnEntry;
        }

        private static void NianTailConfiguring(NianTail __instance)
        {
            if (current != null && current.nianTails.TryGetValue(__instance, out var old))
            { RestoreNianTail(__instance, old); current.nianTails.Remove(__instance); }
        }
        private static void NianTailConfigured(NianTail __instance, MonsterController controller)
        {
            if (current != null && current.controllers.TryGetValue(controller, out var scope))
            { current.nianTails.Add(__instance, new NianTailLease(scope, __instance)); scope.NianTail = __instance; }
        }
        private static bool NianTailInvoking(NianTail __instance) => current == null || !current.nianTails.TryGetValue(__instance, out var lease) || lease.Valid;
        private static bool NianTailFlickering(NianTail __instance, ref Action? callback)
        {
            if (current == null || !current.nianTails.TryGetValue(__instance, out var lease)) return true;
            if (!lease.Valid) return false;
            var original = callback; callback = () => { if (lease.Valid) original?.Invoke(); }; return true;
        }
        private static void NianTailFlickered(NianTail __instance)
        {
            if (current == null || !current.nianTails.TryGetValue(__instance, out var lease) || !lease.Valid
                || NativeAccess.Get(__instance, "_flickerAnimation") is not Tween tween) return;
            var completed = tween.onComplete;
            tween.onComplete = () => { if (lease.Valid && ReferenceEquals(NativeAccess.Get(__instance, "_flickerAnimation"), tween)) completed?.Invoke(); };
        }
        private static void StopNianTail(NianTail tail)
        {
            tail.GetComponent<Collider2D>().enabled = false; tail.ShouldFollow = false;
            CancelTweens(tail);
            var shaker = NativeAccess.Get(tail, "_shaker");
            if (shaker != null && NativeAccess.Get(shaker, "tween") is Tween)
            { CancelTweens(shaker); tail.transform.position = NativeAccess.Get<Vector3>(shaker, "originalPos"); }
            var shiner = NativeAccess.Get(tail, "_shiner");
            if (shiner != null && NativeAccess.Get(shiner, "coroutine") is Coroutine coroutine)
            {
                DolocAPI.StopCoroutine(coroutine); NativeAccess.Set(shiner, "coroutine", null);
                var renderer = tail.GetComponent<SpriteRenderer>(); renderer.sharedMaterial = NativeAccess.Get<Material>(shiner, "originalMat"); renderer.color = NativeAccess.Get<Color>(shiner, "originColor");
            }
            var body = tail.GetComponent<Rigidbody2D>(); body.velocity = Vector2.zero; body.gravityScale = 0;
            NativeAccess.Set(tail, "_interactWithGround", false); tail.ShowOutline = false;
        }
        private static void RestoreNianTail(NianTail tail, NianTailLease lease)
        {
            StopNianTail(tail); tail.GetComponent<Collider2D>().enabled = lease.ColliderEnabled; tail.gameObject.layer = lease.Layer;
            if (lease.Light != null) AccessTools.Property(lease.Light.GetType(), "intensity").SetValue(lease.Light, lease.Intensity);
        }
        private void CancelNian(InstanceScope scope)
        {
            var controller = scope.Controller;
            if (controller?.Decorator is not MonsterDecoratorNian decorator) return;
            var tail = NativeAccess.Get<GameEntitySlot<NianTail>>(decorator, "_tailSlot").Fetch();
            if (tail != null)
            {
                // Pool acquisition can fail before OnMonsterLoaded/ConfigureTail binds
                // this execution. Preserve that tail's baseline before making it inert.
                if (!nianTails.TryGetValue(tail, out _)) nianTails.Add(tail, new NianTailLease(scope, tail));
                StopNianTail(tail);
            }
            StopNianLights(controller);
            NativeAccess.Set(decorator, "_isWaitingForDrop", false); NativeAccess.Set(decorator, "_isGroundCheckAvailable", false);
            decorator.InteractWithGround = false;
            Fault("NianTailCleanup");
        }
        private static IEnumerable<CodeInstruction> NianDelayAdapter(IEnumerable<CodeInstruction> instructions)
        {
            int count = 0;
            foreach (var code in instructions)
            {
                if (code.operand is MethodInfo m && m.Name == "ContinueWith" && m.GetParameters().Length == 2 && m.GetParameters()[1].ParameterType == typeof(Func<bool>))
                {
                    var call = new CodeInstruction(code.opcode, code.operand);
                    code.opcode = OpCodes.Ldarg_0; code.operand = null;
                    yield return code;
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NativeModule), nameof(NianCallback)));
                    yield return call; count++; continue;
                }
                yield return code;
            }
            if (count != 1) throw new InvalidOperationException("Nian delayed ground callback boundary changed.");
        }
        private static Func<bool> NianCallback(Func<bool> callback, MonsterDecoratorNian decorator)
        {
            var controller = decorator.GetComponent<MonsterController>();
            if (current == null || controller == null || !current.controllers.TryGetValue(controller, out var scope)) return callback;
            var handle = scope.Handle;
            return () => scope.Active && scope.Handle.Equals(handle) && scope.Logical?.IsDead == false && !scope.PendingRemoveLogic && callback();
        }
        private static bool NianLightEntering(NianTailLight __instance, object light, out NianLightState? __state)
        {
            __state = current?.creatingNianLight;
            if (current == null) return true;
            current.creatingNianLight = null;
            if (light == null) return true;
            var controller = __instance.GetComponentInParent<MonsterController>();
            if (controller != null && current.controllers.TryGetValue(controller, out var scope) && !scope.Active) return false;
            current.creatingNianLight = current.nianLights.GetValue(__instance, _ => new NianLightState(light)); return true;
        }
        private static Exception? NianLightReturned(NianLightState? __state, Exception? __exception)
        {
            if (current != null)
            {
                var state = current.creatingNianLight;
                if (__exception == null && state?.Latest is Sequence sequence)
                {
                    var completed = sequence.onComplete; long revision = state.Revision;
                    sequence.onComplete = () => { if (state.Revision == revision) completed?.Invoke(); };
                }
                current.creatingNianLight = __state;
            }
            return __exception;
        }
        private static IEnumerable<CodeInstruction> NianLightAdapter(IEnumerable<CodeInstruction> codes) => ReplaceNianSequence(codes, nameof(NianLightSequence));
        private static IEnumerable<CodeInstruction> NianDeathEffectsAdapter(IEnumerable<CodeInstruction> codes) => ReplaceNianSequence(codes, nameof(NianDeathSequence));
        private static IEnumerable<CodeInstruction> ReplaceNianSequence(IEnumerable<CodeInstruction> codes, string replacement)
        {
            int count = 0; var original = AccessTools.Method(typeof(DOTween), "Sequence");
            foreach (var code in codes) { if (code.Calls(original)) { code.operand = AccessTools.Method(typeof(NativeModule), replacement); count++; } yield return code; }
            if (count != 1) throw new InvalidOperationException("Nian sequence boundary changed: " + replacement);
        }
        private static Sequence NianLightSequence()
        {
            var sequence = DOTween.Sequence(); var state = current?.creatingNianLight;
            if (state != null) { state.Tweens.RemoveWhere(t => !t.IsActive()); state.Tweens.Add(sequence); state.Latest = sequence; }
            return sequence;
        }
        private void StopNianLights(MonsterController controller)
        {
            foreach (var light in controller.GetComponentsInChildren<NianTailLight>(true))
                if (nianLights.TryGetValue(light, out var state))
                { state.Revision++; foreach (var tween in state.Tweens) CancelTween(tween); state.Tweens.Clear(); state.Latest = null; AccessTools.Property(state.Light.GetType(), "intensity").SetValue(state.Light, state.Intensity); }
        }
        private static Sequence NianDeathSequence()
        {
            var sequence = DOTween.Sequence(); var module = current; var scope = module?.nianDeath?.Scope;
            if (module != null && scope != null)
            { module.nianDeathEffects.Add(sequence, scope); sequence.onKill += () => module.nianDeathEffects.Remove(sequence); }
            return sequence;
        }
        private void CancelNianDeathEffects(Func<InstanceScope, bool> predicate)
        { foreach (var pair in nianDeathEffects.Where(p => predicate(p.Value)).ToArray()) { CancelTween(pair.Key); nianDeathEffects.Remove(pair.Key); } }
        private static void NianRoomHiding(IMonsterHost __instance) => current?.CancelNianDeathEffects(s => ReferenceEquals(s.Host, __instance));
    }
}
