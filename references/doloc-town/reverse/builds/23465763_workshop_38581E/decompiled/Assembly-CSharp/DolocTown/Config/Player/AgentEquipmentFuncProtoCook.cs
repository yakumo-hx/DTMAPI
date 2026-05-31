using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Player;

public sealed class AgentEquipmentFuncProtoCook : AgentEquipmentFuncProto
{
	public const int __ID__ = -839342122;

	public float Probability { get; private set; }

	public AgentEquipmentFuncProtoCook(JSONNode _json)
		: base(_json)
	{
		if (!_json["probability"].IsNumber)
		{
			throw new SerializationException();
		}
		Probability = _json["probability"];
	}

	public AgentEquipmentFuncProtoCook(float probability)
	{
		Probability = probability;
	}

	public static AgentEquipmentFuncProtoCook DeserializeAgentEquipmentFuncProtoCook(JSONNode _json)
	{
		return new AgentEquipmentFuncProtoCook(_json);
	}

	public override int GetTypeId()
	{
		return -839342122;
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
		return "{ Probability:" + Probability + ",}";
	}
}
