using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Fishing;

public sealed class TbFishingPool
{
	private readonly Dictionary<string, FishingPoolInfo> _dataMap;

	private readonly List<FishingPoolInfo> _dataList;

	public Dictionary<string, FishingPoolInfo> DataMap => _dataMap;

	public List<FishingPoolInfo> DataList => _dataList;

	public FishingPoolInfo this[string key] => _dataMap[key];

	public TbFishingPool(JSONNode _json)
	{
		_dataMap = new Dictionary<string, FishingPoolInfo>();
		_dataList = new List<FishingPoolInfo>();
		foreach (JSONNode child in _json.Children)
		{
			FishingPoolInfo fishingPoolInfo = FishingPoolInfo.DeserializeFishingPoolInfo(child);
			if (_dataMap.TryAdd(fishingPoolInfo.Id, fishingPoolInfo))
			{
				_dataList.Add(fishingPoolInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + fishingPoolInfo.Id + " in table: TbFishingPool");
			}
		}
	}

	public FishingPoolInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public FishingPoolInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (FishingPoolInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (FishingPoolInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
