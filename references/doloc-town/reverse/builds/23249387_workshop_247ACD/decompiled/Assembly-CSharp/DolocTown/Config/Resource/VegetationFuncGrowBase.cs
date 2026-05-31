using System;
using System.Collections.Generic;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Resource;

public abstract class VegetationFuncGrowBase : VegetationFuncBase
{
	public Vector2Int GrowthValue { get; private set; }

	public SpriteAssetArray LevelSprites { get; private set; }

	public VegetationFuncGrowBase(JSONNode _json)
		: base(_json)
	{
		if (!_json["growth_value"].IsObject)
		{
			throw new SerializationException();
		}
		GrowthValue = ExternalTypeUtil.Vector2IntConverter(CfgVector2Int.DeserializeCfgVector2Int(_json["growth_value"]));
		if (!_json["level_sprites"].IsObject)
		{
			throw new SerializationException();
		}
		LevelSprites = ExternalTypeUtil.SpriteAssetArrayConverter(CfgSpriteAssetArray.DeserializeCfgSpriteAssetArray(_json["level_sprites"]));
	}

	public VegetationFuncGrowBase(Vector2Int growth_value, SpriteAssetArray level_sprites)
	{
		GrowthValue = growth_value;
		LevelSprites = level_sprites;
	}

	public static VegetationFuncGrowBase DeserializeVegetationFuncGrowBase(JSONNode _json)
	{
		return (string)_json["$type"] switch
		{
			"VegetationFuncBerryThicket" => new VegetationFuncBerryThicket(_json), 
			"VegetationFuncCrop" => new VegetationFuncCrop(_json), 
			"VegetationFuncGrowLuminous" => new VegetationFuncGrowLuminous(_json), 
			"VegetationFuncGrow" => new VegetationFuncGrow(_json), 
			"VegetationFuncDandelion" => new VegetationFuncDandelion(_json), 
			_ => throw new SerializationException(), 
		};
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ GrowthValue:" + GrowthValue.ToString() + ",LevelSprites:" + LevelSprites?.ToString() + ",}";
	}
}
