using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Resource;

public sealed class TbResourceDocument
{
	private readonly Dictionary<string, ResourceDocumentInfo> _dataMap;

	private readonly List<ResourceDocumentInfo> _dataList;

	public Dictionary<string, ResourceDocumentInfo> DataMap => _dataMap;

	public List<ResourceDocumentInfo> DataList => _dataList;

	public ResourceDocumentInfo this[string key] => _dataMap[key];

	public TbResourceDocument(JSONNode _json)
	{
		_dataMap = new Dictionary<string, ResourceDocumentInfo>();
		_dataList = new List<ResourceDocumentInfo>();
		foreach (JSONNode child in _json.Children)
		{
			ResourceDocumentInfo resourceDocumentInfo = ResourceDocumentInfo.DeserializeResourceDocumentInfo(child);
			if (_dataMap.TryAdd(resourceDocumentInfo.Id, resourceDocumentInfo))
			{
				_dataList.Add(resourceDocumentInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + resourceDocumentInfo.Id + " in table: TbResourceDocument");
			}
		}
	}

	public ResourceDocumentInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public ResourceDocumentInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (ResourceDocumentInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (ResourceDocumentInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
