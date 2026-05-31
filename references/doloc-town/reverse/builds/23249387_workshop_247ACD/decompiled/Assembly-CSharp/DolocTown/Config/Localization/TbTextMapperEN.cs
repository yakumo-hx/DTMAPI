using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Localization;

public sealed class TbTextMapperEN : ITextProvider
{
	private readonly Dictionary<string, TextMapperEN> _dataMap;

	private readonly List<TextMapperEN> _dataList;

	public Dictionary<string, TextMapperEN> DataMap => _dataMap;

	public List<TextMapperEN> DataList => _dataList;

	public TextMapperEN this[string key] => _dataMap[key];

	public TbTextMapperEN(JSONNode _json)
	{
		_dataMap = new Dictionary<string, TextMapperEN>();
		_dataList = new List<TextMapperEN>();
		foreach (JSONNode child in _json.Children)
		{
			TextMapperEN textMapperEN = TextMapperEN.DeserializeTextMapperEN(child);
			if (_dataMap.TryAdd(textMapperEN.Key, textMapperEN))
			{
				_dataList.Add(textMapperEN);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + textMapperEN.Key + " in table: TbTextMapperEN");
			}
		}
	}

	public TextMapperEN GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public TextMapperEN Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (TextMapperEN data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (TextMapperEN data in _dataList)
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
			TextMapperEN textMapperEN = TextMapperEN.DeserializeTextMapperEN(child);
			if (_dataMap.TryAdd(textMapperEN.Key, textMapperEN))
			{
				_dataList.Add(textMapperEN);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + textMapperEN.Key + " in table: TbTextMapperEN");
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
