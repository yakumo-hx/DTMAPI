using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Buff;

public sealed class TbBuff
{
	private readonly Dictionary<string, BuffInfo> _dataMap;

	private readonly List<BuffInfo> _dataList;

	public Dictionary<string, BuffInfo> DataMap => _dataMap;

	public List<BuffInfo> DataList => _dataList;

	public BuffInfo this[string key] => _dataMap[key];

	public TbBuff(JSONNode _json)
	{
		_dataMap = new Dictionary<string, BuffInfo>();
		_dataList = new List<BuffInfo>();
		foreach (JSONNode child in _json.Children)
		{
			BuffInfo buffInfo = BuffInfo.DeserializeBuffInfo(child);
			if (_dataMap.TryAdd(buffInfo.Id, buffInfo))
			{
				_dataList.Add(buffInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + buffInfo.Id + " in table: TbBuff");
			}
		}
	}

	public BuffInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public BuffInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (BuffInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (BuffInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
