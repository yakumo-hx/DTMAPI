using System;
using System.Collections.Generic;
using Bright.Serialization;
using DolocTown.Config.Plant;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ItemFunctionSeedTree : ItemFunctionBase
{
	public const int __ID__ = 1312603861;

	public string TreeSeedId { get; private set; }

	public TreeSeedInfo TreeSeedId_Ref { get; private set; }

	public ItemFunctionSeedTree(JSONNode _json)
		: base(_json)
	{
		if (!_json["tree_seed_id"].IsString)
		{
			throw new SerializationException();
		}
		TreeSeedId = _json["tree_seed_id"];
	}

	public ItemFunctionSeedTree(string tree_seed_id)
	{
		TreeSeedId = tree_seed_id;
	}

	public static ItemFunctionSeedTree DeserializeItemFunctionSeedTree(JSONNode _json)
	{
		return new ItemFunctionSeedTree(_json);
	}

	public override int GetTypeId()
	{
		return 1312603861;
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
		TreeSeedId_Ref = (_tables["Plant.TbTreeSeed"] as TbTreeSeed).GetOrDefault(TreeSeedId);
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ TreeSeedId:" + TreeSeedId + ",}";
	}
}
