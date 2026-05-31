using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.UI;

public sealed class TbUIEntity
{
	private readonly Dictionary<string, UIEntityInfo> _dataMap;

	private readonly List<UIEntityInfo> _dataList;

	public Dictionary<string, UIEntityInfo> DataMap => _dataMap;

	public List<UIEntityInfo> DataList => _dataList;

	public UIEntityInfo this[string key] => _dataMap[key];

	public TbUIEntity(JSONNode _json)
	{
		_dataMap = new Dictionary<string, UIEntityInfo>();
		_dataList = new List<UIEntityInfo>();
		foreach (JSONNode child in _json.Children)
		{
			UIEntityInfo uIEntityInfo = UIEntityInfo.DeserializeUIEntityInfo(child);
			if (_dataMap.TryAdd(uIEntityInfo.Id, uIEntityInfo))
			{
				_dataList.Add(uIEntityInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + uIEntityInfo.Id + " in table: TbUIEntity");
			}
		}
	}

	public UIEntityInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public UIEntityInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (UIEntityInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (UIEntityInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
