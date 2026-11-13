using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DTMAPI.MonsterContent;
using DolocTown;
using DolocTown.Config;
using DolocTown.GameData;
using HarmonyLib;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private void InstallHooks()
        {
            Patch(AccessTools.Method(typeof(GameManager), "OnEverythingLoaded"), postfix: nameof(ReadyPostfix));
            Patch(AccessTools.Method(typeof(GameManager), "InitGameSystems"), postfix: nameof(ReadyPostfix));
            Patch(AccessTools.Method(typeof(DolocConfig), "Reload"), postfix: nameof(ReadyPostfix));
            Patch(AccessTools.Method(typeof(MonsterAI), "Create"), prefix: nameof(AiPrefix), postfix: nameof(AiCreated));
            Patch(AccessTools.Method(typeof(MonsterManager), "CreateMonster"), postfix: nameof(LogicalCreated));
            Patch(AccessTools.Method(typeof(MonsterManager), "RemoveMonster"), prefix: nameof(LogicalRemoving));
            Patch(AccessTools.Method(typeof(MonsterManager), "Clear"), prefix: nameof(LogicalClearing), finalizer: nameof(LogicalCleared));
            Patch(AccessTools.Method(typeof(GameEntityManager<MonsterController>), "NextGameEntity", new[] { typeof(string) }), prefix: nameof(PoolPrefix), postfix: nameof(PoolPostfix));
            Patch(AccessTools.Method(typeof(MonsterController), "Run"), prefix: nameof(RunPrefix), postfix: nameof(RunPostfix), finalizer: nameof(RunFailed));
            Patch(AccessTools.Method(typeof(MonsterController), "OnFixedUpdate"), prefix: nameof(ControlledFixedPrefix));
            Patch(AccessTools.Method(typeof(MonsterController), "StopAttack"), prefix: nameof(NativeAttackStopping), postfix: nameof(NativeAttackStopped));
            Patch(AccessTools.Method(typeof(MonsterController), "OnRecycle"), prefix: nameof(RecyclePrefix), postfix: nameof(RecyclePostfix), transpiler: nameof(RecycleSoundTranspiler));
            Patch(AccessTools.Method(typeof(MonsterAttackBehaviourHelper), "CreateMonsterAttackBehaviour"), prefix: nameof(NativePartAttackCreating), postfix: nameof(AttackCreated));
            Patch(AccessTools.Method(typeof(Monster), "Kill"), prefix: nameof(KillPrefix), finalizer: nameof(NianKillReturned));
            InstallCombatDamageHooks();
            InstallShotHooks();
            InstallNativeSkillHooks();
            InstallNativeCallHooks();
            InstallNativeShipHooks();
            InstallNativeNianHooks();
            InstallTrafficLightHooks();
            InstallBastionHooks();
            InstallTrainingHooks();
            InstallLifecycleHooks();
            InstallPhysicalHooks();
            InstallPresentationHooks();
            InstallSoundHooks();
            InstallSpawnHooks();
            InstallCompendiumHooks();
            InstallOverrideHooks();
        }
        private void Patch(MethodBase? method, string? prefix = null, string? postfix = null, string? finalizer = null, string? transpiler = null)
        {
            if (method == null) throw new MissingMethodException("Required native method missing.");
            HarmonyMethod? Get(string? name, IEnumerable<HarmonyLib.Patch>? installed)
            {
                if (name == null) return null;
                var callback = typeof(NativeModule).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic);
                return installed?.Any(p => p.owner == HarmonyOwner && p.PatchMethod == callback) == true ? null : new HarmonyMethod(callback);
            }
            var patches = Harmony.GetPatchInfo(method);
            var before = Get(prefix, patches?.Prefixes);
            var after = Get(postfix, patches?.Postfixes);
            var rewrite = Get(transpiler, patches?.Transpilers);
            var finish = Get(finalizer, patches?.Finalizers);
            if (before == null && after == null && rewrite == null && finish == null) return;
            harmony.Patch(method, before, after, rewrite, finalizer: finish, ilmanipulator: null);
            Log("Hook installed " + method.DeclaringType!.FullName + "." + method.Name);
        }
        private static void ReadyPostfix(MethodBase __originalMethod) => current?.Observe(__originalMethod.DeclaringType!.Name + "." + __originalMethod.Name);
        private static bool AiPrefix(ref string name, MonsterController controller, ref MonsterAI __result)
        {
            if (current != null && current.bindings.TryGetValue(name, out var binding))
            {
                if (!binding.NativeChild && UsesControlledBehavior(binding.Definition.Definition.Behavior))
                {
                    __result = new CodeAi(current, current.controllers[controller]);
                    __result.SetController(controller);
                    return false;
                }
                name = binding.Template.Proto.Name;
            }
            return true;
        }
        private static void LogicalCreated(MonsterManager __instance, Monster __result)
        {
            var module = current;
            module?.CaptureCallChild(__instance, __result);
            module?.CaptureShipPart(__instance, __result);
            module?.CaptureNianPart(__instance, __result);
            if (module != null && module.creating == null && module.pendingAllocations.TryGetValue(__result.proto.Name, out var allocations) && allocations.Count > 0)
                module.allocatingMonsters.Add(__result, allocations.Dequeue());
            if (module?.creating == null || __result.proto.Name != module.creating.Binding.Definition.NativeId) return;
            module.creating.Logical = __result;
            module.instances.Add(__result, module.creating);
        }
        private static void LogicalRemoving(Monster monster)
        {
            if (current != null && monster != null && current.instances.TryGetValue(monster, out var scope)) { scope.Removed = true; scope.Active = false; if (scope.Controller == null) current.instances.Remove(monster); }
        }
        private static void LogicalClearing(MonsterManager __instance)
        {
            if (current == null) return;

            current.StopNativeCommonSounds(v => ReferenceEquals(v.Manager, __instance));

            current.StopNativeHurtSounds(v => ReferenceEquals(v.Manager, __instance));
            current.StopNativeDeadSounds(v => ReferenceEquals(v.Manager, __instance));
            current.StopNativeHummingSounds(v => ReferenceEquals(v.Manager, __instance));
            foreach (var scope in current.instances.Values.Where(s => ReferenceEquals(s.Host.DM_monster, __instance)).ToArray()) current.Revoke(scope, true);
        }
        private static Exception? LogicalCleared(MonsterManager __instance, Exception? __exception)
        {
            if (current == null || __exception != null || __instance.AllMonsters.Any()) return __exception;
            foreach (var encounter in current.encounters.Values.Where(e => ReferenceEquals(e.Manager, __instance)).ToArray()) encounter.Cleared();
            // Fault after the confirmed mutation tests incomplete native regeneration.
            current.Fault("Spawn.AfterClear");
            return __exception;
        }
        private static bool PoolPrefix(string alias, ref GameEntity __result)
        {
            if (current?.creating != null && current.creating.Binding.Definition.NativeId == alias && current.FaultStage == "PoolAcquire") { current.FaultStage = ""; __result = null!; return false; }
            return true;
        }
        private static void PoolPostfix(string alias, GameEntity __result)
        {
            var child = current?.generatingCall?.Child;
            if (child != null && child.Binding.Proto.Name == alias && __result is MonsterController childController)
            { child.Controller = childController; current!.controllers[childController] = child; }
            var part = current?.generatingShip?.Child;
            if (part != null && part.Binding.Proto.Name == alias && __result is MonsterController partController)
            { part.Controller = partController; current!.controllers[partController] = part; }
            var nianPart = current?.generatingNian?.Child;
            if (nianPart != null && nianPart.Binding.Proto.Name == alias && __result is MonsterController nianController)
            { nianPart.Controller = nianController; current!.controllers[nianController] = nianPart; }
            if (current?.creating != null && current.creating.Binding.Definition.NativeId == alias && __result is MonsterController controller)
            { current.creating.Controller = controller; current.controllers[controller] = current.creating; }
        }
        private static void RunPrefix(MonsterController __instance, Monster monster, out InstanceScope? __state)
        {
            __state = null;
            var module = current;
            module?.RestoreNativeInstanceOverrides(__instance);
            module?.RestoreUnownedTraining(__instance, monster);
            if (module == null) return;
            if (!module.instances.TryGetValue(monster, out var scope))
            {
                if (!module.bindings.TryGetValue(monster.proto.Name, out var published)) return;
                scope = new InstanceScope(published, monster.Host) { Logical = monster }; module.instances.Add(monster, scope);
            }
            var binding = scope.Binding;
            if (scope.Handle.Generation != 0 || scope.SpawnRuleId.Length == 0 && !ReferenceEquals(module.creating, scope))
            { scope.RequestedPosition = monster.position; scope.SpawnOffset = UnityEngine.Vector2.zero; }
            scope.ResolvedPosition = NativeLanding(scope.Host, binding, monster.position);
            scope.Controller = __instance; scope.Initialized = false; scope.Removed = false;
            scope.ShipDeathNotified = false;
            scope.Handle = new MonsterHandle(Guid.NewGuid(), ++module.instanceSequence); scope.Active = true;
            scope.SoundWithdrawalRental = null;
            module.handles.Add(scope.Handle, scope);
            module.controllers[__instance] = scope;
            // RunMonsters captured its array before a failed batch was compensated. Reject
            // removed siblings even if the native caller has just reassigned their Host.
            if (!scope.Host.DM_monster.AllMonsters.Contains(monster)) throw new InvalidOperationException("Removed batch member cannot be rendered.");
            if (scope.PendingRemoveLogic || binding.Definition.State != MonsterDefinitionState.Creatable) throw new InvalidOperationException("DefinitionUnavailable");
            // Traffic AI start/notification callbacks are inside Run's native continuation.
            if (binding.Template.Ai == typeof(MonsterAI_TrafficLight)) { scope.NativeCallDepth++; __state = scope; }
            module.Fault("BattleRegistered");
        }
        private static void RunPostfix(MonsterController __instance)
        {
            current?.ApplyNativeInstanceOverrides(__instance);
            if (current == null || !current.controllers.TryGetValue(__instance, out var scope)) return;
            if (scope.PendingRemoveLogic || !scope.Active) throw new InvalidOperationException("Execution retired while native Run was returning.");
            current.ValidateInstance(scope); scope.Initialized = true;
            current.CheckPlacement(scope);
            current.Fault("RunCompleted");
            current.Fault("GroupRegistered");
            current.ObserveHealthPhase(scope);
            current.BindPresentation(scope);
            current.StartBehavior(scope);
        }
        private static Exception? RunFailed(MonsterController __instance, InstanceScope? __state, Exception? __exception)
        {
            __exception = FinishTrafficCall(__state, __exception);
            if (__exception != null) current?.RestoreNativeInstanceOverrides(__instance);
            if (__exception == null || current == null || !current.controllers.TryGetValue(__instance, out var scope)) return __exception;
            // Explicit spawn and native re-entry share the same compensation. Spawn owns its result.
            if (ReferenceEquals(current.creating, scope)) return __exception;
            current.CompensateReentry(scope, __exception);
            return null;
        }
        private static void AttackCreated(MonsterAttackBehaviour __result)
        {
            if (__result == null || current == null) return;
            var controller = NativeAccess.Get<MonsterController>(__result, "_controller");
            current.BindNativeAttackParameters(controller, __result);
            if (current.controllers.TryGetValue(controller, out var scope))
            {
                scope.AcquiredAttacks.Add(__result);
                if (__result is MonsterAttackBehaviourCall call) current.callSources.Add(call, new CallSource(scope));
                if (__result is MonsterAttackBehaviourShadowImpact shadow) current.nativeShadowSources.Add(shadow, new NativeShadowSource(scope));
                if (__result is MonsterAttackBehaviourSkillChomperMimicry mimicry) current.BindNativeMimicry(scope, mimicry);
                if (__result is MonsterAttackBehaviourSkillThunderPile thunder) current.nativeThunderSources.Add(thunder, new NativeThunderSource(scope));
            }
        }
        private static bool RecyclePrefix(MonsterController __instance, out InstanceScope? __state)
        {
            __state = null;
            current?.RestoreNativeInstanceOverrides(__instance);
            if (current == null || !current.controllers.TryGetValue(__instance, out var scope)) return true;
            __state = scope;
            if (scope.Nian != null && scope.Logical?.IsDead == false)
                current.CancelNianDeathEffects(s => ReferenceEquals(s.Nian, scope.Nian));
            current.CancelActions(scope);
            if (!current.ReleaseGroups(scope)) throw new InvalidOperationException("Group broadcast still owns the retiring AI; recycling is deferred.");
            if (scope.Initialized) return true;
            if (IsSoundWithdrawal(scope))
            {
                StopWithdrawingNativeEmitter(__instance);
                DolocAPI.Sound.UnregisterGameObject(__instance.gameObject);
            }
            if (__instance.Decorator is MonsterDecoratorNian nian) nian.OnRecycle();
            // Native OnRecycle assumes an initialized AI and battle owner; unwind only acquired objects.
            foreach (var attack in scope.AcquiredAttacks.Concat(__instance.attackBehaviourManager.AttackBehaviours).Distinct()) attack.Dispose(scope.Battle);
            scope.Battle.RemoveBattleUnit(__instance);
            if (scope.Logical != null) scope.Logical.Controller = null!;
            NativeAccess.SetProperty(__instance, "Monster", null);
            __instance.gameObject.SetActive(false);
            return false;
        }
        private static void RecyclePostfix(MonsterController __instance, InstanceScope? __state)
        {
            var module = current;
            if (module == null || __state == null || !module.controllers.TryGetValue(__instance, out var scope) || !ReferenceEquals(scope, __state)) return;
            module.ForgetMovement(__state); module.CompleteRecycle(__state, __instance);
        }
        private static bool KillPrefix(Monster __instance, out InstanceScope? __state)
        {
            __state = null;
            current?.StopNativeCommonSounds(v => v.Use != "dead" && ReferenceEquals(v.Logical, __instance));

            current?.StopNativeHurtSounds(v => ReferenceEquals(v.Logical, __instance));
            current?.StopNativeHummingSounds(v => ReferenceEquals(v.Logical, __instance));
            if (current == null || !current.instances.TryGetValue(__instance, out var scope)) return true;
            if (__instance.IsDead) return false;
            if (scope.Nian != null) { __state = scope; scope.NativeCallDepth++; }
            current.CancelActions(scope);
            return true;
        }
        private static Exception? NianKillReturned(InstanceScope? __state, Exception? __exception)
        {
            if (current == null || __state == null) return __exception;
            __state.NativeCallDepth--;
            if (__state.PendingRemoveLogic || __exception != null)
            {
                try { current.Revoke(__state, true); }
                catch (Exception cleanup) { return __exception == null ? cleanup : new AggregateException(__exception, cleanup); }
            }
            return __exception;
        }
    }
}
