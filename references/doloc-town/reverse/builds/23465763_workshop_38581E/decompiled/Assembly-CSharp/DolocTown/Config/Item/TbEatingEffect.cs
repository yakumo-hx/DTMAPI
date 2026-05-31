using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Item;

public sealed class TbEatingEffect
{
	private readonly Dictionary<string, EatingEffectInfo> _dataMap;

	private readonly List<EatingEffectInfo> _dataList;

	public Dictionary<string, EatingEffectInfo> DataMap => _dataMap;

	public List<EatingEffectInfo> DataList => _dataList;

	public EatingEffectInfo this[string key] => _dataMap[key];

	public TbEatingEffect(JSONNode _json)
	{
		_dataMap = new Dictionary<string, EatingEffectInfo>();
		_dataList = new List<EatingEffectInfo>();
		foreach (JSONNode child in _json.Children)
		{
			EatingEffectInfo eatingEffectInfo = EatingEffectInfo.DeserializeEatingEffectInfo(child);
			if (_dataMap.TryAdd(eatingEffectInfo.Id, eatingEffectInfo))
			{
				_dataList.Add(eatingEffectInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + eatingEffectInfo.Id + " in table: TbEatingEffect");
			}
		}
	}

	public EatingEffectInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public EatingEffectInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (EatingEffectInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (EatingEffectInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
