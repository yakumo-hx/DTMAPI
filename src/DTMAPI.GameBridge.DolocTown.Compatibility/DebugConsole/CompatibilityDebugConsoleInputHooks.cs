using System;
using System.Collections.Generic;
using System.Reflection;
using DTMAPI.Core.Runtime;

namespace DTMAPI.DebugConsole
{
    internal static class CompatibilityDebugConsoleInputHooks
    {
        internal static bool ModalOpen { get; set; }
        internal static bool NativeInputDrainActive { get; set; }
        internal static bool ShouldSuppress =>
            ModalOpen || NativeInputDrainActive;

        public static bool UseToolPrefix() => !ShouldSuppress;
        public static bool UseItemPrefix() => !ShouldSuppress;

        public static bool EnterUiCheckPrefix(ref bool __result)
        {
            if (!ShouldSuppress)
                return true;
            __result = true;
            return false;
        }

        internal static void Reset()
        {
            ModalOpen = false;
            NativeInputDrainActive = false;
        }
    }

    internal sealed class CompatibilityDebugConsolePatchSpec
    {
        internal CompatibilityDebugConsolePatchSpec(
            string targetTypeName,
            string methodName,
            int parameterCount,
            MethodInfo callback,
            bool postfix = false)
        {
            TargetTypeName = targetTypeName;
            MethodName = methodName;
            ParameterCount = parameterCount;
            Callback = callback;
            Postfix = postfix;
        }

        internal string TargetTypeName { get; }
        internal string MethodName { get; }
        internal int ParameterCount { get; }
        internal MethodInfo Callback { get; }
        internal bool Postfix { get; }
        internal string Key =>
            TargetTypeName + "." + MethodName + "/" +
            ParameterCount + (Postfix ? ":postfix" : ":prefix");
    }

    internal interface ICompatibilityDebugConsolePatchBackend
    {
        bool TryPatch(CompatibilityDebugConsolePatchSpec spec);
        bool TryUnpatch(CompatibilityDebugConsolePatchSpec spec);
        int CountOwnerPatches(
            CompatibilityDebugConsolePatchSpec spec,
            string ownerId);
    }

    internal sealed class
        ReflectionCompatibilityDebugConsolePatchBackend :
        ICompatibilityDebugConsolePatchBackend
    {
        private readonly DTMAPI.GameBridge.DolocTown
            .HarmonyReflectionPatcher patcher;

        internal ReflectionCompatibilityDebugConsolePatchBackend(
            DtmApiRuntime runtime,
            string ownerId)
        {
            patcher =
                new DTMAPI.GameBridge.DolocTown
                    .HarmonyReflectionPatcher(
                        runtime,
                        ownerId);
        }

        public bool TryPatch(
            CompatibilityDebugConsolePatchSpec spec) =>
            spec.Postfix
                ? patcher.TryPatchPostfix(
                    spec.TargetTypeName,
                    spec.MethodName,
                    spec.Callback,
                    spec.ParameterCount)
                : patcher.TryPatchPrefix(
                    spec.TargetTypeName,
                    spec.MethodName,
                    spec.Callback,
                    spec.ParameterCount);

        public bool TryUnpatch(
            CompatibilityDebugConsolePatchSpec spec) =>
            patcher.TryUnpatchOwnedPatch(
                spec.TargetTypeName,
                spec.MethodName,
                spec.ParameterCount);

        public int CountOwnerPatches(
            CompatibilityDebugConsolePatchSpec spec,
            string ownerId) =>
            patcher.CountOwnerPatches(
                spec.TargetTypeName,
                spec.MethodName,
                spec.ParameterCount,
                ownerId);
    }

    internal sealed class CompatibilityDebugConsoleHookOwner
    {
        internal const string HarmonyOwner =
            "dtmapi.compatibility.debugconsole.legacy";
        internal const string ProductHarmonyOwner =
            "dtmapi.mod.dtmapi.debugconsolemod";
        private readonly DtmApiRuntime runtime;
        private readonly bool installNativeHooks;
        private readonly ICompatibilityDebugConsolePatchBackend? backend;
        private readonly IReadOnlyList<CompatibilityDebugConsolePatchSpec>
            inputSpecs;
        private readonly IReadOnlyList<CompatibilityDebugConsolePatchSpec>
            creativeSpecs;
        private readonly IReadOnlyList<CompatibilityDebugConsolePatchSpec>
            movementSpecs;
        private bool modalOpen;
        private bool nativeInputDrainActive;
        private bool creativeEnabled;
        private object? movementPlayer;
        private double movementMultiplier = 1d;
        private bool inputInstalled;
        private bool creativeInstalled;
        private bool movementInstalled;
        private bool cleanupPending;

        internal CompatibilityDebugConsoleHookOwner(
            DtmApiRuntime runtime,
            bool installNativeHooks)
        {
            this.runtime = runtime ??
                throw new ArgumentNullException(nameof(runtime));
            this.installNativeHooks = installNativeHooks;
            if (installNativeHooks)
            {
                backend =
                    new ReflectionCompatibilityDebugConsolePatchBackend(
                        runtime,
                        HarmonyOwner);
            }
            inputSpecs = BuildInputSpecs();
            creativeSpecs = BuildCreativeSpecs();
            movementSpecs = BuildMovementSpecs();
        }

        internal CompatibilityDebugConsoleHookOwner(
            DtmApiRuntime runtime,
            ICompatibilityDebugConsolePatchBackend backend)
        {
            this.runtime = runtime ??
                throw new ArgumentNullException(nameof(runtime));
            this.backend = backend ??
                throw new ArgumentNullException(nameof(backend));
            installNativeHooks = true;
            inputSpecs = BuildInputSpecs();
            creativeSpecs = BuildCreativeSpecs();
            movementSpecs = BuildMovementSpecs();
        }

        internal int InstalledPatchCount { get; private set; }
        internal int PatchOperationCount { get; private set; }
        internal int TopologyTransitionCount { get; private set; }
        internal int StatusPublicationCount { get; private set; }
        internal bool CleanupPending => cleanupPending;

        internal void SetModalOpen(bool value)
        {
            Reconcile(
                value,
                nativeInputDrainActive,
                creativeEnabled,
                movementPlayer,
                movementMultiplier,
                "modal demand changed");
        }

        internal void SetNativeInputDrainActive(bool value)
        {
            Reconcile(
                modalOpen,
                value,
                creativeEnabled,
                movementPlayer,
                movementMultiplier,
                "input-drain demand changed");
        }

        internal void SetCreativeEnabled(bool value)
        {
            Reconcile(
                modalOpen,
                nativeInputDrainActive,
                value,
                movementPlayer,
                movementMultiplier,
                "creative demand changed");
        }

        internal void SetMovementMultiplier(
            object? player,
            double multiplier)
        {
            Reconcile(
                modalOpen,
                nativeInputDrainActive,
                creativeEnabled,
                player,
                multiplier,
                "movement multiplier changed");
        }

        internal void Shutdown(string reason)
        {
            Reconcile(false, false, false, null, 1d, reason);
        }

        private void Reconcile(
            bool requestedModalOpen,
            bool requestedNativeInputDrainActive,
            bool requestedCreativeEnabled,
            object? requestedMovementPlayer,
            double requestedMovementMultiplier,
            string reason)
        {
            bool requestedMovement =
                Math.Abs(requestedMovementMultiplier - 1d) >= 0.0001d;
            if (requestedMovement && requestedMovementPlayer == null)
            {
                throw new InvalidOperationException(
                    "A non-default DebugConsole movement multiplier requires the current player BodyController.");
            }
            if (!installNativeHooks)
            {
                CommitDemand(
                    requestedModalOpen,
                    requestedNativeInputDrainActive,
                    requestedCreativeEnabled,
                    requestedMovementPlayer,
                    requestedMovementMultiplier);
                return;
            }
            if (requestedModalOpen == modalOpen &&
                requestedNativeInputDrainActive ==
                    nativeInputDrainActive &&
                requestedCreativeEnabled == creativeEnabled &&
                ReferenceEquals(
                    requestedMovementPlayer,
                    movementPlayer) &&
                Math.Abs(
                    requestedMovementMultiplier -
                    movementMultiplier) < 0.0001d &&
                !cleanupPending)
            {
                return;
            }

            bool requestedInput =
                requestedModalOpen ||
                requestedNativeInputDrainActive;
            bool topologyChanged =
                requestedInput != inputInstalled ||
                requestedCreativeEnabled != creativeInstalled ||
                requestedMovement != movementInstalled;
            if (!topologyChanged && !cleanupPending)
            {
                CommitDemand(
                    requestedModalOpen,
                    requestedNativeInputDrainActive,
                    requestedCreativeEnabled,
                    requestedMovementPlayer,
                    requestedMovementMultiplier);
                return;
            }

            bool previousModalOpen = modalOpen;
            bool previousNativeInputDrainActive =
                nativeInputDrainActive;
            bool previousCreativeEnabled = creativeEnabled;
            object? previousMovementPlayer = movementPlayer;
            double previousMovementMultiplier = movementMultiplier;
            bool previousInputInstalled = inputInstalled;
            bool previousCreativeInstalled =
                creativeInstalled;
            bool previousMovementInstalled =
                movementInstalled;
            try
            {
                if (cleanupPending)
                    RecoverPendingCleanup();

                topologyChanged =
                    requestedInput != inputInstalled ||
                    requestedCreativeEnabled !=
                        creativeInstalled ||
                    requestedMovement != movementInstalled;
                if (!topologyChanged)
                {
                    cleanupPending = false;
                    CommitDemand(
                        requestedModalOpen,
                        requestedNativeInputDrainActive,
                        requestedCreativeEnabled,
                        requestedMovementPlayer,
                        requestedMovementMultiplier);
                    PublishTopologyTransition(
                        requestedInput,
                        requestedCreativeEnabled,
                        requestedMovement,
                        reason + " cleanup retry");
                    return;
                }

                if (requestedInput && !inputInstalled)
                {
                    InstallGroup(inputSpecs, "input");
                    inputInstalled = true;
                }
                if (requestedCreativeEnabled &&
                    !creativeInstalled)
                {
                    InstallGroup(creativeSpecs, "creative");
                    creativeInstalled = true;
                }
                if (requestedMovement &&
                    !movementInstalled)
                {
                    InstallGroup(movementSpecs, "movement");
                    movementInstalled = true;
                }
                if (!requestedMovement &&
                    movementInstalled)
                {
                    RemoveGroup(movementSpecs, "movement");
                    movementInstalled = false;
                }
                if (!requestedCreativeEnabled &&
                    creativeInstalled)
                {
                    RemoveGroup(creativeSpecs, "creative");
                    creativeInstalled = false;
                }
                if (!requestedInput && inputInstalled)
                {
                    RemoveGroup(inputSpecs, "input");
                    inputInstalled = false;
                }

                CommitDemand(
                    requestedModalOpen,
                    requestedNativeInputDrainActive,
                    requestedCreativeEnabled,
                    requestedMovementPlayer,
                    requestedMovementMultiplier);
                RefreshInstalledPatchCount();
                int expected =
                    (inputInstalled ? inputSpecs.Count : 0) +
                    (creativeInstalled
                        ? creativeSpecs.Count
                        : 0) +
                    (movementInstalled
                        ? movementSpecs.Count
                        : 0);
                if (InstalledPatchCount != expected)
                {
                    throw new InvalidOperationException(
                        "Frozen DebugConsole compatibility topology proof failed: expected " +
                        expected + " exact-owner patches but observed " +
                        InstalledPatchCount + ".");
                }
                cleanupPending = false;
                PublishTopologyTransition(
                    requestedInput,
                    requestedCreativeEnabled,
                    requestedMovement,
                    reason);
            }
            catch (Exception transitionFailure)
            {
                var failures =
                    new List<Exception> { transitionFailure };
                TryRollbackGroup(
                    inputSpecs,
                    previousInputInstalled,
                    ref inputInstalled,
                    "input",
                    failures);
                TryRollbackGroup(
                    creativeSpecs,
                    previousCreativeInstalled,
                    ref creativeInstalled,
                    "creative",
                    failures);
                TryRollbackGroup(
                    movementSpecs,
                    previousMovementInstalled,
                    ref movementInstalled,
                    "movement",
                    failures);
                try
                {
                    RefreshInstalledPatchCount();
                    cleanupPending =
                        !TopologyMatches(
                            previousInputInstalled,
                            previousCreativeInstalled,
                            previousMovementInstalled);
                }
                catch (Exception rollbackObservationFailure)
                {
                    cleanupPending = true;
                    failures.Add(rollbackObservationFailure);
                }
                CommitDemand(
                    previousModalOpen,
                    previousNativeInputDrainActive,
                    previousCreativeEnabled,
                    previousMovementPlayer,
                    previousMovementMultiplier);
                if (failures.Count == 1)
                    throw;
                throw new AggregateException(
                    "Frozen DebugConsole compatibility Hook transition failed and exact prior topology restoration was incomplete.",
                    failures);
            }
        }

        private void RecoverPendingCleanup()
        {
            var failures = new List<Exception>();
            CleanupPartialGroup(
                inputSpecs,
                "input cleanup-pending retry",
                failures);
            CleanupPartialGroup(
                creativeSpecs,
                "creative cleanup-pending retry",
                failures);
            CleanupPartialGroup(
                movementSpecs,
                "movement cleanup-pending retry",
                failures);
            if (failures.Count > 0)
            {
                throw new AggregateException(
                    "Frozen DebugConsole compatibility cleanup-pending topology could not be reduced to zero.",
                    failures);
            }

            inputInstalled = false;
            creativeInstalled = false;
            movementInstalled = false;
            DebugConsoleMovementHooks.Reset();
            RefreshInstalledPatchCount();
            if (InstalledPatchCount != 0)
            {
                throw new InvalidOperationException(
                    "Frozen DebugConsole compatibility cleanup-pending retry retained " +
                    InstalledPatchCount + " exact-owner patch(es).");
            }
            cleanupPending = false;
        }

        private bool TopologyMatches(
            bool expectedInputInstalled,
            bool expectedCreativeInstalled,
            bool expectedMovementInstalled)
        {
            foreach (CompatibilityDebugConsolePatchSpec spec in
                     inputSpecs)
            {
                int count =
                    backend!.CountOwnerPatches(
                        spec,
                        HarmonyOwner);
                if (count !=
                    (expectedInputInstalled ? 1 : 0))
                {
                    return false;
                }
            }
            foreach (CompatibilityDebugConsolePatchSpec spec in
                     creativeSpecs)
            {
                int count =
                    backend!.CountOwnerPatches(
                        spec,
                        HarmonyOwner);
                if (count !=
                    (expectedCreativeInstalled ? 1 : 0))
                {
                    return false;
                }
            }
            foreach (CompatibilityDebugConsolePatchSpec spec in
                     movementSpecs)
            {
                int count =
                    backend!.CountOwnerPatches(
                        spec,
                        HarmonyOwner);
                if (count !=
                    (expectedMovementInstalled ? 1 : 0))
                {
                    return false;
                }
            }
            return true;
        }

        private void PublishTopologyTransition(
            bool requestedInput,
            bool requestedCreativeEnabled,
            bool requestedMovement,
            string reason)
        {
            TopologyTransitionCount++;
            StatusPublicationCount++;
            runtime.SetHookStatus(
                "Compatibility.DebugConsoleHooks",
                InstalledPatchCount == 0
                    ? "resident-dormant"
                    : "active",
                "optional Compatibility exact Harmony owner",
                "Legacy DebugConsole patches=" +
                InstalledPatchCount +
                " input=" +
                requestedInput +
                " creative=" +
                requestedCreativeEnabled +
                " movement=" +
                requestedMovement +
                " cleanupPending=" +
                cleanupPending +
                " reason=" + reason + ".");
        }

        private void InstallGroup(
            IReadOnlyList<CompatibilityDebugConsolePatchSpec> specs,
            string group)
        {
            foreach (CompatibilityDebugConsolePatchSpec spec in
                     specs)
            {
                int productCount =
                    backend!.CountOwnerPatches(
                        spec,
                        ProductHarmonyOwner);
                if (productCount < 0)
                {
                    throw new InvalidOperationException(
                        "Frozen DebugConsole compatibility owner could not inspect ProductNative owner state for " +
                        spec.Key + ".");
                }
                if (productCount > 0)
                {
                    throw new InvalidOperationException(
                        "Frozen DebugConsole compatibility owner refused " +
                        group + " installation because ProductNative owner " +
                        ProductHarmonyOwner + " is already active on " +
                        spec.Key + ".");
                }
                int ownCount =
                    backend.CountOwnerPatches(spec, HarmonyOwner);
                if (ownCount != 0)
                {
                    throw new InvalidOperationException(
                        "Frozen DebugConsole compatibility " +
                        group + " target already has " +
                        ownCount + " exact-owner patch(es): " +
                        spec.Key + ".");
                }
            }
            var failures = new List<Exception>();
            foreach (CompatibilityDebugConsolePatchSpec spec in
                     specs)
            {
                PatchOperationCount++;
                if (!backend!.TryPatch(spec))
                {
                    failures.Add(
                        new InvalidOperationException(
                            "Frozen DebugConsole compatibility " +
                            group + " patch failed: " +
                            spec.Key + "."));
                    break;
                }
            }
            if (failures.Count == 0)
            {
                foreach (CompatibilityDebugConsolePatchSpec spec in
                         specs)
                {
                    if (backend!.CountOwnerPatches(
                            spec,
                            HarmonyOwner) != 1)
                    {
                        failures.Add(
                            new InvalidOperationException(
                                "Frozen DebugConsole compatibility " +
                                group +
                                " install could not prove exactly one owner patch on " +
                                spec.Key + "."));
                    }
                }
            }
            if (failures.Count == 0)
                return;
            CleanupPartialGroup(specs, group, failures);
            throw new AggregateException(
                "Frozen DebugConsole compatibility " +
                group + " install was rolled back.",
                failures);
        }

        private void RemoveGroup(
            IReadOnlyList<CompatibilityDebugConsolePatchSpec> specs,
            string group)
        {
            var failures = new List<Exception>();
            foreach (CompatibilityDebugConsolePatchSpec spec in
                     specs)
            {
                PatchOperationCount++;
                if (!backend!.TryUnpatch(spec))
                {
                    failures.Add(
                        new InvalidOperationException(
                            "Frozen DebugConsole compatibility " +
                            group + " unpatch failed: " +
                            spec.Key + "."));
                }
            }
            foreach (CompatibilityDebugConsolePatchSpec spec in
                     specs)
            {
                int remaining =
                    backend!.CountOwnerPatches(
                        spec,
                        HarmonyOwner);
                if (remaining != 0)
                {
                    failures.Add(
                        new InvalidOperationException(
                            "Frozen DebugConsole compatibility " +
                            group + " unpatch retained " +
                            remaining + " owner patch(es): " +
                            spec.Key + "."));
                }
            }
            if (failures.Count > 0)
            {
                TryRestoreGroupAfterFailedRemoval(
                    specs,
                    group,
                    failures);
                throw new AggregateException(
                    "Frozen DebugConsole compatibility " +
                    group +
                    " removal failed; the exact prior topology was restored when possible.",
                    failures);
            }
        }

        private void TryRestoreGroupAfterFailedRemoval(
            IReadOnlyList<CompatibilityDebugConsolePatchSpec> specs,
            string group,
            ICollection<Exception> failures)
        {
            var rollbackFailures = new List<Exception>();
            CleanupPartialGroup(
                specs,
                group + " failed-removal cleanup",
                rollbackFailures);
            if (rollbackFailures.Count == 0)
            {
                try
                {
                    InstallGroup(
                        specs,
                        group + " failed-removal rollback");
                    return;
                }
                catch (Exception rollbackFailure)
                {
                    rollbackFailures.Add(rollbackFailure);
                }
            }
            foreach (Exception rollbackFailure in
                     rollbackFailures)
            {
                failures.Add(rollbackFailure);
            }
        }

        private void CleanupPartialGroup(
            IReadOnlyList<CompatibilityDebugConsolePatchSpec> specs,
            string group,
            ICollection<Exception> failures)
        {
            foreach (CompatibilityDebugConsolePatchSpec spec in
                     specs)
            {
                int count =
                    backend!.CountOwnerPatches(
                        spec,
                        HarmonyOwner);
                if (count <= 0)
                    continue;
                PatchOperationCount++;
                if (!backend.TryUnpatch(spec))
                {
                    failures.Add(
                        new InvalidOperationException(
                            "Frozen DebugConsole compatibility " +
                            group +
                            " partial-install rollback failed: " +
                            spec.Key + "."));
                }
            }
            foreach (CompatibilityDebugConsolePatchSpec spec in
                     specs)
            {
                int count =
                    backend!.CountOwnerPatches(
                        spec,
                        HarmonyOwner);
                if (count != 0)
                {
                    failures.Add(
                        new InvalidOperationException(
                            "Frozen DebugConsole compatibility " +
                            group +
                            " partial-install rollback retained " +
                            count + " patch(es): " +
                            spec.Key + "."));
                }
            }
        }

        private void TryRollbackGroup(
            IReadOnlyList<CompatibilityDebugConsolePatchSpec> specs,
            bool previousInstalled,
            ref bool currentInstalled,
            string group,
            ICollection<Exception> failures)
        {
            if (currentInstalled == previousInstalled)
                return;
            try
            {
                if (previousInstalled)
                    InstallGroup(specs, group + " rollback");
                else
                    RemoveGroup(specs, group + " rollback");
                currentInstalled = previousInstalled;
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        }

        private void CommitDemand(
            bool requestedModalOpen,
            bool requestedNativeInputDrainActive,
            bool requestedCreativeEnabled,
            object? requestedMovementPlayer,
            double requestedMovementMultiplier)
        {
            modalOpen = requestedModalOpen;
            nativeInputDrainActive =
                requestedNativeInputDrainActive;
            creativeEnabled = requestedCreativeEnabled;
            movementPlayer = requestedMovementPlayer;
            movementMultiplier = requestedMovementMultiplier;
            CompatibilityDebugConsoleInputHooks.ModalOpen =
                modalOpen;
            CompatibilityDebugConsoleInputHooks
                .NativeInputDrainActive =
                nativeInputDrainActive;
            DebugConsoleCreativeHooks.Installed =
                creativeInstalled && !cleanupPending;
            DebugConsoleMovementHooks.SetMultiplier(
                requestedMovementPlayer,
                requestedMovementMultiplier);
        }

        private void RefreshInstalledPatchCount()
        {
            int count = 0;
            foreach (CompatibilityDebugConsolePatchSpec spec in
                     inputSpecs)
            {
                int observed =
                    backend!.CountOwnerPatches(
                        spec,
                        HarmonyOwner);
                if (observed < 0)
                    throw new InvalidOperationException(
                        "Could not observe compatibility input patch " +
                        spec.Key + ".");
                count += observed;
            }
            foreach (CompatibilityDebugConsolePatchSpec spec in
                     creativeSpecs)
            {
                int observed =
                    backend!.CountOwnerPatches(
                        spec,
                        HarmonyOwner);
                if (observed < 0)
                    throw new InvalidOperationException(
                        "Could not observe compatibility creative patch " +
                        spec.Key + ".");
                count += observed;
            }
            foreach (CompatibilityDebugConsolePatchSpec spec in
                     movementSpecs)
            {
                int observed =
                    backend!.CountOwnerPatches(
                        spec,
                        HarmonyOwner);
                if (observed < 0)
                {
                    throw new InvalidOperationException(
                        "Could not observe compatibility movement patch " +
                        spec.Key + ".");
                }
                count += observed;
            }
            InstalledPatchCount = count;
        }

        private static MethodInfo Callback(string name) =>
            typeof(CompatibilityDebugConsoleInputHooks).GetMethod(
                name,
                BindingFlags.Public | BindingFlags.Static) ??
            throw new MissingMethodException(
                typeof(CompatibilityDebugConsoleInputHooks).FullName,
                name);

        private static MethodInfo CreativeCallback(string name) =>
            typeof(DebugConsoleCreativeHooks).GetMethod(
                name,
                BindingFlags.Public | BindingFlags.Static) ??
            throw new MissingMethodException(
                typeof(DebugConsoleCreativeHooks).FullName,
                name);

        private static MethodInfo MovementCallback(string name) =>
            typeof(DebugConsoleMovementHooks).GetMethod(
                name,
                BindingFlags.Public | BindingFlags.Static) ??
            throw new MissingMethodException(
                typeof(DebugConsoleMovementHooks).FullName,
                name);

        private static IReadOnlyList<
            CompatibilityDebugConsolePatchSpec>
            BuildInputSpecs() =>
            new[]
            {
                new CompatibilityDebugConsolePatchSpec(
                    "DolocTown.AgentControllerState, Assembly-CSharp",
                    "UseTool",
                    1,
                    Callback(nameof(
                        CompatibilityDebugConsoleInputHooks
                            .UseToolPrefix))),
                new CompatibilityDebugConsolePatchSpec(
                    "DolocTown.AgentControllerState, Assembly-CSharp",
                    "UseItem",
                    1,
                    Callback(nameof(
                        CompatibilityDebugConsoleInputHooks
                            .UseItemPrefix))),
                new CompatibilityDebugConsolePatchSpec(
                    "DolocTown.AgentControllerState, Assembly-CSharp",
                    "EnterUICheck",
                    2,
                    Callback(nameof(
                        CompatibilityDebugConsoleInputHooks
                            .EnterUiCheckPrefix)))
            };

        private static IReadOnlyList<
            CompatibilityDebugConsolePatchSpec>
            BuildCreativeSpecs()
        {
            MethodInfo boolTrue = CreativeCallback(
                nameof(DebugConsoleCreativeHooks.BoolTruePrefix));
            MethodInfo voidSkip = CreativeCallback(
                nameof(DebugConsoleCreativeHooks.VoidSkipPrefix));
            MethodInfo recipeTime = CreativeCallback(
                nameof(DebugConsoleCreativeHooks.RecipeTimePostfix));
            return new[]
            {
                new CompatibilityDebugConsolePatchSpec("DolocAPI, Assembly-CSharp", "CostEnergy", 1, boolTrue),
                new CompatibilityDebugConsolePatchSpec("DolocAPI, Assembly-CSharp", "CostToolEnergy", 0, boolTrue),
                new CompatibilityDebugConsolePatchSpec("DolocAPI, Assembly-CSharp", "HasEnoughEnergy", 1, boolTrue),
                new CompatibilityDebugConsolePatchSpec("DolocAPI, Assembly-CSharp", "HasEnoughEnergyForUsingTool", 0, boolTrue),
                new CompatibilityDebugConsolePatchSpec("DolocAPI, Assembly-CSharp", "CostItem", 3, boolTrue),
                new CompatibilityDebugConsolePatchSpec("DolocAPI, Assembly-CSharp", "CostItem", 4, boolTrue),
                new CompatibilityDebugConsolePatchSpec("DolocAPI, Assembly-CSharp", "CostItemNoCheck", 2, voidSkip),
                new CompatibilityDebugConsolePatchSpec("DolocAPI, Assembly-CSharp", "CostItemNoCheck", 3, voidSkip),
                new CompatibilityDebugConsolePatchSpec("DolocAPI, Assembly-CSharp", "CostSelectedItem", 2, boolTrue),
                new CompatibilityDebugConsolePatchSpec("DolocAPI, Assembly-CSharp", "CostSelectedItem", 3, boolTrue),
                new CompatibilityDebugConsolePatchSpec("DolocAPI, Assembly-CSharp", "CostItemAt", 4, boolTrue),
                new CompatibilityDebugConsolePatchSpec("DolocAPI, Assembly-CSharp", "CanAfford", 2, boolTrue),
                new CompatibilityDebugConsolePatchSpec("DolocAPI, Assembly-CSharp", "CanAfford", 3, boolTrue),
                new CompatibilityDebugConsolePatchSpec("DolocAPI, Assembly-CSharp", "CanAffordMoney", 1, boolTrue),
                new CompatibilityDebugConsolePatchSpec(
                    "DolocTown.Synthesizer, Assembly-CSharp",
                    "GetRecipeTime",
                    2,
                    recipeTime,
                    postfix: true)
            };
        }

        private static IReadOnlyList<
            CompatibilityDebugConsolePatchSpec>
            BuildMovementSpecs() =>
            new[]
            {
                new CompatibilityDebugConsolePatchSpec(
                    "DolocTown.BodyController, Assembly-CSharp",
                    "get_MoveSpeed",
                    0,
                    MovementCallback(nameof(
                        DebugConsoleMovementHooks.MoveSpeedPostfix)),
                    postfix: true)
            };
    }
}
