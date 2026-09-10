using System;
using System.Collections.Generic;
using System.Linq;
using DTMAPI.Abstractions;

namespace DTMAPI.DebugConsole
{
    internal enum DebugCatalogKind
    {
        Item,
        Monster,
        Animal
    }

    internal enum AnimalSpawnState
    {
        Child,
        Adult,
        Ready
    }

    internal sealed class WeatherPanelSnapshot
    {
        internal WeatherDebugState State { get; set; } = new WeatherDebugState();
        internal IReadOnlyList<WeatherDebugOption> Options { get; set; } =
            Array.Empty<WeatherDebugOption>();
        internal IReadOnlyCollection<string> AvailableWeatherIds { get; set; } =
            Array.Empty<string>();

        internal bool IsAvailable(string weatherId) =>
            AvailableWeatherIds.Contains(
                weatherId ?? string.Empty,
                StringComparer.OrdinalIgnoreCase);
    }

    internal sealed class SpawnCatalogOption
    {
        internal string Id { get; set; } = string.Empty;
        internal string DisplayName { get; set; } = string.Empty;
        internal string Category { get; set; } = string.Empty;
        internal string IconAssetKey { get; set; } = string.Empty;
        internal object? Icon { get; set; }
        internal bool IsAvailable { get; set; }
        internal string UnavailableReason { get; set; } = string.Empty;
    }

    internal sealed class AnimalCatalogOption
    {
        internal string CardId { get; set; } = string.Empty;
        internal string AnimalId { get; set; } = string.Empty;
        internal string DisplayName { get; set; } = string.Empty;
        internal AnimalSpawnState State { get; set; }
        internal string IconAssetKey { get; set; } = string.Empty;
        internal object? Icon { get; set; }
        internal bool IsAvailable { get; set; }
        internal string UnavailableReason { get; set; } = string.Empty;
        internal string SourceId { get; set; } = "__base";
        internal string SourceDisplayName { get; set; } = string.Empty;
        internal string SourceKind { get; set; } = "Vanilla";
        internal bool IsModSource { get; set; }
        internal bool SourceEnabled { get; set; } = true;
        internal bool SourceEnablementKnown { get; set; } = true;
        internal ulong? WorkshopId { get; set; }
    }

    internal sealed class AnimalContentSource
    {
        internal string AnimalId { get; set; } = string.Empty;
        internal string SourceId { get; set; } = string.Empty;
        internal string DisplayName { get; set; } = string.Empty;
        internal string SourceKind { get; set; } = "DTMAPI";
        internal bool Enabled { get; set; } = true;
        internal bool EnablementKnown { get; set; } = true;
        internal ulong? WorkshopId { get; set; }
    }

    internal sealed class SpawnActionResult
    {
        internal SpawnActionResult(
            SpawnDebugResult result,
            int addedEntityCount,
            string stopReason)
        {
            Result = result ?? throw new ArgumentNullException(nameof(result));
            AddedEntityCount = Math.Max(0, addedEntityCount);
            StopReason = stopReason ?? string.Empty;
        }

        internal SpawnDebugResult Result { get; }
        internal int AddedEntityCount { get; }
        internal string StopReason { get; }
    }
}
