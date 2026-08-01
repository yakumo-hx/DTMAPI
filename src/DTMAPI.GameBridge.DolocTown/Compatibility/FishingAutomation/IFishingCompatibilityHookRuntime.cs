namespace DTMAPI.GameBridge.DolocTown
{
    /// <summary>Private callback surface for the frozen IFishingAutomationApi executor.</summary>
    internal interface IFishingCompatibilityHookRuntime
    {
        bool HasActiveRuntimeConsumer { get; }
        bool FishingHooksInstalled { get; }
        int ReadyAccessorBuildCount { get; }
        int ReadyAccessorRebuildCount { get; }
        int ReadyAccessorBuildFailureCount { get; }
        int ReadyAccessorInvocationFailureCount { get; }

        void SetFishingHooksInstalled(bool installed);
        void NotifyFishingNativeFrame();
        void NotifyFishingPhase(string phase, object? source);
        void ApplyFishingReadyAutomation(object readyState);
        bool ApplyFishingWaitAutomation(object waitState, string hookSource);
        void ConfirmFishingWaitNativeReelAccepted(object waitState, object? nextState);
        void NotifyFishingMiniGameStart(object gameHandle);
        void PrepareFishingMiniGameAutomationInput(object gameHandle);
        void ApplyFishingMiniGameAutomationTick(object gameHandle);
        void NotifyFishingMiniGameStop(object gameHandle);
        bool TryOverrideFishingReadyChargeInput(string inputName, out bool value);
        bool TryOverrideFishingMiniGameInput(string inputName, out bool value);
        bool TryQueueVisibleReelInput(object waitState);
        void ClearQueuedFishingReelInput();
        void AdjustFishingCastHookPhysics(object renderer);
        void RestoreExperimentalAnimatorSpeeds(string reason);
        void NotifyFishingNativeExit(string source);
        void AdjustFishingPullDurationResult(ref float result, string source);
    }
}
