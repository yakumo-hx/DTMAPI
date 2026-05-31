using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.EnvOptimizer;

public sealed class TbEnvOptimizerBranch
{
	private readonly Dictionary<EnvOptimizerBranchType, EnvOptimizerBranchInfo> _dataMap;

	private readonly List<EnvOptimizerBranchInfo> _dataList;

	public Dictionary<EnvOptimizerBranchType, EnvOptimizerBranchInfo> DataMap => _dataMap;

	public List<EnvOptimizerBranchInfo> DataList => _dataList;

	public EnvOptimizerBranchInfo this[EnvOptimizerBranchType key] => _dataMap[key];

	public TbEnvOptimizerBranch(JSONNode _json)
	{
		_dataMap = new Dictionary<EnvOptimizerBranchType, EnvOptimizerBranchInfo>();
		_dataList = new List<EnvOptimizerBranchInfo>();
		foreach (JSONNode child in _json.Children)
		{
			EnvOptimizerBranchInfo envOptimizerBranchInfo = EnvOptimizerBranchInfo.DeserializeEnvOptimizerBranchInfo(child);
			if (_dataMap.TryAdd(envOptimizerBranchInfo.Id, envOptimizerBranchInfo))
			{
				_dataList.Add(envOptimizerBranchInfo);
			}
			else
			{
				Debug.LogError($"[Config] Duplicate key: {envOptimizerBranchInfo.Id} in table: TbEnvOptimizerBranch");
			}
		}
	}

	public EnvOptimizerBranchInfo GetOrDefault(EnvOptimizerBranchType key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public EnvOptimizerBranchInfo Get(EnvOptimizerBranchType key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (EnvOptimizerBranchInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (EnvOptimizerBranchInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
