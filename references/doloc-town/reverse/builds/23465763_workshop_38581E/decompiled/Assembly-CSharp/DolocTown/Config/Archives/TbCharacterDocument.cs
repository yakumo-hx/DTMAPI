using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Archives;

public sealed class TbCharacterDocument
{
	private readonly Dictionary<string, CharacterDocumentInfo> _dataMap;

	private readonly List<CharacterDocumentInfo> _dataList;

	public Dictionary<string, CharacterDocumentInfo> DataMap => _dataMap;

	public List<CharacterDocumentInfo> DataList => _dataList;

	public CharacterDocumentInfo this[string key] => _dataMap[key];

	public TbCharacterDocument(JSONNode _json)
	{
		_dataMap = new Dictionary<string, CharacterDocumentInfo>();
		_dataList = new List<CharacterDocumentInfo>();
		foreach (JSONNode child in _json.Children)
		{
			CharacterDocumentInfo characterDocumentInfo = CharacterDocumentInfo.DeserializeCharacterDocumentInfo(child);
			if (_dataMap.TryAdd(characterDocumentInfo.Id, characterDocumentInfo))
			{
				_dataList.Add(characterDocumentInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + characterDocumentInfo.Id + " in table: TbCharacterDocument");
			}
		}
	}

	public CharacterDocumentInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public CharacterDocumentInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (CharacterDocumentInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (CharacterDocumentInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
