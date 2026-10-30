using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using DolocTown;
using DolocTown.Config;
using DolocTown.GameData;
using DTMAPI.MonsterContent;
using HarmonyLib;
using UnityEngine;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private sealed class NativeShipState
        {
            internal readonly InstanceScope?[] Parts = new InstanceScope?[2];
        }
        private sealed class ShipLoading
        {
            internal readonly ShipLoading? Previous;
            internal readonly InstanceScope? Scope;
            internal int Slot;
            internal ShipLoading(ShipLoading? previous, InstanceScope? scope) { Previous = previous; Scope = scope; }
        }
        private sealed class ShipGenerating
        {
            internal readonly ShipGenerating? Previous;
            internal readonly InstanceScope Parent;
            internal readonly string Target;
            internal readonly int Slot;
            internal InstanceScope? Child;
            internal ShipGenerating(ShipGenerating? previous, InstanceScope parent, string target, int slot)
            { Previous = previous; Parent = parent; Target = target; Slot = slot; }
        }
        private ShipLoading? loadingShip;
        private ShipGenerating? generatingShip;
        private bool suppressShipStoryPosition;

        private static bool IsShipDecorator(MonsterDecorator decorator) => decorator.GetType() == typeof(MonsterDecoratorSpaceShip)
            || decorator.GetType() == typeof(MonsterDecoratorSpaceShipBastion);
        private void ValidateNativeShips()
        {
            foreach (var template in templates.Values.Where(t => t.Prefab?.GetComponent<MonsterDecoratorSpaceShip>() != null))
            {
                var decorator = template.Prefab!.GetComponent<MonsterDecoratorSpaceShip>();
                string id = NativeAccess.Get<string>(decorator, "bastionId");
                string? error = NativeAccess.Get<Transform>(decorator, "bastionPosition01") == null || NativeAccess.Get<Transform>(decorator, "bastionPosition02") == null
                    ? "SpaceShip requires both native bastion mounts."
                    : !templates.TryGetValue(id, out var part) || part.Errors.Count != 0 || part.Prefab?.GetComponent<MonsterDecoratorSpaceShipBastion>() == null
                        ? "Missing or unadmitted native SpaceShip bastion: " + id : null;
                if (error != null) template.Errors.Add(new MonsterDiagnostic("AdditionalLifecycleOwner", error, templateId: template.Proto.Name,
                    stage: "Template", field: "decorator.parts", recovery: "Use the complete native ship and admitted bastion resources."));
            }
        }
        private void InstallNativeShipHooks()
        {
            Patch(AccessTools.DeclaredMethod(typeof(MonsterDecoratorSpaceShip), "OnMonsterLoaded"), prefix: nameof(ShipLoadEntering), finalizer: nameof(ShipLoadReturned));
            Patch(AccessTools.DeclaredMethod(typeof(MonsterDecoratorSpaceShip), "_CreateBastion"), transpiler: nameof(ShipGenerationAdapter));
            Patch(AccessTools.DeclaredMethod(typeof(MonsterDecoratorSpaceShipBastion), "OnDead"), prefix: nameof(ShipPartDying));
            Patch(AccessTools.DeclaredMethod(typeof(Monster), "InvokeDeadEffects"), prefix: nameof(ShipEffectsEntering), finalizer: nameof(ShipEffectsReturned));
            foreach (var method in typeof(EffectsSOSpecial_SpaceShipExplosion).GetMethods().Where(m => m.DeclaringType == typeof(EffectsSOSpecial_SpaceShipExplosion) && m.Name == "Raise"))
                Patch(method, prefix: nameof(ShipStoryPositionWriting));
        }
        private static void ShipLoadEntering(Monster monster, out ShipLoading? __state)
        {
            __state = null;
            if (current == null) return;
            current.instances.TryGetValue(monster, out var scope);
            __state = new ShipLoading(current.loadingShip, scope); current.loadingShip = __state;
            if (scope != null) { scope.Ship ??= new NativeShipState(); scope.NativeCallDepth++; }
        }
        private static Exception? ShipLoadReturned(ShipLoading? __state, Exception? __exception)
        {
            if (current == null || __state == null) return __exception;
            var module = current; module.loadingShip = __state.Previous;
            var scope = __state.Scope;
            if (scope == null) return __exception;
            scope.NativeCallDepth--;
            if (__exception == null && (!scope.Active || __state.Slot != 2 || scope.Ship!.Parts.Any(p => p?.Logical == null)))
                __exception = new InvalidOperationException("SpaceShip attachment creation did not complete.");
            if (__exception != null || scope.PendingRemoveLogic)
            {
                try { module.Revoke(scope, true); }
                catch (Exception cleanup) { __exception = __exception == null ? cleanup : new AggregateException(__exception, cleanup); }
            }
            return __exception;
        }
        private static IEnumerable<CodeInstruction> ShipGenerationAdapter(IEnumerable<CodeInstruction> instructions)
        {
            var original = AccessTools.Method(typeof(IMonsterHost), "GenerateMonster", new[] { typeof(MonsterProto), typeof(Vector2), typeof(bool) });
            var replacement = AccessTools.Method(typeof(NativeModule), nameof(GenerateShipPart));
            int count = 0;
            foreach (var instruction in instructions)
            {
                if (instruction.Calls(original)) { instruction.opcode = OpCodes.Call; instruction.operand = replacement; count++; }
                yield return instruction;
            }
            if (count != 1) throw new InvalidOperationException("SpaceShip creation boundary changed: expected one exact GenerateMonster call.");
        }
        private static Monster? GenerateShipPart(IMonsterHost host, MonsterProto proto, Vector2 position, bool render)
        {
            var module = current; var load = module?.loadingShip; var parent = load?.Scope;
            if (parent == null) return host.GenerateMonster(proto, position, render);
            if (!parent.Active || parent.PendingRemoveLogic) return null;
            int slot = load!.Slot++;
            if (slot >= 2 || !ReferenceEquals(parent.Host, host)) throw new InvalidOperationException("Unexpected SpaceShip attachment source.");
            if (slot == 1) module!.Fault("ShipSecondPart");
            var frame = new ShipGenerating(module!.generatingShip, parent, proto.Name, slot) { Child = parent.Ship!.Parts[slot] };
            // A hidden ship keeps its exact logical parts, including dead slots. Do not respawn them.
            if (frame.Child?.Logical?.IsDead == true) return frame.Child.Logical;
            if (frame.Child != null) frame.Child.NativeCallDepth++;
            module.generatingShip = frame;
            try
            {
                var result = frame.Child?.Logical;
                if (result == null) result = host.GenerateMonster(parent.Binding.PrivateParts.TryGetValue(frame.Target, out var privatePart) ? privatePart.Proto : proto, position, render);
                else if (result.Controller == null && !host._RunMonster(host.MonsterEnv, result, position)) throw new InvalidOperationException("SpaceShip part reentry failed.");
                if (frame.Child == null || !ReferenceEquals(result, frame.Child.Logical) || !frame.Child.Initialized || !frame.Child.Active)
                    throw new InvalidOperationException("SpaceShip part did not finish native initialization.");
                module.Fault("ShipPartCreated");
                return result;
            }
            finally
            {
                module.generatingShip = frame.Previous;
                if (frame.Child != null)
                {
                    frame.Child.NativeCallDepth--;
                    if (frame.Child.PendingRemoveLogic || !parent.Active) module.Revoke(frame.Child, true);
                }
            }
        }
        private void CaptureShipPart(MonsterManager manager, Monster monster)
        {
            var frame = generatingShip;
            if (frame == null || frame.Child != null || !ReferenceEquals(manager, frame.Parent.Host.DM_monster) || monster.proto.Name != (frame.Parent.Binding.PrivateParts.TryGetValue(frame.Target, out var expectedPart) ? expectedPart.Proto.Name : frame.Target)) return;
            var template = templates[frame.Target];
            var binding = frame.Parent.Binding.PrivateParts.TryGetValue(frame.Target, out var privatePart) ? privatePart : new Binding(frame.Parent.Binding.Definition, template, template.Row!, DolocConfig.Tables.TbMonster,
                DolocAPI.assets.monsters, DolocAPI.EntitySystem.GetGameEntityManager<MonsterController>(), nativeChild: true);
            var child = new InstanceScope(binding, frame.Parent.Host) { Logical = monster, ShipParent = frame.Parent, NativeCallDepth = 1 };
            frame.Child = child; frame.Parent.Ship!.Parts[frame.Slot] = child; instances.Add(monster, child);
        }
        private void RevokeShipParts(InstanceScope scope)
        {
            if (scope.Ship == null) return;
            var errors = new List<Exception>();
            foreach (var child in scope.Ship.Parts.Where(p => p != null && (!p.Removed || p.Controller != null)).ToArray())
                Attempt(errors, () => Revoke(child!, true));
            ThrowCleanup(errors);
        }
        private static bool ShipPartDying(MonsterDecoratorSpaceShipBastion __instance)
        {
            var module = current; var controller = __instance.GetComponent<MonsterController>(); var monster = controller.Monster;
            if (module == null) return true;
            if (monster == null || !monster.IsDead) return false;
            if (module.instances.TryGetValue(monster, out var scope))
            {
                if (scope.ShipDeathNotified || scope.PendingRemoveLogic) return false;
                scope.ShipDeathNotified = true;
                var parent = scope.ShipParent;
                if (parent != null && !parent.PendingRemoveLogic && parent.Controller != null)
                    parent.Controller.GetComponent<MonsterDecoratorSpaceShip>().OnBastionDead();
                return false;
            }
            // Original turrets must not choose a content-owned ship through the global first match.
            var nativeParent = monster.Host.DM_monster.AllMonsters.FirstOrDefault(m => m.Controller != null
                && m.Controller.Decorator is MonsterDecoratorSpaceShip && !module.instances.ContainsKey(m));
            (nativeParent?.Controller.Decorator as MonsterDecoratorSpaceShip)?.OnBastionDead();
            return false;
        }
        private static void ShipEffectsEntering(Monster __instance, out bool __state)
        {
            __state = current?.suppressShipStoryPosition ?? false;
            if (current != null) current.suppressShipStoryPosition = current.instances.TryGetValue(__instance, out var scope)
                && scope.Binding.Template.Prefab?.GetComponent<MonsterDecoratorSpaceShip>() != null;
        }
        private static Exception? ShipEffectsReturned(bool __state, Exception? __exception)
        { if (current != null) current.suppressShipStoryPosition = __state; return __exception; }
        private static bool ShipStoryPositionWriting() => current?.suppressShipStoryPosition != true;
    }
}
