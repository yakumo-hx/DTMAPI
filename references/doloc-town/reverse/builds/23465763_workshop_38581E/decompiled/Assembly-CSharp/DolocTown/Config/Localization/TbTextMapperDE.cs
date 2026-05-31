using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Localization;

public sealed class TbTextMapperDE : ITextProvider
{
	private readonly Dictionary<string, TextMapperDE> _dataMap;

	private readonly List<TextMapperDE> _dataList;

	public Dictionary<string, TextMapperDE> DataMap => _dataMap;

	public List<TextMapperDE> DataList => _dataList;

	public TextMapperDE this[string key] => _dataMap[key];

	public TbTextMapperDE(JSONNode _json)
	{
		_dataMap = new Dictionary<string, TextMapperDE>();
		_dataList = new List<TextMapperDE>();
		foreach (JSONNode child in _json.Children)
		{
			TextMapperDE textMapperDE = TextMapperDE.DeserializeTextMapperDE(child);
			if (_dataMap.TryAdd(textMapperDE.Key, textMapperDE))
			{
				_dataList.Add(textMapperDE);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + textMapperDE.Key + " in table: TbTextMapperDE");
			}
		}
	}

	public TextMapperDE GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public TextMapperDE Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (TextMapperDE data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (TextMapperDE data in _dataList)
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
			TextMapperDE textMapperDE = TextMapperDE.DeserializeTextMapperDE(child);
			if (_dataMap.TryAdd(textMapperDE.Key, textMapperDE))
			{
				_dataList.Add(textMapperDE);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + textMapperDE.Key + " in table: TbTextMapperDE");
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
