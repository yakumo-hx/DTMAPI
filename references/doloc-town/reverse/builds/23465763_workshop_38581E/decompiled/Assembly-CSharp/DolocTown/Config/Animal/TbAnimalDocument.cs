using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Animal;

public sealed class TbAnimalDocument
{
	private readonly Dictionary<string, AnimalDocumentInfo> _dataMap;

	private readonly List<AnimalDocumentInfo> _dataList;

	public Dictionary<string, AnimalDocumentInfo> DataMap => _dataMap;

	public List<AnimalDocumentInfo> DataList => _dataList;

	public AnimalDocumentInfo this[string key] => _dataMap[key];

	public TbAnimalDocument(JSONNode _json)
	{
		_dataMap = new Dictionary<string, AnimalDocumentInfo>();
		_dataList = new List<AnimalDocumentInfo>();
		foreach (JSONNode child in _json.Children)
		{
			AnimalDocumentInfo animalDocumentInfo = AnimalDocumentInfo.DeserializeAnimalDocumentInfo(child);
			if (_dataMap.TryAdd(animalDocumentInfo.Id, animalDocumentInfo))
			{
				_dataList.Add(animalDocumentInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + animalDocumentInfo.Id + " in table: TbAnimalDocument");
			}
		}
	}

	public AnimalDocumentInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public AnimalDocumentInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (AnimalDocumentInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (AnimalDocumentInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
