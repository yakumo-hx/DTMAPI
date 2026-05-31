using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Animal;

public sealed class TbFeed
{
	private readonly Dictionary<string, FeedInfo> _dataMap;

	private readonly List<FeedInfo> _dataList;

	public Dictionary<string, FeedInfo> DataMap => _dataMap;

	public List<FeedInfo> DataList => _dataList;

	public FeedInfo this[string key] => _dataMap[key];

	public TbFeed(JSONNode _json)
	{
		_dataMap = new Dictionary<string, FeedInfo>();
		_dataList = new List<FeedInfo>();
		foreach (JSONNode child in _json.Children)
		{
			FeedInfo feedInfo = FeedInfo.DeserializeFeedInfo(child);
			if (_dataMap.TryAdd(feedInfo.Id, feedInfo))
			{
				_dataList.Add(feedInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + feedInfo.Id + " in table: TbFeed");
			}
		}
	}

	public FeedInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public FeedInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (FeedInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (FeedInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}

	public bool IsFeederFeeds(string name, out int energy)
	{
		energy = 0;
		if (name.IsNullOrEmpty())
		{
			return false;
		}
		if (!DataMap.TryGetValue(name, out var value))
		{
			return false;
		}
		energy = value.Energy;
		return energy > 0;
	}
}
