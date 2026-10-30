using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Reflection.Emit;
using DTMAPI.MonsterContent;
using DolocTown;
using DolocTown.Config;
using DolocTown.GameData;
using DolocTown.MonsterAttackBehaviours;
using HarmonyLib;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        // Attack objects can outlive a pooled controller. A weak key retains the original
        // execution identity without making a retired callback a new controller's work.
        private sealed class CallSource
        {
            internal readonly InstanceScope Scope;
            internal readonly MonsterHandle Instance;
            internal CallSource(InstanceScope scope) { Scope = scope; Instance = scope.Handle; }
            internal bool Valid => Scope.Active && !Scope.Removed && Scope.Handle.Equals(Instance)
                && Scope.Logical?.IsDead == false && Scope.Binding.Definition.State == MonsterDefinitionState.Creatable;
        }
        private sealed class CallInvocation
        {
            internal readonly CallSource? Source;
            internal readonly string Target;
            internal readonly CallInvocation? Previous;
            internal readonly List<InstanceScope> Children = new List<InstanceScope>();
            internal bool InEnvironment;
            internal CallInvocation(CallSource? source, string target, CallInvocation? previous)
            { Source = source; Target = target; Previous = previous; }
        }
        private sealed class CallGeneration
        {
            internal readonly CallGeneration? Previous;
            internal readonly CallInvocation? Call;
            internal readonly IMonsterHost Host;
            internal InstanceScope? Child;
            internal CallGeneration(CallGeneration? previous, CallInvocation? call, IMonsterHost host)
            { Previous = previous; Call = call; Host = host; }
        }
        private readonly ConditionalWeakTable<MonsterAttackBehaviourCall, CallSource> callSources = new ConditionalWeakTable<MonsterAttackBehaviourCall, CallSource>();
        private CallInvocation? calling;
        private CallGeneration? generatingCall;

        private void ValidateNativeCalls()
        {
            var checkedIds = new HashSet<string>(StringComparer.Ordinal);
            var visiting = new HashSet<string>(StringComparer.Ordinal);
            bool Visit(Template template)
            {
                if (!visiting.Add(template.Proto.Name)) return false;
                if (checkedIds.Contains(template.Proto.Name)) { visiting.Remove(template.Proto.Name); return template.Errors.Count == 0; }
                foreach (var call in (template.Proto.AttackBehaviourProtos ?? Array.Empty<IMonsterAttackBehaviour>()).OfType<IMonsterAttackBehaviourCall>().Where(a => a.AttackId != MonsterAttackId.Unset))
                {
                    string? error = !templates.TryGetValue(call.MonsterId, out var child) ? "Missing native Call target: " + call.MonsterId
                        : visiting.Contains(call.MonsterId) ? "Cyclic native Call target: " + call.MonsterId
                        : !Visit(child) ? "Call target has an unadmitted lifecycle/resource: " + call.MonsterId : null;
                    if (error != null) template.Errors.Add(new MonsterDiagnostic("AdditionalLifecycleOwner", error, templateId: template.Proto.Name,
                        stage: "Template", field: "attacks.children", recovery: "Use an acyclic native target with admitted resources and lifecycle."));
                }
                visiting.Remove(template.Proto.Name); checkedIds.Add(template.Proto.Name);
                return template.Errors.Count == 0;
            }
            foreach (var template in templates.Values) Visit(template);
        }
        private void InstallNativeCallHooks()
        {
            Patch(AccessTools.DeclaredMethod(typeof(MonsterAttackBehaviourCall), "CallMonster"), prefix: nameof(CallStarting), finalizer: nameof(CallFinished));
            Patch(AccessTools.Method(typeof(MonsterEnv), "CallMonsters"), prefix: nameof(CallEnvironmentStarting),
                finalizer: nameof(CallEnvironmentFinished), transpiler: nameof(CallGenerationAdapter));
        }
        private static IEnumerable<CodeInstruction> CallGenerationAdapter(IEnumerable<CodeInstruction> instructions)
        {
            var original = AccessTools.Method(typeof(IMonsterHost), "GenerateMonster", new[] { typeof(MonsterProto), typeof(bool) });
            var wrapper = AccessTools.Method(typeof(NativeModule), nameof(GenerateCallChild));
            int matched = 0;
            foreach (var instruction in instructions)
            {
                if (instruction.Calls(original)) { instruction.opcode = OpCodes.Call; instruction.operand = wrapper; matched++; }
                yield return instruction;
            }
            if (matched != 1) throw new InvalidOperationException("Native Call generation boundary changed: expected one exact Host.GenerateMonster call.");
        }
        private static bool CallStarting(MonsterAttackBehaviourCall __instance, out CallInvocation? __state)
        {
            __state = null;
            var module = current;
            if (module == null) return true;
            if (!module.callSources.TryGetValue(__instance, out var source))
            {
                __state = new CallInvocation(null, "", module.calling);
                module.calling = __state;
                return true;
            }
            if (!source.Valid || NativeAccess.Get(__instance, "_tween") == null) return false;
            var proto = (IMonsterAttackBehaviourCall)__instance._proto;
            __state = new CallInvocation(source, proto.MonsterId, module.calling);
            module.calling = __state;
            source.Scope.NativeCallDepth++;
            return true;
        }
        private static Exception? CallFinished(CallInvocation? __state, Exception? __exception)
        {
            var module = current;
            if (module == null || __state == null) return __exception;
            module.calling = __state.Previous;
            if (__state.Source == null) return __exception;
            var scope = __state.Source.Scope;
            scope.NativeCallDepth--;
            var errors = new List<Exception>();
            if (__exception != null || !__state.Source.Valid)
                foreach (var child in __state.Children) Attempt(errors, () => module.Revoke(child, true));
            if (scope.PendingRemoveLogic) Attempt(errors, () => module.Revoke(scope, true));
            if (__exception != null || errors.Count > 0)
                module.FailAction(scope, "NativeCall", new AggregateException("Native Call creation/cleanup failed", (__exception == null ? errors : new[] { __exception }.Concat(errors))));
            // Native attacks share the module's contained action-failure boundary.
            return null;
        }
        private static void CallEnvironmentStarting(MonsterEnv __instance, string monsterId, out bool __state)
        {
            var call = current?.calling;
            __state = call?.InEnvironment ?? false;
            if (call != null) call.InEnvironment = call.Source != null && !__state && ReferenceEquals(__instance, call.Source.Scope.Host.MonsterEnv) && monsterId == call.Target;
        }
        private static Exception? CallEnvironmentFinished(bool __state, Exception? __exception)
        { if (current?.calling != null) current.calling.InEnvironment = __state; return __exception; }
        private static Monster? GenerateCallChild(IMonsterHost host, MonsterProto proto, bool shouldRender)
        {
            var module = current;
            if (module?.calling == null) return host.GenerateMonster(proto, shouldRender);
            var call = module.calling;
            bool owned = module.generatingCall == null && call.Source != null && call.InEnvironment
                && ReferenceEquals(host, call.Source.Scope.Host) && proto.Name == call.Target;
            // Withdrawal in an earlier iteration closes the rest of this native burst.
            if (owned && !call.Source!.Valid) return null;
            var frame = new CallGeneration(module.generatingCall, owned ? call : null, host);
            module.generatingCall = frame;
            Monster? result = null;
            Exception? error = null;
            try { result = host.GenerateMonster(proto, shouldRender); }
            catch (Exception ex) { error = ex; }
            error = CallGenerationFinished(frame, result, error);
            if (error != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
            return result;
        }
        private void CaptureCallChild(MonsterManager manager, Monster monster)
        {
            var frame = generatingCall;
            if (frame?.Call == null || frame.Child != null || !ReferenceEquals(manager, frame.Host.DM_monster) || monster.proto.Name != frame.Call.Target) return;
            var template = templates[monster.proto.Name];
            var binding = new Binding(frame.Call.Source!.Scope.Binding.Definition, template, template.Row!, DolocConfig.Tables.TbMonster,
                DolocAPI.assets.monsters, DolocAPI.EntitySystem.GetGameEntityManager<MonsterController>(), nativeChild: true);
            var child = new InstanceScope(binding, frame.Host) { Logical = monster, Summoner = frame.Call.Source.Scope.Logical,
                SummonerHandle = frame.Call.Source.Instance, NativeCallDepth = 1 };
            frame.Child = child; frame.Call.Children.Add(child); instances.Add(monster, child);
        }
        private static Exception? CallGenerationFinished(CallGeneration? __state, Monster? __result, Exception? __exception)
        {
            var module = current;
            if (module == null || __state == null) return __exception;
            module.generatingCall = __state.Previous;
            var child = __state.Child;
            if (child == null) return __exception;
            child.NativeCallDepth--;
            try
            {
                if (__exception != null || !__state.Call!.Source!.Valid)
                    module.Revoke(child, true);
                else
                {
                    if (child.PendingRemoveLogic || !ReferenceEquals(child.Logical, __result) || !child.Initialized || !child.Active) throw new InvalidOperationException("Native Call child did not finish initialization.");
                    module.ValidateInstance(child);
                    module.Fault("NativeCallChildCreated");
                }
                return __exception;
            }
            catch (Exception ex) { return __exception == null ? ex : new AggregateException(__exception, ex); }
        }
    }
}
