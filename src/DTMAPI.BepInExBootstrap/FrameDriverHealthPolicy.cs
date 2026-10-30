using System;

namespace DTMAPI.BepInExBootstrap
{
    internal enum FrameDriverHealthDecision
    {
        NoAction,
        GlobalPause,
        RecoverInputSystem
    }

    internal enum FrameDriverHealthProbeKind
    {
        None,
        RetryMissingSubscription,
        ObserveGlobalPause,
        VerifyIsolatedInputStall
    }

    internal readonly struct FrameDriverHealthSnapshot
    {
        public FrameDriverHealthSnapshot(
            bool inputSystemSubscribed,
            bool inputSystemCallbackSeen,
            bool playerLoopCallbackSeen,
            bool nativeCallbackSeen,
            bool unityUpdateCallbackSeen,
            long inputSystemCallbackCount,
            long playerLoopCallbackCount,
            long nativeCallbackCount,
            long unityUpdateCallbackCount,
            DateTimeOffset lastInputSystemCallbackAt,
            DateTimeOffset lastInputSystemSubscribeAttemptAt,
            DateTimeOffset lastPlayerLoopCallbackAt,
            DateTimeOffset lastNativeCallbackAt,
            DateTimeOffset lastUnityUpdateCallbackAt)
        {
            InputSystemSubscribed = inputSystemSubscribed;
            InputSystemCallbackSeen = inputSystemCallbackSeen;
            PlayerLoopCallbackSeen = playerLoopCallbackSeen;
            NativeCallbackSeen = nativeCallbackSeen;
            UnityUpdateCallbackSeen = unityUpdateCallbackSeen;
            InputSystemCallbackCount = inputSystemCallbackCount;
            PlayerLoopCallbackCount = playerLoopCallbackCount;
            NativeCallbackCount = nativeCallbackCount;
            UnityUpdateCallbackCount = unityUpdateCallbackCount;
            LastInputSystemCallbackAt = lastInputSystemCallbackAt;
            LastInputSystemSubscribeAttemptAt = lastInputSystemSubscribeAttemptAt;
            LastPlayerLoopCallbackAt = lastPlayerLoopCallbackAt;
            LastNativeCallbackAt = lastNativeCallbackAt;
            LastUnityUpdateCallbackAt = lastUnityUpdateCallbackAt;
        }

        public bool InputSystemSubscribed { get; }
        public bool InputSystemCallbackSeen { get; }
        public bool PlayerLoopCallbackSeen { get; }
        public bool NativeCallbackSeen { get; }
        public bool UnityUpdateCallbackSeen { get; }
        public long InputSystemCallbackCount { get; }
        public long PlayerLoopCallbackCount { get; }
        public long NativeCallbackCount { get; }
        public long UnityUpdateCallbackCount { get; }
        public DateTimeOffset LastInputSystemCallbackAt { get; }
        public DateTimeOffset LastInputSystemSubscribeAttemptAt { get; }
        public DateTimeOffset LastPlayerLoopCallbackAt { get; }
        public DateTimeOffset LastNativeCallbackAt { get; }
        public DateTimeOffset LastUnityUpdateCallbackAt { get; }
    }

    internal static class FrameDriverHealthPolicy
    {
        internal static readonly TimeSpan FrameFreshness = TimeSpan.FromMilliseconds(500);
        internal static readonly TimeSpan InputSystemStallThreshold = TimeSpan.FromSeconds(2);
        internal static readonly TimeSpan InputSystemRetryInterval = TimeSpan.FromSeconds(10);

        public static bool ShouldQueue(FrameDriverHealthSnapshot current, DateTimeOffset now)
        {
            return GetProbeKind(current, now) != FrameDriverHealthProbeKind.None;
        }

        public static FrameDriverHealthProbeKind GetProbeKind(FrameDriverHealthSnapshot current, DateTimeOffset now)
        {
            if (!current.InputSystemSubscribed && IsElapsed(current.LastInputSystemSubscribeAttemptAt, now, InputSystemRetryInterval))
                return FrameDriverHealthProbeKind.RetryMissingSubscription;

            bool siblingRecent = HasRecentSibling(current, now);
            if (current.InputSystemSubscribed && IsInputSystemStale(current, now) && siblingRecent)
                return FrameDriverHealthProbeKind.VerifyIsolatedInputStall;

            return !HasAnyRecentFrameSource(current, now)
                ? FrameDriverHealthProbeKind.ObserveGlobalPause
                : FrameDriverHealthProbeKind.None;
        }

        public static FrameDriverHealthDecision Evaluate(
            FrameDriverHealthProbeKind probeKind,
            FrameDriverHealthSnapshot queued,
            FrameDriverHealthSnapshot current,
            DateTimeOffset now)
        {
            if (probeKind == FrameDriverHealthProbeKind.ObserveGlobalPause)
                return FrameDriverHealthDecision.GlobalPause;
            if (probeKind != FrameDriverHealthProbeKind.VerifyIsolatedInputStall)
                return FrameDriverHealthDecision.NoAction;
            if (!current.InputSystemSubscribed || !IsInputSystemStale(current, now))
                return FrameDriverHealthDecision.NoAction;
            if (current.InputSystemCallbackCount > queued.InputSystemCallbackCount)
                return FrameDriverHealthDecision.NoAction;

            bool siblingAdvanced =
                current.PlayerLoopCallbackCount > queued.PlayerLoopCallbackCount ||
                current.NativeCallbackCount > queued.NativeCallbackCount ||
                current.UnityUpdateCallbackCount > queued.UnityUpdateCallbackCount;
            if (siblingAdvanced || HasRecentSibling(current, now))
                return FrameDriverHealthDecision.RecoverInputSystem;

            return FrameDriverHealthDecision.GlobalPause;
        }

        private static bool IsInputSystemStale(FrameDriverHealthSnapshot snapshot, DateTimeOffset now)
        {
            DateTimeOffset reference = snapshot.InputSystemCallbackSeen
                ? snapshot.LastInputSystemCallbackAt
                : snapshot.LastInputSystemSubscribeAttemptAt;
            TimeSpan threshold = snapshot.InputSystemCallbackSeen
                ? InputSystemStallThreshold
                : InputSystemRetryInterval;
            return IsElapsed(reference, now, threshold);
        }

        private static bool HasRecentSibling(FrameDriverHealthSnapshot snapshot, DateTimeOffset now)
        {
            return IsRecent(snapshot.PlayerLoopCallbackSeen, snapshot.LastPlayerLoopCallbackAt, now) ||
                IsRecent(snapshot.NativeCallbackSeen, snapshot.LastNativeCallbackAt, now) ||
                IsRecent(snapshot.UnityUpdateCallbackSeen, snapshot.LastUnityUpdateCallbackAt, now);
        }

        private static bool HasAnyRecentFrameSource(FrameDriverHealthSnapshot snapshot, DateTimeOffset now)
        {
            return IsRecent(snapshot.InputSystemCallbackSeen, snapshot.LastInputSystemCallbackAt, now) ||
                HasRecentSibling(snapshot, now);
        }

        private static bool IsRecent(bool seen, DateTimeOffset value, DateTimeOffset now)
        {
            return seen && value != DateTimeOffset.MinValue && now - value < FrameFreshness;
        }

        private static bool IsElapsed(DateTimeOffset value, DateTimeOffset now, TimeSpan threshold)
        {
            return value == DateTimeOffset.MinValue || now - value >= threshold;
        }
    }
}
