using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.EnvOptimizer;

public sealed class TbEnvOptimizerSlot
{
	private readonly List<EnvOptimizerSlotInfo> _dataList;

	private Dictionary<string, EnvOptimizerSlotInfo> _dataMap_id;

	private Dictionary<EnvOptimizerComponentType, EnvOptimizerSlotInfo> _dataMap_component_type;

	public List<EnvOptimizerSlotInfo> DataList => _dataList;

	public TbEnvOptimizerSlot(JSONNode _json)
	{
		_dataList = new List<EnvOptimizerSlotInfo>();
		foreach (JSONNode child in _json.Children)
		{
			EnvOptimizerSlotInfo item = EnvOptimizerSlotInfo.DeserializeEnvOptimizerSlotInfo(child);
			_dataList.Add(item);
		}
		_dataMap_id = new Dictionary<string, EnvOptimizerSlotInfo>();
		_dataMap_component_type = new Dictionary<EnvOptimizerComponentType, EnvOptimizerSlotInfo>();
		foreach (EnvOptimizerSlotInfo data in _dataList)
		{
			if (!_dataMap_id.TryAdd(data.Id, data))
			{
				Debug.LogError("[Config] Duplicate key: " + data.Id + " in table: TbEnvOptimizerSlot");
			}
			if (!_dataMap_component_type.TryAdd(data.ComponentType, data))
			{
				Debug.LogError($"[Config] Duplicate key: {data.ComponentType} in table: TbEnvOptimizerSlot");
			}
		}
	}

	public EnvOptimizerSlotInfo GetById(string key)
	{
		if (!_dataMap_id.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public EnvOptimizerSlotInfo GetByComponentType(EnvOptimizerComponentType key)
	{
		if (!_dataMap_component_type.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (EnvOptimizerSlotInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (EnvOptimizerSlotInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
