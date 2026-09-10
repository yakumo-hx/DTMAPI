using System.Collections.Generic;
using DTMAPI.Abstractions;

namespace DTMAPI.DebugConsole
{
    internal interface IInventoryActions
    {
        InventoryDebugPage GetItems(InventoryDebugQuery query);
        InventoryGiveResult GiveItem(
            IManifest owner,
            string itemId,
            int count);
        BridgeFeatureStatus GetStatus();
    }

    internal interface IWeatherActions
    {
        WeatherPanelSnapshot GetPanelSnapshot();
        WeatherDebugState GetState();
        IReadOnlyList<WeatherDebugOption> GetAvailableWeathers();
        WeatherSetResult SetWeather(
            IManifest owner,
            string weatherId,
            bool patchCurrentPeriod);
        BridgeFeatureStatus GetStatus();
    }

    internal interface ITeleportActions
    {
        IReadOnlyList<TeleportDestination> GetDestinations();
        TeleportResult Teleport(
            IManifest owner,
            string destinationId);
        BridgeFeatureStatus GetStatus();
    }

    internal interface IInstantSaveActions
    {
        InstantSaveDebugState GetState();
        InstantSaveDebugResult Save(
            IManifest owner,
            bool reloadAfterSave);
        BridgeFeatureStatus GetStatus();
    }

    internal interface ITimeActions
    {
        TimeDebugState GetState();
        TimeSkipResult SkipToNextWeatherPeriod(IManifest owner);
        BridgeFeatureStatus GetStatus();
    }

    internal interface IMovementActions
    {
        MovementDebugState GetState();
        MovementSpeedResult SetSpeedMultiplier(
            IManifest owner,
            double multiplier);
        MovementSpeedResult ResetSpeed(
            IManifest owner,
            string reason);
        BridgeFeatureStatus GetStatus();
    }

    internal interface IAdvancedActions
    {
        IReadOnlyList<SpawnCatalogOption> GetMonsterCatalog();
        IReadOnlyList<AnimalCatalogOption> GetAnimalCatalog();
        CreativeModeState GetCreativeModeState();
        TimeSkipResult AdvanceTime(
            IManifest owner,
            AdvancedTimeAdvanceKind kind,
            int amount);
        TimeScaleDebugResult SetTimeScale(
            IManifest owner,
            double multiplier);
        TimeScaleDebugResult ResetTimeScale(
            IManifest owner,
            string reason);
        DebugValueResult AddMoney(IManifest owner, int amount);
        DebugValueResult AddTechPoint(
            IManifest owner,
            string pointTypeId,
            int amount);
        DebugCommandResult UnlockAllTechTrees(IManifest owner);
        CropMaturityResult MatureAllCrops(IManifest owner);
        CreativeModeResult SetCreativeMode(
            IManifest owner,
            bool enabled);
        SpawnActionResult SpawnMonster(
            IManifest owner,
            string monsterId,
            int count);
        SpawnActionResult SpawnAnimal(
            IManifest owner,
            string cardId,
            int count);
        BridgeFeatureStatus GetStatus();
    }
}
