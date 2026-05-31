using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Localization;

public sealed class TbL10nText
{
	private readonly Dictionary<string, L10nTextInfo> _dataMap;

	private readonly List<L10nTextInfo> _dataList;

	public Dictionary<string, L10nTextInfo> DataMap => _dataMap;

	public List<L10nTextInfo> DataList => _dataList;

	public L10nTextInfo this[string key] => _dataMap[key];

	public TbL10nText(JSONNode _json)
	{
		_dataMap = new Dictionary<string, L10nTextInfo>();
		_dataList = new List<L10nTextInfo>();
		foreach (JSONNode child in _json.Children)
		{
			L10nTextInfo l10nTextInfo = L10nTextInfo.DeserializeL10nTextInfo(child);
			if (_dataMap.TryAdd(l10nTextInfo.Id, l10nTextInfo))
			{
				_dataList.Add(l10nTextInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + l10nTextInfo.Id + " in table: TbL10nText");
			}
		}
	}

	public L10nTextInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public L10nTextInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (L10nTextInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (L10nTextInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
