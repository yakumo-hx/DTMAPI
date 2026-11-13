#pragma warning disable CS0618 // Obsolete fishing DTOs reference each other inside their frozen declaration surface.
using System;
using System.Collections.Generic;

namespace DTMAPI.Abstractions
{

    /// <summary>
    /// On the DTMAPI Runtime/main thread, performs a read-only lookup from an item id to its current localized display name.
    /// Each call reads the current native title using the exact item id. Missing or unavailable items return false.
    /// This contract does not expose item DTOs,
    /// enumeration, mutation, hooks, formatting, or UI behavior.
    /// </summary>
    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.5.5")]
    public interface IItemDisplayNameApi
    {
        bool TryGetDisplayName(string itemId, out string displayName);
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.4.2")]
    public interface IPanoramaCameraApi
    {
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.5.0-alpha", Notes = "Semantic crop-container harvesting bridge backed by native PlantBasin maturity and harvest responsibility; first version only executes ordinary PlantBasin-family Harvest(bool,bool) paths and keeps tree-basin cocoa, grass/forage, and other native-owner families unsupported until their ownership is reviewed.")]
    public interface ICropHarvestingApi
    {
        CropHarvestResult ScanMatureCrops(IManifest owner, CropHarvestRequest request);
        CropHarvestResult HarvestMatureCrops(IManifest owner, CropHarvestRequest request);
        BridgeFeatureStatus GetStatus(string uniqueId);
    }

    public enum CropHarvestTargetKind
    {
        Unknown = 0,
        OrdinaryCrop = 1,
        TreeBasinCrop = 5,
        GrassForageBasin = 6
    }

    public enum CropHarvestTargetStatus
    {
        Pending,
        Harvested,
        NotMature,
        AlreadyHarvested,
        UnsupportedBasinType,
        NativeHarvestFailed,
        SkippedByRequestFilter,
        Busy
    }

    public sealed class CropHarvestRequest
    {
        /// <summary>A positive value limits this call. Leaving it unset harvests all eligible crops.</summary>
        public int MaxHarvests { get; set; }
        public bool DryRun { get; set; }
        public bool SendNativeMessage { get; set; } = true;
        public bool VerboseLogging { get; set; }
        public IReadOnlyList<string> TargetIds { get; set; } = Array.Empty<string>();
    }

    public sealed class CropHarvestResult
    {
        public bool Success { get; set; }
        public bool DryRun { get; set; }
        public string OwnerId { get; set; } = string.Empty;
        public int RoomsVisited { get; set; }
        public int PlantBasinsVisited { get; set; }
        public int MatureTargetsFound { get; set; }
        public int HarvestedCount { get; set; }
        public int SkippedCount { get; set; }
        public int FailedCount { get; set; }
        public bool Busy { get; set; }
        public string FailureReason { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public IReadOnlyList<CropHarvestTargetResult> Targets { get; set; } = Array.Empty<CropHarvestTargetResult>();
    }

    public sealed class CropHarvestTargetResult
    {
        /// <summary>
        /// Opaque transient target id. It is only valid for an immediate follow-up harvest request in the same loaded world/session and must not be persisted.
        /// </summary>
        public string TargetId { get; set; } = string.Empty;
        public string RoomId { get; set; } = string.Empty;
        public string RoomTitle { get; set; } = string.Empty;
        public string EquipmentName { get; set; } = string.Empty;
        public string CropId { get; set; } = string.Empty;
        public string CropTitle { get; set; } = string.Empty;
        public CropHarvestTargetKind Kind { get; set; }
        public CropHarvestTargetStatus Status { get; set; }
        public bool IsMature { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public sealed class BridgeFeatureStatus
    {
        public BridgeFeatureStatus(string status, string details)
        {
            Status = string.IsNullOrWhiteSpace(status) ? "unknown" : status;
            Details = details ?? string.Empty;
        }

        public string Status { get; }
        public string Details { get; }
    }

    public struct DtmColor
    {
        public DtmColor(double r, double g, double b, double a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public double R { get; }
        public double G { get; }
        public double B { get; }
        public double A { get; }
    }
}
