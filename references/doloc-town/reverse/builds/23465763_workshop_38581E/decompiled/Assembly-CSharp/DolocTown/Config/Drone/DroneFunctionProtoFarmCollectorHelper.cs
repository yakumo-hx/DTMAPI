using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Drone;

public sealed class DroneFunctionProtoFarmCollectorHelper : DroneFunctionProto
{
	public const int __ID__ = -285665001;

	public DroneFunctionProtoFarmCollectorHelper(JSONNode _json)
		: base(_json)
	{
	}

	public DroneFunctionProtoFarmCollectorHelper()
	{
	}

	public static DroneFunctionProtoFarmCollectorHelper DeserializeDroneFunctionProtoFarmCollectorHelper(JSONNode _json)
	{
		return new DroneFunctionProtoFarmCollectorHelper(_json);
	}

	public override int GetTypeId()
	{
		return -285665001;
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
