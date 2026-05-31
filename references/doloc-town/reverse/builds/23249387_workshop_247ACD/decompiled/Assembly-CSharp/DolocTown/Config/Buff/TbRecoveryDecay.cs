using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Buff;

public sealed class TbRecoveryDecay
{
	private readonly Dictionary<int, RecoveryDecayInfo> _dataMap;

	private readonly List<RecoveryDecayInfo> _dataList;

	public Dictionary<int, RecoveryDecayInfo> DataMap => _dataMap;

	public List<RecoveryDecayInfo> DataList => _dataList;

	public RecoveryDecayInfo this[int key] => _dataMap[key];

	public TbRecoveryDecay(JSONNode _json)
	{
		_dataMap = new Dictionary<int, RecoveryDecayInfo>();
		_dataList = new List<RecoveryDecayInfo>();
		foreach (JSONNode child in _json.Children)
		{
			RecoveryDecayInfo recoveryDecayInfo = RecoveryDecayInfo.DeserializeRecoveryDecayInfo(child);
			if (_dataMap.TryAdd(recoveryDecayInfo.TimeSinceAwake, recoveryDecayInfo))
			{
				_dataList.Add(recoveryDecayInfo);
			}
			else
			{
				Debug.LogError($"[Config] Duplicate key: {recoveryDecayInfo.TimeSinceAwake} in table: TbRecoveryDecay");
			}
		}
	}

	public RecoveryDecayInfo GetOrDefault(int key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public RecoveryDecayInfo Get(int key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (RecoveryDecayInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (RecoveryDecayInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
