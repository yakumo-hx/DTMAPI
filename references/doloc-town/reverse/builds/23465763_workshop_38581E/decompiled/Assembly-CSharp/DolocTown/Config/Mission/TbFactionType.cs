using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Mission;

public sealed class TbFactionType
{
	private readonly Dictionary<FactionType, FactionTypeInfo> _dataMap;

	private readonly List<FactionTypeInfo> _dataList;

	public Dictionary<FactionType, FactionTypeInfo> DataMap => _dataMap;

	public List<FactionTypeInfo> DataList => _dataList;

	public FactionTypeInfo this[FactionType key] => _dataMap[key];

	public TbFactionType(JSONNode _json)
	{
		_dataMap = new Dictionary<FactionType, FactionTypeInfo>();
		_dataList = new List<FactionTypeInfo>();
		foreach (JSONNode child in _json.Children)
		{
			FactionTypeInfo factionTypeInfo = FactionTypeInfo.DeserializeFactionTypeInfo(child);
			if (_dataMap.TryAdd(factionTypeInfo.Id, factionTypeInfo))
			{
				_dataList.Add(factionTypeInfo);
			}
			else
			{
				Debug.LogError($"[Config] Duplicate key: {factionTypeInfo.Id} in table: TbFactionType");
			}
		}
	}

	public FactionTypeInfo GetOrDefault(FactionType key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public FactionTypeInfo Get(FactionType key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (FactionTypeInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (FactionTypeInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
