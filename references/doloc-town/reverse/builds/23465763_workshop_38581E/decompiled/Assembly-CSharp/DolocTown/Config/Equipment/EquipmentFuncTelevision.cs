using System;
using System.Collections.Generic;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncTelevision : EquipmentFuncEquipment
{
	public const int __ID__ = 657232516;

	public SpriteAsset ScreenMask { get; private set; }

	public SpriteAsset ScreenSprite { get; private set; }

	public EquipmentFuncTelevision(JSONNode _json)
		: base(_json)
	{
		if (!_json["screen_mask"].IsObject)
		{
			throw new SerializationException();
		}
		ScreenMask = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["screen_mask"]));
		if (!_json["screen_sprite"].IsObject)
		{
			throw new SerializationException();
		}
		ScreenSprite = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["screen_sprite"]));
	}

	public EquipmentFuncTelevision(SpriteAsset screen_mask, SpriteAsset screen_sprite)
	{
		ScreenMask = screen_mask;
		ScreenSprite = screen_sprite;
	}

	public static EquipmentFuncTelevision DeserializeEquipmentFuncTelevision(JSONNode _json)
	{
		return new EquipmentFuncTelevision(_json);
	}

	public override int GetTypeId()
	{
		return 657232516;
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
		return "{ ScreenMask:" + ScreenMask?.ToString() + ",ScreenSprite:" + ScreenSprite?.ToString() + ",}";
	}
}
