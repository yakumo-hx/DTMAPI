using System;
using System.Collections.Generic;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncFishIncubator : EquipmentFuncCaseBase
{
	public const int __ID__ = 2069960599;

	public int WorkInterval { get; private set; }

	public SpriteAsset MaskSprite { get; private set; }

	public SpriteAsset EmissionSprite { get; private set; }

	public EquipmentFuncFishIncubator(JSONNode _json)
		: base(_json)
	{
		if (!_json["work_interval"].IsNumber)
		{
			throw new SerializationException();
		}
		WorkInterval = _json["work_interval"];
		if (!_json["mask_sprite"].IsObject)
		{
			throw new SerializationException();
		}
		MaskSprite = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["mask_sprite"]));
		if (!_json["emission_sprite"].IsObject)
		{
			throw new SerializationException();
		}
		EmissionSprite = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["emission_sprite"]));
	}

	public EquipmentFuncFishIncubator(int total_capacity, int line_capacity, int work_interval, SpriteAsset mask_sprite, SpriteAsset emission_sprite)
		: base(total_capacity, line_capacity)
	{
		WorkInterval = work_interval;
		MaskSprite = mask_sprite;
		EmissionSprite = emission_sprite;
	}

	public static EquipmentFuncFishIncubator DeserializeEquipmentFuncFishIncubator(JSONNode _json)
	{
		return new EquipmentFuncFishIncubator(_json);
	}

	public override int GetTypeId()
	{
		return 2069960599;
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
		return "{ TotalCapacity:" + base.TotalCapacity + ",LineCapacity:" + base.LineCapacity + ",WorkInterval:" + WorkInterval + ",MaskSprite:" + MaskSprite?.ToString() + ",EmissionSprite:" + EmissionSprite?.ToString() + ",}";
	}
}
