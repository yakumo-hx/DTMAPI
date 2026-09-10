using System;
using System.Linq;
using DTMAPI.Abstractions;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed partial class QaScenarioController
    {
        internal bool IsG5WorldMutationReadyForFixture()
        {
            if (!IsNormalGameplayForFixture())
                return false;

            patcher ??= new HarmonyReflectionPatcher(runtime);
            Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
            object? archive = ReadStaticMember(dolocApi, "archiveHandle");
            object? currentRoom = archive == null
                ? null
                : ReadMember(archive, "currentRoom");
            object? roomInfo = currentRoom == null
                ? null
                : ReadMember(currentRoom, "RoomInfo");
            object? agent = ReadStaticMember(dolocApi, "agent");
            return archive != null &&
                currentRoom != null &&
                roomInfo != null &&
                agent != null;
        }

        internal G4FixtureStepResult AdvanceG5WorldMutationForFixture(string caseId)
        {
            caseId = caseId ?? string.Empty;
            string hookId;
            FixtureAttemptResult result;
            switch (caseId ?? string.Empty)
            {
                case "ActionSpeedTool":
                    hookId = "Smoke.ActionSpeedTool";
                    result = TryExerciseActionSpeedToolForFixture();
                    break;
                case "ActionSpeedConfigApply":
                    hookId = "Smoke.ActionSpeedConfigApply";
                    result = TryExerciseActionSpeedConfigApplyForFixture();
                    break;
                case "ActionSpeedInteraction":
                    hookId = "Smoke.ActionSpeedInteraction";
                    result = TryExerciseActionSpeedInteractionForFixture();
                    break;
                case "ActionSpeedGcLadder":
                    hookId = "Smoke.Batch5GcLadder.ActionSpeed";
                    result = TryExerciseActionSpeedGcLadderForFixture();
                    break;
                case "OneActionResourceHit":
                    hookId = "Smoke.OneActionResourceHit";
                    result = TryExerciseOneActionResourceHitForFixture();
                    break;
                case "OneActionWrongTool":
                    hookId = "Smoke.OneActionWrongTool";
                    result = TryExerciseOneActionWrongToolForFixture();
                    break;
                case "OneActionFuelFeed":
                    hookId = "Smoke.OneActionFuelFeed";
                    result = TryExerciseOneActionFuelFeedForFixture();
                    break;
                case "OneActionVegetation":
                    hookId = "Smoke.OneActionVegetation";
                    result = TryExerciseOneActionVegetationForFixture();
                    break;
                case "NewContent":
                    hookId = "Smoke.NewContentMineProduction";
                    result = TryExerciseNewContentApisForFixture(mineOnly: false, expectOilAbsentOverride: false, oilOnlyOverride: false);
                    break;
                case "OilOnly":
                    hookId = "Smoke.NewContentOilOnly";
                    result = TryExerciseNewContentApisForFixture(mineOnly: false, expectOilAbsentOverride: false, oilOnlyOverride: true);
                    break;
                case "MineContent":
                    hookId = "Smoke.NewContentMineProduction";
                    result = TryExerciseNewContentApisForFixture(mineOnly: true, expectOilAbsentOverride: false, oilOnlyOverride: false);
                    break;
                case "ChestLocatorEnhancer":
                    hookId = "Smoke.ChestLocatorEnhancer";
                    TryExerciseChestLocatorEnhancerForFixture();
                    return ReadG5VoidCaseTerminal(caseId, hookId);
                case "ZoomProductNative":
                    hookId = "Smoke.ZoomProductNative";
                    TryExerciseZoomProductNativeForFixture();
                    return ReadG5VoidCaseTerminal(caseId, hookId);
                case "MoreEquipmentSlots":
                    hookId = "Smoke.MoreEquipmentSlots";
                    result = TryExerciseMoreEquipmentSlotsForFixture();
                    break;
                case "MoreEquipmentSlotsCommittedShieldSetup":
                    hookId =
                        "Smoke.MoreEquipmentSlotsCommittedShieldSetup";
                    result =
                        TrySetupMoreEquipmentSlotsCommittedShieldForFixture();
                    break;
                case "MoreEquipmentSlotsCommittedShieldDamageNoNativeSave":
                    hookId =
                        "Smoke.MoreEquipmentSlotsCommittedShieldDamageNoNativeSave";
                    result =
                        TryDamageMoreEquipmentSlotsCommittedShieldNoNativeSaveForFixture();
                    break;
                case "MoreEquipmentSlotsCommittedShieldBreakReplaceNoNativeSave":
                    hookId =
                        "Smoke.MoreEquipmentSlotsCommittedShieldBreakReplaceNoNativeSave";
                    result =
                        TryBreakReplaceMoreEquipmentSlotsCommittedShieldNoNativeSaveForFixture();
                    break;
                case "MoreEquipmentSlotsNoNativeSave":
                    hookId = "Smoke.MoreEquipmentSlotsNoNativeSave";
                    result =
                        TryExerciseMoreEquipmentSlotsNoNativeSaveForFixture();
                    break;
                case "MoreEquipmentSlotsNoNativeSaveColdObserver":
                    hookId =
                        "Smoke.MoreEquipmentSlotsNoNativeSaveColdObserver";
                    result =
                        TryObserveMoreEquipmentSlotsNoNativeSaveColdForFixture();
                    break;
                case "MoreEquipmentSlotsTransition":
                    hookId =
                        "Smoke.MoreEquipmentSlotsTransition";
                    result =
                        TryExerciseMoreEquipmentSlotsTransitionForFixture();
                    break;
                case "StrongPlantingGun":
                    hookId = "Smoke.StrongPlantingGun";
                    result = TryExerciseStrongPlantingGunForFixture();
                    break;
                case "CropHarvestingApi":
                    hookId = "Smoke.CropHarvestingApi";
                    TryExerciseCropHarvestingApiForFixture();
                    return ReadG5VoidCaseTerminal(caseId, hookId);
                case "InstantSave":
                    hookId = "Smoke.InstantSave";
                    TryExerciseInstantSaveForFixture();
                    return ReadG5VoidCaseTerminal(caseId, hookId);
                case "DebugConsoleSaveAcceptance":
                    hookId =
                        "Smoke.DebugConsoleSaveAcceptance";
                    TryExerciseDebugConsoleSaveAcceptanceForFixture();
                    return ReadG5VoidCaseTerminal(caseId, hookId);
                case "DebugInventory":
                    hookId = "Smoke.DebugInventory";
                    TryExerciseDebugInventoryForFixture();
                    return ReadG5VoidCaseTerminal(caseId, hookId);
                case "DebugWeather":
                    hookId = "Smoke.DebugWeather";
                    TryExerciseDebugWeatherForFixture();
                    return ReadG5VoidCaseTerminal(caseId, hookId);
                case "DebugTeleport":
                    hookId = "Smoke.DebugTeleport";
                    if (debugTeleportRequestedAt == default)
                    {
                        result = TryExerciseDebugTeleportForFixture();
                        break;
                    }
                    if ((DateTimeOffset.Now - debugTeleportRequestedAt).TotalSeconds < 4)
                        return G4FixtureStepResult.Pending(G5Receipt(caseId, hookId, "pending-transport"));
                    CompleteDebugTeleportForFixture();
                    return ReadG5VoidCaseTerminal(caseId, hookId);
                case "DebugTime":
                    hookId = "Smoke.DebugTime";
                    TryExerciseDebugTimeForFixture();
                    return ReadG5VoidCaseTerminal(caseId, hookId);
                case "DebugMovement":
                    hookId = "Smoke.DebugMovement";
                    TryExerciseDebugMovementForFixture();
                    return ReadG5VoidCaseTerminal(caseId, hookId);
                case "AdvancedDebug":
                    hookId = "Smoke.AdvancedDebug";
                    TryExerciseAdvancedDebugForFixture();
                    return ReadG5VoidCaseTerminal(caseId, hookId);
                default:
                    throw new ArgumentOutOfRangeException(nameof(caseId), caseId, "The G5 fixture seam accepts only the reviewed world-mutation whitelist.");
            }

            if (result == FixtureAttemptResult.Pending)
                return G4FixtureStepResult.Pending(G5Receipt(caseId, hookId, "pending"));
            if (result == FixtureAttemptResult.Failed)
                return G4FixtureStepResult.Failed(G5Receipt(caseId, hookId, "failed"));
            return G4FixtureStepResult.Verified(G5Receipt(caseId, hookId, "verified"));
        }

        private G4FixtureStepResult ReadG5VoidCaseTerminal(string? caseId, string hookId)
        {
            IHookStatusInfo? status = runtime.Diagnostics.GetHookStatuses()
                .LastOrDefault(item => item.HookId.Equals(hookId, StringComparison.OrdinalIgnoreCase));
            if (status == null || status.Status.Equals("pending", StringComparison.OrdinalIgnoreCase) || status.Status.Equals("waiting", StringComparison.OrdinalIgnoreCase))
                return G4FixtureStepResult.Pending(G5Receipt(caseId, hookId, "pending"));
            if (!status.Status.Equals("verified", StringComparison.OrdinalIgnoreCase) && !status.Status.Equals("experimental", StringComparison.OrdinalIgnoreCase))
                return G4FixtureStepResult.Failed(G5Receipt(caseId, hookId, status.Status));
            return G4FixtureStepResult.Verified(G5Receipt(caseId, hookId, status.Status));
        }

        private static string G5Receipt(string? caseId, string hookId, string terminal)
        {
            return "case=" + (caseId ?? string.Empty) +
                "; setup=qa-explicit-whitelist+positive-save-slot" +
                "; commit=" + hookId + ":" + terminal +
                "; cleanup=case-local-finally+runner-post-exit-save-config-profile-transaction" +
                "; unknownDllOwnership=false; fallback=false";
        }
    }
}
