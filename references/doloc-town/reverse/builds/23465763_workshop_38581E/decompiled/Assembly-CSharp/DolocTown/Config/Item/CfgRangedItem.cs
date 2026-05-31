using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class CfgRangedItem : BeanBase
{
	public const int __ID__ = -514812317;

	public string ItemName { get; private set; }

	public ItemInfo ItemName_Ref { get; private set; }

	public int MinCount { get; private set; }

	public int MaxCount { get; private set; }

	public CfgRangedItem(JSONNode _json)
	{
		if (!_json["item_name"].IsString)
		{
			throw new SerializationException();
		}
		ItemName = _json["item_name"];
		if (!_json["min_count"].IsNumber)
		{
			throw new SerializationException();
		}
		MinCount = _json["min_count"];
		if (!_json["max_count"].IsNumber)
		{
			throw new SerializationException();
		}
		MaxCount = _json["max_count"];
	}

	public CfgRangedItem(string item_name, int min_count, int max_count)
	{
		ItemName = item_name;
		MinCount = min_count;
		MaxCount = max_count;
	}

	public static CfgRangedItem DeserializeCfgRangedItem(JSONNode _json)
	{
		return new CfgRangedItem(_json);
	}

	public override int GetTypeId()
	{
		return -514812317;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		ItemName_Ref = (_tables["Item.TbItem"] as TbItem).GetOrDefault(ItemName);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ ItemName:" + ItemName + ",MinCount:" + MinCount + ",MaxCount:" + MaxCount + ",}";
	}
}
