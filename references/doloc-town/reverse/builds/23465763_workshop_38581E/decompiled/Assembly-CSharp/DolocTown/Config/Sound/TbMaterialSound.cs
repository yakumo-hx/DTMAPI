using System;
using System.Collections.Generic;
using DolocTown.Config.Tile;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Sound;

public sealed class TbMaterialSound
{
	private readonly Dictionary<TileMaterial, MaterialSoundInfo> _dataMap;

	private readonly List<MaterialSoundInfo> _dataList;

	public Dictionary<TileMaterial, MaterialSoundInfo> DataMap => _dataMap;

	public List<MaterialSoundInfo> DataList => _dataList;

	public MaterialSoundInfo this[TileMaterial key] => _dataMap[key];

	public TbMaterialSound(JSONNode _json)
	{
		_dataMap = new Dictionary<TileMaterial, MaterialSoundInfo>();
		_dataList = new List<MaterialSoundInfo>();
		foreach (JSONNode child in _json.Children)
		{
			MaterialSoundInfo materialSoundInfo = MaterialSoundInfo.DeserializeMaterialSoundInfo(child);
			if (_dataMap.TryAdd(materialSoundInfo.Id, materialSoundInfo))
			{
				_dataList.Add(materialSoundInfo);
			}
			else
			{
				Debug.LogError($"[Config] Duplicate key: {materialSoundInfo.Id} in table: TbMaterialSound");
			}
		}
	}

	public MaterialSoundInfo GetOrDefault(TileMaterial key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public MaterialSoundInfo Get(TileMaterial key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (MaterialSoundInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (MaterialSoundInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
