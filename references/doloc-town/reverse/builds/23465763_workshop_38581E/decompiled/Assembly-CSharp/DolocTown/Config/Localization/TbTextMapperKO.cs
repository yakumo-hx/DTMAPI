using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Localization;

public sealed class TbTextMapperKO : ITextProvider
{
	private readonly Dictionary<string, TextMapperKO> _dataMap;

	private readonly List<TextMapperKO> _dataList;

	public Dictionary<string, TextMapperKO> DataMap => _dataMap;

	public List<TextMapperKO> DataList => _dataList;

	public TextMapperKO this[string key] => _dataMap[key];

	public TbTextMapperKO(JSONNode _json)
	{
		_dataMap = new Dictionary<string, TextMapperKO>();
		_dataList = new List<TextMapperKO>();
		foreach (JSONNode child in _json.Children)
		{
			TextMapperKO textMapperKO = TextMapperKO.DeserializeTextMapperKO(child);
			if (_dataMap.TryAdd(textMapperKO.Key, textMapperKO))
			{
				_dataList.Add(textMapperKO);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + textMapperKO.Key + " in table: TbTextMapperKO");
			}
		}
	}

	public TextMapperKO GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public TextMapperKO Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (TextMapperKO data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (TextMapperKO data in _dataList)
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
			TextMapperKO textMapperKO = TextMapperKO.DeserializeTextMapperKO(child);
			if (_dataMap.TryAdd(textMapperKO.Key, textMapperKO))
			{
				_dataList.Add(textMapperKO);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + textMapperKO.Key + " in table: TbTextMapperKO");
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
