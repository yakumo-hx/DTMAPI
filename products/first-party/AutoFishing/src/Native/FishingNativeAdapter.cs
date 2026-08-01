using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using global::DTMAPI.Abstractions;
using static Yuuka.DTMAPI.AutoFishing.ProductNativeHelpers;

namespace Yuuka.DTMAPI.AutoFishing
{
    internal sealed class FishingNativeAdapter
    {
        private readonly FishingNativeStateCache stateCache;
        private readonly FishingMiniGameNativeCache miniGameCache = new FishingMiniGameNativeCache();
        private readonly FishingNativeTransactionCache transactionCache;
        private readonly FishingNativeEnergyGate energyGate = new FishingNativeEnergyGate();
        private Type? dolocApiType;
        private Type? fishingPoolType;
        private Type? unityObjectType;
        private MethodInfo? findObjectsOfType;
        private Action<object, object>? useFishRod;
        private Type? useFishRodAgentType;
        private Type? useFishRodItemType;
        private bool poolCacheValid;
        private bool hasFishingPool;
        private int environmentGeneration;
        private int reflectionCacheMisses;
        private int poolScans;
        private int useFishRodBuilds;
        private int useFishRodRebuilds;
        private int useFishRodBuildFailures;
        private int useFishRodInvocationFailures;

        internal FishingNativeAdapter(FishingNativeStateCache stateCache)
        {
            this.stateCache = stateCache ?? throw new ArgumentNullException(nameof(stateCache));
            transactionCache = new FishingNativeTransactionCache(stateCache.Runtime);
        }

        internal int EnvironmentGeneration => environmentGeneration;
        internal int ReflectionCacheMisses => reflectionCacheMisses;
        internal int PoolScans => poolScans;
        internal int MiniGameAccessorBuilds => miniGameCache.AccessorBuilds;
        internal int MiniGameAccessorRebuilds => miniGameCache.AccessorRebuilds;
        internal int MiniGameAccessorBuildFailures => miniGameCache.AccessorBuildFailures;
        internal int MiniGameAccessorInvocationFailures => miniGameCache.AccessorInvocationFailures;
        internal int MiniGameAccessorFailures => miniGameCache.AccessorFailures;
        internal string MiniGameLastAccessorFailure => miniGameCache.LastAccessorFailure;
        internal int TransactionAccessorBuilds => transactionCache.AccessorBuilds;
        internal int TransactionAccessorRebuilds => transactionCache.AccessorRebuilds;
        internal int TransactionAccessorBuildFailures => transactionCache.AccessorBuildFailures;
        internal int TransactionAccessorInvocationFailures => transactionCache.AccessorInvocationFailures;
        internal int TransactionAccessorFailures => transactionCache.AccessorFailures;
        internal string TransactionLastAccessorFailure => transactionCache.LastAccessorFailure;
        internal int UseFishRodAccessorBuilds => useFishRodBuilds;
        internal int UseFishRodAccessorRebuilds => useFishRodRebuilds;
        internal int UseFishRodAccessorBuildFailures => useFishRodBuildFailures;
        internal int UseFishRodAccessorInvocationFailures => useFishRodInvocationFailures;
        internal int EnergyGateAccessorBuilds => energyGate.AccessorBuilds;
        internal int EnergyGateAccessorRebuilds => energyGate.AccessorRebuilds;
        internal int EnergyGateAccessorBuildFailures => energyGate.AccessorBuildFailures;
        internal int EnergyGateAccessorInvocationFailures => energyGate.AccessorInvocationFailures;

        internal void InvalidateEnvironment(string reason)
        {
            environmentGeneration++;
            poolCacheValid = false;
            hasFishingPool = false;
            stateCache.InvalidateEnvironment(reason);
        }

        internal FishingNativeOperationResult TryCast(string reason)
        {
            FishingNativeAvailability availability = stateCache.Current;
            if (!availability.CanCast)
                return FishingNativeOperationResult.Rejected("not-normal-state", "The cached native agent state is not currently castable.");

            object? agent = stateCache.Agent;
            if (agent == null)
                return FishingNativeOperationResult.Rejected("missing-agent", "DolocAPI.agent is unavailable.");
            if (!HasFishingPool())
                return FishingNativeOperationResult.Rejected("no-water", "No FishingPool exists in the current scene.");

            object? rod = stateCache.SelectedRod;
            if (rod == null)
                return FishingNativeOperationResult.Rejected("no-selected-rod", "The selected item is not an ItemFishingRod.");

            dolocApiType ??= ResolveType("DolocAPI, Assembly-CSharp");
            if (!energyGate.TryCheck(dolocApiType, out bool sufficientEnergy, out int fishingEnergyCost, out string energyFailure))
            {
                return FishingNativeOperationResult.Rejected(
                    "energy-gate-unavailable",
                    "Native fishing energy could not be verified before BodyController.UseFishRod. " + energyFailure);
            }
            if (!sufficientEnergy)
            {
                return FishingNativeOperationResult.Rejected(
                    "insufficient-energy",
                    "DolocAPI.HasEnoughEnergy(GlobalParameter.FishingEnergyCost) rejected the cast. fishingEnergyCost=" + fishingEnergyCost + ".");
            }

            Action<object, object>? method = ResolveUseFishRod(agent.GetType(), rod.GetType());
            if (method == null)
                return FishingNativeOperationResult.Rejected("missing-use-fish-rod", "BodyController.UseFishRod(ItemFishingRod) was not found.");
            try
            {
                method(agent, rod);
                return FishingNativeOperationResult.Success("cast", "Invoked native BodyController.UseFishRod reason=" + (reason ?? string.Empty) + ".");
            }
            catch (Exception ex)
            {
                useFishRodInvocationFailures++;
                Exception actual = ex is TargetInvocationException invocation && invocation.InnerException != null ? invocation.InnerException : ex;
                return FishingNativeOperationResult.Rejected("cast-failed", actual.GetType().Name + ": " + actual.Message);
            }
        }

        internal FishingMiniGameFrameReadStatus TryBuildMiniGameFrame(
            object gameHandle,
            long sequence,
            out FishingMiniGameFrame frame,
            out string failureReason)
        {
            return miniGameCache.TryBuildFrame(gameHandle, sequence, bonusAlreadyTapped: false, out frame, out failureReason);
        }

        internal bool TryReadMiniGameStatus(object gameHandle, out FishingNativeMiniGameStatus status)
        {
            return miniGameCache.TryReadStatus(gameHandle, out status);
        }

        internal bool TryPrepareNativeBite(object waitState, out bool isFish, out string message)
        {
            return transactionCache.TryPrepareNativeBite(waitState, out isFish, out message);
        }

        internal bool TryReel(object waitState, FishingPrimitiveReelMode mode, out string nativeState)
        {
            return transactionCache.TryReel(waitState, mode, out nativeState);
        }

        private bool HasFishingPool()
        {
            if (poolCacheValid)
                return hasFishingPool;
            poolScans++;
            try
            {
                fishingPoolType ??= ResolveType("DolocTown.FishingPool, Assembly-CSharp");
                unityObjectType ??= ResolveType("UnityEngine.Object, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.Object, UnityEngine");
                findObjectsOfType ??= unityObjectType?.GetMethod("FindObjectsOfType", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Type) }, null);
                object? result = fishingPoolType == null ? null : findObjectsOfType?.Invoke(null, new object[] { fishingPoolType });
                hasFishingPool = result is Array array ? array.Length > 0 : Enumerate(result).Any();
            }
            catch
            {
                hasFishingPool = true;
            }
            poolCacheValid = true;
            stateCache.SetPoolAvailability(hasFishingPool);
            return hasFishingPool;
        }

        private Action<object, object>? ResolveUseFishRod(Type agentType, Type rodType)
        {
            if (useFishRod != null && useFishRodAgentType == agentType && useFishRodItemType == rodType)
                return useFishRod;
            reflectionCacheMisses++;
            if (useFishRodAgentType != null && (useFishRodAgentType != agentType || useFishRodItemType != rodType))
                useFishRodRebuilds++;
            useFishRodAgentType = agentType;
            useFishRodItemType = rodType;
            MethodInfo? method = agentType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(candidate => candidate.Name == "UseFishRod" && candidate.GetParameters().Length == 1 && candidate.GetParameters()[0].ParameterType.IsAssignableFrom(rodType));
            useFishRod = FishingNativeAccessors.CreateVoidObjectMethod(method);
            useFishRodBuilds++;
            if (useFishRod == null)
                useFishRodBuildFailures++;
            return useFishRod;
        }

        private static System.Collections.Generic.IEnumerable<object> Enumerate(object? value)
        {
            if (value is IEnumerable values)
            {
                foreach (object? item in values)
                {
                    if (item != null)
                        yield return item;
                }
            }
        }
    }

    internal readonly struct FishingNativeAvailability
    {
        public FishingNativeAvailability(bool canCast, bool hasSelectedRod, bool hasFishingPool, bool horizontalMoveFactorAvailable, double horizontalMoveFactor, string agentState)
        {
            CanCast = canCast;
            HasSelectedRod = hasSelectedRod;
            HasFishingPool = hasFishingPool;
            HorizontalMoveFactorAvailable = horizontalMoveFactorAvailable;
            HorizontalMoveFactor = horizontalMoveFactor;
            AgentState = agentState ?? string.Empty;
        }

        public bool CanCast { get; }
        public bool HasSelectedRod { get; }
        public bool HasFishingPool { get; }
        public bool HorizontalMoveFactorAvailable { get; }
        public double HorizontalMoveFactor { get; }
        public string AgentState { get; }
    }

    internal readonly struct FishingNativeOperationResult
    {
        private FishingNativeOperationResult(bool applied, string status, string message)
        {
            Applied = applied;
            Status = status;
            Message = message;
        }

        public bool Applied { get; }
        public string Status { get; }
        public string Message { get; }
        public static FishingNativeOperationResult Success(string status, string message) => new FishingNativeOperationResult(true, status, message);
        public static FishingNativeOperationResult Rejected(string status, string message) => new FishingNativeOperationResult(false, status, message);
    }
}
