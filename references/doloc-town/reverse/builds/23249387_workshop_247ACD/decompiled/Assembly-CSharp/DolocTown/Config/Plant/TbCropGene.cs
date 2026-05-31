using System;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Plant;

public sealed class TbCropGene
{
	private readonly Dictionary<string, CropGeneInfo> _dataMap;

	private readonly List<CropGeneInfo> _dataList;

	public Dictionary<string, CropGeneInfo> DataMap => _dataMap;

	public List<CropGeneInfo> DataList => _dataList;

	public CropGeneInfo this[string key] => _dataMap[key];

	public string[] DefaultUnlockedGenes { get; private set; }

	public TbCropGene(JSONNode _json)
	{
		_dataMap = new Dictionary<string, CropGeneInfo>();
		_dataList = new List<CropGeneInfo>();
		foreach (JSONNode child in _json.Children)
		{
			CropGeneInfo cropGeneInfo = CropGeneInfo.DeserializeCropGeneInfo(child);
			if (_dataMap.TryAdd(cropGeneInfo.Id, cropGeneInfo))
			{
				_dataList.Add(cropGeneInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + cropGeneInfo.Id + " in table: TbCropGene");
			}
		}
	}

	public CropGeneInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public CropGeneInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (CropGeneInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
		PostResolve();
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (CropGeneInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}

	private void PostResolve()
	{
		DefaultUnlockedGenes = (from x in _dataList
			where x.DefaultUnlock
			select x.Id).ToArray();
	}
}
