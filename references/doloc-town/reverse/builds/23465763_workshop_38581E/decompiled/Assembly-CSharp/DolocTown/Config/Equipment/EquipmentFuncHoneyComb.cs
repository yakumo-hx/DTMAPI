using System;
using System.Collections.Generic;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncHoneyComb : EquipmentFuncEquipment
{
	public const int __ID__ = -1341536214;

	public int Capacity { get; private set; }

	public SpriteAsset HalfSprite { get; private set; }

	public SpriteAsset FullSprite { get; private set; }

	public EquipmentFuncHoneyComb(JSONNode _json)
		: base(_json)
	{
		if (!_json["capacity"].IsNumber)
		{
			throw new SerializationException();
		}
		Capacity = _json["capacity"];
		if (!_json["half_sprite"].IsObject)
		{
			throw new SerializationException();
		}
		HalfSprite = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["half_sprite"]));
		if (!_json["full_sprite"].IsObject)
		{
			throw new SerializationException();
		}
		FullSprite = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["full_sprite"]));
	}

	public EquipmentFuncHoneyComb(int capacity, SpriteAsset half_sprite, SpriteAsset full_sprite)
	{
		Capacity = capacity;
		HalfSprite = half_sprite;
		FullSprite = full_sprite;
	}

	public static EquipmentFuncHoneyComb DeserializeEquipmentFuncHoneyComb(JSONNode _json)
	{
		return new EquipmentFuncHoneyComb(_json);
	}

	public override int GetTypeId()
	{
		return -1341536214;
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
		return "{ Capacity:" + Capacity + ",HalfSprite:" + HalfSprite?.ToString() + ",FullSprite:" + FullSprite?.ToString() + ",}";
	}
}
