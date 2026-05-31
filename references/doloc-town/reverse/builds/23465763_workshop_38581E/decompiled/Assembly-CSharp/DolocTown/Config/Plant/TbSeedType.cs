using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Plant;

public sealed class TbSeedType
{
	private readonly Dictionary<string, SeedTypeInfo> _dataMap;

	private readonly List<SeedTypeInfo> _dataList;

	public Dictionary<string, SeedTypeInfo> DataMap => _dataMap;

	public List<SeedTypeInfo> DataList => _dataList;

	public SeedTypeInfo this[string key] => _dataMap[key];

	public TbSeedType(JSONNode _json)
	{
		_dataMap = new Dictionary<string, SeedTypeInfo>();
		_dataList = new List<SeedTypeInfo>();
		foreach (JSONNode child in _json.Children)
		{
			SeedTypeInfo seedTypeInfo = SeedTypeInfo.DeserializeSeedTypeInfo(child);
			if (_dataMap.TryAdd(seedTypeInfo.Id, seedTypeInfo))
			{
				_dataList.Add(seedTypeInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + seedTypeInfo.Id + " in table: TbSeedType");
			}
		}
	}

	public SeedTypeInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public SeedTypeInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (SeedTypeInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (SeedTypeInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
