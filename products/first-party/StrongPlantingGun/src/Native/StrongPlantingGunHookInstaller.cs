using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using global::DTMAPI.Abstractions;
using HarmonyLib;

namespace DTMAPI.StrongPlantingGun
{
    internal sealed class StrongPlantingGunHookInstaller
    {
        private readonly IMonitor monitor;
        private readonly Func<int, bool>? installGate;
        private readonly Action? unpatchOverride;
        private readonly List<MethodBase> targets =
            new List<MethodBase>(
                StrongPlantingGunProductContract.ExpectedHookCount);
        private Harmony? harmony;

        internal StrongPlantingGunHookInstaller(
            IMonitor monitor,
            Func<int, bool>? installGate = null,
            Action? unpatchOverride = null)
        {
            this.monitor = monitor ??
                throw new ArgumentNullException(nameof(monitor));
            this.installGate = installGate;
            this.unpatchOverride = unpatchOverride;
        }

        internal bool IsInstalled { get; private set; }

        internal int InstalledPatchCount { get; private set; }

        internal void InstallAtomically(
            StrongPlantingGunNativeRuntime runtime)
        {
            if (runtime == null)
                throw new ArgumentNullException(nameof(runtime));
            if (IsInstalled)
                return;

            ResolveAllTargets();
            ThrowIfOwnerCannotInstall();

            MethodInfo ctorCallback = RequireCallback(
                nameof(StrongPlantingGunCallbacks
                    .ItemFarmingGunCtorPostfix));
            MethodInfo toolCallback = RequireCallback(
                nameof(StrongPlantingGunCallbacks
                    .ItemFarmingGunOnUseAsToolPrefix));
            MethodInfo previewCallback = RequireCallback(
                nameof(StrongPlantingGunCallbacks.ItemFarmingGunRefreshCellTipPrefix));
            var containerForwarder = new HarmonyMethod(typeof(StrongPlantingGunHookInstaller),
                nameof(ForwardNativeContainerOperation));

            harmony = new Harmony(
                StrongPlantingGunProductContract.HarmonyOwner);
            StrongPlantingGunCallbacks.Attach(runtime);
            try
            {
                PatchWithGate(
                    1,
                    targets[0],
                    postfix: new HarmonyMethod(ctorCallback));
                PatchWithGate(
                    2,
                    targets[1],
                    postfix: new HarmonyMethod(ctorCallback));
                PatchWithGate(
                    3,
                    targets[2],
                    prefix: new HarmonyMethod(toolCallback));
                PatchWithGate(4, targets[3], prefix: new HarmonyMethod(previewCallback));
                PatchWithGate(5, targets[4], prefix: new HarmonyMethod(RequireCallback(
                    nameof(StrongPlantingGunCallbacks.ItemFarmingGunOpenContainerUiPrefix))));
                for (int i = 5; i < 14; i++)
                    PatchWithGate(i + 1, targets[i], transpiler: containerForwarder);
                PatchWithGate(15, targets[14],
                    prefix: new HarmonyMethod(RequireCallback(nameof(StrongPlantingGunCallbacks.ItemFarmingGunInitCellTipPrefix))),
                    transpiler: new HarmonyMethod(typeof(StrongPlantingGunHookInstaller), nameof(AdaptNativeRange)));

                PublishObservedState();
                if (!IsInstalled ||
                    InstalledPatchCount !=
                    StrongPlantingGunProductContract
                        .ExpectedHookCount)
                {
                    throw new InvalidOperationException(
                        "StrongPlantingGun could not prove all exact-owner Hooks after installation.");
                }

                StrongPlantingGunCallbacks
                    .PublishLifecycleSummary(
                        runtime.BuildLifecycleSummary());
                monitor.Log(
                    "StrongPlantingGun product-owned Hook set installed owner=" +
                    StrongPlantingGunProductContract.HarmonyOwner +
                    " count=" +
                    InstalledPatchCount +
                    ".");
            }
            catch (Exception installFailure)
            {
                Exception? cleanupFailure = null;
                try
                {
                    UnpatchExactOwner();
                }
                catch (Exception ex)
                {
                    cleanupFailure = ex;
                }
                finally
                {
                    PublishObservedState();
                    StrongPlantingGunCallbacks.Detach(
                        runtime,
                        runtime.BuildLifecycleSummary(
                            callbacksOverride: 0));
                }

                Exception? residueFailure =
                    IsInstalled ||
                    InstalledPatchCount != 0
                        ? new InvalidOperationException(
                            "StrongPlantingGun rollback left one or more exact ProductNative owners installed.")
                        : null;
                if (cleanupFailure != null ||
                    residueFailure != null)
                {
                    var failures = new List<Exception>
                    {
                        installFailure
                    };
                    if (cleanupFailure != null)
                        failures.Add(cleanupFailure);
                    if (residueFailure != null)
                        failures.Add(residueFailure);
                    throw new AggregateException(
                        "StrongPlantingGun atomic Hook installation failed and exact-owner rollback also failed.",
                        failures);
                }
                throw;
            }
        }

        internal void UnpatchOwnedHooks(
            StrongPlantingGunNativeRuntime runtime)
        {
            Exception? resolutionFailure = null;
            if (targets.Count !=
                StrongPlantingGunProductContract.ExpectedHookCount)
            {
                try
                {
                    ResolveAllTargets();
                }
                catch (Exception ex)
                {
                    resolutionFailure = ex;
                }
            }
            Exception? unpatchFailure = null;
            try
            {
                UnpatchExactOwner();
            }
            catch (Exception ex)
            {
                unpatchFailure = ex;
            }
            finally
            {
                PublishObservedState();
                StrongPlantingGunCallbacks.Detach(
                    runtime,
                    runtime.BuildLifecycleSummary(
                        callbacksOverride: 0));
            }

            if (IsInstalled || InstalledPatchCount != 0)
            {
                var residue = new InvalidOperationException(
                    "StrongPlantingGun exact-owner unpatch left one or more product Hooks installed.");
                if (unpatchFailure != null)
                {
                    throw new AggregateException(
                        unpatchFailure,
                        residue);
                }
                throw residue;
            }
            if (unpatchFailure != null)
                throw unpatchFailure;
            if (resolutionFailure != null)
                throw resolutionFailure;
            harmony = null;
        }

        private void ResolveAllTargets()
        {
            targets.Clear();
            Type gunType = ResolveGameType(
                "DolocTown.ItemFarmingGun");
            ConstructorInfo? ordinary = null;
            ConstructorInfo? json = null;
            foreach (ConstructorInfo ctor in gunType.GetConstructors(
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance))
            {
                ParameterInfo[] parameters = ctor.GetParameters();
                if (parameters.Length == 2 &&
                    parameters[0].ParameterType.FullName ==
                        "DolocTown.Config.Item.ItemInfo" &&
                    parameters[1].ParameterType == typeof(int))
                {
                    ordinary = ctor;
                }
                else if (parameters.Length == 3 &&
                    parameters[0].ParameterType == typeof(string) &&
                    parameters[1].ParameterType == typeof(int) &&
                    parameters[2].ParameterType.FullName ==
                        "DolocTown.LinearInventory")
                {
                    json = ctor;
                }
            }
            targets.Add(ordinary ??
                throw new MissingMethodException(
                    gunType.FullName,
                    ".ctor(ItemInfo,int)"));
            targets.Add(json ??
                throw new MissingMethodException(
                    gunType.FullName,
                    ".ctor(string,int,LinearInventory)"));
            targets.Add(ResolveInstanceMethod(
                gunType,
                "OnUseAsTool",
                Type.EmptyTypes));

            targets.Add(ResolveInstanceMethod(gunType, "RefreshCellTip", Type.EmptyTypes));
            targets.Add(RequireDeclared(gunType, "OpenContainerUI", Type.EmptyTypes));
            Type uiType = ResolveGameType("DolocTown.FarmingGunUiState");
            foreach (string name in new[] { "HandlePlaceToOtherSide", "HandleSwapOneItem",
                "OnBackpackItemClick", "OnContainerItemClick", "HandleSwapHalfItems" })
                targets.Add(RequireDeclared(uiType, name, new[] { typeof(int) }));
            foreach (string name in new[] { "PutAll", "PutMax", "GetTipInBackpack", "GetTipInContainer" })
                targets.Add(RequireDeclared(uiType, name, Type.EmptyTypes));
            targets.Add(RequireDeclared(gunType, "InitCellTip", Type.EmptyTypes));
        }

        private static IEnumerable<CodeInstruction> AdaptNativeRange(IEnumerable<CodeInstruction> instructions)
        {
            var result = new List<CodeInstruction>();
            int reads = 0;
            MethodInfo getter = RequireDeclared(ResolveGameType("DolocTown.Config.Item.ItemFunctionFarmingGun"), "get_Area", Type.EmptyTypes);
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.Calls(getter))
                {
                    reads++;
                    // Keep branch/exception markers on the replacement call.
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = RequireCallback(nameof(StrongPlantingGunCallbacks.FarmingGunArea));
                    result.Add(instruction);
                    result.Add(new CodeInstruction(OpCodes.Unbox_Any, getter.ReturnType));
                }
                else result.Add(instruction);
            }
            if (reads != 1) throw new InvalidOperationException("Native InitCellTip must read ItemFunctionFarmingGun.Area exactly once.");
            return result;
        }

        private static MethodInfo RequireDeclared(Type type, string name, Type[] parameters) =>
            type.GetMethod(name, BindingFlags.DeclaredOnly | BindingFlags.Instance |
                BindingFlags.Public | BindingFlags.NonPublic, null, parameters, null) ??
            throw new MissingMethodException(type.FullName, name);

        // Emit only a direct call to the existing native base operation. No native
        // method body is copied, and no shared ContainerBaseUiState target is patched.
        private static IEnumerable<CodeInstruction> ForwardNativeContainerOperation(MethodBase __originalMethod)
        {
            Type type = __originalMethod.DeclaringType!;
            if (type.FullName != "DolocTown.FarmingGunUiState" ||
                type.BaseType?.FullName != "DolocTown.ContainerBaseUiState")
                throw new InvalidOperationException("Unexpected farming container owner.");
            ParameterInfo[] parameters = __originalMethod.GetParameters();
            var types = new Type[parameters.Length];
            for (int i = 0; i < types.Length; i++) types[i] = parameters[i].ParameterType;
            MethodInfo native = RequireDeclared(type.BaseType!, __originalMethod.Name, types);
            if (native.IsAbstract || native.ReturnType != ((MethodInfo)__originalMethod).ReturnType)
                throw new InvalidOperationException("Native container operation changed.");
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            for (int i = 0; i < parameters.Length; i++)
                yield return new CodeInstruction(OpCodes.Ldarg, i + 1);
            yield return new CodeInstruction(OpCodes.Call, native);
            yield return new CodeInstruction(OpCodes.Ret);
        }

        private void ThrowIfOwnerCannotInstall()
        {
            bool[] compatibility = ObserveOwners(
                StrongPlantingGunProductContract
                    .CompatibilityOwner);
            bool[] legacyProduct = ObserveOwners(StrongPlantingGunProductContract.LegacyProductOwner);
            for (int index = 0; index < compatibility.Length; index++)
                compatibility[index] |= legacyProduct[index];
            bool[] product = ObserveOwners(
                StrongPlantingGunProductContract.HarmonyOwner);
            switch (StrongPlantingGunHookOwnership.DecideInstall(
                compatibility,
                product))
            {
                case StrongPlantingGunInstallDecision.Install:
                    return;
                case StrongPlantingGunInstallDecision
                    .RejectCompatibilityOwner:
                    throw new InvalidOperationException(
                        "StrongPlantingGun refused to install because a legacy GameBridge/product owner is present on at least one of its exact native targets.");
                case StrongPlantingGunInstallDecision
                    .RejectDuplicateProductOwner:
                    throw new InvalidOperationException(
                        "StrongPlantingGun refused a duplicate exact ProductNative owner.");
                default:
                    throw new InvalidOperationException(
                        "StrongPlantingGun found a partial ProductNative owner set and failed closed.");
            }
        }

        private void PatchWithGate(
            int ordinal,
            MethodBase target,
            HarmonyMethod? prefix = null,
            HarmonyMethod? postfix = null,
            HarmonyMethod? transpiler = null)
        {
            if (installGate != null &&
                !installGate(ordinal))
            {
                throw new InvalidOperationException(
                    "Injected StrongPlantingGun Hook installation failure at ordinal " +
                    ordinal +
                    ".");
            }
            harmony!.Patch(
                target,
                prefix: prefix,
                postfix: postfix,
                transpiler: transpiler);
        }

        private void PublishObservedState()
        {
            if (targets.Count !=
                StrongPlantingGunProductContract.ExpectedHookCount)
            {
                IsInstalled = false;
                InstalledPatchCount = 0;
                return;
            }

            StrongPlantingGunObservedHookState observed =
                StrongPlantingGunHookOwnership.Observe(
                    ObserveOwners(
                        StrongPlantingGunProductContract
                            .HarmonyOwner));
            IsInstalled = observed.IsInstalled;
            InstalledPatchCount =
                observed.InstalledPatchCount;
        }

        private bool[] ObserveOwners(string owner)
        {
            var result = new bool[targets.Count];
            for (int index = 0; index < targets.Count; index++)
            {
                Patches? info =
                    Harmony.GetPatchInfo(targets[index]);
                if (info == null)
                    continue;
                foreach (string installedOwner in info.Owners)
                {
                    if (installedOwner.Equals(
                        owner,
                        StringComparison.Ordinal))
                    {
                        result[index] = true;
                        break;
                    }
                }
            }
            return result;
        }

        private void UnpatchExactOwner()
        {
            if (unpatchOverride != null)
            {
                unpatchOverride();
                return;
            }
            Harmony ownerHarmony = harmony ??
                new Harmony(
                    StrongPlantingGunProductContract.HarmonyOwner);
            foreach (MethodInfo method in typeof(Harmony).GetMethods(
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Static |
                BindingFlags.Instance))
            {
                ParameterInfo[] parameters = method.GetParameters();
                if (method.Name != "UnpatchAll" ||
                    parameters.Length != 1 ||
                    parameters[0].ParameterType != typeof(string))
                {
                    continue;
                }

                method.Invoke(
                    method.IsStatic ? null : ownerHarmony,
                    new object[]
                    {
                        StrongPlantingGunProductContract
                            .HarmonyOwner
                    });
                return;
            }
            throw new MissingMethodException(
                typeof(Harmony).FullName,
                "UnpatchAll(string)");
        }

        private static Type ResolveGameType(string fullName) =>
            typeof(DolocAPI).Assembly.GetType(
                fullName,
                throwOnError: false) ??
            throw new TypeLoadException(
                "StrongPlantingGun could not resolve " +
                fullName +
                ".");

        private static MethodInfo ResolveInstanceMethod(
            Type type,
            string name,
            Type[] parameters)
        {
            for (Type? current = type;
                 current != null;
                 current = current.BaseType)
            {
                MethodInfo? result = current.GetMethod(
                    name,
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance,
                    binder: null,
                    types: parameters,
                    modifiers: null);
                if (result != null)
                    return result;
            }
            throw new MissingMethodException(type.FullName, name);
        }

        private static MethodInfo RequireCallback(string name) =>
            typeof(StrongPlantingGunCallbacks).GetMethod(
                name,
                BindingFlags.Public | BindingFlags.Static) ??
            throw new MissingMethodException(
                typeof(StrongPlantingGunCallbacks).FullName,
                name);
    }
}
