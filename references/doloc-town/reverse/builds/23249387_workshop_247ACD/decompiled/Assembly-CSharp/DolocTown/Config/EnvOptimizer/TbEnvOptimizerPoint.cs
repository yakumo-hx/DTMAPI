using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.EnvOptimizer;

public sealed class TbEnvOptimizerPoint
{
	private readonly Dictionary<EnvOptimizerBehaviourType, EnvOptimizerPointInfo> _dataMap;

	private readonly List<EnvOptimizerPointInfo> _dataList;

	public Dictionary<EnvOptimizerBehaviourType, EnvOptimizerPointInfo> DataMap => _dataMap;

	public List<EnvOptimizerPointInfo> DataList => _dataList;

	public EnvOptimizerPointInfo this[EnvOptimizerBehaviourType key] => _dataMap[key];

	public TbEnvOptimizerPoint(JSONNode _json)
	{
		_dataMap = new Dictionary<EnvOptimizerBehaviourType, EnvOptimizerPointInfo>();
		_dataList = new List<EnvOptimizerPointInfo>();
		foreach (JSONNode child in _json.Children)
		{
			EnvOptimizerPointInfo envOptimizerPointInfo = EnvOptimizerPointInfo.DeserializeEnvOptimizerPointInfo(child);
			if (_dataMap.TryAdd(envOptimizerPointInfo.Id, envOptimizerPointInfo))
			{
				_dataList.Add(envOptimizerPointInfo);
			}
			else
			{
				Debug.LogError($"[Config] Duplicate key: {envOptimizerPointInfo.Id} in table: TbEnvOptimizerPoint");
			}
		}
	}

	public EnvOptimizerPointInfo GetOrDefault(EnvOptimizerBehaviourType key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public EnvOptimizerPointInfo Get(EnvOptimizerBehaviourType key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (EnvOptimizerPointInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (EnvOptimizerPointInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
