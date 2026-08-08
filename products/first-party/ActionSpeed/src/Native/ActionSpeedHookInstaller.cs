using System;
using System.Collections.Generic;
using System.Reflection;
using global::DTMAPI.Abstractions;
using HarmonyLib;

namespace Yuuka.DTMAPI.ActionSpeed
{
    internal sealed class ActionSpeedHookInstaller
    {
        internal const string HarmonyOwner = "dtmapi.mod.yuuka.dtmapi.actionspeed";
        private const string GameBridgeOwner = "dtmapi.gamebridge.doloctown";
        private const int ExpectedPatchCount = 11;
        private readonly IMonitor monitor;
        private Harmony? harmony;

        internal ActionSpeedHookInstaller(IMonitor monitor) => this.monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        internal bool IsInstalled { get; private set; }
        internal int InstalledPatchCount { get; private set; }

        internal void InstallAtomically()
        {
            if (IsInstalled)
                return;

            Assembly gameAssembly = typeof(DolocAPI).Assembly;
            ResolvedPatch[] patches =
            {
                Resolve(gameAssembly, "DolocTown.AgentStateTool", "OnEnter", 0, nameof(ActionSpeedCallbacks.AgentStateToolEnterPostfix), PatchKind.Postfix),
                Resolve(gameAssembly, "DolocTown.AgentStateTool", "OnExit", 0, nameof(ActionSpeedCallbacks.AgentStateToolExitPostfix), PatchKind.Postfix),
                Resolve(gameAssembly, "DolocTown.AgentStateWater", "OnEnter", 0, nameof(ActionSpeedCallbacks.AgentStateWaterEnterPostfix), PatchKind.Postfix),
                Resolve(gameAssembly, "DolocTown.AgentStateWater", "OnExit", 0, nameof(ActionSpeedCallbacks.AgentStateWaterExitPostfix), PatchKind.Postfix),
                Resolve(gameAssembly, "DolocTown.AgentStateInteract", "OnEnter", 0, nameof(ActionSpeedCallbacks.AgentStateInteractEnterPostfix), PatchKind.Postfix),
                Resolve(gameAssembly, "DolocTown.AgentStateInteract", "OnExit", 0, nameof(ActionSpeedCallbacks.AgentStateInteractExitPostfix), PatchKind.Postfix),
                Resolve(gameAssembly, "DolocTown.AgentStateEat", "OnEnter", 0, nameof(ActionSpeedCallbacks.AgentStateEatEnterPostfix), PatchKind.Postfix),
                Resolve(gameAssembly, "DolocTown.AgentControllerState", "UseItemContinues", 1, nameof(ActionSpeedCallbacks.AgentControllerStateUseItemContinuesPrefix), PatchKind.Prefix),
                Resolve(gameAssembly, "DolocTown.AgentControllerState", "InteractContinues", 1, nameof(ActionSpeedCallbacks.AgentControllerStateInteractContinuesPrefix), PatchKind.Prefix),
                Resolve(gameAssembly, "DolocTown.AnimalRenderer", "OnInteract", 0, nameof(ActionSpeedCallbacks.AnimalRendererOnInteractPrefix), PatchKind.Prefix),
                Resolve(gameAssembly, "AgentStateBase", "OnExit", 0, nameof(ActionSpeedCallbacks.AgentStateBaseExitPostfix), PatchKind.Postfix)
            };
            if (patches.Length != ExpectedPatchCount)
                throw new InvalidOperationException("ActionSpeed patch inventory drifted.");

            ThrowIfCompatibilityActionSpeedOwnerIsPresent(patches);
            harmony = new Harmony(HarmonyOwner);
            try
            {
                foreach (ResolvedPatch patch in patches)
                {
                    HarmonyMethod callback = new HarmonyMethod(patch.Callback);
                    if (patch.Kind == PatchKind.Prefix)
                        harmony.Patch(patch.Target, prefix: callback);
                    else
                        harmony.Patch(patch.Target, postfix: callback);
                    InstalledPatchCount++;
                }
                if (InstalledPatchCount != ExpectedPatchCount)
                    throw new InvalidOperationException("ActionSpeed installed an incomplete patch inventory.");
                IsInstalled = true;
                monitor.Log("ActionSpeed product-owned patches installed owner=" + HarmonyOwner + " count=" + InstalledPatchCount + ".");
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

        private static ResolvedPatch Resolve(Assembly gameAssembly, string typeName, string methodName, int parameterCount, string callbackName, PatchKind kind)
        {
            Type type = gameAssembly.GetType(typeName, throwOnError: false)
                ?? throw new TypeLoadException("ActionSpeed could not resolve native patch type " + typeName + ".");
            MethodInfo target = FindMethod(type, methodName, parameterCount)
                ?? throw new MissingMethodException(typeName, methodName + "/" + parameterCount);
            MethodInfo callback = typeof(ActionSpeedCallbacks).GetMethod(callbackName, BindingFlags.Public | BindingFlags.Static)
                ?? throw new MissingMethodException(typeof(ActionSpeedCallbacks).FullName, callbackName);
            return new ResolvedPatch(target, callback, kind);
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

        private static void ThrowIfCompatibilityActionSpeedOwnerIsPresent(IEnumerable<ResolvedPatch> patches)
        {
            foreach (ResolvedPatch patch in patches)
            {
                // Shared exit targets can legitimately carry GameBridge's OneActionComplete
                // compatibility callback. Unique ActionSpeed stage targets prove whether the
                // old compatibility executor was physically installed in this process.
                if (patch.Target.Name == "OnExit")
                    continue;
                Patches? info = Harmony.GetPatchInfo(patch.Target);
                if (info == null)
                    continue;
                foreach (string owner in info.Owners)
                {
                    if (owner.Equals(GameBridgeOwner, StringComparison.Ordinal))
                        throw new InvalidOperationException("ActionSpeed refused to install because frozen compatibility already owns native ActionSpeed stage " + patch.Target.DeclaringType?.FullName + "." + patch.Target.Name + ". Restart after disabling the old Strict consumer.");
                }
            }
        }

        private static void UnpatchExactOwner(Harmony ownerHarmony)
        {
            foreach (MethodInfo method in typeof(Harmony).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
            {
                ParameterInfo[] parameters = method.GetParameters();
                if (method.Name != "UnpatchAll" || parameters.Length != 1 || parameters[0].ParameterType != typeof(string))
                    continue;
                method.Invoke(method.IsStatic ? null : ownerHarmony, new object[] { HarmonyOwner });
                return;
            }
            throw new MissingMethodException(typeof(Harmony).FullName, "UnpatchAll(string)");
        }

        private enum PatchKind { Prefix, Postfix }

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
