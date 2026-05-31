using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Room;

public sealed class TbMapType
{
	private readonly Dictionary<string, MapTypeInfo> _dataMap;

	private readonly List<MapTypeInfo> _dataList;

	public Dictionary<string, MapTypeInfo> DataMap => _dataMap;

	public List<MapTypeInfo> DataList => _dataList;

	public MapTypeInfo this[string key] => _dataMap[key];

	public TbMapType(JSONNode _json)
	{
		_dataMap = new Dictionary<string, MapTypeInfo>();
		_dataList = new List<MapTypeInfo>();
		foreach (JSONNode child in _json.Children)
		{
			MapTypeInfo mapTypeInfo = MapTypeInfo.DeserializeMapTypeInfo(child);
			if (_dataMap.TryAdd(mapTypeInfo.Id, mapTypeInfo))
			{
				_dataList.Add(mapTypeInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + mapTypeInfo.Id + " in table: TbMapType");
			}
		}
	}

	public MapTypeInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public MapTypeInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (MapTypeInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (MapTypeInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
