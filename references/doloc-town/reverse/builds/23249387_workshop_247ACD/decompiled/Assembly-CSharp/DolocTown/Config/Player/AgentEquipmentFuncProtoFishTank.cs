using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Player;

public sealed class AgentEquipmentFuncProtoFishTank : AgentEquipmentFuncProto
{
	public const int __ID__ = 1344517360;

	public float Probability { get; private set; }

	public int Rarity { get; private set; }

	public AgentEquipmentFuncProtoFishTank(JSONNode _json)
		: base(_json)
	{
		if (!_json["probability"].IsNumber)
		{
			throw new SerializationException();
		}
		Probability = _json["probability"];
		if (!_json["rarity"].IsNumber)
		{
			throw new SerializationException();
		}
		Rarity = _json["rarity"];
	}

	public AgentEquipmentFuncProtoFishTank(float probability, int rarity)
	{
		Probability = probability;
		Rarity = rarity;
	}

	public static AgentEquipmentFuncProtoFishTank DeserializeAgentEquipmentFuncProtoFishTank(JSONNode _json)
	{
		return new AgentEquipmentFuncProtoFishTank(_json);
	}

	public override int GetTypeId()
	{
		return 1344517360;
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
		return "{ Probability:" + Probability + ",Rarity:" + Rarity + ",}";
	}
}
