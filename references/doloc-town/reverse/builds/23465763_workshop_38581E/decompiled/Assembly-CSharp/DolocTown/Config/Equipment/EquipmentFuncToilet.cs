using System;
using System.Collections.Generic;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncToilet : EquipmentFuncEquipment
{
	public const int __ID__ = 433756543;

	public int Capacity { get; private set; }

	public SpriteAsset FullSprite { get; private set; }

	public EquipmentFuncToilet(JSONNode _json)
		: base(_json)
	{
		if (!_json["capacity"].IsNumber)
		{
			throw new SerializationException();
		}
		Capacity = _json["capacity"];
		if (!_json["full_sprite"].IsObject)
		{
			throw new SerializationException();
		}
		FullSprite = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["full_sprite"]));
	}

	public EquipmentFuncToilet(int capacity, SpriteAsset full_sprite)
	{
		Capacity = capacity;
		FullSprite = full_sprite;
	}

	public static EquipmentFuncToilet DeserializeEquipmentFuncToilet(JSONNode _json)
	{
		return new EquipmentFuncToilet(_json);
	}

	public override int GetTypeId()
	{
		return 433756543;
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
		return "{ Capacity:" + Capacity + ",FullSprite:" + FullSprite?.ToString() + ",}";
	}
}
