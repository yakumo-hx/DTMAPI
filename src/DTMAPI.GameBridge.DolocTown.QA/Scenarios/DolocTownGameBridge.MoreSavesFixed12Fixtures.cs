using System;
using System.Globalization;
using System.Reflection;
using static DTMAPI.GameBridge.DolocTown.GameBridgeNativeHelpers;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed partial class QaScenarioController
    {
        private const string MoreSavesFixed12HookId =
            "Smoke.MoreSavesFixed12Lifecycle";
        private const string MoreSavesFixed12OwnerId = "DTMAPI.MoreSavesMod";

        private int g6MoreSavesFixed12Stage;
        private object? g6MoreSavesFixed12UiState;
        private DateTimeOffset g6MoreSavesFixed12HomeObservedAt;
        private bool g6MoreSavesFixed12SaveLoaded;
        private int? g6MoreSavesFixed12LoadedSlot;
        private bool g6MoreSavesFixed12LoadedIsNew;
        private bool g6MoreSavesFixed12SaveSaved;
        private int? g6MoreSavesFixed12SavedSlot;
        private DateTimeOffset g6MoreSavesFixed12SaveSavedAt;
        private DateTimeOffset g6MoreSavesFixed12DialogueObservedAt;
        private DateTimeOffset g6MoreSavesFixed12PostDialogueNormalObservedAt;
        private bool g6MoreSavesFixed12DialogueQuitRequested;
        private int g6MoreSavesFixed12DuplicateIndex = -1;

        private G4FixtureStepResult AdvanceMoreSavesFixed12ForFixture(
            string caseId)
        {
            try
            {
                switch (caseId)
                {
                    case "MoreSavesFixed12EnabledLifecycle":
                        return AdvanceMoreSavesFixed12EnabledLifecycle();
                    case "MoreSavesFixed12DisabledCold":
                        return AdvanceMoreSavesFixed12ColdObservation(
                            caseId,
                            expectedSlotCount: 6,
                            expectProductLoaded: false,
                            loadCreatedSlot: false);
                    case "MoreSavesFixed12ReenabledCold":
                        return AdvanceMoreSavesFixed12ColdObservation(
                            caseId,
                            expectedSlotCount: 12,
                            expectProductLoaded: true,
                            loadCreatedSlot: true);
                    default:
                        return FailMoreSavesFixed12(
                            caseId,
                            "unsupported-case");
                }
            }
            catch (Exception ex)
            {
                return FailMoreSavesFixed12(
                    caseId,
                    ex.GetType().Name + ":" + ex.Message);
            }
        }

        private G4FixtureStepResult AdvanceMoreSavesFixed12EnabledLifecycle()
        {
            const string caseId = "MoreSavesFixed12EnabledLifecycle";
            int targetIndex = GetMoreSavesFixed12TargetIndex();
            if (targetIndex < 6 || targetIndex >= 12)
                return FailMoreSavesFixed12(caseId, "target-index-outside-extra-slots");

            if (g6MoreSavesFixed12Stage == 0)
            {
                if (!TryObserveMoreSavesFixed12HomePage())
                    return PendingMoreSavesFixed12(caseId, "waiting-continuous-home");
                if (!TryValidateMoreSavesFixed12NativeShape(
                        expectedSlotCount: 12,
                        expectProductLoaded: true,
                        out string shape))
                {
                    return FailMoreSavesFixed12(caseId, shape);
                }
                if (!TryOpenMoreSavesFixed12OfficialUi(out string opened))
                    return PendingMoreSavesFixed12(caseId, opened);
                g6MoreSavesFixed12Stage = 1;
                return PendingMoreSavesFixed12(caseId, shape + "; " + opened);
            }

            if (g6MoreSavesFixed12Stage == 1)
            {
                if (!TryValidateMoreSavesFixed12Panel(12, out string panel))
                    return PendingMoreSavesFixed12(caseId, panel);
                for (int index = 0; index < 6; index++)
                {
                    if (ReadMoreSavesFixed12ArchiveInfo(index) == null)
                        return FailMoreSavesFixed12(caseId, "native-source-slot-empty:" + index);
                }
                for (int index = 6; index < 12; index++)
                {
                    if (ReadMoreSavesFixed12ArchiveInfo(index) != null)
                        return FailMoreSavesFixed12(caseId, "extra-slot-not-empty-before-create:" + index);
                }
                if (!SelectRequestedSaveForFixture(
                        g6MoreSavesFixed12UiState,
                        targetIndex,
                        out string selection))
                {
                    return FailMoreSavesFixed12(caseId, selection);
                }
                Type? stateType = ResolveType(
                    "DolocTown.GameDataUiState, Assembly-CSharp");
                MethodInfo? confirm = FindMethodInHierarchy(
                    stateType,
                    "OnConfirm",
                    0);
                if (confirm == null || g6MoreSavesFixed12UiState == null)
                    return FailMoreSavesFixed12(caseId, "official-new-game-continuation-missing");
                confirm.Invoke(g6MoreSavesFixed12UiState, null);
                g6MoreSavesFixed12Stage = 2;
                return PendingMoreSavesFixed12(
                    caseId,
                    panel + "; " + selection + "; official-new-game-issued=true");
            }

            if (g6MoreSavesFixed12Stage == 2)
            {
                if (!g6MoreSavesFixed12SaveLoaded)
                    return PendingMoreSavesFixed12(caseId, "waiting-new-game-save-loaded");
                int nativeArchiveIndex =
                    ReadMoreSavesFixed12ActiveArchiveIndex();
                if (!g6MoreSavesFixed12LoadedIsNew ||
                    nativeArchiveIndex != targetIndex ||
                    (g6MoreSavesFixed12LoadedSlot.HasValue &&
                     g6MoreSavesFixed12LoadedSlot.Value != targetIndex))
                {
                    return FailMoreSavesFixed12(
                        caseId,
                        "unexpected-new-game-receipt:slot=" +
                        (g6MoreSavesFixed12LoadedSlot?.ToString(
                            CultureInfo.InvariantCulture) ?? "none") +
                        ";isNew=" + g6MoreSavesFixed12LoadedIsNew +
                        ";nativeArchiveIndex=" + nativeArchiveIndex);
                }
                if (!RetireMoreSavesFixed12ExitedUi())
                    return FailMoreSavesFixed12(caseId, "new-game-ui-cache-cleanup-failed");
                if (!IsNormalGameplayForFixture())
                    return PendingMoreSavesFixed12(caseId, "waiting-normal-gameplay-before-save");
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                MethodInfo? save = FindMethodInHierarchy(dolocApi, "SaveGame", 1);
                if (save == null)
                    return FailMoreSavesFixed12(caseId, "DolocAPI.SaveGame-missing");
                g6MoreSavesFixed12Stage = 3;
                object? saved = save.Invoke(null, new object[] { targetIndex });
                if (!(saved is bool succeeded) || !succeeded)
                    return FailMoreSavesFixed12(caseId, "DolocAPI.SaveGame-returned-false");
                return PendingMoreSavesFixed12(caseId, "native-save-returned-true; waiting-SaveSaved");
            }

            if (g6MoreSavesFixed12Stage == 3)
            {
                if (!g6MoreSavesFixed12SaveSaved)
                    return PendingMoreSavesFixed12(caseId, "waiting-SaveSaved");
                if (g6MoreSavesFixed12SavedSlot != targetIndex ||
                    ReadMoreSavesFixed12ArchiveInfo(targetIndex) == null)
                {
                    return FailMoreSavesFixed12(caseId, "saved-slot-readback-failed");
                }
                if (!TryRetireMoreSavesFixed12FirstPlayDialogue(
                        out string dialogueRetirement))
                {
                    return PendingMoreSavesFixed12(caseId, dialogueRetirement);
                }
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                MethodInfo? returnHome = FindMethodInHierarchy(
                    dolocApi,
                    "ReturnHome",
                    1);
                if (returnHome == null)
                    return FailMoreSavesFixed12(caseId, "DolocAPI.ReturnHome-missing");
                g6MoreSavesFixed12Stage = 4;
                g6MoreSavesFixed12HomeObservedAt = default;
                returnHome.Invoke(null, new object[] { false });
                return PendingMoreSavesFixed12(
                    caseId,
                    "return-home-issued=true; " + dialogueRetirement);
            }

            if (g6MoreSavesFixed12Stage == 4)
            {
                if (!TryObserveMoreSavesFixed12HomePage())
                    return PendingMoreSavesFixed12(caseId, "waiting-home-after-native-save");
                if (!TryOpenMoreSavesFixed12OfficialUi(out string opened))
                    return PendingMoreSavesFixed12(caseId, opened);
                g6MoreSavesFixed12Stage = 5;
                return PendingMoreSavesFixed12(caseId, opened + "; post-save=true");
            }

            if (g6MoreSavesFixed12Stage == 5)
            {
                if (!TryValidateMoreSavesFixed12Panel(12, out string panel))
                    return PendingMoreSavesFixed12(caseId, panel);
                if (ReadMoreSavesFixed12ArchiveInfo(targetIndex) == null)
                    return FailMoreSavesFixed12(caseId, "created-slot-missing-before-copy");
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                MethodInfo? duplicate = FindMethodInHierarchy(
                    dolocApi,
                    "DuplicateGame",
                    2);
                MethodInfo? delete = FindMethodInHierarchy(
                    dolocApi,
                    "DeleteGame",
                    1);
                if (duplicate == null || delete == null)
                    return FailMoreSavesFixed12(caseId, "official-copy-delete-method-missing");
                object?[] duplicateArguments = { targetIndex, -1 };
                object? copied = duplicate.Invoke(null, duplicateArguments);
                g6MoreSavesFixed12DuplicateIndex = Convert.ToInt32(
                    duplicateArguments[1],
                    CultureInfo.InvariantCulture);
                if (!(copied is bool copiedOk) || !copiedOk ||
                    g6MoreSavesFixed12DuplicateIndex != targetIndex + 1 ||
                    ReadMoreSavesFixed12ArchiveInfo(
                        g6MoreSavesFixed12DuplicateIndex) == null)
                {
                    return FailMoreSavesFixed12(
                        caseId,
                        "official-copy-readback-failed:target=" +
                        g6MoreSavesFixed12DuplicateIndex);
                }
                delete.Invoke(
                    null,
                    new object[] { g6MoreSavesFixed12DuplicateIndex });
                if (ReadMoreSavesFixed12ArchiveInfo(targetIndex) == null ||
                    ReadMoreSavesFixed12ArchiveInfo(
                        g6MoreSavesFixed12DuplicateIndex) != null)
                {
                    return FailMoreSavesFixed12(caseId, "official-delete-readback-failed");
                }
                if (!CloseNativeUiOwnedByFixture(
                        ref g6MoreSavesFixed12UiState))
                {
                    return FailMoreSavesFixed12(caseId, "post-copy-delete-ui-close-failed");
                }
                g6MoreSavesFixed12Stage = 6;
                return PendingMoreSavesFixed12(
                    caseId,
                    panel + "; copiedTo=" + g6MoreSavesFixed12DuplicateIndex +
                    "; deleted=true; waiting-fresh-panel");
            }

            if (g6MoreSavesFixed12Stage == 6)
            {
                if (!TryOpenMoreSavesFixed12OfficialUi(out string opened))
                    return PendingMoreSavesFixed12(caseId, opened);
                g6MoreSavesFixed12Stage = 7;
                return PendingMoreSavesFixed12(caseId, opened + "; fresh-panel=true");
            }

            if (!TryValidateMoreSavesFixed12Panel(12, out string finalPanel))
                return PendingMoreSavesFixed12(caseId, finalPanel);
            if (!TryReadMoreSavesFixed12PanelSlotEmpty(
                    targetIndex,
                    expectedEmpty: false) ||
                !TryReadMoreSavesFixed12PanelSlotEmpty(
                    g6MoreSavesFixed12DuplicateIndex,
                    expectedEmpty: true))
            {
                return FailMoreSavesFixed12(caseId, "fresh-panel-create-delete-state-mismatch");
            }
            if (!CloseNativeUiOwnedByFixture(ref g6MoreSavesFixed12UiState))
                return FailMoreSavesFixed12(caseId, "final-panel-close-failed");
            return CompleteMoreSavesFixed12(
                caseId,
                "phase=EnabledLifecycle; nativeCount=12; create=true; save=true; " +
                "copyTarget=" + g6MoreSavesFixed12DuplicateIndex +
                "; delete=true; sourceRetained=true; freshPanel=true");
        }

        private G4FixtureStepResult AdvanceMoreSavesFixed12ColdObservation(
            string caseId,
            int expectedSlotCount,
            bool expectProductLoaded,
            bool loadCreatedSlot)
        {
            int targetIndex = GetMoreSavesFixed12TargetIndex();
            if (g6MoreSavesFixed12Stage == 0)
            {
                if (!TryObserveMoreSavesFixed12HomePage())
                    return PendingMoreSavesFixed12(caseId, "waiting-continuous-home");
                if (!TryValidateMoreSavesFixed12NativeShape(
                        expectedSlotCount,
                        expectProductLoaded,
                        out string shape))
                {
                    return FailMoreSavesFixed12(caseId, shape);
                }
                if (!TryOpenMoreSavesFixed12OfficialUi(out string opened))
                    return PendingMoreSavesFixed12(caseId, opened);
                g6MoreSavesFixed12Stage = 1;
                return PendingMoreSavesFixed12(caseId, shape + "; " + opened);
            }

            if (g6MoreSavesFixed12Stage == 1)
            {
                if (!TryValidateMoreSavesFixed12Panel(
                        expectedSlotCount,
                        out string panel))
                {
                    return PendingMoreSavesFixed12(caseId, panel);
                }
                if (!loadCreatedSlot)
                {
                    if (!CloseNativeUiOwnedByFixture(
                            ref g6MoreSavesFixed12UiState))
                    {
                        return FailMoreSavesFixed12(caseId, "disabled-panel-close-failed");
                    }
                    return CompleteMoreSavesFixed12(
                        caseId,
                        "phase=DisabledCold; productLoaded=false; nativeCount=6; " +
                        "panelSlots=6; archiveMutation=false");
                }

                if (targetIndex < 6 || targetIndex >= 12 ||
                    ReadMoreSavesFixed12ArchiveInfo(targetIndex) == null ||
                    ReadMoreSavesFixed12ArchiveInfo(targetIndex + 1) != null)
                {
                    return FailMoreSavesFixed12(caseId, "reenabled-archive-state-mismatch");
                }
                if (!TryReadMoreSavesFixed12PanelSlotEmpty(
                        targetIndex,
                        expectedEmpty: false) ||
                    !TryReadMoreSavesFixed12PanelSlotEmpty(
                        targetIndex + 1,
                        expectedEmpty: true))
                {
                    return FailMoreSavesFixed12(caseId, "reenabled-panel-state-mismatch");
                }
                if (!SelectRequestedSaveForFixture(
                        g6MoreSavesFixed12UiState,
                        targetIndex,
                        out string selection))
                {
                    return FailMoreSavesFixed12(caseId, selection);
                }
                Type? stateType = ResolveType(
                    "DolocTown.GameDataUiState, Assembly-CSharp");
                MethodInfo? confirm = FindMethodInHierarchy(
                    stateType,
                    "OnConfirm",
                    0);
                if (confirm == null || g6MoreSavesFixed12UiState == null)
                    return FailMoreSavesFixed12(caseId, "official-load-continuation-missing");
                confirm.Invoke(g6MoreSavesFixed12UiState, null);
                g6MoreSavesFixed12Stage = 2;
                return PendingMoreSavesFixed12(
                    caseId,
                    panel + "; " + selection + "; official-load-issued=true");
            }

            if (!g6MoreSavesFixed12SaveLoaded)
                return PendingMoreSavesFixed12(caseId, "waiting-reenabled-SaveLoaded");
            if (g6MoreSavesFixed12LoadedSlot != targetIndex ||
                g6MoreSavesFixed12LoadedIsNew)
            {
                return FailMoreSavesFixed12(
                    caseId,
                    "unexpected-reenabled-load-receipt");
            }
            if (!RetireMoreSavesFixed12ExitedUi())
                return FailMoreSavesFixed12(caseId, "reenabled-ui-cache-cleanup-failed");
            return CompleteMoreSavesFixed12(
                caseId,
                "phase=ReenabledCold; productLoaded=true; nativeCount=12; " +
                "createdSlotLoaded=true; isNew=false; archiveMutation=false");
        }

        private int GetMoreSavesFixed12TargetIndex() =>
            (g6FixtureOptions?.SaveSlot ?? 0) - 1;

        private bool TryRetireMoreSavesFixed12FirstPlayDialogue(
            out string details)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            Type? dialogueType = ResolveType(
                "DolocTown.DialogueState, Assembly-CSharp");
            object? userInput = ReadStaticMember(dolocApi, "userInput");
            object? currentState = ReadMemberOrNull(userInput, "CurrentState");
            string currentStateName = currentState?.GetType().FullName ?? "none";

            if (!g6MoreSavesFixed12DialogueQuitRequested &&
                dialogueType != null &&
                currentState != null &&
                dialogueType.IsInstanceOfType(currentState))
            {
                bool dialogueStable =
                    HasContinuousStableObservationForFixture(
                        true,
                        now,
                        0.5d,
                        ref g6MoreSavesFixed12DialogueObservedAt);
                if (!dialogueStable)
                {
                    details = "waiting-first-play-dialogue-stable:" +
                        currentStateName;
                    return false;
                }

                MethodInfo? quitDialogue = FindMethodInHierarchy(
                    dialogueType,
                    "QuitDialogue",
                    0);
                if (quitDialogue == null)
                    throw new MissingMethodException("DialogueState.QuitDialogue");
                quitDialogue.Invoke(currentState, null);
                g6MoreSavesFixed12DialogueQuitRequested = true;
                g6MoreSavesFixed12PostDialogueNormalObservedAt = default;
                details = "first-play-dialogue-quit-issued=true";
                return false;
            }

            if (!g6MoreSavesFixed12DialogueQuitRequested &&
                (now - g6MoreSavesFixed12SaveSavedAt).TotalSeconds < 1d)
            {
                details = "waiting-first-play-dialogue-or-stable-gameplay:" +
                    currentStateName;
                return false;
            }

            bool normalGameplay = IsNormalGameplayForFixture();
            if (!HasContinuousStableObservationForFixture(
                    normalGameplay,
                    now,
                    0.5d,
                    ref g6MoreSavesFixed12PostDialogueNormalObservedAt))
            {
                details = "waiting-normal-gameplay-after-dialogue:" +
                    currentStateName +
                    "; quitRequested=" +
                    g6MoreSavesFixed12DialogueQuitRequested;
                return false;
            }

            details = "firstPlayDialogue=" +
                (g6MoreSavesFixed12DialogueQuitRequested
                    ? "retired-by-native-QuitDialogue"
                    : "not-observed") +
                "; normalGameplayStable=true";
            return true;
        }

        private bool TryObserveMoreSavesFixed12HomePage()
        {
            bool home = runtime.UI.InputContext.Equals(
                "HomePageUiState",
                StringComparison.OrdinalIgnoreCase);
            return HasContinuousStableObservationForFixture(
                home,
                DateTimeOffset.UtcNow,
                0.5d,
                ref g6MoreSavesFixed12HomeObservedAt);
        }

        private bool TryValidateMoreSavesFixed12NativeShape(
            int expectedSlotCount,
            bool expectProductLoaded,
            out string details)
        {
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            object? manager = ReadStaticMember(dolocApi, "gameManager");
            int nativeCount = ReadOptionalInt(manager, "archiveFileCount");
            MethodInfo? getAll = FindMethodInHierarchy(
                dolocApi,
                "GetAllArchiveInfos",
                0);
            Array? infos = getAll?.Invoke(null, null) as Array;
            bool productLoaded = runtime.ModRegistry.IsLoaded(
                MoreSavesFixed12OwnerId);
            bool passed = nativeCount == expectedSlotCount &&
                infos?.Length == expectedSlotCount &&
                productLoaded == expectProductLoaded;
            details = "nativeCount=" + nativeCount +
                "; infoCount=" + (infos?.Length ?? -1) +
                "; productLoaded=" + productLoaded.ToString().ToLowerInvariant() +
                "; expectedCount=" + expectedSlotCount +
                "; expectedProductLoaded=" + expectProductLoaded.ToString().ToLowerInvariant();
            return passed;
        }

        private bool TryOpenMoreSavesFixed12OfficialUi(out string details)
        {
            if (g6MoreSavesFixed12UiState != null)
            {
                details = "official-ui=already-owned";
                return true;
            }
            if (!runtime.UI.InputContext.Equals(
                    "HomePageUiState",
                    StringComparison.OrdinalIgnoreCase))
            {
                details = "waiting-HomePage-before-official-ui:" +
                    runtime.UI.InputContext;
                return false;
            }
            Type? stateType = ResolveType(
                "DolocTown.GameDataUiState, Assembly-CSharp");
            if (stateType == null)
            {
                details = "GameDataUiState-missing";
                return false;
            }
            g6MoreSavesFixed12UiState = EnterNativeUiForFixture(
                stateType,
                null);
            details = g6MoreSavesFixed12UiState == null
                ? "official-ui-open-pending"
                : "official-ui=opened";
            return g6MoreSavesFixed12UiState != null;
        }

        private bool TryValidateMoreSavesFixed12Panel(
            int expectedSlotCount,
            out string details)
        {
            if (!HasExactNativeUiReceiptForFixture(
                    GetCurrentNativeUiStateForFixture(),
                    g6MoreSavesFixed12UiState))
            {
                details = "official-ui-ownership-pending";
                return false;
            }
            object? panel = ReadMemberOrNull(
                g6MoreSavesFixed12UiState,
                "panel");
            int panelCount = panel == null
                ? -1
                : Math.Max(
                    ReadOptionalInt(panel, "slotCount"),
                    CountObjects(ReadMemberOrNull(panel, "slots")));
            details = "panelSlots=" + panelCount +
                "; expectedPanelSlots=" + expectedSlotCount;
            return panelCount == expectedSlotCount;
        }

        private object? ReadMoreSavesFixed12ArchiveInfo(int index)
        {
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            MethodInfo? method = FindMethodInHierarchy(
                dolocApi,
                "GetArchiveInfo",
                1);
            if (method == null)
                throw new MissingMethodException("DolocAPI.GetArchiveInfo");
            return method.Invoke(null, new object[] { index });
        }

        private int ReadMoreSavesFixed12ActiveArchiveIndex()
        {
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            object? archiveHandle = ReadStaticMember(
                dolocApi,
                "archiveHandle");
            return ReadOptionalInt(archiveHandle, "archiveIndex");
        }

        private bool TryReadMoreSavesFixed12PanelSlotEmpty(
            int index,
            bool expectedEmpty)
        {
            object? panel = ReadMemberOrNull(
                g6MoreSavesFixed12UiState,
                "panel");
            MethodInfo? getSlot = FindMethodInHierarchy(
                panel?.GetType(),
                "GetSlot",
                1);
            object? slot = getSlot?.Invoke(panel, new object[] { index });
            object? grayed = ReadMemberOrNull(slot, "grayed");
            return grayed != null &&
                Convert.ToBoolean(grayed, CultureInfo.InvariantCulture) ==
                expectedEmpty;
        }

        private void NotifyMoreSavesFixed12SaveLoadedForFixture(
            int? slot,
            bool isNewGame)
        {
            if (!HasG6Case("MoreSavesFixed12EnabledLifecycle") &&
                !HasG6Case("MoreSavesFixed12ReenabledCold"))
            {
                return;
            }
            g6MoreSavesFixed12SaveLoaded = true;
            g6MoreSavesFixed12LoadedSlot = slot;
            g6MoreSavesFixed12LoadedIsNew = isNewGame;
        }

        private void NotifyMoreSavesFixed12SaveSavedForFixture(int? slot)
        {
            if (!HasG6Case("MoreSavesFixed12EnabledLifecycle"))
                return;
            g6MoreSavesFixed12SaveSaved = true;
            g6MoreSavesFixed12SavedSlot = slot;
            g6MoreSavesFixed12SaveSavedAt = DateTimeOffset.UtcNow;
        }

        private bool RetireMoreSavesFixed12ExitedUi()
        {
            if (g6MoreSavesFixed12UiState == null)
                return true;
            if (ReferenceEquals(
                    GetCurrentNativeUiStateForFixture(),
                    g6MoreSavesFixed12UiState))
            {
                return false;
            }
            MarkNativeUiStateExitedForFixture(g6MoreSavesFixed12UiState);
            return CloseNativeUiOwnedByFixture(
                ref g6MoreSavesFixed12UiState);
        }

        private G4FixtureStepResult PendingMoreSavesFixed12(
            string caseId,
            string details)
        {
            string receipt = "case=" + caseId + "; " + details;
            runtime.SetHookStatus(
                MoreSavesFixed12HookId,
                "pending",
                "optional QA disposable MoreSaves fixed-12 lifecycle",
                receipt);
            return G4FixtureStepResult.Pending(receipt);
        }

        private G4FixtureStepResult CompleteMoreSavesFixed12(
            string caseId,
            string details)
        {
            string receipt = "case=" + caseId + "; " + details +
                "; officialOwners=GameDataUiState|DolocAPI; productWrites=archiveFileCount-only";
            runtime.SetHookStatus(
                MoreSavesFixed12HookId,
                "verified",
                "optional QA disposable MoreSaves fixed-12 lifecycle",
                receipt);
            runtime.RuntimeMonitor.Log(
                "Smoke exercise MoreSavesFixed12Lifecycle OK " + receipt + ".");
            return G4FixtureStepResult.Verified(receipt);
        }

        private G4FixtureStepResult FailMoreSavesFixed12(
            string caseId,
            string details)
        {
            string receipt = "case=" + caseId + "; failure=" + details;
            runtime.SetHookStatus(
                MoreSavesFixed12HookId,
                "failed",
                "optional QA disposable MoreSaves fixed-12 lifecycle",
                receipt);
            return G4FixtureStepResult.Failed(receipt);
        }

        private void CleanupMoreSavesFixed12OnClose(string reason)
        {
            if (g6MoreSavesFixed12UiState == null)
                return;
            if (!ReferenceEquals(
                    GetCurrentNativeUiStateForFixture(),
                    g6MoreSavesFixed12UiState))
            {
                MarkNativeUiStateExitedForFixture(
                    g6MoreSavesFixed12UiState);
            }
            if (!CloseNativeUiOwnedByFixture(
                    ref g6MoreSavesFixed12UiState))
            {
                throw new InvalidOperationException(
                    "MoreSaves fixed-12 QA UI cleanup failed: " + reason);
            }
        }
    }
}
