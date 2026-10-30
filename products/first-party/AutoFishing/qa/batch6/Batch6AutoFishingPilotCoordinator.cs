using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using DTMAPI.Core.Manifesting;

namespace DTMAPI.GameBridge.DolocTown.QA
{
    internal sealed class Batch6AutoFishingPilotCoordinator
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct ProcessMemoryCountersEx
        {
            internal uint Cb;
            internal uint PageFaultCount;
            internal UIntPtr PeakWorkingSetSize;
            internal UIntPtr WorkingSetSize;
            internal UIntPtr QuotaPeakPagedPoolUsage;
            internal UIntPtr QuotaPagedPoolUsage;
            internal UIntPtr QuotaPeakNonPagedPoolUsage;
            internal UIntPtr QuotaNonPagedPoolUsage;
            internal UIntPtr PagefileUsage;
            internal UIntPtr PeakPagefileUsage;
            internal UIntPtr PrivateUsage;
        }

        [DllImport("psapi.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetProcessMemoryInfo(
            IntPtr process,
            ref ProcessMemoryCountersEx counters,
            uint size);

        private enum PilotState
        {
            Created,
            WaitingForSaveLoaded,
            WaitingForNativeFishingContext,
            L0WarmingUp,
            L0Measuring,
            WaitingForInitialDirectNeutralPreflight,
            WaitingForInitialEnable,
            WaitingForManualMovementCancel,
            WaitingForManualMovementReenable,
            WarmingUp,
            Measuring,
            WaitingForInitialDisable,
            L4Recovering,
            WaitingForTitle,
            WaitingForReentry,
            WaitingForReentryDirectNeutralPreflight,
            WaitingForReentryEnable,
            ReentryActive,
            WaitingForFinalDisable,
            Passed,
            Blocked,
            Failed
        }

        private readonly GameBridgeFixtureAccess access;
        private readonly Batch6AutoFishingPilotSettings settings;
        private readonly Batch6AutoFishingReflectionObserver observer = new Batch6AutoFishingReflectionObserver();
        private readonly Batch6AutoFishingPilotEvidenceWriter writer;
        private Batch6AutoFishingCompatibilityDriver? compatibilityDriver;
        private Batch6AutoFishingProductRecoveryDriver? productRecoveryDriver;
        private global::DTMAPI.GameBridge.DolocTown.AutoFishingNativeVitalsCommandAdapter? nativeVitals;
        private PilotState state = PilotState.Created;
        private DateTimeOffset stateEnteredAtUtc;
        private DateTimeOffset measurementStartedAtUtc;
        private DateTimeOffset nextSampleAtUtc;
        private DateTimeOffset nextObservationAtUtc;
        private DateTimeOffset nextNativeVitalsMaintenanceAtUtc;
        private DateTimeOffset lastMeasurementNativeProgressAtUtc;
        private long warmupStartFish;
        private long measurementStartFish;
        private long lastMeasurementNativeProgress;
        private long reentryStartFish;
        private long l0WarmupStartNativeExit;
        private int l0WarmupStartAutoCastConfirmed;
        private long l0MeasurementStartNativeExit;
        private int l0MeasurementStartAutoCastConfirmed;
        private int l0MeasurementStartMiniGameComplete;
        private int l0MeasurementStartFailureCount;
        private Batch6AutoFishingObservation? behaviorBaseline;
        private int discardedNonProductInitialLoops;
        private bool manualMovementHandshakePublished;
        private int manualMovementTargetIndex;
        private string manualMovementObservedPhase = string.Empty;
        private int saveLoadOrdinal;
        private bool returnedToTitleObserved;
        private bool titleReturnReleasePendingLogged;
        private Batch6AutoFishingObservation? pendingSaveLoadedObservation;
        private Batch6AutoFishingNativeFishingContextReceipt? pendingNativeFishingContext;
        private Batch6AutoFishingDirectNeutralPreflightGate? directNeutralPreflightGate;
        private string terminalDetails = string.Empty;

        internal Batch6AutoFishingPilotCoordinator(GameBridgeFixtureAccess access, Batch6AutoFishingPilotSettings settings)
        {
            this.access = access ?? throw new ArgumentNullException(nameof(access));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            DateTimeOffset now = DateTimeOffset.UtcNow;
            stateEnteredAtUtc = now;
            writer = new Batch6AutoFishingPilotEvidenceWriter(access.RuntimeEvidenceRoot, access.RunId, settings, now);
        }

        internal bool Enabled => settings.Enabled;
        internal bool Terminal => state == PilotState.Passed || state == PilotState.Blocked || state == PilotState.Failed;
        internal bool TerminalSucceeded => state == PilotState.Passed;
        internal bool ShouldRequestReturnHome
        {
            get
            {
                if (state != PilotState.WaitingForTitle)
                    return false;
                Batch6AutoFishingObservation observation = observer.Observe(access.Runtime, DateTimeOffset.UtcNow);
                ValidateProductObservation(observation, requireActive: false, requireInactive: true);
                if (!observation.ToggleAwaitingRelease)
                    return true;
                if (!titleReturnReleasePendingLogged)
                {
                    titleReturnReleasePendingLogged = true;
                    access.Log("Batch6 AutoFishing L5 is waiting for the completed configured-toggle release transaction before QA requests ReturnHome.");
                }
                return false;
            }
        }
        internal bool ShouldRequestReentryLoad => state == PilotState.WaitingForReentry;
        internal string ResultPath => writer.ResultPath;

        internal void Start()
        {
            if (!Enabled)
                return;
            try
            {
                DateTimeOffset now = DateTimeOffset.UtcNow;
                access.SetHookStatus(
                    Batch6AutoFishingPilotSettings.OverallHookId,
                    "pending",
                    "product-owned optional QA linked source",
                    Details("starting"));
                access.SetHookStatus(
                    Batch6AutoFishingPilotSettings.CleanupHookId,
                    "pending",
                    "product-owned optional QA cleanup observer",
                    Details("waiting-for-product-inactive-terminal"));

                Batch6AutoFishingObservation observation = observer.Observe(access.Runtime, now);
                writer.RecordInitialState(observation);
                DiscoveredMod[] loaded = FindLoadedProducts();
                if (settings.ProductMustBeAbsent)
                {
                    if (observation.ProductPresent || observation.ProductAssemblyLoaded || loaded.Length != 0)
                    {
                        Fail("l0-product-present", "L0 requires the AutoFishing product package, loaded assembly, and Core-resident ModEntry to be absent.", observation, now);
                        return;
                    }
                    MoveTo(PilotState.WaitingForSaveLoaded, "L0 product absence verified; waiting for the authoritative fifth SaveLoaded boundary.", observation, now);
                    return;
                }

                if (!observation.ProductPresent)
                {
                    Fail("product-instance-missing", "Core DtmApiRuntime.modInstances does not contain the required AutoFishing ModEntry.", observation, now);
                    return;
                }
                if (loaded.Length != 1)
                {
                    Fail("loaded-product-cardinality", "Expected exactly one loaded AutoFishing package, observed " + loaded.Length.ToString(CultureInfo.InvariantCulture) + ".", observation, now);
                    return;
                }
                ValidateCoreClassification(loaded[0]);
                Batch6AutoFishingPackageProvenance package = Batch6AutoFishingPackageVerifier.Verify(loaded[0].RootPath, settings);
                writer.BindPackage(package);
                writer.BindProductDriver();
                ValidateProductObservation(observation, requireActive: false, requireInactive: true);
                ValidateScenarioConfig(observation);
                MoveTo(PilotState.WaitingForSaveLoaded, "Core-resident product instance and receipt-bound Advanced package verified; waiting for fifth SaveLoaded.", observation, now);
            }
            catch (Exception ex)
            {
                FailFromException("pilot-start-failed", ex);
            }
        }

        internal G4FixtureStepResult Advance()
        {
            if (!Enabled)
                return G4FixtureStepResult.Failed("Batch6AutoFishingPilot was selected without Enabled=true.");
            if (state == PilotState.Passed)
                return G4FixtureStepResult.Verified(terminalDetails);
            if (state == PilotState.Blocked || state == PilotState.Failed)
                return G4FixtureStepResult.Failed(terminalDetails);

            try
            {
                DateTimeOffset now = DateTimeOffset.UtcNow;
                CheckStateTimeout(now);
                if (state == PilotState.WaitingForSaveLoaded || state == PilotState.WaitingForReentry || state == PilotState.WaitingForTitle)
                    return G4FixtureStepResult.Pending(Details(StateName(state)));

                if (IsNativeWorkloadActive(state))
                    MaintainNativeVitals(now);

                if (now < nextObservationAtUtc)
                    return G4FixtureStepResult.Pending(Details(StateName(state) + "; bounded-observer-wait"));
                nextObservationAtUtc = settings.ManualMovementCancel || IsDirectNeutralPreflightState(state)
                    ? now
                    : now.AddMilliseconds(Batch6AutoFishingPilotSettings.ObservationCadenceMilliseconds);

                if (state == PilotState.WaitingForNativeFishingContext)
                {
                    AdvanceNativeFishingContext(now);
                    return CurrentStepResult();
                }

                if (state == PilotState.L0WarmingUp || state == PilotState.L0Measuring)
                {
                    AdvanceProductAbsentL0(now);
                    return CurrentStepResult();
                }

                if (state == PilotState.L4Recovering)
                {
                    AdvanceProductRecovery(now);
                    return CurrentStepResult();
                }

                bool directNeutralPreflight = IsDirectNeutralPreflightState(state);
                Batch6AutoFishingObservation observation = observer.Observe(
                    access.Runtime,
                    now,
                    refreshInactiveNativeMovement: directNeutralPreflight);
                ValidateProductObservation(observation, requireActive: false, requireInactive: directNeutralPreflight);
                switch (state)
                {
                    case PilotState.WaitingForInitialDirectNeutralPreflight:
                        AdvanceDirectNeutralPreflight(observation, now, reentry: false);
                        break;

                    case PilotState.WaitingForInitialEnable:
                        if (!IsActive(observation))
                            return G4FixtureStepResult.Pending(Details("awaiting-initial-enable-toggle"));
                        ValidateScenarioConfig(observation);
                        if (settings.IsFormalBehaviorContract && settings.ManualMovementCancel)
                        {
                            if (behaviorBaseline == null)
                            {
                                behaviorBaseline = observation;
                                writer.RecordBehaviorBaseline(observation);
                            }
                            measurementStartedAtUtc = now;
                            manualMovementTargetIndex = 0;
                            manualMovementHandshakePublished = false;
                            manualMovementObservedPhase = string.Empty;
                            CaptureSample(observation, now, force: true);
                            MoveTo(PilotState.WaitingForManualMovementCancel, "Real ModEntry is active through rebound F7; waiting for the first exact movement target phase before authorizing physical A.", observation, now);
                            break;
                        }
                        warmupStartFish = observation.FishCompletedCount;
                        MoveTo(PilotState.WarmingUp, "Initial configured-toggle enable observed from the real ModEntry; waiting for warm-up fish.", observation, now);
                        if (settings.WarmupFish == 0)
                            BeginMeasurement(observation, now);
                        break;

                    case PilotState.WaitingForManualMovementCancel:
                        if (IsActive(observation))
                        {
                            string targetPhase = CurrentManualMovementTarget();
                            if (!manualMovementHandshakePublished && MatchesManualMovementTarget(targetPhase, observation.SessionPhase))
                            {
                                manualMovementHandshakePublished = true;
                                manualMovementObservedPhase = observation.SessionPhase;
                                PublishHandshake(
                                    "awaiting-manual-movement-" + MovementPhaseSlug(targetPhase) + "-a",
                                    "targetPhase=" + targetPhase + "; observedPhase=" + observation.SessionPhase + "; activeSession=true");
                            }
                            return G4FixtureStepResult.Pending(Details(manualMovementHandshakePublished
                                ? "awaiting-physical-a-cancel-for-" + targetPhase
                                : "awaiting-target-phase-" + targetPhase));
                        }
                        if (!manualMovementHandshakePublished)
                            throw new InvalidDataException("The product became inactive before the formal target-phase movement handshake was published. target=" + CurrentManualMovementTarget() + ".");
                        if (!IsInactive(observation))
                            throw new InvalidDataException("Physical movement changed product state without reaching exact inactive/zero-transient cleanup.");
                        if (!IsExactMovementReleaseReason(observation.LastReason))
                            throw new InvalidDataException("Physical A did not disable AutoFishing through an exact manual/native movement boundary. lastReason=" + observation.LastReason + ".");
                        string completedTarget = CurrentManualMovementTarget();
                        writer.RecordManualMovementPhase(completedTarget, manualMovementObservedPhase, observation);
                        CaptureSample(observation, now, force: true);
                        manualMovementTargetIndex++;
                        if (manualMovementTargetIndex < Batch6AutoFishingPilotSettings.ManualMovementTargetPhases.Length)
                        {
                            string nextTarget = CurrentManualMovementTarget();
                            manualMovementHandshakePublished = false;
                            manualMovementObservedPhase = string.Empty;
                            MoveTo(PilotState.WaitingForManualMovementReenable, "Exact " + completedTarget + " movement cancellation and cleanup verified; waiting for rebound F7 before " + nextTarget + ".", observation, now);
                            PublishHandshake("awaiting-manual-movement-" + MovementPhaseSlug(nextTarget) + "-reenable", "completedTarget=" + completedTarget + "; nextTarget=" + nextTarget + "; toggleKey=" + settings.ToggleKey);
                            break;
                        }
                        writer.RecordBehaviorResult(observation, verified: true, fullNativeLoopVerified: false,
                            "Physical A followed five target-phase handshakes; each Ready/Cast/Wait/BiteReady/MiniGame receipt retained its observed phase, exact movement reason, and zero-transient cleanup, with rebound F7 between targets; discardedNonProductInitialLoops=0");
                        writer.SetDriverProgress(0, 0, verified: true);
                        ObserveNativeVitalsFinal("manual-movement-cancel-end");
                        writer.SetCleanup(observation, returnedToTitleObserved, verified: true,
                            "The fifth physical A disabled the real ModEntry and released its updater/session/transient leases; all five phase receipts were already verified and product-owned patches remain installed as inactive callbacks.");
                        access.SetHookStatus(
                            Batch6AutoFishingPilotSettings.CleanupHookId,
                            "verified",
                            "formal per-phase manual-movement cleanup",
                            Details("phases=" + string.Join("|", Batch6AutoFishingPilotSettings.ManualMovementTargetPhases) + "; enabled=false; updateSubscribed=false; sessionPresent=false; nativeTransientCount=0; lastReason=" + observation.LastReason));
                        Pass("Formal manual movement behavior verified Ready/Cast/Wait/BiteReady/MiniGame as five independent physical-input receipts plus the actual rebound F7 path.", observation, now);
                        break;

                    case PilotState.WaitingForManualMovementReenable:
                        if (!IsActive(observation))
                            return G4FixtureStepResult.Pending(Details("awaiting-rebound-f7-for-" + CurrentManualMovementTarget()));
                        ValidateScenarioConfig(observation);
                        MoveTo(PilotState.WaitingForManualMovementCancel, "Rebound F7 re-enabled the real ModEntry; waiting for target phase " + CurrentManualMovementTarget() + ".", observation, now);
                        break;

                    case PilotState.WarmingUp:
                        RequireActive(observation, "warm-up");
                        if (observation.FishCompletedCount - warmupStartFish >= settings.WarmupFish)
                            BeginMeasurement(observation, now);
                        break;

                    case PilotState.Measuring:
                        RequireActive(observation, "measurement");
                        TrackMeasurementNativeProgress(observation.FishCompletedCount, now);
                        if (now >= nextSampleAtUtc)
                        {
                            CaptureSample(observation, now);
                            nextSampleAtUtc = now.AddSeconds(settings.SampleSeconds);
                        }
                        if (TryRebaselineFormalBehaviorAfterNonProductInitialLoop(observation, now))
                            break;
                        if (now - measurementStartedAtUtc >= TimeSpan.FromSeconds(settings.MeasureSeconds) &&
                            observation.FishCompletedCount - measurementStartFish >= settings.TargetFish)
                        {
                            if (settings.IsFormalLongRunContract)
                                RequireTrailingNativeProgress(now);
                            CaptureSample(observation, now, force: true);
                            if (settings.IsFormalBehaviorContract)
                                ValidateAndRecordBehaviorResult(observation);
                            ObserveNativeVitalsFinal("initial-measurement-end");
                            writer.SetDriverProgress(
                                measurementStartFish - warmupStartFish,
                                observation.FishCompletedCount - measurementStartFish,
                                verified: true);
                            MoveTo(PilotState.WaitingForInitialDisable, "Active measurement completed; waiting for the runner's configured toggle key.", observation, now);
                            PublishHandshake("awaiting-disable-toggle", "measurement-complete; toggleKey=" + settings.ToggleKey);
                        }
                        break;

                    case PilotState.WaitingForInitialDisable:
                        if (!IsInactive(observation))
                            return G4FixtureStepResult.Pending(Details("awaiting-disable-toggle"));
                        writer.SetCleanup(observation, returnedToTitleObserved, verified: true, "Real ModEntry disabled its updater and released its session; product-owned patches remain installed as inactive callbacks.");
                        access.SetHookStatus(
                            Batch6AutoFishingPilotSettings.CleanupHookId,
                            "verified",
                            "real ModEntry reflected cleanup",
                            Details("enabled=false; updateSubscribed=false; sessionPresent=false; patches=" + observation.InstalledPatchCount));
                        if (string.Equals(settings.Level, "L4", StringComparison.Ordinal))
                        {
                            productRecoveryDriver ??= new Batch6AutoFishingProductRecoveryDriver(access, saveLoadOrdinal);
                            if (!productRecoveryDriver.IsEntryReady(out string entryDetails))
                            {
                                access.SetHookStatus(
                                    Batch6AutoFishingPilotSettings.CleanupHookId,
                                    "pending",
                                    "L4 QA recovery entry gate",
                                    Details(entryDetails));
                                return G4FixtureStepResult.Pending(Details("awaiting-l4-normal-game-state; " + entryDetails));
                            }
                            PrepareL4NativeVitalsCheckpoint();
                            Batch6AutoFishingProductRecoverySnapshot recovery = productRecoveryDriver.Start();
                            writer.BindRecoveryDriver(recovery);
                            CaptureRecoverySample(observation, recovery, now);
                            MoveTo(PilotState.L4Recovering, "Real ModEntry configured-toggle cleanup verified; a distinct QA owner acquired product-native primitives for one recovery loop without enabling the product updater.", observation, now);
                            PublishHandshake("l4-qa-recovery-active", "driverKind=" + Batch6AutoFishingProductRecoveryDriver.DriverKind);
                        }
                        else if (string.Equals(settings.Level, "L5", StringComparison.Ordinal))
                        {
                            MoveTo(PilotState.WaitingForTitle, "Initial active window and disable cleanup verified; waiting for the QA host-owned ReturnHome transition.", observation, now);
                            PublishHandshake("awaiting-title", "qa-host-return-home-required");
                        }
                        else
                        {
                            Pass("Active product window, configured duration/target, and reflected disable cleanup verified.", observation, now);
                        }
                        break;

                    case PilotState.WaitingForReentryDirectNeutralPreflight:
                        AdvanceDirectNeutralPreflight(observation, now, reentry: true);
                        break;

                    case PilotState.WaitingForReentryEnable:
                        if (!IsActive(observation))
                            return G4FixtureStepResult.Pending(Details("awaiting-reentry-enable-toggle"));
                        ValidateScenarioConfig(observation);
                        reentryStartFish = observation.FishCompletedCount;
                        MoveTo(PilotState.ReentryActive, "Second-save configured-toggle enable observed after title re-entry; waiting for one real completed fish.", observation, now);
                        break;

                    case PilotState.ReentryActive:
                        RequireActive(observation, "title re-entry recovery");
                        if (observation.FishCompletedCount - reentryStartFish >= 1)
                        {
                            ObserveNativeVitalsFinal("post-reload-recovery-end");
                            MoveTo(PilotState.WaitingForFinalDisable, "Post-reload real fishing progress observed; waiting for the configured final cleanup toggle.", observation, now);
                            PublishHandshake("awaiting-final-disable-toggle", "post-reentry-fish-complete; toggleKey=" + settings.ToggleKey);
                        }
                        break;

                    case PilotState.WaitingForFinalDisable:
                        if (!IsInactive(observation))
                            return G4FixtureStepResult.Pending(Details("awaiting-final-disable-toggle"));
                        writer.SetCleanup(observation, returnedToTitleObserved, verified: true, "L5 final ModEntry updater/session cleanup verified after title re-entry.");
                        access.SetHookStatus(
                            Batch6AutoFishingPilotSettings.CleanupHookId,
                            "verified",
                            "real ModEntry reflected cleanup",
                            Details("l5-final=true; enabled=false; updateSubscribed=false; sessionPresent=false; patches=" + observation.InstalledPatchCount));
                        Pass("L5 active window, title return, second fifth-save load, re-enable progress, and final disable cleanup verified.", observation, now);
                        break;

                    default:
                        throw new InvalidOperationException("Batch6AutoFishingPilot entered an unsupported state " + state + ".");
                }
            }
            catch (Exception ex)
            {
                FailFromException("pilot-advance-failed", ex);
            }

            return state == PilotState.Passed
                ? G4FixtureStepResult.Verified(terminalDetails)
                : state == PilotState.Blocked || state == PilotState.Failed
                    ? G4FixtureStepResult.Failed(terminalDetails)
                    : G4FixtureStepResult.Pending(Details(StateName(state)));
        }

        internal void OnSaveLoaded(int? slot, bool isNewGame)
        {
            if (!Enabled || Terminal)
                return;
            try
            {
                DateTimeOffset now = DateTimeOffset.UtcNow;
                int expectedIndex = 4;
                if (!slot.HasValue || slot.Value != expectedIndex || isNewGame)
                    throw new InvalidDataException("Batch6AutoFishingPilot requires existing fifth save index 4; actualIndex=" + (slot?.ToString() ?? "unknown") + "; isNewGame=" + isNewGame.ToString().ToLowerInvariant() + ".");
                bool initialLoad = saveLoadOrdinal == 0 && state == PilotState.WaitingForSaveLoaded;
                bool reentryLoad = saveLoadOrdinal == 1 && state == PilotState.WaitingForReentry;
                if (!initialLoad && !reentryLoad)
                    throw new InvalidOperationException("Unexpected SaveLoaded ordinal/state for Batch6AutoFishingPilot: ordinal=" + (saveLoadOrdinal + 1).ToString(CultureInfo.InvariantCulture) + "; state=" + state + ".");
                saveLoadOrdinal++;
                Batch6AutoFishingObservation observation = observer.Observe(access.Runtime, now);
                if (settings.ProductMustBeAbsent)
                {
                    Batch6AutoFishingNativeDriverOutcome outcome = Batch6AutoFishingFailClosedPolicy.EvaluateProductAbsentL0(
                        fifthSaveLoaded: true,
                        coreProductInstancePresent: observation.ProductPresent,
                        loadedProductCount: FindLoadedProducts().Length,
                        safeIndependentNativeDriverAvailable: true);
                    if (outcome == Batch6AutoFishingNativeDriverOutcome.FailedProductPresent)
                        throw new InvalidDataException("L0 product absence changed before SaveLoaded.");
                    if (outcome != Batch6AutoFishingNativeDriverOutcome.ContinueWithNativeDriver)
                        throw new InvalidOperationException("L0 native-driver boundary returned unexpected outcome " + outcome + ".");
                }
                else
                    ValidateProductObservation(observation, requireActive: false, requireInactive: true);

                pendingSaveLoadedObservation = observation;
                pendingNativeFishingContext = null;
                MoveTo(PilotState.WaitingForNativeFishingContext,
                    "Authoritative fifth SaveLoaded observed; waiting for bounded native CurrentRoom, selected rod, and fishing-pool readiness.",
                    observation,
                    now);
            }
            catch (Exception ex)
            {
                FailFromException("save-loaded-boundary-failed", ex);
            }
        }

        internal void OnReturnedToTitle()
        {
            if (!Enabled || Terminal)
                return;
            try
            {
                DateTimeOffset now = DateTimeOffset.UtcNow;
                if (state != PilotState.WaitingForTitle)
                    throw new InvalidOperationException("Unexpected ReturnedToTitle while Batch6AutoFishingPilot state=" + state + ".");
                Batch6AutoFishingObservation observation = observer.Observe(access.Runtime, now);
                ValidateProductObservation(observation, requireActive: false, requireInactive: true);
                returnedToTitleObserved = true;
                writer.SetCleanup(observation, titleObserved: true, verified: true, "ReturnedToTitle retained the reflected inactive ModEntry state.");
                MoveTo(PilotState.WaitingForReentry, "ReturnedToTitle observed with inactive product state; the QA host must load the fifth save again through its existing save-load authority.", observation, now);
                PublishHandshake("awaiting-fifth-save-reentry", "qa-host-load-slot-5-required");
            }
            catch (Exception ex)
            {
                FailFromException("returned-to-title-boundary-failed", ex);
            }
        }

        internal void Close(string reason)
        {
            if (!Enabled)
                return;
            compatibilityDriver?.TryCleanup("host-close:" + (reason ?? string.Empty));
            productRecoveryDriver?.TryCleanup("host-close:" + (reason ?? string.Empty));
            Batch6AutoFishingObservation? observation = null;
            try { observation = observer.Observe(access.Runtime, DateTimeOffset.UtcNow); }
            catch (Exception ex)
            {
                access.Log("Batch6AutoFishingPilot close observation failed: " + ex.GetType().Name + ": " + ex.Message);
            }
            bool cleanup = observation == null
                ? settings.ProductMustBeAbsent
                : !observation.ProductPresent || IsInactive(observation);
            writer.SetCleanup(observation, returnedToTitleObserved, cleanup, cleanup
                ? "Close observed no active product updater/session. reason=" + (reason ?? string.Empty)
                : "Close observed an active product updater/session; QA did not mutate the product. reason=" + (reason ?? string.Empty));
            if (!Terminal)
                Fail("host-closed-before-terminal", "QA host closed before Batch6AutoFishingPilot reached a terminal result. reason=" + (reason ?? string.Empty), observation, DateTimeOffset.UtcNow);
        }

        private void AdvanceProductAbsentL0(DateTimeOffset now)
        {
            Batch6AutoFishingObservation product = observer.Observe(access.Runtime, now);
            if (product.ProductPresent || product.ProductAssemblyLoaded || FindLoadedProducts().Length != 0)
                throw new InvalidDataException("The AutoFishing product appeared during product-absent L0.");
            if (product.QaHasStaticProductAssemblyRef)
                throw new InvalidDataException("The optional QA assembly gained a static AutoFishing Product AssemblyRef during L0.");
            Batch6AutoFishingCompatibilityDriver driver = compatibilityDriver
                ?? throw new InvalidOperationException("The L0 compatibility-native-control driver is unavailable.");
            Batch6AutoFishingCompatibilityDriverSnapshot snapshot = driver.Capture();
            if (!snapshot.Enabled || !snapshot.HooksReady || snapshot.PatchCount <= 0 || snapshot.OwnerResourceCount <= 0 || !snapshot.CallbackRuntimePresent)
                throw new InvalidDataException("The L0 compatibility-native-control owner lost its active native executor.");

            if (state == PilotState.L0WarmingUp)
            {
                long warmupNativeExit = snapshot.NativeExitCount - l0WarmupStartNativeExit;
                int warmupConfirmed = snapshot.AutoCastConfirmedCount - l0WarmupStartAutoCastConfirmed;
                if (warmupNativeExit >= settings.WarmupFish && warmupConfirmed >= settings.WarmupFish)
                    BeginProductAbsentMeasurement(snapshot, product, now);
                return;
            }

            if (state != PilotState.L0Measuring)
                throw new InvalidOperationException("Unexpected product-absent L0 state " + state + ".");
            if (now >= nextSampleAtUtc)
            {
                CaptureProductAbsentSample(snapshot, now);
                nextSampleAtUtc = now.AddSeconds(settings.SampleSeconds);
            }

            long measuredNativeExit = snapshot.NativeExitCount - l0MeasurementStartNativeExit;
            TrackMeasurementNativeProgress(snapshot.NativeExitCount, now);
            int measuredConfirmed = snapshot.AutoCastConfirmedCount - l0MeasurementStartAutoCastConfirmed;
            int measuredMiniGameComplete = snapshot.MiniGameCompleteApplicationCount - l0MeasurementStartMiniGameComplete;
            if (snapshot.FailureCount != l0MeasurementStartFailureCount)
                throw new InvalidDataException("The L0 compatibility-native-control driver recorded failures during measurement.");
            bool visibleProgress = !RequiresVisibleMiniGame() || measuredMiniGameComplete >= settings.TargetFish;
            if (now - measurementStartedAtUtc < TimeSpan.FromSeconds(settings.MeasureSeconds) ||
                measuredNativeExit < settings.TargetFish || measuredConfirmed < settings.TargetFish || !visibleProgress)
                return;

            RequireTrailingNativeProgress(now);
            CaptureProductAbsentSample(snapshot, now, force: true);
            ObserveNativeVitalsFinal("l0-measurement-end");
            long warmupUnits = snapshot.NativeExitCount - l0WarmupStartNativeExit - measuredNativeExit;
            writer.SetDriverProgress(warmupUnits, measuredNativeExit, verified: true);
            Batch6AutoFishingCompatibilityDriverCleanup cleanup = driver.Cleanup("measurement-complete");
            Batch6AutoFishingObservation finalProduct = observer.Observe(access.Runtime, DateTimeOffset.UtcNow);
            if (finalProduct.ProductPresent || finalProduct.ProductAssemblyLoaded || FindLoadedProducts().Length != 0)
                throw new InvalidDataException("L0 product absence was lost during compatibility owner cleanup.");
            writer.SetCompatibilityCleanup(finalProduct, cleanup, returnedToTitleObserved);
            access.SetHookStatus(
                Batch6AutoFishingPilotSettings.CleanupHookId,
                "verified",
                "L0 exact compatibility owner cleanup",
                Details(cleanup.Details));
            Pass("Product-absent L0 completed real native warm-up and measurement through the frozen compatibility executor, then removed its exact QA owner and hooks.", finalProduct, DateTimeOffset.UtcNow);
        }

        private void BeginProductAbsentMeasurement(
            Batch6AutoFishingCompatibilityDriverSnapshot driver,
            Batch6AutoFishingObservation product,
            DateTimeOffset now)
        {
            measurementStartedAtUtc = now;
            nextSampleAtUtc = now.AddSeconds(settings.SampleSeconds);
            l0MeasurementStartNativeExit = driver.NativeExitCount;
            l0MeasurementStartAutoCastConfirmed = driver.AutoCastConfirmedCount;
            l0MeasurementStartMiniGameComplete = driver.MiniGameCompleteApplicationCount;
            l0MeasurementStartFailureCount = driver.FailureCount;
            lastMeasurementNativeProgress = driver.NativeExitCount;
            lastMeasurementNativeProgressAtUtc = now;
            MoveTo(PilotState.L0Measuring, "L0 real native warm-up completed; bounded product-absent measurement started.", product, now);
            CaptureProductAbsentSample(driver, now, force: true);
        }

        private void CaptureProductAbsentSample(Batch6AutoFishingCompatibilityDriverSnapshot driver, DateTimeOffset now, bool force = false)
        {
            if (!force && writer.Evidence.Samples.Count > 0 && now < nextSampleAtUtc)
                return;
            var sample = CreateRuntimeSample(now);
            sample.DriverNativeExitCount = driver.NativeExitCount;
            sample.DriverAutoCastConfirmedCount = driver.AutoCastConfirmedCount;
            sample.DriverMiniGameCompleteApplicationCount = driver.MiniGameCompleteApplicationCount;
            sample.DriverFailureCount = driver.FailureCount;
            sample.DriverPatchCount = driver.PatchCount;
            sample.DriverOwnerResourceCount = driver.OwnerResourceCount;
            sample.DriverNativeTransientCount = driver.NativeTransientHandleCount;
            writer.AddSample(sample);
        }

        private void AdvanceProductRecovery(DateTimeOffset now)
        {
            Batch6AutoFishingObservation product = observer.Observe(access.Runtime, now);
            ValidateProductObservation(product, requireActive: false, requireInactive: false);
            ValidateProductUpdaterInactiveDuringRecovery(product);
            Batch6AutoFishingProductRecoveryDriver driver = productRecoveryDriver
                ?? throw new InvalidOperationException("The L4 QA product-native recovery driver is unavailable.");
            Batch6AutoFishingProductRecoverySnapshot recovery = driver.Advance();
            if (!recovery.DriverCastApplied || recovery.DriverRecoveryUnits < 1)
                return;

            CaptureRecoverySample(product, recovery, now);
            writer.SetRecoveryDriverProgress(recovery.DriverRecoveryUnits, verified: true);
            Batch6AutoFishingProductRecoveryCleanup cleanup = driver.Cleanup("native-pull-exited");
            Batch6AutoFishingObservation finalProduct = observer.Observe(access.Runtime, DateTimeOffset.UtcNow);
            ValidateProductObservation(finalProduct, requireActive: false, requireInactive: true);
            writer.SetProductRecoveryCleanup(finalProduct, cleanup, returnedToTitleObserved);
            access.SetHookStatus(
                Batch6AutoFishingPilotSettings.CleanupHookId,
                "verified",
                "L4 QA-owned product-native session cleanup",
                Details(cleanup.Details));
            Pass("L4 real product configured-toggle cleanup and one QA-owner product-native recovery loop were verified; the QA session and transient leases returned to zero without enabling the ModEntry updater.", finalProduct, DateTimeOffset.UtcNow);
        }

        private void CaptureRecoverySample(
            Batch6AutoFishingObservation product,
            Batch6AutoFishingProductRecoverySnapshot recovery,
            DateTimeOffset now)
        {
            var sample = CreateRuntimeSample(now);
            PopulateProductSample(sample, product);
            sample.RecoveryPullExitedCount = recovery.PullExitedCount;
            sample.RecoveryCastAppliedCount = recovery.CastAppliedCount;
            sample.RecoveryBitePreparedCount = recovery.NativeBitePreparedCount;
            sample.RecoverySkipReelCount = recovery.NativeSkipReelCount;
            sample.RecoveryOwnerResourceCount = recovery.OwnerResourceCount;
            sample.RecoveryNativeAccessorFailureCount = recovery.NativeAccessorFailureCount;
            writer.AddSample(sample);
        }

        private void BeginMeasurement(Batch6AutoFishingObservation observation, DateTimeOffset now)
        {
            measurementStartedAtUtc = now;
            measurementStartFish = observation.FishCompletedCount;
            lastMeasurementNativeProgress = observation.FishCompletedCount;
            lastMeasurementNativeProgressAtUtc = now;
            nextSampleAtUtc = now.AddSeconds(settings.SampleSeconds);
            if (settings.IsFormalBehaviorContract)
            {
                if (behaviorBaseline == null)
                {
                    behaviorBaseline = observation;
                    writer.RecordBehaviorBaseline(observation);
                }
            }
            MoveTo(PilotState.Measuring, "Warm-up completed; bounded active measurement started.", observation, now);
            CaptureSample(observation, now, force: true);
        }

        private bool TryRebaselineFormalBehaviorAfterNonProductInitialLoop(
            Batch6AutoFishingObservation observation,
            DateTimeOffset now)
        {
            if (!settings.IsFormalBehaviorContract || settings.ManualMovementCancel ||
                observation.FishCompletedCount - measurementStartFish < settings.TargetFish)
            {
                return false;
            }

            Batch6AutoFishingObservation baseline = behaviorBaseline
                ?? throw new InvalidOperationException("Formal behavior rebaseline has no active baseline.");
            RequireBehaviorCountersNonDecreasing(baseline, observation);
            long castApplied = observation.CastAppliedCount - baseline.CastAppliedCount;
            if (castApplied >= 1)
                return false;

            long completedFish = observation.FishCompletedCount - measurementStartFish;
            if (completedFish > settings.TargetFish || discardedNonProductInitialLoops != 0)
            {
                throw new InvalidDataException(
                    "Formal behavior observed a second completed loop without a product-owned cast. discardedNonProductInitialLoops=" +
                    discardedNonProductInitialLoops.ToString(CultureInfo.InvariantCulture) +
                    "; completedFish=" + completedFish.ToString(CultureInfo.InvariantCulture) +
                    "; castAppliedDelta=" + castApplied.ToString(CultureInfo.InvariantCulture) + ".");
            }

            discardedNonProductInitialLoops = 1;
            behaviorBaseline = observation;
            writer.RecordBehaviorBaseline(observation);
            measurementStartedAtUtc = now;
            measurementStartFish = observation.FishCompletedCount;
            lastMeasurementNativeProgress = observation.FishCompletedCount;
            lastMeasurementNativeProgressAtUtc = now;
            nextSampleAtUtc = now.AddSeconds(settings.SampleSeconds);
            CaptureSample(observation, now, force: true);
            MoveTo(
                PilotState.Measuring,
                "Discarded exactly one completed initial loop without a product-owned cast and reset every formal behavior measurement baseline; discardedNonProductInitialLoops=1.",
                observation,
                now);
            return true;
        }

        private static void RequireBehaviorCountersNonDecreasing(
            Batch6AutoFishingObservation baseline,
            Batch6AutoFishingObservation observation)
        {
            if (observation.FishCompletedCount < baseline.FishCompletedCount ||
                observation.CastAppliedCount < baseline.CastAppliedCount ||
                observation.PullEnteredCount < baseline.PullEnteredCount ||
                observation.PullExitedCount < baseline.PullExitedCount ||
                observation.NativeBitePreparedCount < baseline.NativeBitePreparedCount ||
                observation.InstantBiteCommittedCount < baseline.InstantBiteCommittedCount ||
                observation.NativeVisibleReelCount < baseline.NativeVisibleReelCount ||
                observation.NativeSkipReelCount < baseline.NativeSkipReelCount ||
                observation.VisibleReelQueued < baseline.VisibleReelQueued ||
                observation.VisibleReelConsumed < baseline.VisibleReelConsumed ||
                observation.VisibleReelNativeAccepted < baseline.VisibleReelNativeAccepted ||
                observation.VisibleReelRetries < baseline.VisibleReelRetries ||
                observation.VisibleReelTimeouts < baseline.VisibleReelTimeouts ||
                observation.AnimationApplicationCount < baseline.AnimationApplicationCount ||
                observation.ReadyChargeApplicationCount < baseline.ReadyChargeApplicationCount)
            {
                throw new InvalidDataException("Formal behavior counters regressed before the bounded initial-loop rebaseline.");
            }
        }

        private void TrackMeasurementNativeProgress(long current, DateTimeOffset now)
        {
            if (current <= lastMeasurementNativeProgress)
                return;
            lastMeasurementNativeProgress = current;
            lastMeasurementNativeProgressAtUtc = now;
        }

        private void RequireTrailingNativeProgress(DateTimeOffset now)
        {
            const int TrailingWindowSeconds = 90;
            double gap = Math.Max(0d, (now - lastMeasurementNativeProgressAtUtc).TotalSeconds);
            writer.SetTrailingNativeProgress(lastMeasurementNativeProgressAtUtc, gap, TrailingWindowSeconds, gap <= TrailingWindowSeconds);
            if (gap > TrailingWindowSeconds)
                throw new InvalidDataException("Batch6 AutoFishing measured target was reached without native progress in the final 90-second window; trailingGapSeconds=" + gap.ToString("0.###", CultureInfo.InvariantCulture) + ".");
        }

        private void CaptureSample(Batch6AutoFishingObservation observation, DateTimeOffset now, bool force = false)
        {
            if (!force && writer.Evidence.Samples.Count > 0 && now < nextSampleAtUtc)
                return;
            Batch6AutoFishingPilotSample sample = CreateRuntimeSample(now);
            PopulateProductSample(sample, observation);
            writer.AddSample(sample);
        }

        private void AdvanceNativeFishingContext(DateTimeOffset now)
        {
            Batch6AutoFishingObservation observation = pendingSaveLoadedObservation
                ?? throw new InvalidOperationException("Native fishing-context readiness lost its SaveLoaded product observation.");
            if (!TryPrepareNativeFishingWorkload(saveLoadOrdinal, now))
                return;

            pendingSaveLoadedObservation = null;
            pendingNativeFishingContext = null;
            if (settings.ProductMustBeAbsent)
            {
                compatibilityDriver = new Batch6AutoFishingCompatibilityDriver(access, settings);
                Batch6AutoFishingCompatibilityDriverSnapshot driver = compatibilityDriver.Start();
                writer.BindDriver(driver);
                l0WarmupStartNativeExit = driver.NativeExitCount;
                l0WarmupStartAutoCastConfirmed = driver.AutoCastConfirmedCount;
                MoveTo(PilotState.L0WarmingUp, "Authoritative fifth save reached native fishing readiness with the product absent; frozen compatibility executor activated for a separate QA owner and real native warm-up.", observation, now);
                if (settings.WarmupFish == 0)
                    BeginProductAbsentMeasurement(driver, observation, now);
                return;
            }

            Batch6AutoFishingObservation current = observer.Observe(access.Runtime, now);
            ValidateProductObservation(current, requireActive: false, requireInactive: true);
            if (saveLoadOrdinal == 1)
            {
                BeginDirectNeutralPreflight(now, reentry: false);
                return;
            }
            if (saveLoadOrdinal == 2)
            {
                BeginDirectNeutralPreflight(now, reentry: true);
                return;
            }
            throw new InvalidOperationException("Unexpected native fishing-context ordinal " + saveLoadOrdinal.ToString(CultureInfo.InvariantCulture) + ".");
        }

        private void BeginDirectNeutralPreflight(DateTimeOffset now, bool reentry)
        {
            string diagnosticPrefix = reentry ? "reentry" : "initial";
            Batch6AutoFishingObservation observation = observer.Observe(
                access.Runtime,
                now,
                refreshInactiveNativeMovement: true);
            ValidateProductObservation(observation, requireActive: false, requireInactive: true);
            Batch6AutoFishingNativeSurfaceReceipt nativeSurface = CaptureNativeSurfaceDiagnostic(
                diagnosticPrefix + "-initial-observation",
                now,
                requireVerified: true,
                record: false);
            directNeutralPreflightGate = new Batch6AutoFishingDirectNeutralPreflightGate(
                Batch6AutoFishingPilotSettings.DirectNeutralStabilityMilliseconds);
            Batch6AutoFishingDirectNeutralPreflightStatus status;
            try
            {
                status = directNeutralPreflightGate.Observe(
                    now,
                    observation.PreflightNativeMovementRefreshed,
                    observation.NativeMovementAvailable,
                    observation.NativeInputMultiplier,
                    observation.NativeVelocityX,
                    observation.NativeOffsetX,
                    nativeSurface);
            }
            catch
            {
                nativeSurface.Phase = diagnosticPrefix + "-direct-neutral-failed";
                writer.RecordNativeSurfaceDiagnostic(nativeSurface);
                throw;
            }
            if (status == Batch6AutoFishingDirectNeutralPreflightStatus.WaitingForDirectNeutral)
                nativeSurface.Phase = diagnosticPrefix + "-load-contact-pending";
            else if (status == Batch6AutoFishingDirectNeutralPreflightStatus.WaitingForStableNeutral)
                nativeSurface.Phase = diagnosticPrefix + "-before-input";
            else
                throw new InvalidDataException("Direct-neutral preflight entered an unexpected status from the untouched fifth-save load position.");
            writer.RecordNativeSurfaceDiagnostic(nativeSurface);
            PilotState next = reentry
                ? PilotState.WaitingForReentryDirectNeutralPreflight
                : PilotState.WaitingForInitialDirectNeutralPreflight;
            string load = reentry ? "second-fifth-save" : "fifth-save";
            string readiness = status == Batch6AutoFishingDirectNeutralPreflightStatus.WaitingForDirectNeutral
                ? "the exact same-position no-contact loading state is still awaiting directly observed ordinary ground before the stability window"
                : "ordinary ground is directly verified and the native-parity neutral stability window has begun";
            MoveTo(
                next,
                "Authoritative " + load + " native fishing context is ready with the real ModEntry inactive; the untouched load position directly verified face-right, no wall/platform, exact input/offset and Rigidbody Y zero with X inside the native Wait pre-base threshold; " + readiness + ". No preflight input is authorized. " + directNeutralPreflightGate.Details,
                observation,
                now);
        }

        private void AdvanceDirectNeutralPreflight(Batch6AutoFishingObservation observation, DateTimeOffset now, bool reentry)
        {
            Batch6AutoFishingDirectNeutralPreflightGate gate = directNeutralPreflightGate
                ?? throw new InvalidOperationException("Direct-neutral preflight state has no gate.");
            string diagnosticPrefix = reentry ? "reentry" : "initial";
            Batch6AutoFishingNativeSurfaceReceipt nativeSurface = CaptureNativeSurfaceDiagnostic(
                diagnosticPrefix + "-neutral-observation",
                now,
                requireVerified: false,
                record: false);
            Batch6AutoFishingDirectNeutralPreflightStatus previousStatus = gate.Status;
            Batch6AutoFishingDirectNeutralPreflightStatus status;
            try
            {
                status = gate.Observe(
                    now,
                    observation.PreflightNativeMovementRefreshed,
                    observation.NativeMovementAvailable,
                    observation.NativeInputMultiplier,
                    observation.NativeVelocityX,
                    observation.NativeOffsetX,
                    nativeSurface);
            }
            catch
            {
                nativeSurface.Phase = diagnosticPrefix + "-direct-neutral-failed";
                writer.RecordNativeSurfaceDiagnostic(nativeSurface);
                throw;
            }
            if (status == Batch6AutoFishingDirectNeutralPreflightStatus.WaitingForDirectNeutral)
                return;
            if (status == Batch6AutoFishingDirectNeutralPreflightStatus.WaitingForStableNeutral)
            {
                if (previousStatus == Batch6AutoFishingDirectNeutralPreflightStatus.WaitingForDirectNeutral)
                {
                    nativeSurface.Phase = diagnosticPrefix + "-before-input";
                    writer.RecordNativeSurfaceDiagnostic(nativeSurface);
                }
                return;
            }
            if (status != Batch6AutoFishingDirectNeutralPreflightStatus.Ready)
                throw new InvalidDataException("Direct-neutral preflight entered an unknown status while waiting for stable neutral.");

            nativeSurface.Phase = diagnosticPrefix + "-stable-neutral-ready";
            writer.RecordNativeSurfaceDiagnostic(nativeSurface);
            string details = gate.Details;
            if (!reentry && settings.IsFormalBehaviorContract)
            {
                behaviorBaseline = observation;
                writer.RecordBehaviorBaseline(observation);
            }
            directNeutralPreflightGate = null;
            PilotState next = reentry ? PilotState.WaitingForReentryEnable : PilotState.WaitingForInitialEnable;
            string handshake = reentry ? "awaiting-reentry-enable-toggle" : "awaiting-initial-enable-toggle";
            MoveTo(
                next,
                (reentry ? "Second fifth-save" : "Initial fifth-save") +
                " untouched-load direct precondition and same-position stable native-parity neutral were observed without D, Escape, or any other preflight input; waiting for the configured toggle. " + details,
                observation,
                now);
            PublishHandshake(
                handshake,
                "direct-face-right-same-position-stable-native-parity-neutral-verified; preflightInputCount=0; inputContext=" + (access.InputContext ?? string.Empty) +
                "; " + details + "; toggleKey=" + settings.ToggleKey +
                "; nativeSurface={" + nativeSurface.Summary + "}");
        }

        private Batch6AutoFishingNativeSurfaceReceipt CaptureNativeSurfaceDiagnostic(
            string phase,
            DateTimeOffset now,
            bool requireVerified,
            bool record = true)
        {
            Batch6AutoFishingNativeSurfaceReceipt receipt = Batch6AutoFishingNativeSurfaceObserver.Capture(
                saveLoadOrdinal,
                phase,
                access.InputContext ?? string.Empty,
                now);
            if (record)
                writer.RecordNativeSurfaceDiagnostic(receipt);
            if (requireVerified && !receipt.Verified)
            {
                throw new InvalidDataException(
                    "Batch6AutoFishingPilot native surface diagnostic failed closed at " + phase + ": " + receipt.Error);
            }
            return receipt;
        }

        private bool TryPrepareNativeFishingWorkload(int ordinal, DateTimeOffset now)
        {
            string phase = ordinal == 1
                ? global::DTMAPI.GameBridge.DolocTown.AutoFishingNativeVitalsReceipt.InitialSaveWorkloadPhase
                : global::DTMAPI.GameBridge.DolocTown.AutoFishingNativeVitalsReceipt.PostReloadWorkloadPhase;
            Batch6AutoFishingNativeFishingContextReceipt context = Batch6AutoFishingNativeFishingContextObserver.Capture(ordinal, phase, now);
            pendingNativeFishingContext = context;
            if (!context.Verified)
                return false;
            writer.RecordNativeFishingContext(context);

            Type dolocApi = Batch6AutoFishingNativeFishingContextObserver.RequireDolocApiType();
            nativeVitals ??= global::DTMAPI.GameBridge.DolocTown.AutoFishingNativeVitalsCommandAdapter.Create(dolocApi);
            global::DTMAPI.GameBridge.DolocTown.AutoFishingNativeVitalsReceipt receipt = nativeVitals.PrepareWorkload(
                "Batch6AutoFishingPilot " + settings.Level + " " + phase,
                ordinal,
                phase);
            RecordAndRequireNativeVitals(receipt);
            nextNativeVitalsMaintenanceAtUtc = now.AddMilliseconds(Batch6AutoFishingPilotSettings.ObservationCadenceMilliseconds);
            return true;
        }

        private void MaintainNativeVitals(DateTimeOffset now)
        {
            if (nativeVitals == null)
                throw new InvalidOperationException("The active Batch6 AutoFishing workload has no official native-vitals adapter.");
            if (now < nextNativeVitalsMaintenanceAtUtc)
                return;
            nextNativeVitalsMaintenanceAtUtc = now.AddMilliseconds(Batch6AutoFishingPilotSettings.ObservationCadenceMilliseconds);
            if (nativeVitals.TryMaintainWorkload("Batch6AutoFishingPilot " + settings.Level + " " + StateName(state), out global::DTMAPI.GameBridge.DolocTown.AutoFishingNativeVitalsReceipt? receipt) && receipt != null)
                RecordAndRequireNativeVitals(receipt);
        }

        private void PrepareL4NativeVitalsCheckpoint()
        {
            if (nativeVitals == null)
                throw new InvalidOperationException("L4 recovery has no official native-vitals adapter.");
            RecordAndRequireNativeVitals(nativeVitals.PrepareL4RecoveryCheckpoint(
                global::DTMAPI.GameBridge.DolocTown.AutoFishingNativeVitalsReceipt.OfficialL4RecoveryCheckpointContext));
        }

        private void ObserveNativeVitalsFinal(string context)
        {
            if (nativeVitals == null)
                throw new InvalidOperationException("Batch6 AutoFishing measurement ended without an official native-vitals adapter.");
            RecordAndRequireNativeVitals(nativeVitals.ObserveMeasurementEnd("Batch6AutoFishingPilot " + settings.Level + " " + (context ?? string.Empty)));
        }

        private void RecordAndRequireNativeVitals(global::DTMAPI.GameBridge.DolocTown.AutoFishingNativeVitalsReceipt receipt)
        {
            writer.RecordNativeVitals(receipt);
            Batch6AutoFishingNativeVitalsReceipt mapped = writer.Evidence.NativeVitals.Receipts[writer.Evidence.NativeVitals.Receipts.Count - 1];
            if (!mapped.ReadbackVerified)
                throw new InvalidDataException("Official AutoFishing native-vitals receipt failed command identity or energy/spirit readback verification. kind=" + mapped.Kind + ".");
        }

        private static bool IsNativeWorkloadActive(PilotState value) =>
            value == PilotState.L0WarmingUp || value == PilotState.L0Measuring ||
            value == PilotState.WaitingForInitialDirectNeutralPreflight || value == PilotState.WaitingForReentryDirectNeutralPreflight ||
            value == PilotState.WaitingForManualMovementCancel || value == PilotState.WaitingForManualMovementReenable ||
            value == PilotState.WarmingUp || value == PilotState.Measuring ||
            value == PilotState.L4Recovering || value == PilotState.ReentryActive;

        private Batch6AutoFishingPilotSample CreateRuntimeSample(DateTimeOffset now)
        {
            var sample = new Batch6AutoFishingPilotSample
            {
                ObservedAtUtc = now.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
                ElapsedSeconds = Math.Max(0d, (now - measurementStartedAtUtc).TotalSeconds),
                DtmApiRecordCount = access.Runtime.RuntimeMemoryRecordCount,
                OwnerRootCount = access.Runtime.RuntimeMemoryOwnerRootCount,
                InputOwnerCount = access.Runtime.Input.GetOwnerSnapshot().OwnerCount,
                EventActiveHandlers = access.Runtime.Events.GetHandlerCleanupSnapshot().ActiveHandlers,
                ApiRootCount = access.Runtime.ModRegistry.TotalRootCount,
                DemandEntryCount = access.Runtime.RuntimeDemandSnapshot.DemandEntryCount,
                TotalDemand = access.Runtime.RuntimeDemandSnapshot.TotalDemand,
                Gen0Collections = GC.CollectionCount(0),
                Gen1Collections = GC.CollectionCount(1),
                Gen2Collections = GC.CollectionCount(2)
            };
            CaptureProcessMetrics(sample);
            return sample;
        }

        private static void CaptureProcessMetrics(Batch6AutoFishingPilotSample sample)
        {
            try
            {
                using (Process process = Process.GetCurrentProcess())
                {
                    sample.ProcessPrivateBytes = process.PrivateMemorySize64;
                    sample.ProcessWorkingSetBytes = process.WorkingSet64;
                    if (sample.ProcessPrivateBytes > 0 && sample.ProcessWorkingSetBytes > 0)
                    {
                        sample.ProcessMetricsAvailable = true;
                        sample.ProcessMetricsSource = "System.Diagnostics.Process";
                        return;
                    }

                    if (TryCaptureWindowsProcessMetrics(process, out long privateBytes, out long workingSetBytes, out string nativeError))
                    {
                        sample.ProcessPrivateBytes = privateBytes;
                        sample.ProcessWorkingSetBytes = workingSetBytes;
                        sample.ProcessMetricsAvailable = true;
                        sample.ProcessMetricsSource = "Windows.GetProcessMemoryInfo";
                        sample.ProcessMetricsError = string.Empty;
                        return;
                    }
                    sample.ProcessMetricsError = "System.Diagnostics.Process returned non-positive values; " + nativeError;
                }
            }
            catch (Exception ex)
            {
                sample.ProcessPrivateBytes = -1;
                sample.ProcessWorkingSetBytes = -1;
                sample.ProcessMetricsAvailable = false;
                sample.ProcessMetricsError = ex.GetType().Name + ": " + ex.Message;
            }
        }

        private static bool TryCaptureWindowsProcessMetrics(
            Process process,
            out long privateBytes,
            out long workingSetBytes,
            out string error)
        {
            privateBytes = 0;
            workingSetBytes = 0;
            error = string.Empty;
            try
            {
                var counters = new ProcessMemoryCountersEx();
                counters.Cb = checked((uint)Marshal.SizeOf(typeof(ProcessMemoryCountersEx)));
                if (!GetProcessMemoryInfo(process.Handle, ref counters, counters.Cb))
                {
                    error = "Windows.GetProcessMemoryInfo failed with Win32 error " + Marshal.GetLastWin32Error().ToString(CultureInfo.InvariantCulture) + ".";
                    return false;
                }
                privateBytes = checked((long)counters.PrivateUsage.ToUInt64());
                workingSetBytes = checked((long)counters.WorkingSetSize.ToUInt64());
                if (privateBytes <= 0 || workingSetBytes <= 0)
                {
                    error = "Windows.GetProcessMemoryInfo returned non-positive private-byte or working-set values.";
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                error = "Windows.GetProcessMemoryInfo failed: " + ex.GetType().Name + ": " + ex.Message;
                return false;
            }
        }

        private static void PopulateProductSample(Batch6AutoFishingPilotSample sample, Batch6AutoFishingObservation observation)
        {
            sample.Enabled = observation.Enabled;
            sample.UpdateSubscribed = observation.UpdateSubscribed;
            sample.SessionPresent = observation.SessionPresent;
            sample.InstalledPatchCount = observation.InstalledPatchCount;
            sample.FishCompletedCount = observation.FishCompletedCount;
            sample.PullEnteredCount = observation.PullEnteredCount;
            sample.PullExitedCount = observation.PullExitedCount;
            sample.VisibleReelConsumed = observation.VisibleReelConsumed;
            sample.VisibleReelNativeAccepted = observation.VisibleReelNativeAccepted;
            sample.CastAppliedCount = observation.CastAppliedCount;
            sample.NativeBitePreparedCount = observation.NativeBitePreparedCount;
            sample.InstantBiteCommittedCount = observation.InstantBiteCommittedCount;
            sample.NativeVisibleReelCount = observation.NativeVisibleReelCount;
            sample.NativeSkipReelCount = observation.NativeSkipReelCount;
            sample.VisibleReelQueued = observation.VisibleReelQueued;
            sample.VisibleReelRetries = observation.VisibleReelRetries;
            sample.VisibleReelTimeouts = observation.VisibleReelTimeouts;
            sample.AnimationApplicationCount = observation.AnimationApplicationCount;
            sample.ReadyChargeApplicationCount = observation.ReadyChargeApplicationCount;
            sample.LastReason = observation.LastReason;
            sample.NativeAccessorBuildCount = observation.NativeAccessorBuildCount;
            sample.NativeAccessorFailureCount = observation.NativeAccessorFailureCount;
            sample.NativeTransientCount = observation.NativeTransientCount;
        }

        private void ValidateCoreClassification(DiscoveredMod mod)
        {
            if (!string.Equals(mod.Manifest.EntryType, Batch6AutoFishingPilotSettings.ProductEntryType, StringComparison.Ordinal) ||
                !string.Equals(mod.Manifest.CodeModKind, "Advanced", StringComparison.Ordinal) ||
                !mod.Classification.IsAdvanced ||
                !mod.Classification.AdvancedReferenceVerified)
            {
                throw new InvalidDataException("The loaded Core product record is not one SDK-verified Advanced AutoFishing identity.");
            }
            if (!Batch6AutoFishingPilotSettings.NormalizeSha256(mod.Classification.EntryDllSha256, "Core classification entry hash")
                    .Equals(settings.ExpectedEntryDllSha256, StringComparison.Ordinal) ||
                !Batch6AutoFishingPilotSettings.NormalizeSha256(mod.Classification.ReferencePolicySha256, "Core classification policy hash")
                    .Equals(settings.ExpectedReferencePolicySha256, StringComparison.Ordinal))
            {
                throw new InvalidDataException("The Core-resident product classification does not bind the expected entry/policy hashes.");
            }
        }

        private void ValidateProductObservation(Batch6AutoFishingObservation observation, bool requireActive, bool requireInactive)
        {
            if (!observation.ProductPresent)
                throw new InvalidDataException("The real Core-resident AutoFishing ModEntry disappeared during the pilot.");
            if (observation.QaHasStaticProductAssemblyRef)
                throw new InvalidDataException("The QA assembly unexpectedly gained a static Product AssemblyRef.");
            if (observation.InstalledPatchCount != Batch6AutoFishingPilotSettings.ExpectedProductPatchCount)
                throw new InvalidDataException("The product-owned patch inventory is incomplete or changed. expected=" + Batch6AutoFishingPilotSettings.ExpectedProductPatchCount + "; actual=" + observation.InstalledPatchCount + ".");
            if (!observation.ProductCallbackRuntimePresent || observation.CanonicalHarmonyPatchCount != Batch6AutoFishingPilotSettings.ExpectedProductPatchCount)
                throw new InvalidDataException("The process-level AutoFishing callback root or canonical Harmony owner inventory is incomplete. callbackRuntime=" + observation.ProductCallbackRuntimePresent + "; canonicalPatches=" + observation.CanonicalHarmonyPatchCount + ".");
            if (observation.NativeAccessorFailureCount != 0)
                throw new InvalidDataException("The real product reported native accessor failures=" + observation.NativeAccessorFailureCount + ".");
            if (requireActive)
                RequireActive(observation, "required active state");
            if (requireInactive && !IsInactive(observation))
                throw new InvalidDataException("Expected inactive real ModEntry state, observed enabled=" + observation.Enabled + "; updateSubscribed=" + observation.UpdateSubscribed + "; sessionPresent=" + observation.SessionPresent + "; nativeTransientCount=" + observation.NativeTransientCount + ".");
        }

        private static void ValidateProductUpdaterInactiveDuringRecovery(Batch6AutoFishingObservation observation)
        {
            if (observation.Enabled || observation.UpdateSubscribed || observation.SessionPresent)
            {
                throw new InvalidDataException("L4 recovery requires the real ModEntry updater and product session inactive while the distinct QA owner holds ProductNative primitives. enabled=" +
                    observation.Enabled + "; updateSubscribed=" + observation.UpdateSubscribed + "; productSessionPresent=" + observation.SessionPresent +
                    "; sharedNativeTransientCount=" + observation.NativeTransientCount + ".");
            }
        }

        private void ValidateScenarioConfig(Batch6AutoFishingObservation observation)
        {
            bool instant = settings.Scenario == "InstantBite" || settings.Scenario == "InstantSkip" ||
                settings.Scenario == "CombinedInstantSkip" || settings.Scenario == "CombinedInstantComplete";
            bool skip = settings.Scenario == "SkipMiniGame" || settings.Scenario == "InstantSkip" || settings.Scenario == "CombinedInstantSkip";
            bool fast = settings.Scenario == "FastAnimations" || settings.Scenario == "CombinedInstantSkip" || settings.Scenario == "CombinedInstantComplete";
            if (!string.Equals(observation.ToggleKey, settings.ToggleKey, StringComparison.OrdinalIgnoreCase) ||
                observation.InstantBite != instant || observation.SkipMiniGame != skip || observation.FastAnimations != fast ||
                Math.Abs(observation.AnimationMultiplier - settings.Multiplier) > 0.000001d ||
                Math.Abs(observation.CastChargeRatio - settings.CastChargeRatio) > 0.000001d)
            {
                throw new InvalidDataException(
                    "The real ModEntry config does not match the pilot scenario. scenario=" + settings.Scenario +
                    "; expectedToggle=" + settings.ToggleKey +
                    "; actualToggle=" + observation.ToggleKey +
                    "; instant=" + observation.InstantBite +
                    "; skip=" + observation.SkipMiniGame +
                    "; fast=" + observation.FastAnimations +
                    "; multiplier=" + observation.AnimationMultiplier.ToString("0.###", CultureInfo.InvariantCulture) +
                    "; castChargeRatio=" + observation.CastChargeRatio.ToString("0.###", CultureInfo.InvariantCulture) + ".");
            }
        }

        private void ValidateAndRecordBehaviorResult(Batch6AutoFishingObservation observation)
        {
            Batch6AutoFishingObservation baseline = behaviorBaseline
                ?? throw new InvalidOperationException("Formal behavior validation has no active baseline.");
            bool instant = settings.Scenario == "InstantBite" || settings.Scenario == "InstantSkip" ||
                settings.Scenario == "CombinedInstantSkip" || settings.Scenario == "CombinedInstantComplete";
            bool skip = settings.Scenario == "SkipMiniGame" || settings.Scenario == "InstantSkip" ||
                settings.Scenario == "CombinedInstantSkip";
            bool fast = settings.Scenario == "FastAnimations" || settings.Scenario == "CombinedInstantSkip" ||
                settings.Scenario == "CombinedInstantComplete";
            long cast = observation.CastAppliedCount - baseline.CastAppliedCount;
            long pullEntered = observation.PullEnteredCount - baseline.PullEnteredCount;
            long pullExited = observation.PullExitedCount - baseline.PullExitedCount;
            long bitePrepared = observation.NativeBitePreparedCount - baseline.NativeBitePreparedCount;
            long instantBiteCommitted = observation.InstantBiteCommittedCount - baseline.InstantBiteCommittedCount;
            long visible = observation.NativeVisibleReelCount - baseline.NativeVisibleReelCount;
            long skipped = observation.NativeSkipReelCount - baseline.NativeSkipReelCount;
            long queued = observation.VisibleReelQueued - baseline.VisibleReelQueued;
            long consumed = observation.VisibleReelConsumed - baseline.VisibleReelConsumed;
            long accepted = observation.VisibleReelNativeAccepted - baseline.VisibleReelNativeAccepted;
            long timeouts = observation.VisibleReelTimeouts - baseline.VisibleReelTimeouts;
            int animation = observation.AnimationApplicationCount - baseline.AnimationApplicationCount;
            int readyCharge = observation.ReadyChargeApplicationCount - baseline.ReadyChargeApplicationCount;
            bool fullLoop = cast >= 1 && pullEntered >= 1 && pullExited >= 1;
            bool biteOk = instant ? instantBiteCommitted >= 1 : instantBiteCommitted == 0;
            bool resultPathOk = skip
                ? skipped >= 1 && visible == 0 && queued >= 1 && consumed >= 1 && accepted >= 1 && timeouts == 0
                : visible >= 1 && skipped == 0 && queued >= 1 && consumed >= 1 && accepted >= 1 && timeouts == 0;
            bool animationOk = fast ? animation >= 1 : animation == 0;
            bool chargeOk = settings.CastChargeRatio > 0d ? readyCharge >= 1 : readyCharge == 0;
            bool verified = fullLoop && biteOk && resultPathOk && animationOk && chargeOk &&
                Math.Abs(observation.CastChargeRatio - settings.CastChargeRatio) <= 0.000001d;
            string details = "fullLoop=" + fullLoop.ToString().ToLowerInvariant() +
                "; instantExpected=" + instant.ToString().ToLowerInvariant() +
                "; instantBiteCommittedDelta=" + instantBiteCommitted.ToString(CultureInfo.InvariantCulture) +
                "; nativeBitePreparedDelta=" + bitePrepared.ToString(CultureInfo.InvariantCulture) +
                "; skipExpected=" + skip.ToString().ToLowerInvariant() + "; visibleDelta=" + visible.ToString(CultureInfo.InvariantCulture) +
                "; skipDelta=" + skipped.ToString(CultureInfo.InvariantCulture) + "; queued/consumed/accepted=" +
                queued.ToString(CultureInfo.InvariantCulture) + "/" + consumed.ToString(CultureInfo.InvariantCulture) + "/" + accepted.ToString(CultureInfo.InvariantCulture) +
                "; fastExpected=" + fast.ToString().ToLowerInvariant() + "; animationDelta=" + animation.ToString(CultureInfo.InvariantCulture) +
                "; castChargeRatio=" + settings.CastChargeRatio.ToString("0.###", CultureInfo.InvariantCulture) +
                "; readyChargeDelta=" + readyCharge.ToString(CultureInfo.InvariantCulture) + "; visibleTimeoutDelta=" + timeouts.ToString(CultureInfo.InvariantCulture) +
                "; discardedNonProductInitialLoops=" + discardedNonProductInitialLoops.ToString(CultureInfo.InvariantCulture);
            writer.RecordBehaviorResult(observation, verified, fullLoop, details);
            if (!verified)
                throw new InvalidDataException("Formal Batch6 AutoFishing behavior deltas failed. " + details + ".");
        }

        private string CurrentManualMovementTarget()
        {
            if (manualMovementTargetIndex < 0 || manualMovementTargetIndex >= Batch6AutoFishingPilotSettings.ManualMovementTargetPhases.Length)
                throw new InvalidOperationException("Manual movement target index escaped the frozen phase set: " + manualMovementTargetIndex.ToString(CultureInfo.InvariantCulture) + ".");
            return Batch6AutoFishingPilotSettings.ManualMovementTargetPhases[manualMovementTargetIndex];
        }

        private static bool MatchesManualMovementTarget(string targetPhase, string observedPhase)
        {
            if (targetPhase == "Ready")
                return observedPhase == "ReadyEntered";
            if (targetPhase == "Cast")
                return observedPhase == "CastEntered";
            if (targetPhase == "Wait")
                return observedPhase == "WaitEntered" || observedPhase == "WaitPlayable";
            if (targetPhase == "BiteReady")
                return observedPhase == "BiteReady";
            if (targetPhase == "MiniGame")
                return observedPhase == "MiniGameStarted" || observedPhase == "MiniGameRunning";
            return false;
        }

        private static bool IsExactMovementReleaseReason(string reason) =>
            reason.StartsWith("manual-move inputMultiplier=", StringComparison.Ordinal) ||
            reason.StartsWith("native-move VelocityX=", StringComparison.Ordinal);

        private static string MovementPhaseSlug(string phase)
        {
            if (phase == "BiteReady")
                return "bite-ready";
            if (phase == "MiniGame")
                return "minigame";
            return phase.ToLowerInvariant();
        }

        private static bool ManualMovementReceiptsVerified(Batch6AutoFishingBehaviorReceipt behavior)
        {
            if (behavior.MovementPhaseReceipts.Count != Batch6AutoFishingPilotSettings.ManualMovementTargetPhases.Length)
                return false;
            for (int index = 0; index < Batch6AutoFishingPilotSettings.ManualMovementTargetPhases.Length; index++)
            {
                Batch6AutoFishingMovementPhaseReceipt receipt = behavior.MovementPhaseReceipts[index];
                string target = Batch6AutoFishingPilotSettings.ManualMovementTargetPhases[index];
                if (receipt.Sequence != index + 1 || receipt.TargetPhase != target ||
                    !MatchesManualMovementTarget(target, receipt.ObservedPhase) ||
                    !IsExactMovementReleaseReason(receipt.ReleaseReason) ||
                    !receipt.InactiveCleanupVerified)
                {
                    return false;
                }
            }
            return true;
        }

        private bool RequiresVisibleMiniGame() =>
            settings.Scenario != "SkipMiniGame" &&
            settings.Scenario != "InstantSkip" &&
            settings.Scenario != "CombinedInstantSkip";

        private G4FixtureStepResult CurrentStepResult() => state == PilotState.Passed
            ? G4FixtureStepResult.Verified(terminalDetails)
            : state == PilotState.Blocked || state == PilotState.Failed
                ? G4FixtureStepResult.Failed(terminalDetails)
                : G4FixtureStepResult.Pending(Details(StateName(state)));

        private void CheckStateTimeout(DateTimeOffset now)
        {
            TimeSpan elapsed = now - stateEnteredAtUtc;
            double limitSeconds;
            switch (state)
            {
                case PilotState.L0WarmingUp: limitSeconds = Math.Max(600, settings.MeasureSeconds); break;
                case PilotState.L0Measuring: limitSeconds = settings.MeasureSeconds + Math.Max(180, settings.MeasureSeconds / 2d); break;
                case PilotState.WarmingUp: limitSeconds = Math.Max(600, settings.MeasureSeconds); break;
                case PilotState.Measuring: limitSeconds = settings.MeasureSeconds + Math.Max(180, settings.MeasureSeconds / 2d); break;
                case PilotState.WaitingForReentry: limitSeconds = 600; break;
                case PilotState.WaitingForNativeFishingContext: limitSeconds = Batch6AutoFishingPilotSettings.NativeFishingContextReadyTimeoutSeconds; break;
                case PilotState.WaitingForInitialDirectNeutralPreflight:
                case PilotState.WaitingForReentryDirectNeutralPreflight:
                    limitSeconds = Batch6AutoFishingPilotSettings.DirectNeutralReadyTimeoutSeconds;
                    break;
                default: limitSeconds = 300; break;
            }
            if (elapsed > TimeSpan.FromSeconds(limitSeconds))
            {
                if (state == PilotState.WaitingForNativeFishingContext && pendingNativeFishingContext != null)
                {
                    writer.RecordNativeFishingContext(pendingNativeFishingContext);
                    throw new InvalidDataException("The authoritative fifth-save selected-rod/pool preflight did not become ready within " +
                        Batch6AutoFishingPilotSettings.NativeFishingContextReadyTimeoutSeconds.ToString(CultureInfo.InvariantCulture) +
                        " seconds. Last observation: " + pendingNativeFishingContext.Error);
                }
                if (state == PilotState.L4Recovering && productRecoveryDriver != null)
                {
                    throw new TimeoutException("Batch6AutoFishingPilot timed out in state " + StateName(state) + " after " +
                        elapsed.TotalSeconds.ToString("0", CultureInfo.InvariantCulture) + " seconds. Last recovery observation: " +
                        productRecoveryDriver.LastObservationDetails + ".");
                }
                if (state == PilotState.WaitingForInitialDisable && string.Equals(settings.Level, "L4", StringComparison.Ordinal) && productRecoveryDriver != null)
                {
                    throw new TimeoutException("Batch6AutoFishingPilot timed out waiting for the L4 recovery entry gate after " +
                        elapsed.TotalSeconds.ToString("0", CultureInfo.InvariantCulture) + " seconds. Last recovery entry observation: " +
                        productRecoveryDriver.LastObservationDetails + ".");
                }
                if (IsDirectNeutralPreflightState(state) && directNeutralPreflightGate != null)
                {
                    Batch6AutoFishingNativeSurfaceReceipt nativeSurface = CaptureNativeSurfaceDiagnostic(
                        (state == PilotState.WaitingForReentryDirectNeutralPreflight ? "reentry" : "initial") + "-timeout",
                        now,
                        requireVerified: false);
                    throw new TimeoutException("Batch6AutoFishingPilot timed out waiting for the untouched-load direct precondition and same-position stable native-parity neutral after " +
                        elapsed.TotalSeconds.ToString("0", CultureInfo.InvariantCulture) + " seconds. Last preflight observation: " +
                        "inputContext=" + access.InputContext + "; " + directNeutralPreflightGate.Details +
                        "; nativeSurface={" + nativeSurface.Summary + "}.");
                }
                throw new TimeoutException("Batch6AutoFishingPilot timed out in state " + StateName(state) + " after " + elapsed.TotalSeconds.ToString("0", CultureInfo.InvariantCulture) + " seconds.");
            }
        }

        private void RequireActive(Batch6AutoFishingObservation observation, string phase)
        {
            if (!IsActive(observation))
                throw new InvalidDataException("The real ModEntry lost active state during " + phase + ": enabled=" + observation.Enabled + "; updateSubscribed=" + observation.UpdateSubscribed + "; sessionPresent=" + observation.SessionPresent + "; sessionReleased=" + observation.SessionReleased + ".");
        }

        private static bool IsActive(Batch6AutoFishingObservation observation) =>
            observation.Enabled && observation.UpdateSubscribed && observation.SessionPresent && !observation.SessionReleased;

        private static bool IsInactive(Batch6AutoFishingObservation observation) =>
            !observation.Enabled && !observation.UpdateSubscribed && !observation.SessionPresent && observation.NativeTransientCount == 0;

        private static bool IsDirectNeutralPreflightState(PilotState value) =>
            value == PilotState.WaitingForInitialDirectNeutralPreflight ||
            value == PilotState.WaitingForReentryDirectNeutralPreflight;

        private DiscoveredMod[] FindLoadedProducts() => access.Runtime.LoadedMods
            .Where(mod => string.Equals(mod.Manifest.UniqueID, Batch6AutoFishingPilotSettings.ProductUniqueId, StringComparison.Ordinal))
            .ToArray();

        private void MoveTo(PilotState next, string details, Batch6AutoFishingObservation? observation, DateTimeOffset now)
        {
            state = next;
            stateEnteredAtUtc = now;
            nextObservationAtUtc = now;
            writer.RecordStage(StateName(next), "Pending", details, saveLoadOrdinal, observation, now);
            access.SetHookStatus(
                Batch6AutoFishingPilotSettings.OverallHookId,
                "pending",
                settings.ProductMustBeAbsent ? "product-absent compatibility native control" : "real Core-resident product reflection plus receipt-bound QA",
                Details(details));
            access.Log(Batch6AutoFishingPilotSettings.OverallHookId + " state=" + StateName(next) + " " + Details(details) + ".");
        }

        private void PublishHandshake(string handshakeState, string details)
        {
            string receipt = Details("handshake=" + handshakeState + "; " + details);
            access.SetHookStatus(
                Batch6AutoFishingPilotSettings.HandshakeHookId,
                "ready",
                "runner-visible product QA handshake",
                receipt);
            access.Log(Batch6AutoFishingPilotSettings.HandshakeHookId + " state=" + handshakeState + " " + receipt + ".");
        }

        private void RequireAcceptanceEvidence()
        {
            Batch6AutoFishingPilotEvidence evidence = writer.Evidence;
            if (evidence.ProductAssemblyReferenced)
                throw new InvalidDataException("Formal acceptance forbids a static Product AssemblyRef from the optional QA assembly.");
            if (!evidence.DriverNativeProgress || evidence.DriverPatchCount != Batch6AutoFishingPilotSettings.ExpectedProductPatchCount)
            {
                throw new InvalidDataException(
                    "Batch6 AutoFishing requires exact native driver progress. patches=" + evidence.DriverPatchCount +
                    "; warmup=" + evidence.DriverWarmupUnits + "; measured=" + evidence.DriverMeasuredUnits + ".");
            }
            if (settings.IsFormalLongRunContract &&
                (!evidence.NativeProgressTrailingWindowVerified || evidence.NativeProgressTrailingWindowSeconds != 90 ||
                 evidence.DriverWarmupUnits < Math.Max(5, settings.WarmupFish) ||
                 evidence.DriverMeasuredUnits < Math.Max(10, settings.TargetFish)))
            {
                throw new InvalidDataException("Formal LongRun requires exact 600/30/5/10 progress plus the final 90-second native-progress window.");
            }
            if (settings.IsFormalBehaviorContract)
            {
                Batch6AutoFishingBehaviorReceipt behavior = evidence.Behavior;
                if (!behavior.Verified || behavior.Baseline == null || behavior.Final == null ||
                    behavior.ManualMovementCancel != settings.ManualMovementCancel ||
                    (!settings.ManualMovementCancel && (!behavior.FullNativeLoopVerified || evidence.DriverMeasuredUnits < 1)) ||
                    (settings.ManualMovementCancel &&
                        (behavior.FullNativeLoopVerified || behavior.CastAppliedDelta < 1 ||
                         behavior.ToggleKey != "F7" || !ManualMovementReceiptsVerified(behavior) ||
                         !IsExactMovementReleaseReason(behavior.ManualMovementReason))))
                {
                    throw new InvalidDataException("Formal Behavior requires its exact per-run delta receipt; manual movement is the only no-full-loop exception and still requires an active native cast before physical A.");
                }
                if (evidence.NativeProgressTrailingWindowVerified || evidence.NativeProgressTrailingWindowSeconds != 0)
                    throw new InvalidDataException("Formal Behavior must not claim the distinct LongRun 90-second trailing-progress authority.");
            }
            if (!settings.ProductMustBeAbsent && evidence.Package?.Verified != true)
                throw new InvalidDataException("The loaded Advanced product package did not retain package.verified=true.");
            if (settings.ProductMustBeAbsent &&
                (evidence.Package != null || evidence.PackageSha256.Length != 0 || evidence.EntryDllSha256.Length != 0 ||
                 evidence.ManifestSha256.Length != 0 || evidence.ReferencePolicySha256.Length != 0))
            {
                throw new InvalidDataException("Product-absent L0 must not report actual product provenance.");
            }
            if (evidence.Samples.Count == 0 || evidence.Samples.Any(sample =>
                    !sample.ProcessMetricsAvailable || sample.ProcessPrivateBytes <= 0 || sample.ProcessWorkingSetBytes <= 0 ||
                    !string.IsNullOrEmpty(sample.ProcessMetricsError)))
            {
                throw new InvalidDataException("Batch6 AutoFishing process metrics are unavailable or invalid; zero/error values cannot stand in for a valid capture.");
            }

            int requiredWorkloads = string.Equals(settings.Level, "L5", StringComparison.Ordinal) ? 2 : 1;
            int requiredFinalReadbacks = requiredWorkloads;
            bool requireL4Checkpoint = string.Equals(settings.Level, "L4", StringComparison.Ordinal);
            if (!writer.FinalizeNativeEvidence(requiredWorkloads, requiredFinalReadbacks, requireL4Checkpoint))
                throw new InvalidDataException("Selected rod/pool and official energy/spirit pre/post/final readback evidence is incomplete.");

            Batch6AutoFishingCleanupReceipt cleanup = evidence.Cleanup;
            if (!cleanup.Verified || cleanup.Enabled || cleanup.UpdateSubscribed || cleanup.SessionPresent || cleanup.NativeTransientCount != 0)
                throw new InvalidDataException("The final product/driver cleanup did not prove inactive state and zero native transient handles.");
            if (settings.ProductMustBeAbsent)
            {
                if (cleanup.DriverOwnerResourceCount != 0 || cleanup.DriverServicePresent || cleanup.DriverCallbackRuntimePresent ||
                    cleanup.DriverHooksPresent || cleanup.DriverPatchCount != 0 ||
                    evidence.FinalProductState?.ProductPresent == true || evidence.FinalProductState?.ProductAssemblyLoaded == true)
                {
                    throw new InvalidDataException("L0 exact owner cleanup requires zero resources, service, callback runtime, hooks, patches, and no loaded product assembly.");
                }
            }
            if (requireL4Checkpoint)
            {
                if (!evidence.RecoveryDriverNativeProgress || evidence.RecoveryDriverUnits < 1 ||
                    cleanup.RecoveryOwnerResourceCount != 0 || cleanup.RecoveryActiveSessionCount != 0 ||
                    cleanup.RecoveryInputLeaseCount != 0 || cleanup.RecoveryAnimationLeaseCount != 0 || cleanup.RecoverySchedulerPending)
                {
                    throw new InvalidDataException("L4 exact recovery cleanup/progress evidence is incomplete.");
                }
            }
            if (string.Equals(settings.Level, "L5", StringComparison.Ordinal) && !cleanup.ReturnedToTitleObserved)
                throw new InvalidDataException("L5 acceptance requires the QA-host-owned ReturnedToTitle boundary.");
            if (settings.Formal && !settings.IsFormalContract)
                throw new InvalidDataException("Formal Batch6 AutoFishing acceptance does not match either exact LongRun or exact Behavior contract.");
        }

        private void Pass(string details, Batch6AutoFishingObservation observation, DateTimeOffset now)
        {
            RequireAcceptanceEvidence();
            string status = settings.IsFormalContract ? "Passed" : "NonAuthoritativeCompleted";
            state = PilotState.Passed;
            stateEnteredAtUtc = now;
            terminalDetails = Details("status=" + status + "; authority=" + writer.Evidence.Authority + "; " + details + "; evidence=" + writer.ResultPath);
            writer.RecordStage(StateName(state), status, details, saveLoadOrdinal, observation, now);
            writer.Complete(status, string.Empty, string.Empty, now);
            access.SetHookStatus(Batch6AutoFishingPilotSettings.OverallHookId, "verified", (settings.ProductMustBeAbsent ? "product-absent compatibility native control" : "real Core-resident product reflection plus receipt-bound QA") + "; authority=" + writer.Evidence.Authority, terminalDetails);
            access.Log(Batch6AutoFishingPilotSettings.OverallHookId + " terminal=" + status + " " + terminalDetails + ".");
        }

        private void Block(string code, string details, Batch6AutoFishingObservation? observation, DateTimeOffset now, bool cleanupVerified)
        {
            state = PilotState.Blocked;
            stateEnteredAtUtc = now;
            terminalDetails = Details("status=Blocked; failureCode=" + code + "; " + details + "; evidence=" + writer.ResultPath);
            writer.RecordStage(StateName(state), "Blocked", details, saveLoadOrdinal, observation, now);
            if (!cleanupVerified)
                writer.SetCleanup(observation, returnedToTitleObserved, verified: false, "Blocked before cleanup verification: " + details);
            writer.Complete("Blocked", code, details, now);
            access.SetHookStatus(Batch6AutoFishingPilotSettings.OverallHookId, "blocked", "fail-closed product QA boundary", terminalDetails);
            access.Log(Batch6AutoFishingPilotSettings.OverallHookId + " terminal=Blocked " + terminalDetails + ".");
        }

        private void Fail(string code, string details, Batch6AutoFishingObservation? observation, DateTimeOffset now)
        {
            Batch6AutoFishingCompatibilityDriverCleanup? compatibilityCleanup = null;
            Batch6AutoFishingProductRecoveryCleanup? recoveryCleanup = null;
            string cleanupFailure = string.Empty;
            try
            {
                if (compatibilityDriver?.Started == true)
                    compatibilityCleanup = compatibilityDriver.Cleanup("failure:" + code);
            }
            catch (Exception ex)
            {
                cleanupFailure = " compatibilityCleanup=" + ex.GetType().Name + ":" + ex.Message;
            }
            try
            {
                if (productRecoveryDriver != null)
                    recoveryCleanup = productRecoveryDriver.Cleanup("failure:" + code);
            }
            catch (Exception ex)
            {
                cleanupFailure += " recoveryCleanup=" + ex.GetType().Name + ":" + ex.Message;
            }
            try { observation = observer.Observe(access.Runtime, DateTimeOffset.UtcNow); }
            catch { }
            state = PilotState.Failed;
            stateEnteredAtUtc = now;
            string finalDetails = details + cleanupFailure;
            terminalDetails = Details("status=Failed; failureCode=" + code + "; " + finalDetails + "; evidence=" + writer.ResultPath);
            writer.RecordStage(StateName(state), "Failed", finalDetails, saveLoadOrdinal, observation, now);
            bool productCleanup = observation == null ? false : !observation.ProductPresent || IsInactive(observation);
            bool cleanup = productCleanup && string.IsNullOrEmpty(cleanupFailure) &&
                (compatibilityCleanup?.Verified ?? true) && (recoveryCleanup?.Verified ?? true);
            if (compatibilityCleanup != null && observation != null)
                writer.SetCompatibilityCleanup(observation, compatibilityCleanup, returnedToTitleObserved);
            else if (recoveryCleanup != null && observation != null)
                writer.SetProductRecoveryCleanup(observation, recoveryCleanup, returnedToTitleObserved);
            else
                writer.SetCleanup(observation, returnedToTitleObserved, cleanup, cleanup ? "Failure terminal retained inactive/absent product state." : "Failure cleanup could not prove all product and QA-owner roots inactive.");
            writer.Complete("Failed", code, finalDetails, now);
            access.SetHookStatus(Batch6AutoFishingPilotSettings.OverallHookId, "failed", "fail-closed product QA boundary", terminalDetails);
            access.SetHookStatus(Batch6AutoFishingPilotSettings.CleanupHookId, cleanup ? "verified" : "failed", "failure cleanup observation", Details(writer.Evidence.Cleanup.Details));
            access.Log(Batch6AutoFishingPilotSettings.OverallHookId + " terminal=Failed " + terminalDetails + ".");
        }

        private void FailFromException(string code, Exception ex)
        {
            Batch6AutoFishingObservation? observation = null;
            try { observation = observer.Observe(access.Runtime, DateTimeOffset.UtcNow); }
            catch { }
            Fail(code, ex.GetType().Name + ": " + ex.Message, observation, DateTimeOffset.UtcNow);
        }

        private string Details(string value) =>
            "case=" + Batch6AutoFishingPilotSettings.CaseId +
            "; runId=" + access.RunId +
            "; level=" + settings.Level +
            "; scenario=" + settings.Scenario +
            "; measureSeconds=" + settings.MeasureSeconds.ToString(CultureInfo.InvariantCulture) +
            "; sampleSeconds=" + settings.SampleSeconds.ToString(CultureInfo.InvariantCulture) +
            "; warmupFish=" + settings.WarmupFish.ToString(CultureInfo.InvariantCulture) +
            "; targetFish=" + settings.TargetFish.ToString(CultureInfo.InvariantCulture) +
            "; multiplier=" + settings.Multiplier.ToString("0.###", CultureInfo.InvariantCulture) +
            "; contract=" + settings.FormalContractKind +
            "; castChargeRatio=" + settings.CastChargeRatio.ToString("0.###", CultureInfo.InvariantCulture) +
            "; manualMovementCancel=" + settings.ManualMovementCancel.ToString().ToLowerInvariant() +
            "; ForcedGc=false; state=" + StateName(state) + "; " + value;

        private static string StateName(PilotState value)
        {
            switch (value)
            {
                case PilotState.WaitingForSaveLoaded: return "waiting-save-loaded";
                case PilotState.WaitingForNativeFishingContext: return "waiting-native-fishing-context";
                case PilotState.L0WarmingUp: return "l0-warming-up";
                case PilotState.L0Measuring: return "l0-measuring";
                case PilotState.WaitingForInitialDirectNeutralPreflight: return "waiting-initial-direct-neutral-preflight";
                case PilotState.WaitingForInitialEnable: return "awaiting-initial-enable-toggle";
                case PilotState.WaitingForManualMovementCancel: return "awaiting-manual-movement-cancel";
                case PilotState.WaitingForManualMovementReenable: return "awaiting-manual-movement-reenable";
                case PilotState.WarmingUp: return "warming-up";
                case PilotState.Measuring: return "measuring";
                case PilotState.WaitingForInitialDisable: return "awaiting-disable-toggle";
                case PilotState.L4Recovering: return "l4-qa-product-native-recovery";
                case PilotState.WaitingForTitle: return "awaiting-title";
                case PilotState.WaitingForReentry: return "awaiting-fifth-save-reentry";
                case PilotState.WaitingForReentryDirectNeutralPreflight: return "waiting-reentry-direct-neutral-preflight";
                case PilotState.WaitingForReentryEnable: return "awaiting-reentry-enable-toggle";
                case PilotState.ReentryActive: return "reentry-active";
                case PilotState.WaitingForFinalDisable: return "awaiting-final-disable-toggle";
                case PilotState.Passed: return "passed";
                case PilotState.Blocked: return "blocked";
                case PilotState.Failed: return "failed";
                default: return value.ToString();
            }
        }
    }

    internal enum Batch6AutoFishingDirectNeutralPreflightStatus
    {
        WaitingForDirectNeutral,
        WaitingForStableNeutral,
        Ready
    }

    internal sealed class Batch6AutoFishingDirectNeutralPreflightGate
    {
        private const float NativePreBaseVelocityThreshold = 0.001f;
        private readonly int stabilityMilliseconds;
        private DateTimeOffset? lastObservedAtUtc;
        private DateTimeOffset? neutralStartedAtUtc;
        private bool baselineCaptured;
        private int baselineSaveLoadOrdinal;
        private string baselineRoomType = string.Empty;
        private string baselineRoomId = string.Empty;
        private string baselineSceneRawName = string.Empty;
        private double baselinePositionX;
        private double baselinePositionY;
        private double baselinePositionZ;
        private int baselineCellX;
        private int baselineCellY;

        internal Batch6AutoFishingDirectNeutralPreflightGate(int stabilityMilliseconds)
        {
            if (stabilityMilliseconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(stabilityMilliseconds));
            this.stabilityMilliseconds = stabilityMilliseconds;
        }

        internal bool DirectNeutralPreconditionObserved { get; private set; }
        internal int LoadContactPendingObservationCount { get; private set; }
        internal int NeutralObservationCount { get; private set; }
        internal double LastInputMultiplier { get; private set; }
        internal double LastVelocityX { get; private set; }
        internal double LastNativeOffsetX { get; private set; }
        internal double LastConveyorOffsetX { get; private set; }
        internal double LastConveyorOffsetY { get; private set; }
        internal double LastVelocityY { get; private set; }
        internal Batch6AutoFishingDirectNeutralPreflightStatus Status { get; private set; } = Batch6AutoFishingDirectNeutralPreflightStatus.WaitingForDirectNeutral;

        internal string Details =>
            "directNeutralPreconditionObserved=" + DirectNeutralPreconditionObserved.ToString().ToLowerInvariant() +
            "; preflightInputObserved=false" +
            "; baselineCaptured=" + baselineCaptured.ToString().ToLowerInvariant() +
            "; status=" + Status +
            "; loadContactPendingObservationCount=" + LoadContactPendingObservationCount.ToString(CultureInfo.InvariantCulture) +
            "; stableNeutralMilliseconds=" + StableNeutralMilliseconds.ToString("0.###", CultureInfo.InvariantCulture) +
            "; requiredNeutralMilliseconds=" + stabilityMilliseconds.ToString(CultureInfo.InvariantCulture) +
            "; neutralObservationCount=" + NeutralObservationCount.ToString(CultureInfo.InvariantCulture) +
            "; baselinePosition=" + baselinePositionX.ToString("R", CultureInfo.InvariantCulture) + "," +
                baselinePositionY.ToString("R", CultureInfo.InvariantCulture) + "," +
                baselinePositionZ.ToString("R", CultureInfo.InvariantCulture) +
            "; baselineCell=" + baselineCellX.ToString(CultureInfo.InvariantCulture) + "," + baselineCellY.ToString(CultureInfo.InvariantCulture) +
            "; inputMultiplier=" + LastInputMultiplier.ToString("R", CultureInfo.InvariantCulture) +
            "; productNativeOffsetX=" + LastNativeOffsetX.ToString("R", CultureInfo.InvariantCulture) +
            "; conveyorOffset=" + LastConveyorOffsetX.ToString("R", CultureInfo.InvariantCulture) + "," + LastConveyorOffsetY.ToString("R", CultureInfo.InvariantCulture) +
            "; rigidbodyVelocity=" + LastVelocityX.ToString("R", CultureInfo.InvariantCulture) + "," + LastVelocityY.ToString("R", CultureInfo.InvariantCulture);

        internal double StableNeutralMilliseconds => neutralStartedAtUtc.HasValue && lastObservedAtUtc.HasValue
            ? Math.Max(0d, (lastObservedAtUtc.Value - neutralStartedAtUtc.Value).TotalMilliseconds)
            : 0d;

        internal Batch6AutoFishingDirectNeutralPreflightStatus Observe(
            DateTimeOffset observedAtUtc,
            bool preflightNativeMovementRefreshed,
            bool nativeMovementAvailable,
            double inputMultiplier,
            double velocityX,
            double offsetX,
            Batch6AutoFishingNativeSurfaceReceipt nativeSurface)
        {
            if (!preflightNativeMovementRefreshed)
                throw new InvalidDataException("Direct-neutral preflight did not execute the bounded inactive native-state refresh.");
            if (!nativeMovementAvailable)
                throw new InvalidDataException("Direct-neutral preflight cannot observe the current ProductNative movement owner.");
            if (double.IsNaN(inputMultiplier) || double.IsInfinity(inputMultiplier) ||
                double.IsNaN(velocityX) || double.IsInfinity(velocityX) ||
                double.IsNaN(offsetX) || double.IsInfinity(offsetX))
            {
                throw new InvalidDataException("Direct-neutral preflight observed non-finite ProductNative movement values.");
            }
            if (inputMultiplier != 0d || Math.Abs(velocityX) > NativePreBaseVelocityThreshold || offsetX != 0d)
            {
                throw new InvalidDataException(
                    "Direct-neutral preflight requires native-parity ProductNative movement without a preflight input; actualInputMultiplier=" +
                    inputMultiplier.ToString("R", CultureInfo.InvariantCulture) +
                    "; actualVelocityX=" + velocityX.ToString("R", CultureInfo.InvariantCulture) +
                    "; nativePreBaseVelocityThreshold=" + ((double)NativePreBaseVelocityThreshold).ToString("R", CultureInfo.InvariantCulture) +
                    "; actualOffsetX=" + offsetX.ToString("R", CultureInfo.InvariantCulture) + ".");
            }
            if (lastObservedAtUtc.HasValue && observedAtUtc < lastObservedAtUtc.Value)
                throw new InvalidDataException("Direct-neutral preflight observations moved backwards in time.");

            bool ordinaryGround = ValidateNativeSurface(nativeSurface);
            if (nativeSurface.InputMultiplier != inputMultiplier ||
                nativeSurface.RigidbodyVelocityX != velocityX ||
                nativeSurface.ConveyorOffsetX != offsetX)
            {
                throw new InvalidDataException("Direct-neutral preflight ProductNative movement does not match the direct native surface.");
            }

            if (!baselineCaptured)
            {
                CaptureBaseline(nativeSurface);
                baselineCaptured = true;
            }

            lastObservedAtUtc = observedAtUtc;
            LastInputMultiplier = inputMultiplier;
            LastVelocityX = velocityX;
            LastNativeOffsetX = offsetX;
            LastConveyorOffsetX = nativeSurface.ConveyorOffsetX;
            LastConveyorOffsetY = nativeSurface.ConveyorOffsetY;
            LastVelocityY = nativeSurface.RigidbodyVelocityY;
            if (!ordinaryGround)
            {
                if (DirectNeutralPreconditionObserved)
                    throw new InvalidDataException("Direct-neutral preflight lost ordinary ground contact after the native-parity neutral stability window began.");
                LoadContactPendingObservationCount++;
                neutralStartedAtUtc = null;
                NeutralObservationCount = 0;
                Status = Batch6AutoFishingDirectNeutralPreflightStatus.WaitingForDirectNeutral;
                return Status;
            }
            if (!DirectNeutralPreconditionObserved)
            {
                DirectNeutralPreconditionObserved = true;
                neutralStartedAtUtc = observedAtUtc;
                NeutralObservationCount = 1;
                Status = Batch6AutoFishingDirectNeutralPreflightStatus.WaitingForStableNeutral;
                return Status;
            }

            NeutralObservationCount++;
            Status = NeutralObservationCount >= 2 && StableNeutralMilliseconds >= stabilityMilliseconds
                ? Batch6AutoFishingDirectNeutralPreflightStatus.Ready
                : Batch6AutoFishingDirectNeutralPreflightStatus.WaitingForStableNeutral;
            return Status;
        }

        private bool ValidateNativeSurface(Batch6AutoFishingNativeSurfaceReceipt receipt)
        {
            if (receipt == null)
                throw new ArgumentNullException(nameof(receipt));
            if (!receipt.Verified || !string.IsNullOrEmpty(receipt.Error))
                throw new InvalidDataException("Direct-neutral preflight requires a complete verified native-surface receipt: " + receipt.Error);
            if (!string.Equals(receipt.InputContext, "Gameplay", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Direct-neutral preflight requires the native Gameplay input context.");
            if (!string.Equals(receipt.AgentStateType, "DolocTown.AgentStateIdle", StringComparison.Ordinal))
                throw new InvalidDataException("Direct-neutral preflight requires DolocTown.AgentStateIdle.");
            if (!receipt.AgentFaceRight)
                throw new InvalidDataException("Direct-neutral preflight requires the untouched fifth-save load position to already face right.");
            RequireFinite(receipt.AgentPositionX, "AgentPosition.x");
            RequireFinite(receipt.AgentPositionY, "AgentPosition.y");
            RequireFinite(receipt.AgentPositionZ, "AgentPosition.z");
            RequireFinite(receipt.InputMultiplier, "MoveModifier.inputMultiplier");
            RequireFinite(receipt.ConveyorOffsetX, "MoveModifier.conveyorOffset.x");
            RequireFinite(receipt.ConveyorOffsetY, "MoveModifier.conveyorOffset.y");
            RequireFinite(receipt.RigidbodyVelocityX, "Rigidbody.velocity.x");
            RequireFinite(receipt.RigidbodyVelocityY, "Rigidbody.velocity.y");
            if (receipt.InputMultiplier != 0d || receipt.ConveyorOffsetX != 0d || receipt.ConveyorOffsetY != 0d ||
                Math.Abs(receipt.RigidbodyVelocityX) > NativePreBaseVelocityThreshold || receipt.RigidbodyVelocityY != 0d)
            {
                throw new InvalidDataException("Direct-neutral preflight requires exact native input/offset, exact-zero Rigidbody Y, and Rigidbody X within the native Wait pre-base threshold.");
            }
            if (!receipt.ConveyorPlatformsEnumerated || receipt.ConveyorPlatforms == null ||
                receipt.ConveyorPlatformInstanceCount != receipt.ConveyorPlatforms.Count ||
                receipt.ConveyorPlatformInstanceCount != 0 || receipt.ConveyorPlatformTouchedCount != 0 || receipt.ConveyorPlatformStayCount != 0)
            {
                throw new InvalidDataException("Direct-neutral preflight requires a complete empty active/touched/stay ConveyorPlatform enumeration.");
            }
            if (string.IsNullOrEmpty(receipt.RoomType) || string.IsNullOrEmpty(receipt.RoomId) || string.IsNullOrEmpty(receipt.SceneRawName))
                throw new InvalidDataException("Direct-neutral preflight requires stable room and scene identity.");

            if (baselineCaptured &&
                (receipt.SaveLoadOrdinal != baselineSaveLoadOrdinal ||
                 !string.Equals(receipt.RoomType, baselineRoomType, StringComparison.Ordinal) ||
                 !string.Equals(receipt.RoomId, baselineRoomId, StringComparison.Ordinal) ||
                 !string.Equals(receipt.SceneRawName, baselineSceneRawName, StringComparison.Ordinal) ||
                 receipt.AgentPositionX != baselinePositionX || receipt.AgentPositionY != baselinePositionY || receipt.AgentPositionZ != baselinePositionZ ||
                 receipt.AgentCellX != baselineCellX || receipt.AgentCellY != baselineCellY))
            {
                throw new InvalidDataException("Direct-neutral preflight position, cell, room, or save-load ordinal changed during the no-input load-contact or stability boundary.");
            }

            bool ordinaryGround = receipt.GroundTouched && receipt.GroundTouchedExcludePlatforms &&
                receipt.GroundPlatformCount == 0 && !receipt.TouchWall;
            bool boundedLoadContactPending = !receipt.GroundTouched && !receipt.GroundTouchedExcludePlatforms &&
                receipt.GroundPlatformCount == 0 && !receipt.TouchWall;
            if (!ordinaryGround && !boundedLoadContactPending)
                throw new InvalidDataException("Direct-neutral preflight requires ordinary ground contact or the exact same-position no-contact loading state, with no platform or wall contact.");
            return ordinaryGround;
        }

        private void CaptureBaseline(Batch6AutoFishingNativeSurfaceReceipt receipt)
        {
            baselineSaveLoadOrdinal = receipt.SaveLoadOrdinal;
            baselineRoomType = receipt.RoomType;
            baselineRoomId = receipt.RoomId;
            baselineSceneRawName = receipt.SceneRawName;
            baselinePositionX = receipt.AgentPositionX;
            baselinePositionY = receipt.AgentPositionY;
            baselinePositionZ = receipt.AgentPositionZ;
            baselineCellX = receipt.AgentCellX;
            baselineCellY = receipt.AgentCellY;
        }

        private static void RequireFinite(double value, string label)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new InvalidDataException("Direct-neutral preflight observed non-finite " + label + ".");
        }
    }
}
