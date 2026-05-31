using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Building;

public sealed class TbBuildingWallpaper
{
	private readonly Dictionary<string, BuildingWallpaperInfo> _dataMap;

	private readonly List<BuildingWallpaperInfo> _dataList;

	public Dictionary<string, BuildingWallpaperInfo> DataMap => _dataMap;

	public List<BuildingWallpaperInfo> DataList => _dataList;

	public BuildingWallpaperInfo this[string key] => _dataMap[key];

	public TbBuildingWallpaper(JSONNode _json)
	{
		_dataMap = new Dictionary<string, BuildingWallpaperInfo>();
		_dataList = new List<BuildingWallpaperInfo>();
		foreach (JSONNode child in _json.Children)
		{
			BuildingWallpaperInfo buildingWallpaperInfo = BuildingWallpaperInfo.DeserializeBuildingWallpaperInfo(child);
			if (_dataMap.TryAdd(buildingWallpaperInfo.Id, buildingWallpaperInfo))
			{
				_dataList.Add(buildingWallpaperInfo);
			}
			else
			{
				Debug.LogError("[Config] Duplicate key: " + buildingWallpaperInfo.Id + " in table: TbBuildingWallpaper");
			}
		}
	}

	public BuildingWallpaperInfo GetOrDefault(string key)
	{
		if (!_dataMap.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	public BuildingWallpaperInfo Get(string key)
	{
		return _dataMap[key];
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (BuildingWallpaperInfo data in _dataList)
		{
			data.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (BuildingWallpaperInfo data in _dataList)
		{
			data.TranslateText(translator);
		}
	}
}
