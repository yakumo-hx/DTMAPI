using System;
using System.Collections.Generic;
using Bright.Config;
using Bright.Serialization;
using DolocTown.Config.Asset;
using DolocTown.Config.Item;
using SimpleJSON;
using UnityEngine;

namespace DolocTown.Config.Drone;

public sealed class DroneEngineInfo : BeanBase
{
	public const int __ID__ = 881447986;

	public string Id { get; private set; }

	public ItemInfo Id_Ref { get; private set; }

	public SpriteAsset Sprite { get; private set; }

	public Vector2Int SpritePivot { get; private set; }

	public float MoveSpeedIncrease { get; private set; }

	public float PowerCapacityIncrease { get; private set; }

	public float PowerRecvIncrease { get; private set; }

	public string SkillId { get; private set; }

	public DroneEngineInfo(JSONNode _json)
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
		if (!_json["move_speed_increase"].IsNumber)
		{
			throw new SerializationException();
		}
		MoveSpeedIncrease = _json["move_speed_increase"];
		if (!_json["power_capacity_increase"].IsNumber)
		{
			throw new SerializationException();
		}
		PowerCapacityIncrease = _json["power_capacity_increase"];
		if (!_json["power_recv_increase"].IsNumber)
		{
			throw new SerializationException();
		}
		PowerRecvIncrease = _json["power_recv_increase"];
		if (!_json["skillId"].IsString)
		{
			throw new SerializationException();
		}
		SkillId = _json["skillId"];
	}

	public DroneEngineInfo(string id, SpriteAsset sprite, Vector2Int sprite_pivot, float move_speed_increase, float power_capacity_increase, float power_recv_increase, string skillId)
	{
		Id = id;
		Sprite = sprite;
		SpritePivot = sprite_pivot;
		MoveSpeedIncrease = move_speed_increase;
		PowerCapacityIncrease = power_capacity_increase;
		PowerRecvIncrease = power_recv_increase;
		SkillId = skillId;
	}

	public static DroneEngineInfo DeserializeDroneEngineInfo(JSONNode _json)
	{
		return new DroneEngineInfo(_json);
	}

	public override int GetTypeId()
	{
		return 881447986;
	}

	public void Resolve(Dictionary<string, object> _tables)
	{
		Id_Ref = (_tables["Item.TbItem"] as TbItem).GetOrDefault(Id);
	}

	public void TranslateText(Func<string, string, string> translator)
	{
	}

	public override string ToString()
	{
		return "{ Id:" + Id + ",Sprite:" + Sprite?.ToString() + ",SpritePivot:" + SpritePivot.ToString() + ",MoveSpeedIncrease:" + MoveSpeedIncrease + ",PowerCapacityIncrease:" + PowerCapacityIncrease + ",PowerRecvIncrease:" + PowerRecvIncrease + ",SkillId:" + SkillId + ",}";
	}
}
