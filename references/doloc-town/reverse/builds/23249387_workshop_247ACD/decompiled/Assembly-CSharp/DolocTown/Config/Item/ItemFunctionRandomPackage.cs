using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ItemFunctionRandomPackage : ItemFunctionBase
{
	public const int __ID__ = 1308865693;

	public ItemSpawnEntry DropSpawnEntry { get; private set; }

	public ItemFunctionRandomPackage(JSONNode _json)
		: base(_json)
	{
		if (!_json["drop_spawn_entry"].IsObject)
		{
			throw new SerializationException();
		}
		DropSpawnEntry = ItemSpawnEntry.DeserializeItemSpawnEntry(_json["drop_spawn_entry"]);
	}

	public ItemFunctionRandomPackage(ItemSpawnEntry drop_spawn_entry)
	{
		DropSpawnEntry = drop_spawn_entry;
	}

	public static ItemFunctionRandomPackage DeserializeItemFunctionRandomPackage(JSONNode _json)
	{
		return new ItemFunctionRandomPackage(_json);
	}

	public override int GetTypeId()
	{
		return 1308865693;
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
		DropSpawnEntry?.Resolve(_tables);
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
		DropSpawnEntry?.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ DropSpawnEntry:" + DropSpawnEntry?.ToString() + ",}";
	}
}
