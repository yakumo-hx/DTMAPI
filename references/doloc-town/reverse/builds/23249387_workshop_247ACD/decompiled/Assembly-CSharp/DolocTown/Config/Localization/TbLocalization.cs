using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Localization;

public sealed class TbLocalization
{
	private readonly Dictionary<string, LocalizationInfo> _dataMap;

	private readonly List<LocalizationInfo> _dataList;

	public Dictionary<string, LocalizationInfo> DataMap => _dataMap;

	public List<LocalizationInfo> DataList => _dataList;

	public LocalizationInfo this[string key] => _dataMap[key];

	public TbLocalization(JSONNode _json)
	{
		_dataMap = new Dictionary<string, LocalizationInfo>();
		_dataList = new List<LocalizationInfo>();
		foreach (JSONNode child in _json.Children)
		{
			LocalizationInfo localizationInfo = LocalizationInfo.DeserializeLocalizationInfo(child);
			if (_dataMap.TryAdd(localizationInfo.Id, localizationInfo))
			{
				_dataList.Add(localizationInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + localizationInfo.Id + " in table: TbLocalization");
			}
		}
	}

	public LocalizationInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public LocalizationInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (LocalizationInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (LocalizationInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
