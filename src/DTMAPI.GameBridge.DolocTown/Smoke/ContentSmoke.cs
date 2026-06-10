using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading;
using DTMAPI.Abstractions;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    public sealed partial class DolocTownGameBridge
    {
        private string EnsureNewContentEvidenceDir()
        {
            if (!string.IsNullOrWhiteSpace(newContentEvidenceDir))
                return newContentEvidenceDir!;

            string timestamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture);
            newContentEvidenceDir = Path.Combine(runtime.Paths.EvidencePath, "NEWCONTENT-025", timestamp);
            Directory.CreateDirectory(newContentEvidenceDir);
            return newContentEvidenceDir;
        }

        private string CaptureMinePlacementEvidenceForSmoke(object mine, string createSummary)
        {
            try
            {
                string evidenceDir = EnsureNewContentEvidenceDir();
                string screenshotPath = Path.Combine(evidenceDir, "mine-placed-dtmapi-mine.png");
                bool screenshotRequested = TryCaptureScreenshot(screenshotPath);
                string equipmentSummary = DescribeEquipmentForSmoke(mine);
                string summary = "playerItem=dtmapi_mine ItemEquipment, placed=" + equipmentSummary + ", create={" + createSummary + "}, screenshot=" + (screenshotRequested ? screenshotPath : "unavailable");
                File.WriteAllText(Path.Combine(evidenceDir, "mine-placement-summary.txt"),
                    "Captured=" + DateTimeOffset.Now.ToString("o", System.Globalization.CultureInfo.InvariantCulture) + Environment.NewLine +
                    "ScreenshotRequested=" + screenshotRequested + Environment.NewLine +
                    "Screenshot=" + screenshotPath + Environment.NewLine +
                    "Create=" + createSummary + Environment.NewLine +
                    "Equipment=" + equipmentSummary + Environment.NewLine);
                runtime.RuntimeMonitor.Log("Mine placement evidence " + (screenshotRequested ? "OK" : "unavailable") + " " + summary + ".");
                runtime.SetHookStatus("Smoke.NewContentMinePlacement", screenshotRequested ? "verified" : "pending", "ItemEquipment dtmapi_mine + IEquipmentHost.CreateEquipment + UnityEngine.ScreenCapture", summary);
                return summary;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Mine placement evidence capture failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.NewContentMinePlacement", "failed", "ItemEquipment dtmapi_mine + IEquipmentHost.CreateEquipment", ex.GetType().Name + ": " + ex.Message);
                return "failed:" + ex.GetType().Name + ":" + ex.Message;
            }
        }

        private SmokeAttemptResult TryExerciseNewContentApisForSmoke(bool mineOnly)
        {
            object? transientMine = null;
            object? room = null;
            string newContentStage = mineOnly ? "MineOfficialJson" : "OilItemMetadata";
            try
            {
                if (experimentalApi == null)
                    throw new InvalidOperationException("Experimental GameBridge API was not registered.");

                patcher ??= new HarmonyReflectionPatcher(runtime);
                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                if (dolocApi == null)
                    throw new MissingMemberException("DolocAPI was not visible.");

                if (mineOnly && mineOfficialTechTreeUiOpenRequested && !mineOfficialTechTreeUiEvidenceCaptured)
                    return CaptureMineOfficialTechTreeUiEvidenceForSmoke(dolocApi);

                if (!TryGetStaticBoolProperty(dolocApi, "IsNormalState"))
                {
                    string recovery = mineOnly && mineOfficialTechTreeUiEvidenceCaptured
                        ? TryRecoverNormalStateAfterMineTechTreeUiForSmoke(dolocApi)
                        : "not-applicable";
                    runtime.SetHookStatus("Smoke.NewContentMineProduction", "pending", "NormalGameState", "Waiting for NormalGameState before creating a temporary dtmapi_mine. recovery={" + recovery + "}");
                    return SmokeAttemptResult.Pending;
                }

                room = ReadStaticMember(dolocApi, "CurrentRoom");
                if (room == null)
                    throw new InvalidOperationException("CurrentRoom was not available for new-content smoke.");

                if (!mineOnly && ReadBoolMember(room, "IsInHouse", false))
                {
                    if (!autoExerciseOneActionMainFarmRequested && TryEnterMainFarmForOneActionSmoke(dolocApi, room, out string transitionSummary))
                    {
                        runtime.SetHookStatus("Smoke.NewContentOilCoalDrop", "pending", "DolocAPI.EnterFarm", transitionSummary);
                        return SmokeAttemptResult.Pending;
                    }

                    runtime.SetHookStatus("Smoke.NewContentOilCoalDrop", "pending", "DolocAPI.EnterFarm", "Waiting for outdoor farm transition before OilMod coal-resource smoke. room=" + DescribeRoomForSmoke(room));
                    return SmokeAttemptResult.Pending;
                }

                string oilItemMetadataSummary = mineOnly ? "skipped-mine-only" : TryExerciseOilItemMetadataForSmoke(dolocApi);
                newContentStage = mineOnly ? "MineOfficialJson" : "OilCoalDrop";
                string oilCoalDropSummary = mineOnly ? "skipped-mine-only" : TryExerciseOilCoalDropForSmoke(dolocApi, room);
                newContentStage = "MineOfficialJson";
                string mineOfficialJsonSummary = TryExerciseMineOfficialJsonForSmoke(dolocApi);
                if (mineOnly && !mineOfficialTechTreeUiEvidenceCaptured)
                    return OpenMineOfficialTechTreeUiForSmoke(dolocApi, mineOfficialJsonSummary);

                newContentStage = mineOnly ? "MineProduction" : "EquipmentSlots";
                string equipmentSlotsSummary = mineOnly ? "skipped-mine-only" : TryExerciseEquipmentSlotsForSmoke();
                newContentStage = "MineProduction";

                transientMine = TryCreateTransientEquipmentForSmoke(dolocApi, room, "DolocTown.Equipment", new[] { "dtmapi_mine" }, out string createSummary);
                if (transientMine == null)
                    throw new InvalidOperationException("Could not create transient dtmapi_mine. " + createSummary);
                if (!ReadStringMember(transientMine, "Name", string.Empty).Equals("dtmapi_mine", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Transient equipment was not dtmapi_mine. target=" + DescribeEquipmentForSmoke(transientMine) + ", create={" + createSummary + "}");
                UpdateRuntimeAutomation(forceMachineProductionPoll: true);
                string scaleContainmentSummary = experimentalApi.ProbeMachineVisualScaleContainmentForSmoke("dtmapi_mine");
                if (ContainsIgnoreCase(scaleContainmentSummary, "contamination=True") ||
                    ContainsIgnoreCase(scaleContainmentSummary, "containment=failed"))
                {
                    throw new InvalidOperationException("Mine visual scale containment failed. " + scaleContainmentSummary);
                }

                int beforeCycles = experimentalApi.GetMachineProductionCycleCountForSmoke("DTMAPI.MineMod");
                TimeSkipResult timeSkip = experimentalApi.SkipToNextWeatherPeriod(CreateSmokeManifest());
                if (!timeSkip.Success)
                    throw new InvalidOperationException("Native pass-time smoke path failed before Mine catch-up. reason=" + timeSkip.FailureReason + ", message=" + timeSkip.Message);

                for (int attempt = 1; attempt <= 20; attempt++)
                {
                    UpdateRuntimeAutomation(forceMachineProductionPoll: true);
                    int afterCycles = experimentalApi.GetMachineProductionCycleCountForSmoke("DTMAPI.MineMod");
                    if (afterCycles > beforeCycles)
                    {
                        string state = experimentalApi.GetMachineProductionStateSummaryForSmoke("DTMAPI.MineMod");
                        if (!ContainsIgnoreCase(state, "outputTarget=equipment-storage") ||
                            !ContainsIgnoreCase(state, "storageLineCapacity=4"))
                        {
                            throw new InvalidOperationException("Machine runtime produced but did not place output into Mine-owned 16-slot storage. state={" + state + "}");
                        }

                        string minePlacementEvidenceSummary = CaptureMinePlacementEvidenceForSmoke(transientMine, createSummary);
                        string summary = "mode=" + (mineOnly ? "mine-only" : "all-new-content") + ", oilItemMetadata={" + oilItemMetadataSummary + "}, oilCoalDrop={" + oilCoalDropSummary + "}, mineOfficialJson={" + mineOfficialJsonSummary + "}, equipmentSlots={" + equipmentSlotsSummary + "}, timeSkip={" + timeSkip.Message + "}, minePlacement={" + minePlacementEvidenceSummary + "}, scaleContainment={" + scaleContainmentSummary + "}, mine={" + DescribeEquipmentForSmoke(transientMine) + "}, create={" + createSummary + "}, beforeCycles=" + beforeCycles + ", afterCycles=" + afterCycles + ", state={" + state + "}";
                        runtime.RuntimeMonitor.Log("Smoke exercise NewContentMineProduction OK " + summary);
                        runtime.SetHookStatus("Smoke.NewContentMineProduction", "verified", "ArchiveDataHandle.PassTimeNoControl -> IMachineProductionApi catch-up + equipment IContainer/LinearInventory", summary);
                        return SmokeAttemptResult.Succeeded;
                    }

                    Thread.Sleep(125);
                }

                throw new InvalidOperationException("Machine runtime loop did not catch up after native pass-time for transient dtmapi_mine. beforeCycles=" + beforeCycles + ", timeSkip={" + timeSkip.Message + "}, state={" + experimentalApi.GetMachineProductionStateSummaryForSmoke("DTMAPI.MineMod") + "}");
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke new-content mine production failed.", ex.InnerException.ToString());
                runtime.SetHookStatus("Smoke.NewContent" + newContentStage, "failed", "NewContent smoke", ex.InnerException.GetType().Name + ": " + ex.InnerException.Message);
                return SmokeAttemptResult.Failed;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke new-content mine production failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.NewContent" + newContentStage, "failed", "NewContent smoke", ex.GetType().Name + ": " + ex.Message);
                return SmokeAttemptResult.Failed;
            }
            finally
            {
                if (experimentalApi != null)
                {
                    experimentalApi.ForceOilDropForSmoke = false;
                    experimentalApi.ForceMachineProductionDueForSmoke = false;
                }
                if (room != null && transientMine != null)
                    TryRemoveTransientEquipmentForSmoke(room, transientMine);
            }
        }

        private SmokeAttemptResult OpenMineOfficialTechTreeUiForSmoke(Type dolocApi, string mineOfficialJsonSummary)
        {
            string treeId = ExtractMineTechTreeId(mineOfficialJsonSummary);
            string nodeId = "dtmapi_mine";
            MethodInfo? jumpTechTreeNode = FindMethod(dolocApi, "JumpTechTreeNode", 2);
            if (jumpTechTreeNode == null)
                throw new MissingMethodException("DolocAPI.JumpTechTreeNode(string,string) was not found.");

            mineOfficialTechTreeUiTreeId = treeId;
            mineOfficialTechTreeUiNodeId = nodeId;
            mineOfficialTechTreeUiOpenRequested = true;
            mineOfficialTechTreeUiOpenAt = DateTimeOffset.Now;
            runtime.SetHookStatus("Smoke.NewContentMineOfficialTechTreeUi", "pending", "DolocAPI.JumpTechTreeNode + TechTreeUiState", "Opening official tech tree node tree=" + treeId + ", node=" + nodeId + " for visual evidence.");
            runtime.RuntimeMonitor.Log("Mine official tech tree UI opening tree=" + treeId + " node=" + nodeId + " via DolocAPI.JumpTechTreeNode.");
            jumpTechTreeNode.Invoke(null, new object[] { treeId, nodeId });
            return SmokeAttemptResult.Pending;
        }

        private SmokeAttemptResult CaptureMineOfficialTechTreeUiEvidenceForSmoke(Type dolocApi)
        {
            if ((DateTimeOffset.Now - mineOfficialTechTreeUiOpenAt).TotalSeconds < 1.75)
            {
                runtime.SetHookStatus("Smoke.NewContentMineOfficialTechTreeUi", "pending", "DolocAPI.JumpTechTreeNode + TechTreeUiState", "Waiting for official tech tree panel render before screenshot. tree=" + mineOfficialTechTreeUiTreeId + ", node=" + mineOfficialTechTreeUiNodeId + ".");
                return SmokeAttemptResult.Pending;
            }

            try
            {
                string evidenceDir = EnsureNewContentEvidenceDir();
                string screenshotPath = Path.Combine(evidenceDir, "mine-official-tech-tree-dtmapi-mine.png");
                bool screenshotRequested = TryCaptureScreenshot(screenshotPath);
                string closeSummary = CloseMineOfficialTechTreeUiForSmoke(dolocApi);
                mineOfficialTechTreeUiEvidenceCaptured = true;
                mineOfficialTechTreeUiClosedAt = DateTimeOffset.Now;

                string summary = "tree=" + mineOfficialTechTreeUiTreeId +
                    ", node=" + mineOfficialTechTreeUiNodeId +
                    ", screenshot=" + (screenshotRequested ? screenshotPath : "unavailable") +
                    ", close={" + closeSummary + "}";
                File.AppendAllText(Path.Combine(evidenceDir, "mine-official-tech-tree-summary.txt"),
                    "Captured=" + DateTimeOffset.Now.ToString("o", CultureInfo.InvariantCulture) + Environment.NewLine +
                    "Tree=" + mineOfficialTechTreeUiTreeId + Environment.NewLine +
                    "Node=" + mineOfficialTechTreeUiNodeId + Environment.NewLine +
                    "ScreenshotRequested=" + screenshotRequested + Environment.NewLine +
                    "Screenshot=" + screenshotPath + Environment.NewLine +
                    "Close=" + closeSummary + Environment.NewLine);
                runtime.RuntimeMonitor.Log("Mine official tech tree UI evidence " + (screenshotRequested ? "OK" : "unavailable") + " " + summary + ".");
                runtime.SetHookStatus("Smoke.NewContentMineOfficialTechTreeUi", screenshotRequested ? "verified" : "pending", "DolocAPI.JumpTechTreeNode + TechTreeUiState + UnityEngine.ScreenCapture", summary);
                return SmokeAttemptResult.Pending;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Mine official tech tree UI evidence capture failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.NewContentMineOfficialTechTreeUi", "failed", "DolocAPI.JumpTechTreeNode + TechTreeUiState", ex.GetType().Name + ": " + ex.Message);
                return SmokeAttemptResult.Failed;
            }
        }

        private string CloseMineOfficialTechTreeUiForSmoke(Type dolocApi)
        {
            Type? techTreeUiState = patcher?.ResolveType("DolocTown.TechTreeUiState, Assembly-CSharp");
            if (techTreeUiState == null)
                return "skipped:missing-TechTreeUiState";

            object? userInput = ReadStaticMember(dolocApi, "userInput");
            object? currentState = userInput == null ? null : ReadMember(userInput, "CurrentState");
            if (currentState != null && techTreeUiState.IsAssignableFrom(currentState.GetType()))
            {
                MethodInfo? popState = FindMethod(userInput!.GetType(), "PopState", 0);
                if (popState != null && popState.Invoke(userInput, null) is bool popped && popped)
                    return "popped-current-TechTreeUiState";
            }

            MethodInfo? removeUiState = dolocApi.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .FirstOrDefault(method => method.Name.Equals("RemoveUiState", StringComparison.Ordinal) && method.IsGenericMethodDefinition && method.GetParameters().Length == 0);
            if (removeUiState == null)
                return "skipped:missing-DolocAPI.RemoveUiState";

            removeUiState.MakeGenericMethod(techTreeUiState).Invoke(null, null);
            return "removed-TechTreeUiState";
        }

        private string TryRecoverNormalStateAfterMineTechTreeUiForSmoke(Type dolocApi)
        {
            object? userInput = ReadStaticMember(dolocApi, "userInput");
            object? currentState = userInput == null ? null : ReadMember(userInput, "CurrentState");
            string currentStateName = currentState?.GetType().FullName ?? "null";
            double secondsSinceClose = mineOfficialTechTreeUiClosedAt == default ? -1 : (DateTimeOffset.Now - mineOfficialTechTreeUiClosedAt).TotalSeconds;
            Type? techTreeUiState = patcher?.ResolveType("DolocTown.TechTreeUiState, Assembly-CSharp");
            if (userInput != null && currentState != null && techTreeUiState != null && techTreeUiState.IsAssignableFrom(currentState.GetType()))
            {
                MethodInfo? popState = FindMethod(userInput.GetType(), "PopState", 0);
                if (popState != null && popState.Invoke(userInput, null) is bool popped)
                    return "retry-pop-TechTreeUiState popped=" + popped + ", secondsSinceClose=" + FormatRatio(secondsSinceClose);
            }

            if ((DateTimeOffset.Now - lastMineOfficialTechTreeUiRecoveryLogAt).TotalSeconds >= 5)
            {
                lastMineOfficialTechTreeUiRecoveryLogAt = DateTimeOffset.Now;
                runtime.RuntimeMonitor.Log("Mine official tech tree UI recovery waiting currentState=" + currentStateName + " secondsSinceClose=" + FormatRatio(secondsSinceClose) + ".");
            }

            return "currentState=" + currentStateName + ", secondsSinceClose=" + FormatRatio(secondsSinceClose);
        }

        private static string ExtractMineTechTreeId(string summary)
        {
            const string marker = "tree=";
            int start = summary.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (start < 0)
                return "industrial_techtree";

            start += marker.Length;
            int end = start;
            while (end < summary.Length && summary[end] != ',' && summary[end] != '}' && !char.IsWhiteSpace(summary[end]))
                end++;

            string tree = summary.Substring(start, end - start).Trim();
            return string.IsNullOrWhiteSpace(tree) ? "industrial_techtree" : tree;
        }

        private string TryExerciseEquipmentSlotsForSmoke()
        {
            if (experimentalApi == null)
                throw new InvalidOperationException("Experimental GameBridge API was not registered.");

            var owner = new ManifestModel
            {
                Name = "DTMAPI More Equipment Slots",
                Author = "DTMAPI",
                Version = DtmApiRuntime.ApiVersion,
                UniqueID = "DTMAPI.MoreEquipmentSlotsMod",
                Type = "CodeMod"
            };

            InventoryGiveResult passiveGive = experimentalApi.GiveItem(CreateSmokeManifest(), "grandmas_button", 1);
            if (!passiveGive.Success)
                throw new InvalidOperationException("Could not give grandmas_button for equipment-slot smoke. " + passiveGive.Message);

            IReadOnlyList<EquipmentSlotInfo> beforeSlots = experimentalApi.GetSlots(owner.UniqueID);
            EquipmentSlotEquipResult passiveEquip = experimentalApi.EquipExtraSlot(owner, string.Empty, "grandmas_button");
            if (!passiveEquip.Success)
                throw new InvalidOperationException("Could not equip DTMAPI extra slot with passive item. " + passiveEquip.Message);

            IReadOnlyList<EquipmentSlotInfo> equippedSlots = experimentalApi.GetSlots(owner.UniqueID);
            string passiveUiSummary = experimentalApi.CaptureEquipmentSlotsUiEvidenceForSmoke("new-content smoke after passive equip");
            EquipmentSlotEquipResult passiveRecover = experimentalApi.UnequipExtraSlot(owner, passiveEquip.SlotId, "new-content smoke passive recovery");
            if (!passiveRecover.Success)
                throw new InvalidOperationException("Could not recover DTMAPI passive extra slot. " + passiveRecover.Message);

            string nativeHatBefore = GetNativeAgentEquipmentItemId("hatItem");
            InventoryGiveResult hatGive = experimentalApi.GiveItem(CreateSmokeManifest(), "straw_hat", 1);
            if (!hatGive.Success)
                throw new InvalidOperationException("Could not give straw_hat for equipment-slot hat smoke. " + hatGive.Message);

            EquipmentSlotEquipResult hatEquip = experimentalApi.EquipExtraSlot(owner, string.Empty, "straw_hat");
            if (!hatEquip.Success)
                throw new InvalidOperationException("Could not equip DTMAPI extra slot with hat item. " + hatEquip.Message);
            string nativeHatAfterEquip = GetNativeAgentEquipmentItemId("hatItem");
            if (!string.Equals(nativeHatBefore, nativeHatAfterEquip, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("DTMAPI extra-slot hat changed the native hat visual slot. before=" + nativeHatBefore + ", after=" + nativeHatAfterEquip);

            IReadOnlyList<EquipmentSlotInfo> hatEquippedSlots = experimentalApi.GetSlots(owner.UniqueID);
            string hatUiSummary = experimentalApi.CaptureEquipmentSlotsUiEvidenceForSmoke("new-content smoke after hat equip");
            EquipmentSlotEquipResult hatRecover = experimentalApi.UnequipExtraSlot(owner, hatEquip.SlotId, "new-content smoke hat recovery");
            if (!hatRecover.Success)
                throw new InvalidOperationException("Could not recover DTMAPI hat extra slot. " + hatRecover.Message);
            string nativeHatAfterRecover = GetNativeAgentEquipmentItemId("hatItem");

            IReadOnlyList<EquipmentSlotInfo> recoveredSlots = experimentalApi.GetSlots(owner.UniqueID);
            string summary = "passiveGive=" + passiveGive.ItemId + " " + passiveGive.BeforeCount + "->" + passiveGive.AfterCount +
                ", beforeSlots=" + beforeSlots.Count +
                ", passiveSlot=" + passiveEquip.SlotId +
                ", passiveBackpack=" + passiveEquip.BeforeBackpackCount + "->" + passiveEquip.AfterBackpackCount +
                ", equippedStored=" + equippedSlots.Count(slot => slot.IsOccupied) +
                ", passiveUi={" + passiveUiSummary + "}" +
                ", passiveRecover=" + passiveRecover.RecoveredCount +
                ", passiveRecoverBackpack=" + passiveRecover.BeforeBackpackCount + "->" + passiveRecover.AfterBackpackCount +
                ", hatGive=" + hatGive.ItemId + " " + hatGive.BeforeCount + "->" + hatGive.AfterCount +
                ", hatSlot=" + hatEquip.SlotId +
                ", hatBackpack=" + hatEquip.BeforeBackpackCount + "->" + hatEquip.AfterBackpackCount +
                ", hatEquippedStored=" + hatEquippedSlots.Count(slot => slot.IsOccupied) +
                ", nativeHat=" + nativeHatBefore + "->" + nativeHatAfterEquip + "->" + nativeHatAfterRecover +
                ", hatUi={" + hatUiSummary + "}" +
                ", hatRecover=" + hatRecover.RecoveredCount +
                ", hatRecoverBackpack=" + hatRecover.BeforeBackpackCount + "->" + hatRecover.AfterBackpackCount +
                ", recoveredStored=" + recoveredSlots.Count(slot => slot.IsOccupied) +
                ", state={" + experimentalApi.GetEquipmentSlotsStateSummaryForSmoke(owner.UniqueID) + "}";
            runtime.RuntimeMonitor.Log("Smoke exercise NewContentEquipmentSlots OK " + summary);
            runtime.SetHookStatus("Smoke.NewContentEquipmentSlots", "verified", "IEquipmentSlotsApi passive+hat EquipExtraSlot/UnequipExtraSlot", summary);
            return summary;
        }

        private string GetNativeAgentEquipmentItemId(string memberName)
        {
            try
            {
                patcher ??= new HarmonyReflectionPatcher(runtime);
                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                object? archive = ReadStaticMember(dolocApi, "archiveHandle");
                object? farmData = archive == null ? null : ReadMember(archive, "farmData");
                object? agentData = farmData == null ? null : ReadMember(farmData, "agentData");
                object? equipment = agentData == null ? null : ReadMember(agentData, "agentEquipment");
                object? item = equipment == null ? null : ReadMember(equipment, memberName);
                return item == null ? "none" : ReadStringMember(item, "name", item.GetType().Name);
            }
            catch
            {
                return "unknown";
            }
        }

        private string TryExerciseMineOfficialJsonForSmoke(Type dolocApi)
        {
            if (experimentalApi == null)
                throw new InvalidOperationException("Experimental GameBridge API was not registered.");

            if (!TryGetNativeItemProto(dolocApi, "dtmapi_mine", out object? itemProto, out string itemProbe))
            {
                string reloadDetail;
                if (!TryReloadOfficialModsAndConfig("Smoke.NewContentMineOfficialJson", "missing dtmapi_mine before Mine metadata smoke", out reloadDetail))
                    throw new InvalidOperationException("dtmapi_mine is not present in the native item table before Mine metadata smoke, and official reload failed. probe={" + itemProbe + "}, reload={" + reloadDetail + "}");

                if (!TryGetNativeItemProto(dolocApi, "dtmapi_mine", out itemProto, out itemProbe))
                    throw new InvalidOperationException("dtmapi_mine is not present in the native item table after official reload. probe={" + itemProbe + "}, reload={" + reloadDetail + "}");
            }

            object? equipmentProto = QueryEquipmentProtoForSmoke(dolocApi, "dtmapi_mine");
            object? wellProto = QueryEquipmentProtoForSmoke(dolocApi, "well");
            if (!TryGetNativeRecipeProto(dolocApi, "dtmapi_mine", out object? recipeProto, out string recipeProbe))
                throw new InvalidOperationException("dtmapi_mine recipe is not present in the native recipe table. probe={" + recipeProbe + "}");
            object? generatedMineItem = GenerateItemForSmoke(dolocApi, "dtmapi_mine");
            string generatedMineItemType = generatedMineItem == null ? "null" : generatedMineItem.GetType().FullName ?? generatedMineItem.GetType().Name;
            bool generatedMineIsItemEquipment = generatedMineItem != null && IsTypeOrBase(generatedMineItem.GetType(), "DolocTown.ItemEquipment");

            object? recipeGroup = GetDolocConfigDataMapValueForSmoke("TbRecipeGroup", "equipment_workbench");
            IContentItemInfo? sourceInfo = runtime.GetIndexedContentItem("dtmapi_mine");
            UpdateRuntimeAutomation();
            MachineProductionState state = ((IMachineProductionApi)experimentalApi).GetState("DTMAPI.MineMod");
            object equipmentObject = equipmentProto ?? new object();
            object wellObject = wellProto ?? new object();
            object recipeObject = recipeProto ?? new object();

            string title = FirstNonEmpty(ReadAnyStringMember(itemProto!, string.Empty, "Title", "title"), "dtmapi_mine");
            string description = ReadAnyStringMember(itemProto!, string.Empty, "DescriptionBasic", "description_basic");
            string subType = ReadAnyStringMember(itemProto!, string.Empty, "SubType", "sub_type");
            string itemFunction = ReadAnyMember(itemProto!, "Function", "function")?.GetType().Name ?? "none";
            int overlay = ReadAnyIntMember(itemProto!, -1, "Overlay", "overlay");
            int buyingPrice = ReadAnyIntMember(itemProto!, -1, "BuyingPrice", "buying_price");
            bool viewable = ReadAnyBoolMember(itemProto!, false, "Viewable", "viewable");
            string indexedIcon = sourceInfo?.IconAssetKey ?? string.Empty;

            (int mineWidth, int mineHeight) = ReadVector2IntForSmoke(ReadAnyMember(equipmentObject, "CoverSize", "cover_size"));
            (int wellWidth, int wellHeight) = ReadVector2IntForSmoke(ReadAnyMember(wellObject, "CoverSize", "cover_size"));
            string sceneAsset = ReadAnyStringMember(ReadAnyMember(equipmentObject, "SceneAsset", "scene_asset") ?? new object(), string.Empty, "AssetUrl", "url");
            object? equipmentFunctionObject = ReadAnyMember(equipmentObject, "Function", "function");
            string equipmentFunction = equipmentFunctionObject?.GetType().Name ?? "none";
            int caseTotalCapacity = ReadAnyIntMember(equipmentFunctionObject ?? new object(), -1, "TotalCapacity", "total_capacity");
            int caseLineCapacity = ReadAnyIntMember(equipmentFunctionObject ?? new object(), -1, "LineCapacity", "line_capacity");
            object? electronicComponentObject = ReadAnyMember(equipmentObject, "ElectronicComponent", "electronic_component");
            string electronicComponent = electronicComponentObject?.GetType().Name ?? "none";
            int electronicThreshold = ReadAnyIntMember(electronicComponentObject ?? new object(), -1, "Threshold", "threshold");
            string outputItem = ReadAnyStringMember(ReadAnyMember(recipeObject, "OutputItem", "output_item") ?? new object(), string.Empty, "itemName", "ItemName", "item_name");
            int techPoint = ReadAnyIntMember(recipeObject, -1, "TechPoint", "tech_point");
            bool defaultUnlock = ReadAnyBoolMember(recipeObject, false, "DefaultUnlock", "default_unlock");
            bool groupIncludesRecipe = recipeGroup != null && ReadStringValues(ReadAnyMember(recipeGroup, "RecipeIds", "recipe_ids")).Any(v => v.Equals("dtmapi_mine", StringComparison.OrdinalIgnoreCase));
            string inputs = DescribeCountItemsForSmoke(ReadAnyMember(recipeObject, "InputItems", "input_items"));
            string outputRules = string.Join("|", experimentalApi.GetMachines("DTMAPI.MineMod")
                .SelectMany(machine => machine.OutputRules ?? Array.Empty<MachineOutputRule>())
                .Select(rule => rule.ItemId + ":" + rule.Weight.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture) + ":" + rule.Source));
            string nativeTechTreeSummary = state.NativeTechTreeSummary ?? string.Empty;

            var failures = new List<string>();
            if (equipmentProto == null)
                failures.Add("missing-equipment-proto");
            if (wellProto == null)
                failures.Add("missing-well-proto");
            if (sourceInfo == null || !sourceInfo.SourceKind.Equals("DTMAPI", StringComparison.OrdinalIgnoreCase))
                failures.Add("missing-dtmapi-source:" + (sourceInfo == null ? "none" : sourceInfo.SourceKind));
            if (!ContainsIgnoreCase(title, "矿井") && !ContainsIgnoreCase(title, "Mine"))
                failures.Add("missing-title:" + title);
            if (!ContainsIgnoreCase(description, "水井") && !ContainsIgnoreCase(description, "well"))
                failures.Add("missing-well-description");
            if (!subType.Equals("equipment_ornament", StringComparison.OrdinalIgnoreCase))
                failures.Add("unexpected-subtype:" + subType);
            if (!itemFunction.Equals("ItemFunctionEquipment", StringComparison.OrdinalIgnoreCase))
                failures.Add("unexpected-item-function:" + itemFunction);
            if (!generatedMineIsItemEquipment)
                failures.Add("generated-item-not-ItemEquipment:" + generatedMineItemType);
            if (overlay <= 0 || buyingPrice <= 0 || !viewable)
                failures.Add("item-not-viewable-buyable-stackable:overlay=" + overlay + ", buy=" + buyingPrice + ", viewable=" + viewable);
            if (!ContainsIgnoreCase(indexedIcon, "icon_item_well"))
                failures.Add("missing-indexed-icon:" + indexedIcon);
            if (mineWidth != 8 || mineHeight != 6)
                failures.Add("mine-cover-size=" + mineWidth + "x" + mineHeight);
            if (wellWidth <= 0 || wellHeight <= 0 || mineWidth != wellWidth * 2 || mineHeight != wellHeight * 2)
                failures.Add("not-double-well-cover:mine=" + mineWidth + "x" + mineHeight + ", well=" + wellWidth + "x" + wellHeight);
            if (!ContainsIgnoreCase(sceneAsset, "sprite_equipment_well"))
                failures.Add("scene-asset=" + sceneAsset);
            if (!equipmentFunction.Equals("EquipmentFuncCase", StringComparison.OrdinalIgnoreCase))
                failures.Add("unexpected-equipment-function:" + equipmentFunction);
            if (caseTotalCapacity != 16 || caseLineCapacity != 4)
                failures.Add("unexpected-case-storage:" + caseTotalCapacity + "/" + caseLineCapacity);
            if (!electronicComponent.Equals("EComProtoAppliance", StringComparison.OrdinalIgnoreCase) || electronicThreshold != 10)
                failures.Add("unexpected-electronic-component:" + electronicComponent + "/" + electronicThreshold);
            if (!outputItem.Equals("dtmapi_mine", StringComparison.OrdinalIgnoreCase))
                failures.Add("recipe-output=" + outputItem);
            bool hasOilRecipe = CountItemsContain(ReadAnyMember(recipeObject, "InputItems", "input_items"), "crude_oil", 10) &&
                CountItemsContain(ReadAnyMember(recipeObject, "InputItems", "input_items"), "metal_framework", 10) &&
                CountItemsContain(ReadAnyMember(recipeObject, "InputItems", "input_items"), "engine_core", 5) &&
                CountItemsContain(ReadAnyMember(recipeObject, "InputItems", "input_items"), "steel_ingot", 20);
            bool hasFallbackRecipe = CountItemsContain(ReadAnyMember(recipeObject, "InputItems", "input_items"), "metal_framework", 15) &&
                CountItemsContain(ReadAnyMember(recipeObject, "InputItems", "input_items"), "engine_core", 10) &&
                CountItemsContain(ReadAnyMember(recipeObject, "InputItems", "input_items"), "steel_ingot", 20) &&
                CountItemsContain(ReadAnyMember(recipeObject, "InputItems", "input_items"), "coal", 100);
            if (!hasOilRecipe && !hasFallbackRecipe)
                failures.Add("recipe-inputs=" + inputs);
            if (techPoint != 1 || defaultUnlock)
                failures.Add("recipe-tech-default:tech=" + techPoint + ", defaultUnlock=" + defaultUnlock);
            if (!groupIncludesRecipe)
                failures.Add("equipment_workbench-missing-dtmapi_mine");
            if (!ContainsIgnoreCase(nativeTechTreeSummary, "node=dtmapi_mine") ||
                !ContainsIgnoreCase(nativeTechTreeSummary, "parent=alloy_material") ||
                !ContainsIgnoreCase(nativeTechTreeSummary, "rightOfParent=True") ||
                !ContainsIgnoreCase(nativeTechTreeSummary, "aboveCommander=True") ||
                !ContainsIgnoreCase(nativeTechTreeSummary, "unlockEntries=recipe-only") ||
                !ContainsIgnoreCase(nativeTechTreeSummary, "equipmentEntries=0") ||
                !ContainsIgnoreCase(nativeTechTreeSummary, "recipeEntries=1") ||
                !ContainsIgnoreCase(nativeTechTreeSummary, "costValues=") ||
                !ContainsIgnoreCase(nativeTechTreeSummary, ":1") ||
                ContainsIgnoreCase(nativeTechTreeSummary, "pending") ||
                ContainsIgnoreCase(nativeTechTreeSummary, "failed"))
            {
                failures.Add("native-tech-route=" + nativeTechTreeSummary);
            }
            if (!state.MachineId.Equals("dtmapi.mine", StringComparison.OrdinalIgnoreCase) ||
                !state.EquipmentId.Equals("dtmapi_mine", StringComparison.OrdinalIgnoreCase) ||
                !state.RecipeGroupId.Equals("equipment_workbench", StringComparison.OrdinalIgnoreCase) ||
                state.VisualScale < 1.99 ||
                !state.AllowFuelMode ||
                !state.AllowElectricMode ||
                !state.DefaultMode.Equals("electric", StringComparison.OrdinalIgnoreCase) ||
                state.FuelCapacity <= 0 ||
                state.FuelOnlyFuelCostPerCycle <= state.ElectricModeFuelCostPerCycle ||
                state.ElectricModeFuelCostPerCycle <= 0 ||
                state.ElectricModePowerCostPerCycle != 10 ||
                (ContainsIgnoreCase(outputRules, "DTMAPI.OilMod") && !ContainsIgnoreCase(outputRules, "crude_oil")))
                failures.Add("machine-api-state=machine:" + state.MachineId + ", equipment:" + state.EquipmentId + ", group:" + state.RecipeGroupId + ", visualScale:" + state.VisualScale.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + ", hybrid=" + (state.AllowFuelMode && state.AllowElectricMode) + ", defaultMode=" + state.DefaultMode + ", fuelCapacity=" + state.FuelCapacity + ", fuelOnlyCost=" + state.FuelOnlyFuelCostPerCycle + ", electricFuelCost=" + state.ElectricModeFuelCostPerCycle + ", powerCost=" + state.ElectricModePowerCostPerCycle + ", outputs=" + outputRules);

            string summary = "item=" + title +
                ", source=" + (sourceInfo == null ? "none" : sourceInfo.SourceKind + "/" + sourceInfo.SourceId + "/" + sourceInfo.SourceModTitle) +
                ", icon=" + indexedIcon +
                ", itemFunction=" + itemFunction +
                ", subtype=" + subType +
                ", cover=" + mineWidth + "x" + mineHeight +
                ", baseWellCover=" + wellWidth + "x" + wellHeight +
                ", sceneAsset=" + sceneAsset +
                ", equipmentFunction=" + equipmentFunction +
                ", caseStorage=" + caseTotalCapacity + "/" + caseLineCapacity +
                ", electronicComponent=" + electronicComponent + "/" + electronicThreshold +
                ", generatedItemType=" + generatedMineItemType +
                ", recipeOutput=" + outputItem +
                ", recipeInputs=" + inputs +
                ", techPoint=" + techPoint +
                ", defaultUnlock=" + defaultUnlock +
                ", recipeGroup=equipment_workbench includes=" + groupIncludesRecipe +
                ", nativeTech={" + nativeTechTreeSummary + "}" +
                ", machineApi=machine:" + state.MachineId + "/item:" + state.ItemId + "/equipment:" + state.EquipmentId + "/recipe:" + state.RecipeId + "/group:" + state.RecipeGroupId + "/visualScale:" + state.VisualScale.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + "/hybrid:" + (state.AllowFuelMode && state.AllowElectricMode) + "/defaultMode:" + state.DefaultMode + "/fuelCapacity:" + state.FuelCapacity + "/fuelOnlyCost:" + state.FuelOnlyFuelCostPerCycle + "/electricFuelCost:" + state.ElectricModeFuelCostPerCycle + "/cycleMinutes:" + state.CycleMinutes + "/powerCost:" + state.ElectricModePowerCostPerCycle +
                ", outputRules=" + outputRules +
                ", probes=item{" + itemProbe + "}, recipe{" + recipeProbe + "}";
            if (failures.Count > 0)
                throw new InvalidOperationException("Mine official JSON/API smoke failed: " + string.Join(", ", failures) + ". " + summary);

            runtime.RuntimeMonitor.Log("Smoke exercise NewContentMineOfficialJson OK " + summary);
            runtime.SetHookStatus("Smoke.NewContentMineOfficialJson", "verified", "DolocConfig item/equipment/recipe/group + IMachineProductionApi state", summary);
            return summary;
        }

        private string TryExerciseOilItemMetadataForSmoke(Type dolocApi)
        {
            if (experimentalApi == null)
                throw new InvalidOperationException("Experimental GameBridge API was not registered.");

            if (!TryGetNativeItemProto(dolocApi, "crude_oil", out object? proto, out string nativeProbe))
            {
                string reloadDetail;
                if (!TryReloadOfficialModsAndConfig("Smoke.NewContentOilItemMetadata", "missing crude_oil before Oil metadata smoke", out reloadDetail))
                    throw new InvalidOperationException("crude_oil is not present in the native item table before Oil metadata smoke, and official reload failed. probe={" + nativeProbe + "}, reload={" + reloadDetail + "}");

                if (!TryGetNativeItemProto(dolocApi, "crude_oil", out proto, out nativeProbe))
                    throw new InvalidOperationException("crude_oil is not present in the native item table after official reload. probe={" + nativeProbe + "}, reload={" + reloadDetail + "}");
            }

            if (proto == null)
                throw new InvalidOperationException("crude_oil native item query returned no proto. probe={" + nativeProbe + "}");

            InventoryDebugPage page = experimentalApi.GetItems(new InventoryDebugQuery
            {
                SearchText = "crude_oil",
                IncludeUnavailable = true,
                PageSize = 50
            });
            InventoryDebugItem? item = page.Items.FirstOrDefault(i => i.Id.Equals("crude_oil", StringComparison.OrdinalIgnoreCase));
            if (item == null)
                throw new InvalidOperationException("IInventoryDebugApi did not return crude_oil. status=" + page.Status + ", total=" + page.TotalItems + ".");

            InventoryDebugPage sourcePage = experimentalApi.GetItems(new InventoryDebugQuery
            {
                SourceId = item.SourceId,
                IncludeUnavailable = true,
                PageSize = 50
            });
            IContentItemInfo? sourceInfo = runtime.GetIndexedContentItem("crude_oil");
            bool sourceFilterIncludesOil = sourcePage.Items.Any(i => i.Id.Equals("crude_oil", StringComparison.OrdinalIgnoreCase));
            InventoryDebugSourceGroup? sourceGroup = page.Sources.FirstOrDefault(g => g.Id.Equals(item.SourceId, StringComparison.OrdinalIgnoreCase));
            string effectiveCategory = FirstNonEmpty(item.SubCategory, item.Category);
            bool categoryListed = !string.IsNullOrWhiteSpace(effectiveCategory) &&
                page.Categories.Any(c => c.Equals(effectiveCategory, StringComparison.OrdinalIgnoreCase));
            string[] tags = item.Tags?.ToArray() ?? Array.Empty<string>();
            string[] nativeSources = ReadStringValues(ReadAnyMember(proto, "Source", "source")).ToArray();

            string title = FirstNonEmpty(ReadAnyStringMember(proto, string.Empty, "Title", "title"), item.DisplayName, item.ChineseName, item.Id);
            string description = ReadAnyStringMember(proto, string.Empty, "DescriptionBasic", "description_basic");
            bool salable = ReadAnyBoolMember(proto, false, "Salable", "salable");
            bool viewable = ReadAnyBoolMember(proto, false, "Viewable", "viewable");
            int sellingPrice = ReadAnyIntMember(proto, -1, "SellingPrice", "selling_price");
            int buyingPrice = ReadAnyIntMember(proto, -1, "BuyingPrice", "buying_price");
            int electricEnergy = ReadAnyIntMember(proto, -1, "ElectricEnergy", "electric_energy");
            int overlay = ReadAnyIntMember(proto, -1, "Overlay", "overlay");
            string nativeIcon = ReadAnyMember(proto, "UiSpriteAsset", "ui_sprite_asset")?.ToString() ?? string.Empty;
            string indexedIcon = sourceInfo?.IconAssetKey ?? string.Empty;
            string nativeSubType = ReadAnyStringMember(proto, string.Empty, "SubType", "sub_type");
            string functionType = ReadAnyMember(proto, "Function", "function")?.GetType().Name ?? "none";
            int highestBaseFuel = FindHighestNativeFuelEnergyExcept("crude_oil", out string highestBaseFuelItem);

            var failures = new List<string>();
            if (!item.RuntimeLoaded)
                failures.Add("not-runtime-loaded");
            if (!item.CanSpawn || !item.CanGive)
                failures.Add("not-giveable:" + item.CannotGiveReason);
            if (!item.IsModItem)
                failures.Add("not-marked-mod-item");
            if (!item.SourceKind.Equals("DTMAPI", StringComparison.OrdinalIgnoreCase))
                failures.Add("source-kind=" + item.SourceKind);
            if (!item.SourceId.Equals("Local.DTMAPI_Oil", StringComparison.OrdinalIgnoreCase))
                failures.Add("source-id=" + item.SourceId);
            if (sourceGroup == null || sourceGroup.Count <= 0)
                failures.Add("missing-source-group");
            if (!sourceFilterIncludesOil)
                failures.Add("source-filter-misses-oil");
            if (string.IsNullOrWhiteSpace(effectiveCategory))
                failures.Add("missing-category");
            if (!categoryListed)
                failures.Add("category-not-listed:" + effectiveCategory);
            if (!ContainsAny(tags, "material_ore", "mining", "processing") &&
                !ContainsAny(nativeSources, "material_ore", "mining", "processing") &&
                !ContainsIgnoreCase(item.SearchText, "material_ore"))
                failures.Add("missing-mining-category-tags");
            if (!item.HasIcon || string.IsNullOrWhiteSpace(nativeIcon) || !ContainsIgnoreCase(indexedIcon, "icon_item_coal"))
                failures.Add("missing-icon:hasIcon=" + item.HasIcon + ", item=" + item.IconAssetKey + ", native=" + nativeIcon + ", indexed=" + indexedIcon);
            if (!ContainsIgnoreCase(title, "原油") && !ContainsIgnoreCase(title, "石油") && !ContainsIgnoreCase(title, "Oil") && !ContainsIgnoreCase(item.DisplayName, "原油") && !ContainsIgnoreCase(item.DisplayName, "Oil"))
                failures.Add("missing-localized-title:" + title + "/" + item.DisplayName);
            if (!ContainsIgnoreCase(description, "燃料") && !ContainsIgnoreCase(description, "fuel"))
                failures.Add("missing-fuel-description");
            if (!salable || sellingPrice <= 0)
                failures.Add("not-salable:sale=" + salable + ", price=" + sellingPrice);
            if (buyingPrice <= 0)
                failures.Add("missing-buy-price:" + buyingPrice);
            if (!viewable)
                failures.Add("not-viewable");
            if (overlay <= 0)
                failures.Add("not-stackable-overlay:" + overlay);
            if (electricEnergy <= highestBaseFuel)
                failures.Add("fuel-not-above-base-highest:" + electricEnergy + "<=" + highestBaseFuel + "(" + highestBaseFuelItem + ")");

            string summary = "id=" + item.Id +
                ", display=" + FirstNonEmpty(item.DisplayName, title, item.Id) +
                ", sourceKind=" + item.SourceKind +
                ", sourceId=" + item.SourceId +
                ", sourceTitle=" + item.SourceModTitle +
                ", sourceGroup=" + (sourceGroup == null ? "missing" : sourceGroup.DisplayName + "/" + sourceGroup.Count) +
                ", sourceFilterIncludesOil=" + sourceFilterIncludesOil +
                ", category=" + effectiveCategory +
                ", categoryListed=" + categoryListed +
                ", tags=" + (tags.Length == 0 ? "none" : string.Join("|", tags)) +
                ", nativeSources=" + (nativeSources.Length == 0 ? "none" : string.Join("|", nativeSources)) +
                ", salable=" + salable +
                ", sellingPrice=" + sellingPrice +
                ", buyingPrice=" + buyingPrice +
                ", fuelEnergy=" + electricEnergy +
                ", baseHighestFuel=" + highestBaseFuelItem + ":" + highestBaseFuel +
                ", icon=" + FirstNonEmpty(item.IconAssetKey, nativeIcon) +
                ", indexedIcon=" + indexedIcon +
                ", title=" + title +
                ", subType=" + nativeSubType +
                ", function=" + functionType +
                ", nativeProbe={" + nativeProbe + "}";
            if (failures.Count > 0)
                throw new InvalidOperationException("Oil item metadata smoke failed: " + string.Join(", ", failures) + ". " + summary);

            runtime.RuntimeMonitor.Log("Smoke exercise NewContentOilItemMetadata OK " + summary);
            runtime.SetHookStatus("Smoke.NewContentOilItemMetadata", "verified", "IInventoryDebugApi.GetItems + DolocAPI.QueryItemProto", summary);
            return summary;
        }

        private string TryExerciseOilCoalDropForSmoke(Type dolocApi, object room)
        {
            if (experimentalApi == null)
                throw new InvalidOperationException("Experimental GameBridge API was not registered.");

            if (!TryQueryNativeItemProto(dolocApi, "crude_oil", out string oilProbe))
            {
                string reloadDetail;
                if (!TryReloadOfficialModsAndConfig("Smoke.NewContentOilCoalDrop", "missing crude_oil before OilMod smoke", out reloadDetail))
                    throw new InvalidOperationException("crude_oil is not present in the native item table before OilMod smoke, and official reload failed. probe={" + oilProbe + "}, reload={" + reloadDetail + "}");

                if (!TryQueryNativeItemProto(dolocApi, "crude_oil", out oilProbe))
                    throw new InvalidOperationException("crude_oil is not present in the native item table after official reload. probe={" + oilProbe + "}, reload={" + reloadDetail + "}");
            }

            Type? toolColliderType = patcher?.ResolveType("DolocTown.ToolCollider, Assembly-CSharp");
            Type? resourceRendererType = patcher?.ResolveType("DolocTown.DungeonResourceRenderer, Assembly-CSharp");
            if (toolColliderType == null || resourceRendererType == null)
                throw new MissingMemberException("ToolCollider or DungeonResourceRenderer was not visible.");

            if (!TryEnsureCoalResourceForOilSmoke(room, resourceRendererType, out string setupSummary))
                throw new InvalidOperationException("Could not prepare a coal resource for OilMod smoke. " + setupSummary);

            MethodInfo? handleTools = FindMethod(toolColliderType, "HandleTools", 1);
            MethodInfo? resetTool = FindMethod(toolColliderType, "ResetTool", 1);
            MethodInfo? resetChopCounter = FindMethod(toolColliderType, "ResetChopCounter", 1);
            if (handleTools == null || resetTool == null)
                throw new MissingMethodException("ToolCollider.HandleTools/ResetTool path was not found for OilMod smoke.");

            object? toolCollider = FindToolColliderForSmoke(dolocApi, toolColliderType);
            if (toolCollider == null)
                throw new InvalidOperationException("No ToolCollider instance was available for OilMod smoke.");

            object? tool = GenerateItemForSmoke(dolocApi, "steel_pickaxe") ?? GenerateItemForSmoke(dolocApi, "iron_pickaxe") ?? GenerateItemForSmoke(dolocApi, "old_pickaxe");
            if (tool == null)
                throw new InvalidOperationException("Could not generate a pickaxe for OilMod smoke.");

            string toolName = ReadStringMember(tool, "name", tool.GetType().Name);
            int toolDamage = ReadIntMember(tool, "ChopNumber", 0);
            int beforeDrops = experimentalApi.OilMiningDropCount;
            int rendererCount = 0;
            int coalCount = 0;
            int invokedCount = 0;
            List<string> samples = new List<string>();
            List<string> attempts = new List<string>();

            try
            {
                experimentalApi.ForceOilDropForSmoke = true;
                foreach (object renderer in FindUnityObjects(resourceRendererType))
                {
                    rendererCount++;
                    object? resource = ReadMember(renderer, "DungeonResource");
                    if (resource == null || IsRemoved(resource))
                        continue;

                    string resourceName = ReadStringMember(resource, "ResourceName", resource.GetType().Name);
                    string resourceClass = ReadResourceClass(resource);
                    int healthBefore = ReadIntMember(resource, "currentHealth", 0);
                    if (samples.Count < 8)
                        samples.Add(resourceName + "/class=" + resourceClass + "/health=" + healthBefore);
                    if (!IsCoalResourceNameForSmoke(resourceName) || healthBefore <= 0)
                        continue;

                    coalCount++;
                    object? collider = ReadMember(renderer, "PolygonCollider");
                    if (collider == null)
                        continue;

                    int seededHealth = Math.Max(1, toolDamage > 0 ? Math.Min(healthBefore, toolDamage) : 1);
                    WriteIntMember(resource, "currentHealth", seededHealth);
                    resetTool.Invoke(toolCollider, new object[] { tool });
                    resetChopCounter?.Invoke(toolCollider, new object[] { 999 });
                    handleTools.Invoke(toolCollider, new object[] { collider });
                    invokedCount++;

                    int healthAfter = ReadIntMember(resource, "currentHealth", 0);
                    bool removedAfter = IsRemoved(resource);
                    if (attempts.Count < 8)
                        attempts.Add(resourceName + "/tool=" + toolName + "/toolDamage=" + toolDamage + "/health=" + healthBefore + "->" + healthAfter + "/seeded=" + seededHealth + "/removed=" + removedAfter + "/drops=" + beforeDrops + "->" + experimentalApi.OilMiningDropCount + "/bridge={" + experimentalApi.LastOilMiningDropSummary + "}");
                    if (experimentalApi.OilMiningDropCount > beforeDrops)
                    {
                        string summary = "setup={" + setupSummary + "}, resource=" + resourceName + ", class=" + resourceClass + ", tool=" + toolName + ", toolDamage=" + toolDamage + ", healthBefore=" + healthBefore + ", seededHealth=" + seededHealth + ", healthAfter=" + healthAfter + ", removed=" + removedAfter + ", beforeDrops=" + beforeDrops + ", afterDrops=" + experimentalApi.OilMiningDropCount + ", bridge={" + experimentalApi.LastOilMiningDropSummary + "}";
                        runtime.RuntimeMonitor.Log("Smoke exercise NewContentOilCoalDrop OK " + summary);
                        runtime.SetHookStatus("Smoke.NewContentOilCoalDrop", "verified", "ToolCollider.HandleTools private path + OilMod.MiningDrop", summary);
                        return summary;
                    }
                }
            }
            finally
            {
                experimentalApi.ForceOilDropForSmoke = false;
            }

            throw new InvalidOperationException("No coal resource produced crude_oil during OilMod smoke. renderers=" + rendererCount + ", coalResources=" + coalCount + ", invoked=" + invokedCount + ", beforeDrops=" + beforeDrops + ", afterDrops=" + experimentalApi.OilMiningDropCount + ", setup={" + setupSummary + "}, attempts=" + (attempts.Count == 0 ? "none" : string.Join(" ; ", attempts)) + ", samples=" + string.Join(" ; ", samples));
        }

        private static bool TryQueryNativeItemProto(Type dolocApi, string itemId, out string detail)
        {
            return TryGetNativeItemProto(dolocApi, itemId, out _, out detail);
        }

        private static bool TryGetNativeRecipeProto(Type dolocApi, string recipeId, out object? proto, out string detail)
        {
            detail = string.Empty;
            proto = null;
            try
            {
                MethodInfo? queryRecipeProto = dolocApi.GetMethod("QueryRecipeProto", BindingFlags.Public | BindingFlags.Static);
                if (queryRecipeProto == null)
                {
                    detail = "DolocAPI.QueryRecipeProto was not found.";
                    return false;
                }

                object?[] args = new object?[] { recipeId, null };
                bool found = queryRecipeProto.Invoke(null, args) is bool ok && ok && args[1] != null;
                proto = found ? args[1] : null;
                detail = found ? "found " + recipeId + " in DolocConfig.Tables.TbRecipe." : "missing " + recipeId + " in DolocConfig.Tables.TbRecipe.";
                return found;
            }
            catch (Exception ex)
            {
                Exception root = ex is TargetInvocationException && ex.InnerException != null ? ex.InnerException : ex;
                detail = root.GetType().Name + ": " + root.Message;
                return false;
            }
        }

        private static bool TryGetNativeItemProto(Type dolocApi, string itemId, out object? proto, out string detail)
        {
            detail = string.Empty;
            proto = null;
            try
            {
                MethodInfo? queryItemProto = dolocApi.GetMethod("QueryItemProto", BindingFlags.Public | BindingFlags.Static);
                if (queryItemProto == null)
                {
                    detail = "DolocAPI.QueryItemProto was not found.";
                    return false;
                }

                object?[] args = new object?[] { itemId, null };
                bool found = queryItemProto.Invoke(null, args) is bool ok && ok && args[1] != null;
                proto = found ? args[1] : null;
                detail = found ? "found " + itemId + " in DolocConfig.Tables.TbItem." : "missing " + itemId + " in DolocConfig.Tables.TbItem.";
                return found;
            }
            catch (Exception ex)
            {
                Exception root = ex is TargetInvocationException && ex.InnerException != null ? ex.InnerException : ex;
                detail = root.GetType().Name + ": " + root.Message;
                return false;
            }
        }

        private int FindHighestNativeFuelEnergyExcept(string excludedItemId, out string itemId)
        {
            itemId = string.Empty;
            int highest = 0;
            HashSet<string> indexedContentIds = new HashSet<string>(
                runtime.GetIndexedContentItems()
                    .Select(i => i.ItemId)
                    .Where(id => !string.IsNullOrWhiteSpace(id)),
                StringComparer.OrdinalIgnoreCase);
            patcher ??= new HarmonyReflectionPatcher(runtime);
            Type? dolocConfig = patcher.ResolveType("DolocTown.Config.DolocConfig, Assembly-CSharp");
            object? tables = dolocConfig?.GetProperty("Tables", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            object? tbItem = tables == null ? null : ReadMember(tables, "TbItem");
            object? dataList = tbItem == null ? null : ReadMember(tbItem, "DataList");
            if (!(dataList is IEnumerable enumerable))
                return highest;

            foreach (object proto in enumerable)
            {
                string id = ReadAnyStringMember(proto, string.Empty, "Id", "id");
                if (string.IsNullOrWhiteSpace(id) || id.Equals(excludedItemId, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (indexedContentIds.Contains(id))
                    continue;

                int energy = ReadAnyIntMember(proto, 0, "ElectricEnergy", "electric_energy");
                if (energy > highest)
                {
                    highest = energy;
                    itemId = id;
                }
            }

            return highest;
        }

        private object? GetDolocConfigDataMapValueForSmoke(string tableName, string id)
        {
            patcher ??= new HarmonyReflectionPatcher(runtime);
            Type? dolocConfig = patcher.ResolveType("DolocTown.Config.DolocConfig, Assembly-CSharp");
            object? tables = dolocConfig == null ? null : ReadStaticMember(dolocConfig, "Tables");
            object? table = tables == null ? null : ReadMember(tables, tableName);
            object? dataMap = table == null ? null : ReadMember(table, "DataMap");
            foreach (object entry in EnumerateObjects(dataMap))
            {
                object? key = ReadMember(entry, "Key");
                if (key == null || !id.Equals(key.ToString(), StringComparison.OrdinalIgnoreCase))
                    continue;
                return ReadMember(entry, "Value");
            }

            return null;
        }

        private static string DescribeCountItemsForSmoke(object? items)
        {
            List<string> parts = new List<string>();
            foreach (object item in EnumerateObjects(items))
            {
                string itemId = ReadAnyStringMember(item, string.Empty, "itemName", "ItemName", "item_name");
                int count = ReadAnyIntMember(item, 0, "itemCount", "ItemCount", "item_count");
                if (!string.IsNullOrWhiteSpace(itemId))
                    parts.Add(itemId + "x" + count);
            }

            return parts.Count == 0 ? "none" : string.Join("|", parts);
        }

        private static bool CountItemsContain(object? items, string itemId, int minimumCount)
        {
            foreach (object item in EnumerateObjects(items))
            {
                string currentId = ReadAnyStringMember(item, string.Empty, "itemName", "ItemName", "item_name");
                int count = ReadAnyIntMember(item, 0, "itemCount", "ItemCount", "item_count");
                if (currentId.Equals(itemId, StringComparison.OrdinalIgnoreCase) && count >= minimumCount)
                    return true;
            }

            return false;
        }

        private static IEnumerable<object> EnumerateObjects(object? value)
        {
            if (value is IEnumerable enumerable && !(value is string))
            {
                foreach (object? item in enumerable)
                {
                    if (item != null)
                        yield return item;
                }
            }
        }

        private bool TryEnsureCoalResourceForOilSmoke(object room, Type resourceRendererType, out string summary)
        {
            TryRenderAllResourcesForSmoke(room);
            int existing = CountRenderedCoalResources(FindUnityObjects(resourceRendererType), out string existingSamples);
            if (existing > 0)
            {
                summary = "existingCoalResources=" + existing + ", samples=" + existingSamples;
                return true;
            }

            if (TryCreateTransientOneActionResourceForSmoke(room, "Ore", "coal", out string createSummary))
            {
                TryRenderAllResourcesForSmoke(room);
                int created = CountRenderedCoalResources(FindUnityObjects(resourceRendererType), out string createdSamples);
                summary = "createdCoalResources=" + created + ", create={" + createSummary + "}, samples=" + createdSamples;
                return created > 0;
            }

            summary = "existingCoalResources=0, samples=" + existingSamples + ", create={" + createSummary + "}";
            return false;
        }

        private static int CountRenderedCoalResources(IEnumerable<object> renderers, out string samplesText)
        {
            int count = 0;
            List<string> samples = new List<string>();
            foreach (object renderer in renderers)
            {
                object? resource = ReadMember(renderer, "DungeonResource");
                if (resource == null || IsRemoved(resource))
                    continue;
                string resourceName = ReadStringMember(resource, "ResourceName", resource.GetType().Name);
                string resourceClass = ReadResourceClass(resource);
                int health = ReadIntMember(resource, "currentHealth", 0);
                if (samples.Count < 8)
                    samples.Add(resourceName + "/class=" + resourceClass + "/health=" + health);
                if (IsCoalResourceNameForSmoke(resourceName))
                    count++;
            }

            samplesText = samples.Count == 0 ? "none" : string.Join(" ; ", samples);
            return count;
        }

        private object? TryCreateTransientEquipmentForSmoke(Type dolocApi, object room, string targetTypeName, IReadOnlyList<string> equipmentIds, out string summary)
        {
            summary = string.Empty;
            Type? hostType = patcher?.ResolveType("DolocTown.IEquipmentHost, Assembly-CSharp");
            if (hostType == null || !hostType.IsInstanceOfType(room))
            {
                summary = "current room is not an IEquipmentHost. room=" + DescribeRoomForSmoke(room);
                return null;
            }

            MethodInfo? createEquipment = hostType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == "CreateEquipment" && m.GetParameters().Length == 6);
            if (createEquipment == null)
            {
                summary = "IEquipmentHost.CreateEquipment was not found.";
                return null;
            }

            List<string> notes = new List<string>();
            int attempted = 0;
            foreach (string equipmentId in equipmentIds)
            {
                object? proto = QueryEquipmentProtoForSmoke(dolocApi, equipmentId);
                if (proto == null)
                {
                    notes.Add(equipmentId + ":missing-proto");
                    continue;
                }

                object? coverSize = ReadMember(proto, "CoverSize");
                int width = coverSize == null ? 1 : Math.Max(1, ReadIntMember(coverSize, "x", 1));
                int height = coverSize == null ? 1 : Math.Max(1, ReadIntMember(coverSize, "y", 1));
                foreach ((int x, int y) in EnumerateSmokeEquipmentAnchors(dolocApi, room, width, height))
                {
                    attempted++;
                    object? anchor = CreateVector2IntForSmoke(x, y);
                    if (anchor == null)
                        continue;
                    if (!AreEquipmentCellsEmptyForSmoke(hostType, room, x, y, width, height))
                        continue;

                    object? worldPosition = CreateEquipmentWorldPositionForSmoke(room, x, y, width);
                    if (worldPosition == null)
                        continue;

                    try
                    {
                        object? equipment = createEquipment.Invoke(room, new object?[] { worldPosition, anchor, proto, false, null, -1 });
                        if (equipment == null)
                        {
                            notes.Add(equipmentId + "@" + x + "," + y + ":null");
                            continue;
                        }

                        if (IsTypeOrBase(equipment.GetType(), targetTypeName))
                        {
                            summary = "transient:" + equipmentId + "@" + x + "," + y + ", attempted=" + attempted + ", room=" + DescribeRoomForSmoke(room);
                            return equipment;
                        }

                        notes.Add(equipmentId + "@" + x + "," + y + ":wrong-type=" + equipment.GetType().FullName);
                        TryRemoveTransientEquipmentForSmoke(room, equipment);
                    }
                    catch (TargetInvocationException ex) when (ex.InnerException != null)
                    {
                        notes.Add(equipmentId + "@" + x + "," + y + ":" + ex.InnerException.GetType().Name);
                    }
                    catch (Exception ex)
                    {
                        notes.Add(equipmentId + "@" + x + "," + y + ":" + ex.GetType().Name);
                    }
                }
            }

            summary = "transient-create-failed targetType=" + targetTypeName + ", attempted=" + attempted + ", notes=" + string.Join("|", notes.Take(8));
            return null;
        }

        private object? TryCreateTransientEquipmentNoRenderForSmoke(Type dolocApi, object room, string targetTypeName, IReadOnlyList<string> equipmentIds, out string summary)
        {
            summary = string.Empty;
            Type? hostType = patcher?.ResolveType("DolocTown.IEquipmentHost, Assembly-CSharp");
            if (hostType == null || !hostType.IsInstanceOfType(room))
            {
                summary = "room is not an IEquipmentHost. room=" + DescribeRoomForSmoke(room);
                return null;
            }

            MethodInfo? createEquipmentNoRender = hostType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == "CreateEquipmentNoRender" && m.GetParameters().Length == 4);
            if (createEquipmentNoRender == null)
            {
                summary = "IEquipmentHost.CreateEquipmentNoRender was not found.";
                return null;
            }

            List<string> notes = new List<string>();
            int attempted = 0;
            foreach (string equipmentId in equipmentIds)
            {
                object? proto = QueryEquipmentProtoForSmoke(dolocApi, equipmentId);
                if (proto == null)
                {
                    notes.Add(equipmentId + ":missing-proto");
                    continue;
                }

                object? coverSize = ReadMember(proto, "CoverSize");
                int width = coverSize == null ? 1 : Math.Max(1, ReadIntMember(coverSize, "x", 1));
                int height = coverSize == null ? 1 : Math.Max(1, ReadIntMember(coverSize, "y", 1));
                foreach ((int x, int y) in EnumerateSmokeEquipmentAnchors(dolocApi, room, width, height))
                {
                    attempted++;
                    object? anchor = CreateVector2IntForSmoke(x, y);
                    object? worldPosition = CreateEquipmentWorldPositionForSmoke(room, x, y, width);
                    if (anchor == null || worldPosition == null)
                        continue;
                    if (!AreEquipmentCellsEmptyForSmoke(hostType, room, x, y, width, height))
                        continue;

                    try
                    {
                        object? equipment = createEquipmentNoRender.Invoke(room, new object?[] { worldPosition, anchor, proto, false });
                        if (equipment == null)
                        {
                            notes.Add(equipmentId + "@" + x + "," + y + ":null");
                            continue;
                        }

                        if (IsTypeOrBase(equipment.GetType(), targetTypeName))
                        {
                            summary = "transient-no-render:" + equipmentId + "@" + x + "," + y + ", attempted=" + attempted + ", room=" + DescribeRoomForSmoke(room);
                            return equipment;
                        }

                        notes.Add(equipmentId + "@" + x + "," + y + ":wrong-type=" + equipment.GetType().FullName);
                        TryRemoveTransientEquipmentForSmoke(room, equipment);
                    }
                    catch (TargetInvocationException ex) when (ex.InnerException != null)
                    {
                        notes.Add(equipmentId + "@" + x + "," + y + ":" + ex.InnerException.GetType().Name);
                    }
                    catch (Exception ex)
                    {
                        notes.Add(equipmentId + "@" + x + "," + y + ":" + ex.GetType().Name);
                    }
                }
            }

            summary = "transient-no-render-create-failed targetType=" + targetTypeName + ", attempted=" + attempted + ", notes=" + string.Join("|", notes.Take(8));
            return null;
        }

        private object? FindChestLocatorSmokeBuildingRoom(Type dolocApi, object archive, out string summary)
        {
            object? currentRoom = ReadStaticMember(dolocApi, "CurrentRoom");
            object? mainFarm = ReadMember(archive, "MainFarm");
            object? buildingManager = mainFarm == null ? null : ReadMember(mainFarm, "DM_building");
            object? buildings = buildingManager == null ? null : ReadMember(buildingManager, "Buildings");
            int scanned = 0;
            var samples = new List<string>();

            if (buildings is IEnumerable enumerable)
            {
                foreach (object? building in enumerable)
                {
                    if (building == null)
                        continue;
                    scanned++;
                    object? room = ReadMember(building, "room");
                    if (room == null)
                    {
                        samples.Add("building" + scanned + ":no-room");
                        continue;
                    }
                    samples.Add(DescribeRoomForSmoke(room));
                    if (ReferenceEquals(room, currentRoom))
                        continue;
                    if (ReadMember(room, "DM_equipment") == null)
                        continue;

                    summary = "selected=" + DescribeRoomForSmoke(room) + ", scanned=" + scanned + ", current=" + (currentRoom == null ? "none" : DescribeRoomForSmoke(currentRoom));
                    return room;
                }
            }

            summary = "scanned=" + scanned + ", current=" + (currentRoom == null ? "none" : DescribeRoomForSmoke(currentRoom)) + ", samples=" + string.Join(" | ", samples.Take(5));
            return null;
        }

        private string SelectZeroBaselineSmokeItemId(Type dolocApi, IReadOnlyList<string> candidates, out int baseline, out string summary)
        {
            baseline = 0;
            var notes = new List<string>();
            foreach (string candidate in candidates)
            {
                object? item = GenerateItemForSmoke(dolocApi, candidate, 1);
                if (item == null)
                {
                    notes.Add(candidate + ":missing");
                    continue;
                }

                int count = CountNativeItemForSmoke(dolocApi, candidate, checkBox: true);
                notes.Add(candidate + ":baseline=" + count);
                if (count == 0)
                {
                    baseline = count;
                    summary = string.Join("|", notes);
                    return candidate;
                }
            }

            summary = string.Join("|", notes);
            return string.Empty;
        }

        private static int CountNativeItemForSmoke(Type dolocApi, string itemId, bool checkBox)
        {
            MethodInfo? countItem = dolocApi.GetMethod("CountItem", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), typeof(bool) }, null);
            object? result = countItem == null ? null : countItem.Invoke(null, new object[] { itemId, checkBox });
            return result is int value ? value : -1;
        }

        private static bool CostNativeItemForSmoke(Type dolocApi, string itemId, int count, bool checkBox)
        {
            MethodInfo? costItem = dolocApi.GetMethod("CostItem", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), typeof(int), typeof(bool) }, null);
            object? result = costItem == null ? null : costItem.Invoke(null, new object[] { itemId, count, checkBox });
            return result is bool value && value;
        }

        private object? FindExistingEquipmentForSmoke(object room, string targetTypeName, string ratioMember, out string summary)
        {
            int scanned = 0;
            object? manager = ReadMember(room, "DM_equipment");
            object? allEquipments = manager == null ? null : ReadMember(manager, "AllEquipments");
            if (allEquipments is IEnumerable enumerable)
            {
                foreach (object equipment in enumerable)
                {
                    if (equipment == null)
                        continue;
                    scanned++;
                    if (!IsTypeOrBase(equipment.GetType(), targetTypeName))
                        continue;
                    double ratio = ReadDoubleMember(equipment, ratioMember, -1);
                    if (ratio < 0 || ratio < 0.999)
                    {
                        summary = "existing:" + DescribeEquipmentForSmoke(equipment) + ", ratio=" + FormatRatio(ratio) + ", scanned=" + scanned;
                        return equipment;
                    }
                }
            }

            summary = "no existing low target. targetType=" + targetTypeName + ", scanned=" + scanned;
            return null;
        }

        private bool AreEquipmentCellsEmptyForSmoke(Type hostType, object room, int anchorX, int anchorY, int width, int height)
        {
            MethodInfo? getEquipment = hostType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(m =>
                {
                    if (m.Name != "GetEquipment")
                        return false;
                    ParameterInfo[] parameters = m.GetParameters();
                    return parameters.Length == 1 && parameters[0].ParameterType.FullName == "UnityEngine.Vector2Int";
                });
            if (getEquipment == null)
                return false;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    object? cell = CreateVector2IntForSmoke(anchorX + x, anchorY + y);
                    if (cell == null)
                        return false;
                    object? existing = getEquipment.Invoke(room, new[] { cell });
                    if (existing != null)
                        return false;
                }
            }
            return true;
        }

        private object? CreateEquipmentWorldPositionForSmoke(object room, int anchorX, int anchorY, int width)
        {
            object? roomPosition = ReadMember(room, "RoomPosition");
            double roomX = ReadDoubleMember(roomPosition, "x", 0);
            double roomY = ReadDoubleMember(roomPosition, "y", 0);
            double worldX = roomX + (anchorX + width * 0.5) * 1.5;
            double worldY = roomY + anchorY * 1.5;
            return CreateVector3ForSmoke(worldX, worldY, 0);
        }

        private object? CreateVector2IntForSmoke(int x, int y)
        {
            Type? vector2Int = patcher?.ResolveType("UnityEngine.Vector2Int, UnityEngine.CoreModule") ??
                patcher?.ResolveType("UnityEngine.Vector2Int, UnityEngine");
            return vector2Int == null ? null : Activator.CreateInstance(vector2Int, new object[] { x, y });
        }

        private object? CreateVector2ForSmoke(double x, double y)
        {
            Type? vector2 = patcher?.ResolveType("UnityEngine.Vector2, UnityEngine.CoreModule") ??
                patcher?.ResolveType("UnityEngine.Vector2, UnityEngine");
            return vector2 == null ? null : Activator.CreateInstance(vector2, new object[] { (float)x, (float)y });
        }

        private object? CreateVector3ForSmoke(double x, double y, double z)
        {
            Type? vector3 = patcher?.ResolveType("UnityEngine.Vector3, UnityEngine.CoreModule") ??
                patcher?.ResolveType("UnityEngine.Vector3, UnityEngine");
            return vector3 == null ? null : Activator.CreateInstance(vector3, new object[] { (float)x, (float)y, (float)z });
        }

        private void TryRemoveTransientEquipmentForSmoke(object? room, object equipment)
        {
            try
            {
                Type? hostType = patcher?.ResolveType("DolocTown.IEquipmentHost, Assembly-CSharp");
                MethodInfo? removeEquipment = hostType?.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .FirstOrDefault(m => m.Name == "RemoveEquipment" && m.GetParameters().Length == 4);
                if (room != null && hostType != null && hostType.IsInstanceOfType(room) && removeEquipment != null)
                    removeEquipment.Invoke(room, new object?[] { equipment, false, false, true });
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke transient equipment removal failed.", ex.ToString());
            }
        }

        private static string DescribeEquipmentForSmoke(object equipment)
        {
            object? anchor = ReadMember(equipment, "Anchor");
            object? proto = ReadMember(equipment, "proto") ?? ReadMember(equipment, "Proto");
            object? coverSize = proto == null ? null : ReadMember(proto, "CoverSize");
            object? sceneAsset = proto == null ? null : ReadMember(proto, "SceneAsset");
            object? function = proto == null ? null : ReadMember(proto, "Function");
            object? renderer = ReadMember(equipment, "Renderer");
            object? transform = renderer == null ? null : ReadMember(renderer, "transform");
            object? localScale = transform == null ? null : ReadMember(transform, "localScale");
            object? inventory = ReadMember(equipment, "inventory");
            string name = ReadStringMember(equipment, "Name", ReadStringMember(equipment, "Title", equipment.GetType().Name));
            int index = ReadIntMember(equipment, "index", -1);
            string anchorText = anchor == null ? "unknown" : ReadIntMember(anchor, "x", 0) + "," + ReadIntMember(anchor, "y", 0);
            string coverText = coverSize == null ? "unknown" : ReadIntMember(coverSize, "x", 0) + "x" + ReadIntMember(coverSize, "y", 0);
            string sceneText = sceneAsset == null ? "unknown" : FirstNonEmpty(ReadStringMember(sceneAsset, "AssetUrl", string.Empty), sceneAsset.ToString() ?? string.Empty);
            string scaleText = localScale == null ? "unknown" : ReadDoubleMember(localScale, "x", 0).ToString("0.##") + "x" + ReadDoubleMember(localScale, "y", 0).ToString("0.##");
            string storageText = inventory == null
                ? "none"
                : ReadIntMember(inventory, "filledCount", 0) + "/" + ReadIntMember(inventory, "capacity", 0) + "/line=" + ReadIntMember(equipment, "lineCapacity", 0);
            return name + "/" + (equipment.GetType().FullName ?? equipment.GetType().Name) + "/index=" + index + "/anchor=" + anchorText + "/cover=" + coverText + "/scene=" + sceneText + "/function=" + (function == null ? "unknown" : function.GetType().Name) + "/rendererScale=" + scaleText + "/storage=" + storageText;
        }
    }
}
