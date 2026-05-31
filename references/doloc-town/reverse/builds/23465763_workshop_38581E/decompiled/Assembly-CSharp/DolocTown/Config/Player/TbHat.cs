using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Player;

public sealed class TbHat
{
	private readonly Dictionary<string, HatInfo> _dataMap;

	private readonly List<HatInfo> _dataList;

	public Dictionary<string, HatInfo> DataMap => _dataMap;

	public List<HatInfo> DataList => _dataList;

	public HatInfo this[string key] => _dataMap[key];

	public TbHat(JSONNode _json)
	{
		_dataMap = new Dictionary<string, HatInfo>();
		_dataList = new List<HatInfo>();
		foreach (JSONNode child in _json.Children)
		{
			HatInfo hatInfo = HatInfo.DeserializeHatInfo(child);
			if (_dataMap.TryAdd(hatInfo.Id, hatInfo))
			{
				_dataList.Add(hatInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + hatInfo.Id + " in table: TbHat");
			}
		}
	}

	public HatInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public HatInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (HatInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (HatInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
