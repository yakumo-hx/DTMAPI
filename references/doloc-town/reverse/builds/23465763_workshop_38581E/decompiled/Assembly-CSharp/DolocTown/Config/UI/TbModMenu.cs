using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.UI;

public sealed class TbModMenu
{
	private readonly Dictionary<ModMenuType, ModMenuInfo> _dataMap;

	private readonly List<ModMenuInfo> _dataList;

	public Dictionary<ModMenuType, ModMenuInfo> DataMap => _dataMap;

	public List<ModMenuInfo> DataList => _dataList;

	public ModMenuInfo this[ModMenuType key] => _dataMap[key];

	public TbModMenu(JSONNode _json)
	{
		_dataMap = new Dictionary<ModMenuType, ModMenuInfo>();
		_dataList = new List<ModMenuInfo>();
		foreach (JSONNode child in _json.Children)
		{
			ModMenuInfo modMenuInfo = ModMenuInfo.DeserializeModMenuInfo(child);
			if (_dataMap.TryAdd(modMenuInfo.Id, modMenuInfo))
			{
				_dataList.Add(modMenuInfo);
			}
			else
			{
				Debug.LogError($"[Config] Duplicate key: {modMenuInfo.Id} in table: TbModMenu");
			}
		}
	}

	public ModMenuInfo GetOrDefault(ModMenuType key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public ModMenuInfo Get(ModMenuType key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (ModMenuInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (ModMenuInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
