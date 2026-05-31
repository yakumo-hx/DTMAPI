using System;
using System.Collections.Generic;
using Bright.Serialization;
using DolocTown.Config.Player;
using SimpleJSON;

namespace DolocTown.Config.Item;

public abstract class ItemFunctionPassiveBase : ItemFunctionAgentEquipment
{
	public string Skill { get; private set; }

	public AgentEquipmentSkillInfo Skill_Ref { get; private set; }

	public ItemFunctionPassiveBase(JSONNode _json)
		: base(_json)
	{
		if (!_json["skill"].IsString)
		{
			throw new SerializationException();
		}
		Skill = _json["skill"];
	}

	public ItemFunctionPassiveBase(string skill)
	{
		Skill = skill;
	}

	public static ItemFunctionPassiveBase DeserializeItemFunctionPassiveBase(JSONNode _json)
	{
		string text = _json["$type"];
		if (!(text == "ItemFunctionPassive"))
		{
			if (text == "ItemFunctionHerbPackage")
			{
				return new ItemFunctionHerbPackage(_json);
			}
			throw new SerializationException();
		}
		return new ItemFunctionPassive(_json);
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
		Skill_Ref = (_tables["Player.TbAgentEquipmentSkill"] as TbAgentEquipmentSkill).GetOrDefault(Skill);
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ Skill:" + Skill + ",}";
	}
}
