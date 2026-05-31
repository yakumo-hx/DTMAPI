using System;
using System.Collections.Generic;
using Bright.Serialization;
using DolocTown.Config.Building;
using SimpleJSON;

namespace DolocTown.Config.Item;

public sealed class ItemFunctionWallpaper : ItemFunctionBase
{
	public const int __ID__ = -1403156068;

	public string WallpaperId { get; private set; }

	public BuildingWallpaperInfo WallpaperId_Ref { get; private set; }

	public ItemFunctionWallpaper(JSONNode _json)
		: base(_json)
	{
		if (!_json["wallpaper_id"].IsString)
		{
			throw new SerializationException();
		}
		WallpaperId = _json["wallpaper_id"];
	}

	public ItemFunctionWallpaper(string wallpaper_id)
	{
		WallpaperId = wallpaper_id;
	}

	public static ItemFunctionWallpaper DeserializeItemFunctionWallpaper(JSONNode _json)
	{
		return new ItemFunctionWallpaper(_json);
	}

	public override int GetTypeId()
	{
		return -1403156068;
	}

	public override void Resolve(Dictionary<string, object> _tables)
	{
		base.Resolve(_tables);
		WallpaperId_Ref = (_tables["Building.TbBuildingWallpaper"] as TbBuildingWallpaper).GetOrDefault(WallpaperId);
	}

	public override void TranslateText(Func<string, string, string> translator)
	{
		base.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ WallpaperId:" + WallpaperId + ",}";
	}
}
