using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ToolConstraint : BeanBase
{
	public const int __ID__ = -724662832;

	public ToolType ToolType { get; private set; }

	public int ToolLevel { get; private set; }

	public ToolConstraint(JSONNode _json)
	{
		if (!_json["tool_type"].IsNumber)
		{
			throw new SerializationException();
		}
		ToolType = (ToolType)_json["tool_type"].AsInt;
		if (!_json["tool_level"].IsNumber)
		{
			throw new SerializationException();
		}
		ToolLevel = _json["tool_level"];
	}

	public ToolConstraint(ToolType tool_type, int tool_level)
	{
		ToolType = tool_type;
		ToolLevel = tool_level;
	}

	public static ToolConstraint DeserializeToolConstraint(JSONNode _json)
	{
		return new ToolConstraint(_json);
	}

	public override int GetTypeId()
	{
		return -724662832;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ ToolType:" + ToolType.ToString() + ",ToolLevel:" + ToolLevel + ",}";
	}
}
