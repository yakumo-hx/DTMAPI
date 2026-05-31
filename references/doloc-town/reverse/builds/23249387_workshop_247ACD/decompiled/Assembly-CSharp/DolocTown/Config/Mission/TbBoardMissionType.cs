using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Mission;

public sealed class TbBoardMissionType
{
	private readonly Dictionary<string, BoardMissionTypeInfo> _dataMap;

	private readonly List<BoardMissionTypeInfo> _dataList;

	public Dictionary<string, BoardMissionTypeInfo> DataMap => _dataMap;

	public List<BoardMissionTypeInfo> DataList => _dataList;

	public BoardMissionTypeInfo this[string key] => _dataMap[key];

	public TbBoardMissionType(JSONNode _json)
	{
		_dataMap = new Dictionary<string, BoardMissionTypeInfo>();
		_dataList = new List<BoardMissionTypeInfo>();
		foreach (JSONNode child in _json.Children)
		{
			BoardMissionTypeInfo boardMissionTypeInfo = BoardMissionTypeInfo.DeserializeBoardMissionTypeInfo(child);
			if (_dataMap.TryAdd(boardMissionTypeInfo.Id, boardMissionTypeInfo))
			{
				_dataList.Add(boardMissionTypeInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + boardMissionTypeInfo.Id + " in table: TbBoardMissionType");
			}
		}
	}

	public BoardMissionTypeInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public BoardMissionTypeInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (BoardMissionTypeInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (BoardMissionTypeInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
