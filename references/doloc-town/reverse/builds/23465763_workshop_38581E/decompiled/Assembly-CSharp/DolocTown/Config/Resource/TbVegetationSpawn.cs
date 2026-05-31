using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Resource;

public sealed class TbVegetationSpawn
{
	private readonly Dictionary<string, VegetationSpawnInfo> _dataMap;

	private readonly List<VegetationSpawnInfo> _dataList;

	public Dictionary<string, VegetationSpawnInfo> DataMap => _dataMap;

	public List<VegetationSpawnInfo> DataList => _dataList;

	public VegetationSpawnInfo this[string key] => _dataMap[key];

	public TbVegetationSpawn(JSONNode _json)
	{
		_dataMap = new Dictionary<string, VegetationSpawnInfo>();
		_dataList = new List<VegetationSpawnInfo>();
		foreach (JSONNode child in _json.Children)
		{
			VegetationSpawnInfo vegetationSpawnInfo = VegetationSpawnInfo.DeserializeVegetationSpawnInfo(child);
			if (_dataMap.TryAdd(vegetationSpawnInfo.Id, vegetationSpawnInfo))
			{
				_dataList.Add(vegetationSpawnInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + vegetationSpawnInfo.Id + " in table: TbVegetationSpawn");
			}
		}
	}

	public VegetationSpawnInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public VegetationSpawnInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (VegetationSpawnInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (VegetationSpawnInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
