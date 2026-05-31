using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Fishing;

public sealed class TbFarmFish
{
	private readonly Dictionary<string, FarmFishInfo> _dataMap;

	private readonly List<FarmFishInfo> _dataList;

	public Dictionary<string, FarmFishInfo> DataMap => _dataMap;

	public List<FarmFishInfo> DataList => _dataList;

	public FarmFishInfo this[string key] => _dataMap[key];

	public TbFarmFish(JSONNode _json)
	{
		_dataMap = new Dictionary<string, FarmFishInfo>();
		_dataList = new List<FarmFishInfo>();
		foreach (JSONNode child in _json.Children)
		{
			FarmFishInfo farmFishInfo = FarmFishInfo.DeserializeFarmFishInfo(child);
			if (_dataMap.TryAdd(farmFishInfo.Id, farmFishInfo))
			{
				_dataList.Add(farmFishInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + farmFishInfo.Id + " in table: TbFarmFish");
			}
		}
	}

	public FarmFishInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public FarmFishInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (FarmFishInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (FarmFishInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}

	public int GetTechPoint(string fishName)
	{
		if (fishName.IsNullOrEmpty())
		{
			return 0;
		}
		if (!_dataMap.TryGetValue(fishName, out var value))
		{
			return 0;
		}
		return value.TechPoint;
	}

	public bool IsFarmFish(string name, out FarmFishInfo proto)
	{
		proto = null;
		if (!name.IsNullOrEmpty())
		{
			return _dataMap.TryGetValue(name, out proto);
		}
		return false;
	}
}
