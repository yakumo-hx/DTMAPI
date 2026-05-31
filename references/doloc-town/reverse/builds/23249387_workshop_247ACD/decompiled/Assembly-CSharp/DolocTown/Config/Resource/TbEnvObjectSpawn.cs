using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Resource;

public sealed class TbEnvObjectSpawn
{
	private readonly Dictionary<string, EnvObjectSpawnInfo> _dataMap;

	private readonly List<EnvObjectSpawnInfo> _dataList;

	public Dictionary<string, EnvObjectSpawnInfo> DataMap => _dataMap;

	public List<EnvObjectSpawnInfo> DataList => _dataList;

	public EnvObjectSpawnInfo this[string key] => _dataMap[key];

	public TbEnvObjectSpawn(JSONNode _json)
	{
		_dataMap = new Dictionary<string, EnvObjectSpawnInfo>();
		_dataList = new List<EnvObjectSpawnInfo>();
		foreach (JSONNode child in _json.Children)
		{
			EnvObjectSpawnInfo envObjectSpawnInfo = EnvObjectSpawnInfo.DeserializeEnvObjectSpawnInfo(child);
			if (_dataMap.TryAdd(envObjectSpawnInfo.Id, envObjectSpawnInfo))
			{
				_dataList.Add(envObjectSpawnInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + envObjectSpawnInfo.Id + " in table: TbEnvObjectSpawn");
			}
		}
	}

	public EnvObjectSpawnInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public EnvObjectSpawnInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (EnvObjectSpawnInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (EnvObjectSpawnInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
