using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;

namespace DolocTown.Config.Building;

public sealed class BuildingWallpaperData : BeanBase
{
	public const int __ID__ = 333675198;

	public string BuildingId { get; private set; }

	public BuildingInfo BuildingId_Ref { get; private set; }

	public SpriteAsset InternalSprite { get; private set; }

	public SpriteAsset WindowMask { get; private set; }

	public bool ShowWindow => !WindowMask.AssetUrl.IsNullOrEmpty();

	public BuildingWallpaperData(JSONNode _json)
	{
		if (!_json["building_id"].IsString)
		{
			throw new SerializationException();
		}
		BuildingId = _json["building_id"];
		if (!_json["internal_sprite"].IsObject)
		{
			throw new SerializationException();
		}
		InternalSprite = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["internal_sprite"]));
		if (!_json["window_mask"].IsObject)
		{
			throw new SerializationException();
		}
		WindowMask = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["window_mask"]));
	}

	public BuildingWallpaperData(string building_id, SpriteAsset internal_sprite, SpriteAsset window_mask)
	{
		BuildingId = building_id;
		InternalSprite = internal_sprite;
		WindowMask = window_mask;
	}

	public static BuildingWallpaperData DeserializeBuildingWallpaperData(JSONNode _json)
	{
		return new BuildingWallpaperData(_json);
	}

	public override int GetTypeId()
	{
		return 333675198;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		BuildingId_Ref = (_tables["Building.TbBuilding"] as TbBuilding).GetOrDefault(BuildingId);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ BuildingId:" + BuildingId + ",InternalSprite:" + InternalSprite?.ToString() + ",WindowMask:" + WindowMask?.ToString() + ",}";
	}
}
