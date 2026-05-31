using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Serialization;
using DolocTown.Config.Item;
using SimpleJSON;

namespace DolocTown.Config.Player;

public sealed class AgentEquipmentFuncProtoFoodEffectsAddition : AgentEquipmentFuncProto
{
	public const int __ID__ = 1538732946;

	public string[] ItemNames { get; private set; }

	public ItemInfo[] ItemNames_Ref { get; private set; }

	public float AdditionRate { get; private set; }

	public AgentEquipmentFuncProtoFoodEffectsAddition(JSONNode _json)
		: base(_json)
	{
		JSONNode jSONNode = _json["item_names"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		ItemNames = new string[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsString)
			{
				throw new SerializationException();
			}
			string text = child;
			ItemNames[num++] = text;
		}
		if (!_json["addition_rate"].IsNumber)
		{
			throw new SerializationException();
		}
		AdditionRate = _json["addition_rate"];
	}

	public AgentEquipmentFuncProtoFoodEffectsAddition(string[] item_names, float addition_rate)
	{
		ItemNames = item_names;
		AdditionRate = addition_rate;
	}

	public static AgentEquipmentFuncProtoFoodEffectsAddition DeserializeAgentEquipmentFuncProtoFoodEffectsAddition(JSONNode _json)
	{
		return new AgentEquipmentFuncProtoFoodEffectsAddition(_json);
	}

	public override int GetTypeId()
	{
		return 1538732946;
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
		int num = ItemNames.Length;
		TbItem tbItem = (TbItem)_tables["Item.TbItem"];
		ItemNames_Ref = new ItemInfo[num];
		for (int i = 0; i < num; i++)
		{
			ItemNames_Ref[i] = tbItem.GetOrDefault(ItemNames[i]);
		}
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ ItemNames:" + StringUtil.CollectionToString(ItemNames) + ",AdditionRate:" + AdditionRate + ",}";
	}
}
