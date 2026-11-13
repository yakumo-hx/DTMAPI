using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using DTMAPI.Abstractions;
using DTMAPI.Abstractions.Internal;
using DTMAPI.AnimalContent;
using DolocTown;
using HarmonyLib;
using RedSaw.AI.StateMachine;
using UnityEngine;

namespace DTMAPI.Modules.Animals
{
    internal sealed partial class NativeModule
    {
        private readonly Harmony definitionHooks = new Harmony("DTMAPI.Animals.Definitions");
        private readonly List<Definitions> definitionRegistrations = new List<Definitions>();
        private ConditionalWeakTable<object, SpriteContext> spriteContexts = new ConditionalWeakTable<object, SpriteContext>();
        private IDisposable? session;
        private bool definitionsInstalled;
        private FieldInfo? defaultAnyState;
        private MethodInfo? defaultStateType;
        private FieldInfo? machineStates;
        public IAnimalRegistration Register(AnimalDefinition[] rows, Func<string, PackageResourceReference> resource, Action guard)
        {
            guard();
            if (!definitionsInstalled)
            {
                context.AuthorizePatches(InstallDefinitions);
                definitionsInstalled = true;
            }
            if (session == null)
            {
                long? saveEpoch = context.Runtime.Snapshot.SaveSessionEpoch;
                session = context.Runtime.Subscribe(next =>
                {
                    if (saveEpoch != next.SaveSessionEpoch || next.Phase == DtmRuntimePhase.ReturningToTitle)
                        spriteContexts = new ConditionalWeakTable<object, SpriteContext>();
                    saveEpoch = next.SaveSessionEpoch;
                });
            }
            var registration = new Definitions(this, rows, resource, guard);
            definitionRegistrations.Add(registration);
            return registration;
        }
        private static Type NativeType(string name) => Type.GetType(name + ", Assembly-CSharp", true)!;
        private void Patch(string type, string method, Type[] arguments, string handler, bool prefix = false)
        {
            var target = AccessTools.Method(NativeType(type), method, arguments) ?? throw new MissingMethodException(type, method);
            Patch(target, handler, prefix);
        }
        private void Patch(MethodBase target, string handler, bool prefix = false)
        {
            var callback = AccessTools.Method(typeof(NativeModule), handler);
            var patches = Harmony.GetPatchInfo(target);
            if ((prefix ? patches?.Prefixes : patches?.Postfixes)?.Any(p => p.owner == definitionHooks.Id && p.PatchMethod == callback) == true) return;
            var patch = new HarmonyMethod(callback);
            definitionHooks.Patch(target, prefix: prefix ? patch : null, postfix: prefix ? null : patch);
        }
        private void InstallDefinitions()
        {
            Patch("DolocTown.AnimatorAsset", "TryLoadAsset", new[] { typeof(string), typeof(RuntimeAnimatorController).MakeByRefType() }, nameof(LoadAnimator), true);
            InstallDefaultAi();
            Patch("DolocTown.Animal", "OnRender", Type.EmptyTypes, nameof(RenderAnimal));
            Patch("DolocTown.Animal", "DEBUG_SetAdult", new[] { typeof(bool) }, nameof(RenderAnimal));
            Patch("DolocTown.AnimalRenderer", "OnRecycle", Type.EmptyTypes, nameof(RecycleRenderer));
            Patch("DolocTown.SpriteOverrideHandler", "TryGetModOverrideSprite", new[] { typeof(string), typeof(Sprite).MakeByRefType() }, nameof(OverrideSprite));
        }
        private void InstallDefaultAi()
        {
            try
            {
                Type state = typeof(AnimalAI.AnimalAIState);
                Type machine = typeof(StateMachine);
                var create = AccessTools.Method(typeof(AnimalAI), nameof(AnimalAI.Create), new[] { typeof(string), typeof(Animal) })
                    ?? throw new MissingMethodException(nameof(AnimalAI), "Create(string, Animal)");
                defaultAnyState = AccessTools.Field(state, "defaultAnyState")
                    ?? throw new MissingFieldException(state.FullName, "defaultAnyState");
                defaultStateType = AccessTools.Method(typeof(AnimalAI), "GetDefaultAnyState", new[] { typeof(string) })
                    ?? throw new MissingMethodException("AnimalAI", "GetDefaultAnyState(string)");
                machineStates = AccessTools.Field(machine, "states")
                    ?? throw new MissingFieldException(machine.FullName, "states");
                Patch(create, nameof(DefaultAi));
            }
            catch (Exception ex) { Warn("AI initialization", ex); }
        }
        private static void DefaultAi(Animal __1, AnimalAI __result)
        {
            var module = current;
            if (module == null) return;
            string schedule = "";
            try
            {
                if (module.FindSpecies(__1.protoName) == null) return;
                // 同物种的实例可以持有不同日程，不能再从物种表取值。
                schedule = __1.proto.ScheduleId;
                Type type = module.defaultStateType!.Invoke(null, new object[] { schedule }) as Type
                    ?? throw new InvalidOperationException("AnimalAI.GetDefaultAnyState returned no type for " + schedule);
                State target = __result.GetState(type)
                    ?? throw new InvalidOperationException("StateMachine.GetState has no " + type.FullName);
                // Create 返回时状态已全部登记，首次 Update / MakeDecision 尚未执行。
                var states = (Dictionary<Type, State>)module.machineStates!.GetValue(__result);
                foreach (State state in states.Values)
                    if (state is AnimalAI.AnimalAIState)
                        module.defaultAnyState!.SetValue(state, target);
            }
            catch (Exception ex)
            {
                module.context.Monitor.LogOnce("Animals.DefaultAi." + __1.protoName + "." + schedule,
                    "Animals AI default state (" + schedule + "): " + ex.GetBaseException().Message, LogLevel.Warn);
            }
        }
        private (Definitions Batch, AnimalDefinition Row)? FindSpecies(string species)
        {
            foreach (var batch in definitionRegistrations.AsEnumerable().Reverse())
                foreach (var row in batch.Rows)
                    if (!batch.Closed && string.Equals(row.SpeciesId, species, StringComparison.OrdinalIgnoreCase)) return (batch, row);
            return null;
        }
        private static bool LoadAnimator(string __0, ref RuntimeAnimatorController __1, ref bool __result)
        {
            var module = current; if (module == null) return true;
            foreach (var batch in module.definitionRegistrations.AsEnumerable().Reverse())
            {
                if (batch.Closed) continue;
                foreach (var row in batch.Rows)
                {
                    if (row.AnimatorMode != "assetBundle") continue;
                    bool child = row.ChildAnimatorKey.Length != 0 && string.Equals(row.ChildAnimatorKey, __0, StringComparison.OrdinalIgnoreCase);
                    if (!child && (row.AdultAnimatorKey.Length == 0 || !string.Equals(row.AdultAnimatorKey, __0, StringComparison.OrdinalIgnoreCase))) continue;
                    try { batch.Guard(); __1 = batch.Controller(row, child)!; __result = __1 != null; return false; }
                    catch (Exception ex) { module.Warn("Animator", ex); return true; }
                }
            }
            return true;
        }
        private static Component? Renderer(object animal) => Member(animal, "Renderer") as Component;
        private static void RenderAnimal(object __instance)
        {
            var module = current; if (module == null) return;
            var renderer = Renderer(__instance); if (renderer == null) return;
            var entry = module.FindSpecies(Member(__instance, "protoName") as string ?? "");
            if (entry == null || entry.Value.Row.AnimatorMode != "pngSpriteOverride") return;
            try
            {
                var type = NativeType("DolocTown.SpriteOverrideHandler");
                var handler = renderer.gameObject.GetComponent(type) ?? renderer.gameObject.AddComponent(type);
                AccessTools.Field(type, "onlyOverrideAnimatedSprites")?.SetValue(handler, true);
                AccessTools.Property(type, "onlyOverrideAnimatedSprites")?.SetValue(handler, true);
                module.spriteContexts.Remove(handler);
                module.spriteContexts.Add(handler, new SpriteContext(entry.Value.Batch, entry.Value.Row));
            }
            catch (Exception ex) { module.Warn("Sprite context", ex); }
        }
        private static void RecycleRenderer(Component __instance)
        {
            var module = current; if (module == null) return;
            var handler = __instance.gameObject.GetComponent(NativeType("DolocTown.SpriteOverrideHandler"));
            if (handler != null) module.spriteContexts.Remove(handler);
        }
        private static void OverrideSprite(object __instance, string __0, ref Sprite __1, ref bool __result)
        {
            var module = current;
            if (module == null || !module.spriteContexts.TryGetValue(__instance, out var entry)) return;
            if (entry.Batch.Closed) { module.spriteContexts.Remove(__instance); return; }
            string mapped = "";
            try
            {
                mapped = DTMAPI.RuntimeModules.Animals.AnimalNativeRules.MapSprite(entry.Row.TemplateSpritePrefix, entry.Row.CustomSpritePrefix, __0);
                if (mapped.Length == 0) return;
                var manager = AccessTools.Property(NativeType("DolocAPI"), "modManager").GetValue(null);
                var load = AccessTools.Method(manager.GetType(), "LoadSpriteFromFile", new[] { typeof(string), typeof(Sprite).MakeByRefType() });
                object?[] arguments = { mapped, null };
                __result = load.Invoke(manager, arguments) is bool success && success;
                __1 = arguments[1] as Sprite ?? null!;
            }
            catch (Exception ex) { module.Warn("PNG " + mapped, ex); }
        }
        private void Warn(string operation, Exception error) => context.Monitor.Log("Animals " + operation + ": " + error.GetBaseException().Message, LogLevel.Warn);
        private void RemoveDefinitions(Definitions registration)
        {
            registration.Closed = true;
            // Unload only the containers. Controllers already handed to Unity can stay referenced by native animals.
            foreach (var bundle in registration.Bundles.Values) if (bundle != null) bundle.Unload(false);
            registration.Bundles.Clear(); registration.Controllers.Clear();
            definitionRegistrations.Remove(registration);
            if (definitionRegistrations.Count != 0) return;
            session?.Dispose(); session = null; spriteContexts = new ConditionalWeakTable<object, SpriteContext>();
        }
        private void DisposeDefinitions() { foreach (var batch in definitionRegistrations.ToArray()) RemoveDefinitions(batch); }
        private sealed class SpriteContext
        {
            internal readonly Definitions Batch; internal readonly AnimalDefinition Row;
            internal SpriteContext(Definitions batch, AnimalDefinition row) { Batch = batch; Row = row; }
        }
        private sealed class Definitions : IAnimalRegistration
        {
            private readonly NativeModule module;
            internal readonly AnimalDefinition[] Rows; internal readonly Action Guard;
            private readonly Func<string, PackageResourceReference> resource;
            internal readonly Dictionary<string, AssetBundle> Bundles = new Dictionary<string, AssetBundle>();
            internal readonly Dictionary<string, RuntimeAnimatorController> Controllers = new Dictionary<string, RuntimeAnimatorController>();
            internal bool Closed;
            internal Definitions(NativeModule module, AnimalDefinition[] rows, Func<string, PackageResourceReference> resource, Action guard)
            { this.module = module; Rows = rows; this.resource = resource; Guard = guard; }
            internal RuntimeAnimatorController? Controller(AnimalDefinition row, bool child)
            {
                string key = child ? row.ChildAnimatorKey : row.AdultAnimatorKey;
                if (Controllers.TryGetValue(key, out var cached) && cached != null) return cached;
                if (!Bundles.TryGetValue(row.AnimatorBundle, out var bundle))
                {
                    var source = resource(row.AnimatorBundle); var metadata = source.Inspect();
                    // LoadFromMemory requires one byte array, whose length is represented by Int32.
                    using (var stream = source.Acquire(metadata, int.MaxValue)) using (var bytes = new MemoryStream())
                    { stream.CopyTo(bytes, 65536); bundle = AssetBundle.LoadFromMemory(bytes.ToArray()); }
                    if (bundle != null) Bundles.Add(row.AnimatorBundle, bundle);
                }
                RuntimeAnimatorController? result = bundle == null ? null : bundle.LoadAsset<RuntimeAnimatorController>(child ? row.ChildAnimatorAsset : row.AdultAnimatorAsset);
                if (result == null)
                {
                    string template = row.TemplateSpeciesId.Length == 0 ? row.SpeciesId : row.TemplateSpeciesId;
                    string address = "game_anim_animal_" + template + (child ? "_child" : "");
                    var get = NativeType("DolocAPI").GetMethods(BindingFlags.Public | BindingFlags.Static).Single(m => m.Name == "GetAsset" && m.IsGenericMethodDefinition && m.GetParameters().Length == 2);
                    result = get.MakeGenericMethod(typeof(RuntimeAnimatorController)).Invoke(null, new object[] { address, false }) as RuntimeAnimatorController;
                }
                if (result != null) Controllers[key] = result;
                return result;
            }
            public AnimalRegistrationSnapshot GetSnapshot() => new AnimalRegistrationSnapshot(Closed ? AnimalRegistrationState.Released : module.context.Runtime.Snapshot.Phase == DtmRuntimePhase.Starting ? AnimalRegistrationState.WaitingNative : AnimalRegistrationState.Ready);
            public void Dispose() => module.RemoveDefinitions(this);
        }
    }
}
