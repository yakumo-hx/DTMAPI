using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Player;

public sealed class AgentEquipmentFuncProtoIncreaseCriticalRate : AgentEquipmentFuncProto
{
	public const int __ID__ = -1019810545;

	public float CriticalRate { get; private set; }

	public AgentEquipmentFuncProtoIncreaseCriticalRate(JSONNode _json)
		: base(_json)
	{
		if (!_json["critical_rate"].IsNumber)
		{
			throw new SerializationException();
		}
		CriticalRate = _json["critical_rate"];
	}

	public AgentEquipmentFuncProtoIncreaseCriticalRate(float critical_rate)
	{
		CriticalRate = critical_rate;
	}

	public static AgentEquipmentFuncProtoIncreaseCriticalRate DeserializeAgentEquipmentFuncProtoIncreaseCriticalRate(JSONNode _json)
	{
		return new AgentEquipmentFuncProtoIncreaseCriticalRate(_json);
	}

	public override int GetTypeId()
	{
		return -1019810545;
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
		return "{ CriticalRate:" + CriticalRate + ",}";
	}
}
