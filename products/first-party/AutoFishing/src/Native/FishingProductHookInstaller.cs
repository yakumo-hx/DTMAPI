using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace Yuuka.DTMAPI.AutoFishing
{
    internal sealed class FishingProductHookInstaller
    {
        internal const string HarmonyOwner = "dtmapi.mod.yuuka.dtmapi.autofishing";
        private const string CompatibilityHarmonyOwner = "dtmapi.gamebridge.doloctown.fishingcompatibility";
        internal const int ExpectedPatchCount = 22;

        private readonly FishingProductContext context;
        private Harmony? harmony;

        internal FishingProductHookInstaller(FishingProductContext context)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
        }

        internal bool IsInstalled { get; private set; }
        internal int InstalledPatchCount { get; private set; }

        internal void InstallAtomically()
        {
            if (IsInstalled)
                return;

            Assembly gameAssembly = typeof(DolocAPI).Assembly;
            PatchPlan[] plans = BuildPlans();
            if (plans.Length != ExpectedPatchCount)
                throw new InvalidOperationException("AutoFishing patch inventory drifted: expected " + ExpectedPatchCount + ", found " + plans.Length + ".");

            var resolved = new List<ResolvedPatch>(plans.Length);
            foreach (PatchPlan plan in plans)
                resolved.Add(plan.Resolve(gameAssembly));
            ThrowIfCompatibilityOwnerIsPresent(resolved);

            harmony = new Harmony(HarmonyOwner);
            try
            {
                foreach (ResolvedPatch patch in resolved)
                {
                    HarmonyMethod callback = new HarmonyMethod(patch.Callback);
                    if (patch.Kind == PatchKind.Prefix)
                        harmony.Patch(patch.Target, prefix: callback);
                    else
                        harmony.Patch(patch.Target, postfix: callback);
                    InstalledPatchCount++;
                }

                if (InstalledPatchCount != ExpectedPatchCount)
                    throw new InvalidOperationException("AutoFishing installed an incomplete patch inventory.");
                IsInstalled = true;
                context.RuntimeMonitor.Log("AutoFishing product-owned fishing patches installed owner=" + HarmonyOwner + " count=" + InstalledPatchCount + ".");
            }
            catch
            {
                UnpatchOwnedHooks();
                throw;
            }
        }

        internal void UnpatchOwnedHooks()
        {
            try
            {
                if (harmony != null)
                    UnpatchExactOwner(harmony);
            }
            finally
            {
                InstalledPatchCount = 0;
                IsInstalled = false;
            }
        }

        private static void UnpatchExactOwner(Harmony ownerHarmony)
        {
            MethodInfo? exactUnpatch = null;
            foreach (MethodInfo method in typeof(Harmony).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
            {
                ParameterInfo[] parameters = method.GetParameters();
                if (method.Name == "UnpatchAll" && parameters.Length == 1 && parameters[0].ParameterType == typeof(string))
                {
                    exactUnpatch = method;
                    break;
                }
            }
            if (exactUnpatch == null)
                throw new MissingMethodException(typeof(Harmony).FullName, "UnpatchAll(string)");
            exactUnpatch.Invoke(exactUnpatch.IsStatic ? null : ownerHarmony, new object[] { HarmonyOwner });
        }

        private static void ThrowIfCompatibilityOwnerIsPresent(IEnumerable<ResolvedPatch> patches)
        {
            foreach (ResolvedPatch patch in patches)
            {
                var patchInfo = Harmony.GetPatchInfo(patch.Target);
                if (patchInfo == null)
                    continue;
                foreach (string owner in patchInfo.Owners)
                {
                    if (!owner.Equals(CompatibilityHarmonyOwner, StringComparison.Ordinal))
                        continue;
                    throw new InvalidOperationException(
                        "AutoFishing refused to install its product patch inventory because frozen fishing compatibility owner " +
                        CompatibilityHarmonyOwner + " already patches " + patch.Target.DeclaringType?.FullName + "." + patch.Target.Name + ".");
                }
            }
        }

        private static PatchPlan[] BuildPlans()
        {
            return new[]
            {
                Postfix("DolocTown.AgentStateFishingReady", "OnEnter", 0, nameof(FishingProductCallbacks.FishingReadyEnterPostfix)),
                Postfix("DolocTown.AgentStateFishingReady", "OnPlay", 0, nameof(FishingProductCallbacks.FishingReadyPlayPostfix)),
                Postfix("DolocTown.AgentStateFishingCast", "OnEnter", 0, nameof(FishingProductCallbacks.FishingCastEnterPostfix)),
                Postfix("DolocTown.AgentStateFishingWait", "OnEnter", 0, nameof(FishingProductCallbacks.FishingWaitEnterPostfix)),
                Postfix("DolocTown.AgentStateFishingWait", "OnPlay", 0, nameof(FishingProductCallbacks.FishingWaitPlayPostfix)),
                Postfix("DolocTown.AgentStateFishingWait", "NextState", 0, nameof(FishingProductCallbacks.FishingWaitNextStatePostfix)),
                Postfix("DolocTown.FishingGameScrollBar", "StartGame", 2, nameof(FishingProductCallbacks.FishingMiniGameStartPostfix)),
                Prefix("DolocTown.FishingGameScrollBar", "UpdateGame", 1, nameof(FishingProductCallbacks.FishingMiniGameUpdatePrefix)),
                Postfix("DolocTown.FishingGameScrollBar", "UpdateGame", 1, nameof(FishingProductCallbacks.FishingMiniGameUpdatePostfix)),
                Postfix("DolocTown.FishingGameScrollBar", "StopGame", 0, nameof(FishingProductCallbacks.FishingMiniGameStopPostfix)),
                Prefix("DolocTown.DolocUserInput", "get_NormalUseTool", 0, nameof(FishingProductCallbacks.FishingInputNormalUseToolPrefix)),
                Prefix("DolocTown.DolocUserInput", "get_NormalUseToolInProgress", 0, nameof(FishingProductCallbacks.FishingInputNormalUseToolInProgressPrefix)),
                Prefix("DolocTown.DolocUserInput", "get_NormalUseItem", 0, nameof(FishingProductCallbacks.FishingInputNormalUseItemPrefix)),
                Prefix("DolocTown.DolocUserInput", "get_NormalUseItemInProgress", 0, nameof(FishingProductCallbacks.FishingInputNormalUseItemInProgressPrefix)),
                Prefix("DolocTown.DolocUserInput", "get_NormalFishing", 0, nameof(FishingProductCallbacks.FishingInputNormalFishingPrefix)),
                Prefix("DolocTown.DolocUserInput", "get_NormalFishingInProgress", 0, nameof(FishingProductCallbacks.FishingInputNormalFishingInProgressPrefix)),
                Postfix("DolocTown.FishRodRenderer", "CastHook", 0, nameof(FishingProductCallbacks.FishRodRendererCastHookPostfix)),
                Postfix("DolocTown.FishRodRenderer", "Pull", 1, nameof(FishingProductCallbacks.FishRodRendererPullPostfix)),
                Postfix("DolocTown.FishRodRenderer", "PullCancel", 0, nameof(FishingProductCallbacks.FishRodRendererPullCancelPostfix)),
                Postfix("DolocTown.AgentStateFishingPull", "OnEnter", 0, nameof(FishingProductCallbacks.FishingPullEnterPostfix)),
                Postfix("DolocTown.AgentStateFishingPull", "OnExit", 0, nameof(FishingProductCallbacks.FishingPullExitPostfix)),
                Postfix("AgentStateBase", "OnExit", 0, nameof(FishingProductCallbacks.AgentStateBaseExitPostfix))
            };
        }

        private static PatchPlan Prefix(string typeName, string methodName, int parameterCount, string callback) =>
            new PatchPlan(typeName, methodName, parameterCount, callback, PatchKind.Prefix);

        private static PatchPlan Postfix(string typeName, string methodName, int parameterCount, string callback) =>
            new PatchPlan(typeName, methodName, parameterCount, callback, PatchKind.Postfix);

        private enum PatchKind
        {
            Prefix,
            Postfix
        }

        private readonly struct PatchPlan
        {
            internal PatchPlan(string typeName, string methodName, int parameterCount, string callbackName, PatchKind kind)
            {
                TypeName = typeName;
                MethodName = methodName;
                ParameterCount = parameterCount;
                CallbackName = callbackName;
                Kind = kind;
            }

            internal string TypeName { get; }
            internal string MethodName { get; }
            internal int ParameterCount { get; }
            internal string CallbackName { get; }
            internal PatchKind Kind { get; }

            internal ResolvedPatch Resolve(Assembly gameAssembly)
            {
                Type type = gameAssembly.GetType(TypeName, throwOnError: false)
                    ?? throw new TypeLoadException("AutoFishing could not resolve native patch type " + TypeName + ".");
                MethodInfo target = FindMethod(type, MethodName, ParameterCount)
                    ?? throw new MissingMethodException(TypeName, MethodName + "/" + ParameterCount);
                MethodInfo callback = typeof(FishingProductCallbacks).GetMethod(CallbackName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                    ?? throw new MissingMethodException(typeof(FishingProductCallbacks).FullName, CallbackName);
                return new ResolvedPatch(target, callback, Kind);
            }

            private static MethodInfo? FindMethod(Type type, string name, int parameterCount)
            {
                for (Type? current = type; current != null; current = current.BaseType)
                {
                    foreach (MethodInfo method in current.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                    {
                        if (method.Name == name && method.GetParameters().Length == parameterCount)
                            return method;
                    }
                }
                return null;
            }
        }

        private readonly struct ResolvedPatch
        {
            internal ResolvedPatch(MethodInfo target, MethodInfo callback, PatchKind kind)
            {
                Target = target;
                Callback = callback;
                Kind = kind;
            }

            internal MethodInfo Target { get; }
            internal MethodInfo Callback { get; }
            internal PatchKind Kind { get; }
        }
    }
}
