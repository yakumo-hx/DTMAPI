using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Recipe;

public sealed class TbDishGroup
{
	private readonly Dictionary<string, DishGroupInfo> _dataMap;

	private readonly List<DishGroupInfo> _dataList;

	public Dictionary<string, DishGroupInfo> DataMap => _dataMap;

	public List<DishGroupInfo> DataList => _dataList;

	public DishGroupInfo this[string key] => _dataMap[key];

	public TbDishGroup(JSONNode _json)
	{
		_dataMap = new Dictionary<string, DishGroupInfo>();
		_dataList = new List<DishGroupInfo>();
		foreach (JSONNode child in _json.Children)
		{
			DishGroupInfo dishGroupInfo = DishGroupInfo.DeserializeDishGroupInfo(child);
			if (_dataMap.TryAdd(dishGroupInfo.Id, dishGroupInfo))
			{
				_dataList.Add(dishGroupInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + dishGroupInfo.Id + " in table: TbDishGroup");
			}
		}
	}

	public DishGroupInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public DishGroupInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (DishGroupInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (DishGroupInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
