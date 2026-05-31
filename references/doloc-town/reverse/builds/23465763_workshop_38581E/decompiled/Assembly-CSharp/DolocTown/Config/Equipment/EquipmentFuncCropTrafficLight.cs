using System;
using System.Collections.Generic;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncCropTrafficLight : EquipmentFuncEquipment
{
	public const int __ID__ = 1874742459;

	public SpriteAsset MaskSprite { get; private set; }

	public EquipmentFuncCropTrafficLight(JSONNode _json)
		: base(_json)
	{
		if (!_json["mask_sprite"].IsObject)
		{
			throw new SerializationException();
		}
		MaskSprite = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["mask_sprite"]));
	}

	public EquipmentFuncCropTrafficLight(SpriteAsset mask_sprite)
	{
		MaskSprite = mask_sprite;
	}

	public static EquipmentFuncCropTrafficLight DeserializeEquipmentFuncCropTrafficLight(JSONNode _json)
	{
		return new EquipmentFuncCropTrafficLight(_json);
	}

	public override int GetTypeId()
	{
		return 1874742459;
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
		return "{ MaskSprite:" + MaskSprite?.ToString() + ",}";
	}
}
