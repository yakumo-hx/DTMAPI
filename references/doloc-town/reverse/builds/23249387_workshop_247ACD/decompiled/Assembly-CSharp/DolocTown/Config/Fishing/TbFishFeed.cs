using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Fishing;

public sealed class TbFishFeed
{
	private readonly Dictionary<string, FishFeedInfo> _dataMap;

	private readonly List<FishFeedInfo> _dataList;

	public Dictionary<string, FishFeedInfo> DataMap => _dataMap;

	public List<FishFeedInfo> DataList => _dataList;

	public FishFeedInfo this[string key] => _dataMap[key];

	public TbFishFeed(JSONNode _json)
	{
		_dataMap = new Dictionary<string, FishFeedInfo>();
		_dataList = new List<FishFeedInfo>();
		foreach (JSONNode child in _json.Children)
		{
			FishFeedInfo fishFeedInfo = FishFeedInfo.DeserializeFishFeedInfo(child);
			if (_dataMap.TryAdd(fishFeedInfo.Id, fishFeedInfo))
			{
				_dataList.Add(fishFeedInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + fishFeedInfo.Id + " in table: TbFishFeed");
			}
		}
	}

	public FishFeedInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public FishFeedInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (FishFeedInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (FishFeedInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}

	public bool IsFishFeeds(string name, out int energy)
	{
		energy = 0;
		if (!name.IsNullOrEmpty() && _dataMap.TryGetValue(name, out var value))
		{
			energy = value.Energy;
			return energy > 0;
		}
		return false;
	}
}
