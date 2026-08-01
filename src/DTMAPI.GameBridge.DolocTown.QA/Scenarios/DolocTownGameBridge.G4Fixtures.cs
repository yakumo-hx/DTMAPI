using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;
using DTMAPI.Core.Services;
using static DTMAPI.GameBridge.DolocTown.GameBridgeNativeHelpers;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed partial class QaScenarioController
    {
        private object? g4OfficialUiOwnedState;
        private DateTimeOffset g4OfficialUiOpenedAt;
        private string? g4OfficialScreenshotPath;
        private DateTimeOffset g4OfficialScreenshotRequestedAt;
        private object? g4PauseUiOwnedState;
        private DateTimeOffset g4PauseUiOpenedAt;
        private string? g4PauseScreenshotPath;
        private DateTimeOffset g4PauseScreenshotRequestedAt;
        private object? g4AnimalUiOwnedState;
        private string? g4AnimalScreenshotPath;
        private DateTimeOffset g4AnimalScreenshotRequestedAt;
        private int g4AnimalSwitchStage;
        private int g4AnimalExpectedReceiptSequence;
        private bool g4AnimalAwaitingNextFrameGuard;
        private bool g4AnimalPreflightComplete;
        private int g4AnimalConstructedCount;
        private int g4AnimalProgressRowCount;
        private string? g4EquipmentSlotsScreenshotPath;
        private DateTimeOffset g4EquipmentSlotsScreenshotRequestedAt;
        private bool g4EquipmentSlotsEvidenceCaptured;
        private string g4EquipmentSlotsEvidenceSummary = string.Empty;
        private long g4TitleOverlaySessionSequence;
        private string g4TitleOverlayMenuId = string.Empty;
        private string g4TitleOverlayOwnerId = string.Empty;
        private DtmOverlayPage g4TitleOverlayPage;
        private string? g4TitleOverlayRequestedConfigId;
        private string? g4TitleScreenshotPath;
        private DateTimeOffset g4TitleScreenshotRequestedAt;
        private string? g4DebugScreenshotPath;
        private DateTimeOffset g4DebugScreenshotRequestedAt;
        private bool g4DebugEvidenceCaptured;
        private string g4DebugEvidenceSummary = string.Empty;
        private int g4UiObservationCount;

        private bool g4SaveCoordinatorArmed;
        private int g4SaveCoordinatorStage;
        private int g4SaveIndex = -1;
        private object? g4SaveUiState;
        private string? g4SaveScreenshotPath;
        private DateTimeOffset g4SaveScreenshotRequestedAt;
        private object? g4MoreSavesPostTitleUiState;
        private string? g4MoreSavesPostTitleScreenshotPath;
        private DateTimeOffset g4MoreSavesPostTitleScreenshotRequestedAt;
        private DateTimeOffset g4MoreSavesPostTitleOpenedAt;
        private DateTimeOffset g4SaveRequestAt;
        private bool g4DirectFallbackAttempted;
        private bool g4ModChangeContinuationIssued;
        private object? g4ModChangeReceiptState;
        private FieldInfo? g4ModChangeReceiptField;
        private Action? g4ModChangeOriginalAction;
        private Action? g4ModChangeWrappedAction;
        private bool g4SaveSelectionVerifiedBeforeContinuation;
        private int g4SaveNativeEnterBaseline;
        private int g4SaveNativeReturnBaseline;
        private readonly List<object> g4NativeUiExitedStateReceipts = new List<object>();
        private NativeUiLayoutObservation? g4LastVerifiedNativeUiLayoutObservation;
        private string g4LastVerifiedNativeUiRepairHooks = string.Empty;

        private HatchVoiceFixtureSession? g4HatchVoiceSession;
        private CameraPlayableMovementFixtureScenario? g4CameraSession;

        internal void NotifyQaHostUiObservation(string source)
        {
            g4UiObservationCount++;
        }

        internal G4FixtureStepResult ObserveTitleSettingsUiForFixture(string screenshotPath)
        {
            if (!runtime.UI.InputContext.Equals("HomePageUiState", StringComparison.OrdinalIgnoreCase))
                return G4FixtureStepResult.Pending("Waiting for HomePage before opening the DTMAPI settings overlay.");

            if (g4TitleOverlaySessionSequence == 0)
            {
                if (runtime.UI.IsOpen)
                    return G4FixtureStepResult.Pending("Waiting for the pre-existing DTMAPI overlay to close; QA will not claim or close it.");
                long before = runtime.UI.OverlaySessionSequence;
                runtime.UI.OpenConfigPage();
                if (!runtime.UI.IsOpen || runtime.UI.OverlaySessionSequence <= before)
                    return G4FixtureStepResult.Failed("The DTMAPI settings overlay did not create a distinct QA-owned session.");
                CaptureTitleOverlayReceipt();
            }
            if (!IsTitleOverlayReceiptCurrent())
                return G4FixtureStepResult.Failed("The QA-owned title settings overlay session identity changed before observation completed; unrelated overlay was left untouched.");
            if (!IsExpectedTitleSettingsOverlay())
                return G4FixtureStepResult.Failed("The QA-requested Runtime config surface did not open the expected DTMAPI.Config session.");

            NativeUiLayoutObservation? layout = NativeUiLayoutRepairService?.CaptureObservation();
            if (!TryValidateNativeUiLayoutObservation(layout, requireHomePage: true, out string repairHooks))
                return G4FixtureStepResult.Failed("The title settings layout receipt was not concrete or its production repair hooks were unavailable. " + FormatNativeUiObservation(layout) + "; hooks={" + repairHooks + "}.");
            g4LastVerifiedNativeUiLayoutObservation = layout;
            g4LastVerifiedNativeUiRepairHooks = repairHooks;
            G4FixtureStepResult screenshot = ObserveScreenshotForFixture(screenshotPath, ref g4TitleScreenshotPath, ref g4TitleScreenshotRequestedAt);
            if (!screenshot.Completed)
                return screenshot;
            if (!screenshot.Succeeded)
                return screenshot;
            bool closed = CloseTitleOverlayOwnedByFixture();
            return closed
                ? G4FixtureStepResult.Verified(FormatNativeUiObservation(layout) + "; hooks={" + repairHooks + "}; overlaySession=exact; observerCallbacks=" + g4UiObservationCount + "; " + screenshot.Details + "; close=owned-only")
                : G4FixtureStepResult.Failed("The QA-owned title settings overlay could not be closed.");
        }

        internal G4FixtureStepResult ObserveOfficialModUiForFixture(string screenshotPath)
        {
            Type? stateType = ResolveType("DolocTown.ModUiState, Assembly-CSharp");
            if (stateType == null)
                return G4FixtureStepResult.Failed("ModUiState is unavailable.");

            if (g4OfficialUiOwnedState == null)
            {
                if (!runtime.UI.InputContext.Equals("HomePageUiState", StringComparison.OrdinalIgnoreCase))
                    return G4FixtureStepResult.Pending("Waiting for HomePage before opening the official Mod UI.");
                g4OfficialUiOwnedState = EnterNativeUiForFixture(stateType, null);
                if (g4OfficialUiOwnedState == null)
                    return G4FixtureStepResult.Failed("The official Mod UI did not open.");
                g4OfficialUiOpenedAt = DateTimeOffset.UtcNow;
                return G4FixtureStepResult.Pending("Official Mod UI opened; waiting for metadata rows.");
            }

            object? current = GetCurrentNativeUiStateForFixture();
            if (!ReferenceEquals(current, g4OfficialUiOwnedState))
                return G4FixtureStepResult.Failed("The QA-owned official Mod UI lost current-state ownership before observation completed.");
            object? panel = ReadMember(g4OfficialUiOwnedState, "panel");
            object? list = ReadMember(g4OfficialUiOwnedState, "currentModList");
            if (panel == null || !(list is IEnumerable enumerable))
                return G4FixtureStepResult.Pending("Waiting for the official Mod UI list and panel.");

            var rows = EnumerateObjects(enumerable).Where(IsDtmApiOfficialRowForFixture).ToList();
            if (rows.Count == 0)
            {
                if ((DateTimeOffset.UtcNow - g4OfficialUiOpenedAt).TotalSeconds > 12d)
                    return G4FixtureStepResult.Failed("The official Mod UI did not expose any DTMAPI product rows.");
                return G4FixtureStepResult.Pending("Waiting for DTMAPI product rows in the official Mod UI.");
            }

            object selected = rows[0];
            int selectedIndex = IndexOfReference(enumerable, selected);
            MethodInfo? select = panel.GetType().GetMethod("Select", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(int) }, null);
            if (selectedIndex >= 0)
                select?.Invoke(panel, new object[] { selectedIndex });
            string id = FirstText(ReadStringMember(selected, "id"), ReadStringMember(selected, "Id"));
            string iconPath = FirstText(ReadStringMember(selected, "iconPath"), ReadStringMember(selected, "IconPath"));
            string previewPath = FirstText(ReadStringMember(selected, "previewPath"), ReadStringMember(selected, "PreviewPath"));
            bool assetsObserved = (string.IsNullOrWhiteSpace(iconPath) || File.Exists(iconPath)) && (string.IsNullOrWhiteSpace(previewPath) || File.Exists(previewPath));
            G4FixtureStepResult screenshot = ObserveScreenshotForFixture(screenshotPath, ref g4OfficialScreenshotPath, ref g4OfficialScreenshotRequestedAt);
            if (!screenshot.Completed)
                return screenshot;
            if (!screenshot.Succeeded)
                return screenshot;
            bool closed = CloseNativeUiOwnedByFixture(ref g4OfficialUiOwnedState);
            return assetsObserved && closed
                ? G4FixtureStepResult.Verified("rows=" + rows.Count + "; selected=" + id + "; assetsObserved=true; " + screenshot.Details + "; close=owned-only")
                : G4FixtureStepResult.Failed("Official Mod UI metadata receipt failed. assetsObserved=" + assetsObserved + "; closed=" + closed + ".");
        }

        internal G4FixtureStepResult ObservePauseMenuLayoutForFixture(string screenshotPath)
        {
            Type? stateType = ResolveType("DolocTown.MainMenuUiState, Assembly-CSharp");
            if (stateType == null)
                return G4FixtureStepResult.Failed("MainMenuUiState is unavailable.");
            if (g4PauseUiOwnedState == null)
            {
                if (!IsNormalGameplayForFixture())
                    return G4FixtureStepResult.Pending("Waiting for normal Gameplay before opening the pause menu.");
                g4PauseUiOwnedState = EnterNativeUiForFixture(stateType, null);
                if (g4PauseUiOwnedState == null)
                    return G4FixtureStepResult.Failed("The pause menu did not open.");
                g4PauseUiOpenedAt = DateTimeOffset.UtcNow;
                return G4FixtureStepResult.Pending("Pause menu opened; production repair remains the sole layout writer.");
            }

            if (!ReferenceEquals(GetCurrentNativeUiStateForFixture(), g4PauseUiOwnedState))
                return G4FixtureStepResult.Failed("The QA-owned pause menu lost current-state ownership.");
            if ((DateTimeOffset.UtcNow - g4PauseUiOpenedAt).TotalSeconds < 1.5d)
                return G4FixtureStepResult.Pending("Waiting for a post-repair pause-menu sample.");
            NativeUiLayoutObservation? layout = NativeUiLayoutRepairService?.CaptureObservation();
            bool valid = TryValidateNativeUiLayoutObservation(layout, requireHomePage: false, out string repairHooks) &&
                layout!.VisibleSlots >= 5 && layout.ConstraintCount == layout.VisibleSlots;
            if (valid)
            {
                g4LastVerifiedNativeUiLayoutObservation = layout;
                g4LastVerifiedNativeUiRepairHooks = repairHooks;
            }
            G4FixtureStepResult screenshot = ObserveScreenshotForFixture(screenshotPath, ref g4PauseScreenshotPath, ref g4PauseScreenshotRequestedAt);
            if (!screenshot.Completed)
                return screenshot;
            if (!screenshot.Succeeded)
                return screenshot;
            bool closed = CloseNativeUiOwnedByFixture(ref g4PauseUiOwnedState);
            return valid && closed
                ? G4FixtureStepResult.Verified(FormatNativeUiObservation(layout) + "; hooks={" + repairHooks + "}; productionRepair=true; " + screenshot.Details + "; close=owned-only")
                : G4FixtureStepResult.Failed("Pause-menu read-only observation failed. valid=" + valid + "; closed=" + closed + "; " + FormatNativeUiObservation(layout) + "; hooks={" + repairHooks + "}.");
        }

        internal G4FixtureStepResult ObserveDebugConsoleUiForFixture(string screenshotPath)
        {
            bool productConsoleOpen = runtime.UI.IsOwnerBoundCustomMenuOpen(
                "DTMAPI.DebugConsole",
                "DTMAPI.DebugConsoleMod");

            // The real DebugConsole product owns this surface. G4 may observe it, but
            // must never forge a second owner or open/close the product-owned UI.
            if (g4DebugEvidenceCaptured)
            {
                if (productConsoleOpen)
                    return G4FixtureStepResult.Pending("EVIDENCE_CAPTURED_WAITING_ESCAPE; uiOwner=product; inputOwner=runner; evidenceOwner=qa; " + g4DebugEvidenceSummary + ".");
                return G4FixtureStepResult.Verified(g4DebugEvidenceSummary + "; closeReceipt=product-owner-modal-closed; ownerUnchanged=true; syntheticInput=false; mouseGive=false");
            }
            if (!productConsoleOpen && g4DebugScreenshotPath == null)
                return G4FixtureStepResult.Pending("Waiting for the runner-owned real Y-key console before read-only screenshot observation.");
            if (!productConsoleOpen)
                return G4FixtureStepResult.Failed("The product-owned DebugConsole closed before the QA screenshot receipt completed.");

            G4FixtureStepResult screenshot = ObserveScreenshotForFixture(screenshotPath, ref g4DebugScreenshotPath, ref g4DebugScreenshotRequestedAt);
            if (!screenshot.Completed)
                return screenshot;
            if (!screenshot.Succeeded)
                return screenshot;
            g4DebugEvidenceCaptured = true;
            g4DebugEvidenceSummary = "realOwnerUi=true; readOnly=true; " + screenshot.Details;
            return G4FixtureStepResult.Pending("EVIDENCE_CAPTURED_WAITING_ESCAPE; uiOwner=product; inputOwner=runner; evidenceOwner=qa; " + g4DebugEvidenceSummary + ".");
        }

        internal G4FixtureStepResult ObservePendingSaveSlotsPagingForFixture(string screenshotPath)
        {
            if (!g4SaveCoordinatorArmed || g4SaveUiState == null || g4SaveIndex < 0)
                return G4FixtureStepResult.Pending("Waiting for the QA save coordinator to open the official panel.");
            if (!HasExactNativeUiReceiptForFixture(GetCurrentNativeUiStateForFixture(), g4SaveUiState))
                return FailSaveSlotCoordinatorOwnershipLoss("before paging/select");
            object? panel = ReadMember(g4SaveUiState, "panel");
            if (panel == null)
                return G4FixtureStepResult.Pending("Waiting for the official save panel.");
            int slotCount = Math.Max(ReadOptionalInt(panel, "slotCount"), CountObjects(ReadMember(panel, "slots")));
            MethodInfo? select = panel.GetType().GetMethod("Select", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(int) }, null);
            if (slotCount <= 0 || select == null)
                return G4FixtureStepResult.Failed("The official save panel does not expose read-only paging selection.");
            var targets = new List<int> { 0 };
            if (slotCount > 12) targets.Add(Math.Min(12, slotCount - 1));
            if (slotCount > 18) targets.Add(slotCount - 1);
            foreach (int target in targets.Distinct())
                select.Invoke(panel, new object[] { target });
            G4FixtureStepResult screenshot = ObserveScreenshotForFixture(screenshotPath, ref g4SaveScreenshotPath, ref g4SaveScreenshotRequestedAt);
            if (!screenshot.Completed || !screenshot.Succeeded)
                return screenshot;
            return G4FixtureStepResult.Verified("slotCount=" + slotCount + "; targets=" + string.Join("|", targets.Select(item => (item + 1).ToString(CultureInfo.InvariantCulture))) + "; requestedSlot=" + (g4SaveIndex + 1) + "; " + screenshot.Details + "; coordinatorOwner=qa");
        }

        internal G4FixtureStepResult AdvanceSaveSlotScenarioForFixture(int saveSlot, string screenshotPath)
        {
            if (qaHostObservedSaveLoaded)
                return G4FixtureStepResult.Verified("SaveLoaded observed after the QA-owned official save path; slot=" + saveSlot + ".");
            if (saveSlot <= 0)
                return G4FixtureStepResult.Failed("The QA save coordinator requires a positive slot.");

            Type? gameDataType = ResolveType("DolocTown.GameDataUiState, Assembly-CSharp");
            if (gameDataType == null)
                return G4FixtureStepResult.Failed("GameDataUiState is unavailable.");

            if (g4SaveCoordinatorStage == 0)
            {
                if (!runtime.UI.InputContext.Equals("HomePageUiState", StringComparison.OrdinalIgnoreCase))
                    return G4FixtureStepResult.Pending("Waiting for HomePage before the QA-owned official save path.");
                g4SaveCoordinatorArmed = true;
                g4SaveIndex = saveSlot - 1;
                g4SaveUiState = EnterNativeUiForFixture(gameDataType, null);
                if (g4SaveUiState == null)
                    return G4FixtureStepResult.Failed("The official save UI did not open.");
                g4SaveCoordinatorStage = 1;
                return G4FixtureStepResult.Pending("Official save UI opened; waiting for paging and screenshot receipt.");
            }

            if (g4SaveCoordinatorStage == 1)
            {
                G4FixtureStepResult paging = ObservePendingSaveSlotsPagingForFixture(screenshotPath);
                if (!paging.Completed || !paging.Succeeded)
                    return paging;
                if (!SelectRequestedSaveForFixture(g4SaveUiState, g4SaveIndex, out string selectionDetails))
                    return G4FixtureStepResult.Failed("The requested save slot could not be reselected with state/panel readback after paging. " + selectionDetails + ".");
                g4SaveSelectionVerifiedBeforeContinuation = true;
                MethodInfo? continueMethod = FindMethodInHierarchy(gameDataType, "OnConfirm", 0);
                if (continueMethod == null)
                    return G4FixtureStepResult.Failed("The official save UI continuation method is unavailable.");
                if (!runtime.RefactorOptions.SaveLoadRequestCoordinator)
                    return G4FixtureStepResult.Failed("The QA save fallback requires the production SaveLoadRequestCoordinator to be enabled before native continuation.");
                SaveLoadRequestSnapshot beforeContinuation = runtime.SaveLoadRequestSnapshot;
                g4SaveNativeEnterBaseline = beforeContinuation.NativeEnterCount;
                g4SaveNativeReturnBaseline = beforeContinuation.NativeReturnCount;
                g4SaveRequestAt = DateTimeOffset.UtcNow;
                if (!HasExactNativeUiReceiptForFixture(GetCurrentNativeUiStateForFixture(), g4SaveUiState))
                    return FailSaveSlotCoordinatorOwnershipLoss("before native OnConfirm continuation");
                continueMethod.Invoke(g4SaveUiState, null);
                g4SaveCoordinatorStage = 2;
                return G4FixtureStepResult.Pending(paging.Details + "; requested slot reselected with " + selectionDetails + "; coordinatorEnabled=true; native continuation issued once; waiting for SaveLoaded or optional mod-change prompt.");
            }

            if (g4SaveCoordinatorStage == 2)
            {
                object? current = GetCurrentNativeUiStateForFixture();
                if (!g4ModChangeContinuationIssued && current != null && current.GetType().Name.IndexOf("ModChangeListUiState", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (!ContinueModChangeForFixture(current))
                        return G4FixtureStepResult.Failed("The mod-change continuation receipt could not be installed or invoked.");
                    g4ModChangeContinuationIssued = true;
                    return G4FixtureStepResult.Pending("Optional mod-change prompt continued with requested-slot reselection and delegate restoration; waiting for SaveLoaded.");
                }
                if (!g4DirectFallbackAttempted && (DateTimeOffset.UtcNow - g4SaveRequestAt).TotalSeconds >= 15d)
                {
                    g4DirectFallbackAttempted = true;
                    if (!RequestSingleDirectSaveFallbackForFixture(out bool issued, out string fallbackDetails))
                        return G4FixtureStepResult.Failed("The single delayed save fallback could not be evaluated: " + fallbackDetails + ".");
                    g4SaveCoordinatorStage = 3;
                    return G4FixtureStepResult.Pending(
                        issued
                            ? "Exactly one delayed direct fallback was issued after 15 seconds only because the coordinator was enabled, no native entry/transition receipt existed, and the exact GameData UI selection remained active; " + fallbackDetails + "."
                            : "The delayed direct fallback was blocked by a production coordinator/native continuation receipt; no direct request was issued; " + fallbackDetails + ".");
                }
            }
            if (g4SaveCoordinatorStage >= 2 && g4SaveRequestAt != default && (DateTimeOffset.UtcNow - g4SaveRequestAt).TotalSeconds > 120d)
                return G4FixtureStepResult.Failed("The official save path did not reach SaveLoaded within 120 seconds, including after the single delayed fallback attempt.");
            return G4FixtureStepResult.Pending("Waiting for SaveLoaded after QA-owned save coordination; directFallbackAttempted=" + g4DirectFallbackAttempted + ".");
        }

        internal G4FixtureStepResult ObservePostTitleMoreSavesOfficialSaveUiForFixture(string screenshotPath)
        {
            Type? gameDataType = ResolveType("DolocTown.GameDataUiState, Assembly-CSharp");
            if (gameDataType == null)
                return G4FixtureStepResult.Failed("GameDataUiState is unavailable for the post-title MoreSaves receipt.");

            if (g4MoreSavesPostTitleUiState == null)
            {
                if (!runtime.UI.InputContext.Equals("HomePageUiState", StringComparison.OrdinalIgnoreCase))
                    return G4FixtureStepResult.Pending("Waiting for continuous HomePage before the post-title MoreSaves panel receipt.");
                g4MoreSavesPostTitleUiState = EnterNativeUiForFixture(gameDataType, null);
                if (g4MoreSavesPostTitleUiState == null)
                    return G4FixtureStepResult.Failed("The post-title official save UI did not open.");
                g4MoreSavesPostTitleOpenedAt = DateTimeOffset.UtcNow;
                return G4FixtureStepResult.Pending("Post-title official save UI opened; waiting for the exact twelve-slot panel.");
            }

            if (!HasExactNativeUiReceiptForFixture(GetCurrentNativeUiStateForFixture(), g4MoreSavesPostTitleUiState))
                return G4FixtureStepResult.Failed("The QA-owned post-title official save UI lost current-state ownership.");
            object? panel = ReadMember(g4MoreSavesPostTitleUiState, "panel");
            int slotCount = panel == null ? 0 : Math.Max(ReadOptionalInt(panel, "slotCount"), CountObjects(ReadMember(panel, "slots")));
            if (slotCount != 12)
            {
                if ((DateTimeOffset.UtcNow - g4MoreSavesPostTitleOpenedAt).TotalSeconds <= 10d)
                    return G4FixtureStepResult.Pending("Waiting for the post-title official panel to render exactly twelve slots; observed=" + slotCount + ".");
                return G4FixtureStepResult.Failed("The post-title official save panel did not render exactly twelve slots; observed=" + slotCount + ".");
            }

            G4FixtureStepResult screenshot = ObserveScreenshotForFixture(
                screenshotPath,
                ref g4MoreSavesPostTitleScreenshotPath,
                ref g4MoreSavesPostTitleScreenshotRequestedAt);
            if (!screenshot.Completed || !screenshot.Succeeded)
                return screenshot;
            bool closed = CloseNativeUiOwnedByFixture(ref g4MoreSavesPostTitleUiState);
            return closed
                ? G4FixtureStepResult.Verified("postTitle=true; slotCount=12; " + screenshot.Details + "; close=owned-only")
                : G4FixtureStepResult.Failed("The QA-owned post-title official save UI could not be closed.");
        }

        internal G4FixtureStepResult ObserveAnimalsForFixture(string screenshotPath)
        {
            List<object> animals = FindAnimalsForFixture();
            if (animals.Count < 2)
                return G4FixtureStepResult.Failed("At least two save animals are required for repeated-switch observation.");
            Type? dataType = ResolveType("DolocTown.UI.AnimalFullInfoData, Assembly-CSharp");
            if (dataType == null)
                return G4FixtureStepResult.Failed("AnimalFullInfoData is unavailable.");

            if (!g4AnimalPreflightComplete)
            {
                foreach (object animal in animals)
                {
                    ConstructorInfo? constructor = dataType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                        .FirstOrDefault(item => item.GetParameters().Length == 1 && item.GetParameters()[0].ParameterType.IsInstanceOfType(animal));
                    object? data = constructor?.Invoke(new[] { animal });
                    if (data == null)
                        continue;
                    g4AnimalConstructedCount++;
                    if (TryObserveAnimalProductRows(data, out _))
                        g4AnimalProgressRowCount++;
                }
                g4AnimalPreflightComplete = true;
            }
            if (g4AnimalConstructedCount == 0)
                return G4FixtureStepResult.Failed("No AnimalFullInfoData row could be constructed from existing animals.");
            if (g4AnimalProgressRowCount == 0)
                return G4FixtureStepResult.Failed("No configured product-owned animal progress row was projected into AnimalFullInfoData; the native panel alone cannot satisfy the staged-QA positive gate.");
            if (g4AnimalUiOwnedState == null)
            {
                object? room = FindAnimalRoomForFixture(animals);
                Type? panelStateType = ResolveType("DolocTown.AnimalPanelUiState, Assembly-CSharp");
                if (room == null || panelStateType == null)
                    return G4FixtureStepResult.Failed("Animal panel room/state is unavailable for read-only UI observation.");
                g4AnimalUiOwnedState = EnterAnimalPanelForFixture(panelStateType, room);
                if (g4AnimalUiOwnedState == null)
                    return G4FixtureStepResult.Failed("AnimalPanelUiState did not open.");
                g4AnimalSwitchStage = 0;
                g4AnimalExpectedReceiptSequence = 0;
                g4AnimalAwaitingNextFrameGuard = false;
                return G4FixtureStepResult.Pending("QA-owned AnimalPanelUiState opened; waiting for read-only rendering and screenshot.");
            }

            if (!ReferenceEquals(GetCurrentNativeUiStateForFixture(), g4AnimalUiOwnedState))
                return G4FixtureStepResult.Failed("The QA-owned animal panel lost current-state ownership.");
            object? panel = ReadMember(g4AnimalUiOwnedState, "panel");
            string renderSummary = ReadAnimalProductObservationSummary();
            int requiredSwitches = Math.Min(3, animals.Count);
            if (g4AnimalAwaitingNextFrameGuard)
            {
                int receiptSequence = ReadAnimalReceiptSequence(renderSummary);
                if (renderSummary.IndexOf("state=visible", StringComparison.OrdinalIgnoreCase) < 0 ||
                    renderSummary.IndexOf("overlayRows=", StringComparison.OrdinalIgnoreCase) < 0 ||
                    renderSummary.IndexOf("nextFrameGuard=completed", StringComparison.OrdinalIgnoreCase) < 0 ||
                    receiptSequence < g4AnimalExpectedReceiptSequence)
                {
                    return G4FixtureStepResult.Pending(
                        "Waiting for AnimalHusbandryProgress next-frame guard after repeated switch " +
                        g4AnimalSwitchStage.ToString(CultureInfo.InvariantCulture) + "/" +
                        requiredSwitches.ToString(CultureInfo.InvariantCulture) +
                        "; expectedReceipt=" + g4AnimalExpectedReceiptSequence.ToString(CultureInfo.InvariantCulture) +
                        "; observation={" + renderSummary + "}.");
                }
                g4AnimalAwaitingNextFrameGuard = false;
                return G4FixtureStepResult.Pending(
                    "AnimalHusbandryProgress repeated switch " +
                    g4AnimalSwitchStage.ToString(CultureInfo.InvariantCulture) + "/" +
                    requiredSwitches.ToString(CultureInfo.InvariantCulture) +
                    " completed its one next-frame guard.");
            }
            if (g4AnimalSwitchStage < requiredSwitches)
            {
                int beforeSequence = ReadAnimalReceiptSequence(renderSummary);
                if (g4AnimalSwitchStage == 0)
                    panel?.GetType().GetMethod("RefreshView", BindingFlags.Public | BindingFlags.Instance)?.Invoke(panel, null);
                int targetIndex = g4AnimalSwitchStage;
                panel?.GetType().GetMethod("Select", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(int) }, null)?.Invoke(panel, new object[] { targetIndex });
                g4AnimalSwitchStage++;
                g4AnimalExpectedReceiptSequence = beforeSequence + 1;
                g4AnimalAwaitingNextFrameGuard = true;
                return G4FixtureStepResult.Pending(
                    "Selected AnimalHusbandryProgress animal index=" +
                    targetIndex.ToString(CultureInfo.InvariantCulture) +
                    "; awaiting one next-frame guard receipt.");
            }
            G4FixtureStepResult screenshot = ObserveScreenshotForFixture(screenshotPath, ref g4AnimalScreenshotPath, ref g4AnimalScreenshotRequestedAt);
            if (!screenshot.Completed || !screenshot.Succeeded)
                return screenshot;
            bool closed = CloseNativeUiOwnedByFixture(ref g4AnimalUiOwnedState);
            g4AnimalAwaitingNextFrameGuard = false;
            return closed
                ? G4FixtureStepResult.Verified("animals=" + animals.Count + "; constructed=" + g4AnimalConstructedCount + "; existingProgressRows=" + g4AnimalProgressRowCount + "; repeatedSwitches=" + requiredSwitches + "; nextFrameGuard=completed-per-switch; productObservation={" + renderSummary + "}; panelObserved=true; " + screenshot.Details + "; mutation=false; close=owned-only")
                : G4FixtureStepResult.Failed("Animal panel observation passed but the QA-owned UI did not close.");
        }

        private static int ReadAnimalReceiptSequence(string summary)
        {
            const string marker = "receiptSequence=";
            string text = summary ?? string.Empty;
            int start = text.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (start < 0)
                return 0;
            start += marker.Length;
            int end = start;
            while (end < text.Length && char.IsDigit(text[end]))
                end++;
            return int.TryParse(text.Substring(start, end - start), NumberStyles.None, CultureInfo.InvariantCulture, out int value)
                ? value
                : 0;
        }

        private static bool TryObserveAnimalProductRows(object data, out string summary)
        {
            summary = "AnimalHusbandryProgress product assembly is not loaded.";
            Assembly? assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(candidate => string.Equals(candidate.GetName().Name, "Yuuka.DTMAPI.AnimalHusbandryProgress", StringComparison.Ordinal));
            Type? callbacks = assembly?.GetType("Yuuka.DTMAPI.AnimalHusbandryProgress.AnimalHusbandryCallbacks", false, false);
            MethodInfo? observe = callbacks?.GetMethod("TryObserveRows", BindingFlags.Public | BindingFlags.Static);
            if (observe == null)
                return false;
            object?[] args = { data, summary };
            bool result = observe.Invoke(null, args) is bool ok && ok;
            summary = args[1] as string ?? summary;
            return result;
        }

        private static string ReadAnimalProductObservationSummary()
        {
            Assembly? assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(candidate => string.Equals(candidate.GetName().Name, "Yuuka.DTMAPI.AnimalHusbandryProgress", StringComparison.Ordinal));
            Type? callbacks = assembly?.GetType("Yuuka.DTMAPI.AnimalHusbandryProgress.AnimalHusbandryCallbacks", false, false);
            MethodInfo? observe = callbacks?.GetMethod("GetObservationSummary", BindingFlags.Public | BindingFlags.Static);
            return observe?.Invoke(null, null) as string ?? "AnimalHusbandryProgress product observation seam is unavailable.";
        }

        internal G4FixtureStepResult ObserveEquipmentSlotsUiForFixture(string screenshotPath, string summaryPath)
        {
            Type? stateType = ResolveType("DolocTown.EquipmentBarUiState, Assembly-CSharp");
            object? current = GetCurrentNativeUiStateForFixture();
            if (stateType == null)
                return G4FixtureStepResult.Failed("EquipmentBarUiState was not available for read-only observation.");
            if (g4EquipmentSlotsEvidenceCaptured)
            {
                string postCloseObservation =
                    ReadMoreEquipmentSlotsUiObservation(
                        out string postCloseOwner);
                if (current != null &&
                    stateType.IsInstanceOfType(current))
                {
                    return G4FixtureStepResult.Pending("EVIDENCE_CAPTURED_WAITING_ESCAPE; uiOwner=" + postCloseOwner + "; inputOwner=runner; evidenceOwner=qa; observation={" + postCloseObservation + "}.");
                }
                if (!postCloseOwner.Equals(
                        "compatibility-frozen",
                        StringComparison.Ordinal) &&
                    postCloseObservation.IndexOf(
                        "rendered=True",
                        StringComparison.Ordinal) >= 0)
                {
                    return G4FixtureStepResult.Pending("EVIDENCE_CAPTURED_WAITING_ESCAPE; uiOwner=" + postCloseOwner + "; inputOwner=runner; evidenceOwner=qa; observation={" + postCloseObservation + "}.");
                }
                if (postCloseOwner.Equals(
                        "compatibility-frozen",
                        StringComparison.Ordinal))
                {
                    return G4FixtureStepResult.Verified(g4EquipmentSlotsEvidenceSummary + "; closeReceipt=native-equipment-state-exited; compatibilityUiLifecycle=cleared-at-save-title-boundary; postCloseObservation={" + postCloseObservation + "}");
                }
                return G4FixtureStepResult.Verified(g4EquipmentSlotsEvidenceSummary + "; closeReceipt=equipment-owner-lifecycle-cleared; postCloseObservation={" + postCloseObservation + "}");
            }
            if (current == null || !stateType.IsInstanceOfType(current))
                return G4FixtureStepResult.Pending("Waiting for runner real B input to open the equipment-slot owner EquipmentBarUiState; current=" + (current?.GetType().FullName ?? "null") + ".");

            string observation =
                ReadMoreEquipmentSlotsUiObservation(
                    out string uiOwner);
            if (observation.IndexOf("rendered=True", StringComparison.Ordinal) < 0)
                return G4FixtureStepResult.Pending("EquipmentBarUiState is current; waiting for the selected equipment-slot owner render state. observation={" + observation + "}.");
            if (uiOwner.Equals(
                    "compatibility-frozen",
                    StringComparison.Ordinal) &&
                (observation.IndexOf("extra=3", StringComparison.Ordinal) < 0 ||
                 observation.IndexOf("stored=2", StringComparison.Ordinal) < 0 ||
                 observation.IndexOf("applied=2", StringComparison.Ordinal) < 0 ||
                 observation.IndexOf("box_hat:50/80", StringComparison.Ordinal) < 0))
            {
                return G4FixtureStepResult.Pending("Frozen compatibility UI rendered before its exact three-slot/two-item/shield state was observable. observation={" + observation + "}.");
            }

            G4FixtureStepResult screenshot = ObserveScreenshotForFixture(
                screenshotPath,
                ref g4EquipmentSlotsScreenshotPath,
                ref g4EquipmentSlotsScreenshotRequestedAt);
            if (!screenshot.Completed || !screenshot.Succeeded)
                return screenshot;

            File.WriteAllText(
                summaryPath,
                "Captured=" + DateTimeOffset.Now.ToString("o", CultureInfo.InvariantCulture) + Environment.NewLine +
                "UiOwner=" + uiOwner + Environment.NewLine +
                "EvidenceOwner=qa" + Environment.NewLine +
                "InputOwner=runner-real-B" + Environment.NewLine +
                "Screenshot=" + screenshotPath + Environment.NewLine +
                "ScreenshotReceipt=" + screenshot.Details + Environment.NewLine +
                "ProductObservation=" + observation + Environment.NewLine);
            g4EquipmentSlotsEvidenceCaptured = true;
            g4EquipmentSlotsEvidenceSummary = "EquipmentBarUiState=exact; uiOwner=" + uiOwner + "; inputOwner=runner-real-B; evidenceOwner=qa; observation={" + observation + "}; summary=" + summaryPath + "; " + screenshot.Details;
            return G4FixtureStepResult.Pending("EVIDENCE_CAPTURED_WAITING_ESCAPE; " + g4EquipmentSlotsEvidenceSummary);
        }

        private string ReadMoreEquipmentSlotsUiObservation(
            out string owner)
        {
            string product =
                ReadMoreEquipmentSlotsProductObservation();
            if (product.IndexOf(
                    "runtime=null",
                    StringComparison.Ordinal) < 0)
            {
                owner = "product";
                return product;
            }

            try
            {
                string ui =
                    ReadMoreEquipmentSlotsCompatibilityBackendSummary(
                        "GetEquipmentSlotsUiObservation");
                string state =
                    ReadMoreEquipmentSlotsCompatibilityBackendSummary(
                        "GetEquipmentSlotsStateSummaryForFixture",
                        MoreEquipmentSlotsTransitionOwnerId);
                string lifecycle =
                    ReadMoreEquipmentSlotsCompatibilityBackendSummary(
                        "GetEquipmentSlotsLifecycleSummary");
                owner = "compatibility-frozen";
                return "runtime=compatibility; " +
                    ui +
                    "; state={" +
                    state +
                    "}; lifecycle={" +
                    lifecycle +
                    "}";
            }
            catch (Exception ex)
            {
                owner = "unavailable";
                return "runtime=null; rendered=False; compatibilityError=" +
                    ex.GetType().Name +
                    ":" +
                    ex.Message;
            }
        }

        private static string ReadMoreEquipmentSlotsProductObservation()
        {
            Assembly? assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(candidate => string.Equals(
                    candidate.GetName().Name,
                    "DTMAPI.MoreEquipmentSlots",
                    StringComparison.Ordinal));
            Type? callbacks = assembly?.GetType(
                "DTMAPI.MoreEquipmentSlots.MoreEquipmentSlotsCallbacks",
                throwOnError: false,
                ignoreCase: false);
            object? productRuntime = callbacks?.GetField(
                "runtime",
                BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null);
            if (productRuntime == null)
                return "runtime=null; rendered=False";

            Type runtimeType = productRuntime.GetType();
            MethodInfo? diagnosticsMethod = runtimeType.GetMethod(
                "GetDiagnosticsSnapshot",
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic,
                binder: null,
                types: Type.EmptyTypes,
                modifiers: null);
            object? diagnostics =
                diagnosticsMethod?.Invoke(productRuntime, null);
            if (diagnostics != null)
            {
                int clones = ReadIntProperty(
                    diagnostics,
                    "CloneCount");
                int listeners = ReadIntProperty(
                    diagnostics,
                    "ListenerCount");
                int functions = ReadIntProperty(
                    diagnostics,
                    "FunctionCount");
                int roots = ReadIntProperty(
                    diagnostics,
                    "RootCount");
                bool rendered =
                    clones == 3 &&
                    listeners > 0 &&
                    roots == 1;
                return "runtime=ready; rendered=" +
                    rendered +
                    "; clones=" +
                    clones +
                    "; listeners=" +
                    listeners +
                    "; functions=" +
                    functions +
                    "; roots=" +
                    roots;
            }

            foreach (string methodName in new[]
            {
                "GetUiObservationSummary",
                "GetObservationSummary"
            })
            {
                MethodInfo? method = runtimeType.GetMethod(
                    methodName,
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic,
                    binder: null,
                    types: Type.EmptyTypes,
                    modifiers: null);
                if (method?.ReturnType == typeof(string))
                {
                    return method.Invoke(productRuntime, null) as string ??
                        "runtime=ready; rendered=unknown";
                }
            }

            foreach (string propertyName in new[]
            {
                "UiObservationSummary",
                "ObservationSummary"
            })
            {
                PropertyInfo? property = runtimeType.GetProperty(
                    propertyName,
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);
                if (property?.PropertyType == typeof(string))
                {
                    return property.GetValue(productRuntime) as string ??
                        "runtime=ready; rendered=unknown";
                }
            }

            return "runtime=ready; rendered=unknown";
        }

        private static int ReadIntProperty(
            object instance,
            string propertyName)
        {
            object? value = instance.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic)?.GetValue(instance);
            return value == null
                ? -1
                : Convert.ToInt32(
                    value,
                    CultureInfo.InvariantCulture);
        }

        internal G4FixtureStepResult ObserveAudioDefinitionsForFixture()
        {
            AudioReplacementService? service = audioReplacementFeature?.Service;
            if (service == null)
                return G4FixtureStepResult.Failed("Audio replacement service is unavailable.");
            AudioReplacementState state = service.GetState("Yuuka.DTMAPI.ManboCardboardAudio");
            int ready = state.Replacements.Count(item => item.PreloadReady);
            bool paperBoxReady = state.Replacements.Any(item =>
                item.PreloadReady &&
                item.ReplacementId.Equals("manbo-paper-box", StringComparison.OrdinalIgnoreCase) &&
                item.NativeSoundEvent.Equals("PLAY_RESOURCE_PAPER_BOX", StringComparison.OrdinalIgnoreCase));
            return paperBoxReady
                ? G4FixtureStepResult.Verified("status=" + state.Status + "; definitions=" + state.Replacements.Count + "; preloadReady=" + ready + "; paperBoxReady=true; refresh=false")
                : G4FixtureStepResult.Pending("Waiting for the existing paper-box definition preload. status=" + state.Status + "; definitions=" + state.Replacements.Count + "; preloadReady=" + ready + "; refresh=false");
        }

        internal G4FixtureStepResult ExerciseHatchVoiceForFixture()
        {
            AudioReplacementService? service = audioReplacementFeature?.Service;
            if (service == null)
                return G4FixtureStepResult.Failed("Audio replacement service is unavailable.");
            AudioReplacementState state = service.GetState("DTMAPI.HatchAssets");
            bool childReady = state.Replacements.Any(item => item.PreloadReady && item.ReplacementId.Equals("hatch-pet-child", StringComparison.OrdinalIgnoreCase));
            bool adultReady = state.Replacements.Any(item => item.PreloadReady && item.ReplacementId.Equals("hatch-pet-adult", StringComparison.OrdinalIgnoreCase));
            if (!childReady || !adultReady)
                return G4FixtureStepResult.Pending("Waiting for already-loaded Hatch definitions; refresh=false.");
            List<object> animals = FindAnimalsForFixture();
            object? hatch = animals.FirstOrDefault(item => GetAnimalIdForFixture(item).Equals("hatch", StringComparison.OrdinalIgnoreCase));
            if (hatch == null)
                return G4FixtureStepResult.Failed("No Hatch animal exists in the loaded save.");
            MethodInfo? stage = hatch.GetType().GetMethod("DEBUG_SetAdult", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo? play = hatch.GetType().GetMethod("PlayAnimalSound", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
            if (stage == null || play == null)
                return G4FixtureStepResult.Failed("Hatch stage/sound methods are unavailable.");
            object? vanilla = animals.FirstOrDefault(item => !ReferenceEquals(item, hatch) && GetAnimalIdForFixture(item).Equals("chicken", StringComparison.OrdinalIgnoreCase));
            MethodInfo? vanillaPlay = vanilla?.GetType().GetMethod("PlayAnimalSound", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
            g4HatchVoiceSession ??= new HatchVoiceFixtureSession(
                (out bool adult) => TryReadAnimalAdultForFixture(hatch, out adult),
                value => stage.Invoke(hatch, new object[] { value }),
                () => play.Invoke(hatch, null),
                vanillaPlay == null || vanilla == null ? (Action?)null : () => vanillaPlay.Invoke(vanilla, null));
            string run = g4HatchVoiceSession.Run();
            string close = g4HatchVoiceSession.Close();
            g4HatchVoiceSession = null;
            return G4FixtureStepResult.Verified(run + "; close={" + close + "}");
        }

        internal G4FixtureStepResult ExerciseCameraPlayableForFixture(string runId, string evidenceDirectory)
        {
            if (cameraFeature == null)
                return G4FixtureStepResult.Pending("Camera production feature is not available yet.");
            try
            {
                g4CameraSession ??= new CameraPlayableMovementFixtureScenario(
                    cameraFeature.ViewApi,
                    runId,
                    ownerId => cameraFeature.CountOwnerResources(ownerId),
                    CaptureAgentPositionForFixture,
                    RestoreAgentPositionForFixture,
                    CaptureCameraPositionForFixture,
                    RestoreCameraPositionForFixture,
                    MoveAgentAndCameraForFixture,
                    () => cameraFeature?.Update(),
                    TryRequestScreenshotForFixture,
                    evidenceDirectory);
                G4FixtureStepResult result = g4CameraSession.Advance();
                if (result.Completed && result.Succeeded)
                    g4CameraSession = null;
                return result;
            }
            catch (Exception ex)
            {
                return G4FixtureStepResult.Failed(ex.GetType().Name + ": " + ex.Message);
            }
        }

        internal G4FixtureStepResult ObserveContentMetadataForFixture(bool expectOilAbsent)
        {
            Type? configType = ResolveType("DolocTown.Config.DolocConfig, Assembly-CSharp");
            object? tables = ReadStaticMember(configType, "Tables");
            object? hats = ReadMemberOrNull(ReadMemberOrNull(tables, "TbHat"), "DataList");
            object? items = ReadMemberOrNull(ReadMemberOrNull(tables, "TbItem"), "DataList");
            List<object> hatRows = EnumerateObjects(hats).ToList();
            List<object> itemRows = EnumerateObjects(items).ToList();
            if (hatRows.Count == 0 || itemRows.Count == 0)
                return G4FixtureStepResult.Failed("TbHat/TbItem read-only enumeration returned no rows.");

            bool oilInItemTable = itemRows.Any(item => FirstText(ReadStringMember(item, "Id"), ReadStringMember(item, "id")).Equals("crude_oil", StringComparison.OrdinalIgnoreCase));
            string lut = "not-requested";
            if (expectOilAbsent)
            {
                if (oilInItemTable || runtime.GetIndexedContentItem("crude_oil") != null)
                    return G4FixtureStepResult.Failed("crude_oil unexpectedly exists during the Oil-absent metadata observation.");
                object? spawnTable = ReadMemberOrNull(tables, "TbItemSpawn");
                object? map = ReadMemberOrNull(spawnTable, "DataMap");
                object? spawnInfo = TryReadDictionaryValue(map, "coal_mine_drop");
                List<string> names = EnumerateObjects(ReadMemberOrNull(spawnInfo, "SpawnDatas"))
                    .Select(item => FirstText(ReadStringMember(item, "ItemName"), ReadStringMember(item, "item_name")))
                    .ToList();
                if (names.Count != 2 || !names[0].Equals("coal", StringComparison.OrdinalIgnoreCase) || !names[1].Equals("amber_ore", StringComparison.OrdinalIgnoreCase))
                    return G4FixtureStepResult.Failed("Oil-absent coal_mine_drop LUT differs from the native coal/amber order: " + string.Join("|", names) + ".");
                lut = string.Join("|", names);
            }
            return G4FixtureStepResult.Verified("hats=" + hatRows.Count + "; items=" + itemRows.Count + "; oilInItemTable=" + oilInItemTable + "; oilAbsentLut=" + lut + "; readOnly=true");
        }

        internal G4FixtureStepResult ObserveNativeUiLayoutForFixture()
        {
            NativeUiLayoutObservation? snapshot = NativeUiLayoutRepairService?.CaptureObservation();
            bool requireHomePage = snapshot != null && snapshot.State.IndexOf("HomePageUiState", StringComparison.OrdinalIgnoreCase) >= 0;
            if (!TryValidateNativeUiLayoutObservation(snapshot, requireHomePage, out string repairHooks))
            {
                snapshot = g4LastVerifiedNativeUiLayoutObservation;
                repairHooks = g4LastVerifiedNativeUiRepairHooks;
                if (!TryValidateNativeUiLayoutObservation(snapshot, snapshot != null && snapshot.State.IndexOf("HomePageUiState", StringComparison.OrdinalIgnoreCase) >= 0, out string retainedHooks))
                    return G4FixtureStepResult.Failed("No concrete HomePage/MainMenu native UI repair receipt exists. current={" + FormatNativeUiObservation(NativeUiLayoutRepairService?.CaptureObservation()) + "}; retained={" + FormatNativeUiObservation(snapshot) + "}; hooks={" + repairHooks + "}.");
                repairHooks = retainedHooks;
            }
            g4LastVerifiedNativeUiLayoutObservation = snapshot;
            g4LastVerifiedNativeUiRepairHooks = repairHooks;
            return G4FixtureStepResult.Verified(FormatNativeUiObservation(snapshot) + "; hooks={" + repairHooks + "}; concrete=true; observerCallbacks=" + g4UiObservationCount);
        }

        internal bool IsNormalGameplayForFixture()
        {
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            return runtime.UI.InputContext.Equals("Gameplay", StringComparison.OrdinalIgnoreCase) && ReadStaticBoolMember(dolocApi, "IsNormalState", false);
        }

        internal string CloseG4Fixtures()
        {
            bool title = CloseTitleOverlayOwnedByFixture();
            bool official = CloseNativeUiOwnedByFixture(ref g4OfficialUiOwnedState);
            bool pause = CloseNativeUiOwnedByFixture(ref g4PauseUiOwnedState);
            bool animal = CloseNativeUiOwnedByFixture(ref g4AnimalUiOwnedState);
            g4AnimalSwitchStage = 0;
            g4AnimalExpectedReceiptSequence = 0;
            g4AnimalAwaitingNextFrameGuard = false;
            g4AnimalPreflightComplete = false;
            g4AnimalConstructedCount = 0;
            g4AnimalProgressRowCount = 0;
            bool debug = CloseDebugConsoleOwnedByFixture();
            bool save = RestoreG4SaveSlotsCoordinatorReceipt();
            bool moreSavesPostTitle = CloseNativeUiOwnedByFixture(ref g4MoreSavesPostTitleUiState);
            bool hatch = CloseRetainedHatchSession();
            bool camera = CloseRetainedCameraSession();
            string summary = "title=" + title + "; official=" + official + "; pause=" + pause + "; animal=" + animal + "; debug=" + debug + "; save=" + save + "; moreSavesPostTitle=" + moreSavesPostTitle + "; hatch=" + hatch + "; camera=" + camera;
            if (!title || !official || !pause || !animal || !debug || !save || !moreSavesPostTitle || !hatch || !camera)
                throw new InvalidOperationException("G4 cleanup receipt failed: " + summary + ".");
            return summary;
        }

        private G4FixtureStepResult ObserveScreenshotForFixture(
            string requestedPath,
            ref string? receiptPath,
            ref DateTimeOffset requestedAt)
        {
            if (string.IsNullOrWhiteSpace(requestedPath) || !Path.IsPathRooted(requestedPath))
                return G4FixtureStepResult.Failed("The QA screenshot receipt path must be absolute.");
            if (receiptPath == null)
            {
                receiptPath = Path.GetFullPath(requestedPath);
                string? directory = Path.GetDirectoryName(receiptPath);
                if (string.IsNullOrWhiteSpace(directory))
                    return G4FixtureStepResult.Failed("The QA screenshot receipt directory is unavailable.");
                Directory.CreateDirectory(directory);
                if (!TryRequestScreenshotForFixture(receiptPath))
                    return G4FixtureStepResult.Failed("Unity ScreenCapture could not accept the QA-owned screenshot request: " + receiptPath + ".");
                requestedAt = DateTimeOffset.UtcNow;
                return G4FixtureStepResult.Pending("screenshotRequested=" + receiptPath + "; waitingForFile=true");
            }

            try
            {
                var file = new FileInfo(receiptPath);
                if (file.Exists && file.Length > 0)
                    return G4FixtureStepResult.Verified("screenshot=" + receiptPath + "; screenshotBytes=" + file.Length);
            }
            catch (IOException)
            {
                // A capture may still be completing on Unity's render thread.
            }

            if ((DateTimeOffset.UtcNow - requestedAt).TotalSeconds > 10d)
                return G4FixtureStepResult.Failed("The QA-owned screenshot file was not created within 10 seconds: " + receiptPath + ".");
            return G4FixtureStepResult.Pending("screenshotRequested=" + receiptPath + "; waitingForFile=true");
        }

        private static bool TryRequestScreenshotForFixture(string path)
        {
            Type? captureType = ResolveType("UnityEngine.ScreenCapture, UnityEngine.CoreModule") ??
                ResolveType("UnityEngine.ScreenCapture, UnityEngine");
            MethodInfo? method = captureType?.GetMethod("CaptureScreenshot", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
            if (method == null)
                return false;
            method.Invoke(null, new object[] { path });
            return true;
        }

        private bool ContinueModChangeForFixture(object state)
        {
            if (!g4SaveSelectionVerifiedBeforeContinuation || g4SaveIndex < 0)
                return false;
            FieldInfo? continuationField = FindFieldInHierarchy(state.GetType(), "onConfirm");
            if (!(continuationField?.GetValue(state) is Action original))
                return false;
            if (g4ModChangeReceiptState != null && !ReferenceEquals(g4ModChangeReceiptState, state))
                return false;
            g4ModChangeReceiptState = state;
            g4ModChangeReceiptField = continuationField;
            g4ModChangeOriginalAction = original;
            Action wrapped = new Action(() =>
            {
                if (!g4SaveSelectionVerifiedBeforeContinuation)
                    throw new InvalidOperationException("The requested save selection did not retain its pre-continuation readback receipt.");
                try { original(); }
                finally
                {
                    if (!RestoreG4ModChangeDelegateReceipt())
                        throw new InvalidOperationException("The native mod-change continuation delegate was not restored.");
                }
            });
            g4ModChangeWrappedAction = wrapped;
            continuationField.SetValue(state, wrapped);
            if (!ReferenceEquals(continuationField.GetValue(state), wrapped))
            {
                RestoreG4ModChangeDelegateReceipt();
                return false;
            }
            MethodInfo? continueMethod = FindMethodInHierarchy(state.GetType(), "OnConfirm", 0);
            if (continueMethod == null)
            {
                RestoreG4ModChangeDelegateReceipt();
                return false;
            }
            continueMethod.Invoke(state, null);
            // ModChangeListUiState.OnConfirm dispatches its continuation through
            // DolocAPI.DelayFrame. The exact wrapper receipt may therefore remain
            // installed until the next native frame; it restores itself in its
            // finally block. The old GameData panel is never touched here.
            return g4ModChangeReceiptState == null ||
                (ReferenceEquals(g4ModChangeReceiptState, state) &&
                    ReferenceEquals(g4ModChangeReceiptField, continuationField) &&
                    ReferenceEquals(continuationField.GetValue(state), wrapped));
        }

        private bool RequestSingleDirectSaveFallbackForFixture(out bool issued, out string details)
        {
            issued = false;
            details = string.Empty;
            if (!runtime.RefactorOptions.SaveLoadRequestCoordinator)
            {
                details = "coordinatorEnabled=false; directBlocked=true";
                return false;
            }
            SaveLoadRequestSnapshot snapshot = runtime.SaveLoadRequestSnapshot;
            SaveLoadRequestEntry? nativeReceipt = snapshot.Requests.LastOrDefault(item =>
                item.Slot == g4SaveIndex && item.NativeEntered &&
                snapshot.NativeEnterCount > g4SaveNativeEnterBaseline);
            if (nativeReceipt != null || snapshot.NativeReturnCount > g4SaveNativeReturnBaseline)
            {
                details = "coordinatorEnabled=true; nativeReceipt=" + (nativeReceipt?.RequestId ?? "return-count") +
                    "; nativeEnterDelta=" + (snapshot.NativeEnterCount - g4SaveNativeEnterBaseline) +
                    "; nativeReturnDelta=" + (snapshot.NativeReturnCount - g4SaveNativeReturnBaseline) +
                    "; directBlocked=true";
                return true;
            }
            if (!ReferenceEquals(GetCurrentNativeUiStateForFixture(), g4SaveUiState))
            {
                details = "coordinatorEnabled=true; nativeUiTransitionObserved=true; directBlocked=true; " + snapshot.FormatSummary();
                return true;
            }
            if (!VerifyRequestedSaveSelectionForFixture(g4SaveUiState, g4SaveIndex, out string selectionDetails))
            {
                details = "coordinatorEnabled=true; nativeUiTransitionObserved=false; selectionReadback=false; " + selectionDetails;
                return false;
            }
            if (snapshot.HasActiveRequest && snapshot.ActiveSlot != g4SaveIndex)
            {
                details = "coordinatorEnabled=true; unrelatedActiveRequest=true; directBlocked=true; " + snapshot.FormatSummary();
                return true;
            }
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            MethodInfo? method = FindMethodInHierarchy(dolocApi, "LoadGame", 1);
            if (method == null || g4SaveIndex < 0)
            {
                details = "native load method or requested index unavailable";
                return false;
            }
            if (runtime.ShouldSuppressDtmapiLoadGameRequest(g4SaveIndex, "DTMAPI.QA", "G4.SaveSlotCoordinator", out string suppression))
            {
                details = "coordinatorEnabled=true; suppressed=" + suppression + "; directBlocked=true; " + selectionDetails;
                return true;
            }
            method.Invoke(null, new object[] { g4SaveIndex });
            issued = true;
            details = "coordinatorEnabled=true; nativeReceipt=false; nativeUiTransitionObserved=false; issued=true; index=" + g4SaveIndex + "; " + selectionDetails;
            return true;
        }

        private bool SelectRequestedSaveForFixture(object? state, int index, out string details)
        {
            details = string.Empty;
            if (state == null || index < 0)
            {
                details = "state/index unavailable";
                return false;
            }
            object? panel = ReadMember(state, "panel");
            MethodInfo? panelSelect = FindMethodInHierarchy(panel?.GetType(), "Select", 1);
            if (panel == null || panelSelect == null)
            {
                details = "official panel/Select unavailable";
                return false;
            }
            panelSelect.Invoke(panel, new object[] { index });
            return VerifyRequestedSaveSelectionForFixture(state, index, out details);
        }

        private bool VerifyRequestedSaveSelectionForFixture(object? state, int index, out string details)
        {
            details = string.Empty;
            if (state == null || index < 0)
            {
                details = "state/index unavailable";
                return false;
            }
            object? panel = ReadMember(state, "panel");
            int stateIndex = ReadOptionalInt(state, "currentIndex");
            int panelIndex = ReadOptionalInt(panel, "selectedIndex");
            MethodInfo? getSlot = FindMethodInHierarchy(panel?.GetType(), "GetSlot", 1);
            object? selectedSlot = null;
            try { selectedSlot = getSlot?.Invoke(panel, new object[] { index }); } catch { }
            bool verified = stateIndex == index && panelIndex == index && selectedSlot != null;
            details = "stateIndex=" + stateIndex + "; panelIndex=" + panelIndex + "; selectedSlot=" + (selectedSlot != null).ToString().ToLowerInvariant() + "; expectedIndex=" + index + "; readback=" + verified.ToString().ToLowerInvariant();
            return verified;
        }

        private bool RestoreG4SaveSlotsCoordinatorReceipt()
        {
            if (!g4SaveCoordinatorArmed)
                return true;
            bool delegateRestored = RestoreG4ModChangeDelegateReceipt();
            if (g4SaveUiState == null)
            {
                if (delegateRestored)
                    g4SaveCoordinatorArmed = false;
                return delegateRestored;
            }
            bool stateStillCurrent = ReferenceEquals(GetCurrentNativeUiStateForFixture(), g4SaveUiState);
            bool selectionRestored = stateStillCurrent
                ? VerifyRequestedSaveSelectionForFixture(g4SaveUiState, g4SaveIndex, out _)
                : g4SaveSelectionVerifiedBeforeContinuation;
            if (!selectionRestored && stateStillCurrent)
                selectionRestored = SelectRequestedSaveForFixture(g4SaveUiState, g4SaveIndex, out _);
            // OnConfirm is the exact QA-owned native continuation for this
            // GameDataUiState. Once it has changed the current state, its state
            // exit phase is complete; cleanup must only remove that state's
            // cached UI and must never pop the unrelated successor state.
            if (!stateStillCurrent && g4SaveCoordinatorStage >= 2 && g4SaveSelectionVerifiedBeforeContinuation)
                MarkNativeUiStateExitedForFixture(g4SaveUiState);
            bool ownedUiClosed = CloseNativeUiOwnedByFixture(ref g4SaveUiState);
            bool restored = delegateRestored && selectionRestored && ownedUiClosed;
            if (restored)
                g4SaveCoordinatorArmed = false;
            return restored;
        }

        private G4FixtureStepResult FailSaveSlotCoordinatorOwnershipLoss(string phase)
        {
            bool delegateRestored = RestoreG4ModChangeDelegateReceipt();
            bool staleReceipt = g4SaveUiState != null;
            g4SaveUiState = null;
            g4SaveCoordinatorArmed = false;
            g4SaveCoordinatorStage = -1;
            return G4FixtureStepResult.Failed(
                "The QA-owned GameDataUiState lost exact current-state ownership " + phase +
                "; staleReceiptCleared=" + staleReceipt.ToString().ToLowerInvariant() +
                "; delegateRestored=" + delegateRestored.ToString().ToLowerInvariant() +
                "; unrelatedCurrentUiUntouched=true; continuationIssued=false.");
        }

        private bool RestoreG4ModChangeDelegateReceipt()
        {
            if (g4ModChangeReceiptState == null || g4ModChangeReceiptField == null || g4ModChangeOriginalAction == null || g4ModChangeWrappedAction == null)
                return true;
            try
            {
                object? current = g4ModChangeReceiptField.GetValue(g4ModChangeReceiptState);
                if (ReferenceEquals(current, g4ModChangeOriginalAction))
                {
                    g4ModChangeReceiptState = null;
                    g4ModChangeReceiptField = null;
                    g4ModChangeOriginalAction = null;
                    g4ModChangeWrappedAction = null;
                    return true;
                }
                if (!ReferenceEquals(current, g4ModChangeWrappedAction))
                    return false;
                g4ModChangeReceiptField.SetValue(g4ModChangeReceiptState, g4ModChangeOriginalAction);
                bool restored = ReferenceEquals(g4ModChangeReceiptField.GetValue(g4ModChangeReceiptState), g4ModChangeOriginalAction);
                if (restored)
                {
                    g4ModChangeReceiptState = null;
                    g4ModChangeReceiptField = null;
                    g4ModChangeOriginalAction = null;
                    g4ModChangeWrappedAction = null;
                }
                return restored;
            }
            catch { return false; }
        }

        private bool CloseRetainedHatchSession()
        {
            if (g4HatchVoiceSession == null)
                return true;
            try { g4HatchVoiceSession.Close(); g4HatchVoiceSession = null; return true; }
            catch { return false; }
        }

        private bool CloseRetainedCameraSession()
        {
            if (g4CameraSession == null)
                return true;
            try { g4CameraSession.Close(); g4CameraSession = null; return true; }
            catch { return false; }
        }

        private bool CloseTitleOverlayOwnedByFixture()
        {
            if (g4TitleOverlaySessionSequence == 0)
                return true;
            if (!runtime.UI.IsOpen)
            {
                bool sameClosedSession = runtime.UI.OverlaySessionSequence == g4TitleOverlaySessionSequence;
                if (sameClosedSession)
                    ClearTitleOverlayReceipt();
                return sameClosedSession;
            }
            if (!IsTitleOverlayReceiptCurrent())
                return false;
            runtime.UI.Close();
            bool closed = !runtime.UI.IsOpen && runtime.UI.OverlaySessionSequence == g4TitleOverlaySessionSequence;
            if (closed)
                ClearTitleOverlayReceipt();
            return closed;
        }

        private void CaptureTitleOverlayReceipt()
        {
            g4TitleOverlaySessionSequence = runtime.UI.OverlaySessionSequence;
            g4TitleOverlayMenuId = runtime.UI.ActiveMenuId;
            g4TitleOverlayOwnerId = runtime.UI.ActiveMenuOwnerId;
            g4TitleOverlayPage = runtime.UI.CurrentPage;
            g4TitleOverlayRequestedConfigId = runtime.UI.RequestedConfigUniqueId;
        }

        private bool IsTitleOverlayReceiptCurrent()
        {
            return runtime.UI.IsOpen &&
                runtime.UI.OverlaySessionSequence == g4TitleOverlaySessionSequence &&
                runtime.UI.ActiveMenuId.Equals(g4TitleOverlayMenuId, StringComparison.Ordinal) &&
                runtime.UI.ActiveMenuOwnerId.Equals(g4TitleOverlayOwnerId, StringComparison.Ordinal) &&
                runtime.UI.CurrentPage == g4TitleOverlayPage &&
                string.Equals(runtime.UI.RequestedConfigUniqueId, g4TitleOverlayRequestedConfigId, StringComparison.OrdinalIgnoreCase);
        }

        private bool IsExpectedTitleSettingsOverlay()
        {
            return g4TitleOverlayMenuId.Equals("DTMAPI.Config", StringComparison.Ordinal) &&
                string.IsNullOrEmpty(g4TitleOverlayOwnerId) &&
                g4TitleOverlayPage == DtmOverlayPage.Config &&
                string.IsNullOrWhiteSpace(g4TitleOverlayRequestedConfigId);
        }

        private void ClearTitleOverlayReceipt()
        {
            g4TitleOverlaySessionSequence = 0;
            g4TitleOverlayMenuId = string.Empty;
            g4TitleOverlayOwnerId = string.Empty;
            g4TitleOverlayPage = default;
            g4TitleOverlayRequestedConfigId = null;
        }

        private bool CloseDebugConsoleOwnedByFixture()
        {
            // QA never owns the production DebugConsole surface, so cleanup must not
            // close or otherwise mutate the real product owner's UI.
            return true;
        }

        private object? GetCurrentNativeUiStateForFixture()
        {
            object? input = ReadStaticMember(ResolveType("DolocAPI, Assembly-CSharp"), "userInput");
            return input == null ? null : ReadMember(input, "CurrentState");
        }

        internal static bool HasExactNativeUiReceiptForFixture(object? currentState, object? ownedState) =>
            ownedState != null && ReferenceEquals(currentState, ownedState);

        private bool CloseNativeUiOwnedByFixture(ref object? ownedState)
        {
            if (ownedState == null)
                return true;

            object receipt = ownedState;
            bool stateExitCompleted = HasNativeUiStateExitedForFixture(receipt);
            object? userInput = ReadStaticMember(ResolveType("DolocAPI, Assembly-CSharp"), "userInput");
            object? manager = ReadStaticMember(ResolveType("DolocAPI, Assembly-CSharp"), "gameUiStates");
            bool closed = TryCloseExactNativeUiReceiptForFixture(
                receipt,
                GetCurrentNativeUiStateForFixture,
                state => TryInvokeExactPopStateForFixture(userInput, state),
                stateType => TryInvokeExactGenericRemoveUiForFixture(manager, stateType),
                ref stateExitCompleted);

            if (stateExitCompleted)
                MarkNativeUiStateExitedForFixture(receipt);
            if (closed)
            {
                ClearNativeUiStateExitedForFixture(receipt);
                ownedState = null;
            }
            return closed;
        }

        internal static bool TryCloseExactNativeUiReceiptForFixture(
            object ownedState,
            Func<object?> getCurrentState,
            Func<object, bool> tryPopState,
            Func<Type, bool> tryRemoveCache,
            ref bool stateExitCompleted)
        {
            try
            {
                if (!stateExitCompleted)
                {
                    if (!ReferenceEquals(getCurrentState(), ownedState))
                        return false;
                    if (!tryPopState(ownedState))
                        return false;
                    if (ReferenceEquals(getCurrentState(), ownedState))
                        return false;
                    stateExitCompleted = true;
                }

                return tryRemoveCache(ownedState.GetType());
            }
            catch
            {
                return false;
            }
        }

        private static bool TryInvokeExactPopStateForFixture(object? userInput, object? ownedState)
        {
            if (userInput == null || ownedState == null)
                return false;
            try
            {
                MethodInfo? pop = null;
                for (Type? type = userInput.GetType(); type != null && pop == null; type = type.BaseType)
                {
                    pop = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                        .FirstOrDefault(item =>
                            item.Name.Equals("TryPopState", StringComparison.Ordinal) &&
                            item.ReturnType == typeof(bool) &&
                            item.GetParameters().Length == 1 &&
                            item.GetParameters()[0].ParameterType.IsInstanceOfType(ownedState));
                }
                return pop?.Invoke(userInput, new[] { ownedState }) is bool popped && popped;
            }
            catch
            {
                return false;
            }
        }

        private bool HasNativeUiStateExitedForFixture(object state) =>
            g4NativeUiExitedStateReceipts.Any(receipt => ReferenceEquals(receipt, state));

        private void MarkNativeUiStateExitedForFixture(object state)
        {
            if (!HasNativeUiStateExitedForFixture(state))
                g4NativeUiExitedStateReceipts.Add(state);
        }

        private void ClearNativeUiStateExitedForFixture(object state)
        {
            for (int index = g4NativeUiExitedStateReceipts.Count - 1; index >= 0; index--)
            {
                if (ReferenceEquals(g4NativeUiExitedStateReceipts[index], state))
                    g4NativeUiExitedStateReceipts.RemoveAt(index);
            }
        }

        internal static bool TryInvokeExactGenericRemoveUiForFixture(object? manager, Type? ownedStateType)
        {
            if (manager == null || ownedStateType == null)
                return false;
            try
            {
                MethodInfo? remove = manager.GetType()
                    .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .FirstOrDefault(item =>
                        item.Name.Equals("RemoveUI", StringComparison.Ordinal) &&
                        item.IsGenericMethodDefinition &&
                        item.GetGenericArguments().Length == 1 &&
                        item.GetParameters().Length == 0);
                if (remove == null)
                    return false;
                remove.MakeGenericMethod(ownedStateType).Invoke(manager, null);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static object? EnterNativeUiForFixture(Type stateType, object? startupDelegate)
        {
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            MethodInfo? generic = dolocApi?.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(item => item.Name.Equals("EnterUI", StringComparison.Ordinal) && item.IsGenericMethodDefinition && item.GetParameters().Length == (startupDelegate == null ? 0 : 1));
            if (generic == null)
                return null;
            return generic.MakeGenericMethod(stateType).Invoke(null, startupDelegate == null ? null : new[] { startupDelegate });
        }

        private static object? EnterAnimalPanelForFixture(Type stateType, object room)
        {
            MethodInfo? startup = stateType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(item => item.Name.Equals("HandleStartUpArgs", StringComparison.Ordinal) && item.GetParameters().Length == 1 && item.GetParameters()[0].ParameterType.IsInstanceOfType(room));
            if (startup == null)
                return null;
            Type delegateType = typeof(Func<,>).MakeGenericType(stateType, typeof(bool));
            ParameterExpression state = Expression.Parameter(stateType, "state");
            Delegate callback = Expression.Lambda(delegateType, Expression.Call(state, startup, Expression.Constant(room, startup.GetParameters()[0].ParameterType)), state).Compile();
            return EnterNativeUiForFixture(stateType, callback);
        }

        private List<object> FindAnimalsForFixture()
        {
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            var animals = new List<object>();
            AddAnimalsFromRoomForFixture(animals, ReadStaticMember(dolocApi, "CurrentRoom"));
            object? archive = ReadStaticMember(dolocApi, "archiveHandle");
            AddAnimalsFromRoomForFixture(animals, ReadMemberOrNull(archive, "MainFarm"));
            AddAnimalsFromRoomForFixture(animals, ReadMemberOrNull(ReadMemberOrNull(archive, "farmData"), "MainFarm"));
            return animals;
        }

        private static object? FindAnimalRoomForFixture(IEnumerable<object> animals)
        {
            object? room = animals.Select(item => ReadMemberOrNull(item, "currentRoom")).FirstOrDefault(item => item != null);
            if (room != null) return room;
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            return ReadStaticMember(dolocApi, "CurrentRoom") ?? ReadMemberOrNull(ReadStaticMember(dolocApi, "archiveHandle"), "MainFarm");
        }

        private static void AddAnimalsFromRoomForFixture(List<object> animals, object? room)
        {
            if (room == null) return;
            AddUniqueObjects(animals, ReadMemberOrNull(room, "AllAnimals"));
            AddUniqueObjects(animals, ReadMemberOrNull(ReadMemberOrNull(room, "animalSystem"), "Animals"));
            AddUniqueObjects(animals, ReadMemberOrNull(ReadMemberOrNull(room, "DM_animal"), "AllAnimals"));
        }

        private static void AddUniqueObjects(List<object> target, object? source)
        {
            foreach (object item in EnumerateObjects(source))
                if (!target.Contains(item)) target.Add(item);
        }

        private static bool TryReadAnimalAdultForFixture(object animal, out bool adult)
        {
            adult = false;
            object? data = ReadMemberOrNull(animal, "data");
            object? rawAdult = ReadMemberOrNull(data, "isAdult");
            if (rawAdult is bool value)
            {
                adult = value;
                return true;
            }
            object? child = ReadMemberOrNull(data, "isChild");
            if (child is bool childValue)
            {
                adult = !childValue;
                return true;
            }
            return false;
        }

        private static string GetAnimalIdForFixture(object animal) => FirstText(ReadStringMember(animal, "protoName"), animal.GetType().Name);

        private AgentPositionFixtureReceipt CaptureAgentPositionForFixture()
        {
            object? vector = ReadStaticMember(ResolveType("DolocAPI, Assembly-CSharp"), "AgentPosition");
            return TryReadVector(vector, out double x, out double y, out double z)
                ? new AgentPositionFixtureReceipt(true, x, y, z)
                : new AgentPositionFixtureReceipt(false, 0d, 0d, 0d);
        }

        private CameraPositionFixtureReceipt CaptureCameraPositionForFixture()
        {
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            object? controller = ReadStaticMember(dolocApi, "cameraController");
            object? vector = ReadMemberOrNull(controller, "position2d");
            return TryReadVector(vector, out double x, out double y, out double z)
                ? new CameraPositionFixtureReceipt(true, x, y, z)
                : new CameraPositionFixtureReceipt(false, 0d, 0d, 0d);
        }

        private bool RestoreAgentPositionForFixture(AgentPositionFixtureReceipt receipt)
        {
            if (!receipt.Captured) return true;
            if (!TryWriteAgentPositionForFixture(receipt.X, receipt.Y, receipt.Z))
                return false;
            AgentPositionFixtureReceipt restored = CaptureAgentPositionForFixture();
            return restored.Captured &&
                Math.Abs(restored.X - receipt.X) <= 0.5d &&
                Math.Abs(restored.Y - receipt.Y) <= 0.5d &&
                Math.Abs(restored.Z - receipt.Z) <= 0.5d;
        }

        private bool RestoreCameraPositionForFixture(CameraPositionFixtureReceipt receipt)
        {
            if (!receipt.Captured) return true;
            if (!TryWriteCameraPositionForFixture(receipt.X, receipt.Y))
                return false;
            CameraPositionFixtureReceipt restored = CaptureCameraPositionForFixture();
            return restored.Captured &&
                Math.Abs(restored.X - receipt.X) <= 1d &&
                Math.Abs(restored.Y - receipt.Y) <= 1d;
        }

        private CameraPlayablePositionSample MoveAgentAndCameraForFixture(double x, double y, double z)
        {
            if (!TryWriteAgentPositionForFixture(x, y, z))
                throw new InvalidOperationException("DolocAPI.AgentPosition is not writable.");
            if (!TryWriteCameraPositionForFixture(x, y))
                throw new InvalidOperationException("CameraController.ForceSetPosition(Vector2, false) is unavailable.");

            AgentPositionFixtureReceipt agent = CaptureAgentPositionForFixture();
            CameraPositionFixtureReceipt camera = CaptureCameraPositionForFixture();
            if (!agent.Captured)
                throw new InvalidOperationException("CameraPlayable could not read back AgentPosition after movement.");
            if (!camera.Captured)
                throw new InvalidOperationException("CameraPlayable could not read back CameraController.position2d after movement.");
            return new CameraPlayablePositionSample
            {
                AgentX = agent.X,
                AgentY = agent.Y,
                AgentZ = agent.Z,
                CameraX = camera.X,
                CameraY = camera.Y,
                CameraZ = camera.Z
            };
        }

        private static bool TryWriteAgentPositionForFixture(double x, double y, double z)
        {
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            if (dolocApi == null)
                return false;
            PropertyInfo? property = dolocApi.GetProperty("AgentPosition", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (property != null && property.CanWrite)
            {
                object? value = CreateVector(property.PropertyType, x, y, z);
                if (value != null)
                {
                    property.SetValue(null, value);
                    return true;
                }
            }
            FieldInfo? field = dolocApi.GetField("AgentPosition", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (field != null)
            {
                object? value = CreateVector(field.FieldType, x, y, z);
                if (value != null)
                {
                    field.SetValue(null, value);
                    return true;
                }
            }
            return false;
        }

        private static bool TryWriteCameraPositionForFixture(double x, double y)
        {
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            if (dolocApi == null)
                return false;
            object? controller = ReadStaticMember(dolocApi, "cameraController");
            MethodInfo? forceSetPosition = controller?.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(item =>
                {
                    ParameterInfo[] parameters = item.GetParameters();
                    return item.Name.Equals("ForceSetPosition", StringComparison.Ordinal) && parameters.Length == 2 &&
                        (parameters[0].ParameterType.FullName ?? string.Empty).IndexOf("Vector2", StringComparison.OrdinalIgnoreCase) >= 0 &&
                        parameters[1].ParameterType == typeof(bool);
                });
            if (forceSetPosition == null)
                return false;
            object? cameraTarget = CreateVector(forceSetPosition.GetParameters()[0].ParameterType, x, y, 0d);
            if (cameraTarget == null)
                return false;
            forceSetPosition.Invoke(controller, new[] { cameraTarget, (object)false });
            return true;
        }

        private static object? CreateVector(Type type, double x, double y, double z)
        {
            try
            {
                ConstructorInfo? ctor3 = type.GetConstructor(new[] { typeof(float), typeof(float), typeof(float) });
                if (ctor3 != null) return ctor3.Invoke(new object[] { (float)x, (float)y, (float)z });
                ConstructorInfo? ctor2 = type.GetConstructor(new[] { typeof(float), typeof(float) });
                return ctor2?.Invoke(new object[] { (float)x, (float)y });
            }
            catch { return null; }
        }

        private static bool TryReadVector(object? vector, out double x, out double y, out double z)
        {
            x = y = z = 0d;
            if (vector == null) return false;
            try
            {
                object? rawX = ReadMember(vector, "x");
                object? rawY = ReadMember(vector, "y");
                object? rawZ = ReadMember(vector, "z");
                if (rawX == null || rawY == null) return false;
                x = Convert.ToDouble(rawX, CultureInfo.InvariantCulture);
                y = Convert.ToDouble(rawY, CultureInfo.InvariantCulture);
                z = rawZ == null ? 0d : Convert.ToDouble(rawZ, CultureInfo.InvariantCulture);
                return true;
            }
            catch { return false; }
        }

        private static bool IsDtmApiOfficialRowForFixture(object row)
        {
            string id = FirstText(ReadStringMember(row, "id"), ReadStringMember(row, "Id"));
            string title = FirstText(ReadStringMember(row, "title"), ReadStringMember(row, "Title"));
            return id.IndexOf("Yuuka_DTMAPI_", StringComparison.OrdinalIgnoreCase) >= 0 || title.IndexOf("DTMAPI", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool TryValidateNativeUiLayoutObservation(
            NativeUiLayoutObservation? observation,
            bool requireHomePage,
            out string repairHooks)
        {
            NativeUiLayoutDiagnosticsFeature? feature = nativeUiLayoutDiagnosticsFeature;
            bool homeHooks = feature != null && feature.HomePageRepairReceiptReady;
            bool mainHooks = feature != null && feature.MainMenuRepairReceiptReady;
            repairHooks = "repairService=" + (feature?.RepairService != null).ToString().ToLowerInvariant() +
                "; home=" + homeHooks.ToString().ToLowerInvariant() +
                "; main=" + mainHooks.ToString().ToLowerInvariant();
            if (observation == null)
                return false;

            bool home = observation.State.IndexOf("HomePageUiState", StringComparison.OrdinalIgnoreCase) >= 0;
            if (requireHomePage && !home)
                return false;
            if (home)
            {
                return homeHooks &&
                    observation.InputContext.Equals("HomePageUiState", StringComparison.OrdinalIgnoreCase) &&
                    observation.TargetType.IndexOf("HomePageTextMenu", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    observation.Slots > 0 && observation.VisibleSlots > 0 &&
                    observation.ConstraintCount == 1;
            }

            bool main = observation.State.IndexOf("MainMenuUiState", StringComparison.OrdinalIgnoreCase) >= 0;
            return !requireHomePage && main && mainHooks &&
                observation.InputContext.IndexOf("MainMenuUiState", StringComparison.OrdinalIgnoreCase) >= 0 &&
                observation.TargetType.Equals("DolocTown.UI.MenuUI", StringComparison.Ordinal) &&
                observation.Slots > 0 && observation.VisibleSlots > 0 &&
                observation.ConstraintCount == observation.VisibleSlots;
        }

        private static string FormatNativeUiObservation(NativeUiLayoutObservation? observation)
        {
            return observation == null
                ? "state=missing; context=missing; target=missing; slots=-1; visible=-1; constraint=-1"
                : "state=" + observation.State + "; context=" + observation.InputContext +
                  "; target=" + observation.TargetType + "; slots=" + observation.Slots +
                  "; visible=" + observation.VisibleSlots + "; constraint=" + observation.ConstraintCount;
        }

        private static FieldInfo? FindFieldInHierarchy(Type? type, string name)
        {
            for (Type? current = type; current != null; current = current.BaseType)
            {
                FieldInfo? field = current.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                if (field != null) return field;
            }
            return null;
        }

        private static object? ReadMemberOrNull(object? instance, string name) => instance == null ? null : ReadMember(instance, name);

        private static int ReadOptionalInt(object? instance, string name)
        {
            try { object? value = ReadMemberOrNull(instance, name); return value == null ? -1 : Convert.ToInt32(value, CultureInfo.InvariantCulture); }
            catch { return -1; }
        }

        private static int CountObjects(object? value) => EnumerateObjects(value).Count();

        private static int IndexOfReference(IEnumerable values, object target)
        {
            int index = 0;
            foreach (object? item in values) { if (ReferenceEquals(item, target)) return index; index++; }
            return -1;
        }

        private static object? TryReadDictionaryValue(object? dictionary, string key)
        {
            if (dictionary is IDictionary map && map.Contains(key)) return map[key];
            MethodInfo? method = dictionary?.GetType().GetMethod("TryGetValue", BindingFlags.Public | BindingFlags.Instance);
            if (method == null) return null;
            object?[] args = { key, null };
            return method.Invoke(dictionary, args) is bool found && found ? args[1] : null;
        }

        private static string FormatInt(int value) => value < 0 ? "unknown" : value.ToString(CultureInfo.InvariantCulture);
    }
}
