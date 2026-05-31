using System;
using System.Collections.Generic;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Player;

public sealed class AgentEquipmentFuncProtoCounterBack : AgentEquipmentFuncProto
{
	public const int __ID__ = 567893173;

	public string SkillId { get; private set; }

	public float Probability { get; private set; }

	public AgentEquipmentFuncProtoCounterBack(JSONNode _json)
		: base(_json)
	{
		if (!_json["skill_id"].IsString)
		{
			throw new SerializationException();
		}
		SkillId = _json["skill_id"];
		if (!_json["probability"].IsNumber)
		{
			throw new SerializationException();
		}
		Probability = _json["probability"];
	}

	public AgentEquipmentFuncProtoCounterBack(string skill_id, float probability)
	{
		SkillId = skill_id;
		Probability = probability;
	}

	public static AgentEquipmentFuncProtoCounterBack DeserializeAgentEquipmentFuncProtoCounterBack(JSONNode _json)
	{
		return new AgentEquipmentFuncProtoCounterBack(_json);
	}

	public override int GetTypeId()
	{
		return 567893173;
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
		return "{ SkillId:" + SkillId + ",Probability:" + Probability + ",}";
	}
}
