using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Item;
using SimpleJSON;

namespace DolocTown.Config.Store;

public sealed class StoreScaleOfType : BeanBase
{
	public const int __ID__ = -1761720921;

	public string ItemSubType { get; private set; }

	public ItemSubTypeInfo ItemSubType_Ref { get; private set; }

	public float PriceScale { get; private set; }

	public StoreScaleOfType(JSONNode _json)
	{
		if (!_json["item_sub_type"].IsString)
		{
			throw new SerializationException();
		}
		ItemSubType = _json["item_sub_type"];
		if (!_json["price_scale"].IsNumber)
		{
			throw new SerializationException();
		}
		PriceScale = _json["price_scale"];
	}

	public StoreScaleOfType(string item_sub_type, float price_scale)
	{
		ItemSubType = item_sub_type;
		PriceScale = price_scale;
	}

	public static StoreScaleOfType DeserializeStoreScaleOfType(JSONNode _json)
	{
		return new StoreScaleOfType(_json);
	}

	public override int GetTypeId()
	{
		return -1761720921;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		ItemSubType_Ref = (_tables["Item.TbItemSubType"] as TbItemSubType).GetOrDefault(ItemSubType);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ ItemSubType:" + ItemSubType + ",PriceScale:" + PriceScale + ",}";
	}
}
