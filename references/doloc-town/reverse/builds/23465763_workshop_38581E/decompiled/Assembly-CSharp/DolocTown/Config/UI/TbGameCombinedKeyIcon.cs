using System;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.UI;

public sealed class TbGameCombinedKeyIcon
{
	private readonly Dictionary<string, GameCombinedKeyIconInfo> _dataMap;

	private readonly List<GameCombinedKeyIconInfo> _dataList;

	private Dictionary<string, List<string>> urlCache = new Dictionary<string, List<string>>();

	public Dictionary<string, GameCombinedKeyIconInfo> DataMap => _dataMap;

	public List<GameCombinedKeyIconInfo> DataList => _dataList;

	public GameCombinedKeyIconInfo this[string key] => _dataMap[key];

	public TbGameCombinedKeyIcon(JSONNode _json)
	{
		_dataMap = new Dictionary<string, GameCombinedKeyIconInfo>();
		_dataList = new List<GameCombinedKeyIconInfo>();
		foreach (JSONNode child in _json.Children)
		{
			GameCombinedKeyIconInfo gameCombinedKeyIconInfo = GameCombinedKeyIconInfo.DeserializeGameCombinedKeyIconInfo(child);
			if (_dataMap.TryAdd(gameCombinedKeyIconInfo.Id, gameCombinedKeyIconInfo))
			{
				_dataList.Add(gameCombinedKeyIconInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + gameCombinedKeyIconInfo.Id + " in table: TbGameCombinedKeyIcon");
			}
		}
	}

	public GameCombinedKeyIconInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public GameCombinedKeyIconInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (GameCombinedKeyIconInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
		PostResolve();
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (GameCombinedKeyIconInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}

	private void PostResolve()
	{
		foreach (GameCombinedKeyIconInfo data in DataList)
		{
			string[] subPaths = data.SubPaths;
			foreach (string key in subPaths)
			{
				urlCache.TryAdd(key, new List<string>());
				urlCache[key].Add(data.Id);
			}
		}
	}

	public bool TryMatch(string[] urls, out SpriteAsset asset)
	{
		HashSet<string> hashSet = new HashSet<string>();
		foreach (string key in urls)
		{
			if (!urlCache.TryGetValue(key, out var value))
			{
				continue;
			}
			foreach (string item in value)
			{
				hashSet.Add(item);
			}
		}
		List<string> list = urls.OrderBy((string x) => x).ToList();
		foreach (string item2 in hashSet)
		{
			if (!DataMap.TryGetValue(item2, out var value2) || value2.SubPaths.Length != urls.Length)
			{
				continue;
			}
			string[] array = value2.SubPaths.OrderBy((string x) => x).ToArray();
			bool flag = true;
			for (int j = 0; j < list.Count; j++)
			{
				if (list[j] != array[j])
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				asset = value2.CombinedIcon;
				return true;
			}
		}
		asset = null;
		return false;
	}
}
