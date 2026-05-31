using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Player;

public sealed class AgentEquipmentFuncProtoFellCountAdditionOre : AgentEquipmentFuncProto
{
	public const int __ID__ = 1914477284;

	public int FellCountIncrease { get; private set; }

	public AgentEquipmentFuncProtoFellCountAdditionOre(JSONNode _json)
		: base(_json)
	{
		if (!_json["fell_count_increase"].IsNumber)
		{
			throw new SerializationException();
		}
		FellCountIncrease = _json["fell_count_increase"];
	}

	public AgentEquipmentFuncProtoFellCountAdditionOre(int fell_count_increase)
	{
		FellCountIncrease = fell_count_increase;
	}

	public static AgentEquipmentFuncProtoFellCountAdditionOre DeserializeAgentEquipmentFuncProtoFellCountAdditionOre(JSONNode _json)
	{
		return new AgentEquipmentFuncProtoFellCountAdditionOre(_json);
	}

	public override int GetTypeId()
	{
		return 1914477284;
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
		return "{ FellCountIncrease:" + FellCountIncrease + ",}";
	}
}
