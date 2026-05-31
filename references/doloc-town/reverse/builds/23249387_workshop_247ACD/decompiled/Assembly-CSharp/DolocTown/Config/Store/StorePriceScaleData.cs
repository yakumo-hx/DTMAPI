using System;
using System.Collections.Generic;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Store;

public sealed class StorePriceScaleData : BeanBase
{
	public readonly Dictionary<string, StoreScaleOfType> ScaleByType_Index = new Dictionary<string, StoreScaleOfType>();

	public const int __ID__ = 1204427583;

	public float DefaultScale { get; private set; }

	public StoreScaleOfType[] ScaleByType { get; private set; }

	public StorePriceScaleData(JSONNode _json)
	{
		if (!_json["default_scale"].IsNumber)
		{
			throw new SerializationException();
		}
		DefaultScale = _json["default_scale"];
		JSONNode jSONNode = _json["scale_by_type"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		int count = jSONNode.Count;
		ScaleByType = new StoreScaleOfType[count];
		int num = 0;
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			StoreScaleOfType storeScaleOfType = StoreScaleOfType.DeserializeStoreScaleOfType(child);
			ScaleByType[num++] = storeScaleOfType;
		}
		StoreScaleOfType[] scaleByType = ScaleByType;
		foreach (StoreScaleOfType storeScaleOfType2 in scaleByType)
		{
			ScaleByType_Index.Add(storeScaleOfType2.ItemSubType, storeScaleOfType2);
		}
	}

	public StorePriceScaleData(float default_scale, StoreScaleOfType[] scale_by_type)
	{
		DefaultScale = default_scale;
		ScaleByType = scale_by_type;
		StoreScaleOfType[] scaleByType = ScaleByType;
		foreach (StoreScaleOfType storeScaleOfType in scaleByType)
		{
			ScaleByType_Index.Add(storeScaleOfType.ItemSubType, storeScaleOfType);
		}
	}

	public static StorePriceScaleData DeserializeStorePriceScaleData(JSONNode _json)
	{
		return new StorePriceScaleData(_json);
	}

	public override int GetTypeId()
	{
		return 1204427583;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		StoreScaleOfType[] scaleByType = ScaleByType;
		for (int i = 0; i < scaleByType.Length; i++)
		{
			scaleByType[i]?.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		StoreScaleOfType[] scaleByType = ScaleByType;
		for (int i = 0; i < scaleByType.Length; i++)
		{
			scaleByType[i]?.TranslateText(translator);
		}
	}

	public override string ToString()
	{
		return "{ DefaultScale:" + DefaultScale + ",ScaleByType:" + StringUtil.CollectionToString(ScaleByType) + ",}";
	}
}
