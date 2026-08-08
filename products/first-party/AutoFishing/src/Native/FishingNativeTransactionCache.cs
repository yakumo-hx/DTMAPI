using System;
using System.Linq;
using System.Reflection;
using global::DTMAPI.Abstractions;

using static Yuuka.DTMAPI.AutoFishing.ProductNativeHelpers;

namespace Yuuka.DTMAPI.AutoFishing
{
    /// <summary>Compiled native fishing write transactions used by first-party sessions.</summary>
    internal sealed class FishingNativeTransactionCache
    {
        private readonly FishingProductContext runtime;
        private Type? waitType;
        private Func<object, object?>? getBody;
        private Func<object, bool>? getWaitForBite;
        private Action<object, bool>? setWaitForBite;
        private Action<object, bool>? setHasRolled;
        private Action<object, float>? setHookProbability;
        private Action<object, float>? setFishOnHookDuration;
        private Func<object, bool>? rollFish;
        private Action<object>? invokeFishOnHookTip;
        private Func<object, object?>? nextState;
        private Type? bodyType;
        private Func<object, object?>? getStateManager;
        private Func<object, object?>? getFishingCache;
        private Func<object, object?>? getFishRodRenderer;
        private Type? fishRodRendererType;
        private Func<object, object?>? getFishingLine;
        private Action<object>? enableFishShadow;
        private Type? fishingLineType;
        private Action<object>? useStraightLine;
        private Type? stateManagerType;
        private Func<object, object?>? getCurrentState;
        private Action<object, object>? overwriteState;
        private Type? fishingCacheType;
        private Func<object, object?>? getFishProto;
        private Type? fishProtoType;
        private Func<object, bool>? getIsFish;
        private Type? dolocApiType;
        private Func<object?>? getGameManager;
        private Type? gameManagerType;
        private Func<object, object?>? getGameInitConfig;
        private Type? gameInitConfigType;
        private Func<object, bool>? getSkipFishingGame;
        private Action<object, bool>? setSkipFishingGame;
        private Func<object, bool>? getSkipFishingWait;
        private Func<object?>? getGlobalParameter;
        private Type? globalParameterType;
        private Func<object, float>? getPullTiming;
        private bool failureLogged;
        private Type? unavailableWaitType;
        private Type? unavailableBodyType;
        private Type? unavailableStateManagerType;
        private Type? unavailableFishingCacheType;
        private Type? unavailableFishProtoType;

        internal FishingNativeTransactionCache(FishingProductContext runtime)
        {
            this.runtime = runtime;
        }

        internal int AccessorBuilds { get; private set; }
        internal int AccessorRebuilds { get; private set; }
        internal int AccessorBuildFailures { get; private set; }
        internal int AccessorInvocationFailures { get; private set; }
        internal int AccessorFailures => AccessorBuildFailures + AccessorInvocationFailures;
        internal string LastAccessorFailure { get; private set; } = string.Empty;

        internal bool TryPrepareNativeBite(
            object waitState,
            out bool isFish,
            out bool requiresFaultClose,
            out FishingBitePreparationProvenance provenance,
            out string message)
        {
            isFish = false;
            requiresFaultClose = false;
            provenance = FishingBitePreparationProvenance.None;
            message = string.Empty;
            if (waitState == null || !EnsureWaitAccessors(waitState.GetType()))
            {
                message = "Native Wait fast accessors are unavailable.";
                return false;
            }
            bool nativeMutationAttempted = false;
            try
            {
                object? body = getBody!(waitState);
                if (body == null || !EnsureBodyAccessors(body.GetType()))
                {
                    message = "The native fishing body is unavailable.";
                    return false;
                }
                object? manager = getStateManager!(body);
                if (manager == null || !EnsureStateManagerAccessors(manager.GetType()))
                {
                    message = "The native fishing state manager is unavailable.";
                    return false;
                }
                object? current = getCurrentState!(manager);
                if (current != null && !ReferenceEquals(current, waitState))
                {
                    message = "The native fishing Wait state is no longer current.";
                    return false;
                }

                if (getWaitForBite!(waitState))
                {
                    bool rolled = false;
                    for (int attempt = 0; attempt < 100 && !rolled; attempt++)
                    {
                        nativeMutationAttempted = true;
                        rolled = rollFish!(waitState);
                    }
                    if (!rolled)
                    {
                        message = "AgentStateFishingWait.RollFish did not produce a native bite.";
                        return false;
                    }

                    FishingBitePreparationResult prepared = FishingBitePreparationTransaction.Commit(
                        ResolveFishOnHookDuration(),
                        () => getWaitForBite!(waitState),
                        value => setHookProbability!(waitState, value),
                        value => setFishOnHookDuration!(waitState, value),
                        value => setHasRolled!(waitState, value),
                        value => setWaitForBite!(waitState, value),
                        () => invokeFishOnHookTip?.Invoke(waitState),
                        () => RefreshFishingRenderer(body),
                        () => ReadIsFish(body));
                    RecordPreparationFailures(prepared);
                    if (!prepared.Committed)
                    {
                        requiresFaultClose = prepared.RequiresFaultClose;
                        message = "Native fish roll changed FishingCache, but the Wait commit was not proven at stage " +
                            prepared.Stage + "; AutoFishing must close and leave native Wait in control.";
                        return false;
                    }

                    isFish = prepared.IsFish;
                    provenance = prepared.Provenance;
                    message = "Native bite committed through ordered cached accessors stage=" + prepared.Stage +
                        "; provenance=" + provenance +
                        "; fish-observation=" + (prepared.FishObservationAvailable ? "available" : "unavailable") +
                        "; post-commit-failures=" + prepared.PostCommitFailureCount + ".";
                    return true;
                }

                try
                {
                    isFish = ReadIsFish(body);
                    message = "Native bite was already committed before the cached transaction ran.";
                }
                catch (Exception observationError)
                {
                    MarkInvocationFailure("prepare-bite-fish-observation", observationError);
                    message = "Native bite was already committed; fish observation is unavailable and native NextState remains authoritative.";
                }
                provenance = FishingBitePreparationProvenance.AlreadyNativeCommitted;
                return true;
            }
            catch (Exception ex)
            {
                MarkInvocationFailure("prepare-bite", ex);
                requiresFaultClose = nativeMutationAttempted;
                message = requiresFaultClose
                    ? "Native bite mutation became uncertain and AutoFishing must close: " + ex.GetType().Name + "."
                    : "Native bite transaction failed before native mutation: " + ex.GetType().Name + ".";
                return false;
            }
        }

        internal bool TryReel(object waitState, FishingPrimitiveReelMode mode, out string nativeState)
        {
            nativeState = string.Empty;
            if (waitState == null || !EnsureWaitAccessors(waitState.GetType()))
                return false;
            bool wroteSkip = false;
            bool originalSkip = false;
            object? skipConfig = null;
            try
            {
                if (getWaitForBite!(waitState))
                    return false;
                object? body = getBody!(waitState);
                if (body == null || !EnsureBodyAccessors(body.GetType()))
                    return false;
                object? manager = getStateManager!(body);
                if (manager == null || !EnsureStateManagerAccessors(manager.GetType()))
                    return false;
                object? current = getCurrentState!(manager);
                if (current != null && !ReferenceEquals(current, waitState))
                    return false;

                if (mode == FishingPrimitiveReelMode.SkipMiniGameNativeResult)
                {
                    if (!TryGetNativeSkipFlag(out skipConfig, out originalSkip) || skipConfig == null)
                        return false;
                    if (!originalSkip)
                    {
                        setSkipFishingGame!(skipConfig, true);
                        wroteSkip = true;
                    }
                }

                object? target = nextState!(waitState);
                if (target == null || ReferenceEquals(target, waitState))
                    return false;
                overwriteState!(manager, target);
                nativeState = target.GetType().Name;
                return true;
            }
            catch (Exception ex)
            {
                MarkInvocationFailure("reel", ex);
                return false;
            }
            finally
            {
                if (wroteSkip && skipConfig != null)
                {
                    try
                    {
                        setSkipFishingGame!(skipConfig, originalSkip);
                    }
                    catch (Exception ex)
                    {
                        MarkInvocationFailure("restore-skip-fishing-game", ex);
                    }
                }
            }
        }

        private bool EnsureWaitAccessors(Type type)
        {
            if (waitType == type && getBody != null && getWaitForBite != null && setWaitForBite != null &&
                setHasRolled != null && setHookProbability != null && setFishOnHookDuration != null && rollFish != null && nextState != null)
            {
                return true;
            }
            if (unavailableWaitType == type)
                return false;
            try
            {
                getBody = FishingNativeAccessors.CreateObjectGetter(FishingNativeAccessors.FindMember(type, "body"));
                MemberInfo? waitForBite = FishingNativeAccessors.FindMember(type, "_waitForFishBite");
                getWaitForBite = FishingNativeAccessors.CreateBoolGetter(waitForBite);
                setWaitForBite = FishingNativeAccessors.CreateBoolSetter(waitForBite);
                setHasRolled = FishingNativeAccessors.CreateBoolSetter(FishingNativeAccessors.FindMember(type, "_hasRolled"));
                setHookProbability = FishingNativeAccessors.CreateFloatSetter(FishingNativeAccessors.FindMember(type, "_hookProbability"));
                setFishOnHookDuration = FishingNativeAccessors.CreateFloatSetter(FishingNativeAccessors.FindMember(type, "_fishOnHookDuration"));
                rollFish = FishingNativeAccessors.CreateBoolMethod(FishingNativeAccessors.FindMethod(type, "RollFish", 0));
                invokeFishOnHookTip = FishingNativeAccessors.CreateVoidMethod(FishingNativeAccessors.FindMethod(type, "InvokeFishOnHookTip", 0));
                nextState = FishingNativeAccessors.CreateObjectMethod(FishingNativeAccessors.FindMethod(type, "NextState", 0));
                RecordRebuild(waitType, type);
                waitType = type;
                AccessorBuilds++;
                bool ready = getBody != null && getWaitForBite != null && setWaitForBite != null && setHasRolled != null &&
                    setHookProbability != null && setFishOnHookDuration != null && rollFish != null && nextState != null;
                if (!ready)
                {
                    unavailableWaitType = type;
                    MarkBuildFailure("wait-transaction-members", null);
                }
                return ready;
            }
            catch (Exception ex)
            {
                unavailableWaitType = type;
                MarkBuildFailure("wait-transaction-members", ex);
                return false;
            }
        }

        private bool EnsureBodyAccessors(Type type)
        {
            if (bodyType == type && getStateManager != null && getFishingCache != null)
                return true;
            if (unavailableBodyType == type)
                return false;
            try
            {
                getStateManager = FishingNativeAccessors.CreateObjectGetter(FishingNativeAccessors.FindMember(type, "StateManager"));
                getFishingCache = FishingNativeAccessors.CreateObjectGetter(FishingNativeAccessors.FindMember(type, "FishingCache"));
                getFishRodRenderer = FishingNativeAccessors.CreateObjectGetter(FishingNativeAccessors.FindMember(type, "fishRodRenderer"));
                RecordRebuild(bodyType, type);
                bodyType = type;
                AccessorBuilds++;
                bool ready = getStateManager != null && getFishingCache != null;
                if (!ready)
                {
                    unavailableBodyType = type;
                    MarkBuildFailure("body-transaction-members", null);
                }
                return ready;
            }
            catch (Exception ex)
            {
                unavailableBodyType = type;
                MarkBuildFailure("body-transaction-members", ex);
                return false;
            }
        }

        private bool EnsureStateManagerAccessors(Type type)
        {
            if (stateManagerType == type && getCurrentState != null && overwriteState != null)
                return true;
            if (unavailableStateManagerType == type)
                return false;
            try
            {
                getCurrentState = FishingNativeAccessors.CreateObjectGetter(FishingNativeAccessors.FindMember(type, "current"));
                MethodInfo? overwrite = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .FirstOrDefault(method => method.Name == "Overwrite" && !method.IsGenericMethodDefinition && method.GetParameters().Length == 2)
                    ?? type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .FirstOrDefault(method => method.Name == "Overwrite" && !method.IsGenericMethodDefinition && method.GetParameters().Length == 1);
                overwriteState = FishingNativeAccessors.CreateVoidObjectForceMethod(overwrite);
                RecordRebuild(stateManagerType, type);
                stateManagerType = type;
                AccessorBuilds++;
                bool ready = getCurrentState != null && overwriteState != null;
                if (!ready)
                {
                    unavailableStateManagerType = type;
                    MarkBuildFailure("state-manager-transaction-members", null);
                }
                return ready;
            }
            catch (Exception ex)
            {
                unavailableStateManagerType = type;
                MarkBuildFailure("state-manager-transaction-members", ex);
                return false;
            }
        }

        private bool ReadIsFish(object body)
        {
            object? cache = getFishingCache!(body);
            if (cache == null || !EnsureFishingCacheAccessors(cache.GetType()))
                throw new InvalidOperationException("FishingCache.FishProto is unavailable.");
            object? fish = getFishProto!(cache);
            if (fish == null)
                return false;
            if (!EnsureFishProtoAccessors(fish.GetType()))
                throw new InvalidOperationException("FishProto.IsFish is unavailable.");
            return getIsFish!(fish);
        }

        private void RefreshFishingRenderer(object body)
        {
            object? renderer = getFishRodRenderer?.Invoke(body);
            if (renderer == null)
                return;
            if (fishRodRendererType != renderer.GetType() || getFishingLine == null || enableFishShadow == null)
            {
                fishRodRendererType = renderer.GetType();
                getFishingLine = FishingNativeAccessors.CreateObjectGetter(FishingNativeAccessors.FindMember(fishRodRendererType, "Line"));
                enableFishShadow = FishingNativeAccessors.CreateVoidMethod(FishingNativeAccessors.FindMethod(fishRodRendererType, "EnableFishShadow", 0));
                AccessorBuilds++;
            }
            object? line = getFishingLine?.Invoke(renderer);
            if (line != null)
            {
                if (fishingLineType != line.GetType() || useStraightLine == null)
                {
                    fishingLineType = line.GetType();
                    useStraightLine = FishingNativeAccessors.CreateVoidMethod(FishingNativeAccessors.FindMethod(fishingLineType, "UseStraightLine", 0));
                    AccessorBuilds++;
                }
                useStraightLine?.Invoke(line);
            }
            enableFishShadow?.Invoke(renderer);
        }

        private void RecordPreparationFailures(FishingBitePreparationResult result)
        {
            if (result.CommitError != null)
                MarkInvocationFailure("prepare-bite-commit:" + result.Stage, result.CommitError);
            if (result.TipError != null)
                MarkInvocationFailure("prepare-bite-post-commit-tip", result.TipError);
            if (result.RendererError != null)
                MarkInvocationFailure("prepare-bite-post-commit-renderer", result.RendererError);
            if (result.FishObservationError != null)
                MarkInvocationFailure("prepare-bite-post-commit-fish-observation", result.FishObservationError);
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
                AccessorBuilds++;
                if (getFishProto == null)
                {
                    unavailableFishingCacheType = type;
                    MarkBuildFailure("fishing-cache.FishProto", null);
                }
                return getFishProto != null;
            }
            catch (Exception ex)
            {
                unavailableFishingCacheType = type;
                MarkBuildFailure("fishing-cache.FishProto", ex);
                return false;
            }
        }

        private bool EnsureFishProtoAccessors(Type type)
        {
            if (fishProtoType == type && getIsFish != null)
                return true;
            if (unavailableFishProtoType == type)
                return false;
            try
            {
                getIsFish = FishingNativeAccessors.CreateBoolGetter(FishingNativeAccessors.FindMember(type, "IsFish"));
                RecordRebuild(fishProtoType, type);
                fishProtoType = type;
                AccessorBuilds++;
                if (getIsFish == null)
                {
                    unavailableFishProtoType = type;
                    MarkBuildFailure("fish-proto.IsFish", null);
                }
                return getIsFish != null;
            }
            catch (Exception ex)
            {
                unavailableFishProtoType = type;
                MarkBuildFailure("fish-proto.IsFish", ex);
                return false;
            }
        }

        private bool TryGetNativeSkipFlag(out object? config, out bool value)
        {
            config = null;
            value = false;
            try
            {
                dolocApiType ??= ResolveType("DolocAPI, Assembly-CSharp");
                getGameManager ??= FishingNativeAccessors.CreateStaticObjectGetter(FishingNativeAccessors.FindMember(dolocApiType, "gameManager", isStatic: true));
                object? manager = getGameManager?.Invoke();
                if (manager == null)
                    return false;
                if (gameManagerType != manager.GetType() || getGameInitConfig == null)
                {
                    gameManagerType = manager.GetType();
                    getGameInitConfig = FishingNativeAccessors.CreateObjectGetter(FishingNativeAccessors.FindMember(gameManagerType, "gameInitConfig"));
                    AccessorBuilds++;
                }
                config = getGameInitConfig?.Invoke(manager);
                if (config == null)
                    return false;
                if (gameInitConfigType != config.GetType() || getSkipFishingGame == null || setSkipFishingGame == null)
                {
                    gameInitConfigType = config.GetType();
                    MemberInfo? member = FishingNativeAccessors.FindMember(gameInitConfigType, "skipFishingGame");
                    getSkipFishingGame = FishingNativeAccessors.CreateBoolGetter(member);
                    setSkipFishingGame = FishingNativeAccessors.CreateBoolSetter(member);
                    AccessorBuilds++;
                }
                if (getSkipFishingGame == null || setSkipFishingGame == null)
                {
                    MarkBuildFailure("game-init.skipFishingGame", null);
                    return false;
                }
                value = getSkipFishingGame(config);
                return true;
            }
            catch (Exception ex)
            {
                MarkInvocationFailure("game-init.skipFishingGame", ex);
                return false;
            }
        }

        private float ResolveFishOnHookDuration()
        {
            try
            {
                if (TryGetNativeSkipFlag(out object? config, out _) && config != null)
                {
                    getSkipFishingWait ??= FishingNativeAccessors.CreateBoolGetter(FishingNativeAccessors.FindMember(config.GetType(), "skipFishingWait"));
                    if (getSkipFishingWait?.Invoke(config) == true)
                        return 100f;
                }

                dolocApiType ??= ResolveType("DolocAPI, Assembly-CSharp");
                getGlobalParameter ??= FishingNativeAccessors.CreateStaticObjectGetter(FishingNativeAccessors.FindMember(dolocApiType, "GlobalParameter", isStatic: true));
                object? parameter = getGlobalParameter?.Invoke();
                if (parameter == null)
                    return 2f;
                if (globalParameterType != parameter.GetType() || getPullTiming == null)
                {
                    globalParameterType = parameter.GetType();
                    getPullTiming = FishingNativeAccessors.CreateFloatGetter(FishingNativeAccessors.FindMember(globalParameterType, "PullTiming"));
                    AccessorBuilds++;
                }
                float duration = getPullTiming?.Invoke(parameter) ?? 2f;
                return float.IsNaN(duration) || float.IsInfinity(duration) || duration <= 0f
                    ? 2f
                    : Math.Min(30f, Math.Max(0.25f, duration));
            }
            catch (Exception ex)
            {
                MarkInvocationFailure("global-parameter.PullTiming", ex);
                return 2f;
            }
        }

        private void RecordRebuild(Type? previousType, Type nextType)
        {
            if (previousType != null && previousType != nextType)
                AccessorRebuilds++;
        }

        private void MarkBuildFailure(string member, Exception? ex)
        {
            AccessorBuildFailures++;
            MarkFailure(member, ex, "build");
        }

        private void MarkInvocationFailure(string member, Exception ex)
        {
            AccessorInvocationFailures++;
            MarkFailure(member, ex, "invoke");
        }

        private void MarkFailure(string member, Exception? ex, string kind)
        {
            LastAccessorFailure = kind + ":" + member + (ex == null ? string.Empty : ":" + ex.GetType().Name);
            if (failureLogged)
                return;
            failureLogged = true;
            runtime.RuntimeMonitor.Log(
                "Fishing native transaction accessor failure kind=" + kind + " member=" + member + (ex == null ? "." : " error=" + ex.GetType().Name + ": " + ex.Message),
                LogLevel.Warn);
        }
    }
}
