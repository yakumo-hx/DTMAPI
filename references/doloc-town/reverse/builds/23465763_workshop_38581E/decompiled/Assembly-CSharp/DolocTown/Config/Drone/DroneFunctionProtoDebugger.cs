using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Drone;

public sealed class DroneFunctionProtoDebugger : DroneFunctionProto
{
	public const int __ID__ = -687588913;

	public DroneFunctionProtoDebugger(JSONNode _json)
		: base(_json)
	{
	}

	public DroneFunctionProtoDebugger()
	{
	}

	public static DroneFunctionProtoDebugger DeserializeDroneFunctionProtoDebugger(JSONNode _json)
	{
		return new DroneFunctionProtoDebugger(_json);
	}

	public override int GetTypeId()
	{
		return -687588913;
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
		return "{ }";
	}
}
