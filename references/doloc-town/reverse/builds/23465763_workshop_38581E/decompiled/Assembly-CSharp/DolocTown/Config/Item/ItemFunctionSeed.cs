using System;
using System.Collections.Generic;
using Bright.Serialization;
using DolocTown.Config.Plant;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ItemFunctionSeed : ItemFunctionBase
{
	public const int __ID__ = -1823343977;

	public string SeedId { get; private set; }

	public SeedInfo SeedId_Ref { get; private set; }

	public ItemFunctionSeed(JSONNode _json)
		: base(_json)
	{
		if (!_json["seed_id"].IsString)
		{
			throw new SerializationException();
		}
		SeedId = _json["seed_id"];
	}

	public ItemFunctionSeed(string seed_id)
	{
		SeedId = seed_id;
	}

	public static ItemFunctionSeed DeserializeItemFunctionSeed(JSONNode _json)
	{
		return new ItemFunctionSeed(_json);
	}

	public override int GetTypeId()
	{
		return -1823343977;
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
		SeedId_Ref = (_tables["Plant.TbSeed"] as TbSeed).GetOrDefault(SeedId);
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ SeedId:" + SeedId + ",}";
	}
}
