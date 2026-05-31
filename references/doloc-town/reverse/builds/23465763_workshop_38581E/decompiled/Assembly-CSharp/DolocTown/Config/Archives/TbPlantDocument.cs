using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Archives;

public sealed class TbPlantDocument
{
	private readonly Dictionary<string, PlantDocumentInfo> _dataMap;

	private readonly List<PlantDocumentInfo> _dataList;

	public Dictionary<string, PlantDocumentInfo> DataMap => _dataMap;

	public List<PlantDocumentInfo> DataList => _dataList;

	public PlantDocumentInfo this[string key] => _dataMap[key];

	public TbPlantDocument(JSONNode _json)
	{
		_dataMap = new Dictionary<string, PlantDocumentInfo>();
		_dataList = new List<PlantDocumentInfo>();
		foreach (JSONNode child in _json.Children)
		{
			PlantDocumentInfo plantDocumentInfo = PlantDocumentInfo.DeserializePlantDocumentInfo(child);
			if (_dataMap.TryAdd(plantDocumentInfo.Id, plantDocumentInfo))
			{
				_dataList.Add(plantDocumentInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + plantDocumentInfo.Id + " in table: TbPlantDocument");
			}
		}
	}

	public PlantDocumentInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public PlantDocumentInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (PlantDocumentInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (PlantDocumentInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
