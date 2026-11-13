using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;
using static DTMAPI.GameBridge.DolocTown.GameBridgeNativeHelpers;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class CropHarvestingService : ICropHarvestingApi
    {
        private readonly DtmApiRuntime runtime;
        private readonly Dictionary<string, BridgeFeatureStatus> statuses = new Dictionary<string, BridgeFeatureStatus>(StringComparer.OrdinalIgnoreCase);
        private bool operationInProgress;

        public CropHarvestingService(DtmApiRuntime runtime)
        {
            this.runtime = runtime;
        }

        public CropHarvestResult ScanMatureCrops(IManifest owner, CropHarvestRequest request)
        {
            CropHarvestRequest normalized = NormalizeRequest(request);
            normalized.DryRun = true;
            return Execute(owner, normalized, dryRun: true);
        }

        public CropHarvestResult HarvestMatureCrops(IManifest owner, CropHarvestRequest request)
        {
            CropHarvestRequest normalized = NormalizeRequest(request);
            return Execute(owner, normalized, dryRun: normalized.DryRun);
        }

        internal int RemoveOwner(string ownerId, string reason)
        {
            return statuses.Remove(ownerId) ? 1 : 0;
        }

        internal int CountOwnerResources(string ownerId) => statuses.ContainsKey(ownerId) ? 1 : 0;

        BridgeFeatureStatus ICropHarvestingApi.GetStatus(string uniqueId)
        {
            if (uniqueId != null && statuses.TryGetValue(uniqueId, out BridgeFeatureStatus status))
                return status;

            return new BridgeFeatureStatus("experimental", "Crop harvesting API is available. It scans native crop-container equipment and only executes the reviewed PlantBasin.Harvest(bool,bool) path; tree-basin cocoa and grass/forage containers are scan-only until separate native-owner review.");
        }

        internal void PublishHookStatus()
        {
            runtime.SetHookStatus("Crops.HarvestingApi", "experimental", "ICropHarvestingApi -> Crop.isMature/isDead -> PlantBasin.Harvest", "No Harmony hook is installed; calls are explicit API requests and use native crop-container harvest responsibility.");
        }

        internal void ResetRuntimeState(string reason)
        {
            operationInProgress = false;
            runtime.RuntimeMonitor.Log("CropHarvesting runtime state reset reason=" + reason + ".");
        }

        private CropHarvestResult Execute(IManifest owner, CropHarvestRequest request, bool dryRun)
        {
            var result = new CropHarvestResult
            {
                OwnerId = owner.UniqueID,
                DryRun = dryRun
            };

            if (!TryEnterOperation())
                return Finish(result, "busy", "Crop harvesting request skipped because another scan/harvest operation is already running.", "busy", owner.UniqueID);
            try
            {
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                object? archive = ReadStaticMember(dolocApi, "archiveHandle");
                object? currentRoom = ReadStaticMember(dolocApi, "CurrentRoom");
                if (dolocApi == null || archive == null || currentRoom == null)
                    return Finish(result, "missing-room", "Current room/archive is unavailable; crop harvesting cannot run outside a loaded save.", "pending", owner.UniqueID);

                var targetIds = new HashSet<string>(request.TargetIds, StringComparer.OrdinalIgnoreCase);
                List<CropHarvestTargetResult> targets = new List<CropHarvestTargetResult>();
                int harvested = 0;
                object? farmData = ReadMember(archive, "farmData");
                object? farm = ReadMember(archive, "MainFarm") ?? (farmData == null ? null : ReadMember(farmData, "MainFarm"));
                if (farm == null)
                    return Finish(result, "no-farm-scope", "Native farm root is unavailable; crop harvesting cannot run yet.", "pending", owner.UniqueID);
                RoomCandidate[] roomCandidates = EnumerateRoomAndBuildingRooms(farm, new List<object>()).ToArray();

                foreach (RoomCandidate room in roomCandidates)
                {
                    result.RoomsVisited++;

                    foreach (object equipment in EnumerateEquipments(room.Room).ToArray())
                    {
                        if (!ReferenceEquals(archive, ReadStaticMember(dolocApi, "archiveHandle")) ||
                            !ReferenceEquals(currentRoom, ReadStaticMember(dolocApi, "CurrentRoom")))
                        {
                            result.FailureReason = "save-or-room-changed";
                            break;
                        }
                        if (!IsPlantBasinFamily(equipment))
                            continue;

                        result.PlantBasinsVisited++;
                        CropHarvestTargetResult target = BuildTarget(room, equipment);
                        ApplyRequestFilter(target, targetIds);

                        if (target.Status == CropHarvestTargetStatus.Pending)
                        {
                            result.MatureTargetsFound++;
                            if (dryRun)
                            {
                                target.Message = "Mature crop target is available; dry run did not call native Harvest.";
                            }
                            else if (request.MaxHarvests > 0 && harvested >= request.MaxHarvests)
                            {
                                target.Status = CropHarvestTargetStatus.SkippedByRequestFilter;
                                target.Message = "Skipped because MaxHarvests was reached.";
                            }
                            else
                            {
                                target = HarvestTarget(equipment, target, request.SendNativeMessage);
                                if (target.Status == CropHarvestTargetStatus.Harvested)
                                    harvested++;
                            }
                        }

                        targets.Add(target);
                    }
                    if (result.FailureReason.Length > 0)
                        break;
                }

                result.Targets = targets.ToArray();
                result.HarvestedCount = targets.Count(t => t.Status == CropHarvestTargetStatus.Harvested);
                result.FailedCount = targets.Count(t => t.Status == CropHarvestTargetStatus.NativeHarvestFailed || t.Status == CropHarvestTargetStatus.Busy);
                result.SkippedCount = targets.Count - result.HarvestedCount - result.FailedCount - (dryRun ? result.MatureTargetsFound : 0);
                result.Success = result.FailedCount == 0 && result.FailureReason.Length == 0;
                result.Message = (dryRun ? "Scan" : "Harvest") +
                    " owner=" + owner.UniqueID +
                    " rooms=" + result.RoomsVisited +
                    " basins=" + result.PlantBasinsVisited +
                    " mature=" + result.MatureTargetsFound +
                    " harvested=" + result.HarvestedCount +
                    " skipped=" + result.SkippedCount +
                    " failed=" + result.FailedCount +
                    " targetFilter=" + targetIds.Count +
                    ".";

                string hookStatus = result.Success ? (dryRun ? "scan-verified" : "verified") : "pending";
                if (result.FailedCount > 0)
                    hookStatus = "failed";

                if (request.VerboseLogging || result.Success || result.FailedCount > 0)
                    runtime.RuntimeMonitor.Log("CropHarvesting API " + result.Message);
                return Finish(result, result.FailureReason, result.Message, hookStatus, owner.UniqueID);
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge.CropHarvesting", "Crop harvesting API failed.", ex.ToString());
                return Finish(result, ex.GetType().Name, ex.Message, "failed", owner.UniqueID);
            }
            finally
            {
                ExitOperation();
            }
        }

        private CropHarvestTargetResult HarvestTarget(object equipment, CropHarvestTargetResult target, bool sendNativeMessage)
        {
            MethodInfo? harvest = FindHarvestMethod(equipment.GetType());
            if (harvest == null)
            {
                target.Status = CropHarvestTargetStatus.UnsupportedBasinType;
                target.Message = "Native PlantBasin.Harvest(bool,bool) was not available.";
                return target;
            }

            try
            {
                harvest.Invoke(equipment, new object[] { true, sendNativeMessage });
                object? afterCrop = ReadMember(equipment, "Crop") ?? ReadMember(equipment, "crop");
                bool afterCropMature = IsCropMature(afterCrop);
                bool afterCouldHarvest = IsBasinHarvestable(equipment);
                if (afterCrop != null && afterCropMature && afterCouldHarvest)
                {
                    target.Status = CropHarvestTargetStatus.NativeHarvestFailed;
                    target.Message = "Native Harvest returned but target still appears mature/harvestable.";
                }
                else
                {
                    target.Status = CropHarvestTargetStatus.Harvested;
                    target.IsMature = false;
                    target.Message = "Native PlantBasin.Harvest(true," + sendNativeMessage + ") completed.";
                }
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                target.Status = CropHarvestTargetStatus.NativeHarvestFailed;
                target.Message = ex.InnerException.GetType().Name + ": " + ex.InnerException.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge.CropHarvesting", "Native crop harvest failed.", ex.InnerException.ToString());
            }
            catch (Exception ex)
            {
                target.Status = CropHarvestTargetStatus.NativeHarvestFailed;
                target.Message = ex.GetType().Name + ": " + ex.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge.CropHarvesting", "Native crop harvest failed.", ex.ToString());
            }

            return target;
        }

        private CropHarvestTargetResult BuildTarget(RoomCandidate room, object equipment)
        {
            object? crop = ReadMember(equipment, "Crop") ?? ReadMember(equipment, "crop");
            CropHarvestTargetKind kind = ClassifyTarget(equipment);
            bool hasCrop = ReadBoolMember(equipment, "HasCrop", crop != null);
            bool basinHarvestable = IsBasinHarvestable(equipment);
            bool isMature = IsCropMature(crop);
            bool hasHarvested = crop != null && ReadBoolMember(crop, "hasHarvested", false);
            string cropId = crop == null ? string.Empty : FirstText(
                ReadStringMember(crop, "SeedId"),
                ReadStringMember(crop, "seedName"),
                ReadStringMember(ReadMember(crop, "seedProto") ?? crop, "Id"),
                ReadStringMember(ReadMember(crop, "Proto") ?? crop, "Id"));
            string cropTitle = FirstText(ReadStringMember(equipment, "CropTitle"), cropId);
            string equipmentName = FirstText(ReadStringMember(equipment, "Name"), ReadStringMember(equipment, "EquipmentTypeName"), equipment.GetType().Name);

            var target = new CropHarvestTargetResult
            {
                TargetId = BuildTargetId(equipment),
                RoomId = room.RoomId,
                RoomTitle = room.RoomTitle,
                EquipmentName = equipmentName,
                CropId = cropId,
                CropTitle = cropTitle,
                Kind = kind,
                IsMature = isMature
            };

            if (ReadBoolMember(equipment, "IsRemoved", false) ||
                !ReferenceEquals(room.Room, ReadMember(equipment, "CurrentRoom")) ||
                !EnumerateEquipments(room.Room).Any(candidate => ReferenceEquals(candidate, equipment)))
            {
                target.Status = CropHarvestTargetStatus.SkippedByRequestFilter;
                target.Message = "Crop container is no longer attached to the scanned room.";
            }
            else if (!IsSupportedHarvestKind(kind))
            {
                target.Status = CropHarvestTargetStatus.UnsupportedBasinType;
                target.Message = BuildUnsupportedFamilyMessage(kind);
            }
            else if (!hasCrop)
            {
                target.Status = CropHarvestTargetStatus.AlreadyHarvested;
                target.Message = "No crop is currently attached to the basin.";
            }
            else if (hasHarvested)
            {
                target.Status = CropHarvestTargetStatus.AlreadyHarvested;
                target.Message = "Native crop state reports hasHarvested.";
            }
            else if (!isMature || !basinHarvestable)
            {
                target.Status = CropHarvestTargetStatus.NotMature;
                target.Message = "Crop must be mature, alive, and harvestable before native Harvest can run.";
            }
            else
            {
                target.Status = CropHarvestTargetStatus.Pending;
                target.Message = "Mature crop-container target guarded by basin-level harvestability.";
            }

            return target;
        }

        private static void ApplyRequestFilter(CropHarvestTargetResult target, HashSet<string> targetIds)
        {
            if (targetIds.Count > 0 && !targetIds.Contains(target.TargetId))
            {
                target.Status = CropHarvestTargetStatus.SkippedByRequestFilter;
                target.Message = "Skipped by TargetIds filter.";
            }
        }

        private static CropHarvestTargetKind ClassifyTarget(object equipment)
        {
            Type equipmentType = equipment.GetType();
            if (IsTypeOrBase(equipmentType, "DolocTown.PlantBasinTree"))
                return CropHarvestTargetKind.TreeBasinCrop;
            if (IsTypeOrBase(equipmentType, "DolocTown.PlantBasinGrass"))
                return CropHarvestTargetKind.GrassForageBasin;
            return IsTypeOrBase(equipmentType, "DolocTown.PlantBasin")
                ? CropHarvestTargetKind.OrdinaryCrop
                : CropHarvestTargetKind.Unknown;
        }

        private static bool IsSupportedHarvestKind(CropHarvestTargetKind kind) =>
            kind == CropHarvestTargetKind.OrdinaryCrop;

        private static string BuildUnsupportedFamilyMessage(CropHarvestTargetKind kind)
        {
            if (kind == CropHarvestTargetKind.TreeBasinCrop)
                return "Tree-basin crop container is visible to scan, but cocoa/tree-basin harvest uses a different native owner and is not executed by this PlantBasin.Harvest API slice.";
            if (kind == CropHarvestTargetKind.GrassForageBasin)
                return "Grass/forage basin is visible to scan, but forage harvesting is not part of this crop-container Harvest API slice.";
            if (kind == CropHarvestTargetKind.Unknown)
                return "Unknown crop container is visible to scan, but first-version API only executes reviewed PlantBasin.Harvest(bool,bool) targets.";
            return "This crop container is visible to scan, but its native owner has not been approved for execution.";
        }

        private static bool IsPlantBasinFamily(object equipment)
        {
            Type type = equipment.GetType();
            return IsTypeOrBase(type, "DolocTown.PlantBasin") ||
                IsTypeOrBase(type, "DolocTown.PlantBasinTree") ||
                IsTypeOrBase(type, "DolocTown.PlantBasinGrass") ||
                IsTypeOrBase(type, "DolocTown.PlantBasinSimple");
        }

        private CropHarvestResult Finish(CropHarvestResult result, string failureReason, string message, string hookStatus, string ownerId)
        {
            result.FailureReason = failureReason;
            if (string.IsNullOrWhiteSpace(result.Message))
                result.Message = message;
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                result.Success = false;
                result.Busy = string.Equals(failureReason, "busy", StringComparison.OrdinalIgnoreCase);
            }

            string statusText = result.Busy ? "busy" : hookStatus;
            string detail = result.Message;
            statuses[ownerId] = new BridgeFeatureStatus(statusText, detail);
            runtime.SetHookStatus("Crops.HarvestingApi", statusText, "ICropHarvestingApi -> Crop.isMature/isDead -> PlantBasin.Harvest", detail);
            return result;
        }

        private bool TryEnterOperation()
        {
            if (operationInProgress)
                return false;
            operationInProgress = true;
            return true;
        }

        private void ExitOperation()
        {
            operationInProgress = false;
        }

        private static CropHarvestRequest NormalizeRequest(CropHarvestRequest? request)
        {
            request ??= new CropHarvestRequest();
            return new CropHarvestRequest
            {
                MaxHarvests = request.MaxHarvests,
                DryRun = request.DryRun,
                SendNativeMessage = request.SendNativeMessage,
                VerboseLogging = request.VerboseLogging,
                TargetIds = (request.TargetIds ?? Array.Empty<string>()).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
            };
        }

        private static string BuildTargetId(object equipment)
        {
            return "equipment:" + RuntimeHelpers.GetHashCode(equipment).ToString(CultureInfo.InvariantCulture);
        }

        private static IEnumerable<RoomCandidate> EnumerateRoomAndBuildingRooms(object? room, List<object> visitedRooms)
        {
            if (room == null)
                yield break;

            if (visitedRooms.Any(visited => ReferenceEquals(visited, room)))
                yield break;
            visitedRooms.Add(room);
            yield return new RoomCandidate(room);

            object? buildingManager = ReadMember(room, "DM_building");
            object? buildings = buildingManager == null ? null : ReadMember(buildingManager, "Buildings");
            if (!(buildings is IEnumerable buildingEnumerable))
                yield break;

            foreach (object? building in buildingEnumerable)
            {
                object? childRoom = building == null ? null : ReadMember(building, "room");
                foreach (RoomCandidate child in EnumerateRoomAndBuildingRooms(childRoom, visitedRooms))
                    yield return child;
            }
        }

        private static IEnumerable<object> EnumerateEquipments(object room)
        {
            object? equipmentManager = ReadMember(room, "DM_equipment");
            object? allEquipments = equipmentManager == null ? null : ReadMember(equipmentManager, "AllEquipments");
            if (!(allEquipments is IEnumerable enumerable))
                yield break;

            foreach (object? equipment in enumerable)
            {
                if (equipment != null)
                    yield return equipment;
            }
        }

        private static MethodInfo? FindHarvestMethod(Type type)
        {
            MethodInfo? method = FindMethodInHierarchy(type, "Harvest", 2);
            if (method == null)
                return null;

            ParameterInfo[] parameters = method.GetParameters();
            if (parameters[0].ParameterType == typeof(bool) && parameters[1].ParameterType == typeof(bool))
                return method;

            return null;
        }

        private static bool IsBasinHarvestable(object equipment)
        {
            return ReadBoolMember(equipment, "CouldHarvest", false);
        }

        private static bool IsCropMature(object? crop)
        {
            return crop != null && ReadBoolMember(crop, "isMature", false) &&
                !ReadBoolMember(crop, "isDead", true);
        }

        private sealed class RoomCandidate
        {
            public RoomCandidate(object room)
            {
                Room = room;
                RoomId = FirstText(ReadStringMember(room, "RoomId"), ReadStringMember(room, "Id"), room.GetType().Name);
                RoomTitle = FirstText(ReadStringMember(room, "Title"), ReadStringMember(room, "RoomTitle"), RoomId);
            }

            public object Room { get; }
            public string RoomId { get; }
            public string RoomTitle { get; }
        }
    }
}
