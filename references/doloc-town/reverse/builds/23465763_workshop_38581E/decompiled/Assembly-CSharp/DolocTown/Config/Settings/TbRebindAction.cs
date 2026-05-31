using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Settings;

public sealed class TbRebindAction
{
	private readonly Dictionary<string, RebindActionInfo> _dataMap;

	private readonly List<RebindActionInfo> _dataList;

	public Dictionary<string, RebindActionInfo> DataMap => _dataMap;

	public List<RebindActionInfo> DataList => _dataList;

	public RebindActionInfo this[string key] => _dataMap[key];

	public TbRebindAction(JSONNode _json)
	{
		_dataMap = new Dictionary<string, RebindActionInfo>();
		_dataList = new List<RebindActionInfo>();
		foreach (JSONNode child in _json.Children)
		{
			RebindActionInfo rebindActionInfo = RebindActionInfo.DeserializeRebindActionInfo(child);
			if (_dataMap.TryAdd(rebindActionInfo.Id, rebindActionInfo))
			{
				_dataList.Add(rebindActionInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + rebindActionInfo.Id + " in table: TbRebindAction");
			}
		}
	}

	public RebindActionInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public RebindActionInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (RebindActionInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (RebindActionInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
