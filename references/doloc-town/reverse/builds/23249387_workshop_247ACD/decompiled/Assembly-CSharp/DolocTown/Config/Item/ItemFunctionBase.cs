using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Item;

public abstract class ItemFunctionBase : BeanBase
{
	public ItemFunctionBase(JSONNode _json)
	{
	}

	public ItemFunctionBase()
	{
	}

	public static ItemFunctionBase DeserializeItemFunctionBase(JSONNode _json)
	{
		return (string)_json["$type"] switch
		{
			"ItemFunction" => new ItemFunction(_json), 
			"ItemFunctionTool" => new ItemFunctionTool(_json), 
			"ItemFunctionFishingRod" => new ItemFunctionFishingRod(_json), 
			"ItemFunctionWaterCan" => new ItemFunctionWaterCan(_json), 
			"ItemFunctionFarmingGun" => new ItemFunctionFarmingGun(_json), 
			"ItemFunctionSeed" => new ItemFunctionSeed(_json), 
			"ItemFunctionCrop" => new ItemFunctionCrop(_json), 
			"ItemFunctionGeneCapsule" => new ItemFunctionGeneCapsule(_json), 
			"ItemFunctionSeedMaternal" => new ItemFunctionSeedMaternal(_json), 
			"ItemFunctionSeedTree" => new ItemFunctionSeedTree(_json), 
			"ItemFunctionRecipe" => new ItemFunctionRecipe(_json), 
			"ItemFunctionRecipeGroup" => new ItemFunctionRecipeGroup(_json), 
			"ItemFunctionFood" => new ItemFunctionFood(_json), 
			"ItemFunctionBox" => new ItemFunctionBox(_json), 
			"ItemFunctionPatch" => new ItemFunctionPatch(_json), 
			"ItemFunctionFilm" => new ItemFunctionFilm(_json), 
			"ItemFunctionFertilizer" => new ItemFunctionFertilizer(_json), 
			"ItemFunctionBuilding" => new ItemFunctionBuilding(_json), 
			"ItemFunctionDroneStructure" => new ItemFunctionDroneStructure(_json), 
			"ItemFunctionDroneWeapon" => new ItemFunctionDroneWeapon(_json), 
			"ItemFunctionDroneEngine" => new ItemFunctionDroneEngine(_json), 
			"ItemFunctionDroneChip" => new ItemFunctionDroneChip(_json), 
			"ItemFunctionDroneAssist" => new ItemFunctionDroneAssist(_json), 
			"ItemFunctionEquipment" => new ItemFunctionEquipment(_json), 
			"ItemFunctionAutomateBot" => new ItemFunctionAutomateBot(_json), 
			"ItemFunctionAnimation" => new ItemFunctionAnimation(_json), 
			"ItemFunctionSleepingBag" => new ItemFunctionSleepingBag(_json), 
			"ItemFunctionRescuePager" => new ItemFunctionRescuePager(_json), 
			"ItemFunctionEdenFruit" => new ItemFunctionEdenFruit(_json), 
			"ItemFunctionPlatform" => new ItemFunctionPlatform(_json), 
			"ItemFunctionMissile" => new ItemFunctionMissile(_json), 
			"ItemFunctionDemolitionTool" => new ItemFunctionDemolitionTool(_json), 
			"ItemFunctionDoorplate" => new ItemFunctionDoorplate(_json), 
			"ItemFunctionHat" => new ItemFunctionHat(_json), 
			"ItemFunctionHatShield" => new ItemFunctionHatShield(_json), 
			"ItemFunctionPassive" => new ItemFunctionPassive(_json), 
			"ItemFunctionHerbPackage" => new ItemFunctionHerbPackage(_json), 
			"ItemFunctionRandomPackage" => new ItemFunctionRandomPackage(_json), 
			"ItemFunctionBattery" => new ItemFunctionBattery(_json), 
			"ItemFunctionBottle" => new ItemFunctionBottle(_json), 
			"ItemFunctionMotorKey" => new ItemFunctionMotorKey(_json), 
			"ItemFunctionConstructController" => new ItemFunctionConstructController(_json), 
			"ItemFunctionAnimalPackage" => new ItemFunctionAnimalPackage(_json), 
			"ItemFunctionFishRoe" => new ItemFunctionFishRoe(_json), 
			"ItemFunctionFishFry" => new ItemFunctionFishFry(_json), 
			"ItemFunctionWallpaper" => new ItemFunctionWallpaper(_json), 
			"ItemFunctionBuildingExterior" => new ItemFunctionBuildingExterior(_json), 
			_ => throw new SerializationException(), 
		};
	}

	public virtual void Resolve(Dictionary<string, object> _tables)
	{
	}

	public virtual void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ }";
	}
}
