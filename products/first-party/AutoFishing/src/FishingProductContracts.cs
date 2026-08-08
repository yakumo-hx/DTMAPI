namespace Yuuka.DTMAPI.AutoFishing
{
    internal enum FishingPrimitivePhase
    {
        Idle,
        ReadyEntered,
        CastEntered,
        WaitEntered,
        WaitPlayable,
        BiteReady,
        MiniGameStarted,
        MiniGameRunning,
        MiniGameStopped,
        PullEntered,
        PullExited,
        Interrupted
    }

    internal enum FishingPrimitiveTransitionKind
    {
        SnapshotChanged,
        ReadyEntered,
        CastEntered,
        WaitEntered,
        WaitPlayable,
        BiteReady,
        MiniGameStarted,
        MiniGameStopped,
        PullEntered,
        PullExited,
        Interrupted,
        Released
    }

    internal enum FishingPrimitiveReelMode
    {
        NativeVisibleMiniGame,
        SkipMiniGameNativeResult
    }

    internal enum FishingSyntheticInputAction
    {
        Release,
        Hold,
        TapBonus
    }

    internal enum FishingMiniGameNoteKind
    {
        None,
        Stable,
        Avoid,
        Bonus,
        Unknown
    }

    internal readonly struct FishingPrimitiveSnapshot
    {
        public FishingPrimitiveSnapshot(
            long sequence,
            FishingPrimitivePhase phase,
            bool canCast,
            bool hasSelectedRod,
            bool hasFishingPool,
            bool waitPlayable,
            bool biteReady,
            bool isFishResult,
            bool miniGameActive,
            bool nativeMovementAvailable,
            double nativeInputMultiplier,
            double nativeVelocityX,
            double nativeOffsetX)
        {
            Sequence = sequence;
            Phase = phase;
            CanCast = canCast;
            HasSelectedRod = hasSelectedRod;
            HasFishingPool = hasFishingPool;
            WaitPlayable = waitPlayable;
            BiteReady = biteReady;
            IsFishResult = isFishResult;
            MiniGameActive = miniGameActive;
            NativeMovementAvailable = nativeMovementAvailable;
            NativeInputMultiplier = nativeInputMultiplier;
            NativeVelocityX = nativeVelocityX;
            NativeOffsetX = nativeOffsetX;
        }

        public long Sequence { get; }
        public FishingPrimitivePhase Phase { get; }
        public bool CanCast { get; }
        public bool HasSelectedRod { get; }
        public bool HasFishingPool { get; }
        public bool WaitPlayable { get; }
        public bool BiteReady { get; }
        public bool IsFishResult { get; }
        public bool MiniGameActive { get; }
        public bool NativeMovementAvailable { get; }
        public double NativeInputMultiplier { get; }
        public double NativeVelocityX { get; }
        public double NativeOffsetX { get; }
    }

    internal readonly struct FishingPrimitiveResult
    {
        public FishingPrimitiveResult(bool applied, string status, string message, long sequence)
        {
            Applied = applied;
            Status = status ?? string.Empty;
            Message = message ?? string.Empty;
            Sequence = sequence;
        }

        public bool Applied { get; }
        public string Status { get; }
        public string Message { get; }
        public long Sequence { get; }
    }

    internal readonly struct FishingMiniGameFrame
    {
        public FishingMiniGameFrame(
            long sequence,
            double currentTime,
            FishingMiniGameNoteKind noteKind,
            int noteIndex,
            double noteStart,
            double noteEnd,
            bool bonusAlreadyTapped)
        {
            Sequence = sequence;
            CurrentTime = currentTime;
            NoteKind = noteKind;
            NoteIndex = noteIndex;
            NoteStart = noteStart;
            NoteEnd = noteEnd;
            BonusAlreadyTapped = bonusAlreadyTapped;
        }

        public long Sequence { get; }
        public double CurrentTime { get; }
        public FishingMiniGameNoteKind NoteKind { get; }
        public int NoteIndex { get; }
        public double NoteStart { get; }
        public double NoteEnd { get; }
        public bool BonusAlreadyTapped { get; }
    }

    internal readonly struct FishingAnimationLeaseRequest
    {
        public FishingAnimationLeaseRequest(double readyMultiplier, double castHookMultiplier, double pullMultiplier)
        {
            ReadyMultiplier = readyMultiplier;
            CastHookMultiplier = castHookMultiplier;
            PullMultiplier = pullMultiplier;
        }

        public double ReadyMultiplier { get; }
        public double CastHookMultiplier { get; }
        public double PullMultiplier { get; }
    }

    internal readonly struct FishingPrimitiveCastRequest
    {
        public FishingPrimitiveCastRequest(double chargeRatio)
        {
            ChargeRatio = chargeRatio;
        }

        public double ChargeRatio { get; }
    }

}
