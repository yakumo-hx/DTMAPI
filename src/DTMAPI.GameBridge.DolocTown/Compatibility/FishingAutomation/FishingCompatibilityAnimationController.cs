using System;
using System.Collections.Generic;
using System.Reflection;
using static DTMAPI.GameBridge.DolocTown.GameBridgeNativeHelpers;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class FishingCompatibilityAnimationController
    {
        private readonly Dictionary<object, double> originalAnimatorSpeeds = new Dictionary<object, double>();
        private readonly Dictionary<object, double> originalHookGravityScales = new Dictionary<object, double>();
        private Type? readyType;
        private Func<object, object?>? getReadyTimer;
        private Func<object, object?>? getReadyPowerBar;
        private Func<object, object?>? getReadyBody;
        private Type? readyBodyType;
        private Func<object, object?>? getReadyRenderer;
        private Type? readyRendererType;
        private Action<object, object, float>? refreshPowerBarColor;
        private Type? timerType;
        private Func<object, float>? getTimerProgress;
        private Action<object, float>? tickTimer;
        private Type? powerBarType;
        private Action<object, float>? setPowerBarProgress;
        private Func<float>? getFixedDeltaTime;
        private bool fixedDeltaTimeUnavailable;
        private Type? unavailableReadyType;
        private Type? unavailableTimerType;
        private Type? unavailableReadyBodyType;
        private Type? unavailablePowerBarType;
        private Type? unavailableReadyRendererType;
        private Type? unavailablePowerBarColorType;

        internal Dictionary<object, double> AnimatorSnapshots => originalAnimatorSpeeds;
        internal Dictionary<object, double> HookGravitySnapshots => originalHookGravityScales;
        internal int AnimatorSnapshotCount => originalAnimatorSpeeds.Count;
        internal int HookGravitySnapshotCount => originalHookGravityScales.Count;
        internal bool HasSnapshots => originalAnimatorSpeeds.Count != 0 || originalHookGravityScales.Count != 0;
        internal int ReadyAccessorBuilds { get; private set; }
        internal int ReadyAccessorRebuilds { get; private set; }
        internal int ReadyAccessorBuildFailures { get; private set; }
        internal int ReadyAccessorInvocationFailures { get; private set; }
        internal int ReadyAccessorFailures => ReadyAccessorBuildFailures + ReadyAccessorInvocationFailures;
        internal string LastReadyAccessorFailure { get; private set; } = string.Empty;

        internal bool TryReadReadyProgress(object readyState, out double progress)
        {
            progress = 0d;
            if (!TryGetReadyHandles(readyState, out object? timer, out _) || timer == null || !EnsureTimerAccessors(timer.GetType()))
                return false;
            try
            {
                float value = getTimerProgress!(timer);
                if (float.IsNaN(value) || float.IsInfinity(value))
                    return false;
                progress = Math.Min(1d, Math.Max(0d, value));
                return true;
            }
            catch (Exception ex)
            {
                MarkInvocationFailure("ready-progress", ex);
                return false;
            }
        }

        internal bool TryAdvanceReadyCharge(object readyState, double multiplier, out double before, out double after, out double extraDelta, out int powerBarChanges)
        {
            before = double.NaN;
            after = double.NaN;
            extraDelta = 0d;
            powerBarChanges = 0;
            if (!TryGetReadyHandles(readyState, out object? timer, out object? powerBar) || timer == null || !EnsureTimerAccessors(timer.GetType()))
                return false;
            try
            {
                float fixedDelta = GetFixedDeltaTime();
                float extra = fixedDelta * (float)(multiplier - 1d);
                if (extra <= 0f)
                    return false;
                before = getTimerProgress!(timer);
                tickTimer!(timer, extra);
                after = getTimerProgress!(timer);
                extraDelta = extra;
                if (powerBar != null && EnsurePowerBarAccessors(powerBar.GetType()))
                {
                    setPowerBarProgress!(powerBar, (float)after);
                    powerBarChanges = 1;
                    object? renderer = TryGetReadyRenderer(readyState);
                    if (renderer != null && EnsurePowerBarColorAccessor(renderer.GetType(), powerBar.GetType()))
                    {
                        refreshPowerBarColor!(renderer, powerBar, (float)after);
                        powerBarChanges++;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                MarkInvocationFailure("ready-charge", ex);
                return false;
            }
        }

        private bool TryGetReadyHandles(object readyState, out object? timer, out object? powerBar)
        {
            timer = null;
            powerBar = null;
            Type type = readyState.GetType();
            if (unavailableReadyType == type)
                return false;
            if (readyType != type || getReadyTimer == null || getReadyPowerBar == null)
            {
                try
                {
                    getReadyTimer = GameBridgeNativeAccessors.CreateObjectGetter(GameBridgeNativeAccessors.FindMember(type, "_castTimer"));
                    getReadyPowerBar = GameBridgeNativeAccessors.CreateObjectGetter(GameBridgeNativeAccessors.FindMember(type, "_powerBar"));
                    getReadyBody = GameBridgeNativeAccessors.CreateObjectGetter(GameBridgeNativeAccessors.FindMember(type, "body"));
                    RecordRebuild(readyType, type);
                    readyType = type;
                    ReadyAccessorBuilds++;
                    if (getReadyTimer == null || getReadyPowerBar == null)
                    {
                        unavailableReadyType = type;
                        MarkBuildFailure("ready-members", null);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    unavailableReadyType = type;
                    MarkBuildFailure("ready-members", ex);
                    return false;
                }
            }
            timer = getReadyTimer(readyState);
            powerBar = getReadyPowerBar(readyState);
            return timer != null;
        }

        private object? TryGetReadyRenderer(object readyState)
        {
            object? body = getReadyBody?.Invoke(readyState);
            if (body == null)
                return null;
            if (unavailableReadyBodyType == body.GetType())
                return null;
            if (readyBodyType != body.GetType() || getReadyRenderer == null)
            {
                RecordRebuild(readyBodyType, body.GetType());
                readyBodyType = body.GetType();
                getReadyRenderer = GameBridgeNativeAccessors.CreateObjectGetter(GameBridgeNativeAccessors.FindMember(readyBodyType, "fishRodRenderer"));
                ReadyAccessorBuilds++;
                if (getReadyRenderer == null)
                {
                    unavailableReadyBodyType = readyBodyType;
                    MarkBuildFailure("ready-renderer", null);
                }
            }
            return getReadyRenderer?.Invoke(body);
        }

        private bool EnsureTimerAccessors(Type type)
        {
            if (timerType == type && getTimerProgress != null && tickTimer != null)
                return true;
            if (unavailableTimerType == type)
                return false;
            try
            {
                getTimerProgress = GameBridgeNativeAccessors.CreateFloatGetter(GameBridgeNativeAccessors.FindMember(type, "Progress"));
                tickTimer = GameBridgeNativeAccessors.CreateVoidFloatMethod(GameBridgeNativeAccessors.FindMethod(type, "Tick", 1));
                RecordRebuild(timerType, type);
                timerType = type;
                ReadyAccessorBuilds++;
                if (getTimerProgress == null || tickTimer == null)
                {
                    unavailableTimerType = type;
                    MarkBuildFailure("ready-timer", null);
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                unavailableTimerType = type;
                MarkBuildFailure("ready-timer", ex);
                return false;
            }
        }

        private bool EnsurePowerBarAccessors(Type type)
        {
            if (powerBarType == type && setPowerBarProgress != null)
                return true;
            if (unavailablePowerBarType == type)
                return false;
            try
            {
                setPowerBarProgress = GameBridgeNativeAccessors.CreateFloatSetter(GameBridgeNativeAccessors.FindMember(type, "Progress"));
                RecordRebuild(powerBarType, type);
                powerBarType = type;
                ReadyAccessorBuilds++;
                if (setPowerBarProgress == null)
                {
                    unavailablePowerBarType = type;
                    MarkBuildFailure("ready-power-bar", null);
                }
                return setPowerBarProgress != null;
            }
            catch (Exception ex)
            {
                unavailablePowerBarType = type;
                MarkBuildFailure("ready-power-bar", ex);
                return false;
            }
        }

        private bool EnsurePowerBarColorAccessor(Type rendererType, Type targetPowerBarType)
        {
            if (readyRendererType == rendererType && powerBarType == targetPowerBarType && refreshPowerBarColor != null)
                return true;
            if (unavailableReadyRendererType == rendererType && unavailablePowerBarColorType == targetPowerBarType)
                return false;
            try
            {
                MethodInfo? getColor = rendererType.GetMethod("GetCastForceColor", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null, new[] { typeof(float) }, null);
                refreshPowerBarColor = GameBridgeNativeAccessors.CreateObjectFloatResultSetter(getColor, GameBridgeNativeAccessors.FindMember(targetPowerBarType, "Color"));
                RecordRebuild(readyRendererType, rendererType);
                readyRendererType = rendererType;
                ReadyAccessorBuilds++;
                if (refreshPowerBarColor == null)
                {
                    unavailableReadyRendererType = rendererType;
                    unavailablePowerBarColorType = targetPowerBarType;
                    MarkBuildFailure("ready-power-bar-color", null);
                }
                return refreshPowerBarColor != null;
            }
            catch (Exception ex)
            {
                unavailableReadyRendererType = rendererType;
                unavailablePowerBarColorType = targetPowerBarType;
                MarkBuildFailure("ready-power-bar-color", ex);
                return false;
            }
        }

        private float GetFixedDeltaTime()
        {
            if (getFixedDeltaTime != null)
                return Math.Max(0.0001f, getFixedDeltaTime());
            if (fixedDeltaTimeUnavailable)
                return 0.02f;
            try
            {
                Type? timeType = ResolveType("UnityEngine.Time, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.Time, UnityEngine");
                getFixedDeltaTime = GameBridgeNativeAccessors.CreateStaticFloatGetter(GameBridgeNativeAccessors.FindMember(timeType, "fixedDeltaTime", isStatic: true));
                ReadyAccessorBuilds++;
                if (getFixedDeltaTime == null)
                {
                    fixedDeltaTimeUnavailable = true;
                    MarkBuildFailure("UnityEngine.Time.fixedDeltaTime", null);
                    return 0.02f;
                }
                return Math.Max(0.0001f, getFixedDeltaTime());
            }
            catch (Exception ex)
            {
                fixedDeltaTimeUnavailable = true;
                MarkBuildFailure("UnityEngine.Time.fixedDeltaTime", ex);
                return 0.02f;
            }
        }

        internal void Clear()
        {
            originalAnimatorSpeeds.Clear();
            originalHookGravityScales.Clear();
        }

        private void RecordRebuild(Type? previousType, Type nextType)
        {
            if (previousType != null && previousType != nextType)
                ReadyAccessorRebuilds++;
        }

        private void MarkBuildFailure(string member, Exception? ex)
        {
            ReadyAccessorBuildFailures++;
            LastReadyAccessorFailure = "build:" + member + (ex == null ? string.Empty : ":" + ex.GetType().Name);
        }

        private void MarkInvocationFailure(string member, Exception ex)
        {
            ReadyAccessorInvocationFailures++;
            LastReadyAccessorFailure = "invoke:" + member + ":" + ex.GetType().Name;
        }
    }
}
