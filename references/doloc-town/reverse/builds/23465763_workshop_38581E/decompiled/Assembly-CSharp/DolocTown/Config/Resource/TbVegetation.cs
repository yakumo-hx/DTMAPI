using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Resource;

public sealed class TbVegetation
{
	private readonly Dictionary<string, VegetationInfo> _dataMap;

	private readonly List<VegetationInfo> _dataList;

	public Dictionary<string, VegetationInfo> DataMap => _dataMap;

	public List<VegetationInfo> DataList => _dataList;

	public VegetationInfo this[string key] => _dataMap[key];

	public TbVegetation(JSONNode _json)
	{
		_dataMap = new Dictionary<string, VegetationInfo>();
		_dataList = new List<VegetationInfo>();
		foreach (JSONNode child in _json.Children)
		{
			VegetationInfo vegetationInfo = VegetationInfo.DeserializeVegetationInfo(child);
			if (_dataMap.TryAdd(vegetationInfo.Id, vegetationInfo))
			{
				_dataList.Add(vegetationInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + vegetationInfo.Id + " in table: TbVegetation");
			}
		}
	}

	public VegetationInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public VegetationInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (VegetationInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (VegetationInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
