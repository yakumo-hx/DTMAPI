using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Mission;

public sealed class TbBoardMissionLevel
{
	private readonly Dictionary<string, BoardMissionLevelInfo> _dataMap;

	private readonly List<BoardMissionLevelInfo> _dataList;

	public Dictionary<string, BoardMissionLevelInfo> DataMap => _dataMap;

	public List<BoardMissionLevelInfo> DataList => _dataList;

	public BoardMissionLevelInfo this[string key] => _dataMap[key];

	public TbBoardMissionLevel(JSONNode _json)
	{
		_dataMap = new Dictionary<string, BoardMissionLevelInfo>();
		_dataList = new List<BoardMissionLevelInfo>();
		foreach (JSONNode child in _json.Children)
		{
			BoardMissionLevelInfo boardMissionLevelInfo = BoardMissionLevelInfo.DeserializeBoardMissionLevelInfo(child);
			if (_dataMap.TryAdd(boardMissionLevelInfo.MissionLv, boardMissionLevelInfo))
			{
				_dataList.Add(boardMissionLevelInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + boardMissionLevelInfo.MissionLv + " in table: TbBoardMissionLevel");
			}
		}
	}

	public BoardMissionLevelInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public BoardMissionLevelInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (BoardMissionLevelInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (BoardMissionLevelInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
