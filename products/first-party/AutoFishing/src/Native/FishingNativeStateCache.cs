using System;

using static Yuuka.DTMAPI.AutoFishing.ProductNativeHelpers;

namespace Yuuka.DTMAPI.AutoFishing
{
    internal sealed class FishingNativeStateCache
    {
        private readonly FishingProductContext runtime;
        private Type? dolocApiType;
        private Type? fishingRodType;
        private Func<object?>? getAgent;
        private Func<object?>? getSelectedItem;
        private Func<bool>? getIsNormalState;
        private Type? agentType;
        private Func<object, bool>? getSupportsUseItem;
        private Func<object, object?>? getStateManager;
        private Func<object, object?>? getStatus;
        private Type? stateManagerType;
        private Func<object, object?>? getCurrentState;
        private Type? statusType;
        private Func<object, object?>? getMoveModifier;
        private Func<object, float>? getVelocityX;
        private Type? moveModifierType;
        private Func<object, float>? getInputMultiplier;
        private Func<object, float>? getOffsetX;
        private Type? waitType;
        private Func<object, object?>? getWaitBody;
        private Func<object, bool>? getWaitForFishBite;
        private Type? waitBodyType;
        private Func<object, object?>? getWaitBodyStateManager;
        private Func<object, object?>? getFishingCache;
        private Type? fishingCacheType;
        private Func<object, object?>? getFishProto;
        private Type? fishProtoType;
        private Func<object, bool>? getFishProtoIsFish;
        private bool accessorsReady;
        private bool staticAccessorsUnavailable;
        private Type? unavailableAgentType;
        private Type? unavailableStateManagerType;
        private Type? unavailableStatusType;
        private Type? unavailableMoveModifierType;
        private Type? unavailableWaitType;
        private Type? unavailableWaitBodyType;
        private Type? unavailableFishingCacheType;
        private Type? unavailableFishProtoType;
        private bool accessorFailureLogged;
        private bool hasFishingPool;
        private bool poolKnown;
        private int environmentGeneration;
        private int accessorBuilds;
        private int accessorRebuilds;
        private int accessorBuildFailures;
        private int accessorInvocationFailures;
        private string lastAccessorFailure = string.Empty;
        private long frameRefreshes;

        internal FishingNativeStateCache(FishingProductContext runtime)
        {
            this.runtime = runtime;
            Current = new FishingNativeAvailability(false, false, false, false, 0d, 0d, 0d, string.Empty);
        }

        internal FishingNativeAvailability Current { get; private set; }
        internal FishingProductContext Runtime => runtime;
        internal object? Agent { get; private set; }
        internal object? SelectedRod { get; private set; }
        internal object? WaitState { get; set; }
        internal int EnvironmentGeneration => environmentGeneration;
        internal int AccessorBuilds => accessorBuilds;
        internal int AccessorRebuilds => accessorRebuilds;
        internal int AccessorBuildFailures => accessorBuildFailures;
        internal int AccessorInvocationFailures => accessorInvocationFailures;
        internal int AccessorFailures => accessorBuildFailures + accessorInvocationFailures;
        internal string LastAccessorFailure => lastAccessorFailure;
        internal long FrameRefreshes => frameRefreshes;

        internal FishingNativeAvailability RefreshFrame()
        {
            frameRefreshes++;
            if (!EnsureStaticAccessors())
                return Current;
            try
            {
                object? agent = getAgent!();
                object? selected = getSelectedItem!();
                Agent = agent;
                SelectedRod = selected != null && IsFishingRod(selected) ? selected : null;
                if (agent == null || !EnsureAgentAccessors(agent))
                {
                    Current = new FishingNativeAvailability(false, SelectedRod != null, poolKnown && hasFishingPool, false, 0d, 0d, 0d, string.Empty);
                    return Current;
                }

                object? manager = getStateManager!(agent);
                object? status = getStatus!(agent);
                object? currentState = manager != null && EnsureStateManagerAccessor(manager) ? getCurrentState!(manager) : null;
                bool movementAvailable = TryReadNativeMovement(status, out double inputMultiplier, out double velocityX, out double offsetX);
                bool nativeFishingBusy = IsNativeFishingState(currentState?.GetType());
                bool canCast = getIsNormalState!() && getSupportsUseItem!(agent) && !nativeFishingBusy;
                Current = new FishingNativeAvailability(
                    canCast,
                    SelectedRod != null,
                    poolKnown && hasFishingPool,
                    movementAvailable,
                    inputMultiplier,
                    velocityX,
                    offsetX,
                    currentState?.GetType().Name ?? string.Empty);
                return Current;
            }
            catch (Exception ex)
            {
                MarkAccessorInvocationFailure("frame-refresh", ex);
                Agent = null;
                SelectedRod = null;
                Current = new FishingNativeAvailability(false, false, poolKnown && hasFishingPool, false, 0d, 0d, 0d, string.Empty);
                return Current;
            }
        }

        internal void SetPoolAvailability(bool value)
        {
            poolKnown = true;
            hasFishingPool = value;
            Current = new FishingNativeAvailability(
                Current.CanCast,
                Current.HasSelectedRod,
                value,
                Current.NativeMovementAvailable,
                Current.NativeInputMultiplier,
                Current.NativeVelocityX,
                Current.NativeOffsetX,
                Current.AgentState);
        }

        internal bool TryReadWaitState(object waitState, out bool isCurrent, out bool waitingForBite, out bool isFish)
        {
            isCurrent = false;
            waitingForBite = false;
            isFish = false;
            if (waitState == null || !EnsureWaitAccessors(waitState.GetType()))
                return false;
            try
            {
                object? body = getWaitBody!(waitState);
                if (body == null || !EnsureWaitBodyAccessors(body.GetType()))
                    return false;
                object? manager = getWaitBodyStateManager!(body);
                object? current = manager != null && EnsureStateManagerAccessor(manager) ? getCurrentState!(manager) : null;
                isCurrent = current == null || ReferenceEquals(current, waitState);
                waitingForBite = getWaitForFishBite!(waitState);
                object? cache = getFishingCache!(body);
                if (cache != null && EnsureFishingCacheAccessors(cache.GetType()))
                {
                    object? fishProto = getFishProto!(cache);
                    if (fishProto != null && EnsureFishProtoAccessors(fishProto.GetType()))
                        isFish = getFishProtoIsFish!(fishProto);
                }
                return true;
            }
            catch (Exception ex)
            {
                MarkAccessorInvocationFailure("wait-state", ex);
                return false;
            }
        }

        internal void InvalidateEnvironment(string reason)
        {
            environmentGeneration++;
            poolKnown = false;
            hasFishingPool = false;
            WaitState = null;
            Agent = null;
            SelectedRod = null;
            Current = new FishingNativeAvailability(false, false, false, false, 0d, 0d, 0d, string.Empty);
        }

        internal void ClearRuntimeReferences()
        {
            WaitState = null;
            Agent = null;
            SelectedRod = null;
            Current = new FishingNativeAvailability(false, false, poolKnown && hasFishingPool, false, 0d, 0d, 0d, string.Empty);
        }

        private bool EnsureStaticAccessors()
        {
            if (accessorsReady)
                return true;
            if (staticAccessorsUnavailable)
                return false;
            try
            {
                dolocApiType ??= ResolveType("DolocAPI, Assembly-CSharp");
                fishingRodType ??= ResolveType("DolocTown.ItemFishingRod, Assembly-CSharp");
                if (dolocApiType == null || fishingRodType == null)
                {
                    accessorBuilds++;
                    staticAccessorsUnavailable = true;
                    MarkAccessorBuildFailure("static-types", null);
                    return false;
                }
                getAgent = FishingNativeAccessors.CreateStaticObjectGetter(FishingNativeAccessors.FindMember(dolocApiType, "agent", isStatic: true));
                getSelectedItem = FishingNativeAccessors.CreateStaticObjectGetter(FishingNativeAccessors.FindMember(dolocApiType, "SelectedItem", isStatic: true));
                getIsNormalState = FishingNativeAccessors.CreateStaticBoolGetter(FishingNativeAccessors.FindMember(dolocApiType, "IsNormalState", isStatic: true));
                accessorsReady = getAgent != null && getSelectedItem != null && getIsNormalState != null;
                accessorBuilds++;
                if (!accessorsReady)
                {
                    staticAccessorsUnavailable = true;
                    MarkAccessorBuildFailure("static-members", null);
                }
                return accessorsReady;
            }
            catch (Exception ex)
            {
                staticAccessorsUnavailable = true;
                MarkAccessorBuildFailure("static-members", ex);
                return false;
            }
        }

        private bool EnsureAgentAccessors(object agent)
        {
            Type type = agent.GetType();
            if (agentType == type && getSupportsUseItem != null && getStateManager != null && getStatus != null)
                return true;
            if (unavailableAgentType == type)
                return false;
            try
            {
                getSupportsUseItem = FishingNativeAccessors.CreateBoolGetter(FishingNativeAccessors.FindMember(type, "IsCurrentStateSupportUseItem"));
                getStateManager = FishingNativeAccessors.CreateObjectGetter(FishingNativeAccessors.FindMember(type, "StateManager"));
                getStatus = FishingNativeAccessors.CreateObjectGetter(FishingNativeAccessors.FindMember(type, "Status"));
                RecordRebuild(agentType, type);
                agentType = type;
                accessorBuilds++;
                bool ready = getSupportsUseItem != null && getStateManager != null && getStatus != null;
                if (!ready)
                {
                    unavailableAgentType = type;
                    MarkAccessorBuildFailure("agent-members", null);
                }
                return ready;
            }
            catch (Exception ex)
            {
                unavailableAgentType = type;
                MarkAccessorBuildFailure("agent-members", ex);
                return false;
            }
        }

        private bool EnsureStateManagerAccessor(object manager)
        {
            Type type = manager.GetType();
            if (stateManagerType == type && getCurrentState != null)
                return true;
            if (unavailableStateManagerType == type)
                return false;
            try
            {
                getCurrentState = FishingNativeAccessors.CreateObjectGetter(FishingNativeAccessors.FindMember(type, "current"));
                RecordRebuild(stateManagerType, type);
                stateManagerType = type;
                accessorBuilds++;
                if (getCurrentState == null)
                {
                    unavailableStateManagerType = type;
                    MarkAccessorBuildFailure("state-manager.current", null);
                }
                return getCurrentState != null;
            }
            catch (Exception ex)
            {
                unavailableStateManagerType = type;
                MarkAccessorBuildFailure("state-manager.current", ex);
                return false;
            }
        }

        private bool TryReadNativeMovement(object? status, out double inputMultiplier, out double velocityX, out double offsetX)
        {
            inputMultiplier = 0d;
            velocityX = 0d;
            offsetX = 0d;
            if (status == null || !EnsureStatusAccessor(status))
                return false;
            object? modifier = getMoveModifier!(status);
            if (modifier == null || !EnsureMoveModifierAccessor(modifier))
                return false;
            inputMultiplier = getInputMultiplier!(modifier);
            velocityX = getVelocityX!(status);
            offsetX = getOffsetX!(modifier);
            return true;
        }

        private bool EnsureStatusAccessor(object status)
        {
            Type type = status.GetType();
            if (statusType == type && getMoveModifier != null && getVelocityX != null)
                return true;
            if (unavailableStatusType == type)
                return false;
            try
            {
                getMoveModifier = FishingNativeAccessors.CreateObjectGetter(FishingNativeAccessors.FindMember(type, "MoveModifier"));
                getVelocityX = FishingNativeAccessors.CreateFloatGetter(FishingNativeAccessors.FindMember(type, "VelocityX"));
                RecordRebuild(statusType, type);
                statusType = type;
                accessorBuilds++;
                if (getMoveModifier == null || getVelocityX == null)
                {
                    unavailableStatusType = type;
                    MarkAccessorBuildFailure("status.MoveModifier+VelocityX", null);
                }
                return getMoveModifier != null && getVelocityX != null;
            }
            catch (Exception ex)
            {
                unavailableStatusType = type;
                MarkAccessorBuildFailure("status.MoveModifier+VelocityX", ex);
                return false;
            }
        }

        private bool EnsureMoveModifierAccessor(object modifier)
        {
            Type type = modifier.GetType();
            if (moveModifierType == type && getInputMultiplier != null && getOffsetX != null)
                return true;
            if (unavailableMoveModifierType == type)
                return false;
            try
            {
                getInputMultiplier = FishingNativeAccessors.CreateFloatGetter(FishingNativeAccessors.FindMember(type, "inputMultiplier"));
                getOffsetX = FishingNativeAccessors.CreateFloatGetter(FishingNativeAccessors.FindMember(type, "OffsetX"));
                RecordRebuild(moveModifierType, type);
                moveModifierType = type;
                accessorBuilds++;
                if (getInputMultiplier == null || getOffsetX == null)
                {
                    unavailableMoveModifierType = type;
                    MarkAccessorBuildFailure("move-modifier.inputMultiplier+OffsetX", null);
                }
                return getInputMultiplier != null && getOffsetX != null;
            }
            catch (Exception ex)
            {
                unavailableMoveModifierType = type;
                MarkAccessorBuildFailure("move-modifier.inputMultiplier+OffsetX", ex);
                return false;
            }
        }

        private bool EnsureWaitAccessors(Type type)
        {
            if (waitType == type && getWaitBody != null && getWaitForFishBite != null)
                return true;
            if (unavailableWaitType == type)
                return false;
            try
            {
                getWaitBody = FishingNativeAccessors.CreateObjectGetter(FishingNativeAccessors.FindMember(type, "body"));
                getWaitForFishBite = FishingNativeAccessors.CreateBoolGetter(FishingNativeAccessors.FindMember(type, "_waitForFishBite"));
                RecordRebuild(waitType, type);
                waitType = type;
                accessorBuilds++;
                bool ready = getWaitBody != null && getWaitForFishBite != null;
                if (!ready)
                {
                    unavailableWaitType = type;
                    MarkAccessorBuildFailure("wait-members", null);
                }
                return ready;
            }
            catch (Exception ex)
            {
                unavailableWaitType = type;
                MarkAccessorBuildFailure("wait-members", ex);
                return false;
            }
        }

        private bool EnsureWaitBodyAccessors(Type type)
        {
            if (waitBodyType == type && getWaitBodyStateManager != null && getFishingCache != null)
                return true;
            if (unavailableWaitBodyType == type)
                return false;
            try
            {
                getWaitBodyStateManager = FishingNativeAccessors.CreateObjectGetter(FishingNativeAccessors.FindMember(type, "StateManager"));
                getFishingCache = FishingNativeAccessors.CreateObjectGetter(FishingNativeAccessors.FindMember(type, "FishingCache"));
                RecordRebuild(waitBodyType, type);
                waitBodyType = type;
                accessorBuilds++;
                bool ready = getWaitBodyStateManager != null && getFishingCache != null;
                if (!ready)
                {
                    unavailableWaitBodyType = type;
                    MarkAccessorBuildFailure("wait-body-members", null);
                }
                return ready;
            }
            catch (Exception ex)
            {
                unavailableWaitBodyType = type;
                MarkAccessorBuildFailure("wait-body-members", ex);
                return false;
            }
        }

        private bool EnsureFishingCacheAccessors(Type type)
        {
            if (fishingCacheType == type && getFishProto != null)
                return true;
            if (unavailableFishingCacheType == type)
                return false;
            try
            {
                getFishProto = FishingNativeAccessors.CreateObjectGetter(FishingNativeAccessors.FindMember(type, "FishProto"));
                RecordRebuild(fishingCacheType, type);
                fishingCacheType = type;
                accessorBuilds++;
                if (getFishProto == null)
                {
                    unavailableFishingCacheType = type;
                    MarkAccessorBuildFailure("fishing-cache.FishProto", null);
                }
                return getFishProto != null;
            }
            catch (Exception ex)
            {
                unavailableFishingCacheType = type;
                MarkAccessorBuildFailure("fishing-cache.FishProto", ex);
                return false;
            }
        }

        private bool EnsureFishProtoAccessors(Type type)
        {
            if (fishProtoType == type && getFishProtoIsFish != null)
                return true;
            if (unavailableFishProtoType == type)
                return false;
            try
            {
                getFishProtoIsFish = FishingNativeAccessors.CreateBoolGetter(FishingNativeAccessors.FindMember(type, "IsFish"));
                RecordRebuild(fishProtoType, type);
                fishProtoType = type;
                accessorBuilds++;
                if (getFishProtoIsFish == null)
                {
                    unavailableFishProtoType = type;
                    MarkAccessorBuildFailure("fish-proto.IsFish", null);
                }
                return getFishProtoIsFish != null;
            }
            catch (Exception ex)
            {
                unavailableFishProtoType = type;
                MarkAccessorBuildFailure("fish-proto.IsFish", ex);
                return false;
            }
        }

        private bool IsFishingRod(object selected)
        {
            return fishingRodType?.IsInstanceOfType(selected) == true;
        }

        private static bool IsNativeFishingState(Type? type)
        {
            string? name = type?.FullName;
            return name != null && name.StartsWith("DolocTown.AgentStateFishing", StringComparison.Ordinal);
        }

        private void RecordRebuild(Type? previousType, Type nextType)
        {
            if (previousType != null && previousType != nextType)
                accessorRebuilds++;
        }

        private void MarkAccessorBuildFailure(string member, Exception? ex)
        {
            accessorBuildFailures++;
            MarkAccessorFailure(member, ex, "build");
        }

        private void MarkAccessorInvocationFailure(string member, Exception ex)
        {
            accessorInvocationFailures++;
            MarkAccessorFailure(member, ex, "invoke");
        }

        private void MarkAccessorFailure(string member, Exception? ex, string kind)
        {
            lastAccessorFailure = kind + ":" + member + (ex == null ? string.Empty : ":" + ex.GetType().Name);
            if (accessorFailureLogged)
                return;
            accessorFailureLogged = true;
            runtime.RuntimeMonitor.Log("Fishing native fast accessor failure kind=" + kind + " member=" + member + (ex == null ? "." : " error=" + ex.GetType().Name + ": " + ex.Message), global::DTMAPI.Abstractions.LogLevel.Warn);
        }
    }
}
