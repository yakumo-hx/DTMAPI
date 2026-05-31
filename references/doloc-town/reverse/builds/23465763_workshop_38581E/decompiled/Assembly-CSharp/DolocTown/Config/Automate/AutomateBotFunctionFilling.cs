using System;
using System.Collections.Generic;
using Bright.Serialization;
using DolocTown.Config.Item;
using SimpleJSON;

namespace DolocTown.Config.Automate;

public sealed class AutomateBotFunctionFilling : AutomateBotFunction
{
	public const int __ID__ = -1709760242;

	public string DefaultFuelType { get; private set; }

	public ItemSubTypeInfo DefaultFuelType_Ref { get; private set; }

	public float FuelThreshold { get; private set; }

	public string DefaultAnimalFeedsType { get; private set; }

	public ItemSubTypeInfo DefaultAnimalFeedsType_Ref { get; private set; }

	public float AnimalFeedsThreshold { get; private set; }

	public string DefaultFishFeedsType { get; private set; }

	public ItemSubTypeInfo DefaultFishFeedsType_Ref { get; private set; }

	public float FishFeedsThreshold { get; private set; }

	public AutomateBotFunctionFilling(JSONNode _json)
		: base(_json)
	{
		if (!_json["default_fuel_type"].IsString)
		{
			throw new SerializationException();
		}
		DefaultFuelType = _json["default_fuel_type"];
		if (!_json["fuel_threshold"].IsNumber)
		{
			throw new SerializationException();
		}
		FuelThreshold = _json["fuel_threshold"];
		if (!_json["default_animal_feeds_type"].IsString)
		{
			throw new SerializationException();
		}
		DefaultAnimalFeedsType = _json["default_animal_feeds_type"];
		if (!_json["animal_feeds_threshold"].IsNumber)
		{
			throw new SerializationException();
		}
		AnimalFeedsThreshold = _json["animal_feeds_threshold"];
		if (!_json["default_fish_feeds_type"].IsString)
		{
			throw new SerializationException();
		}
		DefaultFishFeedsType = _json["default_fish_feeds_type"];
		if (!_json["fish_feeds_threshold"].IsNumber)
		{
			throw new SerializationException();
		}
		FishFeedsThreshold = _json["fish_feeds_threshold"];
	}

	public AutomateBotFunctionFilling(string default_fuel_type, float fuel_threshold, string default_animal_feeds_type, float animal_feeds_threshold, string default_fish_feeds_type, float fish_feeds_threshold)
	{
		DefaultFuelType = default_fuel_type;
		FuelThreshold = fuel_threshold;
		DefaultAnimalFeedsType = default_animal_feeds_type;
		AnimalFeedsThreshold = animal_feeds_threshold;
		DefaultFishFeedsType = default_fish_feeds_type;
		FishFeedsThreshold = fish_feeds_threshold;
	}

	public static AutomateBotFunctionFilling DeserializeAutomateBotFunctionFilling(JSONNode _json)
	{
		return new AutomateBotFunctionFilling(_json);
	}

	public override int GetTypeId()
	{
		return -1709760242;
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
		DefaultFuelType_Ref = (_tables["Item.TbItemSubType"] as TbItemSubType).GetOrDefault(DefaultFuelType);
		DefaultAnimalFeedsType_Ref = (_tables["Item.TbItemSubType"] as TbItemSubType).GetOrDefault(DefaultAnimalFeedsType);
		DefaultFishFeedsType_Ref = (_tables["Item.TbItemSubType"] as TbItemSubType).GetOrDefault(DefaultFishFeedsType);
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ DefaultFuelType:" + DefaultFuelType + ",FuelThreshold:" + FuelThreshold + ",DefaultAnimalFeedsType:" + DefaultAnimalFeedsType + ",AnimalFeedsThreshold:" + AnimalFeedsThreshold + ",DefaultFishFeedsType:" + DefaultFishFeedsType + ",FishFeedsThreshold:" + FishFeedsThreshold + ",}";
	}
}
