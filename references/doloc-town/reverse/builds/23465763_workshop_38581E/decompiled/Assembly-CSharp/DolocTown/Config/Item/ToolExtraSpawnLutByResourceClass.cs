using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Resource;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ToolExtraSpawnLutByResourceClass : BeanBase
{
	public const int __ID__ = 1819256134;

	public DungeonResourceClass TargetResourceClass { get; private set; }

	public ItemSpawnRandom SpawnEntry { get; private set; }

	public int[] TargetLevels { get; private set; }

	public ToolExtraSpawnLutByResourceClass(JSONNode _json)
	{
		if (!_json["target_resource_class"].IsNumber)
		{
			throw new SerializationException();
		}
		TargetResourceClass = (DungeonResourceClass)_json["target_resource_class"].AsInt;
		if (!_json["spawn_entry"].IsObject)
		{
			throw new SerializationException();
		}
		SpawnEntry = ItemSpawnRandom.DeserializeItemSpawnRandom(_json["spawn_entry"]);
		JSONNode jSONNode = _json["target_levels"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		TargetLevels = new int[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsNumber)
			{
				throw new SerializationException();
			}
			int num2 = child;
			TargetLevels[num++] = num2;
		}
	}

	public ToolExtraSpawnLutByResourceClass(DungeonResourceClass target_resource_class, ItemSpawnRandom spawn_entry, int[] target_levels)
	{
		TargetResourceClass = target_resource_class;
		SpawnEntry = spawn_entry;
		TargetLevels = target_levels;
	}

	public static ToolExtraSpawnLutByResourceClass DeserializeToolExtraSpawnLutByResourceClass(JSONNode _json)
	{
		return new ToolExtraSpawnLutByResourceClass(_json);
	}

	public override int GetTypeId()
	{
		return 1819256134;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		SpawnEntry?.Resolve(_tables);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		SpawnEntry?.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ TargetResourceClass:" + TargetResourceClass.ToString() + ",SpawnEntry:" + SpawnEntry?.ToString() + ",TargetLevels:" + StringUtil.CollectionToString(TargetLevels) + ",}";
	}
}
