using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown.QA
{
    [DataContract]
    internal sealed class Batch6AutoFishingObservation
    {
        [DataMember(Name = "observedAtUtc", Order = 1)] internal DateTimeOffset ObservedAtUtc { get; set; }
        [DataMember(Name = "productPresent", Order = 2)] internal bool ProductPresent { get; set; }
        [DataMember(Name = "uniqueId", Order = 3)] internal string UniqueId { get; set; } = string.Empty;
        [DataMember(Name = "entryType", Order = 4)] internal string EntryType { get; set; } = string.Empty;
        [DataMember(Name = "entryAssemblyName", Order = 5)] internal string EntryAssemblyName { get; set; } = string.Empty;
        [DataMember(Name = "qaHasStaticProductAssemblyRef", Order = 6)] internal bool QaHasStaticProductAssemblyRef { get; set; }
        [DataMember(Name = "enabled", Order = 7)] internal bool Enabled { get; set; }
        [DataMember(Name = "updateSubscribed", Order = 8)] internal bool UpdateSubscribed { get; set; }
        [DataMember(Name = "sessionPresent", Order = 9)] internal bool SessionPresent { get; set; }
        [DataMember(Name = "sessionReleased", Order = 10)] internal bool SessionReleased { get; set; }
        [DataMember(Name = "installedPatchCount", Order = 11)] internal int InstalledPatchCount { get; set; }
        [DataMember(Name = "transitionCount", Order = 12)] internal long TransitionCount { get; set; }
        [DataMember(Name = "fishCompletedCount", Order = 13)] internal long FishCompletedCount { get; set; }
        [DataMember(Name = "pullEnteredCount", Order = 14)] internal long PullEnteredCount { get; set; }
        [DataMember(Name = "pullExitedCount", Order = 15)] internal long PullExitedCount { get; set; }
        [DataMember(Name = "nativeVisibleReelCount", Order = 16)] internal long NativeVisibleReelCount { get; set; }
        [DataMember(Name = "nativeSkipReelCount", Order = 17)] internal long NativeSkipReelCount { get; set; }
        [DataMember(Name = "visibleReelQueued", Order = 18)] internal long VisibleReelQueued { get; set; }
        [DataMember(Name = "visibleReelConsumed", Order = 19)] internal long VisibleReelConsumed { get; set; }
        [DataMember(Name = "visibleReelNativeAccepted", Order = 20)] internal long VisibleReelNativeAccepted { get; set; }
        [DataMember(Name = "visibleReelRetries", Order = 21)] internal long VisibleReelRetries { get; set; }
        [DataMember(Name = "visibleReelTimeouts", Order = 22)] internal long VisibleReelTimeouts { get; set; }
        [DataMember(Name = "nativeAccessorBuildCount", Order = 23)] internal int NativeAccessorBuildCount { get; set; }
        [DataMember(Name = "nativeAccessorFailureCount", Order = 24)] internal int NativeAccessorFailureCount { get; set; }
        [DataMember(Name = "nativeFrameRefreshCount", Order = 25)] internal long NativeFrameRefreshCount { get; set; }
        [DataMember(Name = "nativeTransientCount", Order = 26)] internal int NativeTransientCount { get; set; }
        [DataMember(Name = "toggleKey", Order = 27)] internal string ToggleKey { get; set; } = string.Empty;
        [DataMember(Name = "instantBite", Order = 28)] internal bool InstantBite { get; set; }
        [DataMember(Name = "skipMiniGame", Order = 29)] internal bool SkipMiniGame { get; set; }
        [DataMember(Name = "fastAnimations", Order = 30)] internal bool FastAnimations { get; set; }
        [DataMember(Name = "animationMultiplier", Order = 31)] internal double AnimationMultiplier { get; set; }
        [DataMember(Name = "productAssemblyLoaded", Order = 32)] internal bool ProductAssemblyLoaded { get; set; }
        [DataMember(Name = "castChargeRatio", Order = 33)] internal double CastChargeRatio { get; set; }
        [DataMember(Name = "lastReason", Order = 34)] internal string LastReason { get; set; } = string.Empty;
        [DataMember(Name = "castAppliedCount", Order = 35)] internal long CastAppliedCount { get; set; }
        [DataMember(Name = "nativeBitePreparedCount", Order = 36)] internal long NativeBitePreparedCount { get; set; }
        [DataMember(Name = "animationApplicationCount", Order = 37)] internal int AnimationApplicationCount { get; set; }
        [DataMember(Name = "readyChargeApplicationCount", Order = 38)] internal int ReadyChargeApplicationCount { get; set; }
        [DataMember(Name = "productCallbackRuntimePresent", Order = 39)] internal bool ProductCallbackRuntimePresent { get; set; }
        [DataMember(Name = "canonicalHarmonyPatchCount", Order = 40)] internal int CanonicalHarmonyPatchCount { get; set; }
        [DataMember(Name = "coreOwnerRootCount", Order = 41)] internal int CoreOwnerRootCount { get; set; }
        [DataMember(Name = "coreInstanceCount", Order = 42)] internal int CoreInstanceCount { get; set; }
        [DataMember(Name = "loadedOwnerCount", Order = 43)] internal int LoadedOwnerCount { get; set; }
        [DataMember(Name = "ownerRequiresRestart", Order = 44)] internal bool OwnerRequiresRestart { get; set; }
        [DataMember(Name = "diagnosticsStatusCode", Order = 45)] internal string DiagnosticsStatusCode { get; set; } = string.Empty;
        [DataMember(Name = "inputOverrideActive", Order = 46)] internal bool InputOverrideActive { get; set; }
        [DataMember(Name = "visibleReelInputPending", Order = 47)] internal bool VisibleReelInputPending { get; set; }
        [DataMember(Name = "readyTargetCount", Order = 48)] internal int ReadyTargetCount { get; set; }
        [DataMember(Name = "readyReleasedCount", Order = 49)] internal int ReadyReleasedCount { get; set; }
        [DataMember(Name = "currentReadyStatePresent", Order = 50)] internal bool CurrentReadyStatePresent { get; set; }
        [DataMember(Name = "animatorSpeedSnapshotCount", Order = 51)] internal int AnimatorSpeedSnapshotCount { get; set; }
        [DataMember(Name = "hookGravitySnapshotCount", Order = 52)] internal int HookGravitySnapshotCount { get; set; }
        [DataMember(Name = "hookVelocitySnapshotCount", Order = 53)] internal int HookVelocitySnapshotCount { get; set; }
        [DataMember(Name = "pullDurationSnapshotCount", Order = 54)] internal int PullDurationSnapshotCount { get; set; }
        [DataMember(Name = "deepNativeTransientCount", Order = 55)] internal int DeepNativeTransientCount { get; set; }
        internal bool ToggleAwaitingRelease { get; set; }
    }

    internal sealed class Batch6AutoFishingReflectionObserver
    {
        private static readonly object MemberCacheSync = new object();
        private static readonly Dictionary<Type, Dictionary<string, FieldInfo>> FieldCache = new Dictionary<Type, Dictionary<string, FieldInfo>>();
        private static readonly Dictionary<Type, Dictionary<string, PropertyInfo>> PropertyCache = new Dictionary<Type, Dictionary<string, PropertyInfo>>();
        private readonly string uniqueId;
        private readonly string entryType;
        private readonly Assembly qaAssembly;
        private readonly bool qaHasStaticProductReference;
        private readonly bool enforceProductAssemblyName;
        private bool productAssemblyLoaded;
        private Type? cachedEntryType;
        private bool cachedStaticProductReference;

        internal Batch6AutoFishingReflectionObserver(
            string uniqueId = Batch6AutoFishingPilotSettings.ProductUniqueId,
            string entryType = Batch6AutoFishingPilotSettings.ProductEntryType,
            Assembly? qaAssembly = null)
        {
            this.uniqueId = uniqueId ?? throw new ArgumentNullException(nameof(uniqueId));
            this.entryType = entryType ?? throw new ArgumentNullException(nameof(entryType));
            this.qaAssembly = qaAssembly ?? typeof(Batch6AutoFishingReflectionObserver).Assembly;
            enforceProductAssemblyName = string.Equals(uniqueId, Batch6AutoFishingPilotSettings.ProductUniqueId, StringComparison.Ordinal) &&
                string.Equals(entryType, Batch6AutoFishingPilotSettings.ProductEntryType, StringComparison.Ordinal);
            qaHasStaticProductReference = this.qaAssembly.GetReferencedAssemblies()
                .Any(reference => string.Equals(reference.Name, Batch6AutoFishingPilotSettings.ProductAssemblyName, StringComparison.Ordinal));
            RefreshProcessAssemblyState();
        }

        internal void RefreshProcessAssemblyState()
        {
            productAssemblyLoaded = AppDomain.CurrentDomain.GetAssemblies()
                .Any(assembly => string.Equals(assembly.GetName().Name, Batch6AutoFishingPilotSettings.ProductAssemblyName, StringComparison.Ordinal));
        }

        internal Batch6AutoFishingObservation Observe(object runtime, DateTimeOffset? observedAtUtc = null)
        {
            if (runtime == null)
                throw new ArgumentNullException(nameof(runtime));

            // Re-evaluate the process assembly set at the same bounded cadence as
            // the ordinary observer so product-absent L0 cannot pass after a late
            // AutoFishing load that never registered a Core ModEntry.
            RefreshProcessAssemblyState();

            IDictionary instances = ReadField(runtime, "modInstances") as IDictionary
                ?? throw new InvalidDataException("DtmApiRuntime.modInstances is unavailable or is not an IDictionary.");
            var observation = new Batch6AutoFishingObservation
            {
                ObservedAtUtc = observedAtUtc ?? DateTimeOffset.UtcNow,
                UniqueId = uniqueId,
                QaHasStaticProductAssemblyRef = qaHasStaticProductReference,
                ProductAssemblyLoaded = productAssemblyLoaded
            };
            if (qaHasStaticProductReference)
                throw new InvalidDataException("The optional QA assembly has a forbidden static Product AssemblyRef to " + Batch6AutoFishingPilotSettings.ProductAssemblyName + ".");
            if (!instances.Contains(uniqueId))
            {
                PopulateProcessLifetimeProductState(observation);
                PopulateCoreOwnerState(runtime, observation, uniqueId);
                return observation;
            }

            object entry = instances[uniqueId]
                ?? throw new InvalidDataException("DtmApiRuntime.modInstances contains a null AutoFishing entry.");
            Type actualType = entry.GetType();
            if (!string.Equals(actualType.FullName, entryType, StringComparison.Ordinal))
                throw new InvalidDataException("The Core-resident AutoFishing instance has unexpected EntryType " + (actualType.FullName ?? actualType.Name) + ".");

            string assemblyName = actualType.Assembly.GetName().Name ?? string.Empty;
            if (enforceProductAssemblyName &&
                !string.Equals(assemblyName, Batch6AutoFishingPilotSettings.ProductAssemblyName, StringComparison.Ordinal))
                throw new InvalidDataException("The Core-resident AutoFishing entry assembly name is unexpected: " + assemblyName + ".");
            if (!ReferenceEquals(cachedEntryType, actualType))
            {
                cachedEntryType = actualType;
                cachedStaticProductReference = qaHasStaticProductReference;
            }
            if (cachedStaticProductReference)
                throw new InvalidDataException("The optional QA assembly has a forbidden static Product AssemblyRef to " + assemblyName + ".");

            object nativeRuntime = RequireField(entry, "nativeRuntime");
            object primitives = RequireField(entry, "primitives");
            object diagnostics = InvokeParameterless(primitives, "EnableQaObservation");
            object config = RequireField(entry, "config");
            object? session = ReadField(entry, "session");
            Batch6AutoFishingDeepTransientSnapshot deep = Batch6AutoFishingDeepTransientObserver.CaptureFromPrimitives(primitives);
            int transient = ReadInt32Property(primitives, "ActiveSessionCount") +
                ReadInt32Property(primitives, "ActiveInputLeaseCount") +
                ReadInt32Property(primitives, "ActiveAnimationLeaseCount") +
                (ReadBooleanProperty(primitives, "SchedulerPending") ? 1 : 0) +
                deep.TotalCount;

            long pullExited = ReadInt64Property(diagnostics, "PullExitedCount");
            observation.ProductPresent = true;
            observation.EntryType = actualType.FullName ?? actualType.Name;
            observation.EntryAssemblyName = assemblyName;
            observation.QaHasStaticProductAssemblyRef = qaHasStaticProductReference;
            observation.ProductAssemblyLoaded = true;
            observation.Enabled = ReadBooleanField(entry, "enabled");
            observation.UpdateSubscribed = ReadBooleanField(entry, "updateSubscribed");
            observation.ToggleAwaitingRelease = ReadBooleanField(entry, "toggleAwaitingRelease");
            observation.SessionPresent = session != null;
            observation.SessionReleased = session != null && ReadBooleanProperty(session, "IsReleased");
            observation.InstalledPatchCount = ReadInt32Property(nativeRuntime, "InstalledPatchCount");
            observation.TransitionCount = ReadInt64Property(diagnostics, "TransitionCount");
            // PullExited is the product's only durable completed-fish diagnostic.
            // The evidence names that semantic projection explicitly and also keeps
            // the exact source counter beside it; no independent fish count is invented.
            observation.FishCompletedCount = pullExited;
            observation.PullEnteredCount = ReadInt64Property(diagnostics, "PullEnteredCount");
            observation.PullExitedCount = pullExited;
            observation.NativeVisibleReelCount = ReadInt64Property(diagnostics, "NativeVisibleReelCount");
            observation.NativeSkipReelCount = ReadInt64Property(diagnostics, "NativeSkipReelCount");
            observation.VisibleReelQueued = ReadInt64Property(diagnostics, "VisibleReelQueued");
            observation.VisibleReelConsumed = ReadInt64Property(diagnostics, "VisibleReelConsumed");
            observation.VisibleReelNativeAccepted = ReadInt64Property(diagnostics, "VisibleReelNativeAccepted");
            observation.VisibleReelRetries = ReadInt64Property(diagnostics, "VisibleReelRetries");
            observation.VisibleReelTimeouts = ReadInt64Property(diagnostics, "VisibleReelTimeouts");
            observation.NativeAccessorBuildCount = ReadInt32Property(primitives, "NativeAccessorBuildCount");
            observation.NativeAccessorFailureCount = ReadInt32Property(primitives, "NativeAccessorFailureCount");
            observation.NativeFrameRefreshCount = ReadInt64Property(primitives, "NativeFrameRefreshCount");
            observation.NativeTransientCount = transient;
            observation.InputOverrideActive = deep.InputOverrideActive;
            observation.VisibleReelInputPending = deep.VisibleReelInputPending;
            observation.ReadyTargetCount = deep.ReadyTargetCount;
            observation.ReadyReleasedCount = deep.ReadyReleasedCount;
            observation.CurrentReadyStatePresent = deep.CurrentReadyStatePresent;
            observation.AnimatorSpeedSnapshotCount = deep.AnimatorSpeedSnapshotCount;
            observation.HookGravitySnapshotCount = deep.HookGravitySnapshotCount;
            observation.HookVelocitySnapshotCount = deep.HookVelocitySnapshotCount;
            observation.PullDurationSnapshotCount = deep.PullDurationSnapshotCount;
            observation.DeepNativeTransientCount = deep.TotalCount;
            observation.LastReason = Convert.ToString(ReadField(entry, "lastReason"), CultureInfo.InvariantCulture) ?? string.Empty;
            observation.CastAppliedCount = ReadInt64Property(diagnostics, "CastAppliedCount");
            observation.NativeBitePreparedCount = ReadInt64Property(diagnostics, "NativeBitePreparedCount");
            observation.AnimationApplicationCount = ReadInt32Property(diagnostics, "AnimationApplicationCount");
            observation.ReadyChargeApplicationCount = ReadInt32Property(diagnostics, "ReadyChargeApplicationCount");
            observation.ToggleKey = ReadStringProperty(config, "ToggleKey");
            observation.InstantBite = ReadBooleanProperty(config, "InstantBite");
            observation.SkipMiniGame = ReadBooleanProperty(config, "SkipMiniGame");
            observation.FastAnimations = ReadBooleanProperty(config, "FastAnimations");
            observation.AnimationMultiplier = ReadDoubleProperty(config, "AnimationMultiplier");
            observation.CastChargeRatio = ReadDoubleProperty(config, "CastChargeRatio");
            PopulateProcessLifetimeProductState(observation);
            PopulateCoreOwnerState(runtime, observation, uniqueId);
            return observation;
        }

        private static void PopulateCoreOwnerState(object runtime, Batch6AutoFishingObservation observation, string ownerId)
        {
            if (!(runtime is DtmApiRuntime typed))
                return;
            observation.CoreOwnerRootCount = typed.CountCoreOwnerRoots(ownerId);
            observation.CoreInstanceCount = typed.HasOwnerInstance(ownerId) ? 1 : 0;
            observation.LoadedOwnerCount = typed.LoadedMods.Count(mod =>
                mod.Manifest.UniqueID.Equals(ownerId, StringComparison.OrdinalIgnoreCase));
            observation.OwnerRequiresRestart = typed.OwnerRequiresRestart(ownerId);
            IDtmModStatusInfo? status = ((IDtmDiagnosticsApi)typed).GetSnapshot().Mods.FirstOrDefault(item =>
                item.UniqueID.Equals(ownerId, StringComparison.OrdinalIgnoreCase));
            observation.DiagnosticsStatusCode = status?.StatusCode ?? string.Empty;
        }

        private static void PopulateProcessLifetimeProductState(Batch6AutoFishingObservation observation)
        {
            Assembly? productAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(assembly => string.Equals(assembly.GetName().Name, Batch6AutoFishingPilotSettings.ProductAssemblyName, StringComparison.Ordinal));
            if (productAssembly != null)
            {
                Type callbacks = productAssembly.GetType("Yuuka.DTMAPI.AutoFishing.FishingProductCallbacks", throwOnError: true, ignoreCase: false)
                    ?? throw new InvalidDataException("Loaded AutoFishing assembly does not contain FishingProductCallbacks.");
                FieldInfo runtime = callbacks.GetField("runtime", BindingFlags.Static | BindingFlags.NonPublic)
                    ?? throw new InvalidDataException("FishingProductCallbacks.runtime is unavailable.");
                observation.ProductCallbackRuntimePresent = runtime.GetValue(null) != null;
            }
            observation.CanonicalHarmonyPatchCount = productAssembly == null
                ? 0
                : CountHarmonyOwnerPatches("dtmapi.mod.yuuka.dtmapi.autofishing", productAssembly);
        }

        private static int CountHarmonyOwnerPatches(string owner, Assembly productAssembly)
        {
            AssemblyName? harmonyReference = productAssembly.GetReferencedAssemblies()
                .SingleOrDefault(reference => string.Equals(
                    reference.Name,
                    Batch6AutoFishingPilotSettings.ProductHarmonyAssemblyName,
                    StringComparison.Ordinal));
            if (harmonyReference == null)
                throw new InvalidDataException("Loaded AutoFishing assembly does not reference the receipt-bound 0Harmony assembly.");
            Assembly[] loadedHarmonyAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => string.Equals(
                    assembly.GetName().Name,
                    harmonyReference.Name,
                    StringComparison.Ordinal))
                .ToArray();
            if (loadedHarmonyAssemblies.Length != 1)
            {
                throw new InvalidDataException(
                    "Expected exactly one loaded receipt-bound 0Harmony assembly but observed " +
                    loadedHarmonyAssemblies.Length.ToString(CultureInfo.InvariantCulture) + ".");
            }
            Type harmony = loadedHarmonyAssemblies[0].GetType("HarmonyLib.Harmony", throwOnError: true, ignoreCase: false)
                ?? throw new InvalidDataException("The receipt-bound 0Harmony assembly does not expose HarmonyLib.Harmony.");
            MethodInfo getAll = harmony.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(method => method.Name == "GetAllPatchedMethods" && method.GetParameters().Length == 0)
                ?? throw new InvalidDataException("Harmony.GetAllPatchedMethods() is unavailable.");
            MethodInfo getInfo = harmony.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(method => method.Name == "GetPatchInfo" && method.GetParameters().Length == 1)
                ?? throw new InvalidDataException("Harmony.GetPatchInfo(MethodBase) is unavailable.");
            IEnumerable methods = getAll.Invoke(null, Array.Empty<object>()) as IEnumerable
                ?? throw new InvalidDataException("Harmony.GetAllPatchedMethods() did not return an enumerable.");
            int count = 0;
            foreach (object? method in methods)
            {
                if (method == null)
                    continue;
                object? info = getInfo.Invoke(null, new[] { method });
                if (info == null)
                    continue;
                foreach (string collectionName in new[] { "Prefixes", "Postfixes", "Transpilers", "Finalizers" })
                {
                    if (!(ReadHarmonyMember(info, collectionName) is IEnumerable patches))
                        continue;
                    foreach (object? patch in patches)
                    {
                        if (patch == null)
                            continue;
                        string patchOwner = Convert.ToString(
                            ReadHarmonyMember(patch, "owner") ?? ReadHarmonyMember(patch, "Owner"),
                            CultureInfo.InvariantCulture) ?? string.Empty;
                        if (string.Equals(patchOwner, owner, StringComparison.Ordinal))
                            count++;
                    }
                }
            }
            return count;
        }

        private static object? ReadHarmonyMember(object value, string name)
        {
            Type type = value.GetType();
            PropertyInfo? property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null && property.GetIndexParameters().Length == 0)
                return property.GetValue(value, null);
            FieldInfo? field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return field?.GetValue(value);
        }

        private static object RequireField(object target, string name) =>
            ReadField(target, name) ?? throw new InvalidDataException(target.GetType().FullName + "." + name + " is null.");

        private static object? ReadField(object target, string name)
        {
            FieldInfo? field = FindField(target.GetType(), name);
            if (field == null)
                throw new InvalidDataException(target.GetType().FullName + " does not expose required field " + name + ".");
            try { return field.GetValue(target); }
            catch (Exception ex) { throw ReflectionFailure(target, name, ex); }
        }

        private static object RequireProperty(object target, string name) =>
            ReadProperty(target, name) ?? throw new InvalidDataException(target.GetType().FullName + "." + name + " is null.");

        private static object? ReadProperty(object target, string name)
        {
            PropertyInfo? property = FindProperty(target.GetType(), name);
            if (property == null || property.GetIndexParameters().Length != 0)
                throw new InvalidDataException(target.GetType().FullName + " does not expose required property " + name + ".");
            try { return property.GetValue(target, null); }
            catch (Exception ex) { throw ReflectionFailure(target, name, ex); }
        }

        private static object InvokeParameterless(object target, string name)
        {
            MethodInfo? method = null;
            for (Type? current = target.GetType(); current != null && method == null; current = current.BaseType)
            {
                method = current.GetMethod(
                    name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                    binder: null,
                    types: Type.EmptyTypes,
                    modifiers: null);
            }
            if (method == null)
                throw new InvalidDataException(target.GetType().FullName + " does not expose required method " + name + "().");
            try
            {
                return method.Invoke(target, Array.Empty<object>())
                    ?? throw new InvalidDataException(target.GetType().FullName + "." + name + "() returned null.");
            }
            catch (Exception ex)
            {
                throw ReflectionFailure(target, name, ex);
            }
        }

        private static bool ReadBooleanField(object target, string name) =>
            Convert.ToBoolean(ReadField(target, name), CultureInfo.InvariantCulture);

        private static bool ReadBooleanProperty(object target, string name) =>
            Convert.ToBoolean(ReadProperty(target, name), CultureInfo.InvariantCulture);

        private static int ReadInt32Property(object target, string name) =>
            Convert.ToInt32(ReadProperty(target, name), CultureInfo.InvariantCulture);

        private static long ReadInt64Property(object target, string name) =>
            Convert.ToInt64(ReadProperty(target, name), CultureInfo.InvariantCulture);

        private static double ReadDoubleProperty(object target, string name) =>
            Convert.ToDouble(ReadProperty(target, name), CultureInfo.InvariantCulture);

        private static string ReadStringProperty(object target, string name) =>
            Convert.ToString(ReadProperty(target, name), CultureInfo.InvariantCulture) ?? string.Empty;

        private static FieldInfo? FindField(Type type, string name)
        {
            lock (MemberCacheSync)
            {
                if (FieldCache.TryGetValue(type, out Dictionary<string, FieldInfo> cached) && cached.TryGetValue(name, out FieldInfo value))
                    return value;
            }
            for (Type? current = type; current != null; current = current.BaseType)
            {
                FieldInfo? field = current.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null)
                {
                    lock (MemberCacheSync)
                    {
                        if (!FieldCache.TryGetValue(type, out Dictionary<string, FieldInfo> cached))
                        {
                            cached = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
                            FieldCache[type] = cached;
                        }
                        cached[name] = field;
                    }
                    return field;
                }
            }
            return null;
        }

        private static PropertyInfo? FindProperty(Type type, string name)
        {
            lock (MemberCacheSync)
            {
                if (PropertyCache.TryGetValue(type, out Dictionary<string, PropertyInfo> cached) && cached.TryGetValue(name, out PropertyInfo value))
                    return value;
            }
            for (Type? current = type; current != null; current = current.BaseType)
            {
                PropertyInfo? property = current.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (property != null)
                {
                    lock (MemberCacheSync)
                    {
                        if (!PropertyCache.TryGetValue(type, out Dictionary<string, PropertyInfo> cached))
                        {
                            cached = new Dictionary<string, PropertyInfo>(StringComparer.Ordinal);
                            PropertyCache[type] = cached;
                        }
                        cached[name] = property;
                    }
                    return property;
                }
            }
            return null;
        }

        private static InvalidDataException ReflectionFailure(object target, string member, Exception ex)
        {
            Exception cause = ex is TargetInvocationException invocation && invocation.InnerException != null
                ? invocation.InnerException
                : ex;
            return new InvalidDataException(
                "Failed to observe " + target.GetType().FullName + "." + member + ": " + cause.GetType().Name + ": " + cause.Message,
                cause);
        }
    }

    internal sealed class Batch6AutoFishingDeepTransientSnapshot
    {
        internal bool InputOverrideActive { get; set; }
        internal bool VisibleReelInputPending { get; set; }
        internal int ReadyTargetCount { get; set; }
        internal int ReadyReleasedCount { get; set; }
        internal bool CurrentReadyStatePresent { get; set; }
        internal int AnimatorSpeedSnapshotCount { get; set; }
        internal int HookGravitySnapshotCount { get; set; }
        internal int HookVelocitySnapshotCount { get; set; }
        internal int PullDurationSnapshotCount { get; set; }
        internal int TotalCount =>
            (InputOverrideActive ? 1 : 0) +
            (VisibleReelInputPending ? 1 : 0) +
            ReadyTargetCount +
            ReadyReleasedCount +
            (CurrentReadyStatePresent ? 1 : 0) +
            AnimatorSpeedSnapshotCount +
            HookGravitySnapshotCount +
            HookVelocitySnapshotCount +
            PullDurationSnapshotCount;
    }

    /// <summary>
    /// Optional-QA-only inventory of ProductNative holders which must be empty
    /// after disable/title cleanup. It observes product-held overrides/snapshots;
    /// it does not claim to compare every native object value independently.
    /// </summary>
    internal static class Batch6AutoFishingDeepTransientObserver
    {
        private const BindingFlags InstanceMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        internal static Batch6AutoFishingDeepTransientSnapshot CaptureFromPrimitives(object primitives)
        {
            if (primitives == null)
                throw new ArgumentNullException(nameof(primitives));
            object? hookRuntime = ReadRequiredField(primitives, "hookRuntime");
            if (hookRuntime == null)
                return new Batch6AutoFishingDeepTransientSnapshot();

            object inputOverride = ReadRequiredField(hookRuntime, "inputOverride")
                ?? throw new InvalidDataException(hookRuntime.GetType().FullName + ".inputOverride is null.");
            object visibleReelInput = ReadRequiredField(hookRuntime, "visibleReelInput")
                ?? throw new InvalidDataException(hookRuntime.GetType().FullName + ".visibleReelInput is null.");
            return new Batch6AutoFishingDeepTransientSnapshot
            {
                InputOverrideActive = ReadRequiredBooleanProperty(inputOverride, "IsActive"),
                VisibleReelInputPending = ReadRequiredBooleanProperty(visibleReelInput, "IsPending"),
                ReadyTargetCount = ReadRequiredCount(ReadRequiredField(hookRuntime, "readyTargets"), "readyTargets"),
                ReadyReleasedCount = ReadRequiredCount(ReadRequiredField(hookRuntime, "readyReleased"), "readyReleased"),
                CurrentReadyStatePresent = ReadRequiredField(hookRuntime, "currentReadyState") != null,
                AnimatorSpeedSnapshotCount = ReadRequiredCount(ReadRequiredField(hookRuntime, "animatorSpeeds"), "animatorSpeeds"),
                HookGravitySnapshotCount = ReadRequiredCount(ReadRequiredField(hookRuntime, "hookGravityScales"), "hookGravityScales"),
                HookVelocitySnapshotCount = ReadRequiredCount(ReadRequiredField(hookRuntime, "hookVelocities"), "hookVelocities"),
                PullDurationSnapshotCount = ReadRequiredCount(ReadRequiredField(hookRuntime, "pullDurations"), "pullDurations")
            };
        }

        private static object? ReadRequiredField(object target, string name)
        {
            for (Type? current = target.GetType(); current != null; current = current.BaseType)
            {
                FieldInfo? field = current.GetField(name, InstanceMembers | BindingFlags.DeclaredOnly);
                if (field != null)
                    return field.GetValue(target);
            }
            throw new InvalidDataException(target.GetType().FullName + " does not expose required field " + name + ".");
        }

        private static bool ReadRequiredBooleanProperty(object target, string name)
        {
            PropertyInfo? property = target.GetType().GetProperty(name, InstanceMembers);
            if (property == null || property.GetIndexParameters().Length != 0)
                throw new InvalidDataException(target.GetType().FullName + " does not expose required property " + name + ".");
            return Convert.ToBoolean(property.GetValue(target, null), CultureInfo.InvariantCulture);
        }

        private static int ReadRequiredCount(object? value, string name)
        {
            if (value == null)
                throw new InvalidDataException("AutoFishing deep transient holder " + name + " is null.");
            PropertyInfo? count = value.GetType().GetProperty("Count", InstanceMembers);
            if (count == null || count.GetIndexParameters().Length != 0)
                throw new InvalidDataException(value.GetType().FullName + " does not expose required Count for " + name + ".");
            return Convert.ToInt32(count.GetValue(value, null), CultureInfo.InvariantCulture);
        }
    }
}
