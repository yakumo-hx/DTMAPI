using System;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.NPC;

public sealed class TbIdleTalkNode
{
	private readonly Dictionary<string, IdleTalkNodeInfo> _dataMap;

	private readonly List<IdleTalkNodeInfo> _dataList;

	private Dictionary<string, List<IdleTalkNodeInfo>> npcNodeCache = new Dictionary<string, List<IdleTalkNodeInfo>>();

	public Dictionary<string, IdleTalkNodeInfo> DataMap => _dataMap;

	public List<IdleTalkNodeInfo> DataList => _dataList;

	public IdleTalkNodeInfo this[string key] => _dataMap[key];

	public TbIdleTalkNode(JSONNode _json)
	{
		_dataMap = new Dictionary<string, IdleTalkNodeInfo>();
		_dataList = new List<IdleTalkNodeInfo>();
		foreach (JSONNode child in _json.Children)
		{
			IdleTalkNodeInfo idleTalkNodeInfo = IdleTalkNodeInfo.DeserializeIdleTalkNodeInfo(child);
			if (_dataMap.TryAdd(idleTalkNodeInfo.Id, idleTalkNodeInfo))
			{
				_dataList.Add(idleTalkNodeInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + idleTalkNodeInfo.Id + " in table: TbIdleTalkNode");
			}
		}
	}

	public IdleTalkNodeInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public IdleTalkNodeInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (IdleTalkNodeInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
		PostResolve();
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (IdleTalkNodeInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}

	private void PostResolve()
	{
		Dictionary<string, List<IdleTalkNodeInfo>> dictionary = new Dictionary<string, List<IdleTalkNodeInfo>>();
		foreach (IdleTalkNodeInfo data in _dataList)
		{
			dictionary.TryAdd(data.NpcName, new List<IdleTalkNodeInfo>());
			dictionary[data.NpcName].Add(data);
		}
		foreach (string key in dictionary.Keys)
		{
			npcNodeCache[key] = dictionary[key].OrderByDescending((IdleTalkNodeInfo x) => x.Order).ToList();
		}
	}

	public List<IdleTalkNodeInfo> GetNpcNodeByOrder(string npcName)
	{
		if (npcNodeCache.TryGetValue(npcName, out var value))
		{
			return value;
		}
		return new List<IdleTalkNodeInfo>();
	}
}
