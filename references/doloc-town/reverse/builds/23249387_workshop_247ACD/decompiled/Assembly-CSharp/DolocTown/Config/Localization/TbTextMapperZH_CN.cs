using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Localization;

public sealed class TbTextMapperZH_CN : ITextProvider
{
	private readonly Dictionary<string, TextMapperZH_CN> _dataMap;

	private readonly List<TextMapperZH_CN> _dataList;

	public Dictionary<string, TextMapperZH_CN> DataMap => _dataMap;

	public List<TextMapperZH_CN> DataList => _dataList;

	public TextMapperZH_CN this[string key] => _dataMap[key];

	public TbTextMapperZH_CN(JSONNode _json)
	{
		_dataMap = new Dictionary<string, TextMapperZH_CN>();
		_dataList = new List<TextMapperZH_CN>();
		foreach (JSONNode child in _json.Children)
		{
			TextMapperZH_CN textMapperZH_CN = TextMapperZH_CN.DeserializeTextMapperZH_CN(child);
			if (_dataMap.TryAdd(textMapperZH_CN.Key, textMapperZH_CN))
			{
				_dataList.Add(textMapperZH_CN);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + textMapperZH_CN.Key + " in table: TbTextMapperZH_CN");
			}
		}
	}

	public TextMapperZH_CN GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public TextMapperZH_CN Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (TextMapperZH_CN data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (TextMapperZH_CN data in _dataList)
		{
			data.TranslateText(translator);
		}
	}

	public void Load(JSONNode _json)
	{
		if (_dataList.Count > 0)
		{
			return;
		}
		foreach (JSONNode child in _json.Children)
		{
			TextMapperZH_CN textMapperZH_CN = TextMapperZH_CN.DeserializeTextMapperZH_CN(child);
			if (_dataMap.TryAdd(textMapperZH_CN.Key, textMapperZH_CN))
			{
				_dataList.Add(textMapperZH_CN);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + textMapperZH_CN.Key + " in table: TbTextMapperZH_CN");
			}
		}
	}

	public void Unload()
	{
		_dataMap.Clear();
		_dataList.Clear();
	}

	public string GetText(string key)
	{
		return GetOrDefault(key)?.Text;
	}
}
