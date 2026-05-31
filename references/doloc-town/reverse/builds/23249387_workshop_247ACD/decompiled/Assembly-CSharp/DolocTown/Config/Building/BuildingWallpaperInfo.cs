using System;
using System.Collections.Generic;
using System.Linq;
using Bright.Common;
using Bright.Config;
using Bright.Serialization;
using SimpleJSON;

namespace DolocTown.Config.Building;

public sealed class BuildingWallpaperInfo : BeanBase
{
	public readonly Dictionary<string, BuildingWallpaperData> WallpaperDatas_Index = new Dictionary<string, BuildingWallpaperData>();

	public const int __ID__ = 333836226;

	public string Id { get; private set; }

	public List<BuildingWallpaperData> WallpaperDatas { get; private set; }

	public bool AdaptAll => DolocConfig.Tables.TbBuilding.DataList.All((BuildingInfo info) => info.DefaultWallpaper.IsNullOrEmpty() || WallpaperDatas_Index.ContainsKey(info.Id));

	public BuildingWallpaperInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		JSONNode jSONNode = _json["wallpaper_datas"];
		if (!jSONNode.IsArray)
		{
			throw new SerializationException();
		}
		WallpaperDatas = new List<BuildingWallpaperData>(jSONNode.Count);
		foreach (JSONNode child in jSONNode.Children)
		{
			if (!child.IsObject)
			{
				throw new SerializationException();
			}
			BuildingWallpaperData item = BuildingWallpaperData.DeserializeBuildingWallpaperData(child);
			WallpaperDatas.Add(item);
		}
		foreach (BuildingWallpaperData wallpaperData in WallpaperDatas)
		{
			WallpaperDatas_Index.Add(wallpaperData.BuildingId, wallpaperData);
		}
	}

	public BuildingWallpaperInfo(string id, List<BuildingWallpaperData> wallpaper_datas)
	{
		Id = id;
		WallpaperDatas = wallpaper_datas;
		foreach (BuildingWallpaperData wallpaperData in WallpaperDatas)
		{
			WallpaperDatas_Index.Add(wallpaperData.BuildingId, wallpaperData);
		}
	}

	public static BuildingWallpaperInfo DeserializeBuildingWallpaperInfo(JSONNode _json)
	{
		return new BuildingWallpaperInfo(_json);
	}

	public override int GetTypeId()
	{
		return 333836226;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		foreach (BuildingWallpaperData wallpaperData in WallpaperDatas)
		{
			wallpaperData?.Resolve(_tables);
		}
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		foreach (BuildingWallpaperData wallpaperData in WallpaperDatas)
		{
			wallpaperData?.TranslateText(translator);
		}
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",WallpaperDatas:" + StringUtil.CollectionToString(WallpaperDatas) + ",}";
	}
}
