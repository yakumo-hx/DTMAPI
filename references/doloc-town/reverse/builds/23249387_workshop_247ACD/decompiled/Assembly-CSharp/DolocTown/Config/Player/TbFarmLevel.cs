using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Player;

public sealed class TbFarmLevel
{
	private readonly List<FarmLevelInfo> _dataList;

	private Dictionary<int, FarmLevelInfo> _dataMap_level;

	private Dictionary<string, FarmLevelInfo> _dataMap_id;

	public List<FarmLevelInfo> DataList => _dataList;

	public TbFarmLevel(JSONNode _json)
	{
		_dataList = new List<FarmLevelInfo>();
		foreach (JSONNode child in _json.Children)
		{
			FarmLevelInfo item = FarmLevelInfo.DeserializeFarmLevelInfo(child);
			_dataList.Add(item);
		}
		_dataMap_level = new Dictionary<int, FarmLevelInfo>();
		_dataMap_id = new Dictionary<string, FarmLevelInfo>();
		foreach (FarmLevelInfo data in _dataList)
		{
			if (!_dataMap_level.TryAdd(data.Level, data))
			{
				Debug.LogError($"[Config] Duplicate key: {data.Level} in table: TbFarmLevel");
			}
			if (!_dataMap_id.TryAdd(data.Id, data))
			{
				Debug.LogError("[Config] Duplicate key: " + data.Id + " in table: TbFarmLevel");
			}
		}
	}

	public FarmLevelInfo GetByLevel(int key)
	{
		if (!_dataMap_level.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public FarmLevelInfo GetById(string key)
	{
		if (!_dataMap_id.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (FarmLevelInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (FarmLevelInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
