using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.UI;

public sealed class TbUIPooledObject
{
	private readonly Dictionary<string, UIPooledObjectInfo> _dataMap;

	private readonly List<UIPooledObjectInfo> _dataList;

	public Dictionary<string, UIPooledObjectInfo> DataMap => _dataMap;

	public List<UIPooledObjectInfo> DataList => _dataList;

	public UIPooledObjectInfo this[string key] => _dataMap[key];

	public TbUIPooledObject(JSONNode _json)
	{
		_dataMap = new Dictionary<string, UIPooledObjectInfo>();
		_dataList = new List<UIPooledObjectInfo>();
		foreach (JSONNode child in _json.Children)
		{
			UIPooledObjectInfo uIPooledObjectInfo = UIPooledObjectInfo.DeserializeUIPooledObjectInfo(child);
			if (_dataMap.TryAdd(uIPooledObjectInfo.Id, uIPooledObjectInfo))
			{
				_dataList.Add(uIPooledObjectInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + uIPooledObjectInfo.Id + " in table: TbUIPooledObject");
			}
		}
	}

	public UIPooledObjectInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public UIPooledObjectInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (UIPooledObjectInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (UIPooledObjectInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
