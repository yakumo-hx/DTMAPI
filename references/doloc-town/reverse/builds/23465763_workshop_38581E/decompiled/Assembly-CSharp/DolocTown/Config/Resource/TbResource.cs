using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Resource;

public sealed class TbResource
{
	private readonly Dictionary<string, ResourceInfo> _dataMap;

	private readonly List<ResourceInfo> _dataList;

	public Dictionary<string, ResourceInfo> DataMap => _dataMap;

	public List<ResourceInfo> DataList => _dataList;

	public ResourceInfo this[string key] => _dataMap[key];

	public TbResource(JSONNode _json)
	{
		_dataMap = new Dictionary<string, ResourceInfo>();
		_dataList = new List<ResourceInfo>();
		foreach (JSONNode child in _json.Children)
		{
			ResourceInfo resourceInfo = ResourceInfo.DeserializeResourceInfo(child);
			if (_dataMap.TryAdd(resourceInfo.Id, resourceInfo))
			{
				_dataList.Add(resourceInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + resourceInfo.Id + " in table: TbResource");
			}
		}
	}

	public ResourceInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public ResourceInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (ResourceInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (ResourceInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
