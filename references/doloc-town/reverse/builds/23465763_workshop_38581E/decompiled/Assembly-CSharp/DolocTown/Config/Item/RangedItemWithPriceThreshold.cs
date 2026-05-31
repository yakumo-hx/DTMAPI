using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class RangedItemWithPriceThreshold : BeanBase
{
	public const int __ID__ = 1881675933;

	public int PriceThreshold { get; private set; }

	public RangedItem RangedItem { get; private set; }

	public RangedItemWithPriceThreshold(JSONNode _json)
	{
		if (!_json["price_threshold"].IsNumber)
		{
			throw new SerializationException();
		}
		PriceThreshold = _json["price_threshold"];
		if (!_json["ranged_item"].IsObject)
		{
			throw new SerializationException();
		}
		RangedItem = ExternalTypeUtil.RangedItemConverter(CfgRangedItem.DeserializeCfgRangedItem(_json["ranged_item"]));
	}

	public RangedItemWithPriceThreshold(int price_threshold, RangedItem ranged_item)
	{
		PriceThreshold = price_threshold;
		RangedItem = ranged_item;
	}

	public static RangedItemWithPriceThreshold DeserializeRangedItemWithPriceThreshold(JSONNode _json)
	{
		return new RangedItemWithPriceThreshold(_json);
	}

	public override int GetTypeId()
	{
		return 1881675933;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ PriceThreshold:" + PriceThreshold + ",RangedItem:" + RangedItem.ToString() + ",}";
	}
}
