using System;
using System.Reflection;
using global::DTMAPI.Abstractions;
using static Yuuka.DTMAPI.AutoFishing.ProductNativeHelpers;

namespace Yuuka.DTMAPI.AutoFishing
{
    internal enum FishingNativeMiniGameStatus
    {
        Unknown,
        Running,
        Success,
        Failed
    }

    internal sealed class FishingMiniGameNativeCache
    {
        private readonly Func<float>? suppliedTimeGetter;
        private Func<float>? getUnityTime;
        private Type? gameType;
        private Func<object, int>? getGameStatus;
        private Func<object, object?>? getNoteSpawner;
        private Func<object, float>? getStartTime;
        private Func<object, int>? getCurrentNoteIndex;
        private Func<object, object?>? getCurrentNote;
        private int runningStatus;
        private int successStatus;
        private int failedStatus;
        private Type? spawnerType;
        private Func<object, float>? getDelayTime;
        private Func<object, int, object?>? getNote;
        private Type? noteType;
        private Func<object, int>? getNoteKind;
        private Func<object, float>? getNoteStart;
        private Func<object, float>? getNoteEnd;
        private Func<object, int>? getNoteIndex;
        private int delayNoteKind;
        private int stableNoteKind;
        private int bonusNoteKind;
        private int avoidNoteKind;
        private bool unityTimeUnavailable;
        private Type? unavailableGameType;
        private Type? unavailableSpawnerType;
        private Type? unavailableNoteType;

        internal int AccessorBuilds { get; private set; }
        internal int AccessorRebuilds { get; private set; }
        internal int AccessorBuildFailures { get; private set; }
        internal int AccessorInvocationFailures { get; private set; }
        internal int AccessorFailures => AccessorBuildFailures + AccessorInvocationFailures;
        internal string LastAccessorFailure { get; private set; } = string.Empty;

        internal FishingMiniGameNativeCache(Func<float>? suppliedTimeGetter = null)
        {
            this.suppliedTimeGetter = suppliedTimeGetter;
            getUnityTime = suppliedTimeGetter;
        }

        internal bool TryReadStatus(object gameHandle, out FishingNativeMiniGameStatus status)
        {
            status = FishingNativeMiniGameStatus.Unknown;
            if (gameHandle == null || !EnsureGameAccessors(gameHandle.GetType()))
                return false;
            try
            {
                int value = getGameStatus!(gameHandle);
                if (value == runningStatus)
                    status = FishingNativeMiniGameStatus.Running;
                else if (value == successStatus)
                    status = FishingNativeMiniGameStatus.Success;
                else if (value == failedStatus)
                    status = FishingNativeMiniGameStatus.Failed;
                return status != FishingNativeMiniGameStatus.Unknown;
            }
            catch (Exception ex)
            {
                MarkInvocationFailure("game-status", ex);
                return false;
            }
        }

        internal FishingMiniGameFrameReadStatus TryBuildFrame(
            object gameHandle,
            long sequence,
            bool bonusAlreadyTapped,
            out FishingMiniGameFrame frame,
            out string failureReason)
        {
            frame = default;
            failureReason = string.Empty;
            if (gameHandle == null)
            {
                failureReason = "game-handle-null";
                return FishingMiniGameFrameReadStatus.Faulted;
            }
            if (!EnsureGameAccessors(gameHandle.GetType()))
            {
                failureReason = LastAccessorFailure.Length > 0 ? LastAccessorFailure : "game-accessors-unavailable";
                return FishingMiniGameFrameReadStatus.Faulted;
            }
            if (!EnsureUnityTimeAccessor())
            {
                failureReason = LastAccessorFailure.Length > 0 ? LastAccessorFailure : "unity-time-unavailable";
                return FishingMiniGameFrameReadStatus.Faulted;
            }
            try
            {
                object? spawner = getNoteSpawner!(gameHandle);
                if (spawner == null)
                {
                    failureReason = "note-spawner-not-ready";
                    return FishingMiniGameFrameReadStatus.NotReady;
                }
                if (!EnsureSpawnerAccessors(spawner.GetType()))
                {
                    failureReason = LastAccessorFailure.Length > 0 ? LastAccessorFailure : "spawner-accessors-unavailable";
                    return FishingMiniGameFrameReadStatus.Faulted;
                }

                double currentTime = Math.Max(0d, getUnityTime!() - getStartTime!(gameHandle));
                double delayTime = getDelayTime!(spawner);
                FishingMiniGameNoteKind kind = FishingMiniGameNoteKind.None;
                int index = -1;
                double start = 0d;
                double end = 0d;
                if (currentTime >= delayTime)
                {
                    object? note = getCurrentNote!(gameHandle);
                    if (note != null && !EnsureNoteAccessors(note.GetType()))
                    {
                        failureReason = LastAccessorFailure.Length > 0 ? LastAccessorFailure : "note-accessors-unavailable";
                        return FishingMiniGameFrameReadStatus.Faulted;
                    }
                    if (note == null || currentTime > getNoteEnd!(note))
                    {
                        int nextIndex = getCurrentNoteIndex!(gameHandle);
                        note = getNote!(spawner, nextIndex);
                        if (note != null && !EnsureNoteAccessors(note.GetType()))
                        {
                            failureReason = LastAccessorFailure.Length > 0 ? LastAccessorFailure : "note-accessors-unavailable";
                            return FishingMiniGameFrameReadStatus.Faulted;
                        }
                    }

                    if (note != null)
                    {
                        int nativeKind = getNoteKind!(note);
                        if (nativeKind != delayNoteKind)
                        {
                            kind = nativeKind == stableNoteKind
                                ? FishingMiniGameNoteKind.Stable
                                : nativeKind == bonusNoteKind
                                    ? FishingMiniGameNoteKind.Bonus
                                    : nativeKind == avoidNoteKind
                                        ? FishingMiniGameNoteKind.Avoid
                                        : FishingMiniGameNoteKind.Unknown;
                            index = getNoteIndex != null ? getNoteIndex(note) : getCurrentNoteIndex!(gameHandle);
                            start = getNoteStart!(note);
                            end = getNoteEnd!(note);
                        }
                    }
                }

                frame = new FishingMiniGameFrame(sequence, currentTime, kind, index, start, end, bonusAlreadyTapped);
                return FishingMiniGameFrameReadStatus.Ready;
            }
            catch (Exception ex)
            {
                MarkInvocationFailure("frame", ex);
                failureReason = LastAccessorFailure;
                return FishingMiniGameFrameReadStatus.Faulted;
            }
        }

        private bool EnsureUnityTimeAccessor()
        {
            if (getUnityTime != null)
                return true;
            if (unityTimeUnavailable)
                return false;
            try
            {
                Type? timeType = ResolveType("UnityEngine.Time, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.Time, UnityEngine");
                getUnityTime = FishingNativeAccessors.CreateStaticFloatGetter(FishingNativeAccessors.FindMember(timeType, "time", isStatic: true));
                AccessorBuilds++;
                if (getUnityTime == null)
                {
                    unityTimeUnavailable = true;
                    MarkBuildFailure("UnityEngine.Time.time", null);
                }
                return getUnityTime != null;
            }
            catch (Exception ex)
            {
                unityTimeUnavailable = true;
                MarkBuildFailure("UnityEngine.Time.time", ex);
                return false;
            }
        }

        private bool EnsureGameAccessors(Type type)
        {
            if (gameType == type && getGameStatus != null && getNoteSpawner != null && getStartTime != null && getCurrentNoteIndex != null && getCurrentNote != null)
                return true;
            if (unavailableGameType == type)
                return false;
            try
            {
                MemberInfo? statusMember = FishingNativeAccessors.FindMember(type, "currentGameStatus");
                Type? statusEnum = statusMember is FieldInfo field ? field.FieldType : (statusMember as PropertyInfo)?.PropertyType;
                getGameStatus = FishingNativeAccessors.CreateEnumIntGetter(statusMember);
                getNoteSpawner = FishingNativeAccessors.CreateObjectGetter(FishingNativeAccessors.FindMember(type, "noteSpawner"));
                getStartTime = FishingNativeAccessors.CreateFloatGetter(FishingNativeAccessors.FindMember(type, "startTime"));
                getCurrentNoteIndex = FishingNativeAccessors.CreateIntGetter(FishingNativeAccessors.FindMember(type, "currentNoteIndex"));
                getCurrentNote = FishingNativeAccessors.CreateObjectGetter(FishingNativeAccessors.FindMember(type, "currentNote"));
                if (statusEnum != null)
                {
                    runningStatus = ParseEnum(statusEnum, "Running");
                    successStatus = ParseEnum(statusEnum, "Success");
                    failedStatus = ParseEnum(statusEnum, "Failed");
                }
                RecordRebuild(gameType, type);
                gameType = type;
                AccessorBuilds++;
                bool ready = getGameStatus != null && getNoteSpawner != null && getStartTime != null && getCurrentNoteIndex != null && getCurrentNote != null;
                if (!ready)
                {
                    unavailableGameType = type;
                    MarkBuildFailure("game-members", null);
                }
                return ready;
            }
            catch (Exception ex)
            {
                unavailableGameType = type;
                MarkBuildFailure("game-members", ex);
                return false;
            }
        }

        private bool EnsureSpawnerAccessors(Type type)
        {
            if (spawnerType == type && getDelayTime != null && getNote != null)
                return true;
            if (unavailableSpawnerType == type)
                return false;
            try
            {
                getDelayTime = FishingNativeAccessors.CreateFloatGetter(FishingNativeAccessors.FindMember(type, "delayTime"));
                getNote = FishingNativeAccessors.CreateObjectIntMethod(FishingNativeAccessors.FindMethod(type, "GetNote", 1));
                RecordRebuild(spawnerType, type);
                spawnerType = type;
                AccessorBuilds++;
                bool ready = getDelayTime != null && getNote != null;
                if (!ready)
                {
                    unavailableSpawnerType = type;
                    MarkBuildFailure("spawner-members", null);
                }
                return ready;
            }
            catch (Exception ex)
            {
                unavailableSpawnerType = type;
                MarkBuildFailure("spawner-members", ex);
                return false;
            }
        }

        private bool EnsureNoteAccessors(Type type)
        {
            if (noteType == type && getNoteKind != null && getNoteStart != null && getNoteEnd != null)
                return true;
            if (unavailableNoteType == type)
                return false;
            try
            {
                MemberInfo? kindMember = FishingNativeAccessors.FindMember(type, "noteType");
                Type? kindEnum = kindMember is FieldInfo field ? field.FieldType : (kindMember as PropertyInfo)?.PropertyType;
                getNoteKind = FishingNativeAccessors.CreateEnumIntGetter(kindMember);
                getNoteStart = FishingNativeAccessors.CreateFloatGetter(FishingNativeAccessors.FindMember(type, "startTime"));
                getNoteEnd = FishingNativeAccessors.CreateFloatGetter(FishingNativeAccessors.FindMember(type, "endTime"));
                getNoteIndex = FishingNativeAccessors.CreateIntGetter(FishingNativeAccessors.FindMember(type, "index"));
                if (kindEnum != null)
                {
                    delayNoteKind = ParseEnum(kindEnum, "Delay");
                    stableNoteKind = ParseEnum(kindEnum, "Stable");
                    bonusNoteKind = ParseEnum(kindEnum, "Bonus");
                    avoidNoteKind = ParseEnumOrMissing(kindEnum, "Avoid");
                }
                RecordRebuild(noteType, type);
                noteType = type;
                AccessorBuilds++;
                bool ready = getNoteKind != null && getNoteStart != null && getNoteEnd != null;
                if (!ready)
                {
                    unavailableNoteType = type;
                    MarkBuildFailure("note-members", null);
                }
                return ready;
            }
            catch (Exception ex)
            {
                unavailableNoteType = type;
                MarkBuildFailure("note-members", ex);
                return false;
            }
        }

        private static int ParseEnum(Type enumType, string name)
        {
            object value = Enum.Parse(enumType, name, ignoreCase: false);
            return Convert.ToInt32(value);
        }

        private static int ParseEnumOrMissing(Type enumType, string name)
        {
            return Array.IndexOf(Enum.GetNames(enumType), name) >= 0
                ? ParseEnum(enumType, name)
                : int.MinValue;
        }

        private void RecordRebuild(Type? previousType, Type nextType)
        {
            if (previousType != null && previousType != nextType)
                AccessorRebuilds++;
        }

        private void MarkBuildFailure(string member, Exception? ex)
        {
            AccessorBuildFailures++;
            LastAccessorFailure = "build:" + member + (ex == null ? string.Empty : ":" + ex.GetType().Name);
        }

        private void MarkInvocationFailure(string member, Exception ex)
        {
            AccessorInvocationFailures++;
            LastAccessorFailure = "invoke:" + member + ":" + ex.GetType().Name;
        }
    }
}
