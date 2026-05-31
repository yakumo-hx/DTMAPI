using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ItemFunctionCrop : ItemFunctionBase
{
	public const int __ID__ = -1823807818;

	public string SeedItem { get; private set; }

	public ItemInfo SeedItem_Ref { get; private set; }

	public string EatingEffect { get; private set; }

	public EatingEffectInfo EatingEffect_Ref { get; private set; }

	public ItemFunctionCrop(JSONNode _json)
		: base(_json)
	{
		if (!_json["seed_item"].IsString)
		{
			throw new SerializationException();
		}
		SeedItem = _json["seed_item"];
		if (!_json["eating_effect"].IsString)
		{
			throw new SerializationException();
		}
		EatingEffect = _json["eating_effect"];
	}

	public ItemFunctionCrop(string seed_item, string eating_effect)
	{
		SeedItem = seed_item;
		EatingEffect = eating_effect;
	}

	public static ItemFunctionCrop DeserializeItemFunctionCrop(JSONNode _json)
	{
		return new ItemFunctionCrop(_json);
	}

	public override int GetTypeId()
	{
		return -1823807818;
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
		SeedItem_Ref = (_tables["Item.TbItem"] as TbItem).GetOrDefault(SeedItem);
		EatingEffect_Ref = (_tables["Item.TbEatingEffect"] as TbEatingEffect).GetOrDefault(EatingEffect);
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ SeedItem:" + SeedItem + ",EatingEffect:" + EatingEffect + ",}";
	}
}
