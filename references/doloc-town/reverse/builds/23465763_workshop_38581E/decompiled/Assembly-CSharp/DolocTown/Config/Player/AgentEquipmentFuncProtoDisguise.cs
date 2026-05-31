using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Serialization;
using DolocTown.Config.Monster;
using SimpleJSON;

namespace DolocTown.Config.Player;

public sealed class AgentEquipmentFuncProtoDisguise : AgentEquipmentFuncProto
{
	public const int __ID__ = -2141035891;

	public string[] MonsterNames { get; private set; }

	public MonsterInfo[] MonsterNames_Ref { get; private set; }

	public AgentEquipmentFuncProtoDisguise(JSONNode _json)
		: base(_json)
	{
		JSONNode jSONNode = _json["monster_names"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		MonsterNames = new string[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsString)
			{
				throw new SerializationException();
			}
			string text = child;
			MonsterNames[num++] = text;
		}
	}

	public AgentEquipmentFuncProtoDisguise(string[] monster_names)
	{
		MonsterNames = monster_names;
	}

	public static AgentEquipmentFuncProtoDisguise DeserializeAgentEquipmentFuncProtoDisguise(JSONNode _json)
	{
		return new AgentEquipmentFuncProtoDisguise(_json);
	}

	public override int GetTypeId()
	{
		return -2141035891;
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
		int num = MonsterNames.Length;
		TbMonster tbMonster = (TbMonster)_tables["Monster.TbMonster"];
		MonsterNames_Ref = new MonsterInfo[num];
		for (int i = 0; i < num; i++)
		{
			MonsterNames_Ref[i] = tbMonster.GetOrDefault(MonsterNames[i]);
		}
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ MonsterNames:" + StringUtil.CollectionToString(MonsterNames) + ",}";
	}
}
