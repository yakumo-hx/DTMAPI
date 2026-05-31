using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Plant;

public sealed class TbCropGeneMatrix
{
	private readonly Dictionary<string, CropGeneMatrixInfo> _dataMap;

	private readonly List<CropGeneMatrixInfo> _dataList;

	public Dictionary<string, CropGeneMatrixInfo> DataMap => _dataMap;

	public List<CropGeneMatrixInfo> DataList => _dataList;

	public CropGeneMatrixInfo this[string key] => _dataMap[key];

	public TbCropGeneMatrix(JSONNode _json)
	{
		_dataMap = new Dictionary<string, CropGeneMatrixInfo>();
		_dataList = new List<CropGeneMatrixInfo>();
		foreach (JSONNode child in _json.Children)
		{
			CropGeneMatrixInfo cropGeneMatrixInfo = CropGeneMatrixInfo.DeserializeCropGeneMatrixInfo(child);
			if (_dataMap.TryAdd(cropGeneMatrixInfo.Id, cropGeneMatrixInfo))
			{
				_dataList.Add(cropGeneMatrixInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + cropGeneMatrixInfo.Id + " in table: TbCropGeneMatrix");
			}
		}
	}

	public CropGeneMatrixInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public CropGeneMatrixInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (CropGeneMatrixInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (CropGeneMatrixInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}

	public bool TryRollGene(string seedName, out string geneId)
	{
		geneId = null;
		return GetOrDefault(seedName ?? "")?.TryRollGene(out geneId) ?? false;
	}

	public bool TryRollGene(string seedName, out string geneId, string[] availableGeneNames)
	{
		geneId = null;
		return GetOrDefault(seedName ?? "")?.TryRollGene(out geneId, availableGeneNames) ?? false;
	}
}
