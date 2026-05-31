using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Resource;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ToolOverrideLevel : BeanBase
{
	public const int __ID__ = -2022066619;

	public DungeonResourceClass TargetResourceClass { get; private set; }

	public int OverrideLevel { get; private set; }

	public ToolOverrideLevel(JSONNode _json)
	{
		if (!_json["target_resource_class"].IsNumber)
		{
			throw new SerializationException();
		}
		TargetResourceClass = (DungeonResourceClass)_json["target_resource_class"].AsInt;
		if (!_json["override_level"].IsNumber)
		{
			throw new SerializationException();
		}
		OverrideLevel = _json["override_level"];
	}

	public ToolOverrideLevel(DungeonResourceClass target_resource_class, int override_level)
	{
		TargetResourceClass = target_resource_class;
		OverrideLevel = override_level;
	}

	public static ToolOverrideLevel DeserializeToolOverrideLevel(JSONNode _json)
	{
		return new ToolOverrideLevel(_json);
	}

	public override int GetTypeId()
	{
		return -2022066619;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ TargetResourceClass:" + TargetResourceClass.ToString() + ",OverrideLevel:" + OverrideLevel + ",}";
	}
}
