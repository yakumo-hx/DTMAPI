using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Player;

public sealed class AgentEquipmentFuncProtoGrandmasButton : AgentEquipmentFuncProto
{
	public const int __ID__ = 714195187;

	public float RecoveryIncrease { get; private set; }

	public AgentEquipmentFuncProtoGrandmasButton(JSONNode _json)
		: base(_json)
	{
		if (!_json["recovery_increase"].IsNumber)
		{
			throw new SerializationException();
		}
		RecoveryIncrease = _json["recovery_increase"];
	}

	public AgentEquipmentFuncProtoGrandmasButton(float recovery_increase)
	{
		RecoveryIncrease = recovery_increase;
	}

	public static AgentEquipmentFuncProtoGrandmasButton DeserializeAgentEquipmentFuncProtoGrandmasButton(JSONNode _json)
	{
		return new AgentEquipmentFuncProtoGrandmasButton(_json);
	}

	public override int GetTypeId()
	{
		return 714195187;
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
		return "{ RecoveryIncrease:" + RecoveryIncrease + ",}";
	}
}
