using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Mission;

public sealed class TbRewardPool
{
	private readonly Dictionary<string, RewardPoolInfo> _dataMap;

	private readonly List<RewardPoolInfo> _dataList;

	public Dictionary<string, RewardPoolInfo> DataMap => _dataMap;

	public List<RewardPoolInfo> DataList => _dataList;

	public RewardPoolInfo this[string key] => _dataMap[key];

	public TbRewardPool(JSONNode _json)
	{
		_dataMap = new Dictionary<string, RewardPoolInfo>();
		_dataList = new List<RewardPoolInfo>();
		foreach (JSONNode child in _json.Children)
		{
			RewardPoolInfo rewardPoolInfo = RewardPoolInfo.DeserializeRewardPoolInfo(child);
			if (_dataMap.TryAdd(rewardPoolInfo.Id, rewardPoolInfo))
			{
				_dataList.Add(rewardPoolInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + rewardPoolInfo.Id + " in table: TbRewardPool");
			}
		}
	}

	public RewardPoolInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public RewardPoolInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (RewardPoolInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (RewardPoolInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
