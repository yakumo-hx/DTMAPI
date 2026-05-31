using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Resource;

public sealed class TbResourceType
{
	private readonly Dictionary<DungeonResourceType, ResourceTypeInfo> _dataMap;

	private readonly List<ResourceTypeInfo> _dataList;

	public Dictionary<DungeonResourceType, ResourceTypeInfo> DataMap => _dataMap;

	public List<ResourceTypeInfo> DataList => _dataList;

	public ResourceTypeInfo this[DungeonResourceType key] => _dataMap[key];

	public TbResourceType(JSONNode _json)
	{
		_dataMap = new Dictionary<DungeonResourceType, ResourceTypeInfo>();
		_dataList = new List<ResourceTypeInfo>();
		foreach (JSONNode child in _json.Children)
		{
			ResourceTypeInfo resourceTypeInfo = ResourceTypeInfo.DeserializeResourceTypeInfo(child);
			if (_dataMap.TryAdd(resourceTypeInfo.ResourceType, resourceTypeInfo))
			{
				_dataList.Add(resourceTypeInfo);
			}
			else
			{
				Debug.LogError($"[Config] Duplicate key: {resourceTypeInfo.ResourceType} in table: TbResourceType");
			}
		}
	}

	public ResourceTypeInfo GetOrDefault(DungeonResourceType key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public ResourceTypeInfo Get(DungeonResourceType key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (ResourceTypeInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (ResourceTypeInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
