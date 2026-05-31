using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Localization;

public sealed class TbTextMapperRU : ITextProvider
{
	private readonly Dictionary<string, TextMapperRU> _dataMap;

	private readonly List<TextMapperRU> _dataList;

	public Dictionary<string, TextMapperRU> DataMap => _dataMap;

	public List<TextMapperRU> DataList => _dataList;

	public TextMapperRU this[string key] => _dataMap[key];

	public TbTextMapperRU(JSONNode _json)
	{
		_dataMap = new Dictionary<string, TextMapperRU>();
		_dataList = new List<TextMapperRU>();
		foreach (JSONNode child in _json.Children)
		{
			TextMapperRU textMapperRU = TextMapperRU.DeserializeTextMapperRU(child);
			if (_dataMap.TryAdd(textMapperRU.Key, textMapperRU))
			{
				_dataList.Add(textMapperRU);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + textMapperRU.Key + " in table: TbTextMapperRU");
			}
		}
	}

	public TextMapperRU GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public TextMapperRU Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (TextMapperRU data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (TextMapperRU data in _dataList)
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
			TextMapperRU textMapperRU = TextMapperRU.DeserializeTextMapperRU(child);
			if (_dataMap.TryAdd(textMapperRU.Key, textMapperRU))
			{
				_dataList.Add(textMapperRU);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + textMapperRU.Key + " in table: TbTextMapperRU");
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
