using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Sound;

public sealed class TbExclusiveSFX
{
	private readonly Dictionary<string, ExclusiveSFXInfo> _dataMap;

	private readonly List<ExclusiveSFXInfo> _dataList;

	public Dictionary<string, ExclusiveSFXInfo> DataMap => _dataMap;

	public List<ExclusiveSFXInfo> DataList => _dataList;

	public ExclusiveSFXInfo this[string key] => _dataMap[key];

	public TbExclusiveSFX(JSONNode _json)
	{
		_dataMap = new Dictionary<string, ExclusiveSFXInfo>();
		_dataList = new List<ExclusiveSFXInfo>();
		foreach (JSONNode child in _json.Children)
		{
			ExclusiveSFXInfo exclusiveSFXInfo = ExclusiveSFXInfo.DeserializeExclusiveSFXInfo(child);
			if (_dataMap.TryAdd(exclusiveSFXInfo.SoundEvent, exclusiveSFXInfo))
			{
				_dataList.Add(exclusiveSFXInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + exclusiveSFXInfo.SoundEvent + " in table: TbExclusiveSFX");
			}
		}
	}

	public ExclusiveSFXInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public ExclusiveSFXInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (ExclusiveSFXInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (ExclusiveSFXInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
