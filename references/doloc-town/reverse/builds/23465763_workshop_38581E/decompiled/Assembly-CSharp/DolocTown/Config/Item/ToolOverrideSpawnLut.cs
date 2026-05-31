using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Resource;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ToolOverrideSpawnLut : BeanBase
{
	public const int __ID__ = 1727570991;

	public DungeonResourceClass TargetResourceClass { get; private set; }

	public string OverrideSpawnLut { get; private set; }

	public ItemSpawnInfo OverrideSpawnLut_Ref { get; private set; }

	public ToolOverrideSpawnLut(JSONNode _json)
	{
		if (!_json["target_resource_class"].IsNumber)
		{
			throw new SerializationException();
		}
		TargetResourceClass = (DungeonResourceClass)_json["target_resource_class"].AsInt;
		if (!_json["override_spawn_lut"].IsString)
		{
			throw new SerializationException();
		}
		OverrideSpawnLut = _json["override_spawn_lut"];
	}

	public ToolOverrideSpawnLut(DungeonResourceClass target_resource_class, string override_spawn_lut)
	{
		TargetResourceClass = target_resource_class;
		OverrideSpawnLut = override_spawn_lut;
	}

	public static ToolOverrideSpawnLut DeserializeToolOverrideSpawnLut(JSONNode _json)
	{
		return new ToolOverrideSpawnLut(_json);
	}

	public override int GetTypeId()
	{
		return 1727570991;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		OverrideSpawnLut_Ref = (_tables["Item.TbItemSpawn"] as TbItemSpawn).GetOrDefault(OverrideSpawnLut);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ TargetResourceClass:" + TargetResourceClass.ToString() + ",OverrideSpawnLut:" + OverrideSpawnLut + ",}";
	}
}
