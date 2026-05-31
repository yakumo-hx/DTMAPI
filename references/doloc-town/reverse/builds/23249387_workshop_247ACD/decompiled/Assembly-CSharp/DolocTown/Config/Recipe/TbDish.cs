using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Recipe;

public sealed class TbDish
{
	private readonly Dictionary<string, DishInfo> _dataMap;

	private readonly List<DishInfo> _dataList;

	public Dictionary<string, DishInfo> DataMap => _dataMap;

	public List<DishInfo> DataList => _dataList;

	public DishInfo this[string key] => _dataMap[key];

	public TbDish(JSONNode _json)
	{
		_dataMap = new Dictionary<string, DishInfo>();
		_dataList = new List<DishInfo>();
		foreach (JSONNode child in _json.Children)
		{
			DishInfo dishInfo = DishInfo.DeserializeDishInfo(child);
			if (_dataMap.TryAdd(dishInfo.Id, dishInfo))
			{
				_dataList.Add(dishInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + dishInfo.Id + " in table: TbDish");
			}
		}
	}

	public DishInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public DishInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (DishInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (DishInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
