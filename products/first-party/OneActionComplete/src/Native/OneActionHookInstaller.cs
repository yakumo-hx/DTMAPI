using System;
using System.Collections.Generic;
using System.Reflection;
using global::DTMAPI.Abstractions;
using HarmonyLib;

namespace Yuuka.DTMAPI.OneActionComplete
{
    internal sealed class OneActionHookInstaller
    {
        internal const string HarmonyOwner = "dtmapi.mod.yuuka.dtmapi.oneactioncomplete";
        private const string GameBridgeOwner = "dtmapi.gamebridge.doloctown";
        private const int ExpectedPatchCount = 2;
        private readonly IMonitor monitor;
        private Harmony? harmony;

        internal OneActionHookInstaller(IMonitor monitor) => this.monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        internal bool IsInstalled { get; private set; }
        internal int InstalledPatchCount { get; private set; }

        internal void InstallAtomically()
        {
            if (IsInstalled)
                return;

            Assembly gameAssembly = typeof(DolocAPI).Assembly;
            ResolvedPatch[] patches =
            {
                Resolve(gameAssembly, "DolocTown.ToolCollider", "HandleTools", 1, nameof(OneActionCallbacks.ToolColliderHandleToolsPostfix)),
                Resolve(gameAssembly, "DolocTown.AgentStateInteract", "OnExit", 0, nameof(OneActionCallbacks.AgentStateInteractExitPostfix))
            };
            if (patches.Length != ExpectedPatchCount)
                throw new InvalidOperationException("OneActionComplete patch inventory drifted.");
            ThrowIfCompatibilityResourceOwnerIsPresent(patches[0].Target);

            harmony = new Harmony(HarmonyOwner);
            try
            {
                foreach (ResolvedPatch patch in patches)
                {
                    harmony.Patch(patch.Target, postfix: new HarmonyMethod(patch.Callback));
                    InstalledPatchCount++;
                }
                if (InstalledPatchCount != ExpectedPatchCount)
                    throw new InvalidOperationException("OneActionComplete installed an incomplete patch inventory.");
                IsInstalled = true;
                monitor.Log("OneActionComplete product-owned patches installed owner=" + HarmonyOwner + " count=" + InstalledPatchCount + ".");
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

        private static ResolvedPatch Resolve(Assembly gameAssembly, string typeName, string methodName, int parameterCount, string callbackName)
        {
            Type type = gameAssembly.GetType(typeName, throwOnError: false)
                ?? throw new TypeLoadException("OneActionComplete could not resolve native patch type " + typeName + ".");
            MethodInfo target = FindMethod(type, methodName, parameterCount)
                ?? throw new MissingMethodException(typeName, methodName + "/" + parameterCount);
            MethodInfo callback = typeof(OneActionCallbacks).GetMethod(callbackName, BindingFlags.Public | BindingFlags.Static)
                ?? throw new MissingMethodException(typeof(OneActionCallbacks).FullName, callbackName);
            return new ResolvedPatch(target, callback);
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

        private static void ThrowIfCompatibilityResourceOwnerIsPresent(MethodBase resourceTarget)
        {
            Patches? info = Harmony.GetPatchInfo(resourceTarget);
            if (info == null)
                return;
            foreach (string owner in info.Owners)
            {
                if (owner.Equals(GameBridgeOwner, StringComparison.Ordinal))
                    throw new InvalidOperationException("OneActionComplete refused to install because frozen action-completion compatibility already owns ToolCollider.HandleTools.");
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

        private readonly struct ResolvedPatch
        {
            internal ResolvedPatch(MethodInfo target, MethodInfo callback)
            {
                Target = target;
                Callback = callback;
            }
            internal MethodInfo Target { get; }
            internal MethodInfo Callback { get; }
        }
    }
}
