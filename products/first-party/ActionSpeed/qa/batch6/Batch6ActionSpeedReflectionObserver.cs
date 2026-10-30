using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class Batch6ActionSpeedObservation
    {
        internal bool ProductPresent { get; set; }
        internal bool ProductAssemblyLoaded { get; set; }
        internal int InstalledPatchCount { get; set; }
        internal int ResolvedHarmonyTargetCount { get; set; }
        internal int CanonicalHarmonyTargetCount { get; set; }
        internal int CanonicalHarmonyPatchCount { get; set; }
        internal bool ProductCallbackRuntimePresent { get; set; }
        internal int CoreOwnerRootCount { get; set; }
        internal int LoadedOwnerCount { get; set; }
        internal string HarmonyInventoryDetails { get; set; } = string.Empty;
        internal bool ToolConfigured { get; set; }
        internal bool InteractionConfigured { get; set; }
        internal int ApplicationCount { get; set; }
        internal int ContinuousUseApplicationCount { get; set; }
        internal int AutoFillApplicationCount { get; set; }
        internal string LastApplicationSummary { get; set; } = string.Empty;
        internal string LastContinuousUseSummary { get; set; } = string.Empty;
        internal string LastAutoFillSummary { get; set; } = string.Empty;
        internal int AnimatorSnapshotCount { get; set; }
        internal bool PendingAnimalInteract { get; set; }
        internal bool PendingAnimalTimestampActive { get; set; }
        internal bool AutoFillCooldownActive { get; set; }
        internal bool UpdateSubscribed { get; set; }
        internal object? NativeRuntime { get; set; }

        internal int NativeTransientCount => AnimatorSnapshotCount +
            (PendingAnimalInteract ? 1 : 0) +
            (PendingAnimalTimestampActive ? 1 : 0) +
            (AutoFillCooldownActive ? 1 : 0);

        internal bool ActualHarmonyOwnerReady => ProductAssemblyLoaded &&
            ResolvedHarmonyTargetCount == 11 &&
            CanonicalHarmonyTargetCount == 11 &&
            CanonicalHarmonyPatchCount == 11;
    }

    internal sealed class Batch6ActionSpeedReflectionObserver
    {
        private const string UniqueId = "Yuuka.DTMAPI.ActionSpeed";
        private const string EntryType = "Yuuka.DTMAPI.ActionSpeed.ModEntry";
        private const string ProductAssemblyName = "Yuuka.DTMAPI.ActionSpeed";
        private const string HarmonyOwner = "dtmapi.mod.yuuka.dtmapi.actionspeed";
        private static readonly Batch6HarmonyPatchTarget[] HarmonyTargets =
        {
            new Batch6HarmonyPatchTarget("DolocTown.AgentStateTool", "OnEnter", 0),
            new Batch6HarmonyPatchTarget("DolocTown.AgentStateTool", "OnExit", 0),
            new Batch6HarmonyPatchTarget("DolocTown.AgentStateWater", "OnEnter", 0),
            new Batch6HarmonyPatchTarget("DolocTown.AgentStateWater", "OnExit", 0),
            new Batch6HarmonyPatchTarget("DolocTown.AgentStateInteract", "OnEnter", 0),
            new Batch6HarmonyPatchTarget("DolocTown.AgentStateInteract", "OnExit", 0),
            new Batch6HarmonyPatchTarget("DolocTown.AgentStateEat", "OnEnter", 0),
            new Batch6HarmonyPatchTarget("DolocTown.AgentControllerState", "UseItemContinues", 1),
            new Batch6HarmonyPatchTarget("DolocTown.AgentControllerState", "InteractContinues", 1),
            new Batch6HarmonyPatchTarget("DolocTown.AnimalRenderer", "OnInteract", 0),
            new Batch6HarmonyPatchTarget("AgentStateBase", "OnExit", 0)
        };

        internal Batch6ActionSpeedObservation Observe(DtmApiRuntime runtime)
        {
            if (runtime == null)
                throw new ArgumentNullException(nameof(runtime));
            IDictionary instances = ReadField(runtime, "modInstances") as IDictionary
                ?? throw new InvalidOperationException("DtmApiRuntime.modInstances is unavailable.");
            var observation = new Batch6ActionSpeedObservation();
            PopulateProcessLifetimeState(runtime, observation);
            if (!instances.Contains(UniqueId))
                return observation;

            object entry = instances[UniqueId] ?? throw new InvalidOperationException("ActionSpeed product entry is null.");
            Type entryType = entry.GetType();
            if (!string.Equals(entryType.FullName, EntryType, StringComparison.Ordinal) ||
                !string.Equals(entryType.Assembly.GetName().Name, "Yuuka.DTMAPI.ActionSpeed", StringComparison.Ordinal))
                throw new InvalidOperationException("ActionSpeed loaded entry identity is not the admitted Advanced product.");

            object nativeRuntime = RequireField(entry, "nativeRuntime");
            object engine = RequireField(nativeRuntime, "engine");
            object diagnostics = InvokeParameterless(engine, "EnableQaObservation");
            object config = RequireField(entry, "config");
            IDictionary animatorSnapshots = RequireField(engine, "originalAnimatorSpeeds") as IDictionary
                ?? throw new InvalidOperationException("ActionSpeed animator snapshot table is unavailable.");
            bool enabled = ReadBoolean(config, "Enabled");
            double toolMultiplier = ReadDouble(config, "ToolMultiplier");
            double interactionMultiplier = Math.Max(
                Math.Max(ReadDouble(config, "BottleFillMultiplier"), ReadDouble(config, "EatDrinkMultiplier")),
                Math.Max(Math.Max(ReadDouble(config, "MachineAddMultiplier"), ReadDouble(config, "HarvestMultiplier")), ReadDouble(config, "PlantMultiplier")));

            observation.ProductPresent = true;
            observation.NativeRuntime = nativeRuntime;
            observation.InstalledPatchCount = ReadInt32Property(nativeRuntime, "InstalledPatchCount");
            observation.ToolConfigured = enabled && ReadBoolean(config, "ToolSpeedEnabled") && toolMultiplier > 1;
            observation.InteractionConfigured = enabled && interactionMultiplier > 1 &&
                (ReadBoolean(config, "BottleFillSpeedEnabled") || ReadBoolean(config, "EatDrinkSpeedEnabled") ||
                 ReadBoolean(config, "MachineAddSpeedEnabled") || ReadBoolean(config, "HarvestSpeedEnabled") ||
                 ReadBoolean(config, "PlantSpeedEnabled") || ReadBoolean(config, "ContinuousDrinkWithRightClick"));
            observation.ApplicationCount = ReadInt32Property(diagnostics, "ActionSpeedApplicationCount");
            observation.ContinuousUseApplicationCount = ReadInt32Property(diagnostics, "ActionSpeedContinuousUseApplicationCount");
            observation.AutoFillApplicationCount = ReadInt32Property(diagnostics, "ActionSpeedAutoFillApplicationCount");
            observation.LastApplicationSummary = ReadStringProperty(diagnostics, "LastActionSpeedApplicationSummary");
            observation.LastContinuousUseSummary = ReadStringProperty(diagnostics, "LastActionSpeedContinuousUseSummary");
            observation.LastAutoFillSummary = ReadStringProperty(diagnostics, "LastActionSpeedAutoFillSummary");
            observation.AnimatorSnapshotCount = animatorSnapshots.Count;
            observation.PendingAnimalInteract = ReadField(engine, "pendingNativeAnimalInteract") != null;
            observation.PendingAnimalTimestampActive = ReadDateTimeOffset(engine, "pendingNativeAnimalInteractAt") != DateTimeOffset.MinValue;
            observation.AutoFillCooldownActive = ReadDateTimeOffset(engine, "lastActionSpeedAutoFillAt") != DateTimeOffset.MinValue;
            observation.UpdateSubscribed = ReadBoolean(entry, "updateSubscribed");
            return observation;
        }

        private static void PopulateProcessLifetimeState(DtmApiRuntime runtime, Batch6ActionSpeedObservation observation)
        {
            Batch6HarmonyOwnerInventory inventory = Batch6AdvancedHarmonyOwnerObserver.Observe(ProductAssemblyName, HarmonyOwner, HarmonyTargets);
            observation.ProductAssemblyLoaded = inventory.ProductAssemblyLoaded;
            observation.ResolvedHarmonyTargetCount = inventory.ResolvedTargetCount;
            observation.CanonicalHarmonyTargetCount = inventory.ExactOwnerTargetCount;
            observation.CanonicalHarmonyPatchCount = inventory.ExactOwnerPatchCount;
            observation.HarmonyInventoryDetails = inventory.Details;
            observation.CoreOwnerRootCount = runtime.CountCoreOwnerRoots(UniqueId);
            observation.LoadedOwnerCount = runtime.LoadedMods.Count(mod =>
                mod.Manifest.UniqueID.Equals(UniqueId, StringComparison.OrdinalIgnoreCase));

            Assembly? productAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(assembly => string.Equals(assembly.GetName().Name, ProductAssemblyName, StringComparison.Ordinal));
            Type? callbacks = productAssembly?.GetType("Yuuka.DTMAPI.ActionSpeed.ActionSpeedCallbacks", throwOnError: false, ignoreCase: false);
            FieldInfo? callbackRuntime = callbacks?.GetField("runtime", BindingFlags.Static | BindingFlags.NonPublic);
            observation.ProductCallbackRuntimePresent = callbackRuntime?.GetValue(null) != null;
        }

        internal void ResetBoundary(DtmApiRuntime runtime, string reason)
        {
            Batch6ActionSpeedObservation observation = Observe(runtime);
            if (!observation.ProductPresent || observation.NativeRuntime == null)
                return;
            MethodInfo method = FindMethod(observation.NativeRuntime.GetType(), "ResetBoundary")
                ?? throw new MissingMethodException(observation.NativeRuntime.GetType().FullName, "ResetBoundary");
            method.Invoke(observation.NativeRuntime, new object[] { reason ?? string.Empty });
        }

        private static object RequireField(object instance, string name) =>
            ReadField(instance, name) ?? throw new MissingFieldException(instance.GetType().FullName, name);

        private static object InvokeParameterless(object instance, string name)
        {
            MethodInfo method = FindMethod(instance.GetType(), name)
                ?? throw new MissingMethodException(instance.GetType().FullName, name);
            return method.Invoke(instance, Array.Empty<object>())
                ?? throw new InvalidOperationException(instance.GetType().FullName + "." + name + " returned null.");
        }

        private static object? ReadField(object instance, string name)
        {
            for (Type? type = instance.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo? field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                if (field != null)
                    return field.GetValue(instance);
            }
            return null;
        }

        private static MethodInfo? FindMethod(Type type, string name)
        {
            for (Type? current = type; current != null; current = current.BaseType)
            {
                MethodInfo? method = current.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                if (method != null)
                    return method;
            }
            return null;
        }

        private static object? ReadProperty(object instance, string name) =>
            instance.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(instance);

        private static int ReadInt32Property(object instance, string name) => Convert.ToInt32(ReadProperty(instance, name) ?? 0);
        private static string ReadStringProperty(object instance, string name) => Convert.ToString(ReadProperty(instance, name)) ?? string.Empty;
        private static bool ReadBoolean(object instance, string name) => Convert.ToBoolean(ReadField(instance, name) ?? ReadProperty(instance, name) ?? false);
        private static double ReadDouble(object instance, string name) => Convert.ToDouble(ReadField(instance, name) ?? ReadProperty(instance, name) ?? 0d);
        private static DateTimeOffset ReadDateTimeOffset(object instance, string name) =>
            (DateTimeOffset)(ReadField(instance, name) ?? ReadProperty(instance, name) ?? DateTimeOffset.MinValue);
    }
}
