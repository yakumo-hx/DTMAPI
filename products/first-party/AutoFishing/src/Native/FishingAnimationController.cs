using System;
using System.Reflection;
using static Yuuka.DTMAPI.AutoFishing.ProductNativeHelpers;

namespace Yuuka.DTMAPI.AutoFishing
{
    internal sealed class FishingAnimationController
    {
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
            catch
            {
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
            catch
            {
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
                    getReadyTimer = FishingNativeAccessors.CreateObjectGetter(FishingNativeAccessors.FindMember(type, "_castTimer"));
                    getReadyPowerBar = FishingNativeAccessors.CreateObjectGetter(FishingNativeAccessors.FindMember(type, "_powerBar"));
                    getReadyBody = FishingNativeAccessors.CreateObjectGetter(FishingNativeAccessors.FindMember(type, "body"));
                    readyType = type;
                    if (getReadyTimer == null || getReadyPowerBar == null)
                    {
                        unavailableReadyType = type;
                        return false;
                    }
                }
                catch
                {
                    unavailableReadyType = type;
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
                readyBodyType = body.GetType();
                getReadyRenderer = FishingNativeAccessors.CreateObjectGetter(FishingNativeAccessors.FindMember(readyBodyType, "fishRodRenderer"));
                if (getReadyRenderer == null)
                    unavailableReadyBodyType = readyBodyType;
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
                getTimerProgress = FishingNativeAccessors.CreateFloatGetter(FishingNativeAccessors.FindMember(type, "Progress"));
                tickTimer = FishingNativeAccessors.CreateVoidFloatMethod(FishingNativeAccessors.FindMethod(type, "Tick", 1));
                timerType = type;
                if (getTimerProgress == null || tickTimer == null)
                {
                    unavailableTimerType = type;
                    return false;
                }
                return true;
            }
            catch
            {
                unavailableTimerType = type;
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
                setPowerBarProgress = FishingNativeAccessors.CreateFloatSetter(FishingNativeAccessors.FindMember(type, "Progress"));
                powerBarType = type;
                if (setPowerBarProgress == null)
                    unavailablePowerBarType = type;
                return setPowerBarProgress != null;
            }
            catch
            {
                unavailablePowerBarType = type;
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
                refreshPowerBarColor = FishingNativeAccessors.CreateObjectFloatResultSetter(getColor, FishingNativeAccessors.FindMember(targetPowerBarType, "Color"));
                readyRendererType = rendererType;
                if (refreshPowerBarColor == null)
                {
                    unavailableReadyRendererType = rendererType;
                    unavailablePowerBarColorType = targetPowerBarType;
                }
                return refreshPowerBarColor != null;
            }
            catch
            {
                unavailableReadyRendererType = rendererType;
                unavailablePowerBarColorType = targetPowerBarType;
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
                getFixedDeltaTime = FishingNativeAccessors.CreateStaticFloatGetter(FishingNativeAccessors.FindMember(timeType, "fixedDeltaTime", isStatic: true));
                if (getFixedDeltaTime == null)
                {
                    fixedDeltaTimeUnavailable = true;
                    return 0.02f;
                }
                return Math.Max(0.0001f, getFixedDeltaTime());
            }
            catch
            {
                fixedDeltaTimeUnavailable = true;
                return 0.02f;
            }
        }

    }
}
