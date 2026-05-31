using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Serialization;
using DolocTown.Config.Item;
using SimpleJSON;

namespace DolocTown.Config.Drone;

public sealed class DroneFunctionProtoGarbageCombustionEngine : DroneFunctionProto
{
	public const int __ID__ = 1520843772;

	public string[] AvailableItemNames { get; private set; }

	public ItemInfo[] AvailableItemNames_Ref { get; private set; }

	public float Energy { get; private set; }

	public DroneFunctionProtoGarbageCombustionEngine(JSONNode _json)
		: base(_json)
	{
		JSONNode jSONNode = _json["available_item_names"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		AvailableItemNames = new string[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsString)
			{
				throw new SerializationException();
			}
			string text = child;
			AvailableItemNames[num++] = text;
		}
		if (!_json["energy"].IsNumber)
		{
			throw new SerializationException();
		}
		Energy = _json["energy"];
	}

	public DroneFunctionProtoGarbageCombustionEngine(string[] available_item_names, float energy)
	{
		AvailableItemNames = available_item_names;
		Energy = energy;
	}

	public static DroneFunctionProtoGarbageCombustionEngine DeserializeDroneFunctionProtoGarbageCombustionEngine(JSONNode _json)
	{
		return new DroneFunctionProtoGarbageCombustionEngine(_json);
	}

	public override int GetTypeId()
	{
		return 1520843772;
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
		int num = AvailableItemNames.Length;
		TbItem tbItem = (TbItem)_tables["Item.TbItem"];
		AvailableItemNames_Ref = new ItemInfo[num];
		for (int i = 0; i < num; i++)
		{
			AvailableItemNames_Ref[i] = tbItem.GetOrDefault(AvailableItemNames[i]);
		}
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ AvailableItemNames:" + StringUtil.CollectionToString(AvailableItemNames) + ",Energy:" + Energy + ",}";
	}
}
