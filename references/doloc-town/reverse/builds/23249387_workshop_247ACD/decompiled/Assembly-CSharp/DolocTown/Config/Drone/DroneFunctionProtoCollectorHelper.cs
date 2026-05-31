using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Serialization;
using DolocTown.Config.Resource;
using SimpleJSON;

namespace DolocTown.Config.Drone;

public sealed class DroneFunctionProtoCollectorHelper : DroneFunctionProto
{
	public const int __ID__ = 802161965;

	public DungeonResourceType[] Types { get; private set; }

	public int ToolLevel { get; private set; }

	public int ChopCount { get; private set; }

	public DroneFunctionProtoCollectorHelper(JSONNode _json)
		: base(_json)
	{
		JSONNode jSONNode = _json["types"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		Types = new DungeonResourceType[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsNumber)
			{
				throw new SerializationException();
			}
			DungeonResourceType asInt = (DungeonResourceType)child.AsInt;
			Types[num++] = asInt;
		}
		if (!_json["tool_level"].IsNumber)
		{
			throw new SerializationException();
		}
		ToolLevel = _json["tool_level"];
		if (!_json["chop_count"].IsNumber)
		{
			throw new SerializationException();
		}
		ChopCount = _json["chop_count"];
	}

	public DroneFunctionProtoCollectorHelper(DungeonResourceType[] types, int tool_level, int chop_count)
	{
		Types = types;
		ToolLevel = tool_level;
		ChopCount = chop_count;
	}

	public static DroneFunctionProtoCollectorHelper DeserializeDroneFunctionProtoCollectorHelper(JSONNode _json)
	{
		return new DroneFunctionProtoCollectorHelper(_json);
	}

	public override int GetTypeId()
	{
		return 802161965;
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ Types:" + StringUtil.CollectionToString(Types) + ",ToolLevel:" + ToolLevel + ",ChopCount:" + ChopCount + ",}";
	}
}
