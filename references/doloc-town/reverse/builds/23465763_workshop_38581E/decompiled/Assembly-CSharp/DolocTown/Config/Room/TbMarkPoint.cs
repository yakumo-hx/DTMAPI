using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Room;

public sealed class TbMarkPoint
{
	private readonly Dictionary<string, MarkPointInfo> _dataMap;

	private readonly List<MarkPointInfo> _dataList;

	public Dictionary<string, MarkPointInfo> DataMap => _dataMap;

	public List<MarkPointInfo> DataList => _dataList;

	public MarkPointInfo this[string key] => _dataMap[key];

	public TbMarkPoint(JSONNode _json)
	{
		_dataMap = new Dictionary<string, MarkPointInfo>();
		_dataList = new List<MarkPointInfo>();
		foreach (JSONNode child in _json.Children)
		{
			MarkPointInfo markPointInfo = MarkPointInfo.DeserializeMarkPointInfo(child);
			if (_dataMap.TryAdd(markPointInfo.Id, markPointInfo))
			{
				_dataList.Add(markPointInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + markPointInfo.Id + " in table: TbMarkPoint");
			}
		}
	}

	public MarkPointInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public MarkPointInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (MarkPointInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (MarkPointInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
