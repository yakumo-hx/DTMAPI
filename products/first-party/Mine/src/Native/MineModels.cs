using System;
using System.Collections.Generic;

namespace DTMAPI.Mine
{
    internal sealed class MineDefinition
    {
        internal int CycleMinutes { get; set; } = 120;
        internal string NativeTechTreeId { get; set; } = string.Empty;
        internal string NativeTechNodeId { get; set; } =
            MineProductContract.MineItemId;
        internal string NativeTechNodeTitle { get; set; } = "矿井";
        internal string NativeTechNodeParentId { get; set; } =
            "alloy_material";
        internal string NativeTechNodeAboveTitleContains { get; set; } =
            "指挥官";
        internal IReadOnlyList<MineRecipeInput> RecipeInputs { get; set; } =
            Array.Empty<MineRecipeInput>();
        internal IReadOnlyList<MineOutputRule> OutputRules { get; set; } =
            Array.Empty<MineOutputRule>();
    }

    internal sealed class MineRecipeInput
    {
        internal string ItemId { get; set; } = string.Empty;
        internal int Count { get; set; }
    }

    internal sealed class MineOutputRule
    {
        internal string ItemId { get; set; } = string.Empty;
        internal string DisplayName { get; set; } = string.Empty;
        internal double Weight { get; set; }
        internal int MinCount { get; set; } = 1;
        internal int MaxCount { get; set; } = 1;
    }

    internal sealed class MineRuntimeState
    {
        internal int PlacedMineCount { get; set; }
        internal int ProductionCycleCount { get; set; }
        internal int LastObservedTotalTUs { get; set; } = -1;
        internal int NextDueTotalTUs { get; set; } = -1;
        internal string LastOutputItemId { get; set; } = string.Empty;
        internal string LastOutputDisplayName { get; set; } = string.Empty;
        internal int LastOutputCount { get; set; }
        internal int LastStorageFilledSlots { get; set; }
        internal int LastStorageCapacity { get; set; }
        internal int LastStorageLineCapacity { get; set; }
        internal string NativeTechTreeSummary { get; set; } = string.Empty;
        internal string LastMessage { get; set; } =
            "Mine runtime is not active.";
    }

    internal sealed class MineScheduleEntry
    {
        internal MineScheduleEntry(
            object equipment,
            int nextDueTotalTus,
            int lastObservedTotalTus)
        {
            Equipment = equipment ??
                throw new ArgumentNullException(nameof(equipment));
            NextDueTotalTus = nextDueTotalTus;
            FirstObservedTotalTus = lastObservedTotalTus;
            LastObservedTotalTus = lastObservedTotalTus;
        }

        internal object Equipment { get; }
        internal int NextDueTotalTus { get; set; }
        internal int FirstObservedTotalTus { get; }
        internal int LastObservedTotalTus { get; set; }
        internal int ProductionCycleCount { get; set; }
        internal string BlockedKind { get; set; } = string.Empty;
        internal double BlockedPower { get; set; } = double.NaN;
        internal int BlockedEmptySlots { get; set; } = -1;
        internal int BlockedCapacity { get; set; } = -1;
        internal string LastMessage { get; set; } = string.Empty;
        internal string LastOutputItemId { get; set; } = string.Empty;
        internal string LastOutputDisplayName { get; set; } = string.Empty;
        internal int LastOutputCount { get; set; }
        internal int LastStorageFilledSlots { get; set; }
        internal int LastStorageCapacity { get; set; }
        internal int LastStorageLineCapacity { get; set; }
        internal string LastVisualScaleSummary { get; set; } = string.Empty;
    }
}
