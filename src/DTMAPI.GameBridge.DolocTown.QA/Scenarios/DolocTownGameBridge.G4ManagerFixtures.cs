using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using DTMAPI.Core.Manager;
using DTMAPI.Core.Services;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed partial class QaScenarioController
    {
        private const int ManagerUiModsPageSize = 10;
        private bool g4ManagerRouteArmed;
        private bool g4ManagerRouteIsMvp;
        private int g4ManagerStage;
        private string g4ManagerStatusDetails = string.Empty;
        private string g4ManagerExportDetails = string.Empty;
        private string? g4ManagerStatusScreenshotPath;
        private DateTimeOffset g4ManagerStatusScreenshotRequestedAt;
        private string? g4ManagerModsScreenshotPath;
        private DateTimeOffset g4ManagerModsScreenshotRequestedAt;
        private string? g4ManagerAdvancedScreenshotPath;
        private DateTimeOffset g4ManagerAdvancedScreenshotRequestedAt;
        private string? g4ManagerLogsScreenshotPath;
        private DateTimeOffset g4ManagerLogsScreenshotRequestedAt;
        private string g4ManagerInitialModDetailTitle = string.Empty;
        private string g4ManagerSelectedModDetailTitle = string.Empty;
        private string g4ManagerLastPagerText = string.Empty;
        private string g4ManagerWarningViewText = string.Empty;
        private string g4ManagerAdvancedFeatureText = string.Empty;
        private int g4ManagerObservedModPageIndex;
        private string g4ManagerInteractionDetails = string.Empty;

        internal G4FixtureStepResult ObserveManagerStatusUiForFixture(
            string statusScreenshotPath,
            Action<string, string, string, string> publishStatus) =>
            ObserveManagerUiForFixture(false, statusScreenshotPath, string.Empty, string.Empty, string.Empty, publishStatus);

        internal G4FixtureStepResult ObserveManagerMvpUiForFixture(
            string statusScreenshotPath,
            string modsScreenshotPath,
            string advancedScreenshotPath,
            string logsScreenshotPath,
            Action<string, string, string, string> publishStatus) =>
            ObserveManagerUiForFixture(true, statusScreenshotPath, modsScreenshotPath, advancedScreenshotPath, logsScreenshotPath, publishStatus);

        private G4FixtureStepResult ObserveManagerUiForFixture(
            bool managerMvp,
            string statusScreenshotPath,
            string modsScreenshotPath,
            string advancedScreenshotPath,
            string logsScreenshotPath,
            Action<string, string, string, string> publishStatus)
        {
            if (publishStatus == null)
                throw new ArgumentNullException(nameof(publishStatus));
            if (!g4ManagerRouteArmed)
            {
                g4ManagerRouteArmed = true;
                g4ManagerRouteIsMvp = managerMvp;
            }
            else if (g4ManagerRouteIsMvp != managerMvp)
            {
                return G4FixtureStepResult.Failed("The QA Manager route changed after its exact overlay session was armed.");
            }

            switch (g4ManagerStage)
            {
                case 0:
                    return OpenAndObserveManagerStatusForFixture(publishStatus);
                case 1:
                    return CaptureManagerStatusForFixture(statusScreenshotPath, publishStatus);
                case 2:
                    return OpenManagerPageForFixture(DtmOverlayPage.Mods, "Smoke.ManagerModsPage", "Mods", 3, publishStatus);
                case 3:
                    return SelectManagerModRowForFixture(publishStatus);
                case 4:
                    return VerifyManagerModSelectionAndStartPagingForFixture(publishStatus);
                case 5:
                    return TraverseManagerModPagesForFixture(publishStatus);
                case 6:
                    return CaptureManagerModsAndStartPreviousPageForFixture(modsScreenshotPath, publishStatus);
                case 7:
                    return VerifyManagerPreviousPageAndOpenErrorsForFixture(publishStatus);
                case 8:
                    return SelectManagerWarningsForFixture(publishStatus);
                case 9:
                    return VerifyManagerWarningsAndSelectErrorsForFixture(publishStatus);
                case 10:
                    return VerifyManagerErrorsAndOpenHooksForFixture(publishStatus);
                case 11:
                    return OpenManagerAdvancedForFixture(publishStatus);
                case 12:
                    return SelectManagerFeatureEvidenceForFixture(publishStatus);
                case 13:
                    return VerifyManagerFeatureEvidenceAndSelectRegistryForFixture(publishStatus);
                case 14:
                    return VerifyManagerRegistryEvidenceForFixture(publishStatus);
                case 15:
                    return CaptureManagerAdvancedAndOpenLogsForFixture(advancedScreenshotPath, publishStatus);
                case 16:
                    return ExportManagerLogsForFixture(publishStatus);
                case 17:
                    return CaptureManagerLogsAndCloseForFixture(logsScreenshotPath, publishStatus);
                default:
                    return G4FixtureStepResult.Failed("The QA Manager fixture was invoked after its terminal stage.");
            }
        }

        private G4FixtureStepResult OpenAndObserveManagerStatusForFixture(Action<string, string, string, string> publishStatus)
        {
            if (!runtime.UI.InputContext.Equals("HomePageUiState", StringComparison.OrdinalIgnoreCase))
                return G4FixtureStepResult.Pending("Waiting for HomePage before opening the DTMAPI Manager Status page.");
            if (g4TitleOverlaySessionSequence == 0)
            {
                if (runtime.UI.IsOpen)
                    return G4FixtureStepResult.Pending("Waiting for the pre-existing DTMAPI overlay to close; QA will not claim or close it.");
                long before = runtime.UI.OverlaySessionSequence;
                runtime.UI.OpenDtmApiStatusPage();
                if (!runtime.UI.IsOpen || runtime.UI.OverlaySessionSequence <= before)
                    return G4FixtureStepResult.Failed("The Manager Status page did not create a distinct QA-owned overlay session.");
                CaptureTitleOverlayReceipt();
            }
            if (!IsTitleOverlayReceiptCurrent())
                return G4FixtureStepResult.Failed("The QA-owned Manager Status overlay session identity changed before observation completed; unrelated overlay was left untouched.");
            if (!IsExpectedManagerOverlayForFixture(DtmOverlayPage.Status))
                return G4FixtureStepResult.Failed("The QA Manager route did not open the expected DTMAPI.Status session.");

            NativeUiLayoutObservation? layout = NativeUiLayoutRepairService?.CaptureObservation();
            if (!TryValidateNativeUiLayoutObservation(layout, requireHomePage: true, out string repairHooks))
                return G4FixtureStepResult.Failed("The Manager Status layout receipt was not concrete or its production repair hooks were unavailable. " + FormatNativeUiObservation(layout) + "; hooks={" + repairHooks + "}.");
            g4LastVerifiedNativeUiLayoutObservation = layout;
            g4LastVerifiedNativeUiRepairHooks = repairHooks;

            runtime.UI.RefreshDtmManagerModel();
            DtmManagerViewModel? manager = runtime.UI.CurrentManagerModel;
            if (manager == null)
                return G4FixtureStepResult.Failed("The Manager Status refresh returned no model. error=" + runtime.UI.LastManagerRefreshError + ".");
            int modelRows = CountManagerRows(manager);
            if (modelRows <= 0)
                return G4FixtureStepResult.Failed("The Manager Status refresh returned an empty model.");
            if (!string.IsNullOrWhiteSpace(runtime.UI.LastManagerRefreshError))
                return G4FixtureStepResult.Failed("The Manager Status refresh retained an error: " + runtime.UI.LastManagerRefreshError + ".");

            string summary = ManagerPageRowFormatter.FormatStatusSummary(manager, runtime.UI.LastManagerRefreshError, runtime.UI.LastManagerReportExport);
            if (!IsCompleteManagerSummary(summary))
                return G4FixtureStepResult.Failed("The formatted Manager Status summary is incomplete: " + summary + ".");
            string copiedText = string.Empty;
            DtmManagerCopySummaryResult copy = runtime.UI.CopyManagerSummary(text =>
            {
                copiedText = text ?? string.Empty;
                return true;
            });
            if (!copy.Copied || !string.Equals(copy.Text, summary, StringComparison.Ordinal) || !string.Equals(copiedText, summary, StringComparison.Ordinal))
                return G4FixtureStepResult.Failed("CopyManagerSummary did not complete through the in-memory QA callback. status=" + copy.Status + "; error=" + copy.ErrorMessage + ".");

            g4ManagerStatusDetails = "page=Status; modelRows=" + modelRows + "; summaryFormatted=true; copyStatus=" + copy.Status +
                "; clipboard=os-not-used; overlaySession=exact; " + FormatNativeUiObservation(layout) + "; hooks={" + repairHooks + "}";
            runtime.RuntimeMonitor.Log("Smoke automation opened DTMAPI Manager Status page.");
            runtime.RuntimeMonitor.Log("Manager Status summary text OK " + summary + ".");
            runtime.RuntimeMonitor.Log("Manager Status summary copy OK status=" + copy.Status + " text=" + copy.Text + ".");
            PublishManagerFixtureStatus(publishStatus, "Smoke.ManagerStatusPage", "verified", "DTMAPI Manager Status page", "Opened and read back the exact Manager Status title overlay session.");
            PublishManagerFixtureStatus(publishStatus, "Smoke.ManagerStatusSummaryText", "verified", "DTMAPI Manager Status view model", summary);
            PublishManagerFixtureStatus(publishStatus, "Smoke.ManagerStatusSummaryCopy", "verified", "DTMAPI Manager Status Copy Summary", copy.Status + "; " + copy.Text + "; clipboard=os-not-used");
            g4ManagerStage = 1;
            return G4FixtureStepResult.Pending(g4ManagerStatusDetails + "; waitingForStatusScreenshot=true");
        }

        private G4FixtureStepResult CaptureManagerStatusForFixture(
            string screenshotPath,
            Action<string, string, string, string> publishStatus)
        {
            if (!IsTitleOverlayReceiptCurrent() || !IsExpectedManagerOverlayForFixture(DtmOverlayPage.Status))
                return G4FixtureStepResult.Failed("The exact Manager Status overlay session changed before screenshot completion; unrelated overlay was left untouched.");
            G4FixtureStepResult screenshot = ObserveScreenshotForFixture(
                screenshotPath,
                ref g4ManagerStatusScreenshotPath,
                ref g4ManagerStatusScreenshotRequestedAt);
            if (!screenshot.Completed || !screenshot.Succeeded)
                return screenshot;

            runtime.RuntimeMonitor.Log("Manager Status page screenshot OK screenshot=" + g4ManagerStatusScreenshotPath + ".");
            PublishManagerFixtureStatus(publishStatus, "Smoke.ManagerStatusPageScreenshot", "verified", "UnityEngine.ScreenCapture.CaptureScreenshot", "Manager Status page screenshot=" + g4ManagerStatusScreenshotPath + ".");
            if (g4ManagerRouteIsMvp)
            {
                g4ManagerStage = 2;
                return G4FixtureStepResult.Pending(g4ManagerStatusDetails + "; " + screenshot.Details + "; Manager MVP pages pending");
            }

            bool closed = CloseTitleOverlayOwnedByFixture();
            return closed
                ? G4FixtureStepResult.Verified(g4ManagerStatusDetails + "; " + screenshot.Details + "; close=exact-session; route=manager-status")
                : G4FixtureStepResult.Failed("The exact QA-owned Manager Status overlay could not be closed; cleanup remains retryable.");
        }

        private G4FixtureStepResult SelectManagerModRowForFixture(Action<string, string, string, string> publishStatus)
        {
            if (!IsTitleOverlayReceiptCurrent() || !IsExpectedManagerOverlayForFixture(DtmOverlayPage.Mods))
                return G4FixtureStepResult.Failed("The exact Manager Mods overlay changed before row-selection interaction.");
            DtmManagerViewModel? manager = runtime.UI.CurrentManagerModel;
            if (manager == null || manager.Mods.Count < 2)
                return G4FixtureStepResult.Failed("Manager row-selection acceptance requires at least two real Mod rows.");
            if (!TryReadUnityText("DTMAPI.Mods.DetailTitle", out g4ManagerInitialModDetailTitle, out string readFailure))
                return G4FixtureStepResult.Pending("Waiting for the reflected Manager Mod detail title. " + readFailure);
            if (TryReadUnityText("DTMAPI.Title", out string uiTitle, out _) &&
                uiTitle.IndexOf("设置", StringComparison.Ordinal) >= 0 &&
                g4ManagerInitialModDetailTitle.IndexOf("所选 Mod", StringComparison.Ordinal) < 0)
            {
                return G4FixtureStepResult.Failed("Simplified-Chinese Manager still rendered the selected-Mod heading through an English fallback: " + g4ManagerInitialModDetailTitle + ".");
            }
            if (!TryInvokeUnityButton("DTMAPI.Mods.Row.1", out string clickFailure))
                return G4FixtureStepResult.Pending("Waiting for the second reflected Manager Mod row button. " + clickFailure);

            runtime.RuntimeMonitor.Log("Manager player interaction clicked Mod row 2.");
            PublishManagerFixtureStatus(publishStatus, "Smoke.ManagerModRowSelection", "observed", "UnityEngine.UI.Button.onClick", "Clicked DTMAPI.Mods.Row.1; awaiting detail replacement.");
            g4ManagerStage = 4;
            return G4FixtureStepResult.Pending("Manager Mod row click dispatched through the reflected Unity Button.");
        }

        private G4FixtureStepResult VerifyManagerModSelectionAndStartPagingForFixture(Action<string, string, string, string> publishStatus)
        {
            DtmManagerViewModel? manager = runtime.UI.CurrentManagerModel;
            if (manager == null || manager.Mods.Count < 2)
                return G4FixtureStepResult.Failed("Manager Mod selection model disappeared after the row click.");
            if (!TryReadUnityText("DTMAPI.Mods.DetailTitle", out g4ManagerSelectedModDetailTitle, out string readFailure))
                return G4FixtureStepResult.Pending("Waiting for selected-Mod detail replacement. " + readFailure);
            string expectedId = manager.Mods[1].UniqueID;
            if (g4ManagerSelectedModDetailTitle.Equals(g4ManagerInitialModDetailTitle, StringComparison.Ordinal) ||
                g4ManagerSelectedModDetailTitle.IndexOf(expectedId, StringComparison.OrdinalIgnoreCase) < 0)
            {
                return G4FixtureStepResult.Failed("Clicking the second Mod row did not replace the player-visible detail title. before={" +
                    g4ManagerInitialModDetailTitle + "}; after={" + g4ManagerSelectedModDetailTitle + "}; expectedId=" + expectedId + ".");
            }

            int pageCount = (manager.Mods.Count + ManagerUiModsPageSize - 1) / ManagerUiModsPageSize;
            if (pageCount < 2)
                return G4FixtureStepResult.Failed("Manager pagination acceptance requires more than one real Mod page; rows=" + manager.Mods.Count + ".");
            if (!TryInvokeUnityButton("DTMAPI.Mods.Pager.Next", out string clickFailure))
                return G4FixtureStepResult.Pending("Waiting for the reflected Manager next-page button. " + clickFailure);

            g4ManagerObservedModPageIndex = 1;
            runtime.RuntimeMonitor.Log("Manager player interaction selected Mod id=" + expectedId + " and clicked next page.");
            PublishManagerFixtureStatus(publishStatus, "Smoke.ManagerModDetailReplacement", "verified", "UnityEngine.UI.Button.onClick + reflected Text", "Selected id=" + expectedId + "; detailBefore={" + g4ManagerInitialModDetailTitle + "}; detailAfter={" + g4ManagerSelectedModDetailTitle + "}.");
            g4ManagerStage = 5;
            return G4FixtureStepResult.Pending("Manager selected-Mod detail replacement verified; pagination traversal started.");
        }

        private G4FixtureStepResult TraverseManagerModPagesForFixture(Action<string, string, string, string> publishStatus)
        {
            DtmManagerViewModel? manager = runtime.UI.CurrentManagerModel;
            if (manager == null)
                return G4FixtureStepResult.Failed("Manager model disappeared during reflected pagination.");
            int pageCount = (manager.Mods.Count + ManagerUiModsPageSize - 1) / ManagerUiModsPageSize;
            int start = g4ManagerObservedModPageIndex * ManagerUiModsPageSize;
            int end = Math.Min(manager.Mods.Count, start + ManagerUiModsPageSize);
            if (!TryReadUnityText("DTMAPI.Mods.Pager.Label", out string pagerText, out string readFailure))
                return G4FixtureStepResult.Pending("Waiting for reflected Manager pager text. " + readFailure);
            string numericMarker = (start + 1).ToString(CultureInfo.InvariantCulture) + "-" +
                end.ToString(CultureInfo.InvariantCulture) + "/" +
                manager.Mods.Count.ToString(CultureInfo.InvariantCulture);
            if (pagerText.IndexOf(numericMarker, StringComparison.Ordinal) < 0)
                return G4FixtureStepResult.Failed("Manager pager did not render the expected player-visible range. expected=" + numericMarker + "; actual={" + pagerText + "}.");
            if (!TryReadUnityText("DTMAPI.Mods.DetailTitle", out string detailTitle, out readFailure))
                return G4FixtureStepResult.Pending("Waiting for the page-clamped selected-Mod detail. " + readFailure);
            if (detailTitle.IndexOf(manager.Mods[start].UniqueID, StringComparison.OrdinalIgnoreCase) < 0)
                return G4FixtureStepResult.Failed("Manager page change did not clamp selection to the visible page. expectedId=" + manager.Mods[start].UniqueID + "; detail={" + detailTitle + "}.");

            g4ManagerLastPagerText = pagerText;
            if (g4ManagerObservedModPageIndex == pageCount - 1)
            {
                int lastPageRows = end - start;
                if (lastPageRows <= 0 || lastPageRows >= ManagerUiModsPageSize)
                    return G4FixtureStepResult.Failed("The live Manager Mod set did not exercise a partial last page. rows=" + manager.Mods.Count + "; pageCount=" + pageCount + "; lastRows=" + lastPageRows + ".");
                g4ManagerInteractionDetails = "rowSelection=true; detailReplacement=true; pages=" + pageCount +
                    "; partialLastRows=" + lastPageRows + "; lastPager={" + pagerText + "}";
                runtime.RuntimeMonitor.Log("Manager player interaction reached partial last Mod page " + pagerText + ".");
                PublishManagerFixtureStatus(publishStatus, "Smoke.ManagerModsPagination", "verified", "UnityEngine.UI.Button.onClick + reflected Text", g4ManagerInteractionDetails);
                g4ManagerStage = 6;
                return G4FixtureStepResult.Pending("Manager last-page interaction verified; awaiting Mods screenshot.");
            }

            if (!TryInvokeUnityButton("DTMAPI.Mods.Pager.Next", out string clickFailure))
                return G4FixtureStepResult.Pending("Waiting for the next reflected Manager pager button. " + clickFailure);
            g4ManagerObservedModPageIndex++;
            return G4FixtureStepResult.Pending("Manager pagination advanced to requested page " +
                (g4ManagerObservedModPageIndex + 1).ToString(CultureInfo.InvariantCulture) + "/" +
                pageCount.ToString(CultureInfo.InvariantCulture) + ".");
        }

        private G4FixtureStepResult CaptureManagerModsAndStartPreviousPageForFixture(
            string screenshotPath,
            Action<string, string, string, string> publishStatus)
        {
            if (!IsTitleOverlayReceiptCurrent() || !IsExpectedManagerOverlayForFixture(DtmOverlayPage.Mods))
                return G4FixtureStepResult.Failed("The exact Manager Mods overlay changed before screenshot completion.");
            G4FixtureStepResult screenshot = ObserveScreenshotForFixture(
                screenshotPath,
                ref g4ManagerModsScreenshotPath,
                ref g4ManagerModsScreenshotRequestedAt);
            if (!screenshot.Completed || !screenshot.Succeeded)
                return screenshot;
            runtime.RuntimeMonitor.Log("Manager Mods interaction page screenshot OK screenshot=" + g4ManagerModsScreenshotPath + ".");
            PublishManagerFixtureStatus(publishStatus, "Smoke.ManagerModsPageScreenshot", "verified", "UnityEngine.ScreenCapture.CaptureScreenshot", "Manager Mods partial-last-page screenshot=" + g4ManagerModsScreenshotPath + ".");
            if (!TryInvokeUnityButton("DTMAPI.Mods.Pager.Prev", out string clickFailure))
                return G4FixtureStepResult.Failed("Manager previous-page button disappeared after the last-page screenshot. " + clickFailure);
            g4ManagerObservedModPageIndex--;
            g4ManagerStage = 7;
            return G4FixtureStepResult.Pending("Manager Mods screenshot captured; previous-page click dispatched.");
        }

        private G4FixtureStepResult VerifyManagerPreviousPageAndOpenErrorsForFixture(Action<string, string, string, string> publishStatus)
        {
            DtmManagerViewModel? manager = runtime.UI.CurrentManagerModel;
            if (manager == null)
                return G4FixtureStepResult.Failed("Manager model disappeared after previous-page interaction.");
            int start = g4ManagerObservedModPageIndex * ManagerUiModsPageSize;
            int end = Math.Min(manager.Mods.Count, start + ManagerUiModsPageSize);
            if (!TryReadUnityText("DTMAPI.Mods.Pager.Label", out string pagerText, out string readFailure))
                return G4FixtureStepResult.Pending("Waiting for reflected previous-page text. " + readFailure);
            string expected = (start + 1).ToString(CultureInfo.InvariantCulture) + "-" +
                end.ToString(CultureInfo.InvariantCulture) + "/" +
                manager.Mods.Count.ToString(CultureInfo.InvariantCulture);
            if (pagerText.IndexOf(expected, StringComparison.Ordinal) < 0 || pagerText.Equals(g4ManagerLastPagerText, StringComparison.Ordinal))
                return G4FixtureStepResult.Failed("Manager previous-page click did not replace the player-visible range. expected=" + expected + "; actual={" + pagerText + "}.");
            runtime.RuntimeMonitor.Log("Manager player interaction clicked previous page and observed " + pagerText + ".");
            return OpenManagerPageForFixture(DtmOverlayPage.Errors, "Smoke.ManagerErrorsPage", "Errors", 8, publishStatus);
        }

        private G4FixtureStepResult SelectManagerWarningsForFixture(Action<string, string, string, string> publishStatus)
        {
            if (!IsTitleOverlayReceiptCurrent() || !IsExpectedManagerOverlayForFixture(DtmOverlayPage.Errors))
                return G4FixtureStepResult.Failed("The exact Manager Errors overlay changed before category interaction.");
            if (!TryInvokeUnityButton("DTMAPI.Errors.SelectWarnings", out string clickFailure))
                return G4FixtureStepResult.Pending("Waiting for the reflected Warnings category button. " + clickFailure);
            runtime.RuntimeMonitor.Log("Manager player interaction clicked Warnings category.");
            PublishManagerFixtureStatus(publishStatus, "Smoke.ManagerWarningsSelection", "observed", "UnityEngine.UI.Button.onClick", "Clicked the player-visible Warnings category.");
            g4ManagerStage = 9;
            return G4FixtureStepResult.Pending("Manager Warnings category click dispatched.");
        }

        private G4FixtureStepResult VerifyManagerWarningsAndSelectErrorsForFixture(Action<string, string, string, string> publishStatus)
        {
            if (!TryReadFirstUnityText(new[] { "DTMAPI.Errors.Count", "DTMAPI.Errors.EmptySelected" }, out g4ManagerWarningViewText, out string readFailure))
                return G4FixtureStepResult.Pending("Waiting for Warnings category content. " + readFailure);
            if (!TryInvokeUnityButton("DTMAPI.Errors.SelectErrors", out string clickFailure))
                return G4FixtureStepResult.Pending("Waiting for the reflected Errors category button. " + clickFailure);
            runtime.RuntimeMonitor.Log("Manager player interaction observed Warnings view {" + g4ManagerWarningViewText + "} and clicked Errors category.");
            g4ManagerStage = 10;
            return G4FixtureStepResult.Pending("Manager Warnings content observed; Errors category click dispatched.");
        }

        private G4FixtureStepResult VerifyManagerErrorsAndOpenHooksForFixture(Action<string, string, string, string> publishStatus)
        {
            if (!TryReadFirstUnityText(new[] { "DTMAPI.Errors.Count", "DTMAPI.Errors.EmptySelected" }, out string errorViewText, out string readFailure))
                return G4FixtureStepResult.Pending("Waiting for Errors category content. " + readFailure);
            if (errorViewText.Equals(g4ManagerWarningViewText, StringComparison.Ordinal))
                return G4FixtureStepResult.Failed("Errors/Warnings category clicks left identical player-visible content: {" + errorViewText + "}.");
            runtime.RuntimeMonitor.Log("Manager player interaction switched Warnings -> Errors. warnings={" + g4ManagerWarningViewText + "}; errors={" + errorViewText + "}.");
            PublishManagerFixtureStatus(publishStatus, "Smoke.ManagerDiagnosticCategorySwitch", "verified", "UnityEngine.UI.Button.onClick + reflected Text", "warnings={" + g4ManagerWarningViewText + "}; errors={" + errorViewText + "}.");
            return OpenManagerPageForFixture(DtmOverlayPage.Hooks, "Smoke.ManagerHooksPage", "Hooks", 11, publishStatus);
        }

        private G4FixtureStepResult OpenManagerAdvancedForFixture(Action<string, string, string, string> publishStatus)
        {
            return OpenManagerPageForFixture(DtmOverlayPage.Features, "Smoke.ManagerFeaturesPage", "Advanced diagnostics", 12, publishStatus);
        }

        private G4FixtureStepResult SelectManagerFeatureEvidenceForFixture(Action<string, string, string, string> publishStatus)
        {
            if (!IsTitleOverlayReceiptCurrent() || !IsExpectedManagerOverlayForFixture(DtmOverlayPage.Features))
                return G4FixtureStepResult.Failed("The exact Manager Advanced overlay changed before subsection interaction.");
            if (TryReadUnityText("DTMAPI.Title", out string uiTitle, out _) &&
                uiTitle.IndexOf("设置", StringComparison.Ordinal) >= 0 &&
                (!TryReadUnityText("DTMAPI.Advanced.Title", out string advancedTitle, out _) ||
                 advancedTitle.IndexOf("高级诊断", StringComparison.Ordinal) < 0))
            {
                return G4FixtureStepResult.Failed("Simplified-Chinese Manager Advanced heading did not use the localized player text.");
            }
            if (!TryInvokeUnityButton("DTMAPI.Advanced.SelectFeatures", out string clickFailure))
                return G4FixtureStepResult.Pending("Waiting for the reflected Feature evidence button. " + clickFailure);
            runtime.RuntimeMonitor.Log("Manager player interaction clicked Feature evidence.");
            g4ManagerStage = 13;
            return G4FixtureStepResult.Pending("Manager Feature evidence click dispatched.");
        }

        private G4FixtureStepResult VerifyManagerFeatureEvidenceAndSelectRegistryForFixture(Action<string, string, string, string> publishStatus)
        {
            if (!TryReadFirstUnityText(new[] { "DTMAPI.Advanced.Count", "DTMAPI.Advanced.Empty" }, out g4ManagerAdvancedFeatureText, out string readFailure))
                return G4FixtureStepResult.Pending("Waiting for Feature evidence content. " + readFailure);
            if (!TryInvokeUnityButton("DTMAPI.Advanced.SelectRegistry", out string clickFailure))
                return G4FixtureStepResult.Pending("Waiting for the reflected Registry evidence button. " + clickFailure);
            runtime.RuntimeMonitor.Log("Manager player interaction observed Feature evidence {" + g4ManagerAdvancedFeatureText + "} and clicked Registry evidence.");
            g4ManagerStage = 14;
            return G4FixtureStepResult.Pending("Manager Feature content observed; Registry evidence click dispatched.");
        }

        private G4FixtureStepResult VerifyManagerRegistryEvidenceForFixture(Action<string, string, string, string> publishStatus)
        {
            if (!TryReadFirstUnityText(new[] { "DTMAPI.Advanced.Count", "DTMAPI.Advanced.Empty" }, out string registryText, out string readFailure))
                return G4FixtureStepResult.Pending("Waiting for Registry evidence content. " + readFailure);
            if (registryText.Equals(g4ManagerAdvancedFeatureText, StringComparison.Ordinal))
                return G4FixtureStepResult.Failed("Feature/Registry subsection clicks left identical player-visible content: {" + registryText + "}.");
            runtime.RuntimeMonitor.Log("Manager player interaction switched Feature -> Registry evidence. features={" + g4ManagerAdvancedFeatureText + "}; registry={" + registryText + "}.");
            PublishManagerFixtureStatus(publishStatus, "Smoke.ManagerAdvancedSubsectionSwitch", "verified", "UnityEngine.UI.Button.onClick + reflected Text", "features={" + g4ManagerAdvancedFeatureText + "}; registry={" + registryText + "}.");
            g4ManagerStage = 15;
            return G4FixtureStepResult.Pending("Manager Advanced subsection interaction verified; awaiting screenshot.");
        }

        private G4FixtureStepResult CaptureManagerAdvancedAndOpenLogsForFixture(
            string screenshotPath,
            Action<string, string, string, string> publishStatus)
        {
            if (!IsTitleOverlayReceiptCurrent() || !IsExpectedManagerOverlayForFixture(DtmOverlayPage.Features))
                return G4FixtureStepResult.Failed("The exact Manager Advanced overlay changed before screenshot completion.");
            G4FixtureStepResult screenshot = ObserveScreenshotForFixture(
                screenshotPath,
                ref g4ManagerAdvancedScreenshotPath,
                ref g4ManagerAdvancedScreenshotRequestedAt);
            if (!screenshot.Completed || !screenshot.Succeeded)
                return screenshot;
            runtime.RuntimeMonitor.Log("Manager Advanced interaction page screenshot OK screenshot=" + g4ManagerAdvancedScreenshotPath + ".");
            PublishManagerFixtureStatus(publishStatus, "Smoke.ManagerAdvancedPageScreenshot", "verified", "UnityEngine.ScreenCapture.CaptureScreenshot", "Manager Advanced Registry screenshot=" + g4ManagerAdvancedScreenshotPath + ".");
            return OpenManagerPageForFixture(DtmOverlayPage.Logs, "Smoke.ManagerLogsPage", "Logs", 16, publishStatus);
        }

        private G4FixtureStepResult OpenManagerPageForFixture(
            DtmOverlayPage page,
            string hookId,
            string label,
            int nextStage,
            Action<string, string, string, string> publishStatus)
        {
            if (!IsTitleOverlayReceiptCurrent())
                return G4FixtureStepResult.Failed("The QA-owned Manager overlay session changed before opening the " + label + " page; unrelated overlay was left untouched.");
            long before = runtime.UI.OverlaySessionSequence;
            runtime.UI.SetPage(page);
            if (!runtime.UI.IsOpen || runtime.UI.OverlaySessionSequence <= before)
                return G4FixtureStepResult.Failed("The Manager " + label + " page did not create a distinct overlay session receipt.");
            CaptureTitleOverlayReceipt();
            if (!IsTitleOverlayReceiptCurrent() || !IsExpectedManagerOverlayForFixture(page))
                return G4FixtureStepResult.Failed("The Manager " + label + " page did not match its exact overlay receipt.");
            DtmManagerViewModel? manager = runtime.UI.CurrentManagerModel;
            if (manager == null || !string.IsNullOrWhiteSpace(runtime.UI.LastManagerRefreshError))
                return G4FixtureStepResult.Failed("The Manager " + label + " page has no refreshed model. error=" + runtime.UI.LastManagerRefreshError + ".");

            int pageRows = CountManagerPageRows(manager, page);
            if (page == DtmOverlayPage.Features)
            {
                string advancedSummary = ManagerPageRowFormatter.FormatAdvancedSummary(manager.AdvancedDiagnostics);
                if (string.IsNullOrWhiteSpace(advancedSummary) ||
                    (manager.AdvancedDiagnostics.RegistryAvailable &&
                     advancedSummary.IndexOf("registryRows=", StringComparison.OrdinalIgnoreCase) < 0))
                    return G4FixtureStepResult.Failed("The Manager Advanced diagnostics page did not expose a complete registry summary: " + advancedSummary + ".");
            }
            runtime.RuntimeMonitor.Log("Smoke automation opened DTMAPI Manager " + label + " page.");
            PublishManagerFixtureStatus(publishStatus, hookId, "verified", "DTMAPI Manager " + label + " page", "Opened and read back the exact Manager " + label + " tab; pageRows=" + pageRows + ".");
            g4ManagerStage = nextStage;
            return G4FixtureStepResult.Pending("Manager " + label + " page verified; pageRows=" + pageRows + "; overlaySession=exact; nextStage=" + nextStage + ".");
        }

        private G4FixtureStepResult ExportManagerLogsForFixture(Action<string, string, string, string> publishStatus)
        {
            if (!IsTitleOverlayReceiptCurrent() || !IsExpectedManagerOverlayForFixture(DtmOverlayPage.Logs))
                return G4FixtureStepResult.Failed("The exact Manager Logs overlay session changed before export; unrelated overlay was left untouched.");
            long before = runtime.UI.OverlaySessionSequence;
            string exportedPath = runtime.UI.ExportLogs();
            if (!runtime.UI.IsOpen || runtime.UI.OverlaySessionSequence <= before)
                return G4FixtureStepResult.Failed("Manager Logs export did not retain a distinct Logs overlay receipt.");
            CaptureTitleOverlayReceipt();
            if (!IsTitleOverlayReceiptCurrent() || !IsExpectedManagerOverlayForFixture(DtmOverlayPage.Logs))
                return G4FixtureStepResult.Failed("Manager Logs export changed the exact QA-owned Logs overlay receipt unexpectedly.");

            ManagerLogsPageState state = ManagerLogsPageState.From(runtime.UI.LastManagerReportExport, runtime.UI.CurrentManagerModel);
            string stateSummary = ManagerPageRowFormatter.FormatLogsExportState(state);
            bool exported = state.ExportStatus.Equals("exported", StringComparison.OrdinalIgnoreCase) &&
                state.PathMatchStatus.Equals("matched", StringComparison.OrdinalIgnoreCase) &&
                state.SnapshotReportPathMatched &&
                !string.IsNullOrWhiteSpace(exportedPath) &&
                File.Exists(exportedPath) &&
                exportedPath.Equals(state.ExportedReportPath, StringComparison.OrdinalIgnoreCase) &&
                exportedPath.Equals(state.SnapshotLatestReportPath, StringComparison.OrdinalIgnoreCase) &&
                exportedPath.Equals(runtime.UI.LastExportPath, StringComparison.OrdinalIgnoreCase);
            runtime.RuntimeMonitor.Log("Manager Logs export button " + (exported ? "OK" : "failed") + " status=" + state.ExportStatus + " pathMatch=" + state.PathMatchStatus + " path=" + exportedPath + ".");
            runtime.RuntimeMonitor.Log("Manager Logs export state " + (exported ? "OK" : "failed") + " " + stateSummary + ".");
            PublishManagerFixtureStatus(publishStatus, "Smoke.ManagerLogsExportButton", exported ? "verified" : "failed", "IDiagnosticsHelper.ExportLogs", stateSummary + "; path=" + exportedPath);
            PublishManagerFixtureStatus(publishStatus, "Smoke.ManagerLogsExportStateText", exported ? "verified" : "failed", "DTMAPI Manager Logs page", stateSummary);
            if (!exported)
                return G4FixtureStepResult.Failed("Manager Logs export/path-match verification failed. " + stateSummary + "; path=" + exportedPath + ".");

            g4ManagerExportDetails = stateSummary + "; path=" + exportedPath + "; fileExists=true";
            g4ManagerStage = 17;
            return G4FixtureStepResult.Pending("Manager Logs export verified; " + g4ManagerExportDetails + "; waitingForLogsScreenshot=true");
        }

        private G4FixtureStepResult CaptureManagerLogsAndCloseForFixture(
            string screenshotPath,
            Action<string, string, string, string> publishStatus)
        {
            if (!IsTitleOverlayReceiptCurrent() || !IsExpectedManagerOverlayForFixture(DtmOverlayPage.Logs))
                return G4FixtureStepResult.Failed("The exact Manager Logs overlay session changed before screenshot completion; unrelated overlay was left untouched.");
            G4FixtureStepResult screenshot = ObserveScreenshotForFixture(
                screenshotPath,
                ref g4ManagerLogsScreenshotPath,
                ref g4ManagerLogsScreenshotRequestedAt);
            if (!screenshot.Completed || !screenshot.Succeeded)
                return screenshot;

            runtime.RuntimeMonitor.Log("Manager Logs page screenshot OK screenshot=" + g4ManagerLogsScreenshotPath + ".");
            PublishManagerFixtureStatus(publishStatus, "Smoke.ManagerLogsPageScreenshot", "verified", "UnityEngine.ScreenCapture.CaptureScreenshot", "Manager Logs page screenshot=" + g4ManagerLogsScreenshotPath + ".");
            bool closed = CloseTitleOverlayOwnedByFixture();
            return closed
                ? G4FixtureStepResult.Verified(g4ManagerStatusDetails + "; " + g4ManagerInteractionDetails + "; " + g4ManagerExportDetails + "; " + screenshot.Details + "; pages=Status|Mods|Errors|Hooks|Advanced|Logs; interactions=mod-row|next|partial-last|previous|warnings|errors|features|registry; close=exact-session; route=manager-mvp")
                : G4FixtureStepResult.Failed("The exact QA-owned Manager MVP overlay could not be closed; cleanup remains retryable.");
        }

        private static bool TryReadFirstUnityText(
            string[] objectNames,
            out string value,
            out string failure)
        {
            foreach (string objectName in objectNames)
            {
                if (TryReadUnityText(objectName, out value, out _))
                {
                    failure = string.Empty;
                    return true;
                }
            }
            value = string.Empty;
            failure = "None of the reflected Text objects were active: " + string.Join(",", objectNames) + ".";
            return false;
        }

        private static bool TryReadUnityText(string objectName, out string value, out string failure)
        {
            value = string.Empty;
            failure = string.Empty;
            try
            {
                object? gameObject = FindUnityGameObject(objectName);
                if (gameObject == null)
                {
                    failure = "GameObject.Find returned null for " + objectName + ".";
                    return false;
                }
                Type? textType = GameBridgeNativeHelpers.ResolveType("UnityEngine.UI.Text, UnityEngine.UI");
                if (textType == null)
                {
                    failure = "UnityEngine.UI.Text was not resolved.";
                    return false;
                }
                object? text = GetUnityComponent(gameObject, textType);
                PropertyInfo? property = textType.GetProperty("text", BindingFlags.Public | BindingFlags.Instance);
                if (text == null || property == null)
                {
                    failure = "Text component/property was absent on " + objectName + ".";
                    return false;
                }
                value = property.GetValue(text)?.ToString() ?? string.Empty;
                return true;
            }
            catch (Exception ex)
            {
                failure = ex.GetBaseException().GetType().Name + ": " + ex.GetBaseException().Message;
                return false;
            }
        }

        private static bool TryInvokeUnityButton(string objectName, out string failure)
        {
            failure = string.Empty;
            try
            {
                object? gameObject = FindUnityGameObject(objectName);
                if (gameObject == null)
                {
                    failure = "GameObject.Find returned null for " + objectName + ".";
                    return false;
                }
                Type? buttonType = GameBridgeNativeHelpers.ResolveType("UnityEngine.UI.Button, UnityEngine.UI");
                if (buttonType == null)
                {
                    failure = "UnityEngine.UI.Button was not resolved.";
                    return false;
                }
                object? button = GetUnityComponent(gameObject, buttonType);
                PropertyInfo? onClickProperty = buttonType.GetProperty("onClick", BindingFlags.Public | BindingFlags.Instance);
                if (button == null || onClickProperty == null)
                {
                    failure = "Button/onClick/Invoke was absent on " + objectName + ".";
                    return false;
                }
                object? onClick = onClickProperty.GetValue(button);
                MethodInfo? invoke = onClick?.GetType().GetMethod("Invoke", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
                if (onClick == null || invoke == null)
                {
                    failure = "Button/onClick/Invoke was absent on " + objectName + ".";
                    return false;
                }
                invoke.Invoke(onClick, null);
                return true;
            }
            catch (Exception ex)
            {
                failure = ex.GetBaseException().GetType().Name + ": " + ex.GetBaseException().Message;
                return false;
            }
        }

        private static object? FindUnityGameObject(string objectName)
        {
            Type? gameObjectType = GameBridgeNativeHelpers.ResolveType("UnityEngine.GameObject, UnityEngine.CoreModule") ??
                GameBridgeNativeHelpers.ResolveType("UnityEngine.GameObject, UnityEngine");
            MethodInfo? find = gameObjectType?.GetMethod(
                "Find",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(string) },
                null);
            return find?.Invoke(null, new object[] { objectName });
        }

        private static object? GetUnityComponent(object gameObject, Type componentType)
        {
            MethodInfo? getComponent = gameObject.GetType().GetMethod(
                "GetComponent",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new[] { typeof(Type) },
                null);
            return getComponent?.Invoke(gameObject, new object[] { componentType });
        }

        private bool IsExpectedManagerOverlayForFixture(DtmOverlayPage page)
        {
            return g4TitleOverlayMenuId.Equals("DTMAPI." + page, StringComparison.Ordinal) &&
                string.IsNullOrEmpty(g4TitleOverlayOwnerId) &&
                g4TitleOverlayPage == page;
        }

        private static int CountManagerRows(DtmManagerViewModel manager) =>
            manager.Mods.Count + manager.Errors.Count + manager.Warnings.Count + manager.Hooks.Count + manager.Features.Count;

        private static int CountManagerPageRows(DtmManagerViewModel manager, DtmOverlayPage page)
        {
            switch (page)
            {
                case DtmOverlayPage.Mods: return manager.Mods.Count;
                case DtmOverlayPage.Errors: return manager.Errors.Count + manager.Warnings.Count;
                case DtmOverlayPage.Hooks: return manager.Hooks.Count;
                case DtmOverlayPage.Features: return manager.Features.Count + manager.AdvancedDiagnostics.Rows.Count;
                case DtmOverlayPage.Logs: return string.IsNullOrWhiteSpace(manager.LatestLogPath) ? 0 : 1;
                default: return CountManagerRows(manager);
            }
        }

        private static bool IsCompleteManagerSummary(string summary)
        {
            if (string.IsNullOrWhiteSpace(summary))
                return false;
            foreach (string marker in new[]
            {
                "overall=", "mods=loaded:", "diagnostics=errors:", "hooks=failed:",
                "features=failed:", "install=", "report=", "log=", "reportPath="
            })
            {
                if (summary.IndexOf(marker, StringComparison.OrdinalIgnoreCase) < 0)
                    return false;
            }
            return true;
        }

        private static void PublishManagerFixtureStatus(
            Action<string, string, string, string> publishStatus,
            string hookId,
            string status,
            string source,
            string details)
        {
            publishStatus(hookId, status, source, (details ?? string.Empty).TrimEnd('.') + "; owner=qa; fallback=false");
        }
    }
}
