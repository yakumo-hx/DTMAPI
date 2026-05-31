using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Resource;

public sealed class TbEnvObject
{
	private readonly Dictionary<string, EnvObjectInfo> _dataMap;

	private readonly List<EnvObjectInfo> _dataList;

	public Dictionary<string, EnvObjectInfo> DataMap => _dataMap;

	public List<EnvObjectInfo> DataList => _dataList;

	public EnvObjectInfo this[string key] => _dataMap[key];

	public TbEnvObject(JSONNode _json)
	{
		_dataMap = new Dictionary<string, EnvObjectInfo>();
		_dataList = new List<EnvObjectInfo>();
		foreach (JSONNode child in _json.Children)
		{
			EnvObjectInfo envObjectInfo = EnvObjectInfo.DeserializeEnvObjectInfo(child);
			if (_dataMap.TryAdd(envObjectInfo.Id, envObjectInfo))
			{
				_dataList.Add(envObjectInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + envObjectInfo.Id + " in table: TbEnvObject");
			}
		}
	}

	public EnvObjectInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public EnvObjectInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (EnvObjectInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (EnvObjectInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
