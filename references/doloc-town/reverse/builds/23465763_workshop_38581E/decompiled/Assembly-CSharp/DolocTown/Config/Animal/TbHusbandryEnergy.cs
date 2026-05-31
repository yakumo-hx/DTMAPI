using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Animal;

public sealed class TbHusbandryEnergy
{
	private readonly Dictionary<string, HusbandryEnergyInfo> _dataMap;

	private readonly List<HusbandryEnergyInfo> _dataList;

	public Dictionary<string, HusbandryEnergyInfo> DataMap => _dataMap;

	public List<HusbandryEnergyInfo> DataList => _dataList;

	public HusbandryEnergyInfo this[string key] => _dataMap[key];

	public TbHusbandryEnergy(JSONNode _json)
	{
		_dataMap = new Dictionary<string, HusbandryEnergyInfo>();
		_dataList = new List<HusbandryEnergyInfo>();
		foreach (JSONNode child in _json.Children)
		{
			HusbandryEnergyInfo husbandryEnergyInfo = HusbandryEnergyInfo.DeserializeHusbandryEnergyInfo(child);
			if (_dataMap.TryAdd(husbandryEnergyInfo.Id, husbandryEnergyInfo))
			{
				_dataList.Add(husbandryEnergyInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + husbandryEnergyInfo.Id + " in table: TbHusbandryEnergy");
			}
		}
	}

	public HusbandryEnergyInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public HusbandryEnergyInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (HusbandryEnergyInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (HusbandryEnergyInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}

	public bool IsHusbandryFeeds(string name, out int energy)
	{
		energy = 0;
		if (name.IsNullOrEmpty())
		{
			return false;
		}
		if (!DataMap.TryGetValue(name, out var value))
		{
			return false;
		}
		energy = value.Energy;
		return energy > 0;
	}
}
