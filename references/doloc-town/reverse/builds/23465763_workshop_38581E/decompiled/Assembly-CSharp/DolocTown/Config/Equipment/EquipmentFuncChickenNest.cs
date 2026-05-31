using System;
using System.Collections.Generic;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;

namespace DolocTown.Config.Equipment;

public sealed class EquipmentFuncChickenNest : EquipmentFuncEquipment
{
	public const int __ID__ = -562578053;

	public int Capacity { get; private set; }

	public SpriteAssetArray Sprites { get; private set; }

	public EquipmentFuncChickenNest(JSONNode _json)
		: base(_json)
	{
		if (!_json["capacity"].IsNumber)
		{
			throw new SerializationException();
		}
		Capacity = _json["capacity"];
		if (!_json["sprites"].IsObject)
		{
			throw new SerializationException();
		}
		Sprites = ExternalTypeUtil.SpriteAssetArrayConverter(CfgSpriteAssetArray.DeserializeCfgSpriteAssetArray(_json["sprites"]));
	}

	public EquipmentFuncChickenNest(int capacity, SpriteAssetArray sprites)
	{
		Capacity = capacity;
		Sprites = sprites;
	}

	public static EquipmentFuncChickenNest DeserializeEquipmentFuncChickenNest(JSONNode _json)
	{
		return new EquipmentFuncChickenNest(_json);
	}

	public override int GetTypeId()
	{
		return -562578053;
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
		return "{ Capacity:" + Capacity + ",Sprites:" + Sprites?.ToString() + ",}";
	}
}
