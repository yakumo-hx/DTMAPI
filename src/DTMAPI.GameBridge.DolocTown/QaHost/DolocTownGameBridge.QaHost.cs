using System;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    public sealed partial class DolocTownGameBridge
    {
        private readonly PreparedQaHost? preparedQaHost;
        private IQaHostParticipant? qaHostParticipant;
        private bool qaHostStarted;
        private bool qaHostClosed;
        private bool qaHostClosing;
        private bool qaHostPreparedLifecyclePublished;
        private bool qaHostFailureObserved;
        private bool qaHostPreRuntimePrepared;
        private bool qaHostApplicationQuitRequested;
        private bool qaHostObservedSaveLoaded;
        private bool qaHostInitialSaveLoadAccepted;
        private int qaHostUpdateCount;
        private Action? qaHostFrameUpdate;
        private Action<int?>? qaHostSaveSavedNotification;
        private Action? qaHostWorkshopReloadNotification;
        private Action<string>? qaHostUiObservationNotification;
        private Func<int, bool>? fixtureInitialSaveLoadRequestOverrideForTests;
        private Action? fixtureInitialSaveLoadContinuationOverrideForTests;

        internal bool QaHostAttachedForTests => qaHostParticipant != null;

        internal bool QaHostStartedForTests => qaHostStarted;

        internal bool QaHostClosedForTests => qaHostClosed;

        internal bool QaHostClosingForTests => qaHostClosing;

        internal bool QaHostFailureObservedForTests => qaHostFailureObserved;

        internal int QaHostUpdateCountForTests => qaHostUpdateCount;

        internal Action<int?>? QaHostSaveSavedNotification => qaHostSaveSavedNotification;

        internal Action? QaHostWorkshopReloadNotification => qaHostWorkshopReloadNotification;

        internal Action<string>? QaHostUiObservationNotification => qaHostUiObservationNotification;

        internal Func<int, bool>? FixtureInitialSaveLoadRequestOverrideForTests
        {
            set => fixtureInitialSaveLoadRequestOverrideForTests = value;
        }

        internal Action? FixtureInitialSaveLoadContinuationOverrideForTests
        {
            set => fixtureInitialSaveLoadContinuationOverrideForTests = value;
        }

        internal void StartQaHostForTests() => StartQaHostParticipant();

        internal void UpdateQaHostForTests() => UpdateQaHostParticipant();

        internal void CloseQaHostForTests(string reason) => CloseQaHostParticipant(reason);

        internal void CloseQaHostBeforeFixtureExit(string reason) =>
            CloseQaHostParticipant("fixture-exit:" + (reason ?? string.Empty));

        internal void PrepareQaHostBeforeRuntimeStart()
        {
            if (preparedQaHost == null)
                return;
            if (qaHostParticipant == null || qaHostClosed || qaHostClosing || qaHostStarted || qaHostPreRuntimePrepared)
                throw new InvalidOperationException("The optional QA pre-Runtime boundary is unavailable or was already consumed.");

            if (preparedQaHost.StartupOptions.RequirePreRuntimeSaveIsolation)
            {
                if (!(qaHostParticipant is IQaHostPreRuntimeParticipant preRuntimeParticipant))
                    throw new InvalidOperationException("The validated QA participant requires disposable save isolation but exposes no pre-Runtime guard contract.");
                preRuntimeParticipant.PrepareBeforeRuntimeStart();
            }
            qaHostPreRuntimePrepared = true;
        }

        internal void AbortQaHostBeforeRuntimeStart(string reason)
        {
            if (preparedQaHost == null || qaHostClosed)
                return;
            qaHostFailureObserved = true;
            CloseQaHostParticipant("failure:pre-runtime:" + (reason ?? string.Empty));
        }

        private void PublishQaHostPreparedLifecycle()
        {
            if (preparedQaHost == null || qaHostPreparedLifecyclePublished)
                return;
            if (qaHostParticipant == null || qaHostClosed || qaHostClosing)
                throw new InvalidOperationException("The validated optional fixture participant was not attached before Runtime initialization.");
            if (!qaHostPreRuntimePrepared)
                throw new InvalidOperationException("The validated optional fixture participant did not cross its pre-Runtime boundary before Runtime initialization.");

            qaHostPreparedLifecyclePublished = true;
            PublishQaHostLifecycle("validated", "protocol=" + QaHostProtocol.ProtocolVersion + "; hashBound=true; fallback=false");
            PublishQaHostLifecycle("activated", "mode=" + QaHostProtocol.ParticipantOnlyMode + "; protocol=" + QaHostProtocol.ProtocolVersion + "; fallback=false");
            PublishQaHostLifecycle("attached", "participant=" + qaHostParticipant.Id + "; scenarioOwner=qa; fallback=false");
            runtime.SetHookStatus(
                "InternalFixture.QaHost",
                "attached",
                "DolocTownGameBridge optional participant",
                "runId=" + preparedQaHost.PreparationContext.RunId + "; participant=" + qaHostParticipant.Id + "; scenarioOwner=qa; fallback=false");
        }

        private void PublishQaHostLifecycle(string state, string details)
        {
            if (preparedQaHost == null)
                return;

            runtime.RuntimeMonitor.Log(
                "QA host lifecycle runId=" + preparedQaHost.PreparationContext.RunId +
                "; state=" + (state ?? string.Empty) +
                "; " + (details ?? string.Empty));
        }

        private void AttachQaHostParticipant()
        {
            if (preparedQaHost == null)
                return;
            if (qaHostParticipant != null || qaHostClosed || qaHostClosing)
                throw new InvalidOperationException("The optional fixture participant may be attached only once.");

            qaHostParticipant = preparedQaHost.Factory.CreateParticipant(
                new GameBridgeFixtureAccess(
                    runtime,
                    preparedQaHost.PreparationContext.RunId,
                    preparedQaHost.PreparationContext.EvidenceRoot,
                    () => initializedAt != default(DateTimeOffset),
                    this));
            if (qaHostParticipant == null)
                throw new InvalidOperationException("The optional fixture factory returned no participant.");

            runtime.SaveSessionLoaded += OnQaHostSaveLoaded;
            runtime.ReturnedToTitleBoundary += OnQaHostReturnedToTitle;
            qaHostFrameUpdate = UpdateQaHostParticipant;
            qaHostSaveSavedNotification = NotifyQaHostSaveSaved;
            qaHostWorkshopReloadNotification = NotifyQaHostWorkshopReloadCompleted;
            qaHostUiObservationNotification = NotifyQaHostUiObservation;
        }

        private void StartQaHostParticipant()
        {
            if (qaHostParticipant == null || qaHostStarted || qaHostClosed || qaHostClosing)
                return;

            try
            {
                qaHostParticipant.Start();
                qaHostStarted = true;
                GameBridgeDemandRoutes.SetOwnerDemand(runtime, GameBridgeDemandRoutes.QaHost, GameBridgeDemandRoutes.QaOwner, RuntimeDemandSourceType.ExplicitQa, RuntimeDemandLifetime.Session, "participant", true, "QA host participant started");
                PublishQaHostLifecycle("started", "participant=" + qaHostParticipant.Id + "; startCount=1; fallback=false");
                runtime.SetHookStatus("InternalFixture.QaHost", "started", "DolocTownGameBridge optional participant", "participant=" + qaHostParticipant.Id + "; startCount=1; fallback=false");
            }
            catch (Exception ex)
            {
                FailQaHost("start", ex);
                throw;
            }
        }

        private void UpdateQaHostParticipant()
        {
            IQaHostParticipant? participant = qaHostParticipant;
            if (participant == null || !qaHostStarted || qaHostClosed || qaHostClosing)
                return;

            try
            {
                participant.Update();
                qaHostUpdateCount++;
                if (qaHostUpdateCount == 1)
                {
                    PublishQaHostLifecycle("updated", "participant=" + participant.Id + "; updateCount=1; fallback=false");
                    runtime.SetHookStatus("InternalFixture.QaHost", "updated", "DolocTownGameBridge optional participant", "participant=" + participant.Id + "; updateCount=1; fallback=false");
                }
                if (qaHostInitialSaveLoadAccepted && !qaHostObservedSaveLoaded)
                {
                    if (fixtureInitialSaveLoadContinuationOverrideForTests != null)
                        fixtureInitialSaveLoadContinuationOverrideForTests();
                    else
                        (participant as IQaHostNativeDriver)?.ContinueInitialSaveLoad();
                }
                switch (participant.GetRunDisposition())
                {
                    case QaHostRunDisposition.RequestInitialSaveLoad:
                        if (!qaHostInitialSaveLoadAccepted)
                        {
                            int saveSlot = preparedQaHost?.StartupOptions.SaveSlot ?? 0;
                            qaHostInitialSaveLoadAccepted = fixtureInitialSaveLoadRequestOverrideForTests?.Invoke(saveSlot)
                                ?? ((participant as IQaHostNativeDriver)?.TryRequestInitialSaveLoad(saveSlot)
                                    ?? throw new InvalidOperationException("The optional QA participant requested a native save load without implementing the native driver contract."));
                        }
                        break;
                    case QaHostRunDisposition.RequestReturnHome:
                        RequestReturnHomeFromQaHost();
                        break;
                    case QaHostRunDisposition.RequestQuit:
                        RequestApplicationQuitFromQaHost("optional QA participant reached its terminal state");
                        break;
                }
            }
            catch (Exception ex)
            {
                FailQaHost("update", ex);
                RequestApplicationQuitFromQaHost("optional QA participant failed during update");
            }
        }

        private void OnQaHostSaveLoaded(int? slot, bool isNewGame)
        {
            IQaHostParticipant? participant = qaHostParticipant;
            if (participant == null || qaHostClosed || qaHostClosing)
                return;

            try
            {
                // A startup title notification is a receipt for the pre-save
                // generation only. SaveLoaded starts a new lifecycle generation;
                // do not replay that old title receipt from Update.
                qaHostObservedSaveLoaded = true;
                participant.OnSaveLoaded(slot, isNewGame);
            }
            catch (Exception ex)
            {
                FailQaHost("save-loaded", ex);
            }
        }

        internal void NotifyQaHostSaveSaved(int? slot)
        {
            IQaHostParticipant? participant = qaHostParticipant;
            if (participant == null || qaHostClosed || qaHostClosing)
                return;

            try
            {
                participant.OnSaveSaved(slot);
            }
            catch (Exception ex)
            {
                FailQaHost("save-saved", ex);
            }
        }

        internal void NotifyQaHostWorkshopReloadCompleted()
        {
            IQaHostParticipant? participant = qaHostParticipant;
            if (participant == null || qaHostClosed || qaHostClosing)
                return;

            try
            {
                participant.OnWorkshopReloadCompleted();
            }
            catch (Exception ex)
            {
                FailQaHost("workshop-reload-completed", ex);
            }
        }

        private void OnQaHostReturnedToTitle()
        {
            IQaHostParticipant? participant = qaHostParticipant;
            if (participant == null || qaHostClosed || qaHostClosing)
                return;

            try
            {
                if (participant.OnReturnedToTitle(qaHostObservedSaveLoaded) == QaHostBoundaryDisposition.Close)
                    RequestApplicationQuitFromQaHost("optional QA participant completed at the title boundary");
            }
            catch (Exception ex)
            {
                FailQaHost("returned-to-title", ex);
            }
        }

        internal void NotifyQaHostUiObservation(string source)
        {
            IQaHostParticipant? participant = qaHostParticipant;
            if (participant == null || qaHostClosed || qaHostClosing)
                return;
            (participant as IQaHostNativeDriver)?.OnUiObservation(source ?? string.Empty);
        }

        private void FailQaHost(string operation, Exception ex)
        {
            qaHostFailureObserved = true;
            runtime.Diagnostics.RecordError("DTMAPI.InternalFixtureHost", "Optional fixture participant failed during " + operation + ".", ex.ToString());
            PublishQaHostLifecycle("failed", "operation=" + operation + "; errorType=" + ex.GetType().Name + "; error=" + ex.Message.Replace("\r", " ").Replace("\n", " ") + "; fallback=false");
            runtime.SetHookStatus("InternalFixture.QaHost", "failed", "DolocTownGameBridge optional participant", "operation=" + operation + "; error=" + ex.GetType().Name + ": " + ex.Message + "; fallback=false");
            if (qaHostPreRuntimePrepared)
            {
                RetainQaHostForProcessExit("failure:" + operation);
                RequestApplicationQuitWithoutQaHostClose("optional QA participant failed during " + operation + "; pre-Runtime owner retained until process shutdown");
                return;
            }
            CloseQaHostParticipant("failure:" + operation);
        }

        private void RetainQaHostForProcessExit(string reason)
        {
            if (qaHostClosed)
                return;
            if (!qaHostClosing)
            {
                qaHostClosing = true;
                qaHostFrameUpdate = null;
                qaHostSaveSavedNotification = null;
                qaHostWorkshopReloadNotification = null;
                qaHostUiObservationNotification = null;
                runtime.SaveSessionLoaded -= OnQaHostSaveLoaded;
                runtime.ReturnedToTitleBoundary -= OnQaHostReturnedToTitle;
            }
            PublishQaHostLifecycle("failed-retained", "reason=" + (reason ?? string.Empty) + "; participantRetained=" + (qaHostParticipant != null).ToString().ToLowerInvariant() + "; releaseBoundary=process-shutdown; fallback=false");
            runtime.SetHookStatus(
                "InternalFixture.QaHost",
                "failed-retained",
                "DolocTownGameBridge optional participant",
                "reason=" + (reason ?? string.Empty) + "; participantRetained=" + (qaHostParticipant != null).ToString().ToLowerInvariant() + "; releaseBoundary=process-shutdown; fallback=false");
        }

        private void CloseQaHostParticipant(string reason)
        {
            if (qaHostClosed)
                return;

            GameBridgeDemandRoutes.SetOwnerDemand(runtime, GameBridgeDemandRoutes.QaHost, GameBridgeDemandRoutes.QaOwner, RuntimeDemandSourceType.ExplicitQa, RuntimeDemandLifetime.Session, "participant", false, "QA host participant closing " + (reason ?? string.Empty));

            if (!qaHostClosing)
            {
                qaHostClosing = true;
                qaHostFrameUpdate = null;
                qaHostSaveSavedNotification = null;
                qaHostWorkshopReloadNotification = null;
                qaHostUiObservationNotification = null;
                runtime.SaveSessionLoaded -= OnQaHostSaveLoaded;
                runtime.ReturnedToTitleBoundary -= OnQaHostReturnedToTitle;
            }
            IQaHostParticipant? participant = qaHostParticipant;
            bool participantCloseSucceeded = true;

            if (participant != null)
            {
                try
                {
                    participant.Close(reason ?? string.Empty);
                }
                catch (Exception ex)
                {
                    participantCloseSucceeded = false;
                    qaHostFailureObserved = true;
                    runtime.Diagnostics.RecordError("DTMAPI.InternalFixtureHost", "Optional fixture participant close failed.", ex.ToString());
                    PublishQaHostLifecycle("cleanup-failed", "operation=close; errorType=" + ex.GetType().Name + "; retainedParticipant=true; retryable=true; fallback=false");
                }
            }

            if (!participantCloseSucceeded)
            {
                runtime.SetHookStatus(
                    "InternalFixture.QaHost",
                    "cleanup-failed",
                    "DolocTownGameBridge optional participant",
                    "reason=" + (reason ?? string.Empty) + "; participantRetained=" + (participant != null).ToString().ToLowerInvariant() + "; retryable=true; listeners=0; fallback=false");
                return;
            }

            qaHostParticipant = null;
            qaHostClosed = true;
            qaHostClosing = false;

            string closeDetails =
                "reason=" + (reason ?? string.Empty) +
                "; startCount=" + (qaHostStarted ? "1" : "0") +
                "; updateCount=" + qaHostUpdateCount +
                "; closeCount=1; listeners=0; participant=null; participantCloseSucceeded=true" +
                "; failureObserved=" + qaHostFailureObserved.ToString().ToLowerInvariant() +
                "; fallback=false";
            PublishQaHostLifecycle("closed", closeDetails);
            runtime.SetHookStatus(
                "InternalFixture.QaHost",
                qaHostFailureObserved ? "failed-closed" : "closed",
                "DolocTownGameBridge optional participant",
                closeDetails);
        }
    }
}
