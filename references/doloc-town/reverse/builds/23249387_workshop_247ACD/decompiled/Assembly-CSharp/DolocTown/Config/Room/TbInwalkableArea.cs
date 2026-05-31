using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Room;

public sealed class TbInwalkableArea
{
	private readonly Dictionary<string, InwalkableAreaInfo> _dataMap;

	private readonly List<InwalkableAreaInfo> _dataList;

	public Dictionary<string, InwalkableAreaInfo> DataMap => _dataMap;

	public List<InwalkableAreaInfo> DataList => _dataList;

	public InwalkableAreaInfo this[string key] => _dataMap[key];

	public TbInwalkableArea(JSONNode _json)
	{
		_dataMap = new Dictionary<string, InwalkableAreaInfo>();
		_dataList = new List<InwalkableAreaInfo>();
		foreach (JSONNode child in _json.Children)
		{
			InwalkableAreaInfo inwalkableAreaInfo = InwalkableAreaInfo.DeserializeInwalkableAreaInfo(child);
			if (_dataMap.TryAdd(inwalkableAreaInfo.Id, inwalkableAreaInfo))
			{
				_dataList.Add(inwalkableAreaInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + inwalkableAreaInfo.Id + " in table: TbInwalkableArea");
			}
		}
	}

	public InwalkableAreaInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public InwalkableAreaInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (InwalkableAreaInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (InwalkableAreaInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
