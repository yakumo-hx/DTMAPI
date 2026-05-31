using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Drone;

public sealed class DroneFunctionProtoMineHelper : DroneFunctionProto
{
	public const int __ID__ = 1773070351;

	public DroneFunctionProtoMineHelper(JSONNode _json)
		: base(_json)
	{
	}

	public DroneFunctionProtoMineHelper()
	{
	}

	public static DroneFunctionProtoMineHelper DeserializeDroneFunctionProtoMineHelper(JSONNode _json)
	{
		return new DroneFunctionProtoMineHelper(_json);
	}

	public override int GetTypeId()
	{
		return 1773070351;
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
