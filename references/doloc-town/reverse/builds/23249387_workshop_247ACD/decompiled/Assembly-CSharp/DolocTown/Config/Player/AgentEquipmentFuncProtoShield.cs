using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Player;

public sealed class AgentEquipmentFuncProtoShield : AgentEquipmentFuncProto
{
	public const int __ID__ = 1297493111;

	public int Defend { get; private set; }

	public AgentEquipmentFuncProtoShield(JSONNode _json)
		: base(_json)
	{
		if (!_json["defend"].IsNumber)
		{
			throw new SerializationException();
		}
		Defend = _json["defend"];
	}

	public AgentEquipmentFuncProtoShield(int defend)
	{
		Defend = defend;
	}

	public static AgentEquipmentFuncProtoShield DeserializeAgentEquipmentFuncProtoShield(JSONNode _json)
	{
		return new AgentEquipmentFuncProtoShield(_json);
	}

	public override int GetTypeId()
	{
		return 1297493111;
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
		return "{ Defend:" + Defend + ",}";
	}
}
