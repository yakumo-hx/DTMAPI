using System;
using System.Collections.Generic;
using Bright.Serialization;
using DolocTown.Config.Global;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ItemSpawnData : SpawnData
{
	public const int __ID__ = 334483543;

	public string ItemName { get; private set; }

	public ItemInfo ItemName_Ref { get; private set; }

	public override string SpawnId => ItemName;

	public ItemSpawnData(JSONNode _json)
		: base(_json)
	{
		if (!_json["item_name"].IsString)
		{
			throw new SerializationException();
		}
		ItemName = _json["item_name"];
	}

	public ItemSpawnData(float spawn_weight, int min_count, int max_count, string item_name)
		: base(spawn_weight, min_count, max_count)
	{
		ItemName = item_name;
	}

	public static ItemSpawnData DeserializeItemSpawnData(JSONNode _json)
	{
		return new ItemSpawnData(_json);
	}

	public override int GetTypeId()
	{
		return 334483543;
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
		ItemName_Ref = (_tables["Item.TbItem"] as TbItem).GetOrDefault(ItemName);
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ SpawnWeight:" + base.SpawnWeight + ",MinCount:" + base.MinCount + ",MaxCount:" + base.MaxCount + ",ItemName:" + ItemName + ",}";
	}
}
