using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Buff;

public sealed class TbSpiritThreshold
{
	private readonly Dictionary<int, SpiritThresholdInfo> _dataMap;

	private readonly List<SpiritThresholdInfo> _dataList;

	public Dictionary<int, SpiritThresholdInfo> DataMap => _dataMap;

	public List<SpiritThresholdInfo> DataList => _dataList;

	public SpiritThresholdInfo this[int key] => _dataMap[key];

	public TbSpiritThreshold(JSONNode _json)
	{
		_dataMap = new Dictionary<int, SpiritThresholdInfo>();
		_dataList = new List<SpiritThresholdInfo>();
		foreach (JSONNode child in _json.Children)
		{
			SpiritThresholdInfo spiritThresholdInfo = SpiritThresholdInfo.DeserializeSpiritThresholdInfo(child);
			if (_dataMap.TryAdd(spiritThresholdInfo.SpiritThreshold, spiritThresholdInfo))
			{
				_dataList.Add(spiritThresholdInfo);
			}
			else
			{
				Debug.LogError($"[Config] Duplicate key: {spiritThresholdInfo.SpiritThreshold} in table: TbSpiritThreshold");
			}
		}
	}

	public SpiritThresholdInfo GetOrDefault(int key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public SpiritThresholdInfo Get(int key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (SpiritThresholdInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (SpiritThresholdInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
