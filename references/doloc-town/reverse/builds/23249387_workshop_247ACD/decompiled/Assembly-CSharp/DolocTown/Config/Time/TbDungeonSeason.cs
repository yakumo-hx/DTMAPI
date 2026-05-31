using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Time;

public sealed class TbDungeonSeason
{
	private readonly Dictionary<string, DungeonSeasonInfo> _dataMap;

	private readonly List<DungeonSeasonInfo> _dataList;

	public Dictionary<string, DungeonSeasonInfo> DataMap => _dataMap;

	public List<DungeonSeasonInfo> DataList => _dataList;

	public DungeonSeasonInfo this[string key] => _dataMap[key];

	public TbDungeonSeason(JSONNode _json)
	{
		_dataMap = new Dictionary<string, DungeonSeasonInfo>();
		_dataList = new List<DungeonSeasonInfo>();
		foreach (JSONNode child in _json.Children)
		{
			DungeonSeasonInfo dungeonSeasonInfo = DungeonSeasonInfo.DeserializeDungeonSeasonInfo(child);
			if (_dataMap.TryAdd(dungeonSeasonInfo.Id, dungeonSeasonInfo))
			{
				_dataList.Add(dungeonSeasonInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + dungeonSeasonInfo.Id + " in table: TbDungeonSeason");
			}
		}
	}

	public DungeonSeasonInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public DungeonSeasonInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (DungeonSeasonInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (DungeonSeasonInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
