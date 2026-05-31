using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Player;

public sealed class AgentEquipmentFuncProtoAmoeba : AgentEquipmentFuncProto
{
	public const int __ID__ = 786964431;

	public AgentEquipmentFuncProtoAmoeba(JSONNode _json)
		: base(_json)
	{
	}

	public AgentEquipmentFuncProtoAmoeba()
	{
	}

	public static AgentEquipmentFuncProtoAmoeba DeserializeAgentEquipmentFuncProtoAmoeba(JSONNode _json)
	{
		return new AgentEquipmentFuncProtoAmoeba(_json);
	}

	public override int GetTypeId()
	{
		return 786964431;
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
