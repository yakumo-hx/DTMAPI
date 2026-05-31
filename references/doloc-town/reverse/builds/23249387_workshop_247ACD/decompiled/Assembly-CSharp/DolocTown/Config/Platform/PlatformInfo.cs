using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;
using UnityEngine.Tilemaps;

namespace DolocTown.Config.Platform;

public sealed class PlatformInfo : BeanBase
{
	public const int __ID__ = -1218066084;

	public string Id { get; private set; }

	public SpriteAsset SceneSpriteAsset { get; private set; }

	public PlatformSurfaceData PlatformSuface { get; private set; }

	public PlatformColumnData PlatformLeftColumn { get; private set; }

	public PlatformColumnData PlatformRightColumn { get; private set; }

	public string Title => DolocAPI.GetItemTitle(Id);

	public string Description => DolocAPI.GetItemDescription(Id);

	public UnityEngine.Tilemaps.Tile LeftSockTile => PlatformTileGeneratorUtils.GetTileBySpriteAsset(PlatformSuface.LeftSock);

	public UnityEngine.Tilemaps.Tile RightSockTile => PlatformTileGeneratorUtils.GetTileBySpriteAsset(PlatformSuface.RightSock);

	public UnityEngine.Tilemaps.Tile ColumnTile => PlatformTileGeneratorUtils.GetTileBySpriteAsset(PlatformLeftColumn.ColumnBottomTile);

	public CountItem[] CostItems
	{
		get
		{
			if (DolocAPI.QueryRecipe(Id, out var recipe))
			{
				return recipe.InputItems;
			}
			return null;
		}
	}

	public PlatformInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["scene_sprite_asset"].IsObject)
		{
			throw new SerializationException();
		}
		SceneSpriteAsset = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["scene_sprite_asset"]));
		if (!_json["platform_suface"].IsObject)
		{
			throw new SerializationException();
		}
		PlatformSuface = PlatformSurfaceData.DeserializePlatformSurfaceData(_json["platform_suface"]);
		if (!_json["platform_left_column"].IsObject)
		{
			throw new SerializationException();
		}
		PlatformLeftColumn = PlatformColumnData.DeserializePlatformColumnData(_json["platform_left_column"]);
		if (!_json["platform_right_column"].IsObject)
		{
			throw new SerializationException();
		}
		PlatformRightColumn = PlatformColumnData.DeserializePlatformColumnData(_json["platform_right_column"]);
	}

	public PlatformInfo(string id, SpriteAsset scene_sprite_asset, PlatformSurfaceData platform_suface, PlatformColumnData platform_left_column, PlatformColumnData platform_right_column)
	{
		Id = id;
		SceneSpriteAsset = scene_sprite_asset;
		PlatformSuface = platform_suface;
		PlatformLeftColumn = platform_left_column;
		PlatformRightColumn = platform_right_column;
	}

	public static PlatformInfo DeserializePlatformInfo(JSONNode _json)
	{
		return new PlatformInfo(_json);
	}

	public override int GetTypeId()
	{
		return -1218066084;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		PlatformSuface?.Resolve(_tables);
		PlatformLeftColumn?.Resolve(_tables);
		PlatformRightColumn?.Resolve(_tables);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		PlatformSuface?.TranslateText(translator);
		PlatformLeftColumn?.TranslateText(translator);
		PlatformRightColumn?.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",SceneSpriteAsset:" + SceneSpriteAsset?.ToString() + ",PlatformSuface:" + PlatformSuface?.ToString() + ",PlatformLeftColumn:" + PlatformLeftColumn?.ToString() + ",PlatformRightColumn:" + PlatformRightColumn?.ToString() + ",}";
	}
}
