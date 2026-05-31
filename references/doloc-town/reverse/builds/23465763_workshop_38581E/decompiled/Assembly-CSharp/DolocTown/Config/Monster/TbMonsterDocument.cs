using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Monster;

public sealed class TbMonsterDocument
{
	private readonly Dictionary<string, MonsterDocumentInfo> _dataMap;

	private readonly List<MonsterDocumentInfo> _dataList;

	public Dictionary<string, MonsterDocumentInfo> DataMap => _dataMap;

	public List<MonsterDocumentInfo> DataList => _dataList;

	public MonsterDocumentInfo this[string key] => _dataMap[key];

	public TbMonsterDocument(JSONNode _json)
	{
		_dataMap = new Dictionary<string, MonsterDocumentInfo>();
		_dataList = new List<MonsterDocumentInfo>();
		foreach (JSONNode child in _json.Children)
		{
			MonsterDocumentInfo monsterDocumentInfo = MonsterDocumentInfo.DeserializeMonsterDocumentInfo(child);
			if (_dataMap.TryAdd(monsterDocumentInfo.Id, monsterDocumentInfo))
			{
				_dataList.Add(monsterDocumentInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + monsterDocumentInfo.Id + " in table: TbMonsterDocument");
			}
		}
	}

	public MonsterDocumentInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public MonsterDocumentInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (MonsterDocumentInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (MonsterDocumentInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
