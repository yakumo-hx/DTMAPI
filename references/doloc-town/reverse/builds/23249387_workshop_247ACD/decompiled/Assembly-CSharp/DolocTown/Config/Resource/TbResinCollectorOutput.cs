using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Resource;

public sealed class TbResinCollectorOutput
{
	private readonly Dictionary<string, ResinCollectorOutputInfo> _dataMap;

	private readonly List<ResinCollectorOutputInfo> _dataList;

	public Dictionary<string, ResinCollectorOutputInfo> DataMap => _dataMap;

	public List<ResinCollectorOutputInfo> DataList => _dataList;

	public ResinCollectorOutputInfo this[string key] => _dataMap[key];

	public TbResinCollectorOutput(JSONNode _json)
	{
		_dataMap = new Dictionary<string, ResinCollectorOutputInfo>();
		_dataList = new List<ResinCollectorOutputInfo>();
		foreach (JSONNode child in _json.Children)
		{
			ResinCollectorOutputInfo resinCollectorOutputInfo = ResinCollectorOutputInfo.DeserializeResinCollectorOutputInfo(child);
			if (_dataMap.TryAdd(resinCollectorOutputInfo.Id, resinCollectorOutputInfo))
			{
				_dataList.Add(resinCollectorOutputInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + resinCollectorOutputInfo.Id + " in table: TbResinCollectorOutput");
			}
		}
	}

	public ResinCollectorOutputInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public ResinCollectorOutputInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (ResinCollectorOutputInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (ResinCollectorOutputInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
