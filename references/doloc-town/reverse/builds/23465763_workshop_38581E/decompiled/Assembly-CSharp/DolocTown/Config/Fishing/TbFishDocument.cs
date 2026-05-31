using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Fishing;

public sealed class TbFishDocument
{
	private readonly Dictionary<string, FishDocumentInfo> _dataMap;

	private readonly List<FishDocumentInfo> _dataList;

	public Dictionary<string, FishDocumentInfo> DataMap => _dataMap;

	public List<FishDocumentInfo> DataList => _dataList;

	public FishDocumentInfo this[string key] => _dataMap[key];

	public TbFishDocument(JSONNode _json)
	{
		_dataMap = new Dictionary<string, FishDocumentInfo>();
		_dataList = new List<FishDocumentInfo>();
		foreach (JSONNode child in _json.Children)
		{
			FishDocumentInfo fishDocumentInfo = FishDocumentInfo.DeserializeFishDocumentInfo(child);
			if (_dataMap.TryAdd(fishDocumentInfo.Id, fishDocumentInfo))
			{
				_dataList.Add(fishDocumentInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + fishDocumentInfo.Id + " in table: TbFishDocument");
			}
		}
	}

	public FishDocumentInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public FishDocumentInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (FishDocumentInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (FishDocumentInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
