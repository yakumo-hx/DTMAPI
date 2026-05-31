using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Store;

public sealed class StorePriceScaleInfo : BeanBase
{
	public const int __ID__ = 1204588611;

	public string Id { get; private set; }

	public StorePriceScaleData[] SeasonPriceScale { get; private set; }

	public StorePriceScaleInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		JSONNode jSONNode = _json["season_price_scale"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		SeasonPriceScale = new StorePriceScaleData[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			StorePriceScaleData storePriceScaleData = StorePriceScaleData.DeserializeStorePriceScaleData(child);
			SeasonPriceScale[num++] = storePriceScaleData;
		}
	}

	public StorePriceScaleInfo(string id, StorePriceScaleData[] season_price_scale)
	{
		Id = id;
		SeasonPriceScale = season_price_scale;
	}

	public static StorePriceScaleInfo DeserializeStorePriceScaleInfo(JSONNode _json)
	{
		return new StorePriceScaleInfo(_json);
	}

	public override int GetTypeId()
	{
		return 1204588611;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		StorePriceScaleData[] seasonPriceScale = SeasonPriceScale;
		for (int i = 0; i < seasonPriceScale.Length; i++)
		{
			seasonPriceScale[i]?.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		StorePriceScaleData[] seasonPriceScale = SeasonPriceScale;
		for (int i = 0; i < seasonPriceScale.Length; i++)
		{
			seasonPriceScale[i]?.TranslateText(translator);
		}
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",SeasonPriceScale:" + StringUtil.CollectionToString(SeasonPriceScale) + ",}";
	}
}
