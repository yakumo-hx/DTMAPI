using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Serialization;
using DolocTown.Config.Animal;
using SimpleJSON;

namespace DolocTown.Config.Player;

public sealed class AgentEquipmentFuncProtoShepherd : AgentEquipmentFuncProto
{
	public const int __ID__ = 1245891389;

	public string[] AnimalNames { get; private set; }

	public AnimalInfo[] AnimalNames_Ref { get; private set; }

	public int MoodIncrease { get; private set; }

	public AgentEquipmentFuncProtoShepherd(JSONNode _json)
		: base(_json)
	{
		JSONNode jSONNode = _json["animal_names"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		AnimalNames = new string[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsString)
			{
				throw new SerializationException();
			}
			string text = child;
			AnimalNames[num++] = text;
		}
		if (!_json["mood_increase"].IsNumber)
		{
			throw new SerializationException();
		}
		MoodIncrease = _json["mood_increase"];
	}

	public AgentEquipmentFuncProtoShepherd(string[] animal_names, int mood_increase)
	{
		AnimalNames = animal_names;
		MoodIncrease = mood_increase;
	}

	public static AgentEquipmentFuncProtoShepherd DeserializeAgentEquipmentFuncProtoShepherd(JSONNode _json)
	{
		return new AgentEquipmentFuncProtoShepherd(_json);
	}

	public override int GetTypeId()
	{
		return 1245891389;
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
		int num = AnimalNames.Length;
		TbAnimal tbAnimal = (TbAnimal)_tables["Animal.TbAnimal"];
		AnimalNames_Ref = new AnimalInfo[num];
		for (int i = 0; i < num; i++)
		{
			AnimalNames_Ref[i] = tbAnimal.GetOrDefault(AnimalNames[i]);
		}
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ AnimalNames:" + StringUtil.CollectionToString(AnimalNames) + ",MoodIncrease:" + MoodIncrease + ",}";
	}
}
