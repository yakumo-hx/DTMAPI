using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Player;

public sealed class TbPlayerAnimationFrame
{
	private readonly Dictionary<string, PlayerAnimationFrameInfo> _dataMap;

	private readonly List<PlayerAnimationFrameInfo> _dataList;

	public Dictionary<string, PlayerAnimationFrameInfo> DataMap => _dataMap;

	public List<PlayerAnimationFrameInfo> DataList => _dataList;

	public PlayerAnimationFrameInfo this[string key] => _dataMap[key];

	public TbPlayerAnimationFrame(JSONNode _json)
	{
		_dataMap = new Dictionary<string, PlayerAnimationFrameInfo>();
		_dataList = new List<PlayerAnimationFrameInfo>();
		foreach (JSONNode child in _json.Children)
		{
			PlayerAnimationFrameInfo playerAnimationFrameInfo = PlayerAnimationFrameInfo.DeserializePlayerAnimationFrameInfo(child);
			if (_dataMap.TryAdd(playerAnimationFrameInfo.Id, playerAnimationFrameInfo))
			{
				_dataList.Add(playerAnimationFrameInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + playerAnimationFrameInfo.Id + " in table: TbPlayerAnimationFrame");
			}
		}
	}

	public PlayerAnimationFrameInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public PlayerAnimationFrameInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (PlayerAnimationFrameInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (PlayerAnimationFrameInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
