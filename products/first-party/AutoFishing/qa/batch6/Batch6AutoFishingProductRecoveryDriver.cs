using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace DTMAPI.GameBridge.DolocTown.QA
{
    /// <summary>
    /// L4-only recovery driver. It deliberately has no compile-time reference to
    /// Yuuka.DTMAPI.AutoFishing: the already Core-resident product owns the native
    /// primitives and physical Harmony patches, while this QA owner briefly owns
    /// only one reflected primitive session after the real ModEntry F6 path is off.
    /// </summary>
    internal sealed class Batch6AutoFishingProductRecoveryDriver
    {
        internal const string DriverKind = "QaProductNativeRecovery";
        private const BindingFlags InstanceMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private readonly GameBridgeFixtureAccess access;
        private readonly string ownerId;
        private readonly int saveLoadOrdinal;
        private object? primitives;
        private object? session;
        private long lastActionSequence = -1L;
        private string lastOperation = string.Empty;
        private string lastStatus = string.Empty;
        private string lastObservationIdentity = string.Empty;
        private DateTimeOffset nextObservationLogAtUtc = DateTimeOffset.MinValue;
        private DateTimeOffset entryReadySinceUtc = DateTimeOffset.MinValue;
        private bool castAppliedByDriver;
        private long driverCastStartPullExited = -1L;
        private bool cleanupComplete;

        internal Batch6AutoFishingProductRecoveryDriver(GameBridgeFixtureAccess access, int saveLoadOrdinal)
        {
            this.access = access ?? throw new ArgumentNullException(nameof(access));
            this.saveLoadOrdinal = saveLoadOrdinal;
            ownerId = "DTMAPI.QA.Batch6.AutoFishingPilot.L4." + access.RunId;
        }

        internal string OwnerId => ownerId;
        internal bool CleanupComplete => cleanupComplete;
        internal string LastObservationDetails { get; private set; } = "not-observed";

        internal bool IsEntryReady(out string details)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            bool normalGameState = Batch6AutoFishingNativeFishingContextObserver.IsNormalGameState();
            Batch6AutoFishingNativeFishingContextReceipt context = Batch6AutoFishingNativeFishingContextObserver.Capture(
                saveLoadOrdinal,
                "l4-recovery-entry",
                now);
            bool gameplay = access.InputContext.Equals("Gameplay", StringComparison.OrdinalIgnoreCase);
            bool rawReady = gameplay && normalGameState && context.Verified;
            if (!rawReady)
                entryReadySinceUtc = DateTimeOffset.MinValue;
            else if (entryReadySinceUtc == DateTimeOffset.MinValue)
                entryReadySinceUtc = now;
            double stableSeconds = entryReadySinceUtc == DateTimeOffset.MinValue ? 0d : Math.Max(0d, (now - entryReadySinceUtc).TotalSeconds);
            details = "inputContext=" + access.InputContext +
                "; normalGameState=" + normalGameState.ToString().ToLowerInvariant() +
                "; stableSeconds=" + stableSeconds.ToString("0.###", CultureInfo.InvariantCulture) +
                "; currentRoom=" + context.CurrentRoomType +
                "; selectedRod=" + context.SelectedRodObserved.ToString().ToLowerInvariant() +
                "; selectedRodIdentity=" + context.SelectedRodIdentity +
                "; fishingPool=" + context.FishingPoolObserved.ToString().ToLowerInvariant() +
                "; fishingPoolCount=" + context.FishingPoolCount.ToString(CultureInfo.InvariantCulture) +
                "; error=" + context.Error;
            RecordEntryObservation(details, now);
            return rawReady && stableSeconds >= 1d;
        }

        internal Batch6AutoFishingProductRecoverySnapshot Start()
        {
            if (session != null)
                throw new InvalidOperationException("The L4 reflected product-native recovery driver may start only once.");
            object entry = FindCoreResidentEntry();
            RequireEntryInactive(entry);
            primitives = ReadRequiredField(entry, "primitives");
            if (ReadInt(primitives, "ActiveSessionCount") != 0 || InvokeInt(primitives, "CountOwnerResources", ownerId) != 0)
                throw new InvalidOperationException("AutoFishing product primitives were not session-clean before the L4 QA recovery owner entered.");

            session = InvokeRequired(primitives, "StartSession", ownerId, null!);
            Batch6AutoFishingProductRecoverySnapshot snapshot = Capture(refreshNative: true);
            if (!snapshot.Active || snapshot.OwnerResourceCount != 1 || !snapshot.FishingHooksReady || snapshot.NativeAccessorFailureCount != 0)
                throw new InvalidOperationException("The reflected L4 product-native recovery session did not activate exactly one clean QA owner.");
            RecordObservation(snapshot, force: true);
            access.Log(
                "Batch6 AutoFishing L4 recovery started driverKind=" + DriverKind +
                "; driverOwner=" + ownerId +
                "; productUpdater=false; productSession=false; ownerResources=1.");
            return snapshot;
        }

        internal Batch6AutoFishingProductRecoverySnapshot Advance()
        {
            Batch6AutoFishingProductRecoverySnapshot snapshot = Capture(refreshNative: true);
            RecordObservation(snapshot, force: false);
            if (!snapshot.Active)
                throw new InvalidOperationException("The L4 product-native recovery session disappeared before one PullExited receipt.");
            if (snapshot.NativeAccessorFailureCount != 0)
                throw new InvalidDataException("The L4 product-native recovery driver observed native accessor failures=" + snapshot.NativeAccessorFailureCount + ".");
            if (snapshot.DriverCastApplied && snapshot.DriverRecoveryUnits >= 1)
                return snapshot;
            if (snapshot.Sequence == lastActionSequence)
                return snapshot;

            object primitiveSnapshot = InvokeRequired(session!, "GetSnapshot");
            string phase = ReadText(primitiveSnapshot, "Phase");
            long sequence = ReadLong(primitiveSnapshot, "Sequence");
            object? result = null;
            string operation = string.Empty;
            if ((phase == "Idle" || phase == "PullExited" || phase == "Interrupted") && snapshot.NativeCanCast && snapshot.HasSelectedRod)
            {
                Type requestType = RequireMethod(session!.GetType(), "TryCast", 3).GetParameters()[1].ParameterType;
                object request = Activator.CreateInstance(requestType, new object[] { 0d })
                    ?? throw new InvalidOperationException("Could not create the reflected FishingPrimitiveCastRequest.");
                operation = "TryCast";
                result = InvokeRequired(session!, operation, sequence, request, "batch6-l4-qa-recovery-cast");
            }
            else if (phase == "WaitPlayable")
            {
                operation = "TryPrepareNativeBite";
                result = InvokeRequired(session!, operation, sequence, "batch6-l4-qa-recovery-bite");
            }
            else if (phase == "BiteReady")
            {
                MethodInfo reel = RequireMethod(session!.GetType(), "TryReel", 3);
                Type modeType = reel.GetParameters()[1].ParameterType;
                object skipMode = Enum.Parse(modeType, "SkipMiniGameNativeResult", ignoreCase: false);
                operation = "TryReel";
                result = reel.Invoke(session, new[] { (object)sequence, skipMode, "batch6-l4-qa-recovery-reel" })
                    ?? throw new InvalidOperationException("Reflected TryReel returned null.");
            }

            if (result != null)
            {
                lastOperation = operation;
                lastStatus = ReadText(result, "Status");
                if (ReadBool(result, "Applied"))
                {
                    lastActionSequence = sequence;
                    if (operation == "TryCast")
                    {
                        castAppliedByDriver = true;
                        driverCastStartPullExited = snapshot.PullExitedCount;
                    }
                }
            }
            snapshot = Capture(refreshNative: false);
            RecordObservation(snapshot, force: false);
            return snapshot;
        }

        internal Batch6AutoFishingProductRecoverySnapshot Capture(bool refreshNative)
        {
            if (primitives == null || session == null)
                throw new InvalidOperationException("The L4 product-native recovery driver has not started.");
            if (refreshNative)
                InvokeRequiredAllowingVoid(primitives, "RefreshNativeState");
            object diagnostics = InvokeRequired(primitives, "EnableQaObservation");
            object primitiveSnapshot = InvokeRequired(session, "GetSnapshot");
            object nativeStateCache = ReadRequiredProperty(primitives, "NativeStateCache");
            object nativeAvailability = ReadRequiredProperty(nativeStateCache, "Current");
            long pullExitedCount = ReadLong(diagnostics, "PullExitedCount");
            Batch6AutoFishingDeepTransientSnapshot deep = Batch6AutoFishingDeepTransientObserver.CaptureFromPrimitives(primitives);
            return new Batch6AutoFishingProductRecoverySnapshot
            {
                ObservedAtUtc = DateTimeOffset.UtcNow,
                DriverKind = DriverKind,
                DriverOwner = ownerId,
                Active = !ReadBool(session, "IsReleased"),
                OwnerResourceCount = InvokeInt(primitives, "CountOwnerResources", ownerId),
                ActiveSessionCount = ReadInt(primitives, "ActiveSessionCount"),
                FishingHooksReady = ReadBool(primitives, "FishingHooksReady"),
                Phase = ReadText(primitiveSnapshot, "Phase"),
                Sequence = ReadLong(primitiveSnapshot, "Sequence"),
                CanCast = ReadBool(primitiveSnapshot, "CanCast"),
                InputContext = access.InputContext,
                NativeCanCast = ReadBool(nativeAvailability, "CanCast"),
                HasSelectedRod = ReadBool(nativeAvailability, "HasSelectedRod"),
                HasFishingPool = ReadBool(nativeAvailability, "HasFishingPool"),
                AgentState = ReadText(nativeAvailability, "AgentState"),
                DriverCastApplied = castAppliedByDriver,
                DriverRecoveryUnits = driverCastStartPullExited < 0L ? 0L : Math.Max(0L, pullExitedCount - driverCastStartPullExited),
                PullEnteredCount = ReadLong(diagnostics, "PullEnteredCount"),
                PullExitedCount = pullExitedCount,
                CastAppliedCount = ReadLong(diagnostics, "CastAppliedCount"),
                NativeBitePreparedCount = ReadLong(diagnostics, "NativeBitePreparedCount"),
                NativeSkipReelCount = ReadLong(diagnostics, "NativeSkipReelCount"),
                RejectedOperationCount = ReadLong(diagnostics, "RejectedOperationCount"),
                NativeAccessorFailureCount = ReadInt(primitives, "NativeAccessorFailureCount"),
                SchedulerPending = ReadBool(primitives, "SchedulerPending"),
                NativeTransientCount = ReadInt(primitives, "ActiveInputLeaseCount") + ReadInt(primitives, "ActiveAnimationLeaseCount") + (ReadBool(primitives, "SchedulerPending") ? 1 : 0) + deep.TotalCount,
                DeepNativeTransientCount = deep.TotalCount,
                LastOperation = lastOperation,
                LastStatus = lastStatus
            };
        }

        private void RecordEntryObservation(string details, DateTimeOffset now)
        {
            string identity = "entry|" + details;
            LastObservationDetails = details;
            if (identity == lastObservationIdentity && now < nextObservationLogAtUtc)
                return;
            lastObservationIdentity = identity;
            nextObservationLogAtUtc = now.AddSeconds(10);
            access.Log("Batch6 AutoFishing L4 recovery entry observation " + details + ".");
        }

        private void RecordObservation(Batch6AutoFishingProductRecoverySnapshot snapshot, bool force)
        {
            string identity = snapshot.InputContext + "|" + snapshot.Phase + "|" +
                snapshot.Sequence.ToString(CultureInfo.InvariantCulture) + "|" + snapshot.CanCast + "|" +
                snapshot.NativeCanCast + "|" + snapshot.HasSelectedRod + "|" + snapshot.HasFishingPool + "|" +
                snapshot.AgentState + "|" + snapshot.LastOperation + "|" + snapshot.LastStatus + "|" +
                snapshot.PullExitedCount.ToString(CultureInfo.InvariantCulture);
            LastObservationDetails = "inputContext=" + snapshot.InputContext +
                "; phase=" + snapshot.Phase +
                "; sequence=" + snapshot.Sequence.ToString(CultureInfo.InvariantCulture) +
                "; canCast=" + snapshot.CanCast.ToString().ToLowerInvariant() +
                "; nativeCanCast=" + snapshot.NativeCanCast.ToString().ToLowerInvariant() +
                "; selectedRod=" + snapshot.HasSelectedRod.ToString().ToLowerInvariant() +
                "; fishingPool=" + snapshot.HasFishingPool.ToString().ToLowerInvariant() +
                "; agentState=" + snapshot.AgentState +
                "; driverCastApplied=" + snapshot.DriverCastApplied.ToString().ToLowerInvariant() +
                "; driverRecoveryUnits=" + snapshot.DriverRecoveryUnits.ToString(CultureInfo.InvariantCulture) +
                "; lastOperation=" + snapshot.LastOperation +
                "; lastStatus=" + snapshot.LastStatus +
                "; castApplied=" + snapshot.CastAppliedCount.ToString(CultureInfo.InvariantCulture) +
                "; pullExited=" + snapshot.PullExitedCount.ToString(CultureInfo.InvariantCulture) +
                "; rejected=" + snapshot.RejectedOperationCount.ToString(CultureInfo.InvariantCulture);
            if (!force && identity == lastObservationIdentity && snapshot.ObservedAtUtc < nextObservationLogAtUtc)
                return;
            lastObservationIdentity = identity;
            nextObservationLogAtUtc = snapshot.ObservedAtUtc.AddSeconds(10);
            access.Log("Batch6 AutoFishing L4 recovery observation " + LastObservationDetails + ".");
        }

        internal Batch6AutoFishingProductRecoveryCleanup Cleanup(string reason)
        {
            if (cleanupComplete)
                return VerifyCleanup("already-clean");
            if (primitives == null)
                return new Batch6AutoFishingProductRecoveryCleanup { Verified = true, DriverOwner = ownerId, Details = "driver never started" };

            Exception? failure = null;
            try
            {
                if (session != null && !ReadBool(session, "IsReleased"))
                    InvokeRequiredAllowingVoid(session, "Release", "batch6-l4-qa-recovery:" + (reason ?? string.Empty));
            }
            catch (Exception ex)
            {
                failure = Unwrap(ex);
                try { InvokeInt(primitives, "RemoveOwner", ownerId, "fallback:" + (reason ?? string.Empty)); }
                catch (Exception fallback) { failure = new AggregateException(failure, Unwrap(fallback)); }
            }

            Batch6AutoFishingProductRecoveryCleanup cleanup = VerifyCleanup(reason ?? string.Empty);
            cleanupComplete = cleanup.Verified;
            if (failure != null || !cleanup.Verified)
                throw new InvalidOperationException("The L4 reflected product-native recovery owner did not release exactly. " + cleanup.Details, failure);
            access.Log("Batch6 AutoFishing L4 recovery cleanup verified " + cleanup.Details + ".");
            return cleanup;
        }

        internal void TryCleanup(string reason)
        {
            if (cleanupComplete)
                return;
            try { Cleanup(reason); }
            catch (Exception ex)
            {
                access.Log("Batch6 AutoFishing L4 recovery cleanup failed: " + ex.GetType().Name + ": " + ex.Message, DTMAPI.Abstractions.LogLevel.Warn);
            }
        }

        private Batch6AutoFishingProductRecoveryCleanup VerifyCleanup(string reason)
        {
            int resources = primitives == null ? 0 : InvokeInt(primitives, "CountOwnerResources", ownerId);
            int sessions = primitives == null ? 0 : ReadInt(primitives, "ActiveSessionCount");
            int inputLeases = primitives == null ? 0 : ReadInt(primitives, "ActiveInputLeaseCount");
            int animationLeases = primitives == null ? 0 : ReadInt(primitives, "ActiveAnimationLeaseCount");
            bool schedulerPending = primitives != null && ReadBool(primitives, "SchedulerPending");
            bool released = session == null || ReadBool(session, "IsReleased");
            Batch6AutoFishingDeepTransientSnapshot deep = primitives == null
                ? new Batch6AutoFishingDeepTransientSnapshot()
                : Batch6AutoFishingDeepTransientObserver.CaptureFromPrimitives(primitives);
            bool verified = released && resources == 0 && sessions == 0 && inputLeases == 0 && animationLeases == 0 && !schedulerPending && deep.TotalCount == 0;
            return new Batch6AutoFishingProductRecoveryCleanup
            {
                Verified = verified,
                DriverOwner = ownerId,
                OwnerResourceCount = resources,
                ActiveSessionCount = sessions,
                ActiveInputLeaseCount = inputLeases,
                ActiveAnimationLeaseCount = animationLeases,
                SchedulerPending = schedulerPending,
                InputOverrideActive = deep.InputOverrideActive,
                VisibleReelInputPending = deep.VisibleReelInputPending,
                ReadyTargetCount = deep.ReadyTargetCount,
                ReadyReleasedCount = deep.ReadyReleasedCount,
                CurrentReadyStatePresent = deep.CurrentReadyStatePresent,
                AnimatorSpeedSnapshotCount = deep.AnimatorSpeedSnapshotCount,
                HookGravitySnapshotCount = deep.HookGravitySnapshotCount,
                HookVelocitySnapshotCount = deep.HookVelocitySnapshotCount,
                PullDurationSnapshotCount = deep.PullDurationSnapshotCount,
                DeepNativeTransientCount = deep.TotalCount,
                Details = "driverKind=" + DriverKind +
                    "; driverOwner=" + ownerId +
                    "; released=" + released.ToString().ToLowerInvariant() +
                    "; ownerResources=" + resources.ToString(CultureInfo.InvariantCulture) +
                    "; activeSessions=" + sessions.ToString(CultureInfo.InvariantCulture) +
                    "; inputLeases=" + inputLeases.ToString(CultureInfo.InvariantCulture) +
                    "; animationLeases=" + animationLeases.ToString(CultureInfo.InvariantCulture) +
                    "; schedulerPending=" + schedulerPending.ToString().ToLowerInvariant() +
                    "; inputOverrideActive=" + deep.InputOverrideActive.ToString().ToLowerInvariant() +
                    "; visibleReelInputPending=" + deep.VisibleReelInputPending.ToString().ToLowerInvariant() +
                    "; readyTargets=" + deep.ReadyTargetCount.ToString(CultureInfo.InvariantCulture) +
                    "; readyReleased=" + deep.ReadyReleasedCount.ToString(CultureInfo.InvariantCulture) +
                    "; currentReadyState=" + deep.CurrentReadyStatePresent.ToString().ToLowerInvariant() +
                    "; animatorSpeedSnapshots=" + deep.AnimatorSpeedSnapshotCount.ToString(CultureInfo.InvariantCulture) +
                    "; hookGravitySnapshots=" + deep.HookGravitySnapshotCount.ToString(CultureInfo.InvariantCulture) +
                    "; hookVelocitySnapshots=" + deep.HookVelocitySnapshotCount.ToString(CultureInfo.InvariantCulture) +
                    "; pullDurationSnapshots=" + deep.PullDurationSnapshotCount.ToString(CultureInfo.InvariantCulture) +
                    "; reason=" + (reason ?? string.Empty)
            };
        }

        private object FindCoreResidentEntry()
        {
            FieldInfo field = access.Runtime.GetType().GetField("modInstances", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidDataException("DtmApiRuntime.modInstances is unavailable to the L4 reflection driver.");
            IDictionary instances = field.GetValue(access.Runtime) as IDictionary
                ?? throw new InvalidDataException("DtmApiRuntime.modInstances is not an IDictionary.");
            foreach (DictionaryEntry pair in instances)
            {
                if (!string.Equals(pair.Key?.ToString(), Batch6AutoFishingPilotSettings.ProductUniqueId, StringComparison.Ordinal))
                    continue;
                object entry = pair.Value ?? throw new InvalidDataException("The Core-resident AutoFishing entry is null.");
                if (!string.Equals(entry.GetType().FullName, Batch6AutoFishingPilotSettings.ProductEntryType, StringComparison.Ordinal))
                    throw new InvalidDataException("The Core-resident AutoFishing entry type changed before L4 recovery.");
                return entry;
            }
            throw new InvalidDataException("The Core-resident AutoFishing entry disappeared before L4 recovery.");
        }

        private static void RequireEntryInactive(object entry)
        {
            if (ReadBool(entry, "enabled") || ReadBool(entry, "updateSubscribed") || ReadField(entry, "session") != null)
                throw new InvalidDataException("L4 recovery requires the real ModEntry updater and session to remain disabled.");
        }

        private static object ReadRequiredField(object target, string name) => ReadField(target, name)
            ?? throw new InvalidDataException(target.GetType().FullName + "." + name + " is null or unavailable.");

        private static object? ReadField(object target, string name) =>
            target.GetType().GetField(name, InstanceMembers)?.GetValue(target);

        private static object ReadRequiredProperty(object target, string name) => ReadProperty(target, name)
            ?? throw new InvalidDataException(target.GetType().FullName + "." + name + " is null or unavailable.");

        private static object? ReadProperty(object target, string name) =>
            target.GetType().GetProperty(name, InstanceMembers)?.GetValue(target, null);

        private static bool ReadBool(object target, string name) => Convert.ToBoolean(ReadMember(target, name), CultureInfo.InvariantCulture);
        private static int ReadInt(object target, string name) => Convert.ToInt32(ReadMember(target, name), CultureInfo.InvariantCulture);
        private static long ReadLong(object target, string name) => Convert.ToInt64(ReadMember(target, name), CultureInfo.InvariantCulture);
        private static string ReadText(object target, string name) => ReadMember(target, name)?.ToString() ?? string.Empty;

        private static object? ReadMember(object target, string name)
        {
            PropertyInfo? property = target.GetType().GetProperty(name, InstanceMembers);
            if (property != null)
                return property.GetValue(target, null);
            FieldInfo? field = target.GetType().GetField(name, InstanceMembers);
            if (field != null)
                return field.GetValue(target);
            throw new MissingMemberException(target.GetType().FullName, name);
        }

        private static MethodInfo RequireMethod(Type type, string name, int parameterCount)
        {
            foreach (MethodInfo method in type.GetMethods(InstanceMembers))
            {
                if (method.Name == name && method.GetParameters().Length == parameterCount)
                    return method;
            }
            throw new MissingMethodException(type.FullName, name + "(" + parameterCount.ToString(CultureInfo.InvariantCulture) + ")");
        }

        private static object InvokeRequired(object target, string method, params object[] arguments)
        {
            try
            {
                return RequireMethod(target.GetType(), method, arguments.Length).Invoke(target, arguments)
                    ?? throw new InvalidOperationException("Reflected " + method + " returned null.");
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                throw ex.InnerException;
            }
        }

        private static void InvokeRequiredAllowingVoid(object target, string method, params object[] arguments)
        {
            try { RequireMethod(target.GetType(), method, arguments.Length).Invoke(target, arguments); }
            catch (TargetInvocationException ex) when (ex.InnerException != null) { throw ex.InnerException; }
        }

        private static int InvokeInt(object target, string method, params object[] arguments) =>
            Convert.ToInt32(InvokeRequired(target, method, arguments), CultureInfo.InvariantCulture);

        private static Exception Unwrap(Exception ex) => ex is TargetInvocationException invocation && invocation.InnerException != null
            ? invocation.InnerException
            : ex;
    }

    internal sealed class Batch6AutoFishingProductRecoverySnapshot
    {
        internal DateTimeOffset ObservedAtUtc { get; set; }
        internal string DriverKind { get; set; } = string.Empty;
        internal string DriverOwner { get; set; } = string.Empty;
        internal bool Active { get; set; }
        internal int OwnerResourceCount { get; set; }
        internal int ActiveSessionCount { get; set; }
        internal bool FishingHooksReady { get; set; }
        internal string Phase { get; set; } = string.Empty;
        internal long Sequence { get; set; }
        internal bool CanCast { get; set; }
        internal string InputContext { get; set; } = string.Empty;
        internal bool NativeCanCast { get; set; }
        internal bool HasSelectedRod { get; set; }
        internal bool HasFishingPool { get; set; }
        internal string AgentState { get; set; } = string.Empty;
        internal bool DriverCastApplied { get; set; }
        internal long DriverRecoveryUnits { get; set; }
        internal long PullEnteredCount { get; set; }
        internal long PullExitedCount { get; set; }
        internal long CastAppliedCount { get; set; }
        internal long NativeBitePreparedCount { get; set; }
        internal long NativeSkipReelCount { get; set; }
        internal long RejectedOperationCount { get; set; }
        internal int NativeAccessorFailureCount { get; set; }
        internal int NativeTransientCount { get; set; }
        internal int DeepNativeTransientCount { get; set; }
        internal bool SchedulerPending { get; set; }
        internal string LastOperation { get; set; } = string.Empty;
        internal string LastStatus { get; set; } = string.Empty;
    }

    internal sealed class Batch6AutoFishingProductRecoveryCleanup
    {
        internal bool Verified { get; set; }
        internal string DriverOwner { get; set; } = string.Empty;
        internal int OwnerResourceCount { get; set; }
        internal int ActiveSessionCount { get; set; }
        internal int ActiveInputLeaseCount { get; set; }
        internal int ActiveAnimationLeaseCount { get; set; }
        internal bool SchedulerPending { get; set; }
        internal bool InputOverrideActive { get; set; }
        internal bool VisibleReelInputPending { get; set; }
        internal int ReadyTargetCount { get; set; }
        internal int ReadyReleasedCount { get; set; }
        internal bool CurrentReadyStatePresent { get; set; }
        internal int AnimatorSpeedSnapshotCount { get; set; }
        internal int HookGravitySnapshotCount { get; set; }
        internal int HookVelocitySnapshotCount { get; set; }
        internal int PullDurationSnapshotCount { get; set; }
        internal int DeepNativeTransientCount { get; set; }
        internal string Details { get; set; } = string.Empty;
    }
}
