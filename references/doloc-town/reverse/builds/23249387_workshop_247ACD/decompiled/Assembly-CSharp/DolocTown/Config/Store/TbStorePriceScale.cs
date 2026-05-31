using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Store;

public sealed class TbStorePriceScale
{
	private readonly Dictionary<string, StorePriceScaleInfo> _dataMap;

	private readonly List<StorePriceScaleInfo> _dataList;

	public Dictionary<string, StorePriceScaleInfo> DataMap => _dataMap;

	public List<StorePriceScaleInfo> DataList => _dataList;

	public StorePriceScaleInfo this[string key] => _dataMap[key];

	public TbStorePriceScale(JSONNode _json)
	{
		_dataMap = new Dictionary<string, StorePriceScaleInfo>();
		_dataList = new List<StorePriceScaleInfo>();
		foreach (JSONNode child in _json.Children)
		{
			StorePriceScaleInfo storePriceScaleInfo = StorePriceScaleInfo.DeserializeStorePriceScaleInfo(child);
			if (_dataMap.TryAdd(storePriceScaleInfo.Id, storePriceScaleInfo))
			{
				_dataList.Add(storePriceScaleInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + storePriceScaleInfo.Id + " in table: TbStorePriceScale");
			}
		}
	}

	public StorePriceScaleInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public StorePriceScaleInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (StorePriceScaleInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (StorePriceScaleInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
