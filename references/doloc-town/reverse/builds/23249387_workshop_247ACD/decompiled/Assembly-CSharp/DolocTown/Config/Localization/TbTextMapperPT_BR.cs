using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Localization;

public sealed class TbTextMapperPT_BR : ITextProvider
{
	private readonly Dictionary<string, TextMapperPT_BR> _dataMap;

	private readonly List<TextMapperPT_BR> _dataList;

	public Dictionary<string, TextMapperPT_BR> DataMap => _dataMap;

	public List<TextMapperPT_BR> DataList => _dataList;

	public TextMapperPT_BR this[string key] => _dataMap[key];

	public TbTextMapperPT_BR(JSONNode _json)
	{
		_dataMap = new Dictionary<string, TextMapperPT_BR>();
		_dataList = new List<TextMapperPT_BR>();
		foreach (JSONNode child in _json.Children)
		{
			TextMapperPT_BR textMapperPT_BR = TextMapperPT_BR.DeserializeTextMapperPT_BR(child);
			if (_dataMap.TryAdd(textMapperPT_BR.Key, textMapperPT_BR))
			{
				_dataList.Add(textMapperPT_BR);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + textMapperPT_BR.Key + " in table: TbTextMapperPT_BR");
			}
		}
	}

	public TextMapperPT_BR GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public TextMapperPT_BR Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (TextMapperPT_BR data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (TextMapperPT_BR data in _dataList)
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
			TextMapperPT_BR textMapperPT_BR = TextMapperPT_BR.DeserializeTextMapperPT_BR(child);
			if (_dataMap.TryAdd(textMapperPT_BR.Key, textMapperPT_BR))
			{
				_dataList.Add(textMapperPT_BR);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + textMapperPT_BR.Key + " in table: TbTextMapperPT_BR");
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
