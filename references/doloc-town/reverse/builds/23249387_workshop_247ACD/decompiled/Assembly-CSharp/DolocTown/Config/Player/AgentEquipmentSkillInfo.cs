using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Player;

public sealed class AgentEquipmentSkillInfo : BeanBase
{
	public const int __ID__ = 213527689;

	public string Id { get; private set; }

	public string GearEntry { get; private set; }

	public string GearEntry_l10n_key { get; }

	public AgentEquipmentFuncProto Function { get; private set; }

	public AgentEquipmentSkillInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["gear_entry"]["key"].IsString)
		{
			throw new SerializationException();
		}
		GearEntry_l10n_key = _json["gear_entry"]["key"];
		if (!_json["gear_entry"]["text"].IsString)
		{
			throw new SerializationException();
		}
		GearEntry = _json["gear_entry"]["text"];
		if (!_json["function"].IsObject)
		{
			throw new SerializationException();
		}
		Function = AgentEquipmentFuncProto.DeserializeAgentEquipmentFuncProto(_json["function"]);
	}

	public AgentEquipmentSkillInfo(string id, string gear_entry, AgentEquipmentFuncProto function)
	{
		Id = id;
		GearEntry = gear_entry;
		Function = function;
	}

	public static AgentEquipmentSkillInfo DeserializeAgentEquipmentSkillInfo(JSONNode _json)
	{
		return new AgentEquipmentSkillInfo(_json);
	}

	public override int GetTypeId()
	{
		return 213527689;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		Function?.Resolve(_tables);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		GearEntry = translator(GearEntry_l10n_key, GearEntry);
		Function?.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",GearEntry:" + GearEntry + ",Function:" + Function?.ToString() + ",}";
	}
}
