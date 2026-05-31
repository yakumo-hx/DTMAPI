using System;
using System.Collections.Generic;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ItemFunctionFertilizer : ItemFunctionBase
{
	public const int __ID__ = 44302184;

	public float Addition { get; private set; }

	public int Duration { get; private set; }

	public bool IsTree { get; private set; }

	public SpriteAsset FertilizerSprite { get; private set; }

	public ItemFunctionFertilizer(JSONNode _json)
		: base(_json)
	{
		if (!_json["addition"].IsNumber)
		{
			throw new SerializationException();
		}
		Addition = _json["addition"];
		if (!_json["duration"].IsNumber)
		{
			throw new SerializationException();
		}
		Duration = _json["duration"];
		if (!_json["is_tree"].IsBoolean)
		{
			throw new SerializationException();
		}
		IsTree = _json["is_tree"];
		if (!_json["fertilizer_sprite"].IsObject)
		{
			throw new SerializationException();
		}
		FertilizerSprite = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["fertilizer_sprite"]));
	}

	public ItemFunctionFertilizer(float addition, int duration, bool is_tree, SpriteAsset fertilizer_sprite)
	{
		Addition = addition;
		Duration = duration;
		IsTree = is_tree;
		FertilizerSprite = fertilizer_sprite;
	}

	public static ItemFunctionFertilizer DeserializeItemFunctionFertilizer(JSONNode _json)
	{
		return new ItemFunctionFertilizer(_json);
	}

	public override int GetTypeId()
	{
		return 44302184;
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
		return "{ Addition:" + Addition + ",Duration:" + Duration + ",IsTree:" + IsTree + ",FertilizerSprite:" + FertilizerSprite?.ToString() + ",}";
	}
}
