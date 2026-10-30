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
            "DTMAPI.DebugConsoleMod.Native";
        private readonly List<MethodBase> targets =
            new List<MethodBase>(12);
        private readonly List<(MethodBase Target, MethodInfo Callback)> contributions =
            new List<(MethodBase, MethodInfo)>(12);
        private int expectedPatchCount;

        internal int InstalledPatchCount { get; private set; }

        internal void InstallAtomically()
        {
            if (InstalledPatchCount > 0 &&
                InstalledPatchCount == expectedPatchCount)
                return;
            ResolveTargets();
            var harmony = new Harmony(HarmonyOwner);
            void Patch(int index, HarmonyMethod callback, bool postfix = false)
            {
                contributions.Add((targets[index], callback.method));
                if (postfix) harmony.Patch(targets[index], postfix: callback);
                else harmony.Patch(targets[index], prefix: callback);
            }
            try
            {
                Patch(0, Callback(nameof(DebugConsoleInputHooks.UseToolPrefix)));
                Patch(1, Callback(nameof(DebugConsoleInputHooks.UseItemPrefix)));
                Patch(2, Callback(nameof(DebugConsoleInputHooks.EnterUiCheckPrefix)));
                Patch(3, MovementCallback(nameof(DebugConsoleMovementHooks.MoveSpeedPostfix)), postfix: true);
                Patch(4, ModeCallback(nameof(DebugConsoleModeHooks.HealthCostPrefix)));
                Patch(5, ModeCallback(nameof(DebugConsoleModeHooks.EnergyCostPrefix)));
                Patch(6, ModeCallback(nameof(DebugConsoleModeHooks.EnergyCostNoProtectPrefix)));
                Patch(7, ModeCallback(nameof(DebugConsoleModeHooks.SpiritTickPrefix)));
                Patch(8, ModeCallback(nameof(DebugConsoleModeHooks.SpiritChangedPostfix)), postfix: true);
                Patch(9, ModeCallback(nameof(DebugConsoleModeHooks.TimeScaleWritePrefix)));
                Patch(10, ModeCallback(nameof(DebugConsoleModeHooks.LoadGamePrefix)));
                Patch(11, ModeCallback(nameof(DebugConsoleModeHooks.NewGamePrefix)));
                expectedPatchCount = targets.Count;
                RefreshCount();
                if (InstalledPatchCount != expectedPatchCount)
                    throw new InvalidOperationException(
                        "DebugConsole could not prove its input, god mode, time control, session entry and player-speed patches.");
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
            var failures = new List<Exception>();
            DebugConsoleInputGate.Reset();
            DebugConsoleModeHooks.Reset();
            DebugConsoleMovementHooks.Reset();
            DebugConsoleMovementHooks.SessionIsCurrent = null;
            foreach (var contribution in contributions)
            {
                try { harmony.Unpatch(contribution.Target, contribution.Callback); }
                catch (Exception error) { failures.Add(error); }
            }
            RefreshCount();
            if (failures.Count > 0) throw new AggregateException("DebugConsole patch cleanup needs retry.", failures);
            if (InstalledPatchCount != 0)
            {
                throw new InvalidOperationException(
                    "DebugConsole exact-owner unpatch left native input-isolation residue.");
            }
            contributions.Clear();
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
            Type bodyController = Type.GetType(
                "DolocTown.BodyController, Assembly-CSharp",
                throwOnError: true)!;
            Add(bodyController, "get_MoveSpeed", 0);
            Type archive = Type.GetType("DolocTown.GameData.ArchiveOperationGlobal, Assembly-CSharp", true)!;
            Add(archive, "_CostHealth", 2);
            Add(archive, "_CostEnergy", 2);
            Add(archive, "_CostEnergyNoProtect", 2);
            Type data = Type.GetType("DolocTown.GameData.AgentArchiveData, Assembly-CSharp", true)!;
            Add(data, "_UpdateSpirit", 0);
            Add(data, "AddSpirit", 1);
            Add(Type.GetType("DolocTown.TimeScaleManager, Assembly-CSharp", true)!, "SetTimeScale", 2);
            Add(api, "LoadGame", 1);
            Add(api, "NewGame", 1);
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

        private static HarmonyMethod ModeCallback(string name)
        {
            return new HarmonyMethod(
                typeof(DebugConsoleModeHooks).GetMethod(
                    name,
                    BindingFlags.Public |
                    BindingFlags.Static) ??
                throw new MissingMethodException(
                    typeof(DebugConsoleModeHooks).FullName,
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

        private void RefreshCount()
        {
            InstalledPatchCount = contributions.Sum(contribution =>
            {
                Patches? patches = Harmony.GetPatchInfo(contribution.Target);
                return patches == null
                    ? 0
                    : patches.Prefixes.Concat(patches.Postfixes).Concat(patches.Transpilers).Concat(patches.Finalizers)
                        .Count(patch => patch.PatchMethod == contribution.Callback);
            });
        }
    }
}
