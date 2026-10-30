using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using DolocTown;
using DTMAPI.MonsterContent;
using HarmonyLib;
using UnityEngine;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private sealed class TrafficExecution
        {
            internal readonly InstanceScope Scope;
            internal readonly MonsterHandle Handle;
            internal TrafficExecution(InstanceScope scope) { Scope = scope; Handle = scope.Handle; }
            internal bool Valid => Scope.Active && Scope.Handle.Equals(Handle) && !Scope.PendingRemoveLogic
                && Scope.Logical?.IsDead == false && Scope.Binding.Definition.State == MonsterDefinitionState.Creatable;
        }
        private readonly ConditionalWeakTable<MonsterAI_TrafficLight, TrafficExecution> trafficExecutions = new ConditionalWeakTable<MonsterAI_TrafficLight, TrafficExecution>();

        private void ValidateTrafficLights()
        {
            foreach (var template in templates.Values.Where(t => t.Ai == typeof(MonsterAI_TrafficLight)))
            {
                var decorator = template.Prefab?.GetComponent<MonsterDecoratorTrafficLight>();
                if (decorator == null || template.Prefab!.GetComponent<SpriteRenderer>() == null
                    || template.Prefab.GetComponentInChildren<MonsterSight>(true) == null
                    || new[] { "greenLightSprite", "greenLightMask", "yellowLightSprite", "yellowLightMask", "redLightSprite", "redLightMask" }
                        .Any(name => NativeAccess.Get<Sprite>(decorator, name) == null))
                    template.Errors.Add(new MonsterDiagnostic("ResourceMissing", "Traffic light requires its native decorator, sight and all three light sprites/masks.",
                        templateId: template.Proto.Name, stage: "Template", field: "decorator.trafficLight"));
            }
        }
        private void InstallTrafficLightHooks()
        {
            foreach (var name in new[] { "OnStart", "OnFixedUpdate", "OnHurt", "OnLightChanged", "OnLightReadyChange" })
                Patch(AccessTools.DeclaredMethod(typeof(MonsterAI_TrafficLight), name), prefix: nameof(TrafficEntering), finalizer: nameof(TrafficReturned));
            Patch(AccessTools.DeclaredMethod(typeof(MonsterAI), "OnUpdate"), prefix: nameof(TrafficUpdating), finalizer: nameof(TrafficReturned));
            foreach (var name in new[] { "InformTrafficLightStateChange", "InformTrafficLightReadyChange" })
                Patch(AccessTools.DeclaredMethod(typeof(MonsterEnv), name), transpiler: nameof(TrafficBroadcastAdapter));
            Patch(AccessTools.DeclaredMethod(typeof(MonsterDecoratorTrafficLight), "OnFixedUpdate"), prefix: nameof(TrafficDecorating));
        }
        private static void AiCreated(MonsterController controller, MonsterAI __result)
        {
            if (current == null || !current.controllers.TryGetValue(controller, out var scope)) return;
            if (__result is MonsterAI_TrafficLight traffic) current.trafficExecutions.Add(traffic, new TrafficExecution(scope));
            if (__result is MonsterAI_Group) current.groupExecutions.Add(__result, new GroupExecution(scope));
        }
        private static bool TrafficEntering(MonsterAI_TrafficLight __instance, out InstanceScope? __state)
        {
            __state = null;
            if (current == null || !current.trafficExecutions.TryGetValue(__instance, out var execution)) return true;
            if (!execution.Valid || !ReferenceEquals(execution.Scope.Controller?.MonsterAI, __instance)) return false;
            __state = execution.Scope; __state.NativeCallDepth++; return true;
        }
        private static bool TrafficUpdating(MonsterAI __instance, out InstanceScope? __state)
        {
            __state = null;
            return __instance is not MonsterAI_TrafficLight traffic || TrafficEntering(traffic, out __state);
        }
        private static Exception? TrafficReturned(InstanceScope? __state, Exception? __exception) => FinishTrafficCall(__state, __exception);
        private static Exception? FinishTrafficCall(InstanceScope? scope, Exception? error)
        {
            if (scope == null) return error;
            scope.NativeCallDepth--;
            if (current != null && (!scope.Active || scope.PendingRemoveLogic) && scope.NativeCallDepth == 0)
            {
                try { current.Revoke(scope, scope.PendingRemoveLogic); }
                catch (Exception cleanup) { return error == null ? cleanup : new AggregateException(error, cleanup); }
            }
            return error;
        }
        private static IEnumerable<CodeInstruction> TrafficBroadcastAdapter(IEnumerable<CodeInstruction> codes)
        {
            var getter = AccessTools.PropertyGetter(typeof(MonsterEnv), "AllMonsters"); int count = 0;
            foreach (var code in codes)
            {
                if (code.Calls(getter))
                { code.opcode = OpCodes.Call; code.operand = AccessTools.Method(typeof(NativeModule), nameof(TrafficBroadcastTargets)); count++; }
                yield return code;
            }
            if (count != 1) throw new InvalidOperationException("Traffic-light room broadcast boundary changed.");
        }
        private static IEnumerable<Monster> TrafficBroadcastTargets(MonsterEnv env)
        {
            // Native dispatch still chooses live controllers by AI type. A bounded
            // snapshot only protects its enumeration from a reentrant owner withdrawal.
            return current != null && current.instances.Values.Any(s => ReferenceEquals(s.Host.MonsterEnv, env)
                && s.Binding.Template.Ai == typeof(MonsterAI_TrafficLight)) ? env.AllMonsters.ToArray() : env.AllMonsters;
        }
        private static bool TrafficDecorating(MonsterDecoratorTrafficLight __instance) => current == null
            || !current.controllers.TryGetValue(__instance.GetComponent<MonsterController>(), out var scope) || scope.Active;
        private void CancelTrafficLight(InstanceScope scope)
        {
            if (scope.Controller?.Decorator is not MonsterDecoratorTrafficLight decorator) return;
            decorator.SetHovering(false);
            NativeAccess.Set(decorator, "_moveSpeed", 0f);
            decorator.RenderShake(false);
            Fault("TrafficLightCleanup");
        }
    }
}
