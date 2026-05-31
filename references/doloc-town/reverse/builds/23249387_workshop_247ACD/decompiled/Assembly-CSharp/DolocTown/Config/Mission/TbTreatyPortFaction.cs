using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Mission;

public sealed class TbTreatyPortFaction
{
	private readonly List<TreatyPortFactionInfo> _dataList;

	private Dictionary<string, TreatyPortFactionInfo> _dataMap_id;

	private Dictionary<FactionType, TreatyPortFactionInfo> _dataMap_faction_type;

	public List<TreatyPortFactionInfo> DataList => _dataList;

	public TbTreatyPortFaction(JSONNode _json)
	{
		_dataList = new List<TreatyPortFactionInfo>();
		foreach (JSONNode child in _json.Children)
		{
			TreatyPortFactionInfo item = TreatyPortFactionInfo.DeserializeTreatyPortFactionInfo(child);
			_dataList.Add(item);
		}
		_dataMap_id = new Dictionary<string, TreatyPortFactionInfo>();
		_dataMap_faction_type = new Dictionary<FactionType, TreatyPortFactionInfo>();
		foreach (TreatyPortFactionInfo data in _dataList)
		{
			if (!_dataMap_id.TryAdd(data.Id, data))
			{
				Debug.LogError("[Config] Duplicate key: " + data.Id + " in table: TbTreatyPortFaction");
			}
			if (!_dataMap_faction_type.TryAdd(data.FactionType, data))
			{
				Debug.LogError($"[Config] Duplicate key: {data.FactionType} in table: TbTreatyPortFaction");
			}
		}
	}

	public TreatyPortFactionInfo GetById(string key)
	{
		if (!_dataMap_id.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public TreatyPortFactionInfo GetByFactionType(FactionType key)
	{
		if (!_dataMap_faction_type.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (TreatyPortFactionInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (TreatyPortFactionInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
