using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Plant;

public sealed class TbSeedUnlock
{
	private readonly Dictionary<string, SeedUnlockInfo> _dataMap;

	private readonly List<SeedUnlockInfo> _dataList;

	public Dictionary<string, SeedUnlockInfo> DataMap => _dataMap;

	public List<SeedUnlockInfo> DataList => _dataList;

	public SeedUnlockInfo this[string key] => _dataMap[key];

	public TbSeedUnlock(JSONNode _json)
	{
		_dataMap = new Dictionary<string, SeedUnlockInfo>();
		_dataList = new List<SeedUnlockInfo>();
		foreach (JSONNode child in _json.Children)
		{
			SeedUnlockInfo seedUnlockInfo = SeedUnlockInfo.DeserializeSeedUnlockInfo(child);
			if (_dataMap.TryAdd(seedUnlockInfo.Id, seedUnlockInfo))
			{
				_dataList.Add(seedUnlockInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + seedUnlockInfo.Id + " in table: TbSeedUnlock");
			}
		}
	}

	public SeedUnlockInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public SeedUnlockInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (SeedUnlockInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (SeedUnlockInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
