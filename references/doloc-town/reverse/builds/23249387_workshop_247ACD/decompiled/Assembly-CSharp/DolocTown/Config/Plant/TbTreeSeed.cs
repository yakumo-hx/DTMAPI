using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Plant;

public sealed class TbTreeSeed
{
	private readonly Dictionary<string, TreeSeedInfo> _dataMap;

	private readonly List<TreeSeedInfo> _dataList;

	public Dictionary<string, TreeSeedInfo> DataMap => _dataMap;

	public List<TreeSeedInfo> DataList => _dataList;

	public TreeSeedInfo this[string key] => _dataMap[key];

	public TbTreeSeed(JSONNode _json)
	{
		_dataMap = new Dictionary<string, TreeSeedInfo>();
		_dataList = new List<TreeSeedInfo>();
		foreach (JSONNode child in _json.Children)
		{
			TreeSeedInfo treeSeedInfo = TreeSeedInfo.DeserializeTreeSeedInfo(child);
			if (_dataMap.TryAdd(treeSeedInfo.Id, treeSeedInfo))
			{
				_dataList.Add(treeSeedInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + treeSeedInfo.Id + " in table: TbTreeSeed");
			}
		}
	}

	public TreeSeedInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public TreeSeedInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (TreeSeedInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (TreeSeedInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
