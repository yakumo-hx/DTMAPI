using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.NPC;

public sealed class TbLikingLevelMap
{
	private readonly Dictionary<int, LikingLevelMapInfo> _dataMap;

	private readonly List<LikingLevelMapInfo> _dataList;

	public Dictionary<int, LikingLevelMapInfo> DataMap => _dataMap;

	public List<LikingLevelMapInfo> DataList => _dataList;

	public LikingLevelMapInfo this[int key] => _dataMap[key];

	public TbLikingLevelMap(JSONNode _json)
	{
		_dataMap = new Dictionary<int, LikingLevelMapInfo>();
		_dataList = new List<LikingLevelMapInfo>();
		foreach (JSONNode child in _json.Children)
		{
			LikingLevelMapInfo likingLevelMapInfo = LikingLevelMapInfo.DeserializeLikingLevelMapInfo(child);
			if (_dataMap.TryAdd(likingLevelMapInfo.Level, likingLevelMapInfo))
			{
				_dataList.Add(likingLevelMapInfo);
			}
			else
			{
				Debug.LogError($"[Config] Duplicate key: {likingLevelMapInfo.Level} in table: TbLikingLevelMap");
			}
		}
	}

	public LikingLevelMapInfo GetOrDefault(int key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public LikingLevelMapInfo Get(int key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (LikingLevelMapInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (LikingLevelMapInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
