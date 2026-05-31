using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Resource;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ToolExtraSpawnLutByResourceName : BeanBase
{
	public const int __ID__ = -1049375491;

	public string TargetResourceName { get; private set; }

	public ResourceInfo TargetResourceName_Ref { get; private set; }

	public ItemSpawnRandom SpawnEntry { get; private set; }

	public int[] TargetLevels { get; private set; }

	public ToolExtraSpawnLutByResourceName(JSONNode _json)
	{
		if (!_json["target_resource_name"].IsString)
		{
			throw new SerializationException();
		}
		TargetResourceName = _json["target_resource_name"];
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

	public ToolExtraSpawnLutByResourceName(string target_resource_name, ItemSpawnRandom spawn_entry, int[] target_levels)
	{
		TargetResourceName = target_resource_name;
		SpawnEntry = spawn_entry;
		TargetLevels = target_levels;
	}

	public static ToolExtraSpawnLutByResourceName DeserializeToolExtraSpawnLutByResourceName(JSONNode _json)
	{
		return new ToolExtraSpawnLutByResourceName(_json);
	}

	public override int GetTypeId()
	{
		return -1049375491;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		TargetResourceName_Ref = (_tables["Resource.TbResource"] as TbResource).GetOrDefault(TargetResourceName);
		SpawnEntry?.Resolve(_tables);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		SpawnEntry?.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ TargetResourceName:" + TargetResourceName + ",SpawnEntry:" + SpawnEntry?.ToString() + ",TargetLevels:" + StringUtil.CollectionToString(TargetLevels) + ",}";
	}
}
