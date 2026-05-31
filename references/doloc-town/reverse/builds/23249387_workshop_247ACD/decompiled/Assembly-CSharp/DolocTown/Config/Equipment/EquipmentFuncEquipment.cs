using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public abstract class EquipmentFuncEquipment : BeanBase
{
	public EquipmentFuncEquipment(JSONNode _json)
	{
	}

	public EquipmentFuncEquipment()
	{
	}

	public static EquipmentFuncEquipment DeserializeEquipmentFuncEquipment(JSONNode _json)
	{
		return (string)_json["$type"] switch
		{
			"EquipmentFuncDecorator" => new EquipmentFuncDecorator(_json), 
			"EquipmentFuncBarrel" => new EquipmentFuncBarrel(_json), 
			"EquipmentFuncResinCollector" => new EquipmentFuncResinCollector(_json), 
			"EquipmentFuncSimpleWell" => new EquipmentFuncSimpleWell(_json), 
			"EquipmentFuncMalignantWeatherSuppressor" => new EquipmentFuncMalignantWeatherSuppressor(_json), 
			"EquipmentFuncPlantBasin" => new EquipmentFuncPlantBasin(_json), 
			"EquipmentFuncPlantBasinSimple" => new EquipmentFuncPlantBasinSimple(_json), 
			"EquipmentFuncPlantBasinTree" => new EquipmentFuncPlantBasinTree(_json), 
			"EquipmentFuncSynthesizerGenerator" => new EquipmentFuncSynthesizerGenerator(_json), 
			"EquipmentFuncSynthesizer" => new EquipmentFuncSynthesizer(_json), 
			"EquipmentFuncSeedCompressor" => new EquipmentFuncSeedCompressor(_json), 
			"EquipmentFuncGeneExtractor" => new EquipmentFuncGeneExtractor(_json), 
			"EquipmentFuncGeneIncubator" => new EquipmentFuncGeneIncubator(_json), 
			"EquipmentFuncGeneReplicator" => new EquipmentFuncGeneReplicator(_json), 
			"EquipmentFuncGeneSynthesizer" => new EquipmentFuncGeneSynthesizer(_json), 
			"EquipmentFuncGarbageShredder" => new EquipmentFuncGarbageShredder(_json), 
			"EquipmentFuncWorkbench" => new EquipmentFuncWorkbench(_json), 
			"EquipmentFuncEquipmentWorkbench" => new EquipmentFuncEquipmentWorkbench(_json), 
			"EquipmentFuncSprinkler" => new EquipmentFuncSprinkler(_json), 
			"EquipmentFuncSprinklerManual" => new EquipmentFuncSprinklerManual(_json), 
			"EquipmentFuncFarmLight" => new EquipmentFuncFarmLight(_json), 
			"EquipmentFuncLightningArrester" => new EquipmentFuncLightningArrester(_json), 
			"EquipmentFuncAutomateBotStation" => new EquipmentFuncAutomateBotStation(_json), 
			"EquipmentFuncCase" => new EquipmentFuncCase(_json), 
			"EquipmentFuncStorageShelf" => new EquipmentFuncStorageShelf(_json), 
			"EquipmentFuncParkingApron" => new EquipmentFuncParkingApron(_json), 
			"EquipmentFuncPlantAnalyzer" => new EquipmentFuncPlantAnalyzer(_json), 
			"EquipmentFuncFishTank" => new EquipmentFuncFishTank(_json), 
			"EquipmentFuncFishTankEcological" => new EquipmentFuncFishTankEcological(_json), 
			"EquipmentFuncFishTankElectricEel" => new EquipmentFuncFishTankElectricEel(_json), 
			"EquipmentFuncFishIncubator" => new EquipmentFuncFishIncubator(_json), 
			"EquipmentFuncCaseLocator" => new EquipmentFuncCaseLocator(_json), 
			"EquipmentFuncPowerGeneratorFuel" => new EquipmentFuncPowerGeneratorFuel(_json), 
			"EquipmentFuncPowerGeneratorWind" => new EquipmentFuncPowerGeneratorWind(_json), 
			"EquipmentFuncPowerGeneratorBiogas" => new EquipmentFuncPowerGeneratorBiogas(_json), 
			"EquipmentFuncBattery" => new EquipmentFuncBattery(_json), 
			"EquipmentFuncBed" => new EquipmentFuncBed(_json), 
			"EquipmentFuncEquipmentRoom" => new EquipmentFuncEquipmentRoom(_json), 
			"EquipmentFuncLamp" => new EquipmentFuncLamp(_json), 
			"EquipmentFuncMailBox" => new EquipmentFuncMailBox(_json), 
			"EquipmentFuncChair" => new EquipmentFuncChair(_json), 
			"EquipmentFuncTrampolineEquipment" => new EquipmentFuncTrampolineEquipment(_json), 
			"EquipmentFuncShowCase" => new EquipmentFuncShowCase(_json), 
			"EquipmentFuncShowerRoom" => new EquipmentFuncShowerRoom(_json), 
			"EquipmentFuncFlowerPot" => new EquipmentFuncFlowerPot(_json), 
			"EquipmentFuncWeatherRegulator" => new EquipmentFuncWeatherRegulator(_json), 
			"EquipmentFuncTelevision" => new EquipmentFuncTelevision(_json), 
			"EquipmentFuncFeeder" => new EquipmentFuncFeeder(_json), 
			"EquipmentFuncAnimalStation" => new EquipmentFuncAnimalStation(_json), 
			"EquipmentFuncToilet" => new EquipmentFuncToilet(_json), 
			"EquipmentFuncLivestockNursery" => new EquipmentFuncLivestockNursery(_json), 
			"EquipmentFuncChickenNest" => new EquipmentFuncChickenNest(_json), 
			"EquipmentFuncLintRoller" => new EquipmentFuncLintRoller(_json), 
			"EquipmentFuncMilkingMachine" => new EquipmentFuncMilkingMachine(_json), 
			"EquipmentFuncFence" => new EquipmentFuncFence(_json), 
			"EquipmentFuncAnimalDecorator" => new EquipmentFuncAnimalDecorator(_json), 
			"EquipmentFuncFishTankExtension" => new EquipmentFuncFishTankExtension(_json), 
			"EquipmentFuncHoneyComb" => new EquipmentFuncHoneyComb(_json), 
			"EquipmentFuncPlantBasinGrass" => new EquipmentFuncPlantBasinGrass(_json), 
			"EquipmentFuncCropTrafficLight" => new EquipmentFuncCropTrafficLight(_json), 
			"EquipmentFuncEquipmentAnimation" => new EquipmentFuncEquipmentAnimation(_json), 
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
