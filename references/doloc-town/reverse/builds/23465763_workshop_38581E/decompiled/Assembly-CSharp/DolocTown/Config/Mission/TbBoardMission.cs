using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Mission;

public sealed class TbBoardMission
{
	private readonly Dictionary<string, BoardMissionInfo> _dataMap;

	private readonly List<BoardMissionInfo> _dataList;

	public Dictionary<string, BoardMissionInfo> DataMap => _dataMap;

	public List<BoardMissionInfo> DataList => _dataList;

	public BoardMissionInfo this[string key] => _dataMap[key];

	public TbBoardMission(JSONNode _json)
	{
		_dataMap = new Dictionary<string, BoardMissionInfo>();
		_dataList = new List<BoardMissionInfo>();
		foreach (JSONNode child in _json.Children)
		{
			BoardMissionInfo boardMissionInfo = BoardMissionInfo.DeserializeBoardMissionInfo(child);
			if (_dataMap.TryAdd(boardMissionInfo.Id, boardMissionInfo))
			{
				_dataList.Add(boardMissionInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + boardMissionInfo.Id + " in table: TbBoardMission");
			}
		}
	}

	public BoardMissionInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public BoardMissionInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (BoardMissionInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (BoardMissionInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
