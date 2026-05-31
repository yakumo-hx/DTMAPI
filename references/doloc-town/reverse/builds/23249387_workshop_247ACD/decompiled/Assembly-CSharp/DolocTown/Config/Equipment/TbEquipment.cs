using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Equipment;

public sealed class TbEquipment
{
	private readonly Dictionary<string, EquipmentInfo> _dataMap;

	private readonly List<EquipmentInfo> _dataList;

	public Dictionary<string, EquipmentInfo> DataMap => _dataMap;

	public List<EquipmentInfo> DataList => _dataList;

	public EquipmentInfo this[string key] => _dataMap[key];

	public TbEquipment(JSONNode _json)
	{
		_dataMap = new Dictionary<string, EquipmentInfo>();
		_dataList = new List<EquipmentInfo>();
		foreach (JSONNode child in _json.Children)
		{
			EquipmentInfo equipmentInfo = EquipmentInfo.DeserializeEquipmentInfo(child);
			if (_dataMap.TryAdd(equipmentInfo.Id, equipmentInfo))
			{
				_dataList.Add(equipmentInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + equipmentInfo.Id + " in table: TbEquipment");
			}
		}
	}

	public EquipmentInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public EquipmentInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (EquipmentInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (EquipmentInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
