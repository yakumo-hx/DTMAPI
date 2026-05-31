using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;

namespace DolocTown.Config.Building;

public sealed class BuildingExteriorData : BeanBase
{
	public const int __ID__ = 1696745302;

	public string BuildingId { get; private set; }

	public BuildingInfo BuildingId_Ref { get; private set; }

	public SpriteAsset OverrideItemIcon { get; private set; }

	public BuildingSpriteGroup ClosedSpriteGroup { get; private set; }

	public BuildingSpriteGroup OpenedSpriteGroup { get; private set; }

	public bool CanOpenDoor => !OpenedSpriteGroup.SceneSprite.AssetUrl.IsNullOrEmpty();

	public BuildingExteriorData(JSONNode _json)
	{
		if (!_json["building_id"].IsString)
		{
			throw new SerializationException();
		}
		BuildingId = _json["building_id"];
		if (!_json["override_item_icon"].IsObject)
		{
			throw new SerializationException();
		}
		OverrideItemIcon = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["override_item_icon"]));
		if (!_json["closed_sprite_group"].IsObject)
		{
			throw new SerializationException();
		}
		ClosedSpriteGroup = BuildingSpriteGroup.DeserializeBuildingSpriteGroup(_json["closed_sprite_group"]);
		if (!_json["opened_sprite_group"].IsObject)
		{
			throw new SerializationException();
		}
		OpenedSpriteGroup = BuildingSpriteGroup.DeserializeBuildingSpriteGroup(_json["opened_sprite_group"]);
	}

	public BuildingExteriorData(string building_id, SpriteAsset override_item_icon, BuildingSpriteGroup closed_sprite_group, BuildingSpriteGroup opened_sprite_group)
	{
		BuildingId = building_id;
		OverrideItemIcon = override_item_icon;
		ClosedSpriteGroup = closed_sprite_group;
		OpenedSpriteGroup = opened_sprite_group;
	}

	public static BuildingExteriorData DeserializeBuildingExteriorData(JSONNode _json)
	{
		return new BuildingExteriorData(_json);
	}

	public override int GetTypeId()
	{
		return 1696745302;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		BuildingId_Ref = (_tables["Building.TbBuilding"] as TbBuilding).GetOrDefault(BuildingId);
		ClosedSpriteGroup?.Resolve(_tables);
		OpenedSpriteGroup?.Resolve(_tables);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
		ClosedSpriteGroup?.TranslateText(translator);
		OpenedSpriteGroup?.TranslateText(translator);
	}

	public override string ToString()
	{
		return "{ BuildingId:" + BuildingId + ",OverrideItemIcon:" + OverrideItemIcon?.ToString() + ",ClosedSpriteGroup:" + ClosedSpriteGroup?.ToString() + ",OpenedSpriteGroup:" + OpenedSpriteGroup?.ToString() + ",}";
	}
}
