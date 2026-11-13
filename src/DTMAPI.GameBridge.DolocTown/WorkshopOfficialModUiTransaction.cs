using System;
using DTMAPI.Abstractions;

namespace DTMAPI.GameBridge.DolocTown
{
    public sealed partial class DolocTownGameBridge
    {
        private object? workshopModUiInstance;
        private object? workshopModUiManager;
        private NativeWorkshopSubscriptionSnapshot? workshopModUiCloseCandidate;
        private NativeWorkshopSubscriptionSnapshot? pendingWorkshopModUiCommit;
        private long workshopModUiTransactionGeneration;
        private bool workshopModUiCloseRequested;
        private bool workshopModUiSaveObserved;
        private bool workshopModUiSaveSucceeded;

        internal void BeginOfficialModUiTransaction(object uiInstance)
        {
            workshopModUiTransactionGeneration++;
            workshopModUiInstance = uiInstance;
            workshopModUiManager = null;
            workshopModUiCloseCandidate = null;
            workshopModUiCloseRequested = false;
            workshopModUiSaveObserved = false;
            workshopModUiSaveSucceeded = false;

            runtime.RuntimeMonitor.Log(
                "Official Mod UI source transaction opened; native ReloadMods is preview-only until a successful close save.",
                LogLevel.Debug);
        }

        internal void BeginOfficialModUiClose(object uiInstance)
        {
            if (!ReferenceEquals(workshopModUiInstance, uiInstance))
                return;
            workshopModUiCloseRequested = true;
            workshopModUiCloseCandidate = null;
            workshopModUiSaveObserved = false;
            workshopModUiSaveSucceeded = false;
        }

        internal void HandleNativeModManagerReloaded(
            object modManager,
            bool requireSteamInitialized = true)
        {
            bool transactionOwned;
            bool closing;
            long generation;
            transactionOwned = workshopModUiInstance != null &&
                (workshopModUiManager == null || ReferenceEquals(workshopModUiManager, modManager));
            if (transactionOwned && workshopModUiManager == null)
                workshopModUiManager = modManager;
            closing = transactionOwned && workshopModUiCloseRequested;
            generation = workshopModUiTransactionGeneration;

            if (!transactionOwned)
            {
                CaptureNativeWorkshopSubscriptions("ModManager.ReloadMods.Postfix.External",
                    modManager, requireSteamInitialized);
                return;
            }

            if (!TryCaptureNativeWorkshopSubscriptionSnapshot(
                    closing
                        ? "ModUiState.Hide.ReloadMods.CloseCandidate"
                        : "ModUiState.Register.ReloadMods.Preview",
                    modManager,
                    requireSteamInitialized,
                    publishUnavailableOnFailure: false,
                    out NativeWorkshopSubscriptionSnapshot? snapshot))
            {
                if (generation == workshopModUiTransactionGeneration && closing)
                    workshopModUiCloseCandidate = null;
                return;
            }

            if (generation != workshopModUiTransactionGeneration ||
                !ReferenceEquals(workshopModUiManager, modManager))
                return;

            if (closing)
                workshopModUiCloseCandidate = snapshot;

            runtime.RuntimeMonitor.Log(
                closing
                    ? "Official Mod UI close candidate staged; waiting for native SaveModManager and close completion."
                    : "Official Mod UI opening reload observed as preview; committed DTMAPI source authority retained.",
                LogLevel.Debug);
        }

        internal void ObserveOfficialModManagerSave(object modManager, bool succeeded)
        {
            if (!workshopModUiCloseRequested ||
                !ReferenceEquals(workshopModUiManager, modManager))
                return;
            workshopModUiSaveObserved = true;
            workshopModUiSaveSucceeded = succeeded;
        }

        internal void CompleteOfficialModUiClose(object uiInstance)
        {
            bool committed = false;
            bool saveObserved = false;
            bool saveSucceeded = false;
            if (!ReferenceEquals(workshopModUiInstance, uiInstance) ||
                !workshopModUiCloseRequested)
                return;

            saveObserved = workshopModUiSaveObserved;
            saveSucceeded = workshopModUiSaveSucceeded;
            if (saveObserved && saveSucceeded && workshopModUiCloseCandidate != null)
            {
                pendingWorkshopModUiCommit = workshopModUiCloseCandidate;
                committed = true;
            }

            workshopModUiInstance = null;
            workshopModUiManager = null;
            workshopModUiCloseCandidate = null;
            workshopModUiCloseRequested = false;
            workshopModUiSaveObserved = false;
            workshopModUiSaveSucceeded = false;

            runtime.RuntimeMonitor.Log(
                committed
                    ? "Official Mod UI close transaction committed; one DTMAPI source refresh is deferred to the next Runtime frame."
                    : "Official Mod UI close transaction discarded; committed source authority retained saveObserved=" +
                      (saveObserved ? "true" : "false") + "; saveSucceeded=" +
                      (saveSucceeded ? "true" : "false") + ".",
                committed || !saveObserved ? LogLevel.Debug : LogLevel.Warn);
        }

        private void ProcessPendingOfficialModUiCommit()
        {
            NativeWorkshopSubscriptionSnapshot? snapshot;
            snapshot = pendingWorkshopModUiCommit;
            pendingWorkshopModUiCommit = null;
            if (snapshot == null)
                return;

            try
            {
                PublishNativeWorkshopSubscriptionSnapshot(
                    snapshot,
                    "ModUiState.Hide.SuccessfulSave.DeferredCommit");
                runtime.NotifyWorkshopModListChanged();
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError(
                    "DTMAPI.GameBridge",
                    "Official Mod UI committed source refresh failed.",
                    ex.ToString());
            }
        }

    }
}
