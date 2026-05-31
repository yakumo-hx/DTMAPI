using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.NPC;

public sealed class TbNpcLiking
{
	private readonly Dictionary<string, NpcLikingInfo> _dataMap;

	private readonly List<NpcLikingInfo> _dataList;

	public Dictionary<string, NpcLikingInfo> DataMap => _dataMap;

	public List<NpcLikingInfo> DataList => _dataList;

	public NpcLikingInfo this[string key] => _dataMap[key];

	public TbNpcLiking(JSONNode _json)
	{
		_dataMap = new Dictionary<string, NpcLikingInfo>();
		_dataList = new List<NpcLikingInfo>();
		foreach (JSONNode child in _json.Children)
		{
			NpcLikingInfo npcLikingInfo = NpcLikingInfo.DeserializeNpcLikingInfo(child);
			if (_dataMap.TryAdd(npcLikingInfo.Id, npcLikingInfo))
			{
				_dataList.Add(npcLikingInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + npcLikingInfo.Id + " in table: TbNpcLiking");
			}
		}
	}

	public NpcLikingInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public NpcLikingInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (NpcLikingInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (NpcLikingInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
