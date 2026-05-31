using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.TechTree;

public sealed class TbTechNode
{
	private readonly Dictionary<string, TechNodeInfo> _dataMap;

	private readonly List<TechNodeInfo> _dataList;

	public Dictionary<string, TechNodeInfo> DataMap => _dataMap;

	public List<TechNodeInfo> DataList => _dataList;

	public TechNodeInfo this[string key] => _dataMap[key];

	public TbTechNode(JSONNode _json)
	{
		_dataMap = new Dictionary<string, TechNodeInfo>();
		_dataList = new List<TechNodeInfo>();
		foreach (JSONNode child in _json.Children)
		{
			TechNodeInfo techNodeInfo = TechNodeInfo.DeserializeTechNodeInfo(child);
			if (_dataMap.TryAdd(techNodeInfo.Id, techNodeInfo))
			{
				_dataList.Add(techNodeInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + techNodeInfo.Id + " in table: TbTechNode");
			}
		}
	}

	public TechNodeInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public TechNodeInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (TechNodeInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (TechNodeInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
