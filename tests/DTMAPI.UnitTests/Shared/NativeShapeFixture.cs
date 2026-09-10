using System.Collections.Generic;

namespace DolocTown.UI
{
    public enum FakeGridConstraint
    {
        Flexible,
        FixedColumnCount,
        FixedRowCount
    }

    public sealed class FakeGridLayoutGroup
    {
        public FakeGridConstraint constraint { get; set; } = FakeGridConstraint.FixedColumnCount;

        public int constraintCount { get; set; }
    }

    public sealed class TextButton
    {
        public bool isVisible { get; set; } = true;
    }

    public sealed class MenuButton
    {
        public bool isVisible { get; set; } = true;
    }

    public sealed class HomePageTextMenu
    {
        public FakeGridLayoutGroup slotLayoutGroup { get; set; } = new FakeGridLayoutGroup { constraintCount = 2 };

        public List<TextButton> slots { get; } = new List<TextButton>();

        public int totalCapacity { get; set; } = 5;

        public int lineCapacity { get; set; } = 1;

        public int rowCount { get; set; } = 5;

        public int RebuildCount { get; private set; }

        public void RebuildLayout()
        {
            RebuildCount++;
        }
    }

    public sealed class MenuUI
    {
        public FakeGridLayoutGroup slotLayoutGroup { get; set; } = new FakeGridLayoutGroup { constraintCount = 2 };

        public List<MenuButton> slots { get; } = new List<MenuButton>();

        public int totalCapacity { get; set; } = 8;

        public int lineCapacity { get; set; } = 8;

        public int rowCount { get; set; } = 1;

        public int RebuildCount { get; private set; }

        public void RebuildLayout()
        {
            RebuildCount++;
        }
    }

    public sealed class MainMenuPanel
    {
        public MenuUI menu { get; set; } = new MenuUI();
    }
}

namespace DolocTown
{
    public sealed class HomePageUiState
    {
        public DolocTown.UI.HomePageTextMenu textMenu { get; set; } = new DolocTown.UI.HomePageTextMenu();
    }

    public sealed class MainMenuUiState
    {
        public DolocTown.UI.MainMenuPanel panel { get; set; } = new DolocTown.UI.MainMenuPanel();
    }

    public sealed class AgentStateFishingBattle
    {
    }

    public sealed class AgentStateFishingPull
    {
        public bool IsFailed { get; set; }
    }

    public sealed class ItemSeed
    {
    }

    public sealed class ItemFertilizer
    {
        public ItemFunctionFertilizer func { get; set; } = new ItemFunctionFertilizer();
    }

    public sealed class ItemFilm
    {
    }

    public sealed class ItemFunctionFertilizer
    {
        public bool IsTree { get; set; }
    }

    public interface IGatherableEquipment
    {
    }

    public sealed class ResinCollector : IGatherableEquipment
    {
        public int currentValue { get; set; }

        public bool IsGatherable { get; set; }
    }

    public sealed class PlantBasin
    {
        public bool IsPlanted { get; set; }

        public bool CouldHarvest { get; set; }

        public bool IsFertilizerd { get; set; }

        public bool IsProtected { get; set; }

        public PlantBasinSupply supply { get; } = new PlantBasinSupply();
    }

    public sealed class PlantBasinSupply
    {
        public bool IsProtectedFull { get; set; }
    }

    public sealed class FlowerPot
    {
        public bool IsPlanted { get; set; }
    }

    public sealed class PlantBasinTree
    {
        public TreeCrop? Crop { get; set; }
    }

    public sealed class TreeCrop
    {
        public bool IsFertilizered { get; set; }
    }

    public sealed class Sprinkler
    {
    }

    public sealed class FarmLight
    {
    }

    public sealed class Sound
    {
    }

    public sealed class AnimalRenderer
    {
    }

    public sealed class RoomConnector
    {
    }
}
