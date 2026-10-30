using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using DTMAPI.Abstractions;
using DTMAPI.Abstractions.Internal;
using DTMAPI.AnimalContent;
using HarmonyLib;
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
        public IAnimalRegistration Register(AnimalDefinition[] rows, Func<string, PackageResourceReference> resource, Action guard)
        {
            guard();
            if (!definitionsInstalled)
            {
                try
                {
                    context.AuthorizePatches(InstallDefinitions);
                    long? saveEpoch = context.Runtime.Snapshot.SaveSessionEpoch;
                    session = context.Runtime.Subscribe(next =>
                    {
                        if (saveEpoch != next.SaveSessionEpoch || next.Phase == DtmRuntimePhase.ReturningToTitle)
                            spriteContexts = new ConditionalWeakTable<object, SpriteContext>();
                        saveEpoch = next.SaveSessionEpoch;
                    });
                    definitionsInstalled = true;
                }
                catch { definitionHooks.UnpatchSelf(); session?.Dispose(); session = null; throw; }
            }
            var registration = new Definitions(this, rows, resource, guard);
            definitionRegistrations.Add(registration);
            return registration;
        }
        private static Type NativeType(string name) => Type.GetType(name + ", Assembly-CSharp", true)!;
        private void Patch(string type, string method, Type[] arguments, string handler, bool prefix = false)
        {
            var target = AccessTools.Method(NativeType(type), method, arguments) ?? throw new MissingMethodException(type, method);
            var patch = new HarmonyMethod(typeof(NativeModule), handler);
            definitionHooks.Patch(target, prefix: prefix ? patch : null, postfix: prefix ? null : patch);
        }
        private void InstallDefinitions()
        {
            Patch("DolocTown.AnimatorAsset", "TryLoadAsset", new[] { typeof(string), typeof(RuntimeAnimatorController).MakeByRefType() }, nameof(LoadAnimator), true);
            Patch("DolocTown.AnimalAI", "GetDefaultAnyState", new[] { typeof(string) }, nameof(DefaultAi));
            Patch("DolocTown.Animal", "OnRender", Type.EmptyTypes, nameof(RenderAnimal));
            Patch("DolocTown.Animal", "DEBUG_SetAdult", new[] { typeof(bool) }, nameof(RenderAnimal));
            Patch("DolocTown.AnimalRenderer", "OnRecycle", Type.EmptyTypes, nameof(RecycleRenderer));
            Patch("DolocTown.SpriteOverrideHandler", "TryGetModOverrideSprite", new[] { typeof(string), typeof(Sprite).MakeByRefType() }, nameof(OverrideSprite));
            var decision = AccessTools.Method(NativeType("DolocTown.AnimalAI+AnimalAIState"), "MakeDecision_FreeTime", Type.EmptyTypes);
            if (decision == null) throw new MissingMethodException("AnimalAIState.MakeDecision_FreeTime");
            var sleep = AccessTools.Method(typeof(NativeModule), nameof(SleepDecision)).MakeGenericMethod(decision.ReturnType);
            definitionHooks.Patch(decision, prefix: new HarmonyMethod(sleep));
            Patch("DolocTown.AnimalController", "OnUpdate", new[] { typeof(float) }, nameof(AfterControllerUpdate));
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
                    bool child = string.Equals(row.ChildAnimatorKey, __0, StringComparison.OrdinalIgnoreCase);
                    if (!child && !string.Equals(row.AdultAnimatorKey, __0, StringComparison.OrdinalIgnoreCase)) continue;
                    try { batch.Guard(); __1 = batch.Controller(row, child)!; __result = __1 != null; return false; }
                    catch (Exception ex) { module.Warn("Animator", ex); return true; }
                }
            }
            return true;
        }
        private static void DefaultAi(string __0, ref Type __result)
        {
            var entry = current?.FindSpecies(__0); if (entry == null) return;
            string ai = entry.Value.Row.AiTemplate.Length == 0 ? entry.Value.Row.TemplateSpeciesId : entry.Value.Row.AiTemplate;
            string name = ai == "chicken" ? "Chicken" : ai == "goat" ? "Goat" : ai == "slime" ? "Slime" : ai == "marsh_pangolin" ? "MarshPangolin" : "";
            if (name.Length != 0) __result = NativeType("DolocTown.AnimalAI+" + name + "_FreeTimeState");
        }
        private static Component? Renderer(object animal) => Member(animal, "Renderer") as Component;
        private static void RenderAnimal(object __instance)
        {
            var module = current; var renderer = Renderer(__instance); if (module == null || renderer == null) return;
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
            var handler = __instance.gameObject.GetComponent(NativeType("DolocTown.SpriteOverrideHandler"));
            if (handler != null) current?.spriteContexts.Remove(handler);
        }
        private static void OverrideSprite(object __instance, string __0, ref Sprite __1, ref bool __result)
        {
            var module = current;
            if (module == null || !module.spriteContexts.TryGetValue(__instance, out var entry)) return;
            if (entry.Batch.Closed) { module.spriteContexts.Remove(__instance); return; }
            string mapped = DTMAPI.RuntimeModules.Animals.AnimalNativeRules.MapSprite(entry.Row.TemplateSpritePrefix, entry.Row.CustomSpritePrefix, __0);
            if (mapped.Length == 0) return;
            try
            {
                var manager = AccessTools.Property(NativeType("DolocAPI"), "modManager").GetValue(null);
                var load = AccessTools.Method(manager.GetType(), "LoadSpriteFromFile", new[] { typeof(string), typeof(Sprite).MakeByRefType() });
                object?[] arguments = { mapped, null };
                __result = load.Invoke(manager, arguments) is bool success && success;
                __1 = arguments[1] as Sprite ?? null!;
            }
            catch (Exception ex) { module.Warn("PNG " + mapped, ex); }
        }
        private static bool Sleeping(object? animal)
        {
            if (animal == null || current?.FindSpecies(Member(animal, "protoName") as string ?? "") == null || !(Member(animal, "isSleep") is bool sleeping) || !sleeping) return false;
            var archive = AccessTools.Property(NativeType("DolocAPI"), "archiveHandle")?.GetValue(null) ?? AccessTools.Field(NativeType("DolocAPI"), "archiveHandle")?.GetValue(null);
            return string.Equals(Member(archive, "CurrentDayPeriodType")?.ToString(), "Night", StringComparison.OrdinalIgnoreCase);
        }
        private static bool SleepDecision<T>(object __instance, ref T __result) where T : class
        {
            if (!Sleeping(Member(__instance, "animal"))) return true;
            var wait = AccessTools.Method(typeof(T), "WaitFrames", new[] { typeof(int) });
            if (wait?.Invoke(null, new object[] { 5 }) is T task) { __result = task; return false; }
            return true;
        }
        private static void AfterControllerUpdate(object __instance)
        {
            if (!Sleeping(Member(__instance, "animal"))) return;
            string task = (Member(__instance, "CurrentTaskType") as Type)?.FullName ?? "";
            if (task == "DolocTown.AnimalMove" || task == "DolocTown.AnimalJump" || task == "DolocTown.AnimalEnterRoom") AccessTools.Method(__instance.GetType(), "StopTask", Type.EmptyTypes)?.Invoke(__instance, null);
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
            definitionHooks.UnpatchSelf(); definitionsInstalled = false;
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
                RuntimeAnimatorController? result = null;
                if (row.AnimatorMode == "assetBundle")
                {
                    if (!Bundles.TryGetValue(row.AnimatorBundle, out var bundle))
                    {
                        var source = resource(row.AnimatorBundle); var metadata = source.Inspect();
                        using (var stream = source.Acquire(metadata, 128 * 1024 * 1024)) using (var bytes = new MemoryStream())
                        { stream.CopyTo(bytes); bundle = AssetBundle.LoadFromMemory(bytes.ToArray()); }
                        if (bundle != null) Bundles.Add(row.AnimatorBundle, bundle);
                    }
                    if (bundle != null) result = bundle.LoadAsset<RuntimeAnimatorController>(child ? row.ChildAnimatorAsset : row.AdultAnimatorAsset);
                }
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
