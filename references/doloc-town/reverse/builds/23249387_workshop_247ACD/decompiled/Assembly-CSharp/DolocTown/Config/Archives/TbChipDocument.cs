using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Archives;

public sealed class TbChipDocument
{
	private readonly Dictionary<string, ChipDocumentInfo> _dataMap;

	private readonly List<ChipDocumentInfo> _dataList;

	public Dictionary<string, ChipDocumentInfo> DataMap => _dataMap;

	public List<ChipDocumentInfo> DataList => _dataList;

	public ChipDocumentInfo this[string key] => _dataMap[key];

	public TbChipDocument(JSONNode _json)
	{
		_dataMap = new Dictionary<string, ChipDocumentInfo>();
		_dataList = new List<ChipDocumentInfo>();
		foreach (JSONNode child in _json.Children)
		{
			ChipDocumentInfo chipDocumentInfo = ChipDocumentInfo.DeserializeChipDocumentInfo(child);
			if (_dataMap.TryAdd(chipDocumentInfo.Id, chipDocumentInfo))
			{
				_dataList.Add(chipDocumentInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + chipDocumentInfo.Id + " in table: TbChipDocument");
			}
		}
	}

	public ChipDocumentInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public ChipDocumentInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (ChipDocumentInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (ChipDocumentInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
