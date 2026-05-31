using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Asset;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Drone;

public sealed class DroneAssistInfo : BeanBase
{
	public const int __ID__ = 816050393;

	public string Id { get; private set; }

	public SpriteAsset Sprite { get; private set; }

	public Vector2Int SpritePivot { get; private set; }

	public string SkillId { get; private set; }

	public DroneAssistInfo(JSONNode _json)
	{
		if (!_json["id"].IsString)
		{
			throw new SerializationException();
		}
		Id = _json["id"];
		if (!_json["sprite"].IsObject)
		{
			throw new SerializationException();
		}
		Sprite = ExternalTypeUtil.SpriteAssetConverter(CfgSpriteAsset.DeserializeCfgSpriteAsset(_json["sprite"]));
		if (!_json["sprite_pivot"].IsObject)
		{
			throw new SerializationException();
		}
		SpritePivot = ExternalTypeUtil.Vector2IntConverter(CfgVector2Int.DeserializeCfgVector2Int(_json["sprite_pivot"]));
		if (!_json["skillId"].IsString)
		{
			throw new SerializationException();
		}
		SkillId = _json["skillId"];
	}

	public DroneAssistInfo(string id, SpriteAsset sprite, Vector2Int sprite_pivot, string skillId)
	{
		Id = id;
		Sprite = sprite;
		SpritePivot = sprite_pivot;
		SkillId = skillId;
	}

	public static DroneAssistInfo DeserializeDroneAssistInfo(JSONNode _json)
	{
		return new DroneAssistInfo(_json);
	}

	public override int GetTypeId()
	{
		return 816050393;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",Sprite:" + Sprite?.ToString() + ",SpritePivot:" + SpritePivot.ToString() + ",SkillId:" + SkillId + ",}";
	}
}
