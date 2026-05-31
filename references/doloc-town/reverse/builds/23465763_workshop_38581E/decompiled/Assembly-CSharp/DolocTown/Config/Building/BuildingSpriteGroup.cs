using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;

namespace DolocTown.Config.Building;

public sealed class BuildingSpriteGroup : BeanBase
{
	public const int __ID__ = 178693420;

	public SpriteAsset SceneSprite { get; private set; }

	public SpriteAsset DoorSprite { get; private set; }

	public BuildingSpriteGroup(JSONNode _json)
	{
		if (!_json["scene_sprite"].IsObject)
		{
			throw new SerializationException();
		}
		SceneSprite = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["scene_sprite"]));
		if (!_json["door_sprite"].IsObject)
		{
			throw new SerializationException();
		}
		DoorSprite = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["door_sprite"]));
	}

	public BuildingSpriteGroup(SpriteAsset scene_sprite, SpriteAsset door_sprite)
	{
		SceneSprite = scene_sprite;
		DoorSprite = door_sprite;
	}

	public static BuildingSpriteGroup DeserializeBuildingSpriteGroup(JSONNode _json)
	{
		return new BuildingSpriteGroup(_json);
	}

	public override int GetTypeId()
	{
		return 178693420;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ SceneSprite:" + SceneSprite?.ToString() + ",DoorSprite:" + DoorSprite?.ToString() + ",}";
	}
}
