using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Player;

public sealed class TbBackpackLevel
{
	private readonly List<BackpackLevelInfo> _dataList;

	private Dictionary<int, BackpackLevelInfo> _dataMap_level;

	private Dictionary<int, BackpackLevelInfo> _dataMap_capacity;

	public List<BackpackLevelInfo> DataList => _dataList;

	public TbBackpackLevel(JSONNode _json)
	{
		_dataList = new List<BackpackLevelInfo>();
		foreach (JSONNode child in _json.Children)
		{
			BackpackLevelInfo item = BackpackLevelInfo.DeserializeBackpackLevelInfo(child);
			_dataList.Add(item);
		}
		_dataMap_level = new Dictionary<int, BackpackLevelInfo>();
		_dataMap_capacity = new Dictionary<int, BackpackLevelInfo>();
		foreach (BackpackLevelInfo data in _dataList)
		{
			if (!_dataMap_level.TryAdd(data.Level, data))
			{
				Debug.LogError($"[Config] Duplicate key: {data.Level} in table: TbBackpackLevel");
			}
			if (!_dataMap_capacity.TryAdd(data.Capacity, data))
			{
				Debug.LogError($"[Config] Duplicate key: {data.Capacity} in table: TbBackpackLevel");
			}
		}
	}

	public BackpackLevelInfo GetByLevel(int key)
	{
		if (!_dataMap_level.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public BackpackLevelInfo GetByCapacity(int key)
	{
		if (!_dataMap_capacity.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (BackpackLevelInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (BackpackLevelInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
