using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Player;

public sealed class AgentEquipmentFuncProtoMimeticAffinity : AgentEquipmentFuncProto
{
	public const int __ID__ = -322857516;

	public AgentEquipmentFuncProtoMimeticAffinity(JSONNode _json)
		: base(_json)
	{
	}

	public AgentEquipmentFuncProtoMimeticAffinity()
	{
	}

	public static AgentEquipmentFuncProtoMimeticAffinity DeserializeAgentEquipmentFuncProtoMimeticAffinity(JSONNode _json)
	{
		return new AgentEquipmentFuncProtoMimeticAffinity(_json);
	}

	public override int GetTypeId()
	{
		return -322857516;
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
