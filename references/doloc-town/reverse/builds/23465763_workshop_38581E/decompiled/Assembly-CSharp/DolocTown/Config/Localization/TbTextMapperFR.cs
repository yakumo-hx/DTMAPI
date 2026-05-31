using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Localization;

public sealed class TbTextMapperFR : ITextProvider
{
	private readonly Dictionary<string, TextMapperFR> _dataMap;

	private readonly List<TextMapperFR> _dataList;

	public Dictionary<string, TextMapperFR> DataMap => _dataMap;

	public List<TextMapperFR> DataList => _dataList;

	public TextMapperFR this[string key] => _dataMap[key];

	public TbTextMapperFR(JSONNode _json)
	{
		_dataMap = new Dictionary<string, TextMapperFR>();
		_dataList = new List<TextMapperFR>();
		foreach (JSONNode child in _json.Children)
		{
			TextMapperFR textMapperFR = TextMapperFR.DeserializeTextMapperFR(child);
			if (_dataMap.TryAdd(textMapperFR.Key, textMapperFR))
			{
				_dataList.Add(textMapperFR);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + textMapperFR.Key + " in table: TbTextMapperFR");
			}
		}
	}

	public TextMapperFR GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public TextMapperFR Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (TextMapperFR data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (TextMapperFR data in _dataList)
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
			TextMapperFR textMapperFR = TextMapperFR.DeserializeTextMapperFR(child);
			if (_dataMap.TryAdd(textMapperFR.Key, textMapperFR))
			{
				_dataList.Add(textMapperFR);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + textMapperFR.Key + " in table: TbTextMapperFR");
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
