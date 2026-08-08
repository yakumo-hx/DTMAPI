using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using static DTMAPI.GameBridge.DolocTown.DolocTownExperimentalBridgeApi;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed partial class EquipmentSlotsCompatibilityService
    {
        private const string ManagedProductHarmonyOwner = "dtmapi.mod.dtmapi.moreequipmentslotsmod";
        private static readonly object CallbackGate = new object();
        private static EquipmentSlotsCompatibilityService? callbackRuntime;

        private bool EnsureCompatibilityHooks(out string failure)
        {
            failure = string.Empty;
            MethodInfo? reloadParams = FindExactTarget(
                "DolocTown.GameData.AgentEquipmentManager, Assembly-CSharp",
                "ReloadParams",
                0);
            MethodInfo? attacked = FindExactTarget(
                "DolocTown.BodyController, Assembly-CSharp",
                "OnAttacked",
                5);
            MethodInfo? accessoriesInit = FindExactTarget(
                "DolocTown.UI.AccessoriesBar, Assembly-CSharp",
                "__Init",
                0);
            MethodInfo? accessoriesStartShow = FindExactTarget(
                "DolocTown.UI.AccessoriesBar, Assembly-CSharp",
                "OnStartShow",
                0);
            MethodInfo?[] targets = { reloadParams, attacked, accessoriesInit, accessoriesStartShow };
            if (targets.Any(target => target == null))
            {
                failure = "Frozen IEquipmentSlotsApi compatibility refused partial Hook ownership because one or more exact native targets are unavailable.";
                SetEquipmentSlotsRuntimeHooksInstalled(false);
                SetEquipmentSlotsUiHooksInstalled(false);
                return false;
            }

            int productOwned = targets.Count(target => HasHarmonyOwner(target!, ManagedProductHarmonyOwner));
            int compatibilityOwned = targets.Count(target => HasHarmonyOwner(target!, CompatibilityHarmonyOwner));
            if (productOwned != 0)
            {
                failure = "Frozen IEquipmentSlotsApi compatibility refused native ownership because the managed MoreEquipmentSlots product owns " +
                    productOwned + "/4 exact targets.";
                SetEquipmentSlotsRuntimeHooksInstalled(false);
                SetEquipmentSlotsUiHooksInstalled(false);
                return false;
            }

            if (compatibilityOwned == targets.Length)
            {
                lock (CallbackGate)
                    callbackRuntime = this;
                SetEquipmentSlotsRuntimeHooksInstalled(true);
                SetEquipmentSlotsUiHooksInstalled(true);
                return true;
            }

            if (compatibilityOwned != 0)
            {
                TryReleaseCompatibilityHooks(
                    "residual partial owner preflight",
                    out string releaseFailure);
                failure = "Frozen IEquipmentSlotsApi compatibility found residual partial owner state and failed closed. " +
                    releaseFailure;
                return false;
            }

            compatibilityPatcher = new HarmonyReflectionPatcher(runtime, CompatibilityHarmonyOwner);
            lock (CallbackGate)
                callbackRuntime = this;

            bool reloadPatched = CanAttemptCompatibilityHook(1) &&
                compatibilityPatcher.TryPatchPostfix(
                "DolocTown.GameData.AgentEquipmentManager, Assembly-CSharp",
                "ReloadParams",
                typeof(EquipmentSlotsCompatibilityService).GetMethod(nameof(AgentEquipmentReloadParamsPostfix), BindingFlags.Public | BindingFlags.Static),
                0);
            bool attackedPatched = reloadPatched &&
                CanAttemptCompatibilityHook(2) &&
                compatibilityPatcher.TryPatchPrefix(
                "DolocTown.BodyController, Assembly-CSharp",
                "OnAttacked",
                typeof(EquipmentSlotsCompatibilityService).GetMethod(nameof(BodyControllerOnAttackedPrefix), BindingFlags.Public | BindingFlags.Static),
                5);
            bool initPatched = attackedPatched &&
                CanAttemptCompatibilityHook(3) &&
                compatibilityPatcher.TryPatchPostfix(
                "DolocTown.UI.AccessoriesBar, Assembly-CSharp",
                "__Init",
                typeof(EquipmentSlotsCompatibilityService).GetMethod(nameof(AccessoriesBarInitPostfix), BindingFlags.Public | BindingFlags.Static),
                0);
            bool startShowPatched = initPatched &&
                CanAttemptCompatibilityHook(4) &&
                compatibilityPatcher.TryPatchPostfix(
                "DolocTown.UI.AccessoriesBar, Assembly-CSharp",
                "OnStartShow",
                typeof(EquipmentSlotsCompatibilityService).GetMethod(nameof(AccessoriesBarOnStartShowPostfix), BindingFlags.Public | BindingFlags.Static),
                0);

            if (!(reloadPatched && attackedPatched && initPatched && startShowPatched))
            {
                TryReleaseCompatibilityHooks(
                    "atomic install rollback",
                    out string releaseFailure);
                failure = "Frozen IEquipmentSlotsApi compatibility failed to install all four exact Hooks. " +
                    releaseFailure;
                return false;
            }

            SetEquipmentSlotsRuntimeHooksInstalled(true);
            SetEquipmentSlotsUiHooksInstalled(true);
            runtime.RuntimeMonitor.Log("Frozen IEquipmentSlotsApi compatibility installed all four native Hooks owner=" + CompatibilityHarmonyOwner + ".");
            return true;
        }

        private bool CanAttemptCompatibilityHook(int ordinal) =>
            compatibilityHookInstallGate == null ||
            compatibilityHookInstallGate(ordinal);

        private bool ReleaseCompatibilityHooksIfUnused()
        {
            if (equipmentSlotOptions.Values.Any(options => options.Enabled))
                return true;

            ClearEquipmentSlotsUiLifecycle("no enabled frozen compatibility owners");
            return TryReleaseCompatibilityHooks(
                "no enabled frozen compatibility owners",
                out _);
        }

        private bool TryReleaseCompatibilityHooks(string reason, out string result)
        {
            bool unpatchCallSucceeded = TryUnpatchCompatibilityOwner();
            MethodInfo?[] targets =
            {
                FindExactTarget("DolocTown.GameData.AgentEquipmentManager, Assembly-CSharp", "ReloadParams", 0),
                FindExactTarget("DolocTown.BodyController, Assembly-CSharp", "OnAttacked", 5),
                FindExactTarget("DolocTown.UI.AccessoriesBar, Assembly-CSharp", "__Init", 0),
                FindExactTarget("DolocTown.UI.AccessoriesBar, Assembly-CSharp", "OnStartShow", 0)
            };
            if (targets.Any(target => target == null))
            {
                result = "Could not prove exact-owner cleanup because one or more native targets were unavailable; callback retention remains fail-closed.";
                runtime.SetHookStatus(
                    "Player.EquipmentSlotsCompatibilityOwner",
                    "cleanup-unproven",
                    CompatibilityHarmonyOwner,
                    "reason=" + reason + "; unpatchCall=" + unpatchCallSucceeded);
                return false;
            }

            bool reloadOwned = HasHarmonyOwner(targets[0]!, CompatibilityHarmonyOwner);
            bool attackedOwned = HasHarmonyOwner(targets[1]!, CompatibilityHarmonyOwner);
            bool initOwned = HasHarmonyOwner(targets[2]!, CompatibilityHarmonyOwner);
            bool startShowOwned = HasHarmonyOwner(targets[3]!, CompatibilityHarmonyOwner);
            int remaining = (reloadOwned ? 1 : 0) +
                (attackedOwned ? 1 : 0) +
                (initOwned ? 1 : 0) +
                (startShowOwned ? 1 : 0);
            SetEquipmentSlotsRuntimeHooksInstalled(reloadOwned && attackedOwned);
            SetEquipmentSlotsUiHooksInstalled(initOwned || startShowOwned);
            if (remaining != 0)
            {
                result = "Exact-owner cleanup left " + remaining + "/4 Hook owner observations; callback retention remains active.";
                runtime.SetHookStatus(
                    "Player.EquipmentSlotsCompatibilityOwner",
                    "cleanup-failed-residual-owner",
                    CompatibilityHarmonyOwner,
                    "reason=" + reason + "; remaining=" + remaining + "/4; unpatchCall=" + unpatchCallSucceeded);
                return false;
            }

            compatibilityPatcher = null;
            ClearCallbackRuntime();
            SetEquipmentSlotsRuntimeHooksInstalled(false);
            SetEquipmentSlotsUiHooksInstalled(false);
            result = "Exact-owner cleanup observed 0/4 remaining Hooks.";
            runtime.SetHookStatus(
                "Player.EquipmentSlotsCompatibilityOwner",
                "inactive",
                CompatibilityHarmonyOwner,
                "reason=" + reason + "; remaining=0/4");
            return true;
        }

        private static bool TryUnpatchCompatibilityOwner()
        {
            try
            {
                Type? harmonyType = ResolveType("HarmonyLib.Harmony, 0Harmony");
                MethodInfo? unpatchAll = harmonyType?
                    .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
                    .FirstOrDefault(method =>
                    {
                        if (!method.Name.Equals("UnpatchAll", StringComparison.Ordinal))
                            return false;
                        ParameterInfo[] parameters = method.GetParameters();
                        return parameters.Length == 1 && parameters[0].ParameterType == typeof(string);
                    });
                if (unpatchAll == null)
                    return false;

                object? target = null;
                if (!unpatchAll.IsStatic)
                {
                    ConstructorInfo? constructor = harmonyType?.GetConstructor(new[] { typeof(string) });
                    target = constructor?.Invoke(new object[] { CompatibilityHarmonyOwner });
                    if (target == null)
                        return false;
                }
                unpatchAll.Invoke(target, new object[] { CompatibilityHarmonyOwner });
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void ClearCallbackRuntime()
        {
            lock (CallbackGate)
            {
                if (ReferenceEquals(callbackRuntime, this))
                    callbackRuntime = null;
            }
        }

        public static void AgentEquipmentReloadParamsPostfix(object __instance)
        {
            EquipmentSlotsCompatibilityService? target;
            lock (CallbackGate)
                target = callbackRuntime;
            target?.ApplyEquipmentSlotsAfterReloadParams(__instance);
        }

        public static void AccessoriesBarInitPostfix(object __instance)
        {
            EquipmentSlotsCompatibilityService? target;
            lock (CallbackGate)
                target = callbackRuntime;
            target?.RenderEquipmentSlotsUiForAccessoriesBar(__instance, "AccessoriesBar.__Init");
        }

        public static bool BodyControllerOnAttackedPrefix(
            object __instance,
            ref float __0,
            bool __1,
            object __2,
            object __3,
            ref bool __result,
            ref bool __4)
        {
            EquipmentSlotsCompatibilityService? target;
            lock (CallbackGate)
                target = callbackRuntime;
            return target?.HandleBodyControllerOnAttackedPrefix(__instance, ref __0, __1, __2, __3, ref __result, ref __4) ?? true;
        }

        public static void AccessoriesBarOnStartShowPostfix(object __instance)
        {
            EquipmentSlotsCompatibilityService? target;
            lock (CallbackGate)
                target = callbackRuntime;
            target?.RenderEquipmentSlotsUiForAccessoriesBar(__instance, "AccessoriesBar.OnStartShow");
        }

        private static MethodInfo? FindExactTarget(string typeName, string methodName, int parameterCount)
        {
            Type? type = ResolveType(typeName);
            for (Type? current = type; current != null; current = current.BaseType)
            {
                MethodInfo? result = current
                    .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .SingleOrDefault(method =>
                        method.Name.Equals(methodName, StringComparison.Ordinal) &&
                        method.GetParameters().Length == parameterCount);
                if (result != null)
                    return result;
            }
            return null;
        }

        private static bool HasHarmonyOwner(MethodBase target, string owner)
        {
            try
            {
                Type? harmonyType = ResolveType("HarmonyLib.Harmony, 0Harmony");
                MethodInfo? getPatchInfo = harmonyType?.GetMethod(
                    "GetPatchInfo",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(MethodBase) },
                    null);
                object? patchInfo = getPatchInfo?.Invoke(null, new object[] { target });
                object? ownersValue = patchInfo?.GetType()
                    .GetProperty("Owners", BindingFlags.Public | BindingFlags.Instance)?
                    .GetValue(patchInfo);
                if (!(ownersValue is IEnumerable owners))
                    return false;

                foreach (object? value in owners)
                {
                    if (string.Equals(value as string, owner, StringComparison.Ordinal))
                        return true;
                }
            }
            catch
            {
            }
            return false;
        }
    }
}
