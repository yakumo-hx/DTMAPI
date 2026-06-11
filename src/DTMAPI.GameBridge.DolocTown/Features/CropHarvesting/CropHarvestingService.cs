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
        private const int DefaultMaxHarvests = 24;
        private const int MaxSafeHarvests = 200;
        private readonly object gate = new object();
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

        BridgeFeatureStatus ICropHarvestingApi.GetStatus(string uniqueId)
        {
            if (uniqueId != null && statuses.TryGetValue(uniqueId, out BridgeFeatureStatus status))
                return status;

            return new BridgeFeatureStatus("experimental", "Crop harvesting API is available. It scans native PlantBasin equipment and only executes the reviewed ordinary PlantBasin.Harvest path.");
        }

        internal void PublishHookStatus()
        {
            runtime.SetHookStatus("Crops.HarvestingApi", "experimental", "ICropHarvestingApi -> PlantBasin.CouldHarvest/Harvest", "No Harmony hook is installed; calls are explicit API requests and use native PlantBasin harvest responsibility.");
        }

        internal void ResetRuntimeState(string reason)
        {
            lock (gate)
                operationInProgress = false;
            runtime.RuntimeMonitor.Log("CropHarvesting runtime state reset reason=" + reason + ".");
        }

        private CropHarvestResult Execute(IManifest owner, CropHarvestRequest request, bool dryRun)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

            var result = new CropHarvestResult
            {
                OwnerId = owner.UniqueID,
                Scope = request.Scope,
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

                var targetIds = new HashSet<string>(request.TargetIds ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
                List<CropHarvestTargetResult> targets = new List<CropHarvestTargetResult>();
                HashSet<int> roomKeys = new HashSet<int>();
                int harvested = 0;
                foreach (RoomCandidate room in EnumerateCandidateRooms(dolocApi, archive, currentRoom))
                {
                    if (room.Room == null)
                        continue;
                    roomKeys.Add(RuntimeHelpers.GetHashCode(room.Room));

                    foreach (object equipment in EnumerateEquipments(room.Room).ToArray())
                    {
                        if (!IsPlantBasinFamily(equipment))
                            continue;

                        result.PlantBasinsVisited++;
                        CropHarvestTargetResult target = BuildTarget(room, equipment);
                        ApplyRequestFilter(target, request, targetIds);

                        if (target.Status == CropHarvestTargetStatus.Pending)
                        {
                            result.MatureTargetsFound++;
                            if (dryRun)
                            {
                                target.Message = "Mature crop target is available; dry run did not call native Harvest.";
                            }
                            else if (harvested >= request.MaxHarvests)
                            {
                                target.Status = CropHarvestTargetStatus.SkippedByRequestFilter;
                                target.Message = "Skipped because MaxHarvests was reached.";
                            }
                            else
                            {
                                target = HarvestTarget(room, equipment, target, request.SendNativeMessage);
                                if (target.Status == CropHarvestTargetStatus.Harvested)
                                    harvested++;
                            }
                        }

                        targets.Add(target);
                    }
                }

                result.RoomsVisited = roomKeys.Count;
                result.Targets = targets.ToArray();
                result.HarvestedCount = targets.Count(t => t.Status == CropHarvestTargetStatus.Harvested);
                result.FailedCount = targets.Count(t => t.Status == CropHarvestTargetStatus.NativeHarvestFailed || t.Status == CropHarvestTargetStatus.Busy);
                result.SkippedCount = targets.Count - result.HarvestedCount - result.FailedCount - (dryRun ? result.MatureTargetsFound : 0);
                result.Success = result.FailedCount == 0 && (dryRun ? result.MatureTargetsFound > 0 : result.HarvestedCount > 0);
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
                return Finish(result, string.Empty, result.Message, hookStatus, owner.UniqueID);
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

        private CropHarvestTargetResult HarvestTarget(RoomCandidate room, object equipment, CropHarvestTargetResult target, bool sendNativeMessage)
        {
            CropHarvestTargetResult revalidated = BuildTarget(room, equipment);
            revalidated.TargetId = target.TargetId;
            if (revalidated.Status != CropHarvestTargetStatus.Pending)
                return revalidated;

            MethodInfo? harvest = FindMethodInHierarchy(equipment.GetType(), "Harvest", 2);
            if (harvest == null)
            {
                revalidated.Status = CropHarvestTargetStatus.UnsupportedBasinType;
                revalidated.Message = "Native PlantBasin.Harvest(bool,bool) was not available.";
                return revalidated;
            }

            try
            {
                harvest.Invoke(equipment, new object[] { true, sendNativeMessage });
                object? afterCrop = ReadMember(equipment, "Crop") ?? ReadMember(equipment, "crop");
                bool afterMature = afterCrop != null && ReadBoolMember(afterCrop, "isMature", false);
                bool afterCouldHarvest = ReadBoolMember(equipment, "CouldHarvest", false) || ReadBoolMember(equipment, "IsCropMature", false);
                if (afterCrop != null && afterMature && afterCouldHarvest)
                {
                    revalidated.Status = CropHarvestTargetStatus.NativeHarvestFailed;
                    revalidated.Message = "Native Harvest returned but target still appears mature/harvestable.";
                }
                else
                {
                    revalidated.Status = CropHarvestTargetStatus.Harvested;
                    revalidated.IsMature = false;
                    revalidated.Message = "Native PlantBasin.Harvest(true," + sendNativeMessage + ") completed.";
                }
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                revalidated.Status = CropHarvestTargetStatus.NativeHarvestFailed;
                revalidated.Message = ex.InnerException.GetType().Name + ": " + ex.InnerException.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge.CropHarvesting", "Native crop harvest failed.", ex.InnerException.ToString());
            }
            catch (Exception ex)
            {
                revalidated.Status = CropHarvestTargetStatus.NativeHarvestFailed;
                revalidated.Message = ex.GetType().Name + ": " + ex.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge.CropHarvesting", "Native crop harvest failed.", ex.ToString());
            }

            return revalidated;
        }

        private CropHarvestTargetResult BuildTarget(RoomCandidate room, object equipment)
        {
            object? crop = ReadMember(equipment, "Crop") ?? ReadMember(equipment, "crop");
            CropHarvestTargetKind kind = ClassifyTarget(equipment, crop);
            bool hasCrop = ReadBoolMember(equipment, "HasCrop", crop != null);
            bool isMature = ReadBoolMember(equipment, "CouldHarvest", false) ||
                ReadBoolMember(equipment, "IsCropMature", false) ||
                (crop != null && (ReadBoolMember(crop, "isMature", false) || ReadBoolMember(crop, "IsMature", false)));
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

            if (!IsSupportedHarvestKind(kind, equipment))
            {
                target.Status = CropHarvestTargetStatus.UnsupportedBasinType;
                target.Message = "This crop family is visible to scan, but first-version API only executes ordinary PlantBasin.Harvest.";
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
            else if (!isMature)
            {
                target.Status = CropHarvestTargetStatus.NotMature;
                target.Message = "Crop is not currently mature/harvestable.";
            }
            else
            {
                target.Status = CropHarvestTargetStatus.Pending;
                target.Message = "Mature ordinary PlantBasin crop.";
            }

            return target;
        }

        private static void ApplyRequestFilter(CropHarvestTargetResult target, CropHarvestRequest request, HashSet<string> targetIds)
        {
            if (targetIds.Count > 0 && !targetIds.Contains(target.TargetId))
            {
                target.Status = CropHarvestTargetStatus.SkippedByRequestFilter;
                target.Message = "Skipped by TargetIds filter.";
                return;
            }

            bool included =
                (target.Kind == CropHarvestTargetKind.OrdinaryCrop && request.IncludeOrdinaryCrops) ||
                (target.Kind == CropHarvestTargetKind.Vine && request.IncludeVines) ||
                (target.Kind == CropHarvestTargetKind.Mushroom && request.IncludeMushrooms) ||
                (target.Kind == CropHarvestTargetKind.Tree && request.IncludeTrees);
            if (!included && target.Status == CropHarvestTargetStatus.Pending)
            {
                target.Status = CropHarvestTargetStatus.SkippedByRequestFilter;
                target.Message = "Skipped by crop-kind request filter.";
            }
        }

        private static CropHarvestTargetKind ClassifyTarget(object equipment, object? crop)
        {
            Type equipmentType = equipment.GetType();
            if (IsTypeOrBase(equipmentType, "DolocTown.PlantBasinTree"))
                return CropHarvestTargetKind.Tree;
            if (IsTypeOrBase(equipmentType, "DolocTown.PlantBasinGrass"))
                return CropHarvestTargetKind.Grass;

            string text = (ReadStringMember(equipment, "Name") + " " +
                ReadStringMember(equipment, "EquipmentTypeName") + " " +
                ReadStringMember(equipment, "CropTitle") + " " +
                (crop == null ? string.Empty : ReadStringMember(crop, "SeedId") + " " + ReadStringMember(crop, "seedName"))).ToLowerInvariant();
            if (text.IndexOf("mushroom", StringComparison.Ordinal) >= 0 || text.IndexOf("fungus", StringComparison.Ordinal) >= 0 || text.IndexOf("fugus", StringComparison.Ordinal) >= 0)
                return CropHarvestTargetKind.Mushroom;
            if (text.IndexOf("vine", StringComparison.Ordinal) >= 0)
                return CropHarvestTargetKind.Vine;
            if (IsTypeOrBase(equipmentType, "DolocTown.PlantBasin"))
                return CropHarvestTargetKind.OrdinaryCrop;
            return CropHarvestTargetKind.Unknown;
        }

        private static bool IsSupportedHarvestKind(CropHarvestTargetKind kind, object equipment)
        {
            if (kind == CropHarvestTargetKind.Tree || kind == CropHarvestTargetKind.Grass || kind == CropHarvestTargetKind.Unknown)
                return false;
            return IsTypeOrBase(equipment.GetType(), "DolocTown.PlantBasin") &&
                !IsTypeOrBase(equipment.GetType(), "DolocTown.PlantBasinTree") &&
                !IsTypeOrBase(equipment.GetType(), "DolocTown.PlantBasinGrass");
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
            result.FailureReason = failureReason ?? string.Empty;
            if (string.IsNullOrWhiteSpace(result.Message))
                result.Message = message ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(failureReason))
            {
                result.Success = false;
                result.Busy = string.Equals(failureReason, "busy", StringComparison.OrdinalIgnoreCase);
            }

            string statusText = result.Success ? hookStatus : (result.Busy ? "busy" : hookStatus);
            string detail = result.Message;
            statuses[ownerId ?? string.Empty] = new BridgeFeatureStatus(statusText, detail);
            runtime.SetHookStatus("Crops.HarvestingApi", statusText, "ICropHarvestingApi -> PlantBasin.CouldHarvest/Harvest", detail);
            return result;
        }

        private bool TryEnterOperation()
        {
            lock (gate)
            {
                if (operationInProgress)
                    return false;
                operationInProgress = true;
                return true;
            }
        }

        private void ExitOperation()
        {
            lock (gate)
                operationInProgress = false;
        }

        private static CropHarvestRequest NormalizeRequest(CropHarvestRequest? request)
        {
            request ??= new CropHarvestRequest();
            return new CropHarvestRequest
            {
                Scope = CropHarvestScope.CurrentFarmAndFarmRooms,
                IncludeOrdinaryCrops = request.IncludeOrdinaryCrops,
                IncludeVines = request.IncludeVines,
                IncludeTrees = request.IncludeTrees,
                IncludeMushrooms = request.IncludeMushrooms,
                MaxHarvests = Math.Max(1, Math.Min(MaxSafeHarvests, request.MaxHarvests <= 0 ? DefaultMaxHarvests : request.MaxHarvests)),
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

        private static IEnumerable<RoomCandidate> EnumerateCandidateRooms(Type dolocApi, object archive, object currentRoom)
        {
            HashSet<int> visitedRooms = new HashSet<int>();
            foreach (object? room in EnumerateRootRooms(dolocApi, archive, currentRoom))
            {
                foreach (RoomCandidate candidate in EnumerateRoomAndBuildingRooms(room, visitedRooms))
                    yield return candidate;
            }
        }

        private static IEnumerable<object?> EnumerateRootRooms(Type dolocApi, object archive, object currentRoom)
        {
            yield return currentRoom;
            yield return ReadMember(currentRoom, "RootRoom");
            yield return ReadStaticMember(dolocApi, "CurrentRootRoom");
            yield return ReadMember(archive, "currentRoom") ?? ReadMember(archive, "CurrentRoom");
            yield return ReadMember(archive, "MainFarm");
            object? farmData = ReadMember(archive, "farmData");
            yield return farmData == null ? null : ReadMember(farmData, "currentRoom");
            yield return farmData == null ? null : ReadMember(farmData, "MainFarm");
        }

        private static IEnumerable<RoomCandidate> EnumerateRoomAndBuildingRooms(object? room, HashSet<int> visitedRooms)
        {
            if (room == null)
                yield break;

            int key = RuntimeHelpers.GetHashCode(room);
            if (!visitedRooms.Add(key))
                yield break;

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
