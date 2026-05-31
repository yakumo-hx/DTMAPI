using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Room;

public sealed class TbScene
{
	private readonly Dictionary<string, SceneInfo> _dataMap;

	private readonly List<SceneInfo> _dataList;

	public Dictionary<string, SceneInfo> DataMap => _dataMap;

	public List<SceneInfo> DataList => _dataList;

	public SceneInfo this[string key] => _dataMap[key];

	public TbScene(JSONNode _json)
	{
		_dataMap = new Dictionary<string, SceneInfo>();
		_dataList = new List<SceneInfo>();
		foreach (JSONNode child in _json.Children)
		{
			SceneInfo sceneInfo = SceneInfo.DeserializeSceneInfo(child);
			if (_dataMap.TryAdd(sceneInfo.Id, sceneInfo))
			{
				_dataList.Add(sceneInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + sceneInfo.Id + " in table: TbScene");
			}
		}
	}

	public SceneInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public SceneInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (SceneInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (SceneInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
