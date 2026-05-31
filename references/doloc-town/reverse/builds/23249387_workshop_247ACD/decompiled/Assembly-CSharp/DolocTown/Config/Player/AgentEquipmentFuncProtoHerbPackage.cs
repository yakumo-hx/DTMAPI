using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Player;

public sealed class AgentEquipmentFuncProtoHerbPackage : AgentEquipmentFuncProto
{
	public const int __ID__ = 1814205259;

	public int RecoveryAmount { get; private set; }

	public AgentEquipmentFuncProtoHerbPackage(JSONNode _json)
		: base(_json)
	{
		if (!_json["recovery_amount"].IsNumber)
		{
			throw new SerializationException();
		}
		RecoveryAmount = _json["recovery_amount"];
	}

	public AgentEquipmentFuncProtoHerbPackage(int recovery_amount)
	{
		RecoveryAmount = recovery_amount;
	}

	public static AgentEquipmentFuncProtoHerbPackage DeserializeAgentEquipmentFuncProtoHerbPackage(JSONNode _json)
	{
		return new AgentEquipmentFuncProtoHerbPackage(_json);
	}

	public override int GetTypeId()
	{
		return 1814205259;
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
		return "{ RecoveryAmount:" + RecoveryAmount + ",}";
	}
}
