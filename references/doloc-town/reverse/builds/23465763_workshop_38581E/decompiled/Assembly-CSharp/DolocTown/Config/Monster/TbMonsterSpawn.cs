using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Monster;

public sealed class TbMonsterSpawn
{
	private readonly Dictionary<string, MonsterSpawnInfo> _dataMap;

	private readonly List<MonsterSpawnInfo> _dataList;

	public Dictionary<string, MonsterSpawnInfo> DataMap => _dataMap;

	public List<MonsterSpawnInfo> DataList => _dataList;

	public MonsterSpawnInfo this[string key] => _dataMap[key];

	public TbMonsterSpawn(JSONNode _json)
	{
		_dataMap = new Dictionary<string, MonsterSpawnInfo>();
		_dataList = new List<MonsterSpawnInfo>();
		foreach (JSONNode child in _json.Children)
		{
			MonsterSpawnInfo monsterSpawnInfo = MonsterSpawnInfo.DeserializeMonsterSpawnInfo(child);
			if (_dataMap.TryAdd(monsterSpawnInfo.Id, monsterSpawnInfo))
			{
				_dataList.Add(monsterSpawnInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + monsterSpawnInfo.Id + " in table: TbMonsterSpawn");
			}
		}
	}

	public MonsterSpawnInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public MonsterSpawnInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (MonsterSpawnInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (MonsterSpawnInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
