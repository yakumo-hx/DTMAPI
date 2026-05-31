using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.NPC;

public sealed class TbNpc
{
	private readonly Dictionary<string, NpcInfo> _dataMap;

	private readonly List<NpcInfo> _dataList;

	public Dictionary<string, NpcInfo> DataMap => _dataMap;

	public List<NpcInfo> DataList => _dataList;

	public NpcInfo this[string key] => _dataMap[key];

	public TbNpc(JSONNode _json)
	{
		_dataMap = new Dictionary<string, NpcInfo>();
		_dataList = new List<NpcInfo>();
		foreach (JSONNode child in _json.Children)
		{
			NpcInfo npcInfo = NpcInfo.DeserializeNpcInfo(child);
			if (_dataMap.TryAdd(npcInfo.Id, npcInfo))
			{
				_dataList.Add(npcInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + npcInfo.Id + " in table: TbNpc");
			}
		}
	}

	public NpcInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public NpcInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (NpcInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (NpcInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
