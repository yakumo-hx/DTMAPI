using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Localization;

public sealed class TbTextMapperZH_TW : ITextProvider
{
	private readonly Dictionary<string, TextMapperZH_TW> _dataMap;

	private readonly List<TextMapperZH_TW> _dataList;

	public Dictionary<string, TextMapperZH_TW> DataMap => _dataMap;

	public List<TextMapperZH_TW> DataList => _dataList;

	public TextMapperZH_TW this[string key] => _dataMap[key];

	public TbTextMapperZH_TW(JSONNode _json)
	{
		_dataMap = new Dictionary<string, TextMapperZH_TW>();
		_dataList = new List<TextMapperZH_TW>();
		foreach (JSONNode child in _json.Children)
		{
			TextMapperZH_TW textMapperZH_TW = TextMapperZH_TW.DeserializeTextMapperZH_TW(child);
			if (_dataMap.TryAdd(textMapperZH_TW.Key, textMapperZH_TW))
			{
				_dataList.Add(textMapperZH_TW);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + textMapperZH_TW.Key + " in table: TbTextMapperZH_TW");
			}
		}
	}

	public TextMapperZH_TW GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public TextMapperZH_TW Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (TextMapperZH_TW data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (TextMapperZH_TW data in _dataList)
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
			TextMapperZH_TW textMapperZH_TW = TextMapperZH_TW.DeserializeTextMapperZH_TW(child);
			if (_dataMap.TryAdd(textMapperZH_TW.Key, textMapperZH_TW))
			{
				_dataList.Add(textMapperZH_TW);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + textMapperZH_TW.Key + " in table: TbTextMapperZH_TW");
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
