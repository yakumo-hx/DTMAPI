using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Plant;

public sealed class TbSeed
{
	private readonly Dictionary<string, SeedInfo> _dataMap;

	private readonly List<SeedInfo> _dataList;

	public Dictionary<string, SeedInfo> DataMap => _dataMap;

	public List<SeedInfo> DataList => _dataList;

	public SeedInfo this[string key] => _dataMap[key];

	public TbSeed(JSONNode _json)
	{
		_dataMap = new Dictionary<string, SeedInfo>();
		_dataList = new List<SeedInfo>();
		foreach (JSONNode child in _json.Children)
		{
			SeedInfo seedInfo = SeedInfo.DeserializeSeedInfo(child);
			if (_dataMap.TryAdd(seedInfo.Id, seedInfo))
			{
				_dataList.Add(seedInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + seedInfo.Id + " in table: TbSeed");
			}
		}
	}

	public SeedInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public SeedInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (SeedInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (SeedInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
