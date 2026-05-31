using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Sound;

public sealed class TbFootStep
{
	private readonly Dictionary<string, FootStepInfo> _dataMap;

	private readonly List<FootStepInfo> _dataList;

	public Dictionary<string, FootStepInfo> DataMap => _dataMap;

	public List<FootStepInfo> DataList => _dataList;

	public FootStepInfo this[string key] => _dataMap[key];

	public TbFootStep(JSONNode _json)
	{
		_dataMap = new Dictionary<string, FootStepInfo>();
		_dataList = new List<FootStepInfo>();
		foreach (JSONNode child in _json.Children)
		{
			FootStepInfo footStepInfo = FootStepInfo.DeserializeFootStepInfo(child);
			if (_dataMap.TryAdd(footStepInfo.Id, footStepInfo))
			{
				_dataList.Add(footStepInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + footStepInfo.Id + " in table: TbFootStep");
			}
		}
	}

	public FootStepInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public FootStepInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (FootStepInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (FootStepInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
