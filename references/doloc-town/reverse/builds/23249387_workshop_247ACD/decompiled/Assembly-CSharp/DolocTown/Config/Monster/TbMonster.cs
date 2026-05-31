using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Monster;

public sealed class TbMonster
{
	private readonly Dictionary<string, MonsterInfo> _dataMap;

	private readonly List<MonsterInfo> _dataList;

	public Dictionary<string, MonsterInfo> DataMap => _dataMap;

	public List<MonsterInfo> DataList => _dataList;

	public MonsterInfo this[string key] => _dataMap[key];

	public TbMonster(JSONNode _json)
	{
		_dataMap = new Dictionary<string, MonsterInfo>();
		_dataList = new List<MonsterInfo>();
		foreach (JSONNode child in _json.Children)
		{
			MonsterInfo monsterInfo = MonsterInfo.DeserializeMonsterInfo(child);
			if (_dataMap.TryAdd(monsterInfo.Id, monsterInfo))
			{
				_dataList.Add(monsterInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + monsterInfo.Id + " in table: TbMonster");
			}
		}
	}

	public MonsterInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public MonsterInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (MonsterInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (MonsterInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
