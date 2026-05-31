using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Localization;

public sealed class TbTextMapperJA : ITextProvider
{
	private readonly Dictionary<string, TextMapperJA> _dataMap;

	private readonly List<TextMapperJA> _dataList;

	public Dictionary<string, TextMapperJA> DataMap => _dataMap;

	public List<TextMapperJA> DataList => _dataList;

	public TextMapperJA this[string key] => _dataMap[key];

	public TbTextMapperJA(JSONNode _json)
	{
		_dataMap = new Dictionary<string, TextMapperJA>();
		_dataList = new List<TextMapperJA>();
		foreach (JSONNode child in _json.Children)
		{
			TextMapperJA textMapperJA = TextMapperJA.DeserializeTextMapperJA(child);
			if (_dataMap.TryAdd(textMapperJA.Key, textMapperJA))
			{
				_dataList.Add(textMapperJA);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + textMapperJA.Key + " in table: TbTextMapperJA");
			}
		}
	}

	public TextMapperJA GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public TextMapperJA Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (TextMapperJA data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (TextMapperJA data in _dataList)
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
			TextMapperJA textMapperJA = TextMapperJA.DeserializeTextMapperJA(child);
			if (_dataMap.TryAdd(textMapperJA.Key, textMapperJA))
			{
				_dataList.Add(textMapperJA);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + textMapperJA.Key + " in table: TbTextMapperJA");
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
