using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace DTMAPI.DebugConsole
{
    internal sealed class DebugConsoleHookInstaller
    {
        internal const string HarmonyOwner =
            "dtmapi.mod.dtmapi.debugconsolemod";
        internal const string CompatibilityHarmonyOwner =
            "dtmapi.compatibility.debugconsole.legacy";
        private const int RecipeTargetIndex = 17;
        private const int MovementTargetIndex = 18;
        private readonly List<MethodBase> targets =
            new List<MethodBase>(19);
        private int expectedPatchCount;

        internal int InstalledPatchCount { get; private set; }

        internal void InstallAtomically()
        {
            if (InstalledPatchCount > 0 &&
                InstalledPatchCount == expectedPatchCount)
                return;
            ResolveTargets();
            ThrowIfOwnerPresent();
            var harmony = new Harmony(HarmonyOwner);
            try
            {
                harmony.Patch(
                    targets[0],
                    prefix: Callback(nameof(
                        DebugConsoleInputHooks.UseToolPrefix)));
                harmony.Patch(
                    targets[1],
                    prefix: Callback(nameof(
                        DebugConsoleInputHooks.UseItemPrefix)));
                harmony.Patch(
                    targets[2],
                    prefix: Callback(nameof(
                        DebugConsoleInputHooks.EnterUiCheckPrefix)));
                for (int index = 3; index < RecipeTargetIndex; index++)
                {
                    bool voidTarget =
                        targets[index].Name.Equals(
                            "CostItemNoCheck",
                            StringComparison.Ordinal);
                    harmony.Patch(
                        targets[index],
                        prefix: CreativeCallback(
                            voidTarget
                                ? nameof(DebugConsoleCreativeHooks.VoidSkipPrefix)
                                : nameof(DebugConsoleCreativeHooks.BoolTruePrefix)));
                }
                harmony.Patch(
                    targets[RecipeTargetIndex],
                    postfix: CreativeCallback(nameof(
                        DebugConsoleCreativeHooks.RecipeTimePostfix)));
                harmony.Patch(
                    targets[MovementTargetIndex],
                    postfix: MovementCallback(nameof(
                        DebugConsoleMovementHooks.MoveSpeedPostfix)));
                expectedPatchCount = targets.Count;
                RefreshCount();
                if (InstalledPatchCount != expectedPatchCount)
                    throw new InvalidOperationException(
                        "DebugConsole could not prove its three input, bounded creative and final player-speed ProductNative patches.");
                DebugConsoleCreativeHooks.Installed = true;
            }
            catch
            {
                Unpatch();
                throw;
            }
        }

        internal void Unpatch()
        {
            var harmony = new Harmony(HarmonyOwner);
            foreach (MethodBase target in targets)
            {
                harmony.Unpatch(
                    target,
                    HarmonyPatchType.All,
                    HarmonyOwner);
            }
            RefreshCount();
            if (InstalledPatchCount != 0)
            {
                throw new InvalidOperationException(
                    "DebugConsole exact-owner unpatch left native input-isolation residue.");
            }
            DebugConsoleInputGate.Reset();
            DebugConsoleCreativeHooks.Reset();
            DebugConsoleMovementHooks.Reset();
        }

        private void ResolveTargets()
        {
            targets.Clear();
            Type type =
                Type.GetType(
                    "DolocTown.AgentControllerState, Assembly-CSharp",
                    throwOnError: true)!;
            targets.Add(Resolve(type, "UseTool", 1));
            targets.Add(Resolve(type, "UseItem", 1));
            targets.Add(Resolve(type, "EnterUICheck", 2));
            Type api = Type.GetType(
                "DolocAPI, Assembly-CSharp",
                throwOnError: true)!;
            Add(api, "CostEnergy", 1);
            Add(api, "CostToolEnergy", 0);
            Add(api, "HasEnoughEnergy", 1);
            Add(api, "HasEnoughEnergyForUsingTool", 0);
            Add(api, "CostItem", 3);
            Add(api, "CostItem", 4);
            Add(api, "CostItemNoCheck", 2);
            Add(api, "CostItemNoCheck", 3);
            Add(api, "CostSelectedItem", 2);
            Add(api, "CostSelectedItem", 3);
            Add(api, "CostItemAt", 2);
            Add(api, "CanAfford", 2);
            Add(api, "CanAfford", 3);
            Add(api, "CanAffordMoney", 1);
            Type synthesizer = Type.GetType(
                "DolocTown.Synthesizer, Assembly-CSharp",
                throwOnError: true)!;
            Add(synthesizer, "GetRecipeTime", 2);
            Type bodyController = Type.GetType(
                "DolocTown.BodyController, Assembly-CSharp",
                throwOnError: true)!;
            Add(bodyController, "get_MoveSpeed", 0);
        }

        private void Add(
            Type type,
            string name,
            int parameterCount) =>
            targets.Add(Resolve(type, name, parameterCount));

        private static MethodInfo Resolve(
            Type type,
            string name,
            int parameterCount)
        {
            return type.GetMethods(
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance |
                    BindingFlags.Static)
                .Single(method =>
                    method.Name == name &&
                    method.GetParameters().Length ==
                    parameterCount);
        }

        private static HarmonyMethod Callback(string name)
        {
            return new HarmonyMethod(
                typeof(DebugConsoleInputHooks).GetMethod(
                    name,
                    BindingFlags.Public |
                    BindingFlags.Static) ??
                throw new MissingMethodException(
                    typeof(DebugConsoleInputHooks).FullName,
                    name));
        }

        private static HarmonyMethod CreativeCallback(string name)
        {
            return new HarmonyMethod(
                typeof(DebugConsoleCreativeHooks).GetMethod(
                    name,
                    BindingFlags.Public |
                    BindingFlags.Static) ??
                throw new MissingMethodException(
                    typeof(DebugConsoleCreativeHooks).FullName,
                    name));
        }

        private static HarmonyMethod MovementCallback(string name)
        {
            return new HarmonyMethod(
                typeof(DebugConsoleMovementHooks).GetMethod(
                    name,
                    BindingFlags.Public |
                    BindingFlags.Static) ??
                throw new MissingMethodException(
                    typeof(DebugConsoleMovementHooks).FullName,
                    name));
        }

        private void ThrowIfOwnerPresent()
        {
            foreach (MethodBase target in targets)
            {
                Patches? patches = Harmony.GetPatchInfo(target);
                if (patches == null)
                    continue;
                if (CountOwner(
                        patches,
                        CompatibilityHarmonyOwner) > 0)
                {
                    throw new InvalidOperationException(
                        "DebugConsole ProductNative owner refused installation because Compatibility owner " +
                        CompatibilityHarmonyOwner +
                        " is already present on " +
                        target.Name + ".");
                }
                if (CountOwner(patches, HarmonyOwner) > 0)
                {
                    throw new InvalidOperationException(
                        "DebugConsole Harmony owner is already present on " +
                        target.Name + ".");
                }
            }
        }

        private void RefreshCount()
        {
            InstalledPatchCount = targets.Sum(target =>
            {
                Patches? patches = Harmony.GetPatchInfo(target);
                return patches == null
                    ? 0
                    : CountOwner(patches, HarmonyOwner);
            });
        }

        private static int CountOwner(
            Patches patches,
            string owner)
        {
            return patches.Prefixes.Count(p =>
                    p.owner == owner) +
                patches.Postfixes.Count(p =>
                    p.owner == owner) +
                patches.Transpilers.Count(p =>
                    p.owner == owner) +
                patches.Finalizers.Count(p =>
                    p.owner == owner);
        }
    }
}
