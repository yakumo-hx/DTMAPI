using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace DTMAPI.GameBridge.DolocTown.QA
{
    [DataContract]
    internal sealed class Batch6AutoFishingPilotEvidence
    {
        [DataMember(Name = "schemaVersion", Order = 1)] internal int SchemaVersion { get; set; } = 1;
        [DataMember(Name = "caseId", Order = 2)] internal string CaseId { get; set; } = Batch6AutoFishingPilotSettings.CaseId;
        [DataMember(Name = "runId", Order = 3)] internal string RunId { get; set; } = string.Empty;
        [DataMember(Name = "level", Order = 4)] internal string Level { get; set; } = string.Empty;
        [DataMember(Name = "scenario", Order = 5)] internal string Scenario { get; set; } = string.Empty;
        [DataMember(Name = "measureSeconds", Order = 6)] internal int MeasureSeconds { get; set; }
        [DataMember(Name = "sampleSeconds", Order = 7)] internal int SampleSeconds { get; set; }
        [DataMember(Name = "warmupFish", Order = 8)] internal int WarmupFish { get; set; }
        [DataMember(Name = "targetFish", Order = 9)] internal int TargetFish { get; set; }
        [DataMember(Name = "multiplier", Order = 10)] internal double Multiplier { get; set; }
        [DataMember(Name = "forcedGc", Order = 11)] internal bool ForcedGc { get; set; }
        [DataMember(Name = "status", Order = 12)] internal string Status { get; set; } = "Pending";
        [DataMember(Name = "failureCode", Order = 13)] internal string FailureCode { get; set; } = string.Empty;
        [DataMember(Name = "failureReason", Order = 14)] internal string FailureReason { get; set; } = string.Empty;
        [DataMember(Name = "startedAtUtc", Order = 15)] internal string StartedAtUtc { get; set; } = string.Empty;
        [DataMember(Name = "completedAtUtc", Order = 16)] internal string CompletedAtUtc { get; set; } = string.Empty;
        [DataMember(Name = "packageSha256", Order = 17)] internal string PackageSha256 { get; set; } = string.Empty;
        [DataMember(Name = "entryDllSha256", Order = 18)] internal string EntryDllSha256 { get; set; } = string.Empty;
        [DataMember(Name = "manifestSha256", Order = 19)] internal string ManifestSha256 { get; set; } = string.Empty;
        [DataMember(Name = "referencePolicySha256", Order = 20)] internal string ReferencePolicySha256 { get; set; } = string.Empty;
        [DataMember(Name = "expectedPackageSha256", Order = 21)] internal string ExpectedPackageSha256 { get; set; } = string.Empty;
        [DataMember(Name = "expectedEntryDllSha256", Order = 22)] internal string ExpectedEntryDllSha256 { get; set; } = string.Empty;
        [DataMember(Name = "expectedManifestSha256", Order = 23)] internal string ExpectedManifestSha256 { get; set; } = string.Empty;
        [DataMember(Name = "expectedReferencePolicySha256", Order = 24)] internal string ExpectedReferencePolicySha256 { get; set; } = string.Empty;
        [DataMember(Name = "productAssemblyReferenced", Order = 25)] internal bool ProductAssemblyReferenced { get; set; }
        [DataMember(Name = "productAssemblyReferenceMeaning", Order = 26)] internal string ProductAssemblyReferenceMeaning { get; set; } = "QA assembly has no static Product AssemblyRef";
        [DataMember(Name = "productPresent", Order = 27)] internal bool ProductPresent { get; set; }
        [DataMember(Name = "coreResidentProductInstanceObserved", Order = 28)] internal bool CoreResidentProductInstanceObserved { get; set; }
        [DataMember(Name = "productAbsentObserved", Order = 29)] internal bool ProductAbsentObserved { get; set; }
        [DataMember(Name = "driverKind", Order = 30)] internal string DriverKind { get; set; } = string.Empty;
        [DataMember(Name = "driverOwner", Order = 31)] internal string DriverOwner { get; set; } = string.Empty;
        [DataMember(Name = "driverPatchCount", Order = 32)] internal int DriverPatchCount { get; set; }
        [DataMember(Name = "driverNativeProgress", Order = 33)] internal bool DriverNativeProgress { get; set; }
        [DataMember(Name = "driverWarmupUnits", Order = 34)] internal long DriverWarmupUnits { get; set; }
        [DataMember(Name = "driverMeasuredUnits", Order = 35)] internal long DriverMeasuredUnits { get; set; }
        [DataMember(Name = "recoveryDriverKind", Order = 36)] internal string RecoveryDriverKind { get; set; } = string.Empty;
        [DataMember(Name = "recoveryDriverOwner", Order = 37)] internal string RecoveryDriverOwner { get; set; } = string.Empty;
        [DataMember(Name = "recoveryDriverNativeProgress", Order = 38)] internal bool RecoveryDriverNativeProgress { get; set; }
        [DataMember(Name = "recoveryDriverUnits", Order = 39)] internal long RecoveryDriverUnits { get; set; }
        [DataMember(Name = "package", Order = 40)] internal Batch6AutoFishingPackageProvenance? Package { get; set; }
        [DataMember(Name = "initialProductState", Order = 41)] internal Batch6AutoFishingObservation? InitialProductState { get; set; }
        [DataMember(Name = "finalProductState", Order = 42)] internal Batch6AutoFishingObservation? FinalProductState { get; set; }
        [DataMember(Name = "stages", Order = 43)] internal List<Batch6AutoFishingPilotStageReceipt> Stages { get; set; } = new List<Batch6AutoFishingPilotStageReceipt>();
        [DataMember(Name = "samples", Order = 44)] internal List<Batch6AutoFishingPilotSample> Samples { get; set; } = new List<Batch6AutoFishingPilotSample>();
        [DataMember(Name = "cleanup", Order = 45)] internal Batch6AutoFishingCleanupReceipt Cleanup { get; set; } = new Batch6AutoFishingCleanupReceipt();
        [DataMember(Name = "observerCadenceMilliseconds", Order = 46)] internal int ObserverCadenceMilliseconds { get; set; }
        [DataMember(Name = "observerEffect", Order = 47)] internal string ObserverEffect { get; set; } = string.Empty;
        [DataMember(Name = "formal", Order = 48)] internal bool Formal { get; set; }
        [DataMember(Name = "authoritative", Order = 49)] internal bool Authoritative { get; set; }
        [DataMember(Name = "authority", Order = 50)] internal string Authority { get; set; } = "non-authoritative";
        [DataMember(Name = "nativeFishingContextVerified", Order = 51)] internal bool NativeFishingContextVerified { get; set; }
        [DataMember(Name = "nativeFishingContexts", Order = 52)] internal List<Batch6AutoFishingNativeFishingContextReceipt> NativeFishingContexts { get; set; } = new List<Batch6AutoFishingNativeFishingContextReceipt>();
        [DataMember(Name = "nativeVitals", Order = 53)] internal Batch6AutoFishingNativeVitalsEvidence NativeVitals { get; set; } = new Batch6AutoFishingNativeVitalsEvidence();
        [DataMember(Name = "nativeProgressTrailingWindowVerified", Order = 54)] internal bool NativeProgressTrailingWindowVerified { get; set; }
        [DataMember(Name = "nativeProgressTrailingWindowSeconds", Order = 55)] internal int NativeProgressTrailingWindowSeconds { get; set; }
        [DataMember(Name = "lastNativeProgressAtUtc", Order = 56)] internal string LastNativeProgressAtUtc { get; set; } = string.Empty;
        [DataMember(Name = "trailingNativeProgressGapSeconds", Order = 57)] internal double TrailingNativeProgressGapSeconds { get; set; }
        [DataMember(Name = "contractKind", Order = 58)] internal string ContractKind { get; set; } = Batch6AutoFishingPilotSettings.LongRunContract;
        [DataMember(Name = "authorityScope", Order = 59)] internal string AuthorityScope { get; set; } = "non-authoritative";
        [DataMember(Name = "castChargeRatio", Order = 60)] internal double CastChargeRatio { get; set; }
        [DataMember(Name = "manualMovementCancel", Order = 61)] internal bool ManualMovementCancel { get; set; }
        [DataMember(Name = "behavior", Order = 62)] internal Batch6AutoFishingBehaviorReceipt Behavior { get; set; } = new Batch6AutoFishingBehaviorReceipt();
        [DataMember(Name = "toggleKey", Order = 63)] internal string ToggleKey { get; set; } = string.Empty;
        [DataMember(Name = "manualMovementTargetPhases", Order = 64)] internal string[] ManualMovementTargetPhases { get; set; } = Array.Empty<string>();
        [DataMember(Name = "nativeSurfaceDiagnostics", Order = 65)] internal List<Batch6AutoFishingNativeSurfaceReceipt> NativeSurfaceDiagnostics { get; set; } = new List<Batch6AutoFishingNativeSurfaceReceipt>();
    }

    [DataContract]
    internal sealed class Batch6AutoFishingBehaviorReceipt
    {
        [DataMember(Name = "verified", Order = 1)] internal bool Verified { get; set; }
        [DataMember(Name = "scenario", Order = 2)] internal string Scenario { get; set; } = string.Empty;
        [DataMember(Name = "castChargeRatio", Order = 3)] internal double CastChargeRatio { get; set; }
        [DataMember(Name = "manualMovementCancel", Order = 4)] internal bool ManualMovementCancel { get; set; }
        [DataMember(Name = "baseline", Order = 5)] internal Batch6AutoFishingObservation? Baseline { get; set; }
        [DataMember(Name = "final", Order = 6)] internal Batch6AutoFishingObservation? Final { get; set; }
        [DataMember(Name = "castAppliedDelta", Order = 7)] internal long CastAppliedDelta { get; set; }
        [DataMember(Name = "pullEnteredDelta", Order = 8)] internal long PullEnteredDelta { get; set; }
        [DataMember(Name = "pullExitedDelta", Order = 9)] internal long PullExitedDelta { get; set; }
        [DataMember(Name = "nativeBitePreparedDelta", Order = 10)] internal long NativeBitePreparedDelta { get; set; }
        [DataMember(Name = "nativeVisibleReelDelta", Order = 11)] internal long NativeVisibleReelDelta { get; set; }
        [DataMember(Name = "nativeSkipReelDelta", Order = 12)] internal long NativeSkipReelDelta { get; set; }
        [DataMember(Name = "visibleReelQueuedDelta", Order = 13)] internal long VisibleReelQueuedDelta { get; set; }
        [DataMember(Name = "visibleReelConsumedDelta", Order = 14)] internal long VisibleReelConsumedDelta { get; set; }
        [DataMember(Name = "visibleReelNativeAcceptedDelta", Order = 15)] internal long VisibleReelNativeAcceptedDelta { get; set; }
        [DataMember(Name = "visibleReelRetryDelta", Order = 16)] internal long VisibleReelRetryDelta { get; set; }
        [DataMember(Name = "visibleReelTimeoutDelta", Order = 17)] internal long VisibleReelTimeoutDelta { get; set; }
        [DataMember(Name = "animationApplicationDelta", Order = 18)] internal int AnimationApplicationDelta { get; set; }
        [DataMember(Name = "readyChargeApplicationDelta", Order = 19)] internal int ReadyChargeApplicationDelta { get; set; }
        [DataMember(Name = "manualMovementReason", Order = 20)] internal string ManualMovementReason { get; set; } = string.Empty;
        [DataMember(Name = "physicalMovementHandshakeRequired", Order = 21)] internal bool PhysicalMovementHandshakeRequired { get; set; }
        [DataMember(Name = "fullNativeLoopVerified", Order = 22)] internal bool FullNativeLoopVerified { get; set; }
        [DataMember(Name = "details", Order = 23)] internal string Details { get; set; } = string.Empty;
        [DataMember(Name = "instantBiteCommittedDelta", Order = 24)] internal long InstantBiteCommittedDelta { get; set; }
        [DataMember(Name = "toggleKey", Order = 25)] internal string ToggleKey { get; set; } = string.Empty;
        [DataMember(Name = "movementPhaseReceipts", Order = 26)] internal List<Batch6AutoFishingMovementPhaseReceipt> MovementPhaseReceipts { get; set; } = new List<Batch6AutoFishingMovementPhaseReceipt>();
    }

    [DataContract]
    internal sealed class Batch6AutoFishingMovementPhaseReceipt
    {
        [DataMember(Name = "sequence", Order = 1)] internal int Sequence { get; set; }
        [DataMember(Name = "targetPhase", Order = 2)] internal string TargetPhase { get; set; } = string.Empty;
        [DataMember(Name = "observedPhase", Order = 3)] internal string ObservedPhase { get; set; } = string.Empty;
        [DataMember(Name = "releaseReason", Order = 4)] internal string ReleaseReason { get; set; } = string.Empty;
        [DataMember(Name = "inactiveCleanupVerified", Order = 5)] internal bool InactiveCleanupVerified { get; set; }
        [DataMember(Name = "details", Order = 6)] internal string Details { get; set; } = string.Empty;
    }

    [DataContract]
    internal sealed class Batch6AutoFishingPilotStageReceipt
    {
        [DataMember(Name = "index", Order = 1)] internal int Index { get; set; }
        [DataMember(Name = "observedAtUtc", Order = 2)] internal string ObservedAtUtc { get; set; } = string.Empty;
        [DataMember(Name = "state", Order = 3)] internal string State { get; set; } = string.Empty;
        [DataMember(Name = "status", Order = 4)] internal string Status { get; set; } = string.Empty;
        [DataMember(Name = "details", Order = 5)] internal string Details { get; set; } = string.Empty;
        [DataMember(Name = "saveLoadOrdinal", Order = 6)] internal int SaveLoadOrdinal { get; set; }
        [DataMember(Name = "productState", Order = 7)] internal Batch6AutoFishingObservation? ProductState { get; set; }
        [DataMember(Name = "driverKind", Order = 8)] internal string DriverKind { get; set; } = string.Empty;
        [DataMember(Name = "driverPatchCount", Order = 9)] internal int DriverPatchCount { get; set; }
        [DataMember(Name = "recoveryDriverKind", Order = 10)] internal string RecoveryDriverKind { get; set; } = string.Empty;
        [DataMember(Name = "packageSha256", Order = 11)] internal string PackageSha256 { get; set; } = string.Empty;
        [DataMember(Name = "entryDllSha256", Order = 12)] internal string EntryDllSha256 { get; set; } = string.Empty;
        [DataMember(Name = "manifestSha256", Order = 13)] internal string ManifestSha256 { get; set; } = string.Empty;
        [DataMember(Name = "referencePolicySha256", Order = 14)] internal string ReferencePolicySha256 { get; set; } = string.Empty;
        [DataMember(Name = "expectedPackageSha256", Order = 15)] internal string ExpectedPackageSha256 { get; set; } = string.Empty;
        [DataMember(Name = "expectedEntryDllSha256", Order = 16)] internal string ExpectedEntryDllSha256 { get; set; } = string.Empty;
        [DataMember(Name = "expectedManifestSha256", Order = 17)] internal string ExpectedManifestSha256 { get; set; } = string.Empty;
        [DataMember(Name = "expectedReferencePolicySha256", Order = 18)] internal string ExpectedReferencePolicySha256 { get; set; } = string.Empty;
        [DataMember(Name = "metrics", Order = 19)] internal Batch6AutoFishingPilotSample? Metrics { get; set; }
        [DataMember(Name = "cleanup", Order = 20)] internal Batch6AutoFishingCleanupReceipt Cleanup { get; set; } = new Batch6AutoFishingCleanupReceipt();
    }

    [DataContract]
    internal sealed class Batch6AutoFishingPilotSample
    {
        [DataMember(Name = "observedAtUtc", Order = 1)] internal string ObservedAtUtc { get; set; } = string.Empty;
        [DataMember(Name = "elapsedSeconds", Order = 2)] internal double ElapsedSeconds { get; set; }
        [DataMember(Name = "enabled", Order = 3)] internal bool Enabled { get; set; }
        [DataMember(Name = "updateSubscribed", Order = 4)] internal bool UpdateSubscribed { get; set; }
        [DataMember(Name = "sessionPresent", Order = 5)] internal bool SessionPresent { get; set; }
        [DataMember(Name = "installedPatchCount", Order = 6)] internal int InstalledPatchCount { get; set; }
        [DataMember(Name = "fishCompletedCount", Order = 7)] internal long FishCompletedCount { get; set; }
        [DataMember(Name = "pullEnteredCount", Order = 8)] internal long PullEnteredCount { get; set; }
        [DataMember(Name = "pullExitedCount", Order = 9)] internal long PullExitedCount { get; set; }
        [DataMember(Name = "visibleReelConsumed", Order = 10)] internal long VisibleReelConsumed { get; set; }
        [DataMember(Name = "visibleReelNativeAccepted", Order = 11)] internal long VisibleReelNativeAccepted { get; set; }
        [DataMember(Name = "nativeAccessorBuildCount", Order = 12)] internal int NativeAccessorBuildCount { get; set; }
        [DataMember(Name = "nativeAccessorFailureCount", Order = 13)] internal int NativeAccessorFailureCount { get; set; }
        [DataMember(Name = "nativeTransientCount", Order = 14)] internal int NativeTransientCount { get; set; }
        [DataMember(Name = "dtmApiRecordCount", Order = 15)] internal long DtmApiRecordCount { get; set; }
        [DataMember(Name = "ownerRootCount", Order = 16)] internal long OwnerRootCount { get; set; }
        [DataMember(Name = "inputOwnerCount", Order = 17)] internal long InputOwnerCount { get; set; }
        [DataMember(Name = "eventActiveHandlers", Order = 18)] internal long EventActiveHandlers { get; set; }
        [DataMember(Name = "apiRootCount", Order = 19)] internal long ApiRootCount { get; set; }
        [DataMember(Name = "demandEntryCount", Order = 20)] internal long DemandEntryCount { get; set; }
        [DataMember(Name = "totalDemand", Order = 21)] internal long TotalDemand { get; set; }
        [DataMember(Name = "processPrivateBytes", Order = 22)] internal long ProcessPrivateBytes { get; set; }
        [DataMember(Name = "processWorkingSetBytes", Order = 23)] internal long ProcessWorkingSetBytes { get; set; }
        [DataMember(Name = "gen0Collections", Order = 24)] internal int Gen0Collections { get; set; }
        [DataMember(Name = "gen1Collections", Order = 25)] internal int Gen1Collections { get; set; }
        [DataMember(Name = "gen2Collections", Order = 26)] internal int Gen2Collections { get; set; }
        [DataMember(Name = "driverNativeExitCount", Order = 27)] internal long DriverNativeExitCount { get; set; }
        [DataMember(Name = "driverAutoCastConfirmedCount", Order = 28)] internal int DriverAutoCastConfirmedCount { get; set; }
        [DataMember(Name = "driverMiniGameCompleteApplicationCount", Order = 29)] internal int DriverMiniGameCompleteApplicationCount { get; set; }
        [DataMember(Name = "driverFailureCount", Order = 30)] internal int DriverFailureCount { get; set; }
        [DataMember(Name = "driverPatchCount", Order = 31)] internal int DriverPatchCount { get; set; }
        [DataMember(Name = "driverOwnerResourceCount", Order = 32)] internal int DriverOwnerResourceCount { get; set; }
        [DataMember(Name = "driverNativeTransientCount", Order = 33)] internal int DriverNativeTransientCount { get; set; }
        [DataMember(Name = "recoveryPullExitedCount", Order = 34)] internal long RecoveryPullExitedCount { get; set; }
        [DataMember(Name = "recoveryCastAppliedCount", Order = 35)] internal long RecoveryCastAppliedCount { get; set; }
        [DataMember(Name = "recoveryBitePreparedCount", Order = 36)] internal long RecoveryBitePreparedCount { get; set; }
        [DataMember(Name = "recoverySkipReelCount", Order = 37)] internal long RecoverySkipReelCount { get; set; }
        [DataMember(Name = "recoveryOwnerResourceCount", Order = 38)] internal int RecoveryOwnerResourceCount { get; set; }
        [DataMember(Name = "recoveryNativeAccessorFailureCount", Order = 39)] internal int RecoveryNativeAccessorFailureCount { get; set; }
        [DataMember(Name = "processMetricsAvailable", Order = 40)] internal bool ProcessMetricsAvailable { get; set; }
        [DataMember(Name = "processMetricsError", Order = 41)] internal string ProcessMetricsError { get; set; } = string.Empty;
        [DataMember(Name = "castAppliedCount", Order = 42)] internal long CastAppliedCount { get; set; }
        [DataMember(Name = "nativeBitePreparedCount", Order = 43)] internal long NativeBitePreparedCount { get; set; }
        [DataMember(Name = "nativeVisibleReelCount", Order = 44)] internal long NativeVisibleReelCount { get; set; }
        [DataMember(Name = "nativeSkipReelCount", Order = 45)] internal long NativeSkipReelCount { get; set; }
        [DataMember(Name = "visibleReelQueued", Order = 46)] internal long VisibleReelQueued { get; set; }
        [DataMember(Name = "visibleReelRetries", Order = 47)] internal long VisibleReelRetries { get; set; }
        [DataMember(Name = "visibleReelTimeouts", Order = 48)] internal long VisibleReelTimeouts { get; set; }
        [DataMember(Name = "animationApplicationCount", Order = 49)] internal int AnimationApplicationCount { get; set; }
        [DataMember(Name = "readyChargeApplicationCount", Order = 50)] internal int ReadyChargeApplicationCount { get; set; }
        [DataMember(Name = "lastReason", Order = 51)] internal string LastReason { get; set; } = string.Empty;
        [DataMember(Name = "processMetricsSource", Order = 52)] internal string ProcessMetricsSource { get; set; } = string.Empty;
        [DataMember(Name = "instantBiteCommittedCount", Order = 53)] internal long InstantBiteCommittedCount { get; set; }
    }

    [DataContract]
    internal sealed class Batch6AutoFishingCleanupReceipt
    {
        [DataMember(Name = "enabled", Order = 1)] internal bool Enabled { get; set; }
        [DataMember(Name = "updateSubscribed", Order = 2)] internal bool UpdateSubscribed { get; set; }
        [DataMember(Name = "sessionPresent", Order = 3)] internal bool SessionPresent { get; set; }
        [DataMember(Name = "installedPatchCount", Order = 4)] internal int InstalledPatchCount { get; set; }
        [DataMember(Name = "returnedToTitleObserved", Order = 5)] internal bool ReturnedToTitleObserved { get; set; }
        [DataMember(Name = "verified", Order = 6)] internal bool Verified { get; set; }
        [DataMember(Name = "details", Order = 7)] internal string Details { get; set; } = "pending";
        [DataMember(Name = "driverOwner", Order = 8)] internal string DriverOwner { get; set; } = string.Empty;
        [DataMember(Name = "driverOwnerResourceCount", Order = 9)] internal int DriverOwnerResourceCount { get; set; }
        [DataMember(Name = "driverServicePresent", Order = 10)] internal bool DriverServicePresent { get; set; }
        [DataMember(Name = "driverCallbackRuntimePresent", Order = 11)] internal bool DriverCallbackRuntimePresent { get; set; }
        [DataMember(Name = "driverHooksPresent", Order = 12)] internal bool DriverHooksPresent { get; set; }
        [DataMember(Name = "driverPatchCount", Order = 13)] internal int DriverPatchCount { get; set; }
        [DataMember(Name = "recoveryDriverOwner", Order = 14)] internal string RecoveryDriverOwner { get; set; } = string.Empty;
        [DataMember(Name = "recoveryOwnerResourceCount", Order = 15)] internal int RecoveryOwnerResourceCount { get; set; }
        [DataMember(Name = "recoveryActiveSessionCount", Order = 16)] internal int RecoveryActiveSessionCount { get; set; }
        [DataMember(Name = "recoveryInputLeaseCount", Order = 17)] internal int RecoveryInputLeaseCount { get; set; }
        [DataMember(Name = "recoveryAnimationLeaseCount", Order = 18)] internal int RecoveryAnimationLeaseCount { get; set; }
        [DataMember(Name = "nativeTransientCount", Order = 19)] internal int NativeTransientCount { get; set; }
        [DataMember(Name = "recoverySchedulerPending", Order = 20)] internal bool RecoverySchedulerPending { get; set; }
    }

    internal sealed class Batch6AutoFishingPilotEvidenceWriter
    {
        private readonly string directory;
        private readonly string resultPath;
        private readonly Batch6AutoFishingPilotEvidence evidence;

        internal Batch6AutoFishingPilotEvidenceWriter(
            string runtimeEvidenceRoot,
            string runId,
            Batch6AutoFishingPilotSettings settings,
            DateTimeOffset startedAtUtc)
        {
            if (string.IsNullOrWhiteSpace(runtimeEvidenceRoot) || !Path.IsPathRooted(runtimeEvidenceRoot))
                throw new InvalidDataException("Batch6AutoFishingPilot requires an absolute Runtime evidence root.");
            if (string.IsNullOrWhiteSpace(runId) || runId.Any(character => !(char.IsLetterOrDigit(character) || character == '-' || character == '_')))
                throw new InvalidDataException("Batch6AutoFishingPilot runId is not safe for an evidence directory.");
            directory = Path.Combine(Path.GetFullPath(runtimeEvidenceRoot), "AUTO-FISHING-PERF", runId);
            resultPath = Path.Combine(directory, "auto-fishing-performance.json");
            evidence = new Batch6AutoFishingPilotEvidence
            {
                RunId = runId,
                Level = settings.Level,
                Scenario = settings.Scenario,
                MeasureSeconds = settings.MeasureSeconds,
                SampleSeconds = settings.SampleSeconds,
                WarmupFish = settings.WarmupFish,
                TargetFish = settings.TargetFish,
                Multiplier = settings.Multiplier,
                ForcedGc = false,
                StartedAtUtc = Format(startedAtUtc),
                ExpectedPackageSha256 = settings.ExpectedPackageSha256,
                ExpectedEntryDllSha256 = settings.ExpectedEntryDllSha256,
                ExpectedManifestSha256 = settings.ExpectedManifestSha256,
                ExpectedReferencePolicySha256 = settings.ExpectedReferencePolicySha256,
                ProductAssemblyReferenced = false,
                Formal = settings.Formal,
                Authoritative = settings.IsFormalContract,
                Authority = settings.IsFormalContract ? "formal" : "non-authoritative",
                ContractKind = settings.FormalContractKind,
                AuthorityScope = settings.AuthorityScope,
                CastChargeRatio = settings.CastChargeRatio,
                ManualMovementCancel = settings.ManualMovementCancel,
                ToggleKey = settings.ToggleKey,
                ManualMovementTargetPhases = settings.ManualMovementCancel
                    ? Batch6AutoFishingPilotSettings.ManualMovementTargetPhases.ToArray()
                    : Array.Empty<string>(),
                ObserverCadenceMilliseconds = settings.ManualMovementCancel
                    ? 0
                    : Batch6AutoFishingPilotSettings.ObservationCadenceMilliseconds,
                ObserverEffect = settings.ManualMovementCancel
                    ? "QA observes every host update only for the bounded per-phase movement contract so transient target phases cannot be skipped; product updates remain native."
                    : "QA reflection/counter snapshots are bounded to the declared cadence; native product/compatibility updaters continue on their real runtime cadence"
            };
        }

        internal Batch6AutoFishingPilotEvidence Evidence => evidence;
        internal string ResultPath => resultPath;

        internal void BindPackage(Batch6AutoFishingPackageProvenance package)
        {
            evidence.Package = package ?? throw new ArgumentNullException(nameof(package));
            evidence.PackageSha256 = package.PackageSha256;
            evidence.EntryDllSha256 = package.EntryDllSha256;
            evidence.ManifestSha256 = package.ManifestSha256;
            evidence.ReferencePolicySha256 = package.ReferencePolicySha256;
            Write();
        }

        internal void BindDriver(Batch6AutoFishingCompatibilityDriverSnapshot snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));
            evidence.DriverKind = snapshot.DriverKind;
            evidence.DriverOwner = snapshot.DriverOwner;
            evidence.DriverPatchCount = snapshot.PatchCount;
            Write();
        }

        internal void BindProductDriver()
        {
            evidence.DriverKind = "AdvancedProduct";
            evidence.DriverOwner = Batch6AutoFishingPilotSettings.ProductUniqueId;
            evidence.DriverPatchCount = Batch6AutoFishingPilotSettings.ExpectedProductPatchCount;
            Write();
        }

        internal void BindRecoveryDriver(Batch6AutoFishingProductRecoverySnapshot snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));
            evidence.RecoveryDriverKind = snapshot.DriverKind;
            evidence.RecoveryDriverOwner = snapshot.DriverOwner;
            Write();
        }

        internal void SetDriverProgress(long warmupUnits, long measuredUnits, bool verified)
        {
            evidence.DriverWarmupUnits = Math.Max(0L, warmupUnits);
            evidence.DriverMeasuredUnits = Math.Max(0L, measuredUnits);
            evidence.DriverNativeProgress = verified;
            Write();
        }

        internal void SetTrailingNativeProgress(DateTimeOffset lastProgressAtUtc, double gapSeconds, int windowSeconds, bool verified)
        {
            evidence.LastNativeProgressAtUtc = Format(lastProgressAtUtc);
            evidence.TrailingNativeProgressGapSeconds = Math.Max(0d, gapSeconds);
            evidence.NativeProgressTrailingWindowSeconds = Math.Max(0, windowSeconds);
            evidence.NativeProgressTrailingWindowVerified = verified;
            Write();
        }

        internal void SetRecoveryDriverProgress(long units, bool verified)
        {
            evidence.RecoveryDriverUnits = Math.Max(0L, units);
            evidence.RecoveryDriverNativeProgress = verified;
            Write();
        }

        internal void RecordBehaviorBaseline(Batch6AutoFishingObservation observation)
        {
            evidence.Behavior = new Batch6AutoFishingBehaviorReceipt
            {
                Scenario = evidence.Scenario,
                CastChargeRatio = evidence.CastChargeRatio,
                ManualMovementCancel = evidence.ManualMovementCancel,
                ToggleKey = evidence.ToggleKey,
                Baseline = observation ?? throw new ArgumentNullException(nameof(observation)),
                PhysicalMovementHandshakeRequired = evidence.ManualMovementCancel,
                Details = "active baseline captured before the formal behavior action window"
            };
            Write();
        }

        internal void RecordBehaviorResult(Batch6AutoFishingObservation observation, bool verified, bool fullNativeLoopVerified, string details)
        {
            Batch6AutoFishingObservation baseline = evidence.Behavior.Baseline
                ?? throw new InvalidDataException("A formal behavior result cannot be recorded without an active baseline.");
            evidence.Behavior.Final = observation ?? throw new ArgumentNullException(nameof(observation));
            evidence.Behavior.Verified = verified;
            evidence.Behavior.FullNativeLoopVerified = fullNativeLoopVerified;
            evidence.Behavior.CastAppliedDelta = observation.CastAppliedCount - baseline.CastAppliedCount;
            evidence.Behavior.PullEnteredDelta = observation.PullEnteredCount - baseline.PullEnteredCount;
            evidence.Behavior.PullExitedDelta = observation.PullExitedCount - baseline.PullExitedCount;
            evidence.Behavior.NativeBitePreparedDelta = observation.NativeBitePreparedCount - baseline.NativeBitePreparedCount;
            evidence.Behavior.InstantBiteCommittedDelta = observation.InstantBiteCommittedCount - baseline.InstantBiteCommittedCount;
            evidence.Behavior.NativeVisibleReelDelta = observation.NativeVisibleReelCount - baseline.NativeVisibleReelCount;
            evidence.Behavior.NativeSkipReelDelta = observation.NativeSkipReelCount - baseline.NativeSkipReelCount;
            evidence.Behavior.VisibleReelQueuedDelta = observation.VisibleReelQueued - baseline.VisibleReelQueued;
            evidence.Behavior.VisibleReelConsumedDelta = observation.VisibleReelConsumed - baseline.VisibleReelConsumed;
            evidence.Behavior.VisibleReelNativeAcceptedDelta = observation.VisibleReelNativeAccepted - baseline.VisibleReelNativeAccepted;
            evidence.Behavior.VisibleReelRetryDelta = observation.VisibleReelRetries - baseline.VisibleReelRetries;
            evidence.Behavior.VisibleReelTimeoutDelta = observation.VisibleReelTimeouts - baseline.VisibleReelTimeouts;
            evidence.Behavior.AnimationApplicationDelta = observation.AnimationApplicationCount - baseline.AnimationApplicationCount;
            evidence.Behavior.ReadyChargeApplicationDelta = observation.ReadyChargeApplicationCount - baseline.ReadyChargeApplicationCount;
            evidence.Behavior.ManualMovementReason = evidence.ManualMovementCancel ? observation.LastReason : string.Empty;
            evidence.Behavior.Details = details ?? string.Empty;
            Write();
        }

        internal void RecordManualMovementPhase(string targetPhase, string observedPhase, Batch6AutoFishingObservation observation)
        {
            if (observation == null)
                throw new ArgumentNullException(nameof(observation));
            evidence.Behavior.MovementPhaseReceipts.Add(new Batch6AutoFishingMovementPhaseReceipt
            {
                Sequence = evidence.Behavior.MovementPhaseReceipts.Count + 1,
                TargetPhase = targetPhase ?? string.Empty,
                ObservedPhase = observedPhase ?? string.Empty,
                ReleaseReason = observation.LastReason,
                InactiveCleanupVerified = !observation.Enabled && !observation.UpdateSubscribed && !observation.SessionPresent && observation.NativeTransientCount == 0,
                Details = "Physical movement was sent only after the target-phase handshake; the product then exposed its exact movement release reason and zero-transient inactive state."
            });
            Write();
        }

        internal void RecordInitialState(Batch6AutoFishingObservation observation)
        {
            evidence.InitialProductState = observation;
            evidence.ProductPresent = observation.ProductPresent;
            evidence.CoreResidentProductInstanceObserved = observation.ProductPresent;
            evidence.ProductAbsentObserved = !observation.ProductPresent && !observation.ProductAssemblyLoaded;
            evidence.ProductAssemblyReferenced = observation.QaHasStaticProductAssemblyRef;
            Write();
        }

        internal void RecordStage(
            string state,
            string status,
            string details,
            int saveLoadOrdinal,
            Batch6AutoFishingObservation? productState,
            DateTimeOffset observedAtUtc)
        {
            var stage = new Batch6AutoFishingPilotStageReceipt
            {
                Index = evidence.Stages.Count,
                ObservedAtUtc = Format(observedAtUtc),
                State = state ?? string.Empty,
                Status = status ?? string.Empty,
                Details = details ?? string.Empty,
                SaveLoadOrdinal = saveLoadOrdinal,
                ProductState = productState,
                DriverKind = evidence.DriverKind,
                DriverPatchCount = evidence.DriverPatchCount,
                RecoveryDriverKind = evidence.RecoveryDriverKind,
                PackageSha256 = evidence.PackageSha256,
                EntryDllSha256 = evidence.EntryDllSha256,
                ManifestSha256 = evidence.ManifestSha256,
                ReferencePolicySha256 = evidence.ReferencePolicySha256,
                ExpectedPackageSha256 = evidence.ExpectedPackageSha256,
                ExpectedEntryDllSha256 = evidence.ExpectedEntryDllSha256,
                ExpectedManifestSha256 = evidence.ExpectedManifestSha256,
                ExpectedReferencePolicySha256 = evidence.ExpectedReferencePolicySha256,
                Metrics = evidence.Samples.Count == 0 ? null : evidence.Samples[evidence.Samples.Count - 1],
                Cleanup = evidence.Cleanup
            };
            evidence.Stages.Add(stage);
            WriteStage(stage);
            Write();
        }

        internal void AddSample(Batch6AutoFishingPilotSample sample)
        {
            evidence.Samples.Add(sample ?? throw new ArgumentNullException(nameof(sample)));
            Write();
        }

        internal void RecordNativeFishingContext(Batch6AutoFishingNativeFishingContextReceipt receipt)
        {
            if (receipt == null)
                throw new ArgumentNullException(nameof(receipt));
            evidence.NativeFishingContexts.Add(receipt);
            evidence.NativeFishingContextVerified = evidence.NativeFishingContexts.All(item => item.Verified);
            Write();
        }

        internal void RecordNativeSurfaceDiagnostic(Batch6AutoFishingNativeSurfaceReceipt receipt)
        {
            if (receipt == null)
                throw new ArgumentNullException(nameof(receipt));
            evidence.NativeSurfaceDiagnostics.Add(receipt);
            Write();
        }

        internal void RecordNativeVitals(global::DTMAPI.GameBridge.DolocTown.AutoFishingNativeVitalsReceipt source)
        {
            Batch6AutoFishingNativeVitalsReceipt receipt = Batch6AutoFishingNativeVitalsEvidenceMapper.From(source);
            evidence.NativeVitals.Receipts.Add(receipt);
            if (string.Equals(receipt.Kind, "WorkloadStart", StringComparison.Ordinal))
                evidence.NativeVitals.WorkloadStartCount++;
            else if (string.Equals(receipt.Kind, "MaintenanceRefill", StringComparison.Ordinal))
                evidence.NativeVitals.MaintenanceCount++;
            else if (string.Equals(receipt.Kind, "L4RecoveryCheckpoint", StringComparison.Ordinal))
                evidence.NativeVitals.L4RecoveryCheckpointCount++;
            else if (string.Equals(receipt.Kind, "FinalReadback", StringComparison.Ordinal))
                evidence.NativeVitals.FinalReadbackCount++;
            if (receipt.NativeEnergyInsufficientObserved)
                evidence.NativeVitals.EnergyInsufficientObservationCount++;
            if (receipt.NativeEnergyReserveLowObserved)
                evidence.NativeVitals.EnergyReserveLowObservationCount++;
            if (receipt.NativeSpiritLowObserved)
                evidence.NativeVitals.SpiritLowObservationCount++;
            evidence.NativeVitals.ReadbackVerified = evidence.NativeVitals.Receipts.All(item => item.ReadbackVerified);
            Write();
        }

        internal bool FinalizeNativeEvidence(int requiredWorkloads, int requiredFinalReadbacks, bool requireL4RecoveryCheckpoint)
        {
            bool contexts = evidence.NativeFishingContexts.Count == requiredWorkloads &&
                evidence.NativeFishingContexts.All(item => item.Verified);
            bool vitals = evidence.NativeVitals.WorkloadStartCount == requiredWorkloads &&
                evidence.NativeVitals.FinalReadbackCount == requiredFinalReadbacks &&
                evidence.NativeVitals.L4RecoveryCheckpointCount == (requireL4RecoveryCheckpoint ? 1 : 0) &&
                evidence.NativeVitals.EnergyInsufficientObservationCount == 0 &&
                evidence.NativeVitals.Receipts.Count > 0 &&
                evidence.NativeVitals.Receipts.All(item => item.ReadbackVerified);
            evidence.NativeFishingContextVerified = contexts;
            evidence.NativeVitals.ReadbackVerified = evidence.NativeVitals.Receipts.Count > 0 &&
                evidence.NativeVitals.Receipts.All(item => item.ReadbackVerified);
            evidence.NativeVitals.Verified = vitals;
            Write();
            return contexts && vitals;
        }

        internal void SetCleanup(Batch6AutoFishingObservation? state, bool titleObserved, bool verified, string details)
        {
            evidence.FinalProductState = state;
            evidence.Cleanup = new Batch6AutoFishingCleanupReceipt
            {
                Enabled = state?.Enabled ?? false,
                UpdateSubscribed = state?.UpdateSubscribed ?? false,
                SessionPresent = state?.SessionPresent ?? false,
                InstalledPatchCount = state?.InstalledPatchCount ?? 0,
                NativeTransientCount = state?.NativeTransientCount ?? 0,
                ReturnedToTitleObserved = titleObserved,
                Verified = verified,
                Details = details ?? string.Empty
            };
            Write();
        }

        internal void SetCompatibilityCleanup(
            Batch6AutoFishingObservation productState,
            Batch6AutoFishingCompatibilityDriverCleanup cleanup,
            bool titleObserved)
        {
            if (productState == null)
                throw new ArgumentNullException(nameof(productState));
            if (cleanup == null)
                throw new ArgumentNullException(nameof(cleanup));
            evidence.FinalProductState = productState;
            evidence.Cleanup = new Batch6AutoFishingCleanupReceipt
            {
                Enabled = false,
                UpdateSubscribed = false,
                SessionPresent = false,
                InstalledPatchCount = 0,
                NativeTransientCount = productState.NativeTransientCount,
                ReturnedToTitleObserved = titleObserved,
                Verified = cleanup.Verified && !productState.ProductPresent,
                Details = cleanup.Details,
                DriverOwner = evidence.DriverOwner,
                DriverOwnerResourceCount = cleanup.OwnerResourceCount,
                DriverServicePresent = cleanup.ServicePresent,
                DriverCallbackRuntimePresent = cleanup.CallbackRuntimePresent,
                DriverHooksPresent = cleanup.HooksPresent,
                DriverPatchCount = cleanup.PatchCount
            };
            Write();
        }

        internal void SetProductRecoveryCleanup(
            Batch6AutoFishingObservation productState,
            Batch6AutoFishingProductRecoveryCleanup cleanup,
            bool titleObserved)
        {
            if (productState == null)
                throw new ArgumentNullException(nameof(productState));
            if (cleanup == null)
                throw new ArgumentNullException(nameof(cleanup));
            evidence.FinalProductState = productState;
            evidence.Cleanup = new Batch6AutoFishingCleanupReceipt
            {
                Enabled = productState.Enabled,
                UpdateSubscribed = productState.UpdateSubscribed,
                SessionPresent = productState.SessionPresent,
                InstalledPatchCount = productState.InstalledPatchCount,
                NativeTransientCount = productState.NativeTransientCount,
                ReturnedToTitleObserved = titleObserved,
                Verified = cleanup.Verified && !productState.Enabled && !productState.UpdateSubscribed && !productState.SessionPresent,
                Details = cleanup.Details,
                RecoveryDriverOwner = cleanup.DriverOwner,
                RecoveryOwnerResourceCount = cleanup.OwnerResourceCount,
                RecoveryActiveSessionCount = cleanup.ActiveSessionCount,
                RecoveryInputLeaseCount = cleanup.ActiveInputLeaseCount,
                RecoveryAnimationLeaseCount = cleanup.ActiveAnimationLeaseCount,
                RecoverySchedulerPending = cleanup.SchedulerPending
            };
            Write();
        }

        internal void Complete(string status, string failureCode, string failureReason, DateTimeOffset completedAtUtc)
        {
            evidence.Status = status ?? string.Empty;
            evidence.FailureCode = failureCode ?? string.Empty;
            evidence.FailureReason = failureReason ?? string.Empty;
            evidence.CompletedAtUtc = Format(completedAtUtc);
            Write();
        }

        internal void Write()
        {
            Directory.CreateDirectory(directory);
            Serialize(resultPath, evidence, typeof(Batch6AutoFishingPilotEvidence));
        }

        private void WriteStage(Batch6AutoFishingPilotStageReceipt stage)
        {
            Directory.CreateDirectory(directory);
            string name = "stage-" + stage.Index.ToString("D3", CultureInfo.InvariantCulture) + "-" + SafeName(stage.State) + ".json";
            Serialize(Path.Combine(directory, name), stage, typeof(Batch6AutoFishingPilotStageReceipt));
        }

        private static void Serialize(string path, object value, Type type)
        {
            using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read))
                new DataContractJsonSerializer(type).WriteObject(stream, value);
        }

        private static string Format(DateTimeOffset value) => value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

        private static string SafeName(string value)
        {
            string safe = new string((value ?? string.Empty)
                .Select(character => char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : '-')
                .ToArray()).Trim('-');
            return safe.Length == 0 ? "stage" : safe;
        }
    }
}
