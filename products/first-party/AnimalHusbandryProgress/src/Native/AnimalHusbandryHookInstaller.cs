using System;
using System.Collections.Generic;
using System.Reflection;
using global::DTMAPI.Abstractions;
using HarmonyLib;

namespace Yuuka.DTMAPI.AnimalHusbandryProgress
{
    internal sealed class AnimalHusbandryHookInstaller
    {
        internal const string HarmonyOwner = "dtmapi.mod.yuuka.dtmapi.animalhusbandryprogress";
        private const string CompatibilityOwner = "dtmapi.gamebridge.doloctown";
        private const int ExpectedPatchCount = 4;
        private readonly IMonitor monitor;
        private Harmony? harmony;
        private MethodBase[] targets = Array.Empty<MethodBase>();

        internal AnimalHusbandryHookInstaller(IMonitor monitor) => this.monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        internal bool IsInstalled { get; private set; }
        internal int InstalledPatchCount { get; private set; }
        internal IReadOnlyList<MethodBase> Targets => targets;

        internal void InstallAtomically()
        {
            if (IsInstalled)
                return;

            Assembly gameAssembly = typeof(DolocAPI).Assembly;
            Type dataType = gameAssembly.GetType("DolocTown.UI.AnimalFullInfoData", false)
                ?? throw new TypeLoadException("AnimalHusbandryProgress could not resolve DolocTown.UI.AnimalFullInfoData.");
            Type viewerType = gameAssembly.GetType("DolocTown.UI.AnimalViewer", false)
                ?? throw new TypeLoadException("AnimalHusbandryProgress could not resolve DolocTown.UI.AnimalViewer.");
            Type panelType = gameAssembly.GetType("DolocTown.AnimalPanelUiState", false)
                ?? throw new TypeLoadException("AnimalHusbandryProgress could not resolve DolocTown.AnimalPanelUiState.");
            ConstructorInfo ctor = ResolveSingleArgumentConstructor(dataType);
            MethodInfo show = ResolveSingleArgumentMethod(viewerType, "Show");
            MethodInfo unregister = panelType.GetMethod("Unregister", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null)
                ?? throw new MissingMethodException(panelType.FullName, "Unregister()");
            targets = new MethodBase[] { ctor, show, unregister };
            ThrowIfCompatibilityOwnerIsPresent(targets);

            MethodInfo ctorPostfix = Callback(nameof(AnimalHusbandryCallbacks.AnimalFullInfoDataCtorPostfix));
            MethodInfo showPrefix = Callback(nameof(AnimalHusbandryCallbacks.AnimalViewerShowPrefix));
            MethodInfo showPostfix = Callback(nameof(AnimalHusbandryCallbacks.AnimalViewerShowPostfix));
            MethodInfo unregisterPostfix = Callback(nameof(AnimalHusbandryCallbacks.AnimalPanelUiStateUnregisterPostfix));
            harmony = new Harmony(HarmonyOwner);
            try
            {
                harmony.Patch(ctor, postfix: new HarmonyMethod(ctorPostfix));
                harmony.Patch(show, prefix: new HarmonyMethod(showPrefix), postfix: new HarmonyMethod(showPostfix));
                harmony.Patch(unregister, postfix: new HarmonyMethod(unregisterPostfix));
                InstalledPatchCount = ExpectedPatchCount;
                IsInstalled = true;
                monitor.Log("AnimalHusbandryProgress product-owned patches installed owner=" + HarmonyOwner + " count=" + InstalledPatchCount + " targets=" + targets.Length + ".");
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

        private static ConstructorInfo ResolveSingleArgumentConstructor(Type type)
        {
            foreach (ConstructorInfo constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                if (constructor.GetParameters().Length == 1)
                    return constructor;
            throw new MissingMethodException(type.FullName, ".ctor(one argument)");
        }

        private static MethodInfo ResolveSingleArgumentMethod(Type type, string name)
        {
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                if (method.Name == name && method.GetParameters().Length == 1)
                    return method;
            throw new MissingMethodException(type.FullName, name + "(one argument)");
        }

        private static MethodInfo Callback(string name)
            => typeof(AnimalHusbandryCallbacks).GetMethod(name, BindingFlags.Public | BindingFlags.Static)
                ?? throw new MissingMethodException(typeof(AnimalHusbandryCallbacks).FullName, name);

        private static void ThrowIfCompatibilityOwnerIsPresent(IEnumerable<MethodBase> methods)
        {
            foreach (MethodBase target in methods)
            {
                Patches? info = Harmony.GetPatchInfo(target);
                if (info == null)
                    continue;
                foreach (string owner in info.Owners)
                    if (owner.Equals(CompatibilityOwner, StringComparison.Ordinal))
                        throw new InvalidOperationException("AnimalHusbandryProgress refused to install because frozen animal-viewer compatibility already owns a reviewed target. Restart after disabling the old Strict consumer.");
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
    }
}
