using System;
using System.Collections.Generic;
using Bright.Serialization;
using DolocTown.Config.Asset;
using DolocTown.Config.Plant;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Equipment;

public abstract class EquipmentFuncPlantBasinBase : EquipmentFuncEquipment
{
	public string SeedType { get; private set; }

	public SeedTypeInfo SeedType_Ref { get; private set; }

	public Vector2Int SupplyCapacity { get; private set; }

	public Vector2 CenterOffset { get; private set; }

	public SpriteAsset SpriteWetAsset { get; private set; }

	public EquipmentFuncPlantBasinBase(JSONNode _json)
		: base(_json)
	{
		if (!_json["seed_type"].IsString)
		{
			throw new SerializationException();
		}
		SeedType = _json["seed_type"];
		if (!_json["supply_capacity"].IsObject)
		{
			throw new SerializationException();
		}
		SupplyCapacity = ExternalTypeUtil.Vector2IntConverter(CfgVector2Int.DeserializeCfgVector2Int(_json["supply_capacity"]));
		JSONNode jSONNode = _json["center_offset"];
		if (!jSONNode.IsObject)
		{
			throw new SerializationException();
		}
		if (!jSONNode["x"].IsNumber)
		{
			throw new SerializationException();
		}
		float x = jSONNode["x"];
		if (!jSONNode["y"].IsNumber)
		{
			throw new SerializationException();
		}
		float y = jSONNode["y"];
		CenterOffset = new Vector2(x, y);
		if (!_json["sprite_wet_asset"].IsObject)
		{
			throw new SerializationException();
		}
		SpriteWetAsset = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["sprite_wet_asset"]));
	}

	public EquipmentFuncPlantBasinBase(string seed_type, Vector2Int supply_capacity, Vector2 center_offset, SpriteAsset sprite_wet_asset)
	{
		SeedType = seed_type;
		SupplyCapacity = supply_capacity;
		CenterOffset = center_offset;
		SpriteWetAsset = sprite_wet_asset;
	}

	public static EquipmentFuncPlantBasinBase DeserializeEquipmentFuncPlantBasinBase(JSONNode _json)
	{
		string text = _json["$type"];
		if (!(text == "EquipmentFuncPlantBasin"))
		{
			if (text == "EquipmentFuncPlantBasinSimple")
			{
				return new EquipmentFuncPlantBasinSimple(_json);
			}
			throw new SerializationException();
		}
		return new EquipmentFuncPlantBasin(_json);
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
		SeedType_Ref = (_tables["Plant.TbSeedType"] as TbSeedType).GetOrDefault(SeedType);
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ SeedType:" + SeedType + ",SupplyCapacity:" + SupplyCapacity.ToString() + ",CenterOffset:" + CenterOffset.ToString() + ",SpriteWetAsset:" + SpriteWetAsset?.ToString() + ",}";
	}
}
