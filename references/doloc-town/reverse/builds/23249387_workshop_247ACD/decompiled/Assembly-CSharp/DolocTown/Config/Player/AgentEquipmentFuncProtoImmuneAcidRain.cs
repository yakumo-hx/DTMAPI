using System;
using System.Collections.Generic;
using SimpleJSON;

namespace DolocTown.Config.Player;

public sealed class AgentEquipmentFuncProtoImmuneAcidRain : AgentEquipmentFuncProto
{
	public const int __ID__ = -1810747742;

	public AgentEquipmentFuncProtoImmuneAcidRain(JSONNode _json)
		: base(_json)
	{
	}

	public AgentEquipmentFuncProtoImmuneAcidRain()
	{
	}

	public static AgentEquipmentFuncProtoImmuneAcidRain DeserializeAgentEquipmentFuncProtoImmuneAcidRain(JSONNode _json)
	{
		return new AgentEquipmentFuncProtoImmuneAcidRain(_json);
	}

	public override int GetTypeId()
	{
		return -1810747742;
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
