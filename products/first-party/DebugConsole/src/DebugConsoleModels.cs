using System;
using System.Collections.Generic;
using DTMAPI.Abstractions;

namespace DTMAPI.DebugConsole
{
    internal enum AdvancedTimeAdvanceKind
    {
        Day,
        Week,
        Month
    }

    internal sealed class InventoryDebugQuery
    {
        public string SearchText { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string SourceId { get; set; } = string.Empty;
        public bool ModItemsOnly { get; set; }
        public bool IncludeUnavailable { get; set; } = true;
        public int Page { get; set; }
        public int PageSize { get; set; } = 12;
    }

    internal sealed class InventoryDebugPage
    {
        public IReadOnlyList<InventoryDebugItem> Items { get; set; } = Array.Empty<InventoryDebugItem>();
        public IReadOnlyList<string> Categories { get; set; } = Array.Empty<string>();
        public IReadOnlyList<InventoryDebugSourceGroup> Sources { get; set; } = Array.Empty<InventoryDebugSourceGroup>();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    internal sealed class InventoryDebugSourceGroup
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string SourceKind { get; set; } = string.Empty;
        public bool IsModSource { get; set; }
        public int Count { get; set; }
        public bool Enabled { get; set; } = true;
        public bool EnablementKnown { get; set; } = true;
        public ulong? WorkshopId { get; set; }
    }

    internal sealed class InventoryDebugItem
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string ChineseName { get; set; } = string.Empty;
        public string EnglishName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string SubCategory { get; set; } = string.Empty;
        public IReadOnlyList<string> Tags { get; set; } = Array.Empty<string>();
        public string SearchText { get; set; } = string.Empty;
        public int MaxStack { get; set; }
        public bool CanSpawn { get; set; }
        public bool CanGive { get; set; }
        public bool RuntimeLoaded { get; set; }
        public bool IsModItem { get; set; }
        public string CannotGiveReason { get; set; } = string.Empty;
        public bool HasIcon { get; set; }
        public string IconAssetKey { get; set; } = string.Empty;
        public string IconPath { get; set; } = string.Empty;
        public string SourceKind { get; set; } = "Vanilla";
        public string SourceModTitle { get; set; } = string.Empty;
        public string SourceId { get; set; } = string.Empty;
        public ulong? WorkshopId { get; set; }
        public bool SourceEnabled { get; set; } = true;
        public bool SourceEnablementKnown { get; set; } = true;
        public string RootPath { get; set; } = string.Empty;
        public string ContentPath { get; set; } = string.Empty;
        public int LoadOrder { get; set; } = -1;
        public int RuntimeOrder { get; set; } = int.MaxValue;
    }

    internal sealed class InventoryGiveResult
    {
        public bool Success { get; set; }
        public string ItemId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int RequestedCount { get; set; }
        public int BeforeCount { get; set; }
        public int AfterCount { get; set; }
        public int GivenCount { get; set; }
        public string FailureReason { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    internal sealed class WeatherDebugState
    {
        public string CurrentWeatherId { get; set; } = string.Empty;
        public string CurrentWeatherName { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }
        public int Hour { get; set; }
        public string SeasonName { get; set; } = string.Empty;
        public IReadOnlyList<string> CurrentDayForecastWeatherIds { get; set; } = Array.Empty<string>();
    }

    internal sealed class WeatherDebugOption
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsCurrent { get; set; }
        public bool IsCurrentDayForecast { get; set; }
        public bool IsMalignant { get; set; }
        public bool IsRainy { get; set; }
        public bool IsWindy { get; set; }
        public double Sun { get; set; }
        public double Water { get; set; }
        public double Wind { get; set; }
    }

    internal sealed class WeatherSetResult
    {
        public bool Success { get; set; }
        public string WeatherId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string BeforeWeatherId { get; set; } = string.Empty;
        public string AfterWeatherId { get; set; } = string.Empty;
        public bool PatchedCurrentPeriod { get; set; }
        public string FailureReason { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    internal sealed class TeleportDestination
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string SuggestedDisplayName { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
        public string MarkPointId { get; set; } = string.Empty;
        public string RoomId { get; set; } = string.Empty;
        public double X { get; set; }
        public double Y { get; set; }
        public bool IsStation { get; set; }
        public bool IsUnlocked { get; set; } = true;
        public string Source { get; set; } = string.Empty;
    }

    internal sealed class TeleportSnapshot
    {
        public string RoomId { get; set; } = string.Empty;
        public string RoomTitle { get; set; } = string.Empty;
        public string RoomType { get; set; } = string.Empty;
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
    }

    internal sealed class TeleportResult
    {
        public bool Success { get; set; }
        public string DestinationId { get; set; } = string.Empty;
        public string DestinationName { get; set; } = string.Empty;
        public string MarkPointId { get; set; } = string.Empty;
        public TeleportSnapshot Before { get; set; } = new TeleportSnapshot();
        public TeleportSnapshot AfterRequest { get; set; } = new TeleportSnapshot();
        public string FailureReason { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    internal sealed class InstantSaveDebugState
    {
        public bool CanSave { get; set; }
        public int? SaveSlot { get; set; }
        public TeleportSnapshot CurrentLocation { get; set; } = new TeleportSnapshot();
        public string FailureReason { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    internal sealed class InstantSaveDebugResult
    {
        public bool Success { get; set; }
        public bool ReloadAfterSave { get; set; }
        public bool ReloadRequested { get; set; }
        public int? SaveSlot { get; set; }
        public InstantSaveDebugState Before { get; set; } = new InstantSaveDebugState();
        public InstantSaveDebugState AfterSave { get; set; } = new InstantSaveDebugState();
        public InstantSaveDebugState AfterReloadRequest { get; set; } = new InstantSaveDebugState();
        public string FailureReason { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    internal sealed class TimeDebugState
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }
        public int Hour { get; set; }
        public int Minute { get; set; }
        public string CurrentWeatherId { get; set; } = string.Empty;
        public string CurrentWeatherName { get; set; } = string.Empty;
        public string SeasonName { get; set; } = string.Empty;
        public string Period { get; set; } = string.Empty;
    }

    internal sealed class TimeSkipResult
    {
        public bool Success { get; set; }
        public TimeDebugState Before { get; set; } = new TimeDebugState();
        public TimeDebugState After { get; set; } = new TimeDebugState();
        public int AdvancedGameMinutes { get; set; }
        public int AdvancedSeconds { get; set; }
        public int TargetHour { get; set; }
        public string FailureReason { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    internal sealed class TimeScaleDebugResult
    {
        public bool Success { get; set; }
        public double RequestedMultiplier { get; set; }
        public double BeforeMultiplier { get; set; }
        public double AfterMultiplier { get; set; }
        public string FailureReason { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    internal sealed class DebugValueResult
    {
        public bool Success { get; set; }
        public string ValueId { get; set; } = string.Empty;
        public int RequestedDelta { get; set; }
        public int BeforeValue { get; set; }
        public int AfterValue { get; set; }
        public string FailureReason { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    internal sealed class DebugCommandResult
    {
        public bool Success { get; set; }
        public string CommandId { get; set; } = string.Empty;
        public int AffectedCount { get; set; }
        public string FailureReason { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    internal sealed class SpawnDebugResult
    {
        public bool Success { get; set; }
        public string SpawnId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int RequestedCount { get; set; }
        public int SpawnedCount { get; set; }
        public string FailureReason { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    internal sealed class MovementDebugState
    {
        public double Multiplier { get; set; } = 1;
        public double MoveSpeed { get; set; }
        public bool IsDefault { get; set; } = true;
        public string Source { get; set; } = string.Empty;
    }

    internal sealed class MovementSpeedResult
    {
        public bool Success { get; set; }
        public double RequestedMultiplier { get; set; }
        public double AppliedMultiplier { get; set; }
        public MovementDebugState Before { get; set; } = new MovementDebugState();
        public MovementDebugState After { get; set; } = new MovementDebugState();
        public string FailureReason { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
