using System;
using System.Reflection;
using global::DTMAPI.Abstractions;
using HarmonyLib;

namespace Yuuka.DTMAPI.FishBreedingAssistant
{
    internal sealed class FishBreedingHookInstaller
    {
        internal const string HarmonyOwner = "dtmapi.mod.yuuka.dtmapi.fishbreedingassistant";
        private const string GameBridgeOwner = "dtmapi.gamebridge.doloctown";
        private const int ExpectedPatchCount = 1;
        private readonly IMonitor monitor;
        private Harmony? harmony;

        internal FishBreedingHookInstaller(IMonitor monitor) => this.monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        internal bool IsInstalled { get; private set; }
        internal int InstalledPatchCount { get; private set; }

        internal void InstallAtomically()
        {
            if (IsInstalled)
                return;
            Assembly gameAssembly = typeof(DolocAPI).Assembly;
            Type itemType = gameAssembly.GetType("DolocTown.Item", throwOnError: false)
                ?? throw new TypeLoadException("FishBreedingAssistant could not resolve DolocTown.Item.");
            MethodInfo target = itemType.GetProperty("title", BindingFlags.Public | BindingFlags.Instance)?.GetMethod
                ?? throw new MissingMethodException("DolocTown.Item", "get_title");
            MethodInfo callback = typeof(FishBreedingCallbacks).GetMethod(nameof(FishBreedingCallbacks.ItemTitlePostfix), BindingFlags.Public | BindingFlags.Static)
                ?? throw new MissingMethodException(typeof(FishBreedingCallbacks).FullName, nameof(FishBreedingCallbacks.ItemTitlePostfix));
            ThrowIfCompatibilityTitleOwnerIsPresent(target);

            harmony = new Harmony(HarmonyOwner);
            try
            {
                harmony.Patch(target, postfix: new HarmonyMethod(callback));
                InstalledPatchCount = ExpectedPatchCount;
                IsInstalled = true;
                monitor.Log("FishBreedingAssistant product-owned patch installed owner=" + HarmonyOwner + " count=" + InstalledPatchCount + ".");
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

        private static void ThrowIfCompatibilityTitleOwnerIsPresent(MethodBase target)
        {
            Patches? info = Harmony.GetPatchInfo(target);
            if (info == null)
                return;
            foreach (string owner in info.Owners)
            {
                if (owner.Equals(GameBridgeOwner, StringComparison.Ordinal))
                    throw new InvalidOperationException("FishBreedingAssistant refused to install because frozen item-tooltip compatibility already owns Item.get_title. Restart after disabling the old Strict consumer.");
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
