using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Serialization;
using DolocTown.Config.Item;
using DolocTown.Config.Monster;
using SimpleJSON;

namespace DolocTown.Config.Drone;

public sealed class DroneFunctionProtoChipDissolver : DroneFunctionProto
{
	public const int __ID__ = 2102950471;

	public MonsterType[] MonsterType { get; private set; }

	public string ItemName { get; private set; }

	public ItemInfo ItemName_Ref { get; private set; }

	public float Probability { get; private set; }

	public DroneFunctionProtoChipDissolver(JSONNode _json)
		: base(_json)
	{
		JSONNode jSONNode = _json["monster_type"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		MonsterType = new MonsterType[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsNumber)
			{
				throw new SerializationException();
			}
			MonsterType asInt = (MonsterType)child.AsInt;
			MonsterType[num++] = asInt;
		}
		if (!_json["item_name"].IsString)
		{
			throw new SerializationException();
		}
		ItemName = _json["item_name"];
		if (!_json["probability"].IsNumber)
		{
			throw new SerializationException();
		}
		Probability = _json["probability"];
	}

	public DroneFunctionProtoChipDissolver(MonsterType[] monster_type, string item_name, float probability)
	{
		MonsterType = monster_type;
		ItemName = item_name;
		Probability = probability;
	}

	public static DroneFunctionProtoChipDissolver DeserializeDroneFunctionProtoChipDissolver(JSONNode _json)
	{
		return new DroneFunctionProtoChipDissolver(_json);
	}

	public override int GetTypeId()
	{
		return 2102950471;
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
		ItemName_Ref = (_tables["Item.TbItem"] as TbItem).GetOrDefault(ItemName);
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ MonsterType:" + StringUtil.CollectionToString(MonsterType) + ",ItemName:" + ItemName + ",Probability:" + Probability + ",}";
	}
}
