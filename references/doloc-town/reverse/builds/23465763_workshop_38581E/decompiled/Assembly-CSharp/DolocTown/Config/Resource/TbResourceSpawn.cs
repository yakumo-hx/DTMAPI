using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Resource;

public sealed class TbResourceSpawn
{
	private readonly Dictionary<string, ResourceSpawnInfo> _dataMap;

	private readonly List<ResourceSpawnInfo> _dataList;

	public Dictionary<string, ResourceSpawnInfo> DataMap => _dataMap;

	public List<ResourceSpawnInfo> DataList => _dataList;

	public ResourceSpawnInfo this[string key] => _dataMap[key];

	public TbResourceSpawn(JSONNode _json)
	{
		_dataMap = new Dictionary<string, ResourceSpawnInfo>();
		_dataList = new List<ResourceSpawnInfo>();
		foreach (JSONNode child in _json.Children)
		{
			ResourceSpawnInfo resourceSpawnInfo = ResourceSpawnInfo.DeserializeResourceSpawnInfo(child);
			if (_dataMap.TryAdd(resourceSpawnInfo.Id, resourceSpawnInfo))
			{
				_dataList.Add(resourceSpawnInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + resourceSpawnInfo.Id + " in table: TbResourceSpawn");
			}
		}
	}

	public ResourceSpawnInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public ResourceSpawnInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (ResourceSpawnInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (ResourceSpawnInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
