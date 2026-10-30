#pragma warning disable CS0618 // Obsolete fishing DTOs reference each other inside their frozen declaration surface.
using System;
using System.Collections.Generic;

namespace DTMAPI.Abstractions
{

    /// <summary>
    /// On the DTMAPI Runtime/main thread, performs a read-only lookup from an item id to its current localized display name.
    /// Implementations cache only successful non-empty results, do not negatively cache failures, and clear the cache across
    /// save load, return-to-title, language changes, and runtime-environment reset boundaries. If the current language
    /// cannot be identified, implementations return fresh native results without caching. This contract does not expose item DTOs,
    /// enumeration, mutation, hooks, formatting, or UI behavior.
    /// </summary>
    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.5.5")]
    public interface IItemDisplayNameApi
    {
        bool TryGetDisplayName(string itemId, out string displayName);
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.2.3")]
    public interface IMailDeliveryApi
    {
        MailItemDeliveryResult SendItemMail(IManifest owner, MailItemDeliveryRequest request);
        BridgeFeatureStatus GetStatus();
    }

    [DtmApiStatus(DtmApiStatus.Proposed, Since = "0.4.2")]
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

    public sealed class MailItemDeliveryRequest
    {
        public string ItemId { get; set; } = string.Empty;
        public int Count { get; set; } = 1;
        public string EmailName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Sender { get; set; } = string.Empty;
        public string TemplateName { get; set; } = "send_item_template";
        public bool SkipIfAlreadyOwned { get; set; } = true;
        public bool PreventDuplicatePendingMail { get; set; } = true;
        public bool RequireEnabledContentSource { get; set; }
        public string RequiredSourceId { get; set; } = string.Empty;
    }

    public sealed class MailItemDeliveryResult
    {
        public bool Success { get; set; }
        public bool Sent { get; set; }
        public bool Skipped { get; set; }
        public string ItemId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int RequestedCount { get; set; }
        public int BackpackCount { get; set; }
        public int PendingMailCount { get; set; }
        public string EmailName { get; set; } = string.Empty;
        public string TemplateName { get; set; } = string.Empty;
        public string SourceId { get; set; } = string.Empty;
        public bool SourceEnabled { get; set; }
        public bool SourceEnablementKnown { get; set; }
        public string FailureReason { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public enum CropHarvestScope
    {
        CurrentFarmAndFarmRooms
    }

    public enum CropHarvestTargetKind
    {
        Unknown,
        OrdinaryCrop,
        Vine,
        MushroomBag,
        Bush,
        TreeBasinCrop,
        GrassForageBasin
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
        public CropHarvestScope Scope { get; set; } = CropHarvestScope.CurrentFarmAndFarmRooms;
        public bool IncludeOrdinaryCrops { get; set; } = true;
        public bool IncludeVines { get; set; }
        public bool IncludeMushroomBags { get; set; }
        public bool IncludeBushes { get; set; }
        public bool IncludeTreeBasinCrops { get; set; }
        public int MaxHarvests { get; set; } = 24;
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
        public CropHarvestScope Scope { get; set; }
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
