using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Dialogue;

public sealed class TbDialogueTextStyle
{
	private readonly Dictionary<string, DialogueTextStyleInfo> _dataMap;

	private readonly List<DialogueTextStyleInfo> _dataList;

	public Dictionary<string, DialogueTextStyleInfo> DataMap => _dataMap;

	public List<DialogueTextStyleInfo> DataList => _dataList;

	public DialogueTextStyleInfo this[string key] => _dataMap[key];

	public TbDialogueTextStyle(JSONNode _json)
	{
		_dataMap = new Dictionary<string, DialogueTextStyleInfo>();
		_dataList = new List<DialogueTextStyleInfo>();
		foreach (JSONNode child in _json.Children)
		{
			DialogueTextStyleInfo dialogueTextStyleInfo = DialogueTextStyleInfo.DeserializeDialogueTextStyleInfo(child);
			if (_dataMap.TryAdd(dialogueTextStyleInfo.Id, dialogueTextStyleInfo))
			{
				_dataList.Add(dialogueTextStyleInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + dialogueTextStyleInfo.Id + " in table: TbDialogueTextStyle");
			}
		}
	}

	public DialogueTextStyleInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public DialogueTextStyleInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (DialogueTextStyleInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (DialogueTextStyleInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
