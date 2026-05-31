using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Animal;

public sealed class TbAnimal
{
	private readonly Dictionary<string, AnimalInfo> _dataMap;

	private readonly List<AnimalInfo> _dataList;

	public Dictionary<string, AnimalInfo> DataMap => _dataMap;

	public List<AnimalInfo> DataList => _dataList;

	public AnimalInfo this[string key] => _dataMap[key];

	public TbAnimal(JSONNode _json)
	{
		_dataMap = new Dictionary<string, AnimalInfo>();
		_dataList = new List<AnimalInfo>();
		foreach (JSONNode child in _json.Children)
		{
			AnimalInfo animalInfo = AnimalInfo.DeserializeAnimalInfo(child);
			if (_dataMap.TryAdd(animalInfo.Id, animalInfo))
			{
				_dataList.Add(animalInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + animalInfo.Id + " in table: TbAnimal");
			}
		}
	}

	public AnimalInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public AnimalInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (AnimalInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (AnimalInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
