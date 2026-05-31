using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.UI;

public sealed class TbEquipmentMenu
{
	private readonly Dictionary<EquipmentMenuSubType, EquipmentMenuInfo> _dataMap;

	private readonly List<EquipmentMenuInfo> _dataList;

	public Dictionary<EquipmentMenuSubType, EquipmentMenuInfo> DataMap => _dataMap;

	public List<EquipmentMenuInfo> DataList => _dataList;

	public EquipmentMenuInfo this[EquipmentMenuSubType key] => _dataMap[key];

	public TbEquipmentMenu(JSONNode _json)
	{
		_dataMap = new Dictionary<EquipmentMenuSubType, EquipmentMenuInfo>();
		_dataList = new List<EquipmentMenuInfo>();
		foreach (JSONNode child in _json.Children)
		{
			EquipmentMenuInfo equipmentMenuInfo = EquipmentMenuInfo.DeserializeEquipmentMenuInfo(child);
			if (_dataMap.TryAdd(equipmentMenuInfo.Id, equipmentMenuInfo))
			{
				_dataList.Add(equipmentMenuInfo);
			}
			else
			{
				Debug.LogError($"[Config] Duplicate key: {equipmentMenuInfo.Id} in table: TbEquipmentMenu");
			}
		}
	}

	public EquipmentMenuInfo GetOrDefault(EquipmentMenuSubType key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public EquipmentMenuInfo Get(EquipmentMenuSubType key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (EquipmentMenuInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (EquipmentMenuInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
