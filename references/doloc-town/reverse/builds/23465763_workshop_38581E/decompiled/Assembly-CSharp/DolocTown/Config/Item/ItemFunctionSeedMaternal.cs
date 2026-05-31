using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ItemFunctionSeedMaternal : ItemFunctionBase
{
	public const int __ID__ = 281235363;

	public string SeedItemId { get; private set; }

	public ItemInfo SeedItemId_Ref { get; private set; }

	public ItemFunctionSeedMaternal(JSONNode _json)
		: base(_json)
	{
		if (!_json["seed_item_id"].IsString)
		{
			throw new SerializationException();
		}
		SeedItemId = _json["seed_item_id"];
	}

	public ItemFunctionSeedMaternal(string seed_item_id)
	{
		SeedItemId = seed_item_id;
	}

	public static ItemFunctionSeedMaternal DeserializeItemFunctionSeedMaternal(JSONNode _json)
	{
		return new ItemFunctionSeedMaternal(_json);
	}

	public override int GetTypeId()
	{
		return 281235363;
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
		SeedItemId_Ref = (_tables["Item.TbItem"] as TbItem).GetOrDefault(SeedItemId);
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ SeedItemId:" + SeedItemId + ",}";
	}
}
