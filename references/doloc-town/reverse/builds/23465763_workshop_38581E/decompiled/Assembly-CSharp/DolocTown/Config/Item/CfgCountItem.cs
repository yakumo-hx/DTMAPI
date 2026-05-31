using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class CfgCountItem : BeanBase
{
	public const int __ID__ = 423431097;

	public string ItemName { get; private set; }

	public ItemInfo ItemName_Ref { get; private set; }

	public int ItemCount { get; private set; }

	public CfgCountItem(JSONNode _json)
	{
		if (!_json["item_name"].IsString)
		{
			throw new SerializationException();
		}
		ItemName = _json["item_name"];
		if (!_json["item_count"].IsNumber)
		{
			throw new SerializationException();
		}
		ItemCount = _json["item_count"];
	}

	public CfgCountItem(string item_name, int item_count)
	{
		ItemName = item_name;
		ItemCount = item_count;
	}

	public static CfgCountItem DeserializeCfgCountItem(JSONNode _json)
	{
		return new CfgCountItem(_json);
	}

	public override int GetTypeId()
	{
		return 423431097;
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
		return "{ ItemName:" + ItemName + ",ItemCount:" + ItemCount + ",}";
	}
}
