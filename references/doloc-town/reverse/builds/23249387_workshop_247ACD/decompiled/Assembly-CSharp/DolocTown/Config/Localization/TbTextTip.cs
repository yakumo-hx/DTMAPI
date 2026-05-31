using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Localization;

public sealed class TbTextTip
{
	private readonly Dictionary<string, TextTipInfo> _dataMap;

	private readonly List<TextTipInfo> _dataList;

	public Dictionary<string, TextTipInfo> DataMap => _dataMap;

	public List<TextTipInfo> DataList => _dataList;

	public TextTipInfo this[string key] => _dataMap[key];

	public TbTextTip(JSONNode _json)
	{
		_dataMap = new Dictionary<string, TextTipInfo>();
		_dataList = new List<TextTipInfo>();
		foreach (JSONNode child in _json.Children)
		{
			TextTipInfo textTipInfo = TextTipInfo.DeserializeTextTipInfo(child);
			if (_dataMap.TryAdd(textTipInfo.Id, textTipInfo))
			{
				_dataList.Add(textTipInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + textTipInfo.Id + " in table: TbTextTip");
			}
		}
	}

	public TextTipInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public TextTipInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (TextTipInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (TextTipInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
