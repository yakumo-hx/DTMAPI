using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Resource;

public sealed class TbGlobalGuaranteed
{
	private readonly List<GlobalGuaranteedInfo> _dataList;

	private Dictionary<(GuaranteedType, string), GlobalGuaranteedInfo> _dataMapUnion;

	public List<GlobalGuaranteedInfo> DataList => _dataList;

	public TbGlobalGuaranteed(JSONNode _json)
	{
		_dataList = new List<GlobalGuaranteedInfo>();
		foreach (JSONNode child in _json.Children)
		{
			GlobalGuaranteedInfo item = GlobalGuaranteedInfo.DeserializeGlobalGuaranteedInfo(child);
			_dataList.Add(item);
		}
		_dataMapUnion = new Dictionary<(GuaranteedType, string), GlobalGuaranteedInfo>();
		List<GlobalGuaranteedInfo> list = new List<GlobalGuaranteedInfo>();
		foreach (GlobalGuaranteedInfo data in _dataList)
		{
			if (!_dataMapUnion.TryAdd((data.Type, data.SpawnId), data))
			{
				list.Add(data);
				Debug.LogError($"[Config] Duplicate key: {(data.Type, data.SpawnId)} in table: TbGlobalGuaranteed");
			}
		}
		foreach (GlobalGuaranteedInfo item2 in list)
		{
			_dataList.Remove(item2);
		}
	}

	public GlobalGuaranteedInfo Get(GuaranteedType type, string spawn_id)
	{
		if (!_dataMapUnion.TryGetValue((type, spawn_id), out var value))
		{
			return null;
		}
		return value;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (GlobalGuaranteedInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (GlobalGuaranteedInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
