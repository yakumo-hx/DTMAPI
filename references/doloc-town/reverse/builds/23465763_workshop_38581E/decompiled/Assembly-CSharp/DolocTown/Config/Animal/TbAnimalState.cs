using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Animal;

public sealed class TbAnimalState
{
	private readonly Dictionary<AnimalState, AnimalStateInfo> _dataMap;

	private readonly List<AnimalStateInfo> _dataList;

	public Dictionary<AnimalState, AnimalStateInfo> DataMap => _dataMap;

	public List<AnimalStateInfo> DataList => _dataList;

	public AnimalStateInfo this[AnimalState key] => _dataMap[key];

	public TbAnimalState(JSONNode _json)
	{
		_dataMap = new Dictionary<AnimalState, AnimalStateInfo>();
		_dataList = new List<AnimalStateInfo>();
		foreach (JSONNode child in _json.Children)
		{
			AnimalStateInfo animalStateInfo = AnimalStateInfo.DeserializeAnimalStateInfo(child);
			if (_dataMap.TryAdd(animalStateInfo.Id, animalStateInfo))
			{
				_dataList.Add(animalStateInfo);
			}
			else
			{
				Debug.LogError($"[Config] Duplicate key: {animalStateInfo.Id} in table: TbAnimalState");
			}
		}
	}

	public AnimalStateInfo GetOrDefault(AnimalState key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public AnimalStateInfo Get(AnimalState key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (AnimalStateInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (AnimalStateInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
