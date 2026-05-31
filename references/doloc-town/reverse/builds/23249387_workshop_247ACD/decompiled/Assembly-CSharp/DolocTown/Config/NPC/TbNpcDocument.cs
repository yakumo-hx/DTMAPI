using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.NPC;

public sealed class TbNpcDocument
{
	private readonly Dictionary<string, NpcDocumentInfo> _dataMap;

	private readonly List<NpcDocumentInfo> _dataList;

	public Dictionary<string, NpcDocumentInfo> DataMap => _dataMap;

	public List<NpcDocumentInfo> DataList => _dataList;

	public NpcDocumentInfo this[string key] => _dataMap[key];

	public TbNpcDocument(JSONNode _json)
	{
		_dataMap = new Dictionary<string, NpcDocumentInfo>();
		_dataList = new List<NpcDocumentInfo>();
		foreach (JSONNode child in _json.Children)
		{
			NpcDocumentInfo npcDocumentInfo = NpcDocumentInfo.DeserializeNpcDocumentInfo(child);
			if (_dataMap.TryAdd(npcDocumentInfo.Id, npcDocumentInfo))
			{
				_dataList.Add(npcDocumentInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + npcDocumentInfo.Id + " in table: TbNpcDocument");
			}
		}
	}

	public NpcDocumentInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public NpcDocumentInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (NpcDocumentInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (NpcDocumentInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
